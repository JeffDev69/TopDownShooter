using System.Collections;
using UnityEngine;

// Regelt zombie health, hit feedback, death rewards, corpses en random power-up drops.
public class EnemyHealth : MonoBehaviour
{
    public int health = 3;
    [Range(0.05f, 10f)] public float incomingDamageMultiplier = 1f;
    public Color hitFlashColor = Color.red;
    public float hitFlashDuration = 0.1f;
    public ParticleSystem bloodEffect;
    public int coinsOnDeath = 10;

    [Header("Blood Pool")]
    public float bloodPoolWidth = 1.8f;
    public float bloodPoolDuration = 6f;
    public float bloodPoolFadeDuration = 1.5f;
    public float deathCleanupDelay = 0.3f;

    [Header("Power-Up Drops")]
    [Range(0f, 1f)] public float maxAmmoDropChance = 0.08f;
    [Range(0f, 1f)] public float instaKillDropChance = 0.04f;
    [Range(0f, 1f)] public float nukeDropChance = 0.02f;

    private SpriteRenderer spriteRenderer;
    private Color normalColor;
    private bool isDead;
    private EnemySpawner roundOwner;
    private static Sprite cachedBloodPoolSprite;

    public int CurrentHealth => health;
    public bool IsDead => isDead;
    public event System.Action<EnemyHealth> Died;

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

        if (damage > 0 && PlayerShoot.InstaKillActive)
            health = 0;
        else
            health -= Mathf.Max(1, Mathf.CeilToInt(damage * incomingDamageMultiplier));
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
        StartCoroutine(Die(false, false));
    }

    public void KillFromEnemyAbility()
    {
        if (isDead) return;
        health = 0;
        StopAllCoroutines();
        SpawnBlood();
        StartCoroutine(Die(false, true));
    }

    private IEnumerator Die(bool allowPowerUpDrop = true, bool triggerDeathEffects = true)
    {
        isDead = true;

        // Special zombies mogen hier hun eigen death effect uitvoeren.
        if (triggerDeathEffects)
            Died?.Invoke(this);

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

        SpawnBloodPool();

        // Het oude corpse verdwijnt; de losse blood pool blijft zelfstandig liggen.
        if (spriteRenderer != null)
            spriteRenderer.enabled = false;

        yield return new WaitForSeconds(Mathf.Max(0f, deathCleanupDelay));

        Destroy(gameObject);
    }

    private void SpawnBloodPool()
    {
        if (cachedBloodPoolSprite == null)
            cachedBloodPoolSprite = Resources.Load<Sprite>("BloodPool");
        if (cachedBloodPoolSprite == null)
        {
            Debug.LogWarning("BloodPool sprite could not be loaded from Assets/Resources.", this);
            return;
        }

        GameObject pool = new GameObject("BloodPool");
        pool.transform.position = transform.position;
        pool.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

        SpriteRenderer poolRenderer = pool.AddComponent<SpriteRenderer>();
        poolRenderer.sprite = cachedBloodPoolSprite;
        if (spriteRenderer != null)
        {
            poolRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
            poolRenderer.sortingOrder = spriteRenderer.sortingOrder - 1;
        }

        float spriteWidth = Mathf.Max(0.01f, cachedBloodPoolSprite.bounds.size.x);
        float zombieSize = Mathf.Clamp(Mathf.Max(transform.lossyScale.x, transform.lossyScale.y), 1f, 1.6f);
        float scale = Mathf.Max(0.2f, bloodPoolWidth) * zombieSize / spriteWidth;
        pool.transform.localScale = Vector3.one * scale * Random.Range(0.9f, 1.1f);

        BloodPoolFade fade = pool.AddComponent<BloodPoolFade>();
        fade.Initialize(poolRenderer, bloodPoolDuration, bloodPoolFadeDuration);
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
