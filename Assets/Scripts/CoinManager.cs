using TMPro;
using UnityEngine;

// Beheert de munten van de speler, verwerkt aankopen en werkt de muntenteller in de HUD bij.
public class CoinManager : MonoBehaviour
{
    public static CoinManager Instance { get; private set; }

    public TextMeshProUGUI coinText;
    public int startingCoins;

    public int Coins { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Coins = Mathf.Max(0, startingCoins);
    }

    void Start()
    {
        if (coinText != null)
        {
            GameObject panel = GameObject.Find("CoinsPanel");
            if (panel != null && panel.transform is RectTransform)
            {
                RectTransform rect = coinText.rectTransform;
                rect.SetParent(panel.transform, false);
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.offsetMin = new Vector2(10f, 10f);
                rect.offsetMax = new Vector2(-10f, -10f);
                rect.localRotation = Quaternion.identity;
                Vector3 position = rect.localPosition;
                position.z = 0f;
                rect.localPosition = position;
                rect.SetAsLastSibling();
            }
            coinText.gameObject.SetActive(true);
            coinText.enabled = true;
            coinText.margin = Vector4.zero;
            coinText.rectTransform.localScale = Vector3.one;
            coinText.color = new Color(0.08f, 0.10f, 0.14f, 1f);
            coinText.alignment = TextAlignmentOptions.Center;
            coinText.enableAutoSizing = true;
            coinText.fontSizeMin = 12f;
            coinText.fontSizeMax = 36f;
            UnityEngine.UI.Outline outline = coinText.GetComponent<UnityEngine.UI.Outline>();
            if (outline != null) outline.enabled = false;
        }
        UpdateText();
    }

    public void AddCoins(int amount)
    {
        Coins += Mathf.Max(0, amount);
        UpdateText();
    }

    public bool TrySpendCoins(int amount)
    {
        amount = Mathf.Max(0, amount);

        if (Coins < amount)
            return false;

        Coins -= amount;
        UpdateText();
        return true;
    }

    private void UpdateText()
    {
        if (coinText != null)
            coinText.text = "COINS: " + Coins;
    }
}
