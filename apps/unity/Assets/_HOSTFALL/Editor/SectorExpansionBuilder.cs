using System;
using System.IO;
using System.Linq;
using Hostfall.Core;
using Hostfall.Devices;
using Hostfall.Interaction;
using Hostfall.Rendering;
using Hostfall.Simulation;
using Hostfall.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Hostfall.Editor
{
    public static class SectorExpansionBuilder
    {
        private const string RootName = "NORTHLINE / Expanded Sectors";

        [MenuItem("HOSTFALL/Build Expanded Facility Sectors")]
        public static void Apply()
        {
            var scene = EditorSceneManager.OpenScene(FacilityBuilder.ScenePath, OpenSceneMode.Single);
            ModularKit.Prepare();
            Build(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("HOSTFALL: Mega Facility Sectors built and saved successfully!");
        }

        public static void Build(Scene scene)
        {
            // Remove previous expansion if already built (idempotent)
            foreach (var go in scene.GetRootGameObjects())
            {
                if (go.name == RootName) UnityEngine.Object.DestroyImmediate(go);
            }

            // Adapt server room back wall to create opening for Vault Gate
            AdaptServerRoomBackWall(scene);

            // Adapt SOC west wall to create opening for Executive Shortcut Door
            AdaptSocWestWall(scene);

            var root = new GameObject(RootName);
            SceneManager.MoveGameObjectToScene(root, scene);

            var matMetal = LoadMat("Industrial Metal");
            var matDark = LoadMat("Dark Metal");
            var matConcrete = LoadMat("Concrete");
            var matFloor = LoadMat("Floor Panels");
            var matAmber = LoadMat("Indicator Amber");
            var matGlass = LoadMat("Glass");
            var matScreen = LoadMat("Screen");
            var matSage = LoadMat("Service sage paint");
            var matGreen = LoadMat("Indicator Green");

            // ReflectionProbe for SOC and Server Room
            CreateReflectionProbe("Reflection Probe - SOC & Server Room", new Vector3(0, 2.0f, 6.0f), new Vector3(14.0f, 5.0f, 22.0f), root.transform);

            // =========================================================================
            // 1. VAULT GATE (Z = 15.0) — Between Server Room and Dataset Vault
            // =========================================================================
            var vaultGateGroup = new GameObject("Vault Gate (Z=15)").transform;
            vaultGateGroup.SetParent(root.transform, false);
            vaultGateGroup.position = new Vector3(0, 0, 15f);

            CreateBox("Vault Left Jamb", new Vector3(-1.35f, 1.5f, 0), new Vector3(0.3f, 3.0f, 0.4f), matDark, vaultGateGroup, true);
            CreateBox("Vault Right Jamb", new Vector3(1.35f, 1.5f, 0), new Vector3(0.3f, 3.0f, 0.4f), matDark, vaultGateGroup, true);
            CreateBox("Vault Lintel", new Vector3(0, 3.15f, 0), new Vector3(3.0f, 0.3f, 0.4f), matDark, vaultGateGroup, true);

            var vaultLeaf = new GameObject("Vault Sliding Leaf").transform;
            vaultLeaf.SetParent(vaultGateGroup, false);
            BuildArmoredDoorLeaf(vaultLeaf, 2.6f, 2.95f, 0.22f, 1.8f, 1.25f, 0.42f, matDark, matGlass, matMetal, matAmber, "Vault");

            var vaultDoor = vaultGateGroup.gameObject.AddComponent<FacilityDoor>();
            vaultDoor.Configure(vaultLeaf, DoorAccessCondition.CoreTerminalComplete,
                "Защитные ворота заблокированы · завершите сеанс на CORE-01",
                "[E] Открыть защитные ворота хранилища",
                "[E] Закрыть защитные ворота хранилища",
                true, Vector3.right, 2.6f, 1.8f);

            // Illumination and wayfinding for Vault Gate portal
            CreatePointLight("Vault Threshold Downlight", vaultGateGroup, new Vector3(0, 3.1f, 0.15f), new Color(0.45f, 0.85f, 1.0f), 5.0f, 8.0f);
            CreateSpotLight("Vault Gateway Spotlight", vaultGateGroup, new Vector3(0, 3.15f, -0.3f), new Vector3(65, 0, 0), new Color(0.85f, 0.95f, 1.0f), 6.5f, 14f, 85f);
            CreateSignboard(vaultGateGroup, "СЕКТОР 02: ВЫЧИСЛИТЕЛЬНЫЙ УЗЕЛ", "SEC-02", new Vector3(0, 3.35f, -0.22f), 180, new Color(0.45f, 0.85f, 1.0f));

            // Transom Bulkhead: seal height difference between Server Room (Y=3.35) and Sector 02 (Y=5.4)
            CreateBox("Server-to-Sector02 Bulkhead", new Vector3(0, 4.375f, 15.0f), new Vector3(10.0f, 2.05f, 0.4f), matDark, root.transform, true);
            CreateBox("Vault South Wall Extension Left", new Vector3(-6.0f, 2.7f, 15.0f), new Vector3(2.0f, 5.4f, 0.4f), matConcrete, root.transform, true);
            CreateBox("Vault South Wall Extension Right", new Vector3(6.0f, 2.7f, 15.0f), new Vector3(2.0f, 5.4f, 0.4f), matConcrete, root.transform, true);

            // Prominent Emergency Weapon Racks with NORTHLINE ARC-12 Shotgun
            BuildWeaponRack(root.transform, new Vector3(2.4f, 1.35f, 14.1f), -90, "Server Room Emergency Rack");
            BuildWeaponRack(root.transform, new Vector3(3.8f, 1.35f, -5.8f), -90, "SOC Security Post Rack");

            // =========================================================================
            // 2. SECTOR 02: HIGH-DENSITY COMPUTE VAULT / DATASET HALL (Z = 15..38)
            // =========================================================================
            var sector02 = new GameObject("Sector 02 - Compute Vault").transform;
            sector02.SetParent(root.transform, false);

            // Floor: 14m wide x 23m deep (Z: 15..38, X: -7..+7) with solid Floor Panels material
            CreateBox("Compute Vault Floor Base", new Vector3(0, -0.1f, 26.5f), new Vector3(14.4f, 0.2f, 23.4f), matFloor, sector02, true);
            CreateBox("Central Runner Slab", new Vector3(0, 0.02f, 26.5f), new Vector3(2.4f, 0.04f, 23.0f), matDark, sector02, false);

            // Ceiling (Y = 5.4m)
            CreateBox("Compute Vault Ceiling", new Vector3(0, 5.45f, 26.5f), new Vector3(14.4f, 0.2f, 23.4f), matDark, sector02, true);

            // East Outer Wall (X = +7.0, Z: 15..38 with clear doorway for Armory at Z: 24.5..27.5)
            // Monolithic hermetic backer walls guaranteeing zero exterior voids or out-of-bounds gaps
            CreateBox("Sector02 East Wall South Segment", new Vector3(7.05f, 2.7f, 19.75f), new Vector3(0.5f, 5.4f, 9.5f), matConcrete, sector02, true);
            CreateBox("Sector02 East Wall North Segment", new Vector3(7.05f, 2.7f, 32.75f), new Vector3(0.5f, 5.4f, 10.5f), matConcrete, sector02, true);
            CreateBox("East Wall Armory Header", new Vector3(7.0f, 4.3f, 26.0f), new Vector3(0.5f, 2.2f, 3.2f), matDark, sector02, true);
            // Decorative architectural modular wall panels
            ModularKit.Place("WallAstra_Straight", sector02, new Vector3(6.9f, 0, 17.5f), new Vector3(0.3f, 5.4f, 4.5f), 180, true);
            ModularKit.Place("WallAstra_Straight_Divided", sector02, new Vector3(6.9f, 0, 22.0f), new Vector3(0.3f, 5.4f, 4.5f), 180, true);
            ModularKit.Place("WallAstra_Straight_Divided", sector02, new Vector3(6.9f, 0, 30.0f), new Vector3(0.3f, 5.4f, 4.5f), 180, true);
            ModularKit.Place("WallAstra_Straight", sector02, new Vector3(6.9f, 0, 35.0f), new Vector3(0.3f, 5.4f, 4.5f), 180, true);

            // West Outer Wall (X = -7.0, Z: 15..38 with clear opening for Energy Airlock at Z: 24.5..27.5)
            // Monolithic hermetic backer walls guaranteeing zero gaps into Sector 03 or void
            CreateBox("Sector02 West Wall South Segment", new Vector3(-7.05f, 2.7f, 19.75f), new Vector3(0.5f, 5.4f, 9.5f), matConcrete, sector02, true);
            CreateBox("Sector02 West Wall North Segment", new Vector3(-7.05f, 2.7f, 32.75f), new Vector3(0.5f, 5.4f, 10.5f), matConcrete, sector02, true);
            CreateBox("West Wall Airlock Header", new Vector3(-7.0f, 4.3f, 26.0f), new Vector3(0.5f, 2.2f, 3.2f), matDark, sector02, true);
            // Decorative architectural modular wall panels
            ModularKit.Place("WallAstra_Straight", sector02, new Vector3(-6.9f, 0, 17.5f), new Vector3(0.3f, 5.4f, 4.5f), 0, true);
            ModularKit.Place("WallAstra_Straight_Divided", sector02, new Vector3(-6.9f, 0, 22.0f), new Vector3(0.3f, 5.4f, 4.5f), 0, true);
            ModularKit.Place("WallAstra_Straight_Divided", sector02, new Vector3(-6.9f, 0, 30.0f), new Vector3(0.3f, 5.4f, 4.5f), 0, true);
            ModularKit.Place("WallAstra_Straight", sector02, new Vector3(-6.9f, 0, 35.0f), new Vector3(0.3f, 5.4f, 4.5f), 0, true);

            // North Wall of Sector 02 (Z = 38.0 with opening in center for Cryo Gate to Sector 04)
            // Fully sealed from X = -7.0 to X = +7.0 with zero gaps around Cryo Gate jambs (jambs at X = +/- 1.55)
            CreateBox("Sector02 North Wall Left", new Vector3(-4.275f, 2.7f, 38.0f), new Vector3(5.45f, 5.4f, 0.5f), matConcrete, sector02, true);
            CreateBox("Sector02 North Wall Right", new Vector3(4.275f, 2.7f, 38.0f), new Vector3(5.45f, 5.4f, 0.5f), matConcrete, sector02, true);
            CreateBox("Sector02 North Wall Lintel", new Vector3(0, 4.575f, 38.0f), new Vector3(3.2f, 1.65f, 0.5f), matDark, sector02, true);
            ModularKit.Place("WallAstra_Straight", sector02, new Vector3(-4.3f, 0, 37.9f), new Vector3(5.2f, 5.4f, 0.3f), 90, true);
            ModularKit.Place("WallAstra_Straight", sector02, new Vector3(4.3f, 0, 37.9f), new Vector3(5.2f, 5.4f, 0.3f), 90, true);

            // Structural Columns
            for (int z = 18; z <= 34; z += 7)
            {
                ModularKit.Place("Column_Large_Straight", sector02, new Vector3(-3.2f, 0, z), new Vector3(0.8f, 5.4f, 0.8f), 0, true);
                ModularKit.Place("Column_Large_Straight", sector02, new Vector3(3.2f, 0, z), new Vector3(0.8f, 5.4f, 0.8f), 0, true);
            }

            // Suspended Overhead Cable Trays with Ceiling Support Rods
            for (int z = 18; z <= 34; z += 5)
            {
                ModularKit.Place("TopCables_Straight_Hanging", sector02, new Vector3(0, 4.8f, z), new Vector3(12f, 0.8f, 0.6f), 90, false);
                CreateBox("Cable Hanger Rod Left " + z, new Vector3(-3.2f, 5.1f, z), new Vector3(0.06f, 0.6f, 0.06f), matDark, sector02, false);
                CreateBox("Cable Hanger Rod Right " + z, new Vector3(3.2f, 5.1f, z), new Vector3(0.06f, 0.6f, 0.06f), matDark, sector02, false);
            }

            // High-Density Dataset Rack Clusters (West server bays flanking the main aisle)
            BuildDatasetMonolith(sector02, new Vector3(-4.8f, 0, 19.5f), matDark, matScreen, matAmber);
            BuildDatasetMonolith(sector02, new Vector3(-4.8f, 0, 32.5f), matDark, matScreen, matAmber);

            // Elevated Catwalk Mezzanine (Y = 2.4m) running along the East flank (Z: 20..35, X = 4.6)
            var catwalk = new GameObject("Catwalk Mezzanine (Y=2.4)").transform;
            catwalk.SetParent(sector02, false);

            for (int z = 20; z <= 35; z += 3)
            {
                ModularKit.Place("Platform_Metal", catwalk, new Vector3(4.6f, 2.4f, z), new Vector3(2.2f, 0.15f, 3.0f), 0, true);
                // Aisle safety railing on West side
                ModularKit.Place("Platform_Rails_4", catwalk, new Vector3(3.45f, 2.46f, z), new Vector3(0.08f, 1.1f, 3.0f), 0, true);
                // Wall gap safety railing on East side
                ModularKit.Place("Platform_Rails_4", catwalk, new Vector3(5.75f, 2.46f, z), new Vector3(0.08f, 1.1f, 3.0f), 180, true);
            }
            // North end safety railing
            ModularKit.Place("Platform_Rails_4", catwalk, new Vector3(4.6f, 2.46f, 36.5f), new Vector3(2.2f, 1.1f, 0.08f), 90, true);

            // Structural vertical steel support pillars under catwalk connecting to floor (Y=0 to Y=2.4)
            for (int z = 22; z <= 34; z += 4)
            {
                ModularKit.Place("Column_MetalSupport", catwalk, new Vector3(3.55f, 0, z), new Vector3(0.35f, 2.4f, 0.35f), 0, true);
                ModularKit.Place("Column_MetalSupport", catwalk, new Vector3(5.65f, 0, z), new Vector3(0.35f, 2.4f, 0.35f), 0, true);
            }

            // Modular Stairs leading to upper Catwalk
            // Starts on open ground floor at Z = 16.4 and ascends North up to Z = 20.0 at Y = 2.4m
            // 100% open headroom overhead, connects seamlessly flush with the first catwalk platform at Z = 20.0
            ModularKit.Place("Platform_Stairs_4", catwalk, new Vector3(4.6f, 0, 18.2f), new Vector3(1.8f, 2.4f, 3.6f), 0, true);

            // Focused architectural stair illumination
            CreateSpotLight("Catwalk Stairs Spotlight", sector02, new Vector3(4.6f, 5.0f, 18.2f), new Vector3(68, 0, 0), new Color(1.0f, 0.92f, 0.82f), 5.5f, 14f, 85f);

            // Elevated Observation & Diagnostics Post at the North end of the Catwalk (Z = 35.0)
            CreateBox("Observation Console Base", new Vector3(4.6f, 2.95f, 35.8f), new Vector3(1.4f, 0.9f, 0.5f), matDark, catwalk, true);
            CreateBox("Observation Console Screen", new Vector3(4.6f, 3.55f, 35.95f), new Vector3(1.2f, 0.45f, 0.04f), matScreen, catwalk, false);
            ScreenText(catwalk, "SEC-02 MEZZANINE // OBSERVATION", new Vector3(4.1f, 3.75f, 35.92f), 0.0028f, new Color(0.45f, 0.85f, 1.0f));
            CreatePointLight("Observation Task Light", catwalk, new Vector3(4.6f, 4.2f, 35.5f), new Color(0.45f, 0.85f, 1.0f), 2.5f, 5.0f);

            // Build Dedicated Armory Room in Sector 02 (X: 7.0..14.5, Z: 21.0..31.0)
            var armoryDoor = BuildArmoryRoom(root.transform, matMetal, matDark, matAmber, matGlass, matScreen);

            // Props and Cable Conduits in Sector 02
            ModularKit.Place("Prop_Vent_Wide", sector02, new Vector3(-6.75f, 3.8f, 20f), new Vector3(0.4f, 1.2f, 2.0f), 0, false);
            ModularKit.Place("Prop_Vent_Wide", sector02, new Vector3(6.75f, 3.8f, 20f), new Vector3(0.4f, 1.2f, 2.0f), 180, false);
            ModularKit.Place("Prop_Cable_1", sector02, new Vector3(-2.8f, 0.05f, 26f), new Vector3(1.2f, 0.2f, 1.2f), 0, false);
            ModularKit.Place("Prop_Cable_3", sector02, new Vector3(2.8f, 0.05f, 26f), new Vector3(1.2f, 0.2f, 1.2f), 90, false);
            ModularKit.Place("Prop_Crate3", sector02, new Vector3(-5.8f, 0, 36.0f), new Vector3(0.9f, 0.9f, 0.9f), 10, true);
            ModularKit.Place("Prop_Crate4", sector02, new Vector3(-5.0f, 0, 36.3f), new Vector3(0.85f, 0.85f, 0.85f), -25, true);

            // West Heavy Airlock Door into Sector 03 (Energy Center) at X = -7.0, Z = 26.0
            var energyAirlockGroup = new GameObject("Airlock to Energy Center").transform;
            energyAirlockGroup.SetParent(sector02, false);
            energyAirlockGroup.position = new Vector3(-7.0f, 0, 26.0f);
            energyAirlockGroup.rotation = Quaternion.Euler(0, 90, 0); // Door width along world Z!

            // Frame jambs along local X (world Z)
            CreateBox("Airlock Left Jamb", new Vector3(-1.45f, 1.6f, 0), new Vector3(0.3f, 3.2f, 0.4f), matDark, energyAirlockGroup, true);
            CreateBox("Airlock Right Jamb", new Vector3(1.45f, 1.6f, 0), new Vector3(0.3f, 3.2f, 0.4f), matDark, energyAirlockGroup, true);
            CreateBox("Airlock Lintel", new Vector3(0, 3.35f, 0), new Vector3(3.2f, 0.3f, 0.4f), matDark, energyAirlockGroup, true);
            CreateBox("Airlock Upper Bulkhead", new Vector3(0, 4.45f, 0), new Vector3(3.2f, 1.9f, 0.4f), matDark, energyAirlockGroup, true);

            var energyLeaf = new GameObject("Energy Sliding Leaf").transform;
            energyLeaf.SetParent(energyAirlockGroup, false);
            BuildArmoredDoorLeaf(energyLeaf, 2.65f, 3.1f, 0.2f, 1.8f, 1.28f, 0.45f, matDark, matGlass, matMetal, matAmber, "Energy Airlock");

            var energyDoor = energyAirlockGroup.gameObject.AddComponent<FacilityDoor>();
            energyDoor.Configure(energyLeaf, DoorAccessCondition.ManualUnlockOnly,
                "Шлюз энергоцентра заблокирован · авторизуйтесь на терминале DATA-01",
                "[E] Открыть гермошлюз энергоцентра",
                "[E] Закрыть гермошлюз энергоцентра",
                true, Vector3.up, 3.2f, 1.8f);

            // Door Access Console mounted on jamb wall facing the room
            var doorAccessGroup = new GameObject("Door Access Console").transform;
            doorAccessGroup.SetParent(sector02, false);
            doorAccessGroup.position = new Vector3(-6.82f, 1.35f, 24.15f);
            doorAccessGroup.rotation = Quaternion.Euler(0, 90, 0);
            CreateBox("Console Enclosure", Vector3.zero, new Vector3(0.45f, 0.65f, 0.16f), matDark, doorAccessGroup, true);
            CreateBox("Console Status Plate", new Vector3(0, 0.14f, 0.09f), new Vector3(0.38f, 0.22f, 0.02f), matScreen, doorAccessGroup, false);
            ScreenText(doorAccessGroup, "AIRLOCK 03\nCONTROL", new Vector3(-0.16f, 0.22f, 0.102f), 0.0020f, new Color(0.45f, 0.85f, 0.95f));
            CreateBox("Console Status LED", new Vector3(0, -0.14f, 0.09f), new Vector3(0.10f, 0.05f, 0.02f), LoadMat("Indicator Amber"), doorAccessGroup, false);

            // Central Dataset Terminal Workstation (DATA-01) — Unlocks BOTH energyDoor and armoryDoor!
            BuildDatasetTerminalPodium(sector02, new Vector3(0, 0, 26.0f), energyDoor, armoryDoor, matMetal, matDark, matSage, matScreen);

            // Directional Signboards for Wayfinding
            CreateSignboard(sector02, "СЕКТОР 02: ВЫЧИСЛИТЕЛЬНЫЙ УЗЕЛ", "SEC-02", new Vector3(0, 3.45f, 14.8f), 0, new Color(0.45f, 0.85f, 1.0f));
            CreateSignboard(sector02, "СЕКТОР 03: ЭНЕРГОЦЕНТР", "SEC-03", new Vector3(-6.78f, 3.45f, 26.0f), -90, new Color(1.0f, 0.75f, 0.25f));

            // Comprehensive Architectural Illumination for Sector 02 (Compute Vault)
            // Central Aisle High-Bay Downlights & Linear Fixtures
            int[] vaultAisleZ = new[] { 18, 24, 30, 36 };
            for (int i = 0; i < vaultAisleZ.Length; i++)
            {
                int zPos = vaultAisleZ[i];
                ModularKit.Place("Prop_Light_Wide", sector02, new Vector3(0, 5.35f, zPos), new Vector3(1.2f, 1.0f, 1.0f), 0, false);
                CreateSpotLight("Vault Aisle Light " + zPos, sector02, new Vector3(0, 5.2f, zPos), new Vector3(90, 0, 0), new Color(0.85f, 0.94f, 1.0f), 6.0f, 18f, 110f);
                CreatePointLight("Vault Aisle Fill " + zPos, sector02, new Vector3(0, 4.8f, zPos), new Color(0.65f, 0.82f, 0.98f), 4.0f, 16f);
            }
            // Side Bay Rack Illumination (West & East server rows)
            int[] rackBayZ = new[] { 20, 26, 32 };
            for (int i = 0; i < rackBayZ.Length; i++)
            {
                int zPos = rackBayZ[i];
                CreateSpotLight("Vault West Bay " + zPos, sector02, new Vector3(-4.5f, 5.0f, zPos), new Vector3(90, 0, 0), new Color(0.78f, 0.90f, 1.0f), 4.5f, 14f, 100f);
                CreateSpotLight("Vault East Bay " + zPos, sector02, new Vector3(4.5f, 5.0f, zPos), new Vector3(90, 0, 0), new Color(0.78f, 0.90f, 1.0f), 4.5f, 14f, 100f);
            }
            // Portal Entry Wayfinding Lights
            CreatePointLight("Vault North Portal Light", sector02, new Vector3(0, 3.8f, 16.0f), new Color(0.85f, 0.92f, 1.0f), 4.0f, 10f);
            CreatePointLight("Vault South Portal Light", sector02, new Vector3(0, 3.8f, 37.0f), new Color(0.85f, 0.92f, 1.0f), 4.0f, 10f);
            CreatePointLight("Vault Airlock Portal Light", sector02, new Vector3(-6.2f, 3.5f, 24.15f), new Color(1.0f, 0.85f, 0.55f), 4.0f, 10f);

            // ReflectionProbe for Compute Vault
            CreateReflectionProbe("Reflection Probe - Compute Vault", new Vector3(0, 2.5f, 26.5f), new Vector3(16.0f, 6.0f, 25.0f), sector02);

            // Autonomous Patrol Robot: SENTINEL R21 (QuadShell)
            BuildPatrolRobot(sector02, "sentinel-r21", "Enemy_QuadShell",
                new Vector3(0, 0, 22.0f), 180f,
                new Vector3(0, 0, 18.0f), new Vector3(0, 0, 34.0f),
                new Vector2(-5.5f, 5.5f), new Vector2(17.0f, 36.0f),
                matMetal, matDark, matAmber);

            // =========================================================================
            // 3. SECTOR 03: POWER & ENERGY CENTER (X = -7..-26, Z = 18..38, H = 6.2m)
            // =========================================================================
            var sector03 = new GameObject("Sector 03 - Energy Center").transform;
            sector03.SetParent(root.transform, false);

            CreateBox("Energy Center Floor Base", new Vector3(-16.5f, -0.1f, 28f), new Vector3(19.4f, 0.2f, 20.4f), matFloor, sector03, true);
            CreateBox("Generator Outer Ring Slab", new Vector3(-16.5f, 0.02f, 28f), new Vector3(7.2f, 0.04f, 7.2f), matDark, sector03, false);

            CreateBox("Energy Center Ceiling", new Vector3(-16.5f, 6.25f, 28f), new Vector3(19.4f, 0.2f, 20.4f), matDark, sector03, true);

            // Bulkhead at X = -7.0 sealing height difference between Sector 02 (Y=5.4) and Sector 03 (Y=6.25)
            CreateBox("Sector02-to-Sector03 Bulkhead", new Vector3(-7.0f, 5.825f, 28.0f), new Vector3(0.4f, 0.85f, 20.0f), matDark, root.transform, true);

            // West Outer Wall (X = -26.0)
            CreateBox("Sector03 West Monolithic Wall", new Vector3(-26.25f, 3.125f, 28.0f), new Vector3(0.5f, 6.25f, 20.5f), matConcrete, sector03, true);
            for (int bay = 0; bay < 5; bay++)
            {
                ModularKit.Place("WallAstra_Straight", sector03, new Vector3(-26.0f, 0, 20f + bay * 4.0f), new Vector3(0.5f, 6.2f, 4.0f), 0, true);
            }
            // North Outer Wall (Z = 38.0)
            CreateBox("Sector03 North Monolithic Wall", new Vector3(-16.5f, 3.125f, 38.25f), new Vector3(19.5f, 6.25f, 0.5f), matConcrete, sector03, true);
            for (int bay = 0; bay < 4; bay++)
            {
                ModularKit.Place("WallAstra_Straight", sector03, new Vector3(-24f + bay * 4.5f, 0, 38.0f), new Vector3(4.5f, 6.2f, 0.5f), 90, true);
            }
            // South Outer Wall (Z = 18.0 with opening at X = -16.5 for maintenance bypass)
            CreateBox("Sector03 South Monolithic Wall West", new Vector3(-22.35f, 3.125f, 18.0f), new Vector3(7.8f, 6.25f, 0.5f), matConcrete, sector03, true);
            CreateBox("Sector03 South Monolithic Wall East", new Vector3(-10.75f, 3.125f, 18.0f), new Vector3(7.6f, 6.25f, 0.5f), matConcrete, sector03, true);
            ModularKit.Place("WallAstra_Straight", sector03, new Vector3(-22.25f, 0, 18.0f), new Vector3(7.5f, 6.2f, 0.5f), -90, true);
            ModularKit.Place("WallAstra_Straight", sector03, new Vector3(-10.75f, 0, 18.0f), new Vector3(7.5f, 6.2f, 0.5f), -90, true);

            // Bulkhead at Z = 18.0 sealing height difference between Bypass Corridor (Y=4.0) and Sector 03 (Y=6.25)
            CreateBox("Bypass-to-Sector03 Bulkhead", new Vector3(-16.5f, 5.125f, 18.0f), new Vector3(4.0f, 2.25f, 0.5f), matDark, root.transform, true);

            // Central Power Reactor Generator
            var coreGroup = new GameObject("Central Generator Core").transform;
            coreGroup.SetParent(sector03, false);
            coreGroup.position = new Vector3(-16.5f, 0, 28f);

            CreateBox("Reactor Pedestal", new Vector3(0, 0.4f, 0), new Vector3(5.6f, 0.8f, 5.6f), matDark, coreGroup, true);
            CreateBox("Reactor Containment Ring", new Vector3(0, 2.0f, 0), new Vector3(4.4f, 2.4f, 4.4f), matMetal, coreGroup, true);

            CreateBox("Magnetic Coil 1", new Vector3(0, 1.8f, 0), new Vector3(4.6f, 0.35f, 4.6f), matAmber, coreGroup, false);
            CreateBox("Magnetic Coil 2", new Vector3(0, 2.6f, 0), new Vector3(4.6f, 0.35f, 4.6f), matAmber, coreGroup, false);

            ModularKit.Place("Column_Pipes", coreGroup, new Vector3(-3.0f, 0, -3.0f), new Vector3(1.3f, 6.2f, 1.3f), 45, true);
            ModularKit.Place("Column_Pipes", coreGroup, new Vector3(3.0f, 0, -3.0f), new Vector3(1.3f, 6.2f, 1.3f), -45, true);
            ModularKit.Place("Column_Pipes", coreGroup, new Vector3(-3.0f, 0, 3.0f), new Vector3(1.3f, 6.2f, 1.3f), 135, true);
            ModularKit.Place("Column_Pipes", coreGroup, new Vector3(3.0f, 0, 3.0f), new Vector3(1.3f, 6.2f, 1.3f), -135, true);

            ModularKit.Place("Prop_Vent_Big", sector03, new Vector3(-20.5f, 2.8f, 37.6f), new Vector3(2.4f, 2.4f, 0.8f), 0, true);
            ModularKit.Place("Prop_Vent_Big", sector03, new Vector3(-12.5f, 2.8f, 37.6f), new Vector3(2.4f, 2.4f, 0.8f), 0, true);

            for (int i = 0; i < 3; i++)
            {
                ModularKit.Place("Prop_Barrel_Large", sector03, new Vector3(-23.5f, 0, 22f + i * 1.5f), new Vector3(1.1f, 1.4f, 1.1f), 0, true);
                ModularKit.Place("Prop_Barrel_Large", sector03, new Vector3(-24.7f, 0, 22.7f + i * 1.5f), new Vector3(1.1f, 1.4f, 1.1f), 25, true);
            }

            // Authentic Poly Haven Props in Sector 03
            PlaceDressing("Maintenance cart", sector03, new Vector3(-22.5f, 0, 31f), 45, "Sector03 Maintenance Cart");
            // Mounted solidly onto the SW reactor cooling pipe column (X=-19.5, Z=25.0)
            PlaceDressing("Cooling valve", sector03, new Vector3(-18.7f, 1.35f, 25.0f), 90, "Sector03 Cooling Valve");
            CreateBox("Reactor Valve Flange", new Vector3(-19.1f, 1.35f, 25.0f), new Vector3(0.75f, 0.16f, 0.16f), matDark, sector03, false);

            // Cryo Gate into Sector 04 (Z = 38.0, X = 0)
            var cryoGateGroup = new GameObject("Cryo Gate (Z=38)").transform;
            cryoGateGroup.SetParent(root.transform, false);
            cryoGateGroup.position = new Vector3(0, 0, 38f);

            CreateBox("Cryo Left Jamb", new Vector3(-1.55f, 1.8f, 0), new Vector3(0.35f, 3.6f, 0.4f), matDark, cryoGateGroup, true);
            CreateBox("Cryo Right Jamb", new Vector3(1.55f, 1.8f, 0), new Vector3(0.35f, 3.6f, 0.4f), matDark, cryoGateGroup, true);
            CreateBox("Cryo Lintel", new Vector3(0, 3.75f, 0), new Vector3(3.45f, 0.35f, 0.4f), matDark, cryoGateGroup, true);

            var cryoLeaf = new GameObject("Cryo Sliding Leaf").transform;
            cryoLeaf.SetParent(cryoGateGroup, false);
            BuildArmoredDoorLeaf(cryoLeaf, 2.95f, 3.55f, 0.25f, 2.1f, 1.4f, 0.48f, matDark, matGlass, matMetal, matAmber, "Cryo");

            var cryoGateDoor = cryoGateGroup.gameObject.AddComponent<FacilityDoor>();
            cryoGateDoor.Configure(cryoLeaf, DoorAccessCondition.ManualUnlockOnly,
                "Крио-шлюз заблокирован · запустите подстанцию на PWR-01",
                "[E] Открыть криогенный шлюз",
                "[E] Закрыть криогенный шлюз",
                true, Vector3.up, 3.6f, 1.8f);

            // Transom Bulkhead at Z = 38.0 sealing height differences into Sector 04 (Y=7.55)
            CreateBox("Sector02-to-Sector04 Bulkhead", new Vector3(0, 6.475f, 38.0f), new Vector3(14.0f, 2.15f, 0.5f), matDark, root.transform, true);
            CreateBox("Sector03-to-Sector04 Bulkhead", new Vector3(-16.5f, 6.9f, 38.0f), new Vector3(19.0f, 1.3f, 0.5f), matDark, root.transform, true);

            // Power Distribution Console Station (PWR-01) — Hooked to unlock cryoGateDoor!
            BuildPowerConsole(sector03, new Vector3(-16.5f, 0, 35.5f), cryoGateDoor, matMetal, matDark, matAmber, matScreen);

            // Overhead Cable Drops
            for (int z = 22; z <= 34; z += 6)
            {
                CreateBox("S03 Cable Hanger Left " + z, new Vector3(-21.0f, 5.8f, z), new Vector3(0.06f, 0.8f, 0.06f), matDark, sector03, false);
                CreateBox("S03 Cable Hanger Right " + z, new Vector3(-12.0f, 5.8f, z), new Vector3(0.06f, 0.8f, 0.06f), matDark, sector03, false);
            }

            // Directional Signboard over Cryo Gate into Sector 04
            CreateSignboard(sector03, "СЕКТОР 04: КВАНТОВЫЙ УЗЕЛ", "SEC-04", new Vector3(0, 3.95f, 37.8f), 0, new Color(0.4f, 0.9f, 1.0f));

            // ReflectionProbe for Energy Center
            CreateReflectionProbe("Reflection Probe - Energy Center", new Vector3(-16.5f, 3.0f, 28.0f), new Vector3(22.0f, 7.0f, 22.0f), sector03);

            // Comprehensive Architectural Illumination for Sector 03 (Energy Center)
            CreatePointLight("Core Amber Light", coreGroup, new Vector3(0, 2.2f, 0), new Color(1.0f, 0.65f, 0.15f), 7.5f, 18f);

            // 4 High-Bay Overhead Floodlights illuminating the reactor hall floor and machinery
            Vector3[] highBayPositions = new[] {
                new Vector3(-12.0f, 6.2f, 24.0f),
                new Vector3(-21.0f, 6.2f, 24.0f),
                new Vector3(-12.0f, 6.2f, 34.0f),
                new Vector3(-21.0f, 6.2f, 34.0f)
            };
            for (int i = 0; i < highBayPositions.Length; i++)
            {
                Vector3 hbPos = highBayPositions[i];
                ModularKit.Place("Prop_Light_Wide", sector03, hbPos, new Vector3(1.3f, 1.0f, 1.3f), 0, false);
                CreateSpotLight("Reactor HighBay " + i, sector03, hbPos, new Vector3(90, 0, 0), new Color(0.98f, 0.94f, 0.86f), 6.5f, 22f, 115f);
                CreatePointLight("Reactor HighBay Fill " + i, sector03, hbPos - Vector3.up * 0.5f, new Color(0.92f, 0.84f, 0.68f), 4.2f, 16f);
            }
            // Perimeter Walkway Industrial Wall Sconces
            CreatePointLight("Energy West Sconce 1", sector03, new Vector3(-26.2f, 3.2f, 24.0f), new Color(1.0f, 0.82f, 0.55f), 4.0f, 12.0f);
            CreatePointLight("Energy West Sconce 2", sector03, new Vector3(-26.2f, 3.2f, 34.0f), new Color(1.0f, 0.82f, 0.55f), 4.0f, 12.0f);
            CreatePointLight("Energy South Sconce", sector03, new Vector3(-16.5f, 3.2f, 20.2f), new Color(1.0f, 0.82f, 0.55f), 4.0f, 12.0f);
            CreatePointLight("Energy South Portal Downlight", sector03, new Vector3(-16.5f, 4.8f, 19.5f), new Color(1.0f, 0.88f, 0.65f), 4.5f, 12.0f);

            // Autonomous Patrol Robot: REACTOR SENTRY R22 (Trilobite)
            BuildPatrolRobot(sector03, "reactor-sentry-r22", "Enemy_Trilobite",
                new Vector3(-16.5f, 0, 23.0f), 0f,
                new Vector3(-21.0f, 0, 23.0f), new Vector3(-12.0f, 0, 33.0f),
                new Vector2(-24.5f, -8.5f), new Vector2(19.0f, 36.5f),
                matMetal, matDark, matAmber);

            // =========================================================================
            // 4. SECTOR 04: SUB-LEVEL CRYO & QUANTUM CORE NEXUS (Z = 38..62, X = -10..10, H = 7.5m)
            // =========================================================================
            var sector04 = new GameObject("Sector 04 - Quantum Nexus").transform;
            sector04.SetParent(root.transform, false);

            // Cavernous floor with lower trench
            CreateBox("Quantum Chamber Upper Floor Left", new Vector3(-6.5f, -0.1f, 50f), new Vector3(7.4f, 0.2f, 24.4f), matConcrete, sector04, true);
            CreateBox("Quantum Chamber Upper Floor Right", new Vector3(6.5f, -0.1f, 50f), new Vector3(7.4f, 0.2f, 24.4f), matConcrete, sector04, true);

            // Lower sub-floor trench (X: -2.8..2.8, Y = -1.2m)
            CreateBox("Trench Floor", new Vector3(0, -1.3f, 50f), new Vector3(5.8f, 0.2f, 24.4f), matDark, sector04, true);
            CreateBox("Trench Wall Left", new Vector3(-2.85f, -0.6f, 50f), new Vector3(0.1f, 1.2f, 24.4f), matDark, sector04, true);
            CreateBox("Trench Wall Right", new Vector3(2.85f, -0.6f, 50f), new Vector3(0.1f, 1.2f, 24.4f), matDark, sector04, true);

            // Watertight Trench Bulkheads: completely seal both ends of the trench
            CreateBox("Trench North Bulkhead", new Vector3(0, -0.65f, 62.0f), new Vector3(6.0f, 1.3f, 0.5f), matDark, sector04, true);
            CreateBox("Trench South Bulkhead", new Vector3(0, -0.65f, 38.0f), new Vector3(6.0f, 1.3f, 0.5f), matDark, sector04, true);

            // Massive Structural Bridge Support Pylons connecting bridge underside to trench floor (replacing clipping stairs)
            for (int pz = 42; pz <= 58; pz += 5)
            {
                CreateBox("Bridge Support Pylon " + pz, new Vector3(0, -0.65f, pz), new Vector3(1.8f, 1.3f, 1.2f), matDark, sector04, true);
                CreateBox("Pylon Strut Left " + pz, new Vector3(-1.4f, -0.65f, pz), new Vector3(0.8f, 0.25f, 0.8f), matMetal, sector04, false);
                CreateBox("Pylon Strut Right " + pz, new Vector3(1.4f, -0.65f, pz), new Vector3(0.8f, 0.25f, 0.8f), matMetal, sector04, false);
            }

            // High Ceiling (Y = 7.5m)
            CreateBox("Quantum Nexus Ceiling", new Vector3(0, 7.55f, 50f), new Vector3(20.4f, 0.2f, 24.4f), matDark, sector04, true);

            // Outer Perimeter Walls with Monolithic Hermetic Backers
            CreateBox("Sector04 East Monolithic Wall", new Vector3(10.25f, 3.775f, 50.0f), new Vector3(0.5f, 7.55f, 24.5f), matConcrete, sector04, true);
            CreateBox("Sector04 West Monolithic Wall", new Vector3(-10.25f, 3.775f, 50.0f), new Vector3(0.5f, 7.55f, 24.5f), matConcrete, sector04, true);
            CreateBox("Sector04 North Monolithic Wall", new Vector3(0, 3.775f, 62.25f), new Vector3(20.5f, 7.55f, 0.5f), matConcrete, sector04, true);

            // South Boundary Wings sealing Sector 04 from X = -10.0 to X = 10.0 at Z = 38.0
            CreateBox("Sector04 South Wall West Wing", new Vector3(-8.625f, 3.775f, 38.0f), new Vector3(3.25f, 7.55f, 0.5f), matConcrete, sector04, true);
            ModularKit.Place("WallAstra_Straight", sector04, new Vector3(-8.5f, 0, 38.15f), new Vector3(3.0f, 7.5f, 0.3f), -90, true);
            CreateBox("Sector04 South Wall East Wing", new Vector3(8.625f, 3.775f, 38.0f), new Vector3(3.25f, 7.55f, 0.5f), matConcrete, sector04, true);
            ModularKit.Place("WallAstra_Straight", sector04, new Vector3(8.5f, 0, 38.15f), new Vector3(3.0f, 7.5f, 0.3f), -90, true);

            for (int bay = 0; bay < 6; bay++)
            {
                ModularKit.Place("WallAstra_Straight", sector04, new Vector3(-10.0f, 0, 40f + bay * 4.0f), new Vector3(0.5f, 7.5f, 4.0f), 0, true);
                ModularKit.Place("WallAstra_Straight", sector04, new Vector3(10.0f, 0, 40f + bay * 4.0f), new Vector3(0.5f, 7.5f, 4.0f), 180, true);
            }
            // North End Wall (Z = 62.0)
            for (int bay = 0; bay < 5; bay++)
            {
                ModularKit.Place("WallAstra_Straight", sector04, new Vector3(-8f + bay * 4.0f, 0, 62.0f), new Vector3(4.0f, 7.5f, 0.5f), 90, true);
            }

            // Massive Cryogenic Supply Pipes running overhead
            for (int z = 41; z <= 59; z += 6)
            {
                ModularKit.Place("Column_Pipes", sector04, new Vector3(-6.0f, 0, z), new Vector3(1.4f, 7.5f, 1.4f), 0, true);
                ModularKit.Place("Column_Pipes", sector04, new Vector3(6.0f, 0, z), new Vector3(1.4f, 7.5f, 1.4f), 0, true);
                ModularKit.Place("TopCables_Straight_Hanging", sector04, new Vector3(0, 6.8f, z), new Vector3(16f, 1.0f, 0.8f), 90, false);
            }

            // Central Quantum Relay Bridge spanning continuously over the trench from Z = 38.0 to Z = 62.0
            CreateBox("Relay Bridge Walkway", new Vector3(0, 0, 50.0f), new Vector3(4.2f, 0.25f, 24.0f), matMetal, sector04, true);

            // Side Safety Railings along the West and East edges of the bridge (Z = 38.0 to Z = 62.0 with crossway opening at Z = 50)
            int[] railZ = new[] { 40, 44, 56, 60 };
            for (int i = 0; i < railZ.Length; i++)
            {
                int rz = railZ[i];
                ModularKit.Place("Platform_Rails_4Wide", sector04, new Vector3(-2.05f, 0.125f, rz), new Vector3(0.08f, 1.1f, 4.0f), 90, true);
                ModularKit.Place("Platform_Rails_4Wide", sector04, new Vector3(2.05f, 0.125f, rz), new Vector3(0.08f, 1.1f, 4.0f), -90, true);
            }
            // Half railings flanking the crossway opening (Z: 47..48.8 and 51.2..53)
            ModularKit.Place("Platform_Rails_4", sector04, new Vector3(-2.05f, 0.125f, 47.4f), new Vector3(0.08f, 1.1f, 2.4f), 90, true);
            ModularKit.Place("Platform_Rails_4", sector04, new Vector3(2.05f, 0.125f, 47.4f), new Vector3(0.08f, 1.1f, 2.4f), -90, true);
            ModularKit.Place("Platform_Rails_4", sector04, new Vector3(-2.05f, 0.125f, 52.6f), new Vector3(0.08f, 1.1f, 2.4f), 90, true);
            ModularKit.Place("Platform_Rails_4", sector04, new Vector3(2.05f, 0.125f, 52.6f), new Vector3(0.08f, 1.1f, 2.4f), -90, true);

            // Crossway service walkways connecting central bridge to East and West cryogenic platforms
            CreateBox("West Platform Crossway", new Vector3(-5.5f, 0, 50.0f), new Vector3(6.8f, 0.22f, 2.4f), matMetal, sector04, true);
            ModularKit.Place("Platform_Rails_4Wide", sector04, new Vector3(-5.5f, 0.12f, 51.2f), new Vector3(6.8f, 1.1f, 0.08f), 0, true);
            ModularKit.Place("Platform_Rails_4Wide", sector04, new Vector3(-5.5f, 0.12f, 48.8f), new Vector3(6.8f, 1.1f, 0.08f), 180, true);

            CreateBox("East Platform Crossway", new Vector3(5.5f, 0, 50.0f), new Vector3(6.8f, 0.22f, 2.4f), matMetal, sector04, true);
            ModularKit.Place("Platform_Rails_4Wide", sector04, new Vector3(5.5f, 0.12f, 51.2f), new Vector3(6.8f, 1.1f, 0.08f), 0, true);
            ModularKit.Place("Platform_Rails_4Wide", sector04, new Vector3(5.5f, 0.12f, 48.8f), new Vector3(6.8f, 1.1f, 0.08f), 180, true);

            // Trench Maintenance Access: Service Egress Stairs connecting trench floor (Y=-1.2) to bridge level (Y=0)
            for (int side = -1; side <= 1; side += 2)
            {
                float sx = side * 1.6f;
                // South trench egress stairs (ascending North towards bridge)
                ModularKit.Place("Platform_Stairs_2", sector04, new Vector3(sx, -1.2f, 39.8f), new Vector3(1.1f, 1.2f, 2.2f), 0, true);
                // North trench egress stairs (ascending South towards bridge)
                ModularKit.Place("Platform_Stairs_2", sector04, new Vector3(sx, -1.2f, 60.2f), new Vector3(1.1f, 1.2f, 2.2f), 180, true);
            }

            // =========================================================================
            // 5. EXECUTIVE SHORTCUT DOOR (X = -5.0, Z = -1.75) INTO SOC
            // =========================================================================
            var execAirlockGroup = new GameObject("Executive Shortcut Door").transform;
            execAirlockGroup.SetParent(root.transform, false);
            execAirlockGroup.position = new Vector3(-5.0f, 0, -1.75f);
            execAirlockGroup.rotation = Quaternion.Euler(0, 90, 0); // Door width along world Z!

            CreateBox("Exec Left Jamb", new Vector3(-1.3f, 1.5f, 0), new Vector3(0.3f, 3.0f, 0.4f), matDark, execAirlockGroup, true);
            CreateBox("Exec Right Jamb", new Vector3(1.3f, 1.5f, 0), new Vector3(0.3f, 3.0f, 0.4f), matDark, execAirlockGroup, true);
            CreateBox("Exec Lintel", new Vector3(0, 3.15f, 0), new Vector3(2.9f, 0.3f, 0.4f), matDark, execAirlockGroup, true);

            var execLeaf = new GameObject("Exec Sliding Leaf").transform;
            execLeaf.SetParent(execAirlockGroup, false);
            BuildArmoredDoorLeaf(execLeaf, 2.4f, 2.9f, 0.18f, 1.8f, 1.2f, 0.42f, matDark, matGlass, matMetal, matAmber, "Exec");

            var execDoor = execAirlockGroup.gameObject.AddComponent<FacilityDoor>();
            execDoor.Configure(execLeaf, DoorAccessCondition.ManualUnlockOnly,
                "Служебный шлюз заблокирован · активируйте обходной контур на QNT-01",
                "[E] Открыть служебный шлюз",
                "[E] Закрыть служебный шлюз",
                true, Vector3.up, 3.0f, 1.8f);

            // Quantum Terminal (QNT-01) on the central bridge — Hooked to unlock execDoor!
            BuildQuantumTerminal(sector04, new Vector3(0, 0.25f, 50.0f), execDoor, matMetal, matDark, matAmber, matScreen);

            // Directional Signboard over Executive Shortcut Door
            CreateSignboard(root.transform, "СЛУЖЕБНЫЙ ШЛЮЗ // EXECUTIVE SHORTCUT", "SOC", new Vector3(-4.8f, 3.35f, -1.75f), -90, new Color(0.45f, 0.85f, 1.0f));

            // ReflectionProbe for Quantum Nexus
            CreateReflectionProbe("Reflection Probe - Quantum Nexus", new Vector3(0, 3.5f, 50.0f), new Vector3(22.0f, 9.0f, 26.0f), sector04);

            // Comprehensive Architectural Illumination for Sector 04 (Quantum Nexus)
            // 4 Overhead Central Gantry Floodlights along the Bridge
            int[] nexusZ = new[] { 42, 48, 54, 60 };
            for (int i = 0; i < nexusZ.Length; i++)
            {
                int zPos = nexusZ[i];
                ModularKit.Place("Prop_Light_Wide", sector04, new Vector3(0, 7.3f, zPos), new Vector3(1.3f, 1.0f, 1.3f), 0, false);
                CreateSpotLight("Nexus HighBay " + zPos, sector04, new Vector3(0, 7.2f, zPos), new Vector3(90, 0, 0), new Color(0.88f, 0.95f, 1.0f), 7.0f, 24f, 110f);
                CreatePointLight("Nexus Fill " + zPos, sector04, new Vector3(0, 6.2f, zPos), new Color(0.68f, 0.86f, 1.0f), 4.5f, 18f);
            }
            // Cryo Containment Tank Illumination
            Vector3[] tankSpots = new[] {
                new Vector3(-6.0f, 7.0f, 45.0f),
                new Vector3(6.0f, 7.0f, 45.0f),
                new Vector3(-6.0f, 7.0f, 55.0f),
                new Vector3(6.0f, 7.0f, 55.0f)
            };
            for (int i = 0; i < tankSpots.Length; i++)
            {
                CreateSpotLight("Tank Spot " + i, sector04, tankSpots[i], new Vector3(90, 0, 0), new Color(0.40f, 0.85f, 1.0f), 5.0f, 16f, 90f);
            }

            // Autonomous Patrol Robot: QUANTUM WATCHER R23 (QuadShell)
            BuildPatrolRobot(sector04, "quantum-watcher-r23", "Enemy_QuadShell",
                new Vector3(0, 0, 44.0f), 0f,
                new Vector3(0, 0, 42.0f), new Vector3(0, 0, 58.0f),
                new Vector2(-1.8f, 1.8f), new Vector2(40.0f, 60.0f),
                matMetal, matDark, matAmber);

            // =========================================================================
            // 6. SERVICE MAINTENANCE SHORTCUT / BYPASS CORRIDOR (Watertight geometry)
            // =========================================================================
            var bypass = new GameObject("Service Maintenance Bypass Corridor").transform;
            bypass.SetParent(root.transform, false);

            // East-West Connector Corridor: from X = -16.5 to X = -5.0 at Z = -1.75
            CreateBox("Connector Floor", new Vector3(-10.75f, -0.1f, -1.75f), new Vector3(12.0f, 0.2f, 3.0f), matConcrete, bypass, true);
            CreateBox("Connector Ceiling", new Vector3(-10.75f, 3.35f, -1.75f), new Vector3(12.0f, 0.2f, 3.0f), matDark, bypass, true);
            CreateBox("Connector Wall North", new Vector3(-10.75f, 1.675f, -0.25f), new Vector3(12.0f, 3.35f, 0.4f), matConcrete, bypass, true);
            CreateBox("Connector Wall South", new Vector3(-10.75f, 1.675f, -3.25f), new Vector3(12.0f, 3.35f, 0.4f), matConcrete, bypass, true);

            // North-South Bypass Corridor: from Z = -3.25 to Z = 18.0 at X = -16.5
            CreateBox("Bypass Floor", new Vector3(-16.5f, -0.1f, 7.375f), new Vector3(3.9f, 0.2f, 21.5f), matFloor, bypass, true);
            CreateBox("Bypass Ceiling", new Vector3(-16.5f, 4.05f, 7.375f), new Vector3(3.9f, 0.2f, 21.5f), matDark, bypass, true);

            // Solid South End Wall completely sealing the corridor at Z = -3.25
            CreateBox("Bypass South Wall", new Vector3(-16.5f, 2.0f, -3.25f), new Vector3(3.9f, 4.0f, 0.4f), matConcrete, bypass, true);

            // Bulkhead at X = -16.5 sealing height difference between Connector (Y=3.35) and Bypass (Y=4.0)
            CreateBox("Connector-to-Bypass Bulkhead", new Vector3(-16.5f, 3.675f, -1.75f), new Vector3(0.4f, 0.65f, 3.0f), matDark, bypass, true);

            // Bypass West Wall (X = -18.25) sealed continuously from Z = -3.25 to Z = 18.0
            CreateBox("Bypass West Monolithic Backer", new Vector3(-18.45f, 2.0f, 7.375f), new Vector3(0.4f, 4.0f, 21.25f), matConcrete, bypass, true);
            CreateBox("Bypass West Wall South Segment", new Vector3(-18.25f, 2.0f, -1.625f), new Vector3(0.4f, 4.0f, 3.25f), matConcrete, bypass, true);
            for (int z = 0; z <= 16; z += 4)
            {
                ModularKit.Place("WallAstra_Straight", bypass, new Vector3(-18.25f, 0, z), new Vector3(0.4f, 4.0f, 4.0f), 0, true);
            }

            // Bypass East Wall (X = -14.75) sealed with opening into Connector at Z in [-3.25, -0.25]
            CreateBox("Bypass East Monolithic Backer", new Vector3(-14.55f, 2.0f, 8.875f), new Vector3(0.4f, 4.0f, 18.25f), matConcrete, bypass, true);
            for (int z = 2; z <= 14; z += 4)
            {
                ModularKit.Place("WallAstra_Straight", bypass, new Vector3(-14.75f, 0, z), new Vector3(0.4f, 4.0f, 4.0f), 180, true);
            }
            CreateBox("Bypass East Wall North Segment", new Vector3(-14.75f, 2.0f, 17.0f), new Vector3(0.4f, 4.0f, 2.4f), matConcrete, bypass, true);

            // Storage Crates in Bypass
            ModularKit.Place("Prop_Crate3", bypass, new Vector3(-17.2f, 0, 11.5f), new Vector3(0.8f, 0.8f, 0.8f), 15, true);
            ModularKit.Place("Prop_Crate4", bypass, new Vector3(-17.2f, 0, 12.5f), new Vector3(0.85f, 0.85f, 0.85f), -20, true);

            // Comprehensive Tunnel Illumination along Bypass & Executive Connector
            float[] bypassZ = new[] { -1.75f, 3.0f, 8.0f, 13.0f, 18.0f };
            for (int i = 0; i < bypassZ.Length; i++)
            {
                float zPos = bypassZ[i];
                float xPos = zPos < 0 ? -10.75f : -16.5f;
                CreatePointLight("Bypass Bulkhead Light " + i, bypass, new Vector3(xPos, 2.8f, zPos), new Color(1.0f, 0.88f, 0.65f), 4.2f, 11.0f);
            }

            // Autonomous Patrol Robot: PATROL DRONE R24 (Trilobite)
            BuildPatrolRobot(bypass, "patrol-drone-r24", "Enemy_Trilobite",
                new Vector3(-16.5f, 0, 5.0f), 0f,
                new Vector3(-16.5f, 0, 1.5f), new Vector3(-16.5f, 0, 15.0f),
                new Vector2(-17.5f, -15.5f), new Vector2(0.5f, 16.5f),
                matMetal, matDark, matAmber);
        }

        private static void AdaptServerRoomBackWall(Scene scene)
        {
            var srvRoom = scene.GetRootGameObjects()
                .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(t => t.name == "SRV / Server Room");

            if (srvRoom != null)
            {
                var toDestroy = srvRoom.Cast<Transform>()
                    .Where(child => child.name == "End wall" ||
                                    child.name == "Painted room backdrop" ||
                                    child.name == "Service lower wall" ||
                                    (child.name.StartsWith("Portal") && child.localPosition.z > 2.0f))
                    .Select(child => child.gameObject)
                    .ToList();

                foreach (var go in toDestroy)
                {
                    UnityEngine.Object.DestroyImmediate(go);
                }
            }

            // Also ensure any stray backdrop near Z >= 5 in the facility is purged so the gateway is 100% unobstructed
            var strayBackdrops = scene.GetRootGameObjects()
                .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                .Where(t => t.name == "Painted room backdrop" && t.position.z > 5.0f)
                .Select(t => t.gameObject)
                .ToList();
            foreach (var go in strayBackdrops)
            {
                UnityEngine.Object.DestroyImmediate(go);
            }

            var rootObj = scene.GetRootGameObjects().FirstOrDefault(r => r.name == "NORTHLINE / Facility" || r.name == "NORTHLINE / Facility 04");
            var parentTransform = rootObj != null ? rootObj.transform : srvRoom;
            if (parentTransform != null)
            {
                var wallsToDestroy = parentTransform.Cast<Transform>()
                    .Where(child => child.name.StartsWith("Server North Wall"))
                    .Select(child => child.gameObject)
                    .ToList();

                foreach (var go in wallsToDestroy)
                {
                    UnityEngine.Object.DestroyImmediate(go);
                }

                var matConcrete = LoadMat("Concrete");
                var matDark = LoadMat("Dark Metal");

                // Doorway opening between X = -1.35 and X = +1.35 up to Y = 3.0
                CreateBox("Server North Wall Left", new Vector3(-3.25f, 1.5f, 15.0f), new Vector3(3.8f, 3.0f, 0.3f), matConcrete, parentTransform, true);
                CreateBox("Server North Wall Right", new Vector3(3.25f, 1.5f, 15.0f), new Vector3(3.8f, 3.0f, 0.3f), matConcrete, parentTransform, true);
                CreateBox("Server North Wall Lintel", new Vector3(0, 3.175f, 15.0f), new Vector3(2.7f, 0.35f, 0.3f), matDark, parentTransform, true);
            }
        }

        private static void AdaptSocWestWall(Scene scene)
        {
            var socRoom = scene.GetRootGameObjects()
                .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(t => t.name == "SOC / Operations Center");

            if (socRoom != null)
            {
                var toDestroy = socRoom.Cast<Transform>()
                    .Where(child => (child.name.StartsWith("SOC West Shell") ||
                                     (child.name == "Concrete shell" && child.localPosition.x < -4.0f) ||
                                     ((child.name.StartsWith("Wall liner") || child.name.StartsWith("Wall skirting") || child.name.StartsWith("WallAstra")) &&
                                      child.localPosition.x < -4.0f && child.localPosition.z >= -0.1f && child.localPosition.z <= 2.6f)))
                    .Select(t => t.gameObject)
                    .ToList();

                foreach (var go in toDestroy)
                {
                    UnityEngine.Object.DestroyImmediate(go);
                }

                var matConcrete = LoadMat("Concrete");
                var matDark = LoadMat("Dark Metal");

                // Fully hermetic West wall sealing all 8 meters of depth with zero voids:
                // South segment: from local Z = -4.0 to local Z = 0.0 (length 4.0m, center Z = -2.0m)
                CreateBox("SOC West Shell South", new Vector3(-5.1f, 1.65f, -2.0f), new Vector3(0.4f, 3.3f, 4.0f), matConcrete, socRoom, true);
                // North segment: from local Z = +2.5 to local Z = +4.0 (length 1.5m, center Z = +3.25m)
                CreateBox("SOC West Shell North", new Vector3(-5.1f, 1.65f, 3.25f), new Vector3(0.4f, 3.3f, 1.5f), matConcrete, socRoom, true);
                // Doorway lintel header: from local Z = 0.0 to local Z = +2.5 (length 2.5m, center Z = +1.25m, Y: 3.0..3.35)
                CreateBox("SOC West Shell Lintel", new Vector3(-5.1f, 3.175f, 1.25f), new Vector3(0.4f, 0.35f, 2.5f), matDark, socRoom, true);
            }
        }

        private static void BuildDatasetMonolith(Transform parent, Vector3 pos, Material dark, Material screen, Material amber)
        {
            var cluster = new GameObject("Dataset Monolith Cluster").transform;
            cluster.SetParent(parent, false);
            cluster.position = pos;

            CreateBox("Monolith Cabinet", new Vector3(0, 2.3f, 0), new Vector3(2.6f, 4.6f, 2.4f), dark, cluster, true);

            for (int shelf = 0; shelf < 4; shelf++)
            {
                float y = 0.9f + shelf * 1.0f;
                CreateBox("LED Array 1", new Vector3(-0.7f, y, 1.23f), new Vector3(0.9f, 0.12f, 0.02f), screen, cluster, false);
                CreateBox("LED Array 2", new Vector3(0.7f, y, 1.23f), new Vector3(0.9f, 0.12f, 0.02f), screen, cluster, false);
                CreateBox("Warning Trim", new Vector3(0, y - 0.35f, 1.22f), new Vector3(2.4f, 0.025f, 0.02f), amber, cluster, false);
            }

            CreatePointLight("Rack Glow", cluster, new Vector3(0, 2.2f, 1.6f), new Color(0.15f, 0.65f, 0.85f), 1.0f, 3.5f);
        }

        private static void BuildDatasetTerminalPodium(Transform parent, Vector3 pos, FacilityDoor targetDoor, FacilityDoor armoryDoor, Material metal, Material dark, Material sage, Material screen)
        {
            var podium = new GameObject("DATA-01 Master Compute Workstation").transform;
            podium.SetParent(parent, false);
            podium.position = pos;

            // Desk Frame and Surface
            CreateBox("Podium Base", new Vector3(0, 0.3f, 0), new Vector3(2.6f, 0.6f, 2.2f), metal, podium, true);
            CreateBox("Console Desk", new Vector3(0, 0.95f, 0), new Vector3(1.8f, 0.1f, 0.95f), dark, podium, true);

            // Keyboard & Mouse Controls
            CreateBox("Keyboard Base", new Vector3(0, 1.02f, -0.22f), new Vector3(0.65f, 0.04f, 0.22f), dark, podium, false);
            CreateBox("Keyboard Keybed", new Vector3(0, 1.045f, -0.22f), new Vector3(0.62f, 0.015f, 0.19f), metal, podium, false);
            CreateBox("Mouse Unit", new Vector3(0.45f, 1.02f, -0.20f), new Vector3(0.08f, 0.03f, 0.12f), dark, podium, false);

            // Monitor Chassis with SOLID BoxCollider at eye level for reliable player raycasts
            CreateBox("Monitor Stand", new Vector3(0, 1.15f, 0.18f), new Vector3(0.12f, 0.32f, 0.12f), metal, podium, false);
            CreateBox("Monitor Casing", new Vector3(0, 1.55f, 0.18f), new Vector3(1.15f, 0.65f, 0.09f), dark, podium, true);
            var screenObj = CreateBox("Screen Surface", new Vector3(0, 1.55f, 0.13f), new Vector3(1.05f, 0.55f, 0.018f), screen, podium, false);

            // Prominent physical identification badge mounted directly above the screen
            CreateBox("Badge Backing Plate", new Vector3(0, 1.94f, 0.18f), new Vector3(1.0f, 0.12f, 0.04f), dark, podium, false);
            ScreenText(podium, "DATA-01 // COMPUTE VAULT", new Vector3(-0.45f, 1.98f, 0.155f), 0.0034f, new Color(0.45f, 0.85f, 0.95f));

            // Equipment Tower with Status LED
            CreateBox("Tower Chassis", new Vector3(0.72f, 0.5f, 0), new Vector3(0.28f, 0.7f, 0.6f), dark, podium, true);
            var ledObj = CreateBox("Tower Power LED", new Vector3(0.72f, 0.75f, -0.28f), new Vector3(0.04f, 0.02f, 0.02f), LoadMat("Indicator Amber"), podium, false);
            for (int v = 0; v < 6; v++)
            {
                CreateBox("Tower Vent " + v, new Vector3(0.72f, 0.25f + v * 0.06f, -0.28f), new Vector3(0.18f, 0.02f, 0.01f), metal, podium, false);
            }

            // Shift Notebook from Poly Haven
            PlaceDressing("Shift notebook", podium, new Vector3(-0.55f, 1.01f, -0.15f), 15, "DATA-01 Log Notebook");

            // In-World Screen Text rendered on monitor display
            var title = ScreenText(podium, "DATA-01 // COMPUTE VAULT", new Vector3(-0.48f, 1.77f, 0.12f), 0.0032f, new Color(0.45f, 0.85f, 0.95f));
            var body = ScreenText(podium,
                "HOST: data01.infra.northline [10.17.0.101]\n" +
                "POOL: /dev/nvme0n1 [DATASET FABRIC ONLINE]\n" +
                "GATE: SECTOR 03 AIRLOCK [LOCKED]",
                new Vector3(-0.48f, 1.70f, 0.12f), 0.0025f, new Color(0.65f, 0.85f, 0.88f));
            var status = ScreenText(podium, "[STANDBY · APPROACH TO ENGAGE]", new Vector3(-0.48f, 1.34f, 0.12f), 0.0024f, new Color(1.0f, 0.75f, 0.25f));

            var screenComp = podium.gameObject.AddComponent<ComputerScreen>();
            screenComp.Configure(title, body, status);

            var console = podium.gameObject.AddComponent<DoorTerminalConsole>();
            console.Configure(targetDoor, "data01", "10.17.0.101",
                new[] {
                    "nmap -sS -T4 -p 22,8080,9100 10.17.0.101",
                    "python3 -c \"import socket; s=socket.socket(); s.connect(('10.17.0.101', 8080)); s.send(b'AUTH PAM_OVERRIDE_ROOT\\n')\"",
                    "vault-extract --keys --service armory-access",
                    "facility-bus --target armory-sec02 --auth-token KEY-SEC02-ARMORY --actuate"
                },
                new[] {
                    "[PORT SCAN] 22/tcp OPEN (SSH-8.9) | 8080/tcp OPEN (Vault-API) | 9100/tcp OPEN (Telemetry)",
                    "[EXPLOIT] Buffer integrity check: BYPASSED | Kernel privilege: ROOT_EUID_0",
                    "[EXTRACT] Dumping security keyring...\n>>> EXTRACTED TOKEN: [KEY-SEC02-ARMORY] · Clearance Level 4\n>>> EXTRACTED TOKEN: [AUTH-REACTOR-BYPASS]",
                    "[OK] Armory Security Door: UNLOCKED\n[OK] Sector 03 West Airlock: UNLOCKED"
                },
                "Ключ [KEY-SEC02-ARMORY] получен · Открыты: ШЛЮЗ ЭНЕРГОЦЕНТРА (СЛЕВА) и ОРУЖЕЙНЫЙ ОТСЕК (СПРАВА)",
                "[E] Авторизовать доступ через DATA-01",
                screenObj.GetComponent<Renderer>(), screen, LoadMat("Indicator Green"), DeviceId.Data01,
                statusTxt: status, bodyTxt: body, led: ledObj.GetComponent<Renderer>());

            if (armoryDoor != null) console.AddTargetDoor(armoryDoor);

            // Neat atmospheric blue task light shining from ABOVE the workstation onto desk & keyboard
            BuildOverheadWorkstationLamp(podium, new Vector3(0, 2.75f, 0.12f), new Vector3(80, 0, 0), new Color(0.18f, 0.65f, 1.0f), 4.5f, 4.2f, 65f);
        }

        private static void BuildPowerConsole(Transform parent, Vector3 pos, FacilityDoor targetDoor, Material metal, Material dark, Material amber, Material screen)
        {
            var station = new GameObject("PWR-01 Energy Distribution Workstation").transform;
            station.SetParent(parent, false);
            station.position = pos;
            station.rotation = Quaternion.Euler(0, 180, 0);

            CreateBox("Transformer Cabinet", new Vector3(0, 1.25f, 0), new Vector3(2.4f, 2.5f, 0.9f), dark, station, true);
            CreateBox("Breaker Array", new Vector3(0, 1.5f, -0.46f), new Vector3(2.1f, 1.2f, 0.04f), metal, station, false);

            // Telemetry Monitor Casing with SOLID collider for raycasts
            CreateBox("Telemetry Monitor Casing", new Vector3(0, 1.15f, -0.47f), new Vector3(1.22f, 0.58f, 0.06f), dark, station, true);
            var screenObj = CreateBox("Telemetry Screen", new Vector3(0, 1.15f, -0.49f), new Vector3(1.15f, 0.52f, 0.02f), screen, station, false);

            // Prominent physical identification badge
            CreateBox("Badge Plate", new Vector3(0, 1.88f, -0.47f), new Vector3(1.1f, 0.12f, 0.04f), dark, station, false);
            ScreenText(station, "PWR-01 // SUBSTATION & GRID CONTROL", new Vector3(-0.52f, 1.92f, -0.485f), 0.0033f, new Color(1.0f, 0.75f, 0.25f));

            var ledObj = CreateBox("PWR Status LED", new Vector3(0.52f, 1.15f, -0.495f), new Vector3(0.04f, 0.04f, 0.02f), LoadMat("Indicator Amber"), station, false);

            // In-World Screen Text rendered on telemetry monitor
            var title = ScreenText(station, "PWR-01 // SUBSTATION & GRID CONTROL", new Vector3(-0.55f, 1.37f, -0.505f), 0.0031f, new Color(1.0f, 0.75f, 0.25f));
            var body = ScreenText(station,
                "HOST: pwr01.infra.northline [10.17.0.201]\n" +
                "GRID: REACTOR CORE B [1.21 GW ONLINE]\n" +
                "GATE: SECTOR 04 CRYO GATE [LOCKED]",
                new Vector3(-0.55f, 1.30f, -0.505f), 0.0024f, new Color(0.95f, 0.85f, 0.65f));
            var status = ScreenText(station, "[STANDBY · APPROACH TO ENGAGE]", new Vector3(-0.55f, 0.96f, -0.505f), 0.0024f, new Color(1.0f, 0.75f, 0.25f));

            var screenComp = station.gameObject.AddComponent<ComputerScreen>();
            screenComp.Configure(title, body, status);

            var console = station.gameObject.AddComponent<DoorTerminalConsole>();
            console.Configure(targetDoor, "pwr01", "10.17.0.201",
                new[] {
                    "grid-mon --substation 03 --query-telemetry",
                    "python3 -c \"import struct; p=struct.pack('<QI', 0x7FFF0000, 0x1337); print('INJECTING REG_OVERRIDE')\"",
                    "cryo-gate --unlock --sector 04 --token AUTH-REACTOR-BYPASS"
                },
                new[] {
                    "[BUS] Core B Tokamak: 1.21 GW ACTIVE | Primary interlock: ENGAGED",
                    "[PAYLOAD] Overriding substation interlock register: 0x40003000 -> 0x00000000\n[CRITICAL] Cryogenic cooling line bypass: FORCED",
                    "[OK] Sector 04 Cryogenic Nexus Gate: UNLOCKED\n[OK] Sector 03 Substation: ENERGIZED"
                },
                "Криогенный комплекс разблокирован · Сектор 04 доступен",
                "[E] Запустить подстанцию через PWR-01",
                screenObj.GetComponent<Renderer>(), screen, LoadMat("Indicator Green"), DeviceId.Pwr01,
                statusTxt: status, bodyTxt: body, led: ledObj.GetComponent<Renderer>());

            // Neat atmospheric blue task light shining from ABOVE the workstation onto console & telemetry
            BuildOverheadWorkstationLamp(station, new Vector3(0, 2.65f, -0.65f), new Vector3(80, 0, 0), new Color(0.18f, 0.65f, 1.0f), 4.2f, 4.0f, 65f);
        }

        private static void BuildQuantumTerminal(Transform parent, Vector3 pos, FacilityDoor targetDoor, Material metal, Material dark, Material amber, Material screen)
        {
            var bridgeTerminal = new GameObject("QNT-01 Quantum Relay Workstation").transform;
            bridgeTerminal.SetParent(parent, false);
            bridgeTerminal.localPosition = pos;

            CreateBox("Terminal Pedestal", new Vector3(0, 0.5f, 0), new Vector3(1.5f, 1.0f, 0.9f), dark, bridgeTerminal, true);
            CreateBox("Keyboard Base", new Vector3(0, 1.02f, -0.2f), new Vector3(0.6f, 0.04f, 0.2f), dark, bridgeTerminal, false);
            CreateBox("Keyboard Keybed", new Vector3(0, 1.045f, -0.2f), new Vector3(0.56f, 0.015f, 0.17f), metal, bridgeTerminal, false);

            // Screen Casing with SOLID collider for raycasts
            CreateBox("Terminal Screen Bezel", new Vector3(0, 1.4f, 0.14f), new Vector3(1.22f, 0.68f, 0.08f), dark, bridgeTerminal, true);
            var screenObj = CreateBox("Terminal Screen", new Vector3(0, 1.4f, 0.09f), new Vector3(1.15f, 0.6f, 0.02f), screen, bridgeTerminal, false);

            // Prominent physical identification badge
            CreateBox("Badge Plate", new Vector3(0, 1.80f, 0.14f), new Vector3(1.1f, 0.12f, 0.04f), dark, bridgeTerminal, false);
            ScreenText(bridgeTerminal, "QNT-01 // QUANTUM NEXUS RELAY", new Vector3(-0.52f, 1.84f, 0.115f), 0.0033f, new Color(0.4f, 0.9f, 1.0f));

            var ledObj = CreateBox("QNT Status LED", new Vector3(0.52f, 1.4f, 0.08f), new Vector3(0.04f, 0.04f, 0.02f), LoadMat("Indicator Amber"), bridgeTerminal, false);

            // In-World Screen Text rendered on quantum console
            var title = ScreenText(bridgeTerminal, "QNT-01 // QUANTUM BUS & CORE RELAY", new Vector3(-0.52f, 1.63f, 0.075f), 0.0031f, new Color(0.4f, 0.9f, 1.0f));
            var body = ScreenText(bridgeTerminal,
                "HOST: qnt01.infra.northline [10.17.0.301]\n" +
                "BUS:  QUANTUM FABRIC [NOMINAL]\n" +
                "GATE: EXECUTIVE SHORTCUT [LOCKED]",
                new Vector3(-0.52f, 1.56f, 0.075f), 0.0024f, new Color(0.7f, 0.9f, 1.0f));
            var status = ScreenText(bridgeTerminal, "[STANDBY · APPROACH TO ENGAGE]", new Vector3(-0.52f, 1.20f, 0.075f), 0.0024f, new Color(0.4f, 0.9f, 1.0f));

            var screenComp = bridgeTerminal.gameObject.AddComponent<ComputerScreen>();
            screenComp.Configure(title, body, status);

            var console = bridgeTerminal.gameObject.AddComponent<DoorTerminalConsole>();
            console.Configure(targetDoor, "qnt01", "10.17.0.301",
                new[] {
                    "quantum-ctl --bus-matrix --dump",
                    "qnt-inject --sig 0xCAFEBABE --override-route executive-corridor"
                },
                new[] {
                    "[FABRIC] 64 Qubit entangled register: STABLE | Coherence: 99.8%",
                    "[OVERRIDE] Routing table recalculated: SUPER AI firewall quarantined\n>>> EXTRACTED TOKEN: [SUPER-OVERRIDE-CORE]\n[OK] Executive Shortcut Door: UNLOCKED"
                },
                "Обходной контур активирован · Служебный шлюз в операторскую разблокирован",
                "[E] Активировать обходной контур через QNT-01",
                screenObj.GetComponent<Renderer>(), screen, LoadMat("Indicator Green"), DeviceId.Qnt01,
                statusTxt: status, bodyTxt: body, led: ledObj.GetComponent<Renderer>());

            // Neat atmospheric blue task light shining from ABOVE the workstation onto quantum terminal
            BuildOverheadWorkstationLamp(bridgeTerminal, new Vector3(0, 2.65f, 0.10f), new Vector3(80, 0, 0), new Color(0.18f, 0.65f, 1.0f), 4.5f, 4.2f, 65f);
        }

        private static void BuildArmoredDoorLeaf(
            Transform leaf,
            float totalWidth,
            float totalHeight,
            float thickness,
            float windowCenterY,
            float windowWidth,
            float windowHeight,
            Material panelMat,
            Material glassMat,
            Material frameMat,
            Material hazardMat,
            string prefix)
        {
            float bottomHeight = windowCenterY - windowHeight / 2f;
            float topHeight = totalHeight - (windowCenterY + windowHeight / 2f);
            float sideWidth = (totalWidth - windowWidth) / 2f;

            // Lower solid panel (floor up to window sill)
            CreateBox(prefix + " Lower Panel", new Vector3(0, bottomHeight / 2f, 0), new Vector3(totalWidth, bottomHeight, thickness), panelMat, leaf, true);

            // Upper solid panel (window head up to door top)
            CreateBox(prefix + " Upper Panel", new Vector3(0, totalHeight - topHeight / 2f, 0), new Vector3(totalWidth, topHeight, thickness), panelMat, leaf, true);

            // Left side solid jamb panel (flanking window)
            CreateBox(prefix + " Left Panel", new Vector3(-windowWidth / 2f - sideWidth / 2f, windowCenterY, 0), new Vector3(sideWidth, windowHeight, thickness), panelMat, leaf, true);

            // Right side solid jamb panel (flanking window)
            CreateBox(prefix + " Right Panel", new Vector3(windowWidth / 2f + sideWidth / 2f, windowCenterY, 0), new Vector3(sideWidth, windowHeight, thickness), panelMat, leaf, true);

            // Inspection glass pane sitting in the cutout aperture (solid ballistic glass)
            CreateBox(prefix + " Inspection Glass", new Vector3(0, windowCenterY, 0), new Vector3(windowWidth - 0.02f, windowHeight - 0.02f, 0.035f), glassMat, leaf, true);

            // Protective rubber gasket & metal bezel framing both front and rear faces
            for (int side = -1; side <= 1; side += 2)
            {
                float zBezel = side * (thickness / 2f + 0.012f);
                CreateBox(prefix + " Bezel Top " + side, new Vector3(0, windowCenterY + windowHeight / 2f + 0.025f, zBezel), new Vector3(windowWidth + 0.12f, 0.05f, 0.025f), frameMat, leaf, false);
                CreateBox(prefix + " Bezel Bottom " + side, new Vector3(0, windowCenterY - windowHeight / 2f - 0.025f, zBezel), new Vector3(windowWidth + 0.12f, 0.05f, 0.025f), frameMat, leaf, false);
                CreateBox(prefix + " Bezel Left " + side, new Vector3(-windowWidth / 2f - 0.025f, windowCenterY, zBezel), new Vector3(0.05f, windowHeight + 0.12f, 0.025f), frameMat, leaf, false);
                CreateBox(prefix + " Bezel Right " + side, new Vector3(windowWidth / 2f + 0.025f, windowCenterY, zBezel), new Vector3(0.05f, windowHeight + 0.12f, 0.025f), frameMat, leaf, false);

                // Industrial hazard warning stripe
                CreateBox(prefix + " Hazard Stripe " + side, new Vector3(0, 0.75f, side * (thickness / 2f + 0.01f)), new Vector3(totalWidth * 0.9f, 0.08f, 0.015f), hazardMat, leaf, false);
            }
        }

        private static void BuildOverheadWorkstationLamp(Transform terminalRoot, Vector3 localPos, Vector3 aimAngles, Color lightColor, float intensity = 4.2f, float range = 4.0f, float spotAngle = 65f)
        {
            var dark = LoadMat("Dark Metal");
            var lensMat = LoadMat("Kit M_Light");

            // Sleek horizontal cantilevered lamp hood
            CreateBox("Lamp Hood", localPos, new Vector3(0.85f, 0.08f, 0.18f), dark, terminalRoot, false);
            // Emissive blue LED diffuser on underside
            CreateBox("Lamp Blue Lens", localPos + new Vector3(0, -0.042f, 0), new Vector3(0.75f, 0.015f, 0.12f), lensMat, terminalRoot, false);

            // Heavy industrial stanchions anchored directly into the console/desk chassis (Y=0.95m up to localPos.y)
            float stanchionH = localPos.y - 0.95f;
            float stanchionMidY = 0.95f + stanchionH / 2f;
            CreateBox("Lamp Support Mast L", new Vector3(-0.35f, stanchionMidY, localPos.z + 0.05f), new Vector3(0.04f, stanchionH, 0.04f), dark, terminalRoot, false);
            CreateBox("Lamp Support Mast R", new Vector3(0.35f, stanchionMidY, localPos.z + 0.05f), new Vector3(0.04f, stanchionH, 0.04f), dark, terminalRoot, false);
            // Horizontal cantilever arms supporting the hood
            CreateBox("Lamp Cantilever L", new Vector3(-0.35f, localPos.y, localPos.z + 0.025f), new Vector3(0.035f, 0.04f, 0.10f), dark, terminalRoot, false);
            CreateBox("Lamp Cantilever R", new Vector3(0.35f, localPos.y, localPos.z + 0.025f), new Vector3(0.035f, 0.04f, 0.10f), dark, terminalRoot, false);

            // Neat, atmospheric blue task light shining from ABOVE down onto keyboard and desktop
            var lampLight = CreateSpotLight("Overhead Blue Task Light", terminalRoot, localPos + new Vector3(0, -0.05f, 0), aimAngles, lightColor, intensity, range, spotAngle);
            lampLight.innerSpotAngle = spotAngle * 0.58f;
        }

        private static GameObject CreateBox(string name, Vector3 pos, Vector3 size, Material mat, Transform parent, bool solid = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = size;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.On;
            mr.receiveShadows = true;
            if (!solid)
            {
                var col = go.GetComponent<BoxCollider>();
                if (col != null) UnityEngine.Object.DestroyImmediate(col);
            }
            return go;
        }

        private static Light CreatePointLight(string name, Transform parent, Vector3 localPos, Color color, float intensity, float range)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            return l;
        }

        private static Light CreateSpotLight(string name, Transform parent, Vector3 localPos, Vector3 rotation, Color color, float intensity, float range, float spotAngle)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(rotation);
            var l = go.AddComponent<Light>();
            l.type = LightType.Spot;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            l.spotAngle = spotAngle;
            l.innerSpotAngle = spotAngle * 0.6f;
            return l;
        }

        private static TextMesh ScreenText(Transform parent, string text, Vector3 position, float size, Color color, float yaw = 0, TextAnchor anchor = TextAnchor.UpperLeft)
        {
            var go = new GameObject("Screen text");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            var label = go.AddComponent<TextMesh>();
            label.text = text;
            label.fontSize = 56;
            label.characterSize = size;
            label.anchor = anchor;
            label.lineSpacing = 1.15f;
            label.color = color;
            label.font = AssetDatabase.LoadAssetAtPath<Font>(FacilityBuilder.Content + "/Fonts/RobotoMono-Regular.ttf");
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = label.font.material;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            go.AddComponent<WorldTextDepth>().Configure(Shader.Find("HOSTFALL/World Text"));
            return label;
        }

        private static void CreateSignboard(Transform parent, string title, string code, Vector3 pos, float yaw, Color textColor)
        {
            var sign = new GameObject("Signboard - " + code).transform;
            sign.SetParent(parent, false);
            sign.position = pos;
            sign.rotation = Quaternion.Euler(0, yaw, 0);

            var dark = LoadMat("Dark Metal");
            var amber = LoadMat("Indicator Amber");

            CreateBox("Sign Plate", Vector3.zero, new Vector3(3.2f, 0.48f, 0.08f), dark, sign, false);
            CreateBox("Sign Border Top", new Vector3(0, 0.23f, 0.045f), new Vector3(3.2f, 0.02f, 0.02f), amber, sign, false);
            CreateBox("Sign Border Bottom", new Vector3(0, -0.23f, 0.045f), new Vector3(3.2f, 0.02f, 0.02f), amber, sign, false);

            ScreenText(sign, title + " // " + code, new Vector3(0, 0.02f, -0.05f), 0.0034f, textColor, 0, TextAnchor.MiddleCenter);
            CreatePointLight("Sign Illum", sign, new Vector3(0, -0.25f, -0.2f), textColor, 0.8f, 2.5f);
        }

        private static void CreateReflectionProbe(string name, Vector3 pos, Vector3 size, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var probe = go.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
            probe.boxProjection = true;
            probe.size = size;
            probe.intensity = 1.0f;
        }

        private static FacilityDoor BuildArmoryRoom(Transform parent, Material metal, Material dark, Material amber, Material glass, Material screen)
        {
            var armory = new GameObject("Sector 02 - Armory [SEC-02]").transform;
            armory.SetParent(parent, false);

            var matFloor = LoadMat("Floor Panels");
            var matRed = LoadMat("Emergency Red");

            // Floor & Ceiling (X: 7.0..14.5, Z: 21.0..31.0)
            CreateBox("Armory Floor Base", new Vector3(10.75f, -0.1f, 26.0f), new Vector3(7.5f, 0.2f, 10.0f), matFloor, armory, true);
            CreateBox("Armory Runner Slab", new Vector3(10.75f, 0.02f, 26.0f), new Vector3(1.8f, 0.04f, 8.5f), dark, armory, false);
            CreateBox("Armory Ceiling", new Vector3(10.75f, 4.55f, 26.0f), new Vector3(7.5f, 0.2f, 10.0f), dark, armory, true);

            // East Wall (X = 14.5)
            CreateBox("Armory East Monolithic Wall", new Vector3(14.75f, 2.25f, 26.0f), new Vector3(0.5f, 4.55f, 10.5f), LoadMat("Concrete"), armory, true);
            ModularKit.Place("WallAstra_Straight", armory, new Vector3(14.5f, 0, 23.5f), new Vector3(0.5f, 4.5f, 5.0f), 180, true);
            ModularKit.Place("WallAstra_Straight", armory, new Vector3(14.5f, 0, 28.5f), new Vector3(0.5f, 4.5f, 5.0f), 180, true);

            // South Wall (Z = 21.0)
            CreateBox("Armory South Monolithic Wall", new Vector3(10.75f, 2.25f, 20.75f), new Vector3(8.0f, 4.55f, 0.5f), LoadMat("Concrete"), armory, true);
            ModularKit.Place("WallAstra_Straight", armory, new Vector3(10.75f, 0, 21.0f), new Vector3(7.5f, 4.5f, 0.5f), -90, true);

            // North Wall (Z = 31.0)
            CreateBox("Armory North Monolithic Wall", new Vector3(10.75f, 2.25f, 31.25f), new Vector3(8.0f, 4.55f, 0.5f), LoadMat("Concrete"), armory, true);
            ModularKit.Place("WallAstra_Straight", armory, new Vector3(10.75f, 0, 31.0f), new Vector3(7.5f, 4.5f, 0.5f), 90, true);

            // West Wall Flanking Doorway (X = 7.0)
            CreateBox("Armory West Wall South", new Vector3(7.0f, 2.25f, 22.75f), new Vector3(0.4f, 4.5f, 3.5f), dark, armory, true);
            CreateBox("Armory West Wall North", new Vector3(7.0f, 2.25f, 29.25f), new Vector3(0.4f, 4.5f, 3.5f), dark, armory, true);
            CreateBox("Armory West Doorway Lintel", new Vector3(7.0f, 3.85f, 26.0f), new Vector3(0.4f, 1.3f, 3.0f), dark, armory, true);

            // Sliding Armory Security Door
            var armoryDoorGroup = new GameObject("Armory Security Door").transform;
            armoryDoorGroup.SetParent(armory, false);
            armoryDoorGroup.position = new Vector3(7.0f, 0, 26.0f);
            armoryDoorGroup.rotation = Quaternion.Euler(0, 90, 0); // Door width along world Z

            CreateBox("Armory Left Jamb", new Vector3(-1.45f, 1.6f, 0), new Vector3(0.3f, 3.2f, 0.4f), dark, armoryDoorGroup, true);
            CreateBox("Armory Right Jamb", new Vector3(1.45f, 1.6f, 0), new Vector3(0.3f, 3.2f, 0.4f), dark, armoryDoorGroup, true);
            CreateBox("Armory Lintel", new Vector3(0, 3.35f, 0), new Vector3(3.2f, 0.3f, 0.4f), dark, armoryDoorGroup, true);

            var armoryLeaf = new GameObject("Armory Sliding Leaf").transform;
            armoryLeaf.SetParent(armoryDoorGroup, false);
            BuildArmoredDoorLeaf(armoryLeaf, 2.65f, 3.1f, 0.2f, 1.8f, 1.28f, 0.45f, dark, glass, metal, amber, "Armory");

            var armoryDoor = armoryDoorGroup.gameObject.AddComponent<FacilityDoor>();
            armoryDoor.Configure(armoryLeaf, DoorAccessCondition.ManualUnlockOnly,
                "Оружейный отсек заблокирован · требуется ключ [KEY-SEC02-ARMORY]",
                "[E] Открыть оружейный отсек",
                "[E] Закрыть оружейный отсек",
                true, Vector3.up, 3.2f, 1.8f);

            // Overhead Signboard over Armory Doorway
            CreateSignboard(armory, "ОРУЖЕЙНЫЙ ОТСЕК // ARMORY", "SEC-02", new Vector3(6.78f, 3.45f, 26.0f), 90, new Color(1.0f, 0.5f, 0.15f));

            // Weapon Lockers along North wall
            for (int i = 0; i < 4; i++)
            {
                float x = 8.8f + i * 1.3f;
                CreateBox("Weapon Locker " + i, new Vector3(x, 1.3f, 30.6f), new Vector3(1.0f, 2.6f, 0.6f), dark, armory, true);
                CreateBox("Locker Vent " + i, new Vector3(x, 2.2f, 30.28f), new Vector3(0.7f, 0.4f, 0.04f), metal, armory, false);
                CreateBox("Locker Handle " + i, new Vector3(x + 0.35f, 1.3f, 30.28f), new Vector3(0.04f, 0.25f, 0.04f), amber, armory, false);
            }

            // Supply Crates and High-Capacity Barrels along walls
            ModularKit.Place("Prop_Barrel_Large", armory, new Vector3(13.5f, 0, 23.0f), new Vector3(1.1f, 1.4f, 1.1f), 0, true);
            ModularKit.Place("Prop_Barrel_Large", armory, new Vector3(13.5f, 0, 24.5f), new Vector3(1.1f, 1.4f, 1.1f), 20, true);
            ModularKit.Place("Prop_Crate3", armory, new Vector3(13.2f, 0, 28.5f), new Vector3(0.9f, 0.9f, 0.9f), 15, true);
            ModularKit.Place("Prop_Crate4", armory, new Vector3(13.2f, 0, 29.5f), new Vector3(0.85f, 0.85f, 0.85f), -20, true);

            // Central Illuminated Weapon Display Pedestal with NORTHLINE ARC-12 Shotgun
            var pedestalObj = new GameObject("Weapon Display Pedestal");
            pedestalObj.transform.SetParent(armory, false);
            pedestalObj.transform.position = new Vector3(10.75f, 0, 26.0f);

            CreateBox("Pedestal Base Octagon", new Vector3(0, 0.45f, 0), new Vector3(1.6f, 0.9f, 1.2f), dark, pedestalObj.transform, true);
            CreateBox("Pedestal Top Surface", new Vector3(0, 0.91f, 0), new Vector3(1.4f, 0.04f, 1.0f), metal, pedestalObj.transform, false);
            CreateBox("Pedestal Emissive Trim", new Vector3(0, 0.92f, 0), new Vector3(1.44f, 0.015f, 1.04f), amber, pedestalObj.transform, false);

            CreateSpotLight("Weapon Pedestal Spotlight", pedestalObj.transform, new Vector3(0, 3.4f, 0), new Vector3(90, 0, 0), new Color(1.0f, 0.88f, 0.65f), 3.0f, 4.5f, 45f);

            // Interactive Pickup Item: NORTHLINE ARC-12 Shotgun Prop
            var pickupProp = new GameObject("Weapon Pickup - NORTHLINE ARC-12");
            pickupProp.transform.SetParent(pedestalObj.transform, false);
            pickupProp.transform.localPosition = new Vector3(0, 1.08f, 0);
            pickupProp.transform.localRotation = Quaternion.Euler(0, 35f, 0);

            CreateBox("Prop Receiver", new Vector3(0, 0, 0.05f), new Vector3(0.065f, 0.085f, 0.38f), dark, pickupProp.transform, false);
            CreateBox("Prop Heavy Barrel", new Vector3(0, 0.02f, 0.38f), new Vector3(0.045f, 0.045f, 0.34f), metal, pickupProp.transform, false);
            CreateBox("Prop Barrel Shroud", new Vector3(0, 0.02f, 0.30f), new Vector3(0.058f, 0.058f, 0.18f), dark, pickupProp.transform, false);
            CreateBox("Prop Arc Coil Front", new Vector3(0, 0.02f, 0.36f), new Vector3(0.062f, 0.062f, 0.025f), LoadMat("Indicator Green"), pickupProp.transform, false);
            CreateBox("Prop Arc Coil Rear", new Vector3(0, 0.02f, 0.25f), new Vector3(0.062f, 0.062f, 0.025f), LoadMat("Indicator Green"), pickupProp.transform, false);
            CreateBox("Prop Pistol Grip", new Vector3(0, -0.08f, -0.05f), new Vector3(0.045f, 0.12f, 0.055f), dark, pickupProp.transform, false);
            CreateBox("Prop Tactical Stock", new Vector3(0, -0.02f, -0.22f), new Vector3(0.055f, 0.075f, 0.22f), dark, pickupProp.transform, false);
            CreateBox("Prop Sight Reticle", new Vector3(0, 0.085f, 0.08f), new Vector3(0.035f, 0.035f, 0.005f), matRed, pickupProp.transform, false);

            var col = pickupProp.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0, 0.1f);
            col.size = new Vector3(0.7f, 0.45f, 0.9f);

            pickupProp.AddComponent<Hostfall.Player.WeaponPickup>();

            ScreenText(pedestalObj.transform, "ИМПУЛЬСНЫЙ ДРОБОВИК NORTHLINE ARC-12\n[E] ВЗЯТЬ ОРУЖИЕ", new Vector3(0, 1.55f, 0), 0.0028f, new Color(1.0f, 0.85f, 0.35f), 0, TextAnchor.MiddleCenter);

            // Bright Security Lighting in Armory
            CreateSpotLight("Armory Ceiling Troffer 1", armory, new Vector3(10.75f, 4.4f, 24.5f), new Vector3(90, 0, 0), new Color(1.0f, 0.96f, 0.88f), 6.0f, 12f, 100f);
            CreateSpotLight("Armory Ceiling Troffer 2", armory, new Vector3(10.75f, 4.4f, 29.5f), new Vector3(90, 0, 0), new Color(1.0f, 0.96f, 0.88f), 6.0f, 12f, 100f);
            CreatePointLight("Armory Ambient Fill", armory, new Vector3(10.75f, 3.5f, 27.0f), new Color(0.98f, 0.90f, 0.75f), 4.0f, 11f);
            CreatePointLight("Armory Red Alert Light 1", armory, new Vector3(10.75f, 3.8f, 23.5f), new Color(1.0f, 0.25f, 0.08f), 2.5f, 7.0f);
            CreatePointLight("Armory Red Alert Light 2", armory, new Vector3(10.75f, 3.8f, 28.5f), new Color(1.0f, 0.25f, 0.08f), 2.5f, 7.0f);

            CreateReflectionProbe("Reflection Probe - Armory", new Vector3(10.75f, 2.0f, 26.0f), new Vector3(8.5f, 5.0f, 11.0f), armory);

            return armoryDoor;
        }

        private static void BuildPatrolRobot(Transform parent, string id, string modelName,
            Vector3 spawnPos, float spawnYaw,
            Vector3 patrolA, Vector3 patrolB,
            Vector2 boundsX, Vector2 boundsZ,
            Material metal, Material dark, Material amber)
        {
            var robotObj = new GameObject("Patrol Robot: " + id);
            robotObj.transform.SetParent(parent, false);
            robotObj.transform.position = spawnPos;
            robotObj.transform.rotation = Quaternion.Euler(0, spawnYaw, 0);

            var shape = robotObj.AddComponent<CapsuleCollider>();
            shape.center = new Vector3(0, 0.52f, 0);
            shape.height = 1f;
            shape.radius = 0.43f;

            var body = robotObj.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            var animator = RobotArt.Place(modelName, robotObj.transform);
            animator.Rebind();
            animator.Play("Idle", 0, 0);
            animator.Update(0);

            var anchor = animator.GetComponentsInChildren<Transform>().Single(t => t.name == "Body");
            var shell = animator.GetComponentsInChildren<SkinnedMeshRenderer>().First(r => r.name.EndsWith("_Body"));

            var baked = new Mesh();
            shell.BakeMesh(baked, true);
            var shellBounds = new Bounds(shell.transform.TransformPoint(baked.vertices[0]), Vector3.zero);
            foreach (var vertex in baked.vertices) shellBounds.Encapsulate(shell.transform.TransformPoint(vertex));
            UnityEngine.Object.DestroyImmediate(baked);

            var sensor = new GameObject("Optical sensor").transform;
            sensor.SetParent(anchor, true);
            sensor.position = new Vector3(shellBounds.center.x, shellBounds.max.y + 0.025f, shellBounds.center.z);
            sensor.rotation = robotObj.transform.rotation;

            CreateBox("Sensor mounting plate", new Vector3(0, -0.055f, 0), new Vector3(0.22f, 0.06f, 0.17f), metal, sensor, false);
            CreateBox("Sensor housing", Vector3.zero, new Vector3(0.18f, 0.1f, 0.12f), dark, sensor, false);
            CreateBox("Optical lens", new Vector3(0, 0, 0.07f), new Vector3(0.1f, 0.035f, 0.015f), amber, sensor, false);

            var spotGo = new GameObject("Patrol Searchlight");
            spotGo.transform.SetParent(sensor, false);
            spotGo.transform.localPosition = new Vector3(0, 0, 0.12f);
            spotGo.transform.localRotation = Quaternion.Euler(15f, 0, 0);
            var spot = spotGo.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.color = new Color(1.0f, 0.45f, 0.15f);
            spot.intensity = 2.4f;
            spot.range = 8.5f;
            spot.spotAngle = 65f;
            spot.innerSpotAngle = 35f;

            var watcher = robotObj.AddComponent<WatcherController>();
            watcher.Configure(sensor, spot, animator, id, patrolA, patrolB, boundsX, boundsZ);
        }

        private static void PlaceDressing(string propName, Transform parent, Vector3 localPos, float yaw, string instanceName = null)
        {
            var prefabPath = FacilityBuilder.Content + "/Prefabs/Engineering/" + propName + ".prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) return;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = string.IsNullOrEmpty(instanceName) ? propName : instanceName;
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = localPos;
            instance.transform.localRotation = Quaternion.Euler(0, yaw, 0);
        }

        private static GameObject BuildWeaponRack(Transform parent, Vector3 pos, float yaw, string label)
        {
            var rackObj = new GameObject("Security Weapon Rack - " + label);
            rackObj.transform.SetParent(parent, false);
            rackObj.transform.position = pos;
            rackObj.transform.rotation = Quaternion.Euler(0, yaw, 0);

            var matDark = LoadMat("Dark Metal");
            var matMetal = LoadMat("Industrial Metal");
            var matAmber = LoadMat("Indicator Amber");
            var matGreen = LoadMat("Indicator Green");
            var matRed = LoadMat("Emergency Red");

            // Wall mounting backplate
            CreateBox("Rack Backing Plate", new Vector3(0, 0, 0), new Vector3(1.2f, 0.8f, 0.08f), matDark, rackObj.transform, true);
            CreateBox("Rack Steel Trim", new Vector3(0, 0, 0.045f), new Vector3(1.24f, 0.84f, 0.02f), matMetal, rackObj.transform, false);
            CreateBox("Rack Warning Header", new Vector3(0, 0.36f, 0.055f), new Vector3(1.16f, 0.06f, 0.015f), matAmber, rackObj.transform, false);
            CreateBox("Rack Warning Footer", new Vector3(0, -0.36f, 0.055f), new Vector3(1.16f, 0.06f, 0.015f), matAmber, rackObj.transform, false);

            // Steel weapon brackets
            CreateBox("Bracket Left", new Vector3(-0.25f, -0.06f, 0.12f), new Vector3(0.04f, 0.12f, 0.16f), matMetal, rackObj.transform, false);
            CreateBox("Bracket Right", new Vector3(0.25f, -0.06f, 0.12f), new Vector3(0.04f, 0.12f, 0.16f), matMetal, rackObj.transform, false);

            // Dedicated illumination spotlight on the weapon
            CreateSpotLight("Weapon Rack Spot", rackObj.transform, new Vector3(0, 0.65f, 0.45f), new Vector3(55, 0, 0), new Color(1.0f, 0.90f, 0.70f), 4.5f, 3.5f, 65f);

            // Interactive Weapon Pickup item
            var pickupProp = new GameObject("Weapon Pickup - " + label);
            pickupProp.transform.SetParent(rackObj.transform, false);
            pickupProp.transform.localPosition = new Vector3(0, -0.02f, 0.14f);
            pickupProp.transform.localRotation = Quaternion.identity;

            // Shotgun mesh parts on the rack
            CreateBox("Prop Receiver", new Vector3(0, 0, 0.05f), new Vector3(0.065f, 0.085f, 0.38f), matDark, pickupProp.transform, false);
            CreateBox("Prop Heavy Barrel", new Vector3(0, 0.02f, 0.38f), new Vector3(0.045f, 0.045f, 0.34f), matMetal, pickupProp.transform, false);
            CreateBox("Prop Barrel Shroud", new Vector3(0, 0.02f, 0.30f), new Vector3(0.058f, 0.058f, 0.18f), matDark, pickupProp.transform, false);
            CreateBox("Prop Arc Coil Front", new Vector3(0, 0.02f, 0.36f), new Vector3(0.062f, 0.062f, 0.025f), matGreen, pickupProp.transform, false);
            CreateBox("Prop Arc Coil Rear", new Vector3(0, 0.02f, 0.25f), new Vector3(0.062f, 0.062f, 0.025f), matGreen, pickupProp.transform, false);
            CreateBox("Prop Pistol Grip", new Vector3(0, -0.08f, -0.05f), new Vector3(0.045f, 0.12f, 0.055f), matDark, pickupProp.transform, false);
            CreateBox("Prop Tactical Stock", new Vector3(0, -0.02f, -0.22f), new Vector3(0.055f, 0.075f, 0.22f), matDark, pickupProp.transform, false);
            CreateBox("Prop Sight Reticle", new Vector3(0, 0.085f, 0.08f), new Vector3(0.035f, 0.035f, 0.005f), matRed, pickupProp.transform, false);

            var col = pickupProp.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0, 0.1f);
            col.size = new Vector3(0.8f, 0.6f, 0.9f);

            pickupProp.AddComponent<Hostfall.Player.WeaponPickup>();

            // In-world marker text
            ScreenText(rackObj.transform, "ОРУЖЕЙНЫЙ ПОСТ // SECURITY RACK\n[E] ВЗЯТЬ ДРОБОВИК ARC-12", new Vector3(0, 0.22f, 0.06f), 0.0026f, new Color(1.0f, 0.85f, 0.35f), 0, TextAnchor.MiddleCenter);

            return rackObj;
        }

        private static Material LoadMat(string name)
        {
            var path = FacilityBuilder.Content + "/Materials/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                path = FacilityBuilder.Content + "/Materials/Kit " + name + ".mat";
                mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            }
            return mat;
        }
    }
}
