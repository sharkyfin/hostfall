namespace Hostfall.Simulation
{
    // Stable IDs shared with the future native core; never use scene names as IDs.
    public enum DeviceId : uint { Ws07 = 7, Edge01 = 1, Core01 = 99 }
    public static class DeviceNames
    {
        public static string Hostname(this DeviceId id) => id switch
        {
            DeviceId.Ws07 => "ws07", DeviceId.Edge01 => "edge01", DeviceId.Core01 => "core01",
            _ => throw new System.ArgumentOutOfRangeException(nameof(id))
        };
        public static bool TryParse(string value, out DeviceId id)
        {
            foreach (var candidate in new[] { DeviceId.Ws07, DeviceId.Edge01, DeviceId.Core01 })
                if (candidate.Hostname() == value.ToLowerInvariant()) { id = candidate; return true; }
            id = default; return false;
        }
    }
}
