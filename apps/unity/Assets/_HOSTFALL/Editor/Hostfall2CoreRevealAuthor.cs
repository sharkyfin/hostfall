using System.IO;
using System.Linq;
using Hostfall.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hostfall.Editor
{
    public static partial class Hostfall2Workbench
    {
        [MenuItem("HOSTFALL/2.0/Author Core Reveal")]
        public static void AuthorCoreReveal()
        {
            WorkstationWorkshop.RequireCopy();
            var scene = EditorSceneManager.OpenScene(PreviewScene);
            if (Object.FindAnyObjectByType<CoreRevealSequence>() != null) return;
            var avatar = Object.FindAnyObjectByType<CoreAvatarEncounter>();
            var sector = avatar.transform.parent;
            File.Copy(PreviewScene, Path.Combine(Output, "scene-before-core-reveal.unity"), true);

            var steel = MaterialAt("Industrial Metal");
            var dark = MaterialAt("Dark Metal");
            var green = MaterialAt("Indicator Green");
            var amber = MaterialAt("Indicator Amber");
            var terminal = Anchor("SUPER / physical handoff report", sector, new Vector3(10, 1.35f, 185.25f));
            Box("Terminal pedestal", terminal, new Vector3(0, -.72f, .04f), new Vector3(.76f, 1.22f, .68f), steel, true);
            var frame = Box("Data display frame", terminal, new Vector3(0, .04f, 0), new Vector3(1.77f, 1.02f, .2f), steel, true);
            Box("Nonreflective glass", terminal, new Vector3(0, .04f, -.112f), new Vector3(1.63f, .9f, .038f), dark, false);
            Box("Relay status strip", terminal, new Vector3(0, -.48f, -.112f), new Vector3(1.62f, .026f, .028f), amber, false);
            AddText("ОТЧЁТ ВОССТАНОВЛЕНИЯ", terminal, new Vector3(0, .14f, -.144f), Quaternion.identity, .047f);
            var status = terminal.GetComponentInChildren<TextMesh>();
            status.anchor = TextAnchor.MiddleCenter;
            status.alignment = TextAlignment.Center;
            status.color = new Color(.55f, .94f, .81f);

            var terminalShot = Anchor("Reveal shot / terminal", sector, new Vector3(10.36f, 1.62f, 183.2f));
            terminalShot.rotation = Quaternion.LookRotation(terminal.position - terminalShot.position);
            var exitShot = Anchor("Reveal shot / evacuation window", sector, new Vector3(9.38f, 2.1f, 202.55f));
            exitShot.rotation = Quaternion.identity;
            var horizon = Anchor("Reveal target / external relay valley", sector, new Vector3(10, 1.93f, 230));

            var outside = Anchor("Exterior relay district", sector, new Vector3(10, 0, 224));
            Box("Valley shelf", outside, new Vector3(0, -1.65f, 13), new Vector3(70, 1.5f, 60), dark, false);
            var stations = new GameObject[3];
            Vector3[] pads = { new(-7.9f, 0, 1.5f), new(4.6f, 1.3f, 9), new(-2.8f, 1.8f, 21) };
            for (int i = 0; i < pads.Length; i++)
            {
                var at = pads[i];
                float height = 3.1f + i * .7f;
                Box("Relay mast " + (i + 1), outside, at + Vector3.up * (height * .45f), new Vector3(.48f, height, .57f), steel, false);
                Box("Relay dish", outside, at + new Vector3(0, height * .84f, -.27f), new Vector3(1.8f, .54f, .75f), steel, false);
                var power = Box("Restored relay indicator " + (i + 1), outside, at + new Vector3(0, height * .84f, -.67f),
                    new Vector3(1.6f, .12f, .08f), green, false);
                var lamp = power.AddComponent<Light>();
                lamp.color = new Color(.36f, .88f, .62f);
                lamp.intensity = 5;
                lamp.range = 5.6f;
                stations[i] = power;
                power.SetActive(false);
            }

            var exit = Object.FindObjectsByType<FacilityDoor>(FindObjectsSortMode.None).Single(d => d.DoorId == "door.sec10.evac");
            // The scanner resolves an interactable through ancestors of the visible display collider.
            var sequence = terminal.gameObject.AddComponent<CoreRevealSequence>();
            sequence.Configure(exit, status, terminalShot, exitShot, horizon, stations);
            frame.GetComponent<BoxCollider>().size = new Vector3(1, 1, 1.3f);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        [MenuItem("HOSTFALL/2.0/Review Core Avatar")]
        public static void ReviewCoreAvatar()
        {
            WorkstationWorkshop.RequireCopy();
            EditorSceneManager.OpenScene(PreviewScene);
            CaptureContainmentReview("hf2-core-avatar", new Vector3(11.9f, 3.1f, 140.6f), new Vector3(10, 3.1f, 153));
            CaptureContainmentReview("hf2-core-relay-horizon", new Vector3(9.38f, 2.1f, 202.55f), new Vector3(10, 1.93f, 230));
        }
    }
}
