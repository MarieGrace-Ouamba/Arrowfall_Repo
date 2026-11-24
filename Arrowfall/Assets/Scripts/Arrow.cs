using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Arrow : MonoBehaviour
{
    public int damage = 10;
    public float lifetime = 10f;

    private Rigidbody rb;
    private bool hasHit;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Improve collision detection for fast arrows.
        rb.collisionDetectionMode =
            CollisionDetectionMode.ContinuousDynamic;

        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        if (!hasHit && rb.linearVelocity.sqrMagnitude > 0.1f)
        {
            transform.rotation =
                Quaternion.LookRotation(rb.linearVelocity);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasHit)
            return;

        hasHit = true;

        // Find Slender even when the arrow hits a child collider.
        SlenderBoss slender =
            collision.collider.GetComponentInParent<SlenderBoss>();

        if (slender != null)
        {
            // SlenderBoss applies 20 damage per registered arrow.
            slender.TakeDamage(damage);
            Debug.Log("Arrow hit Slender.", slender);
        }
        else
        {
            // Preserve the existing damage behaviour for other enemies.
            collision.gameObject.SendMessage(
                "TakeDamage",
                damage,
                SendMessageOptions.DontRequireReceiver);
        }

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        // Prevent a stuck arrow from blocking later arrows.
        foreach (Collider arrowCollider in
                 GetComponentsInChildren<Collider>())
        {
            arrowCollider.enabled = false;
        }

        transform.SetParent(collision.transform, true);

        Destroy(gameObject, 5f);
    }
}