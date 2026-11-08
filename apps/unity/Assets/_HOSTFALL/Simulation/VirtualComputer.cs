using System;
using System.Collections.Generic;
using System.Linq;

namespace Hostfall.Simulation
{
    public sealed class VirtualProcess
    {
        public int Pid { get; }
        public string Owner { get; }
        public string Command { get; }
        public VirtualProcess(int pid, string owner, string command) { Pid = pid; Owner = owner; Command = command; }
    }

    public sealed class VirtualComputer
    {
        private readonly SortedDictionary<string, string> files = new(StringComparer.Ordinal);
        private readonly HashSet<string> directories = new(StringComparer.Ordinal) { "/" };
        private readonly List<VirtualProcess> processes = new();
        public DeviceId Id { get; }
        public string Address { get; }
        public TerminalMemory Terminal { get; private set; }
        public bool Powered { get; internal set; } = true;
        public string WorkingDirectory { get; internal set; } = "/home/operator";
        public string LastCommand { get; internal set; } = "";
        public string ScreenOutput { get; internal set; } = "";
        public IReadOnlyList<VirtualProcess> Processes => processes.AsReadOnly();
        public VirtualComputer(DeviceId id, string address) { Id = id; Address = address; Terminal = new TerminalMemory(id); AddDirectory(WorkingDirectory); }
        public static string Resolve(string cwd, string path)
        {
            if (path == "~") path = "/home/operator";
            if (path.StartsWith("~/", StringComparison.Ordinal)) path = "/home/operator/" + path.Substring(2);
            var parts = new List<string>();
            foreach (var part in (path.StartsWith("/", StringComparison.Ordinal) ? path : cwd + "/" + path).Split('/'))
            {
                if (part == "" || part == ".") continue;
                if (part == "..") { if (parts.Count > 0) parts.RemoveAt(parts.Count - 1); }
                else parts.Add(part);
            }
            return "/" + string.Join("/", parts);
        }
        public void AddDirectory(string path)
        {
            path = Resolve("/", path); directories.Add(path);
            while (path.LastIndexOf('/') > 0) { path = path.Substring(0, path.LastIndexOf('/')); directories.Add(path); }
        }
        public void WriteFile(string path, string content)
        {
            path = Resolve("/", path);
            if (directories.Contains(path)) throw new ArgumentException("Path is a directory", nameof(path));
            AddDirectory(path.Substring(0, Math.Max(1, path.LastIndexOf('/')))); files[path] = content;
        }
        public bool IsDirectory(string path) => directories.Contains(path);
        public bool TryRead(string path, out string content) => files.TryGetValue(path, out content);
        public IEnumerable<string> List(string path)
        {
            string prefix = path == "/" ? "/" : path + "/";
            return directories.Where(p => p.StartsWith(prefix, StringComparison.Ordinal) && p != path && !p.Substring(prefix.Length).Contains('/'))
                .Select(p => p.Substring(prefix.Length) + "/")
                .Concat(files.Keys.Where(p => p.StartsWith(prefix, StringComparison.Ordinal) && !p.Substring(prefix.Length).Contains('/')).Select(p => p.Substring(prefix.Length)))
                .OrderBy(p => p, StringComparer.Ordinal);
        }
        public ComputerStateData Capture() => new()
        {
            id = Id, terminal = Terminal.Capture(), powered = Powered, cwd = WorkingDirectory, lastCommand = LastCommand, screenOutput = ScreenOutput,
            files = files.Select(p => new VirtualFileData { path = p.Key, text = p.Value }).ToArray(),
            directories = directories.OrderBy(p => p, StringComparer.Ordinal).ToArray(),
            processes = processes.Select(p => new ProcessStateData { pid = p.Pid, owner = p.Owner, command = p.Command }).ToArray()
        };
        internal void Restore(ComputerStateData data)
        {
            SaveValidation.Computer(data);
            Terminal = TerminalMemory.Restore(data.terminal, Id);
            files.Clear(); directories.Clear(); processes.Clear();
            foreach (string path in data.directories) directories.Add(path);
            foreach (var file in data.files) files.Add(file.path, file.text);
            foreach (var process in data.processes) processes.Add(new VirtualProcess(process.pid, process.owner, process.command));
            Powered = data.powered; WorkingDirectory = data.cwd;
            LastCommand = data.lastCommand ?? ""; ScreenOutput = data.screenOutput ?? "";
            if(TerminalMemory.IsLegacyEmpty(data.terminal) && LastCommand.Length>0)
            {
                Terminal.Initialized=true; Terminal.AddCommand(LastCommand); Terminal.AddLine("$ "+LastCommand,"command-line");
                if(ScreenOutput.Length>0) Terminal.AddLine(ScreenOutput,"result");
            }
        }
        internal void Start(VirtualProcess process) => processes.Add(process);
        internal bool Stop(int pid) => processes.RemoveAll(p => p.Pid == pid) > 0;
        public bool HasProcess(string command) => processes.Any(p => p.Command == command);
    }
}
