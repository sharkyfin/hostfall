using System.IO;
using System.Linq;
using Hostfall.Devices;
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
        [MenuItem("HOSTFALL/2.0/Author Passage Repair Locker")]
        public static void AuthorPassageRepairLocker()
        {
            WorkstationWorkshop.RequireCopy();
            EditorSceneManager.OpenScene(PreviewScene);
            if (Object.FindAnyObjectByType<BufferCachePickup>() != null) return;

            File.Copy(PreviewScene, Path.Combine(Output, "scene-before-buffer.unity"), true);
            var station = Object.FindObjectsByType<ComputerStation>(FindObjectsSortMode.None)
                .Single(device => device.StationId == ComputerCampaign.BufferStation);
            var root = Anchor("EDGE-01 / emergency repair locker", station.transform.parent, new Vector3(-1.245f, 1.24f, 5.22f));
            root.localRotation = Quaternion.Euler(0, 90, 0);

            var dark = MaterialAt("Dark Metal");
            var rail = MaterialAt("Industrial Metal");
            var green = MaterialAt("Indicator Green");
            var amber = MaterialAt("Indicator Amber");
            Box("Locker wall mount", root, new Vector3(0, 0, .014f), new Vector3(.78f, .74f, .036f), dark, true);
            foreach (int side in new[] { -1, 1 })
            {
                Box("Locker folded steel upright", root, new Vector3(side * .38f, 0, .102f), new Vector3(.061f, .74f, .18f), rail, true);
                Box("Locker upper and lower sill", root, new Vector3(0, side * .354f, .105f), new Vector3(.73f, .044f, .18f), rail, true);
                Box("Locker retention bracket", root, new Vector3(side * .27f, 0, .072f), new Vector3(.052f, .58f, .058f), dark, false);
            }
            Box("Locker actuator status lamp", root, new Vector3(0, .357f, .203f), new Vector3(.21f, .021f, .018f), amber, false);

            var leaf = Anchor("Repair locker sliding leaf", root, root.position);
            Box("Locker steel front", leaf, new Vector3(0, -.016f, .175f), new Vector3(.714f, .662f, .054f), dark, true);
            Box("Locker front reinforcement", leaf, new Vector3(0, -.02f, .207f), new Vector3(.618f, .54f, .014f), rail, false);
            Box("Locker pull recess", leaf, new Vector3(0, -.22f, .224f), new Vector3(.267f, .069f, .029f), dark, true);
            AddText("РЕМКОМПЛЕКТ", leaf, new Vector3(0, .07f, .224f), Quaternion.Euler(0, 180, 0), .022f);

            var device = root.gameObject.AddComponent<FacilityDoor>();
            device.Configure(leaf, DoorAccessCondition.ManualUnlockOnly, "Шкаф закрыт · восстановите буфер EDGE-01",
                "[E] Открыть ремонтный шкаф", "[E] Закрыть ремонтный шкаф", true, Vector3.up, .73f, 1.5f, ComputerCampaign.BufferCache);

            var pickup = Anchor("TYPE-IV / field repair package", root, root.TransformPoint(new Vector3(0, -.025f, .105f)));
            pickup.gameObject.AddComponent<BufferCachePickup>();
            Box("Device field kit chassis", pickup, Vector3.zero, new Vector3(.47f, .35f, .105f), rail, true);
            Box("Device field kit inset", pickup, new Vector3(0, 0, .059f), new Vector3(.378f, .247f, .023f), dark, false);
            Box("Device repair lamp A", pickup, new Vector3(0, 0, .075f), new Vector3(.17f, .031f, .019f), green, false);
            Box("Device repair lamp B", pickup, new Vector3(0, 0, .075f), new Vector3(.032f, .17f, .019f), green, false);
            AddText("EDGE-01", root, new Vector3(0, .52f, .049f), Quaternion.Euler(0, 180, 0), .027f);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            CaptureContainmentReview("hf2-buffer-locker", new Vector3(.54f, 1.65f, 5.65f), new Vector3(-1.12f, 1.22f, 5.22f));
        }
    }
}
