using UnityEngine;

// Tekent een korte expanding ring voor rocket explosions zonder extra prefab.
public class WeaponExplosionEffect : MonoBehaviour
{
    private LineRenderer ring;
    private Material ringMaterial;
    private float radius;
    private float startedAt;
    private const float Duration = 0.28f;

    public static void Spawn(Vector2 position, float explosionRadius)
    {
        GameObject effect = new GameObject("RocketExplosion");
        effect.transform.position = position;
        WeaponExplosionEffect explosion = effect.AddComponent<WeaponExplosionEffect>();
        explosion.radius = Mathf.Max(0.2f, explosionRadius);
        explosion.CreateRing();
    }

    private void CreateRing()
    {
        startedAt = Time.time;
        ring = gameObject.AddComponent<LineRenderer>();
        ring.useWorldSpace = false;
        ring.loop = true;
        ring.positionCount = 48;
        ring.widthMultiplier = 0.16f;
        ring.sortingOrder = 120;
        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            ringMaterial = new Material(shader);
            ring.material = ringMaterial;
        }
    }

    void Update()
    {
        float progress = Mathf.Clamp01((Time.time - startedAt) / Duration);
        float currentRadius = Mathf.Lerp(0.15f, radius, progress);
        for (int i = 0; i < ring.positionCount; i++)
        {
            float angle = i * Mathf.PI * 2f / ring.positionCount;
            ring.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * currentRadius);
        }
        Color color = new Color(1f, Mathf.Lerp(0.85f, 0.12f, progress), 0.02f, 1f - progress);
        ring.startColor = color;
        ring.endColor = color;
        if (progress >= 1f) Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (ringMaterial != null) Destroy(ringMaterial);
    }
}
