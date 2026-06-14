using UnityEditor;
using UnityEngine;

namespace JoseonMurimTactics.Editor
{
public sealed class CharacterArtPostprocessor : AssetPostprocessor
{
    private const float BattlePosePixelsPerUnit = 420f;
    private const float PixelSpritePixelsPerUnit = 64f;
    private static readonly Vector2 BattlePosePivot = new Vector2(0.5f, 32f / 384f);
    private static readonly Vector2 PixelSpritePivot = new Vector2(0.5f, 0.08f);

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
        importer.filterMode = assetPath.Contains("/Sprites/Pixel/") ? FilterMode.Point : FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 4096;

        if (assetPath.Contains("/Sprites/"))
        {
            bool pixelSprite = assetPath.Contains("/Sprites/Pixel/");
            importer.spritePixelsPerUnit = pixelSprite ? PixelSpritePixelsPerUnit : BattlePosePixelsPerUnit;
            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            importer.SetTextureSettings(settings);
            importer.spritePivot = pixelSprite ? PixelSpritePivot : BattlePosePivot;
        }
    }
}
}
