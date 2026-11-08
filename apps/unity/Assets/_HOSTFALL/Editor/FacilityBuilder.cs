using System;
using System.IO;
using System.Linq;
using Hostfall.Audio;
using Hostfall.CameraSystem;
using Hostfall.Core;
using Hostfall.Devices;
using Hostfall.Field;
using Hostfall.Interaction;
using Hostfall.Missions;
using Hostfall.Player;
using Hostfall.Rendering;
using Hostfall.Persistence;
using Hostfall.Simulation;
using Hostfall.UI;
using Hostfall.World;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

namespace Hostfall.Editor
{
    public static class FacilityBuilder
    {
        public const string Content = "Assets/_HOSTFALL/Content";
        public const string ScenePath = Content + "/Scenes/NorthlineFacility.unity";
        private static Material metal, dark, concrete, floor, glass, screen, green, amber, red, white, bone, sage, corePanel, warmLight;
        private static Transform world;
        private static GameObject rackPrefab, deskPrefab;

        [MenuItem("HOSTFALL/Rebuild NORTHLINE Facility")]
        public static void Rebuild()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(Content + "/Scenes"); Directory.CreateDirectory(Content + "/Materials");
            Directory.CreateDirectory(Content + "/Prefabs"); Directory.CreateDirectory(Content + "/Settings");
            ConfigureProject(); ImportSurfaces(); CreateMaterials();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            world = new GameObject("NORTHLINE / Facility 04").transform;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.18f, .21f, .24f);
            RenderSettings.ambientEquatorColor = new Color(.10f, .12f, .14f);
            RenderSettings.ambientGroundColor = new Color(.045f, .055f, .06f);
            RenderSettings.fog = true; RenderSettings.fogColor = new Color(.07f, .10f, .13f);
            RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogDensity = .018f;
            ModularKit.Prepare(); EngineeringDressing.Prepare(); RobotArt.Prepare(); CreatePrefabs();
            Room("SOC / Operations Center", new Vector3(0, 0, -3), 10, 8, false, true);
            Room("SRV / Server Room", new Vector3(0, 0, 11), 10, 8, true, false);
            Corridor();
            var terminals = new ComputerTerminal[3];
            terminals[0] = Workstation(DeviceId.Ws07, new Vector3(-2.65f, 0, -3.9f), 180);
            Workstation(null, new Vector3(2.65f, 0, -3.9f), 180);
            terminals[1] = Workstation(DeviceId.Edge01, new Vector3(.55f, 0, 4), 90);
            terminals[2] = Workstation(DeviceId.Core01, new Vector3(0, 0, 12.8f), 0);
            for (int side = -1; side <= 1; side += 2)
                for (int row = 0; row < 3; row++)
                {
                    var rack = (GameObject)PrefabUtility.InstantiatePrefab(rackPrefab);
                    rack.transform.SetParent(world); rack.transform.position = new Vector3(side * 3.1f, 0, 8.7f + row * 2.15f);
                    rack.transform.rotation = Quaternion.Euler(0, side < 0 ? 90 : -90, 0);
                }
            DetailSoc();
            var door = Door(); var power = PowerPanel();
            var watcher=Watcher("watcher-r17","Enemy_QuadShell",new Vector3(-.4f,0,4.9f));
            var sentry=Watcher("sentry-r18","Enemy_Trilobite",new Vector3(.7f,0,10.2f));
            var notes = new[] {
                Note("shift-note", new Vector3(-3.26f, .85f, -3.72f), new Vector3(.34f, .025f, .42f)),
                Note("maintenance-note", new Vector3(-1.12f, 1.45f, 3.35f), new Vector3(.025f, .4f, .3f)),
                Note("core-note", new Vector3(.65f, .85f, 12.67f), new Vector3(.34f, .025f, .42f))
            };
            Sign("NORTHLINE", new Vector3(0, 2.32f, -6.77f), 180, .18f, new Color(.7f, .77f, .8f));
            Sign("ОПЕРАТОРСКАЯ  /  04", new Vector3(0, 1.95f, -6.76f), 180, .075f, new Color(.6f, .67f, .7f));
            Sign("СЕРВЕРНАЯ", new Vector3(0, 2.92f, 6.78f), 0, .10f, new Color(.72f, .8f, .82f));
            Sign("СЕТЕВОЙ ШЛЮЗ", new Vector3(-1.09f, 2.45f, 4), -90, .06f, new Color(.75f, .68f, .48f));
            var roots = new GameObject("Systems");
            roots.AddComponent<FacilityAmbience>();
            var save = roots.AddComponent<WorldSaveController>();
            var controls = roots.AddComponent<ControlState>(); var input = roots.AddComponent<FacilityInput>();
            var playerObject = new GameObject("Operator / Player"); playerObject.layer = CollisionLayers.Player;
            playerObject.transform.position = new Vector3(-2.65f, .08f, -1.7f); playerObject.transform.rotation = Quaternion.Euler(0, 180, 0);
            var character = playerObject.AddComponent<CharacterController>(); character.height = 1.8f; character.radius = .3f;
            character.center = new Vector3(0, .9f, 0); character.skinWidth = .035f; character.stepOffset = .25f; character.slopeLimit = 48;
            var player = playerObject.AddComponent<PlayerMotor>();
            var visual = PlayerBody(playerObject.transform);
            var scanner = playerObject.AddComponent<InteractionScanner>();
            var main = new GameObject("Main Camera"); main.tag = "MainCamera";
            var camera = main.AddComponent<UnityEngine.Camera>(); camera.nearClipPlane = .06f; camera.farClipPlane = 80;
            camera.backgroundColor = RenderSettings.fogColor; camera.clearFlags = CameraClearFlags.SolidColor;
            camera.allowHDR = true; main.AddComponent<AudioListener>();
            var cameraData = main.AddComponent<UniversalAdditionalCameraData>(); cameraData.renderPostProcessing = true;
            cameraData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
            var brain = main.AddComponent<CinemachineBrain>();
            var fps = VirtualCamera("FPS / Operator Eyes", 72); var tps = VirtualCamera("TPS / Follow", 65);
            var rig = roots.AddComponent<PlayerCameraRig>();
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(Content + "/Settings/FacilityPanel.asset");
            if (panel == null) { panel = ScriptableObject.CreateInstance<PanelSettings>(); AssetDatabase.CreateAsset(panel, Content + "/Settings/FacilityPanel.asset"); }
            panel.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>("Assets/_HOSTFALL/UI/Facility.tss");
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize; panel.referenceResolution = new Vector2Int(1600, 900); panel.match = .5f;
            EditorUtility.SetDirty(panel);
            var uiObject = new GameObject("UI / NORTHLINE"); var document = uiObject.AddComponent<UIDocument>(); document.panelSettings = panel;
            document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/_HOSTFALL/UI/Facility.uxml");
            var ui = uiObject.AddComponent<FacilityUI>();
            var fieldUI = uiObject.AddComponent<FieldSystemUI>();
            var mission = AssetDatabase.LoadAssetAtPath<IncidentMissionAsset>(Content + "/Settings/NightShift.asset");
            if (mission == null) { mission = ScriptableObject.CreateInstance<IncidentMissionAsset>(); AssetDatabase.CreateAsset(mission, Content + "/Settings/NightShift.asset"); }
            roots.AddComponent<FacilityRoot>().Configure(controls, input, player, rig, fps, tps, brain, visual, scanner, ui, terminals, door, power, mission, fieldUI, watcher, notes, save,new[] { sentry });
            PostProcessing();
            ActionRouteUpgrade.AddPlatforms(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("HOSTFALL: authored facility saved: " + ScenePath);
        }
        private static void ConfigureProject()
        {
            var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            tags.FindProperty("layers").GetArrayElementAtIndex(CollisionLayers.Player).stringValue = "Player";
            tags.ApplyModifiedPropertiesWithoutUndo();
            PlayerSettings.companyName = "NORTHLINE"; PlayerSettings.productName = "HOSTFALL";
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone, "com.northline.hostfall");
            PlayerSettings.colorSpace = ColorSpace.Linear; PlayerSettings.runInBackground = false;
            PlayerSettings.defaultScreenWidth = 1600; PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            settings.FindProperty("activeInputHandler").intValue = 1; settings.ApplyModifiedPropertiesWithoutUndo();
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.WebGL, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.WebGL, new[] { GraphicsDeviceType.WebGPU, GraphicsDeviceType.OpenGLES3 });
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(Content + "/Settings/FacilityRenderer.asset");
            if (renderer == null) { renderer = ScriptableObject.CreateInstance<UniversalRendererData>(); AssetDatabase.CreateAsset(renderer, Content + "/Settings/FacilityRenderer.asset"); }
            renderer.renderingMode = RenderingMode.Forward;
            // Old template debug resources can retain GUIDs removed in newer URP releases.
            var rendererSettings = new SerializedObject(renderer);
            var property = rendererSettings.GetIterator();
            while (property.Next(true))
                if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null)
                    property.objectReferenceValue = null;
            rendererSettings.ApplyModifiedPropertiesWithoutUndo();
            var defaults = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(defaults, Content + "/Settings/DefaultVolumeProfile.asset");
            var defaultTone = defaults.Add<Tonemapping>(); defaultTone.mode.Override(TonemappingMode.ACES);
            AssetDatabase.AddObjectToAsset(defaultTone, defaults);
            EditorUtility.SetDirty(defaults);
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Content + "/Settings/FacilityURP.asset");
            if (pipeline == null) { pipeline = UniversalRenderPipelineAsset.Create(renderer); AssetDatabase.CreateAsset(pipeline, Content + "/Settings/FacilityURP.asset"); }
            pipeline.msaaSampleCount = 2; pipeline.shadowDistance = 24; pipeline.renderScale = 1;
            pipeline.supportsHDR = true;
            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.SetQualityLevel(Mathf.Min(2, QualitySettings.names.Length - 1), false);
            QualitySettings.renderPipeline = pipeline; QualitySettings.vSyncCount = 1;
            EditorUtility.SetDirty(renderer); EditorUtility.SetDirty(pipeline);
        }
        private static Material Material(string name, Color color, float metallic, float smoothness, float emission = 0)
        {
            string path = Content + "/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
            color.a=1;
            material.SetFloat("_Surface",0); material.SetFloat("_ZWrite",1); material.SetFloat("_SrcBlend",1); material.SetFloat("_DstBlend",0); material.SetFloat("_AlphaClip",0);
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.SetOverrideTag("RenderType","Opaque"); material.renderQueue=(int)RenderQueue.Geometry;
            material.SetColor("_BaseColor", color); material.SetFloat("_Metallic", metallic); material.SetFloat("_Smoothness", smoothness);
            if (emission > 0) { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", color * emission); }
            else material.DisableKeyword("_EMISSION");
            material.globalIlluminationFlags = emission > 0 ? MaterialGlobalIlluminationFlags.BakedEmissive : MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            material.enableInstancing = true; EditorUtility.SetDirty(material); return material;
        }
        private static void CreateMaterials()
        {
            metal = Material("Industrial Metal", new Color(.4f, .44f, .42f), .15f, .3f);
            dark = Material("Dark Metal", new Color(.055f, .075f, .09f), .5f, .35f);
            concrete = Material("Concrete", new Color(.44f, .45f, .41f), 0, .18f);
            floor = Material("Floor Panels", new Color(.25f, .28f, .27f), .05f, .3f);
            glass = Material("Glass", new Color(.05f, .11f, .14f), .8f, .9f);
            screen = Material("Screen", new Color(.012f, .03f, .035f), .05f, .25f, .3f);
            green = Material("Indicator Green", new Color(.25f, .7f, .49f), .1f, .4f, 2);
            amber = Material("Indicator Amber", new Color(.85f, .48f, .17f), .1f, .4f, 2);
            red = Material("Emergency Red", new Color(.7f, .09f, .055f), .1f, .4f, 1.5f);
            FacilityArt.Surface(metal, "paint", 1); FacilityArt.Surface(dark, "paint", 1);
            FacilityArt.Surface(concrete, "concrete", 2); FacilityArt.Surface(floor, "floor", 1);
            white = Material("Cold White", new Color(.65f, .79f, .85f), .1f, .35f, 2.2f);
            bone = Material("SOC wall enamel", new Color(.62f,.6f,.51f),0,.25f);
            sage = Material("Service sage paint", new Color(.22f,.33f,.29f),0,.25f);
            corePanel = Material("CORE painted frame",new Color(.46f,.52f,.52f),.05f,.32f);
            warmLight = Material("Warm task diffuser",new Color(.95f,.67f,.35f),0,.2f,1.8f);
            FacilityArt.Surface(bone,"paint",1); FacilityArt.Surface(sage,"paint",1); FacilityArt.Surface(corePanel,"paint",1);
        }
        private static GameObject Box(string name, Vector3 position, Vector3 size, Material material, Transform parent = null, bool collision = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name;
            go.transform.SetParent(parent != null ? parent : world, false); go.transform.localPosition = position; go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (Mathf.Min(size.x, size.y, size.z) > .035f) go.GetComponent<MeshFilter>().sharedMesh = FacilityArt.BeveledBox();
            if (!collision) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }
        private static void Room(string name, Vector3 center, int width, int depth, bool southOpening, bool northOpening)
        {
            bool soc=name.StartsWith("SOC");
            var group = new GameObject(name).transform; group.SetParent(world); group.position = center;
            Box("Foundation", new Vector3(0, -.2f, 0), new Vector3(width + .4f, .35f, depth + .4f), dark, group);
            for (int x = 0; x < width / 2; x++) for (int z = 0; z < depth / 2; z++)
                Box("Raised floor tile", new Vector3(-width / 2f + 1 + x * 2, -.035f, -depth / 2f + 1 + z * 2), new Vector3(1.975f, .07f, 1.975f), floor, group);
            Box("Acoustic ceiling", new Vector3(0, 3.35f, 0), new Vector3(width + .4f, .18f, depth + .4f), dark, group);
            for (int side = -1; side <= 1; side += 2)
            {
                Box("Concrete shell", new Vector3(side * (width / 2f + .1f), 1.65f, 0), new Vector3(.2f, 3.3f, depth), concrete, group);
                for (int z = 0; z < depth / 2; z++)
                {
                    Box("Wall liner", new Vector3(side * (width / 2f - .025f), 1.4f, -depth / 2f + 1 + z * 2), new Vector3(.08f, 2.7f, 1.95f), metal, group);
                    Box("Wall skirting", new Vector3(side * (width / 2f - .08f), .18f, -depth / 2f + 1 + z * 2), new Vector3(.12f, .24f, 2), dark, group);
                }
            }
            ModularKit.Room(group,width,depth);
            EndWall(group, width, -depth / 2f, southOpening); EndWall(group, width, depth / 2f, northOpening);
            // Quiet back-wall planes give the rooms an identity without covering every bay.
            float back=soc?-depth/2f+.115f:depth/2f-.115f;
            Box("Painted room backdrop",new Vector3(0,1.9f,back),new Vector3(width-.4f,2.65f,.025f),soc?bone:corePanel,group,false);
            Box("Service lower wall",new Vector3(0,.48f,back+(soc?.02f:-.02f)),new Vector3(width-.4f,.9f,.025f),sage,group,false);
            for (int z = -2; z <= 2; z += 4)
            {
                var fixture=ModularKit.Place("Prop_Light_Small",group,new Vector3(0,3.08f,z),new Vector3(3.7f,.12f,.35f));
                bool lit=!soc||z<0;
                if(!lit) foreach(var renderer in fixture.GetComponentsInChildren<Renderer>()) renderer.sharedMaterial=metal;
                LightAt(soc?"SOC local reflected fill":"CORE local reflected fill",center+new Vector3(0,2.7f,z),soc?new Color(.93f,.82f,.65f):new Color(.7f,.84f,.95f),lit?.85f:.16f,4.2f,false);
                if(lit)
                {
                    var task=new GameObject(soc?"SOC overhead pool":"CORE overhead pool").AddComponent<Light>(); task.transform.SetParent(group,false);
                    task.transform.localPosition=new Vector3(0,3.02f,z); task.transform.localRotation=Quaternion.Euler(90,0,0);
                    task.type=LightType.Spot; task.color=soc?new Color(1,.87f,.7f):new Color(.76f,.87f,1);
                    task.intensity=soc?5.2f:4.3f; task.range=7; task.spotAngle=100; task.innerSpotAngle=64; task.shadows=LightShadows.Soft;
                }
                Box("Overhead cable tray", new Vector3(-3.7f, 3.05f, 0), new Vector3(.4f, .1f, depth), dark, group);
            }
            for (int x = -4; x <= 4; x += 2)
                Box("Ceiling rib", new Vector3(x, 3.2f, 0), new Vector3(.08f, .22f, depth), metal, group);
        }
        private static void EndWall(Transform group, float width, float z, bool opening)
        {
            if (!opening) { Box("End wall", new Vector3(0, 1.65f, z), new Vector3(width, 3.3f, .2f), concrete, group); return; }
            float side = (width - 2.5f) / 2;
            for (int sign = -1; sign <= 1; sign += 2)
                Box("Portal wall", new Vector3(sign * (1.25f + side / 2), 1.65f, z), new Vector3(side, 3.3f, .2f), concrete, group);
            Box("Portal transom", new Vector3(0, 2.98f, z), new Vector3(2.5f, .65f, .2f), metal, group);
        }
        private static void Corridor()
        {
            Box("Corridor floor", new Vector3(0, -.08f, 4), new Vector3(2.5f, .16f, 6), floor);
            Box("Corridor ceiling", new Vector3(0, 3.1f, 4), new Vector3(2.7f, .2f, 6), dark);
            for (int side = -1; side <= 1; side += 2)
            {
                Box("Corridor wall", new Vector3(side * 1.35f, 1.5f, 4), new Vector3(.2f, 3, 6), dark);
                for(int bay=0;bay<3;bay++) ModularKit.Place("WallBand_Straight",world,new Vector3(side*1.36f,0,2+bay*2),new Vector3(.2f,3,2),side<0?0:180,true);
                Box("Wayfinding strip", new Vector3(side * 1.235f, .18f, 4), new Vector3(.025f, .04f, 5.8f), amber, null, false);
                for (int z = 2; z <= 6; z += 2)
                    Box("Structural rib", new Vector3(side * 1.2f, 1.5f, z), new Vector3(.12f, 3, .10f), dark);
            }
            for (int z = 2; z <= 6; z += 2)
            {
                Box("Corridor luminaire", new Vector3(0, 2.96f, z), new Vector3(.7f, .04f, .18f), z==4?metal:warmLight, null, false);
                if(z!=4) LightAt("Corridor pool",new Vector3(0,2.8f,z),new Color(1,.7f,.4f),.85f,2.65f,false);
            }
        }
        private static void CreatePrefabs()
        {
            var rack = new GameObject("ServerRack").transform;
            Box("Cabinet", new Vector3(0, 1.15f, 0), new Vector3(.85f, 2.3f, 1), dark, rack);
            Box("Front glass", new Vector3(0, 1.18f, -.515f), new Vector3(.72f, 2.05f, .035f), glass, rack, false);
            for (int i = 0; i < 9; i++)
            {
                float y = .28f + i * .22f;
                Box("Rack unit", new Vector3(0, y, -.55f), new Vector3(.7f, .18f, .065f), metal, rack, false);
                Box("Vent", new Vector3(-.08f, y, -.59f), new Vector3(.37f, .055f, .012f), dark, rack, false);
                Box("Status", new Vector3(.25f, y, -.595f), new Vector3(.035f, .025f, .015f), i == 7 ? amber : green, rack, false);
            }
            for (int side = -1; side <= 1; side += 2)
                Box("Rack upright", new Vector3(side * .395f, 1.15f, -.55f), new Vector3(.055f, 2.3f, .08f), corePanel, rack);
            rackPrefab = PrefabUtility.SaveAsPrefabAsset(rack.gameObject, Content + "/Prefabs/ServerRack.prefab"); UnityEngine.Object.DestroyImmediate(rack.gameObject);
            var desk = new GameObject("OperationsDesk").transform;
            Box("Desktop", new Vector3(0, .78f, 0), new Vector3(1.65f, .1f, .8f), metal, desk);
            for (int side = -1; side <= 1; side += 2)
                Box("Leg", new Vector3(side * .65f, .37f, 0), new Vector3(.12f, .74f, .65f), dark, desk);
            Box("Cable channel", new Vector3(0, .55f, .2f), new Vector3(1.3f, .16f, .15f), dark, desk);
            Box("Keyboard", new Vector3(0, .86f, -.15f), new Vector3(.48f, .025f, .17f), dark, desk);
            for (int row = 0; row < 4; row++) for (int col = 0; col < 13; col++)
                Box("Keycap", new Vector3(-.205f + col * .033f, .88f, -.207f + row * .037f), new Vector3(.027f, .009f, .028f), metal, desk, false);
            Box("Mouse pad", new Vector3(.5f, .837f, -.14f), new Vector3(.26f, .008f, .23f), dark, desk, false);
            Shape("Mouse", PrimitiveType.Capsule, new Vector3(.5f, .862f, -.14f), new Vector3(.055f, .025f, .095f), metal, desk);
            Box("Monitor base", new Vector3(0, .845f, .18f), new Vector3(.34f, .025f, .24f), dark, desk, false);
            deskPrefab = PrefabUtility.SaveAsPrefabAsset(desk.gameObject, Content + "/Prefabs/OperationsDesk.prefab"); UnityEngine.Object.DestroyImmediate(desk.gameObject);
        }
        private static ComputerTerminal Workstation(DeviceId? id, Vector3 position, float yaw)
        {
            var group = new GameObject(id.HasValue ? "Terminal / " + id.Value.Hostname() : "Unassigned console").transform;
            group.SetParent(world); group.position = position; group.rotation = Quaternion.Euler(0, yaw, 0);
            var desk = (GameObject)PrefabUtility.InstantiatePrefab(deskPrefab); desk.transform.SetParent(group, false);
            Box("Monitor stand", new Vector3(0, 1.04f, .18f), new Vector3(.08f, .38f, .1f), metal, group);
            Box("Monitor case", new Vector3(0, 1.35f, .18f), new Vector3(.97f, .56f, .09f), dark, group);
            var display = Box("Display", new Vector3(0, 1.35f, .125f), new Vector3(.87f, .46f, .018f), screen, group, false);
            Box("Tower", new Vector3(.5f, .32f, .1f), new Vector3(.24f, .6f, .5f), dark, group);
            var lines = new GameObject("Live readouts").transform; lines.SetParent(group, false);
            Box("Power LED", new Vector3(.5f, .53f, -.16f), new Vector3(.04f, .02f, .02f), green, lines, false);
            for (int vent = 0; vent < 10; vent++)
                Box("Tower ventilation", new Vector3(.5f, .13f + vent * .022f, -.157f), new Vector3(.16f, .006f, .006f), metal, group, false);
            Box("Display bezel / lower", new Vector3(0, 1.1f, .12f), new Vector3(.93f, .025f, .012f), metal, group, false);
            // EDGE/CORE are standing service posts; a chair would block the patrol aisle.
            if(!id.HasValue || id.Value==DeviceId.Ws07) Chair(group);
            if (!id.HasValue) { ScreenText(group, "NORTHLINE / WS-08\n\nСеанс завершён.\nСтанция заблокирована.", new Vector3(-.4f,1.51f,.109f), .003f, new Color(.43f,.58f,.59f)); return null; }
            var title = ScreenText(group, "", new Vector3(-.405f, 1.553f, .109f), .0033f, new Color(.57f,.8f,.77f));
            var bodyText = ScreenText(group, "", new Vector3(-.405f, 1.49f, .109f), .00265f, new Color(.68f,.78f,.76f));
            var status = ScreenText(group, "", new Vector3(-.405f, 1.153f, .109f), .0025f, new Color(.43f,.58f,.59f));
            group.gameObject.AddComponent<ComputerScreen>().Configure(title, bodyText, status);
            var glow = new GameObject("Display spill").AddComponent<Light>(); glow.transform.SetParent(group,false);
            glow.transform.localPosition = new Vector3(0,1.45f,-.05f); glow.type = LightType.Point; glow.range = 1.5f; glow.intensity = .15f; glow.color = new Color(.4f,.75f,.7f);
            var terminal = group.gameObject.AddComponent<ComputerTerminal>(); terminal.Configure(id.Value, display.GetComponent<Renderer>(), screen, dark, lines.gameObject);
            var tag = Sign(id.Value.Hostname().ToUpperInvariant(), Vector3.zero, 0, .055f, new Color(.8f, .87f, .89f));
            tag.transform.SetParent(group, false); tag.transform.localPosition = new Vector3(0, 1.68f, .16f); tag.transform.localRotation = Quaternion.identity;
            return terminal;
        }
        private static void DetailSoc()
        {
            EngineeringDressing.Place("Shift notebook",world,new Vector3(2.15f,.842f,-3.63f),165);
            EngineeringDressing.Place("Maintenance cart",world,new Vector3(4.15f,0,-2.05f),90);
            EngineeringDressing.Place("Cooling valve",world,new Vector3(4.55f,1.05f,10.6f),90);
            TaskLamp(new Vector3(-3.27f,.84f,-4.07f));
            ModularKit.Place("Prop_Cable_1",world,new Vector3(4.35f,.012f,-4.9f),new Vector3(.7f,.07f,1.2f));
            ModularKit.Place("Prop_Crate3",world,new Vector3(4.15f,0,-5.75f),new Vector3(.65f,.65f,.65f),0,true);
            ModularKit.Place("Prop_Crate4",world,new Vector3(4.25f,0,13.85f),new Vector3(.75f,.75f,.75f),20,true);
            ModularKit.Place("Prop_Cable_3",world,new Vector3(-4.55f,.012f,11),new Vector3(.35f,.05f,3.8f));
            for (int side = -1; side <= 1; side += 2)
            {
                Box("Wall display case", new Vector3(side * 2.1f, 2.1f, -6.83f), new Vector3(2.7f, 1.2f, .15f), dark);
                Box("Wall telemetry", new Vector3(side * 2.1f, 2.1f, -6.73f), new Vector3(2.5f, 1, .025f), screen, null, false);
                var panel = new GameObject("Facility information panel").transform; panel.SetParent(world); panel.position = new Vector3(side*2.1f,2.1f,-6.69f); panel.rotation = Quaternion.Euler(0,180,0);
                ScreenText(panel, side < 0 ? "NORTHLINE / УЧАСТОК 04\n\nSOC       ОПЕРАТОРСКАЯ\nEDGE      СЛУЖЕБНЫЙ ПРОХОД\nCORE      СЕРВЕРНАЯ\n\nВХОД ПО ПРОПУСКАМ" : "ДЕЖУРНАЯ СМЕНА\n\n03:17     ПЕРЕДАЧА ПОСТА\n\nОтветственный: И. Ветрова\n\nПри сбое: журнал на WS-07", new Vector3(-1.08f,.43f,0), .0065f, new Color(.47f,.65f,.66f));
                Box("Equipment cabinet", new Vector3(side * 4.2f, .9f, -.1f), new Vector3(.9f, 1.8f, .7f), dark);
                Box("Cabinet inset", new Vector3(side * 4.2f, 1, -.47f), new Vector3(.75f, 1.4f, .04f), metal);
                Box("Floor guide", new Vector3(side * 1.18f, .012f, -1.2f), new Vector3(.035f, .012f, 3.9f), amber, null, false);
            }
        }
        private static void TaskLamp(Vector3 position)
        {
            var lamp=new GameObject("WS-07 / task lamp").transform; lamp.SetParent(world); lamp.position=position;
            Box("Lamp base",Vector3.zero,new Vector3(.16f,.022f,.16f),sage,lamp,false);
            Box("Lamp stem",new Vector3(0,.16f,0),new Vector3(.025f,.32f,.025f),dark,lamp,false);
            Box("Lamp shade",new Vector3(.06f,.32f,0),new Vector3(.23f,.06f,.15f),sage,lamp,false);
            Box("Task diffuser",new Vector3(.06f,.285f,0),new Vector3(.18f,.015f,.11f),warmLight,lamp,false);
            var light=new GameObject("Warm desk pool").AddComponent<Light>(); light.transform.SetParent(lamp,false);
            light.transform.localPosition=new Vector3(.06f,.27f,0); light.transform.localRotation=Quaternion.Euler(90,0,0);
            light.type=LightType.Spot; light.color=new Color(1,.66f,.32f); light.intensity=1.7f; light.range=1.3f; light.spotAngle=100;
        }
        private static FacilityDoor Door()
        {
            var group = new GameObject("Server airlock").transform; group.SetParent(world); group.position = new Vector3(0, 0, 7);
            for (int side = -1; side <= 1; side += 2)
                Box("Portal jamb", new Vector3(side * 1.23f, 1.3f, 0), new Vector3(.12f, 2.6f, .36f), dark, group);
            var leaf = new GameObject("Sliding leaf").transform; leaf.SetParent(group, false);
            Box("Airlock panel", new Vector3(0, 1.27f, 0), new Vector3(2.3f, 2.54f, .16f), metal, leaf);
            Box("Inspection glass", new Vector3(0, 1.7f, -.09f), new Vector3(1.4f, .48f, .025f), glass, leaf, false);
            Box("Warning band", new Vector3(0, .6f, -.09f), new Vector3(2.1f, .055f, .025f), amber, leaf, false);
            Box("Door control / both sides", new Vector3(-1.06f, 1.2f, 0), new Vector3(.18f, .35f, .45f), amber, group);
            var door = group.gameObject.AddComponent<FacilityDoor>(); door.Configure(leaf);
            // Whole hierarchy is dynamic: the leaf retains its own solid collider.
            return door;
        }
        private static PowerSwitch PowerPanel()
        {
            var group = new GameObject("EDGE-01 / power disconnect").transform; group.SetParent(world); group.position = new Vector3(-1.1f, 1.2f, 4);
            Box("Electrical enclosure", Vector3.zero, new Vector3(.2f, .52f, .4f), dark, group);
            var indicator = Box("Power switch", new Vector3(.12f, 0, 0), new Vector3(.08f, .2f, .18f), green, group);
            var power = group.gameObject.AddComponent<PowerSwitch>(); power.Configure(indicator.GetComponent<Renderer>(), green, red); return power;
        }
        private static FieldDocument Note(string id, Vector3 position, Vector3 size)
        {
            var paper = Material("Document paper", new Color(.66f,.63f,.51f), 0,.1f);
            if (size.y < .05f) size.y = .003f; else size.x = .003f;
            var note = Box("Document / " + id, position, size, paper);
            string title = id == "shift-note" ? "ЛИСТ ОБХОДА / 04" : id == "maintenance-note" ? "ЗАЯВКА / R17" : "ПОСТ СВЕРКИ / CORE";
            var print = ScreenText(note.transform, title + "\n\n03:17    И. ВЕТРОВА\n\nNORTHLINE / ВНУТРЕННИЙ",Vector3.zero,.003f,new Color(.12f,.14f,.14f));
            // TextMesh belongs to a non-scaled child so glyph proportions stay intact.
            print.transform.SetParent(world,true); print.transform.position=position+(size.y < .05f ? Vector3.up*.0035f : Vector3.right*.0035f);
            print.transform.rotation=size.y < .05f ? Quaternion.Euler(90,id=="shift-note"?180:0,0) : Quaternion.Euler(0,-90,0);
            print.transform.localScale=Vector3.one; print.anchor=TextAnchor.MiddleCenter; print.fontSize=32;
            var document = note.AddComponent<FieldDocument>(); document.Configure(id); return document;
        }
        private static WatcherController Watcher(string id,string model,Vector3 position)
        {
            var root=new GameObject(id=="watcher-r17"?"WATCHER / R17":"SENTRY / R18"); root.transform.SetParent(world); root.transform.position=position; root.transform.rotation=Quaternion.Euler(0,180,0);
            var shape = root.AddComponent<CapsuleCollider>(); shape.center = new Vector3(0, .52f, 0); shape.height = 1; shape.radius = .43f;
            var body = root.AddComponent<Rigidbody>(); body.isKinematic = true; body.useGravity = false;
            var animator=RobotArt.Place(model,root.transform);
            animator.Rebind(); animator.Play("Idle",0,0); animator.Update(0);
            var anchor=animator.GetComponentsInChildren<Transform>().Single(t=>t.name=="Body");
            var shell=animator.GetComponentsInChildren<SkinnedMeshRenderer>().First(r=>r.name.EndsWith("_Body"));
            // Imported FBX bones carry a centimetre conversion. Compensate renderer scale
            // before TransformPoint, otherwise the scale would be applied twice.
            var baked=new Mesh(); shell.BakeMesh(baked,true);
            var shellBounds=new Bounds(shell.transform.TransformPoint(baked.vertices[0]),Vector3.zero);
            foreach(var vertex in baked.vertices) shellBounds.Encapsulate(shell.transform.TransformPoint(vertex));
            UnityEngine.Object.DestroyImmediate(baked);
            Debug.Log("HOSTFALL optical anchor "+id+": skin="+shell.bounds+" baked="+shellBounds+" bone="+anchor.position+" scale="+shell.transform.lossyScale);
            var sensor = new GameObject("Optical sensor").transform;
            sensor.SetParent(anchor,true); sensor.position=new Vector3(shellBounds.center.x,shellBounds.max.y+.025f,shellBounds.center.z); sensor.rotation=root.transform.rotation;
            Box("Sensor mounting plate",new Vector3(0,-.055f,0),new Vector3(.22f,.06f,.17f),metal,sensor,false);
            Box("Sensor housing",Vector3.zero,new Vector3(.18f,.1f,.12f),dark,sensor,false);
            Box("Optical lens",new Vector3(0,0,.07f),new Vector3(.1f,.035f,.015f),amber,sensor,false);
            var lamp = new GameObject("Sensor indicator").AddComponent<Light>(); lamp.transform.SetParent(sensor, false); lamp.transform.localPosition = new Vector3(0, 0, .12f);
            lamp.type = LightType.Spot; lamp.range = 5; lamp.spotAngle = 65; lamp.intensity = 2; lamp.shadows = LightShadows.None;
            var watcher=root.AddComponent<WatcherController>();
            watcher.Configure(sensor,lamp,animator,id,id=="sentry-r18"?new Vector3(.7f,0,9.1f):(Vector3?)null,id=="sentry-r18"?new Vector3(.7f,0,11.8f):(Vector3?)null);
            return watcher;
        }
        private static GameObject PlayerBody(Transform parent)
        {
            var body = new GameObject("Operator / field suit"); body.transform.SetParent(parent, false);
            var fill = new GameObject("Operator reflected light").AddComponent<Light>(); fill.transform.SetParent(parent,false);
            fill.transform.localPosition = new Vector3(0,1.8f,.35f); fill.type=LightType.Point; fill.range=1.5f; fill.intensity=.55f;
            fill.color=new Color(.85f,.8f,.7f); fill.cullingMask=1<<CollisionLayers.Player;
            var suit = Material("Operator canvas", new Color(.16f,.22f,.23f), .08f,.25f);
            var trim = Material("Operator webbing", new Color(.055f,.065f,.06f), .05f,.2f);
            var gloves = Material("Glove leather", new Color(.24f,.18f,.12f), .05f,.28f);
            FacilityArt.Surface(suit,"paint",2); FacilityArt.Surface(gloves,"paint",1);
            Shape("Jacket", PrimitiveType.Capsule, new Vector3(0,1.12f,-.065f),new Vector3(.43f,.36f,.28f),suit,body.transform);
            Box("Chest rig",new Vector3(0,1.21f,.09f),new Vector3(.34f,.32f,.07f),trim,body.transform,false);
            Box("Identity patch",new Vector3(-.08f,1.29f,.134f),new Vector3(.12f,.055f,.008f),metal,body.transform,false);
            Box("Belt",new Vector3(0,.89f,-.025f),new Vector3(.39f,.075f,.29f),trim,body.transform,false);
            for(int i=-1;i<=1;i+=2) Box("Tool pouch",new Vector3(i*.21f,.88f,-.02f),new Vector3(.1f,.16f,.15f),suit,body.transform,false);
            var helmet = new GameObject("Head / first person shadow").transform; helmet.SetParent(body.transform,false);
            Shape("Helmet shell",PrimitiveType.Sphere,new Vector3(0,1.66f,-.015f),new Vector3(.29f,.33f,.29f),metal,helmet);
            Shape("Visor",PrimitiveType.Sphere,new Vector3(0,1.66f,.08f),new Vector3(.245f,.15f,.16f),glass,helmet);
            Shape("Neck seal",PrimitiveType.Cylinder,new Vector3(0,1.5f,-.02f),new Vector3(.12f,.075f,.12f),trim,body.transform);
            var arms=new Transform[2]; var legs=new Transform[2];
            for(int side=-1;side<=1;side+=2)
            {
                int i=side<0?0:1;
                arms[i]=new GameObject("Shoulder").transform; arms[i].SetParent(body.transform,false); arms[i].localPosition=new Vector3(side*.27f,1.38f,-.02f);
                Shape("Upper sleeve",PrimitiveType.Capsule,new Vector3(0,-.17f,0),new Vector3(.15f,.19f,.16f),suit,arms[i]);
                Shape("Forearm sleeve",PrimitiveType.Capsule,new Vector3(0,-.42f,.04f),new Vector3(.13f,.15f,.14f),suit,arms[i]);
                Glove(arms[i],new Vector3(0,-.59f,.06f),side,gloves,trim,false);
                legs[i]=new GameObject("Hip").transform; legs[i].SetParent(body.transform,false); legs[i].localPosition=new Vector3(side*.115f,.83f,-.04f);
                Shape("Trouser thigh",PrimitiveType.Capsule,new Vector3(0,-.2f,0),new Vector3(.19f,.23f,.21f),suit,legs[i]);
                Shape("Trouser calf",PrimitiveType.Capsule,new Vector3(0,-.54f,-.015f),new Vector3(.16f,.21f,.18f),suit,legs[i]);
                Box("Knee protection",new Vector3(0,-.39f,.08f),new Vector3(.145f,.15f,.055f),trim,legs[i],false);
                Box("Boot",new Vector3(0,-.725f,.05f),new Vector3(.185f,.17f,.34f),trim,legs[i],false);
                Box("Boot sole",new Vector3(0,-.8f,.05f),new Vector3(.19f,.035f,.35f),dark,legs[i],false);
                for(int lace=0;lace<3;lace++) Box("Boot lace",new Vector3(0,-.65f,.05f+lace*.035f),new Vector3(.1f,.008f,.008f),metal,legs[i],false);
            }
            body.AddComponent<OperatorVisual>().Configure(parent.GetComponent<CharacterController>(),arms,legs,helmet.GetComponentsInChildren<Renderer>());
            foreach(Transform child in body.GetComponentsInChildren<Transform>()) child.gameObject.layer=CollisionLayers.Player;
            return body;
        }
        private static GameObject Shape(string name,PrimitiveType type,Vector3 position,Vector3 scale,Material material,Transform parent)
        {
            var go=GameObject.CreatePrimitive(type); go.name=name; go.transform.SetParent(parent,false); go.transform.localPosition=position; go.transform.localScale=scale;
            go.GetComponent<Renderer>().sharedMaterial=material; UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>()); return go;
        }
        private static void Glove(Transform parent,Vector3 position,int side,Material leather,Material trim,bool first)
        {
            var hand=new GameObject("Gloved hand").transform; hand.SetParent(parent,false); hand.localPosition=position;
            if(!first) hand.localRotation=Quaternion.Euler(90,0,0);
            Box("Palm",Vector3.zero,new Vector3(.105f,.055f,.13f),leather,hand,false);
            Box("Wrist cuff",new Vector3(0,-.012f,-.08f),new Vector3(.12f,.07f,.045f),trim,hand,false);
            for(int digit=0;digit<4;digit++)
            {
                var finger=Shape("Finger",PrimitiveType.Capsule,new Vector3(-.037f+digit*.025f,-.014f,.083f),new Vector3(.022f,.045f,.023f),leather,hand);
                finger.transform.localRotation=Quaternion.Euler(65,0,0);
                Box("Knuckle",new Vector3(-.037f+digit*.025f,.025f,.045f),new Vector3(.021f,.017f,.027f),trim,hand,false);
            }
            var thumb=Shape("Thumb",PrimitiveType.Capsule,new Vector3(-side*.062f,-.01f,.02f),new Vector3(.029f,.048f,.03f),leather,hand);
            thumb.transform.localRotation=Quaternion.Euler(45,0,side*30);
        }
        private static TextMesh ScreenText(Transform parent,string text,Vector3 position,float size,Color color)
        {
            var go=new GameObject("Screen text"); go.transform.SetParent(parent,false); go.transform.localPosition=position;
            var label=go.AddComponent<TextMesh>(); label.text=text; label.fontSize=64; label.characterSize=size; label.anchor=TextAnchor.UpperLeft; label.lineSpacing=1.18f; label.color=color;
            label.font=AssetDatabase.LoadAssetAtPath<Font>(Content+"/Fonts/RobotoMono-Regular.ttf"); go.GetComponent<MeshRenderer>().sharedMaterial=label.font.material;
            go.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off; go.GetComponent<MeshRenderer>().receiveShadows=false;
            go.AddComponent<WorldTextDepth>().Configure(Shader.Find("HOSTFALL/World Text")); return label;
        }
        private static void Chair(Transform desk)
        {
            // Offset from the WS-07 approach so the chair does not obstruct interaction.
            var group=new GameObject("Task chair").transform; group.SetParent(desk,false); group.localPosition=new Vector3(.52f,0,-1.03f); group.localRotation=Quaternion.Euler(0,-18,0);
            var solid=group.gameObject.AddComponent<BoxCollider>(); solid.center=new Vector3(0,.54f,-.02f); solid.size=new Vector3(.58f,1.08f,.65f);
            Shape("Seat",PrimitiveType.Cube,new Vector3(0,.47f,0),new Vector3(.46f,.1f,.44f),dark,group);
            Box("Backrest",new Vector3(0,.82f,-.19f),new Vector3(.42f,.48f,.08f),dark,group,false);
            Box("Back support",new Vector3(0,.61f,-.2f),new Vector3(.06f,.42f,.05f),metal,group,false);
            Shape("Gas lift",PrimitiveType.Cylinder,new Vector3(0,.23f,0),new Vector3(.055f,.23f,.055f),metal,group);
            for(int side=-1;side<=1;side+=2)
            {
                Box("Arm rest",new Vector3(side*.25f,.67f,0),new Vector3(.055f,.045f,.29f),dark,group,false);
                Box("Arm support",new Vector3(side*.24f,.56f,-.04f),new Vector3(.025f,.2f,.025f),metal,group,false);
            }
            for(int leg=0;leg<5;leg++)
            {
                var pivot=new GameObject("Chair base spoke").transform; pivot.SetParent(group,false); pivot.localRotation=Quaternion.Euler(0,leg*72,0);
                Box("Spoke",new Vector3(0,.08f,.15f),new Vector3(.045f,.045f,.32f),metal,pivot,false);
                Shape("Castor",PrimitiveType.Sphere,new Vector3(0,.045f,.28f),new Vector3(.07f,.07f,.07f),dark,pivot);
            }
        }
        private static void ImportSurfaces()
        {
            AssetDatabase.Refresh();
            foreach(string file in Directory.GetFiles(Content+"/Textures","*.png"))
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(file);
                importer.textureType=file.Contains("-normal")?TextureImporterType.NormalMap:TextureImporterType.Default;
                importer.wrapMode=TextureWrapMode.Repeat; importer.maxTextureSize=512; importer.SaveAndReimport();
            }
        }
        private static CinemachineCamera VirtualCamera(string name, float fov)
        {
            var camera = new GameObject(name).AddComponent<CinemachineCamera>();
            camera.Lens.FieldOfView = fov; camera.Lens.NearClipPlane = .06f; camera.Lens.FarClipPlane = 80; return camera;
        }
        private static GameObject Sign(string text, Vector3 position, float yaw, float size, Color color)
        {
            var go = new GameObject("Sign / " + text); go.transform.SetParent(world); go.transform.position = position; go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            var label = go.AddComponent<TextMesh>(); label.text = text; label.fontSize = 64; label.characterSize = size * .22f;
            label.anchor = TextAnchor.MiddleCenter; label.alignment = TextAlignment.Center; label.color = color;
            label.font = AssetDatabase.LoadAssetAtPath<Font>(Content + "/Fonts/RobotoMono-Regular.ttf");
            go.GetComponent<MeshRenderer>().sharedMaterial = label.font.material; go.AddComponent<WorldTextDepth>().Configure(Shader.Find("HOSTFALL/World Text")); return go;
        }
        private static void LightAt(string name, Vector3 position, Color color, float intensity, float range, bool shadows)
        {
            var go = new GameObject(name); go.transform.SetParent(world); go.transform.position = position;
            var light = go.AddComponent<Light>(); light.type = LightType.Point; light.color = color; light.intensity = intensity;
            light.range = range; light.shadows = shadows ? LightShadows.Soft : LightShadows.None;
        }
        private static void PostProcessing()
        {
            string path = Content + "/Settings/FacilityVolume.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(profile, path);
                var bloom = profile.Add<Bloom>(); bloom.intensity.Override(.18f); bloom.threshold.Override(1.2f);
                var tone = profile.Add<Tonemapping>(); tone.mode.Override(TonemappingMode.ACES);
                var vignette = profile.Add<Vignette>(); vignette.intensity.Override(.21f); vignette.smoothness.Override(.4f);
                foreach (var component in profile.components) AssetDatabase.AddObjectToAsset(component, profile);
            }
            var go = new GameObject("Atmosphere / Global Volume"); var volume = go.AddComponent<Volume>(); volume.isGlobal = true; volume.sharedProfile = profile;
            EditorUtility.SetDirty(profile);
        }
        public static void BuildDesktop()
        {
            UnityEditor.OSXStandalone.UserBuildSettings.architecture = UnityEditor.Build.OSArchitecture.x64;
            Build(BuildTarget.StandaloneOSX, "Builds/macOS/HOSTFALL.app");
        }
        public static void BuildWeb()
        {
            PlayerSettings.WebGL.template = "PROJECT:HOSTFALL";
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            Build(BuildTarget.WebGL, "Builds/Web");
        }
        private static void Build(BuildTarget target, string path)
        {
            if (!File.Exists(ScenePath)) throw new InvalidOperationException("Author the scene with FacilityBuilder.Rebuild first.");
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone, "com.northline.hostfall");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { ScenePath }, locationPathName = path, target = target, options = BuildOptions.None });
            if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Build failed: " + report.summary.result);
        }
    }
}
