using System;
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
        [MenuItem("HOSTFALL/2.0/Fit Operations Duct and Pursuit")]
        public static void FitOperationsDuctAndPursuit()
        {
            var scene = OpenClearanceScene();
            var vent = Object.FindAnyObjectByType<VentReleaseController>();
            if (vent.transform.Find("Service duct extension") != null) return;
            var office = FindTransform("SOC / Operations Center");
            var watcher = Object.FindObjectsByType<WatcherController>().Single(actor => actor.Id == "watcher-r17");
            ExpandPatrolBounds(watcher, new Vector2(-4, 4), new Vector2(-6.65f, 10.6f));
            ExpandPatrolBounds(vent.Actor, new Vector2(-4, 4), new Vector2(-6.65f, 6.75f));

            const float centreZ = -5.60f;
            const float openingHalfWidth = .88f;
            const float clearHeight = 1.69f;
            var shell = office.Find("SOC East Shell South");
            var wall = shell.GetComponent<MeshRenderer>().sharedMaterial;
            var liner = office.Cast<Transform>().First(t => t.name == "Wall liner" &&
                t.position.x > 4.8f && Mathf.Abs(t.position.z + 6) < .2f).GetComponent<MeshRenderer>().sharedMaterial;
            var rim = new GameObject("Operations east wall / cut duct aperture").transform;
            rim.SetParent(office, false);
            foreach (var member in office.Cast<Transform>().Where(t =>
                t == shell || t.name == "WallAstra_Straight" && t.GetComponent<Collider>() is { } panels &&
                    panels.bounds.center.x > 5 && Mathf.Abs(panels.bounds.center.z + 5) < .1f ||
                (t.name is "Wall liner" or "Wall skirting") && t.position.x > 4.8f
                    && Mathf.Abs(t.position.z + 5) < 1.1f).ToArray())
                Object.DestroyImmediate(member.gameObject);

            WallSlice("Wall south of duct", -7f, centreZ - openingHalfWidth, 0, 3.3f);
            WallSlice("Wall north of duct", centreZ + openingHalfWidth, -3f, 0, 3.3f);
            WallSlice("Duct aperture lintel", centreZ - openingHalfWidth, centreZ + openingHalfWidth, clearHeight, 3.3f);

            var duct = Anchor("Service duct extension", vent.transform, new Vector3(4.83f, 0, centreZ));
            var casing = MaterialAt("Dark Metal");
            var steel = MaterialAt("Industrial Metal");
            Box("Service duct floor", duct, new Vector3(1.625f, -.085f, 0), new Vector3(3.25f, .14f, 1.9f), steel, true);
            foreach (float side in new[] { -1f, 1f })
            {
                Box("Duct extension side", duct, new Vector3(1.625f, .845f, side * .94f), new Vector3(3.25f, 1.72f, .12f), casing, true);
                Box("Riser side", duct, new Vector3(2.51f, 2.50f, side * .94f), new Vector3(1.46f, 1.64f, .12f), casing, true);
                Box("Bolted duct flange", duct, new Vector3(.40f, .845f, side * .93f),
                    new Vector3(.10f, 1.72f, .075f), steel, false);
            }
            Box("Cross-duct roof", duct, new Vector3(.895f, 1.75f, 0), new Vector3(1.79f, .12f, 1.92f), casing, true);
            Box("Riser front", duct, new Vector3(1.79f, 2.50f, 0), new Vector3(.12f, 1.64f, 1.88f), casing, true);
            Box("Bend rear panel", duct, new Vector3(3.20f, 1.645f, 0), new Vector3(.12f, 3.31f, 1.89f), casing, true);
            Box("Riser cap", duct, new Vector3(2.50f, 3.34f, 0), new Vector3(1.54f, .14f, 1.91f), casing, true);
            Box("Raised duct threshold", duct, new Vector3(.25f, -.010f, 0), new Vector3(.50f, .035f, 1.76f), steel, true);

            var damper = Box("Isolation damper V-14", duct, new Vector3(.96f, .81f, 0),
                new Vector3(.09f, 1.62f, 1.75f), steel, true).transform;
            for (int i = 0; i < 5; i++)
                Box("Stiffened damper section", damper, new Vector3(-.53f, -.4f + .2f * i, 0),
                    new Vector3(.26f, .043f, .97f), casing, false);
            var lamp = Anchor("Duct inspection lamp", duct, new Vector3(5.45f, 1.51f, centreZ - .74f));
            Box("V-14 lamp bracket", lamp, new Vector3(0, 0, -.13f), new Vector3(.25f, .16f, .08f), steel, false);
            var glow = lamp.gameObject.AddComponent<Light>();
            glow.range = 3.4f;
            glow.intensity = .48f;
            glow.color = new Color(.94f, .31f, .13f);
            glow.shadows = LightShadows.None;

            var cable = Object.FindObjectsByType<MeshRenderer>().Single(item =>
                item.transform.parent?.name == "Prop_Cable_1" &&
                Vector3.Distance(item.bounds.center, new Vector3(4.35f, .05f, -4.90f)) < .18f);
            var cableRoot = cable.transform.parent;
            var cableCentre = cable.bounds.center;
            cableRoot.position += new Vector3(4.40f - cableCentre.x, 0, -3.93f - cableCentre.z);
            Physics.SyncTransforms();
            if (!Physics.Raycast(cable.bounds.center + Vector3.up * .5f, Vector3.down, out var support,
                    .75f, CollisionLayers.WorldMask, QueryTriggerInteraction.Ignore) ||
                Mathf.Abs(support.normal.y) < .95f)
                throw new InvalidOperationException("No floor support for the Operations cable coil");
            cableRoot.position += Vector3.up * (support.point.y - cable.bounds.min.y + .003f);

            var staging = new Vector3(6.94f, 0, centreZ);
            vent.Actor.transform.position = staging;
            vent.ConfigureDuct(staging, damper, glow);
            var grate = vent.transform.Find("Sliding vent grate");
            var grateColliders = grate.GetComponentsInChildren<Collider>();
            foreach (var shape in grateColliders) shape.enabled = false;
            damper.GetComponent<Collider>().enabled = false;
            Physics.SyncTransforms();
            foreach (var hit in Physics.CapsuleCastAll(staging + Vector3.up * .53f, staging + Vector3.up * 1.04f, .43f,
                Vector3.left, staging.x - vent.ExitPosition.x, CollisionLayers.WorldMask, QueryTriggerInteraction.Ignore))
                if (!hit.collider.transform.IsChildOf(vent.Actor.transform))
                    throw new InvalidOperationException("V-14 emergence intersects " + hit.collider.name);
            for (float x = staging.x; x >= vent.ExitPosition.x; x -= .2f)
                if (!Physics.Raycast(new Vector3(x, .2f, centreZ), Vector3.down, .32f, CollisionLayers.WorldMask, QueryTriggerInteraction.Ignore))
                    throw new InvalidOperationException($"V-14 floor has a gap at {x:F2}");
            damper.GetComponent<Collider>().enabled = true;
            foreach (var shape in grateColliders) shape.enabled = true;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            CaptureContainmentReview("hf2-operations-duct", new Vector3(2.35f, 1.25f, -5.87f), new Vector3(7.7f, 1.01f, centreZ));
            grate.position += Vector3.up * 1.7f;
            damper.position += Vector3.up * 1.7f;
            CaptureContainmentReview("hf2-operations-duct-open", new Vector3(2.85f, 1.28f, -5.64f),
                new Vector3(7.6f, .98f, centreZ));
            grate.position -= Vector3.up * 1.7f;
            damper.position -= Vector3.up * 1.7f;

            void WallSlice(string title, float from, float to, float bottomY, float topY)
            {
                var center = new Vector3(5.1f, (topY + bottomY) / 2, (to + from) / 2);
                var size = new Vector3(.40f, topY - bottomY, to - from);
                Box(title, rim, rim.InverseTransformPoint(center), size, wall, true);
                Box(title + " liner", rim, rim.InverseTransformPoint(center - Vector3.right * .216f),
                    new Vector3(.032f, size.y - .014f, size.z - .008f), liner, false);
            }
        }
    }
}
