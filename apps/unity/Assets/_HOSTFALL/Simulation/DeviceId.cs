namespace Hostfall.Simulation
{
    // Stable IDs shared with the future native core; never use scene names as IDs.
    public enum DeviceId : uint
    {
        Ws07 = 7,
        Edge01 = 1,
        Core01 = 99,
        Data01 = 101,
        Pwr01 = 201,
        Qnt01 = 301,
        Cargo01 = 401,
        Rbt01 = 501,
        Sec07 = 601,
        Neuro01 = 701,
        Cloud09 = 801,
        Super10 = 901,
        Home01 = 1007
    }
    public static class DeviceNames
    {
        public static string Hostname(this DeviceId id) => id switch
        {
            DeviceId.Home01 => "home", DeviceId.Ws07 => "ws07", DeviceId.Edge01 => "edge01", DeviceId.Core01 => "core01",
            DeviceId.Data01 => "data01", DeviceId.Pwr01 => "pwr01", DeviceId.Qnt01 => "qnt01",
            DeviceId.Cargo01 => "cargo01", DeviceId.Rbt01 => "rbt01", DeviceId.Sec07 => "sec07",
            DeviceId.Neuro01 => "neuro01", DeviceId.Cloud09 => "cloud09", DeviceId.Super10 => "super10",
            _ => throw new System.ArgumentOutOfRangeException(nameof(id))
        };
        public static bool TryParse(string value, out DeviceId id)
        {
            foreach (var candidate in new[]
            {
                DeviceId.Ws07, DeviceId.Edge01, DeviceId.Core01,
                DeviceId.Data01, DeviceId.Pwr01, DeviceId.Qnt01,
                DeviceId.Cargo01, DeviceId.Rbt01, DeviceId.Sec07,
                DeviceId.Neuro01, DeviceId.Cloud09, DeviceId.Super10, DeviceId.Home01
            })
                if (candidate.Hostname() == value.ToLowerInvariant()) { id = candidate; return true; }
            id = default; return false;
        }
    }
}
