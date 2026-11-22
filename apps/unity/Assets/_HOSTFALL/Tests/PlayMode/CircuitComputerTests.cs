using System;
using System.Collections;
using System.Linq;
using Hostfall.Core;
using Hostfall.Persistence;
using Hostfall.Player;
using Hostfall.Simulation.Computers;
using Hostfall.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object=UnityEngine.Object;

namespace Hostfall.Tests
{
    public sealed partial class ComputerWorldTests
    {
        private static void ReachEnergyComputer(ComputerCampaign campaign)
        {
            CompleteFirstComputer(campaign);campaign.BeginNetwork();
            campaign.Network.SetIsolation(5,true);campaign.Network.SetIsolation(6,true);campaign.Network.SetCpu(5,1);campaign.Network.SetCpu(6,1);
            for(int i=0;i<12000&&campaign.Network.Stage<3;i++)
            {if(campaign.Network.Phase is NetworkPhase.Ready or NetworkPhase.Failed)campaign.BeginNetwork();DriveNetwork(campaign.Network);}
            Assert.That(campaign.CircuitAccessible,Is.True);
        }
        private static void AlignCircuit(CircuitSession game)
        {
            for(int i=0;i<CircuitSession.Count;i++)if(CircuitSession.Rotatable(i))while(game.Rotation(i)!=0)game.Rotate(i);
            if(!game.SourceEnabled(0))game.ToggleSource(0);if(!game.SourceEnabled(1))game.ToggleSource(1);
        }
        private static void CircuitBranch(CircuitSession game,CircuitLoad load,bool closed)
        {int relay=CircuitSession.OutputCell(load)-2;if(game.RelayClosed(relay)!=closed)game.ToggleRelay(relay);}
        [UnityTest]public IEnumerator LegacyPowerSnapshotRestoresActualLightsShuttersAndSafeDoor()
        {
            yield return EnterGame();var campaign=Root.Simulation.ComputerCampaign;ReachEnergyComputer(campaign);
            foreach(var actor in Object.FindObjectsByType<WatcherController>(FindObjectsSortMode.None))actor.enabled=false;
            var mechanism=Object.FindAnyObjectByType<CircuitPowerController>();Assert.That(mechanism,Is.Not.Null);
            var motor=Object.FindAnyObjectByType<PlayerMotor>();motor.Restore(new PlayerStateData{position=new Vector3(-16.5f,.08f,34.1f),yaw=0});
            Desktop.Open(Station(ComputerCampaign.CircuitStation));yield return new WaitForSecondsRealtime(.7f);
            AlignCircuit(campaign.Circuit);
            foreach (var load in new[] { CircuitLoad.MainDoor, CircuitLoad.Lighting, CircuitLoad.Ventilation })
                CircuitBranch(campaign.Circuit, load, true);
            Assert.That(campaign.Circuit.Apply(), Is.True);
            Assert.That(campaign.Circuit.Energized,Is.True);Assert.That(mechanism.MainDoor.IsUnlocked,Is.True);Assert.That(mechanism.ShortcutDoor.IsUnlocked,Is.False);
            Assert.That(mechanism.RouteLights.All(l=>l.enabled),Is.True);yield return new WaitForSecondsRealtime(.4f);
            Assert.That(campaign.Circuit.VentOpening,Is.GreaterThan(0));Assert.That(mechanism.Louvers.All(l=>l.localEulerAngles.x>0),Is.True);
            Desktop.Close();
            yield return new WaitForSecondsRealtime(2.1f);
            var cc=motor.GetComponent<CharacterController>();cc.enabled=false;motor.transform.position=new Vector3(0,.08f,35.5f);cc.enabled=true;
            Physics.SyncTransforms();Walk(cc,new Vector3(0,0,40));Walk(cc,new Vector3(0,0,38));
            campaign.Circuit.Disconnect();yield return new WaitForSecondsRealtime(.2f);
            Assert.That(mechanism.MainDoor.IsUnlocked,Is.False);Assert.That(mechanism.MainDoor.Capture().opened,Is.True,"Power loss cannot close a gate through the real player");
            Walk(cc,new Vector3(0,0,35.5f));yield return new WaitForSecondsRealtime(2.1f);Assert.That(mechanism.MainDoor.Capture().opened,Is.False);
            campaign.Circuit.Apply();yield return new WaitForSecondsRealtime(.2f);Controls.SetMode(ControlMode.Paused);
            double vent=campaign.Circuit.VentOpening;Assert.That(Root.Persistence.Save("manual1"),Is.True);
            double loadedOpening=-1;float loadedAngle=-1;
            void ObserveLoaded(Scene scene,LoadSceneMode mode)
            {loadedOpening=Root.Simulation.ComputerCampaign.Circuit.VentOpening;loadedAngle=Object.FindAnyObjectByType<CircuitPowerController>().Louvers[0].localEulerAngles.x;}
            SceneManager.sceneLoaded+=ObserveLoaded;
            try{Root.Persistence.Load("manual1");yield return null;yield return new WaitForSecondsRealtime(.1f);}
            finally{SceneManager.sceneLoaded-=ObserveLoaded;}
            var loaded=Root.Simulation.ComputerCampaign;var rebound=Object.FindAnyObjectByType<CircuitPowerController>();
            Assert.That(loaded.Circuit.Powered(CircuitLoad.MainDoor),Is.True);Assert.That(rebound.MainDoor.IsUnlocked,Is.True);
            Assert.That(loadedOpening,Is.EqualTo(vent).Within(1e-12),"Observe before live world clocks legitimately advance");
            Assert.That(loadedAngle,Is.EqualTo((float)vent*82).Within(.001f));
            Assert.That(rebound.RouteLights.All(l=>l.enabled),Is.True);Assert.That(loaded.Home.TryRead(ComputerCampaign.CircuitIncidentPath,out _),Is.True);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest]public IEnumerator PoweredEnergyShortcutMakesAnActualRoundTripToSoc()
        {
            yield return EnterGame();var campaign=Root.Simulation.ComputerCampaign;ReachEnergyComputer(campaign);AlignCircuit(campaign.Circuit);
            foreach(var actor in Object.FindObjectsByType<WatcherController>(FindObjectsSortMode.None))actor.enabled=false;
            CircuitBranch(campaign.Circuit,CircuitLoad.MainDoor,true);CircuitBranch(campaign.Circuit,CircuitLoad.Shortcut,true);campaign.Circuit.ToggleRelay(23);campaign.Circuit.Apply();
            var power=Object.FindAnyObjectByType<CircuitPowerController>();Assert.That(power.ShortcutDoor.DoorId,Is.EqualTo("door.sec01.exec"));
            yield return new WaitForSecondsRealtime(2.1f);var cc=Object.FindAnyObjectByType<PlayerMotor>().GetComponent<CharacterController>();
            cc.enabled=false;cc.transform.position=new Vector3(-15.4f,.08f,19.5f);cc.enabled=true;Physics.SyncTransforms();
            Walk(cc,new Vector3(-15.4f,0,14));Walk(cc,new Vector3(-15.4f,0,-1.75f));Walk(cc,new Vector3(-3.5f,0,-1.75f));
            Walk(cc,new Vector3(-15.4f,0,-1.75f));Walk(cc,new Vector3(-15.4f,0,14));Walk(cc,new Vector3(-15.4f,0,19.5f));
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest]public IEnumerator CircuitShortcutDefenceAndOverloadAffectTheLiveWorld()
        {
            yield return EnterGame();var campaign=Root.Simulation.ComputerCampaign;ReachEnergyComputer(campaign);AlignCircuit(campaign.Circuit);
            var mechanism=Object.FindAnyObjectByType<CircuitPowerController>();var game=campaign.Circuit;
            foreach(var load in new[]{CircuitLoad.MainDoor,CircuitLoad.Shortcut,CircuitLoad.Turret})CircuitBranch(game,load,true);
            game.ToggleRelay(23);Assert.That(game.Apply(),Is.True);
            Assert.That(mechanism.ShortcutDoor.IsUnlocked,Is.True);Assert.That(mechanism.RouteLights.All(l=>!l.enabled),Is.True);
            var actors=Object.FindObjectsByType<WatcherController>(FindObjectsSortMode.None);var guard=actors.Single(a=>a.Id=="reactor-sentry-r22");
            foreach(var actor in actors)actor.enabled=false;guard.enabled=true;guard.transform.position=new Vector3(-11,0,34.85f);Physics.SyncTransforms();
            for(int i=0;i<10;i++)campaign.AdvanceWorld(.25);
            Assert.That(mechanism.LastPulseTargetId,Is.EqualTo(guard.Id));Assert.That(guard.IsStunned,Is.True,"Powered defence must stun an actual local guard");
            CircuitBranch(game,CircuitLoad.Lighting,true);Assert.That(game.Energized,Is.False,"Edits are break-before-make");
            Assert.That(game.Apply(),Is.False);Assert.That(game.Tripped,Is.True);Assert.That(mechanism.MainDoor.IsUnlocked||mechanism.ShortcutDoor.IsUnlocked,Is.False);
            Assert.That(mechanism.RouteLights.Any(l=>l.enabled),Is.False);Assert.That(game.ResetTrip(),Is.False);
            var snapshot=game.Capture();Assert.That(snapshot.turretClock,Is.Zero);
            CircuitBranch(game,CircuitLoad.Lighting,false);for(int i=0;i<8;i++)campaign.AdvanceWorld(.25);
            Assert.That(game.Energized,Is.False);Assert.That(game.ResetTrip(),Is.True);Assert.That(game.Apply(),Is.True);
            Assert.That(mechanism.ShortcutDoor.IsUnlocked,Is.True);Assert.That(game.Commissioned,Is.True);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
