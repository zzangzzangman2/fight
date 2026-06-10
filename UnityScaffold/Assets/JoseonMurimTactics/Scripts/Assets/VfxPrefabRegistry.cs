using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace JoseonMurimTactics
{
public static class VfxPrefabRegistry
{
    private static readonly Dictionary<string, GameObject> Cache = new Dictionary<string, GameObject>();
    private static readonly HashSet<string> MissingWarnings = new HashSet<string>();

    public static GameObject LoadPrefab(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        if (Cache.TryGetValue(id, out GameObject cached))
        {
            return cached;
        }

        GameObject prefab = null;
        GeneratedAssetRecord record = GeneratedAssetManifestLoader.Find(id);
        if (record != null)
        {
            prefab = LoadPrefabFromPath(record.prefabPath);
        }

        if (prefab == null && MissingWarnings.Add(id))
        {
            Debug.LogWarning("[VfxPrefabRegistry] VFX prefab is not loadable at runtime yet: " + id);
        }

        Cache[id] = prefab;
        return prefab;
    }

    private static GameObject LoadPrefabFromPath(string assetPath)
    {
        string resourcePath = ToResourcesPath(assetPath);
        if (!string.IsNullOrEmpty(resourcePath))
        {
            GameObject runtimePrefab = Resources.Load<GameObject>(resourcePath);
            if (runtimePrefab != null)
            {
                return runtimePrefab;
            }
        }

#if UNITY_EDITOR
        if (!string.IsNullOrEmpty(assetPath))
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
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
