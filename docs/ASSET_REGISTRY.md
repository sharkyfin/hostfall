# Asset registry — 2026-11-07

| Материал | Источник / автор | Лицензия / коммерческое использование / публичный Git | Назначение / статус |
| --- | --- | --- | --- |
| Facility geometry, ServerRack, OperationsDesk, operator proxy, robot controller/optical mast, UI, field content, generated materials | Собственная работа в этом репозитории / HOSTFALL | Собственный код; лицензия репозитория владельцем ещё не выбрана, сторонних ограничений на распространение не добавлено | Используется; генерируется FacilityBuilder и сохраняется в Content |
| Начальные URP/project settings | Шаблон com.unity.template.urp-blank 17.2.6 из Editor 6000.6.8f1 / Unity Technologies | Unity Companion License для Unity-dependent projects; исходное уведомление сохранено в docs/licenses/Unity-URP-Template-LICENSE.md | Использованы как начальная конфигурация, адаптированы и сохранены редактором |
| Roboto Mono Regular + OFL.txt | [googlefonts/RobotoMono](https://github.com/googlefonts/RobotoMono) / Roboto Mono Project Authors, Christian Robertson | SIL OFL 1.1; коммерческое использование и включение исходного font с OFL разрешены | Импортирован TTF, моноширинный шрифт команд, кода и дисплеев уровня; файл лицензии рядом |
| URP, Input System, Cinemachine, ProBuilder, Test Framework | Unity official package registry / Unity Technologies | Условия соответствующих Unity packages; не копируются отдельно в Git, получаются Package Manager | Зависимости проекта; ProBuilder для дальнейшего ручного моделирования |
| Modular Sci-Fi Megakit (free subset) | [Quaternius](https://quaternius.com/packs/modularscifimegakit.html) / Quaternius | CC0; коммерческое использование и публикация CC0 файлов разрешены | Standard импортирован: 16 FBX (Unity) и trim textures, CC0; URP материалы подготовлены здесь. Платные Pro/Source не использованы |
| Ultimate Modular Sci-Fi Pack | [Quaternius](https://quaternius.com/packs/ultimatemodularscifi.html) / Quaternius | CC0; коммерческое использование и публичное включение разрешены | Исследован; не импортирован |
| Kenney asset library | [Kenney](https://kenney.nl/assets) / Kenney | Лицензия конкретного пакета должна быть проверена перед импортом | Не импортирован; доступ страницы через web был ограничен |
| Poly Haven library | [Poly Haven license](https://polyhaven.com/license) / автор конкретного файла | CC0 для assets; проверить downloaded LICENSE каждого импорта | Выбраны и импортированы три PBR props; точный состав ниже |
| ambientCG | [ambientCG license](https://docs.ambientcg.com/license/) / Lennart Demes | CC0; каждый импорт должен иметь provenance | Кандидат; не импортирован |
| Asset Store Modular SciFi / Free Sci-Fi Office, Sketchfab | Конкретные позиции не выбраны | Права на публичное распространение не подтверждены | Не импортировать без проверки конкретной лицензии |
| Noto Sans Regular + OFL-NotoSans.txt | [notofonts/noto-fonts](https://github.com/notofonts/noto-fonts) / Noto Project Authors | SIL OFL 1.1, коммерческое включение/публичный Git разрешены; LICENSE рядом с TTF | Основной шрифт чтения, кириллица; исходный TTF без изменения |
| Paint/concrete/floor maps + normals, beveled mesh, suit/gloves/boots/chairs/keycaps | Оригинальные процедурные assets HOSTFALL | Собственный код и результат; сторонних bitmap/model assets нет | tools/generate-surfaces.py (seed 4717); FacilityArt/FacilityBuilder. Сохранены PNG, mesh/prefab/scene |

В source включена выбранная часть Quaternius Standard с License_Standard.txt и PROVENANCE.json.
Старые исследования других наборов не означают их импорт.
Roboto Mono включён в интерфейс и надписи уровня; кириллица не зависит от системных шрифтов.



Noto Sans SHA256: `b85c38ecea8a7cfb39c24e395a4007474fa5a4fc864f6ee33309eb4948d232d5`.
Поверхности — исходные детерминированные карты, не генеративные сторонние изображения.

Quaternius Standard получен из публикации самого автора на OpenGameArt, архив проверен:
`4ca9acbc7a13e7baa48e24c4853b779889b7ab21587004c36668826e705c8a10`.
Источник/точный состав/производные карты: Assets/_HOSTFALL/Art/QuaterniusSciFi/PROVENANCE.json.
Исходные FBX/PNG сохранены, ORM преобразованы в отдельные URP MetalSmooth/Occlusion PNG.
Материалы, нормализованные префабы и расположение в уровне — локальная работа.

## Дополнительное исследование 2026-11-07

[Каталог 24 кандидатов](research/2026-11-07-environment/asset-candidates.csv) и
[оценка сочетаемости/света/геометрии](research/2026-11-07-environment/RESEARCH.md).
Восемь Poly Haven glTF измерены, но не импортированы. Остальные позиции имеют только
проверку первичной страницы; это не подтверждённые готовые Unity assets. Бесплатные
Asset Store наборы используют EULA и не являются CC0. При импорте хранить точный
архив, его лицензию, источник и checksum отдельно от общей записи о библиотеке.

Оригинальный концепт northline-direction-v1.png создан встроенным image_gen. Сохранён
в docs/research/2026-11-07-environment с полным промптом. Только художественное
направление; не используется в runtime, не представляет извлечённые игровые assets.
Демонстрационные рендеры сторонних сайтов не включены в репозиторий как CC0-материалы.


## Импорт умеренного окружения — 2026-11-07

| Материал | Автор / источник | Лицензия | Статус |
| --- | --- | --- | --- |
| Office Notepads | Ulan Cabanilla / https://polyhaven.com/a/office_notepads | CC0-1.0 | FBX + 1K карты; в сцене только один блокнот |
| Industrial Storage Cart | Jule Bielitz / https://polyhaven.com/a/industrial_storage_cart | CC0-1.0 | FBX + 1K карты; одна тележка с коллизией |
| Modular Industrial Pipes 01 | Jorge Camacho / https://polyhaven.com/a/modular_industrial_pipes_01 | CC0-1.0 | FBX + 1K карты; в сцене одна секция |

Art/PolyHavenEngineering содержит исходники, LICENSE.txt, URL/SHA256/derived formulas в
PROVENANCE.json каждого импорта. MD5 сверены с официальным манифестом при скачивании.
Демонстрационные рендеры сайта не импортированы. Роботы только исследованы:
[shortlist](research/2026-11-07-environment/ROBOTS.md), [измерения](research/2026-11-07-environment/robot-candidates.json).

## Два анимированных робота — 2026-11-07

Quaternius Sci-Fi Essentials **Standard**, исходная публикация автора:
[OpenGameArt](https://opengameart.org/content/sci-fi-essentials-kit),
[страница Quaternius](https://quaternius.com/packs/scifiessentialskit.html).
CC0-1.0, коммерческое включение/публичные source assets разрешены. Импортированы только
Enemy_QuadShell и Enemy_Trilobite, их восемь карт и лицензия. Платный Source не использован.
Art/QuaterniusRobots/PROVENANCE.json хранит точные URL двух ZIP, SHA256, состав и преобразования.

Исходные FBX/анимации/текстуры — Quaternius. Нормализация масштаба, URP материалы,
MetalSmooth/Occlusion, AnimatorController, оптическая мачта, AI и сохранение — локальная
работа HOSTFALL (Editor/RobotArt.cs, World/WatcherController.cs). В scene ровно два экземпляра.
EyeDrone и другие модели shortlist не включены. Сторонних executable/scripts нет.
