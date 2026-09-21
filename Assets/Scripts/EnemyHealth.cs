using System.Collections;
using UnityEngine;

// Regelt zombie health, hit feedback, death rewards, corpses en random power-up drops.
public class EnemyHealth : MonoBehaviour
{
    public int health = 3;
    public Color hitFlashColor = Color.red;
    public float hitFlashDuration = 0.1f;
    public ParticleSystem bloodEffect;
    public Sprite corpseSprite;
    public float corpseDuration = 2f;
    public float corpseFadeDuration = 0.75f;
    public int coinsOnDeath = 10;

    [Header("Power-Up Drops")]
    [Range(0f, 1f)] public float maxAmmoDropChance = 0.08f;
    [Range(0f, 1f)] public float instaKillDropChance = 0.04f;
    [Range(0f, 1f)] public float nukeDropChance = 0.02f;

    private SpriteRenderer spriteRenderer;
    private Color normalColor;
    private bool isDead;
    private EnemySpawner roundOwner;

    public void SetRoundOwner(EnemySpawner owner)
    {
        roundOwner = owner;
    }

    void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            normalColor = spriteRenderer.color;
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead)
        {
            return;
        }

        health = damage > 0 && PlayerShoot.InstaKillActive ? 0 : health - damage;
        SpawnBlood();

        if (health <= 0)
        {
            StopAllCoroutines();
            StartCoroutine(Die());
        }
        else if (spriteRenderer != null)
        {
            StopAllCoroutines();
            StartCoroutine(FlashWhenHit());
        }
    }

    private IEnumerator FlashWhenHit()
    {
        spriteRenderer.color = hitFlashColor;
        yield return new WaitForSeconds(hitFlashDuration);
        spriteRenderer.color = normalColor;
    }

    public void KillFromNuke()
    {
        if (isDead) return;
        health = 0;
        StopAllCoroutines();
        SpawnBlood();
        // Nuke kills still award coins and count toward the round, but do not drop more power-ups.
        StartCoroutine(Die(false));
    }

    private IEnumerator Die(bool allowPowerUpDrop = true)
    {
        isDead = true;

        if (roundOwner != null)
            roundOwner.RegisterKill();

        if (CoinManager.Instance != null)
            CoinManager.Instance.AddCoins(coinsOnDeath);

        if (allowPowerUpDrop)
        {
            float roll = Random.value;
            float maxAmmoChance = Mathf.Clamp01(maxAmmoDropChance);
            float instaKillChance = Mathf.Clamp01(instaKillDropChance);
            float nukeChance = Mathf.Clamp01(nukeDropChance);
            if (roll < maxAmmoChance)
                MaxAmmoPickup.Spawn(transform.position);
            else if (roll < maxAmmoChance + instaKillChance)
                MaxAmmoPickup.Spawn(transform.position, MaxAmmoPickup.PowerUpType.InstaKill);
            else if (roll < maxAmmoChance + instaKillChance + nukeChance)
                MaxAmmoPickup.Spawn(transform.position, MaxAmmoPickup.PowerUpType.Nuke);
        }

        EnemyMovement movement = GetComponent<EnemyMovement>();

        if (movement != null)
        {
            movement.enabled = false;
        }

        Collider2D enemyCollider = GetComponent<Collider2D>();

        if (enemyCollider != null)
        {
            enemyCollider.enabled = false;
        }

        Rigidbody2D enemyBody = GetComponent<Rigidbody2D>();

        if (enemyBody != null)
        {
            enemyBody.linearVelocity = Vector2.zero;
            enemyBody.angularVelocity = 0f;
            enemyBody.simulated = false;
        }

        if (spriteRenderer != null)
        {
            if (corpseSprite != null)
            {
                spriteRenderer.sprite = corpseSprite;
            }

            spriteRenderer.color = Color.white;
        }

        yield return new WaitForSeconds(corpseDuration);

        if (spriteRenderer != null && corpseFadeDuration > 0f)
        {
            float elapsed = 0f;
            Color startColor = spriteRenderer.color;

            while (elapsed < corpseFadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = 1f - Mathf.Clamp01(elapsed / corpseFadeDuration);
                spriteRenderer.color = new Color(
                    startColor.r,
                    startColor.g,
                    startColor.b,
                    alpha
                );
                yield return null;
            }
        }

        Destroy(gameObject);
    }

    private void SpawnBlood()
    {
        if (bloodEffect == null)
        {
            return;
        }

        ParticleSystem effect = Instantiate(
            bloodEffect,
            transform.position,
            Quaternion.identity
        );

        ParticleSystem.MainModule main = effect.main;
        Destroy(
            effect.gameObject,
            main.duration + main.startLifetime.constantMax
        );
    }
}
