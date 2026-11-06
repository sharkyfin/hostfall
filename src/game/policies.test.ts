import { expect, it } from 'vitest';
import { advance, createWorld, execute } from './engine';
import { ASSETS, hash } from './scenario';
import type { Command } from './types';
import { precise } from './policies';

it('audits 64 seeds against observation-led, random and blanket-isolation baselines', () => {
  const results = { investigate: 0, random: 0, blanket: 0 };
  for (let i = 0; i < 64; i++) {
    const seed = `audit-${i}`;
    // The policy uses the visible VPN destination to inspect/isolate WS-07;
    // it does not consult foothold truth to select a response.
    if (precise(seed).outcome === 'clean') results.investigate++;
    let random = createWorld(seed);
    for (let n = 0; n < 15 && random.outcome === 'active'; n++) {
      const assetId = ASSETS[hash(`${seed}:${n}:target`) % 7].id;
      const commands: Command[] = [
        { type: 'inspect', assetId }, { type: 'isolate', assetId, isolated: true },
        { type: 'revoke', accountId: random.accountId }, { type: 'verify' },
      ];
      const next = execute(random, commands[hash(`${seed}:${n}:action`) % commands.length]).world;
      random = next === random ? advance(random) : next;
    }
    if (random.outcome === 'clean') results.random++;
    let blanket = createWorld(seed);
    for (const asset of ASSETS.filter(a => a.id !== 'SOC-01')) blanket = execute(blanket, { type: 'isolate', assetId: asset.id, isolated: true }).world;
    if (blanket.outcome === 'clean') results.blanket++;
  }
  expect(results.investigate).toBe(64);
  expect(results.blanket).toBe(0);
  expect(results.investigate).toBeGreaterThan(results.random);
  console.info('64-seed diagnostic (not a human playtest):', results);
});
