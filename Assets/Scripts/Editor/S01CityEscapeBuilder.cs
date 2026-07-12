using System;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class S01CityEscapeBuilder
{
    private const string RootName = "S01_CityEscape_Generated";
    private const string ModularBuildingsPath = "Assets/Models/Environment/Buildings/modular_buildings.glb";
    private const string LowPolyBuildingsPath = "Assets/Models/Environment/Buildings/low-poly_city_buildings.glb";
    private const string IndustrialPackPath = "Assets/Models/Environment/Buildings/psx_industrial_pack.glb";
    private const string RoadPackPath = "Assets/Models/Environment/Roads/modular_city_road_pack__game_ready.glb";
    private const string BarrierConePackPath = "Assets/Models/Environment/Props/Construction/barrier__traffic_cone_pack.glb";
    private const string CardboardBoxesPath = "Assets/Models/Environment/Props/Construction/cardboard_boxes.glb";
    private const string CaseBoxesPath = "Assets/Models/Environment/Props/Construction/case_boxes.glb";
    private const string ConstructionFencePath = "Assets/Models/Environment/Props/Construction/construction_fence.glb";
    private const string MinionPrefabPath = "Assets/Prefabs/Minion.prefab";
    private const string DumpsterPath = "Assets/Models/Environment/Props/Construction/dumpster_-_4096px2.glb";
    private const string FallenTreePath = "Assets/Models/Environment/Props/Construction/fallen_tree.glb";
    private const string RockDebrisPath = "Assets/Models/Environment/Props/Construction/rock_debris_1.glb";
    private const string WoodenCratePath = "Assets/Models/Environment/Props/Construction/wooden_crate_-_game_asset.glb";
    private const string IronFencePath = "Assets/Models/Environment/Props/Fences/iron_fence.glb";
    private const string MetalFenceBrokenPath = "Assets/Models/Environment/Props/Fences/metal_fence_broken.glb";
    private const string ModularFencePath = "Assets/Models/Environment/Props/Fences/modular_fence_system.glb";
    private const string OldWoodFencePath = "Assets/Models/Environment/Props/Fences/old_wood_fence.glb";
    private const string RoadSignsPackPath = "Assets/Models/Environment/Props/Street/road_signs_pack.glb";
    private const string StreetLightPath = "Assets/Models/Environment/Props/Street/street_light_fbx.glb";
    private const string TrashBinPath = "Assets/Models/Environment/Props/Street/trash_bin.glb";
    private const string WarningSignsPath = "Assets/Models/Environment/Props/Street/us_warning_road_signs.glb";
    private const float FloorY = 0f;
    private const float BarrierHeight = 2.2f;

    private static Transform staticEnvironment;
    private static Transform routeBarriers;
    private static Transform waypointGuides;
    private static Transform interactiveObstacles;
    private static Transform dynamicZones;
    private static Transform chaseLaneTriggers;
    private static Transform collapseSequence;
    private static TMP_Text interactionText;
    private static S01WarningTextUI warningUI;

    public static Vector3[] GetChaseWaypointPositions()
    {
        return new[]
        {
            new Vector3(0f, 1f, -8f),
            new Vector3(0f, 1f, 12f),
            new Vector3(0f, 1f, 32f),
            new Vector3(0f, 1f, 45f),
            new Vector3(18f, 1f, 45f),
            new Vector3(36f, 1f, 45f),
            new Vector3(45f, 1f, 45f),
            new Vector3(45f, 1f, 66f),
            new Vector3(45f, 1f, 88f),
            new Vector3(45f, 1f, 100f),
            new Vector3(25f, 1f, 100f),
            new Vector3(5f, 1f, 100f),
            new Vector3(5f, 1f, 128f),
            new Vector3(5f, 1f, 155f),
            new Vector3(-18f, 1f, 155f),
            new Vector3(-40f, 1f, 155f),
            new Vector3(-40f, 1f, 185f),
            new Vector3(-40f, 1f, 215f),
            new Vector3(-15f, 1f, 215f),
            new Vector3(10f, 1f, 215f),
            new Vector3(10f, 1f, 240f),
            new Vector3(10f, 1f, 265f)
        };
    }

    public static void BuildScene()
    {
        CleanupOldS01();

        GameObject root = new GameObject(RootName);
        staticEnvironment = CreateGroup(root.transform, "Static_Environment");
        routeBarriers = CreateGroup(root.transform, "Route_Barriers");
        waypointGuides = CreateGroup(root.transform, "Waypoint_Guides");
        interactiveObstacles = CreateGroup(root.transform, "Interactive_Obstacles");
        dynamicZones = CreateGroup(root.transform, "Dynamic_Zones");
        chaseLaneTriggers = CreateGroup(root.transform, "Chase_Lane_Triggers");
        collapseSequence = CreateGroup(root.transform, "Collapse_Sequence");

        Material roadMat = CreateMaterial("S01_Road", new Color32(42, 45, 50, 255));
        Material dirtMat = CreateMaterial("S01_Construction_Path", new Color32(112, 85, 52, 255));
        Material edgeMat = CreateMaterial("S01_Path_Edge", new Color32(62, 86, 54, 255));
        Material concreteMat = CreateMaterial("S01_Concrete", new Color32(92, 96, 100, 255));
        Material fenceMat = CreateMaterial("S01_Fence", new Color32(45, 48, 54, 255));
        Material warningMat = CreateMaterial("S01_Warning_Yellow", new Color32(242, 190, 42, 255));
        Material orangeMat = CreateMaterial("S01_Warning_Orange", new Color32(230, 92, 32, 255));
        Material mudMat = CreateMaterial("S01_Mud", new Color32(72, 49, 31, 255));
        Material woodMat = CreateMaterial("S01_Wood", new Color32(104, 70, 39, 255));
        Material bronzeMat = CreateMaterial("S01_Museum_Bronze", new Color32(166, 117, 48, 255));
        Material crackMat = CreateMaterial("S01_Crack", new Color32(12, 11, 12, 255));
        Material collapseMat = CreateMaterial("S01_Collapse", new Color32(130, 38, 42, 255));

        SetupUI();
        SetupPlayer();
        CreateSafetyFloor();
        BuildLongRoute(roadMat, dirtMat, fenceMat);
        BuildMuseumStart(concreteMat, bronzeMat, warningMat);
        BuildImportedEnvironmentKitVisuals();
        BuildCinematicS01SetPieces(concreteMat, fenceMat, warningMat, orangeMat, mudMat, woodMat, crackMat);
        BuildBlockedMainRoad(concreteMat, warningMat, orangeMat);
        BuildWheelbarrowDelayTrap(concreteMat, warningMat, woodMat);
        BuildConstructionFence(concreteMat, fenceMat, warningMat);
        BuildFallingDebrisArea(concreteMat, woodMat, orangeMat, crackMat);
        BuildMudZone(mudMat, warningMat);
        BuildNarrowPassage(concreteMat, fenceMat);
        BuildAmbushDodgeQTE(warningMat, orangeMat);
        BuildRouteGuidance(warningMat, orangeMat);
        BuildStoryTriggers();
        BuildCollapse(collapseMat, crackMat, warningMat);

        CreateEmpty(chaseLaneTriggers, "MinionSpawn_ChaseStart", new Vector3(0f, 1f, -8f));
        S01ChaseSetupBuilder.CreateS01ChaseThreat();

        Selection.activeGameObject = root;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("S01 rebuilt from scratch with clean long route.");
    }

    public static void WriteS01ImportedModelKitReport()
    {
        string reportPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Library", "CodexBridge", "s01_modelkit_report.json");
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(reportPath));

        GameObject group = GameObject.Find("Imported_ModelKit_Visuals");
        StringBuilder builder = new StringBuilder();
        builder.Append("{\n");
        builder.Append("  \"scene\": \"").Append(EscapeJsonForReport(EditorSceneManager.GetActiveScene().name)).Append("\",\n");
        builder.Append("  \"found\": ").Append(group != null ? "true" : "false").Append(",\n");
        builder.Append("  \"items\": [\n");

        if (group != null)
        {
            bool first = true;
            foreach (Transform child in group.transform)
            {
                Renderer[] renderers = child.GetComponentsInChildren<Renderer>(true);
                Bounds bounds = renderers.Length > 0 ? renderers[0].bounds : new Bounds(child.position, Vector3.zero);
                for (int i = 1; i < renderers.Length; i++)
                    bounds.Encapsulate(renderers[i].bounds);

                if (!first)
                    builder.Append(",\n");

                first = false;
                builder.Append("    {\n");
                builder.Append("      \"name\": \"").Append(EscapeJsonForReport(child.name)).Append("\",\n");
                AppendVector(builder, "position", child.position, 6);
                builder.Append(",\n");
                AppendVector(builder, "rotation", child.eulerAngles, 6);
                builder.Append(",\n");
                AppendVector(builder, "scale", child.localScale, 6);
                builder.Append(",\n");
                AppendVector(builder, "boundsCenter", bounds.center, 6);
                builder.Append(",\n");
                AppendVector(builder, "boundsSize", bounds.size, 6);
                builder.Append("\n    }");
            }
        }

        builder.Append("\n  ]\n");
        builder.Append("}\n");
        System.IO.File.WriteAllText(reportPath, builder.ToString());
        Debug.Log("S01 imported model kit report written to " + reportPath);
    }

    private static void BuildLongRoute(Material roadMat, Material dirtMat, Material fenceMat)
    {
        RouteSegment[] segments =
        {
            new RouteSegment("MuseumStreet_Start", new Vector3(0f, 0f, -6f), new Vector3(0f, 0f, 45f), 18f, roadMat),
            new RouteSegment("ConstructionDetour_East", new Vector3(0f, 0f, 45f), new Vector3(45f, 0f, 45f), 14.5f, dirtMat),
            new RouteSegment("ConstructionRun_North", new Vector3(45f, 0f, 45f), new Vector3(45f, 0f, 100f), 14.5f, dirtMat),
            new RouteSegment("DebrisRun_West", new Vector3(45f, 0f, 100f), new Vector3(5f, 0f, 100f), 12.5f, dirtMat),
            new RouteSegment("MudRun_North", new Vector3(5f, 0f, 100f), new Vector3(5f, 0f, 155f), 13.5f, dirtMat),
            new RouteSegment("NarrowRun_West", new Vector3(5f, 0f, 155f), new Vector3(-40f, 0f, 155f), 12.5f, dirtMat),
            new RouteSegment("LongEscape_North", new Vector3(-40f, 0f, 155f), new Vector3(-40f, 0f, 215f), 12.5f, dirtMat),
            new RouteSegment("LongEscape_East", new Vector3(-40f, 0f, 215f), new Vector3(10f, 0f, 215f), 14.5f, dirtMat),
            new RouteSegment("CollapseApproach_North", new Vector3(10f, 0f, 215f), new Vector3(10f, 0f, 265f), 13.5f, dirtMat)
        };

        foreach (RouteSegment segment in segments)
            CreateRouteSegment(segment, fenceMat);

        Vector3[] corners =
        {
            new Vector3(0f, FloorY, 45f),
            new Vector3(45f, FloorY, 45f),
            new Vector3(45f, FloorY, 100f),
            new Vector3(5f, FloorY, 100f),
            new Vector3(5f, FloorY, 155f),
            new Vector3(-40f, FloorY, 155f),
            new Vector3(-40f, FloorY, 215f),
            new Vector3(10f, FloorY, 215f)
        };

        for (int i = 0; i < corners.Length; i++)
            CreateCube(staticEnvironment, "RouteCorner_" + (i + 1).ToString("00"), corners[i], Vector3.zero, new Vector3(12f, 0.32f, 12f), dirtMat);
    }

    private static void BuildMuseumStart(Material concreteMat, Material bronzeMat, Material warningMat)
    {
        GameObject museum = CreateParent(staticEnvironment, "Museum_Facade", new Vector3(-15f, 0f, 10f), Vector3.zero);
        CreateChildCube(museum.transform, "Museum_Block", new Vector3(0f, 4f, 0f), new Vector3(12f, 8f, 8f), concreteMat);
        CreateChildCube(museum.transform, "Museum_Entrance", new Vector3(6.2f, 2f, 0f), new Vector3(0.35f, 4f, 4f), bronzeMat);

        CreateStreetLamp(new Vector3(9f, 0f, 8f), warningMat);
        CreateStreetLamp(new Vector3(-9f, 0f, 26f), warningMat);
        CreateStreetLamp(new Vector3(9f, 0f, 39f), warningMat);
    }

    private static void BuildImportedEnvironmentKitVisuals()
    {
        GameObject modelGroup = CreateParent(staticEnvironment, "Imported_ModelKit_Visuals", Vector3.zero, Vector3.zero);
        int placed = 0;

        // The current city-building GLBs are whole-scene chunks with bad pivots and extra slab meshes.
        // Keep S01 readable by using the cleaner construction props as the imported visual layer.
        placed += CreateImportedModelVisual(modelGroup.transform, BarrierConePackPath, "BarrierConePack_MainRoadBlock_A", new Vector3(2f, 0f, 53f), new Vector3(0f, 8f, 0f), 1.4f, 8f) ? 1 : 0;
        placed += CreateImportedModelVisual(modelGroup.transform, BarrierConePackPath, "BarrierConePack_DetourEntrance", new Vector3(22f, 0f, 47.5f), new Vector3(0f, -18f, 0f), 1.25f, 7f) ? 1 : 0;
        placed += CreateImportedModelVisual(modelGroup.transform, BarrierConePackPath, "BarrierConePack_MudWarning", new Vector3(7f, 0f, 116f), new Vector3(0f, 32f, 0f), 1.15f, 6f) ? 1 : 0;

        placed += CreateImportedModelVisual(modelGroup.transform, CardboardBoxesPath, "CardboardBoxes_MuseumSide", new Vector3(-10.8f, 0f, 31f), new Vector3(0f, -12f, 0f), 1f, 3f) ? 1 : 0;
        placed += CreateImportedModelVisual(modelGroup.transform, CardboardBoxesPath, "CardboardBoxes_MudApproach_Side", new Vector3(10.8f, 0f, 114f), new Vector3(0f, 25f, 0f), 1.1f, 3f) ? 1 : 0;
        placed += CreateImportedModelVisual(modelGroup.transform, CaseBoxesPath, "CaseBoxes_ConstructionRun_Side_A", new Vector3(51.5f, 0f, 72f), new Vector3(0f, -20f, 0f), 1.4f, 3.8f) ? 1 : 0;
        placed += CreateImportedModelVisual(modelGroup.transform, CaseBoxesPath, "CaseBoxes_ConstructionRun_Side_B", new Vector3(52f, 0f, 94f), new Vector3(0f, 18f, 0f), 1.25f, 3.5f) ? 1 : 0;

        placed += CreateImportedModelVisual(modelGroup.transform, ConstructionFencePath, "ConstructionFence_DetourEdge_A", new Vector3(50.8f, 0f, 67f), new Vector3(0f, 0f, 0f), 2.1f, 5f) ? 1 : 0;
        placed += CreateImportedModelVisual(modelGroup.transform, ConstructionFencePath, "ConstructionFence_MudEntry_Edge", new Vector3(12f, 0f, 121f), new Vector3(0f, 90f, 0f), 2.1f, 5f) ? 1 : 0;

        placed += CreateImportedModelVisual(modelGroup.transform, DumpsterPath, "Dumpster_DetourCorner_Side", new Vector3(47.8f, 0f, 57f), new Vector3(0f, 15f, 0f), 1.7f, 4.5f) ? 1 : 0;
        placed += CreateImportedModelVisual(modelGroup.transform, DumpsterPath, "Dumpster_DebrisCorner_Side", new Vector3(36f, 0f, 106.3f), new Vector3(0f, 90f, 0f), 1.9f, 4.8f) ? 1 : 0;

        placed += CreateImportedModelVisual(modelGroup.transform, WoodenCratePath, "WoodenCrate_DetourSide_A", new Vector3(51.2f, 0f, 54f), new Vector3(0f, 25f, 0f), 1.2f, 3.5f) ? 1 : 0;
        placed += CreateImportedModelVisual(modelGroup.transform, WoodenCratePath, "WoodenCrate_DetourSide_B", new Vector3(39f, 0f, 103f), new Vector3(0f, -18f, 0f), 1.1f, 3.2f) ? 1 : 0;

        placed += CreateImportedModelVisual(modelGroup.transform, RockDebrisPath, "RockDebris_DebrisTurn_Side", new Vector3(21f, 0f, 104f), new Vector3(0f, 12f, 0f), 1f, 6f) ? 1 : 0;
        placed += CreateImportedModelVisual(modelGroup.transform, RockDebrisPath, "RockDebris_CollapseZone_Side_A", new Vector3(14.7f, 0f, 252f), new Vector3(0f, -25f, 0f), 1.25f, 7f) ? 1 : 0;
        placed += CreateImportedModelVisual(modelGroup.transform, RockDebrisPath, "RockDebris_CollapseZone_Side_B", new Vector3(5.8f, 0f, 258f), new Vector3(0f, 36f, 0f), 1.1f, 6f) ? 1 : 0;

        placed += CreateImportedModelVisual(modelGroup.transform, WarningSignsPath, "WarningSigns_Collapse_Side", new Vector3(4.2f, 0f, 246f), new Vector3(0f, 25f, 0f), 2f, 4.5f) ? 1 : 0;
        placed += CreateImportedModelVisual(modelGroup.transform, WarningSignsPath, "WarningSigns_MudEntry_Side", new Vector3(10.5f, 0f, 119f), new Vector3(0f, 350f, 0f), 1.8f, 4f) ? 1 : 0;
        placed += CreateImportedModelVisual(modelGroup.transform, TrashBinPath, "TrashBin_MuseumStreet_Side", new Vector3(-10.8f, 0f, 23f), new Vector3(0f, 20f, 0f), 1.2f, 2.3f) ? 1 : 0;

        placed = RemoveUnsafeImportedModelChildren(modelGroup.transform);

        if (placed == 0)
        {
            UnityEngine.Object.DestroyImmediate(modelGroup);
            BuildRoadSurfaceDressing();
            Debug.LogWarning("S01CityEscapeBuilder: no imported environment models are importable yet. Using primitive fallback visuals.");
            return;
        }

        BuildRoadSurfaceDressing();
        Debug.Log("S01CityEscapeBuilder: placed " + placed + " imported environment model visuals. Imported models are visual-only; gameplay colliders remain primitive and stable.");
    }

    private static int RemoveUnsafeImportedModelChildren(Transform modelGroup)
    {
        int kept = 0;
        for (int i = modelGroup.childCount - 1; i >= 0; i--)
        {
            Transform child = modelGroup.GetChild(i);
            Bounds bounds = CalculateRendererBounds(child.gameObject);
            string childName = child.name.ToLowerInvariant();
            bool sourcePackRoot =
                childName == "barrier__traffic_cone_pack" ||
                childName == "cardboard_boxes" ||
                childName == "case_boxes" ||
                childName == "dumpster_-_4096px2" ||
                childName == "wooden_crate_-_game_asset" ||
                childName == "rock_debris_1" ||
                childName == "construction_fence";
            bool wrongSceneProp = childName.Contains("fallentree") || childName.Contains("fallen_tree");
            bool originRoot = child.position.sqrMagnitude < 0.001f;
            bool hugeBounds = Mathf.Max(bounds.size.x, bounds.size.z) > 40f || bounds.size.y > 30f;

            if (wrongSceneProp || (sourcePackRoot && originRoot) || hugeBounds)
            {
                UnityEngine.Object.DestroyImmediate(child.gameObject);
                continue;
            }

            kept++;
        }

        return kept;
    }

    private static void BuildRoadSurfaceDressing()
    {
        Material sidewalkMat = CreateMaterial("S01_City_Sidewalk", new Color32(116, 120, 124, 255));
        Material stripeMat = CreateMaterial("S01_City_Road_Marking", new Color32(230, 225, 190, 255));

        GameObject cityGroup = CreateParent(staticEnvironment, "Road_Surface_Dressing", Vector3.zero, Vector3.zero);

        CreateCube(cityGroup.transform, "Left_Sidewalk_Start", new Vector3(-10.5f, 0.18f, 18f), Vector3.zero, new Vector3(5f, 0.22f, 58f), sidewalkMat);
        CreateCube(cityGroup.transform, "Right_Sidewalk_Start", new Vector3(10.5f, 0.18f, 18f), Vector3.zero, new Vector3(5f, 0.22f, 58f), sidewalkMat);

        for (int i = 0; i < 7; i++)
        {
            float z = -4f + i * 7.5f;
            RemoveCollider(CreateCube(cityGroup.transform, "CenterLaneDash_" + i.ToString("00"), new Vector3(0f, 0.31f, z), Vector3.zero, new Vector3(0.22f, 0.045f, 3.2f), stripeMat));
        }
    }

    private static void BuildCinematicS01SetPieces(Material concreteMat, Material fenceMat, Material warningMat, Material orangeMat, Material mudMat, Material woodMat, Material crackMat)
    {
        Material glassMat = CreateMaterial("S01_Cinematic_Glass", new Color32(64, 130, 160, 255));
        Material darkMetalMat = CreateMaterial("S01_Cinematic_DarkMetal", new Color32(24, 27, 32, 255));
        Material bannerMat = CreateMaterial("S01_Cinematic_Banner_Red", new Color32(146, 28, 34, 255));
        Material blueBannerMat = CreateMaterial("S01_Cinematic_Banner_Blue", new Color32(32, 78, 142, 255));
        Material leafMat = CreateMaterial("S01_Cinematic_Leaves", new Color32(46, 94, 55, 255));
        Material shadowMat = CreateMaterial("S01_BlackStar_ShadowMarks", new Color32(18, 9, 28, 255));
        Material lightGlowMat = CreateMaterial("S01_Cinematic_WarmGlow", new Color32(255, 211, 86, 255));

        GameObject dressing = CreateParent(staticEnvironment, "Cinematic_SetPieces", Vector3.zero, Vector3.zero);
        BuildMuseumPlazaDressing(dressing.transform, concreteMat, glassMat, bannerMat, blueBannerMat, warningMat, lightGlowMat);
        BuildCityDepthDressing(dressing.transform, concreteMat, glassMat, bannerMat, blueBannerMat, darkMetalMat);
        BuildConstructionDressing(dressing.transform, concreteMat, fenceMat, warningMat, orangeMat, woodMat, darkMetalMat, lightGlowMat);
        BuildMudParkDressing(dressing.transform, mudMat, woodMat, leafMat, concreteMat, warningMat);
        BuildCollapseDressing(dressing.transform, crackMat, shadowMat, orangeMat, warningMat, lightGlowMat);
    }

    private static void BuildMuseumPlazaDressing(Transform parent, Material concreteMat, Material glassMat, Material bannerMat, Material blueBannerMat, Material warningMat, Material lightGlowMat)
    {
        GameObject plaza = CreateParent(parent, "Museum_Plaza_SetPiece", Vector3.zero, Vector3.zero);
        CreateVisualCube(plaza.transform, "MuseumFront_Steps", new Vector3(-8.6f, 0.35f, 10f), Vector3.zero, new Vector3(5f, 0.25f, 7f), concreteMat);
        CreateVisualCube(plaza.transform, "MuseumFront_Awning", new Vector3(-8.8f, 5.9f, 10f), Vector3.zero, new Vector3(1.2f, 0.35f, 8.6f), warningMat);
        CreateVisualCube(plaza.transform, "MuseumFront_GlassPanel_A", new Vector3(-8.15f, 2.9f, 8.1f), Vector3.zero, new Vector3(0.12f, 2.8f, 1.4f), glassMat);
        CreateVisualCube(plaza.transform, "MuseumFront_GlassPanel_B", new Vector3(-8.15f, 2.9f, 11.9f), Vector3.zero, new Vector3(0.12f, 2.8f, 1.4f), glassMat);

        for (int i = 0; i < 4; i++)
        {
            float z = 5.8f + i * 2.8f;
            CreateVisualCylinder(plaza.transform, "MuseumColumn_" + i.ToString("00"), new Vector3(-8.1f, 2.4f, z), Vector3.zero, new Vector3(0.32f, 2.4f, 0.32f), concreteMat);
        }

        CreateVisualCube(plaza.transform, "MuseumBanner_DongSon", new Vector3(-7.9f, 4.5f, 6f), Vector3.zero, new Vector3(0.08f, 1.4f, 2.4f), bannerMat);
        CreateVisualCube(plaza.transform, "MuseumBanner_CoLoa", new Vector3(-7.9f, 4.5f, 14f), Vector3.zero, new Vector3(0.08f, 1.4f, 2.4f), blueBannerMat);
        CreateVisualCube(plaza.transform, "PhoneSignalWarning_Kiosk", new Vector3(8.5f, 1.35f, 3f), new Vector3(0f, -15f, 0f), new Vector3(0.45f, 2.7f, 1.4f), glassMat);
        CreateVisualCube(plaza.transform, "Kiosk_LitScreen", new Vector3(8.18f, 1.55f, 3f), new Vector3(0f, -15f, 0f), new Vector3(0.08f, 1.3f, 0.9f), lightGlowMat);

        for (int i = 0; i < 7; i++)
        {
            float z = -4f + i * 7f;
            CreateVisualCylinder(plaza.transform, "StreetBollard_Left_" + i.ToString("00"), new Vector3(-8.4f, 0.55f, z), Vector3.zero, new Vector3(0.18f, 0.55f, 0.18f), warningMat);
            CreateVisualCylinder(plaza.transform, "StreetBollard_Right_" + i.ToString("00"), new Vector3(8.4f, 0.55f, z + 3f), Vector3.zero, new Vector3(0.18f, 0.55f, 0.18f), warningMat);
        }
    }

    private static void BuildCityDepthDressing(Transform parent, Material concreteMat, Material glassMat, Material bannerMat, Material blueBannerMat, Material darkMetalMat)
    {
        GameObject city = CreateParent(parent, "City_Depth_SetPiece", Vector3.zero, Vector3.zero);
        CreateVisualCube(city.transform, "Overhead_Skywalk_Broken", new Vector3(21f, 7.5f, 44.5f), new Vector3(0f, 0f, -8f), new Vector3(18f, 0.35f, 2.1f), darkMetalMat);
        CreateVisualCube(city.transform, "Skywalk_GlassShard_A", new Vector3(13f, 6.8f, 44.4f), new Vector3(0f, 0f, 18f), new Vector3(2f, 0.08f, 1.2f), glassMat);
        CreateVisualCube(city.transform, "Skywalk_GlassShard_B", new Vector3(28f, 6.9f, 44.6f), new Vector3(0f, 0f, -20f), new Vector3(2.3f, 0.08f, 1.1f), glassMat);

        CreateCylinderBetween(city.transform, "OverheadCable_Street_A", new Vector3(-10f, 6.2f, 12f), new Vector3(10f, 5.5f, 26f), 0.04f, darkMetalMat);
        CreateCylinderBetween(city.transform, "OverheadCable_Street_B", new Vector3(-11f, 5.8f, 34f), new Vector3(12f, 5.2f, 42f), 0.04f, darkMetalMat);
    }

    private static void BuildConstructionDressing(Transform parent, Material concreteMat, Material fenceMat, Material warningMat, Material orangeMat, Material woodMat, Material darkMetalMat, Material lightGlowMat)
    {
        GameObject construction = CreateParent(parent, "Construction_Run_SetPiece", Vector3.zero, Vector3.zero);
        CreateScaffoldTower(construction.transform, "Scaffold_East_A", new Vector3(52f, 0f, 60f), darkMetalMat, warningMat);
        CreateScaffoldTower(construction.transform, "Scaffold_East_B", new Vector3(52f, 0f, 88f), darkMetalMat, warningMat);
        CreateCylinderBetween(construction.transform, "LooseCable_Construction_A", new Vector3(52f, 6f, 60f), new Vector3(52f, 5.4f, 88f), 0.05f, darkMetalMat);
        CreateCylinderBetween(construction.transform, "LooseCable_Construction_B", new Vector3(38f, 5.2f, 47f), new Vector3(52f, 5.8f, 60f), 0.04f, darkMetalMat);

        CreateVisualCube(construction.transform, "Excavator_Silhouette_Base", new Vector3(56f, 0.7f, 82f), new Vector3(0f, -25f, 0f), new Vector3(4.2f, 1.4f, 2.2f), orangeMat);
        CreateVisualCube(construction.transform, "Excavator_Silhouette_Arm", new Vector3(53f, 2.6f, 80.4f), new Vector3(0f, -25f, -24f), new Vector3(5.5f, 0.35f, 0.35f), orangeMat);
        CreateVisualCube(construction.transform, "Excavator_Bucket", new Vector3(49.4f, 1.25f, 78.7f), new Vector3(0f, -25f, -8f), new Vector3(1.4f, 0.6f, 1f), darkMetalMat);

        for (int i = 0; i < 5; i++)
            CreateVisualCube(construction.transform, "CautionTape_" + i.ToString("00"), new Vector3(39f + i * 2.3f, 1.4f, 104f), new Vector3(0f, 12f, i % 2 == 0 ? 8f : -8f), new Vector3(1.8f, 0.12f, 0.08f), warningMat);

        CreateVisualCube(construction.transform, "FloodLight_Stand_A", new Vector3(52f, 1.5f, 100f), Vector3.zero, new Vector3(0.18f, 3f, 0.18f), darkMetalMat);
        CreateVisualCube(construction.transform, "FloodLight_A", new Vector3(51.5f, 3.25f, 100f), new Vector3(0f, -35f, 0f), new Vector3(0.8f, 0.5f, 0.35f), lightGlowMat);
        CreateVisualCube(construction.transform, "StackedTimber_A", new Vector3(50.8f, 0.45f, 63f), new Vector3(0f, -10f, 0f), new Vector3(4.8f, 0.28f, 0.35f), woodMat);
        CreateVisualCube(construction.transform, "StackedTimber_B", new Vector3(51.3f, 0.85f, 63.4f), new Vector3(0f, -12f, 0f), new Vector3(3.9f, 0.25f, 0.35f), woodMat);
    }

    private static void BuildMudParkDressing(Transform parent, Material mudMat, Material woodMat, Material leafMat, Material concreteMat, Material warningMat)
    {
        GameObject park = CreateParent(parent, "Mud_ParkEdge_SetPiece", Vector3.zero, Vector3.zero);
        CreateVisualCube(park.transform, "MudRun_WetReflection_A", new Vector3(0.7f, 0.34f, 126f), Vector3.zero, new Vector3(2.4f, 0.035f, 7f), mudMat);
        CreateVisualCube(park.transform, "MudRun_WetReflection_B", new Vector3(7.6f, 0.34f, 137f), Vector3.zero, new Vector3(1.5f, 0.035f, 5f), mudMat);

        Vector3[] treePositions =
        {
            new Vector3(13.2f, 0f, 118f),
            new Vector3(13.5f, 0f, 132f),
            new Vector3(-3.4f, 0f, 143f),
            new Vector3(12.5f, 0f, 150f)
        };

        foreach (Vector3 position in treePositions)
            CreateRoughTree(park.transform, position, woodMat, leafMat);

        CreateVisualCube(park.transform, "BrokenParkFence_A", new Vector3(11.5f, 1f, 121f), new Vector3(0f, 18f, 8f), new Vector3(0.25f, 2f, 4.2f), woodMat);
        CreateVisualCube(park.transform, "BrokenParkFence_B", new Vector3(11.2f, 1f, 139f), new Vector3(0f, -14f, -10f), new Vector3(0.25f, 2f, 5f), woodMat);
        CreateVisualCube(park.transform, "WarningBoard_Mud", new Vector3(9.8f, 1.8f, 119f), new Vector3(0f, -30f, 0f), new Vector3(0.12f, 1.2f, 1.9f), warningMat);
        CreateVisualCube(park.transform, "WarningBoard_Post", new Vector3(10.2f, 0.8f, 119.2f), new Vector3(0f, -30f, 0f), new Vector3(0.16f, 1.6f, 0.16f), concreteMat);
    }

    private static void BuildCollapseDressing(Transform parent, Material crackMat, Material shadowMat, Material orangeMat, Material warningMat, Material lightGlowMat)
    {
        GameObject collapse = CreateParent(parent, "Collapse_Finale_SetPiece", Vector3.zero, Vector3.zero);
        for (int i = 0; i < 10; i++)
        {
            float angle = i * 36f;
            Vector3 offset = Quaternion.Euler(0f, angle, 0f) * new Vector3(0f, 0f, 8f);
            CreateVisualCube(collapse.transform, "CollapseCrack_Ray_" + i.ToString("00"), new Vector3(10f, 0.42f, 260f) + offset * 0.45f, new Vector3(0f, angle, 0f), new Vector3(0.18f, 0.04f, 7f), i % 2 == 0 ? crackMat : shadowMat);
        }

        CreateVisualCube(collapse.transform, "BlackStar_ShadowTrail_A", new Vector3(10f, 0.38f, 235f), new Vector3(0f, 8f, 0f), new Vector3(3f, 0.035f, 13f), shadowMat);
        CreateVisualCube(collapse.transform, "BlackStar_ShadowTrail_B", new Vector3(4f, 0.39f, 247f), new Vector3(0f, -18f, 0f), new Vector3(1.2f, 0.035f, 8f), shadowMat);
        CreateVisualCube(collapse.transform, "EmergencyLight_Post_A", new Vector3(2.6f, 1.2f, 255f), new Vector3(0f, 0f, -12f), new Vector3(0.18f, 2.4f, 0.18f), orangeMat);
        CreateVisualCube(collapse.transform, "EmergencyLight_Lamp_A", new Vector3(2.35f, 2.45f, 255f), Vector3.zero, new Vector3(0.7f, 0.35f, 0.35f), lightGlowMat);
        CreateVisualCube(collapse.transform, "EmergencyLight_Post_B", new Vector3(17.2f, 1.2f, 258f), new Vector3(0f, 0f, 15f), new Vector3(0.18f, 2.4f, 0.18f), orangeMat);
        CreateVisualCube(collapse.transform, "EmergencyLight_Lamp_B", new Vector3(17.45f, 2.45f, 258f), Vector3.zero, new Vector3(0.7f, 0.35f, 0.35f), warningMat);
    }

    private static void CreateFacadeBlock(Transform parent, string name, Vector3 basePosition, Vector3 size, Material wallMat, Material glassMat, Material signMat, bool facesRight)
    {
        GameObject facade = CreateParent(parent, name, basePosition, Vector3.zero);
        CreateVisualChildCube(facade.transform, "Mass", new Vector3(0f, size.y * 0.5f, 0f), Vector3.zero, size, wallMat);

        float frontX = facesRight ? size.x * 0.51f : -size.x * 0.51f;
        for (int floor = 0; floor < Mathf.Max(3, Mathf.FloorToInt(size.y / 3f)); floor++)
        {
            float y = 3f + floor * 2.8f;
            for (int col = -2; col <= 2; col++)
                CreateVisualChildCube(facade.transform, "LitWindow_" + floor + "_" + col, new Vector3(frontX, y, col * 2.1f), Vector3.zero, new Vector3(0.09f, 1f, 1.1f), glassMat);
        }

        CreateVisualChildCube(facade.transform, "StreetLevel_Sign", new Vector3(frontX, 2f, 0f), Vector3.zero, new Vector3(0.12f, 0.8f, size.z * 0.7f), signMat);
    }

    private static void CreateScaffoldTower(Transform parent, string name, Vector3 basePosition, Material metalMat, Material warningMat)
    {
        GameObject tower = CreateParent(parent, name, basePosition, Vector3.zero);
        for (int level = 0; level < 3; level++)
        {
            float y = 1.2f + level * 1.8f;
            CreateVisualChildCube(tower.transform, "Platform_" + level, new Vector3(0f, y, 0f), Vector3.zero, new Vector3(4f, 0.18f, 2.2f), metalMat);
            CreateVisualChildCube(tower.transform, "Rail_" + level + "_A", new Vector3(0f, y + 0.55f, 1.1f), Vector3.zero, new Vector3(4f, 0.12f, 0.12f), warningMat);
            CreateVisualChildCube(tower.transform, "Rail_" + level + "_B", new Vector3(0f, y + 0.55f, -1.1f), Vector3.zero, new Vector3(4f, 0.12f, 0.12f), warningMat);
        }

        for (int x = -1; x <= 1; x += 2)
        {
            for (int z = -1; z <= 1; z += 2)
                CreateVisualChildCube(tower.transform, "Post_" + x + "_" + z, new Vector3(x * 1.9f, 2.8f, z * 1f), Vector3.zero, new Vector3(0.12f, 5.6f, 0.12f), metalMat);
        }
    }

    private static void CreateRoughTree(Transform parent, Vector3 position, Material trunkMat, Material leafMat)
    {
        GameObject tree = CreateParent(parent, "RoughTree_" + Mathf.RoundToInt(position.x) + "_" + Mathf.RoundToInt(position.z), position, Vector3.zero);
        CreateVisualChildPrimitive(tree.transform, "Trunk", PrimitiveType.Cylinder, new Vector3(0f, 1.7f, 0f), new Vector3(0f, 0f, 6f), new Vector3(0.28f, 1.7f, 0.28f), trunkMat);
        CreateVisualChildPrimitive(tree.transform, "Canopy_A", PrimitiveType.Sphere, new Vector3(0f, 3.8f, 0f), Vector3.zero, new Vector3(1.9f, 1.1f, 1.5f), leafMat);
        CreateVisualChildPrimitive(tree.transform, "Canopy_B", PrimitiveType.Sphere, new Vector3(0.7f, 3.45f, -0.25f), Vector3.zero, new Vector3(1.5f, 0.9f, 1.2f), leafMat);
        CreateVisualChildPrimitive(tree.transform, "Canopy_C", PrimitiveType.Sphere, new Vector3(-0.65f, 3.5f, 0.35f), Vector3.zero, new Vector3(1.4f, 0.85f, 1.25f), leafMat);
    }

    private static void CreatePrimitiveCityBuilding(Transform parent, string name, Vector3 basePosition, Vector3 size, Material wallMat, Material glassMat, Material signMat, bool facesRight)
    {
        GameObject building = CreateParent(parent, name, basePosition, Vector3.zero);
        CreateChildCube(building.transform, "Mass", new Vector3(0f, size.y * 0.5f, 0f), size, wallMat);

        float frontX = facesRight ? size.x * 0.51f : -size.x * 0.51f;
        float surfaceOffset = facesRight ? 0.02f : -0.02f;
        float yStart = 3.3f;
        int floors = Mathf.Max(2, Mathf.FloorToInt((size.y - 2f) / 3f));
        for (int floor = 0; floor < floors; floor++)
        {
            float y = yStart + floor * 2.5f;
            for (int col = -1; col <= 1; col++)
            {
                GameObject window = CreateChildPrimitive(building.transform, "Window_" + floor + "_" + col, PrimitiveType.Cube, new Vector3(frontX + surfaceOffset, y, col * 2.1f), Vector3.zero, new Vector3(0.08f, 1.1f, 1.25f), glassMat);
                RemoveCollider(window);
            }
        }

        GameObject sign = CreateChildPrimitive(building.transform, "Store_Sign", PrimitiveType.Cube, new Vector3(frontX + surfaceOffset, 2.15f, 0f), Vector3.zero, new Vector3(0.1f, 0.7f, size.z * 0.72f), signMat);
        RemoveCollider(sign);

        GameObject shutter = CreateChildPrimitive(building.transform, "Ground_Shutter", PrimitiveType.Cube, new Vector3(frontX + surfaceOffset * 1.5f, 1.05f, 0f), Vector3.zero, new Vector3(0.1f, 1.75f, size.z * 0.42f), glassMat);
        RemoveCollider(shutter);
    }

    private static void CreateAlleyHint(Transform parent, string name, Vector3 position, Material wallMat, Material warningMat)
    {
        GameObject hint = CreateParent(parent, name, position, new Vector3(0f, -16f, 0f));
        CreateChildCube(hint.transform, "LeftBlock", new Vector3(0f, 1.2f, -2.4f), new Vector3(0.8f, 2.4f, 5f), wallMat);
        CreateChildCube(hint.transform, "RightBlock", new Vector3(7f, 1.2f, 2.4f), new Vector3(0.8f, 2.4f, 5f), wallMat);
        RemoveCollider(CreateChildPrimitive(hint.transform, "DetourSign", PrimitiveType.Cube, new Vector3(3.5f, 2.4f, -1.9f), new Vector3(0f, 0f, 0f), new Vector3(4.2f, 0.85f, 0.12f), warningMat));
    }

    private static void CreateStreetTree(Transform parent, Vector3 position, Material foliageMat)
    {
        Material trunkMat = CreateMaterial("S01_City_Tree_Trunk", new Color32(82, 58, 38, 255));
        GameObject tree = CreateParent(parent, "StreetTree_" + Mathf.RoundToInt(position.x) + "_" + Mathf.RoundToInt(position.z), position, Vector3.zero);
        RemoveCollider(CreateChildPrimitive(tree.transform, "Trunk", PrimitiveType.Cylinder, new Vector3(0f, 1f, 0f), Vector3.zero, new Vector3(0.22f, 1.8f, 0.22f), trunkMat));
        RemoveCollider(CreateChildPrimitive(tree.transform, "Canopy", PrimitiveType.Sphere, new Vector3(0f, 2.5f, 0f), Vector3.zero, new Vector3(1.6f, 1.5f, 1.6f), foliageMat));
    }

    private static bool CreateImportedModelVisual(Transform parent, string assetPath, string name, Vector3 targetPosition, Vector3 rotation, float targetHeight, float maxFootprint)
    {
        GameObject sourceModel = LoadImportedModel(assetPath);
        if (sourceModel == null)
            return false;

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(sourceModel);
        if (instance == null)
            instance = UnityEngine.Object.Instantiate(sourceModel);

        instance.name = name;
        instance.transform.SetParent(parent, false);
        instance.transform.position = Vector3.zero;
        instance.transform.eulerAngles = rotation;
        instance.transform.localScale = Vector3.one;

        DisableImportedGameplayComponents(instance);
        NormalizeImportedVisual(instance, targetPosition, targetHeight, maxFootprint);
        instance.name = name;

        if (!ImportedVisualLooksUsable(instance, targetPosition, maxFootprint))
        {
            Debug.LogWarning("S01CityEscapeBuilder: skipped imported model with unsafe bounds/pivot: " + name);
            UnityEngine.Object.DestroyImmediate(instance);
            return false;
        }

        return true;
    }

    private static bool CreateImportedFilteredModelVisual(Transform parent, string assetPath, string name, Vector3 targetPosition, Vector3 rotation, float targetHeight, float maxFootprint, params string[] keepNameTokens)
    {
        GameObject sourceModel = LoadImportedModel(assetPath);
        if (sourceModel == null)
            return false;

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(sourceModel);
        if (instance == null)
            instance = UnityEngine.Object.Instantiate(sourceModel);

        instance.name = name;
        instance.transform.SetParent(parent, false);
        instance.transform.position = Vector3.zero;
        instance.transform.eulerAngles = rotation;
        instance.transform.localScale = Vector3.one;

        DisableImportedGameplayComponents(instance);
        DisableRenderersExceptNameTokens(instance, keepNameTokens);

        Bounds filteredBounds = CalculateRendererBounds(instance);
        if (filteredBounds.size.sqrMagnitude <= 0.001f)
        {
            Debug.LogWarning("S01CityEscapeBuilder: filtered model has no visible renderer after token filtering: " + name);
            UnityEngine.Object.DestroyImmediate(instance);
            return false;
        }

        NormalizeImportedVisual(instance, targetPosition, targetHeight, maxFootprint);
        instance.name = name;

        if (!ImportedVisualLooksUsable(instance, targetPosition, maxFootprint))
        {
            Debug.LogWarning("S01CityEscapeBuilder: skipped filtered imported model with unsafe bounds/pivot: " + name);
            UnityEngine.Object.DestroyImmediate(instance);
            return false;
        }

        return true;
    }

    private static GameObject LoadImportedModel(string assetPath)
    {
        if (!System.IO.File.Exists(assetPath))
        {
            Debug.LogWarning("S01CityEscapeBuilder: model file missing: " + assetPath);
            return null;
        }

        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (model == null)
            Debug.LogWarning("S01CityEscapeBuilder: Unity has not imported this model as a GameObject yet: " + assetPath);

        return model;
    }

    private static void DisableImportedGameplayComponents(GameObject root)
    {
        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        foreach (Collider collider in colliders)
            UnityEngine.Object.DestroyImmediate(collider);

        Rigidbody[] rigidbodies = root.GetComponentsInChildren<Rigidbody>(true);
        foreach (Rigidbody rigidbody in rigidbodies)
            UnityEngine.Object.DestroyImmediate(rigidbody);

        Animator[] animators = root.GetComponentsInChildren<Animator>(true);
        foreach (Animator animator in animators)
            animator.enabled = false;
    }

    private static void DisableRenderersExceptNameTokens(GameObject root, string[] keepNameTokens)
    {
        if (keepNameTokens == null || keepNameTokens.Length == 0)
            return;

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            string path = GetHierarchyPath(renderer.transform).ToLowerInvariant();
            bool keep = false;

            foreach (string token in keepNameTokens)
            {
                if (!string.IsNullOrWhiteSpace(token) && path.Contains(token.ToLowerInvariant()))
                {
                    keep = true;
                    break;
                }
            }

            renderer.enabled = keep;
        }
    }

    private static string GetHierarchyPath(Transform transform)
    {
        StringBuilder builder = new StringBuilder(transform.name);
        Transform current = transform.parent;
        while (current != null)
        {
            builder.Insert(0, current.name + "/");
            current = current.parent;
        }

        return builder.ToString();
    }

    private static void NormalizeImportedVisual(GameObject visualRoot, Vector3 targetPosition, float targetHeight, float maxFootprint)
    {
        Bounds bounds = CalculateRendererBounds(visualRoot);
        if (bounds.size.sqrMagnitude <= 0.001f)
        {
            visualRoot.transform.position = targetPosition;
            return;
        }

        float heightScale = targetHeight / Mathf.Max(0.1f, bounds.size.y);
        float footprint = Mathf.Max(bounds.size.x, bounds.size.z);
        float footprintScale = maxFootprint / Mathf.Max(0.1f, footprint);
        float finalScale = Mathf.Min(heightScale, footprintScale);
        visualRoot.transform.localScale *= finalScale;

        bounds = CalculateRendererBounds(visualRoot);
        Vector3 offset = targetPosition - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        visualRoot.transform.position += offset;
    }

    private static bool ImportedVisualLooksUsable(GameObject visualRoot, Vector3 targetPosition, float maxFootprint)
    {
        Bounds bounds = CalculateRendererBounds(visualRoot);
        if (bounds.size.sqrMagnitude <= 0.001f)
            return false;

        float footprint = Mathf.Max(bounds.size.x, bounds.size.z);
        if (footprint > maxFootprint * 1.35f)
            return false;

        Vector2 targetXZ = new Vector2(targetPosition.x, targetPosition.z);
        Vector2 centerXZ = new Vector2(bounds.center.x, bounds.center.z);
        if (Vector2.Distance(targetXZ, centerXZ) > maxFootprint * 0.75f)
            return false;

        if (bounds.min.y < -0.35f || bounds.min.y > 0.35f)
            return false;

        return true;
    }

    private static Bounds CalculateRendererBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        Bounds bounds = new Bounds(root.transform.position, Vector3.zero);
        bool hasBounds = false;

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || !renderer.enabled)
                continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return bounds;
    }

    private static void BuildBlockedMainRoad(Material concreteMat, Material warningMat, Material orangeMat)
    {
        GameObject truck = CreateParent(staticEnvironment, "ConstructionTruck_BlockingMainRoad", new Vector3(0f, 0f, 55f), Vector3.zero);
        CreateChildCube(truck.transform, "Truck_Body", new Vector3(0f, 1.2f, 0f), new Vector3(12f, 2.4f, 4f), concreteMat);
        CreateChildCube(truck.transform, "Truck_Cabin", new Vector3(4.2f, 1.7f, 0f), new Vector3(3.2f, 3f, 3.8f), warningMat);
        CreateConcreteBlock(new Vector3(-6f, 0.5f, 50f), concreteMat);
        CreateConcreteBlock(new Vector3(6f, 0.5f, 50f), concreteMat);
        CreateConeRow(new Vector3(4f, 0f, 40f), Vector3.right, 5, orangeMat);
    }

    private static void BuildWheelbarrowDelayTrap(Material concreteMat, Material warningMat, Material woodMat)
    {
        GameObject delayObstacle = CreateParent(chaseLaneTriggers, "HacTinhBreakableDelayObstacle", new Vector3(45f, 0f, 65f), Vector3.zero);
        GameObject scatterRoot = CreateParent(delayObstacle.transform, "DelayScatterPieces", Vector3.zero, Vector3.zero);
        RemoveCollider(CreateChildPrimitive(scatterRoot.transform, "Broken_Frame", PrimitiveType.Cube, new Vector3(0f, 0.5f, 0f), Vector3.zero, new Vector3(2.5f, 0.3f, 1.1f), concreteMat));
        RemoveCollider(CreateChildPrimitive(scatterRoot.transform, "Brick_01", PrimitiveType.Cube, new Vector3(-1f, 0.3f, -0.8f), Vector3.zero, new Vector3(0.5f, 0.4f, 0.5f), warningMat));
        RemoveCollider(CreateChildPrimitive(scatterRoot.transform, "Brick_02", PrimitiveType.Cube, new Vector3(0f, 0.3f, -1f), Vector3.zero, new Vector3(0.5f, 0.4f, 0.5f), warningMat));
        RemoveCollider(CreateChildPrimitive(scatterRoot.transform, "Brick_03", PrimitiveType.Cube, new Vector3(1f, 0.3f, -0.8f), Vector3.zero, new Vector3(0.5f, 0.4f, 0.5f), warningMat));
        BoxCollider delayCollider = delayObstacle.AddComponent<BoxCollider>();
        delayCollider.center = new Vector3(0f, 1f, 0f);
        delayCollider.size = new Vector3(7f, 2f, 6f);
        delayCollider.isTrigger = true;
        Rigidbody body = delayObstacle.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        AddBreakableComponent(delayObstacle, scatterRoot.transform, delayCollider);
        delayObstacle.SetActive(false);

        GameObject wheelbarrow = CreateParent(interactiveObstacles, "QTE_Wheelbarrow_DelayTrap", new Vector3(41.5f, 0f, 70f), Vector3.zero);
        CreateChildCube(wheelbarrow.transform, "Bucket", new Vector3(0f, 0.55f, 0f), new Vector3(1.2f, 0.6f, 0.65f), concreteMat);
        CreateChildCube(wheelbarrow.transform, "Handles", new Vector3(-0.85f, 0.55f, 0f), new Vector3(0.9f, 0.12f, 0.55f), woodMat);
        CreateChildPrimitive(wheelbarrow.transform, "Wheel", PrimitiveType.Cylinder, new Vector3(0.75f, 0.25f, 0f), new Vector3(90f, 0f, 0f), new Vector3(0.28f, 0.16f, 0.28f), warningMat);

        CreateHoldTrigger(
            "Wheelbarrow_HoldTrigger",
            wheelbarrow.transform,
            new Vector3(0f, 1f, 0f),
            new Vector3(4f, 2.5f, 4f),
            "Giữ E để lật xe rùa cản Hắc Tinh",
            "Đang lật xe rùa: X%",
            1f,
            wheelbarrow.transform,
            new Vector3(3.5f, 0f, -2f),
            new Vector3(-70f, 20f, 20f),
            null,
            delayObstacle,
            "Cản được nó một chút thôi! Chạy tiếp!",
            "",
            "Wheelbarrow delay trap activated.");
    }

    private static void BuildConstructionFence(Material concreteMat, Material fenceMat, Material warningMat)
    {
        CreateConcreteBlock(new Vector3(42f, 1f, 91f), concreteMat);
        CreateConcreteBlock(new Vector3(48f, 1f, 91f), concreteMat);
        CreateCube(routeBarriers, "ConstructionFence_LeftSideBlocker", new Vector3(40.4f, 1.1f, 91f), Vector3.zero, new Vector3(5.2f, 2.2f, 1.2f), concreteMat);
        CreateCube(routeBarriers, "ConstructionFence_RightSideBlocker", new Vector3(49.6f, 1.1f, 91f), Vector3.zero, new Vector3(5.2f, 2.2f, 1.2f), concreteMat);

        GameObject fence = CreateParent(interactiveObstacles, "QTE_ConstructionFence", new Vector3(45f, 0f, 91f), Vector3.zero);
        CreateChildCube(fence.transform, "Fence_Panel", new Vector3(0f, 1.1f, 0f), new Vector3(3.8f, 2.2f, 0.2f), fenceMat);
        CreateChildCube(fence.transform, "Warning_Rail", new Vector3(0f, 1.1f, -0.14f), new Vector3(3.4f, 0.25f, 0.08f), warningMat);
        BoxCollider blockingCollider = fence.AddComponent<BoxCollider>();
        blockingCollider.center = new Vector3(0f, 1.1f, 0f);
        blockingCollider.size = new Vector3(4f, 2.2f, 0.45f);

        CreateHoldTrigger(
            "ConstructionFence_HoldTrigger",
            fence.transform,
            new Vector3(0f, 1.2f, -2.4f),
            new Vector3(5f, 2.8f, 4f),
            "Giữ E để kéo rào chắn",
            "Đang kéo rào chắn: X%",
            1.8f,
            fence.transform,
            new Vector3(4.5f, 0f, 0f),
            new Vector3(0f, 72f, 0f),
            blockingCollider,
            null,
            "Lối đi đã mở! Chạy tiếp!",
            "Construction fence hold prompt shown.",
            "Construction fence opened; path clear.");
    }

    private static void BuildFallingDebrisArea(Material concreteMat, Material woodMat, Material orangeMat, Material crackMat)
    {
        CreateDebrisWarningZone("FallingDebris_WarningZone_01", new Vector3(34f, 0f, 100f), "Coi chừng! Vật liệu đang rơi!", concreteMat, woodMat, orangeMat, crackMat);
        CreateDebrisWarningZone("FallingDebris_WarningZone_02", new Vector3(20f, 0f, 100f), "Tránh khu vực có đá vụn phía trước!", concreteMat, woodMat, orangeMat, crackMat);
        CreateDebrisWarningZone("FallingDebris_WarningZone_03", new Vector3(-40f, 0f, 185f), "Coi chừng! Vật liệu đang rơi!", concreteMat, woodMat, orangeMat, crackMat);
    }

    private static void BuildMudZone(Material mudMat, Material warningMat)
    {
        GameObject mud = CreateCube(dynamicZones, "SlowZone_Mud", new Vector3(3.6f, 0.22f, 130f), Vector3.zero, new Vector3(5.2f, 0.12f, 16f), mudMat);
        BoxCollider collider = mud.GetComponent<BoxCollider>();
        collider.isTrigger = true;
        SlowZone slowZone = mud.AddComponent<SlowZone>();
        slowZone.slowMoveSpeed = 1.5f;

        CreateWarningTrigger("WarningTrigger_SlowZone", new Vector3(5f, 1.2f, 119f), new Vector3(8f, 3f, 5f), "Bùn lầy làm bạn di chuyển chậm lại!", false);
        CreateGuideBeacon(new Vector3(8.3f, 0f, 123f), warningMat);
        CreateGuideBeacon(new Vector3(8.3f, 0f, 138f), warningMat);
    }

    private static void BuildNarrowPassage(Material concreteMat, Material fenceMat)
    {
        CreateCube(routeBarriers, "NarrowPassage_SouthWall", new Vector3(-18f, 1.2f, 152.4f), Vector3.zero, new Vector3(18f, 2.4f, 2.8f), concreteMat);
        CreateCube(routeBarriers, "NarrowPassage_NorthWall", new Vector3(-18f, 1.2f, 157.6f), Vector3.zero, new Vector3(18f, 2.4f, 2.8f), concreteMat);
        CreateWarningTrigger("NarrowPassage_ConstructionGap", new Vector3(-5f, 1.2f, 155f), new Vector3(8f, 3f, 5f), "Lách qua khe hẹp phía trước!", false);
        CreateGuideBeacon(new Vector3(-8f, 0f, 155f), fenceMat);
        CreateGuideBeacon(new Vector3(-28f, 0f, 155f), fenceMat);
    }

    private static void BuildAmbushDodgeQTE(Material warningMat, Material orangeMat)
    {
        CreateAmbushDodgeTrigger(
            "AmbushDodgeQTE_01",
            new Vector3(32f, 1.2f, 100f),
            new Vector3(8f, 3f, 7f),
            new Vector3(0f, 1f, -9f),
            new Vector3(0f, 1f, 9f),
            "Hắc Tinh lao ra từ hai bên! Nhấn E để né!",
            "Né được rồi! Đừng dừng lại!",
            warningMat,
            orangeMat);

        CreateAmbushDodgeTrigger(
            "AmbushDodgeQTE_02",
            new Vector3(-25f, 1.2f, 155f),
            new Vector3(8f, 3f, 6f),
            new Vector3(0f, 1f, -8f),
            new Vector3(0f, 1f, 8f),
            "Nó vòng qua khe hẹp! Nhấn E để lách người né!",
            "Thoát sát nút! Chạy tới vùng sụp!",
            warningMat,
            orangeMat);
    }

    private static void CreateAmbushDodgeTrigger(string name, Vector3 position, Vector3 size, Vector3 leftOffset, Vector3 rightOffset, string warningMessage, string successMessage, Material warningMat, Material orangeMat)
    {
        GameObject trigger = CreateCube(dynamicZones, name, position, Vector3.zero, size, null);
        Renderer renderer = trigger.GetComponent<Renderer>();
        if (renderer != null)
            renderer.enabled = false;

        BoxCollider collider = trigger.GetComponent<BoxCollider>();
        collider.isTrigger = true;

        AddAmbushDodgeComponent(trigger, leftOffset, rightOffset, warningMessage, successMessage);

        CreateVisualCube(dynamicZones, name + "_Shadow_Left", position + leftOffset + new Vector3(0f, -0.84f, 0f), Vector3.zero, new Vector3(2.4f, 0.04f, 3.2f), orangeMat);
        CreateVisualCube(dynamicZones, name + "_Shadow_Right", position + rightOffset + new Vector3(0f, -0.84f, 0f), Vector3.zero, new Vector3(2.4f, 0.04f, 3.2f), orangeMat);
        CreateGuideBeacon(position + new Vector3(-3.2f, -1.2f, 0f), warningMat);
        CreateGuideBeacon(position + new Vector3(3.2f, -1.2f, 0f), warningMat);
    }

    private static void AddAmbushDodgeComponent(GameObject trigger, Vector3 leftOffset, Vector3 rightOffset, string warningMessage, string successMessage)
    {
        Type ambushType = Type.GetType("S01AmbushDodgeQTE, Assembly-CSharp");
        if (ambushType == null)
        {
            Debug.LogWarning("S01AmbushDodgeQTE is not compiled yet. Let Unity compile scripts, then rebuild S01.");
            return;
        }

        Component ambush = trigger.AddComponent(ambushType);
        SetPublicField(ambush, "player", FindPlayerTransform());
        SetPublicField(ambush, "warningUI", warningUI);
        SetPublicField(ambush, "minionPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(MinionPrefabPath));
        SetPublicField(ambush, "leftStartOffset", leftOffset);
        SetPublicField(ambush, "rightStartOffset", rightOffset);
        SetPublicField(ambush, "attackTargetOffset", Vector3.zero);
        SetPublicField(ambush, "qteDuration", 1f);
        SetPublicField(ambush, "slowMotionTimeScale", 0.28f);
        SetPublicField(ambush, "lungeDistancePastPlayer", 3.5f);
        SetPublicField(ambush, "ambushMinionsJoinChaseOnDodge", true);
        SetPublicField(ambush, "joinedMinionMoveSpeed", 5.5f);
        SetPublicField(ambush, "joinedMinionChaseRange", 120f);
        SetPublicField(ambush, "joinedMinionAttackGrace", 1.2f);
        SetPublicField(ambush, "joinBehindPlayerDistance", 7f);
        SetPublicField(ambush, "joinSideSpacing", 1.2f);
        SetPublicField(ambush, "joinColliderDelay", 0.35f);
        SetPublicField(ambush, "attackFeedbackPause", 0.85f);
        SetPublicField(ambush, "failedDodgeDamage", 20);
        SetPublicField(ambush, "warningMessage", warningMessage);
        SetPublicField(ambush, "successMessage", successMessage);
        SetPublicField(ambush, "failMessage", "Bạn né không kịp. Hắc Tinh đã bắt được bạn!");
    }

    public static void ConfigureExistingAmbushMinions()
    {
        GameObject minionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MinionPrefabPath);
        S01AmbushDodgeQTE[] ambushes = UnityEngine.Object.FindObjectsByType<S01AmbushDodgeQTE>(FindObjectsInactive.Include);

        foreach (S01AmbushDodgeQTE ambush in ambushes)
        {
            ambush.minionPrefab = minionPrefab;
            ambush.ambushMinionsJoinChaseOnDodge = true;
            ambush.joinedMinionMoveSpeed = 5.5f;
            ambush.joinedMinionChaseRange = 120f;
            ambush.joinedMinionAttackGrace = 1.2f;
            ambush.joinBehindPlayerDistance = 7f;
            ambush.joinSideSpacing = 1.2f;
            ambush.joinColliderDelay = 0.35f;
            ambush.attackFeedbackPause = 0.85f;
            ambush.failedDodgeDamage = 20;
            EditorUtility.SetDirty(ambush);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("S01CityEscapeBuilder: configured " + ambushes.Length + " ambush trigger(s) to spawn Minion prefab and join chase after missed attacks.");
    }

    private static void SetPublicField(Component target, string fieldName, object value)
    {
        if (target == null)
            return;

        System.Reflection.FieldInfo field = target.GetType().GetField(fieldName);
        if (field != null)
            field.SetValue(target, value);
    }

    private static void BuildRouteGuidance(Material warningMat, Material orangeMat)
    {
        Vector3[] positions =
        {
            new Vector3(0f, 0.38f, 28f),
            new Vector3(0f, 0.38f, 43f),
            new Vector3(20f, 0.38f, 45f),
            new Vector3(43f, 0.38f, 45f),
            new Vector3(45f, 0.38f, 74f),
            new Vector3(45f, 0.38f, 98f),
            new Vector3(28f, 0.38f, 100f),
            new Vector3(7f, 0.38f, 100f),
            new Vector3(5f, 0.38f, 142f),
            new Vector3(-8f, 0.38f, 155f),
            new Vector3(-38f, 0.38f, 155f),
            new Vector3(-40f, 0.38f, 195f),
            new Vector3(-38f, 0.38f, 215f),
            new Vector3(-10f, 0.38f, 215f),
            new Vector3(8f, 0.38f, 215f),
            new Vector3(10f, 0.38f, 245f)
        };

        float[] yaws = { 0f, 90f, 90f, 0f, 0f, 0f, -90f, -90f, 0f, -90f, 0f, 0f, 90f, 90f, 0f, 0f };

        for (int i = 0; i < positions.Length; i++)
            CreateRouteGuide("RouteGuide_" + (i + 1).ToString("00"), positions[i], yaws[i], warningMat);

        CreateGuideBeacon(new Vector3(8f, 0f, 45f), orangeMat);
        CreateGuideBeacon(new Vector3(45f, 0f, 48f), orangeMat);
        CreateGuideBeacon(new Vector3(42f, 0f, 100f), orangeMat);
        CreateGuideBeacon(new Vector3(8f, 0f, 103f), orangeMat);
        CreateGuideBeacon(new Vector3(2f, 0f, 155f), orangeMat);
        CreateGuideBeacon(new Vector3(-40f, 0f, 160f), orangeMat);
        CreateGuideBeacon(new Vector3(-36f, 0f, 215f), orangeMat);
        CreateGuideBeacon(new Vector3(10f, 0f, 220f), orangeMat);
    }

    private static void BuildStoryTriggers()
    {
        CreateWarningTrigger("TutorialTrigger_Controls", new Vector3(0f, 1.2f, 6f), new Vector3(14f, 3f, 5f), "WASD để di chuyển. Giữ Shift để chạy.", false);
        CreateWarningTrigger("WarningTrigger_ChaseStart", new Vector3(0f, 1.2f, 24f), new Vector3(14f, 3f, 5f), "Chạy! Đừng để Hắc Tinh chạm vào bạn.", false);
        CreateWarningTrigger("WarningTrigger_Detour", new Vector3(0f, 1.2f, 40f), new Vector3(14f, 3f, 5f), "Đường chính bị chặn rồi! Rẽ vào công trường!", false);
        CreateWarningTrigger("WarningTrigger_Fence", new Vector3(45f, 1.2f, 84f), new Vector3(8f, 3f, 5f), "Giữ E để kéo rào chắn mở đường!", false);
        CreateWarningTrigger("WarningTrigger_LongEscape", new Vector3(-40f, 1.2f, 170f), new Vector3(8f, 3f, 6f), "Đừng dừng lại! Tiếp tục theo đèn vàng!", false);
    }

    private static void BuildCollapse(Material collapseMat, Material crackMat, Material warningMat)
    {
        CreateCube(collapseSequence, "Collapse_Zone", new Vector3(10f, 0.18f, 260f), Vector3.zero, new Vector3(14f, 0.16f, 16f), collapseMat);
        CreateCube(collapseSequence, "Collapse_Crack_Mark", new Vector3(10f, 0.31f, 260f), Vector3.zero, new Vector3(9f, 0.05f, 10f), crackMat);
        CreateGuideBeacon(new Vector3(6f, 0f, 252f), warningMat);
        CreateGuideBeacon(new Vector3(14f, 0f, 252f), warningMat);
        CreateWarningTrigger("StoryTrigger_Collapse", new Vector3(10f, 1.2f, 250f), new Vector3(8f, 3f, 6f), "Mặt đất đang nứt ra!", true);
        CreateWarningTrigger("WarningTrigger_Final", new Vector3(10f, 1.2f, 258f), new Vector3(8f, 3f, 5f), "Không phải nó đuổi theo... nó đang lùa chúng ta tới đây!", false);

        GameObject exit = CreateCube(collapseSequence, "ExitTrigger_Test", new Vector3(10f, 1.5f, 264f), Vector3.zero, new Vector3(8f, 3f, 4f), null);
        BoxCollider collider = exit.GetComponent<BoxCollider>();
        collider.isTrigger = true;

        if (IsSceneInBuildSettings("S02_UndergroundCave"))
        {
            SceneTransitionTrigger transition = exit.AddComponent<SceneTransitionTrigger>();
            transition.nextSceneName = "S02_UndergroundCave";
            transition.delayBeforeLoad = 0.1f;
            transition.waitForGroundCollapseSound = true;
            transition.maxGroundCollapseWaitTime = 1.2f;
            transition.postSoundLoadPadding = 0f;
        }
        else
        {
            Debug.LogWarning("S02_UndergroundCave is not in Build Settings. ExitTrigger_Test was created without scene loading.");
        }
    }

    private static void CreateRouteSegment(RouteSegment segment, Material barrierMat)
    {
        Vector3 delta = segment.end - segment.start;
        bool alongZ = Mathf.Abs(delta.z) >= Mathf.Abs(delta.x);
        float length = alongZ ? Mathf.Abs(delta.z) : Mathf.Abs(delta.x);
        Vector3 center = (segment.start + segment.end) * 0.5f;
        center.y = FloorY;

        Vector3 floorScale = alongZ
            ? new Vector3(segment.width, 0.32f, length + 1f)
            : new Vector3(length + 1f, 0.32f, segment.width);
        CreateCube(staticEnvironment, segment.name + "_Floor", center, Vector3.zero, floorScale, segment.material);

        float barrierLength = Mathf.Max(1f, length - 10f);
        Vector3 barrierScale = alongZ
            ? new Vector3(0.45f, BarrierHeight, barrierLength)
            : new Vector3(barrierLength, BarrierHeight, 0.45f);
        Vector3 side = alongZ ? Vector3.right : Vector3.forward;
        float offset = segment.width * 0.5f + 0.25f;

        CreateCube(routeBarriers, segment.name + "_Barrier_A", center + side * offset + Vector3.up * (BarrierHeight * 0.5f), Vector3.zero, barrierScale, barrierMat);
        CreateCube(routeBarriers, segment.name + "_Barrier_B", center - side * offset + Vector3.up * (BarrierHeight * 0.5f), Vector3.zero, barrierScale, barrierMat);
    }

    private static void CreateDebrisWarningZone(string name, Vector3 position, string message, Material concreteMat, Material woodMat, Material orangeMat, Material crackMat)
    {
        GameObject marker = CreateCube(dynamicZones, name + "_GroundMarker", position + Vector3.up * 0.24f, Vector3.zero, new Vector3(5f, 0.05f, 5f), orangeMat);
        RemoveCollider(marker);
        RemoveCollider(CreatePrimitive(dynamicZones, name + "_Rubble_A", PrimitiveType.Sphere, position + new Vector3(-2.2f, 0.35f, 1.8f), Vector3.zero, new Vector3(1f, 0.6f, 0.8f), concreteMat));
        RemoveCollider(CreatePrimitive(dynamicZones, name + "_Rubble_B", PrimitiveType.Sphere, position + new Vector3(2.1f, 0.35f, -1.7f), Vector3.zero, new Vector3(0.9f, 0.55f, 0.9f), concreteMat));
        RemoveCollider(CreateCube(dynamicZones, name + "_TiltedBeam", position + new Vector3(2.8f, 1.1f, 1.8f), new Vector3(0f, 18f, 55f), new Vector3(0.25f, 2.4f, 0.25f), woodMat));
        RemoveCollider(CreateCube(dynamicZones, name + "_Crack", position + new Vector3(0f, 0.28f, 0f), new Vector3(0f, 28f, 0f), new Vector3(0.15f, 0.04f, 4f), crackMat));
        CreateWarningTrigger(name, position + Vector3.up * 1.2f, new Vector3(7f, 3f, 7f), message, false);
    }

    private static void CreateHoldTrigger(string name, Transform parent, Vector3 localPosition, Vector3 size, string prompt, string progress, float duration, Transform target, Vector3 moveOffset, Vector3 rotationOffset, Collider colliderToDisable, GameObject activateOnComplete, string completionMessage, string promptLog, string completeLog)
    {
        GameObject trigger = new GameObject(name);
        trigger.transform.SetParent(parent, false);
        trigger.transform.localPosition = localPosition;
        BoxCollider triggerCollider = trigger.AddComponent<BoxCollider>();
        triggerCollider.size = size;
        triggerCollider.isTrigger = true;

        Type holdType = Type.GetType("HoldInteractionPrompt, Assembly-CSharp");
        if (holdType == null)
        {
            Debug.LogWarning("HoldInteractionPrompt is not compiled yet. Let Unity compile scripts, then rebuild S01.");
            return;
        }

        Component hold = trigger.AddComponent(holdType);
        SetField(holdType, hold, "interactionText", interactionText);
        SetField(holdType, hold, "warningUI", warningUI);
        SetField(holdType, hold, "promptText", prompt);
        SetField(holdType, hold, "progressText", progress);
        SetField(holdType, hold, "holdDuration", duration);
        SetField(holdType, hold, "targetTransform", target);
        SetField(holdType, hold, "completedLocalMoveOffset", moveOffset);
        SetField(holdType, hold, "completedLocalRotationOffset", rotationOffset);
        SetField(holdType, hold, "colliderToDisable", colliderToDisable);
        SetField(holdType, hold, "activateOnComplete", activateOnComplete);
        SetField(holdType, hold, "completionMessage", completionMessage);
        SetField(holdType, hold, "promptShownLogMessage", promptLog);
        SetField(holdType, hold, "completedLogMessage", completeLog);
    }

    private static void AddBreakableComponent(GameObject obstacle, Transform scatterRoot, Collider delayCollider)
    {
        Type breakableType = Type.GetType("BreakableChaseObstacle, Assembly-CSharp");
        if (breakableType == null)
        {
            Debug.LogWarning("BreakableChaseObstacle is not compiled yet. Let Unity compile scripts, then rebuild S01.");
            return;
        }

        Component breakable = obstacle.AddComponent(breakableType);
        SetField(breakableType, breakable, "scatterRoot", scatterRoot);
        SetField(breakableType, breakable, "delayCollider", delayCollider);
        SetField(breakableType, breakable, "slowDuration", 2.5f);
        SetField(breakableType, breakable, "slowMultiplier", 0.04f);
    }

    private static void SetField(Type type, Component component, string fieldName, object value)
    {
        System.Reflection.FieldInfo field = type.GetField(fieldName);
        if (field != null)
            field.SetValue(component, value);
    }

    private static void CreateWarningTrigger(string name, Vector3 position, Vector3 size, string message, bool story)
    {
        GameObject trigger = CreateCube(dynamicZones, name, position, Vector3.zero, size, null);
        BoxCollider collider = trigger.GetComponent<BoxCollider>();
        collider.isTrigger = true;
        S01WarningTrigger warningTrigger = trigger.AddComponent<S01WarningTrigger>();
        warningTrigger.warningUI = warningUI;
        warningTrigger.message = message;
        warningTrigger.showAsStory = story;
        warningTrigger.duration = story ? 6f : 5f;
    }

    private static void CreateRouteGuide(string name, Vector3 position, float yaw, Material material)
    {
        GameObject guide = CreateParent(waypointGuides, name, position, new Vector3(0f, yaw, 0f));
        RemoveCollider(CreateChildPrimitive(guide.transform, "Stem", PrimitiveType.Cube, Vector3.zero, Vector3.zero, new Vector3(0.8f, 0.08f, 3.2f), material));
        RemoveCollider(CreateChildPrimitive(guide.transform, "Head", PrimitiveType.Cube, new Vector3(0f, 0f, 1.9f), new Vector3(0f, 45f, 0f), new Vector3(1.4f, 0.08f, 1.4f), material));
    }

    private static void CreateGuideBeacon(Vector3 position, Material material)
    {
        GameObject beacon = CreateParent(waypointGuides, "GuideBeacon_" + Mathf.RoundToInt(position.x) + "_" + Mathf.RoundToInt(position.z), position, Vector3.zero);
        RemoveCollider(CreateChildPrimitive(beacon.transform, "Base", PrimitiveType.Cylinder, new Vector3(0f, 0.25f, 0f), Vector3.zero, new Vector3(0.45f, 0.5f, 0.45f), material));
        RemoveCollider(CreateChildPrimitive(beacon.transform, "Pole", PrimitiveType.Cube, new Vector3(0f, 1.05f, 0f), Vector3.zero, new Vector3(0.14f, 1.6f, 0.14f), material));
        RemoveCollider(CreateChildPrimitive(beacon.transform, "Lamp", PrimitiveType.Sphere, new Vector3(0f, 1.95f, 0f), Vector3.zero, new Vector3(0.45f, 0.45f, 0.45f), material));
        GameObject lightObject = new GameObject("GuideLight");
        lightObject.transform.SetParent(beacon.transform, false);
        lightObject.transform.localPosition = new Vector3(0f, 1.9f, 0f);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.82f, 0.25f);
        light.intensity = 1.5f;
        light.range = 7f;
    }

    private static void CreateStreetLamp(Vector3 position, Material material)
    {
        GameObject lamp = CreateParent(staticEnvironment, "StreetLamp_" + Mathf.RoundToInt(position.z), position, Vector3.zero);
        CreateChildCube(lamp.transform, "Pole", new Vector3(0f, 2f, 0f), new Vector3(0.18f, 4f, 0.18f), material);
        RemoveCollider(CreateChildPrimitive(lamp.transform, "Lamp", PrimitiveType.Sphere, new Vector3(0f, 4.1f, 0f), Vector3.zero, new Vector3(0.45f, 0.45f, 0.45f), material));
        GameObject lightObject = new GameObject("PointLight");
        lightObject.transform.SetParent(lamp.transform, false);
        lightObject.transform.localPosition = new Vector3(0f, 4f, 0f);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.82f, 0.45f);
        light.intensity = 1.4f;
        light.range = 10f;
    }

    private static void CreateConcreteBlock(Vector3 position, Material material)
    {
        CreateCube(routeBarriers, "ConcreteBlock_" + Mathf.RoundToInt(position.x) + "_" + Mathf.RoundToInt(position.z), position, Vector3.zero, new Vector3(2f, 2f, 1f), material);
    }

    private static void CreateConeRow(Vector3 start, Vector3 direction, int count, Material material)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject cone = CreatePrimitive(staticEnvironment, "Cone_" + i + "_" + Mathf.RoundToInt(start.z), PrimitiveType.Cylinder, start + direction * (i * 1.5f) + Vector3.up * 0.35f, Vector3.zero, new Vector3(0.35f, 0.7f, 0.35f), material);
            RemoveCollider(cone);
        }
    }

    private static void SetupPlayer()
    {
        GameObject player = GameObject.Find("Player");
        if (player == null)
            return;

        CharacterController characterController = player.GetComponent<CharacterController>();
        bool wasEnabled = characterController != null && characterController.enabled;
        if (wasEnabled)
            characterController.enabled = false;

        player.transform.position = new Vector3(0f, 2f, 0f);
        player.transform.rotation = Quaternion.identity;
        player.tag = "Player";

        if (wasEnabled)
            characterController.enabled = true;

        PlayerCombat3D combat = player.GetComponent<PlayerCombat3D>();
        if (combat != null)
            combat.enabled = false;

        PlayerController3D controller = player.GetComponent<PlayerController3D>();
        if (controller != null)
        {
            controller.moveSpeed = 8f;
        }
    }

    private static Transform FindPlayerTransform()
    {
        GameObject player = GameObject.Find("Player");
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player");

        return player != null ? player.transform : null;
    }

    private static void SetupUI()
    {
        Canvas canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("Canvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        interactionText = EnsureUniqueText(canvas.transform, "InteractionText", new Vector2(0.5f, 0f), new Vector2(0f, 100f), 28);
        TMP_Text warningText = EnsureUniqueText(canvas.transform, "WarningText", new Vector2(0.5f, 1f), new Vector2(0f, -120f), 30);
        TMP_Text storyText = EnsureUniqueText(canvas.transform, "StoryText", new Vector2(0.5f, 1f), new Vector2(0f, -70f), 26);

        S01WarningTextUI[] warningUis = Resources.FindObjectsOfTypeAll<S01WarningTextUI>();
        warningUI = canvas.GetComponent<S01WarningTextUI>();
        if (warningUI == null)
            warningUI = canvas.gameObject.AddComponent<S01WarningTextUI>();

        foreach (S01WarningTextUI other in warningUis)
        {
            if (other != null && other != warningUI && other.gameObject.scene.IsValid())
                UnityEngine.Object.DestroyImmediate(other);
        }

        warningUI.warningText = warningText;
        warningUI.storyText = storyText;
        warningUI.defaultDuration = 5f;
        interactionText.gameObject.SetActive(false);
        warningText.gameObject.SetActive(false);
        storyText.gameObject.SetActive(false);
    }

    private static TMP_Text EnsureUniqueText(Transform canvas, string name, Vector2 anchor, Vector2 anchoredPosition, int fontSize)
    {
        TMP_Text[] texts = Resources.FindObjectsOfTypeAll<TMP_Text>();
        TMP_Text keep = null;

        foreach (TMP_Text text in texts)
        {
            if (text.name != name || !text.gameObject.scene.IsValid())
                continue;

            if (keep == null)
                keep = text;
            else
                UnityEngine.Object.DestroyImmediate(text.gameObject);
        }

        if (keep == null)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(canvas, false);
            keep = textObject.AddComponent<TextMeshProUGUI>();
        }
        else
        {
            keep.transform.SetParent(canvas, false);
        }

        keep.fontSize = fontSize;
        keep.alignment = TextAlignmentOptions.Center;
        keep.color = Color.white;
        keep.raycastTarget = false;
        keep.text = string.Empty;

        RectTransform rect = keep.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(1100f, 90f);
        return keep;
    }

    private static void AppendVector(StringBuilder builder, string name, Vector3 value, int indent)
    {
        string spacing = new string(' ', indent);
        builder.Append(spacing).Append("\"").Append(name).Append("\": { ");
        builder.Append("\"x\": ").Append(value.x.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)).Append(", ");
        builder.Append("\"y\": ").Append(value.y.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)).Append(", ");
        builder.Append("\"z\": ").Append(value.z.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)).Append(" }");
    }

    private static string EscapeJsonForReport(string value)
    {
        if (value == null)
            return string.Empty;

        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n")
            .Replace("\t", "\\t");
    }

    private static void CreateSafetyFloor()
    {
        GameObject floor = CreateCube(staticEnvironment, "Safety_Floor_S01", new Vector3(0f, -1f, 130f), Vector3.zero, new Vector3(180f, 0.2f, 330f), null);
        Renderer renderer = floor.GetComponent<Renderer>();
        if (renderer != null)
            renderer.enabled = false;
    }

    private static Transform CreateGroup(Transform parent, string name)
    {
        GameObject group = new GameObject(name);
        group.transform.SetParent(parent, false);
        return group.transform;
    }

    private static GameObject CreateEmpty(Transform parent, string name, Vector3 position)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.transform.position = position;
        return obj;
    }

    private static GameObject CreateParent(Transform parent, string name, Vector3 position, Vector3 rotation)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.transform.position = position;
        obj.transform.eulerAngles = rotation;
        return obj;
    }

    private static GameObject CreateCube(Transform parent, string name, Vector3 position, Vector3 rotation, Vector3 scale, Material material)
    {
        return CreatePrimitive(parent, name, PrimitiveType.Cube, position, rotation, scale, material);
    }

    private static GameObject CreateVisualCube(Transform parent, string name, Vector3 position, Vector3 rotation, Vector3 scale, Material material)
    {
        GameObject obj = CreateCube(parent, name, position, rotation, scale, material);
        RemoveCollider(obj);
        return obj;
    }

    private static GameObject CreateVisualCylinder(Transform parent, string name, Vector3 position, Vector3 rotation, Vector3 scale, Material material)
    {
        GameObject obj = CreatePrimitive(parent, name, PrimitiveType.Cylinder, position, rotation, scale, material);
        RemoveCollider(obj);
        return obj;
    }

    private static GameObject CreateVisualPrimitive(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 rotation, Vector3 scale, Material material)
    {
        GameObject obj = CreatePrimitive(parent, name, type, position, rotation, scale, material);
        RemoveCollider(obj);
        return obj;
    }

    private static GameObject CreateVisualChildCube(Transform parent, string name, Vector3 localPosition, Vector3 localRotation, Vector3 scale, Material material)
    {
        GameObject obj = CreateChildPrimitive(parent, name, PrimitiveType.Cube, localPosition, localRotation, scale, material);
        RemoveCollider(obj);
        return obj;
    }

    private static GameObject CreateVisualChildPrimitive(Transform parent, string name, PrimitiveType type, Vector3 localPosition, Vector3 localRotation, Vector3 scale, Material material)
    {
        GameObject obj = CreateChildPrimitive(parent, name, type, localPosition, localRotation, scale, material);
        RemoveCollider(obj);
        return obj;
    }

    private static GameObject CreateCylinderBetween(Transform parent, string name, Vector3 start, Vector3 end, float radius, Material material)
    {
        Vector3 direction = end - start;
        float length = direction.magnitude;
        if (length <= 0.001f)
            return null;

        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        obj.name = name;
        obj.transform.SetParent(parent, false);
        obj.transform.position = (start + end) * 0.5f;
        obj.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
        obj.transform.localScale = new Vector3(radius, length * 0.5f, radius);
        SetMaterial(obj, material);
        RemoveCollider(obj);
        return obj;
    }

    private static GameObject CreatePrimitive(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 rotation, Vector3 scale, Material material)
    {
        GameObject obj = GameObject.CreatePrimitive(type);
        obj.name = name;
        obj.transform.SetParent(parent, false);
        obj.transform.position = position;
        obj.transform.eulerAngles = rotation;
        obj.transform.localScale = scale;
        SetMaterial(obj, material);
        return obj;
    }

    private static void CreateChildCube(Transform parent, string name, Vector3 localPosition, Vector3 scale, Material material)
    {
        CreateChildPrimitive(parent, name, PrimitiveType.Cube, localPosition, Vector3.zero, scale, material);
    }

    private static GameObject CreateChildPrimitive(Transform parent, string name, PrimitiveType type, Vector3 localPosition, Vector3 localRotation, Vector3 scale, Material material)
    {
        GameObject obj = GameObject.CreatePrimitive(type);
        obj.name = name;
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = localPosition;
        obj.transform.localEulerAngles = localRotation;
        obj.transform.localScale = scale;
        SetMaterial(obj, material);
        return obj;
    }

    private static void SetMaterial(GameObject obj, Material material)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer == null)
            return;

        if (material == null)
            renderer.enabled = false;
        else
            renderer.sharedMaterial = material;
    }

    private static void RemoveCollider(GameObject obj)
    {
        Collider collider = obj.GetComponent<Collider>();
        if (collider != null)
            UnityEngine.Object.DestroyImmediate(collider);
    }

    private static Material CreateMaterial(string name, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = new Material(shader)
        {
            name = name,
            color = color
        };
        return material;
    }

    private static bool IsSceneInBuildSettings(string sceneName)
    {
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (!scene.enabled)
                continue;

            string pathWithoutExtension = System.IO.Path.ChangeExtension(scene.path, null);
            if (System.IO.Path.GetFileName(pathWithoutExtension) == sceneName)
                return true;
        }

        return false;
    }

    private static void CleanupOldS01()
    {
        string[] legacyNames =
        {
            RootName,
            "Road", "Road_01", "Road_02", "Road_03", "Road_04", "Road_05",
            "MetalGate_01", "MetalGate_02",
            "QTE_ConstructionFence", "QTE_Wheelbarrow_Block", "QTE_Wheelbarrow_DelayTrap", "QTE_FallenTree", "QTE_BrokenFence",
            "HacTinhBreakableDelayObstacle",
            "AmbushDodgeQTE_01", "AmbushDodgeQTE_02",
            "SlowZone_Electric", "SlowZone_Mud", "SlowZone_Debris",
            "ExitTrigger_Test", "Collapse_Crack_Mark", "Collapse_Zone", "Safety_Floor_S01",
            "S01_ChaseThreat", "S01_ChaseWaypoints", "MinionSpawn_ChaseStart", "S01_EventController",
            "TutorialTrigger_Controls", "StoryTrigger_Signal", "WarningTrigger_ChaseStart", "WarningTrigger_Detour",
            "WarningTrigger_SlowZone", "StoryTrigger_Collapse", "WarningTrigger_Final",
            "Imported_ModelKit_Visuals", "Road_Surface_Dressing", "Cinematic_SetPieces"
        };

        foreach (string name in legacyNames)
            DeleteSceneObject(name);

        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform sceneTransform in transforms)
        {
            if (!sceneTransform.gameObject.scene.IsValid())
                continue;

            string name = sceneTransform.name;
            if (name.StartsWith("RouteGuide_") ||
                name.StartsWith("GuideBeacon_") ||
                name.StartsWith("FallingDebris_") ||
                name.StartsWith("RouteHint_"))
            {
                UnityEngine.Object.DestroyImmediate(sceneTransform.gameObject);
            }
        }
    }

    private static void DeleteSceneObject(string name)
    {
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform sceneTransform in transforms)
        {
            if (sceneTransform.name == name && sceneTransform.gameObject.scene.IsValid())
            {
                UnityEngine.Object.DestroyImmediate(sceneTransform.gameObject);
                return;
            }
        }
    }

    private readonly struct RouteSegment
    {
        public readonly string name;
        public readonly Vector3 start;
        public readonly Vector3 end;
        public readonly float width;
        public readonly Material material;

        public RouteSegment(string name, Vector3 start, Vector3 end, float width, Material material)
        {
            this.name = name;
            this.start = start;
            this.end = end;
            this.width = width;
            this.material = material;
        }
    }
}

