using UnityEngine;

// Geeft special weapon bullets een herkenbare kleur, size en trail.
public static class ProjectileVisuals
{
    private static Material trailMaterial;

    public static void Apply(GameObject projectile, Color color, float sizeMultiplier, float trailTime)
    {
        if (projectile == null) return;
        projectile.transform.localScale *= Mathf.Max(0.1f, sizeMultiplier);

        SpriteRenderer renderer = projectile.GetComponentInChildren<SpriteRenderer>();
        if (renderer != null) renderer.color = color;

        TrailRenderer trail = projectile.AddComponent<TrailRenderer>();
        trail.time = Mathf.Max(0.05f, trailTime);
        trail.startWidth = 0.18f;
        trail.endWidth = 0f;
        trail.startColor = color;
        trail.endColor = new Color(color.r, color.g, color.b, 0f);
        trail.sortingOrder = renderer != null ? renderer.sortingOrder - 1 : 0;
        if (trailMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null) trailMaterial = new Material(shader);
        }
        if (trailMaterial != null) trail.material = trailMaterial;
    }
}
