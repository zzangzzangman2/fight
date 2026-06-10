using UnityEditor;
using UnityEngine;

namespace JoseonMurimTactics
{
public sealed class MapAssetSpritePostprocessor : AssetPostprocessor
{
    private const string MapAssetRoot = "Assets/JoseonMurimTactics/Resources/MapAssets/";
    private const float TilePixelsPerUnit = 512f;
    private const float ObjectPixelsPerUnit = 320f;
    private const float BackgroundPixelsPerUnit = 320f;

    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(MapAssetRoot))
        {
            return;
        }

        TextureImporter importer = (TextureImporter)assetImporter;

        if (assetPath.Contains("/Source/"))
        {
            importer.textureType = TextureImporterType.Default;
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = !assetPath.Contains("/Backgrounds/");
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.maxTextureSize = assetPath.Contains("/Backgrounds/") ? 4096 : 2048;
        importer.spritePixelsPerUnit = assetPath.Contains("/Tiles/") ? TilePixelsPerUnit
                                     : assetPath.Contains("/Backgrounds/") ? BackgroundPixelsPerUnit
                                     : ObjectPixelsPerUnit;
    }
}
}
