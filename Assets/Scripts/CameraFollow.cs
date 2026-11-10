using UnityEngine;

// Laat de camera de speler binnen de map volgen zonder de vaste Z-positie te veranderen.
public class CameraFollow : MonoBehaviour
{
    public Transform target;
    private Camera cameraComponent;
    private SpriteRenderer mapRenderer;

    void Awake()
    {
        cameraComponent = GetComponent<Camera>();
        GameObject map = GameObject.Find("Map");
        if (map != null)
            mapRenderer = map.GetComponent<SpriteRenderer>();
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 position = target.position;
        if (cameraComponent != null && cameraComponent.orthographic && mapRenderer != null)
        {
            Bounds bounds = mapRenderer.bounds;
            float halfHeight = cameraComponent.orthographicSize;
            float halfWidth = halfHeight * cameraComponent.aspect;
            const float edgeMargin = 0.1f;

            position.x = bounds.size.x <= 2f * (halfWidth + edgeMargin)
                ? bounds.center.x
                : Mathf.Clamp(position.x, bounds.min.x + halfWidth + edgeMargin,
                    bounds.max.x - halfWidth - edgeMargin);
            position.y = bounds.size.y <= 2f * (halfHeight + edgeMargin)
                ? bounds.center.y
                : Mathf.Clamp(position.y, bounds.min.y + halfHeight + edgeMargin,
                    bounds.max.y - halfHeight - edgeMargin);
        }

        position.z = transform.position.z;
        transform.position = position;
    }
}
