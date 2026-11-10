using System;
using System.Linq;
using Hostfall.Core;
using Hostfall.Field;
using Hostfall.Player;
using Hostfall.Persistence;
using Hostfall.Simulation;
using UnityEngine;
using UnityEngine.UIElements;

namespace Hostfall.UI
{
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
        private VisualElement terminal, pause, hud, menuMain, menuPage, front, introduction, facilityMap, mapSectorsContainer;
        private GamePreferences preferences;
        public GamePreferences Preferences => preferences;
        public bool HasSession => entered || persistence.Loaded;
        private Label objective, prompt, host, inputPrompt, taskHeading, menuTitle, pageTitle, menuError;
        private Label locationSector, locationRoom, mapPlayerStatus;
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
        private int terminalOpenedFrame = -1;
        private VisualElement healthBarFill, damageVignette, deathScreen;
        private bool deathVisible;
        private Label healthText, deathCauseLabel, deathStatsLabel;
        private Button deathRestartButton, deathMainMenuButton;
        private PlayerHealth playerHealth;
        private DeathController deathController;
        private float vignetteTimer;
        public void Initialize(FacilitySimulation model, ControlState state, FacilityInput inputBindings, FieldSession progress, FieldSystemUI fieldView, WorldSaveController saveController)
        {
            simulation=model; controls=state; bindings=inputBindings; field=progress; fieldUI=fieldView; persistence=saveController;
            var root=GetComponent<UIDocument>().rootVisualElement;
            terminal=root.Q("terminal"); pause=root.Q("pause"); hud=root.Q("hud"); front=root.Q("main-menu"); introduction=root.Q("introduction");
            facilityMap=root.Q("facility-map"); locationSector=root.Q<Label>("location-sector"); locationRoom=root.Q<Label>("location-room");
            mapPlayerStatus=root.Q<Label>("map-player-status"); mapSectorsContainer=root.Q("map-sectors-container");
            openMap=root.Q<Button>("open-map"); if (openMap != null) openMap.clicked += OpenMap;
            closeMap=root.Q<Button>("close-map"); if (closeMap != null) closeMap.clicked += CloseMap;
            healthBarFill = root.Q("health-bar-fill");
            healthText = root.Q<Label>("health-text");
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
            preferences=new GamePreferences(playerMotor);
            objective=root.Q<Label>("objective"); prompt=root.Q<Label>("interaction"); host=root.Q<Label>("host"); inputPrompt=root.Q<Label>("input-prompt");
            taskHeading=root.Q<Label>("task-heading"); menuTitle=root.Q<Label>("menu-title"); resume=root.Q<Button>("resume");
            menuMain=root.Q("menu-main"); menuPage=root.Q("menu-page"); pageTitle=root.Q<Label>("menu-page-title"); pageBody=root.Q<ScrollView>("menu-page-body"); menuError=root.Q<Label>("menu-error");
            typedCommand=root.Q<Label>("automatic-command"); routineStatus=root.Q<Label>("routine-status"); output=root.Q<ScrollView>("output");
            output.horizontalScrollerVisibility=ScrollerVisibility.Hidden;
            output.verticalScroller.valueChanged+=value=> { if(memory!=null) memory.Scroll=Mathf.Max(0,value); };
            root.Q<Button>("open-journal").clicked+=fieldUI.OpenJournal; root.Q<Button>("open-archive").clicked+=fieldUI.OpenArchive;
            root.Q<Button>("close-terminal").clicked+=CloseTerminal;
            resume.clicked+=BeginSession;
            root.Q<Button>("front-continue").clicked+=BeginSession;
            root.Q<Button>("intro-understood").clicked+=()=> { field.AcknowledgeIntroduction(); controls.SetMode(ControlMode.Exploring); };
            root.Q<Button>("save-game").clicked+=()=>OpenPage("save"); root.Q<Button>("load-game").clicked+=()=>OpenPage("load");
            root.Q<Button>("front-settings").clicked+=()=>OpenPage("settings"); root.Q<Button>("pause-settings").clicked+=()=>OpenPage("settings");
            root.Q<Button>("return-main").clicked+=ShowMainMenu; root.Q<Button>("menu-back").clicked+=Back;
            restart=root.Q<Button>("restart"); restart.clicked+=()=>OpenPage("new-game");
            root.Q<Button>("quit").clicked+=Application.Quit;
            controls.Changed+=RefreshMode; simulation.StateChanged+=RefreshSimulation; persistence.StatusChanged+=OnSaveResult;
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
            if(!simulation.Terminals.CanRun(id)) { ShowActionNotice(simulation.Terminals.Status(id)); return; }
            terminalOpenedFrame = Time.frameCount;
            customSession = null;
            activeTerminal=id; session=new TerminalSession(simulation,id); memory=simulation[id].Terminal;
            simulation.Terminals.Begin(id);
            output.Clear(); foreach(var line in memory.Lines) RenderLine(line.text,line.style);
            renderedRevision=memory.Revision;
            RefreshPrompt(); controls.SetMode(ControlMode.Terminal);
            rootFocus();
        }

        private struct CustomSession
        {
            public string Host;
            public string Address;
            public string WorkingDir;
            public TerminalOsFamily OsFamily;
            public string[] Commands;
            public string[] Outputs;
            public int Step;
            public int Typed;
            public float Wait;
            public float SpeedMultiplier;
            public bool IsPaused;
            public string PromptFormat;
            public string EventId;
            public Action OnComplete;
        }
        private CustomSession? customSession;

        public void OpenTerminalScenario(TerminalScenario scenario, Action onComplete = null)
        {
            if (scenario == null) return;
            terminalOpenedFrame = Time.frameCount;
            session = null; memory = null;

            var cmds = new string[scenario.Steps.Count];
            var outs = new string[scenario.Steps.Count];
            for (int i = 0; i < scenario.Steps.Count; i++)
            {
                cmds[i] = scenario.Steps[i].Command;
                outs[i] = scenario.Steps[i].Output;
            }

            customSession = new CustomSession
            {
                Host = scenario.Hostname,
                Address = scenario.IpAddress,
                WorkingDir = scenario.WorkingDirectory,
                OsFamily = scenario.OsFamily,
                Commands = cmds,
                Outputs = outs,
                Step = 0,
                Typed = 0,
                Wait = 0,
                SpeedMultiplier = 1.0f,
                IsPaused = false,
                PromptFormat = scenario.FormatPrompt(),
                EventId = scenario.WorldEventId,
                OnComplete = onComplete
            };
            output.Clear();
            host.text = scenario.GetOsTitle();
            inputPrompt.text = scenario.FormatPrompt();
            typedCommand.text = "";
            routineStatus.text = "Авторизация канала · Скорость: 1.0x [SPACE - пауза, [ / ] - скорость]";
            controls.SetMode(ControlMode.Terminal);
            rootFocus();
        }

        public void OpenTerminalSession(string hostName, string address, string[] commands, string[] outputs, Action onComplete = null)
        {
            terminalOpenedFrame = Time.frameCount;
            session = null; memory = null;
            customSession = new CustomSession
            {
                Host = hostName,
                Address = address,
                WorkingDir = "~",
                OsFamily = TerminalOsFamily.LinuxBash,
                Commands = commands ?? Array.Empty<string>(),
                Outputs = outputs ?? Array.Empty<string>(),
                Step = 0,
                Typed = 0,
                Wait = 0,
                SpeedMultiplier = 1.0f,
                IsPaused = false,
                PromptFormat = "operator@" + hostName + ":~$ ",
                EventId = null,
                OnComplete = onComplete
            };
            output.Clear();
            host.text = hostName.ToUpperInvariant() + " / " + address;
            inputPrompt.text = "operator@" + hostName + ":~$ ";
            typedCommand.text = "";
            routineStatus.text = "Авторизация канала · Скорость: 1.0x [SPACE - пауза, [ / ] - скорость]";
            controls.SetMode(ControlMode.Terminal);
            rootFocus();
        }

        private void rootFocus()=>GetComponent<UIDocument>().rootVisualElement.Q<Button>("close-terminal").Focus();
        private void CloseTerminal()
        {
            // Do not call OnComplete for unfinished session on exit (ADR 0007 / Rule 1.1 Item B)
            session = null;
            memory = null;
            customSession = null;
            GetComponent<UIDocument>()?.rootVisualElement?.Q<Button>("close-terminal")?.Blur();
            controls.SetMode(ControlMode.Exploring);
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;
        }
        public void BeginSession()
        {
            if (playerHealth != null && playerHealth.IsDestroyed)
            {
                deathController?.TriggerDeath("Критическое разрушение корпуса");
                return;
            }
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

            if (vignetteTimer > 0f)
            {
                vignetteTimer -= Time.unscaledDeltaTime;
                if (vignetteTimer <= 0f && damageVignette != null)
                {
                    damageVignette.style.display = DisplayStyle.None;
                }
            }

            if(locationSector != null && locationRoom != null)
            {
                if(playerMotor == null) playerMotor = UnityEngine.Object.FindAnyObjectByType<PlayerMotor>();
                Vector3 playerPos = playerMotor != null ? playerMotor.transform.position : 
                    (Camera.main != null ? Camera.main.transform.position : Vector3.zero);
                var curSector = GetCurrentSectorInfo(playerPos);
                locationSector.text = curSector.Code;
                locationRoom.text = curSector.Name;
            }

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

            if(controls.Mode==ControlMode.Terminal && memory!=null)
            {
                bool fastForward = bindings != null && (bindings.Interact.WasPressedThisFrame() || bindings.Pulse.WasPressedThisFrame());
                simulation.Terminals.Tick(activeTerminal, fastForward ? 5f : Time.unscaledDeltaTime * 2.5f);
                RefreshPrompt();
            }
            else if(controls.Mode==ControlMode.Terminal && customSession.HasValue)
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
            if(mode!=ControlMode.Exploring && mode!=ControlMode.Paused && isMapOpen) CloseMap();
            hud.style.display=mode==ControlMode.Exploring?DisplayStyle.Flex:DisplayStyle.None;
            if(mode==ControlMode.Exploring) entered=true;
            if(mode!=ControlMode.Paused&&mode!=ControlMode.MainMenu) ResetPage();
            terminal.style.display=mode==ControlMode.Terminal?DisplayStyle.Flex:DisplayStyle.None;
            front.style.display=mode==ControlMode.MainMenu&&page.Length==0?DisplayStyle.Flex:DisplayStyle.None;
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
                var caption=new Label("Язык программирования"); caption.AddToClassList("front-caption"); pageBody.Add(caption);
                var languages=new VisualElement(); languages.AddToClassList("menu-row"); pageBody.Add(languages);
                languages.Add(new Button(()=>ChooseLanguage("cpp")) { name="language-cpp", text="C++" });
                languages.Add(new Button(()=>ChooseLanguage("python")) { name="language-python", text="Python" });
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
                Group("Движение"); Binding("W A S D","Идти"); Binding("Мышь","Осмотреться"); Binding("Shift","Бежать"); Binding("Пробел","Прыгнуть"); Binding("Q","Рывок по направлению движения"); Binding("V","Сменить вид");
                Group("Действия"); Binding("E","Дверь / предмет / повторный просмотр терминала"); Binding("F","Направленный импульс против робота"); Binding("Терминал","Подойдите и задержите взгляд — герой подключится сам");
                Group("Интерфейс"); Binding("Esc","Назад / пауза"); Binding("J","Задания"); Binding("K","Знания"); Binding("F5","Быстрое сохранение"); FocusPage(); return;
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
        private void RefreshSimulation()
        {
            objective.text=simulation.Objective; objective.EnableInClassList("complete",simulation.Mission.IsComplete);
            taskHeading.text=simulation.Mission.IsComplete?"Выполнено":"Задание"; RefreshPrompt();
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
            output.Clear(); foreach(var entry in memory.Lines) RenderLine(entry.text,entry.style);
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
            if(!customSession.HasValue || dt <= 0 || float.IsNaN(dt)) return;
            var cs = customSession.Value;
            if(cs.Step >= cs.Commands.Length) return;

            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null)
            {
                if (kb.spaceKey.wasPressedThisFrame)
                {
                    cs.IsPaused = !cs.IsPaused;
                    routineStatus.text = cs.IsPaused
                        ? "Сеанс приостановлен [SPACE - продолжить]"
                        : $"Авторизация канала · Скорость: {cs.SpeedMultiplier:F1}x [SPACE - пауза, [ / ] - скорость]";
                    customSession = cs;
                    return;
                }
                if (kb.leftBracketKey.wasPressedThisFrame)
                {
                    cs.SpeedMultiplier = Mathf.Max(0.5f, cs.SpeedMultiplier - 0.25f);
                    routineStatus.text = $"Авторизация канала · Скорость: {cs.SpeedMultiplier:F1}x [SPACE - пауза, [ / ] - скорость]";
                    customSession = cs;
                }
                else if (kb.rightBracketKey.wasPressedThisFrame)
                {
                    cs.SpeedMultiplier = Mathf.Min(4.0f, cs.SpeedMultiplier + 0.5f);
                    routineStatus.text = $"Авторизация канала · Скорость: {cs.SpeedMultiplier:F1}x [SPACE - пауза, [ / ] - скорость]";
                    customSession = cs;
                }
            }

            if (cs.IsPaused) return;

            string pfx = string.IsNullOrEmpty(cs.PromptFormat) ? ("operator@" + cs.Host + ":~$ ") : cs.PromptFormat;

            bool fastForward = bindings != null && (bindings.Interact.WasPressedThisFrame() || bindings.Pulse.WasPressedThisFrame());
            if (fastForward)
            {
                while (cs.Step < cs.Commands.Length)
                {
                    string fcmd = cs.Commands[cs.Step];
                    RenderLine("<color=#55AAFF>" + pfx + "</color>" + fcmd, "prompt");
                    if (cs.Step < cs.Outputs.Length && !string.IsNullOrEmpty(cs.Outputs[cs.Step]))
                    {
                        RenderLine(FormatTerminalOutput(cs.Outputs[cs.Step]), "result");
                    }
                    cs.Step++;
                }
                cs.Typed = 0;
                cs.Wait = 0;
                typedCommand.text = "";
                routineStatus.text = "Сеанс завершён · Доступ открыт";
                output.schedule.Execute(() => output.scrollOffset = new Vector2(0, Mathf.Max(0, output.verticalScroller.highValue)));
                var callback = cs.OnComplete;
                cs.OnComplete = null;
                customSession = cs;
                callback?.Invoke();
                return;
            }

            string cmd = cs.Commands[cs.Step];
            cs.Wait += Mathf.Min(dt * cs.SpeedMultiplier, 0.25f);

            if(cs.Typed < cmd.Length)
            {
                int count = (int)(cs.Wait * 450);
                if(count > 0)
                {
                    cs.Wait -= count / 450f;
                    cs.Typed = Mathf.Min(cmd.Length, cs.Typed + count);
                    typedCommand.text = cmd.Substring(0, cs.Typed);
                    customSession = cs;
                }
                return;
            }

            if(cs.Wait < 0.06f) { customSession = cs; return; }

            // Commit command
            RenderLine("<color=#55AAFF>" + pfx + "</color>" + cmd, "prompt");
            if(cs.Step < cs.Outputs.Length && !string.IsNullOrEmpty(cs.Outputs[cs.Step]))
            {
                RenderLine(FormatTerminalOutput(cs.Outputs[cs.Step]), "result");
            }
            output.schedule.Execute(()=>output.scrollOffset=new Vector2(0,Mathf.Max(0,output.verticalScroller.highValue)));

            cs.Step++;
            cs.Typed = 0;
            cs.Wait = 0;
            typedCommand.text = "";

            if(cs.Step == cs.Commands.Length)
            {
                routineStatus.text = "Сеанс завершён · Доступ открыт";
                var callback = cs.OnComplete;
                cs.OnComplete = null;
                customSession = cs;
                callback?.Invoke();
                return;
            }

            customSession = cs;
        }
        public struct SectorMapEntry
        {
            public string Code;
            public string Name;
            public string Detail;
            public string Connections;
            public Bounds BoundingBox;
        }

        private static readonly SectorMapEntry[] FacilitySectors = new SectorMapEntry[]
        {
            new SectorMapEntry
            {
                Code = "УБЕЖИЩЕ",
                Name = "Мастерская инженера [HOME]",
                Detail = "Автономное укрытие. Терминал HOME-PC, верстак ремонта Type-IV, архив инцидента.",
                Connections = "Связь: Сектор 00 (Через сервисный шлюз SEC-HOME)",
                BoundingBox = new Bounds(new Vector3(0, 1.5f, -14.0f), new Vector3(12f, 8f, 12f))
            },
            new SectorMapEntry
            {
                Code = "СЕКТОР 00",
                Name = "Операторская (SOC)",
                Detail = "Точка эвакуации и мониторинга комплекса. Терминал WS-07, главный энергощит.",
                Connections = "Связь: Сектор 01 (Служебный коридор)",
                BoundingBox = new Bounds(new Vector3(0, 1.5f, -3.5f), new Vector3(12f, 8f, 13f))
            },
            new SectorMapEntry
            {
                Code = "СЕКТОР 01",
                Name = "Серверный зал и коридор",
                Detail = "Служебный проход, узел распределения охлаждения, терминал CORE-01.",
                Connections = "Связь: Сектор 00 ↔ Сектор 02",
                BoundingBox = new Bounds(new Vector3(0, 2f, 10f), new Vector3(12f, 8f, 14f))
            },
            new SectorMapEntry
            {
                Code = "СЕКТОР 02",
                Name = "Вычислительный узел",
                Detail = "Стойки квантовых вычислений, консоль DATA-01, смотровой мезонин.",
                Connections = "Связь: Сектор 01 ↔ Арсенал ↔ Сектор 03 ↔ Сектор 04",
                BoundingBox = new Bounds(new Vector3(0, 2.5f, 27.5f), new Vector3(14f, 8f, 21f))
            },
            new SectorMapEntry
            {
                Code = "СЕКТОР 02-А",
                Name = "Оружейный арсенал [SEC-02]",
                Detail = "Тактический дробовик ARC-12, стеллажи боеприпасов, шкафы амуниции.",
                Connections = "Связь: Прямой доступ из Сектора 02",
                BoundingBox = new Bounds(new Vector3(11f, 2f, 26f), new Vector3(8f, 8f, 12f))
            },
            new SectorMapEntry
            {
                Code = "СЕКТОР 03",
                Name = "Энергоцентр",
                Detail = "Трансформаторные подстанции, распределительные шины, консоль SUB-01.",
                Connections = "Связь: Сектор 02 ↔ Служебный обход",
                BoundingBox = new Bounds(new Vector3(-16.5f, 3f, 28f), new Vector3(19f, 9f, 20f))
            },
            new SectorMapEntry
            {
                Code = "СЕКТОР 04",
                Name = "Квантовый узел",
                Detail = "Криогенный шлюз CRYO-01, эстакадный мост, транзитные коммуникационные траншеи.",
                Connections = "Связь: Сектор 02 ↔ Сектор 05 (Логистика)",
                BoundingBox = new Bounds(new Vector3(0, 3f, 50f), new Vector3(20f, 9f, 24f))
            },
            new SectorMapEntry
            {
                Code = "СЕКТОР 05",
                Name = "Логистический центр",
                Detail = "Грузовой терминал, контейнерные платформы, патрули автономных стражей.",
                Connections = "Связь: Сектор 04 ↔ Сектор 06 ↔ Обратный шлюз Сектора 09",
                BoundingBox = new Bounds(new Vector3(-5f, 4f, 78.5f), new Vector3(42f, 10f, 35f))
            },
            new SectorMapEntry
            {
                Code = "СЕКТОР 06",
                Name = "Завод робототехники",
                Detail = "Линии автоматической сборки, подвесные мостки, калибровочные станции.",
                Connections = "Связь: Сектор 05 ↔ Сектор 07 ↔ Сектор 08 (Нейроархив)",
                BoundingBox = new Bounds(new Vector3(33.5f, 5f, 80f), new Vector3(38f, 12f, 32f))
            },
            new SectorMapEntry
            {
                Code = "СЕКТОР 07",
                Name = "Цитадель безопасности",
                Detail = "Укрепленный блокпост, тяжелые рельсовые турели RailEye, охранный рубеж.",
                Connections = "Связь: Сектор 06 ↔ Скоростной переход в Сектор 00",
                BoundingBox = new Bounds(new Vector3(35f, 4f, 38f), new Vector3(40f, 10f, 50f))
            },
            new SectorMapEntry
            {
                Code = "СЕКТОР 08",
                Name = "Нейроархив",
                Detail = "Хранилище матриц личности и бэкапов архитектуры SUPER, монолиты памяти.",
                Connections = "Связь: Сектор 06 ↔ Сектор 09 (Облачный узел)",
                BoundingBox = new Bounds(new Vector3(71f, 4f, 52f), new Vector3(36f, 10f, 70f))
            },
            new SectorMapEntry
            {
                Code = "СЕКТОР 09",
                Name = "Облачный узел управления",
                Detail = "Релейные мачты синхронизации, реакторный распределитель, терминалы фазового доступа.",
                Connections = "Связь: Сектор 08 ↔ Сектор 10 ↔ Шлюз в Логистику (С05)",
                BoundingBox = new Bounds(new Vector3(36f, 4f, 105f), new Vector3(45f, 10f, 45f))
            },
            new SectorMapEntry
            {
                Code = "СЕКТОР 10",
                Name = "Ядро суперкомпьютера",
                Detail = "Главная камера центрального процессора. Арена с автономным боссом ATLAS-0.",
                Connections = "Связь: Финальный рубеж комплекса NORTHLINE",
                BoundingBox = new Bounds(new Vector3(10f, 6f, 160f), new Vector3(55f, 15f, 80f))
            }
        };

        public static SectorMapEntry GetCurrentSectorInfo(Vector3 pos)
        {
            if (pos.z < -8.5f)
                return FacilitySectors[0];

            if (FacilitySectors[4].BoundingBox.Contains(pos))
                return FacilitySectors[4];

            for (int i = 0; i < FacilitySectors.Length; i++)
            {
                if (i == 4) continue;
                if (FacilitySectors[i].BoundingBox.Contains(pos))
                    return FacilitySectors[i];
            }

            float closestDist = float.MaxValue;
            int bestIdx = 1;
            for (int i = 0; i < FacilitySectors.Length; i++)
            {
                float d = Vector3.Distance(pos, FacilitySectors[i].BoundingBox.center);
                if (d < closestDist)
                {
                    closestDist = d;
                    bestIdx = i;
                }
            }
            return FacilitySectors[bestIdx];
        }

        public void OpenMap()
        {
            if (facilityMap == null) return;
            isMapOpen = true;
            facilityMap.style.display = DisplayStyle.Flex;
            BuildMapCards();
            closeMap?.Focus();
        }

        public void CloseMap()
        {
            if (facilityMap == null) return;
            isMapOpen = false;
            facilityMap.style.display = DisplayStyle.None;
            if (controls != null && controls.Mode == ControlMode.Paused)
                resume?.Focus();
        }

        private void BuildMapCards()
        {
            if (mapSectorsContainer == null) return;
            mapSectorsContainer.Clear();

            Vector3 playerPos = playerMotor != null ? playerMotor.transform.position : 
                (Camera.main != null ? Camera.main.transform.position : Vector3.zero);
            var currentSector = GetCurrentSectorInfo(playerPos);

            if (mapPlayerStatus != null)
            {
                mapPlayerStatus.text = $"{currentSector.Code} · {currentSector.Name}   [ X: {playerPos.x:F1}  Y: {playerPos.y:F1}  Z: {playerPos.z:F1} ]";
            }

            for (int i = 0; i < FacilitySectors.Length; i++)
            {
                var entry = FacilitySectors[i];
                bool isCurrent = entry.Code == currentSector.Code;

                var card = new VisualElement();
                card.AddToClassList("map-sector-card");
                if (isCurrent)
                {
                    card.AddToClassList("map-sector-current");
                }

                var head = new VisualElement();
                head.AddToClassList("map-card-head");

                var codeLabel = new Label(entry.Code);
                codeLabel.AddToClassList("map-card-code");
                head.Add(codeLabel);

                if (isCurrent)
                {
                    var badge = new Label("ВЫ ЗДЕСЬ");
                    badge.AddToClassList("map-card-badge");
                    badge.AddToClassList("badge-here");
                    head.Add(badge);
                }

                card.Add(head);

                var title = new Label(entry.Name);
                title.AddToClassList("map-card-title");
                card.Add(title);

                var detail = new Label(entry.Detail);
                detail.AddToClassList("map-card-detail");
                card.Add(detail);

                if (!string.IsNullOrEmpty(entry.Connections))
                {
                    var loop = new Label(entry.Connections);
                    loop.AddToClassList("map-card-loop");
                    card.Add(loop);
                }

                mapSectorsContainer.Add(card);
            }
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

        public void UpdateHealthUI(float current, float max)
        {
            if (healthText != null) healthText.text = $"{Mathf.CeilToInt(current)}%";
            if (healthBarFill != null) healthBarFill.style.width = Length.Percent(Mathf.Clamp01(current / max) * 100f);
        }

        private void OnDestroy()
        { if(controls!=null) controls.Changed-=RefreshMode; if(simulation!=null) simulation.StateChanged-=RefreshSimulation; if(persistence!=null) persistence.StatusChanged-=OnSaveResult; }
    }
}
