#include "hostfall/graph.hpp"
#include <vector>

namespace hostfall {
bool Graph::add_device(DeviceId id) { return edges_.try_emplace(id).second; }
bool Graph::remove_device(DeviceId id) {
    if (!edges_.contains(id)) return false;
    for (const auto peer : edges_.at(id)) edges_.at(peer).erase(id);
    edges_.erase(id);
    return true;
}
bool Graph::connect(DeviceId from, DeviceId to) {
    if (from == to || !edges_.contains(from) || !edges_.contains(to)) return false;
    const bool added = edges_.at(from).insert(to).second;
    edges_.at(to).insert(from);
    return added;
}
bool Graph::disconnect(DeviceId from, DeviceId to) {
    if (!edges_.contains(from) || !edges_.contains(to)) return false;
    const bool removed = edges_.at(from).erase(to) > 0;
    edges_.at(to).erase(from);
    return removed;
}
bool Graph::reachable(DeviceId from, DeviceId to) const {
    if (!edges_.contains(from) || !edges_.contains(to)) return false;
    std::unordered_set<DeviceId> visited{from};
    std::vector<DeviceId> pending{from};
    while (!pending.empty()) {
        const auto node = pending.back(); pending.pop_back();
        if (node == to) return true;
        for (const auto peer : edges_.at(node))
            if (visited.insert(peer).second) pending.push_back(peer);
    }
    return false;
}
}
