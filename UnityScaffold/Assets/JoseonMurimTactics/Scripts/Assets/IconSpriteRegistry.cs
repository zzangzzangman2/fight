using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace JoseonMurimTactics
{
public static class IconSpriteRegistry
{
    private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
    private static readonly HashSet<string> MissingWarnings = new HashSet<string>();

    public static Sprite LoadSprite(string iconId)
    {
        if (string.IsNullOrEmpty(iconId))
        {
            return null;
        }

        if (Cache.TryGetValue(iconId, out Sprite cached))
        {
            return cached;
        }

        Sprite sprite = null;
        GeneratedAssetRecord record = GeneratedAssetManifestLoader.Find(iconId);
        if (record != null)
        {
            sprite = LoadFromRecord(record);
        }

        if (sprite == null && MissingWarnings.Add(iconId))
        {
            Debug.LogWarning("[IconSpriteRegistry] Icon is not loadable at runtime yet: " + iconId);
        }

        Cache[iconId] = sprite;
        return sprite;
    }

    private static Sprite LoadFromRecord(GeneratedAssetRecord record)
    {
        string resourcePath = ToResourcesPath(record.path);
        if (!string.IsNullOrEmpty(resourcePath))
        {
            Sprite runtimeSprite = Resources.Load<Sprite>(resourcePath);
            if (runtimeSprite != null)
            {
                return runtimeSprite;
            }
        }

#if UNITY_EDITOR
        if (!string.IsNullOrEmpty(record.path))
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(record.path);
        }
#endif

        return null;
    }

    private static string ToResourcesPath(string assetPath)
    {
        const string marker = "/Resources/";
        if (string.IsNullOrEmpty(assetPath))
        {
            return null;
        }

        int markerIndex = assetPath.IndexOf(marker, System.StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0)
        {
            return null;
        }

        string withoutRoot = assetPath.Substring(markerIndex + marker.Length);
        int extensionIndex = withoutRoot.LastIndexOf('.');
        return extensionIndex >= 0 ? withoutRoot.Substring(0, extensionIndex) : withoutRoot;
    }
}
}
