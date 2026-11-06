import { useRef, useState, type KeyboardEvent } from 'react';
import { Check, Play, Save, TerminalSquare, X } from 'lucide-react';
import type { RunResult } from './types';

function highlighted(source:string){
  const pattern=/(\/\*[\s\S]*?\*\/|\/\/[^\n]*|#[^\n]*|"(?:\\.|[^"\\])*"|'(?:\\.|[^'\\])*'|\b(?:module|exports|function|return|if|else|const|let|true|false|throw|new|for|of|while)\b|\b\d+\b)/g;
  const parts=[];let start=0;let match:RegExpExecArray|null;
  while((match=pattern.exec(source))){if(match.index>start)parts.push(source.slice(start,match.index));const value=match[0];const kind=value.startsWith('//')||value.startsWith('#')||value.startsWith('/*')?'comment':value.startsWith('"')||value.startsWith("'")?'string':/^\d/.test(value)?'number':'keyword';parts.push(<span key={match.index} className={`syntax-${kind}`}>{value}</span>);start=match.index+value.length;}
  parts.push(source.slice(start));return parts;
}
export function Editor({path,initial,busy,save,run,close}:{path:string;initial:string;busy:boolean;save:(value:string)=>boolean;run:(source:string,test:boolean)=>Promise<RunResult>;close:()=>void}){
  const [value,setValue]=useState(initial);const [saved,setSaved]=useState(initial);const [result,setResult]=useState<RunResult|null>(null);const [discard,setDiscard]=useState(false);
  const textarea=useRef<HTMLTextAreaElement>(null);const highlight=useRef<HTMLPreElement>(null);const gutter=useRef<HTMLPreElement>(null);
  const isJS=path.endsWith('.js');const dirty=value!==saved;
  const saveNow=()=>{if(!dirty)return true;if(save(value)){setSaved(value);return true;}return false;};
  const execute=async()=>{setResult(await run(value,path.endsWith('filter.js')));};
  const keyDown=(event:KeyboardEvent<HTMLTextAreaElement>)=>{
    if((event.ctrlKey||event.metaKey)&&event.key.toLowerCase()==='s'){event.preventDefault();saveNow();return;}
    if((event.ctrlKey||event.metaKey)&&event.key==='Enter'){event.preventDefault();if(isJS&&!busy)void execute();return;}
    if(event.key==='Tab'){event.preventDefault();const element=event.currentTarget;const start=element.selectionStart;const end=element.selectionEnd;setValue(value.slice(0,start)+'  '+value.slice(end));requestAnimationFrame(()=>{element.selectionStart=element.selectionEnd=start+2;});}
    if(event.key==='Enter'){const element=event.currentTarget;const start=element.selectionStart;const line=value.slice(0,start).split('\n').pop()??'';const indent=(line.match(/^\s*/)?.[0]??'')+(line.trimEnd().endsWith('{')?'  ':'');if(indent){event.preventDefault();setValue(value.slice(0,start)+'\n'+indent+value.slice(element.selectionEnd));requestAnimationFrame(()=>{element.selectionStart=element.selectionEnd=start+1+indent.length;});}}
  };
  return <section className="code-window" aria-label={`Редактор ${path}`}><header className="window-header"><div><span className="window-light amber"/><TerminalSquare size={14}/><span>EDIT</span><code>{path}</code>{dirty&&<i className="dirty-dot"/>}</div><button className="icon-button" aria-label="Закрыть редактор" onClick={()=>dirty?setDiscard(true):close()}><X size={16}/></button></header>
    <div className="editor-toolbar"><span>{isJS?'JavaScript / ES2022':'UTF-8 / конфигурация'}</span><span>{dirty?'НЕ СОХРАНЕНО':'СОХРАНЕНО'}</span></div>
    <div className="code-surface"><pre className="line-numbers" ref={gutter} aria-hidden="true">{value.split('\n').map((_,i)=>i+1).join('\n')}</pre><div className="editor-scroll-area"><pre className="code-highlight" ref={highlight} aria-hidden="true">{highlighted(value)}{'\n'}</pre><textarea ref={textarea} aria-label={`Код файла ${path}`} className="editor-input" wrap="off" disabled={busy} maxLength={100000} value={value} spellCheck={false} autoCapitalize="off" autoComplete="off" autoCorrect="off" onChange={event=>{setValue(event.target.value);setResult(null);}} onKeyDown={keyDown} onScroll={event=>{if(highlight.current)highlight.current.style.transform=`translate(${-event.currentTarget.scrollLeft}px,${-event.currentTarget.scrollTop}px)`;if(gutter.current)gutter.current.scrollTop=event.currentTarget.scrollTop;}}/></div></div>
    {result&&<div className={`editor-result ${result.ok?'passed':'failed'}`} role="status"><strong>{result.ok?'PROCESS EXIT 0':'PROCESS EXIT 1'}</strong><pre>{result.stdout}{result.error?`\n${result.error}`:''}</pre></div>}
    {discard?<div className="editor-discard"><span>Сохранить изменения?</span><button disabled={busy} onClick={()=>{if(saveNow())close();}}>Сохранить</button><button onClick={close}>Отбросить</button><button aria-label="Вернуться к редактированию" onClick={()=>setDiscard(false)}><X size={15}/></button></div>:<footer className="editor-footer"><button className="small-button" disabled={busy||!dirty} onClick={saveNow}><Save size={14}/>{dirty?'Сохранить':'Сохранено'}<kbd>⌘S</kbd></button>{isJS&&<button className="small-button accent" disabled={busy} onClick={()=>void execute()}><Play size={13}/>{busy?'Выполняется…':path.endsWith('filter.js')?'Тест черновика':'Выполнить JS'}<kbd>⌘↵</kbd></button>}{!isJS&&!dirty&&<span className="saved-hint"><Check size={13}/>Изменения уже на сервере</span>}</footer>}
  </section>;
}
