using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JoseonMurimTactics.Editor
{
public sealed class ExternalMapBundleBrowser : EditorWindow
{
    public const string RelativeBundleFolder = "Assets/JoseonMurimTactics/StreamingAssets/MAP";

    private readonly List<BundleEntry> entries = new List<BundleEntry>();
    private readonly Dictionary<string, int> typeCounts = new Dictionary<string, int>();
    private Vector2 listScroll;
    private Vector2 detailScroll;
    private string search = string.Empty;
    private int selectedIndex = -1;
    private AssetBundle loadedBundle;
    private string loadedPath = string.Empty;
    private string status = string.Empty;
    private UnityEngine.Object[] loadedAssets = Array.Empty<UnityEngine.Object>();
    private Texture2D[] textures = Array.Empty<Texture2D>();
    private Sprite[] sprites = Array.Empty<Sprite>();
    private GameObject[] gameObjects = Array.Empty<GameObject>();
    private string[] scenePaths = Array.Empty<string>();

    public static string AbsoluteBundleFolder
    {
        get
        {
            return Path.GetFullPath(
                Path.Combine(Application.dataPath, "JoseonMurimTactics/StreamingAssets/MAP")
            );
        }
    }

    public static int BundleCount
    {
        get
        {
            string folder = AbsoluteBundleFolder;
            return Directory.Exists(folder)
                ? Directory.GetFiles(folder, "*.unity3d", SearchOption.TopDirectoryOnly).Length
                : 0;
        }
    }

    [MenuItem("Joseon Murim/External Map Bundle Browser")]
    public static void Open()
    {
        GetWindow<ExternalMapBundleBrowser>("Map Bundles");
    }

    private void OnEnable()
    {
        Refresh();
    }

    private void OnDisable()
    {
        UnloadCurrentBundle();
    }

    private void OnGUI()
    {
        DrawToolbar();

        EditorGUILayout.BeginHorizontal();
        DrawBundleList();
        DrawBundleDetail();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("External MAP bundles", GUILayout.Width(140f));
        search = GUILayout.TextField(search, GUI.skin.FindStyle("ToolbarSeachTextField") ?? EditorStyles.toolbarTextField);
        if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70f)))
        {
            Refresh();
        }

        if (GUILayout.Button("Folder", EditorStyles.toolbarButton, GUILayout.Width(64f)))
        {
            EditorUtility.RevealInFinder(AbsoluteBundleFolder);
        }

        EditorGUILayout.EndHorizontal();

        if (!string.IsNullOrEmpty(status))
        {
            EditorGUILayout.HelpBox(status, MessageType.Info);
        }
    }

    private void DrawBundleList()
    {
        IReadOnlyList<BundleEntry> filtered = FilteredEntries();
        EditorGUILayout.BeginVertical(GUILayout.Width(360f));
        EditorGUILayout.LabelField(
            $"MAP folder: {RelativeBundleFolder}",
            EditorStyles.miniLabel
        );
        EditorGUILayout.LabelField($"Bundles: {filtered.Count}/{entries.Count}", EditorStyles.boldLabel);
        listScroll = EditorGUILayout.BeginScrollView(listScroll);
        for (int i = 0; i < filtered.Count; i++)
        {
            BundleEntry entry = filtered[i];
            bool selected = selectedIndex >= 0 &&
                            selectedIndex < entries.Count &&
                            entries[selectedIndex].Path == entry.Path;
            GUIStyle style = selected ? EditorStyles.helpBox : GUI.skin.box;
            EditorGUILayout.BeginVertical(style);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(entry.Name, EditorStyles.boldLabel))
            {
                selectedIndex = entries.IndexOf(entry);
                status = $"Selected {entry.Name}";
            }
            GUILayout.FlexibleSpace();
            GUILayout.Label(entry.Category, EditorStyles.miniLabel, GUILayout.Width(70f));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField($"{entry.SizeMb:0.00} MB", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawBundleDetail()
    {
        EditorGUILayout.BeginVertical();
        BundleEntry selected = selectedIndex >= 0 && selectedIndex < entries.Count
            ? entries[selectedIndex]
            : null;
        if (selected == null)
        {
            EditorGUILayout.HelpBox("Select a .unity3d map bundle from the MAP folder.", MessageType.None);
            EditorGUILayout.EndVertical();
            return;
        }

        EditorGUILayout.LabelField(selected.Name, EditorStyles.boldLabel);
        EditorGUILayout.SelectableLabel(selected.Path, EditorStyles.textField, GUILayout.Height(18f));
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Load Bundle", GUILayout.Height(28f)))
        {
            LoadSelectedBundle(selected);
        }

        if (GUILayout.Button("Unload", GUILayout.Height(28f), GUILayout.Width(96f)))
        {
            UnloadCurrentBundle();
            status = "Unloaded bundle.";
        }

        if (GUILayout.Button("Reveal", GUILayout.Height(28f), GUILayout.Width(96f)))
        {
            EditorUtility.RevealInFinder(selected.Path);
        }
        EditorGUILayout.EndHorizontal();

        detailScroll = EditorGUILayout.BeginScrollView(detailScroll);
        DrawLoadedSummary();
        DrawSceneBundleControls();
        DrawTextureGrid();
        DrawSpriteGrid();
        DrawGameObjectList();
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawLoadedSummary()
    {
        if (loadedBundle == null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Load the selected bundle to inspect its textures, sprites, prefabs and tile chunks.",
                MessageType.None
            );
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Loaded Summary", EditorStyles.boldLabel);
        if (scenePaths.Length > 0)
        {
            EditorGUILayout.LabelField("Bundle kind", "Streamed scene bundle");
            EditorGUILayout.LabelField("Scene paths", scenePaths.Length.ToString());
            return;
        }

        EditorGUILayout.LabelField($"Assets: {loadedAssets.Length}");
        foreach (KeyValuePair<string, int> pair in typeCounts.OrderByDescending(pair => pair.Value))
        {
            EditorGUILayout.LabelField(pair.Key, pair.Value.ToString());
        }
    }

    private void DrawSceneBundleControls()
    {
        if (loadedBundle == null || scenePaths.Length == 0)
        {
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Scene Bundle", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "This map is a streamed scene AssetBundle. Enter Play Mode, then load it additively to inspect tile chunks, cliffs, stairs and colliders in the Hierarchy.",
            MessageType.Info
        );

        for (int i = 0; i < scenePaths.Length; i++)
        {
            string scenePath = scenePaths[i];
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.SelectableLabel(scenePath, EditorStyles.textField, GUILayout.Height(18f));
            using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying))
            {
                if (GUILayout.Button("Load Additive", GUILayout.Width(110f)))
                {
                    SceneManager.LoadScene(scenePath, LoadSceneMode.Additive);
                    status = "Requested additive scene load: " + scenePath;
                }
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawTextureGrid()
    {
        if (textures.Length == 0)
        {
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"Textures ({textures.Length})", EditorStyles.boldLabel);
        DrawPreviewGrid(textures.Cast<UnityEngine.Object>().ToArray(), 120f);
    }

    private void DrawSpriteGrid()
    {
        if (sprites.Length == 0)
        {
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"Sprites ({sprites.Length})", EditorStyles.boldLabel);
        DrawPreviewGrid(sprites.Cast<UnityEngine.Object>().ToArray(), 96f);
    }

    private void DrawPreviewGrid(UnityEngine.Object[] objects, float size)
    {
        int columns = Mathf.Max(1, Mathf.FloorToInt((position.width - 420f) / (size + 22f)));
        for (int i = 0; i < objects.Length; i += columns)
        {
            EditorGUILayout.BeginHorizontal();
            for (int col = 0; col < columns && i + col < objects.Length; col++)
            {
                UnityEngine.Object obj = objects[i + col];
                EditorGUILayout.BeginVertical(GUILayout.Width(size + 12f));
                Rect rect = GUILayoutUtility.GetRect(size, size, GUILayout.Width(size), GUILayout.Height(size));
                Texture preview = AssetPreview.GetAssetPreview(obj) ?? AssetPreview.GetMiniThumbnail(obj);
                if (preview != null)
                {
                    GUI.DrawTexture(rect, preview, ScaleMode.ScaleToFit, true);
                }
                else
                {
                    GUI.Box(rect, "preview");
                }

                string label = string.IsNullOrEmpty(obj.name) ? obj.GetType().Name : obj.name;
                GUILayout.Label(label, EditorStyles.miniLabel, GUILayout.Width(size));
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawGameObjectList()
    {
        if (gameObjects.Length == 0)
        {
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"GameObjects / tile chunks ({gameObjects.Length})", EditorStyles.boldLabel);
        int limit = Mathf.Min(gameObjects.Length, 120);
        for (int i = 0; i < limit; i++)
        {
            GameObject go = gameObjects[i];
            EditorGUILayout.ObjectField(
                string.IsNullOrEmpty(go.name) ? $"GameObject {i + 1}" : go.name,
                go,
                typeof(GameObject),
                false
            );
        }

        if (gameObjects.Length > limit)
        {
            EditorGUILayout.LabelField($"...and {gameObjects.Length - limit} more", EditorStyles.miniLabel);
        }
    }

    private void Refresh()
    {
        entries.Clear();
        string folder = AbsoluteBundleFolder;
        if (!Directory.Exists(folder))
        {
            status = $"MAP folder missing: {folder}";
            return;
        }

        foreach (string path in Directory.GetFiles(folder, "*.unity3d", SearchOption.TopDirectoryOnly))
        {
            FileInfo file = new FileInfo(path);
            entries.Add(
                new BundleEntry
                {
                    Name = Path.GetFileName(path),
                    Path = path,
                    SizeMb = file.Length / (1024f * 1024f),
                    Category = CategoryFor(Path.GetFileName(path)),
                }
            );
        }

        entries.Sort((left, right) => string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase));
        selectedIndex = entries.Count == 0 ? -1 : Mathf.Clamp(selectedIndex, 0, entries.Count - 1);
        status = $"Loaded MAP folder index: {entries.Count} bundles.";
    }

    private IReadOnlyList<BundleEntry> FilteredEntries()
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return entries;
        }

        string needle = search.Trim();
        return entries
            .Where(entry => entry.Name.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0 ||
                            entry.Category.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
            .ToList();
    }

    private void LoadSelectedBundle(BundleEntry entry)
    {
        if (loadedPath == entry.Path && loadedBundle != null)
        {
            status = $"Already loaded {entry.Name}";
            return;
        }

        UnloadCurrentBundle();
        loadedPath = entry.Path;
        loadedBundle = AssetBundle.LoadFromFile(entry.Path);
        if (loadedBundle == null)
        {
            status = $"Unity could not load {entry.Name}. The bundle may be Android-only; keep it as reference data.";
            return;
        }

        if (loadedBundle.isStreamedSceneAssetBundle)
        {
            scenePaths = loadedBundle.GetAllScenePaths();
            status = $"Loaded scene bundle {entry.Name}: scenes={scenePaths.Length}";
            return;
        }

        loadedAssets = loadedBundle.LoadAllAssets();
        textures = loadedBundle.LoadAllAssets<Texture2D>()
            .OrderByDescending(texture => texture.width * texture.height)
            .ToArray();
        sprites = loadedBundle.LoadAllAssets<Sprite>().ToArray();
        gameObjects = loadedBundle.LoadAllAssets<GameObject>().ToArray();
        typeCounts.Clear();
        for (int i = 0; i < loadedAssets.Length; i++)
        {
            UnityEngine.Object asset = loadedAssets[i];
            if (asset == null)
            {
                continue;
            }

            string typeName = asset.GetType().Name;
            typeCounts[typeName] = typeCounts.TryGetValue(typeName, out int count) ? count + 1 : 1;
        }

        status =
            $"Loaded {entry.Name}: assets={loadedAssets.Length}, textures={textures.Length}, sprites={sprites.Length}, objects={gameObjects.Length}";
    }

    private void UnloadCurrentBundle()
    {
        loadedAssets = Array.Empty<UnityEngine.Object>();
        textures = Array.Empty<Texture2D>();
        sprites = Array.Empty<Sprite>();
        gameObjects = Array.Empty<GameObject>();
        scenePaths = Array.Empty<string>();
        typeCounts.Clear();
        loadedPath = string.Empty;
        if (loadedBundle != null)
        {
            loadedBundle.Unload(true);
            loadedBundle = null;
        }
    }

    private static string CategoryFor(string fileName)
    {
        string lower = fileName.ToLowerInvariant();
        if (lower.Contains("snow"))
        {
            return "snow";
        }
        if (lower.Contains("mountain"))
        {
            return "mountain";
        }
        if (lower.Contains("plain"))
        {
            return "plain";
        }
        if (lower.Contains("city"))
        {
            return "city";
        }
        if (lower.Contains("cave"))
        {
            return "cave";
        }
        if (lower.Contains("desert"))
        {
            return "desert";
        }
        if (lower.Contains("interior"))
        {
            return "interior";
        }
        return "map";
    }

    private sealed class BundleEntry
    {
        public string Name;
        public string Path;
        public float SizeMb;
        public string Category;
    }
}
}
