namespace Hostfall.Simulation.Computers
{
    public sealed partial class ComputerCampaign
    {
        public const string FragmentStation = "pc.fragments";
        public const string FragmentArmory = "door.sec02.armory";
        public const string FragmentLog = "/home/operator/archive/drive_manifest.log";
        public FragmentDockSession Fragments { get; private set; }
        public bool FragmentAccessible => CameraIsolated;

        public bool BeginFragmentDock() => FragmentAccessible && Fragments.Start();

        private void InitializeFragmentDock()
        {
            Fragments = new FragmentDockSession();
            BindFragmentDock();
        }

        private void BindFragmentDock()
        {
            Fragments.Completed += AwardFragmentDock;
            Fragments.Changed += () => Changed?.Invoke();
        }

        private void AwardFragmentDock()
        {
            const string receipt = FragmentStation + "/volume";
            if (!receipts.Add(receipt)) return;
            var armory = new WorldActionData { receipt = receipt, kind = WorldActionKind.UnlockDoor, target = FragmentArmory };
            actions.Add(armory);
            Home.WriteFile(FragmentLog, "DATA-01 / МАНИФЕСТ ФРАГМЕНТОВ\n\n" +
                "Восстановлена таблица местного доступа. Оружейная SEC-02 переведена на резервный ключ.\n" +
                "В той же таблице четыре регистрации обслуживающего персонала. Последняя имеет два носителя: протокол допуска и образ памяти. Оба подписаны одним идентификатором.\n");
            messages.Add(new HomeMessageData { id = receipt, path = FragmentLog, text = "DATA-01 восстановил манифест хранилища и доступ в оружейную." });
            WorldAction?.Invoke(Clone(armory));
            Changed?.Invoke();
        }
    }
}
