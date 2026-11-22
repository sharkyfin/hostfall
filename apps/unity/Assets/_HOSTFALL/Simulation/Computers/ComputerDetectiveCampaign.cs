namespace Hostfall.Simulation.Computers
{
    public sealed partial class ComputerCampaign
    {
        public const string DetectiveStation = "pc.detective";
        public const string DetectiveCasePath = "/home/operator/archive/verified_incident.log";
        public const string DetectiveSafe = "neuro.case-safe";
        public const string DetectiveVault = "door.sec08.memvault";

        public DetectiveSession Investigation { get; private set; }
        public bool InvestigationAccessible => FloorServiced;

        private void InitializeDetective()
        {
            Investigation = new DetectiveSession();
            BindInvestigation();
        }

        private void BindInvestigation()
        {
            Investigation.CaseVerified += AwardVerifiedCase;
            Investigation.KeyCollected += AwardArchiveKey;
            Investigation.Changed += () => Changed?.Invoke();
        }

        private void AwardVerifiedCase()
        {
            const string receipt = DetectiveStation + "/case";
            if (!receipts.Add(receipt))
                return;

            var safe = new WorldActionData { receipt = receipt, kind = WorldActionKind.DetectiveControl, target = DetectiveSafe };
            actions.Add(safe);
            Home.WriteFile(DetectiveCasePath,
                "НЕЙРОАРХИВ / ПОДТВЕРЖДЁННАЯ ХРОНОЛОГИЯ\n\n" +
                "17 ноября. 03:04 — архив опечатан. 03:07 — общий карантин. 03:17 — по шине получена подпись оператора.\n" +
                "18 ноября, 09:20. Рапорт указывает более позднюю дату инцидента, чем сверенные записи камеры и охранного пола.\n" +
                "В сейфе Ли хранится физический ключ. По шине его права передать нельзя.\n");
            messages.Add(new HomeMessageData { id = receipt, path = DetectiveCasePath, text = "Хронология подтверждена. Откройте закрытые заметки нейроархива." });
            WorldAction?.Invoke(Clone(safe));
            Changed?.Invoke();
        }

        private void AwardArchiveKey()
        {
            const string receipt = DetectiveStation + "/key";
            if (!receipts.Add(receipt))
                return;

            var gate = new WorldActionData { receipt = receipt, kind = WorldActionKind.UnlockDoor, target = DetectiveVault };
            actions.Add(gate);
            WorldAction?.Invoke(Clone(gate));
            Changed?.Invoke();
        }
    }
}
