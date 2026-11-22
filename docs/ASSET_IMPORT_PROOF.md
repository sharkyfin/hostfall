# HOSTFALL — ASSET IMPORT & PROVENANCE PROOF (PHASE F)

**Date:** 11 November 2026  
**License Compliance:** 100% CC0 1.0 Universal / SIL Open Font License 1.1 / Original HOSTFALL C# Engine Source  
**Unity Version:** 6000.6.8f1 (URP)  
**Target Scene:** `Assets/_HOSTFALL/Content/Scenes/NorthlineFacility.unity`

---

## 1. LICENSING & PROVENANCE OVERVIEW

All assets imported, generated, or referenced in the HOSTFALL project adhere strictly to the offline single-player commercial distribution guidelines:
- **Quaternius Sci-Fi Megakit (CC0 1.0 Universal)**: Architectural modular kit, stairs, railings, columns, wall panels, industrial crates, cables, conduits, and ventilation fixtures.
- **Quaternius Animated Cyber & Mech Essentials (CC0 1.0 Universal)**: Sentry R18, Watcher R17, Patrol Drone R24, Assembly Sentry R26, Bulwark Sentry R27, Security Jammer R28, Neuro Sentry R29, Trace Hound R30, Bastion Sentry R32, Atlas Platform.
- **Poly Haven Industrial Props (CC0 1.0 Universal)**: Maintenance Cart (Jule Bielitz), Shift Notebook (Ulan Cabanilla), Cooling Valve Handwheel (Jorge Camacho).
- **Typography & Font Assets (SIL Open Font License 1.1)**: JetBrains Mono and Inter SDF font stacks for high-legibility terminal and HUD text.
- **Original HOSTFALL Procedural Assets (MIT / CC0)**: Procedural weapon meshes (ARC-12 Tactical Shotgun, TITANBREAKER Rail Rifle), interactive terminals, high-density server monoliths, and physical media items.

---

## 2. PHYSICAL MEDIA & INTERACTIVE ITEMS INVENTORY

| Asset Identifier | Physical Prefab / Mesh | Placement Sector & Coords | Functional Mechanic | World Effect |
|---|---|---|---|---|
| `item.usb.hf_tools` | USB Housing, Gold Contact Pins, Amber Activity LED | S00 Engineer Shelter (`-2.6, 0.84, -16.42`) on HOME-PC desk | Pickup via [E], stored in `PhysicalMediaInventory` | Carries lead system engineer Vetrova's diagnostic suite; unlocks root diagnostic bypass in complex. |
| `item.cartridge.nl044` | Solid NVMe Enclosure, Heat Spreader, Amber Interface Plate | S02 Compute Vault (`4.6, 4.15, 35.6`) on Observation Mezzanine | Pickup via [E], inserted into `DataCartridgeSlot` at DATA-01 | Prerequisite for `DATA-01` master compute sequence; unlocks S03 Energy Airlock and S02 Armory. |
| `item.key.rbt_diag` | Heavy Crypto-Fob, Encrypted Security Die, Steel Lanyard Ring | S00 Engineer Shelter (`2.55, 0.92, -16.6`) on Hardware Workbench | Pickup via [E], stored in `PhysicalMediaInventory` | Robotics diagnostic keycard for RBT-01 terminal in S06 assembly bay. |

---

## 3. MODULAR STRUCTURAL ASSETS VERIFICATION

All modular kit elements in `Assets/_HOSTFALL/Content/Prefabs/Kit/` were audited for geometry, scale, and collider integrity:
- `Platform_Stairs_4.prefab`: Full-bounding root `BoxCollider` removed from `ModularKit.Place()`, enabling unobstructed player stair climbing.
- `Platform_Stairs_2.prefab`: Trench service egress stairs verified; landing openings cleared of blocking railings.
- `Platform_Rails_4Wide.prefab`: Guardrails segmented at stair entrances across S04, S05, S06, S07, and S09.
- `Platform_Metal.prefab`: Mezzanine catwalk walkways aligned flush with stair tread heights.

---

## 4. VERIFICATION STATUS

1. **Unity Asset Database Integrity**: 349/349 valid `.meta` files verified by `tools/verify_repository.py`.
2. **C# Runtime & Editor Compilation**: Zero errors across `Hostfall.Runtime`, `Hostfall.Simulation`, and `Hostfall.Editor`.
3. **Scene Hierarchy**: All 18 doors assigned stable IDs (`door.xxx`), all 12 terminals assigned unique `DeviceId` values.
