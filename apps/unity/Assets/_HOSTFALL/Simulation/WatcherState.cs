namespace Hostfall.Simulation
{
    public enum WatcherMode { Patrol, Suspicious, Alert, Search, AttackWindup, Offline, Maintenance, Jammed }
    public sealed class WatcherState
    {
        public WatcherMode Mode { get; private set; }
        public void SetMode(WatcherMode mode) => Mode = mode;
        public void Observe(bool sourceIsolated, bool linkPowered, bool visible, bool jammed = false)
        {
            Mode = sourceIsolated ? WatcherMode.Maintenance : !linkPowered ? WatcherMode.Offline : jammed ? WatcherMode.Jammed : visible ? WatcherMode.Alert : WatcherMode.Patrol;
        }
    }
}
