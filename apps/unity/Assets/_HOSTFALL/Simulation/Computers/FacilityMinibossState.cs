using System;

namespace Hostfall.Simulation.Computers
{
    public enum FacilityMinibossKind { SpiderMatriarch, SecurityWarden, Harvester }
    public enum MinibossMove { Leap, Web, Skitter, Brood, ShieldRam, Sweep, GroundStrike, Grab, Reinforce, Blades, Magnet, Debris, Crush, Repair }

    [Serializable] public sealed class FacilityMinibossData
    {
        public FacilityMinibossKind kind;
        public int index;
        public bool fired, powerCut, support, interrupted, engaged;
        public float health, clock, targetX, targetZ;
    }

    public sealed class FacilityMinibossState
    {
        private static readonly MinibossMove[][] Plans =
        {
            new[] { MinibossMove.Leap, MinibossMove.Web, MinibossMove.Skitter, MinibossMove.Brood },
            new[] { MinibossMove.ShieldRam, MinibossMove.Sweep, MinibossMove.Grab, MinibossMove.GroundStrike, MinibossMove.Reinforce },
            new[] { MinibossMove.Blades, MinibossMove.Magnet, MinibossMove.Debris, MinibossMove.Crush, MinibossMove.Repair }
        };
        private FacilityMinibossData data;
        public FacilityMinibossKind Kind => data.kind;
        public float Maximum => Kind == FacilityMinibossKind.SpiderMatriarch ? 320 : Kind == FacilityMinibossKind.SecurityWarden ? 380 : 430;
        public float Health => data.health;
        public bool Engaged => data.engaged;
        public bool Defeated => Health <= 0;
        public bool PowerCut => data.powerCut;
        public bool Support => data.support;
        public bool Exposed => !Defeated && data.fired && Clock < ImpactAt + 1.1f;
        public MinibossMove Move => Plans[(int)Kind][data.index];
        public float Clock => data.clock;
        public float TargetX => data.targetX;
        public float TargetZ => data.targetZ;
        public float ImpactAt => Move is MinibossMove.ShieldRam or MinibossMove.Leap or MinibossMove.Crush ? .98f : 1.28f;

        public FacilityMinibossState(FacilityMinibossKind kind)
        {
            data = new FacilityMinibossData { kind = kind };
            data.health = Maximum;
        }

        public bool CutPower()
        {
            if (PowerCut || Kind != FacilityMinibossKind.SecurityWarden || Defeated) return false;
            data.powerCut = true;
            return true;
        }

        public void Step(float dt, float x, float z, Action<MinibossMove> resolve)
        {
            if (Defeated || dt <= 0 || !float.IsFinite(dt)) return;
            data.engaged = true;
            data.clock += Math.Min(dt, .2f);
            if (Clock < .3f || Clock == Math.Min(dt, .2f))
            {
                data.targetX = x;
                data.targetZ = z;
            }
            if (!data.fired && Clock >= ImpactAt)
            {
                data.fired = true;
                if (Move is MinibossMove.Brood or MinibossMove.Reinforce)
                {
                    if (!data.support) { data.support = true; resolve(Move); }
                }
                else if (Move == MinibossMove.Repair)
                {
                    if (!data.interrupted) data.health = Math.Min(Maximum, Health + 42);
                }
                else if (!data.interrupted) resolve(Move);
            }
            if (Clock < ImpactAt + 1.33f) return;
            data.clock = 0;
            data.index = (data.index + 1) % Plans[(int)Kind].Length;
            data.fired = data.interrupted = false;
        }

        public bool Hit(float damage, bool component, bool front)
        {
            if (Defeated || damage <= 0 || !float.IsFinite(damage)) return false;
            bool shield = Kind == FacilityMinibossKind.SecurityWarden && !PowerCut && !Exposed && front;
            float factor = component && Exposed ? 1.65f : shield ? .16f : component ? 1f : .62f;
            data.health = Math.Max(0, Health - damage * factor);
            if (Move == MinibossMove.Repair && damage >= 18 && component) data.interrupted = true;
            return true;
        }

        public void Interrupt()
        {
            if (Engaged && !data.fired) data.interrupted = true;
        }

        public FacilityMinibossData Capture() => new()
        {
            kind = Kind, index = data.index, health = Health, clock = Clock, targetX = TargetX, targetZ = TargetZ,
            fired = data.fired, powerCut = PowerCut, support = Support, interrupted = data.interrupted, engaged = Engaged
        };

        public static FacilityMinibossState Restore(FacilityMinibossData saved, FacilityMinibossKind kind)
        {
            var state = new FacilityMinibossState(kind);
            SaveValidation.Require(saved != null && saved.kind == kind && saved.index >= 0 && saved.index < Plans[(int)kind].Length &&
                saved.health >= 0 && saved.health <= state.Maximum && saved.clock >= 0 && saved.clock <= 2.62f &&
                saved.targetX is >= -35 and <= 95 && saved.targetZ is >= -25 and <= 210 &&
                (!saved.powerCut || kind == FacilityMinibossKind.SecurityWarden), "Invalid miniboss state");
            state.data = saved;
            SaveValidation.Require(saved.fired == (saved.clock >= state.ImpactAt) &&
                (saved.engaged || saved.clock == 0 && saved.index == 0 && !saved.support), "Invalid miniboss attack clock");
            state.data = state.Capture();
            return state;
        }
    }
}
