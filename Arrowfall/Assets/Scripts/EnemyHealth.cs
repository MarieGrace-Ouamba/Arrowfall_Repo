

using UnityEngine;
using UnityEngine.AI;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;

    [Header("Sound")]
    public AudioClip[] hitSounds;           // one or more armor clangs, a random one plays per hit
    [Range(0f, 1f)]
    public float hitVolume = 1f;
    public float soundHeight = 1.3f;        // plays from the guard's chest, not its feet

    [Header("Death")]
    public string deathState = "Death";     // name of the death state in the Animator
    public bool removeBody = false;         // tick to make bodies disappear after a while
    public float removeDelay = 10f;         // seconds before the body is removed, if removeBody is ticked

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
        PlayHitSound();
        Debug.Log(name + " took " + amount + " damage, " + currentHealth + " left");

        if (currentHealth == 0)
        {
            Die();
        }
    }

    // Plays at the guard's position, so it still finishes even if the guard is removed on death.
    void PlayHitSound()
    {
        if (hitSounds == null || hitSounds.Length == 0)
        {
            return;
        }

        AudioClip clip = hitSounds[Random.Range(0, hitSounds.Length)];
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, transform.position + Vector3.up * soundHeight, hitVolume);
        }
    }

    public bool IsDead()
    {
        return isDead;
    }

    // Stops the guard, plays the death animation, and leaves the body where it fell.
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
        if (agent != null)
        {
            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
            }
            agent.enabled = false;
        }

        // The body should not block the player or catch more arrows.
        Collider body = GetComponent<Collider>();
        if (body != null)
        {
            body.enabled = false;
        }

        Animator animator = GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.CrossFadeInFixedTime("Base Layer." + deathState, 0.1f, 0)
        }

        if (removeBody)
        {
            Destroy(gameObject, removeDelay);
        }
    }
}