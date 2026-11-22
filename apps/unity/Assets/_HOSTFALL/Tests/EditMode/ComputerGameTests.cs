using System;
using System.Linq;
using Hostfall.Simulation.Computers;
using NUnit.Framework;

namespace Hostfall.Tests
{
    public sealed class ComputerGameTests
    {
        [Test] public void PayloadHasInertiaBufferedJumpAndExactPartialReplay()
        {
            var a = new PayloadSession(); a.Start(); a.Advance(.075, 1);
            Assert.That(a.VelocityX, Is.GreaterThan(0).And.LessThan(4.6));
            a.QueueJump(); a.Advance(.011, 1);
            Assert.That(a.VelocityY, Is.GreaterThan(0));
            var b = PayloadSession.Restore(a.Capture());
            for (int i = 0; i < 90; i++) { a.Advance(.016, i < 30 ? 1 : -1); b.Advance(.016, i < 30 ? 1 : -1); }
            var x = a.Capture(); var y = b.Capture();
            Assert.That(y.x, Is.EqualTo(x.x)); Assert.That(y.y, Is.EqualTo(x.y));
            Assert.That(y.vy, Is.EqualTo(x.vy)); Assert.That(y.elapsed, Is.EqualTo(x.elapsed));
            Assert.That(y.ticks, Is.EqualTo(x.ticks)); Assert.That(y.deaths, Is.EqualTo(x.deaths));
        }
        [Test] public void PayloadAllThreeAuthoredLevelsCanBeTraversedWithRealControls()
        {
            var game = new PayloadSession(); int[] milestones = new int[3];
            game.StageCompleted += stage => milestones[stage - 1]++;
            for (int i = 0; i < 24000 && game.Phase != PayloadPhase.Complete; i++)
            {
                game.Start(); DrivePayload(game);
            }
            Assert.That(game.Phase, Is.EqualTo(PayloadPhase.Complete), "stage="+game.Stage+" x="+game.X+" y="+game.Y+" deaths="+game.Deaths+" packets="+game.PacketMask);
            Assert.That(milestones, Is.EqualTo(new[] { 1, 1, 1 }));
        }
        public static void DrivePayload(PayloadSession game)
        {
            int input = 1;
            // Observe the authored obstacle shapes rather than setting score, pose or rewards.
            var laser = game.Objects.FirstOrDefault(o => o.Kind == PayloadObjectKind.Laser && o.X > game.X && o.X - game.X < 1.3);
            if (laser.Width > 0 && laser.Active && game.Y < 2.8) input = 0;
            if ((game.X > 6.9 && game.X < 7.6 || game.X > 17 && game.X < 17.7 ||
                game.Objects.Any(o => o.Kind == PayloadObjectKind.Sentinel && o.X > game.X && o.X - game.X < 1.3)) && Math.Abs(game.VelocityY) < .01)
                game.QueueJump();
            game.Advance(1.0 / 60, input);
        }
        [Test] public void PayloadPitRetryKeepsCheckpointAndPacketsAndDoesNotRepeatMilestones()
        {
            var game = new PayloadSession(); game.Start();
            for (int i=0;i<3600 && game.Checkpoint==0;i++) DrivePayload(game);
            Assert.That(game.Checkpoint,Is.EqualTo(1)); int packets=game.PacketMask;
            // Deliberately walk into the second gap without jumping.
            for(int i=0;i<600 && game.Deaths==0;i++) game.Advance(1.0/60,1);
            Assert.That(game.Deaths,Is.GreaterThan(0)); Assert.That(game.Checkpoint,Is.EqualTo(1));
            Assert.That(game.PacketMask & packets,Is.EqualTo(packets));
            var restored=PayloadSession.Restore(game.Capture()); Assert.That(restored.Deaths,Is.EqualTo(game.Deaths));
        }
        [Test] public void PayloadCampaignPersistsMilestonesMechanismClocksAndSeparateDesktop()
        {
            var campaign=new ComputerCampaign(); Assert.That(campaign.BeginPayload(),Is.False);
            campaign.BeginSnake(); ComputerCampaignTests.Complete(campaign.Snake);
            Assert.That(campaign.BeginPayload(), Is.False);
            SignalTests.CommissionSignalPath(campaign, 2);
            Assert.That(campaign.BeginPayload(), Is.False);
            SignalTests.CommissionSignalPath(campaign);
            int actionsBefore = campaign.Actions.Count, messagesBefore = campaign.Messages.Count;
            for(int i=0;i<24000 && campaign.Payload.Phase!=PayloadPhase.Complete;i++)
            { campaign.BeginPayload();DrivePayload(campaign.Payload);campaign.AdvanceWorld(1.0/60); }
            Assert.That(campaign.Payload.Stage,Is.EqualTo(3));
            campaign.SaveDesktop(new ComputerDesktopData { station=ComputerCampaign.PayloadStation, focusedApp="payload",
                windows=new[]{new DesktopWindowData{app="payload",x=275,y=90}} });
            var restored=ComputerCampaign.Restore(campaign.Capture());
            Assert.That(restored.Actions.Count,Is.EqualTo(actionsBefore + 5)); Assert.That(restored.Messages.Count,Is.EqualTo(messagesBefore + 1));
            Assert.That(restored.Logistics.LiftHeight,Is.EqualTo(campaign.Logistics.LiftHeight));
            Assert.That(restored.Logistics.ConveyorPhase,Is.EqualTo(campaign.Logistics.ConveyorPhase));
            Assert.That(restored.Desktop(ComputerCampaign.PayloadStation).windows[0].x,Is.EqualTo(275));
            Assert.That(restored.Home.TryRead("/home/operator/archive/payload_transfer.log",out _),Is.True);
            for(int i=0;i<60;i++){restored.BeginPayload();restored.AdvanceWorld(.1);}
            Assert.That(restored.Actions.Count,Is.EqualTo(actionsBefore + 5)); Assert.That(restored.Messages.Count,Is.EqualTo(messagesBefore + 1));
            var bad=restored.Capture();bad.logistics.lift=false;
            Assert.Throws<ArgumentException>(()=>ComputerCampaign.Restore(bad));
        }
        [Test] public void PayloadSessionStartedInAnOlderPreviewCanStillFinishWithoutCalibration()
        {
            var campaign = new ComputerCampaign();
            campaign.BeginSnake();
            ComputerCampaignTests.Complete(campaign.Snake);
            var old = campaign.Capture();
            var partial = new PayloadSession();
            partial.Start();
            partial.Advance(.2, 1);
            old.payload = partial.Capture();

            var restored = ComputerCampaign.Restore(old);
            Assert.That(restored.Signal.Stage, Is.Zero);
            Assert.That(restored.Payload.X, Is.EqualTo(partial.X));
            for (int frame = 0; frame < 24000 && restored.Payload.Phase != PayloadPhase.Complete; frame++)
            {
                Assert.That(restored.BeginPayload(), Is.True);
                DrivePayload(restored.Payload);
            }
            Assert.That(ComputerCampaign.Restore(restored.Capture()).Payload.Stage, Is.EqualTo(3));
        }
        [Test] public void PayloadRejectsNonfinitePoseAndCounterOrClockCorruption()
        {
            var data = new PayloadSession().Capture(); data.x = double.NaN;
            Assert.Throws<ArgumentException>(() => PayloadSession.Restore(data));
            data = new PayloadSession().Capture(); data.elapsed = 1;
            Assert.Throws<ArgumentException>(() => PayloadSession.Restore(data));
            data = new PayloadSession().Capture(); data.stage = 3;
            Assert.Throws<ArgumentException>(() => PayloadSession.Restore(data));
        }
    }
}
