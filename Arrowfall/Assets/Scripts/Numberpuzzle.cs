// NumberPuzzle.cs
// Rayan Alduhaiman
// IT 485/585 - ArrowFall
// The gate puzzle, run as one sequence from start to finish:
//   1. Wait until every guard is dead, then announce it.
//   2. Wait for the player to come to the gate, then count down.
//   3. Warm-up: a couple of untimed rounds. Hit the number shown. Misses do not count.
//   4. Main rounds: they get harder as they go. The time limit shrinks each round,
//      and from a set round on, the targets swap their numbers every round.
//      Three misses (wrong target or too slow) restart the main rounds.
//   5. When every main round is cleared, the gate opens.
// An optional EnemySpawner keeps sending guards while the rounds run.

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NumberPuzzle : MonoBehaviour
{
    enum Result
    {
        None,
        Correct,
        Wrong,
        TimeUp
    }

    [Header("Gate")]
    public GateController gate;

    [Header("UI")]
    public GameObject panel;                 // the whole puzzle panel, shown and hidden as one
    public Image panelImage;                 // the panel background, flashes green or red (optional)
    public TMP_Text displayText;             // the big number
    public Image ruleBadge;                  // the box holding the instruction (optional)
    public TMP_Text ruleText;                // the instruction above the number
    public GameObject timerBar;              // the timer bar, only shown in timed rounds
    public Image timerFill;                  // Image Type: Filled, Fill Method: Horizontal
    public TMP_Text progressText;            // progress dots and misses
    public TMP_Text messageText;             // Correct, Wrong, announcements

    [Header("Text")]
    public string instruction = "HIT";       // shown above the number each round

    [Header("Colors")]
    public Color panelColor = new Color(0f, 0f, 0f, 0f);
    public Color correctColor = new Color(0.2f, 0.75f, 0.3f, 0.85f);
    public Color wrongColor = new Color(0.85f, 0.15f, 0.15f, 0.85f);
    public Color timerColor = Color.white;
    public Color timerLowColor = new Color(0.9f, 0.2f, 0.2f);

    [Header("Start Condition")]
    public bool waitForGuards = true;        // the puzzle begins only after these guards are dead
    public EnemyHealth[] guards;             // leave empty to use every guard in the level
    public string guardsClearedMessage = "Outside guards eliminated";
    public float messageTime = 2.5f;         // seconds announcements stay up
    public int startCountdown = 3;           // "3, 2, 1" before the warm-up

    [Header("Enemies During Puzzle")]
    public EnemySpawner spawner;             // optional, keeps sending guards while the puzzle runs
    public bool spawnDuringWarmup = false;   // tick to start spawning from the warm-up instead of the main rounds

    [Header("Visibility")]
    public float showDistance = 25f;         // the panel appears when the player is this close to the puzzle

    [Header("Rounds")]
    public int warmupRounds = 2;             // untimed practice rounds, misses do not count
    public int mainRounds = 6;               // rounds that get harder and must all be cleared
    public int maxMisses = 3;                // this many misses restarts the main rounds
    [Range(2, 4)]
    public int highestNumber = 4;            // must match the targets, numbered 1 up to this

    [Header("Difficulty")]
    public float startRoundTime = 6f;        // seconds allowed in the first main round
    public float endRoundTime = 2.5f;        // seconds allowed in the last main round
    public int shuffleFromRound = 4;         // from this main round on, targets swap numbers every round (0 = never)
    public bool timeoutCountsAsMiss = true;  // running out of time counts toward the misses

    [Header("Timing")]
    public float pauseBetweenRounds = 0.8f;

    [Header("Sound")]
    public AudioClip correctSound;
    public AudioClip wrongSound;
    public AudioClip solvedSound;
    [Range(0f, 1f)]
    public float soundVolume = 1f;

    [Header("Testing")]
    public bool debugLogs = true;            // prints each step to the Console, turn off when it works

    private Transform player;
    private bool panelWanted = false;        // true once the puzzle has something to show
    private float forceShowUntil = 0f;       // keeps the panel up for announcements, even far away

    private bool acceptingHits = false;      // only true while a round is waiting for an answer
    private Result pendingResult = Result.None;
    private int answer;
    private int lastNumber = 0;

    private bool inWarmup = true;
    private int warmupDone = 0;
    private int mainDone = 0;
    private int misses = 0;

    private NumberTarget[] targets;
    private int[] originalNumbers;           // each target's own number, restored when not shuffling

    private float flashAmount = 0f;
    private Color flashColor;
    private float popAmount = 0f;

    void Start()
    {
        PlayerMovement playerMovement = FindFirstObjectByType<PlayerMovement>();
        if (playerMovement != null)
        {
            player = playerMovement.transform;
        }

        if (guards == null || guards.Length == 0)
        {
            guards = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        }

        targets = FindObjectsByType<NumberTarget>(FindObjectsSortMode.None);
        originalNumbers = new int[targets.Length];
        for (int i = 0; i < targets.Length; i++)
        {
            originalNumbers[i] = targets[i].number;
        }

        ShowRoundUI(false);
        SetText(displayText, "");
        SetText(messageText, "");

        if (panel != null)
        {
            panel.SetActive(false);
        }

        StartCoroutine(RunPuzzle());
    }

    void Update()
    {
        AnimateFlash();
        AnimatePop();

        bool show = panelWanted && (IsPlayerNear() || Time.time < forceShowUntil);

        if (panel != null && panel.activeSelf != show)
        {
            panel.SetActive(show);
        }
    }

    // The whole puzzle, step by step.
    IEnumerator RunPuzzle()
    {
        // 1. Guards.
        if (waitForGuards && guards.Length > 0)
        {
            Log("Waiting for " + GuardsAlive() + " guards to die.");

            while (GuardsAlive() > 0)
            {
                yield return null;
            }

            Log("All guards dead.");
            panelWanted = true;
            SetText(displayText, "");
            SetText(messageText, guardsClearedMessage);
            forceShowUntil = Time.time + messageTime;
            yield return new WaitForSeconds(messageTime);
        }

        panelWanted = true;

        // 2. Player at the gate, then countdown.
        Log("Waiting for the player to come within " + showDistance + " m.");
        SetText(displayText, "");
        SetText(messageText, "");

        while (!IsPlayerNear())
        {
            yield return null;
        }

        for (int i = startCountdown; i > 0; i--)
        {
            SetText(displayText, i.ToString());
            SetText(messageText, "Get ready");
            Pop();
            yield return new WaitForSeconds(1f);
        }

        ShowRoundUI(true);

        // 3. Warm-up: untimed, misses do not count.
        if (spawner != null && spawnDuringWarmup)
        {
            spawner.StartSpawning();
        }

        inWarmup = true;
        warmupDone = 0;

        while (warmupDone < warmupRounds)
        {
            SetUpRound(0f, false);
            yield return StartCoroutine(WaitForResult(0f));

            if (pendingResult == Result.Correct)
            {
                Correct();
                warmupDone++;
                SetText(messageText, "Correct!");
            }
            else
            {
                Miss();
                SetText(messageText, "Wrong! Try again");
            }

            UpdateProgress();
            yield return new WaitForSeconds(pauseBetweenRounds);
        }

        if (warmupRounds > 0)
        {
            ShowRoundUI(false);
            SetText(displayText, "");
            SetText(messageText, "Warm-up done!");
            yield return new WaitForSeconds(messageTime);
            ShowRoundUI(true);
        }

        // 4. Main rounds: shrinking time, shuffled targets later on, three misses restart.
        if (spawner != null)
        {
            spawner.StartSpawning();
        }

        inWarmup = false;
        mainDone = 0;
        misses = 0;

        while (mainDone < mainRounds)
        {
            float progress = (mainRounds > 1) ? (float)mainDone / (mainRounds - 1) : 1f;
            float roundTime = Mathf.Lerp(startRoundTime, endRoundTime, progress);
            bool shuffle = shuffleFromRound > 0 && (mainDone + 1) >= shuffleFromRound;

            SetUpRound(roundTime, shuffle);
            yield return StartCoroutine(WaitForResult(roundTime));

            if (pendingResult == Result.Correct)
            {
                Correct();
                mainDone++;
                SetText(messageText, "Correct!");
                UpdateProgress();
                yield return new WaitForSeconds(pauseBetweenRounds);
                continue;
            }

            Miss();
            bool counts = pendingResult == Result.Wrong || timeoutCountsAsMiss;
            string message = (pendingResult == Result.TimeUp) ? "Too slow!" : "Wrong!";

            if (counts)
            {
                misses++;
            }

            if (misses >= maxMisses)
            {
                Log(maxMisses + " misses, restarting the main rounds.");
                mainDone = 0;
                misses = 0;
                UpdateProgress();
                SetText(messageText, maxMisses + " misses! Starting over");
                yield return new WaitForSeconds(messageTime);
            }
            else
            {
                UpdateProgress();
                SetText(messageText, message);
                yield return new WaitForSeconds(pauseBetweenRounds);
            }
        }

        // 5. Done.
        Solve();
    }

    // Waits for a target hit, or for the time limit if the round is timed.
    IEnumerator WaitForResult(float roundTime)
    {
        pendingResult = Result.None;
        acceptingHits = true;
        float timeLeft = roundTime;

        while (pendingResult == Result.None)
        {
            // The clock only runs while the player is at the gate.
            if (roundTime > 0f && IsPlayerNear())
            {
                timeLeft -= Time.deltaTime;
                UpdateTimerBar(timeLeft, roundTime);

                if (timeLeft <= 0f)
                {
                    pendingResult = Result.TimeUp;
                }
            }

            yield return null;
        }

        acceptingHits = false;
    }

    // Picks a new number, sets the targets, and puts everything on screen.
    void SetUpRound(float roundTime, bool shuffle)
    {
        if (shuffle)
        {
            ShuffleTargets();
        }
        else
        {
            RestoreTargets();
        }

        // A different number from last round, so every round visibly changes.
        do
        {
            answer = Random.Range(1, highestNumber + 1);
        }
        while (answer == lastNumber);

        lastNumber = answer;

        SetText(ruleText, instruction);
        SetText(displayText, answer.ToString());
        SetText(messageText, shuffle ? "The targets have moved!" : "");

        if (timerBar != null)
        {
            timerBar.SetActive(roundTime > 0f);
        }
        UpdateTimerBar(roundTime, roundTime);
        UpdateProgress();
        Pop();

        string stage = inWarmup ? "Warm-up " + (warmupDone + 1) : "Main " + (mainDone + 1);
        Log(stage + ": hit " + answer + (roundTime > 0f ? ", " + roundTime.ToString("F1") + " s" : ", untimed")
            + (shuffle ? ", targets shuffled" : ""));
    }

    // Gives the targets their numbers in a random order and updates their labels.
    void ShuffleTargets()
    {
        int[] numbers = (int[])originalNumbers.Clone();

        for (int i = numbers.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int temp = numbers[i];
            numbers[i] = numbers[j];
            numbers[j] = temp;
        }

        ApplyNumbers(numbers);
    }

    // Puts every target back on its own number.
    void RestoreTargets()
    {
        ApplyNumbers(originalNumbers);
    }

    void ApplyNumbers(int[] numbers)
    {
        for (int i = 0; i < targets.Length; i++)
        {
            targets[i].number = numbers[i];
            if (targets[i].label != null)
            {
                targets[i].label.text = numbers[i].ToString();
            }
        }
    }

    // Called by a NumberTarget when an arrow hits it.
    public void TargetHit(NumberTarget target)
    {
        if (!acceptingHits || pendingResult != Result.None)
        {
            Log("Target " + target.number + " hit, but no round is waiting for an answer.");
            return;
        }

        pendingResult = (target.number == answer) ? Result.Correct : Result.Wrong;
        Log("Target " + target.number + " hit: " + pendingResult);
    }

    void Correct()
    {
        PlaySound(correctSound);
        Flash(correctColor);
    }

    void Miss()
    {
        PlaySound(wrongSound);
        Flash(wrongColor);
    }

    // The panel disappears, the targets go back to normal, and the gate swings open.
    void Solve()
    {
        Log("Solved.");

        if (spawner != null)
        {
            spawner.StopSpawning();
        }

        RestoreTargets();
        ShowRoundUI(false);
        SetText(displayText, "");
        SetText(messageText, "");
        panelWanted = false;
        PlaySound(solvedSound);

        if (gate != null)
        {
            gate.Open();
        }
        else
        {
            Log("No gate assigned, so nothing opens.");
        }
    }

    // ---------- Helpers ----------

    bool IsPlayerNear()
    {
        if (player == null)
        {
            return true;
        }
        return Vector3.Distance(player.position, transform.position) <= showDistance;
    }

    // Counts guards that are still alive. A guard whose body was removed counts as dead.
    int GuardsAlive()
    {
        int alive = 0;
        foreach (EnemyHealth guard in guards)
        {
            if (guard != null && !guard.IsDead())
            {
                alive++;
            }
        }
        return alive;
    }

    // Turns the instruction, timer and progress on for rounds, off for announcements.
    void ShowRoundUI(bool visible)
    {
        if (ruleBadge != null)
        {
            ruleBadge.gameObject.SetActive(visible);
        }
        else if (ruleText != null)
        {
            ruleText.gameObject.SetActive(visible);
        }

        if (timerBar != null && !visible)
        {
            timerBar.SetActive(false);
        }

        if (progressText != null)
        {
            progressText.gameObject.SetActive(visible);
        }
    }

    // Drains from full to empty over the round, turning red in the last third.
    void UpdateTimerBar(float timeLeft, float roundTime)
    {
        if (timerFill == null || roundTime <= 0f)
        {
            return;
        }

        float left = Mathf.Clamp01(timeLeft / roundTime);
        timerFill.fillAmount = left;
        timerFill.color = (left < 0.33f) ? timerLowColor : timerColor;
    }

    // Warm-up shows "Warm-up 1 / 2". Main rounds show dots for cleared rounds and an X per miss.
    void UpdateProgress()
    {
        if (inWarmup)
        {
            SetText(progressText, "Warm-up " + Mathf.Min(warmupDone + 1, warmupRounds) + " / " + warmupRounds);
            return;
        }

        string text = "";
        for (int i = 0; i < mainRounds; i++)
        {
            if (i > 0)
            {
                text += " ";
            }
            text += (i < mainDone) ? "●" : "○";
        }

        if (misses > 0)
        {
            text += "     ";
            for (int i = 0; i < misses; i++)
            {
                text += "X";
            }
        }

        SetText(progressText, text);
    }

    // The panel briefly takes on a color, then fades back.
    void Flash(Color color)
    {
        flashColor = color;
        flashAmount = 1f;
    }

    void AnimateFlash()
    {
        if (panelImage == null)
        {
            return;
        }

        flashAmount = Mathf.Max(0f, flashAmount - Time.deltaTime * 2.5f);
        panelImage.color = Color.Lerp(panelColor, flashColor, flashAmount);
    }

    // The number briefly grows when it changes, so a new round catches the eye.
    void Pop()
    {
        popAmount = 1f;
    }

    void AnimatePop()
    {
        if (displayText == null)
        {
            return;
        }

        popAmount = Mathf.Max(0f, popAmount - Time.deltaTime * 4f);
        displayText.rectTransform.localScale = Vector3.one * (1f + 0.25f * popAmount);
    }

    void SetText(TMP_Text text, string value)
    {
        if (text != null)
        {
            text.text = value;
        }
    }

    void PlaySound(AudioClip clip)
    {
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, transform.position, soundVolume);
        }
    }

    void Log(string message)
    {
        if (debugLogs)
        {
            Debug.Log("[NumberPuzzle] " + message);
        }
    }
}
