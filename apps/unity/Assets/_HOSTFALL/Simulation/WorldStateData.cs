using System;
using System.Collections.Generic;
using System.Linq;

namespace Hostfall.Simulation
{
    [Serializable] public sealed class VirtualFileData { public string path, text; }
    [Serializable] public sealed class ProcessStateData { public int pid; public string owner, command; }
    [Serializable] public sealed class ComputerStateData
    {
        public DeviceId id;
        public TerminalMemoryData terminal;
        public bool powered;
        public string cwd, lastCommand, screenOutput;
        public VirtualFileData[] files;
        public string[] directories;
        public ProcessStateData[] processes;
    }
    [Serializable] public sealed class FacilityStateData
    {
        public int version = 1, completedObjectives, nextPid, watcherContacts;
        public bool autostartEnabled, serviceStopped;
        public ComputerStateData[] computers;
        public int routeVersion;
        public RoutineData[] routines;
        public ActionStateData actions;
    }
    [Serializable] public sealed class QuestHintData { public string id; public int level; }
    [Serializable] public sealed class FieldProgressData
    {
        public int version = 1;
        public ArchiveSnapshot knowledge;
        public string[] documents;
        public QuestHintData[] hints;
        public string selectedArticle = "terminal";
        public string programmingLanguage;
        public bool introductionSeen;
        public int introductionVersion;
    }
    public static class SaveValidation
    {
        public static void Require(bool valid, string reason) { if (!valid) throw new ArgumentException(reason); }
        public static void Text(string text, int limit, string reason) => Require(text != null && text.Length <= limit, reason);
        public static void Path(string path)
        {
            Text(path, 1024, "Invalid virtual path");
            Require(path.StartsWith("/", StringComparison.Ordinal) && VirtualComputer.Resolve("/", path) == path, "Noncanonical virtual path");
        }
        public static void Computer(ComputerStateData data)
        {
            Require(data != null && Enum.IsDefined(typeof(DeviceId), data.id), "Unknown saved computer");
            Require(data.files != null && data.directories != null && data.processes != null, "Incomplete saved computer");
            Require(data.files.Length <= 2048 && data.directories.Length <= 2048 && data.processes.Length <= 1024, "Saved computer exceeds limits");
            var files = new HashSet<string>(StringComparer.Ordinal); var dirs = new HashSet<string>(StringComparer.Ordinal);
            foreach (string path in data.directories) { Path(path); Require(dirs.Add(path), "Duplicate directory"); }
            Require(dirs.Contains("/"), "Missing virtual root");
            foreach (var file in data.files)
            {
                Require(file != null, "Missing virtual file"); Path(file.path); Text(file.text, 65536, "Virtual file exceeds limit");
                Require(files.Add(file.path) && !dirs.Contains(file.path), "Conflicting saved path");
                string parent = file.path.Substring(0, Math.Max(1, file.path.LastIndexOf('/')));
                Require(dirs.Contains(parent), "Missing parent directory");
            }
            foreach (string path in dirs.Where(p => p != "/"))
                Require(dirs.Contains(path.Substring(0, Math.Max(1, path.LastIndexOf('/')))), "Orphan directory");
            Path(data.cwd); Require(dirs.Contains(data.cwd), "Missing saved working directory");
            var pids = new HashSet<int>();
            foreach (var process in data.processes)
            {
                Require(process != null && process.pid > 0 && pids.Add(process.pid), "Duplicate/invalid PID");
                Text(process.owner, 128, "Invalid process owner"); Text(process.command, 4096, "Invalid process command");
            }
            Require(pids.Contains(1), "Saved machine is missing init");
            Text(data.lastCommand ?? "", 4096, "Saved command exceeds limit"); Text(data.screenOutput ?? "", 4096, "Saved output exceeds limit");
        }
    }
}
