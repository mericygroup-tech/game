using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class PlayerHealth3D : MonoBehaviour
{
    public int maxHP = 100;
    public int currentHP;

    public bool isDead = false;
    public GameObject gameOverUI;
    public float damageSlowDuration = 0.5f;
    public float damageSlowMultiplier = 0.45f;

    public float DamageReduction { get; set; }
    public int ReviveCharges { get; private set; }
    public int ShieldPoints { get; private set; }

    private PlayerController3D playerController;
    private PlayerCombat3D playerCombat;
    private PlayerAnimatorDriver animatorDriver;
    private CharacterController characterController;
    private readonly RaycastHit[] groundHits = new RaycastHit[16];
    private Coroutine gameOverFollowRoutine;

    private void Awake()
    {
        currentHP = maxHP;

        playerController = GetComponent<PlayerController3D>();
        playerCombat = GetComponent<PlayerCombat3D>();
        animatorDriver = GetComponent<PlayerAnimatorDriver>();
        characterController = GetComponent<CharacterController>();

        if (gameOverUI != null)
            gameOverUI.SetActive(false);
    }

    private void Update()
    {
        if (isDead && Input.GetKeyDown(KeyCode.R))
        {
            RestartScene();
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead)
            return;

        if (playerController != null && playerController.IsDashing && playerController.dashInvincible)
        {
            Debug.Log("PlayerHealth3D: Ignored damage due to Dash I-Frames!");
            return;
        }

        int remainingDamage = Mathf.Max(0, Mathf.RoundToInt(damage * (1f - Mathf.Clamp(DamageReduction, 0f, 0.8f))));
        if (ShieldPoints > 0)
        {
            int absorbed = Mathf.Min(ShieldPoints, remainingDamage);
            ShieldPoints -= absorbed;
            remainingDamage -= absorbed;
        }

        currentHP -= remainingDamage;

        if (currentHP < 0)
            currentHP = 0;

        Debug.Log("Player HP: " + currentHP);

        if (currentHP <= 0)
        {
            Die();
            return;
        }

        if (animatorDriver != null)
            animatorDriver.PlayHit();

        if (playerController != null)
            playerController.ApplyTemporarySlow(damageSlowMultiplier, damageSlowDuration);
    }

    private void Die()
    {
        if (isDead)
            return;

        if (ReviveCharges > 0)
        {
            ReviveCharges--;
            currentHP = Mathf.Max(1, Mathf.RoundToInt(maxHP * 0.5f));
            Debug.Log("PlayerHealth3D: Nữ Vương đã hồi sinh người chơi.");
            return;
        }

        isDead = true;
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        S01Soundscape.StopActionSounds();
        CancelSceneTransitions();
        SnapToGround();
        MoveCameraToDeathView();
        if (animatorDriver != null)
            animatorDriver.PlayDeath();

        Debug.Log("Player đã bị hạ. Toàn bộ quái dừng lại. Bấm R để chơi lại.");

        // Tắt điều khiển player
        if (playerController != null)
            playerController.enabled = false;

        if (playerCombat != null)
            playerCombat.enabled = false;

        // Tắt toàn bộ spawner
        MinionSpawner3D[] spawners = FindObjectsByType<MinionSpawner3D>(FindObjectsInactive.Exclude);
        foreach (MinionSpawner3D spawner in spawners)
        {
            spawner.enabled = false;
        }

        // Tắt AI của toàn bộ quái đang tồn tại
        MinionChase3D[] enemies = FindObjectsByType<MinionChase3D>(FindObjectsInactive.Exclude);
        foreach (MinionChase3D enemy in enemies)
        {
            enemy.enabled = false;
        }

        // Hiện Game Over
        if (gameOverUI != null)
        {
            gameOverUI.SetActive(true);
            ConfigureGameOverUI();
        }
    }

    public void AddShield(int amount)
    {
        ShieldPoints = Mathf.Max(0, ShieldPoints + amount);
    }

    public void GrantReviveCharges(int amount)
    {
        ReviveCharges = Mathf.Max(0, ReviveCharges + amount);
    }

    public void Heal(int amount)
    {
        if (isDead)
            return;
        currentHP = Mathf.Clamp(currentHP + Mathf.Max(0, amount), 0, maxHP);
    }

    private void CancelSceneTransitions()
    {
        SceneTransitionTrigger[] transitions = FindObjectsByType<SceneTransitionTrigger>(FindObjectsInactive.Include);
        foreach (SceneTransitionTrigger transition in transitions)
        {
            if (transition != null)
                transition.CancelTransition();
        }
    }

    private void RestartScene()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        S01ChaseIntroCutscene.ResetSharedState();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void MoveCameraToDeathView()
    {
        ThirdPersonCamera[] cameras = FindObjectsByType<ThirdPersonCamera>(FindObjectsInactive.Include);
        foreach (ThirdPersonCamera playerCamera in cameras)
        {
            if (playerCamera != null)
                playerCamera.BeginDeathView(transform);
        }
    }

    private void ConfigureGameOverUI()
    {
        TMP_Text[] texts = gameOverUI.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text text in texts)
        {
            if (text == null)
                continue;

            text.fontSize = Mathf.Min(text.fontSize, 34f);
            text.enableAutoSizing = true;
            text.fontSizeMax = 34f;
            text.fontSizeMin = 18f;
            text.alignment = TextAlignmentOptions.Center;
        }

        if (gameOverFollowRoutine != null)
            StopCoroutine(gameOverFollowRoutine);

        gameOverFollowRoutine = StartCoroutine(FollowGameOverUIUnderPlayer());
    }

    private System.Collections.IEnumerator FollowGameOverUIUnderPlayer()
    {
        RectTransform uiRect = gameOverUI != null ? gameOverUI.GetComponent<RectTransform>() : null;
        Canvas canvas = gameOverUI != null ? gameOverUI.GetComponentInParent<Canvas>() : null;
        RectTransform canvasRect = canvas != null ? canvas.GetComponent<RectTransform>() : null;
        Camera mainCamera = Camera.main;

        if (uiRect == null || canvas == null || canvasRect == null || mainCamera == null)
            yield break;

        uiRect.anchorMin = new Vector2(0.5f, 0.5f);
        uiRect.anchorMax = new Vector2(0.5f, 0.5f);
        uiRect.pivot = new Vector2(0.5f, 0.5f);
        uiRect.sizeDelta = new Vector2(420f, 92f);

        for (float elapsed = 0f; elapsed < 1.25f; elapsed += Time.unscaledDeltaTime)
        {
            PositionGameOverUIUnderPlayer(uiRect, canvas, canvasRect, mainCamera);
            yield return null;
        }

        PositionGameOverUIUnderPlayer(uiRect, canvas, canvasRect, mainCamera);
        gameOverFollowRoutine = null;
    }

    private void PositionGameOverUIUnderPlayer(RectTransform uiRect, Canvas canvas, RectTransform canvasRect, Camera mainCamera)
    {
        Vector3 screenPosition = mainCamera.WorldToScreenPoint(transform.position + Vector3.up * 0.25f);
        screenPosition.y -= 96f;

        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, uiCamera, out Vector2 localPoint))
            uiRect.anchoredPosition = localPoint;
    }

    private void SnapToGround()
    {
        Vector3 origin = transform.position + Vector3.up * 4f;
        int hitCount = Physics.RaycastNonAlloc(origin, Vector3.down, groundHits, 12f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        float closestDistance = float.MaxValue;
        bool foundGround = false;
        Vector3 groundPoint = transform.position;

        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider = groundHits[i].collider;
            if (hitCollider == null || hitCollider.transform.IsChildOf(transform))
                continue;

            if (groundHits[i].distance >= closestDistance)
                continue;

            closestDistance = groundHits[i].distance;
            groundPoint = groundHits[i].point;
            foundGround = true;
        }

        if (!foundGround)
            return;

        float rootOffset = 0.02f;
        if (characterController != null)
            rootOffset -= characterController.center.y - characterController.height * 0.5f;

        transform.position = new Vector3(transform.position.x, groundPoint.y + rootOffset, transform.position.z);
    }
}
