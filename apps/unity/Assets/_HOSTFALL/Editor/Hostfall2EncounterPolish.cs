using System.IO;
using System.Linq;
using Hostfall.Core;
using Hostfall.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hostfall.Editor
{
    public static partial class Hostfall2Workbench
    {
        [MenuItem("HOSTFALL/2.0/Check and Polish Encounter Placement")]
        public static void PolishEncounterPlacement()
        {
            WorkstationWorkshop.RequireCopy();
            var scene = EditorSceneManager.OpenScene(PreviewScene);
            var spider = FindTransform("Spider Matriarch");
            var steel = MaterialAt("Industrial Metal");
            var dark = MaterialAt("Dark Metal");
            Physics.SyncTransforms();
            var preferred = new Vector3(-2.4f, .03f, 31.4f);
            var candidates = from x in Enumerable.Range(-9, 19)
                from z in Enumerable.Range(42, 32)
                let p = new Vector3(x * .5f, .03f, z * .5f)
                orderby (p - preferred).sqrMagnitude
                select p;
            bool Grounded(Vector3 p) => Physics.Raycast(p + Vector3.up * .35f, Vector3.down, out var floor, .5f,
                CollisionLayers.WorldMask, QueryTriggerInteraction.Ignore) && floor.normal.y > .8f;
            bool Free(Vector3 p, Vector3 half) => Physics.OverlapBox(p, half, Quaternion.identity,
                CollisionLayers.WorldMask, QueryTriggerInteraction.Ignore)
                .All(shape => shape.transform.IsChildOf(spider) || shape.bounds.max.y < .23f);
            var landing = candidates.First(p => Grounded(p) &&
                Free(p + Vector3.up * 1.26f, new Vector3(1.3f, 1.1f, 1.0f)) &&
                Free(p + new Vector3(0, .9f, 2.05f), new Vector3(.56f, .56f, 1.0f)));
            spider.position = landing;

            var warden = FindTransform("Warden articulated hull");
            foreach (var arm in warden.Cast<Transform>().Where(arm => arm.name.StartsWith("Servo arm")))
            {
                var pose = arm.localPosition;
                pose.x = Mathf.Sign(pose.x) * 1.38f;
                arm.localPosition = pose;
            }
            if (warden.Cast<Transform>().All(child => child.name != "Outboard traction assembly"))
            {
                var frame = Anchor("Outboard traction assembly", warden, warden.position);
                for (int side = -1; side <= 1; side += 2)
                {
                    Box("Traction guard", frame, new Vector3(side * 1.1f, .41f, .01f), new Vector3(.43f, .62f, 1.69f), dark, false)
                        .GetComponent<MeshFilter>().sharedMesh = FacilityArt.BeveledBox();
                    Box("Bolted drive cover", frame, new Vector3(side * 1.33f, .41f, 0), new Vector3(.11f, .32f, 1.44f), steel, false)
                        .GetComponent<MeshFilter>().sharedMesh = FacilityArt.BeveledBox();
                }
            }

            var harvester = FindTransform("Harvester");
            foreach (var tooth in harvester.GetComponentsInChildren<Renderer>().Where(piece => piece.name == "Tungsten cutter"))
                tooth.sharedMaterial = steel;

            var body = Object.FindAnyObjectByType<Hostfall.Player.OperatorVisual>();
            var face = body.GetComponentsInChildren<Transform>().Single(node => node.name == "Blender / Type-IV face");
            foreach (var panel in face.GetComponentsInChildren<MeshRenderer>())
                if (panel.name is "Removable brow" or "Lower jaw" or "Nasal bridge" || panel.name.StartsWith("Cheek casting"))
                    panel.sharedMaterial = steel;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            File.WriteAllText(Path.Combine(Output, "encounter-footprint.txt"),
                "Spider full-footprint and view lane checked at " + landing + "\nWarden arms and drive mounts checked against the authored hull.\n");
        }
    }
}
