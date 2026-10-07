// EnemyHealth.cs
// Rayan Alduhaiman
// IT 485/585 - ArrowFall
// Gives a guard health. Arrow.cs delivers the damage by calling TakeDamage on what it hits.
// With 100 health and a full-draw arrow doing 50, two full draws kill a guard.

using UnityEngine;
using UnityEngine.AI;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;

    [Header("Death")]
    public float removeDelay = 0f;          // seconds before the guard disappears after dying

    private int currentHealth;
    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;
    }

    // Called by Arrow.cs when an arrow hits, or by anything else that hurts a guard.
    public void TakeDamage(int amount)
    {
        if (isDead)
        {
            return;
        }

        currentHealth = Mathf.Max(currentHealth - amount, 0);
        Debug.Log(name + " took " + amount + " damage, " + currentHealth + " left");

        if (currentHealth == 0)
        {
            Die();
        }
    }

    public bool IsDead()
    {
        return isDead;
    }

    // Stops the guard from chasing or attacking, then removes it.
    void Die()
    {
        isDead = true;
        Debug.Log(name + " died");

        EnemyChase chase = GetComponent<EnemyChase>();
        if (chase != null)
        {
            chase.enabled = false;
        }

        EnemyAttack attack = GetComponent<EnemyAttack>();
        if (attack != null)
        {
            attack.enabled = false;
        }

        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }

        Destroy(gameObject, removeDelay);
    }
}