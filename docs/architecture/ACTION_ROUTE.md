# Активная архитектура — 2026-11-08

Приоритет: ADR 0007. Старые документы о ручном терминале описывают прежний цикл.

```
InteractionScanner → ComputerTerminal → FacilityUI (показ, без TextField)
                                      ↓ Tick выбранного сеанса
                                TerminalDirector / Simulation.Screens
                                      ↓ доверенная команда
TerminalSession → FacilitySimulation/VirtualComputer → TerminalDisplay
                       ↓ завершение                     ↓
                MissionProgress + ActionState       физический экран
                                      ↑
FacilityInput → PlayerMotor / FacilityActionController → spatial validation
                   ↓                         ↓
            CharacterController       WatcherController читает эффекты
```

Simulation не знает Unity, UI не присваивает готовые результаты миссии. Unity проверяет
видимость, коллизии и ввод способностей; модель решает длительности и доступность.
Каждый сеанс адресован DeviceId, каждый противник — стабильным enemyId. Коллизии/позы
по-прежнему принадлежат Unity, логические таймеры — ActionState.

Показ не является видеозаписью: команда действительно изменяет виртуальную модель,
поэтому физический дисплей и представление при приближении камеры совпадают. Это постановочный игровой интерфейс,
не настоящая ОС. Перемещение — прыжок/бег/рывок; импульс даёт временное преимущество.
Новых постоянных шкал HUD нет: результат и отказ кратко появляются в карточке цели.

2026-11-15 · ADR 0014: WorkstationSurface задаёт углы стекла и фокус камеры;
FacilityUI проецирует нативный текст в эти границы без отдельного окна.
TerminalDirector владеет первыми тремя сеансами, ScenarioPlayback — расширенными
доверенными сценариями с сохранением шага/набора/ожидания и однократной наградой.
ComputerScreen читает тот же TerminalMemory. ServiceSwitchState хранит состояние
локальных кнопок существующего света. Старые world-v2 мигрируют без новых полей.

2026-11-17 · ADR 0015: единый DOM → shared RenderTexture → physical glass / native
composite той же текстуры после фокуса. Второго текстового представления вблизи нет.
OpeningSequence замораживает мир в Cinematic только при новой игре, затем передаёт
управление Introduction. Сохранённые кампании обходят вступление.
