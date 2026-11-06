import { createWorld, execute } from './engine';
import type { Command, World } from './types';

/** Diagnostic policy: follows the public VPN destination, never hidden truth. */
export function precise(seed?: string): World {
  let world = createWorld(seed);
  const commands: Command[] = [
    { type: 'inspect', assetId: 'WS-07' },
    { type: 'isolate', assetId: 'WS-07', isolated: true },
    { type: 'revoke', accountId: world.accountId },
    { type: 'restore', assetId: 'WS-07' },
    { type: 'isolate', assetId: 'WS-07', isolated: false },
    { type: 'verify' },
  ];
  for (const command of commands) world = execute(world, command).world;
  return world;
}
