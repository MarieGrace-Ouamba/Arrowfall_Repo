// NumberPuzzle.cs
// Rayan Alduhaiman
// IT 485/585 - ArrowFall
// The gate puzzle, run as one sequence from start to finish:
//   1. Wait until every guard is dead, then announce it.
//   2. Wait for the player to come to the gate.
//   3. Explain DIGIT and COUNT, then count down.
//   4. Warm-up rounds: the digit and the count match ("2 2"), so either answer is right.
//   5. Challenge rounds: they differ, and each round randomly asks for DIGIT or COUNT.
//   6. When every round is cleared, the gate opens.
// The screen shows one digit repeated a number of times, like "3 3":
// DIGIT is the number shown (3), COUNT is how many there are (2).
// Challenge rounds have a draining timer, paused while the player is away from the gate.
// The warm-up is untimed.

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NumberPuzzle : MonoBehaviour
{
    public enum Rule
    {
        Digit,          // hit the number that is shown
        Count           // hit how many numbers are shown
    }

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
    public Image panelImage;                 // the panel background, flashes green or red
    public TMP_Text displayText;             // the big numbers
    public Image ruleBadge;                  // colored box behind the rule word
    public TMP_Text ruleText;                // DIGIT or COUNT
    public GameObject timerBar;              // the timer's background, holding the fill
    public Image timerFill;                  // Image Type: Filled, Fill Method: Horizontal
    public TMP_Text progressText;            // dots for cleared rounds
    public TMP_Text messageText;             // Correct, Wrong, announcements

    [Header("Colors")]
    public Color panelColor = new Color(0f, 0f, 0f, 0.65f);
    public bool backgroundOnlyDuringRounds = true;   // announcements and the tutorial show text with no box behind it
    public Color digitColor = new Color(1f, 0.78f, 0.2f);
    public Color countColor = new Color(0.2f, 0.8f, 1f);
    public Color correctColor = new Color(0.2f, 0.75f, 0.3f, 0.85f);
    public Color wrongColor = new Color(0.85f, 0.15f, 0.15f, 0.85f);
    public Color timerColor = Color.white;
    public Color timerLowColor = new Color(0.9f, 0.2f, 0.2f);

    [Header("Start Condition")]
    public bool waitForGuards = true;        // the puzzle begins only after these guards are dead
    public EnemyHealth[] guards;             // leave empty to use every guard in the level
    public string guardsClearedMessage = "Outside guards eliminated";
    public float messageTime = 2.5f;         // seconds the announcement stays up
    public int startCountdown = 3;           // "3, 2, 1" before the first round

    [Header("Tutorial")]
    public bool showTutorial = true;         // explains DIGIT and COUNT before the warm-up
    public bool advanceOnHit = true;         // each explanation waits until the player shoots any target
    public float slideTime = 4f;             // used instead when advanceOnHit is off
    public float challengeIntroTime = 3f;    // seconds the "now they differ" message stays up

    [Header("Visibility")]
    public float showDistance = 30f;         // the panel appears when the player is this close to the puzzle
    public float hideAfterSolved = 3f;       // seconds the "gate opens" message stays before hiding

    [Header("Rounds")]
    public int warmupRounds = 4;
    public int challengeRounds = 6;
    public bool restartOnMistake = true;     // a wrong hit sends the player back to the first challenge round
    public int maxSameRuleInARow = 2;        // stops long streaks of the same random rule
    [Range(2, 4)]
    public int highestNumber = 4;            // must match the targets, numbered 1 up to this

    [Header("Timing")]
    public float roundTime = 6f;             // seconds per round before it changes, 0 = no limit
    public bool timeoutCountsAsMistake = false;    // tick to treat running out of time like a wrong hit
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
    private bool solved = false;
    private float solvedTime = 0f;
    private float forceShowUntil = 0f;       // keeps the panel up for announcements, even far away

    private bool acceptingHits = false;      // only true while a round is waiting for an answer
    private Result pendingResult = Result.None;
    private int answer;

    private int round = 0;
    private int lastDigit = 0;
    private int lastCount = 0;
    private Rule lastRule = Rule.Digit;
    private int sameRuleCount = 0;
    private int digitUses = 0;               // how often each rule has come up, to keep them balanced
    private int countUses = 0;

    private bool showBackground = false;     // the tinted box is only drawn during rounds
    private bool waitingForTutorialHit = false;
    private bool tutorialHit = false;

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
        else
        {
            Log("No player found. The panel will always count as near.");
        }

        if (guards == null || guards.Length == 0)
        {
            guards = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
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

        if (solved && Time.time - solvedTime > hideAfterSolved)
        {
            show = false;
        }

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

        // 2. Player at the gate.
        Log("Waiting for the player to come within " + showDistance + " m.");
        SetText(displayText, "");
        SetText(messageText, "");

        while (!IsPlayerNear())
        {
            yield return null;
        }

        // 3. Explanation and countdown.
        if (showTutorial)
        {
            Log("Tutorial.");
            yield return StartCoroutine(Tutorial());
        }

        for (int i = startCountdown; i > 0; i--)
        {
            SetText(displayText, i.ToString());
            SetText(messageText, "Get ready");
            Pop();
            yield return new WaitForSeconds(1f);
        }

        // 4 and 5. Rounds.
        Log("Rounds start.");
        showBackground = true;
        ShowRoundUI(true);
        round = 0;
        int totalRounds = warmupRounds + challengeRounds;

        while (round < totalRounds)
        {
            bool warmup = round < warmupRounds;
            SetUpRound(warmup);

            // Wait for a hit or for the timer to run out.
            pendingResult = Result.None;
            acceptingHits = true;
            float timeLeft = roundTime;

            while (pendingResult == Result.None)
            {
                // The warm-up has no time limit; only the challenge is timed.
                if (!warmup && roundTime > 0f && IsPlayerNear())
                {
                    timeLeft -= Time.deltaTime;
                    UpdateTimerBar(timeLeft);

                    if (timeLeft <= 0f)
                    {
                        pendingResult = Result.TimeUp;
                    }
                }

                yield return null;
            }

            acceptingHits = false;

            // React to the answer.
            if (pendingResult == Result.Correct)
            {
                PlaySound(correctSound);
                Flash(correctColor);
                round++;
                UpdateProgress();
                SetText(messageText, "Correct!");
                yield return new WaitForSeconds(pauseBetweenRounds);

                // The warm-up just ended, so prepare the player for the real thing.
                if (round == warmupRounds && challengeRounds > 0)
                {
                    SetText(displayText, "");
                    SetBadgeVisible(false);
                    SetText(messageText, "Warm-up done! Now they differ");
                    yield return new WaitForSeconds(challengeIntroTime);
                }
            }
            else
            {
                PlaySound(wrongSound);
                Flash(wrongColor);

                bool mistake = pendingResult == Result.Wrong || timeoutCountsAsMistake;
                string message = (pendingResult == Result.TimeUp) ? "Too slow!" : "Wrong!";

                if (mistake && restartOnMistake && round > warmupRounds)
                {
                    round = warmupRounds;
                    message += " Start the challenge again";
                }

                UpdateProgress();
                SetText(messageText, message);
                yield return new WaitForSeconds(pauseBetweenRounds);
            }
        }

        // 6. Done.
        Solve();
    }

    // Two slides with the same example, then a note about the first rounds.
    // Each one stays until the player shoots any target, so nobody gets rushed.
    IEnumerator Tutorial()
    {
        SetBadgeVisible(true);
        ShowSlide(Rule.Digit, "3   3", "DIGIT: hit the number shown (3)");
        yield return StartCoroutine(WaitForSlide());

        ShowSlide(Rule.Count, "3   3", "COUNT: hit how many there are (2)");
        yield return StartCoroutine(WaitForSlide());

        SetBadgeVisible(false);
        SetText(displayText, "2   2");
        SetText(messageText, "First rounds: both answers match");
        Pop();
        yield return StartCoroutine(WaitForSlide());
    }

    // Waits for any target hit, or for slideTime if advanceOnHit is off.
    IEnumerator WaitForSlide()
    {
        if (!advanceOnHit)
        {
            yield return new WaitForSeconds(slideTime);
            yield break;
        }

        tutorialHit = false;
        waitingForTutorialHit = true;

        while (!tutorialHit)
        {
            yield return null;
        }

        waitingForTutorialHit = false;
        yield return new WaitForSeconds(0.3f);
    }

    void ShowSlide(Rule rule, string example, string explanation)
    {
        ShowRule(rule);
        SetText(displayText, example);
        SetText(messageText, explanation);
        Pop();
    }

    // Picks the numbers and the rule for one round and puts them on screen.
    void SetUpRound(bool warmup)
    {
        int digit;
        int count;
        Rule rule = ChooseRule();

        if (warmup)
        {
            // Digit and count match, so either rule gives the same answer.
            do
            {
                digit = Random.Range(1, highestNumber + 1);
            }
            while (digit == lastDigit);

            count = digit;
            answer = digit;
        }
        else
        {
            // Digit and count differ, and never repeat the last round exactly.
            do
            {
                digit = Random.Range(1, highestNumber + 1);
                count = Random.Range(1, highestNumber + 1);
            }
            while (digit == count || (digit == lastDigit && count == lastCount));

            answer = (rule == Rule.Digit) ? digit : count;
        }

        lastDigit = digit;
        lastCount = count;

        SetText(displayText, BuildDisplay(digit, count));
        SetText(messageText, "");
        ShowRule(rule);
        SetBadgeVisible(true);

        if (timerBar != null)
        {
            timerBar.SetActive(!warmup && roundTime > 0f);
        }
        UpdateTimerBar(roundTime);
        UpdateProgress();
        Pop();

        Log("Round " + (round + 1) + ": " + rule + ", showing " + digit + " x" + count + ", answer " + answer);
    }

    // DIGIT or COUNT at random, with two limits so it never feels one-sided:
    // never more than maxSameRuleInARow of the same in a row,
    // and if one rule has come up two more times than the other, the other one is next.
    Rule ChooseRule()
    {
        Rule pick = (Random.value < 0.5f) ? Rule.Digit : Rule.Count;

        if (digitUses - countUses >= 2)
        {
            pick = Rule.Count;
        }
        else if (countUses - digitUses >= 2)
        {
            pick = Rule.Digit;
        }
        else if (pick == lastRule && sameRuleCount >= maxSameRuleInARow)
        {
            pick = (pick == Rule.Digit) ? Rule.Count : Rule.Digit;
        }

        if (pick == Rule.Digit)
        {
            digitUses++;
        }
        else
        {
            countUses++;
        }

        if (pick == lastRule)
        {
            sameRuleCount++;
        }
        else
        {
            lastRule = pick;
            sameRuleCount = 1;
        }

        return pick;
    }

    // Called by a NumberTarget when an arrow hits it.
    public void TargetHit(NumberTarget target)
    {
        // During the explanation, any target moves to the next slide.
        if (waitingForTutorialHit)
        {
            tutorialHit = true;
            Flash(correctColor);
            Log("Tutorial: target " + target.number + " hit, next slide.");
            return;
        }

        if (!acceptingHits || pendingResult != Result.None)
        {
            Log("Target " + target.number + " hit, but no round is waiting for an answer.");
            return;
        }

        pendingResult = (target.number == answer) ? Result.Correct : Result.Wrong;
        Log("Target " + target.number + " hit: " + pendingResult);
    }

    void Solve()
    {
        Log("Solved.");
        solved = true;
        showBackground = false;
        solvedTime = Time.time;

        // No closing message: the panel just disappears and the gate swings open.
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

    // Writes the digit count times with spaces between, like "3   3".
    string BuildDisplay(int digit, int count)
    {
        string display = "";
        for (int i = 0; i < count; i++)
        {
            if (i > 0)
            {
                display += "   ";
            }
            display += digit.ToString();
        }
        return display;
    }

    void ShowRule(Rule rule)
    {
        SetText(ruleText, (rule == Rule.Digit) ? "DIGIT" : "COUNT");

        if (ruleBadge != null)
        {
            ruleBadge.color = (rule == Rule.Digit) ? digitColor : countColor;
        }
    }

    void SetBadgeVisible(bool visible)
    {
        if (ruleBadge != null)
        {
            ruleBadge.gameObject.SetActive(visible);
        }
    }

    // Turns the rule badge, timer and progress on for rounds, off for announcements.
    void ShowRoundUI(bool visible)
    {
        SetBadgeVisible(visible);

        if (timerBar != null)
        {
            timerBar.SetActive(visible && roundTime > 0f);
        }

        if (progressText != null)
        {
            progressText.gameObject.SetActive(visible);
        }
    }

    // Drains from full to empty over the round, turning red in the last third.
    void UpdateTimerBar(float timeLeft)
    {
        if (timerFill == null || roundTime <= 0f)
        {
            return;
        }

        float left = Mathf.Clamp01(timeLeft / roundTime);
        timerFill.fillAmount = left;
        timerFill.color = (left < 0.33f) ? timerLowColor : timerColor;
    }

    // One dot per round, filled for the ones already cleared.
    void UpdateProgress()
    {
        int totalRounds = warmupRounds + challengeRounds;
        string dots = "";

        for (int i = 0; i < totalRounds; i++)
        {
            if (i > 0)
            {
                dots += " ";
            }
            dots += (i < round) ? "●" : "○";
        }

        SetText(progressText, dots);
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

        // Outside the rounds the box is fully see-through, so only the text shows.
        Color baseColor = panelColor;
        if (backgroundOnlyDuringRounds && !showBackground)
        {
            baseColor = new Color(panelColor.r, panelColor.g, panelColor.b, 0f);
        }

        panelImage.color = Color.Lerp(baseColor, flashColor, flashAmount);
    }

    // The numbers briefly grow when they change, so a new round catches the eye.
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
