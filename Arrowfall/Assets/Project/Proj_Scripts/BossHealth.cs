// BossHealth.cs
// Boss version of EnemyHealth (original by Rayan Alduhaiman, IT 485/585 - ArrowFall).
// Arrow.cs delivers damage by calling TakeDamage on what it hits.
// Adds a GetHit flinch (on its own Animator layer), a death event, and health getters
// for the boss health bar.
// Put this on the same root object as the boss's Capsule Collider.

using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public class BossHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 400;

    [Header("Hit reaction (the Hit layer in BossAnimator)")]
    public string hitState = "GetHit";
    public int hitLayer = 1;               // 0 is the Base Layer, 1 is the Hit layer
    public float hitFadeTime = 0.05f;
    public float hitCooldown = 2f;         // so he does not flinch at every arrow

    [Header("Sound")]
    public AudioClip[] hitSounds;          // a random one plays per hit
    [Range(0f, 1f)]
    public float hitVolume = 1f;
    public float soundHeight = 2f;         // plays from the boss's chest, not his feet

    [Header("Death")]
    public string deathState = "Death";    // name of the death state in the Base Layer
    public UnityEvent onDeath;             // hook things up in the Inspector, like opening the prison

    private int currentHealth;
    private bool isDead = false;
    private float nextHitTime = 0f;
    private Animator animator;

    void Start()
    {
        currentHealth = maxHealth;
        animator = GetComponentInChildren<Animator>();
    }

    // Called by Arrow.cs when an arrow hits, or by anything else that hurts the boss.
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
        else
        {
            PlayHitReaction();
        }
    }

    void PlayHitReaction()
    {
        if (animator == null || Time.time < nextHitTime)
        {
            return;
        }

        // Skip quietly if the controller has no Hit layer.
        if (animator.layerCount <= hitLayer)
        {
            return;
        }

        nextHitTime = Time.time + hitCooldown;
        animator.CrossFadeInFixedTime(hitState, hitFadeTime, hitLayer);
    }

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

    // For the boss health bar.
    public int GetHealth()
    {
        return currentHealth;
    }

    public int GetMaxHealth()
    {
        return maxHealth;
    }

    // Stops the boss, plays the death animation, and leaves the body where it fell.
    void Die()
    {
        isDead = true;
        Debug.Log(name + " died");

        BossChase chase = GetComponent<BossChase>();
        if (chase != null)
        {
            chase.enabled = false;
        }

        BossAttack attack = GetComponent<BossAttack>();
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

        if (animator != null)
        {
            // Turn the flinch layer off so it cannot override the death animation.
            if (animator.layerCount > hitLayer)
            {
                animator.SetLayerWeight(hitLayer, 0f);
            }

            animator.CrossFadeInFixedTime("Base Layer." + deathState, 0.1f, 0);
        }

        onDeath.Invoke();
    }
}