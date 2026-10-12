using UnityEditor;
using UnityEngine;

// Houdt alle nieuwe perk machine sprites op dezelfde Unity import size.
public class PerkMachineSpriteImporter : AssetPostprocessor
{
    private const string Folder = "Assets/Sprites/PerkMachines";

    [InitializeOnLoadMethod]
    private static void ConfigureExistingSprites()
    {
        EditorApplication.delayCall += () =>
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null && ApplySettings(importer)) importer.SaveAndReimport();
            }
        };
    }

    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(Folder + "/")) return;

        ApplySettings((TextureImporter)assetImporter);
    }

    private static bool ApplySettings(TextureImporter importer)
    {
        // Keep manually sliced sprites and their scene references intact.
        if (importer.spriteImportMode == SpriteImportMode.Multiple) return false;

        bool changed = importer.textureType != TextureImporterType.Sprite ||
            importer.spriteImportMode != SpriteImportMode.Single ||
            !Mathf.Approximately(importer.spritePixelsPerUnit, 500f) ||
            !importer.alphaIsTransparency || importer.mipmapEnabled ||
            importer.filterMode != FilterMode.Bilinear ||
            importer.textureCompression != TextureImporterCompression.Uncompressed;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 500f;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        return changed;
    }
}
