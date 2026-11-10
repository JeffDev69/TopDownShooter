using UnityEngine;

// Tekent een gelaagde vlamkegel die tijdens het schieten nog even zichtbaar blijft.
public class FlamethrowerVisual : MonoBehaviour
{
    public float visibleDuration = 0.2f;

    private Transform flameRoot;
    private LineRenderer[] flames;
    private Material flameMaterial;
    private Texture2D flameTexture;
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
            float side = Mathf.Lerp(-0.5f, 0.5f, (float)i / (flames.Length - 1));
            float angle = side * coneAngle * 0.78f + Random.Range(-4f, 4f);
            float radians = angle * Mathf.Deg2Rad;
            float edgeShortening = 1f - Mathf.Abs(side) * 0.18f;
            float length = range * edgeShortening * Random.Range(0.9f, 1.03f);
            Vector3 direction = new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f);
            Vector3 normal = new Vector3(-direction.y, direction.x, 0f);
            float wave = Random.Range(-0.13f, 0.13f);

            LineRenderer flame = flames[i];
            flame.SetPosition(0, new Vector3(0.08f, 0f, 0f));
            flame.SetPosition(1, direction * (length * 0.22f) + normal * wave * 0.35f);
            flame.SetPosition(2, direction * (length * 0.48f) - normal * wave * 0.75f);
            flame.SetPosition(3, direction * (length * 0.74f) + normal * wave);
            flame.SetPosition(4, direction * length + normal * Random.Range(-0.08f, 0.08f));
            flame.enabled = true;
        }

        hideAt = Time.time + Mathf.Max(0.04f, visibleDuration);
    }

    private bool CreateFlames(Transform muzzle)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) return false;

        flameTexture = CreateFlameTexture();
        flameMaterial = new Material(shader)
        {
            name = "Procedural Flamethrower Material",
            mainTexture = flameTexture
        };

        flameRoot = new GameObject("Flamethrower Flames").transform;
        flameRoot.SetParent(muzzle, false);
        flames = new LineRenderer[5];

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
            line.positionCount = 5;
            line.numCornerVertices = 5;
            line.numCapVertices = 4;
            line.textureMode = LineTextureMode.Stretch;
            line.widthCurve = CreateWidthCurve();
            line.widthMultiplier = i == 2 ? 0.76f : (i == 1 || i == 3 ? 0.58f : 0.4f);
            line.colorGradient = CreateColorGradient(i);
            line.sortingLayerID = sortingLayer;
            line.sortingOrder = baseOrder + i;
            line.enabled = false;
            flames[i] = line;
        }
        return true;
    }

    private static AnimationCurve CreateWidthCurve()
    {
        return new AnimationCurve(
            new Keyframe(0f, 0.12f),
            new Keyframe(0.12f, 1f),
            new Keyframe(0.52f, 0.7f),
            new Keyframe(0.82f, 0.34f),
            new Keyframe(1f, 0f));
    }

    private static Gradient CreateColorGradient(int index)
    {
        bool core = index == 2;
        bool inner = index == 1 || index == 3;
        Color middle = core
            ? new Color(1f, 0.94f, 0.48f)
            : inner ? new Color(1f, 0.58f, 0.04f) : new Color(1f, 0.22f, 0.01f);

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.96f, 0.72f), 0f),
                new GradientColorKey(middle, 0.36f),
                new GradientColorKey(new Color(1f, 0.25f, 0.01f), 0.76f),
                new GradientColorKey(new Color(0.45f, 0.04f, 0.01f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(core ? 1f : 0.86f, 0f),
                new GradientAlphaKey(core ? 0.94f : 0.78f, 0.58f),
                new GradientAlphaKey(0.38f, 0.86f),
                new GradientAlphaKey(0f, 1f)
            });
        return gradient;
    }

    private static Texture2D CreateFlameTexture()
    {
        const int width = 128;
        const int height = 32;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false, true)
        {
            name = "Procedural Flame Texture",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        Color32[] pixels = new Color32[width * height];
        for (int y = 0; y < height; y++)
        {
            float vertical = Mathf.Abs((y + 0.5f) / height * 2f - 1f);
            float softEdge = Mathf.Pow(Mathf.Clamp01(1f - vertical), 0.72f);
            for (int x = 0; x < width; x++)
            {
                float horizontal = (x + 0.5f) / width;
                float startFade = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(horizontal / 0.06f));
                float tipFade = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((horizontal - 0.68f) / 0.32f));
                float noise = Mathf.Lerp(0.7f, 1f, Mathf.PerlinNoise(horizontal * 11f + 3.7f, vertical * 4f + 1.9f));
                byte alpha = (byte)Mathf.RoundToInt(255f * softEdge * startFade * tipFade * noise);
                pixels[y * width + x] = new Color32(255, 255, 255, alpha);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return texture;
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
        if (flameTexture != null) Destroy(flameTexture);
    }
}
