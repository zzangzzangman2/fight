#if UNITY_EDITOR
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;

namespace JoseonMurimTactics.EditorTools
{
public static class JoseonMurimAssetValidator
{
    private const int MaxListedPlaceholders = 30;

    [MenuItem("Tools/JoseonMurim/Validate Assets")]
    public static void ValidateAssetsFromMenu()
    {
        ValidateAssets(false);
    }

    public static void ValidateAssetsForBatch()
    {
        bool ok = ValidateAssets(true);
        if (Application.isBatchMode)
        {
            EditorApplication.Exit(ok ? 0 : 1);
        }
    }

    public static bool ValidateAssets(bool strict)
    {
        int errors = 0;
        int warnings = 0;

        AuthoringContentManifest content = AuthoringContentManifest.LoadFromResources();
        Dictionary<string, AuthoringMediaItem> backgrounds = BuildBackgroundLookup(content);
        ValidateBackgrounds(content, backgrounds, ref errors, ref warnings);
        ValidateBattleHudAssets(ref errors);
        ValidateGeneratedManifest(strict, ref warnings);

        string summary = "[JoseonMurimAssetValidator] Completed with " +
                         errors.ToString(CultureInfo.InvariantCulture) + " errors and " +
                         warnings.ToString(CultureInfo.InvariantCulture) + " warnings.";
        if (errors > 0)
        {
            Debug.LogError(summary);
        }
        else
        {
            Debug.Log(summary);
        }

        return errors == 0;
    }

    private static Dictionary<string, AuthoringMediaItem> BuildBackgroundLookup(AuthoringContentManifest content)
    {
        Dictionary<string, AuthoringMediaItem> lookup =
            new Dictionary<string, AuthoringMediaItem>(System.StringComparer.OrdinalIgnoreCase);
        if (content == null || content.backgrounds == null)
        {
            return lookup;
        }

        foreach (AuthoringMediaItem background in content.backgrounds)
        {
            if (background == null || string.IsNullOrEmpty(background.id))
            {
                continue;
            }

            string id = AssetAliasResolver.NormalizeBackgroundId(background.id);
            if (!string.IsNullOrEmpty(id))
            {
                lookup[id] = background;
            }
        }

        return lookup;
    }

    private static void ValidateBackgrounds(AuthoringContentManifest content,
                                            Dictionary<string, AuthoringMediaItem> backgrounds,
                                            ref int errors,
                                            ref int warnings)
    {
        foreach (AuthoringMediaItem background in backgrounds.Values)
        {
            if (string.IsNullOrEmpty(background.resourcePath))
            {
                LogError(ref errors, "Background has no resourcePath: " + background.id);
                continue;
            }

            ValidateResource(background.resourcePath, "background " + background.id, ref errors);
            if (!string.IsNullOrEmpty(background.notes) &&
                background.notes.ToLowerInvariant().Contains("placeholder"))
            {
                LogWarning(ref warnings, "Background note still says placeholder: " + background.id);
            }
        }

        if (content == null || content.dialogueScenes == null)
        {
            return;
        }

        foreach (AuthoringDialogueScene scene in content.dialogueScenes)
        {
            if (scene == null)
            {
                continue;
            }

            ValidateBackgroundId(scene.backgroundId, "scene " + scene.id, backgrounds, ref errors);
            if (scene.entries != null)
            {
                foreach (AuthoringDialogueEntry entry in scene.entries)
                {
                    ValidateBackgroundId(entry.backgroundId, "entry " + entry.id, backgrounds, ref errors);
                }
            }

            if (scene.nodes != null)
            {
                foreach (AuthoringDialogueNode node in scene.nodes)
                {
                    ValidateBackgroundId(node.backgroundId, "node " + node.nodeId, backgrounds, ref errors);
                }
            }

            ValidateEntryNodeSync(scene, ref warnings);
        }
    }

    private static void ValidateEntryNodeSync(AuthoringDialogueScene scene, ref int warnings)
    {
        if (scene.entries == null || scene.nodes == null || scene.entries.Count == 0 || scene.nodes.Count == 0)
        {
            return;
        }

        int count = Mathf.Min(scene.entries.Count, scene.nodes.Count);
        for (int i = 0; i < count; i++)
        {
            string entryBackground = DialogueBackgroundRegistry.ResolveBackgroundId(scene.entries[i].backgroundId,
                                                                                    scene.backgroundId);
            string nodeBackground = DialogueBackgroundRegistry.ResolveBackgroundId(scene.nodes[i].backgroundId,
                                                                                   scene.backgroundId);
            if (entryBackground != nodeBackground)
            {
                LogWarning(ref warnings, "Entry/node background mismatch in " + scene.id + " at index " + i +
                                           ": " + scene.entries[i].id + "=" + entryBackground + ", " +
                                           scene.nodes[i].nodeId + "=" + nodeBackground);
            }
        }
    }

    private static void ValidateBackgroundId(string backgroundId,
                                             string owner,
                                             Dictionary<string, AuthoringMediaItem> backgrounds,
                                             ref int errors)
    {
        string id = DialogueBackgroundRegistry.ResolveBackgroundId(backgroundId, null);
        if (!backgrounds.TryGetValue(id, out AuthoringMediaItem background))
        {
            LogError(ref errors, owner + " references missing background id: " + id);
            return;
        }

        ValidateResource(background.resourcePath, owner + " background " + id, ref errors);
    }

    private static void ValidateBattleHudAssets(ref int errors)
    {
        foreach (string id in BattleHudAssetRegistry.RequiredRuntimeAssetIds)
        {
            string resourcePath = BattleHudAssetRegistry.ResolveResourcePath(id);
            ValidateResource(resourcePath, "Battle HUD UI " + id, ref errors);
        }
    }

    private static void ValidateGeneratedManifest(bool strict, ref int warnings)
    {
        GeneratedAssetManifest generated = GeneratedAssetManifestLoader.Load();
        int placeholderCount = 0;
        List<string> examples = new List<string>();
        foreach (GeneratedAssetRecord record in generated.assets)
        {
            if (record == null || !record.isPlaceholder)
            {
                continue;
            }

            placeholderCount++;
            if (examples.Count < MaxListedPlaceholders)
            {
                examples.Add(record.id);
            }
        }

        if (placeholderCount == 0)
        {
            return;
        }

        string message = "Generated asset manifest still contains " +
                         placeholderCount.ToString(CultureInfo.InvariantCulture) +
                         " placeholder records. First items: " + string.Join(", ", examples.ToArray());
        if (strict)
        {
            LogWarning(ref warnings, message);
        }
        else
        {
            Debug.LogWarning("[JoseonMurimAssetValidator] " + message);
            warnings++;
        }
    }

    private static void ValidateResource(string resourcePath, string label, ref int errors)
    {
        if (string.IsNullOrEmpty(resourcePath))
        {
            LogError(ref errors, label + " has an empty resource path.");
            return;
        }

        if (Resources.Load<Sprite>(resourcePath) != null ||
            Resources.Load<Texture2D>(resourcePath) != null ||
            Resources.Load<TextAsset>(resourcePath) != null)
        {
            return;
        }

        LogError(ref errors, label + " cannot be loaded from Resources path: " + resourcePath);
    }

    private static void LogError(ref int errors, string message)
    {
        errors++;
        Debug.LogError("[JoseonMurimAssetValidator] " + message);
    }

    private static void LogWarning(ref int warnings, string message)
    {
        warnings++;
        Debug.LogWarning("[JoseonMurimAssetValidator] " + message);
    }
}
}
#endif
