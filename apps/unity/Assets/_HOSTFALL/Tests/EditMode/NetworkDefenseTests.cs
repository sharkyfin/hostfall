using System;
using System.Linq;
using Hostfall.Simulation.Computers;
using NUnit.Framework;

namespace Hostfall.Tests
{
    public sealed class NetworkDefenseTests
    {
        [Test] public void CpuIsolationAndRoutingShareTheBudgetWithoutDeletingProcesses()
        {
            var game=new NetworkDefenseSession();Assert.That(game.SetCpu(3,3),Is.True);
            Assert.That(game.SetIsolation(3,true),Is.True);Assert.That(game.SetCpu(4,1),Is.False);
            Assert.That(game.SetIsolation(4,true),Is.False);Assert.That(game.SetCpu(0,1),Is.False);
            game.SetCpu(3,0);game.Start();for(int i=0;i<240;i++)game.Advance(1.0/30);
            Assert.That(game.Processes.Any(p=>p.node==3),Is.True);Assert.That(game.Stage,Is.Zero);
            Assert.That(game.ToggleRoute(0),Is.True);Assert.That(game.Destination(0),Is.EqualTo(2));
        }
        [Test] public void UnprotectedNetworkFailsAndRetryRetainsOnlyEarnedWaves()
        {
            var game=new NetworkDefenseSession();game.Start();for(int i=0;i<600&&game.Phase==NetworkPhase.Running;i++)game.Advance(.1);
            Assert.That(game.Phase,Is.EqualTo(NetworkPhase.Failed));game.Start();Assert.That(game.Integrity,Is.EqualTo(100));
            Assert.That(game.Stage,Is.Zero);Assert.That(game.Processes,Is.Empty);NetworkDefenseSession.Restore(game.Capture());
        }
        [Test] public void NetworkThreeWavesCanBeDefendedWithActualRouteCpuIsolationAndPurgeCommands()
        {
            var game=new NetworkDefenseSession();int events=0;game.StageCompleted+=_=>events++;
            Defend(game,3);
            Assert.That(game.Phase,Is.EqualTo(NetworkPhase.Complete),"stage="+game.Stage+" infection5="+game.Infection(5)+" infection6="+game.Infection(6)+" integrity="+game.Integrity);
            Assert.That(events,Is.EqualTo(3));Assert.That(game.Integrity,Is.GreaterThan(0));
            NetworkDefenseSession.Restore(game.Capture());
        }
        [Test] public void PartialNetworkSaveReplaysEveryProcessTimerAndResourceExactly()
        {
            var a=new NetworkDefenseSession();Configure(a);a.Start();for(int i=0;i<163;i++)a.Advance(.023);
            var b=NetworkDefenseSession.Restore(a.Capture());
            for(int i=0;i<400;i++)
            {
                if(i%50==0){a.Purge(5);b.Purge(5);}a.Advance(.017);b.Advance(.017);
                var x=a.Capture();var y=b.Capture();
                Assert.That(y.credits,Is.EqualTo(x.credits));Assert.That(y.elapsed,Is.EqualTo(x.elapsed));Assert.That(y.integrity,Is.EqualTo(x.integrity));
                Assert.That(y.processes.Select(p=>p.health),Is.EqualTo(x.processes.Select(p=>p.health)));
                Assert.That(y.processes.Select(p=>p.node),Is.EqualTo(x.processes.Select(p=>p.node)));
            }
        }
        [Test] public void WorldReinforcementsAndOutageContinueAfterLeavingAppAndDoNotResetOnRestore()
        {
            var security=new NetworkSecuritySystem();security.Start();for(int i=0;i<48;i++)security.Advance(.25);
            Assert.That(security.Released,Is.EqualTo(1));security.EarnStage(1);
            for(int i=0;i<96;i++)security.Advance(.25);Assert.That(security.Released,Is.EqualTo(1));
            security.EarnStage(2);security.Advance(.25);var copy=NetworkSecuritySystem.Restore(security.Capture(),2);
            Assert.That(copy.OutageRemaining,Is.EqualTo(14.75));copy.EarnStage(2);Assert.That(copy.OutageRemaining,Is.EqualTo(14.75));
            copy.EarnStage(3);Assert.That(copy.Released,Is.EqualTo(1));Assert.That(copy.HasPending,Is.False);
            for(int i=0;i<100;i++)copy.Advance(.25);Assert.That(copy.OutageRemaining,Is.Zero);
        }
        [Test] public void NetworkRejectsCorruptBudgetDuplicatePacketsAndNonfiniteClocks()
        {
            var game=new NetworkDefenseSession();var d=game.Capture();d.nodes[3].cpu=4;d.nodes[4].cpu=1;
            Assert.Throws<ArgumentException>(()=>NetworkDefenseSession.Restore(d));
            d=game.Capture();d.elapsed=double.NaN;Assert.Throws<ArgumentException>(()=>NetworkDefenseSession.Restore(d));
            game.Start();game.Advance(.1);d=game.Capture();d.processes=new[]{d.processes[0],d.processes[0]};d.spawned=2;
            Assert.Throws<ArgumentException>(()=>NetworkDefenseSession.Restore(d));
            d=game.Capture();d.nodes[0].isolated=true;Assert.Throws<ArgumentException>(()=>NetworkDefenseSession.Restore(d));
        }
        [Test] public void CampaignRestoresNetworkThenPayloadRewardOrderAndRejectsDuplicateMessages()
        {
            var campaign=new ComputerCampaign();Assert.That(campaign.BeginNetwork(),Is.False);
            campaign.BeginSnake();ComputerCampaignTests.Complete(campaign.Snake);Assert.That(campaign.BeginNetwork(),Is.True);
            Defend(campaign.Network,3);Assert.That(campaign.Network.Stage,Is.EqualTo(3));
            Assert.That(campaign.Actions.Count,Is.EqualTo(9));Assert.That(campaign.NetworkCommandOnline,Is.False);
            Assert.That(campaign.NetworkSecurity.OutageRemaining,Is.EqualTo(15));
            for(int i=0;i<21;i++)campaign.AdvanceWorld(.25);
            Assert.That(campaign.NetworkSecurity.OutageRemaining,Is.EqualTo(9.75));
            SignalTests.CommissionSignalPath(campaign);
            int actionsBefore = campaign.Actions.Count;
            for(int i=0;i<24000&&campaign.Payload.Phase!=PayloadPhase.Complete;i++){campaign.BeginPayload();ComputerGameTests.DrivePayload(campaign.Payload);}
            var restored=ComputerCampaign.Restore(campaign.Capture());
            Assert.That(restored.Messages.Select(m=>m.id),Is.EqualTo(campaign.Messages.Select(m=>m.id)));
            Assert.That(restored.Actions.Count,Is.EqualTo(actionsBefore + 5));Assert.That(restored.NetworkSecurity.OutageRemaining,Is.EqualTo(9.75));
            Assert.That(restored.Home.TryRead(ComputerCampaign.NetworkIncidentPath,out _),Is.True);
            var bad=campaign.Capture();bad.messages[1]=bad.messages[0];Assert.Throws<ArgumentException>(()=>ComputerCampaign.Restore(bad));
            var old=new ComputerCampaign().Capture();old.network=null;old.networkSecurity=null;
            Assert.That(ComputerCampaign.Restore(old).Network.Stage,Is.Zero,"Iteration1 snapshots remain readable");
        }
        [Test] public void RelayCadenceIsSavedAndStopsWithPermanentCommandIsolation()
        {
            var state=new NetworkSecuritySystem();state.Advance(.2);
            var restored=NetworkSecuritySystem.Restore(state.Capture(),0);int messages=0;restored.RelayRequested+=()=>messages++;
            restored.Advance(.2);Assert.That(messages,Is.Zero);restored.Advance(.1);Assert.That(messages,Is.EqualTo(1));
            restored.EarnStage(1);restored.Advance(.25);restored.Advance(.25);Assert.That(messages,Is.EqualTo(1));
            Assert.That(restored.Capture().relayClock,Is.Zero);
        }
        internal static void Configure(NetworkDefenseSession game)
        {
            game.SetIsolation(5,true);game.SetIsolation(6,true);game.SetCpu(5,1);game.SetCpu(6,1);
            game.ToggleRoute(2); // Route EDGE-B through the alternate relay; both paths must remain defendable.
        }
        internal static void Defend(NetworkDefenseSession game,int goal)
        {
            Configure(game);
            for(int i=0;i<12000&&game.Stage<goal;i++)
            {
                game.Start();
                int target=game.Infection(5)>=game.Infection(6)?5:6;
                if(game.Infection(target)>30||game.Processes.Count(p=>p.node==target)>=2)game.Purge(target);
                game.Advance(1.0/30);
            }
        }
    }
}
