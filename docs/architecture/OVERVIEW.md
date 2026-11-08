# Architecture

> Обновление 2026-11-08: пользователь сменил основной цикл. Активны
> постановочные автоматические сеансы, паркур/уклонение и дебаффы роботов.
> Правила ручного исследования ниже исторические; см. ADR 0007 и architecture/ACTION_ROUTE.md.

`FacilityRoot` — composition root со всеми сериализованными ссылками. Создаёт одно
состояние `FacilitySimulation`, явно связывает ввод, игрока, камеры, UI и физические устройства.
Ссылки не ищутся в Update и не определяются по названиям объектов.

Три assembly: Simulation (чистый .NET, noEngineReferences), Runtime (Unity + зависимости),
Editor (создание контента и сборки). EditMode/PlayMode tests — отдельные test assemblies.

- PlayerMotor: единственный CharacterController, скорость, ускорение, гравитация, jump buffer/coyote.
- PlayerCameraRig: единый yaw/pitch, две CinemachineCamera и один Brain, TPS со смещением к плечу. SphereCast исключает
  слой 8 игрока, мгновенно приближает камеру у стены и плавно возвращает её. Переключение — cut:
  интерполяция через геометрию недопустима. Ближняя плоскость 0.06 м.
- FacilityInput: Input System actions; gameplay map выключена в UI. System/Escape остаётся активным.
- ControlState: Exploring / Terminal / Paused / Field, курсор и пауза. Потеря фокуса при исследовании открывает меню.
- InteractionScanner: первый solid hit по центру камеры, физическая дистанция от глаз до 2.8 м,
  дополнительная линия видимости от глаз не позволяет взаимодействовать из-за угла в TPS. Без triggers/игрока. Дверь,
  терминалы и выключатель используют один IInteractable; преграда естественно перекрывает взаимодействие.
- VirtualComputer: host-local FS/cwd/processes/power. DeviceId — uint enum 7, 1, 99.
- TerminalSession: парсер цитируемых аргументов, результат/exit code, команды; независим от UI.
- TerminalMemory: per-computer transcript/history/draft/scroll/remote host; закрытие UI не очищает память.
  Восстановление памяти не исполняет строки. clear очищает только вывод, exit возвращает origin.
- FacilitySimulation: единственный источник питания, инцидента, перезапуска и прогресса.
  StateChanged вызывается при значимых изменениях; подписки снимаются в OnDestroy.
- FacilityUI: UI Toolkit UXML/USS, ограниченные история и вывод, фокус, ошибки, имя выбранного хоста.

Миссия задана IncidentMissionAsset (ScriptableObject): порядок целей, тип условия, DeviceId и текст.
Чистые MissionDefinition/MissionProgress последовательно обрабатывают события инспекции и изоляции.
Другой сценарий из поддержанных условий добавляется данными, без изменения обработчиков команд.
Новые типы условий требуют кода модели; универсального визуального редактора миссий пока нет.

Загрузки из интернета, настройки чувствительности и полный контроллер gamepad пока отсутствуют.
Ввод движения с gamepad частично связан, но полный gamepad UX не заявлен.
Scene authoring выполняется один раз в Editor; геометрия сохраняется, не создаётся каждый запуск игры.

FieldCatalog/FieldSession (Simulation) отделяют контент от состояний знания и событий команд.
FieldSystemUI и KnowledgeGraphView (Runtime/Field) отображают архив/журнал в том же UIDocument.
J/K остаются в system map. Field/Terminal/Paused останавливают время, gameplay map выключена.
WatcherState — чистая модель переходов; WatcherController вычисляет видимость/движение,
читая питание и SourceIsolated той же FacilitySimulation. Дверь использует эту же изоляцию.
WorldSaveStore/WorldSaveController сохраняют весь изменяемый мир текущего участка на desktop.
FacilitySimulation/FieldSession дают валидируемые DTO; компоненты игрока/двери/робота — физические poses.
Схема, atomic replacement, backup и ограничения описаны в ADR 0003.
ComputerScreen выводит реальные процессы/последнюю команду на физические дисплеи.
OperatorVisual оставляет ноги/тело и отдельные руки видимыми в FPS; голова отбрасывает тень,
в TPS отображается целиком. Камера скрывает тело только при тесном приближении в TPS.
Контент/схемы/границы — KNOWLEDGE_ARCHITECTURE, QUEST_SYSTEM и RUNTIME_EXECUTION.

SaveLibrary адресует auto/quick/manual1..3, каждый с собственным atomic/backup store.
FacilityUI отображает отдельные страницы слотов и управления, успех записи не создаёт
постоянную подпись. Batch-тесты получают список пустых слотов без чтения пользовательских файлов.
ModularKit создаёт сохраняемую палитру FacilityModuleCatalog и URP-prefabs из CC0 assets;
геометрия/материалы не генерируются заново при запуске игры. См. ADR 0004.
