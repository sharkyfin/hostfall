using System.Collections;
using System.Linq;
using Hostfall.Core;
using Hostfall.Persistence;
using Hostfall.Player;
using Hostfall.Simulation;
using Hostfall.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Hostfall.Tests
{
    public sealed class CombatIntegrationTests
    {
        [UnityTest] public IEnumerator AuthoredBossModulesAndWeaponsAreRealAndSurviveSave()
        {
            yield return SceneManager.LoadSceneAsync("NorthlineFacility");yield return null;
            var root=Object.FindAnyObjectByType<FacilityRoot>();
            var actors=Object.FindObjectsByType<RobotCombatController>(FindObjectsSortMode.None);
            Assert.That(actors.Count(a=>a.Model.IsBoss),Is.EqualTo(2));
            Assert.That(actors.Select(a=>a.Model.Id).Distinct().Count(),Is.EqualTo(actors.Length));
            Assert.That(actors.Count(a=>a.Model.Id.StartsWith("aux-watch-")),Is.EqualTo(4));
            var boss=actors.First(a=>a.Model.IsBoss);
            Assert.That(boss.GetComponentsInChildren<MeshRenderer>().Length,Is.InRange(16,24),"Fixed parts are grouped; independent weak points remain");
            var fixedMeshes=boss.GetComponentsInChildren<MeshFilter>().Where(f=>f.transform.parent.name=="Fixed render groups").ToArray();
            Assert.That(fixedMeshes.Length,Is.EqualTo(6));
            Assert.That(fixedMeshes.All(f=>f.sharedMesh.vertexCount>0&&f.sharedMesh.GetIndexCount(0)>0),Is.True,"A reduced renderer count must not hide missing geometry");
            Assert.That(fixedMeshes.Sum(f=>f.sharedMesh.vertexCount),Is.GreaterThan(1000));
            Assert.That(boss.GetComponentsInChildren<RobotHitbox>().Select(h=>h.Region),Does.Contain(RobotHitRegion.Core));
            boss.Hit(140,RobotHitRegion.LeftCoolant,boss.transform.position);
            Assert.That(boss.Model.CoreExposed,Is.False);
            boss.Hit(140,RobotHitRegion.RightCoolant,boss.transform.position);
            Assert.That(boss.Model.CoreExposed,Is.True);
            Assert.That(boss.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("CoreHatch")).All(t=>!t.gameObject.activeSelf),Is.True);
            var data=root.Persistence.Capture();GameSaveData.Validate(data);
            var restored=FacilitySimulation.Restore(data.simulation).Combat.Get(boss.Model.Id);
            Assert.That(restored.Health,Is.EqualTo(boss.Model.Health));Assert.That(restored.CoreExposed,Is.True);
            foreach(string id in new[]{"ARC12","CIRRUS","TITAN"})
                Assert.That(Resources.Load<GameObject>("Combat/"+id).GetComponentsInChildren<MeshFilter>().Sum(m=>m.sharedMesh.vertexCount),Is.GreaterThan(2000));
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator PrecisionShotsRespectWallsAndDeadActorsCannotBeHitAgain()
        {
            yield return SceneManager.LoadSceneAsync("NorthlineFacility");yield return null;
            var root=Object.FindAnyObjectByType<FacilityRoot>();var controls=Object.FindAnyObjectByType<ControlState>();controls.SetMode(ControlMode.Paused);
            var actor=new GameObject("Combat test actor");actor.transform.position=new Vector3(150,0,0);actor.AddComponent<WatcherController>();
            var combat=actor.AddComponent<RobotCombatController>();combat.Initialize(new FacilitySimulation(),controls);
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=new Vector3(150,1,-2);wall.transform.localScale=new Vector3(3,3,.1f);
            Physics.SyncTransforms();var ray=new Ray(new Vector3(150,1.05f,-4),Vector3.forward);
            Assert.That(RobotShot.Fire(ray,10,40,out _),Is.True);Assert.That(combat.Model.Health,Is.EqualTo(260));
            Object.Destroy(wall);yield return null;Physics.SyncTransforms();
            RobotShot.Fire(ray,10,40,out _);Assert.That(combat.Model.Health,Is.EqualTo(172).Within(.01f),"Sensor hit deals 2.2x damage");
            combat.Hit(1000,RobotHitRegion.Hull,actor.transform.position);
            Assert.That(combat.Model.IsDead,Is.True);Assert.That(actor.GetComponentsInChildren<Collider>().All(c=>!c.enabled),Is.True);
            Assert.That(RobotShot.Fire(ray,10,40,out _),Is.False);
            Object.Destroy(actor);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator PauseFreezesBossAndHudSeparatesHealthFromRadar()
        {
            yield return SceneManager.LoadSceneAsync("NorthlineFacility");yield return null;
            var controls=Object.FindAnyObjectByType<ControlState>();controls.SetMode(ControlMode.Exploring);
            var ui=Object.FindAnyObjectByType<Hostfall.UI.FacilityUI>().GetComponent<UIDocument>().rootVisualElement;
            yield return null;
            Assert.That(ui.Q("health-badge").parent.name,Is.EqualTo("hud"));
            var dot=ui.Q("crosshair");Assert.That(dot.worldBound.center.x,Is.EqualTo(ui.worldBound.center.x).Within(2));Assert.That(dot.worldBound.center.y,Is.EqualTo(ui.worldBound.center.y).Within(2));
            var boss=Object.FindAnyObjectByType<CrawlerMinibossController>();var p=boss.transform.position;
            controls.SetMode(ControlMode.Paused);yield return new WaitForSecondsRealtime(.3f);
            Assert.That(boss.transform.position,Is.EqualTo(p));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator UpperShellAndSecurityPortalHaveNoVisibleSideGaps()
        {
            yield return SceneManager.LoadSceneAsync("NorthlineFacility");yield return null;
            Physics.SyncTransforms();
            foreach(float z in new[]{17f,21f,25f,29f,33f,36f})
                foreach(float side in new[]{-1f,1f})
                    Assert.That(Physics.Raycast(new Vector3(0,4.9f,z),Vector3.right*side,7.4f,CollisionLayers.WorldMask),Is.True,"S02 upper shell at "+z);
            foreach(float x in new[]{27.5f,28f,32f,32.5f})
                Assert.That(Physics.Raycast(new Vector3(x,2,66),Vector3.back,2,CollisionLayers.WorldMask),Is.True,"SEC07 aperture shoulder "+x);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator CaptureActualHealthAndAimPanel()
        {
            yield return SceneManager.LoadSceneAsync("NorthlineFacility");yield return null;
            var controls=Object.FindAnyObjectByType<ControlState>();controls.SetMode(ControlMode.Exploring);
            var document=Object.FindAnyObjectByType<Hostfall.UI.FacilityUI>().GetComponent<UIDocument>();
            var original=document.panelSettings;var settings=Object.Instantiate(original);var target=new RenderTexture(1280,800,24);target.Create();
            settings.targetTexture=target;settings.clearColor=true;settings.colorClearValue=new Color(.025f,.04f,.05f,1);document.panelSettings=settings;
            yield return null;yield return null;
            var directory=new System.IO.DirectoryInfo(Application.dataPath);while(directory!=null&&!System.IO.File.Exists(System.IO.Path.Combine(directory.FullName,"AGENTS.md")))directory=directory.Parent;
            string output=System.IO.Path.Combine(directory.FullName,".artifacts/combat-rework-20261114");System.IO.Directory.CreateDirectory(output);
            var previous=RenderTexture.active;RenderTexture.active=target;var texture=new Texture2D(1280,800,TextureFormat.RGBA32,false);texture.ReadPixels(new Rect(0,0,1280,800),0,0);texture.Apply();
            System.IO.File.WriteAllBytes(output+"/hud.png",texture.EncodeToPNG());RenderTexture.active=previous;
            document.panelSettings=original;settings.targetTexture=null;Object.Destroy(texture);Object.Destroy(target);Object.Destroy(settings);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator LocalNavigationFindsARealRouteAroundCover()
        {
            yield return SceneManager.LoadSceneAsync("NorthlineFacility");yield return null;
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(150,-.1f,0);floor.transform.localScale=new Vector3(12,.2f,12);
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=new Vector3(150,1,0);wall.transform.localScale=new Vector3(2,2,1);
            var actor=new GameObject("Navigation probe");actor.transform.position=new Vector3(150,0,-3);var nav=new RobotNavigation(actor.transform);
            var goal=new Vector3(150,0,3);Physics.SyncTransforms();
            for(int i=0;i<300;i++){nav.Move(goal,2,.05f,new Vector2(146,154),new Vector2(-4,4));RobotNavigation.ProcessSearchFrame();Physics.SyncTransforms();}
            Assert.That(Vector3.Distance(actor.transform.position,goal),Is.LessThan(.4f));
            var wide=new GameObject("Guardian footprint probe");wide.transform.position=new Vector3(147.2f,0,0);
            var guardianNavigation=new RobotNavigation(wide.transform,1.1f);Physics.SyncTransforms();
            Assert.That(guardianNavigation.Face(Vector3.right,1,90),Is.False,"A wide guardian cannot rotate its hull into cover");
            Assert.That(Quaternion.Angle(wide.transform.rotation,Quaternion.identity),Is.LessThan(.01f));
            Object.Destroy(wide);Object.Destroy(floor);Object.Destroy(wall);Object.Destroy(actor);LogAssert.NoUnexpectedReceived();
        }
    }
}
