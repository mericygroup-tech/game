using System.Collections;
using UnityEngine;

public class BreakableChaseObstacle : MonoBehaviour
{
    public float slowMultiplier = 0.05f;
    public float slowDuration = 2.5f;
    public float postBreakCatchUpGrace = 1.5f;
    public float scatterDuration = 0.55f;
    public Transform scatterRoot;
    public Collider delayCollider;

    private bool triggered;

    private void Awake()
    {
        if (delayCollider == null)
            delayCollider = GetComponent<Collider>();

        if (scatterRoot == null)
            scatterRoot = transform;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggered)
            return;

        S01ChaseThreat threat = other.GetComponentInParent<S01ChaseThreat>();
        if (threat == null && !LooksLikeChaseThreat(other.gameObject))
            return;

        triggered = true;
        Debug.Log("Hắc Tinh hit delay obstacle.");
        S01Soundscape.PlayDebrisPush();
        StartCoroutine(BreakAndRelease(threat));
    }

    private IEnumerator BreakAndRelease(S01ChaseThreat threat)
    {
        float originalDirectSpeed = 0f;
        float originalWaypointSpeed = 0f;
        float originalCatchUpSpeed = 0f;
        float originalMoveSpeed = 0f;

        if (threat != null)
        {
            originalDirectSpeed = threat.directChaseSpeed;
            originalWaypointSpeed = threat.waypointSpeed;
            originalCatchUpSpeed = threat.catchUpSpeed;
            originalMoveSpeed = threat.moveSpeed;

            threat.directChaseSpeed *= slowMultiplier;
            threat.waypointSpeed *= slowMultiplier;
            threat.catchUpSpeed *= slowMultiplier;
            threat.moveSpeed *= slowMultiplier;
        }

        yield return StartCoroutine(ScatterVisuals());
        yield return new WaitForSeconds(Mathf.Max(0f, slowDuration - scatterDuration));

        if (threat != null)
        {
            threat.directChaseSpeed = originalDirectSpeed;
            threat.waypointSpeed = originalWaypointSpeed;
            threat.catchUpSpeed = originalCatchUpSpeed;
            threat.moveSpeed = originalMoveSpeed;
            threat.SuppressCatchUp(postBreakCatchUpGrace);
        }

        if (delayCollider != null)
            delayCollider.enabled = false;

        Debug.Log("Delay obstacle broken; Hắc Tinh continues chase.");
    }

    private IEnumerator ScatterVisuals()
    {
        if (scatterRoot == null)
            yield break;

        int childCount = scatterRoot.childCount;
        Vector3[] startPositions = new Vector3[childCount];
        Quaternion[] startRotations = new Quaternion[childCount];
        Vector3[] targetPositions = new Vector3[childCount];
        Quaternion[] targetRotations = new Quaternion[childCount];

        for (int i = 0; i < childCount; i++)
        {
            Transform child = scatterRoot.GetChild(i);
            startPositions[i] = child.localPosition;
            startRotations[i] = child.localRotation;

            float direction = i % 2 == 0 ? -1f : 1f;
            targetPositions[i] = startPositions[i] + new Vector3(direction * (0.8f + i * 0.18f), 0.25f + i * 0.04f, -0.6f - i * 0.12f);
            targetRotations[i] = startRotations[i] * Quaternion.Euler(18f + i * 7f, direction * (35f + i * 9f), 24f);
        }

        float duration = Mathf.Max(0.01f, scatterDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            for (int i = 0; i < childCount; i++)
            {
                Transform child = scatterRoot.GetChild(i);
                child.localPosition = Vector3.Lerp(startPositions[i], targetPositions[i], t);
                child.localRotation = Quaternion.Slerp(startRotations[i], targetRotations[i], t);
            }

            yield return null;
        }
    }

    private bool LooksLikeChaseThreat(GameObject obj)
    {
        string objectName = obj.name;
        return objectName.Contains("S01_ChaseThreat") ||
               objectName.Contains("HacTinh") ||
               objectName.Contains("Hắc Tinh");
    }
}
