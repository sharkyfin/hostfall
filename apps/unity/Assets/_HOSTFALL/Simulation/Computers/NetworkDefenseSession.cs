using System;
using System.Collections.Generic;
using System.Linq;

namespace Hostfall.Simulation.Computers
{
    public enum NetworkPhase { Ready, Running, Failed, Complete }
    [Serializable] public sealed class NetworkNodeData
    {
        public int cpu, route;
        public bool isolated;
        public double infection;
    }
    [Serializable] public sealed class NetworkProcessData
    {
        public int id, node;
        public double health, travel;
        public bool armored;
    }
    [Serializable] public sealed class NetworkDefenseData
    {
        public int version = 1, stage, spawned, nextId = 1, blocked, leaked, selected = 3;
        public NetworkPhase phase;
        public double integrity = 100, credits = 6, elapsed, waveTime, spawnClock, purgeCooldown;
        public NetworkNodeData[] nodes;
        public NetworkProcessData[] processes = Array.Empty<NetworkProcessData>();
    }
    // Eight-node route/defence strategy. Isolation contains a process; CPU or a charged
    // purge must remove it. No wave advances without actual defensive action.
    public sealed class NetworkDefenseSession
    {
        public const int NodeCount = 8, CpuBudget = 4, StageCount = 3, CommandNode = 5;
        public const double StepSeconds = 1.0 / 30;
        private static readonly int[][] routes = { new[] { 1, 2 }, new[] { 3, 4 }, new[] { 3, 4 }, new[] { 5, 6 }, new[] { 5, 6 }, new[] { 7 }, new[] { 7 }, Array.Empty<int>() };
        private static readonly string[] names = { "UPLINK", "EDGE-A", "EDGE-B", "SANDBOX", "RELAY", "COMMAND", "VAULT", "CORE" };
        private NetworkNodeData[] nodes = Enumerable.Range(0, NodeCount).Select(_ => new NetworkNodeData()).ToArray();
        private readonly List<NetworkProcessData> processes = new(12);
        private double elapsed, waveTime, spawnClock, purgeCooldown;
        private int spawned, nextId = 1;
        public int Stage { get; private set; }
        public int Selected { get; private set; } = 3;
        public int Blocked { get; private set; }
        public int Leaked { get; private set; }
        public double Integrity { get; private set; } = 100;
        public double Credits { get; private set; } = 6;
        public double PurgeCooldown => purgeCooldown;
        public double WaveTime => waveTime;
        public int Spawned => spawned;
        public int WaveSize => 6 + Math.Min(2, Stage) * 2;
        public NetworkPhase Phase { get; private set; }
        public IReadOnlyList<NetworkProcessData> Processes => processes;
        public int UsedCpu => nodes.Sum(n => n.cpu + (n.isolated ? 1 : 0));
        public event Action<int> StageCompleted;
        public event Action Changed;
        public static string NodeName(int node) => names[node];
        public static int EntryFor(int stage,int packet) => stage == 0 ? 0 : stage == 1 ? (packet % 3 == 2 ? 4 : 0) : packet % 4 switch { 0 => 0, 1 => 2, 2 => 5, _ => 6 };
        public static int[] Destinations(int node) => (int[])routes[node].Clone();
        public static int RouteCount(int node) => routes[node].Length;
        public int Destination(int node) => routes[node].Length == 0 ? -1 : routes[node][nodes[node].route];
        public int Cpu(int node) => nodes[node].cpu;
        public bool IsIsolated(int node) => nodes[node].isolated;
        public double Infection(int node) => nodes[node].infection;
        public bool Select(int node)
        { if (!ValidNode(node)) return false; Selected = node; Changed?.Invoke(); return true; }
        public bool SetCpu(int node, int count)
        {
            if (!Editable(node) || count < 0 || count > CpuBudget || UsedCpu - nodes[node].cpu + count > CpuBudget) return false;
            nodes[node].cpu = count; Changed?.Invoke(); return true;
        }
        public bool SetIsolation(int node, bool isolated)
        {
            if (!Editable(node) || isolated && !nodes[node].isolated && UsedCpu >= CpuBudget) return false;
            nodes[node].isolated = isolated; Changed?.Invoke(); return true;
        }
        public bool ToggleRoute(int node)
        {
            if (!ValidNode(node) || routes[node].Length < 2 || Phase == NetworkPhase.Complete) return false;
            nodes[node].route = (nodes[node].route + 1) % routes[node].Length; Changed?.Invoke(); return true;
        }
        public bool Purge(int node)
        {
            if (!Editable(node) || Phase != NetworkPhase.Running || Credits < 2 || purgeCooldown > 0) return false;
            Credits -= 2; purgeCooldown = 3;
            nodes[node].infection = Math.Max(0, nodes[node].infection - 45);
            for (int i = processes.Count - 1; i >= 0; i--)
                if (processes[i].node == node)
                { processes[i].health -= 5; if (processes[i].health <= 0) { processes.RemoveAt(i); Blocked++; } }
            CompleteWaveIfReady(); Changed?.Invoke(); return true;
        }
        public void Start()
        {
            if (Phase == NetworkPhase.Complete || Phase == NetworkPhase.Running) return;
            if (Phase == NetworkPhase.Failed)
            {
                Integrity = 100; Credits = 6; elapsed = waveTime = spawnClock = purgeCooldown = 0;
                spawned = Blocked = Leaked = 0; processes.Clear(); foreach (var node in nodes) node.infection = 0;
            }
            Phase = NetworkPhase.Running; Changed?.Invoke();
        }
        public void Advance(double seconds)
        {
            if (Phase != NetworkPhase.Running || !Finite(seconds) || seconds <= 0) return;
            elapsed += Math.Min(seconds, .25);
            while (elapsed + 1e-12 >= StepSeconds && Phase == NetworkPhase.Running)
            { elapsed = Math.Max(0, elapsed - StepSeconds); Tick(); }
            Changed?.Invoke();
        }
        private void Tick()
        {
            waveTime = Math.Min(86400,waveTime+StepSeconds); spawnClock = spawned < WaveSize ? Math.Max(0,spawnClock-StepSeconds) : 0; purgeCooldown = Math.Max(0, purgeCooldown - StepSeconds);
            Credits = Math.Min(6, Credits + StepSeconds * .24);
            if (spawned < WaveSize && spawnClock <= 1e-12)
            {
                bool armored = spawned % 3 == 2;
                processes.Add(new NetworkProcessData { id = nextId++, node = EntryFor(Stage,spawned),
                    armored = armored, health = armored ? 5 + Stage : 2.4 + Stage * .55 });
                spawned++; spawnClock = 1.3 - Stage * .15;
            }
            foreach (var node in nodes) node.infection = Math.Max(0, node.infection - StepSeconds * .3);
            for (int i = processes.Count - 1; i >= 0; i--)
            {
                var process = processes[i]; var node = nodes[process.node];
                node.infection = Math.Min(100, node.infection + StepSeconds * (process.armored ? 7 : 4));
                if (node.infection < 80) process.health -= node.cpu * StepSeconds * 1.4;
                if (process.health <= 0) { processes.RemoveAt(i); Blocked++; continue; }
                if (node.isolated) continue;
                process.travel += StepSeconds;
                if (process.travel + 1e-12 < .7) continue;
                process.travel = Math.Max(0, process.travel - .7);
                if (process.node == NodeCount - 1)
                {
                    Leaked++; Integrity = Math.Max(0, Integrity - (process.armored ? 30 : 20)); processes.RemoveAt(i);
                    if (Integrity <= 0) { Phase = NetworkPhase.Failed; elapsed = 0; return; }
                }
                else process.node = Destination(process.node);
            }
            CompleteWaveIfReady();
        }
        private void CompleteWaveIfReady()
        {
            if (Phase != NetworkPhase.Running || spawned < WaveSize || processes.Count > 0) return;
            Stage++;
            if (Stage == StageCount) { Phase = NetworkPhase.Complete; elapsed = 0; }
            else
            {
                Phase = NetworkPhase.Ready; Integrity = Math.Max(Integrity, 70); Credits = 6;
                spawned = 0; elapsed = waveTime = spawnClock = purgeCooldown = 0; Blocked = Leaked = 0;
                foreach (var node in nodes) node.infection *= .25;
            }
            StageCompleted?.Invoke(Stage);
        }
        private bool Editable(int node) => node > 0 && node < NodeCount - 1 && Phase != NetworkPhase.Complete;
        private static bool ValidNode(int node) => node >= 0 && node < NodeCount;
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        public NetworkDefenseData Capture() => new()
        {
            stage = Stage, phase = Phase, selected = Selected, integrity = Integrity, credits = Credits, blocked = Blocked, leaked = Leaked,
            elapsed = elapsed, waveTime = waveTime, spawnClock = spawnClock, purgeCooldown = purgeCooldown, spawned = spawned, nextId = nextId,
            nodes = nodes.Select(n => new NetworkNodeData { cpu = n.cpu, isolated = n.isolated, route = n.route, infection = n.infection }).ToArray(),
            processes = processes.Select(p => new NetworkProcessData { id = p.id, node = p.node, health = p.health, travel = p.travel, armored = p.armored }).ToArray()
        };
        public static NetworkDefenseSession Restore(NetworkDefenseData data)
        {
            SaveValidation.Require(data != null && data.version == 1 && Enum.IsDefined(typeof(NetworkPhase), data.phase) &&
                data.stage >= 0 && data.stage <= StageCount && (data.phase == NetworkPhase.Complete) == (data.stage == StageCount) && ValidNode(data.selected), "Invalid network progress");
            SaveValidation.Require(data.nodes != null && data.nodes.Length == NodeCount && data.nodes.All(n => n != null) &&
                data.processes != null && data.processes.Length <= 10 && data.processes.All(p => p != null), "Invalid network topology");
            for (int i = 0; i < NodeCount; i++)
            {
                var node = data.nodes[i];
                SaveValidation.Require(node.cpu >= 0 && node.cpu <= CpuBudget && node.route >= 0 && node.route < Math.Max(1, routes[i].Length) &&
                    Finite(node.infection) && node.infection >= 0 && node.infection <= 100 && (i != 0 && i != 7 || node.cpu == 0 && !node.isolated), "Invalid network node");
            }
            SaveValidation.Require(data.nodes.Sum(n => n.cpu + (n.isolated ? 1 : 0)) <= CpuBudget, "Network CPU budget exceeded");
            SaveValidation.Require(Finite(data.integrity) && data.integrity >= 0 && data.integrity <= 100 && (data.phase == NetworkPhase.Failed) == (data.integrity == 0) &&
                Finite(data.credits) && data.credits >= 0 && data.credits <= 6, "Invalid network resources");
            SaveValidation.Require(Finite(data.elapsed) && data.elapsed >= 0 && data.elapsed < StepSeconds &&
                Finite(data.waveTime) && data.waveTime >= 0 && data.waveTime <= 86400 && Finite(data.spawnClock) && data.spawnClock >= -StepSeconds && data.spawnClock <= 1.3 &&
                Finite(data.purgeCooldown) && data.purgeCooldown >= 0 && data.purgeCooldown <= 3, "Invalid network clocks");
            int waveSize = 6 + Math.Min(2, data.stage) * 2;
            SaveValidation.Require(data.spawned >= 0 && data.spawned <= waveSize && data.blocked >= 0 && data.leaked >= 0 &&
                data.blocked + data.leaked + data.processes.Length == data.spawned && data.nextId > 0 && data.nextId <= 1000000, "Invalid process counters");
            var ids = new HashSet<int>();
            foreach (var process in data.processes)
                SaveValidation.Require(process.id > 0 && process.id < data.nextId && ids.Add(process.id) && ValidNode(process.node) &&
                    Finite(process.health) && process.health > 0 && process.health <= (process.armored ? 5 + Math.Min(2, data.stage) : 2.4 + Math.Min(2, data.stage) * .55) &&
                    Finite(process.travel) && process.travel >= 0 && process.travel < .7, "Invalid malicious process");
            SaveValidation.Require(data.phase != NetworkPhase.Ready || data.spawned == 0 && data.processes.Length == 0 && data.elapsed == 0 && data.waveTime == 0, "Invalid ready network wave");
            SaveValidation.Require(data.phase != NetworkPhase.Complete || data.processes.Length == 0 && data.spawned == waveSize && data.elapsed == 0, "Invalid completed network");
            return new NetworkDefenseSession { Stage = data.stage, Selected = data.selected, Phase = data.phase, Integrity = data.integrity, Credits = data.credits,
                nodes = data.nodes.Select(n => new NetworkNodeData { cpu = n.cpu, route = n.route, isolated = n.isolated, infection = n.infection }).ToArray(),
                elapsed = data.elapsed, waveTime = data.waveTime, spawnClock = data.spawnClock, purgeCooldown = data.purgeCooldown, spawned = data.spawned,
                Blocked = data.blocked, Leaked = data.leaked, nextId = data.nextId }.WithProcesses(data.processes);
        }
        private NetworkDefenseSession WithProcesses(NetworkProcessData[] saved)
        { processes.AddRange(saved.Select(p => new NetworkProcessData { id = p.id, node = p.node, health = p.health, travel = p.travel, armored = p.armored })); return this; }
    }
}
