# Continuous terminal glass and awakening — 2026-11-16

Verification copy: `.artifacts/window-mode-check`, explicit MCP instance `window-mode-check@e864613ac9f05ec9`, Unity 6000.6.8f1. Raw receipts: `.artifacts/awakening-20261116`.

Final acceptance: 2026-11-17. Scope and actual checks are recorded below.

- Root cause: distant TextMesh used a 62-column/15-row tail, different header, whitespace and prompt truncation; focus used another UI Toolkit layout. The near/focused view now shares the actual DOM, render texture and physical glass, with unified model/header/status/prompt/output.
- The first target test run found a UIDocument parent-panel assertion. A render-texture document must not be nested beneath a screen-space UIDocument with different PanelSettings. Its scene ownership is now separate; no expected-log suppression was added. The repeated target suite passed 10/10.
- Opening shows the existing real body, uses a local synthesized voice clip with subtitles and safe skip, freezes robot time, retains the authored start pose and passes into the existing introduction. A loaded slot never requests it.
- Initial paper positions hit existing inspection windows. Reviewed captures led to moving the SOC warnings to the north wall wings beside the new airlock. The new door reuses the existing authored airlock, not a flat replacement wall.
- New corridor door is closed in a fresh world, becomes available after WS-07, has a stable saved ID and leaves the natural route to EDGE-01 intact.

Primary API consulted: [PanelSettings.targetTexture](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/UIElements.PanelSettings-targetTexture.html). sRGB render target follows Unity's linear-colour guidance. This source is for editor validation, not a game UI link.


## Regression results

- Latest **77/77 Play Mode PASS**, including three new behavioural tests for awakening/load, stable DOM/texture and mouse projection, and the gated physical door/notes. The full cartridge route and existing campaign/robot/weapon tests pass.
- The first complete run was contaminated by a stale FMOD device error in the long-running batch editor. It was retained as a failed receipt, not suppressed. Restarting our verification editor with `-noaudio` removed that environment failure; game AudioManager remains enabled (`m_DisableAudio: 0`). Native players run with audio enabled separately. A stale test query for `routine-status` in the removed overlay was also corrected.
- API compilation PASS; one pre-existing Cinemachine nullability warning, zero errors. 33/33 pure Simulation PASS.
- Native initial review confirmed real body/subtitles and world paper messages. It also exposed softness from the Full HD 3D scale. The shared texture was raised to 2048×1142, retaining identical logical wrapping; focused mode composites that same texture natively within the glass rectangle. Mouse coordinates account for the panel scale; regression uses RuntimePanelUtils, not just the projection helper.
- Native screenshot scheduling was corrected: captures are yielded before changing camera/control state. Initial `terminal-focused.png` accidentally included the next frame's exploration HUD; this is not a passed acceptance capture.
- All real save hashes were unchanged by the initial native run. Its six-view 540-sample median 8.461 ms / p95 14.875 ms is an intermediate measurement; final native results follow below. Existing development warnings concern stripped inactive DOF/motion-blur passes; active script/load checks were clean.


## Final native acceptance and scene transfer

- Final native review at **3584×2240** was inspected: terminal near, approaching, focused and after leaving; opening body/subtitles; wall warnings. The near and focused views use the same DOM and texture. Focused glass is crisp and stays within the monitor bezel; the exploration HUD is hidden. The paper warnings sit on walls rather than inspection windows, and the long warning line was wrapped inside its paper bounds.
- Final six-view profile: **540 samples**, AMD Radeon Pro 5300M, uncapped/VSync disabled for measurement; median **5.304 ms**, p95 **12.394 ms**, median managed allocation **1632 bytes/frame**, median draw calls **1867.5**. This varies with view/system state and is not an FPS guarantee or evidence that this content pass improved performance. Default gameplay VSync and Full HD-equivalent world pixel budget are unchanged.
- **Every real user-save hash unchanged** before/after the final normal native players. Development bundle ID remains `com.northline.hostfall.workstationqa`, private TestStorageDirectory set before scene load. Production save files were never used as test output in this task.
- Native review/player audio was enabled. The authored PCM clip is mono 22050 Hz, 5.445 s, RMS 0.14275; provenance identifies the offline system synthesis. Headless tests do not prove audible output-device quality.
- Promoted only the verified scene, paper material, voice/provenance and generated metadata. Source scene hash checked before transfer: `ea9655aa368a52f4491d6eeeeacab14eaa79f5b44e1070ff9c5468a04b8113a8`; after: `569da130d17813bd8fb0922e77bc48e2511d7be51242a7c1877bf3b78689afc7`. Original scene remains backed up in task artifacts. Source scripts matched the verified copy byte-for-byte.
- Repository integrity **644 GUIDs PASS**, legacy reference unchanged. **77/77 Play Mode, 80/80 Edit Mode, 33/33 Simulation PASS**; final API compilation zero errors.


## Release / delivery — 2026-11-17

Native x86_64 production release **Succeeded, 0 errors**, 162,564,574 bytes. Bundle ID `com.northline.hostfall`; diagnostic replay code excluded. Strict deep bundle signature PASS. A 12-second headless smoke remained running, terminated cleanly with exit 0, and had no checked script/load exceptions. Real save hashes unchanged; the production preference domain was restored from the immediate pre-smoke export so Screenmanager state could not be changed by the headless launch.

Delivered `apps/unity/Builds/macOS/HOSTFALL-Awakening.app`; existing Workstations and Robots application paths also updated and independently signature-checked. Their previous bundles are retained under task artifacts. The verification editor was closed; source user editors were not touched. No commit or publication performed. New-game opening is intentionally absent when continuing an existing campaign. This is not a manual playthrough of the entire game or an audio-device quality assessment.
