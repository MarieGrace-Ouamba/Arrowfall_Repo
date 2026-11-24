//Marie-Grace Ouamba
using UnityEngine;

public class HealingPotion : MonoBehaviour
{
    [Range(0f, 1f)]
    public float healPercent = 0.2f;        // 20% of max health
    public bool onlyIfHurt = true;          // leave the potion alone when health is already full

    void OnTriggerEnter(Collider other)
    {
        PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
        if (health == null) return;

        if (onlyIfHurt && health.GetHealth() >= health.maxHealth) return;

        int amount = Mathf.RoundToInt(health.maxHealth * healPercent);
        health.Heal(amount);

        Destroy(gameObject);
    }
}