using UnityEngine;

// Laat een zombie uit een donkere opening in de grond omhoog komen voordat hij kan aanvallen.
public class ZombieSpawnEffect : MonoBehaviour
{
    public float duration = 0.7f;
    public float portalRadius = 0.9f;

    private SpriteRenderer visual;
    private Color originalColor;
    private Vector3 originalScale;
    private Rigidbody2D body;
    private bool bodyWasSimulated;
    private Collider2D[] colliders;
    private bool[] colliderStates;
    private MonoBehaviour[] pausedBehaviours;
    private bool[] behaviourStates;
    private LineRenderer portalRing;
    private Material portalMaterial;
    private float startedAt;
    private bool initialized;

    public static void Play(GameObject zombie)
    {
        if (zombie == null) return;
        ZombieSpawnEffect effect = zombie.GetComponent<ZombieSpawnEffect>();
        if (effect == null) effect = zombie.AddComponent<ZombieSpawnEffect>();
        effect.Begin();
    }

    private void Begin()
    {
        if (initialized) return;
        initialized = true;
        startedAt = Time.time;
        originalScale = transform.localScale;
        visual = GetComponentInChildren<SpriteRenderer>();
        if (visual != null)
        {
            originalColor = visual.color;
            visual.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
        }
        transform.localScale = originalScale * 0.12f;

        body = GetComponent<Rigidbody2D>();
        if (body != null)
        {
            bodyWasSimulated = body.simulated;
            body.linearVelocity = Vector2.zero;
            body.simulated = false;
        }

        colliders = GetComponentsInChildren<Collider2D>();
        colliderStates = new bool[colliders.Length];
        for (int i = 0; i < colliders.Length; i++)
        {
            colliderStates[i] = colliders[i].enabled;
            colliders[i].enabled = false;
        }

        pausedBehaviours = new MonoBehaviour[]
        {
            GetComponent<EnemyMovement>(),
            GetComponent<ExploderZombie>(),
            GetComponent<SpitterZombie>()
        };
        behaviourStates = new bool[pausedBehaviours.Length];
        for (int i = 0; i < pausedBehaviours.Length; i++)
        {
            if (pausedBehaviours[i] == null) continue;
            behaviourStates[i] = pausedBehaviours[i].enabled;
            pausedBehaviours[i].enabled = false;
        }

        CreatePortal();
    }

    void Update()
    {
        if (!initialized) return;
        float progress = Mathf.Clamp01((Time.time - startedAt) / Mathf.Max(0.1f, duration));
        float smooth = progress * progress * (3f - 2f * progress);
        transform.localScale = Vector3.Lerp(originalScale * 0.12f, originalScale, smooth);

        if (visual != null)
            visual.color = new Color(originalColor.r, originalColor.g, originalColor.b, smooth);

        if (portalRing != null)
        {
            SetPortalRadius(Mathf.Lerp(portalRadius * 0.45f, portalRadius, smooth));
            Color color = new Color(0.28f, 0.05f, 0.38f, 1f - progress * 0.75f);
            portalRing.startColor = color;
            portalRing.endColor = color;
        }

        if (progress >= 1f) Finish();
    }

    private void Finish()
    {
        initialized = false;
        transform.localScale = originalScale;
        if (visual != null) visual.color = originalColor;
        if (body != null) body.simulated = bodyWasSimulated;

        for (int i = 0; i < colliders.Length; i++)
            if (colliders[i] != null) colliders[i].enabled = colliderStates[i];

        for (int i = 0; i < pausedBehaviours.Length; i++)
            if (pausedBehaviours[i] != null) pausedBehaviours[i].enabled = behaviourStates[i];

        if (portalRing != null) Destroy(portalRing.gameObject);
        if (portalMaterial != null) Destroy(portalMaterial);
        Destroy(this);
    }

    private void CreatePortal()
    {
        GameObject portal = new GameObject("GroundPortal");
        portal.transform.position = transform.position;
        portalRing = portal.AddComponent<LineRenderer>();
        portalRing.useWorldSpace = false;
        portalRing.loop = true;
        portalRing.positionCount = 48;
        portalRing.widthMultiplier = 0.12f;
        portalRing.numCornerVertices = 2;
        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            portalMaterial = new Material(shader);
            portalRing.material = portalMaterial;
        }
        if (visual != null)
        {
            portalRing.sortingLayerID = visual.sortingLayerID;
            portalRing.sortingOrder = visual.sortingOrder - 1;
        }
        SetPortalRadius(portalRadius * 0.45f);
    }

    private void SetPortalRadius(float radius)
    {
        for (int i = 0; i < portalRing.positionCount; i++)
        {
            float angle = i * Mathf.PI * 2f / portalRing.positionCount;
            float roughness = 1f + Mathf.Sin(angle * 7f) * 0.06f;
            portalRing.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius * roughness);
        }
    }

    void OnDestroy()
    {
        if (portalRing != null) Destroy(portalRing.gameObject);
        if (portalMaterial != null) Destroy(portalMaterial);
    }
}
