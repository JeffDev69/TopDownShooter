using System.Collections.Generic;
using UnityEngine;

// Laat de zombie de speler achtervolgen en schade toebrengen zolang er contact is.
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
    private static readonly List<RaycastHit2D> ObstacleHits = new List<RaycastHit2D>(16);

    private Transform player;
    private Rigidbody2D rb;
    private Collider2D[] ownColliders;
    private readonly ObstacleNavigator obstacleNavigator = new ObstacleNavigator();

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
        Vector2 travelDirection = obstacleNavigator.GetDirection(transform.position,
            player.position, CurrentPersonalSpace * 0.5f);
        Vector2 crowdSteering = CalculateCrowdSteering(
            gameObject,
            rb,
            ownColliders,
            transform.position,
            travelDirection,
            moveSpeed,
            CurrentPersonalSpace,
            crowdLookRadius,
            separationStrength,
            laneSide,
            laneStrength,
            passStrength,
            yieldStrength,
            out speedScale);

        Vector2 moveDirection = travelDirection + crowdSteering;

        if (moveDirection.sqrMagnitude < 0.001f)
            moveDirection = travelDirection;
        else
        {
            moveDirection.Normalize();
            if (Vector2.Dot(moveDirection, travelDirection) < 0.75f)
                moveDirection = Vector2.Lerp(moveDirection, travelDirection, 0.7f).normalized;
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

    public sealed class ObstacleNavigator
    {
        private const float GridSize = 0.8f;
        private static readonly Vector2Int[] Neighbours =
        {
            Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left
        };
        private static readonly List<Collider2D> StaticHits = new List<Collider2D>(16);

        private readonly List<Vector2> path = new List<Vector2>();
        private readonly Dictionary<Vector2Int, bool> walkable = new Dictionary<Vector2Int, bool>();
        private readonly Dictionary<Vector2Int, float> distance = new Dictionary<Vector2Int, float>();
        private readonly Dictionary<Vector2Int, Vector2Int> previous = new Dictionary<Vector2Int, Vector2Int>();
        private readonly HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        private readonly List<Vector2Int> open = new List<Vector2Int>();
        private int pathIndex;
        private float nextSearchTime;
        private Vector2 pathTarget;

        public Vector2 GetDirection(Vector2 position, Vector2 target, float radius)
        {
            Vector2 toTarget = target - position;
            if (toTarget.sqrMagnitude < 0.001f)
                return Vector2.zero;

            radius = Mathf.Max(0.18f, radius);
            if (HasClearPath(position, target, radius))
            {
                path.Clear();
                return toTarget.normalized;
            }

            if (Time.time >= nextSearchTime ||
                (target - pathTarget).sqrMagnitude > 4f)
            {
                nextSearchTime = Time.time + 1f;
                pathTarget = target;
                FindPath(position, target, radius);
            }

            while (pathIndex < path.Count &&
                   (path[pathIndex] - position).sqrMagnitude < 0.25f)
                pathIndex++;

            // Sla tussenpunten over zodra de doorgang vrij is.
            for (int i = path.Count - 1; i > pathIndex; i--)
            {
                if (HasClearPath(position, path[i], radius))
                {
                    pathIndex = i;
                    break;
                }
            }

            return pathIndex < path.Count
                ? (path[pathIndex] - position).normalized
                : toTarget.normalized;
        }

        private void FindPath(Vector2 position, Vector2 target, float radius)
        {
            path.Clear();
            pathIndex = 0;
            walkable.Clear();
            distance.Clear();
            previous.Clear();
            visited.Clear();
            open.Clear();

            Vector2Int start = ToCell(position);
            if (!IsWalkable(start, radius) || !HasClearPath(position, ToWorld(start), radius))
            {
                float closest = float.MaxValue;
                bool found = false;
                Vector2Int center = start;
                for (int x = -2; x <= 2; x++)
                {
                    for (int y = -2; y <= 2; y++)
                    {
                        Vector2Int candidate = center + new Vector2Int(x, y);
                        Vector2 point = ToWorld(candidate);
                        float sqrDistance = (point - position).sqrMagnitude;
                        if (sqrDistance < closest && IsWalkable(candidate, radius) &&
                            HasClearPath(position, point, radius))
                        {
                            start = candidate;
                            closest = sqrDistance;
                            found = true;
                        }
                    }
                }

                if (!found)
                    return;
            }

            open.Add(start);
            distance[start] = 0f;

            while (open.Count > 0 && visited.Count < 3000)
            {
                int bestIndex = 0;
                float bestScore = float.MaxValue;
                for (int i = 0; i < open.Count; i++)
                {
                    Vector2Int candidate = open[i];
                    float score = distance[candidate] + Vector2.Distance(ToWorld(candidate), target);
                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestIndex = i;
                    }
                }

                Vector2Int current = open[bestIndex];
                open.RemoveAt(bestIndex);
                if (!visited.Add(current))
                    continue;

                Vector2 currentPosition = ToWorld(current);
                if (current != start &&
                    (currentPosition - target).sqrMagnitude < 2f &&
                    HasClearPath(currentPosition, target, radius))
                {
                    for (Vector2Int step = current; step != start; step = previous[step])
                        path.Add(ToWorld(step));
                    path.Reverse();
                    if ((ToWorld(start) - position).sqrMagnitude > 0.25f)
                        path.Insert(0, ToWorld(start));
                    path.Add(target);
                    return;
                }

                foreach (Vector2Int offset in Neighbours)
                {
                    Vector2Int neighbour = current + offset;
                    if (visited.Contains(neighbour) || !IsWalkable(neighbour, radius))
                        continue;

                    float newDistance = distance[current] + GridSize;
                    if (distance.TryGetValue(neighbour, out float oldDistance) &&
                        newDistance >= oldDistance)
                        continue;

                    distance[neighbour] = newDistance;
                    previous[neighbour] = current;
                    if (!open.Contains(neighbour))
                        open.Add(neighbour);
                }
            }
        }

        private bool IsWalkable(Vector2Int cell, float radius)
        {
            if (walkable.TryGetValue(cell, out bool result))
                return result;

            StaticHits.Clear();
            Physics2D.OverlapCircle(ToWorld(cell), radius + 0.05f,
                CrowdContactFilter, StaticHits);
            result = true;
            foreach (Collider2D hit in StaticHits)
            {
                if (hit != null && hit.attachedRigidbody == null)
                {
                    result = false;
                    break;
                }
            }

            walkable[cell] = result;
            return result;
        }

        private static bool HasClearPath(Vector2 start, Vector2 end, float radius)
        {
            Vector2 direction = end - start;
            float length = direction.magnitude;
            if (length < 0.01f)
                return true;

            ObstacleHits.Clear();
            Physics2D.CircleCast(start, radius, direction / length,
                CrowdContactFilter, ObstacleHits, length);
            foreach (RaycastHit2D hit in ObstacleHits)
            {
                if (hit.collider != null && hit.collider.attachedRigidbody == null)
                    return false;
            }

            return true;
        }

        private static Vector2Int ToCell(Vector2 position)
        {
            return new Vector2Int(Mathf.RoundToInt(position.x / GridSize),
                Mathf.RoundToInt(position.y / GridSize));
        }

        private static Vector2 ToWorld(Vector2Int cell)
        {
            return new Vector2(cell.x * GridSize, cell.y * GridSize);
        }
    }

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
