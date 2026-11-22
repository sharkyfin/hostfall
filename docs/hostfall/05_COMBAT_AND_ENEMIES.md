# Combat and enemies

Retain real-body CharacterController/motion/IK, component damage, authored armour, shared budgeted A*, dropped weapon state and existing CIRRUS/ARC/TITAN/EMP roles. Audit actual hit reactions and attack controllers before replacing them. No extra gun that duplicates an existing role.

Iteration1: one physically clear, warned vent release, bounded damage/react window, actual enemy death/stun/flee and saved release state. External computer mode runs world/AI; player movement/fire stay disabled until quick exit. No spawn inside solids, no silent repeated rewards, no guaranteed hit during camera return.

Iteration3: perception (sight/occlusion/noise), finite last-known information, Patrol/Suspicious/Investigate/Search/Chase/Attack/Flank/Retreat/Stunned/Disabled. Shared squad information decays; commander/network failures impair it. Attack-token budgets avoid simultaneous unavoidable hits. Preserve immediate collision/floor checks and navigation's 96-edge / soft1.2ms budget unless measured changes justify adjustment.

Tank: separate turret/chassis, cannon burst, hydraulic charge with wall stun, slam, suppression, overheat weakpoints; phase telegraphs and recovery, actual animated parts and component destruction. Spider Matriarch (ceiling/leap/web/minions), Security Warden (shield/arms/grab/bounded support), Harvester (blades/magnet/debris/repair), Core Avatar (3 arena phases). Each has real attacks, visible boss HP, vulnerability, recoverable player choices and a deterministic attack test, not just a mesh.

Horror types are spaced by a tension director, warning/cooldown and persistent one-off IDs: vent release, face lunge/shake-off, vent grab, dead-machine awakening, false safety and stalker. Reduced intensity affects sound peaks/camera motion without hiding gameplay telegraphs. They do not all belong in the first room.
