#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hostfall.Core;
using Hostfall.Devices;
using Hostfall.Persistence;
using Hostfall.Player;
using Hostfall.Simulation;
using Hostfall.Simulation.Computers;
using Hostfall.UI;
using UnityEngine;
using UnityEngine.UIElements;
using Object=UnityEngine.Object;

namespace Hostfall.World
{
    // Explicit diagnostic driver, compiled out of release. Never runs in ordinary gameplay.
    public static class ComputerWorldReview
    {
        private static string output;
        private static FacilityRoot root;
        private static ComputerRuntimeUI desktop;
        private static ControlState controls;
        private static PlayerMotor motor;
        private static CharacterController cc;
        private static readonly List<string> steps=new();
        public static IEnumerator Run(string directory)
        {
            output=directory;steps.Clear();
            Require(Application.identifier=="com.northline.hostfall.computerqa","QA bundle identity");
            Require(Path.GetFullPath(WorldSaveController.TestStorageDirectory)==Path.GetFullPath(Path.Combine(output,"private-saves")),"Private storage before scene load");
            root=Object.FindAnyObjectByType<FacilityRoot>();desktop=Object.FindAnyObjectByType<ComputerRuntimeUI>();controls=Object.FindAnyObjectByType<ControlState>();motor=Object.FindAnyObjectByType<PlayerMotor>();cc=motor.GetComponent<CharacterController>();
            Require(root.Simulation.InteractiveComputers,"Interactive mission");
            var ui=Object.FindAnyObjectByType<FacilityUI>();ui.BeginSession();
            yield return new WaitForSecondsRealtime(2);yield return Capture("home-awakening");ui.Opening.Skip();root.Field.AcknowledgeIntroduction();controls.SetMode(ControlMode.Exploring);motor.enabled=false;
            desktop.Open(Station(ComputerCampaign.HomeStation));yield return new WaitForSecondsRealtime(.8f);
            yield return Capture("home-terminal");
            yield return Click(desktop.HomeControls.Q<Button>("home-knowledge"));yield return Capture("home-knowledge");
            yield return Click(desktop.HomeControls.Q<Button>("home-knowledge"));
            yield return new WaitForSecondsRealtime(.08f);
            var input=desktop.PhysicalDisplay.View.Q<TextField>("home-command");input.value="cat ~/archive/evacuation.txt";
            using(var key=KeyDownEvent.GetPooled('\n',KeyCode.Return,EventModifiers.None))input.SendEvent(key);
            Require(root.Simulation.ComputerCampaign.HasRead("/home/operator/archive/evacuation.txt"),"Home document via actual input");
            desktop.Close();yield return new WaitForSecondsRealtime(.5f);
            yield return Walk(new Vector3(1.7f,0,-14.5f));
            var pickup=Object.FindObjectsByType<PulseSidearmPickup>(FindObjectsSortMode.None).Single(p=>p.name=="Shelter / CIRRUS emergency sidearm");pickup.Interact();
            Require(motor.GetComponent<PlayerWeapon>().HasWeapon,"Emergency pistol pickup");
            yield return Walk(new Vector3(0,0,-14));yield return Walk(new Vector3(0,0,-10));
            Door("door.shelter.blast").Interact();yield return new WaitForSecondsRealtime(2.1f);
            yield return Walk(new Vector3(0,0,-5));yield return Walk(new Vector3(-2.65f,0,-2.3f));
            Aim(Station(ComputerCampaign.SnakeStation).Surface.Centre);yield return new WaitForSecondsRealtime(.5f);yield return Capture("office-near");
            desktop.Open(Station(ComputerCampaign.SnakeStation));yield return new WaitForSecondsRealtime(.8f);
            yield return Click(desktop.PhysicalDisplay.View.Q<Button>("desktop-snake"));yield return Click(desktop.PhysicalDisplay.View.Q<Button>("snake-start"));
            yield return new WaitForSecondsRealtime(.3f);yield return Capture("ventflow-focused");
            Require(controls.WorldRuns&&!controls.CanExplore,"Live computer mode with player input held");
            Require(root.Persistence.Save("manual1"),"Partial application save");
            var library=new SaveLibrary(WorldSaveController.TestStorageDirectory,null,true);
            Require(library.Read("manual1",out var saved,out var error),"Partial save can be read: "+error);
            string actual=JsonUtility.ToJson(saved.simulation.computerCampaign),expected=JsonUtility.ToJson(root.Simulation.ComputerCampaign.Capture());
            File.WriteAllText(Path.Combine(output,"partial-saved.json"),actual);File.WriteAllText(Path.Combine(output,"partial-current.json"),expected);
            var live=root.Simulation.ComputerCampaign.Capture();var read=saved.simulation.computerCampaign;
            Require(Math.Abs(read.snake.elapsed-live.snake.elapsed)<1e-12&&Math.Abs(read.threat.elapsed-live.threat.elapsed)<1e-12&&Math.Abs(read.networkSecurity.relayClock-live.networkSecurity.relayClock)<1e-12,"Partial clocks restored within 1ps JSON tolerance");
            read.snake.elapsed=live.snake.elapsed;read.threat.elapsed=live.threat.elapsed;read.networkSecurity.relayClock=live.networkSecurity.relayClock;
            Require(JsonUtility.ToJson(read)==JsonUtility.ToJson(live),"Exact partial non-clock save state");
            desktop.Close();yield return new WaitForSecondsRealtime(.5f);
            var vent=Object.FindAnyObjectByType<VentReleaseController>();
            float deadline=Time.realtimeSinceStartup+12;
            while(Time.realtimeSinceStartup<deadline&&root.Simulation.ComputerCampaign.ThreatPhase!=VentThreatPhase.Released)yield return null;
            Require(root.Simulation.ComputerCampaign.ThreatPhase==VentThreatPhase.Released&&vent.Actor.enabled,"Visible vent release while away from PC");
            yield return Capture("vent-emerged");
            var weapon=motor.GetComponent<PlayerWeapon>();int fired=0;
            for(int i=0;i<45&&!vent.Actor.IsDestroyed;i++)
            {
                Aim(vent.Actor.transform.position+Vector3.up*.75f);
                if(i==0)Object.FindAnyObjectByType<FacilityActionController>().TryPulse();
                if(weapon.TryFire())fired++;
                yield return new WaitForSeconds(.28f);
                Require(!motor.GetComponent<PlayerHealth>().IsDestroyed,"Survived readable first ambush");
            }
            Require(vent.Actor.IsDestroyed&&fired>2,"Physical pistol shots defeat released robot");yield return Capture("ambush-aftermath");
            Aim(Station(ComputerCampaign.SnakeStation).Surface.Centre);desktop.Open(Station(ComputerCampaign.SnakeStation));yield return new WaitForSecondsRealtime(.6f);
            // Steer real game rules in bounded ticks. No score, receipt or door setter is called here.
            var game=root.Simulation.ComputerCampaign.Snake;
            if(game.Phase!=SnakePhase.Running)root.Simulation.ComputerCampaign.BeginSnake();
            for(int i=0;i<2500&&game.Phase!=SnakePhase.Complete;i++)
            {
                Steer(game);if(i%20==0)yield return null;
            }
            Require(game.Phase==SnakePhase.Complete,"Three playable Snake stages");yield return Capture("ventflow-complete");desktop.Close();
            Require(Door("door.soc.corridor").IsUnlocked&&Door("door.corridor.server").IsUnlocked,"Real doors receive completed milestones");
            yield return Walk(new Vector3(0,0,-4));yield return Walk(new Vector3(0,0,-10));yield return Walk(new Vector3(-2.6f,0,-14.3f));
            Aim(Station(ComputerCampaign.HomeStation).Surface.Centre);desktop.Open(Station(ComputerCampaign.HomeStation));yield return new WaitForSecondsRealtime(.7f);
            root.Simulation.ComputerCampaign.Shell.Execute("cat "+ComputerCampaign.IncidentPath);yield return Capture("home-earned-incident");
            Require(root.Simulation.ComputerCampaign.HasRead(ComputerCampaign.IncidentPath),"Returned home and read earned document");
            Require(root.Persistence.Save("manual2"),"Completed loop save");
            File.WriteAllText(Path.Combine(output,"first-loop-receipt.json"),JsonUtility.ToJson(new Receipt{passed=true,width=Screen.width,height=Screen.height,steps=steps.ToArray(),shots=fired},true));
        }
        private static ComputerStation Station(string id)=>Object.FindObjectsByType<ComputerStation>(FindObjectsSortMode.None).Single(s=>s.StationId==id);
        private static FacilityDoor Door(string id)=>Object.FindObjectsByType<FacilityDoor>(FindObjectsSortMode.None).Single(d=>d.DoorId==id);
        private static IEnumerator Click(Button button)
        {Require(button!=null,"UI control exists");button.Focus();using(var e=NavigationSubmitEvent.GetPooled())button.SendEvent(e);yield return null;}
        private static IEnumerator Capture(string name)
        {
            yield return new WaitForSecondsRealtime(.12f);ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));yield return new WaitForSecondsRealtime(.12f);
        }
        private static void Aim(Vector3 target)
        {
            var rotation=Quaternion.LookRotation(target-motor.Eye).eulerAngles;var state=motor.Capture(false);
            state.yaw=rotation.y;state.pitch=state.targetPitch=Mathf.Clamp(Mathf.DeltaAngle(0,rotation.x),-80,80);motor.Restore(state);
        }
        private static IEnumerator Walk(Vector3 target)
        {
            Aim(new Vector3(target.x,motor.Eye.y,target.z));
            for(int i=0;i<1600;i++)
            {
                var delta=target-motor.transform.position;delta.y=0;
                if(delta.magnitude<.07f){Require(motor.transform.position.y>-.2f,"Grounded route "+target);yield break;}
                cc.Move(Vector3.ClampMagnitude(delta,.05f)+Vector3.down*.035f);
                if(i%3==0)yield return null;
            }
            Require(false,"Blocked route at "+motor.transform.position+" toward "+target);
        }
        private static void Require(bool condition,string message)
        {
            if(!condition){File.WriteAllText(Path.Combine(output,"first-loop-failure.txt"),message);throw new InvalidOperationException("HOSTFALL 2 QA: "+message);}
            steps.Add(message);
        }
        private static void Steer(SnakeSession snake)
        {
            if(snake.Phase!=SnakePhase.Running)snake.Start();
            int head=snake.Body[0],target=snake.Food;var q=new Queue<int>();var parents=new Dictionary<int,int>{{head,-1}};q.Enqueue(head);
            while(q.Count>0&&!parents.ContainsKey(target))
            {
                int c=q.Dequeue();foreach(int n in new[]{c+1,c-1,c+SnakeSession.Width,c-SnakeSession.Width})
                {
                    if(n<0||n>=SnakeSession.Width*SnakeSession.Height||Math.Abs(n%SnakeSession.Width-c%SnakeSession.Width)+Math.Abs(n/SnakeSession.Width-c/SnakeSession.Width)!=1||parents.ContainsKey(n)||snake.IsObstacle(n)||snake.Body.Contains(n))continue;
                    parents[n]=c;q.Enqueue(n);
                }
            }
            if(parents.ContainsKey(target))
            {
                int n=target;while(parents[n]!=head&&parents[n]>=0)n=parents[n];
                snake.Turn(n==head+1?SnakeDirection.Right:n==head-1?SnakeDirection.Left:n>head?SnakeDirection.Down:SnakeDirection.Up);
            }
            snake.Advance(snake.StepSeconds);
        }
        [Serializable]private sealed class Receipt{public bool passed;public int width,height,shots;public string[] steps;}
    }
}
#endif
