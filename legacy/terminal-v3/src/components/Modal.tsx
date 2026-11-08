import { useEffect, useId, useRef, type ReactNode } from 'react';
import { X } from 'lucide-react';

export function Modal({title, children, close, wide=false}:{title:string;children:ReactNode;close:()=>void;wide?:boolean}) {
  const ref=useRef<HTMLDivElement>(null);const closeRef=useRef(close);closeRef.current=close;const id=useId();
  useEffect(()=>{
    const previous=document.activeElement as HTMLElement|null;ref.current?.focus();
    const key=(e:KeyboardEvent)=>{
      if(e.key==='Escape'){e.preventDefault();closeRef.current();}
      if(e.key!=='Tab')return;
      const items=ref.current?.querySelectorAll<HTMLElement>('button:not(:disabled),input,a[href],summary,[tabindex="0"]');
      if(!items?.length)return;const first=items[0];const last=items[items.length-1];
      if(e.shiftKey&&(document.activeElement===first||document.activeElement===ref.current)){e.preventDefault();last.focus();}
      else if(!e.shiftKey&&(document.activeElement===last||document.activeElement===ref.current)){e.preventDefault();first.focus();}
    };
    document.addEventListener('keydown',key);return()=>{document.removeEventListener('keydown',key);previous?.focus();};
  },[]);
  return <div className="modal-shade" onMouseDown={e=>{if(e.target===e.currentTarget)close();}}><div className={`dialog ${wide?'wide':''}`} role="dialog" aria-modal="true" aria-labelledby={id} ref={ref} tabIndex={-1}><header><h2 id={id}>{title}</h2><button className="icon-button" aria-label="Закрыть окно" onClick={close}><X size={21}/></button></header>{children}</div></div>;
}
