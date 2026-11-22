# HOSTFALL 2.0. Field terminal verification

November 20, 2026. Unity 6000.6.8f1. The checked scene is `Hostfall2Facility.unity`.
The revision updates fourteen door prompts. The scene snapshot before that
operation is in `.artifacts/hostfall2-field-terminal/scene-before.unity`.

- The C# behavioral suite passed 118 checks. It includes negative verdicts, reading evidence across save/restore, closure with real containment sensor states, authorization prerequisites, and the save immediately after changing the power preset.
- Unity API compilation and asset identity checks passed.
- Play Mode verified the route from the shelter across the field and freight bridge to SEC-07. Targeted scenarios verified the R26 capture, timed field restore, optional armory, both terminal verdicts, pickup from the physical safe and the shuttle restore.
- The investigation photos are stills captured in Unity for HOSTFALL. Their sources are the freight mechanism, bay shutter, and Operations duct captures. They import at a maximum size of 512. The radio line is original Russian synthetic speech filtered to resemble the local speaker.

Unity Play Mode is an automated route and uses simulated input. The twenty-minute first-play estimate and user-facing pacing require a normal play session.

## Acceptance rerun on November 21

The connected Play Mode test reached the epilogue. It used the two current
terminals, walked through the actual powered doors, performed the Home
authorization and shuttle crossing, defeated the Core Avatar with the game's
hit tests and reloaded the final save. The five-test connected, combat and Home
run passed. Its result is `.artifacts/hostfall2-field-terminal/final-route.xml`.

The packaged x64 macOS app built with zero errors. The batch startup loaded the
scene, the app signature verified, and all three inventoried user saves retained
their hashes. A normal first play remains the measure of total campaign time.

The WebGL package built with zero errors in the staged Web project described in
the combined candidate QA. A fresh Chromium profile rendered the facility film,
opened the new game brief and started the awakening scene at 1280×800. No
uncaught browser exceptions were recorded. The build and verification report are
`apps/unity/Builds/Web/HOSTFALL-2-Investigation` and
`.artifacts/hostfall2-field-terminal/web-smoke.json`.

## Downloadable macOS preview

The November 21 standalone archive is
`.artifacts/hostfall2-desktop-download/HOSTFALL-macOS-Universal.zip`. Unity
6000.6.8f1 built it with zero errors from the staged 2.0 project. The executable
and UnityPlayer library both contain x86_64 and arm64 slices. ZIP integrity and
the extracted app's ad-hoc signature passed verification. The extracted app
loaded the scene on Metal on the Intel test Mac, and both inventoried save files
kept their hashes. See `smoke.json` alongside the archive. Apple Silicon startup
and comparative frame times were outside this check. Distribution notarization
and a Windows build are pending.

## Websim rendering budget

The WebGL template sizes the canvas from its CSS viewport with a maximum
surface of 921,600 pixels. Two consecutive slow 2.5-second windows trigger a
lower tier for the rest of that session. The two lower caps are 589,824 and
451,584 pixels. Background and unfocused measurements are discarded. The
validation script exercised all three tiers, the background guard and a resize.

At 1332×950 CSS pixels, Websim v1 used a 2664×1900 backing buffer on a Retina
display. The local Chrome comparison reused the same player files and the same
workshop view. Two 2-second samples were 26.6 and 29.3 browser animation frames
per second at 2664×1900, and 48.0 and 41.3 at 909×648. The lower resolution softens
text. The Home shell remained readable and accepted `help` without a pointer
click. This short check covers the starting room. Combat and later rooms were
outside the measurement.

The optimized entry page is saved as [Websim revision 2](https://websim.com/p/xli3ad926zpkxg4eqvag/2).
Its hosted preview loaded the menu and reached the workshop in Chrome. The
comparison and a screenshot are in `.artifacts/hostfall-websim-performance/`.
The Unity Web candidate build succeeded and generated the same entry page
byte for byte.
