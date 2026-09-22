using UnityEngine;

// Maakt een procedural flame cone die kort zichtbaar blijft tijdens het vuren.
public class FlamethrowerVisual : MonoBehaviour
{
    public float visibleDuration = 0.14f;

    private Transform flameRoot;
    private LineRenderer[] flames;
    private Material flameMaterial;
    private float hideAt;

    public void Show(Transform muzzle, float range, float coneAngle)
    {
        if (muzzle == null) return;
        if (flameRoot == null && !CreateFlames(muzzle)) return;

        flameRoot.SetParent(muzzle, false);
        flameRoot.localPosition = Vector3.zero;
        flameRoot.localRotation = Quaternion.identity;
        Vector3 scale = muzzle.lossyScale;
        flameRoot.localScale = new Vector3(
            1f / Mathf.Max(0.001f, Mathf.Abs(scale.x)),
            1f / Mathf.Max(0.001f, Mathf.Abs(scale.y)), 1f);

        for (int i = 0; i < flames.Length; i++)
        {
            float side = flames.Length == 1 ? 0f : Mathf.Lerp(-0.5f, 0.5f, (float)i / (flames.Length - 1));
            float angle = side * coneAngle + Random.Range(-4f, 4f);
            float radians = angle * Mathf.Deg2Rad;
            float length = range * Random.Range(0.72f, 1f);
            Vector3 end = new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f) * length;
            Vector3 middle = end * 0.52f + Vector3.up * Random.Range(-0.18f, 0.18f);
            LineRenderer flame = flames[i];
            flame.SetPosition(0, new Vector3(0.12f, 0f, 0f));
            flame.SetPosition(1, middle);
            flame.SetPosition(2, end);
            flame.startWidth = i == 1 ? 0.5f : 0.34f;
            flame.endWidth = 0.04f;
            flame.enabled = true;
        }

        hideAt = Time.time + Mathf.Max(0.04f, visibleDuration);
    }

    private bool CreateFlames(Transform muzzle)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) return false;

        flameMaterial = new Material(shader);
        flameRoot = new GameObject("Flamethrower Flames").transform;
        flameRoot.SetParent(muzzle, false);
        flames = new LineRenderer[3];

        Color[] colors =
        {
            new Color(1f, 0.18f, 0.01f, 0.82f),
            new Color(1f, 0.68f, 0.03f, 0.95f),
            new Color(1f, 0.28f, 0.01f, 0.82f)
        };

        PlayerAim aim = GetComponent<PlayerAim>();
        int baseOrder = aim != null && aim.VisualRenderer != null ? aim.VisualRenderer.sortingOrder + 25 : 25;
        int sortingLayer = aim != null && aim.VisualRenderer != null ? aim.VisualRenderer.sortingLayerID : 0;

        for (int i = 0; i < flames.Length; i++)
        {
            GameObject part = new GameObject("Flame " + (i + 1));
            part.transform.SetParent(flameRoot, false);
            LineRenderer line = part.AddComponent<LineRenderer>();
            line.sharedMaterial = flameMaterial;
            line.useWorldSpace = false;
            line.positionCount = 3;
            line.numCornerVertices = 3;
            line.numCapVertices = 3;
            line.startColor = colors[i];
            line.endColor = new Color(1f, 0.05f, 0f, 0f);
            line.sortingLayerID = sortingLayer;
            line.sortingOrder = baseOrder + i;
            line.enabled = false;
            flames[i] = line;
        }
        return true;
    }

    void LateUpdate()
    {
        if (flames == null || Time.time < hideAt) return;
        foreach (LineRenderer flame in flames)
            if (flame != null) flame.enabled = false;
    }

    void OnDisable()
    {
        if (flames == null) return;
        foreach (LineRenderer flame in flames)
            if (flame != null) flame.enabled = false;
    }

    void OnDestroy()
    {
        if (flameRoot != null) Destroy(flameRoot.gameObject);
        if (flameMaterial != null) Destroy(flameMaterial);
    }
}
