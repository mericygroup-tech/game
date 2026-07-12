using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCombat3D : MonoBehaviour
{
    [Header("Light Attack")]
    public int damage = 24;
    public float attackRange = 5.2f;
    public float attackAngle = 95f;
    public float closeHitRadius = 1.35f;
    public float attackCooldown = 0.65f;
    public float knockbackForce = 6.5f;
    public float enemyStunDuration = 0.42f;

    [Header("Heavy Attack")]
    public int heavyDamage = 58;
    public float heavyAttackRange = 5.9f;
    public float heavyAttackAngle = 125f;
    public float heavyCloseHitRadius = 1.75f;
    public float heavyAttackCooldown = 1.1f;
    public float heavyWindup = 0.18f;
    public float heavyKnockbackForce = 10.5f;
    public float heavyEnemyStunDuration = 0.68f;
    public Color heavyFeedbackColor = new Color(1f, 0.58f, 0.12f, 0.55f);

    [Header("Feedback")]
    public Camera aimCamera;
    public LayerMask aimGroundMask = ~0;
    public float feedbackDuration = 0.18f;
    public Color feedbackColor = new Color(0.15f, 0.9f, 1f, 0.45f);
    public bool faceAttackDirection = true;

    private float attackLockedUntil;
    private Coroutine heavyAttackRoutine;
    private Material feedbackMaterial;
    private PlayerController3D playerController;
    private InputSettingsManager inputManager;
    private PlayerAnimatorDriver animatorDriver;
    private BlessingRuntimeController blessingRuntime;
    private readonly List<MinionHealth3D> hitEnemies = new List<MinionHealth3D>(16);

    public bool IsHeavyAttackActive { get; private set; }
    public bool IsAttacking => IsHeavyAttackActive || Time.time < attackLockedUntil;

    private void Awake()
    {
        playerController = GetComponent<PlayerController3D>();
        inputManager = GetComponent<InputSettingsManager>();
        if (inputManager == null)
        {
            inputManager = GetComponentInParent<InputSettingsManager>();
        }

        blessingRuntime = GetComponent<BlessingRuntimeController>();
        animatorDriver = GetComponent<PlayerAnimatorDriver>();
    }

    private void Update()
    {
        if (playerController != null && (playerController.InputLocked || playerController.IsDashing))
            return;

        KeyCode attackKey = (inputManager != null && inputManager.Keyboard != null) ? inputManager.Keyboard.attack : KeyCode.Mouse0;
        
        // Handle Mouse0 or other keys through Input.GetKeyDown
        bool attackPressed = false;
        if (attackKey == KeyCode.Mouse0)
            attackPressed = Input.GetMouseButtonDown(0);
        else if (attackKey == KeyCode.Mouse1)
            attackPressed = Input.GetMouseButtonDown(1);
        else if (attackKey == KeyCode.Mouse2)
            attackPressed = Input.GetMouseButtonDown(2);
        else
            attackPressed = Input.GetKeyDown(attackKey);

        if (Input.GetMouseButtonDown(1))
        {
            StartHeavyAttack();
            return;
        }

        if (attackPressed)
        {
            Attack();
        }
    }

    private void Attack()
    {
        if (Time.time < attackLockedUntil || IsHeavyAttackActive)
            return;

        attackLockedUntil = Time.time + attackCooldown;
        Vector3 attackDirection = GetAttackDirection();
        FaceAttackDirection(attackDirection);
        animatorDriver?.PlayLightAttack();

        ExecuteAttack(
            damage,
            attackRange,
            attackAngle,
            closeHitRadius,
            knockbackForce,
            enemyStunDuration,
            feedbackColor,
            attackDirection,
            "Light attack"
        );
    }

    private void StartHeavyAttack()
    {
        if (Time.time < attackLockedUntil || IsHeavyAttackActive)
            return;

        attackLockedUntil = Time.time + heavyAttackCooldown;

        Vector3 attackDirection = GetAttackDirection();
        FaceAttackDirection(attackDirection);
        animatorDriver?.PlayPush();

        if (heavyAttackRoutine != null)
            StopCoroutine(heavyAttackRoutine);

        heavyAttackRoutine = StartCoroutine(HeavyAttackSequence(attackDirection));
    }

    private IEnumerator HeavyAttackSequence(Vector3 attackDirection)
    {
        IsHeavyAttackActive = true;
        SpawnAttackFeedback(attackDirection, false, heavyFeedbackColor, heavyAttackRange * 0.72f, "HeavyCharge");

        if (heavyWindup > 0f)
            yield return new WaitForSeconds(heavyWindup);

        ExecuteAttack(
            heavyDamage,
            heavyAttackRange,
            heavyAttackAngle,
            heavyCloseHitRadius,
            heavyKnockbackForce,
            heavyEnemyStunDuration,
            heavyFeedbackColor,
            attackDirection,
            "Heavy attack"
        );

        IsHeavyAttackActive = false;
        heavyAttackRoutine = null;
    }

    private void ExecuteAttack(
        int rawDamage,
        float range,
        float angle,
        float closeRadius,
        float knockback,
        float stunDuration,
        Color pulseColor,
        Vector3 attackDirection,
        string debugLabel)
    {
        BlessingAttackContext attackContext = blessingRuntime != null
            ? blessingRuntime.CreateAttackContext(rawDamage, attackDirection)
            : BlessingAttackContext.Plain(rawDamage);

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        int hitCount = 0;
        hitEnemies.Clear();

        foreach (GameObject enemy in enemies)
        {
            if (enemy == null)
                continue;

            MinionHealth3D enemyHealth = enemy.GetComponent<MinionHealth3D>();
            if (enemyHealth == null || enemyHealth.IsDead)
                continue;

            Vector3 toEnemy = enemy.transform.position - transform.position;
            toEnemy.y = 0f;
            float distance = toEnemy.magnitude;

            if (!IsInsideAttackArea(toEnemy, distance, attackDirection, range, angle, closeRadius))
                continue;

            bool wasDead = enemyHealth.IsDead;
            enemyHealth.TakeDamage(attackContext.Damage);
            hitEnemies.Add(enemyHealth);

            if (!wasDead && enemyHealth.IsDead)
            {
                blessingRuntime?.OnEnemyKilled(enemyHealth.transform.position);
                hitCount++;
                continue;
            }

            blessingRuntime?.OnEnemyHit(enemyHealth, enemyHealth.transform.position, attackContext);

            MinionChase3D chase = enemy.GetComponent<MinionChase3D>();
            if (chase != null)
            {
                Vector3 knockbackDirection = distance > 0.1f ? toEnemy.normalized : attackDirection;
                chase.ApplyKnockback(knockbackDirection, knockback, stunDuration);
            }

            hitCount++;
        }

        blessingRuntime?.AfterPlayerAttack(attackContext, attackDirection, hitEnemies);
        SpawnAttackFeedback(attackDirection, hitCount > 0, pulseColor, range * 0.78f, debugLabel.Replace(" ", string.Empty));
        Debug.Log("PlayerCombat3D: " + debugLabel + " hit enemies: " + hitCount);
    }

    public bool CanCancelCurrentAttack()
    {
        // Light attacks can be canceled; Heavy attacks cannot.
        return IsAttacking && !IsHeavyAttackActive;
    }

    public void CancelAttack()
    {
        // Reset the light attack lock so Dash can cancel quick strikes cleanly.
        attackLockedUntil = 0f;
        Debug.Log("PlayerCombat3D: Attack canceled by Dash.");
    }

    private Vector3 GetAttackDirection()
    {
        if (aimCamera == null)
            aimCamera = Camera.main;

        if (aimCamera != null)
        {
            Ray ray = aimCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 120f, aimGroundMask, QueryTriggerInteraction.Ignore))
            {
                Vector3 cursorDirection = hit.point - transform.position;
                cursorDirection.y = 0f;

                if (cursorDirection.sqrMagnitude > 0.01f)
                    return cursorDirection.normalized;
            }
        }

        Vector3 direction = aimCamera != null ? aimCamera.transform.forward : transform.forward;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            direction = transform.forward;

        return direction.normalized;
    }

    private bool IsInsideAttackArea(Vector3 toEnemy, float distance, Vector3 attackDirection, float range, float angle, float closeRadius)
    {
        if (distance <= closeRadius)
            return true;

        if (distance > range || toEnemy.sqrMagnitude < 0.001f)
            return false;

        float attackArc = Vector3.Angle(attackDirection, toEnemy.normalized);
        return attackArc <= angle * 0.5f;
    }

    private void FaceAttackDirection(Vector3 attackDirection)
    {
        if (!faceAttackDirection || attackDirection.sqrMagnitude <= 0.001f)
            return;

        transform.rotation = Quaternion.LookRotation(attackDirection.normalized);
    }

    private void SpawnAttackFeedback(Vector3 attackDirection, bool hit, Color pulseColor, float pulseLength, string pulseName)
    {
        GameObject pulse = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pulse.name = hit ? pulseName + "_HitPulse" : pulseName + "_MissPulse";
        float clampedLength = Mathf.Clamp(pulseLength, 2.6f, 5.4f);
        pulse.transform.position = transform.position + Vector3.up * 1.05f + attackDirection * (clampedLength * 0.55f);
        pulse.transform.rotation = Quaternion.LookRotation(attackDirection);
        pulse.transform.localScale = new Vector3(hit ? 2.65f : 2.25f, 0.14f, clampedLength);

        Collider collider = pulse.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);

        Renderer renderer = pulse.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = GetFeedbackMaterial(hit, pulseColor);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        Destroy(pulse, feedbackDuration);
    }

    private Material GetFeedbackMaterial(bool hit, Color baseColor)
    {
        if (feedbackMaterial == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            feedbackMaterial = new Material(shader)
            {
                name = "Runtime_ResonanceCounter_Feedback"
            };

            feedbackMaterial.EnableKeyword("_EMISSION");
        }

        Color color = hit ? baseColor : new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * 0.45f);
        feedbackMaterial.color = color;

        if (feedbackMaterial.HasProperty("_BaseColor"))
            feedbackMaterial.SetColor("_BaseColor", color);

        if (feedbackMaterial.HasProperty("_EmissionColor"))
            feedbackMaterial.SetColor("_EmissionColor", color * 1.8f);

        return feedbackMaterial;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = new Color(1f, 0.58f, 0.12f, 0.55f);
        Gizmos.DrawWireSphere(transform.position, heavyAttackRange);
    }

    private void OnDisable()
    {
        if (heavyAttackRoutine != null)
        {
            StopCoroutine(heavyAttackRoutine);
            heavyAttackRoutine = null;
        }

        IsHeavyAttackActive = false;
    }
}
