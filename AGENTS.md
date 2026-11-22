# Active user direction — 2026-11-20

Read `docs/hostfall/HORROR_CAMPAIGN_2026-11-20.md` for the current campaign direction.
Target a twenty-minute physical techno-horror route. The approved computer route retains
VENTFLOW and two investigations, «Пустая камера» at CORE-01 and «Второй голос» at
NEURO-01. Other field stations show local dossiers and selected typed .svc utilities
with a local «Знания» tab. Home keeps its shell and a mandatory dossier signature
before transit. Historical save readers remain supported. See ADR 0028.

## Previous direction — 2026-11-17 / ADR0016

The new user MASTER_REQUEST explicitly supersedes older automatic-terminal / global
knowledge / finale rules below. Read `docs/hostfall/MASTER_REQUEST_2026-11-17.md`,
`docs/hostfall/ITERATION_STATUS.md` and ADR0016. Home has the only manual safe virtual
shell and local Knowledge. External PCs have player-controlled graphical apps/games;
never restore automatic solution playback to the active 2.0 route. Implement four
sequential acceptance gates; iterations 1 and 2 accepted; iteration 3 in progress. Preserve checkpoints,
legacy data and explicit v3/v2 separation. Everything below remains applicable where
it does not conflict with the newer user request.

# HOSTFALL

Подача комплекса 2.0 (2026-11-20). Домашний терминал принимает ввод сразу
после наведения камеры. Пока открыта оболочка, фокус возвращается на строку
ввода. Уход с компьютера остаётся на Esc. На рабочих столах терминалов нет
кнопки «Отойти · Esc», декоративных заголовков и хлебных крошек помещения.
Названия мест показывают карта и радар. Навигационные надписи в мире короткие
и закреплены на стенах или на непрозрачных табличках. Аппаратные дисплеи
вписаны в оборудование и не заходят в дверные проёмы. При редакторской съёмке
надписи проходят тот же тест глубины, что и при игре. Световые акценты нужны
для локального освещения и значимых состояний оборудования.

**Активное направление 2026-11-08 — ADR 0007.** Пользователь отменил ручной ввод команд.
Терминалы автоматически показывают доверенные сеансы; основа игры — перемещение,
уклонение от автономных роботов и временные дебаффы. Не возвращать TextField команд,
обучение вводу через help или ручное отключение служб в игровой UI. Знания, мир,
история и сохранения остаются. Читать docs/architecture/ACTION_ROUTE.md. Более ранние
правила ручного прохождения ниже — исторические и уступают этому решению.

При каждой новой задаче сначала прочитать `AGENTS.md`, `docs/PROJECT_STATE.md`
и относящиеся к задаче документы. Факты и результаты проверок хранить в репозитории.

Одиночная офлайн 3D-игра NORTHLINE / Super. Стек: Unity **6000.6.8f1**, URP,
C#, Input System, Cinemachine 3, UI Toolkit, CharacterController; независимый C++20/CMake.
Канон 1.1: антагонист Super, инженер с повреждённой памятью, единая разветвлённая карта.
Новый цикл: добраться до узла → автоматический сеанс → преимущество против роботов → новый маршрут.
Реальный runtime/Kubernetes-lab отложен, библиотека знаний остаётся дополнительной.
Сначала читать docs/vision/CREATIVE_DIRECTION.md и docs/narrative/STORY_BIBLE.md для контентных задач.
Не менять движок, рендерер и офлайн single-player без обоснованного согласования.
Не добавлять multiplayer, backend, DI, DOTS, платные обязательные ассеты или небезопасный JS runner.

- `apps/unity`: активная игра; стартовая сцена `_HOSTFALL/Content/Scenes/NorthlineFacility.unity`.
- `native/hostfall-core`: независимая графовая библиотека, пока НЕ связана с Unity.
- `legacy/terminal-v3`: исполняемый эталон терминала. Не импортировать его в Unity.
- `docs/architecture`: реальные границы и статус переноса; `docs/qa`: результаты и ручные сценарии.

Запуск: `tools/unity.sh open` (или Unity Hub → Add → apps/unity).
`UNITY_EDITOR` задаёт путь к исполняемому редактору. Версию не подменять молча.
`tools/unity.sh author` пересоздаёт сохраняемые сцену/префабы из FacilityBuilder;
это заменяет изменения сцены, поэтому сначала сохранить авторскую работу в Git.
`tools/unity.sh editmode`, `playmode`, `desktop`, `web`: тесты и сборки, нужны редактор и лицензия.
`tools/test-simulation.sh` — чистые C# behavioral checks через bundled .NET/NUnit.
`python3 tools/check-unity-api.py` — API compilation; `python3 tools/verify_repository.py` — source/meta integrity.
Эти проверки не заменяют запуск Unity. Cinemachine API 3 входит в Editor как core package 6.6.0.
`cmake -S native/hostfall-core -B .artifacts/native-debug && cmake --build .artifacts/native-debug && ctest --test-dir .artifacts/native-debug --output-on-failure`.
Эталон: `cd legacy/terminal-v3 && npm ci && npm test && npm run build`.

Хранить все Unity `.meta`, scene/prefab/material/settings assets в Git; не Library/Temp/Builds.
Game state принадлежит Simulation, физические объекты адресуют DeviceId. UI только отображает/посылает команды.
Изменения модели проверять поведенческими тестами, сцены — Play Mode и сценариями QA.
Не объявлять сборку/визуальную проверку успешной без фактического запуска.
После существенных изменений обновлять PROJECT_STATE, TERMINAL_MIGRATION и QA.
Существенные решения фиксировать коротким ADR: контекст, решение, последствия.
Не коммитить и не публиковать непроверенные изменения автоматически.

FieldContent.json — статьи, типизированные связи, три задания и записки; FieldCatalog валидирует ссылки.
HUD: только текущая цель; журнал/архив открываются отдельно. Не добавлять постоянные виджеты без причины.
Реальные технологии сверять с первоисточниками; отличать просмотр источника от выполнения примера.
Нельзя выдавать виртуальную ОС за настоящую, разработанный runtime/Kubernetes — за запущенный.
Не запускать произвольный код игрока в процессе Unity и не передавать его shell/Docker daemon.

Пользовательское направление UI (2026-11-07): функциональный минимализм. Не возвращать
в игру внешние учебные ссылки, QA-отметки, J/K в подписях кнопок и постоянное «Сохранено».
Объяснения короткие и самодостаточные; источники — только для редакторской проверки.
Терминальный вывод/history/draft/remote host живут у VirtualComputer и входят в save.
Load/controls — отдельные страницы; auto/quick/3 manual slots не смешиваются.
Статические надписи используют WorldTextDepth, крупные объекты — непрозрачные материалы и коллизии.
Палитра окружения: Quaternius SciFi Standard CC0 + собственные функциональные предметы;
не примешивать случайные стили. См. docs/levels/MODULE_SYSTEM.md и ADR 0004.

Художественное уточнение пользователя 2026-11-07: сохранять тёмный технохоррор,
использовать локальный свет и умеренный декор; не заливать весь комплекс светом.
EngineeringDressing — 3 выбранных Poly Haven CC0 props, не импортировать всю библиотеку.
В сцене два Quaternius Essentials CC0 робота: WATCHER R17 (проход, WS/EDGE) и SENTRY R18
(серверная, процесс CORE). Исходники/анимации/provenance — Art/QuaterniusRobots; детали
в docs/research/2026-11-07-environment/ROBOTS.md. Сохранять причинность AI и стабильные Id.
TerminalSession владеет записью команд/ответов; TerminalDisplay — единое представление
для UI и физических экранов. Не возвращать отдельный псевдовывод на монитор.
В заданиях нет скрывающих содержание кнопок «Подробности».


Меню и обучение (2026-11-07): отдельный ControlMode.MainMenu, новая игра/загрузка только
там. В паузе этих действий нет; возврат в главное меню сохраняет текущий мир. Вход
через BeginSession показывает Introduction один раз на версию управления; Q/F и автосеансы описаны там,
а не автоматической строкой терминала. Escape не обходит главное меню/введение.
Выбор cpp/python находится внутри пункта «Новая игра», не на главном экране.
На главном экране нет NORTHLINE / 04 и «Сеанс не завершён».
Выбор языка новой игры хранится в предпочтениях; язык текущей кампании и
introductionSeen — в FieldProgressData. Load восстанавливает язык слота, выбор для
новой игры не переписывает старую кампанию. Старые слоты по умолчанию Python.
FieldContent: 96 авторских статей (24 C++, 24 Python, 48 общих), 72 на кампанию;
JavaScript/HTML/CSS из активной библиотеки убраны. Список тем заменяет большой граф.
IsAvailable проверяет ветку, сюжетный этап 0..3 и прочитанные предпосылки; поиск и ссылки
не обходят блокировки. Источники редакторские; tools/build-knowledge-catalog.py воспроизводит
каталог, tools/check-knowledge-examples.py запускает только доверенные авторские примеры.
В заданиях нет кнопок подсказок, нужное направление дано в описании; старые hints в save
сохранены только для совместимости. Примеры в архиве не означают runtime C++/Python.
FPS использует руки реального тела, без отдельного парящего viewmodel. Оптика робота
прикреплена к анимированной Body-кости; импорт FBX имеет масштаб сантиметров, BakeMesh
вызывается с компенсацией scale. Не возвращать неподвижную мачту отдельно от корпуса.

Приёмка знаний: самостоятельная редакторская и техническая проверка. Не ставить
привлечение пользователей условием выполнения: пользователь поручил проверку агенту.
Фиксировать конкретные найденные проблемы и результаты, без вымышленных A/B-тестов.

Action-route: TerminalDirector владеет фиксированными командами и состоянием их набора.
ActionState владеет cooldown и дебаффами; FacilityActionController — единый таймер мира
и геометрические проверки. Сохранение включает шаг/typed/wait, фазу рывка и эффекты.
Завершение терминала и награды идемпотентны. WATCHER автономен в новом маршруте.
NightShift — внутренний legacy-контракт регрессий, активная миссия — ActionRoute.

Геометрия и инструменты (2026-11-14): читать `docs/tooling/EDITOR_WORKBENCH.md`.
Перед работой проверить реальные MCP instances и явно выбирать редактор; рабочая сцена
и проверочная копия могут быть открыты одновременно. Проверять использование моделей/
текстур по сцене, снимать несколько реальных ракурсов, а не судить только по иерархии.
Сохранять авторскую сцену: адресный ремонт в копии, backup/hash перед переносом.
Наклонный BoxCollider под неравномерным scale не совпадает со ступенями: применять
StairWalkSurface (исходные Quaternius лестницы поднимаются к -Z). Проходы подтверждать
CharacterController в обе стороны; скан нижнего этажа не выдавать за прохождение кампании.


Оружие и анимация (2026-11-15, ADR 0012): одна переносимая пушка; подбор оставляет
прежнюю модель в мире с её боезапасом/нагревом. Simulation.Weapons владеет предметами
и собранными authored IDs; физические позы синхронизируются перед save. Не возвращать
цифровые слоты, вызывающие брошенную пушку из воздуха. ARC — отдельные патроны,
CIRRUS — нагрев, TITAN — удержание/отпускание заряда. Quaternius UAL1 CC0 клипы в
Resources/Motion retargeted через OperatorMotion на реальное тело; CharacterController
владеет перемещением. Источники/экспорт — Art/QuaterniusMotion/PROVENANCE.md.
MapShape.Refresh обязан учитывать явное перемещение/resize; FacilityMapWorld сам
планирует только moving shapes. Не возвращать пересчёт всей статической сцены.


Броня и бюджет роботов (2026-11-15, ADR 0013): пластины подгоняются RobotArmorAuthor
по Body-weighted поверхности отдельно для QuadShell/Trilobite; не возвращать общий
actor-space offset или runtime BakeMesh. MANTIS объединяет только фиксированные части;
52 008 индексов должны сохраниться, шесть групп обязаны быть непустыми. Навигация —
общая round-robin очередь A*, максимум 96 проверок рёбер и мягкий бюджет 1.2 мс;
мгновенные проверки движения/пола остаются. GPU Resident Drawer/occlusion в production
отключены после нативной приёмки; не включать только ради меньшего счётчика draw calls.


Физические станции и экранный бюджет (2026-11-15, ADR 0014): WorkstationSurface
задаёт углы стекла/позу; UI Toolkit проецирует текст на стекло после плавного фокуса.
Не возвращать отдельный fullscreen терминал. Simulation.Screens хранит расширенные
доверенные сеансы/набор/ожидание/награду; первые три остаются у TerminalDirector.
Fullscreen/UI — native; URP 3D — не более 2 073 600 пикселей. TestStorageDirectory
действует и в normal-player; QA bundle ID отдельный. Перед native replay проверять
изоляцию и хеши пользовательских сохранений. Старый отсутствующий weapon roster
допустим; malformed текущий — нет. См. QA PHYSICAL_WORKSTATIONS_2026-11-15.


Непрерывный экран и вступление (ADR 0015, 2026-11-17): NearbyTerminalDisplay использует
один DOM/RenderTexture для подхода и работы. Native glass композитит ту же текстуру,
не новую вёрстку. Общая память и idempotent reward остаются у Simulation. OpeningSequence
только для новой кампании; загрузка не сбрасывает позу и не повторяет катсцену.
Шлюз door.soc.corridor закрыт до WS-07, затем E; не замыкать зависимость от EDGE внутри
того же коридора. Headless проверки запускаются с -noaudio при проблемах устройства,
но production AudioManager не отключать; голос/картинка проверяются нативно отдельно.

Актуальное уточнение 2026-11-19 (ADR 0023): на карте и радаре видны названия
мест без номеров секторов. Герой перенёс сознание в Type-IV до пробуждения.
«Знания» доступны рядом с домашним терминалом, сюжетные пути и ввод сохранены.
Две адресные сцены захвата отпускают компьютер до физического удара.
PAYLOAD запускает две волны с физическими воротами. В финале SUPER объясняет
обман и герой видит катастрофу из эвакуационного зала. Сохранять доступ к
старым слотам и проверять обе сборки в изолированных профилях.
