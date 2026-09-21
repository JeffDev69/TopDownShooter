using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// One-click setup voor de four special zombie prefabs en EnemySpawner links.
public static class SpecialZombieSetup
{
    private const string BasePrefabPath = "Assets/Prefabs/FastZombie.prefab";
    private const string EnemySpriteFolder = "Assets/Sprites/Enemies/";

    [MenuItem("Tools/Setup Special Zombies")]
    public static void Setup()
    {
        GameObject basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BasePrefabPath);
        if (basePrefab == null)
        {
            EditorUtility.DisplayDialog("Special Zombies", "FastZombie.prefab was not found in Assets/Prefabs.", "OK");
            return;
        }

        float pixelsPerUnit = ReadReferencePixelsPerUnit();
        ConfigureSprite("Assets/Resources/BloodPool.png", pixelsPerUnit);
        GameObject exploder = CreateZombiePrefab(basePrefab, "ExploderZombie", typeof(ExploderZombie), 4, 15, 2.7f, pixelsPerUnit);
        GameObject armored = CreateZombiePrefab(basePrefab, "ArmoredZombie", typeof(ArmoredZombie), 4, 10, 1.7f, pixelsPerUnit);
        GameObject spitter = CreateZombiePrefab(basePrefab, "SpitterZombie", typeof(SpitterZombie), 4, 18, 2.2f, pixelsPerUnit);
        GameObject boss = CreateZombiePrefab(basePrefab, "BossZombie", typeof(BossZombie), 8, 25, 1.5f, pixelsPerUnit);

        EnemySpawner spawner = UnityEngine.Object.FindFirstObjectByType<EnemySpawner>();
        if (spawner != null)
        {
            Undo.RecordObject(spawner, "Connect Special Zombie Prefabs");
            spawner.exploderPrefab = exploder;
            spawner.armoredPrefab = armored;
            spawner.spitterPrefab = spitter;
            spawner.bossPrefab = boss;
            EditorUtility.SetDirty(spawner);
            EditorSceneManager.MarkSceneDirty(spawner.gameObject.scene);
            Selection.activeGameObject = spawner.gameObject;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog(
            "Special Zombies Ready",
            spawner == null
                ? "The four prefabs were created. Open the game scene and drag them into EnemySpawner."
                : "The four prefabs were created and connected. Save the scene, then press Play.",
            "Nice!");
    }

    private static GameObject CreateZombiePrefab(
        GameObject basePrefab,
        string zombieName,
        Type behaviourType,
        int baseHealth,
        int baseCoins,
        float speed,
        float pixelsPerUnit)
    {
        string spritePath = EnemySpriteFolder + zombieName + ".png";
        ConfigureSprite(spritePath, pixelsPerUnit);
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (sprite == null)
        {
            Debug.LogError("Could not load " + spritePath);
            return null;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
        instance.name = zombieName;

        SpriteRenderer renderer = instance.GetComponentInChildren<SpriteRenderer>();
        if (renderer != null) renderer.sprite = sprite;

        Animator animator = instance.GetComponentInChildren<Animator>();
        if (animator != null) UnityEngine.Object.DestroyImmediate(animator);

        EnemyHealth health = instance.GetComponent<EnemyHealth>();
        if (health != null)
        {
            health.health = baseHealth;
            health.coinsOnDeath = baseCoins;
        }

        EnemyMovement movement = instance.GetComponent<EnemyMovement>();
        if (movement != null) movement.moveSpeed = speed;

        if (instance.GetComponent(behaviourType) == null)
            instance.AddComponent(behaviourType);

        string prefabPath = "Assets/Prefabs/" + zombieName + ".prefab";
        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
        UnityEngine.Object.DestroyImmediate(instance);
        return savedPrefab;
    }

    private static void ConfigureSprite(string path, float pixelsPerUnit)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.SaveAndReimport();
    }

    private static float ReadReferencePixelsPerUnit()
    {
        TextureImporter reference = AssetImporter.GetAtPath("Assets/Sprites/TankZombie.png") as TextureImporter;
        return reference != null ? reference.spritePixelsPerUnit : 100f;
    }
}
