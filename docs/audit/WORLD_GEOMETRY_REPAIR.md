# HOSTFALL — CRITICAL WORLD GEOMETRY & TRAVERSAL AUDIT & REPAIR REPORT

**Date:** 11 November 2026 • **Unity Version:** 6000.6.8f1 (URP)  
**Status:** COMPLETED & VERIFIED (P0–P3)  
**Target Scene:** `Assets/_HOSTFALL/Content/Scenes/NorthlineFacility.unity`  
**API Status:** 100% Pass (0 Errors) • **Simulation Tests:** 33/33 Pass • **Asset GUIDs:** 349/349 Intact  

---

## 1. СВОДКА ИСПРАВЛЕНИЯ КРИТИЧЕСКИХ ДЕФЕКТОВ

| ID | Сектор | Координаты | Классификация | Причина и дефект | Внесённое исправление | Способ проверки | Статус |
|---|---|---|---|---|---|---|---|
| **DEF-01** | Global / ModularKit | Вся карта | **BLOCKER / COLLISION** | В `ModularKit.Place()` при параметре `solid=true` на корневой объект лестниц `Platform_Stairs_4` и `Platform_Stairs_2` навешивался `BoxCollider` без поворота, блокирующий вход на ступени сплошной невидимой стеной. | В `ModularKit.cs` добавлено исключение: `!name.Contains("Stairs")` и проверка `go.GetComponentInChildren<Collider>()==null`. Ступени используют нативные коллайдеры мешей. | API build, Scene audit, CharacterController traversal check | **RESOLVED** |
| **DEF-02** | S02 (Compute Vault) | `(-4.6, 0, 20.6)` | **BLOCKER / REACHABILITY** | Серверный монолит `BuildDatasetMonolith` в `(-4.8, 0, 19.5)` имел габариты $2.6 \times 4.6 \times 2.4$ м и физически перекрывал западный лестничный марш мезонина `(-4.6, 0, 20.6)`. | Монолит перемещён в северную серверную линейку `(-4.8, 0, 35.5)`, полностью освободив лестничный марш и площадку входа. | Bounds intersection check, Scene rebake verification | **RESOLVED** |
| **DEF-03** | S04 (Quantum Nexus) | `(±2.05, 0.125, 41.0)` и `(±2.05, 0.125, 59.0)` | **BLOCKER / REACHABILITY** | Непрерывные ограждения моста `Platform_Rails_4Wide` ($L=4.0$ м) при $Z=40$ и $Z=60$ блокировали выход с лестниц траншеи на настил моста. | Перила сегментированы: оставлены проёмы шириной 1.8 м на площадках $Z = 41.0$ (юг) и $Z = 59.0$ (север). | Raycast sweep, Scene inspection | **RESOLVED** |
| **DEF-04** | S05 (Logistics) | `(8.5, 0, 72.0)` | **OVERLAP / COLLISION** | Плита мезонина `Mezzanine Slab` ($10.0 \times 1.0 \times 14.0$ м) покрывала лестницы `(-4.5, 0, ±5.5)`, утапливая ступени внутрь монолита. | Ширина плиты скорректирована до $8.8$ м с локальным центром `(0.6, 0.5, 0)` ($X \in [-3.8, +5.0]$). Ступени примыкают к кромке заподлицо. | Geometry audit, Bounds check | **RESOLVED** |
| **DEF-05** | S06 (Robotics Plant) | `(20.5, 0, 91.5)` и `(46.5, 0, 91.5)` | **VISUAL / REACHABILITY** | Настил смотрового мостика `Catwalk Slab` шириной 28.0 м нависал над лестничными пролётами с $X = 19.5$ до $22.75$ и $X = 44.25$ до $47.5$, блокируя проход игрока по высоте. | Ширина настила сокращена до $21.5$ м ($X \in [22.75, 44.25]$), строго между верхними площадками обеих лестниц. | Vertical clearance raycast ($H > 2.2$ м) | **RESOLVED** |
| **DEF-06** | S07 (Security Citadel) | `(38.0, 0, 22.0)` | **OVERLAP / REACHABILITY** | Настил галереи первого яруса `Catwalk T1 Slab` начинался на $Z = 22.0$, нависая над ступенями марша, восходящего с $Z = 20.0$ до $Z = 24.0$. | Глубина плиты скорректирована до $24.0$ м с центром `(0, 0, 1.0)` ($Z \in [24.0, 48.0]$). Верхняя ступень лестницы выходит встык с настилом. | Headroom raycast check | **RESOLVED** |
| **DEF-07** | S09 (Cloud Control) | `(20.0, 0, 118.0)` | **BLOCKER / REACHABILITY** | Сплошное 26-метровое ограждение вдоль южного края диспетчерского мезонина ($Z = 118.1$) не имело разрыва в точке выхода лестницы ($X = 20.0$). Настил не доходил до лестницы. | Настил мезонина расширен до $X = 19.0$ ($W = 32.0$ м). Южные перила смещены ($X \in [22.0, 50.0]$), обеспечив чистый проход шириной 2.5 м. | Traversal path query | **RESOLVED** |
| **DEF-08** | S07 & S08 | `(0, 3.85, 0.24)` | **OVERLAP / VISUAL** | В `CreateSignboard()` использовались мировые координаты вместо локальных родителя, вызывая наложение вывесок ворот Сектора 07 и Хранилища. | Вывески позиционируются строго в локальном пространстве родительских групп порталов. | Visual inspector, Zero duplicates check | **RESOLVED** |
| **DEF-09** | World / AI Enemy Movement | Вся карта | **CRITICAL / COLLISION** | Роботы `BulwarkSentry`, `BastionSentry`, `TrackedSentry`, `TraceHound`, `JammerRobot`, `LensDrone` двигались прямым сложением `transform.position += ...` без физических проверок. | Внедрён метод `PerformMovement()` с `Physics.CapsuleCast` / `SphereCast` по `CollisionLayers.WorldMask` со скольжением вдоль препятствий. | Collision matrix sweep, Unit/Behavioral checks | **RESOLVED** |
| **DEF-10** | Devices / State Persistence | Вся карта | **CRITICAL / PERSISTENCE** | `DataCartridgeState`, `CoolingValveState`, `CargoSwitchyardState`, `UpsPowerState` жили в статических классах, сохранялись между New Game и терялись при загрузке. | Создана структура `DeviceMechanicStateData`, интегрированная в `GameSaveData`. `WorldSaveController` сохраняет/восстанавливает все состояния; `FacilityRoot` сбрасывает их при `FreshStartRequested`. | Serialization roundtrip test, SaveTests suite | **RESOLVED** |
| **DEF-11** | Simulation / DeviceId | Вся карта | **SYSTEMIC / INTEGRATION** | `DeviceId` имел лишь 6 значений; терминалы `CARGO-WIN`, `RBT-01`, `SEC-07`, `NEURO-MAC`, `CLOUD-09`, `SUPER-CORE` переиспользовали `Core01` и `Ws07`. | `DeviceId` расширен: `Cargo01=401`, `Rbt01=501`, `Sec07=601`, `Neuro01=701`, `Cloud09=801`, `Super10=901`. Все 12 терминалов имеют уникальные ID. | Enum reflection, Hostname parsing test | **RESOLVED** |
| **DEF-12** | World / Door Persistence | Вся карта | **SYSTEMIC / PERSISTENCE** | В `GameSaveData` сохранялась лишь одна дверь (`door`), остальные 17 дверей комплекса сбрасывались в запертое состояние при загрузке. | Введена структура `DoorStateEntryData[] doors`. Все 18 дверей сохраняют своё состояние смещения, открытости и ручной разблокировки по `doorId`. | Multi-door save/load test in `WorldSaveTests.cs` | **RESOLVED** |
| **DEF-13** | S02 (Armory Door) | `(6.78, 0, 26.0)` | **DESIGN / PROGRESSION** | Дверь оружейного отсека была настроена как `AlwaysUnlocked`, ломая прогрессию и выдавая дробовик без решения задачи терминала `DATA-01`. | Дверь переведена в `ManualUnlockOnly` с требованием токена `KEY-SEC02-ARMORY`. `DATA-01` открывает её через `AddTargetDoor(armoryDoor)`. | Access gate check in PlayMode | **RESOLVED** |
| **DEF-14** | S06 (RBT-01 Terminal) | `(33.5, 4.3, 92.3)` | **DESIGN / CAUSALITY** | Терминал RBT-01 имел `targetDoor = null` и не оказывал физического влияния на комплекс. | `RBT-01` подключён к гермошлюзу `sec07Door` (`door.sec07.citadel`) и генерирует событие перевода боевых роботов в режим ожидания (`EVT_RBT01_DEFENSE_STANDBY`). | Interactive event dispatch verification | **RESOLVED** |

---

## 2. ИНТЕГРАЦИЯ 18 ШЛЮЗОВ И ДВЕРЕЙ

Все 18 шлюзов комплекса NORTHLINE зарегистрированы и сериализуются по каноническим строковым идентификаторам:

1. `door.corridor.server` — Шлюз коридора серверной (стартовый сектор S01)
2. `door.shelter.blast` — Гермошлюз инженерного убежища S00
3. `door.sec01.exec` — Служебный обходной шлюз в SOC
4. `door.sec02.vault` — Защитные ворота вычислительного узла S02 (открываются после `CORE-01`)
5. `door.sec02.armory` — Гермозатвор оружейного отсека S02 (открывается токеном из `DATA-01`)
6. `door.sec03.energy` — Западный шлюз в энергоцентр S03 (открывается через `DATA-01`)
7. `door.sec04.cryo` — Криогенный портал перехода S02 -> S04 (открывается через `PWR-01`)
8. `door.sec05.logistics` — Грузовой шлюз перехода S04 -> S05 (открывается через `QNT-01`)
9. `door.sec06.robo` — Шлюз завода робототехники S05 -> S06 (открывается через `CARGO-WIN`)
10. `door.sec07.citadel` — Портал в Цитадель безопасности S06 -> S07 (открывается через `RBT-01`)
11. `door.sec07.soc` — Служебный проход безопасности из S07 в SOC (открывается через `SEC-07`)
12. `door.sec08.neuro` — Входной портал в Нейроархив S07 -> S08
13. `door.sec08.memvault` — Гермозатвор глубокого хранилища памяти S08 (открывается через `NEURO-MAC`)
14. `door.sec09.cloud` — Портал перехода в Диспетчерскую кластера S08 -> S09
15. `door.loop5.shortcut` — Служебный люк обратной закольцовки S09 -> S05
16. `door.sec10.super` — Массивный шлюз перехода в Машинный амфитеатр ядра S09 -> S10 (открывается через `CLOUD-09`)
17. `door.sec10.trust` — Шлюз сердцевины доверия в амфитеатре S10
18. `door.sec10.evac` — Аварийный шлюз смотровой площадки эвакуации S10 (открывается после аннулирования автономии SUPER на `SUPER-CORE`)

---

## 3. СИСТЕМА ФИЗИЧЕСКИХ НОСИТЕЛЕЙ (ФАЗА E)

Создан автономный компонент `PhysicalMediaItem` и статический реестр `PhysicalMediaInventory`:
1. `item.usb.hf_tools`: Физический USB-накопитель в защищённом корпусе с индикатором на рабочем столе HOME-PC в убежище инженера (`-2.6, 0.84, -16.42`).
2. `item.cartridge.nl044`: Монолитный картридж эвакуации данных на консоли мезонина Сектора 02 (`4.6, 4.15, 35.6`).
3. `item.key.rbt_diag`: Физический крипто-брелок диагностики робототехники на инструментальном верстаке (`2.55, 0.92, -16.6`).

---

## 4. РЕЗУЛЬТАТЫ ВЕРИФИКАЦИИ

- **API Compilation**: `python3 tools/check-unity-api.py` -> 0 Errors, 1 Warning (obsolete core Clipper warning).
- **Repository Integrity**: `python3 tools/verify_repository.py` -> 349/349 Asset GUIDs valid.
- **Simulation Invariants**: `tools/test-simulation.sh` -> 33 Passed, 0 Failed.
- **C++ Native Core**: `ctest` -> 100% Passed.
- **Scene Serialization**: `NorthlineFacility.unity` пересобрана со всеми новыми коллайдерами, идентификаторами и связями.
