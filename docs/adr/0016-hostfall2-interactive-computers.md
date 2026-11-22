# 0016 — HOSTFALL 2.0 interactive computer campaign

2026-11-17 · Accepted direction, implementation in progress.

## Context
The user's new MASTER_REQUEST explicitly replaces automatic external terminal demonstrations, global Knowledge and the former narrative finale. These new instructions supersede ADR0007/0014/0015 where they conflict. Existing verified geometry, assets, player/weapon systems and logical/physical boundaries remain valuable.

## Decision
Home alone gets a bounded manual virtual shell and local Knowledge/Messages. Thirteen discovered authored PCs become one home and twelve external unique applications, eight mandatory and four optional. Player-controlled games emit typed idempotent world actions. External desktops keep the world/AI alive; pause remains separate. Start new campaigns in the existing shelter. Original OS stylisations reuse a common functional desktop/window architecture and physical screens.

Version new saves explicitly as world-v3/northline-2 with separate files; preserve world-v2 and the full dirty-source checkpoint. Legacy simulation/readers remain migration contracts; active gameplay stops calling their automatic sequences. No arbitrary host execution, real OS/runtime, backend, DI or renderer/engine change.

## Consequences
Four sequential gates in docs/hostfall/ITERATION_STATUS.md; no move to later iteration before first-loop acceptance. New finale reveals that repairs restore isolated SUPER, with hostile independent quarantine controllers explaining combat. Original story/96 articles retained as migration sources, modern help is terminal-only. Audit, concrete models/UI/world changes and actual tests are required; docs alone are not implementation.
