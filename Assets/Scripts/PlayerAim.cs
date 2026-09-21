using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
// Draait alleen de visual en weapon pivot naar de mouse zodat de physics root stabiel blijft.
public class PlayerAim : MonoBehaviour
{
    private Camera mainCamera;
    private Transform aimPivot;
    public SpriteRenderer VisualRenderer { get; private set; }

    void Awake()
    {
        mainCamera = Camera.main;

        // Keep physics on the root; rotate only this visual hierarchy.
        aimPivot = new GameObject("AimPivot").transform;
        aimPivot.SetParent(transform, false);

        SpriteRenderer original = GetComponent<SpriteRenderer>();
        if (original != null)
        {
            Transform visual = new GameObject("PlayerVisual").transform;
            visual.SetParent(aimPivot, false);
            VisualRenderer = visual.gameObject.AddComponent<SpriteRenderer>();
            VisualRenderer.sprite = original.sprite;
            VisualRenderer.color = original.color;
            VisualRenderer.sharedMaterials = original.sharedMaterials;
            VisualRenderer.sortingLayerID = original.sortingLayerID;
            VisualRenderer.sortingOrder = original.sortingOrder;
            VisualRenderer.flipX = original.flipX;
            VisualRenderer.flipY = original.flipY;
            VisualRenderer.drawMode = original.drawMode;
            VisualRenderer.size = original.size;
            VisualRenderer.maskInteraction = original.maskInteraction;
            VisualRenderer.spriteSortPoint = original.spriteSortPoint;
            original.enabled = false;
        }

        PlayerShoot shooting = GetComponent<PlayerShoot>();
        if (shooting != null && shooting.firePoint != null && shooting.firePoint != transform)
            shooting.firePoint.SetParent(aimPivot, true);

        Rigidbody2D body = GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.angularVelocity = 0f;
            body.constraints |= RigidbodyConstraints2D.FreezeRotation;
        }
    }

    void Update()
    {
        if (mainCamera == null || Mouse.current == null)
            return;

        Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();

        Vector3 mouseWorldPosition =
            mainCamera.ScreenToWorldPoint(mouseScreenPosition);

        Vector2 direction =
            mouseWorldPosition - transform.position;

        float angle =
            Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        aimPivot.rotation =
            Quaternion.Euler(0f, 0f, angle);
    }
}
