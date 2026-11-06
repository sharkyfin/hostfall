import { useEffect, useId, useRef, useState } from 'react';
import { ArrowUpRight, Database, Globe2, HardDrive, LockKeyhole, Mail, Monitor, Network, Shield, ShoppingBag, type LucideIcon } from 'lucide-react';
import { latestFinding } from '../game/experience';
import type { AssetId, ObservationState } from '../game/types';

export const FRIENDLY: Record<AssetId,{name:string;description:string;icon:LucideIcon}> = {
  'GW-01':{name:'Удалённый вход',description:'Пропуск в сеть компании',icon:Globe2},
  'WS-07':{name:'Станция подрядчика',description:'Отсюда обслуживают магазин',icon:Monitor},
  'APP-01':{name:'Магазин',description:'Принимает клиентские заказы',icon:ShoppingBag},
  'DB-01':{name:'База заказов',description:'То, что нужно защитить',icon:Database},
  'BACKUP-01':{name:'Ночной архив',description:'Сохраняет резервную копию',icon:HardDrive},
  'MAIL-01':{name:'Почтовый сервер',description:'Ещё один выход наружу',icon:Mail},
  'IDP-01':{name:'Учётные записи',description:'Управляет доступом сотрудников',icon:LockKeyhole},
  'SOC-01':{name:'Твоя консоль',description:'Управляющий канал',icon:Shield},
};
export function NetworkScene({state,selected,select,flash}:{state:ObservationState;selected:AssetId;select:(id:AssetId)=>void;flash?:AssetId}) {
  const [full,setFull]=useState(false);const id=useId().replace(/:/g,'');const training=state.mission==='training';
  const sceneRef=useRef<HTMLElement>(null);const [compact,setCompact]=useState(false);
  useEffect(()=>{const element=sceneRef.current;if(!element)return;const observer=new ResizeObserver(([entry])=>setCompact(entry.contentRect.width<610));observer.observe(element);return()=>observer.disconnect();},[]);
  let nodes: {id:AssetId;x:number;y:number}[]=training
    ? [{id:'WS-07',x:195,y:210},{id:'APP-01',x:480,y:210},{id:'DB-01',x:765,y:210}]
    : [{id:'GW-01',x:110,y:210},{id:'WS-07',x:350,y:210},{id:'APP-01',x:590,y:210},{id:'DB-01',x:830,y:210},{id:'BACKUP-01',x:830,y:405},
      ...(state.mission==='breach'||full||state.events.some(e=>e.assetId==='MAIL-01')?[{id:'MAIL-01' as AssetId,x:590,y:405}]:[]),
      ...(full?[{id:'IDP-01' as AssetId,x:350,y:60},{id:'SOC-01' as AssetId,x:110,y:405}]:[])];
  if(compact)nodes=nodes.map((node,index)=>({...node,x:index%2===0?110:330,y:125+Math.floor(index/2)*155}));
  const canvasHeight=compact?nodes.length>6?720:nodes.length>4?565:410:training?370:500;
  const edges: [AssetId,AssetId][] = [['GW-01','WS-07'],['WS-07','APP-01'],['APP-01','DB-01'],['BACKUP-01','DB-01'],['APP-01','MAIL-01'],['GW-01','IDP-01']];
  const exportVisible=state.mission==='breach'||state.blocked.exchange||state.events.some(e=>e.title==='Исходящая передача данных');
  return <section ref={sceneRef} className={`network-scene ${training?'training-scene':''} ${compact?'compact-scene':''}`} aria-label="Схема компании">
    <header><div><Network size={17}/><span>Твой участок сети</span></div>{!training&&<button className="quiet-button" onClick={()=>setFull(v=>!v)}>{full?'Скрыть служебные узлы':'Ещё узлы'}<ArrowUpRight size={13}/></button>}</header>
    <svg viewBox={`0 0 ${compact?440:960} ${canvasHeight}`} className="network-canvas" aria-label="Выбери узел, чтобы проверить или защитить его">
      <defs><pattern id={`${id}-dots`} width="28" height="28" patternUnits="userSpaceOnUse"><circle cx="2" cy="2" r=".8" fill="#526366" opacity=".45"/></pattern></defs>
      <rect width={compact?440:960} height={canvasHeight} fill={`url(#${id}-dots)`}/>
      <text className="zone-label" x="22" y="24">{training?'СТАНЦИЯ → ПРИЛОЖЕНИЕ → ДАННЫЕ':'NORTHLINE / РАБОЧАЯ СЕТЬ'}</text>
      {edges.map(([from,to])=>{
        const a=nodes.find(n=>n.id===from);const b=nodes.find(n=>n.id===to);if(!a||!b)return null;
        const broken=state.assets[from].isolated||state.assets[to].isolated||(from==='APP-01'&&to==='MAIL-01'&&state.blocked.mail);
        const measured=state.events.some(e=>e.detail.includes(`${from} → ${to}`));
        return <g key={`${from}-${to}`} className={`scene-edge ${broken?'cut':measured?'seen':''}`}><path d={`M${a.x} ${a.y} L${b.x} ${b.y}`}/>{broken&&<text x={(a.x+b.x)/2} y={(a.y+b.y)/2-12} textAnchor="middle">ПУТЬ ЗАКРЫТ</text>}</g>;
      })}
      {!training&&exportVisible&&<g className={`egress-route ${state.blocked.exchange?'cut':''}`} transform={compact?'translate(-235,-28)':undefined}>{!compact&&<path d="M590 154V97Q590 80 568 80H132Q110 80 110 102V154"/>}<text x="455" y="69" textAnchor="middle">{state.blocked.exchange?'ВЫГРУЗКА ЗАБЛОКИРОВАНА':'КАНАЛ ВНЕШНЕЙ ВЫГРУЗКИ'}</text>{!compact&&<g role="button" tabIndex={0} aria-label="Канал выгрузки" onClick={()=>select('APP-01')} onKeyDown={e=>{if(e.key==='Enter')select('APP-01');}}><rect x="385" y="91" width="140" height="30" rx="15"/><text x="455" y="111" textAnchor="middle">{state.blocked.exchange?'Перекрыт':'Проверить выход'}</text></g>}</g>}
      {nodes.map(node=>{
        const meta=FRIENDLY[node.id];const Icon=meta.icon;const host=state.assets[node.id];const finding=latestFinding(state,node.id);
        const clue=finding?.finding==='local-task'||finding?.finding==='rogue-process';
        const symptom=state.events.some(e=>e.assetId===node.id&&e.severity==='critical');
        const cleaned=state.events.some(e=>e.assetId===node.id&&['Подозрительная задача удалена','Доверенный образ восстановлен'].includes(e.title));
        const active=selected===node.id||(training&&selected==='GW-01'&&node.id==='WS-07');
        const status=host.isolated?'Отключён от сети':cleaned&&!clue?'Задача удалена':clue?'Есть найденный след':symptom?'Есть тревога':node.id==='APP-01'?'Заказы работают':node.id==='DB-01'?'Данные компании':'В сети';
        return <g key={node.id} className={`scene-node ${active?'selected':''} ${host.isolated?'isolated':''} ${clue?'clue':''} ${flash===node.id?'flash':''}`} transform={`translate(${node.x-94} ${node.y-57})`} role="button" tabIndex={0} aria-label={`${meta.name} ${node.id}`} onClick={()=>select(node.id)} onKeyDown={e=>{if(e.key==='Enter'||e.key===' '){e.preventDefault();select(node.id);}}}>
          {active&&<rect className="scene-node-outline" x="-6" y="-6" width="200" height="126" rx="15"/>}<rect className="scene-node-body" width="188" height="114" rx="10"/>
          <rect className="scene-icon-box" x="15" y="15" width="34" height="34" rx="8"/><Icon x={22} y={22} width={20} height={20} strokeWidth={1.6} className="scene-icon"/>
          <text className="scene-code" x="61" y="30">{node.id}</text><text className="scene-node-label" x="15" y="73">{meta.name}</text>
          <circle className="scene-state-dot" cx="19" cy="95" r="3"/><text className="scene-node-state" x="28" y="99">{status}</text>
        </g>;
      })}
    </svg>
    <footer><span><i/>Линия — доступный путь</span><span><i className="amber"/>След — повод проверить</span></footer>
  </section>;
}
