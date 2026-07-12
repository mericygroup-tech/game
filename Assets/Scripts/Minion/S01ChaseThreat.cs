using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class S01ChaseThreat : MonoBehaviour
{
    private const string IdleState = "Idle";
    private const string RunState = "Run";
    private const string AttackState = "Attack";
    private const string MoveSpeedParameter = "MoveSpeed";
    private const string AttackParameter = "Attack";

    public Transform player;
    public Transform[] waypoints;

    [HideInInspector] public float startDelay = 0f;
    public float directChaseSpeed = 6f;
    public float waypointSpeed = 5.2f;
    [HideInInspector] public float moveSpeed = 5.2f;
    [HideInInspector] public float catchUpSpeed = 7.5f;
    public float catchDistance = 1.6f;
    public float waypointReachDistance = 0.8f;
    [HideInInspector] public float farFromPlayerDistance = 22f;
    public float rotationSpeed = 10f;

    public float movementStartDistance = 1.2f;
    public bool hideUntilChaseStarts = true;
    public float nearPlayerDistance = 6f;
    public float farPlayerDistance = 22f;
    public float veryFarPlayerDistance = 35f;
    public float farSpeedMultiplier = 1.55f;
    public float veryFarSpeedMultiplier = 2f;
    public float speedMultiplierChangeRate = 1.8f;
    public int catchDamage = 20;
    public float catchAttackImpactDelay = 0.55f;
    public float catchAttackCooldown = 0.85f;
    public float catchAttackEffectDuration = 0.35f;
    public Color catchAttackEffectColor = new Color(0.95f, 0.05f, 0.03f, 0.8f);
    public float tutorialSlowMotionScale = 0.45f;
    public float tutorialSlowMotionDuration = 1.8f;
    public float shiftPromptDistance = 9f;
    public float shiftReleaseExtraDistance = 6f;
    public float earlyThreatSpeed = 2.8f;
    public float earlyMinionSpeed = 1.8f;
    public float fastMinionSpeed = 5.5f;
    public float separationRadius = 1.5f;
    public float separationStrength = 0.45f;
    public float personalSpaceDistance = 1.35f;
    public bool debugLogs = true;
    public bool showPrologueStory = true;
    public string prologueStoryMessage = "Cổ vật trong bảo tàng đang cộng hưởng với một khe nứt lạ.";
    public string prologueWarningMessage = "Ánh sáng đen lan ra sau lưng. Có thứ gì đó vừa tỉnh dậy.";

    private static readonly List<S01ChaseThreat> activeThreats = new List<S01ChaseThreat>();
    private readonly RaycastHit[] visibilityHits = new RaycastHit[24];
    private static readonly Collider[] separationHits = new Collider[14];
    public static IReadOnlyList<S01ChaseThreat> ActiveThreats => activeThreats;

    private float currentSpeedMultiplier = 1f;
    private float suppressCatchUpUntil;
    private float suppressCatchUntil;
    private int waypointIndex;
    private int highestReachedWaypointIndex;
    private int playerRouteWaypointIndex;
    private bool chaseStarted;
    private bool chaseStarting;
    private bool chaseTutorialActive;
    private bool shiftPromptShown;
    private bool shiftSpeedReleased;
    private bool hasCaughtPlayer;
    private bool playerStartPositionSet;
    private Vector3 playerStartPosition;
    private Vector3 chaseStartPlayerPosition;
    private S01WarningTextUI warningUI;
    private Coroutine tutorialSlowMotionRoutine;
    private ChaseMode currentMode = ChaseMode.None;
    private PressureBand currentPressureBand = PressureBand.None;
    private Collider[] selfColliders;
    private Collider[] playerColliders;
    private Renderer[] threatRenderers;
    private Animator[] threatAnimators;

    private enum ChaseMode
    {
        None,
        DirectChase,
        WaypointFollow
    }

    private enum PressureBand
    {
        None,
        Near,
        Normal,
        CatchUp,
        StrongCatchUp
    }

    private void Awake()
    {
        selfColliders = GetComponentsInChildren<Collider>();
        threatRenderers = GetComponentsInChildren<Renderer>();
        threatAnimators = GetComponentsInChildren<Animator>(true);
    }

    private void OnEnable()
    {
        if (!activeThreats.Contains(this))
            activeThreats.Add(this);
    }

    private void OnDisable()
    {
        activeThreats.Remove(this);
    }

    private void OnDestroy()
    {
        activeThreats.Remove(this);
    }

    private void Start()
    {
        FindPlayerIfNeeded();
        FindWaypointsIfNeeded();
        SkipFirstWaypointIfAlreadyThere();
        warningUI = FindAnyObjectByType<S01WarningTextUI>();
        InitializePlayerStartPosition();
        SetThreatVisible(!hideUntilChaseStarts);
        PlayThreatAnimation(IdleState, 0f, true);

        if (warningUI != null)
            ShowOpeningStory();

        Log(player != null
            ? "S01ChaseThreat: Player found: " + player.name
            : "S01ChaseThreat: Player missing.");
        Log("S01ChaseThreat: Waypoint count = " + (waypoints != null ? waypoints.Length : 0));
        Log("S01ChaseThreat: Waiting for player movement.");
        LogCurrentWaypoint();
    }

    private void Update()
    {
        if (hasCaughtPlayer)
            return;

        if (player == null)
            FindPlayerIfNeeded();

        if (player == null)
            return;

        if (!chaseStarted)
        {
            InitializePlayerStartPosition();

            if (!PlayerHasStartedMoving())
                return;

            StartChase();
            return;
        }

        TryCatchPlayer();

        if (hasCaughtPlayer || player == null)
            return;

        UpdateChaseTutorial();
        UpdateRubberBandSpeed();
        UpdateForwardRouteProgress();

        if (CanDirectlyReachPlayer())
            MoveDirectlyTowardPlayer();
        else
            MoveUsingWaypoints();

        TryCatchPlayer();
    }

    private void StartChase()
    {
        if (chaseStarting || chaseStarted)
            return;

        StartCoroutine(StartChaseSequence());
    }

    private IEnumerator StartChaseSequence()
    {
        chaseStarting = true;
        bool playedIntro = false;

        S01ChaseIntroCutscene chaseIntroCutscene = FindAnyObjectByType<S01ChaseIntroCutscene>(FindObjectsInactive.Include);
        if (chaseIntroCutscene != null)
        {
            playedIntro = chaseIntroCutscene.TryPlay(player) || chaseIntroCutscene.IsRunning;

            while (chaseIntroCutscene.IsRunning)
                yield return null;
        }

        BeginChaseNow(playedIntro);
    }

    private void BeginChaseNow(bool playedIntro)
    {
        chaseStarted = true;
        chaseStarting = false;
        chaseTutorialActive = true;
        shiftPromptShown = false;
        shiftSpeedReleased = false;
        chaseStartPlayerPosition = player != null ? player.position : transform.position;
        ApplyMinionSpeed(earlyMinionSpeed);
        SetThreatVisible(true);
        Log("S01 chase started after player movement.");
        if (!playedIntro)
            S01Soundscape.PlayDarkStarRoar();
        S01Soundscape.StartHeartbeat();

        if (warningUI == null)
            warningUI = FindAnyObjectByType<S01WarningTextUI>();

            if (warningUI != null)
                warningUI.ShowWarning("WASD để thoát khỏi bảo tàng. Giữ Shift khi cần bứt tốc.", 4f);

        if (tutorialSlowMotionRoutine != null)
            StopCoroutine(tutorialSlowMotionRoutine);
        tutorialSlowMotionRoutine = StartCoroutine(TutorialSlowMotionWindow());
    }

    private IEnumerator TutorialSlowMotionWindow()
    {
        float previousTimeScale = Time.timeScale;
        float previousFixedDeltaTime = Time.fixedDeltaTime;
        Time.timeScale = Mathf.Clamp(tutorialSlowMotionScale, 0.1f, 1f);
        Time.fixedDeltaTime = previousFixedDeltaTime * Time.timeScale;

        yield return new WaitForSecondsRealtime(Mathf.Max(0f, tutorialSlowMotionDuration));

        Time.timeScale = previousTimeScale;
        Time.fixedDeltaTime = previousFixedDeltaTime;
        tutorialSlowMotionRoutine = null;
    }

    private void UpdateChaseTutorial()
    {
        if (!chaseTutorialActive || player == null)
            return;

        float movedDistance = HorizontalDistance(player.position, chaseStartPlayerPosition);
        ApplyMinionSpeed(shiftSpeedReleased ? fastMinionSpeed : earlyMinionSpeed);

        if (!shiftPromptShown && movedDistance >= shiftPromptDistance)
        {
            shiftPromptShown = true;
            if (warningUI == null)
                warningUI = FindAnyObjectByType<S01WarningTextUI>();
            if (warningUI != null)
                warningUI.ShowWarning("Giữ Shift để bứt tốc qua đoạn nguy hiểm!", 4f);

            shiftSpeedReleased = true;
            chaseTutorialActive = false;
            ApplyMinionSpeed(fastMinionSpeed);
            return;
        }
    }

    private void ApplyMinionSpeed(float speed)
    {
        MinionChase3D[] minions = FindObjectsByType<MinionChase3D>(FindObjectsInactive.Exclude);
        foreach (MinionChase3D minion in minions)
        {
            if (minion != null)
                minion.moveSpeed = speed;
        }
    }

    private bool PlayerHasStartedMoving()
    {
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D))
            return true;

        return playerStartPositionSet &&
               HorizontalDistance(player.position, playerStartPosition) >= movementStartDistance;
    }

    private void ShowOpeningStory()
    {
        if (!showPrologueStory || warningUI == null)
        {
            warningUI.ShowWarning("WASD để di chuyển.", 6f);
            return;
        }

        warningUI.ShowStory(prologueStoryMessage, 4.8f);
        warningUI.ShowWarning(prologueWarningMessage, 5.8f);
    }

    private void InitializePlayerStartPosition()
    {
        if (player == null || playerStartPositionSet)
            return;

        playerStartPosition = player.position;
        playerStartPositionSet = true;
    }

    private void SetThreatVisible(bool visible)
    {
        if (threatRenderers == null)
            return;

        for (int i = 0; i < threatRenderers.Length; i++)
        {
            if (threatRenderers[i] != null)
                threatRenderers[i].enabled = visible;
        }
    }

    private void MoveDirectlyTowardPlayer()
    {
        SetMode(ChaseMode.DirectChase);
        AdvanceWaypointProgressNearThreat();

        Vector3 targetPosition = player.position;
        targetPosition.y = transform.position.y;

        MoveToward(targetPosition, GetPressureSpeed(directChaseSpeed));
    }

    private void MoveUsingWaypoints()
    {
        SetMode(ChaseMode.WaypointFollow);

        if (waypoints == null || waypoints.Length == 0 || waypointIndex >= waypoints.Length)
            return;

        SelectForwardFallbackWaypoint();
        Transform targetWaypoint = GetCurrentValidWaypoint();

        if (targetWaypoint == null)
            return;

        Vector3 targetPosition = targetWaypoint.position;
        targetPosition.y = transform.position.y;

        MoveToward(targetPosition, GetPressureSpeed(waypointSpeed));

        if (HorizontalDistance(transform.position, targetWaypoint.position) <= waypointReachDistance)
        {
            AdvanceWaypointIndex(waypointIndex + 1, false);
            LogCurrentWaypoint();
        }
    }

    private void MoveToward(Vector3 targetPosition, float speed)
    {
        Vector3 currentPosition = transform.position;
        Vector3 nextPosition = Vector3.MoveTowards(currentPosition, targetPosition, speed * Time.deltaTime);
        nextPosition.y = currentPosition.y;

        Vector3 moveDirection = nextPosition - currentPosition;
        Vector3 separation = GetSeparationDirection();
        if (separation.sqrMagnitude > 0.001f && moveDirection.sqrMagnitude > 0.001f)
        {
            Vector3 blendedDirection = (moveDirection.normalized + separation).normalized;
            float stepDistance = Vector3.Distance(currentPosition, nextPosition);
            nextPosition = currentPosition + blendedDirection * stepDistance;
            nextPosition.y = currentPosition.y;
            moveDirection = nextPosition - currentPosition;
        }

        transform.position = nextPosition;
        RotateToward(moveDirection);
        PlayThreatAnimation(moveDirection.sqrMagnitude > 0.0001f ? RunState : IdleState, 1f);
    }

    private Vector3 GetSeparationDirection()
    {
        float radius = Mathf.Max(0f, separationRadius);
        if (radius <= 0.01f)
            return Vector3.zero;

        int hitCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            radius,
            separationHits,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);

        Vector3 separation = Vector3.zero;
        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = separationHits[i];
            separationHits[i] = null;

            if (hit == null || hit.transform.IsChildOf(transform))
                continue;

            if (hit.GetComponentInParent<MinionChase3D>() == null &&
                hit.GetComponentInParent<S01ChaseThreat>() == null)
            {
                continue;
            }

            Vector3 away = transform.position - hit.transform.position;
            away.y = 0f;
            float distance = away.magnitude;
            if (distance <= 0.001f)
                away = transform.right;
            else
                away /= distance;

            float weight = 1f - Mathf.Clamp01(distance / radius);
            separation += away * weight;
        }

        if (separation.sqrMagnitude <= 0.001f)
            return Vector3.zero;

        return separation.normalized * Mathf.Max(0f, separationStrength);
    }

    private void PlayThreatAnimation(string stateName, float moveSpeedParameter, bool forceRestart = false)
    {
        if (threatAnimators == null || threatAnimators.Length == 0)
            threatAnimators = GetComponentsInChildren<Animator>(true);

        int stateHash = Animator.StringToHash(stateName);
        foreach (Animator animator in threatAnimators)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
                continue;

            animator.enabled = true;
            animator.applyRootMotion = false;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            SetAnimatorMoveSpeed(animator, moveSpeedParameter);
            if (forceRestart && stateName == AttackState)
                SetAnimatorTrigger(animator, AttackParameter);

            if (forceRestart || !animator.GetCurrentAnimatorStateInfo(0).IsName(stateName))
                animator.CrossFade(stateHash, 0.08f, 0, forceRestart ? 0f : 0.1f);
        }
    }

    private void SetAnimatorMoveSpeed(Animator animator, float value)
    {
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name == MoveSpeedParameter && parameter.type == AnimatorControllerParameterType.Float)
            {
                animator.SetFloat(MoveSpeedParameter, value);
                return;
            }
        }
    }

    private void SetAnimatorTrigger(Animator animator, string triggerName)
    {
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name == triggerName && parameter.type == AnimatorControllerParameterType.Trigger)
            {
                animator.ResetTrigger(triggerName);
                animator.SetTrigger(triggerName);
                return;
            }
        }
    }

    private void RotateToward(Vector3 moveDirection)
    {
        moveDirection.y = 0f;

        if (moveDirection.sqrMagnitude <= 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(moveDirection.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    private bool CanDirectlyReachPlayer()
    {
        if (player == null)
            return false;

        CachePlayerColliders();

        Vector3 start = GetVisibilityPoint(transform);
        Vector3 end = GetVisibilityPoint(player);
        Vector3 direction = end - start;
        float distance = direction.magnitude;

        if (distance <= 0.01f)
            return true;

        int hitCount = Physics.RaycastNonAlloc(start, direction.normalized, visibilityHits, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        Array.Sort(visibilityHits, 0, hitCount, RaycastHitDistanceComparer.Instance);

        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider = visibilityHits[i].collider;

            if (hitCollider == null)
                continue;

            if (IsSelfCollider(hitCollider) || IsPlayerCollider(hitCollider))
                continue;

            return false;
        }

        return true;
    }

    private Vector3 GetVisibilityPoint(Transform target)
    {
        return target.position + Vector3.up * 0.9f;
    }

    private void TryCatchPlayer()
    {
        if (!chaseStarted || player == null)
            return;

        if (Time.time < suppressCatchUntil || S01ChaseIntroCutscene.IsAnyIntroRunning)
            return;

        if (HorizontalDistance(transform.position, player.position) > catchDistance)
            return;

        CatchPlayer();
    }

    private void CatchPlayer()
    {
        if (hasCaughtPlayer)
            return;

        hasCaughtPlayer = true;
        Debug.Log("Hắc Tinh caught the Player.");

        StartCoroutine(CatchPlayerSequence());
    }

    private IEnumerator CatchPlayerSequence()
    {
        Vector3 faceDirection = player != null ? player.position - transform.position : transform.forward;
        faceDirection.y = 0f;
        if (faceDirection.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(faceDirection.normalized);

        PlayThreatAnimation(AttackState, 0f, true);

        yield return new WaitForSeconds(Mathf.Max(0f, catchAttackImpactDelay));

        SpawnCatchAttackEffect();
        S01Soundscape.PlayImpactHit();

        PlayerHealth3D playerHealth = player != null ? player.GetComponent<PlayerHealth3D>() : null;
        if (playerHealth == null && player != null)
            playerHealth = player.GetComponentInParent<PlayerHealth3D>();

        if (playerHealth != null)
        {
            if (IsPlayerStillInCatchRange())
                playerHealth.TakeDamage(catchDamage);
            else
                Log("S01ChaseThreat: catch attack missed because player left range.");

            if (playerHealth.isDead)
                yield break;

            suppressCatchUntil = Time.time + Mathf.Max(0f, catchAttackCooldown);
            hasCaughtPlayer = false;
            PlayThreatAnimation(RunState, 1f);
            yield break;
        }

        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        S01ChaseIntroCutscene.ResetSharedState();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private bool IsPlayerStillInCatchRange()
    {
        if (player == null)
            return false;

        return HorizontalDistance(transform.position, player.position) <= catchDistance;
    }

    private void SpawnCatchAttackEffect()
    {
        Vector3 forward = player != null ? player.position - transform.position : transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude <= 0.001f)
            forward = transform.forward;
        forward.Normalize();

        GameObject impact = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        impact.name = "S01_MinionCatchImpact";
        impact.transform.position = transform.position + Vector3.up * 1.1f + forward * 0.9f;
        impact.transform.rotation = Quaternion.LookRotation(forward);
        impact.transform.localScale = new Vector3(1.55f, 0.2f, 0.7f);

        Collider collider = impact.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);

        Renderer renderer = impact.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material material = CreateCatchAttackEffectMaterial();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            Destroy(material, catchAttackEffectDuration + 0.05f);
        }

        Destroy(impact, catchAttackEffectDuration);
    }

    private Material CreateCatchAttackEffectMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = new Material(shader)
        {
            name = "Runtime_S01_MinionCatchImpact"
        };

        material.color = catchAttackEffectColor;
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", catchAttackEffectColor);

        material.EnableKeyword("_EMISSION");
        if (material.HasProperty("_EmissionColor"))
            material.SetColor("_EmissionColor", catchAttackEffectColor * 2.6f);

        return material;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryCatchFromCollider(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryCatchFromCollider(collision.collider);
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        TryCatchFromCollider(hit.collider);
    }

    private void TryCatchFromCollider(Collider other)
    {
        if (!chaseStarted || hasCaughtPlayer || other == null)
            return;

        if (Time.time < suppressCatchUntil || S01ChaseIntroCutscene.IsAnyIntroRunning)
            return;

        if (!other.CompareTag("Player") && !other.transform.root.CompareTag("Player"))
            return;

        if (player == null)
            player = other.transform.root;

        CatchPlayer();
    }

    private void FindPlayerIfNeeded()
    {
        if (player != null)
        {
            CachePlayerColliders();
            return;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;
            CachePlayerColliders();
            InitializePlayerStartPosition();
            Log("S01ChaseThreat: Player found: " + player.name);
        }
    }

    private void FindWaypointsIfNeeded()
    {
        GameObject waypointRoot = GameObject.Find("S01_ChaseWaypoints");

        if (waypoints != null && waypoints.Length > 0 && !ShouldReloadWaypointsFromScene(waypointRoot))
        {
            SortWaypointsByName();
            return;
        }

        if (waypointRoot == null)
        {
            if (waypoints == null)
                waypoints = new Transform[0];

            SortWaypointsByName();
            Log("S01ChaseThreat: Waypoint count = 0");
            return;
        }

        int childCount = waypointRoot.transform.childCount;
        waypoints = new Transform[childCount];

        for (int i = 0; i < childCount; i++)
            waypoints[i] = waypointRoot.transform.GetChild(i);

        SortWaypointsByName();
        Log("S01ChaseThreat: Waypoint count = " + waypoints.Length);
    }

    private bool ShouldReloadWaypointsFromScene(GameObject waypointRoot)
    {
        if (waypointRoot == null)
            return false;

        if (waypoints == null || waypoints.Length == 0)
            return true;

        if (waypointRoot.transform.childCount > waypoints.Length)
            return true;

        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null)
                return true;
        }

        return false;
    }

    private void SortWaypointsByName()
    {
        if (waypoints == null || waypoints.Length <= 1)
            return;

        Array.Sort(waypoints, CompareWaypointNames);
    }

    private int CompareWaypointNames(Transform left, Transform right)
    {
        string leftName = left != null ? left.name : string.Empty;
        string rightName = right != null ? right.name : string.Empty;
        return string.CompareOrdinal(leftName, rightName);
    }

    private void SkipFirstWaypointIfAlreadyThere()
    {
        waypointIndex = 0;
        highestReachedWaypointIndex = 0;
        playerRouteWaypointIndex = 0;

        if (waypoints == null || waypoints.Length <= 1 || waypoints[0] == null)
            return;

        if (HorizontalDistance(transform.position, waypoints[0].position) <= waypointReachDistance)
            AdvanceWaypointIndex(1, false);
    }

    private Transform GetCurrentValidWaypoint()
    {
        if (waypointIndex < highestReachedWaypointIndex)
        {
            Log("Ignoring backtrack waypoint.");
            waypointIndex = highestReachedWaypointIndex;
        }

        while (waypointIndex < waypoints.Length && waypoints[waypointIndex] == null)
            AdvanceWaypointIndex(waypointIndex + 1, false);

        if (waypointIndex >= waypoints.Length)
            return null;

        return waypoints[waypointIndex];
    }

    private void AdvanceWaypointProgressNearThreat()
    {
        if (waypoints == null || waypoints.Length == 0)
            return;

        while (waypointIndex < waypoints.Length && waypoints[waypointIndex] != null &&
               HorizontalDistance(transform.position, waypoints[waypointIndex].position) <= waypointReachDistance)
        {
            AdvanceWaypointIndex(waypointIndex + 1, false);
        }
    }

    private void UpdateForwardRouteProgress()
    {
        if (waypoints == null || waypoints.Length == 0 || player == null)
            return;

        UpdatePlayerRouteWaypointIndex();
        SkipWaypointsBehindThreat();

        int nearestThreatIndex = FindClosestWaypointIndex(
            transform.position,
            highestReachedWaypointIndex,
            Mathf.Max(highestReachedWaypointIndex, playerRouteWaypointIndex));

        if (nearestThreatIndex > highestReachedWaypointIndex &&
            waypoints[nearestThreatIndex] != null &&
            HorizontalDistance(transform.position, waypoints[nearestThreatIndex].position) <= 5f)
        {
            AdvanceWaypointIndex(nearestThreatIndex, nearestThreatIndex > waypointIndex + 1);
        }
    }

    private void UpdatePlayerRouteWaypointIndex()
    {
        int closestIndex = FindClosestWaypointIndex(
            player.position,
            Mathf.Max(playerRouteWaypointIndex, highestReachedWaypointIndex),
            waypoints.Length - 1);

        if (closestIndex > playerRouteWaypointIndex)
            playerRouteWaypointIndex = closestIndex;
    }

    private void SelectForwardFallbackWaypoint()
    {
        if (waypointIndex < highestReachedWaypointIndex)
        {
            Log("Ignoring backtrack waypoint.");
            waypointIndex = highestReachedWaypointIndex;
        }

        int maximumCandidate = Mathf.Clamp(playerRouteWaypointIndex, highestReachedWaypointIndex, waypoints.Length - 1);
        int safeCandidate = -1;

        for (int i = maximumCandidate; i >= highestReachedWaypointIndex; i--)
        {
            if (waypoints[i] != null && CanDirectlyReachWaypoint(waypoints[i]))
            {
                safeCandidate = i;
                break;
            }
        }

        if (safeCandidate > waypointIndex)
        {
            bool skippedOldWaypoints = safeCandidate > waypointIndex + 1;
            AdvanceWaypointIndex(safeCandidate, skippedOldWaypoints);
            Log("Fallback waypoint advanced to match player route position.");
        }
    }

    private void SkipWaypointsBehindThreat()
    {
        while (waypointIndex < waypoints.Length - 1)
        {
            Transform currentWaypoint = waypoints[waypointIndex];
            Transform nextWaypoint = waypoints[waypointIndex + 1];

            if (currentWaypoint == null)
            {
                AdvanceWaypointIndex(waypointIndex + 1, false);
                continue;
            }

            if (nextWaypoint == null)
                break;

            Vector3 routeDirection = nextWaypoint.position - currentWaypoint.position;
            Vector3 threatOffset = transform.position - currentWaypoint.position;
            routeDirection.y = 0f;
            threatOffset.y = 0f;

            if (routeDirection.sqrMagnitude <= 0.001f ||
                Vector3.Dot(threatOffset, routeDirection.normalized) <= waypointReachDistance)
            {
                break;
            }

            AdvanceWaypointIndex(waypointIndex + 1, false);
            Log("Skipping old waypoint; player is ahead.");
        }
    }

    private void AdvanceWaypointIndex(int requestedIndex, bool skippedMultiple)
    {
        if (waypoints == null)
            return;

        if (requestedIndex < highestReachedWaypointIndex)
        {
            Log("Ignoring backtrack waypoint.");
            return;
        }

        int clampedIndex = Mathf.Clamp(requestedIndex, 0, waypoints.Length);
        if (clampedIndex <= waypointIndex)
            return;

        waypointIndex = clampedIndex;
        highestReachedWaypointIndex = Mathf.Max(highestReachedWaypointIndex, waypointIndex);

        if (skippedMultiple)
            Log("Skipping old waypoint; player is ahead.");
    }

    private int FindClosestWaypointIndex(Vector3 position, int minimumIndex, int maximumIndex)
    {
        if (waypoints == null || waypoints.Length == 0)
            return 0;

        int min = Mathf.Clamp(minimumIndex, 0, waypoints.Length - 1);
        int max = Mathf.Clamp(maximumIndex, min, waypoints.Length - 1);
        int closestIndex = min;
        float closestDistance = float.MaxValue;

        for (int i = min; i <= max; i++)
        {
            if (waypoints[i] == null)
                continue;

            float distance = HorizontalDistance(position, waypoints[i].position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        return closestIndex;
    }

    private bool CanDirectlyReachWaypoint(Transform waypoint)
    {
        Vector3 start = GetVisibilityPoint(transform);
        Vector3 end = GetVisibilityPoint(waypoint);
        Vector3 direction = end - start;
        float distance = direction.magnitude;

        if (distance <= 0.01f)
            return true;

        int hitCount = Physics.RaycastNonAlloc(
            start,
            direction.normalized,
            visibilityHits,
            distance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider = visibilityHits[i].collider;
            if (hitCollider != null && !IsSelfCollider(hitCollider) && !IsPlayerCollider(hitCollider))
                return false;
        }

        return true;
    }

    private float GetPressureSpeed(float baseSpeed)
    {
        if (chaseTutorialActive && !shiftSpeedReleased)
            return Mathf.Min(baseSpeed, earlyThreatSpeed);

        return baseSpeed * currentSpeedMultiplier;
    }

    private void UpdateRubberBandSpeed()
    {
        if (player == null)
            return;

        float distance = HorizontalDistance(transform.position, player.position);
        PressureBand nextBand;
        float desiredMultiplier;

        if (Time.time < suppressCatchUpUntil)
        {
            nextBand = PressureBand.Normal;
            desiredMultiplier = 1f;
        }
        else if (distance > veryFarPlayerDistance)
        {
            nextBand = PressureBand.StrongCatchUp;
            desiredMultiplier = veryFarSpeedMultiplier;
        }
        else if (distance > farPlayerDistance)
        {
            nextBand = PressureBand.CatchUp;
            desiredMultiplier = farSpeedMultiplier;
        }
        else if (distance < nearPlayerDistance)
        {
            nextBand = PressureBand.Near;
            desiredMultiplier = 1f;
        }
        else
        {
            nextBand = PressureBand.Normal;
            desiredMultiplier = 1f;
        }

        currentSpeedMultiplier = Mathf.MoveTowards(
            currentSpeedMultiplier,
            desiredMultiplier,
            speedMultiplierChangeRate * Time.deltaTime);

        SetPressureBand(nextBand);
    }

    public void SuppressCatchUp(float duration)
    {
        suppressCatchUpUntil = Mathf.Max(suppressCatchUpUntil, Time.time + Mathf.Max(0f, duration));
        currentSpeedMultiplier = Mathf.Min(currentSpeedMultiplier, 1f);
        SetPressureBand(PressureBand.Normal);
    }

    public void SuppressCatch(float duration)
    {
        suppressCatchUntil = Mathf.Max(suppressCatchUntil, Time.time + Mathf.Max(0f, duration));
    }

    private void SetPressureBand(PressureBand nextBand)
    {
        if (currentPressureBand == nextBand)
            return;

        PressureBand previousBand = currentPressureBand;
        currentPressureBand = nextBand;

        if (nextBand == PressureBand.CatchUp || nextBand == PressureBand.StrongCatchUp)
        {
            Log("Hắc Tinh catch-up speed active.");
        }
        else if (nextBand == PressureBand.Near)
        {
            Log("Hắc Tinh near player; no extra speed.");
        }
        else if (nextBand == PressureBand.Normal &&
                 (previousBand == PressureBand.CatchUp || previousBand == PressureBand.StrongCatchUp))
        {
            Log("Hắc Tinh returned to normal chase speed.");
        }
    }

    private float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private void CachePlayerColliders()
    {
        if (player == null)
        {
            playerColliders = null;
            return;
        }

        playerColliders = player.GetComponentsInChildren<Collider>();
    }

    private bool IsSelfCollider(Collider candidate)
    {
        if (selfColliders == null)
            return false;

        for (int i = 0; i < selfColliders.Length; i++)
        {
            if (candidate == selfColliders[i])
                return true;
        }

        return false;
    }

    private bool IsPlayerCollider(Collider candidate)
    {
        if (playerColliders == null)
            return false;

        for (int i = 0; i < playerColliders.Length; i++)
        {
            if (candidate == playerColliders[i])
                return true;
        }

        return false;
    }

    private void SetMode(ChaseMode nextMode)
    {
        if (currentMode == nextMode)
            return;

        currentMode = nextMode;

        if (nextMode == ChaseMode.DirectChase)
            Log("S01 Hắc Tinh mode: DIRECT_CHASE");
        else if (nextMode == ChaseMode.WaypointFollow)
            Log("S01 Hắc Tinh mode: WAYPOINT_FALLBACK");
    }

    private void LogCurrentWaypoint()
    {
        if (!debugLogs)
            return;

        if (waypoints == null || waypoints.Length == 0)
        {
            Debug.Log("S01ChaseThreat: Current waypoint missing because waypoint list is empty.");
            return;
        }

        if (waypointIndex >= waypoints.Length)
        {
            Debug.Log("S01ChaseThreat: Reached final waypoint.");
            return;
        }

        Transform waypoint = waypoints[waypointIndex];
        Debug.Log("S01ChaseThreat: Current waypoint = " + (waypoint != null ? waypoint.name : "null"));
    }

    private void Log(string message)
    {
        if (debugLogs)
            Debug.Log(message);
    }

    private sealed class RaycastHitDistanceComparer : IComparer
    {
        public static readonly RaycastHitDistanceComparer Instance = new RaycastHitDistanceComparer();

        public int Compare(object x, object y)
        {
            RaycastHit left = (RaycastHit)x;
            RaycastHit right = (RaycastHit)y;
            return left.distance.CompareTo(right.distance);
        }
    }
}
