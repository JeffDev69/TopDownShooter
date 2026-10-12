using UnityEngine;
using UnityEngine.SceneManagement;

// Activeert de machines bij iedere scene-load, ook na Restart.
public static class MachineAutoSetup
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneSetup()
    {
        SceneManager.sceneLoaded -= PrepareExistingMachines;
        SceneManager.sceneLoaded += PrepareExistingMachines;
    }

    private static void PrepareExistingMachines(Scene scene, LoadSceneMode mode)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            // Include inactive objects so their machines also work when enabled later.
            foreach (SpriteRenderer renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
                ConfigureMachineFromSprite(renderer);

            foreach (PerkMachine machine in root.GetComponentsInChildren<PerkMachine>(true))
            {
                if (machine.machineRenderer == null)
                    machine.machineRenderer = machine.GetComponent<SpriteRenderer>();
                PrepareVisual(machine.machineRenderer);
            }

            foreach (HealthMachine machine in root.GetComponentsInChildren<HealthMachine>(true))
            {
                if (machine.machineRenderer == null)
                    machine.machineRenderer = machine.GetComponent<SpriteRenderer>();
                PrepareVisual(machine.machineRenderer);
            }

            foreach (PackAPunchMachine machine in root.GetComponentsInChildren<PackAPunchMachine>(true))
                PreparePackAPunchVisual(machine);
        }
    }

    private static void ConfigureMachineFromSprite(SpriteRenderer renderer)
    {
        if (renderer == null || renderer.sprite == null) return;

        string spriteName = renderer.sprite.name;
        if (spriteName == "PackAPunchMachine" || spriteName == "PackAPunchMachine_0")
        {
            PackAPunchMachine packAPunch = renderer.GetComponent<PackAPunchMachine>();
            if (packAPunch == null)
                packAPunch = renderer.gameObject.AddComponent<PackAPunchMachine>();
            packAPunch.machineRenderer = renderer;
            return;
        }

        if (spriteName == "HealthStation" || spriteName == "HealthStation_0")
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
            case "VitalityBrewMachine_0":
                perkType = PerkMachine.PerkType.VitalityBrew;
                break;
            case "rush":
            case "rush_0":
                perkType = PerkMachine.PerkType.StaminaRush;
                break;
            case "reload":
            case "reload_0":
                perkType = PerkMachine.PerkType.QuickReload;
                break;
            case "DoubleDamageMachine":
            case "DoubleDamageMachine_0":
                perkType = PerkMachine.PerkType.DoubleDamage;
                break;
            case "RapidFireMachine":
            case "RapidFireMachine_0":
                perkType = PerkMachine.PerkType.RapidFire;
                break;
            case "ArmorPlatingMachine":
            case "ArmorPlatingMachine_0":
                perkType = PerkMachine.PerkType.ArmorPlating;
                break;
            case "AmmoBeltMachine":
            case "AmmoBeltMachine_0":
                perkType = PerkMachine.PerkType.AmmoBelt;
                break;
            default:
                return;
        }

        PerkMachine perkMachine = renderer.GetComponent<PerkMachine>();
        if (perkMachine == null)
        {
            perkMachine = renderer.gameObject.AddComponent<PerkMachine>();
            perkMachine.perkType = perkType;
        }
        perkMachine.machineRenderer = renderer;
    }

    private static void PrepareVisual(SpriteRenderer renderer)
    {
        if (renderer == null) return;
        renderer.color = Color.white;

        Animator animator = renderer.GetComponent<Animator>();
        if (animator != null)
            animator.enabled = false;
    }

    private static void PreparePackAPunchVisual(PackAPunchMachine machine)
    {
        if (machine == null) return;

        SpriteRenderer renderer = machine.machineRenderer;
        if (renderer == null)
            renderer = machine.GetComponent<SpriteRenderer>();
        if (renderer == null)
            renderer = machine.gameObject.AddComponent<SpriteRenderer>();

        renderer.color = Color.white;
        renderer.sortingOrder = Mathf.Max(renderer.sortingOrder, 20);
        machine.machineRenderer = renderer;
    }
}
