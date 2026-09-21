using UnityEngine;

// Regelt bullet movement, doet damage bij een zombie-hit en verwijdert daarna de projectile.
public class Bullet : MonoBehaviour
{
    public float speed = 5f;
    public float lifeTime = 3f;
    public int damage = 1;

    private Rigidbody2D rb;

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

        EnemyHealth enemy = collision.gameObject.GetComponent<EnemyHealth>();

        if (enemy != null)
        {
            enemy.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        // Destroy bullet when it hits something else, like a wall
        Destroy(gameObject);
    }
}
