using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Presentation and navigation for the gameplay Pause Menu. All animation uses
/// unscaled time because gameplay time is zero while this UI is visible.
/// </summary>
[DisallowMultipleComponent]
public sealed class PauseMenuUI : MonoBehaviour
{
    private const string FullscreenKey = "PauseSettings.Fullscreen";
    private const string ResolutionWidthKey = "PauseSettings.ResolutionWidth";
    private const string ResolutionHeightKey = "PauseSettings.ResolutionHeight";

    [Header("Root")]
    [SerializeField] private GameObject pauseRoot;
    [SerializeField] private CanvasGroup rootGroup;
    [SerializeField] private RectTransform animatedPanel;
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject confirmationPanel;

    [Header("Main Buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitButton;

    [Header("Settings")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private TMP_Text masterVolumeValue;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private TMP_Text musicVolumeValue;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private TMP_Text sfxVolumeValue;
    [SerializeField] private Button fullscreenButton;
    [SerializeField] private TMP_Text fullscreenValue;
    [SerializeField] private Button previousResolutionButton;
    [SerializeField] private Button nextResolutionButton;
    [SerializeField] private TMP_Text resolutionValue;
    [SerializeField] private Button settingsBackButton;

    [Header("Confirmation")]
    [SerializeField] private TMP_Text confirmationMessage;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    [Header("Animation")]
    [SerializeField, Min(0.05f)] private float showDuration = 0.2f;
    [SerializeField, Min(0.05f)] private float hideDuration = 0.14f;

    private readonly List<Vector2Int> availableResolutions = new List<Vector2Int>();
    private PauseManager owner;
    private Action pendingConfirmation;
    private Coroutine visibilityRoutine;
    private bool initialized;
    private int selectedResolutionIndex;

    public bool IsSettingsOpen => settingsPanel != null && settingsPanel.activeSelf;
    public bool IsConfirmationOpen => confirmationPanel != null && confirmationPanel.activeSelf;
    public bool IsVisible => pauseRoot != null && pauseRoot.activeSelf;

    public void Initialize(PauseManager pauseOwner)
    {
        owner = pauseOwner;
        if (initialized)
            return;

        initialized = true;
        ConfigurePauseButtonFeedback();
        BindButtons();
        BindSettings();
        ApplySavedDisplaySettings();
        BuildResolutionList();
        CloseAllSubPanels();

        if (pauseRoot != null)
            pauseRoot.SetActive(false);
    }

    public void ShowPauseMenu()
    {
        if (pauseRoot == null)
        {
            Debug.LogWarning("[Pause UI] Pause root is missing.", this);
            return;
        }

        CloseAllSubPanels();
        pauseRoot.SetActive(true);

        if (visibilityRoutine != null)
            StopCoroutine(visibilityRoutine);
        visibilityRoutine = StartCoroutine(AnimateVisibility(true));
    }

    public void HidePauseMenu()
    {
        if (pauseRoot == null || !pauseRoot.activeSelf)
            return;

        if (visibilityRoutine != null)
            StopCoroutine(visibilityRoutine);
        visibilityRoutine = StartCoroutine(AnimateVisibility(false));
    }

    public bool HandleBackRequest()
    {
        if (IsConfirmationOpen)
        {
            CloseConfirmation();
            return true;
        }

        if (IsSettingsOpen)
        {
            CloseSettings();
            return true;
        }

        return false;
    }

    public void CloseAllSubPanels()
    {
        pendingConfirmation = null;
        SetActive(settingsPanel, false);
        SetActive(confirmationPanel, false);
        SetActive(mainPanel, true);
    }

    public void ShowConfirmation(string message, Action confirmedAction)
    {
        pendingConfirmation = confirmedAction;
        if (confirmationMessage != null)
            confirmationMessage.text = message;

        SetActive(mainPanel, false);
        SetActive(settingsPanel, false);
        SetActive(confirmationPanel, true);
        Select(cancelButton);
    }

    public void SetInteractionEnabled(bool enabled)
    {
        if (rootGroup == null)
            return;

        rootGroup.interactable = enabled;
        rootGroup.blocksRaycasts = enabled;
    }

    public bool ValidateConfiguration(out string error)
    {
        if (pauseRoot == null || rootGroup == null || mainPanel == null || settingsPanel == null || confirmationPanel == null)
        {
            error = "Pause Menu root panels are not fully configured.";
            return false;
        }

        if (resumeButton == null || settingsButton == null || restartButton == null || mainMenuButton == null || quitButton == null)
        {
            error = "One or more Pause Menu buttons are missing.";
            return false;
        }

        if (masterVolumeSlider == null || musicVolumeSlider == null || sfxVolumeSlider == null ||
            fullscreenButton == null || previousResolutionButton == null || nextResolutionButton == null || settingsBackButton == null)
        {
            error = "Pause Settings controls are not fully configured.";
            return false;
        }

        if (confirmationMessage == null || confirmButton == null || cancelButton == null)
        {
            error = "Pause confirmation dialog is not fully configured.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private void BindButtons()
    {
        Bind(resumeButton, () => owner?.ResumeGame());
        Bind(settingsButton, OpenSettings);
        Bind(restartButton, () => owner?.RequestRestart());
        Bind(mainMenuButton, () => owner?.RequestMainMenu());
        Bind(quitButton, () => owner?.RequestQuit());
        Bind(settingsBackButton, CloseSettings);
        Bind(fullscreenButton, ToggleFullscreen);
        Bind(previousResolutionButton, () => StepResolution(-1));
        Bind(nextResolutionButton, () => StepResolution(1));
        Bind(confirmButton, ConfirmPendingAction);
        Bind(cancelButton, CloseConfirmation);
    }

    private void ConfigurePauseButtonFeedback()
    {
        ConfigureButtonFeedback(resumeButton);
        ConfigureButtonFeedback(settingsButton);
        ConfigureButtonFeedback(restartButton);
        ConfigureButtonFeedback(mainMenuButton);
        ConfigureButtonFeedback(quitButton);
        ConfigureButtonFeedback(fullscreenButton);
        ConfigureButtonFeedback(previousResolutionButton);
        ConfigureButtonFeedback(nextResolutionButton);
        ConfigureButtonFeedback(settingsBackButton);
        ConfigureButtonFeedback(confirmButton);
        ConfigureButtonFeedback(cancelButton);
    }

    private static void ConfigureButtonFeedback(Button button)
    {
        if (button == null)
            return;

        AudioButtonFeedback feedback = button.GetComponent<AudioButtonFeedback>();
        if (feedback == null)
            feedback = button.gameObject.AddComponent<AudioButtonFeedback>();

        feedback.Configure(true);
    }

    private void BindSettings()
    {
        ConfigureSlider(masterVolumeSlider, GameAudioBus.Master, masterVolumeValue);
        ConfigureSlider(musicVolumeSlider, GameAudioBus.Music, musicVolumeValue);
        ConfigureSlider(sfxVolumeSlider, GameAudioBus.Sfx, sfxVolumeValue);
        UpdateFullscreenText();
    }

    private void ConfigureSlider(Slider slider, GameAudioBus bus, TMP_Text valueText)
    {
        if (slider == null)
            return;

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        float currentValue = GameAudio.GetVolume(bus);
        slider.SetValueWithoutNotify(currentValue);
        SetPercentage(valueText, currentValue);
        slider.onValueChanged.RemoveAllListeners();
        slider.onValueChanged.AddListener(value =>
        {
            GameAudio.SetVolume(bus, value);
            SetPercentage(valueText, value);
        });
    }

    private void OpenSettings()
    {
        SyncSettingsValues();
        SetActive(mainPanel, false);
        SetActive(confirmationPanel, false);
        SetActive(settingsPanel, true);
        Select(masterVolumeSlider);
    }

    private void CloseSettings()
    {
        SetActive(settingsPanel, false);
        SetActive(confirmationPanel, false);
        SetActive(mainPanel, true);
        Select(settingsButton);
    }

    private void SyncSettingsValues()
    {
        SyncSlider(masterVolumeSlider, masterVolumeValue, GameAudioBus.Master);
        SyncSlider(musicVolumeSlider, musicVolumeValue, GameAudioBus.Music);
        SyncSlider(sfxVolumeSlider, sfxVolumeValue, GameAudioBus.Sfx);
        UpdateFullscreenText();
        FindCurrentResolutionIndex();
        UpdateResolutionText();
    }

    private static void SyncSlider(Slider slider, TMP_Text valueText, GameAudioBus bus)
    {
        float value = GameAudio.GetVolume(bus);
        if (slider != null)
            slider.SetValueWithoutNotify(value);
        SetPercentage(valueText, value);
    }

    private void ToggleFullscreen()
    {
        Screen.fullScreen = !Screen.fullScreen;
        PlayerPrefs.SetInt(FullscreenKey, Screen.fullScreen ? 1 : 0);
        PlayerPrefs.Save();
        UpdateFullscreenText();
    }

    private static void ApplySavedDisplaySettings()
    {
        if (PlayerPrefs.HasKey(FullscreenKey))
            Screen.fullScreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) != 0;

        int width = PlayerPrefs.GetInt(ResolutionWidthKey, 0);
        int height = PlayerPrefs.GetInt(ResolutionHeightKey, 0);
        if (width > 0 && height > 0 && (width != Screen.width || height != Screen.height))
            Screen.SetResolution(width, height, Screen.fullScreen);
    }

    private void BuildResolutionList()
    {
        availableResolutions.Clear();
        Resolution[] resolutions = Screen.resolutions;
        for (int i = 0; i < resolutions.Length; i++)
        {
            Vector2Int candidate = new Vector2Int(resolutions[i].width, resolutions[i].height);
            if (!availableResolutions.Contains(candidate))
                availableResolutions.Add(candidate);
        }

        availableResolutions.Sort((left, right) =>
        {
            int widthComparison = left.x.CompareTo(right.x);
            return widthComparison != 0 ? widthComparison : left.y.CompareTo(right.y);
        });

        Vector2Int current = new Vector2Int(Screen.width, Screen.height);
        if (!availableResolutions.Contains(current))
            availableResolutions.Add(current);

        FindCurrentResolutionIndex();
        UpdateResolutionText();
    }

    private void FindCurrentResolutionIndex()
    {
        if (availableResolutions.Count == 0)
        {
            selectedResolutionIndex = 0;
            return;
        }

        int bestIndex = 0;
        int bestDistance = int.MaxValue;
        for (int i = 0; i < availableResolutions.Count; i++)
        {
            Vector2Int resolution = availableResolutions[i];
            int distance = Mathf.Abs(resolution.x - Screen.width) + Mathf.Abs(resolution.y - Screen.height);
            if (distance >= bestDistance)
                continue;

            bestDistance = distance;
            bestIndex = i;
        }

        selectedResolutionIndex = bestIndex;
    }

    private void StepResolution(int direction)
    {
        if (availableResolutions.Count == 0)
            return;

        selectedResolutionIndex = (selectedResolutionIndex + direction + availableResolutions.Count) % availableResolutions.Count;
        Vector2Int resolution = availableResolutions[selectedResolutionIndex];
        Screen.SetResolution(resolution.x, resolution.y, Screen.fullScreen);
        PlayerPrefs.SetInt(ResolutionWidthKey, resolution.x);
        PlayerPrefs.SetInt(ResolutionHeightKey, resolution.y);
        PlayerPrefs.Save();
        UpdateResolutionText();
    }

    private void UpdateFullscreenText()
    {
        if (fullscreenValue != null)
            fullscreenValue.text = Screen.fullScreen ? "BẬT" : "TẮT";
    }

    private void UpdateResolutionText()
    {
        if (resolutionValue == null)
            return;

        if (availableResolutions.Count == 0)
        {
            resolutionValue.text = Screen.width + " × " + Screen.height;
            return;
        }

        Vector2Int resolution = availableResolutions[Mathf.Clamp(selectedResolutionIndex, 0, availableResolutions.Count - 1)];
        resolutionValue.text = resolution.x + " × " + resolution.y;
    }

    private void ConfirmPendingAction()
    {
        Action action = pendingConfirmation;
        pendingConfirmation = null;
        SetInteractionEnabled(false);
        action?.Invoke();
    }

    private void CloseConfirmation()
    {
        pendingConfirmation = null;
        SetActive(confirmationPanel, false);
        SetActive(settingsPanel, false);
        SetActive(mainPanel, true);
        Select(resumeButton);
    }

    private IEnumerator AnimateVisibility(bool showing)
    {
        if (rootGroup == null)
            yield break;

        SetInteractionEnabled(false);
        float duration = showing ? showDuration : hideDuration;
        float fromAlpha = rootGroup.alpha;
        float toAlpha = showing ? 1f : 0f;
        Vector3 visibleScale = Vector3.one;
        Vector3 hiddenScale = Vector3.one * 0.94f;
        Vector3 fromScale = animatedPanel != null ? animatedPanel.localScale : visibleScale;
        Vector3 toScale = showing ? visibleScale : hiddenScale;

        if (showing && rootGroup.alpha <= 0.001f)
        {
            fromAlpha = 0f;
            if (animatedPanel != null)
                fromScale = hiddenScale;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
            float eased = Mathf.SmoothStep(0f, 1f, t);
            rootGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, eased);
            if (animatedPanel != null)
                animatedPanel.localScale = Vector3.LerpUnclamped(fromScale, toScale, eased);
            yield return null;
        }

        rootGroup.alpha = toAlpha;
        if (animatedPanel != null)
            animatedPanel.localScale = toScale;

        if (showing)
        {
            SetInteractionEnabled(true);
            Select(resumeButton);
        }
        else if (pauseRoot != null)
        {
            pauseRoot.SetActive(false);
        }

        visibilityRoutine = null;
    }

    private static void Bind(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    private static void SetPercentage(TMP_Text label, float value)
    {
        if (label != null)
            label.text = Mathf.RoundToInt(Mathf.Clamp01(value) * 100f) + "%";
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null)
            target.SetActive(active);
    }

    private static void Select(Selectable selectable)
    {
        if (selectable == null || EventSystem.current == null)
            return;

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(selectable.gameObject);
    }
}
