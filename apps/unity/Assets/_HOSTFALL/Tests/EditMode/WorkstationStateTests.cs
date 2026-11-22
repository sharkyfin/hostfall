using System;
using Hostfall.Simulation;
using Hostfall.Rendering;
using NUnit.Framework;
namespace Hostfall.Tests
{
    public sealed class WorkstationStateTests
    {
        [Test] public void PartialScenarioSurvivesWorldSaveAndRewardsOnlyOnce()
        {
            var world=new FacilitySimulation();var scenario=TerminalScenarioCatalog.GetScenario(DeviceId.Data01);var session=world.Screens.Get(scenario);
            session.Tick(.1f);Assert.That(session.Memory.Draft.Length,Is.GreaterThan(0));
            var restored=FacilitySimulation.Restore(world.Capture());var resumed=restored.Screens.Get(scenario);
            Assert.That(resumed.Typed,Is.EqualTo(session.Typed));Assert.That(resumed.Memory.Draft,Is.EqualTo(session.Memory.Draft));
            for(int i=0;i<5000;i++)resumed.Tick(.1f);
            Assert.That(resumed.Complete,Is.True);Assert.That(resumed.Memory.History.Count,Is.EqualTo(scenario.Steps.Count));
            Assert.That(resumed.ClaimCompletion(),Is.True);Assert.That(resumed.ClaimCompletion(),Is.False);
            Assert.That(FacilitySimulation.Restore(restored.Capture()).Screens.Get(scenario).ClaimCompletion(),Is.False);
        }
        [Test] public void UnityJsonFromBeforeWeaponDropsRemainsLoadable()
        {
            var world=new FacilitySimulation();var json=UnityEngine.JsonUtility.ToJson(world.Capture());
            json=System.Text.RegularExpressions.Regex.Replace(json,"\"weapons\":\\{[^{}]*\\},", "");
            Assert.That(json.Contains("\"weapons\""),Is.False);
            var old=UnityEngine.JsonUtility.FromJson<FacilityStateData>(json);Assert.DoesNotThrow(()=>FacilitySimulation.Restore(old));
            var invalid=world.Capture();invalid.weapons=new WeaponWorldData{nextId=10,drops=null,collected=null};Assert.Throws<ArgumentException>(()=>FacilitySimulation.Restore(invalid));
        }
        [Test] public void CampaignAfterExpandedTerminalRestoresItsActualProgress()
        {
            var world=new FacilitySimulation(MissionDefinition.FacilityCampaign);
            foreach(var id in new[]{DeviceId.Ws07,DeviceId.Edge01,DeviceId.Core01}){world.Terminals.Begin(id);for(int i=0;i<5000&&!world.Terminals.IsComplete(id);i++)world.Terminals.Tick(id,.1f);}
            var scenario=TerminalScenarioCatalog.GetScenario(DeviceId.Data01);var screen=world.Screens.Present(scenario);for(int i=0;i<5000;i++)screen.Tick(.1f);
            Assert.That(screen.ClaimCompletion(),Is.True);world.RecordTerminalObjective(DeviceId.Data01);
            Assert.That(world.Mission.CompletedObjectives,Is.EqualTo(4));var data=world.Capture();var restored=FacilitySimulation.Restore(data,MissionDefinition.FacilityCampaign);
            Assert.That(restored.Mission.CompletedObjectives,Is.EqualTo(4));Assert.That(restored.Screens.Find(DeviceId.Data01).Rewarded,Is.True);
            data.screenSessions=null;Assert.That(FacilitySimulation.Restore(data).Mission.CompletedObjectives,Is.EqualTo(4),"Accept old campaign saves without expanded transcripts");
        }
        [Test] public void DiagnosisAndSuccessfulSessionHaveIndependentResumableScreens()
        {
            var world=new FacilitySimulation();var normal=TerminalScenarioCatalog.GetScenario(DeviceId.Data01);
            TerminalScenarioCatalog.TryGetScenario("t07_data_unmet",out var diagnosis);
            world.Screens.Get(normal);var session=world.Screens.Present(diagnosis);session.Tick(.1f);
            Assert.That(world.Screens.Find(DeviceId.Data01),Is.SameAs(session));
            var restored=FacilitySimulation.Restore(world.Capture());Assert.That(restored.Screens.Find(DeviceId.Data01).Scenario.ScenarioId,Is.EqualTo(diagnosis.ScenarioId));
            Assert.That(restored.Screens.Present(normal).Step,Is.Zero);
        }
        [Test] public void OldSavesHaveEmptyScreensAndSwitchesAndCorruptCursorsAreRejected()
        {
            var data=new FacilitySimulation().Capture();data.screenSessions=null;data.disabledWorklights=null;
            var restored=FacilitySimulation.Restore(data);Assert.That(restored.Screens.Capture(),Is.Empty);
            var session=restored.Screens.Get(TerminalScenarioCatalog.GetScenario(DeviceId.Pwr01));var broken=session.Capture();broken.typed=int.MaxValue;
            Assert.Throws<ArgumentException>(()=>session.Restore(broken));broken=session.Capture();broken.wait=float.NaN;Assert.Throws<ArgumentException>(()=>session.Restore(broken));
        }
        [Test] public void SwitchChangesSurviveSaveAndDoNotChangeAnotherWorkstation()
        {
            var world=new FacilitySimulation();world.Switches.SetOff("worklight.Ws07",true);var restored=FacilitySimulation.Restore(world.Capture());
            Assert.That(restored.Switches.IsOff("worklight.Ws07"),Is.True);Assert.That(restored.Switches.IsOff("worklight.Core01"),Is.False);
        }
        [TestCase(1920,1080)][TestCase(3584,2240)][TestCase(3072,1920)][TestCase(1280,800)]
        public void NativeTextDoesNotIncreaseWorldPixelBudget(int width,int height)
        {
            float scale=DisplayPixelBudget.ScaleFor(width,height);Assert.That(width*(double)height*scale*scale,Is.LessThanOrEqualTo(DisplayPixelBudget.PixelBudget+1));
            Assert.That(scale,Is.InRange(.1f,1f));
        }
    }
}
