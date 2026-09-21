using UnityEngine;

// Houdt afstand van de player en schiet green acid projectiles.
[RequireComponent(typeof(Rigidbody2D))]
public class SpitterZombie : MonoBehaviour
{
    public float moveSpeed = 2.2f;
    public float preferredDistance = 4f;
    public float retreatDistance = 2.2f;
    public float shootRange = 7f;
    public float shootInterval = 2f;
    public float projectileSpeed = 6f;
    public int projectileDamage = 1;
    public float spriteAngleOffset = 90f;

    private Transform player;
    private Rigidbody2D body;
    private float nextShotTime;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        EnemyMovement normalMovement = GetComponent<EnemyMovement>();
        if (normalMovement != null) normalMovement.enabled = false;
    }

    void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null) player = playerObject.transform;
        nextShotTime = Time.time + Random.Range(0.4f, Mathf.Max(0.5f, shootInterval));
    }

    void Update()
    {
        if (player == null) return;
        Vector2 toPlayer = player.position - transform.position;
        if (toPlayer.sqrMagnitude <= shootRange * shootRange && Time.time >= nextShotTime)
        {
            AcidProjectile.Spawn(transform.position, toPlayer.normalized, projectileSpeed, projectileDamage);
            nextShotTime = Time.time + Mathf.Max(0.25f, shootInterval);
        }
    }

    void FixedUpdate()
    {
        if (player == null || body == null) return;
        Vector2 toPlayer = player.position - transform.position;
        float distance = toPlayer.magnitude;
        Vector2 direction = distance > 0.01f ? toPlayer / distance : Vector2.zero;

        if (distance > preferredDistance)
            body.linearVelocity = direction * moveSpeed;
        else if (distance < retreatDistance)
            body.linearVelocity = -direction * moveSpeed;
        else
            body.linearVelocity = Vector2.zero;

        if (direction != Vector2.zero)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            body.SetRotation(angle + spriteAngleOffset);
        }
    }
}
