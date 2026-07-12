using System.Collections;
using UnityEngine;

public class MinionHealth3D : MonoBehaviour
{
    public int maxHP = 50;
    public int currentHP;
    public bool destroyOnDeath = true;
    public float deathDelay = 0.05f;

    public bool IsDead { get; private set; }

    private void Start()
    {
        if (currentHP <= 0)
            currentHP = maxHP;
    }

    public void TakeDamage(int damage)
    {
        if (IsDead)
            return;

        currentHP -= damage;
        currentHP = Mathf.Max(0, currentHP);

        Debug.Log(gameObject.name + " HP: " + currentHP);

        if (currentHP <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (IsDead)
            return;

        IsDead = true;
        Debug.Log(gameObject.name + " bị tiêu diệt.");
        MinionDeathNotifier deathNotifier = GetComponent<MinionDeathNotifier>();
        if (deathNotifier != null)
            deathNotifier.MarkKilled();

        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider enemyCollider in colliders)
            enemyCollider.enabled = false;

        MinionChase3D chase = GetComponent<MinionChase3D>();
        if (chase != null)
        {
            float finalAttackDuration = chase.PlayFinalAttackBeforeDeath();
            if (finalAttackDuration > 0f)
                yield return new WaitForSeconds(finalAttackDuration);

            chase.FinishFinalAttack();
            chase.PlayDeathAnimation();
            chase.enabled = false;
        }

        if (destroyOnDeath)
            Destroy(gameObject, deathDelay);
    }
}
