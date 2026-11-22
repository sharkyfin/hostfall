using System.Collections;
using System.IO;
using System.Linq;
using Hostfall.CameraSystem;
using Hostfall.Core;
using Hostfall.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Hostfall.Tests
{
    public sealed class WeaponAtelierTests
    {
        [UnityTest] public IEnumerator ModelsHaveSocketsSurfaceMapsAndABoundedMeshBudget()
        {
            yield return SceneManager.LoadSceneAsync("NorthlineFacility");yield return null;
            foreach(string id in new[]{"ARC12","CIRRUS","TITAN"})
            {
                var model=Resources.Load<GameObject>("Combat/"+id);Assert.That(model,Is.Not.Null);
                var renderers=model.GetComponentsInChildren<MeshRenderer>();Assert.That(renderers.Length,Is.InRange(3,4));
                int indices=model.GetComponentsInChildren<MeshFilter>().Sum(f=>Enumerable.Range(0,f.sharedMesh.subMeshCount).Sum(i=>(int)f.sharedMesh.GetIndexCount(i)));
                Assert.That(indices/3,Is.InRange(15000,60000));
                foreach(string socket in new[]{"GripRight","GripLeft","Muzzle"})Assert.That(model.GetComponentsInChildren<Transform>().Any(t=>t.name==socket),Is.True,id+" needs "+socket);
                foreach(var mat in renderers.SelectMany(r=>r.sharedMaterials))
                {
                    Assert.That(mat.shader.name,Is.EqualTo("Universal Render Pipeline/Lit"));
                    Assert.That(mat.GetTexture("_BaseMap"),Is.Not.Null);Assert.That(mat.GetTexture("_BumpMap"),Is.Not.Null);
                    Assert.That(mat.GetTexture("_MetallicGlossMap"),Is.Not.Null);
                }
            }
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator RealBodyHandsFollowAllWeaponSocketsAndMechanismsMove()
        {
            yield return SceneManager.LoadSceneAsync("NorthlineFacility");yield return null;
            var controls=Object.FindAnyObjectByType<ControlState>();controls.SetMode(ControlMode.Exploring);
            var weapon=Object.FindAnyObjectByType<PlayerWeapon>();weapon.Equip();
            var body=Object.FindAnyObjectByType<OperatorVisual>();var motor=Object.FindAnyObjectByType<PlayerMotor>();motor.enabled=false;
            foreach(var kind in new[]{ActiveWeaponType.Arc12Shotgun,ActiveWeaponType.CirrusPulseSidearm,ActiveWeaponType.TitanbreakerHeavyInductor})
            {
                if(kind==ActiveWeaponType.CirrusPulseSidearm)weapon.UnlockPulseSidearm();else if(kind==ActiveWeaponType.TitanbreakerHeavyInductor)weapon.UnlockTitanbreaker();
                yield return new WaitForSeconds(.32f);
                var presentation=weapon.HeldModel.GetComponent<WeaponPresentation>();
                var arms=body.GetComponentsInChildren<Transform>().Where(t=>t.name=="Shoulder").OrderBy(t=>t.localPosition.x).ToArray();
                Assert.That(Vector3.Distance(arms[1].Find("Gloved hand").position,presentation.RightGrip.position),Is.LessThan(.025f),kind+" right hand");
                Assert.That(Vector3.Distance(arms[0].Find("Gloved hand").position,presentation.LeftGrip.position),Is.LessThan(.06f),kind+" support hand");
                Assert.That(weapon.TryFire(),Is.True);yield return new WaitForSeconds(.06f);
                Assert.That(presentation.ShotAge,Is.GreaterThan(0));
                Assert.That(weapon.StartReload(),Is.True);yield return new WaitForSeconds(.65f);
                var magazine=weapon.HeldModel.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="Magazine");
                Assert.That(magazine!=null||kind==ActiveWeaponType.Arc12Shotgun,Is.True);
                Assert.That(Vector3.Distance(arms[0].Find("Gloved hand").position,presentation.SupportGrip.position),Is.LessThan(.06f),"Support hand carries the moving mechanism or power pack");
                yield return new WaitForSeconds(weapon.ReloadDuration+.05f);
                Assert.That(weapon.IsReloading,Is.False);
            }
            Assert.That(Object.FindObjectsByType<WeaponPresentation>(FindObjectsSortMode.None).Count(p=>p.gameObject.activeInHierarchy),Is.EqualTo(1),"No old held meshes after switching");
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator CaptureActualFirstPersonWeaponsAndReload()
        {
            yield return SceneManager.LoadSceneAsync("NorthlineFacility");yield return null;
            var controls=Object.FindAnyObjectByType<ControlState>();controls.SetMode(ControlMode.Exploring);
            var motor=Object.FindAnyObjectByType<PlayerMotor>();motor.enabled=false;
            var saved=motor.Capture(false);saved.position=new Vector3(0,.05f,-4);saved.yaw=0;saved.pitch=0;saved.targetPitch=0;motor.Restore(saved);
            Object.FindAnyObjectByType<PlayerCameraRig>().RestoreMode(false);
            var weapon=motor.GetComponent<PlayerWeapon>();weapon.Equip();
            var directory=new DirectoryInfo(Application.dataPath);while(directory!=null&&!File.Exists(Path.Combine(directory.FullName,"AGENTS.md")))directory=directory.Parent;
            string output=Path.Combine(directory.FullName,".artifacts/weapon-quality-20261114/review");Directory.CreateDirectory(output);
            foreach(string id in new[]{"ARC12","CIRRUS","TITAN"})
            {
                if(id=="CIRRUS")weapon.UnlockPulseSidearm();else if(id=="TITAN")weapon.UnlockTitanbreaker();
                yield return new WaitForSeconds(.32f);Save(id+"-fps");
                weapon.TryFire();weapon.StartReload();yield return new WaitForSeconds(.6f);Save(id+"-reload");yield return new WaitForSeconds(.8f);
            }
            void Save(string name)
            {
                var camera=Camera.main;var target=new RenderTexture(1440,900,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);target.Create();
                var previous=RenderTexture.active;camera.targetTexture=target;camera.Render();RenderTexture.active=target;
                var image=new Texture2D(1440,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1440,900),0,0);image.Apply();
                File.WriteAllBytes(output+"/"+name+".png",image.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=previous;Object.Destroy(image);Object.Destroy(target);
            }
            LogAssert.NoUnexpectedReceived();
        }
    }
}
