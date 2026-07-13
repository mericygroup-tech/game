using System;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Idempotent editor builder for the gameplay-only Pause System.
/// S01 and S02 are video scenes in this project and are deliberately excluded.
/// </summary>
public static class PauseSystemBuilder
{
    private const string GameplayScenePath = "Assets/Scenes/S03.unity";
    private const string S01VideoScenePath = "Assets/Scenes/S01.unity";
    private const string S02VideoScenePath = "Assets/Scenes/S02.unity";
    private const string RootName = "PauseSystem";

    private static readonly Color OverlayColor = new Color32(3, 4, 7, 222);
    private static readonly Color PanelColor = new Color32(20, 17, 15, 248);
    private static readonly Color PanelBorderColor = new Color32(154, 105, 45, 220);
    private static readonly Color TextColor = new Color32(239, 222, 183, 255);
    private static readonly Color MutedTextColor = new Color32(177, 157, 126, 255);
    private static readonly Color AccentColor = new Color32(213, 154, 64, 255);
    private static readonly Color ButtonColor = new Color32(48, 35, 26, 250);
    private static readonly Color ButtonHighlightColor = new Color32(112, 36, 28, 255);

    [MenuItem("Tools/Dong Chay Anh Hung/Pause/Build S03 Gameplay Pause System")]
    public static void BuildS03PauseSystem()
    {
        Scene scene = GetOrOpenGameplayScene();
        EnsureSingleEventSystem();
        RemoveExistingRoot(scene);

        PlayerController3D playerController = UnityEngine.Object.FindAnyObjectByType<PlayerController3D>(FindObjectsInactive.Include);
        PlayerHealth3D playerHealth = playerController != null ? playerController.GetComponent<PlayerHealth3D>() : null;
        BlessingManager blessingManager = UnityEngine.Object.FindAnyObjectByType<BlessingManager>(FindObjectsInactive.Include);
        S03ArenaDirector arenaDirector = UnityEngine.Object.FindAnyObjectByType<S03ArenaDirector>(FindObjectsInactive.Include);

        if (playerController == null || blessingManager == null || arenaDirector == null)
            throw new InvalidOperationException("S03 gameplay references are incomplete. Player, BlessingManager and S03ArenaDirector are required.");

        TMP_FontAsset font = ResolveFont();
        GameObject root = new GameObject(RootName);
        PauseManager pauseManager = root.AddComponent<PauseManager>();

        GameObject canvasObject = CreateUIObject("PauseCanvas", root.transform, Vector2.zero);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();
        PauseMenuUI pauseMenuUI = canvasObject.AddComponent<PauseMenuUI>();

        GameObject overlay = CreateStretchImage("PauseOverlay", canvasObject.transform, OverlayColor, true);
        CanvasGroup rootGroup = overlay.AddComponent<CanvasGroup>();
        rootGroup.alpha = 0f;
        rootGroup.interactable = false;
        rootGroup.blocksRaycasts = false;

        GameObject titleRule = CreateUIObject("PauseTitleRule", overlay.transform, new Vector2(760f, 2f));
        RectTransform titleRuleRect = (RectTransform)titleRule.transform;
        titleRuleRect.anchorMin = titleRuleRect.anchorMax = new Vector2(0.5f, 0.93f);
        titleRuleRect.anchoredPosition = Vector2.zero;
        titleRule.AddComponent<Image>().color = new Color(AccentColor.r, AccentColor.g, AccentColor.b, 0.55f);

        GameObject mainPanel = CreatePanel("PauseMainPanel", overlay.transform, new Vector2(660f, 760f));
        CreateText("Title", mainPanel.transform, "TẠM DỪNG", 54f, new Vector2(0f, 292f), new Vector2(580f, 74f), TextAlignmentOptions.Center, AccentColor, FontStyles.Bold, font);
        CreateText("Subtitle", mainPanel.transform, "DÒNG CHẢY ANH HÙNG", 19f, new Vector2(0f, 244f), new Vector2(540f, 40f), TextAlignmentOptions.Center, MutedTextColor, FontStyles.Normal, font);

        Button resumeButton = CreateButton("ResumeButton", mainPanel.transform, "TIẾP TỤC", new Vector2(0f, 150f), new Vector2(500f, 68f), font);
        Button settingsButton = CreateButton("SettingsButton", mainPanel.transform, "CÀI ĐẶT", new Vector2(0f, 65f), new Vector2(500f, 68f), font);
        Button restartButton = CreateButton("RestartButton", mainPanel.transform, "CHƠI LẠI", new Vector2(0f, -20f), new Vector2(500f, 68f), font);
        Button menuButton = CreateButton("MainMenuButton", mainPanel.transform, "VỀ MENU CHÍNH", new Vector2(0f, -105f), new Vector2(500f, 68f), font);
        Button quitButton = CreateButton("QuitButton", mainPanel.transform, "THOÁT GAME", new Vector2(0f, -190f), new Vector2(500f, 68f), font);
        CreateText("Hint", mainPanel.transform, "ESC / B  ·  QUAY LẠI     ENTER / A  ·  XÁC NHẬN", 16f, new Vector2(0f, -310f), new Vector2(590f, 42f), TextAlignmentOptions.Center, MutedTextColor, FontStyles.Normal, font);

        GameObject settingsPanel = CreatePanel("PauseSettingsPanel", overlay.transform, new Vector2(920f, 790f));
        CreateText("Title", settingsPanel.transform, "CÀI ĐẶT", 48f, new Vector2(0f, 318f), new Vector2(820f, 68f), TextAlignmentOptions.Center, AccentColor, FontStyles.Bold, font);
        CreateText("Subtitle", settingsPanel.transform, "ÂM THANH  ·  HÌNH ẢNH", 18f, new Vector2(0f, 272f), new Vector2(760f, 36f), TextAlignmentOptions.Center, MutedTextColor, FontStyles.Normal, font);

        Slider masterSlider = CreateVolumeRow(settingsPanel.transform, "MasterVolume", "ÂM LƯỢNG TỔNG", 180f, font, out TMP_Text masterValue);
        Slider musicSlider = CreateVolumeRow(settingsPanel.transform, "MusicVolume", "NHẠC NỀN", 95f, font, out TMP_Text musicValue);
        Slider sfxSlider = CreateVolumeRow(settingsPanel.transform, "SfxVolume", "HIỆU ỨNG", 10f, font, out TMP_Text sfxValue);

        Button fullscreenButton = CreateButton("FullscreenButton", settingsPanel.transform, "TOÀN MÀN HÌNH", new Vector2(-130f, -92f), new Vector2(430f, 62f), font);
        TMP_Text fullscreenValue = CreateText("FullscreenValue", settingsPanel.transform, "TẮT", 24f, new Vector2(300f, -92f), new Vector2(130f, 54f), TextAlignmentOptions.Center, AccentColor, FontStyles.Bold, font);

        CreateText("ResolutionLabel", settingsPanel.transform, "ĐỘ PHÂN GIẢI", 21f, new Vector2(-285f, -178f), new Vector2(220f, 48f), TextAlignmentOptions.Left, TextColor, FontStyles.Bold, font);
        Button previousResolution = CreateButton("PreviousResolutionButton", settingsPanel.transform, "‹", new Vector2(-85f, -178f), new Vector2(64f, 54f), font);
        TMP_Text resolutionValue = CreateText("ResolutionValue", settingsPanel.transform, "1920 × 1080", 22f, new Vector2(80f, -178f), new Vector2(250f, 54f), TextAlignmentOptions.Center, TextColor, FontStyles.Bold, font);
        Button nextResolution = CreateButton("NextResolutionButton", settingsPanel.transform, "›", new Vector2(250f, -178f), new Vector2(64f, 54f), font);
        Button settingsBack = CreateButton("SettingsBackButton", settingsPanel.transform, "QUAY LẠI", new Vector2(0f, -292f), new Vector2(410f, 64f), font);

        GameObject confirmationPanel = CreatePanel("PauseConfirmationPanel", overlay.transform, new Vector2(760f, 380f));
        CreateText("Title", confirmationPanel.transform, "XÁC NHẬN", 38f, new Vector2(0f, 126f), new Vector2(680f, 54f), TextAlignmentOptions.Center, AccentColor, FontStyles.Bold, font);
        TMP_Text confirmationMessage = CreateText("Message", confirmationPanel.transform, string.Empty, 24f, new Vector2(0f, 35f), new Vector2(640f, 120f), TextAlignmentOptions.Center, TextColor, FontStyles.Normal, font);
        confirmationMessage.textWrappingMode = TextWrappingModes.Normal;
        Button confirmButton = CreateButton("ConfirmButton", confirmationPanel.transform, "ĐỒNG Ý", new Vector2(-155f, -112f), new Vector2(270f, 62f), font);
        Button cancelButton = CreateButton("CancelButton", confirmationPanel.transform, "HỦY", new Vector2(155f, -112f), new Vector2(270f, 62f), font);

        settingsPanel.SetActive(false);
        confirmationPanel.SetActive(false);

        SerializedObject uiObject = new SerializedObject(pauseMenuUI);
        SetReference(uiObject, "pauseRoot", overlay);
        SetReference(uiObject, "rootGroup", rootGroup);
        SetReference(uiObject, "animatedPanel", mainPanel.GetComponent<RectTransform>());
        SetReference(uiObject, "mainPanel", mainPanel);
        SetReference(uiObject, "settingsPanel", settingsPanel);
        SetReference(uiObject, "confirmationPanel", confirmationPanel);
        SetReference(uiObject, "resumeButton", resumeButton);
        SetReference(uiObject, "settingsButton", settingsButton);
        SetReference(uiObject, "restartButton", restartButton);
        SetReference(uiObject, "mainMenuButton", menuButton);
        SetReference(uiObject, "quitButton", quitButton);
        SetReference(uiObject, "masterVolumeSlider", masterSlider);
        SetReference(uiObject, "masterVolumeValue", masterValue);
        SetReference(uiObject, "musicVolumeSlider", musicSlider);
        SetReference(uiObject, "musicVolumeValue", musicValue);
        SetReference(uiObject, "sfxVolumeSlider", sfxSlider);
        SetReference(uiObject, "sfxVolumeValue", sfxValue);
        SetReference(uiObject, "fullscreenButton", fullscreenButton);
        SetReference(uiObject, "fullscreenValue", fullscreenValue);
        SetReference(uiObject, "previousResolutionButton", previousResolution);
        SetReference(uiObject, "nextResolutionButton", nextResolution);
        SetReference(uiObject, "resolutionValue", resolutionValue);
        SetReference(uiObject, "settingsBackButton", settingsBack);
        SetReference(uiObject, "confirmationMessage", confirmationMessage);
        SetReference(uiObject, "confirmButton", confirmButton);
        SetReference(uiObject, "cancelButton", cancelButton);
        uiObject.ApplyModifiedPropertiesWithoutUndo();

        pauseManager.Configure(pauseMenuUI, playerController, playerHealth, blessingManager, arenaDirector);
        overlay.SetActive(false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[Pause Builder] Gameplay Pause System built in S03 only. S01/S02 video scenes were not modified.");
    }

    [MenuItem("Tools/Dong Chay Anh Hung/Pause/Verify Gameplay Pause System")]
    public static void VerifyPauseSystem()
    {
        Scene scene = GetOrOpenGameplayScene();
        PauseManager[] managers = UnityEngine.Object.FindObjectsByType<PauseManager>(FindObjectsInactive.Include);
        if (managers.Length != 1)
            throw new InvalidOperationException("Expected exactly one PauseManager in S03, found " + managers.Length + ".");

        if (!managers[0].ValidateConfiguration(out string configurationError))
            throw new InvalidOperationException("Pause configuration invalid: " + configurationError);

        EventSystem[] eventSystems = UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include);
        if (eventSystems.Length != 1)
            throw new InvalidOperationException("Expected exactly one EventSystem in S03, found " + eventSystems.Length + ".");

        if (ContainsPauseManager(S01VideoScenePath) || ContainsPauseManager(S02VideoScenePath))
            throw new InvalidOperationException("PauseManager must not be installed in S01/S02 video scenes.");

        GameObject pauseRoot = FindRoot(scene, RootName);
        if (pauseRoot == null)
            throw new InvalidOperationException("PauseSystem root was not found in S03.");

        Canvas[] canvases = pauseRoot.GetComponentsInChildren<Canvas>(true);
        if (canvases.Length != 1)
            throw new InvalidOperationException("PauseSystem must own exactly one Canvas.");

        Debug.Log("[Pause QA] Passed: S03 has one configured PauseManager/Canvas/EventSystem; S01 and S02 remain video-only.");
    }

    private static Scene GetOrOpenGameplayScene()
    {
        Scene activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.path == GameplayScenePath)
            return activeScene;

        if (activeScene.isDirty && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            throw new OperationCanceledException("Pause build canceled because the active scene has unsaved changes.");

        return EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);
    }

    private static void EnsureSingleEventSystem()
    {
        EventSystem[] systems = UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include);
        if (systems.Length == 1)
            return;

        if (systems.Length == 0)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
            return;
        }

        throw new InvalidOperationException("S03 contains duplicate EventSystem objects. Resolve them before building Pause UI.");
    }

    private static void RemoveExistingRoot(Scene scene)
    {
        GameObject existing = FindRoot(scene, RootName);
        if (existing != null)
            UnityEngine.Object.DestroyImmediate(existing);
    }

    private static GameObject FindRoot(Scene scene, string objectName)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i] != null && roots[i].name == objectName)
                return roots[i];
        }

        return null;
    }

    private static bool ContainsPauseManager(string scenePath)
    {
        return File.Exists(scenePath) && File.ReadAllText(scenePath).Contains("Assembly-CSharp::PauseManager");
    }

    private static TMP_FontAsset ResolveFont()
    {
        TMP_FontAsset font = TMP_Settings.defaultFontAsset;
        if (font == null)
            font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (font == null)
            throw new InvalidOperationException("TextMesh Pro default font asset was not found.");
        return font;
    }

    private static GameObject CreatePanel(string name, Transform parent, Vector2 size)
    {
        GameObject panel = CreateUIObject(name, parent, size);
        Image image = panel.AddComponent<Image>();
        image.color = PanelColor;
        image.raycastTarget = true;
        Outline outline = panel.AddComponent<Outline>();
        outline.effectColor = PanelBorderColor;
        outline.effectDistance = new Vector2(2f, -2f);
        return panel;
    }

    private static GameObject CreateStretchImage(string name, Transform parent, Color color, bool raycastTarget)
    {
        GameObject target = CreateUIObject(name, parent, Vector2.zero);
        RectTransform rect = (RectTransform)target.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Image image = target.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = raycastTarget;
        return target;
    }

    private static GameObject CreateUIObject(string name, Transform parent, Vector2 size)
    {
        GameObject target = new GameObject(name, typeof(RectTransform));
        RectTransform rect = target.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;
        return target;
    }

    private static TMP_Text CreateText(
        string name,
        Transform parent,
        string value,
        float fontSize,
        Vector2 position,
        Vector2 size,
        TextAlignmentOptions alignment,
        Color color,
        FontStyles style,
        TMP_FontAsset font)
    {
        GameObject textObject = CreateUIObject(name, parent, size);
        RectTransform rect = (RectTransform)textObject.transform;
        rect.anchoredPosition = position;
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.font = font;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = color;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        return text;
    }

    private static Button CreateButton(string name, Transform parent, string label, Vector2 position, Vector2 size, TMP_FontAsset font)
    {
        GameObject buttonObject = CreateUIObject(name, parent, size);
        ((RectTransform)buttonObject.transform).anchoredPosition = position;
        Image image = buttonObject.AddComponent<Image>();
        image.color = ButtonColor;
        Outline outline = buttonObject.AddComponent<Outline>();
        outline.effectColor = new Color(AccentColor.r, AccentColor.g, AccentColor.b, 0.5f);
        outline.effectDistance = new Vector2(1f, -1f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = ButtonColor;
        colors.highlightedColor = ButtonHighlightColor;
        colors.selectedColor = ButtonHighlightColor;
        colors.pressedColor = new Color32(137, 49, 35, 255);
        colors.disabledColor = new Color32(47, 44, 40, 180);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        Navigation navigation = button.navigation;
        navigation.mode = Navigation.Mode.Automatic;
        button.navigation = navigation;
        AudioButtonFeedback audioFeedback = buttonObject.AddComponent<AudioButtonFeedback>();
        audioFeedback.Configure(true);

        TMP_Text labelText = CreateText("Label", buttonObject.transform, label, 24f, Vector2.zero, size - new Vector2(28f, 10f), TextAlignmentOptions.Center, TextColor, FontStyles.Bold, font);
        labelText.enableAutoSizing = true;
        labelText.fontSizeMin = 16f;
        labelText.fontSizeMax = 24f;
        return button;
    }

    private static Slider CreateVolumeRow(Transform parent, string name, string label, float y, TMP_FontAsset font, out TMP_Text valueText)
    {
        CreateText(name + "Label", parent, label, 21f, new Vector2(-300f, y), new Vector2(240f, 48f), TextAlignmentOptions.Left, TextColor, FontStyles.Bold, font);
        Slider slider = CreateSlider(name + "Slider", parent, new Vector2(75f, y), new Vector2(420f, 34f));
        valueText = CreateText(name + "Value", parent, "100%", 21f, new Vector2(340f, y), new Vector2(110f, 46f), TextAlignmentOptions.Center, AccentColor, FontStyles.Bold, font);
        return slider;
    }

    private static Slider CreateSlider(string name, Transform parent, Vector2 position, Vector2 size)
    {
        GameObject sliderObject = CreateUIObject(name, parent, size);
        ((RectTransform)sliderObject.transform).anchoredPosition = position;
        Slider slider = sliderObject.AddComponent<Slider>();
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;

        GameObject background = CreateStretchImage("Background", sliderObject.transform, new Color32(38, 34, 30, 255), false);
        RectTransform backgroundRect = (RectTransform)background.transform;
        backgroundRect.offsetMin = new Vector2(0f, 10f);
        backgroundRect.offsetMax = new Vector2(0f, -10f);

        GameObject fillArea = CreateUIObject("Fill Area", sliderObject.transform, Vector2.zero);
        RectTransform fillAreaRect = (RectTransform)fillArea.transform;
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(8f, 10f);
        fillAreaRect.offsetMax = new Vector2(-8f, -10f);
        GameObject fill = CreateStretchImage("Fill", fillArea.transform, AccentColor, false);
        RectTransform fillRect = (RectTransform)fill.transform;

        GameObject handleArea = CreateUIObject("Handle Slide Area", sliderObject.transform, Vector2.zero);
        RectTransform handleAreaRect = (RectTransform)handleArea.transform;
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = new Vector2(10f, 0f);
        handleAreaRect.offsetMax = new Vector2(-10f, 0f);
        GameObject handle = CreateUIObject("Handle", handleArea.transform, new Vector2(24f, 34f));
        Image handleImage = handle.AddComponent<Image>();
        handleImage.color = new Color32(238, 205, 132, 255);

        slider.fillRect = fillRect;
        slider.handleRect = (RectTransform)handle.transform;
        slider.targetGraphic = handleImage;
        Navigation navigation = slider.navigation;
        navigation.mode = Navigation.Mode.Automatic;
        slider.navigation = navigation;
        sliderObject.AddComponent<AudioSliderFeedback>();
        return slider;
    }

    private static void SetReference(SerializedObject serializedObject, string propertyName, UnityEngine.Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
            throw new MissingFieldException(serializedObject.targetObject.GetType().Name, propertyName);
        property.objectReferenceValue = value;
    }
}

/// <summary>
/// Play Mode smoke checks invoked by CodexLocalBridge. These methods never save
/// runtime state and leave gameplay resumed after every successful check.
/// </summary>
public static class PauseSystemRuntimeQa
{
    private const string ArmedSessionKey = "DCAH.PauseRuntimeQa.Armed";
    private static readonly string ResultPath = Path.Combine(
        Directory.GetCurrentDirectory(), "Library", "CodexBridge", "pause_runtime_qa.json");

    private static bool automationRunning;
    private static float automationStartedAt;
    private static bool runInBackgroundBeforeAutomation;

    [InitializeOnLoadMethod]
    private static void InitializePlayModeHook()
    {
        EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
        EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
    }

    public static void ArmAndEnterPlayMode()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Pause Runtime QA can only be armed from Edit Mode.");

        Directory.CreateDirectory(Path.GetDirectoryName(ResultPath));
        if (File.Exists(ResultPath))
            File.Delete(ResultPath);

        Application.runInBackground = false;
        SessionState.SetBool(ArmedSessionKey, true);
        EditorApplication.isPlaying = true;
        Debug.Log("[Pause Runtime QA] Armed before Play Mode.");
    }

    private static void HandlePlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(ArmedSessionKey, false))
            return;

        SessionState.SetBool(ArmedSessionKey, false);
        StartAutomatedSequence();
    }

    public static void StartAutomatedSequence()
    {
        RequirePlayMode();
        Directory.CreateDirectory(Path.GetDirectoryName(ResultPath));
        if (File.Exists(ResultPath))
            File.Delete(ResultPath);

        runInBackgroundBeforeAutomation = Application.runInBackground;
        Application.runInBackground = true;
        EditorApplication.isPaused = false;
        EditorApplication.update -= AutomationTick;

        try
        {
            PauseManager manager = RequireObject<PauseManager>();
            S03ArenaDirector arena = RequireObject<S03ArenaDirector>();
            if (arena.HasArenaStarted)
                throw new InvalidOperationException("Automated Pause QA started too late; S03 intro had already finished.");
            if (manager.TryPauseGame() || PauseManager.IsGamePaused || Time.timeScale <= 0f)
                throw new InvalidOperationException("Pause opened during the S03 intro.");

            automationRunning = true;
            automationStartedAt = Time.realtimeSinceStartup;
            EditorApplication.update += AutomationTick;
            Debug.Log("[Pause Runtime QA] Automated sequence started; intro exclusion passed.");
        }
        catch (Exception ex)
        {
            CompleteAutomation(false, ex.Message);
        }
    }

    public static void ResumeEditorSimulation()
    {
        RequirePlayMode();
        Application.runInBackground = true;
        EditorApplication.isPaused = false;
        Debug.Log("[Pause Runtime QA] Editor simulation resumed with temporary background execution.");
    }

    public static void ResetEditorSimulation()
    {
        Application.runInBackground = false;
        EditorApplication.isPaused = false;
        Debug.Log("[Pause Runtime QA] Editor simulation reset to project background settings.");
    }

    public static void VerifyIntroPauseBlocked()
    {
        RequirePlayMode();
        PauseManager manager = RequireObject<PauseManager>();
        S03ArenaDirector arena = RequireObject<S03ArenaDirector>();

        if (arena.HasArenaStarted)
            throw new InvalidOperationException("Intro pause check ran after arena gameplay had already started.");

        if (manager.TryPauseGame() || PauseManager.IsGamePaused || Time.timeScale <= 0f)
            throw new InvalidOperationException("Pause opened during the S03 intro.");

        Debug.Log("[Pause Runtime QA] Intro gate passed: Pause stayed closed before arena gameplay.");
    }

    public static void VerifyGameplayPauseAndBlessingBlock()
    {
        RequirePlayMode();
        RunGameplayAssertions();
        Debug.Log("[Pause Runtime QA] Passed: gameplay pause/resume, input lock, damage guard, music duck and Blessing exclusion.");
    }

    private static void AutomationTick()
    {
        if (!automationRunning)
            return;

        if (!EditorApplication.isPlaying)
        {
            CompleteAutomation(false, "Play Mode ended before automated Pause QA completed.");
            return;
        }

        if (EditorApplication.isPaused)
            EditorApplication.isPaused = false;

        if (Time.realtimeSinceStartup - automationStartedAt > 45f)
        {
            CompleteAutomation(false, "Timed out waiting for S03 arena gameplay to start.");
            return;
        }

        S03ArenaDirector arena = UnityEngine.Object.FindAnyObjectByType<S03ArenaDirector>(FindObjectsInactive.Include);
        PlayerController3D player = UnityEngine.Object.FindAnyObjectByType<PlayerController3D>(FindObjectsInactive.Include);
        if (arena == null || !arena.HasArenaStarted || player == null || player.InputLocked)
            return;

        try
        {
            RunGameplayAssertions();
            CompleteAutomation(true, "Pause audio feedback, intro exclusion, gameplay pause/resume, input lock, damage guard, 40% music duck and Blessing exclusion passed.");
        }
        catch (Exception ex)
        {
            CompleteAutomation(false, ex.Message);
        }
    }

    private static void RunGameplayAssertions()
    {
        PauseManager manager = RequireObject<PauseManager>();
        PauseMenuUI menuUI = RequireObject<PauseMenuUI>();
        S03ArenaDirector arena = RequireObject<S03ArenaDirector>();
        PlayerController3D player = RequireObject<PlayerController3D>();
        PlayerHealth3D health = player.GetComponent<PlayerHealth3D>();
        BlessingManager blessings = RequireObject<BlessingManager>();

        if (!arena.HasArenaStarted)
            throw new InvalidOperationException("Arena gameplay has not started; runtime Pause test cannot continue.");

        int hpBeforePauseDamage = health != null ? health.currentHP : 0;
        if (!manager.TryPauseGame())
            throw new InvalidOperationException("PauseManager rejected a valid gameplay pause request.");

        if (!PauseManager.IsGamePaused || !manager.IsPaused || Time.timeScale != 0f || !player.InputLocked || !menuUI.IsVisible)
            throw new InvalidOperationException("Gameplay pause state is incomplete.");

        AudioButtonFeedback[] buttonFeedback = menuUI.GetComponentsInChildren<AudioButtonFeedback>(true);
        if (buttonFeedback.Length < 11)
            throw new InvalidOperationException("Pause Menu is missing button audio feedback components.");

        for (int i = 0; i < buttonFeedback.Length; i++)
        {
            if (!buttonFeedback[i].HoverFeedbackEnabled)
                throw new InvalidOperationException("A Pause Menu button has hover/select audio disabled: " + buttonFeedback[i].name);
        }

        AudioSliderFeedback[] sliderFeedback = menuUI.GetComponentsInChildren<AudioSliderFeedback>(true);
        if (sliderFeedback.Length < 3)
            throw new InvalidOperationException("Pause Settings is missing slider tick feedback components.");

        GameAudioDirector audio = GameAudioDirector.Instance;
        if (audio != null && Mathf.Abs(audio.MusicDuckMultiplier - 0.4f) > 0.001f)
            throw new InvalidOperationException("Music was not ducked to 40% while paused.");

        if (health != null && !health.isDead)
        {
            health.TakeDamage(1);
            if (health.currentHP != hpBeforePauseDamage)
                throw new InvalidOperationException("Player received damage while the game was paused.");
        }

        manager.ResumeGame();
        if (PauseManager.IsGamePaused || manager.IsPaused || Time.timeScale != 1f || player.InputLocked)
            throw new InvalidOperationException("Gameplay state was not restored after Resume.");
        if (audio != null && Mathf.Abs(audio.MusicDuckMultiplier - 1f) > 0.001f)
            throw new InvalidOperationException("Music duck was not restored after Resume.");

        blessings.PresentChoices(null);
        if (!blessings.IsSelectionOpen)
            throw new InvalidOperationException("Blessing Choice did not open for overlap validation.");
        if (manager.TryPauseGame())
            throw new InvalidOperationException("Pause opened on top of Blessing Choice.");

        MethodInfo finishSelection = typeof(BlessingManager).GetMethod("FinishSelection", BindingFlags.Instance | BindingFlags.NonPublic);
        if (finishSelection == null)
            throw new MissingMethodException(nameof(BlessingManager), "FinishSelection");
        finishSelection.Invoke(blessings, null);

        if (blessings.IsSelectionOpen || player.InputLocked)
            throw new InvalidOperationException("Blessing overlap test did not restore gameplay input.");
    }

    private static void CompleteAutomation(bool success, string message)
    {
        automationRunning = false;
        EditorApplication.update -= AutomationTick;

        if (PauseManager.IsGamePaused)
        {
            PauseManager manager = UnityEngine.Object.FindAnyObjectByType<PauseManager>(FindObjectsInactive.Include);
            manager?.ResumeGame();
        }

        Time.timeScale = 1f;
        GameAudio.SetMusicDuck(1f);
        Application.runInBackground = runInBackgroundBeforeAutomation;
        string json = "{\n" +
                      "  \"ok\": " + (success ? "true" : "false") + ",\n" +
                      "  \"message\": \"" + EscapeJson(message) + "\"\n" +
                      "}\n";
        File.WriteAllText(ResultPath, json);

        if (success)
            Debug.Log("[Pause Runtime QA] " + message);
        else
            Debug.LogError("[Pause Runtime QA] " + message);
    }

    private static string EscapeJson(string value)
    {
        return (value ?? string.Empty)
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n");
    }

    private static void RequirePlayMode()
    {
        if (!EditorApplication.isPlaying)
            throw new InvalidOperationException("Pause runtime QA requires Play Mode.");
    }

    private static T RequireObject<T>() where T : UnityEngine.Object
    {
        T target = UnityEngine.Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);
        if (target == null)
            throw new InvalidOperationException(typeof(T).Name + " was not found in the active scene.");
        return target;
    }
}
