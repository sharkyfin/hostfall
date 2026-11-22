using System;
using System.IO;
using System.Linq;
using Hostfall.Mapping;
using Hostfall.Simulation.Computers;
using Hostfall.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace Hostfall.Editor
{
    public static partial class Hostfall2Workbench
    {
        [MenuItem("HOSTFALL/2.0/Author Robotics Containment")]
        public static void AuthorRoboticsContainment()
        {
            WorkstationWorkshop.RequireCopy();
            EditorSceneManager.OpenScene(PreviewScene);
            if (Object.FindAnyObjectByType<ContainmentWorldController>() != null) return;
            File.Copy(PreviewScene, Path.Combine(Output, "scene-before-containment.unity"), true);

            var robotics = FindTransform("Sector 06 - Robotics Plant");
            var sentry = Object.FindObjectsByType<WatcherController>(FindObjectsSortMode.None)
                .Single(actor => actor.Id == "assembly-sentry-r26");
            var ringGate = Object.FindObjectsByType<FacilityDoor>(FindObjectsSortMode.None)
                .Single(door => door.DoorId == ComputerCampaign.ContainmentAccess);
            var gateData = new SerializedObject(ringGate);
            gateData.FindProperty("condition").enumValueIndex = (int)DoorAccessCondition.ManualUnlockOnly;
            gateData.FindProperty("lockedPromptText").stringValue = "Кольцо закрыто · изолируйте контур R26 на пульте RBT-01";
            gateData.ApplyModifiedPropertiesWithoutUndo();

            var dark = MaterialAt("Dark Metal");
            var steel = MaterialAt("Industrial Metal");
            var amber = MaterialAt("Indicator Amber");
            var green = MaterialAt("Indicator Green");
            var root = Anchor("RBT-01 quarantine lane", robotics, new Vector3(42, 0, 80.7f));
            var controller = root.gameObject.AddComponent<ContainmentWorldController>();
            const float length = 6.6f, width = 3.48f;
            foreach (int side in new[] { -1, 1 })
            {
                float x = side * 1.87f;
                Box("Quarantine side wall", root, new Vector3(x, .92f, 0), new Vector3(.25f, 1.84f, length), dark, true);
                Box("Quarantine wall base", root, new Vector3(x, .11f, 0), new Vector3(.34f, .22f, length), steel, false);
                Box("Raised cage rail", root, new Vector3(x, 2.5f, 0), new Vector3(.14f, .17f, length), steel, true);
                for (int i = 0; i < 13; i++)
                    Box("Cage guard bar", root, new Vector3(x, 2.17f, -3.1f + .52f * i),
                        new Vector3(.075f, .75f, .064f), steel, true);
            }

            var south = BuildContainmentLeaf(root, -length * .5f, width, "ЮЖНАЯ СТВОРКА", dark, steel, amber);
            var north = BuildContainmentLeaf(root, length * .5f, width, "СЕВЕРНАЯ СТВОРКА", dark, steel, amber);
            var bay = ContainmentSensor(root, "Bay presence sensor", new Vector3(0, 1.12f, 0), new Vector3(width, 2.24f, 5.55f));
            var southSensor = ContainmentSensor(root, "South interlock curtain", new Vector3(0, 1.28f, -length * .5f), new Vector3(width, 2.55f, .86f));
            var northSensor = ContainmentSensor(root, "North interlock curtain", new Vector3(0, 1.28f, length * .5f), new Vector3(width, 2.55f, .86f));
            var calibrationA = Anchor("R26 diagnostics A", root, new Vector3(42, 0, 70.7f));
            var calibrationB = Anchor("R26 diagnostics B", root, new Vector3(42, 0, 74.8f));
            var captureA = Anchor("R26 holding A", root, new Vector3(42, 0, 80f));
            var captureB = Anchor("R26 holding B", root, new Vector3(42, 0, 81.4f));

            ContainmentReleasePanel(root, controller, ContainmentShutter.South, new Vector3(1.65f, 1.25f, -2.65f), 90, green);
            ContainmentReleasePanel(root, controller, ContainmentShutter.North, new Vector3(1.65f, 1.25f, 2.65f), 90, green);
            ContainmentReleasePanel(ringGate.transform, controller, ContainmentShutter.North,
                new Vector3(1.65f, 1.25f, .50f), 180, green, true);

            var light = new GameObject("Quarantine status relay").AddComponent<Light>();
            light.transform.SetParent(root, false);
            light.transform.localPosition = new Vector3(0, 3.45f, .05f);
            light.type = LightType.Point;
            light.intensity = 2.8f;
            light.range = 5.4f;
            light.color = new Color(1, .46f, .12f);
            var spot = new GameObject("Bay inspection fixture").AddComponent<Light>();
            spot.transform.SetParent(root, false);
            spot.transform.localPosition = new Vector3(0, 5.05f, .15f);
            spot.transform.localRotation = Quaternion.Euler(90, 0, 0);
            spot.type = LightType.Spot;
            spot.color = new Color(.69f, .83f, .99f);
            spot.intensity = 17;
            spot.range = 7;
            spot.spotAngle = 77;
            Box("Bay inspection housing", root, new Vector3(0, 5.14f, .15f), new Vector3(.63f, .18f, 1.38f), dark, false);

            var cameras = new[]
            {
                ContainmentCameraAnchor(root, "C01", new Vector3(44.25f, 3.65f, 74.15f), new Vector3(42, 1.05f, 76.6f), dark, steel),
                ContainmentCameraAnchor(root, "C02", new Vector3(43.45f, 3.64f, 79.85f), new Vector3(41.8f, .78f, 81.9f), dark, steel),
                ContainmentCameraAnchor(root, "C03", new Vector3(44.75f, 3.66f, 86.85f), new Vector3(42, .92f, 83.5f), dark, steel)
            };
            var camera = new GameObject("RBT-01 single camera render device").AddComponent<Camera>();
            camera.transform.SetParent(root, false);
            camera.enabled = false;
            camera.allowHDR = false;
            camera.allowMSAA = false;
            camera.nearClipPlane = .065f;
            camera.farClipPlane = 34;
            camera.fieldOfView = 64;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.025f, .04f, .045f);
            var cameraData = camera.GetUniversalAdditionalCameraData();
            cameraData.renderShadows = false;
            cameraData.renderPostProcessing = false;
            cameraData.requiresColorOption = CameraOverrideOption.Off;
            cameraData.requiresDepthOption = CameraOverrideOption.Off;

            controller.Configure(sentry, ringGate, south, north, bay, southSensor, northSensor, calibrationA, calibrationB,
                captureA, captureB, camera, cameras, light);
            south.localPosition += Vector3.up * 2.95f;
            north.localPosition += Vector3.up * 2.95f;
            CaptureContainmentReview("hf2-containment-lane", new Vector3(47.2f, 2.3f, 72.8f), new Vector3(42, 1.75f, 81.4f));
            CaptureContainmentReview("hf2-containment-catwalk", new Vector3(40.2f, 5.55f, 91.15f), new Vector3(42, 1.4f, 81.2f));
            south.localPosition -= Vector3.up * 2.95f;
            north.localPosition -= Vector3.up * 2.95f;

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        private static Transform BuildContainmentLeaf(Transform parent, float z, float width, string label,
            Material dark, Material steel, Material amber)
        {
            var portal = Anchor("Quarantine portal / " + label, parent, parent.position + Vector3.forward * z);
            foreach (int side in new[] { -1, 1 })
            {
                float x = side * (width + .22f) * .5f;
                Box("Shutter jamb", portal, new Vector3(x, 2.68f, 0), new Vector3(.22f, 5.36f, .4f), dark, true);
                Box("Shutter guide", portal, new Vector3(x - side * .10f, 2.65f, -.215f), new Vector3(.07f, 5.0f, .06f), steel, false);
                Box("Interlock beam transmitter", portal, new Vector3(x - side * .13f, .53f, -.14f),
                    new Vector3(.075f, .17f, .16f), amber, false);
            }

            Box("Shutter drive head", portal, new Vector3(0, 5.54f, 0), new Vector3(width + .48f, .35f, .64f), dark, true);
            float front = z < 0 ? -1 : 1;
            AddText(label, portal, new Vector3(0, 5.53f, front * .328f),
                Quaternion.Euler(0, z < 0 ? 0 : 180, 0), .042f);
            var leaf = Anchor("Motorized bay leaf", portal, portal.position);
            leaf.gameObject.AddComponent<MapGeometrySource>().Configure(false, true);
            Box("Solid leaf core", leaf, new Vector3(0, 1.30f, 0), new Vector3(width, 2.6f, .22f), dark, true);
            foreach (int face in new[] { -1, 1 })
            {
                for (int i = 0; i < 5; i++)
                {
                    var rib = Box("Reinforced leaf rib", leaf, new Vector3(0, .24f + i * .54f, .128f * face),
                        new Vector3(width - .14f, .085f, .058f), steel, false);
                    rib.isStatic = false;
                }
                Box("Leaf edge marker", leaf, new Vector3(0, .14f, .151f * face),
                    new Vector3(width - .14f, .036f, .024f), amber, false);
            }
            return leaf;
        }

        private static Transform Anchor(string name, Transform parent, Vector3 point)
        {
            var anchor = new GameObject(name).transform;
            anchor.SetParent(parent, false);
            anchor.position = point;
            return anchor;
        }

        private static BoxCollider ContainmentSensor(Transform root, string name, Vector3 center, Vector3 size)
        {
            var volume = new GameObject(name);
            volume.transform.SetParent(root, false);
            volume.transform.localPosition = center;
            var collider = volume.AddComponent<BoxCollider>();
            collider.size = size;
            collider.isTrigger = true;
            return collider;
        }

        private static void ContainmentReleasePanel(Transform parent, ContainmentWorldController source,
            ContainmentShutter shutter, Vector3 localPosition, float yaw, Material green, bool fromRing = false)
        {
            var panel = Box("Local emergency release", parent, localPosition, new Vector3(.25f, .31f, .17f),
                MaterialAt("Dark Metal"), true);
            panel.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            Box("Release confirmation pad", panel.transform, new Vector3(0, 0, -.57f),
                new Vector3(.63f, .43f, .21f), green, false);
            panel.AddComponent<ContainmentEscapePanel>().Configure(source, shutter, fromRing);
        }

        private static Transform ContainmentCameraAnchor(Transform root, string label, Vector3 point, Vector3 target,
            Material dark, Material steel)
        {
            var anchor = Anchor(label + " lens", root, point);
            anchor.LookAt(target);
            Vector3 mount = anchor.position - root.position - anchor.forward * .31f;
            float supportFloor = label == "C02" ? 2.48f : 0;
            float height = point.y - supportFloor;
            Box("CCTV vertical mount", root, new Vector3(mount.x, supportFloor + height * .5f, mount.z),
                new Vector3(.115f, height, .115f), steel, label != "C02");
            if (label == "C02")
                Box("CCTV rail mount", root, new Vector3((mount.x + 1.87f) * .5f, supportFloor, mount.z),
                    new Vector3(Mathf.Abs(mount.x - 1.87f) + .1f, .105f, .12f), steel, false);
            Box("CCTV enclosure", anchor, new Vector3(0, .015f, -.22f), new Vector3(.32f, .29f, .49f), dark, false);
            Box("Camera lens surround", anchor, new Vector3(0, 0, -.028f), new Vector3(.23f, .19f, .08f), steel, false);
            return anchor;
        }

        private static void CaptureContainmentReview(string name, Vector3 point, Vector3 target)
        {
            WorkstationWorkshop.Capture(name, point, target);
            File.Copy(Path.Combine(WorkstationWorkshop.Output, name + ".png"), Path.Combine(Output, name + ".png"), true);
        }
    }
}
