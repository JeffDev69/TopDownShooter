using UnityEngine;

// Laat de zombie de player chasen en damage doen zolang er contact is.
public class EnemyMovement : MonoBehaviour
{
    public float moveSpeed = 2f;
    public int damage = 1;
    public float damageCooldown = 1f;
    public float spriteAngleOffset = 90f;

    private Transform player;
    private Rigidbody2D rb;

    private float nextDamageTime;

    void Start()
    {
        GameObject playerObject =
            GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;
        }

        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        if (player == null || rb == null)
        {
            return;
        }

        Vector2 direction =
            (player.position - transform.position).normalized;

        rb.linearVelocity =
            direction * moveSpeed;

        float angle =
            Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        rb.SetRotation(angle + spriteAngleOffset);
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        PlayerHealth playerHealth =
            collision.gameObject.GetComponent<PlayerHealth>();

        if (
            playerHealth != null &&
            Time.time >= nextDamageTime
        )
        {
            playerHealth.TakeDamage(damage);

            nextDamageTime =
                Time.time + damageCooldown;
        }
    }
}
