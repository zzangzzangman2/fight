using System;
using System.Collections.Generic;
using UnityEngine;

namespace JoseonMurimTactics
{
public static class GeneratedAssetManifestLoader
{
    private const string ResourcePath = "AssetManifest/generated_asset_manifest";
    private static GeneratedAssetManifest cachedManifest;
    private static Dictionary<string, GeneratedAssetRecord> recordsById;

    public static GeneratedAssetManifest Load()
    {
        if (cachedManifest != null)
        {
            return cachedManifest;
        }

        TextAsset textAsset = Resources.Load<TextAsset>(ResourcePath);
        if (textAsset == null || string.IsNullOrEmpty(textAsset.text))
        {
            Debug.LogWarning("[GeneratedAssetManifestLoader] Missing Resources manifest: " + ResourcePath);
            cachedManifest = new GeneratedAssetManifest();
            recordsById = new Dictionary<string, GeneratedAssetRecord>(StringComparer.OrdinalIgnoreCase);
            return cachedManifest;
        }

        cachedManifest = JsonUtility.FromJson<GeneratedAssetManifest>(textAsset.text) ?? new GeneratedAssetManifest();
        recordsById = new Dictionary<string, GeneratedAssetRecord>(StringComparer.OrdinalIgnoreCase);
        foreach (GeneratedAssetRecord record in cachedManifest.assets)
        {
            if (record == null || string.IsNullOrEmpty(record.id))
            {
                continue;
            }

            recordsById[record.id] = record;
            if (record.aliases == null)
            {
                continue;
            }

            foreach (string alias in record.aliases)
            {
                if (!string.IsNullOrEmpty(alias))
                {
                    recordsById[alias] = record;
                }
            }
        }

        return cachedManifest;
    }

    public static GeneratedAssetRecord Find(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        Load();
        return recordsById != null && recordsById.TryGetValue(id, out GeneratedAssetRecord record) ? record : null;
    }
}
}
