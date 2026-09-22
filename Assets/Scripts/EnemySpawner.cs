using System.Collections;
using UnityEngine;

// Beheert endless rounds, mixt zombie types en trackt hoeveel enemies over zijn.
public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public GameObject tankPrefab;
    [Range(0f, 1f)] public float tankSpawnChance = 0.2f;

    [Header("Special Zombies")]
    public GameObject exploderPrefab;
    public GameObject armoredPrefab;
    public GameObject spitterPrefab;
    public GameObject bossPrefab;
    [Range(0f, 1f)] public float exploderSpawnChance = 0.12f;
    [Range(0f, 1f)] public float armoredSpawnChance = 0.13f;
    [Range(0f, 1f)] public float spitterSpawnChance = 0.10f;
    public int bossEveryRounds = 5;

    public float spawnInterval = 2f;
    public int firstRoundZombies = 5;
    public int extraZombiesPerRound = 3;
    public float breakBetweenRounds = 3f;

    [Header("Difficulty Scaling")]
    public int healthIncreaseEveryRounds = 2;
    public int healthAddedPerStep = 1;
    public float speedIncreasePerRound = 0.05f;
    public float maximumSpeedMultiplier = 1.8f;
    public float spawnIntervalDecreasePerRound = 0.1f;
    public float minimumSpawnInterval = 0.55f;
    public float tankChanceIncreasePerRound = 0.02f;
    public float maximumTankChance = 0.5f;

    public int CurrentRound { get; private set; }
    public int ZombiesRemaining { get; private set; }
    private ScoreManager roundDisplay;
    private PlayerHealth player;

    private bool PlayerAlive => player == null ||
        (player.gameObject.activeInHierarchy && player.currentHealth > 0);

    public float minX = -8f;
    public float maxX = 8f;
    public float minY = -4f;
    public float maxY = 4f;

    IEnumerator Start()
    {
        roundDisplay = FindFirstObjectByType<ScoreManager>();
        player = FindFirstObjectByType<PlayerHealth>();
        if ((enemyPrefab == null && tankPrefab == null) ||
            (enemyPrefab != null && enemyPrefab.GetComponent<EnemyHealth>() == null) ||
            (tankPrefab != null && tankPrefab.GetComponent<EnemyHealth>() == null))
        {
            Debug.LogError("Assign zombie prefabs with EnemyHealth to EnemySpawner.", this);
            yield break;
        }

        yield return new WaitForSeconds(1f);
        while (PlayerAlive)
        {
            CurrentRound++;
            int roundSize = Mathf.Max(1, firstRoundZombies) +
                (CurrentRound - 1) * Mathf.Max(0, extraZombiesPerRound);
            ZombiesRemaining = roundSize;
            UpdateRoundDisplay(false);

            for (int i = 0; i < roundSize; i++)
            {
                if (!PlayerAlive) yield break;
                bool forceBoss = bossPrefab != null && bossEveryRounds > 0 &&
                    CurrentRound % bossEveryRounds == 0 && i == roundSize - 1;
                SpawnEnemy(forceBoss);
                if (i < roundSize - 1)
                    yield return new WaitForSeconds(CurrentSpawnInterval());
            }

            while (ZombiesRemaining > 0 && PlayerAlive)
                yield return null;

            if (!PlayerAlive) yield break;
            UpdateRoundDisplay(true);
            yield return new WaitForSeconds(Mathf.Max(0f, breakBetweenRounds));
        }
    }

    public void RegisterKill()
    {
        if (ZombiesRemaining <= 0) return;
        ZombiesRemaining--;
        UpdateRoundDisplay(false);
    }

    private void UpdateRoundDisplay(bool cleared)
    {
        if (roundDisplay != null)
            roundDisplay.ShowRound(CurrentRound, ZombiesRemaining, cleared);
    }

    void SpawnEnemy(bool forceBoss)
    {
        Vector2 spawnPosition;

        // Pick a random side of the arena
        int side = Random.Range(0, 4);

        if (side == 0) // Top
        {
            spawnPosition = new Vector2(
                Random.Range(minX, maxX),
                maxY
            );
        }
        else if (side == 1) // Bottom
        {
            spawnPosition = new Vector2(
                Random.Range(minX, maxX),
                minY
            );
        }
        else if (side == 2) // Left
        {
            spawnPosition = new Vector2(
                minX,
                Random.Range(minY, maxY)
            );
        }
        else // Right
        {
            spawnPosition = new Vector2(
                maxX,
                Random.Range(minY, maxY)
            );
        }

        GameObject selectedPrefab = forceBoss ? bossPrefab : PickRandomPrefab();

        if (selectedPrefab == null)
            selectedPrefab = enemyPrefab != null ? enemyPrefab : tankPrefab;

        if (selectedPrefab != null)
        {
            GameObject zombie = Instantiate(selectedPrefab, spawnPosition, Quaternion.identity);
            EnemyHealth zombieHealth = zombie.GetComponent<EnemyHealth>();
            if (zombieHealth == null)
            {
                Debug.LogError(selectedPrefab.name + " needs an EnemyHealth component.", selectedPrefab);
                Destroy(zombie);
                RegisterKill();
                return;
            }
            zombieHealth.SetRoundOwner(this);

            // Elke paar rounds krijgt iedere zombie extra health.
            int healthSteps = (CurrentRound - 1) / Mathf.Max(1, healthIncreaseEveryRounds);
            zombieHealth.health += healthSteps * Mathf.Max(0, healthAddedPerStep);

            // Speed groeit rustig mee, maar heeft een cap zodat zombies speelbaar blijven.
            EnemyMovement movement = zombie.GetComponent<EnemyMovement>();
            float speedMultiplier = Mathf.Min(
                Mathf.Max(1f, maximumSpeedMultiplier),
                1f + (CurrentRound - 1) * Mathf.Max(0f, speedIncreasePerRound));
            if (movement != null)
            {
                movement.moveSpeed *= speedMultiplier;
            }

            SpitterZombie spitter = zombie.GetComponent<SpitterZombie>();
            if (spitter != null)
                spitter.moveSpeed *= speedMultiplier;

            ZombieSpawnEffect.Play(zombie);
        }
    }

    private GameObject PickRandomPrefab()
    {
        float currentTankChance = Mathf.Min(
            Mathf.Clamp01(maximumTankChance),
            Mathf.Clamp01(tankSpawnChance + (CurrentRound - 1) * tankChanceIncreasePerRound));

        float roll = Random.value;
        float chance = 0f;

        if (exploderPrefab != null)
        {
            chance += Mathf.Clamp01(exploderSpawnChance);
            if (roll < chance) return exploderPrefab;
        }

        if (armoredPrefab != null)
        {
            chance += Mathf.Clamp01(armoredSpawnChance);
            if (roll < chance) return armoredPrefab;
        }

        if (spitterPrefab != null)
        {
            chance += Mathf.Clamp01(spitterSpawnChance);
            if (roll < chance) return spitterPrefab;
        }

        if (tankPrefab != null)
        {
            chance += currentTankChance;
            if (enemyPrefab == null || roll < chance) return tankPrefab;
        }

        return enemyPrefab;
    }

    private float CurrentSpawnInterval()
    {
        float scaledInterval = spawnInterval -
            (CurrentRound - 1) * Mathf.Max(0f, spawnIntervalDecreasePerRound);
        return Mathf.Max(Mathf.Max(0.1f, minimumSpawnInterval), scaledInterval);
    }
}
