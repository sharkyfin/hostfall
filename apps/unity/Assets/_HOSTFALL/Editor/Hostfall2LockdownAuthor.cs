using System.IO;
using System.Linq;
using Hostfall.Mapping;
using Hostfall.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hostfall.Editor
{
    public static partial class Hostfall2Workbench
    {
        [MenuItem("HOSTFALL/2.0/Author Logistics Lockdown")]
        public static void AuthorLockdown()
        {
            WorkstationWorkshop.RequireCopy();
            var scene = EditorSceneManager.OpenScene(PreviewScene);
            if (Object.FindAnyObjectByType<LogisticsLockdown>() != null) return;
            File.Copy(PreviewScene, Path.Combine(Output, "scene-before-logistics-lockdown.unity"), true);
            var room = FindTransform("Sector 05 - Logistics Center");
            var rig = Anchor("PAYLOAD / quarantine gate hardware", room, new Vector3(8.5f, 1, 73.5f));
            var armor = MaterialAt("Dark Metal");
            var steel = MaterialAt("Industrial Metal");
            var sign = MaterialAt("Emergency Red");
            var leaves = new Transform[2];
            Vector3[] gateCentres = { new(6.58f, 1, 73.6f), new(8.5f, 1, 78.45f) };
            for (int i = 0; i < leaves.Length; i++)
            {
                var guide = Anchor("Quarantine gate housing " + i, rig, gateCentres[i]);
                guide.rotation = Quaternion.Euler(0, i == 0 ? 90 : 0, 0);
                Box("Powered track header", guide, new Vector3(0, 1.83f, 0), new Vector3(2.88f, .20f, .36f), steel, false);
                var leaf = Anchor("Quarantine descending leaf " + i, guide, guide.position + Vector3.up * 1.82f);
                leaf.localRotation = Quaternion.identity;
                var panel = Box("Solid quarantine panel", leaf, Vector3.up * .91f, new Vector3(2.68f, 1.81f, .23f), armor, true);
                panel.GetComponent<MeshFilter>().sharedMesh = FacilityArt.BeveledBox();
                panel.AddComponent<MapGeometrySource>().Configure(false, true);
                for (int j = 0; j < 4; j++)
                    Box("Panel stiffener", leaf, new Vector3(0, .26f + j * .41f, -.134f), new Vector3(2.45f, .11f, .046f), steel, false);
                Box("Lockdown edge lamp", leaf, new Vector3(0, .16f, -.139f), new Vector3(2.46f, .042f, .04f), sign, false);
                leaves[i] = leaf;
            }
            var beacon = Anchor("Quarantine alarm status", rig, new Vector3(8.5f, 3.05f, 76.35f)).gameObject.AddComponent<Light>();
            beacon.range = 7;
            beacon.color = new Color(.94f, .2f, .13f);
            beacon.intensity = 2.2f;
            beacon.enabled = false;

            var watcher = Object.FindObjectsByType<WatcherController>(FindObjectsSortMode.None).Single(actor => actor.Id == "watcher-r17");
            var actors = new WatcherController[3];
            var anchors = new[] { new Vector3(9.1f, 1.03f, 77.1f), new Vector3(6.88f, 1.03f, 75.9f), new Vector3(9.35f, 1.03f, 76.2f) };
            Physics.SyncTransforms();
            for (int i = 0; i < anchors.Length; i++)
            {
                if (Physics.CheckCapsule(anchors[i] + Vector3.up * .37f, anchors[i] + Vector3.up * .97f,
                    .27f, Hostfall.Core.CollisionLayers.WorldMask, QueryTriggerInteraction.Ignore))
                    throw new System.InvalidOperationException("Lockdown reserve overlaps world geometry at " + anchors[i]);
                var actor = Object.Instantiate(watcher.gameObject, rig).GetComponent<WatcherController>();
                actor.name = "Logistics response robot " + (i + 1);
                actor.transform.SetPositionAndRotation(anchors[i], Quaternion.Euler(0, 180, 0));
                var fields = new SerializedObject(actor);
                fields.FindProperty("enemyId").stringValue = "aux-watch-logistics-" + i;
                fields.FindProperty("patrolA").vector3Value = anchors[i];
                fields.FindProperty("patrolB").vector3Value = anchors[i] + Vector3.back * 1.25f;
                fields.FindProperty("xBounds").vector2Value = new Vector2(5.52f, 10.1f);
                fields.FindProperty("zBounds").vector2Value = new Vector2(70.85f, 78.26f);
                fields.ApplyModifiedPropertiesWithoutUndo();
                actors[i] = actor;
            }
            rig.gameObject.AddComponent<LogisticsLockdown>().Configure(leaves, actors, beacon);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
