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
        [MenuItem("HOSTFALL/2.0/Author Release Ending Art")]
        public static void AuthorFinalArt()
        {
            WorkstationWorkshop.RequireCopy();
            var scene = EditorSceneManager.OpenScene(PreviewScene);
            if (Object.FindAnyObjectByType<RelayHorizon>() != null) return;
            File.Copy(PreviewScene, Path.Combine(Output, "scene-before-ending-art.unity"), true);
            var reveal = Object.FindAnyObjectByType<CoreRevealSequence>();
            var avatar = Object.FindAnyObjectByType<CoreAvatarEncounter>();
            var sector = avatar.transform.parent;
            var steel = MaterialAt("Industrial Metal");
            var dark = MaterialAt("Dark Metal");
            var warm = MaterialAt("Indicator Amber");

            foreach (var part in avatar.GetComponentsInChildren<MeshFilter>())
                if (part.sharedMesh.name == "Cube") part.sharedMesh = FacilityArt.BeveledBox();
            var facade = avatar.transform;
            for (int side = -1; side <= 1; side += 2)
            {
                var powerFeed = Anchor("Arm brace and coolant bus", facade, facade.TransformPoint(new Vector3(side * 4.2f, 4.72f, 1.46f)));
                for (int rail = 0; rail < 4; rail++)
                {
                    Box("Reinforced bus channel", powerFeed, new Vector3(0, rail * .29f - .43f, 0), new Vector3(2.2f, .13f, .49f), steel, false)
                        .GetComponent<MeshFilter>().sharedMesh = FacilityArt.BeveledBox();
                }
                var kitPath = "Assets/_HOSTFALL/Content/Prefabs/Kit/Prop_Vent_Big.prefab";
                var kit = AssetDatabase.LoadAssetAtPath<GameObject>(kitPath);
                if (kit != null)
                {
                    var vent = Object.Instantiate(kit, facade);
                    vent.name = "Mounted Quaternius radiator grille";
                    vent.SetActive(false);
                    foreach (var collider in vent.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
                    vent.transform.localPosition = new Vector3(side * 5.2f, 4.7f, 1.12f);
                    vent.transform.localRotation = Quaternion.Euler(0, 180, 0);
                    vent.transform.localScale = Vector3.one * .76f;
                    vent.SetActive(true);
                }
                Box("Armored cable duct", facade, new Vector3(side * 2.55f, 2.6f, 2.3f), new Vector3(.41f, 2.5f, .64f), steel, false)
                    .GetComponent<MeshFilter>().sharedMesh = FacilityArt.BeveledBox();
            }

            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_HOSTFALL/Art/Hostfall2/RelayValley.fbx");
            if (source == null) throw new System.InvalidOperationException("Blender valley export is missing");
            var valley = (GameObject)PrefabUtility.InstantiatePrefab(source, sector);
            valley.name = "Blender / north valley ridges";
            valley.transform.position = new Vector3(10, -1.52f, 219);
            valley.transform.rotation = Quaternion.identity;
            valley.transform.localScale = Vector3.one;
            var oldShelf = FindTransform("Valley shelf");
            oldShelf.gameObject.SetActive(false);
            var shutterPair = new Transform[2];
            for (int index = 0; index < 2; index++)
            {
                float x = index == 0 ? 6.02f : 13.98f;
                var frame = Anchor("Observation window sliding armor", sector, new Vector3(x, 2.46f, 205.89f));
                Box("Shutter shell", frame, Vector3.zero, new Vector3(7.91f, 4.45f, .14f), dark, false)
                    .GetComponent<MeshFilter>().sharedMesh = FacilityArt.BeveledBox();
                for (int rib = 0; rib < 4; rib++)
                    Box("Shutter rib", frame, new Vector3(0, rib * 1.04f - 1.56f, -.08f), new Vector3(7.8f, .11f, .08f), steel, false);
                shutterPair[index] = frame;
            }

            var effect = Anchor("Fictional exterior catastrophe", sector, new Vector3(10, 1.65f, 205.1f));
            var flash = effect.gameObject.AddComponent<Light>();
            flash.type = LightType.Point;
            flash.shadows = LightShadows.None;
            flash.color = new Color(1, .63f, .44f);
            flash.enabled = false;
            flash.range = 10.5f;

            var cloudPoints = new[] { new Vector3(2.4f, -.9f, 247), new Vector3(27, -.1f, 267), new Vector3(-22, .1f, 285) };
            var clouds = new Transform[cloudPoints.Length];
            for (int i = 0; i < clouds.Length; i++)
            {
                var cloud = Anchor("Far horizon rising cloud " + (i + 1), sector, cloudPoints[i]);
                void Puff(string name, Vector3 local, Vector3 scale, Material material)
                {
                    var obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    obj.name = name;
                    obj.transform.SetParent(cloud, false);
                    obj.transform.localPosition = local;
                    obj.transform.localScale = scale;
                    obj.GetComponent<Renderer>().sharedMaterial = material;
                    Object.DestroyImmediate(obj.GetComponent<Collider>());
                }
                Puff("Rising column", new Vector3(0, 5.8f, 0), new Vector3(3.15f, 12, 3.05f), steel);
                Puff("Lit column base", new Vector3(0, 1.5f, -.34f), new Vector3(6f, 4f, 4f), warm);
                for (int j = 0; j < 6; j++)
                {
                    float angle = j * Mathf.PI / 3f;
                    Puff("High smoke billow", new Vector3(Mathf.Cos(angle) * 2.7f, 11.5f + (j % 2) * 1.1f, Mathf.Sin(angle) * 2.6f),
                        new Vector3(5.8f, 4.3f, 4.8f), j < 2 ? steel : dark);
                }
                cloud.localScale = Vector3.one * (1f + i * .23f);
                cloud.gameObject.SetActive(false);
                clouds[i] = cloud;
            }

            var horizon = effect.gameObject.AddComponent<RelayHorizon>();
            horizon.Configure(clouds, shutterPair, flash);
            reveal.BindHorizon(horizon);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
