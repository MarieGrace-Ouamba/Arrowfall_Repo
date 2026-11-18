
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class RiddleDoor : MonoBehaviour
{
    public string riddle = "The more you take from me, the larger I become. What am I?";
    public string answer1 = "A shadow";
    public string answer2 = "A hole";
    public string answer3 = "A debt";
    public int correctAnswer = 2;

    public GameObject hintObject;
    public GameObject riddlePanel;
    public TMP_Text questionText;
    public TMP_Text feedbackText;

    public Button answerButton1;
    public Button answerButton2;
    public Button answerButton3;
    public Button closeButton;

    public GameObject arenaDoor;

    private bool playerNear = false;
    private bool panelOpen = false;
    private bool solved = false;

    private PlayerMovement movement;
    private PlayerLook look;
    private BowShoot bow;

    void Start()
    {
        answerButton1.onClick.AddListener(PickAnswer1);
        answerButton2.onClick.AddListener(PickAnswer2);
        answerButton3.onClick.AddListener(PickAnswer3);
        closeButton.onClick.AddListener(ClosePanel);

        questionText.text = riddle;

        answerButton1.GetComponentInChildren<TMP_Text>().text = answer1;
        answerButton2.GetComponentInChildren<TMP_Text>().text = answer2;
        answerButton3.GetComponentInChildren<TMP_Text>().text = answer3;

        hintObject.SetActive(false);
        riddlePanel.SetActive(false);

        GameObject player = GameObject.FindWithTag("Player");

        movement = player.GetComponent<PlayerMovement>();
        look = player.GetComponentInChildren<PlayerLook>();
        bow = player.GetComponentInChildren<BowShoot>();
    }

    void Update()
    {
        if (solved)
        {
            return;
        }
        Debug.Log("Player near: " + playerNear);
        riddlePanel.SetActive (playerNear && !panelOpen);
        hintObject.SetActive(playerNear && !panelOpen);
        Debug.Log(playerNear && !panelOpen);
        Debug.Log("playerNear = " + playerNear);
        Debug.Log("panelOpen = " + !panelOpen);


        if (Keyboard.current == null)
        {
            return;
        }

        if (playerNear && !panelOpen && Keyboard.current.eKey.wasPressedThisFrame)
        {
            OpenPanel();
        }

        if (panelOpen && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ClosePanel();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("trigger" + other.name);
        if (other.CompareTag("Player"))
        {
            Debug.Log("trigger hit player");
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
        arenaDoor.SetActive(false);

        movement.enabled = true;
        look.enabled = true;
        bow.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
