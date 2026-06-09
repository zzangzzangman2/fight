using UnityEditor;
using UnityEngine;

namespace JoseonMurimTactics
{
public sealed class MapAssetSpritePostprocessor : AssetPostprocessor
{
    private const string MapAssetRoot = "Assets/JoseonMurimTactics/Resources/MapAssets/";

    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(MapAssetRoot))
        {
            return;
        }

        TextureImporter importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.spritePixelsPerUnit = assetPath.Contains("/Tiles/") ? 512f : 320f;
    }
}
}
