using System.Collections.Generic;
using UnityEngine;

public struct BlessingAttackContext
{
    public int Damage;
    public bool IsCritical;
    public bool TriggerDivineCrossbow;
    public int ExtraProjectiles;
    public float LightningChance;
    public int LightningDamage;
    public float LightningRadius;

    public static BlessingAttackContext Plain(int damage)
    {
        return new BlessingAttackContext
        {
            Damage = Mathf.Max(0, damage),
            IsCritical = false,
            TriggerDivineCrossbow = false,
            ExtraProjectiles = 0,
            LightningChance = 0f,
            LightningDamage = 0,
            LightningRadius = 0f
        };
    }
}

public sealed class BlessingRuntimeController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController3D playerController;
    [SerializeField] private PlayerCombat3D playerCombat;
    [SerializeField] private PlayerHealth3D playerHealth;

    [Header("Runtime Feedback")]
    [SerializeField] private Color defenseColor = new Color(0.1f, 0.72f, 1f, 0.55f);
    [SerializeField] private Color offenseColor = new Color(1f, 0.58f, 0.08f, 0.62f);
    [SerializeField] private Color mobilityColor = new Color(0.25f, 1f, 0.62f, 0.58f);
    [SerializeField] private Color leadershipColor = new Color(0.95f, 0.18f, 0.32f, 0.58f);

    private readonly Dictionary<string, BlessingDefinition> blessingsById = new Dictionary<string, BlessingDefinition>();
    private readonly Dictionary<string, int> blessingStacks = new Dictionary<string, int>();
    private readonly HashSet<MinionHealth3D> dashHitTargets = new HashSet<MinionHealth3D>();
    private readonly Collider[] overlapHits = new Collider[48];
    private readonly List<MinionHealth3D> reusableTargets = new List<MinionHealth3D>(16);

    private float baseMoveSpeed;
    private float baseDashCooldown;
    private float baseAttackCooldown;
    private float coLoaTimer;
    private float dashHitTimer;
    private float frenzyUntil;
    private int attackCounter;
    private int killEnergyProgress;
    private bool wasDashing;
    private bool postDashReady;
    private bool choiceMode;
    private bool inputWasLockedBeforeChoice;

    private bool IsFrenzyActive => Time.time < frenzyUntil;

    private void Awake()
    {
        ResolveReferences();
        CaptureBaseStats();
        RecalculatePassiveStats();
    }

    private void Update()
    {
        if (playerController == null)
            return;

        bool isDashing = playerController.IsDashing;
        if (isDashing && !wasDashing)
            BeginDashBlessings();

        if (isDashing)
            TickDashDamage();

        if (!isDashing && wasDashing)
            EndDashBlessings();

        wasDashing = isDashing;
        TickCoLoaCitadel();
        RecalculatePassiveStats();
    }

    public void Configure(PlayerController3D controller, PlayerCombat3D combat, PlayerHealth3D health)
    {
        playerController = controller;
        playerCombat = combat;
        playerHealth = health;
        CaptureBaseStats();
        RecalculatePassiveStats();
    }

    public void SetChoiceMode(bool enabled)
    {
        if (choiceMode == enabled)
            return;

        choiceMode = enabled;
        if (playerController != null)
        {
            if (enabled)
                inputWasLockedBeforeChoice = playerController.InputLocked;

            playerController.InputLocked = enabled || inputWasLockedBeforeChoice;
        }

        Cursor.visible = enabled;
        if (enabled)
            Cursor.lockState = CursorLockMode.None;
    }

    public void ApplyBlessing(BlessingDefinition blessing, int newStack)
    {
        if (blessing == null)
            return;

        int oldEffectStack = GetStack(blessing.EffectType);
        blessingsById[blessing.Id] = blessing;
        blessingStacks[blessing.Id] = Mathf.Clamp(newStack, 0, blessing.MaxStack);
        int newEffectStack = GetStack(blessing.EffectType);

        if (blessing.EffectType == BlessingEffectType.Revive && playerHealth != null && newEffectStack > oldEffectStack)
            playerHealth.GrantReviveCharges(newEffectStack - oldEffectStack);

        if (blessing.EffectType == BlessingEffectType.KyDauFrenzy)
            ActivateFrenzy(7.5f);

        if (blessing.EffectType == BlessingEffectType.CoLoaCitadel)
        {
            coLoaTimer = 0f;
            GrantCoLoaShield();
        }

        RecalculatePassiveStats();
        SpawnPulse(transform.position + Vector3.up * 1.1f, GetHeroColor(blessing.HeroType), 1.8f, 0.75f, "Blessing_AppliedPulse");
    }

    public int GetStack(BlessingEffectType effectType)
    {
        int total = 0;
        foreach (KeyValuePair<string, int> stackPair in blessingStacks)
        {
            if (blessingsById.TryGetValue(stackPair.Key, out BlessingDefinition blessing) &&
                blessing != null &&
                blessing.EffectType == effectType)
            {
                total += stackPair.Value;
            }
        }

        return total;
    }

    public bool HasEffect(BlessingEffectType effectType)
    {
        return GetStack(effectType) > 0;
    }

    public void OnWaveStarted(int waveIndex)
    {
        if (HasEffect(BlessingEffectType.KyDauFrenzy))
            ActivateFrenzy(6.5f);

        if (HasEffect(BlessingEffectType.CoLoaCitadel))
            GrantCoLoaShield();
    }

    public float GetAwarenessSpawnDelay()
    {
        int stack = GetStack(BlessingEffectType.Awareness);
        return stack <= 0 ? 0f : Mathf.Min(2.2f, 0.7f + stack * 0.35f);
    }

    public float GetEnemyAwarenessRangeBonus()
    {
        return GetStack(BlessingEffectType.Awareness) * 2.5f;
    }

    public BlessingAttackContext CreateAttackContext(int baseDamage, Vector3 attackDirection)
    {
        attackCounter++;
        BlessingAttackContext context = BlessingAttackContext.Plain(baseDamage);

        float damage = baseDamage;
        damage *= GetLowHealthDamageMultiplier();
        damage *= GetUprisingDamageMultiplier();

        if (IsFrenzyActive)
            damage *= 1.25f;

        if (postDashReady)
        {
            int postDashStack = GetStack(BlessingEffectType.PostDashDamage);
            damage *= 1f + postDashStack * 0.35f;
            postDashReady = false;
        }

        int criticalPowerStack = GetStack(BlessingEffectType.CriticalPower);
        int lightningStack = GetStack(BlessingEffectType.CriticalLightning);
        float critChance = 0.05f + criticalPowerStack * 0.045f + lightningStack * 0.03f;
        bool isCritical = Random.value < critChance;
        if (isCritical)
            damage *= 1.5f + criticalPowerStack * 0.28f;

        int crossbowStack = GetStack(BlessingEffectType.DivineCrossbow);
        bool triggerCrossbow = crossbowStack > 0 && attackCounter % 5 == 0;

        context.Damage = Mathf.Max(1, Mathf.RoundToInt(damage));
        context.IsCritical = isCritical;
        context.TriggerDivineCrossbow = triggerCrossbow;
        context.ExtraProjectiles = triggerCrossbow ? 3 : 0;
        context.LightningChance = isCritical && lightningStack > 0 ? Mathf.Clamp01(0.2f + lightningStack * 0.12f) : 0f;
        context.LightningDamage = Mathf.RoundToInt(baseDamage * (0.55f + lightningStack * 0.22f));
        context.LightningRadius = 2.2f + lightningStack * 0.25f;
        return context;
    }

    public void OnEnemyHit(MinionHealth3D enemy, Vector3 hitPosition, BlessingAttackContext context)
    {
        if (enemy == null || !context.IsCritical || context.LightningChance <= 0f)
            return;

        if (Random.value > context.LightningChance)
            return;

        ApplyAreaDamage(hitPosition, context.LightningRadius, context.LightningDamage, offenseColor, "Blessing_LightningStrike");
    }

    public void AfterPlayerAttack(BlessingAttackContext context, Vector3 attackDirection, IReadOnlyCollection<MinionHealth3D> alreadyHit)
    {
        if (!context.TriggerDivineCrossbow || context.ExtraProjectiles <= 0)
            return;

        int crossbowStack = GetStack(BlessingEffectType.DivineCrossbow);
        int arrowDamage = Mathf.Max(1, Mathf.RoundToInt(context.Damage * (0.42f + crossbowStack * 0.08f)));
        CollectNearestEnemies(transform.position, 11f, reusableTargets, alreadyHit);

        int shots = Mathf.Min(context.ExtraProjectiles, reusableTargets.Count);
        for (int i = 0; i < shots; i++)
        {
            MinionHealth3D target = reusableTargets[i];
            if (target == null || target.IsDead)
                continue;

            bool wasDead = target.IsDead;
            target.TakeDamage(arrowDamage);
            SpawnLine(transform.position + Vector3.up * 1.1f, target.transform.position + Vector3.up * 0.8f, defenseColor, "Blessing_NoThanArrow");

            if (!wasDead && target.IsDead)
                OnEnemyKilled(target.transform.position);
        }
    }

    public void OnEnemyKilled(Vector3 killPosition)
    {
        int energyStack = GetStack(BlessingEffectType.KillSkillEnergy);
        if (energyStack <= 0)
            return;

        killEnergyProgress++;
        int threshold = Mathf.Max(1, 4 - energyStack);
        if (killEnergyProgress < threshold)
            return;

        killEnergyProgress = 0;
        if (playerController != null)
            playerController.RestoreDashCharge();

        if (playerHealth != null)
            playerHealth.Heal(2 + energyStack * 2);

        SpawnPulse(killPosition + Vector3.up * 0.7f, leadershipColor, 1.4f, 0.45f, "Blessing_KhoiNghiaMeLinh");
    }

    private void ResolveReferences()
    {
        if (playerController == null)
            playerController = GetComponent<PlayerController3D>();
        if (playerCombat == null)
            playerCombat = GetComponent<PlayerCombat3D>();
        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth3D>();
    }

    private void CaptureBaseStats()
    {
        ResolveReferences();
        if (playerController != null)
        {
            baseMoveSpeed = playerController.moveSpeed;
            baseDashCooldown = playerController.dashCooldown;
        }

        if (playerCombat != null)
            baseAttackCooldown = playerCombat.attackCooldown;
    }

    private void RecalculatePassiveStats()
    {
        if (playerController != null)
        {
            float moveMultiplier = 1f + GetStack(BlessingEffectType.MoveSpeed) * 0.08f;
            if (IsFrenzyActive)
                moveMultiplier += 0.12f;

            playerController.moveSpeed = Mathf.Max(2f, baseMoveSpeed * moveMultiplier);

            float cooldownMultiplier = 1f - GetStack(BlessingEffectType.DashCooldown) * 0.1f;
            if (IsFrenzyActive)
                cooldownMultiplier -= 0.18f;

            playerController.dashCooldown = Mathf.Max(0.12f, baseDashCooldown * Mathf.Clamp(cooldownMultiplier, 0.35f, 1f));
        }

        if (playerCombat != null)
        {
            float attackSpeed = 1f + GetStack(BlessingEffectType.AttackSpeed) * 0.11f;
            if (IsFrenzyActive)
                attackSpeed += 0.22f;

            playerCombat.attackCooldown = Mathf.Max(0.16f, baseAttackCooldown / attackSpeed);
        }

        if (playerHealth != null)
        {
            float reduction = GetStack(BlessingEffectType.Armor) * 0.075f;
            reduction += GetStack(BlessingEffectType.CoLoaCitadel) * 0.045f;
            playerHealth.DamageReduction = Mathf.Clamp(reduction, 0f, 0.65f);
        }
    }

    private void BeginDashBlessings()
    {
        dashHitTargets.Clear();
        dashHitTimer = 0f;

        int barrierStack = GetStack(BlessingEffectType.DashBarrier);
        if (barrierStack > 0)
            CreateBarrier(transform.position, barrierStack);

        int decoyStack = GetStack(BlessingEffectType.DashDecoy);
        if (decoyStack > 0)
            CreateDecoy(transform.position, decoyStack);

        TickDashDamage(true);
    }

    private void TickDashDamage(bool force = false)
    {
        int dashStack = GetStack(BlessingEffectType.DashDamage);
        int elephantStack = GetStack(BlessingEffectType.WarElephant);
        if (dashStack <= 0 && elephantStack <= 0)
            return;

        dashHitTimer += Time.deltaTime;
        if (!force && dashHitTimer < 0.045f)
            return;

        dashHitTimer = 0f;
        float radius = elephantStack > 0 ? 2.05f : 1.35f;
        int damage = 12 * dashStack + 34 * elephantStack;
        int hitCount = Physics.OverlapSphereNonAlloc(transform.position + Vector3.up * 0.75f, radius, overlapHits, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = overlapHits[i];
            overlapHits[i] = null;
            if (hit == null)
                continue;

            MinionHealth3D enemy = hit.GetComponentInParent<MinionHealth3D>();
            if (enemy == null || enemy.IsDead || dashHitTargets.Contains(enemy))
                continue;

            dashHitTargets.Add(enemy);
            bool wasDead = enemy.IsDead;
            enemy.TakeDamage(Mathf.Max(1, damage));

            MinionChase3D chase = enemy.GetComponent<MinionChase3D>();
            if (chase != null)
            {
                Vector3 away = enemy.transform.position - transform.position;
                chase.ApplyKnockback(away, elephantStack > 0 ? 5.5f : 2.4f, 0.28f);
            }

            SpawnPulse(enemy.transform.position + Vector3.up * 0.75f, mobilityColor, elephantStack > 0 ? 2f : 1.15f, 0.32f, "Blessing_DashImpact");
            if (!wasDead && enemy.IsDead)
                OnEnemyKilled(enemy.transform.position);
        }
    }

    private void EndDashBlessings()
    {
        if (GetStack(BlessingEffectType.PostDashDamage) > 0)
            postDashReady = true;
    }

    private void TickCoLoaCitadel()
    {
        if (choiceMode || playerHealth == null || !HasEffect(BlessingEffectType.CoLoaCitadel))
            return;

        coLoaTimer += Time.deltaTime;
        if (coLoaTimer < 11f)
            return;

        coLoaTimer = 0f;
        GrantCoLoaShield();
    }

    private void GrantCoLoaShield()
    {
        if (playerHealth == null)
            return;

        int shield = 18 + GetStack(BlessingEffectType.Armor) * 5;
        playerHealth.AddShield(shield);
        SpawnPulse(transform.position + Vector3.up * 0.7f, defenseColor, 3.2f, 0.9f, "Blessing_CoLoaShield");
    }

    private float GetLowHealthDamageMultiplier()
    {
        int stack = GetStack(BlessingEffectType.LowHealthDamage);
        if (stack <= 0 || playerHealth == null || playerHealth.maxHP <= 0)
            return 1f;

        float missingHealth = 1f - Mathf.Clamp01(playerHealth.currentHP / (float)playerHealth.maxHP);
        return 1f + missingHealth * stack * 0.32f;
    }

    private float GetUprisingDamageMultiplier()
    {
        int stack = GetStack(BlessingEffectType.Uprising);
        if (stack <= 0)
            return 1f;

        int nearbyEnemies = CountNearbyEnemies(transform.position, 6.5f);
        return 1f + Mathf.Min(0.75f, nearbyEnemies * stack * 0.045f);
    }

    private void ActivateFrenzy(float duration)
    {
        frenzyUntil = Mathf.Max(frenzyUntil, Time.time + Mathf.Max(0f, duration));
        SpawnPulse(transform.position + Vector3.up * 1f, offenseColor, 2.5f, 0.8f, "Blessing_XuanKyDau");
    }

    private void ApplyAreaDamage(Vector3 center, float radius, int damage, Color color, string effectName)
    {
        int hitCount = Physics.OverlapSphereNonAlloc(center, radius, overlapHits, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = overlapHits[i];
            overlapHits[i] = null;
            if (hit == null)
                continue;

            MinionHealth3D enemy = hit.GetComponentInParent<MinionHealth3D>();
            if (enemy == null || enemy.IsDead)
                continue;

            bool wasDead = enemy.IsDead;
            enemy.TakeDamage(Mathf.Max(1, damage));
            if (!wasDead && enemy.IsDead)
                OnEnemyKilled(enemy.transform.position);
        }

        SpawnPulse(center + Vector3.up * 0.2f, color, radius * 0.9f, 0.45f, effectName);
    }

    private int CountNearbyEnemies(Vector3 center, float radius)
    {
        int count = 0;
        int hitCount = Physics.OverlapSphereNonAlloc(center, radius, overlapHits, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = overlapHits[i];
            overlapHits[i] = null;
            if (hit == null)
                continue;

            MinionHealth3D enemy = hit.GetComponentInParent<MinionHealth3D>();
            if (enemy != null && !enemy.IsDead)
                count++;
        }

        return count;
    }

    private void CollectNearestEnemies(Vector3 center, float radius, List<MinionHealth3D> results, IReadOnlyCollection<MinionHealth3D> excluded)
    {
        results.Clear();
        int hitCount = Physics.OverlapSphereNonAlloc(center, radius, overlapHits, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = overlapHits[i];
            overlapHits[i] = null;
            if (hit == null)
                continue;

            MinionHealth3D enemy = hit.GetComponentInParent<MinionHealth3D>();
            if (enemy == null || enemy.IsDead || results.Contains(enemy))
                continue;

            if (ContainsTarget(excluded, enemy))
                continue;

            results.Add(enemy);
        }

        results.Sort((left, right) =>
            Vector3.SqrMagnitude(left.transform.position - center).CompareTo(Vector3.SqrMagnitude(right.transform.position - center)));
    }

    private static bool ContainsTarget(IReadOnlyCollection<MinionHealth3D> collection, MinionHealth3D target)
    {
        if (collection == null || target == null)
            return false;

        foreach (MinionHealth3D item in collection)
        {
            if (item == target)
                return true;
        }

        return false;
    }

    private void CreateBarrier(Vector3 position, int stack)
    {
        GameObject barrier = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        barrier.name = "Blessing_TuongThanh_Barrier";
        barrier.transform.position = position + Vector3.up * 0.08f;
        barrier.transform.localScale = new Vector3(3.5f + stack * 0.35f, 0.08f, 3.5f + stack * 0.35f);

        Collider collider = barrier.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);

        Renderer renderer = barrier.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = CreateRuntimeMaterial("Blessing_TuongThanh_Mat", defenseColor, defenseColor * 1.3f);

        BlessingBarrierZone zone = barrier.AddComponent<BlessingBarrierZone>();
        zone.Configure(2.6f + stack * 0.35f, 2.3f + stack * 0.25f, 0.25f + stack * 0.04f, defenseColor);
    }

    private void CreateDecoy(Vector3 position, int stack)
    {
        GameObject decoy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        decoy.name = "Blessing_BongChienTruong_Decoy";
        decoy.transform.position = position + Vector3.up * 0.95f;
        decoy.transform.localScale = new Vector3(0.75f, 1.15f, 0.75f);

        Collider collider = decoy.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);

        Renderer renderer = decoy.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = CreateRuntimeMaterial("Blessing_Decoy_Mat", mobilityColor, mobilityColor * 1.5f);

        BlessingBarrierZone zone = decoy.AddComponent<BlessingBarrierZone>();
        zone.Configure(2.2f + stack * 0.35f, 1.6f + stack * 0.28f, 0.18f + stack * 0.04f, mobilityColor);
    }

    private void SpawnPulse(Vector3 position, Color color, float size, float lifetime, string objectName)
    {
        GameObject pulse = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pulse.name = objectName;
        pulse.transform.position = position;
        pulse.transform.localScale = new Vector3(size, 0.08f, size);

        Collider collider = pulse.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);

        Renderer renderer = pulse.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = CreateRuntimeMaterial(objectName + "_Mat", color, color * 1.8f);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        Destroy(pulse, Mathf.Max(0.05f, lifetime));
    }

    private void SpawnLine(Vector3 start, Vector3 end, Color color, string objectName)
    {
        Vector3 delta = end - start;
        if (delta.sqrMagnitude <= 0.001f)
            return;

        GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
        line.name = objectName;
        line.transform.position = start + delta * 0.5f;
        line.transform.rotation = Quaternion.LookRotation(delta.normalized);
        line.transform.localScale = new Vector3(0.07f, 0.07f, delta.magnitude);

        Collider collider = line.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);

        Renderer renderer = line.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = CreateRuntimeMaterial(objectName + "_Mat", color, color * 2f);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        Destroy(line, 0.18f);
    }

    private static Material CreateRuntimeMaterial(string name, Color color, Color emission)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = new Material(shader)
        {
            name = name,
            color = color
        };

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);

        if (material.HasProperty("_EmissionColor"))
            material.SetColor("_EmissionColor", emission);

        material.EnableKeyword("_EMISSION");
        return material;
    }

    private Color GetHeroColor(HeroType hero)
    {
        switch (hero)
        {
            case HeroType.AnDuongVuong: return defenseColor;
            case HeroType.TrungTrac: return leadershipColor;
            case HeroType.TrungNhi: return mobilityColor;
            case HeroType.QuangTrung: return offenseColor;
            default: return Color.white;
        }
    }
}
