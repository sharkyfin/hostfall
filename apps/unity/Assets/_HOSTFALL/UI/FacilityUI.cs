using System;
using System.Linq;
using Hostfall.Core;
using Hostfall.CameraSystem;
using Hostfall.Devices;
using Hostfall.Field;
using Hostfall.Mapping;
using Hostfall.Player;
using Hostfall.Persistence;
using Hostfall.Simulation;
using Hostfall.Simulation.Computers;
using Hostfall.World;
using UnityEngine;
using UnityEngine.UIElements;

namespace Hostfall.UI
{
    [DefaultExecutionOrder(200)]
    [RequireComponent(typeof(UIDocument))]
    public sealed class FacilityUI : MonoBehaviour
    {
        private FacilitySimulation simulation;
        private ControlState controls;
        private FacilityInput bindings;
        private TerminalSession session;
        private TerminalMemory memory;
        private WorldSaveController persistence;
        private FieldSystemUI fieldUI;
        private FieldSession field;
        private VisualElement terminal, pause, hud, menuMain, menuPage, front, introduction, facilityMap;
        private GamePreferences preferences;
        public GamePreferences Preferences => preferences;
        public bool HasSession => entered || persistence.Loaded;
        private Label objective, prompt, host, inputPrompt, taskHeading, menuTitle, pageTitle, menuError;
        private Label locationRoom, mapPlayerStatus;
        private Button resume, restart, openMap, closeMap;
        private Label typedCommand, routineStatus;
        private DeviceId activeTerminal;
        private string interactionText="", actionNotice="";
        private float noticeUntil;
        private ScrollView output, pageBody;
        private int renderedRevision;
        private string page = "", confirmSlot = "";
        private bool entered, confirmingNewGame;
        private PlayerMotor playerMotor;
        private bool isMapOpen;
        private ControlMode mapReturnMode;
        private float nextMapRefresh;private Label weaponStatus;private PlayerWeapon displayedWeapon;
        private int shownAmmo=-1,shownReserve=-1,shownKind=-1,shownCharge=-1;private bool shownReload,shownHot,weaponWasVisible;
        private FacilityMapWorld mapWorld;
        private FacilityMapView mapView, radar;
        public FacilityMapWorld MapWorld=>mapWorld;
        public FacilityMapView MapPlan=>mapView;
        public FacilityMapView Radar=>radar;
        private int terminalOpenedFrame = -1;
        private PlayerCameraRig cameraRig;
        private WorkstationSurface activeSurface;
        private ScenarioPlayback scenarioPlayback;
        private Action scenarioComplete;
        private bool playbackPaused;
        private float playbackSpeed=1;
        private void FocusScreen(DeviceId id)
        {
            if(activeSurface!=null)activeSurface.SetNativeOverlay(false);
            activeSurface=FindObjectsByType<WorkstationSurface>(FindObjectsSortMode.None).FirstOrDefault(s=>s.Device==id&&s.Valid);
            cameraRig??=FindAnyObjectByType<PlayerCameraRig>();
            terminal.AddToClassList("terminal-world");
            terminal.EnableInClassList("terminal-mac",activeSurface!=null&&activeSurface.Os==TerminalOsFamily.MacOsZsh);
            terminal.EnableInClassList("terminal-windows",activeSurface!=null&&activeSurface.Os==TerminalOsFamily.WindowsPowerShell);
            if(activeSurface!=null){cameraRig.Focus(activeSurface);terminal.style.visibility=Visibility.Visible;}
            else terminal.style.visibility=Visibility.Hidden;
        }
        public NearbyTerminalDisplay PhysicalDisplay {get;private set;}
        public OpeningSequence Opening {get;private set;}
        public VisualElement TerminalView=>terminal;
        private Button terminalClose;
        private TerminalMemory physicalMemory;private int physicalRevision=-2;private DeviceId physicalDevice;
        private void LateUpdate()
        {
            if(controls==null||PhysicalDisplay==null)return;
            var target=controls.Mode==ControlMode.Terminal?activeSurface:controls.Mode==ControlMode.Exploring?PhysicalDisplay.Choose(Camera.main):controls.Mode==ControlMode.Paused||controls.Mode==ControlMode.Field?PhysicalDisplay.Surface:null;
            PhysicalDisplay.Interactive=controls.Mode==ControlMode.Terminal&&cameraRig.FocusReady;
            bool changed=PhysicalDisplay.Select(target);PhysicalDisplay.PresentNativeGlass(controls.Mode==ControlMode.Terminal&&cameraRig.FocusReady,Camera.main);if(target==null)return;
            bool legacy=target.Device==DeviceId.Ws07||target.Device==DeviceId.Edge01||target.Device==DeviceId.Core01;
            var display=legacy?TerminalDisplay.Read(simulation,target.Device):null;
            if(legacy&&!simulation[target.Device].Powered){PhysicalDisplay.Select(null);return;}
            var playback=legacy?null:simulation.Screens.Find(target.Device);var current=legacy?display.Memory:playback?.Memory;
            TerminalScenarioCatalog.TryGetScenario(target.Device.ToString(),out var idle);
            host.text=legacy?display.Header:(playback?.Scenario??idle)?.GetOsTitle()??target.Device.Hostname().ToUpperInvariant();
            inputPrompt.text=legacy?display.Prompt:(playback?.Scenario??idle)?.FormatPrompt()??"";
            typedCommand.text=current?.Draft??"";
            routineStatus.text=legacy?simulation.Terminals.Status(target.Device):playback?.Complete==true?playback.Scenario.CompletionNotice:"";
            if(changed||physicalDevice!=target.Device||physicalMemory!=current||physicalRevision!=(current?.Revision??-1))
            {
                output.Clear();if(current!=null)foreach(var line in current.Lines)RenderLine(FormatTerminalOutput(line.text),line.style);
                if(current==null||current.Lines.Count==0)RenderLine("Канал готов","muted");
                physicalMemory=current;physicalRevision=current?.Revision??-1;physicalDevice=target.Device;
                output.schedule.Execute(()=>output.scrollOffset=new Vector2(0,Mathf.Max(0,output.verticalScroller.highValue)));
            }
        }
        private VisualElement healthBarFill, damageVignette, deathScreen;
        private bool deathVisible;
        private Label healthText, deathCauseLabel, deathStatsLabel;
        private VisualElement guardianHealth, guardianFill;
        private Label guardianName;
        private CrawlerMinibossController[] guardians = Array.Empty<CrawlerMinibossController>();
        private CoreAvatarEncounter coreAvatar;
        private FacilityMiniboss[] minibosses = Array.Empty<FacilityMiniboss>();
        private float nextGuardianRefresh;
        private Button deathRestartButton, deathMainMenuButton;
        private PlayerHealth playerHealth;
        private DeathController deathController;
        private float vignetteTimer;
        private FrontMenuFilm frontFilm;
        public void Initialize(FacilitySimulation model, ControlState state, FacilityInput inputBindings, FieldSession progress, FieldSystemUI fieldView, WorldSaveController saveController)
        {
            simulation=model; controls=state; bindings=inputBindings; field=progress; fieldUI=fieldView; persistence=saveController;
            var root=GetComponent<UIDocument>().rootVisualElement;
            terminal=root.Q("terminal"); pause=root.Q("pause"); hud=root.Q("hud"); front=root.Q("main-menu"); introduction=root.Q("introduction");
            facilityMap=root.Q("facility-map"); locationRoom=root.Q<Label>("location-room");
            mapPlayerStatus=root.Q<Label>("map-player-status");
            openMap=root.Q<Button>("open-map"); if (openMap != null) openMap.clicked += OpenMap;
            closeMap=root.Q<Button>("close-map"); if (closeMap != null) closeMap.clicked += CloseMap;
            healthBarFill = root.Q("health-bar-fill");
            healthText = root.Q<Label>("health-text");
            guardianHealth = root.Q("guardian-health");
            guardianFill = root.Q("guardian-health-fill");
            guardianName = root.Q<Label>("guardian-name");
            damageVignette = root.Q("damage-vignette");
            deathScreen = root.Q("death-screen");
            deathCauseLabel = root.Q<Label>("death-cause");
            deathStatsLabel = root.Q<Label>("death-stats");
            deathRestartButton = root.Q<Button>("death-restart");
            deathMainMenuButton = root.Q<Button>("death-mainmenu");
            if (deathRestartButton != null) deathRestartButton.clicked += () => { deathVisible = false; deathScreen.style.display = DisplayStyle.None; deathController?.RestartRaid(); };
            if (deathMainMenuButton != null) deathMainMenuButton.clicked += () => { deathVisible = false; deathScreen.style.display = DisplayStyle.None; ShowMainMenu(); };

            playerMotor=UnityEngine.Object.FindAnyObjectByType<PlayerMotor>();
            if (playerMotor != null)
            {
                BindPlayerHealth(playerMotor.GetComponent<PlayerHealth>(), playerMotor.GetComponent<DeathController>());
            }
            InitializeMap(root);
            preferences=new GamePreferences(playerMotor);
            objective=root.Q<Label>("objective"); prompt=root.Q<Label>("interaction"); host=root.Q<Label>("host"); inputPrompt=root.Q<Label>("input-prompt");
            taskHeading=root.Q<Label>("task-heading"); menuTitle=root.Q<Label>("menu-title"); resume=root.Q<Button>("resume");
            menuMain=root.Q("menu-main"); menuPage=root.Q("menu-page"); pageTitle=root.Q<Label>("menu-page-title"); pageBody=root.Q<ScrollView>("menu-page-body"); menuError=root.Q<Label>("menu-error");
            typedCommand=root.Q<Label>("automatic-command"); routineStatus=root.Q<Label>("routine-status"); output=root.Q<ScrollView>("output");
            output.horizontalScrollerVisibility=ScrollerVisibility.Hidden;
            output.verticalScroller.valueChanged+=value=> { if(memory!=null) memory.Scroll=Mathf.Max(0,value); };
            root.Q<Button>("open-journal").clicked+=fieldUI.OpenJournal; root.Q<Button>("open-archive").clicked+=fieldUI.OpenArchive;
            terminalClose=root.Q<Button>("close-terminal");terminalClose.clicked+=CloseTerminal;
            resume.clicked+=BeginSession;
            root.Q<Button>("front-continue").clicked+=BeginSession;
            root.Q<Button>("intro-understood").clicked+=()=> { field.AcknowledgeIntroduction(); controls.SetMode(ControlMode.Exploring); };
            root.Q<Button>("save-game").clicked+=()=>OpenPage("save"); root.Q<Button>("load-game").clicked+=()=>OpenPage("load");
            root.Q<Button>("front-settings").clicked+=()=>OpenPage("settings"); root.Q<Button>("pause-settings").clicked+=()=>OpenPage("settings");
            root.Q<Button>("return-main").clicked+=ShowMainMenu; root.Q<Button>("menu-back").clicked+=Back;
            restart=root.Q<Button>("restart"); restart.clicked+=()=>OpenPage("new-game");
            root.Q<Button>("quit").clicked+=Application.Quit;
            if (Application.platform == RuntimePlatform.WebGLPlayer)
                foreach (string control in new[] { "quit", "load-game", "save-game" })
                    root.Q<Button>(control).style.display = DisplayStyle.None;
            controls.Changed+=RefreshMode; simulation.StateChanged+=RefreshSimulation; persistence.StatusChanged+=OnSaveResult;
            cameraRig=FindAnyObjectByType<PlayerCameraRig>();
            if(!simulation.InteractiveComputers) PhysicalDisplay=new NearbyTerminalDisplay(GetComponent<UIDocument>(),terminal,FindObjectsByType<WorkstationSurface>(FindObjectsSortMode.None));
            else
            {
                frontFilm = gameObject.AddComponent<FrontMenuFilm>();
                frontFilm.Initialize(root.Q("front-film"));
                root.Q<Button>("open-archive").style.display=DisplayStyle.None;
                root.Q<Label>("intro-library").style.display=DisplayStyle.None;
                root.Q<Label>("intro-title").text = "Пробуждение";
                root.Q<Label>("intro-body").text = "Ты успел перенести сознание в сервисного робота. После аварии комплекс заперт, а охрана продолжает обход. Начни с домашнего терминала. Найди запись о смене и доберись до главного процессора.";
                root.Q<Label>("intro-controls").text = "E — использовать компьютер. Esc — отойти. J — открыть задания. Shift — бежать, Пробел — прыгнуть, Q — сделать рывок. C — отбиться вблизи.";
                if (Application.platform == RuntimePlatform.WebGLPlayer)
                    root.Q<Label>("intro-controls").text += "\nОсмотреться можно стрелками или удерживая правую кнопку мыши.";
            }
            Opening=gameObject.AddComponent<OpeningSequence>();Opening.Initialize(cameraRig,controls,playerMotor,root,!persistence.Loaded,BeginSession,simulation.InteractiveComputers);
            ResetPage(); RefreshSimulation(); controls.SetMode(ControlMode.MainMenu); OnSaveResult();
        }
        public void SetInteractionPrompt(string text) { interactionText=text; RefreshActionNotice(); }
        public void ShowActionNotice(string text) { actionNotice=text; noticeUntil=Time.unscaledTime+1.7f; RefreshActionNotice(); }
        private void RefreshActionNotice()
        {
            prompt.text=Time.unscaledTime<noticeUntil?actionNotice:interactionText;
            prompt.style.display=string.IsNullOrEmpty(prompt.text)?DisplayStyle.None:DisplayStyle.Flex;
        }
        public void OpenTerminal(DeviceId id)
        {
            if(simulation.InteractiveComputers)return;
            if(!simulation.Terminals.CanRun(id)) { ShowActionNotice(simulation.Terminals.Status(id)); return; }
            terminalOpenedFrame = Time.frameCount;
            scenarioPlayback = null; scenarioComplete=null;
            activeTerminal=id; session=new TerminalSession(simulation,id); memory=simulation[id].Terminal;
            simulation.Terminals.Begin(id);
            output.Clear(); foreach(var line in memory.Lines) RenderLine(line.text,line.style);
            renderedRevision=memory.Revision;
            RefreshPrompt(); FocusScreen(id); controls.SetMode(ControlMode.Terminal);
            rootFocus();
        }

        public void OpenTerminalScenario(TerminalScenario scenario, Action onComplete = null)
        {
            if(simulation.InteractiveComputers)return;
            if(scenario==null)return;
            terminalOpenedFrame=Time.frameCount;session=null;activeTerminal=scenario.DeviceId;
            scenarioPlayback=simulation.Screens.Present(scenario);scenarioComplete=onComplete;memory=scenarioPlayback.Memory;
            playbackPaused=false;playbackSpeed=1;
            output.Clear();foreach(var line in memory.Lines)RenderLine(FormatTerminalOutput(line.text),line.style);
            renderedRevision=memory.Revision;host.text=scenario.GetOsTitle();inputPrompt.text=scenario.FormatPrompt();
            typedCommand.text=memory.Draft;routineStatus.text=scenarioPlayback.Complete?scenario.CompletionNotice:"";
            FocusScreen(scenario.DeviceId);controls.SetMode(ControlMode.Terminal);rootFocus();
        }
        public void OpenTerminalSession(string hostName,string address,string[] commands,string[] outputs,Action onComplete=null)
        {
            // Legacy callers still supply trusted authored data. No player code is executed.
            var steps=(commands??Array.Empty<string>()).Select((c,i)=>new TerminalCommandStep(c,i<(outputs?.Length??0)?outputs[i]:"")).ToArray();
            OpenTerminalScenario(new TerminalScenario("legacy."+hostName,DeviceId.Ws07,TerminalOsFamily.LinuxBash,hostName,address,"~",steps,"Сеанс завершён",null),onComplete);
        }

        private void rootFocus()=>terminalClose?.Focus();
        private void CloseTerminal()
        {
            // Do not call OnComplete for unfinished session on exit (ADR 0007 / Rule 1.1 Item B)
            cameraRig?.ReleaseFocus();if(activeSurface!=null)activeSurface.SetNativeOverlay(false);activeSurface=null;
            session = null;
            memory = null;
            scenarioPlayback = null; scenarioComplete=null;
            terminalClose?.Blur();
            controls.SetMode(ControlMode.Exploring);
        }
        public void BeginSession()
        {
            if (playerHealth != null && playerHealth.IsDestroyed)
            {
                deathController?.TriggerDeath("Критическое разрушение корпуса");
                return;
            }
            if(simulation.InteractiveComputers && GetComponent<ComputerRuntimeUI>()?.ResumeComputer()==true)return;
            if(Opening!=null&&Opening.Playing)return;
            if(Opening!=null&&Opening.TryBegin())return;
            ResetPage(); controls.SetMode(field.IntroductionSeen?ControlMode.Exploring:ControlMode.Introduction);
        }
        public void ShowMainMenu() { ResetPage(); controls.SetMode(ControlMode.MainMenu); }
        private void ChooseLanguage(string language)
        {
            preferences.ChooseLanguage(language); confirmingNewGame=false;
            pageBody.Q<Button>("start-new-game").text="Начать игру"; RefreshFront();
        }
        private void RefreshFront()
        {
            var root=GetComponent<UIDocument>().rootVisualElement;
            root.Q<Button>("language-cpp")?.EnableInClassList("language-selected",preferences.NewGameLanguage=="cpp");
            root.Q<Button>("language-python")?.EnableInClassList("language-selected",preferences.NewGameLanguage=="python");
            root.Q<Button>("front-continue").style.display=HasSession?DisplayStyle.Flex:DisplayStyle.None;
        }
        private void Update()
        {
            if(bindings==null) return;
            if(deathVisible) return;
            RefreshActionNotice();
            RefreshObjective();
            RefreshWeaponStatus();
            RefreshGuardianHealth();

            if (vignetteTimer > 0f)
            {
                vignetteTimer -= Time.unscaledDeltaTime;
                if (vignetteTimer <= 0f && damageVignette != null)
                {
                    damageVignette.style.display = DisplayStyle.None;
                }
            }

            if(Time.unscaledTime>=nextMapRefresh && (controls.Mode==ControlMode.Exploring||controls.Mode==ControlMode.Map))
            { RefreshMap();nextMapRefresh=Time.unscaledTime+.1f; }

            if(controls.Mode==ControlMode.Computer)return;
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if(kb != null && kb.mKey.wasPressedThisFrame)
            {
                if(isMapOpen)
                {
                    CloseMap();
                    return;
                }
                else if(controls.Mode == ControlMode.Exploring || controls.Mode == ControlMode.Paused)
                {
                    OpenMap();
                    return;
                }
            }

            if(controls.Mode==ControlMode.Terminal && session!=null && (activeSurface==null||cameraRig.FocusReady))
            {
                bool fastForward = bindings != null && bindings.Pulse.WasPressedThisFrame();
                simulation.Terminals.Tick(activeTerminal, fastForward ? 5f : Time.unscaledDeltaTime * 2.5f);
                RefreshPrompt();
            }
            else if(controls.Mode==ControlMode.Terminal && scenarioPlayback!=null && (activeSurface==null||cameraRig.FocusReady))
            {
                TickCustomSession(Time.unscaledDeltaTime);
            }
            if(controls.Mode==ControlMode.Terminal && Time.frameCount > terminalOpenedFrame)
            {
                bool ePressed = UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.eKey.wasPressedThisFrame;
                if(bindings.Escape.WasPressedThisFrame() || ePressed)
                {
                    CloseTerminal();
                    return;
                }
            }
            if(bindings.Load.WasPressedThisFrame() && controls.Mode==ControlMode.MainMenu) { OpenPage("load"); return; }
            if(!bindings.Escape.WasPressedThisFrame()) return;
            if(isMapOpen) { CloseMap(); return; }
            if(controls.Mode==ControlMode.Field) fieldUI.Close();
            else if(controls.Mode==ControlMode.Terminal) CloseTerminal();
            else if(page.Length>0) Back();
            else if(controls.Mode==ControlMode.Paused) BeginSession();
            else if(controls.Mode==ControlMode.Exploring) controls.SetMode(ControlMode.Paused);
        }
        private void RefreshMode(ControlMode mode)
        {
            if(mode!=ControlMode.Terminal&&mode!=ControlMode.Computer){cameraRig?.ReleaseFocus();if(activeSurface!=null)activeSurface.SetNativeOverlay(false);activeSurface=null;}
            else if(activeSurface==null&&(session!=null||scenarioPlayback!=null))FocusScreen(activeTerminal);
            if(mode!=ControlMode.Map && isMapOpen)
            { isMapOpen=false;facilityMap.style.display=DisplayStyle.None; }
            hud.style.display=mode==ControlMode.Exploring?DisplayStyle.Flex:DisplayStyle.None;
            if(mode==ControlMode.Exploring) entered=true;
            if(mode!=ControlMode.Paused&&mode!=ControlMode.MainMenu) ResetPage();
            terminal.style.display=mode==ControlMode.Terminal?DisplayStyle.Flex:DisplayStyle.None;
            bool showFront = mode == ControlMode.MainMenu && page.Length == 0;
            front.style.display = showFront ? DisplayStyle.Flex : DisplayStyle.None;
            frontFilm?.SetVisible(showFront);
            pause.style.display=mode==ControlMode.Paused||(mode==ControlMode.MainMenu&&page.Length>0)?DisplayStyle.Flex:DisplayStyle.None;
            pause.EnableInClassList("front-dialog",mode==ControlMode.MainMenu);
            introduction.style.display=mode==ControlMode.Introduction?DisplayStyle.Flex:DisplayStyle.None;
            menuMain.style.display=page.Length==0?DisplayStyle.Flex:DisplayStyle.None;
            menuPage.style.display=page.Length>0?DisplayStyle.Flex:DisplayStyle.None;
            RefreshFront();
            var expectedMode=mode;
            GetComponent<UIDocument>().rootVisualElement.schedule.Execute(()=>
            {
                if(deathVisible||controls.Mode!=expectedMode||page.Length>0) return;
                if(mode==ControlMode.MainMenu)
                    GetComponent<UIDocument>().rootVisualElement.Q<Button>(HasSession?"front-continue":"restart").Focus();
                else if(mode==ControlMode.Introduction) GetComponent<UIDocument>().rootVisualElement.Q<Button>("intro-understood").Focus();
                else if(mode==ControlMode.Paused) resume.Focus();
            }).ExecuteLater(30);
            if(mode!=ControlMode.Exploring) SetInteractionPrompt("");
            if(mode==ControlMode.Terminal) rootFocus();
        }
        private void ResetPage()
        {
            page=""; confirmSlot=""; confirmingNewGame=false; restart.text="Новая игра";
            menuError.text=""; menuError.style.display=DisplayStyle.None;
        }
        private void Back()
        {
            if(page=="controls") { OpenPage("settings"); return; }
            ResetPage(); RefreshMode(controls.Mode);
        }
        public void OpenPage(string name)
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer && name is "load" or "save") return;
            if((name=="load"||name=="new-game")&&controls.Mode!=ControlMode.MainMenu) return;
            page=name; confirmSlot=""; confirmingNewGame=false;
            controls.SetMode(controls.Mode==ControlMode.MainMenu?ControlMode.MainMenu:ControlMode.Paused); BuildPage();
        }
        private void BuildPage()
        {
            pageBody.Clear(); menuError.style.display=DisplayStyle.None;
            pageTitle.text=page=="new-game"?"Новая игра":page=="save"?"Сохранить игру":page=="load"?"Загрузить игру":page=="settings"?"Настройки":"Управление";
            if(page=="new-game")
            {
                if (simulation.InteractiveComputers)
                {
                    var lore = new Label("На северном комплексе NORTHLINE пропала ночная смена. Перед потерей связи инженер перенёс своё сознание в сервисный корпус.\n\n" +
                        "Ты приходишь в себя в убежище. Комплекс заперт. Последняя запись о смене подписана твоим именем.") { name = "new-game-lore" };
                    lore.AddToClassList("new-game-lore");
                    pageBody.Add(lore);
                }
                else
                {
                var caption=new Label("Язык программирования"); caption.AddToClassList("front-caption"); pageBody.Add(caption);
                var languages=new VisualElement(); languages.AddToClassList("menu-row"); pageBody.Add(languages);
                languages.Add(new Button(()=>ChooseLanguage("cpp")) { name="language-cpp", text="C++" });
                languages.Add(new Button(()=>ChooseLanguage("python")) { name="language-python", text="Python" });
                }
                var start=new Button() { name="start-new-game", text="Начать игру" }; start.AddToClassList("primary");
                start.style.marginTop=20;
                start.clicked+=()=>
                {
                    if(confirmingNewGame||(!HasSession&&!persistence.HasSave)) persistence.NewGame(preferences.NewGameLanguage);
                    else { confirmingNewGame=true; start.text="Начать новую игру?"; }
                };
                pageBody.Add(start); RefreshFront(); FocusPage(); return;
            }
            if(page=="settings")
            {
                var volume=new Slider("Громкость",0,1) { name="setting-volume", value=preferences.Volume, showInputField=true };
                volume.RegisterValueChangedCallback(e=>preferences.SetVolume(e.newValue)); pageBody.Add(volume);
                var motion=new Slider("Движение камеры",0,1){name="setting-motion",value=preferences.MotionIntensity,showInputField=true};
                motion.RegisterValueChangedCallback(e=>preferences.SetMotion(e.newValue));pageBody.Add(motion);
                if (simulation.InteractiveComputers)
                {
                    var scares = new Slider("Резкие эффекты", 0, 1) { name = "setting-scares", value = preferences.ScareIntensity, showInputField = true };
                    scares.RegisterValueChangedCallback(e => preferences.SetScareIntensity(e.newValue));
                    pageBody.Add(scares);
                }
                var sensitivity=new Slider("Чувствительность мыши",.4f,2) { name="setting-sensitivity", value=preferences.Sensitivity, showInputField=true };
                sensitivity.RegisterValueChangedCallback(e=>preferences.SetSensitivity(e.newValue)); pageBody.Add(sensitivity);
                var fullscreen=new Toggle("Полный экран") { name="setting-fullscreen", value=preferences.Fullscreen };
                fullscreen.RegisterValueChangedCallback(e=>preferences.SetFullscreen(e.newValue)); pageBody.Add(fullscreen);
                var vsync=new Toggle("Вертикальная синхронизация") { name="setting-vsync", value=preferences.VSync };
                vsync.RegisterValueChangedCallback(e=>preferences.SetVSync(e.newValue)); pageBody.Add(vsync);
                pageBody.Add(new Button(()=>OpenPage("controls")) { name="open-controls", text="Управление" });
                pageBody.Add(new Button(()=> { preferences.Reset(); BuildPage(); }) { text="Сбросить звук, мышь и синхронизацию" }); FocusPage(); return;
            }
            if(page=="controls")
            {
                Group("Движение"); Binding("W A S D","Идти"); Binding("Мышь / ↑ ↓ ← →","Осмотреться");
                if (Application.platform == RuntimePlatform.WebGLPlayer) Binding("ПКМ + мышь", "Осмотреться при свободном курсоре");
                Binding("Shift","Бежать"); Binding("Пробел","Прыгнуть"); Binding("Q","Рывок по направлению движения"); Binding("V","Сменить вид");
                Group("Оружие");Binding("ЛКМ","Выстрел · удерживать для заряда TITAN");Binding("R","Перезарядить · ARC-12 можно прервать выстрелом");Binding("E","Подобрать оружие · прежнее выпадет рядом");Binding("G","ЭМИ-граната");
                Group("Действия"); Binding("E","Дверь / предмет / компьютер"); Binding("F","Направленный импульс против робота");
                if(simulation.InteractiveComputers)
                {
                    Binding("C", "Удар вблизи / отразить захват");
                    Binding("Компьютер","Мышь — приложения и окна. Стрелки / WASD — VENTFLOW. Ввод команд доступен дома.");
                    Binding("Esc","Немедленно отойти от компьютера");Binding("Tab / ↑ ↓","Домашний терминал: дополнение / история");
                }
                else { Binding("Терминал","Подойдите к экрану и нажмите E — герой проведёт сеанс сам"); Binding("Esc / E","Отойти от компьютера"); }
                Group("Интерфейс"); Binding("Esc","Назад / пауза"); Binding("J","Задания"); if(!simulation.InteractiveComputers)Binding("K","Знания"); if (Application.platform != RuntimePlatform.WebGLPlayer) Binding("F5","Быстрое сохранение"); FocusPage(); return;
            }
            bool saving=page=="save";
            foreach(var slot in persistence.Slots.Where(s=>!saving||!s.Automatic))
            {
                string id=slot.Id;
                var button=new Button(()=>
                {
                    if(!saving) { persistence.Load(id); return; }
                    if(slot.Exists && confirmSlot!=id) { confirmSlot=id; BuildPage(); return; }
                    if(persistence.Save(id)) { confirmSlot=""; BuildPage(); }
                }) { name="slot-"+id };
                button.AddToClassList("save-slot");
                button.Add(new Label(confirmSlot==id?"Перезаписать "+slot.Title.ToLowerInvariant()+"?":slot.Title) { pickingMode=PickingMode.Ignore });
                string detail=slot.Available?slot.Language+" · "+slot.SavedAt.ToString("dd.MM · HH:mm")+"\n"+slot.Objective:slot.Exists?"Файл недоступен":"Пусто";
                var meta=new Label(detail) { pickingMode=PickingMode.Ignore }; meta.AddToClassList("slot-detail"); button.Add(meta);
                if(!saving) button.SetEnabled(slot.Available); pageBody.Add(button);
            }
            pageBody.scrollOffset=Vector2.zero;
            FocusPage();
        }
        private void FocusPage()
        {
            string expected=page;
            pageBody.schedule.Execute(()=>
            {
                if(page!=expected||page.Length==0) return;
                if(page=="settings") pageBody.Q<Slider>()?.Focus();
                else (pageBody.Query<Button>().ToList().FirstOrDefault(b=>b.enabledInHierarchy)??GetComponent<UIDocument>().rootVisualElement.Q<Button>("menu-back")).Focus();
            }).ExecuteLater(30);
        }
        private void Group(string title) { var label=new Label(title); label.AddToClassList("binding-group"); pageBody.Add(label); }
        private void Binding(string key,string action)
        {
            var row=new VisualElement(); row.AddToClassList("binding-row"); var label=new Label(key); label.AddToClassList("binding-key"); row.Add(label); row.Add(new Label(action)); pageBody.Add(row);
        }
        private void OnSaveResult()
        {
            // Routine success stays silent; only a problem needs the player's attention.
            menuError.text=persistence.Status; menuError.style.display=persistence.Status.Length>0?DisplayStyle.Flex:DisplayStyle.None;
        }
        private void RefreshWeaponStatus()
        {
            if(playerMotor==null)return;
            var weapon=displayedWeapon??(displayedWeapon=playerMotor.GetComponent<PlayerWeapon>());
            if(weaponStatus==null)weaponStatus=hud.Q<Label>("weapon-status");
            if(weaponStatus==null)return;
            bool visible=weapon!=null&&weapon.HasWeapon;
            if(weaponWasVisible!=visible){weaponStatus.style.display=visible?DisplayStyle.Flex:DisplayStyle.None;weaponWasVisible=visible;}
            if(!visible)return;
            int percent=Mathf.RoundToInt(weapon.Charge*100);
            if(shownAmmo==weapon.CurrentAmmo&&shownReserve==weapon.ReserveAmmo&&shownKind==(int)weapon.ActiveWeapon&&shownCharge==percent&&shownReload==weapon.IsReloading&&shownHot==weapon.IsOverheated)return;
            shownAmmo=weapon.CurrentAmmo;shownReserve=weapon.ReserveAmmo;shownKind=(int)weapon.ActiveWeapon;shownCharge=percent;shownReload=weapon.IsReloading;shownHot=weapon.IsOverheated;
            string state=weapon.IsReloading?" · перезарядка":weapon.IsOverheated?" · охлаждение":weapon.Charge>.01f?" · "+Mathf.RoundToInt(weapon.Charge*100)+"%":"";
            string text=PlayerWeapon.DisplayName(weapon.ActiveWeapon)+"  "+weapon.CurrentAmmo+" / "+weapon.ReserveAmmo+state;
            if(weaponStatus.text!=text)weaponStatus.text=text;
        }
        public void AddWorldWeapon(Component item)
        {if(mapWorld!=null)mapWorld.Markers.Add(new Hostfall.Mapping.MapMarker{Source=item,Kind="item",Label="Оружие"});}
        private void RefreshObjective()
        {
            string text=simulation.Objective;
            if (simulation.Mission.CompletedObjectives==3 && !simulation.Mission.IsComplete)
                text=DataCartridgeState.IsInserted ? "Запустите сеанс DATA-01, чтобы открыть шлюзы."
                    : DataCartridgeState.HasCartridge ? "Установите картридж в считыватель слева от экрана DATA-01."
                    : "Найдите картридж на верхнем посту наблюдения. Лестница — справа от входа в вычислительный узел.";
            if (objective.text!=text) objective.text=text;
            bool complete = simulation.ComputerCampaign is { } campaign
                ? campaign.CurrentStage == CampaignStage.Epilogue
                : simulation.Mission.IsComplete;
            objective.EnableInClassList("complete", complete);
            taskHeading.text = complete ? "Выполнено" : "Задание";
        }
        private void RefreshSimulation()
        {
            RefreshObjective();
            RefreshPrompt();
            if(memory!=null && controls.Mode==ControlMode.Terminal)
            {
                typedCommand.text=memory.Draft;
                if(renderedRevision!=memory.Revision) RenderTranscript();
            }
        }
        private void RefreshPrompt()
        {
            if(session==null) return;
            host.text=session.Host.Hostname().ToUpperInvariant()+" / "+session.Computer.Address;
            inputPrompt.text=session.Prompt; typedCommand.text=memory.Draft;
            routineStatus.text=simulation.Terminals.Status(activeTerminal);
        }
        private void RenderTranscript()
        {
            output.Clear(); foreach(var entry in memory.Lines) RenderLine(FormatTerminalOutput(entry.text),entry.style);
            renderedRevision=memory.Revision;
            output.schedule.Execute(()=>output.scrollOffset=new Vector2(0,Mathf.Max(0,output.verticalScroller.highValue)));
        }
        private void RenderLine(string text,string style)
        {
            var label=new Label(text) { enableRichText=true, selection={isSelectable=true} }; label.AddToClassList(style); output.Add(label);
        }

        private static string FormatTerminalOutput(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            return raw
                .Replace("[OK]", "<color=#44FF77>[OK]</color>")
                .Replace("[SUCCESS]", "<color=#44FF77>[SUCCESS]</color>")
                .Replace("[NOMINAL]", "<color=#44FF77>[NOMINAL]</color>")
                .Replace("[ACTIVE]", "<color=#44FF77>[ACTIVE]</color>")
                .Replace("[UP]", "<color=#44FF77>[UP]</color>")
                .Replace("[FAIL]", "<color=#FF4444>[FAIL]</color>")
                .Replace("[ERROR]", "<color=#FF4444>[ERROR]</color>")
                .Replace("[CRITICAL]", "<color=#FF4444>[CRITICAL]</color>")
                .Replace("[ALERT]", "<color=#FF8844>[ALERT]</color>")
                .Replace("[DOWN]", "<color=#FF8844>[DOWN]</color>")
                .Replace("[EXPLOIT]", "<color=#FFAA22>[EXPLOIT]</color>")
                .Replace("[PAYLOAD]", "<color=#FFAA22>[PAYLOAD]</color>")
                .Replace("[ACTION REQUIRED]", "<color=#FFCC00>[ACTION REQUIRED]</color>");
        }

        public void TickCustomSession(float dt)
        {
            if(scenarioPlayback==null)return;
            var kb=UnityEngine.InputSystem.Keyboard.current;
            if(kb!=null)
            {
                if(kb.spaceKey.wasPressedThisFrame)playbackPaused=!playbackPaused;
                if(kb.leftBracketKey.wasPressedThisFrame)playbackSpeed=Mathf.Max(.5f,playbackSpeed-.25f);
                if(kb.rightBracketKey.wasPressedThisFrame)playbackSpeed=Mathf.Min(4,playbackSpeed+.5f);
            }
            if(!playbackPaused)scenarioPlayback.Tick(dt*playbackSpeed);
            typedCommand.text=memory.Draft;
            if(renderedRevision!=memory.Revision)RenderTranscript();
            routineStatus.text=scenarioPlayback.Complete?scenarioPlayback.Scenario.CompletionNotice:playbackPaused?"Приостановлено":"";
            simulation.NotifyTerminalChanged();
            if(scenarioPlayback.Complete&&scenarioComplete!=null&&scenarioPlayback.ClaimCompletion())
            {var callback=scenarioComplete;scenarioComplete=null;callback();}
        }
        private void InitializeMap(VisualElement root)
        {
            mapWorld=new FacilityMapWorld(simulation.Exploration);
            mapView=new FacilityMapView(mapWorld);radar=new FacilityMapView(mapWorld,true);
            root.Q("map-plan").Add(mapView);root.Q("radar-slot").Insert(0,radar);
            root.Q<Button>("map-center").clicked+=mapView.FocusPlayer;
            mapView.LevelChanged+=_=>RefreshMapLevelButtons();
            root.Q<Button>("map-overview").clicked+=mapView.FitAll;
            root.Q<Button>("map-minus").clicked+=()=>mapView.ZoomBy(1/1.3f);
            root.Q<Button>("map-plus").clicked+=()=>mapView.ZoomBy(1.3f);
            foreach(int level in mapWorld.Rooms.Select(r=>r.Level).Distinct().OrderBy(l=>l))
            {
                int selected=level;
                var button=new Button(()=>mapView.SetLevel(selected))
                    {name="map-level-"+level,text=level<0?"Нижний":level==0?"Основной":"Галереи"};
                root.Q("map-levels").Add(button);
            }
            root.RegisterCallback<GeometryChangedEvent>(e =>
            {
                root.EnableInClassList("hud-compact", e.newRect.width < 800 || e.newRect.height < 540);
                root.EnableInClassList("front-stacked", e.newRect.width < 900);
            });
            RefreshMap();
        }
        public void RefreshMap()
        {
            if(mapWorld==null||playerMotor==null)return;
            mapWorld.Refresh();var position=playerMotor.transform.position;
            if(controls.Mode==ControlMode.Exploring)mapWorld.Visit(position);
            var room=mapWorld.At(position);
            locationRoom.text=room?.Name??"Переход";
            mapPlayerStatus.text=room?.Name??"Переход";
            radar.Refresh(position,playerMotor.transform.eulerAngles.y);
            if(isMapOpen)mapView.Refresh(position,playerMotor.transform.eulerAngles.y);
        }
        private void RefreshMapLevelButtons()
        {
            foreach(var button in GetComponent<UIDocument>().rootVisualElement.Q("map-levels").Query<Button>().ToList())
                button.EnableInClassList("map-level-selected",button.name=="map-level-"+mapView.Level);
        }
        public void OpenMap()
        {
            if(facilityMap==null||isMapOpen||page.Length>0||!(controls.Mode==ControlMode.Exploring||controls.Mode==ControlMode.Paused))return;
            RefreshMap();mapReturnMode=controls.Mode;isMapOpen=true;controls.SetMode(ControlMode.Map);
            facilityMap.style.display=DisplayStyle.Flex;
            mapView.Refresh(playerMotor.transform.position,playerMotor.transform.eulerAngles.y);
            mapView.FocusPlayer();RefreshMapLevelButtons();closeMap?.Focus();
        }
        public void CloseMap()
        {
            if(!isMapOpen)return;
            isMapOpen=false;facilityMap.style.display=DisplayStyle.None;
            if(controls.Mode==ControlMode.Map)controls.SetMode(mapReturnMode);
            if(controls.Mode==ControlMode.Paused)resume?.Focus();
        }

        public void TriggerDamageFlash()
        {
            if (damageVignette == null) return;
            damageVignette.style.display = DisplayStyle.Flex;
            damageVignette.style.borderTopWidth = 24;
            damageVignette.style.borderBottomWidth = 24;
            damageVignette.style.borderLeftWidth = 24;
            damageVignette.style.borderRightWidth = 24;
            vignetteTimer = 0.35f;
        }

        public void ShowDeathScreen(string cause, float time)
        {
            if (deathScreen == null) return;
            deathVisible = true;
            pause.style.display = DisplayStyle.None;
            deathScreen.style.display = DisplayStyle.Flex;
            if (deathCauseLabel != null) deathCauseLabel.text = "Причина: " + cause;
            if (deathStatsLabel != null)
            {
                int min = Mathf.FloorToInt(time / 60f);
                int sec = Mathf.FloorToInt(time % 60f);
                deathStatsLabel.text = $"Время рейда: {min:D2}:{sec:D2}  |  Комплекс NORTHLINE";
            }
            deathRestartButton?.Focus();
        }

        public void BindPlayerHealth(PlayerHealth hp, DeathController dc)
        {
            if (playerHealth != null) playerHealth.HealthChanged -= UpdateHealthUI;
            playerHealth = hp;
            deathController = dc;
            if (playerHealth != null)
            {
                playerHealth.HealthChanged += UpdateHealthUI;
                UpdateHealthUI(playerHealth.CurrentHealth, playerHealth.MaxHealth);
            }
        }

        public void BindGuardians(CrawlerMinibossController[] actors) => guardians = actors;
        public void BindCoreAvatar(CoreAvatarEncounter encounter) => coreAvatar = encounter;
        public void BindMinibosses(FacilityMiniboss[] encounters) => minibosses = encounters;

        private void RefreshGuardianHealth()
        {
            if (guardianHealth == null || Time.unscaledTime < nextGuardianRefresh) return;
            nextGuardianRefresh = Time.unscaledTime + .1f;
            if (controls.Mode == ControlMode.Exploring && coreAvatar != null && coreAvatar.Visible)
            {
                guardianHealth.style.display = DisplayStyle.Flex;
                guardianFill.style.width = Length.Percent(coreAvatar.Health / Hostfall.Simulation.Computers.CoreAvatarBattle.Maximum * 100);
                guardianName.text = coreAvatar.IsStunned ? "SUPER · Реактор открыт" : "SUPER · Фаза " + coreAvatar.Battle.Phase;
                return;
            }
            if (controls.Mode == ControlMode.Exploring)
                foreach (var boss in minibosses)
                {
                    if (boss == null || !boss.Visible) continue;
                    guardianHealth.style.display = DisplayStyle.Flex;
                    guardianName.text = boss.IsStunned ? boss.Caption + " · защита открыта" : boss.Caption;
                    guardianFill.style.width = Length.Percent(boss.Health / boss.Maximum * 100);
                    return;
                }
            RobotCombatState visible = null;
            float nearest = 20 * 20;
            if (controls.Mode == ControlMode.Exploring && playerMotor != null)
                foreach (var guardian in guardians)
                {
                    if (guardian == null || !guardian.IsEngaged) continue;
                    float distance = (guardian.transform.position - playerMotor.transform.position).sqrMagnitude;
                    if (distance >= nearest) continue;
                    nearest = distance;
                    visible = guardian.EncounterHealth;
                }
            guardianHealth.style.display = visible != null ? DisplayStyle.Flex : DisplayStyle.None;
            if (visible == null) return;
            guardianFill.style.width = Length.Percent(visible.Health / visible.Maximum * 100f);
            guardianName.text = visible.CoreExposed ? "MANTIS-6 · Реактор открыт" : "MANTIS-6";
        }

        public void UpdateHealthUI(float current, float max)
        {
            if (healthText != null) healthText.text = $"{Mathf.CeilToInt(current)}%";
            if (healthBarFill != null) healthBarFill.style.width = Length.Percent(Mathf.Clamp01(current / max) * 100f);
        }

        private void OnDestroy()
        { PhysicalDisplay?.Dispose(); if(controls!=null) controls.Changed-=RefreshMode; if(simulation!=null) simulation.StateChanged-=RefreshSimulation; if(persistence!=null) persistence.StatusChanged-=OnSaveResult; }
    }
}
