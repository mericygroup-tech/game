using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class S02CutsceneController : MonoBehaviour
{
    public Transform player;
    public Camera mainCamera;
    public ThirdPersonCamera thirdPersonCamera;
    public PlayerController3D playerController;
    public PlayerCombat3D playerCombat;
    public S01WarningTextUI warningUI;
    public TMP_Text interactionText;
    public Transform timeRift;

    public float cameraMoveSpeed = 2.8f;
    public float lookAtHeight = 1.4f;
    public KeyCode skipKey = KeyCode.Space;
    public KeyCode alternateSkipKey = KeyCode.Escape;
    public string skipPrompt = "Space / Esc de bo qua";
    public string introSubtitle = "Văn An tỉnh dậy trong bóng tối dưới lòng thành phố.";

    private Image fadeImage;
    private TMP_Text skipPromptText;
    private TMP_Text subtitleText;
    private Vector3 savedCameraPosition;
    private Quaternion savedCameraRotation;
    private bool savedPlayerControllerEnabled;
    private bool savedThirdPersonCameraEnabled;
    private bool skipRequested;
    private readonly List<MinionChase3D> pausedEnemies = new List<MinionChase3D>();

    public IEnumerator PlayIntro()
    {
        CacheReferences();
        BeginCutscene(false);
        yield return Fade(1f, 1f, 0f);
        if (FinishIfSkipped())
            yield break;

        if (player != null)
            player.rotation = Quaternion.LookRotation(Vector3.forward);

        SetCamera(new Vector3(-4.5f, 2.2f, -3f), player != null ? player.position + Vector3.up * 1.1f : new Vector3(0f, 1.1f, 0f));
        yield return Fade(1f, 0f, 1.6f);
        if (FinishIfSkipped())
            yield break;

        ShowCutsceneSubtitle(introSubtitle);
        yield return SkippableWait(5.4f);
        if (FinishIfSkipped())
            yield break;

        yield return MoveCamera(
            new Vector3(-4.5f, 2.2f, -3f),
            new Vector3(4.4f, 3.1f, 14f),
            player != null ? player.position + Vector3.up * 1.2f : new Vector3(0f, 1.2f, 0f),
            new Vector3(0f, 1.7f, 22f),
            5.4f);
        if (FinishIfSkipped())
            yield break;

        ShowCutsceneSubtitle("Không thể tấn công. Tìm lối ra.");
        ShowWarning("Không thể tấn công. Tìm lối ra.", 5f);
        yield return SkippableWait(3f);
        if (FinishIfSkipped())
            yield break;

        EndCutscene();
    }

    public IEnumerator PlayBlackStarDescent()
    {
        CacheReferences();
        BeginCutscene(false);
        Transform descentHole = FindTransform("HacTinh_Descent_Hole");
        Vector3 focus = descentHole != null ? descentHole.position : new Vector3(1f, 5f, 88f);

        SetCamera(focus + new Vector3(-7f, -1.1f, -8f), focus);
        ShowCutsceneSubtitle("Tiếng đá nứt vang xuống từ hố sụp phía trên.");
        yield return SkippableWait(2.2f);
        if (FinishIfSkipped())
            yield break;

        yield return ShakeCamera(1.8f, 0.14f);
        if (FinishIfSkipped())
            yield break;

        PulseNamedLight("HacTinh_Descent_DarkPurpleLight", 5.8f, 18f);
        yield return MoveCamera(
            focus + new Vector3(-7f, -1.1f, -8f),
            focus + new Vector3(6f, -0.4f, -6f),
            focus,
            focus + Vector3.down * 1.5f,
            2.8f);
        if (FinishIfSkipped())
            yield break;

        ShowCutsceneSubtitle("Hắc Tinh đã xuống hang. Chạy tới ánh sáng phía trước!");
        ShowWarning("Hắc Tinh đã xuống hang. Chạy tới ánh sáng phía trước!", 4.8f);
        yield return SkippableWait(2.4f);
        if (FinishIfSkipped())
            yield break;

        EndCutscene();
    }

    public IEnumerator PlayResonanceUnlock()
    {
        CacheReferences();
        BeginCutscene(false);
        Vector3 riftFocus = GetTimeRiftFocus();

        HideInteractionText();
        SetCamera(riftFocus + new Vector3(0f, 2.2f, -10f), riftFocus + Vector3.up * 1.5f);
        ShowCutsceneSubtitle("TimeRift phản ứng với Văn An.");
        PulseNamedLight("TimeRift_PointLight", 9f, 24f);
        PulseNamedLight("TimeRift_Blue_CoreLight", 7f, 20f);
        yield return OrbitCamera(riftFocus, 9.5f, 3.1f, 6.2f);
        if (FinishIfSkipped())
            yield break;

        ShowCutsceneSubtitle("Năng lượng cộng hưởng tạm thời mở khóa phản kích.");
        yield return ShakeCamera(1.1f, 0.08f);
        if (FinishIfSkipped())
            yield break;

        yield return SkippableWait(2.4f);
        if (FinishIfSkipped())
            yield break;

        EndCutscene();
    }

    public IEnumerator PlayEnding(string nextSceneName)
    {
        CacheReferences();
        BeginCutscene(false);
        Vector3 riftFocus = GetTimeRiftFocus();

        SetCamera(riftFocus + new Vector3(-6f, 3f, -8f), riftFocus + Vector3.up * 1.6f);
        ShowCutsceneSubtitle("Khe nứt không ổn định nữa!");
        PulseNamedLight("TimeRift_PointLight", 12f, 30f);
        PulseNamedLight("TimeRift_Blue_CoreLight", 10f, 24f);
        yield return ShakeCamera(1.8f, 0.16f);
        if (LoadNextSceneIfSkipped(nextSceneName))
            yield break;

        ShowCutsceneSubtitle("Tất cả bị kéo vào dòng chảy thời gian...");
        yield return OrbitCamera(riftFocus, 8f, 2.6f, 4.2f);
        if (LoadNextSceneIfSkipped(nextSceneName))
            yield break;

        yield return Fade(0f, 1f, 1.6f);
        LoadNextScene(nextSceneName);
    }

    private void CacheReferences()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                player = playerObject.transform;
        }

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (thirdPersonCamera == null && mainCamera != null)
            thirdPersonCamera = mainCamera.GetComponent<ThirdPersonCamera>();

        if (playerController == null && player != null)
            playerController = player.GetComponent<PlayerController3D>();

        if (playerCombat == null && player != null)
            playerCombat = player.GetComponent<PlayerCombat3D>();

        if (warningUI == null)
            warningUI = FindAnyObjectByType<S01WarningTextUI>();

        if (timeRift == null)
        {
            GameObject timeRiftObject = GameObject.Find("TimeRift");
            if (timeRiftObject != null)
                timeRift = timeRiftObject.transform;
        }

        EnsureFadeImage();
        EnsureSkipPrompt();
        EnsureSubtitle();
    }

    private void BeginCutscene(bool allowCombat)
    {
        skipRequested = false;
        ShowSkipPrompt();

        if (mainCamera != null)
        {
            savedCameraPosition = mainCamera.transform.position;
            savedCameraRotation = mainCamera.transform.rotation;
        }

        if (playerController != null)
        {
            savedPlayerControllerEnabled = playerController.enabled;
            playerController.enabled = false;
        }

        if (thirdPersonCamera != null)
        {
            savedThirdPersonCameraEnabled = thirdPersonCamera.enabled;
            thirdPersonCamera.enabled = false;
        }

        if (playerCombat != null)
            playerCombat.enabled = allowCombat;

        PauseActiveEnemies();
    }

    private void EndCutscene()
    {
        HideCutsceneSubtitle();
        HideSkipPrompt();
        skipRequested = false;
        RestorePausedEnemies();

        if (thirdPersonCamera != null)
            thirdPersonCamera.enabled = savedThirdPersonCameraEnabled;

        if (playerController != null)
            playerController.enabled = savedPlayerControllerEnabled;

        if (mainCamera != null && thirdPersonCamera == null)
        {
            mainCamera.transform.position = savedCameraPosition;
            mainCamera.transform.rotation = savedCameraRotation;
        }
    }

    private void PauseActiveEnemies()
    {
        pausedEnemies.Clear();

        MinionChase3D[] enemies = FindObjectsByType<MinionChase3D>(FindObjectsInactive.Exclude);
        foreach (MinionChase3D enemy in enemies)
        {
            if (enemy == null || !enemy.enabled)
                continue;

            enemy.enabled = false;
            pausedEnemies.Add(enemy);
        }
    }

    private void RestorePausedEnemies()
    {
        foreach (MinionChase3D enemy in pausedEnemies)
        {
            if (enemy != null)
                enemy.enabled = true;
        }

        pausedEnemies.Clear();
    }

    private IEnumerator MoveCamera(Vector3 fromPosition, Vector3 toPosition, Vector3 fromLookAt, Vector3 toLookAt, float duration)
    {
        if (mainCamera == null)
            yield break;

        float elapsed = 0f;
        while (elapsed < duration && !skipRequested)
        {
            CheckSkipInput();
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            Vector3 position = Vector3.Lerp(fromPosition, toPosition, t);
            Vector3 lookAt = Vector3.Lerp(fromLookAt, toLookAt, t);
            SetCamera(position, lookAt);
            yield return null;
        }
    }

    private IEnumerator OrbitCamera(Vector3 center, float radius, float height, float duration)
    {
        if (mainCamera == null)
            yield break;

        float elapsed = 0f;
        while (elapsed < duration && !skipRequested)
        {
            CheckSkipInput();
            elapsed += Time.deltaTime;
            float angle = Mathf.Lerp(-35f, 210f, elapsed / duration) * Mathf.Deg2Rad;
            Vector3 position = center + new Vector3(Mathf.Sin(angle) * radius, height, Mathf.Cos(angle) * radius);
            SetCamera(position, center + Vector3.up * lookAtHeight);
            yield return null;
        }
    }

    private IEnumerator ShakeCamera(float duration, float strength)
    {
        if (mainCamera == null)
            yield break;

        Vector3 basePosition = mainCamera.transform.position;
        Quaternion baseRotation = mainCamera.transform.rotation;
        float elapsed = 0f;

        while (elapsed < duration && !skipRequested)
        {
            CheckSkipInput();
            elapsed += Time.deltaTime;
            Vector3 offset = Random.insideUnitSphere * strength;
            offset.y *= 0.45f;
            mainCamera.transform.position = basePosition + offset;
            mainCamera.transform.rotation = baseRotation * Quaternion.Euler(Random.Range(-1.2f, 1.2f), Random.Range(-1.2f, 1.2f), Random.Range(-1f, 1f));
            yield return null;
        }

        mainCamera.transform.position = basePosition;
        mainCamera.transform.rotation = baseRotation;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        EnsureFadeImage();
        if (fadeImage == null)
            yield break;

        float elapsed = 0f;
        while (elapsed < duration && !skipRequested)
        {
            CheckSkipInput();
            elapsed += Time.deltaTime;
            SetFadeAlpha(Mathf.Lerp(from, to, elapsed / duration));
            yield return null;
        }

        SetFadeAlpha(to);
    }

    private IEnumerator SkippableWait(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration && !skipRequested)
        {
            CheckSkipInput();
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private void CheckSkipInput()
    {
        if (Input.GetKeyDown(skipKey) || Input.GetKeyDown(alternateSkipKey))
            skipRequested = true;
    }

    private bool FinishIfSkipped()
    {
        if (!skipRequested)
            return false;

        EndCutscene();
        return true;
    }

    private bool LoadNextSceneIfSkipped(string nextSceneName)
    {
        if (!skipRequested)
            return false;

        HideSkipPrompt();
        HideCutsceneSubtitle();
        LoadNextScene(nextSceneName);
        return true;
    }

    private void LoadNextScene(string nextSceneName)
    {
        if (Application.CanStreamedLevelBeLoaded(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
        else
            Debug.LogWarning("S02 next scene is missing from Build Settings: " + nextSceneName);
    }

    private void SetCamera(Vector3 position, Vector3 lookAt)
    {
        if (mainCamera == null)
            return;

        mainCamera.transform.position = position;
        mainCamera.transform.LookAt(lookAt);
    }

    private Vector3 GetTimeRiftFocus()
    {
        if (timeRift != null)
            return timeRift.position;

        GameObject timeRiftObject = GameObject.Find("TimeRift");
        if (timeRiftObject != null)
            return timeRiftObject.transform.position;

        return new Vector3(0f, 2.5f, 166f);
    }

    private Transform FindTransform(string objectName)
    {
        GameObject sceneObject = GameObject.Find(objectName);
        return sceneObject != null ? sceneObject.transform : null;
    }

    private void PulseNamedLight(string objectName, float intensity, float range)
    {
        GameObject lightObject = GameObject.Find(objectName);
        if (lightObject == null)
            return;

        Light light = lightObject.GetComponent<Light>();
        if (light == null)
            return;

        light.intensity = Mathf.Max(light.intensity, intensity);
        light.range = Mathf.Max(light.range, range);
    }

    private void ShowStory(string message, float duration)
    {
        if (warningUI != null)
            warningUI.ShowStory(message, duration);
    }

    private void ShowWarning(string message, float duration)
    {
        if (warningUI != null)
            warningUI.ShowWarning(message, duration);
    }

    private void HideInteractionText()
    {
        if (interactionText != null)
            interactionText.gameObject.SetActive(false);
    }

    private void ShowCutsceneSubtitle(string message)
    {
        EnsureSubtitle();
        if (subtitleText == null)
            return;

        subtitleText.text = message;
        subtitleText.gameObject.SetActive(true);
        subtitleText.transform.SetAsLastSibling();
    }

    private void HideCutsceneSubtitle()
    {
        if (subtitleText != null)
            subtitleText.gameObject.SetActive(false);
    }

    private void EnsureFadeImage()
    {
        if (fadeImage != null)
            return;

        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
            return;

        GameObject fadeObject = GameObject.Find("S02_CutsceneFade");
        if (fadeObject == null)
        {
            fadeObject = new GameObject("S02_CutsceneFade");
            fadeObject.transform.SetParent(canvas.transform, false);
        }

        fadeImage = fadeObject.GetComponent<Image>();
        if (fadeImage == null)
            fadeImage = fadeObject.AddComponent<Image>();

        fadeImage.color = Color.clear;
        fadeImage.raycastTarget = false;

        RectTransform rect = fadeImage.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        fadeObject.transform.SetAsLastSibling();
    }

    private void EnsureSkipPrompt()
    {
        if (skipPromptText != null)
            return;

        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
            return;

        GameObject promptObject = GameObject.Find("S02_CutsceneSkipPrompt");
        if (promptObject == null)
        {
            promptObject = new GameObject("S02_CutsceneSkipPrompt");
            promptObject.transform.SetParent(canvas.transform, false);
        }

        skipPromptText = promptObject.GetComponent<TextMeshProUGUI>();
        if (skipPromptText == null)
            skipPromptText = promptObject.AddComponent<TextMeshProUGUI>();

        skipPromptText.fontSize = 22;
        skipPromptText.alignment = TextAlignmentOptions.Right;
        skipPromptText.color = new Color(1f, 1f, 1f, 0.82f);
        skipPromptText.raycastTarget = false;
        skipPromptText.text = skipPrompt;

        RectTransform rect = skipPromptText.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-34f, 28f);
        rect.sizeDelta = new Vector2(520f, 50f);
        promptObject.SetActive(false);
        promptObject.transform.SetAsLastSibling();
    }

    private void EnsureSubtitle()
    {
        if (subtitleText != null)
            return;

        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
            return;

        GameObject subtitleObject = GameObject.Find("S02_CutsceneSubtitle");
        if (subtitleObject == null)
        {
            subtitleObject = new GameObject("S02_CutsceneSubtitle");
            subtitleObject.transform.SetParent(canvas.transform, false);
        }

        subtitleText = subtitleObject.GetComponent<TextMeshProUGUI>();
        if (subtitleText == null)
            subtitleText = subtitleObject.AddComponent<TextMeshProUGUI>();

        subtitleText.fontSize = 32;
        subtitleText.alignment = TextAlignmentOptions.Center;
        subtitleText.color = new Color(0.92f, 0.98f, 1f, 1f);
        subtitleText.raycastTarget = false;
        subtitleText.textWrappingMode = TextWrappingModes.Normal;
        subtitleText.outlineWidth = 0.22f;
        subtitleText.outlineColor = new Color(0f, 0f, 0f, 0.88f);
        subtitleText.text = string.Empty;

        RectTransform rect = subtitleText.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 92f);
        rect.sizeDelta = new Vector2(1320f, 112f);

        subtitleObject.SetActive(false);
        subtitleObject.transform.SetAsLastSibling();
    }

    private void ShowSkipPrompt()
    {
        EnsureSkipPrompt();
        if (skipPromptText == null)
            return;

        skipPromptText.text = skipPrompt;
        skipPromptText.gameObject.SetActive(true);
    }

    private void HideSkipPrompt()
    {
        if (skipPromptText != null)
            skipPromptText.gameObject.SetActive(false);
    }

    private void SetFadeAlpha(float alpha)
    {
        if (fadeImage == null)
            return;

        Color color = Color.black;
        color.a = Mathf.Clamp01(alpha);
        fadeImage.color = color;
        fadeImage.gameObject.SetActive(alpha > 0.001f);
    }
}
