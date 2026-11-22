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
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object=UnityEngine.Object;
namespace Hostfall.Tests
{
    public sealed partial class ComputerWorldTests
    {
        private static void ReachSignalComputer(ComputerCampaign campaign,bool ventilation=true)
        {
            ReachEnergyComputer(campaign);AlignCircuit(campaign.Circuit);CircuitBranch(campaign.Circuit,CircuitLoad.MainDoor,true);
            CircuitBranch(campaign.Circuit,CircuitLoad.Ventilation,ventilation);campaign.Circuit.Apply();Assert.That(campaign.SignalAccessible,Is.True);
        }
        private static void TuneSignals(SignalSession game)
        {
            for(int i=0;i<3;i++)
            {game.SetEnabled(i,true);game.SetFrequency(i,game.ReferenceUnits);game.SetTrim(i,0);game.SetInverted(i,false);if(Math.Abs(game.PhaseError(i))>90)game.SetInverted(i,true);game.SetTrim(i,(int)Math.Round(-game.PhaseError(i)));}
        }
        private static void CalibrateSignals(ComputerCampaign campaign)
        {
            for(int goal=campaign.Signal.Stage+1;goal<=3;goal++)
            {Assert.That(campaign.BeginSignal(),Is.True);TuneSignals(campaign.Signal);for(int i=0;i<100&&campaign.Signal.Phase==SignalPhase.Running;i++)campaign.Signal.Advance(.1);Assert.That(campaign.Signal.Stage,Is.EqualTo(goal));}
        }
        private static void CalibrateLogisticsEntry(ComputerCampaign campaign)
        {
            if (campaign.Signal.Stage == 3) return;
            ReachSignalComputer(campaign);
            CalibrateSignals(campaign);
        }
        [UnityTest]public IEnumerator PhaseUtilityReopensPhysicalFieldAndRestoresTheRemainingWindow()
        {
            yield return EnterGame();var campaign=Root.Simulation.ComputerCampaign;ReachSignalComputer(campaign);
            foreach(var actor in Object.FindObjectsByType<WatcherController>(FindObjectsSortMode.None))actor.enabled=false;
            var motor=Object.FindAnyObjectByType<PlayerMotor>();motor.Restore(new PlayerStateData{position=new Vector3(0,.33f,48.4f),yaw=0});
            Desktop.Open(Station(ComputerCampaign.SignalStation));yield return new WaitForSecondsRealtime(.7f);
            yield return RunFieldUtility(FieldUtilityId.PhaseBypass);
            Assert.That(campaign.PhaseCalibrated, Is.True);
            yield return RunFieldUtility(FieldUtilityId.PhaseBypass);
            Assert.That(campaign.Actions.Count(action => action.kind == WorldActionKind.SignalControl), Is.EqualTo(3));
            var field=Object.FindAnyObjectByType<SignalBarrierController>();Assert.That(field.IsBlocking,Is.False);Assert.That(field.Door.IsUnlocked,Is.True);
            Desktop.Close();double before=campaign.SignalPlant.BarrierWindow;yield return new WaitForSecondsRealtime(.12f);Assert.That(campaign.SignalPlant.BarrierWindow,Is.LessThan(before));
            Controls.SetMode(ControlMode.Paused);double expected=campaign.SignalPlant.BarrierWindow;Assert.That(Root.Persistence.Save("manual1"),Is.True);
            double observed=-1;bool blocked=true;
            void Loaded(Scene scene,LoadSceneMode mode){observed=Root.Simulation.ComputerCampaign.SignalPlant.BarrierWindow;blocked=Object.FindAnyObjectByType<SignalBarrierController>().IsBlocking;}
            SceneManager.sceneLoaded+=Loaded;try{Root.Persistence.Load("manual1");yield return null;yield return new WaitForSecondsRealtime(.1f);}finally{SceneManager.sceneLoaded-=Loaded;}
            Assert.That(observed,Is.EqualTo(expected).Within(1e-12));Assert.That(blocked,Is.False);Assert.That(Root.Simulation.ComputerCampaign.PhaseCalibrated,Is.True);
            Assert.That(Root.Simulation.ComputerCampaign.Home.TryRead(ComputerCampaign.SignalIncidentPath,out _),Is.True);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest]public IEnumerator SignalFieldCannotBeBypassedAndHoldsOccupiedClosureThenAllowsNorthReturn()
        {
            yield return EnterGame();var campaign=Root.Simulation.ComputerCampaign;ReachSignalComputer(campaign);
            foreach(var actor in Object.FindObjectsByType<WatcherController>(FindObjectsSortMode.None))actor.enabled=false;
            var field=Object.FindAnyObjectByType<SignalBarrierController>();var cc=Object.FindAnyObjectByType<PlayerMotor>().GetComponent<CharacterController>();
            cc.enabled=false;cc.transform.position=new Vector3(0,.33f,60.6f);cc.enabled=true;Physics.SyncTransforms();
            for(int i=0;i<100;i++)cc.Move(Vector3.forward*.04f+Vector3.down*.03f);field.RefreshState();yield return null;
            Assert.That(cc.transform.position.z,Is.LessThan(61.5));Assert.That(field.IsBlocking,Is.True,"Approaching a closed field cannot trigger the occupied-open safeguard");
            field.Interact();Assert.That(campaign.SignalPlant.ReturnWindow,Is.Zero,"North release is inaccessible from south");
            Assert.That(Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Single(r=>r.sharedMaterial!=null&&r.sharedMaterial.shader.name=="HOSTFALL/Energy Field").sharedMaterial.shader.isSupported,Is.True);
            CalibrateSignals(campaign);yield return new WaitForSecondsRealtime(2.3f);Walk(cc,new Vector3(0,0,63.5f));Walk(cc,new Vector3(0,0,61.62f));
            for(int i=0;i<100;i++)campaign.AdvanceWorld(.25);field.RefreshState();Assert.That(field.SafetyHeld,Is.True);Assert.That(field.IsBlocking,Is.False);
            Assert.That(Root.Persistence.Save("manual1"),Is.True);bool loadedSafety=false;
            void Loaded(Scene scene,LoadSceneMode mode){loadedSafety=Object.FindAnyObjectByType<SignalBarrierController>().SafetyHeld;}
            SceneManager.sceneLoaded+=Loaded;try{Root.Persistence.Load("manual1");yield return null;yield return new WaitForSecondsRealtime(.05f);}finally{SceneManager.sceneLoaded-=Loaded;}
            Assert.That(loadedSafety,Is.True,"Expired window plus saved occupied door must restore without crushing the player");
            campaign=Root.Simulation.ComputerCampaign;field=Object.FindAnyObjectByType<SignalBarrierController>();cc=Object.FindAnyObjectByType<PlayerMotor>().GetComponent<CharacterController>();
            Walk(cc,new Vector3(0,0,63.5f));Physics.SyncTransforms();field.RefreshState();
            var volume=field.GetComponentsInChildren<BoxCollider>().Single(c=>c.name.StartsWith("Visible energy field"));
            var half=Vector3.Scale(volume.size*.5f,volume.transform.lossyScale);half.z=Mathf.Max(half.z,.45f);
            string overlap=string.Join(";",Physics.OverlapBox(volume.transform.TransformPoint(volume.center),half,volume.transform.rotation,CollisionLayers.PlayerMask,QueryTriggerInteraction.Ignore).Select(c=>c.name+":"+c.bounds));
            Assert.That(field.IsBlocking,Is.True,"pose="+cc.transform.position+" safety="+field.SafetyHeld+" window="+campaign.SignalPlant.BarrierWindow+" return="+campaign.SignalPlant.ReturnWindow+" volume="+volume.transform.position+" half="+half+" rotation="+volume.transform.eulerAngles+" hits="+overlap);Walk(cc,new Vector3(1.5f,0,63.3f));Physics.SyncTransforms();
            var origin=cc.transform.position+Vector3.up*1.4f;var point=new Vector3(2.7f,1.25f,62.47f);
            Assert.That(Physics.Raycast(origin,(point-origin).normalized,out var hit,2.5f,CollisionLayers.InteractionMask,QueryTriggerInteraction.Ignore),Is.True);
            Assert.That(hit.collider.GetComponentInParent<SignalBarrierController>(),Is.SameAs(field),"The north release has a real reachable actuator");
            field.Interact();Assert.That(campaign.SignalPlant.ReturnWindow,Is.EqualTo(8));Assert.That(field.IsBlocking,Is.False);
            yield return new WaitForSecondsRealtime(2.3f);Walk(cc,new Vector3(0,0,63.3f));Walk(cc,new Vector3(0,0,60.6f));
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest]public IEnumerator SignalHeatContinuesAwayFromAppAndTheRealGuardApproaches()
        {
            yield return EnterGame();var campaign=Root.Simulation.ComputerCampaign;ReachSignalComputer(campaign,false);
            var actors=Object.FindObjectsByType<WatcherController>(FindObjectsSortMode.None);var guard=actors.Single(a=>a.Id=="quantum-watcher-r23");
            foreach(var actor in actors)actor.enabled=false;guard.enabled=true;
            var motor=Object.FindAnyObjectByType<PlayerMotor>();motor.Restore(new PlayerStateData{position=new Vector3(0,.33f,48.4f),yaw=0});float distance=Vector3.Distance(guard.transform.position,motor.transform.position);
            Assert.That(campaign.BeginSignal(),Is.True);Assert.That(guard.Suspicion,Is.EqualTo(1));
            Assert.That(Vector3.Distance(guard.LastKnownPosition,Station(ComputerCampaign.SignalStation).transform.position),Is.LessThan(.01f));
            yield return new WaitForSecondsRealtime(.65f);Assert.That(Vector3.Distance(guard.transform.position,motor.transform.position),Is.LessThan(distance),"An existing quarantine actor really moves toward the live station");
            double appClock=campaign.Signal.RunningTime;
            Assert.That(campaign.Circuit.Powered(CircuitLoad.Ventilation),Is.False);
            for(int i=0;i<800&&campaign.SignalPlant.Recharge==0;i++)campaign.AdvanceWorld(.25);
            Assert.That(campaign.Signal.Phase,Is.EqualTo(SignalPhase.Failed));Assert.That(campaign.Signal.RunningTime,Is.EqualTo(appClock));Assert.That(campaign.SignalPlant.Recharge,Is.GreaterThan(0));
            campaign.AdvanceWorld(.137);double recharge=campaign.SignalPlant.Recharge;Assert.That(Root.Persistence.Save("manual1"),Is.True);
            var library=new SaveLibrary(WorldSaveController.TestStorageDirectory,null,true);Assert.That(library.Read("manual1",out var snapshot,out var error),Is.True,error);
            var copy=ComputerCampaign.Restore(snapshot.simulation.computerCampaign);Assert.That(copy.SignalPlant.Recharge,Is.EqualTo(recharge).Within(1e-12));Assert.That(copy.BeginSignal(),Is.False);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
