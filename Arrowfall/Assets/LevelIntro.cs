using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class LevelIntro : MonoBehaviour
{
    public GameObject objectivePanel;
    private bool introOpen;
    private float previousTimeScale;

    void Start()
    {
        previousTimeScale = Time.timeScale;
        objectivePanel.SetActive(true);
        introOpen = true;
        Time.timeScale = 0f;
    }

    void Update()
    {
        if (!introOpen) return;

#if ENABLE_INPUT_SYSTEM
        bool pressedE = Keyboard.current != null &&
                        Keyboard.current.eKey.wasPressedThisFrame;
#else
        bool pressedE = Input.GetKeyDown(KeyCode.E);
#endif

        if (pressedE)
        {
            objectivePanel.SetActive(false);
            introOpen = false;
            Time.timeScale = previousTimeScale;
        }
    }

    void OnDisable()
    {
        if (introOpen)
        {
            Time.timeScale = previousTimeScale;
            introOpen = false;
        }
    }
}