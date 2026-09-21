using System.Collections;
using UnityEngine;

// Beheert endless rounds, spawnt normal/tank zombies en trackt hoeveel enemies over zijn.
public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public GameObject tankPrefab;
    [Range(0f, 1f)] public float tankSpawnChance = 0.2f;

    public float spawnInterval = 2f;
    public int firstRoundZombies = 5;
    public int extraZombiesPerRound = 3;
    public float breakBetweenRounds = 3f;

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
                SpawnEnemy();
                if (i < roundSize - 1)
                    yield return new WaitForSeconds(Mathf.Max(0.1f, spawnInterval));
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

    void SpawnEnemy()
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

        GameObject selectedPrefab = enemyPrefab;

        if (tankPrefab != null &&
            (enemyPrefab == null || Random.value < tankSpawnChance))
        {
            selectedPrefab = tankPrefab;
        }

        if (selectedPrefab != null)
        {
            GameObject zombie = Instantiate(selectedPrefab, spawnPosition, Quaternion.identity);
            zombie.GetComponent<EnemyHealth>().SetRoundOwner(this);
        }
    }
}
