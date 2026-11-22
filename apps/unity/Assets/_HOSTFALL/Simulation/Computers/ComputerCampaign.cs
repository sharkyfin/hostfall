using System;
using System.Collections.Generic;
using System.Linq;

namespace Hostfall.Simulation.Computers
{
    public enum WorldActionKind { SealVent, EnergizeDoor, UnlockDoor, StartMechanism, RaiseAlarm, NetworkControl, PowerDistribution, SignalControl, MinefieldControl, DetectiveControl, ContainmentControl, IntegrityControl, RecoveryControl }
    public enum VentThreatPhase { Dormant, Warning, Opening, Released, Sealed }

    [Serializable] public sealed class WorldActionData
    {
        public string receipt, target;
        public WorldActionKind kind;
    }
    [Serializable] public sealed class HomeMessageData { public string id, path, text; public bool read; }
    [Serializable] public sealed class DesktopWindowData
    {
        public string app;
        public bool minimized;
        public float x = 120, y = 70, width = 900, height = 620;
    }
    [Serializable] public sealed class ComputerDesktopData
    {
        public string station, focusedApp = "", document = "";
        public DesktopWindowData[] windows = Array.Empty<DesktopWindowData>();
    }
    [Serializable] public sealed class VentThreatData
    {
        public VentThreatPhase phase;
        public double elapsed;
        public bool grabResolved;
    }
    [Serializable] public sealed class ComputerCampaignData
    {
        public int version = 3;
        public ComputerStateData home;
        public HomeShellData shell;
        public SnakeData snake;
        public PayloadData payload;
        public LogisticsData logistics;
        public LogisticsDefenseData logisticsDefense;
        public DetectiveData investigation;
        public ContainmentData containment;
        public BufferRecallData buffer;
        public FragmentDockData fragments;
        public QuorumDispatchData quorum;
        public IntegrityAuditData integrity;
        public RecoveryProtocolData recovery;
        public RecoveryTransitData transit;
        public CoreAvatarBattleData coreAvatar;
        public FacilityMinibossData[] minibosses;
        public MinefieldData minefield;
        public SignalData signal;
        public SignalEquipmentData signalPlant;
        public CircuitData circuit;
        public NetworkDefenseData network;
        public NetworkSecurityData networkSecurity;
        public string homeTab = "terminal", guide = "start";
        public string[] receipts = Array.Empty<string>();
        public string[] readDocuments = Array.Empty<string>();
        public HomeMessageData[] messages = Array.Empty<HomeMessageData>();
        public ComputerDesktopData[] desktops = Array.Empty<ComputerDesktopData>();
        public WorldActionData[] actions = Array.Empty<WorldActionData>();
        public VentThreatData threat;
        public int terminalAmbushes;
        public FieldRouteData fieldRoute;
    }

    // Logical owner; no scene references. Rewards and desired mechanism states travel in one snapshot.
    public sealed partial class ComputerCampaign
    {
        public const string HomeStation = "pc.home", SnakeStation = "pc.ventflow";
        public const string EvacuationPath = "/home/operator/archive/evacuation.txt";
        public const string IncidentPath = "/home/operator/archive/incident_01.log";
        public const string VentTarget = "vent.soc.alpha";
        private const string RemovedHomeBanner = "HOME / локальный сеанс оператора\nАрхив доступен. Справочник — вкладка «Знания».";
        private readonly HashSet<string> receipts = new(StringComparer.Ordinal);
        private readonly HashSet<string> readDocuments = new(StringComparer.Ordinal);
        private readonly List<WorldActionData> actions = new();
        private readonly List<HomeMessageData> messages = new();
        private readonly Dictionary<string, ComputerDesktopData> desktops = new(StringComparer.Ordinal);
        public VirtualComputer Home { get; }
        public HomeShell Shell { get; }
        public SnakeSession Snake { get; private set; }
        public CoreAvatarBattle CoreAvatar { get; private set; } = new();
        public LogisticsDefense LogisticsDefense { get; private set; } = new();
        private FacilityMinibossState[] minibosses =
        {
            new(FacilityMinibossKind.SpiderMatriarch), new(FacilityMinibossKind.SecurityWarden), new(FacilityMinibossKind.Harvester)
        };
        public FacilityMinibossState Miniboss(FacilityMinibossKind kind) => minibosses[(int)kind];
        public string HomeTab { get; private set; } = "terminal";
        public string SelectedGuide { get; private set; } = "start";
        public VentThreatPhase ThreatPhase { get; private set; }
        public double ThreatElapsed { get; private set; }
        public bool VentGrabResolved { get; private set; }
        private int terminalAmbushes;
        public IReadOnlyList<WorldActionData> Actions => actions.Select(Clone).ToArray();
        public IReadOnlyList<HomeMessageData> Messages => messages.Select(Clone).ToArray();
        public bool HasUnreadArchive => messages.Any(message => !message.read);
        public event Action Changed;
        public event Action<WorldActionData> WorldAction;
        public event Action<VentThreatPhase> ThreatChanged;
        public bool VentSealed => actions.Any(a => a.kind == WorldActionKind.SealVent && a.target == VentTarget);
        public bool FirstTaskComplete => Snake.Phase == SnakePhase.Complete;

        public bool ResolveVentGrab()
        {
            if (VentGrabResolved || ThreatPhase != VentThreatPhase.Opening || ThreatElapsed < .82) return false;
            VentGrabResolved = true;
            return true;
        }

        public bool TrySpendTerminalAmbush(string station)
        {
            int bit = station == BufferStation ? 1 : station == NetworkStation ? 2 : 0;
            if (!FirstTaskComplete || bit == 0 || (terminalAmbushes & bit) != 0) return false;
            terminalAmbushes |= bit;
            return true;
        }
        public ComputerCampaign(uint snakeSeed = 0x4e4c0398)
        {
            Home = new VirtualComputer(DeviceId.Home01, "127.0.0.1");
            Home.Start(new VirtualProcess(1, "root", "/sbin/init"));
            Home.Terminal.Initialized = true;
            Home.AddDirectory("/home/operator/archive");
            Home.WriteFile("/home/operator/readme.txt", "Личный терминал инженера\n\nЗаписи лежат в ~/archive. В ~/status.txt сохраняется состояние комплекса. Команда help показывает доступные команды.\n");
            Home.WriteFile("/home/operator/.profile", "# Локальное хранилище. Удалённые подключения недоступны.\n");
            Home.WriteFile(EvacuationPath, "NORTHLINE / СЛУЖЕБНАЯ ЗАПИСКА 0317\n\nОбщая связь не отвечает. Пункты сбора пусты.\nИз убежища можно выйти в операторскую. Вентиляционный контроллер там остался без питания.\nПриложение VENTFLOW связывает ручные команды пульта с локальными приводами.\nНе оставайтесь у экрана, если услышите скрежет в воздуховоде.\n");
            Home.WriteFile("/home/operator/archive/recovery_journal.log", "[локальный журнал]\nМетка оператора: HUMAN OPERATOR\nПамять сеанса: восстановлена частично\nСвязь с персоналом: нет ответа\nКонтур вентиляции: изолирован\nПроверка восстановления ожидает доступ к операторской.\n");
            UpdateStatus();
            messages.Add(new HomeMessageData { id = "arrival", path = EvacuationPath, text = "Сохранена записка о пропавшем персонале." });
            Shell = new HomeShell(Home);
            Shell.DocumentRead += MarkDocumentRead;
            Shell.Changed += () => Changed?.Invoke();
            Snake = new SnakeSession(snakeSeed);
            BindSnake();
            InitializeApplications();
        }

        private void BindSnake()
        {
            Snake.StageCompleted += AwardSnakeStage;
            Snake.Changed += () => Changed?.Invoke();
        }
        public void SelectHomeTab(string tab)
        {
            SaveValidation.Require(tab is "terminal" or "knowledge" or "messages" || tab == RecoveryTab && RecoveryOffered, "Unknown home tab");
            HomeTab = tab; Changed?.Invoke();
        }
        public void SelectGuide(string id)
        {
            SaveValidation.Require(HomeGuide.Find(id) != null, "Unknown guide chapter");
            SelectedGuide = id; Changed?.Invoke();
        }
        private void MarkDocumentRead(string path)
        {
            if (!readDocuments.Add(path)) return;
            foreach (var message in messages.Where(m => m.path == path)) message.read = true;
            Changed?.Invoke();
        }
        public bool HasRead(string path) => readDocuments.Contains(path);

        public bool TryReadArchive(string path, out string record)
        {
            record = "";
            if (!messages.Any(message => message.path == path) || !Home.TryRead(path, out record)) return false;
            MarkDocumentRead(path);
            return true;
        }

        public bool GuideAvailable(string id) => id switch
        {
            "start" => true,
            "network" => FirstTaskComplete,
            "power" => CameraIsolated,
            "timestamp" => CargoReleased,
            "recovery" => RecoveryOffered,
            _ => false
        };

        public ComputerDesktopData Desktop(string station)
        {
            SaveValidation.Require(ComputerApplications.Find(station) != null, "Station not implemented in current iteration");
            if (!desktops.TryGetValue(station, out var value)) desktops.Add(station, value = new ComputerDesktopData { station = station });
            return Clone(value);
        }
        public void SaveDesktop(ComputerDesktopData value)
        {
            ValidateDesktop(value);
            desktops[value.station] = Clone(value);
            Changed?.Invoke();
        }

        public void BeginSnake()
        {
            Snake.Start();
            if (ThreatPhase == VentThreatPhase.Dormant && !VentSealed)
            {
                ThreatPhase = VentThreatPhase.Warning; ThreatElapsed = 0;
                ThreatChanged?.Invoke(ThreatPhase); Changed?.Invoke();
            }
        }
        // World clock, called even after leaving the application. Never tied to Snake.Advance.
        public void AdvanceWorld(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds <= 0) return;
            Logistics.Advance(seconds);
            NetworkSecurity.Advance(seconds);
            Circuit.AdvanceWorld(seconds);
            Minefield.AdvanceWorld(seconds);
            if (!fieldRoute.Has(FieldRouteStep.Voice)) Investigation.AdvanceStationPlayback(seconds);
            SignalPlant.Advance(seconds,!fieldRoute.Has(FieldRouteStep.Signal) && Signal.Phase==SignalPhase.Running,Signal.Coherence,Circuit.Powered(CircuitLoad.Ventilation));
            if (ThreatPhase != VentThreatPhase.Warning && ThreatPhase != VentThreatPhase.Opening) return;
            ThreatElapsed += Math.Min(seconds, .25);
            if (ThreatPhase == VentThreatPhase.Warning && ThreatElapsed >= 5)
            {
                ThreatElapsed -= 5; ThreatPhase = VentThreatPhase.Opening;
                ThreatChanged?.Invoke(ThreatPhase); Changed?.Invoke();
            }
            if (ThreatPhase == VentThreatPhase.Opening && ThreatElapsed >= 1.2)
            {
                ThreatElapsed = 0; ThreatPhase = VentThreatPhase.Released;
                ThreatChanged?.Invoke(ThreatPhase); Changed?.Invoke();
            }
        }

        private void AwardSnakeStage(int stage)
        {
            string receipt = SnakeStation + "/" + stage;
            if (!receipts.Add(receipt)) return;
            var emitted = new List<WorldActionData>();
            void Add(WorldActionKind kind, string target)
            {
                var action = new WorldActionData { receipt = receipt, kind = kind, target = target };
                actions.Add(action); emitted.Add(action);
            }
            if (stage == 1)
            {
                Add(WorldActionKind.SealVent, VentTarget);
                if (ThreatPhase != VentThreatPhase.Released) { ThreatPhase = VentThreatPhase.Sealed; ThreatElapsed = 0; }
            }
            if (stage == 2) Add(WorldActionKind.EnergizeDoor, "door.soc.corridor");
            if (stage == 3)
            {
                Add(WorldActionKind.UnlockDoor, "door.soc.corridor");
                Add(WorldActionKind.UnlockDoor, "door.corridor.server");
                Home.WriteFile(IncidentPath, "NORTHLINE / INCIDENT 0317 / фрагмент 01\n\nВентиляционный узел намеренно отсоединили от общей сети.\nСлужба изоляции записала: «Оставить локальное управление. Не возвращать SUPER доступ к приводам».\nПосле восстановления питание вернулось не только к двери. На шине появился незарегистрированный управляющий сигнал.\n\nАвтор записи отсутствует в списке эвакуированных.\n");
                Home.WriteFile("/home/operator/archive/recovery_journal.log", "[локальный журнал]\nМетка оператора: HUMAN OPERATOR\nПамять сеанса: восстановлена частично\nСвязь с персоналом: нет ответа\nКонтур вентиляции: восстановлен\nНайден фрагмент INCIDENT 0317. Общая шина отвечает.\n");
                messages.Add(new HomeMessageData { id = receipt, path = IncidentPath, text = "Восстановлен фрагмент журнала инцидента." });
            }
            UpdateStatus();
            foreach (var action in emitted) WorldAction?.Invoke(Clone(action));
            ThreatChanged?.Invoke(ThreatPhase);
            Changed?.Invoke();
        }
        private void UpdateStatus() => Home.WriteFile("/home/operator/status.txt", "HOME / контур восстановления\nВентиляция: " +
            (receipts.Contains(SnakeStation + "/1") ? "приводы закрыты" : "нет связи") + "\nДверь операторской: " +
            (receipts.Contains(SnakeStation + "/3") ? "разблокирована" : receipts.Contains(SnakeStation + "/2") ? "питание подано" : "нет питания") + "\nСетевой карантин: " +
            (receipts.Contains(NetworkStation+"/3") ? "командный канал изолирован" : "командный канал активен") + "\n");

        public ComputerCampaignData Capture() => new()
        {
            home = Home.Capture(), shell = Shell.Capture(), snake = Snake.Capture(),
            payload = Payload.Capture(), logistics = Logistics.Capture(), logisticsDefense = LogisticsDefense.Capture(), network = Network.Capture(),
            networkSecurity = NetworkSecurity.Capture(), circuit = Circuit.Capture(),
            signal = Signal.Capture(), signalPlant = SignalPlant.Capture(), minefield = Minefield.Capture(),
            investigation = Investigation.Capture(), containment = Containment.Capture(),
            buffer = Buffer.Capture(), fragments = Fragments.Capture(), quorum = Quorum.Capture(),
            integrity = Integrity.Capture(), recovery = Recovery.Capture(), transit = Transit.Capture(), coreAvatar = CoreAvatar.Capture(),
            minibosses = minibosses.Select(boss => boss.Capture()).ToArray(),
            homeTab = HomeTab, guide = SelectedGuide, receipts = receipts.OrderBy(x => x, StringComparer.Ordinal).ToArray(),
            readDocuments = readDocuments.OrderBy(x => x, StringComparer.Ordinal).ToArray(),
            actions = actions.Select(Clone).ToArray(), messages = messages.Select(Clone).ToArray(),
            desktops = desktops.Values.OrderBy(x => x.station, StringComparer.Ordinal).Select(Clone).ToArray(),
            threat = new VentThreatData { phase = ThreatPhase, elapsed = ThreatElapsed, grabResolved = VentGrabResolved },
            terminalAmbushes = terminalAmbushes, fieldRoute = fieldRoute.Copy()
        };
        public static ComputerCampaign Restore(ComputerCampaignData data)
        {
            SaveValidation.Require(data != null && data.version == 3 && data.home != null && data.home.id == DeviceId.Home01 &&
                data.home.terminal != null && data.home.terminal.host == DeviceId.Home01 && data.shell != null, "Invalid home campaign");
            var route = FieldRouteData.Restore(data.fieldRoute);
            var snake = SnakeSession.Restore(data.snake);
            var payload = data.payload == null ? new PayloadSession() : PayloadSession.Restore(data.payload);
            var investigation = data.investigation == null ? new DetectiveSession() : DetectiveSession.Restore(data.investigation);
            var buffer = data.buffer == null ? new BufferRecallSession() : BufferRecallSession.Restore(data.buffer);
            SaveValidation.Require((buffer.Stage == 0 && buffer.Phase == BufferPhase.Ready) || snake.Stage == 3,
                "Passage buffer used before VENTFLOW access");
            var containment = data.containment == null ? new ContainmentSession() : ContainmentSession.Restore(data.containment);
            SaveValidation.Require(!containment.Started || (investigation.KeyRecovered || route.Has(FieldRouteStep.Key)), "Containment used before archive key");
            var minefield = data.minefield == null ? new MinefieldSession() : MinefieldSession.Restore(data.minefield);
            var signal=data.signal==null?new SignalSession():SignalSession.Restore(data.signal);
            var circuit = data.circuit == null ? new CircuitSession() : CircuitSession.Restore(data.circuit);
            var integrity = data.integrity == null ? new IntegrityAuditSession() : IntegrityAuditSession.Restore(data.integrity);
            SaveValidation.Require((integrity.Stage == 0 && integrity.Phase == IntegrityPhase.Ready) || containment.Complete,
                "Audit used before quarantine access");
            var quorum = data.quorum == null ? new QuorumDispatchSession() : QuorumDispatchSession.Restore(data.quorum);
            SaveValidation.Require(!quorum.Started || containment.Complete, "Witness dispatch used outside Central Ring access");
            var fragments = data.fragments == null ? new FragmentDockSession() : FragmentDockSession.Restore(data.fragments);
            var network = data.network == null ? new NetworkDefenseSession() : NetworkDefenseSession.Restore(data.network);
            int networkStage = route.Has(FieldRouteStep.Camera) ? 3 : network.Stage;
            int signalStage = route.Has(FieldRouteStep.Signal) ? 3 : signal.Stage;
            int payloadStage = route.Has(FieldRouteStep.Cargo) ? 3 : payload.Stage;
            int floorStage = route.Has(FieldRouteStep.Floor) ? MinefieldSession.LevelCount : minefield.Stage;
            bool verified = investigation.Verified || route.Has(FieldRouteStep.Voice);
            bool keyRecovered = investigation.KeyRecovered || route.Has(FieldRouteStep.Key);
            SaveValidation.Require(!fragments.Started || networkStage == 3, "Fragment Dock used before vault access");
            SaveValidation.Require(!investigation.HasProgress || floorStage == MinefieldSession.LevelCount, "Investigation used before neuroarchive access");
            SaveValidation.Require(payloadStage == 3 || minefield.Stage == 0 && minefield.Phase == MinefieldPhase.Ready &&
                minefield.AlarmRemaining == 0, "Minefield used before security access");
            SaveValidation.Require(!(signal.Started||data.signalPlant?.started==true)||circuit.Commissioned,"Signal used before power access");
            SaveValidation.Require(!circuit.Started || networkStage == 3, "Circuit used before energy access");
            SaveValidation.Require((network.Stage == 0 && network.Phase == NetworkPhase.Ready || snake.Stage == 3), "Network started before ventilation access");
            var recovery = data.recovery == null ? new RecoveryProtocolSession() : RecoveryProtocolSession.Restore(data.recovery);
            bool armoryAccessible = fragments.Complete || route.Has(FieldRouteStep.Armory);
            bool recoveryComplete = recovery.Complete || route.Has(FieldRouteStep.Home);
            SaveValidation.Require((route.cameraRead == 0 || snake.Stage == 3) &&
                (route.voiceRead == 0 || floorStage == MinefieldSession.LevelCount) &&
                (!route.Has(FieldRouteStep.Signal) || circuit.Commissioned) &&
                (!route.Has(FieldRouteStep.Cargo) || signalStage == 3) &&
                (!route.Has(FieldRouteStep.Floor) || payloadStage == 3) &&
                (!route.Has(FieldRouteStep.Key) || route.Has(FieldRouteStep.Voice)) &&
                (!route.Has(FieldRouteStep.Armory) || networkStage == 3), "Field procedure out of order");
            int expectedReceiptCount = snake.Stage + payloadStage + networkStage + signalStage + floorStage +
                (circuit.Commissioned ? 1 : 0) + (verified ? 1 : 0) + (keyRecovered ? 1 : 0) +
                (containment.Complete ? 1 : 0) + (buffer.Complete ? 1 : 0) + (armoryAccessible ? 1 : 0) +
                (quorum.Complete ? 1 : 0) + (integrity.Complete ? 1 : 0) + (recoveryComplete ? 1 : 0);
            SaveValidation.Require((payloadStage == 0 && payload.Phase == PayloadPhase.Ready || snake.Stage == 3) &&
                data.receipts?.Length == expectedReceiptCount && data.actions != null, "Incomplete campaign rewards");
            var expected = new ComputerCampaign { fieldRoute = route };
            for (int stage = 1; stage <= snake.Stage; stage++) expected.AwardSnakeStage(stage);
            for (int stage = 1; stage <= payloadStage; stage++) expected.AwardPayloadStage(stage);
            for (int stage = 1; stage <= networkStage; stage++) expected.AwardNetworkStage(stage);
            if(circuit.Commissioned)expected.AwardCircuit();
            for(int stage=1;stage<=signalStage;stage++)expected.AwardSignalStage(stage);
            for (int stage = 1; stage <= floorStage; stage++) expected.AwardMinefieldStage(stage);
            if (verified) expected.AwardVerifiedCase();
            if (keyRecovered) expected.AwardArchiveKey();
            if (containment.Complete) expected.AwardContainment();
            if (buffer.Complete) expected.AwardBuffer();
            if (armoryAccessible) expected.AwardFragmentDock();
            if (quorum.Complete) expected.AwardQuorum();
            if (integrity.Complete) expected.AwardIntegrity();
            if (recoveryComplete) expected.AwardRecovery();
            SaveValidation.Require(new HashSet<string>(data.receipts, StringComparer.Ordinal).SetEquals(expected.receipts), "Invalid campaign receipts");
            var required = expected.actions.Select(ActionKey).OrderBy(x => x, StringComparer.Ordinal);
            SaveValidation.Require(data.actions.All(a => a != null) && required.SequenceEqual(data.actions.Select(ActionKey).OrderBy(x => x, StringComparer.Ordinal)), "Invalid desired world actions");
            SaveValidation.Require(data.messages != null && data.messages.Length == expected.messages.Count, "Invalid campaign messages");
            var expectedMessages = expected.messages.ToDictionary(m => m.id, StringComparer.Ordinal);
            var messageIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var message in data.messages)
                SaveValidation.Require(message != null && messageIds.Add(message.id) && expectedMessages.TryGetValue(message.id, out var template) &&
                    message.path == template.path && message.text == template.text, "Invalid home message");
            expected.Home.Restore(data.home);
            expected.Home.Terminal.RemoveInitialMutedLine(RemovedHomeBanner);
            expected.PublishRecoveryDossier();
            expected.Shell.Restore(data.shell);
            SaveValidation.Require(data.readDocuments != null && data.readDocuments.Length <= 2048 &&
                data.readDocuments.Distinct(StringComparer.Ordinal).Count() == data.readDocuments.Length, "Invalid read documents");
            foreach (string path in data.readDocuments)
            {
                SaveValidation.Path(path); SaveValidation.Require(expected.Home.TryRead(path, out _), "Missing read document");
                expected.readDocuments.Add(path);
            }
            foreach (var message in data.messages)
                SaveValidation.Require(message.read == expected.readDocuments.Contains(message.path), "Inconsistent read message");
            SaveValidation.Require(!recovery.Started && !recoveryComplete || expected.RecoveryAccessible, "Recovery requires all records and systems");
            if (recoveryComplete) SaveValidation.Require(expected.Home.TryRead(RecoveryLog, out _), "Missing recovery status");
            if (integrity.Complete) SaveValidation.Require(expected.Home.TryRead(IntegrityLog, out _), "Missing core telemetry log");
            if (quorum.Complete) SaveValidation.Require(expected.Home.TryRead(QuorumLog, out _), "Missing witness dispatch report");
            if (armoryAccessible) SaveValidation.Require(expected.Home.TryRead(FragmentLog, out _), "Missing volume manifest");
            if (buffer.Complete) SaveValidation.Require(expected.Home.TryRead(BufferLog, out _), "Missing passage buffer log");
            if (containment.Complete) SaveValidation.Require(expected.Home.TryRead(ContainmentLog, out _), "Missing quarantine clearance log");
            if (verified) SaveValidation.Require(expected.Home.TryRead(DetectiveCasePath, out _), "Missing confirmed case file");
            if (floorStage == MinefieldSession.LevelCount) SaveValidation.Require(expected.Home.TryRead(MinefieldIncidentPath, out _), "Missing earned security log");
            if(signalStage==3)SaveValidation.Require(expected.Home.TryRead(SignalIncidentPath,out _),"Missing earned phase log");
            if(circuit.Commissioned)SaveValidation.Require(expected.Home.TryRead(CircuitIncidentPath,out _),"Missing earned power log");
            if (networkStage == 3) SaveValidation.Require(expected.Home.TryRead(NetworkIncidentPath, out _), "Missing earned network log");
            if (payloadStage == 3) SaveValidation.Require(expected.Home.TryRead("/home/operator/archive/payload_transfer.log", out _), "Missing earned payload file");
            if (snake.Stage == 3) SaveValidation.Require(expected.Home.TryRead(IncidentPath, out _), "Missing earned incident file");
            expected.messages.Clear(); expected.messages.AddRange(data.messages.Select(Clone));
            expected.Snake = snake; expected.BindSnake();
            expected.Payload = payload; expected.BindPayload();
            expected.Logistics = LogisticsSystem.Restore(data.logistics, payloadStage);
            expected.LogisticsDefense = LogisticsDefense.Restore(data.logisticsDefense, payloadStage == 3);
            expected.Network = network; expected.BindNetwork();
            expected.Circuit=circuit;expected.BindCircuit();
            expected.Minefield = minefield; expected.BindMinefield();
            expected.Investigation = investigation; expected.BindInvestigation();
            expected.Containment = containment; expected.BindContainment();
            expected.Buffer = buffer; expected.BindBuffer();
            expected.Fragments = fragments; expected.BindFragmentDock();
            expected.Quorum = quorum; expected.BindQuorum();
            expected.Integrity = integrity; expected.BindIntegrity();
            expected.Recovery = recovery;
            expected.Transit = RecoveryTransit.Restore(data.transit, recoveryComplete);
            expected.CoreAvatar = CoreAvatarBattle.Restore(data.coreAvatar, recoveryComplete);
            if (data.minibosses?.Length > 0)
            {
                SaveValidation.Require(data.minibosses.Length == expected.minibosses.Length, "Invalid miniboss roster");
                for (int i = 0; i < expected.minibosses.Length; i++)
                    expected.minibosses[i] = FacilityMinibossState.Restore(data.minibosses[i], (FacilityMinibossKind)i);
            }
            expected.BindRecovery();
            expected.Signal=signal;expected.SignalPlant=SignalEquipment.Restore(data.signalPlant,signalStage);expected.BindSignal();
            SaveValidation.Require(signal.Phase==SignalPhase.Ready&&signal.Stage==0||expected.SignalPlant.Started,"Missing signal equipment start");
            expected.NetworkSecurity = NetworkSecuritySystem.Restore(data.networkSecurity, networkStage);
            SaveValidation.Require(network.Phase == NetworkPhase.Ready && networkStage == 0 || expected.NetworkSecurity.Started, "Missing network encounter state");
            expected.SelectHomeTab(data.homeTab);
            expected.SelectGuide(data.guide is "paths" or "list" or "read" or "search" or "find" or "input" or "messages" or "errors"
                ? "start" : data.guide);
            SaveValidation.Require(data.desktops != null && data.desktops.Length <= ComputerApplications.Implemented.Count && data.desktops.Select(d => d?.station).Distinct().Count() == data.desktops.Length && data.desktops.All(d => d != null), "Invalid desktops");
            foreach (var desktop in data.desktops) expected.SaveDesktop(desktop);
            var threat = data.threat;
            SaveValidation.Require(threat != null && Enum.IsDefined(typeof(VentThreatPhase), threat.phase) && !double.IsNaN(threat.elapsed) &&
                !double.IsInfinity(threat.elapsed) && threat.elapsed >= 0 && threat.elapsed < (threat.phase == VentThreatPhase.Warning ? 5 : 1.2), "Invalid vent threat");
            SaveValidation.Require(threat.phase == VentThreatPhase.Warning || threat.phase == VentThreatPhase.Opening || threat.elapsed == 0, "Invalid settled threat clock");
            SaveValidation.Require(snake.Stage == 0 ? threat.phase != VentThreatPhase.Sealed :
                threat.phase == VentThreatPhase.Sealed || threat.phase == VentThreatPhase.Released, "Inconsistent sealed vent");
            SaveValidation.Require(!threat.grabResolved || threat.phase is VentThreatPhase.Released or VentThreatPhase.Sealed ||
                threat.phase == VentThreatPhase.Opening && threat.elapsed >= .819999999, "Invalid vent grab timing");
            expected.ThreatPhase = threat.phase; expected.ThreatElapsed = threat.elapsed;
            expected.VentGrabResolved = threat.grabResolved;
            SaveValidation.Require(data.terminalAmbushes is >= 0 and <= 3 &&
                (data.terminalAmbushes == 0 || snake.Stage == SnakeSession.StageCount), "Invalid terminal ambush state");
            expected.terminalAmbushes = data.terminalAmbushes;
            return expected;
        }

        private static void ValidateDesktop(ComputerDesktopData data)
        {
            SaveValidation.Require(data != null && ComputerApplications.Find(data.station) != null && data.windows != null && data.windows.Length <= (data.station == DetectiveStation ? DetectiveDesktopApps.Tools.Count + 1 : 3), "Invalid station desktop");
            SaveValidation.Text(data.document ?? "", 128, "Invalid desktop document");
            SaveValidation.Require(data.document is "" or "readme" or "maintenance" ||
                data.station == SnakeStation && VentflowArchive.Find(data.document) != null, "Unknown desktop document");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var window in data.windows)
            {
                string primary = ComputerApplications.Find(data.station).App;
                bool tool = data.station == DetectiveStation
                    ? DetectiveDesktopApps.IsTool(window?.app)
                    : window?.app is "files" or "document";
                SaveValidation.Require(window != null && (window.app == primary || tool) && ids.Add(window.app), "Invalid desktop window");
                SaveValidation.Require(Finite(window.x) && Finite(window.y) && Finite(window.width) && Finite(window.height) &&
                    window.x >= 0 && window.x <= 1400 && window.y >= 0 && window.y <= 760 && window.width >= 240 && window.width <= 1536 &&
                    window.height >= 160 && window.height <= 820, "Invalid window bounds");
            }
            SaveValidation.Require(data.focusedApp == "" || data.windows.Any(w => w.app == data.focusedApp && !w.minimized), "Invalid focused app");
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static string ActionKey(WorldActionData a) => a.receipt + ":" + (int)a.kind + ":" + a.target;
        private static WorldActionData Clone(WorldActionData a) => new() { receipt = a.receipt, kind = a.kind, target = a.target };
        private static HomeMessageData Clone(HomeMessageData m) => new() { id = m.id, path = m.path, text = m.text, read = m.read };
        private static ComputerDesktopData Clone(ComputerDesktopData d) => new()
        {
            station = d.station, focusedApp = d.focusedApp, document = d.document,
            windows = d.windows.Select(w => new DesktopWindowData { app = w.app, minimized = w.minimized, x = w.x, y = w.y, width = w.width, height = w.height }).ToArray()
        };
    }
}
