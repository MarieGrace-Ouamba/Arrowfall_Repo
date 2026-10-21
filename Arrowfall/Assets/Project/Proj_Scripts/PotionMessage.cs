using System.Collections;
using TMPro;
using UnityEngine;

public class PotionMessage : MonoBehaviour
{
    public TMP_Text PotionText;
    public string message = "Take your healing potions";
    public float showSeconds = 4f;
    public bool showOnStart = true;

    void Start()
    {
        if (PotionText != null) PotionText.gameObject.SetActive(false);
        if (showOnStart) Show();
    }

    public void Show()
    {
        StopAllCoroutines();
        StartCoroutine(ShowRoutine());
    }

    IEnumerator ShowRoutine()
    {
        PotionText.text = message;
        PotionText.gameObject.SetActive(true);
        yield return new WaitForSeconds(showSeconds);
        PotionText.gameObject.SetActive(false);
    }
}