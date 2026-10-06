// BowShoot.cs
// Rayan Alduhaiman
// IT 485/585 - ArrowFall
// Hold left mouse to draw the bow, release to fire.
// The bow swings across to the right quickly, then pulls back over the rest of the draw.
// A longer draw means a faster, harder-hitting arrow.

using UnityEngine;

public class BowShoot : MonoBehaviour
{
    [Header("Bow positions")]
    public Transform bowTransform;
    public Vector3 carryPos = new Vector3(-0.35f, -0.35f, 0.6f);
    public Vector3 drawPos = new Vector3(0.25f, -0.25f, 0.55f);
    public Vector3 carryRot = new Vector3(15f, 90f, 0f);
    public Vector3 drawRot = new Vector3(0f, 90f, 0f);
    public float pullDistance = 0.15f;      // how far the bow comes back during the draw
    public float swingPortion = 0.25f;      // fraction of the draw spent swinging across

    [Header("Bob")]
    public float walkBobSpeed = 8f;        // how fast the bob cycles when walking
    public float walkBobAmount = 0.015f;   // how far the bow moves when walking
    public float sprintBobSpeed = 13f;
    public float sprintBobAmount = 0.03f;
    public float bobSmoothing = 6f;        // how quickly bob eases in and out
    
    private float bobTimer = 0f;
    private Vector3 bobOffset = Vector3.zero;
    private CharacterController playerController;

    [Header("Draw")]
    public float maxDrawTime = 1.5f;        // holding longer than this adds nothing

    [Header("Arrow speed")]
    public float minSpeed = 12f;            // a tap on the mouse
    public float maxSpeed = 45f;            // a full draw

    [Header("Damage")]
    public int minDamage = 10;
    public int maxDamage = 50;

    [Header("References")]
    public GameObject arrowPrefab;
    public Transform firePoint;             // empty object on the bow where arrows leave
    public float spawnDistance = 0.8f;      // fallback if no fire point is assigned

    private float drawTime = 0f;
    private bool isDrawing = false;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            isDrawing = true;
            drawTime = 0f;
        }

        if (isDrawing && Input.GetMouseButton(0))
        {
            drawTime += Time.deltaTime;
            if (drawTime > maxDrawTime)
            {
                drawTime = maxDrawTime;
            }
        }

        if (isDrawing && Input.GetMouseButtonUp(0))
        {
            Fire();
            isDrawing = false;
            drawTime = 0f;
        }

        UpdateBob();
        MoveBow();
    }

    // Two phases. The bow swings from the carry pose to the draw pose early,
    // then slides back toward the player for the rest of the hold.
    void MoveBow()
    {
        
        if (bowTransform == null)
        {
            return;
        }

        float t = drawTime / maxDrawTime;

        float swing = Mathf.Clamp01(t / swingPortion);
        float pull = Mathf.Clamp01((t - swingPortion) / (1f - swingPortion));

        // Ease the swing so it does not stop abruptly.
        swing = Mathf.SmoothStep(0f, 1f, swing);

        Vector3 pos = Vector3.Lerp(carryPos, drawPos, swing);
        pos += Vector3.back * pullDistance * pull;

        bowTransform.localPosition = pos + bobOffset;
        bowTransform.localRotation = Quaternion.Euler(Vector3.Lerp(carryRot, drawRot, swing));
    }

    void Fire()
    {
        if (arrowPrefab == null)
        {
            Debug.LogWarning("BowShoot has no arrow prefab assigned.");
            return;
        }

        // 0 at a tap, 1 at a full draw.
        float charge = drawTime / maxDrawTime;

        float speed = Mathf.Lerp(minSpeed, maxSpeed, charge);
        int damage = Mathf.RoundToInt(Mathf.Lerp(minDamage, maxDamage, charge));

        // Leave from the bow if we have a fire point, but always fly where the camera looks.
        Vector3 spawnPos = (firePoint != null)
            ? firePoint.position
            : transform.position + transform.forward * spawnDistance;

        GameObject arrow = Instantiate(arrowPrefab, spawnPos, transform.rotation);

        Arrow arrowScript = arrow.GetComponent<Arrow>();
        if (arrowScript != null)
        {
            arrowScript.damage = damage;
        }

        Rigidbody rb = arrow.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = transform.forward * speed;
        }

        Debug.Log("Draw " + drawTime.ToString("F2") + "s, speed " + speed + ", damage " + damage);
    }

    // Lets other scripts read the draw for a UI bar later. 0 to 1.
    public float GetCharge()
    {
        return drawTime / maxDrawTime;
    }
    void Awake()
    {
        
        // The controller lives on the player, which is this camera's parent.
        playerController = GetComponentInParent<CharacterController>();
        Debug.Log("Bow found controller: " + (playerController != null));
    }
    // Figures out a small up-and-side offset based on how fast the player is moving.
    // Standing still lets it settle back to zero.
    void UpdateBob()
    {
        // Read input directly. CharacterController.velocity is unreliable here because
        // PlayerMovement calls Move() twice per frame, and the gravity call overwrites it.
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        float speed = new Vector2(x, z).magnitude;

        Vector3 target = Vector3.zero;

        if (speed > 0.1f)
        {
            bool sprinting = Input.GetKey(KeyCode.LeftControl);
            float bobSpeed = sprinting ? sprintBobSpeed : walkBobSpeed;
            float bobAmount = sprinting ? sprintBobAmount : walkBobAmount;

            bobTimer += Time.deltaTime * bobSpeed;

            // Vertical moves at twice the rate of horizontal, which is what makes
            // it read as footsteps rather than a circle.
            target.x = Mathf.Cos(bobTimer) * bobAmount;
            target.y = Mathf.Sin(bobTimer * 2f) * bobAmount;
        }
        else
        {
            bobTimer = 0f;
        }

        bobOffset = Vector3.Lerp(bobOffset, target, Time.deltaTime * bobSmoothing);
    }

}