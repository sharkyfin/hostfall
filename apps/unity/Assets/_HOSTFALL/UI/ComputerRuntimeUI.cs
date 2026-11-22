using System;
using System.Collections.Generic;
using System.Linq;
using Hostfall.CameraSystem;
using Hostfall.Core;
using Hostfall.Devices;
using Hostfall.Simulation.Computers;
using Hostfall.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UIElements;

namespace Hostfall.UI
{
    // A single physical panel for both the nearby screen and the focused desktop.
    [DefaultExecutionOrder(250)]
    public sealed partial class ComputerRuntimeUI : MonoBehaviour
    {
        private ComputerCampaign campaign;
        private ControlState controls;
        private PlayerCameraRig rig;
        private NearbyTerminalDisplay display;
        private ComputerStation[] stations;
        private ComputerStation current, active;
        private VisualElement view, content, board, taskbar, homeControls, knowledgePanel;
        private Button knowledgeButton;
        private TextField command, knowledgeSearch;
        private Label transcript, score, knowledgeHint;
        private ScrollView output;
        private readonly VisualElement[] cells = new VisualElement[SnakeSession.Width * SnakeSession.Height];
        private ComputerDesktopData desktop;
        private readonly Dictionary<string, VisualElement> windows = new();
        private int openedFrame, transcriptRevision = -1;
        private KeyControl openingKey;
        private Button exitButton;
        private bool CanFocusKeyboard => rig.FocusReady && Time.frameCount > openedFrame &&
            openingKey?.isPressed != true;
        private bool dirty;
        private bool restoreShellScroll;
        private TerminalAmbush ambush;
        public ComputerStation ActiveStation => active;
        public NearbyTerminalDisplay PhysicalDisplay => display;
        public VisualElement HomeControls => homeControls;
        public void BindAmbush(TerminalAmbush threat) => ambush = threat;

        public void Initialize(ComputerCampaign model, ControlState state, PlayerCameraRig cameraRig, UIDocument source)
        {
            campaign = model; controls = state; rig = cameraRig;
            stations = FindObjectsByType<ComputerStation>(FindObjectsSortMode.None);
            view = new VisualElement { name = "computer-world", focusable = true };
            view.AddToClassList("hf2-computer");
            var sheet = Resources.Load<StyleSheet>("ComputerDesktop");
            if (sheet != null) view.styleSheets.Add(sheet);
            display = new NearbyTerminalDisplay(source, view, stations.Select(s => s.Surface).ToArray());
            BuildHomeControls(source.rootVisualElement, sheet);
            foreach (var station in stations) station.Initialize(this);
            campaign.Changed += OnChanged;
            controls.Changed += OnMode;
            view.RegisterCallback<KeyDownEvent>(OnKey, TrickleDown.TrickleDown);
        }
        public bool ResumeComputer()
        {
            if (active == null || controls.Mode != ControlMode.Paused) return false;
            display.Interactive = false;
            controls.SetMode(ControlMode.Computer); rig.Focus(active.Surface);
            openedFrame = Time.frameCount; FocusInput(); return true;
        }
        public void Open(ComputerStation station)
        {
            if (!controls.CanExplore || ambush?.Busy == true || station.Surface == null || !station.Surface.Valid) return;
            display.Interactive = false;
            active = station; openedFrame = Time.frameCount;
            openingKey = Keyboard.current?.eKey;
            ReleaseKeyboardFocus();
            Select(station); controls.SetMode(ControlMode.Computer); rig.Focus(station.Surface); FocusInput();
        }
        public void Close()
        {
            display.Interactive = false;
            ReleaseKeyboardFocus();
            StopFieldRecording();
            PersistDesktop(); active = null; rig.ReleaseFocus();
            controls.SetMode(ControlMode.Exploring);
        }
        private void OnMode(ControlMode mode)
        {
            if (mode == ControlMode.Computer) return;
            ReleaseKeyboardFocus();
            if (mode == ControlMode.Paused && active != null) return;
            StopFieldRecording();
            PersistDesktop(); active = null;
        }
        private void OnChanged() => dirty = true;
        private void Update()
        {
            if (campaign == null) return;
            if (controls.WorldRuns) campaign.AdvanceWorld(Time.deltaTime);
            if (controls.Mode != ControlMode.Computer || active == null) return;
            if (Time.frameCount > openedFrame &&
                (Keyboard.current?.escapeKey.wasPressedThisFrame == true ||
                 Keyboard.current?.pKey.wasPressedThisFrame == true ||
                 Keyboard.current?.tabKey.wasPressedThisFrame == true))
            {
                Close(); return;
            }
            if (!rig.FocusReady) return;
            if (current?.StationId == ComputerCampaign.SnakeStation && desktop?.focusedApp == "snake" &&
                desktop.windows.Any(w => w.app == "snake" && !w.minimized)) campaign.Snake.Advance(Time.deltaTime);
            if (dirty) RefreshLive();
        }
        private void LateUpdate()
        {
            if (display == null) return;
            WorkstationSurface surface = controls.Mode == ControlMode.Computer ? active?.Surface :
                controls.Mode == ControlMode.Exploring ? display.Choose(Camera.main) :
                controls.Mode == ControlMode.Paused && active != null ? active.Surface : null;
            var station = surface == null ? null : stations.FirstOrDefault(s => s.Surface == surface);
            Select(station);
            display.Interactive = controls.Mode == ControlMode.Computer && rig.FocusReady;
            display.Select(surface);
            display.PresentNativeGlass(controls.Mode == ControlMode.Computer && rig.FocusReady, Camera.main);
            RefreshHomeControls();
            if (exitButton != null)
                exitButton.style.display = controls.Mode == ControlMode.Computer && rig.FocusReady ? DisplayStyle.Flex : DisplayStyle.None;
            if (!display.Interactive || !CanFocusKeyboard) return;
            openingKey = null;
            if (command != null && campaign.HomeTab == "terminal")
            {
                var focused = view.focusController?.focusedElement as VisualElement;
                if (focused == null || focused != command && !command.Contains(focused)) command.Focus();
            }
        }
        private void Select(ComputerStation station)
        {
            if (current == station) return;
            StopFieldRecording();
            PersistDesktop(); current = station; desktop = null; windows.Clear();
            view.EnableInClassList("hf2-home", false);
            view.EnableInClassList("hf2-win98", false);
            fieldConsole = null;
            knowledgeSearch = null;
            view.Clear(); refreshRecovery = null; command = null; transcript = null; score = null; board = null;
            if (station == null) return;
            if (station.StationId == ComputerCampaign.HomeStation) BuildHome();
            else if (station.StationId == ComputerCampaign.SnakeStation) BuildDesktop();
            else if (FieldComputer.Find(station.StationId) is FieldComputer computer) BuildFieldConsole(computer);
            else view.Add(new Label("Контроллер недоступен"));
            dirty = true; RefreshLive();
        }
        private Button Button(VisualElement parent, string text, Action action, string name = "")
        {
            var button = new Button(action) { text = text, name = name }; parent.Add(button); return button;
        }
        private Action refreshRecovery;

        private void BuildHomeControls(VisualElement root, StyleSheet sheet)
        {
            homeControls = new VisualElement { name = "home-external-controls", pickingMode = PickingMode.Ignore };
            homeControls.AddToClassList("hf2-home-external");
            homeControls.style.position = Position.Absolute;
            homeControls.style.top = 24;
            homeControls.style.left = 24;
            homeControls.style.right = StyleKeyword.Auto;
            homeControls.style.alignItems = Align.FlexStart;
            homeControls.style.display = DisplayStyle.None;
            if (sheet != null) homeControls.styleSheets.Add(sheet);
            knowledgeButton = Button(homeControls, "Знания", ToggleKnowledge, "home-knowledge");
            knowledgeHint = Text("Откройте «Знания», чтобы прочитать эвакуационную записку.", "hf2-knowledge-hint");
            knowledgeHint.pickingMode = PickingMode.Ignore;
            homeControls.Add(knowledgeHint);
            knowledgePanel = new VisualElement { name = "home-knowledge-panel" };
            knowledgePanel.AddToClassList("hf2-home-knowledge");
            homeControls.Add(knowledgePanel);
            root.Add(homeControls);
            BuildExitButton(root);
        }

        private void BuildExitButton(VisualElement root)
        {
            exitButton = new Button(Close) { name = "terminal-global-exit", text = "✕ Закрыть терминал (Esc / P)" };
            exitButton.style.position = Position.Absolute;
            exitButton.style.top = 18;
            exitButton.style.right = 24;
            exitButton.style.paddingLeft = 14;
            exitButton.style.paddingRight = 14;
            exitButton.style.paddingTop = 8;
            exitButton.style.paddingBottom = 8;
            exitButton.style.fontSize = 13;
            exitButton.style.color = new Color(0.92f, 0.96f, 1f, 1f);
            exitButton.style.backgroundColor = new Color(0.06f, 0.12f, 0.18f, 0.94f);
            exitButton.style.borderTopWidth = 1;
            exitButton.style.borderBottomWidth = 1;
            exitButton.style.borderLeftWidth = 1;
            exitButton.style.borderRightWidth = 1;
            exitButton.style.borderTopColor = new Color(0.28f, 0.6f, 0.85f, 0.8f);
            exitButton.style.borderBottomColor = new Color(0.28f, 0.6f, 0.85f, 0.8f);
            exitButton.style.borderLeftColor = new Color(0.28f, 0.6f, 0.85f, 0.8f);
            exitButton.style.borderRightColor = new Color(0.28f, 0.6f, 0.85f, 0.8f);
            exitButton.style.borderTopLeftRadius = 4;
            exitButton.style.borderTopRightRadius = 4;
            exitButton.style.borderBottomLeftRadius = 4;
            exitButton.style.borderBottomRightRadius = 4;
            exitButton.style.display = DisplayStyle.None;
            root.Add(exitButton);
        }

        private void ToggleKnowledge()
        {
            if (active?.StationId != ComputerCampaign.HomeStation || !rig.FocusReady) return;
            bool showing = campaign.HomeTab == "knowledge";
            campaign.SelectHomeTab(showing ? "terminal" : "knowledge");
            BuildHomeContent();
            RefreshHomeControls();
            FocusInput();
        }

        private void RefreshHomeControls()
        {
            bool showing = controls.Mode == ControlMode.Computer && active?.StationId == ComputerCampaign.HomeStation && rig.FocusReady;
            bool reading = showing && campaign.HomeTab == "knowledge";
            homeControls.style.display = showing ? DisplayStyle.Flex : DisplayStyle.None;
            knowledgePanel.style.display = reading ? DisplayStyle.Flex : DisplayStyle.None;
            bool firstRecord = !campaign.HasRead(ComputerCampaign.EvacuationPath);
            knowledgeHint.style.display = showing && !reading && firstRecord ? DisplayStyle.Flex : DisplayStyle.None;
            if (showing)
                knowledgeButton.text = reading ? "Закрыть знания" :
                    !firstRecord && campaign.HasUnreadArchive ? "Знания · новые записи" : "Знания";
        }

        private void BuildHome()
        {
            view.EnableInClassList("hf2-home", true);
            content = new VisualElement(); content.AddToClassList("hf2-home-content"); view.Add(content);
            if (campaign.HomeTab == "messages") campaign.SelectHomeTab("terminal");
            BuildHomeContent();
        }
        private void BuildHomeContent()
        {
            content.Clear(); knowledgePanel.Clear(); knowledgeSearch = null;
            refreshRecovery = null; command = null; transcript = null; output = null;
            if (campaign.HomeTab != ComputerCampaign.RecoveryTab)
            {
                if (campaign.RecoveryOffered && !campaign.RecoveryAuthorized)
                    Button(content, "Восстановить доступ", () =>
                    {
                        campaign.SelectHomeTab(ComputerCampaign.RecoveryTab);
                        BuildHomeContent();
                    }, "home-recovery");
                output = new ScrollView(); output.AddToClassList("hf2-shell-output"); content.Add(output);
                restoreShellScroll=true;
                output.verticalScroller.valueChanged+=value=>campaign.Home.Terminal.Scroll=Mathf.Max(0,value);
                transcript = new Label(); transcript.AddToClassList("hf2-transcript"); output.Add(transcript);
                command = new TextField { name = "home-command", label = campaign.Shell.Prompt, maxLength = 4096 };
                command.AddToClassList("hf2-shell-input"); command.SetValueWithoutNotify(campaign.Home.Terminal.Draft);
                command.RegisterValueChangedCallback(e => campaign.Shell.SetDraft(e.newValue)); content.Add(command);
                transcriptRevision = -1;
            }
            else
            {
                Button(content, "Вернуться к терминалу", () =>
                {
                    campaign.SelectHomeTab("terminal");
                    BuildHomeContent();
                    FocusInput();
                }, "home-terminal");
                BuildRecoveryAuthorization();
            }
            if (campaign.HomeTab == "knowledge")
            {
                knowledgeSearch = new TextField { name = "home-guide-search", label = "Поиск" }; knowledgePanel.Add(knowledgeSearch);
                var row = new VisualElement(); row.AddToClassList("hf2-guide-row"); knowledgePanel.Add(row);
                var chapters = new ScrollView(); chapters.AddToClassList("hf2-guide-list"); row.Add(chapters);
                var article = new ScrollView(); article.AddToClassList("hf2-guide-text"); row.Add(article);
                void Show(string id)
                {
                    var chapter = HomeGuide.Find(id); if (chapter == null || !campaign.GuideAvailable(id)) return;
                    campaign.SelectGuide(id); article.Clear();
                    article.Add(Text(chapter.Title, "hf2-heading")); article.Add(Text(chapter.Text));
                    article.scrollOffset = Vector2.zero;
                }
                void OpenRecord(string path, string title)
                {
                    if (!campaign.TryReadArchive(path, out string record)) return;
                    article.Clear();
                    article.Add(Text(title, "hf2-heading"));
                    var document = Text(record);
                    document.enableRichText = false;
                    article.Add(document);
                    article.scrollOffset = Vector2.zero;
                    Filter();
                }
                void Filter()
                {
                    string query = knowledgeSearch.value.Trim();
                    chapters.Clear();
                    foreach (var chapter in HomeGuide.Search(query).Where(chapter => campaign.GuideAvailable(chapter.Id)))
                    {
                        string id = chapter.Id;
                        Button(chapters, chapter.Title, () => Show(id), "guide-" + id);
                    }
                    var records = campaign.Messages.Reverse().Where(entry => query.Length == 0 ||
                        entry.text.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        entry.path.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
                    if (records.Length > 0) chapters.Add(Text("Архив убежища", "hf2-archive-label"));
                    foreach (var entry in records)
                    {
                        string title = ArchiveTitle(entry.path);
                        Button(chapters, entry.read ? title : title + "\nНовая запись",
                            () => OpenRecord(entry.path, title), "archive-" + System.IO.Path.GetFileNameWithoutExtension(entry.path));
                    }
                    if (chapters.childCount == 0) chapters.Add(Text("Ничего не найдено"));
                }
                knowledgeSearch.RegisterValueChangedCallback(_ => Filter()); Filter();
                Show(campaign.GuideAvailable(campaign.SelectedGuide) ? campaign.SelectedGuide : "start");
            }
            dirty = true; RefreshLive();
        }
        private static string ArchiveTitle(string path) => path switch
        {
            ComputerCampaign.EvacuationPath => "Эвакуационная записка",
            ComputerCampaign.IncidentPath => "След вентиляции",
            ComputerCampaign.NetworkIncidentPath => "Карантин сети",
            ComputerCampaign.CircuitIncidentPath => "Отключение питания",
            ComputerCampaign.SignalIncidentPath => "Фазовое эхо",
            ComputerCampaign.DetectiveCasePath => "Подтверждённое дело",
            ComputerCampaign.ContainmentLog => "Пломба карантина",
            ComputerCampaign.RecoveryLog => "Отчёт восстановления",
            "/home/operator/archive/payload_transfer.log" => "Грузовой перенос",
            _ => System.IO.Path.GetFileNameWithoutExtension(path)
        };
        private static Label Text(string text, string className = "hf2-prose")
        { var label = new Label(text); label.AddToClassList(className); return label; }
        private void ReleaseKeyboardFocus()
        {
            if (view.focusController?.focusedElement is VisualElement focused && view.Contains(focused))
                focused.Blur();
            knowledgeSearch?.Blur();
        }

        private void FocusInput() => view.schedule.Execute(() =>
        {
            if (controls.Mode != ControlMode.Computer || !CanFocusKeyboard) return;
            if (knowledgeSearch != null) knowledgeSearch.Focus();
            else if (command != null) command.Focus(); else view.Focus();
        }).ExecuteLater(0);

        private void BuildDesktop()
        {
            view.EnableInClassList("hf2-home", false);
            view.EnableInClassList("hf2-win98", true);
            desktop = campaign.Desktop(ComputerCampaign.SnakeStation);
            var icons = new VisualElement(); icons.AddToClassList("hf2-icons"); view.Add(icons);
            Button(icons, "▣\nVENTFLOW", () => OpenApp("snake"), "desktop-snake");
            Button(icons, "▤\nМой компьютер", () => OpenApp("files"), "desktop-files");
            taskbar = new VisualElement(); taskbar.AddToClassList("hf2-taskbar"); view.Add(taskbar);
            foreach (var saved in desktop.windows.ToArray()) BuildWindow(saved);
            if (desktop.focusedApp.Length > 0) windows[desktop.focusedApp].BringToFront();
            RefreshTaskbar();
        }
        private void OpenApp(string app)
        {
            PlaySystemSound(true);
            var state = desktop.windows.FirstOrDefault(w => w.app == app);
            if (state == null)
            {
                state = new DesktopWindowData
                {
                    app = app, x = 200 + desktop.windows.Length * 35, y = 70 + desktop.windows.Length * 30,
                    width = app == "snake" ? 1070 : 950, height = app == "snake" ? 690 : 620
                };
                desktop.windows = desktop.windows.Concat(new[] { state }).ToArray(); BuildWindow(state);
            }
            state.minimized = false; desktop.focusedApp = app;
            windows[app].style.display = DisplayStyle.Flex; windows[app].BringToFront();
            RefreshTaskbar(); PersistDesktop(); view.Focus(); RefreshLive();
        }
        private void BuildWindow(DesktopWindowData state)
        {
            string app = state.app;
            state.x = Mathf.Clamp(state.x,0,NearbyTerminalDisplay.Width-state.width);
            state.y = Mathf.Clamp(state.y,0,Mathf.Max(0,NearbyTerminalDisplay.Height-57-state.height));
            var window = new VisualElement { name = "app-" + app }; window.AddToClassList("hf2-window");
            window.style.left = state.x; window.style.top = state.y; window.style.width = state.width; window.style.height = state.height;
            window.style.display = state.minimized ? DisplayStyle.None : DisplayStyle.Flex;
            view.Add(window); windows[app] = window;
            window.RegisterCallback<PointerDownEvent>(_ => { desktop.focusedApp = app; window.BringToFront(); RefreshTaskbar(); PersistDesktop(); });
            var bar = new VisualElement(); bar.AddToClassList("hf2-titlebar"); window.Add(bar);
            bar.Add(Text(app == "snake" ? "VENTFLOW" : app == "files" ? "Мой компьютер" : "Блокнот", "hf2-window-title"));
            Button(bar, "_", () => HideApp(app, false), "minimize-" + app);
            Button(bar, "×", () => HideApp(app, true), "close-" + app);
            bool dragging = false; Vector2 origin = default, start = default;
            bar.RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.button != 0 || e.target is Button || (e.target as VisualElement)?.GetFirstAncestorOfType<Button>() != null) return;
                dragging = true; origin = e.position; start = new Vector2(state.x, state.y); bar.CapturePointer(e.pointerId); e.StopPropagation();
            });
            bar.RegisterCallback<PointerMoveEvent>(e =>
            {
                if (!dragging) return;
                Vector2 delta = (Vector2)e.position - origin;
                state.x = Mathf.Clamp(start.x + delta.x, 0, NearbyTerminalDisplay.Width - state.width);
                state.y = Mathf.Clamp(start.y + delta.y, 0, Mathf.Max(0,NearbyTerminalDisplay.Height-57-state.height));
                window.style.left = state.x; window.style.top = state.y;
            });
            bar.RegisterCallback<PointerUpEvent>(e => { if (!dragging) return; dragging = false; bar.ReleasePointer(e.pointerId); PersistDesktop(); });
            var body = new VisualElement(); body.AddToClassList("hf2-window-body"); window.Add(body);
            if (app == "snake") BuildSnake(body);
            else if (app == "files")
            {
                Button(body, "readme.txt", () => OpenDocument("readme"));
                Button(body, "maintenance.log", () => OpenDocument("maintenance"));
                AddVentflowArchiveFiles(body);
            }
            else BuildDocument(body);
        }
        private void OpenDocument(string id)
        {
            desktop.document = id;
            if (windows.TryGetValue("document", out var window)) { window.RemoveFromHierarchy(); windows.Remove("document"); desktop.windows = desktop.windows.Where(w => w.app != "document").ToArray(); }
            OpenApp("document");
        }
        private void BuildDocument(VisualElement body)
        {
            var scroll = new ScrollView(); body.Add(scroll);
            var file = VentflowArchive.Find(desktop.document);
            if (file != null)
            {
                scroll.Add(Text(file.Name, "hf2-heading"));
                if (file.Id == VentflowArchive.PhotographId)
                    scroll.Add(new Image { name = "ventflow-archive-photo", scaleMode = ScaleMode.ScaleToFit,
                        image = Resources.Load<Texture2D>(VentflowArchive.PhotographResource),
                        style = { height = 330, flexShrink = 0 } });
                scroll.Add(Text(file.Text));
                return;
            }
            scroll.Add(Text(desktop.document == "maintenance" ?
                "03:04. На внешней ветке пропала обратная связь.\n03:17. Сервисный привод отвечает локально.\nЕсли услышите стук у корпуса, закройте отсечку V-14." :
                ComputerApplications.Find(ComputerCampaign.SnakeStation).Instructions));
        }
        private void AddVentflowArchiveFiles(VisualElement body)
        {
            var hidden = new VisualElement { name = "ventflow-hidden-files" };
            foreach (var entry in VentflowArchive.Entries)
                Button(entry.Hidden ? hidden : body, entry.Name, () => OpenDocument(entry.Id), "ventflow-file-" + entry.Id);
            bool showHidden = VentflowArchive.Find(desktop.document)?.Hidden == true;
            hidden.style.display = showHidden ? DisplayStyle.Flex : DisplayStyle.None;
            var toggle = Button(body, showHidden ? "Скрыть служебные файлы" : "Показать служебные файлы", null, "ventflow-show-hidden");
            toggle.clicked += () =>
            {
                showHidden = !showHidden;
                hidden.style.display = showHidden ? DisplayStyle.Flex : DisplayStyle.None;
                toggle.text = showHidden ? "Скрыть служебные файлы" : "Показать служебные файлы";
            };
            body.Add(hidden);
        }
        private void HideApp(string app, bool close)
        {
            PlaySystemSound(false);
            if (close)
            {
                windows[app].RemoveFromHierarchy(); windows.Remove(app);
                desktop.windows = desktop.windows.Where(w => w.app != app).ToArray();
            }
            else { desktop.windows.First(w => w.app == app).minimized = true; windows[app].style.display = DisplayStyle.None; }
            desktop.focusedApp = desktop.windows.LastOrDefault(w => !w.minimized)?.app ?? "";
            if (desktop.focusedApp.Length > 0) windows[desktop.focusedApp].BringToFront();
            RefreshTaskbar(); PersistDesktop();
        }
        private void RefreshTaskbar()
        {
            taskbar.BringToFront();
            taskbar.Clear();
            foreach (var state in desktop.windows)
            {
                string app = state.app;
                var button = Button(taskbar, app == "files" ? "Мой компьютер" : app == "document" ? "Блокнот" : "VENTFLOW", () => OpenApp(app));
                button.EnableInClassList("hf2-task-active", desktop.focusedApp == app);
            }
        }
        private void BuildSnake(VisualElement parent)
        {
            score = Text("", "hf2-snake-status"); parent.Add(score);
            board = new VisualElement { name = "snake-board" }; board.AddToClassList("hf2-snake-board"); parent.Add(board);
            for (int i = 0; i < cells.Length; i++)
            {
                var cell = new VisualElement(); cell.AddToClassList("hf2-snake-cell"); cells[i] = cell; board.Add(cell);
            }
            var row = new VisualElement(); row.AddToClassList("hf2-game-controls"); parent.Add(row);
            Button(row, "Старт / повтор", () => { campaign.BeginSnake(); view.Focus(); RefreshLive(); }, "snake-start");
            row.Add(Text("Стрелки / WASD · Esc отойти"));
            RefreshLive();
        }
        private void OnKey(KeyDownEvent e)
        {
            if (controls.Mode != ControlMode.Computer || current == null) return;
            if (!CanFocusKeyboard && (e.keyCode == KeyCode.E || e.character is 'e' or 'E'))
            {
                e.StopImmediatePropagation();
                e.PreventDefault();
                return;
            }
            if (!rig.FocusReady) return;
            if (current.StationId == ComputerCampaign.HomeStation && command != null && campaign.HomeTab == "terminal")
            {
                var shell = campaign.Shell;
                if (shell.Paging)
                {
                    if (e.keyCode == KeyCode.Q) shell.ClosePager();
                    else if (e.keyCode == KeyCode.Space || e.keyCode == KeyCode.PageDown) shell.Page(1);
                    else if (e.keyCode == KeyCode.PageUp) shell.Page(-1);
                    else return;
                }
                else if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter) { shell.Execute(command.value); command.SetValueWithoutNotify(""); }
                else if (e.keyCode == KeyCode.UpArrow || e.keyCode == KeyCode.DownArrow) command.value = shell.BrowseHistory(e.keyCode == KeyCode.UpArrow ? -1 : 1, command.value);
                else if (e.keyCode == KeyCode.Tab)
                {
                    var completion = shell.Complete(command.value); command.value = completion.Text;
                    if (completion.Candidates.Length > 1) campaign.Home.Terminal.AddLine(string.Join("   ", completion.Candidates), "muted");
                    command.SelectRange(command.value.Length, command.value.Length);
                }
                else return;
                RefreshLive(); e.StopPropagation(); e.PreventDefault();
            }
            else if (current.StationId == ComputerCampaign.SnakeStation && desktop.focusedApp == "snake")
            {
                SnakeDirection? direction = e.keyCode switch
                {
                    KeyCode.RightArrow or KeyCode.D => SnakeDirection.Right,
                    KeyCode.LeftArrow or KeyCode.A => SnakeDirection.Left,
                    KeyCode.DownArrow or KeyCode.S => SnakeDirection.Down,
                    KeyCode.UpArrow or KeyCode.W => SnakeDirection.Up, _ => null
                };
                if (direction.HasValue) { campaign.Snake.Turn(direction.Value); e.StopPropagation(); e.PreventDefault(); }
            }
        }
        private void RefreshLive()
        {
            dirty = false;
            refreshRecovery?.Invoke();
            fieldConsole?.Refresh();
            if (transcript != null)
            {
                if (campaign.Shell.Paging) transcript.text = campaign.Shell.PagerText;
                else if (transcriptRevision != campaign.Home.Terminal.Revision)
                {
                    transcript.text = string.Join("\n", campaign.Home.Terminal.Lines.Select(l => l.text));
                    var scroll=output;bool restore=restoreShellScroll;float savedScroll=campaign.Home.Terminal.Scroll;restoreShellScroll=false;
                    scroll?.schedule.Execute(() =>
                    {
                        if(scroll.panel==null)return;
                        float maximum=Mathf.Max(0,scroll.verticalScroller.highValue);
                        scroll.verticalScroller.value=restore?Mathf.Clamp(savedScroll,0,maximum):maximum;
                    }).ExecuteLater(20);
                }
                transcriptRevision = campaign.Shell.Paging ? -1 : campaign.Home.Terminal.Revision;
                command.label = campaign.Shell.Prompt;
            }
            if (score != null && windows.ContainsKey("snake"))
            {
                var snake = campaign.Snake;
                score.text = "Этап " + Math.Min(3, snake.Stage + 1) + "/3    Пакеты " + snake.Score + "/12    " +
                    (snake.Phase == SnakePhase.Failed ? "СТОЛКНОВЕНИЕ — повторите этап" : snake.Phase == SnakePhase.Complete ? "ДИАГНОСТИКА ЗАВЕРШЕНА — проход открыт" : snake.Phase == SnakePhase.Ready ? "Нажмите Старт" : "ПОТОК АКТИВЕН");
                var occupied = new HashSet<int>(snake.Body);
                for (int i = 0; i < cells.Length; i++) cells[i].style.backgroundColor =
                    i == snake.Body[0] ? new Color(.8f, 1, .65f) : occupied.Contains(i) ? new Color(.2f, .68f, .35f) :
                    i == snake.Food ? snake.DiagnosticFood ? new Color(1, .77f, .14f) : new Color(.95f, .38f, .2f) :
                    snake.IsObstacle(i) ? new Color(.34f, .38f, .4f) : new Color(.055f, .09f, .07f);
            }
        }
        private void PersistDesktop() { if (desktop != null) campaign.SaveDesktop(desktop); }
        private void OnDestroy()
        {
            if (campaign != null) { PersistDesktop(); campaign.Changed -= OnChanged; }
            if (controls != null) controls.Changed -= OnMode;
            homeControls?.RemoveFromHierarchy();
            display?.Dispose();
        }
    }
}
