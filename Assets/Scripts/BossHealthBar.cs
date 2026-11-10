using UnityEngine;
using UnityEngine.UI;

// Maakt voor de baas een gezondheidsbalk in de spelwereld die boven hem blijft staan.
public class BossHealthBar : MonoBehaviour
{
    public float heightAboveBoss = 1.8f;
    public Vector2 barSize = new Vector2(220f, 24f);
    public Color fillColor = new Color(0.72f, 0.05f, 0.05f, 1f);

    private EnemyHealth health;
    private int maximumHealth;
    private GameObject barObject;
    private RectTransform fillRect;

    void Start()
    {
        health = GetComponent<EnemyHealth>();
        if (health == null) return;
        maximumHealth = Mathf.Max(1, health.CurrentHealth);
        CreateBar();
    }

    void LateUpdate()
    {
        if (barObject == null || health == null) return;
        if (health.IsDead)
        {
            barObject.SetActive(false);
            return;
        }

        barObject.transform.position = transform.position + Vector3.up * heightAboveBoss;
        barObject.transform.rotation = Quaternion.identity;
        float amount = Mathf.Clamp01((float)health.CurrentHealth / maximumHealth);
        fillRect.anchorMax = new Vector2(amount, 1f);
    }

    private void CreateBar()
    {
        barObject = new GameObject("BossHealthBar");
        Canvas canvas = barObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        canvas.sortingOrder = 100;

        RectTransform canvasRect = barObject.GetComponent<RectTransform>();
        canvasRect.sizeDelta = barSize;
        canvasRect.localScale = Vector3.one * 0.01f;

        GameObject background = CreateImage("Background", canvasRect, new Color(0.05f, 0.05f, 0.05f, 0.92f));
        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        GameObject border = CreateImage("Border", canvasRect, new Color(0.85f, 0.72f, 0.22f, 1f));
        RectTransform borderRect = border.GetComponent<RectTransform>();
        borderRect.offsetMin = new Vector2(-4f, -4f);
        borderRect.offsetMax = new Vector2(4f, 4f);
        border.transform.SetAsFirstSibling();

        GameObject fill = CreateImage("Health", backgroundRect, fillColor);
        fillRect = fill.GetComponent<RectTransform>();
        fillRect.offsetMin = new Vector2(3f, 3f);
        fillRect.offsetMax = new Vector2(-3f, -3f);

        barObject.transform.position = transform.position + Vector3.up * heightAboveBoss;
    }

    private static GameObject CreateImage(string objectName, Transform parent, Color color)
    {
        GameObject imageObject = new GameObject(objectName);
        imageObject.transform.SetParent(parent, false);
        RectTransform rect = imageObject.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        Image image = imageObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return imageObject;
    }

    void OnDestroy()
    {
        if (barObject != null) Destroy(barObject);
    }
}
