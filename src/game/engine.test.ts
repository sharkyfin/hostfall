import { describe, expect, it } from 'vitest';
import { advance, canReach, createWorld, execute, observe, replay, review } from './engine';
import type { Command, World } from './types';
import { precise } from './policies';

const run = (world: World, command: Command) => execute(world, command).world;
function until(world: World, predicate: (w: World) => boolean): World {
  for (let i = 0; i < 36 && world.outcome === 'active' && !predicate(world); i++) world = advance(world);
  return world;
}

describe('causal incident simulation', () => {
  it('replays every event, observation and outcome exactly, including idle time', () => {
    let world = advance(createWorld(), 1);
    world = run(world, { type: 'inspect', assetId: 'WS-07' });
    world = run(world, { type: 'isolate', assetId: 'WS-07', isolated: true });
    world = advance(world, 2);
    world = run(world, { type: 'revoke', accountId: world.accountId });
    expect(replay(world.seed, world.commands, world.tick)).toEqual(world);
  });
  it('has three materially different outcomes on the same seed', () => {
    expect(precise().outcome).toBe('clean');
    let rough = createWorld();
    rough = run(rough, { type: 'isolate', assetId: 'APP-01', isolated: true });
    rough = run(rough, { type: 'isolate', assetId: 'WS-07', isolated: true });
    rough = run(rough, { type: 'revoke', accountId: rough.accountId });
    rough = run(rough, { type: 'isolate', assetId: 'APP-01', isolated: false });
    rough = run(rough, { type: 'restore', assetId: 'WS-07' });
    rough = run(rough, { type: 'isolate', assetId: 'WS-07', isolated: false });
    rough = run(rough, { type: 'verify' });
    expect(rough.outcome).toBe('costly');
    expect(rough.truth.business.ordersDownTicks).toBe(3);
    expect(advance(createWorld(), 36).outcome).toBe('failure');
  });
  it('cuts the real route immediately; isolating backup does not cut it', () => {
    let protectedWorld = run(createWorld(), { type: 'isolate', assetId: 'WS-07', isolated: true });
    expect(canReach(protectedWorld, 'WS-07', 'APP-01')).toBe(false);
    protectedWorld = advance(protectedWorld, 18);
    expect(protectedWorld.truth.attacker.appControlled).toBe(false);
    const wrongTarget = advance(run(createWorld(), { type: 'isolate', assetId: 'BACKUP-01', isolated: true }), 30);
    expect(wrongTarget.truth.attacker.leaked).toBe(100);
  });
  it('revokes the stolen session without removing local persistence', () => {
    const world = run(createWorld(), { type: 'revoke', accountId: createWorld().accountId });
    expect(world.truth.sessionActive).toBe(false);
    expect(world.truth.attacker.footholds).toContain('WS-07');
    expect(advance(world, 30).truth.attacker.leaked).toBe(100);
  });
  it('early revocation stops a session-only seed before persistence completes', () => {
    const seed = Array.from({ length: 30 }, (_, i) => `session-${i}`).find(seed => createWorld(seed).truth.attacker.footholds.length === 0)!;
    let world = createWorld(seed);
    world = run(world, { type: 'revoke', accountId: world.accountId });
    world = advance(world, 15);
    expect(world.truth.attacker.footholds).toEqual([]);
    expect(world.truth.attacker.appControlled).toBe(false);
  });
  it('does not transmit a byte on a blocked channel and uses only a valid fallback', () => {
    let world = until(createWorld(), w => w.truth.attacker.staged > 0);
    world = run(world, { type: 'block', channel: 'exchange', blocked: true });
    expect(world.truth.attacker.leaked).toBe(0);
    expect(world.truth.attacker.job?.type).toBe('reroute');
    world = run(world, { type: 'isolate', assetId: 'MAIL-01', isolated: true });
    world = advance(world, 8);
    expect(world.truth.attacker.leaked).toBe(0);
    expect(world.truth.attacker.appControlled).toBe(true);
  });
  it('actually adapts to the mail route when it is available', () => {
    let world = until(createWorld(), w => w.truth.attacker.staged > 0);
    world = run(world, { type: 'block', channel: 'exchange', blocked: true });
    world = advance(world, 4);
    expect(world.truth.attacker.leaked).toBeGreaterThan(0);
    expect(world.events.some(e => e.causeId.startsWith('reroute-'))).toBe(true);
    expect(world.observations.some(e => e.detail.includes('mail-edge.invalid'))).toBe(true);
  });
  it('counts service outage through dependencies and does not reset accumulated harm', () => {
    let world = run(createWorld(), { type: 'isolate', assetId: 'APP-01', isolated: true });
    expect(observe(world).ordersUp).toBe(false);
    world = advance(world, 2);
    world = run(world, { type: 'isolate', assetId: 'APP-01', isolated: false });
    expect(observe(world).ordersUp).toBe(true);
    expect(world.truth.business.ordersDownTicks).toBe(3);
    world = run(world, { type: 'isolate', assetId: 'DB-01', isolated: true });
    expect(advance(world, 4).outcome).toBe('failure');
  });
  it('makes blanket isolation a business failure rather than a perfect victory', () => {
    let world = createWorld();
    for (const assetId of ['GW-01', 'IDP-01', 'WS-07', 'APP-01', 'DB-01', 'BACKUP-01', 'MAIL-01'] as const) world = run(world, { type: 'isolate', assetId, isolated: true });
    expect(world.outcome).toBe('failure');
    expect(world.truth.business.ordersDownTicks).toBe(6);
  });
  it('keeps backup causal and makes its interruption costly', () => {
    let world = run(createWorld(), { type: 'inspect', assetId: 'BACKUP-01' });
    expect(world.observations.some(e => e.detail.includes('подписанный backup-worker'))).toBe(true);
    expect(world.events.some(e => e.actor === 'normal' && e.severity === 'warning')).toBe(true);
    world = run(world, { type: 'block', channel: 'archive', blocked: true });
    world = advance(world, 2);
    expect(world.truth.business.backupLostTicks).toBeGreaterThan(0);
    expect(world.truth.attacker.job).not.toBeNull();
  });
  it('projects only player observations; identity telemetry cannot inspect endpoint truth', () => {
    const world = run(createWorld(), { type: 'inspect', assetId: 'IDP-01' });
    const observation = observe(world);
    expect(observation).not.toHaveProperty('truth');
    expect(observation).not.toHaveProperty('attacker');
    for (const event of observation.events) {
      expect(event).not.toHaveProperty('actor');
      expect(event).not.toHaveProperty('causeId');
      expect(event).not.toHaveProperty('deliveryTick');
    }
    expect(observation.events.filter(e => e.title.startsWith('Снимок')).every(e => !e.detail.includes('remote-maint'))).toBe(true);
    expect(review(world)).toBeNull();
  });
  it('does not duplicate a command, cost, time or events on retry', () => {
    const first = execute(createWorld(), { type: 'inspect', assetId: 'WS-07' }, 'request-123');
    const retry = execute(first.world, { type: 'inspect', assetId: 'WS-07' }, 'request-123');
    expect(retry.world).toBe(first.world);
    const invalid = execute(first.world, { type: 'restore', assetId: 'APP-01' });
    expect(invalid.result.accepted).toBe(false);
    expect(invalid.result.reason).toContain('Сначала изолируйте');
    expect(invalid.world).toBe(first.world);
  });
  it('does not award eradication for silence behind isolation', () => {
    let world = run(createWorld(), { type: 'isolate', assetId: 'WS-07', isolated: true });
    world = run(world, { type: 'revoke', accountId: world.accountId });
    world = run(world, { type: 'verify' });
    expect(world.outcome).toBe('incomplete');
    expect(review(world)?.residualHosts).toContain('WS-07');
  });
  it('stops at irreversible failure during a long operation', () => {
    let world = until(createWorld(), w => w.truth.attacker.leaked >= 75);
    const operation = execute(world, { type: 'inspect', assetId: 'BACKUP-01' });
    world = operation.world;
    expect(world.outcome).toBe('failure');
    expect(world.truth.attacker.leaked).toBe(100);
    expect(operation.result.completed).toBe(false);
    expect(operation.result.ticks).toBe(1);
  });
  it('requires service permissions in addition to a reachable database', () => {
    let world = createWorld();
    world.truth.identityPermissions = world.truth.identityPermissions.filter(p => p.principal !== 'svc.orders');
    world = advance(world, 25);
    expect(world.truth.attacker.appControlled).toBe(true);
    expect(canReach(world, 'APP-01', 'DB-01')).toBe(true);
    expect(world.truth.attacker.staged).toBe(0);
    expect(world.truth.attacker.leaked).toBe(0);
  });
  it('rejects damaged saves instead of silently changing their replay', () => {
    const world = precise();
    expect(() => replay(world.seed, world.commands, world.tick - 1)).toThrow();
    expect(() => replay(world.seed, [...world.commands, world.commands[0]], world.tick)).toThrow();
    expect(() => replay(world.seed, [{ id: 'bad', tick: 0, command: { type: 'isolate', assetId: 'missing', isolated: true } } as never], 1)).toThrow();
  });
  it('does not let WS recovery delay an independently controlled APP process', () => {
    let world = until(createWorld(), w => w.truth.attacker.appControlled);
    world = run(world, { type: 'isolate', assetId: 'WS-07', isolated: true });
    const expectedEntry = world.truth.attacker.job;
    expect(expectedEntry?.type).toBe('persist-app');
    world = run(world, { type: 'restore', assetId: 'WS-07' });
    expect(world.truth.attacker.staged).toBe(100);
    expect(world.truth.attacker.footholds).toContain('APP-01');
  });
});
