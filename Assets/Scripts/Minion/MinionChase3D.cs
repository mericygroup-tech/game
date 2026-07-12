using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinionChase3D : MonoBehaviour
{
    private const string MoveSpeedParameter = "MoveSpeed";
    private const string AttackParameter = "Attack";
    private const string DieParameter = "Die";

    public Transform target;

    public float moveSpeed = 3f;
    public float rotationSpeed = 10f;
    public float chaseRange = 20f;
    public float attackRange = 1.55f;
    public int damage = 20;
    public float attackCooldown = 1f;
    public float knockbackDamping = 10f;
    public float groundSnapHeight = 6f;
    public float groundSnapOffset = 0.02f;
    public LayerMask groundMask = ~0;
    public string idleState = "Idle";
    public string moveState = "Run";
    public string attackState = "Attack";
    public string hitState = "Hit";
    public string deathState = "Death";
    public float movingAnimationSpeed = 1.15f;
    public float attackAnimationSpeed = 1.2f;
    public float attackImpactDelay = 0.45f;
    public float attackLockDuration = 0.85f;
    public float attackEffectDuration = 0.28f;
    public Color attackEffectColor = new Color(0.9f, 0.08f, 0.04f, 0.72f);
    public bool dieAfterSuccessfulAttack;
    public float separationRadius = 1.35f;
    public float separationStrength = 0.45f;
    public float personalSpaceDistance = 1.15f;
    public float maxOverlapCorrection = 0.55f;
    public float visualGroundOffset = 0.45f;

    private float lastAttackTime;
    private float stunnedUntil;
    private float attackAnimationUntil;
    private Vector3 knockbackVelocity;
    private Animator[] visualAnimators;
    private string currentAnimationState;
    private bool warnedMissingAnimator;
    private bool warnedMissingController;
    private bool isAttacking;
    private bool isDying;
    private float aiPausedUntil;
    private float damageSuppressedUntil;
    private Coroutine attackRoutine;
    private readonly RaycastHit[] groundHits = new RaycastHit[12];
    private static readonly List<MinionChase3D> activeMinions = new List<MinionChase3D>();
    private static readonly Collider[] separationHits = new Collider[12];

    private void Awake()
    {
        RefreshVisualAnimators();
        SnapToGround();
    }

    private void OnEnable()
    {
        if (!activeMinions.Contains(this))
            activeMinions.Add(this);

        SnapToGround();
        ForceVisualAnimation(moveState);
    }

    private void OnDisable()
    {
        activeMinions.Remove(this);
    }

    private void OnDestroy()
    {
        activeMinions.Remove(this);
    }

    private void Update()
    {
        SnapToGround();
        ApplyKnockbackMotion();
        if (!isDying && !S01ChaseIntroCutscene.IsAnyIntroRunning)
            ResolveOverlaps();

        if (S01ChaseIntroCutscene.IsAnyIntroRunning)
        {
            PlayLocomotionState(false);
            return;
        }

        if (isDying)
            return;

        if (Time.time < aiPausedUntil)
        {
            PlayLocomotionState(false);
            return;
        }

        if (isAttacking)
            return;

        if (target == null)
        {
            PlayLocomotionState(false);
            return;
        }

        if (Time.time < stunnedUntil)
        {
            PlayAnimationIfAvailable(hitState, moveState, movingAnimationSpeed);
            return;
        }

        float distance = Vector3.Distance(transform.position, target.position);
        bool isMoving = false;

        if (distance <= chaseRange && distance > attackRange)
        {
            ChaseTarget();
            isMoving = true;
        }

        if (distance <= attackRange)
        {
            AttackTarget();
        }

        if (Time.time >= attackAnimationUntil)
            PlayLocomotionState(isMoving);
    }

    private void ChaseTarget()
    {
        Vector3 direction = target.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        direction = (direction.normalized + GetSeparationDirection()).normalized;
        transform.position += direction * moveSpeed * Time.deltaTime;
        SnapToGround();
        ResolveOverlaps();

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
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

            MinionChase3D otherMinion = hit.GetComponentInParent<MinionChase3D>();
            S01ChaseThreat routeThreat = hit.GetComponentInParent<S01ChaseThreat>();
            if ((otherMinion == null || otherMinion == this) && routeThreat == null)
                continue;

            Transform otherTransform = otherMinion != null && otherMinion != this ? otherMinion.transform : routeThreat.transform;
            Vector3 away = transform.position - otherTransform.position;
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

    private void AttackTarget()
    {
        if (isAttacking || Time.time < lastAttackTime + attackCooldown)
            return;

        if (Time.time < damageSuppressedUntil)
            return;

        attackRoutine = StartCoroutine(AttackSequence());
    }

    private IEnumerator AttackSequence()
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        float attackEndDelay = Mathf.Max(attackLockDuration, attackImpactDelay);
        attackAnimationUntil = Time.time + attackEndDelay;
        FaceTarget();
        PlayAnimationIfAvailable(attackState, moveState, attackAnimationSpeed, true);

        yield return new WaitForSeconds(Mathf.Max(0f, attackEndDelay));

        FaceTarget();
        SpawnAttackEffect(target);

        PlayerHealth3D playerHealth = target != null ? target.GetComponent<PlayerHealth3D>() : null;

        if (playerHealth != null && Time.time >= damageSuppressedUntil && IsTargetStillInAttackRange())
        {
            playerHealth.TakeDamage(damage);

            if (dieAfterSuccessfulAttack)
            {
                MinionHealth3D health = GetComponent<MinionHealth3D>();
                if (health != null)
                    health.TakeDamage(health.maxHP);
            }
        }

        Debug.Log("Minion attack finished.");

        isAttacking = false;
        attackRoutine = null;
    }

    public void SuppressAttacks(float duration)
    {
        damageSuppressedUntil = Mathf.Max(damageSuppressedUntil, Time.time + Mathf.Max(0f, duration));
        lastAttackTime = Time.time;
    }

    public void PauseAI(float duration)
    {
        float safeDuration = Mathf.Max(0f, duration);
        aiPausedUntil = Mathf.Max(aiPausedUntil, Time.time + safeDuration);
        SuppressAttacks(safeDuration);

        if (attackRoutine != null)
            StopCoroutine(attackRoutine);

        attackRoutine = null;
        isAttacking = false;
        attackAnimationUntil = 0f;
        PlayLocomotionState(false);
    }

    public void ResetForSpawn(Transform newTarget)
    {
        if (attackRoutine != null)
            StopCoroutine(attackRoutine);

        target = newTarget;
        isDying = false;
        isAttacking = false;
        attackRoutine = null;
        knockbackVelocity = Vector3.zero;
        stunnedUntil = 0f;
        attackAnimationUntil = 0f;
        aiPausedUntil = 0f;
        damageSuppressedUntil = 0f;
        transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        ForceSnapToGround();
        ForceVisualAnimation(moveState);
    }

    private void FaceTarget()
    {
        if (target == null)
            return;

        FacePosition(target.position);
    }

    private void FacePosition(Vector3 position)
    {
        Vector3 direction = position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.001f)
            return;

        transform.rotation = Quaternion.LookRotation(direction.normalized);
    }

    private bool IsTargetStillInAttackRange()
    {
        if (target == null)
            return false;

        float allowedRange = attackRange;
        Vector3 delta = target.position - transform.position;
        delta.y = 0f;
        return delta.sqrMagnitude <= allowedRange * allowedRange;
    }

    public void PlayAmbushAttackFeedback(Transform effectTarget)
    {
        RefreshVisualAnimators();

        if (effectTarget != null)
            FacePosition(effectTarget.position);

        PlayAnimationIfAvailable(attackState, moveState, attackAnimationSpeed, true);
        attackAnimationUntil = Time.time + Mathf.Max(attackLockDuration, attackImpactDelay);
        SpawnAttackEffect(effectTarget);
    }

    public void JoinChase(Transform newTarget, float newMoveSpeed = -1f, float newChaseRange = -1f)
    {
        target = newTarget;
        if (newMoveSpeed > 0f)
            moveSpeed = newMoveSpeed;
        if (newChaseRange > 0f)
            chaseRange = newChaseRange;

        isDying = false;
        isAttacking = false;
        attackRoutine = null;
        knockbackVelocity = Vector3.zero;
        aiPausedUntil = 0f;
        transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        enabled = true;
        ForceSnapToGround();
        ForceVisualAnimation(moveState);
    }

    private void SpawnAttackEffect(Transform effectTarget)
    {
        Vector3 forward = effectTarget != null ? effectTarget.position - transform.position : transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude <= 0.001f)
            forward = transform.forward;
        forward.Normalize();

        GameObject slash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        slash.name = "Minion_AttackImpact";
        slash.transform.position = transform.position + Vector3.up * 1.05f + forward * 0.85f;
        slash.transform.rotation = Quaternion.LookRotation(forward);
        slash.transform.localScale = new Vector3(1.25f, 0.18f, 0.55f);

        Collider collider = slash.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);

        Renderer renderer = slash.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material material = CreateAttackEffectMaterial();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            Destroy(material, attackEffectDuration + 0.05f);
        }

        Destroy(slash, attackEffectDuration);
        S01Soundscape.PlayImpactHit();
    }

    private Material CreateAttackEffectMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = new Material(shader)
        {
            name = "Runtime_Minion_AttackImpact"
        };

        material.color = attackEffectColor;
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", attackEffectColor);

        material.EnableKeyword("_EMISSION");
        if (material.HasProperty("_EmissionColor"))
            material.SetColor("_EmissionColor", attackEffectColor * 2.4f);

        return material;
    }

    public void ApplyKnockback(Vector3 direction, float force, float stunDuration)
    {
        if (isDying)
            return;

        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
            direction = -transform.forward;

        knockbackVelocity = direction.normalized * force;
        stunnedUntil = Mathf.Max(stunnedUntil, Time.time + stunDuration);
        PlayAnimationIfAvailable(hitState, moveState, movingAnimationSpeed, true);
    }

    private void ApplyKnockbackMotion()
    {
        if (knockbackVelocity.sqrMagnitude < 0.01f)
            return;

        transform.position += knockbackVelocity * Time.deltaTime;
        SnapToGround();
        ResolveOverlaps();
        knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, knockbackDamping * Time.deltaTime);
    }

    private void ResolveOverlaps()
    {
        float minimumDistance = Mathf.Max(0.05f, personalSpaceDistance);
        Vector3 correction = Vector3.zero;

        for (int i = activeMinions.Count - 1; i >= 0; i--)
        {
            MinionChase3D other = activeMinions[i];
            if (other == null)
            {
                activeMinions.RemoveAt(i);
                continue;
            }

            if (other == this || !other.isActiveAndEnabled)
                continue;

            float pairDistance = Mathf.Max(minimumDistance, other.personalSpaceDistance);
            correction += GetSeparationCorrection(other.transform.position, pairDistance);
        }

        IReadOnlyList<S01ChaseThreat> routeThreats = S01ChaseThreat.ActiveThreats;
        for (int i = 0; i < routeThreats.Count; i++)
        {
            S01ChaseThreat routeThreat = routeThreats[i];
            if (routeThreat == null || !routeThreat.isActiveAndEnabled)
                continue;

            float pairDistance = Mathf.Max(minimumDistance, routeThreat.personalSpaceDistance);
            correction += GetSeparationCorrection(routeThreat.transform.position, pairDistance);
        }

        if (correction.sqrMagnitude <= 0.0001f)
            return;

        float maxStep = Mathf.Max(0.01f, maxOverlapCorrection);
        if (correction.magnitude > maxStep)
            correction = correction.normalized * maxStep;

        transform.position += correction;
        SnapToGround();
    }

    private Vector3 GetSeparationCorrection(Vector3 otherPosition, float minimumDistance)
    {
        Vector3 away = transform.position - otherPosition;
        away.y = 0f;
        float distance = away.magnitude;

        if (distance >= minimumDistance)
            return Vector3.zero;

        if (distance <= 0.001f)
            away = Quaternion.Euler(0f, Mathf.Abs(GetInstanceID()) % 360, 0f) * Vector3.right;
        else
            away /= distance;

        float pushDistance = minimumDistance - distance;
        return away.normalized * pushDistance;
    }

    public void ForceSnapToGround()
    {
        SnapToGround();
    }

    private void SnapToGround()
    {
        Vector3 origin = transform.position + Vector3.up * groundSnapHeight;
        int hitCount = Physics.RaycastNonAlloc(
            origin,
            Vector3.down,
            groundHits,
            groundSnapHeight * 3f,
            groundMask,
            QueryTriggerInteraction.Ignore);

        if (hitCount <= 0)
        {
            SnapToFallbackHeight();
            return;
        }

        bool foundGround = false;
        RaycastHit groundHit = default;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = groundHits[i];
            if (hit.collider == null)
                continue;

            if (hit.collider.transform.IsChildOf(transform))
                continue;

            if (Vector3.Dot(hit.normal, Vector3.up) < 0.72f)
                continue;

            if (!IsValidGroundCollider(hit.collider))
                continue;

            if (hit.distance >= bestDistance)
                continue;

            bestDistance = hit.distance;
            groundHit = hit;
            foundGround = true;
        }

        if (!foundGround)
        {
            SnapToFallbackHeight();
            return;
        }

        transform.position = new Vector3(transform.position.x, groundHit.point.y + GetGroundOffset(), transform.position.z);
    }

    private void SnapToFallbackHeight()
    {
        float fallbackY = target != null ? target.position.y + GetGroundOffset() : GetGroundOffset();
        if (Mathf.Abs(transform.position.y - fallbackY) <= 0.08f)
            return;

        transform.position = new Vector3(transform.position.x, fallbackY, transform.position.z);
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

    private float GetGroundOffset()
    {
        return Mathf.Max(groundSnapOffset, visualGroundOffset);
    }

    public void PlayDeathAnimation()
    {
        if (attackRoutine != null)
            StopCoroutine(attackRoutine);

        isAttacking = false;
        attackRoutine = null;
        RefreshVisualAnimators();
        PlayAnimationIfAvailable(deathState, moveState, 1f, true);
    }

    public float PlayFinalAttackBeforeDeath()
    {
        isDying = true;

        if (attackRoutine != null)
            StopCoroutine(attackRoutine);

        attackRoutine = null;
        isAttacking = true;
        attackAnimationUntil = Time.time + Mathf.Max(attackLockDuration, attackImpactDelay);
        FaceTarget();
        PlayAnimationIfAvailable(attackState, moveState, attackAnimationSpeed, true);
        StartCoroutine(FinalAttackEffectSequence());
        return Mathf.Max(attackLockDuration, attackImpactDelay + attackEffectDuration);
    }

    public void FinishFinalAttack()
    {
        isAttacking = false;
        attackRoutine = null;
    }

    private IEnumerator FinalAttackEffectSequence()
    {
        yield return new WaitForSeconds(Mathf.Max(attackLockDuration, attackImpactDelay));
        SpawnAttackEffect(target);
    }

    private void PlayLocomotionState(bool isMoving)
    {
        if (Time.time < attackAnimationUntil)
            return;

        string preferredState = isMoving ? moveState : idleState;
        string fallbackState = isMoving ? idleState : moveState;
        float speed = isMoving ? movingAnimationSpeed : 1f;
        PlayAnimationIfAvailable(preferredState, fallbackState, speed);
    }

    private void SetVisualAnimationSpeed(float speed)
    {
        if (visualAnimators == null || visualAnimators.Length == 0)
            return;

        foreach (Animator animator in visualAnimators)
        {
            if (animator != null)
                animator.speed = speed;
        }
    }

    public void ForceVisualAnimation(string stateName = "Run")
    {
        RefreshVisualAnimators();

        if (visualAnimators == null || visualAnimators.Length == 0)
        {
            if (!warnedMissingAnimator)
            {
                warnedMissingAnimator = true;
                Debug.LogWarning("MinionChase3D: spawned minion has no child Animator: " + name, this);
            }

            return;
        }

        foreach (Animator animator in visualAnimators)
        {
            if (animator == null)
                continue;

            animator.enabled = true;
            animator.applyRootMotion = false;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.speed = 1f;

            if (animator.runtimeAnimatorController == null)
            {
                if (!warnedMissingController)
                {
                    warnedMissingController = true;
                    Debug.LogWarning("MinionChase3D: child Animator is missing RuntimeAnimatorController on " + animator.gameObject.name, animator);
                }

                continue;
            }

            animator.Rebind();
            animator.Update(0f);
        }

        PlayAnimationIfAvailable(stateName, idleState, movingAnimationSpeed, true);
    }

    private void RefreshVisualAnimators()
    {
        visualAnimators = GetComponentsInChildren<Animator>(true);
    }

    private void PlayAnimationIfAvailable(string preferredState, string fallbackState, float speed, bool forceRestart = false)
    {
        RefreshVisualAnimators();

        if (visualAnimators == null || visualAnimators.Length == 0)
            return;

        string stateToPlay = GetAvailableState(preferredState, fallbackState);
        if (string.IsNullOrWhiteSpace(stateToPlay))
            return;

        if (!forceRestart && currentAnimationState == stateToPlay)
        {
            SetVisualAnimationSpeed(speed);
            return;
        }

        int stateHash = Animator.StringToHash(stateToPlay);
        foreach (Animator animator in visualAnimators)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
                continue;

            animator.enabled = true;
            animator.applyRootMotion = false;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.speed = speed;
            SetAnimatorMoveSpeed(animator, stateToPlay == moveState ? 1f : 0f);

            if (forceRestart && stateToPlay == attackState)
                SetAnimatorTrigger(animator, AttackParameter);
            else if (forceRestart && stateToPlay == deathState)
                SetAnimatorTrigger(animator, DieParameter);

            animator.CrossFade(stateHash, 0.08f, 0, forceRestart ? 0f : Random.value);
        }

        currentAnimationState = stateToPlay;
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

    private string GetAvailableState(string preferredState, string fallbackState)
    {
        if (HasAnyAnimatorState(preferredState))
            return preferredState;

        if (HasAnyAnimatorState(fallbackState))
            return fallbackState;

        if (HasAnyAnimatorState(moveState))
            return moveState;

        return null;
    }

    private bool HasAnyAnimatorState(string stateName)
    {
        if (string.IsNullOrWhiteSpace(stateName) || visualAnimators == null)
            return false;

        int stateHash = Animator.StringToHash(stateName);
        foreach (Animator animator in visualAnimators)
        {
            if (animator != null &&
                animator.runtimeAnimatorController != null &&
                animator.HasState(0, stateHash))
            {
                return true;
            }
        }

        return false;
    }
}
