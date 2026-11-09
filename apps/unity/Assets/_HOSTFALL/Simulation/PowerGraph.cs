using System;
using System.Collections.Generic;

namespace Hostfall.Simulation
{
    public sealed class PowerCircuit
    {
        public string Id { get; }
        public string Name { get; }
        public int LoadUnits { get; }
        public bool IsEnabled { get; set; }
        public bool IsEnergized { get; internal set; }

        public PowerCircuit(string id, string name, int loadUnits, bool initialEnabled = false)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? id;
            LoadUnits = Math.Max(0, loadUnits);
            IsEnabled = initialEnabled;
            IsEnergized = false;
        }
    }

    public sealed class PowerGraph
    {
        public int CapacityUnits { get; set; }
        public int TotalActiveLoad { get; private set; }
        public bool IsOverloaded { get; private set; }

        private readonly Dictionary<string, PowerCircuit> circuits = new(StringComparer.OrdinalIgnoreCase);

        public event Action StateChanged;

        public PowerGraph(int capacityUnits = 8)
        {
            CapacityUnits = Math.Max(1, capacityUnits);
        }

        public void AddCircuit(PowerCircuit circuit)
        {
            if (circuit == null) throw new ArgumentNullException(nameof(circuit));
            circuits[circuit.Id] = circuit;
            Evaluate();
        }

        public bool TryGetCircuit(string id, out PowerCircuit circuit) =>
            circuits.TryGetValue(id, out circuit);

        public IEnumerable<PowerCircuit> AllCircuits => circuits.Values;

        public bool ToggleCircuit(string id)
        {
            if (!circuits.TryGetValue(id, out var circuit)) return false;
            circuit.IsEnabled = !circuit.IsEnabled;
            Evaluate();
            StateChanged?.Invoke();
            return true;
        }

        public bool SetCircuit(string id, bool enabled)
        {
            if (!circuits.TryGetValue(id, out var circuit)) return false;
            if (circuit.IsEnabled == enabled) return false;
            circuit.IsEnabled = enabled;
            Evaluate();
            StateChanged?.Invoke();
            return true;
        }

        public void Evaluate()
        {
            int sum = 0;
            foreach (var kvp in circuits)
            {
                if (kvp.Value.IsEnabled)
                    sum += kvp.Value.LoadUnits;
            }

            TotalActiveLoad = sum;
            IsOverloaded = sum > CapacityUnits;

            foreach (var kvp in circuits)
            {
                kvp.Value.IsEnergized = kvp.Value.IsEnabled && !IsOverloaded;
            }
        }

        public static PowerGraph CreateSector03UpsPreset()
        {
            var graph = new PowerGraph(8);
            graph.AddCircuit(new PowerCircuit("vent", "Вентиляция шахты", 3, initialEnabled: true));
            graph.AddCircuit(new PowerCircuit("alarm", "Резервная сигнализация", 2, initialEnabled: true));
            graph.AddCircuit(new PowerCircuit("lift", "Грузовой лифт реактора", 5, initialEnabled: false));
            graph.AddCircuit(new PowerCircuit("door", "Замок криогенного шлюза", 4, initialEnabled: false));
            graph.Evaluate();
            return graph;
        }
    }
}

