using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class DoorInteract : MonoBehaviour
{
    public Transform player;
    public float interactionDistance = 3f;
    public float openAngle = 90f;
    public float rotationSpeed = 180f;
    public bool oppositeHinge = false;

    public bool requiresKey1 = false;
    public bool requiresKey2 = false;
    public bool requiresKey3 = false;

    [Header("Optional quiz or message panel")]
    public GameObject quizPanel;
    public bool closePanelWithE = false;

    private Vector3 hingePosition;
    private Vector3 hingeAxis;
    private Vector3 interactionPosition;
    private float currentAngle;
    private bool isOpen;
    private bool quizShown;

    void Start()
    {
        if (quizPanel != null)
            quizPanel.SetActive(false);

        Bounds bounds = GetComponent<MeshFilter>().sharedMesh.bounds;
        Vector3 hinge = bounds.center;

        // Find the edge along the door's widest horizontal axis.
        if (bounds.size.x >= bounds.size.z)
            hinge.x = oppositeHinge ? bounds.max.x : bounds.min.x;
        else
            hinge.z = oppositeHinge ? bounds.max.z : bounds.min.z;

        hingePosition = transform.TransformPoint(hinge);
        hingeAxis = transform.up;
        interactionPosition = transform.TransformPoint(bounds.center);
    }

    void Update()
    {
        // Close the message without also toggling the door.
        if (quizPanel != null && quizPanel.activeInHierarchy)
        {
            if (closePanelWithE && Input.GetKeyDown(KeyCode.E))
                quizPanel.SetActive(false);

            return;
        }

        if (player == null)
            return;

        if (Vector3.Distance(player.position, interactionPosition)
            <= interactionDistance && Input.GetKeyDown(KeyCode.E))
        {
            if (requiresKey1 && !KeyPickup.hasKey1)
                Debug.Log("Door locked. Find Key 1.");
            else if (requiresKey2 && !KeyPickup.hasKey2)
                Debug.Log("Door locked. Find Key 2.");
            else if (requiresKey3 && !KeyPickup.hasKey3)
                Debug.Log("Door locked. Find Key 3.");
            else
                isOpen = !isOpen;
        }

        float targetAngle = isOpen ? openAngle : 0f;
        float nextAngle = Mathf.MoveTowards(
            currentAngle, targetAngle, rotationSpeed * Time.deltaTime);

        transform.RotateAround(
            hingePosition, hingeAxis, nextAngle - currentAngle);

        currentAngle = nextAngle;

        // Show the panel once, after the door finishes opening.
        if (isOpen && !quizShown && quizPanel != null &&
            Mathf.Approximately(currentAngle, openAngle))
        {
            quizShown = true;
            quizPanel.SetActive(true);
        }
    }
}