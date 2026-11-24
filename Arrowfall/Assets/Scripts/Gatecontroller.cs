// GateController.cs
// Rayan Alduhaiman
// IT 485/585 - ArrowFall
// Swings the gate doors open from their closed position.
// Opens when the player presses the open key, or when another script calls Open()
// (for example, the three-target puzzle).
// Each door turns around the gate's vertical axis by its own open angle,
// so one door can swing left and the other right.

using System.Collections;
using UnityEngine;

public class GateController : MonoBehaviour
{
    [System.Serializable]
    public class Door
    {
        public Transform door;              // one half of the gate
        public float openAngle = 90f;       // degrees to swing; use a negative value for the other half
    }

    [Header("Doors")]
    public Door[] doors;

    [Header("Opening")]
    public KeyCode openKey = KeyCode.G;
    public bool keyCanOpen = true;          // turn off once the target puzzle should be the only way in
    public float openTime = 2f;             // seconds for the doors to swing fully open

    [Header("Sound")]
    public AudioClip openSound;             // optional creak when the gate starts opening
    [Range(0f, 1f)]
    public float soundVolume = 1f;

    private Quaternion[] closedRotations;
    private Quaternion[] openRotations;
    private bool isOpen = false;

    void Start()
    {
        // The rotation each door has in the scene is its closed position.
        closedRotations = new Quaternion[doors.Length];
        openRotations = new Quaternion[doors.Length];

        for (int i = 0; i < doors.Length; i++)
        {
            if (doors[i].door == null)
            {
                continue;
            }

            closedRotations[i] = doors[i].door.localRotation;

            // Turning around the parent's up axis keeps the door upright,
            // even if the model was imported lying on its side.
            openRotations[i] = Quaternion.Euler(0f, doors[i].openAngle, 0f) * closedRotations[i];
        }
    }

    void Update()
    {
        if (keyCanOpen && Input.GetKeyDown(openKey))
        {
            Open();
        }
    }

    // Called by the open key, or by any other script that should open the gate.
    public void Open()
    {
        if (isOpen)
        {
            return;
        }

        isOpen = true;

        if (openSound != null)
        {
            AudioSource.PlayClipAtPoint(openSound, transform.position, soundVolume);
        }

        StartCoroutine(Swing());
    }

    public bool IsOpen()
    {
        return isOpen;
    }

    IEnumerator Swing()
    {
        float time = 0f;

        while (time < openTime)
        {
            time += Time.deltaTime;

            // Starts slowly, speeds up, then settles, like a heavy gate.
            float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(time / openTime));

            for (int i = 0; i < doors.Length; i++)
            {
                if (doors[i].door != null)
                {
                    doors[i].door.localRotation = Quaternion.Slerp(closedRotations[i], openRotations[i], progress);
                }
            }

            yield return null;
        }
    }
}