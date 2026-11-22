namespace Hostfall.Simulation.Computers
{
    public sealed partial class ComputerCampaign
    {
        public const string MinefieldStation = "pc.minefield";
        public const string MinefieldIncidentPath = "/home/operator/archive/security_floor.log";
        public const string MinefieldExit = "door.sec07.sensor-exit";
        public const string MinefieldNeuroPass = "door.sec07.neuro-pass";
        public const string MinefieldFloor = "security.sensor-floor";

        public MinefieldSession Minefield { get; private set; }
        public bool MinefieldAccessible => CargoReleased;

        private void InitializeMinefield()
        {
            Minefield = new MinefieldSession();
            BindMinefield();
        }

        private void BindMinefield()
        {
            Minefield.StageCompleted += AwardMinefieldStage;
            Minefield.Changed += () => Changed?.Invoke();
        }

        public bool BeginMinefield() => MinefieldAccessible && Minefield.Begin();

        private void AwardMinefieldStage(int stage)
        {
            string receipt = MinefieldStation + "/" + stage;
            if (!receipts.Add(receipt))
                return;

            var floor = new WorldActionData { receipt = receipt, kind = WorldActionKind.MinefieldControl, target = MinefieldFloor };
            actions.Add(floor);
            if (stage == MinefieldSession.LevelCount)
            {
                var exit = new WorldActionData { receipt = receipt, kind = WorldActionKind.UnlockDoor, target = MinefieldExit };
                var passage = new WorldActionData { receipt = receipt, kind = WorldActionKind.UnlockDoor, target = MinefieldNeuroPass };
                actions.Add(exit);
                actions.Add(passage);
                Home.WriteFile(MinefieldIncidentPath,
                    "SEC-07 / ЖУРНАЛ ДАТЧИКОВ\n\n" +
                    "17 ноября, 03:04. Камера фиксирует закрытие нейроархива. Оператор покинул пост до тревоги.\n" +
                    "17 ноября, 03:17. У поста снова отмечена его подпись. Датчики пола не зарегистрировали ни одного шага.\n" +
                    "Сменный рапорт датирован 18 ноября. Его копия осталась в кабинете за постом наблюдения.\n");
                messages.Add(new HomeMessageData
                {
                    id = receipt, path = MinefieldIncidentPath, text = "Восстановлен журнал датчиков охраны."
                });
                WorldAction?.Invoke(Clone(exit));
                WorldAction?.Invoke(Clone(passage));
            }
            WorldAction?.Invoke(Clone(floor));
            Changed?.Invoke();
        }
    }
}
