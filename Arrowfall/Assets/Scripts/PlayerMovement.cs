using UnityEngine;
using UnityEngine.UI;

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
    public LayerMask groundMask = ~0;

    [Header("Water")]
    public BoxCollider waterVolume;
    public Transform playerCamera;
    public float waterMoveSpeed = 2f;
    public float sinkSpeed = 1f;

    [Header("Drowning")]
    public float breathDuration = 3f;
    public int drowningDamage = 10;
    public float damageInterval = 1f;

    [Header("Underwater appearance - optional")]
    public Image underwaterOverlay;

    private CharacterController controller;
    private PlayerHealth health;
    private Vector3 velocity;
    private bool grounded;
    private float underwaterTime;
    private float damageTimer;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        health = GetComponent<PlayerHealth>();

        if (playerCamera == null)
        {
            Camera cam = GetComponentInChildren<Camera>();

            if (cam != null)
                playerCamera = cam.transform;
        }

        if (underwaterOverlay != null)
        {
            underwaterOverlay.raycastTarget = false;
            underwaterOverlay.enabled = false;
        }
    }

    void Update()
    {
        CheckGround();

        Vector3 bodyPosition = transform.TransformPoint(controller.center);

        bool inWater = IsInsideWater(bodyPosition);
        bool headUnderwater = playerCamera != null
            && IsInsideWater(playerCamera.position);

        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;

        if (move.sqrMagnitude > 1f)
            move.Normalize();

        float speed = inWater
            ? waterMoveSpeed
            : (Input.GetKey(KeyCode.LeftControl) ? sprintSpeed : walkSpeed);

        controller.Move(move * speed * Time.deltaTime);

        if (inWater)
        {
            // Replace falling speed with a slow, steady sinking speed.
            velocity.y = -Mathf.Abs(sinkSpeed);
        }
        else
        {
            if (grounded && velocity.y < 0f)
                velocity.y = -2f;

            if (Input.GetKeyDown(KeyCode.Space) && grounded)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                grounded = false;
            }

            velocity.y += gravity * Time.deltaTime;
        }

        controller.Move(velocity * Time.deltaTime);

        if (underwaterOverlay != null)
            underwaterOverlay.enabled = headUnderwater;

        UpdateDrowning(headUnderwater);
    }

    bool IsInsideWater(Vector3 position)
    {
        return waterVolume != null
            && waterVolume.enabled
            && waterVolume.gameObject.activeInHierarchy
            && waterVolume.bounds.Contains(position);
    }

    void UpdateDrowning(bool headUnderwater)
    {
        if (!headUnderwater)
        {
            underwaterTime = 0f;
            damageTimer = 0f;
            return;
        }

        float previousTime = underwaterTime;
        underwaterTime += Time.deltaTime;

        // Count only the time after the breath allowance expires.
        float damageTime =
            Mathf.Max(0f, underwaterTime - breathDuration)
            - Mathf.Max(0f, previousTime - breathDuration);

        damageTimer += damageTime;

        float interval = Mathf.Max(0.1f, damageInterval);

        while (damageTimer >= interval)
        {
            damageTimer -= interval;

            if (health != null)
            {
                health.TakeDamage(drowningDamage);

                if (health.GetHealth() <= 0)
                    break;
            }
        }
    }

    void CheckGround()
    {
        Vector3 origin = transform.position + controller.center;
        float rayLength = (controller.height * 0.5f)
            - controller.radius + groundCheckDistance;

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