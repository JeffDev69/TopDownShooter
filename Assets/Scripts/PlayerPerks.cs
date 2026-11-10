using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

// Onthoudt gekochte voordelen en activeert elk permanent effect maar één keer per spelsessie.
public class PlayerPerks : MonoBehaviour
{
    private readonly HashSet<PerkMachine.PerkType> owned = new HashSet<PerkMachine.PerkType>();
    private TextMeshProUGUI perkHud;

    private void Start()
    {
        CreatePerkHud();
        UpdatePerkHud();
    }

    public bool HasPerk(PerkMachine.PerkType perk)
    {
        if (perk == PerkMachine.PerkType.VitalityBrew)
        {
            PlayerHealth health = GetComponent<PlayerHealth>();
            if (health != null && health.HealthUpgradePurchased) return true;
        }
        return owned.Contains(perk);
    }

    public bool BuyPerk(PerkMachine.PerkType perk)
    {
        if (HasPerk(perk)) return false;
        PlayerHealth health = GetComponent<PlayerHealth>();
        PlayerMovement movement = GetComponent<PlayerMovement>();
        PlayerShoot shooting = GetComponent<PlayerShoot>();

        switch (perk)
        {
            case PerkMachine.PerkType.VitalityBrew:
                if (health == null || !health.ApplyHealthUpgrade(3)) return false;
                break;
            case PerkMachine.PerkType.StaminaRush:
                if (movement == null) return false;
                movement.moveSpeed *= 1.35f;
                break;
            case PerkMachine.PerkType.QuickReload:
                if (shooting == null) return false;
                shooting.ApplyFastReloadPerk();
                break;
            case PerkMachine.PerkType.DoubleDamage:
                if (shooting == null) return false;
                shooting.ApplyDoubleDamagePerk();
                break;
            case PerkMachine.PerkType.RapidFire:
                if (shooting == null) return false;
                shooting.ApplyRapidFirePerk();
                break;
            case PerkMachine.PerkType.ArmorPlating:
                if (health == null) return false;
                health.ApplyArmorPerk();
                break;
            case PerkMachine.PerkType.AmmoBelt:
                if (shooting == null) return false;
                shooting.ApplyAmmoBeltPerk();
                break;
        }
        owned.Add(perk);
        UpdatePerkHud();
        return true;
    }

    // Maakt automatisch een HUD onder de hartjes; instellen via de Inspector is niet nodig.
    private void CreatePerkHud()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        GameObject hudObject = new GameObject("PerkHUD");
        hudObject.layer = canvas.gameObject.layer;
        hudObject.transform.SetParent(canvas.transform, false);

        RectTransform rect = hudObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(0f, -140f);
        rect.sizeDelta = new Vector2(520f, 300f);

        perkHud = hudObject.AddComponent<TextMeshProUGUI>();
        perkHud.raycastTarget = false;
        perkHud.richText = true;
        perkHud.fontSize = 24f;
        perkHud.alignment = TextAlignmentOptions.TopLeft;
        perkHud.margin = Vector4.zero;

        UnityEngine.UI.Outline outline = hudObject.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
        outline.effectDistance = new Vector2(2f, -2f);
    }

    private void UpdatePerkHud()
    {
        if (perkHud == null) return;
        StringBuilder text = new StringBuilder();
        if (HasPerk(PerkMachine.PerkType.VitalityBrew))
            text.AppendLine("<color=#FF5B5B><b>[+] VITALITY BREW</b></color>");
        if (HasPerk(PerkMachine.PerkType.StaminaRush))
            text.AppendLine("<color=#61E86F><b>[>>] STAMINA RUSH</b></color>");
        if (HasPerk(PerkMachine.PerkType.QuickReload))
            text.AppendLine("<color=#55C7FF><b>[R] QUICK RELOAD</b></color>");
        if (HasPerk(PerkMachine.PerkType.DoubleDamage))
            text.AppendLine("<color=#D36AFF><b>[x2] DOUBLE DAMAGE</b></color>");
        if (HasPerk(PerkMachine.PerkType.RapidFire))
            text.AppendLine("<color=#FF9E45><b>[>>>] RAPID FIRE</b></color>");
        if (HasPerk(PerkMachine.PerkType.ArmorPlating))
            text.AppendLine("<color=#7FB7FF><b>[#] ARMOR PLATING</b></color>");
        if (HasPerk(PerkMachine.PerkType.AmmoBelt))
            text.AppendLine("<color=#62E5D1><b>[AMMO] AMMO BELT</b></color>");

        perkHud.text = text.ToString().TrimEnd();
        perkHud.gameObject.SetActive(text.Length > 0);
    }
}
