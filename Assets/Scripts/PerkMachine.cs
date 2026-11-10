using TMPro;
using UnityEngine;

// Herbruikbare automaat waarmee de speler blijvende voordelen kan kopen.
public class PerkMachine : MonoBehaviour
{
    // Nieuwe voordelen staan achteraan zodat bestaande scènewaarden hetzelfde blijven.
    public enum PerkType
    {
        VitalityBrew,
        StaminaRush,
        QuickReload,
        DoubleDamage,
        RapidFire,
        ArmorPlating,
        AmmoBelt
    }
    public PerkType perkType;
    public float interactionDistance = 2f;
    public SpriteRenderer machineRenderer;

    private PlayerHealth playerHealth;
    private PlayerPerks playerPerks;
    private TextMeshPro prompt;
    private float messageUntil;

    private string PerkName => perkType switch
    {
        PerkType.VitalityBrew => "VITALITY BREW",
        PerkType.StaminaRush => "STAMINA RUSH",
        PerkType.QuickReload => "QUICK RELOAD",
        PerkType.DoubleDamage => "DOUBLE DAMAGE",
        PerkType.RapidFire => "RAPID FIRE",
        PerkType.ArmorPlating => "ARMOR PLATING",
        _ => "AMMO BELT"
    };

    private int Price => perkType switch
    {
        PerkType.VitalityBrew => 500,
        PerkType.StaminaRush => 400,
        PerkType.QuickReload => 450,
        PerkType.DoubleDamage => 600,
        PerkType.RapidFire => 650,
        PerkType.ArmorPlating => 700,
        _ => 500
    };

    private string Effect => perkType switch
    {
        PerkType.VitalityBrew => "+3 HEARTS + FULL HEAL",
        PerkType.StaminaRush => "+35% MOVEMENT SPEED",
        PerkType.QuickReload => "50% FASTER RELOAD",
        PerkType.DoubleDamage => "DOUBLE BULLET DAMAGE",
        PerkType.RapidFire => "25% FASTER FIRE RATE",
        PerkType.ArmorPlating => "50% LESS DAMAGE",
        _ => "+60% RESERVE AMMO + REFILL"
    };

    private void Start()
    {
        playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerPerks = playerHealth.GetComponent<PlayerPerks>();
            if (playerPerks == null) playerPerks = playerHealth.gameObject.AddComponent<PlayerPerks>();
        }
        if (machineRenderer == null) machineRenderer = GetComponent<SpriteRenderer>();
        GameObject label = new GameObject("Perk Prompt");
        label.transform.SetParent(transform, false);
        prompt = label.AddComponent<TextMeshPro>();
        prompt.fontSize = 3f;
        prompt.color = Color.white;
        prompt.alignment = TextAlignmentOptions.Center;
        prompt.rectTransform.sizeDelta = new Vector2(8f, 1.5f);
        prompt.GetComponent<MeshRenderer>().sortingOrder = 100;
        prompt.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (playerHealth == null || playerPerks == null ||
            !playerHealth.gameObject.activeInHierarchy || playerHealth.currentHealth <= 0)
        {
            if (prompt != null) prompt.gameObject.SetActive(false);
            return;
        }

        Vector3 nearest = machineRenderer != null
            ? machineRenderer.bounds.ClosestPoint(playerHealth.transform.position) : transform.position;
        bool nearby = Vector2.Distance(playerHealth.transform.position, nearest)
            <= Mathf.Max(0.1f, interactionDistance);
        prompt.gameObject.SetActive(nearby);
        if (!nearby) return;
        PlacePrompt();

        bool owned = playerPerks.HasPerk(perkType);
        if (Time.time >= messageUntil)
            prompt.text = owned ? "<color=#73FF91>" + PerkName + " OWNED</color>"
                : GameInput.InteractHint + " " + PerkName + " - " + Price + " COINS\n" + Effect;

        if (owned || Time.time < messageUntil || !GameInput.TryInteract()) return;
        if (CoinManager.Instance == null || !CoinManager.Instance.TrySpendCoins(Price))
        {
            GameAudio.Play(GameAudio.Cue.Error);
            prompt.text = "<color=#FF7373>NEED " + Price + " COINS</color>";
            messageUntil = Time.time + 2f;
            return;
        }
        if (playerPerks.BuyPerk(perkType))
        {
            GameAudio.Play(GameAudio.Cue.Purchase);
            prompt.text = "<color=#73FF91>" + PerkName + " PURCHASED!</color>";
        }
        messageUntil = Time.time + 2f;
    }

    private void PlacePrompt()
    {
        Vector3 scale = transform.lossyScale;
        prompt.transform.localScale = new Vector3(
            1f / Mathf.Max(0.001f, Mathf.Abs(scale.x)),
            1f / Mathf.Max(0.001f, Mathf.Abs(scale.y)), 1f);
        Vector3 position = transform.position + Vector3.down * 1.2f;
        if (machineRenderer != null)
            position = new Vector3(machineRenderer.bounds.center.x, machineRenderer.bounds.min.y - 0.8f, 0f);
        prompt.transform.SetPositionAndRotation(position, Quaternion.identity);
    }

    private void OnDisable()
    {
        if (prompt != null) prompt.gameObject.SetActive(false);
    }
}
