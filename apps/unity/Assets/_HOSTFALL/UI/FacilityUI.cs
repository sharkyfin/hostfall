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
        private VisualElement terminal, pause, hud, menuMain, menuPage, front, introduction;
        private GamePreferences preferences;
        public GamePreferences Preferences => preferences;
        public bool HasSession => entered || persistence.Loaded;
        private Label objective, prompt, host, inputPrompt, taskHeading, menuTitle, pageTitle, menuError;
        private Button resume, restart;
        private Label typedCommand, routineStatus;
        private DeviceId activeTerminal;
        private string interactionText="", actionNotice="";
        private float noticeUntil;
        private ScrollView output, pageBody;
        private int renderedRevision;
        private string page = "", confirmSlot = "";
        private bool entered, confirmingNewGame;
        public void Initialize(FacilitySimulation model, ControlState state, FacilityInput inputBindings, FieldSession progress, FieldSystemUI fieldView, WorldSaveController saveController)
        {
            simulation=model; controls=state; bindings=inputBindings; field=progress; fieldUI=fieldView; persistence=saveController;
            var root=GetComponent<UIDocument>().rootVisualElement;
            terminal=root.Q("terminal"); pause=root.Q("pause"); hud=root.Q("hud"); front=root.Q("main-menu"); introduction=root.Q("introduction");
            preferences=new GamePreferences(UnityEngine.Object.FindAnyObjectByType<PlayerMotor>());
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
            activeTerminal=id; session=new TerminalSession(simulation,id); memory=simulation[id].Terminal;
            simulation.Terminals.Begin(id);
            output.Clear(); foreach(var line in memory.Lines) RenderLine(line.text,line.style);
            renderedRevision=memory.Revision;
            RefreshPrompt(); controls.SetMode(ControlMode.Terminal);
            rootFocus();
        }
        private void rootFocus()=>GetComponent<UIDocument>().rootVisualElement.Q<Button>("close-terminal").Focus();
        private void CloseTerminal() { session=null; memory=null; controls.SetMode(ControlMode.Exploring); }
        public void BeginSession()
        {
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
            RefreshActionNotice();
            if(controls.Mode==ControlMode.Terminal && memory!=null)
            {
                simulation.Terminals.Tick(activeTerminal,Time.unscaledDeltaTime);
                RefreshPrompt();
            }
            if(bindings.Load.WasPressedThisFrame() && controls.Mode==ControlMode.MainMenu) { OpenPage("load"); return; }
            if(!bindings.Escape.WasPressedThisFrame()) return;
            if(controls.Mode==ControlMode.Field) fieldUI.Close();
            else if(controls.Mode==ControlMode.Terminal) CloseTerminal();
            else if(page.Length>0) Back();
            else if(controls.Mode==ControlMode.Paused) BeginSession();
            else if(controls.Mode==ControlMode.Exploring) controls.SetMode(ControlMode.Paused);
        }
        private void RefreshMode(ControlMode mode)
        {
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
                if(controls.Mode!=expectedMode||page.Length>0) return;
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
            var label=new Label(text) { enableRichText=false, selection={isSelectable=true} }; label.AddToClassList(style); output.Add(label);
        }
        private void OnDestroy()
        { if(controls!=null) controls.Changed-=RefreshMode; if(simulation!=null) simulation.StateChanged-=RefreshSimulation; if(persistence!=null) persistence.StatusChanged-=OnSaveResult; }
    }
}
