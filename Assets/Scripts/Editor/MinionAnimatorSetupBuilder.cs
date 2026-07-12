using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MinionAnimatorSetupBuilder
{
    private const string BaseModelPath = "Assets/Models/Minion/Minion/Action_Collector/Minion_Action_Collector/minion.fbx";
    private const string IdlePath = "Assets/Models/Minion/Minion/Action_Collector/Minion_Action_Collector/minion@Idle.fbx";
    private const string WalkPath = "Assets/Models/Minion/Minion/Action_Collector/Minion_Action_Collector/minion@Walking.fbx";
    private const string RunPath = "Assets/Models/Minion/Minion/Action_Collector/Minion_Action_Collector/minion@Zombie Run.fbx";
    private const string AttackPath = "Assets/Models/Minion/Minion/Action_Collector/Minion_Action_Collector/minion@Zombie Attack.fbx";
    private const string DeathPath = "Assets/Models/Minion/Minion/Action_Collector/Minion_Action_Collector/minion@Dying.fbx";
    private const string ControllerPath = "Assets/Animations/Minion/Minion.controller";
    private const string MinionPrefabPath = "Assets/Prefabs/Minion.prefab";
    private const string MoveSpeedParameter = "MoveSpeed";
    private const string AttackParameter = "Attack";
    private const string DieParameter = "Die";

    [MenuItem("Tools/Dong Chay Anh Hung/Setup Minion Animator")]
    public static void SetupMinionAnimator()
    {
        EnsureFolder("Assets/Animations/Minion");

        Avatar avatar = EnsureBaseAvatar();
        ConfigureActionImporter(IdlePath, avatar, "Idle", true);
        ConfigureActionImporter(WalkPath, avatar, "Walk", true);
        ConfigureActionImporter(RunPath, avatar, "Run", true);
        ConfigureActionImporter(AttackPath, avatar, "Attack", false);
        ConfigureActionImporter(DeathPath, avatar, "Death", false);
        AssetDatabase.Refresh();

        AnimationClip idle = FindClip(IdlePath, "Idle");
        AnimationClip walk = FindClip(WalkPath, "Walk");
        AnimationClip run = FindClip(RunPath, "Run");
        AnimationClip attack = FindClip(AttackPath, "Attack");
        AnimationClip death = FindClip(DeathPath, "Death");

        AnimatorController controller = BuildController(idle, walk, run, attack, death);
        GameObject minionPrefab = BuildMinionPrefab(controller, avatar);
        int sceneAssignments = AssignScenes(minionPrefab);
        DeleteOldEnemyAssets();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        WriteReport(controller, avatar, sceneAssignments, idle, walk, run, attack, death);
        Debug.Log("MinionAnimatorSetupBuilder: Minion setup completed.");
    }

    private static Avatar EnsureBaseAvatar()
    {
        ModelImporter importer = AssetImporter.GetAtPath(BaseModelPath) as ModelImporter;
        if (importer == null)
        {
            Debug.LogError("MinionAnimatorSetupBuilder: missing Minion model at " + BaseModelPath);
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
            Debug.LogWarning("MinionAnimatorSetupBuilder: no avatar found in Minion base model.");
        else if (!avatar.isHuman || !avatar.isValid)
            Debug.LogWarning("MinionAnimatorSetupBuilder: Minion avatar exists but is not a valid Humanoid avatar.");

        return avatar;
    }

    private static void ConfigureActionImporter(string path, Avatar avatar, string clipName, bool loop)
    {
        ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null)
        {
            Debug.LogWarning("MinionAnimatorSetupBuilder: missing Minion action FBX at " + path);
            return;
        }

        bool changed = false;
        if (importer.animationType != ModelImporterAnimationType.Human)
        {
            importer.animationType = ModelImporterAnimationType.Human;
            changed = true;
        }

        if (avatar != null)
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

    private static AnimatorController BuildController(AnimationClip idle, AnimationClip walk, AnimationClip run, AnimationClip attack, AnimationClip death)
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

        controller.AddParameter(MoveSpeedParameter, AnimatorControllerParameterType.Float);
        controller.AddParameter(AttackParameter, AnimatorControllerParameterType.Trigger);
        controller.AddParameter(DieParameter, AnimatorControllerParameterType.Trigger);

        AnimatorState idleState = AddState(stateMachine, "Idle", idle, new Vector3(260f, 90f, 0f), 1f);
        AnimatorState walkState = AddState(stateMachine, "Walk", walk, new Vector3(520f, 30f, 0f), 1f);
        AnimatorState runState = AddState(stateMachine, "Run", run != null ? run : walk, new Vector3(520f, 150f, 0f), 1.05f);
        AnimatorState attackState = AddState(stateMachine, "Attack", attack, new Vector3(780f, 90f, 0f), 1f);
        AnimatorState deathState = AddState(stateMachine, "Death", death, new Vector3(1040f, 90f, 0f), 1f);
        stateMachine.defaultState = idleState;

        AddFloatTransition(idleState, walkState, AnimatorConditionMode.Greater, 0.05f, MoveSpeedParameter, false, 0.08f);
        AddFloatTransition(walkState, idleState, AnimatorConditionMode.Less, 0.05f, MoveSpeedParameter, false, 0.08f);
        AddFloatTransition(walkState, runState, AnimatorConditionMode.Greater, 0.65f, MoveSpeedParameter, false, 0.08f);
        AddFloatTransition(runState, walkState, AnimatorConditionMode.Less, 0.65f, MoveSpeedParameter, false, 0.08f);
        AddFloatTransition(runState, idleState, AnimatorConditionMode.Less, 0.05f, MoveSpeedParameter, false, 0.08f);

        AddTriggerTransition(stateMachine, attackState, AttackParameter, 0.03f);
        AddTriggerTransition(stateMachine, deathState, DieParameter, 0.03f);
        AddExitTransition(attackState, idleState, 0.88f, 0.08f);

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

    private static AnimatorStateTransition AddFloatTransition(
        AnimatorState from,
        AnimatorState to,
        AnimatorConditionMode mode,
        float threshold,
        string parameter,
        bool hasExitTime,
        float duration)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = hasExitTime;
        transition.exitTime = hasExitTime ? 0.85f : 0f;
        transition.duration = duration;
        transition.canTransitionToSelf = false;
        transition.AddCondition(mode, threshold, parameter);
        return transition;
    }

    private static AnimatorStateTransition AddTriggerTransition(
        AnimatorStateMachine stateMachine,
        AnimatorState to,
        string parameter,
        float duration)
    {
        AnimatorStateTransition transition = stateMachine.AddAnyStateTransition(to);
        transition.hasExitTime = false;
        transition.duration = duration;
        transition.canTransitionToSelf = false;
        transition.AddCondition(AnimatorConditionMode.If, 0f, parameter);
        return transition;
    }

    private static AnimatorStateTransition AddExitTransition(
        AnimatorState from,
        AnimatorState to,
        float exitTime,
        float duration)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = true;
        transition.exitTime = exitTime;
        transition.duration = duration;
        transition.canTransitionToSelf = false;
        return transition;
    }

    private static GameObject BuildMinionPrefab(AnimatorController controller, Avatar avatar)
    {
        GameObject prefabContents = new GameObject("Minion");
        prefabContents.tag = "Enemy";
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer >= 0)
            prefabContents.layer = enemyLayer;

        CapsuleCollider collider = prefabContents.AddComponent<CapsuleCollider>();
        collider.center = new Vector3(0f, 1.05f, 0f);
        collider.radius = 0.45f;
        collider.height = 2.1f;

        MinionHealth3D health = prefabContents.AddComponent<MinionHealth3D>();
        health.maxHP = 50;
        health.currentHP = 50;
        health.destroyOnDeath = true;
        health.deathDelay = 1.25f;

        MinionChase3D chase = prefabContents.AddComponent<MinionChase3D>();
        chase.moveSpeed = 3f;
        chase.rotationSpeed = 10f;
        chase.chaseRange = 20f;
        chase.attackRange = 1.55f;
        chase.damage = 20;
        chase.attackCooldown = 1.25f;
        chase.groundSnapHeight = 8f;
        chase.groundSnapOffset = 0.02f;
        chase.idleState = "Idle";
        chase.moveState = "Run";
        chase.attackState = "Attack";
        chase.hitState = "Idle";
        chase.deathState = "Death";
        chase.movingAnimationSpeed = 1.05f;
        chase.attackAnimationSpeed = 1.15f;
        chase.attackImpactDelay = 0.45f;
        chase.attackLockDuration = 0.9f;
        chase.attackEffectDuration = 0.28f;
        chase.attackEffectColor = new Color(0.9f, 0.08f, 0.04f, 0.72f);
        chase.dieAfterSuccessfulAttack = false;

        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(BaseModelPath);
        if (model == null)
        {
            Object.DestroyImmediate(prefabContents);
            Debug.LogError("MinionAnimatorSetupBuilder: cannot build Minion prefab because model is missing: " + BaseModelPath);
            return null;
        }

        GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(model, prefabContents.transform);
        visual.name = "MinionVisual";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;
        NormalizeVisualScale(visual.transform, 1.9f);

        Animator animator = visual.GetComponent<Animator>();
        if (animator == null)
            animator = visual.AddComponent<Animator>();

        animator.runtimeAnimatorController = controller;
        if (avatar != null)
            animator.avatar = avatar;
        animator.applyRootMotion = false;
        animator.updateMode = AnimatorUpdateMode.Normal;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(prefabContents, MinionPrefabPath);
        Object.DestroyImmediate(prefabContents);
        return savedPrefab;
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

    private static int AssignScenes(GameObject minionPrefab)
    {
        if (minionPrefab == null)
            return 0;

        string[] scenePaths =
        {
            "Assets/Scenes/S01_CityPrototype.unity",
            "Assets/Scenes/S02_UndergroundCave.unity"
        };

        int assignments = 0;
        foreach (string scenePath in scenePaths)
        {
            if (!File.Exists(scenePath))
                continue;

            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            MonoBehaviour[] behaviours = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include);
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour == null)
                    continue;

                SerializedObject serialized = new SerializedObject(behaviour);
                bool changed = false;
                changed |= AssignObject(serialized, "enemyPrefab", minionPrefab);
                changed |= AssignObject(serialized, "minionPrefab", minionPrefab);

                if (!changed)
                    continue;

                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(behaviour);
                assignments++;
            }

            RenameSceneObject("MinionSpawner", "MinionSpawner");
            RenameSceneObject("MinionSpawn_ChaseStart", "MinionSpawn_ChaseStart");
            RenameSceneObject("MinionSpawnPoint_01", "MinionSpawnPoint_01");
            RenameSceneObject("MinionSpawnPoint_02", "MinionSpawnPoint_02");
            RenameSceneObject("MinionSpawnPoint_03", "MinionSpawnPoint_03");
            RenameSceneObject("CaveMinionSpawn_01", "CaveMinionSpawn_01");
            RenameSceneObject("CaveMinionSpawn_02", "CaveMinionSpawn_02");
            RenameSceneObject("CaveMinionSpawn_03", "CaveMinionSpawn_03");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        return assignments;
    }

    private static bool AssignObject(SerializedObject serialized, string propertyName, Object value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null || property.propertyType != SerializedPropertyType.ObjectReference)
            return false;

        if (property.objectReferenceValue == value)
            return false;

        property.objectReferenceValue = value;
        return true;
    }

    private static void RenameSceneObject(string oldName, string newName)
    {
        GameObject gameObject = GameObject.Find(oldName);
        if (gameObject != null)
            gameObject.name = newName;
    }

    private static void DeleteOldEnemyAssets()
    {
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

    private static void WriteReport(
        AnimatorController controller,
        Avatar avatar,
        int sceneAssignments,
        AnimationClip idle,
        AnimationClip walk,
        AnimationClip run,
        AnimationClip attack,
        AnimationClip death)
    {
        string reportPath = Path.Combine(Directory.GetCurrentDirectory(), "Library", "CodexBridge", "minion_animator_report.json");
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath));

        string json =
            "{\n" +
            "  \"controllerPath\": \"" + ControllerPath + "\",\n" +
            "  \"prefabPath\": \"" + MinionPrefabPath + "\",\n" +
            "  \"controllerCreated\": " + (controller != null ? "true" : "false") + ",\n" +
            "  \"avatarFound\": " + (avatar != null ? "true" : "false") + ",\n" +
            "  \"avatarIsHuman\": " + (avatar != null && avatar.isHuman ? "true" : "false") + ",\n" +
            "  \"avatarIsValid\": " + (avatar != null && avatar.isValid ? "true" : "false") + ",\n" +
            "  \"sceneAssignments\": " + sceneAssignments + ",\n" +
            "  \"idleClip\": \"" + (idle != null ? idle.name : string.Empty) + "\",\n" +
            "  \"walkClip\": \"" + (walk != null ? walk.name : string.Empty) + "\",\n" +
            "  \"runClip\": \"" + (run != null ? run.name : string.Empty) + "\",\n" +
            "  \"attackClip\": \"" + (attack != null ? attack.name : string.Empty) + "\",\n" +
            "  \"deathClip\": \"" + (death != null ? death.name : string.Empty) + "\"\n" +
            "}\n";

        File.WriteAllText(reportPath, json);
    }
}

