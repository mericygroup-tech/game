using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

public static class S02CaveBuilder
{
    private const string RootName = "S02_UndergroundCave_Generated";
    private const float RiftZ = 166f;

    public static void BuildScene()
    {
        DeleteOldGeneratedObjects();

        GameObject root = new GameObject(RootName);

        Material floorMat = CreateMaterial("S02_Wet_Cave_Floor", new Color32(55, 49, 45, 255), 0.2f);
        Material wallMat = CreateMaterial("S02_Cave_Wall_Dark", new Color32(31, 30, 32, 255), 0.12f);
        Material ledgeMat = CreateMaterial("S02_Ledge_Stone", new Color32(73, 68, 62, 255), 0.16f);
        Material debrisMat = CreateMaterial("S02_City_Debris", new Color32(88, 82, 75, 255), 0.18f);
        Material bronzeMat = CreateMaterial("S02_Ancient_Bronze", new Color32(188, 124, 43, 255), 0.38f);
        Material blueGlowMat = CreateMaterial("S02_Rift_Blue_Glow", new Color32(72, 202, 255, 255), 0.6f, new Color(0.1f, 0.85f, 1.5f));
        Material purpleGlowMat = CreateMaterial("S02_Rift_Purple_Glow", new Color32(130, 78, 255, 255), 0.65f, new Color(0.65f, 0.25f, 1.7f));
        Material dangerMat = CreateMaterial("S02_HacTinh_Dark", new Color32(14, 9, 22, 255), 0.35f, new Color(0.12f, 0.02f, 0.25f));
        Material safetyMat = CreateMaterial("S02_Safety_Invisible", new Color32(20, 20, 20, 255), 0.1f);

        Canvas canvas = EnsureCanvas();
        TMP_Text interactionText = EnsureText(canvas.transform, "InteractionText", new Vector2(0.5f, 0f), new Vector2(0f, 104f), new Vector2(1200f, 92f), 28);
        TMP_Text warningText = EnsureText(canvas.transform, "WarningText", new Vector2(0.5f, 0.82f), Vector2.zero, new Vector2(1250f, 100f), 31);
        TMP_Text storyText = EnsureText(canvas.transform, "StoryText", new Vector2(0.5f, 0.94f), Vector2.zero, new Vector2(1300f, 90f), 27);
        S01WarningTextUI warningUI = EnsureWarningUI(canvas, warningText, storyText);
        DeleteDuplicateSceneObjects("InteractionText", interactionText.gameObject);
        DeleteDuplicateSceneObjects("WarningText", warningText.gameObject);
        DeleteDuplicateSceneObjects("StoryText", storyText.gameObject);

        Transform player = SetupPlayer();
        SetupLighting();

        CreateSafetyFloor(root, safetyMat);
        BuildWakeArea(root, floorMat, wallMat, ledgeMat, debrisMat, blueGlowMat);
        BuildAncientSignsPath(root, floorMat, wallMat, ledgeMat, bronzeMat, blueGlowMat, warningUI, interactionText, storyText);
        BuildVoicesAndPressurePath(root, floorMat, wallMat, ledgeMat, debrisMat, bronzeMat, blueGlowMat, dangerMat, warningUI);
        GameObject timeRift = BuildTimeRiftChamber(root, floorMat, wallMat, ledgeMat, bronzeMat, blueGlowMat, purpleGlowMat);

        S02CaveEventController controller = CreateEventController(root, player, warningUI, interactionText, warningText, timeRift.transform);
        CreateStoryTriggers(root, controller, warningUI);

        Selection.activeGameObject = root;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Rebuilt S02_UndergroundCave as a survival mystery TimeRift scene.");
    }

    private static Transform SetupPlayer()
    {
        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            Debug.LogWarning("S02CaveBuilder: Player not found. Scene geometry was still rebuilt.");
            return null;
        }

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
        if (combat == null)
            combat = player.AddComponent<PlayerCombat3D>();

        combat.enabled = false;

        PlayerController3D controller = player.GetComponent<PlayerController3D>();
        if (controller != null)
        {
            controller.moveSpeed = 8f;
        }

        return player.transform;
    }

    private static void SetupLighting()
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.025f, 0.03f, 0.04f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.035f, 0.038f, 0.047f);
        RenderSettings.fogDensity = 0.018f;

        Light directional = Object.FindAnyObjectByType<Light>();
        if (directional != null && directional.type == LightType.Directional)
        {
            directional.intensity = 0.25f;
            directional.color = new Color(0.55f, 0.62f, 0.76f);
            directional.transform.rotation = Quaternion.Euler(50f, -28f, 0f);
            EditorUtility.SetDirty(directional);
        }
    }

    private static void BuildWakeArea(GameObject root, Material floorMat, Material wallMat, Material ledgeMat, Material debrisMat, Material glowMat)
    {
        CreateFloor(root, "WakeArea_BrokenCitySlab", new Vector3(0f, 0f, 8f), new Vector3(20f, 0.45f, 24f), floorMat);
        CreateFloor(root, "WakeArea_CollapseLip", new Vector3(0f, 0.12f, -5f), new Vector3(16f, 0.35f, 5f), ledgeMat);
        CreateWallPair(root, "WakeArea_RoughWalls", 0f, 22f, 10.5f, 4.8f, wallMat);

        CreateCube(root, "Collapsed_Road_Block_Back", new Vector3(0f, 1.1f, -8f), Vector3.zero, new Vector3(17f, 2.2f, 2.4f), debrisMat);
        CreateRubbleCluster(root, "Wake_Rubble_Left", new Vector3(-6.5f, 0.2f, 4.5f), debrisMat);
        CreateRubbleCluster(root, "Wake_Rubble_Right", new Vector3(6.4f, 0.2f, 12f), debrisMat);
        CreateBrokenConcreteBars(root, "Fallen_City_Rebar", new Vector3(-3.7f, 0.55f, 11.5f), debrisMat);

        CreateCeilingHole(root, "S01_Collapse_Hole_Above", new Vector3(0f, 6f, 2.5f), 6f, debrisMat);
        CreateLight(root, "S01_Collapse_Hole_Light", new Vector3(0f, 5.5f, 3f), new Color(0.52f, 0.64f, 0.9f), 2.5f, 18f);
        CreateGlowMarker(root, "Phone_Last_Light", new Vector3(-2.2f, 0.42f, 3f), new Vector3(0f, 24f, 0f), glowMat);
        CreateArrowOnFloor(root, "WakeArea_Route_Glow", new Vector3(0f, 0.31f, 17f), 0f, glowMat);
    }

    private static void BuildAncientSignsPath(GameObject root, Material floorMat, Material wallMat, Material ledgeMat, Material bronzeMat, Material glowMat, S01WarningTextUI warningUI, TMP_Text interactionText, TMP_Text storyText)
    {
        CreateFloor(root, "AncientSigns_Path_A", new Vector3(0f, 0f, 32f), new Vector3(15f, 0.45f, 24f), floorMat);
        CreateFloor(root, "AncientSigns_Path_B", new Vector3(-6f, 0f, 52f), new Vector3(15f, 0.45f, 24f), floorMat);
        CreateFloor(root, "AncientSigns_Path_C", new Vector3(5f, 0f, 72f), new Vector3(17f, 0.45f, 24f), floorMat);

        CreateWallPair(root, "AncientSigns_Walls_A", 20f, 44f, 9f, 4.5f, wallMat);
        CreateAngledWall(root, "AncientSigns_LeftTurn_Wall_A", new Vector3(-12.5f, 2.1f, 52f), new Vector3(0f, -15f, 0f), new Vector3(1.1f, 4.2f, 25f), wallMat);
        CreateAngledWall(root, "AncientSigns_RightTurn_Wall_A", new Vector3(4f, 2.1f, 52f), new Vector3(0f, -15f, 0f), new Vector3(1.1f, 4.2f, 25f), wallMat);
        CreateAngledWall(root, "AncientSigns_LeftTurn_Wall_B", new Vector3(-3.5f, 2.1f, 72f), new Vector3(0f, 18f, 0f), new Vector3(1.1f, 4.2f, 24f), wallMat);
        CreateAngledWall(root, "AncientSigns_RightTurn_Wall_B", new Vector3(14f, 2.1f, 72f), new Vector3(0f, 18f, 0f), new Vector3(1.1f, 4.2f, 24f), wallMat);

        for (int i = 0; i < 7; i++)
        {
            float z = 26f + i * 7.2f;
            float x = i < 3 ? 0f : (i < 5 ? -6f : 5f);
            float side = i % 2 == 0 ? -1f : 1f;
            CreateWallSymbol(root, "DongSon_WallSymbol_" + (i + 1).ToString("00"), new Vector3(x + side * 7.6f, 2f, z), side > 0f ? -90f : 90f, bronzeMat, glowMat);
            CreateGlowCrack(root, "Guiding_Crack_" + (i + 1).ToString("00"), new Vector3(x, 0.32f, z + 2.2f), i % 2 == 0 ? 12f : -16f, glowMat);
        }

        CreateInspectable(root, "Inspect_DongSonSymbol", new Vector3(-7.6f, 1.9f, 34f), new Vector3(0f, 90f, 0f), PrimitiveType.Cylinder, new Vector3(1.55f, 0.08f, 1.55f), bronzeMat, glowMat, warningUI, interactionText, storyText,
            "Nhấn E để kiểm tra hoa văn",
            "Văn An: Hoa văn này giống trống đồng... nhưng sao lại ở dưới lòng thành phố?");
        CreateInspectable(root, "Inspect_CoLoaStoneMark", new Vector3(12.8f, 1.9f, 74f), new Vector3(0f, -90f, 0f), PrimitiveType.Cylinder, new Vector3(1.8f, 0.08f, 1.8f), bronzeMat, glowMat, warningUI, interactionText, storyText,
            "Nhấn E để xem ký hiệu Loa Thành",
            "Các vòng khắc nối nhau như tường thành xoắn ốc. Cổ Loa?");
        CreateArrowOnFloor(root, "AncientSigns_Route_Glow_A", new Vector3(-5f, 0.31f, 47f), -18f, glowMat);
        CreateArrowOnFloor(root, "AncientSigns_Route_Glow_B", new Vector3(4f, 0.31f, 69f), 18f, glowMat);
    }

    private static void BuildVoicesAndPressurePath(GameObject root, Material floorMat, Material wallMat, Material ledgeMat, Material debrisMat, Material bronzeMat, Material glowMat, Material dangerMat, S01WarningTextUI warningUI)
    {
        CreateFloor(root, "Voices_Path_NarrowBridge", new Vector3(0f, 0f, 96f), new Vector3(12f, 0.45f, 28f), floorMat);
        CreateFloor(root, "Pressure_Run_Path_A", new Vector3(7f, 0f, 119f), new Vector3(17f, 0.45f, 30f), floorMat);
        CreateFloor(root, "Pressure_Run_Path_B", new Vector3(-4f, 0f, 142f), new Vector3(17f, 0.45f, 28f), floorMat);
        CreateFloor(root, "RiftApproach_Bridge", new Vector3(0f, 0f, 154f), new Vector3(13f, 0.45f, 18f), ledgeMat);

        CreateWallPair(root, "Voices_Walls", 84f, 108f, 8f, 5f, wallMat);
        CreateAngledWall(root, "Pressure_LeftWall_A", new Vector3(-3f, 2.2f, 119f), new Vector3(0f, -16f, 0f), new Vector3(1.1f, 4.4f, 31f), wallMat);
        CreateAngledWall(root, "Pressure_RightWall_A", new Vector3(16f, 2.2f, 119f), new Vector3(0f, -16f, 0f), new Vector3(1.1f, 4.4f, 31f), wallMat);
        CreateAngledWall(root, "Pressure_LeftWall_B", new Vector3(-13f, 2.2f, 142f), new Vector3(0f, 16f, 0f), new Vector3(1.1f, 4.4f, 31f), wallMat);
        CreateAngledWall(root, "Pressure_RightWall_B", new Vector3(6f, 2.2f, 142f), new Vector3(0f, 16f, 0f), new Vector3(1.1f, 4.4f, 31f), wallMat);

        CreateCeilingHole(root, "HacTinh_Descent_Hole", new Vector3(1f, 6.2f, 88f), 5.5f, dangerMat);
        CreateLight(root, "HacTinh_Descent_DarkPurpleLight", new Vector3(1f, 4.8f, 88f), new Color(0.28f, 0.07f, 0.55f), 3.6f, 18f);
        CreateLight(root, "RiftApproach_BlueGuideLight", new Vector3(0f, 3f, 150f), new Color(0.1f, 0.82f, 1f), 2.2f, 18f);

        CreateRubbleCluster(root, "Pressure_Rubble_Left", new Vector3(-5.5f, 0.2f, 112f), debrisMat);
        CreateRubbleCluster(root, "Pressure_Rubble_Right", new Vector3(13f, 0.2f, 129f), debrisMat);
        CreateRubbleCluster(root, "Pressure_Rubble_CenterGap", new Vector3(-1.5f, 0.2f, 139f), debrisMat);
        CreateGlowCrack(root, "Pressure_Crack_A", new Vector3(7f, 0.32f, 118f), -22f, glowMat);
        CreateGlowCrack(root, "Pressure_Crack_B", new Vector3(-4f, 0.32f, 140f), 24f, glowMat);
        CreateArrowOnFloor(root, "Pressure_Route_Glow_A", new Vector3(7f, 0.31f, 122f), -14f, glowMat);
        CreateArrowOnFloor(root, "Pressure_Route_Glow_B", new Vector3(-3.5f, 0.31f, 145f), 14f, glowMat);

        CreateMarker(root, "CaveMinionSpawn_01", new Vector3(1f, 2.5f, 88f));
        CreateMarker(root, "CaveMinionSpawn_02", new Vector3(-5.5f, 1f, 92f));
        CreateMarker(root, "CaveMinionSpawn_03", new Vector3(6f, 1f, 95f));

        CreateS02WarningTrigger(root, "UnstableGround_Zone_01", new Vector3(7f, 1.5f, 122f), new Vector3(16f, 3f, 7f), "Nền đá đang nứt. Đừng đứng lại!", false, warningUI);
        CreateS02WarningTrigger(root, "UnstableGround_Zone_02", new Vector3(-4f, 1.5f, 142f), new Vector3(16f, 3f, 7f), "Ánh sáng xanh đang dẫn về phía trước.", true, warningUI);
        CreateWallSymbol(root, "Final_Bronze_RiftWarning", new Vector3(6.8f, 2.2f, 151f), -90f, bronzeMat, glowMat);
    }

    private static GameObject BuildTimeRiftChamber(GameObject root, Material floorMat, Material wallMat, Material ledgeMat, Material bronzeMat, Material blueGlowMat, Material purpleGlowMat)
    {
        GameObject mainFloor = CreatePrimitive(root, "TimeRiftChamber_MainFloor", PrimitiveType.Cylinder, new Vector3(0f, -0.05f, RiftZ), Vector3.zero, new Vector3(32f, 0.4f, 32f), floorMat);
        RemoveCollider(mainFloor);
        GameObject raisedPlatform = CreatePrimitive(root, "TimeRiftChamber_RaisedPlatform", PrimitiveType.Cylinder, new Vector3(0f, 0.08f, RiftZ), Vector3.zero, new Vector3(14f, 0.24f, 14f), ledgeMat);
        RemoveCollider(raisedPlatform);
        CreateFloor(root, "TimeRiftChamber_PlayableFloorCollider", new Vector3(0f, 0f, RiftZ - 2f), new Vector3(34f, 0.46f, 38f), floorMat);
        HideRenderer(root, "TimeRiftChamber_PlayableFloorCollider");
        CreateFloor(root, "RiftApproach_EntryFiller", new Vector3(0f, 0f, RiftZ - 12f), new Vector3(14f, 0.46f, 10f), ledgeMat);
        GameObject bronzeSeal = CreatePrimitive(root, "TimeRiftChamber_InnerBronzeSeal", PrimitiveType.Cylinder, new Vector3(0f, 0.24f, RiftZ), Vector3.zero, new Vector3(9f, 0.035f, 9f), bronzeMat);
        RemoveCollider(bronzeSeal);
        CreateCircularWall(root, "TimeRiftChamber_RoughWall", new Vector3(0f, 2.1f, RiftZ), 20f, wallMat);

        CreateLight(root, "TimeRiftChamber_CoolFill", new Vector3(0f, 7.5f, RiftZ - 5f), new Color(0.13f, 0.46f, 0.85f), 2.2f, 30f);

        GameObject rift = CreateParent(root, "TimeRift", new Vector3(0f, 2.75f, RiftZ), Vector3.zero);
        GameObject riftCore = CreatePrimitive(rift, "TimeRift_Core", PrimitiveType.Sphere, Vector3.zero, Vector3.zero, new Vector3(2.2f, 3.6f, 2.2f), purpleGlowMat);
        RemoveCollider(riftCore);
        GameObject riftInnerLight = CreatePrimitive(rift, "TimeRift_InnerLight", PrimitiveType.Sphere, new Vector3(0f, 0.1f, 0f), Vector3.zero, new Vector3(1.05f, 2.2f, 1.05f), blueGlowMat);
        RemoveCollider(riftInnerLight);
        CreateRing(rift, "Rift_Ring_Vertical_A", Vector3.zero, Vector3.zero, 3.5f, blueGlowMat);
        CreateRing(rift, "Rift_Ring_Vertical_B", Vector3.zero, new Vector3(0f, 90f, 0f), 3.8f, purpleGlowMat);
        CreateRing(rift, "Rift_Ring_Tilted", Vector3.zero, new Vector3(64f, 0f, 0f), 4.2f, blueGlowMat);
        GameObject interactCircle = CreatePrimitive(rift, "TimeRift_InteractCircle", PrimitiveType.Cylinder, new Vector3(0f, -2.58f, 0f), Vector3.zero, new Vector3(7.8f, 0.035f, 7.8f), blueGlowMat);
        RemoveCollider(interactCircle);
        CreateLight(rift, "TimeRift_PointLight", new Vector3(0f, 0.6f, 0f), new Color(0.45f, 0.32f, 1f), 6.2f, 24f);
        CreateLight(rift, "TimeRift_Blue_CoreLight", new Vector3(0f, -0.8f, 0f), new Color(0.1f, 0.9f, 1f), 4.5f, 18f);

        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f;
            Vector3 position = new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad) * 10.7f, 0f, RiftZ + Mathf.Cos(angle * Mathf.Deg2Rad) * 10.7f);
            CreatePylon(root, "CoLoa_RiftPylon_" + (i + 1).ToString("00"), position, angle + 180f, ledgeMat, bronzeMat, blueGlowMat);
        }

        CreateGlowCrack(root, "RiftApproach_Crack_Line", new Vector3(0f, 0.31f, RiftZ - 9f), 0f, blueGlowMat, 14f);
        CreateArrowOnFloor(root, "TimeRift_Final_Route_Glow", new Vector3(0f, 0.31f, RiftZ - 12f), 0f, blueGlowMat);
        return rift;
    }

    private static S02CaveEventController CreateEventController(GameObject root, Transform player, S01WarningTextUI warningUI, TMP_Text interactionText, TMP_Text progressText, Transform timeRift)
    {
        GameObject controllerObject = new GameObject("S02CaveEventController");
        controllerObject.transform.SetParent(root.transform, false);
        S02CaveEventController controller = controllerObject.AddComponent<S02CaveEventController>();
        S02CutsceneController cutscene = controllerObject.AddComponent<S02CutsceneController>();
        controller.player = player;
        controller.playerCombat = player != null ? player.GetComponent<PlayerCombat3D>() : null;
        controller.warningUI = warningUI;
        controller.interactionText = interactionText;
        controller.progressText = progressText;
        controller.timeRift = timeRift;
        controller.cutsceneController = cutscene;
        controller.stabilizeDuration = 30f;
        controller.enemySpawnInterval = 6f;
        controller.maxActiveEnemies = 4;
        controller.nextSceneName = "S03_CoLoaArrival";

        Camera mainCamera = Camera.main;
        cutscene.player = player;
        cutscene.playerController = player != null ? player.GetComponent<PlayerController3D>() : null;
        cutscene.playerCombat = player != null ? player.GetComponent<PlayerCombat3D>() : null;
        cutscene.warningUI = warningUI;
        cutscene.interactionText = interactionText;
        cutscene.timeRift = timeRift;
        cutscene.mainCamera = mainCamera;
        cutscene.thirdPersonCamera = mainCamera != null ? mainCamera.GetComponent<ThirdPersonCamera>() : null;

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Minion.prefab");
        if (prefab != null)
            controller.minionPrefab = prefab;
        else
            Debug.LogWarning("S02CaveBuilder: Assets/Prefabs/Minion.prefab not found. S02 will still run without spawning minions.");

        controller.enemySpawnPoints = new[]
        {
            GameObject.Find("CaveMinionSpawn_01")?.transform,
            GameObject.Find("CaveMinionSpawn_02")?.transform,
            GameObject.Find("CaveMinionSpawn_03")?.transform
        };

        return controller;
    }

    private static void CreateStoryTriggers(GameObject root, S02CaveEventController controller, S01WarningTextUI warningUI)
    {
        CreateTrigger(root, "Trigger_AncientSigns", new Vector3(0f, 1.5f, 27f), new Vector3(14f, 3f, 6f), controller, S02TimeRiftTrigger.TriggerKind.AncientSigns);
        CreateTrigger(root, "VoiceTrigger_01", new Vector3(-6f, 1.5f, 58f), new Vector3(14f, 3f, 7f), controller, S02TimeRiftTrigger.TriggerKind.Voices);
        CreateTrigger(root, "HacTinhDescendTrigger", new Vector3(0f, 1.5f, 101f), new Vector3(14f, 3f, 7f), controller, S02TimeRiftTrigger.TriggerKind.BlackStarDescent);
        CreateTrigger(root, "TimeRift_ResonanceZone", new Vector3(0f, 1.7f, RiftZ), new Vector3(9f, 3.4f, 9f), controller, S02TimeRiftTrigger.TriggerKind.TimeRift);
        CreateS02WarningTrigger(root, "VoiceTrigger_02", new Vector3(5f, 1.5f, 76f), new Vector3(15f, 3f, 7f), "Tiếng gọi vọng lại từ phía trước. Đi theo ánh sáng xanh.", true, warningUI);
    }

    private static void CreateTrigger(GameObject root, string name, Vector3 position, Vector3 scale, S02CaveEventController controller, S02TimeRiftTrigger.TriggerKind kind)
    {
        GameObject trigger = CreateCube(root, name, position, Vector3.zero, scale, null);
        BoxCollider collider = trigger.GetComponent<BoxCollider>();
        if (collider != null)
            collider.isTrigger = true;

        S02TimeRiftTrigger triggerScript = trigger.AddComponent<S02TimeRiftTrigger>();
        triggerScript.controller = controller;
        triggerScript.triggerKind = kind;
    }

    private static void CreateS02WarningTrigger(GameObject root, string name, Vector3 position, Vector3 scale, string message, bool story, S01WarningTextUI warningUI)
    {
        GameObject trigger = CreateCube(root, name, position, Vector3.zero, scale, null);
        BoxCollider collider = trigger.GetComponent<BoxCollider>();
        if (collider != null)
            collider.isTrigger = true;

        S01WarningTrigger warningTrigger = trigger.AddComponent<S01WarningTrigger>();
        warningTrigger.warningUI = warningUI;
        warningTrigger.message = message;
        warningTrigger.showAsStory = story;
        warningTrigger.duration = story ? 5.5f : 4f;
    }

    private static void CreateInspectable(GameObject root, string name, Vector3 position, Vector3 rotation, PrimitiveType visualType, Vector3 visualScale, Material visualMaterial, Material glowMaterial, S01WarningTextUI warningUI, TMP_Text interactionText, TMP_Text storyText, string prompt, string story)
    {
        GameObject parent = CreateParent(root, name, position, rotation);
        GameObject visual = CreatePrimitive(parent, name + "_Visual", visualType, Vector3.zero, Vector3.zero, visualScale, visualMaterial);
        Collider visualCollider = visual.GetComponent<Collider>();
        if (visualCollider != null)
            Object.DestroyImmediate(visualCollider);

        GameObject completedGlow = CreatePrimitive(parent, name + "_InteractionGlow", PrimitiveType.Sphere, new Vector3(0f, 0f, 0.12f), Vector3.zero, new Vector3(1.25f, 0.12f, 1.25f), glowMaterial);
        RemoveCollider(completedGlow);
        completedGlow.SetActive(false);

        GameObject trigger = CreateCube(parent, name + "_Trigger", Vector3.zero, Vector3.zero, new Vector3(3.4f, 2.6f, 3.4f), null);
        BoxCollider collider = trigger.GetComponent<BoxCollider>();
        if (collider != null)
            collider.isTrigger = true;

        SimpleInteractPrompt promptScript = trigger.AddComponent<SimpleInteractPrompt>();
        promptScript.interactionText = interactionText;
        promptScript.storyText = storyText;
        promptScript.warningUI = warningUI;
        promptScript.promptMessage = prompt;
        promptScript.storyMessage = story;
        promptScript.storyDuration = 5.5f;
        promptScript.triggerOnce = true;
        promptScript.highlightRenderer = visual.GetComponent<Renderer>();
        promptScript.activateOnInteract = completedGlow;
    }

    private static Canvas EnsureCanvas()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
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

        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        return canvas;
    }

    private static TMP_Text EnsureText(Transform parent, string name, Vector2 anchor, Vector2 anchoredPosition, Vector2 size, int fontSize)
    {
        GameObject textObject = FindSceneObject(name);
        if (textObject == null)
            textObject = new GameObject(name);

        textObject.transform.SetParent(parent, false);

        Text oldText = textObject.GetComponent<Text>();
        if (oldText != null)
            Object.DestroyImmediate(oldText);

        TextMeshProUGUI tmpText = textObject.GetComponent<TextMeshProUGUI>();
        if (tmpText == null)
            tmpText = textObject.AddComponent<TextMeshProUGUI>();

        tmpText.fontSize = fontSize;
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.color = Color.white;
        tmpText.raycastTarget = false;
        tmpText.textWrappingMode = TextWrappingModes.Normal;
        tmpText.text = string.Empty;

        RectTransform rect = tmpText.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        textObject.SetActive(false);
        return tmpText;
    }

    private static S01WarningTextUI EnsureWarningUI(Canvas canvas, TMP_Text warningText, TMP_Text storyText)
    {
        S01WarningTextUI warningUI = canvas.GetComponent<S01WarningTextUI>();
        if (warningUI == null)
            warningUI = canvas.gameObject.AddComponent<S01WarningTextUI>();

        warningUI.warningText = warningText;
        warningUI.storyText = storyText;
        warningUI.defaultDuration = 4.5f;
        return warningUI;
    }

    private static void CreateSafetyFloor(GameObject root, Material material)
    {
        GameObject floor = CreateCube(root, "Safety_Floor_S02", new Vector3(0f, -1.15f, 86f), Vector3.zero, new Vector3(120f, 0.2f, 230f), material);
        Renderer renderer = floor.GetComponent<Renderer>();
        if (renderer != null)
            renderer.enabled = false;
    }

    private static void CreateFloor(GameObject root, string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject floor = CreateCube(root, name, position, Vector3.zero, scale, material);
        Renderer renderer = floor.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = true;
        }
    }

    private static void CreateWallPair(GameObject root, string name, float zStart, float zEnd, float halfWidth, float height, Material material)
    {
        float length = zEnd - zStart;
        float centerZ = zStart + length * 0.5f;
        CreateCube(root, name + "_Left", new Vector3(-halfWidth, height * 0.5f, centerZ), Vector3.zero, new Vector3(1.4f, height, length), material);
        CreateCube(root, name + "_Right", new Vector3(halfWidth, height * 0.5f, centerZ), Vector3.zero, new Vector3(1.4f, height, length), material);
    }

    private static void CreateAngledWall(GameObject root, string name, Vector3 position, Vector3 rotation, Vector3 scale, Material material)
    {
        CreateCube(root, name, position, rotation, scale, material);
    }

    private static void CreateCircularWall(GameObject root, string name, Vector3 center, float radius, Material material)
    {
        for (int i = 0; i < 20; i++)
        {
            if (i >= 8 && i <= 12)
                continue;

            float angle = i * 18f;
            Vector3 position = center + new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad) * radius, 0f, Mathf.Cos(angle * Mathf.Deg2Rad) * radius);
            CreateCube(root, name + "_" + i.ToString("00"), position, new Vector3(0f, angle, 0f), new Vector3(5.8f, 4.5f, 1.2f), material);
        }
    }

    private static void CreateCeilingHole(GameObject root, string name, Vector3 position, float radius, Material material)
    {
        GameObject hole = CreatePrimitive(root, name, PrimitiveType.Cylinder, position, new Vector3(90f, 0f, 0f), new Vector3(radius, 0.25f, radius), material);
        Collider collider = hole.GetComponent<Collider>();
        if (collider != null)
            Object.DestroyImmediate(collider);
    }

    private static void CreateBrokenConcreteBars(GameObject root, string name, Vector3 position, Material material)
    {
        GameObject group = CreateParent(root, name, position, Vector3.zero);
        for (int i = 0; i < 5; i++)
        {
            float x = -1.4f + i * 0.7f;
            CreateCube(group, "Rebar_" + i, new Vector3(x, 0.25f, 0f), new Vector3(0f, 0f, 72f), new Vector3(0.08f, 1.4f, 0.08f), material);
        }

        CreateCube(group, "Broken_Slab", new Vector3(0f, 0.1f, 0f), new Vector3(0f, 18f, 0f), new Vector3(3f, 0.22f, 1.4f), material);
    }

    private static void CreateRubbleCluster(GameObject root, string name, Vector3 position, Material material)
    {
        GameObject group = CreateParent(root, name, position, Vector3.zero);
        CreatePrimitive(group, "Rock_A", PrimitiveType.Sphere, new Vector3(-0.7f, 0.28f, -0.2f), Vector3.zero, new Vector3(1.4f, 0.7f, 1.1f), material);
        CreatePrimitive(group, "Rock_B", PrimitiveType.Sphere, new Vector3(0.5f, 0.38f, 0.35f), Vector3.zero, new Vector3(1.2f, 0.8f, 1.3f), material);
        CreatePrimitive(group, "Rock_C", PrimitiveType.Cube, new Vector3(0.05f, 0.22f, -0.75f), new Vector3(0f, 28f, 0f), new Vector3(1.4f, 0.42f, 0.8f), material);
    }

    private static void CreateWallSymbol(GameObject root, string name, Vector3 position, float yaw, Material bronzeMat, Material glowMat)
    {
        GameObject symbol = CreateParent(root, name, position, new Vector3(0f, yaw, 0f));
        CreatePrimitive(symbol, "Bronze_Disc", PrimitiveType.Cylinder, Vector3.zero, new Vector3(90f, 0f, 0f), new Vector3(1.25f, 0.055f, 1.25f), bronzeMat);
        CreateCube(symbol, "Glow_Line_Horizontal", new Vector3(0f, 0f, -0.08f), Vector3.zero, new Vector3(1.5f, 0.08f, 0.06f), glowMat);
        CreateCube(symbol, "Glow_Line_Vertical", new Vector3(0f, 0f, -0.09f), Vector3.zero, new Vector3(0.08f, 1.5f, 0.06f), glowMat);
    }

    private static void CreatePylon(GameObject root, string name, Vector3 position, float yaw, Material stoneMat, Material bronzeMat, Material glowMat)
    {
        GameObject pylon = CreateParent(root, name, position, new Vector3(0f, yaw, 0f));
        CreateCube(pylon, "Stone", new Vector3(0f, 1.8f, 0f), Vector3.zero, new Vector3(1.2f, 3.6f, 1.2f), stoneMat);
        CreateCube(pylon, "Bronze_Face", new Vector3(0f, 2.25f, -0.64f), Vector3.zero, new Vector3(0.86f, 0.86f, 0.08f), bronzeMat);
        CreateCube(pylon, "Lit_Cut", new Vector3(0f, 2.25f, -0.7f), Vector3.zero, new Vector3(0.72f, 0.08f, 0.06f), glowMat);
    }

    private static void CreateRing(GameObject parent, string name, Vector3 localPosition, Vector3 localRotation, float radius, Material material)
    {
        GameObject ring = CreateParent(parent, name, localPosition, localRotation);
        for (int i = 0; i < 20; i++)
        {
            float angle = i * 18f;
            Vector3 position = new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad) * radius, Mathf.Cos(angle * Mathf.Deg2Rad) * radius, 0f);
            GameObject segment = CreateCube(ring, "Segment_" + i.ToString("00"), position, new Vector3(0f, 0f, -angle), new Vector3(0.18f, 0.82f, 0.18f), material);
            RemoveCollider(segment);
        }
    }

    private static void CreateGlowMarker(GameObject root, string name, Vector3 position, Vector3 rotation, Material material)
    {
        GameObject marker = CreateCube(root, name, position, rotation, new Vector3(0.5f, 0.08f, 0.9f), material);
        RemoveCollider(marker);
    }

    private static void CreateGlowCrack(GameObject root, string name, Vector3 position, float yaw, Material material, float length = 5.5f)
    {
        GameObject crack = CreateCube(root, name, position, new Vector3(0f, yaw, 0f), new Vector3(0.18f, 0.06f, length), material);
        RemoveCollider(crack);
    }

    private static void RemoveCollider(GameObject obj)
    {
        if (obj == null)
            return;

        Collider collider = obj.GetComponent<Collider>();
        if (collider != null)
            Object.DestroyImmediate(collider);
    }

    private static void HideRenderer(GameObject root, string objectName)
    {
        Transform target = root.transform.Find(objectName);
        if (target == null)
            return;

        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer != null)
            renderer.enabled = false;
    }

    private static void CreateArrowOnFloor(GameObject root, string name, Vector3 position, float yaw, Material material)
    {
        GameObject arrow = CreateParent(root, name, position, new Vector3(0f, yaw, 0f));
        CreateCube(arrow, "Arrow_Body", new Vector3(0f, 0f, 0f), Vector3.zero, new Vector3(0.55f, 0.055f, 2.2f), material);
        CreateCube(arrow, "Arrow_Head_Left", new Vector3(-0.32f, 0f, 1.05f), new Vector3(0f, -35f, 0f), new Vector3(0.42f, 0.055f, 1.1f), material);
        CreateCube(arrow, "Arrow_Head_Right", new Vector3(0.32f, 0f, 1.05f), new Vector3(0f, 35f, 0f), new Vector3(0.42f, 0.055f, 1.1f), material);

        Collider[] colliders = arrow.GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders)
            Object.DestroyImmediate(collider);
    }

    private static void CreateLight(GameObject root, string name, Vector3 position, Color color, float intensity, float range)
    {
        GameObject lightObject = new GameObject(name);
        lightObject.transform.SetParent(root.transform, false);
        lightObject.transform.localPosition = position;
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
    }

    private static GameObject CreateMarker(GameObject root, string name, Vector3 position)
    {
        GameObject marker = new GameObject(name);
        marker.transform.SetParent(root.transform, false);
        marker.transform.localPosition = position;
        return marker;
    }

    private static GameObject CreateParent(GameObject root, string name, Vector3 position, Vector3 rotation)
    {
        GameObject parent = new GameObject(name);
        parent.transform.SetParent(root.transform, false);
        parent.transform.localPosition = position;
        parent.transform.localEulerAngles = rotation;
        return parent;
    }

    private static GameObject CreatePrimitive(GameObject root, string name, PrimitiveType primitiveType, Vector3 position, Vector3 rotation, Vector3 scale, Material material)
    {
        GameObject obj = GameObject.CreatePrimitive(primitiveType);
        obj.name = name;
        obj.transform.SetParent(root.transform, false);
        obj.transform.localPosition = position;
        obj.transform.localEulerAngles = rotation;
        obj.transform.localScale = scale;
        SetMaterial(obj, material);
        return obj;
    }

    private static GameObject CreateCube(GameObject root, string name, Vector3 position, Vector3 rotation, Vector3 scale, Material material)
    {
        return CreatePrimitive(root, name, PrimitiveType.Cube, position, rotation, scale, material);
    }

    private static void SetMaterial(GameObject obj, Material material)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (material == null)
        {
            if (renderer != null)
                renderer.enabled = false;
            return;
        }

        if (renderer != null)
        {
            renderer.sharedMaterial = material;
            renderer.receiveShadows = true;
        }
    }

    private static Material CreateMaterial(string name, Color color, float smoothness, Color? emission = null)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = new Material(shader)
        {
            name = name,
            color = color
        };

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);

        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", smoothness);

        if (material.HasProperty("_Metallic"))
            material.SetFloat("_Metallic", 0f);

        if (emission.HasValue)
        {
            Color emissionColor = emission.Value;
            if (material.HasProperty("_EmissionColor"))
                material.SetColor("_EmissionColor", emissionColor);

            material.EnableKeyword("_EMISSION");
        }

        return material;
    }

    private static GameObject FindSceneObject(string name)
    {
        GameObject activeObject = GameObject.Find(name);
        if (activeObject != null)
            return activeObject;

        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform sceneTransform in transforms)
        {
            if (sceneTransform.name == name && sceneTransform.gameObject.scene.IsValid())
                return sceneTransform.gameObject;
        }

        return null;
    }

    private static void DeleteOldGeneratedObjects()
    {
        string[] legacyNames =
        {
            RootName,
            "TimeRift",
            "OldTimeRift",
            "TimeRift_InteractZone",
            "Trigger_TimeRiftInteract",
            "S02_TimeRiftTrigger",
            "TimeRiftTrigger",
            "RiftStabilityTrigger",
            "S02_EventController",
            "S02CaveEventController",
            "S02_TimeRift_Core",
            "S02_TimeRift_Ring",
            "S02_Minion",
            "S02_CutsceneFade",
            "S02_CutsceneSkipPrompt",
            "S02_CutsceneSubtitle",
            "MinionSpawner",
            "MinionSpawnPoint_01",
            "MinionSpawnPoint_02",
            "MinionSpawnPoint_03",
            "CaveMinionSpawn_01",
            "CaveMinionSpawn_02",
            "CaveMinionSpawn_03",
            "ExitTrigger_Test",
            "CollapseHole",
            "Collapse_Hole",
            "HacTinh_Descent_Hole",
            "Cave_Floor",
            "Cave_Wall_Left",
            "Cave_Wall_Right",
            "Cave_Back_Wall",
            "S02_Cave_Floor",
            "Safety_Floor_S02",
            "Inspect_AncientSymbol_01",
            "Inspect_BronzeFragment_01",
            "Inspect_CoLoaRingMark_01",
            "Inspect_DongSonSymbol",
            "Inspect_CoLoaStoneMark",
            "VoiceTrigger_01",
            "VoiceTrigger_02",
            "UnstableGround_Zone_01",
            "UnstableGround_Zone_02",
            "HacTinhDescendTrigger",
            "TimeRift_ResonanceZone"
        };

        foreach (string legacyName in legacyNames)
            DeleteAllSceneObjectsNamed(legacyName);

        DeleteAllSceneObjectsWithPrefix("S02_Minion");
    }

    private static void DeleteAllSceneObjectsNamed(string objectName)
    {
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform sceneTransform in transforms)
        {
            if (sceneTransform == null ||
                sceneTransform.name != objectName ||
                !sceneTransform.gameObject.scene.IsValid())
            {
                continue;
            }

            Object.DestroyImmediate(sceneTransform.gameObject);
        }
    }

    private static void DeleteAllSceneObjectsWithPrefix(string namePrefix)
    {
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform sceneTransform in transforms)
        {
            if (sceneTransform == null ||
                !sceneTransform.name.StartsWith(namePrefix) ||
                !sceneTransform.gameObject.scene.IsValid())
            {
                continue;
            }

            Object.DestroyImmediate(sceneTransform.gameObject);
        }
    }

    private static void DeleteDuplicateSceneObjects(string objectName, GameObject keepObject)
    {
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform sceneTransform in transforms)
        {
            if (sceneTransform == null ||
                sceneTransform.gameObject == keepObject ||
                sceneTransform.name != objectName ||
                !sceneTransform.gameObject.scene.IsValid())
            {
                continue;
            }

            Object.DestroyImmediate(sceneTransform.gameObject);
        }
    }
}

