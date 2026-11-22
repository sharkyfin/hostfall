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
        [MenuItem("HOSTFALL/2.0/Bind Witness Dispatch Shortcut")]
        public static void BindWitnessDispatchShortcut()
        {
            WorkstationWorkshop.RequireCopy();
            EditorSceneManager.OpenScene(PreviewScene);

            var gate = Object.FindObjectsByType<FacilityDoor>(FindObjectsSortMode.None)
                .Single(door => door.DoorId == ComputerCampaign.QuorumShortcut);
            if (gate.transform.Find("CLOUD-09 / west bypass sign") != null) return;
            File.Copy(PreviewScene, Path.Combine(Output, "scene-before-quorum.unity"), true);
            var data = new SerializedObject(gate);
            data.FindProperty("condition").enumValueIndex = (int)DoorAccessCondition.ManualUnlockOnly;
            data.FindProperty("lockedPromptText").stringValue = "Западный обход закрыт · подтвердите кворум в диспетчерской CLOUD-09";
            data.ApplyModifiedPropertiesWithoutUndo();

            var dark = MaterialAt("Dark Metal");
            var steel = MaterialAt("Industrial Metal");
            var green = MaterialAt("Indicator Green");
            var sign = Anchor("CLOUD-09 / west bypass sign", gate.transform, gate.transform.position + Vector3.up * 3.54f);
            sign.rotation = Quaternion.Euler(0, 90, 0);
            Box("West bypass label back", sign, Vector3.zero, new Vector3(2.45f, .29f, .13f), dark, false);
            AddText("ЗАПАД · ЛОГИСТИКА", sign, new Vector3(0, 0, -.071f), Quaternion.identity, .042f);
            AddText("ДИСПЕТЧЕРСКАЯ", sign, new Vector3(0, 0, .071f), Quaternion.Euler(0, 180, 0), .042f);

            var sector = FindTransform("Sector 09 - Cloud Control");
            var witnesses = Anchor("CLOUD-09 witness register", sector, new Vector3(63.97f, 1.33f, 114.8f));
            witnesses.rotation = Quaternion.Euler(0, 90, 0);
            string[] labels = { "РЕЛЕ", "АРХИВ", "ЧАСЫ" };
            for (int i = 0; i < labels.Length; i++)
            {
                var housing = Box("Witness register housing", witnesses, new Vector3((i - 1) * .43f, 0, 0),
                    new Vector3(.365f, .42f, .12f), dark, true);
                Box("Witness module face", housing.transform, new Vector3(0, 0, -.55f), new Vector3(.81f, .77f, .16f), steel, false);
                Box("Witness local readiness lamp", housing.transform, new Vector3(0, .25f, -.66f),
                    new Vector3(.42f, .07f, .14f), green, false);
                AddText(labels[i], witnesses, new Vector3((i - 1) * .43f, -.06f, -.086f), Quaternion.identity, .019f);
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }
    }
}
