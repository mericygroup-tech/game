using UnityEngine;

public sealed class BlessingBarrierZone : MonoBehaviour
{
    private readonly Collider[] hits = new Collider[32];
    private float radius = 2.5f;
    private float duration = 2f;
    private float stunDuration = 0.25f;
    private float elapsed;
    private float nextTick;
    private Color color = Color.cyan;
    private Vector3 baseScale = Vector3.one;

    public void Configure(float zoneRadius, float zoneDuration, float enemyStunDuration, Color zoneColor)
    {
        radius = Mathf.Max(0.5f, zoneRadius);
        duration = Mathf.Max(0.1f, zoneDuration);
        stunDuration = Mathf.Max(0.05f, enemyStunDuration);
        color = zoneColor;
        baseScale = transform.localScale;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        if (elapsed >= duration)
        {
            Destroy(gameObject);
            return;
        }

        float pulse = 1f + Mathf.Sin(Time.time * 12f) * 0.06f;
        transform.localScale = new Vector3(baseScale.x * pulse, baseScale.y, baseScale.z * pulse);

        if (Time.time < nextTick)
            return;

        nextTick = Time.time + 0.22f;
        AffectEnemies();
    }

    private void AffectEnemies()
    {
        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, radius, hits, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = hits[i];
            hits[i] = null;
            if (hit == null)
                continue;

            MinionChase3D minion = hit.GetComponentInParent<MinionChase3D>();
            if (minion == null)
                continue;

            Vector3 away = minion.transform.position - transform.position;
            away.y = 0f;
            if (away.sqrMagnitude < 0.001f)
                away = minion.transform.forward;

            minion.ApplyKnockback(away.normalized, 1.15f, stunDuration);
            minion.SuppressAttacks(0.3f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = color;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
