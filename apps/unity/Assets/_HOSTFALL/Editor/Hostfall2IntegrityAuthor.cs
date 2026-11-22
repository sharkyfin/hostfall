using System.IO;
using System.Linq;
using Hostfall.Devices;
using Hostfall.Simulation.Computers;
using Hostfall.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hostfall.Editor
{
    public static partial class Hostfall2Workbench
    {
        [MenuItem("HOSTFALL/2.0/Author Core Service Telemetry")]
        public static void AuthorCoreServiceTelemetry()
        {
            WorkstationWorkshop.RequireCopy();
            EditorSceneManager.OpenScene(PreviewScene);
            if (Object.FindAnyObjectByType<IntegrityTelemetryController>() != null) return;

            File.Copy(PreviewScene, Path.Combine(Output, "scene-before-integrity.unity"), true);
            var station = Object.FindObjectsByType<ComputerStation>(FindObjectsSortMode.None)
                .Single(device => device.StationId == ComputerCampaign.IntegrityStation);
            var ring = FindTransform("Sector 09 - Cloud Control");
            station.transform.SetParent(ring, true);
            station.transform.SetPositionAndRotation(new Vector3(34.4f, 0, 123.3f), Quaternion.identity);
            station.name = "SUPER-10 / service audit of the quarantine boundary";

            var steel = MaterialAt("Industrial Metal");
            var dark = MaterialAt("Dark Metal");
            var green = MaterialAt("Indicator Green");
            var amber = MaterialAt("Indicator Amber");
            Box("Service terminal identification panel", station.transform, new Vector3(0, .83f, -.504f),
                new Vector3(.96f, .24f, .045f), steel, false);
            AddText("SUPER-10", station.transform, new Vector3(0, .84f, -.531f), Quaternion.identity, .039f);
            Box("Quarantine console status rail", station.transform, new Vector3(0, .65f, -.513f),
                new Vector3(.82f, .022f, .035f), amber, false);

            string[] towers = { "Distribution Tower Power", "Distribution Tower Cryo", "Distribution Tower Optical" };
            string[] labels = { "СИЛОВАЯ СТОЙКА", "ОХЛАЖДЕНИЕ", "ОПТИКА" };
            var markers = new GameObject[towers.Length];
            for (int index = 0; index < towers.Length; index++)
            {
                var tower = FindTransform(towers[index]);
                // Tower objects hold scaled cube meshes. The plate is a sibling
                // so its metric dimensions are independent of tower geometry scale.
                var body = Anchor(labels[index] + " / service identification", tower.parent,
                    tower.position + new Vector3(-1.305f, -1.1f, 0));
                body.rotation = Quaternion.Euler(0, 90, 0);
                Box("Service telemetry backplate", body, Vector3.zero, new Vector3(1.61f, .61f, .09f), dark, false);
                Box("Service telemetry lens", body, new Vector3(0, -.22f, -.053f), new Vector3(1.37f, .053f, .03f), green, false);
                Box("Telemetry panel emergency contacts", body, new Vector3(0, .206f, -.050f), new Vector3(1.36f, .047f, .035f), steel, false);
                AddText(labels[index], body, new Vector3(0, .014f, -.061f), Quaternion.identity, .054f);
                markers[index] = body.gameObject;
                body.gameObject.SetActive(false);
            }

            station.gameObject.AddComponent<IntegrityTelemetryController>().Configure(markers);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            CaptureContainmentReview("hf2-integrity-station", new Vector3(32.65f, 1.7f, 120.8f), new Vector3(34.4f, 1.25f, 123.3f));
        }
    }
}
