namespace Hostfall.Simulation
{
    public enum WatcherMode { Patrol, Alert, Offline, Maintenance, Jammed }
    public sealed class WatcherState
    {
        public WatcherMode Mode { get; private set; }
        public void Observe(bool sourceIsolated, bool linkPowered, bool visible,bool jammed=false)
        {
            Mode = sourceIsolated ? WatcherMode.Maintenance : !linkPowered ? WatcherMode.Offline : jammed?WatcherMode.Jammed:visible ? WatcherMode.Alert : WatcherMode.Patrol;
        }
    }
}
