using System;
using System.Linq;
using Hostfall.Simulation;
using NUnit.Framework;
namespace Hostfall.Tests
{
    public sealed class ActionRouteTests
    {
        public static void Finish(FacilitySimulation world,DeviceId id)
        {
            world.Terminals.Begin(id);
            for(int i=0;i<2000&&!world.Terminals.IsComplete(id);i++) world.Terminals.Tick(id,.1f);
            Assert.That(world.Terminals.IsComplete(id),Is.True,id.ToString());
        }
        [Test] public void AuthoredSequenceTypesBeforeCommitAndCannotRunOutOfOrder()
        {
            var world=new FacilitySimulation(); Assert.That(world.AutomatedRoute,Is.True);
            Assert.That(world.Terminals.Begin(DeviceId.Edge01),Is.False);
            world.Terminals.Tick(DeviceId.Core01,10); Assert.That(world.Mission.CompletedObjectives,Is.Zero);
            Assert.That(world.Terminals.Begin(DeviceId.Ws07),Is.True);
            world.Terminals.Tick(DeviceId.Ws07,.04f);
            Assert.That(world[DeviceId.Ws07].Terminal.Draft,Is.EqualTo("ho"));
            Assert.That(world[DeviceId.Ws07].Terminal.History,Is.Empty);
            new TerminalSession(world,DeviceId.Ws07).Execute("ps");
            Assert.That(world.Mission.CompletedObjectives,Is.Zero,"Internal shell observation cannot complete a route objective");
            Finish(world,DeviceId.Ws07);
            Assert.That(world.PulseUnlocked,Is.True); Assert.That(world.SourceIsolated,Is.True);
            Assert.That(world.ServerAccess,Is.False,"Door belongs to the physical EDGE checkpoint");
            Finish(world,DeviceId.Edge01); Assert.That(world.ServerAccess,Is.True);
            Finish(world,DeviceId.Core01); Assert.That(world.Mission.IsComplete,Is.True);
        }
        [Test] public void MidTypingRoundTripDoesNotReplayCommittedCommandsOrRewards()
        {
            var world=new FacilitySimulation(); world.Terminals.Begin(DeviceId.Ws07);
            for(int i=0;i<14;i++) world.Terminals.Tick(DeviceId.Ws07,.1f);
            var before=world.Capture(); Assert.That(before.routines.Single(r=>r.device==DeviceId.Ws07).typed,Is.GreaterThan(0));
            var restored=FacilitySimulation.Restore(before);
            Assert.That(restored[DeviceId.Ws07].Terminal.Draft,Is.EqualTo(world[DeviceId.Ws07].Terminal.Draft));
            Finish(restored,DeviceId.Ws07);
            Assert.That(restored[DeviceId.Ws07].Terminal.History.Count(h=>h=="hostname"),Is.EqualTo(1));
            int count=restored[DeviceId.Ws07].Terminal.History.Count;
            restored=FacilitySimulation.Restore(restored.Capture()); restored.Terminals.Begin(DeviceId.Ws07);
            restored.Terminals.Tick(DeviceId.Ws07,10);
            Assert.That(restored[DeviceId.Ws07].Terminal.History.Count,Is.EqualTo(count));
        }
        [Test] public void PulseAndDashHavePersistentIndependentBudgetsAndExpire()
        {
            var world=new FacilitySimulation(); Assert.That(world.Actions.TryPulse("watcher-r17",world.PulseUnlocked),Is.False);
            Finish(world,DeviceId.Ws07);
            Assert.That(world.Actions.TryPulse("watcher-r17",world.PulseUnlocked),Is.True);
            Assert.That(world.Actions.TryPulse("sentry-r18",true),Is.False);
            Assert.That(world.Actions.TryDash(),Is.True); Assert.That(world.Actions.TryDash(),Is.False);
            world.Actions.Tick(1);
            var restored=FacilitySimulation.Restore(world.Capture());
            Assert.That(restored.Actions.PulseCooldown,Is.EqualTo(8)); Assert.That(restored.Actions.DashCooldown,Is.EqualTo(.4f).Within(.001f));
            Assert.That(restored.Actions.Stun("watcher-r17"),Is.EqualTo(2));
            restored.Actions.Tick(2); Assert.That(restored.Actions.Stun("watcher-r17"),Is.Zero); Assert.That(restored.Actions.Slow("watcher-r17"),Is.EqualTo(4));
            restored.Actions.Tick(6); Assert.That(restored.Actions.TryPulse("sentry-r18",true),Is.True);
            Assert.That(restored.Actions.Stun("watcher-r17"),Is.Zero);
        }
        [Test] public void CorruptRoutineProgressAndTimersAreRejected()
        {
            var world=new FacilitySimulation(); var data=world.Capture(); data.routines[1].typed=1;
            Assert.Throws<ArgumentException>(()=>FacilitySimulation.Restore(data));
            data=world.Capture(); data.routines[0].step=999;
            Assert.Throws<ArgumentException>(()=>FacilitySimulation.Restore(data));
            data=world.Capture(); data.actions.watcherStun=float.NaN;
            Assert.Throws<ArgumentException>(()=>FacilitySimulation.Restore(data));
            data=world.Capture(); data.routines[0]=null;
            Assert.Throws<ArgumentException>(()=>FacilitySimulation.Restore(data));
        }
        [Test] public void PowerLossSuspendsTypingAndFinishedRelayDoesNotRefreshItsDebuffOnLoad()
        {
            var world=new FacilitySimulation(); world.Terminals.Begin(DeviceId.Ws07); world.Terminals.Tick(DeviceId.Ws07,.04f);
            string draft=world[DeviceId.Ws07].Terminal.Draft;
            world.SetPower(DeviceId.Ws07,false); world.Terminals.Tick(DeviceId.Ws07,1);
            Assert.That(world[DeviceId.Ws07].Terminal.Draft,Is.EqualTo(draft));
            world.SetPower(DeviceId.Ws07,true); Finish(world,DeviceId.Ws07); Finish(world,DeviceId.Edge01);
            world.Actions.Tick(2); var restored=FacilitySimulation.Restore(world.Capture());
            restored.Terminals.Begin(DeviceId.Edge01); restored.Terminals.Tick(DeviceId.Edge01,1);
            Assert.That(restored.Actions.Slow("watcher-r17"),Is.EqualTo(10));
            Assert.That(restored.ServerAccess,Is.True);
        }
        [Test] public void LegacyIsolatedWorldKeepsHistoryAndPreservesUnlockedGate()
        {
            var legacy=new FacilitySimulation(MissionDefinition.NightShift); var shell=new TerminalSession(legacy,DeviceId.Ws07);
            shell.Execute("ps"); shell.Execute("systemctl disable cache-sync"); shell.Execute("systemctl stop cache-sync");
            var restored=FacilitySimulation.Restore(legacy.Capture(),MissionDefinition.ActionRoute);
            Assert.That(restored.PulseUnlocked,Is.True); Assert.That(restored.Mission.CompletedObjectives,Is.EqualTo(2));
            Assert.That(restored.Terminals.IsComplete(DeviceId.Edge01),Is.True);
            Assert.That(restored[DeviceId.Ws07].Terminal.History,Is.EqualTo(legacy[DeviceId.Ws07].Terminal.History));
            Finish(restored,DeviceId.Edge01); Assert.That(restored.ServerAccess,Is.True);
        }
        [Test] public void UpsPowerBudgetEnforcesCapacityAndOverload()
        {
            var ups = PowerGraph.CreateSector03UpsPreset();
            Assert.That(ups.TotalActiveLoad, Is.EqualTo(5));
            Assert.That(ups.IsOverloaded, Is.False);
            Assert.That(ups.TryGetCircuit("vent", out var vent) && vent.IsEnergized, Is.True);
            Assert.That(ups.TryGetCircuit("alarm", out var alarm) && alarm.IsEnergized, Is.True);
            Assert.That(ups.TryGetCircuit("lift", out var lift) && !lift.IsEnergized, Is.True);

            // Attempting to energize lift (5) while vent (3) and alarm (2) are active -> 10/8 OVERLOAD
            ups.SetCircuit("lift", true);
            Assert.That(ups.TotalActiveLoad, Is.EqualTo(10));
            Assert.That(ups.IsOverloaded, Is.True);
            Assert.That(lift.IsEnergized, Is.False);

            // Disabling alarm (2) brings load to 3 + 5 = 8 <= 8 STABLE
            ups.SetCircuit("alarm", false);
            Assert.That(ups.TotalActiveLoad, Is.EqualTo(8));
            Assert.That(ups.IsOverloaded, Is.False);
            Assert.That(lift.IsEnergized, Is.True);
            Assert.That(vent.IsEnergized, Is.True);
            Assert.That(alarm.IsEnergized, Is.False);
        }
    }
}
