using System.Collections;
using UnityEngine;
using TMPro;

// Houdt de hartjes van de speler bij, toont trefferfeedback, verwerkt gezondheidsverbeteringen en regelt het overlijden.
public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 5;
    public int currentHealth;
    public bool HealthUpgradePurchased { get; private set; }

    public GameObject GameOverUI;
    public TextMeshProUGUI healthText;

    public Color hitFlashColor = Color.red;
    public float hitFlashDuration = 0.12f;
    public float hitScaleMultiplier = 1.15f;

    private SpriteRenderer playerSprite;
    private Color normalColor;
    private Vector3 normalScale;
    private Coroutine hitEffectCoroutine;
    private float incomingDamageMultiplier = 1f;

    void Start()
    {
        PlayerAim aiming = GetComponent<PlayerAim>();
        playerSprite = aiming != null && aiming.VisualRenderer != null
            ? aiming.VisualRenderer : GetComponentInChildren<SpriteRenderer>();

        if (playerSprite != null)
        {
            normalColor = playerSprite.color;
            normalScale = playerSprite.transform.localScale;
        }
        currentHealth = maxHealth;
        GameOverUI.SetActive(false);
        UpdateHealthText();
    }

    public void TakeDamage(int damage)
    {
        int appliedDamage = Mathf.Max(1,
            Mathf.CeilToInt(Mathf.Max(0, damage) * incomingDamageMultiplier - 0.001f));
        currentHealth -= appliedDamage;
        GameAudio.Play(GameAudio.Cue.PlayerHit, 0.9f, Random.Range(0.94f, 1.04f));

        Debug.Log("Player Health: " + currentHealth);
        UpdateHealthText();

        if (hitEffectCoroutine != null)
        {
            StopCoroutine(hitEffectCoroutine);
        }

        hitEffectCoroutine = StartCoroutine(PlayHitEffect());

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private IEnumerator PlayHitEffect()
    {
        if (playerSprite != null)
        {
            playerSprite.color = hitFlashColor;
        }

        // Verander de grootte van de collider op het hoofdobject nooit tijdens het treffereffect.
        if (playerSprite != null && playerSprite.transform != transform)
            playerSprite.transform.localScale = new Vector3(
                normalScale.x * hitScaleMultiplier,
                normalScale.y * hitScaleMultiplier,
                normalScale.z);

        yield return new WaitForSeconds(hitFlashDuration);

        if (playerSprite != null)
        {
            playerSprite.color = normalColor;
        }

        if (playerSprite != null && playerSprite.transform != transform)
            playerSprite.transform.localScale = normalScale;
        hitEffectCoroutine = null;
    }

    public bool ApplyHealthUpgrade(int extraHearts)
    {
        if (HealthUpgradePurchased || currentHealth <= 0 || extraHearts <= 0) return false;
        HealthUpgradePurchased = true;
        maxHealth += extraHearts;
        currentHealth = maxHealth;
        UpdateHealthText();
        return true;
    }

    public void ApplyArmorPerk()
    {
        incomingDamageMultiplier = Mathf.Min(incomingDamageMultiplier, 0.5f);
    }

    public bool HealToFull()
    {
        if (currentHealth <= 0 || currentHealth >= maxHealth) return false;
        currentHealth = maxHealth;
        UpdateHealthText();
        return true;
    }

    private void UpdateHealthText()
    {
        int filledHearts = Mathf.Clamp(currentHealth, 0, maxHealth);
        int emptyHearts = maxHealth - filledHearts;

        healthText.text =
            "<color=#FF3B3B>" + new string('♥', filledHearts) + "</color>" +
            "<color=#4A4A4A>" + new string('♥', emptyHearts) + "</color>";
    }

    void Die()
    {
        Debug.Log("GAME OVER");
        GameAudio.Play(GameAudio.Cue.PlayerDeath);
        GameOverUI.SetActive(true);
        gameObject.SetActive(false);
    }
}
