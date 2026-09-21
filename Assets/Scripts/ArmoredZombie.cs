using UnityEngine;

// Geeft de armored zombie extra health en reduceert incoming damage.
[RequireComponent(typeof(EnemyHealth))]
public class ArmoredZombie : MonoBehaviour
{
    public int bonusHealth = 6;
    public int bonusCoins = 15;
    [Range(0.05f, 1f)] public float damageTakenMultiplier = 0.5f;

    void Awake()
    {
        EnemyHealth health = GetComponent<EnemyHealth>();
        health.health += Mathf.Max(0, bonusHealth);
        health.coinsOnDeath += Mathf.Max(0, bonusCoins);
        health.incomingDamageMultiplier = Mathf.Clamp(damageTakenMultiplier, 0.05f, 1f);
    }
}
