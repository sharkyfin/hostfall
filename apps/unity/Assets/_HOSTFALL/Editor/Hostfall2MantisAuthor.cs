using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hostfall.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Hostfall.Editor
{
    public static partial class Hostfall2Workbench
    {
        private const string MantisArt = "Assets/_HOSTFALL/Art/HostfallCombat";
        private const string MantisBase = "Assets/_HOSTFALL/Resources/Combat/MANTIS6.prefab";
        private const string MantisRig = "Assets/_HOSTFALL/Content/Prefabs/MANTIS6-Rigged.prefab";
        private const string MantisMeshes = "Assets/_HOSTFALL/Content/Meshes/GuardianRig";

        [MenuItem("HOSTFALL/2.0/Author MANTIS Articulation")]
        public static void AuthorMantisArticulation()
        {
            WorkstationWorkshop.RequireCopy();
            if (EditorSceneManager.GetActiveScene().isDirty)
                throw new InvalidOperationException("Save the verification scene before authoring the MANTIS rig");
            if (EditorSceneManager.GetActiveScene().path != PreviewScene)
                EditorSceneManager.OpenScene(PreviewScene);
            var scene = EditorSceneManager.GetActiveScene();
            var actors = Object.FindObjectsByType<TrackedSentryController>();
            if (actors.Length != 2)
                throw new InvalidOperationException("Expected the two authored MANTIS guardians");

            File.Copy(PreviewScene, Path.Combine(Output, "scene-before-mantis-rig.unity"), true);
            BuildMantisRig();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MantisRig);
            foreach (var actor in actors)
            {
                var prior = actor.transform.Cast<Transform>().Single(t => t.name.StartsWith("MANTIS6", StringComparison.Ordinal));
                var replacement = (GameObject)PrefabUtility.InstantiatePrefab(prefab, actor.transform);
                replacement.transform.SetLocalPositionAndRotation(prior.localPosition, prior.localRotation);
                replacement.transform.localScale = prior.localScale;
                var parts = replacement.GetComponentsInChildren<Transform>();
                var fields = new SerializedObject(actor);
                fields.FindProperty("turretTransform").objectReferenceValue = parts.Single(t => t.name == "MANTIS turret");
                fields.FindProperty("muzzleTransform").objectReferenceValue = parts.Single(t => t.name == "Emitter");
                fields.ApplyModifiedPropertiesWithoutUndo();
                Object.DestroyImmediate(prior.gameObject);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void BuildMantisRig()
        {
            string rawPath = MantisArt + "/HF_MANTIS6.fbx";
            var importer = (ModelImporter)AssetImporter.GetAtPath(rawPath);
            bool readable = importer.isReadable;
            if (!readable) { importer.isReadable = true; importer.SaveAndReimport(); }
            var root = PrefabUtility.LoadPrefabContents(MantisBase);
            try
            {
                var original = AssetDatabase.LoadAssetAtPath<GameObject>(rawPath);
                var nodes = root.GetComponentsInChildren<Transform>().ToDictionary(t => t.name);
                var staticGroup = nodes["Fixed render groups"];
                var source = original.GetComponentsInChildren<MeshFilter>();
                Vector3 Centre(MeshFilter model) => nodes[model.name].TransformPoint(model.sharedMesh.bounds.center);
                var bones = new List<Transform> { root.transform };
                Transform Pivot(string name, Vector3 position, Transform parent)
                {
                    var pivot = new GameObject(name).transform;
                    pivot.SetParent(parent, false);
                    pivot.position = position;
                    bones.Add(pivot);
                    return pivot;
                }
                var turret = Pivot("MANTIS turret", Centre(source.Single(f => f.name == "Turret bearing")), root.transform);
                var recoil = Pivot("MANTIS recoil", turret.position, turret);
                nodes["Sensor"].SetParent(turret, true);
                nodes["Emitter"].SetParent(recoil, true);
                var wheels = source.Where(f => f.name.StartsWith("Drive wheel", StringComparison.Ordinal))
                    .ToDictionary(f => f.name, f => Pivot("MANTIS wheel", Centre(f), root.transform));
                if (wheels.Count != 12)
                    throw new InvalidOperationException("Expected six wheels per side");
                var tracks = new[]
                {
                    Pivot("MANTIS tread lower L", root.transform.position, root.transform),
                    Pivot("MANTIS tread upper L", root.transform.position, root.transform),
                    Pivot("MANTIS tread lower R", root.transform.position, root.transform),
                    Pivot("MANTIS tread upper R", root.transform.position, root.transform)
                };

                Transform Driver(MeshFilter part)
                {
                    string name = part.name;
                    if (wheels.TryGetValue(name, out var wheel)) return wheel;
                    if (name.StartsWith("Hub")) return wheels.Values.OrderBy(w => (w.position - Centre(part)).sqrMagnitude).First();
                    int side = root.transform.InverseTransformPoint(Centre(part)).x > 0 ? 2 : 0;
                    if (name.StartsWith("Tread shoe")) return tracks[side];
                    if (name.StartsWith("Track return")) return tracks[side + 1];
                    if (name.StartsWith("Emitter") || name.StartsWith("Arc barrel") || name == "Muzzle") return recoil;
                    if (name == "Turret" || name.StartsWith("Brow armor") || name.StartsWith("Optical socket") ||
                        name.StartsWith("Aux optic") || name.StartsWith("Turret shoulder"))
                        return turret;
                    return root.transform;
                }

                var groups = new Dictionary<Material, List<(CombineInstance geometry, int bone)>>();
                foreach (var part in source.Where(f => !IsIndependentMantisPart(f.name)))
                {
                    var renderer = part.GetComponent<MeshRenderer>();
                    for (int i = 0; i < part.sharedMesh.subMeshCount; i++)
                    {
                        var materialName = renderer.sharedMaterials[i].name.Replace(" (Instance)", "");
                        var material = AssetDatabase.LoadAssetAtPath<Material>(MantisArt + "/Materials/" + materialName + ".mat");
                        if (material == null) throw new InvalidOperationException("Unmapped MANTIS material " + materialName);
                        if (!groups.TryGetValue(material, out var list)) groups.Add(material, list = new());
                        list.Add((new CombineInstance {
                            mesh = part.sharedMesh, subMeshIndex = i,
                            transform = root.transform.worldToLocalMatrix * nodes[part.name].localToWorldMatrix
                        }, bones.IndexOf(Driver(part))));
                    }
                }

                Directory.CreateDirectory(MantisMeshes);
                Directory.CreateDirectory(Path.GetDirectoryName(MantisRig));
                AssetDatabase.Refresh();
                Object.DestroyImmediate(staticGroup.gameObject);
                var renderRoot = new GameObject("Articulated render groups").transform;
                renderRoot.SetParent(root.transform, false);
                long indices = 0;
                foreach (var group in groups.OrderBy(pair => pair.Key.name, StringComparer.Ordinal))
                {
                    var skinMesh = new Mesh { name = group.Key.name, indexFormat = IndexFormat.UInt32 };
                    skinMesh.CombineMeshes(group.Value.Select(part => part.geometry).ToArray(), true, true);
                    var weights = new List<BoneWeight>(skinMesh.vertexCount);
                    foreach (var part in group.Value)
                        for (int i = 0; i < part.geometry.mesh.vertexCount; i++)
                            weights.Add(new BoneWeight { boneIndex0 = part.bone, weight0 = 1 });
                    if (weights.Count != skinMesh.vertexCount || skinMesh.GetIndexCount(0) == 0)
                        throw new InvalidOperationException("MANTIS material group lost its authored geometry");
                    indices += skinMesh.GetIndexCount(0);
                    skinMesh.boneWeights = weights.ToArray();
                    skinMesh.bindposes = bones.Select(b => b.worldToLocalMatrix * root.transform.localToWorldMatrix).ToArray();
                    skinMesh.RecalculateBounds();

                    string path = MantisMeshes + "/" + group.Key.name + ".asset";
                    var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    if (saved != null) { EditorUtility.CopySerialized(skinMesh, saved); Object.DestroyImmediate(skinMesh); }
                    else { AssetDatabase.CreateAsset(skinMesh, path); saved = skinMesh; }
                    var partRenderer = new GameObject(group.Key.name).AddComponent<SkinnedMeshRenderer>();
                    partRenderer.transform.SetParent(renderRoot, false);
                    partRenderer.sharedMesh = saved;
                    partRenderer.sharedMaterial = group.Key;
                    partRenderer.bones = bones.ToArray();
                    partRenderer.rootBone = root.transform;
                    partRenderer.quality = SkinQuality.Bone1;
                    var bounds = saved.bounds;
                    bounds.Expand(new Vector3(3, 1, 3));
                    partRenderer.localBounds = bounds;
                }
                if (groups.Count != 6 || indices != 52008)
                    throw new InvalidOperationException($"Expected six groups and 52008 indices; got {groups.Count} groups and {indices} indices");
                PrefabUtility.SaveAsPrefabAsset(root, MantisRig);
                File.WriteAllText(Path.Combine(Output, "mantis-rig-geometry.txt"),
                    $"Articulated groups={groups.Count}; indices={indices}; bones={bones.Count}; wheels={wheels.Count}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
                if (!readable) { importer.isReadable = false; importer.SaveAndReimport(); }
            }
        }

        private static bool IsIndependentMantisPart(string name) => name.StartsWith("ArmorL") ||
            name.StartsWith("ArmorR") || name.StartsWith("CoolingLeft") || name.StartsWith("CoolingRight") ||
            name.StartsWith("Coolant glass") || name.StartsWith("CoreHatch") || name is "Core" or "Sensor";
    }
}
