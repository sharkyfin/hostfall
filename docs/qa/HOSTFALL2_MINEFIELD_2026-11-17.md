# MINEFIELD integration

SEC-07 has an original blue desktop and a three-level Minesweeper application.
The on-screen rows run north to south, with the physical desk to the south.
Coordinates A1–F6 identify the same 36 floor panels, trigger volumes and beams
in the room. Revealing every safe cell and flagging each sensor earns the level.
A correct flag turns off the corresponding beam and sensor. Opening a live sensor
raises a six-second local alarm. Stepping on an armed cell also damages the player.
Retries preserve completed levels. Exact cells, the active level and the remaining
alarm duration are saved.

The final level disarms the whole floor and opens two physical gates. The route
crosses the former archive divider, runs through the CCTV room, and enters the
neuroarchive office. The two existing walls on the final seam were cut together.
CharacterController testing exposed a maintenance cart and three service fixtures
in the approach. The same objects were moved beside the passage. The round trip
passed with the relocated props and both shutters physically open.

The independent deduction test only reads open clues, flags and adjacency. It
solved all three boards using local constraints and subset differences. The world
test uses known fixture coordinates to isolate damage, alarm and actual travel.
The desktop test exercised a keyboard flag, verified the real beam and sensor
response, and observed the same cell and flags at scene load.

- Minefield rules and integration pass within the 76-case pure suite. The initial
  80-case run adds four pure DIGITAL DETECTIVE tests.
- Passage repair verified in Play Mode job `25dd68333a104855b15824c78794af19`.
- Full Play Mode job `3fd80cdd965b4ccf8ad2a30e66b37191` passed 94 of 96, including
  both MINEFIELD cases. The Snake reload comparison differed by one binary64 ULP on
  the partial clock. The legacy terminal typing check assumed a fixed .65s startup.
  The follow-up compares the Snake clock with the established 1ps tolerance, checks
  all discrete state exactly, and waits for the observable typing state.
- Source build passed API compilation with the existing Cinemachine warning.
- The desktop and room captures live under `.artifacts/hostfall2-20261117` as
  `minefield-aero-desktop.png` and `hf2-security-minefield.png`.
- Both corrected legacy assertions passed in targeted Play Mode job
  `d2bf52e4fd5047beacde7c2235767b59`. Full Edit Mode passed 129/129 in job
  `0374782a46714a96bb79bd998e729d6f`. The broad 94/96 result is retained separately.
- The checked scene was copied into the source preview with the original 1.x scene
  hash verified. See `minefield-scene-promotion.json` and the saved predecessor.
- Production auto files had been updated by the observation point. Their write times
  preceded the final regression, and the existing Workstations player was running
  during the campaign checks. Production files were read for comparison. Unity
  computer tests used private directories. The observation is recorded in
  `user-saves-observed-after-minefield.json`.
