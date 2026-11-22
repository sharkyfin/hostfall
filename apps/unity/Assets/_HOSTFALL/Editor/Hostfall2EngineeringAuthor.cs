using System;
using System.IO;
using System.Linq;
using Hostfall.Core;
using Hostfall.Rendering;
using Hostfall.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Hostfall.Editor
{
    public static partial class Hostfall2Workbench
    {
        private const string EngineeringPath = "Assets/_HOSTFALL/Art/Hostfall2/Engineering/";
        private const string EngineeringRoot = "NORTHLINE / Engineering service fittings";

        [MenuItem("HOSTFALL/2.0/Install Engineering Fittings")]
        public static void InstallEngineeringFittings()
        {
            WorkstationWorkshop.RequireCopy();
            var scene = EditorSceneManager.OpenScene(PreviewScene);
            string checkpoint = Path.Combine(Output, "scene-before-engineering-fittings.unity");
            if (!File.Exists(checkpoint)) File.Copy(PreviewScene, checkpoint);
            var previous = GameObject.Find(EngineeringRoot);
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous);
            var root = new GameObject(EngineeringRoot).transform;
            Physics.SyncTransforms();

            var valve = MountFitting("Service passage / Valve V-14", "HF2_IsolationManifold", root,
                new Vector3(0, 1.38f, 3.25f), Vector3.left, .86f);
            var light = MountFitting("Service passage / Inspection lamp", "HF2_InspectionLight", root,
                new Vector3(0, 2.25f, valve.position.z), Vector3.left, 1f);
            var pump = FitPump(root);
            var energyLight = MountFitting("Energy center / Inspection lamp", "HF2_InspectionLight", root,
                new Vector3(-18.4f, 2.3f, 36f), Vector3.forward, 1f);
            AddInspectionGlow(light);
            AddInspectionGlow(energyLight);
            AddPressureSound(pump);
            DressGenerator();
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            File.WriteAllLines(Path.Combine(Output, "engineering-mounts.txt"), root.Cast<Transform>()
                .Select(item => item.name + " | " + item.position.ToString("F3") + " | " + item.GetComponentInChildren<MeshRenderer>().bounds.size.ToString("F3")));
            CaptureContainmentReview("hf2-engineering-service", valve.position + Vector3.right * 1.8f + Vector3.up * .75f - Vector3.forward * .95f,
                valve.GetComponentInChildren<MeshRenderer>().bounds.center + Vector3.up * .18f);
            CaptureContainmentReview("hf2-engineering-pump", pump.position - Vector3.right * 2.45f + Vector3.up * 1.42f - Vector3.forward * 1.55f,
                pump.GetComponentInChildren<MeshRenderer>().bounds.center + Vector3.up * .10f);
        }

        private static Transform MountFitting(string label, string model, Transform root, Vector3 from, Vector3 direction, float scale)
        {
            var fitting = EngineeringFitting(label, model, root);
            var renderer = fitting.GetComponentInChildren<MeshRenderer>();
            var initial = renderer.bounds;
            float front = initial.center.z >= 0 ? 1f : -1f;
            fitting.localScale = Vector3.one * scale;
            Vector3 alongWall = Vector3.Cross(direction, Vector3.up);
            foreach (float offset in new[] { 0f, .52f, -.52f, 1.04f, -1.04f, 1.56f, -1.56f })
            {
                var wall = Physics.RaycastAll(from + alongWall * offset, direction, 8f, CollisionLayers.WorldMask, QueryTriggerInteraction.Ignore)
                    .Where(hit => Mathf.Abs(hit.normal.y) < .06f && Vector3.Dot(hit.normal, direction) < -.9f &&
                        hit.collider.name.Contains("wall", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(hit => hit.distance).FirstOrDefault();
                if (wall.collider == null) continue;
                fitting.SetPositionAndRotation(wall.point + wall.normal * .009f - Vector3.up * initial.center.y * scale,
                    Quaternion.LookRotation(wall.normal * front, Vector3.up));
                var bounds = renderer.bounds;
                bool occupied = Physics.OverlapBox(bounds.center, bounds.extents - Vector3.one * .018f,
                    Quaternion.identity, CollisionLayers.WorldMask, QueryTriggerInteraction.Ignore).Any(collider => collider != wall.collider);
                if (occupied || root.Cast<Transform>().Any(other => other != fitting &&
                    other.GetComponentInChildren<MeshRenderer>().bounds.Intersects(bounds))) continue;
                return fitting;
            }
            throw new InvalidOperationException("No clear wall mount for " + label);
        }

        private static Transform FitPump(Transform root)
        {
            var target = EngineeringFitting("Energy center / Return pump P-07", "HF2_ReturnPump", root);
            var renderer = target.GetComponentInChildren<MeshRenderer>();
            Bounds size = renderer.bounds;
            if (size.size.x < 1.1f || size.size.x > 1.55f || size.size.y < .6f || size.size.y > 1f)
                throw new InvalidOperationException("Unexpected pump mesh scale or axes: " + size.size);
            foreach (var site in new[] { new Vector3(-18.4f, .75f, 32.75f), new Vector3(-17.8f, .75f, 32.25f),
                new Vector3(-17.75f, .75f, 35.1f), new Vector3(-15.2f, .75f, 32.3f) })
            {
                if (!Physics.Raycast(site, Vector3.down, out var floor, 1.9f, CollisionLayers.WorldMask, QueryTriggerInteraction.Ignore) ||
                    floor.normal.y < .9f || Mathf.Abs(floor.point.y) > .12f) continue;
                var half = size.extents - new Vector3(0, .022f, 0);
                if (Physics.CheckBox(floor.point + new Vector3(size.center.x, size.extents.y + .034f, size.center.z), half,
                    Quaternion.identity, CollisionLayers.WorldMask, QueryTriggerInteraction.Ignore)) continue;
                target.position = floor.point - Vector3.up * size.min.y + Vector3.up * .004f;
                var physical = target.gameObject.AddComponent<BoxCollider>();
                physical.center = target.InverseTransformPoint(renderer.bounds.center);
                physical.size = renderer.bounds.size;
                return target;
            }
            throw new InvalidOperationException("No clear grounded pump position in the energy center");
        }

        private static Transform EngineeringFitting(string label, string stem, Transform root)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(EngineeringPath + stem + ".fbx");
            if (model == null) throw new FileNotFoundException(stem);
            var baseTransform = new GameObject(label).transform;
            baseTransform.SetParent(root, false);
            var item = (GameObject)PrefabUtility.InstantiatePrefab(model, baseTransform);
            var renderer = item.GetComponentInChildren<MeshRenderer>();
            renderer.sharedMaterials = renderer.sharedMaterials.Select(EngineeringSurface).ToArray();
            return baseTransform;
        }

        private static Material EngineeringSurface(Material original)
        {
            string role = original.name;
            string asset = EngineeringPath + role + ".mat";
            var result = AssetDatabase.LoadAssetAtPath<Material>(asset);
            if (result != null) return result;
            var (color, metal, smooth) = role switch
            {
                "Steel" => (new Color(.34f,.41f,.42f), .67f, .5f),
                "PetrolEnamel" => (new Color(.12f,.24f,.27f), .29f, .41f),
                "Brass" => (new Color(.42f,.32f,.17f), .58f, .43f),
                "IsolationRed" => (new Color(.56f,.14f,.10f), .24f, .45f),
                "InstrumentIvory" => (new Color(.76f,.74f,.63f), .04f, .36f),
                "CastIron" => (new Color(.15f,.30f,.26f), .45f, .46f),
                "Gasket" => (new Color(.043f,.058f,.063f), .05f, .24f),
                "WarmDiffuser" => (new Color(.92f,.82f,.64f), .04f, .42f),
                _ => (new Color(.14f,.18f,.19f), .32f, .36f)
            };
            result = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = role };
            result.SetColor("_BaseColor", color);
            result.SetFloat("_Metallic", metal);
            result.SetFloat("_Smoothness", smooth);
            if (role == "WarmDiffuser")
            {
                result.EnableKeyword("_EMISSION");
                result.SetColor("_EmissionColor", color * 1.3f);
            }
            AssetDatabase.CreateAsset(result, asset);
            return result;
        }

        private static void AddInspectionGlow(Transform mount)
        {
            var renderer = mount.GetComponentInChildren<MeshRenderer>();
            var forward = (renderer.bounds.center - mount.position);
            forward.y = 0;
            if (forward.sqrMagnitude < .002f) throw new InvalidOperationException("Lamp has no visible depth");
            var light = new GameObject("Inspection lamp / reflected light").AddComponent<Light>();
            light.transform.SetParent(mount, false);
            light.transform.position = renderer.bounds.center + forward.normalized * .12f;
            light.transform.rotation = Quaternion.LookRotation((forward.normalized + Vector3.down * .58f).normalized);
            light.type = LightType.Spot;
            light.range = 3.3f;
            light.spotAngle = 103;
            light.innerSpotAngle = 54;
            light.color = new Color(1f, .85f, .68f);
            light.intensity = 1.05f;
            light.shadows = LightShadows.None;
        }

        private static void AddPressureSound(Transform pump)
        {
            var source = pump.gameObject.AddComponent<AudioSource>();
            source.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_HOSTFALL/Resources/Hostfall2/ReturnPressure.wav");
            if (source.clip == null) throw new InvalidOperationException("Missing pressure loop");
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 1;
            source.volume = .28f;
            source.minDistance = .65f;
            source.maxDistance = 8f;
            source.rolloffMode = AudioRolloffMode.Linear;
            pump.gameObject.AddComponent<MaintenanceResonance>();
        }

        private static void DressGenerator()
        {
            var generator = GameObject.Find("Central Generator Core");
            foreach (var part in generator.GetComponentsInChildren<MeshRenderer>())
            {
                if (part.name.StartsWith("Magnetic Coil ", StringComparison.Ordinal))
                {
                    part.sharedMaterial = EngineeringSurface(part.sharedMaterial, "GeneratorCopper");
                    continue;
                }
                var materials = part.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    var role = materials[i].name switch
                    {
                        "Kit MI_Trim_01" => "GeneratorGuard",
                        "Kit MI_Trim_02" => "GeneratorCasing",
                        "Kit MI_PaddedWall" => "GeneratorFrame",
                        _ => null
                    };
                    if (role != null) materials[i] = EngineeringSurface(materials[i], role);
                }
                part.sharedMaterials = materials;
            }
        }

        private static Material EngineeringSurface(Material original, string role)
        {
            string path = EngineeringPath + role + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(original) { name = role };
            material.DisableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", Color.black);
            var (tint, metallic) = role switch
            {
                "GeneratorCopper" => (new Color(.34f, .20f, .118f), .48f),
                "GeneratorGuard" => (new Color(.34f, .40f, .41f), .44f),
                "GeneratorFrame" => (new Color(.31f, .34f, .32f), .36f),
                _ => (new Color(.54f, .57f, .55f), .38f)
            };
            material.SetColor("_BaseColor", tint);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", .38f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        [MenuItem("HOSTFALL/2.0/Capture Main Menu Shots")]
        public static void CaptureMainMenuShots()
        {
            WorkstationWorkshop.RequireCopy();
            OpenClearanceScene();
            using var textDepth = new CapturedWorldText();
            var fittings = GameObject.Find(EngineeringRoot).transform;
            var valve = fittings.Cast<Transform>().Single(item => item.name == "Service passage / Valve V-14")
                .GetComponentInChildren<MeshRenderer>().bounds.center;
            var pump = fittings.Cast<Transform>().Single(item => item.name == "Energy center / Return pump P-07")
                .GetComponentInChildren<MeshRenderer>().bounds.center;
            var shots = new[]
            {
                (valve + new Vector3(1.82f,.28f,-.68f), valve + new Vector3(1.60f,.19f,-.18f), valve + Vector3.up*.10f),
                (pump + new Vector3(-1.05f,.83f,2.2f), pump + new Vector3(-.48f,.70f,1.97f), pump + Vector3.up*.08f),
                (new Vector3(47.2f,2.3f,72.8f), new Vector3(46.7f,2.13f,73.8f), new Vector3(42,1.75f,81.4f)),
            };
            const int width = 960, height = 540, framesPerShot = 72;
            var storage = Path.Combine(ClearanceOutput, "film-frames");
            Directory.CreateDirectory(storage);
            var camera = new GameObject("Main menu capture camera").AddComponent<Camera>();
            var target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var frame = new Texture2D(width, height, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                camera.fieldOfView = 56;
                camera.nearClipPlane = .05f;
                int number = 0;
                foreach (var (start, end, look) in shots)
                    for (int i = 0; i < framesPerShot; i++)
                    {
                        camera.transform.position = Vector3.Lerp(start, end, Mathf.SmoothStep(0, 1, i / (framesPerShot - 1f)));
                        camera.transform.LookAt(look);
                        camera.Render();
                        RenderTexture.active = target;
                        frame.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                        frame.Apply(false);
                        File.WriteAllBytes(Path.Combine(storage, $"{number:0000}.jpg"), frame.EncodeToJPG(88));
                        number++;
                    }
                File.WriteAllText(Path.Combine(storage, "complete.txt"), "216 original Unity frames · 960 × 540");
            }
            finally
            {
                RenderTexture.active = previous;
                camera.targetTexture = null;
                RenderTexture.ReleaseTemporary(target);
                UnityEngine.Object.DestroyImmediate(frame);
                UnityEngine.Object.DestroyImmediate(camera.gameObject);
            }
        }

        private sealed class CapturedWorldText : IDisposable
        {
            private readonly (MeshRenderer Renderer, Material Original, Material Temporary)[] labels;

            public CapturedWorldText()
            {
                Shader shader = Shader.Find("HOSTFALL/World Text");
                if (shader == null) throw new InvalidOperationException("World text depth shader is missing");
                labels = UnityEngine.Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None)
                    .Where(label => label.font != null && label.TryGetComponent<MeshRenderer>(out _))
                    .Select(label =>
                    {
                        var renderer = label.GetComponent<MeshRenderer>();
                        var original = renderer.sharedMaterial;
                        var temporary = WorldTextDepth.CreateDepthMaterial(label, shader);
                        renderer.sharedMaterial = temporary;
                        return (renderer, original, temporary);
                    }).ToArray();
            }

            public void Dispose()
            {
                foreach (var (renderer, original, temporary) in labels)
                {
                    renderer.sharedMaterial = original;
                    UnityEngine.Object.DestroyImmediate(temporary);
                }
            }
        }
    }
}
