// ShootableLantern.cs
// Rayan Alduhaiman
// IT 485/585 - ArrowFall
// A lantern hung on the far shed. Arrow.cs calls TakeDamage on whatever it hits,
// so shooting the lantern "breaks" it: its glow goes out and it tells the
// GuardDistraction controller to lure the guards over. This is the distraction
// that lets the player slip out of the shed.

using UnityEngine;

public class ShootableLantern : MonoBehaviour
{
    [Header("Wiring")]
    public GuardDistraction distraction;    // told when the lantern breaks

    [Header("Break")]
    public Light glow;                       // the lantern light, switched off when broken
    public GameObject brokenHide;            // the glowing part, hidden when broken
    public AudioClip breakSound;
    [Range(0f, 1f)]
    public float volume = 1f;

    private bool broken = false;

    // Arrow.cs sends this to whatever it hits (same hook the key and targets use).
    public void TakeDamage(int amount)
    {
        Break();
    }

    public void Break()
    {
        if (broken)
        {
            return;
        }
        broken = true;

        if (glow != null)
        {
            glow.enabled = false;
        }

        if (brokenHide != null)
        {
            brokenHide.SetActive(false);
        }

        if (breakSound != null)
        {
            AudioSource.PlayClipAtPoint(breakSound, transform.position, volume);
        }

        if (distraction != null)
        {
            distraction.OnLanternBroken(transform);
        }
    }

    public bool IsBroken()
    {
        return broken;
    }
}
