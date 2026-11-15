// ShootableKey.cs
// Rayan Alduhaiman
// IT 485/585 - ArrowFall
// A key that hangs from a rafter inside the shed, out of reach.
// It starts kinematic so it stays on its hook. Arrow.cs calls TakeDamage on
// whatever it hits, so when an arrow strikes the key it drops: the Rigidbody
// wakes up, gravity turns on, and it falls to the floor for the player to pick up.
// This is the safe bow tutorial at the start of Level 1.

using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ShootableKey : MonoBehaviour
{
    [Header("Drop")]
    public float dropImpulse = 0.5f;        // small downward nudge so it leaves the hook cleanly
    public GameObject rope;                 // the rope holding the key; hidden when the key is shot down

    [Header("Sound")]
    public AudioClip shotSound;             // optional clink when the key is knocked down
    [Range(0f, 1f)]
    public float shotVolume = 1f;

    private Rigidbody rb;
    private bool isShot = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // Hang in place until it is hit.
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    // Arrow.cs sends this to whatever it hits (same hook the number targets use).
    public void TakeDamage(int amount)
    {
        Drop();
    }

    // Also drop on a direct collision, in case something bumps it.
    public void Drop()
    {
        if (isShot)
        {
            return;
        }
        isShot = true;

        // Leave the rafter but keep its current world position.
        transform.SetParent(null, true);

        // The rope is "cut" when the key is shot down.
        if (rope != null)
        {
            rope.SetActive(false);
        }

        // Shrink the tall "aim" collider back to the key itself so the fallen key
        // does not leave an invisible column where the rope used to be.
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
        {
            box.center = Vector3.zero;
            box.size = new Vector3(0.35f, 0.5f, 0.35f);
        }

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.AddForce(Vector3.down * dropImpulse, ForceMode.Impulse);

        if (shotSound != null)
        {
            AudioSource.PlayClipAtPoint(shotSound, transform.position, shotVolume);
        }
    }

    // ShedEscape checks this before it offers the pick-up prompt.
    public bool IsShot()
    {
        return isShot;
    }
}
