namespace Hostfall.Simulation.Computers
{
    public sealed partial class ComputerCampaign
    {
        public const string QuorumStation = "pc.quorum";
        public const string QuorumShortcut = "door.loop5.shortcut";
        public const string QuorumLog = "/home/operator/archive/witness_dispatch.log";
        public QuorumDispatchSession Quorum { get; private set; }
        public bool QuorumAccessible => Containment.Complete;

        public bool BeginQuorum() => QuorumAccessible && Quorum.Start();

        private void InitializeQuorum()
        {
            Quorum = new QuorumDispatchSession();
            BindQuorum();
        }

        private void BindQuorum()
        {
            Quorum.Completed += AwardQuorum;
            Quorum.Changed += () => Changed?.Invoke();
        }

        private void AwardQuorum()
        {
            const string receipt = QuorumStation + "/witness";
            if (!receipts.Add(receipt)) return;
            var shortcut = new WorldActionData { receipt = receipt, kind = WorldActionKind.UnlockDoor, target = QuorumShortcut };
            actions.Add(shortcut);
            Home.WriteFile(QuorumLog, "CLOUD-09 / НЕЗАВИСИМЫЕ СВИДЕТЕЛИ\n\n" +
                "Реле и архив подтвердили расписание. Открыт западный обход из Центрального кольца в логистику.\n" +
                "Сверка часов разделила события: запись допуска сделана до аварии, её повторная передача пришла после карантина. " +
                "Один идентификатор встречается в обеих записях. Свидетель физического поста пометил вторую как копию.\n");
            messages.Add(new HomeMessageData { id = receipt, path = QuorumLog, text = "Кворум подтвердил западный маршрут через диспетчерскую." });
            WorldAction?.Invoke(Clone(shortcut));
            Changed?.Invoke();
        }
    }
}
