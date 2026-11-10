using UnityEngine;

// Laat een bloedplas even liggen en daarna geleidelijk vervagen.
public class BloodPoolFade : MonoBehaviour
{
    private SpriteRenderer poolRenderer;
    private float fadeStartsAt;
    private float destroyAt;
    private Color originalColor;

    public void Initialize(SpriteRenderer renderer, float lifetime, float fadeDuration)
    {
        poolRenderer = renderer;
        originalColor = renderer != null ? renderer.color : Color.white;
        float safeFadeDuration = Mathf.Max(0.05f, fadeDuration);
        destroyAt = Time.time + Mathf.Max(safeFadeDuration, lifetime);
        fadeStartsAt = destroyAt - safeFadeDuration;
    }

    void Update()
    {
        if (poolRenderer == null || Time.time >= destroyAt)
        {
            Destroy(gameObject);
            return;
        }

        if (Time.time < fadeStartsAt) return;
        float progress = Mathf.InverseLerp(fadeStartsAt, destroyAt, Time.time);
        poolRenderer.color = new Color(
            originalColor.r,
            originalColor.g,
            originalColor.b,
            1f - progress);
    }
}
