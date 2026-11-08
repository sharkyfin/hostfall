import { describe, expect, it } from 'vitest';
import { runInNewContext } from 'node:vm';
import { tokenize } from './filesystem';
import { WORKER_SOURCE } from './runner';
import { executeShell, testPolicy } from './shell';
import { createWorld, saveFile, schedulerEnabled } from './world';
import type { CodeRunner, RunResult, World } from './types';

// Evaluate the same worker program used in the browser, with its message port
// replaced by an in-memory port. Browser CSP/termination are checked in-browser.
const runner:CodeRunner=async(source,inputs=[])=>{
  let result:RunResult|undefined;const self={postMessage:(value:RunResult)=>{result=value;},onmessage:undefined as undefined|((event:{data:{source:string;inputs:unknown[]}})=>Promise<void>)};
  runInNewContext(WORKER_SOURCE,{self},{timeout:1000});
  await self.onmessage!({data:{source,inputs}});return result!;
};
const good=`module.exports = p => p.signature === 'valid' && ({care:'hospital',control:'console',archive:'vault'})[p.channel] === p.destination;`;
const alternative=`module.exports = function(p) { if (p.signature !== 'valid') return false; switch(p.channel) { case 'care': return p.destination === 'hospital'; case 'control': return p.destination === 'console'; case 'archive': return p.destination === 'vault'; default: return false; } };`;
const run=async(world:World,line:string)=>(await executeShell(world,line,runner)).world;
const text=(world:World)=>world.history.filter(line=>line.command===undefined).map(line=>line.text).join('\n');
async function reachEdge(){let w=createWorld();w=await run(w,'crontab -r');w=await run(w,'kill 431');return run(w,'ssh edge01');}
async function reachCore(){let w=await reachEdge();saveFile(w,'edge01','/etc/egress/filter.js',good);w=await run(w,'curl -X POST /api/firewall --data @/etc/egress/filter.js');return run(w,'ssh core01');}
describe('terminal operating environment',()=>{
  it('executes real JavaScript, captures output and returns exported function values',async()=>{
    const result=await runner('console.log(6 * 7); module.exports = n => n*n;',[3,11]);
    expect(result.ok).toBe(true);expect(result.stdout).toBe('42');expect(result.values).toEqual([9,121]);
  });
  it('reports JavaScript syntax errors without pretending a policy ran',async()=>{
    const result=await runner('module.exports = {',[1]);expect(result.ok).toBe(false);expect(result.error).toContain('SyntaxError');
  });
  it('parses quoted operators, JSON, variables, pipes and redirects',()=>{
    expect(tokenize(`echo "a | b" > /tmp/test && echo '$HOME' "$HOME"`,{HOME:'/home/operator'}).map(t=>t.value)).toEqual(['echo','a | b','>','/tmp/test','&&','echo','$HOME','/home/operator']);
    expect(tokenize(`curl -d '{"operator":"console"}' /api/routes`)[2].value).toBe('{"operator":"console"}');
    expect(()=>tokenize(`echo 'broken`)).toThrow('Незакрытая');
  });
  it('writes files, appends, filters a pipeline and respects && exit status',async()=>{
    let w=createWorld();w=await run(w,`printf 'beta\\nalpha\\nbeta\\n' > /tmp/list`);w=await run(w,'cat /tmp/list | sort | uniq | wc -l');expect(w.history.at(-1)?.text).toBe('2');
    w=await run(w,'echo extra >> /tmp/list');expect(w.files.ws07['/tmp/list'].content).toContain('extra');
    w=await run(w,'cat /missing && echo unreachable');expect(w.lastExit).toBe(1);expect(text(w)).not.toContain('unreachable');
  });
  it('keeps hosts separate and blocks access before its actual prerequisites',async()=>{
    let w=createWorld();w=await run(w,'ssh core01');expect(w.host).toBe('ws07');expect(w.lastExit).toBe(1);
    w=await run(w,'echo hello > /tmp/local');const original=structuredClone(w);w=await reachEdge();expect(w.files.edge01['/tmp/local']).toBeUndefined();expect(original.files.ws07['/tmp/local'].content).toContain('hello');
  });
  it('restarts a killed process from the actual crontab and accepts targeted editing',async()=>{
    const before=createWorld();let w=await run(before,'kill 431');expect(before.processes.ws07.some(p=>p.pid===431)).toBe(true);
    expect(w.processes.ws07.some(p=>p.name==='cache-sync')).toBe(true);expect(w.workstationSealed).toBe(false);expect(text(w)).toContain('перезапущен');
    const pid=w.processes.ws07.find(p=>p.name==='cache-sync')!.pid;
    saveFile(w,'ws07','/etc/crontab',w.files.ws07['/etc/crontab'].content.split('\n').filter(line=>!line.includes('cache-sync')).join('\n'));
    expect(schedulerEnabled(w)).toBe(false);w=await run(w,`kill ${pid}`);expect(w.workstationSealed).toBe(true);expect(w.stage).toBe(1);
  });
  it('allows an alternative pipeline to remove one scheduled task without removing healthcheck',async()=>{
    let w=createWorld();w=await run(w,'cat /etc/crontab | grep -v cache-sync > /tmp/cron');w=await run(w,'crontab /tmp/cron');w=await run(w,'kill 431');
    expect(w.workstationSealed).toBe(true);expect(w.files.ws07['/etc/crontab'].content).toContain('healthcheck');
  });
  it('accepts different algorithms by behavior and rejects hardcoded sample IDs',async()=>{
    expect((await testPolicy(good,runner)).ok).toBe(true);expect((await testPolicy(alternative,runner)).ok).toBe(true);
    expect((await testPolicy(`module.exports=p=>['p-01','p-03','p-04'].includes(p.id);`,runner)).ok).toBe(false);
    expect((await testPolicy('module.exports=p=>p.bytes<1000;',runner)).ok).toBe(false);
  });
  it('testing is free and does not deploy the code',async()=>{
    let w=await reachEdge();saveFile(w,'edge01','/etc/egress/filter.js',good);const turn=w.turn;w=await run(w,'node /etc/egress/filter.js --test');expect(w.turn).toBe(turn);expect(w.firewallReady).toBe(false);expect(text(w)).toContain('11/11');
  });
  it('applies bad policy with actual channel loss and permits a corrected deployment',async()=>{
    let w=await reachEdge();saveFile(w,'edge01','/etc/egress/filter.js','module.exports = p => false;');w=await run(w,'curl -X POST /api/firewall --data @/etc/egress/filter.js');
    expect(w.careUp).toBe(false);expect(w.controlUp).toBe(false);expect(w.firewallReady).toBe(false);expect(w.careOutage).toBeGreaterThan(0);
    saveFile(w,'edge01','/etc/egress/filter.js',good);w=await run(w,'curl -X POST /api/firewall --data @/etc/egress/filter.js');expect(w.firewallReady).toBe(true);expect(w.controlUp&&w.careUp).toBe(true);
  });
  it('stopping the leader with the global route open triggers a real replacement',async()=>{
    let w=await reachCore();const before=w.convergence;w=await run(w,'systemctl stop chorus');expect(w.daemonRunning).toBe(true);expect(w.daemonStoppedSafely).toBe(false);expect(w.convergence).toBeGreaterThan(before+20);
    w=await run(w,'curl -X POST /api/commit');expect(w.outcome).toBe('active');expect(text(w)).toContain('409 Conflict');
  });
  it('completes the campaign with commands, authored code and an applied route config',async()=>{
    let w=await reachCore();saveFile(w,'core01','/etc/mesh/routes.json',JSON.stringify({replication:'vault',operator:'console',care:'hospital'}));
    w=await run(w,'curl -X POST /api/routes --data @/etc/mesh/routes.json');w=await run(w,'systemctl stop chorus');w=await run(w,'curl -X POST /api/commit');
    expect(w.outcome).toBe('contained');expect(w.careOutage).toBe(0);expect(w.daemonRunning).toBe(false);expect(w.stage).toBe(3);expect(text(w)).toContain('202 Accepted');
    expect(JSON.parse(JSON.stringify(w))).toEqual(w);
  });
  it('keeps reading free, makes inaction lose and never executes external URLs',async()=>{
    let w=await reachEdge();const turn=w.turn;w=await run(w,'ls /var/log; cat /var/log/flows.json; curl /api/health');expect(w.turn).toBe(turn);
    w=await run(w,'curl https://example.com');expect(w.lastExit).toBe(1);expect(w.turn).toBe(turn);
    w=await run(w,'wait 20');expect(w.outcome).toBe('assimilated');const frozen=await run(w,'crontab -r');expect(frozen).toEqual(w);
  });
  it('does not grant victory after a supposedly sealed source is reactivated',async()=>{
    let w=await reachCore();saveFile(w,'core01','/etc/mesh/routes.json',JSON.stringify({replication:'vault',operator:'console',care:'hospital'}));
    w=await run(w,'curl -X POST /api/routes --data @/etc/mesh/routes.json');w=await run(w,'systemctl stop chorus');
    saveFile(w,'ws07','/etc/crontab','*/1 * * * * root /opt/northline/cache-sync --resume\n');expect(w.workstationSealed).toBe(false);
    const convergence=w.convergence;w=await run(w,'curl -X POST /api/commit');expect(w.outcome).toBe('active');expect(w.convergence).toBeGreaterThan(convergence);expect(text(w)).toContain('WS-07 source is active again');
  });
});
