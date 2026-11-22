# CIRCUIT BREAKER — live distribution and maintenance route

Iteration2 remains in progress. Four external applications now have real rules/UI/
world consequences: Snake, Network, Circuit and Payload. The other eight external
apps and home Recovery remain to implement. No new production bundle or FPS claim.

## Actual rules and UI

A 7x7 sparse reciprocal-contact graph has rotatable conductors, branch relays, a
bus-tie relay, MAIN (6) and AUX (3). Five loads cost 3/2/2/2/4. Two independently
checked configurations are a lit main route with ventilation on the separate reserve,
and combined sources for main door/bypass/defence with lights and ventilation off.
All loads cannot fit. Edits remove voltage; overload trips all outputs, cools for
2s and requires an explicit reset after fixing the wiring. Nothing auto-solves.

Original Plasma-like desktop has movable/minimizable windows, file manager/editor,
readme and a maintenance record. The retained schematic displays reciprocal wires,
source/load names, power state and overloads. Controls issue real model commands.
The same physical DOM/texture presents nearby and focused views. Actual texture
`circuit-plasma-desktop.png` was reviewed; taskbar active-button contrast was corrected.

Snapshots include wiring, switches, selection, voltage/trip/cooling, one commission,
world pulse cadence and partial louver travel. World equipment continues away from
or behind a minimized window. No runtime host code or real OS installation.

## Physical implementation

CircuitPowerController controls 11 retained route lights, eight visible louver pivots,
`door.sec04.cryo`, `door.sec01.exec` and a local induction turret reusing retained
art/materials. Every 2.5 world seconds a powered turret checks range/occlusion and
stuns an actual local Watcher for 1.6s, with a short visible/audio pulse. Its cadence
is saved; it does not erase the enemy or substitute for final combat design.

Cryo power gates progression to Quantum. The executive door powers the maintenance
return to SOC. The initial armory binding was replaced because that was a supply
dead end, not a shortcut. Power loss cannot close the gate through the real CC.
Door pose save remains separate; saved circuit state restores power authority.
One commission publishes `~/archive/power_restore.log` without duplicated rewards.

## Geometry defect and repair

A real CC round trip found `Connector Wall North` crossing the maintenance T junction
at (-15.40,.03,.29). The wall and attached collider originally spanned X=-16.75..-4.75;
they now meet the bypass east wall at X=-14.55..-4.75. No collision was merely disabled,
and no invisible bypass was added. Before/after camera images and scene backup are
in `.artifacts/hostfall2-20261117` (`hf2-maintenance-junction-*.png`,
`scene-before-maintenance-junction.unity`). The repaired both-way trip passed.
The rest of the large facility still requires the full later geometry/art audit.

## Verification

- 62/62 pure checks, including seven Circuit cases and real Network/Circuit reward
  order/compatibility. Source API compile passed (existing nullable warning).
- Full EditMode job `25891dd8af044dce8e7d259a112c5cc9`: 111/111, 2.495s.
- Circuit physical job `7f40914a554f4783bdc9dffb6eb05c4b`: 3/3, 11.423s. Actual UI
  rotates contacts/toggles relays/sources; lights/louvers/doors change; CC crosses
  cryo and returns, tests occupied-gate power loss, restores the saved louver pose,
  crosses maintenance route both ways, and observes real defensive stun/overload.
- The first load assertion observed after .1s of resumed simulation and legitimately
  saw additional louver travel. It now observes onSceneLoaded before the live clock;
  no gameplay tolerance was widened. The failed attempt is retained.
- Full broader PlayMode regression is tracked below after its terminal result.
- Source/meta integrity: 683 GUIDs before scene promotion. Scene/UI camera captures
  are actual editor/test renders, not manual native play. Native Circuit acceptance,
  remaining applications, Recovery and all later gates are still outstanding.

## Final regression and promotion

Full PlayMode job `e3de2ccd8cb2468aaf7e53d32c120efd`: **91/91 passed**, 128.488s.
The source preview now contains checked power bindings and the repaired maintenance
junction; backup/hash receipt is `circuit-scene-promotion.json`. Original 1.x scene
remains byte-for-byte unchanged. Final API compile and 683-GUID integrity passed.

Two production auto-file hashes differed from the 16:57 baseline when inspected at
20:20. Their actual modification times were 19:21:34 and 19:21:46, before the full
regression (20:15:58–20:18:08). Their payload shows newer progress near the Quantum
node. The cause is not attributed to the test run; newer data were preserved rather
than overwritten from an older baseline. All 11 observed files were copied exactly
to `user-saves-observed-20261117T192146`; `user-save-baseline-advance.json` records the
evidence/decision and `user-saves-after-circuit.json` is the new comparison baseline.
No production save was written or restored by the agent during this investigation.
