using System;
using System.Collections;
using System.Linq;
using Hostfall.Core;
using Hostfall.Devices;
using Hostfall.Persistence;
using Hostfall.Player;
using Hostfall.Simulation;
using Hostfall.Simulation.Computers;
using Hostfall.World;
using Hostfall.UI;
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
        [UnityTest]public IEnumerator LegacyNetworkSnapshotResumesViaCaseAndOpensRealVaultAndEnergyDoors()
        {
            yield return EnterGame();var campaign=Root.Simulation.ComputerCampaign;CompleteFirstComputer(campaign);
            foreach(var robot in Object.FindObjectsByType<WatcherController>(FindObjectsSortMode.None))robot.enabled=false;
            var motor=Object.FindAnyObjectByType<PlayerMotor>();motor.Restore(new PlayerStateData{position=new Vector3(0,.08f,11.4f),yaw=0});
            Desktop.Open(Station(ComputerCampaign.NetworkStation));yield return new WaitForSecondsRealtime(.8f);
            foreach (int node in new[] { 5, 6 })
            {
                campaign.Network.SetCpu(node, 1);
                campaign.Network.SetIsolation(node, true);
            }
            Assert.That(campaign.Network.UsedCpu, Is.EqualTo(4));
            Assert.That(campaign.BeginNetwork(), Is.True);
            for (int i = 0; i < 160 && !campaign.Network.Processes.Any(p => p.node == 5); i++)
                campaign.Network.Advance(1.0 / 30);
            int before = campaign.Network.Blocked;
            Assert.That(campaign.Network.Purge(5), Is.True);
            Assert.That(campaign.Network.Blocked, Is.GreaterThan(before));
            Assert.That(Root.Persistence.Save("manual1"),Is.True);
            var library=new SaveLibrary(WorldSaveController.TestStorageDirectory,null,true);
            Assert.That(library.Read("manual1",out var saved,out var error),Is.True,error);
            var restored=ComputerCampaign.Restore(saved.simulation.computerCampaign);
            Assert.That(restored.Network.Processes.Select(p=>p.node),Is.EqualTo(campaign.Network.Processes.Select(p=>p.node)));
            var old=campaign.Network.Capture();Desktop.Close();yield return new WaitForSecondsRealtime(.1f);
            Assert.That(campaign.Network.Capture().waveTime,Is.EqualTo(old.waveTime),"Leaving desktop freezes only its game");
            Desktop.Open(Station(ComputerCampaign.NetworkStation));yield return new WaitForSecondsRealtime(.6f);
            yield return SolveFieldCase(1);
            Assert.That(campaign.Network.Stage, Is.Zero);
            Assert.That(campaign.CameraIsolated, Is.True);
            Desktop.Close();var doors=Object.FindObjectsByType<FacilityDoor>(FindObjectsSortMode.None);
            var vault=doors.Single(d=>d.DoorId=="door.sec02.vault");Assert.That(vault.IsUnlocked,Is.True);
            Assert.That(doors.Single(d=>d.DoorId=="door.sec03.energy").IsUnlocked,Is.True);
            if(!vault.Capture().opened)vault.Interact();yield return new WaitForSecondsRealtime(2.2f);
            var cc=motor.GetComponent<CharacterController>();
            Walk(cc,new Vector3(1.8f,0,11.3f));Walk(cc,new Vector3(1.8f,0,14.1f));Walk(cc,new Vector3(0,0,14.1f));
            Walk(cc,new Vector3(0,0,17));Walk(cc,new Vector3(0,0,14.1f));Walk(cc,new Vector3(1.8f,0,14.1f));Walk(cc,new Vector3(1.8f,0,11.3f));
            Assert.That(campaign.Home.TryRead(ComputerCampaign.NetworkIncidentPath,out _),Is.True);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest]public IEnumerator NetworkWorldRelaysAlertsCutsCoordinationAndPreservesReleasedReserve()
        {
            yield return EnterGame();var campaign=Root.Simulation.ComputerCampaign;CompleteFirstComputer(campaign);
            var controller=Object.FindAnyObjectByType<ServerNetworkController>();Assert.That(controller,Is.Not.Null);
            var actors=Object.FindObjectsByType<WatcherController>(FindObjectsSortMode.None);
            var observer=actors.Single(a=>a.Id=="sentry-r18");var receiver=actors.Single(a=>a.Id=="watcher-r17");
            observer.HearShot(new Vector3(0,0,11));controller.RelaySightings();Assert.That(receiver.HasNetworkTarget,Is.True);
            campaign.Network.SetIsolation(5,true);controller.RefreshState();Assert.That(receiver.HasNetworkTarget,Is.False);
            observer.HearShot(new Vector3(0,0,11));controller.RelaySightings();Assert.That(receiver.HasNetworkTarget,Is.False,"Isolated command cannot relay sightings");
            Assert.That(observer.Suspicion,Is.EqualTo(1),"Local hearing remains autonomous");
            Assert.That(controller.Responders.All(r=>!r.enabled),Is.True);
            campaign.BeginNetwork();for(int i=0;i<48;i++)campaign.AdvanceWorld(.25);
            Assert.That(campaign.NetworkSecurity.Released,Is.EqualTo(1));Assert.That(controller.Responders[0].enabled,Is.True);
            Assert.That(controller.Responders[1].enabled,Is.False);
            // Use the same actual strategy controls; no completion/reward setters.
            campaign.Network.SetCpu(5,1);campaign.Network.SetIsolation(6,true);campaign.Network.SetCpu(6,1);
            for(int i=0;i<12000&&campaign.Network.Stage<2;i++)
            { if(campaign.Network.Phase is NetworkPhase.Ready or NetworkPhase.Failed)campaign.BeginNetwork();DriveNetwork(campaign.Network); }
            Assert.That(campaign.Network.Stage,Is.EqualTo(2));observer.Tick(0);
            Assert.That(observer.NetworkDisconnected,Is.True);Assert.That(observer.State.Mode,Is.EqualTo(WatcherMode.Jammed));
            var position=observer.transform.position;observer.Tick(.2f);Assert.That(observer.transform.position,Is.EqualTo(position));
            var animation=observer.GetComponentInChildren<Animator>();Assert.That(animation.speed,Is.Zero);
            observer.TakeDamage(10000);observer.Tick(0);
            Assert.That(observer.IsDestroyed,Is.True);Assert.That(observer.AnimationState,Is.EqualTo("TurnOff"));
            Assert.That(animation.speed,Is.EqualTo(1),"A robot killed during an outage must play death, not remain frozen");
            for(int i=0;i<60;i++)campaign.AdvanceWorld(.25);
            Assert.That(campaign.NetworkSecurity.Released,Is.EqualTo(1),"Stage1 cancels the pending second reserve");
            observer.Tick(0);Assert.That(observer.NetworkDisconnected,Is.False);Assert.That(observer.State.Mode,Is.Not.EqualTo(WatcherMode.Jammed));
            Assert.That(Root.Persistence.Save("manual1"),Is.True,"New actor IDs must be accepted only in v3");
            var cc=Object.FindAnyObjectByType<PlayerMotor>().GetComponent<CharacterController>();
            cc.enabled=false;cc.transform.position=new Vector3(-1.85f,1.23f,13.5f);cc.enabled=true;
            Walk(cc,new Vector3(-1.85f,1.2f,14));Walk(cc,new Vector3(-1.85f,1.2f,13.5f));
            var library=new SaveLibrary(WorldSaveController.TestStorageDirectory,null,true);
            Assert.That(library.Read("manual1",out var snapshot,out var loadError),Is.True,loadError);
            var reservePosition=snapshot.enemies.Single(e=>e.id=="network-response-r31").position;
            Vector3 loadedPose=default;bool observed=false;
            void ObserveLoadedPose(Scene scene,LoadSceneMode mode)
            {loadedPose=Object.FindAnyObjectByType<ServerNetworkController>().Responders[0].transform.position;observed=true;}
            SceneManager.sceneLoaded+=ObserveLoadedPose;
            try {Root.Persistence.Load("manual1");yield return null;yield return new WaitForSecondsRealtime(.15f);}
            finally {SceneManager.sceneLoaded-=ObserveLoadedPose;}
            var loaded=Root.Simulation.ComputerCampaign;var rebound=Object.FindAnyObjectByType<ServerNetworkController>();
            Assert.That(loaded.Network.Stage,Is.EqualTo(2));Assert.That(loaded.NetworkSecurity.Released,Is.EqualTo(1));
            Assert.That(loaded.NetworkSecurity.OutageRemaining,Is.Zero,"Reload cannot replay a consumed outage reward");
            Assert.That(rebound.Responders[0].enabled,Is.True);Assert.That(rebound.Responders[1].enabled,Is.False);
            Assert.That(observed,Is.True);Assert.That(Vector3.Distance(loadedPose,reservePosition),Is.LessThan(.0001f),"Observe saved root pose at sceneLoaded before resumed AI can legitimately patrol");
            LogAssert.NoUnexpectedReceived();
        }
        private static void CompleteFirstComputer(ComputerCampaign campaign)
        { campaign.BeginSnake();for(int i=0;i<2500&&!campaign.FirstTaskComplete;i++)Steer(campaign.Snake);Assert.That(campaign.FirstTaskComplete,Is.True); }
        private static void DriveNetwork(NetworkDefenseSession game)
        {
            int target=game.Infection(5)>=game.Infection(6)?5:6;
            if(game.Infection(target)>30||game.Processes.Count(p=>p.node==target)>=2)game.Purge(target);
            game.Advance(1.0/30);
        }
    }
}
