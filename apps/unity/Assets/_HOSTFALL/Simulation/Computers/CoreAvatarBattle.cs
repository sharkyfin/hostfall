using System;

namespace Hostfall.Simulation.Computers
{
    public enum CoreAvatarAttack { Beam, TwinBeam, FieldPulse, Interceptor, Rush, Cascade }
    public enum CoreAvatarPart { Shell, Relay, Reactor }

    [Serializable] public sealed class CoreAvatarBattleData
    {
        public int version, cycle;
        public bool engaged, fired, revealSeen;
        public float health, clock, targetX, targetZ;
    }

    public sealed class CoreAvatarBattle
    {
        public const float Maximum = 540;
        public const float WarningAt = .35f;
        private CoreAvatarBattleData state = new() { version = 1, health = Maximum };

        public float Health => state.health;
        public bool Engaged => state.engaged;
        public bool Defeated => Health == 0;
        public bool RevealSeen => state.revealSeen;
        public int Phase => Health > 360 ? 1 : Health > 180 ? 2 : 3;
        public CoreAvatarAttack Attack => SelectAttack(Phase, state.cycle);
        public float Clock => state.clock;
        public float FireAt => FireTime(Attack);
        public float TargetX => state.targetX;
        public float TargetZ => state.targetZ;
        public bool GuardOpen => !Defeated && state.fired && state.clock < FireAt + Recovery(Attack);

        public bool Begin(float x, float z)
        {
            if (Engaged) return false;
            state.engaged = true;
            Aim(x, z);
            return true;
        }

        public void Step(float seconds, float x, float z, Action<CoreAvatarAttack> emit)
        {
            if (!Engaged || Defeated || seconds <= 0 || !float.IsFinite(seconds)) return;
            state.clock += Math.Min(.2f, seconds);
            if (state.clock < WarningAt) Aim(x, z);
            if (!state.fired && state.clock >= FireAt)
            {
                state.fired = true;
                emit(Attack);
            }
            if (state.clock >= FireAt + Recovery(Attack) + .7f)
            {
                state.cycle = (state.cycle + 1) % 4;
                state.clock = 0;
                state.fired = false;
                Aim(x, z);
            }
        }

        public bool Hit(float amount, CoreAvatarPart part, int auditedBanks)
        {
            if (!Engaged || Defeated || amount <= 0 || !float.IsFinite(amount)) return false;
            int previous = Phase;
            float coefficient = part switch
            {
                CoreAvatarPart.Reactor when GuardOpen => 1.65f,
                CoreAvatarPart.Reactor when Phase != 2 => .72f,
                CoreAvatarPart.Relay when Phase == 2 => 1.08f,
                CoreAvatarPart.Relay => .9f,
                _ => Phase == 2 && auditedBanks < 2 ? .12f : .28f
            };
            // A heavily charged shot can cross one transition. Leave each
            // defense phase an opportunity to become active.
            float threshold = previous == 1 ? 360 : previous == 2 ? 180 : 0;
            state.health = Math.Max(threshold, Health - amount * coefficient);
            if (Phase != previous || Defeated)
            {
                state.clock = 0;
                state.cycle = 0;
                state.fired = false;
            }
            return true;
        }

        public bool CompleteReveal()
        {
            if (!Defeated || state.revealSeen) return false;
            state.revealSeen = true;
            return true;
        }

        public bool Interrupt()
        {
            if (!Engaged || Defeated || state.fired) return false;
            state.clock = FireAt;
            state.fired = true;
            return true;
        }

        private void Aim(float x, float z)
        {
            state.targetX = Math.Clamp(x, -14, 34);
            state.targetZ = Math.Clamp(z, 129, 177);
        }

        private static CoreAvatarAttack SelectAttack(int phase, int cycle) => phase switch
        {
            1 => cycle % 2 == 0 ? CoreAvatarAttack.Beam : CoreAvatarAttack.TwinBeam,
            2 => cycle % 2 == 0 ? CoreAvatarAttack.FieldPulse : CoreAvatarAttack.Interceptor,
            _ => cycle % 2 == 0 ? CoreAvatarAttack.Rush : CoreAvatarAttack.Cascade
        };

        private static float FireTime(CoreAvatarAttack attack) =>
            attack is CoreAvatarAttack.FieldPulse or CoreAvatarAttack.Cascade ? 1.6f : 1.32f;

        private static float Recovery(CoreAvatarAttack attack) =>
            attack is CoreAvatarAttack.FieldPulse or CoreAvatarAttack.Rush ? 1.45f : 1.15f;

        public CoreAvatarBattleData Capture() => new()
        {
            version = 1, cycle = state.cycle, health = state.health, clock = state.clock,
            engaged = state.engaged, fired = state.fired, revealSeen = state.revealSeen,
            targetX = state.targetX, targetZ = state.targetZ
        };

        public static CoreAvatarBattle Restore(CoreAvatarBattleData data, bool recovered)
        {
            var restored = new CoreAvatarBattle();
            // Unity JSON materializes omitted nested reference objects as all-zero data.
            if (data == null || data.version == 0 && data.health == 0 && data.cycle == 0 && data.clock == 0 &&
                !data.engaged && !data.fired && !data.revealSeen && data.targetX == 0 && data.targetZ == 0) return restored;
            SaveValidation.Require(data.version == 1 && data.health is >= 0 and <= Maximum &&
                data.cycle is >= 0 and <= 3 && data.clock >= 0 && data.clock < 4 &&
                (data.engaged ? recovered && data.targetX is >= -14 and <= 34 && data.targetZ is >= 129 and <= 177 :
                    data.health == Maximum && data.clock == 0 && data.cycle == 0 && data.targetX == 0 && data.targetZ == 0) &&
                (!data.revealSeen || data.health == 0), "Invalid core battle");
            restored.state = data;
            SaveValidation.Require(data.fired == (data.clock >= restored.FireAt) &&
                data.clock < restored.FireAt + Recovery(restored.Attack) + .7f &&
                (!restored.Defeated || data.clock == 0 && data.cycle == 0), "Invalid core attack timing");
            restored.state = restored.Capture();
            return restored;
        }
    }
}
