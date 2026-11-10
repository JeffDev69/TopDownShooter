using System.Collections;
using UnityEngine;

// Beheert eindeloze rondes, wisselt zombiesoorten af en houdt bij hoeveel vijanden er over zijn.
public class EnemySpawner : MonoBehaviour
{
    public enum SpecialRoundType { Normal, FastRush, TankWave, ArmoredWave, SpitterSwarm }

    public static EnemySpawner ActiveSpawner { get; private set; }

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

    [Header("Special Rounds")]
    public int specialRoundEvery = 4;
    public float specialRoundSizeMultiplier = 1.2f;
    public float fastRoundSpeedMultiplier = 1.35f;
    public float tankRoundHealthMultiplier = 1.5f;
    public float armoredRoundHealthMultiplier = 1.35f;

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
    public SpecialRoundType CurrentSpecialRound { get; private set; }
    private ScoreManager roundDisplay;
    private PlayerHealth player;

    void Awake()
    {
        ActiveSpawner = this;
    }

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
            CurrentSpecialRound = DetermineSpecialRound();
            if (CurrentSpecialRound != SpecialRoundType.Normal)
                roundSize = Mathf.CeilToInt(roundSize * Mathf.Max(1f, specialRoundSizeMultiplier));
            ZombiesRemaining = roundSize;
            UpdateRoundDisplay(false);
            GameAudio.Play(bossEveryRounds > 0 && CurrentRound % bossEveryRounds == 0
                ? GameAudio.Cue.Boss : GameAudio.Cue.RoundStart);

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
            GameAudio.Play(GameAudio.Cue.RoundClear);
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
            roundDisplay.ShowRound(CurrentRound, ZombiesRemaining, cleared, SpecialRoundLabel());
    }

    void SpawnEnemy(bool forceBoss)
    {
        Vector2 spawnPosition;

        // Kies een willekeurige kant van de arena.
        int side = Random.Range(0, 4);

        if (side == 0) // Boven
        {
            spawnPosition = new Vector2(Random.Range(minX, maxX), maxY);
        }
        else if (side == 1) // Onder
        {
            spawnPosition = new Vector2(Random.Range(minX, maxX), minY);
        }
        else if (side == 2) // Links
        {
            spawnPosition = new Vector2(minX, Random.Range(minY, maxY));
        }
        else // Rechts
        {
            spawnPosition = new Vector2(maxX, Random.Range(minY, maxY));
        }

        GameObject selectedPrefab = forceBoss ? bossPrefab : PickSpawnPrefab();

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

            ApplyRoundScaling(zombie, zombieHealth, forceBoss);

            ZombieSpawnEffect.Play(zombie);
        }
    }

    public void SpawnBossMinions(Vector3 center, int amount)
    {
        GameObject minionPrefab = enemyPrefab != null ? enemyPrefab : tankPrefab;
        if (minionPrefab == null) return;

        for (int i = 0; i < Mathf.Max(0, amount); i++)
        {
            Vector2 offset = Random.insideUnitCircle.normalized * Random.Range(1.5f, 3.2f);
            GameObject zombie = Instantiate(minionPrefab, center + (Vector3)offset, Quaternion.identity);
            EnemyHealth zombieHealth = zombie.GetComponent<EnemyHealth>();
            if (zombieHealth == null)
            {
                Destroy(zombie);
                continue;
            }

            ZombiesRemaining++;
            zombieHealth.SetRoundOwner(this);
            ApplyRoundScaling(zombie, zombieHealth, false);
            ZombieSpawnEffect.Play(zombie);
        }

        UpdateRoundDisplay(false);
    }

    private GameObject PickSpawnPrefab()
    {
        GameObject specialPrefab = PickSpecialRoundPrefab();
        return specialPrefab != null ? specialPrefab : PickRandomPrefab();
    }

    private GameObject PickSpecialRoundPrefab()
    {
        switch (CurrentSpecialRound)
        {
            case SpecialRoundType.FastRush:
                return enemyPrefab;
            case SpecialRoundType.TankWave:
                return tankPrefab != null ? tankPrefab : enemyPrefab;
            case SpecialRoundType.ArmoredWave:
                return armoredPrefab != null ? armoredPrefab : enemyPrefab;
            case SpecialRoundType.SpitterSwarm:
                return spitterPrefab != null ? spitterPrefab : enemyPrefab;
            default:
                return null;
        }
    }

    private void ApplyRoundScaling(GameObject zombie, EnemyHealth zombieHealth, bool boss)
    {
        // Om de paar rondes krijgt elke zombie extra gezondheid.
        int healthSteps = (CurrentRound - 1) / Mathf.Max(1, healthIncreaseEveryRounds);
        zombieHealth.health += healthSteps * Mathf.Max(0, healthAddedPerStep);

        EnemyMovement movement = zombie.GetComponent<EnemyMovement>();
        float speedMultiplier = Mathf.Min(
            Mathf.Max(1f, maximumSpeedMultiplier),
            1f + (CurrentRound - 1) * Mathf.Max(0f, speedIncreasePerRound));

        if (!boss)
        {
            if (CurrentSpecialRound == SpecialRoundType.FastRush)
                speedMultiplier *= Mathf.Max(1f, fastRoundSpeedMultiplier);
            if (CurrentSpecialRound == SpecialRoundType.TankWave)
                zombieHealth.health = Mathf.CeilToInt(zombieHealth.health * Mathf.Max(1f, tankRoundHealthMultiplier));
            if (CurrentSpecialRound == SpecialRoundType.ArmoredWave)
                zombieHealth.health = Mathf.CeilToInt(zombieHealth.health * Mathf.Max(1f, armoredRoundHealthMultiplier));
        }

        if (movement != null)
            movement.moveSpeed *= speedMultiplier;

        SpitterZombie spitter = zombie.GetComponent<SpitterZombie>();
        if (spitter != null)
            spitter.moveSpeed *= speedMultiplier;
    }

    private SpecialRoundType DetermineSpecialRound()
    {
        if (specialRoundEvery <= 0 || CurrentRound % specialRoundEvery != 0)
            return SpecialRoundType.Normal;
        if (bossEveryRounds > 0 && CurrentRound % bossEveryRounds == 0)
            return SpecialRoundType.Normal;

        int cycle = (CurrentRound / Mathf.Max(1, specialRoundEvery)) % 4;
        switch (cycle)
        {
            case 1: return SpecialRoundType.FastRush;
            case 2: return SpecialRoundType.TankWave;
            case 3: return SpecialRoundType.ArmoredWave;
            default: return SpecialRoundType.SpitterSwarm;
        }
    }

    private string SpecialRoundLabel()
    {
        switch (CurrentSpecialRound)
        {
            case SpecialRoundType.FastRush: return "FAST ROUND";
            case SpecialRoundType.TankWave: return "TANK ROUND";
            case SpecialRoundType.ArmoredWave: return "ARMORED ROUND";
            case SpecialRoundType.SpitterSwarm: return "SPITTER ROUND";
            default: return "";
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
