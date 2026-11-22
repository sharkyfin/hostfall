namespace Hostfall.Simulation.Computers
{
    public sealed partial class ComputerCampaign
    {
        public const string ContainmentStation = "pc.containment";
        public const string ContainmentTarget = "robotics.quarantine";
        public const string ContainmentAccess = "door.sec09.cloud";
        public const string ContainmentLog = "/home/operator/archive/quarantine_release.log";
        public ContainmentSession Containment { get; private set; }
        public bool ContainmentAccessible => ArchiveKeyTaken;

        public bool BeginContainment()
        {
            if (!ContainmentAccessible) return false;
            Containment.Start();
            return true;
        }

        private void InitializeContainment()
        {
            Containment = new ContainmentSession();
            BindContainment();
        }

        private void BindContainment()
        {
            Containment.Isolated += AwardContainment;
            Containment.Changed += () => Changed?.Invoke();
        }

        private void AwardContainment()
        {
            const string receipt = ContainmentStation + "/clearance";
            if (!receipts.Add(receipt)) return;
            var isolation = new WorldActionData { receipt = receipt, kind = WorldActionKind.ContainmentControl, target = ContainmentTarget };
            var passage = new WorldActionData { receipt = receipt, kind = WorldActionKind.UnlockDoor, target = ContainmentAccess };
            actions.Add(isolation);
            actions.Add(passage);
            Home.WriteFile(ContainmentLog, "RBT-01 / ЛОКАЛЬНЫЙ КАРАНТИН\n\n" +
                "Линия цеха отключена, шлюз Центрального кольца свободен. Защитный аватар ядра отвечает автономно.\n" +
                "Служба изоляции: «Оператору доступен маршрут к шлюзу. Не доверяйте приглашениям вернуть внешнюю шину».\n");
            messages.Add(new HomeMessageData { id = receipt, path = ContainmentLog, text = "Пульт карантина передал отчёт с Центрального кольца." });
            PublishRecoveryDossier();
            WorldAction?.Invoke(Clone(isolation));
            WorldAction?.Invoke(Clone(passage));
            Changed?.Invoke();
        }
    }
}
