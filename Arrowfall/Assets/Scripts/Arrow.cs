// Arrow.cs
// Rayan Alduhaiman
// IT 485/585 - ArrowFall
// Flies under gravity, turns to face the way it is travelling, and damages what it hits.

using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Arrow : MonoBehaviour
{
    public int damage = 10;
    public float lifetime = 10f;   // clean up arrows that fly off the map

    private Rigidbody rb;
    private bool hasHit = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        // While flying, point along the direction of travel so the arrow tips downward
        // as it falls. This is what makes the drop readable to the player.
        if (!hasHit && rb.linearVelocity.sqrMagnitude > 0.1f)
        {
            transform.rotation = Quaternion.LookRotation(rb.linearVelocity);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasHit)
        {
            return;
        }
        hasHit = true;

        // Ask whatever we hit to take damage. Nothing happens if it has no health script,
        // so this works before the enemy scripts exist.
        collision.gameObject.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);

        // Stick in place where it landed.
        rb.isKinematic = true;
        transform.SetParent(collision.transform);

        Destroy(gameObject, 5f);
    }
}