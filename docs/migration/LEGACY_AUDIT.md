# Legacy audit — 2026-11-06

Baseline: `e0f8ad1b5d2c2dd49a9e2d42eebf55ad761d6c6f` (`stable`). Working tree was clean.
Original tree and imports: ORIGINAL_TREE.txt and ORIGINAL_IMPORTS.txt.
Recovery: Git history plus verified `.artifacts/hostfall-before-unity.bundle` (complete HEAD history).
No reset, commits, or remote writes performed.

Entry: index.html → src/main.tsx → App.tsx → horror/* and components/Modal.tsx.
There are no imports from the active application into src/game, useGame, NetworkScene or Debrief.
Baseline verification: 40/40 tests in four files; typecheck and production/Websim bundle passed.

| Component | Decision | Result / reason |
| --- | --- | --- |
| horror/{shell,world,filesystem,types,runner} | KEEP / PORT | Preserved byte-for-byte in legacy/terminal-v3/src/horror. C# subset is independent. |
| horror/runtime.test.ts | KEEP | 14 behavioral tests remain executable. |
| App, main, Modal, Editor, PatchPanel, useTerminal, audio, styles | ARCHIVE | Runnable v3 reference, outside Unity. Entire active CSS retained to avoid breaking reference. |
| package manifests, Vite, TypeScript, index.html | ARCHIVE | Reference builds via npm in its own directory. Websim post-build removed. |
| src/game and its tests | DELETE | Unreachable previous SOC simulation; recoverable in baseline Git. |
| useGame, NetworkScene, Debrief | DELETE | Only depend on deleted SOC cluster; no inbound active imports. |
| docs/legacy-v2 | DELETE | Superseded SOC documentation. |
| v3 docs and original README | ARCHIVE | Historical reference under legacy/terminal-v3; not current project truth. |
| scripts/package-websim.mjs, WEB_SIM_DEPLOYMENT.md | DELETE / ARCHIVE | Obsolete delivery script deleted; historical deployment record retained with v3 docs. |
| HOSTFALL_MASTER_PROMPT_CODEX.md | DELETE | Superseded directive; available in Git. |

The terminal reference retains scheduler, pipelines, redirects, environment, isolated JS Worker,
HTTP simulation, routes and endings. Unity does not import it. Detailed port status is in
../architecture/TERMINAL_MIGRATION.md. No claim of complete shell compatibility.
