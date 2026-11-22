using System;
using Hostfall.Simulation.Computers;
using NUnit.Framework;

namespace Hostfall.Tests
{
    public sealed class CircuitTests
    {
        [Test] public void SafeAndShortcutConfigurationsHaveDifferentRealPowerBudgets()
        {
            var game=Aligned();SetOutput(game,CircuitLoad.MainDoor,true);SetOutput(game,CircuitLoad.Lighting,true);SetOutput(game,CircuitLoad.Ventilation,true);
            Assert.That(game.Apply(),Is.True);Assert.That(game.Powered(CircuitLoad.MainDoor),Is.True);Assert.That(game.Powered(CircuitLoad.Lighting),Is.True);
            Assert.That(game.Powered(CircuitLoad.Ventilation),Is.True);Assert.That(game.Powered(CircuitLoad.Shortcut),Is.False);
            Assert.That(game.ConnectedCapacity(CircuitSession.OutputCell(CircuitLoad.MainDoor)),Is.EqualTo(6));
            Assert.That(game.ConnectedCapacity(CircuitSession.OutputCell(CircuitLoad.Ventilation)),Is.EqualTo(3));
            SetOutput(game,CircuitLoad.Lighting,false);SetOutput(game,CircuitLoad.Ventilation,false);SetOutput(game,CircuitLoad.Turret,true);SetOutput(game,CircuitLoad.Shortcut,true);
            game.ToggleRelay(3*7+2);Assert.That(game.Apply(),Is.True);
            Assert.That(game.Powered(CircuitLoad.MainDoor)&&game.Powered(CircuitLoad.Shortcut)&&game.Powered(CircuitLoad.Turret),Is.True);
            Assert.That(game.Powered(CircuitLoad.Lighting)||game.Powered(CircuitLoad.Ventilation),Is.False);
            Assert.That(game.ConnectedDemand(CircuitSession.OutputCell(CircuitLoad.Shortcut)),Is.EqualTo(9));
        }
        [Test] public void OverloadCutsPowerAndBreakerNeedsCoolingAndAnExplicitReset()
        {
            var game=Aligned();Assert.That(game.ToggleRelay(0),Is.False);game.ToggleRelay(3*7+2);
            foreach(CircuitLoad load in Enum.GetValues(typeof(CircuitLoad)))SetOutput(game,load,true);
            Assert.That(game.Apply(),Is.False);Assert.That(game.Tripped,Is.True);Assert.That(game.Cooldown,Is.EqualTo(2));
            foreach(CircuitLoad load in Enum.GetValues(typeof(CircuitLoad)))Assert.That(game.Powered(load),Is.False);
            Assert.That(game.ResetTrip(),Is.False);SetOutput(game,CircuitLoad.Shortcut,false);
            for(int i=0;i<8;i++)game.AdvanceWorld(.25);
            Assert.That(game.Tripped,Is.True);Assert.That(game.Apply(),Is.False,"Cooling alone must not reset the breaker");
            Assert.That(game.ResetTrip(),Is.True);Assert.That(game.Energized,Is.False);Assert.That(game.Apply(),Is.True);
        }
        [Test] public void ReciprocalContactsAndClosedRelaysAreRequiredAndEditsDisconnectImmediately()
        {
            var game=Aligned();SetOutput(game,CircuitLoad.MainDoor,true);int path=3;game.Rotate(path);
            Assert.That(game.Apply(),Is.True);Assert.That(game.Powered(CircuitLoad.MainDoor),Is.False);
            game.Rotate(path);Assert.That(game.Energized,Is.False);Assert.That(game.Apply(),Is.True);Assert.That(game.Powered(CircuitLoad.MainDoor),Is.True);
            game.ToggleSource(0);Assert.That(game.Energized,Is.False);game.Apply();Assert.That(game.Powered(CircuitLoad.MainDoor),Is.False);
            Assert.That(game.Rotate(CircuitSession.OutputCell(CircuitLoad.MainDoor)),Is.False);
            Assert.That(game.Rotate(-1),Is.False);Assert.That(game.ToggleRelay(0),Is.False);
        }
        [Test] public void PartialTripAndRotationsRestoreExactlyWithoutAliasingOrRepeatingCommission()
        {
            var a=Aligned();int rewards=0;a.CommissionedOnce+=()=>rewards++;SetOutput(a,CircuitLoad.MainDoor,true);a.Apply();a.Apply();
            Assert.That(rewards,Is.EqualTo(1));Assert.That(a.ToggleRelay(0),Is.False);
            a.ToggleRelay(3*7+2);foreach(CircuitLoad load in Enum.GetValues(typeof(CircuitLoad)))SetOutput(a,load,true);
            a.Apply();a.AdvanceWorld(.1234567);var data=a.Capture();var b=CircuitSession.Restore(data);data.rotations[3]=1;
            Assert.That(b.Rotation(3),Is.EqualTo(a.Rotation(3)));Assert.That(b.Cooldown,Is.EqualTo(a.Cooldown));
            for(int i=0;i<12;i++){a.AdvanceWorld(.17);b.AdvanceWorld(.17);Assert.That(b.Cooldown,Is.EqualTo(a.Cooldown));}
            b.CommissionedOnce+=()=>rewards++;SetOutput(b,CircuitLoad.Shortcut,false);b.ResetTrip();b.Apply();Assert.That(rewards,Is.EqualTo(1));
        }
        [Test] public void CircuitRejectsImpossibleStatesAndNeverActivatesItself()
        {
            var game=Aligned();SetOutput(game,CircuitLoad.MainDoor,true);for(int i=0;i<100;i++)game.AdvanceWorld(.25);
            Assert.That(game.Energized||game.Commissioned,Is.False);
            var data=game.Capture();data.rotations[0]=1;Assert.Throws<ArgumentException>(()=>CircuitSession.Restore(data));
            data=game.Capture();data.relays[0]=true;Assert.Throws<ArgumentException>(()=>CircuitSession.Restore(data));
            data=game.Capture();data.cooldown=double.NaN;Assert.Throws<ArgumentException>(()=>CircuitSession.Restore(data));
            data=game.Capture();data.energized=true;Assert.Throws<ArgumentException>(()=>CircuitSession.Restore(data),"Door power requires earned commission");
        }
        [Test] public void TurretWorldCadenceAndVentTravelSurviveLeavingAndRestoring()
        {
            var a=Aligned();SetOutput(a,CircuitLoad.Turret,true);SetOutput(a,CircuitLoad.Ventilation,true);a.Apply();a.AdvanceWorld(.23);
            var b=CircuitSession.Restore(a.Capture());int pulses=0;b.TurretPulseRequested+=()=>pulses++;
            for(int i=0;i<11;i++){a.AdvanceWorld(.23);b.AdvanceWorld(.23);}
            Assert.That(pulses,Is.EqualTo(1));Assert.That(b.Capture().turretClock,Is.EqualTo(a.Capture().turretClock));
            Assert.That(b.VentOpening,Is.EqualTo(1));b.Disconnect();Assert.That(b.Capture().turretClock,Is.Zero);
            for(int i=0;i<8;i++)b.AdvanceWorld(.25);Assert.That(b.VentOpening,Is.Zero);Assert.That(pulses,Is.EqualTo(1));
        }
        [Test] public void CircuitCampaignRewardsOnceAndKeepsPowerConfigurationAndArchiveAcrossRestore()
        {
            var campaign=new ComputerCampaign();Assert.That(campaign.CircuitAccessible,Is.False);campaign.BeginSnake();ComputerCampaignTests.Complete(campaign.Snake);
            campaign.BeginNetwork();NetworkDefenseTests.Defend(campaign.Network,3);Assert.That(campaign.CircuitAccessible,Is.True);
            Align(campaign.Circuit);SetOutput(campaign.Circuit,CircuitLoad.MainDoor,true);SetOutput(campaign.Circuit,CircuitLoad.Lighting,true);campaign.Circuit.Apply();
            int count=campaign.Actions.Count;campaign.Circuit.Disconnect();campaign.Circuit.Apply();Assert.That(campaign.Actions.Count,Is.EqualTo(count));
            var restored=ComputerCampaign.Restore(campaign.Capture());Assert.That(restored.Circuit.Powered(CircuitLoad.MainDoor),Is.True);
            Assert.That(restored.Home.TryRead(ComputerCampaign.CircuitIncidentPath,out _),Is.True);Assert.That(restored.Circuit.Commissioned,Is.True);
            var bad=campaign.Capture();bad.circuit.commissioned=false;Assert.Throws<ArgumentException>(()=>ComputerCampaign.Restore(bad));
            var old=new ComputerCampaign().Capture();old.circuit=null;Assert.That(ComputerCampaign.Restore(old).Circuit.Started,Is.False);
        }
        internal static CircuitSession Aligned()
        {
            var game=new CircuitSession();Align(game);return game;
        }
        internal static void Align(CircuitSession game)
        {
            for(int i=0;i<CircuitSession.Count;i++)if(CircuitSession.Rotatable(i))while(game.Rotation(i)!=0)game.Rotate(i);
            if(!game.SourceEnabled(0))game.ToggleSource(0);if(!game.SourceEnabled(1))game.ToggleSource(1);
        }
        internal static void SetOutput(CircuitSession game,CircuitLoad load,bool enabled)
        {int relay=CircuitSession.OutputCell(load)-2;if(game.RelayClosed(relay)!=enabled)game.ToggleRelay(relay);}
    }
}
