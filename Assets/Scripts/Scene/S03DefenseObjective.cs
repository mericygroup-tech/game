using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class S03DefenseObjective : MonoBehaviour
{
    [Header("Protected Upper Level")]
    [SerializeField] private Transform protectedZone;
    [SerializeField] private Vector3 protectedZoneSize = new Vector3(100f, 8f, 24f);
    [SerializeField] private bool killEnemyAfterBreach = true;
    [SerializeField] private float breachCheckInterval = 0.1f;

    [Header("Citadel Integrity")]
    [SerializeField] private int startingCitadelIntegrity = 100;
    [SerializeField] private int breachDamagePerEnemy = 20;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text integrityText;

    private readonly List<MinionHealth3D> trackedEnemies = new List<MinionHealth3D>();
    private readonly HashSet<MinionHealth3D> breachedEnemies = new HashSet<MinionHealth3D>();
    private int citadelIntegrity;
    private bool missionFailed;
    private bool missionSucceeded;
    private float nextBreachCheckTime;

    public int CitadelIntegrity => citadelIntegrity;
    public bool HasFailed => missionFailed;
    public bool HasSucceeded => missionSucceeded;
    public string IntegrityLabel => "Citadel Integrity: " + citadelIntegrity + "%";

    private void Awake()
    {
        ResetObjective();
    }

    private void Update()
    {
        if (missionFailed || missionSucceeded || trackedEnemies.Count == 0)
            return;

        if (Time.unscaledTime < nextBreachCheckTime)
            return;

        nextBreachCheckTime = Time.unscaledTime + Mathf.Max(0.02f, breachCheckInterval);
        CheckForBreaches();
    }

    public void Configure(TMP_Text statusLabel, TMP_Text integrityLabel)
    {
        if (statusLabel != null)
            statusText = statusLabel;

        if (integrityLabel != null)
            integrityText = integrityLabel;

        RefreshIntegrityText();
    }

    public void ResetObjective()
    {
        citadelIntegrity = Mathf.Clamp(startingCitadelIntegrity, 1, 1000);
        missionFailed = false;
        missionSucceeded = false;
        trackedEnemies.Clear();
        breachedEnemies.Clear();
        RefreshIntegrityText();
    }

    public void RegisterEnemy(MinionHealth3D enemy)
    {
        if (enemy == null || trackedEnemies.Contains(enemy))
            return;

        trackedEnemies.Add(enemy);
    }

    public void ClearTrackedEnemies()
    {
        trackedEnemies.Clear();
        breachedEnemies.Clear();
    }

    public bool CheckForBreaches()
    {
        bool anyBreach = false;
        for (int i = trackedEnemies.Count - 1; i >= 0; i--)
        {
            MinionHealth3D enemy = trackedEnemies[i];
            if (enemy == null || enemy.IsDead)
            {
                trackedEnemies.RemoveAt(i);
                continue;
            }

            if (!IsInsideProtectedZone(enemy.transform.position))
                continue;

            trackedEnemies.RemoveAt(i);
            HandleBreach(enemy);
            anyBreach = true;
        }

        return anyBreach;
    }

    public string FormatStatus(string message)
    {
        return message + "\n" + IntegrityLabel;
    }

    public void MarkSucceeded(string message)
    {
        if (missionFailed || missionSucceeded)
            return;

        missionSucceeded = true;
        trackedEnemies.Clear();
        RefreshIntegrityText();

        if (statusText != null)
            statusText.text = FormatStatus(message);
    }

    public void MarkFailed(string message)
    {
        if (missionFailed || missionSucceeded)
            return;

        missionFailed = true;
        RefreshIntegrityText();
        StopTrackedEnemies();

        if (statusText != null)
            statusText.text = FormatStatus(message);
    }

    private void HandleBreach(MinionHealth3D enemy)
    {
        if (enemy == null || !breachedEnemies.Add(enemy))
            return;

        citadelIntegrity = Mathf.Max(0, citadelIntegrity - Mathf.Max(1, breachDamagePerEnemy));
        RefreshIntegrityText();

        if (statusText != null)
            statusText.text = FormatStatus("A Hac Tinh breached the upper civilian level.");

        if (killEnemyAfterBreach)
            enemy.TakeDamage(Mathf.Max(enemy.currentHP, enemy.maxHP) + 1000);

        if (citadelIntegrity <= 0)
            MarkFailed("The upper levels have fallen. Civilians are in danger.");
    }

    private bool IsInsideProtectedZone(Vector3 worldPosition)
    {
        Transform zone = protectedZone != null ? protectedZone : transform;
        Vector3 localPosition = Quaternion.Inverse(zone.rotation) * (worldPosition - zone.position);
        Vector3 halfSize = protectedZoneSize * 0.5f;

        return Mathf.Abs(localPosition.x) <= halfSize.x
            && Mathf.Abs(localPosition.y) <= halfSize.y
            && Mathf.Abs(localPosition.z) <= halfSize.z;
    }

    private void StopTrackedEnemies()
    {
        for (int i = 0; i < trackedEnemies.Count; i++)
        {
            MinionHealth3D enemy = trackedEnemies[i];
            if (enemy == null)
                continue;

            MinionChase3D chase = enemy.GetComponent<MinionChase3D>();
            if (chase != null)
                chase.enabled = false;
        }
    }

    private void RefreshIntegrityText()
    {
        if (integrityText != null)
            integrityText.text = IntegrityLabel;
    }
}
