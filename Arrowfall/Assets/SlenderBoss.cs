using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class SlenderBoss : MonoBehaviour
{
    [Header("References")]
    public PlayerHealth player;
    public Transform arenaCenter;
    public BossExitDoor exitDoor;

    [Header("Health")]
    public int maxHealth = 100;

    private const int DamagePerHit = 20;

    [Header("Flight")]
    public float flightHeight = 8f;
    public float circleRadius = 7f;
    public float flightSpeed = 8f;
    public float circleDegreesPerSecond = 55f;
    public float circleDuration = 6f;
    public float turnSpeed = 180f;

    [Header("Dive")]
    public float warningDuration = 2f;
    public float diveSpeed = 14f;
    public float hitDistance = 1.6f;
    public float vulnerableDuration = 6f;

    private enum State
    {
        Waiting,
        Rising,
        Circling,
        Warning,
        Diving,
        Vulnerable,
        Dead
    }

    private State state = State.Waiting;

    private Rigidbody body;
    private CapsuleCollider bossCollider;

    private Renderer[] bossRenderers;
    private bool[] originalRendererStates;

    private Collider[] bossColliders;
    private bool[] originalColliderStates;

    private Vector3 center;
    private Vector3 diveTarget;

    private float orbitAngle;
    private float timer;
    private int health;
    private bool hitPlayer;

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        bossCollider = GetComponent<CapsuleCollider>();

        body.useGravity = false;
        body.isKinematic = true;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode =
            CollisionDetectionMode.ContinuousSpeculative;

        bossCollider.isTrigger = false;

        Animator animator = GetComponent<Animator>();

        if (animator != null)
            animator.applyRootMotion = false;

        // 100 health and 20 damage per arrow = five hits.
        maxHealth = 100;
        health = maxHealth;

        center = arenaCenter != null
            ? arenaCenter.position
            : transform.position;

        // Hide Slender until the stair trigger activates him.
        bossRenderers = GetComponentsInChildren<Renderer>(true);
        originalRendererStates = new bool[bossRenderers.Length];

        for (int i = 0; i < bossRenderers.Length; i++)
        {
            originalRendererStates[i] = bossRenderers[i].enabled;
            bossRenderers[i].enabled = false;
        }

        // Disable collision while Slender is hidden.
        bossColliders = GetComponentsInChildren<Collider>(true);
        originalColliderStates = new bool[bossColliders.Length];

        for (int i = 0; i < bossColliders.Length; i++)
        {
            originalColliderStates[i] =
                bossColliders[i] == bossCollider ||
                bossColliders[i].enabled;

            bossColliders[i].enabled = false;
        }
    }

    // Called by SlenderBossTrigger at the top of the stairs.
    public void ActivateBoss()
    {
        if (state != State.Waiting)
            return;

        for (int i = 0; i < bossRenderers.Length; i++)
        {
            if (bossRenderers[i] != null)
                bossRenderers[i].enabled = originalRendererStates[i];
        }

        for (int i = 0; i < bossColliders.Length; i++)
        {
            if (bossColliders[i] != null)
                bossColliders[i].enabled = originalColliderStates[i];
        }

        state = State.Rising;
    }

    void FixedUpdate()
    {
        if (state == State.Dead ||
            player == null ||
            player.GetHealth() <= 0)
        {
            return;
        }

        float dt = Time.fixedDeltaTime;

        switch (state)
        {
            case State.Waiting:
                // Only the stair trigger starts the fight.
                break;

            case State.Rising:
                if (MoveTo(OrbitPosition(), flightSpeed, dt))
                {
                    timer = circleDuration;
                    state = State.Circling;
                }
                break;

            case State.Circling:
                orbitAngle += circleDegreesPerSecond * dt;
                MoveTo(OrbitPosition(), flightSpeed, dt);

                timer -= dt;

                if (timer <= 0f)
                {
                    timer = warningDuration;
                    hitPlayer = false;
                    state = State.Warning;
                }
                break;

            case State.Warning:
                {
                    // Aim at the player's actual position and height.
                    Vector3 playerAimPoint =
                        player.transform.position + Vector3.up;

                    Vector3 bossCenterOffset =
                        transform.TransformVector(bossCollider.center);

                    diveTarget = playerAimPoint - bossCenterOffset;

                    Face(diveTarget - body.position, dt);

                    timer -= dt;

                    if (timer <= 0f)
                    {
                        // Lock the target when the dive starts,
                        // allowing the player to dodge.
                        state = State.Diving;
                    }
                    break;
                }

            case State.Diving:
                {
                    Vector3 previousPosition = body.position;

                    Vector3 nextPosition = Vector3.MoveTowards(
                        previousPosition,
                        diveTarget,
                        diveSpeed * dt);

                    Vector3 bossCenterOffset =
                        transform.TransformVector(bossCollider.center);

                    Vector3 playerCenter =
                        player.transform.position + Vector3.up;

                    // Detect hits along the full dive movement.
                    if (!hitPlayer &&
                        DistanceToSegment(
                            playerCenter,
                            previousPosition + bossCenterOffset,
                            nextPosition + bossCenterOffset) <= hitDistance)
                    {
                        hitPlayer = true;
                        KillPlayer();
                    }

                    if (MoveTo(diveTarget, diveSpeed, dt))
                    {
                        timer = vulnerableDuration;
                        state = State.Vulnerable;
                    }
                    break;
                }

            case State.Vulnerable:
                Face(player.transform.position - body.position, dt);

                timer -= dt;

                if (timer <= 0f)
                    state = State.Rising;
                break;
        }

        // Also detect contact with CharacterController players.
        if (state != State.Waiting && state != State.Dead)
            CheckPlayerContact();
    }

    Vector3 OrbitPosition()
    {
        float radians = orbitAngle * Mathf.Deg2Rad;

        return center + new Vector3(
            Mathf.Cos(radians) * circleRadius,
            flightHeight,
            Mathf.Sin(radians) * circleRadius);
    }

    bool MoveTo(Vector3 target, float speed, float dt)
    {
        Vector3 next = Vector3.MoveTowards(
            body.position,
            target,
            speed * dt);

        Face(target - body.position, dt);
        body.MovePosition(next);

        return (next - target).sqrMagnitude < 0.01f;
    }

    void Face(Vector3 direction, float dt)
    {
        // Keep the humanoid upright while flying.
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        body.MoveRotation(Quaternion.RotateTowards(
            body.rotation,
            targetRotation,
            turnSpeed * dt));
    }

    float DistanceToSegment(
        Vector3 point,
        Vector3 start,
        Vector3 end)
    {
        Vector3 segment = end - start;

        if (segment.sqrMagnitude < 0.0001f)
            return Vector3.Distance(point, start);

        float t = Mathf.Clamp01(
            Vector3.Dot(point - start, segment) /
            segment.sqrMagnitude);

        return Vector3.Distance(point, start + segment * t);
    }

    void CheckPlayerContact()
    {
        if (!bossCollider.enabled || player.GetHealth() <= 0)
            return;

        Vector3 scale = transform.lossyScale;

        scale = new Vector3(
            Mathf.Abs(scale.x),
            Mathf.Abs(scale.y),
            Mathf.Abs(scale.z));

        Vector3 axis;
        float axisScale;
        float radiusScale;

        switch (bossCollider.direction)
        {
            case 0:
                axis = transform.right;
                axisScale = scale.x;
                radiusScale = Mathf.Max(scale.y, scale.z);
                break;

            case 2:
                axis = transform.forward;
                axisScale = scale.z;
                radiusScale = Mathf.Max(scale.x, scale.y);
                break;

            default:
                axis = transform.up;
                axisScale = scale.y;
                radiusScale = Mathf.Max(scale.x, scale.z);
                break;
        }

        Vector3 capsuleCenter =
            transform.TransformPoint(bossCollider.center);

        float radius = bossCollider.radius * radiusScale;

        float height = Mathf.Max(
            bossCollider.height * axisScale,
            radius * 2f);

        float halfSegment = height * 0.5f - radius;

        Vector3 top = capsuleCenter + axis * halfSegment;
        Vector3 bottom = capsuleCenter - axis * halfSegment;

        Collider[] contacts = Physics.OverlapCapsule(
            top,
            bottom,
            radius + 0.02f,
            Physics.AllLayers,
            QueryTriggerInteraction.Ignore);

        foreach (Collider contact in contacts)
        {
            PlayerHealth touchedPlayer =
                contact.GetComponentInParent<PlayerHealth>();

            if (touchedPlayer == player)
            {
                KillPlayer();
                return;
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        KillPlayerOnContact(collision);
    }

    void OnCollisionStay(Collision collision)
    {
        KillPlayerOnContact(collision);
    }

    void KillPlayerOnContact(Collision collision)
    {
        if (state == State.Waiting || state == State.Dead)
            return;

        PlayerHealth touchedPlayer =
            collision.gameObject.GetComponentInParent<PlayerHealth>();

        if (touchedPlayer != null && touchedPlayer == player)
            KillPlayer();
    }

    void KillPlayer()
    {
        if (player == null || player.GetHealth() <= 0)
            return;

        player.TakeDamage(player.GetHealth());
    }

    // Called once per arrow hit by Arrow.cs.
    public void TakeDamage(int amount)
    {
        if (state == State.Waiting ||
            state == State.Dead ||
            amount <= 0)
        {
            return;
        }

        // Take damage during every active phase.
        health = Mathf.Max(health - DamagePerHit, 0);

        if (health == 0)
            Die();
    }

    void Die()
    {
        if (state == State.Dead)
            return;

        state = State.Dead;

        // Collect Key 3 for doors using DoorInteract.
        KeyPickup.hasKey3 = true;

        // Also preserve the existing exit-door unlock.
        if (exitDoor != null)
            exitDoor.Unlock();

        Debug.Log("Slender defeated! Key 3 collected.");

        // Hide the boss and any arrows attached to him.
        foreach (Renderer bossRenderer in
                 GetComponentsInChildren<Renderer>(true))
        {
            bossRenderer.enabled = false;
        }

        foreach (Collider bossCollision in
                 GetComponentsInChildren<Collider>(true))
        {
            bossCollision.enabled = false;
        }

        // Keep the script active briefly to show the reward message.
        Destroy(gameObject, 5f);
    }

    void OnGUI()
    {
        if (state == State.Waiting)
            return;

        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.fontSize = 22;
        style.alignment = TextAnchor.MiddleCenter;

        if (state == State.Dead)
        {
            GUI.Box(
                new Rect(Screen.width / 2f - 280f, 75f, 560f, 60f),
                "Slender defeated! Key 3 collected.\nOpen Door 3 to continue.",
                style);

            return;
        }

        if (player == null || player.GetHealth() <= 0)
            return;

        GUI.Box(
            new Rect(Screen.width / 2f - 180f, 20f, 360f, 45f),
            "SLENDER — " + health + " / " + maxHealth,
            style);

        string message = "";

        if (state == State.Warning)
            message = "MOVE! Slender is about to dive!";
        else if (state == State.Vulnerable)
            message = "SHOOT! Slender is recovering!";

        if (message != "")
        {
            GUI.Box(
                new Rect(Screen.width / 2f - 250f, 75f, 500f, 45f),
                message,
                style);
        }
    }
}