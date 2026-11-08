using System;
using System.Collections.Generic;
using System.Linq;

namespace Hostfall.Simulation
{
    public sealed class FacilitySimulation
    {
        private readonly Dictionary<DeviceId, VirtualComputer> computers = new();
        private bool autostartEnabled = true;
        private bool serviceStopped;
        private int nextPid = 432;
        public const string CacheService = "/opt/northline/cache-sync --resume";
        public event Action StateChanged;
        public void NotifyTerminalChanged()=>StateChanged?.Invoke();
        public event Action<CommandObservation> CommandObserved;
        public event Action<string> DocumentObserved;
        internal void ReadDocument(string id) => DocumentObserved?.Invoke(id);
        public int WatcherContacts { get; private set; }
        public void RecordWatcherContact() { WatcherContacts++; StateChanged?.Invoke(); }
        internal void ObserveCommand(string concept, bool success) => CommandObserved?.Invoke(new CommandObservation(concept, success));
        public MissionProgress Mission { get; }
        public TerminalDirector Terminals { get; }
        public ActionState Actions { get; }=new();
        public bool AutomatedRoute=>Mission.UsesTerminalSequences;
        public bool PulseUnlocked=>Terminals.IsComplete(DeviceId.Ws07);
        public VirtualComputer this[DeviceId id] => computers[id];
        public bool SourceIsolated => !autostartEnabled && !this[DeviceId.Ws07].HasProcess(CacheService);
        public bool ServerAccess => AutomatedRoute?Terminals.IsComplete(DeviceId.Edge01):SourceIsolated;
        public string Objective => Mission.Description;
        public FacilitySimulation(MissionDefinition definition = null)
        {
            Mission = new MissionProgress(definition ?? MissionDefinition.ActionRoute);
            Terminals=new TerminalDirector(this);
            Add(DeviceId.Ws07, "10.17.0.7", "WS-07 / INC-0317\nНаблюдатель перекрыл эвакуационный проход и повторяет маршрут после отмены задания. С рабочего места продолжает идти обмен с сетевым шлюзом.\nНужно прекратить передачу управляющих заданий и исключить её самопроизвольное возобновление. Журналы сохранить.\n");
            Add(DeviceId.Edge01, "10.17.0.1", "EDGE-01 / СЕТЕВОЙ ШЛЮЗ\nСоединяет операторскую и серверную. Потеря питания прерывает удалённые соединения. Локальная автоматика узлов при этом может продолжать работу.\n");
            Add(DeviceId.Core01, "10.17.0.99", "CORE-01 / Super\nСерверная продолжает работать при потере связи с операторской. Местный статус не описывает состояние всего комплекса.\nНужна независимая проверка оставшихся управляющих задач.\n");
            this[DeviceId.Ws07].Start(new VirtualProcess(71, "operator", "nlsh"));
            this[DeviceId.Ws07].Start(new VirtualProcess(431, "root", CacheService));
            this[DeviceId.Ws07].WriteFile("/etc/systemd/system/cache-sync.service", "[Unit]\nDescription=Northline watcher task relay\n[Service]\nExecStart=/opt/northline/cache-sync --resume\nRestart=always\n[Install]\nWantedBy=multi-user.target\n");
            this[DeviceId.Ws07].WriteFile("/var/log/scheduler.log", "03:16:59 systemd запустил cache-sync pid=431\n03:17:00 cache-sync -> edge01 / mesh / 4096 bytes\n");
            this[DeviceId.Edge01].Start(new VirtualProcess(90, "root", "packetd --policy local"));
            this[DeviceId.Edge01].WriteFile("/etc/network/interfaces", "eth0 10.17.0.1/24\ncontrol -> ws07, core01\n");
            this[DeviceId.Core01].Start(new VirtualProcess(666, "root", "super-node --leader --replicate"));
            this[DeviceId.Core01].WriteFile("/var/log/super.log", "03:17:00 выбран лидер: core01\n03:17:30 continuity: потеря WS-07 не нарушает управляющий контур\n");
        }
        public FacilityStateData Capture() => new()
        {
            autostartEnabled = autostartEnabled, serviceStopped = serviceStopped, nextPid = nextPid,
            completedObjectives = Mission.CompletedObjectives, watcherContacts = WatcherContacts,
            computers = computers.Values.OrderBy(c => (uint)c.Id).Select(c => c.Capture()).ToArray()
            ,routeVersion=AutomatedRoute?2:1,routines=Terminals.Capture(),actions=Actions.Capture()
        };
        public static FacilitySimulation Restore(FacilityStateData data, MissionDefinition definition = null)
        {
            SaveValidation.Require(data != null && data.version == 1 && data.computers?.Length == 3, "Unsupported or incomplete world state");
            var ids = new HashSet<DeviceId>();
            foreach (var computer in data.computers)
            {
                SaveValidation.Computer(computer); SaveValidation.Require(ids.Add(computer.id), "Duplicate saved computer");
            }
            SaveValidation.Require(data.nextPid >= 432 && data.nextPid < int.MaxValue && data.watcherContacts >= 0, "Invalid world counters");
            var workstation = data.computers.First(c => c.id == DeviceId.Ws07);
            SaveValidation.Require(workstation.processes.Where(p => p.command == CacheService).All(p => p.pid < data.nextPid), "Invalid next service PID");
            bool isolated = !data.autostartEnabled && workstation.processes.All(p => p.command != CacheService);
            SaveValidation.Require(data.completedObjectives < 2 || isolated, "Mission conflicts with source state");
            SaveValidation.Require(!data.serviceStopped || workstation.processes.All(p => p.command != CacheService), "Stopped service is still running");
            var model = new FacilitySimulation(definition??(data.routeVersion==2?MissionDefinition.ActionRoute:MissionDefinition.NightShift));
            foreach (var computer in data.computers) model[computer.id].Restore(computer);
            RefreshAuthoredTickets(model);
            model.autostartEnabled = data.autostartEnabled; model.serviceStopped = data.serviceStopped;
            model.nextPid = data.nextPid; model.WatcherContacts = data.watcherContacts;
            SaveValidation.Require(data.routeVersion>=0&&data.routeVersion<=2,"Unknown route version");
            if(data.routeVersion==2)
            {
                model.Terminals.Restore(data.routines);
                SaveValidation.Require(!model.Terminals.IsComplete(DeviceId.Ws07)||isolated,"Routine conflicts with source state");
                int completed=model.Terminals.Capture().Count(r=>r.completed);
                SaveValidation.Require(completed==data.completedObjectives,"Routine conflicts with mission progress");
                model.Mission.Restore(completed);
            }
            else if(model.AutomatedRoute)
            {
                model.Terminals.MigrateLegacy(data.completedObjectives,isolated);
                model.Mission.Restore(model.Terminals.Capture().Count(r=>r.completed));
            }
            else model.Mission.Restore(data.completedObjectives);
            model.Actions.Restore(data.actions);
            return model;
        }
        internal void CompleteRoutine(DeviceId id)
        {
            if(id!=DeviceId.Ws07) Actions.ApplyNetworkSlow();
            if(id==DeviceId.Core01) this[id].WriteFile("/home/operator/route-map.txt","Карта участка: операторская → служебный проход → серверная.\nДанные скопированы в личный архив инженера.\n");
            Mission.Record(ObjectiveCondition.TerminalSequence,id);
            StateChanged?.Invoke();
        }
        private static void RefreshAuthoredTickets(FacilitySimulation model)
        {
            if(model[DeviceId.Ws07].TryRead("/home/operator/ticket.txt",out var ticketWs07) && ticketWs07 == "WS-07 / INC-0317\nНачните с ps.\nИзучите: cat /var/log/scheduler.log\nОтключите автозапуск: systemctl disable cache-sync\nШтатно остановите службу: systemctl stop cache-sync\nkill завершает процесс, но Restart=always запускает новый.\nЗатем проверьте ps на CORE-01 в серверной.\n")
                model[DeviceId.Ws07].WriteFile("/home/operator/ticket.txt","WS-07 / INC-0317\nНаблюдатель перекрыл эвакуационный проход и повторяет маршрут после отмены задания. С рабочего места продолжает идти обмен с сетевым шлюзом.\nНужно прекратить передачу управляющих заданий и исключить её самопроизвольное возобновление. Журналы сохранить.\n");
            if(model[DeviceId.Edge01].TryRead("/home/operator/ticket.txt",out var ticketEdge01) && ticketEdge01 == "EDGE-01 / СЕТЕВОЙ ШЛЮЗ\nЭтот шлюз связывает WS-07 и CORE-01.\nПитание отключается физическим выключателем в коридоре.\nПри отключении шлюза удалённый ssh недоступен.\n")
                model[DeviceId.Edge01].WriteFile("/home/operator/ticket.txt","EDGE-01 / СЕТЕВОЙ ШЛЮЗ\nСоединяет операторскую и серверную. Потеря питания прерывает удалённые соединения. Локальная автоматика узлов при этом может продолжать работу.\n");
            if(model[DeviceId.Core01].TryRead("/home/operator/ticket.txt",out var ticketCore01) && ticketCore01 == "CORE-01 / Super\nСначала изолируйте источник на WS-07.\nЗатем выполните ps здесь для проверки.\nСохраните журнал: прекращение передачи с WS-07 не означает остановку остальных узлов.\n")
                model[DeviceId.Core01].WriteFile("/home/operator/ticket.txt","CORE-01 / Super\nСерверная продолжает работать при потере связи с операторской. Местный статус не описывает состояние всего комплекса.\nНужна независимая проверка оставшихся управляющих задач.\n");
        }
        internal void RecordTerminalOutput(DeviceId origin, string line, CommandResult result)
        {
            var computer = this[origin]; computer.LastCommand = line;
            computer.ScreenOutput = result.Clear ? "" : result.Text.Length > 4096 ? result.Text.Substring(0, 4096) : result.Text;
            StateChanged?.Invoke();
        }
        private void Add(DeviceId id, string address, string ticket)
        {
            var computer = new VirtualComputer(id, address); computers.Add(id, computer);
            computer.Start(new VirtualProcess(1, "root", "/sbin/init"));
            computer.WriteFile("/home/operator/ticket.txt", ticket);
            computer.AddDirectory("/tmp");
        }
        public bool CanConnect(DeviceId from, DeviceId to) => this[from].Powered && this[to].Powered && (from == to || this[DeviceId.Edge01].Powered);
        public void SetPower(DeviceId id, bool powered)
        {
            if (this[id].Powered == powered) return;
            this[id].Powered = powered; StateChanged?.Invoke();
        }
        internal void Inspect(DeviceId id)
        {
            Mission.Record(ObjectiveCondition.InspectProcesses, id);
            RefreshStage();
            if (Mission.IsComplete)
                this[DeviceId.Core01].WriteFile("/var/log/incident.log", "INC-0317: устранение инцидента подтверждено оператором.\n");
            StateChanged?.Invoke();
        }
        internal void DisableAutostart()
        {
            autostartEnabled = false;
            RefreshStage(); StateChanged?.Invoke();
        }
        internal void StopService()
        {
            serviceStopped = true;
            foreach (var process in new List<VirtualProcess>(this[DeviceId.Ws07].Processes))
                if (process.Command == CacheService) this[DeviceId.Ws07].Stop(process.Pid);
            RefreshStage(); StateChanged?.Invoke();
        }
        internal string ServiceStatus => "cache-sync.service\nАвтозапуск: " + (autostartEnabled ? "включён" : "отключён") + "\nСостояние: " + (this[DeviceId.Ws07].HasProcess(CacheService) ? "active (running)" : "inactive (dead)") + "\nRestart=always";
        internal string Stop(DeviceId id, int pid)
        {
            if (pid == 1) return "kill: системный процесс защищён";
            var computer = this[id];
            if (!computer.Stop(pid)) return "kill: процесс не найден";
            var message = "Процесс " + pid + " остановлен.";
            if (id == DeviceId.Ws07 && !serviceStopped && !computer.HasProcess(CacheService))
            {
                var replacement = nextPid++; computer.Start(new VirtualProcess(replacement, "root", CacheService));
                computer.TryRead("/var/log/scheduler.log", out var log);
                computer.WriteFile("/var/log/scheduler.log", log + "systemd перезапустил cache-sync pid=" + replacement + "\n");
                message += "\nsystemd перезапустил cache-sync pid=" + replacement + ".";
            }
            RefreshStage(); StateChanged?.Invoke(); return message;
        }
        private void RefreshStage()
        {
            if (SourceIsolated) Mission.Record(ObjectiveCondition.IsolateCacheSource, DeviceId.Ws07);
        }
    }
}
