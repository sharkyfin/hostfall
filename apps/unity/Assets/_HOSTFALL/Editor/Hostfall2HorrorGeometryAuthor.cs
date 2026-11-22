using System;
using System.Linq;
using Hostfall.Core;
using Hostfall.Devices;
using Hostfall.Mapping;
using Hostfall.Rendering;
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
        [MenuItem("HOSTFALL/2.0/Fit Energy Equipment and Traversal")]
        public static void FitHorrorGeometry()
        {
            var scene = OpenClearanceScene();
            var station = Object.FindObjectsByType<ComputerStation>(FindObjectsSortMode.None)
                .Single(device => device.StationId == ComputerCampaign.CircuitStation);
            if (station.transform.Find("Floor-mounted distribution desk") != null) return;

            var root = station.transform;
            root.rotation *= Quaternion.Euler(0, 180, 0);
            foreach (string part in new[] { "Transformer Cabinet", "Breaker Array", "Lamp Support Mast L", "Lamp Support Mast R",
                         "Lamp Cantilever L", "Lamp Cantilever R", "Lamp Hood", "Lamp Blue Lens", "Overhead Blue Task Light" })
                Object.DestroyImmediate(root.Find(part).gameObject);
            var desk = EngineeringFitting("Floor-mounted distribution desk", "HF2_DistributionDesk", root);
            var worktop = Box("Distribution desk worktop", desk, new Vector3(0, .957f, 0),
                new Vector3(1.64f, .084f, .83f), MaterialAt("Dark Metal"), true);
            worktop.GetComponent<MeshRenderer>().enabled = false;
            var baseBox = desk.gameObject.AddComponent<BoxCollider>();
            baseBox.center = new Vector3(0, .48f, -.06f);
            baseBox.size = new Vector3(1.19f, .96f, .68f);

            var bank = FindTransform("Central Generator Core");
            foreach (string part in new[] { "Magnetic Coil 1", "Magnetic Coil 2", "Reactor Containment Ring" })
            {
                var mesh = bank.Find(part);
                var renderer = mesh.GetComponent<MeshRenderer>();
                renderer.enabled = false;
            }
            var equipment = EngineeringFitting("Oil-cooled transformer bank", "HF2_TransformerBank", bank);
            equipment.localPosition = Vector3.up * .8f;
            var transformer = equipment.GetComponentInChildren<MeshRenderer>();
            if (transformer.bounds.size.x < 5 || transformer.bounds.size.y < 2.8f)
                throw new InvalidOperationException("Transformer import scale and axes are incorrect");
            var oldCore = bank.Find("Reactor Containment Ring");
            oldCore.localPosition = Vector3.up * 2.18f;
            oldCore.localScale = new Vector3(4.39f, 2.76f, 4.39f);

            // The new campaign uses a continuous maintenance route between the galleries.
            // The authored legacy scene still owns the switch and moving bridge.
            var bridge = FindTransform("Retractable Bridge P03");
            bridge.position += Vector3.down * .07f;
            var leaf = bridge.Find("Sliding Bridge Leaf");
            leaf.localPosition = Vector3.zero;
            bridge.Find("PDU-SEC02 Breaker Console").gameObject.SetActive(false);
            var metal = MaterialAt("Industrial Metal");
            leaf.Find("Leaf Railing North").gameObject.SetActive(false);
            leaf.Find("Leaf Railing South").gameObject.SetActive(false);
            foreach (float edge in new[] { -.95f, .95f })
            {
                foreach (float y in new[] { .55f, 1.07f })
                    Box("Bridge guardrail", bridge, new Vector3(0, y, edge),
                        new Vector3(7, .063f, .063f), metal, true);
                foreach (float x in new[] { -3.45f, -1.8f, 0, 1.8f, 3.45f })
                    Box("Bridge guardrail stanchion", bridge, new Vector3(x, .58f, edge),
                        new Vector3(.074f, 1.03f, .074f), metal, true);
            }

            FindTransform("MANTIS-6 Reactor Guardian").gameObject.SetActive(false);
            var robots = Object.FindObjectsByType<WatcherController>(FindObjectsSortMode.None);
            var reactor = robots.Single(actor => actor.Id == "reactor-sentry-r22");
            ExpandPatrolBounds(reactor, new Vector2(-24.5f, -2.5f), new Vector2(19, 36.5f));
            ExpandPatrolBounds(robots.Single(actor => actor.Id == "sentinel-r21"), new Vector2(-12, 5.5f), new Vector2(12.5f, 36));
            ExpandPatrolBounds(robots.Single(actor => actor.Id == "sentry-r18"), new Vector2(-2.4f, 2.4f), new Vector2(4.6f, 19));
            ExpandPatrolBounds(robots.Single(actor => actor.Id == "watcher-r17"), new Vector2(-1.2f, 1.2f), new Vector2(1.65f, 10.6f));
            var reserve = Object.Instantiate(reactor.gameObject, reactor.transform.parent);
            reserve.name = "Patrol Robot: aux-watch-energy-r36";
            reserve.transform.SetPositionAndRotation(new Vector3(-22.3f, 0, 34.1f), Quaternion.Euler(0, 90, 0));
            using (var edit = new SerializedObject(reserve.GetComponent<WatcherController>()))
            {
                edit.FindProperty("enemyId").stringValue = "aux-watch-energy-r36";
                edit.FindProperty("patrolA").vector3Value = reserve.transform.position;
                edit.FindProperty("patrolB").vector3Value = new Vector3(-19.4f, 0, 34.1f);
                edit.ApplyModifiedPropertiesWithoutUndo();
            }

            Physics.SyncTransforms();
            PaintWarning(FindTransform("Sector 02 - Compute Vault"), new Vector3(2.55f, 1.64f, 35.8f),
                Vector3.forward, "МЫ ЗДЕСЬ ПОГИБНЕМ");
            PaintWarning(root.parent, new Vector3(-13.8f, 1.68f, 36.15f),
                Vector3.forward, "ОНИ ГОВОРЯТ НАШИМИ ГОЛОСАМИ");
            var memo = Box("Repair memo pinned to the distribution desk", root, new Vector3(.40f, 1.006f, .10f),
                new Vector3(.23f, .012f, .19f), AssetDatabase.LoadAssetAtPath<Material>(EngineeringPath + "InstrumentIvory.mat"), true);
            memo.AddComponent<FieldDocument>().Configure("power-warning");
            Vector3 camera = station.Surface.Centre + Vector3.back * 1.42f + Vector3.up * .43f;
            if (!Physics.Linecast(camera, station.Surface.Centre + Vector3.forward * .025f, out var obstruction,
                    CollisionLayers.InteractionMask, QueryTriggerInteraction.Ignore) ||
                obstruction.collider.name != "Screen interaction surface")
                throw new InvalidOperationException("Distribution screen obstructed by " + obstruction.collider?.name);

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            CaptureContainmentReview("hf2-horror-power-approach", camera + Vector3.back * .78f,
                station.Surface.Centre);
            CaptureContainmentReview("hf2-horror-transformer", new Vector3(-11.1f, 1.75f, 30.6f),
                new Vector3(-16.5f, 2.27f, 28f));
            CaptureContainmentReview("hf2-horror-vault-bridge", new Vector3(4.6f, 4.86f, 25),
                new Vector3(-3.2f, 3.7f, 27));
        }

        [MenuItem("HOSTFALL/2.0/Fit Logistics Reserve Bay")]
        public static void FitLogisticsReserveBay()
        {
            var scene = OpenClearanceScene();
            var lockdown = Object.FindAnyObjectByType<LogisticsLockdown>();
            if (lockdown.transform.Find("Service reserve bay") != null) return;
            var bay = Anchor("Service reserve bay", lockdown.transform, new Vector3(10.52f, 1, 76.1f));
            var steel = MaterialAt("Industrial Metal");
            var dark = MaterialAt("Dark Metal");
            Box("Rear bulkhead", bay, new Vector3(2.55f, 1.04f, 0), new Vector3(.17f, 2.08f, 3.85f), dark, true);
            Box("Structural canopy", bay, new Vector3(1.27f, 2.09f, 0), new Vector3(2.72f, .18f, 3.85f), dark, true);
            foreach (float side in new[] { -1f, 1f })
            {
                Box("Reinforced side wall", bay, new Vector3(1.27f, 1.03f, side * 1.86f),
                    new Vector3(2.72f, 2.06f, .12f), dark, true);
                Box("Shutter track", bay, new Vector3(-.08f, 1.82f, side * 1.86f),
                    new Vector3(.24f, 3.64f, .16f), steel, true);
            }
            Box("Shutter motor housing", bay, new Vector3(-.08f, 3.66f, 0),
                new Vector3(.44f, .25f, 3.92f), steel, true);
            var shutter = Box("Sliding shutter", bay, new Vector3(0, .95f, 0),
                new Vector3(.12f, 1.9f, 3.63f), dark, true);
            shutter.AddComponent<MapGeometrySource>().Configure(false, true);
            for (int i = 0; i < 6; i++)
                Box("Pressed shutter rib", shutter.transform, new Vector3(-.55f, -.42f + .16f * i, 0),
                    new Vector3(.22f, .023f, .96f), steel, false);

            using var bindings = new SerializedObject(lockdown);
            var authored = bindings.FindProperty("reserves");
            var starts = new Vector3[authored.arraySize];
            var exits = new Vector3[authored.arraySize];
            shutter.GetComponent<Collider>().enabled = false;
            Physics.SyncTransforms();
            for (int i = 0; i < authored.arraySize; i++)
            {
                var actor = (WatcherController)authored.GetArrayElementAtIndex(i).objectReferenceValue;
                float z = bay.position.z + (i == 0 ? 0 : i == 1 ? -1.16f : 1.16f);
                starts[i] = new Vector3(12.33f, 1.025f, z);
                exits[i] = new Vector3(9.5f, 1.025f, z);
                foreach (var hit in Physics.CapsuleCastAll(starts[i] + Vector3.up * .49f, starts[i] + Vector3.up * 1.04f,
                    .39f, Vector3.left, Vector3.Distance(starts[i], exits[i]), CollisionLayers.WorldMask, QueryTriggerInteraction.Ignore))
                    if (hit.collider.GetComponentInParent<IDamageable>() == null)
                        throw new InvalidOperationException("Reserve bay lane blocked by " + hit.collider.name);
                for (int step = 0; step <= 6; step++)
                {
                    var point = Vector3.Lerp(starts[i], exits[i], step / 6f) + Vector3.up * .24f;
                    if (!Physics.Raycast(point, Vector3.down, out var floor, .35f, CollisionLayers.WorldMask, QueryTriggerInteraction.Ignore) ||
                        Mathf.Abs(floor.point.y - 1) > .04f)
                        throw new InvalidOperationException("Reserve bay lane has no supporting mezzanine at " + point);
                }
                actor.transform.SetPositionAndRotation(starts[i], Quaternion.Euler(0, -90, 0));
                using var edit = new SerializedObject(actor);
                edit.FindProperty("patrolA").vector3Value = exits[i];
                edit.FindProperty("patrolB").vector3Value = exits[i] + Vector3.left * 1.85f;
                edit.ApplyModifiedPropertiesWithoutUndo();
            }
            shutter.GetComponent<Collider>().enabled = true;
            lockdown.ConfigureReserveBay(shutter.transform, starts, exits);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            CaptureContainmentReview("hf2-horror-logistics-bay", new Vector3(7.0f, 2.63f, 74.8f),
                new Vector3(10.52f, 2.0f, 76.1f));
        }

        private static void PaintWarning(Transform room, Vector3 origin, Vector3 direction, string message)
        {
            var wall = WallContact(origin, direction);
            if (wall.collider == null) throw new InvalidOperationException("Missing wall for " + message);
            var lettering = Anchor("Maintenance wall marking", room, wall.point + wall.normal * .008f);
            lettering.rotation = Quaternion.LookRotation(-wall.normal);
            AddText(message, lettering, Vector3.zero, Quaternion.identity, .105f);
            var text = lettering.GetComponentInChildren<TextMesh>();
            text.font = AssetDatabase.LoadAssetAtPath<Font>(FacilityBuilder.Content + "/Fonts/RobotoMono-Regular.ttf");
            text.characterSize = .0084f;
            text.fontSize = 64;
            text.color = new Color(.55f, .243f, .167f);
            lettering.GetComponentInChildren<WorldTextDepth>().Configure(Shader.Find("HOSTFALL/World Text"));
        }

        private static void ExpandPatrolBounds(WatcherController actor, Vector2 x, Vector2 z)
        {
            using var edit = new SerializedObject(actor);
            edit.FindProperty("xBounds").vector2Value = x;
            edit.FindProperty("zBounds").vector2Value = z;
            edit.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
