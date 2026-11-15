// GuardDistraction.cs
// Rayan Alduhaiman
// IT 485/585 - ArrowFall
// Controls the two guards outside the player's shed for the escape.
// While the lantern is intact the guards watch the shed (their own EnemyChase cone),
// so opening the door in front of them gets the player caught. When the player shoots
// the lantern on the far shed, this points each guard's EnemyChase target at the lantern
// and widens its view, so the guards walk off to investigate - reusing EnemyChase's own
// movement and animation. It does not modify the shared EnemyChase script, only its public fields.

using UnityEngine;

public class GuardDistraction : MonoBehaviour
{
    [Header("Guards")]
    public EnemyChase[] guards;             // the two shed guards

    [Header("Lure")]
    public Transform distractTarget;        // where the guards go; defaults to the lantern itself
    public float lureViewDistance = 500f;   // so they keep heading to the lantern
    public float lureViewAngle = 360f;

    private bool distracted = false;

    public bool IsDistracted()
    {
        return distracted;
    }

    // Called by ShootableLantern when the lantern is shot down.
    public void OnLanternBroken(Transform lantern)
    {
        if (distracted)
        {
            return;
        }
        distracted = true;

        Transform target = (distractTarget != null) ? distractTarget : lantern;

        foreach (EnemyChase guard in guards)
        {
            if (guard == null)
            {
                continue;
            }
            // Chase the lantern instead of the player, and make sure they can "see" it.
            guard.player = target;
            guard.viewDistance = lureViewDistance;
            guard.viewAngle = lureViewAngle;
        }
    }
}
