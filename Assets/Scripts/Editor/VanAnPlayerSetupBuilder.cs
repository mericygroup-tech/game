using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class VanAnPlayerSetupBuilder
{
    private const string MaterialFolder = "Assets/Models/Player/Materials";

    public const string BaseModelPath = "Assets/Models/Player/Van_An/Van_An_Model.fbx";
    public const string ControllerPath = "Assets/Animations/Player/VanAn.controller";
    public const string MaterialPath = MaterialFolder + "/VanAn_Player_Color.mat";

    private const string IdlePath = "Assets/Models/Player/Van_An/Van_An@Breathing Idle.fbx";
    private const string WalkPath = "Assets/Models/Player/Van_An/Van_An@Walking.fbx";
    private const string RunPath = "Assets/Models/Player/Van_An/Van_An@Running.fbx";
    private const string LightAttackPath = "Assets/Models/Player/Van_An/Van_An@Light Attack.fbx";
    private const string HitPath = "Assets/Models/Player/Van_An/Van_An@Hit To Body.fbx";
    private const string PushPath = "Assets/Models/Player/Van_An/Van_An@Push.fbx";
    private const string DeathPath = "Assets/Models/Player/Van_An/Van_An@Falling Back Death.fbx";
    private const string BaseColorTexturePath = "Assets/Models/Player/Van_An/texture_pbr_20250901.png";
    private const string NormalTexturePath = "Assets/Models/Player/Van_An/texture_pbr_20250901_normal.png";

    private const string SpeedParameter = "Speed";
    private const string GroundedParameter = "Grounded";
    private const string LightAttackParameter = "LightAttack";
    private const string HitParameter = "Hit";
    private const string PushParameter = "Push";
    private const string DieParameter = "Die";

    [MenuItem("Tools/Dong Chay Anh Hung/Setup Van An Player")]
    public static void SetupVanAnPlayer()
    {
        EnsureFolder("Assets/Animations/Player");

        Avatar avatar = EnsureBaseAvatar();
        ConfigureActionImporter(BaseModelPath, avatar, "ModelIdle", true, false);
        ConfigureActionImporter(IdlePath, avatar, "Idle", true, true);
        ConfigureActionImporter(WalkPath, avatar, "Walk", true, true);
        ConfigureActionImporter(RunPath, avatar, "Run", true, true);
        ConfigureActionImporter(LightAttackPath, avatar, "LightAttack", false, true);
        ConfigureActionImporter(HitPath, avatar, "Hit", false, true);
        ConfigureActionImporter(PushPath, avatar, "Push", false, true);
        ConfigureActionImporter(DeathPath, avatar, "Death", false, true);
        AssetDatabase.Refresh();

        AnimationClip idle = FindClip(IdlePath, "Idle") ?? FindClip(BaseModelPath, "ModelIdle");
        AnimationClip walk = FindClip(WalkPath, "Walk");
        AnimationClip run = FindClip(RunPath, "Run");
        AnimationClip lightAttack = FindClip(LightAttackPath, "LightAttack");
        AnimationClip hit = FindClip(HitPath, "Hit");
        AnimationClip push = FindClip(PushPath, "Push");
        AnimationClip death = FindClip(DeathPath, "Death");

        AnimatorController controller = BuildController(idle, walk, run, lightAttack, hit, push, death);
        Material playerMaterial = GetOrCreateVanAnMaterial();
        int changedScenes = ReplaceScenePlayers(controller, avatar, playerMaterial);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        WriteReport(controller, avatar, changedScenes, idle, walk, run, hit, push, death);
        Debug.Log("VanAnPlayerSetupBuilder: setup completed. Changed scenes: " + changedScenes);
    }

    [MenuItem("Tools/Dong Chay Anh Hung/Rebuild Van An Animator Controller")]
    public static void RebuildVanAnAnimatorController()
    {
        EnsureFolder("Assets/Animations/Player");

        Avatar avatar = EnsureBaseAvatar();
        ConfigureActionImporter(BaseModelPath, avatar, "ModelIdle", true, false);
        ConfigureActionImporter(IdlePath, avatar, "Idle", true, true);
        ConfigureActionImporter(WalkPath, avatar, "Walk", true, true);
        ConfigureActionImporter(RunPath, avatar, "Run", true, true);
        ConfigureActionImporter(LightAttackPath, avatar, "LightAttack", false, true);
        ConfigureActionImporter(HitPath, avatar, "Hit", false, true);
        ConfigureActionImporter(PushPath, avatar, "Push", false, true);
        ConfigureActionImporter(DeathPath, avatar, "Death", false, true);
        AssetDatabase.Refresh();

        AnimationClip idle = FindClip(IdlePath, "Idle") ?? FindClip(BaseModelPath, "ModelIdle");
        AnimationClip walk = FindClip(WalkPath, "Walk");
        AnimationClip run = FindClip(RunPath, "Run");
        AnimationClip lightAttack = FindClip(LightAttackPath, "LightAttack");
        AnimationClip hit = FindClip(HitPath, "Hit");
        AnimationClip push = FindClip(PushPath, "Push");
        AnimationClip death = FindClip(DeathPath, "Death");

        BuildController(idle, walk, run, lightAttack, hit, push, death);
        GetOrCreateVanAnMaterial();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("VanAnPlayerSetupBuilder: rebuilt Van An Animator Controller.");
    }

    [MenuItem("Tools/Dong Chay Anh Hung/Verify Van An Player Scenes")]
    public static void VerifyVanAnPlayerScenes()
    {
        RuntimeAnimatorController expectedController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
        Avatar expectedAvatar = EnsureBaseAvatar();
        string[] scenePaths = Directory.GetFiles("Assets/Scenes", "*.unity", SearchOption.TopDirectoryOnly);
        int verifiedPlayers = 0;

        foreach (string scenePath in scenePaths)
        {
            string sceneName = Path.GetFileNameWithoutExtension(scenePath);
            if (sceneName == "MainMenu")
                continue;

            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            GameObject[] players = FindPlayerObjects();
            if (players.Length == 0)
                throw new UnityException("Van An verify failed: no Player object found in " + sceneName + ".");

            foreach (GameObject player in players)
            {
                RequireVanAnVisual(player, expectedController, expectedAvatar, sceneName);
                RequireVanAnSword(player, sceneName);
                verifiedPlayers++;
            }
        }

        Debug.Log("Van An verification passed across gameplay scenes. Player objects verified: " + verifiedPlayers);
    }

    [MenuItem("Tools/Dong Chay Anh Hung/Run Full Van An Migration")]
    public static void RunFullVanAnMigration()
    {
        SetupVanAnPlayer();
        S03SinglePlayerArenaBuilder.BuildScene();
        S03SinglePlayerArenaBuilder.VerifyScene();
        VerifyVanAnPlayerScenes();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Full Van An migration completed.");
    }

    private static Avatar EnsureBaseAvatar()
    {
        ModelImporter importer = AssetImporter.GetAtPath(BaseModelPath) as ModelImporter;
        if (importer == null)
        {
            Debug.LogError("VanAnPlayerSetupBuilder: missing Van An model at " + BaseModelPath);
            return null;
        }

        bool changed = false;
        if (importer.animationType != ModelImporterAnimationType.Human)
        {
            importer.animationType = ModelImporterAnimationType.Human;
            changed = true;
        }

        if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
        {
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            changed = true;
        }

        if (changed)
            importer.SaveAndReimport();

        Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(BaseModelPath).OfType<Avatar>().FirstOrDefault();
        if (avatar == null)
            Debug.LogWarning("VanAnPlayerSetupBuilder: no avatar found in Van An model.");
        else if (!avatar.isHuman || !avatar.isValid)
            Debug.LogWarning("VanAnPlayerSetupBuilder: Van An avatar is not a valid Humanoid avatar.");

        return avatar;
    }

    private static void ConfigureActionImporter(string path, Avatar avatar, string clipName, bool loop, bool copyAvatar)
    {
        ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null)
        {
            Debug.LogWarning("VanAnPlayerSetupBuilder: missing FBX at " + path);
            return;
        }

        bool changed = false;
        if (importer.animationType != ModelImporterAnimationType.Human)
        {
            importer.animationType = ModelImporterAnimationType.Human;
            changed = true;
        }

        if (copyAvatar && avatar != null)
        {
            if (importer.avatarSetup != ModelImporterAvatarSetup.CopyFromOther)
            {
                importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                changed = true;
            }

            if (importer.sourceAvatar != avatar)
            {
                importer.sourceAvatar = avatar;
                changed = true;
            }
        }

        ModelImporterClipAnimation[] clips = importer.clipAnimations;
        if (clips == null || clips.Length == 0)
            clips = importer.defaultClipAnimations;

        if (clips != null && clips.Length > 0)
        {
            clips[0].name = clipName;
            clips[0].loopTime = loop;
            clips[0].wrapMode = loop ? WrapMode.Loop : WrapMode.Once;
            clips[0].keepOriginalPositionY = false;
            clips[0].keepOriginalPositionXZ = false;
            clips[0].keepOriginalOrientation = false;
            clips[0].heightFromFeet = true;
            importer.clipAnimations = clips;
            changed = true;
        }

        if (changed)
            importer.SaveAndReimport();
    }

    private static AnimationClip FindClip(string path, string preferredName)
    {
        AnimationClip exact = AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<AnimationClip>()
            .FirstOrDefault(clip => clip.name == preferredName && !clip.name.StartsWith("__preview__"));

        if (exact != null)
            return exact;

        return AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<AnimationClip>()
            .FirstOrDefault(clip => !clip.name.StartsWith("__preview__"));
    }

    private static AnimatorController BuildController(AnimationClip idle, AnimationClip walk, AnimationClip run, AnimationClip lightAttack, AnimationClip hit, AnimationClip push, AnimationClip death)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        foreach (ChildAnimatorState childState in stateMachine.states.ToArray())
            stateMachine.RemoveState(childState.state);
        foreach (ChildAnimatorStateMachine childStateMachine in stateMachine.stateMachines.ToArray())
            stateMachine.RemoveStateMachine(childStateMachine.stateMachine);
        foreach (AnimatorControllerParameter parameter in controller.parameters.ToArray())
            controller.RemoveParameter(parameter);

        controller.AddParameter(SpeedParameter, AnimatorControllerParameterType.Float);
        controller.AddParameter(GroundedParameter, AnimatorControllerParameterType.Bool);
        controller.AddParameter(LightAttackParameter, AnimatorControllerParameterType.Trigger);
        controller.AddParameter(HitParameter, AnimatorControllerParameterType.Trigger);
        controller.AddParameter(PushParameter, AnimatorControllerParameterType.Trigger);
        controller.AddParameter(DieParameter, AnimatorControllerParameterType.Trigger);

        AnimatorState idleState = AddState(stateMachine, "Idle", idle, new Vector3(260f, 90f, 0f), 1f);
        AnimatorState walkState = AddState(stateMachine, "Walk", walk, new Vector3(520f, 30f, 0f), 1f);
        AnimatorState runState = AddState(stateMachine, "Run", run != null ? run : walk, new Vector3(520f, 150f, 0f), 1f);
        AnimatorState lightAttackState = AddState(stateMachine, "LightAttack", lightAttack != null ? lightAttack : push, new Vector3(780f, -60f, 0f), 1f);
        AnimatorState hitState = AddState(stateMachine, "Hit", hit, new Vector3(780f, 30f, 0f), 1f);
        AnimatorState pushState = AddState(stateMachine, "Push", push, new Vector3(780f, 100f, 0f), 1f);
        AnimatorState deathState = AddState(stateMachine, "Death", death, new Vector3(1040f, 170f, 0f), 1f);
        stateMachine.defaultState = idleState;

        AddFloatTransition(idleState, walkState, AnimatorConditionMode.Greater, 0.05f, SpeedParameter);
        AddFloatTransition(walkState, idleState, AnimatorConditionMode.Less, 0.05f, SpeedParameter);
        AddFloatTransition(walkState, runState, AnimatorConditionMode.Greater, 0.55f, SpeedParameter);
        AddFloatTransition(runState, walkState, AnimatorConditionMode.Less, 0.55f, SpeedParameter);
        AddFloatTransition(runState, idleState, AnimatorConditionMode.Less, 0.05f, SpeedParameter);
        AddTriggerTransition(stateMachine, lightAttackState, LightAttackParameter);
        AddTriggerTransition(stateMachine, hitState, HitParameter);
        AddTriggerTransition(stateMachine, pushState, PushParameter);
        AddTriggerTransition(stateMachine, deathState, DieParameter);
        AddExitTransition(lightAttackState, idleState, 0.9f);
        AddExitTransition(hitState, idleState, 0.85f);
        AddExitTransition(pushState, idleState, 0.88f);

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static AnimatorState AddState(AnimatorStateMachine stateMachine, string name, Motion motion, Vector3 position, float speed)
    {
        AnimatorState state = stateMachine.AddState(name, position);
        state.motion = motion;
        state.speed = speed;
        return state;
    }

    private static AnimatorStateTransition AddFloatTransition(AnimatorState from, AnimatorState to, AnimatorConditionMode mode, float threshold, string parameter)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.duration = 0.08f;
        transition.canTransitionToSelf = false;
        transition.AddCondition(mode, threshold, parameter);
        return transition;
    }

    private static AnimatorStateTransition AddTriggerTransition(AnimatorStateMachine stateMachine, AnimatorState to, string parameter)
    {
        AnimatorStateTransition transition = stateMachine.AddAnyStateTransition(to);
        transition.hasExitTime = false;
        transition.duration = 0.04f;
        transition.canTransitionToSelf = false;
        transition.AddCondition(AnimatorConditionMode.If, 0f, parameter);
        return transition;
    }

    private static AnimatorStateTransition AddExitTransition(AnimatorState from, AnimatorState to, float exitTime)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = true;
        transition.exitTime = exitTime;
        transition.duration = 0.08f;
        transition.canTransitionToSelf = false;
        return transition;
    }

    private static int ReplaceScenePlayers(AnimatorController controller, Avatar avatar, Material playerMaterial)
    {
        string[] scenePaths = Directory.GetFiles("Assets/Scenes", "*.unity", SearchOption.TopDirectoryOnly);
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(BaseModelPath);
        if (model == null)
        {
            Debug.LogError("VanAnPlayerSetupBuilder: model missing at " + BaseModelPath);
            return 0;
        }

        int changedScenes = 0;
        foreach (string scenePath in scenePaths)
        {
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            bool changed = false;
            GameObject[] players = FindPlayerObjects();

            foreach (GameObject player in players)
            {
                changed |= ReplacePlayerVisual(player, model, controller, avatar, playerMaterial);
                changed |= EnsurePlayerDriver(player);
                changed |= VanAnWeaponLoadoutBuilder.SetupSwordOnPlayer(player) != null;
            }

            changed |= ConfigureCameras(players);

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                changedScenes++;
            }
        }

        return changedScenes;
    }

    private static GameObject[] FindPlayerObjects()
    {
        HashSet<GameObject> players = new HashSet<GameObject>();

        foreach (PlayerController3D controller in Object.FindObjectsByType<PlayerController3D>(FindObjectsInactive.Include))
        {
            if (controller != null)
                players.Add(controller.gameObject);
        }

        foreach (PlayerHealth3D health in Object.FindObjectsByType<PlayerHealth3D>(FindObjectsInactive.Include))
        {
            if (health != null)
                players.Add(health.gameObject);
        }

        foreach (GameObject taggedPlayer in GameObject.FindGameObjectsWithTag("Player"))
        {
            if (taggedPlayer != null)
                players.Add(taggedPlayer);
        }

        return players.ToArray();
    }

    private static bool ReplacePlayerVisual(GameObject player, GameObject model, AnimatorController controller, Avatar avatar, Material playerMaterial)
    {
        Transform oldVisual = player.transform.Find("PlayerVisual");
        if (oldVisual != null)
            Object.DestroyImmediate(oldVisual.gameObject);

        GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(model, player.transform);
        visual.name = "PlayerVisual";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;
        NormalizeVisualScale(visual.transform, 1.75f);
        AlignVisualToControllerFeet(player, visual.transform);
        ApplyMaterialToVisual(visual, playerMaterial);

        Animator animator = visual.GetComponent<Animator>();
        if (animator == null)
            animator = visual.AddComponent<Animator>();

        animator.runtimeAnimatorController = controller;
        if (avatar != null)
            animator.avatar = avatar;
        animator.applyRootMotion = false;
        animator.updateMode = AnimatorUpdateMode.Normal;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        EditorUtility.SetDirty(visual);
        EditorUtility.SetDirty(animator);
        return true;
    }

    private static void ApplyMaterialToVisual(GameObject visual, Material material)
    {
        if (visual == null || material == null)
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

    private static void RequireVanAnVisual(GameObject player, RuntimeAnimatorController expectedController, Avatar expectedAvatar, string sceneName)
    {
        Transform visual = player.transform.Find("PlayerVisual");
        if (visual == null)
            throw new UnityException("Van An verify failed: PlayerVisual missing in " + sceneName + ".");

        Animator animator = visual.GetComponent<Animator>();
        if (animator == null)
            throw new UnityException("Van An verify failed: PlayerVisual has no Animator in " + sceneName + ".");

        if (expectedController != null && animator.runtimeAnimatorController != expectedController)
            throw new UnityException("Van An verify failed: PlayerVisual is not using VanAn.controller in " + sceneName + ".");

        if (expectedAvatar != null && animator.avatar != expectedAvatar)
            throw new UnityException("Van An verify failed: PlayerVisual is not using Van An Avatar in " + sceneName + ".");
    }

    private static void RequireVanAnSword(GameObject player, string sceneName)
    {
        Transform socket = FindChildRecursive(player.transform, "RightHandWeaponSocket");
        if (socket == null)
            throw new UnityException("Van An verify failed: RightHandWeaponSocket missing in " + sceneName + ".");

        Transform sword = FindChildRecursive(player.transform, "VanAn_SwordVisual");
        if (sword == null)
            throw new UnityException("Van An verify failed: VanAn_SwordVisual missing in " + sceneName + ".");

        if (sword.GetComponent<WeaponVisualAnchor3D>() == null)
            throw new UnityException("Van An verify failed: VanAn_SwordVisual has no WeaponVisualAnchor3D in " + sceneName + ".");
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

    private static bool EnsurePlayerDriver(GameObject player)
    {
        bool changed = false;
        PlayerAnimatorDriver driver = player.GetComponent<PlayerAnimatorDriver>();
        if (driver == null)
        {
            driver = player.AddComponent<PlayerAnimatorDriver>();
            changed = true;
        }

        PlayerController3D controller = player.GetComponent<PlayerController3D>();
        Camera mainCamera = Camera.main;
        if (controller != null && mainCamera != null && controller.cameraTransform != mainCamera.transform)
        {
            controller.cameraTransform = mainCamera.transform;
            EditorUtility.SetDirty(controller);
            changed = true;
        }

        EditorUtility.SetDirty(driver);
        return changed;
    }

    private static bool ConfigureCameras(GameObject[] players)
    {
        bool changed = false;
        ThirdPersonCamera[] cameras = Object.FindObjectsByType<ThirdPersonCamera>(FindObjectsInactive.Include);
        foreach (ThirdPersonCamera camera in cameras)
        {
            if (camera == null)
                continue;

            GameObject player = players.FirstOrDefault();
            if (player != null && camera.target != player.transform)
            {
                camera.target = player.transform;
                changed = true;
            }

            camera.fixedAngle = true;
            camera.fixedYaw = 45f;
            camera.fixedPitch = 58f;
            camera.lockCursor = false;
            camera.distance = 4.8f;
            camera.height = 4f;
            camera.smoothSpeed = 8f;
            EditorUtility.SetDirty(camera);
            changed = true;
        }

        return changed;
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

        renderers = visual.GetComponentsInChildren<Renderer>(true);
        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        visual.localPosition += new Vector3(0f, -bounds.min.y, 0f);
    }

    private static void EnsureFolder(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder) || AssetDatabase.IsValidFolder(folder))
            return;

        string parent = Path.GetDirectoryName(folder).Replace("\\", "/");
        string child = Path.GetFileName(folder);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, child);
    }

    public static Material GetOrCreateVanAnMaterial()
    {
        EnsureFolder(MaterialFolder);

        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            material = new Material(shader)
            {
                name = "VanAn_Player_Color"
            };
            AssetDatabase.CreateAsset(material, MaterialPath);
        }

        Texture2D baseColor = AssetDatabase.LoadAssetAtPath<Texture2D>(BaseColorTexturePath);
        Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(NormalTexturePath);

        material.color = Color.white;
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", Color.white);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", Color.white);

        if (baseColor != null)
        {
            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", baseColor);
            if (material.HasProperty("_MainTex"))
                material.SetTexture("_MainTex", baseColor);
        }

        if (normal != null)
        {
            if (material.HasProperty("_BumpMap"))
                material.SetTexture("_BumpMap", normal);
            material.EnableKeyword("_NORMALMAP");
        }

        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", 0.35f);
        if (material.HasProperty("_Metallic"))
            material.SetFloat("_Metallic", 0f);

        EditorUtility.SetDirty(material);
        return material;
    }

    private static void WriteReport(AnimatorController controller, Avatar avatar, int changedScenes, AnimationClip idle, AnimationClip walk, AnimationClip run, AnimationClip hit, AnimationClip push, AnimationClip death)
    {
        string reportPath = Path.Combine(Directory.GetCurrentDirectory(), "Library", "CodexBridge", "van_an_player_setup_report.json");
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath));

        string json =
            "{\n" +
            "  \"controllerPath\": \"" + ControllerPath + "\",\n" +
            "  \"modelPath\": \"" + BaseModelPath + "\",\n" +
            "  \"controllerCreated\": " + (controller != null ? "true" : "false") + ",\n" +
            "  \"avatarFound\": " + (avatar != null ? "true" : "false") + ",\n" +
            "  \"avatarIsHuman\": " + (avatar != null && avatar.isHuman ? "true" : "false") + ",\n" +
            "  \"avatarIsValid\": " + (avatar != null && avatar.isValid ? "true" : "false") + ",\n" +
            "  \"changedScenes\": " + changedScenes + ",\n" +
            "  \"idleClip\": \"" + (idle != null ? idle.name : string.Empty) + "\",\n" +
            "  \"walkClip\": \"" + (walk != null ? walk.name : string.Empty) + "\",\n" +
            "  \"runClip\": \"" + (run != null ? run.name : string.Empty) + "\",\n" +
            "  \"hitClip\": \"" + (hit != null ? hit.name : string.Empty) + "\",\n" +
            "  \"pushClip\": \"" + (push != null ? push.name : string.Empty) + "\",\n" +
            "  \"deathClip\": \"" + (death != null ? death.name : string.Empty) + "\"\n" +
            "}\n";

        File.WriteAllText(reportPath, json);
    }
}
