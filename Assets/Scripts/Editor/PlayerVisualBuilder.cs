using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class PlayerVisualBuilder
{
    private const string VisualRootName = "PlayerVisual";
    private const string ImportedModelName = "ImportedModel";
    private const string PlayerModelPath = VanAnPlayerSetupBuilder.BaseModelPath;
    private const string MaterialFolder = "Assets/Models/Player/Materials";
    private const string ColoredMaterialPath = VanAnPlayerSetupBuilder.MaterialPath;
    private const string BaseColorTexturePath = "Assets/Models/Player/Van_An/texture_pbr_20250901.png";
    private const string NormalTexturePath = "Assets/Models/Player/Van_An/texture_pbr_20250901_normal.png";
    private const float TargetVisualHeight = 1.75f;
    private const float GroundClearance = 0.02f;

    public static void CreatePlayerVisual()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("PlayerVisualBuilder: Exit Play Mode before creating the Player visual.");
            return;
        }

        GameObject player = FindPlayer();
        if (player == null)
        {
            Debug.LogWarning("PlayerVisualBuilder: Player object not found.");
            return;
        }

        Transform oldVisual = player.transform.Find(VisualRootName);
        if (oldVisual != null)
            Object.DestroyImmediate(oldVisual.gameObject);

        HideOldRootVisual(player);

        if (TryCreateImportedVisual(player))
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("Created Player visual from imported humanoid model: " + PlayerModelPath);
            return;
        }

        Debug.LogWarning("PlayerVisualBuilder: Imported humanoid model was not available. Creating the primitive fallback visual.");
        EnsureMaterialFolder();

        Material skinMat = GetOrCreateMaterial("PV_Skin", new Color32(214, 164, 128, 255), 0.15f);
        Material hairMat = GetOrCreateMaterial("PV_Hair", new Color32(22, 24, 27, 255), 0.25f);
        Material shirtMat = GetOrCreateMaterial("PV_Shirt", new Color32(224, 235, 238, 255), 0.15f);
        Material shirtShadowMat = GetOrCreateMaterial("PV_ShirtShadow", new Color32(172, 194, 202, 255), 0.2f);
        Material pantsMat = GetOrCreateMaterial("PV_Pants", new Color32(38, 49, 68, 255), 0.2f);
        Material shoeMat = GetOrCreateMaterial("PV_Shoes", new Color32(34, 36, 40, 255), 0.3f);
        Material soleMat = GetOrCreateMaterial("PV_ShoeSoles", new Color32(195, 199, 198, 255), 0.2f);
        Material eyeMat = GetOrCreateMaterial("PV_Eyes", new Color32(25, 28, 30, 255), 0.2f);
        Material bagMat = GetOrCreateMaterial("PV_Bag", new Color32(59, 79, 75, 255), 0.25f);
        Material bagAccentMat = GetOrCreateMaterial("PV_BagAccent", new Color32(135, 105, 62, 255), 0.25f);

        GameObject visualRoot = new GameObject(VisualRootName);
        Undo.RegisterCreatedObjectUndo(visualRoot, "Create Văn An Player Visual");
        visualRoot.transform.SetParent(player.transform, false);
        visualRoot.transform.localPosition = Vector3.zero;
        visualRoot.transform.localRotation = Quaternion.identity;
        visualRoot.transform.localScale = Vector3.one * 0.9f;
        Undo.AddComponent<PlayerAnimatorDriver>(visualRoot);

        Transform body = CreateGroup(visualRoot.transform, "Body");
        Transform head = CreateGroup(visualRoot.transform, "Head");
        Transform hair = CreateGroup(visualRoot.transform, "Hair");
        Transform arms = CreateGroup(visualRoot.transform, "Arms");
        Transform legs = CreateGroup(visualRoot.transform, "Legs");
        Transform shoes = CreateGroup(visualRoot.transform, "Shoes");
        Transform accessories = CreateGroup(visualRoot.transform, "Accessories");

        BuildBody(body, shirtMat, shirtShadowMat, pantsMat, skinMat);
        BuildHead(head, skinMat, eyeMat, shirtMat);
        BuildHair(hair, hairMat);
        BuildArms(arms, shirtMat, skinMat);
        BuildLegs(legs, pantsMat);
        BuildShoes(shoes, shoeMat, soleMat);
        BuildSatchel(accessories, bagMat, bagAccentMat);

        Selection.activeGameObject = visualRoot;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Created stylized Văn An student visual on Player.");
    }

    public static void PreparePlayerVisualForAnimation()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("PlayerVisualBuilder: Exit Play Mode before preparing Player animation.");
            return;
        }

        GameObject player = FindPlayer();
        if (player == null)
        {
            Debug.LogWarning("PlayerVisualBuilder: Player object not found.");
            return;
        }

        Transform visual = player.transform.Find(VisualRootName);
        if (visual == null)
        {
            CreatePlayerVisual();
            return;
        }

        Animator animator = EnsurePlayerAnimator(visual);
        if (animator == null)
        {
            Debug.LogWarning("PlayerVisualBuilder: Could not prepare the Player Animator.");
            return;
        }

        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(visual.gameObject);
        if (visual.GetComponent<PlayerAnimatorDriver>() == null)
            Undo.AddComponent<PlayerAnimatorDriver>(visual.gameObject);

        animator.runtimeAnimatorController = null;
        animator.enabled = true;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        EnsurePlayerIsNotParentedToLight(player);
        AlignImportedVisualToControllerGround(player, visual);
        EditorUtility.SetDirty(animator);
        EditorUtility.SetDirty(visual.gameObject);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = visual.gameObject;
        Debug.Log("Prepared clean Humanoid Player visual. Assign a valid locomotion Animator Controller when ready.");
    }

    public static void ApplyPlayerColors()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("PlayerVisualBuilder: Exit Play Mode before applying Player colors.");
            return;
        }

        GameObject player = FindPlayer();
        Transform visual = player != null ? player.transform.Find(VisualRootName) : null;
        if (visual == null)
        {
            Debug.LogWarning("PlayerVisualBuilder: PlayerVisual object not found.");
            return;
        }

        if (GetOrCreateColoredMaterial() == null)
        {
            Debug.LogWarning("PlayerVisualBuilder: Could not apply the extracted Player color texture.");
            return;
        }

        ApplyColorMaterial(visual);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = visual.gameObject;
        Debug.Log("Applied Văn An original color texture to PlayerVisual.");
    }

    private static bool TryCreateImportedVisual(GameObject player)
    {
        ConfigurePlayerModelAsHumanoid();
        AssetDatabase.ImportAsset(PlayerModelPath, ImportAssetOptions.ForceUpdate);
        GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerModelPath);
        if (modelAsset == null)
            return false;

        GameObject visualRoot = new GameObject(VisualRootName);
        Undo.RegisterCreatedObjectUndo(visualRoot, "Create imported Player visual");
        visualRoot.transform.SetParent(player.transform, false);
        Undo.AddComponent<PlayerAnimatorDriver>(visualRoot);

        GameObject importedModel = PrefabUtility.InstantiatePrefab(modelAsset) as GameObject;
        if (importedModel == null)
            importedModel = Object.Instantiate(modelAsset);

        importedModel.name = ImportedModelName;
        importedModel.transform.SetParent(visualRoot.transform, false);
        importedModel.transform.localPosition = Vector3.zero;
        importedModel.transform.localRotation = Quaternion.identity;
        importedModel.transform.localScale = Vector3.one;
        PrefabUtility.UnpackPrefabInstance(importedModel, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        Collider[] colliders = importedModel.GetComponentsInChildren<Collider>(true);
        foreach (Collider collider in colliders)
            Object.DestroyImmediate(collider);

        Animator[] animators = importedModel.GetComponentsInChildren<Animator>(true);
        if (animators.Length == 0)
            animators = new[] { EnsurePlayerAnimator(importedModel.transform) };

        foreach (Animator animator in animators)
        {
            if (animator == null)
                continue;

            animator.runtimeAnimatorController = null;
            animator.enabled = true;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        Renderer[] renderers = importedModel.GetComponentsInChildren<Renderer>(true);
        ApplyColorMaterial(importedModel.transform);
        if (!TryGetRendererBounds(renderers, out Bounds bounds) || bounds.size.y <= 0.001f)
        {
            Object.DestroyImmediate(visualRoot);
            Debug.LogWarning("PlayerVisualBuilder: The imported FBX has no usable render bounds.");
            return false;
        }

        float scaleFactor = TargetVisualHeight / bounds.size.y;
        importedModel.transform.localScale = Vector3.one * scaleFactor;

        if (TryGetRendererBounds(renderers, out bounds))
        {
            Vector3 playerPosition = player.transform.position;
            float targetBottom = GetPlayerControllerBottom(player) + GroundClearance;
            Vector3 correction = new Vector3(
                playerPosition.x - bounds.center.x,
                targetBottom - bounds.min.y,
                playerPosition.z - bounds.center.z);
            importedModel.transform.position += correction;
        }

        Selection.activeGameObject = visualRoot;
        return true;
    }

    private static bool EnsurePlayerIsNotParentedToLight(GameObject player)
    {
        if (player == null || player.transform.parent == null ||
            player.transform.parent.GetComponent<Light>() == null)
        {
            return false;
        }

        Undo.SetTransformParent(player.transform, null, "Move Player to scene root");
        EditorUtility.SetDirty(player);
        return true;
    }

    private static bool AlignImportedVisualToControllerGround(GameObject player, Transform visual)
    {
        if (player == null || visual == null)
            return false;

        Transform importedModel = visual.Find(ImportedModelName);
        if (importedModel == null)
            return false;

        Renderer[] renderers = importedModel.GetComponentsInChildren<Renderer>(true);
        if (!TryGetRendererBounds(renderers, out Bounds bounds))
            return false;

        float targetBottom = GetPlayerControllerBottom(player) + GroundClearance;
        float correction = targetBottom - bounds.min.y;
        bool changed = Mathf.Abs(correction) >= 0.002f;
        CharacterController controller = player.GetComponent<CharacterController>();
        bool needsFallbackCorrection = controller != null &&
            importedModel.localPosition.y > controller.center.y + controller.height * 0.25f;

        if (!changed && !needsFallbackCorrection)
            return false;

        Undo.RecordObject(importedModel, "Align Player visual to controller");
        if (changed)
            importedModel.position += Vector3.up * correction;

        if (needsFallbackCorrection &&
            importedModel.localPosition.y > controller.center.y + controller.height * 0.25f)
        {
            Vector3 localPosition = importedModel.localPosition;
            localPosition.y -= controller.height * 0.5f;
            importedModel.localPosition = localPosition;
        }

        EditorUtility.SetDirty(importedModel);
        return true;
    }

    private static float GetPlayerControllerBottom(GameObject player)
    {
        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller == null)
            return player.transform.position.y;

        Vector3 center = player.transform.TransformPoint(controller.center);
        return center.y - controller.height * Mathf.Abs(player.transform.lossyScale.y) * 0.5f;
    }

    private static Animator EnsurePlayerAnimator(Transform visual)
    {
        if (visual == null)
            return null;

        ConfigurePlayerModelAsHumanoid();

        Transform animatorTarget = visual.Find(ImportedModelName);
        if (animatorTarget == null)
            animatorTarget = visual;

        GameObject prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(animatorTarget.gameObject);
        if (prefabRoot != null)
            PrefabUtility.UnpackPrefabInstance(prefabRoot, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        Animator[] childAnimators = visual.GetComponentsInChildren<Animator>(true);
        foreach (Animator childAnimator in childAnimators)
        {
            if (childAnimator != null && childAnimator.transform != animatorTarget)
                Object.DestroyImmediate(childAnimator);
        }

        Animator animator = animatorTarget.GetComponent<Animator>();
        if (animator == null)
            animator = Undo.AddComponent<Animator>(animatorTarget.gameObject);

        Avatar avatar = FindPlayerAvatar();
        if (avatar != null)
            animator.avatar = avatar;
        else
            Debug.LogWarning("PlayerVisualBuilder: A valid Humanoid Avatar was not found in " + PlayerModelPath);

        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        animator.updateMode = AnimatorUpdateMode.Normal;
        animator.enabled = true;
        EditorUtility.SetDirty(animator);
        return animator;
    }

    private static void ConfigurePlayerModelAsHumanoid()
    {
        ModelImporter importer = AssetImporter.GetAtPath(PlayerModelPath) as ModelImporter;
        if (importer == null)
            return;

        bool requiresReimport = importer.animationType != ModelImporterAnimationType.Human ||
                                importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel ||
                                importer.optimizeGameObjects;
        if (!requiresReimport)
            return;

        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.optimizeGameObjects = false;
        importer.importAnimation = true;
        importer.SaveAndReimport();
    }

    private static Avatar FindPlayerAvatar()
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(PlayerModelPath);
        foreach (Object asset in assets)
        {
            Avatar avatar = asset as Avatar;
            if (avatar != null && avatar.isValid && avatar.isHuman)
                return avatar;
        }

        return null;
    }

    private static bool ApplyColorMaterial(Transform visual)
    {
        if (visual == null)
            return false;

        Material coloredMaterial = GetOrCreateColoredMaterial();
        if (coloredMaterial == null)
            return false;

        bool changed = false;
        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] == coloredMaterial)
                    continue;

                materials[i] = coloredMaterial;
                changed = true;
            }

            if (materials.Length == 0)
            {
                materials = new[] { coloredMaterial };
                changed = true;
            }

            renderer.sharedMaterials = materials;
            EditorUtility.SetDirty(renderer);
        }

        return changed;
    }

    private static Material GetOrCreateColoredMaterial()
    {
        EnsureMaterialFolder();
        ConfigurePlayerTexture(BaseColorTexturePath, false);
        ConfigurePlayerTexture(NormalTexturePath, true);

        Texture2D baseColor = AssetDatabase.LoadAssetAtPath<Texture2D>(BaseColorTexturePath);
        Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(NormalTexturePath);
        if (baseColor == null)
            return null;

        Material material = AssetDatabase.LoadAssetAtPath<Material>(ColoredMaterialPath);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            material = new Material(shader)
            {
                name = "VanAn_Player_Color"
            };
            AssetDatabase.CreateAsset(material, ColoredMaterialPath);
        }

        material.color = Color.white;
        if (material.HasProperty("_BaseMap"))
            material.SetTexture("_BaseMap", baseColor);
        else
            material.mainTexture = baseColor;

        if (normal != null && material.HasProperty("_BumpMap"))
        {
            material.SetTexture("_BumpMap", normal);
            material.EnableKeyword("_NORMALMAP");
        }

        if (material.HasProperty("_Metallic"))
            material.SetFloat("_Metallic", 0f);
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", 0.28f);

        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
        return material;
    }

    private static void ConfigurePlayerTexture(string assetPath, bool normalMap)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
            return;

        TextureImporterType desiredType = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
        bool desiredSrgb = !normalMap;
        if (importer.textureType == desiredType && importer.sRGBTexture == desiredSrgb)
            return;

        importer.textureType = desiredType;
        importer.sRGBTexture = desiredSrgb;
        importer.SaveAndReimport();
    }

    private static bool TryGetRendererBounds(Renderer[] renderers, out Bounds bounds)
    {
        bounds = default;
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

        return hasBounds;
    }

    private static void BuildBody(Transform parent, Material shirtMat, Material shirtShadowMat, Material pantsMat, Material skinMat)
    {
        CreatePrimitive(parent, "Torso", PrimitiveType.Capsule, new Vector3(0f, 1.14f, 0f), Vector3.zero, new Vector3(0.34f, 0.34f, 0.25f), shirtMat);
        CreatePrimitive(parent, "Waist", PrimitiveType.Cube, new Vector3(0f, 0.89f, 0f), Vector3.zero, new Vector3(0.48f, 0.18f, 0.30f), pantsMat);

        CreatePrimitive(parent, "ShirtFrontPanel", PrimitiveType.Cube, new Vector3(0f, 1.16f, 0.245f), Vector3.zero, new Vector3(0.42f, 0.48f, 0.035f), shirtMat);
        CreatePrimitive(parent, "ShirtHem", PrimitiveType.Cube, new Vector3(0f, 0.94f, 0.20f), Vector3.zero, new Vector3(0.43f, 0.07f, 0.05f), shirtShadowMat);
        CreatePrimitive(parent, "Collar_Left", PrimitiveType.Cube, new Vector3(-0.085f, 1.40f, 0.245f), new Vector3(0f, 0f, 25f), new Vector3(0.13f, 0.16f, 0.035f), shirtShadowMat);
        CreatePrimitive(parent, "Collar_Right", PrimitiveType.Cube, new Vector3(0.085f, 1.40f, 0.245f), new Vector3(0f, 0f, -25f), new Vector3(0.13f, 0.16f, 0.035f), shirtShadowMat);
        CreatePrimitive(parent, "Neck", PrimitiveType.Cylinder, new Vector3(0f, 1.48f, 0f), Vector3.zero, new Vector3(0.12f, 0.12f, 0.12f), skinMat);
    }

    private static void BuildHead(Transform parent, Material skinMat, Material eyeMat, Material shirtMat)
    {
        CreatePrimitive(parent, "HeadShape", PrimitiveType.Sphere, new Vector3(0f, 1.66f, 0f), Vector3.zero, new Vector3(0.42f, 0.47f, 0.39f), skinMat);
        CreatePrimitive(parent, "Ear_Left", PrimitiveType.Sphere, new Vector3(-0.225f, 1.66f, 0f), Vector3.zero, new Vector3(0.08f, 0.12f, 0.07f), skinMat);
        CreatePrimitive(parent, "Ear_Right", PrimitiveType.Sphere, new Vector3(0.225f, 1.66f, 0f), Vector3.zero, new Vector3(0.08f, 0.12f, 0.07f), skinMat);

        CreatePrimitive(parent, "Eye_Left", PrimitiveType.Sphere, new Vector3(-0.085f, 1.70f, 0.192f), Vector3.zero, new Vector3(0.045f, 0.035f, 0.025f), eyeMat);
        CreatePrimitive(parent, "Eye_Right", PrimitiveType.Sphere, new Vector3(0.085f, 1.70f, 0.192f), Vector3.zero, new Vector3(0.045f, 0.035f, 0.025f), eyeMat);
        CreatePrimitive(parent, "Brow_Left", PrimitiveType.Cube, new Vector3(-0.085f, 1.755f, 0.196f), new Vector3(0f, 0f, -5f), new Vector3(0.09f, 0.018f, 0.018f), eyeMat);
        CreatePrimitive(parent, "Brow_Right", PrimitiveType.Cube, new Vector3(0.085f, 1.755f, 0.196f), new Vector3(0f, 0f, 5f), new Vector3(0.09f, 0.018f, 0.018f), eyeMat);
        CreatePrimitive(parent, "Nose", PrimitiveType.Sphere, new Vector3(0f, 1.65f, 0.21f), Vector3.zero, new Vector3(0.035f, 0.055f, 0.04f), skinMat);
        CreatePrimitive(parent, "Mouth", PrimitiveType.Cube, new Vector3(0f, 1.59f, 0.205f), Vector3.zero, new Vector3(0.08f, 0.012f, 0.012f), shirtMat);
    }

    private static void BuildHair(Transform parent, Material hairMat)
    {
        CreatePrimitive(parent, "HairCap", PrimitiveType.Sphere, new Vector3(0f, 1.81f, -0.01f), Vector3.zero, new Vector3(0.435f, 0.29f, 0.41f), hairMat);
        CreatePrimitive(parent, "HairBack", PrimitiveType.Cube, new Vector3(0f, 1.72f, -0.19f), new Vector3(8f, 0f, 0f), new Vector3(0.35f, 0.24f, 0.08f), hairMat);
        CreatePrimitive(parent, "Fringe_Left", PrimitiveType.Cube, new Vector3(-0.12f, 1.79f, 0.19f), new Vector3(12f, 0f, -18f), new Vector3(0.16f, 0.16f, 0.07f), hairMat);
        CreatePrimitive(parent, "Fringe_Center", PrimitiveType.Cube, new Vector3(0f, 1.80f, 0.205f), new Vector3(18f, 0f, 0f), new Vector3(0.14f, 0.15f, 0.06f), hairMat);
        CreatePrimitive(parent, "Fringe_Right", PrimitiveType.Cube, new Vector3(0.12f, 1.79f, 0.19f), new Vector3(12f, 0f, 18f), new Vector3(0.16f, 0.16f, 0.07f), hairMat);
    }

    private static void BuildArms(Transform parent, Material shirtMat, Material skinMat)
    {
        CreatePrimitive(parent, "UpperArm_Left", PrimitiveType.Capsule, new Vector3(-0.34f, 1.21f, 0f), new Vector3(0f, 0f, -8f), new Vector3(0.13f, 0.27f, 0.13f), shirtMat);
        CreatePrimitive(parent, "UpperArm_Right", PrimitiveType.Capsule, new Vector3(0.34f, 1.21f, 0f), new Vector3(0f, 0f, 8f), new Vector3(0.13f, 0.27f, 0.13f), shirtMat);
        CreatePrimitive(parent, "Forearm_Left", PrimitiveType.Capsule, new Vector3(-0.39f, 0.91f, 0.015f), new Vector3(0f, 0f, -5f), new Vector3(0.105f, 0.23f, 0.105f), skinMat);
        CreatePrimitive(parent, "Forearm_Right", PrimitiveType.Capsule, new Vector3(0.39f, 0.91f, 0.015f), new Vector3(0f, 0f, 5f), new Vector3(0.105f, 0.23f, 0.105f), skinMat);
        CreatePrimitive(parent, "Hand_Left", PrimitiveType.Sphere, new Vector3(-0.41f, 0.69f, 0.02f), Vector3.zero, new Vector3(0.11f, 0.14f, 0.10f), skinMat);
        CreatePrimitive(parent, "Hand_Right", PrimitiveType.Sphere, new Vector3(0.41f, 0.69f, 0.02f), Vector3.zero, new Vector3(0.11f, 0.14f, 0.10f), skinMat);
    }

    private static void BuildLegs(Transform parent, Material pantsMat)
    {
        CreatePrimitive(parent, "Leg_Left", PrimitiveType.Capsule, new Vector3(-0.14f, 0.52f, 0f), Vector3.zero, new Vector3(0.16f, 0.42f, 0.16f), pantsMat);
        CreatePrimitive(parent, "Leg_Right", PrimitiveType.Capsule, new Vector3(0.14f, 0.52f, 0f), Vector3.zero, new Vector3(0.16f, 0.42f, 0.16f), pantsMat);
    }

    private static void BuildShoes(Transform parent, Material shoeMat, Material soleMat)
    {
        CreatePrimitive(parent, "Shoe_Left", PrimitiveType.Cube, new Vector3(-0.14f, 0.10f, 0.07f), Vector3.zero, new Vector3(0.22f, 0.14f, 0.40f), shoeMat);
        CreatePrimitive(parent, "Shoe_Right", PrimitiveType.Cube, new Vector3(0.14f, 0.10f, 0.07f), Vector3.zero, new Vector3(0.22f, 0.14f, 0.40f), shoeMat);
        CreatePrimitive(parent, "Sole_Left", PrimitiveType.Cube, new Vector3(-0.14f, 0.035f, 0.08f), Vector3.zero, new Vector3(0.235f, 0.045f, 0.42f), soleMat);
        CreatePrimitive(parent, "Sole_Right", PrimitiveType.Cube, new Vector3(0.14f, 0.035f, 0.08f), Vector3.zero, new Vector3(0.235f, 0.045f, 0.42f), soleMat);
    }

    private static void BuildSatchel(Transform parent, Material bagMat, Material accentMat)
    {
        CreatePrimitive(parent, "Satchel", PrimitiveType.Cube, new Vector3(0.28f, 1.02f, -0.24f), new Vector3(0f, -8f, 0f), new Vector3(0.32f, 0.40f, 0.15f), bagMat);
        CreatePrimitive(parent, "Satchel_Flap", PrimitiveType.Cube, new Vector3(0.28f, 1.13f, -0.325f), new Vector3(0f, -8f, 0f), new Vector3(0.28f, 0.13f, 0.035f), accentMat);
        CreatePrimitive(parent, "Shoulder_Strap", PrimitiveType.Cube, new Vector3(0.02f, 1.23f, -0.20f), new Vector3(0f, 0f, -28f), new Vector3(0.055f, 0.70f, 0.035f), accentMat);
    }

    private static GameObject FindPlayer()
    {
        GameObject player = GameObject.Find("Player");
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player");
        return player;
    }

    private static void HideOldRootVisual(GameObject player)
    {
        Renderer rootRenderer = player.GetComponent<Renderer>();
        if (rootRenderer != null)
            rootRenderer.enabled = false;
    }

    private static Transform CreateGroup(Transform parent, string name)
    {
        GameObject group = new GameObject(name);
        group.transform.SetParent(parent, false);
        return group.transform;
    }

    private static GameObject CreatePrimitive(Transform parent, string name, PrimitiveType type, Vector3 localPosition, Vector3 localRotation, Vector3 localScale, Material material)
    {
        GameObject obj = GameObject.CreatePrimitive(type);
        obj.name = name;
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = localPosition;
        obj.transform.localEulerAngles = localRotation;
        obj.transform.localScale = localScale;

        Collider collider = obj.GetComponent<Collider>();
        if (collider != null)
            Object.DestroyImmediate(collider);

        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }

        return obj;
    }

    private static void EnsureMaterialFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");

        if (!AssetDatabase.IsValidFolder(MaterialFolder))
            AssetDatabase.CreateFolder("Assets/Materials", "GeneratedPlayerVisual");
    }

    private static Material GetOrCreateMaterial(string name, Color color, float smoothness)
    {
        string path = Path.Combine(MaterialFolder, name + ".mat").Replace("\\", "/");
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            material = new Material(shader)
            {
                name = name
            };
            AssetDatabase.CreateAsset(material, path);
        }

        material.color = color;

        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", smoothness);

        if (material.HasProperty("_Metallic"))
            material.SetFloat("_Metallic", 0f);

        EditorUtility.SetDirty(material);
        return material;
    }
}
