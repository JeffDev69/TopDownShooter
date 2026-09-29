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
    private bool destroyedByHit;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        if (rb == null)
        {
            Debug.LogError("Bullet has no Rigidbody2D!");
            return;
        }

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.linearVelocity = transform.right * speed;
        foreach (Collider2D bulletCollider in GetComponentsInChildren<Collider2D>())
            bulletCollider.isTrigger = true;

        Destroy(gameObject, lifeTime);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        HandleHit(collision.collider);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        HandleHit(other);
    }

    private void HandleHit(Collider2D hitCollider)
    {
        if (destroyedByHit || hitCollider == null) return;
        GameObject hitObject = hitCollider.gameObject;

        // Do not destroy the bullet if it touches the player
        if (hitObject.CompareTag("Player"))
            return;

        // Enemy spit should not block player bullets.
        if (hitObject.GetComponentInParent<AcidProjectile>() != null)
            return;

        if (explosionRadius > 0f)
        {
            destroyedByHit = true;
            Explode(hitCollider);
            return;
        }

        EnemyHealth enemy = hitObject.GetComponentInParent<EnemyHealth>();
        if (enemy != null)
        {
            destroyedByHit = true;
            enemy.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        // Destroy bullet when it hits something else, like a wall
        destroyedByHit = true;
        Destroy(gameObject);
    }

    private void Explode(Collider2D impactCollider)
    {
        if (detonated) return;
        detonated = true;
        Vector2 origin = ExplosionOrigin(impactCollider);
        var visibleEnemies = new System.Collections.Generic.Dictionary<EnemyHealth, float>();
        foreach (Collider2D hit in Physics2D.OverlapCircleAll(origin, explosionRadius))
        {
            EnemyHealth enemy = hit.GetComponentInParent<EnemyHealth>();
            if (enemy == null) continue;
            Vector2 closestPoint = hit.ClosestPoint(origin);
            if (ExplosionBlocked(origin, closestPoint)) continue;

            float distance = Vector2.Distance(origin, closestPoint);
            if (!visibleEnemies.TryGetValue(enemy, out float nearestDistance) || distance < nearestDistance)
                visibleEnemies[enemy] = distance;
        }

        foreach (var target in visibleEnemies)
        {
            // Direct hits keep their punch; enemies at the blast edge take reduced splash damage.
            float falloff = Mathf.InverseLerp(explosionRadius * 0.25f, explosionRadius, target.Value);
            int splashDamage = Mathf.Max(1, Mathf.RoundToInt(explosionDamage * Mathf.Lerp(1f, 0.35f, falloff)));
            target.Key.TakeDamage(splashDamage);
        }
        WeaponExplosionEffect.Spawn(origin, explosionRadius);
        Destroy(gameObject);
    }

    private Vector2 ExplosionOrigin(Collider2D impactCollider)
    {
        Vector2 origin = transform.position;
        if (impactCollider == null || impactCollider.isTrigger ||
            impactCollider.GetComponentInParent<EnemyHealth>() != null) return origin;

        // A trigger projectile can already overlap the wall when the hit arrives. Put the
        // blast just outside its entry surface so the wall only shields its far side.
        Vector2 direction = transform.right;
        float backtrack = Vector2.Distance(origin, impactCollider.bounds.center) +
            impactCollider.bounds.extents.magnitude + 0.05f;
        foreach (RaycastHit2D hit in Physics2D.LinecastAll(origin - direction * backtrack, origin))
        {
            if (hit.collider == impactCollider)
                return hit.point - direction * 0.02f;
        }
        return origin;
    }

    private bool ExplosionBlocked(Vector2 origin, Vector2 target)
    {
        foreach (RaycastHit2D hit in Physics2D.LinecastAll(origin, target))
        {
            Collider2D collider = hit.collider;
            if (collider == null || collider.isTrigger ||
                collider.GetComponentInParent<EnemyHealth>() != null ||
                collider.GetComponentInParent<Bullet>() != null ||
                collider.GetComponentInParent<PlayerHealth>() != null) continue;
            return true;
        }
        return false;
    }
}
