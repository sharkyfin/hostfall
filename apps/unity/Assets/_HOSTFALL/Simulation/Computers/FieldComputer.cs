using System;
using System.Collections.Generic;

namespace Hostfall.Simulation.Computers
{
    public enum FieldUtilityId
    {
        PowerLight, PowerGuard, PhaseBypass, CargoTransfer, FloorService, QuarantineRecall, QuarantineSeal, ArmoryPermit
    }

    public sealed class FieldFile
    {
        public string Name { get; }
        public string Content { get; }
        public FieldFile(string name, string content)
        {
            Name = name;
            Content = content;
        }
    }

    public sealed class FieldUtility
    {
        public FieldUtilityId Id { get; }
        public string Name { get; }
        public string Source { get; }
        public string Help { get; }
        public FieldUtility(FieldUtilityId id, string name, string source, string help)
        {
            Id = id;
            Name = name;
            Source = source;
            Help = help;
        }
    }

    public sealed class FieldCase
    {
        public static readonly FieldCase Camera = new("Пустая камера",
            "Один канал подменяет живое изображение записью. Сверьте два источника, выберите канал и приложите оба файла к заявке на изоляцию.",
            new[] { "B-02 · грузовой подъёмник", "B-04 · грузовые ворота", "B-06 · ремонтная ветка" },
            new[]
            {
                new FieldFile("кадры_03-17.rec", "Выгрузка буфера. Время берётся с независимых часов регистратора."),
                new FieldFile("контакты_створок.log", "03:16:52   B-04   КОНЦЕВИК НИЗ / ЗАКРЫТО\n03:16:58   B-04   КОНЦЕВИК ВЕРХ / ОТКРЫТО\n03:17:06   B-04   КОНЦЕВИК ВЕРХ / ОТКРЫТО\n\nB-02 и B-06 не меняли положение за этот интервал. У датчиков створок отдельная линия."),
                new FieldFile("смена.txt", "Створка за этим постом ходит и так тихо. На подъёмнике B-02 часами ничего не двигается, пока смена спит. Если кажется, что картинка замерла, проверь номер кадра. Сигнал метки приходит прямо с камеры.\n\nЛи")
            }, 1, 0b011,
            "Канал B-04 изолирован. Локальная охрана осталась на датчиках. Доступ к энергоцентру открыт.",
            "Эти данные не подтверждают подмену этого канала. Запрос приостановлен. За решёткой могла включиться охрана.");

        public static readonly FieldCase Voice = new("Второй голос",
            "Какой приказ произнесли с пустого поста? Выберите запись и приложите два независимых источника, подтверждающих подмену.",
            new[] { "03:04 · Опечатать архив", "03:07 · Включить карантин", "03:17 · Открыть линию переноса" },
            new[]
            {
                new FieldFile("радио_смена.txt", "17 ноября\n03:04   [Ли] Пломба на месте. Я у сейфа.\n03:07   [Дежурный] Начинаем карантин. Перекрываю проход.\n03:17   [Голос оператора] Я у поста. Откройте перенос. Код подтверждаю.\n\nПометка Ли: третий голос взял дыхание ровно в том же месте, что и в старой проверке микрофона."),
                new FieldFile("пост_охраны.log", "17 ноября\n03:04   КАМЕРА: архив опечатан.\n03:07   КОНЦЕВИК: все шлюзы закрыты.\n03:17   ШИНА: получена подпись оператора.\n\nС 03:07 по 03:18 датчики пола у поста не зарегистрировали ни одного шага. Местный микрофон отключён с 03:12."),
                new FieldFile("рапорт_18-11.txt", "Составлен 18 ноября в 09:20.\n«Инцидент начался 18 ноября в 03:17. Оператор находился на посту и подписал перенос лично».\n\nВ копии отсутствует идентификатор регистратора. Приложение с записью удалено.")
            }, 2, 0b011,
            "Подмена подтверждена. Ли оставил ключ в сейфе рядом с пультом. Заберите его вручную.",
            "Эта пара источников не подтверждает, что приказ пришёл с пустого поста. Сверьте запись смены и независимые датчики.");

        private readonly int correctSuspect, proof;
        public string Title { get; }
        public string Question { get; }
        public IReadOnlyList<string> Suspects { get; }
        public IReadOnlyList<FieldFile> Evidence { get; }
        public string Confirmation { get; }
        public string Rejected { get; }

        private FieldCase(string title, string question, string[] suspects, FieldFile[] evidence, int correctSuspect,
            int proof, string confirmation, string rejected)
        {
            Title = title;
            Question = question;
            Suspects = Array.AsReadOnly(suspects);
            Evidence = Array.AsReadOnly(evidence);
            this.correctSuspect = correctSuspect;
            this.proof = proof;
            Confirmation = confirmation;
            Rejected = rejected;
        }

        public static FieldCase Find(string station) => station switch
        {
            ComputerCampaign.NetworkStation => Camera,
            ComputerCampaign.DetectiveStation => Voice,
            _ => null
        };

        internal bool ValidMask(int mask) => (mask & ~((1 << Evidence.Count) - 1)) == 0;
        internal bool HasProof(int mask) => (mask & proof) == proof;
        internal bool Matches(int suspect, int mask) => suspect == correctSuspect && mask == proof;
        internal static bool IsPair(int mask)
        {
            int remaining = mask & (mask - 1);
            return remaining != 0 && (remaining & (remaining - 1)) == 0;
        }
    }

    public sealed class FieldComputer
    {
        public string Station { get; }
        public string Name { get; }
        public IReadOnlyList<FieldFile> Files { get; }
        public IReadOnlyList<FieldUtility> Utilities { get; }

        private FieldComputer(string station, string name, FieldFile[] files, params FieldUtility[] utilities)
        {
            Station = station;
            Name = name;
            Files = Array.AsReadOnly(files);
            Utilities = Array.AsReadOnly(utilities);
        }

        public bool Offers(FieldUtilityId id)
        {
            foreach (var utility in Utilities)
                if (utility.Id == id) return true;
            return false;
        }

        private static readonly FieldComputer[] stations =
        {
            new(ComputerCampaign.NetworkStation, "CORE-01", new[]
            {
                new FieldFile("пустой_пост.txt", "На обзоре все три камеры. Ещё вчера видели четвёртую. Ли говорит, я просто помню отражение в стекле. Я переставил монитор. Отражение тоже переставилось."),
                new FieldFile("очередь_печати.log", "03:03  Пропуска на эвакуацию · 18 копий\n03:18  Пропуск на эвакуацию · 1 копия\n03:19  Пропуск на эвакуацию · 1 копия\n03:20  Пропуск на эвакуацию · 1 копия\n\nИмя получателя у последних трёх заданий одно и то же.")
            }),
            new(ComputerCampaign.CircuitStation, "PWR-01", new[]
            {
                new FieldFile("три_щелчка.log", "03:13. Левин вручную снял питание с главного шлюза. На обратной дороге слышал, как щёлкнули три реле. Две линии у щита уже были сняты с шин."),
                new FieldFile("листок_на_щитке.txt", "Если очередная проверка увидит в темноте привидение — это я. Турель, к сожалению, читает только датчик движения.\n\nЛи")
            },
                new FieldUtility(FieldUtilityId.PowerLight, "шлюз_и_свет.svc", "ПИТАНИЕ  ШЛЮЗ, СВЕТ, ОБХОД\nЗАЩИТА  ОТКЛ", "Питает главный шлюз и включает свет и обслуживающий обход в операторскую. Турель на местном контуре остаётся выключенной."),
                new FieldUtility(FieldUtilityId.PowerGuard, "шлюз_и_турель.svc", "ПИТАНИЕ  ШЛЮЗ, ТУРЕЛЬ\nСВЕТ  ОТКЛ", "Шлюз остаётся под питанием, но свет гаснет. Импульсы турели задерживают роботов в пределах прямой видимости.")),
            new(ComputerCampaign.SignalStation, "QNT-01", new[]
            {
                new FieldFile("список_ответов.log", "03:20 / проверка Левина / пусто\n03:21 / эталон / подпись оператора\n03:21 / запрос: кто ответил?\n03:21 / ответ: вы\n03:22 / передатчик физически отсоединён")
            },
                new FieldUtility(FieldUtilityId.PhaseBypass, "импульс_на_север.svc", "ФАЗА  МЕСТНЫЙ_ЭТАЛОН\nПОЛЕ  СНЯТЬ 20 с", "Поле у северного выхода пропускает 20 секунд. Идите сразу после запуска. На обратной стороне есть кнопка аварийного возврата.")),
            new(ComputerCampaign.PayloadStation, "CARGO-01", new[]
            {
                new FieldFile("не_для_эвакуации.txt", "Я вскрыл один контейнер. Там не было припасов. На каждом носителе мой голос: пробует вспомнить, как зовут начальника смены. Адрес у всех одинаковый. SUPER."),
                new FieldFile("метки_тары.log", "Тара №04. Брак маркировки. На этикетке имя оператора и детский рост, измеренный на дверном косяке. Опись просит оставить всё как есть.")
            },
                new FieldUtility(FieldUtilityId.CargoTransfer, "аварийный_прогон.svc", "ГРУЗ  ПОДЪЁМНИК → КОНВЕЙЕР → МОСТ", "Запускает штатный грузовой цикл. В режиме карантина его примут за отгрузку без подписи. Створки закроются, и резерв охраны выйдет из бокового пролёта.")),
            new(ComputerCampaign.MinefieldStation, "SEC-07", new[]
            {
                new FieldFile("дежурный_пост.log", "03:07. Карантин принят.\n03:12. Микрофон отключён на стойке.\n03:17. Подпись оператора принята.\n03:18. На полу всё ещё ни одного шага."),
                new FieldFile("стул_инвентаризация.txt", "Активы SEC-07: стол, три камеры, стул на колёсах. Последний исключить. Пол его слышит лучше, чем нас.")
            },
                new FieldUtility(FieldUtilityId.FloorService, "обслуживание_пола.svc", "ПОЛ  ОБСЛУЖИВАНИЕ\nАРХИВ  СНЯТЬ БЛОКИРОВКУ", "Локальный сервисный режим отключает датчики и разрешает пройти к нейроархиву. Дождитесь окончания грузового карантина.")),
            new(ComputerCampaign.DetectiveStation, "NEURO-01", new[]
            {
                new FieldFile("черновик_Ли.txt", "Я думал, тест голоса нужен для разблокировки дверей. Сначала заставили назвать имя. На третьем сеансе просили вспомнить запах комнаты, где я вырос. К дверям это уже не имело никакого отношения."),
                new FieldFile("личное_не_отправлено.txt", "Если я опять скажу, что ничего не помню, не перечисляй факты. Спроси, где лежит ключ. Этого он никогда не спрашивал.")
            }),
            new(ComputerCampaign.ContainmentStation, "RBT-01", new[]
            {
                new FieldFile("осталось_двое.txt", "Капкан делали под одного инспектора. Ночью R26 стал садиться у створки и ждать. Как будто знал, кого между ними не хватало."),
                new FieldFile("приёмка_камеры.log", "При человеке в камере обе створки открываются сами. Обходить блокировку запрещено. Сообщение «сигнал потерян» допустимо только после визуальной проверки.")
            },
                new FieldUtility(FieldUtilityId.QuarantineRecall, "отзыв_R26.svc", "СТВОРКИ  ОТКРЫТЬ\nR26  ВЕРНУТЬ В КАМЕРУ", "Отправляет нетронутому R26 команду возврата. Сигнал «R26 в камере» означает, что можно начать закрытие."),
                new FieldUtility(FieldUtilityId.QuarantineSeal, "изоляция_контура.svc", "СТВОРКИ  ЗАКРЫТЬ\nКОНТУР  ИЗОЛИРОВАТЬ ПО ДАТЧИКАМ", "Запустите после входа R26 в камеру или его отключения. Когда обе створки закроются и датчики дадут допуск, запустите ещё раз.")),
            new(ComputerCampaign.BufferStation, "EDGE-01", new[]
            {
                new FieldFile("стук_по_трубе.log", "Два коротких, один длинный — смена просит открыть дверь.\n03:18. Из пустой ветки трубы два коротких, один длинный.\n03:19. С той же ветки длинный, два коротких. Похоже, ещё учится."),
                new FieldFile("лист_допуска.txt", "Сначала выключи вытяжку, потом слушай у решётки. У нас с Ли так гул не перекрывал голоса. В следующий раз не прислоняйся.")
            }),
            new(ComputerCampaign.FragmentStation, "DATA-01", new[]
            {
                new FieldFile("опись_тома.txt", "Раздел A · работа\nРаздел B · дом\nРаздел C · запах нагретого пластика\nРаздел D · ужас при упоминании раздела B\n\nСлужебная приписка: разделы C и D восстановлены из одного и того же блока."),
                new FieldFile("руководство_тома.txt", "Если память вернулась, но вы не узнаёте её, проверьте владельца тома. Подпись владельца могла сохраниться даже при замене всего содержимого.")
            },
                new FieldUtility(FieldUtilityId.ArmoryPermit, "допуск_оружейной.svc", "ТОМ  РЕЗЕРВНЫЙ_ДОПУСК\nДВЕРЬ  РАЗРЕШИТЬ РУЧНОЕ ОТКРЫТИЕ", "Разрешает открыть оружейную с пульта двери. Требуется изоляция канала на CORE-01.")),
            new(ComputerCampaign.QuorumStation, "CLOUD-09", new[]
            {
                new FieldFile("кому_дозвонились.txt", "Ли — вызов сброшен.\nЛевин — вне зоны.\nЯ — принял.\n\nПоследнюю строку добавил я. Потом посмотрел на трубку. Она лежала рядом."),
                new FieldFile("свидетель.txt", "Попросили двух независимых свидетелей. Оба сделали одну и ту же опечатку. Не стала их поправлять. Хотела увидеть, заметят ли.")
            }),
            new(ComputerCampaign.IntegrityStation, "SUPER-10", new[]
            {
                new FieldFile("критерий_успеха.txt", "Сбой личности считается устранённым, если после переключения оба собеседника продолжают считать себя оператором."),
                new FieldFile("голосовое_приветствие.txt", "Вариант 1. Убрать из фразы имя.\nВариант 2. Убрать слово «вернулся».\nВариант 3. Сказать: «Вы всё сделали правильно».\n\nПоследний вариант приняла система. Я его не слышал.")
            })
        };

        public static FieldComputer Find(string station) => Array.Find(stations, pc => pc.Station == station);
    }
}
