using UnityEngine;

// Houdt afstand van de speler en schiet groene zuurprojectielen.
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

    [Header("Crowd Steering")]
    public float personalSpace = 1.1f;
    public float crowdLookRadius = 2.5f;
    public float separationStrength = 3.2f;
    public float laneStrength = 0.75f;
    public float passStrength = 1.1f;
    public float yieldStrength = 1.2f;
    public float velocitySmoothing = 10f;

    private Transform player;
    private Rigidbody2D body;
    private Collider2D[] ownColliders;
    private readonly EnemyMovement.ObstacleNavigator obstacleNavigator = new EnemyMovement.ObstacleNavigator();
    private float nextShotTime;
    private float laneSide;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        ownColliders = GetComponentsInChildren<Collider2D>();
        EnemyMovement normalMovement = GetComponent<EnemyMovement>();
        if (normalMovement != null) normalMovement.enabled = false;
    }

    void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null) player = playerObject.transform;
        nextShotTime = Time.time + Random.Range(0.4f, Mathf.Max(0.5f, shootInterval));
        laneSide = (Mathf.Abs(gameObject.GetHashCode()) & 1) == 0 ? 1f : -1f;
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

        Vector2 desiredDirection = Vector2.zero;
        float desiredSpeed = 0f;
        if (distance > preferredDistance)
        {
            desiredDirection = direction;
            desiredSpeed = moveSpeed;
        }
        else if (distance < retreatDistance)
        {
            desiredDirection = -direction;
            desiredSpeed = moveSpeed;
        }

        if (desiredDirection.sqrMagnitude > 0.001f)
            desiredDirection = obstacleNavigator.GetDirection(transform.position,
                (Vector2)transform.position + desiredDirection * preferredDistance,
                CurrentPersonalSpace * 0.5f);

        Vector2 crowdForward = desiredDirection.sqrMagnitude > 0.001f
            ? desiredDirection.normalized
            : direction;
        float speedScale;
        Vector2 crowdSteering = EnemyMovement.CalculateCrowdSteering(
            gameObject,
            body,
            ownColliders,
            transform.position,
            crowdForward,
            moveSpeed,
            CurrentPersonalSpace,
            crowdLookRadius,
            separationStrength,
            laneSide,
            laneStrength,
            passStrength,
            yieldStrength,
            out speedScale);

        if (desiredDirection.sqrMagnitude > 0.001f)
        {
            desiredDirection += crowdSteering;
        }
        else
        {
            desiredDirection = crowdSteering;
            desiredSpeed = crowdSteering.sqrMagnitude > 0.001f ? moveSpeed * 0.75f : 0f;
        }

        if (desiredDirection.sqrMagnitude > 0.001f)
            desiredDirection.Normalize();

        Vector2 targetVelocity = desiredDirection * desiredSpeed * speedScale;
        float smoothing = 1f - Mathf.Exp(-Mathf.Max(0f, velocitySmoothing) * Time.fixedDeltaTime);
        body.linearVelocity = Vector2.Lerp(body.linearVelocity, targetVelocity, smoothing);
        body.angularVelocity = 0f;

        if (direction != Vector2.zero)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            body.SetRotation(angle + spriteAngleOffset);
        }
    }

    public float CurrentCrowdSpeed => moveSpeed;

    public float CurrentPersonalSpace =>
        personalSpace * Mathf.Max(1f, transform.lossyScale.x, transform.lossyScale.y);
}
