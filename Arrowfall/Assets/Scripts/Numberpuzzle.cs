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
    public GameObject panel;
    public Image panelImage;
    public TMP_Text displayText;
    public Image ruleBadge;
    public TMP_Text ruleText;
    public GameObject timerBar;
    public Image timerFill;
    public TMP_Text progressText;
    public TMP_Text messageText;

    [Header("Text")]
    public string instruction = "HIT";

    [Header("Colors")]
    public Color panelColor = new Color(0f, 0f, 0f, 0f);
    public Color correctColor = new Color(0.2f, 0.75f, 0.3f, 0.85f);
    public Color wrongColor = new Color(0.85f, 0.15f, 0.15f, 0.85f);
    public Color timerColor = Color.white;
    public Color timerLowColor = new Color(0.9f, 0.2f, 0.2f);

    [Header("Start Condition")]
    public bool waitForGuards = true;
    public EnemyHealth[] guards;
    public string guardsClearedMessage = "Outside guards eliminated";
    public float messageTime = 2.5f;
    public int startCountdown = 3;

    [Header("Enemies During Puzzle")]
    public EnemySpawner spawner;
    public bool spawnDuringWarmup = false;

    [Header("Visibility")]
    public float showDistance = 25f;

    [Header("Rounds")]
    public int warmupRounds = 2;
    public int mainRounds = 6;
    public int maxMisses = 3;
    [Range(2, 4)]
    public int highestNumber = 4;

    [Header("Difficulty")]
    public float startRoundTime = 6f;
    public float endRoundTime = 2.5f;
    public int shuffleFromRound = 4;
    public bool timeoutCountsAsMiss = true;

    [Header("Timing")]
    public float pauseBetweenRounds = 0.8f;

    [Header("Sound")]
    public AudioClip correctSound;
    public AudioClip wrongSound;
    public AudioClip solvedSound;
    [Range(0f, 1f)]
    public float soundVolume = 1f;

    private Transform player;
    private bool panelWanted = false;
    private float forceShowUntil = 0f;

    private bool acceptingHits = false;
    private Result pendingResult = Result.None;
    private int answer;
    private int lastNumber = 0;

    private bool inWarmup = true;
    private int warmupDone = 0;
    private int mainDone = 0;
    private int misses = 0;

    private NumberTarget[] targets;
    private int[] originalNumbers;

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

    IEnumerator RunPuzzle()
    {
        if (waitForGuards && guards.Length > 0)
        {
            while (GuardsAlive() > 0)
            {
                yield return null;
            }

            panelWanted = true;
            SetText(displayText, "");
            SetText(messageText, guardsClearedMessage);
            forceShowUntil = Time.time + messageTime;
            yield return new WaitForSeconds(messageTime);
        }

        panelWanted = true;

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

        Solve();
    }

    IEnumerator WaitForResult(float roundTime)
    {
        pendingResult = Result.None;
        acceptingHits = true;
        float timeLeft = roundTime;

        while (pendingResult == Result.None)
        {
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
    }

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

    public void TargetHit(NumberTarget target)
    {
        if (!acceptingHits || pendingResult != Result.None)
        {
            return;
        }

        pendingResult = (target.number == answer) ? Result.Correct : Result.Wrong;
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

    void Solve()
    {
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
        }
    }

    bool IsPlayerNear()
    {
        if (player == null)
        {
            return true;
        }
        return Vector3.Distance(player.position, transform.position) <= showDistance;
    }

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

}