namespace Hostfall.Simulation.Computers
{
    public sealed partial class ComputerCampaign
    {
        public const string BufferStation = "pc.buffer";
        public const string BufferCache = "door.optional.cache.edge";
        public const string BufferLog = "/home/operator/archive/buffer_0317.log";
        public BufferRecallSession Buffer { get; private set; }
        public bool BufferAccessible => FirstTaskComplete;

        public bool ShowBuffer()
        {
            return BufferAccessible && Buffer.Show();
        }

        private void InitializeBuffer()
        {
            Buffer = new BufferRecallSession();
            BindBuffer();
        }

        private void BindBuffer()
        {
            Buffer.Completed += AwardBuffer;
            Buffer.Changed += () => Changed?.Invoke();
        }

        private void AwardBuffer()
        {
            const string receipt = BufferStation + "/cache";
            if (!receipts.Add(receipt)) return;
            var release = new WorldActionData { receipt = receipt, kind = WorldActionKind.UnlockDoor, target = BufferCache };
            actions.Add(release);
            Home.WriteFile(BufferLog, "EDGE-01 / БУФЕР ДАТЧИКОВ\n\n" +
                "Восстановлены три серии импульсов. В первые две смены через проход двигались люди. В последней серии сохранилась одна траектория.\n" +
                "Пост отметил её той же подписью, которой опечатали архив. На последнем кадре датчик веса не ответил.\n");
            messages.Add(new HomeMessageData { id = receipt, path = BufferLog, text = "Служебный пульт восстановил память прохода." });
            WorldAction?.Invoke(Clone(release));
            Changed?.Invoke();
        }
    }
}
