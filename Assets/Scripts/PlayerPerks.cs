using System.Collections.Generic;
using UnityEngine;

// Onthoudt purchased perks en activeert elk permanent effect maar één keer per run.
public class PlayerPerks : MonoBehaviour
{
    private readonly HashSet<PerkMachine.PerkType> owned = new HashSet<PerkMachine.PerkType>();

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
        }
        owned.Add(perk);
        return true;
    }
}
