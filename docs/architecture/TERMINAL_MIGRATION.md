## Current HOSTFALL 2.0 terminal route · 2026-11-20

The active route uses one physical desktop per station. See [ADR 0028](../adr/0028-field-investigations-and-service-utilities.md).

| Station | Current operation | Physical result |
| --- | --- | --- |
| WS-07 | VENTFLOW | Vent threat, power and service corridor access |
| CORE-01 | «Пустая камера» | Isolate the replay, disable shared tracking and open vault and energy routes |
| PWR-01 | Service power presets | Energize the gate and choose lighting and maintenance return or turret cover |
| QNT-01 | Local reference pulse | Open the existing field for twenty seconds |
| CARGO-01 | Freight run | Move cargo mechanisms, start physical lockdown |
| SEC-07 | Floor service | Disable floor sensors and permit archive access |
| NEURO-01 | «Второй голос» | Open Lee's safe for physical key pickup |
| RBT-01 | Recall and isolate utilities | Use real berth, shutters and presence sensors |
| DATA-01 | Optional reserve permit | Preserve access to the armory |
| EDGE-01, CLOUD-09, SUPER-10 | Local fiction and logs | Historical optional rewards persist in old saves |
| Home | Shell, «Знания», signed dossier | Power the shuttle and open the Core entrance |

The retired game sessions retain their readers for v3. New steps store explicitly
earned field progress, while validation reconstructs receipts from old and new
progress. A restored historical desktop is kept in its payload. The active scene
renders the current station console and does not mount those windows.

## Previous HOSTFALL 2.0 migration — 2026-11-17 / ADR0016

The new master request replaces active automatic solving with home-only virtual shell
and external interactive desktops. Legacy TerminalDirector / ScenarioSessions / 96-article
content remain preserved for compatibility, but the 2.0 preview has no old terminal
interactors. Unique pc.* IDs replace the duplicated HOME/WS identity. Home01 owns its
own VirtualComputer/TerminalMemory; new ComputerCampaign owns mini-game state,
idempotent receipts, archive messages and desired world actions. Save-v3 slots and
northline-2 are explicit and separate from v2. Shared physical DOM/RT/camera focus is
reused. Computer control mode runs the world while holding player movement/fire.

2026-11-20 · The campaign state supplies the short HUD goal and the detailed
briefing in **Задания**. Journal progress derives from existing computer,
world and finale state. Versioned save fields are unchanged. Home starts with
an empty transcript, and restoring a v3 slot drops the exact obsolete initial
banner while preserving subsequent output, command history and the draft.

The active Home guide uses five route-gated chapters (ADR 0024). **Знания**
shows earned archive records on the same Home session and marks them read
through ComputerCampaign, shared with the virtual shell. Retired chapter
selections migrate to the first chapter. New optional journal entries flag
the VENTFLOW and PAYLOAD records. The Recovery dossier and shuttle retain their
existing conditions before entry into the Core.

ADR 0025 removes the desktop location headers and on-screen exit controls.
Escape remains the computer exit input. The physical shell keeps keyboard focus
once its panel is interactive, and opening Knowledge gives focus to its search.
Returning to the shell restores focus to the current command without changing
the saved draft or history.

In the combined candidate, the Home guide opens from a separate button at the
upper right. Older Home message-tab saves reopen the shell. Archived evidence
remains in ComputerCampaign. Damage now releases computer focus. At EDGE and
CORE, the authored ambush hands control back for a timed counter or dodge before
the robot can strike. PAYLOAD completion activates the physical lockdown and
two finite response waves. The connected Play Mode check passes through these
systems and verifies the v3 epilogue restore. See
`docs/qa/HOSTFALL2_COMBINED_CANDIDATE_2026-11-19.md`.

Iteration1 accepted: 42 pure checks, full 90 Edit / 82 Play regressions, an additional
projected physical-pointer check, and native first-loop round trip passed. Production
promotion waits for the full new campaign. Iteration2 now adds a reusable app catalog,
XP PAYLOAD RUNNER with deterministic platform physics, and saved logistics mechanism
clocks. Current pure suite: 47 passing; full EditMode 95/95; affected computer/map/weapon
integration 16/16 after the broad run’s one coroutine-timing failure. Reviewed logistics
geometry and explicit moving map sources are in the source preview. See docs/hostfall/ITERATION_STATUS.md. Historical migration notes below are
not claims that the full new campaign is implemented.

NETWORK DEFENSE adds saved graph/packet/infection state and separate world radio,
outage and reserve clocks. The physical east-rack duct and actual CC bypass are in
the reviewed preview. 104 Edit / 28 affected Play checks pass; native/end-to-end
iteration2 acceptance remains open. See HOSTFALL2_NETWORK_2026-11-17.md.

CIRCUIT BREAKER now adds reciprocal wiring/source/relay snapshots, live power and
breaker state, saved louver/pulse clocks and one idempotent archive commission.
Power authority is explicitly restored onto cryo and the SOC maintenance-bypass
(gated by current wiring, not permanent unlock). Legacy doors keep previous rules.
62 pure / 111 Edit / 91 full Play pass. The repaired T junction is in the source
preview; production bundle and full iteration2 gate are unchanged.

SIGNAL SYNC saves the oscillator and equipment clocks in v3. App focus controls the
former, world time controls heat and the passage window. The physical gate and
north actuator restore from that state. 119 Edit and 23 affected Play checks passed.

MINEFIELD adds an optional v3 board mask, level and alarm clock. Cell IDs match the
real sensor floor. Final receipts open the CCTV and neuroarchive shutters. Existing
v3 saves without the board start at its first layout.

# Terminal migration

2026-11-17 · ADR 0015: NearbyTerminalDisplay сохраняет один DOM/RenderTexture при
подходе, фокусе и выходе. Native glass использует ту же текстуру, не второй текстовый
экран. Владельцы памяти/награды и схема world-v2 прежние. Вступление только для новой
игры; загруженный слот его не повторяет. Новый door.soc.corridor использует существующие
DoorStateEntryData, старым слотам новая запись не требуется. 77 Play / 80 Edit PASS.

2026-11-15 · ADR 0014: физические экраны и плавный фокус вместо отдельного окна.
Первые три сеанса остаются у TerminalDirector; расширенные доверенные сценарии —
Simulation.Screens с bounded TerminalMemory, сохранением typed/wait и награды.
Сценарии используют реальные DeviceId. В world-v2 необязательные screenSessions и
disabledWorklights. Исправлена загрузка кампании после третьей цели и старых JSON
без weapons. 80 Edit / 75 Play PASS, включая изолированную загрузку реального backup.
Инцидент с native QA auto и восстановление DATA-01 описаны в workstation QA.

2026-11-15: ADR 0012 добавляет optional `simulation.weapons` и `weapon.carryVersion=1`
в прежний world-v2. Simulation хранит IDs/боезапас брошенного оружия и подборы;
Unity синхронизирует физическую позу перед Capture. Старые слоты без этих полей
принимаются, прежние дополнительные пушки становятся предметами рядом с игроком.
Нагрев, cooldown, перезарядка и заряд восстанавливаются. Автоматические сеансы,
их DeviceId, прогресс и правила ADR 0007 не меняются. 69 Play / 69 Edit PASS.


2026-11-14: адресный ремонт геометрии S04–S09 и границ мира не меняет автоматические
терминалы, DeviceId или схему сохранений. 50/50 Play Mode и 63/63 Edit Mode PASS.
Инструменты редактора/Blender и окружение — ADR 0009;
проверки: `docs/qa/WORLD_WORKBENCH_2026-11-14.md`.


2026-11-13: ADR 0008 добавляет необязательное simulation.exploration в world-v2.
Состояние посещённых помещений принадлежит FacilitySimulation, геометрический
адаптер читает текущую сцену. ControlMode.Map приостанавливает мир и забирает ввод.
Это не возврат ручного терминала и не изменение маршрута ADR 0007.

2026-11-13: UI автоматических сценариев использует их CompletionNotice, вместо
безусловного «Доступ открыт». Сценарии `_unmet` не заменяют основной сценарий
устройства в каталоге и не завершают миссию. Для S02 проверена физическая цепочка
картридж → считыватель → DATA-01 → энергоцентр/оружейная. См. QA COMPUTE_VAULT_2026-11-13.

13 ноября: геометрические проходы между секторами исправлены без изменения условий
терминальной разблокировки. Отчёт: `docs/qa/MAP_REPAIR_2026-11-13.md`.

Эталон сохранён в `legacy/terminal-v3/src/horror`, включая Worker и исходные runtime tests.
Unity не исполняет TS/React и не подключает браузерный UI.

| Функция | Unity slice | Эталон v3 |
| --- | --- | --- |
| WS-07 / EDGE-01 / CORE-01 | Раздельные модели, FS/cwd/process/power, физические терминалы | Полностью сохранено |
| help/pwd/ls/cd/cat/ps/hostname/clear/exit | Реальные данные модели, ошибки/аргументы | Сохранено |
| ssh | Переключение virtual host; физическое питание EDGE влияет на доступ | Более сложные mission gates |
| kill | Удаляет процесс; cache-sync действительно перезапускается | Сохранено |
| service manager | cache-sync: status, disable (автозапуск), stop (штатная остановка), Restart=always | Старый crontab/turn scheduler сохранён |
| quotes/path normalization | Простые кавычки, .., ., ~, host-local cwd | Полный lexer |
| pipes/redirects/env/control operators | Явная ошибка unsupported | Сохранено с тестами |
| JS editor/runner | Не перенесён | Worker sandbox сохранён |
| HTTP/firewall/routes/patch panel | Не перенесены | Сохранено |
| outcomes/convergence/care consequences | Только локальное сдерживание нового среза | Все финалы сохранены |
| persistence | Desktop world-v2: FS/cwd/power/processes/служба/PID/миссия, знания, поза и прочность игрока; полное состояние расширенных секторов пока не сохраняется | localStorage v3 |

`ps` на WS обнаруживает источник. Отключение автозапуска + штатная остановка службы изолируют источник и разрешают шлюз,
`ps` на CORE подтверждает сдерживание. Цель обновляется из модели, UI не завершает миссию.
Симуляция не обращается к реальному shell, файловой системе, сети ОС или eval.
`ssh` — внутриигровая команда. Смена питания не уничтожает данные устройства.

Интерфейс, справка и ответы команд локализованы на русский. Имена команд,
виртуальные пути, hostnames и процессы сохраняют техническое написание. HUD показывает
одну карточку текущего задания справа сверху; клавиши перечислены на отдельной странице управления.
Noto Sans для чтения и Roboto Mono для команд/дисплеев включены с OFL и кириллицей.
Дисплеи в мире и интерактивное окно используют один TerminalDisplay и память сеанса;
физический экран не содержит отдельной копии ОС.

2026-11-07: память терминала привязана к устройству: transcript/history/draft/scroll/remote
host. Reopen сохраняет сеанс, другой компьютер имеет отдельную память. Она включена в
world-v2 как необязательное поле для совместимости. Старое сохранение переносит последний
сохранённый вывод; более ранний уже утраченный transcript старой версии восстановить нельзя.
F5 — отдельный quick-slot, загрузка — список auto/quick/3 ручных сохранений.


Умеренный художественный проход 2026-11-07 не меняет команды/модель/формат save.
После пересборки проверены 4 PlayMode сценария и совпадение simulation/field/door
с пользовательским backup при native загрузке/F5; transcript остаётся частью модели.

## Единый сеанс — 2026-11-07

TerminalSession.Execute записывает prompt+command, result/error и history в TerminalMemory.
UI посылает команду и отображает модель; дополнительной записи из UI нет. Host — поле
памяти физического origin, поэтому новые экземпляры session видят тот же remote host.
TerminalDisplay создаёт заголовок/prompt и общий transcript. На маленьком дисплее виден
хвост из 12 перенесённых строк; окно даёт прокрутку всей сохранённой истории. Это разные
размеры представления одного сеанса, без независимого fallback по LastCommand/процессам.

Инициализация добавляет общий снимок PID, не вызывая ps/mission observation. Старый
инициализированный сеанс не заменяется новым снимком. clear явно очищает обе поверхности;
повторный вход не добавляет приветствие обратно. Draft уведомляет модель; Revision
отслеживает изменение transcript для уже открытого окна. Power/link ошибки проходят
через тот же путь записи. Legacy LastCommand/ScreenOutput оставлены только для миграции.

Регрессии: 22 чистых C#; 38 EditMode; 9 PlayMode. TerminalSyncTests проверяет окно и
реальный ComputerScreen, ssh/exit, потерю связи, внешний execute, draft, clear и restore.
Нативный pwd/закрытие/reopen также выполнены. Остальная таблица ограничений выше актуальна.

## Введение и языковая библиотека — 2026-11-07

Автоматическое «help — команды с объяснениями» убрано из приветствия и мигрируется из
старой muted-строки. Явно введённый help остаётся рабочим; стартовое полноэкранное
введение объясняет его один раз на кампанию. Текст ошибок больше не содержит постоянной
рекламы help. Transcript/draft/remote host продолжают принадлежать общему сеансу.

Загрузка находится в главном меню, которое отделено от паузы. Язык C++/Python и
introductionSeen входят в field progress, восстановление слота не подменяет их настройкой
новой игры. Это ветки учебного содержания; к выполнению команд nlsh новые языки не
подключены. JS из активных знаний удалён, сохранённый TS-reference остаётся историческим.

## Смена парадигмы — 2026-11-08

ADR 0007 имеет приоритет над прежним ручным циклом выше. В активном UI больше нет
командного TextField/Submit/истории стрелками. TerminalSession стал внутренним исполнителем
фиксированных доверенных TerminalDirector-последовательностей. Постепенный draft,
команда и ответ принадлежат тому же VirtualComputer и общему TerminalDisplay.
Команды запускаются автоматически при показе; произвольных строк от игрока нет.

MissionDefinition.ActionRoute/IncidentMissionAsset требуют завершения физического узла:
WS → EDGE → CORE. Событие ps больше не завершает новую цель. Старый NightShift оставлен
для регрессий внутренней симуляции; это не доступный игровой режим. ServerAccess нового
маршрута зависит от завершения EDGE. Автономный WATCHER переживает потерю сетевого канала.

Сохранены старые состояния и терминальные истории. routeVersion=2 сериализует RoutineData
и ActionStateData; сохранения прежнего среза мигрируют без переисполнения команд. Новый
вводный экран с Q/F заменяет старый один раз. Чтение знаний остаётся дополнительным.
Сведения о реальном Linux в архиве не означают ручного или настоящего ОС-терминала в игре.


## 2026-11-14 — Combat adapter and persistence

ADR 0010 adds Simulation.Combat for patrol/guardian damage, modules, pose and AI
budgets, and an optional weapon loadout save. Existing terminal sessions, physical
screens and automatic commands remain under ADR 0007. No manual command field was
reintroduced. Source-backed Blender weapons/guardian are presentation assets, not
an OS/runtime migration. Existing saves default missing combat/loadout fields safely.
Validation: 56 Play / 67 Edit / 33 Simulation tests passed; native build and bounded
standalone smoke passed. See COMBAT_WORKSHOP_2026-11-14.md for exact scope/limits.


## 2026-11-14 — Weapon Atelier presentation

ADR 0011 replaces weapon hero assets and stand presentation, adds semantic
mechanical/grip sockets and poses the existing real body hands. It does not add
manual terminal input, a language runtime or a new simulation/backend layer.
Ownership/ammunition retain the optional loadout save contract from ADR 0010.
Validated: 59 Play, 67 Edit; native Arsenal build/signature/bounded smoke passed.
See docs/qa/WEAPON_ATELIER_2026-11-14.md for visual evidence and scope.


Collision audit follow-up (2026-11-15): tightened seven weapon display and eight
fixture proxies, made the existing S10 glass solid and closed two visible room shoulders.
No changes to simulation, terminal sessions, IDs or persistence contracts. Physical
regression suite: 63/63 Play Mode, 67/67 Edit Mode; QA: COLLISION_AUDIT_2026-11-15.md.


## 2026-11-15 — robot armour and bounded work (ADR 0013)

Native release: `apps/unity/Builds/macOS/HOSTFALL-Robots.app`. 72/72 Play, 69/69 Edit,
33/33 simulation, API and 614 GUID integrity PASS; signature and 12s native headless
smoke PASS. Automatic terminals and world/save contracts remain unchanged.
13 patrol bodies received surface-mounted armour; fixed guardian geometry is grouped
without removing damage components. Shared A* budget and non-alloc queries replace
simultaneous synchronous searches. GPU candidates were tested and rejected; production
uses conventional URP with original quality settings. Native engine-interval comparisons,
failed experiments and scope limits: `docs/qa/ROBOT_ARMOR_PERFORMANCE_2026-11-15.md`.

DIGITAL DETECTIVE adds optional v3 evidence, verdict, key and playback state.
The saved case opens its safe on binding. Only the collected key owns the vault
unlock receipt. Earlier slots begin with an untouched investigation.

CONTAINMENT adds optional v3 route, shutter, camera and clearance state. The
case key permits startup, and the saved clearance permits Central Ring entry.
A ring-side evacuation actuator remains available to older save positions.

BUFFER RECALL adds optional v3 pulse and answer state with a one-use supply
claim. Existing slots start with a closed buffer unless its new receipt exists.

FRAGMENT DOCK persists its placements, selected fragment and orientation as
optional v3 state. The previous cartridge/command gate remains legacy-only.

QUORUM DISPATCH adds optional v3 schedule and unlock state. Witness permission,
concurrency and timing validate on restore. The sole earned receipt opens the
existing west logistics bypass.

INTEGRITY AUDIT adds optional v3 per-bank checks, guesses gated on a single
remaining sector and an idempotent telemetry receipt. The receipt restores the
three physical service markers.

Recovery adds optional v3 stages, evidence reads and the service shuttle.
The three stages validate against immutable evidence published from eight
mandatory receipts. The shuttle stores destination, elapsed time and passenger
state. Saves below the facility floor require an authorized transit and a player
position consistent with its sampled cab pose. Older v3 slots start with a
dormant shuttle. No changes to the v2 reader or 1.x save directory are required.

Fresh PAYLOAD RUNNER sessions require all three SIGNAL SYNC calibrations.
Existing v3 sessions that have already started can resume via their saved
phase and stage. No additional save flag is needed.

VENTFLOW's optional archive adds four station-specific document IDs to the v3
desktop validator. The current document and window are saved in the existing
desktop state. The two letters, photograph and hidden record do not award
progression.
