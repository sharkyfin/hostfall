using System;

namespace Hostfall.Simulation
{
    // A permit covers an attack telegraph and impact. Expiry also frees an actor
    // that was deactivated before it could explicitly relinquish the permit.
    public sealed class AttackPermitPool
    {
        private readonly Slot[] slots;
        private double clock;

        private struct Slot
        {
            public string owner;
            public double expires;
        }

        public AttackPermitPool(int capacity)
        {
            if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity));
            slots = new Slot[capacity];
        }

        public void Advance(double seconds)
        {
            if (seconds > 0 && !double.IsInfinity(seconds)) clock += seconds;
        }

        public bool Acquire(string owner, double duration)
        {
            if (string.IsNullOrEmpty(owner) || !(duration > 0 && duration <= 5)) return false;
            int free = -1;
            for (int i = 0; i < slots.Length; i++)
            {
                bool available = slots[i].owner == null || slots[i].expires <= clock;
                if (!available && slots[i].owner == owner) return true;
                if (free < 0 && available) free = i;
            }
            if (free < 0) return false;
            slots[free] = new Slot { owner = owner, expires = clock + duration };
            return true;
        }

        public void Release(string owner)
        {
            for (int i = 0; i < slots.Length; i++)
                if (slots[i].owner == owner)
                    slots[i] = default;
        }
    }
}
