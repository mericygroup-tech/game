using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class S01AmbushDodgeQTE : MonoBehaviour
{
    public Transform player;
    public S01WarningTextUI warningUI;
    public GameObject minionPrefab;

    public Vector3 leftStartOffset = new Vector3(-7f, 1f, 0f);
    public Vector3 rightStartOffset = new Vector3(7f, 1f, 0f);
    public Vector3 attackTargetOffset = new Vector3(0f, 1f, 0f);
    public float qteDuration = 1f;
    public float slowMotionTimeScale = 0.28f;
    public float lungeDistancePastPlayer = 3f;
    public bool ambushMinionsJoinChaseOnDodge = true;
    public float joinedMinionMoveSpeed = 5.5f;
    public float joinedMinionChaseRange = 120f;
    public float joinedMinionAttackGrace = 1.2f;
    public float joinBehindPlayerDistance = 7f;
    public float joinSideSpacing = 1.2f;
    public float joinColliderDelay = 0.35f;
    public float attackFeedbackPause = 0.85f;
    public int failedDodgeDamage = 20;
    public string warningMessage = "Hắc Tinh lao ra từ hai bên! Nhấn E để né!";
    public string successMessage = "Né được rồi! Chạy tiếp!";
    public string failMessage = "Bạn bị Hắc Tinh bắt!";
    public bool triggerOnce = true;

    private bool triggered;
    private bool running;
    private Canvas qteCanvas;
    private GameObject qteRoot;
    private Image radialFill;
    private TMP_Text keyText;
    private TMP_Text promptText;
    private Material threatMaterial;
    private bool slowMotionActive;
    private float previousTimeScale = 1f;
    private float previousFixedDeltaTime = 0.02f;
    private int joinedThreatCount;

    private void Start()
    {
        FindReferencesIfNeeded();
        HideQteUI();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (running || (triggerOnce && triggered) || !IsPlayer(other))
            return;

        triggered = true;
        StartCoroutine(RunAmbush());
    }

    private IEnumerator RunAmbush()
    {
        running = true;
        joinedThreatCount = 0;
        FindReferencesIfNeeded();
        MinionChase3D[] existingChasers = FindObjectsByType<MinionChase3D>(FindObjectsInactive.Exclude);
        S01ChaseThreat[] existingRouteThreats = FindObjectsByType<S01ChaseThreat>(FindObjectsInactive.Exclude);

        if (warningUI != null)
            warningUI.ShowWarning(warningMessage, Mathf.Max(2f, qteDuration + 0.8f));

        S01Soundscape.PlayDarkStarRoar();

        Vector3 center = transform.position + attackTargetOffset;
        GameObject leftThreat = CreateThreatVisual("S01_AmbushThreat_Left", transform.position + leftStartOffset, center);
        GameObject rightThreat = CreateThreatVisual("S01_AmbushThreat_Right", transform.position + rightStartOffset, center);

        ShowQteUI();
        BeginSlowMotion();

        float elapsed = 0f;
        bool dodged = false;

        while (elapsed < qteDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, qteDuration));
            float eased = 1f - Mathf.Pow(1f - t, 2f);

            MoveThreat(leftThreat, transform.position + leftStartOffset, GetThreatEndPosition(transform.position + leftStartOffset, center), eased);
            MoveThreat(rightThreat, transform.position + rightStartOffset, GetThreatEndPosition(transform.position + rightStartOffset, center), eased);
            UpdateQteUI(1f - t);

            InputSettingsManager inputSettings = FindAnyObjectByType<InputSettingsManager>();
            KeyCode interactKey = (inputSettings != null && inputSettings.Keyboard != null) ? inputSettings.Keyboard.interact : KeyCode.E;

            if (Input.GetKeyDown(interactKey))
            {
                dodged = true;
                break;
            }

            yield return null;
        }

        HideQteUI();
        EndSlowMotion();
        PlayAmbushAttackFeedback(leftThreat);
        PlayAmbushAttackFeedback(rightThreat);

        if (attackFeedbackPause > 0f)
            yield return new WaitForSeconds(attackFeedbackPause);

        if (dodged)
        {
            if (warningUI != null)
                warningUI.ShowWarning(successMessage, 2.4f);

            if (ambushMinionsJoinChaseOnDodge)
            {
                if (JoinAmbushThreatToChase(leftThreat))
                    leftThreat = null;
                if (JoinAmbushThreatToChase(rightThreat))
                    rightThreat = null;
            }
            else
            {
                yield return RetreatThreat(leftThreat, -transform.right * 4f + Vector3.back * 2f);
                yield return RetreatThreat(rightThreat, transform.right * 4f + Vector3.back * 2f);
            }
        }
        else
        {
            if (warningUI != null)
                warningUI.ShowWarning(failMessage, 1.5f);

            CatchPlayer();
        }

        RestoreExistingChasers(existingChasers, existingRouteThreats, leftThreat, rightThreat);
        DestroyFallbackThreat(leftThreat);
        DestroyFallbackThreat(rightThreat);
        running = false;

        if (triggerOnce)
            gameObject.SetActive(false);
    }

    private GameObject CreateThreatVisual(string objectName, Vector3 position, Vector3 target)
    {
        GameObject minionThreat = CreateMinionThreat(objectName, position, target);
        if (minionThreat != null)
            return minionThreat;

        GameObject threat = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        threat.name = objectName;
        threat.transform.position = position;
        threat.transform.localScale = new Vector3(0.9f, 1.25f, 0.9f);

        Vector3 direction = target - position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.01f)
            threat.transform.rotation = Quaternion.LookRotation(direction.normalized);

        Collider collider = threat.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);

        Renderer renderer = threat.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = GetThreatMaterial();

        GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        eye.name = "Ambush_EyeGlow";
        eye.transform.SetParent(threat.transform, false);
        eye.transform.localPosition = new Vector3(0f, 0.55f, 0.42f);
        eye.transform.localScale = new Vector3(0.24f, 0.12f, 0.06f);
        Collider eyeCollider = eye.GetComponent<Collider>();
        if (eyeCollider != null)
            Destroy(eyeCollider);

        Renderer eyeRenderer = eye.GetComponent<Renderer>();
        if (eyeRenderer != null)
            eyeRenderer.sharedMaterial = GetEyeMaterial();

        SnapThreatToGround(threat);
        return threat;
    }

    private GameObject CreateMinionThreat(string objectName, Vector3 position, Vector3 target)
    {
        GameObject prefab = ResolveMinionPrefab();
        if (prefab == null)
            return null;

        GameObject threat = Instantiate(prefab, position, Quaternion.identity);
        threat.name = objectName;

        Vector3 direction = target - position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.01f)
            threat.transform.rotation = Quaternion.LookRotation(direction.normalized);

        MinionChase3D chase = threat.GetComponent<MinionChase3D>();
        if (chase != null)
        {
            chase.target = player;
            chase.enabled = false;
            chase.ForceVisualAnimation(chase.moveState);
        }

        SetThreatColliders(threat, false);
        SnapThreatToGround(threat);
        return threat;
    }

    private GameObject ResolveMinionPrefab()
    {
        if (minionPrefab != null)
            return minionPrefab;

        MinionSpawner3D spawner = FindAnyObjectByType<MinionSpawner3D>(FindObjectsInactive.Include);
        if (spawner != null && spawner.minionPrefab != null)
        {
            minionPrefab = spawner.minionPrefab;
            return minionPrefab;
        }

        return null;
    }

    private Vector3 GetThreatEndPosition(Vector3 start, Vector3 center)
    {
        Vector3 direction = center - start;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f)
            direction = transform.forward;

        return center + direction.normalized * lungeDistancePastPlayer;
    }

    private void MoveThreat(GameObject threat, Vector3 start, Vector3 end, float t)
    {
        if (threat == null)
            return;

        threat.transform.position = Vector3.Lerp(start, end, t);
        SnapThreatToGround(threat);
        Vector3 direction = end - start;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.01f)
            threat.transform.rotation = Quaternion.LookRotation(direction.normalized);
    }

    private IEnumerator RetreatThreat(GameObject threat, Vector3 offset)
    {
        if (threat == null)
            yield break;

        Vector3 start = threat.transform.position;
        Vector3 end = start + offset;
        float elapsed = 0f;

        while (elapsed < 0.25f)
        {
            elapsed += Time.unscaledDeltaTime;
            threat.transform.position = Vector3.Lerp(start, end, elapsed / 0.25f);
            yield return null;
        }
    }

    private void PlayAmbushAttackFeedback(GameObject threat)
    {
        if (threat == null)
            return;

        MinionChase3D chase = threat.GetComponent<MinionChase3D>();
        if (chase != null)
        {
            chase.PlayAmbushAttackFeedback(player);
            return;
        }

        SpawnFallbackAttackEffect(threat);
    }

    private bool JoinAmbushThreatToChase(GameObject threat)
    {
        if (threat == null)
            return false;

        MinionChase3D chase = threat.GetComponent<MinionChase3D>();
        if (chase == null)
            return false;

        SetThreatColliders(threat, true);
        PrepareThreatForImmediateChase(threat, joinedThreatCount++);
        threat.name = "S01_AmbushMinion_Chaser";
        chase.SuppressAttacks(joinedMinionAttackGrace);
        chase.JoinChase(player, joinedMinionMoveSpeed, joinedMinionChaseRange);
        if (joinColliderDelay > 0f)
        {
            SetThreatColliders(threat, false);
            StartCoroutine(EnableThreatCollidersAfterDelay(threat, joinColliderDelay));
        }
        return true;
    }

    private void RestoreExistingChasers(
        MinionChase3D[] existingChasers,
        S01ChaseThreat[] existingRouteThreats,
        GameObject leftAmbushThreat,
        GameObject rightAmbushThreat)
    {
        if (existingChasers != null)
        {
            foreach (MinionChase3D chaser in existingChasers)
            {
                if (chaser == null || IsAmbushThreat(chaser.gameObject, leftAmbushThreat, rightAmbushThreat))
                    continue;

                if (!chaser.gameObject.activeSelf)
                    chaser.gameObject.SetActive(true);

                chaser.target = player;
                chaser.chaseRange = Mathf.Max(chaser.chaseRange, joinedMinionChaseRange);
                chaser.SuppressAttacks(0.6f);
                chaser.enabled = true;
            }
        }

        if (existingRouteThreats == null)
            return;

        foreach (S01ChaseThreat routeThreat in existingRouteThreats)
        {
            if (routeThreat == null || IsAmbushThreat(routeThreat.gameObject, leftAmbushThreat, rightAmbushThreat))
                continue;

            if (!routeThreat.gameObject.activeSelf)
                routeThreat.gameObject.SetActive(true);

            routeThreat.SuppressCatch(0.6f);
            routeThreat.enabled = true;
        }
    }

    private bool IsAmbushThreat(GameObject candidate, GameObject leftAmbushThreat, GameObject rightAmbushThreat)
    {
        if (candidate == null)
            return false;

        return IsSameObjectOrChild(candidate, leftAmbushThreat) ||
               IsSameObjectOrChild(candidate, rightAmbushThreat);
    }

    private bool IsSameObjectOrChild(GameObject candidate, GameObject possibleRoot)
    {
        if (candidate == null || possibleRoot == null)
            return false;

        return candidate == possibleRoot || candidate.transform.IsChildOf(possibleRoot.transform);
    }

    private void PrepareThreatForImmediateChase(GameObject threat, int joinIndex)
    {
        if (threat == null)
            return;

        Vector3 position = threat.transform.position;
        if (player != null)
            position.y = player.position.y + 0.05f;
        threat.transform.position = position;
        SnapThreatToGround(threat);

        float side = joinIndex - 0.5f;
        Vector3 sideOffset = transform.right * side * Mathf.Min(joinSideSpacing, 0.75f);
        threat.transform.position += sideOffset;
        SnapThreatToGround(threat);

        if (player == null)
            return;

        Vector3 faceDirection = player.position - threat.transform.position;
        faceDirection.y = 0f;
        if (faceDirection.sqrMagnitude > 0.001f)
            threat.transform.rotation = Quaternion.LookRotation(faceDirection.normalized, Vector3.up);
    }

    private void PlaceThreatBehindPlayerForChase(GameObject threat, int joinIndex)
    {
        if (threat == null || player == null)
            return;

        Vector3 routeForward = GetRouteForward();
        Vector3 right = Vector3.Cross(Vector3.up, -routeForward).normalized;
        if (right.sqrMagnitude <= 0.001f)
            right = Vector3.right;

        float side = joinIndex - 0.5f;
        Vector3 position = player.position -
                           routeForward * joinBehindPlayerDistance +
                           right * side * joinSideSpacing;
        position.y = player.position.y + 0.05f;
        threat.transform.position = position;
        SnapThreatToGround(threat);

        Vector3 faceDirection = player.position - threat.transform.position;
        faceDirection.y = 0f;
        if (faceDirection.sqrMagnitude > 0.001f)
            threat.transform.rotation = Quaternion.LookRotation(faceDirection.normalized, Vector3.up);
    }

    private void SnapThreatToGround(GameObject threat)
    {
        if (threat == null)
            return;

        MinionChase3D chase = threat.GetComponent<MinionChase3D>();
        if (chase != null)
        {
            chase.ForceSnapToGround();
            return;
        }

        Vector3 origin = threat.transform.position + Vector3.up * 8f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 18f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) &&
            IsValidGroundCollider(hit.collider))
        {
            threat.transform.position = new Vector3(threat.transform.position.x, hit.point.y + 0.02f, threat.transform.position.z);
            return;
        }

        float fallbackY = player != null ? player.position.y + 0.02f : 0.02f;
        threat.transform.position = new Vector3(threat.transform.position.x, fallbackY, threat.transform.position.z);
    }

    private static bool IsValidGroundCollider(Collider hitCollider)
    {
        if (hitCollider == null)
            return false;

        if (hitCollider.GetComponentInParent<MinionChase3D>() != null ||
            hitCollider.GetComponentInParent<S01ChaseThreat>() != null ||
            hitCollider.GetComponentInParent<PlayerHealth3D>() != null)
        {
            return false;
        }

        Transform current = hitCollider.transform;
        while (current != null)
        {
            string objectName = current.name;
            if (!string.IsNullOrEmpty(objectName) &&
                (objectName.Contains("Barrier") ||
                 objectName.Contains("Fence") ||
                 objectName.Contains("Wall") ||
                 objectName.Contains("Gate") ||
                 objectName.Contains("Cone") ||
                 objectName.Contains("Truck") ||
                 objectName.Contains("Obstacle") ||
                 objectName.Contains("Blocking")))
            {
                return false;
            }

            current = current.parent;
        }

        return true;
    }

    private Vector3 GetRouteForward()
    {
        GameObject waypointRoot = GameObject.Find("S01_ChaseWaypoints");
        if (waypointRoot != null && waypointRoot.transform.childCount > 0 && player != null)
        {
            Transform farthestAhead = null;
            float farthestZ = float.MinValue;
            for (int i = 0; i < waypointRoot.transform.childCount; i++)
            {
                Transform waypoint = waypointRoot.transform.GetChild(i);
                if (waypoint != null && waypoint.position.z > farthestZ)
                {
                    farthestZ = waypoint.position.z;
                    farthestAhead = waypoint;
                }
            }

            if (farthestAhead != null)
            {
                Vector3 direction = farthestAhead.position - player.position;
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.001f)
                    return direction.normalized;
            }
        }

        Vector3 fallback = transform.forward;
        fallback.y = 0f;
        return fallback.sqrMagnitude > 0.001f ? fallback.normalized : Vector3.forward;
    }

    private IEnumerator EnableThreatCollidersAfterDelay(GameObject threat, float delay)
    {
        yield return new WaitForSeconds(Mathf.Max(0f, delay));

        if (threat != null)
            SetThreatColliders(threat, true);
    }

    private void DestroyFallbackThreat(GameObject threat)
    {
        if (threat == null)
            return;

        if (threat.GetComponent<MinionChase3D>() != null)
            return;

        Destroy(threat);
    }

    private void SetThreatColliders(GameObject threat, bool enabled)
    {
        if (threat == null)
            return;

        Collider[] colliders = threat.GetComponentsInChildren<Collider>(true);
        foreach (Collider threatCollider in colliders)
        {
            if (threatCollider != null)
                threatCollider.enabled = enabled;
        }
    }

    private void SpawnFallbackAttackEffect(GameObject threat)
    {
        Vector3 forward = player != null ? player.position - threat.transform.position : threat.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude <= 0.001f)
            forward = threat.transform.forward;
        forward.Normalize();

        GameObject slash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        slash.name = "Ambush_AttackImpact";
        slash.transform.position = threat.transform.position + Vector3.up * 1.05f + forward * 0.85f;
        slash.transform.rotation = Quaternion.LookRotation(forward);
        slash.transform.localScale = new Vector3(1.25f, 0.18f, 0.55f);

        Collider slashCollider = slash.GetComponent<Collider>();
        if (slashCollider != null)
            Destroy(slashCollider);

        Renderer renderer = slash.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = GetEyeMaterial();

        Destroy(slash, 0.28f);
        S01Soundscape.PlayImpactHit();
    }

    private void CatchPlayer()
    {
        EndSlowMotion();
        Debug.Log("S01 ambush QTE failed. Hắc Tinh caught the Player.");

        if (player == null)
            FindReferencesIfNeeded();

        PlayerHealth3D health = player != null ? player.GetComponent<PlayerHealth3D>() : null;
        if (health != null)
        {
            health.TakeDamage(failedDodgeDamage);
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.name);
    }

    private void BeginSlowMotion()
    {
        if (slowMotionActive)
            return;

        previousTimeScale = Time.timeScale;
        previousFixedDeltaTime = Time.fixedDeltaTime;
        Time.timeScale = Mathf.Clamp(slowMotionTimeScale, 0.05f, 1f);
        Time.fixedDeltaTime = previousFixedDeltaTime * Time.timeScale;
        slowMotionActive = true;
        S01Soundscape.PlaySlowMotionWhoosh();
    }

    private void EndSlowMotion()
    {
        if (!slowMotionActive)
            return;

        Time.timeScale = previousTimeScale;
        Time.fixedDeltaTime = previousFixedDeltaTime;
        slowMotionActive = false;
    }

    private void ShowQteUI()
    {
        EnsureQteUI();
        if (qteRoot != null)
            qteRoot.SetActive(true);

        UpdateQteUI(1f);
    }

    private void HideQteUI()
    {
        if (qteRoot != null)
            qteRoot.SetActive(false);
    }

    private void UpdateQteUI(float normalizedTimeLeft)
    {
        if (radialFill != null)
            radialFill.fillAmount = Mathf.Clamp01(normalizedTimeLeft);

        if (keyText != null)
            keyText.text = "E";

        if (promptText != null)
            promptText.text = "NHẤN E ĐỂ NÉ";
    }

    private void EnsureQteUI()
    {
        if (qteRoot != null)
            return;

        qteCanvas = FindAnyObjectByType<Canvas>();
        if (qteCanvas == null)
            return;

        qteRoot = new GameObject("S01_AmbushDodgeQTE_UI");
        qteRoot.transform.SetParent(qteCanvas.transform, false);

        RectTransform rootRect = qteRoot.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.anchoredPosition = new Vector2(0f, -70f);
        rootRect.sizeDelta = new Vector2(220f, 220f);

        Image background = CreateImage(qteRoot.transform, "Key_Background", new Vector2(0f, 0f), new Vector2(116f, 116f), new Color(0.05f, 0.04f, 0.08f, 0.82f));
        background.sprite = CreateCircleSprite();

        radialFill = CreateImage(qteRoot.transform, "Key_Timer_Ring", new Vector2(0f, 0f), new Vector2(142f, 142f), new Color(1f, 0.82f, 0.18f, 0.95f));
        radialFill.sprite = CreateCircleSprite();
        radialFill.type = Image.Type.Filled;
        radialFill.fillMethod = Image.FillMethod.Radial360;
        radialFill.fillOrigin = 2;
        radialFill.fillClockwise = false;

        keyText = CreateText(qteRoot.transform, "Key_Label", new Vector2(0f, 2f), new Vector2(100f, 80f), 56, TextAlignmentOptions.Center);
        keyText.text = "E";

        promptText = CreateText(qteRoot.transform, "Prompt_Label", new Vector2(0f, -88f), new Vector2(260f, 42f), 24, TextAlignmentOptions.Center);
        promptText.text = "NHẤN E ĐỂ NÉ";
    }

    private Image CreateImage(Transform parent, string objectName, Vector2 position, Vector2 size, Color color)
    {
        GameObject obj = new GameObject(objectName);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Image image = obj.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private TMP_Text CreateText(Transform parent, string objectName, Vector2 position, Vector2 size, int fontSize, TextAlignmentOptions alignment)
    {
        GameObject obj = new GameObject(objectName);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        TMP_Text text = obj.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private Sprite CreateCircleSprite()
    {
        Texture2D texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(31.5f, 31.5f);
        float radius = 30f;

        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                texture.SetPixel(x, y, distance <= radius ? Color.white : Color.clear);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }

    private Material GetThreatMaterial()
    {
        if (threatMaterial != null)
            return threatMaterial;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        threatMaterial = new Material(shader);
        threatMaterial.name = "S01_AmbushThreat_RuntimeDarkPurple";
        threatMaterial.color = new Color(0.11f, 0.02f, 0.2f, 1f);
        return threatMaterial;
    }

    private Material GetEyeMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = new Material(shader);
        material.name = "S01_AmbushThreat_RuntimeEyeGlow";
        material.color = new Color(1f, 0.16f, 0.25f, 1f);
        return material;
    }

    private void FindReferencesIfNeeded()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                player = playerObject.transform;
        }

        if (warningUI == null)
            warningUI = FindAnyObjectByType<S01WarningTextUI>();
    }

    private bool IsPlayer(Collider other)
    {
        return other.CompareTag("Player") || other.transform.root.CompareTag("Player");
    }

    private void OnDisable()
    {
        HideQteUI();
        EndSlowMotion();
    }
}
