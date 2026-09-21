using UnityEngine;

// Maakt iedere vijfde round boss groter, sterker en sneller wanneer hij bijna dood is.
[RequireComponent(typeof(EnemyHealth))]
public class BossZombie : MonoBehaviour
{
    public int bonusHealth = 30;
    public int bonusCoins = 90;
    public float sizeMultiplier = 1.5f;
    public float enragedSpeedMultiplier = 1.35f;
    public int contactDamage = 2;

    private EnemyHealth health;
    private EnemyMovement movement;
    private int startingHealth;
    private bool enraged;

    void Awake()
    {
        health = GetComponent<EnemyHealth>();
        movement = GetComponent<EnemyMovement>();
        health.health += Mathf.Max(0, bonusHealth);
        health.coinsOnDeath += Mathf.Max(0, bonusCoins);
        transform.localScale *= Mathf.Max(1f, sizeMultiplier);

        if (movement != null)
            movement.damage = Mathf.Max(movement.damage, contactDamage);
    }

    void Start()
    {
        // Start runs after de spawner zijn round health heeft toegevoegd.
        startingHealth = health.health;
    }

    void Update()
    {
        if (enraged || health == null || health.IsDead || health.CurrentHealth > startingHealth / 2) return;
        enraged = true;
        if (movement != null) movement.moveSpeed *= Mathf.Max(1f, enragedSpeedMultiplier);
    }
}
