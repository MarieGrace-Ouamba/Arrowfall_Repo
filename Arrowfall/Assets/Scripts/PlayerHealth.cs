// PlayerHealth.cs
// Rayan Alduhaiman
// IT 485/585 - ArrowFall
// Tracks the player's health and shows it on a UI slider.
// The bar fades from green at full health, through yellow, to red as health runs out.
// Other scripts (enemies, traps) call TakeDamage() to hurt the player.

using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;

    [Header("UI")]
    public Slider healthBar;               // drag the health bar slider here
    public Image fillImage;                // drag the slider's Fill object here
    public GameOverScreen gameOverScreen;  // drag the Canvas (it has GameOverScreen) here

    [Header("Bar colors")]
    public Color fullColor = Color.green;
    public Color halfColor = Color.yellow;
    public Color emptyColor = Color.red;

    [Header("Testing")]
    public bool allowTestKeys = true;      // K hurts, H heals. Turn off for the final build.

    private int currentHealth;
    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;

        if (healthBar != null)
        {
            healthBar.minValue = 0;
            healthBar.maxValue = maxHealth;
            healthBar.value = currentHealth;
            healthBar.interactable = false;   // display only, the player cannot drag it
        }

        UpdateBar();
    }

    void Update()
    {
        if (!allowTestKeys)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            TakeDamage(10);
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            Heal(10);
        }
    }

    // Called by anything that hurts the player.
    public void TakeDamage(int amount)
    {
        if (isDead)
        {
            return;
        }

        currentHealth = Mathf.Max(currentHealth - amount, 0);
        UpdateBar();

        if (currentHealth == 0)
        {
            Die();
        }
    }

    // Called by pickups or anything that restores health.
    public void Heal(int amount)
    {
        if (isDead)
        {
            return;
        }

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        UpdateBar();
    }

    public int GetHealth()
    {
        return currentHealth;
    }

    void UpdateBar()
    {
        if (healthBar != null)
        {
            healthBar.value = currentHealth;
        }

        if (fillImage != null)
        {
            // 1 at full health, 0 when dead.
            float t = (float)currentHealth / maxHealth;

            // Top half fades green to yellow, bottom half fades yellow to red.
            if (t > 0.5f)
            {
                fillImage.color = Color.Lerp(halfColor, fullColor, (t - 0.5f) * 2f);
            }
            else
            {
                fillImage.color = Color.Lerp(emptyColor, halfColor, t * 2f);
            }
        }
    }

    // Stops the player and shows the game over screen.
    void Die()
    {
        isDead = true;
        Debug.Log("Player died");

        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null)
        {
            movement.enabled = false;
        }

        PlayerLook look = GetComponentInChildren<PlayerLook>();
        if (look != null)
        {
            look.enabled = false;
        }

        BowShoot bow = GetComponentInChildren<BowShoot>();
        if (bow != null)
        {
            bow.enabled = false;
        }

        if (gameOverScreen != null)
        {
            gameOverScreen.Show();
        }
    }
}