# NETWORK DEFENSE — iteration2 integration

This is the third implemented external app (Snake, Payload, Network), not acceptance
of the full computer-world iteration. No new production bundle or FPS claim.

## Rules and desktop

GNOME-inspired original desktop uses the same physical DOM/RT in nearby/focused views.
Three waves cross an eight-node directed graph. Four shared CPU units fund defence
and isolation; isolation retains processes. Infection disables CPU at 80%. Recharging
purges, adjustable forks and later local injections require active decisions.
The app freezes away/minimized; the world, reserve clock and outage continue.
Retained graph/process elements avoid rebuilding the DOM every frame. Window bounds
stay above the taskbar. UI texture `network-gnome-desktop.png` was actually inspected.

Exact snapshots contain packets, routes, infection, resources, partial game step,
world reserve/outage and radio cadence. Unity JSON can move a binary64 clock by one
ULP: comparisons tolerate 1ps for clocks, not arbitrary gameplay state. A long replay
after actual JsonUtility serialization confirms the same packets, nodes and outcome.
Messages validate by unique ID, preserving real completion order across applications.

## Physical consequences

- Command isolation removes shared target memory; local hearing/vision remain.
- Wave1 permanently ends bounded local radio relay and reduces future reserves.
- Wave2 gives one 15-second outage; save/load never replays it.
- Wave3 cancels future reserves, unlocks vault/energy and publishes the home journal.
- Two dormant Quaternius actors have stable IDs, visible duct emergence and collision
  checks; already released actors stay in the world/save after cancellation.
- Robot death during outage resumes the TurnOff animation instead of freezing it.

Initial north-wall duct passed stationary capsule samples but blocked the real CC
bypass at (1.78,.04,12.81). It was moved into the east rack bay at (3.65,0,13), yaw90.
The northeast rack was retained at a tested clear vault position (5.5,0,21.5). The
weapon rack remains the same pickup/ID on the upper gallery. CC now travels through
vault and back; gallery clearance is also exercised. Actual before/after renders:
`hf2-server-reserve-before.png`, `hf2-server-network-authored.png`,
`hf2-server-network-clearance.png` under `.artifacts/hostfall2-20261117`.

## Verification

- 55/55 pure checks before starting Circuit work, including eight Network cases.
- Full EditMode job `ca40c5512109425ab53be3ebfe4640e5`: **104/104**, 2.929 s.
- Final computer/enemy/restore PlayMode job `6684e3132c4849e789b1d074a19779c8`:
  **28/28**, 38.926 s. Includes UI commands, real passages, live relay/local hearing,
  one reserve, outage/death, exact onSceneLoaded pose and private save/restore.
- Earlier repaired Network-only job `49e0a19ceda14355ab820f7532f737c8`: 2/2.
- Earlier 34-case run found one post-load timing assertion and one 1-ULP clock
  comparison; both corrected without relaxing structural gameplay comparisons.
- Two subsequent runs failed from FMOD's output reinitialization warning. Restarting
  the isolated editor with `-noaudio` did not fix it; a clean audio-enabled editor
  passed all 28 affected checks. No warnings were suppressed or production audio
  disabled. These failed runs remain recorded rather than counted as green.
- Source API compile passed (existing Cinemachine nullable warning). 676 GUID
  integrity passed before adding Circuit files. All 11 production save hashes remain
  unchanged (`user-saves-after-network.json`). Tests use private storage.
- Reviewed clone scene promoted only to the 2.0 source preview with backup/hash:
  `network-scene-promotion.json`. Original NorthlineFacility SHA256 remains
  `569da130d17813bd8fb0922e77bc48e2511d7be51242a7c1877bf3b78689afc7`.

Native Network acceptance and the remaining nine external apps, Recovery, complete
route and later iteration gates remain outstanding. The bounded relay is a foundation,
not completion of iteration3 tactical squad AI.

Map follow-up job `3d03f9bc728e456697488aada46cbecc`: 6/6 PlayMode passed.
