# HOSTFALL 2.0 / CONTAINMENT

Unity 6000.6.8f1 isolated workbench. Actual R26 went from its existing patrol to
the calibration lane, then walked through the south opening into a new
two-shutter capture bay. The camera showed the same actor and shutter positions
as the sensor overlay. Closing both leaves and allowing the sensors to confirm
the bay enabled the relay switch, opened the Ring and left R26 in maintenance.
The core gate stayed locked. A real CharacterController traversed the Ring
door in both directions. A private reload preserved the completed capture, feed
choice and Ring access. The alternate case confirmed egress with occupied
sensors and explicit clearance after R26 was destroyed.

- Standalone rules: 84 passed. Full Edit Mode: 133/133. Full Play Mode: 100/100.
- Targeted Play Mode followed original R26 without teleporting the actor.
  It confirmed the live feed stopped rendering while minimized and after exit.
  An initial timing assertion clicked one UI refresh too early. Retrying waited
  for the actual displayed button to become enabled. Both targeted cases passed.
- Investigated visually using lower and catwalk scene views and actual rendered
  CCTV. Turned the north label toward its approach, shortened the launcher name
  to fit and assigned the taskbar the workstation's ID.
- Promoted the verified preview to source. Original NorthlineFacility hash still
  matches its checkpoint. Preview SHA-256:
  `6bd48f1fcd9d9bb2d5ede254fd235acf11078c2bddc3448098117bd782cd5fad`.
  Native performance and end-to-end campaign acceptance are pending.

Raw evidence under `.artifacts/hostfall2-20261117` includes
`play-containment-full.json`, `edit-containment-full.json`,
`containment-scene-promotion.json`, `containment-live-desktop.png` and
`containment-live-cctv.png`.
