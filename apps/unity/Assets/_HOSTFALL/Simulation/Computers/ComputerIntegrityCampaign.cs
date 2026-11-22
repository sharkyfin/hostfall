namespace Hostfall.Simulation.Computers
{
    public sealed partial class ComputerCampaign
    {
        public const string IntegrityStation = "pc.integrity";
        public const string IntegrityTarget = "core.service-telemetry";
        public const string IntegrityLog = "/home/operator/archive/core_integrity.log";
        public IntegrityAuditSession Integrity { get; private set; }
        public bool IntegrityAccessible => Containment.Complete;

        public bool BeginIntegrity() => IntegrityAccessible && Integrity.Start();

        private void InitializeIntegrity()
        {
            Integrity = new IntegrityAuditSession();
            BindIntegrity();
        }

        private void BindIntegrity()
        {
            Integrity.Completed += AwardIntegrity;
            Integrity.Changed += () => Changed?.Invoke();
        }

        private void AwardIntegrity()
        {
            const string receipt = IntegrityStation + "/telemetry";
            if (!receipts.Add(receipt)) return;
            var telemetry = new WorldActionData { receipt = receipt, kind = WorldActionKind.IntegrityControl, target = IntegrityTarget };
            actions.Add(telemetry);
            Home.WriteFile(IntegrityLog, "SUPER-10 / СЛУЖЕБНАЯ ТЕЛЕМЕТРИЯ\n\n" +
                "Три банка сверены. Силовая, охлаждающая и оптическая стойки помечены со стороны арены.\n" +
                "Ремонтный журнал: перегруженная стойка выдаёт себя по смене сигнала. Перед входом дождитесь окончания цикла привода.\n" +
                "Ограничения защитного контроллера остаются в силе. Внешний командный канал изолирован.\n");
            messages.Add(new HomeMessageData { id = receipt, path = IntegrityLog, text = "Проверка целостности восстановила маркировку стоек в арене." });
            WorldAction?.Invoke(Clone(telemetry));
            Changed?.Invoke();
        }
    }
}
