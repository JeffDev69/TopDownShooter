using UnityEngine;

// Runtime acid shot zodat de Spitter geen apart projectile prefab nodig heeft.
public class AcidProjectile : MonoBehaviour
{
    private int damage;
    private float destroyAt;
    private static Sprite acidSprite;

    public static void Spawn(Vector2 position, Vector2 direction, float speed, int damageAmount)
    {
        GameObject projectile = new GameObject("AcidProjectile");
        projectile.transform.position = position;

        AcidProjectile acid = projectile.AddComponent<AcidProjectile>();
        acid.damage = Mathf.Max(1, damageAmount);
        acid.destroyAt = Time.time + 3f;

        SpriteRenderer renderer = projectile.AddComponent<SpriteRenderer>();
        renderer.sprite = GetAcidSprite();
        renderer.color = new Color(0.55f, 1f, 0.05f, 1f);
        renderer.sortingOrder = 20;
        projectile.transform.localScale = Vector3.one * 0.32f;

        Rigidbody2D body = projectile.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.linearVelocity = direction.normalized * Mathf.Max(0.1f, speed);

        CircleCollider2D collider = projectile.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.48f;
    }

    void Update()
    {
        if (Time.time >= destroyAt) Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<EnemyHealth>() != null) return;

        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
            Destroy(gameObject);
        }
        else if (!other.isTrigger)
        {
            Destroy(gameObject);
        }
    }

    private static Sprite GetAcidSprite()
    {
        if (acidSprite != null) return acidSprite;
        const int size = 32;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color clear = new Color(1f, 1f, 1f, 0f);
        Color white = Color.white;
        float radius = size * 0.46f;
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
            texture.SetPixel(x, y, Vector2.Distance(new Vector2(x, y), center) <= radius ? white : clear);

        texture.Apply();
        texture.filterMode = FilterMode.Bilinear;
        acidSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return acidSprite;
    }
}
