// ShedEscape.cs
// Rayan Alduhaiman
// IT 485/585 - ArrowFall
// The shed escape at the very start of Level 1, which also teaches the bow.
// The player wakes locked inside the shed. A key hangs out of reach; the player
// shoots it down (ShootableKey), picks it up, then unlocks the door.
// The door opens with the existing GateController (one door, Key Can Open off).
// All prompts go to one shared bottom-centre text line, separate from the gate
// puzzle panel at the top of the screen.

using UnityEngine;
using TMPro;

public class ShedEscape : MonoBehaviour
{
    [Header("References")]
    public TMP_Text promptText;             // shared bottom-centre prompt line
    public GateController door;             // the shed door, Key Can Open off
    public Transform doorPoint;            // where the player stands to use the door (defaults to the door)
    public ShootableKey key;               // the hanging key
    public Transform player;               // found automatically if left empty
    public GameObject doorwayBlocker;      // invisible collider sealing the doorway; removed when the door opens

    [Header("Guards")]
    public GuardDistraction distraction;   // optional: the guards outside
    public GameObject topHint;             // "Distract the guards first" - shown if the door opens undistracted

    [Header("Reach")]
    public float doorRange = 3f;            // how close counts as "at the door"
    public float keyRange = 2.5f;           // how close counts as "at the fallen key"

    [Header("Messages")]
    public string startMessage = "Find the key to get out";
    public float startMessageTime = 4f;
    public string promptOpen = "Press E to open";
    public string lockedMessage = "It's locked. Find the key";
    public string promptPickup = "Press E to pick up the key";
    public string foundKeyMessage = "You found the key";
    public string promptUnlock = "Press E to unlock";
    public float flashTime = 2f;            // how long a one-off message stays up

    [Header("Input")]
    public KeyCode interactKey = KeyCode.E;

    [Header("Sound")]
    public AudioClip pickupSound;
    public AudioClip unlockSound;
    [Range(0f, 1f)]
    public float volume = 1f;

    private bool hasKey = false;
    private bool keyCollected = false;
    private bool doorOpened = false;
    private float flashUntil = 0f;
    private string flashMessage = "";

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

        if (doorPoint == null && door != null)
        {
            doorPoint = door.transform;
        }

        Flash(startMessage, startMessageTime);
    }

    void Update()
    {
        // A one-off message (locked / found key) stays up briefly and overrides prompts.
        if (Time.time < flashUntil)
        {
            SetPrompt(flashMessage);
            return;
        }

        if (doorOpened)
        {
            SetPrompt("");
            return;
        }

        bool pressed = Input.GetKeyDown(interactKey);

        // The fallen key takes priority while the player is standing over it.
        if (key != null && key.IsShot() && !keyCollected && NearKey())
        {
            SetPrompt(promptPickup);
            if (pressed)
            {
                PickUpKey();
            }
            return;
        }

        if (NearDoor())
        {
            if (hasKey)
            {
                SetPrompt(promptUnlock);
                if (pressed)
                {
                    UnlockDoor();
                }
            }
            else
            {
                SetPrompt(promptOpen);
                if (pressed)
                {
                    Flash(lockedMessage, flashTime);
                }
            }
            return;
        }

        SetPrompt("");
    }

    bool NearKey()
    {
        if (key == null || player == null)
        {
            return false;
        }
        return Vector3.Distance(player.position, key.transform.position) <= keyRange;
    }

    bool NearDoor()
    {
        if (doorPoint == null || player == null)
        {
            return false;
        }
        // Compare on the ground plane so the door's height does not matter.
        Vector3 a = player.position; a.y = 0f;
        Vector3 b = doorPoint.position; b.y = 0f;
        return Vector3.Distance(a, b) <= doorRange;
    }

    void PickUpKey()
    {
        keyCollected = true;
        hasKey = true;

        if (key != null)
        {
            key.gameObject.SetActive(false);
        }

        if (pickupSound != null && player != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, player.position, volume);
        }

        Flash(foundKeyMessage, flashTime);
    }

    void UnlockDoor()
    {
        if (doorOpened)
        {
            return;
        }
        doorOpened = true;

        if (unlockSound != null && doorPoint != null)
        {
            AudioSource.PlayClipAtPoint(unlockSound, doorPoint.position, volume);
        }

        if (door != null)
        {
            door.Open();
        }

        // Let the player walk out as the door swings.
        if (doorwayBlocker != null)
        {
            doorwayBlocker.SetActive(false);
        }

        // Opening the door with the guards still watching is a trap: hint the player.
        if (topHint != null && distraction != null && !distraction.IsDistracted())
        {
            topHint.SetActive(true);
        }

        SetPrompt("");
    }

    void Flash(string message, float seconds)
    {
        flashMessage = message;
        flashUntil = Time.time + seconds;
    }

    void SetPrompt(string message)
    {
        if (promptText != null && promptText.text != message)
        {
            promptText.text = message;
        }
    }
}
