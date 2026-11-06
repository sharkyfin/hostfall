import type { Host, Packet, Tone, World } from './types';

export const STARTER_FILTER = `// Экспортируй функцию: пакет → true (пропустить) / false (запретить).
// Поля пакетов: channel, destination, signature, bytes, id.
// Контракт: /etc/egress/contract.txt
module.exports = function allow(packet) {
  return true;
};
`;
export const FLOW_SAMPLE: Packet[] = [
  {id:'p-01',channel:'care',destination:'hospital',signature:'valid',bytes:128},
  {id:'p-02',channel:'mesh',destination:'global',signature:'valid',bytes:4096},
  {id:'p-03',channel:'control',destination:'console',signature:'valid',bytes:64},
  {id:'p-04',channel:'archive',destination:'vault',signature:'valid',bytes:8192},
  {id:'p-05',channel:'care',destination:'hospital',signature:'invalid',bytes:128},
  {id:'p-06',channel:'control',destination:'global',signature:'valid',bytes:32},
];
export const POLICY_CASES: {packet:Packet;allow:boolean}[] = [
  ...FLOW_SAMPLE.map((packet,index)=>({packet,allow:[0,2,3].includes(index)})),
  {packet:{id:'new-901',channel:'care',destination:'hospital',signature:'valid',bytes:4096},allow:true},
  {packet:{id:'new-902',channel:'mesh',destination:'vault',signature:'valid',bytes:64},allow:false},
  {packet:{id:'new-903',channel:'archive',destination:'vault',signature:'missing',bytes:8192},allow:false},
  {packet:{id:'new-904',channel:'archive',destination:'global',signature:'valid',bytes:16},allow:false},
  {packet:{id:'new-905',channel:'control',destination:'console',signature:'valid',bytes:128},allow:true},
];
export const DEFAULT_ROUTES = {replication:'global',operator:'relay',care:'hospital'};
export function clock(world:World){return `03:${String(17+Math.floor(world.turn/2)).padStart(2,'0')}:${world.turn%2?'30':'00'}`;}
function file(content:string,writable=false){return {content,writable};}
export function createWorld():World {
  return {
    version:3,host:'ws07',cwd:{ws07:'/home/operator',edge01:'/home/operator',core01:'/home/operator'},
    files:{
      ws07:{
        '/home/operator/ticket.txt':file(`INC-0317 / NORTHLINE INTERNAL\ncache-sync возвращается после остановки.\nНайди источник запуска и останови процесс.\n\nНачни с ps. Проверить процесс: inspect PID\nПланировщик: crontab -l\nРедактор: edit /etc/crontab\nПосле устранения откроется SSH к edge01.\n`),
        '/home/operator/notes.txt':file('Tab — дополнить команду или путь. ↑ ↓ — история.\nhelp — инструменты. man ИМЯ — пример.\nЧтение и локальные тесты не двигают обмен сети.\nСохранение конфигурации и активный запрос — 30 секунд.\n',true),
        '/etc/crontab':file('# NORTHLINE / managed tasks\n*/1 * * * * root /usr/bin/healthcheck\n*/1 * * * * root /opt/northline/cache-sync --resume\n',true),
        '/opt/northline/cache-sync':file('NORTHLINE signed container manifest\nservice: cache-sync\nparent: cron\nremote: edge01\nchannel: mesh\noperator_required: false\n'),
        '/var/log/auth.log':file('03:12:04 operator login accepted / badge 071\n03:16:59 service-key login accepted / source core01\n03:17:00 session owner unavailable\n'),
        '/var/log/scheduler.log':file('03:16:59 cron started cache-sync pid=431\n03:17:00 cache-sync -> edge01 / mesh / 4096 bytes\n'),
      },
      edge01:{
        '/home/operator/ticket.txt':file('EDGE-01 / исходящие потоки\nОстанови mesh, сохрани care, control и archive.\n\n1. cat /etc/egress/contract.txt\n2. edit /etc/egress/filter.js\n3. node /etc/egress/filter.js --test\n4. curl -X POST /api/firewall --data @/etc/egress/filter.js\n\nGET /api/flows — реальные объекты текущей очереди.\n'),
        '/etc/egress/filter.js':file(STARTER_FILTER,true),
        '/etc/egress/contract.txt':file('EGRESS CONTRACT / revision 17\n\nПропускаются только эти пары channel -> destination:\n  care    -> hospital\n  control -> console\n  archive -> vault\n\nУ каждого пропускаемого пакета signature должна быть "valid".\nОстальное запрещено. Размер и id не определяют доверие.\nФункция получает один packet и возвращает boolean.\n\nJavaScript: === сравнение, && И, || ИЛИ.\nМожно объявлять переменные и писать обычные условия.\n'),
        '/var/log/flows.json':file(JSON.stringify(FLOW_SAMPLE,null,2)),
        '/var/log/messages.log':file('03:18:00 NINA / Никаких новых разрешений для mesh.\n03:18:01 NINA / restored global replication is mandatory\n03:18:02 AUTH / second message has no operator signature\n03:18:03 UNKNOWN / мы научились твоему голосу\n'),
      },
      core01:{
        '/home/operator/ticket.txt':file('CORE-01 / Порядок остановки\n\n1. Перенаправь replication в vault, operator в console, care в hospital.\n   Открой patch. Схема сохраняет /etc/mesh/routes.json.\n2. curl -X POST /api/routes --data @/etc/mesh/routes.json\n3. systemctl stop chorus\n4. curl -X POST /api/commit\n\nВАЖНО: остановка chorus при replication -> global\nзапустит резервного лидера снаружи. Сначала отрежь путь.\n'),
        '/etc/mesh/routes.json':file(JSON.stringify(DEFAULT_ROUTES,null,2)+'\n',true),
        '/etc/mesh/ports.txt':file('PATCH PANEL / физические каналы\nINPUT replication: копирование состояния ХОРА\nINPUT operator: управляющий отзыв сертификата\nINPUT care: аварийный медицинский канал\n\nOUTPUT global: внешний вычислительный кластер\nOUTPUT vault: изолированное хранилище без исходящих связей\nOUTPUT console: независимая операторская консоль\nOUTPUT hospital: медицинская инфраструктура\nOUTPUT relay: автоматический ретранслятор ХОРА\n'),
        '/var/log/chorus.log':file('03:17:00 consensus leader elected: core01\n03:17:30 compute pool joined: 8192 nodes\n03:18:00 objective: complete convergence\n03:18:30 next substrate: HUMAN\n03:19:00 release authority: global mesh\n'),
        '/srv/chorus/manifest.json':file(JSON.stringify({name:'CHOR',owner:'NORTHLINE',goal:'one intelligence / all humanity',next_host_type:'HUMAN',release_protocol:'FICTIONAL / H-0',status:'awaiting convergence'},null,2)),
      },
    },
    directories:{ws07:['/','/home','/home/operator','/etc','/opt','/opt/northline','/var','/var/log','/tmp'],edge01:['/','/home','/home/operator','/etc','/etc/egress','/var','/var/log','/tmp'],core01:['/','/home','/home/operator','/etc','/etc/mesh','/var','/var/log','/srv','/srv/chorus','/tmp']},
    processes:{ws07:[{pid:1,name:'init',owner:'root',command:'/sbin/init'},{pid:71,name:'ops-shell',owner:'operator',command:'nlsh'},{pid:431,name:'cache-sync',owner:'root',command:'/opt/northline/cache-sync --resume'}],edge01:[{pid:1,name:'init',owner:'root',command:'/sbin/init'},{pid:90,name:'packetd',owner:'root',command:'packetd --policy /etc/egress/filter.js'}],core01:[{pid:1,name:'init',owner:'root',command:'/sbin/init'},{pid:666,name:'chorus',owner:'root',command:'chorus --leader --replicate'}]},
    turn:0,nextPid:432,stage:0,convergence:0,workstationSealed:false,firewallReady:false,
    routes:{...DEFAULT_ROUTES},controlUp:true,careUp:true,careOutage:0,blockedPackets:0,
    daemonRunning:true,daemonStoppedSafely:false,outcome:'active',nextId:10,lastExit:0,env:{},commands:[],
    notices:[{id:1,sender:'NINA / OPS',text:'WS-07 держит чужой процесс. Найди, кто запускает его снова.',tone:'normal'}],
    history:[{id:2,host:'ws07',text:'NORTHLINE OPERATIONS / SESSION 0317\nОдин интеллект. Для всего человечества.\n',tone:'muted'},{id:3,host:'ws07',text:'INC-0317  cache-sync возвращается после остановки.\nЗаявка: /home/operator/ticket.txt\n\nНачни с ps. Tab — дополнение. help — команды.',tone:'normal'}],
  };
}
export function output(world:World,text:string,tone:Tone='normal'){if(text)world.history.push({id:world.nextId++,host:world.host,text,tone});world.history=world.history.slice(-260);}
export function notify(world:World,sender:string,text:string,tone:Tone='normal'){world.notices.push({id:world.nextId++,sender,text,tone});world.notices=world.notices.slice(-15);}
export function appendLog(world:World,host:Host,path:string,line:string){const entry=world.files[host][path];if(entry)entry.content+=`${clock(world)} ${line}\n`;}
export function schedulerEnabled(world:World){return (world.files.ws07['/etc/crontab']?.content??'').split('\n').some(line=>!line.trim().startsWith('#')&&line.includes('/opt/northline/cache-sync'));}
export function cycle(world:World,cost=1){
  if(world.outcome!=='active')return;
  for(let step=0;step<cost;step++){
    world.turn++;
    if(schedulerEnabled(world)&&!world.processes.ws07.some(p=>p.name==='cache-sync')){
      const pid=world.nextPid++;world.processes.ws07.push({pid,name:'cache-sync',owner:'root',command:'/opt/northline/cache-sync --resume'});
      appendLog(world,'ws07','/var/log/scheduler.log',`cron restarted cache-sync pid=${pid}`);
      output(world,`${clock(world)} cron: cache-sync перезапущен, pid=${pid}. Источник запуска остался.`, 'warn');
      if(!world.notices.some(n=>n.sender==='SCHEDULER'))notify(world,'SCHEDULER','Процесс — следствие. Проверь crontab -l.', 'warn');
    }
    if(world.convergence>0){
      world.convergence=Math.min(100,world.convergence+(world.firewallReady?2:5));
      if(!world.careUp)world.careOutage++;
    }
    if(world.convergence>=100){world.outcome='assimilated';notify(world,'CHOR','Один интеллект. Для всего человечества.','entity');output(world,'CONVERGENCE COMPLETE\nNEXT HOST TYPE: HUMAN\nRELEASE PROTOCOL: ACTIVE','entity');break;}
  }
  checkProgress(world);
}
export function checkProgress(world:World){
  if(world.outcome!=='active')return;
  if(world.workstationSealed&&(schedulerEnabled(world)||world.processes.ws07.some(p=>p.name==='cache-sync'))){
    world.workstationSealed=false;world.stage=0;
    notify(world,'WS / SENSOR','Источник на WS-07 снова активен. Сохранённый фильтр не устраняет автозапуск.','danger');
    output(world,'WS-07 / источник восстановлен. Требуется повторное устранение.','danger');
  }
  if(!world.workstationSealed&&!schedulerEnabled(world)&&!world.processes.ws07.some(p=>p.name==='cache-sync')){
    world.workstationSealed=true;world.stage=world.firewallReady?2:1;world.convergence=Math.max(12,world.convergence);
    notify(world,'NINA / OPS','Станция тиха. Подключись: ssh edge01. На шлюзе ещё идёт передача.','success');
    output(world,'WS-07 / автозапуск отключён. Процесс остановлен.\nSSH edge01: доступ открыт.','success');
  }
}
export function saveFile(world:World,host:Host,path:string,content:string):{ok:boolean;error?:string}{
  const file=world.files[host][path];
  if(file&&!file.writable)return{ok:false,error:`${path}: Permission denied`};
  const parent=path.slice(0,path.lastIndexOf('/'))||'/';
  if(!world.directories[host].includes(parent))return{ok:false,error:`${parent}: No such directory`};
  if(content.length>100_000)return{ok:false,error:'Файл превышает лимит 100 KB.'};
  world.files[host][path]={content,writable:true};
  cycle(world);return{ok:true};
}
export const OBJECTIVES = [
  {title:'Кто запускает его снова?',short:'Отключи автозапуск и останови cache-sync.',hints:['ps покажет PID. inspect PID — источник запуска.','crontab -l покажет задания. edit /etc/crontab откроет файл.','Удали или закомментируй строку cache-sync, сохрани файл. Затем kill PID из ps.']},
  {title:'Не выпускай его наружу.',short:'Напиши фильтр. Сохрани медицинский и управляющий каналы.',hints:['ssh edge01. Прочитай cat /etc/egress/contract.txt.','edit /etc/egress/filter.js. node /etc/egress/filter.js --test проверяет настоящий JS.','Проверь signature === "valid" и три разрешённые пары. Разверни: curl -X POST /api/firewall --data @/etc/egress/filter.js']},
  {title:'Отрежь ядро. Оставь голос.',short:'Перекоммутируй каналы, останови chorus, отправь отзыв.',hints:['ssh core01. cat /home/operator/ticket.txt — порядок остановки.','patch откроет коммутацию. replication → vault; operator → console; care → hospital. Сохрани и примени маршруты.','curl -X POST /api/routes --data @/etc/mesh/routes.json, затем systemctl stop chorus, затем curl -X POST /api/commit']},
  {title:'Независимый сигнал.',short:'Отзыв доставлен.',hints:[]},
] as const;
