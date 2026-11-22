using System.IO;
using System.Linq;
using Hostfall.Player;
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
        [MenuItem("HOSTFALL/2.0/Author Minibosses and Operator Face")]
        public static void AuthorMinibosses()
        {
            WorkstationWorkshop.RequireCopy();
            var scene = EditorSceneManager.OpenScene(PreviewScene);
            if (Object.FindAnyObjectByType<FacilityMiniboss>() != null) return;
            File.Copy(PreviewScene, Path.Combine(Output, "scene-before-minibosses.unity"), true);
            var steel = MaterialAt("Industrial Metal");
            var dark = MaterialAt("Dark Metal");
            var amber = MaterialAt("Indicator Amber");
            var cyan = MaterialAt("Indicator Green");
            var red = MaterialAt("Emergency Red");

            GameObject Form(string label, Transform parent, Vector3 p, Vector3 size, Material material, bool solid = false)
            {
                var piece = Box(label, parent, p, size, material, solid);
                piece.GetComponent<MeshFilter>().sharedMesh = FacilityArt.BeveledBox();
                return piece;
            }

            void Cylinder(string label, Transform parent, Vector3 p, Vector3 size, Quaternion rotation, Material material)
            {
                var joint = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                joint.name = label;
                joint.transform.SetParent(parent, false);
                joint.transform.localPosition = p;
                joint.transform.localRotation = rotation;
                joint.transform.localScale = size;
                joint.GetComponent<Renderer>().sharedMaterial = material;
                Object.DestroyImmediate(joint.GetComponent<Collider>());
            }

            void ReuseBody(Transform root, Transform assembly, Component legacy)
            {
                foreach (var child in root.Cast<Transform>().Where(child => child != assembly).ToArray()) child.SetParent(assembly, true);
                var hull = root.GetComponent<BoxCollider>();
                if (hull != null)
                {
                    var solid = assembly.gameObject.AddComponent<BoxCollider>();
                    solid.center = hull.center;
                    solid.size = hull.size;
                    Object.DestroyImmediate(hull);
                }
                Object.DestroyImmediate(legacy);
            }

            FacilityMiniboss Boss(FacilityMinibossKind kind, Transform root, Transform assembly, Transform shield,
                Transform reserve, Transform debris, Transform[] actuators, Vector3 lensPoint)
            {
                var encounter = root.gameObject.AddComponent<FacilityMiniboss>();
                var lens = Form("Exposed optical coupler", assembly, lensPoint, new Vector3(.43f, .34f, .16f), red, true);
                lens.layer = Hostfall.Core.CollisionLayers.RobotHitbox;
                lens.AddComponent<MinibossComponent>().Configure(encounter);
                var lamp = lens.AddComponent<Light>();
                lamp.intensity = 2;
                lamp.range = 3.3f;
                lamp.color = new Color(1f, .52f, .21f);
                lamp.shadows = LightShadows.None;
                encounter.Configure(kind, assembly, shield, reserve, debris, actuators, amber, lamp);
                return encounter;
            }

            Transform Support(Transform root, Vector3 p, Material signal)
            {
                var drone = Anchor("Mounted reserve drone", root, root.TransformPoint(p));
                Form("Stowed reserve shell", drone, Vector3.zero, new Vector3(.79f, .31f, .53f), steel);
                Form("Reserve emitter", drone, new Vector3(0, .03f, .266f), new Vector3(.29f, .1f, .063f), signal);
                return drone;
            }

            var spiderArea = FindTransform("Sector 02 - Compute Vault");
            Physics.SyncTransforms();
            var preferred = new Vector3(-4.6f, .03f, 33.4f);
            var candidates = from x in Enumerable.Range(-10, 17)
                from z in Enumerable.Range(37, 36)
                let point = new Vector3(x * .5f, .03f, z * .5f)
                orderby (point - preferred).sqrMagnitude
                select point;
            Vector3 spiderFloor = candidates.First(point =>
                Physics.Raycast(point + Vector3.up * .31f, Vector3.down, out var floor, .48f, Hostfall.Core.CollisionLayers.WorldMask, QueryTriggerInteraction.Ignore) &&
                floor.normal.y > .8f &&
                Physics.OverlapCapsule(point + Vector3.up * .60f, point + Vector3.up * 1.14f, .47f,
                    Hostfall.Core.CollisionLayers.WorldMask, QueryTriggerInteraction.Ignore).All(shape => shape.bounds.max.y < .21f));
            var spider = Anchor("Spider Matriarch", spiderArea, spiderFloor);
            var abdomen = Anchor("Matriarch articulated shell", spider, spider.position);
            Form("Armored abdomen", abdomen, new Vector3(0, .68f, -.49f), new Vector3(1.16f, .57f, 1.46f), dark, true);
            Form("Cephalothorax", abdomen, new Vector3(0, .58f, .34f), new Vector3(1.36f, .5f, .82f), steel, true);
            var legs = new Transform[8];
            for (int i = 0; i < legs.Length; i++)
            {
                float sign = i < 4 ? -1 : 1;
                float along = -.68f + .39f * (i % 4);
                var joint = Anchor("Articulated hip " + i, abdomen, abdomen.TransformPoint(new Vector3(.49f * sign, .57f, along)));
                Cylinder("Hip bearing", joint, Vector3.zero, new Vector3(.29f, .12f, .29f), Quaternion.Euler(0, 0, 90), amber);
                Form("Upper link", joint, new Vector3(.32f * sign, .06f, 0), new Vector3(.68f, .17f, .19f), steel).transform.localRotation = Quaternion.Euler(0, 0, sign * 19);
                Form("Ground talon", joint, new Vector3(.7f * sign, -.21f, .07f), new Vector3(.18f, .73f, .18f), dark).transform.localRotation = Quaternion.Euler(0, 0, sign * 20);
                legs[i] = joint;
            }
            Boss(FacilityMinibossKind.SpiderMatriarch, spider, abdomen, null, Support(spider, new Vector3(0, 1.38f, -.76f), cyan), null, legs, new Vector3(0, .57f, .81f));

            var oldWarden = Object.FindAnyObjectByType<BulwarkSentryController>();
            var warden = oldWarden.transform;
            warden.name = "Security Warden";
            var sentinel = Anchor("Warden articulated hull", warden, warden.position);
            ReuseBody(warden, sentinel, oldWarden);
            var shield = sentinel.Find("Frontal Ballistic Shield");
            var arms = new Transform[2];
            for (int i = 0; i < arms.Length; i++)
            {
                float side = i == 0 ? -1 : 1;
                var arm = Anchor("Servo arm " + i, sentinel, sentinel.TransformPoint(new Vector3(side * 1.02f, 1.33f, 0)));
                Cylinder("Shoulder reduction gear", arm, Vector3.zero, new Vector3(.52f, .24f, .52f), Quaternion.Euler(0, 0, 90), steel);
                Form("Forearm casting", arm, new Vector3(side * .13f, -.22f, .32f), new Vector3(.38f, .53f, .83f), dark);
                Form("Mechanical knuckle", arm, new Vector3(side * .12f, -.24f, .72f), new Vector3(.36f, .34f, .31f), steel);
                arms[i] = arm;
            }
            var guard = Boss(FacilityMinibossKind.SecurityWarden, warden, sentinel, shield,
                Support(warden, new Vector3(0, 2.56f, -.78f), amber), null, arms, new Vector3(0, 1.99f, .76f));
            var panel = Anchor("Warden shield supply pedestal", warden.parent, warden.position + new Vector3(-4.3f, 0, -1.6f));
            Form("Bolted pedestal foot", panel, new Vector3(0, .072f, 0), new Vector3(.61f, .14f, .48f), dark, true);
            Form("Load-bearing column", panel, new Vector3(0, .65f, 0), new Vector3(.23f, 1.18f, .29f), steel, true);
            var housing = Form("Disconnect enclosure", panel, new Vector3(0, 1.28f, 0), new Vector3(.41f, .38f, .31f), dark, true);
            var lever = Form("Mechanical lever", panel, new Vector3(0, 1.29f, -.185f), new Vector3(.19f, .086f, .13f), amber);
            housing.AddComponent<WardenBreaker>().Configure(guard, lever.transform);

            var oldHarvester = Object.FindAnyObjectByType<BastionSentryController>();
            var harvester = oldHarvester.transform;
            harvester.name = "Harvester";
            var excavator = Anchor("Harvester articulated chassis", harvester, harvester.position);
            ReuseBody(harvester, excavator, oldHarvester);
            var tools = new Transform[2];
            for (int i = 0; i < tools.Length; i++)
            {
                float side = i == 0 ? -1 : 1;
                var arm = Anchor("Blade gantry " + i, excavator, excavator.TransformPoint(new Vector3(side * 1.33f, 1.5f, .22f)));
                Form("Hydraulic bearing rail", arm, new Vector3(0, 0, .46f), new Vector3(.43f, .49f, 1.23f), steel);
                var blades = Anchor("Saw spindle", arm, arm.TransformPoint(new Vector3(0, 0, 1.13f)));
                Cylinder("Serrated disc", blades, Vector3.zero, new Vector3(1.12f, .06f, 1.12f), Quaternion.Euler(90, 0, 0), dark);
                for (int j = 0; j < 8; j++)
                {
                    float angle = j * Mathf.PI / 4;
                    var tooth = Form("Tungsten cutter", blades, new Vector3(Mathf.Cos(angle) * .52f, Mathf.Sin(angle) * .52f, 0),
                        new Vector3(.2f, .12f, .12f), amber);
                    tooth.transform.localRotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg);
                }
                tools[i] = blades;
            }
            var chunk = Form("Loose welded debris", harvester, new Vector3(0, .46f, .2f), new Vector3(.73f, .39f, .57f), dark);
            chunk.SetActive(false);
            Boss(FacilityMinibossKind.Harvester, harvester, excavator, null, null, chunk.transform, tools, new Vector3(0, 1.69f, 1.45f));

            var operatorBody = Object.FindAnyObjectByType<OperatorVisual>();
            var head = operatorBody.GetComponentsInChildren<Transform>(true).Single(part => part.name == "Head / first person shadow");
            foreach (var visor in head.GetComponentsInChildren<Renderer>().Where(renderer => renderer.name.Contains("Visor")))
                visor.gameObject.SetActive(false);
            var faceAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_HOSTFALL/Art/Hostfall2/OperatorFace.fbx");
            if (faceAsset == null) throw new System.InvalidOperationException("Blender operator face export is missing");
            var face = (GameObject)PrefabUtility.InstantiatePrefab(faceAsset, head);
            face.name = "Blender / Type-IV face";
            face.transform.localPosition = new Vector3(0, 1.65f, .098f);
            face.transform.localRotation = Quaternion.identity;
            foreach (var mesh in face.GetComponentsInChildren<MeshRenderer>())
            {
                mesh.gameObject.layer = Hostfall.Core.CollisionLayers.Player;
                if (mesh.name.StartsWith("Optical lens")) mesh.sharedMaterial = cyan;
            }
            var fieldsBody = new SerializedObject(operatorBody);
            var shadows = fieldsBody.FindProperty("head");
            var renderers = head.GetComponentsInChildren<Renderer>();
            shadows.arraySize = renderers.Length;
            for (int i = 0; i < renderers.Length; i++) shadows.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
            fieldsBody.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
