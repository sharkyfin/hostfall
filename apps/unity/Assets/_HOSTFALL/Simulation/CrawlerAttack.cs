using System;

namespace Hostfall.Simulation
{
    public enum CrawlerAttackKind { None, CannonBurst, HydraulicCharge, GroundSlam, Suppression }
    public enum CrawlerAttackPhase { Idle, Telegraph, Active, Recovery }

    [Serializable]
    public sealed class CrawlerAttackData
    {
        public int version;
        public CrawlerAttackKind kind;
        public CrawlerAttackPhase phase;
        public float remaining, heading, pitch;
        public int emitted;
        public bool overheated, contact;

        public CrawlerAttackData Clone() => (CrawlerAttackData)MemberwiseClone();
    }

    public sealed class CrawlerAttack
    {
        public const int SaveVersion = 1;
        private readonly CrawlerAttackData data;
        public CrawlerAttack(CrawlerAttackData data)
        {
            this.data = data;
            data.version = SaveVersion;
        }
        public CrawlerAttackKind Kind => data.kind;
        public CrawlerAttackPhase Phase => data.phase;
        public float Heading => data.heading;
        public float Pitch => data.pitch;
        public bool Overheated => data.overheated;

        public CrawlerAttackKind Select(float distance, float mobility, bool overheated)
        {
            if (distance < 4.35f && Kind != CrawlerAttackKind.GroundSlam)
                return CrawlerAttackKind.GroundSlam;
            if (distance >= 4.35f && distance <= 9.3f && mobility > .5f &&
                Kind is not (CrawlerAttackKind.HydraulicCharge or CrawlerAttackKind.Suppression))
                return CrawlerAttackKind.HydraulicCharge;
            if (Kind == CrawlerAttackKind.CannonBurst || Kind == CrawlerAttackKind.HydraulicCharge ||
                overheated && Kind != CrawlerAttackKind.Suppression)
                return CrawlerAttackKind.Suppression;
            return CrawlerAttackKind.CannonBurst;
        }

        public bool Commit(CrawlerAttackKind kind, float heading, float pitch, bool overheated)
        {
            if (Phase != CrawlerAttackPhase.Idle || (uint)kind - 1u > 3u)
                return false;
            data.kind = kind;
            data.phase = CrawlerAttackPhase.Telegraph;
            data.heading = heading;
            data.pitch = pitch;
            data.overheated = overheated;
            data.contact = false;
            data.emitted = 0;
            data.remaining = kind switch
            {
                CrawlerAttackKind.HydraulicCharge => 1.1f,
                CrawlerAttackKind.GroundSlam => .95f,
                CrawlerAttackKind.Suppression => .8f,
                _ => 1f
            };
            return true;
        }

        public void Step(float seconds, Action<int> pulse, Action<float> drive)
        {
            if (Phase == CrawlerAttackPhase.Idle || seconds <= 0) return;
            seconds = Math.Min(seconds, .1f);
            if (Phase == CrawlerAttackPhase.Active && Kind == CrawlerAttackKind.HydraulicCharge)
            {
                drive(Math.Min(seconds, data.remaining));
                if (Phase != CrawlerAttackPhase.Active) return;
            }
            data.remaining = Math.Max(0, data.remaining - seconds);
            if (data.remaining > 0) return;

            switch (Phase)
            {
                case CrawlerAttackPhase.Telegraph:
                    data.phase = CrawlerAttackPhase.Active;
                    if (Kind == CrawlerAttackKind.HydraulicCharge)
                    {
                        data.remaining = .86f;
                        return;
                    }
                    Emit(pulse);
                    break;
                case CrawlerAttackPhase.Active:
                    if (Kind == CrawlerAttackKind.HydraulicCharge)
                        Recover(1.35f);
                    else
                        Emit(pulse);
                    break;
                case CrawlerAttackPhase.Recovery:
                    data.phase = CrawlerAttackPhase.Idle;
                    break;
            }
        }

        private void Emit(Action<int> pulse)
        {
            pulse(data.emitted++);
            int limit = PulseLimit(Kind, Overheated);
            if (data.emitted >= limit)
                Recover(Overheated ? 1.85f : 1.45f);
            else
                data.remaining = Kind == CrawlerAttackKind.Suppression ? .31f : .66f;
        }

        public bool SpendContact()
        {
            if (data.contact || Phase != CrawlerAttackPhase.Active || Kind != CrawlerAttackKind.HydraulicCharge)
                return false;
            data.contact = true;
            return true;
        }

        public void StopCharge(bool hitWall)
        {
            if (Phase == CrawlerAttackPhase.Active && Kind == CrawlerAttackKind.HydraulicCharge)
                Recover(hitWall ? 2.6f : 1.35f);
        }

        public void Interrupt()
        {
            if (Phase is CrawlerAttackPhase.Telegraph or CrawlerAttackPhase.Active)
                Recover(.8f);
        }

        private void Recover(float seconds)
        {
            data.phase = CrawlerAttackPhase.Recovery;
            data.remaining = seconds;
        }

        private static int PulseLimit(CrawlerAttackKind kind, bool overheated) => kind switch
        {
            CrawlerAttackKind.CannonBurst => 3,
            CrawlerAttackKind.Suppression => overheated ? 5 : 4,
            CrawlerAttackKind.GroundSlam => 1,
            _ => 0
        };

        public static void Validate(CrawlerAttackData data)
        {
            if (data == null) return;
            if (data.version == 0)
            {
                SaveValidation.Require(data.kind == CrawlerAttackKind.None && data.phase == CrawlerAttackPhase.Idle &&
                    data.remaining == 0 && data.heading == 0 && data.pitch == 0 && data.emitted == 0 &&
                    !data.overheated && !data.contact, "Malformed empty guardian attack");
                return;
            }
            SaveValidation.Require(data.version == SaveVersion &&
                (uint)data.kind <= (uint)CrawlerAttackKind.Suppression &&
                (uint)data.phase <= (uint)CrawlerAttackPhase.Recovery &&
                data.remaining >= 0 && data.remaining <= 2.6f &&
                data.heading >= 0 && data.heading <= 360 &&
                data.pitch >= -35 && data.pitch <= 35 &&
                data.emitted >= 0 && data.emitted <= PulseLimit(data.kind, data.overheated) &&
                (data.phase == CrawlerAttackPhase.Idle ? data.remaining == 0 : data.kind != CrawlerAttackKind.None && data.remaining > 0) &&
                (!data.contact || data.kind == CrawlerAttackKind.HydraulicCharge) &&
                (data.phase != CrawlerAttackPhase.Telegraph || data.emitted == 0 && !data.contact) &&
                (data.phase != CrawlerAttackPhase.Active || data.kind == CrawlerAttackKind.HydraulicCharge ||
                    data.emitted > 0 && data.emitted < PulseLimit(data.kind, data.overheated)), "Invalid guardian attack");
        }
    }
}
