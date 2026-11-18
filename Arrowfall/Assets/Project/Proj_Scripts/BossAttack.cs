// BossAttack.cs
// Boss copy of EnemyAttack (original by Rayan Alduhaiman, IT 485/585 - ArrowFall).
// While the boss is within attack range, he hits the player every few seconds.
// Each hit takes a share of the player's max health (40% for the boss, so three hits kill).
// Uses BossChase's attack distance so the damage lines up with the attack animation.

using UnityEngine;

[RequireComponent(typeof(BossChase))]
public class BossAttack : MonoBehaviour
{
    [Header("Damage")]
    [Range(0f, 1f)]
    public float damagePercent = 0.4f;      // 0.4 = 40% of the player's max health

    [Header("Timing")]
    public float windUp = 0.5f;             // delay before the first hit after reaching the player
    public float attackCooldown = 1.5f;     // time between hits (match it to the attack clip length)

    private BossChase chase;
    private PlayerHealth playerHealth;
    private float timer = 0f;
    private bool inRange = false;

    void Start()
    {
        chase = GetComponent<BossChase>();
        FindPlayerHealth();
    }

    // Uses the player BossChase already knows about, or finds the object tagged Player.
    void FindPlayerHealth()
    {
        if (chase != null && chase.player != null)
        {
            playerHealth = chase.player.GetComponent<PlayerHealth>();
        }

        if (playerHealth == null)
        {
            GameObject playerObject = GameObject.FindWithTag("Player");
            if (playerObject != null)
            {
                playerHealth = playerObject.GetComponent<PlayerHealth>();
            }
        }
    }

    void Update()
    {
        if (playerHealth == null)
        {
            FindPlayerHealth();
            return;
        }

        Vector3 toPlayer = playerHealth.transform.position - transform.position;
        toPlayer.y = 0f;

        if (toPlayer.magnitude > chase.attackDistance)
        {
            inRange = false;
            return;
        }

        // Just reached the player, so wait for the swing before the first hit.
        if (!inRange)
        {
            inRange = true;
            timer = windUp;
        }

        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            int damage = Mathf.RoundToInt(playerHealth.maxHealth * damagePercent);
            playerHealth.TakeDamage(damage);
            timer = attackCooldown;
        }
    }
}