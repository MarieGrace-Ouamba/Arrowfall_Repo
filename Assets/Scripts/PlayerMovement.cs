// PlayerMovement.cs
// Rayan Alduhaiman
// IT 485/585 - ArrowFall
// Moves the player with WASD, sprint and jump using a CharacterController.

using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Speed")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 9f;

    [Header("Jump and gravity")]
    public float jumpHeight = 1.2f;
    public float gravity = -20f;

    [Header("Ground check")]
    public float groundCheckDistance = 0.3f;
    public LayerMask groundMask = ~0;      // everything by default

    private CharacterController controller;
    private Vector3 velocity;
    private bool grounded;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        CheckGround();

        // Press the player into the ground so contact stays reliable.
        if (grounded && velocity.y < 0f)
        {
            velocity.y = -2f;
        }

        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        // Move relative to the direction the player is facing.
        Vector3 move = transform.right * x + transform.forward * z;
        if (move.sqrMagnitude > 1f)
        {
            move.Normalize();   // stops diagonal movement being faster
        }

        float speed = Input.GetKey(KeyCode.LeftControl) ? sprintSpeed : walkSpeed;
        controller.Move(move * speed * Time.deltaTime);

        if (Input.GetKeyDown(KeyCode.Space) && grounded)
        {
            // The speed needed to rise exactly jumpHeight metres.
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            grounded = false;
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    // A sphere cast just under the feet is more forgiving than isGrounded,
    // which can flicker off while the controller is being pushed down by gravity.
    void CheckGround()
    {
        Vector3 origin = transform.position + controller.center;
        float rayLength = (controller.height * 0.5f) - controller.radius + groundCheckDistance;

        grounded = Physics.SphereCast(
            origin,
            controller.radius * 0.9f,
            Vector3.down,
            out RaycastHit hit,
            rayLength,
            groundMask,
            QueryTriggerInteraction.Ignore);
    }
}