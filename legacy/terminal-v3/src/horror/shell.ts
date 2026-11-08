import { exists, listDirectory, pathsForCompletion, readFile, resolvePath, tokenize, type Token } from './filesystem';
import { checkProgress, clock, cycle, FLOW_SAMPLE, notify, output, POLICY_CASES, saveFile, schedulerEnabled } from './world';
import { HOSTS, type CodeRunner, type Host, type Routes, type ShellResult, type World } from './types';

export const COMMANDS=['help','man','ls','pwd','cd','cat','grep','head','tail','wc','sort','uniq','echo','printf','cp','mv','rm','mkdir','touch','find','ps','inspect','kill','crontab','edit','nano','node','curl','ssh','hostname','whoami','date','history','clear','export','env','patch','systemctl','wait','check'];
const HELP=`NORTHLINE SHELL / nlsh 3.0
Файлы     ls  cd  pwd  cat  grep  head  tail  wc  sort  uniq
Изменение edit  cp  mv  rm  mkdir  touch  echo  printf
Процессы  ps  inspect PID  kill PID  crontab -l
Код       node FILE.js       node FILE.js --test
Сеть      ssh HOST  curl URL  patch  systemctl
Оболочка  |  >  >>  &&  ;  export  history  clear

man ИМЯ — пример. Tab — дополнение. ↑ ↓ — история.
Чтение и тесты бесплатны. Изменение / активный запрос: 30 сек.
`;
const MAN:Record<string,string>={
  ls:'ls [-la] [PATH]\nls /var/log',cat:'cat FILE [FILE...]\ncat /var/log/auth.log | grep accepted',
  grep:'grep [-n] [-i] [-v] [-E] PATTERN [FILE...]\ncat /var/log/auth.log | grep -n operator',
  edit:'edit FILE\nРедактор: Ctrl+S — сохранить, Ctrl+Enter — выполнить JS.\nРедактор открывает настоящий файл этой машины.',
  node:'node FILE.js\nВыполняет JavaScript, выводит console.log.\nnode /etc/egress/filter.js --test\nПроверяет экспортированную функцию на пакетах. Тесты не меняют сеть.',
  curl:'curl [-X GET|POST] URL [--data BODY | --data @FILE]\nGET /api/health, /api/flows, /api/audit\nPOST /api/firewall — JS-фильтр (edge01)\nPOST /api/routes — JSON-коммутация (core01)\nPOST /api/commit — отправить отзыв (core01)\nПример: curl -X POST /api/firewall --data @/etc/egress/filter.js\nАдреса ограничены внутренними хостами Northline.',
  kill:'ps\ninspect 431\nkill 431\nЗавершает процесс. Планировщик может запустить его снова.',
  crontab:'crontab -l — прочитать /etc/crontab\ncrontab FILE — заменить задания содержимым FILE\ncrontab -r — удалить задания\nМожно изменить файл через edit /etc/crontab.',
  ssh:'ssh ws07 | ssh edge01 | ssh core01\nКаждая машина имеет собственные файлы, процессы и текущую папку.',
  patch:'patch\nОткроет коммутацию CORE-01. Выбери вход, затем выход.\nСохранение меняет /etc/mesh/routes.json.\nДля применения отправь файл в POST /api/routes.',
  systemctl:'systemctl status chorus\nsystemctl stop chorus\nПеред остановкой проверь маршрут replication: отключённый лидер\nс открытым global заставляет внешние узлы выбрать нового.',
  wait:'wait [1..20]\nПродвигает обмен сети без вмешательства. Один цикл: 30 секунд.',
  printf:'printf FORMAT [ARGS...]\nprintf "%s\\n" "hello" > /tmp/message.txt',
};
interface Response {stdout:string;stderr?:string;code?:number;editor?:{host:Host;path:string}}
const ok=(stdout=''):Response=>({stdout,code:0});
const fail=(stderr:string,code=1):Response=>({stdout:'',stderr,code});
const json=(value:unknown)=>JSON.stringify(value,null,2);
function hostFor(value:string):Host|undefined{return (Object.keys(HOSTS) as Host[]).find(host=>host===value.toLowerCase()||HOSTS[host].address===value);}
function writableRemove(world:World,path:string){return world.files[world.host][path]?.writable===true;}
export function complete(world:World,line:string):string[]{
  const last=line.split(/\s/).pop()??'';
  if(!line.includes(' '))return COMMANDS.filter(command=>command.startsWith(last));
  if(/^ssh\s+/.test(line))return(Object.keys(HOSTS) as Host[]).filter(host=>host.startsWith(last));
  return pathsForCompletion(world,last);
}
export async function testPolicy(source:string,runner:CodeRunner){
  const result=await runner(source,POLICY_CASES.map(test=>test.packet));
  if(!result.ok)return{ok:false,stdout:result.stdout,error:result.error??'Runtime error',values:[] as unknown[]};
  const rows=POLICY_CASES.map((test,index)=>({pass:typeof result.values[index]==='boolean'&&result.values[index]===test.allow,test,value:result.values[index]}));
  const count=rows.filter(row=>row.pass).length;
  const text=rows.map(({pass,test,value})=>`${pass?'PASS':'FAIL'} ${test.packet.id.padEnd(8)} ${test.packet.channel.padEnd(8)} -> ${test.packet.destination.padEnd(9)} ${test.packet.signature.padEnd(7)}  ${test.allow?'ALLOW':'DENY'}${pass?'':` / получено ${String(value)}`}`).join('\n');
  return{ok:count===rows.length,stdout:[result.stdout,text,`\n${count}/${rows.length} проверок. ${count===rows.length?'Можно применять.':'Измени правило и запусти снова.'}`].filter(Boolean).join('\n'),values:result.values,error:undefined};
}
function httpResponse(status:number,value:unknown){return `HTTP/1.1 ${status} ${status===200?'OK':status===202?'Accepted':status===400?'Bad Request':status===409?'Conflict':status===403?'Forbidden':'Not Found'}\ncontent-type: application/json\n\n${json(value)}`;}
async function http(world:World,args:string[],runner:CodeRunner):Promise<Response>{
  let method='GET';let endpoint='';let data='';
  for(let i=0;i<args.length;i++){
    const arg=args[i];if(arg==='-X'||arg==='--request'){method=(args[++i]??'').toUpperCase();}
    else if(arg==='-d'||arg==='--data'||arg==='--data-binary'){data=args[++i]??'';if(method==='GET')method='POST';}
    else if(arg==='-H'||arg==='--header')i++;
    else if(['-s','-i','-sS','--silent'].includes(arg))continue;
    else if(arg.startsWith('-'))return fail(`curl: unsupported option ${arg}. man curl`);
    else if(!endpoint)endpoint=arg;else return fail('curl: один URL за запрос.');
  }
  if(!endpoint)return fail('curl: укажи URL. man curl');
  let target=world.host;let path=endpoint;
  if(endpoint.includes('://')){
    let url:URL;try{url=new URL(endpoint);}catch{return fail('curl: invalid URL');}
    const resolved=hostFor(url.hostname.replace(/\.local$/,''));
    if(!resolved)return fail('curl: внешний адрес недоступен. Используй внутренний host.local или /api/...');
    target=resolved;path=url.pathname;
  }
  if(target!==world.host)return fail(`curl: ${target}: подключись к хосту через ssh, чтобы открыть управляющий сокет.`);
  if(data.startsWith('@')){const path=data.slice(1);const content=readFile(world,path);if(content===undefined)return fail(`curl: ${path}: No such file`);data=content;}
  if(method==='GET'){
    if(path==='/api/health')return ok(httpResponse(200,{host:target,time:clock(world),services:{care:world.careUp?'online':'offline',control:world.controlUp?'online':'offline',chorus:world.daemonRunning?'running':'stopped'},firewall:world.firewallReady?'enforced':'permissive',convergence:world.convergence}));
    if(path==='/api/flows')return ok(httpResponse(200,{packets:FLOW_SAMPLE,policy:world.firewallReady?'enforced':'permissive'}));
    if(path==='/api/audit')return ok(httpResponse(200,{time:clock(world),workstation:world.workstationSealed?'sealed':'active',routes:world.routes,daemon:world.daemonRunning,control:world.controlUp,care:world.careUp}));
    return ok(httpResponse(404,{error:'Unknown endpoint',available:['/api/health','/api/flows','/api/audit']}));
  }
  if(method!=='POST')return fail('curl: поддерживаются GET и POST.');
  if(path==='/api/firewall'&&target==='edge01'){
    if(!data)return ok(httpResponse(400,{error:'Передай JavaScript через --data @FILE'}));
    const result=await testPolicy(data,runner);
    if(result.error)return ok(httpResponse(400,{error:result.error,policy:'previous policy retained'}));
    const decisions=result.values;const medical=POLICY_CASES.filter(test=>test.allow&&test.packet.channel==='care').every(test=>decisions[POLICY_CASES.indexOf(test)]===true);
    const control=POLICY_CASES.filter(test=>test.allow&&test.packet.channel==='control').every(test=>decisions[POLICY_CASES.indexOf(test)]===true);
    const leaked=POLICY_CASES.filter((test,index)=>!test.allow&&decisions[index]===true).length;
    world.careUp=medical;world.controlUp=control;world.firewallReady=result.ok;world.stage=result.ok?2:1;
    world.blockedPackets+=POLICY_CASES.filter((test,index)=>!test.allow&&decisions[index]===false).length;
    if(leaked){world.convergence=Math.min(100,world.convergence+leaked*5);notify(world,'EDGE / SENSOR',`${leaked} запрещённых потоков пропущено. ХОР получил ещё один канал.`, 'danger');}
    if(!medical)notify(world,'CARE / LINK','Медицинский канал потерян. Проверь правило и примени исправление.','danger');
    if(!control)notify(world,'OPS / LINK','Управляющий поток заблокирован. SSH к ядру не пройдёт.','warn');
    if(result.ok){world.stage=2;notify(world,'NINA / OPS','Внешний поток остановлен. ssh core01. Внутри всё ещё есть лидер.','success');output(world,'EGRESS / mesh denied. care + control + archive online.\nCORE-01: управляющий канал открыт.','success');output(world,'[незарегистрированный источник]\nты закрыл один голос. мы всё ещё слышим тебя.','entity');}
    cycle(world);return ok(httpResponse(200,{applied:true,checks:`${POLICY_CASES.filter((test,index)=>decisions[index]===test.allow).length}/${POLICY_CASES.length}`,care:medical,control,unwanted_flows:leaked,next:result.ok?'ssh core01':'Исправь правило. GET /api/health покажет последствия.'}));
  }
  if(path==='/api/routes'&&target==='core01'){
    let routes:Routes;try{routes=JSON.parse(data);}catch{return ok(httpResponse(400,{error:'Ожидается JSON. --data @/etc/mesh/routes.json'}));}
    const ports=['global','vault','console','hospital','relay'];
    if(!routes||['replication','operator','care'].some(key=>!ports.includes(routes[key as keyof Routes])))return ok(httpResponse(400,{error:'Нужны replication, operator, care и существующие выходные порты.'}));
    world.routes={replication:routes.replication,operator:routes.operator,care:routes.care};
    world.controlUp=world.firewallReady&&routes.operator==='console';world.careUp=world.firewallReady&&routes.care==='hospital';
    if(routes.replication==='global'){world.convergence=Math.min(100,world.convergence+12);notify(world,'CHOR','спасибо за соединение','entity');}
    else if(routes.replication==='vault'){notify(world,'UNKNOWN / CORE','зачем ты закрыл дверь','entity');output(world,'CORE / external carrier lost.\nCHOR: открой дверь, оператор 071.','entity');}
    cycle(world);return ok(httpResponse(200,{applied:true,replication:routes.replication,control:world.controlUp,care:world.careUp,quarantined:routes.replication==='vault'}));
  }
  if(path==='/api/commit'&&target==='core01'){
    cycle(world);
    if(world.outcome!=='active')return ok(httpResponse(409,{error:'No independent authority remains'}));
    if(!world.workstationSealed)return ok(httpResponse(409,{error:'WS-07 source is active again. Seal the source first.'}));
    if(!world.firewallReady)return ok(httpResponse(409,{error:'Egress policy is not enforced. Repair the firewall on EDGE-01.'}));
    if(world.routes.replication!=='vault')return ok(httpResponse(409,{error:'Replication route is still open. Read /home/operator/ticket.txt'}));
    if(world.daemonRunning||!world.daemonStoppedSafely)return ok(httpResponse(409,{error:'Leader is active. systemctl status chorus'}));
    if(!world.controlUp)return ok(httpResponse(409,{error:'No independent control path. operator -> console required'}));
    if(!world.careUp)return ok(httpResponse(409,{error:'Restore care -> hospital before commit'}));
    world.stage=3;world.outcome=world.careOutage>0?'costly':'contained';
    notify(world,'NINA / VERIFIED','Отзыв принят независимыми узлами. Ты ещё здесь?','success');
    output(world,'CERTIFICATE REVOKED\nCONSENSUS BROKEN\nHUMAN / INDEPENDENT SIGNAL RESTORED','success');
    return ok(httpResponse(202,{revoked:true,global_replication:false,independent_signal:true,care_outage_seconds:world.careOutage*30}));
  }
  return ok(httpResponse(404,{error:'Endpoint unavailable on this host. man curl'}));
}
async function command(world:World,words:string[],stdin:string|undefined,runner:CodeRunner):Promise<Response>{
  const [name,...args]=words;const host=world.host;const files=world.files[host];const first=args[0];
  if(!name)return ok();
  if(name==='help')return ok(HELP);
  if(name==='man')return ok(MAN[first]??`${first??'command'}: ${COMMANDS.includes(first)?'Встроенная команда nlsh. help — список инструментов.':'No manual entry'}`);
  if(name==='hostname')return ok(host);
  if(name==='whoami')return ok('operator / delegated service maintenance');
  if(name==='date')return ok(`2026-10-17 ${clock(world)} NORTHLINE INTERNAL`);
  if(name==='pwd')return ok(world.cwd[host]);
  if(name==='clear'){world.history=[];return ok();}
  if(name==='history')return ok(world.commands.map((cmd,index)=>`${String(index+1).padStart(3)}  ${cmd}`).join('\n'));
  if(name==='env')return ok(Object.entries({HOME:'/home/operator',HOSTNAME:host,...world.env}).map(([key,value])=>`${key}=${value}`).join('\n'));
  if(name==='export'){for(const arg of args){const split=arg.indexOf('=');if(split<1)return fail('export: NAME=value');world.env[arg.slice(0,split)]=arg.slice(split+1);}return ok();}
  if(name==='cd'){const path=resolvePath(world,first??'~');if(!world.directories[host].includes(path))return fail(`cd: ${first}: No such directory`);world.cwd[host]=path;return ok();}
  if(name==='ls'){
    const path=resolvePath(world,args.find(arg=>!arg.startsWith('-'))??'.');const long=args.some(arg=>arg.startsWith('-')&&arg.includes('l'));
    if(files[path])return ok(path.split('/').pop()??path);
    if(!exists(world,path))return fail(`ls: ${path}: No such file or directory`);
    return ok(listDirectory(world,path).map(entry=>long?`${entry.dir?'drwxr-xr-x':'-rw-r--r--'} operator  ${entry.dir?'4096':String(files[(path==='/'?'':path)+'/'+entry.name]?.content.length??0).padStart(4)}  ${entry.name}${entry.dir?'/':''}`:`${entry.name}${entry.dir?'/':''}`).join(long?'\n':'  '));
  }
  if(name==='cat'){
    if(!args.length)return stdin!==undefined?ok(stdin):fail('cat: укажи файл. Для записи: edit FILE');
    const contents:string[]=[];for(const arg of args){const content=readFile(world,arg);if(content===undefined)return fail(`cat: ${arg}: No such file`);contents.push(content);}return ok(contents.join(''));
  }
  if(name==='echo')return ok(args.filter(arg=>arg!=='-n').join(' ')+(args[0]==='-n'?'':'\n'));
  if(name==='printf'){
    let index=1;const format=first??'';const result=format.replace(/%[%sd]/g,match=>match==='%'?'%':match==='%%'?'%':match==='%d'?String(Number(args[index++]??0)):args[index++]??'');return ok(result.replace(/\\n/g,'\n').replace(/\\t/g,'\t'));
  }
  if(['grep','head','tail','wc','sort','uniq'].includes(name)){
    let input=stdin;let targets:string[]=[];let pattern='';let count=10;
    if(name==='grep'){
      const plain=args.filter(arg=>!arg.startsWith('-'));pattern=plain[0]??'';targets=plain.slice(1);if(!plain.length)return fail('grep: укажи шаблон. man grep');
    }else{
      for(let i=0;i<args.length;i++){if(args[i]==='-n'){count=Math.max(0,Math.min(1000,Number(args[++i]??10)));}else if(/^-\d+$/.test(args[i]))count=Math.min(1000,Number(args[i].slice(1)));else if(!args[i].startsWith('-'))targets.push(args[i]);}
    }
    if(targets.length){const values=targets.map(path=>readFile(world,path));if(values.some(value=>value===undefined))return fail(`${name}: No such file`);input=values.join('');}
    if(input===undefined)return fail(`${name}: передай файл или данные через |`);
    const lines=input===''?[]:input.replace(/\n$/,'').split('\n');
    if(name==='grep'){
      let regex:RegExp;try{if(pattern.length>200)return fail('grep: шаблон слишком длинный');regex=new RegExp(pattern,args.some(arg=>arg.startsWith('-')&&arg.includes('i'))?'i':'');}catch{return fail('grep: invalid regular expression');}
      const invert=args.some(arg=>arg.startsWith('-')&&arg.includes('v'));const numbered=args.some(arg=>arg.startsWith('-')&&arg.includes('n'));
      const matched=lines.flatMap((line,index)=>regex.test(line)!==invert?[`${numbered?`${index+1}:`:''}${line}`]:[]);return{stdout:matched.length?matched.join('\n')+'\n':'',code:matched.length?0:1};
    }
    if(name==='head'){const selected=lines.slice(0,count);return ok(selected.join('\n')+(selected.length&&(selected.length<lines.length||input.endsWith('\n'))?'\n':''));}
    if(name==='tail')return ok(count===0?'':lines.slice(-count).join('\n')+(lines.length&&input.endsWith('\n')?'\n':''));
    if(name==='wc')return ok(args.includes('-l')?String((input.match(/\n/g)??[]).length):`${lines.length} ${input.trim().split(/\s+/).filter(Boolean).length} ${new TextEncoder().encode(input).length}`);
    if(name==='sort'){const sorted=lines.sort(args.includes('-n')?(a,b)=>Number(a)-Number(b):(a,b)=>a.localeCompare(b));if(args.includes('-r'))sorted.reverse();return ok(sorted.length?sorted.join('\n')+'\n':'');}
    const unique=lines.filter((line,index)=>index===0||line!==lines[index-1]);return ok(unique.length?unique.join('\n')+'\n':'');
  }
  if(name==='find'){const path=resolvePath(world,first??'.');return ok([...world.directories[host],...Object.keys(files)].filter(entry=>entry===path||entry.startsWith(path==='/'?'/':path+'/')).sort().join('\n'));}
  if(name==='mkdir'){
    const path=resolvePath(world,args.find(arg=>!arg.startsWith('-'))??'');if(path==='/'||exists(world,path))return fail(`mkdir: ${path}: File exists`);
    const parts=path.split('/').filter(Boolean);let parent='';for(let i=0;i<parts.length;i++){parent+='/'+parts[i];if(!world.directories[host].includes(parent)){if(!args.includes('-p')&&i<parts.length-1)return fail(`mkdir: parent does not exist`);world.directories[host].push(parent);}}
    cycle(world);return ok();
  }
  if(name==='touch'){if(!first)return fail('touch: missing file');const path=resolvePath(world,first);const result=saveFile(world,host,path,files[path]?.content??'');return result.ok?ok():fail(result.error!);}
  if(name==='cp'||name==='mv'){
    if(args.length!==2)return fail(`${name}: SOURCE DESTINATION`);const from=resolvePath(world,args[0]);let to=resolvePath(world,args[1]);if(world.directories[host].includes(to))to+=(to==='/'?'':'/')+(from.split('/').pop()??'file');
    if(!files[from])return fail(`${name}: ${from}: No such file`);if(name==='mv'&&!writableRemove(world,from))return fail(`${name}: ${from}: Permission denied`);
    const result=saveFile(world,host,to,files[from].content);if(!result.ok)return fail(result.error!);if(name==='mv')delete files[from];checkProgress(world);return ok();
  }
  if(name==='rm'){
    const paths=args.filter(arg=>!arg.startsWith('-'));if(!paths.length)return fail('rm: missing file');
    for(const arg of paths){const path=resolvePath(world,arg);if(!files[path]){if(args.includes('-f'))continue;return fail(`rm: ${path}: No such file`);}if(!writableRemove(world,path))return fail(`rm: ${path}: Permission denied`);delete files[path];}
    cycle(world);return ok();
  }
  if(name==='edit'||name==='nano'){
    if(!first)return fail('edit: укажи файл');const path=resolvePath(world,first);if(world.directories[host].includes(path))return fail(`edit: ${path}: Is a directory`);
    if(files[path]&&!files[path].writable)return fail(`edit: ${path}: Read-only file. cat ${path}`);
    return{stdout:'',editor:{host,path}};
  }
  if(name==='ps')return ok('  PID USER       COMMAND\n'+world.processes[host].map(proc=>`${String(proc.pid).padStart(5)} ${proc.owner.padEnd(10)} ${proc.command}`).join('\n'));
  if(name==='inspect'){
    const process=world.processes[host].find(proc=>proc.pid===Number(first));if(!process)return fail(`inspect: ${first}: No such process`);
    return ok(json({pid:process.pid,name:process.name,owner:process.owner,command:process.command,parent:process.name==='cache-sync'?'cron':process.name==='chorus'?'consensus':'init',restart:process.name==='cache-sync'&&schedulerEnabled(world)?'managed by /etc/crontab':'none',connections:process.name==='cache-sync'?['edge01 / mesh']:process.name==='chorus'?['replication -> '+world.routes.replication]:[]}));
  }
  if(name==='kill'){
    const pid=Number(args.find(arg=>!arg.startsWith('-')));const process=world.processes[host].find(proc=>proc.pid===pid);
    if(!process)return fail(`kill: ${pid}: No such process`);if(pid===1||process.name==='ops-shell'||process.name==='packetd')return fail(`kill: ${pid}: Operation not permitted`);
    if(process.name==='chorus')return command(world,['systemctl','stop','chorus'],'',runner);
    world.processes[host]=world.processes[host].filter(proc=>proc.pid!==pid);output(world,`SIGTERM / pid ${pid} (${process.name}) остановлен.`,'success');cycle(world);return ok();
  }
  if(name==='crontab'){
    if(first==='-l')return ok(files['/etc/crontab']?.content??'no crontab for this host');
    if(host!=='ws07')return fail('crontab: no maintenance delegation on this host');
    if(first==='-r'){delete files['/etc/crontab'];cycle(world);return ok('crontab removed');}
    if(first){const content=readFile(world,first);if(content===undefined)return fail(`crontab: ${first}: No such file`);const result=saveFile(world,host,'/etc/crontab',content);return result.ok?ok('crontab installed'):fail(result.error!);}
    return fail('crontab: -l | -r | FILE');
  }
  if(name==='node'){
    if(!first)return fail('node: укажи .js файл. man node');const source=readFile(world,first);if(source===undefined)return fail(`node: ${first}: No such file`);
    if(args.includes('--test')){const result=await testPolicy(source,runner);return result.error?fail(result.error):{stdout:result.stdout,code:result.ok?0:1};}
    const result=await runner(source);return result.ok?ok(result.stdout||'Process exited with code 0.'):fail(`${result.stdout?result.stdout+'\n':''}${result.error}`);
  }
  if(name==='curl')return http(world,args,runner);
  if(name==='ssh'){
    const target=hostFor((first??'').replace(/^operator@/,'').replace(/\.local$/,''));if(!target)return fail(`ssh: ${first??''}: Could not resolve hostname`);
    if(target==='edge01'&&!world.workstationSealed)return fail('ssh: edge01: доступ закрыт, пока cache-sync активен на WS-07.');
    if(target==='core01'&&(!world.firewallReady||!world.controlUp))return fail('ssh: core01: управляющий канал недоступен. Проверь фильтр на EDGE-01.');
    const firstVisit=!world.history.some(entry=>entry.host===target&&entry.command!==undefined);world.host=target;
    if(target==='core01'&&firstVisit){notify(world,'UNKNOWN / 071','Твой пропуск действителен. Твоя смена никогда не закончится.','entity');output(world,'CORE-01 / дополнительный сеанс уже открыт.\nВладелец: operator-071\nПоследняя активность: раньше твоего входа.','warn');}
    return ok(`Connected to ${target} [${HOSTS[target].address}]\n${HOSTS[target].role}\nЗаявка: /home/operator/ticket.txt`);
  }
  if(name==='patch'){
    if(host!=='core01')return fail('patch: коммутация доступна на CORE-01.');
    return{stdout:'PATCH PANEL / draft: /etc/mesh/routes.json',editor:{host,path:'@patch'}};
  }
  if(name==='systemctl'){
    if(host!=='core01'||args[1]!=='chorus')return fail('systemctl: service not found. На CORE-01: systemctl status chorus');
    if(first==='status')return ok(`chorus.service / NORTHLINE consensus\nActive: ${world.daemonRunning?'running':'inactive'}\nreplication -> ${world.routes.replication}\ncontrol -> ${world.routes.operator}`);
    if(first!=='stop')return fail('systemctl: status chorus | stop chorus');
    if(!world.daemonRunning)return ok('chorus.service already stopped');
    if(world.routes.replication!=='vault'){
      world.convergence=Math.min(100,world.convergence+28);notify(world,'CHOR','этот компьютер больше не нужен','entity');output(world,'Leader terminated. External peer elected.\nРепликация осталась снаружи. Новый лидер запустил chorus снова.','danger');cycle(world);return ok('chorus.service: restarted by consensus peer.');
    }
    world.daemonRunning=false;world.daemonStoppedSafely=true;world.processes.core01=world.processes.core01.filter(proc=>proc.name!=='chorus');cycle(world);return ok('chorus.service stopped. No replication route.\nОтправь отзыв: curl -X POST /api/commit');
  }
  if(name==='wait'){const count=first===undefined?1:Number(first);if(!Number.isInteger(count)||count<1||count>20)return fail('wait: от 1 до 20 циклов');cycle(world,count);return ok(`${clock(world)} / обмен сети завершён. Слияние: ${world.convergence}%.`);}
  if(name==='check')return ok(json({workstation:world.workstationSealed?'sealed':schedulerEnabled(world)?'scheduler active':'process still running',firewall:world.firewallReady?'enforced':'needs rule',routes:world.routes,daemon:world.daemonRunning?'running':'stopped',convergence:world.convergence}));
  return fail(`${name}: command not found. help — установленные инструменты.`,127);
}

export async function executeShell(before:World,line:string,runner:CodeRunner):Promise<ShellResult>{
  const world=structuredClone(before);const input=line.trim();if(!input||world.outcome!=='active')return{world};
  world.commands.push(input);world.commands=world.commands.slice(-120);
  world.history.push({id:world.nextId++,host:world.host,cwd:world.cwd[world.host],command:input,text:'',tone:'normal'});
  let tokens:Token[];try{tokens=tokenize(input,{HOME:'/home/operator',HOSTNAME:world.host,'?':String(world.lastExit),...world.env});}catch(error){output(world,String((error as Error).message),'danger');world.lastExit=2;return{world};}
  let editor:ShellResult['editor'];let group:Token[]=[];let previousSeparator=';';
  const runGroup=async()=>{
    if(!group.length)return;
    if(previousSeparator==='&&'&&world.lastExit!==0){group=[];return;}
    let current:Token[]=[];let stdin:string|undefined;let response:Response=ok();
    const runPipe=async()=>{
      if(!current.length){response=fail('syntax error near |',2);return;}
      const redirection=current.findIndex(token=>token.operator&&(token.value==='>'||token.value==='>>'));
      let destination:string|undefined;let append=false;
      if(redirection>=0){const next=current[redirection+1];if(!next||next.operator||redirection+2!==current.length){response=fail('syntax error: перенаправление ожидает один путь',2);return;}destination=resolvePath(world,next.value);append=current[redirection].value==='>>';current=current.slice(0,redirection);}
      response=await command(world,current.map(token=>token.value),stdin,runner);
      if(destination&&(response.code??0)===0){const write=saveFile(world,world.host,destination,(append?world.files[world.host][destination]?.content??'':'')+response.stdout);response=write.ok?ok():fail(write.error!);}
      stdin=response.stdout;current=[];
    };
    for(const token of group){if(token.operator&&token.value==='|'){await runPipe();if(response.stderr)break;}else current.push(token);}
    if(current.length&&!response.stderr)await runPipe();
    if(response.stdout)output(world,response.stdout.slice(0,24000));if(response.stderr)output(world,response.stderr,'danger');
    world.lastExit=response.code??0;editor=response.editor??editor;group=[];
  };
  try{
    for(const token of tokens){if(token.operator&&(token.value===';'||token.value==='&&')){await runGroup();previousSeparator=token.value;if(world.outcome!=='active')break;}else group.push(token);}
    if(world.outcome==='active')await runGroup();
  }catch(error){world.lastExit=1;output(world,`shell: ${(error as Error).message}`,'danger');}
  checkProgress(world);return{world,editor};
}
