// NumberPuzzle.cs
// Rayan Alduhaiman
// IT 485/585 - ArrowFall
// The gate puzzle. The screen shows one digit repeated a number of times, like "3 3".
// Warm-up: the digit and the count match ("2 2"), so either reading gives the same answer.
// Challenge: they differ, and the rule alternates every round between
// DIGIT (hit the number shown) and COUNT (hit how many there are).
// Each round has a countdown; when it runs out, the numbers and rule change.
// The puzzle waits until every guard is dead, announces it, counts down, then begins.
// Clearing every round opens the gate.

using System.Collections;
using TMPro;
using UnityEngine;

public class NumberPuzzle : MonoBehaviour
{
    public enum Rule
    {
        Matching,       // warm-up, digit and count are the same
        Digit,          // hit the number that is shown
        Count           // hit how many numbers are shown
    }

    [Header("References")]
    public GateController gate;
    public TMP_Text displayText;             // the big numbers
    public TMP_Text ruleText;                // DIGIT or COUNT, progress, and time left

    [Header("Start Condition")]
    public bool waitForGuards = true;        // the puzzle begins only after these guards are dead
    public EnemyHealth[] guards;             // leave empty to use every guard in the level
    public string guardsClearedMessage = "Outside guards eliminated";
    public float messageTime = 2.5f;         // seconds the message stays up
    public int startCountdown = 3;           // "3, 2, 1" before the first round

    [Header("Visibility")]
    public float showDistance = 30f;         // texts appear when the player is this close to the puzzle
    public float hideAfterSolved = 3f;       // seconds the "gate opens" message stays before hiding

    [Header("Rounds")]
    public int warmupRounds = 4;
    public int challengeRounds = 6;
    public Rule firstChallengeRule = Rule.Digit;    // the challenge alternates starting with this rule
    public bool restartOnMistake = true;     // a wrong hit sends the player back to the first challenge round
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

    private int round = 0;
    private Rule currentRule;
    private int answer;
    private int lastDigit = 0;
    private int lastCount = 0;
    private float roundTimer = 0f;

    private bool started = false;            // rounds are running
    private bool introRunning = false;       // message and countdown are playing
    private bool waiting = false;            // showing "Correct" or "Wrong" between rounds
    private bool solved = false;
    private float solvedTime = 0f;
    private float forceShowUntil = 0f;       // keeps the texts visible for announcements, even far away
    private Transform player;

    void Start()
    {
        PlayerMovement playerMovement = FindFirstObjectByType<PlayerMovement>();
        if (playerMovement != null)
        {
            player = playerMovement.transform;
        }

        if (waitForGuards && (guards == null || guards.Length == 0))
        {
            guards = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        }

        // No guards to wait for: skip the announcement and go straight to the countdown.
        if (!waitForGuards || guards.Length == 0)
        {
            StartCoroutine(Intro(false));
        }
    }

    void Update()
    {
        // Until the guards are dead, the screen stays empty.
        if (!started && !introRunning)
        {
            if (GuardsAlive() == 0)
            {
                StartCoroutine(Intro(true));
            }
            else
            {
                displayText.text = "";
                ruleText.text = "";
            }
        }

        // The round countdown.
        if (started && !solved && !waiting && roundTime > 0f)
        {
            roundTimer -= Time.deltaTime;
            UpdateRuleText();

            if (roundTimer <= 0f)
            {
                TimeUp();
            }
        }

        // Show the texts near the gate, during announcements, and briefly after solving.
        bool show = IsPlayerNear() || Time.time < forceShowUntil;

        if (solved && Time.time - solvedTime > hideAfterSolved)
        {
            show = false;
        }

        SetTextsVisible(show);
    }

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

    // Announces the guards are gone, waits for the player to reach the gate, counts down, then starts.
    IEnumerator Intro(bool announceGuards)
    {
        introRunning = true;

        if (announceGuards)
        {
            displayText.text = "";
            ruleText.text = guardsClearedMessage;
            forceShowUntil = Time.time + messageTime;
            yield return new WaitForSeconds(messageTime);
        }

        // The countdown only starts once the player is at the gate to see it.
        while (!IsPlayerNear())
        {
            displayText.text = "";
            ruleText.text = "Go to the gate";
            yield return null;
        }

        for (int i = startCountdown; i > 0; i--)
        {
            displayText.text = i.ToString();
            ruleText.text = "Get ready";
            yield return new WaitForSeconds(1f);
        }

        introRunning = false;
        started = true;
        round = 0;
        NextRound();
    }

    void NextRound()
    {
        int totalRounds = warmupRounds + challengeRounds;
        if (round >= totalRounds)
        {
            Solve();
            return;
        }

        int digit;
        int count;

        if (round < warmupRounds)
        {
            currentRule = Rule.Matching;

            // Same digit and count, never the same as the last round.
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
            int challengeIndex = round - warmupRounds;
            Rule otherRule = (firstChallengeRule == Rule.Digit) ? Rule.Count : Rule.Digit;
            currentRule = (challengeIndex % 2 == 0) ? firstChallengeRule : otherRule;

            // Digit and count always differ, otherwise the rule would not matter.
            // Also avoid showing exactly the same thing twice in a row.
            do
            {
                digit = Random.Range(1, highestNumber + 1);
                count = Random.Range(1, highestNumber + 1);
            }
            while (digit == count || (digit == lastDigit && count == lastCount));

            answer = (currentRule == Rule.Digit) ? digit : count;
        }

        lastDigit = digit;
        lastCount = count;

        displayText.text = BuildDisplay(digit, count);
        roundTimer = roundTime;
        UpdateRuleText();

        waiting = false;
    }

    // Rule on the first line, progress and time left on the second.
    void UpdateRuleText()
    {
        string progress = (round + 1) + " / " + (warmupRounds + challengeRounds);

        if (roundTime > 0f)
        {
            progress += "     " + Mathf.CeilToInt(Mathf.Max(roundTimer, 0f)) + "s";
        }

        ruleText.text = RuleLabel(currentRule) + "\n" + progress;
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

    string RuleLabel(Rule rule)
    {
        switch (rule)
        {
            case Rule.Matching:
                return "DIGIT = COUNT";
            case Rule.Digit:
                return "DIGIT";
            default:
                return "COUNT";
        }
    }

    // The countdown ran out before the player hit anything.
    void TimeUp()
    {
        PlaySound(wrongSound);

        if (timeoutCountsAsMistake && restartOnMistake && round > warmupRounds)
        {
            round = warmupRounds;
            StartCoroutine(ShowThenContinue("Too slow! Start the challenge again"));
        }
        else
        {
            StartCoroutine(ShowThenContinue("Too slow!"));
        }
    }

    // Called by a NumberTarget when an arrow hits it.
    public void TargetHit(NumberTarget target)
    {
        if (!started || solved || waiting)
        {
            return;
        }

        if (target.number == answer)
        {
            PlaySound(correctSound);
            round++;
            StartCoroutine(ShowThenContinue("Correct!"));
        }
        else
        {
            PlaySound(wrongSound);

            if (restartOnMistake && round > warmupRounds)
            {
                round = warmupRounds;
                StartCoroutine(ShowThenContinue("Wrong! Start the challenge again"));
            }
            else
            {
                StartCoroutine(ShowThenContinue("Wrong! Try again"));
            }
        }
    }

    // Shows the result for a moment, then moves on to the next round.
    IEnumerator ShowThenContinue(string message)
    {
        waiting = true;
        ruleText.text = message;
        yield return new WaitForSeconds(pauseBetweenRounds);
        NextRound();
    }

    void Solve()
    {
        solved = true;
        solvedTime = Time.time;
        displayText.text = "OPEN";
        ruleText.text = "The gate opens";
        PlaySound(solvedSound);

        if (gate != null)
        {
            gate.Open();
        }
    }

    void SetTextsVisible(bool visible)
    {
        if (displayText != null && displayText.gameObject.activeSelf != visible)
        {
            displayText.gameObject.SetActive(visible);
        }

        if (ruleText != null && ruleText.gameObject.activeSelf != visible)
        {
            ruleText.gameObject.SetActive(visible);
        }
    }

    void PlaySound(AudioClip clip)
    {
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, transform.position, soundVolume);
        }
    }
}