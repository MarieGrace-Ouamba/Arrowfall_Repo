
using UnityEngine;

public class MagicTrap : MonoBehaviour
{
    public int damage = 10;

    private bool activated = false;


    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Something entered the magic trap: " + other.name);

        if (other.CompareTag("Player"))
        {
            Debug.Log("PLAYER DETECTED!");

            activated = true;

            PlayerHealth health = other.GetComponent<PlayerHealth>();

            if (health != null)
            {
                health.TakeDamage(damage);
                Debug.Log("Damage applied!");
            }
            else
            {
                Debug.Log("PlayerHealth was not found!");
            }
        }
    }
}