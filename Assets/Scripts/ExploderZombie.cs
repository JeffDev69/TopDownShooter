using System.Collections;
using UnityEngine;

// Explodeert bij de player of bij death en doet area damage.
[RequireComponent(typeof(EnemyHealth))]
public class ExploderZombie : MonoBehaviour
{
    public float triggerDistance = 1.1f;
    public float explosionRadius = 2f;
    public int explosionDamage = 2;

    [Header("Warning Effect")]
    public float fuseDuration = 0.9f;
    public float warningFlashSpeed = 10f;
    public Color warningColor = new Color(1f, 0.15f, 0.02f, 1f);
    public float warningRingRadius = 0.85f;

    private EnemyHealth enemyHealth;
    private Transform player;
    private Rigidbody2D body;
    private EnemyMovement movement;
    private SpriteRenderer visual;
    private Color normalColor;
    private LineRenderer warningRing;
    private Material warningMaterial;
    private bool fuseStarted;
    private float explodeAt;
    private bool exploded;

    void Awake()
    {
        enemyHealth = GetComponent<EnemyHealth>();
        enemyHealth.Died += OnDied;
        body = GetComponent<Rigidbody2D>();
        movement = GetComponent<EnemyMovement>();
        visual = GetComponentInChildren<SpriteRenderer>();
        if (visual != null) normalColor = visual.color;
    }

    void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null) player = playerObject.transform;
        CreateWarningRing();
    }

    void Update()
    {
        if (exploded || enemyHealth.IsDead || player == null) return;

        if (!fuseStarted && Vector2.Distance(transform.position, player.position) <= triggerDistance)
            StartFuse();

        if (!fuseStarted) return;

        float pulse = Mathf.PingPong(Time.time * warningFlashSpeed, 1f);
        if (visual != null)
            visual.color = Color.Lerp(normalColor, warningColor, pulse);

        if (warningRing != null)
        {
            float radius = warningRingRadius * Mathf.Lerp(0.85f, 1.15f, pulse);
            SetRingRadius(radius);
            Color ringColor = new Color(1f, 0.2f, 0.02f, Mathf.Lerp(0.35f, 0.95f, pulse));
            warningRing.startColor = ringColor;
            warningRing.endColor = ringColor;
        }

        if (Time.time >= explodeAt)
            enemyHealth.KillFromEnemyAbility();
    }

    private void StartFuse()
    {
        fuseStarted = true;
        explodeAt = Time.time + Mathf.Max(0.15f, fuseDuration);
        if (movement != null) movement.enabled = false;
        if (body != null) body.linearVelocity = Vector2.zero;
        if (warningRing != null) warningRing.enabled = true;
    }

    private void OnDied(EnemyHealth deadEnemy)
    {
        if (exploded) return;
        exploded = true;
        if (visual != null) visual.color = normalColor;
        if (warningRing != null) StartCoroutine(PlayExplosionRing());

        foreach (Collider2D hit in Physics2D.OverlapCircleAll(transform.position, explosionRadius))
        {
            PlayerHealth playerHealth = hit.GetComponentInParent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(Mathf.Max(1, explosionDamage));
                break;
            }
        }
    }

    private void CreateWarningRing()
    {
        GameObject ringObject = new GameObject("ExplosionWarningRing");
        ringObject.transform.SetParent(transform, false);
        warningRing = ringObject.AddComponent<LineRenderer>();
        warningRing.useWorldSpace = false;
        warningRing.loop = true;
        warningRing.positionCount = 40;
        warningRing.widthMultiplier = 0.07f;
        warningRing.numCornerVertices = 2;
        warningRing.enabled = false;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            warningMaterial = new Material(shader);
            warningRing.material = warningMaterial;
        }

        if (visual != null)
        {
            warningRing.sortingLayerID = visual.sortingLayerID;
            warningRing.sortingOrder = visual.sortingOrder + 1;
        }
        SetRingRadius(warningRingRadius);
    }

    private void SetRingRadius(float radius)
    {
        if (warningRing == null) return;
        for (int i = 0; i < warningRing.positionCount; i++)
        {
            float angle = i * Mathf.PI * 2f / warningRing.positionCount;
            warningRing.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius);
        }
    }

    private IEnumerator PlayExplosionRing()
    {
        warningRing.enabled = true;
        float duration = 0.25f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            SetRingRadius(Mathf.Lerp(0.3f, explosionRadius, progress));
            Color color = new Color(1f, Mathf.Lerp(0.8f, 0.1f, progress), 0f, 1f - progress);
            warningRing.startColor = color;
            warningRing.endColor = color;
            yield return null;
        }
        warningRing.enabled = false;
    }

    void OnDestroy()
    {
        if (enemyHealth != null) enemyHealth.Died -= OnDied;
        if (warningMaterial != null) Destroy(warningMaterial);
    }
}
