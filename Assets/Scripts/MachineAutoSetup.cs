using UnityEngine;

// Geeft bestaande machines hun juiste sprite, maar spawnt zelf geen nieuwe machines.
public static class MachineAutoSetup
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void PrepareExistingMachines()
    {
        // Sprites die handmatig in de Scene zijn gezet worden automatisch echte machines.
        foreach (SpriteRenderer renderer in Object.FindObjectsByType<SpriteRenderer>())
            ConfigureMachineFromSprite(renderer);

        foreach (PerkMachine machine in Object.FindObjectsByType<PerkMachine>())
        {
            string spriteName = GetSpriteName(machine.perkType);
            if (!string.IsNullOrEmpty(spriteName))
                PrepareVisual(machine.gameObject, spriteName);
        }

        foreach (HealthMachine machine in Object.FindObjectsByType<HealthMachine>())
            PrepareVisual(machine.gameObject, "HealthStation");
    }

    private static void ConfigureMachineFromSprite(SpriteRenderer renderer)
    {
        if (renderer == null || renderer.sprite == null) return;

        string spriteName = renderer.sprite.name;
        if (spriteName == "HealthStation")
        {
            HealthMachine healthMachine = renderer.GetComponent<HealthMachine>();
            if (healthMachine == null)
                healthMachine = renderer.gameObject.AddComponent<HealthMachine>();
            healthMachine.machineRenderer = renderer;
            return;
        }

        PerkMachine.PerkType perkType;
        switch (spriteName)
        {
            case "VitalityBrewMachine":
                perkType = PerkMachine.PerkType.VitalityBrew;
                break;
            case "RapidFireMachine":
                perkType = PerkMachine.PerkType.RapidFire;
                break;
            case "ArmorPlatingMachine":
                perkType = PerkMachine.PerkType.ArmorPlating;
                break;
            case "AmmoBeltMachine":
                perkType = PerkMachine.PerkType.AmmoBelt;
                break;
            default:
                return;
        }

        PerkMachine perkMachine = renderer.GetComponent<PerkMachine>();
        if (perkMachine == null)
            perkMachine = renderer.gameObject.AddComponent<PerkMachine>();
        perkMachine.perkType = perkType;
        perkMachine.machineRenderer = renderer;
    }

    private static string GetSpriteName(PerkMachine.PerkType perkType)
    {
        switch (perkType)
        {
            case PerkMachine.PerkType.VitalityBrew:
                return "VitalityBrewMachine";
            case PerkMachine.PerkType.RapidFire:
                return "RapidFireMachine";
            case PerkMachine.PerkType.ArmorPlating:
                return "ArmorPlatingMachine";
            case PerkMachine.PerkType.AmmoBelt:
                return "AmmoBeltMachine";
            default:
                return null;
        }
    }

    private static void PrepareVisual(GameObject machine, string spriteName)
    {
        SpriteRenderer renderer = machine.GetComponent<SpriteRenderer>();
        Sprite sprite = Resources.Load<Sprite>("PerkMachines/" + spriteName);
        if (renderer != null)
        {
            renderer.color = Color.white;
            if (sprite != null)
                renderer.sprite = sprite;
            else
                Debug.LogWarning("Could not load perk machine sprite: " + spriteName);
        }

        Animator animator = machine.GetComponent<Animator>();
        if (animator != null)
            animator.enabled = false;
    }
}
