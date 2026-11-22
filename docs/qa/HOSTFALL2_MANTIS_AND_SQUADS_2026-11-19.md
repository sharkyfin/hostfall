# HOSTFALL 2.0 · MANTIS-6 and Watcher squads

The isolated Unity 6000.6.8f1 workbench validated the four-attack encounter,
turret aim and recoil, wheel motion, legacy prefab, full computer route,
shuttle restore and physical R26 containment. Eight of nine checks initially
passed. The remaining squad fixture was missing the capsule required by
WatcherController. The same fixture with the actual capsule passed role
assignment, the two-attack cap and loss of coordination after command isolation.

Six rigid material groups preserve all 52,008 source mesh indices. The 19-bone
rig includes 12 wheels, the two track carriers and a separate turret and slide.
Two rendered views were inspected. The preview scene and its rig were promoted
from the workbench with a before/after hash receipt. Both the 1.x scene and
its original MANTIS prefab match their checkpoints.

The first full round passed 154/154 Edit Mode checks and 107/114 Play Mode
checks. Six Play failures involved a discarded procedural sound clip, and a
physical pointer check missed the Home tab. After recreating clips when Unity
unloads them, targeted Play Mode passed 7/7 including both affected PAYLOAD
scenarios, pointer interaction, MANTIS cover and two new vent checks. The vent
checks exercised an actual hit, reduced camera effects, parry, one-shot
persistence, and release from the physical grate. A new broad run is pending.

Receipts under `.artifacts/hostfall2-20261117` include
`play-watcher-squad.json`, `play-squad-fixture.json`,
`mantis-scene-promotion.json`, `mantis-rig-geometry.txt`,
`hf2-mantis-rig-front.png`, `hf2-mantis-rig-rear.png`,
`edit-combat-round.json`, `play-combat-round.json` and
`play-vent-check.json`.
