
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class RiddleDoor : MonoBehaviour
{
    [Header("Riddle")]
    [TextArea(2, 4)]
    public string riddle = "The more you take from me, the larger I become. What am I?";

    public string answer1 = "A shadow";
    public string answer2 = "A hole";
    public string answer3 = "A debt";

    public int correctAnswer = 2;

    [Header("Riddle UI")]
    public GameObject hintObject;
    public GameObject riddlePanel;

    public TMP_Text questionText;
    public TMP_Text feedbackText;

    public Button answerButton1;
    public Button answerButton2;
    public Button answerButton3;
    public Button closeButton;

    [Header("Arena Door")]
    public GameObject arenaDoor;

    [Header("Boss Fight")]
    public BossChase bossChase;
    public BossAttack bossAttack;
    public BossHealthBar bossBar;

    public GameObject entranceBlocker;
    public AudioSource entranceSound;

    private bool playerNear = false;
    private bool panelOpen = false;
    private bool solved = false;
    private bool fightStarted = false;

    private PlayerMovement movement;
    private PlayerLook look;
    private BowShoot bow;

    void Start()
    {
        // Hide the riddle at the beginning
        hintObject.SetActive(false);
        riddlePanel.SetActive(false);

        // Prepare the boss fight
        bossChase.enabled = false;
        bossAttack.enabled = false;

        if (entranceBlocker != null)
        {
            entranceBlocker.SetActive(false);
        }

        // Set up the riddle text
        questionText.text = riddle;

        answerButton1.GetComponentInChildren<TMP_Text>().text = answer1;
        answerButton2.GetComponentInChildren<TMP_Text>().text = answer2;
        answerButton3.GetComponentInChildren<TMP_Text>().text = answer3;

        // Connect the answer buttons
        answerButton1.onClick.AddListener(PickAnswer1);
        answerButton2.onClick.AddListener(PickAnswer2);
        answerButton3.onClick.AddListener(PickAnswer3);
        closeButton.onClick.AddListener(ClosePanel);

        // Find the player
        GameObject player = GameObject.FindWithTag("Player");

        movement = player.GetComponent<PlayerMovement>();
        look = player.GetComponentInChildren<PlayerLook>();
        bow = player.GetComponentInChildren<BowShoot>();
    }

    void Update()
    {
        hintObject.SetActive(playerNear && !panelOpen && !solved);

        if (Keyboard.current == null)
        {
            return;
        }

        if (playerNear && !panelOpen && !solved &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            OpenPanel();
        }

        if (panelOpen && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ClosePanel();
        }
    }

    // Detect the player near the riddle door
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerNear = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerNear = false;
        }
    }

    void OpenPanel()
    {
        panelOpen = true;
        feedbackText.text = "";

        riddlePanel.SetActive(true);
        hintObject.SetActive(false);

        movement.enabled = false;
        look.enabled = false;
        bow.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void ClosePanel()
    {
        panelOpen = false;
        riddlePanel.SetActive(false);

        movement.enabled = true;
        look.enabled = true;
        bow.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void PickAnswer1()
    {
        CheckAnswer(1);
    }

    void PickAnswer2()
    {
        CheckAnswer(2);
    }

    void PickAnswer3()
    {
        CheckAnswer(3);
    }

    void CheckAnswer(int answer)
    {
        if (answer == correctAnswer)
        {
            solved = true;
            feedbackText.text = "Correct! The door opens.";

            Invoke("OpenDoor", 1.5f);
        }
        else
        {
            feedbackText.text = "Wrong answer. Think again.";
        }
    }

    void OpenDoor()
    {
        riddlePanel.SetActive(false);
        hintObject.SetActive(false);

        if (arenaDoor != null)
        {
            arenaDoor.SetActive(false);
        }

        panelOpen = false;

        movement.enabled = true;
        look.enabled = true;
        bow.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Start the boss entrance sequence
    public void OnArenaEnter(Collider other)
    {
        if (other.CompareTag("Player") && !fightStarted)
        {
            fightStarted = true;

            if (entranceBlocker != null)
            {
                entranceBlocker.SetActive(true);
            }

            if (entranceSound != null)
            {
                entranceSound.Play();
            }

            Invoke("StartBossFight", 2f);
        }
    }

    void StartBossFight()
    {
        bossBar.Show();

        bossChase.enabled = true;
        bossAttack.enabled = true;
    }

    public void EndFight()
    {
        bossBar.Hide();

        if (entranceBlocker != null)
        {
            entranceBlocker.SetActive(false);
        }
    }
}
