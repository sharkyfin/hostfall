using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Hostfall.Simulation
{
    public readonly struct CommandResult
    {
        public string Text { get; }
        public int ExitCode { get; }
        public bool Clear { get; }
        public CommandResult(string text, int exitCode = 0, bool clear = false) { Text = text; ExitCode = exitCode; Clear = clear; }
    }
    public sealed class TerminalSession
    {
        private readonly FacilitySimulation simulation;
        private readonly DeviceId origin;
        public DeviceId Host { get=>simulation[origin].Terminal.Host; private set=>simulation[origin].Terminal.Host=value; }
        public VirtualComputer Computer => simulation[Host];
        public string Prompt => "operator@" + Host.Hostname() + ":" + Computer.WorkingDirectory + "$";
        public TerminalSession(FacilitySimulation simulation, DeviceId host)
        { this.simulation=simulation; origin=host; TerminalDisplay.Ensure(simulation[host]); Host=simulation[host].Terminal.Host; }
        public void Resume(DeviceId host) { if (!Enum.IsDefined(typeof(DeviceId),host)) throw new ArgumentException("Unknown host"); Host=host; simulation[origin].Terminal.Host=host; }
        public CommandResult Execute(string line)
        {
            var memory=simulation[origin].Terminal;
            memory.AddCommand(line.Length>4096?line.Substring(0,4096):line);
            memory.AddLine(Prompt+" "+line,"command-line"); memory.Draft="";
            string first = line.Trim().Split(' ')[0];
            string concept = first == "ps" || first == "kill" || first == "systemctl" ? "process" :
                first == "cat" || first == "ls" || first == "pwd" || first == "cd" ? "filesystem" :
                first == "ssh" || first == "hostname" ? "ip" : "terminal";
            var result = ExecuteCore(line);
            memory.Host=Host;
            if(result.Clear) memory.Clear();
            else if(result.Text.Length>0) memory.AddLine(result.Text,result.ExitCode==0?"result":"error");
            simulation.ObserveCommand(concept, result.ExitCode == 0);
            simulation.RecordTerminalOutput(origin, line, result);
            return result;
        }
        private CommandResult ExecuteCore(string line)
        {
            if (line.Length > 4096) return Error("nlsh: команда длиннее 4096 символов");
            if (line.Trim()=="exit") { Host=origin; return new CommandResult("Возврат: "+Host.Hostname()); }
            if (!simulation.CanConnect(origin, Host)) return Error("Связь потеряна. Восстановите питание EDGE-01 или введите exit.");
            if (!Computer.Powered) return Error("Устройство выключено. Закройте терминал и восстановите питание.");
            List<string> args;
            try { args = Parse(line); } catch (ArgumentException ex) { return Error(ex.Message); }
            if (args.Count == 0) return new CommandResult("");
            var command = args[0]; var values = args.Skip(1).ToArray();
            string path = VirtualComputer.Resolve(Computer.WorkingDirectory, values.Length == 0 ? "." : values[0]);
            switch (command)
            {
                case "help":
                    return values.Length == 0 ? new CommandResult("Команды\n\npwd — показать текущую папку\nls [путь] — показать содержимое папки\ncd <папка> — перейти в папку\ncat <файл> — прочитать файл\nps — показать работающие процессы\nkill <PID> — завершить процесс по его номеру\nsystemctl status <служба> — показать состояние службы\nsystemctl disable <служба> — отключить автозапуск\nsystemctl stop <служба> — остановить службу\nssh <узел> — открыть удалённый сеанс\nexit — закрыть удалённый сеанс\nhostname — показать имя текущего узла\nclear — очистить экран\n\nЗаменяйте <значение> нужным именем или номером без угловых скобок. [путь] — необязательный аргумент.") : Usage("help");
                case "pwd": return values.Length == 0 ? new CommandResult(Computer.WorkingDirectory) : Usage("pwd");
                case "hostname": return values.Length == 0 ? new CommandResult(Host.Hostname() + " / " + Computer.Address) : Usage("hostname");
                case "clear": return values.Length == 0 ? new CommandResult("", clear: true) : Usage("clear");
                case "ls":
                    if (values.Length > 1) return Usage("ls [path]");
                    if (Computer.TryRead(path, out _)) return new CommandResult(path.Substring(path.LastIndexOf('/') + 1));
                    return Computer.IsDirectory(path) ? new CommandResult(string.Join("\n", Computer.List(path))) : Error("ls: путь не найден: " + path);
                case "cd":
                    if (values.Length > 1) return Usage("cd [path]");
                    if (values.Length == 0) path = "/home/operator";
                    if (!Computer.IsDirectory(path)) return Error("cd: каталог не найден: " + path);
                    Computer.WorkingDirectory = path; return new CommandResult("");
                case "cat":
                    if (values.Length != 1) return Usage("cat <path>");
                    if (!Computer.TryRead(path, out var content)) return Error("cat: файл не найден: " + path);
                    if (path == "/home/operator/shift-note.txt" || path == "/home/operator/maintenance-note.txt" || path == "/home/operator/core-note.txt") simulation.ReadDocument(path.Substring(15).Replace(".txt", ""));
                    return new CommandResult(content);
                case "ps":
                    if (values.Length != 0) return Usage("ps");
                    var result = "PID    ВЛАДЕЛЕЦ    КОМАНДА\n" + string.Join("\n", Computer.Processes.Select(p => $"{p.Pid,-6} {p.Owner,-11} {p.Command}"));
                    simulation.Inspect(Host); return new CommandResult(result);
                case "ssh":
                    if (values.Length != 1) return Usage("ssh <hostname>");
                    if (!DeviceNames.TryParse(values[0], out var target)) return Error("ssh: неизвестное устройство");
                    if (!simulation.CanConnect(Host, target)) return Error("ssh: связь недоступна; проверьте питание EDGE-01");
                    Host = target; return new CommandResult("Подключено: " + Host.Hostname() + ".");
                case "kill":
                    if (values.Length != 1 || !int.TryParse(values[0], out var pid) || pid <= 0) return Usage("kill <PID>");
                    string response = simulation.Stop(Host, pid);
                    return new CommandResult(response, response.StartsWith("kill:", StringComparison.Ordinal) ? 1 : 0);
                case "systemctl":
                    if (values.Length != 2 || (values[0] != "disable" && values[0] != "stop" && values[0] != "status")) return Usage("systemctl <status|disable|stop> <служба>");
                    if (Host != DeviceId.Ws07 || values[1] != "cache-sync") return Error("systemctl: указанная служба не найдена на этом устройстве");
                    if (values[0] == "status") return new CommandResult(simulation.ServiceStatus);
                    if (values[0] == "stop") { simulation.StopService(); return new CommandResult("Служба остановлена."); }
                    simulation.DisableAutostart(); return new CommandResult("Автозапуск отключён.");
                default: return Error("nlsh: неизвестная команда: " + command);
            }
        }
        private static CommandResult Error(string text) => new(text, 1);
        private static CommandResult Usage(string usage) => Error("Использование: " + usage);
        private static List<string> Parse(string line)
        {
            var result = new List<string>(); var token = new StringBuilder(); char quote = '\0'; bool started = false;
            foreach (char c in line)
            {
                if (quote != '\0') { if (c == quote) quote = '\0'; else token.Append(c); started = true; }
                else if (c == '\'' || c == '"') { quote = c; started = true; }
                else if ("|><;&".Contains(c)) throw new ArgumentException("nlsh: операторы оболочки не поддерживаются");
                else if (char.IsWhiteSpace(c)) { if (started) { result.Add(token.ToString()); token.Clear(); started = false; } }
                else { token.Append(c); started = true; }
            }
            if (quote != '\0') throw new ArgumentException("nlsh: незакрытая кавычка");
            if (started) result.Add(token.ToString()); return result;
        }
    }
}
