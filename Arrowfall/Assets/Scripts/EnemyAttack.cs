// EnemyAttack.cs
// Rayan Alduhaiman
// IT 485/585 - ArrowFall
// While the guard is within attack range, it hits the player every few seconds.
// Each hit takes a share of the player's max health (20% by default, so five hits kill).
// Uses EnemyChase's attack distance so the damage lines up with the attack animation.

using UnityEngine;

[RequireComponent(typeof(EnemyChase))]
public class EnemyAttack : MonoBehaviour
{
    [Header("Damage")]
    [Range(0f, 1f)]
    public float damagePercent = 0.2f;      // 0.2 = 20% of the player's max health

    [Header("Timing")]
    public float windUp = 0.5f;             // delay before the first hit after reaching the player
    public float attackCooldown = 1.5f;     // time between hits

    private EnemyChase chase;
    private PlayerHealth playerHealth;
    private float timer = 0f;
    private bool inRange = false;

    void Start()
    {
        chase = GetComponent<EnemyChase>();
        FindPlayerHealth();
    }

    // Uses the player EnemyChase already knows about, or finds the object tagged Player.
    void FindPlayerHealth()
    {
        if (chase.player != null)
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
