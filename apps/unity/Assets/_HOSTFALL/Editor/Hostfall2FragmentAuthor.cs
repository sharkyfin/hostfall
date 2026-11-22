using System.IO;
using System.Linq;
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
        [MenuItem("HOSTFALL/2.0/Bind Archive Volume Armory")]
        public static void BindArchiveVolumeArmory()
        {
            WorkstationWorkshop.RequireCopy();
            EditorSceneManager.OpenScene(PreviewScene);
            var door = Object.FindObjectsByType<FacilityDoor>(FindObjectsSortMode.None)
                .Single(gate => gate.DoorId == ComputerCampaign.FragmentArmory);
            if (door.transform.Find("DATA-01 / local key reader") != null) return;

            File.Copy(PreviewScene, Path.Combine(Output, "scene-before-fragments.unity"), true);
            var saved = new SerializedObject(door);
            saved.FindProperty("lockedPromptText").stringValue = "Оружейная закрыта · восстановите таблицу FRAGMENT DOCK на DATA-01";
            saved.ApplyModifiedPropertiesWithoutUndo();

            var metal = MaterialAt("Industrial Metal");
            var dark = MaterialAt("Dark Metal");
            var amber = MaterialAt("Indicator Amber");
            var reader = Anchor("DATA-01 / local key reader", door.transform,
                door.transform.TransformPoint(new Vector3(-1.48f, 1.18f, -.225f)));
            Box("Armory local key reader housing", reader, Vector3.zero, new Vector3(.19f, .28f, .13f), dark, true);
            Box("Armory key socket", reader, new Vector3(0, .039f, -.072f), new Vector3(.104f, .047f, .028f), metal, false);
            Box("Armory local key indicator", reader, new Vector3(0, -.063f, -.089f), new Vector3(.086f, .016f, .018f), amber, false);
            AddText("DATA-01", reader, new Vector3(0, .17f, -.06f), Quaternion.identity, .013f);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }
    }
}
