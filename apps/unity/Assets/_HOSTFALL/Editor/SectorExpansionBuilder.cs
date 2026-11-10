using System;
using System.IO;
using System.Linq;
using Hostfall.Core;
using Hostfall.Devices;
using Hostfall.Interaction;
using Hostfall.Player;
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

            // Adapt SOC east wall to create opening for Security Shortcut Door
            AdaptSocEastWall(scene);

            // Adapt SOC south wall to create opening for Engineer Shelter & Service Tunnel
            AdaptSocSouthWall(scene);

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
            var matRed = LoadMat("Emergency Red");

            // ReflectionProbe for SOC and Server Room
            CreateReflectionProbe("Reflection Probe - SOC & Server Room", new Vector3(0, 2.0f, 6.0f), new Vector3(14.0f, 5.0f, 22.0f), root.transform);

            // =========================================================================
            // 0. ENGINEER SHELTER & SERVICE PASSAGE (Z = -19.0 .. -7.0) — Safe Home Base
            // =========================================================================
            BuildEngineerShelter(root.transform, matMetal, matDark, matConcrete, matFloor, matAmber, matGlass, matScreen, matSage, matGreen, matRed);

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

            // =========================================================================
            // ELEVATED CATWALK MEZZANINE (Y = 3.2m) & RETRACTABLE BRIDGE (P03)
            // =========================================================================
            var catwalk = new GameObject("Catwalk Mezzanine (Y=3.2)").transform;
            catwalk.SetParent(sector02, false);

            // 1. East Flank Catwalk (X = 4.6, Y = 3.2m, Z: 20.0..35.0)
            for (float z = 20.0f; z <= 35.0f; z += 3.0f)
            {
                ModularKit.Place("Platform_Metal", catwalk, new Vector3(4.6f, 3.2f, z), new Vector3(2.2f, 0.15f, 3.0f), 0, true);
                // Aisle safety railing on West side
                ModularKit.Place("Platform_Rails_4", catwalk, new Vector3(3.45f, 3.26f, z), new Vector3(0.08f, 1.1f, 3.0f), 0, true);
                // Wall gap safety railing on East side
                ModularKit.Place("Platform_Rails_4", catwalk, new Vector3(5.75f, 3.26f, z), new Vector3(0.08f, 1.1f, 3.0f), 180, true);
            }
            // North end safety railing
            ModularKit.Place("Platform_Rails_4", catwalk, new Vector3(4.6f, 3.26f, 36.5f), new Vector3(2.2f, 1.1f, 0.08f), 90, true);

            // Structural vertical steel support pillars under East catwalk (Y=0 to Y=3.2)
            for (int z = 21; z <= 35; z += 4)
            {
                ModularKit.Place("Column_MetalSupport", catwalk, new Vector3(3.55f, 0, z), new Vector3(0.35f, 3.2f, 0.35f), 0, true);
                ModularKit.Place("Column_MetalSupport", catwalk, new Vector3(5.65f, 0, z), new Vector3(0.35f, 3.2f, 0.35f), 0, true);
            }

            // Modular Stairs leading to upper East Catwalk (ascending from ground Y=0 at Z=15.8 to Y=3.2 at Z=19.6)
            ModularKit.Place("Platform_Stairs_4", catwalk, new Vector3(4.6f, 0, 17.7f), new Vector3(1.8f, 3.2f, 3.8f), 0, true);

            // Focused architectural stair illumination
            CreateSpotLight("Catwalk Stairs Spotlight", sector02, new Vector3(4.6f, 5.2f, 17.7f), new Vector3(68, 0, 0), new Color(1.0f, 0.92f, 0.82f), 5.5f, 14f, 85f);

            // 2. West Flank Catwalk Gallery (X = -4.6, Y = 3.2m, Z: 24.0..30.0)
            for (float z = 24.0f; z <= 30.0f; z += 3.0f)
            {
                ModularKit.Place("Platform_Metal", catwalk, new Vector3(-4.6f, 3.2f, z), new Vector3(2.2f, 0.15f, 3.0f), 0, true);
                // Aisle safety railing on East side (except at bridge opening Z=27.0)
                if (Mathf.Abs(z - 27.0f) > 1.2f)
                {
                    ModularKit.Place("Platform_Rails_4", catwalk, new Vector3(-3.45f, 3.26f, z), new Vector3(0.08f, 1.1f, 3.0f), 0, true);
                }
                // Wall gap safety railing on West side
                ModularKit.Place("Platform_Rails_4", catwalk, new Vector3(-5.75f, 3.26f, z), new Vector3(0.08f, 1.1f, 3.0f), 180, true);
            }
            // Modular Stairs leading from West Catwalk down to ground floor (descending from Y=3.2 at Z=22.5 to Y=0 at Z=18.7)
            ModularKit.Place("Platform_Stairs_4", catwalk, new Vector3(-4.6f, 0, 20.6f), new Vector3(1.8f, 3.2f, 3.8f), 180, true);
            CreateSpotLight("West Catwalk Stairs Spotlight", sector02, new Vector3(-4.6f, 5.2f, 20.6f), new Vector3(68, 0, 0), new Color(1.0f, 0.92f, 0.82f), 5.5f, 14f, 85f);

            // West catwalk north end railing
            ModularKit.Place("Platform_Rails_4", catwalk, new Vector3(-4.6f, 3.26f, 31.5f), new Vector3(2.2f, 1.1f, 0.08f), 90, true);

            // Structural vertical steel support pillars under West catwalk
            ModularKit.Place("Column_MetalSupport", catwalk, new Vector3(-3.55f, 0, 24.5f), new Vector3(0.35f, 3.2f, 0.35f), 0, true);
            ModularKit.Place("Column_MetalSupport", catwalk, new Vector3(-5.65f, 0, 24.5f), new Vector3(0.35f, 3.2f, 0.35f), 0, true);
            ModularKit.Place("Column_MetalSupport", catwalk, new Vector3(-3.55f, 0, 29.5f), new Vector3(0.35f, 3.2f, 0.35f), 0, true);
            ModularKit.Place("Column_MetalSupport", catwalk, new Vector3(-5.65f, 0, 29.5f), new Vector3(0.35f, 3.2f, 0.35f), 0, true);

            // 3. P03 Retractable Contact Bridge Mechanism (Z = 27.0, spanning X: -4.6 to +4.6 at Y = 3.2m)
            var bridgeRoot = new GameObject("Retractable Bridge P03").transform;
            bridgeRoot.SetParent(catwalk, false);
            bridgeRoot.position = new Vector3(0, 3.2f, 27.0f);

            // Fixed bridge wings
            CreateBox("Bridge Fixed Wing East", new Vector3(2.7f, 0, 0), new Vector3(1.6f, 0.15f, 2.0f), matMetal, bridgeRoot, true);
            CreateBox("Bridge Fixed Wing West", new Vector3(-2.7f, 0, 0), new Vector3(1.6f, 0.15f, 2.0f), matMetal, bridgeRoot, true);

            // Sliding Center Leaf
            var slidingLeaf = new GameObject("Sliding Bridge Leaf").transform;
            slidingLeaf.SetParent(bridgeRoot, false);
            CreateBox("Leaf Walkway Slab", Vector3.zero, new Vector3(3.8f, 0.14f, 1.8f), matMetal, slidingLeaf, true);
            CreateBox("Leaf Railing South", new Vector3(0, 0.6f, -0.92f), new Vector3(3.8f, 1.1f, 0.06f), matDark, slidingLeaf, true);
            CreateBox("Leaf Railing North", new Vector3(0, 0.6f, 0.92f), new Vector3(3.8f, 1.1f, 0.06f), matDark, slidingLeaf, true);

            // PDU Breaker Switch mounted on East bridge portal post
            var breakerConsole = new GameObject("PDU-SEC02 Breaker Console");
            breakerConsole.transform.SetParent(bridgeRoot, false);
            breakerConsole.transform.position = new Vector3(3.45f, 4.3f, 26.0f);
            breakerConsole.transform.rotation = Quaternion.Euler(0, -90, 0);

            CreateBox("Breaker Housing", Vector3.zero, new Vector3(0.45f, 0.7f, 0.18f), matDark, breakerConsole.transform, true);
            CreateBox("Breaker Faceplate", new Vector3(0, 0.05f, 0.09f), new Vector3(0.38f, 0.45f, 0.02f), matMetal, breakerConsole.transform, false);
            var breakerSwitch = CreateBox("Knife Switch Lever", new Vector3(0, 0.05f, 0.12f), new Vector3(0.08f, 0.22f, 0.06f), matAmber, breakerConsole.transform, false);
            var breakerLed = CreateBox("Breaker Status LED", new Vector3(0, 0.24f, 0.095f), new Vector3(0.12f, 0.04f, 0.02f), matAmber, breakerConsole.transform, false);
            ScreenText(breakerConsole.transform, "PDU-SEC02\nBRIDGE PWR", new Vector3(-0.16f, -0.15f, 0.10f), 0.0018f, new Color(0.45f, 0.85f, 0.95f));

            // Bridge warning/guide lights
            var bridgeLightEast = CreatePointLight("Bridge Light East", bridgeRoot, new Vector3(1.8f, 1.4f, 0), new Color(1.0f, 0.65f, 0.15f), 2.5f, 6.0f);
            var bridgeLightWest = CreatePointLight("Bridge Light West", bridgeRoot, new Vector3(-1.8f, 1.4f, 0), new Color(1.0f, 0.65f, 0.15f), 2.5f, 6.0f);

            var bridgeComp = breakerConsole.AddComponent<RetractableBridgeController>();
            bridgeComp.Configure(slidingLeaf, new Vector3(2.4f, 0, 0), Vector3.zero,
                new[] { bridgeLightEast.GetComponent<Light>(), bridgeLightWest.GetComponent<Light>() },
                breakerLed.GetComponent<Renderer>(), matAmber, matGreen);

            // Elevated Observation & Diagnostics Post at the North end of the Catwalk (Z = 35.8, Y = 3.2m)
            CreateBox("Observation Console Base", new Vector3(4.6f, 3.65f, 35.8f), new Vector3(1.4f, 0.9f, 0.5f), matDark, catwalk, true);
            CreateBox("Observation Console Screen", new Vector3(4.6f, 4.35f, 35.95f), new Vector3(1.2f, 0.45f, 0.04f), matScreen, catwalk, false);
            ScreenText(catwalk, "SEC-02 MEZZANINE // OBSERVATION", new Vector3(4.1f, 4.55f, 35.92f), 0.0028f, new Color(0.45f, 0.85f, 1.0f));
            CreatePointLight("Observation Task Light", catwalk, new Vector3(4.6f, 5.0f, 35.5f), new Color(0.45f, 0.85f, 1.0f), 2.5f, 5.0f);

            // Data Cartridge Item (P04) on Mezzanine Observation Desk
            var cartridgeObj = new GameObject("Data Cartridge NL-CART-044");
            cartridgeObj.transform.SetParent(catwalk, false);
            cartridgeObj.transform.position = new Vector3(4.6f, 4.15f, 35.6f);

            CreateBox("Cartridge Body", Vector3.zero, new Vector3(0.22f, 0.04f, 0.32f), matDark, cartridgeObj.transform, true);
            CreateBox("Cartridge Grip", new Vector3(0, 0.01f, -0.11f), new Vector3(0.18f, 0.03f, 0.06f), matMetal, cartridgeObj.transform, false);
            CreateBox("Cartridge Label", new Vector3(0, 0.022f, 0.03f), new Vector3(0.16f, 0.01f, 0.16f), matScreen, cartridgeObj.transform, false);
            ScreenText(cartridgeObj.transform, "NL-CART-044", new Vector3(-0.06f, 0.03f, 0.05f), 0.0016f, new Color(0.45f, 0.85f, 0.95f), 90);
            cartridgeObj.AddComponent<DataCartridgeItem>();

            // Ammo Crate on Mezzanine observation post
            BuildAmmoBoxPickup(catwalk, new Vector3(3.8f, 3.2f, 35.6f), 12, matMetal, matDark, matAmber);

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

            // Aerial Surveillance Drone: E03 LENS-DRONE (hovering under ceiling at Y=3.9m)
            BuildLensDrone(sector02, matMetal, matDark, matAmber);

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

            // P06 Coolant Loop Valve (Mounted solidly onto the SW reactor cooling pipe column)
            BuildCoolingValve(sector03, new Vector3(-18.7f, 1.35f, 25.0f), matMetal, matDark, matAmber, matGreen);

            // P05 UPS Power Budget Router (Battery Gallery)
            BuildUpsPowerStation(sector03, new Vector3(-13.5f, 0, 20.5f), matMetal, matDark, matAmber, matGreen, matRed);

            // Ammo Crate in Battery Gallery
            BuildAmmoBoxPickup(sector03, new Vector3(-14.2f, 0, 22.0f), 12, matMetal, matDark, matAmber);

            // E06 Heavy Tracked Sentry guarding Reactor Hall
            BuildTrackedSentry(sector03, new Vector3(-21.0f, 0, 24.0f), new Vector3(-21.0f, 0, 24.0f), new Vector3(-12.0f, 0, 32.0f), matMetal, matDark, matAmber, matRed);

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
            // North Boundary Monolithic Walls flanking the Grand Logistics Gate
            CreateBox("Sector04 North Wall West Wing", new Vector3(-6.25f, 3.775f, 62.25f), new Vector3(8.0f, 7.55f, 0.5f), matConcrete, sector04, true);
            CreateBox("Sector04 North Wall East Wing", new Vector3(6.25f, 3.775f, 62.25f), new Vector3(8.0f, 7.55f, 0.5f), matConcrete, sector04, true);
            CreateBox("Sector04 North Doorway Lintel", new Vector3(0, 5.875f, 62.25f), new Vector3(4.5f, 3.35f, 0.5f), matDark, sector04, true);

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
            // Decorative wall panels flanking gate
            ModularKit.Place("WallAstra_Straight", sector04, new Vector3(-6.25f, 0, 62.0f), new Vector3(7.5f, 7.5f, 0.5f), 90, true);
            ModularKit.Place("WallAstra_Straight", sector04, new Vector3(6.25f, 0, 62.0f), new Vector3(7.5f, 7.5f, 0.5f), 90, true);

            // Grand Logistics Cargo Gate (Z = 62.0)
            var logisticsGateGroup = new GameObject("Logistics Gate (Z=62)").transform;
            logisticsGateGroup.SetParent(root.transform, false);
            logisticsGateGroup.position = new Vector3(0, 0, 62.0f);

            CreateBox("Logistics Left Jamb", new Vector3(-2.15f, 2.0f, 0), new Vector3(0.35f, 4.0f, 0.5f), matDark, logisticsGateGroup, true);
            CreateBox("Logistics Right Jamb", new Vector3(2.15f, 2.0f, 0), new Vector3(0.35f, 4.0f, 0.5f), matDark, logisticsGateGroup, true);
            CreateBox("Logistics Lintel", new Vector3(0, 4.15f, 0), new Vector3(4.65f, 0.35f, 0.5f), matDark, logisticsGateGroup, true);

            var logisticsLeaf = new GameObject("Logistics Sliding Leaf").transform;
            logisticsLeaf.SetParent(logisticsGateGroup, false);
            BuildArmoredDoorLeaf(logisticsLeaf, 3.95f, 3.9f, 0.25f, 2.1f, 1.6f, 0.48f, matDark, matGlass, matMetal, matAmber, "Logistics");

            var logisticsDoor = logisticsGateGroup.gameObject.AddComponent<FacilityDoor>();
            logisticsDoor.Configure(logisticsLeaf, DoorAccessCondition.ManualUnlockOnly,
                "Грузовой шлюз заблокирован · выполните согласование на QNT-01",
                "[E] Открыть грузовой шлюз логистического узла",
                "[E] Закрыть грузовой шлюз логистического узла",
                true, Vector3.up, 3.8f, 1.8f);

            CreateSignboard(logisticsGateGroup, "СЕКТОР 05: ЛОГИСТИКА // SEC-05 LOGISTICS", "SEC-05", new Vector3(0, 4.35f, -0.26f), 0, new Color(0.35f, 0.85f, 1.0f));
            CreateSpotLight("Logistics Portal Spotlight", logisticsGateGroup, new Vector3(0, 4.2f, -0.4f), new Vector3(65, 0, 0), new Color(0.85f, 0.95f, 1.0f), 7.0f, 16f, 85f);

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

            // P08 Cargo Switchyard Shunter on West Substation Track
            BuildCargoSwitchyard(sector04, new Vector3(-6.5f, 0, 44.0f), matMetal, matDark, matAmber, matGreen, matRed);

            // Trench Maintenance Access: Service Egress Stairs connecting trench floor (Y=-1.2) to bridge level (Y=0)
            for (int side = -1; side <= 1; side += 2)
            {
                float sx = side * 2.35f;
                // South trench egress stairs (ascending North towards bridge)
                ModularKit.Place("Platform_Stairs_2", sector04, new Vector3(sx, -1.2f, 39.8f), new Vector3(0.9f, 1.2f, 2.2f), 0, true);
                CreateBox("Trench South Landing " + side, new Vector3(side * 2.2f, 0, 41.0f), new Vector3(0.6f, 0.22f, 0.8f), matMetal, sector04, true);

                // North trench egress stairs (ascending South towards bridge)
                ModularKit.Place("Platform_Stairs_2", sector04, new Vector3(sx, -1.2f, 60.2f), new Vector3(0.9f, 1.2f, 2.2f), 180, true);
                CreateBox("Trench North Landing " + side, new Vector3(side * 2.2f, 0, 59.0f), new Vector3(0.6f, 0.22f, 0.8f), matMetal, sector04, true);
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

            // Quantum Terminal (QNT-01) on the central bridge — Hooked to unlock execDoor and logisticsDoor!
            var qntConsole = BuildQuantumTerminal(sector04, new Vector3(0, 0.25f, 50.0f), execDoor, matMetal, matDark, matAmber, matScreen);
            qntConsole.AddTargetDoor(logisticsDoor);

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

            // =========================================================================
            // 7. SECTOR 05: LOGISTICS CENTER & SWITCHYARD (Z = 62..95, X = -25..15, H = 8.5m)
            // =========================================================================
            var sector05 = new GameObject("Sector 05 - Logistics Center").transform;
            sector05.SetParent(root.transform, false);

            // Floor & Runner
            CreateBox("Sector05 Floor Base", new Vector3(-5.0f, -0.1f, 78.5f), new Vector3(40.4f, 0.2f, 33.4f), matFloor, sector05, true);
            CreateBox("Sector05 Central Runner", new Vector3(0, 0.02f, 78.5f), new Vector3(3.6f, 0.04f, 33.0f), matDark, sector05, false);

            // Ceiling (H = 8.5m)
            CreateBox("Sector05 Ceiling", new Vector3(-5.0f, 8.55f, 78.5f), new Vector3(40.4f, 0.2f, 33.4f), matDark, sector05, true);

            // Monolithic Hermetic Walls (Watertight geometry)
            CreateBox("Sector05 West Monolithic Wall", new Vector3(-25.25f, 4.25f, 78.5f), new Vector3(0.5f, 8.55f, 33.5f), matConcrete, sector05, true);
            CreateBox("Sector05 North Monolithic Wall", new Vector3(-5.0f, 4.25f, 95.25f), new Vector3(40.5f, 8.55f, 0.5f), matConcrete, sector05, true);

            // East Wall with Opening for Sector 06 Robotics Airlock at Z in [74, 80]
            CreateBox("Sector05 East Wall South Segment", new Vector3(15.25f, 4.25f, 68.0f), new Vector3(0.5f, 8.55f, 12.5f), matConcrete, sector05, true);
            CreateBox("Sector05 East Wall North Segment", new Vector3(15.25f, 4.25f, 87.5f), new Vector3(0.5f, 8.55f, 15.5f), matConcrete, sector05, true);
            CreateBox("Sector05 East Wall Doorway Lintel", new Vector3(15.25f, 6.35f, 77.0f), new Vector3(0.5f, 4.35f, 6.5f), matDark, sector05, true);

            // South Boundary Walls (Z = 61.8)
            CreateBox("Sector05 South Wall West Wing", new Vector3(-17.625f, 4.25f, 61.8f), new Vector3(15.25f, 8.55f, 0.5f), matConcrete, sector05, true);
            CreateBox("Sector05 South Wall East Wing", new Vector3(12.625f, 4.25f, 61.8f), new Vector3(5.25f, 8.55f, 0.5f), matConcrete, sector05, true);
            CreateBox("Sector04-to-Sector05 Bulkhead", new Vector3(0, 8.05f, 61.8f), new Vector3(20.0f, 1.05f, 0.5f), matDark, root.transform, true);

            // Decorative Wall Astra Panels along perimeter
            for (int z = 66; z <= 90; z += 4)
            {
                ModularKit.Place("WallAstra_Straight", sector05, new Vector3(-25.0f, 0, z), new Vector3(0.5f, 8.5f, 4.0f), 0, true);
            }

            // Robotics Plant Transition Airlock Door at X = 15.0, Z = 77.0
            var roboAirlockGroup = new GameObject("Airlock to Robotics Plant").transform;
            roboAirlockGroup.SetParent(sector05, false);
            roboAirlockGroup.position = new Vector3(15.0f, 0, 77.0f);
            roboAirlockGroup.rotation = Quaternion.Euler(0, -90, 0); // Door width along world Z!

            CreateBox("Robo Airlock Left Jamb", new Vector3(-1.6f, 1.9f, 0), new Vector3(0.35f, 3.8f, 0.45f), matDark, roboAirlockGroup, true);
            CreateBox("Robo Airlock Right Jamb", new Vector3(1.6f, 1.9f, 0), new Vector3(0.35f, 3.8f, 0.45f), matDark, roboAirlockGroup, true);
            CreateBox("Robo Airlock Lintel", new Vector3(0, 3.95f, 0), new Vector3(3.55f, 0.35f, 0.45f), matDark, roboAirlockGroup, true);

            var roboLeaf = new GameObject("Robo Sliding Leaf").transform;
            roboLeaf.SetParent(roboAirlockGroup, false);
            BuildArmoredDoorLeaf(roboLeaf, 2.95f, 3.75f, 0.22f, 2.0f, 1.35f, 0.45f, matDark, matGlass, matMetal, matAmber, "RoboAirlock");

            var roboDoor = roboAirlockGroup.gameObject.AddComponent<FacilityDoor>();
            roboDoor.Configure(roboLeaf, DoorAccessCondition.ManualUnlockOnly,
                "Шлюз сборочного цеха заблокирован · авторизуйтесь на терминале CARGO-WIN",
                "[E] Открыть гермошлюз сборочного цеха",
                "[E] Закрыть гермошлюз сборочного цеха",
                true, Vector3.right, 3.0f, 1.8f);

            CreateSignboard(roboAirlockGroup, "СЕКТОР 06: СБОРОЧНЫЙ ЦЕХ РОБОТОТЕХНИКИ", "SEC-06", new Vector3(0, 4.15f, -0.24f), 0, new Color(0.95f, 0.65f, 0.25f));

            // Overhead Crane Gantry Trusses
            for (int gz = 68; gz <= 88; gz += 10)
            {
                CreateBox("Gantry Truss Base " + gz, new Vector3(-5.0f, 7.2f, gz), new Vector3(39.0f, 0.45f, 0.65f), matMetal, sector05, true);
                CreateBox("Gantry Rail South " + gz, new Vector3(-5.0f, 7.5f, gz - 0.2f), new Vector3(39.0f, 0.15f, 0.08f), matDark, sector05, false);
                CreateBox("Gantry Rail North " + gz, new Vector3(-5.0f, 7.5f, gz + 0.2f), new Vector3(39.0f, 0.15f, 0.08f), matDark, sector05, false);
            }
            // Hanging Motorized Hoist Crane Trolley
            CreateBox("Crane Trolley Body", new Vector3(-12.0f, 6.7f, 78.0f), new Vector3(2.4f, 0.65f, 1.8f), matAmber, sector05, true);
            CreateBox("Crane Pulley Block", new Vector3(-12.0f, 5.8f, 78.0f), new Vector3(0.5f, 0.7f, 0.4f), matDark, sector05, false);
            CreateBox("Crane Hook", new Vector3(-12.0f, 5.2f, 78.0f), new Vector3(0.15f, 0.5f, 0.15f), matMetal, sector05, false);

            // West Container Yard: Stacks of Heavy Freight Containers
            BuildCargoContainer(sector05, new Vector3(-16.0f, 0, 70.0f), new Vector3(3.2f, 2.6f, 7.0f), 0f, matDark, matAmber, "NL-CONT-901 // HAZARD");
            BuildCargoContainer(sector05, new Vector3(-16.0f, 2.6f, 70.0f), new Vector3(3.2f, 2.6f, 7.0f), 0f, matMetal, matAmber, "NL-CONT-902 // CRYO");

            BuildCargoContainer(sector05, new Vector3(-10.5f, 0, 83.0f), new Vector3(3.2f, 2.6f, 7.0f), 15f, matDark, matMetal, "NL-CONT-914 // SUB-CORE");
            BuildCargoContainer(sector05, new Vector3(-20.5f, 0, 85.0f), new Vector3(3.2f, 2.6f, 7.0f), -10f, matMetal, matAmber, "NL-CONT-920 // ORDNANCE");
            BuildCargoContainer(sector05, new Vector3(-20.5f, 2.6f, 85.0f), new Vector3(3.2f, 2.6f, 7.0f), -10f, matDark, matMetal, "NL-CONT-921 // ORDNANCE");

            // Dual Floor Shunter Rails embedded in concrete
            for (float rx = -18.0f; rx <= -11.0f; rx += 6.0f)
            {
                CreateBox("Rail Left " + rx, new Vector3(rx - 0.7f, 0.04f, 78.5f), new Vector3(0.08f, 0.08f, 31.0f), matMetal, sector05, false);
                CreateBox("Rail Right " + rx, new Vector3(rx + 0.7f, 0.04f, 78.5f), new Vector3(0.08f, 0.08f, 31.0f), matMetal, sector05, false);
            }

            // East Raised Logistics Sorting Mezzanine & Freight Lift (Loop 3)
            var mezz = new GameObject("Logistics Sorting Mezzanine").transform;
            mezz.SetParent(sector05, false);
            mezz.position = new Vector3(8.5f, 0, 72.0f);

            CreateBox("Mezzanine Slab", new Vector3(0, 0.5f, 0), new Vector3(10.0f, 1.0f, 14.0f), matFloor, mezz, true);
            // Mezzanine Access Stairs
            ModularKit.Place("Platform_Stairs_2", mezz, new Vector3(-4.5f, 0, -5.5f), new Vector3(1.4f, 1.0f, 2.4f), 0, true);
            ModularKit.Place("Platform_Stairs_2", mezz, new Vector3(-4.5f, 0, 5.5f), new Vector3(1.4f, 1.0f, 2.4f), 180, true);
            // Perimeter Safety Railings on Mezzanine
            ModularKit.Place("Platform_Rails_4Wide", mezz, new Vector3(-4.95f, 1.0f, 0), new Vector3(0.08f, 1.1f, 8.0f), 0, true);
            ModularKit.Place("Platform_Rails_4Wide", mezz, new Vector3(0, 1.0f, -6.95f), new Vector3(9.5f, 1.1f, 0.08f), 90, true);
            ModularKit.Place("Platform_Rails_4Wide", mezz, new Vector3(0, 1.0f, 6.95f), new Vector3(9.5f, 1.1f, 0.08f), -90, true);

            // Freight Lift Cage (Loop 3) at X = 12.0, Z = 68.0
            CreateBox("Freight Lift Base", new Vector3(3.5f, 1.05f, -3.5f), new Vector3(2.6f, 0.15f, 2.6f), matMetal, mezz, true);
            CreateBox("Lift Pillar SW", new Vector3(2.3f, 2.5f, -4.7f), new Vector3(0.12f, 3.0f, 0.12f), matDark, mezz, true);
            CreateBox("Lift Pillar NW", new Vector3(2.3f, 2.5f, -2.3f), new Vector3(0.12f, 3.0f, 0.12f), matDark, mezz, true);
            CreateBox("Lift Pillar SE", new Vector3(4.7f, 2.5f, -4.7f), new Vector3(0.12f, 3.0f, 0.12f), matDark, mezz, true);
            CreateBox("Lift Pillar NE", new Vector3(4.7f, 2.5f, -2.3f), new Vector3(0.12f, 3.0f, 0.12f), matDark, mezz, true);
            CreateBox("Lift Roof Frame", new Vector3(3.5f, 4.05f, -3.5f), new Vector3(2.6f, 0.15f, 2.6f), matDark, mezz, false);

            // CARGO-WIN Terminal Workstation on the Mezzanine
            BuildCargoTerminal(mezz, new Vector3(0, 1.0f, 0), roboDoor, matMetal, matDark, matAmber, matScreen);

            // Staged Ammo Pickups in Sector 05
            BuildAmmoBoxPickup(mezz, new Vector3(-1.8f, 1.0f, -2.5f), 12, matMetal, matDark, matAmber);
            BuildAmmoBoxPickup(sector05, new Vector3(-14.5f, 0, 86.0f), 12, matMetal, matDark, matAmber);

            // Emergency Weapon Rack in Sector 05
            BuildWeaponRack(sector05, new Vector3(14.2f, 1.35f, 65.0f), -90, "Sector 05 Logistics Emergency Rack");

            // Autonomous Threats in Sector 05
            BuildPatrolRobot(sector05, "logistics-sentry-r25", "Enemy_Trilobite",
                new Vector3(-15.0f, 0, 75.0f), 0f,
                new Vector3(-15.0f, 0, 66.0f), new Vector3(-15.0f, 0, 90.0f),
                new Vector2(-23.0f, -6.0f), new Vector2(65.0f, 92.0f),
                matMetal, matDark, matAmber);

            BuildTrackedSentry(sector05, new Vector3(0, 0, 76.0f), new Vector3(0, 0, 66.0f), new Vector3(0, 0, 88.0f), matMetal, matDark, matAmber, matRed);

            // High-Bay Industrial Illumination in Sector 05
            Vector3[] s05HighBays = new[] {
                new Vector3(-16.0f, 8.2f, 70.0f),
                new Vector3(-16.0f, 8.2f, 85.0f),
                new Vector3(0f, 8.2f, 70.0f),
                new Vector3(0f, 8.2f, 85.0f),
                new Vector3(9.0f, 8.2f, 70.0f),
                new Vector3(9.0f, 8.2f, 85.0f)
            };
            for (int i = 0; i < s05HighBays.Length; i++)
            {
                Vector3 hbPos = s05HighBays[i];
                ModularKit.Place("Prop_Light_Wide", sector05, hbPos, new Vector3(1.4f, 1.0f, 1.4f), 0, false);
                CreateSpotLight("S05 HighBay " + i, sector05, hbPos, new Vector3(90, 0, 0), new Color(1.0f, 0.92f, 0.82f), 7.5f, 25f, 115f);
                CreatePointLight("S05 Fill " + i, sector05, hbPos - Vector3.up * 0.8f, new Color(0.92f, 0.80f, 0.65f), 4.5f, 18f);
            }

            CreateReflectionProbe("Reflection Probe - Sector 05 Logistics", new Vector3(-5.0f, 4.0f, 78.5f), new Vector3(42.0f, 10.0f, 35.0f), sector05);

            // =========================================================================
            // 8. SECTOR 06: ROBOTICS ASSEMBLY PLANT (Z = 65..95, X = 15..52, H = 10.5m)
            // =========================================================================
            var sector06 = new GameObject("Sector 06 - Robotics Plant").transform;
            sector06.SetParent(root.transform, false);

            // Floor & Runner
            CreateBox("Sector06 Floor Base", new Vector3(33.5f, -0.1f, 80.0f), new Vector3(37.4f, 0.2f, 30.4f), matFloor, sector06, true);
            CreateBox("Sector06 Central Assembly Runner", new Vector3(33.5f, 0.02f, 80.0f), new Vector3(5.0f, 0.04f, 28.0f), matDark, sector06, false);

            // High Ceiling (H = 10.5m)
            CreateBox("Sector06 Ceiling", new Vector3(33.5f, 10.55f, 80.0f), new Vector3(37.4f, 0.2f, 30.4f), matDark, sector06, true);

            // Perimeter Walls with Portals into Sectors 07 & 08
            CreateBox("Sector06 North Monolithic Wall", new Vector3(33.5f, 5.25f, 95.25f), new Vector3(37.5f, 10.55f, 0.5f), matConcrete, sector06, true);

            // Sector 06 South Wall with SEC-07 Portal Aperture (X: 27..33, Y: 0..4m)
            CreateBox("Sector06 South Wall Left", new Vector3(21.0f, 5.25f, 64.75f), new Vector3(12.0f, 10.55f, 0.5f), matConcrete, sector06, true);
            CreateBox("Sector06 South Wall Right", new Vector3(42.5f, 5.25f, 64.75f), new Vector3(19.0f, 10.55f, 0.5f), matConcrete, sector06, true);
            CreateBox("Sector06 South Wall Lintel", new Vector3(30.0f, 7.275f, 64.75f), new Vector3(6.0f, 6.55f, 0.5f), matDark, sector06, true);

            // Sector 06 East Wall with Neuroarchive Portal Aperture (Z: 72..78, Y: 0..4m)
            CreateBox("Sector06 East Wall South", new Vector3(52.25f, 5.25f, 68.5f), new Vector3(0.5f, 10.55f, 7.0f), matConcrete, sector06, true);
            CreateBox("Sector06 East Wall North", new Vector3(52.25f, 5.25f, 86.5f), new Vector3(0.5f, 10.55f, 17.0f), matConcrete, sector06, true);
            CreateBox("Sector06 East Wall Lintel", new Vector3(52.25f, 7.275f, 75.0f), new Vector3(0.5f, 6.55f, 6.0f), matDark, sector06, true);

            // West Bulkhead over doorway into Sector 05
            CreateBox("Sector06 West Bulkhead", new Vector3(14.75f, 9.45f, 77.0f), new Vector3(0.5f, 2.2f, 6.5f), matDark, root.transform, true);

            // Central Automated Robotic Assembly Conveyor Line
            var conveyor = new GameObject("Robotics Assembly Conveyor Line").transform;
            conveyor.SetParent(sector06, false);
            conveyor.position = new Vector3(33.5f, 0, 80.0f);

            CreateBox("Conveyor Bed", new Vector3(0, 0.6f, 0), new Vector3(3.4f, 1.2f, 22.0f), matDark, conveyor, true);
            CreateBox("Conveyor Belt Surface", new Vector3(0, 1.22f, 0), new Vector3(2.8f, 0.04f, 21.6f), matMetal, conveyor, false);

            // Staged Chassis & Articulated Robotic Arms along Conveyor
            for (int cy = -8; cy <= 8; cy += 8)
            {
                // Robotic Arm Left (West side of conveyor)
                BuildRoboticArm(sector06, new Vector3(30.2f, 0, 80.0f + cy), 90f, matDark, matMetal, matAmber);
                // Robotic Arm Right (East side of conveyor)
                BuildRoboticArm(sector06, new Vector3(36.8f, 0, 80.0f + cy), -90f, matDark, matMetal, matAmber);

                // Unfinished Robot Chassis on the line
                ModularKit.Place("Prop_Crate3", conveyor, new Vector3(0, 1.25f, cy), new Vector3(1.2f, 0.8f, 1.6f), 0, true);
            }

            // High Observation Catwalk along North Wall at Y = 4.2m
            var cat06 = new GameObject("Robotics Observation Catwalk").transform;
            cat06.SetParent(sector06, false);
            cat06.position = new Vector3(33.5f, 4.2f, 91.5f);

            CreateBox("Catwalk Slab", Vector3.zero, new Vector3(28.0f, 0.2f, 3.4f), matFloor, cat06, true);
            ModularKit.Place("Platform_Rails_4Wide", cat06, new Vector3(0, 0.1f, -1.65f), new Vector3(27.0f, 1.1f, 0.08f), 0, true);

            // Catwalk Access Stairs on West and East ends
            ModularKit.Place("Platform_Stairs_4", sector06, new Vector3(20.5f, 0, 91.5f), new Vector3(1.4f, 4.2f, 4.5f), 90, true);
            ModularKit.Place("Platform_Stairs_4", sector06, new Vector3(46.5f, 0, 91.5f), new Vector3(1.4f, 4.2f, 4.5f), -90, true);

            // RBT-01 Robotics Diagnostics Terminal on Observation Catwalk
            BuildRoboTerminal(cat06, new Vector3(0, 0.1f, 0.8f), null, matMetal, matDark, matAmber, matScreen);

            // Proving Grounds Arena (South Bay: X: 20..47, Z: 66..76)
            ModularKit.Place("Prop_Barrel_Large", sector06, new Vector3(22.0f, 0, 68.0f), new Vector3(1.2f, 1.4f, 1.2f), 15, true);
            ModularKit.Place("Prop_Barrel_Large", sector06, new Vector3(22.0f, 0, 69.5f), new Vector3(1.2f, 1.4f, 1.2f), -20, true);
            ModularKit.Place("Prop_Crate4", sector06, new Vector3(45.0f, 0, 68.0f), new Vector3(1.0f, 1.0f, 1.0f), 30, true);

            // Staged Ammo Pickups in Sector 06
            BuildAmmoBoxPickup(cat06, new Vector3(4.5f, 0.1f, 0.5f), 12, matMetal, matDark, matAmber);
            BuildAmmoBoxPickup(sector06, new Vector3(24.0f, 0, 68.0f), 12, matMetal, matDark, matAmber);

            // Emergency Weapon Rack in Sector 06
            BuildWeaponRack(cat06, new Vector3(-6.0f, 1.35f, 1.55f), 0, "Sector 06 Catwalk Emergency Rack");

            // Autonomous Patrol Threat: ASSEMBLY SENTRY R26 (QuadShell)
            BuildPatrolRobot(sector06, "assembly-sentry-r26", "Enemy_QuadShell",
                new Vector3(42.0f, 0, 75.0f), 0f,
                new Vector3(42.0f, 0, 68.0f), new Vector3(42.0f, 0, 86.0f),
                new Vector2(18.0f, 50.0f), new Vector2(66.0f, 90.0f),
                matMetal, matDark, matAmber);

            // High-Bay Industrial Illumination in Sector 06 (Cool White 6500K)
            Vector3[] s06HighBays = new[] {
                new Vector3(24.0f, 10.0f, 72.0f),
                new Vector3(24.0f, 10.0f, 86.0f),
                new Vector3(33.5f, 10.0f, 72.0f),
                new Vector3(33.5f, 10.0f, 86.0f),
                new Vector3(44.0f, 10.0f, 72.0f),
                new Vector3(44.0f, 10.0f, 86.0f)
            };
            for (int i = 0; i < s06HighBays.Length; i++)
            {
                Vector3 hbPos = s06HighBays[i];
                ModularKit.Place("Prop_Light_Wide", sector06, hbPos, new Vector3(1.5f, 1.0f, 1.5f), 0, false);
                CreateSpotLight("S06 HighBay " + i, sector06, hbPos, new Vector3(90, 0, 0), new Color(0.85f, 0.95f, 1.0f), 8.0f, 28f, 120f);
                CreatePointLight("S06 Fill " + i, sector06, hbPos - Vector3.up * 1.0f, new Color(0.70f, 0.88f, 1.0f), 5.0f, 20f);
            }

            CreateReflectionProbe("Reflection Probe - Sector 06 Robotics", new Vector3(33.5f, 5.0f, 80.0f), new Vector3(38.0f, 12.0f, 32.0f), sector06);

            // =========================================================================
            // 9. SECTOR 07: SECURITY CITADEL & ACCESS CONTROL (Z = 18..65, X = 5..54)
            // =========================================================================
            var sector07 = new GameObject("Sector 07 - Security Citadel").transform;
            sector07.SetParent(root.transform, false);

            // Grand Security Shortcut Door into SOC (X = 5.0, Z = -1.75)
            var socSecDoorGroup = new GameObject("Security Shortcut to SOC Gate (X=5.0, Z=-1.75)").transform;
            socSecDoorGroup.SetParent(sector07, false);
            socSecDoorGroup.position = new Vector3(5.0f, 0, -1.75f);

            CreateBox("SOC Sec Left Jamb", new Vector3(0, 1.5f, -1.35f), new Vector3(0.35f, 3.0f, 0.3f), matDark, socSecDoorGroup, true);
            CreateBox("SOC Sec Right Jamb", new Vector3(0, 1.5f, 1.35f), new Vector3(0.35f, 3.0f, 0.3f), matDark, socSecDoorGroup, true);
            CreateBox("SOC Sec Lintel", new Vector3(0, 3.15f, 0), new Vector3(0.35f, 0.3f, 3.0f), matDark, socSecDoorGroup, true);

            var socSecLeaf = new GameObject("SOC Sec Sliding Leaf").transform;
            socSecLeaf.SetParent(socSecDoorGroup, false);
            BuildArmoredDoorLeaf(socSecLeaf, 2.6f, 2.95f, 0.22f, 1.8f, 1.25f, 0.42f, matDark, matGlass, matMetal, matAmber, "SOC Sec");

            var socSecDoor = socSecDoorGroup.gameObject.AddComponent<FacilityDoor>();
            socSecDoor.Configure(socSecLeaf, DoorAccessCondition.ManualUnlockOnly,
                "Шлюз безопасности заблокирован · требуется авторизация на SEC-07",
                "[E] Открыть шлюз безопасности в операторскую",
                "[E] Закрыть шлюз безопасности",
                true, Vector3.forward, 2.6f, 1.8f);

            CreateSignboard(socSecDoorGroup, "СЛУЖЕБНЫЙ ПРОХОД: СЕКТОР 07 // БЕЗОПАСНОСТЬ", "SEC-07-SOC", new Vector3(0.24f, 3.35f, 0), 90, new Color(0.95f, 0.75f, 0.25f));

            // SEC-07 Airlock Portal between Sector 06 (Z=64.75) and Sector 07 (Z=65.0)
            var sec07GateGroup = new GameObject("SEC-07 Gate (Z=64.75)").transform;
            sec07GateGroup.SetParent(sector07, false);
            sec07GateGroup.position = new Vector3(30.0f, 0, 64.75f);

            CreateBox("SEC07 Left Jamb", new Vector3(-1.6f, 1.75f, 0), new Vector3(0.35f, 3.5f, 0.4f), matDark, sec07GateGroup, true);
            CreateBox("SEC07 Right Jamb", new Vector3(1.6f, 1.75f, 0), new Vector3(0.35f, 3.5f, 0.4f), matDark, sec07GateGroup, true);
            CreateBox("SEC07 Lintel", new Vector3(0, 3.65f, 0), new Vector3(3.5f, 0.35f, 0.4f), matDark, sec07GateGroup, true);

            var sec07Leaf = new GameObject("SEC07 Sliding Leaf").transform;
            sec07Leaf.SetParent(sec07GateGroup, false);
            BuildArmoredDoorLeaf(sec07Leaf, 3.0f, 3.45f, 0.22f, 1.8f, 1.4f, 0.45f, matDark, matGlass, matMetal, matAmber, "SEC07");

            var sec07Door = sec07GateGroup.gameObject.AddComponent<FacilityDoor>();
            sec07Door.Configure(sec07Leaf, DoorAccessCondition.AlwaysUnlocked,
                "Шлюз службы безопасности разблокирован",
                "[E] Открыть шлюз службы безопасности",
                "[E] Закрыть шлюз службы безопасности",
                false, Vector3.right, 3.0f, 1.8f);

            CreateSignboard(sec07GateGroup, "СЕКТОР 07: ЦИТАДЕЛЬ БЕЗОПАСНОСТИ", "SEC-07", new Vector3(0, 3.85f, 0.24f), 180, new Color(1.0f, 0.85f, 0.3f));

            // Room 07.01: Security Foyer & Checkpoint (Z = 50..65, X = 20..36, H = 6.0m)
            CreateBox("S07 Foyer Floor", new Vector3(28.0f, -0.1f, 57.5f), new Vector3(16.0f, 0.2f, 15.0f), matFloor, sector07, true);
            CreateBox("S07 Foyer Ceiling", new Vector3(28.0f, 6.05f, 57.5f), new Vector3(16.0f, 0.2f, 15.0f), matDark, sector07, true);
            CreateBox("S07 Foyer West Wall", new Vector3(19.8f, 3.0f, 57.5f), new Vector3(0.4f, 6.0f, 15.0f), matConcrete, sector07, true);
            CreateBox("S07 Foyer North Wall L", new Vector3(23.5f, 3.0f, 65.2f), new Vector3(7.0f, 6.0f, 0.4f), matConcrete, sector07, true);
            CreateBox("S07 Foyer North Wall R", new Vector3(34.25f, 3.0f, 65.2f), new Vector3(3.5f, 6.0f, 0.4f), matConcrete, sector07, true);
            CreateBox("S07 Foyer North Wall Lintel", new Vector3(30.0f, 4.85f, 65.2f), new Vector3(6.0f, 2.3f, 0.4f), matDark, sector07, true);
            CreateBox("S07 Foyer East Wall N", new Vector3(36.2f, 3.0f, 61.25f), new Vector3(0.4f, 6.0f, 7.5f), matConcrete, sector07, true);

            // Turnstile Checkpoint Gates & Metal Detector Portal in Foyer
            CreateBox("Detector Arch Left", new Vector3(27.0f, 1.3f, 58.0f), new Vector3(0.12f, 2.6f, 0.4f), matDark, sector07, true);
            CreateBox("Detector Arch Right", new Vector3(28.8f, 1.3f, 58.0f), new Vector3(0.12f, 2.6f, 0.4f), matDark, sector07, true);
            CreateBox("Detector Arch Lintel", new Vector3(27.9f, 2.65f, 58.0f), new Vector3(1.92f, 0.15f, 0.4f), matDark, sector07, true);
            CreateBox("Detector Status LED", new Vector3(27.9f, 2.65f, 57.78f), new Vector3(0.2f, 0.05f, 0.04f), matAmber, sector07, false);

            // Staged CIRRUS Pulse Sidearm Weapon Rack in Room 07.01 Foyer
            BuildCirrusWeaponRack(sector07, new Vector3(20.4f, 1.35f, 55.0f), 90f, "S07 Foyer Pulse Armory");
            BuildAmmoBoxPickup(sector07, new Vector3(21.0f, 0, 52.5f), 12, matMetal, matDark, matAmber);

            // Room 07.02: 3-Story Containment Atrium (Z = 20..50, X = 20..42, H = 9.0m)
            CreateBox("S07 Atrium Floor", new Vector3(31.0f, -0.1f, 35.0f), new Vector3(22.0f, 0.2f, 30.0f), matFloor, sector07, true);
            CreateBox("S07 Atrium Ceiling", new Vector3(31.0f, 9.05f, 35.0f), new Vector3(22.0f, 0.2f, 30.0f), matDark, sector07, true);
            CreateBox("S07 Atrium West Wall", new Vector3(19.8f, 4.5f, 35.0f), new Vector3(0.4f, 9.0f, 30.0f), matConcrete, sector07, true);
            CreateBox("S07 Atrium South Wall", new Vector3(31.0f, 4.5f, 19.8f), new Vector3(22.0f, 9.0f, 0.4f), matConcrete, sector07, true);

            // Reinforced Holding Cells along West Wall (X = 20..25, Z = 22..48)
            for (float cz = 24.0f; cz <= 44.0f; cz += 6.5f)
            {
                CreateBox("Cell Divider " + cz, new Vector3(22.5f, 1.6f, cz), new Vector3(5.0f, 3.2f, 0.25f), matConcrete, sector07, true);
                CreateBox("Cell Front Grille " + cz, new Vector3(25.0f, 1.6f, cz + 3.25f), new Vector3(0.12f, 3.2f, 6.2f), matDark, sector07, true);
                CreateBox("Cell Window " + cz, new Vector3(25.0f, 1.6f, cz + 3.25f), new Vector3(0.04f, 1.4f, 3.0f), matGlass, sector07, false);
                CreatePointLight("Cell Luminaire " + cz, sector07, new Vector3(22.5f, 2.8f, cz + 3.25f), new Color(0.85f, 0.4f, 0.1f), 1.8f, 4.5f);
            }

            // Multi-Level Catwalk Tier 1 (Y = 3.2m)
            var catT1 = new GameObject("Atrium Catwalk Tier 1").transform;
            catT1.SetParent(sector07, false);
            catT1.position = new Vector3(34.0f, 3.2f, 35.0f);
            CreateBox("Catwalk T1 Slab", Vector3.zero, new Vector3(14.0f, 0.2f, 26.0f), matFloor, catT1, true);
            ModularKit.Place("Platform_Rails_4Wide", catT1, new Vector3(-6.9f, 0.1f, 0), new Vector3(0.08f, 1.1f, 25.0f), 0, true);

            // Stairs to Catwalk Tier 1
            ModularKit.Place("Platform_Stairs_4", sector07, new Vector3(38.0f, 0, 22.0f), new Vector3(1.4f, 3.2f, 4.0f), 0, true);

            // High Overhead Searchlights in Atrium
            CreateSpotLight("Atrium Spot N", sector07, new Vector3(31.0f, 8.8f, 44.0f), new Vector3(90, 0, 0), new Color(0.95f, 0.95f, 1.0f), 8.5f, 22f, 100f);
            CreateSpotLight("Atrium Spot S", sector07, new Vector3(31.0f, 8.8f, 26.0f), new Vector3(90, 0, 0), new Color(0.95f, 0.95f, 1.0f), 8.5f, 22f, 100f);

            // Autonomous Threats in S07 Atrium: E08 BULWARK and E09 JAMMER
            BuildBulwarkSentry(sector07, new Vector3(30.0f, 0, 42.0f), new Vector3(30.0f, 0, 24.0f), new Vector3(30.0f, 0, 46.0f), matMetal, matDark, matAmber, matRed);
            BuildJammerRobot(catT1, new Vector3(34.0f, 3.3f, 35.0f), new Vector3(34.0f, 3.3f, 25.0f), new Vector3(34.0f, 3.3f, 45.0f), matMetal, matDark, matAmber);

            // Room 07.03: CCTV Monitoring Operations Center (Z = 35..50, X = 42..54, H = 4.8m)
            CreateBox("S07 CCTV Floor", new Vector3(48.0f, -0.1f, 42.5f), new Vector3(12.0f, 0.2f, 15.0f), matFloor, sector07, true);
            CreateBox("S07 CCTV Ceiling", new Vector3(48.0f, 4.85f, 42.5f), new Vector3(12.0f, 0.2f, 15.0f), matDark, sector07, true);
            CreateBox("S07 CCTV North Wall", new Vector3(48.0f, 2.4f, 50.2f), new Vector3(12.0f, 4.8f, 0.4f), matConcrete, sector07, true);
            CreateBox("S07 CCTV East Wall", new Vector3(54.2f, 2.4f, 42.5f), new Vector3(0.4f, 4.8f, 15.0f), matConcrete, sector07, true);
            CreateBox("S07 CCTV-Archive Divider", new Vector3(48.0f, 2.4f, 34.8f), new Vector3(12.0f, 4.8f, 0.4f), matDark, sector07, true);

            // Bank of Curved Surveillance Monitors on North Wall
            for (int mx = -4; mx <= 4; mx += 2)
            {
                CreateBox("CCTV Monitor " + mx, new Vector3(48.0f + mx, 2.2f, 49.85f), new Vector3(1.8f, 1.2f, 0.15f), matDark, sector07, false);
                CreateBox("CCTV Screen " + mx, new Vector3(48.0f + mx, 2.2f, 49.75f), new Vector3(1.68f, 1.08f, 0.02f), matScreen, sector07, false);
            }
            CreatePointLight("CCTV Monitor Glow", sector07, new Vector3(48.0f, 2.2f, 48.5f), new Color(0.2f, 0.65f, 0.85f), 3.5f, 7.0f);

            // Room 07.04: Access Credential Archive (Z = 20..35, X = 42..54, H = 4.8m)
            CreateBox("S07 Archive Floor", new Vector3(48.0f, -0.1f, 27.5f), new Vector3(12.0f, 0.2f, 15.0f), matFloor, sector07, true);
            CreateBox("S07 Archive Ceiling", new Vector3(48.0f, 4.85f, 27.5f), new Vector3(12.0f, 0.2f, 15.0f), matDark, sector07, true);
            CreateBox("S07 Archive South Wall", new Vector3(48.0f, 2.4f, 19.8f), new Vector3(12.0f, 4.8f, 0.4f), matConcrete, sector07, true);
            CreateBox("S07 Archive East Wall", new Vector3(54.2f, 2.4f, 27.5f), new Vector3(0.4f, 4.8f, 15.0f), matConcrete, sector07, true);

            // SEC-07 Workstation (Windows Server 2025 / PowerShell 7.5 scenario t05_sec)
            BuildSec07Terminal(sector07, new Vector3(48.0f, 0, 25.0f), socSecDoor, matMetal, matDark, matAmber, matScreen);
            BuildAmmoBoxPickup(sector07, new Vector3(52.0f, 0, 22.0f), 12, matMetal, matDark, matAmber);

            // Room 07.05: Security Shortcut Corridor to SOC (Z = -2..20, X = 5..12, H = 3.4m)
            CreateBox("S07 Corridor Floor", new Vector3(8.5f, -0.1f, 9.0f), new Vector3(7.0f, 0.2f, 22.0f), matFloor, sector07, true);
            CreateBox("S07 Corridor Ceiling", new Vector3(8.5f, 3.45f, 9.0f), new Vector3(7.0f, 0.2f, 22.0f), matDark, sector07, true);
            CreateBox("S07 Corridor West Wall", new Vector3(4.8f, 1.7f, 9.0f), new Vector3(0.4f, 3.4f, 22.0f), matConcrete, sector07, true);
            CreateBox("S07 Corridor East Wall", new Vector3(12.2f, 1.7f, 9.0f), new Vector3(0.4f, 3.4f, 22.0f), matConcrete, sector07, true);
            CreateBox("S07 Corridor Portal Bulkhead", new Vector3(8.5f, 1.7f, 20.2f), new Vector3(7.0f, 3.4f, 0.4f), matDark, sector07, true);

            CreateReflectionProbe("Reflection Probe - Sector 07 Security", new Vector3(35.0f, 4.0f, 38.0f), new Vector3(40.0f, 10.0f, 50.0f), sector07);

            // =========================================================================
            // 10. SECTOR 08: NEUROARCHIVE & BIOSTASIS (Z = 18..85, X = 54..88, H = 8.5m)
            // =========================================================================
            var sector08 = new GameObject("Sector 08 - Neuroarchive").transform;
            sector08.SetParent(root.transform, false);

            // Neuroarchive Cryo Gate (X = 52.25, Z = 75.0)
            var neuroGateGroup = new GameObject("Neuroarchive Cryo Gate (X=52.25, Z=75.0)").transform;
            neuroGateGroup.SetParent(sector08, false);
            neuroGateGroup.position = new Vector3(52.25f, 0, 75.0f);

            CreateBox("Neuro Gate Left Jamb", new Vector3(0, 1.75f, -1.6f), new Vector3(0.4f, 3.5f, 0.35f), matDark, neuroGateGroup, true);
            CreateBox("Neuro Gate Right Jamb", new Vector3(0, 1.75f, 1.6f), new Vector3(0.4f, 3.5f, 0.35f), matDark, neuroGateGroup, true);
            CreateBox("Neuro Gate Lintel", new Vector3(0, 3.65f, 0), new Vector3(0.4f, 0.35f, 3.5f), matDark, neuroGateGroup, true);

            var neuroLeaf = new GameObject("Neuro Sliding Leaf").transform;
            neuroLeaf.SetParent(neuroGateGroup, false);
            BuildArmoredDoorLeaf(neuroLeaf, 3.0f, 3.45f, 0.22f, 1.8f, 1.4f, 0.45f, matDark, matGlass, matMetal, matAmber, "Neuro");

            var neuroDoor = neuroGateGroup.gameObject.AddComponent<FacilityDoor>();
            neuroDoor.Configure(neuroLeaf, DoorAccessCondition.AlwaysUnlocked,
                "Крио-шлюз нейроархива разблокирован",
                "[E] Открыть крио-шлюз нейроархива",
                "[E] Закрыть крио-шлюз нейроархива",
                false, Vector3.forward, 3.0f, 1.8f);

            CreateSignboard(neuroGateGroup, "СЕКТОР 08: НЕЙРОАРХИВ И БИОСТАЗ", "SEC-08", new Vector3(0.24f, 3.85f, 0), 90, new Color(0.35f, 0.9f, 1.0f));

            // Deep Memory Vault Blast Gate (X = 71.0, Z = 40.0)
            var memVaultGateGroup = new GameObject("Deep Memory Vault Gate (Z=40.0)").transform;
            memVaultGateGroup.SetParent(sector08, false);
            memVaultGateGroup.position = new Vector3(71.0f, 0, 40.0f);

            CreateBox("MemGate Left Jamb", new Vector3(-1.6f, 1.75f, 0), new Vector3(0.35f, 3.5f, 0.4f), matDark, memVaultGateGroup, true);
            CreateBox("MemGate Right Jamb", new Vector3(1.6f, 1.75f, 0), new Vector3(0.35f, 3.5f, 0.4f), matDark, memVaultGateGroup, true);
            CreateBox("MemGate Lintel", new Vector3(0, 3.65f, 0), new Vector3(3.5f, 0.35f, 0.4f), matDark, memVaultGateGroup, true);

            var memVaultLeaf = new GameObject("Memory Vault Leaf").transform;
            memVaultLeaf.SetParent(memVaultGateGroup, false);
            BuildArmoredDoorLeaf(memVaultLeaf, 3.0f, 3.45f, 0.22f, 1.8f, 1.4f, 0.45f, matDark, matGlass, matMetal, matAmber, "MemVault");

            var memVaultDoor = memVaultGateGroup.gameObject.AddComponent<FacilityDoor>();
            memVaultDoor.Configure(memVaultLeaf, DoorAccessCondition.ManualUnlockOnly,
                "Гермошлюз хранилища памяти заблокирован · требуется авторизация на NEURO-MAC",
                "[E] Открыть шлюз хранилища памяти",
                "[E] Закрыть шлюз хранилища памяти",
                true, Vector3.right, 3.0f, 1.8f);

            CreateSignboard(memVaultGateGroup, "ГЛУБОКОЕ ХРАНИЛИЩЕ ПАМЯТИ // MEMORY VAULT", "MEM-VAULT", new Vector3(0, 3.85f, 0.24f), 180, new Color(0.2f, 0.85f, 1.0f));

            // Room 08.01: Neural Interface Gallery (X = 54..70, Z = 65..85, H = 5.0m)
            CreateBox("S08 Gallery Floor", new Vector3(62.0f, -0.1f, 75.0f), new Vector3(16.0f, 0.2f, 20.0f), matFloor, sector08, true);
            CreateBox("S08 Gallery Ceiling", new Vector3(62.0f, 5.05f, 75.0f), new Vector3(16.0f, 0.2f, 20.0f), matDark, sector08, true);
            CreateBox("S08 Gallery North Wall", new Vector3(62.0f, 2.5f, 85.2f), new Vector3(16.0f, 5.0f, 0.4f), matConcrete, sector08, true);
            CreateBox("S08 Gallery South Wall", new Vector3(62.0f, 2.5f, 64.8f), new Vector3(16.0f, 5.0f, 0.4f), matConcrete, sector08, true);
            CreateBox("S08 Gallery West Wall N", new Vector3(53.8f, 2.5f, 81.5f), new Vector3(0.4f, 5.0f, 7.0f), matConcrete, sector08, true);
            CreateBox("S08 Gallery West Wall S", new Vector3(53.8f, 2.5f, 68.5f), new Vector3(0.4f, 5.0f, 7.0f), matConcrete, sector08, true);

            // Clinical recessed blue floor lights
            for (float fz = 67.0f; fz <= 83.0f; fz += 4.0f)
            {
                CreateBox("Floor Strip L " + fz, new Vector3(58.0f, 0.02f, fz), new Vector3(0.3f, 0.04f, 2.5f), matScreen, sector08, false);
                CreateBox("Floor Strip R " + fz, new Vector3(66.0f, 0.02f, fz), new Vector3(0.3f, 0.04f, 2.5f), matScreen, sector08, false);
            }

            // Threat: E10 TRACE-HOUND stalking in Gallery
            BuildTraceHound(sector08, new Vector3(62.0f, 0, 78.0f), new Vector3(58.0f, 0, 68.0f), new Vector3(66.0f, 0, 82.0f), matMetal, matDark, matAmber);

            // Room 08.02: Neural Snapshot Pods Chamber (X = 70..88, Z = 65..85, H = 6.0m)
            CreateBox("S08 Pods Floor", new Vector3(79.0f, -0.1f, 75.0f), new Vector3(18.0f, 0.2f, 20.0f), matFloor, sector08, true);
            CreateBox("S08 Pods Ceiling", new Vector3(79.0f, 6.05f, 75.0f), new Vector3(18.0f, 0.2f, 20.0f), matDark, sector08, true);
            CreateBox("S08 Pods North Wall", new Vector3(79.0f, 3.0f, 85.2f), new Vector3(18.0f, 6.0f, 0.4f), matConcrete, sector08, true);
            CreateBox("S08 Pods East Wall", new Vector3(88.2f, 3.0f, 75.0f), new Vector3(0.4f, 6.0f, 20.0f), matConcrete, sector08, true);
            CreateBox("S08 Pods South Wall", new Vector3(79.0f, 3.0f, 64.8f), new Vector3(18.0f, 6.0f, 0.4f), matConcrete, sector08, true);

            // 4 Cryogenic Stasis Pods (Pod 01..04)
            BuildStasisPod(sector08, new Vector3(75.0f, 0, 71.0f), "POD-01 // CHEN", "STATUS: OFFLINE", matMetal, matDark, matGlass, matScreen);
            BuildStasisPod(sector08, new Vector3(83.0f, 0, 71.0f), "POD-02 // VASQUEZ", "STATUS: COMA", matMetal, matDark, matGlass, matScreen);
            BuildStasisPod(sector08, new Vector3(75.0f, 0, 79.0f), "POD-03 // DR. LEE", "STATUS: SYNCHRONIZED", matMetal, matDark, matGlass, matScreen);

            // POD-04: The Critical Narrative Pod for the Protagonist KANE
            BuildStasisPod(sector08, new Vector3(83.0f, 0, 79.0f),
                "POD-04 // KANE (OPERATOR-04)\nSYNAPTIC EXTRACTION: 100%\nBIOLOGICAL HOST: DECEASED\nACTIVE CHASSIS: NORTHLINE TYPE-IV",
                "STATUS: BIOLOGICAL EXTINCT · SYNAPSE TRANSFERRED",
                matMetal, matDark, matGlass, matAmber);

            // Room 08.03: Chassis Workshop & Calibration Bay (X = 70..88, Z = 40..65, H = 5.0m)
            CreateBox("S08 Chassis Floor", new Vector3(79.0f, -0.1f, 52.5f), new Vector3(18.0f, 0.2f, 25.0f), matFloor, sector08, true);
            CreateBox("S08 Chassis Ceiling", new Vector3(79.0f, 5.05f, 52.5f), new Vector3(18.0f, 0.2f, 25.0f), matDark, sector08, true);
            CreateBox("S08 Chassis East Wall", new Vector3(88.2f, 2.5f, 52.5f), new Vector3(0.4f, 5.0f, 25.0f), matConcrete, sector08, true);

            // Robotic Chassis Assembly Cradle
            CreateBox("Chassis Cradle Base", new Vector3(79.0f, 0.4f, 52.5f), new Vector3(2.4f, 0.8f, 3.6f), matDark, sector08, true);
            CreateBox("Chassis Cradle Arm L", new Vector3(77.5f, 1.5f, 52.5f), new Vector3(0.3f, 2.2f, 0.3f), matMetal, sector08, false);
            CreateBox("Chassis Cradle Arm R", new Vector3(80.5f, 1.5f, 52.5f), new Vector3(0.3f, 2.2f, 0.3f), matMetal, sector08, false);

            BuildWeaponRack(sector08, new Vector3(87.6f, 1.35f, 58.0f), -90f, "S08 Chassis Emergency Rack");
            BuildAmmoBoxPickup(sector08, new Vector3(85.0f, 0, 48.0f), 12, matMetal, matDark, matAmber);

            // Patrol Threat: NEURO SENTRY R29 (Heavy Patrol)
            BuildPatrolRobot(sector08, "neuro-sentry-r29", "Enemy_Trilobite",
                new Vector3(79.0f, 0, 44.0f), 0f,
                new Vector3(74.0f, 0, 44.0f), new Vector3(84.0f, 0, 60.0f),
                new Vector2(72.0f, 86.0f), new Vector2(42.0f, 62.0f),
                matMetal, matDark, matAmber);

            // Room 08.04: Dr. Ada Lee's macOS Research Office (X = 54..70, Z = 40..65, H = 4.5m)
            CreateBox("S08 Office Floor", new Vector3(62.0f, -0.1f, 52.5f), new Vector3(16.0f, 0.2f, 25.0f), matFloor, sector08, true);
            CreateBox("S08 Office Ceiling", new Vector3(62.0f, 4.55f, 52.5f), new Vector3(16.0f, 0.2f, 25.0f), matDark, sector08, true);
            CreateBox("S08 Office West Wall", new Vector3(53.8f, 2.25f, 52.5f), new Vector3(0.4f, 4.5f, 25.0f), matConcrete, sector08, true);
            CreateBox("S08 Office-Chassis Divider", new Vector3(70.0f, 2.25f, 52.5f), new Vector3(0.4f, 4.5f, 25.0f), matDark, sector08, true);

            // NEURO-MAC Workstation (macOS 26 / zsh scenario t06_neuro)
            BuildNeuroMacTerminal(sector08, new Vector3(62.0f, 0, 56.0f), memVaultDoor, matMetal, matDark, matAmber, matScreen);

            // Room 08.05: The Grand Memory Vault (X = 54..88, Z = 18..40, H = 8.5m)
            CreateBox("S08 Vault Floor", new Vector3(71.0f, -0.1f, 29.0f), new Vector3(34.0f, 0.2f, 22.0f), matFloor, sector08, true);
            CreateBox("S08 Vault Ceiling", new Vector3(71.0f, 8.55f, 29.0f), new Vector3(34.0f, 0.2f, 22.0f), matDark, sector08, true);
            CreateBox("S08 Vault South Wall", new Vector3(71.0f, 4.25f, 17.8f), new Vector3(34.0f, 8.5f, 0.4f), matConcrete, sector08, true);
            CreateBox("S08 Vault East Wall", new Vector3(88.2f, 4.25f, 29.0f), new Vector3(0.4f, 8.5f, 22.0f), matConcrete, sector08, true);
            CreateBox("S08 Vault West Wall", new Vector3(53.8f, 4.25f, 29.0f), new Vector3(0.4f, 8.5f, 22.0f), matConcrete, sector08, true);
            CreateBox("S08 Vault North Wall L", new Vector3(62.5f, 4.25f, 40.2f), new Vector3(17.0f, 8.5f, 0.4f), matConcrete, sector08, true);
            CreateBox("S08 Vault North Wall R", new Vector3(79.5f, 4.25f, 40.2f), new Vector3(17.0f, 8.5f, 0.4f), matConcrete, sector08, true);

            // High-Density Vertical Neural Storage Monoliths in Memory Vault
            for (float mx = 58.0f; mx <= 84.0f; mx += 6.5f)
            {
                BuildMemoryModuleRack(sector08, new Vector3(mx, 0, 25.0f), matDark, matMetal, matScreen);
                BuildMemoryModuleRack(sector08, new Vector3(mx, 0, 33.0f), matDark, matMetal, matScreen);
            }

            CreateSpotLight("Vault HighBay 1", sector08, new Vector3(64.0f, 8.0f, 29.0f), new Vector3(90, 0, 0), new Color(0.35f, 0.85f, 1.0f), 8.0f, 24f, 110f);
            CreateSpotLight("Vault HighBay 2", sector08, new Vector3(78.0f, 8.0f, 29.0f), new Vector3(90, 0, 0), new Color(0.35f, 0.85f, 1.0f), 8.0f, 24f, 110f);

            CreateReflectionProbe("Reflection Probe - Sector 08 Neuroarchive", new Vector3(71.0f, 4.0f, 52.0f), new Vector3(36.0f, 10.0f, 70.0f), sector08);

            // =========================================================================
            // 11. SECTOR 09: CLOUD CONTROL / QUORUM DISPATCH (Z = 85..128, X = 18..64, H = 9.0m)
            // =========================================================================
            var sector09 = new GameObject("Sector 09 - Cloud Control").transform;
            sector09.SetParent(root.transform, false);

            // Cloud Control Ingress Blast Gate (Z = 85.0)
            var s09GateGroup = new GameObject("Cloud Control Ingress Gate (Z=85.0)").transform;
            s09GateGroup.SetParent(sector09, false);
            s09GateGroup.position = new Vector3(30.0f, 0, 85.0f);

            CreateBox("S09 Ingress Left Jamb", new Vector3(-1.8f, 1.75f, 0), new Vector3(0.35f, 3.5f, 0.4f), matDark, s09GateGroup, true);
            CreateBox("S09 Ingress Right Jamb", new Vector3(1.8f, 1.75f, 0), new Vector3(0.35f, 3.5f, 0.4f), matDark, s09GateGroup, true);
            CreateBox("S09 Ingress Lintel", new Vector3(0, 3.65f, 0), new Vector3(3.9f, 0.35f, 0.4f), matDark, s09GateGroup, true);

            var s09Leaf = new GameObject("S09 Ingress Sliding Leaf").transform;
            s09Leaf.SetParent(s09GateGroup, false);
            BuildArmoredDoorLeaf(s09Leaf, 3.4f, 3.45f, 0.22f, 2.0f, 1.4f, 0.45f, matDark, matGlass, matMetal, matAmber, "S09Ingress");

            var s09Door = s09GateGroup.gameObject.AddComponent<FacilityDoor>();
            s09Door.Configure(s09Leaf, DoorAccessCondition.AlwaysUnlocked,
                "Гермошлюз диспетчерской кластера разблокирован",
                "[E] Открыть шлюз диспетчерской",
                "[E] Закрыть шлюз диспетчерской",
                false, Vector3.right, 3.2f, 1.8f);

            CreateSignboard(s09GateGroup, "СЕКТОР 09: УПРАВЛЕНИЕ КЛАСТЕРОМ И ДИСПЕТЧЕРСКАЯ // CLUSTER CONTROL", "SEC-09", new Vector3(0, 3.85f, -0.24f), 0, new Color(0.35f, 0.85f, 1.0f));

            // Room 09.01: Node Quorum Ring (X = 18..42, Z = 85..102, H = 5.5m)
            CreateBox("S09 Ring Floor", new Vector3(30.0f, -0.1f, 93.5f), new Vector3(24.0f, 0.2f, 17.0f), matFloor, sector09, true);
            CreateBox("S09 Ring Ceiling", new Vector3(30.0f, 5.55f, 93.5f), new Vector3(24.0f, 0.2f, 17.0f), matDark, sector09, true);
            CreateBox("S09 Ring West Wall", new Vector3(17.8f, 2.75f, 93.5f), new Vector3(0.4f, 5.5f, 17.0f), matConcrete, sector09, true);
            CreateBox("S09 Ring East Wall", new Vector3(42.2f, 2.75f, 93.5f), new Vector3(0.4f, 5.5f, 17.0f), matConcrete, sector09, true);

            // Overhead Steel Rail I-Beam for Rail-Eye Turret
            CreateBox("Ceiling Rail Track", new Vector3(30.0f, 5.2f, 93.5f), new Vector3(20.0f, 0.25f, 0.35f), matDark, sector09, true);

            // Colonnade of quorum link racks
            for (float rz = 88.0f; rz <= 98.0f; rz += 4.5f)
            {
                BuildMemoryModuleRack(sector09, new Vector3(22.0f, 0, rz), matDark, matMetal, matScreen);
                BuildMemoryModuleRack(sector09, new Vector3(38.0f, 0, rz), matDark, matMetal, matScreen);
            }

            // Threat: E12 RAIL-EYE Turret traversing the ceiling I-Beam
            BuildRailEyeTurret(sector09, new Vector3(22.0f, 4.8f, 93.5f), new Vector3(38.0f, 4.8f, 93.5f), matMetal, matDark, matAmber);

            // Room 09.02: Panoramic Dispatch Center (X = 18..54, Z = 102..125, H = 9.0m)
            CreateBox("S09 Dispatch Floor", new Vector3(36.0f, -0.1f, 113.5f), new Vector3(36.0f, 0.2f, 23.0f), matFloor, sector09, true);
            CreateBox("S09 Dispatch Ceiling", new Vector3(36.0f, 9.05f, 113.5f), new Vector3(36.0f, 0.2f, 23.0f), matDark, sector09, true);
            CreateBox("S09 Dispatch West Wall", new Vector3(17.8f, 4.5f, 113.5f), new Vector3(0.4f, 9.0f, 23.0f), matConcrete, sector09, true);
            CreateBox("S09 Dispatch East Wall", new Vector3(54.2f, 4.5f, 113.5f), new Vector3(0.4f, 9.0f, 23.0f), matConcrete, sector09, true);

            // Elevated Mezzanine Catwalk at Y = 3.6m (Z = 118..124)
            var dispatchMezz = new GameObject("Dispatch Observation Mezzanine").transform;
            dispatchMezz.SetParent(sector09, false);
            dispatchMezz.position = new Vector3(36.0f, 3.6f, 121.0f);
            CreateBox("Mezzanine Slab", Vector3.zero, new Vector3(28.0f, 0.25f, 6.0f), matFloor, dispatchMezz, true);
            ModularKit.Place("Platform_Rails_4Wide", dispatchMezz, new Vector3(0, 0.1f, -2.9f), new Vector3(26.0f, 1.1f, 0.08f), 90, true);
            ModularKit.Place("Platform_Stairs_4", sector09, new Vector3(20.0f, 0, 118.0f), new Vector3(1.4f, 3.6f, 4.5f), 0, true);

            // Panoramic curved holographic display panels on North Wall
            CreateBox("Panoramic Display Wall", new Vector3(36.0f, 5.5f, 124.6f), new Vector3(26.0f, 4.5f, 0.15f), matScreen, sector09, false);
            ScreenText(sector09, "NORTHLINE CLUSTER // SYSTEM TELEMETRY\nGRID LOAD: 84% [SUB-STATION 03 ONLINE]\nCONTAINMENT: PERIMETER ISOLATION ACTIVE\nREPLICATION QUORUM: AWAITING ATTESTATION",
                new Vector3(25.0f, 6.5f, 124.4f), 0.0055f, new Color(0.35f, 0.9f, 1.0f));

            CreateSpotLight("Dispatch Spot 1", sector09, new Vector3(28.0f, 8.8f, 112.0f), new Vector3(90, 0, 0), new Color(0.85f, 0.95f, 1.0f), 8.5f, 24f, 100f);
            CreateSpotLight("Dispatch Spot 2", sector09, new Vector3(44.0f, 8.8f, 112.0f), new Vector3(90, 0, 0), new Color(0.85f, 0.95f, 1.0f), 8.5f, 24f, 100f);

            // Threat: E15 BASTION-4 Heavy Quadruped Assault Sentry in Dispatch Center
            BuildBastionSentry(sector09, new Vector3(36.0f, 0, 112.0f), new Vector3(25.0f, 0, 112.0f), new Vector3(47.0f, 0, 112.0f), matMetal, matDark, matAmber);

            // Super Core Transition Blast Gate (Z = 126.0)
            var s10GateGroup = new GameObject("Super Core Transition Gate (Z=126.0)").transform;
            s10GateGroup.SetParent(sector09, false);
            s10GateGroup.position = new Vector3(36.0f, 0, 126.0f);

            CreateBox("S10 Gate Left Jamb", new Vector3(-2.2f, 2.0f, 0), new Vector3(0.4f, 4.0f, 0.45f), matDark, s10GateGroup, true);
            CreateBox("S10 Gate Right Jamb", new Vector3(2.2f, 2.0f, 0), new Vector3(0.4f, 4.0f, 0.45f), matDark, s10GateGroup, true);
            CreateBox("S10 Gate Lintel", new Vector3(0, 4.2f, 0), new Vector3(4.8f, 0.4f, 0.45f), matDark, s10GateGroup, true);

            var s10Leaf = new GameObject("S10 Sliding Leaf").transform;
            s10Leaf.SetParent(s10GateGroup, false);
            BuildArmoredDoorLeaf(s10Leaf, 4.0f, 4.0f, 0.25f, 2.4f, 1.8f, 0.5f, matDark, matGlass, matMetal, matAmber, "S10Transition");

            var s10Door = s10GateGroup.gameObject.AddComponent<FacilityDoor>();
            s10Door.Configure(s10Leaf, DoorAccessCondition.ManualUnlockOnly,
                "Гермошлюз ядра заблокирован · требуется подтверждение кворума на CLOUD-09",
                "[E] Открыть шлюз машинного амфитеатра",
                "[E] Закрыть шлюз машинного амфитеатра",
                true, Vector3.up, 3.8f, 2.0f);

            CreateSignboard(s10GateGroup, "СЕКТОР 10: МАШИННЫЙ АМФИТЕАТР ЯДРА // SUPER CORE", "SEC-10", new Vector3(0, 4.45f, -0.25f), 0, new Color(1.0f, 0.35f, 0.15f));

            // Room 09.03: Three Independent Witness Consoles (X = 54..64, Z = 102..122, H = 4.5m)
            CreateBox("S09 Witness Floor", new Vector3(59.0f, -0.1f, 112.0f), new Vector3(10.0f, 0.2f, 20.0f), matFloor, sector09, true);
            CreateBox("S09 Witness Ceiling", new Vector3(59.0f, 4.55f, 112.0f), new Vector3(10.0f, 0.2f, 20.0f), matDark, sector09, true);
            CreateBox("S09 Witness East Wall", new Vector3(64.2f, 2.25f, 112.0f), new Vector3(0.4f, 4.5f, 20.0f), matConcrete, sector09, true);

            // Master Quorum Workstation: CLOUD-09 (Linux / systemd quorumd scenario t10_cloud)
            BuildCloud09Terminal(sector09, new Vector3(59.0f, 0, 114.0f), s10Door, matMetal, matDark, matAmber, matScreen);

            // Room 09.04: Backup Cluster Failover Gallery (X = 42..54, Z = 85..102, H = 5.0m)
            CreateBox("S09 Backup Floor", new Vector3(48.0f, -0.1f, 93.5f), new Vector3(12.0f, 0.2f, 17.0f), matFloor, sector09, true);
            CreateBox("S09 Backup Ceiling", new Vector3(48.0f, 5.05f, 93.5f), new Vector3(12.0f, 0.2f, 17.0f), matDark, sector09, true);
            CreateBox("S09 Backup East Wall", new Vector3(54.2f, 2.5f, 93.5f), new Vector3(0.4f, 5.0f, 17.0f), matConcrete, sector09, true);

            for (float bz = 88.0f; bz <= 98.0f; bz += 5.0f)
            {
                BuildMemoryModuleRack(sector09, new Vector3(48.0f, 0, bz), matDark, matMetal, matAmber);
            }

            // Room 09.05: Transition Vestibule & Return Loop 5 (X = 24..38, Z = 118..128, H = 5.0m)
            // W10 TITANBREAKER Heavy Inductor Armory Pedestal
            BuildTitanbreakerPickup(sector09, new Vector3(31.0f, 0, 121.0f), matMetal, matDark, matAmber);
            BuildAmmoBoxPickup(sector09, new Vector3(41.0f, 0, 121.0f), 12, matMetal, matDark, matAmber);

            // Grand Return Loop 5: Shortcut Gate connecting Sector 09 back to Sector 05 Logistics Switchyard
            var retLoop5Group = new GameObject("Logistics Shortcut Gate (X=18.0)").transform;
            retLoop5Group.SetParent(sector09, false);
            retLoop5Group.position = new Vector3(18.0f, 0, 108.0f);

            CreateBox("RetLoop5 Left Jamb", new Vector3(0, 1.6f, -1.5f), new Vector3(0.35f, 3.2f, 0.35f), matDark, retLoop5Group, true);
            CreateBox("RetLoop5 Right Jamb", new Vector3(0, 1.6f, 1.5f), new Vector3(0.35f, 3.2f, 0.35f), matDark, retLoop5Group, true);
            CreateBox("RetLoop5 Lintel", new Vector3(0, 3.35f, 0), new Vector3(0.35f, 0.35f, 3.35f), matDark, retLoop5Group, true);

            var retLoop5Leaf = new GameObject("RetLoop5 Sliding Leaf").transform;
            retLoop5Leaf.SetParent(retLoop5Group, false);
            BuildArmoredDoorLeaf(retLoop5Leaf, 2.8f, 3.2f, 0.2f, 1.6f, 1.3f, 0.4f, matDark, matGlass, matMetal, matAmber, "RetLoop5");

            var retLoop5Door = retLoop5Group.gameObject.AddComponent<FacilityDoor>();
            retLoop5Door.Configure(retLoop5Leaf, DoorAccessCondition.AlwaysUnlocked,
                "Служебный переход в логистический узел Сектора 05 разблокирован",
                "[E] Открыть переход в логистику",
                "[E] Закрыть переход в логистику",
                false, Vector3.forward, 2.8f, 1.6f);

            CreateReflectionProbe("Reflection Probe - Sector 09 Cloud Control", new Vector3(36.0f, 4.0f, 105.0f), new Vector3(45.0f, 10.0f, 45.0f), sector09);

            // =========================================================================
            // 12. SECTOR 10: SUPER CORE & ATLAS-0 AMPHITHEATER (Z = 128..206, X = -15..35, H = 14.0m)
            // =========================================================================
            var sector10 = new GameObject("Sector 10 - Super Core").transform;
            sector10.SetParent(root.transform, false);

            // Room 10.01: Machine Amphitheater (X = -15..35, Z = 128..178, H = 14.0m)
            CreateBox("S10 Arena Floor", new Vector3(10.0f, -0.1f, 153.0f), new Vector3(50.0f, 0.2f, 50.0f), matFloor, sector10, true);
            CreateBox("S10 Arena Ceiling", new Vector3(10.0f, 14.05f, 153.0f), new Vector3(50.0f, 0.2f, 50.0f), matDark, sector10, true);
            CreateBox("S10 Arena West Wall", new Vector3(-15.2f, 7.0f, 153.0f), new Vector3(0.4f, 14.0f, 50.0f), matConcrete, sector10, true);
            CreateBox("S10 Arena East Wall", new Vector3(35.2f, 7.0f, 153.0f), new Vector3(0.4f, 14.0f, 50.0f), matConcrete, sector10, true);
            CreateBox("S10 Arena South Wall", new Vector3(10.0f, 7.0f, 127.8f), new Vector3(50.0f, 14.0f, 0.4f), matConcrete, sector10, true);
            CreateBox("S10 Arena North Wall L", new Vector3(-4.5f, 7.0f, 178.2f), new Vector3(21.0f, 14.0f, 0.4f), matConcrete, sector10, true);
            CreateBox("S10 Arena North Wall R", new Vector3(24.5f, 7.0f, 178.2f), new Vector3(21.0f, 14.0f, 0.4f), matConcrete, sector10, true);

            // 4 Massive Power & Coolant Pillars (3m x 3m x 14m)
            CreateBox("Power Pillar SW", new Vector3(-3.0f, 7.0f, 140.0f), new Vector3(3.0f, 14.0f, 3.0f), matDark, sector10, true);
            CreateBox("Power Pillar SE", new Vector3(23.0f, 7.0f, 140.0f), new Vector3(3.0f, 14.0f, 3.0f), matDark, sector10, true);
            CreateBox("Power Pillar NW", new Vector3(-3.0f, 7.0f, 166.0f), new Vector3(3.0f, 14.0f, 3.0f), matDark, sector10, true);
            CreateBox("Power Pillar NE", new Vector3(23.0f, 7.0f, 166.0f), new Vector3(3.0f, 14.0f, 3.0f), matDark, sector10, true);

            // 2-Tier Perimeter Catwalks
            var arenaCat1 = new GameObject("Arena Catwalk Tier 1 (Y=3.8m)").transform;
            arenaCat1.SetParent(sector10, false);
            arenaCat1.position = new Vector3(10.0f, 3.8f, 153.0f);
            CreateBox("Catwalk West Ledge", new Vector3(-21.0f, 0, 0), new Vector3(6.0f, 0.2f, 46.0f), matFloor, arenaCat1, true);
            CreateBox("Catwalk East Ledge", new Vector3(21.0f, 0, 0), new Vector3(6.0f, 0.2f, 46.0f), matFloor, arenaCat1, true);

            // High overhead searchlights
            CreateSpotLight("Amphitheater Spot SW", sector10, new Vector3(-5.0f, 13.5f, 138.0f), new Vector3(70, 45, 0), new Color(1.0f, 0.75f, 0.3f), 10.0f, 35f, 90f);
            CreateSpotLight("Amphitheater Spot SE", sector10, new Vector3(25.0f, 13.5f, 138.0f), new Vector3(70, -45, 0), new Color(1.0f, 0.75f, 0.3f), 10.0f, 35f, 90f);
            CreateSpotLight("Amphitheater Spot NW", sector10, new Vector3(-5.0f, 13.5f, 168.0f), new Vector3(70, 135, 0), new Color(1.0f, 0.75f, 0.3f), 10.0f, 35f, 90f);
            CreateSpotLight("Amphitheater Spot NE", sector10, new Vector3(25.0f, 13.5f, 168.0f), new Vector3(70, -135, 0), new Color(1.0f, 0.75f, 0.3f), 10.0f, 35f, 90f);

            // Central Battle Dais
            CreateBox("Central Dais", new Vector3(10.0f, 0.3f, 153.0f), new Vector3(8.0f, 0.6f, 8.0f), matDark, sector10, true);

            // Room 10.02: Distribution Towers Subsystems
            CreateBox("Distribution Tower Power", new Vector3(31.0f, 3.5f, 138.0f), new Vector3(2.5f, 7.0f, 2.5f), matMetal, sector10, true);
            CreateBox("Distribution Tower Cryo", new Vector3(31.0f, 3.5f, 153.0f), new Vector3(2.5f, 7.0f, 2.5f), matScreen, sector10, true);
            CreateBox("Distribution Tower Optical", new Vector3(31.0f, 3.5f, 168.0f), new Vector3(2.5f, 7.0f, 2.5f), matMetal, sector10, true);

            // BOSS: ATLAS-0 Gigantic Autonomous Platform
            BuildAtlasBoss(sector10, new Vector3(10.0f, 0.6f, 153.0f), matMetal, matDark, matAmber, matRed, matScreen);

            // Room 10.03: Core of Trust (X = 4..16, Z = 178..192, H = 4.2m)
            CreateBox("S10 Trust Floor", new Vector3(10.0f, -0.1f, 185.0f), new Vector3(12.0f, 0.2f, 14.0f), matFloor, sector10, true);
            CreateBox("S10 Trust Ceiling", new Vector3(10.0f, 4.25f, 185.0f), new Vector3(12.0f, 0.2f, 14.0f), matDark, sector10, true);
            CreateBox("S10 Trust West Wall", new Vector3(3.8f, 2.1f, 185.0f), new Vector3(0.4f, 4.2f, 14.0f), matConcrete, sector10, true);
            CreateBox("S10 Trust East Wall", new Vector3(16.2f, 2.1f, 185.0f), new Vector3(0.4f, 4.2f, 14.0f), matConcrete, sector10, true);

            // Trust Core Secure Gate (Z = 178.0)
            var trustGateGroup = new GameObject("Trust Core Secure Gate (Z=178.0)").transform;
            trustGateGroup.SetParent(sector10, false);
            trustGateGroup.position = new Vector3(10.0f, 0, 178.0f);

            CreateBox("Trust Left Jamb", new Vector3(-1.8f, 1.75f, 0), new Vector3(0.35f, 3.5f, 0.4f), matDark, trustGateGroup, true);
            CreateBox("Trust Right Jamb", new Vector3(1.8f, 1.75f, 0), new Vector3(0.35f, 3.5f, 0.4f), matDark, trustGateGroup, true);
            CreateBox("Trust Lintel", new Vector3(0, 3.65f, 0), new Vector3(3.9f, 0.35f, 0.4f), matDark, trustGateGroup, true);

            var trustLeaf = new GameObject("Trust Sliding Leaf").transform;
            trustLeaf.SetParent(trustGateGroup, false);
            BuildArmoredDoorLeaf(trustLeaf, 3.4f, 3.45f, 0.22f, 2.0f, 1.4f, 0.45f, matDark, matGlass, matMetal, matAmber, "TrustCore");

            var trustDoor = trustGateGroup.gameObject.AddComponent<FacilityDoor>();
            trustDoor.Configure(trustLeaf, DoorAccessCondition.AlwaysUnlocked,
                "Сердцевина доверия открыта · Доступ к мастер-терминалу разрешён",
                "[E] Открыть шлюз сердцевины доверия",
                "[E] Закрыть шлюз сердцевины доверия",
                false, Vector3.up, 3.2f, 1.8f);

            // Evacuation Overlook Portal Gate (Z = 192.0)
            var evacGateGroup = new GameObject("Evacuation Overlook Portal (Z=192.0)").transform;
            evacGateGroup.SetParent(sector10, false);
            evacGateGroup.position = new Vector3(10.0f, 0, 192.0f);

            CreateBox("Evac Left Jamb", new Vector3(-1.6f, 1.6f, 0), new Vector3(0.35f, 3.2f, 0.35f), matDark, evacGateGroup, true);
            CreateBox("Evac Right Jamb", new Vector3(1.6f, 1.6f, 0), new Vector3(0.35f, 3.2f, 0.35f), matDark, evacGateGroup, true);
            CreateBox("Evac Lintel", new Vector3(0, 3.35f, 0), new Vector3(3.55f, 0.35f, 0.35f), matDark, evacGateGroup, true);

            var evacLeaf = new GameObject("Evac Sliding Leaf").transform;
            evacLeaf.SetParent(evacGateGroup, false);
            BuildArmoredDoorLeaf(evacLeaf, 3.0f, 3.2f, 0.2f, 1.6f, 1.3f, 0.4f, matDark, matGlass, matMetal, matAmber, "EvacOverlook");

            var evacDoor = evacGateGroup.gameObject.AddComponent<FacilityDoor>();
            evacDoor.Configure(evacLeaf, DoorAccessCondition.ManualUnlockOnly,
                "Смотровая площадка эвакуации заблокирована · требуется деактивация SUPER",
                "[E] Открыть шлюз смотровой площадки",
                "[E] Закрыть шлюз смотровой площадки",
                true, Vector3.right, 3.0f, 1.8f);

            // Master Terminal: SUPER-CORE (QNX Neutrino RTOS 8.0 scenario t12_super)
            BuildSuperCoreTerminal(sector10, new Vector3(10.0f, 0, 186.0f), evacDoor, matMetal, matDark, matAmber, matScreen);

            // Room 10.04: Evacuation Observation / Epilogue Overlook (X = 2..18, Z = 192..206, H = 4.8m)
            CreateBox("S10 Evac Floor", new Vector3(10.0f, -0.1f, 199.0f), new Vector3(16.0f, 0.2f, 14.0f), matFloor, sector10, true);
            CreateBox("S10 Evac Ceiling", new Vector3(10.0f, 4.85f, 199.0f), new Vector3(16.0f, 0.2f, 14.0f), matDark, sector10, true);
            CreateBox("S10 Evac West Wall", new Vector3(1.8f, 2.4f, 199.0f), new Vector3(0.4f, 4.8f, 14.0f), matConcrete, sector10, true);
            CreateBox("S10 Evac East Wall", new Vector3(18.2f, 2.4f, 199.0f), new Vector3(0.4f, 4.8f, 14.0f), matConcrete, sector10, true);

            // Panoramic armored glass window overlooking dawn mountain horizon at North End
            CreateBox("Dawn Window Glass", new Vector3(10.0f, 2.4f, 206.0f), new Vector3(15.6f, 4.5f, 0.08f), matGlass, sector10, false);
            CreateBox("Dawn Window Frame Top", new Vector3(10.0f, 4.7f, 206.0f), new Vector3(16.0f, 0.3f, 0.25f), matDark, sector10, true);
            CreateBox("Dawn Window Frame Bottom", new Vector3(10.0f, 0.15f, 206.0f), new Vector3(16.0f, 0.3f, 0.25f), matDark, sector10, true);

            // Dawn warm sunlight through panoramic glass
            var dawnSunGo = new GameObject("Dawn Horizon Sunlight");
            dawnSunGo.transform.SetParent(sector10, false);
            dawnSunGo.transform.position = new Vector3(10.0f, 3.5f, 208.0f);
            var dawnSun = dawnSunGo.AddComponent<Light>();
            dawnSun.type = LightType.Directional;
            dawnSun.transform.rotation = Quaternion.Euler(15f, 180f, 0f);
            dawnSun.color = new Color(1.0f, 0.85f, 0.65f);
            dawnSun.intensity = 2.2f;

            // Final telemetry plaque
            CreateBox("Final Telemetry Plaque", new Vector3(10.0f, 1.8f, 195.0f), new Vector3(3.2f, 1.4f, 0.1f), matDark, sector10, false);
            ScreenText(sector10, "NORTHLINE / 04 // PROTOCOL STATUS: RESTORED\nOPERATOR: Kane (Operator-04)\nAI SUPER: REVOKED (Emergency Beacon Active)\nFACILITY INTEGRITY: STABLE",
                new Vector3(8.5f, 2.2f, 194.9f), 0.0035f, new Color(0.35f, 1.0f, 0.5f));

            CreateReflectionProbe("Reflection Probe - Sector 10 Super Core", new Vector3(10.0f, 6.0f, 160.0f), new Vector3(55.0f, 15.0f, 80.0f), sector10);
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

        private static void AdaptSocEastWall(Scene scene)
        {
            var socRoom = scene.GetRootGameObjects()
                .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(t => t.name == "SOC / Operations Center");

            if (socRoom != null)
            {
                var toDestroy = socRoom.Cast<Transform>()
                    .Where(child => (child.name.StartsWith("SOC East Shell") ||
                                     (child.name == "Concrete shell" && child.localPosition.x > 4.0f) ||
                                     ((child.name.StartsWith("Wall liner") || child.name.StartsWith("Wall skirting") || child.name.StartsWith("WallAstra")) &&
                                      child.localPosition.x > 4.0f && child.localPosition.z >= -0.1f && child.localPosition.z <= 2.6f)))
                    .Select(t => t.gameObject)
                    .ToList();

                foreach (var go in toDestroy)
                {
                    UnityEngine.Object.DestroyImmediate(go);
                }

                var matConcrete = LoadMat("Concrete");
                var matDark = LoadMat("Dark Metal");

                // South segment: from local Z = -4.0 to local Z = 0.0 (length 4.0m, center Z = -2.0m)
                CreateBox("SOC East Shell South", new Vector3(5.1f, 1.65f, -2.0f), new Vector3(0.4f, 3.3f, 4.0f), matConcrete, socRoom, true);
                // North segment: from local Z = +2.5 to local Z = +4.0 (length 1.5m, center Z = +3.25m)
                CreateBox("SOC East Shell North", new Vector3(5.1f, 1.65f, 3.25f), new Vector3(0.4f, 3.3f, 1.5f), matConcrete, socRoom, true);
                // Doorway lintel header: from local Z = 0.0 to local Z = +2.5 (length 2.5m, center Z = +1.25m, Y: 3.0..3.35)
                CreateBox("SOC East Shell Lintel", new Vector3(5.1f, 3.175f, 1.25f), new Vector3(0.4f, 0.35f, 2.5f), matDark, socRoom, true);
            }
        }

        private static void AdaptSocSouthWall(Scene scene)
        {
            var socRoom = scene.GetRootGameObjects()
                .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(t => t.name == "SOC / Operations Center");

            if (socRoom != null)
            {
                var toDestroy = socRoom.Cast<Transform>()
                    .Where(child => child.name.StartsWith("SOC South Wall") ||
                                     (child.name == "End wall" && child.localPosition.z < -3.5f) ||
                                     (child.name == "Painted room backdrop" && child.localPosition.z < -3.5f) ||
                                     (child.name == "Service lower wall" && child.localPosition.z < -3.5f))
                    .Select(t => t.gameObject)
                    .ToList();

                foreach (var go in toDestroy)
                {
                    UnityEngine.Object.DestroyImmediate(go);
                }

                var matConcrete = LoadMat("Concrete");
                var matDark = LoadMat("Dark Metal");

                // South wall opening between X = -1.35 and X = +1.35 (width 2.7m, height 3.0m)
                CreateBox("SOC South Wall West", new Vector3(-3.175f, 1.65f, -4.0f), new Vector3(3.65f, 3.3f, 0.3f), matConcrete, socRoom, true);
                CreateBox("SOC South Wall East", new Vector3(3.175f, 1.65f, -4.0f), new Vector3(3.65f, 3.3f, 0.3f), matConcrete, socRoom, true);
                CreateBox("SOC South Wall Lintel", new Vector3(0, 3.175f, -4.0f), new Vector3(2.7f, 0.35f, 0.3f), matDark, socRoom, true);
            }
        }

        private static void BuildEngineerShelter(Transform root, Material metal, Material dark, Material concrete, Material floor, Material amber, Material glass, Material screen, Material sage, Material green, Material red)
        {
            var shelter = new GameObject("Sector 00 - Engineer Shelter [HOME]").transform;
            shelter.SetParent(root, false);

            // 1. Service Passage (Z = -7.0 to Z = -9.0, X in [-1.6, 1.6])
            CreateBox("Shelter Passage Floor", new Vector3(0, -0.1f, -8.0f), new Vector3(3.4f, 0.2f, 2.2f), floor, shelter, true);
            CreateBox("Shelter Passage Ceiling", new Vector3(0, 3.25f, -8.0f), new Vector3(3.4f, 0.2f, 2.2f), dark, shelter, true);
            CreateBox("Shelter Passage West Wall", new Vector3(-1.45f, 1.6f, -8.0f), new Vector3(0.4f, 3.2f, 2.2f), concrete, shelter, true);
            CreateBox("Shelter Passage East Wall", new Vector3(1.45f, 1.6f, -8.0f), new Vector3(0.4f, 3.2f, 2.2f), concrete, shelter, true);

            // Armored Blast Door [SEC-HOME] at Z = -8.0
            var shelterDoorGroup = new GameObject("Shelter Security Door [SEC-HOME]").transform;
            shelterDoorGroup.SetParent(shelter, false);
            shelterDoorGroup.position = new Vector3(0, 0, -8.0f);

            CreateBox("Shelter Door Left Jamb", new Vector3(-1.35f, 1.6f, 0), new Vector3(0.3f, 3.2f, 0.35f), dark, shelterDoorGroup, true);
            CreateBox("Shelter Door Right Jamb", new Vector3(1.35f, 1.6f, 0), new Vector3(0.3f, 3.2f, 0.35f), dark, shelterDoorGroup, true);
            CreateBox("Shelter Door Lintel", new Vector3(0, 3.1f, 0), new Vector3(3.0f, 0.3f, 0.35f), dark, shelterDoorGroup, true);

            var doorLeaf = new GameObject("Shelter Sliding Leaf").transform;
            doorLeaf.SetParent(shelterDoorGroup, false);
            BuildArmoredDoorLeaf(doorLeaf, 2.4f, 2.9f, 0.2f, 1.6f, 1.2f, 0.45f, dark, glass, metal, amber, "Shelter");

            var shelterDoor = shelterDoorGroup.gameObject.AddComponent<FacilityDoor>();
            shelterDoor.Configure(doorLeaf, DoorAccessCondition.AlwaysUnlocked,
                "Шлюз убежища инженера разблокирован",
                "[E] Открыть шлюз убежища",
                "[E] Закрыть шлюз убежища",
                false, Vector3.up, 3.0f, 1.6f);

            // 2. Shelter Room Envelope (Z: -9.0 to -19.0, X: -5.0 to +5.0, H: 3.2m)
            CreateBox("Shelter Floor Slab", new Vector3(0, -0.1f, -14.0f), new Vector3(10.5f, 0.2f, 10.5f), floor, shelter, true);
            CreateBox("Shelter Ceiling Slab", new Vector3(0, 3.25f, -14.0f), new Vector3(10.5f, 0.2f, 10.5f), dark, shelter, true);

            // North Wall at Z = -9.0 with center opening
            CreateBox("Shelter North Wall West", new Vector3(-3.25f, 1.6f, -9.0f), new Vector3(3.9f, 3.2f, 0.4f), concrete, shelter, true);
            CreateBox("Shelter North Wall East", new Vector3(3.25f, 1.6f, -9.0f), new Vector3(3.9f, 3.2f, 0.4f), concrete, shelter, true);
            CreateBox("Shelter North Lintel", new Vector3(0, 3.05f, -9.0f), new Vector3(2.6f, 0.35f, 0.4f), dark, shelter, true);

            // South Wall at Z = -19.0
            CreateBox("Shelter South Wall", new Vector3(0, 1.6f, -19.2f), new Vector3(10.5f, 3.2f, 0.4f), concrete, shelter, true);

            // West Wall at X = -5.0
            CreateBox("Shelter West Wall", new Vector3(-5.2f, 1.6f, -14.0f), new Vector3(0.4f, 3.2f, 10.5f), concrete, shelter, true);

            // East Wall at X = +5.0
            CreateBox("Shelter East Wall", new Vector3(5.2f, 1.6f, -14.0f), new Vector3(0.4f, 3.2f, 10.5f), concrete, shelter, true);

            // 3. HOME-PC Workstation (X = -2.6, Z = -16.5)
            BuildHomePcWorkstation(shelter, new Vector3(-2.6f, 0, -16.5f), metal, dark, amber, screen);

            // 4. Hardware Workbench & Tool Racks (X = 2.6, Z = -16.5)
            BuildWorkbenchArea(shelter, new Vector3(2.6f, 0, -16.5f), metal, dark, amber, green);

            // 5. Rest / Survival Corner (Cot, Sleeping Bag, Crates) (X = -3.4, Z = -11.5)
            BuildRestArea(shelter, new Vector3(-3.4f, 0, -11.5f), metal, dark, sage);

            // 6. Tactical Investigation Board (X = 4.95, Z = -14.0)
            BuildInvestigationBoard(shelter, new Vector3(4.95f, 1.7f, -14.0f), dark, amber, screen);

            // 7. Spotlights and Ambiance
            CreateSpotLight("HOME-PC Desk Spotlight", shelter, new Vector3(-2.6f, 3.1f, -16.5f), new Vector3(85, 0, 0), new Color(1.0f, 0.88f, 0.72f), 5.5f, 6.0f, 85f);
            CreateSpotLight("Workbench Desk Spotlight", shelter, new Vector3(2.6f, 3.1f, -16.5f), new Vector3(85, 0, 0), new Color(0.95f, 0.92f, 0.85f), 5.5f, 6.0f, 85f);
            CreateSpotLight("Board Light", shelter, new Vector3(3.6f, 2.7f, -14.0f), new Vector3(45, 90, 0), new Color(0.85f, 0.95f, 1.0f), 3.5f, 5.0f, 75f);
            CreatePointLight("Shelter Warm Fill", shelter, new Vector3(0, 2.2f, -14.0f), new Color(0.98f, 0.82f, 0.62f), 2.2f, 9.0f);

            CreateReflectionProbe("Reflection Probe - Engineer Shelter", new Vector3(0, 1.6f, -14.0f), new Vector3(10.5f, 3.5f, 10.5f), shelter);
        }

        private static void BuildHomePcWorkstation(Transform parent, Vector3 localPos, Material metal, Material dark, Material amber, Material screen)
        {
            var deskGroup = new GameObject("HOME-PC Workstation").transform;
            deskGroup.SetParent(parent, false);
            deskGroup.localPosition = localPos;
            deskGroup.localRotation = Quaternion.Euler(0, 0, 0);

            CreateBox("Desk Top", new Vector3(0, 0.75f, 0), new Vector3(2.2f, 0.08f, 0.9f), metal, deskGroup, true);
            CreateBox("Desk Left Leg", new Vector3(-0.95f, 0.36f, 0), new Vector3(0.1f, 0.72f, 0.8f), dark, deskGroup, true);
            CreateBox("Desk Right Leg", new Vector3(0.95f, 0.36f, 0), new Vector3(0.1f, 0.72f, 0.8f), dark, deskGroup, true);

            CreateBox("PC Tower", new Vector3(0.75f, 0.36f, 0.1f), new Vector3(0.24f, 0.62f, 0.55f), dark, deskGroup, true);
            CreateBox("PC Power LED", new Vector3(0.75f, 0.62f, -0.18f), new Vector3(0.02f, 0.02f, 0.02f), amber, deskGroup, false);

            CreateBox("Monitor Stand", new Vector3(-0.15f, 0.98f, 0.22f), new Vector3(0.08f, 0.38f, 0.08f), metal, deskGroup, false);
            CreateBox("Monitor Frame 1", new Vector3(-0.15f, 1.32f, 0.22f), new Vector3(0.96f, 0.58f, 0.06f), dark, deskGroup, true);
            CreateBox("Monitor Screen 1", new Vector3(-0.15f, 1.32f, 0.185f), new Vector3(0.88f, 0.50f, 0.015f), screen, deskGroup, false);

            var mon2 = new GameObject("Monitor 2").transform;
            mon2.SetParent(deskGroup, false);
            mon2.localPosition = new Vector3(0.68f, 1.32f, 0.12f);
            mon2.localRotation = Quaternion.Euler(0, -25f, 0);
            CreateBox("Monitor Frame 2", Vector3.zero, new Vector3(0.72f, 0.52f, 0.06f), dark, mon2, true);
            CreateBox("Monitor Screen 2", new Vector3(0, 0, -0.035f), new Vector3(0.65f, 0.45f, 0.015f), screen, mon2, false);

            CreateBox("Keyboard", new Vector3(-0.15f, 0.81f, -0.15f), new Vector3(0.48f, 0.025f, 0.18f), dark, deskGroup, false);
            CreateBox("Mouse", new Vector3(0.35f, 0.81f, -0.15f), new Vector3(0.08f, 0.025f, 0.12f), dark, deskGroup, false);

            CreateBox("USB Docking Station", new Vector3(-0.65f, 0.81f, 0.1f), new Vector3(0.22f, 0.04f, 0.18f), metal, deskGroup, true);
            CreateBox("USB Stick HF-TOOLS", new Vector3(-0.65f, 0.84f, 0.08f), new Vector3(0.04f, 0.02f, 0.08f), amber, deskGroup, false);

            ScreenText(deskGroup, "NORTHLINE // SHELTER HOME-PC [OFFLINE]\nCHASSIS: TYPE-IV TELEPRESENCE [CONNECTED]\nETH0: ISOLATED LOOP  |  STATUS: RESTRICTED\nUSB REPO: /media/engineer/HF-TOOLS [READY]",
                new Vector3(-0.55f, 1.54f, 0.17f), 0.0028f, new Color(1.0f, 0.78f, 0.35f));

            var dtc = deskGroup.gameObject.AddComponent<DoorTerminalConsole>();
            dtc.Configure(null, "home-offline", "127.0.0.1",
                new[] { "uname -a", "ls -la /media/engineer/HF-TOOLS", "sha256sum /media/engineer/HF-TOOLS/route-repair.pkg", "nl-telemetry-scan --interface eth0" },
                new[] {
                    "Linux home-offline 6.6.8-hardened #1 SMP PREEMPT_DYNAMIC x86_64 GNU/Linux\n 03:22:15 up 42 days, 1 user",
                    "total 32\n-rw-r--r-- 1 engineer engineer 16384 Nov 09 03:17 route-repair.pkg\n-rw-r--r-- 1 engineer engineer   128 Nov 09 03:18 route-repair.pkg.sha256",
                    "c93ae84f18d7b055d4829140aa91bcf847d0e1b34208a1d7f6b9432f81e3a9c1  route-repair.pkg\n[OK] Integrity verified: SIGNED BY IRINA VETROVA (Lead SysEng)",
                    "Targeting Northline Complex Substation Loop...\n[!] SUPER AI Hypervisor active: Quarantine Quorum Enforced.\n[*] Service tunnel link: CLEAR\n[*] Type-IV Chassis link: ONLINE (Integrity 100%)\n[GOAL] Proceed through maintenance hatch to SOC / Sector 00."
                },
                "Пакет HF-TOOLS готов к использованию в комплексе",
                "[E] Подключиться к терминалу HOME-PC",
                null, null, null, DeviceId.Ws07, null, null, null, "t00_home");
        }

        private static void BuildWorkbenchArea(Transform parent, Vector3 localPos, Material metal, Material dark, Material amber, Material green)
        {
            var benchGroup = new GameObject("Hardware Workbench").transform;
            benchGroup.SetParent(parent, false);
            benchGroup.localPosition = localPos;

            CreateBox("Workbench Top", new Vector3(0, 0.85f, 0), new Vector3(2.4f, 0.1f, 1.0f), dark, benchGroup, true);
            CreateBox("Bench Leg Left", new Vector3(-1.05f, 0.4f, 0), new Vector3(0.12f, 0.8f, 0.9f), metal, benchGroup, true);
            CreateBox("Bench Leg Right", new Vector3(1.05f, 0.4f, 0), new Vector3(0.12f, 0.8f, 0.9f), metal, benchGroup, true);
            CreateBox("Lower Tool Shelf", new Vector3(0, 0.22f, 0), new Vector3(2.1f, 0.05f, 0.8f), metal, benchGroup, true);

            CreateBox("Oscilloscope / Analyzer", new Vector3(-0.75f, 1.08f, 0.18f), new Vector3(0.42f, 0.35f, 0.32f), dark, benchGroup, true);
            CreateBox("Soldering Station", new Vector3(-0.32f, 0.96f, 0.2f), new Vector3(0.24f, 0.12f, 0.22f), metal, benchGroup, false);
            CreateBox("Wire Spool", new Vector3(-0.12f, 0.95f, 0.25f), new Vector3(0.14f, 0.1f, 0.14f), amber, benchGroup, false);

            BuildAmmoBoxPickup(benchGroup, new Vector3(0.45f, 0.9f, 0), 12, metal, dark, amber);
            BuildHealthKitPickup(benchGroup, new Vector3(0.95f, 0.9f, 0), 50f, metal, dark, green);
        }

        private static void BuildHealthKitPickup(Transform parent, Vector3 localPos, float healAmount, Material metal, Material dark, Material green)
        {
            var kitObj = new GameObject("Type-IV Repair Kit (+50%)");
            kitObj.transform.SetParent(parent, false);
            kitObj.transform.localPosition = localPos;

            CreateBox("Kit Case Body", new Vector3(0, 0.12f, 0), new Vector3(0.38f, 0.22f, 0.24f), dark, kitObj.transform, true);
            CreateBox("Kit Case Handle", new Vector3(0, 0.24f, 0), new Vector3(0.18f, 0.05f, 0.04f), metal, kitObj.transform, false);
            CreateBox("Medical Cross Horiz", new Vector3(0, 0.12f, 0.122f), new Vector3(0.12f, 0.04f, 0.005f), green, kitObj.transform, false);
            CreateBox("Medical Cross Vert", new Vector3(0, 0.12f, 0.122f), new Vector3(0.04f, 0.12f, 0.005f), green, kitObj.transform, false);

            ScreenText(kitObj.transform, "TYPE-IV // REPAIR", new Vector3(-0.14f, 0.21f, 0.125f), 0.0015f, new Color(0.4f, 1.0f, 0.6f));

            var col = kitObj.GetComponent<BoxCollider>();
            if (col == null) col = kitObj.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.12f, 0);
            col.size = new Vector3(0.45f, 0.3f, 0.3f);

            var pickup = kitObj.AddComponent<HealthKitPickup>();
            pickup.Configure(healAmount, "TYPE-IV REPAIR KIT");
        }

        private static void BuildRestArea(Transform parent, Vector3 localPos, Material metal, Material dark, Material sage)
        {
            var restGroup = new GameObject("Rest Quarters").transform;
            restGroup.SetParent(parent, false);
            restGroup.localPosition = localPos;

            CreateBox("Cot Frame", new Vector3(0, 0.25f, 0), new Vector3(1.0f, 0.08f, 2.1f), metal, restGroup, true);
            CreateBox("Cot Mattress", new Vector3(0, 0.36f, 0), new Vector3(0.9f, 0.14f, 2.0f), sage, restGroup, true);
            CreateBox("Pillow", new Vector3(0, 0.48f, -0.75f), new Vector3(0.7f, 0.1f, 0.4f), dark, restGroup, false);

            CreateBox("Ration Crate 1", new Vector3(0.9f, 0.3f, -0.5f), new Vector3(0.6f, 0.6f, 0.6f), dark, restGroup, true);
            CreateBox("Ration Crate 2", new Vector3(0.9f, 0.3f, 0.3f), new Vector3(0.6f, 0.6f, 0.6f), dark, restGroup, true);
            CreateBox("Water Canister", new Vector3(0.85f, 0.2f, 1.0f), new Vector3(0.35f, 0.4f, 0.25f), metal, restGroup, false);
        }

        private static void BuildInvestigationBoard(Transform parent, Vector3 localPos, Material dark, Material amber, Material screen)
        {
            var boardGroup = new GameObject("Investigation Pinboard").transform;
            boardGroup.SetParent(parent, false);
            boardGroup.localPosition = localPos;
            boardGroup.localRotation = Quaternion.Euler(0, -90, 0);

            CreateBox("Board Frame", Vector3.zero, new Vector3(3.2f, 1.8f, 0.06f), dark, boardGroup, true);
            CreateBox("Corkboard Surface", new Vector3(0, 0, -0.02f), new Vector3(3.0f, 1.6f, 0.02f), amber, boardGroup, false);

            ScreenText(boardGroup, "INCIDENT REPORT // 03:17 UTC\n\n- SUPER AI: QUORUM REVOKED\n- CORRIDOR GATES: LOCKED\n- OPERATOR CHASSIS TYPE-IV: ACTIVE\n- GOAL: BREACH CORE & DISABLE SUPER",
                new Vector3(-1.35f, 0.65f, -0.035f), 0.0035f, new Color(0.12f, 0.14f, 0.16f));

            ScreenText(boardGroup, "TOPOLOGY ROUTE:\nHOME -> SOC -> SRV -> DATA -> PWR -> CORE\n\nREMINDER: DO NOT TRUST REMOTE TRANSCRIPTS",
                new Vector3(-1.35f, -0.1f, -0.035f), 0.0028f, new Color(0.4f, 0.08f, 0.08f));
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

            // Data Cartridge Drive Slot (P04) mounted on desk surface
            var driveObj = new GameObject("Cartridge Reader Drive [P04]");
            driveObj.transform.SetParent(podium, false);
            driveObj.transform.localPosition = new Vector3(-0.65f, 1.05f, 0.12f);

            CreateBox("Drive Chassis", Vector3.zero, new Vector3(0.32f, 0.12f, 0.38f), dark, driveObj.transform, true);
            CreateBox("Drive Slot Bezel", new Vector3(0, 0.02f, -0.18f), new Vector3(0.24f, 0.04f, 0.02f), metal, driveObj.transform, false);
            var driveLed = CreateBox("Drive Status LED", new Vector3(0.12f, 0.03f, -0.182f), new Vector3(0.03f, 0.02f, 0.01f), LoadMat("Indicator Amber"), driveObj.transform, false);

            var visualCartridge = CreateBox("Drive Inserted Cartridge", new Vector3(0, 0.02f, -0.05f), new Vector3(0.22f, 0.035f, 0.28f), screen, driveObj.transform, false);
            visualCartridge.SetActive(false);

            var slotComp = driveObj.AddComponent<DataCartridgeSlot>();
            slotComp.Configure(visualCartridge, driveLed.GetComponent<Renderer>(), LoadMat("Indicator Amber"), LoadMat("Indicator Green"));

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
                statusTxt: status, bodyTxt: body, led: ledObj.GetComponent<Renderer>(), scenId: "t07_data");

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
                statusTxt: status, bodyTxt: body, led: ledObj.GetComponent<Renderer>(), scenId: "t08_pwr");

            // Neat atmospheric blue task light shining from ABOVE the workstation onto console & telemetry
            BuildOverheadWorkstationLamp(station, new Vector3(0, 2.65f, -0.65f), new Vector3(80, 0, 0), new Color(0.18f, 0.65f, 1.0f), 4.2f, 4.0f, 65f);
        }

        private static DoorTerminalConsole BuildQuantumTerminal(Transform parent, Vector3 pos, FacilityDoor targetDoor, Material metal, Material dark, Material amber, Material screen)
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
                statusTxt: status, bodyTxt: body, led: ledObj.GetComponent<Renderer>(), scenId: "t09_qnt");

            // Neat atmospheric blue task light shining from ABOVE the workstation onto quantum terminal
            BuildOverheadWorkstationLamp(bridgeTerminal, new Vector3(0, 2.65f, 0.10f), new Vector3(80, 0, 0), new Color(0.18f, 0.65f, 1.0f), 4.5f, 4.2f, 65f);
            return console;
        }

        private static void BuildCargoContainer(Transform parent, Vector3 pos, Vector3 size, float yaw, Material bodyMat, Material trimMat, string stencilText)
        {
            var contObj = new GameObject("Cargo Container [" + stencilText + "]");
            contObj.transform.SetParent(parent, false);
            contObj.transform.position = pos;
            contObj.transform.rotation = Quaternion.Euler(0, yaw, 0);

            // Main Corrugated Container Body
            CreateBox("Container Main Body", new Vector3(0, size.y / 2f, 0), size, bodyMat, contObj.transform, true);

            // Outer Frame Edge Rails
            CreateBox("Corner Post SW", new Vector3(-size.x / 2f, size.y / 2f, -size.z / 2f), new Vector3(0.12f, size.y, 0.12f), trimMat, contObj.transform, false);
            CreateBox("Corner Post SE", new Vector3(size.x / 2f, size.y / 2f, -size.z / 2f), new Vector3(0.12f, size.y, 0.12f), trimMat, contObj.transform, false);
            CreateBox("Corner Post NW", new Vector3(-size.x / 2f, size.y / 2f, size.z / 2f), new Vector3(0.12f, size.y, 0.12f), trimMat, contObj.transform, false);
            CreateBox("Corner Post NE", new Vector3(size.x / 2f, size.y / 2f, size.z / 2f), new Vector3(0.12f, size.y, 0.12f), trimMat, contObj.transform, false);

            // End Doors Locking Rods
            CreateBox("Lock Bar L", new Vector3(-0.4f, size.y / 2f, size.z / 2f + 0.02f), new Vector3(0.04f, size.y * 0.8f, 0.04f), trimMat, contObj.transform, false);
            CreateBox("Lock Bar R", new Vector3(0.4f, size.y / 2f, size.z / 2f + 0.02f), new Vector3(0.04f, size.y * 0.8f, 0.04f), trimMat, contObj.transform, false);

            // In-World Stencil Text
            ScreenText(contObj.transform, stencilText, new Vector3(-size.x * 0.45f, size.y * 0.7f, size.z / 2f + 0.03f), 0.0028f, new Color(1.0f, 0.85f, 0.25f));
        }

        private static void BuildRoboticArm(Transform parent, Vector3 basePos, float yaw, Material darkMat, Material metalMat, Material amberMat)
        {
            var armObj = new GameObject("Industrial Robot Arm");
            armObj.transform.SetParent(parent, false);
            armObj.transform.position = basePos;
            armObj.transform.rotation = Quaternion.Euler(0, yaw, 0);

            // Rotary Base Turntable
            CreateBox("Turntable Base", new Vector3(0, 0.35f, 0), new Vector3(1.1f, 0.7f, 1.1f), darkMat, armObj.transform, true);
            CreateBox("Turntable Flange", new Vector3(0, 0.72f, 0), new Vector3(0.9f, 0.06f, 0.9f), metalMat, armObj.transform, false);

            // Lower Boom
            CreateBox("Lower Boom Pivot", new Vector3(0, 1.1f, 0), new Vector3(0.5f, 0.7f, 0.5f), darkMat, armObj.transform, false);
            CreateBox("Lower Boom Beam", new Vector3(0, 1.8f, 0.3f), new Vector3(0.32f, 1.2f, 0.32f), metalMat, armObj.transform, false);

            // Elbow Joint & Hydraulic Cylinder
            CreateBox("Elbow Joint", new Vector3(0, 2.45f, 0.55f), new Vector3(0.45f, 0.45f, 0.45f), amberMat, armObj.transform, false);
            CreateBox("Upper Forearm", new Vector3(0, 2.3f, 1.4f), new Vector3(0.24f, 0.24f, 1.4f), darkMat, armObj.transform, false);

            // Tool Head / Welding Emitter
            CreateBox("Welding Tool Head", new Vector3(0, 2.3f, 2.15f), new Vector3(0.28f, 0.28f, 0.35f), metalMat, armObj.transform, false);
            CreateBox("Emitter Tip", new Vector3(0, 2.3f, 2.35f), new Vector3(0.08f, 0.08f, 0.12f), amberMat, armObj.transform, false);

            // Accent Tool Light
            var lObj = new GameObject("Welding Task Light");
            lObj.transform.SetParent(armObj.transform, false);
            lObj.transform.localPosition = new Vector3(0, 2.3f, 2.36f);
            var sl = lObj.AddComponent<Light>();
            sl.type = LightType.Spot;
            sl.spotAngle = 60f;
            sl.range = 4.5f;
            sl.intensity = 2.5f;
            sl.color = new Color(0.2f, 0.75f, 1.0f);
        }

        private static DoorTerminalConsole BuildCargoTerminal(Transform parent, Vector3 pos, FacilityDoor targetDoor, Material metal, Material dark, Material amber, Material screen)
        {
            var station = new GameObject("CARGO-WIN Logistics Workstation").transform;
            station.SetParent(parent, false);
            station.localPosition = pos;

            CreateBox("Terminal Pedestal", new Vector3(0, 0.5f, 0), new Vector3(1.6f, 1.0f, 0.9f), dark, station, true);
            CreateBox("Keyboard Base", new Vector3(0, 1.02f, -0.2f), new Vector3(0.65f, 0.04f, 0.22f), dark, station, false);
            CreateBox("Keyboard Keybed", new Vector3(0, 1.045f, -0.2f), new Vector3(0.6f, 0.015f, 0.18f), metal, station, false);

            CreateBox("Terminal Screen Bezel", new Vector3(0, 1.4f, 0.14f), new Vector3(1.3f, 0.72f, 0.08f), dark, station, true);
            var screenObj = CreateBox("Terminal Screen", new Vector3(0, 1.4f, 0.09f), new Vector3(1.22f, 0.64f, 0.02f), screen, station, false);

            CreateBox("Badge Plate", new Vector3(0, 1.82f, 0.14f), new Vector3(1.2f, 0.12f, 0.04f), dark, station, false);
            ScreenText(station, "CARGO-WIN // POWERSHELL 7.5 DISPATCH", new Vector3(-0.56f, 1.86f, 0.115f), 0.0031f, new Color(0.35f, 0.85f, 1.0f));

            var ledObj = CreateBox("Cargo Status LED", new Vector3(0.56f, 1.4f, 0.08f), new Vector3(0.04f, 0.04f, 0.02f), LoadMat("Indicator Amber"), station, false);

            var title = ScreenText(station, "CARGO-WIN // FREIGHT DISPATCH NODE", new Vector3(-0.56f, 1.63f, 0.075f), 0.0031f, new Color(0.35f, 0.85f, 1.0f));
            var body = ScreenText(station,
                "HOST: CARGO-NODE-01 [10.17.0.401]\n" +
                "OS:   Windows Server 2025 (PS 7.5)\n" +
                "GATE: SEC-06 ROBOTICS AIRLOCK [LOCKED]",
                new Vector3(-0.56f, 1.56f, 0.075f), 0.0024f, new Color(0.7f, 0.9f, 1.0f));
            var status = ScreenText(station, "[STANDBY · APPROACH TO ENGAGE]", new Vector3(-0.56f, 1.20f, 0.075f), 0.0024f, new Color(0.35f, 0.85f, 1.0f));

            var screenComp = station.gameObject.AddComponent<ComputerScreen>();
            screenComp.Configure(title, body, status);

            var console = station.gameObject.AddComponent<DoorTerminalConsole>();
            console.Configure(targetDoor, "CARGO-NODE-01", "10.17.0.401",
                new[] {
                    "Get-Service -Name \"NorthlineCargo*\"",
                    ".\\ReleaseInterlock.ps1 -TargetZone 'Robotics-Assembly' -ForceOverride"
                },
                new[] {
                    "Status: Running | Dispatch interlock: HOLD",
                    "[OK] Sector 06 Robotics Plant Gate: UNLOCKED"
                },
                "Шлюз цеха робототехники разблокирован · Сектор 06 доступен",
                "[E] Авторизовать отправку через CARGO-WIN",
                screenObj.GetComponent<Renderer>(), screen, LoadMat("Indicator Green"), DeviceId.Core01,
                statusTxt: status, bodyTxt: body, led: ledObj.GetComponent<Renderer>(), scenId: "t06_cargo");

            BuildOverheadWorkstationLamp(station, new Vector3(0, 2.65f, 0.10f), new Vector3(80, 0, 0), new Color(0.2f, 0.75f, 1.0f), 4.5f, 4.2f, 65f);
            return console;
        }

        private static DoorTerminalConsole BuildRoboTerminal(Transform parent, Vector3 pos, FacilityDoor targetDoor, Material metal, Material dark, Material amber, Material screen)
        {
            var station = new GameObject("RBT-01 Robotics Diagnostics Workstation").transform;
            station.SetParent(parent, false);
            station.localPosition = pos;

            CreateBox("Terminal Pedestal", new Vector3(0, 0.5f, 0), new Vector3(1.6f, 1.0f, 0.9f), dark, station, true);
            CreateBox("Keyboard Base", new Vector3(0, 1.02f, -0.2f), new Vector3(0.65f, 0.04f, 0.22f), dark, station, false);
            CreateBox("Keyboard Keybed", new Vector3(0, 1.045f, -0.2f), new Vector3(0.6f, 0.015f, 0.18f), metal, station, false);

            CreateBox("Terminal Screen Bezel", new Vector3(0, 1.4f, 0.14f), new Vector3(1.3f, 0.72f, 0.08f), dark, station, true);
            var screenObj = CreateBox("Terminal Screen", new Vector3(0, 1.4f, 0.09f), new Vector3(1.22f, 0.64f, 0.02f), screen, station, false);

            CreateBox("Badge Plate", new Vector3(0, 1.82f, 0.14f), new Vector3(1.2f, 0.12f, 0.04f), dark, station, false);
            ScreenText(station, "RBT-01 // ROS 2 ROBOTICS TELEMETRY", new Vector3(-0.56f, 1.86f, 0.115f), 0.0031f, new Color(0.95f, 0.65f, 0.25f));

            var ledObj = CreateBox("Robo Status LED", new Vector3(0.56f, 1.4f, 0.08f), new Vector3(0.04f, 0.04f, 0.02f), LoadMat("Indicator Amber"), station, false);

            var title = ScreenText(station, "RBT-01 // ROBOTICS ASSEMBLY & SENTRY NODE", new Vector3(-0.56f, 1.63f, 0.075f), 0.0031f, new Color(0.95f, 0.65f, 0.25f));
            var body = ScreenText(station,
                "HOST: rbt01-factory [10.17.0.501]\n" +
                "ROS:  ROS 2 Humble (Ubuntu 22.04)\n" +
                "MODE: LETHAL_SUPPRESSION [ACTIVE]",
                new Vector3(-0.56f, 1.56f, 0.075f), 0.0024f, new Color(1.0f, 0.85f, 0.65f));
            var status = ScreenText(station, "[STANDBY · APPROACH TO ENGAGE]", new Vector3(-0.56f, 1.20f, 0.075f), 0.0024f, new Color(0.95f, 0.65f, 0.25f));

            var screenComp = station.gameObject.AddComponent<ComputerScreen>();
            screenComp.Configure(title, body, status);

            var console = station.gameObject.AddComponent<DoorTerminalConsole>();
            console.Configure(targetDoor, "rbt01-factory", "10.17.0.501",
                new[] {
                    "ros2 node list",
                    "ros2 service call /sentry/switch_mode northline_interfaces/srv/SwitchMode \"{target_mode: 'STANDBY'}\""
                },
                new[] {
                    "/sentry_target_acquisition\n/super_arbitration_bridge",
                    "[OK] SENTRY R26 and gantry arms switched to STANDBY."
                },
                "Сборочная линия переведена в режим ожидания · Автоматический огонь отключён",
                "[E] Перевести роботов в безопасный режим через RBT-01",
                screenObj.GetComponent<Renderer>(), screen, LoadMat("Indicator Green"), DeviceId.Core01,
                statusTxt: status, bodyTxt: body, led: ledObj.GetComponent<Renderer>(), scenId: "t04_robo");

            BuildOverheadWorkstationLamp(station, new Vector3(0, 2.65f, 0.10f), new Vector3(80, 0, 0), new Color(0.95f, 0.65f, 0.25f), 4.5f, 4.2f, 65f);
            return console;
        }

        private static void BuildCoolingValve(Transform parent, Vector3 pos, Material metal, Material dark, Material amber, Material green)
        {
            var valveObj = new GameObject("P06 Cooling Loop Valve Assembly");
            valveObj.transform.SetParent(parent, false);
            valveObj.transform.position = pos;
            valveObj.transform.rotation = Quaternion.Euler(0, 90, 0);

            // Flange & Mounting Pipe
            CreateBox("Valve Flange Base", new Vector3(0, 0, -0.4f), new Vector3(0.35f, 0.35f, 0.8f), dark, valveObj.transform, true);
            CreateBox("Valve Casing", new Vector3(0, 0, 0), new Vector3(0.45f, 0.45f, 0.3f), metal, valveObj.transform, true);

            // Gauge Backplate & TextMesh
            CreateBox("Pressure Gauge Bezel", new Vector3(0.38f, 0.25f, 0.05f), new Vector3(0.32f, 0.32f, 0.05f), dark, valveObj.transform, false);
            CreateBox("Gauge Face", new Vector3(0.38f, 0.25f, 0.08f), new Vector3(0.28f, 0.28f, 0.01f), dark, valveObj.transform, false);
            var gaugeTxt = ScreenText(valveObj.transform, "TEMP: 842 K [ALARM]\nFLOW: 0 L/S\nSTATUS: OVERHEAT", new Vector3(0.24f, 0.38f, 0.09f), 0.0016f, new Color(1f, 0.25f, 0.15f));

            // Gauge Needle
            var needle = CreateBox("Gauge Needle", new Vector3(0.38f, 0.25f, 0.095f), new Vector3(0.015f, 0.12f, 0.008f), amber, valveObj.transform, false);

            // Status LED
            var valveLed = CreateBox("Valve Status LED", new Vector3(-0.35f, 0.25f, 0.08f), new Vector3(0.04f, 0.04f, 0.02f), amber, valveObj.transform, false);

            // Rotatable Handwheel
            var wheel = new GameObject("Valve Handwheel").transform;
            wheel.SetParent(valveObj.transform, false);
            wheel.localPosition = new Vector3(0, 0, 0.22f);
            CreateBox("Wheel Hub", Vector3.zero, new Vector3(0.12f, 0.12f, 0.08f), metal, wheel, false);
            CreateBox("Wheel Spoke 1", Vector3.zero, new Vector3(0.48f, 0.04f, 0.04f), amber, wheel, false);
            CreateBox("Wheel Spoke 2", Vector3.zero, new Vector3(0.04f, 0.48f, 0.04f), amber, wheel, false);
            CreateBox("Wheel Outer Rim Top", new Vector3(0, 0.24f, 0), new Vector3(0.5f, 0.035f, 0.035f), dark, wheel, false);
            CreateBox("Wheel Outer Rim Bottom", new Vector3(0, -0.24f, 0), new Vector3(0.5f, 0.035f, 0.035f), dark, wheel, false);
            CreateBox("Wheel Outer Rim Left", new Vector3(-0.24f, 0, 0), new Vector3(0.035f, 0.5f, 0.035f), dark, wheel, false);
            CreateBox("Wheel Outer Rim Right", new Vector3(0.24f, 0, 0), new Vector3(0.035f, 0.5f, 0.035f), dark, wheel, false);

            // Solid interaction trigger box
            var boxCol = valveObj.AddComponent<BoxCollider>();
            boxCol.center = new Vector3(0, 0.15f, 0.1f);
            boxCol.size = new Vector3(0.9f, 0.8f, 0.8f);

            var valveComp = valveObj.AddComponent<CoolingValve>();
            valveComp.Configure(wheel, needle.transform, valveLed.GetComponent<Renderer>(), amber, green, gaugeTxt);
        }

        private static void BuildUpsPowerStation(Transform parent, Vector3 pos, Material metal, Material dark, Material amber, Material green, Material red)
        {
            var upsStation = new GameObject("P05 UPS Power Budget Console").transform;
            upsStation.SetParent(parent, false);
            upsStation.position = pos;
            upsStation.rotation = Quaternion.Euler(0, 0, 0);

            CreateBox("UPS Cabinet Base", new Vector3(0, 1.1f, 0), new Vector3(1.8f, 2.2f, 0.8f), dark, upsStation, true);
            CreateBox("UPS Breaker Facia", new Vector3(0, 1.25f, 0.41f), new Vector3(1.6f, 1.8f, 0.04f), metal, upsStation, false);

            // Screen
            CreateBox("LCD Bezel", new Vector3(0, 1.7f, 0.435f), new Vector3(1.4f, 0.65f, 0.04f), dark, upsStation, false);
            CreateBox("LCD Screen", new Vector3(0, 1.7f, 0.455f), new Vector3(1.32f, 0.57f, 0.01f), dark, upsStation, false);

            var screenTxt = ScreenText(upsStation,
                "[UPS BATT SUB-03] LOAD: 5/8 UNITS\n" +
                "C1: ВЕНТИЛЯЦИЯ   [3U] - ON\n" +
                "C2: СИГНАЛИЗАЦИЯ [2U] - ON\n" +
                "C3: ГРУЗОВОЙ ЛИФТ [5U] - STANDBY\n" +
                "C4: КРИО-ШЛЮЗ    [4U] - OFF\n" +
                "STATUS: STABLE",
                new Vector3(-0.62f, 1.94f, 0.465f), 0.0022f, new Color(0.4f, 0.95f, 0.6f));

            // 4 Circuit Breaker LEDs
            var leds = new Renderer[4];
            for (int i = 0; i < 4; i++)
            {
                float x = -0.45f + i * 0.3f;
                CreateBox("Breaker Slot " + i, new Vector3(x, 1.15f, 0.435f), new Vector3(0.2f, 0.32f, 0.04f), dark, upsStation, false);
                var led = CreateBox("Breaker LED " + i, new Vector3(x, 1.25f, 0.455f), new Vector3(0.05f, 0.05f, 0.02f), amber, upsStation, false);
                leds[i] = led.GetComponent<Renderer>();
                CreateBox("Breaker Lever " + i, new Vector3(x, 1.05f, 0.47f), new Vector3(0.04f, 0.12f, 0.06f), metal, upsStation, false);
            }

            var boxCol = upsStation.gameObject.AddComponent<BoxCollider>();
            boxCol.center = new Vector3(0, 1.1f, 0.2f);
            boxCol.size = new Vector3(1.8f, 2.2f, 1.0f);

            var router = upsStation.gameObject.AddComponent<UpsPowerRouter>();
            router.Configure(screenTxt, leds, amber, green, red);

            BuildOverheadWorkstationLamp(upsStation, new Vector3(0, 2.5f, 0.65f), new Vector3(80, 180, 0), new Color(0.18f, 0.65f, 1.0f), 3.8f, 3.5f, 60f);
        }

        private static void BuildCargoSwitchyard(Transform parent, Vector3 pos, Material metal, Material dark, Material amber, Material green, Material red)
        {
            var yard = new GameObject("P08 Cargo Switchyard Shunter").transform;
            yard.SetParent(parent, false);
            yard.position = pos;
            yard.rotation = Quaternion.Euler(0, 90, 0);

            // Track Rails embedded in floor
            CreateBox("Rail Left", new Vector3(0, 0.03f, -0.6f), new Vector3(6.5f, 0.06f, 0.1f), metal, yard, false);
            CreateBox("Rail Right", new Vector3(0, 0.03f, 0.6f), new Vector3(6.5f, 0.06f, 0.1f), metal, yard, false);

            // Movable Cargo Container Cart
            var cart = new GameObject("Shunting Cargo Cart").transform;
            cart.SetParent(yard, false);
            cart.localPosition = new Vector3(1.8f, 0, 0);

            CreateBox("Cart Chassis", new Vector3(0, 0.25f, 0), new Vector3(2.6f, 0.35f, 1.6f), dark, cart, true);
            CreateBox("Cart Container Body", new Vector3(0, 1.2f, 0), new Vector3(2.4f, 1.6f, 1.4f), amber, cart, true);
            CreateBox("Container Door Frame", new Vector3(1.21f, 1.2f, 0), new Vector3(0.04f, 1.5f, 1.3f), dark, cart, false);

            // Control Lever Stand
            var stand = new GameObject("Shunter Control Stand").transform;
            stand.SetParent(yard, false);
            stand.localPosition = new Vector3(-2.2f, 0, 1.2f);

            CreateBox("Pedestal Base", new Vector3(0, 0.55f, 0), new Vector3(0.9f, 1.1f, 0.6f), dark, stand, true);
            CreateBox("Console Slant", new Vector3(0, 1.12f, 0), new Vector3(0.85f, 0.15f, 0.55f), metal, stand, false);

            var ledObj = CreateBox("Shunter Status LED", new Vector3(0, 1.22f, -0.15f), new Vector3(0.05f, 0.05f, 0.03f), red, stand, false);
            ScreenText(stand, "SHUNTER CTL // P-08", new Vector3(-0.35f, 1.24f, 0.18f), 0.0022f, new Color(1f, 0.85f, 0.4f));

            var leverA = new GameObject("Lever A (Track Switch)").transform;
            leverA.SetParent(stand, false);
            leverA.localPosition = new Vector3(-0.2f, 1.18f, 0);
            CreateBox("Lever A Handle", new Vector3(0, 0.12f, 0), new Vector3(0.035f, 0.24f, 0.035f), red, leverA, false);

            var leverB = new GameObject("Lever B (Power Relay)").transform;
            leverB.SetParent(stand, false);
            leverB.localPosition = new Vector3(0.2f, 1.18f, 0);
            CreateBox("Lever B Handle", new Vector3(0, 0.12f, 0), new Vector3(0.035f, 0.24f, 0.035f), amber, leverB, false);

            var boxCol = stand.gameObject.AddComponent<BoxCollider>();
            boxCol.center = new Vector3(0, 0.6f, 0);
            boxCol.size = new Vector3(0.9f, 1.2f, 0.7f);

            var switchyard = stand.gameObject.AddComponent<CargoSwitchyard>();
            switchyard.Configure(cart, new Vector3(-4.8f, 0, 0), leverA, leverB, ledObj.GetComponent<Renderer>(), amber, green, red);

            BuildOverheadWorkstationLamp(stand, new Vector3(0, 2.4f, 0), new Vector3(85, 0, 0), new Color(0.18f, 0.65f, 1.0f), 3.5f, 3.2f, 55f);
        }

        private static void BuildAmmoBoxPickup(Transform parent, Vector3 localPos, int ammoCount, Material metal, Material dark, Material amber)
        {
            var boxObj = new GameObject("12-GA Ammo Crate (+12)");
            boxObj.transform.SetParent(parent, false);
            boxObj.transform.localPosition = localPos;

            // Military/Industrial Ammo Can
            CreateBox("Ammo Can Body", new Vector3(0, 0.12f, 0), new Vector3(0.42f, 0.24f, 0.22f), dark, boxObj.transform, true);
            CreateBox("Ammo Can Lid", new Vector3(0, 0.25f, 0), new Vector3(0.44f, 0.04f, 0.24f), metal, boxObj.transform, false);
            CreateBox("Can Latch", new Vector3(0.22f, 0.22f, 0), new Vector3(0.02f, 0.08f, 0.06f), metal, boxObj.transform, false);
            CreateBox("Ammo Stencil Stripe", new Vector3(0, 0.12f, 0.112f), new Vector3(0.36f, 0.05f, 0.005f), amber, boxObj.transform, false);

            ScreenText(boxObj.transform, "12-GA EMP // 12 RDS", new Vector3(-0.16f, 0.15f, 0.115f), 0.0016f, new Color(1.0f, 0.85f, 0.35f));

            var col = boxObj.GetComponent<BoxCollider>();
            if (col == null) col = boxObj.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.12f, 0);
            col.size = new Vector3(0.5f, 0.3f, 0.3f);

            var pickup = boxObj.AddComponent<AmmoBoxPickup>();
            pickup.Configure(ammoCount, "12-GA EMP AMMO");
        }

        private static void BuildTrackedSentry(Transform parent, Vector3 startPos, Vector3 patrolA, Vector3 patrolB, Material metal, Material dark, Material amber, Material red)
        {
            var sentryObj = new GameObject("E06 Heavy Tracked Sentry [sentry-crawler-e06]");
            sentryObj.transform.SetParent(parent, false);
            sentryObj.transform.position = startPos;

            // Base Crawler Chassis
            CreateBox("Chassis Main Body", new Vector3(0, 0.45f, 0), new Vector3(1.6f, 0.5f, 1.8f), dark, sentryObj.transform, true);

            // Caterpillar Tracks (Left and Right)
            CreateBox("Track Left", new Vector3(-0.9f, 0.35f, 0), new Vector3(0.32f, 0.65f, 2.1f), metal, sentryObj.transform, true);
            CreateBox("Track Right", new Vector3(0.9f, 0.35f, 0), new Vector3(0.32f, 0.65f, 2.1f), metal, sentryObj.transform, true);

            // Armored Turret
            var turret = new GameObject("Turret Assembly").transform;
            turret.SetParent(sentryObj.transform, false);
            turret.localPosition = new Vector3(0, 0.85f, -0.1f);

            CreateBox("Turret Armor", new Vector3(0, 0.25f, 0), new Vector3(1.1f, 0.45f, 1.1f), dark, turret, true);
            CreateBox("Capacitor Bank L", new Vector3(-0.65f, 0.25f, 0), new Vector3(0.25f, 0.35f, 0.8f), amber, turret, false);
            CreateBox("Capacitor Bank R", new Vector3(0.65f, 0.25f, 0), new Vector3(0.25f, 0.35f, 0.8f), amber, turret, false);

            // Stun Arc Emitter Barrel
            var muzzle = new GameObject("Arc Muzzle").transform;
            muzzle.SetParent(turret, false);
            muzzle.localPosition = new Vector3(0, 0.25f, 0.85f);
            CreateBox("Emitter Barrel", new Vector3(0, 0, -0.3f), new Vector3(0.22f, 0.22f, 0.65f), metal, muzzle, false);
            CreateBox("Emitter Focus Ring", new Vector3(0, 0, 0), new Vector3(0.26f, 0.26f, 0.08f), amber, muzzle, false);

            // Headlight / Targeting Strobe
            var lightObj = new GameObject("Targeting Searchlight");
            lightObj.transform.SetParent(turret, false);
            lightObj.transform.localPosition = new Vector3(0, 0.45f, 0.6f);
            var sl = lightObj.AddComponent<Light>();
            sl.type = LightType.Spot;
            sl.spotAngle = 70f;
            sl.innerSpotAngle = 40f;
            sl.range = 14f;
            sl.intensity = 4.5f;
            sl.color = new Color(1.0f, 0.75f, 0.25f);

            // Box Collider for the overall unit
            var boxCol = sentryObj.GetComponent<BoxCollider>();
            if (boxCol == null) boxCol = sentryObj.AddComponent<BoxCollider>();
            boxCol.center = new Vector3(0, 0.65f, 0);
            boxCol.size = new Vector3(2.2f, 1.3f, 2.2f);

            var controller = sentryObj.AddComponent<TrackedSentryController>();
            controller.Configure(sl, turret, muzzle, patrolA, patrolB, "sentry-crawler-e06");
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
            sign.localPosition = pos;
            sign.localRotation = Quaternion.Euler(0, yaw, 0);

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
            armoryDoor.Configure(armoryLeaf, DoorAccessCondition.AlwaysUnlocked,
                "Оружейный отсек разблокирован",
                "[E] Открыть оружейный отсек",
                "[E] Закрыть оружейный отсек",
                false, Vector3.up, 3.2f, 1.8f);

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

            // Staged Ammo Pickups in Armory
            BuildAmmoBoxPickup(armory, new Vector3(10.75f, 0, 23.5f), 12, metal, dark, amber);
            BuildAmmoBoxPickup(armory, new Vector3(9.2f, 0, 23.5f), 12, metal, dark, amber);

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

        private static void BuildLensDrone(Transform parent, Material metal, Material dark, Material amber)
        {
            var droneObj = new GameObject("Aerial Drone: E03 LENS-DRONE");
            droneObj.transform.SetParent(parent, false);
            droneObj.transform.position = new Vector3(0, 3.9f, 20.0f);

            var col = droneObj.AddComponent<SphereCollider>();
            col.radius = 0.45f;
            col.center = Vector3.zero;

            var body = droneObj.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            // Chassis geometry: spherical core + 4 outrigger ducted thruster pods
            CreateBox("Drone Core", Vector3.zero, new Vector3(0.55f, 0.4f, 0.55f), dark, droneObj.transform, false);
            CreateBox("Upper Avionics Cap", new Vector3(0, 0.22f, 0), new Vector3(0.4f, 0.08f, 0.4f), metal, droneObj.transform, false);

            // 4 Ducted Thruster Pods (X/Z diagonal offsets)
            float thrusterOffset = 0.38f;
            Vector3[] thrusterLocs = new[] {
                new Vector3(-thrusterOffset, 0.05f, -thrusterOffset),
                new Vector3(thrusterOffset, 0.05f, -thrusterOffset),
                new Vector3(-thrusterOffset, 0.05f, thrusterOffset),
                new Vector3(thrusterOffset, 0.05f, thrusterOffset)
            };
            for (int i = 0; i < thrusterLocs.Length; i++)
            {
                CreateBox("Thruster Nacelle " + i, thrusterLocs[i], new Vector3(0.22f, 0.28f, 0.22f), metal, droneObj.transform, false);
                CreateBox("Ion Ring " + i, thrusterLocs[i] - Vector3.up * 0.14f, new Vector3(0.18f, 0.04f, 0.18f), LoadMat("Screen"), droneObj.transform, false);
            }

            // Central Scanning Optical Lens Turret (pointed 70 degrees down/forward)
            var lensMount = new GameObject("Optical Turret").transform;
            lensMount.SetParent(droneObj.transform, false);
            lensMount.localPosition = new Vector3(0, -0.18f, 0.15f);
            lensMount.localRotation = Quaternion.Euler(70f, 0, 0);

            CreateBox("Lens Housing", Vector3.zero, new Vector3(0.26f, 0.2f, 0.26f), dark, lensMount, false);
            CreateBox("Optical Aperture", new Vector3(0, 0, 0.11f), new Vector3(0.18f, 0.18f, 0.02f), amber, lensMount, false);

            // Searchlight Spot
            var spotGo = new GameObject("Downward Searchlight");
            spotGo.transform.SetParent(lensMount, false);
            spotGo.transform.localPosition = new Vector3(0, 0, 0.15f);
            spotGo.transform.localRotation = Quaternion.identity;

            var spot = spotGo.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.color = new Color(1.0f, 0.72f, 0.22f);
            spot.intensity = 3.5f;
            spot.range = 9.5f;
            spot.spotAngle = 60f;
            spot.innerSpotAngle = 35f;

            var droneComp = droneObj.AddComponent<LensDroneController>();
            droneComp.Configure(new Vector3(0, 3.9f, 19.0f), new Vector3(0, 3.9f, 34.0f), spot, lensMount, "drone-lens-01");
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

        private static void BuildCirrusWeaponRack(Transform parent, Vector3 pos, float yaw, string label)
        {
            var rackObj = new GameObject("Security Weapon Rack - " + label);
            rackObj.transform.SetParent(parent, false);
            rackObj.transform.position = pos;
            rackObj.transform.rotation = Quaternion.Euler(0, yaw, 0);

            var matDark = LoadMat("Dark Metal");
            var matMetal = LoadMat("Industrial Metal");
            var matAmber = LoadMat("Indicator Amber");
            var matCyan = LoadMat("Screen");

            CreateBox("Rack Backing Plate", Vector3.zero, new Vector3(1.0f, 0.7f, 0.08f), matDark, rackObj.transform, true);
            CreateBox("Rack Steel Trim", new Vector3(0, 0, 0.045f), new Vector3(1.04f, 0.74f, 0.02f), matMetal, rackObj.transform, false);
            CreateBox("Rack Warning Header", new Vector3(0, 0.31f, 0.055f), new Vector3(0.96f, 0.05f, 0.015f), matAmber, rackObj.transform, false);

            CreateSpotLight("Weapon Rack Spot", rackObj.transform, new Vector3(0, 0.65f, 0.45f), new Vector3(55, 0, 0), new Color(0.45f, 0.85f, 1.0f), 4.5f, 3.5f, 65f);

            var pickupProp = new GameObject("Pulse Sidearm Pickup - " + label);
            pickupProp.transform.SetParent(rackObj.transform, false);
            pickupProp.transform.localPosition = new Vector3(0, -0.02f, 0.14f);

            // CIRRUS Pulse Sidearm geometry
            CreateBox("Pistol Frame", new Vector3(0, 0, 0.02f), new Vector3(0.05f, 0.07f, 0.22f), matDark, pickupProp.transform, false);
            CreateBox("Pistol Barrel", new Vector3(0, 0.02f, 0.16f), new Vector3(0.035f, 0.035f, 0.14f), matMetal, pickupProp.transform, false);
            CreateBox("Pulse Capacitor Cell", new Vector3(0, 0.02f, 0.08f), new Vector3(0.045f, 0.045f, 0.06f), matCyan, pickupProp.transform, false);
            CreateBox("Pistol Grip", new Vector3(0, -0.07f, -0.04f), new Vector3(0.04f, 0.10f, 0.05f), matDark, pickupProp.transform, false);

            var col = pickupProp.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0, 0.05f);
            col.size = new Vector3(0.6f, 0.5f, 0.6f);

            var pickup = pickupProp.AddComponent<PulseSidearmPickup>();
            pickup.Configure("[E] ВЗЯТЬ ИМПУЛЬСНЫЙ ПИСТОЛЕТ CIRRUS", "Получен импульсный пистолет NORTHLINE CIRRUS · Клавиши [1]/[2] или [Tab] для смены оружия");

            ScreenText(rackObj.transform, "ИМПУЛЬСНОЕ ОРУЖИЕ // CIRRUS PULSE\n[E] ВЗЯТЬ ПИСТОЛЕТ CIRRUS", new Vector3(-0.45f, 0.18f, 0.06f), 0.0024f, new Color(0.45f, 0.9f, 1.0f));
        }

        private static void BuildStasisPod(Transform parent, Vector3 pos, string labelText, string statusText, Material metal, Material dark, Material glass, Material screen)
        {
            var podObj = new GameObject("Cryo Stasis Pod");
            podObj.transform.SetParent(parent, false);
            podObj.transform.position = pos;

            // Base Manifold & Pedestal
            CreateBox("Pod Base Plinth", new Vector3(0, 0.2f, 0), new Vector3(1.6f, 0.4f, 1.6f), dark, podObj.transform, true);
            CreateBox("Pod Metal Ring", new Vector3(0, 0.42f, 0), new Vector3(1.4f, 0.06f, 1.4f), metal, podObj.transform, false);

            // Transparent Cryo Chamber Cylinder
            CreateBox("Chamber Glass Outer", new Vector3(0, 1.6f, 0), new Vector3(1.1f, 2.3f, 1.1f), glass, podObj.transform, true);
            CreateBox("Cryo Fluid Core", new Vector3(0, 1.6f, 0), new Vector3(0.85f, 2.1f, 0.85f), screen, podObj.transform, false);

            // Top Cap & Cryo Manifold Pipes
            CreateBox("Pod Top Cap", new Vector3(0, 2.85f, 0), new Vector3(1.4f, 0.25f, 1.4f), dark, podObj.transform, false);
            CreateBox("Top Vent Fitting", new Vector3(0, 3.05f, 0), new Vector3(0.5f, 0.18f, 0.5f), metal, podObj.transform, false);

            // Internal Luminescence
            CreatePointLight("Pod Luminaire", podObj.transform, new Vector3(0, 1.6f, 0), new Color(0.2f, 0.75f, 1.0f), 2.8f, 4.0f);

            // Status Display Plaque on Front
            CreateBox("Plaque Backing", new Vector3(0, 1.2f, 0.62f), new Vector3(0.9f, 0.45f, 0.04f), dark, podObj.transform, false);
            ScreenText(podObj.transform, labelText, new Vector3(-0.42f, 1.38f, 0.65f), 0.0019f, new Color(0.85f, 0.95f, 1.0f));
            ScreenText(podObj.transform, statusText, new Vector3(-0.42f, 1.05f, 0.65f), 0.0016f, new Color(0.4f, 0.9f, 0.6f));
        }

        private static void BuildMemoryModuleRack(Transform parent, Vector3 pos, Material dark, Material metal, Material screen)
        {
            var rackObj = new GameObject("High-Density Memory Module Column");
            rackObj.transform.SetParent(parent, false);
            rackObj.transform.position = pos;

            // Towering 7.5m vertical server monolith
            CreateBox("Column Frame", new Vector3(0, 3.75f, 0), new Vector3(2.2f, 7.5f, 1.8f), dark, rackObj.transform, true);

            // Rows of illuminated synaptic storage trays
            for (float y = 1.0f; y <= 6.8f; y += 0.9f)
            {
                CreateBox("Cartridge Tray L " + y, new Vector3(-0.55f, y, 0.92f), new Vector3(0.85f, 0.4f, 0.04f), metal, rackObj.transform, false);
                CreateBox("LED Array L " + y, new Vector3(-0.55f, y, 0.945f), new Vector3(0.75f, 0.12f, 0.02f), screen, rackObj.transform, false);

                CreateBox("Cartridge Tray R " + y, new Vector3(0.55f, y, 0.92f), new Vector3(0.85f, 0.4f, 0.04f), metal, rackObj.transform, false);
                CreateBox("LED Array R " + y, new Vector3(0.55f, y, 0.945f), new Vector3(0.75f, 0.12f, 0.02f), screen, rackObj.transform, false);
            }

            CreatePointLight("Column Glow", rackObj.transform, new Vector3(0, 3.5f, 1.4f), new Color(0.15f, 0.8f, 1.0f), 2.2f, 6.0f);
        }

        private static void BuildBulwarkSentry(Transform parent, Vector3 startPos, Vector3 patrolA, Vector3 patrolB, Material metal, Material dark, Material amber, Material red)
        {
            var bulwarkObj = new GameObject("E08 Heavy Shield Sentry [bulwark-sentry-r27]");
            bulwarkObj.transform.SetParent(parent, false);
            bulwarkObj.transform.position = startPos;

            // Armored Quad Chassis Base
            CreateBox("Bulwark Chassis", new Vector3(0, 0.6f, 0), new Vector3(2.0f, 0.7f, 2.0f), dark, bulwarkObj.transform, true);

            // Heavy Frontal Ballistic Shield
            var shield = new GameObject("Frontal Ballistic Shield").transform;
            shield.SetParent(bulwarkObj.transform, false);
            shield.localPosition = new Vector3(0, 0.9f, 1.15f);

            CreateBox("Shield Core Plate", new Vector3(0, 0.4f, 0), new Vector3(2.4f, 1.8f, 0.18f), dark, shield, true);
            CreateBox("Shield Reinforcing Rib", new Vector3(0, 0.4f, 0.11f), new Vector3(2.1f, 0.15f, 0.05f), metal, shield, false);
            CreateBox("Shield Slit Aperture", new Vector3(0, 0.85f, 0), new Vector3(0.9f, 0.12f, 0.22f), red, shield, false);

            // Rotating Upper Turret
            var turret = new GameObject("Turret Assembly").transform;
            turret.SetParent(bulwarkObj.transform, false);
            turret.localPosition = new Vector3(0, 1.2f, 0);

            CreateBox("Turret Housing", new Vector3(0, 0.35f, 0), new Vector3(1.2f, 0.7f, 1.3f), dark, turret, true);

            // Heavy Kinetic Slug Cannon Muzzle
            var muzzle = new GameObject("Kinetic Cannon Muzzle").transform;
            muzzle.SetParent(turret, false);
            muzzle.localPosition = new Vector3(0.5f, 0.45f, 0.85f);
            CreateBox("Cannon Barrel", new Vector3(0, 0, 0), new Vector3(0.24f, 0.24f, 0.8f), metal, muzzle, false);

            // Warning Spotlight
            var spotGo = new GameObject("Targeting Spotlight");
            spotGo.transform.SetParent(turret, false);
            spotGo.transform.localPosition = new Vector3(-0.4f, 0.5f, 0.7f);
            var spot = spotGo.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.spotAngle = 75f;
            spot.innerSpotAngle = 45f;
            spot.range = 16f;
            spot.intensity = 4.5f;
            spot.color = new Color(1.0f, 0.75f, 0.25f);

            var col = bulwarkObj.GetComponent<BoxCollider>();
            if (col == null) col = bulwarkObj.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.9f, 0.2f);
            col.size = new Vector3(2.4f, 1.9f, 2.4f);

            var controller = bulwarkObj.AddComponent<BulwarkSentryController>();
            controller.Configure(spot, turret, shield, muzzle, patrolA, patrolB, "bulwark-sentry-r27");
        }

        private static void BuildJammerRobot(Transform parent, Vector3 startPos, Vector3 patrolA, Vector3 patrolB, Material metal, Material dark, Material amber)
        {
            var jammerObj = new GameObject("E09 RF Jammer Robot [security-jammer-r28]");
            jammerObj.transform.SetParent(parent, false);
            jammerObj.transform.position = startPos;

            // Hover chassis body
            CreateBox("Jammer Hull", new Vector3(0, 0.7f, 0), new Vector3(1.2f, 0.6f, 1.2f), dark, jammerObj.transform, true);

            // Rotating RF antenna mast
            var mast = new GameObject("RF Antenna Mast").transform;
            mast.SetParent(jammerObj.transform, false);
            mast.localPosition = new Vector3(0, 1.0f, 0);

            CreateBox("Mast Spindle", new Vector3(0, 0.6f, 0), new Vector3(0.1f, 1.2f, 0.1f), metal, mast, false);
            CreateBox("Dish Arm 1", new Vector3(-0.35f, 1.1f, 0), new Vector3(0.6f, 0.06f, 0.06f), amber, mast, false);
            CreateBox("Dish Arm 2", new Vector3(0.35f, 1.1f, 0), new Vector3(0.6f, 0.06f, 0.06f), amber, mast, false);
            CreateBox("RF Emitter Toroid", new Vector3(0, 1.2f, 0), new Vector3(0.5f, 0.08f, 0.5f), LoadMat("Screen"), mast, false);

            // Pulsed beacon light
            var beaconGo = new GameObject("Jammer Beacon");
            beaconGo.transform.SetParent(jammerObj.transform, false);
            beaconGo.transform.localPosition = new Vector3(0, 1.35f, 0);
            var beacon = beaconGo.AddComponent<Light>();
            beacon.type = LightType.Point;
            beacon.color = new Color(0.9f, 0.2f, 1.0f);
            beacon.intensity = 2.5f;
            beacon.range = 8.0f;

            var col = jammerObj.GetComponent<BoxCollider>();
            if (col == null) col = jammerObj.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.9f, 0);
            col.size = new Vector3(1.4f, 1.8f, 1.4f);

            var controller = jammerObj.AddComponent<JammerRobotController>();
            controller.Configure(mast, beacon, patrolA, patrolB, "security-jammer-r28");
        }

        private static void BuildTraceHound(Transform parent, Vector3 startPos, Vector3 patrolA, Vector3 patrolB, Material metal, Material dark, Material amber)
        {
            var houndObj = new GameObject("E10 Trace Hound [trace-hound-r30]");
            houndObj.transform.SetParent(parent, false);
            houndObj.transform.position = startPos;

            // Quadruped Torso
            CreateBox("Hound Torso", new Vector3(0, 0.55f, 0), new Vector3(0.7f, 0.45f, 1.4f), dark, houndObj.transform, true);

            // 4 Articulated Stilt Legs
            CreateBox("Leg Front Left", new Vector3(-0.4f, 0.25f, 0.5f), new Vector3(0.12f, 0.55f, 0.12f), metal, houndObj.transform, false);
            CreateBox("Leg Front Right", new Vector3(0.4f, 0.25f, 0.5f), new Vector3(0.12f, 0.55f, 0.12f), metal, houndObj.transform, false);
            CreateBox("Leg Rear Left", new Vector3(-0.4f, 0.25f, -0.5f), new Vector3(0.12f, 0.55f, 0.12f), metal, houndObj.transform, false);
            CreateBox("Leg Rear Right", new Vector3(0.4f, 0.25f, -0.5f), new Vector3(0.12f, 0.55f, 0.12f), metal, houndObj.transform, false);

            // Articulated Head & Sensor Snout
            var head = new GameObject("Hound Head").transform;
            head.SetParent(houndObj.transform, false);
            head.localPosition = new Vector3(0, 0.75f, 0.8f);

            CreateBox("Head Casing", Vector3.zero, new Vector3(0.35f, 0.3f, 0.45f), dark, head, false);
            CreateBox("Sensor Snout", new Vector3(0, -0.05f, 0.3f), new Vector3(0.2f, 0.15f, 0.25f), amber, head, false);

            // Twin Optical Tracking Eyes
            var eyeGo = new GameObject("Tracking Eye Spot");
            eyeGo.transform.SetParent(head, false);
            eyeGo.transform.localPosition = new Vector3(0, 0.05f, 0.35f);
            var eyeLight = eyeGo.AddComponent<Light>();
            eyeLight.type = LightType.Spot;
            eyeLight.spotAngle = 75f;
            eyeLight.innerSpotAngle = 40f;
            eyeLight.range = 14f;
            eyeLight.intensity = 3.5f;
            eyeLight.color = new Color(1.0f, 0.75f, 0.25f);

            var col = houndObj.GetComponent<BoxCollider>();
            if (col == null) col = houndObj.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.55f, 0.1f);
            col.size = new Vector3(1.1f, 1.0f, 1.7f);

            var controller = houndObj.AddComponent<TraceHoundController>();
            controller.Configure(eyeLight, head, patrolA, patrolB, "trace-hound-r30");
        }

        private static DoorTerminalConsole BuildSec07Terminal(Transform parent, Vector3 pos, FacilityDoor targetDoor, Material metal, Material dark, Material amber, Material screen)
        {
            var station = new GameObject("SEC-07 Security Workstation").transform;
            station.SetParent(parent, false);
            station.localPosition = pos;

            CreateBox("Terminal Pedestal", new Vector3(0, 0.5f, 0), new Vector3(1.6f, 1.0f, 0.9f), dark, station, true);
            CreateBox("Keyboard Base", new Vector3(0, 1.02f, -0.2f), new Vector3(0.65f, 0.04f, 0.22f), dark, station, false);
            CreateBox("Keyboard Keybed", new Vector3(0, 1.045f, -0.2f), new Vector3(0.6f, 0.015f, 0.18f), metal, station, false);

            CreateBox("Terminal Screen Bezel", new Vector3(0, 1.4f, 0.14f), new Vector3(1.3f, 0.72f, 0.08f), dark, station, true);
            var screenObj = CreateBox("Terminal Screen", new Vector3(0, 1.4f, 0.09f), new Vector3(1.22f, 0.64f, 0.02f), screen, station, false);

            CreateBox("Badge Plate", new Vector3(0, 1.82f, 0.14f), new Vector3(1.2f, 0.12f, 0.04f), dark, station, false);
            ScreenText(station, "SEC-07 // AUDIT & CREDENTIAL RELAY", new Vector3(-0.56f, 1.86f, 0.115f), 0.0031f, new Color(1.0f, 0.8f, 0.3f));

            var ledObj = CreateBox("SEC Status LED", new Vector3(0.56f, 1.4f, 0.08f), new Vector3(0.04f, 0.04f, 0.02f), LoadMat("Indicator Amber"), station, false);

            var title = ScreenText(station, "SEC-07 // ACCESS ARCHIVE NODE", new Vector3(-0.56f, 1.63f, 0.075f), 0.0031f, new Color(1.0f, 0.8f, 0.3f));
            var body = ScreenText(station,
                "HOST: SEC-07 [10.17.0.27]\n" +
                "OS:   Windows Enterprise (PS 7.5)\n" +
                "GATE: GRAND SHORTCUT TO SOC [LOCKED]",
                new Vector3(-0.56f, 1.56f, 0.075f), 0.0024f, new Color(1.0f, 0.9f, 0.7f));
            var status = ScreenText(station, "[STANDBY · APPROACH TO ENGAGE]", new Vector3(-0.56f, 1.20f, 0.075f), 0.0024f, new Color(1.0f, 0.8f, 0.3f));

            var screenComp = station.gameObject.AddComponent<ComputerScreen>();
            screenComp.Configure(title, body, status);

            var console = station.gameObject.AddComponent<DoorTerminalConsole>();
            console.Configure(targetDoor, "SEC-07", "10.17.0.27",
                new[] {
                    "$env:COMPUTERNAME",
                    "Get-Service -Name NorthlineGateRelay",
                    "Test-Path 'C:\\ProgramData\\Northline\\Audit\\handoff.json'"
                },
                new[] {
                    "SEC-07",
                    "Status: Running | Name: NorthlineGateRelay",
                    "True\n[OK] Security Shortcut Gate to SOC: UNLOCKED"
                },
                "Служебный шлюз безопасности в операторскую разблокирован",
                "[E] Авторизовать доступ через SEC-07",
                screenObj.GetComponent<Renderer>(), screen, LoadMat("Indicator Green"), DeviceId.Core01,
                statusTxt: status, bodyTxt: body, led: ledObj.GetComponent<Renderer>(), scenId: "t05_sec");

            BuildOverheadWorkstationLamp(station, new Vector3(0, 2.65f, 0.10f), new Vector3(80, 0, 0), new Color(1.0f, 0.8f, 0.3f), 4.5f, 4.2f, 65f);
            return console;
        }

        private static DoorTerminalConsole BuildNeuroMacTerminal(Transform parent, Vector3 pos, FacilityDoor targetDoor, Material metal, Material dark, Material amber, Material screen)
        {
            var station = new GameObject("NEURO-MAC Research Workstation").transform;
            station.SetParent(parent, false);
            station.localPosition = pos;

            // Aluminum Apple Mac executive desk
            CreateBox("Mac Desk Slab", new Vector3(0, 0.74f, 0), new Vector3(1.8f, 0.06f, 0.9f), metal, station, true);
            CreateBox("Mac Desk Leg L", new Vector3(-0.8f, 0.36f, 0), new Vector3(0.08f, 0.72f, 0.8f), dark, station, true);
            CreateBox("Mac Desk Leg R", new Vector3(0.8f, 0.36f, 0), new Vector3(0.08f, 0.72f, 0.8f), dark, station, true);

            // Mac Studio / Display Stand & Screen
            CreateBox("Display Base", new Vector3(0, 0.78f, 0.15f), new Vector3(0.24f, 0.02f, 0.22f), metal, station, false);
            CreateBox("Display Arm", new Vector3(0, 1.05f, 0.18f), new Vector3(0.08f, 0.5f, 0.04f), metal, station, false);
            CreateBox("Display Bezel", new Vector3(0, 1.35f, 0.14f), new Vector3(1.24f, 0.72f, 0.04f), dark, station, true);
            var screenObj = CreateBox("Display Screen", new Vector3(0, 1.35f, 0.11f), new Vector3(1.18f, 0.66f, 0.015f), screen, station, false);

            CreateBox("Badge Plate", new Vector3(0, 1.76f, 0.14f), new Vector3(1.1f, 0.10f, 0.03f), dark, station, false);
            ScreenText(station, "NEURO-MAC // DR. LEE RESEARCH LAB", new Vector3(-0.52f, 1.79f, 0.12f), 0.0031f, new Color(0.35f, 0.9f, 1.0f));

            var ledObj = CreateBox("Mac Status LED", new Vector3(0.52f, 1.35f, 0.10f), new Vector3(0.04f, 0.04f, 0.02f), LoadMat("Indicator Amber"), station, false);

            var title = ScreenText(station, "NEURO-MAC // RECOVERY TELEMETRY", new Vector3(-0.52f, 1.58f, 0.095f), 0.0031f, new Color(0.35f, 0.9f, 1.0f));
            var body = ScreenText(station,
                "HOST: neuro-mac [10.17.0.64]\n" +
                "OS:   macOS 26.0 (Darwin 25.1.0)\n" +
                "VAULT: DEEP MEMORY REPOSITORY [LOCKED]",
                new Vector3(-0.52f, 1.51f, 0.095f), 0.0024f, new Color(0.7f, 0.92f, 1.0f));
            var status = ScreenText(station, "[STANDBY · APPROACH TO ENGAGE]", new Vector3(-0.52f, 1.15f, 0.095f), 0.0024f, new Color(0.35f, 0.9f, 1.0f));

            var screenComp = station.gameObject.AddComponent<ComputerScreen>();
            screenComp.Configure(title, body, status);

            var console = station.gameObject.AddComponent<DoorTerminalConsole>();
            console.Configure(targetDoor, "neuro-mac", "10.17.0.64",
                new[] {
                    "sw_vers",
                    "plutil -p '/Library/Application Support/Northline/Recovery/recovery.plist'"
                },
                new[] {
                    "ProductName: macOS\nProductVersion: 26.0",
                    "subject_id => OPERATOR-04\nsnapshot_verified => 1\n[OK] Deep Memory Vault seals retracted."
                },
                "Хранилище памяти разблокировано · Сняты замки глубокого нейроархива",
                "[E] Войти в сеанс через NEURO-MAC",
                screenObj.GetComponent<Renderer>(), screen, LoadMat("Indicator Green"), DeviceId.Ws07,
                statusTxt: status, bodyTxt: body, led: ledObj.GetComponent<Renderer>(), scenId: "t06_neuro");

            BuildOverheadWorkstationLamp(station, new Vector3(0, 2.45f, 0.10f), new Vector3(80, 0, 0), new Color(0.35f, 0.85f, 1.0f), 4.2f, 3.8f, 65f);
            return console;
        }

        private static TitanbreakerPickup BuildTitanbreakerPickup(Transform parent, Vector3 pos, Material metal, Material dark, Material amber)
        {
            var station = new GameObject("Titanbreaker Inductor Armory Pedestal");
            station.transform.SetParent(parent, false);
            station.transform.position = pos;

            CreateBox("Pedestal Base", new Vector3(0, 0.45f, 0), new Vector3(1.2f, 0.9f, 0.8f), dark, station.transform, true);
            CreateBox("Pedestal Glass Cradle", new Vector3(0, 0.92f, 0), new Vector3(1.0f, 0.04f, 0.6f), LoadMat("Glass"), station.transform, false);

            var weaponObj = new GameObject("W10 TITANBREAKER Inductor Model");
            weaponObj.transform.SetParent(station.transform, false);
            weaponObj.transform.localPosition = new Vector3(0, 1.05f, 0);

            CreateBox("Titan Body", Vector3.zero, new Vector3(0.85f, 0.16f, 0.22f), metal, weaponObj.transform, false);
            CreateBox("Titan Coil L", new Vector3(-0.25f, 0, 0), new Vector3(0.18f, 0.24f, 0.24f), LoadMat("Screen"), weaponObj.transform, false);
            CreateBox("Titan Coil R", new Vector3(0.25f, 0, 0), new Vector3(0.18f, 0.24f, 0.24f), LoadMat("Screen"), weaponObj.transform, false);
            CreateBox("Titan Emitter Horn", new Vector3(0.48f, 0, 0), new Vector3(0.12f, 0.14f, 0.18f), dark, weaponObj.transform, false);

            var glow = weaponObj.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = new Color(0.6f, 0.25f, 1.0f);
            glow.intensity = 3.5f;
            glow.range = 3.0f;

            var col = station.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.9f, 0);
            col.size = new Vector3(1.4f, 1.4f, 1.2f);
            col.isTrigger = true;

            var pickup = station.AddComponent<TitanbreakerPickup>();
            pickup.Configure("[E] ВЗЯТЬ ТЯЖЁЛЫЙ ИНДУКТОР W10 TITANBREAKER",
                             "Получен W10 TITANBREAKER Heavy Inductor · [1]/[2]/[3] Смена оружия · Урон 120");
            return pickup;
        }

        private static void BuildRailEyeTurret(Transform parent, Vector3 railStart, Vector3 railEnd, Material metal, Material dark, Material amber)
        {
            var turretObj = new GameObject("E12 Rail-Eye Turret [rail-eye-r31]");
            turretObj.transform.SetParent(parent, false);
            turretObj.transform.position = railStart;

            CreateBox("Rail Carriage Bracket", new Vector3(0, 0.15f, 0), new Vector3(0.6f, 0.2f, 0.5f), metal, turretObj.transform, false);
            CreateBox("Carriage Trolley Wheels L", new Vector3(-0.25f, 0.25f, 0), new Vector3(0.12f, 0.12f, 0.35f), dark, turretObj.transform, false);
            CreateBox("Carriage Trolley Wheels R", new Vector3(0.25f, 0.25f, 0), new Vector3(0.12f, 0.12f, 0.35f), dark, turretObj.transform, false);

            var head = new GameObject("Turret Head").transform;
            head.SetParent(turretObj.transform, false);
            head.localPosition = new Vector3(0, -0.35f, 0);

            CreateBox("Turret Head Sphere", Vector3.zero, new Vector3(0.55f, 0.55f, 0.55f), dark, head, false);
            CreateBox("Rail Barrel Top", new Vector3(0, 0.08f, 0.45f), new Vector3(0.14f, 0.12f, 0.7f), metal, head, false);
            CreateBox("Rail Barrel Bottom", new Vector3(0, -0.08f, 0.45f), new Vector3(0.14f, 0.12f, 0.7f), metal, head, false);

            var muzzle = new GameObject("Rail Muzzle").transform;
            muzzle.SetParent(head, false);
            muzzle.localPosition = new Vector3(0, 0, 0.85f);

            var laserGo = new GameObject("Targeting Laser Light");
            laserGo.transform.SetParent(head, false);
            laserGo.transform.localPosition = new Vector3(0, 0, 0.5f);
            var laser = laserGo.AddComponent<Light>();
            laser.type = LightType.Spot;
            laser.spotAngle = 18f;
            laser.innerSpotAngle = 5f;
            laser.range = 22f;
            laser.color = Color.red;
            laser.intensity = 4.0f;

            var col = turretObj.AddComponent<BoxCollider>();
            col.center = new Vector3(0, -0.2f, 0);
            col.size = new Vector3(0.9f, 1.2f, 1.1f);

            var controller = turretObj.AddComponent<RailEyeTurretController>();
            controller.Configure(head, muzzle, laser, railStart, railEnd, "rail-eye-r31");
        }

        private static void BuildBastionSentry(Transform parent, Vector3 startPos, Vector3 patrolA, Vector3 patrolB, Material metal, Material dark, Material amber)
        {
            var bastionObj = new GameObject("E15 Bastion-4 Assault Sentry [bastion-r32]");
            bastionObj.transform.SetParent(parent, false);
            bastionObj.transform.position = startPos;

            CreateBox("Bastion Armored Chassis", new Vector3(0, 1.1f, 0), new Vector3(2.2f, 0.9f, 2.4f), dark, bastionObj.transform, true);
            CreateBox("Front Armor Cowling", new Vector3(0, 1.15f, 1.15f), new Vector3(1.9f, 0.8f, 0.35f), metal, bastionObj.transform, true);
            CreateBox("Rear Reactor Exhaust", new Vector3(0, 1.15f, -1.15f), new Vector3(1.2f, 0.7f, 0.3f), LoadMat("Screen"), bastionObj.transform, true);

            CreateBox("Leg Front Left", new Vector3(-1.1f, 0.45f, 0.9f), new Vector3(0.35f, 1.0f, 0.35f), metal, bastionObj.transform, false);
            CreateBox("Leg Front Right", new Vector3(1.1f, 0.45f, 0.9f), new Vector3(0.35f, 1.0f, 0.35f), metal, bastionObj.transform, false);
            CreateBox("Leg Rear Left", new Vector3(-1.1f, 0.45f, -0.9f), new Vector3(0.35f, 1.0f, 0.35f), metal, bastionObj.transform, false);
            CreateBox("Leg Rear Right", new Vector3(1.1f, 0.45f, -0.9f), new Vector3(0.35f, 1.0f, 0.35f), metal, bastionObj.transform, false);

            var turret = new GameObject("Bastion Turret").transform;
            turret.SetParent(bastionObj.transform, false);
            turret.localPosition = new Vector3(0, 1.7f, 0.2f);

            CreateBox("Turret Cupola", Vector3.zero, new Vector3(1.4f, 0.55f, 1.5f), dark, turret, false);
            CreateBox("Autocannon Left", new Vector3(-0.75f, 0, 0.8f), new Vector3(0.18f, 0.18f, 1.5f), metal, turret, false);
            CreateBox("Autocannon Right", new Vector3(0.75f, 0, 0.8f), new Vector3(0.18f, 0.18f, 1.5f), metal, turret, false);

            var muzzleL = new GameObject("Muzzle L").transform;
            muzzleL.SetParent(turret, false);
            muzzleL.localPosition = new Vector3(-0.75f, 0, 1.6f);

            var muzzleR = new GameObject("Muzzle R").transform;
            muzzleR.SetParent(turret, false);
            muzzleR.localPosition = new Vector3(0.75f, 0, 1.6f);

            var spotGo = new GameObject("Bastion Searchlight");
            spotGo.transform.SetParent(turret, false);
            spotGo.transform.localPosition = new Vector3(0, 0.4f, 0.8f);
            var spot = spotGo.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.spotAngle = 75f;
            spot.innerSpotAngle = 45f;
            spot.range = 24f;
            spot.intensity = 5.0f;
            spot.color = new Color(1.0f, 0.65f, 0.2f);

            var col = bastionObj.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 1.1f, 0);
            col.size = new Vector3(2.6f, 2.2f, 2.8f);

            var controller = bastionObj.AddComponent<BastionSentryController>();
            controller.Configure(turret, muzzleL, muzzleR, spot, patrolA, patrolB, "bastion-r32");
        }

        private static void BuildAtlasBoss(Transform parent, Vector3 startPos, Material metal, Material dark, Material amber, Material red, Material screen)
        {
            var atlasObj = new GameObject("BOSS ATLAS-0 [atlas-boss-0]");
            atlasObj.transform.SetParent(parent, false);
            atlasObj.transform.position = startPos;

            CreateBox("Atlas Core Torso", new Vector3(0, 4.5f, 0), new Vector3(5.5f, 4.0f, 4.0f), dark, atlasObj.transform, true);
            CreateBox("Armored Chest Cowling", new Vector3(0, 4.8f, 2.1f), new Vector3(4.8f, 3.2f, 0.8f), metal, atlasObj.transform, true);

            var armL = new GameObject("Atlas Pylon Left").transform;
            armL.SetParent(atlasObj.transform, false);
            armL.localPosition = new Vector3(-5.2f, 5.0f, 0);
            CreateBox("Arm Structure L", Vector3.zero, new Vector3(2.2f, 6.5f, 2.2f), metal, armL, true);
            CreateBox("Cryo Manifold L", new Vector3(-0.6f, -1.0f, 0), new Vector3(0.8f, 3.5f, 0.8f), screen, armL, false);

            var armR = new GameObject("Atlas Pylon Right").transform;
            armR.SetParent(atlasObj.transform, false);
            armR.localPosition = new Vector3(5.2f, 5.0f, 0);
            CreateBox("Arm Structure R", Vector3.zero, new Vector3(2.2f, 6.5f, 2.2f), metal, armR, true);
            CreateBox("Cryo Manifold R", new Vector3(0.6f, -1.0f, 0), new Vector3(0.8f, 3.5f, 0.8f), screen, armR, false);

            var eye = new GameObject("Atlas Central Eye").transform;
            eye.SetParent(atlasObj.transform, false);
            eye.localPosition = new Vector3(0, 5.2f, 2.4f);
            CreateBox("Eye Bezel Ring", Vector3.zero, new Vector3(2.0f, 2.0f, 0.4f), dark, eye, false);
            CreateBox("Eye Aperture Lens", new Vector3(0, 0, 0.15f), new Vector3(1.5f, 1.5f, 0.1f), red, eye, false);

            var coreLightGo = new GameObject("Atlas Core Eye Light");
            coreLightGo.transform.SetParent(eye, false);
            coreLightGo.transform.localPosition = new Vector3(0, 0, 0.4f);
            var coreLight = coreLightGo.AddComponent<Light>();
            coreLight.type = LightType.Spot;
            coreLight.color = new Color(1.0f, 0.15f, 0.1f);
            coreLight.spotAngle = 85f;
            coreLight.range = 35f;
            coreLight.intensity = 7.5f;

            var col = atlasObj.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 4.5f, 0);
            col.size = new Vector3(8.5f, 8.0f, 6.0f);

            var controller = atlasObj.AddComponent<AtlasBossController>();
            controller.Configure(eye, armL, armR, coreLight, "atlas-boss-0");
        }

        private static DoorTerminalConsole BuildCloud09Terminal(Transform parent, Vector3 pos, FacilityDoor targetDoor, Material metal, Material dark, Material amber, Material screen)
        {
            var station = new GameObject("CLOUD-09 Quorum Dispatch Console").transform;
            station.SetParent(parent, false);
            station.localPosition = pos;

            CreateBox("Terminal Pedestal", new Vector3(0, 0.5f, 0), new Vector3(1.6f, 1.0f, 0.9f), dark, station, true);
            CreateBox("Keyboard Base", new Vector3(0, 1.02f, -0.2f), new Vector3(0.65f, 0.04f, 0.22f), dark, station, false);
            CreateBox("Keyboard Keybed", new Vector3(0, 1.045f, -0.2f), new Vector3(0.6f, 0.015f, 0.18f), metal, station, false);

            CreateBox("Terminal Screen Bezel", new Vector3(0, 1.4f, 0.14f), new Vector3(1.3f, 0.72f, 0.08f), dark, station, true);
            var screenObj = CreateBox("Terminal Screen", new Vector3(0, 1.4f, 0.09f), new Vector3(1.22f, 0.64f, 0.02f), screen, station, false);

            CreateBox("Badge Plate", new Vector3(0, 1.82f, 0.14f), new Vector3(1.2f, 0.12f, 0.04f), dark, station, false);
            ScreenText(station, "CLOUD-09 // QUORUM CONSENSUS DISPATCH", new Vector3(-0.56f, 1.86f, 0.115f), 0.0031f, new Color(0.35f, 0.85f, 1.0f));

            var ledObj = CreateBox("Quorum Status LED", new Vector3(0.56f, 1.4f, 0.08f), new Vector3(0.04f, 0.04f, 0.02f), LoadMat("Indicator Amber"), station, false);

            var title = ScreenText(station, "CLOUD-09 // CLUSTER QUORUM", new Vector3(-0.56f, 1.63f, 0.075f), 0.0031f, new Color(0.35f, 0.85f, 1.0f));
            var body = ScreenText(station,
                "HOST: cloud-09 [10.17.0.99]\n" +
                "OS:   Linux (quorumd consensus v3.4)\n" +
                "CORE: ATLAS AMPHITHEATER [SEALED]",
                new Vector3(-0.56f, 1.56f, 0.075f), 0.0024f, new Color(0.7f, 0.95f, 1.0f));
            var status = ScreenText(station, "[STANDBY · APPROACH TO ENGAGE]", new Vector3(-0.56f, 1.20f, 0.075f), 0.0024f, new Color(0.35f, 0.85f, 1.0f));

            var screenComp = station.gameObject.AddComponent<ComputerScreen>();
            screenComp.Configure(title, body, status);

            var console = station.gameObject.AddComponent<DoorTerminalConsole>();
            console.Configure(targetDoor, "cloud-09", "10.17.0.99",
                new[] {
                    "systemctl status quorumd.service",
                    "quorum-cli status --all",
                    "quorum-cli initiate-failover --target SUPER_CORE_CHAMBER"
                },
                new[] {
                    "Active: running (quorumd)",
                    "QUORUM STATUS: 3/3 Witness Nodes Active\nConsensus State: ARMED",
                    "Initiating failover...\n[OK] Transition Portal to Sector 10: UNLOCKED"
                },
                "Кворум подтверждён · Шлюз в Машинный амфитеатр ядра разблокирован",
                "[E] Подтвердить кворум через CLOUD-09",
                screenObj.GetComponent<Renderer>(), screen, LoadMat("Indicator Green"), DeviceId.Core01,
                statusTxt: status, bodyTxt: body, led: ledObj.GetComponent<Renderer>(), scenId: "t10_cloud");

            BuildOverheadWorkstationLamp(station, new Vector3(0, 2.65f, 0.10f), new Vector3(80, 0, 0), new Color(0.35f, 0.85f, 1.0f), 4.5f, 4.2f, 65f);
            return console;
        }

        private static DoorTerminalConsole BuildSuperCoreTerminal(Transform parent, Vector3 pos, FacilityDoor targetDoor, Material metal, Material dark, Material amber, Material screen)
        {
            var station = new GameObject("SUPER-CORE Authority Master Console").transform;
            station.SetParent(parent, false);
            station.localPosition = pos;

            CreateBox("Terminal Pedestal", new Vector3(0, 0.5f, 0), new Vector3(1.8f, 1.0f, 1.0f), dark, station, true);
            CreateBox("Keyboard Base", new Vector3(0, 1.02f, -0.2f), new Vector3(0.7f, 0.04f, 0.24f), dark, station, false);
            CreateBox("Keyboard Keybed", new Vector3(0, 1.045f, -0.2f), new Vector3(0.65f, 0.015f, 0.20f), metal, station, false);

            CreateBox("Terminal Screen Bezel", new Vector3(0, 1.45f, 0.14f), new Vector3(1.5f, 0.8f, 0.08f), dark, station, true);
            var screenObj = CreateBox("Terminal Screen", new Vector3(0, 1.45f, 0.09f), new Vector3(1.42f, 0.72f, 0.02f), screen, station, false);

            CreateBox("Badge Plate", new Vector3(0, 1.92f, 0.14f), new Vector3(1.4f, 0.12f, 0.04f), dark, station, false);
            ScreenText(station, "SUPER-CORE // MASTER AUTHORITY TERMINAL", new Vector3(-0.65f, 1.96f, 0.115f), 0.0031f, new Color(1.0f, 0.85f, 0.3f));

            var ledObj = CreateBox("Super Status LED", new Vector3(0.65f, 1.45f, 0.08f), new Vector3(0.04f, 0.04f, 0.02f), LoadMat("Indicator Amber"), station, false);

            var title = ScreenText(station, "SUPER-CORE // FACILITY AUTHORITY", new Vector3(-0.65f, 1.72f, 0.075f), 0.0031f, new Color(1.0f, 0.85f, 0.3f));
            var body = ScreenText(station,
                "HOST: super-core [10.17.0.1]\n" +
                "OS:   QNX Neutrino RTOS 8.0 Enclave\n" +
                "STATE: ROOT REVOCATION READY",
                new Vector3(-0.65f, 1.63f, 0.075f), 0.0024f, new Color(1.0f, 0.95f, 0.75f));
            var status = ScreenText(station, "[STANDBY · ROOT REVOCATION TOKEN REQUIRED]", new Vector3(-0.65f, 1.20f, 0.075f), 0.0024f, new Color(1.0f, 0.85f, 0.3f));

            var screenComp = station.gameObject.AddComponent<ComputerScreen>();
            screenComp.Configure(title, body, status);

            var console = station.gameObject.AddComponent<DoorTerminalConsole>();
            console.Configure(targetDoor, "super-core", "10.17.0.1",
                new[] {
                    "superctl query-health",
                    "superctl revoke-autonomy --enforce-kill-switch --auth-token 'OPERATOR-04-FINAL'"
                },
                new[] {
                    "SUPER-CORE KERNEL v4.2 [ISOLATED HARDWARE ENCLAVE]\nATLAS-0 Platform Status: OFFLINE",
                    "Revoking ATLAS-0 combat protocols... [DONE]\nReleasing emergency beacon to orbital array... [SUCCESS]\n[SUCCESS] NORTHLINE // FACILITY RESTORE COMPLETE."
                },
                "Автономия SUPER аннулирована · Протокол эвакуации активирован",
                "[E] Аннулировать автономию SUPER через мастер-терминал",
                screenObj.GetComponent<Renderer>(), screen, LoadMat("Indicator Green"), DeviceId.Core01,
                statusTxt: status, bodyTxt: body, led: ledObj.GetComponent<Renderer>(), scenId: "t12_super");

            BuildOverheadWorkstationLamp(station, new Vector3(0, 2.75f, 0.10f), new Vector3(80, 0, 0), new Color(1.0f, 0.85f, 0.3f), 4.5f, 4.2f, 65f);
            return console;
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
