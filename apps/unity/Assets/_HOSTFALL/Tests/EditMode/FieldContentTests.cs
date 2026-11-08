using System;
using System.Linq;
using Hostfall.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace Hostfall.Tests
{
    public sealed class FieldContentTests
    {
        private static FieldContentData Load() => JsonUtility.FromJson<FieldContentData>(Resources.Load<TextAsset>("FieldContent").text);
        [Test]
        public void AuthoredCatalogHasNinetySixUsefulLessonsAndNoJavaScript()
        {
            var catalog = new FieldCatalog(Load());
            Assert.That(catalog.Articles.Count, Is.EqualTo(96));
            Assert.That(catalog.Data.quests.Length, Is.EqualTo(3));
            Assert.That(catalog.Data.links.Any(l => l.relation != "Requires"), Is.True);
            Assert.That(catalog.Articles.Values.Count(a=>a.track=="cpp"),Is.EqualTo(24));
            Assert.That(catalog.Articles.Values.Count(a=>a.track=="python"),Is.EqualTo(24));
            Assert.That(catalog.Articles.Values.Count(a=>a.category=="Кибербезопасность"),Is.EqualTo(12));
            Assert.That(catalog.Data.articles.SelectMany(a=>a.examples).Any(e=>e.language=="javascript"),Is.False);
            foreach (var article in catalog.Articles.Values)
            {
                Assert.That(article.officialSources.All(s => s.StartsWith("https://", StringComparison.Ordinal)), Is.True);
                Assert.That(article.body.Length,Is.GreaterThan(150),article.id);
                Assert.That(article.exercise,Is.Not.Empty,article.id);
                Assert.That(article.mistakes,Is.Not.Empty,article.id);
                Assert.That(article.examples.All(e=>!string.IsNullOrWhiteSpace(e.code)&&!string.IsNullOrWhiteSpace(e.expected)),Is.True,article.id);
            }
        }
        [Test]
        public void ReferenceLessonsAndQuestDirectionsDoNotDiscloseIncidentAnswers()
        {
            var content=Load();
            var answers=new System.Text.RegularExpressions.Regex(@"cache-sync|\bWS-?07\b|\bCORE-?01\b|\bEDGE-?01\b|ticket\.txt|super-node|\b431\b",System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            foreach(var a in content.articles)
            {
                var visible=a.title+"\n"+a.summary+"\n"+a.body+"\n"+a.mistakes+"\n"+
                    string.Join("\n",a.examples.Select(e=>e.code+"\n"+e.expected))+"\n"+
                    string.Join("\n",a.quickCommands.Select(c=>c.command+"\n"+c.description+"\n"+c.example));
                Assert.That(answers.IsMatch(visible),Is.False,a.id);
            }
            foreach(var quest in content.quests)
            {
                Assert.That(quest.description+quest.objective+quest.summary+quest.optional,
                    Does.Not.Contain("cache-sync").And.Not.Contain("systemctl").And.Not.Contain("kill ").And.Not.Contain("ssh "),quest.id);
                Assert.That(quest.hints,Is.Empty,quest.id);
            }
        }
        [Test]
        public void LegacyIntroductionIsReplacedOnceForTheNewActionControls()
        {
            var catalog=new FieldCatalog(Load()); var world=new FacilitySimulation(MissionDefinition.ActionRoute);
            using var field=new FieldSession(catalog,world); field.AcknowledgeIntroduction();
            var old=field.CaptureProgress(); old.introductionVersion=0;
            field.RestoreProgress(old); Assert.That(field.IntroductionSeen,Is.False);
            field.AcknowledgeIntroduction(); var current=field.CaptureProgress();
            field.RestoreProgress(current); Assert.That(field.IntroductionSeen,Is.True);
        }
        [Test]
        public void CatalogRejectsMissingLinksDuplicateIdsAndPrerequisiteCycles()
        {
            var missing = Load(); missing.links[0].to = "not-an-article";
            Assert.Throws<ArgumentException>(() => new FieldCatalog(missing));
            var duplicate = Load(); duplicate.articles[1].id = duplicate.articles[0].id;
            Assert.Throws<ArgumentException>(() => new FieldCatalog(duplicate));
            var cycle = Load(); cycle.articles[0].prerequisites = new[] { cycle.articles[0].id };
            Assert.Throws<ArgumentException>(() => new FieldCatalog(cycle));
        }
        [Test]
        public void CommandsDiscoverPracticeAndApplyKnowledgeWithoutAutoSolving()
        {
            var world = new FacilitySimulation(MissionDefinition.NightShift); using var field = new FieldSession(new FieldCatalog(Load()), world);
            var terminal = new TerminalSession(world, DeviceId.Ws07);
            terminal.Execute("systemctl invalid");
            Assert.That(field.LastErrorConcept, Is.EqualTo("process"));
            Assert.That(field.State("process"), Is.EqualTo(KnowledgeState.Discovered));
            Assert.That(world.Mission.CompletedObjectives, Is.Zero);
            terminal.Execute("ps"); Assert.That(field.State("process"), Is.EqualTo(KnowledgeState.Practiced));
            field.Study("process"); Assert.That(field.State("process"), Is.EqualTo(KnowledgeState.Practiced));
            Assert.That(world.SourceIsolated, Is.False);
            terminal.Execute("systemctl disable cache-sync"); terminal.Execute("systemctl stop cache-sync");
            Assert.That(field.State("process"), Is.EqualTo(KnowledgeState.Applied));
            Assert.That(field.QuestStatus(field.Catalog.Data.quests[1]), Is.EqualTo("Завершено"));
            Assert.That(field.QuestStatus(field.Catalog.Data.quests[2]), Is.EqualTo("В работе"));
        }
        [Test]
        public void ArchiveProgressContainsIdsAndStatesAndRejectsUnsupportedVersions()
        {
            var catalog = new FieldCatalog(Load()); using var first = new FieldSession(catalog, new FacilitySimulation(MissionDefinition.NightShift));
            first.Study("terminal"); var snapshot = first.Capture();
            using var second = new FieldSession(catalog, new FacilitySimulation(MissionDefinition.NightShift)); second.Restore(snapshot);
            Assert.That(second.State("terminal"), Is.EqualTo(KnowledgeState.Studied));
            snapshot.version = 2; Assert.Throws<ArgumentException>(() => second.Restore(snapshot));
            Assert.That(second.Simulation.Mission.CompletedObjectives, Is.Zero, "Archive memory must not advance the new world's story.");
        }
        [TestCase("cpp", "python")]
        [TestCase("python", "cpp")]
        public void LanguageBranchesUnlockByReadingAndWorldMilestonesAndRoundTrip(string track,string other)
        {
            var catalog=new FieldCatalog(Load()); var world=new FacilitySimulation(MissionDefinition.NightShift); using var field=new FieldSession(catalog,world,track);
            Assert.That(field.IsAvailable(track+".start"),Is.True);
            Assert.That(field.IsAvailable(track+".variables"),Is.False);
            field.Study(track+".start"); Assert.That(field.IsAvailable(track+".variables"),Is.True);
            field.Study(other+".start"); field.Discover(other+".start");
            Assert.That(field.State(other+".start"),Is.EqualTo(KnowledgeState.Unknown));
            Assert.That(field.Search("","Программирование").All(a=>a.track==track),Is.True);
            foreach(string id in new[] { "variables","types","operators","conditions" }) field.Study(track+"."+id);
            Assert.That(field.IsAvailable(track+".for"),Is.False,"Reading alone cannot bypass a world milestone");
            new TerminalSession(world,DeviceId.Ws07).Execute("ps"); Assert.That(field.IsAvailable(track+".for"),Is.True);
            field.AcknowledgeIntroduction(); var saved=field.CaptureProgress();
            using var restored=new FieldSession(catalog,world,other); restored.RestoreProgress(saved);
            Assert.That(restored.ProgrammingLanguage,Is.EqualTo(track)); Assert.That(restored.IntroductionSeen,Is.True);
            Assert.That(restored.State(track+".start"),Is.EqualTo(KnowledgeState.Studied));
            Assert.That(restored.IsAvailable(other+".start"),Is.False);
            saved.programmingLanguage="javascript"; Assert.Throws<ArgumentException>(()=>restored.RestoreProgress(saved));
        }
        [Test]
        public void EveryLessonIsReachableInItsOwnCampaignAndLegacyProgressMigrates()
        {
            var catalog=new FieldCatalog(Load());
            foreach(string track in new[] { "cpp","python" })
            {
                var world=new FacilitySimulation(MissionDefinition.NightShift); using var field=new FieldSession(catalog,world,track);
                var shell=new TerminalSession(world,DeviceId.Ws07); shell.Execute("ps"); shell.Execute("systemctl disable cache-sync"); shell.Execute("systemctl stop cache-sync"); shell.Execute("ssh core01"); shell.Execute("ps");
                Assert.That(world.Mission.IsComplete,Is.True);
                for(int pass=0;pass<100;pass++) foreach(var a in catalog.Articles.Values.Where(a=>field.IsAvailable(a.id)).ToArray()) field.Study(a.id);
                Assert.That(catalog.Articles.Values.Count(a=>field.IsAvailable(a.id)),Is.EqualTo(72));
                Assert.That(field.Search("", "Программирование").Count(),Is.EqualTo(24));
            }
            using var legacy=new FieldSession(catalog,new FacilitySimulation(MissionDefinition.NightShift)); var data=legacy.CaptureProgress();
            data.programmingLanguage=null; data.selectedArticle="algorithm";
            data.knowledge.entries=data.knowledge.entries.Concat(new[] { new KnowledgeProgressEntry { id="algorithm",state=KnowledgeState.Studied } }).ToArray();
            using var migrated=new FieldSession(catalog,new FacilitySimulation(MissionDefinition.NightShift),"cpp"); migrated.RestoreProgress(data);
            Assert.That(migrated.ProgrammingLanguage,Is.EqualTo("python")); Assert.That(migrated.SelectedArticle,Is.EqualTo("terminal"));
            Assert.That(migrated.IntroductionSeen,Is.False);
        }
        [Test]
        public void EveryAuthoredNlshExampleRunsAgainstTheVirtualModel()
        {
            foreach (var article in Load().articles)
                foreach (var example in article.examples.Where(e => e.language == "nlsh"))
                {
                    var terminal = new TerminalSession(new FacilitySimulation(MissionDefinition.NightShift), DeviceId.Ws07);
                    foreach (string line in example.code.Split('\n'))
                        Assert.That(terminal.Execute(line).ExitCode, Is.Zero, article.id + ": " + line);
                }
        }
        [Test]
        public void SearchFindsCommandsAndCategoriesAndReadingFindsADocument()
        {
            var world = new FacilitySimulation(MissionDefinition.NightShift); using var field = new FieldSession(new FieldCatalog(Load()), world);
            Assert.That(field.Search("kubectl", ""),Is.Empty,"Future content stays locked");
            field.Discover("kubernetes");
            Assert.That(field.Search("kubectl", "").Single().id, Is.EqualTo("kubernetes"));
            Assert.That(field.Search("", "Сети"),Is.Empty);
            world[DeviceId.Ws07].WriteFile("/home/operator/shift-note.txt", "test record");
            new TerminalSession(world, DeviceId.Ws07).Execute("cat shift-note.txt");
            Assert.That(field.FoundDocuments.Contains("shift-note"), Is.True);
        }
    }
}
