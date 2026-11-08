using System;
using System.Collections.Generic;
using System.Linq;
using Hostfall.Core;
using Hostfall.Player;
using Hostfall.Simulation;
using UnityEngine;
using UnityEngine.UIElements;

namespace Hostfall.Field
{
    public sealed class FieldSystemUI : MonoBehaviour
    {
        private FieldSession field;
        private ControlState controls;
        private FacilityInput input;
        private VisualElement overlay, archive, journal;
        private ScrollView articleBody, questBody, results, questList;
        private TextField search;
        private Label progressLabel;
        private readonly Dictionary<string, Button> domains = new();
        private Button journalTab, knowledgeTab;
        private string selectedArticle = "terminal", domain = "Системы";
        public string Domain => domain;
        private ControlMode returnMode;
        public FieldSession Session => field;
        public void Initialize(FieldSession session, ControlState state, FacilityInput bindings, VisualElement root)
        {
            field = session; controls = state; input = bindings; selectedArticle = field.SelectedArticle;
            overlay = root.Q("field-system"); archive = root.Q("archive"); journal = root.Q("journal");
            articleBody = root.Q<ScrollView>("article-body"); questBody = root.Q<ScrollView>("quest-body");
            results = root.Q<ScrollView>("knowledge-results"); questList = root.Q<ScrollView>("quest-list");
            foreach (var pane in new[] { articleBody, questBody, results, questList })
            { pane.horizontalScrollerVisibility = ScrollerVisibility.Hidden; pane.contentContainer.style.width = Length.Percent(100); }
            search = root.Q<TextField>("knowledge-search"); search.RegisterValueChangedCallback(_ => Filter());
            progressLabel=root.Q<Label>("knowledge-progress");
            foreach (string area in new[] { "Системы", "Программирование", "Сети", "Кибербезопасность", "Системное программирование", "Инфраструктура" })
            {
                string value = area; var button = new Button(() => SelectDomain(value)) { text = area=="Программирование"?area+" · "+field.LanguageTitle:area };
                button.AddToClassList("domain-tab"); domains.Add(area, button); root.Q("knowledge-domains").Add(button);
            }
            root.Q<Button>("field-close").clicked += Close;
            journalTab = root.Q<Button>("field-quests"); knowledgeTab = root.Q<Button>("field-knowledge");
            journalTab.clicked += ShowJournal; knowledgeTab.clicked += ShowArchive;
            field.Changed += OnProgress; controls.Changed += OnMode;
            SelectDomain(field.Catalog.Articles[selectedArticle].category); OnMode(controls.Mode);
        }
        private void Update()
        {
            if (field == null || controls.Mode == ControlMode.Terminal || controls.Mode==ControlMode.MainMenu || controls.Mode==ControlMode.Introduction) return;
            var focused = overlay.focusController?.focusedElement as VisualElement;
            if (controls.Mode == ControlMode.Field && (focused is TextField || focused?.GetFirstAncestorOfType<TextField>() != null)) return;
            if (input.Journal.WasPressedThisFrame()) { if (controls.Mode == ControlMode.Field && journal.style.display == DisplayStyle.Flex) Close(); else OpenJournal(); }
            else if (input.Archive.WasPressedThisFrame()) { if (controls.Mode == ControlMode.Field && archive.style.display == DisplayStyle.Flex) Close(); else OpenArchive(); }
        }
        private void Open() { if (controls.Mode != ControlMode.Field) returnMode = controls.Mode; controls.SetMode(ControlMode.Field); }
        public void OpenJournal() { Open(); ShowJournal(); }
        public void OpenArchive() { Open(); ShowArchive(); }
        public void OpenArticle(string id) { Open(); ShowArchive(); ShowArticle(id); }
        public void OpenDocument(string id)
        {
            field.ReadDocument(id); Open(); ShowJournal(); questBody.Clear(); var document = field.Catalog.Documents[id];
            Heading(questBody, document.title); Paragraph(questBody, document.body); AddConcepts(questBody, document.concepts);
        }
        public void Close() => controls.SetMode(returnMode);
        private void OnMode(ControlMode mode) => overlay.style.display = mode == ControlMode.Field ? DisplayStyle.Flex : DisplayStyle.None;
        private void Tabs(bool knowledge)
        {
            archive.style.display = knowledge ? DisplayStyle.Flex : DisplayStyle.None; journal.style.display = knowledge ? DisplayStyle.None : DisplayStyle.Flex;
            journalTab.EnableInClassList("tab-selected", !knowledge); knowledgeTab.EnableInClassList("tab-selected", knowledge);
        }
        private void ShowArchive() { Tabs(true); ShowArticle(selectedArticle); Filter(); }
        private void ShowJournal()
        {
            Tabs(false); questList.Clear();
            int stage = Mathf.Min(field.Simulation.Mission.CompletedObjectives, 2);
            foreach (var quest in field.Catalog.Data.quests.Where(q => q.stage <= stage).OrderByDescending(q => q.stage))
            {
                var button = new Button(() => ShowQuest(quest.id)) { name = "quest-" + quest.id, text = quest.title + "\n" + field.QuestStatus(quest) };
                button.AddToClassList("journal-entry"); questList.Add(button);
            }
            ShowQuest(field.Catalog.Data.quests.First(q => q.stage == stage).id);
        }
        private void ShowQuest(string id)
        {
            var quest = field.Catalog.Data.quests.First(q => q.id == id); questBody.Clear();
            foreach (var button in questList.Query<Button>().ToList()) button.EnableInClassList("quest-selected", button.name == "quest-" + id);
            Heading(questBody, quest.title); Paragraph(questBody, quest.summary, "article-summary"); Paragraph(questBody, quest.description);
            if (field.Simulation.Mission.CompletedObjectives > quest.stage) Section(questBody, "Что изменилось", quest.outcome);
            else
            {
                var objective = new VisualElement(); objective.AddToClassList("quest-objective"); Paragraph(objective, quest.objective); questBody.Add(objective);
            }
            AddConcepts(questBody, quest.concepts);
            if (field.FoundDocuments.Contains(quest.document))
                questBody.Add(new Button(() => OpenDocument(quest.document)) { text = field.Catalog.Documents[quest.document].title });
            questBody.scrollOffset = Vector2.zero;
        }
        public void SelectDomain(string value)
        {
            if (!domains.ContainsKey(value)) return; domain = value; search.SetValueWithoutNotify("");
            foreach (var pair in domains) pair.Value.EnableInClassList("domain-selected", pair.Key == value);
            Filter();
            var article=field.Search("",domain).OrderBy(a=>a.order).FirstOrDefault();
            if (article!=null) ShowArticle(article.id);
            else
            {
                articleBody.Clear(); Heading(articleBody,value);
                Paragraph(articleBody,"Новые темы появятся по мере расследования. Уже открытые материалы остаются с вами.","article-summary");
            }
        }
        private void Filter()
        {
            if(field==null) return;
            var all=field.Catalog.Articles.Values.Where(a=>a.category==domain&&field.MatchesTrack(a)).ToArray();
            var matches=field.Search(search.value,domain).OrderBy(a=>a.order).ToArray(); results.Clear();
            results.style.display=DisplayStyle.Flex;
            int available=all.Count(a=>field.IsAvailable(a.id));
            progressLabel.text=available+" из "+all.Length+" тем доступны";
            foreach(var article in matches)
            {
                string id=article.id; var button=new Button(()=>ShowArticle(id)) { name="article-"+id };
                button.AddToClassList("library-entry"); button.EnableInClassList("library-selected",id==selectedArticle);
                button.Add(new Label(article.title) { pickingMode=PickingMode.Ignore });
                var status=new Label(field.State(id)>=KnowledgeState.Practiced?"Проверено действием":field.State(id)>=KnowledgeState.Studied?"Прочитано":"Новое") { pickingMode=PickingMode.Ignore };
                status.AddToClassList("library-state"); button.Add(status); results.Add(button);
            }
            if(matches.Length==0) Paragraph(results,string.IsNullOrWhiteSpace(search.value)?"Пока нет открытых тем":"Среди открытых тем ничего не найдено");

        }
        private void ShowArticle(string id)
        {
            if (!field.Catalog.Articles.TryGetValue(id,out var a)||!field.IsAvailable(id)) return;
            selectedArticle=id; field.SelectedArticle=id; domain=a.category;
            foreach(var pair in domains) pair.Value.EnableInClassList("domain-selected",pair.Key==domain);
            field.Discover(id); Filter(); articleBody.Clear();
            Heading(articleBody,a.title); Paragraph(articleBody,a.summary,"article-summary"); Paragraph(articleBody,a.body);
            if(a.quickCommands?.Length>0)
            {
                foreach(var tip in a.quickCommands)
                {
                    var card=new VisualElement(); card.AddToClassList("command-tip");
                    Paragraph(card,tip.command,"command-name"); Paragraph(card,tip.description); Paragraph(card,tip.example,"code-example"); articleBody.Add(card);
                }
            }
            else
            {
                var example=a.examples.First();
                string heading=example.language=="nlsh"?"В терминале станции":example.language=="cpp"?"C++ · пример":example.language=="python"?"Python · пример":example.language=="shell"?"Linux · справочный пример":"Пример";
                Paragraph(articleBody,heading,"section-heading"); Paragraph(articleBody,example.code,"code-example"); Paragraph(articleBody,example.expected);
                if(example.language=="shell") Paragraph(articleBody,"Это команда настоящей Linux-системы. В терминале текущего участка она недоступна.","article-meta");
            }
            if(!string.IsNullOrWhiteSpace(a.mistakes)) Section(articleBody,"Частая ошибка",a.mistakes);
            var study=new Button(()=> { field.Study(id); ShowArticle(id); }) { name="article-understood", text=field.State(id)>=KnowledgeState.Studied?"Прочитано":"Понятно" };
            study.AddToClassList("hint-button"); study.SetEnabled(field.State(id)<KnowledgeState.Studied); articleBody.Add(study);
            var links=new VisualElement(); links.AddToClassList("chip-row");
            foreach(string next in a.relatedConcepts.Concat(a.prerequisites).Distinct().Where(field.IsAvailable).Take(4))
            { string target=next; var button=new Button(()=>ShowArticle(target)) { text=field.Catalog.Articles[next].title }; button.AddToClassList("link-chip"); links.Add(button); }
            articleBody.Add(links); articleBody.scrollOffset=Vector2.zero;
        }
        private void OnProgress() { if(controls?.Mode==ControlMode.Field) Filter(); }
        private static void Heading(VisualElement parent, string text) => Paragraph(parent, text, "article-title");
        private static void Section(VisualElement parent, string title, string body)
        { Paragraph(parent, title, "section-heading"); Paragraph(parent, body); }
        private static void Paragraph(VisualElement parent, string text, string style = "")
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            var label = new Label(text) { enableRichText = false, selection = { isSelectable = true } };
            label.AddToClassList("article-text"); label.AddToClassList("reading-copy"); if (style.Length > 0) label.AddToClassList(style); parent.Add(label);
        }
        private void AddConcepts(VisualElement parent, string[] concepts)
        {
            var row = new VisualElement(); row.AddToClassList("chip-row"); parent.Add(row);
            foreach (string id in concepts.Where(field.IsAvailable))
            { string target = id; var button = new Button(() => OpenArticle(target)) { text = field.Catalog.Articles[id].title }; button.AddToClassList("link-chip"); row.Add(button); }
        }
        private void OnDestroy() { if (field != null) field.Changed -= OnProgress; if (controls != null) controls.Changed -= OnMode; }
    }
}
