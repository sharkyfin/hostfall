using System;
using System.IO;
using System.Linq;
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
        private const float MinefieldSpacing = 1.3f;

        [MenuItem("HOSTFALL/2.0/Author Security Sensor Floor")]
        public static void AuthorSecuritySensorFloor()
        {
            WorkstationWorkshop.RequireCopy();
            EditorSceneManager.OpenScene(PreviewScene);
            if (Object.FindAnyObjectByType<MinefieldFloorController>() != null)
                return;

            File.Copy(PreviewScene, Path.Combine(Output, "scene-before-minefield.unity"), true);
            var sector = FindTransform("Sector 07 - Security Citadel");
            var station = Object.FindObjectsByType<Hostfall.Devices.ComputerStation>(FindObjectsSortMode.None)
                .Single(c => c.StationId == ComputerCampaign.MinefieldStation);
            var entry = Object.FindObjectsByType<FacilityDoor>(FindObjectsSortMode.None).Single(d => d.DoorId == "door.sec07.citadel");
            var geometry = new GameObject("SEC-07 / synchronized sensor floor").transform;
            geometry.SetParent(sector, false);
            var metal = MaterialAt("Dark Metal");
            var red = MaterialAt("Indicator Amber");
            var green = MaterialAt("Indicator Green");

            var sensors = new BoxCollider[MinefieldSession.CellCount];
            var beams = new Renderer[sensors.Length];
            var markers = new Renderer[sensors.Length];
            var initial = new MinefieldSession();
            Physics.SyncTransforms();
            for (int cell = 0; cell < sensors.Length; cell++)
            {
                int column = cell % MinefieldSession.Side, row = cell / MinefieldSession.Side;
                string label = ((char)('A' + column)).ToString() + (row + 1);
                Vector3 center = new(44.75f + column * MinefieldSpacing, 0, 27.05f + row * MinefieldSpacing);
                if (!Physics.Raycast(center + Vector3.up * .65f, Vector3.down, out var floor, .85f,
                        Hostfall.Core.CollisionLayers.WorldMask, QueryTriggerInteraction.Ignore) ||
                    Mathf.Abs(floor.point.y) > .065f)
                    throw new InvalidOperationException("SEC-07 sensor cell lacks clear floor: " + label);

                var tile = new GameObject("Sensor cell " + label).transform;
                tile.SetParent(geometry, false);
                tile.position = center;
                var sensor = tile.gameObject.AddComponent<BoxCollider>();
                sensor.center = new Vector3(0, .22f, 0);
                sensor.size = new Vector3(1.27f, .44f, 1.27f);
                sensor.isTrigger = true;
                sensors[cell] = sensor;

                Box("Numbered steel plate", tile, new Vector3(0, .013f, 0), new Vector3(1.24f, .025f, 1.24f), metal, false);
                var projector = Box("Laser emitter", tile, new Vector3(-.52f, .07f, 0), new Vector3(.14f, .12f, .18f), metal, false);
                beams[cell] = Box("Armed light beam", tile, new Vector3(.05f, .105f, 0), new Vector3(1.02f, .024f, .024f), red, false)
                    .GetComponent<Renderer>();
                markers[cell] = Box("Isolated sensor LED", projector.transform, new Vector3(0, .61f, 0), new Vector3(.52f, .19f, .45f), green, false)
                    .GetComponent<Renderer>();
                AddText(label, tile, new Vector3(0, .034f, -.33f), Quaternion.Euler(90, 0, 0), .043f);
                bool armed = initial.IsArmed(cell);
                sensor.enabled = beams[cell].enabled = armed;
                markers[cell].enabled = false;
            }

            var divider = FindTransform("S07 CCTV-Archive Divider");
            CutOpening(divider, Vector3.right, 47.35f, 2.25f, 3.4f);
            var vaultExit = CreateSensorGate(ComputerCampaign.MinefieldExit, sector, new Vector3(47.35f, 0, 34.8f), 0, metal);
            AddText("ПОСТ НАБЛЮДЕНИЯ", vaultExit.transform, new Vector3(0, 3.58f, -.25f), Quaternion.identity, .07f);

            // The CCTV exterior wall and adjoining office wall both cover this seam.
            CutOpening(FindTransform("S07 CCTV East Wall"), Vector3.forward, 43, 2.25f, 3.4f);
            CutOpening(FindTransform("S08 Office West Wall"), Vector3.forward, 43, 2.25f, 3.4f);
            var officePass = CreateSensorGate(ComputerCampaign.MinefieldNeuroPass, sector, new Vector3(54, 0, 43), 90, metal);
            AddText("НЕЙРОАРХИВ", officePass.transform, new Vector3(0, 3.58f, -.47f), Quaternion.identity, .07f);
            RelocateSecurityFixtures();

            var alarm = new GameObject("SEC-07 local floor alarm");
            alarm.transform.SetParent(geometry, false);
            alarm.transform.position = station.transform.position + new Vector3(-1.6f, 2.2f, .25f);
            var light = alarm.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1, .15f, .06f);
            light.range = 7;
            light.intensity = 3;
            light.shadows = LightShadows.None;
            light.enabled = false;
            var audio = alarm.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.spatialBlend = 1;
            audio.minDistance = 2;
            audio.maxDistance = 15;

            geometry.gameObject.AddComponent<MinefieldFloorController>()
                .Configure(entry, sensors, beams, markers, light, audio, MinefieldSpacing);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            WorkstationWorkshop.Capture("hf2-security-minefield", new Vector3(44.3f, 2.3f, 24.9f), new Vector3(48, .7f, 31));
            File.Copy(Path.Combine(WorkstationWorkshop.Output, "hf2-security-minefield.png"),
                Path.Combine(Output, "hf2-security-minefield.png"), true);
            Debug.Log("SEC-07 sensor floor, CCTV exit and office passage authored.");
        }

        [MenuItem("HOSTFALL/2.0/Clear New SEC-07 Passage")]
        public static void ClearNewSecurityPassage()
        {
            WorkstationWorkshop.RequireCopy();
            EditorSceneManager.OpenScene(PreviewScene);
            if (Object.FindAnyObjectByType<MinefieldFloorController>() == null)
                throw new InvalidOperationException("Author the sensor floor before repairing its passage.");
            File.Copy(PreviewScene, Path.Combine(Output, "scene-before-minefield-cart.unity"), true);
            RelocateSecurityFixtures();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        private static void RelocateSecurityFixtures()
        {
            Vector3 previous = new(52.1f, 0, 43.15f);
            Vector3 replacement = new(52.65f, 0, 39.6f);
            var cart = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
                .Single(t => t.name == "Maintenance cart" && t.parent.name == "Security repair" &&
                    (Vector3.Distance(t.position, previous) < .2f || Vector3.Distance(t.position, replacement) < .2f));
            MoveFixture(cart, replacement);
            var set = cart.parent;
            // The inspected service bay is now a doorway. Keep its prop cluster beside the opening.
            foreach (string name in new[] { "Service junction", "Prop_Barrel_Large", "Prop_Crate4" })
            {
                var part = set.Find(name) ?? throw new InvalidOperationException("Missing SEC-07 service prop: " + name);
                if (part.position.z < 44)
                    MoveFixture(part, part.position + Vector3.forward * 2.7f);
            }
        }

        private static void MoveFixture(Transform fixture, Vector3 destination)
        {
            if (Vector3.Distance(fixture.position, destination) < .05f)
                return;

            Physics.SyncTransforms();
            Vector3 delta = destination - fixture.position;
            foreach (var collider in fixture.GetComponentsInChildren<Collider>())
            {
                var bounds = collider.bounds;
                Vector3 size = bounds.extents - Vector3.one * .02f;
                var obstruction = Physics.OverlapBox(bounds.center + delta, size,
                    Quaternion.identity, Hostfall.Core.CollisionLayers.WorldMask, QueryTriggerInteraction.Ignore)
                    .FirstOrDefault(c => !c.transform.IsChildOf(fixture) && c.bounds.max.y > .035f);
                if (obstruction != null)
                    throw new InvalidOperationException("No clearance for " + fixture.name + ": " + obstruction.name);
            }
            fixture.position = destination;
        }

        private static Transform FindTransform(string name) =>
            Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Single(t => t.name == name);

        private static Material MaterialAt(string name) =>
            AssetDatabase.LoadAssetAtPath<Material>("Assets/_HOSTFALL/Content/Materials/" + name + ".mat")
            ?? throw new InvalidOperationException("Missing sensor floor material: " + name);

        private static void CutOpening(Transform wall, Vector3 axis, float center, float width, float head)
        {
            Physics.SyncTransforms();
            var collider = wall.GetComponent<BoxCollider>();
            var renderer = wall.GetComponent<MeshRenderer>();
            if (collider == null || renderer == null ||
                Quaternion.Angle(wall.rotation, Quaternion.identity) > .1f ||
                wall.GetComponentsInChildren<Transform>().Length != 1)
                throw new InvalidOperationException("Inspect structural wall before cutting: " + wall.name);

            Bounds bounds = collider.bounds;
            float low = Vector3.Dot(bounds.min, axis), high = Vector3.Dot(bounds.max, axis);
            float start = center - width * .5f, end = center + width * .5f;
            if (low >= start || high <= end || bounds.max.y <= head || Mathf.Abs(bounds.min.y) > .12f)
                throw new InvalidOperationException("Portal outside structural wall: " + wall.name);

            void Fit(Transform part, float a, float b, float bottom, float top)
            {
                Vector3 position = bounds.center + axis * ((a + b) * .5f - Vector3.Dot(bounds.center, axis));
                position.y = (bottom + top) * .5f;
                Vector3 scale = bounds.size + axis * (b - a - Vector3.Dot(bounds.size, axis));
                scale.y = top - bottom;
                part.position = position;
                part.localScale = scale;
            }

            var second = Object.Instantiate(wall.gameObject, wall.parent).transform;
            second.name = wall.name + " / far side";
            var lintel = Object.Instantiate(wall.gameObject, wall.parent).transform;
            lintel.name = wall.name + " / lintel";
            Fit(wall, low, start, bounds.min.y, bounds.max.y);
            Fit(second, end, high, bounds.min.y, bounds.max.y);
            Fit(lintel, start, end, head, bounds.max.y);
        }

        private static FacilityDoor CreateSensorGate(string id, Transform parent, Vector3 position, float yaw, Material metal)
        {
            var frame = new GameObject(id).transform;
            frame.SetParent(parent, false);
            frame.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
            Box("Portal jamb left", frame, new Vector3(-1.07f, 1.7f, 0), new Vector3(.11f, 3.4f, .83f), metal, true);
            Box("Portal jamb right", frame, new Vector3(1.07f, 1.7f, 0), new Vector3(.11f, 3.4f, .83f), metal, true);
            Box("Portal lintel", frame, new Vector3(0, 3.32f, 0), new Vector3(2.24f, .16f, .83f), metal, true);
            var leaf = Box("Sensor interlock shutter", frame, new Vector3(0, 1.63f, 0), new Vector3(2.02f, 3.26f, .24f), metal, true);
            Box("Interlock status inlay", leaf.transform, new Vector3(0, .12f, -.54f), new Vector3(.73f, .023f, .07f), MaterialAt("Indicator Amber"), false);
            var door = frame.gameObject.AddComponent<FacilityDoor>();
            door.Configure(leaf.transform, DoorAccessCondition.ManualUnlockOnly, "Сенсорный контур активен · завершите MINEFIELD",
                "[E] Открыть служебный шлюз", "[E] Закрыть служебный шлюз", true, Vector3.up, 3.4f, 2.2f, id);
            return door;
        }

        private static GameObject Box(string name, Transform parent, Vector3 localPosition, Vector3 scale, Material material, bool solid)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = localPosition;
            box.transform.localScale = scale;
            box.GetComponent<Renderer>().sharedMaterial = material;
            if (!solid)
                Object.DestroyImmediate(box.GetComponent<Collider>());
            return box;
        }

        private static void AddText(string label, Transform parent, Vector3 position, Quaternion rotation, float size)
        {
            var child = new GameObject(label);
            child.transform.SetParent(parent, false);
            child.transform.localPosition = position;
            child.transform.localRotation = rotation;
            var text = child.AddComponent<TextMesh>();
            text.anchor = TextAnchor.MiddleCenter;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 48;
            text.characterSize = size;
            text.color = new Color(.68f, .85f, .89f);
            text.text = label;
            child.AddComponent<WorldTextDepth>().Configure(Shader.Find("HOSTFALL/World Text"));
        }
    }
}
