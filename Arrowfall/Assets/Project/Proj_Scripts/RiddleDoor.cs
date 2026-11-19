
using UnityEngine;

public class RiddleDoor: MonoBehaviour
{
    public BossChase bossChase;
    public BossAttack bossAttack;
    public BossHealthBar bossBar;

    public GameObject entranceBlocker;
    public AudioSource entranceSound;

    private bool fightStarted = false;

    void Start()
    {
        bossChase.enabled = false;
        bossAttack.enabled = false;

        if (entranceBlocker != null)
        {
            entranceBlocker.SetActive(false);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !fightStarted)
        {
            fightStarted = true;

            entranceBlocker.SetActive(true);

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
