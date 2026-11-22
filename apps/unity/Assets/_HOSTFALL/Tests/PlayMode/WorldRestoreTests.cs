using System.Collections;
using System.IO;
using System.Linq;
using Hostfall.CameraSystem;
using Hostfall.Core;
using Hostfall.Devices;
using Hostfall.Persistence;
using Hostfall.Player;
using Hostfall.Simulation;
using Hostfall.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Hostfall.Tests
{
    public sealed class WorldRestoreTests
    {
        [UnityTest]
        public IEnumerator SerializedWorldRestoresPhysicalPosesAndDisplaysAcrossSceneReload()
        {
            yield return SceneManager.LoadSceneAsync("NorthlineFacility"); yield return null;
            var root = Object.FindAnyObjectByType<FacilityRoot>();
            MenuFlowTests.FinishRoutine(root.Simulation,DeviceId.Ws07);
            MenuFlowTests.FinishRoutine(root.Simulation,DeviceId.Edge01);
            var data = root.Persistence.Capture();
            data.player.position = new Vector3(.2f,.05f,5.3f); data.player.yaw = 45; data.player.pitch = 40; data.player.targetPitch = 40; data.player.thirdPerson = true;
            data.door.opened = true; data.door.offset = 1.1f;
            data.watcher.position = new Vector3(-.25f,0,4); data.watcher.direction = -1; data.watcher.contactCooldown = 1;
            string path = Path.Combine(Path.GetTempPath(),"hostfall-play-save-"+System.Guid.NewGuid().ToString("N"));
            try
            {
                var store = new WorldSaveStore(path); store.Write(data);
                yield return SceneManager.LoadSceneAsync("NorthlineFacility"); yield return null;
                Assert.That(store.TryLoad(out data,out string error),Is.True,error);
                var world = FacilitySimulation.Restore(data.simulation);
                var player = Object.FindAnyObjectByType<PlayerMotor>(); player.Restore(data.player);
                var rig = Object.FindAnyObjectByType<PlayerCameraRig>(); rig.RestoreMode(data.player.thirdPerson);
                var door = Object.FindAnyObjectByType<FacilityDoor>(); door.Restore(data.door);
                var robot = Object.FindObjectsByType<WatcherController>(FindObjectsSortMode.None).Single(r=>r.Id=="watcher-r17"); robot.Restore(data.watcher);
                Assert.That(Vector3.Distance(player.transform.position,data.player.position),Is.LessThan(.001f));
                Assert.That(player.Capture(true).pitch,Is.EqualTo(40)); Assert.That(rig.ThirdPerson,Is.True);
                Assert.That(door.Capture().offset,Is.EqualTo(1.1f)); Assert.That(robot.Capture().direction,Is.EqualTo(-1));
                var terminal = Object.FindObjectsByType<ComputerTerminal>().Single(t=>t.Id==DeviceId.Ws07);
                terminal.GetComponent<ComputerScreen>().Refresh(world,DeviceId.Ws07);
                Assert.That(terminal.GetComponent<ComputerScreen>().DisplayedText,Is.EqualTo(TerminalDisplay.Read(world,DeviceId.Ws07).Tail(62,15)));
                Assert.That(TerminalDisplay.Read(world,DeviceId.Ws07).Transcript,Does.Contain("systemctl stop cache-sync"));
                var visual = Object.FindAnyObjectByType<OperatorVisual>(FindObjectsInactive.Include); visual.SetView(true,true);
                var shoulders = visual.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Shoulder").ToArray(); Assert.That(shoulders.Length,Is.EqualTo(2));
                Assert.That(shoulders.All(t=>t.gameObject.activeSelf),Is.True);
                Assert.That(visual.GetComponentsInChildren<Renderer>().Any(r=>r.shadowCastingMode==ShadowCastingMode.ShadowsOnly),Is.True);
                visual.SetView(false,true); Assert.That(shoulders.All(t=>t.gameObject.activeSelf),Is.True);
                Assert.That(visual.GetComponentsInChildren<Renderer>().Any(r=>r.shadowCastingMode==ShadowCastingMode.ShadowsOnly),Is.False);
                LogAssert.NoUnexpectedReceived();
            }
            finally { if(Directory.Exists(path)) Directory.Delete(path,true); }
        }
    }
}
