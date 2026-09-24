using UnityEngine;

// Laat de zombie de player chasen en damage doen zolang er contact is.
public class EnemyMovement : MonoBehaviour
{
    public float moveSpeed = 2f;
    public int damage = 1;
    public float damageCooldown = 1f;
    public float spriteAngleOffset = 90f;

    [Header("Crowd Steering")]
    public float personalSpace = 0.45f;
    public float crowdLookRadius = 1.25f;
    public float separationStrength = 0.9f;
    public float laneStrength = 0.25f;
    public float passStrength = 0.35f;
    public float yieldStrength = 0.25f;
    public float velocitySmoothing = 18f;

    private const int MaxCrowdHits = 24;
    private static readonly Collider2D[] CrowdHits = new Collider2D[MaxCrowdHits];
    private static readonly ContactFilter2D CrowdContactFilter = new ContactFilter2D
    {
        useTriggers = false
    };

    private Transform player;
    private Rigidbody2D rb;
    private Collider2D[] ownColliders;

    private float nextDamageTime;
    private float laneSide;

    void Start()
    {
        GameObject playerObject =
            GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;
        }

        rb = GetComponent<Rigidbody2D>();
        ownColliders = GetComponentsInChildren<Collider2D>();
        int stableSeed = Mathf.Abs(gameObject.GetHashCode());
        laneSide = StableLaneSide(stableSeed);
    }

    void FixedUpdate()
    {
        if (player == null || rb == null)
        {
            return;
        }

        Vector2 toPlayer =
            player.position - transform.position;
        float distanceToPlayer = toPlayer.magnitude;
        Vector2 direction =
            distanceToPlayer > 0.01f ? toPlayer / distanceToPlayer : Vector2.zero;

        if (direction == Vector2.zero)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float speedScale;
        Vector2 crowdSteering = CalculateCrowdSteering(
            gameObject,
            rb,
            ownColliders,
            transform.position,
            direction,
            moveSpeed,
            CurrentPersonalSpace,
            crowdLookRadius,
            separationStrength,
            laneSide,
            laneStrength,
            passStrength,
            yieldStrength,
            out speedScale);

        Vector2 moveDirection = direction + crowdSteering;

        if (moveDirection.sqrMagnitude < 0.001f)
            moveDirection = direction;
        else
        {
            moveDirection.Normalize();
            if (Vector2.Dot(moveDirection, direction) < 0.75f)
                moveDirection = Vector2.Lerp(moveDirection, direction, 0.7f).normalized;
        }

        Vector2 targetVelocity = moveDirection * moveSpeed * speedScale;
        float smoothing = 1f - Mathf.Exp(-Mathf.Max(0f, velocitySmoothing) * Time.fixedDeltaTime);
        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, targetVelocity, smoothing);
        rb.angularVelocity = 0f;

        float angle =
            Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        rb.SetRotation(angle + spriteAngleOffset);
    }

    public float CurrentCrowdSpeed => moveSpeed;

    public float CurrentPersonalSpace =>
        personalSpace * Mathf.Max(1f, transform.lossyScale.x, transform.lossyScale.y);

    public static Vector2 CalculateCrowdSteering(
        GameObject owner,
        Rigidbody2D ownerBody,
        Collider2D[] ownerColliders,
        Vector2 position,
        Vector2 goalDirection,
        float ownerSpeed,
        float ownerPersonalSpace,
        float lookRadius,
        float separationStrength,
        float laneSide,
        float laneStrength,
        float passStrength,
        float yieldStrength,
        out float speedScale)
    {
        speedScale = 1f;

        if (goalDirection.sqrMagnitude < 0.001f || lookRadius <= 0f)
            return Vector2.zero;

        Vector2 forward = goalDirection.normalized;
        Vector2 side = Perpendicular(forward);
        Vector2 steering = Vector2.zero;
        int hitCount = Physics2D.OverlapCircle(position, lookRadius, CrowdContactFilter, CrowdHits);

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = CrowdHits[i];
            if (!TryGetCrowdAgent(hit, owner, ownerBody, out Transform otherTransform, out float otherSpeed, out float otherPersonalSpace))
                continue;

            IgnoreEnemyCollision(ownerColliders, hit);

            Vector2 toOther = (Vector2)otherTransform.position - position;
            float sqrDistance = toOther.sqrMagnitude;
            if (sqrDistance < 0.0001f)
                toOther = side * laneSide * 0.01f;

            float distance = Mathf.Max(0.01f, toOther.magnitude);
            Vector2 toOtherDirection = toOther / distance;
            float desiredClearance = Mathf.Max(0.1f, ownerPersonalSpace + otherPersonalSpace);

            if (distance < desiredClearance)
            {
                float crowding = 1f - distance / desiredClearance;
                steering -= toOtherDirection * crowding * crowding * separationStrength;
                speedScale = Mathf.Min(speedScale, Mathf.Lerp(0.75f, 1f, distance / desiredClearance));
            }

            float aheadAmount = Vector2.Dot(forward, toOtherDirection);
            float sideAmount = Mathf.Abs(Cross(forward, toOtherDirection));
            float closeness = 1f - Mathf.Clamp01(distance / Mathf.Max(lookRadius, desiredClearance));

            if (aheadAmount > 0.35f)
            {
                float sideChoice = sideAmount > 0.05f
                    ? -Mathf.Sign(Cross(forward, toOtherDirection))
                    : Mathf.Sign(laneSide);
                if (ownerSpeed > otherSpeed + 0.15f)
                {
                    steering += side * sideChoice * closeness * passStrength;
                }
                else if (distance < desiredClearance * 1.25f)
                {
                    speedScale = Mathf.Min(speedScale, Mathf.Lerp(0.85f, 1f, distance / (desiredClearance * 1.25f)));
                    steering += side * sideChoice * closeness * laneStrength;
                }
            }
            else if (aheadAmount < -0.25f && otherSpeed > ownerSpeed + 0.15f)
            {
                float otherSide = Cross(forward, toOtherDirection);
                float sideChoice = Mathf.Abs(otherSide) > 0.05f ? -Mathf.Sign(otherSide) : Mathf.Sign(laneSide);
                steering += side * sideChoice * closeness * yieldStrength;
                speedScale = Mathf.Min(speedScale, Mathf.Lerp(0.9f, 1f, distance / Mathf.Max(0.01f, lookRadius)));
            }
        }

        return Vector2.ClampMagnitude(steering, 0.55f);
    }

    private static bool TryGetCrowdAgent(
        Collider2D hit,
        GameObject owner,
        Rigidbody2D ownerBody,
        out Transform otherTransform,
        out float otherSpeed,
        out float otherPersonalSpace)
    {
        otherTransform = null;
        otherSpeed = 0f;
        otherPersonalSpace = 0f;

        if (hit == null || hit.isTrigger)
            return false;

        Rigidbody2D hitBody = hit.attachedRigidbody;
        if (hitBody != null && hitBody == ownerBody)
            return false;

        GameObject hitObject = hitBody != null ? hitBody.gameObject : hit.gameObject;
        if (hitObject == owner)
            return false;

        EnemyHealth health = hitObject.GetComponentInParent<EnemyHealth>();
        if (health != null && health.IsDead)
            return false;

        EnemyMovement movement = hitObject.GetComponentInParent<EnemyMovement>();
        if (movement != null && movement.enabled)
        {
            if (movement.gameObject == owner)
                return false;

            otherTransform = movement.transform;
            otherSpeed = movement.CurrentCrowdSpeed;
            otherPersonalSpace = movement.CurrentPersonalSpace;
            return true;
        }

        SpitterZombie spitter = hitObject.GetComponentInParent<SpitterZombie>();
        if (spitter != null && spitter.enabled)
        {
            if (spitter.gameObject == owner)
                return false;

            otherTransform = spitter.transform;
            otherSpeed = spitter.CurrentCrowdSpeed;
            otherPersonalSpace = spitter.CurrentPersonalSpace;
            return true;
        }

        return false;
    }

    private static void IgnoreEnemyCollision(Collider2D[] ownerColliders, Collider2D otherCollider)
    {
        if (ownerColliders == null || otherCollider == null)
            return;

        for (int i = 0; i < ownerColliders.Length; i++)
        {
            Collider2D ownerCollider = ownerColliders[i];
            if (ownerCollider == null || ownerCollider == otherCollider)
                continue;

            Physics2D.IgnoreCollision(ownerCollider, otherCollider, true);
        }
    }

    public static Vector2 Perpendicular(Vector2 direction)
    {
        return new Vector2(-direction.y, direction.x);
    }

    private static float Cross(Vector2 a, Vector2 b)
    {
        return a.x * b.y - a.y * b.x;
    }

    private static float StableLaneSide(int stableSeed)
    {
        return (stableSeed & 1) == 0 ? 1f : -1f;
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
