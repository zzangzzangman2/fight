using UnityEditor;
using UnityEngine;

namespace JoseonMurimTactics.Editor
{
public sealed class CharacterArtPostprocessor : AssetPostprocessor
{
    private const float BattlePosePixelsPerUnit = 420f;
    private static readonly Vector2 BattlePosePivot = new Vector2(0.5f, 32f / 384f);

    private void OnPreprocessTexture()
    {
        if (!assetPath.Contains("/JoseonMurimTactics/Art/Characters/"))
        {
            return;
        }

        TextureImporter importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 4096;

        if (assetPath.Contains("/Sprites/"))
        {
            importer.spritePixelsPerUnit = BattlePosePixelsPerUnit;
            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            importer.SetTextureSettings(settings);
            importer.spritePivot = BattlePosePivot;
        }
    }
}
}
