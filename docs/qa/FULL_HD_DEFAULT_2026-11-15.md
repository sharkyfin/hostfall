# Full HD default — 15 November 2026

Requested narrow change: start at 1920×1080. PlayerSettings and the matching
FacilityBuilder.ConfigureProject authoring defaults now use width 1920, height 1080,
with defaultIsNativeResolution=false. FullScreenWindow, Retina support, resizableWindow
and the existing fullscreen preference behaviour are retained. UI reference resolution
is unchanged. No runtime SetResolution or scene-load resize was introduced.

The isolated Unity 6000.6.8f1 build read back and asserted the imported PlayerSettings:
1920×1080, nativeResolution=false, resizable=true, FullScreenWindow. Native release
build Succeeded / 0 errors and updated `apps/unity/Builds/macOS/HOSTFALL-Robots.app`.
Ad-hoc signature verification PASS. A 12-second headless startup remained running,
then exited cleanly with code 0; no script, shader or load errors. This does not constitute
a native-window resolution measurement. Receipts/logs and the previous app are in `.artifacts/fullhd-default-20261115/`.
The authored scene hash remains 06fd575b16dd232c5a7547874523733f375212623e5c0ed8e3c90574286ab706.
No gameplay or save changes; broad gameplay suites were not repeated for this settings-only edit.

These checks confirm the shipped defaults, not a measured FPS gain or the dimensions
of a manually resized window. Fullscreen scaling behaviour is documented by
[Unity FullScreenWindow](https://docs.unity3d.com/6000.0/ScriptReference/FullScreenMode.FullScreenWindow.html).


## Saved display override found after lag report

The existing com.northline.hostfall display profile contains 3584×2240, Use Native=1
and Fullscreen mode=2, despite its Width/Height Default fields reading 1920×1080.
The build-default change did not replace this existing display override. This is nearly
3.87 times the pixel count of Full HD. Preferences were exported for backup. The game
was initially running, so changes waited for the user to save and close it. After closure,
backed up preferences again and changed only width=1920, height=1080, Use Native=0,
Fullscreen mode=1 (FullScreenWindow). Readback PASS; all unrelated preferences compared
equal. World saves were not edited. This confirms saved startup settings, not a new
measurement of gameplay performance. Receipt: `display-profile-fixed.json`.
Evidence: `.artifacts/fullhd-default-20261115/display-diagnosis.json`.

Hardware readback: i7-9750H, 16 GB RAM, Radeon Pro 5300M 4 GB plus Intel UHD 630.
Previous native runs used the Radeon. At inspection pmset reported CPU speed/scheduler
limits 100 and no recorded thermal warning; this does not rule out throttling during play.
A separate structures_benchmark process used roughly one logical CPU and was left intact.
Historical swap usage alone was not treated as proof of current memory pressure.
No assertion that hardware is the sole cause, or that earlier short replay samples
represent the user's present gameplay session, is justified by these checks.
