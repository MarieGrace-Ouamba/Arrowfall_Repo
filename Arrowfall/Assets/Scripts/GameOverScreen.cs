// GameOverScreen.cs
// Rayan Alduhaiman
// IT 485/585 - ArrowFall
// Plays the death sequence when the player dies:
// the world slows to a stop while the view drops and tips over, the screen darkens,
// GAME OVER slams in, then the Restart button fades in and pulses.
// Everything runs on unscaled time, because the game itself is frozen.

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverScreen : MonoBehaviour
{
    [Header("References")]
    public GameObject panel;                // the Game Over panel, hidden until the player dies
    public RectTransform title;             // the GAME OVER text
    public RectTransform restartButton;     // the Restart button
    public Transform playerCamera;          // Main Camera, tips over when the player dies

    [Header("Death")]
    public float slowMotionTime = 1.2f;     // real seconds for the world to slow to a stop
    public float cameraDrop = 0.6f;         // how far the view sinks
    public float cameraRoll = 25f;          // how far the view tips sideways, in degrees

    [Header("Title")]
    public float titleSlamTime = 0.35f;     // how long GAME OVER takes to land
    public float titleStartScale = 2.5f;    // how big it starts before shrinking into place

    [Header("Button")]
    public float buttonDelay = 0.3f;        // pause after the title lands
    public float buttonFadeTime = 0.3f;
    public float pulseSpeed = 3f;           // how fast the button breathes
    public float pulseAmount = 0.05f;       // how much it grows, 0.05 = 5%

    private CanvasGroup panelGroup;
    private CanvasGroup buttonGroup;
    private bool showing = false;

    void Start()
    {
        if (panel != null)
        {
            panelGroup = GetCanvasGroup(panel);
            panel.SetActive(false);
        }

        if (restartButton != null)
        {
            buttonGroup = GetCanvasGroup(restartButton.gameObject);
        }
    }

    // A CanvasGroup lets us fade a whole piece of UI at once. Adds one if it is missing.
    CanvasGroup GetCanvasGroup(GameObject target)
    {
        CanvasGroup group = target.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = target.AddComponent<CanvasGroup>();
        }
        return group;
    }

    // Called by PlayerHealth when health reaches zero.
    public void Show()
    {
        if (showing || panel == null)
        {
            return;
        }

        showing = true;
        StartCoroutine(PlayDeath());
    }

    IEnumerator PlayDeath()
    {
        panel.SetActive(true);
        panelGroup.alpha = 0f;
        panelGroup.interactable = false;
        panelGroup.blocksRaycasts = false;

        if (title != null)
        {
            title.localScale = Vector3.zero;
        }

        if (buttonGroup != null)
        {
            buttonGroup.alpha = 0f;
        }

        Vector3 cameraStartPos = Vector3.zero;
        Quaternion cameraStartRot = Quaternion.identity;
        Quaternion cameraEndRot = Quaternion.identity;

        if (playerCamera != null)
        {
            cameraStartPos = playerCamera.localPosition;
            cameraStartRot = playerCamera.localRotation;
            cameraEndRot = cameraStartRot * Quaternion.Euler(0f, 0f, cameraRoll);
        }

        // 1. Slow motion while the view drops, tips over, and the screen darkens.
        float time = 0f;
        while (time < slowMotionTime)
        {
            time += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(time / slowMotionTime);
            float eased = 1f - (1f - progress) * (1f - progress);   // fast at first, settles at the end

            Time.timeScale = Mathf.Lerp(1f, 0f, progress);
            panelGroup.alpha = progress;

            if (playerCamera != null)
            {
                playerCamera.localPosition = cameraStartPos + Vector3.down * cameraDrop * eased;
                playerCamera.localRotation = Quaternion.Slerp(cameraStartRot, cameraEndRot, eased);
            }

            yield return null;
        }

        Time.timeScale = 0f;
        panelGroup.alpha = 1f;

        // 2. GAME OVER starts big and slams down to its normal size.
        if (title != null)
        {
            time = 0f;
            while (time < titleSlamTime)
            {
                time += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(time / titleSlamTime);
                float eased = 1f - Mathf.Pow(1f - progress, 3f);

                title.localScale = Vector3.one * Mathf.Lerp(titleStartScale, 1f, eased);
                yield return null;
            }

            title.localScale = Vector3.one;
        }

        // 3. Short pause, then free the mouse and fade the button in.
        yield return new WaitForSecondsRealtime(buttonDelay);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        panelGroup.interactable = true;
        panelGroup.blocksRaycasts = true;

        if (buttonGroup != null)
        {
            time = 0f;
            while (time < buttonFadeTime)
            {
                time += Time.unscaledDeltaTime;
                buttonGroup.alpha = Mathf.Clamp01(time / buttonFadeTime);
                yield return null;
            }

            buttonGroup.alpha = 1f;
        }

        // 4. The button gently pulses until it is clicked.
        while (true)
        {
            if (restartButton != null)
            {
                float pulse = 1f + Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseAmount;
                restartButton.localScale = Vector3.one * pulse;
            }

            yield return null;
        }
    }

    // Hooked to the Restart button's On Click.
    public void Restart()
    {
        StopAllCoroutines();

        // Time scale survives a scene reload, so unfreeze first.
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}