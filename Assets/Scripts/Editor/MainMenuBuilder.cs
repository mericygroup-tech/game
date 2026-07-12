using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MainMenuBuilder
{
    private const string ScenePath = "Assets/Scenes/MainMenu.unity";
    private const string StartScenePath = "Assets/Scenes/S01_CityPrototype.unity";
    private const string StartSceneName = "S01_CityPrototype";
    private const string RootName = "MainMenu_Generated";
    private const string MaterialFolder = "Assets/Materials/MainMenu";

    [MenuItem("Tools/Dong Chay Anh Hung/Rebuild Main Menu")]
    public static void BuildScene()
    {
        EnsureFolders();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject root = new GameObject(RootName);
        Material stoneMat = CreateMaterial("MainMenu_Stone_Dark", new Color32(25, 22, 18, 255), 0.1f);
        Material bronzeMat = CreateMaterial("MainMenu_Bronze_Gold", new Color32(169, 112, 42, 255), 0.35f, new Color(0.55f, 0.32f, 0.08f));
        Material redMat = CreateMaterial("MainMenu_Red_Cloth", new Color32(95, 20, 16, 255), 0.2f);
        Material emberMat = CreateMaterial("MainMenu_Ember_Glow", new Color32(255, 122, 36, 255), 0.1f, new Color(2.2f, 0.55f, 0.08f));

        SetupCameraAndLighting(root.transform);
        BuildBackdrop(root.transform, stoneMat, bronzeMat, redMat, emberMat);

        Canvas canvas = BuildCanvas(root.transform);
        EnsureEventSystem(root.transform);

        BuildMenuUI(
            canvas.transform,
            out CanvasGroup blackFade,
            out CanvasGroup logoGroup,
            out CanvasGroup menuGroup,
            out CanvasGroup footerGroup,
            out RectTransform swordRoot,
            out Button startButton,
            out Button settingsButton,
            out Button achievementsButton,
            out Button exitButton,
            out Button settingsCloseButton,
            out Button achievementsCloseButton,
            out GameObject settingsPanel,
            out GameObject achievementsPanel,
            out TMP_Text statusText,
            out TMP_Text versionText);

        GameObject controllerObject = new GameObject("MainMenu_Controller");
        controllerObject.transform.SetParent(root.transform, false);
        MainMenuController controller = controllerObject.AddComponent<MainMenuController>();
        controller.Configure(
            blackFade,
            logoGroup,
            menuGroup,
            footerGroup,
            swordRoot,
            startButton,
            settingsButton,
            achievementsButton,
            exitButton,
            settingsCloseButton,
            achievementsCloseButton,
            settingsPanel,
            achievementsPanel,
            statusText,
            versionText,
            StartSceneName);

        Selection.activeGameObject = root;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        EnsureBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Main Menu rebuilt. Start button loads " + StartSceneName + ".");
    }

    [MenuItem("Tools/Dong Chay Anh Hung/Verify Main Menu")]
    public static void VerifyScene()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        RequireObject(RootName);
        RequireObject("MainMenu_Canvas");
        RequireObject("MainMenu_StartButton");
        RequireObject("MainMenu_SettingsButton");
        RequireObject("MainMenu_AchievementsButton");
        RequireObject("MainMenu_ExitButton");

        if (Object.FindAnyObjectByType<MainMenuController>() == null)
            throw new UnityException("Main Menu verify failed: MainMenuController was not found.");

        if (Object.FindObjectsByType<MainMenuButtonFX>(FindObjectsInactive.Include).Length < 4)
            throw new UnityException("Main Menu verify failed: expected four menu button effects.");

        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        if (scenes.Length == 0 || scenes[0].path != ScenePath || !scenes[0].enabled)
            throw new UnityException("Main Menu verify failed: MainMenu.unity is not the first enabled Build Settings scene.");

        bool hasS01 = false;
        foreach (EditorBuildSettingsScene buildScene in scenes)
        {
            if (buildScene.path == StartScenePath && buildScene.enabled)
                hasS01 = true;
        }

        if (!hasS01)
            throw new UnityException("Main Menu verify failed: S01_CityPrototype is not enabled in Build Settings.");

        Debug.Log("Main Menu verification passed: scene, controller, buttons, and S01 build link are ready.");
    }

    private static void EnsureFolders()
    {
        if (!Directory.Exists("Assets/Scenes"))
            Directory.CreateDirectory("Assets/Scenes");

        if (!Directory.Exists("Assets/Materials"))
            Directory.CreateDirectory("Assets/Materials");

        if (!Directory.Exists(MaterialFolder))
            Directory.CreateDirectory(MaterialFolder);
    }

    private static void SetupCameraAndLighting(Transform parent)
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.transform.SetParent(parent, false);
        cameraObject.transform.position = new Vector3(0f, 3.1f, -9.2f);
        cameraObject.transform.rotation = Quaternion.Euler(15f, 0f, 0f);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color32(8, 8, 9, 255);
        camera.fieldOfView = 44f;
        cameraObject.AddComponent<AudioListener>();

        GameObject keyLightObject = new GameObject("MainMenu_KeyLight");
        keyLightObject.transform.SetParent(parent, false);
        keyLightObject.transform.rotation = Quaternion.Euler(42f, -32f, 0f);
        Light keyLight = keyLightObject.AddComponent<Light>();
        keyLight.type = LightType.Directional;
        keyLight.color = new Color32(255, 183, 95, 255);
        keyLight.intensity = 1.2f;

        GameObject rimLightObject = new GameObject("MainMenu_RimLight");
        rimLightObject.transform.SetParent(parent, false);
        rimLightObject.transform.position = new Vector3(0f, 3f, 4f);
        Light rimLight = rimLightObject.AddComponent<Light>();
        rimLight.type = LightType.Point;
        rimLight.color = new Color32(190, 58, 32, 255);
        rimLight.range = 11f;
        rimLight.intensity = 2.6f;

        RenderSettings.ambientLight = new Color32(27, 23, 22, 255);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color32(15, 13, 12, 255);
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.035f;
    }

    private static void BuildBackdrop(Transform parent, Material stoneMat, Material bronzeMat, Material redMat, Material emberMat)
    {
        GameObject backdropRoot = new GameObject("MainMenu_Backdrop");
        backdropRoot.transform.SetParent(parent, false);

        CreateCube("MainMenu_Ground", backdropRoot.transform, new Vector3(0f, -0.65f, 3.8f), new Vector3(24f, 0.3f, 16f), stoneMat);
        CreateCube("MainMenu_CoLoaWall_Left", backdropRoot.transform, new Vector3(-5.7f, 0f, 5.3f), new Vector3(6.4f, 1.7f, 0.6f), stoneMat);
        CreateCube("MainMenu_CoLoaWall_Right", backdropRoot.transform, new Vector3(5.7f, 0f, 5.3f), new Vector3(6.4f, 1.7f, 0.6f), stoneMat);
        CreateCube("MainMenu_Gate_LeftTower", backdropRoot.transform, new Vector3(-2.2f, 0.5f, 5.35f), new Vector3(1.2f, 3f, 0.9f), stoneMat);
        CreateCube("MainMenu_Gate_RightTower", backdropRoot.transform, new Vector3(2.2f, 0.5f, 5.35f), new Vector3(1.2f, 3f, 0.9f), stoneMat);
        CreateCube("MainMenu_Gate_Top", backdropRoot.transform, new Vector3(0f, 2.15f, 5.35f), new Vector3(5.3f, 0.55f, 0.9f), stoneMat);
        CreateCube("MainMenu_Gate_GoldLine", backdropRoot.transform, new Vector3(0f, 2.5f, 5f), new Vector3(5.6f, 0.08f, 0.12f), bronzeMat);

        for (int i = 0; i < 7; i++)
        {
            float x = -10.5f + i * 3.5f;
            float height = 1.8f + (i % 3) * 0.45f;
            CreateCube("MainMenu_Mountain_" + i, backdropRoot.transform, new Vector3(x, -0.05f, 9.5f), new Vector3(2.8f, height, 1.4f), stoneMat);
        }

        CreateBanner(backdropRoot.transform, new Vector3(4.8f, 2.25f, 4.55f), redMat, bronzeMat, -9f);
        CreateBanner(backdropRoot.transform, new Vector3(-4.8f, 2.05f, 4.65f), redMat, bronzeMat, 8f);
        CreateEmbers(backdropRoot.transform, emberMat);
    }

    private static Canvas BuildCanvas(Transform parent)
    {
        GameObject canvasObject = new GameObject("MainMenu_Canvas");
        canvasObject.transform.SetParent(parent, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static void BuildMenuUI(
        Transform parent,
        out CanvasGroup blackFade,
        out CanvasGroup logoGroup,
        out CanvasGroup menuGroup,
        out CanvasGroup footerGroup,
        out RectTransform swordRoot,
        out Button startButton,
        out Button settingsButton,
        out Button achievementsButton,
        out Button exitButton,
        out Button settingsCloseButton,
        out Button achievementsCloseButton,
        out GameObject settingsPanel,
        out GameObject achievementsPanel,
        out TMP_Text statusText,
        out TMP_Text versionText)
    {
        CreateFullScreenImage("MainMenu_Vignette", parent, new Color32(0, 0, 0, 92), false);
        CreateFullScreenImage("MainMenu_WarmSmokeOverlay", parent, new Color32(58, 29, 12, 38), false);

        GameObject logoObject = CreateUIObject("MainMenu_LogoGroup", parent, new Vector2(900f, 340f), new Vector2(0.5f, 0.66f), new Vector2(0f, 0f));
        logoGroup = logoObject.AddComponent<CanvasGroup>();
        swordRoot = CreateSword(logoObject.transform);

        TMP_Text title = CreateText("MainMenu_Title", logoObject.transform, "DÒNG CHẢY\nANH HÙNG", 86f, new Color32(230, 203, 146, 255), TextAlignmentOptions.Center, FontStyles.Bold);
        SetRect(title.rectTransform, new Vector2(760f, 190f), new Vector2(0.5f, 0.58f), new Vector2(42f, 16f));
        title.lineSpacing = -12f;
        title.outlineColor = new Color32(30, 18, 8, 255);
        title.outlineWidth = 0.18f;

        TMP_Text subtitle = CreateText("MainMenu_Subtitle", logoObject.transform, "Hành động lịch sử Việt Nam - Single Player", 25f, new Color32(198, 151, 70, 255), TextAlignmentOptions.Center, FontStyles.Normal);
        SetRect(subtitle.rectTransform, new Vector2(660f, 44f), new Vector2(0.5f, 0.16f), new Vector2(0f, 0f));

        GameObject menuObject = CreateUIObject("MainMenu_ButtonGroup", parent, new Vector2(440f, 315f), new Vector2(0.5f, 0.33f), new Vector2(0f, 0f));
        menuGroup = menuObject.AddComponent<CanvasGroup>();
        VerticalLayoutGroup layout = menuObject.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 13f;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        startButton = CreateMenuButton(menuObject.transform, "BẮT ĐẦU", true, "MainMenu_StartButton");
        settingsButton = CreateMenuButton(menuObject.transform, "CÀI ĐẶT", false, "MainMenu_SettingsButton");
        achievementsButton = CreateMenuButton(menuObject.transform, "THÀNH TỰU", false, "MainMenu_AchievementsButton");
        exitButton = CreateMenuButton(menuObject.transform, "THOÁT", false, "MainMenu_ExitButton");

        GameObject footerObject = CreateUIObject("MainMenu_Footer", parent, new Vector2(1840f, 120f), new Vector2(0.5f, 0.04f), Vector2.zero);
        footerGroup = footerObject.AddComponent<CanvasGroup>();
        statusText = CreateText("MainMenu_StatusText", footerObject.transform, "Chọn BẮT ĐẦU để vào game.", 21f, new Color32(220, 207, 183, 255), TextAlignmentOptions.Center, FontStyles.Normal);
        SetRect(statusText.rectTransform, new Vector2(1100f, 44f), new Vector2(0.5f, 0.52f), Vector2.zero);
        versionText = CreateText("MainMenu_VersionText", footerObject.transform, "v1.0.0", 18f, new Color32(181, 144, 83, 255), TextAlignmentOptions.Right, FontStyles.Normal);
        SetRect(versionText.rectTransform, new Vector2(220f, 38f), new Vector2(0.93f, 0.52f), Vector2.zero);

        settingsPanel = CreateInfoPanel(parent, "MainMenu_SettingsPanel", "CÀI ĐẶT", "Âm lượng, đồ họa, điều khiển bàn phím và tay cầm sẽ được nối trong bước sau.", out settingsCloseButton);
        achievementsPanel = CreateInfoPanel(parent, "MainMenu_AchievementsPanel", "THÀNH TỰU", "Khu vực này sẽ lưu tiến trình, các mốc vượt wave và thành tựu cốt truyện.", out achievementsCloseButton);

        blackFade = CreateFullScreenImage("MainMenu_BlackFade", parent, Color.black, false).AddComponent<CanvasGroup>();
    }

    private static Button CreateMenuButton(Transform parent, string labelText, bool primary, string name)
    {
        GameObject buttonObject = CreateUIObject(name, parent, new Vector2(390f, 58f), new Vector2(0.5f, 0.5f), Vector2.zero);
        LayoutElement layoutElement = buttonObject.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = 390f;
        layoutElement.preferredHeight = 58f;

        Image frame = buttonObject.AddComponent<Image>();
        frame.color = primary ? new Color32(102, 26, 22, 220) : new Color32(40, 31, 22, 205);
        Outline outline = buttonObject.AddComponent<Outline>();
        outline.effectColor = new Color32(181, 122, 45, 190);
        outline.effectDistance = new Vector2(2f, -2f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = frame;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.selectedColor = Color.white;
        colors.pressedColor = new Color32(225, 190, 120, 255);
        colors.colorMultiplier = 1f;
        button.colors = colors;

        GameObject glowObject = CreateStretchImage(name + "_Glow", buttonObject.transform, new Color32(255, 190, 70, 0), false);
        Image glow = glowObject.GetComponent<Image>();

        TMP_Text label = CreateText(name + "_Label", buttonObject.transform, labelText, 27f, new Color32(230, 205, 150, 255), TextAlignmentOptions.Center, FontStyles.Bold);
        Stretch(label.rectTransform);

        CreateDiamond(name + "_LeftDiamond", buttonObject.transform, new Vector2(-205f, 0f));
        CreateDiamond(name + "_RightDiamond", buttonObject.transform, new Vector2(205f, 0f));

        MainMenuButtonFX fx = buttonObject.AddComponent<MainMenuButtonFX>();
        fx.Configure(label, frame, glow, primary);
        return button;
    }

    private static GameObject CreateInfoPanel(Transform parent, string name, string titleText, string bodyText, out Button closeButton)
    {
        GameObject overlay = CreateFullScreenImage(name, parent, new Color32(0, 0, 0, 130), true);

        GameObject panel = CreateUIObject(name + "_Box", overlay.transform, new Vector2(650f, 330f), new Vector2(0.5f, 0.5f), Vector2.zero);
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color32(20, 17, 14, 242);
        Outline outline = panel.AddComponent<Outline>();
        outline.effectColor = new Color32(181, 122, 45, 210);
        outline.effectDistance = new Vector2(2f, -2f);

        TMP_Text title = CreateText(name + "_Title", panel.transform, titleText, 36f, new Color32(226, 180, 85, 255), TextAlignmentOptions.Center, FontStyles.Bold);
        SetRect(title.rectTransform, new Vector2(560f, 64f), new Vector2(0.5f, 0.78f), Vector2.zero);

        TMP_Text body = CreateText(name + "_Body", panel.transform, bodyText, 23f, new Color32(226, 218, 203, 255), TextAlignmentOptions.Center, FontStyles.Normal);
        SetRect(body.rectTransform, new Vector2(545f, 110f), new Vector2(0.5f, 0.49f), Vector2.zero);

        closeButton = CreateMenuButton(panel.transform, "ĐÓNG", false, name + "_CloseButton");
        RectTransform closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.5f, 0.17f);
        closeRect.anchorMax = new Vector2(0.5f, 0.17f);
        closeRect.anchoredPosition = Vector2.zero;
        closeRect.sizeDelta = new Vector2(220f, 54f);

        overlay.SetActive(false);
        return overlay;
    }

    private static RectTransform CreateSword(Transform parent)
    {
        GameObject sword = CreateUIObject("MainMenu_LogoSword", parent, new Vector2(120f, 310f), new Vector2(0.2f, 0.58f), new Vector2(8f, 2f));
        RectTransform root = sword.GetComponent<RectTransform>();

        CreateRectPart("Sword_Blade", sword.transform, new Vector2(13f, 212f), new Vector2(0.5f, 0.54f), new Vector2(0f, -12f), new Color32(218, 187, 124, 255));
        CreateRectPart("Sword_Guard", sword.transform, new Vector2(98f, 14f), new Vector2(0.5f, 0.76f), Vector2.zero, new Color32(168, 110, 38, 255));
        CreateRectPart("Sword_Hilt", sword.transform, new Vector2(20f, 74f), new Vector2(0.5f, 0.91f), new Vector2(0f, -8f), new Color32(72, 43, 24, 255));
        CreateRectPart("Sword_Pommel", sword.transform, new Vector2(32f, 32f), new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Color32(176, 121, 48, 255));

        RectTransform ribbonA = CreateRectPart("Sword_Ribbon_A", sword.transform, new Vector2(145f, 14f), new Vector2(0.75f, 0.88f), new Vector2(42f, -10f), new Color32(127, 22, 18, 220));
        ribbonA.localRotation = Quaternion.Euler(0f, 0f, -13f);
        RectTransform ribbonB = CreateRectPart("Sword_Ribbon_B", sword.transform, new Vector2(115f, 12f), new Vector2(0.8f, 0.83f), new Vector2(52f, -28f), new Color32(96, 18, 17, 210));
        ribbonB.localRotation = Quaternion.Euler(0f, 0f, 10f);

        return root;
    }

    private static RectTransform CreateRectPart(string name, Transform parent, Vector2 size, Vector2 anchor, Vector2 position, Color color)
    {
        GameObject part = CreateUIObject(name, parent, size, anchor, position);
        Image image = part.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return part.GetComponent<RectTransform>();
    }

    private static void CreateDiamond(string name, Transform parent, Vector2 position)
    {
        GameObject diamond = CreateUIObject(name, parent, new Vector2(16f, 16f), new Vector2(0.5f, 0.5f), position);
        diamond.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        Image image = diamond.AddComponent<Image>();
        image.color = new Color32(178, 116, 42, 230);
        image.raycastTarget = false;
    }

    private static GameObject CreateFullScreenImage(string name, Transform parent, Color color, bool raycastTarget)
    {
        GameObject obj = CreateUIObject(name, parent, Vector2.zero, Vector2.zero, Vector2.zero);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = obj.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = raycastTarget;
        return obj;
    }

    private static GameObject CreateStretchImage(string name, Transform parent, Color color, bool raycastTarget)
    {
        GameObject obj = CreateUIObject(name, parent, Vector2.zero, Vector2.zero, Vector2.zero);
        RectTransform rect = obj.GetComponent<RectTransform>();
        Stretch(rect);

        Image image = obj.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = raycastTarget;
        return obj;
    }

    private static TMP_Text CreateText(string name, Transform parent, string text, float size, Color color, TextAlignmentOptions alignment, FontStyles fontStyle)
    {
        GameObject obj = CreateUIObject(name, parent, new Vector2(420f, 80f), new Vector2(0.5f, 0.5f), Vector2.zero);
        TextMeshProUGUI label = obj.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.color = color;
        label.alignment = alignment;
        label.fontStyle = fontStyle;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.raycastTarget = false;
        return label;
    }

    private static GameObject CreateUIObject(string name, Transform parent, Vector2 size, Vector2 anchor, Vector2 anchoredPosition)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        SetRect(rect, size, anchor, anchoredPosition);
        return obj;
    }

    private static void SetRect(RectTransform rect, Vector2 size, Vector2 anchor, Vector2 anchoredPosition)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void EnsureEventSystem(Transform parent)
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null)
            return;

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.transform.SetParent(parent, false);
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    private static Material CreateMaterial(string assetName, Color color, float smoothness, Color? emission = null)
    {
        string path = MaterialFolder + "/" + assetName + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        else if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);

        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", smoothness);

        if (emission.HasValue)
        {
            material.EnableKeyword("_EMISSION");
            if (material.HasProperty("_EmissionColor"))
                material.SetColor("_EmissionColor", emission.Value);
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    private static void CreateCube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = position;
        cube.transform.localScale = scale;
        Renderer renderer = cube.GetComponent<Renderer>();
        renderer.sharedMaterial = material;

        Collider collider = cube.GetComponent<Collider>();
        if (collider != null)
            Object.DestroyImmediate(collider);
    }

    private static void CreateBanner(Transform parent, Vector3 position, Material clothMat, Material poleMat, float angle)
    {
        GameObject bannerRoot = new GameObject("MainMenu_Banner");
        bannerRoot.transform.SetParent(parent, false);
        bannerRoot.transform.localPosition = position;
        bannerRoot.transform.localRotation = Quaternion.Euler(0f, 0f, angle);

        CreateCube("Banner_Pole", bannerRoot.transform, Vector3.zero, new Vector3(0.08f, 2.4f, 0.08f), poleMat);
        CreateCube("Banner_Cloth", bannerRoot.transform, new Vector3(0.38f, 0.4f, 0f), new Vector3(0.8f, 1.15f, 0.05f), clothMat);
        CreateCube("Banner_BottomTear", bannerRoot.transform, new Vector3(0.38f, -0.28f, 0f), new Vector3(0.5f, 0.32f, 0.05f), clothMat);
    }

    private static void CreateEmbers(Transform parent, Material emberMat)
    {
        GameObject embers = new GameObject("MainMenu_Embers");
        embers.transform.SetParent(parent, false);
        embers.transform.localPosition = new Vector3(0f, 0.2f, 1f);

        ParticleSystem particles = embers.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 5.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.35f, 1.05f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.09f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color32(255, 117, 38, 180), new Color32(255, 197, 88, 230));
        main.maxParticles = 180;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 24f;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(14f, 0.25f, 4f);

        ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.y = new ParticleSystem.MinMaxCurve(0.35f, 0.8f);
        velocity.x = new ParticleSystem.MinMaxCurve(-0.15f, 0.15f);

        ParticleSystemRenderer renderer = embers.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = emberMat;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
    }

    private static void EnsureBuildSettings()
    {
        string[] priorityScenes =
        {
            ScenePath,
            StartScenePath,
            "Assets/Scenes/S02_UndergroundCave.unity",
            "Assets/Scenes/S03.unity"
        };

        List<EditorBuildSettingsScene> finalScenes = new List<EditorBuildSettingsScene>();
        HashSet<string> added = new HashSet<string>();

        foreach (string path in priorityScenes)
        {
            if (!File.Exists(path))
                continue;

            finalScenes.Add(new EditorBuildSettingsScene(path, true));
            added.Add(path);
        }

        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (added.Contains(scene.path) || !File.Exists(scene.path))
                continue;

            finalScenes.Add(scene);
            added.Add(scene.path);
        }

        EditorBuildSettings.scenes = finalScenes.ToArray();
    }

    private static void RequireObject(string name)
    {
        if (GameObject.Find(name) == null)
            throw new UnityException("Main Menu verify failed: missing object " + name + ".");
    }
}
