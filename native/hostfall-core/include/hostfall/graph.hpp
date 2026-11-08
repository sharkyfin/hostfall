#pragma once
#include <cstdint>
#include <unordered_map>
#include <unordered_set>

namespace hostfall {
using DeviceId = std::uint32_t;
// Undirected physical links. IDs are stable game IDs, never Unity instance IDs.
class Graph {
public:
    bool add_device(DeviceId id);
    bool remove_device(DeviceId id);
    bool connect(DeviceId from, DeviceId to);
    bool disconnect(DeviceId from, DeviceId to);
    [[nodiscard]] bool reachable(DeviceId from, DeviceId to) const;
private:
    std::unordered_map<DeviceId, std::unordered_set<DeviceId>> edges_;
};
}
