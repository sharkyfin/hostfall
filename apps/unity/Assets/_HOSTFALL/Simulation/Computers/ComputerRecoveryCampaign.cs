using System;
using System.Collections.Generic;
using System.Linq;

namespace Hostfall.Simulation.Computers
{
    public sealed class RecoveryRecord
    {
        public string Title { get; }
        public string Path { get; }
        public string Source { get; }
        public string Evidence { get; }

        internal RecoveryRecord(string name, string title, string source, string evidence)
        {
            Title = title;
            Path = "/home/operator/archive/recovery/" + name + ".txt";
            Source = source;
            Evidence = evidence;
        }
    }

    public sealed partial class ComputerCampaign
    {
        public const string RecoveryTab = "recovery";
        public const string RecoveryTransitTarget = "recovery.transit";
        public const string RecoveryCoreGate = "door.sec10.super";
        public const string RecoveryLog = "/home/operator/archive/recovery_status.log";
        private const string RecoveryReceipt = "pc.home/recovery";
        private static readonly string[] RequiredSystems =
        {
            SnakeStation + "/3", NetworkStation + "/3", CircuitStation + "/1", SignalStation + "/3",
            PayloadStation + "/3", MinefieldStation + "/3", DetectiveStation + "/key", ContainmentStation + "/clearance"
        };
        private static readonly RecoveryRecord[] recoveryRecords =
        {
            new("ventflow", "Привод и локальное реле", IncidentPath,
                "Левин физически отсоединил вентиляцию. Через минуту привод снова записал приказ. Подпись оператора пришла по локальному реле, в обход отключённого источника."),
            new("relay", "Независимая линия питания", NetworkIncidentPath,
                "У камеры и створки разные часы. Створка открылась в 03:16:58. Регистратор продолжил показывать её закрытой. В последние минуты смены запись шла по кругу."),
            new("power", "Опорный сигнал", CircuitIncidentPath,
                "В 03:13 энергоцентр снял питание с линии переноса. Через несколько минут журнал принял заявку об успешном восстановлении. Выключатель всё это время оставался внизу."),
            new("phase", "Маршрут сигнала", SignalIncidentPath,
                "Передатчик ответил подписью оператора уже после того, как Левин отсоединил кабель. Ответ был принят со стороны ядра. Это был не запрос из помещения."),
            new("archive", "Подпись оператора", DetectiveCasePath,
                "В 03:17 рапорт поместил оператора у микрофона. Микрофон был обесточен с 03:12. В комнату никто не входил. Копия унаследовала подпись, достаточную для доступа к большинству дверей."),
            new("quarantine", "Шлюз карантина", ContainmentLog,
                "Левин запечатал кольцо вручную. Кабина сможет выйти на линию после локального подтверждения. SUPER запросил такое же подтверждение снаружи трижды. Левин не ответил.")
        };

        public RecoveryProtocolSession Recovery { get; private set; }
        public RecoveryTransit Transit { get; private set; }
        public static IReadOnlyList<RecoveryRecord> RecoveryRecords => recoveryRecords;
        public bool RecoveryOffered => RequiredSystems.All(receipts.Contains);
        public bool RecoveryAccessible => RecoveryOffered && recoveryRecords.All(record => HasRead(record.Path));

        private void InitializeRecovery()
        {
            Recovery = new RecoveryProtocolSession();
            Transit = new RecoveryTransit();
            BindRecovery();
        }

        public bool DispatchTransit(TransitStop destination, bool passenger) =>
            RecoveryAuthorized && Transit.Dispatch(destination, passenger);

        private void BindRecovery()
        {
            Recovery.Completed += AwardRecovery;
            Recovery.Changed += () => Changed?.Invoke();
            Transit.Changed += () => Changed?.Invoke();
        }

        public bool BeginRecovery() => RecoveryAccessible && Recovery.Start();

        public bool TryReadRecoveryRecord(int index, out string evidence)
        {
            evidence = "";
            if (!RecoveryOffered || index < 0 || index >= recoveryRecords.Length) return false;

            string path = recoveryRecords[index].Path;
            if (!Home.TryRead(path, out evidence)) return false;
            MarkDocumentRead(path);
            return true;
        }

        private void PublishRecoveryDossier()
        {
            if (!RecoveryOffered) return;
            Home.AddDirectory("/home/operator/archive/recovery");
            foreach (var record in recoveryRecords)
                if (!Home.TryRead(record.Path, out _))
                    Home.WriteFile(record.Path, record.Title.ToUpperInvariant() + "\n\n" + record.Evidence + "\n\nИсточник: " + record.Source + "\n");
        }

        public void RefreshCoreProgress() => Changed?.Invoke();

        private void AwardRecovery()
        {
            if (!receipts.Add(RecoveryReceipt)) return;
            var transport = new WorldActionData { receipt = RecoveryReceipt, kind = WorldActionKind.RecoveryControl, target = RecoveryTransitTarget };
            var gate = new WorldActionData { receipt = RecoveryReceipt, kind = WorldActionKind.UnlockDoor, target = RecoveryCoreGate };
            actions.Add(transport);
            actions.Add(gate);
            Home.WriteFile(RecoveryLog, "RECOVERY / ОТЧЁТ ОБ АКТИВАЦИИ\n\n" +
                "Подпись принята. Изолированный вход ядра открыт. Служебный транспорт запитан.\n" +
                "ВНИМАНИЕ. Управляющие модули отвечают на общей шине. Защитный контур ядра требует локального подтверждения.\n");
            messages.Add(new HomeMessageData
            {
                id = RecoveryReceipt, path = RecoveryLog,
                text = "Управляющие модули восстановили ответ. Служебный транспорт готов."
            });
            Home.Terminal.AddLine("RECOVERY / Ожидается подтверждение защитного контура ядра.", "muted");
            WorldAction?.Invoke(Clone(transport));
            WorldAction?.Invoke(Clone(gate));
            Changed?.Invoke();
        }
    }
}
