//Marie-Grace Ouamba
using UnityEngine;

public class ArenaFight : MonoBehaviour
{
    public BossChase bossChase;
    public BossAttack bossAttack;
    public BossHealthBar bossBar;
    public GameObject entranceBlocker;

    public AudioSource entrancesound;

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
        if (other.CompareTag("Player") && !fightStarted) // *Create a unity layer
        {
            fightStarted = true;

            bossChase.enabled = true;
            bossAttack.enabled = true;
            bossBar.Show();

            if (entranceBlocker != null)
            {
                entranceBlocker.SetActive(true);
            }
        }
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
