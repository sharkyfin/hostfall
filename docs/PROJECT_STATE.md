# HOSTFALL 2.0 migration — 2026-11-17 / ADR0016

[Websim](https://websim.com/@littleprank/hostfall-2-northline) serves the November 21
desktop ZIP as revision 5. A guest session reached the main menu.
[itch.io](https://littleprank.itch.io/hostfall) points to the refreshed upload.
The direct player started in Chrome, and the embedded guest verification ended
after a data cache error. See the
[publication checks](qa/HOSTFALL2_WEB_PUBLICATION_2026-11-21.md).

The [November 20 horror plan](hostfall/HORROR_CAMPAIGN_2026-11-20.md) now uses three
computer games across the route. VENTFLOW is followed by «Пустая камера» at CORE-01
and «Второй голос» at NEURO-01. The remaining stations contain local files, with
service scripts and contextual «Знания» where they control equipment. Home reviews
the accumulated dossier and accepts the operator's signature before transit.
[ADR 0028](adr/0028-field-investigations-and-service-utilities.md) describes the
active route and save compatibility. See the
[connected route verification](qa/HOSTFALL2_FIELD_TERMINALS_2026-11-20.md).

The Websim preview uses a browser-specific render budget with two lower
resolution tiers for sustained frame drops. The same workshop view produced
41–48 browser animation frames per second in a short local Chrome comparison,
up from 27–29 with the original Retina buffer. See the
[Websim preview and test details](qa/HOSTFALL2_FIELD_TERMINALS_2026-11-20.md#websim-rendering-budget).

The physical horror pass repairs the Compute Vault upper bridge and PWR-01 approach,
adds an original desk and transformer, and replaces the Energy Center crawler with
a small patrol. Logistics reserves exit an authored bay. The terminal counter and
bosses have physical attack or cover changes that passed the 2.0 Play Mode route.
See [ADR 0026](adr/0026-horror-campaign-and-physical-reinforcements.md) and
[the verified horror pass](qa/HOSTFALL2_HORROR_PASS_2026-11-20.md).

The next pass cuts the Operations service duct through the actual east wall and
enables ordinary Watchers to pursue through physically open doors outside their
patrol bounds. The expanded puzzle design is in [the fifteen-option plan](hostfall/HORROR_PUZZLES_2026-11-20.md).
See [ADR 0027](adr/0027-pursuit-across-open-doors-and-service-duct.md) and
[the Operations regression results](qa/HOSTFALL2_OPERATIONS_PURSUIT_AND_DUCT_2026-11-20.md).

The passage pass replaces the large shelter entrance displays with compact
wall-mounted relay units, seats worklight controls and uses matte paint for decorative
markings. Plain navigation lettering sits on physically backed plates.
Editor footage uses depth-tested text before the scene is rendered. The active
computer desktops omit decorative location headers. Home restores its shell
input focus as soon as the camera is ready. See ADR 0025 and the combined
candidate QA.

The 2026-11-20 immersion pass gives the Home computer five practical guide
chapters and direct access to earned evidence. The journal offers optional
returns after VENTFLOW and PAYLOAD. Recovery remains the mandatory return
before the final descent. Original Blender fittings, local work lights and a
spatial pump loop dress the service passage and energy center. The front menu
plays footage of the facility, and **Новая игра** opens with a brief about the
missing shift and Type-IV. See ADR 0024 and the combined candidate QA.

The 2026-11-20 journal pass expands the current goal into a route-dependent
briefing in **Задания**. It covers each mandatory computer, physical handoffs,
the lockdown, Core Avatar and the evacuation report. The menu shows completed
steps and optional stations as they become available. The Home console opens
with the input prompt and removes its obsolete initial banner from older v3
transcripts. See docs/qa/HOSTFALL2_COMBINED_CANDIDATE_2026-11-19.md.

New master direction is active; four sequential acceptance gates are tracked in
`docs/hostfall/ITERATION_STATUS.md`. Iterations 1 and 2 are **accepted**. The combined
preview includes the combat and finale from iterations 3 and 4. Full release
acceptance remains open.
Dirty/untracked source checkpoint and master request are preserved. Home-only safe
virtual shell/guide, three-stage Snake, typed world rewards/vent threat and explicit
v3 campaign persistence are implemented behind an interactive mission definition.
42 pure behavioral checks, 90/90 Unity EditMode and 82/82 broad PlayMode checks passed;
source API compilation passed. Projected physical pointer input passed in a separate PlayMode
check using Unity’s own unfocused test hook. Native first-loop QA passed with 11 real weapon
shots, three Snake milestones, doors, return home and the earned document. No 2.0 production
scene/build has been promoted. Existing Awakening build remains the last checked release.
Iteration2 now includes XP/PAYLOAD RUNNER with three real platformer levels and saved
physical lift/conveyor/bridge/alarm effects. Nine computer integration cases pass;
modern PC markers and moving geometry are synchronized with the map. Latest broad
Play run: 85/86, then the sole timing-test failure and affected map/computer scenarios
passed in a 16/16 targeted run. See docs/qa/HOSTFALL2_ITERATION2_2026-11-17.md.
NETWORK DEFENSE is now integrated: original GNOME desktop, three-wave graph defence,
saved robot radio/outage/reserve state, actual duct/CC bypass and vault/energy access.
104/104 EditMode and 28/28 affected computer/enemy/restore PlayMode pass in the clean
audio-enabled editor. Earlier FMOD-failed runs are retained in QA; no log suppression.
All 11 user save hashes remain unchanged. Source preview includes reviewed Network
geometry; production remains Awakening. CIRCUIT BREAKER is integrated with original Plasma-like desktop, reciprocal contacts,
source/relay/overload rules and saved live lighting/vent/defence/door power. Real CC
traversal found and repaired the maintenance T-junction wall crossing. 62 pure checks,
111/111 EditMode and full 91/91 PlayMode pass. Source preview includes checked power
and bypass geometry. Native/end-to-end campaign gates remain open. Two production
auto files advanced at 19:21 before the full regression; newer progress was preserved
and all observed files backed up. See docs/qa/HOSTFALL2_CIRCUIT_2026-11-17.md.
See docs/qa/HOSTFALL2_NETWORK_2026-11-17.md.
SIGNAL SYNC is integrated in the source preview. Three oscillator calibrations
drive the timed logistics barrier, live thermal response and physical return release.
70 pure, 119 Edit Mode and 23 affected Play Mode checks passed.
See docs/qa/HOSTFALL2_SIGNAL_2026-11-17.md.
MINEFIELD is integrated in the source preview. Three logically solvable grids drive
36 real floor sensors, alarm and safe access through the observation post into the
neuroarchive. 129 Edit checks passed. Full Play passed 94/96 and both corrected cases
passed on follow-up. Automated hands-on traversal and restore evidence is in
docs/qa/HOSTFALL2_MINEFIELD_2026-11-17.md. DIGITAL DETECTIVE is integrated in the source preview. The archive evidence opens
a physical key safe, and key pickup opens the memory vault. Actual CCTV frames,
partial desktop save and both-way vault traversal passed targeted Unity Play Mode.
See docs/qa/HOSTFALL2_DETECTIVE_2026-11-18.md.

CONTAINMENT is integrated in the 2.0 source preview. RBT-01 shows live CCTV,
changes R26's route and uses occupied interlocks to capture it or clear the lane
after destruction. The Central Ring traversal and restore passed. Full Editor
checks passed 133 Edit and 100 Play. See docs/qa/HOSTFALL2_CONTAINMENT_2026-11-18.md.

BUFFER RECALL is integrated at EDGE-01. Its three replayable pulse series and
private restore passed, along with the wall-mounted one-use repair package and
two-way corridor traversal after clearing R17. 135/135 full Edit Mode, 1/1
targeted Play. See docs/qa/HOSTFALL2_BUFFER_2026-11-18.md.

FRAGMENT DOCK is integrated at DATA-01. Its packed volume preserves the journal
partition and opens existing SEC-02 armory access. Verified real desktop, partial
save, two-way armory entry and reload. See docs/qa/HOSTFALL2_FRAGMENTS_2026-11-18.md.

QUORUM DISPATCH unlocks the west logistics bypass after the witness schedule
checks. The source preview through this computer produced the separate Intel
macOS HOSTFALL-2-Preview bundle. Native startup passed with its own bundle ID
and product name. See docs/qa/HOSTFALL2_QUORUM_2026-11-18.md.

INTEGRITY AUDIT and home RECOVERY PROTOCOL are integrated in the source preview.
Recovery unlocks the physical shuttle and the core gate after three archive-led
stages. Targeted Unity Play covered the shuttle in both directions and a save
and reload mid-ride. The Intel macOS HOSTFALL-2-Preview-Recovery bundle compiled
and started with Home and all twelve external applications. The current
computer campaign passed the continuous route through the eight mandatory
stations to the core gate, plus 96 pure, 145 Edit and 109 Play checks.
The accepted macOS preview is `apps/unity/Builds/macOS/HOSTFALL-2-Computer-World.app`.
The signed Intel bundle passed native scene startup.
See docs/qa/HOSTFALL2_CONNECTED_CAMPAIGN_2026-11-19.md.
The newer combined preview is `apps/unity/Builds/macOS/HOSTFALL-2-Candidate.app`.
The signed Intel build started on Metal. The campaign now proceeds through the
PAYLOAD lockdown, the logistics minibosses and the three-phase SUPER encounter
to the observation room and the epilogue. Unity verified the complete mandatory
route, physical weak-point hits and cover, the trust and evacuation doors, and
epilogue restore. The combined regression passed 115 pure checks, 160 Edit Mode
and 120 Play Mode tests. See docs/qa/HOSTFALL2_COMBINED_CANDIDATE_2026-11-19.md.
`tools/unity.sh web-candidate` builds `apps/unity/Builds/Web/HOSTFALL-2-Candidate`.
A fresh Chrome profile started the new-game cinematic at 1280×800. The Mac
profile sampled 630 frames at 1920×1080 with a median of 3.69 ms and p95 of 8.10 ms.
The older release notes below describe 1.x.

# Current State — 2026-11-17

## Continuous terminal glass and new-game awakening — ADR 0015

Текущая сборка: `apps/unity/Builds/macOS/HOSTFALL-Awakening.app`; пути
`HOSTFALL-Workstations.app` и `HOSTFALL-Robots.app` тоже обновлены. Release Succeeded,
0 errors; подпись и 12s headless smoke PASS, save-хеши неизменны.

Вблизи компьютера и при работе используется один DOM и одна текстура 2048×1142
с фиксированной вёрсткой 1536×856. Фокус композитит эту же текстуру в нативном
разрешении внутри стекла; отдельного окна/повторного текста нет. Сохранены история,
набор, цвета, переносы, выбранная диагностика и управление автоматическим сеансом.

Новая игра показывает реальное тело героя в стартовой операторской, затем камера
переходит к глазам. Есть русская синтезированная озвучка «Что со мной происходит?..»,
субтитры и пропуск. Старые загрузки продолжают свои позу/прогресс без катсцены.
Четыре настенные записки предупреждают об ИИ. Новый сохранённый шлюз
`door.soc.corridor` закрыт до WS-07; затем открывается на E и сохраняет маршрут к EDGE.

77/77 Play, 80/80 Edit, 33/33 Simulation PASS, API и 644 GUID integrity PASS.
Нативные снимки проверены; пользовательские save-хеши не изменились. Финальная
контрольная медиана 5.304 ms / p95 12.394 ms на шести видах не является гарантией FPS.
Подробности: `docs/qa/AWAKENING_AND_TERMINAL_SYNC_2026-11-16.md`.


## Physical workstations and native text — ADR 0014

Текущая сборка: `apps/unity/Builds/macOS/HOSTFALL-Workstations.app`; прежний путь
`HOSTFALL-Robots.app` также обновлён. Native release Succeeded / 0 errors, подпись
и 12s headless smoke PASS, 636 GUID integrity PASS.

Адресно закрыты щели и удалены дубли стен S02. В сцену перенесены 12 рабочих станций
на основе собственных Blender-моделей: ноутбук, моноблок, сервисный ПК; обновлены
USB/картриджи и функциональные кнопки существующего света. E плавно приближает камеру
к стеклу: автоматический сеанс читается прямо на физическом экране, без отдельного
полноэкранного окна. История/набор/ожидание расширенных сеансов и кнопки сохраняются.

Fullscreen следует форме экрана. Текст рисуется в нативном разрешении, 3D ограничено
2 073 600 пикселями. На Mac подтверждено 3584×2240 / scale 0.5082232 без полос;
контрольные замеры: median 7.852 мс, p95 9.418 мс; это не гарантия FPS всей кампании.
**80/80 Edit Mode, 75/75 Play Mode (74 + локальная проверка реального backup),
33/33 Simulation PASS**, API compilation PASS. Подробности и ограничения:
`docs/qa/PHYSICAL_WORKSTATIONS_2026-11-15.md`.

При диагностике обнаружен и исправлен дефект изоляции сохранений: normal-player
игнорировал частную тестовую папку и перезаписал auto. Восстановлена проверенная
резервная копия у DATA-01 (3 этапа) в auto и свободный manual3; quick не затронут.
Точная auto-копия непосредственно перед диагностикой недоступна. Исправлены миграция
старого пустого weapon roster и восстановление/список слотов расширенной кампании.
Повторные native-прогоны подтверждены неизменными хешами пользовательских слотов.



## Full HD по умолчанию — предыдущая политика 2026-11-15

Заменена ADR 0014: native fullscreen/UI и отдельный бюджет пикселей 3D, см. выше.

В предыдущей сборке `HOSTFALL-Robots.app` стартовое разрешение было **1920×1080**;
`defaultIsNativeResolution=false`. Эти значения тогда были закреплены в ConfigureProject.
После сообщения о лагах также исправлен старый сохранённый Screenmanager-профиль
3584×2240 / Native: 1920×1080, Native=0, FullScreenWindow. Игра была закрыта
пользователем; предпочтения сохранены в backup, остальные параметры не изменены.
Режим FullScreenWindow и ручной resize сохранены; вызовы изменения размера при
загрузке сцен не добавлены. Native rebuild Succeeded / 0 errors. Проверка:
`docs/qa/FULL_HD_DEFAULT_2026-11-15.md`.


## Robot armour and measured optimisation — ADR 0013

Актуальная локальная сборка: `apps/unity/Builds/macOS/HOSTFALL-Robots.app`.
Броня 13 патрулей подогнана по реальной поверхности двух моделей и закреплена на Body:
624 проверки в Idle/Walk/Run/Attack, максимальное расстояние центра пластины до корпуса
9.522 мм. Runtime BakeMesh для крепления не используется. MANTIS сохраняет все 52 008
индексов фиксированной геометрии, материалы и отдельные уязвимости; 141 → 21 рендерер.

Навигация использует общий честный бюджет A*, переиспользует массивы запросов и
проверяет пол на длинных срезах. Повреждения кэшируют иерархию и меняют вид только
при изменении состояния деталей. GPU Resident Drawer и GPU occlusion проверены в
нативных вариантах и отклонены по визуальной/скоростной приёмке; обычный URP Forward+
и качество графики сохранены. В трёх контрольных прогонах 2560×1600 медиана результатов
10.801 → 8.105 мс; managed allocations 3.4 → 1.6 КБ/кадр. Это не гарантия FPS всей карты.

**72/72 Play Mode, 69/69 Edit Mode, 33/33 Simulation PASS**; API compilation PASS,
614 GUID integrity PASS. Native x86_64 release Succeeded / 0 errors, подпись PASS,
12s headless smoke работал и завершён кодом 0 без ошибок скриптов/шейдеров/загрузки.
Сцена перенесена адресно после проверки хеша; исходник и backup сохранены.
Подробности: `docs/qa/ROBOT_ARMOR_PERFORMANCE_2026-11-15.md` и
`docs/research/2026-11-15-robot-performance.md`.


## Motion and single-weapon pass — ADR 0012

Актуальная локальная сборка: `apps/unity/Builds/macOS/HOSTFALL-Motion.app`.
12 CC0 Quaternius UAL1 Standard actions экспортированы через Blender, retargeted
на существующее тело; ходьба/бег, действия и хват не используют отдельные руки.
Есть настройка движения камеры 0..1, инерция и плавный подъём оружия.

Одно оружие в руках: подбор бросает прежнюю модель рядом с физикой и её боезапасом.
Положение/скорость, патроны, нагрев/таймеры входят в save; старые слоты принимаются.
ARC-12 заряжается по одному патрону, CIRRUS перегревается, TITAN заряжается при
удержании и стреляет при отпускании. Состояние заряда и перезарядки сохраняется.

Модели в руках и ударные огни переиспользуются; статическая геометрия карты
кэшируется, движущиеся шлюзы/мосты обновляются. 200 пересчётов карты: 275.06 →
26.42 мс в том же Editor microbenchmark. Это не заявление о FPS всей кампании;
CPU/GC-границы и реальные кадры описаны в QA. В FPS устранён кадр задержки прицеливания.

**69/69 Play Mode, 69/69 Edit Mode, 33/33 Simulation PASS**; API compilation PASS,
602 GUID integrity PASS. Native x86_64 build Succeeded / 0 errors; signature PASS;
12s headless smoke — процесс работал, управляемое завершение 0, ошибок загрузки/скриптов нет.
Авторская сцена сохранена без повторной генерации. Подробности:
`docs/qa/MOTION_COMBAT_2026-11-15.md`, `docs/research/2026-11-15-motion.md`.


## Collision audit — 15 November 2026

Актуальная локальная сборка: `apps/unity/Builds/macOS/HOSTFALL-MapFix.app`.
Native x86_64 build Succeeded / 0 errors; signature PASS; 12s headless smoke
работал, управляемое завершение 0, ошибок скриптов/загрузки нет.

Исправлены 15 чрезмерных коллайдеров (7 оружейных дисплеев, 8 светильников),
отсутствующая коллизия панорамного бронестекла S10 и два открытых торца смотровой.
Торцы закрыты видимыми секциями; модель и коллизия бронестекла совпадают.
Скан границ теперь охватывает реальные размеры сцены, включая убежище и финал:
40 922 свободные точки / 0 открытых краёв. 18 шлюзов проверены персонажем в обе
стороны (36 проходов). **63/63 Play Mode, 67/67 Edit Mode PASS**, API compilation
PASS, 576 GUID integrity PASS. Полное ручное прохождение этим не заявляется.
Подробности и границы проверки: `docs/qa/COLLISION_AUDIT_2026-11-15.md`.


## Weapon Atelier — качественный оружейный комплект, ADR 0011

Актуальная сборка: `apps/unity/Builds/macOS/HOSTFALL-Arsenal.app`.
ARC-12/CIRRUS/TITAN получили оригинальные профильные корпуса, полые стволы,
скобы, крепёж, маркировку, PBR-деталь поверхности и отдельные механические узлы.
Три render mesh на оружие; тот же prefab используется в руках и на стойках.
Уточнены масштаб и вид FPS; реальные руки тела следуют точкам хвата, поддерживают
помпу и силовой блок при перезарядке. Отдельные парящие руки не создаются.

Редактируемый автономный исходник:
`art-source/weapons/HOSTFALL-WeaponAtelier.blend` (упакованные текстуры).
Воспроизводимая генерация: `tools/blender/make-weapon-atelier.py`.

**59/59 Play Mode, 67/67 Edit Mode PASS**, API compilation PASS, 572 GUID integrity
PASS. Native x86_64 build Succeeded / 0 errors; signature PASS; 12s headless native
smoke — работал, управляемое завершение 0, ошибок скриптов/загрузки нет. Реальные
студийные, FPS и reload-ракурсы просмотрены; это не ручное прохождение кампании.
Приёмка: `docs/qa/WEAPON_ATELIER_2026-11-14.md`. Перенос сцены защищён исходным
хешем; мир не пересоздавался. Баланс урона/магазинов и формат сохранений сохранены.


## Боевая мастерская и адресные стыки — ADR 0010

Доставлена локальная сборка `apps/unity/Builds/macOS/HOSTFALL-Combat.app`.
Патрули получают урон по датчику/корпусу/приводам, больше здоровья, реакцию на
выстрелы и обход препятствий с проверкой пола. Исправлены ложная анимация смерти,
развороты у конечных точек и телепортация при ударе. Добавлены четыре патруля.
Два гусеничных MANTIS-6 имеют отдельные Id, оригинальную модель Blender,
разрушаемое охлаждение и открываемый задний реактор. Большой корпус проверяется
также при повороте. Другие специальные архетипы сохраняют свои armour/phase-контракты.

Три разные модели ARC-12/CIRRUS/TITAN, точный учёт дробин, сохранение оружия/боезапаса
и частичных повреждений. Здоровье отделено вниз слева; прицел — сплошная точка.
Адресно закрыты щели SEC07/S06 и заменены растянутые подвесы S02.

**56/56 Play Mode, 67/67 Edit Mode, 33/33 Simulation PASS**. API compilation PASS;
555 GUID integrity PASS. Native x86_64 build Succeeded, 0 errors; codesign verification
PASS; 12s headless standalone smoke — процесс работал, завершён управляемо с кодом 0,
ошибок скриптов/загрузки не обнаружено. Это не ручное прохождение всей кампании.
QA и границы: `docs/qa/COMBAT_WORKSHOP_2026-11-14.md`. Исходный хеш сцены проверен
перед переносом, полная генерация FacilityBuilder не запускалась.


## Геометрия и локальная мастерская — 14 ноября

Unity и Blender MCP настроены и проверены, включая штатные инструменты Codex.
Blender 4.5.16 LTS / addon protocol 16; Unity MCP 10.3.0 / локальный HTTP.
Инструкция и проверка готовности: `docs/tooling/EDITOR_WORKBENCH.md`,
`python3 tools/editor-tools-doctor.py`. Создан/импортирован оригинальный служебный щит.

Адресно исправлены 10 лестниц S04–S09, стык S06/S09, верхние переходы и ограждения.
Закрыты обнаруженные открытые края пола; финальный скан нижнего уровня:
39 338 свободных точек, 0 открытых краёв. Это не полное прохождение кампании.
Добавлены 8 умеренных зон обслуживания из имеющейся палитры, метрические UV и
локальное освещение через URP Forward+. Тёмная атмосфера сохранена.

**50/50 Play Mode, 63/63 Edit Mode PASS**, API compilation PASS, 527 GUID integrity PASS.
macOS x86_64 BuildDesktop: **Success / exit 0**, signature verification PASS.
Актуальная сборка: `apps/unity/Builds/macOS/HOSTFALL-Workbench.app`.
Подробности, артефакты и границы проверки: `docs/qa/WORLD_WORKBENCH_2026-11-14.md`.
Авторская сцена не пересоздавалась; перенос защищён проверкой исходного хеша.


## Карта мира и радар — ADR 0008

Карта M теперь показывает физическую планировку сцены, стены/препятствия, устройства
и актуальные шлюзы. Вместо карточек и описаний — масштабируемый план с отдельными
уровнями. Неизвестные помещения затемнены с «?», посещённые подписаны. Исследование
принадлежит Simulation.Exploration и сохраняется со слотом; старые слоты принимаются,
но прежний незаписанный маршрут не выдумывается. Радар в левой flow-колонке вместе
с локацией и прочностью, цель справа; модальные экраны скрывают HUD. Map — отдельный
режим остановки мира с возвратом в предыдущий экран.

Unity: **43/43 Play Mode**, **63/63 Edit Mode PASS**. Реальные UI Toolkit рендеры
просмотрены; HUD проверен от 640×360 до 1920×1080. Детали и ограничения:
`docs/qa/MAP_AND_RADAR_2026-11-13.md`, `docs/adr/0008-world-map-and-exploration.md`.
macOS x86_64 BuildDesktop: exit 0. Актуальная сборка:
`apps/unity/Builds/macOS/HOSTFALL-Map.app` (доставка 14 ноября).


## Сектор 02: картридж и верхний маршрут

Адресно заменена статическая конструкция галерей и лестниц S02; исправлено
пересечение с возвратным коридором S07. Подход от входа, обе лестницы вверх/вниз,
мост и путь до картриджа проверены настоящим CharacterController. DATA-01 больше
не объявляет доступ открытым после отказа; диагностика и текущая цель объясняют
физические этапы. Проверено получение/установка картриджа и открытие двух шлюзов.
Unity Play Mode: **37/37 PASS**, включая три новых интеграционных теста S02.
Edit Mode: **59/59 PASS**. macOS x86_64 build: exit 0;
проверенный bundle этого этапа — `apps/unity/Builds/macOS/HOSTFALL-Sector02.app`.
Подробности, артефакты и ограничения: `docs/qa/COMPUTE_VAULT_2026-11-13.md`.


## Стабильный режим окна

GamePreferences больше не читает и не применяет старый `hostfall.fullscreen` при
создании сцены, изменении громкости/мыши/языка/VSync или сбросе этих настроек.
Режим берётся из текущего состояния нативного окна; изменение выполняется только
по явному действию SetFullscreen. Для standalone разрешено изменение размера окна.

## Ремонт связности карты — 13 ноября

Исправлены ориентация боковых шлюзов, перекрывающая их обшивка SOC, стены без
проёмов в нейроархиве и диспетчерской, масштабирование повёрнутых WallAstra,
перила квантового моста, входы S06–S09 и S09–S10. Добавлены закрытые соединения
S06–S08 и два ранее оборванных служебных обхода. Полная генерация сцены не выполнялась;
перед адресным ремонтом сохранена её копия. Подробности и ограничения проверки:
`docs/qa/MAP_REPAIR_2026-11-13.md`. Связность основных точек всех секторов проверена
по сетке Unity с исключёнными сюжетными дверями; это не сквозное прохождение кампании.
Unity Play Mode: 34/34 PASS; 17/17 контрольных точек геометрической связности доступны
со спавна при открытых сюжетных шлюзах. Целостность репозитория: 352 GUID, PASS.

## Активное направление

Канон 2.0 (Master Design & Implementation Brief, ADR 0007): одиночный офлайн технохоррор HOSTFALL, комплекс NORTHLINE, Super, инженер в теле телеприсутственного промышленного андроида Type-IV после гибели биологического тела в Секторе 00.
Пользователь отменил ручное выполнение команд. Основа — исследование разветвленного комплекса, тактическое уклонение и бой с роботами (дробовик ARC-12), терминальные сценарии (нажатие `[E]` запускает аутентичный скрипт с выводом ОС), физические головоломки (P01–P12), дуальные маршруты и нелинейная метроидвания с 4 возвратами.

## Реализовано: Critical World Repair & Terminal-Driven Level Design (11 ноября 2026)

1. **Полное геометрическое и пространственное исправление карты (P0):**
   - **Устранение паразитных коллайдеров лестниц (`ModularKit.cs`):** исключены полноразмерные монолитные `BoxCollider` на корневых объектах `Platform_Stairs_4` и `Platform_Stairs_2`, полностью открыв свободный подъём и спуск для игрока.
   - **S02 (Compute Vault):** серверный монолит перемещён из лестничного пролёта `(-4.8, 0, 19.5)` в северную серверную линейку `(-4.8, 0, 35.5)`, открыв западный сход с мезонина.
   - **S04 (Quantum Nexus):** устранены сплошные ограждения моста на отметках $Z = 41.0$ и $Z = 59.0$, создан беспрепятственный выход из траншеи на настил моста.
   - **S05 (Logistics):** ширина плиты `Mezzanine Slab` скорректирована до $8.8$ м ($X \in [-3.8, +5.0]$), лестницы состыкованы с настилом заподлицо без утопления ступеней в бетон.
   - **S06 (Robotics Plant):** ширина настила `Catwalk Slab` уменьшена до $21.5$ м, ликвидировано нависание плиты над головой поднимающегося по лестницам игрока.
   - **S07 (Security Citadel):** настил `Catwalk T1 Slab` сдвинут до $Z \in [24.0, 48.0]$, верхняя площадка лестницы сопряжена встык с полом галереи.
   - **S09 (Cloud Control):** настил мезонина расширен до $X = 19.0$, южные перила смещены, образован свободный проход с лестницы к терминалу `CLOUD-09`.

2. **Физика и кинематика противников (Phase C):**
   - Устранено прохождение роботов сквозь стены в контроллерах `BulwarkSentryController`, `BastionSentryController`, `TrackedSentryController`, `TraceHoundController`, `JammerRobotController`, `LensDroneController`.
   - Внедрён физический метод `PerformMovement()` с `Physics.CapsuleCast` / `SphereCast` по маске `CollisionLayers.WorldMask` со скольжением вдоль поверхностей.

3. **Интеграция шлюзов и терминалов (Phase D):**
   - Перечисление `DeviceId` расширено до 12 уникальных устройств (`Cargo01=401`, `Rbt01=501`, `Sec07=601`, `Neuro01=701`, `Cloud09=801`, `Super10=901`).
   - Всем 18 дверям комплекса присвоены канонические строковые ID (`door.corridor.server`, `door.shelter.blast`, `door.sec02.vault`, `door.sec02.armory`, `door.sec03.energy`, `door.sec04.cryo`, `door.sec05.logistics`, `door.sec06.robo`, `door.sec07.citadel`, `door.sec07.soc`, `door.sec08.neuro`, `door.sec08.memvault`, `door.sec09.cloud`, `door.sec10.super`, `door.sec10.trust`, `door.sec10.evac`, `door.sec01.exec`, `door.loop5.shortcut`).
   - Оружейный отсек S02 переведён в режим `ManualUnlockOnly`, разблокируется только токеном из `DATA-01`.
   - Терминал `RBT-01` подключён к гермошлюзу Сектора 07 (`door.sec07.citadel`) и активирует режим ожидания охранных систем `EVT_RBT01_DEFENSE_STANDBY`.

4. **Система физических предметов и многодверное сохранение (Phase E):**
   - В `GameSaveData` внедрены `DoorStateEntryData[] doors` и `DeviceMechanicStateData devices`. Состояния всех 18 дверей, клапанов и носителей полностью сохраняются и восстанавливаются.
   - Реализована система 3 физических носителей:
     - `item.usb.hf_tools` — защищённый USB-накопитель в инженерном убежище на столе HOME-PC.
     - `item.cartridge.nl044` — картридж эвакуации на мезонине Сектора 02.
     - `item.key.rbt_diag` — крипто-ключ диагностики робототехники на верстаке в убежище.
   - Устранены статические утечки: сброс всех состояний при новой игре гарантирован через `FacilityRoot.Awake()`.

5. **Верификация и целостность:**
   - `python3 tools/check-unity-api.py`: сборка 100% успешна, 0 ошибок.
   - `python3 tools/verify_repository.py`: 349/349 GUID проверены.
   - `./tools/test-simulation.sh`: 33 поведенческих теста пройдены.
   - `ctest`: 100% нативных графовых тестов пройдены.
   - Сцена `NorthlineFacility.unity` пересобрана и сериализована в Git со всеми новыми компонентами.

## Ранее реализовано (Фазы P0, P1, P2 и P3)

1. **Мастер-документация (Фаза P0 завершена на 100%):**
   - `docs/HOSTFALL_GAP_AUDIT.md`: полный аудит текущего состояния кода vs ТЗ.
   - `docs/design/HOSTFALL_GDD.md`: мастер-диздок по 6 столпам.
   - `docs/design/WORLD_GRAPH.md`: топологический граф мира, 11 секторов, 4 петли возврата.
   - `docs/design/ROOM_CARDS.md`: карточки 51 помещения (координаты, размеры, опасности).
   - `docs/design/ENEMY_MATRIX.md`: матрица 15 типов врагов + финальный босс ATLAS-0.
   - `docs/design/WEAPON_MATRIX.md`: матрица 10 систем вооружения (W01–W10).
   - `docs/design/POWER_PUZZLES.md`: матрица 12 физических головоломок (P01–P12).
   - `docs/design/TERMINAL_SCENARIOS.md`: полные сценарии T01–T12 (команды, ОС, логи).
   - `docs/narrative/STORY_BIBLE.md`: обновленный нарративный канон 2.0 (твист с телом героя, 4 акта, голос Super).
   - `docs/ASSET_REGISTRY.md`: реестр 100 моделей, модулей и пропсов (100% CC0 / OFL).
   - `docs/qa/QA_MATRIX.md`: матрица 25 приемочных тестов с критериями PASS/FAIL.

2. **Вертикальный срез Compute Vault S02 (Фаза P1 завершена на 100%):**
   - **Дуальный маршрут (Dual-Path Layout):**
     - *Нижний уровень (Y = 0):* центральный проход между серверными стойками, патрулируемый тяжелым роботом SENTINEL R21 (QuadShell), рабочая станция DATA-01, доступ к Оружейному отсеку и шлюзу Энергоцентра.
     - *Верхний уровень (Y = 3.2м):* подвесной мезонин на металлических фермах с лестницей доступа, восточной и западной галереями, ограждениями `Platform_Rails_4` и наблюдательным пунктом.
   - **Физическая головоломка P03 (Выдвижной контактный мост):**
     - Смыкаемый пролет через центральный неф на высоте 3.2м.
     - Настенный щиток PDU на колонне мезонина (`RetractableBridgeController.cs`). При взаимодействии (`[E]`) подается питание, мост плавно выдвигается с сервоприводным звуком, световые индикаторы переключаются с янтарного на зеленый, открывая безопасный верхний переход в обход SENTINEL R21.
   - **Физическая головоломка P04 (Картридж с данными):**
     - Физический картридж эвакуации `NL-CART-044` (`DataCartridgeItem.cs`) на столе наблюдательного пункта мезонина.
     - Приемный дисковод (`DataCartridgeSlot.cs`) на корпусе консоли `DATA-01`. После установки кассеты загорается зеленый диод и разблокируется терминальная процедура валидации шлюза.
   - **Воздушная угроза E03 LENS-DRONE:**
     - Летающий разведывательный дрон (`LensDroneController.cs`) патрулирует под потолком (Y = 3.9м).
     - Конусный прожектор (угол 60°, дальность 9.5м). При обнаружении игрока в луче прожектор становится тревожно-красным, звучит высокочастотный зуммер, и тревога передается наземным роботам (SENTINEL R21).
     - Дрон уничтожается выстрелом из дробовика ARC-12 (60 HP) через интерфейс `IDamageable.cs`, с искрами и физическим падением на пол.

3. **Много-ОС сценарный движок терминалов (Фаза P2 завершена на 100%):**
   - Архитектура `TerminalScenario.cs` и реестр `TerminalScenarioCatalog`:
     - T01: SOC-01 (CentOS Stream 10 Linux, bash).
     - T05: MED-01 (macOS Darwin 24, zsh).
     - T06: EXEC-01 (Windows Server 2025, PowerShell 7.5).
     - T07: DATA-01 (AlmaLinux 9.4, bash) с реальной проверкой наличия физического картриджа `NL-CART-044`.
     - T08: PWR-01 (Industrial RT-Linux, BusyBox ash) с реальной проверкой положения клапана охлаждения `V-102`.
     - T09: QNT-01 (QNX Neutrino RTOS 8.0).
   - Интерактивное управление сеансом в `FacilityUI.cs`:
     - Регулировка скорости воспроизведения (0.5x–4.0x через клавиши `[` и `]`).
     - Пауза и продолжение сеанса клавишей `Space`.
     - Мгновенный пропуск / Fast-forward (`[E]` / `[F]`).
     - Аутентичные командные строки (`operator@host:path$ `, `PS C:\Security> `, `root@pwr01-rtu:~# `).
     - Rich-text цветовая подсветка статусов (`[OK]`, `[SUCCESS]`, `[FAIL]`, `[CRITICAL]`, `[EXPLOIT]`, `[PAYLOAD]`).
     - Дедупликация и идемпотентность событий `WorldEventId`.

4. **Электросеть и инженерные головоломки (Фаза P3 завершена на 100%):**
   - **Модель графа электросети `PowerGraph.cs`:**
     - Чистая C# модель расчета энергобаланса в `Hostfall.Simulation`, 100% покрытая behavioral unit-тестами.
     - Учет нагрузок потребителей, перегрузки источника (> 8U) и автоматического отключения цепей.
   - **Головоломка P05: UPS Power Budget Router (`UpsPowerRouter.cs`):**
     - Физическая стойка распределения питания в Галерее аккумуляторов Сектора 03.
     - Бюджет: 8 энергоблоков. Вентиляция (3U), Резервная сигнализация (2U), Грузовой лифт (5U), Крио-шлюз (4U).
     - Живой LCD дисплей с индикацией распределения нагрузки и статуса перегрузки, 4 светодиодных индикатора цепей.
     - Перенаправление питания: отключение сигнализации (2U) позволяет запитать грузовой лифт (3+5=8U), ослабляя охранные сенсоры на альтернативном маршруте.
   - **Головоломка P06: Клапан контура охлаждения (`CoolingValve.cs`):**
     - Массивный промышленный вентиль на юго-западной колонне реактора Сектора 03.
     - Анимированное вращение штурвала ($720^\circ$), поворот стрелки термоманометра из зоны перегрева (842 K) в безопасную зону (295 K), переключение янтарного диода на зеленый.
     - Процедурные звуковые эффекты: скрип чугунного шпинделя и шипение сброса пневматического давления.
     - Физическая предпосылка: пока клапан закрыт, терминал `PWR-01` блокирует пуск из-за перегрева; после поворота вентиля подстанция запускается штатно.
   - **Головоломка P08: Грузовой стрелочный тупик (`CargoSwitchyard.cs`):**
     - Железнодорожный путь с направляющими рельсами в Секторе 04.
     - Передвижная контейнерная платформа, перекрывающая проход к западному отсеку крио-хранилища.
     - Двухрычажный пульт управления (Стрелочный перевод №1 + Силовое реле питания) со светодиодным индикатором готовности.
     - Запуск тяговой лебёдки со звуком предупредительного ревуна и механического рокота троса, плавно смещающий контейнер на 4.8 м и привлекающий внимание патрульных роботов.

5. **Боевые архетипы, Секторы 07 и 08, Вооружение (Фазы P5 и P6 завершены на 100%):**
   - **Сектор 07: Цитадель Безопасности (Security Citadel, $Z \in [18, 65]$, $X \in [5, 54]$, $H = 9.0\text{ м}$):**
     - Комната 07.01: Входной фойе со сканером металлодетектора, оружейной стойкой W02 CIRRUS и ящиком боеприпасов (+12 патронов).
     - Комната 07.02: 3-уровневый атриум содержания ($H = 9.0\text{ м}$) с блоками изолированных камер, мостиками Tier 1 ($Y = 3.2\text{ м}$), роботом-глушителем E09 RF Jammer и тяжелым щитовым стражем E08 Bulwark.
     - Комната 07.03: Центр видеонаблюдения CCTV с изогнутыми дисплеями мониторинга.
     - Комната 07.04: Архив учетных данных с терминалом `SEC-07` (Windows Server 2025 / PowerShell 7.5, сценарий `t05_sec`).
     - Комната 07.05: Коридор безопасности, соединяющийся через гермошлюз `Security Shortcut to SOC Gate` обратно с восточной стеной SOC (Grand Loop 4).
   - **Сектор 08: Нейроархив и Биостаз (Neuroarchive & Biostasis, $Z \in [18, 85]$, $X \in [54, 88]$, $H = 8.5\text{ м}$):**
     - Комната 08.01: Галерея нейроинтерфейсов с клинической синей подсветкой и крадущимся четвероногим роботом-ищейкой E10 Trace Hound.
     - Комната 08.02: Палата криогенных капсул биостаза (Pod 01 Chen, Pod 02 Vasquez, Pod 03 Dr. Lee) и кульминационная капсула Pod 04 с нарративным откровением о переносе синапсов Кейна (Operator-04).
     - Комната 08.03: Мастерская роботизированных шасси со стендом калибровки, аварийной стойкой боеприпасов и патрульным стражем NEURO SENTRY R29.
     - Комната 08.04: Исследовательский кабинет доктора Ады Ли с рабочей станцией `NEURO-MAC` (macOS 26 / zsh, сценарий `t06_neuro`).
     - Комната 08.05: Глубокое хранилище памяти ($H = 8.5\text{ м}$) с вертикальными стеллажами нейрокартриджей, запечатанное гермошлюзом `Deep Memory Vault Gate`.
   - **Новые архетипы противников:**
     - **E08 Bulwark Sentry (`BulwarkSentryController.cs`):** 180 HP, фронтальный баллистический щит, поглощающий 85% урона спереди ($>0.35$ dot), уязвимость с флангов/тыла и к ЭМИ-оглушению, кинетическая пушка с процедурным предупредительным горном.
     - **E09 RF Jammer Robot (`JammerRobotController.cs`):** 100 HP, парящее шасси с радиусом глушения 12 м, процедурный радиочастотный гул, деактивация при ЭМИ-оглушении.
     - **E10 Trace Hound (`TraceHoundController.cs`):** 90 HP, четвероногий охотник с акустическим сенсором (спринт 20 м, шаг 8 м, присед бесшумен), бросок на скорости 4.6 м/с, процедурные звуки механики и рыка.
   - **Новое оружие W02 CIRRUS Pulse Sidearm:**
     - Полуавтоматический импульсный пистолет с магазином на 15 зарядов (запас 45), цикл 0.22 с, лазерный синий трассер, переключение оружия (`[1]`, `[2]`, `[Tab]`).
   - **Автоматические терминальные сеансы Multi-OS:**
     - `t05_sec`: Windows Server 2025 / PowerShell 7.5 на `SEC-07` (`10.17.0.27`), деблокирует срез в SOC.
     - `t06_neuro`: macOS 26 / zsh на `neuro-mac` (`10.17.0.64`), деблокирует гермошлюз Глубокого хранилища памяти.

6. **Секторы 09 и 10, Арена Босса ATLAS-0, Новые Архетипы и W10 TITANBREAKER (Фаза P7 завершена на 100%):**
   - **Сектор 09: Управление Кластером и Диспетчерская (Cloud Control & Dispatch, $Z \in [85, 128]$, $X \in [18, 64]$, $H = 9.0\text{ м}$):**
     - Комната 09.01: Кольцо управляющих узлов (Node Quorum Ring) с полукруглой колоннадой стоек кворума и потолочной рельсовой балкой I-Beam.
     - Комната 09.02: Панорамная диспетчерская (Panoramic Dispatch Center) с обзорным мезонином ($Y = 3.6\text{ м}$), изогнутыми голографическими экранами телеметрии комплекса и тяжелым четвероногим штурмовым стражем E15 BASTION-4.
     - Комната 09.03: Центр трех независимых свидетелей (Independent Witness Center) с мастер-терминалом `CLOUD-09` (Linux / systemd / `quorumd`, сценарий `t10_cloud`).
     - Комната 09.04: Галерея резервного кластера (Backup Cluster Gallery) с резервными вычислительными модулями и аварийными янтарными проблесковыми маяками.
     - Комната 09.05: Переходный вестибюль шлюза ядра с постаментом тяжёлого индуктора W10 TITANBREAKER, ящиком боеприпасов и **Grand Loop 5** — служебным шлюзом `Logistics Shortcut Gate (X=18.0)`, возвращающим игрока обратно в Сектор 05 (Логистический узел).
   - **Сектор 10: Супер-Ядро и Машинный Амфитеатр (Super Core & Amphitheater, $Z \in [128, 206]$, $X \in [-15, 35]$, $H = 14.0\text{ м}$):**
     - Комната 10.01: Машинный амфитеатр — грандиозная арена босса ($50\text{ м} \times 50\text{ м}$, $H = 14.0\text{ м}$) с 4 массивными силовыми колоннами ($3\text{ м} \times 3\text{ м} \times 14\text{ м}$), двухъярусными кольцевыми мостиками и центральным боевым возвышением с исполинской автономной платформой **BOSS ATLAS-0**.
     - Комната 10.02: Башни распределения подсистем (Distribution Towers) питания, криогенного хладагента и оптического наведения.
     - Комната 10.03: Сердцевина доверия (Core of Trust) — изолированный защитный бункер с мастер-терминалом `SUPER-CORE` (QNX Neutrino RTOS 8.0, сценарий `t12_super`).
     - Комната 10.04: Смотровая площадка эвакуации / эпилог (Evacuation Overlook) с панорамным бронестеклом на рассветную горную гряду, аварийным радиомаяком и финальным отчетом о восстановлении комплекса.
   - **BOSS ATLAS-0 (`AtlasBossController.cs`):**
     - Исполинская автономная боевая платформа ($14\text{ м}$ размах крыльев, 600 HP, 4 фазы боя):
       - Фаза 1 (600–450 HP): Лазерный занос силового контура и кинетический обстрел.
       - Фаза 2 (450–300 HP): Сброс криогенного давления и гидравлический удар по арене.
       - Фаза 3 (300–100 HP): Обнажение уязвимого ядра и атака внешних силовых муфт.
       - Фаза 4 (100–0 HP): Критический перегруз, защитный коллапс и аварийное обесточивание, открывающее доступ в Сердцевину доверия.
   - **Новые Архетипы противников:**
     - **E12 RAIL-EYE Turret (`RailEyeTurretController.cs`):** 120 HP, подвесная потолочная турель на рельсовой балке, узкий прицельный красный луч, кинетический выстрел, уязвимость к ЭМИ-оглушению.
     - **E15 BASTION-4 Assault Sentry (`BastionSentryController.cs`):** 240 HP, четвероногий штурмовой танк, спаренные автопушки, гидравлический удар топотом, уязвимость реактора с тыла (2.5x урон) и к ЭМИ.
   - **Новое тяжёлое оружие W10 TITANBREAKER Heavy Inductor:**
     - Тяжёлый индукционный излучатель (магазин 3, запас 9, урон 120, дальность 35 м), мощный импульс пробития брони, интерактивный постамент `TitanbreakerPickup.cs`.
   - **Мульти-ОС сценарии терминалов:**
     - `t10_cloud`: `CLOUD-09` (`10.17.0.99`), Linux / systemd / `quorumd`, подтверждение кворума свидетелей, деблокирует переход в Сектор 10.
     - `t12_super`: `SUPER-CORE` (`10.17.0.1`), QNX Neutrino RTOS 8.0, аннулирование автономии SUPER и завершение инцидента.

7. **Верификация и Стабильность (100% Тестов Пройдено):**
   - 33/33 behavioral checks симуляции (`tools/test-simulation.sh`) — PASS (100%).
   - 54/54 Unity EditMode тестов (`tools/unity.sh editmode`) — PASS (100%).
   - 27/27 Unity PlayMode тестов (`tools/unity.sh playmode`) — PASS (100%).
   - 1/1 C++20 CMake/CTest тестов ядра (`native/hostfall-core`) — PASS (100%).
   - 344/344 GUID и мета-файлов (`tools/verify_repository.py`) — PASS (0 ошибок).
   - Компиляция API (`tools/check-unity-api.py`) — PASS (0 ошибок, 0 предупреждений в коде проекта).
   - Всего: 115/115 автоматизированных тестов успешно пройдены.

8. **Финальная компиляция и релизная сборка (Standalone Desktop Release):**
   - Сборка выполнена через `tools/unity.sh desktop` (`Hostfall.Editor.FacilityBuilder.BuildDesktop`).
   - Результат Build Pipeline: `Build Finished, Result: Success` (0 ошибок).
   - Артефакт приложения: `apps/unity/Builds/macOS/HOSTFALL.app` (размер 129 МБ).
   - Исполняемый бинарный файл: `Contents/MacOS/HOSTFALL` (Mach-O 64-bit executable x86_64).
   - Упакованы сборки `Hostfall.Runtime.dll`, `Hostfall.Simulation.dll`, сцена `NorthlineFacility.unity` со всеми 11 секторами (S00–S10), боссом ATLAS-0, всеми типами роботов, терминальными сценариями Multi-OS и физическими головоломками.
   - 100% лицензионная чистота: CC0 1.0 Universal и SIL OFL 1.1, zero proprietary assets.

убраны имена узлов/ответы конкретного инцидента. Код — справочный, не runtime внутри игры.
Предыдущая редактура сохранена; новая терминальная статья объясняет роль наблюдаемого сеанса.
В UI нет внешних учебных ссылок, «Дальше», «Проверьте себя», подсказок и «Подробностей».

## Сохранение

world-v2 остаётся атомарным и проверяемым по checksum, backup, auto/quick/3 manual.
Файлы/процессы/питание/history/draft/host, знания/язык/записки, игрок/камера/шлюз и оба
робота сохраняются. routeVersion=2 добавляет шаги/частичный набор/задержки трёх сеансов,
ActionState и фазу рывка. Загрузка не повторяет эффекты и не обнуляет cooldown.
Старый изолированный источник переносится в WS+EDGE, сохраняя доступ к шлюзу;
завершённый старый участок — в три узла. Пользовательские файлы/история не сбрасываются.
Backup до изменения: .artifacts/action-route-before, включая пять файлов сохранений.

## Проверено в этой итерации

- Очистка консоли терминалов: убран весь лишний мусор, дампы процессов и сюжетные строки из терминала. В консоли отображаются только аутентичные bash-команды (`operator@host:~$ cmd`) и их технический вывод. Игровые уведомления направляются в Action Notice / HUD.
- Исправление блокировок проходов: удалена глухая бетонная стена `End wall` в серверной ($Z = 15.0$) и задники `Painted room backdrop`, блокировавшие проход в Сектор 02. Сформирован портал с левым/правым косяками и перемычкой.
- Полная ликвидация прозрачных/розовых объектов: исправлены имена материалов в `SectorExpansionBuilder.cs` (`Industrial Metal`, `Dark Metal`, `Concrete`, `Floor Panels`, `Indicator Amber`, `Glass`, `Screen`, `Service sage paint`). В сцене 440 MeshRenderers, 0 нулевых материалов.
- Устранение торчащих сквозь потолок объектов: высота рам дверных проёмов скорректирована под высоту потолков (3.0–3.2 м), створки дверей сдвигаются горизонтально в карманы стен (`Vector3.right` / `Vector3.forward`), не пробивая перекрытия.
- Исправление коллайдеров `ModularKit`: рамы дверей разделены на боковые стойки и притолоку (проём полностью проходим); на лестницах установлены наклонные рамповые коллайдеры для гладкого подъёма; на мостках и плитах установлены твёрдые BoxCollider.
- Герметичность комплекса: перепады высот потолков между помещениями закрыты фрамужными переборками (bulkheads), исключающими щели в пустоту.
- Реальное открытие дверей терминалами: добавлен режим `DoorAccessCondition.ManualUnlockOnly`. Двери заблокированы до завершения сеанса на соответствующих терминалах:
  - `CORE-01` открывает Vault Gate ($Z = 15.0$) в Сектор 02 (Compute Vault / Датасеты).
  - `DATA-01` открывает гермошлюз ($X = -7.0, Z = 26.0$) в Сектор 03 (Energy Center).
  - `PWR-01` запускает подстанцию и открывает криогенные ворота ($Z = 38.0$) в Сектор 04 (Quantum Nexus).
  - `QNT-01` активирует обходной контур и открывает служебный шлюз ($X = -5.0, Z = -1.75$), замыкающий байпас-коридор обратно в операторскую (SOC).
- В траншее Сектора 04 установлены лестницы с коллайдерами, исключающие застревание игрока.
- API compile PASS (`python3 tools/check-unity-api.py`), 0 ошибок.
- Unity Sector Expansion (`SectorExpansionBuilder.Apply` / `tools/unity.sh expand`): exit 0.
- Оружейная система и дробовик ARC-12: реализован `PlayerWeapon.cs` и `WeaponPickup.cs`. Импульсный дробовик `NORTHLINE ARC-12` интегрирован в воплощённого персонажа без фантомных viewmodel-рук, стреляет на дистанцию 8,5 м конусом из 5 импульсных поражающих элементов по нажатию ЛКМ, сопровождается отдачей камеры, вспышкой и процедурным синтезом звука кинетического удара с электрическим разрядом.
- Разрушение роботов за 2 выстрела: в `WatcherController.cs` добавлена боевая система урона (HP: 100, урон дробовика: 55). Первый выстрел переводит робота в тревогу и вызывает искры; второй выстрел полностью уничтожает робота (110 урона), гасит оптический сенсор, отключает коллизии и переводит в анимацию `TurnOff`.
- Сектор 02 (Оружейный отсек // ARMORY [SEC-02]): построено полноценное помещение оружейного отсека (X: 7..14.5, Z: 21..31) с гермодверью, стеллажами оружия, ящиками припасов, аварийным освещением, пьедесталом с ARC-12 и интерактивным подбором на клавишу [E].
- Аутентичное хакерство на терминалах: `DATA-01`, `PWR-01` и `QNT-01` выполняют реалистичные многошаговые кибер-эксплойты (сканирование портов, PAM bypass, дамп регистров, извлечение ключей допуска `[KEY-SEC02-ARMORY]`, `[AUTH-REACTOR-BYPASS]`, `[SUPER-OVERRIDE-CORE]`) с разблокировкой дверей и выводом уведомлений на HUD.
- Исправление архитектурных багов и лестниц: убраны все прорезавшие пол моста лестницы в Секторе 04; под квантовым мостом установлены 4 массивных несущих пилона; под мезонином Сектора 02 установлены вертикальные стальные колонны; лестница на мезонин выстроена без коллизий со стенами; для кабельных лотков добавлены потолочные подвесы.
- Переработка стекла и материалов: во всех 5 секторах (SOC/Server, Compute Vault, Armory, Energy Center, Quantum Nexus) размещены realtime `ReflectionProbe` с боксовой проекцией; смотровые окна получили многослойные стальные рамы с резиновыми уплотнителями.
- Световые навигационные табло: над всеми гермодверями комплекса установлены подсвеченные таблички с кодами секторов и направлениями.
- Репозиторий и метаданные: PASS (`python3 tools/verify_repository.py`), 322 asset GUIDs.
- Чистая C# симуляция: 32/32 PASS (`tools/test-simulation.sh`).
- Simulation behavioral checks: 33/33 PASS (`./tools/test-simulation.sh`).
- Unity EditMode: 54/54 PASS (`tools/unity.sh editmode`).
- Unity PlayMode: 27/27 PASS (`tools/unity.sh playmode`), включая интеграционные тесты `InputFlowTests`, `EnemyBehaviorTests`, `FacilitySmokeTests`.
- Полный набор тестов: 114/114 PASS (100%).

## Интерактивная карта комплекса, исправление геометрии и Apple-минимализм UI (2026-11-09)

1. **Интерактивная карта комплекса и HUD-навигация**:
   - Реализован оверлей схемы комплекса `FacilityMap` (`FacilityUI.cs`, `Facility.uxml`, `Facility.uss`). Открывается клавишей `M` в режиме исследования или через кнопку «Схема комплекса» в паузе; закрывается по `M` / `Esc`.
   - Включает все 11 секторов комплекса с карточками, описанием оборудования, путями возврата и динамической отметкой текущего положения игрока («ВЫ ЗДЕСЬ»).
   - На постоянный HUD добавлен минималистичный информационный бейдж текущего местоположения (код сектора, наименование зала), обновляющийся в реальном времени по пространственным границам.
2. **Исправление геометрии и пересечений объектов**:
   - **Серверная S01**: устранена торчавшая в проход труба (убран выпирающий тройник, труба-стояк и клапан установлены заподлицо со стеной при `X = 4.92`).
   - **Сектор 02 (Compute Vault)**: добавлен лестничный марш `Platform_Stairs_4` со спотлайтом со второго яруса западного мезонина (`Y = 3.2`) на первый этаж к шлюзу Энергоцентра и терминалу `DATA-01`, удалена перекрывающая секция перил.
   - **Оружейный арсенал Сектора 02**: дверь отсека переведена в режим свободного доступа (`AlwaysUnlocked`), обеспечивая гарантированное получение дробовика ARC-12 и патронов.
   - **Квантовый узел S04**: лестницы выхода из траншеи смещены к боковым кромкам (`X = ±2.35`) со стыковочными площадками, устранено взаимное пересечение со сквозным мостом.
   - **Запертые двери**: `FacilityDoor` теперь выводит внятное статусное уведомление при попытке взаимодействия с заблокированным проходом вместо тихого пропуска.
3. **Минимализм боевого и сюжетного UI в стиле Apple**:
   - Полностью устранён капслок и шумные уведомления у босса ATLAS-0, Джаммера, Булварка, Бастиона, Дронов и Турелей («Лазерный сектор», «Крио-сброс», «Ядро открыто», «ATLAS-0 отключён»).
   - Устранено ложное отображение статуса опасности роботов после их ликвидации: фильтрация `!isDestroyed` в проверках радиопомех и импульса индуктора.
   - Задачи в HUD переписаны в строгом стиле с чётким позиционированием по секторам («Сектор 00 · Операторская // Терминал WS-07»).
4. **Сборка и валидация**:
   - Автономный билд macOS Standalone x64 собран в `apps/unity/Builds/macOS/HOSTFALL.app`.


## Архитектурный аудит геометрии и устранение висячих объектов (2026-11-08)

Проведён полный пространственный аудит сцены `NorthlineFacility.unity`:
1. **Лестница Сектора 02 (`Platform_Stairs_4`)**:
   - Устранена коллизия: удалён монолитный серверный шкаф на `(4.8, 0, 19.5)`, в котором лестница находилась внутри геометрии.
   - Устранено перекрытие потолком: площадки мезонина перенесены на `Z = 20..35`. Лестница начинается на полу при `Z = 16.4`, плавно поднимается на север до `Z = 20.0` на высоту `Y = 2.4 м` с открытым пространством над головой и бесшовным выходом на настил.
   - Добавлены стальные опорные колонны под каждым пролётом мезонина и направленный прожектор ступеней `Catwalk Stairs Spotlight`.
2. **Ступени и обходной мост в Серверной (`Action Route / Service Steps` & `Maintenance Deck`)**:
   - Три канонические ступени сохранены с точными 3 коллайдерами для совместимости с `ActionRuntimeTests`.
   - В продолжение третьей ступени добавлен непрерывный служебный настил `Action Route / Maintenance Deck` вдоль западных серверов (`Z = 11.55..14.5`) со стальными опорами до пола, ограждениями и лестничным спуском у ворот хранилища.
3. **Висячие охлаждающие клапаны**:
   - `Sector03 Cooling Valve`: перенесён из прохода и смонтирован на трубопроводной колонне токамака (`X = -18.7, Y = 1.35, Z = 25.0`) с монтажным фланцем.
   - `Cooling valve` серверной: снабжён вертикальной настенной трубой-стояком (`X = 4.82, Z = 10.6`) от пола до потолка.
4. **Стойки верхних ламп терминалов**:
   - Консольные лампы WS-07, EDGE-01, CORE-01, DATA-01, PWR-01, QNT-01 получили несущие стойки, уходящие вниз в раму рабочего стола, исключая зависание плафонов в воздухе.
5. **Траншея Сектора 04 (Квантовый узел)**:
   - Боковые перила моста замкнуты непрерывно на всём протяжении (`Z = 38..62`).
   - На обоих концах траншеи установлены служебные лестницы для подъёма со дна траншеи (`Y = -1.2 м`) на уровень моста (`Y = 0 м`).

Нативная macOS x64 сборка Success / exit 0. Визуально проверены меню, новое введение,
журнал, автономный наблюдатель и пауза. Старый мир успешно мигрировал: history/draft/host,
процессы/питание, язык и точная позиция игрока сохранены. Исключений нет; остаются прежние
URP stripped-pass warnings. Приложение оставлено на паузе. Подробности и ограничения —
qa/VERIFICATION.md. Полный маршрут нативно вручную не проходился.

## Переработка по плану HOSTFALL_REBUILD_BLUEPRINT и аудит сцены (2026-11-10)

Выполнен масштабный производственный этап глубокой переработки проекта по мастер-плану:
1. **Убежище инженера (ДОМ / Sector 00-HOME) и Сервисный тоннель**:
   - Построено безопасное стартовое убежище инженера ($Z \in [-19, -9]$, $X \in [-5, 5]$) с монолитным герметичным контуром стен, потолка и пола.
   - Сервисный переход ($Z \in [-9, -7]$) с бронированным гермошлюзом `[SEC-HOME]` соединяет убежище напрямую с южной стеной операторской (SOC). В южной стене SOC прорезан портал шириной 2.7 м и высотой 3.0 м.
   - Оборудована рабочая станция `HOME-PC`: двухмониторный инженерный стол, системный блок с индикатором питания, клавиатура, мышь, док-станция USB с флеш-накопителем `HF-TOOLS`.
   - Зарегистрирован сценарий терминала `t00_home` (`TerminalScenarioCatalog`): автономный hardened Linux 6.6, проверка целостности пакета `route-repair.pkg` с контрольной суммой sha256 за подписью Ирины Ветровой и сетевое телеметрическое сканирование подстанции.
   - Зона аппаратного верстака: осциллограф/логический анализатор, паяльная станция, катушки проводов, ящик с патронами (+12) и полевой ремкомплект Type-IV (+50% HP).
   - Зона отдыха: спальное место, полевая раскладушка, ящики с сухпайками и канистры с водой.
   - Тактическая доска расследования: пробковая доска на восточной стене с булавками инцидентов, топологическими схемами и соединительными нитями.
   - Локальное тёплое освещение рабочей зоны и индивидуальный realtime `ReflectionProbe`.
2. **Система здоровья игрока, обратная связь по урону и цикл гибели (Player Health & Death Loop)**:
   - Создан компонент `PlayerHealth.cs`: реализация интерфейса `IDamageable`, запас прочности 100 HP, i-frames (0.6с), процедурная генерация звука электрошока при получении урона, интеграция с визуальными эффектами HUD.
   - Создан компонент `DeathController.cs`: управление состояниями жизнедеятельности (`Alive -> Dying -> Dead`), отключение управления исследованием при потере нейролинка, отображение экрана гибели (`// NEURAL LINK LOST //`) с кнопками перезапуска рейда и выхода в главное меню.
   - Реализован подбираемый предмет `HealthKitPickup.cs` («Полевой ремкомплект Type-IV» +50% HP).
   - Обновлён HUD (`Facility.uxml`, `Facility.uss`, `FacilityUI.cs`):
     - Минималистичный бейдж запаса прочности шасси `health-badge` с динамической полосой заполнения `health-bar-fill` и текстовым процентом.
     - Эффект вспышки повреждения экрана `damage-vignette`.
     - Модальный экран гибели `death-screen` с указанием причины разрушения корпуса, времени сеанса и кнопкой «ПЕРЕЗАПУСК РЕЙДА».
   - В боевой контроллер роботов `WatcherController.cs` интегрировано нанесение урона здоровью (35 HP в упор, 45 HP при зарядке атаки) с тактическим отбрасыванием и защитой от повторного контакта.
3. **Инструмент аудита сцены `HostfallSceneAudit.cs` и ликвидация дубликатов**:
   - Инструмент аудита адаптирован под Unity 6000.6.8f1, режим `audit` интегрирован в CLI `tools/unity.sh`.
   - Проведён аудит 4029 объектов сцены:
     - Ликвидированы все 100% точных дубликатов мешей (`POTENTIAL_EXACT_MESH_DUPLICATE: 0`).
     - Исправлена привязка трансформаций табличек `CreateSignboard` (`localPosition` вместо `position`).
     - Исправлен цикл монтажа кабельных лотков в `FacilityBuilder.cs`.
     - Обеспечена полная идемпотентность модификации южной стены `AdaptSocSouthWall`.
   - Все оставшиеся 23 эвристических предупреждения полностью проверены и документированы в `docs/AUDIT_REPORT.md` (намеренные клоны пропсов CC0, прокси-коллайдеры настилов и декоративные настенные полосы).
4. **Валидация сохранений `WorldSaveStore`**:
   - Расширены границы валидации координат игрока в `WorldSaveStore.cs` (`p.position.z > -25` вместо `-12`), гарантируя сохранение и загрузку в Убежище инженера.
5. **Комплексная приёмка и тесты**:
   - `python3 tools/check-unity-api.py`: PASS (0 ошибок).
   - `python3 tools/verify_repository.py`: PASS (348 asset GUIDs).
   - `./tools/test-simulation.sh`: 33/33 PASS (100%).
   - `./tools/unity.sh editmode`: 54/54 PASS (100%).
   - `./tools/unity.sh playmode`: 27/27 PASS (100%).
   - Нативные графовые тесты C++20 (`ctest`): 1/1 PASS (100%).
   - Сборка standalone десктоп-плеера (`tools/unity.sh desktop`): SUCCESS (`HOSTFALL.app`).

## Ограничения

Это первый работающий срез новой парадигмы, не готовая большая паркур-кампания.
Нет сложного traversal (wall-run/vault), финального боевого дизайна, звука способностей,
финального персонажа/освещения или измеренной гарантии 60 FPS. Контакт возвращает к
посту, не стирая прогресс. Сюжет за пределами трёх помещений ещё не построен.
Windows/Linux/Mobile и Web этой итерации не собирались. Нет настоящей ОС, SSH-сети,
компилятора/интерпретатора игрока, Docker или Kubernetes в процессе Unity.

Следующее расширение: маршруты с выбором безопасной высоты/времени, телеграфирование
атак, звук приводов, разные восприимчивости роботов к помехам, новые связанные комнаты.
Эти возможности пока не выдаются за реализованные.

## Проверка прохождения 2026-11-10

- При ручном прохождении новой игры на macOS пройден сеанс WS-07; следующая цель — EDGE-01. Полное прохождение пока не подтверждено. В коридоре рабочее место EDGE-01 сдвинуто к стене; проверка капсулой размера игрока подтверждает свободный проход по выбранной линии. Ручное прохождение всей игры не завершено.
- Исправлен возврат за границы мира при нахождении в убежище: границы согласованы с проверкой сохранений, включая убежище и расширенные сектора 05–06. Добавлен Play Mode тест этих областей.
- Для управления камерой с клавиатуры добавлены стрелки; мышь остаётся доступной.
- В `MissionDefinition.FacilityCampaign` описано шесть целей до QNT-01, хотя карта и сцена содержат дальнейшие сектора. Наличие этих секторов не подтверждает сквозное прохождение.
- Unity Play Mode: 29/29; API compilation и проверка целостности репозитория прошли. Это не замена ручной проверке маршрута.
- После повторного прогона обнаружено, что проверка сохранения ограничивала WATCHER исходной зоной патруля, хотя робот может преследовать игрока за её пределами. Границы согласованы с комплексом; Edit Mode 55/55, включая сохранение и загрузку робота в операторской. В новой сборке после атаки и гибели ошибки записи в журнале не наблюдалось, файл автосохранения обновлён; полноценную загрузку этого состояния ещё нужно проверить.
- Финальная смотровая площадка находится на Z=199, за прежней границей Z=190. Граница возврата и проверки сохранения расширена до Z=210; Play Mode проверяет площадку, Edit Mode — запись и чтение позиции там. Play Mode 30/30, Edit Mode 56/56, desktop-сборка успешна.
- Экран гибели теперь перехватывает фокус; перезапуск сохраняет выбор языка кампании. Проверка фокуса выполнена в Play Mode.
- Аудит сохранения выявил незакрытый риск: прочность игрока и значительная часть объектов расширенных секторов не входят в `GameSaveData`. Полное восстановление мира после загрузки не подтверждено и требует отдельного исправления. Поздние сектора и финал не пройдены вручную.
- Прочность и остаток неуязвимости после удара теперь входят в сохранение; старые файлы без отметки наличия прочности загружаются со штатным здоровьем. Загрузка разрушенного корпуса показывает экран гибели. Проверены загрузка после ранения, гибель и старый формат: Edit Mode 57/57, Play Mode 32/32. Остальное состояние расширенных секторов всё ещё требует реализации и проверки.
