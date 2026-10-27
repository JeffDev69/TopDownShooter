using TMPro;
using UnityEngine;

// Laat de player coins uitgeven voor een random weapon en toont dichtbij een world-space prompt.
public class MysteryBox : MonoBehaviour
{
    public int price = 100;
    public TextMeshProUGUI messageText;
    public SpriteRenderer boxRenderer;
    public Color openedColor = Color.gray;
    public float interactionDistance = 2f;
    public float rollCooldown = 1f;

    private PlayerShoot nearbyPlayer;
    private PlayerShoot player;
    private Color originalBoxColor;
    private float nextRollTime;
    private float messageUntil;
    private TextMeshPro worldPrompt;

    void Start()
    {
        player = FindFirstObjectByType<PlayerShoot>();
        if (boxRenderer == null)
            boxRenderer = GetComponent<SpriteRenderer>();
        if (boxRenderer != null)
            originalBoxColor = boxRenderer.color;

        // Replace the old screen-space label with the same world-space style as perk machines.
        if (messageText != null)
            messageText.gameObject.SetActive(false);
        GameObject label = new GameObject("Mystery Box Prompt");
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

    void Update()
    {
        if (boxRenderer != null && Time.time >= nextRollTime)
            boxRenderer.color = originalBoxColor;

        if (player == null || !player.gameObject.activeInHierarchy)
        {
            nearbyPlayer = null;
            ShowMessage("");
            return;
        }

        Vector3 boxPosition = boxRenderer != null
            ? boxRenderer.bounds.ClosestPoint(player.transform.position)
            : transform.position;
        bool inRange = Vector2.Distance(player.transform.position, boxPosition)
            <= Mathf.Max(0.1f, interactionDistance);

        if (!inRange)
        {
            if (nearbyPlayer != null)
                ShowMessage("");
            nearbyPlayer = null;
            return;
        }

        if (nearbyPlayer == null || Time.time >= messageUntil)
            ShowMessage("<color=#5EEBFF>" + GameInput.InteractHint + " MYSTERY BOX - " + price + " COINS</color>\n"
                + player.LoadoutText + "\nHOLDING: " + player.CurrentWeaponFullName);

        PlacePrompt();

        nearbyPlayer = player;

        if (Time.time >= nextRollTime && GameInput.TryInteract())
            TryOpen();
    }

    private void TryOpen()
    {
        if (Time.time < nextRollTime)
            return;

        if (CoinManager.Instance == null)
        {
            ShowMessage("ADD A COIN MANAGER");
            messageUntil = Time.time + 2f;
            return;
        }

        if (!CoinManager.Instance.TrySpendCoins(price))
        {
            GameAudio.Play(GameAudio.Cue.Error);
            ShowMessage("NEED " + price + " COINS");
            messageUntil = Time.time + 2f;
            return;
        }

        nearbyPlayer.GiveRandomMysteryWeapon();
        GameAudio.Play(GameAudio.Cue.Purchase);
        nextRollTime = Time.time + Mathf.Max(0.1f, rollCooldown);
        messageUntil = Time.time + 2f;

        if (boxRenderer != null)
            boxRenderer.color = openedColor;

        ShowMessage("YOU GOT: " + nearbyPlayer.CurrentWeaponFullName
            + "!\n" + nearbyPlayer.LoadoutText);
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
        if (boxRenderer != null)
            position = new Vector3(boxRenderer.bounds.center.x, boxRenderer.bounds.min.y - 0.8f, 0f);
        worldPrompt.transform.SetPositionAndRotation(position, Quaternion.identity);
    }

    private void OnDisable()
    {
        if (worldPrompt != null) worldPrompt.gameObject.SetActive(false);
    }
}
