using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MainMenuBuilder
{
    private const string ScenePath = "Assets/Scenes/MainMenu.unity";
    private const string StartScenePath = "Assets/Scenes/S01.unity";
    private const string StartSceneName = "S01";
    private const string RootName = "MainMenu_Generated";
    private const string MaterialFolder = "Assets/Materials/MainMenu";
    private const string ExactArtworkPath = "Assets/Art/UI/MainMenu/MainMenu_ExactArtwork.png";

    [MenuItem("Tools/Dong Chay Anh Hung/Rebuild Main Menu")]
    public static void BuildScene()
    {
        EnsureFolders();
        ConfigureExactArtworkImporter();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject root = new GameObject(RootName);
        SetupExactArtworkCamera(root.transform);

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
        RequireObject("MainMenu_ExactArtwork");
        RequireObject("MainMenu_MasterVolumeSlider");

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
            throw new UnityException("Main Menu verify failed: S01 is not enabled in Build Settings.");

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
        camera.backgroundColor = new Color32(11, 9, 8, 255);
        camera.fieldOfView = 44f;
        cameraObject.AddComponent<AudioListener>();

        GameObject keyLightObject = new GameObject("MainMenu_KeyLight");
        keyLightObject.transform.SetParent(parent, false);
        keyLightObject.transform.rotation = Quaternion.Euler(42f, -32f, 0f);
        Light keyLight = keyLightObject.AddComponent<Light>();
        keyLight.type = LightType.Directional;
        keyLight.color = new Color32(255, 183, 95, 255);
        keyLight.intensity = 1.55f;

        GameObject rimLightObject = new GameObject("MainMenu_RimLight");
        rimLightObject.transform.SetParent(parent, false);
        rimLightObject.transform.position = new Vector3(0f, 3f, 4f);
        Light rimLight = rimLightObject.AddComponent<Light>();
        rimLight.type = LightType.Point;
        rimLight.color = new Color32(190, 58, 32, 255);
        rimLight.range = 11f;
        rimLight.intensity = 2.6f;

        RenderSettings.ambientLight = new Color32(38, 31, 26, 255);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color32(15, 13, 12, 255);
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.025f;
    }

    private static void BuildBackdrop(Transform parent, Material stoneMat, Material bronzeMat, Material redMat, Material emberMat)
    {
        GameObject backdropRoot = new GameObject("MainMenu_Backdrop");
        backdropRoot.transform.SetParent(parent, false);

        CreateCube("MainMenu_Ground", backdropRoot.transform, new Vector3(0f, -0.85f, 4.5f), new Vector3(28f, 0.35f, 18f), stoneMat);

        // Three staggered rings evoke the spiral defensive walls of ancient Co Loa.
        for (int ring = 0; ring < 3; ring++)
        {
            float z = 8.8f + ring * 1.65f;
            float y = -0.08f + ring * 0.28f;
            float width = 20f - ring * 3.2f;
            CreateCube("MainMenu_WallRing_" + ring, backdropRoot.transform, new Vector3(0f, y, z), new Vector3(width, 1.15f + ring * 0.18f, 0.72f), stoneMat);
            CreateBattlements(backdropRoot.transform, "MainMenu_Battlement_" + ring, width, y + 0.78f, z - 0.2f, stoneMat);
        }

        CreateCube("MainMenu_CoLoaWall_Left", backdropRoot.transform, new Vector3(-6.2f, 0.2f, 5.55f), new Vector3(7.6f, 2.15f, 0.72f), stoneMat);
        CreateCube("MainMenu_CoLoaWall_Right", backdropRoot.transform, new Vector3(6.2f, 0.2f, 5.55f), new Vector3(7.6f, 2.15f, 0.72f), stoneMat);
        CreateBattlements(backdropRoot.transform, "MainMenu_FrontCrenel", 15.8f, 1.48f, 5.28f, stoneMat);

        CreateCube("MainMenu_Gate_LeftTower", backdropRoot.transform, new Vector3(-2.35f, 0.85f, 5.22f), new Vector3(1.55f, 4.05f, 1.35f), stoneMat);
        CreateCube("MainMenu_Gate_RightTower", backdropRoot.transform, new Vector3(2.35f, 0.85f, 5.22f), new Vector3(1.55f, 4.05f, 1.35f), stoneMat);
        CreateCube("MainMenu_Gate_Top", backdropRoot.transform, new Vector3(0f, 2.48f, 5.2f), new Vector3(5.9f, 0.72f, 1.28f), stoneMat);
        CreateCube("MainMenu_Gate_GoldLine", backdropRoot.transform, new Vector3(0f, 2.86f, 4.52f), new Vector3(6.2f, 0.09f, 0.12f), bronzeMat);
        CreateCube("MainMenu_GateDoor", backdropRoot.transform, new Vector3(0f, 0.15f, 4.84f), new Vector3(2.7f, 3.75f, 0.22f), redMat);
        CreateCylinder("MainMenu_GateDrum", backdropRoot.transform, new Vector3(0f, 2.46f, 4.42f), new Vector3(0.62f, 0.14f, 0.62f), bronzeMat, new Vector3(90f, 0f, 0f));

        // Mountain silhouettes create depth and frame the logo without visual clutter.
        for (int i = 0; i < 11; i++)
        {
            float x = -13f + i * 2.6f;
            float height = 2.2f + (i % 4) * 0.65f;
            GameObject mountain = CreateCube("MainMenu_Mountain_" + i, backdropRoot.transform, new Vector3(x, 0.1f, 13.4f), new Vector3(2.7f, height, 1.6f), stoneMat);
            mountain.transform.localRotation = Quaternion.Euler(0f, 0f, i % 2 == 0 ? 18f : -16f);
        }

        CreateBanner(backdropRoot.transform, new Vector3(5.4f, 2.7f, 4.35f), redMat, bronzeMat, -8f);
        CreateBanner(backdropRoot.transform, new Vector3(-5.4f, 2.55f, 4.5f), redMat, bronzeMat, 7f);
        CreateBrazier(backdropRoot.transform, new Vector3(-3.7f, 0.25f, 3.85f), bronzeMat, emberMat);
        CreateBrazier(backdropRoot.transform, new Vector3(3.7f, 0.25f, 3.85f), bronzeMat, emberMat);
        CreateEmbers(backdropRoot.transform, emberMat);
    }

    private static void ConfigureExactArtworkImporter()
    {
        TextureImporter importer = AssetImporter.GetAtPath(ExactArtworkPath) as TextureImporter;
        if (importer == null)
            throw new UnityException("Main Menu artwork importer is missing: " + ExactArtworkPath);

        bool changed =
            importer.mipmapEnabled ||
            importer.npotScale != TextureImporterNPOTScale.None ||
            importer.textureCompression != TextureImporterCompression.Uncompressed ||
            importer.filterMode != FilterMode.Bilinear ||
            importer.wrapMode != TextureWrapMode.Clamp ||
            importer.maxTextureSize < 2048;

        importer.textureType = TextureImporterType.Default;
        importer.sRGBTexture = true;
        importer.mipmapEnabled = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.maxTextureSize = 2048;

        if (changed)
            importer.SaveAndReimport();
    }

    private static void SetupExactArtworkCamera(Transform parent)
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.transform.SetParent(parent, false);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.cullingMask = 0;
        cameraObject.AddComponent<AudioListener>();

        RenderSettings.fog = false;
        RenderSettings.ambientLight = Color.black;
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
        Texture2D artwork = AssetDatabase.LoadAssetAtPath<Texture2D>(ExactArtworkPath);
        if (artwork == null)
            throw new UnityException("Main Menu artwork is missing: " + ExactArtworkPath);

        CreateFullScreenImage("MainMenu_Letterbox", parent, Color.black, false);

        GameObject artObject = CreateUIObject("MainMenu_ExactArtwork", parent, new Vector2(742f, 541f), new Vector2(0.5f, 0.5f), Vector2.zero);
        RawImage artImage = artObject.AddComponent<RawImage>();
        artImage.texture = artwork;
        artImage.color = Color.white;
        artImage.raycastTarget = false;

        AspectRatioFitter fitter = artObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = 742f / 541f;
        logoGroup = artObject.AddComponent<CanvasGroup>();

        GameObject hitboxRoot = CreateUIObject("MainMenu_ArtworkHitboxes", artObject.transform, Vector2.zero, Vector2.zero, Vector2.zero);
        Stretch(hitboxRoot.GetComponent<RectTransform>());
        menuGroup = hitboxRoot.AddComponent<CanvasGroup>();

        Vector2 artSize = new Vector2(742f, 541f);
        startButton = CreateArtworkButton(hitboxRoot.transform, "MainMenu_StartButton", new Rect(273f, 350f, 199f, 33f), artSize);
        settingsButton = CreateArtworkButton(hitboxRoot.transform, "MainMenu_SettingsButton", new Rect(274f, 389f, 197f, 35f), artSize);
        achievementsButton = CreateArtworkButton(hitboxRoot.transform, "MainMenu_AchievementsButton", new Rect(274f, 429f, 197f, 35f), artSize);
        exitButton = CreateArtworkButton(hitboxRoot.transform, "MainMenu_ExitButton", new Rect(274f, 469f, 197f, 35f), artSize);

        GameObject footerObject = CreateUIObject("MainMenu_Footer", parent, Vector2.zero, Vector2.zero, Vector2.zero);
        Stretch(footerObject.GetComponent<RectTransform>());
        footerGroup = footerObject.AddComponent<CanvasGroup>();
        footerObject.GetComponent<RectTransform>().SetAsFirstSibling();

        swordRoot = null;
        statusText = null;
        versionText = null;

        settingsPanel = CreateVolumeSettingsPanel(parent, out settingsCloseButton);
        achievementsPanel = CreateEpicInfoPanel(
            parent,
            "MainMenu_AchievementsPanel",
            "THÀNH TỰU",
            "DẤU ẤN NGƯỜI ANH HÙNG",
            "Tiến trình chiến đấu và các cột mốc cốt truyện sẽ xuất hiện tại đây.",
            out achievementsCloseButton);

        blackFade = CreateFullScreenImage("MainMenu_BlackFade", parent, Color.black, false).AddComponent<CanvasGroup>();
    }

    private static void BuildGeneratedMenuUI(
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
        // Layered translucent panels give a cinematic vignette without requiring a large bitmap.
        CreateFullScreenImage("MainMenu_AshWash", parent, new Color32(8, 7, 6, 74), false);
        CreateFullScreenImage("MainMenu_WarmHaze", parent, new Color32(93, 43, 14, 25), false);
        CreateEdgeShade(parent, true);
        CreateEdgeShade(parent, false);

        GameObject topRule = CreateUIObject("MainMenu_TopRule", parent, new Vector2(920f, 2f), new Vector2(0.5f, 0.955f), Vector2.zero);
        topRule.AddComponent<Image>().color = new Color32(170, 112, 42, 105);
        TMP_Text chapter = CreateText("MainMenu_Chapter", parent, "ĐẠI VIỆT · THỜI ĐẠI ANH HÙNG", 18f, new Color32(190, 148, 76, 245), TextAlignmentOptions.Center, FontStyles.Bold);
        SetRect(chapter.rectTransform, new Vector2(620f, 34f), new Vector2(0.5f, 0.928f), Vector2.zero);
        chapter.characterSpacing = 7f;

        GameObject logoObject = CreateUIObject("MainMenu_LogoGroup", parent, new Vector2(1050f, 430f), new Vector2(0.5f, 0.69f), Vector2.zero);
        logoGroup = logoObject.AddComponent<CanvasGroup>();
        CreateLogoOrnaments(logoObject.transform);
        swordRoot = CreateEpicSword(logoObject.transform);

        TMP_Text titleShadow = CreateText("MainMenu_TitleShadow", logoObject.transform, "DÒNG CHẢY\nANH HÙNG", 99f, new Color32(19, 10, 5, 245), TextAlignmentOptions.Center, FontStyles.Bold);
        SetRect(titleShadow.rectTransform, new Vector2(900f, 235f), new Vector2(0.5f, 0.53f), new Vector2(29f, -5f));
        titleShadow.lineSpacing = -18f;
        titleShadow.characterSpacing = -1.5f;

        TMP_Text title = CreateText("MainMenu_Title", logoObject.transform, "DÒNG CHẢY\nANH HÙNG", 96f, new Color32(225, 195, 137, 255), TextAlignmentOptions.Center, FontStyles.Bold);
        SetRect(title.rectTransform, new Vector2(900f, 235f), new Vector2(0.5f, 0.54f), new Vector2(24f, 4f));
        title.lineSpacing = -18f;
        title.characterSpacing = -1.5f;
        title.outlineColor = new Color32(51, 27, 10, 255);
        title.outlineWidth = 0.22f;

        CreateRectPart("MainMenu_TitleGlint", logoObject.transform, new Vector2(420f, 2f), new Vector2(0.5f, 0.79f), new Vector2(22f, 0f), new Color32(255, 220, 145, 95));

        TMP_Text subtitle = CreateText("MainMenu_Subtitle", logoObject.transform, "TRUYỀN THUYẾT CỔ LOA", 20f, new Color32(188, 128, 53, 255), TextAlignmentOptions.Center, FontStyles.Bold);
        SetRect(subtitle.rectTransform, new Vector2(520f, 38f), new Vector2(0.5f, 0.13f), new Vector2(20f, 0f));
        subtitle.characterSpacing = 10f;

        GameObject menuObject = CreateUIObject("MainMenu_ButtonGroup", parent, new Vector2(500f, 300f), new Vector2(0.5f, 0.315f), Vector2.zero);
        menuGroup = menuObject.AddComponent<CanvasGroup>();
        VerticalLayoutGroup layout = menuObject.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 10f;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        startButton = CreateEpicMenuButton(menuObject.transform, "BẮT ĐẦU", "01", true, "MainMenu_StartButton");
        settingsButton = CreateEpicMenuButton(menuObject.transform, "CÀI ĐẶT", "02", false, "MainMenu_SettingsButton");
        achievementsButton = CreateEpicMenuButton(menuObject.transform, "THÀNH TỰU", "03", false, "MainMenu_AchievementsButton");
        exitButton = CreateEpicMenuButton(menuObject.transform, "THOÁT", "04", false, "MainMenu_ExitButton");

        TMP_Text inputHint = CreateText("MainMenu_InputHint", parent, "W S / ↑ ↓  DI CHUYỂN     ENTER  XÁC NHẬN", 15f, new Color32(166, 145, 112, 210), TextAlignmentOptions.Center, FontStyles.Normal);
        SetRect(inputHint.rectTransform, new Vector2(650f, 30f), new Vector2(0.5f, 0.118f), Vector2.zero);
        inputHint.characterSpacing = 2.5f;

        GameObject footerObject = CreateUIObject("MainMenu_Footer", parent, new Vector2(1840f, 76f), new Vector2(0.5f, 0.045f), Vector2.zero);
        footerGroup = footerObject.AddComponent<CanvasGroup>();
        GameObject footerRule = CreateUIObject("MainMenu_FooterRule", footerObject.transform, new Vector2(1800f, 1f), new Vector2(0.5f, 0.86f), Vector2.zero);
        footerRule.AddComponent<Image>().color = new Color32(169, 112, 42, 90);
        statusText = CreateText("MainMenu_StatusText", footerObject.transform, "Chọn BẮT ĐẦU để bước vào dòng chảy lịch sử.", 18f, new Color32(213, 199, 173, 235), TextAlignmentOptions.Left, FontStyles.Normal);
        SetRect(statusText.rectTransform, new Vector2(850f, 34f), new Vector2(0.27f, 0.42f), Vector2.zero);
        versionText = CreateText("MainMenu_VersionText", footerObject.transform, "v1.0.0", 16f, new Color32(181, 144, 83, 230), TextAlignmentOptions.Right, FontStyles.Normal);
        SetRect(versionText.rectTransform, new Vector2(240f, 34f), new Vector2(0.94f, 0.42f), Vector2.zero);

        settingsPanel = CreateEpicInfoPanel(parent, "MainMenu_SettingsPanel", "CÀI ĐẶT", "ÂM THANH  ·  HÌNH ẢNH  ·  ĐIỀU KHIỂN", "Các tùy chọn nâng cao sẽ được mở ở bản cập nhật tiếp theo.", out settingsCloseButton);
        achievementsPanel = CreateEpicInfoPanel(parent, "MainMenu_AchievementsPanel", "THÀNH TỰU", "DẤU ẤN NGƯỜI ANH HÙNG", "Tiến trình chiến đấu và các cột mốc cốt truyện sẽ xuất hiện tại đây.", out achievementsCloseButton);

        blackFade = CreateFullScreenImage("MainMenu_BlackFade", parent, Color.black, false).AddComponent<CanvasGroup>();
    }

    private static void BuildLegacyMenuUI(
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

    private static Button CreateArtworkButton(Transform parent, string name, Rect pixelRect, Vector2 artworkSize)
    {
        GameObject buttonObject = CreateUIObject(name, parent, Vector2.zero, Vector2.zero, Vector2.zero);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(pixelRect.xMin / artworkSize.x, 1f - pixelRect.yMax / artworkSize.y);
        rect.anchorMax = new Vector2(pixelRect.xMax / artworkSize.x, 1f - pixelRect.yMin / artworkSize.y);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image hitTarget = buttonObject.AddComponent<Image>();
        // Alpha zero lets CanvasRenderer cull the mesh, which also removes it
        // from GraphicRaycaster in Unity 6000.5. One byte of alpha is visually
        // imperceptible but keeps the hit target alive and clickable.
        hitTarget.color = new Color32(255, 255, 255, 1);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = hitTarget;
        button.transition = Selectable.Transition.None;

        CanvasGroup highlight = CreateArtworkButtonHighlight(buttonObject.transform, name + "_Highlight");
        MainMenuButtonFX fx = buttonObject.AddComponent<MainMenuButtonFX>();
        fx.ConfigureArtwork(highlight);
        return button;
    }

    private static CanvasGroup CreateArtworkButtonHighlight(Transform parent, string name)
    {
        GameObject highlightObject = CreateUIObject(name, parent, Vector2.zero, Vector2.zero, Vector2.zero);
        RectTransform highlightRect = highlightObject.GetComponent<RectTransform>();
        highlightRect.anchorMin = Vector2.zero;
        highlightRect.anchorMax = Vector2.one;
        highlightRect.offsetMin = new Vector2(12f, 4f);
        highlightRect.offsetMax = new Vector2(-12f, -4f);

        Image fill = highlightObject.AddComponent<Image>();
        fill.color = new Color32(170, 43, 27, 170);
        fill.raycastTarget = false;

        CreateArtworkHighlightRail(highlightObject.transform, "TopRail", true);
        CreateArtworkHighlightRail(highlightObject.transform, "BottomRail", false);

        CanvasGroup group = highlightObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
        return group;
    }

    private static void CreateArtworkHighlightRail(Transform parent, string name, bool top)
    {
        GameObject railObject = CreateUIObject(name, parent, Vector2.zero, Vector2.zero, Vector2.zero);
        RectTransform railRect = railObject.GetComponent<RectTransform>();
        float anchorY = top ? 1f : 0f;
        railRect.anchorMin = new Vector2(0.08f, anchorY);
        railRect.anchorMax = new Vector2(0.92f, anchorY);
        railRect.pivot = new Vector2(0.5f, anchorY);
        railRect.anchoredPosition = Vector2.zero;
        railRect.sizeDelta = new Vector2(0f, 1.5f);

        Image rail = railObject.AddComponent<Image>();
        rail.color = new Color32(238, 186, 88, 235);
        rail.raycastTarget = false;
    }

    private static GameObject CreateVolumeSettingsPanel(Transform parent, out Button closeButton)
    {
        GameObject overlay = CreateFullScreenImage("MainMenu_SettingsPanel", parent, new Color32(0, 0, 0, 196), true);
        GameObject panel = CreateUIObject("MainMenu_SettingsPanel_Box", overlay.transform, new Vector2(760f, 400f), new Vector2(0.5f, 0.5f), Vector2.zero);
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color32(17, 14, 11, 250);

        Outline outline = panel.AddComponent<Outline>();
        outline.effectColor = new Color32(188, 128, 48, 235);
        outline.effectDistance = new Vector2(2f, -2f);

        CreateRectPart("MainMenu_SettingsHeader", panel.transform, new Vector2(714f, 78f), new Vector2(0.5f, 0.84f), Vector2.zero, new Color32(91, 24, 18, 230));
        TMP_Text title = CreateText("MainMenu_SettingsTitle", panel.transform, "CÀI ĐẶT", 38f, new Color32(238, 201, 121, 255), TextAlignmentOptions.Center, FontStyles.Bold);
        SetRect(title.rectTransform, new Vector2(640f, 60f), new Vector2(0.5f, 0.845f), Vector2.zero);
        title.characterSpacing = 5f;

        TMP_Text label = CreateText("MainMenu_MasterVolumeLabel", panel.transform, "ÂM LƯỢNG TỔNG", 21f, new Color32(224, 204, 164, 255), TextAlignmentOptions.Left, FontStyles.Bold);
        SetRect(label.rectTransform, new Vector2(360f, 38f), new Vector2(0.34f, 0.61f), Vector2.zero);
        label.characterSpacing = 2f;

        TMP_Text valueText = CreateText("MainMenu_MasterVolumeValue", panel.transform, "100%", 22f, new Color32(230, 173, 76, 255), TextAlignmentOptions.Right, FontStyles.Bold);
        SetRect(valueText.rectTransform, new Vector2(150f, 38f), new Vector2(0.78f, 0.61f), Vector2.zero);

        Slider slider = CreateMasterVolumeSlider(panel.transform, valueText);
        slider.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0.45f);
        slider.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.45f);

        TMP_Text hint = CreateText("MainMenu_MasterVolumeHint", panel.transform, "Kéo thanh hoặc dùng phím ← → để điều chỉnh âm lượng trong game", 17f, new Color32(174, 153, 120, 230), TextAlignmentOptions.Center, FontStyles.Normal);
        SetRect(hint.rectTransform, new Vector2(650f, 36f), new Vector2(0.5f, 0.31f), Vector2.zero);

        closeButton = CreateEpicMenuButton(panel.transform, "ĐÓNG", "ESC", false, "MainMenu_SettingsPanel_CloseButton");
        RectTransform closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.5f, 0.13f);
        closeRect.anchorMax = new Vector2(0.5f, 0.13f);
        closeRect.anchoredPosition = Vector2.zero;
        closeRect.sizeDelta = new Vector2(300f, 56f);

        overlay.SetActive(false);
        return overlay;
    }

    private static Slider CreateMasterVolumeSlider(Transform parent, TMP_Text valueText)
    {
        GameObject sliderObject = CreateUIObject("MainMenu_MasterVolumeSlider", parent, new Vector2(580f, 58f), new Vector2(0.5f, 0.5f), Vector2.zero);
        Slider slider = sliderObject.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        slider.wholeNumbers = false;
        slider.direction = Slider.Direction.LeftToRight;
        slider.transition = Selectable.Transition.ColorTint;

        Image background = CreateRectPart("Background", sliderObject.transform, new Vector2(520f, 14f), new Vector2(0.5f, 0.5f), Vector2.zero, new Color32(43, 31, 22, 255)).GetComponent<Image>();
        Outline backgroundOutline = background.gameObject.AddComponent<Outline>();
        backgroundOutline.effectColor = new Color32(139, 88, 35, 230);
        backgroundOutline.effectDistance = new Vector2(1f, -1f);

        GameObject fillArea = CreateUIObject("Fill Area", sliderObject.transform, new Vector2(510f, 10f), new Vector2(0.5f, 0.5f), Vector2.zero);
        Image fill = CreateStretchImage("Fill", fillArea.transform, new Color32(183, 61, 29, 255), false).GetComponent<Image>();

        GameObject handleArea = CreateUIObject("Handle Slide Area", sliderObject.transform, new Vector2(510f, 54f), new Vector2(0.5f, 0.5f), Vector2.zero);
        GameObject handleObject = CreateUIObject("Handle", handleArea.transform, new Vector2(30f, 44f), new Vector2(0.5f, 0.5f), Vector2.zero);
        Image handle = handleObject.AddComponent<Image>();
        handle.color = new Color32(221, 167, 72, 255);
        Outline handleOutline = handleObject.AddComponent<Outline>();
        handleOutline.effectColor = new Color32(72, 39, 17, 255);
        handleOutline.effectDistance = new Vector2(2f, -2f);

        slider.fillRect = fill.rectTransform;
        slider.handleRect = handleObject.GetComponent<RectTransform>();
        slider.targetGraphic = handle;

        sliderObject.AddComponent<AudioSliderFeedback>();
        MainMenuVolumeControl control = sliderObject.AddComponent<MainMenuVolumeControl>();
        control.Configure(valueText);
        UnityEventTools.AddPersistentListener(slider.onValueChanged, control.SetMasterVolume);
        return slider;
    }

    private static void CreateEdgeShade(Transform parent, bool left)
    {
        GameObject shade = CreateUIObject(left ? "MainMenu_LeftShade" : "MainMenu_RightShade", parent, new Vector2(420f, 1080f), new Vector2(left ? 0f : 1f, 0.5f), Vector2.zero);
        RectTransform rect = shade.GetComponent<RectTransform>();
        rect.pivot = new Vector2(left ? 0f : 1f, 0.5f);
        Image image = shade.AddComponent<Image>();
        image.color = new Color32(0, 0, 0, 132);
        image.raycastTarget = false;
    }

    private static void CreateLogoOrnaments(Transform parent)
    {
        RectTransform leftRule = CreateRectPart("Logo_LeftRule", parent, new Vector2(220f, 2f), new Vector2(0.5f, 0.13f), new Vector2(-310f, 0f), new Color32(168, 109, 39, 180));
        RectTransform rightRule = CreateRectPart("Logo_RightRule", parent, new Vector2(220f, 2f), new Vector2(0.5f, 0.13f), new Vector2(350f, 0f), new Color32(168, 109, 39, 180));
        CreateDiamond("Logo_LeftSeal", parent, new Vector2(-425f, -159f));
        CreateDiamond("Logo_RightSeal", parent, new Vector2(465f, -159f));
        leftRule.localRotation = Quaternion.Euler(0f, 0f, 0.25f);
        rightRule.localRotation = Quaternion.Euler(0f, 0f, -0.25f);

        GameObject seal = CreateUIObject("MainMenu_RedSeal", parent, new Vector2(46f, 54f), new Vector2(0.84f, 0.33f), Vector2.zero);
        Image sealImage = seal.AddComponent<Image>();
        sealImage.color = new Color32(122, 28, 20, 225);
        Outline outline = seal.AddComponent<Outline>();
        outline.effectColor = new Color32(208, 118, 54, 180);
        TMP_Text mark = CreateText("MainMenu_RedSealMark", seal.transform, "ẤN", 15f, new Color32(222, 174, 94, 255), TextAlignmentOptions.Center, FontStyles.Bold);
        Stretch(mark.rectTransform);
    }

    private static RectTransform CreateEpicSword(Transform parent)
    {
        GameObject sword = CreateUIObject("MainMenu_LogoSword", parent, new Vector2(175f, 410f), new Vector2(0.195f, 0.58f), new Vector2(8f, 20f));
        RectTransform root = sword.GetComponent<RectTransform>();

        RectTransform bladeShadow = CreateRectPart("Sword_BladeShadow", sword.transform, new Vector2(25f, 278f), new Vector2(0.5f, 0.43f), new Vector2(4f, -18f), new Color32(18, 10, 6, 220));
        bladeShadow.localRotation = Quaternion.Euler(0f, 0f, 1f);
        CreateRectPart("Sword_Blade", sword.transform, new Vector2(18f, 282f), new Vector2(0.5f, 0.43f), new Vector2(0f, -18f), new Color32(214, 183, 117, 255));
        CreateRectPart("Sword_BladeCore", sword.transform, new Vector2(4f, 264f), new Vector2(0.5f, 0.43f), new Vector2(-3f, -17f), new Color32(255, 230, 170, 210));
        RectTransform point = CreateRectPart("Sword_Point", sword.transform, new Vector2(19f, 19f), new Vector2(0.5f, 0.075f), new Vector2(0f, -3f), new Color32(214, 183, 117, 255));
        point.localRotation = Quaternion.Euler(0f, 0f, 45f);

        CreateRectPart("Sword_Guard", sword.transform, new Vector2(136f, 18f), new Vector2(0.5f, 0.77f), Vector2.zero, new Color32(171, 107, 35, 255));
        CreateDiamond("Sword_GuardGem", sword.transform, new Vector2(0f, 111f));
        RectTransform guardLeft = CreateRectPart("Sword_GuardLeft", sword.transform, new Vector2(58f, 13f), new Vector2(0.28f, 0.77f), new Vector2(-8f, -5f), new Color32(205, 145, 57, 255));
        guardLeft.localRotation = Quaternion.Euler(0f, 0f, 12f);
        RectTransform guardRight = CreateRectPart("Sword_GuardRight", sword.transform, new Vector2(58f, 13f), new Vector2(0.72f, 0.77f), new Vector2(8f, -5f), new Color32(205, 145, 57, 255));
        guardRight.localRotation = Quaternion.Euler(0f, 0f, -12f);

        CreateRectPart("Sword_Hilt", sword.transform, new Vector2(24f, 82f), new Vector2(0.5f, 0.9f), new Vector2(0f, -2f), new Color32(56, 31, 19, 255));
        for (int i = 0; i < 5; i++)
        {
            RectTransform wrap = CreateRectPart("Sword_HiltWrap_" + i, sword.transform, new Vector2(29f, 5f), new Vector2(0.5f, 0.835f), new Vector2(0f, i * 13f), new Color32(179, 119, 44, 255));
            wrap.localRotation = Quaternion.Euler(0f, 0f, -12f);
        }
        CreateRectPart("Sword_Pommel", sword.transform, new Vector2(38f, 38f), new Vector2(0.5f, 0.995f), new Vector2(0f, -10f), new Color32(184, 126, 47, 255));
        CreateDiamond("Sword_PommelGem", sword.transform, new Vector2(0f, 188f));

        RectTransform ribbonA = CreateRectPart("Sword_Ribbon_A", sword.transform, new Vector2(182f, 14f), new Vector2(0.78f, 0.89f), new Vector2(48f, -8f), new Color32(129, 22, 18, 225));
        ribbonA.localRotation = Quaternion.Euler(0f, 0f, -12f);
        RectTransform ribbonB = CreateRectPart("Sword_Ribbon_B", sword.transform, new Vector2(142f, 11f), new Vector2(0.83f, 0.84f), new Vector2(62f, -25f), new Color32(92, 17, 15, 215));
        ribbonB.localRotation = Quaternion.Euler(0f, 0f, 9f);
        return root;
    }

    private static Button CreateEpicMenuButton(Transform parent, string labelText, string index, bool primary, string name)
    {
        GameObject buttonObject = CreateUIObject(name, parent, new Vector2(470f, 60f), new Vector2(0.5f, 0.5f), Vector2.zero);
        LayoutElement layoutElement = buttonObject.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = 470f;
        layoutElement.preferredHeight = 60f;

        Image frame = buttonObject.AddComponent<Image>();
        frame.color = primary ? new Color32(104, 24, 19, 236) : new Color32(24, 20, 16, 226);
        Outline outline = buttonObject.AddComponent<Outline>();
        outline.effectColor = new Color32(181, 121, 43, 205);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = frame;
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color32(255, 238, 204, 255);
        colors.selectedColor = new Color32(255, 238, 204, 255);
        colors.pressedColor = new Color32(205, 150, 76, 255);
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        Image glow = CreateStretchImage(name + "_Glow", buttonObject.transform, new Color32(255, 151, 42, 0), false).GetComponent<Image>();
        CreateRectPart(name + "_TopRail", buttonObject.transform, new Vector2(382f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -3f), new Color32(216, 157, 69, 140));
        CreateRectPart(name + "_BottomRail", buttonObject.transform, new Vector2(382f, 1f), new Vector2(0.5f, 0f), new Vector2(0f, 3f), new Color32(103, 63, 26, 180));

        TMP_Text number = CreateText(name + "_Index", buttonObject.transform, index, 14f, new Color32(170, 122, 60, 220), TextAlignmentOptions.Center, FontStyles.Bold);
        SetRect(number.rectTransform, new Vector2(42f, 30f), new Vector2(0.12f, 0.5f), Vector2.zero);
        TMP_Text label = CreateText(name + "_Label", buttonObject.transform, labelText, 25f, new Color32(230, 205, 151, 255), TextAlignmentOptions.Center, FontStyles.Bold);
        SetRect(label.rectTransform, new Vector2(320f, 48f), new Vector2(0.52f, 0.5f), Vector2.zero);
        label.characterSpacing = 3f;

        CreateDiamond(name + "_LeftDiamond", buttonObject.transform, new Vector2(-235f, 0f));
        CreateDiamond(name + "_RightDiamond", buttonObject.transform, new Vector2(235f, 0f));
        CreateRectPart(name + "_LeftCap", buttonObject.transform, new Vector2(28f, 28f), new Vector2(0f, 0.5f), new Vector2(2f, 0f), new Color32(78, 45, 23, 240)).localRotation = Quaternion.Euler(0f, 0f, 45f);
        CreateRectPart(name + "_RightCap", buttonObject.transform, new Vector2(28f, 28f), new Vector2(1f, 0.5f), new Vector2(-2f, 0f), new Color32(78, 45, 23, 240)).localRotation = Quaternion.Euler(0f, 0f, 45f);

        MainMenuButtonFX fx = buttonObject.AddComponent<MainMenuButtonFX>();
        fx.Configure(label, frame, glow, primary);
        return button;
    }

    private static GameObject CreateEpicInfoPanel(Transform parent, string name, string titleText, string kickerText, string bodyText, out Button closeButton)
    {
        GameObject overlay = CreateFullScreenImage(name, parent, new Color32(0, 0, 0, 185), true);
        GameObject panel = CreateUIObject(name + "_Box", overlay.transform, new Vector2(720f, 390f), new Vector2(0.5f, 0.5f), Vector2.zero);
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color32(18, 15, 12, 250);
        Outline outline = panel.AddComponent<Outline>();
        outline.effectColor = new Color32(188, 128, 48, 230);
        outline.effectDistance = new Vector2(2f, -2f);

        CreateRectPart(name + "_Header", panel.transform, new Vector2(680f, 72f), new Vector2(0.5f, 0.82f), Vector2.zero, new Color32(89, 23, 18, 220));
        TMP_Text title = CreateText(name + "_Title", panel.transform, titleText, 38f, new Color32(235, 196, 113, 255), TextAlignmentOptions.Center, FontStyles.Bold);
        SetRect(title.rectTransform, new Vector2(620f, 58f), new Vector2(0.5f, 0.83f), Vector2.zero);
        TMP_Text kicker = CreateText(name + "_Kicker", panel.transform, kickerText, 16f, new Color32(184, 128, 58, 255), TextAlignmentOptions.Center, FontStyles.Bold);
        SetRect(kicker.rectTransform, new Vector2(600f, 32f), new Vector2(0.5f, 0.6f), Vector2.zero);
        kicker.characterSpacing = 4f;
        TMP_Text body = CreateText(name + "_Body", panel.transform, bodyText, 22f, new Color32(220, 212, 195, 255), TextAlignmentOptions.Center, FontStyles.Normal);
        SetRect(body.rectTransform, new Vector2(590f, 88f), new Vector2(0.5f, 0.43f), Vector2.zero);

        closeButton = CreateEpicMenuButton(panel.transform, "ĐÓNG", "ESC", false, name + "_CloseButton");
        RectTransform closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.5f, 0.14f);
        closeRect.anchorMax = new Vector2(0.5f, 0.14f);
        closeRect.anchoredPosition = Vector2.zero;
        closeRect.sizeDelta = new Vector2(300f, 56f);
        overlay.SetActive(false);
        return overlay;
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

    private static GameObject CreateCube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
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

        return cube;
    }

    private static GameObject CreateCylinder(string name, Transform parent, Vector3 position, Vector3 scale, Material material, Vector3 rotation)
    {
        GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinder.name = name;
        cylinder.transform.SetParent(parent, false);
        cylinder.transform.localPosition = position;
        cylinder.transform.localEulerAngles = rotation;
        cylinder.transform.localScale = scale;
        cylinder.GetComponent<Renderer>().sharedMaterial = material;

        Collider collider = cylinder.GetComponent<Collider>();
        if (collider != null)
            Object.DestroyImmediate(collider);

        return cylinder;
    }

    private static void CreateBattlements(Transform parent, string prefix, float wallWidth, float y, float z, Material material)
    {
        int count = Mathf.Max(3, Mathf.RoundToInt(wallWidth / 1.15f));
        float step = wallWidth / count;
        for (int i = 0; i <= count; i++)
        {
            float x = -wallWidth * 0.5f + i * step;
            CreateCube(prefix + "_" + i, parent, new Vector3(x, y, z), new Vector3(0.48f, 0.5f, 0.46f), material);
        }
    }

    private static void CreateBrazier(Transform parent, Vector3 position, Material bronzeMat, Material emberMat)
    {
        GameObject root = new GameObject("MainMenu_Brazier");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = position;

        CreateCylinder("Brazier_Bowl", root.transform, Vector3.zero, new Vector3(0.42f, 0.14f, 0.42f), bronzeMat, Vector3.zero);
        CreateCube("Brazier_LegA", root.transform, new Vector3(-0.18f, -0.42f, 0f), new Vector3(0.08f, 0.65f, 0.08f), bronzeMat);
        CreateCube("Brazier_LegB", root.transform, new Vector3(0.18f, -0.42f, 0f), new Vector3(0.08f, 0.65f, 0.08f), bronzeMat);

        GameObject flame = new GameObject("Brazier_Flame");
        flame.transform.SetParent(root.transform, false);
        flame.transform.localPosition = new Vector3(0f, 0.28f, 0f);
        ParticleSystem particles = flame.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.35f, 0.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color32(255, 203, 80, 240), new Color32(194, 42, 18, 220));
        main.maxParticles = 45;
        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 18f;
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.radius = 0.16f;
        shape.angle = 10f;
        ParticleSystemRenderer renderer = flame.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = emberMat;

        Light light = flame.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color32(255, 128, 42, 255);
        light.range = 4.5f;
        light.intensity = 2.1f;
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
        Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i] != null && transforms[i].name == name)
                return;
        }

        throw new UnityException("Main Menu verify failed: missing object " + name + ".");
    }
}
