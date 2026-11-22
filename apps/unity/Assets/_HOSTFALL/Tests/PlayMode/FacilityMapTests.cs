using System;
using System.Collections;
using System.IO;
using System.Linq;
using Hostfall.Core;
using Hostfall.Devices;
using Hostfall.Mapping;
using Hostfall.Persistence;
using Hostfall.Player;
using Hostfall.Simulation;
using Hostfall.UI;
using Hostfall.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Hostfall.Tests
{
    public sealed class FacilityMapTests
    {
        [UnitySetUp] public IEnumerator LoadFresh()
        {WorldSaveController.FreshStartRequested=true;yield return SceneManager.LoadSceneAsync("NorthlineFacility");yield return null;}
        [UnityTest] public IEnumerator ExplorationUsesRealRoomsSeparatesFloorsAndSurvivesSaveData()
        {
            var ui=UnityEngine.Object.FindAnyObjectByType<FacilityUI>();var root=UnityEngine.Object.FindAnyObjectByType<FacilityRoot>();
            var world=ui.MapWorld;
            Assert.That(world.Rooms.Count,Is.GreaterThan(30));
            Assert.That(world.At(new Vector3(20.5f,.04f,18.5f)).Sector,Is.EqualTo("ПЕРЕХОД"),"Authoring ownership must not mislabel a connector as Sector 09");
            var lower=world.At(new Vector3(4.6f,.04f,34));var upper=world.At(new Vector3(4.6f,3.24f,34));
            Assert.That(lower,Is.Not.Null);Assert.That(upper,Is.Not.Null);
            Assert.That(lower.Id,Is.Not.EqualTo(upper.Id));Assert.That(upper.Name,Is.EqualTo("Пост наблюдения"));
            Assert.That(world.Exploration.HasVisited(upper.Id),Is.False);
            world.Visit(new Vector3(4.6f,.04f,34));
            Assert.That(world.Exploration.HasVisited(lower.Id),Is.True);Assert.That(world.Exploration.HasVisited(upper.Id),Is.False);
            world.Visit(new Vector3(4.6f,3.24f,34));
            Assert.That(world.Exploration.HasVisited(upper.Id),Is.True);
            var captured=root.Persistence.Capture();
            var saved=JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(captured));
            var restored=new ExplorationState();restored.Restore(saved.simulation.exploration);
            Assert.That(restored.HasVisited(upper.Id),Is.True);
            var unknown=world.At(new Vector3(62,.04f,75));Assert.That(restored.HasVisited(unknown.Id),Is.False);
            // The floor polygon is the actual repaired gallery, not an old sector rectangle.
            Assert.That(upper.Bounds.yMin,Is.EqualTo(21.7f).Within(.01f));
            Assert.That(upper.Bounds.width,Is.EqualTo(2.2f).Within(.01f));
            Assert.That(world.Rooms.Single(r=>r.Name=="Грузовая галерея").Level,Is.EqualTo(1),"The raised logistics deck must not be drawn over the ground floor");
            yield return null;
        }
        [UnityTest] public IEnumerator MapOwnsInputAndReturnsToTheScreenThatOpenedIt()
        {
            var ui=UnityEngine.Object.FindAnyObjectByType<FacilityUI>();var controls=UnityEngine.Object.FindAnyObjectByType<ControlState>();
            var root=ui.GetComponent<UIDocument>().rootVisualElement;
            controls.SetMode(ControlMode.Exploring);ui.OpenMap();yield return null;
            Assert.That(controls.Mode,Is.EqualTo(ControlMode.Map));Assert.That(controls.CanExplore,Is.False);
            Assert.That(Time.timeScale,Is.Zero);Assert.That(root.Q("hud").resolvedStyle.display,Is.EqualTo(DisplayStyle.None));
            Assert.That(root.Q("pause").resolvedStyle.display,Is.EqualTo(DisplayStyle.None));
            Assert.That(root.Q("facility-map").resolvedStyle.display,Is.EqualTo(DisplayStyle.Flex));
            Assert.That(root.Q("map-sectors-container"),Is.Null);
            ui.CloseMap();Assert.That(controls.Mode,Is.EqualTo(ControlMode.Exploring));
            controls.SetMode(ControlMode.Paused);ui.OpenMap();ui.CloseMap();
            Assert.That(controls.Mode,Is.EqualTo(ControlMode.Paused));
        }
        [UnityTest] public IEnumerator RadarAndHudRegionsDoNotOverlapAtSmallAndLargeWindowSizes()
        {
            var ui=UnityEngine.Object.FindAnyObjectByType<FacilityUI>();var controls=UnityEngine.Object.FindAnyObjectByType<ControlState>();
            var root=ui.GetComponent<UIDocument>().rootVisualElement;
            controls.SetMode(ControlMode.Exploring);
            var sim=UnityEngine.Object.FindAnyObjectByType<FacilityRoot>().Simulation;
            foreach(var id in new[]{DeviceId.Ws07,DeviceId.Edge01,DeviceId.Core01})sim.RecordTerminalObjective(id);
            foreach(var size in new[]{new Vector2(640,360),new Vector2(800,600),new Vector2(1280,720),new Vector2(1920,1080)})
            {
                root.style.width=size.x;root.style.height=size.y;yield return null;yield return null;
                var names=new[]{"radar-slot","location-badge","health-badge","task-card"};
                for(int i=0;i<names.Length;i++)for(int j=i+1;j<names.Length;j++)
                    Assert.That(root.Q(names[i]).worldBound.Overlaps(root.Q(names[j]).worldBound),Is.False,names[i]+" / "+names[j]+" at "+size);
                foreach(string name in names)
                {
                    var b=root.Q(name).worldBound;
                    Assert.That(b.width,Is.GreaterThan(20),name);
                    Assert.That(b.yMax,Is.LessThanOrEqualTo(root.worldBound.yMax+1),name+" outside window");
                }
            }
        }
        [UnityTest] public IEnumerator SelectedFloorFitsTheViewAndRoomLabelsNeverOverlap()
        {
            var ui=UnityEngine.Object.FindAnyObjectByType<FacilityUI>();
            foreach(var room in ui.MapWorld.Rooms)ui.MapWorld.Exploration.Visit(room.Id);
            UnityEngine.Object.FindAnyObjectByType<ControlState>().SetMode(ControlMode.Exploring);
            ui.OpenMap();yield return null;yield return null;
            ui.MapPlan.SetLevel(1);ui.MapPlan.FitAll();yield return null;yield return null;
            foreach(var room in ui.MapWorld.Rooms.Where(r=>r.Level==1))
            {
                Assert.That(ui.MapPlan.contentRect.Contains(ui.MapPlan.ToScreen(room.Bounds.min)),Is.True,room.Name);
                Assert.That(ui.MapPlan.contentRect.Contains(ui.MapPlan.ToScreen(room.Bounds.max)),Is.True,room.Name);
            }
            var labels=ui.MapPlan.Query<Label>().ToList().Where(l=>l.resolvedStyle.display!=DisplayStyle.None).ToArray();
            Assert.That(labels.Length,Is.GreaterThan(2),"Visited room names remain readable in overview");
            for(int i=0;i<labels.Length;i++)for(int j=i+1;j<labels.Length;j++)
                Assert.That(labels[i].worldBound.Overlaps(labels[j].worldBound),Is.False,labels[i].text+" / "+labels[j].text);
            var center=ui.MapPlan.Center;ui.MapPlan.ZoomBy(1.3f);
            Assert.That(Vector2.Distance(center,ui.MapPlan.Center),Is.LessThan(.001f),"Zoom at view center must not jump to another room");
        }
        [UnityTest] public IEnumerator MapReflectsDoorMotionAndHidesThreatsBehindWalls()
        {
            var ui=UnityEngine.Object.FindAnyObjectByType<FacilityUI>();var world=ui.MapWorld;
            var door=UnityEngine.Object.FindObjectsByType<FacilityDoor>(FindObjectsSortMode.None).Single(d=>d.DoorId=="door.sec02.armory");
            var collider=door.GetComponentsInChildren<BoxCollider>().Single(c=>c.name=="Armory Lower Panel");
            var shape=world.Shapes.Single(s=>s.Collider==collider);
            var before=shape.HorizontalBounds;
            UnityEngine.Object.FindAnyObjectByType<ControlState>().SetMode(ControlMode.Exploring);
            door.UnlockAndOpen();yield return new WaitForSeconds(2);
            world.Refresh();
            Assert.That(collider.bounds.center.y>3 || shape.HorizontalBounds!=before,Is.True,"Moving leaf must update the plan");
            var marker=world.Markers.Single(m=>m.Source==door);Assert.That(((FacilityDoor)marker.Source).IsUnlocked,Is.True);
            var watcher=world.Threats.OfType<WatcherController>().First();
            var eye=watcher.transform.position+Vector3.back*2+Vector3.up*.5f;
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=watcher.transform.position+Vector3.back+Vector3.up;
            wall.transform.localScale=new Vector3(8,4,.4f);Physics.SyncTransforms();
            try{Assert.That(world.CanSeeThreat(watcher,eye),Is.False,"Radar must not expose robots through a wall");}
            finally{UnityEngine.Object.Destroy(wall);}
        }
        [UnityTest] public IEnumerator CaptureMapAndRadarForVisualReview()
        {
            string directory=Environment.GetEnvironmentVariable("HOSTFALL_MAP_REVIEW");
            if(string.IsNullOrEmpty(directory))yield break;
            Directory.CreateDirectory(directory);
            var ui=UnityEngine.Object.FindAnyObjectByType<FacilityUI>();var document=ui.GetComponent<UIDocument>();
            var controls=UnityEngine.Object.FindAnyObjectByType<ControlState>();var motor=UnityEngine.Object.FindAnyObjectByType<PlayerMotor>();
            // Render the actual UI Toolkit panel at a fixed size, without reading player saves.
            var original=document.panelSettings;var settings=UnityEngine.Object.Instantiate(original);var target=new RenderTexture(1280,800,24);
            target.Create();settings.targetTexture=target;document.panelSettings=settings;
            settings.clearColor=true;settings.colorClearValue=new Color(.025f,.04f,.05f,1);
            yield return null;yield return null;
            controls.SetMode(ControlMode.Exploring);motor.enabled=false;
            var cc=motor.GetComponent<CharacterController>();cc.enabled=false;motor.transform.position=new Vector3(0,.05f,24);cc.enabled=true;
            ui.RefreshMap();
            yield return null;yield return null;Save("radar");
            ui.OpenMap();yield return null;yield return null;Save("map-local");
            ui.MapPlan.FitAll();yield return null;yield return null;Save("map-overview");
            ui.CloseMap();
            foreach(var p in new[]{new Vector3(4.6f,3.24f,34),new Vector3(-16.5f,.05f,20),new Vector3(31,.05f,36)})ui.MapWorld.Visit(p);
            ui.OpenMap();ui.MapPlan.SetLevel(1);ui.MapPlan.FitAll();yield return null;yield return null;Save("map-upper");
            ui.CloseMap();document.panelSettings=original;settings.targetTexture=null;UnityEngine.Object.Destroy(target);UnityEngine.Object.Destroy(settings);
            void Save(string name)
            {
                var previous=RenderTexture.active;RenderTexture.active=target;
                var texture=new Texture2D(target.width,target.height,TextureFormat.RGBA32,false);
                texture.ReadPixels(new Rect(0,0,target.width,target.height),0,0);texture.Apply();
                File.WriteAllBytes(Path.Combine(directory,name+".png"),texture.EncodeToPNG());
                RenderTexture.active=previous;UnityEngine.Object.Destroy(texture);
            }
        }
    }
}
