import type { Host, World } from './types';
export function resolvePath(world:World,path:string,host=world.host){
  const home='/home/operator';let full=path==='~'?home:path.startsWith('~/')?home+path.slice(1):path.startsWith('/')?path:`${world.cwd[host]}/${path}`;
  const parts:string[]=[];for(const bit of full.split('/')){if(!bit||bit==='.')continue;if(bit==='..')parts.pop();else parts.push(bit);}return '/'+parts.join('/');
}
export function exists(world:World,path:string,host=world.host){return Boolean(world.files[host][path])||world.directories[host].includes(path);}
export function listDirectory(world:World,path:string,host=world.host){
  const prefix=path==='/'?'/':path+'/';const entries=new Map<string,boolean>();
  for(const dir of world.directories[host]){if(dir.startsWith(prefix)&&dir!==path){const name=dir.slice(prefix.length).split('/')[0];if(name)entries.set(name,true);}}
  for(const filename of Object.keys(world.files[host])){if(filename.startsWith(prefix)){const rest=filename.slice(prefix.length);const name=rest.split('/')[0];if(name)entries.set(name,rest.includes('/'));}}
  return [...entries].sort(([a],[b])=>a.localeCompare(b)).map(([name,dir])=>({name,dir}));
}
export function pathsForCompletion(world:World,prefix:string){
  const slash=prefix.lastIndexOf('/');const parent=slash>=0?resolvePath(world,prefix.slice(0,slash+1)):world.cwd[world.host];
  const partial=prefix.slice(slash+1);if(!world.directories[world.host].includes(parent))return[];
  return listDirectory(world,parent).filter(e=>e.name.startsWith(partial)).map(e=>(slash>=0?prefix.slice(0,slash+1):'')+e.name+(e.dir?'/':''));
}
export function readFile(world:World,path:string,host:Host=world.host){return world.files[host][resolvePath(world,path,host)]?.content;}

export interface Token { value:string;operator:boolean }
export function tokenize(line:string,env:Record<string,string>={}):Token[]{
  const tokens:Token[]=[];let buffer='';let quoted=false;let quote='';let started=false;
  const flush=()=>{if(started){tokens.push({value:buffer,operator:false});buffer='';started=false;}};
  for(let i=0;i<line.length;i++){
    const ch=line[i];
    if(quote){if(ch===quote){quote='';quoted=false;}else if(ch==='\\'&&quote==='"'&&i+1<line.length){const next=line[++i];buffer+=next==='n'?'\n':next==='t'?'\t':next;}else if(ch==='$'&&quote==='"'){const match=line.slice(i+1).match(/^(\?|[A-Za-z_][\w]*)/);if(match){buffer+=env[match[0]]??'';i+=match[0].length;}else buffer+=ch;}else buffer+=ch;continue;}
    if(ch==='"'||ch==="'"){quote=ch;quoted=true;started=true;continue;}
    if(ch==='\\'&&i+1<line.length){buffer+=line[++i];started=true;continue;}
    if(ch==='#'&&!started){while(i<line.length&&line[i]!=='\n')i++;if(i<line.length){flush();tokens.push({value:';',operator:true});}continue;}
    if(ch==='$'){const match=line.slice(i+1).match(/^(\?|[A-Za-z_][\w]*)/);if(match){buffer+=env[match[0]]??'';i+=match[0].length;started=true;continue;}}
    if(/\s/.test(ch)){flush();if(ch==='\n')tokens.push({value:';',operator:true});continue;}
    if(['|','>',';','&'].includes(ch)){flush();let op=ch;if((ch==='>'&&line[i+1]==='>')||(ch==='&'&&line[i+1]==='&'))op+=line[++i];if(op==='&')throw Error('Фоновый & не поддерживается. Используй одну команду.');tokens.push({value:op,operator:true});continue;}
    buffer+=ch;started=true;
  }
  if(quote||quoted)throw Error('Незакрытая кавычка.');flush();return tokens;
}
