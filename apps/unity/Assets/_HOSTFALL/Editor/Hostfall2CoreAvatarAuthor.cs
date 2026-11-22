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
        [MenuItem("HOSTFALL/2.0/Author Core Avatar Encounter")]
        public static void AuthorCoreAvatarEncounter()
        {
            WorkstationWorkshop.RequireCopy();
            var scene = EditorSceneManager.OpenScene(PreviewScene);
            if (Object.FindAnyObjectByType<CoreAvatarEncounter>() != null) return;
            File.Copy(PreviewScene, Path.Combine(Output, "scene-before-core-avatar.unity"), true);
            var placeholder = Object.FindObjectsByType<AtlasBossController>(FindObjectsSortMode.None).Single();
            var platform = placeholder.transform;
            Object.DestroyImmediate(placeholder);
            platform.name = "SUPER / ATLAS-0 containment avatar";
            platform.rotation = Quaternion.Euler(0, 180, 0);
            var encounter = platform.gameObject.AddComponent<CoreAvatarEncounter>();

            var baseMetal = MaterialAt("Industrial Metal");
            var armor = MaterialAt("Dark Metal");
            var amber = MaterialAt("Indicator Amber");
            var red = MaterialAt("Emergency Red");
            var green = MaterialAt("Indicator Green");

            Transform Socket(string label, Vector3 position)
            {
                var socket = new GameObject(label).transform;
                socket.SetParent(platform, false);
                socket.localPosition = position;
                return socket;
            }

            void MarkComponent(GameObject node, CoreAvatarPart part)
            {
                node.layer = Hostfall.Core.CollisionLayers.RobotHitbox;
                node.AddComponent<CoreAvatarHitbox>().Configure(encounter, part);
            }

            var lens = Socket("Sliding reactor optical assembly", new Vector3(0, 1.71f, 3.1f));
            Box("Reactor ceramic housing", lens, new Vector3(0, 0, -.09f), new Vector3(2.05f, 1.33f, .31f), armor, false);
            var reactor = Box("Low reactor lens / exposed during recovery", lens, new Vector3(0, 0, .16f),
                new Vector3(1.13f, .62f, .19f), amber, true);
            MarkComponent(reactor, CoreAvatarPart.Reactor);
            for (int side = -1; side <= 1; side += 2)
            {
                Box("Steel lower reaction pad", platform, new Vector3(side * 2.84f, .32f, .35f),
                    new Vector3(1.18f, .64f, 3.5f), baseMetal, true);
                var relay = Box("Power relay / target under field protection", platform, new Vector3(side * 2.15f, 2.06f, 3.21f),
                    new Vector3(.72f, 1.16f, .26f), red, true);
                MarkComponent(relay, CoreAvatarPart.Relay);
                Box("Relay bracing rail", platform, new Vector3(side * 2.6f, 2.07f, 3.07f),
                    new Vector3(.2f, 1.38f, .48f), armor, false);
            }

            var left = Socket("Rotating emitter L", new Vector3(-5.2f, 1.95f, 2.53f));
            var right = Socket("Rotating emitter R", new Vector3(5.2f, 1.95f, 2.53f));
            foreach (var emitter in new[] { left, right })
            {
                Box("Swiveling chamber", emitter, new Vector3(0, 0, -.50f), new Vector3(.75f, .71f, 1.02f), baseMetal, false);
                Box("Impulse port", emitter, new Vector3(0, 0, -.05f), new Vector3(.47f, .37f, .11f), amber, false);
            }

            var lamp = lens.gameObject.AddComponent<Light>();
            lamp.range = 6.5f;
            lamp.shadows = LightShadows.None;
            lamp.intensity = 1.8f;
            var ram = Socket("Emergency ram", new Vector3(0, .64f, 3.51f));
            Box("Ram rail", ram, new Vector3(0, 0, -.65f), new Vector3(.86f, .46f, 1.23f), armor, false);
            Box("Ram head", ram, new Vector3(0, 0, -.17f), new Vector3(1.1f, .68f, .32f), baseMetal, false);

            var sector = platform.parent;
            var interceptor = Anchor("SUPER / ceiling reserve emitter", sector, new Vector3(17.7f, 3.75f, 142.4f));
            Box("Cradle", interceptor, new Vector3(0, .55f, 0), new Vector3(1.24f, .54f, .87f), armor, false);
            Box("Live power strip", interceptor, new Vector3(0, .35f, 0), new Vector3(.72f, .14f, .49f), green, false);

            var barriers = new Transform[4];
            var positions = new[] { new Vector3(4, 0, 139.4f), new Vector3(14.6f, 0, 143.2f),
                new Vector3(5.2f, 0, 164.1f), new Vector3(16.2f, 0, 165.2f) };
            for (int i = 0; i < barriers.Length; i++)
            {
                var root = Anchor("Core floor riser " + (i + 1), sector, positions[i] - Vector3.up * 1.58f);
                Box("Power barrier / solid", root, Vector3.up * .76f, new Vector3(3.0f, 1.52f, .5f), armor, true);
                Box("Barrier status lamp", root, new Vector3(0, 1.43f, -.267f), new Vector3(2.78f, .05f, .04f), amber, false);
                Box("Riser socket", sector, sector.InverseTransformPoint(positions[i] - Vector3.up * .024f), new Vector3(3.55f, .04f, .9f), baseMetal, false);
                barriers[i] = root;
            }

            var gate = Object.FindObjectsByType<FacilityDoor>(FindObjectsSortMode.None).Single(door => door.DoorId == "door.sec10.trust");
            var data = new SerializedObject(gate);
            data.FindProperty("condition").enumValueIndex = (int)DoorAccessCondition.ManualUnlockOnly;
            data.FindProperty("lockedPromptText").stringValue = "Шлюз закрыт · отключите аватар SUPER";
            data.ApplyModifiedPropertiesWithoutUndo();
            encounter.Configure(lens, left, right, interceptor, ram, barriers, amber, lamp, gate);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
