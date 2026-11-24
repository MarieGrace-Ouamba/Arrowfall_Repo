// NumberTarget.cs
// Rayan Alduhaiman
// IT 485/585 - ArrowFall
// One numbered target on the gate pillars. When an arrow hits it,
// it tells the number puzzle which number was hit and gives a small pulse as feedback.

using TMPro;
using UnityEngine;

public class NumberTarget : MonoBehaviour
{
    [Header("Target")]
    [Range(1, 4)]
    public int number = 1;
    public NumberPuzzle puzzle;             // found automatically if left empty
    public TMP_Text label;                  // optional text on the target, filled in with its number

    [Header("Feedback")]
    public float pulseSize = 0.2f;          // how much the target grows when hit, 0.2 = 20%

    private Vector3 baseScale;
    private float pulse = 0f;
    private float lastHitTime = -10f;

    void Start()
    {
        if (puzzle == null)
        {
            puzzle = FindFirstObjectByType<NumberPuzzle>();
        }

        if (label != null)
        {
            label.text = number.ToString();
        }

        baseScale = transform.localScale;
    }

    void Update()
    {
        // Shrinks back to normal size after a hit.
        if (pulse > 0f)
        {
            pulse = Mathf.Max(0f, pulse - Time.deltaTime * 4f);
            transform.localScale = baseScale * (1f + pulseSize * pulse);
        }
    }

    // An arrow touching the target.
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.GetComponentInParent<Arrow>() != null)
        {
            Hit();
        }
    }

    // Arrow.cs calls TakeDamage on whatever it hits, so targets answer to it too.
    public void TakeDamage(int amount)
    {
        Hit();
    }

    void Hit()
    {
        // The arrow can report the same hit twice (collision and TakeDamage), so ignore repeats.
        if (Time.time - lastHitTime < 0.5f)
        {
            return;
        }

        lastHitTime = Time.time;
        pulse = 1f;

        if (puzzle != null)
        {
            puzzle.TargetHit(this);
        }
    }
}