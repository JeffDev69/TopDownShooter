using UnityEngine;

// Maakt de baas van elke vijfde ronde groter, sterker en sneller wanneer hij bijna verslagen is.
[RequireComponent(typeof(EnemyHealth))]
public class BossZombie : MonoBehaviour
{
    public int bonusHealth = 120;
    public int minimumHealth = 160;
    public int bonusCoins = 90;
    public float sizeMultiplier = 1.65f;
    [Range(0.05f, 1f)] public float damageTakenMultiplier = 0.55f;
    public float enragedSpeedMultiplier = 1.55f;
    public int contactDamage = 3;
    public int minionsAtSecondPhase = 4;
    public float finalPhaseSpeedMultiplier = 1.35f;
    public int finalPhaseExtraDamage = 2;
    private EnemyHealth health;
    private EnemyMovement movement;
    private int startingHealth;
    private bool phaseOne;
    private bool phaseTwo;
    private bool phaseThree;

    void Awake()
    {
        health = GetComponent<EnemyHealth>();
        movement = GetComponent<EnemyMovement>();
        health.health += Mathf.Max(0, bonusHealth);
        health.health = Mathf.Max(health.health, Mathf.Max(1, minimumHealth));
        health.coinsOnDeath += Mathf.Max(0, bonusCoins);
        health.incomingDamageMultiplier = Mathf.Min(
            health.incomingDamageMultiplier,
            Mathf.Clamp(damageTakenMultiplier, 0.05f, 1f));
        health.immuneToInstaKill = true;
        health.immuneToNuke = true;
        transform.localScale *= Mathf.Max(1f, sizeMultiplier);

        if (movement != null)
            movement.damage = Mathf.Max(movement.damage, contactDamage);
    }

    void Start()
    {
        // Start draait nadat de spawner de extra gezondheid voor de ronde heeft toegevoegd.
        startingHealth = health.health;
        if (GetComponent<BossHealthBar>() == null)
            gameObject.AddComponent<BossHealthBar>();
    }

    void Update()
    {
        if (health == null || health.IsDead || startingHealth <= 0) return;

        float healthPercent = (float)health.CurrentHealth / startingHealth;

        if (!phaseOne && healthPercent <= 0.7f)
        {
            phaseOne = true;
            if (movement != null)
                movement.moveSpeed *= Mathf.Max(1f, enragedSpeedMultiplier);
            health.SetVisualColor(new Color(1f, 0.85f, 0.55f, 1f));
        }

        if (!phaseTwo && healthPercent <= 0.4f)
        {
            phaseTwo = true;
            if (EnemySpawner.ActiveSpawner != null)
                EnemySpawner.ActiveSpawner.SpawnBossMinions(transform.position, minionsAtSecondPhase);
            if (movement != null)
                movement.damage = Mathf.Max(movement.damage, contactDamage + 1);
            health.SetVisualColor(new Color(1f, 0.45f, 0.35f, 1f));
        }

        if (!phaseThree && healthPercent <= 0.15f)
        {
            phaseThree = true;
            if (movement != null)
            {
                movement.moveSpeed *= Mathf.Max(1f, finalPhaseSpeedMultiplier);
                movement.damage = Mathf.Max(movement.damage, contactDamage + Mathf.Max(0, finalPhaseExtraDamage));
            }
            health.incomingDamageMultiplier = Mathf.Min(health.incomingDamageMultiplier, 0.45f);
            health.SetVisualColor(new Color(1f, 0.2f, 0.2f, 1f));
        }
    }
}
