using System.IO;
using System.Linq;
using Hostfall.Mapping;
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
        private const float ShuttleHalfDepth = 1.45f;
        private const float DockClearance = 2.24f;

        [MenuItem("HOSTFALL/2.0/Author Recovery Service Shuttle")]
        public static void AuthorRecoveryServiceShuttle()
        {
            WorkstationWorkshop.RequireCopy();
            EditorSceneManager.OpenScene(PreviewScene);
            if (Object.FindAnyObjectByType<RecoveryTransitController>() != null) return;
            File.Copy(PreviewScene, Path.Combine(Output, "scene-before-recovery.unity"), true);

            var shelterWall = FindTransform("Shelter South Wall");
            var dispatchWall = FindTransform("Dispatch north east seal");
            var home = PosePosition(RecoveryTransit.Shelter);
            var ring = PosePosition(RecoveryTransit.Ring);
            var dark = MaterialAt("Dark Metal");
            var metal = MaterialAt("Industrial Metal");
            var floor = MaterialAt("Floor Panels");
            var cyan = MaterialAt("Indicator Green");
            var amber = MaterialAt("Indicator Amber");

            CutOpening(shelterWall, Vector3.right, home.x, DockClearance, 2.52f);
            CutOpening(dispatchWall, Vector3.right, ring.x, DockClearance, 2.52f);
            var root = Anchor("Recovery / service shuttle and two shaft stations", dispatchWall.root, Vector3.zero);
            var controller = root.gameObject.AddComponent<RecoveryTransitController>();
            var lift = Anchor("Service shuttle car", root, home);
            lift.rotation = Quaternion.Euler(0, RecoveryTransit.Shelter.Yaw, 0);
            lift.gameObject.AddComponent<MapGeometrySource>().Configure(false, true);
            Box("Shuttle cabin floor", lift, new Vector3(0, -.09f, 0), new Vector3(2.64f, .18f, ShuttleHalfDepth * 2), floor, true);
            Box("Shuttle cabin rear wall", lift, new Vector3(0, 1.25f, ShuttleHalfDepth), new Vector3(2.64f, 2.5f, .14f), dark, true);
            Box("Shuttle roof", lift, new Vector3(0, 2.55f, 0), new Vector3(2.64f, .14f, ShuttleHalfDepth * 2), metal, true);
            foreach (int side in new[] { -1, 1 })
            {
                Box("Car interior wall", lift, new Vector3(1.31f * side, 1.25f, 0), new Vector3(.12f, 2.5f, ShuttleHalfDepth * 2), dark, true);
                Box("Car handrail", lift, new Vector3(1.15f * side, .97f, .1f), new Vector3(.075f, .06f, 2.18f), metal, false);
                Box("Car guide light", lift, new Vector3(1.225f * side, 2.22f, .11f), new Vector3(.036f, .036f, 1.9f), cyan, false);
            }
            AddText("СЛУЖЕБНЫЙ ТРАНСПОРТ", lift, new Vector3(0, 1.8f, 1.36f), Quaternion.identity, .049f);
            AddText("УБЕЖИЩЕ  ↔  ЦЕНТРАЛЬНОЕ КОЛЬЦО", lift, new Vector3(0, .69f, 1.36f), Quaternion.identity, .032f);
            var cabinDoor = MakeShuttleDoor(lift, new Vector3(0, 0, -ShuttleHalfDepth), dark, metal, amber);
            MakeCallPanel(lift, new Vector3(.54f, 1.17f, 1.348f), true, TransitStop.Shelter, controller, dark, cyan);

            Transform shelterDoor = BuildShuttleLanding(root, home, -19.25f, TransitStop.Shelter, controller, dark, metal, floor, cyan, amber);
            Transform ringDoor = BuildShuttleLanding(root, ring, 125.0f, TransitStop.Ring, controller, dark, metal, floor, cyan, amber);

            var lower = home;
            var upper = ring;
            lower.y = upper.y = RecoveryTransit.TunnelY;
            var line = Anchor("Shuttle guide tunnel", root, (lower + upper) * .5f);
            line.rotation = Quaternion.LookRotation(upper - lower);
            float length = Vector3.Distance(lower, upper);
            Box("Guide tunnel slab", line, new Vector3(0, -.32f, 0), new Vector3(5.45f, .23f, length + 3.5f), floor, true);
            Box("Guide tunnel roof", line, new Vector3(0, 3.1f, 0), new Vector3(5.45f, .2f, length - 9f), dark, true);
            foreach (int side in new[] { -1, 1 })
            {
                Box("Guide tunnel wall", line, new Vector3(2.86f * side, 1.42f, 0), new Vector3(.29f, 3.36f, length - 5.4f), dark, true);
                Box("Linear motor rail", line, new Vector3(.91f * side, -.18f, 0), new Vector3(.14f, .058f, length), metal, false);
            }

            var lamps = new[]
            {
                AddRelayLight(root, "Shelter transfer relay", new Vector3(0, 2.6f, -18.7f), 5.5f),
                AddRelayLight(root, "Energy transfer relay", new Vector3(-16.5f, 2.6f, 35), 6.8f),
                AddRelayLight(root, "Dispatch transfer relay", new Vector3(38, 2.55f, 124.4f), 7.2f),
                AddRelayLight(root, "Core quarantine relay", new Vector3(29.4f, 3.6f, 128.7f), 8.6f),
                AddRelayLight(lift, "Cabin service lamp", new Vector3(0, 2.39f, 0), 4f)
            };

            var emitter = lamps[0].gameObject.AddComponent<AudioSource>();
            emitter.spatialBlend = .82f;
            emitter.minDistance = 3.5f;
            emitter.maxDistance = 18f;
            emitter.playOnAwake = false;
            controller.Configure(lift, cabinDoor, shelterDoor, ringDoor, lamps, emitter);

            var gate = Object.FindObjectsByType<FacilityDoor>(FindObjectsSortMode.None)
                .Single(door => door.DoorId == ComputerCampaign.RecoveryCoreGate);
            var gateData = new SerializedObject(gate);
            gateData.FindProperty("condition").enumValueIndex = (int)DoorAccessCondition.ManualUnlockOnly;
            gateData.FindProperty("lockedPromptText").stringValue = "Ядро изолировано · завершите Recovery Protocol в убежище";
            gateData.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            CaptureContainmentReview("hf2-recovery-home-dock", new Vector3(1.65f, 1.68f, -15.9f), new Vector3(0, 1.38f, -19.67f));
            CaptureContainmentReview("hf2-recovery-ring-dock", new Vector3(36.2f, 1.68f, 122.15f), new Vector3(38, 1.4f, 125.62f));
        }

        private static Vector3 PosePosition(TransitPose pose) => new(pose.X, pose.Y, pose.Z);

        private static Transform BuildShuttleLanding(Transform root, Vector3 center, float roomEdge, TransitStop which,
            RecoveryTransitController controller, Material dark, Material metal, Material floor, Material cyan, Material amber)
        {
            bool home = which == TransitStop.Shelter;
            var shell = Anchor(which + " transfer shaft", root, center);
            shell.rotation = Quaternion.Euler(0, home ? 180 : 0, 0);
            float distance = Mathf.Abs(roomEdge - center.z);
            float porchDepth = distance - ShuttleHalfDepth;
            float porchCenter = -(ShuttleHalfDepth + porchDepth * .5f);
            Box("Shuttle landing floor", shell, new Vector3(0, -.095f, porchCenter), new Vector3(2.86f, .19f, porchDepth), floor, true);
            Box("Shuttle landing roof", shell, new Vector3(0, 3.12f, porchCenter), new Vector3(3.54f, .14f, porchDepth), dark, true);
            Box("Transfer shaft roof", shell, new Vector3(0, 3.12f, .04f), new Vector3(3.54f, .14f, 3.55f), dark, true);
            foreach (int side in new[] { -1, 1 })
            {
                Box("Landing side wall", shell, new Vector3(1.62f * side, 1.51f, porchCenter),
                    new Vector3(.24f, 3.14f, porchDepth + .12f), dark, true);
                Box("Transfer shaft side wall", shell, new Vector3(1.62f * side, -2.01f, .03f),
                    new Vector3(.24f, 10.2f, 3.49f), dark, true);
                Box("Guide shaft vertical runner", shell, new Vector3(1.47f * side, -2.01f, .35f),
                    new Vector3(.10f, 10.2f, .15f), metal, false);
            }
            Box("Transfer shaft rear wall", shell, new Vector3(0, -2.01f, 1.72f),
                new Vector3(3.47f, 10.2f, .21f), dark, true);
            foreach (int side in new[] { -1, 1 })
                Box("Transfer landing jamb", shell, new Vector3(1.275f * side, 1.32f, -1.57f), new Vector3(.29f, 2.64f, .21f), metal, true);
            Box("Transfer landing lintel", shell, new Vector3(0, 2.58f, -1.57f), new Vector3(2.83f, .17f, .25f), metal, true);
            var hatch = MakeShuttleDoor(shell, new Vector3(0, 0, -1.58f), dark, metal, amber);
            MakeCallPanel(shell, new Vector3(1.29f, 1.19f, -(distance - .30f)), false, which, controller, dark, cyan);

            string name = home ? "ТРАНСПОРТ · КОЛЬЦО" : "ТРАНСПОРТ · УБЕЖИЩЕ";
            Box("Transfer landing sign backing", shell, new Vector3(0, 2.68f, -distance + .12f), new Vector3(2.95f, .29f, .11f), dark, false);
            AddText(name, shell, new Vector3(0, 2.68f, -distance + .058f), Quaternion.identity, .043f);
            return hatch;
        }

        private static Transform MakeShuttleDoor(Transform parent, Vector3 localPosition, Material dark, Material metal, Material amber)
        {
            var gate = Anchor("Interlocked shuttle leaf", parent, parent.TransformPoint(localPosition));
            gate.gameObject.AddComponent<MapGeometrySource>().Configure(false, true);
            Box("Solid shuttle hatch", gate, new Vector3(0, 1.20f, 0), new Vector3(DockClearance + .03f, 2.4f, .085f), dark, true);
            Box("Shuttle hatch inset", gate, new Vector3(0, 1.34f, -.065f), new Vector3(1.98f, 1.77f, .05f), metal, false);
            Box("Shuttle hatch amber seal", gate, new Vector3(0, .23f, -.078f), new Vector3(1.99f, .032f, .026f), amber, false);
            return gate;
        }

        private static void MakeCallPanel(Transform parent, Vector3 position, bool aboard, TransitStop which,
            RecoveryTransitController controller, Material dark, Material cyan)
        {
            var plate = Anchor(aboard ? "Cabin route selection" : which + " shuttle call plate", parent, parent.TransformPoint(position));
            Box("Shuttle control housing", plate, Vector3.zero, new Vector3(.22f, .34f, .105f), dark, true);
            Box("Shuttle control pad", plate, new Vector3(0, .025f, -.061f), new Vector3(.112f, .091f, .023f), cyan, false);
            plate.gameObject.AddComponent<RecoveryTransitPanel>().Configure(controller, which, aboard);
        }

        private static Light AddRelayLight(Transform parent, string name, Vector3 offset, float range)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = offset;
            var light = obj.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = range;
            light.shadows = LightShadows.None;
            light.enabled = false;
            return light;
        }
    }
}
