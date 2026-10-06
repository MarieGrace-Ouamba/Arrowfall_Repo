using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(CharacterController))]
public class TestPlayerMovement : MonoBehaviour
{
    public float speed = 5f;

    private CharacterController controller;
    private float verticalSpeed;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        float x = 0f;
        float z = 0f;

#if ENABLE_INPUT_SYSTEM
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            x = (keyboard.dKey.isPressed ? 1f : 0f)
              - (keyboard.aKey.isPressed ? 1f : 0f);
            z = (keyboard.wKey.isPressed ? 1f : 0f)
              - (keyboard.sKey.isPressed ? 1f : 0f);
        }
#else
        x = Input.GetAxisRaw("Horizontal");
        z = Input.GetAxisRaw("Vertical");
#endif

        Vector3 movement = new Vector3(x, 0f, z).normalized;

        if (controller.isGrounded && verticalSpeed < 0f)
            verticalSpeed = -2f;

        verticalSpeed -= 9.81f * Time.deltaTime;

        controller.Move(
            (movement * speed + Vector3.up * verticalSpeed)
            * Time.deltaTime
        );
    }
}