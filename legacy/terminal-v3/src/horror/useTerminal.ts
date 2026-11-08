import { useCallback, useEffect, useRef, useState } from 'react';
import { executeShell, testPolicy } from './shell';
import { createWorld, output, saveFile } from './world';
import { runCode } from './runner';
import type { Host, RunResult, World } from './types';

const KEY='hostfall.terminal.v3';
interface Saved { world:World; started:boolean }
function load():Saved {
  try{
    const raw=localStorage.getItem(KEY);if(!raw)return{world:createWorld(),started:false};
    const save=JSON.parse(raw) as Saved;const w=save.world;
    if(w.version!==3||!['ws07','edge01','core01'].includes(w.host)||!Number.isInteger(w.stage)||w.stage<0||w.stage>3||!Array.isArray(w.history)||!Array.isArray(w.commands)||!Array.isArray(w.notices)||!Number.isFinite(w.turn)||!Number.isFinite(w.convergence)||!['active','contained','costly','assimilated'].includes(w.outcome))throw Error('Invalid session');
    for(const host of ['ws07','edge01','core01'] as const){if(!Array.isArray(w.processes[host])||!Array.isArray(w.directories[host])||typeof w.cwd[host]!=='string')throw Error('Invalid host');for(const file of Object.values(w.files[host]))if(typeof file.content!=='string'||file.content.length>100_000||typeof file.writable!=='boolean')throw Error('Invalid file');}
    return{world:w,started:Boolean(save.started)};
  }catch{return{world:createWorld(),started:false};}
}
export function useTerminal(){
  const [initial]=useState(load);const [world,setWorld]=useState(initial.world);const model=useRef(initial.world);
  const [started,setStarted]=useState(initial.started);const [busy,setBusy]=useState(false);const lock=useRef(false);const generation=useRef(0);
  const [editor,setEditor]=useState<{host:Host;path:string}|null>(null);const [storageError,setStorageError]=useState(false);
  const commit=useCallback((next:World)=>{model.current=next;setWorld(next);},[]);
  const execute=useCallback(async(line:string)=>{
    if(lock.current||!line.trim())return;lock.current=true;setBusy(true);const current=generation.current;
    try{const result=await executeShell(model.current,line,runCode);if(current===generation.current){commit(result.world);if(result.editor)setEditor(result.editor);}}
    finally{if(current===generation.current){lock.current=false;setBusy(false);}}
  },[commit]);
  const save=useCallback((host:Host,path:string,content:string)=>{
    if(lock.current)return false;const next=structuredClone(model.current);const result=saveFile(next,host,path,content);
    output(next,result.ok?`${path} / ${new TextEncoder().encode(content).length} bytes saved.`:result.error!,result.ok?'success':'danger');commit(next);return result.ok;
  },[commit]);
  const runEditor=useCallback(async(source:string,test:boolean):Promise<RunResult>=>{
    if(lock.current)return{ok:false,stdout:'',values:[],error:'Процесс уже выполняется.'};
    lock.current=true;setBusy(true);const current=generation.current;
    try{if(test){const result=await testPolicy(source,runCode);return{ok:result.ok,stdout:result.stdout,values:result.values,error:result.error};}return await runCode(source);}
    finally{if(current===generation.current){lock.current=false;setBusy(false);}}
  },[]);
  const restart=()=>{generation.current++;lock.current=false;setBusy(false);commit(createWorld());setEditor(null);setStarted(true);};
  useEffect(()=>{try{localStorage.setItem(KEY,JSON.stringify({world,started}));setStorageError(false);}catch{setStorageError(true);}},[world,started]);
  return{world,started,begin:()=>setStarted(true),busy,editor,setEditor,execute,save,runEditor,restart,storageError};
}
