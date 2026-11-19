// ChestLock.cs
// Rayan Alduhaiman
// IT 485/585 - ArrowFall
// The ancient ring lock on the chest in the shed. The bow is inside the chest,
// so the player starts Level 1 without it.
// Press E at the chest to open the lock full screen. Click a ring to turn it.
// The rings are geared together: turning a ring also turns the ring inside it
// one step the other way. When all three glowing runes sit under the arrow at
// the top, the lock opens and the bow appears on the chest. Right click takes it.

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChestLock : MonoBehaviour
{
    [Header("Player")]
    public Transform player;                // found automatically if left empty
    public PlayerLook playerLook;           // switched off while the lock is on screen
    public PlayerMovement playerMovement;   // switched off while the lock is on screen
    public BowShoot bowShoot;               // switched off until the bow is picked up
    public GameObject bowInHand;            // the bow model the player holds
    public GameObject crosshair;            // the aiming dot, hidden until the player has the bow

    [Header("Chest")]
    public GameObject bowOnChest;           // bow inside the chest, shown when the lock opens
    public Transform lid;                   // the lid's hinge, swung open when the lock opens
    public float lidOpenAngle = -80f;       // how far the lid swings
    public float lidSpeed = 90f;            // degrees per second
    public float reach = 2.5f;              // how close the player must be to use the chest

    [Header("Lock screen")]
    public GameObject lockScreen;           // the full screen lock
    public RectTransform[] rings;           // 0 = outer, 1 = middle, 2 = inner
    public RectTransform centre;            // the keyhole piece in the middle
    public Image centreImage;
    public Sprite centreLitSprite;          // shown when the lock opens
    public float turnSpeed = 180f;          // degrees per second the rings turn on screen
    public float closeDelay = 1.5f;         // how long the open lock stays up before closing

    [Header("Puzzle")]
    public int[] ringSteps = { 3, 6, 1 };   // each ring's position, 0 to 7. All 0 = open

    [Header("Prompts")]
    public TMP_Text promptText;
    public string promptOpen = "Press E";
    public string promptTake = "Right click";

    [Header("Sound")]
    public AudioClip turnSound;
    public AudioClip unlockSound;
    public AudioClip pickupSound;
    [Range(0f, 1f)]
    public float volume = 1f;

    // Remembered across level reloads, so dying and restarting keeps the bow.
    // "static" means the value belongs to the script itself, not to one object,
    // which is why it is still there after the scene is loaded again.
    private static bool hasBow = false;

    private bool screenOpen = false;
    private bool solved = false;
    private bool bowTaken = false;
    private float closeTime = 0f;
    private float[] ringAngle = new float[3];   // the angle each ring is drawn at right now

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

        lockScreen.SetActive(false);
        bowOnChest.SetActive(false);

        // Already took the bow before dying: keep it, and leave the chest open and empty.
        if (hasBow)
        {
            solved = true;
            bowTaken = true;
            lid.localRotation = Quaternion.Euler(lidOpenAngle, 0f, 0f);
            promptText.text = "";
            return;
        }

        // The player starts without the bow.
        bowInHand.SetActive(false);
        bowShoot.enabled = false;
        crosshair.SetActive(false);

        // Draw the rings at their starting positions.
        for (int i = 0; i < 3; i++)
        {
            ringAngle[i] = ringSteps[i] * -45f;
            rings[i].localRotation = Quaternion.Euler(0f, 0f, ringAngle[i]);
        }
    }

    void Update()
    {
        // Once the lock is solved and the screen has closed, swing the lid open.
        if (solved && !screenOpen)
        {
            Quaternion open = Quaternion.Euler(lidOpenAngle, 0f, 0f);
            lid.localRotation = Quaternion.RotateTowards(lid.localRotation, open, lidSpeed * Time.deltaTime);
        }

        // Nothing here can be used while the game is paused or over.
        if (bowTaken || Time.timeScale == 0f)
        {
            return;
        }

        if (screenOpen)
        {
            UpdateLockScreen();
            return;
        }

        bool nearChest = Vector3.Distance(player.position, transform.position) <= reach;

        if (!nearChest)
        {
            promptText.text = "";
            return;
        }

        if (!solved)
        {
            promptText.text = promptOpen;
            if (Input.GetKeyDown(KeyCode.E))
            {
                OpenScreen();
            }
        }
        else
        {
            promptText.text = promptTake;
            if (Input.GetMouseButtonDown(1))
            {
                TakeBow();
            }
        }
    }

    void UpdateLockScreen()
    {
        // Turn each ring smoothly toward where it should be.
        for (int i = 0; i < 3; i++)
        {
            float target = ringSteps[i] * -45f;
            ringAngle[i] = Mathf.MoveTowardsAngle(ringAngle[i], target, turnSpeed * Time.deltaTime);
            rings[i].localRotation = Quaternion.Euler(0f, 0f, ringAngle[i]);
        }

        // Once solved, show the open lock for a moment, then close the screen.
        if (solved)
        {
            if (Time.time >= closeTime)
            {
                CloseScreen();
            }
            return;
        }

        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape))
        {
            CloseScreen();
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            ClickRings();
        }
    }

    // Works out which ring is under the mouse from how far the click is from the middle.
    void ClickRings()
    {
        float distance = Vector2.Distance(Input.mousePosition, centre.position);

        for (int i = 0; i < 3; i++)
        {
            float outerEdge = Radius(rings[i]);
            float innerEdge;
            if (i < 2)
            {
                innerEdge = Radius(rings[i + 1]);
            }
            else
            {
                innerEdge = Radius(centre);
            }

            if (distance <= outerEdge && distance > innerEdge)
            {
                TurnRing(i);
                return;
            }
        }
    }

    // Half the width of a round image, in screen pixels.
    float Radius(RectTransform image)
    {
        return image.rect.width * image.lossyScale.x / 2f;
    }

    void TurnRing(int index)
    {
        // The clicked ring goes one step clockwise.
        ringSteps[index] = (ringSteps[index] + 1) % 8;

        // The ring inside it is geared to it, so it goes one step the other way.
        if (index < 2)
        {
            ringSteps[index + 1] = (ringSteps[index + 1] + 7) % 8;
        }

        PlaySound(turnSound);

        if (ringSteps[0] == 0 && ringSteps[1] == 0 && ringSteps[2] == 0)
        {
            solved = true;
            closeTime = Time.time + closeDelay;
            centreImage.sprite = centreLitSprite;
            PlaySound(unlockSound);
        }
    }

    void OpenScreen()
    {
        screenOpen = true;
        lockScreen.SetActive(true);
        promptText.text = "";

        // Stop the player walking and looking around, and free the mouse for clicking.
        playerLook.enabled = false;
        playerMovement.enabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void CloseScreen()
    {
        screenOpen = false;
        lockScreen.SetActive(false);

        playerLook.enabled = true;
        playerMovement.enabled = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (solved)
        {
            bowOnChest.SetActive(true);
        }
    }

    void TakeBow()
    {
        bowTaken = true;
        hasBow = true;
        bowOnChest.SetActive(false);
        bowInHand.SetActive(true);
        bowShoot.enabled = true;
        crosshair.SetActive(true);
        promptText.text = "";
        PlaySound(pickupSound);
    }

    void PlaySound(AudioClip clip)
    {
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, player.position, volume);
        }
    }
}
