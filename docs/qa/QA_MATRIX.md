# HOSTFALL — Master QA Acceptance Matrix (25 Acceptance Tests)

**Revision:** 2026-11-09  
**Specification:** HOSTFALL Master Design & Implementation Brief  
**Execution Harness:** Unified Behavioral Simulation Suite (`tools/test-simulation.sh`), Unity EditMode Suite (`tools/unity.sh editmode`), Unity PlayMode Suite (`tools/unity.sh playmode`), and Static Repository Checkers (`python3 tools/verify_repository.py`).

---

## 1. QA Acceptance Test Specifications (QA01–QA25)

### Group 1: Character Motor, Locomotion & Spatial Bounds (QA01–QA05)

| Test ID | Test Name | Target Subsystem | Test Steps | Acceptance Criteria (PASS) | Verification Method |
|---|---|---|---|---|---|
| **QA01** | First-Person Locomotion & Inertia | `PlayerMotor.cs` | 1. Spawn player at $(0, 0.1, -12)$.<br>2. Apply WASD inputs.<br>3. Verify walk speed ($4.2$ m/s) and sprint speed ($6.8$ m/s). | Smooth acceleration, deceleration without slipping; head height nominal ($1.70$ m). | `PlayerMotorTests.cs` (PlayMode) |
| **QA02** | Stair Incline & Vertical Traversal | `PlayerMotor.cs`, `Platform_Stairs_4` | 1. Walk player up modular stairs in S02 from $Y=0$ to $Y=2.4$ m.<br>2. Reverse and walk down. | Zero snagging, zero micro-teleportation, step offset smoothly handled. | `SpatialNavigationTests.cs` (PlayMode) |
| **QA03** | Spatial Containment & Anti-Void Failsafe | `PlayerMotor.cs`, Monolithic Walls | 1. Drive player against all perimeter boundaries in S01, S02, S03, S04.<br>2. Force collision with door jambs. | Player cannot clip through geometry. Failsafe recovery triggers if $Y < -2.0$ m, returning player to safe ground. | `BoundariesSafetyTests.cs` (Simulation/PlayMode) |
| **QA04** | Crouch & Low Clearance Navigation | `PlayerMotor.cs`, `CharacterController` | 1. Press `[Ctrl]` / Crouch.<br>2. Move under low piping ($H = 1.3$ m).<br>3. Attempt uncrouch beneath obstacle. | Controller height shrinks to $1.1$ m smoothly; cannot stand until clearance $\ge 1.8$ m. | `MotorPostureTests.cs` (PlayMode) |
| **QA05** | Fall Impact & Landing Head Bob | `PlayerMotor.cs`, `PlayerCameraMotor` | 1. Walk off Catwalk ($Y=2.4$ m) onto concrete floor.<br>2. Measure vertical velocity and recovery. | Velocity decelerates cleanly; camera applies subtle vertical landing dip without disorientation. | `CameraDynamicsTests.cs` (PlayMode) |

---

### Group 2: Ballistics, Combat & Robotics Destruction (QA06–QA10)

| Test ID | Test Name | Target Subsystem | Test Steps | Acceptance Criteria (PASS) | Verification Method |
|---|---|---|---|---|---|
| **QA06** | Weapon Pickup & First-Person Rigging | `WeaponPickup.cs`, `FpsWeaponView.cs` | 1. Walk up to Armory Display Pedestal.<br>2. Press `[E]` on ARC-12 shotgun. | Shotgun attaches to player viewport; ammo counter updates in HUD; viewmodel follows camera pitch/yaw rigidly. | `CombatInventoryTests.cs` (PlayMode) |
| **QA07** | Pellet Spread & Raycast Penetration | `WeaponSystem.cs` | 1. Fire ARC-12 at flat wall from 5m distance.<br>2. Count raycast hit points. | 8 distinct pellets generated within conical spread ($3.5^\circ$); spark particles and decal marks spawn at hit points. | `BallisticsMathTests.cs` (EditMode) |
| **QA08** | Lethal Damage Scaling vs Robot Armor | `WeaponSystem.cs`, `WatcherController.cs` | 1. Fire point-blank at WATCHER R17 ($HP = 120$).<br>2. Fire second shot. | First shot deals $\approx 64$ damage (triggers stagger); second shot reduces $HP \le 0$, destroying robot in 2 shots. | `DamageMatrixTests.cs` (Simulation) |
| **QA09** | Robot Stagger & Sparks FX Feedback | `WatcherController.cs`, VFX | 1. Hit active pursuing SENTRY R18 with 1 shotgun blast. | Robot enters STAGGER state for $0.8$ s; optic light flickers amber/red; pursuit pauses temporarily. | `RobotAiBehaviorTests.cs` (PlayMode) |
| **QA10** | Reload Cycle & Ammo Depletion | `WeaponSystem.cs`, `WeaponAudioSynthesizer` | 1. Discharge all 6 shells from tube.<br>2. Press `[R]`.<br>3. Verify ammo reserve depletion. | Weapon cannot fire when empty; reload animation/audio plays; magazine replenishes to 6 from reserve. | `WeaponStateTests.cs` (PlayMode) |

---

### Group 3: Terminal Systems, CLI Emulation & Automation (QA11–QA15)

| Test ID | Test Name | Target Subsystem | Test Steps | Acceptance Criteria (PASS) | Verification Method |
|---|---|---|---|---|---|
| **QA11** | Zero-Typing Interaction Trigger (`[E]`) | `DoorTerminalConsole.cs`, `FacilityUI` | 1. Stand 1.5m in front of `DATA-01`.<br>2. Press `[E]`. | Monospace terminal modal opens; NO TextField or keyboard input prompt appears (ADR 0007). | `TerminalInteractionTests.cs` (EditMode/PlayMode) |
| **QA12** | Scripted Command Sequence Flow | `FacilityUI.cs` | 1. Observe automated typing on `DATA-01`.<br>2. Verify timing of 4 commands. | Commands render sequentially with typewriter effect; status updates on physical terminal screen in real time. | `TerminalEmulationTests.cs` (PlayMode) |
| **QA13** | Callback Single-Invocation Guarantee | `FacilityUI.cs` (`OnComplete`) | 1. Complete terminal session.<br>2. Monitor event log for `OnComplete` dispatch count. | `OnComplete` is dispatched EXACTLY ONCE; `cs.OnComplete = null;` prevents multi-frame loop firing. | `CallbackIntegrityTests.cs` (EditMode) |
| **QA14** | World Door Actuation on Completion | `DoorTerminalConsole.cs`, `FacilityDoor` | 1. Let `DATA-01` finish.<br>2. Inspect Energy Airlock and Armory Door. | Both doors unlock immediately; status LEDs turn Green; doors slide open with audible pneumatic hiss. | `DoorActuationTests.cs` (PlayMode) |
| **QA15** | Terminal Screen & LED Material Swap | `DoorTerminalConsole.cs`, Materials | 1. Verify terminal screen material before interaction.<br>2. Complete session.<br>3. Re-verify. | Material swaps to `Indicator Green` or lit screen; in-world 3D text displays `[ACCESS GRANTED]`. | `VisualIndicatorTests.cs` (PlayMode) |

---

### Group 4: Electrical Puzzles & Physical Devices (QA16–QA20)

| Test ID | Test Name | Target Subsystem | Test Steps | Acceptance Criteria (PASS) | Verification Method |
|---|---|---|---|---|---|
| **QA16** | Mezzanine Breaker Circuit Interlock | `BreakerSwitch.cs`, `FacilityDoor` | 1. Interact with Breaker P03 on Catwalk.<br>2. Observe electrical bridge lights. | Circuit indicator turns Amber to Green; contact bridge servo closes; circuit continuity confirmed. | `PowerPuzzleTests.cs` (EditMode) |
| **QA17** | Anti-Crush Door Safety Sensor | `FacilityDoor.cs` | 1. Stand directly in doorway while door attempts to auto-close. | Door detects presence via trigger, halts closure, and returns to fully OPEN state. | `SafetySensorsTests.cs` (PlayMode) |
| **QA18** | Physical Data Cartridge Insertion | `CartridgeSlot.cs`, `Simulation` | 1. Pick up cartridge `DAT-04`.<br>2. Insert into reader slot at `DATA-01`. | Cartridge clicks into physical latch; reader LED illuminates; manifest data populates on screen. | `PhysicalPropTests.cs` (Simulation) |
| **QA19** | Coolant Bypass Valve Actuation | `CoolingValve.cs`, ParticleSystem | 1. Rotate manual handwheel on SW cooling column in S03. | Handwheel rotates $360^\circ$; steam release particle burst vents; core pressure gauge drops by $30\%$. | `MachineryTests.cs` (PlayMode) |
| **QA20** | Executive Shortcut Gate Dependency | `DoorTerminalConsole.cs` (`QNT-01`) | 1. Attempt opening Executive Door from SOC side.<br>2. Travel to S04, complete `QNT-01`.<br>3. Return to SOC. | Door remains locked until `QNT-01` completes; opens permanently creating fast return loop to SOC. | `WorldLoopTests.cs` (PlayMode) |

---

### Group 5: Save/Load State, Performance & Integrity (QA21–QA25)

| Test ID | Test Name | Target Subsystem | Test Steps | Acceptance Criteria (PASS) | Verification Method |
|---|---|---|---|---|---|
| **QA21** | Complete Save/Load Roundtrip | `WorldSaveStore.cs`, `SimulationStore` | 1. Pick up shotgun, unlock Armory, kill WATCHER.<br>2. Save to Slot 1.<br>3. Restart scene and Load Slot 1. | Player position, inventory ammo, door unlock states, and dead robot states restored with 100% fidelity. | `PersistenceTests.cs` (Simulation/PlayMode) |
| **QA22** | Frame Rate & Frame Pacing Under Load | URP Lighting & Shaders | 1. Spawn in S02 with all lights, robots, and shadows active.<br>2. Measure frame times over 60 seconds. | Stable 60 FPS ($\le 16.6$ ms frame time) on Apple Silicon M-series and desktop GPUs; zero stutter spikes $> 33$ ms. | `PerformanceProfilingTests.cs` (PlayMode) |
| **QA23** | Memory Leak & Asset Lifetime Audit | Unity Profiler / Garbage Collector | 1. Perform 20 consecutive door transitions and terminal opens.<br>2. Inspect mono heap growth. | Mono heap stabilizes $\le 128$ MB; zero orphaned GameObject leaks after scene reload. | `MemoryLeakAudit.cs` (PlayMode) |
| **QA24** | Zero Console Error / Warning Free Execution | Log Assertions | 1. Run full 10-minute automated smoke-test walkthrough. | ZERO `NullReferenceException`, ZERO `MissingReferenceException`, ZERO invalid physics layer warnings. | `ZeroWarningAudit.cs` (PlayMode) |
| **QA25** | Repository Asset GUID & Meta Integrity | `verify_repository.py` | 1. Run `python3 tools/verify_repository.py` on workspace. | All 322 asset GUIDs intact; zero dangling `.meta` files; zero missing references. | Automated Python Script |

---

## 2. Test Execution Commands & Automation Matrix

```bash
# 1. Pure C# Simulation Behavioral Suite (Fast Unit Verification)
./tools/test-simulation.sh

# 2. Asset Integrity, Python Syntax & GUID Consistency Checks
python3 tools/check-unity-api.py
python3 tools/verify_repository.py

# 3. Unity EditMode Test Suite (Scene & Component Configuration)
./tools/unity.sh editmode

# 4. Unity PlayMode Test Suite (Runtime Physics, AI, Weapons & Motor)
./tools/unity.sh playmode
```

