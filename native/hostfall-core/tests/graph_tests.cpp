#include "hostfall/graph.hpp"
#include <iostream>
#include <stdexcept>

static void require(bool value, const char* message) {
    if (!value) throw std::runtime_error(message);
}
int main() {
    try {
        hostfall::Graph graph;
        require(!graph.reachable(7, 7), "Missing vertex cannot reach itself");
        for (const auto id : {7u, 1u, 99u, 300u}) require(graph.add_device(id), "Add device");
        require(!graph.add_device(7), "Duplicate rejected");
        require(!graph.connect(7, 800), "Unknown endpoint rejected");
        require(!graph.connect(7, 7), "Self edge rejected");
        require(graph.connect(7, 1) && graph.connect(1, 99), "Physical chain");
        require(!graph.connect(1, 7), "Duplicate undirected edge rejected");
        require(graph.reachable(7, 99) && graph.reachable(99, 7), "Chain is bidirectional");
        require(!graph.reachable(7, 300), "Unconnected vertex stays isolated");
        require(graph.disconnect(1, 99) && !graph.reachable(7, 99), "Link removal propagates");
        require(graph.connect(99, 7) && graph.connect(1, 99), "Cycle");
        require(graph.reachable(99, 300) == false, "Cycle terminates");
        require(graph.remove_device(1), "Device removal");
        require(!graph.reachable(1, 7) && graph.reachable(7, 99), "Incident edges removed");
        require(graph.disconnect(7, 99) && !graph.reachable(7, 99), "Final connection removed");
        require(graph.reachable(7, 7), "Present vertex reaches itself");
        std::cout << "All graph invariants passed\n";
    } catch (const std::exception& error) { std::cerr << error.what() << '\n'; return 1; }
}
