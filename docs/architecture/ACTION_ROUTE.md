# Активная архитектура — 2026-11-08

Приоритет: ADR 0007. Старые документы о ручном терминале описывают прежний цикл.

```
InteractionScanner → ComputerTerminal → FacilityUI (показ, без TextField)
                                      ↓ Tick выбранного сеанса
                                TerminalDirector
                                      ↓ доверенная команда
TerminalSession → FacilitySimulation/VirtualComputer → TerminalDisplay
                       ↓ завершение                     ↓
                MissionProgress + ActionState       окно + экран
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
поэтому физический дисплей и окно совпадают. Это постановочный игровой интерфейс,
не настоящая ОС. Перемещение — прыжок/бег/рывок; импульс даёт временное преимущество.
Новых постоянных шкал HUD нет: результат и отказ кратко появляются в карточке цели.
