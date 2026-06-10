using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace JoseonMurimTactics.Editor
{
public static class NoMapAssetValidator
{
    private const string Root = "Assets/JoseonMurimTactics";
    private const string GeneratedManifestPath = Root + "/Resources/AssetManifest/generated_asset_manifest.json";
    private const string VfxMappingPath = Root + "/Resources/AssetManifest/vfx_mapping.json";
    private const string ContentManifestPath = Root + "/Resources/AuthoringContent/content_manifest.json";
    private const string MarkdownReportPath = Root + "/Docs/NO_MAP_ASSET_VALIDATION_REPORT.md";
    private const string CsvReportPath = Root + "/Docs/no_map_asset_validation_report.csv";

    [MenuItem("Joseon Murim Tactics/Assets/Validate No-Map Placeholder Assets")]
    public static void ValidateFromMenu()
    {
        bool ok = ValidateNoMapPlaceholderAssets();
        if (!ok)
        {
            Debug.LogError("[NoMapAssetValidator] Validation found errors. See " + MarkdownReportPath);
        }
    }

    public static void ValidateNoMapPlaceholderAssetsBatch()
    {
        bool ok = ValidateNoMapPlaceholderAssets();
        EditorApplication.Exit(ok ? 0 : 1);
    }

    public static bool ValidateNoMapPlaceholderAssets()
    {
        List<ValidationIssue> issues = new List<ValidationIssue>();
        GeneratedAssetManifest manifest = LoadJsonAsset<GeneratedAssetManifest>(GeneratedManifestPath, issues, "manifest");
        if (manifest != null && manifest.assets != null)
        {
            ValidateGeneratedAssets(manifest.assets, issues);
        }

        ValidateVfxMapping(issues);
        ValidateAuthoringContent(issues);
        WriteReports(issues);

        int errors = issues.Count(issue => issue.Severity == "Error");
        int warnings = issues.Count(issue => issue.Severity == "Warning");
        Debug.Log("[NoMapAssetValidator] Completed with " + errors.ToString(CultureInfo.InvariantCulture) +
                  " errors and " + warnings.ToString(CultureInfo.InvariantCulture) + " warnings.");
        return errors == 0;
    }

    private static void ValidateGeneratedAssets(List<GeneratedAssetRecord> assets, List<ValidationIssue> issues)
    {
        foreach (GeneratedAssetRecord record in assets)
        {
            if (record == null || string.IsNullOrEmpty(record.path))
            {
                Add(issues, "Error", "Manifest", "", "", "Generated manifest contains an empty asset record.");
                continue;
            }

            ExpectedImport expected = ExpectedImport.For(record);
            ValidateTextureRecord(record, expected, issues);

            if (record.category == "VFX" && !string.IsNullOrEmpty(record.prefabPath))
            {
                ValidateVfxPrefab(record, issues);
            }
        }
    }

    private static void ValidateTextureRecord(GeneratedAssetRecord record, ExpectedImport expected, List<ValidationIssue> issues)
    {
        if (!File.Exists(ToAbsolutePath(record.path)))
        {
            Add(issues, "Error", record.category, record.id, record.path, "PNG file is missing.");
            return;
        }

        TextureImporter importer = AssetImporter.GetAtPath(record.path) as TextureImporter;
        if (importer == null)
        {
            Add(issues, "Error", record.category, record.id, record.path, "TextureImporter is missing.");
            return;
        }

        if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single)
        {
            Add(issues, "Error", record.category, record.id, record.path, "Texture must import as Sprite Single.");
        }

        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        if (settings.spriteAlignment != (int)SpriteAlignment.Custom || Vector2.Distance(settings.spritePivot, expected.Pivot) > 0.01f)
        {
            Add(issues, "Error", record.category, record.id, record.path,
                "Sprite pivot mismatch. Expected " + expected.Pivot + " but found " + settings.spritePivot + ".");
        }

        if (importer.textureCompression != TextureImporterCompression.Uncompressed)
        {
            Add(issues, "Warning", record.category, record.id, record.path, "Default texture compression should be Uncompressed.");
        }

        ValidatePlatform(importer, record, "Standalone", issues);
        ValidatePlatform(importer, record, "WebGL", issues);

        TextureInfo info = ReadTextureInfo(record.path);
        if (info == null)
        {
            Add(issues, "Error", record.category, record.id, record.path, "PNG bytes could not be decoded.");
            return;
        }

        if (info.Width != expected.Width || info.Height != expected.Height)
        {
            Add(issues, "Error", record.category, record.id, record.path,
                "PNG size mismatch. Expected " + expected.Width + "x" + expected.Height + " but found " +
                info.Width + "x" + info.Height + ".");
        }

        if (expected.TransparentCorners && !info.HasTransparentCorners)
        {
            Add(issues, "Error", record.category, record.id, record.path, "Transparent placeholder corners are not transparent.");
        }
    }

    private static void ValidatePlatform(TextureImporter importer, GeneratedAssetRecord record, string platform, List<ValidationIssue> issues)
    {
        TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings(platform);
        if (settings.overridden && settings.textureCompression != TextureImporterCompression.Uncompressed)
        {
            Add(issues, "Warning", record.category, record.id, record.path, platform + " texture compression should be Uncompressed.");
        }
    }

    private static void ValidateVfxPrefab(GeneratedAssetRecord record, List<ValidationIssue> issues)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(record.prefabPath);
        if (prefab == null)
        {
            Add(issues, "Error", "VFX Prefab", record.id, record.prefabPath, "Prefab path is missing.");
            return;
        }

        SpriteRenderer renderer = prefab.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            Add(issues, "Error", "VFX Prefab", record.id, record.prefabPath, "SpriteRenderer is missing.");
            return;
        }

        if (renderer.sprite == null)
        {
            Add(issues, "Error", "VFX Prefab", record.id, record.prefabPath, "SpriteRenderer sprite is missing.");
        }

        if (renderer.sortingOrder < 1000)
        {
            Add(issues, "Warning", "VFX Prefab", record.id, record.prefabPath, "VFX sortingOrder should render in front of combat units.");
        }
    }

    private static void ValidateVfxMapping(List<ValidationIssue> issues)
    {
        VfxMappingDocument mapping = LoadJsonAsset<VfxMappingDocument>(VfxMappingPath, issues, "vfx_mapping");
        if (mapping == null || mapping.weaponAnimationSets == null)
        {
            return;
        }

        if (mapping.weaponAnimationSets.Count < 6)
        {
            Add(issues, "Error", "VFX Mapping", "", VfxMappingPath, "Expected at least six WeaponAnimationSet mappings.");
        }

        foreach (VfxWeaponMapping link in mapping.weaponAnimationSets)
        {
            ValidatePath(issues, "VFX Mapping", link.characterId, link.weaponSetPath, "WeaponAnimationSet asset is missing.");
            ValidatePath(issues, "VFX Mapping", link.characterId, link.attackVfxPrefab, "attackVfxPrefab path is missing.");
            ValidatePath(issues, "VFX Mapping", link.characterId, link.skillVfxPrefab, "skillVfxPrefab path is missing.");
            ValidatePath(issues, "VFX Mapping", link.characterId, link.projectilePrefab, "projectilePrefab path is missing.");
            ValidatePath(issues, "VFX Mapping", link.characterId, link.weaponTrailPrefab, "weaponTrailPrefab path is missing.");
            ValidatePath(issues, "VFX Mapping", link.characterId, link.impactVfxPrefab, "impactVfxPrefab path is missing.");
            ValidatePath(issues, "VFX Mapping", link.characterId, link.guardVfxPrefab, "guardVfxPrefab path is missing.");
            ValidatePath(issues, "VFX Mapping", link.characterId, link.footstepVfxPrefab, "footstepVfxPrefab path is missing.");

            WeaponAnimationSet set = AssetDatabase.LoadAssetAtPath<WeaponAnimationSet>(link.weaponSetPath);
            if (set == null)
            {
                continue;
            }

            CheckSlot(issues, link.characterId, link.weaponSetPath, "attackVfxPrefab", set.attackVfxPrefab);
            CheckSlot(issues, link.characterId, link.weaponSetPath, "skillVfxPrefab", set.skillVfxPrefab);
            CheckSlot(issues, link.characterId, link.weaponSetPath, "projectilePrefab", set.projectilePrefab);
            CheckSlot(issues, link.characterId, link.weaponSetPath, "weaponTrailPrefab", set.weaponTrailPrefab);
            CheckSlot(issues, link.characterId, link.weaponSetPath, "impactVfxPrefab", set.impactVfxPrefab);
            CheckSlot(issues, link.characterId, link.weaponSetPath, "guardVfxPrefab", set.guardVfxPrefab);
            CheckSlot(issues, link.characterId, link.weaponSetPath, "footstepVfxPrefab", set.footstepVfxPrefab);
        }
    }

    private static void ValidateAuthoringContent(List<ValidationIssue> issues)
    {
        AuthoringContentManifest manifest = LoadJsonAsset<AuthoringContentManifest>(ContentManifestPath, issues, "content_manifest");
        if (manifest == null)
        {
            return;
        }

        CheckCharacterPortrait(issues, manifest, "park_mugyeom", "park_mugyeom_face", "Portraits/NPC/park_mugyeom/park_mugyeom_face");
        CheckCharacterPortrait(issues, manifest, "yeon_ok", "yeon_ok_face", "Portraits/NPC/yeon_ok/yeon_ok_face");
        CheckCharacterPortrait(issues, manifest, "cho_hui", "chohui_face", "Portraits/NPC/chohui/chohui_face");

        foreach (AuthoringDialogueScene scene in manifest.dialogueScenes)
        {
            if (scene == null)
            {
                continue;
            }

            if (scene.backgroundId == "joseon_murim_game_map")
            {
                Add(issues, "Error", "Dialogue", scene.id, ContentManifestPath, "Scene still points at legacy world-map background id.");
            }

            foreach (AuthoringDialogueEntry entry in scene.entries)
            {
                if (entry.backgroundId == "joseon_murim_game_map")
                {
                    Add(issues, "Error", "Dialogue", entry.id, ContentManifestPath, "Entry still points at legacy world-map background id.");
                }
            }

            foreach (AuthoringDialogueNode node in scene.nodes)
            {
                if (node.backgroundId == "joseon_murim_game_map")
                {
                    Add(issues, "Error", "Dialogue", node.nodeId, ContentManifestPath, "Node still points at legacy world-map background id.");
                }
            }
        }
    }

    private static void CheckCharacterPortrait(List<ValidationIssue> issues, AuthoringContentManifest manifest, string id, string portraitId, string resource)
    {
        AuthoringCharacter character = manifest.FindCharacter(id);
        if (character == null)
        {
            Add(issues, "Error", "Content Manifest", id, ContentManifestPath, "Character entry is missing.");
            return;
        }

        if (character.portraitId != portraitId || character.portraitResource != resource)
        {
            Add(issues, "Error", "Content Manifest", id, ContentManifestPath, "Character portrait id/resource is not connected.");
        }

        if (Resources.Load<Sprite>(resource) == null && Resources.Load<Texture2D>(resource) == null)
        {
            Add(issues, "Error", "Content Manifest", id, resource, "Portrait Resources asset is not loadable.");
        }
    }

    private static void ValidatePath(List<ValidationIssue> issues, string area, string id, string assetPath, string message)
    {
        if (string.IsNullOrEmpty(assetPath) || AssetDatabase.LoadMainAssetAtPath(assetPath) == null)
        {
            Add(issues, "Error", area, id, assetPath, message);
        }
    }

    private static void CheckSlot(List<ValidationIssue> issues, string id, string path, string slotName, Object slot)
    {
        if (slot == null)
        {
            Add(issues, "Error", "WeaponAnimationSet", id, path, slotName + " is null.");
        }
    }

    private static T LoadJsonAsset<T>(string assetPath, List<ValidationIssue> issues, string label) where T : class
    {
        string absolutePath = ToAbsolutePath(assetPath);
        if (!File.Exists(absolutePath))
        {
            Add(issues, "Error", label, "", assetPath, "JSON file is missing.");
            return null;
        }

        try
        {
            return JsonUtility.FromJson<T>(File.ReadAllText(absolutePath, Encoding.UTF8));
        }
        catch (Exception exception)
        {
            Add(issues, "Error", label, "", assetPath, "JSON parse failed: " + exception.Message);
            return null;
        }
    }

    private static TextureInfo ReadTextureInfo(string assetPath)
    {
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        try
        {
            if (!texture.LoadImage(File.ReadAllBytes(ToAbsolutePath(assetPath))))
            {
                return null;
            }

            Color32[] pixels = texture.GetPixels32();
            bool cornersTransparent =
                pixels[0].a < 8 &&
                pixels[texture.width - 1].a < 8 &&
                pixels[pixels.Length - texture.width].a < 8 &&
                pixels[pixels.Length - 1].a < 8;
            return new TextureInfo(texture.width, texture.height, cornersTransparent);
        }
        finally
        {
            Object.DestroyImmediate(texture);
        }
    }

    private static void WriteReports(List<ValidationIssue> issues)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ToAbsolutePath(MarkdownReportPath)));

        StringBuilder markdown = new StringBuilder();
        markdown.AppendLine("# No-Map Asset Validation Report");
        markdown.AppendLine();
        markdown.AppendLine("- Errors: " + issues.Count(issue => issue.Severity == "Error").ToString(CultureInfo.InvariantCulture));
        markdown.AppendLine("- Warnings: " + issues.Count(issue => issue.Severity == "Warning").ToString(CultureInfo.InvariantCulture));
        markdown.AppendLine();
        markdown.AppendLine("| Severity | Area | Id | Path | Detail |");
        markdown.AppendLine("| --- | --- | --- | --- | --- |");
        foreach (ValidationIssue issue in issues)
        {
            markdown.AppendLine("| " + EscapeMarkdown(issue.Severity) + " | " + EscapeMarkdown(issue.Area) + " | " +
                                EscapeMarkdown(issue.Id) + " | `" + EscapeMarkdown(issue.Path) + "` | " +
                                EscapeMarkdown(issue.Detail) + " |");
        }

        if (issues.Count == 0)
        {
            markdown.AppendLine("| Info | All | - | - | No issues found. |");
        }

        File.WriteAllText(ToAbsolutePath(MarkdownReportPath), markdown.ToString(), new UTF8Encoding(false));

        StringBuilder csv = new StringBuilder();
        csv.AppendLine("severity,area,id,path,detail");
        foreach (ValidationIssue issue in issues)
        {
            csv.AppendLine(Csv(issue.Severity) + "," + Csv(issue.Area) + "," + Csv(issue.Id) + "," + Csv(issue.Path) + "," + Csv(issue.Detail));
        }

        File.WriteAllText(ToAbsolutePath(CsvReportPath), csv.ToString(), new UTF8Encoding(false));
        AssetDatabase.ImportAsset(MarkdownReportPath);
        AssetDatabase.ImportAsset(CsvReportPath);
    }

    private static void Add(List<ValidationIssue> issues, string severity, string area, string id, string path, string detail)
    {
        issues.Add(new ValidationIssue(severity, area, id, path, detail));
    }

    private static string ToAbsolutePath(string assetPath)
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        return Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
    }

    private static string EscapeMarkdown(string value)
    {
        return (value ?? string.Empty).Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
    }

    private static string Csv(string value)
    {
        string escaped = (value ?? string.Empty).Replace("\"", "\"\"");
        return "\"" + escaped + "\"";
    }

    private sealed class ValidationIssue
    {
        public readonly string Severity;
        public readonly string Area;
        public readonly string Id;
        public readonly string Path;
        public readonly string Detail;

        public ValidationIssue(string severity, string area, string id, string path, string detail)
        {
            Severity = severity;
            Area = area;
            Id = id;
            Path = path;
            Detail = detail;
        }
    }

    private sealed class TextureInfo
    {
        public readonly int Width;
        public readonly int Height;
        public readonly bool HasTransparentCorners;

        public TextureInfo(int width, int height, bool hasTransparentCorners)
        {
            Width = width;
            Height = height;
            HasTransparentCorners = hasTransparentCorners;
        }
    }

    private sealed class ExpectedImport
    {
        public readonly int Width;
        public readonly int Height;
        public readonly Vector2 Pivot;
        public readonly bool TransparentCorners;

        private ExpectedImport(int width, int height, Vector2 pivot, bool transparentCorners)
        {
            Width = width;
            Height = height;
            Pivot = pivot;
            TransparentCorners = transparentCorners;
        }

        public static ExpectedImport For(GeneratedAssetRecord record)
        {
            if (record.category == "VFX")
            {
                return new ExpectedImport(1024, 1024, new Vector2(0.5f, 0.5f), true);
            }

            if (record.category == "UI/Icon")
            {
                return new ExpectedImport(256, 256, new Vector2(0.5f, 0.5f), true);
            }

            if (record.category == "Enemy/Pose")
            {
                return new ExpectedImport(768, 768, new Vector2(0.5f, 0.03f), true);
            }

            if (record.category == "NPC/Portrait" && record.path.Contains("_face"))
            {
                return new ExpectedImport(512, 512, new Vector2(0.5f, 0.5f), true);
            }

            if (record.category == "Dialogue/Background")
            {
                return new ExpectedImport(1920, 1080, new Vector2(0.5f, 0.5f), false);
            }

            return new ExpectedImport(record.width, record.height, new Vector2(0.5f, 0.5f), record.category != "Dialogue/Background");
        }
    }

    [Serializable]
    private sealed class VfxMappingDocument
    {
        public int schemaVersion;
        public string note;
        public List<VfxWeaponMapping> weaponAnimationSets = new List<VfxWeaponMapping>();
    }

    [Serializable]
    private sealed class VfxWeaponMapping
    {
        public string characterId;
        public List<string> aliases = new List<string>();
        public string weaponSetPath;
        public string attackVfxPrefab;
        public string skillVfxPrefab;
        public string projectilePrefab;
        public string weaponTrailPrefab;
        public string impactVfxPrefab;
        public string guardVfxPrefab;
        public string footstepVfxPrefab;
    }
}
}
