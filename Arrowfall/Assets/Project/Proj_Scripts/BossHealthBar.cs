//Marie-Grace Ouamba
using UnityEngine;
using UnityEngine.UI;

public class BossHealthBar : MonoBehaviour
{
    public BossHealth boss;
    public Slider slider;

    void Awake()
    {
        if (slider == null)
        {
            slider = GetComponent<Slider>();
        }

        gameObject.SetActive(false);
    }

    public void Show()
    {
        if (boss != null && !boss.IsDead())
        {
            gameObject.SetActive(true);

            slider.maxValue = boss.GetMaxHealth();
            slider.value = boss.GetHealth();
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (boss != null && slider != null)
        {
            slider.value = boss.GetHealth(); //Health changes whenever the boss takes damage.

            if (boss.IsDead())
            {
                Hide();
            }
        }
    }
}
