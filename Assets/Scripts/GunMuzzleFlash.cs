using UnityEngine;

// Maakt tijdens het schieten een korte procedural muzzle flash zonder extra sprite asset.
public class GunMuzzleFlash : MonoBehaviour
{
    public float flashDuration = 0.045f;
    private Transform flashRoot;
    private LineRenderer outer;
    private LineRenderer core;
    private Material flashMaterial;
    private float hideAt;

    public void Show(Transform muzzle, PlayerShoot.WeaponType weapon)
    {
        if (muzzle == null) return;
        if (flashRoot == null && !CreateFlash(muzzle)) return;
        flashRoot.SetParent(muzzle, false);
        flashRoot.localPosition = Vector3.zero;
        flashRoot.localRotation = Quaternion.identity;
        // Keep flash dimensions consistent despite the player's scaled hierarchy.
        Vector3 scale = muzzle.lossyScale;
        flashRoot.localScale = new Vector3(
            1f / Mathf.Max(0.001f, Mathf.Abs(scale.x)),
            1f / Mathf.Max(0.001f, Mathf.Abs(scale.y)), 1f);

        float length = 0.35f;
        float width = 0.18f;
        switch (weapon)
        {
            case PlayerShoot.WeaponType.Shotgun:
            case PlayerShoot.WeaponType.DoubleBarrel:
            case PlayerShoot.WeaponType.SPAS12:
            case PlayerShoot.WeaponType.AA12:
                length = 0.6f; width = 0.35f; break;
            case PlayerShoot.WeaponType.Sniper:
            case PlayerShoot.WeaponType.Dragunov:
            case PlayerShoot.WeaponType.BarrettM82:
                length = 0.65f; width = 0.23f; break;
            case PlayerShoot.WeaponType.Minigun:
            case PlayerShoot.WeaponType.RPK:
            case PlayerShoot.WeaponType.M60:
                length = 0.45f; width = 0.25f; break;
        }
        length *= Random.Range(0.85f, 1.15f);
        SetShape(outer, length, width);
        SetShape(core, length * 0.65f, width * 0.45f);
        outer.enabled = true;
        core.enabled = true;
        hideAt = Time.time + Mathf.Max(0.01f, flashDuration);
    }

    private bool CreateFlash(Transform muzzle)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) return false;
        flashMaterial = new Material(shader);
        flashRoot = new GameObject("Muzzle Flash").transform;
        flashRoot.SetParent(muzzle, false);
        outer = CreateLine("Gold Flash", new Color(1f, 0.65f, 0.08f), 20);
        core = CreateLine("White Core", new Color(1f, 0.97f, 0.75f), 21);
        return true;
    }

    private LineRenderer CreateLine(string objectName, Color color, int order)
    {
        GameObject part = new GameObject(objectName);
        part.transform.SetParent(flashRoot, false);
        LineRenderer line = part.AddComponent<LineRenderer>();
        line.sharedMaterial = flashMaterial;
        line.useWorldSpace = false;
        line.positionCount = 2;
        line.startColor = color;
        line.endColor = color;
        line.sortingOrder = order;
        PlayerAim aim = GetComponent<PlayerAim>();
        if (aim != null && aim.VisualRenderer != null)
        {
            line.sortingLayerID = aim.VisualRenderer.sortingLayerID;
            line.sortingOrder = aim.VisualRenderer.sortingOrder + order;
        }
        line.enabled = false;
        return line;
    }

    private static void SetShape(LineRenderer line, float length, float width)
    {
        line.SetPosition(0, new Vector3(0.02f, 0f, 0f));
        line.SetPosition(1, new Vector3(length, 0f, 0f));
        line.startWidth = width;
        line.endWidth = 0f;
    }

    private void LateUpdate()
    {
        if (outer != null && Time.time >= hideAt)
        {
            outer.enabled = false;
            core.enabled = false;
        }
    }

    private void OnDisable()
    {
        if (outer != null) outer.enabled = false;
        if (core != null) core.enabled = false;
    }

    private void OnDestroy()
    {
        if (flashRoot != null) Destroy(flashRoot.gameObject);
        if (flashMaterial != null) Destroy(flashMaterial);
    }
}
