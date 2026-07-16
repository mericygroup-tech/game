using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class S03ArenaDirector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private GameObject minionPrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private BlessingManager blessingManager;
    [SerializeField] private BlessingRuntimeController blessingRuntime;
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private TMP_Text statusText;

    [Header("Startup")]
    public bool autoStartOnSceneLoad = true;

    [Header("Wave Tuning")]
    [SerializeField] private int firstWaveEnemyCount = 3;
    [SerializeField] private int enemiesAddedPerWave = 1;
    [SerializeField] private int maxEnemiesPerWave = 12;
    [SerializeField] private int maxWaves;
    [SerializeField] private float arenaRadius = 18f;
    [SerializeField] private float timeBeforeFirstWave = 1.2f;
    [SerializeField] private float timeBetweenWaves = 1.15f;
    [SerializeField] private float enemyHealthPerWave = 8f;
    [SerializeField] private int baseEnemyDamage = 12;
    [SerializeField] private float enemyDamagePerWave = 0f;

    [Header("Spawn Safety")]
    [SerializeField] private bool spawnFirstWaveInFrontOfPlayer = true;
    [SerializeField, Min(1f)] private float firstWaveSpawnMinDistance = 8f;
    [SerializeField, Min(1f)] private float firstWaveSpawnMaxDistance = 12f;
    [SerializeField, Min(0.5f)] private float spawnPointSpreadRadius = 2.35f;
    [SerializeField, Min(0f)] private float enemyActivationDelay = 0.6f;
    [SerializeField, Min(1f)] private float groundProbeHeight = 8f;
    [SerializeField, Min(1f)] private float groundProbeDistance = 24f;

    private readonly List<MinionHealth3D> activeEnemies = new List<MinionHealth3D>();
    private readonly RaycastHit[] groundHits = new RaycastHit[12];
    private int waveIndex;
    private bool running;
    private bool preparedForIntro;
    private bool arenaStarted;
    private float firstWaveDelayOverride = -1f;
    private Coroutine flowRoutine;

    public bool HasArenaStarted => arenaStarted;

    private void Start()
    {
        ResolveReferences();
        if (autoStartOnSceneLoad && !preparedForIntro)
            BeginArena();
        else if (preparedForIntro)
            ClearIntroBlockedLabels();
    }

    private void Update()
    {
        if (!arenaStarted && Input.GetKeyDown(KeyCode.K))
        {
            Debug.Log("S03ArenaDirector: Manual start triggered by key press (K)!");
            BeginArena();
        }
    }

    public void Configure(
        Transform playerTransform,
        GameObject enemyPrefab,
        Transform[] enemySpawnPoints,
        BlessingManager manager,
        BlessingRuntimeController runtime,
        TMP_Text waveLabel,
        TMP_Text statusLabel)
    {
        player = playerTransform;
        minionPrefab = enemyPrefab;
        spawnPoints = enemySpawnPoints;
        blessingManager = manager;
        blessingRuntime = runtime;
        waveText = waveLabel;
        statusText = statusLabel;
    }

    public void ConfigureWaveTuning(
        int firstWaveCount,
        int addedPerWave,
        int maxPerWave,
        int waveLimit,
        float radius,
        float firstWaveDelay,
        float betweenWaveDelay,
        float healthPerWave,
        int enemyDamage,
        float damagePerWave)
    {
        firstWaveEnemyCount = Mathf.Max(1, firstWaveCount);
        enemiesAddedPerWave = Mathf.Max(0, addedPerWave);
        maxEnemiesPerWave = Mathf.Max(firstWaveEnemyCount, maxPerWave);
        maxWaves = Mathf.Max(0, waveLimit);
        arenaRadius = Mathf.Max(6f, radius);
        timeBeforeFirstWave = Mathf.Max(0f, firstWaveDelay);
        timeBetweenWaves = Mathf.Max(0f, betweenWaveDelay);
        enemyHealthPerWave = Mathf.Max(0f, healthPerWave);
        baseEnemyDamage = Mathf.Max(1, enemyDamage);
        enemyDamagePerWave = Mathf.Max(0f, damagePerWave);
    }

    public void PrepareForIntro()
    {
        if (arenaStarted)
            return;

        preparedForIntro = true;
        running = false;
        firstWaveDelayOverride = -1f;
        activeEnemies.Clear();

        if (flowRoutine != null)
        {
            StopCoroutine(flowRoutine);
            flowRoutine = null;
        }

        ResolveReferences();
        ClearIntroBlockedLabels();
    }

    public void BeginArena()
    {
        BeginArena(-1f);
    }

    public void BeginArena(float firstWaveDelay)
    {
        if (arenaStarted || flowRoutine != null)
            return;

        ResolveReferences();
        preparedForIntro = false;
        arenaStarted = true;
        running = true;
        firstWaveDelayOverride = firstWaveDelay;
        GameAudio.PlayMusic(GameMusicState.Combat, 0.7f);
        flowRoutine = StartCoroutine(ArenaFlow());
    }

    public void SetEnemyActivationDelay(float delay)
    {
        enemyActivationDelay = Mathf.Max(0f, delay);
    }

    private IEnumerator ArenaFlow()
    {
        SetStatus("San dau da san sang. Tieu diet toan bo Hac Tinh.");
        float firstWaveDelay = firstWaveDelayOverride >= 0f ? firstWaveDelayOverride : timeBeforeFirstWave;
        firstWaveDelayOverride = -1f;
        if (firstWaveDelay > 0f)
            yield return new WaitForSeconds(firstWaveDelay);

        while (running)
        {
            if (IsPlayerDead())
                yield break;

            if (maxWaves > 0 && waveIndex >= maxWaves)
            {
                SetStatus("Hoan thanh toan bo wave S03. Build cua ban da thanh hinh.");
                GameAudio.PlayVictory();
                yield break;
            }

            waveIndex++;
            yield return StartCoroutine(RunWave(waveIndex));
            if (IsPlayerDead())
                yield break;

            yield return StartCoroutine(OpenBlessingChoice());
            yield return new WaitForSeconds(timeBetweenWaves);
        }
    }

    private IEnumerator RunWave(int currentWave)
    {
        activeEnemies.Clear();
        blessingRuntime?.OnWaveStarted(currentWave);

        float awarenessDelay = blessingRuntime != null ? blessingRuntime.GetAwarenessSpawnDelay() : 0f;
        if (awarenessDelay > 0f)
        {
            SetStatus("Canh Gioi: dot Hac Tinh tiep theo dang ap sat...");
            yield return new WaitForSeconds(awarenessDelay);
        }

        int enemyCount = Mathf.Clamp(
            firstWaveEnemyCount + (currentWave - 1) * enemiesAddedPerWave,
            1,
            Mathf.Max(1, maxEnemiesPerWave));

        if (currentWave == 8)
        {
            enemyCount = 1;
            SetWaveText("WAVE 8: TRÙM CUỐI");
            SetStatus("CẢNH BÁO: Hắc Tinh Vương khổng lồ xuất hiện!");
        }
        else
        {
            SetWaveText("Wave " + currentWave);
            SetStatus("Wave " + currentWave + ": ha " + enemyCount + " ke dich.");
        }

        for (int i = 0; i < enemyCount; i++)
        {
            MinionHealth3D enemy = SpawnEnemy(currentWave, i, enemyCount);
            if (enemy != null)
            {
                activeEnemies.Add(enemy);
                if (currentWave == 8)
                {
                    CreateBossHealthBar(enemy);
                }
            }

            yield return new WaitForSeconds(0.18f);
        }

        while (!IsWaveCleared())
        {
            if (IsPlayerDead())
                yield break;

            SetStatus("Con lai: " + CountAliveEnemies() + " Hac Tinh.");
            yield return new WaitForSeconds(0.35f);
        }

        SetStatus("Wave " + currentWave + " da sach. Chuan bi nhan Chuc Phuc Anh Linh.");
        yield return new WaitForSeconds(0.55f);
    }

    private IEnumerator OpenBlessingChoice()
    {
        if (blessingManager == null)
            yield break;

        bool choiceComplete = false;
        float oldFixedDeltaTime = Time.fixedDeltaTime;
        Time.timeScale = 0f;

        blessingManager.PresentChoices(() => choiceComplete = true);
        while (!choiceComplete)
            yield return null;

        Time.timeScale = 1f;
        Time.fixedDeltaTime = oldFixedDeltaTime > 0f ? oldFixedDeltaTime : 0.02f;
        SetStatus("Chuc phuc da ap dung. Dot tiep theo sap bat dau.");
    }

    private MinionHealth3D SpawnEnemy(int currentWave, int spawnIndex, int enemyCount)
    {
        GameObject enemy = minionPrefab != null
            ? Instantiate(minionPrefab)
            : GameObject.CreatePrimitive(PrimitiveType.Capsule);

        enemy.SetActive(true);

        bool isBoss = (currentWave == 8);

        if (isBoss)
        {
            enemy.name = "S03_BOSS_HacTinh";
            enemy.transform.localScale = Vector3.one * 2.5f;

            CharacterController cc = enemy.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.height *= 2.5f;
                cc.radius *= 2.5f;
                cc.center = new Vector3(cc.center.x, cc.center.y * 2.5f, cc.center.z);
            }

            MinionHealthBarUI healthBar = enemy.GetComponentInChildren<MinionHealthBarUI>(true);
            if (healthBar != null)
            {
                healthBar.gameObject.SetActive(true);
            }
        }
        else
        {
            enemy.name = "S03_Wave" + currentWave.ToString("00") + "_Enemy" + (spawnIndex + 1).ToString("00");
        }

        enemy.tag = "Enemy";
        SetLayerRecursively(enemy, LayerMask.NameToLayer("Enemy"));
        enemy.transform.position = GetSpawnPosition(currentWave, spawnIndex, enemyCount);
        enemy.transform.rotation = Quaternion.LookRotation(GetDirectionToPlayer(enemy.transform.position));

        MinionHealth3D health = enemy.GetComponent<MinionHealth3D>();
        if (health == null)
            health = enemy.AddComponent<MinionHealth3D>();

        int baseHp = Mathf.RoundToInt(health.maxHP + (currentWave - 1) * enemyHealthPerWave);
        health.maxHP = isBoss ? baseHp * 8 : baseHp;
        health.currentHP = health.maxHP;
        health.destroyOnDeath = true;
        health.deathDelay = 0.12f;

        MinionChase3D chase = enemy.GetComponent<MinionChase3D>();
        if (chase == null)
            chase = enemy.AddComponent<MinionChase3D>();

        chase.ResetForSpawn(player);
        chase.target = player;
        chase.chaseRange = arenaRadius * 2f + (blessingRuntime != null ? blessingRuntime.GetEnemyAwarenessRangeBonus() : 0f);
        
        int baseDmg = Mathf.RoundToInt(baseEnemyDamage + (currentWave - 1) * enemyDamagePerWave);
        chase.damage = isBoss ? baseDmg * 2 : baseDmg;
        
        chase.moveSpeed += Mathf.Min(1.8f, currentWave * 0.08f);
        if (isBoss)
        {
            chase.moveSpeed *= 0.85f;
        }

        if (enemyActivationDelay > 0f)
            chase.PauseAI(enemyActivationDelay);

        return health;
    }

    private Vector3 GetSpawnPosition(int currentWave, int index, int enemyCount)
    {
        if (currentWave == 1 && spawnFirstWaveInFrontOfPlayer && TryGetFrontSpawnPosition(index, enemyCount, out Vector3 frontSpawn))
            return frontSpawn;

        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            Transform point = spawnPoints[index % spawnPoints.Length];
            if (point != null)
            {
                Vector3 candidate = point.position + GetSpawnSpreadOffset(index);
                if (TryProjectToGround(candidate, out Vector3 groundedCandidate))
                    return groundedCandidate;

                return candidate;
            }
        }

        float angle = index * 137.5f * Mathf.Deg2Rad;
        Vector3 generated = transform.position + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * arenaRadius;
        return TryProjectToGround(generated, out Vector3 groundedGenerated) ? groundedGenerated : generated;
    }

    private bool TryGetFrontSpawnPosition(int index, int enemyCount, out Vector3 spawnPosition)
    {
        spawnPosition = Vector3.zero;
        if (player == null)
            return false;

        Vector3 forward = player.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude <= 0.001f)
            forward = transform.forward;
        if (forward.sqrMagnitude <= 0.001f)
            forward = Vector3.forward;

        forward.Normalize();
        Vector3 right = new Vector3(forward.z, 0f, -forward.x);
        int safeEnemyCount = Mathf.Max(1, enemyCount);
        float centerIndex = (safeEnemyCount - 1) * 0.5f;
        float lateralOffset = (index - centerIndex) * spawnPointSpreadRadius;
        float distanceT = safeEnemyCount <= 1 ? 0.5f : index / (safeEnemyCount - 1f);
        float distance = Mathf.Lerp(firstWaveSpawnMinDistance, Mathf.Max(firstWaveSpawnMinDistance, firstWaveSpawnMaxDistance), distanceT);
        Vector3 candidate = player.position + forward * distance + right * lateralOffset;

        if (!TryProjectToGround(candidate, out Vector3 groundedCandidate))
            return false;

        Vector3 toPlayer = groundedCandidate - player.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude < firstWaveSpawnMinDistance * firstWaveSpawnMinDistance * 0.64f)
            return false;

        spawnPosition = groundedCandidate;
        return true;
    }

    private Vector3 GetSpawnSpreadOffset(int index)
    {
        if (index <= 0 || spawnPointSpreadRadius <= 0f)
            return Vector3.zero;

        float angle = index * 137.5f * Mathf.Deg2Rad;
        float radius = spawnPointSpreadRadius * Mathf.Sqrt(index);
        return new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * radius;
    }

    private bool TryProjectToGround(Vector3 candidate, out Vector3 groundedPosition)
    {
        Vector3 origin = candidate + Vector3.up * groundProbeHeight;
        int hitCount = Physics.RaycastNonAlloc(
            origin,
            Vector3.down,
            groundHits,
            groundProbeHeight + groundProbeDistance,
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
            if (hit.collider == null)
                continue;

            if (hit.distance >= bestDistance)
                continue;

            if (Vector3.Dot(hit.normal, Vector3.up) < 0.72f)
                continue;

            if (!IsValidGroundCollider(hit.collider))
                continue;

            bestDistance = hit.distance;
            bestHit = hit;
            foundGround = true;
        }

        if (!foundGround)
            return false;

        groundedPosition = new Vector3(candidate.x, bestHit.point.y, candidate.z);
        return true;
    }

    private static bool IsValidGroundCollider(Collider hitCollider)
    {
        if (hitCollider == null)
            return false;

        if (hitCollider.GetComponentInParent<PlayerHealth3D>() != null ||
            hitCollider.GetComponentInParent<MinionHealth3D>() != null ||
            hitCollider.GetComponentInParent<S01ChaseThreat>() != null)
        {
            return false;
        }

        Transform current = hitCollider.transform;
        while (current != null)
        {
            string objectName = current.name;
            if (ContainsGroundBlockerName(objectName))
                return false;

            current = current.parent;
        }

        return true;
    }

    private static bool ContainsGroundBlockerName(string objectName)
    {
        if (string.IsNullOrEmpty(objectName))
            return false;

        return objectName.Contains("Barrier") ||
               objectName.Contains("Fence") ||
               objectName.Contains("Wall") ||
               objectName.Contains("Gate") ||
               objectName.Contains("Cone") ||
               objectName.Contains("Truck") ||
               objectName.Contains("Obstacle") ||
               objectName.Contains("Blocking");
    }

    private Vector3 GetDirectionToPlayer(Vector3 position)
    {
        if (player == null)
            return Vector3.forward;

        Vector3 direction = player.position - position;
        direction.y = 0f;
        return direction.sqrMagnitude <= 0.001f ? Vector3.forward : direction.normalized;
    }

    private bool IsWaveCleared()
    {
        return CountAliveEnemies() <= 0;
    }

    private int CountAliveEnemies()
    {
        int count = 0;
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            MinionHealth3D enemy = activeEnemies[i];
            if (enemy == null)
            {
                activeEnemies.RemoveAt(i);
                continue;
            }

            if (!enemy.IsDead)
                count++;
        }

        return count;
    }

    private bool IsPlayerDead()
    {
        if (player == null)
            return false;

        PlayerHealth3D health = player.GetComponent<PlayerHealth3D>();
        return health != null && health.isDead;
    }

    private void ResolveReferences()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                player = playerObject.transform;
        }

        if (blessingRuntime == null && player != null)
            blessingRuntime = player.GetComponent<BlessingRuntimeController>();

        if (blessingManager == null)
            blessingManager = FindAnyObjectByType<BlessingManager>();
    }

    private void ClearIntroBlockedLabels()
    {
        SetWaveText(string.Empty);
        SetStatus(string.Empty);
    }

    private void SetWaveText(string message)
    {
        if (waveText != null)
            waveText.text = message;
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    private static void SetLayerRecursively(GameObject obj, int layer)
    {
        if (obj == null || layer < 0)
            return;

        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    private void CreateBossHealthBar(MinionHealth3D bossHealth)
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
            return;

        // Create root panel
        GameObject root = new GameObject("S03_BossHealthRoot");
        root.transform.SetParent(canvas.transform, false);
        RectTransform rootRect = root.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 1f);
        rootRect.anchorMax = new Vector2(0.5f, 1f);
        rootRect.pivot = new Vector2(0.5f, 1f);
        rootRect.anchoredPosition = new Vector2(0f, -60f);
        rootRect.sizeDelta = new Vector2(600f, 60f);

        // Add background panel
        Image panel = root.AddComponent<Image>();
        panel.color = new Color(0.02f, 0.01f, 0.01f, 0.75f);
        Outline outline = root.AddComponent<Outline>();
        outline.effectColor = new Color(0.9f, 0.1f, 0.1f, 0.4f);
        outline.effectDistance = new Vector2(1f, -1f);

        // Add Boss Name Text
        GameObject nameObj = new GameObject("BossName");
        nameObj.transform.SetParent(root.transform, false);
        RectTransform nameRect = nameObj.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0.5f, 1f);
        nameRect.anchorMax = new Vector2(0.5f, 1f);
        nameRect.pivot = new Vector2(0.5f, 1f);
        nameRect.anchoredPosition = new Vector2(0f, -5f);
        nameRect.sizeDelta = new Vector2(580f, 22f);
        
        TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.fontSize = 18;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.fontStyle = FontStyles.Bold;
        nameText.color = new Color(1f, 0.85f, 0.2f, 1f); // Gold name
        nameText.text = "HẮC TINH VƯƠNG (TRÙM CUỐI)";

        // Add Slider Object
        GameObject sliderObj = new GameObject("BossSlider");
        sliderObj.transform.SetParent(root.transform, false);
        RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0f, 0f);
        sliderRect.anchorMax = new Vector2(1f, 0f);
        sliderRect.pivot = new Vector2(0.5f, 0f);
        sliderRect.anchoredPosition = new Vector2(0f, 8f);
        sliderRect.sizeDelta = new Vector2(-20f, 20f);

        Slider slider = sliderObj.AddComponent<Slider>();
        slider.transition = Selectable.Transition.None;
        slider.direction = Slider.Direction.LeftToRight;

        // Background of Slider
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(sliderObj.transform, false);
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.04f, 0.05f, 1f);

        // Fill Area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(2f, 2f);
        fillAreaRect.offsetMax = new Vector2(-2f, -2f);

        // Fill Image
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(fillArea.transform, false);
        RectTransform fillRect = fillObj.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        Image fillImg = fillObj.AddComponent<Image>();
        fillImg.color = new Color(0.9f, 0.08f, 0.12f, 1f); // Dark red fill

        slider.fillRect = fillRect;

        // Setup the script
        S03BossHealthUI uiScript = root.AddComponent<S03BossHealthUI>();
        uiScript.Setup(bossHealth, "HẮC TINH VƯƠNG (TRÙM CUỐI)");
    }
}
