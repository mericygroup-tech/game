using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Owns gameplay pause state. This component is scene-scoped so video-only
/// scenes and the Main Menu are never affected.
/// </summary>
[DefaultExecutionOrder(-900)]
[DisallowMultipleComponent]
public sealed class PauseManager : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private PauseMenuUI pauseMenuUI;
    [SerializeField] private PlayerController3D playerController;
    [SerializeField] private PlayerHealth3D playerHealth;
    [SerializeField] private BlessingManager blessingManager;
    [SerializeField] private S03ArenaDirector arenaDirector;

    [Header("Pause Rules")]
    [SerializeField] private bool waitForArenaGameplay = true;
    [SerializeField, Range(0f, 1f)] private float pausedMusicMultiplier = 0.4f;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private bool isPaused;
    private bool sceneChangeInProgress;
    private bool inputWasLocked;
    private bool cursorWasVisible;
    private CursorLockMode cursorLockBeforePause;
    private float fixedDeltaTimeBeforePause = 0.02f;
    private int lastCancelFrame = -1;
    private float nextBlockedWarningTime;

    public static bool IsGamePaused { get; private set; }
    public bool IsPaused => isPaused;

    public void Configure(
        PauseMenuUI menuUI,
        PlayerController3D controller,
        PlayerHealth3D health,
        BlessingManager blessings,
        S03ArenaDirector arena)
    {
        pauseMenuUI = menuUI;
        playerController = controller;
        playerHealth = health;
        blessingManager = blessings;
        arenaDirector = arena;
    }

    private void Awake()
    {
        ResolveReferences();
        pauseMenuUI?.Initialize(this);

        // A previous scene must never leave the new scene frozen.
        IsGamePaused = false;
        isPaused = false;
        GameAudio.SetMusicDuck(1f);
    }

    private void Update()
    {
        if (sceneChangeInProgress || !WasCancelPressedThisFrame())
            return;

        if (pauseMenuUI != null && pauseMenuUI.HandleBackRequest())
        {
            GameAudio.PlayUiClick();
            return;
        }

        if (isPaused)
        {
            GameAudio.PlayUiClick();
            ResumeGame();
            return;
        }

        if (TryPauseGame())
            GameAudio.PlayUiClick();
    }

    public bool TryPauseGame()
    {
        if (!CanPause(out string reason))
        {
            if (!string.IsNullOrEmpty(reason) && Time.unscaledTime >= nextBlockedWarningTime)
            {
                nextBlockedWarningTime = Time.unscaledTime + 1f;
                Debug.Log("[Pause] Pause request ignored: " + reason, this);
            }

            return false;
        }

        inputWasLocked = playerController != null && playerController.InputLocked;
        cursorWasVisible = Cursor.visible;
        cursorLockBeforePause = Cursor.lockState;
        fixedDeltaTimeBeforePause = Time.fixedDeltaTime > 0f ? Time.fixedDeltaTime : 0.02f;

        if (playerController != null)
        {
            playerController.EndDash();
            playerController.SetInputEnabled(false);
        }

        isPaused = true;
        IsGamePaused = true;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        GameAudio.SetMusicDuck(pausedMusicMultiplier);
        pauseMenuUI.ShowPauseMenu();
        return true;
    }

    public void ResumeGame()
    {
        if (!isPaused || sceneChangeInProgress)
            return;

        pauseMenuUI?.CloseAllSubPanels();
        pauseMenuUI?.HidePauseMenu();

        Time.timeScale = 1f;
        Time.fixedDeltaTime = fixedDeltaTimeBeforePause > 0f ? fixedDeltaTimeBeforePause : 0.02f;
        GameAudio.SetMusicDuck(1f);

        if (playerController != null)
            playerController.SetInputEnabled(!inputWasLocked);

        Cursor.lockState = cursorLockBeforePause;
        Cursor.visible = cursorWasVisible;
        isPaused = false;
        IsGamePaused = false;
    }

    public void RequestRestart()
    {
        if (!isPaused || sceneChangeInProgress)
            return;

        pauseMenuUI?.ShowConfirmation(
            "Bạn có chắc muốn chơi lại màn này?",
            RestartCurrentScene);
    }

    public void RequestMainMenu()
    {
        if (!isPaused || sceneChangeInProgress)
            return;

        pauseMenuUI?.ShowConfirmation(
            "Tiến trình của lượt chơi hiện tại sẽ bị mất. Bạn có muốn tiếp tục?",
            ReturnToMainMenu);
    }

    public void RequestQuit()
    {
        if (!isPaused || sceneChangeInProgress)
            return;

        pauseMenuUI?.ShowConfirmation(
            "Bạn có chắc muốn thoát game?",
            QuitGame);
    }

    public bool ValidateConfiguration(out string error)
    {
        if (pauseMenuUI == null)
        {
            error = "PauseMenuUI reference is missing.";
            return false;
        }

        if (playerController == null)
        {
            error = "PlayerController3D reference is missing.";
            return false;
        }

        if (blessingManager == null)
        {
            error = "BlessingManager reference is missing.";
            return false;
        }

        if (waitForArenaGameplay && arenaDirector == null)
        {
            error = "S03ArenaDirector reference is missing.";
            return false;
        }

        if (!pauseMenuUI.ValidateConfiguration(out error))
            return false;

        error = string.Empty;
        return true;
    }

    private bool CanPause(out string reason)
    {
        if (pauseMenuUI == null)
        {
            reason = "Pause UI is not configured.";
            return false;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (scene.name == mainMenuSceneName || scene.name.StartsWith("MainMenu_"))
        {
            reason = "Main Menu does not use gameplay pause.";
            return false;
        }

        if (blessingManager != null && blessingManager.IsSelectionOpen)
        {
            reason = "Blessing Choice is open.";
            return false;
        }

        if (waitForArenaGameplay && arenaDirector != null && !arenaDirector.HasArenaStarted)
        {
            reason = "gameplay has not started yet.";
            return false;
        }

        if (playerHealth != null && playerHealth.isDead)
        {
            reason = "the player is defeated.";
            return false;
        }

        if (playerController != null && playerController.InputLocked)
        {
            reason = "another gameplay modal owns player input.";
            return false;
        }

        if (Time.timeScale <= 0.0001f)
        {
            reason = "another system already paused time.";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    private bool WasCancelPressedThisFrame()
    {
        if (lastCancelFrame == Time.frameCount)
            return false;

        bool pressed = Input.GetKeyDown(KeyCode.Escape) || Input.GetButtonDown("Cancel");
        if (pressed)
            lastCancelFrame = Time.frameCount;

        return pressed;
    }

    private void RestartCurrentScene()
    {
        PrepareForSceneChange();
        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.buildIndex, LoadSceneMode.Single);
    }

    private void ReturnToMainMenu()
    {
        if (!Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
        {
            Debug.LogError("[Pause] Main Menu scene is not included in Build Profiles: " + mainMenuSceneName, this);
            return;
        }

        PrepareForSceneChange();
        SceneManager.LoadScene(mainMenuSceneName, LoadSceneMode.Single);
    }

    private void QuitGame()
    {
        PrepareForSceneChange();
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void PrepareForSceneChange()
    {
        sceneChangeInProgress = true;
        isPaused = false;
        IsGamePaused = false;
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        AudioListener.pause = false;
        GameAudio.SetMusicDuck(1f);

        if (playerController != null)
            playerController.SetInputEnabled(true);

        pauseMenuUI?.SetInteractionEnabled(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void ResolveReferences()
    {
        if (pauseMenuUI == null)
            pauseMenuUI = FindAnyObjectByType<PauseMenuUI>(FindObjectsInactive.Include);
        if (playerController == null)
            playerController = FindAnyObjectByType<PlayerController3D>(FindObjectsInactive.Include);
        if (playerHealth == null && playerController != null)
            playerHealth = playerController.GetComponent<PlayerHealth3D>();
        if (blessingManager == null)
            blessingManager = FindAnyObjectByType<BlessingManager>(FindObjectsInactive.Include);
        if (arenaDirector == null)
            arenaDirector = FindAnyObjectByType<S03ArenaDirector>(FindObjectsInactive.Include);
    }

    private void OnDestroy()
    {
        if (isPaused)
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = fixedDeltaTimeBeforePause > 0f ? fixedDeltaTimeBeforePause : 0.02f;
            GameAudio.SetMusicDuck(1f);
            if (playerController != null)
                playerController.SetInputEnabled(!inputWasLocked);
            Cursor.lockState = cursorLockBeforePause;
            Cursor.visible = cursorWasVisible;
        }

        if (IsGamePaused)
            IsGamePaused = false;
    }
}
