using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

public static class S03SinglePlayerArenaBuilder
{
    private const string RootName = "S03_SinglePlayerArena_Generated";
    private const string ScenePath = "Assets/Scenes/S03.unity";
    private const string BlessingFolder = "Assets/Blessings/S03";
    private const string BlessingBackgroundFolder = BlessingFolder + "/Backgrounds";
    private const string AnDuongVuongBackdropPath = BlessingBackgroundFolder + "/An_Duong_Vuong.png";
    private const string TrungTracBackdropPath = BlessingBackgroundFolder + "/Trung_Trac.png";
    private const string TrungNhiBackdropPath = BlessingBackgroundFolder + "/Trung_Nhi.png";
    private const string QuangTrungBackdropPath = BlessingBackgroundFolder + "/Quang_Trung.png";
    private const string CoLoaMapAssetPath = "Assets/Models/CoLoa/coloa_map_stage03_unity_colored.glb";
    private const string CoLoaMapObjectName = "coloa_map_stage03_unity_colored";
    private const string MinionPrefabPath = "Assets/Prefabs/Minion.prefab";
    private const string PlayerDisplayName = "Van An";
    private const string PlayerModelPath = VanAnPlayerSetupBuilder.BaseModelPath;
    private const string PlayerAnimatorControllerPath = VanAnPlayerSetupBuilder.ControllerPath;
    private const string PlayerMaterialPath = VanAnPlayerSetupBuilder.MaterialPath;
    private const string SwordVisualName = "VanAn_SwordVisual";
    private const float PhongHTArenaRadius = 72f;
    private const float PhongHTGroundY = 12.56f;
    private static readonly Vector3 PhongHTIntegrationRoot = Vector3.zero;
    private static readonly Vector3 PhongHTCombatCenter = new Vector3(35.33f, PhongHTGroundY, 106.12f);
    private static readonly Vector3 PhongHTCombatFloorScale = new Vector3(140f, 0.24f, 190f);
    private static readonly Vector3 PhongHTPlayerSpawn = new Vector3(35.33f, PhongHTGroundY + 0.08f, 106.955f);
    private static readonly Vector3[] PhongHTEnemySpawns =
    {
        new Vector3(35.55f, 12.56f, 106.12f),
        new Vector3(35.33f, 12.56f, 106.12f),
        new Vector3(35.55f, 12.56f, 106.12f),
        new Vector3(35.55f, 12.56f, 106.12f),
        new Vector3(35.55f, 12.56f, 106.12f),
        new Vector3(35.55f, 12.56f, 106.12f),
        new Vector3(35.55f, 12.56f, 106.59f),
        new Vector3(35.55f, 12.56f, 106.59f),
    };

    [MenuItem("Tools/Dong Chay Anh Hung/Rebuild S03 Combat Arena")]
    public static void BuildScene()
    {
        OpenS03Scene();
        DeleteOldGeneratedObjects();
        List<BlessingDefinition> blessings = CreateBlessingAssets();
        ConfigureHeroBackdropImportSettings();
        VanAnPlayerSetupBuilder.RebuildVanAnAnimatorController();
        Sprite anDuongVuongBackdrop = LoadHeroBackdrop(AnDuongVuongBackdropPath);
        Sprite trungTracBackdrop = LoadHeroBackdrop(TrungTracBackdropPath);
        Sprite trungNhiBackdrop = LoadHeroBackdrop(TrungNhiBackdropPath);
        Sprite quangTrungBackdrop = LoadHeroBackdrop(QuangTrungBackdropPath);
        EnsureCoLoaMap();

        GameObject root = new GameObject(RootName);
        root.transform.position = PhongHTIntegrationRoot;
        root.transform.rotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;
        CombatLayout layout = BuildArena(root);
        SetupLighting();

        Camera mainCamera = SetupCamera(layout);
        Transform player = SetupPlayer(mainCamera, layout.PlayerSpawn);
        Canvas canvas = EnsureCanvas();
        BuildHud(
            canvas,
            out TMP_Text waveText,
            out TMP_Text statusText,
            out GameObject choiceRoot,
            out BlessingChoiceUI[] choiceCards,
            out TMP_Text choiceTitle,
            out TMP_Text choiceSubtitle,
            out TMP_Text choiceResult,
            out Image blessingBackdrop,
            out TMP_Text blessingHeroName,
            out TMP_Text blessingHeroLore,
            out Button rerollButton,
            out TMP_Text rerollButtonText,
            out Button skipButton,
            out TMP_Text skipButtonText);
        CreatePlayerHealthBar(canvas.transform, player.GetComponent<PlayerHealth3D>());

        BlessingRuntimeController runtime = player.GetComponent<BlessingRuntimeController>();
        if (runtime == null)
            runtime = player.gameObject.AddComponent<BlessingRuntimeController>();

        PlayerController3D controller = player.GetComponent<PlayerController3D>();
        PlayerCombat3D combat = player.GetComponent<PlayerCombat3D>();
        PlayerHealth3D health = player.GetComponent<PlayerHealth3D>();
        runtime.Configure(controller, combat, health);

        GameObject managerObject = new GameObject("S03_BlessingManager");
        managerObject.transform.SetParent(root.transform, false);
        BlessingManager blessingManager = managerObject.AddComponent<BlessingManager>();
        blessingManager.Configure(
            blessings,
            runtime,
            choiceRoot,
            choiceCards,
            choiceTitle,
            choiceSubtitle,
            choiceResult,
            blessingBackdrop,
            blessingHeroName,
            blessingHeroLore,
            rerollButton,
            rerollButtonText,
            skipButton,
            skipButtonText,
            anDuongVuongBackdrop,
            trungTracBackdrop,
            trungNhiBackdrop,
            quangTrungBackdrop);

        GameObject directorObject = new GameObject("S03_ArenaDirector");
        directorObject.transform.SetParent(root.transform, false);
        S03ArenaDirector director = directorObject.AddComponent<S03ArenaDirector>();
        director.Configure(
            player,
            AssetDatabase.LoadAssetAtPath<GameObject>(MinionPrefabPath),
            layout.SpawnPoints,
            blessingManager,
            runtime,
            waveText,
            statusText);
        director.ConfigureWaveTuning(3, 1, 12, 0, layout.Radius, 1.2f, 1.15f, 8f, 12, 0f);

        Selection.activeGameObject = root;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
        Debug.Log("S03 Combat Arena rebuilt with single-player wave combat and Blessing choices.");
    }

    public static void VerifyScene()
    {
        OpenS03Scene();
        ConfigureHeroBackdropImportSettings();

        GameObject generatedRoot = FindSceneObject(RootName);
        if (generatedRoot == null)
            throw new UnityException("S03 verify failed: missing scene object " + RootName + ".");

        if (generatedRoot.transform.position.sqrMagnitude > 0.0001f)
            throw new UnityException("S03 verify failed: generated root must stay at world origin.");

        RequireSceneObject("S03_BlessingManager");
        RequireSceneObject("S03_ArenaDirector");
        RequireSceneObject("S03_BlessingChoiceRoot");
        RequireSceneObject("S03_BlessingChoiceSubtitle");
        RequireSceneObject("S03_BlessingHeroName");
        RequireSceneObject("S03_BlessingHeroLore");
        RequireSceneObject("S03_BlessingRerollButton");
        RequireSceneObject("S03_BlessingSkipButton");
        RequireSceneObject(CoLoaMapObjectName);
        RequireAsset<GameObject>(CoLoaMapAssetPath);
        RequireAsset<Sprite>(AnDuongVuongBackdropPath);
        RequireAsset<Sprite>(TrungTracBackdropPath);
        RequireAsset<Sprite>(TrungNhiBackdropPath);
        RequireAsset<Sprite>(QuangTrungBackdropPath);
        RequireAsset<GameObject>(PlayerModelPath);
        RequireAsset<RuntimeAnimatorController>(PlayerAnimatorControllerPath);
        RequireAsset<Material>(PlayerMaterialPath);
        RequireAsset<Material>(NoKimQuyWeaponAssetBuilder.SwordBladeMaterialPath);
        RequireAsset<Material>(NoKimQuyWeaponAssetBuilder.SwordHiltMaterialPath);

        GameObject player = GameObject.Find("Player");
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
            throw new UnityException("S03 verify failed: Player was not found.");

        RequireComponent<PlayerController3D>(player);
        RequireComponent<PlayerCombat3D>(player);
        RequireComponent<PlayerHealth3D>(player);
        RequireComponent<PlayerFallGuard3D>(player);
        RequireComponent<BlessingRuntimeController>(player);
        RequireComponent<PlayerWeaponSlot3D>(player);
        if (player.transform.Find("PlayerVisual") == null)
            throw new UnityException("S03 verify failed: PlayerVisual was not found on Player.");
        RequirePlayerGrounding(player);
        RequireChild(player.transform, "RightHandWeaponSocket");
        RequireVanAnVisual(player);
        Transform swordVisual = RequireChild(player.transform, SwordVisualName);
        RequireComponent<WeaponVisualAnchor3D>(swordVisual.gameObject);
        RequireNoPhysicsComponents(swordVisual, SwordVisualName);
        RequireSceneObject("S03_PlayerHealthRoot");

        if (Object.FindAnyObjectByType<S03ArenaDirector>() == null)
            throw new UnityException("S03 verify failed: S03ArenaDirector component was not found.");
        if (Object.FindAnyObjectByType<BlessingManager>() == null)
            throw new UnityException("S03 verify failed: BlessingManager component was not found.");
        if (Object.FindObjectsByType<BlessingChoiceUI>(FindObjectsInactive.Include).Length < 3)
            throw new UnityException("S03 verify failed: not enough BlessingChoiceUI cards.");

        string[] blessingGuids = AssetDatabase.FindAssets("t:BlessingDefinition", new[] { BlessingFolder });
        if (blessingGuids.Length < 20)
            throw new UnityException("S03 verify failed: expected 20 BlessingDefinition assets, found " + blessingGuids.Length + ".");

        Debug.Log("S03 verification passed: scene, player, weapon loadout, arena director, UI, and 20 Blessing assets are present.");
    }

    private static void OpenS03Scene()
    {
        if (EditorSceneManager.GetActiveScene().path == ScenePath)
            return;

        EditorSceneManager.OpenScene(ScenePath);
    }

    private static GameObject EnsureCoLoaMap()
    {
        GameObject map = FindSceneObject(CoLoaMapObjectName);
        if (map != null)
            return map;

        GameObject mapAsset = AssetDatabase.LoadAssetAtPath<GameObject>(CoLoaMapAssetPath);
        if (mapAsset == null)
            throw new UnityException("S03 builder could not find Co Loa map asset: " + CoLoaMapAssetPath);

        map = PrefabUtility.InstantiatePrefab(mapAsset) as GameObject;
        if (map == null)
            map = Object.Instantiate(mapAsset);

        map.name = CoLoaMapObjectName;
        map.transform.position = Vector3.zero;
        map.transform.rotation = Quaternion.identity;
        map.transform.localScale = Vector3.one;
        return map;
    }

    private static CombatLayout BuildArena(GameObject root)
    {
        float radius = PhongHTArenaRadius;
        Vector3 center = PhongHTCombatCenter;

        // Hidden gameplay support only. The visible graybox arena was removed so the Co Loa map owns the scene.
        CreateInvisibleColliderCube(root, "S03_CoLoa_CombatFloor", center + new Vector3(0f, -0.12f, 0f), PhongHTCombatFloorScale);

        Transform[] spawnPoints = new Transform[8];
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            Vector3 position = PhongHTEnemySpawns[i];
            GameObject point = new GameObject("S03_EnemySpawn_" + (i + 1).ToString("00"));
            point.transform.SetParent(root.transform, false);
            point.transform.localPosition = position;
            spawnPoints[i] = point.transform;
        }

        return new CombatLayout
        {
            Center = PhongHTPlayerSpawn,
            Radius = radius,
            PlayerSpawn = PhongHTPlayerSpawn,
            SpawnPoints = spawnPoints
        };
    }

    private static void CreateInvisibleColliderCube(GameObject root, string name, Vector3 position, Vector3 scale)
    {
        GameObject obj = CreateCube(root, name, position, Vector3.zero, scale, null);
        MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
        if (renderer != null)
            renderer.enabled = false;
    }

    private static Transform SetupPlayer(Camera mainCamera, Vector3 spawnPosition)
    {
        GameObject player = GameObject.Find("Player");
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
        }

        TrySetTag(player, "Player");

        CharacterController characterController = player.GetComponent<CharacterController>();
        if (characterController == null)
            characterController = player.AddComponent<CharacterController>();

        bool wasEnabled = characterController.enabled;
        characterController.enabled = false;
        player.transform.position = spawnPosition;
        player.transform.rotation = Quaternion.identity;
        characterController.height = 1.8f;
        characterController.radius = 0.32f;
        characterController.center = new Vector3(0f, characterController.height * 0.5f, 0f);
        characterController.enabled = wasEnabled;

        PlayerController3D controller = player.GetComponent<PlayerController3D>();
        if (controller == null)
            controller = player.AddComponent<PlayerController3D>();

        controller.moveSpeed = 8.2f;
        controller.dashSpeed = 17f;
        controller.dashDuration = 0.18f;
        controller.dashCooldown = 0.4f;
        controller.dashTowardsMouse = true;
        controller.cameraTransform = mainCamera != null ? mainCamera.transform : null;

        PlayerHealth3D health = player.GetComponent<PlayerHealth3D>();
        if (health == null)
            health = player.AddComponent<PlayerHealth3D>();

        health.maxHP = 100;
        health.currentHP = health.maxHP;
        health.isDead = false;

        PlayerFallGuard3D fallGuard = player.GetComponent<PlayerFallGuard3D>();
        if (fallGuard == null)
            fallGuard = player.AddComponent<PlayerFallGuard3D>();

        fallGuard.Configure(spawnPosition, PhongHTGroundY - 4f);

        PlayerCombat3D combat = player.GetComponent<PlayerCombat3D>();
        if (combat == null)
            combat = player.AddComponent<PlayerCombat3D>();

        combat.enabled = true;
        combat.damage = 28;
        combat.attackRange = 4.8f;
        combat.attackAngle = 105f;
        combat.closeHitRadius = 1.45f;
        combat.attackCooldown = 0.55f;
        combat.knockbackForce = 6.8f;
        combat.enemyStunDuration = 0.36f;
        combat.heavyDamage = 70;
        combat.heavyAttackRange = 6.2f;
        combat.heavyAttackAngle = 128f;
        combat.heavyCloseHitRadius = 1.8f;
        combat.heavyAttackCooldown = 1.05f;
        combat.heavyWindup = 0.18f;
        combat.heavyKnockbackForce = 11.5f;
        combat.heavyEnemyStunDuration = 0.7f;
        combat.aimCamera = mainCamera;

        BlessingRuntimeController runtime = player.GetComponent<BlessingRuntimeController>();
        if (runtime == null)
            runtime = player.AddComponent<BlessingRuntimeController>();

        runtime.Configure(controller, combat, health);
        SetupPlayerVisual(player);
        VanAnWeaponLoadoutBuilder.SetupSwordOnPlayer(player);

        if (mainCamera != null)
        {
            ThirdPersonCamera followCamera = mainCamera.GetComponent<ThirdPersonCamera>();
            if (followCamera == null)
                followCamera = mainCamera.gameObject.AddComponent<ThirdPersonCamera>();

            followCamera.target = player.transform;
            followCamera.distance = 5.8f;
            followCamera.height = 2.7f;
            followCamera.fixedAngle = true;
            followCamera.fixedYaw = 45f;
            followCamera.fixedPitch = 36f;
            followCamera.lockCursor = false;
        }

        return player.transform;
    }

    private static void SetupPlayerVisual(GameObject player)
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerModelPath);
        if (model == null)
        {
            Debug.LogWarning("S03 builder could not find player model at " + PlayerModelPath);
            return;
        }

        Transform oldVisual = player.transform.Find("PlayerVisual");
        if (oldVisual != null)
            Object.DestroyImmediate(oldVisual.gameObject);

        GameObject visual = PrefabUtility.InstantiatePrefab(model, player.transform) as GameObject;
        if (visual == null)
            visual = Object.Instantiate(model, player.transform);

        visual.name = "PlayerVisual";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;

        NormalizeVisualScale(visual.transform, 1.75f);
        AlignVisualToControllerFeet(player, visual.transform);
        ApplyPlayerMaterial(visual);
        ConfigureVisualAnimator(visual);
        RemovePrimitivePlayerBody(player);

        PlayerAnimatorDriver driver = player.GetComponent<PlayerAnimatorDriver>();
        if (driver == null)
            driver = player.AddComponent<PlayerAnimatorDriver>();

        EditorUtility.SetDirty(driver);
        EditorUtility.SetDirty(visual);
        EditorUtility.SetDirty(player);
    }

    private static void ConfigureVisualAnimator(GameObject visual)
    {
        Animator animator = visual.GetComponent<Animator>();
        if (animator == null)
            animator = visual.AddComponent<Animator>();

        RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(PlayerAnimatorControllerPath);
        if (controller != null)
            animator.runtimeAnimatorController = controller;

        Avatar avatar = FindPlayerAvatar();
        if (avatar != null)
            animator.avatar = avatar;

        animator.applyRootMotion = false;
        animator.updateMode = AnimatorUpdateMode.Normal;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        EditorUtility.SetDirty(animator);
    }

    private static Avatar FindPlayerAvatar()
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(PlayerModelPath);
        foreach (Object asset in assets)
        {
            Avatar avatar = asset as Avatar;
            if (avatar != null)
                return avatar;
        }

        return null;
    }

    private static void ApplyPlayerMaterial(GameObject visual)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(PlayerMaterialPath);
        if (material == null)
            return;

        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            int materialCount = Mathf.Max(1, renderer.sharedMaterials.Length);
            Material[] materials = new Material[materialCount];
            for (int i = 0; i < materials.Length; i++)
                materials[i] = material;

            renderer.sharedMaterials = materials;
            EditorUtility.SetDirty(renderer);
        }
    }

    private static void AlignVisualToControllerFeet(GameObject player, Transform visual)
    {
        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller == null || visual == null)
            return;

        Vector3 localPosition = visual.localPosition;
        localPosition.y = controller.center.y - controller.height * 0.5f;
        visual.localPosition = localPosition;
    }

    private static void NormalizeVisualScale(Transform visual, float targetHeight)
    {
        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        if (bounds.size.y <= 0.01f)
            return;

        visual.localScale *= targetHeight / bounds.size.y;
    }

    private static void RemovePrimitivePlayerBody(GameObject player)
    {
        MeshRenderer meshRenderer = player.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
            Object.DestroyImmediate(meshRenderer);

        MeshFilter meshFilter = player.GetComponent<MeshFilter>();
        if (meshFilter != null)
            Object.DestroyImmediate(meshFilter);

        CapsuleCollider capsuleCollider = player.GetComponent<CapsuleCollider>();
        if (capsuleCollider != null)
            Object.DestroyImmediate(capsuleCollider);
    }

    private static Camera SetupCamera(CombatLayout layout)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            GameObject cameraObject = GameObject.Find("Main Camera");
            if (cameraObject == null)
                cameraObject = new GameObject("Main Camera");

            mainCamera = cameraObject.GetComponent<Camera>();
            if (mainCamera == null)
                mainCamera = cameraObject.AddComponent<Camera>();

            TrySetTag(cameraObject, "MainCamera");
        }

        float distance = Mathf.Clamp(layout.Radius * 0.58f, 12f, 28f);
        mainCamera.transform.position = layout.Center + new Vector3(-distance, distance * 0.72f, -distance);
        mainCamera.transform.rotation = Quaternion.Euler(46f, 45f, 0f);
        mainCamera.fieldOfView = 48f;
        mainCamera.orthographic = false;
        mainCamera.nearClipPlane = 0.1f;
        mainCamera.farClipPlane = Mathf.Max(180f, layout.Radius * 8f);
        return mainCamera;
    }

    private static void SetupLighting()
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.22f, 0.24f, 0.28f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.16f, 0.17f, 0.2f);
        RenderSettings.fogDensity = 0.009f;

        Light directional = Object.FindAnyObjectByType<Light>();
        if (directional == null || directional.type != LightType.Directional)
        {
            GameObject lightObject = new GameObject("Directional Light");
            directional = lightObject.AddComponent<Light>();
            directional.type = LightType.Directional;
        }

        directional.intensity = 1.05f;
        directional.color = new Color(0.92f, 0.94f, 1f);
        directional.transform.rotation = Quaternion.Euler(54f, -38f, 0f);
    }

    private static void BuildHud(
        Canvas canvas,
        out TMP_Text waveText,
        out TMP_Text statusText,
        out GameObject choiceRoot,
        out BlessingChoiceUI[] choiceCards,
        out TMP_Text choiceTitle,
        out TMP_Text choiceSubtitle,
        out TMP_Text choiceResult,
        out Image blessingBackdrop,
        out TMP_Text blessingHeroName,
        out TMP_Text blessingHeroLore,
        out Button rerollButton,
        out TMP_Text rerollButtonText,
        out Button skipButton,
        out TMP_Text skipButtonText)
    {
        waveText = CreateText(canvas.transform, "S03_WaveText", new Vector2(0.5f, 1f), new Vector2(0f, -46f), new Vector2(520f, 52f), 34, TextAlignmentOptions.Center);
        waveText.text = "S03 ARENA";

        statusText = CreateText(canvas.transform, "S03_StatusText", new Vector2(0.5f, 1f), new Vector2(0f, -92f), new Vector2(980f, 54f), 24, TextAlignmentOptions.Center);
        statusText.text = "WASD di chuyen | Mouse0 danh thuong | Mouse1 heavy | Shift Dash";

        TMP_Text hintText = CreateText(canvas.transform, "S03_ControlHintText", new Vector2(0.5f, 0f), new Vector2(0f, 42f), new Vector2(1200f, 48f), 22, TextAlignmentOptions.Center);
        hintText.text = "Sau moi wave, chon 1 trong 3 Chuc Phuc Anh Linh de tao build rieng.";

        choiceRoot = new GameObject("S03_BlessingChoiceRoot");
        choiceRoot.transform.SetParent(canvas.transform, false);
        RectTransform rootRect = choiceRoot.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        blessingBackdrop = choiceRoot.AddComponent<Image>();
        blessingBackdrop.color = new Color(0.04f, 0.26f, 0.30f, 0.92f);

        Image darkVeil = CreatePanelImage(choiceRoot.transform, "S03_BlessingDarkVeil", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.015f, 0.012f, 0.014f, 0.64f));
        RectTransform darkVeilRect = darkVeil.GetComponent<RectTransform>();
        darkVeilRect.anchorMin = Vector2.zero;
        darkVeilRect.anchorMax = Vector2.one;
        darkVeilRect.offsetMin = Vector2.zero;
        darkVeilRect.offsetMax = Vector2.zero;

        Image warmHorizon = CreatePanelImage(choiceRoot.transform, "S03_BlessingWarmHorizon", new Vector2(0.5f, 0.53f), Vector2.zero, new Vector2(1900f, 620f), new Color(0.75f, 0.34f, 0.06f, 0.19f));
        warmHorizon.raycastTarget = false;

        Image heroInfoPanel = CreatePanelImage(choiceRoot.transform, "S03_BlessingHeroInfoPanel", new Vector2(0f, 1f), new Vector2(300f, -185f), new Vector2(560f, 142f), new Color(0.02f, 0.014f, 0.012f, 0.58f));
        heroInfoPanel.raycastTarget = false;
        Outline heroInfoOutline = heroInfoPanel.gameObject.AddComponent<Outline>();
        heroInfoOutline.effectColor = new Color(0.95f, 0.55f, 0.16f, 0.34f);
        heroInfoOutline.effectDistance = new Vector2(2f, -2f);

        blessingHeroName = CreateText(choiceRoot.transform, "S03_BlessingHeroName", new Vector2(0f, 1f), new Vector2(300f, -142f), new Vector2(500f, 42f), 26, TextAlignmentOptions.Left);
        blessingHeroName.text = "AN DUONG VUONG";
        blessingHeroName.color = new Color(1f, 0.76f, 0.28f, 1f);

        blessingHeroLore = CreateText(choiceRoot.transform, "S03_BlessingHeroLore", new Vector2(0f, 1f), new Vector2(300f, -202f), new Vector2(500f, 78f), 20, TextAlignmentOptions.Left);
        blessingHeroLore.text = "Nen thanh Co Loa, rong vang, troi chieu u nghi.";
        blessingHeroLore.color = new Color(0.96f, 0.88f, 0.68f, 0.96f);

        TMP_Text resourceText = CreateText(choiceRoot.transform, "S03_BlessingResourceText", new Vector2(0.91f, 0.94f), Vector2.zero, new Vector2(330f, 42f), 24, TextAlignmentOptions.Right);
        resourceText.text = "<color=#B77CFF>◆ 120</color>    <color=#E2A83D>◎ 2.500</color>";

        resourceText.text = "<color=#B77CFF>TINH THACH 120</color>    <color=#E2A83D>VANG 2.500</color>";

        choiceTitle = CreateText(choiceRoot.transform, "S03_BlessingChoiceTitle", new Vector2(0.5f, 0.88f), Vector2.zero, new Vector2(900f, 78f), 55, TextAlignmentOptions.Center);
        choiceTitle.text = "CHỌN BLESSING";
        choiceTitle.text = "CH\u1eccN BLESSING";
        choiceTitle.color = new Color(0.98f, 0.86f, 0.62f, 1f);
        choiceTitle.fontStyle = FontStyles.Bold;

        choiceSubtitle = CreateText(choiceRoot.transform, "S03_BlessingChoiceSubtitle", new Vector2(0.5f, 0.815f), Vector2.zero, new Vector2(900f, 44f), 25, TextAlignmentOptions.Center);
        choiceSubtitle.text = "Chọn một sức mạnh để tiếp tục hành trình";
        choiceSubtitle.text = "Ch\u1ecdn m\u1ed9t s\u1ee9c m\u1ea1nh \u0111\u1ec3 ti\u1ebfp t\u1ee5c h\u00e0nh tr\u00ecnh";
        choiceSubtitle.color = new Color(0.92f, 0.84f, 0.7f, 0.95f);

        choiceResult = CreateText(choiceRoot.transform, "S03_BlessingChoiceResult", new Vector2(0.5f, 0.17f), Vector2.zero, new Vector2(1180f, 92f), 23, TextAlignmentOptions.Center);
        choiceResult.text = string.Empty;
        choiceResult.color = new Color(0.96f, 0.9f, 0.78f, 0.95f);

        choiceCards = new BlessingChoiceUI[3];
        float[] xOffsets = { -390f, 0f, 390f };
        for (int i = 0; i < choiceCards.Length; i++)
            choiceCards[i] = CreateBlessingCard(choiceRoot.transform, i, xOffsets[i]);

        rerollButton = CreateBlessingActionButton(choiceRoot.transform, "S03_BlessingRerollButton", "LÀM MỚI (1)", new Vector2(0.5f, 0.085f), new Vector2(-185f, 0f), out rerollButtonText);
        skipButton = CreateBlessingActionButton(choiceRoot.transform, "S03_BlessingSkipButton", "BỎ QUA", new Vector2(0.5f, 0.085f), new Vector2(185f, 0f), out skipButtonText);

        rerollButtonText.text = "L\u00c0M M\u1edaI (1)";
        skipButtonText.text = "B\u1ece QUA";

        TMP_Text hint = CreateText(choiceRoot.transform, "S03_BlessingHintText", new Vector2(0.5f, 0.035f), Vector2.zero, new Vector2(1200f, 36f), 19, TextAlignmentOptions.Center);
        hint.text = "Giữ Alt hoặc rê chuột qua card để xem chi tiết. Reroll chỉ dùng 1 lần mỗi lựa chọn.";
        hint.text = "Gi\u1eef Alt ho\u1eb7c r\u00ea chu\u1ed9t qua card \u0111\u1ec3 xem chi ti\u1ebft. Reroll ch\u1ec9 d\u00f9ng 1 l\u1ea7n m\u1ed7i l\u1ef1a ch\u1ecdn.";
        hint.color = new Color(0.86f, 0.76f, 0.56f, 0.88f);

        choiceRoot.SetActive(false);
    }

    private static void CreatePlayerHealthBar(Transform parent, PlayerHealth3D health)
    {
        GameObject root = new GameObject("S03_PlayerHealthRoot");
        root.transform.SetParent(parent, false);
        RectTransform rootRect = root.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0f, 1f);
        rootRect.anchorMax = new Vector2(0f, 1f);
        rootRect.pivot = new Vector2(0f, 1f);
        rootRect.anchoredPosition = new Vector2(34f, -34f);
        rootRect.sizeDelta = new Vector2(390f, 82f);

        Image panel = root.AddComponent<Image>();
        panel.color = new Color(0.035f, 0.025f, 0.028f, 0.82f);

        TMP_Text nameText = CreateText(root.transform, "S03_PlayerHealthName", new Vector2(0f, 1f), new Vector2(92f, -21f), new Vector2(180f, 26f), 22, TextAlignmentOptions.Left);
        nameText.text = PlayerDisplayName;

        TMP_Text valueText = CreateText(root.transform, "S03_PlayerHealthValue", new Vector2(1f, 1f), new Vector2(-78f, -21f), new Vector2(140f, 26f), 20, TextAlignmentOptions.Right);
        valueText.text = health != null ? health.currentHP + " / " + health.maxHP : "100 / 100";

        GameObject sliderObject = new GameObject("S03_PlayerHealthSlider");
        sliderObject.transform.SetParent(root.transform, false);
        RectTransform sliderRect = sliderObject.AddComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0f, 0f);
        sliderRect.anchorMax = new Vector2(1f, 0f);
        sliderRect.pivot = new Vector2(0.5f, 0f);
        sliderRect.anchoredPosition = new Vector2(0f, 17f);
        sliderRect.sizeDelta = new Vector2(-30f, 26f);

        Slider slider = sliderObject.AddComponent<Slider>();
        slider.transition = Selectable.Transition.None;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = health != null ? health.maxHP : 100f;
        slider.value = health != null ? health.currentHP : 100f;

        Image background = CreatePanelImage(sliderObject.transform, "Background", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.18f, 0.025f, 0.035f, 1f));
        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObject.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(4f, 4f);
        fillAreaRect.offsetMax = new Vector2(-4f, -4f);

        Image fill = CreatePanelImage(fillArea.transform, "Fill", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.86f, 0.1f, 0.13f, 1f));
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        slider.fillRect = fillRect;
        slider.targetGraphic = fill;

        PlayerHealthUI healthUI = root.AddComponent<PlayerHealthUI>();
        healthUI.playerHealth = health;
        healthUI.healthSlider = slider;
        healthUI.healthText = valueText;
        healthUI.fillImage = fill;
    }

    private static BlessingChoiceUI CreateBlessingCard(Transform parent, int index, float xOffset)
    {
        GameObject card = new GameObject("S03_BlessingCard_" + (index + 1));
        card.transform.SetParent(parent, false);
        RectTransform rect = card.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(xOffset, -4f);
        rect.sizeDelta = new Vector2(315f, 455f);

        Image frame = card.AddComponent<Image>();
        frame.color = new Color(0.45f, 0.31f, 0.11f, 0.95f);
        Outline outline = card.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 0.68f, 0.22f, 0.55f);
        outline.effectDistance = new Vector2(2f, -2f);

        Button button = card.AddComponent<Button>();
        button.targetGraphic = frame;
        button.transition = Selectable.Transition.None;

        Image glow = CreatePanelImage(card.transform, "Glow", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(333f, 473f), new Color(1f, 0.62f, 0.08f, 0.08f));
        Image body = CreatePanelImage(card.transform, "Body", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(288f, 426f), new Color(0.025f, 0.028f, 0.034f, 0.96f));
        Image topRule = CreatePanelImage(card.transform, "TopRule", new Vector2(0.5f, 0.91f), Vector2.zero, new Vector2(245f, 3f), new Color(0.86f, 0.56f, 0.17f, 0.82f));
        Image bottomRule = CreatePanelImage(card.transform, "BottomRule", new Vector2(0.5f, 0.17f), Vector2.zero, new Vector2(245f, 3f), new Color(0.86f, 0.56f, 0.17f, 0.82f));

        Image rarityGem = CreatePanelImage(card.transform, "RarityGem", new Vector2(0.5f, 0.965f), Vector2.zero, new Vector2(18f, 18f), new Color(0.18f, 0.58f, 1f, 1f));
        rarityGem.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);

        TMP_Text name = CreateText(card.transform, "Name", new Vector2(0.5f, 0.855f), Vector2.zero, new Vector2(270f, 56f), 25, TextAlignmentOptions.Center);
        name.fontStyle = FontStyles.Bold;
        name.color = new Color(0.96f, 0.84f, 0.62f, 1f);

        TMP_Text rarity = CreateText(card.transform, "Rarity", new Vector2(0.5f, 0.79f), Vector2.zero, new Vector2(240f, 30f), 18, TextAlignmentOptions.Center);
        rarity.fontStyle = FontStyles.Bold;

        Image iconBack = CreatePanelImage(card.transform, "IconBack", new Vector2(0.5f, 0.61f), Vector2.zero, new Vector2(150f, 150f), new Color(0.05f, 0.035f, 0.025f, 0.92f));
        Image icon = CreatePanelImage(card.transform, "Icon", new Vector2(0.5f, 0.61f), Vector2.zero, new Vector2(106f, 106f), new Color(1f, 1f, 1f, 0.22f));

        TMP_Text description = CreateText(card.transform, "Description", new Vector2(0.5f, 0.37f), Vector2.zero, new Vector2(255f, 118f), 20, TextAlignmentOptions.Center);
        description.color = new Color(0.92f, 0.84f, 0.68f, 0.96f);

        TMP_Text stack = CreateText(card.transform, "Stack", new Vector2(0.5f, 0.225f), Vector2.zero, new Vector2(250f, 30f), 17, TextAlignmentOptions.Center);
        stack.color = new Color(0.74f, 0.92f, 1f, 0.95f);

        TMP_Text hero = CreateText(card.transform, "Hero", new Vector2(0.5f, 0.08f), Vector2.zero, new Vector2(260f, 34f), 18, TextAlignmentOptions.Center);
        hero.color = new Color(0.98f, 0.72f, 0.28f, 0.95f);
        hero.fontStyle = FontStyles.Bold;

        topRule.raycastTarget = false;
        bottomRule.raycastTarget = false;
        iconBack.raycastTarget = false;

        BlessingChoiceUI cardUI = card.AddComponent<BlessingChoiceUI>();
        cardUI.ConfigureReferences(button, frame, body, glow, icon, rarityGem, hero, name, description, rarity, stack);
        return cardUI;
    }

    private static Button CreateBlessingActionButton(Transform parent, string name, string label, Vector2 anchor, Vector2 position, out TMP_Text labelText)
    {
        GameObject buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(265f, 58f);

        Image frame = buttonObject.AddComponent<Image>();
        frame.color = new Color(0.22f, 0.105f, 0.045f, 0.92f);
        Outline outline = buttonObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.92f, 0.58f, 0.18f, 0.72f);
        outline.effectDistance = new Vector2(2f, -2f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = frame;

        labelText = CreateText(buttonObject.transform, name + "_Label", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240f, 36f), 22, TextAlignmentOptions.Center);
        labelText.text = label;
        labelText.fontStyle = FontStyles.Bold;
        labelText.color = new Color(0.96f, 0.8f, 0.48f, 1f);
        return button;
    }

    private static Image CreatePanelImage(Transform parent, string name, Vector2 anchor, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = obj.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private static TMP_Text CreateText(Transform parent, string name, Vector2 anchor, Vector2 anchoredPosition, Vector2 size, int fontSize, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        return text;
    }

    private static void ConfigureHeroBackdropImportSettings()
    {
        AssetDatabase.Refresh();
        ConfigureBackdropSpriteImport(AnDuongVuongBackdropPath);
        ConfigureBackdropSpriteImport(TrungTracBackdropPath);
        ConfigureBackdropSpriteImport(TrungNhiBackdropPath);
        ConfigureBackdropSpriteImport(QuangTrungBackdropPath);
    }

    private static void ConfigureBackdropSpriteImport(string assetPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning("S03 missing Blessing backdrop image: " + assetPath);
            return;
        }

        bool changed = false;
        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            changed = true;
        }

        if (importer.spriteImportMode != SpriteImportMode.Single)
        {
            importer.spriteImportMode = SpriteImportMode.Single;
            changed = true;
        }

        if (importer.mipmapEnabled)
        {
            importer.mipmapEnabled = false;
            changed = true;
        }

        if (importer.maxTextureSize < 2048)
        {
            importer.maxTextureSize = 2048;
            changed = true;
        }

        if (changed)
            importer.SaveAndReimport();
    }

    private static Sprite LoadHeroBackdrop(string assetPath)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (sprite == null)
            Debug.LogWarning("S03 could not load Blessing backdrop sprite: " + assetPath);
        return sprite;
    }

    private static void RequireSceneObject(string objectName)
    {
        if (FindSceneObject(objectName) == null)
            throw new UnityException("S03 verify failed: missing scene object " + objectName + ".");
    }

    private static void RequireAsset<T>(string assetPath) where T : Object
    {
        if (AssetDatabase.LoadAssetAtPath<T>(assetPath) == null)
            throw new UnityException("S03 verify failed: missing asset " + assetPath + ".");
    }

    private static GameObject FindSceneObject(string objectName)
    {
        GameObject activeObject = GameObject.Find(objectName);
        if (activeObject != null)
            return activeObject;

        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform sceneTransform in transforms)
        {
            if (sceneTransform != null &&
                sceneTransform.name == objectName &&
                sceneTransform.gameObject.scene.IsValid())
            {
                return sceneTransform.gameObject;
            }
        }

        return null;
    }

    private static void RequireComponent<T>(GameObject obj) where T : Component
    {
        if (obj.GetComponent<T>() == null)
            throw new UnityException("S03 verify failed: " + obj.name + " is missing " + typeof(T).Name + ".");
    }

    private static Transform RequireChild(Transform root, string childName)
    {
        Transform child = FindChildRecursive(root, childName);
        if (child == null)
            throw new UnityException("S03 verify failed: missing child object " + childName + ".");

        return child;
    }

    private static void RequireNoPhysicsComponents(Transform root, string label)
    {
        if (root.GetComponentsInChildren<Rigidbody>(true).Length > 0 ||
            root.GetComponentsInChildren<Rigidbody2D>(true).Length > 0)
        {
            throw new UnityException("S03 verify failed: " + label + " must not contain Rigidbody components.");
        }

        if (root.GetComponentsInChildren<Collider>(true).Length > 0 ||
            root.GetComponentsInChildren<Collider2D>(true).Length > 0)
        {
            throw new UnityException("S03 verify failed: " + label + " must not contain Collider components.");
        }
    }

    private static void RequirePlayerGrounding(GameObject player)
    {
        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller == null)
            throw new UnityException("S03 verify failed: Player is missing CharacterController.");

        float expectedCenterY = controller.height * 0.5f;
        if (Mathf.Abs(controller.center.y - expectedCenterY) > 0.03f)
            throw new UnityException("S03 verify failed: Player CharacterController center must keep Player transform at feet.");

        GameObject floor = FindSceneObject("S03_CoLoa_CombatFloor");
        if (floor == null)
            throw new UnityException("S03 verify failed: S03_CoLoa_CombatFloor was not found.");

        Collider floorCollider = floor.GetComponent<Collider>();
        if (floorCollider == null)
            throw new UnityException("S03 verify failed: S03_CoLoa_CombatFloor is missing a Collider.");

        Physics.SyncTransforms();
        Bounds bounds = CalculateColliderBounds(floorCollider);
        Vector3 position = player.transform.position;
        bool insideFloor =
            position.x >= bounds.min.x &&
            position.x <= bounds.max.x &&
            position.z >= bounds.min.z &&
            position.z <= bounds.max.z;

        if (!insideFloor)
            throw new UnityException(
                "S03 verify failed: Player spawn is outside the combat floor collider. " +
                "Player=" + position + ", FloorMin=" + bounds.min + ", FloorMax=" + bounds.max + ".");

        if (Mathf.Abs(bounds.max.y - PhongHTGroundY) > 0.05f)
            throw new UnityException("S03 verify failed: combat floor top is not aligned to the S03 ground height.");

        if (position.y < PhongHTGroundY || position.y > PhongHTGroundY + 0.35f)
            throw new UnityException("S03 verify failed: Player spawn Y is not on the combat floor.");
    }

    private static Bounds CalculateColliderBounds(Collider collider)
    {
        BoxCollider box = collider as BoxCollider;
        if (box == null)
            return collider.bounds;

        Transform transform = box.transform;
        Vector3 scale = transform.lossyScale;
        Vector3 size = new Vector3(
            Mathf.Abs(box.size.x * scale.x),
            Mathf.Abs(box.size.y * scale.y),
            Mathf.Abs(box.size.z * scale.z));

        return new Bounds(transform.TransformPoint(box.center), size);
    }

    private static void RequireVanAnVisual(GameObject player)
    {
        Transform visual = player.transform.Find("PlayerVisual");
        if (visual == null)
            throw new UnityException("S03 verify failed: PlayerVisual was not found on Player.");

        Animator animator = visual.GetComponent<Animator>();
        if (animator == null)
            throw new UnityException("S03 verify failed: Van An PlayerVisual is missing Animator.");

        RuntimeAnimatorController expectedController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(PlayerAnimatorControllerPath);
        if (expectedController != null && animator.runtimeAnimatorController != expectedController)
            throw new UnityException("S03 verify failed: PlayerVisual is not using Van An Animator Controller.");

        Avatar expectedAvatar = FindPlayerAvatar();
        if (expectedAvatar != null && animator.avatar != expectedAvatar)
            throw new UnityException("S03 verify failed: PlayerVisual is not using Van An Avatar.");
    }

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null || string.IsNullOrEmpty(childName))
            return null;

        foreach (Transform child in root)
        {
            if (child.name == childName)
                return child;

            Transform nested = FindChildRecursive(child, childName);
            if (nested != null)
                return nested;
        }

        return null;
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

    private static List<BlessingDefinition> CreateBlessingAssets()
    {
        EnsureFolder("Assets", "Blessings");
        EnsureFolder("Assets/Blessings", "S03");

        List<BlessingDefinition> blessings = new List<BlessingDefinition>
        {
            CreateOrUpdateBlessing("ADV_ThanhGiapAuLac", "adv_thanh_giap_au_lac", "Th\u00e0nh Gi\u00e1p \u00c2u L\u1ea1c", HeroType.AnDuongVuong, "Tang giap va giam sat thuong nhan vao.", BlessingRarity.Common, 3, BlessingEffectType.Armor, false),
            CreateOrUpdateBlessing("ADV_NoThan", "adv_no_than", "N\u1ecf Th\u1ea7n", HeroType.AnDuongVuong, "Moi don danh thu 5 ban them 3 mui ten nang luong.", BlessingRarity.Rare, 3, BlessingEffectType.DivineCrossbow, false),
            CreateOrUpdateBlessing("ADV_TuongThanh", "adv_tuong_thanh", "T\u01b0\u1eddng Th\u00e0nh", HeroType.AnDuongVuong, "Dash tao ket gioi ngan, lam cham va chan nhip tan cong cua ke dich.", BlessingRarity.Epic, 3, BlessingEffectType.DashBarrier, false),
            CreateOrUpdateBlessing("ADV_CanhGioi", "adv_canh_gioi", "C\u1ea3nh Gi\u1edbi", HeroType.AnDuongVuong, "Bao som wave tiep theo va tang tam phat hien cua dau truong.", BlessingRarity.Common, 3, BlessingEffectType.Awareness, false),
            CreateOrUpdateBlessing("ADV_ThanhCoLoa", "adv_thanh_co_loa", "Th\u00e0nh C\u1ed5 Loa", HeroType.AnDuongVuong, "Ultimate: dinh ky tao la chan dien rong bao ve ban than.", BlessingRarity.Legendary, 1, BlessingEffectType.CoLoaCitadel, true),

            CreateOrUpdateBlessing("TT_HieuTrieu", "tt_hieu_trieu", "Hi\u1ec7u Tri\u1ec7u", HeroType.TrungTrac, "Mau cang thap, sat thuong cang cao.", BlessingRarity.Rare, 3, BlessingEffectType.LowHealthDamage, false),
            CreateOrUpdateBlessing("TT_CoKhoiNghia", "tt_co_khoi_nghia", "C\u1edd Kh\u1edfi Ngh\u0129a", HeroType.TrungTrac, "Tang toc do danh.", BlessingRarity.Common, 3, BlessingEffectType.AttackSpeed, false),
            CreateOrUpdateBlessing("TT_KhoiNghiaMeLinh", "tt_khoi_nghia_me_linh", "Kh\u1edfi Ngh\u0129a M\u00ea Linh", HeroType.TrungTrac, "Ha du so quai se hoi nang luong ky nang va mot it mau.", BlessingRarity.Epic, 3, BlessingEffectType.KillSkillEnergy, false),
            CreateOrUpdateBlessing("TT_NuVuong", "tt_nu_vuong", "N\u1eef V\u01b0\u01a1ng", HeroType.TrungTrac, "Nhan them mot lan hoi sinh trong luot choi.", BlessingRarity.Legendary, 1, BlessingEffectType.Revive, false),
            CreateOrUpdateBlessing("TT_HaiBaKhoiNghia", "tt_hai_ba_khoi_nghia", "Hai B\u00e0 Kh\u1edfi Ngh\u0129a", HeroType.TrungTrac, "Ultimate: sat thuong tang theo so ke dich xung quanh.", BlessingRarity.Legendary, 1, BlessingEffectType.Uprising, true),

            CreateOrUpdateBlessing("TN_KyTuong", "tn_ky_tuong", "K\u1ef5 T\u01b0\u1ee3ng", HeroType.TrungNhi, "Tang toc do di chuyen.", BlessingRarity.Common, 3, BlessingEffectType.MoveSpeed, false),
            CreateOrUpdateBlessing("TN_XungPhong", "tn_xung_phong", "Xung Phong", HeroType.TrungNhi, "Dash gay sat thuong len ke dich tren duong luot.", BlessingRarity.Rare, 3, BlessingEffectType.DashDamage, false),
            CreateOrUpdateBlessing("TN_TruyKich", "tn_truy_kich", "Truy K\u00edch", HeroType.TrungNhi, "Don danh dau tien sau Dash gay them sat thuong.", BlessingRarity.Epic, 3, BlessingEffectType.PostDashDamage, false),
            CreateOrUpdateBlessing("TN_BongChienTruong", "tn_bong_chien_truong", "B\u00f3ng Chi\u1ebfn Tr\u01b0\u1eddng", HeroType.TrungNhi, "Dash tao phan than ngan han lam roi loan ke dich.", BlessingRarity.Epic, 3, BlessingEffectType.DashDecoy, false),
            CreateOrUpdateBlessing("TN_VoiChien", "tn_voi_chien", "Voi Chi\u1ebfn", HeroType.TrungNhi, "Ultimate: Dash xuyen qua ke dich va gay sat thuong lon.", BlessingRarity.Legendary, 1, BlessingEffectType.WarElephant, true),

            CreateOrUpdateBlessing("QT_HanhQuanThanToc", "qt_hanh_quan_than_toc", "H\u00e0nh Qu\u00e2n Th\u1ea7n T\u1ed1c", HeroType.QuangTrung, "Tang toc do danh.", BlessingRarity.Common, 3, BlessingEffectType.AttackSpeed, false),
            CreateOrUpdateBlessing("QT_DongDa", "qt_dong_da", "\u0110\u1ed1ng \u0110a", HeroType.QuangTrung, "Tang sat thuong chi mang.", BlessingRarity.Rare, 3, BlessingEffectType.CriticalPower, false),
            CreateOrUpdateBlessing("QT_ThanTocBacTien", "qt_than_toc_bac_tien", "Th\u1ea7n T\u1ed1c B\u1eafc Ti\u1ebfn", HeroType.QuangTrung, "Giam thoi gian hoi Dash.", BlessingRarity.Common, 3, BlessingEffectType.DashCooldown, false),
            CreateOrUpdateBlessing("QT_ThienLoiTaySon", "qt_thien_loi_tay_son", "Thi\u00ean L\u00f4i T\u00e2y S\u01a1n", HeroType.QuangTrung, "Don chi mang co ti le goi set danh xuong muc tieu.", BlessingRarity.Epic, 3, BlessingEffectType.CriticalLightning, false),
            CreateOrUpdateBlessing("QT_XuanKyDau", "qt_xuan_ky_dau", "Xu\u00e2n K\u1ef7 D\u1eadu", HeroType.QuangTrung, "Ultimate: cuong chien trong vai giay, tang toc danh, sat thuong va giam hoi Dash.", BlessingRarity.Legendary, 1, BlessingEffectType.KyDauFrenzy, true)
        };

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return blessings;
    }

    private static BlessingDefinition CreateOrUpdateBlessing(
        string assetName,
        string id,
        string displayName,
        HeroType hero,
        string description,
        BlessingRarity rarity,
        int maxStack,
        BlessingEffectType effect,
        bool ultimate)
    {
        string path = BlessingFolder + "/" + assetName + ".asset";
        BlessingDefinition blessing = AssetDatabase.LoadAssetAtPath<BlessingDefinition>(path);
        if (blessing == null)
        {
            blessing = ScriptableObject.CreateInstance<BlessingDefinition>();
            AssetDatabase.CreateAsset(blessing, path);
        }

        blessing.Configure(id, displayName, hero, description, rarity, maxStack, effect, ultimate);
        return blessing;
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, child);
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
        if (renderer != null && material != null)
            renderer.sharedMaterial = material;
    }

    private static void RemoveCollider(GameObject obj)
    {
        Collider collider = obj.GetComponent<Collider>();
        if (collider != null)
            Object.DestroyImmediate(collider);
    }

    private static void DeleteOldGeneratedObjects()
    {
        string[] names =
        {
            RootName,
            "S03_CoLoaCombatIntegration",
            "S03_BlessingChoiceRoot",
            "S03_WaveText",
            "S03_StatusText",
            "S03_ControlHintText",
            "S03_PlayerHealthRoot",
            "S03_BlessingManager",
            "S03_ArenaDirector"
        };

        foreach (string objectName in names)
            DeleteAllSceneObjectsNamed(objectName);

        DeleteAllSceneObjectsWithPrefix("S03_Wave");
        DeleteAllSceneObjectsNamed("S03_CoLoa_CombatSeal");
        DeleteAllSceneObjectsWithPrefix("S03_CoLoa_Boundary_");
        DeleteAllSceneObjectsWithPrefix("S03_CoLoa_PathRune_");
        DeleteAllSceneObjectsWithPrefix("S03_EnemySpawnMarker");
        DeleteAllSceneObjectsWithPrefix("S03_EnemySpawn_");
        DeleteAllSceneObjectsWithPrefix("vietnam_city_game_map");
        DeleteAllSceneObjectsWithPrefix("Vietnam_City_Game_Map");
        DeleteAllSceneObjectsNamed("minion");
        DeleteAllSceneObjectsNamed("Minion");
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

    private static void TrySetTag(GameObject obj, string tagName)
    {
        try
        {
            obj.tag = tagName;
        }
        catch (UnityException)
        {
            Debug.LogWarning("Tag not found: " + tagName + ". Please add it in Project Settings if needed.");
        }
    }

    private struct CombatLayout
    {
        public Vector3 Center;
        public float Radius;
        public Vector3 PlayerSpawn;
        public Transform[] SpawnPoints;
    }
}
