import { describe, expect, it } from 'vitest';
import { advance, createWorld, execute, observe, replay } from './engine';
import { advanceLesson, coachMessage, LESSONS, observedTransfer } from './experience';
import type { Command, World } from './types';

const run=(world:World, command:Command)=>execute(world,command).world;
describe('learnable decisions and two different incidents',()=>{
  it('completes all eight tutorial steps through actual accepted actions',()=>{
    let world=createWorld('NIGHT-0317','training');let step=0;
    step=advanceLesson(step,{type:'open',assetId:'GW-01'});
    const actions:Command[]=[{type:'contact',target:'contractor'},{type:'revoke',accountId:world.accountId},{type:'inspect',assetId:'WS-07'},{type:'isolate',assetId:'WS-07',isolated:true},{type:'purge',assetId:'WS-07'},{type:'isolate',assetId:'WS-07',isolated:false},{type:'verify'}];
    for(const command of actions){const result=execute(world,command);expect(result.result.accepted).toBe(true);world=result.world;step=advanceLesson(step,command,result.result.completed);}
    expect(step).toBe(LESSONS.length);expect(world.tick).toBe(10);expect(world.outcome).toBe('clean');expect(world.truth.attacker.leaked).toBe(0);
    expect(replay(world.seed,world.commands,world.tick,'training')).toEqual(world);
  });
  it('does not advance a lesson on an unrelated click or rejected command',()=>{
    expect(advanceLesson(0,{type:'open',assetId:'APP-01'})).toBe(0);
    expect(advanceLesson(2,{type:'inspect',assetId:'WS-07'})).toBe(2);
    expect(advanceLesson(3,{type:'inspect',assetId:'WS-07'},false)).toBe(3);
  });
  it('requires a measured process before allowing targeted removal',()=>{
    let world=createWorld();const rejected=execute(world,{type:'purge',assetId:'WS-07'});
    expect(rejected.result.accepted).toBe(false);expect(rejected.world.tick).toBe(0);
    world=run(world,{type:'inspect',assetId:'WS-07'});
    expect(observe(world).events.some(e=>e.finding==='local-task')).toBe(true);
    world=run(world,{type:'purge',assetId:'WS-07'});
    expect(world.truth.attacker.footholds).not.toContain('WS-07');
    expect(execute(world,{type:'purge',assetId:'WS-07'}).result.accepted).toBe(false);
    world=advance(world,4);expect(world.truth.attacker.footholds).toContain('WS-07');
  });
  it('shows a real remaining connection after revocation without announcing hidden infection',()=>{
    let world=createWorld();world=run(world,{type:'revoke',accountId:world.accountId});
    expect(observe(world).events.some(e=>e.title==='После отзыва остался сетевой поток')).toBe(true);
    expect(observe(world).events.filter(e=>e.title==='После отзыва остался сетевой поток')[0].detail).not.toContain('foothold');
  });
  it('rewards blocking first, inspecting, then stopping the process on the last mile',()=>{
    let world=createWorld('HOT-1','breach');
    world=run(world,{type:'block',channel:'exchange',blocked:true});
    expect(coachMessage(observe(world)).title).toBe('Первый выход закрыт. Найди процесс.');
    world=run(world,{type:'inspect',assetId:'APP-01'});
    expect(world.observations.some(e=>e.title==='Новый маршрут исходящей передачи')).toBe(true);
    world=run(world,{type:'purge',assetId:'APP-01'});
    expect(coachMessage(observe(world)).title).toBe('Проверь последствия');
    world=run(world,{type:'verify'});
    expect(world.outcome).toBe('clean');expect(world.truth.attacker.leaked).toBe(0);expect(world.truth.business.ordersDownTicks).toBe(0);
    expect(replay(world.seed,world.commands,world.tick,'breach')).toEqual(world);
  });
  it('makes investigating before containment cost real data',()=>{
    let world=createWorld('HOT-1','breach');world=run(world,{type:'inspect',assetId:'APP-01'});
    expect(world.truth.attacker.leaked).toBe(50);expect(observedTransfer(observe(world))).toBe(50);
    world=run(world,{type:'block',channel:'exchange',blocked:true});world=run(world,{type:'purge',assetId:'APP-01'});world=run(world,{type:'verify'});
    expect(world.outcome).toBe('costly');expect(world.truth.attacker.leaked).toBe(50);
  });
  it('allows emergency isolation but charges outage and makes slow recovery fail',()=>{
    let world=createWorld('HOT-1','breach');world=run(world,{type:'isolate',assetId:'APP-01',isolated:true});
    world=run(world,{type:'restore',assetId:'APP-01'});world=run(world,{type:'isolate',assetId:'APP-01',isolated:false});world=run(world,{type:'verify'});
    expect(world.outcome).toBe('costly');expect(world.truth.attacker.leaked).toBe(0);expect(world.truth.business.ordersDownTicks).toBe(5);
    let late=createWorld('HOT-1','breach');late=run(late,{type:'isolate',assetId:'APP-01',isolated:true});late=run(late,{type:'inspect',assetId:'APP-01'});late=run(late,{type:'restore',assetId:'APP-01'});
    expect(late.outcome).toBe('failure');
  });
  it('keeps witness information separate from endpoint knowledge',()=>{
    const world=run(createWorld(),{type:'contact',target:'contractor'});
    const witness=world.observations.find(e=>e.source==='СООБЩЕНИЕ СОТРУДНИКА');expect(witness).toBeDefined();expect(witness?.finding).toBeUndefined();
    expect(execute(world,{type:'purge',assetId:'WS-07'}).result.accepted).toBe(false);
  });
});
