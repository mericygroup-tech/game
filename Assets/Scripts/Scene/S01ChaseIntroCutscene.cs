using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class S01ChaseIntroCutscene : MonoBehaviour
{
    public Transform player;
    public Camera mainCamera;
    public ThirdPersonCamera thirdPersonCamera;
    public S01WarningTextUI warningUI;

    public string subtitle = "Khe nứt trong bảo tàng kéo Hắc Tinh ra đời thực. Chạy khỏi đây!";
    public float cutsceneDuration = 3.1f;
    public float cameraMoveDuration = 1.2f;
    public float cameraDistanceFromPlayer = 4f;
    public float cameraHeight = 2.4f;
    public float lookAtHeight = 1.1f;
    public float spawnBehindDistance = 14f;
    public float spawnSideSpacing = 2f;
    public float introStopBehindDistance = 6f;
    public float introApproachSpeed = 4.5f;
    public float postCutsceneAttackGrace = 1.4f;

    private bool played;
    private bool running;

    public static bool IsAnyIntroRunning { get; private set; }
    public bool HasPlayed => played;
    public bool IsRunning => running;

    public static void ResetSharedState()
    {
        IsAnyIntroRunning = false;
    }

    public bool TryPlay(Transform playerTransform)
    {
        if (played || running)
            return false;

        if (player == null && playerTransform != null)
            player = playerTransform;

        played = true;
        StartCoroutine(PlaySequence());
        return true;
    }

    private void OnDisable()
    {
        if (running)
            IsAnyIntroRunning = false;
    }

    private IEnumerator PlaySequence()
    {
        running = true;
        IsAnyIntroRunning = true;
        ResolveReferences();

        Vector3 savedCameraPosition = mainCamera != null ? mainCamera.transform.position : Vector3.zero;
        Quaternion savedCameraRotation = mainCamera != null ? mainCamera.transform.rotation : Quaternion.identity;
        bool savedThirdPersonCameraEnabled = thirdPersonCamera != null && thirdPersonCamera.enabled;

        PlayerController3D playerController = player != null ? player.GetComponent<PlayerController3D>() : null;
        PlayerCombat3D playerCombat = player != null ? player.GetComponent<PlayerCombat3D>() : null;
        bool savedPlayerControllerEnabled = playerController != null && playerController.enabled;
        bool savedPlayerCombatEnabled = playerCombat != null && playerCombat.enabled;

        MinionSpawner3D[] spawners = FindObjectsByType<MinionSpawner3D>(FindObjectsInactive.Exclude);
        MoveSpawnPointsBehindPlayer(spawners);
        foreach (MinionSpawner3D spawner in spawners)
        {
            if (spawner != null)
                spawner.BeginSpawning();
        }

        MinionChase3D[] minions = FindObjectsByType<MinionChase3D>(FindObjectsInactive.Exclude);
        bool[] minionEnabled = new bool[minions.Length];
        for (int i = 0; i < minions.Length; i++)
        {
            if (minions[i] == null)
                continue;

            minionEnabled[i] = minions[i].enabled;
            minions[i].SuppressAttacks(cutsceneDuration + postCutsceneAttackGrace);
            minions[i].enabled = false;
            minions[i].ForceVisualAnimation(minions[i].moveState);
        }

        S01ChaseThreat[] chaseThreats = FindObjectsByType<S01ChaseThreat>(FindObjectsInactive.Exclude);
        foreach (S01ChaseThreat chaseThreat in chaseThreats)
        {
            if (chaseThreat != null)
                chaseThreat.SuppressCatch(cutsceneDuration + postCutsceneAttackGrace);
        }

        if (playerController != null)
            playerController.enabled = false;
        if (playerCombat != null)
            playerCombat.enabled = false;
        if (thirdPersonCamera != null)
            thirdPersonCamera.enabled = false;

        if (warningUI != null)
            warningUI.ShowWarning(subtitle, cutsceneDuration + 0.4f);

        S01Soundscape.PlayDarkStarRoar();
        S01Soundscape.StartHeartbeat();

        Vector3 threatFocus = FindThreatFocus();
        Vector3 playerPosition = player != null ? player.position : transform.position;
        Vector3 awayFromThreat = playerPosition - threatFocus;
        awayFromThreat.y = 0f;
        if (awayFromThreat.sqrMagnitude < 0.001f)
            awayFromThreat = Vector3.forward;
        awayFromThreat.Normalize();

        Vector3 startPosition = savedCameraPosition;
        Quaternion startRotation = savedCameraRotation;
        Vector3 cutscenePosition = playerPosition + awayFromThreat * cameraDistanceFromPlayer + Vector3.up * cameraHeight;
        Vector3 lookAt = Vector3.Lerp(threatFocus, playerPosition, 0.28f) + Vector3.up * lookAtHeight;

        float elapsed = 0f;
        float moveDuration = Mathf.Max(0.01f, cameraMoveDuration);
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / moveDuration));
            SetCamera(Vector3.Lerp(startPosition, cutscenePosition, t), lookAt, startRotation, t);
            MoveMinionsForIntro(minions);
            yield return null;
        }

        float holdTime = Mathf.Max(0f, cutsceneDuration - moveDuration);
        elapsed = 0f;
        while (elapsed < holdTime)
        {
            elapsed += Time.deltaTime;
            SetCamera(cutscenePosition, lookAt, Quaternion.identity, 1f);
            MoveMinionsForIntro(minions);
            yield return null;
        }

        for (int i = 0; i < minions.Length; i++)
        {
            if (minions[i] != null)
            {
                minions[i].SuppressAttacks(postCutsceneAttackGrace);
                minions[i].enabled = minionEnabled[i];
            }
        }

        foreach (S01ChaseThreat chaseThreat in chaseThreats)
        {
            if (chaseThreat != null)
                chaseThreat.SuppressCatch(postCutsceneAttackGrace);
        }

        if (playerController != null)
            playerController.enabled = savedPlayerControllerEnabled;
        if (playerCombat != null)
            playerCombat.enabled = savedPlayerCombatEnabled;

        if (thirdPersonCamera != null)
            thirdPersonCamera.enabled = savedThirdPersonCameraEnabled;
        else if (mainCamera != null)
        {
            mainCamera.transform.position = savedCameraPosition;
            mainCamera.transform.rotation = savedCameraRotation;
        }

        running = false;
        IsAnyIntroRunning = false;
    }

    private void MoveMinionsForIntro(MinionChase3D[] minions)
    {
        if (player == null || minions == null)
            return;

        Vector3 routeForward = GetRouteForward();
        Vector3 targetCenter = player.position - routeForward * introStopBehindDistance;

        for (int i = 0; i < minions.Length; i++)
        {
            MinionChase3D minion = minions[i];
            if (minion == null)
                continue;

            Vector3 targetPosition = targetCenter;
            targetPosition.x += (i - (minions.Length - 1) * 0.5f) * 1.05f;
            targetPosition.y = minion.transform.position.y;

            minion.transform.position = Vector3.MoveTowards(
                minion.transform.position,
                targetPosition,
                introApproachSpeed * Time.deltaTime);
            minion.ForceSnapToGround();

            Vector3 faceDirection = player.position - minion.transform.position;
            faceDirection.y = 0f;
            if (faceDirection.sqrMagnitude > 0.001f)
                minion.transform.rotation = Quaternion.LookRotation(faceDirection.normalized);
        }
    }

    private void MoveSpawnPointsBehindPlayer(MinionSpawner3D[] spawners)
    {
        if (player == null || spawners == null)
            return;

        Vector3 routeForward = GetRouteForward();
        Vector3 behindDirection = -routeForward;
        Vector3 right = Vector3.Cross(Vector3.up, behindDirection).normalized;
        if (right.sqrMagnitude <= 0.001f)
            right = Vector3.right;

        for (int s = 0; s < spawners.Length; s++)
        {
            MinionSpawner3D spawner = spawners[s];
            if (spawner == null || spawner.spawnPoints == null)
                continue;

            for (int i = 0; i < spawner.spawnPoints.Length; i++)
            {
                Transform spawnPoint = spawner.spawnPoints[i];
                if (spawnPoint == null)
                    continue;

                float side = i - (spawner.spawnPoints.Length - 1) * 0.5f;
                spawnPoint.position = player.position +
                                      behindDirection * spawnBehindDistance +
                                      right * side * spawnSideSpacing +
                                      Vector3.up * 0.05f;
                spawnPoint.rotation = Quaternion.LookRotation(routeForward, Vector3.up);
            }
        }
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

        return Vector3.forward;
    }

    private void SetCamera(Vector3 position, Vector3 lookAt, Quaternion fallbackStartRotation, float blend)
    {
        if (mainCamera == null)
            return;

        mainCamera.transform.position = position;
        Quaternion lookRotation = Quaternion.LookRotation((lookAt - position).normalized, Vector3.up);
        mainCamera.transform.rotation = blend < 1f
            ? Quaternion.Slerp(fallbackStartRotation, lookRotation, blend)
            : lookRotation;
    }

    private Vector3 FindThreatFocus()
    {
        MinionChase3D nearestMinion = null;
        float nearestDistance = float.MaxValue;
        Vector3 origin = player != null ? player.position : transform.position;

        MinionChase3D[] minions = FindObjectsByType<MinionChase3D>(FindObjectsInactive.Exclude);
        foreach (MinionChase3D minion in minions)
        {
            if (minion == null)
                continue;

            float distance = Vector3.SqrMagnitude(minion.transform.position - origin);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestMinion = minion;
            }
        }

        if (nearestMinion != null)
            return nearestMinion.transform.position;

        S01ChaseThreat chaseThreat = FindAnyObjectByType<S01ChaseThreat>(FindObjectsInactive.Include);
        if (chaseThreat != null)
            return chaseThreat.transform.position;

        return origin + Vector3.back * 8f;
    }

    private void ResolveReferences()
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

        if (warningUI == null)
            warningUI = FindAnyObjectByType<S01WarningTextUI>();
    }
}
