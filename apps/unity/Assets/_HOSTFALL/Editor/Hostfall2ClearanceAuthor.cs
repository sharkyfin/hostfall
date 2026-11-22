using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hostfall.Core;
using Hostfall.Devices;
using Hostfall.Rendering;
using Hostfall.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Hostfall.Editor
{
    public static partial class Hostfall2Workbench
    {
        private static string ClearanceOutput
        {
            get
            {
                string path = Path.GetFullPath(Path.Combine(Output, "..", "hostfall2-clearance"));
                Directory.CreateDirectory(path);
                return path;
            }
        }

        private static Scene OpenClearanceScene()
        {
            WorkstationWorkshop.RequireCopy();
            var currentScene = SceneManager.GetActiveScene();
            if (currentScene.isDirty) throw new InvalidOperationException("Save isolated scene changes before clearing the route");
            return EditorSceneManager.OpenScene(PreviewScene);
        }

        [MenuItem("HOSTFALL/2.0/Inspect Passage and Fixtures")]
        public static void InspectPassageAndFixtures()
        {
            OpenClearanceScene();
            Physics.SyncTransforms();
            var lines = new List<string>();
            var renderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
            foreach (var label in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None).OrderBy(label => WorkstationWorkshop.PathOf(label.transform)))
            {
                string text = label.text.Replace("\n", " / ");
                string shader = label.GetComponent<MeshRenderer>().sharedMaterial?.shader.name;
                lines.Add($"TEXT | {WorkstationWorkshop.PathOf(label.transform)} | {label.transform.position:F3} | {label.transform.eulerAngles:F0} | {label.characterSize:F3} | {shader} | {text}");
            }

            foreach (var renderer in renderers.OrderBy(r => WorkstationWorkshop.PathOf(r.transform)))
            {
                var materials = renderer.sharedMaterials.Where(m => m != null).Select(m => m.name).ToArray();
                var b = renderer.bounds;
                bool fixture = renderer.name.Contains("Button", StringComparison.OrdinalIgnoreCase) ||
                    renderer.name.Contains("Switch", StringComparison.OrdinalIgnoreCase) ||
                    renderer.name is "Wall display case" or "Wall telemetry" or "Floor guide";
                bool early = b.center.z >= -19 && b.center.z <= 10;
                if (fixture || early && materials.Any(m => m is "Indicator Amber" or "WarmLight" or "Warm Light"))
                    lines.Add($"PROP | {WorkstationWorkshop.PathOf(renderer.transform)} | {b.center:F3} | {b.size:F3} | {renderer.transform.eulerAngles:F0} | {string.Join(", ", materials)}");
            }

            foreach (var panel in Object.FindObjectsByType<ServiceButtonPanel>(FindObjectsSortMode.None).OrderBy(p => p.name))
            {
                var b = panel.GetComponent<BoxCollider>().bounds;
                var physical = Physics.RaycastAll(b.center + Vector3.up * .05f, Vector3.down, 2.5f, CollisionLayers.WorldMask, QueryTriggerInteraction.Ignore)
                    .Where(h => !h.transform.IsChildOf(panel.transform) && h.normal.y > .85f)
                    .OrderBy(h => h.distance).FirstOrDefault();
                lines.Add($"CONTROL | {WorkstationWorkshop.PathOf(panel.transform)} | {b.center:F3} | {panel.transform.eulerAngles:F0} | {(physical.collider == null ? "unsupported" : physical.collider.name + " " + (b.min.y - physical.point.y).ToString("F3"))} | {string.Join(", ", panel.transform.Cast<Transform>().Select(t => t.name))}");
            }

            File.WriteAllLines(Path.Combine(ClearanceOutput, "fixtures.txt"), lines);
            using var depth = new CapturedWorldText();
            ReviewPassage("before");
        }

        private const string Details = "Assets/_HOSTFALL/Art/Hostfall2/ClearanceDetails/";
        private const string FittingsRoot = "NORTHLINE / Passage fixtures";

        [MenuItem("HOSTFALL/2.0/Apply Passage Clearance")]
        public static void ApplyPassageClearance()
        {
            var scene = OpenClearanceScene();
            try { ApplyPassageClearance(scene); }
            catch
            {
                EditorSceneManager.OpenScene(PreviewScene);
                throw;
            }
        }

        private static void ApplyPassageClearance(Scene scene)
        {
            var office = GameObject.Find("NORTHLINE / Facility 04").transform;
            var oldFittings = GameObject.Find(FittingsRoot);
            if (oldFittings != null) Object.DestroyImmediate(oldFittings);
            var root = new GameObject(FittingsRoot).transform;
            var lines = new List<string>();

            foreach (Transform child in office.Cast<Transform>().ToArray())
                if (child.name is "Wall display case" or "Wall telemetry" or "Facility information panel" or "Floor guide" or "Wayfinding strip" ||
                    child.name.StartsWith("Sign / ", StringComparison.Ordinal))
                    Object.DestroyImmediate(child.gameObject);

            foreach (var sign in Object.FindObjectsByType<Transform>().Where(t => t.name.StartsWith("Signboard - ", StringComparison.Ordinal)).ToArray())
                Object.DestroyImmediate(sign.gameObject);

            ReplaceDecorativeAmber();
            Physics.SyncTransforms();
            WallDetail(root, "Office / left relay", "HF2_ContactRepeater", new Vector3(-2.05f, 1.44f, -5.98f), Vector3.back, lines);
            WallDetail(root, "Office / right relay", "HF2_ContactRepeater", new Vector3(2.05f, 1.44f, -5.98f), Vector3.back, lines);
            WallDetail(root, "Office / cable feed", "HF2_GlandBox", new Vector3(2.05f, .97f, -5.98f), Vector3.back, lines);
            WallDetail(root, "Service / cable feed", "HF2_GlandBox", new Vector3(.04f, 1.13f, 2.18f), Vector3.left, lines);
            RoomPlate(root, "СЕТЕВОЙ ШЛЮЗ", new Vector3(.12f, 2.48f, 4f), Vector3.left, lines);
            RoomPlate(root, "СЕРВЕРНАЯ", new Vector3(-1.75f, 2.03f, 6.18f), Vector3.forward, lines);
            GroundDetail(root, "Energy center / spill kit", new Vector3(-18.16f, .58f, 34.55f), lines);
            GroundDetail(root, "Office / spill kit", new Vector3(-3.59f, .58f, -6.18f), lines);

            foreach (var panel in Object.FindObjectsByType<ServiceButtonPanel>().OrderBy(p => p.name))
                SeatPanel(panel, lines);

            var shader = Shader.Find("HOSTFALL/World Text");
            foreach (var text in Object.FindObjectsByType<TextMesh>())
            {
                if (text.text.StartsWith("NORTHLINE / WS-08", StringComparison.Ordinal) ||
                    text.text.StartsWith("NORTHLINE / 04 // PROTOCOL STATUS:", StringComparison.Ordinal))
                {
                    Object.DestroyImmediate(text.gameObject);
                    continue;
                }
                if (text.transform.parent?.name == "Investigation Pinboard")
                {
                    text.text = "СВЯЗИ НЕТ\n\nСобраться у шлюза.\nЖдать смену.";
                    text.transform.SetPositionAndRotation(new Vector3(4.955f, 2.25f, -12.72f),
                        Quaternion.LookRotation(Vector3.right));
                }
                if (!text.TryGetComponent<WorldTextDepth>(out var depth))
                    depth = text.gameObject.AddComponent<WorldTextDepth>();
                depth.Configure(shader);
                EditorUtility.SetDirty(depth);
            }
            Physics.SyncTransforms();
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save isolated passage scene");
            File.WriteAllLines(Path.Combine(ClearanceOutput, "installed-fixtures.txt"), lines);
            using var labelDepth = new CapturedWorldText();
            ReviewPassage("after");
            ReviewClearanceShot("office-wall-after", new Vector3(1.14f,1.54f,-4.88f),new Vector3(2.05f,1.45f,-6.73f));
        }

        [MenuItem("HOSTFALL/2.0/Audit Active Map Collisions")]
        public static void AuditActiveMapCollisions()
        {
            OpenClearanceScene();
            string output = Path.Combine(ClearanceOutput, "map-collisions");
            CollisionAudit.GroundGrid(PreviewScene, output, false);
            CollisionAudit.Surfaces(output);
        }

        private static void ReplaceDecorativeAmber()
        {
            foreach (var renderer in Object.FindObjectsByType<MeshRenderer>())
            {
                if (renderer.sharedMaterial?.name != "Indicator Amber") continue;
                string name = renderer.name;
                if (name == "Corkboard Surface")
                    renderer.sharedMaterial = DetailMaterial("NoticeBoard", new Color(.32f,.224f,.145f), .04f, .27f);
                else if (name == "Wire Spool")
                    renderer.sharedMaterial = DetailMaterial("CopperWire", new Color(.32f,.158f,.086f), .50f, .44f);
                else if (name == "Door control / both sides")
                    renderer.sharedMaterial = DetailMaterial("DoorControlCeramic", new Color(.24f,.37f,.33f), .12f, .35f);
                else if (name.Contains("Hazard Stripe", StringComparison.Ordinal) || name.StartsWith("Warning band ", StringComparison.Ordinal) ||
                    name is "Visible edge" or "Rack Warning Header" or "Rack Warning Footer" or "Ammo Stencil Stripe")
                    renderer.sharedMaterial = DetailMaterial("PaintedSafetyEdge", new Color(.41f,.31f,.186f), .09f, .41f);
            }
        }

        private static Material DetailMaterial(string role, Color tint, float metal, float smooth)
        {
            string path = Details + role + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = role };
            material.SetColor("_BaseColor", tint);
            material.SetFloat("_Metallic", metal);
            material.SetFloat("_Smoothness", smooth);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Transform DetailModel(Transform parent, string title, string asset)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(Details + asset + ".fbx");
            if (model == null) throw new FileNotFoundException(asset);
            var mount = new GameObject(title).transform;
            mount.SetParent(parent, false);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, mount);
            var mesh = instance.GetComponentInChildren<MeshRenderer>();
            mesh.sharedMaterials = mesh.sharedMaterials.Select(m => m.name switch
            {
                "SmokedGlass" => DetailMaterial(m.name, new Color(.034f,.074f,.076f), .22f, .58f),
                "IndicatorPaint" => DetailMaterial(m.name, new Color(.29f,.52f,.43f), .12f, .37f),
                "AbsorbentCloth" => DetailMaterial(m.name, new Color(.54f,.49f,.38f), .02f, .19f),
                _ => EngineeringSurface(m)
            }).ToArray();
            return mount;
        }

        private static RaycastHit WallContact(Vector3 from, Vector3 direction)
        {
            return Physics.RaycastAll(from, direction, 2f, CollisionLayers.WorldMask, QueryTriggerInteraction.Ignore)
                .Where(h => Vector3.Dot(h.normal, -direction) > .95f &&
                    (h.collider.name.Contains("wall", StringComparison.OrdinalIgnoreCase) ||
                     h.collider.name.Contains("panel", StringComparison.OrdinalIgnoreCase) ||
                     h.collider.name.Contains("bulkhead", StringComparison.OrdinalIgnoreCase)))
                .OrderBy(h => h.distance).FirstOrDefault();
        }

        private static void WallDetail(Transform root, string title, string asset, Vector3 from, Vector3 direction, List<string> report)
        {
            var mount = DetailModel(root, title, asset);
            var initial = mount.GetComponentInChildren<MeshRenderer>().bounds;
            var solid = mount.gameObject.AddComponent<BoxCollider>();
            ColliderProfileRepair.Fit(solid);
            float front = Mathf.Sign(initial.center.z);
            Vector3 along = Vector3.Cross(direction, Vector3.up);
            foreach (float offset in new[] { 0f, -.28f, .28f, -.55f, .55f, -.8f, .8f })
            {
                var support = WallContact(from + along * offset, direction);
                if (support.collider == null) continue;
                mount.SetPositionAndRotation(support.point + support.normal * .003f - Vector3.up * initial.center.y,
                    Quaternion.LookRotation(support.normal * front, Vector3.up));
                if (DetailObstruction(solid, support.collider) != null) continue;
                var b = solid.bounds;
                report.Add($"{mount.name} | {support.collider.name} | {b.center:F3} | {b.size:F3}");
                return;
            }
            throw new InvalidOperationException("No clear wall mount for " + title);
        }

        private static void GroundDetail(Transform root, string title, Vector3 from, List<string> report)
        {
            if (!Physics.Raycast(from, Vector3.down, out var support, 1.4f, CollisionLayers.WorldMask, QueryTriggerInteraction.Ignore) ||
                support.normal.y < .95f)
                throw new InvalidOperationException("Missing floor for " + title);
            var mount = DetailModel(root, title, "HF2_SpillKit");
            float bottom = mount.GetComponentInChildren<MeshRenderer>().bounds.min.y;
            mount.position = support.point + Vector3.up * (.002f - bottom);
            AddCheckedDetailCollider(mount, support.collider, report);
        }

        private static void AddCheckedDetailCollider(Transform mount, Collider support, List<string> report)
        {
            var solid = mount.gameObject.AddComponent<BoxCollider>();
            ColliderProfileRepair.Fit(solid);
            var obstruction = DetailObstruction(solid, support);
            var bounds = solid.bounds;
            if (obstruction != null) throw new InvalidOperationException(mount.name + " intersects " + obstruction.name);
            report.Add($"{mount.name} | {support.name} | {bounds.center:F3} | {bounds.size:F3}");
        }

        private static Collider DetailObstruction(BoxCollider solid, Collider support)
        {
            Physics.SyncTransforms();
            var bounds = solid.bounds;
            return Physics.OverlapBox(bounds.center, bounds.extents - Vector3.one * .011f,
                Quaternion.identity, CollisionLayers.WorldMask, QueryTriggerInteraction.Ignore)
                .FirstOrDefault(c => c != support && c != solid);
        }

        private static void RoomPlate(Transform root, string title, Vector3 from, Vector3 direction, List<string> report)
        {
            var support = WallContact(from, direction);
            if (support.collider == null) return;
            var mount = new GameObject("Wall plate / " + title).transform;
            mount.SetParent(root, false);
            mount.SetPositionAndRotation(support.point + support.normal*.013f, Quaternion.LookRotation(-support.normal));
            float width = title.Length > 10 ? .90f : .63f;
            Box("Mounted enamel plate", mount, Vector3.zero, new Vector3(width,.154f,.026f), MaterialAt("Dark Metal"), false);
            var text = new GameObject("Enamel lettering").AddComponent<TextMesh>();
            text.transform.SetParent(mount, false);
            text.transform.localPosition = new Vector3(0,0,-.016f);
            text.font = AssetDatabase.LoadAssetAtPath<Font>(FacilityBuilder.Content + "/Fonts/RobotoMono-Regular.ttf");
            text.fontSize = 64;
            text.characterSize = .0051f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = new Color(.70f,.74f,.72f);
            text.richText = false;
            text.GetComponent<MeshRenderer>().sharedMaterial = text.font.material;
            report.Add($"{title} | {support.collider.name} | {mount.position:F3}");
        }

        private static void SeatPanel(ServiceButtonPanel panel, List<string> report)
        {
            var mount = panel.transform;
            var screen = panel.GetComponentInParent<WorkstationSurface>();
            var solid = panel.GetComponent<BoxCollider>();
            var model = mount.Cast<Transform>().FirstOrDefault(t => t.name == "HF_ServicePanel");
            if (screen == null || solid == null || model == null) return;
            var initialPosition = mount.position;
            var initialRotation = mount.rotation;
            var supportStand = mount.Cast<Transform>().FirstOrDefault(t => t.name == "Bolted worklight mount");
            solid.enabled = false;
            if (supportStand != null) supportStand.gameObject.SetActive(false);
            var front = Vector3.ProjectOnPlane(screen.Normal, Vector3.up).normalized;
            var side = Vector3.Cross(front,Vector3.up);
            mount.rotation = Quaternion.LookRotation(front) * Quaternion.Euler(-90,0,0);
            foreach (var offset in new[] { Vector3.zero, side*.17f, -side*.17f, -front*.17f, side*.29f-front*.16f,
                -side*.29f-front*.16f, -side*.40f, -side*.45f+front*.12f })
            {
                Vector3 origin = initialPosition + offset + Vector3.up*.36f;
                var floor = Physics.RaycastAll(origin, Vector3.down, .85f, CollisionLayers.WorldMask, QueryTriggerInteraction.Ignore)
                    .Where(h => h.normal.y > .93f && h.collider.transform.IsChildOf(screen.transform) &&
                        (h.collider.name.Contains("Desk",StringComparison.OrdinalIgnoreCase) ||
                         h.collider.name.Contains("Pedestal",StringComparison.OrdinalIgnoreCase) ||
                         h.collider.name.Contains("Slab",StringComparison.OrdinalIgnoreCase)))
                    .OrderBy(h => h.distance).FirstOrDefault();
                if (floor.collider == null) continue;
                mount.position = initialPosition + offset;
                mount.position += Vector3.up * (floor.point.y - VisiblePanelBounds(model).min.y + .002f);
                var bounds = VisiblePanelBounds(model);
                if (Physics.OverlapBox(bounds.center, bounds.extents - Vector3.one*.009f, Quaternion.identity,
                    CollisionLayers.WorldMask, QueryTriggerInteraction.Ignore).Any(c => c != floor.collider))
                    continue;
                if (supportStand != null) Object.DestroyImmediate(supportStand.gameObject);
                ColliderProfileRepair.Fit(solid);
                solid.enabled = true;
                Physics.SyncTransforms();
                report.Add($"Switch | {panel.name} | {floor.collider.name} | {mount.position:F3} | {front:F2}");
                return;
            }
            mount.SetPositionAndRotation(initialPosition,initialRotation);
            solid.enabled = true;
            if (supportStand == null) throw new InvalidOperationException("No panel support for " + panel.name);
            supportStand.gameObject.SetActive(true);
            Physics.SyncTransforms();
            var support = Physics.RaycastAll(supportStand.position + Vector3.up*.06f, Vector3.down, .12f,
                    CollisionLayers.WorldMask, QueryTriggerInteraction.Ignore)
                .FirstOrDefault(hit => !hit.transform.IsChildOf(mount) && hit.normal.y > .95f);
            if (support.collider == null)
                throw new InvalidOperationException("No floor contact for " + panel.name);
            report.Add($"Switch | {panel.name} | bolted stand on {support.collider.name} | {initialPosition:F3}");
        }

        private static Bounds VisiblePanelBounds(Transform model)
        {
            var renderers = model.GetComponentsInChildren<MeshRenderer>();
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        private static void ReviewPassage(string phase)
        {
            var door = GameObject.Find("Shelter Security Door [SEC-HOME]").GetComponent<FacilityDoor>();
            var fields = new SerializedObject(door);
            var leaf = (Transform)fields.FindProperty("leaf").objectReferenceValue;
            var closed = leaf.localPosition;
            try
            {
                leaf.localPosition = closed + fields.FindProperty("slideDirection").vector3Value.normalized *
                    fields.FindProperty("travel").floatValue;
                ReviewClearanceShot("office-to-shelter-" + phase, new Vector3(.12f, 1.65f, -4.15f), new Vector3(.1f, 1.6f, -7.5f));
                ReviewClearanceShot("shelter-to-office-" + phase, new Vector3(-.12f, 1.65f, -9.85f), new Vector3(-.08f, 1.63f, -5.7f));
            }
            finally { leaf.localPosition = closed; }
            ReviewClearanceShot("service-overview-" + phase, new Vector3(.04f, 1.63f, 2.93f), new Vector3(-.23f, 1.5f, 5.25f));
        }

        private static void ReviewClearanceShot(string name, Vector3 eye, Vector3 target)
        {
            WorkstationWorkshop.Capture(name, eye, target);
            File.Copy(Path.Combine(WorkstationWorkshop.Output, name + ".png"), Path.Combine(ClearanceOutput, name + ".png"), true);
        }
    }
}
