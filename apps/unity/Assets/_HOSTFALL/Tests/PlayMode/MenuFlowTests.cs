using System.Collections;
using System.IO;
using System.Linq;
using Hostfall.Core;
using Hostfall.UI;
using Hostfall.Field;
using Hostfall.Persistence;
using Hostfall.Player;
using Hostfall.Simulation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
namespace Hostfall.Tests
{
    public sealed class MenuFlowTests
    {
        public static void FinishRoutine(FacilitySimulation world,DeviceId id)
        {
            world.Terminals.Begin(id);
            for(int i=0;i<2000&&!world.Terminals.IsComplete(id);i++) world.Terminals.Tick(id,.1f);
            Assert.That(world.Terminals.IsComplete(id),Is.True);
        }
        private static VisualElement View()=>Object.FindAnyObjectByType<UIDocument>().rootVisualElement;
        private static IEnumerator Click(Button button)
        {
            Assert.That(button,Is.Not.Null); button.Focus();
            using(var submit=NavigationSubmitEvent.GetPooled()) button.SendEvent(submit);
            yield return null;
        }
        [UnityTest] public IEnumerator MainMenuOwnsNewAndLoadAndPauseHasNeither()
        {
            yield return SceneManager.LoadSceneAsync("NorthlineFacility"); yield return null;
            var root=View(); var ui=Object.FindAnyObjectByType<FacilityUI>(); var controls=Object.FindAnyObjectByType<ControlState>();
            Assert.That(controls.Mode,Is.EqualTo(ControlMode.MainMenu)); Assert.That(Time.timeScale,Is.Zero);
            yield return new WaitForSecondsRealtime(.08f);
            Assert.That(root.focusController.focusedElement,Is.EqualTo(root.Q<Button>("restart")),"Main menu needs keyboard focus before any pointer click");
            Assert.That(root.Q("main-menu").Contains(root.Q<Button>("restart")),Is.True);
            Assert.That(root.Q("main-menu").Contains(root.Q<Button>("load-game")),Is.True);
            Assert.That(root.Q("menu-main").Q<Button>("load-game"),Is.Null); Assert.That(root.Q("menu-main").Q<Button>("restart"),Is.Null);
            ui.OpenPage("load"); yield return null;
            Assert.That(root.Q<ScrollView>("menu-page-body").Query<Button>().ToList().Count,Is.EqualTo(5));
            ui.ShowMainMenu(); ui.BeginSession(); yield return null;
            Assert.That(controls.Mode,Is.EqualTo(ControlMode.Introduction));
            Assert.That(root.Q("introduction").Query<Label>().ToList().Any(l=>l.text.Contains("импульс")),Is.True);
            yield return Click(root.Q<Button>("intro-understood"));
            Assert.That(controls.Mode,Is.EqualTo(ControlMode.Exploring));
            Assert.That(Object.FindAnyObjectByType<FacilityRoot>().Field.IntroductionSeen,Is.True);
            controls.SetMode(ControlMode.Paused); ui.OpenPage("load");
            Assert.That(root.Q("menu-main").style.display.value,Is.EqualTo(DisplayStyle.Flex),"Load cannot be opened from pause");
            ui.OpenPage("settings"); yield return new WaitForSecondsRealtime(.08f);
            foreach(string setting in new[]{"setting-fullscreen","setting-vsync"})
            {
                var mark=root.Q<Toggle>(setting).Q(className:"unity-toggle__checkmark");
                Assert.That(mark.worldBound.width,Is.GreaterThanOrEqualTo(20),"Settings checkmarks must remain visible beside wrapped labels");
                Assert.That(mark.worldBound.height,Is.GreaterThanOrEqualTo(20));
            }
            root.Q<Slider>("setting-sensitivity").value=1.4f;
            root.Q<Slider>("setting-volume").value=.35f;
            Assert.That(AudioListener.volume,Is.EqualTo(.35f).Within(.01f));
            Assert.That(Object.FindAnyObjectByType<PlayerMotor>().LookSensitivity,Is.EqualTo(1.4f).Within(.01f));
            yield return Click(root.Q<Button>("open-controls"));
            Assert.That(root.Q<Label>("menu-page-title").text,Is.EqualTo("Управление"));
            Assert.That(root.Q<ScrollView>("menu-page-body").Query<Label>().ToList().Any(l=>l.text=="W A S D"),Is.True);
            ui.ShowMainMenu(); ui.BeginSession(); Assert.That(controls.Mode,Is.EqualTo(ControlMode.Exploring),"Intro is shown once per campaign");
            controls.SetMode(ControlMode.Paused); LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator LibraryUsesOneLanguageKeepsFutureLessonsLockedAndHasNoQuestHintButtons()
        {
            WorldSaveController.NewGameLanguage="cpp"; WorldSaveController.FreshStartRequested=true;
            yield return SceneManager.LoadSceneAsync("NorthlineFacility"); yield return null;
            var root=View(); var field=Object.FindAnyObjectByType<FieldSystemUI>();
            Object.FindAnyObjectByType<ControlState>().SetMode(ControlMode.Exploring);
            field.OpenArchive(); field.SelectDomain("Программирование"); yield return null;
            Assert.That(root.Q<KnowledgeGraphView>(),Is.Null);
            Assert.That(root.Q<ScrollView>("knowledge-results").Q<Button>("article-cpp.start"),Is.Not.Null);
            Assert.That(root.Q<ScrollView>("knowledge-results").Q<Button>("article-python.start"),Is.Null);
            Assert.That(root.Q<ScrollView>("knowledge-results").Q<Button>("article-cpp.for"),Is.Null);
            yield return Click(root.Q<Button>("article-understood"));
            Assert.That(root.Q<ScrollView>("knowledge-results").Q<Button>("article-cpp.variables"),Is.Not.Null);
            field.OpenArticle("process"); yield return null;
            var article=root.Q<ScrollView>("article-body");
            Assert.That(article.Query<Label>().ToList().Any(l=>l.text=="systemctl stop"),Is.True);
            Assert.That(article.Query<Label>().ToList().Any(l=>l.text.Contains("man7.org")||l.text.Contains("FieldContentTests")||l.text.Contains("Источники")),Is.False);
            field.OpenJournal(); yield return null;
            Assert.That(root.Q<Button>("quest-hint"),Is.Null);
            Assert.That(root.Q<ScrollView>("quest-body").Query<Foldout>().ToList(),Is.Empty);
            Assert.That(root.Q<ScrollView>("quest-body").Query<Label>().ToList().Any(l=>l.text.Contains("Доберитесь")),Is.True);
            var simulation=Object.FindAnyObjectByType<FacilityRoot>().Simulation;
            Assert.That(simulation.AutomatedRoute,Is.True);
            new TerminalSession(simulation,DeviceId.Ws07).Execute("ps");
            Assert.That(simulation.Mission.CompletedObjectives,Is.Zero);
            FinishRoutine(simulation,DeviceId.Ws07);
            Assert.That(simulation.Mission.CompletedObjectives,Is.EqualTo(1));
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator NewCampaignChoiceAndLoadingRestoreLanguageWorldAndOneTimeIntroduction()
        {
            string directory=Path.Combine(Path.GetTempPath(),"hostfall-menu-"+System.Guid.NewGuid().ToString("N"));
            WorldSaveController.TestStorageDirectory=directory;
            try
            {
                yield return SceneManager.LoadSceneAsync("NorthlineFacility"); yield return null;
                yield return Click(View().Q<Button>("restart"));
                yield return Click(View().Q<Button>("language-cpp"));
                Assert.That(Object.FindAnyObjectByType<FacilityUI>().Preferences.NewGameLanguage,Is.EqualTo("cpp"));
                yield return Click(View().Q<Button>("start-new-game")); yield return null;
                var composition=Object.FindAnyObjectByType<FacilityRoot>();
                Assert.That(composition.Field.ProgrammingLanguage,Is.EqualTo("cpp"));
                Assert.That(Object.FindAnyObjectByType<ControlState>().Mode,Is.EqualTo(ControlMode.Introduction));
                yield return Click(View().Q<Button>("intro-understood"));
                composition.Field.Study("cpp.start"); FinishRoutine(composition.Simulation,DeviceId.Ws07);
                Assert.That(composition.Persistence.Save("manual1"),Is.True);
                Object.FindAnyObjectByType<FacilityUI>().ShowMainMenu();
                yield return Click(View().Q<Button>("restart"));
                yield return Click(View().Q<Button>("language-python"));
                yield return Click(View().Q<Button>("start-new-game")); yield return Click(View().Q<Button>("start-new-game")); yield return null;
                composition=Object.FindAnyObjectByType<FacilityRoot>();
                Assert.That(composition.Field.ProgrammingLanguage,Is.EqualTo("python"));
                Assert.That(composition.Simulation.Mission.CompletedObjectives,Is.Zero);
                yield return Click(View().Q<Button>("intro-understood"));
                Assert.That(composition.Persistence.Save("manual2"),Is.True);
                var ui=Object.FindAnyObjectByType<FacilityUI>(); ui.ShowMainMenu(); ui.OpenPage("load"); yield return null;
                yield return Click(View().Q<Button>("slot-manual1")); yield return null;
                composition=Object.FindAnyObjectByType<FacilityRoot>();
                Assert.That(composition.Field.ProgrammingLanguage,Is.EqualTo("cpp"));
                Assert.That(composition.Field.State("cpp.start"),Is.EqualTo(KnowledgeState.Studied));
                Assert.That(composition.Simulation.Mission.CompletedObjectives,Is.EqualTo(1));
                Assert.That(composition.Field.IntroductionSeen,Is.True);
                Assert.That(Object.FindAnyObjectByType<ControlState>().Mode,Is.EqualTo(ControlMode.Exploring));
                Assert.That(composition.Simulation[DeviceId.Ws07].Terminal.Lines.Any(l=>l.text.Contains("431")),Is.True);
                ui=Object.FindAnyObjectByType<FacilityUI>(); ui.ShowMainMenu(); yield return Click(View().Q<Button>("restart")); yield return Click(View().Q<Button>("language-python")); ui.BeginSession();
                Assert.That(composition.Field.ProgrammingLanguage,Is.EqualTo("cpp"),"New-game choice cannot rewrite a loaded campaign");
                Object.FindAnyObjectByType<ControlState>().SetMode(ControlMode.Paused); LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                foreach(var store in Object.FindObjectsByType<WorldSaveController>(FindObjectsSortMode.None)) store.enabled=false;
                WorldSaveController.TestStorageDirectory=null; WorldSaveController.FreshStartRequested=false; WorldSaveController.ResumeRequested=false; WorldSaveController.PendingLoad=null;
                if(Directory.Exists(directory)) Directory.Delete(directory,true);
            }
        }
    }
}
