using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(MeshFilter))]
public class BossExitDoor : MonoBehaviour
{
    [Header("References")]
    public Transform player;

    [Tooltip("Exact scene name. Can be filled in later.")]
    public string finalSceneName = "";

    [Header("Door")]
    public float interactionDistance = 3f;
    public float openAngle = 90f;
    public float rotationSpeed = 180f;
    public bool oppositeHinge = false;

    private Vector3 hingePosition;
    private Vector3 hingeAxis;
    private Vector3 interactionPosition;

    private float currentAngle;
    private bool unlocked;
    private bool opening;
    private bool loading;

    private string statusMessage = "";
    private float statusUntil;

    void Start()
    {
        Mesh mesh = GetComponent<MeshFilter>().sharedMesh;

        if (mesh == null)
        {
            Debug.LogError("BossExitDoor needs a door mesh.", this);
            enabled = false;
            return;
        }

        Bounds bounds = mesh.bounds;
        Vector3 hinge = bounds.center;

        if (bounds.size.x >= bounds.size.z)
            hinge.x = oppositeHinge ? bounds.max.x : bounds.min.x;
        else
            hinge.z = oppositeHinge ? bounds.max.z : bounds.min.z;

        hingePosition = transform.TransformPoint(hinge);
        hingeAxis = transform.up;
        interactionPosition = transform.TransformPoint(bounds.center);
    }

    public void Unlock()
    {
        unlocked = true;
        statusMessage = "Slender defeated! The final door is unlocked.";
        statusUntil = Time.unscaledTime + 5f;
    }

    void Update()
    {
        if (loading)
            return;

        if (opening)
        {
            float nextAngle = Mathf.MoveTowards(
                currentAngle,
                openAngle,
                rotationSpeed * Time.deltaTime);

            transform.RotateAround(
                hingePosition,
                hingeAxis,
                nextAngle - currentAngle);

            currentAngle = nextAngle;
        }

        if (!PlayerNearby() || !Input.GetKeyDown(KeyCode.E))
            return;

        if (!unlocked)
        {
            statusMessage = "Defeat Slender to unlock this door.";
            statusUntil = Time.unscaledTime + 3f;
            return;
        }

        if (!opening)
        {
            opening = true;
            return;
        }

        if (!Mathf.Approximately(currentAngle, openAngle))
            return;

        if (string.IsNullOrWhiteSpace(finalSceneName) ||
            !Application.CanStreamedLevelBeLoaded(finalSceneName))
        {
            statusMessage = "Final level is not configured yet.";
            statusUntil = Time.unscaledTime + 4f;

            Debug.LogWarning(
                "Set Final Scene Name and add that scene " +
                "to the build's scene list.", this);

            return;
        }

        loading = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(finalSceneName);
    }

    bool PlayerNearby()
    {
        return player != null &&
            Vector3.Distance(
                player.position,
                interactionPosition) <= interactionDistance;
    }

    void OnGUI()
    {
        string message = "";

        if (Time.unscaledTime < statusUntil)
        {
            message = statusMessage;
        }
        else if (PlayerNearby())
        {
            if (!unlocked)
                message = "Defeat Slender to unlock this door.";
            else if (!opening)
                message = "Press E to open the final door.";
            else if (Mathf.Approximately(currentAngle, openAngle))
                message = "Press E to enter the final level.";
        }

        if (message == "")
            return;

        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.fontSize = 20;
        style.alignment = TextAnchor.MiddleCenter;

        GUI.Box(
            new Rect(
                Screen.width / 2f - 300f,
                Screen.height - 100f,
                600f,
                55f),
            message,
            style);
    }
}