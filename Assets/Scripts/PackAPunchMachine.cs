using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// Machine waarmee de player het huidige wapen permanent sterker maakt.
public class PackAPunchMachine : MonoBehaviour
{
    public int tierOnePrice = 1500;
    public int tierTwoPrice = 3000;
    public int tierThreePrice = 5000;
    public float interactionDistance = 2f;
    public SpriteRenderer machineRenderer;

    private PlayerShoot nearbyPlayer;
    private PlayerShoot player;
    private TextMeshPro worldPrompt;
    private float messageUntil;

    private void Start()
    {
        player = FindAnyObjectByType<PlayerShoot>();
        if (machineRenderer == null) machineRenderer = GetComponent<SpriteRenderer>();
        if (machineRenderer != null)
            machineRenderer.color = Color.white;
        else
            Debug.LogWarning("Pack-a-Punch machine needs a SpriteRenderer.");

        Collider2D collider = GetComponent<Collider2D>();
        if (collider == null)
        {
            BoxCollider2D box = gameObject.AddComponent<BoxCollider2D>();
            box.size = new Vector2(1.4f, 1.9f);
        }

        GameObject label = new GameObject("Pack-a-Punch Prompt");
        label.transform.SetParent(transform, false);
        worldPrompt = label.AddComponent<TextMeshPro>();
        worldPrompt.fontSize = 3f;
        worldPrompt.color = Color.white;
        worldPrompt.alignment = TextAlignmentOptions.Center;
        worldPrompt.rectTransform.sizeDelta = new Vector2(10f, 2f);
        worldPrompt.GetComponent<MeshRenderer>().sortingOrder = 100;
        worldPrompt.gameObject.SetActive(false);

        ShowMessage("");
    }

    private void Update()
    {
        if (player == null || !player.gameObject.activeInHierarchy)
        {
            nearbyPlayer = null;
            ShowMessage("");
            return;
        }

        Vector3 nearest = machineRenderer != null
            ? machineRenderer.bounds.ClosestPoint(player.transform.position)
            : transform.position;
        bool nearby = Vector2.Distance(player.transform.position, nearest)
            <= Mathf.Max(0.1f, interactionDistance);
        if (!nearby)
        {
            nearbyPlayer = null;
            ShowMessage("");
            return;
        }

        if (nearbyPlayer == null || Time.time >= messageUntil)
            ShowMessage(BuildPromptText());

        PlacePrompt();
        nearbyPlayer = player;

        if (Keyboard.current == null || !Keyboard.current.eKey.wasPressedThisFrame ||
            Time.time < messageUntil)
            return;

        TryPack();
    }

    private void TryPack()
    {
        if (!player.CanPackCurrentWeapon())
        {
            ShowMessage(player.CurrentWeapon == PlayerShoot.WeaponType.Empty
                ? "<color=#FF7373>NO WEAPON TO PACK</color>"
                : "<color=#73FF91>MAX PACK-A-PUNCH</color>");
            messageUntil = Time.time + 2f;
            return;
        }

        int price = CurrentPrice();
        if (CoinManager.Instance == null || !CoinManager.Instance.TrySpendCoins(price))
        {
            GameAudio.Play(GameAudio.Cue.Error);
            ShowMessage("<color=#FF7373>NEED " + price + " COINS</color>");
            messageUntil = Time.time + 2f;
            return;
        }

        player.PackCurrentWeapon();
        GameAudio.Play(GameAudio.Cue.Purchase, 1f, 0.82f);
        ShowMessage("<color=#73FF91>" + player.CurrentWeaponFullName + "!</color>");
        messageUntil = Time.time + 2f;
    }

    private string BuildPromptText()
    {
        if (player.CurrentWeapon == PlayerShoot.WeaponType.Empty)
            return "<color=#FF7373>NO WEAPON TO PACK</color>";

        if (!player.CanPackCurrentWeapon())
            return "<color=#73FF91>" + player.CurrentWeaponFullName + " MAXED</color>\n"
                + player.LoadoutText;

        int nextTier = player.CurrentPackTier + 1;
        int price = CurrentPrice();
        return "<color=#F05CFF>[E] PACK-A-PUNCH - " + price + " COINS</color>\n"
            + player.LoadoutText + "\nUPGRADE TO PAP " + nextTier + ": " + player.CurrentWeaponFullName;
    }

    private int CurrentPrice()
    {
        switch (player.CurrentPackTier)
        {
            case 0: return Mathf.Max(0, tierOnePrice);
            case 1: return Mathf.Max(0, tierTwoPrice);
            default: return Mathf.Max(0, tierThreePrice);
        }
    }

    private void ShowMessage(string message)
    {
        if (worldPrompt != null)
        {
            worldPrompt.text = message;
            worldPrompt.gameObject.SetActive(!string.IsNullOrEmpty(message));
        }
    }

    private void PlacePrompt()
    {
        if (worldPrompt == null) return;
        Vector3 scale = transform.lossyScale;
        worldPrompt.transform.localScale = new Vector3(
            1f / Mathf.Max(0.001f, Mathf.Abs(scale.x)),
            1f / Mathf.Max(0.001f, Mathf.Abs(scale.y)), 1f);
        Vector3 position = transform.position + Vector3.down * 1.2f;
        if (machineRenderer != null)
            position = new Vector3(machineRenderer.bounds.center.x, machineRenderer.bounds.min.y - 0.8f, 0f);
        worldPrompt.transform.SetPositionAndRotation(position, Quaternion.identity);
    }

    private void OnDisable()
    {
        if (worldPrompt != null) worldPrompt.gameObject.SetActive(false);
    }
}
