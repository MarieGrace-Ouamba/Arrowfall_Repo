// Marie-Grace Ouamba
// The prison lock. The player presses four symbol buttons in the right order to open the cell.

using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SymbolLock : MonoBehaviour
{
    [Header("Boss (optional)")]
    public BossHealth boss;          // the lock only works once the boss is dead

    [Header("The four symbol buttons")]
    public Button moonButton;
    public Button sunButton;
    public Button flameButton;
    public Button crownButton;

    [Header("Other UI")]
    public GameObject hintObject;   
    public GameObject puzzlePanel;   
    public TMP_Text messageText;     
    public Button closeButton;
    public GameObject winPanel;      
    public Button restartButton;

    [Header("The cell door")]
    public GameObject cellDoor;      // this object disappears when the lock opens

    // The correct order.
    private string[] solution = { "Moon", "Flame", "Crown", "Sun" };

    private int step = 0;            // how many symbols the player has got right so far
    private bool playerNear = false;
    private bool puzzleOpen = false;
    private bool solved = false;

    
    private PlayerMovement movement;
    private PlayerLook look;
    private BowShoot bow;

    void Start()
    {
        // Connect each button to its method
        moonButton.onClick.AddListener(PressMoon);
        sunButton.onClick.AddListener(PressSun);
        flameButton.onClick.AddListener(PressFlame);
        crownButton.onClick.AddListener(PressCrown);
        closeButton.onClick.AddListener(ClosePuzzle);
        restartButton.onClick.AddListener(RestartGame);

        
        hintObject.SetActive(false);
        puzzlePanel.SetActive(false);
        winPanel.SetActive(false);

        
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

        bool bossIsDead = (boss == null || boss.IsDead());

        if (!puzzleOpen)
        {
            
            hintObject.SetActive(playerNear && bossIsDead);

            if (playerNear && bossIsDead && Keyboard.current.eKey.wasPressedThisFrame)
            {
                OpenPuzzle();
            }
        }
        else if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ClosePuzzle();
        }
    }

   
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

    void OpenPuzzle()
    {
        puzzleOpen = true;
        step = 0;

        hintObject.SetActive(false);
        puzzlePanel.SetActive(true);
        messageText.text = "Choose the symbols in the correct order";

        FreezePlayer();
    }

    void ClosePuzzle()
    {
        puzzleOpen = false;
        puzzlePanel.SetActive(false);

        UnfreezePlayer();
    }

   
    void PressMoon() { Press("Moon"); }
    void PressSun() { Press("Sun"); }
    void PressFlame() { Press("Flame"); }
    void PressCrown() { Press("Crown"); }

   
    void Press(string symbol)
    {
        Debug.Log("Pressed " + symbol);

        if (symbol == solution[step])
        {
            step = step + 1;

            if (step == solution.Length)
            {
                Solve();
            }
            else
            {
                messageText.text = "Correct! " + step + "/" + solution.Length;
            }
        }
        else
        {
            step = 0;
            messageText.text = "Wrong sequence! Try again.";
        }
    }

    void Solve()
    {
        solved = true;

        puzzlePanel.SetActive(false);
        cellDoor.SetActive(false);  
        winPanel.SetActive(true);    // the player stays frozen and the mouse stays free for the Restart button
    }

    void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void FreezePlayer()
    {
        movement.enabled = false;
        look.enabled = false;
        bow.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    
    void UnfreezePlayer()
    {
        movement.enabled = true;
        look.enabled = true;
        bow.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}