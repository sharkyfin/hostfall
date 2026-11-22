using System;
using System.Collections.Generic;

namespace Hostfall.Simulation.Computers
{
    public enum DesktopStyle { Classic98, LunaXp, Plasma, Cupertino, Aero, Gnome, Fluent, Arch, Buffer, Storage, Dispatch, Audit }
    public sealed class ComputerApplicationDefinition
    {
        public string Station { get; }
        public string App { get; }
        public string Title { get; }
        public string Location { get; }
        public DesktopStyle Style { get; }
        public string Instructions { get; }
        public ComputerApplicationDefinition(string station, string app, string title, string location, DesktopStyle style, string instructions)
        { Station = station; App = app; Title = title; Location = location; Style = style; Instructions = instructions; }
    }
    public static class ComputerApplications
    {
        private static readonly ComputerApplicationDefinition[] implemented =
        {
            new(ComputerCampaign.SnakeStation,"snake","VENTFLOW","Операторская / вентиляция",DesktopStyle.Classic98,
                "Стрелки / WASD — направление. Четыре пакета на этап. Золотой пакет подтверждает диагностику.\n1 / Закрыть заслонку\n2 / Подать питание двери\n3 / Разблокировать проход\nСтолкновение сбрасывает только текущий этап. Esc позволяет немедленно отойти. Не игнорируйте звук воздуховода."),
            new(ComputerCampaign.NetworkStation,"netdef","NETWORK DEFENSE","Серверная / охранная сеть",DesktopStyle.Gnome,
                "Выберите узел. Защита использует CPU; изоляция удерживает процессы и тоже стоит 1 CPU. Всего доступно четыре единицы.\nПереключайте маршруты до CORE, распределяйте CPU и очищайте заражение. При 80% заражения защита узла перестаёт работать. Очистка стоит 2 заряда; они восстанавливаются.\nТри волны приходят через разные входы: UPLINK; затем RELAY; затем COMMAND и VAULT.\n1 / Командная связь роботов\n2 / Временный сетевой сбой\n3 / Отмена подкреплений, доступ к хранилищу и энергоцентру.\nЛокальное зрение и слух роботов сохраняются. Esc — немедленно отойти."),
            new(ComputerCampaign.CircuitStation,"circuit","CIRCUIT BREAKER","Энергоцентр / распределение питания",DesktopStyle.Plasma,
                "Выберите контакт и поверните его. Проводники соединяются только встречными концами. Реле размыкают ветви. Подключите источники: главный 6, резервный 3 единицы.\nСуммарная нагрузка ветви не должна превышать мощность подключённых источников. Объединение шин возможно через реле TIE.\nСвет, главный шлюз, защитная турель, вентиляция и служебный обход в операторскую требуют питания одновременно. На всё мощности не хватит.\nИзменение схемы снимает напряжение. После перегрузки устраните причину, дождитесь охлаждения и вручную сбросьте автомат.\nПитание действует в реальном помещении и сохраняется после выхода. Esc — немедленно отойти."),
            new(ComputerCampaign.SignalStation,"signal","SIGNAL SYNC","Квантовый узел / фазовое поле",DesktopStyle.Fluent,
                "Совместите три цветных сигнала с серым эталоном. Частота задаёт скорость дрейфа, подстройка фазы ограничена ±90°, инверсия меняет полярность на 180°. Включите все три канала.\nУдерживайте совпадение без разрыва: три калибровки с уменьшающимся допуском и временным окном. Ошибка вызывает перезарядку.\nВентиляция из CIRCUIT BREAKER охлаждает установку. Без неё нагрев продолжается и после выхода из окна.\nФинальная настройка снимает реальное поле на 20 с. Настройку можно повторить без потери прогресса; с северной стороны есть кнопка аварийного возврата. Esc — немедленно отойти."),
            new(ComputerCampaign.MinefieldStation, "minefield", "MINEFIELD", "SEC-07 / сенсорный зал", DesktopStyle.Aero,
                "Испытайте три конфигурации реального пола. Число показывает количество датчиков в восьми соседних клетках. Первая область открывается со стороны пульта.\n" +
                "ЛКМ или Enter открывает клетку. ПКМ или F ставит флаг. Повторное открытие числа проверяет соседей, если флагов столько же, сколько указано.\n" +
                "Правильный флаг отключает соответствующий датчик. Откройте все безопасные клетки и отметьте все датчики. Ошибка поднимает тревогу, повтор сохраняет уже завершённые уровни.\n" +
                "После третьего уровня пройдите через пол на север. За постом наблюдения откроется проход в кабинет нейроархива."),
            new(ComputerCampaign.DetectiveStation, "detective", "DIGITAL DETECTIVE", "Нейроархив / кабинет Ли", DesktopStyle.Cupertino,
                "Откройте материалы в архиве, почте, кадрах, заметках и записях. Сохраните нужные улики в дело. Расставьте четыре события и укажите источник для каждого. Сверьте фактическую дату с рапортом."),
            new(ComputerCampaign.ContainmentStation, "containment", "CONTAINMENT", "RBT-01 / локальный карантин", DesktopStyle.Arch,
                "Выберите маршрут R26 и следите за ним на карте и на камерах. Перевод в камеру держит патруль между двумя створками. " +
                "Опустите створки, дождитесь устойчивого сигнала датчиков и изолируйте местный контур. Если робот выведен из строя, датчик связи подтвердит остановку."),
            new(ComputerCampaign.BufferStation, "buffer", "BUFFER RECALL", "EDGE-01 / память прохода", DesktopStyle.Buffer,
                "Посмотрите последовательность импульсов. Запомните клетки, отмеченные сплошным кругом. Шум помечен крестом. " +
                "После записи повторите импульсы по порядку. Запись можно посмотреть снова, а ошибочный ответ отредактировать. " +
                "Три серии откроют ремонтный шкаф в проходе."),
            new(ComputerCampaign.FragmentStation, "fragments", "FRAGMENT DOCK", "DATA-01 / таблица тома", DesktopStyle.Storage,
                "Уложите пять фрагментов в левый раздел. Тёмные клетки с × заняты загрузчиком. Две правые колонки зарезервированы для журнала. " +
                "Поверните блок, затем выберите место для клетки с ромбом. Сохранение размещения откроет оружейную SEC-02."),
            new(ComputerCampaign.QuorumStation, "quorum", "QUORUM DISPATCH", "CLOUD-09 / свидетели", DesktopStyle.Dispatch,
                "Назначьте шесть задач трём свидетелям в пределах семи тактов. Каждая задача показывает срок, длительность и допуск. " +
                "Одновременно работают не более двух свидетелей. Зависимые подписи стартуют после завершения всех своих источников. " +
                "Подтверждённое расписание разрешит открыть западный обход в логистику."),
            new(ComputerCampaign.IntegrityStation, "integrity", "INTEGRITY AUDIT", "SUPER-10 / проверка изоляции", DesktopStyle.Audit,
                "В каждом из трёх банков повреждена одна страница. Выберите начало и конец диапазона и сравните контрольную сумму. " +
                "Сужайте область по результату. Когда останется единственная страница, изолируйте её. Лимит проб относится к текущему банку. " +
                "Завершённая диагностика подсветит обслуживаемые узлы в арене."),
            new(ComputerCampaign.PayloadStation,"payload","PAYLOAD RUNNER","Логистика / грузовой комплекс",DesktopStyle.LunaXp,
                "A/D или ←/→ — бег. Пробел / ↑ — прыжок. Соберите три контейнера и доберитесь до приёмника.\nФлажок — контрольная точка. Смотрите на период лазеров; робота можно перепрыгнуть.\n1 / Подъёмник\n2 / Конвейер\n3 / Мост\nПосле третьего цикла система охраны поднимет тревогу. Esc — отойти; игра сохраняет точное состояние.")
        };
        public static IReadOnlyList<ComputerApplicationDefinition> Implemented => implemented;
        public static ComputerApplicationDefinition Find(string station) => Array.Find(implemented, d => d.Station == station);
    }
    public sealed partial class ComputerCampaign
    {
        public const string PayloadStation = "pc.payload";
        public PayloadSession Payload { get; private set; }
        public LogisticsSystem Logistics { get; private set; }
        // An already started v3 preview session retains access if it predates SIGNAL SYNC.
        public bool PayloadAccessible => FirstTaskComplete &&
            (PhaseCalibrated || Payload.Stage > 0 || Payload.Phase == PayloadPhase.Running);
        private void InitializeApplications()
        {
            InitializeNetwork();
            InitializeCircuit();
            InitializeSignal();
            InitializeMinefield();
            InitializeDetective();
            InitializeContainment();
            InitializeBuffer();
            InitializeFragmentDock();
            InitializeQuorum();
            InitializeIntegrity();
            InitializeRecovery();
            Payload = new PayloadSession();
            Logistics = new LogisticsSystem();
            BindPayload();
        }
        private void BindPayload()
        {
            Payload.StageCompleted += AwardPayloadStage;
            Payload.Changed += () => Changed?.Invoke();
        }
        public bool BeginPayload()
        {
            if (!PayloadAccessible) return false;
            Payload.Start();
            return true;
        }
        private void AwardPayloadStage(int stage)
        {
            string receipt = PayloadStation + "/" + stage;
            if (!receipts.Add(receipt)) return;
            Logistics.Activate(stage);
            var emitted = new List<WorldActionData>();
            void Add(WorldActionKind kind, string target)
            {
                var action = new WorldActionData { receipt = receipt, kind = kind, target = target };
                actions.Add(action); emitted.Add(action);
            }
            Add(WorldActionKind.StartMechanism, stage == 1 ? "logistics.lift" : stage == 2 ? "logistics.conveyor" : "logistics.bridge");
            if (stage == 3)
            {
                Add(WorldActionKind.RaiseAlarm, "logistics.alarm");
                Add(WorldActionKind.UnlockDoor, "door.sec06.robo");
                string path = "/home/operator/archive/payload_transfer.log";
                Home.WriteFile(path, "PAYLOAD TRANSFER / журнал 0317\n\nКонтейнеры не содержали людей. Три пакета — образцы памяти одного оператора.\nИнженер Левин остановил мост, когда прочитал адрес назначения: SUPER / persistent identity.\nСлужба карантина продолжает искать носитель.\n");
                messages.Add(new HomeMessageData { id = receipt, path = path, text = "Восстановлен журнал грузового переноса." });
            }
            foreach (var action in emitted) WorldAction?.Invoke(Clone(action));
            Changed?.Invoke();
        }
    }
}
