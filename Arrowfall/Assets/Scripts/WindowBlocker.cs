// WindowBlocker.cs
// Rayan Alduhaiman
// IT 485/585 - ArrowFall
// Stops the player climbing out of a shed window, without putting a wall in it.
// There is no collider here, so arrows fly through and the bow's aim is not blocked.
// The object sits in the middle of the window opening with its blue arrow (forward)
// pointing outside. If the player pushes into the opening, they are slid back inside.
// The same script guards the castle gate, whose bars have gaps the player could
// squeeze through; there it switches off when the gate opens.

using UnityEngine;

public class WindowBlocker : MonoBehaviour
{
    public Transform player;            // found automatically if left empty
    public float halfWidth = 0.75f;     // half the width of the window opening
    public float keepBack = 0.6f;       // how far inside the window the player must stay
    public GateController gate;         // optional: stop blocking once this gate has opened

    private CharacterController controller;

    void Start()
    {
        if (player == null)
        {
            PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
            if (pm != null)
            {
                player = pm.transform;
            }
        }

        controller = player.GetComponent<CharacterController>();
    }

    void Update()
    {
        // Used on the castle gate: once the puzzle opens it, let the player through.
        if (gate != null && gate.IsOpen())
        {
            return;
        }

        Vector3 offset = player.position - transform.position;

        // How far the player is to the side of the window, and how far through it.
        // depth is negative inside the shed and positive outside.
        float side = Vector3.Dot(offset, transform.right);
        float depth = Vector3.Dot(offset, transform.forward);

        bool inFrontOfWindow = Mathf.Abs(side) < halfWidth;
        bool pushingThrough = depth > -keepBack && depth < 0.3f;

        if (inFrontOfWindow && pushingThrough)
        {
            // Slide the player back to the allowed distance.
            controller.Move(-transform.forward * (depth + keepBack));
        }
    }
}
