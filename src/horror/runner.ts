import type { CodeRunner, RunResult } from './types';

// User code executes only in a disposable worker owned by an opaque, sandboxed
// iframe. Its own CSP denies network, external scripts, frames and resources.
// It never runs in the application's origin or receives game/account state.
export const WORKER_SOURCE = `
self.onmessage = async ({data}) => {
  const lines=[];
  const format=x=>typeof x==='string'?x:JSON.stringify(x);
  const logger={};
  for(const method of ['log','info','warn','error','debug'])logger[method]=(...args)=>{if(lines.length<80)lines.push(args.map(format).join(' ').slice(0,3000));};
  try {
    const module={exports:{}};
    new Function('module','exports','console','"use strict";\\n'+data.source)(module,module.exports,logger);
    const values=[];
    if(data.inputs.length){
      if(typeof module.exports!=='function')throw Error('Экспортируй функцию: module.exports = function(packet) { ... }');
      for(const input of data.inputs)values.push(await module.exports(input));
    }
    self.postMessage({ok:true,stdout:lines.join('\\n'),values});
  }catch(error){self.postMessage({ok:false,stdout:lines.join('\\n'),values:[],error:error.name+': '+error.message});}
};`;
function documentSource(){
  const bootstrap=`
    addEventListener('message',event=>{
      if(event.source!==parent||!event.data||event.data.type!=='EXECUTE')return;
      const worker=new Worker(URL.createObjectURL(new Blob([${JSON.stringify(WORKER_SOURCE)}],{type:'text/javascript'})));
      const timer=setTimeout(()=>{worker.terminate();parent.postMessage({token:event.data.token,result:{ok:false,stdout:'',values:[],error:'ExecutionTimeout: процесс остановлен через 700 мс. Проверь цикл.'}},'*');},700);
      worker.onmessage=message=>{clearTimeout(timer);worker.terminate();parent.postMessage({token:event.data.token,result:message.data},'*');};
      worker.onerror=error=>{clearTimeout(timer);worker.terminate();parent.postMessage({token:event.data.token,result:{ok:false,stdout:'',values:[],error:error.message}},'*');};
      worker.postMessage({source:event.data.source,inputs:event.data.inputs});
    });
    parent.postMessage({ready:true},'*');
  `;
  return `<!doctype html><meta http-equiv="Content-Security-Policy" content="default-src 'none'; script-src 'unsafe-inline' 'unsafe-eval' blob:; worker-src blob:; connect-src 'none'; img-src 'none'; frame-src 'none'; form-action 'none'; base-uri 'none'"><script>${bootstrap.replace(/<\/script/gi,'<\\/script')}</script>`;
}
export const runCode:CodeRunner=(source,inputs=[])=>new Promise<RunResult>(resolve=>{
  const frame=document.createElement('iframe');frame.sandbox.add('allow-scripts');frame.hidden=true;frame.title='Изолированный процесс JavaScript';
  const token=crypto.randomUUID();let settled=false;let sent=false;let executionDeadline:number|undefined;
  const finish=(result:RunResult)=>{if(settled)return;settled=true;clearTimeout(deadline);clearTimeout(executionDeadline);window.removeEventListener('message',message);frame.remove();resolve(result);};
  const message=(event:MessageEvent)=>{
    if(event.source!==frame.contentWindow||!event.data)return;
    if(event.data.ready&&!sent){sent=true;clearTimeout(deadline);executionDeadline=window.setTimeout(()=>finish({ok:false,stdout:'',values:[],error:'ExecutionTimeout: процесс остановлен. Проверь условие выхода из цикла.'}),1500);frame.contentWindow?.postMessage({type:'EXECUTE',token,source,inputs},'*');}
    else if(event.data.token===token&&event.data.result){const result=event.data.result as RunResult;finish({ok:result.ok===true,stdout:String(result.stdout??'').slice(0,10000),values:Array.isArray(result.values)?result.values:[],error:result.error?String(result.error):undefined});}
  };
  const deadline=window.setTimeout(()=>finish({ok:false,stdout:'',values:[],error:'RuntimeTimeout: процесс не ответил. Попробуй снова.'}),5000);
  window.addEventListener('message',message);frame.srcdoc=documentSource();document.body.append(frame);
});
