using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class S02CaveEventController : MonoBehaviour
{
    private const int PreResonanceThreatHP = 9999;
    private const int PreResonanceThreatDamage = 9999;
    private const int ResonanceEnemyHP = 48;
    private const int ResonanceEnemyDamage = 10;
    private const float ResonanceEnemyMoveSpeed = 2.85f;
    private const float ResonanceEnemyAttackCooldown = 1.6f;
    private const float PressureEnemyMoveSpeed = 3.25f;
    private const float SafeSpawnDistanceBehindPlayer = 8f;

    public Transform player;
    public PlayerCombat3D playerCombat;
    public S01WarningTextUI warningUI;
    public TMP_Text interactionText;
    public TMP_Text progressText;
    public GameObject minionPrefab;
    public Transform[] enemySpawnPoints;
    public Transform timeRift;
    public S02CutsceneController cutsceneController;

    public float stabilizeDuration = 24f;
    public float enemySpawnInterval = 5.5f;
    public int maxActiveEnemies = 4;
    public string nextSceneName = "S03";

    private enum FlowStage
    {
        Intro,
        Explore,
        PressureChase,
        ResonanceCombat,
        Complete
    }

    private FlowStage stage = FlowStage.Intro;
    private bool ancientSignsTriggered;
    private bool voicesTriggered;
    private bool descentTriggered;
    private bool playerNearTimeRift;
    private bool resonanceUnlocked;
    private bool stabilizationRunning;
    private bool overloadWarningShown;
    private bool friendsWarningShown;
    private bool warnedMissingEnemyPrefab;
    private bool warnedMissingInteractionText;
    private float stabilizationStartTime;
    private float nextMinionSpawnTime;
    private int spawnedEnemyCounter;

    private void Start()
    {
        CleanupSceneStartRuntimeState();
        FindReferencesIfNeeded();
        ConfigureDiabloStyleCamera();
        EnsureTimeRiftChamberWalkableSurface();
        DisableBlockingTimeRiftVisualColliders();
        SetPlayerCombat(false);
        HideInteractionText();
        HideProgressText();
        StartCoroutine(SceneStartSequence());
    }

    private void Update()
    {
        if (playerNearTimeRift && !resonanceUnlocked)
        {
            PlayerController3D playerCtrl = FindAnyObjectByType<PlayerController3D>();
            bool isLocked = playerCtrl != null && playerCtrl.InputLocked;

            if (!isLocked)
            {
                InputSettingsManager inputSettings = FindAnyObjectByType<InputSettingsManager>();
                KeyCode interactKey = (inputSettings != null && inputSettings.Keyboard != null) ? inputSettings.Keyboard.interact : KeyCode.E;

                if (Input.GetKeyDown(interactKey))
                    ActivateResonance();
            }
        }

        if (stabilizationRunning)
            UpdateStabilization();
    }

    public void TriggerAncientSigns()
    {
        if (ancientSignsTriggered)
            return;

        ancientSignsTriggered = true;
        StartCoroutine(ShowStorySequence(
            "Những hoa văn này... không giống thứ gì trong bảo tàng.",
            "Theo dấu ký hiệu phát sáng trên vách đá."));
    }

    public void TriggerVoices()
    {
        if (voicesTriggered || !ancientSignsTriggered)
            return;

        voicesTriggered = true;
        StartCoroutine(ShowStorySequence(
            "Minh: An! Cậu nghe thấy không?",
            "Giọng nói vọng ra từ sâu trong hang."));
    }

    public void TriggerBlackStarDescent()
    {
        if (descentTriggered || !voicesTriggered)
            return;

        descentTriggered = true;
        stage = FlowStage.PressureChase;
        StartCoroutine(BlackStarDescentSequence());
    }

    public void SetPlayerNearTimeRift(bool near)
    {
        playerNearTimeRift = near;

        if (resonanceUnlocked)
        {
            HideInteractionText();
            return;
        }

        if (near)
        {
            if (!CanUseTimeRift())
            {
                ShowStory("Khe nứt chưa phản ứng. Đi theo dấu sáng sâu hơn trong hang.", 3.2f);
                HideInteractionText();
                return;
            }

            ShowStory("Khe nứt thời gian cộng hưởng khi Văn An đến gần.", 3.5f);
            ShowInteractionText("Nhấn E để cộng hưởng với khe nứt thời gian");
        }
        else
        {
            HideInteractionText();
        }
    }

    private IEnumerator SceneStartSequence()
    {
        if (cutsceneController != null)
        {
            yield return cutsceneController.PlayIntro();
            stage = FlowStage.Explore;
            ShowStory("Ánh sáng xanh yếu ớt dẫn sâu vào lòng đất.", 4.5f);
            yield break;
        }

        yield return new WaitForSeconds(0.35f);
        ShowStory("Văn An: Mình... còn sống sao?", 4.5f);
        yield return new WaitForSeconds(4.7f);
        ShowWarning("Không thể tấn công. Tìm lối ra.", 4.5f);
        yield return new WaitForSeconds(4.7f);
        ShowStory("Ánh sáng xanh yếu ớt dẫn sâu vào lòng đất.", 4.5f);
        stage = FlowStage.Explore;
    }

    private IEnumerator ShowStorySequence(string first, string second)
    {
        ShowStory(first, 3.7f);
        yield return new WaitForSeconds(3.9f);
        ShowStory(second, 3.7f);
    }

    private IEnumerator ShowDescentSequence()
    {
        ShowStory("Tiếng gầm vang xuống từ hố sụp phía trên.", 3.5f);
        yield return new WaitForSeconds(3.7f);
        ShowWarning("Hắc Tinh đã xuống hang. Chạy tới ánh sáng phía trước!", 4.8f);
    }

    private IEnumerator BlackStarDescentSequence()
    {
        if (cutsceneController != null)
            yield return cutsceneController.PlayBlackStarDescent();
        else
            yield return ShowDescentSequence();

        SpawnEnemyAtIndex(0, false);
    }

    private void ActivateResonance()
    {
        if (resonanceUnlocked)
            return;

        if (!CanUseTimeRift())
        {
            ShowStory("TimeRift còn im lặng. Hãy đi theo dấu sáng trong hang.", 3f);
            return;
        }

        resonanceUnlocked = true;
        stage = FlowStage.ResonanceCombat;
        HideInteractionText();
        StartCoroutine(ResonanceSequence());
    }

    private IEnumerator ResonanceSequence()
    {
        if (cutsceneController != null)
        {
            yield return cutsceneController.PlayResonanceUnlock();
        }
        else
        {
            ShowStory("TimeRift phản ứng với Văn An.", 4f);
            yield return new WaitForSeconds(4.2f);
            ShowStory("Năng lượng cộng hưởng tạm thời mở khóa phản kích.", 4.2f);
        }

        SetPlayerCombat(true);
        NormalizeActiveEnemiesForResonance();
        if (cutsceneController == null)
            yield return new WaitForSeconds(4.4f);

        ShowWarning("Bấm chuột trái để đẩy lùi Hắc Tinh. Giữ TimeRift ổn định!", 5.5f);
        StartStabilization();
    }

    private void StartStabilization()
    {
        ConfigureDiabloStyleCamera();
        stabilizationRunning = true;
        stabilizationStartTime = Time.time;
        nextMinionSpawnTime = Time.time + 1.2f;
        ShowProgressText("Ổn định khe nứt: 0%");
        Debug.Log("S02 TimeRift stabilization started.");
    }

    private void UpdateStabilization()
    {
        if (IsPlayerDead())
        {
            stabilizationRunning = false;
            HideProgressText();
            return;
        }

        float elapsed = Time.time - stabilizationStartTime;
        float normalized = Mathf.Clamp01(elapsed / Mathf.Max(0.1f, stabilizeDuration));
        int percent = Mathf.RoundToInt(normalized * 100f);

        ShowProgressText("Ổn định khe nứt: " + percent + "%");

        if (Time.time >= nextMinionSpawnTime)
        {
            if (GetActivePressureEnemyCount() < maxActiveEnemies)
                SpawnEnemyAtIndex(Random.Range(0, GetSafeSpawnCount()), true);

            nextMinionSpawnTime = Time.time + Mathf.Max(1f, enemySpawnInterval);
        }

        if (!overloadWarningShown && normalized >= 0.58f)
        {
            overloadWarningShown = true;
            ShowWarning("Khe nứt đang quá tải!", 4.5f);
        }

        if (!friendsWarningShown && normalized >= 0.82f)
        {
            friendsWarningShown = true;
            ShowStory("Như Ý: An! Nó đang kéo tụi mình vào!", 4.8f);
        }

        if (elapsed >= stabilizeDuration)
            CompleteStabilization();
    }

    private void CompleteStabilization()
    {
        if (!stabilizationRunning)
            return;

        if (IsPlayerDead())
            return;

        stabilizationRunning = false;
        stage = FlowStage.Complete;
        HideProgressText();
        StartCoroutine(CompleteSequence());
    }

    private IEnumerator CompleteSequence()
    {
        SetPlayerCombat(false);
        StopAllPressureEnemiesForEnding();

        if (cutsceneController != null)
        {
            yield return cutsceneController.PlayEnding(nextSceneName);
            yield break;
        }

        ShowStory("Khe nứt không ổn định nữa!", 3.8f);
        yield return new WaitForSeconds(4f);
        ShowStory("Tất cả bị kéo vào dòng chảy thời gian...", 4.5f);
        yield return new WaitForSeconds(2f);

        if (Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.LogWarning("S02 next scene is missing from Build Settings: " + nextSceneName);
        }
    }

    private void StopAllPressureEnemiesForEnding()
    {
        MinionChase3D[] enemies = FindObjectsByType<MinionChase3D>(FindObjectsInactive.Exclude);
        foreach (MinionChase3D enemy in enemies)
        {
            if (enemy == null || !IsS02PressureEnemy(enemy.gameObject))
                continue;

            enemy.enabled = false;

            Collider[] colliders = enemy.GetComponentsInChildren<Collider>();
            foreach (Collider enemyCollider in colliders)
                enemyCollider.enabled = false;

            Rigidbody rigidbody = enemy.GetComponent<Rigidbody>();
            if (rigidbody != null)
            {
                rigidbody.linearVelocity = Vector3.zero;
                rigidbody.angularVelocity = Vector3.zero;
                rigidbody.isKinematic = true;
            }
        }

        Debug.Log("S02 ending cutscene started. Pressure enemies stopped.");
    }

    private bool IsS02PressureEnemy(GameObject enemy)
    {
        return enemy != null &&
               (enemy.name.StartsWith("S02_Minion") || enemy.CompareTag("Enemy"));
    }

    private void SpawnEnemyAtIndex(int spawnIndex, bool resonancePhase)
    {
        if (minionPrefab == null)
        {
            if (!warnedMissingEnemyPrefab)
            {
                warnedMissingEnemyPrefab = true;
                Debug.LogWarning("Minion prefab is missing. S02 continues without pressure enemies.");
            }

            return;
        }

        Transform spawnPoint = GetSpawnPointBehindPlayer(spawnIndex);
        Vector3 spawnPosition = GetSafeSpawnPosition(spawnPoint);
        Quaternion spawnRotation = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;
        GameObject enemy = Instantiate(minionPrefab, spawnPosition, spawnRotation);
        spawnedEnemyCounter++;
        enemy.name = resonancePhase ? "S02_Minion_Resonance_" + spawnedEnemyCounter.ToString("00") : "S02_Minion_Pressure";
        enemy.tag = "Enemy";
        EnsureEnemyCollider(enemy);

        MinionHealth3D health = enemy.GetComponent<MinionHealth3D>();
        if (health == null)
            health = enemy.AddComponent<MinionHealth3D>();

        health.maxHP = resonancePhase ? ResonanceEnemyHP : PreResonanceThreatHP;
        health.currentHP = health.maxHP;
        health.deathDelay = 0.35f;

        MinionChase3D chase = enemy.GetComponent<MinionChase3D>();
        if (chase == null)
            chase = enemy.AddComponent<MinionChase3D>();

        if (player != null)
            chase.target = player;

        chase.chaseRange = 120f;
        chase.attackRange = resonancePhase ? 1.7f : 1.35f;
        chase.moveSpeed = resonancePhase ? ResonanceEnemyMoveSpeed : PressureEnemyMoveSpeed;
        chase.damage = resonancePhase ? ResonanceEnemyDamage : PreResonanceThreatDamage;
        chase.attackCooldown = resonancePhase ? ResonanceEnemyAttackCooldown : 1f;
        chase.separationRadius = Mathf.Max(chase.separationRadius, 1.55f);
        chase.separationStrength = Mathf.Max(chase.separationStrength, 0.62f);
        chase.personalSpaceDistance = Mathf.Max(chase.personalSpaceDistance, 1.35f);
        chase.ResetForSpawn(player);
        ForceEnemyAnimation(enemy);
    }

    private void EnsureEnemyCollider(GameObject enemy)
    {
        if (enemy == null)
            return;

        CapsuleCollider capsule = enemy.GetComponent<CapsuleCollider>();
        if (capsule == null)
            capsule = enemy.AddComponent<CapsuleCollider>();

        capsule.isTrigger = false;
        capsule.center = new Vector3(0f, 1.05f, 0f);
        capsule.radius = 0.45f;
        capsule.height = 2.1f;
    }

    private int GetActivePressureEnemyCount()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        int count = 0;

        foreach (GameObject enemy in enemies)
        {
            if (enemy != null && enemy.name.StartsWith("S02_Minion"))
                count++;
        }

        return count;
    }

    private int GetSafeSpawnCount()
    {
        if (enemySpawnPoints == null || enemySpawnPoints.Length == 0)
            return 1;

        return enemySpawnPoints.Length;
    }

    private Transform GetSpawnPointBehindPlayer(int spawnIndex)
    {
        if (enemySpawnPoints == null || enemySpawnPoints.Length == 0)
            return null;

        int safeIndex = Mathf.Clamp(spawnIndex, 0, enemySpawnPoints.Length - 1);
        Transform preferred = enemySpawnPoints[safeIndex];
        if (IsBehindPlayer(preferred))
            return preferred;

        foreach (Transform spawnPoint in enemySpawnPoints)
        {
            if (IsBehindPlayer(spawnPoint))
                return spawnPoint;
        }

        return preferred;
    }

    private Vector3 GetSafeSpawnPosition(Transform spawnPoint)
    {
        if (spawnPoint != null)
        {
            Vector3 spawnPosition = spawnPoint.position;
            if (player == null || spawnPosition.z <= player.position.z - SafeSpawnDistanceBehindPlayer)
                return SpreadSpawnPosition(spawnPosition);
        }

        if (player != null)
            return SpreadSpawnPosition(player.position + new Vector3(0f, 0.4f, -12f));

        return transform.position;
    }

    private bool IsBehindPlayer(Transform spawnPoint)
    {
        return spawnPoint != null && (player == null || spawnPoint.position.z <= player.position.z - SafeSpawnDistanceBehindPlayer);
    }

    private Vector3 SpreadSpawnPosition(Vector3 basePosition)
    {
        int sideIndex = spawnedEnemyCounter % 3;
        float sideOffset = sideIndex == 0 ? 0f : (sideIndex == 1 ? -2.1f : 2.1f);
        return basePosition + new Vector3(sideOffset, 0f, -0.9f * spawnedEnemyCounter);
    }

    private void NormalizeActiveEnemiesForResonance()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            if (enemy == null || !enemy.name.StartsWith("S02_Minion"))
                continue;

            ForceEnemyAnimation(enemy);

            MinionHealth3D health = enemy.GetComponent<MinionHealth3D>();
            if (health == null)
                health = enemy.AddComponent<MinionHealth3D>();

            health.maxHP = ResonanceEnemyHP;
            health.currentHP = ResonanceEnemyHP;
            health.destroyOnDeath = true;

            MinionChase3D chase = enemy.GetComponent<MinionChase3D>();
            if (chase == null)
                chase = enemy.AddComponent<MinionChase3D>();

            if (player != null)
                chase.target = player;

            chase.chaseRange = 120f;
            chase.attackRange = 1.7f;
            chase.moveSpeed = ResonanceEnemyMoveSpeed;
            chase.damage = ResonanceEnemyDamage;
            chase.attackCooldown = ResonanceEnemyAttackCooldown;
            chase.separationRadius = Mathf.Max(chase.separationRadius, 1.55f);
            chase.separationStrength = Mathf.Max(chase.separationStrength, 0.62f);
            chase.personalSpaceDistance = Mathf.Max(chase.personalSpaceDistance, 1.35f);
        }

        Debug.Log("S02 resonance started. Active Minions normalized to " + ResonanceEnemyHP + " HP.");
    }

    private void ForceEnemyAnimation(GameObject enemy)
    {
        if (enemy == null)
            return;

        MinionChase3D chase = enemy.GetComponent<MinionChase3D>();
        if (chase != null)
        {
            chase.ForceVisualAnimation();
            return;
        }

        Animator[] animators = enemy.GetComponentsInChildren<Animator>(true);
        foreach (Animator animator in animators)
        {
            if (animator == null)
                continue;

            animator.enabled = true;
            animator.applyRootMotion = false;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.speed = 1f;

            if (animator.runtimeAnimatorController != null)
            {
                animator.Rebind();
                animator.Update(0f);
                animator.Play("Run", 0, Random.value);
            }
        }

        if (animators.Length == 0)
            Debug.LogWarning("S02 spawned enemy has no child Animator: " + enemy.name, enemy);
    }

    private void CleanupSceneStartRuntimeState()
    {
        MinionChase3D[] enemies = FindObjectsByType<MinionChase3D>(FindObjectsInactive.Exclude);
        foreach (MinionChase3D enemy in enemies)
        {
            if (enemy == null || !enemy.name.StartsWith("S02_Minion"))
                continue;

            Destroy(enemy.gameObject);
        }

        MinionSpawner3D[] spawners = FindObjectsByType<MinionSpawner3D>(FindObjectsInactive.Exclude);
        foreach (MinionSpawner3D spawner in spawners)
        {
            if (spawner != null)
                spawner.enabled = false;
        }
    }

    private void FindReferencesIfNeeded()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                player = playerObject.transform;
        }

        if (playerCombat == null && player != null)
            playerCombat = player.GetComponent<PlayerCombat3D>();

        if (playerCombat == null && player != null)
        {
            playerCombat = player.gameObject.AddComponent<PlayerCombat3D>();
            Debug.Log("S02 added missing PlayerCombat3D to Player for resonance combat.");
        }

        if (warningUI == null)
            warningUI = FindAnyObjectByType<S01WarningTextUI>();

        if (interactionText == null)
            interactionText = FindTextInScene("InteractionText");

        if (progressText == null)
            progressText = FindTextInScene("WarningText");

        if (cutsceneController == null)
            cutsceneController = FindAnyObjectByType<S02CutsceneController>();

        if (cutsceneController == null)
            cutsceneController = gameObject.AddComponent<S02CutsceneController>();

        cutsceneController.player = player;
        cutsceneController.playerCombat = playerCombat;
        cutsceneController.warningUI = warningUI;
        cutsceneController.interactionText = interactionText;
        cutsceneController.timeRift = timeRift;
    }

    private void DisableBlockingTimeRiftVisualColliders()
    {
        Collider[] colliders = FindObjectsByType<Collider>(FindObjectsInactive.Include);
        foreach (Collider collider in colliders)
        {
            if (collider == null || collider.isTrigger)
                continue;

            if (!IsTimeRiftVisualCollider(collider.transform))
                continue;

            collider.enabled = false;
        }
    }

    private void EnsureTimeRiftChamberWalkableSurface()
    {
        CreateInvisibleWalkableBox(
            "S02_Runtime_TimeRift_PlayableFloor",
            new Vector3(0f, 0f, 164f),
            new Vector3(34f, 0.46f, 38f));

        CreateInvisibleWalkableBox(
            "S02_Runtime_TimeRift_EntryFiller",
            new Vector3(0f, 0f, 154f),
            new Vector3(14f, 0.46f, 16f));
    }

    private void CreateInvisibleWalkableBox(string objectName, Vector3 position, Vector3 scale)
    {
        if (GameObject.Find(objectName) != null)
            return;

        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = objectName;
        box.transform.position = position;
        box.transform.rotation = Quaternion.identity;
        box.transform.localScale = scale;

        Renderer renderer = box.GetComponent<Renderer>();
        if (renderer != null)
            renderer.enabled = false;
    }

    private bool IsTimeRiftVisualCollider(Transform target)
    {
        while (target != null)
        {
            string objectName = target.name;
            if (objectName.StartsWith("Rift_Ring") ||
                objectName.StartsWith("TimeRift_InteractCircle") ||
                objectName.StartsWith("TimeRift_Core") ||
                objectName.StartsWith("TimeRift_InnerLight") ||
                objectName.StartsWith("TimeRiftChamber_MainFloor") ||
                objectName.StartsWith("S02_TimeRift_Core") ||
                objectName.StartsWith("S02_TimeRift_Ring") ||
                objectName.StartsWith("TimeRiftChamber_RaisedPlatform") ||
                IsOldTimeRiftEntranceWall(objectName))
            {
                return true;
            }

            target = target.parent;
        }

        return false;
    }

    private bool IsOldTimeRiftEntranceWall(string objectName)
    {
        return objectName.StartsWith("TimeRiftChamber_RoughWall_08") ||
               objectName.StartsWith("TimeRiftChamber_RoughWall_09") ||
               objectName.StartsWith("TimeRiftChamber_RoughWall_10") ||
               objectName.StartsWith("TimeRiftChamber_RoughWall_11") ||
               objectName.StartsWith("TimeRiftChamber_RoughWall_12");
    }

    private TMP_Text FindTextInScene(string objectName)
    {
        TMP_Text[] texts = Resources.FindObjectsOfTypeAll<TMP_Text>();
        foreach (TMP_Text text in texts)
        {
            if (text.name == objectName && text.gameObject.scene.IsValid())
                return text;
        }

        return null;
    }

    private void SetPlayerCombat(bool enabled)
    {
        FindReferencesIfNeeded();

        if (playerCombat == null)
        {
            Debug.LogWarning("S02 could not enable resonance combat because PlayerCombat3D is missing.");
            return;
        }

        if (enabled)
            ConfigureResonanceCombat();

        playerCombat.enabled = enabled;
        Debug.Log(enabled ? "S02 resonance combat enabled." : "S02 resonance combat disabled.");
    }

    private bool IsPlayerDead()
    {
        if (player == null)
            return false;

        PlayerHealth3D health = player.GetComponent<PlayerHealth3D>();
        return health != null && health.isDead;
    }

    private bool CanUseTimeRift()
    {
        return descentTriggered && (stage == FlowStage.PressureChase || stage == FlowStage.ResonanceCombat);
    }

    private void ConfigureResonanceCombat()
    {
        playerCombat.damage = 24;
        playerCombat.attackRange = 5.4f;
        playerCombat.attackAngle = 105f;
        playerCombat.closeHitRadius = 1.45f;
        playerCombat.attackCooldown = 0.62f;
        playerCombat.knockbackForce = 7f;
        playerCombat.enemyStunDuration = 0.45f;

        if (playerCombat.aimCamera == null)
            playerCombat.aimCamera = Camera.main;
    }

    private void ConfigureDiabloStyleCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
            return;

        ThirdPersonCamera playerCamera = mainCamera.GetComponent<ThirdPersonCamera>();
        if (playerCamera == null)
            return;

        playerCamera.fixedAngle = true;
        playerCamera.fixedYaw = 45f;
        playerCamera.fixedPitch = 58f;
        playerCamera.distance = 4.8f;
        playerCamera.height = 4f;
        playerCamera.smoothSpeed = 8f;
        playerCamera.lockCursor = false;
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

    private void ShowInteractionText(string message)
    {
        if (interactionText == null)
        {
            FindReferencesIfNeeded();
            if (interactionText == null)
            {
                if (!warnedMissingInteractionText)
                {
                    warnedMissingInteractionText = true;
                    Debug.LogWarning("InteractionText not found for S02 TimeRift prompt.");
                }

                return;
            }
        }

        interactionText.text = message;
        interactionText.gameObject.SetActive(true);
    }

    private void HideInteractionText()
    {
        if (interactionText != null)
            interactionText.gameObject.SetActive(false);
    }

    private void ShowProgressText(string message)
    {
        if (progressText == null)
            return;

        progressText.text = message;
        progressText.gameObject.SetActive(true);
    }

    private void HideProgressText()
    {
        if (progressText != null)
            progressText.gameObject.SetActive(false);
    }
}
