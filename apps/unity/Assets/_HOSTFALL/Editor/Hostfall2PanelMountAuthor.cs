using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hostfall.Devices;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hostfall.Editor
{
    public static partial class Hostfall2Workbench
    {
        [MenuItem("HOSTFALL/2.0/Seat and Audit Worklight Controls")]
        public static void SeatWorklightControls()
        {
            WorkstationWorkshop.RequireCopy();
            var scene = EditorSceneManager.OpenScene(PreviewScene);
            File.Copy(PreviewScene, Path.Combine(Output, "scene-before-switch-mounts.unity"), true);
            var controls = Object.FindObjectsByType<ServiceButtonPanel>(FindObjectsSortMode.None).OrderBy(panel => panel.name);
            var report = new List<string>();
            var metal = MaterialAt("Industrial Metal");
            var black = MaterialAt("Dark Metal");
            Physics.SyncTransforms();
            foreach (var panel in controls)
            {
                var target = panel.GetComponent<BoxCollider>();
                if (target == null) throw new InvalidOperationException("No interaction collider for " + panel.name);
                var start = target.bounds.center + Vector3.up * .11f;
                var hit = Physics.RaycastAll(start, Vector3.down, 3.2f, Hostfall.Core.CollisionLayers.WorldMask, QueryTriggerInteraction.Ignore)
                    .Where(value => !value.transform.IsChildOf(panel.transform) && value.normal.y >= .85f &&
                        value.point.y <= target.bounds.min.y + .08f)
                    .OrderBy(value => value.distance).FirstOrDefault();
                if (hit.collider == null)
                {
                    report.Add(panel.name + " · no clear vertical support");
                    continue;
                }

                float baseY = target.bounds.min.y + .019f;
                float gap = baseY - hit.point.y;
                report.Add(panel.name + " · " + hit.collider.name + " · " + gap.ToString("F3") + " m");
                if (gap <= .029f || panel.transform.Find("Bolted worklight mount") != null) continue;
                var support = Anchor("Bolted worklight mount", panel.transform, new Vector3(start.x, hit.point.y, start.z));
                support.rotation = Quaternion.Euler(0, panel.transform.eulerAngles.y, 0);
                var shaft = Box("Visible switch support", support, Vector3.up * (gap * .5f), new Vector3(.125f, gap, .13f), metal, gap > .26f);
                shaft.GetComponent<MeshFilter>().sharedMesh = FacilityArt.BeveledBox();
                Box("Fastened mounting plate", support, Vector3.up * .019f,
                    new Vector3(.228f, .038f, .238f), black, false);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            File.WriteAllLines(Path.Combine(Output, "worklight-mounts.txt"), report);
        }

        [MenuItem("HOSTFALL/2.0/Review Operator and Minibosses")]
        public static void ReviewOperatorAndMinibosses()
        {
            WorkstationWorkshop.RequireCopy();
            EditorSceneManager.OpenScene(PreviewScene);
            var person = Object.FindAnyObjectByType<Hostfall.Player.OperatorVisual>().transform;
            var head = person.position + Vector3.up * 1.5f;
            CaptureContainmentReview("hf2-operator-profile", head + person.forward * .79f + person.right * .39f, head);
            foreach (var actor in Object.FindObjectsByType<Hostfall.World.FacilityMiniboss>(FindObjectsSortMode.None))
                CaptureContainmentReview("hf2-boss-" + actor.DamageableId, actor.transform.position + actor.transform.forward * 3.55f + Vector3.up * 2.05f,
                    actor.transform.position + Vector3.up * 1.04f);
        }
    }
}
