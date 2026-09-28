using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

[DefaultExecutionOrder(-100)]
// Draait alleen de visual en weapon pivot naar de mouse zodat de physics root stabiel blijft.
public class PlayerAim : MonoBehaviour
{
    private Camera mainCamera;
    private Transform aimPivot;
    private PlayerShoot shooting;
    private Sprite originalSprite;
    private SpriteRenderer weaponRenderer;
    private PlayerWeaponSprites weaponSprites;
    private PlayerShoot.WeaponType? shownWeapon;
    private Vector3 originalFirePosition;
    private Quaternion originalFireRotation;
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

            originalSprite = original.sprite;
            weaponSprites = new PlayerWeaponSprites(originalSprite);
            SortingGroup sorting = visual.gameObject.AddComponent<SortingGroup>();
            sorting.sortingLayerID = original.sortingLayerID;
            sorting.sortingOrder = original.sortingOrder;
            Transform weapon = new GameObject("WeaponVisual").transform;
            weapon.SetParent(visual, false);
            weaponRenderer = weapon.gameObject.AddComponent<SpriteRenderer>();
            weaponRenderer.sharedMaterials = original.sharedMaterials;
            weaponRenderer.sortingLayerID = original.sortingLayerID;
            weaponRenderer.sortingOrder = original.sortingOrder - 1;
            weaponRenderer.maskInteraction = original.maskInteraction;
            weaponRenderer.enabled = false;
        }

        shooting = GetComponent<PlayerShoot>();
        if (shooting != null && shooting.firePoint != null && shooting.firePoint != transform)
        {
            shooting.firePoint.SetParent(aimPivot, true);
            originalFirePosition = shooting.firePoint.localPosition;
            originalFireRotation = shooting.firePoint.localRotation;
        }

        Rigidbody2D body = GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.angularVelocity = 0f;
            body.constraints |= RigidbodyConstraints2D.FreezeRotation;
        }
        RefreshWeaponVisual();
    }

    void Update()
    {
        RefreshWeaponVisual();
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

    private void LateUpdate()
    {
        RefreshWeaponVisual();
    }

    public void RefreshWeaponVisual()
    {
        UpdateWeaponVisual();
        if (weaponRenderer == null) return;

        weaponRenderer.color = VisualRenderer.color;
        Vector2 facing = new Vector2(VisualRenderer.flipX ? -1f : 1f, VisualRenderer.flipY ? -1f : 1f);
        weaponRenderer.transform.localPosition = weaponSprites.WeaponPosition * facing;
        weaponRenderer.transform.localScale = new Vector3(
            facing.x * weaponSprites.WeaponScale, facing.y * weaponSprites.WeaponScale, 1f);
        if (shooting != null && shooting.firePoint != null && shooting.firePoint != transform)
        {
            // Match the visible barrel, including the player's damage-scale animation.
            if (weaponRenderer.enabled)
            {
                shooting.firePoint.position = weaponRenderer.transform.TransformPoint(
                    new Vector3(weaponRenderer.sprite.bounds.max.x + 0.01f, 0f, 0f));
                Vector3 direction = weaponRenderer.transform.TransformVector(Vector3.right);
                shooting.firePoint.rotation = Quaternion.Euler(0f, 0f,
                    Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            }
            else
            {
                shooting.firePoint.localPosition = originalFirePosition;
                shooting.firePoint.localRotation = originalFireRotation;
            }
        }
    }

    private void UpdateWeaponVisual()
    {
        if (shooting == null || weaponRenderer == null || shownWeapon == shooting.CurrentWeapon) return;
        shownWeapon = shooting.CurrentWeapon;
        Sprite gun = weaponSprites.Get(shownWeapon.Value);
        weaponRenderer.sprite = gun;
        weaponRenderer.enabled = gun != null;
        VisualRenderer.sprite = gun != null ? weaponSprites.Body : originalSprite;
    }
}
