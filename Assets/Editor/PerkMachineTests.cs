using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class PerkMachineTests
{
    [UnityTest]
    public IEnumerator MachinesShowPromptsAndSellPerksAfterRestart()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/game.unity");
        yield return new EnterPlayMode();

        // Exercise the actual restart path twice, including fresh perk ownership.
        for (int run = 0; run < 3; run++)
        {
            yield return null;
            EnemySpawner spawner = Object.FindFirstObjectByType<EnemySpawner>();
            spawner.StopAllCoroutines();
            foreach (EnemyHealth enemy in Object.FindObjectsByType<EnemyHealth>())
                enemy.gameObject.SetActive(false);

            PerkMachine[] machines = Object.FindObjectsByType<PerkMachine>();
            Assert.That(machines.Length, Is.EqualTo(7), "Perk machine count in run " + run);
            Assert.That(machines.Select(machine => machine.perkType).Distinct().Count(), Is.EqualTo(7));
            Assert.That(Object.FindObjectsByType<HealthMachine>().Length, Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<PackAPunchMachine>().Length, Is.EqualTo(1));

            PlayerHealth player = Object.FindFirstObjectByType<PlayerHealth>();
            PlayerPerks perks = player.GetComponent<PlayerPerks>();
            player.GetComponent<PlayerMovement>().enabled = false;
            player.GetComponent<PlayerShoot>().enabled = false;
            player.GetComponent<Rigidbody2D>().simulated = false;
            CoinManager.Instance.AddCoins(10000);
            int[] prices = { 500, 400, 450, 600, 650, 700, 500 };

            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                foreach (PerkMachine machine in machines)
                {
                    Assert.That(perks.HasPerk(machine.perkType), Is.False);
                    Bounds bounds = machine.machineRenderer.bounds;
                    player.transform.position = new Vector3(bounds.center.x, bounds.min.y - 0.5f, 0f);
                    yield return null;
                    yield return null;

                    TextMeshPro prompt = machine.GetComponentInChildren<TextMeshPro>(true);
                    Assert.That(prompt, Is.Not.Null, machine.name);
                    Assert.That(prompt.gameObject.activeInHierarchy, Is.True, machine.name);
                    Assert.That(prompt.text, Does.Contain("[E]"), machine.name);
                    int before = CoinManager.Instance.Coins;
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E));
                    yield return null;
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                    yield return null;

                    Assert.That(perks.HasPerk(machine.perkType), Is.True, machine.name);
                    Assert.That(CoinManager.Instance.Coins, Is.EqualTo(before - prices[(int)machine.perkType]), machine.name);
                    Assert.That(prompt.text, Does.Contain("PURCHASED"), machine.name);

                    player.transform.position = new Vector3(200f, 200f, 0f);
                    yield return null;
                    Assert.That(prompt.gameObject.activeSelf, Is.False, machine.name);
                }
                Assert.That(player.maxHealth, Is.EqualTo(8));
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
            }

            if (run < 2)
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        yield return new ExitPlayMode();
    }

    [UnityTearDown]
    public IEnumerator ExitPlayModeAfterFailure()
    {
        if (Application.isPlaying)
            yield return new ExitPlayMode();
    }
}
