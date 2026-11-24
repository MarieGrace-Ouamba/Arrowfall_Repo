// GameMenu.cs
// Rayan Alduhaiman
// IT 485/585 - ArrowFall
// One menu used two ways: as the main menu when the game starts, and as the
// pause menu when the pause key is pressed during play.
// It has a Settings page (master volume) and a Controls page.
// The whole menu is the GameMenu prefab. Drag it into any level and it works;
// it finds the player's bow by itself.

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;

public class GameMenu : MonoBehaviour
{
    [Header("Keys")]
    public KeyCode pauseKey = KeyCode.Q;    // change to Escape later

    [Header("Menu parts")]
    public GameObject menuRoot;             // everything that is shown and hidden
    public GameObject mainPage;
    public GameObject settingsPage;
    public GameObject controlsPage;
    public TMP_Text titleText;
    public GameObject playButton;           // only on the main menu
    public GameObject resumeButton;         // only on the pause menu
    public GameObject restartButton;        // only on the pause menu

    [Header("Main menu background")]
    public Image backgroundImage;           // full screen picture behind the main menu
    public Sprite startPicture;             // this level's picture; leave empty for no picture

    [Header("Settings")]
    public Slider volumeSlider;
    public TMP_Text volumeLabel;

    [Header("Text")]
    public string gameTitle = "ArrowFall";
    public string pauseTitle = "Paused";

    // The main menu is shown once when the game is launched, not after every
    // restart. "static" keeps the value when the level is loaded again.
    private static bool startMenuShown = false;

    private bool firstFrame = true;
    private bool menuOpen = false;
    private BowShoot bow;
    private bool bowWasEnabled;
    private CursorLockMode cursorModeBefore;
    private bool cursorVisibleBefore;

    void Start()
    {
        bow = FindFirstObjectByType<BowShoot>();

        // Buttons need an EventSystem in the scene. Add one if the level has none.
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        // Use the volume saved last time (1 = full volume if nothing was saved).
        float volume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        AudioListener.volume = volume;
        volumeSlider.value = volume;
        ShowVolume(volume);

        menuRoot.SetActive(false);
    }

    void Update()
    {
        // Wait one frame so every other script has finished starting up,
        // then show the main menu if this is the first time.
        if (firstFrame)
        {
            firstFrame = false;
            if (!startMenuShown)
            {
                startMenuShown = true;
                OpenMenu(true);
            }
            return;
        }

        if (Input.GetKeyDown(pauseKey))
        {
            if (menuOpen)
            {
                // The main menu can only be left with Play.
                if (!playButton.activeSelf)
                {
                    CloseMenu();
                }
            }
            else if (Time.timeScale > 0f)
            {
                // Do not open over the Game Over screen, which also freezes time.
                OpenMenu(false);
            }
        }
    }

    void OpenMenu(bool asMainMenu)
    {
        menuOpen = true;
        menuRoot.SetActive(true);
        ShowPage(mainPage);

        if (asMainMenu)
        {
            titleText.text = gameTitle;
        }
        else
        {
            titleText.text = pauseTitle;
        }

        // The main menu shows a picture of the level. The pause menu does not,
        // so the player can still see where they are.
        bool showPicture = asMainMenu && startPicture != null;
        backgroundImage.gameObject.SetActive(showPicture);
        if (showPicture)
        {
            backgroundImage.sprite = startPicture;
        }

        playButton.SetActive(asMainMenu);
        resumeButton.SetActive(!asMainMenu);
        restartButton.SetActive(!asMainMenu);

        // Freeze the game and free the mouse, remembering how things were.
        Time.timeScale = 0f;
        cursorModeBefore = Cursor.lockState;
        cursorVisibleBefore = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Stop the bow so clicking a button does not shoot an arrow.
        if (bow != null)
        {
            bowWasEnabled = bow.enabled;
            bow.enabled = false;
        }
    }

    void CloseMenu()
    {
        menuOpen = false;
        menuRoot.SetActive(false);

        Time.timeScale = 1f;
        Cursor.lockState = cursorModeBefore;
        Cursor.visible = cursorVisibleBefore;

        if (bow != null)
        {
            bow.enabled = bowWasEnabled;
        }
    }

    void ShowPage(GameObject page)
    {
        mainPage.SetActive(page == mainPage);
        settingsPage.SetActive(page == settingsPage);
        controlsPage.SetActive(page == controlsPage);
    }

    void ShowVolume(float volume)
    {
        volumeLabel.text = Mathf.RoundToInt(volume * 100f) + "%";
    }

    // ----- Hooked to the buttons' On Click -----

    public void OnPlayOrResume()
    {
        CloseMenu();
    }

    public void OnRestart()
    {
        // Time scale survives a scene reload, so unfreeze first.
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnSettings()
    {
        ShowPage(settingsPage);
    }

    public void OnControls()
    {
        ShowPage(controlsPage);
    }

    public void OnBack()
    {
        ShowPage(mainPage);
    }

    public void OnQuit()
    {
        Application.Quit();
#if UNITY_EDITOR
        // Quit does nothing inside the Unity editor, so stop Play mode instead.
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // Hooked to the volume slider's On Value Changed.
    public void OnVolumeChanged(float volume)
    {
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat("MasterVolume", volume);
        ShowVolume(volume);
    }
}
