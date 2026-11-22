using System;
using NUnit.Framework;
using Hostfall.Simulation.Computers;
namespace Hostfall.Tests
{
    public sealed class SignalTests
    {
        [Test]public void FrequencyMismatchAccumulatesPhaseAndThreeEnabledSignalsMustHoldTogether()
        {
            var game=new SignalSession();game.Start();var before=game.PhaseError(0);game.Advance(.2);
            Assert.That(game.PhaseError(0),Is.Not.EqualTo(before));Assert.That(game.StableSeconds,Is.Zero);
            Tune(game);game.SetEnabled(2,false);for(int i=0;i<50;i++)game.Advance(.1);
            Assert.That(game.Stage,Is.Zero);Assert.That(game.StableSeconds,Is.Zero);
            Tune(game);for(int i=0;i<30&&game.Stage==0;i++)game.Advance(.1);Assert.That(game.Stage,Is.EqualTo(1));
        }
        [Test]public void ThreeDifferentCalibrationsUseFrequencyTrimAndPolarityAndCanBeRetunedWithoutDuplicateRewards()
        {
            var game=new SignalSession();int stages=0,locks=0;game.StageCompleted+=_=>stages++;game.FinalLock+=()=>locks++;
            for(int stage=1;stage<=3;stage++)
            {
                game.Start();Tune(game);Assert.That(game.AllInLock,Is.True);
                for(int i=0;i<100&&game.Phase==SignalPhase.Running;i++)game.Advance(.1);
                Assert.That(game.Stage,Is.EqualTo(stage));
            }
            Assert.That(game.Phase,Is.EqualTo(SignalPhase.Complete));Assert.That(stages,Is.EqualTo(3));Assert.That(locks,Is.EqualTo(1));
            Assert.That(game.SetTrim(0,90),Is.False,"Completed captures stay valid until explicit retuning starts");
            game.Start();Tune(game);for(int i=0;i<100&&game.Phase==SignalPhase.Running;i++)game.Advance(.1);
            Assert.That(locks,Is.EqualTo(2));Assert.That(stages,Is.EqualTo(3));SignalSession.Restore(game.Capture());
        }
        [Test]public void MistimedSignalsFailWithinTheKnownWindowAndRetryRetainsEarnedCalibration()
        {
            var game=new SignalSession();game.Start();Tune(game);for(int i=0;i<160;i++)game.Advance(.02);
            Assert.That(game.Stage,Is.EqualTo(1));game.Start();game.SetEnabled(0,false);
            for(int i=0;i<400;i++)game.Advance(.1);
            Assert.That(game.Phase,Is.EqualTo(SignalPhase.Failed));Assert.That(game.Stage,Is.EqualTo(1));Assert.That(game.WindowRemaining,Is.Zero);
            game.Start();Tune(game);for(int i=0;i<50;i++)game.Advance(.1);Assert.That(game.Stage,Is.EqualTo(2));
        }
        [Test]public void PartialOscillatorAndHoldStateReplayExactlyAfterCapture()
        {
            var a=new SignalSession();a.Start();Tune(a);a.Advance(.2173219876);var b=SignalSession.Restore(a.Capture());
            for(int i=0;i<110;i++)
            {
                if(i==50){a.SetFrequency(1,38);b.SetFrequency(1,38);}a.Advance(.013);b.Advance(.013);
                var x=a.Capture();var y=b.Capture();Assert.That(y.drift,Is.EqualTo(x.drift));Assert.That(y.elapsed,Is.EqualTo(x.elapsed));Assert.That(y.stable,Is.EqualTo(x.stable));
            }
            Assert.That(a.StableSeconds,Is.Zero,"Leaving the frequency tolerance resets continuous hold");
        }
        [Test]public void WorldHeatRechargeAndTimedBarrierKeepTheirClocksAndRepeatLockDoesNotRepeatCalibration()
        {
            var equipment=new SignalEquipment();int starts=0;equipment.FirstStarted+=()=>starts++;equipment.Start();equipment.Start();Assert.That(starts,Is.EqualTo(1));
            for(int i=1;i<=3;i++)Assert.That(equipment.Calibrate(i),Is.True);Assert.That(equipment.Calibrate(3),Is.False);equipment.OpenBarrier();
            equipment.Advance(.237,false,0,false);var copy=SignalEquipment.Restore(equipment.Capture(),3);Assert.That(copy.BarrierWindow,Is.EqualTo(19.763));
            for(int i=0;i<100;i++)copy.Advance(.25,false,0,false);Assert.That(copy.BarrierRequestedOpen,Is.False);copy.OpenBarrier();Assert.That(copy.BarrierWindow,Is.EqualTo(20));
            copy.Fail();copy.Advance(.25,false,0,false);copy.Fail();Assert.That(copy.Recharge,Is.EqualTo(4.75));Assert.That(copy.Start(),Is.False);
            for(int i=0;i<19;i++)copy.Advance(.25,false,0,false);Assert.That(copy.Start(),Is.True);
            Assert.That(copy.ReleaseReturn(),Is.True);copy.Advance(.137,false,0,false);
            var resumed=SignalEquipment.Restore(copy.Capture(),3);Assert.That(resumed.ReturnWindow,Is.EqualTo(copy.ReturnWindow));
            Assert.That(new SignalEquipment().ReleaseReturn(),Is.False);
        }
        [Test]public void ActualVentilationPowerChangesOverheatingWithoutStoppingWorldTime()
        {
            var hot=new SignalEquipment();var cooled=new SignalEquipment();hot.Start();cooled.Start();int overheats=0;hot.Overheated+=()=>overheats++;
            for(int i=0;i<100;i++){hot.Advance(.25,true,0,false);cooled.Advance(.25,true,0,true);}
            Assert.That(overheats,Is.EqualTo(1));Assert.That(hot.Recharge,Is.GreaterThan(0));Assert.That(cooled.Recharge,Is.Zero);Assert.That(cooled.Heat,Is.LessThan(.2));
        }
        [Test]public void SignalSnapshotsRejectNonfinitePhaseImpossibleChannelsAndMissingFinalLock()
        {
            var game=new SignalSession();var data=game.Capture();data.drift[1]=double.NaN;Assert.Throws<ArgumentException>(()=>SignalSession.Restore(data));
            data=game.Capture();data.frequency[1]=100;Assert.Throws<ArgumentException>(()=>SignalSession.Restore(data));
            data=game.Capture();data.stage=3;data.phase=SignalPhase.Complete;Assert.Throws<ArgumentException>(()=>SignalSession.Restore(data));
            var equipment=new SignalEquipment().Capture();equipment.barrierWindow=1;Assert.Throws<ArgumentException>(()=>SignalEquipment.Restore(equipment,0));
            Assert.That(game.SetFrequency(-1,30),Is.False);Assert.That(game.SetTrim(0,180),Is.False);
        }
        [Test]public void SignalCampaignEarnsThreeReceiptsButReopensTheTimedGateWithoutDuplicatingThem()
        {
            var campaign=new ComputerCampaign();Assert.That(campaign.BeginSignal(),Is.False);
            CommissionSignalPath(campaign);
            int count=campaign.Actions.Count;Assert.That(campaign.SignalPlant.BarrierWindow,Is.EqualTo(20));
            for(int i=0;i<84;i++)campaign.AdvanceWorld(.25);Assert.That(campaign.SignalPlant.BarrierWindow,Is.Zero);
            var restored=ComputerCampaign.Restore(campaign.Capture());Assert.That(restored.Home.TryRead(ComputerCampaign.SignalIncidentPath,out _),Is.True);
            Assert.That(restored.BeginSignal(),Is.True);Tune(restored.Signal);for(int i=0;i<100&&restored.Signal.Phase==SignalPhase.Running;i++)restored.Signal.Advance(.1);
            Assert.That(restored.Actions.Count,Is.EqualTo(count));Assert.That(restored.SignalPlant.BarrierWindow,Is.EqualTo(20));
            var old=new ComputerCampaign().Capture();old.signal=null;old.signalPlant=null;Assert.That(ComputerCampaign.Restore(old).Signal.Stage,Is.Zero);
        }
        internal static void CommissionSignalPath(ComputerCampaign campaign, int targetStage = 3)
        {
            if (!campaign.FirstTaskComplete)
            {
                campaign.BeginSnake();
                ComputerCampaignTests.Complete(campaign.Snake);
            }
            if (campaign.Network.Stage < 3)
            {
                campaign.BeginNetwork();
                NetworkDefenseTests.Defend(campaign.Network, 3);
            }
            if (!campaign.SignalAccessible)
            {
                CircuitTests.Align(campaign.Circuit);
                CircuitTests.SetOutput(campaign.Circuit, CircuitLoad.MainDoor, true);
                campaign.Circuit.Apply();
            }
            for (int goal = campaign.Signal.Stage + 1; goal <= targetStage; goal++)
            {
                Assert.That(campaign.BeginSignal(), Is.True);
                Tune(campaign.Signal);
                for (int frame = 0; frame < 100 && campaign.Signal.Phase == SignalPhase.Running; frame++)
                    campaign.Signal.Advance(.1);
                Assert.That(campaign.Signal.Stage, Is.EqualTo(goal));
            }
        }
        internal static void Tune(SignalSession game)
        {
            for(int i=0;i<3;i++)
            {
                game.SetEnabled(i,true);game.SetFrequency(i,game.ReferenceUnits);game.SetTrim(i,0);game.SetInverted(i,false);
                if(Math.Abs(game.PhaseError(i))>90)game.SetInverted(i,true);
                game.SetTrim(i,(int)Math.Round(-game.PhaseError(i)));
            }
        }
    }
}
