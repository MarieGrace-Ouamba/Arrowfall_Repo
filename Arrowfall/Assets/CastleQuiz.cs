using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CastleQuiz : MonoBehaviour
{
    public TMP_Text questionText;
    public Button[] answerButtons = new Button[3];

    [Header("Object revealed when the player passes")]
    public GameObject key2;

    [Header("Movement, camera and shooting scripts")]
    public MonoBehaviour[] controlsToDisable;

    private class Question
    {
        public string text;
        public string[] answers;
        public int correct;

        public Question(string text, string a, string b,
                        string c, int correct)
        {
            this.text = text;
            answers = new string[] { a, b, c };
            this.correct = correct;
        }
    }

    // Correct answer: 0 = A, 1 = B, 2 = C.
    private Question[] questions =
    {
        new Question("Which planet is known as the Red Planet?",
            "Venus", "Mars", "Jupiter", 1),

        new Question("How many sides does a hexagon have?",
            "Five", "Eight", "Six", 2),

        new Question("What is the capital of Norway?",
            "Oslo", "Stockholm", "Copenhagen", 0),

        new Question("Which ocean is the largest?",
            "Atlantic", "Pacific", "Indian", 1),

        new Question("What is the chemical symbol for gold?",
            "Ag", "Fe", "Au", 2)
    };

    private int[] order;
    private int currentQuestion;
    private int correctAnswers;
    private bool finished;
    private bool sessionActive;

    private float previousTimeScale;
    private CursorLockMode previousCursorLock;
    private bool previousCursorVisible;
    private bool[] previousControlStates;

    void Awake()
    {
        for (int i = 0; i < answerButtons.Length; i++)
        {
            int answerIndex = i;
            answerButtons[i].onClick.AddListener(
                () => Answer(answerIndex));
        }
    }

    void OnEnable()
    {
        currentQuestion = 0;
        correctAnswers = 0;
        finished = false;

        if (key2 != null)
            key2.SetActive(false);

        // Shuffle question indices without repetitions.
        order = new int[questions.Length];

        for (int i = 0; i < order.Length; i++)
            order[i] = i;

        for (int i = order.Length - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            int temp = order[i];
            order[i] = order[randomIndex];
            order[randomIndex] = temp;
        }

        previousTimeScale = Time.timeScale;
        previousCursorLock = Cursor.lockState;
        previousCursorVisible = Cursor.visible;

        previousControlStates =
            new bool[controlsToDisable.Length];

        for (int i = 0; i < controlsToDisable.Length; i++)
        {
            MonoBehaviour control = controlsToDisable[i];

            if (control == null || control == this)
                continue;

            previousControlStates[i] = control.enabled;
            control.enabled = false;
        }

        sessionActive = true;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        ShowQuestion();
    }

    void ShowQuestion()
    {
        Question question = questions[order[currentQuestion]];

        questionText.text =
            $"Question {currentQuestion + 1} / 5\n" +
            question.text;

        for (int i = 0; i < answerButtons.Length; i++)
        {
            answerButtons[i].gameObject.SetActive(true);
            answerButtons[i].GetComponentInChildren<TMP_Text>().text =
                question.answers[i];
        }
    }

    void Answer(int answerIndex)
    {
        if (finished)
        {
            gameObject.SetActive(false);
            return;
        }

        Question question = questions[order[currentQuestion]];

        if (answerIndex == question.correct)
            correctAnswers++;

        currentQuestion++;

        if (currentQuestion < 5)
            ShowQuestion();
        else
            ShowResult();
    }

    void ShowResult()
    {
        finished = true;
        bool passed = correctAnswers >= 3;

        questionText.text = passed
            ? $"You scored {correctAnswers}/5!\n" +
              "Collect Key 2 to unlock Door 2."
            : $"You scored {correctAnswers}/5.\n" +
              "You need at least 3 correct answers.";

        if (passed && key2 != null)
            key2.SetActive(true);

        answerButtons[0].GetComponentInChildren<TMP_Text>().text =
            "Continue";

        answerButtons[1].gameObject.SetActive(false);
        answerButtons[2].gameObject.SetActive(false);
    }

    void OnDisable()
    {
        if (!sessionActive)
            return;

        sessionActive = false;
        Time.timeScale = previousTimeScale;
        Cursor.lockState = previousCursorLock;
        Cursor.visible = previousCursorVisible;

        for (int i = 0; i < controlsToDisable.Length; i++)
        {
            if (controlsToDisable[i] != null &&
                controlsToDisable[i] != this)
            {
                controlsToDisable[i].enabled =
                    previousControlStates[i];
            }
        }
    }
}