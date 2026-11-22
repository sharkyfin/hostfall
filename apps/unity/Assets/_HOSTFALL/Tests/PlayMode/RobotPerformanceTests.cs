using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Hostfall.Core;
using Hostfall.Persistence;
using Hostfall.Player;
using Hostfall.World;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace Hostfall.Tests
{
    public sealed class RobotPerformanceTests
    {
        private static string Output
        {
            get {var p=new DirectoryInfo(Application.dataPath);while(p!=null&&!File.Exists(Path.Combine(p.FullName,"AGENTS.md")))p=p.Parent;var path=Path.Combine(p.FullName,".artifacts/robot-performance-20261115");Directory.CreateDirectory(path);return path;}
        }
        [TearDown]public void Reset(){WorldSaveController.FreshStartRequested=false;}
        [UnityTest]public IEnumerator InspectArmorAndRenderInventory()
        {
            WorldSaveController.FreshStartRequested=true;yield return SceneManager.LoadSceneAsync("NorthlineFacility");yield return null;
            var controls=Object.FindAnyObjectByType<ControlState>();controls.SetMode(ControlMode.Exploring);
            var player=Object.FindAnyObjectByType<PlayerMotor>();player.enabled=false;
            var watchers=Object.FindObjectsByType<WatcherController>(FindObjectsSortMode.None);
            var lines=new List<string>();float maximumDistance=0;int mountsChecked=0;
            foreach(var w in watchers)
            {
                w.enabled=false;
                var animator=w.GetComponentInChildren<Animator>();animator.speed=0;
                lines.Add("ACTOR "+w.Id+" renderers="+w.GetComponentsInChildren<Renderer>().Length);
                foreach(var r in w.GetComponentsInChildren<SkinnedMeshRenderer>())lines.Add("SKIN "+r.name+" bounds="+r.bounds+" bones="+string.Join(",",r.bones.Select(b=>b.name)));
                foreach(var plate in w.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Armor")))
                    lines.Add("PLATE "+plate.name+" parent="+plate.parent.name+" pos="+w.transform.InverseTransformPoint(plate.position)+" local="+plate.localPosition+" scale="+plate.lossyScale);
                var original=w.transform.position;w.transform.position=new Vector3(150,0,0);
                var lamp=new GameObject("Armor review light").AddComponent<Light>();lamp.transform.position=new Vector3(150,3,2);lamp.range=8;lamp.intensity=5;
                foreach(string clip in new[]{"Idle","Walk","Run","Attack"})foreach(float phase in new[]{.1f,.5f,.9f})
                {
                    animator.Play(clip,0,phase);animator.Update(0);
                    var shell=w.GetComponentsInChildren<SkinnedMeshRenderer>().First(r=>r.name.EndsWith("_Body",StringComparison.Ordinal));
                    var baked=new Mesh();shell.BakeMesh(baked,true);var vertices=baked.vertices;var triangles=baked.triangles;
                    for(int v=0;v<vertices.Length;v++)vertices[v]=shell.transform.TransformPoint(vertices[v]);
                    foreach(var plate in w.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("ArmorL")||t.name.StartsWith("ArmorR")))
                    {
                        Assert.That(plate.parent.name,Is.EqualTo("Body"));float distance=float.PositiveInfinity;
                        for(int t=0;t<triangles.Length;t+=3)distance=Mathf.Min(distance,TriangleDistance(plate.position,vertices[triangles[t]],vertices[triangles[t+1]],vertices[triangles[t+2]]));
                        if(phase==.1f&&clip=="Idle")
                        {
                            File.AppendAllText(Output+"/surface-probe.txt",w.Id+" "+plate.name+" actorLocal="+w.transform.InverseTransformPoint(plate.position)+" skinBounds="+shell.bounds+" bakedBounds="+baked.bounds+" scale="+shell.transform.lossyScale+" distance="+distance+"\n");
                            Save(w.Id+"-surface-proof",w.transform.position+w.transform.forward*2.3f+w.transform.right*1.2f+Vector3.up*1.1f,w.transform.position+Vector3.up*.65f);
                        }
                        maximumDistance=Mathf.Max(maximumDistance,distance);mountsChecked++;
                        Assert.That(distance,Is.LessThan(.025f),w.Id+" / "+plate.name+" / "+clip+" must touch the animated torso");
                    }
                    Object.Destroy(baked);
                    if(w.Id=="watcher-r17"||w.Id=="sentry-r18")Save(w.Id+"-"+clip+"-"+phase.ToString("0.0",System.Globalization.CultureInfo.InvariantCulture),w.transform.position+w.transform.forward*2.3f+w.transform.right*1.2f+Vector3.up*1.1f,w.transform.position+Vector3.up*.65f);
                }
                w.transform.position=original;Object.DestroyImmediate(lamp.gameObject);
            }
            lines.Add("ARMOR checked="+mountsChecked+" maximumDistance="+maximumDistance);
            var guardian=Object.FindObjectsByType<TrackedSentryController>(FindObjectsSortMode.None).First();
            var guardianPosition=guardian.transform.position;guardian.transform.position=new Vector3(150,0,0);
            var guardianLamp=new GameObject("Guardian review light").AddComponent<Light>();guardianLamp.transform.position=new Vector3(150,4,1);guardianLamp.range=12;guardianLamp.intensity=5;
            Save("guardian-front",guardian.transform.position+guardian.transform.forward*5+guardian.transform.right*4+Vector3.up*3,guardian.transform.position+Vector3.up*.8f);
            Save("guardian-rear",guardian.transform.position-guardian.transform.forward*5-guardian.transform.right*4+Vector3.up*3,guardian.transform.position+Vector3.up*.8f);
            guardian.transform.position=guardianPosition;Object.DestroyImmediate(guardianLamp.gameObject);
            var renderers=Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);var lights=Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            lines.Add("WORLD renderers="+renderers.Length+" lights="+lights.Length+" shadows="+lights.Count(l=>l.shadows!=LightShadows.None));
            foreach(var r in renderers.GroupBy(r=>r.transform.root.name).OrderByDescending(g=>g.Count()).Take(15))lines.Add("ROOT "+r.Key+" renderers="+r.Count());
            foreach(var boss in Object.FindObjectsByType<TrackedSentryController>(FindObjectsSortMode.None))lines.Add("BOSS "+boss.name+" renderers="+boss.GetComponentsInChildren<Renderer>().Length+" materials="+boss.GetComponentsInChildren<Renderer>().SelectMany(r=>r.sharedMaterials).Distinct().Count());
            File.WriteAllLines(Output+"/armor-inventory.txt",lines);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest]public IEnumerator NavigationBurstBenchmark()
        {
            WorldSaveController.FreshStartRequested=true;yield return SceneManager.LoadSceneAsync("NorthlineFacility");yield return null;
            Object.FindAnyObjectByType<ControlState>().SetMode(ControlMode.Paused);
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(150,-.1f,0);floor.transform.localScale=new Vector3(14,.2f,14);
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=new Vector3(150,1,0);wall.transform.localScale=new Vector3(4,2,.5f);
            var navs=new RobotNavigation[12];var actors=new GameObject[12];
            for(int i=0;i<navs.Length;i++){actors[i]=new GameObject("Navigation load "+i);actors[i].transform.position=new Vector3(148.5f+i*.25f,0,-4);navs[i]=new RobotNavigation(actors[i].transform);}
            Physics.SyncTransforms();var times=new double[120];var clock=new Stopwatch();
            for(int f=0;f<times.Length;f++)
            {
                clock.Restart();for(int i=0;i<navs.Length;i++)navs[i].Move(new Vector3(151.5f-i*.25f,0,4),2,1/60f,new Vector2(144,156),new Vector2(-6,6));RobotNavigation.ProcessSearchFrame();clock.Stop();times[f]=clock.Elapsed.TotalMilliseconds;
                Assert.That(RobotNavigation.LastSearchChecks,Is.LessThanOrEqualTo(RobotNavigation.MaximumSearchChecksPerFrame));
                Physics.SyncTransforms();yield return null;
            }
            Assert.That(actors.All(a=>a.transform.position.z>-3.8f),Is.True,"Every queued robot must make progress, not merely save CPU by stopping.");
            Array.Sort(times);File.WriteAllText(Output+"/navigation-window.json",JsonUtility.ToJson(new Window{frames=times.Length,median=times[60],p95=times[114],maximum=times[119],total=times.Sum()}));
            foreach(var a in actors)Object.Destroy(a);Object.Destroy(wall);Object.Destroy(floor);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest]public IEnumerator NavigationRespondsToDoorAndRejectsFloorGap()
        {
            WorldSaveController.FreshStartRequested=true;yield return SceneManager.LoadSceneAsync("NorthlineFacility");yield return null;
            Object.FindAnyObjectByType<ControlState>().SetMode(ControlMode.Paused);RobotNavigation.ResetSearchQueue();
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(150,-.1f,0);floor.transform.localScale=new Vector3(4,.2f,12);
            var door=GameObject.CreatePrimitive(PrimitiveType.Cube);door.transform.position=new Vector3(150,1,0);door.transform.localScale=new Vector3(5,2,.4f);
            var actor=new GameObject("Door navigation probe");actor.transform.position=new Vector3(150,0,-3);var nav=new RobotNavigation(actor.transform);
            var goal=new Vector3(150,0,3);Physics.SyncTransforms();
            for(int f=0;f<180;f++){nav.Move(goal,2,1/60f,new Vector2(148.5f,151.5f),new Vector2(-5,5));RobotNavigation.ProcessSearchFrame();Physics.SyncTransforms();}
            Assert.That(actor.transform.position.z,Is.LessThan(-.45f),"Closed door must stop the robot");
            door.SetActive(false);Physics.SyncTransforms();
            for(int f=0;f<420;f++){nav.Move(goal,2,1/60f,new Vector2(148.5f,151.5f),new Vector2(-5,5));RobotNavigation.ProcessSearchFrame();Physics.SyncTransforms();}
            Assert.That(Vector3.Distance(actor.transform.position,goal),Is.LessThan(.2f),"Opening the door must release a waiting robot");
            actor.transform.position=new Vector3(150,0,-3);floor.transform.position=new Vector3(150,-.1f,-3);floor.transform.localScale=new Vector3(4,.2f,4);
            var far=GameObject.CreatePrimitive(PrimitiveType.Cube);far.transform.position=new Vector3(150,-.1f,3);far.transform.localScale=new Vector3(4,.2f,4);
            nav=new RobotNavigation(actor.transform);Physics.SyncTransforms();
            for(int f=0;f<360;f++){nav.Move(goal,2,1/60f,new Vector2(148.5f,151.5f),new Vector2(-5,5));RobotNavigation.ProcessSearchFrame();Physics.SyncTransforms();}
            Assert.That(actor.transform.position.z,Is.LessThan(-.7f),"A long direct shortcut must not cross a floor gap");
            Object.Destroy(actor);Object.Destroy(door);Object.Destroy(floor);Object.Destroy(far);RobotNavigation.ResetSearchQueue();LogAssert.NoUnexpectedReceived();
        }
        [Serializable]private class Window{public int frames;public double median,p95,maximum,total;}
        private static float TriangleDistance(Vector3 point,Vector3 a,Vector3 b,Vector3 c)
        {
            var normal=Vector3.Cross(b-a,c-a);float squared=normal.sqrMagnitude;
            if(squared>1e-12f)
            {
                var projected=point-normal*(Vector3.Dot(point-a,normal)/squared);
                if(Vector3.Dot(Vector3.Cross(b-a,projected-a),normal)>=-1e-10f&&Vector3.Dot(Vector3.Cross(c-b,projected-b),normal)>=-1e-10f&&Vector3.Dot(Vector3.Cross(a-c,projected-c),normal)>=-1e-10f)return Vector3.Distance(point,projected);
            }
            return Mathf.Min(Edge(a,b),Mathf.Min(Edge(b,c),Edge(c,a)));
            float Edge(Vector3 x,Vector3 y){var d=y-x;return Vector3.Distance(point,x+d*Mathf.Clamp01(Vector3.Dot(point-x,d)/Mathf.Max(1e-12f,d.sqrMagnitude)));}
        }
        private static void Save(string name,Vector3 position,Vector3 target)
        {
            var go=new GameObject("Robot review camera");var camera=go.AddComponent<Camera>();camera.transform.position=position;camera.transform.LookAt(target);camera.fieldOfView=46;camera.nearClipPlane=.03f;
            var rt=RenderTexture.GetTemporary(960,720,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);var old=RenderTexture.active;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            var texture=new Texture2D(960,720,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,960,720),0,0);texture.Apply();File.WriteAllBytes(Output+"/"+name+".png",texture.EncodeToPNG());RenderTexture.active=old;camera.targetTexture=null;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(texture);Object.DestroyImmediate(go);
        }
    }
}
