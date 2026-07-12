using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Owns the main-menu presentation and scene flow. Visual construction stays in
/// MainMenuBuilder so the generated scene remains reproducible and easy to debug.
/// </summary>
public sealed class MainMenuController : MonoBehaviour
{
    [Header("Scene Flow")]
    [SerializeField] private string startSceneName = "S01";

    [Header("Intro")]
    [SerializeField] private CanvasGroup blackFade;
    [SerializeField] private CanvasGroup logoGroup;
    [SerializeField] private CanvasGroup menuGroup;
    [SerializeField] private CanvasGroup footerGroup;
    [SerializeField] private RectTransform swordRoot;
    [SerializeField, Min(0.8f)] private float introDuration = 2.15f;

    [Header("Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button achievementsButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private Button settingsCloseButton;
    [SerializeField] private Button achievementsCloseButton;

    [Header("Panels")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject achievementsPanel;

    [Header("Text")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text versionText;

    [Header("Audio")]
    [SerializeField] private bool playIntroDrum = true;
    [SerializeField, Range(0f, 1f)] private float introDrumVolume = 0.42f;

    private bool isTransitioning;
    private bool introFinished;
    private Vector2 swordStartPosition;
    private Vector2 swordEndPosition;
    private Vector2 menuEndPosition;
    private Vector3 logoEndScale;
    private AudioSource audioSource;
    private AudioClip introDrumClip;

    public void Configure(
        CanvasGroup blackFade,
        CanvasGroup logoGroup,
        CanvasGroup menuGroup,
        CanvasGroup footerGroup,
        RectTransform swordRoot,
        Button startButton,
        Button settingsButton,
        Button achievementsButton,
        Button exitButton,
        Button settingsCloseButton,
        Button achievementsCloseButton,
        GameObject settingsPanel,
        GameObject achievementsPanel,
        TMP_Text statusText,
        TMP_Text versionText,
        string startSceneName)
    {
        this.blackFade = blackFade;
        this.logoGroup = logoGroup;
        this.menuGroup = menuGroup;
        this.footerGroup = footerGroup;
        this.swordRoot = swordRoot;
        this.startButton = startButton;
        this.settingsButton = settingsButton;
        this.achievementsButton = achievementsButton;
        this.exitButton = exitButton;
        this.settingsCloseButton = settingsCloseButton;
        this.achievementsCloseButton = achievementsCloseButton;
        this.settingsPanel = settingsPanel;
        this.achievementsPanel = achievementsPanel;
        this.statusText = statusText;
        this.versionText = versionText;
        this.startSceneName = string.IsNullOrWhiteSpace(startSceneName) ? "S01" : startSceneName;
    }

    private void Awake()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        AudioListener.volume = 1f;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;

        BindButtons();
        if (versionText != null)
            versionText.text = "v1.0.0";

        ShowPanel(settingsPanel, false);
        ShowPanel(achievementsPanel, false);

        if (swordRoot != null)
        {
            swordEndPosition = swordRoot.anchoredPosition;
            swordStartPosition = swordEndPosition + new Vector2(-32f, 300f);
            swordRoot.anchoredPosition = swordStartPosition;
            swordRoot.localRotation = Quaternion.Euler(0f, 0f, -10f);
        }

        if (logoGroup != null)
        {
            logoEndScale = logoGroup.transform.localScale;
            if (swordRoot != null)
                logoGroup.transform.localScale = logoEndScale * 0.91f;
        }

        if (menuGroup != null)
        {
            RectTransform rect = menuGroup.transform as RectTransform;
            if (rect != null)
            {
                menuEndPosition = rect.anchoredPosition;
                if (swordRoot != null)
                    rect.anchoredPosition = menuEndPosition + new Vector2(0f, -28f);
            }
        }

        SetGroup(blackFade, 1f, false);
        SetGroup(logoGroup, 0f, false);
        SetGroup(menuGroup, 0f, false);
        SetGroup(footerGroup, 0f, false);
    }

    private IEnumerator Start()
    {
        PlayIntroDrum();
        SetStatus("Âm trống Đông Sơn vang lên...");

        // The supplied artwork is already the complete composition. Reveal it
        // immediately and only fade the black cover; this keeps all four
        // hitboxes deterministic even if an Editor domain reload interrupts a
        // longer cinematic coroutine.
        if (logoGroup != null)
            logoGroup.transform.localScale = logoEndScale;
        if (menuGroup != null && menuGroup.transform is RectTransform menuRect)
            menuRect.anchoredPosition = menuEndPosition;

        SetGroup(logoGroup, 1f, true);
        SetGroup(menuGroup, 1f, true);
        SetGroup(footerGroup, 1f, false);
        yield return FadeGroup(blackFade, 1f, 0f, 0.45f);

        introFinished = true;
        SetStatus("Chọn BẮT ĐẦU để bước vào dòng chảy lịch sử.");
        SelectStartButton();
    }

    private void Update()
    {
        if (introFinished && swordRoot != null && !isTransitioning)
        {
            float drift = Mathf.Sin(Time.unscaledTime * 0.65f) * 1.5f;
            swordRoot.anchoredPosition = swordEndPosition + new Vector2(0f, drift);
        }

        if (!Input.GetKeyDown(KeyCode.Escape))
            return;

        if (settingsPanel != null && settingsPanel.activeSelf)
            CloseSettings();
        else if (achievementsPanel != null && achievementsPanel.activeSelf)
            CloseAchievements();
        else if (introFinished)
            ExitGame();
    }

    private void OnDestroy()
    {
        if (audioSource != null)
            audioSource.Stop();
        if (introDrumClip != null)
            Destroy(introDrumClip);
    }

    public void StartGame()
    {
        if (!isTransitioning)
            StartCoroutine(LoadStartScene());
    }

    public void OpenSettings()
    {
        ShowPanel(settingsPanel, true);
        ShowPanel(achievementsPanel, false);
        SetStatus("Cài đặt âm thanh, hình ảnh và điều khiển.");
        SelectButton(settingsCloseButton);
    }

    public void CloseSettings()
    {
        ShowPanel(settingsPanel, false);
        SetStatus("Chọn BẮT ĐẦU để bước vào dòng chảy lịch sử.");
        SelectStartButton();
    }

    public void OpenAchievements()
    {
        ShowPanel(achievementsPanel, true);
        ShowPanel(settingsPanel, false);
        SetStatus("Theo dõi những dấu ấn trên hành trình người anh hùng.");
        SelectButton(achievementsCloseButton);
    }

    public void CloseAchievements()
    {
        ShowPanel(achievementsPanel, false);
        SetStatus("Chọn BẮT ĐẦU để bước vào dòng chảy lịch sử.");
        SelectStartButton();
    }

    public void ExitGame()
    {
        SetStatus("Đang thoát game...");
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void BindButtons()
    {
        Bind(startButton, StartGame);
        Bind(settingsButton, OpenSettings);
        Bind(achievementsButton, OpenAchievements);
        Bind(exitButton, ExitGame);
        Bind(settingsCloseButton, CloseSettings);
        Bind(achievementsCloseButton, CloseAchievements);
    }

    private static void Bind(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
            return;
        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private IEnumerator LoadStartScene()
    {
        isTransitioning = true;
        if (startButton != null)
            startButton.interactable = false;

        SetGroup(menuGroup, 1f, false);
        SetStatus("Đang mở chương đầu tiên...");
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        yield return FadeGroup(blackFade, blackFade != null ? blackFade.alpha : 0f, 1f, 0.5f);

        if (!string.IsNullOrWhiteSpace(startSceneName) && Application.CanStreamedLevelBeLoaded(startSceneName))
        {
            SceneManager.LoadScene(startSceneName, LoadSceneMode.Single);
            yield break;
        }

        Debug.LogError($"[Main Menu] Scene '{startSceneName}' is not included in Build Profiles.", this);
        SetStatus("Không thể tải scene. Hãy kiểm tra Build Profiles.");
        yield return FadeGroup(blackFade, 1f, 0f, 0.25f);
        SetGroup(menuGroup, 1f, true);
        if (startButton != null)
            startButton.interactable = true;
        isTransitioning = false;
        SelectStartButton();
    }

    private IEnumerator AnimateSwordDrop(float duration)
    {
        if (swordRoot == null)
            yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
            float eased = 1f - Mathf.Pow(1f - t, 4f);
            float overshoot = Mathf.Sin(t * Mathf.PI) * 13f * (1f - t);
            swordRoot.anchoredPosition = Vector2.LerpUnclamped(swordStartPosition, swordEndPosition, eased) + new Vector2(0f, -overshoot);
            swordRoot.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-10f, 0f, eased));
            yield return null;
        }

        swordRoot.anchoredPosition = swordEndPosition;
        swordRoot.localRotation = Quaternion.identity;
    }

    private IEnumerator ScaleLogo(float duration)
    {
        if (logoGroup == null)
            yield break;

        Vector3 from = logoEndScale * 0.91f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / duration), 3f);
            logoGroup.transform.localScale = Vector3.LerpUnclamped(from, logoEndScale, t);
            yield return null;
        }
        logoGroup.transform.localScale = logoEndScale;
    }

    private IEnumerator RevealMenu(float duration)
    {
        RectTransform rect = menuGroup != null ? menuGroup.transform as RectTransform : null;
        Vector2 from = menuEndPosition + new Vector2(0f, -28f);
        float elapsed = 0f;
        if (menuGroup != null)
            menuGroup.alpha = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration)), 3f);
            if (menuGroup != null)
                menuGroup.alpha = t;
            if (rect != null)
                rect.anchoredPosition = Vector2.LerpUnclamped(from, menuEndPosition, t);
            yield return null;
        }
    }

    private static IEnumerator FadeGroup(CanvasGroup group, float from, float to, float duration)
    {
        if (group == null)
            yield break;

        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.01f, duration);
        group.alpha = from;
        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / safeDuration)));
            yield return null;
        }
        group.alpha = to;
    }

    private static void ShowPanel(GameObject panel, bool show)
    {
        if (panel != null)
            panel.SetActive(show);
    }

    private void SelectStartButton()
    {
        SelectButton(startButton);
    }

    private static void SelectButton(Button button)
    {
        if (button != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(button.gameObject);
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    private void PlayIntroDrum()
    {
        if (!playIntroDrum || audioSource == null)
            return;
        introDrumClip = CreateIntroDrumClip();
        audioSource.PlayOneShot(introDrumClip, introDrumVolume);
    }

    private static AudioClip CreateIntroDrumClip()
    {
        const int sampleRate = 44100;
        const float duration = 0.92f;
        int sampleCount = Mathf.RoundToInt(sampleRate * duration);
        float[] data = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float attack = Mathf.Clamp01(t / 0.014f);
            float decay = Mathf.Exp(-5.1f * t);
            float pitch = Mathf.Lerp(102f, 47f, t / duration);
            float body = Mathf.Sin(2f * Mathf.PI * pitch * t);
            float lowBody = Mathf.Sin(2f * Mathf.PI * 52f * t) * 0.5f;
            float noise = (Mathf.PerlinNoise(t * 96f, 0.37f) - 0.5f) * 0.2f;
            data[i] = (body * 0.73f + lowBody + noise) * attack * decay;
        }

        AudioClip clip = AudioClip.Create("MainMenu_DongSonDrum", sampleCount, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static void SetGroup(CanvasGroup group, float alpha, bool interactive)
    {
        if (group == null)
            return;
        group.alpha = alpha;
        group.interactable = interactive;
        group.blocksRaycasts = interactive;
    }
}
