using System.Collections;
using System.Linq;
using Hostfall.Core;
using Hostfall.Player;
using Hostfall.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace Hostfall.Tests
{
    public sealed class EmbodimentTests
    {
        [UnityTest] public IEnumerator ArmsRemainConnectedAndOpticsFollowAnimatedBodyInEveryUsedClip()
        {
            yield return SceneManager.LoadSceneAsync("NorthlineFacility"); yield return null;
            var visual=Object.FindAnyObjectByType<OperatorVisual>();
            Assert.That(visual.GetComponentsInChildren<Transform>(true).Any(t=>t.name.Contains("First person")),Is.False,"No detached duplicate arms");
            foreach(bool first in new[] { true,false })
            {
                visual.SetView(first,true);
                foreach(var arm in visual.GetComponentsInChildren<Transform>().Where(t=>t.name=="Shoulder"))
                {
                    Assert.That(arm.gameObject.activeSelf,Is.True);
                    Assert.That(arm.Find("Upper sleeve").GetComponent<Renderer>().bounds.Intersects(arm.Find("Forearm sleeve").GetComponent<Renderer>().bounds),Is.True);
                    Assert.That(arm.Find("Forearm sleeve").GetComponent<Renderer>().bounds.Intersects(arm.Find("Gloved hand/Wrist cuff").GetComponent<Renderer>().bounds),Is.True);
                }
            }
            var motor=Object.FindAnyObjectByType<PlayerMotor>();
            foreach(float pitch in new[] { -80f,0,80f })
            {
                var state=motor.Capture(false); state.pitch=state.targetPitch=pitch; motor.Restore(state); yield return null;
                Assert.That(Vector3.Distance(visual.transform.position,motor.transform.position),Is.LessThan(.01f));
            }
            foreach(var enemy in Object.FindObjectsByType<WatcherController>(FindObjectsSortMode.None))
            {
                var optic=enemy.GetComponentsInChildren<Transform>().Single(t=>t.name=="Optical sensor");
                Assert.That(optic.parent.name,Is.EqualTo("Body"));
                Assert.That(enemy.GetComponentsInChildren<Transform>().Any(t=>t.name=="Sensor mast"),Is.False);
                var mount=optic.localPosition; var animator=enemy.GetComponentInChildren<Animator>(); animator.speed=1;
                foreach(string clip in new[] { "Idle","Walk","Run","Attack","TurnOff" })
                {
                    animator.Play(clip,0,0); animator.Update(.2f);
                    Assert.That(Vector3.Distance(optic.localPosition,mount),Is.LessThan(.0001f),enemy.Id+" "+clip);
                    Assert.That(Vector3.Distance(optic.position,enemy.transform.position),Is.LessThan(1.5f),"Optics cannot remain floating above a collapsed body");
                }
            }
            Object.FindAnyObjectByType<ControlState>().SetMode(ControlMode.Paused); LogAssert.NoUnexpectedReceived();
        }
    }
}
