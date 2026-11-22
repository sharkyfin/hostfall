# Technical architecture / migration

Keep boundaries: pure Simulation owns game/application/progress/archive/desired mechanism state; Unity adapters own geometry, input, sound, animation and scene registration. UI sends typed commands and presents results. No runtime player code, host shell, backend, new DI or DOTS.

- Reuse VirtualComputer/TerminalMemory for a dedicated Home01 machine, separate from the three legacy simulated hosts. HomeShell only handles bounded virtual navigation/read/search/history/help/completion; it cannot invoke systemctl/host processes or an OS shell.
- ComputerCampaign stores unique pc.* station states, typed milestone receipts, archive revisions, desired world actions and persistent horror state. MiniGameSession models are pure C# and independent of loaded scenes. WorldActionController binds stable target IDs and applies desired state on registration/load; missing required targets fail authoring/QA loudly.
- ComputerStation replaces the active legacy interactors. Shared desktop/window manager and apps are reusable UI Toolkit components. Reuse screen corners, focus/return and one bounded offscreen display; native glass samples the same texture. Text input is home-only. External games own input focus and never execute commands automatically.
- Add ControlMode.Computer with a running world clock, separately from CanExplore. AI/physics run while desktop input is active. Pause/map/menu freeze; fast exit restores player controls and weapon safety. No hidden automated typing remains active in modern route.
- Explicit **world-v3** / `northline-2` and separate v3 slot filenames. v2 files/checkpoint remain untouched, no silent objective reinterpretation. Legacy models/readers/tests remain isolated until migrations are verified; active scene does not call TerminalDirector. V3 includes home files/output/draft/history, app windows/session state, puzzle state/seeds, milestone receipts, desired actions, devices/enemies/poses, archive and progression.
- Preserve old FieldContent and FieldProgress snapshots; modern guide replaces global knowledge access. Existing quest/journal UI may display new objectives, not tutorial/legacy command tasks.
- Iteration4 zone ownership: logical state outlives scenes; registered devices reapply desired state before becoming interactive. Pin player/current computer/boss arena, preload connected neighbors, cancel stale loads safely, reuse shared assets. Begin with measured authored sector boundaries; no fake streaming that merely toggles every object while retaining all scene memory.

Testing copies use private storage and separate QA bundle identity. Never run a native replay into production slots. Use actual scene and player checks in addition to API/model checks.

## Implemented iteration2 additions

`ComputerApplications` is the typed desktop app catalog (currently Snake + Payload + Network + Circuit + Signal).
`PayloadSession` owns deterministic 120 Hz 2D physics and snapshots; its UI adapter
uses retained elements and raw keyboard state only while its active window has focus.
`LogisticsSystem` owns bounded mechanism clocks; `LogisticsMechanisms` projects saved
state to the physical lift/belt/hinged span/alarm and carries CharacterController riders.
Save synchronizes physical mechanisms before player capture. `MapGeometrySource` is
a zero-Update authoring tag for explicit floor identity/moving ancestors; map refresh
still schedules only movers. Modern ComputerStation provides all 13 map markers.

NetworkDefenseSession owns the deterministic graph/waves/resources; NetworkSecuritySystem
owns saved world radio cadence/outage/reserve release. ServerNetworkController binds
that state to two physical reserve actors and existing patrols, retaining local senses.
Messages validate by stable ID while preserving chronological application completion.

CircuitSession owns reciprocal contacts, component power budgets, live voltage and
breaker/cooling state, plus world induction-pulse and louver clocks. One commission
receipt publishes a journal; it never replaces current power distribution.
CircuitPowerController projects to physical devices. FacilityDoor's optional power
authority overrides stale unlock data and preserves real occupied-door safety.


SignalSession holds oscillator and app timing. SignalEquipment holds world heat,
recharge and barrier windows. SignalBarrierController applies the desired state to
the existing gate and checks the player bounds before closure. Earned calibration
receipts leave the final window repeatable without duplicating archive progress.


MinefieldSession owns the same cell IDs used by the screen and authored world.
Its v3 snapshot keeps logical cell masks and alarm timing. The physical floor
projects active sensors and beams and raises damage only inside an armed volume.
Final receipts drive both named FacilityDoor targets and the security log.

DetectiveSession persists evidence selection, chronology, citations and archive
playhead. A reconstructed case and a recovered key are separate receipts.
NeuroInvestigationController owns the safe movement and projects the same
authored atlas on the observation-room monitors without per-frame rendering.

ContainmentSession accepts typed observations of the actor and safety beams.
The local controller applies saved leaf positions and R26 uses its existing
navigation for the selected route. CCTV uses a single low-resolution camera
request while the projected app is in focus. See ADR 0020.

BufferRecallSession keeps the preview clock, reconstruction and a claim bit.
The typed unlock applies to an authored locker. The pickup consumes its claim
only when a living player can actually use the repair.

FragmentDockSession validates every occupied disk cell on save and placement.
Its single volume receipt unlocks the original SEC-02 armory door.

QuorumDispatchSession accepts legal machine reservations in any planning order
and validates dependencies at dispatch. The one successful dispatch receipt
retains the unlocked west passage across loads.

IntegrityAuditSession owns per-bank checksums, probe limits and verified reports.
One receipt drives IntegrityTelemetryController and its actual arena markers.
RecoveryProtocolSession owns the three evidence-led stages. The campaign derives
the dossier from earned records. RecoveryTransit owns the saved shuttle trip and
door interlocks. The world controller moves the cab and its passenger along the
same authored route. See ADR 0021.
