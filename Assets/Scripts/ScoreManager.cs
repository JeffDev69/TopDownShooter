using UnityEngine;
using TMPro;

// Update de round counter en houdt de text zichtbaar boven het HUD panel.
public class ScoreManager : MonoBehaviour
{
    public TextMeshProUGUI scoreText;

    private void Start()
    {
        if (scoreText == null) scoreText = GetComponent<TextMeshProUGUI>();
        if (scoreText != null)
        {
            // Restore the counter on top of its background, regardless of Inspector layout changes.
            GameObject panel = GameObject.Find("RoundsPanel");
            if (panel != null && panel.transform is RectTransform)
            {
                RectTransform panelRect = (RectTransform)panel.transform;
                panelRect.anchorMin = Vector2.one;
                panelRect.anchorMax = Vector2.one;
                panelRect.pivot = Vector2.one;
                panelRect.anchoredPosition = new Vector2(-30f, -30f);
                panelRect.sizeDelta = new Vector2(450f, 150f);
                panelRect.localScale = Vector3.one;
                RectTransform rect = scoreText.rectTransform;
                rect.SetParent(panel.transform, false);
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.offsetMin = new Vector2(15f, 15f);
                rect.offsetMax = new Vector2(-15f, -15f);
                rect.localScale = Vector3.one;
                rect.localRotation = Quaternion.identity;
                Vector3 position = rect.localPosition;
                position.z = 0f;
                rect.localPosition = position;
                rect.SetAsLastSibling();
                scoreText.color = new Color(0.08f, 0.10f, 0.14f, 1f);
            }
            scoreText.gameObject.SetActive(true);
            scoreText.enabled = true;
            // A separate sorting canvas prevents other HUD images from covering this text.
            Canvas textCanvas = scoreText.GetComponent<Canvas>();
            if (textCanvas == null) textCanvas = scoreText.gameObject.AddComponent<Canvas>();
            textCanvas.overrideSorting = true;
            textCanvas.sortingOrder = 100;
            CanvasGroup visibility = scoreText.GetComponent<CanvasGroup>();
            if (visibility == null) visibility = scoreText.gameObject.AddComponent<CanvasGroup>();
            visibility.ignoreParentGroups = true;
            visibility.alpha = 1f;
            scoreText.maskable = false;
            scoreText.alignment = TextAlignmentOptions.Center;
            scoreText.margin = Vector4.zero;
            scoreText.fontSize = 40f;
            scoreText.enableAutoSizing = true;
            scoreText.fontSizeMin = 12f;
            scoreText.fontSizeMax = 40f;
            scoreText.text = "ROUND 1 - GET READY";
        }
    }

    // Keep the existing component and text reference so scene wiring is preserved.
    public void ShowRound(int round, int remaining, bool cleared)
    {
        ShowRound(round, remaining, cleared, "");
    }

    public void ShowRound(int round, int remaining, bool cleared, string roundLabel)
    {
        if (scoreText == null) return;
        string prefix = string.IsNullOrEmpty(roundLabel) ? "" : roundLabel + "\n";
        scoreText.text = cleared
            ? prefix + "ROUND " + round + " CLEAR!"
            : prefix + "ROUND " + round + " | LEFT: " + remaining;
    }
}
