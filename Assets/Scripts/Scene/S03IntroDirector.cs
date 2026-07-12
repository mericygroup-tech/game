using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class S03IntroDirector : MonoBehaviour
{
    private const string LogPrefix = "[S03 Intro] ";

    [Header("Debug")]
    [SerializeField] private bool enableIntro = true;
    [SerializeField] private bool allowSkip = true;
    [SerializeField] private bool disableDuplicatePlayers = true;

    [Header("Player")]
    [SerializeField] private Transform playerEntryPoint;
    [SerializeField] private GameObject playerRoot;
    [SerializeField] private CharacterController playerCharacterController;
    [SerializeField] private PlayerController3D playerController;
    [SerializeField] private PlayerCombat3D playerCombat;
    [SerializeField] private PlayerFallGuard3D playerFallGuard;
    [SerializeField, Min(1f)] private float playerGroundProbeHeight = 18f;
    [SerializeField, Min(1f)] private float playerGroundProbeDistance = 70f;

    [Header("Cameras")]
    [SerializeField] private Camera introCamera;
    [SerializeField] private Camera gameplayCamera;
    [SerializeField] private ThirdPersonCamera gameplayCameraController;
    [SerializeField] private Transform[] introCameraPoints;
    [SerializeField] private AnimationCurve cameraMovementCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("UI")]
    [SerializeField] private CanvasGroup gameplayUI;
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private bool disableFadeObjectWhenClear = true;

    [Header("Arrival")]
    [SerializeField] private Transform arrivalStartPoint;
    [SerializeField] private S03ArrivalLightVFX arrivalVFX;
    [SerializeField] private AudioSource introAudioSource;
    [SerializeField] private AudioClip arrivalSound;
    [SerializeField] private AudioClip impactSound;

    [Header("Arena")]
    [SerializeField] private S03ArenaDirector arenaDirector;
    [SerializeField, Min(0f)] private float arenaFirstWaveDelayAfterIntro = 0f;
    [SerializeField, Min(0f)] private float enemyActivationDelay = 0.65f;

    [Header("Timing")]
    [SerializeField, Min(0f)] private float blackHoldDuration = 0.5f;
    [SerializeField, Min(0.05f)] private float fadeDuration = 0.5f;
    [SerializeField, Min(0.05f)] private float cameraTourDuration = 2.3f;
    [SerializeField, Min(0.05f)] private float arrivalFallbackDuration = 0.7f;
    [SerializeField, Min(0.05f)] private float cameraTransitionDuration = 1.2f;
    [SerializeField, Min(0f)] private float enemyRevealDelay = 0.65f;

    [Header("Gameplay Camera Framing")]
    [SerializeField, Min(0.1f)] private float gameplayCameraDistance = 5.1f;
    [SerializeField] private float gameplayCameraHeight = 2.25f;
    [SerializeField] private float gameplayCameraPitch = 22f;
    [SerializeField] private float gameplayCameraShoulderOffset = 0.45f;
    [SerializeField, Min(0.1f)] private float gameplayCameraDamping = 10f;
    [SerializeField, Range(1f, 120f)] private float gameplayCameraFieldOfView = 55f;

    private readonly RaycastHit[] groundHits = new RaycastHit[16];

    private Coroutine introRoutine;
    private Renderer[] playerRenderers;
    private bool[] playerRendererEnabledStates;
    private CanvasGroupState gameplayUIState;
    private CanvasGroupState fadeUIState;
    private bool savedGameplayCameraControllerEnabled;
    private bool savedGameplayCameraEnabled;
    private bool savedIntroCameraEnabled;
    private bool statesCaptured;
    private bool introRunning;
    private bool introFinished;
    private bool playerShown;
    private bool arenaStartRequested;
    private bool skipLogged;
    private bool warnedMissingPlayerEntryPoint;

    private struct CanvasGroupState
    {
        public bool IsValid;
        public bool WasActive;
        public float Alpha;
        public bool Interactable;
        public bool BlocksRaycasts;
    }

    private void Awake()
    {
        CacheReferences();
        CaptureInitialStates();

        if (!enableIntro)
            return;

        arenaDirector?.PrepareForIntro();
        PrepareInitialIntroState();
    }

    private void Start()
    {
        if (!enableIntro)
        {
            ApplyGameplayCameraSettings();
            ClearFade();
            RestoreGameplayUI();
            return;
        }

        if (introRoutine != null)
            return;

        introRoutine = StartCoroutine(RunIntroSequence());
    }

    private void Update()
    {
        if (!introRunning || introFinished || !allowSkip)
            return;

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape))
        {
            LogSkipOnce();
            FinishIntroSafely();
        }
    }

    private IEnumerator RunIntroSequence()
    {
        introRunning = true;
        Debug.Log(LogPrefix + "Intro started.", this);

        bool completedSequence = false;
        try
        {
            yield return PlacePlayerAtEntryRoutine();
            yield return SkippableWait(blackHoldDuration);
            yield return FadeRoutine(1f, 0f, fadeDuration);
            yield return CameraTourRoutine();
            yield return ArrivalRoutine();
            yield return CameraTransitionRoutine();
            BeginArenaFromIntro();
            yield return SkippableWait(enemyRevealDelay);
            completedSequence = true;
        }
        finally
        {
            if (!introFinished)
            {
                if (!completedSequence && !skipLogged)
                    Debug.LogWarning(LogPrefix + "Intro failed; restoring gameplay state.", this);

                FinishIntroSafely(false);
            }

            introRunning = false;
            introRoutine = null;
        }
    }

    private void CacheReferences()
    {
        PlayerController3D preferredPlayer = ResolvePreferredPlayer();
        if (playerController == null)
            playerController = preferredPlayer;

        if (playerRoot == null && playerController != null)
            playerRoot = playerController.gameObject;

        if (playerRoot == null)
        {
            GameObject taggedPlayer = FindTaggedScenePlayer();
            if (taggedPlayer != null)
                playerRoot = taggedPlayer;
        }

        if (playerController == null && playerRoot != null)
            playerController = playerRoot.GetComponent<PlayerController3D>();
        if (playerCombat == null && playerRoot != null)
            playerCombat = playerRoot.GetComponent<PlayerCombat3D>();
        if (playerCharacterController == null && playerRoot != null)
            playerCharacterController = playerRoot.GetComponent<CharacterController>();
        if (playerFallGuard == null && playerRoot != null)
            playerFallGuard = playerRoot.GetComponent<PlayerFallGuard3D>();

        if (arenaDirector == null)
            arenaDirector = FindAnyObjectByType<S03ArenaDirector>();

        if (gameplayCamera == null)
            gameplayCamera = Camera.main;
        if (gameplayCameraController == null && gameplayCamera != null)
            gameplayCameraController = gameplayCamera.GetComponent<ThirdPersonCamera>();
        if (gameplayCameraController == null)
            gameplayCameraController = FindAnyObjectByType<ThirdPersonCamera>();
        if (gameplayCamera == null && gameplayCameraController != null)
            gameplayCamera = gameplayCameraController.GetComponent<Camera>();

        if (introCamera == null)
            introCamera = FindIntroCameraCandidate();
        if (introCamera == null)
            introCamera = gameplayCamera;

        if (playerEntryPoint == null)
            playerEntryPoint = FindSceneTransform("PlayerEntryPoint");
        if (arrivalStartPoint == null)
            arrivalStartPoint = FindSceneTransform("ArrivalStartPoint");
        if (arrivalVFX == null)
            arrivalVFX = GetComponentInChildren<S03ArrivalLightVFX>(true);
        if (introAudioSource == null)
            introAudioSource = GetComponent<AudioSource>();

        CachePlayerRenderers();
    }

    private PlayerController3D ResolvePreferredPlayer()
    {
        PlayerController3D[] controllers = FindObjectsByType<PlayerController3D>(FindObjectsInactive.Include);
        PlayerController3D preferred = playerController;

        if (preferred == null && playerRoot != null)
            preferred = playerRoot.GetComponent<PlayerController3D>();

        if (preferred == null)
        {
            for (int i = 0; i < controllers.Length; i++)
            {
                PlayerController3D candidate = controllers[i];
                if (candidate == null)
                    continue;

                if (candidate.CompareTag("Player") && candidate.gameObject.activeInHierarchy)
                {
                    preferred = candidate;
                    break;
                }

                if (preferred == null)
                    preferred = candidate;
            }
        }

        if (disableDuplicatePlayers && preferred != null)
            DisableDuplicatePlayers(controllers, preferred);

        return preferred;
    }

    private void DisableDuplicatePlayers(PlayerController3D[] controllers, PlayerController3D preferred)
    {
        for (int i = 0; i < controllers.Length; i++)
        {
            PlayerController3D candidate = controllers[i];
            if (candidate == null || candidate == preferred || !candidate.gameObject.activeSelf)
                continue;

            candidate.gameObject.SetActive(false);
            Debug.LogWarning(LogPrefix + "Disabled duplicate Player object: " + candidate.name, candidate);
        }
    }

    private GameObject FindTaggedScenePlayer()
    {
        PlayerHealth3D[] healthComponents = FindObjectsByType<PlayerHealth3D>(FindObjectsInactive.Include);
        for (int i = 0; i < healthComponents.Length; i++)
        {
            PlayerHealth3D health = healthComponents[i];
            if (health != null && health.CompareTag("Player"))
                return health.gameObject;
        }

        return null;
    }

    private Camera FindIntroCameraCandidate()
    {
        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsInactive.Include);
        for (int i = 0; i < cameras.Length; i++)
        {
            Camera candidate = cameras[i];
            if (candidate != null && candidate != gameplayCamera && candidate.name.Contains("Intro"))
                return candidate;
        }

        return null;
    }

    private Transform FindSceneTransform(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
            return null;

        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];
            if (candidate != null && candidate.name == objectName)
                return candidate;
        }

        return null;
    }

    private void CaptureInitialStates()
    {
        if (statesCaptured)
            return;

        savedGameplayCameraControllerEnabled = gameplayCameraController == null || gameplayCameraController.enabled;
        savedGameplayCameraEnabled = gameplayCamera == null || gameplayCamera.enabled;
        savedIntroCameraEnabled = introCamera != null && introCamera.enabled;
        gameplayUIState = CaptureCanvasGroupState(gameplayUI);
        fadeUIState = CaptureCanvasGroupState(fadeCanvasGroup);
        statesCaptured = true;
    }

    private CanvasGroupState CaptureCanvasGroupState(CanvasGroup group)
    {
        if (group == null)
            return default;

        return new CanvasGroupState
        {
            IsValid = true,
            WasActive = group.gameObject.activeSelf,
            Alpha = group.alpha,
            Interactable = group.interactable,
            BlocksRaycasts = group.blocksRaycasts
        };
    }

    private void CachePlayerRenderers()
    {
        if (playerRoot == null)
            return;

        playerRenderers = playerRoot.GetComponentsInChildren<Renderer>(true);
        playerRendererEnabledStates = new bool[playerRenderers.Length];
        for (int i = 0; i < playerRenderers.Length; i++)
            playerRendererEnabledStates[i] = playerRenderers[i] != null && playerRenderers[i].enabled;
    }

    private void PrepareInitialIntroState()
    {
        SetFadeAlpha(1f);
        HideGameplayUI();
        SetPlayerInput(false);
        SetPlayerGameplayEnabled(false);
        SetPlayerVisible(false);

        if (playerCharacterController != null)
            playerCharacterController.enabled = false;

        playerController?.ResetMotion();
        playerCombat?.ResetCombatState();
        ApplyGameplayCameraSettings();
        ActivateIntroCamera();
    }

    private IEnumerator PlacePlayerAtEntryRoutine()
    {
        PlacePlayerAtEntry(false);
        yield return null;

        if (playerCharacterController != null)
            playerCharacterController.enabled = true;

        Physics.SyncTransforms();
        Debug.Log(LogPrefix + "Player placed at entry point.", this);
    }

    private void PlacePlayerAtEntry(bool enableCharacterController)
    {
        if (playerRoot == null)
        {
            Debug.LogWarning(LogPrefix + "Missing Player reference; continuing without placement.", this);
            return;
        }

        ResolveEntryPose(out Vector3 entryPosition, out Quaternion entryRotation);
        if (playerCharacterController != null)
            playerCharacterController.enabled = false;

        if (playerController != null)
            playerController.TeleportAndReset(entryPosition, entryRotation);
        else
            playerRoot.transform.SetPositionAndRotation(entryPosition, entryRotation);

        Physics.SyncTransforms();

        if (playerFallGuard != null)
        {
            playerFallGuard.Configure(entryPosition, entryPosition.y - 6f);
            playerFallGuard.enabled = true;
        }

        if (playerCharacterController != null)
            playerCharacterController.enabled = enableCharacterController;
    }

    private void ResolveEntryPose(out Vector3 position, out Quaternion rotation)
    {
        if (playerEntryPoint != null)
        {
            position = playerEntryPoint.position;
            rotation = playerEntryPoint.rotation;
        }
        else
        {
            position = GetFallbackEntryPosition();
            rotation = playerRoot != null ? playerRoot.transform.rotation : Quaternion.identity;
            if (!warnedMissingPlayerEntryPoint)
            {
                warnedMissingPlayerEntryPoint = true;
                Debug.LogWarning(LogPrefix + "Missing PlayerEntryPoint; using grounded fallback.", this);
            }
        }

        if (TryProjectPlayerPositionToGround(position, out Vector3 groundedPosition))
            position = groundedPosition;
    }

    private Vector3 GetFallbackEntryPosition()
    {
        if (playerRoot != null && TryProjectPlayerPositionToGround(playerRoot.transform.position, out Vector3 groundedPlayerPosition))
            return groundedPlayerPosition;

        Collider combatFloor = FindNamedCollider("S03_CoLoa_CombatFloor");
        if (combatFloor != null)
        {
            Bounds bounds = combatFloor.bounds;
            return new Vector3(bounds.center.x, bounds.max.y + GetPlayerGroundOffset(), bounds.center.z);
        }

        return playerRoot != null ? playerRoot.transform.position : transform.position;
    }

    private Collider FindNamedCollider(string objectName)
    {
        Collider[] colliders = FindObjectsByType<Collider>(FindObjectsInactive.Include);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider candidate = colliders[i];
            if (candidate != null && candidate.name == objectName)
                return candidate;
        }

        return null;
    }

    private bool TryProjectPlayerPositionToGround(Vector3 candidate, out Vector3 groundedPosition)
    {
        Vector3 origin = candidate + Vector3.up * playerGroundProbeHeight;
        int hitCount = Physics.RaycastNonAlloc(
            origin,
            Vector3.down,
            groundHits,
            playerGroundProbeHeight + playerGroundProbeDistance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);

        groundedPosition = candidate;
        if (hitCount <= 0)
            return false;

        bool foundGround = false;
        RaycastHit bestHit = default;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = groundHits[i];
            groundHits[i] = default;
            if (hit.collider == null || hit.distance >= bestDistance)
                continue;

            if (playerRoot != null && hit.collider.transform.IsChildOf(playerRoot.transform))
                continue;

            if (Vector3.Dot(hit.normal, Vector3.up) < 0.72f)
                continue;

            if (hit.collider.GetComponentInParent<MinionHealth3D>() != null)
                continue;

            bestDistance = hit.distance;
            bestHit = hit;
            foundGround = true;
        }

        if (!foundGround)
            return false;

        groundedPosition = new Vector3(candidate.x, bestHit.point.y + GetPlayerGroundOffset(), candidate.z);
        return true;
    }

    private float GetPlayerGroundOffset()
    {
        if (playerCharacterController == null)
            return 0.02f;

        return 0.02f - (playerCharacterController.center.y - playerCharacterController.height * 0.5f);
    }

    private IEnumerator FadeRoutine(float from, float to, float duration)
    {
        if (fadeCanvasGroup == null)
            yield break;

        fadeCanvasGroup.gameObject.SetActive(true);
        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.01f, duration);
        while (elapsed < safeDuration && !introFinished)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            SetFadeAlpha(Mathf.Lerp(from, to, t));
            yield return null;
        }

        SetFadeAlpha(to);
    }

    private IEnumerator CameraTourRoutine()
    {
        Camera activeIntroCamera = GetIntroCamera();
        if (activeIntroCamera == null)
            yield break;

        int validPointCount = CountValidIntroCameraPoints();
        if (validPointCount >= 2)
        {
            yield return MoveThroughCameraPoints(activeIntroCamera, validPointCount);
            Debug.Log(LogPrefix + "Camera tour completed.", this);
            yield break;
        }

        if (validPointCount == 1)
        {
            Transform point = GetValidIntroCameraPoint(0);
            activeIntroCamera.transform.SetPositionAndRotation(point.position, point.rotation);
            yield return SkippableWait(cameraTourDuration);
            Debug.Log(LogPrefix + "Camera tour completed.", this);
            yield break;
        }

        Debug.LogWarning(LogPrefix + "Missing optional camera points; using fallback arc.", this);
        yield return FallbackCameraArc(activeIntroCamera);
        Debug.Log(LogPrefix + "Camera tour completed.", this);
    }

    private IEnumerator MoveThroughCameraPoints(Camera activeIntroCamera, int validPointCount)
    {
        Transform firstPoint = GetValidIntroCameraPoint(0);
        activeIntroCamera.transform.SetPositionAndRotation(firstPoint.position, firstPoint.rotation);

        float segmentDuration = Mathf.Max(0.05f, cameraTourDuration / Mathf.Max(1, validPointCount - 1));
        for (int i = 1; i < validPointCount && !introFinished; i++)
        {
            Transform previousPoint = GetValidIntroCameraPoint(i - 1);
            Transform nextPoint = GetValidIntroCameraPoint(i);
            yield return MoveCameraBetween(activeIntroCamera, previousPoint.position, previousPoint.rotation, nextPoint.position, nextPoint.rotation, segmentDuration);
        }
    }

    private IEnumerator FallbackCameraArc(Camera activeIntroCamera)
    {
        Vector3 focus = GetEntryFocus();
        Vector3 fromPosition = focus + new Vector3(-8f, 3.2f, -9f);
        Vector3 toPosition = focus + new Vector3(6f, 3.8f, -7f);
        Quaternion fromRotation = Quaternion.LookRotation((focus - fromPosition).normalized, Vector3.up);
        Quaternion toRotation = Quaternion.LookRotation((focus + Vector3.up * 0.6f - toPosition).normalized, Vector3.up);
        yield return MoveCameraBetween(activeIntroCamera, fromPosition, fromRotation, toPosition, toRotation, cameraTourDuration);
    }

    private IEnumerator MoveCameraBetween(
        Camera activeIntroCamera,
        Vector3 fromPosition,
        Quaternion fromRotation,
        Vector3 toPosition,
        Quaternion toRotation,
        float duration)
    {
        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.01f, duration);
        while (elapsed < safeDuration && !introFinished)
        {
            elapsed += Time.deltaTime;
            float t = EvaluateCameraCurve(Mathf.Clamp01(elapsed / safeDuration));
            activeIntroCamera.transform.SetPositionAndRotation(
                Vector3.Lerp(fromPosition, toPosition, t),
                Quaternion.Slerp(fromRotation, toRotation, t));
            yield return null;
        }

        activeIntroCamera.transform.SetPositionAndRotation(toPosition, toRotation);
    }

    private IEnumerator ArrivalRoutine()
    {
        Vector3 impactPosition = GetEntryFocus();
        Vector3 startPosition = arrivalStartPoint != null ? arrivalStartPoint.position : impactPosition + Vector3.up * 13f;
        PlayClip(arrivalSound);

        if (arrivalVFX != null)
        {
            yield return arrivalVFX.PlayArrival(startPosition, impactPosition, OnArrivalImpact);
            Debug.Log(LogPrefix + "Arrival effect played.", this);
            yield break;
        }

        Debug.LogWarning(LogPrefix + "Missing optional arrival VFX; continuing.", this);
        yield return SkippableWait(arrivalFallbackDuration);
        OnArrivalImpact();
    }

    private void OnArrivalImpact()
    {
        if (playerShown)
            return;

        PlayClip(impactSound);
        SetPlayerVisible(true);
        playerShown = true;
    }

    private IEnumerator CameraTransitionRoutine()
    {
        Camera activeIntroCamera = GetIntroCamera();
        if (activeIntroCamera == null || gameplayCamera == null)
            yield break;

        ApplyGameplayCameraSettings();
        GetGameplayCameraPose(out Vector3 targetPosition, out Quaternion targetRotation);
        Vector3 startPosition = activeIntroCamera.transform.position;
        Quaternion startRotation = activeIntroCamera.transform.rotation;
        yield return MoveCameraBetween(activeIntroCamera, startPosition, startRotation, targetPosition, targetRotation, cameraTransitionDuration);

        gameplayCamera.transform.SetPositionAndRotation(targetPosition, targetRotation);
    }

    private IEnumerator SkippableWait(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration && !introFinished)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private void FinishIntroSafely()
    {
        FinishIntroSafely(true);
    }

    private void FinishIntroSafely(bool stopRunningCoroutine)
    {
        if (introFinished)
            return;

        introFinished = true;

        if (stopRunningCoroutine && introRoutine != null)
        {
            Coroutine runningRoutine = introRoutine;
            introRoutine = null;
            StopCoroutine(runningRoutine);
        }

        CacheReferences();
        PlacePlayerAtEntry(true);
        SetPlayerVisible(true);
        playerShown = true;
        RestoreGameplayCamera();
        ClearFade();
        RestoreGameplayUI();
        RestorePlayerGameplay();
        BeginArenaFromIntro();

        Debug.Log(skipLogged ? LogPrefix + "Intro completed after skip." : LogPrefix + "Intro completed.", this);
    }

    private void BeginArenaFromIntro()
    {
        if (arenaStartRequested)
            return;

        arenaStartRequested = true;
        if (arenaDirector == null)
        {
            Debug.LogWarning(LogPrefix + "Missing ArenaDirector; gameplay restored without arena start.", this);
            return;
        }

        arenaDirector.SetEnemyActivationDelay(enemyActivationDelay);
        arenaDirector.BeginArena(arenaFirstWaveDelayAfterIntro);
        Debug.Log(LogPrefix + "Arena started.", this);
    }

    private void RestorePlayerGameplay()
    {
        if (playerCharacterController != null)
            playerCharacterController.enabled = true;

        if (playerController != null)
        {
            playerController.ResetMotion();
            playerController.enabled = true;
            playerController.SetInputEnabled(true);
        }

        if (playerCombat != null)
        {
            playerCombat.ResetCombatState();
            playerCombat.enabled = true;
        }
    }

    private void SetPlayerGameplayEnabled(bool enabled)
    {
        if (playerController != null)
            playerController.enabled = enabled;

        if (playerCombat != null)
        {
            if (!enabled)
                playerCombat.ResetCombatState();

            playerCombat.enabled = enabled;
        }
    }

    private void SetPlayerInput(bool enabled)
    {
        if (playerController != null)
            playerController.SetInputEnabled(enabled);
    }

    private void SetPlayerVisible(bool visible)
    {
        if (playerRenderers == null)
            CachePlayerRenderers();

        if (playerRenderers == null)
            return;

        for (int i = 0; i < playerRenderers.Length; i++)
        {
            Renderer playerRenderer = playerRenderers[i];
            if (playerRenderer == null)
                continue;

            playerRenderer.enabled = visible
                ? playerRendererEnabledStates == null || i >= playerRendererEnabledStates.Length || playerRendererEnabledStates[i]
                : false;
        }
    }

    private void ActivateIntroCamera()
    {
        Camera activeIntroCamera = GetIntroCamera();
        if (activeIntroCamera == null)
            return;

        activeIntroCamera.gameObject.SetActive(true);
        activeIntroCamera.enabled = true;

        if (gameplayCamera != null && gameplayCamera != activeIntroCamera)
            gameplayCamera.enabled = false;

        if (gameplayCameraController != null)
            gameplayCameraController.enabled = false;

        SetSingleMainCamera(activeIntroCamera);
        EnsureSingleAudioListener(activeIntroCamera);
    }

    private void RestoreGameplayCamera()
    {
        if (gameplayCamera == null)
            return;

        ApplyGameplayCameraSettings();
        gameplayCamera.gameObject.SetActive(true);
        gameplayCamera.enabled = savedGameplayCameraEnabled || !statesCaptured;

        if (gameplayCameraController != null)
        {
            gameplayCameraController.enabled = false;
            gameplayCameraController.SnapToDesiredPose();
            gameplayCameraController.enabled = savedGameplayCameraControllerEnabled || !statesCaptured;
        }

        if (introCamera != null && introCamera != gameplayCamera)
            introCamera.enabled = savedIntroCameraEnabled && !enableIntro;

        SetSingleMainCamera(gameplayCamera);
        EnsureSingleAudioListener(gameplayCamera);
    }

    private void ApplyGameplayCameraSettings()
    {
        if (gameplayCameraController != null)
        {
            if (playerRoot != null)
                gameplayCameraController.SetTarget(playerRoot.transform);

            gameplayCameraController.ApplyAdventureFraming(
                gameplayCameraDistance,
                gameplayCameraHeight,
                gameplayCameraPitch,
                gameplayCameraShoulderOffset,
                gameplayCameraDamping);
        }

        if (gameplayCamera != null)
            gameplayCamera.fieldOfView = gameplayCameraFieldOfView;

        if (playerController != null && gameplayCamera != null)
            playerController.cameraTransform = gameplayCamera.transform;

        if (playerCombat != null && gameplayCamera != null)
            playerCombat.aimCamera = gameplayCamera;
    }

    private void GetGameplayCameraPose(out Vector3 position, out Quaternion rotation)
    {
        if (gameplayCameraController != null)
        {
            gameplayCameraController.GetDesiredPose(out position, out rotation);
            return;
        }

        Vector3 focus = GetEntryFocus();
        Vector3 backward = playerRoot != null ? -playerRoot.transform.forward : Vector3.back;
        backward.y = 0f;
        if (backward.sqrMagnitude <= 0.001f)
            backward = Vector3.back;

        backward.Normalize();
        position = focus + backward * gameplayCameraDistance + Vector3.up * gameplayCameraHeight;
        rotation = Quaternion.LookRotation((focus + Vector3.up * 1.4f - position).normalized, Vector3.up);
    }

    private Camera GetIntroCamera()
    {
        return introCamera != null ? introCamera : gameplayCamera;
    }

    private Vector3 GetEntryFocus()
    {
        ResolveEntryPose(out Vector3 position, out _);
        return position + Vector3.up * 1.25f;
    }

    private int CountValidIntroCameraPoints()
    {
        int count = 0;
        if (introCameraPoints == null)
            return count;

        for (int i = 0; i < introCameraPoints.Length; i++)
        {
            if (introCameraPoints[i] != null)
                count++;
        }

        return count;
    }

    private Transform GetValidIntroCameraPoint(int validIndex)
    {
        if (introCameraPoints == null)
            return null;

        int count = 0;
        for (int i = 0; i < introCameraPoints.Length; i++)
        {
            Transform point = introCameraPoints[i];
            if (point == null)
                continue;

            if (count == validIndex)
                return point;

            count++;
        }

        return null;
    }

    private float EvaluateCameraCurve(float t)
    {
        if (cameraMovementCurve == null || cameraMovementCurve.length == 0)
            return Mathf.SmoothStep(0f, 1f, t);

        return Mathf.Clamp01(cameraMovementCurve.Evaluate(t));
    }

    private void HideGameplayUI()
    {
        if (gameplayUI == null)
            return;

        gameplayUI.gameObject.SetActive(true);
        gameplayUI.alpha = 0f;
        gameplayUI.interactable = false;
        gameplayUI.blocksRaycasts = false;
    }

    private void RestoreGameplayUI()
    {
        if (gameplayUI == null)
            return;

        gameplayUI.gameObject.SetActive(true);
        gameplayUI.alpha = 1f;
        gameplayUI.interactable = gameplayUIState.IsValid && gameplayUIState.Interactable;
        gameplayUI.blocksRaycasts = gameplayUIState.IsValid && gameplayUIState.BlocksRaycasts;
    }

    private void SetFadeAlpha(float alpha)
    {
        if (fadeCanvasGroup == null)
            return;

        fadeCanvasGroup.gameObject.SetActive(true);
        fadeCanvasGroup.alpha = Mathf.Clamp01(alpha);
        fadeCanvasGroup.interactable = false;
        fadeCanvasGroup.blocksRaycasts = alpha > 0.001f;
    }

    private void ClearFade()
    {
        if (fadeCanvasGroup == null)
            return;

        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.interactable = false;
        fadeCanvasGroup.blocksRaycasts = false;
        if (disableFadeObjectWhenClear)
            fadeCanvasGroup.gameObject.SetActive(false);
        else if (fadeUIState.IsValid)
            fadeCanvasGroup.gameObject.SetActive(fadeUIState.WasActive);
    }

    private void SetSingleMainCamera(Camera activeCamera)
    {
        if (activeCamera == null)
            return;

        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsInactive.Include);
        for (int i = 0; i < cameras.Length; i++)
        {
            Camera candidate = cameras[i];
            if (candidate == null)
                continue;

            candidate.tag = candidate == activeCamera ? "MainCamera" : "Untagged";
        }
    }

    private void EnsureSingleAudioListener(Camera preferredCamera)
    {
        AudioListener preferred = preferredCamera != null ? preferredCamera.GetComponent<AudioListener>() : null;
        if (preferred == null && gameplayCamera != null)
            preferred = gameplayCamera.GetComponent<AudioListener>();

        AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Include);
        if (preferred == null && listeners.Length > 0)
            preferred = listeners[0];

        for (int i = 0; i < listeners.Length; i++)
        {
            AudioListener listener = listeners[i];
            if (listener != null)
                listener.enabled = listener == preferred;
        }
    }

    private void PlayClip(AudioClip clip)
    {
        if (introAudioSource == null || clip == null)
            return;

        introAudioSource.PlayOneShot(clip);
    }

    private void LogSkipOnce()
    {
        if (skipLogged)
            return;

        skipLogged = true;
        Debug.Log(LogPrefix + "Intro skipped.", this);
    }
}
