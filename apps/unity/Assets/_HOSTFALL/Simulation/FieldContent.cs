using System;
using System.Collections.Generic;
using System.Linq;

namespace Hostfall.Simulation
{
    [Serializable] public sealed class ArticleExample
    {
        public string language, code, expected, verification;
    }
    [Serializable] public sealed class CommandTip { public string command, description, example; }
    [Serializable] public sealed class KnowledgeArticle
    {
        public string track;
        public int unlockStage, order;
        public string id, title, category, summary, body, purpose, realExample, gameApplications, exercise, mistakes, deeperStudy;
        public string verifiedAt, technologyVersion, reviewStatus;
        public string[] relatedConcepts, prerequisites, officialSources, commands;
        public ArticleExample[] examples;
        public CommandTip[] quickCommands;
        public float x, y;
    }
    [Serializable] public sealed class KnowledgeLink { public string from, to, relation; }
    [Serializable] public sealed class QuestDefinition
    {
        public string id, title, summary, description, stakes, known, unknown, objective, optional, room, document, outcome;
        public string[] concepts, hints, solutions, consequences;
        public int stage;
    }
    [Serializable] public sealed class FieldDocumentDefinition { public string id, title, body; public string[] concepts; }
    [Serializable] public sealed class FieldContentData
    {
        public int version;
        public KnowledgeArticle[] articles;
        public KnowledgeLink[] links;
        public QuestDefinition[] quests;
        public FieldDocumentDefinition[] documents;
    }
    public sealed class FieldCatalog
    {
        public FieldContentData Data { get; }
        public IReadOnlyDictionary<string, KnowledgeArticle> Articles { get; }
        public IReadOnlyDictionary<string, FieldDocumentDefinition> Documents { get; }
        private static readonly HashSet<string> Relations = new() { "Requires", "RelatedTo", "UsedBy", "PartOf", "Implements", "ContrastsWith" };
        private static readonly HashSet<string> SourceHosts = new() { "docs.python.org", "isocpp.github.io", "eel.is", "cheatsheetseries.owasp.org", "docs.kernel.org", "csrc.nist.gov", "developer.mozilla.org", "learn.microsoft.com", "man7.org", "xlinux.nist.gov", "www.rfc-editor.org", "docs.docker.com", "kubernetes.io", "www.freedesktop.org" };
        public FieldCatalog(FieldContentData data)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
            Require(data.version == 1, "Unsupported field content version");
            Articles = Index(data.articles, a => a.id); Documents = Index(data.documents, d => d.id);
            var quests = Index(data.quests, q => q.id);
            foreach (var a in Articles.Values)
            {
                Require(string.IsNullOrEmpty(a.track)||a.track=="cpp"||a.track=="python", "Unknown programming track " + a.id);
                Require(a.unlockStage>=0&&a.unlockStage<=3, "Unreachable world milestone " + a.id);
                Require(a.relatedConcepts != null && a.prerequisites != null && a.commands != null, "Incomplete article links " + a.id);
                Require(!string.IsNullOrWhiteSpace(a.title) && !string.IsNullOrWhiteSpace(a.body) && !string.IsNullOrWhiteSpace(a.summary), "Incomplete article " + a.id);
                Require(a.examples != null && a.examples.Length > 0 && a.officialSources != null && a.officialSources.Length > 0, "Article lacks examples/sources " + a.id);
                foreach (string id in a.relatedConcepts.Concat(a.prerequisites)) Require(Articles.ContainsKey(id), "Broken knowledge reference " + id);
                foreach (string url in a.officialSources) Require(Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == "https" && SourceHosts.Contains(uri.Host), "Unapproved source URL " + url);
            }
            Require(data.links != null, "Missing graph links");
            foreach (var link in data.links) Require(Articles.ContainsKey(link.from) && Articles.ContainsKey(link.to) && link.from != link.to && Relations.Contains(link.relation), "Invalid graph link");
            Require(data.quests.Length == 3 && data.quests.Select(q => q.stage).OrderBy(s => s).SequenceEqual(new[] { 0, 1, 2 }), "First story needs three distinct stages");
            foreach (var q in quests.Values)
            {
                Require(!string.IsNullOrWhiteSpace(q.description) && !string.IsNullOrWhiteSpace(q.objective), "Incomplete quest " + q.id);
                Require(Documents.ContainsKey(q.document), "Missing quest document " + q.document);
                foreach (string id in q.concepts) Require(Articles.ContainsKey(id), "Broken quest knowledge " + id);
            }
            foreach (var doc in Documents.Values) foreach (string id in doc.concepts) Require(Articles.ContainsKey(id), "Broken document knowledge " + id);
            VisitPrerequisites();
        }
        private void VisitPrerequisites()
        {
            var seen = new HashSet<string>(); var active = new HashSet<string>();
            void Visit(string id)
            {
                if (seen.Contains(id)) return;
                Require(active.Add(id), "Prerequisite cycle at " + id);
                foreach (string next in Articles[id].prerequisites) Visit(next);
                active.Remove(id); seen.Add(id);
            }
            foreach (string id in Articles.Keys) Visit(id);
        }
        private static Dictionary<string, T> Index<T>(T[] entries, Func<T, string> id)
        {
            Require(entries != null, "Missing content array"); var result = new Dictionary<string, T>(StringComparer.Ordinal);
            foreach (T item in entries) { string key = id(item); Require(!string.IsNullOrWhiteSpace(key) && !result.ContainsKey(key), "Duplicate/empty content ID"); result.Add(key, item); }
            return result;
        }
        private static void Require(bool condition, string reason) { if (!condition) throw new ArgumentException(reason); }
    }
    public enum KnowledgeState { Unknown, Discovered, Studied, Practiced, Applied }
    [Serializable] public sealed class KnowledgeProgressEntry { public string id; public KnowledgeState state; }
    [Serializable] public sealed class ArchiveSnapshot { public int version = 1; public KnowledgeProgressEntry[] entries; }
    public sealed class FieldSession : IDisposable
    {
        public FieldCatalog Catalog { get; }
        public FacilitySimulation Simulation { get; }
        private readonly Dictionary<string, KnowledgeState> states = new();
        private readonly HashSet<string> documents = new();
        private readonly Dictionary<string, int> hintLevels = new();
        public int HintLevel(string questId) => hintLevels.TryGetValue(questId, out int level) ? level : 0;
        public void RequestHint(string questId)
        {
            var quest = Catalog.Data.quests.FirstOrDefault(q => q.id == questId);
            if (quest == null || quest.stage > Simulation.Mission.CompletedObjectives) return;
            hintLevels[questId] = Math.Min(3, HintLevel(questId) + 1); Changed?.Invoke();
        }
        public event Action Changed;
        public IReadOnlyCollection<string> FoundDocuments => documents;
        public string SelectedArticle { get; set; } = "terminal";
        public string LastErrorConcept { get; private set; } = "terminal";
        public string ProgrammingLanguage { get; private set; }
        public string LanguageTitle => ProgrammingLanguage=="cpp"?"C++":"Python";
        public bool IntroductionSeen { get; private set; }
        public void AcknowledgeIntroduction() { IntroductionSeen=true; Changed?.Invoke(); }
        public FieldSession(FieldCatalog catalog, FacilitySimulation simulation, string language="python")
        {
            ProgrammingLanguage=NormalizeLanguage(language);
            Catalog = catalog; Simulation = simulation;
            simulation.DocumentObserved += ReadDocument;
            simulation.CommandObserved += OnCommand; simulation.StateChanged += OnWorldChanged;
            Discover("terminal"); Discover("process"); Discover("filesystem");
        }
        public KnowledgeState State(string id) => states.TryGetValue(id, out var state) ? state : KnowledgeState.Unknown;
        public void Discover(string id) => Advance(id, KnowledgeState.Discovered);
        public void Study(string id) { if(IsAvailable(id)) Advance(id, KnowledgeState.Studied); }
        public static string NormalizeLanguage(string value)
        {
            if(string.IsNullOrEmpty(value)) return "python"; // Existing world-v2 slots predate the choice.
            SaveValidation.Require(value=="cpp"||value=="python", "Unknown programming language"); return value;
        }
        public bool MatchesTrack(KnowledgeArticle article) => string.IsNullOrEmpty(article.track)||article.track==ProgrammingLanguage;
        public bool IsAvailable(string id)
        {
            if(!Catalog.Articles.TryGetValue(id,out var a)||!MatchesTrack(a)) return false;
            if(State(id)>=KnowledgeState.Discovered) return true;
            return Simulation.Mission.CompletedObjectives>=a.unlockStage && a.prerequisites.All(p=>State(p)>=KnowledgeState.Studied);
        }
        public string UnlockReason(KnowledgeArticle a)
        {
            if(!MatchesTrack(a)) return "Другая языковая ветка";
            if(Simulation.Mission.CompletedObjectives<a.unlockStage)
                return a.unlockStage==1?"После проверки процессов WS-07":a.unlockStage==2?"После изоляции источника WS-07":"После независимой проверки CORE-01";
            var missing=a.prerequisites.Where(p=>State(p)<KnowledgeState.Studied).Select(p=>Catalog.Articles[p].title);
            return "Сначала прочитайте: "+string.Join(", ",missing);
        }
        private void Advance(string id, KnowledgeState state)
        {
            if (!Catalog.Articles.TryGetValue(id,out var article)||!MatchesTrack(article)) return;
            if (State(id) >= state) return;
            states[id] = state; Changed?.Invoke();
        }
        public void ReadDocument(string id)
        {
            if (!Catalog.Documents.TryGetValue(id, out var doc)) return;
            documents.Add(id); foreach (string concept in doc.concepts) Discover(concept); Changed?.Invoke();
        }
        public string QuestStatus(QuestDefinition quest) => Simulation.Mission.CompletedObjectives > quest.stage ? "Завершено" : Simulation.Mission.CompletedObjectives == quest.stage ? "В работе" : "Ещё не начато";
        public IEnumerable<KnowledgeArticle> Search(string text, string category)
        {
            text = text?.Trim() ?? "";
            return Catalog.Articles.Values.Where(a => IsAvailable(a.id) && (string.IsNullOrEmpty(category) || a.category == category) &&
                (a.title.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0 || a.summary.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0 || a.commands.Any(c => c.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)));
        }
        private void OnCommand(CommandObservation command)
        {
            if (!command.Success) { LastErrorConcept = command.Concept; Discover(command.Concept); return; }
            Advance(command.Concept, KnowledgeState.Practiced);
        }
        private void OnWorldChanged()
        {
            if (Simulation.SourceIsolated) Advance("process", KnowledgeState.Applied);
            if (Simulation.Terminals.IsComplete(DeviceId.Core01) || Simulation.Mission.IsComplete) Advance("server", KnowledgeState.Applied);
            Changed?.Invoke();
        }
        public FieldProgressData CaptureProgress() => new()
        {
            programmingLanguage=ProgrammingLanguage, introductionSeen=IntroductionSeen,introductionVersion=2,
            knowledge = Capture(), documents = documents.OrderBy(id => id, StringComparer.Ordinal).ToArray(), selectedArticle = SelectedArticle,
            hints = hintLevels.OrderBy(h => h.Key, StringComparer.Ordinal).Select(h => new QuestHintData { id = h.Key, level = h.Value }).ToArray()
        };
        public void RestoreProgress(FieldProgressData data)
        {
            SaveValidation.Require(data != null && data.version == 1 && data.knowledge?.version == 1 && data.knowledge.entries != null && data.documents != null && data.hints != null, "Unsupported field progress");
            foreach (var entry in data.knowledge.entries)
                SaveValidation.Require(entry != null && !string.IsNullOrEmpty(entry.id) && Enum.IsDefined(typeof(KnowledgeState), entry.state), "Invalid knowledge state");
            foreach (var hint in data.hints)
                SaveValidation.Require(hint != null && Catalog.Data.quests.Any(q => q.id == hint.id) && hint.level >= 0 && hint.level <= 3, "Invalid quest hint");
            SaveValidation.Require(data.documents.All(id => id != null && Catalog.Documents.ContainsKey(id)), "Unknown found document");
            SaveValidation.Require(data.introductionVersion>=0&&data.introductionVersion<=2,"Unknown introduction version");
            ProgrammingLanguage=NormalizeLanguage(data.programmingLanguage); IntroductionSeen=data.introductionSeen&&(!Simulation.AutomatedRoute||data.introductionVersion>=2);
            states.Clear(); documents.Clear(); hintLevels.Clear();
            Restore(data.knowledge);
            foreach (string id in data.documents) documents.Add(id);
            foreach (var hint in data.hints) hintLevels[hint.id] = hint.level;
            SelectedArticle = IsAvailable(data.selectedArticle ?? "") ? data.selectedArticle : "terminal";
            Changed?.Invoke();
        }
        public ArchiveSnapshot Capture() => new() { entries = states.Select(e => new KnowledgeProgressEntry { id = e.Key, state = e.Value }).ToArray() };
        public void Restore(ArchiveSnapshot snapshot)
        {
            if (snapshot == null || snapshot.version != 1 || snapshot.entries == null) throw new ArgumentException("Unsupported archive progress version");
            foreach (var entry in snapshot.entries)
            {
                if (entry == null || !Enum.IsDefined(typeof(KnowledgeState), entry.state)) throw new ArgumentException("Invalid knowledge progress state");
            }
            foreach (var entry in snapshot.entries) Advance(entry.id, entry.state);
        }
        public void Dispose() { Simulation.DocumentObserved -= ReadDocument; Simulation.CommandObserved -= OnCommand; Simulation.StateChanged -= OnWorldChanged; }
    }
    public readonly struct CommandObservation
    {
        public string Concept { get; }
        public bool Success { get; }
        public CommandObservation(string concept, bool success) { Concept = concept; Success = success; }
    }
}
