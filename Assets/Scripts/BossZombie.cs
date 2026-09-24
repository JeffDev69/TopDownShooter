using UnityEngine;

// Maakt iedere vijfde round boss groter, sterker en sneller wanneer hij bijna dood is.
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
    private EnemyHealth health;
    private EnemyMovement movement;
    private int startingHealth;
    private bool enraged;

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
        // Start runs after de spawner zijn round health heeft toegevoegd.
        startingHealth = health.health;
        if (GetComponent<BossHealthBar>() == null)
            gameObject.AddComponent<BossHealthBar>();
    }

    void Update()
    {
        if (enraged || health == null || health.IsDead || health.CurrentHealth > startingHealth / 2) return;
        enraged = true;
        if (movement != null) movement.moveSpeed *= Mathf.Max(1f, enragedSpeedMultiplier);
    }
}
