// PlayerLook.cs
// Rayan Alduhaiman
// IT 485/585 - ArrowFall
// First person mouse look. Turns the body left and right, tilts the camera up and down.

using UnityEngine;

public class PlayerLook : MonoBehaviour
{
    [Header("Sensitivity")]
    public float sensitivity = 200f;

    [Header("References")]
    public Transform playerBody;   // drag the Player object here

    private float xRotation = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * sensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity * Time.deltaTime;

        // Up and down tilts the camera only, and never past straight up or down.
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Left and right turns the whole body, so movement follows your view.
        playerBody.Rotate(Vector3.up * mouseX);

        // Esc frees the mouse so you can get back to the Editor.
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}