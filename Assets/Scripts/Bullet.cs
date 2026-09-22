using UnityEngine;

// Regelt bullet movement, doet damage bij een zombie-hit en verwijdert daarna de projectile.
public class Bullet : MonoBehaviour
{
    public float speed = 5f;
    public float lifeTime = 3f;
    public int damage = 1;
    [HideInInspector] public float explosionRadius;
    [HideInInspector] public int explosionDamage;

    private Rigidbody2D rb;
    private bool detonated;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        if (rb == null)
        {
            Debug.LogError("Bullet has no Rigidbody2D!");
            return;
        }

        rb.linearVelocity = transform.right * speed;

        Destroy(gameObject, lifeTime);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Do not destroy the bullet if it touches the player
        if (collision.gameObject.CompareTag("Player"))
        {
            return;
        }

        if (explosionRadius > 0f)
        {
            Explode();
            return;
        }

        EnemyHealth enemy = collision.gameObject.GetComponentInParent<EnemyHealth>();

        if (enemy != null)
        {
            enemy.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        // Destroy bullet when it hits something else, like a wall
        Destroy(gameObject);
    }

    private void Explode()
    {
        if (detonated) return;
        detonated = true;
        var damagedEnemies = new System.Collections.Generic.HashSet<EnemyHealth>();
        foreach (Collider2D hit in Physics2D.OverlapCircleAll(transform.position, explosionRadius))
        {
            EnemyHealth enemy = hit.GetComponentInParent<EnemyHealth>();
            if (enemy == null || !damagedEnemies.Add(enemy)) continue;
            enemy.TakeDamage(Mathf.Max(1, explosionDamage));
        }
        WeaponExplosionEffect.Spawn(transform.position, explosionRadius);
        Destroy(gameObject);
    }
}
