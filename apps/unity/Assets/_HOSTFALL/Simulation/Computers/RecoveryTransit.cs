using System;

namespace Hostfall.Simulation.Computers
{
    public enum TransitStop { Shelter, Ring }

    [Serializable]
    public sealed class RecoveryTransitData
    {
        public TransitStop destination;
        public bool moving, passenger;
        public double elapsed;
    }

    public readonly struct TransitPose
    {
        public readonly float X, Y, Z, Yaw;

        public TransitPose(float x, float y, float z, float yaw)
        {
            X = x;
            Y = y;
            Z = z;
            Yaw = yaw;
        }
    }

    public sealed class RecoveryTransit
    {
        public const float TunnelY = -10.7f;
        public const double GateDuration = .55, ShaftDuration = 1.7, TunnelDuration = 7.2;
        public const double Duration = 2 * (GateDuration + ShaftDuration) + TunnelDuration;
        public static readonly TransitPose Shelter = new(0, 0, -22.35f, 180);
        public static readonly TransitPose Ring = new(38, 0, 128.45f, 0);
        private RecoveryTransitData state = new();

        public TransitStop Destination => state.destination;
        public bool Moving => state.moving;
        public bool Passenger => state.passenger;
        public double Elapsed => state.elapsed;
        public event Action Changed;

        public bool Dispatch(TransitStop destination, bool passenger)
        {
            if (Moving || Destination == destination || !Enum.IsDefined(typeof(TransitStop), destination)) return false;
            state.destination = destination;
            state.moving = true;
            state.passenger = passenger;
            state.elapsed = 0;
            Changed?.Invoke();
            return true;
        }

        public void Advance(double seconds)
        {
            if (!Moving || double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds <= 0) return;
            state.elapsed += Math.Min(seconds, .12);
            if (state.elapsed < Duration) return;
            state.moving = false;
            state.passenger = false;
            state.elapsed = 0;
            Changed?.Invoke();
        }

        public TransitPose Pose => !Moving ? (Destination == TransitStop.Shelter ? Shelter : Ring) :
            Sample(Destination == TransitStop.Ring ? Elapsed : Duration - Elapsed);

        public double GateOpening(TransitStop station)
        {
            if (!Moving) return Destination == station ? 1 : 0;
            return Destination == station
                ? Smooth((Elapsed - (Duration - GateDuration)) / GateDuration)
                : 1 - Smooth(Elapsed / GateDuration);
        }

        private static TransitPose Sample(double fromShelter)
        {
            double time = fromShelter - GateDuration;
            if (time < ShaftDuration)
                return new TransitPose(Shelter.X, TunnelY * Smooth(time / ShaftDuration), Shelter.Z, Shelter.Yaw);
            time -= ShaftDuration;
            if (time < TunnelDuration)
            {
                float distance = Smooth(time / TunnelDuration);
                float rotation = Smooth((time - .8) / (TunnelDuration - 1.6));
                return new TransitPose(Lerp(Shelter.X, Ring.X, distance), TunnelY, Lerp(Shelter.Z, Ring.Z, distance),
                    Lerp(Shelter.Yaw, Ring.Yaw, rotation));
            }

            time -= TunnelDuration;
            return new TransitPose(Ring.X, Lerp(TunnelY, 0, Smooth(time / ShaftDuration)), Ring.Z, Ring.Yaw);
        }

        private static float Smooth(double value)
        {
            double progress = Math.Max(0, Math.Min(1, value));
            return (float)(progress * progress * (3 - 2 * progress));
        }

        private static float Lerp(float start, float end, float t) => start + (end - start) * t;

        public RecoveryTransitData Capture() => new()
        {
            destination = Destination, moving = Moving, passenger = Passenger, elapsed = Elapsed
        };

        public static RecoveryTransit Restore(RecoveryTransitData data, bool enabled)
        {
            if (data == null) return new RecoveryTransit();
            SaveValidation.Require(Enum.IsDefined(typeof(TransitStop), data.destination) &&
                data.elapsed >= 0 && data.elapsed < Duration && (!data.passenger || data.moving) &&
                (data.moving || data.elapsed == 0), "Invalid service shuttle state");
            SaveValidation.Require(enabled || !data.moving && data.destination == TransitStop.Shelter,
                "Service shuttle used before recovery");
            return new RecoveryTransit { state = new RecoveryTransitData
            {
                destination = data.destination, moving = data.moving, passenger = data.passenger, elapsed = data.elapsed
            } };
        }
    }
}
