using System;
using System.Collections.Generic;
using UnityEngine;

namespace JoseonMurimTactics
{
    public enum BattleMapRuntimeEditTool
    {
        BlockedTile,
        AllyStartZone,
        EnemyStartPoint,
    }

    public enum BattleMapRuntimeEditorStage
    {
        MapList,
        Paint,
    }

    internal readonly struct BattleMapRuntimeEditorEntry
    {
        public readonly BattleTestMapVariant variant;
        public readonly string title;
        public readonly string subtitle;

        public BattleMapRuntimeEditorEntry(
            BattleTestMapVariant variant,
            string title,
            string subtitle
        )
        {
            this.variant = variant;
            this.title = title;
            this.subtitle = subtitle;
        }
    }

    [DisallowMultipleComponent]
    public sealed class BattleMapRuntimeEditorOverlay : MonoBehaviour
    {
        private static readonly BattleMapRuntimeEditorEntry[] MapEntries =
        {
            new BattleMapRuntimeEditorEntry(
                BattleTestMapVariant.BaekduSnowGate,
                "Baekdu Snow Gate",
                "main painted gate battlefield"
            ),
            new BattleMapRuntimeEditorEntry(
                BattleTestMapVariant.BaekduMountainSnowfield,
                "Baekdu Snowfield",
                "snowfield free battle"
            ),
            new BattleMapRuntimeEditorEntry(
                BattleTestMapVariant.BanditLair,
                "Bandit Lair",
                "bandit free battle"
            ),
            new BattleMapRuntimeEditorEntry(
                BattleTestMapVariant.WolfPass,
                "Wolf Pass",
                "wolf pass free battle"
            ),
            new BattleMapRuntimeEditorEntry(
                BattleTestMapVariant.TigerRavine,
                "Tiger Ravine",
                "tiger ravine free battle"
            ),
            new BattleMapRuntimeEditorEntry(
                BattleTestMapVariant.LeopardCliff,
                "Leopard Cliff",
                "leopard cliff free battle"
            ),
            new BattleMapRuntimeEditorEntry(
                BattleTestMapVariant.SeorakPassRescue,
                "Seorak Rescue",
                "rescue route battle"
            ),
        };

        private static readonly Color DefaultTileColor = new Color(0.70f, 0.72f, 0.75f, 1f);
        private static readonly Color BlockedTileColor = new Color(1.00f, 0.10f, 0.08f, 1f);
        private static readonly Color AllyStartColor = new Color(0.10f, 0.60f, 1.00f, 1f);
        private static readonly Color EnemyStartColor = new Color(1.00f, 0.55f, 0.05f, 1f);
        private static readonly Color HoverColor = new Color(1.00f, 0.90f, 0.18f, 1f);

        private BattleTestController controller;
        private BattleMapRuntimeEditorStage stage = BattleMapRuntimeEditorStage.MapList;
        private BattleMapRuntimeEditTool tool = BattleMapRuntimeEditTool.BlockedTile;
        private BattleTestTile hoverTile;
        private GUIStyle panelStyle;
        private GUIStyle listPanelStyle;
        private GUIStyle cardStyle;
        private GUIStyle savedCardStyle;
        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle textStyle;
        private GUIStyle hotkeyStyle;
        private string statusText = string.Empty;
        private float statusUntil;
        private float overlayAlpha = 0.34f;
        private bool initializedThisSession;
        private bool checkedCommandLine;
        private BattleTestMapVariant activeVariant = BattleTestMapVariant.BaekduSnowGate;

        public bool IsEditing { get; private set; }

        public void Bind(BattleTestController owner)
        {
            controller = owner;
        }

        private void Start()
        {
            if (!checkedCommandLine && ShouldOpenFromCommandLine())
            {
                checkedCommandLine = true;
                OpenMapList();
            }
        }

        private void Update()
        {
            if (controller == null)
            {
                controller = FindAnyObjectByType<BattleTestController>();
            }

            if (!checkedCommandLine)
            {
                checkedCommandLine = true;
                if (ShouldOpenFromCommandLine())
                {
                    OpenMapList();
                    return;
                }
            }

            if (Input.GetKeyDown(KeyCode.F10))
            {
                if (IsEditing)
                {
                    SetEditing(false);
                }
                else
                {
                    OpenMapList();
                }
                return;
            }

            if (!IsEditing || controller == null)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                SetEditing(false);
                return;
            }

            if (stage == BattleMapRuntimeEditorStage.MapList)
            {
                HideUnits();
                return;
            }

            hoverTile = ResolveTileAtMouse();
            HandleHotkeys();

            if (Input.GetMouseButtonDown(0))
            {
                ToggleTile(hoverTile);
            }
        }

        private void LateUpdate()
        {
            if (
                !IsEditing
                || stage != BattleMapRuntimeEditorStage.Paint
                || controller == null
                || controller.PreviewTiles == null
            )
            {
                return;
            }

            foreach (BattleTestTile tile in controller.PreviewTiles)
            {
                if (tile == null)
                {
                    continue;
                }

                tile.SetHighlight(
                    tile == hoverTile ? WithAlpha(HoverColor, 0.76f) : OverlayColorFor(tile)
                );
            }
        }

        private void OnDisable()
        {
            RestoreUnits();
            if (controller != null)
            {
                controller.ClearPreviewHighlights();
            }
        }

        private void OnGUI()
        {
            if (!IsEditing)
            {
                return;
            }

            if (stage == BattleMapRuntimeEditorStage.MapList)
            {
                DrawMapList();
                return;
            }

            DrawPaintHud();
        }

        private void DrawMapList()
        {
            EnsureStyles();
            GUI.Box(new Rect(0f, 0f, Screen.width, Screen.height), GUIContent.none, listPanelStyle);

            float width = Mathf.Min(980f, Screen.width - 64f);
            float startX = (Screen.width - width) * 0.5f;
            GUI.Label(new Rect(startX, 48f, width, 38f), "MAP EDITOR", titleStyle);
            GUI.Label(
                new Rect(startX, 86f, width, 26f),
                "Choose a map. Characters stay hidden until you leave edit mode.",
                subtitleStyle
            );

            const float cardW = 460f;
            const float cardH = 86f;
            const float gap = 18f;
            float x0 = startX;
            float x1 = startX + cardW + gap;
            float y = 132f;
            for (int i = 0; i < MapEntries.Length; i++)
            {
                BattleMapRuntimeEditorEntry entry = MapEntries[i];
                int column = i % 2;
                int row = i / 2;
                Rect rect = new Rect(column == 0 ? x0 : x1, y + row * (cardH + gap), cardW, cardH);
                bool saved = BattleMapRuntimeEditStore.HasSavedOverride(entry.variant);
                GUI.Box(rect, GUIContent.none, saved ? savedCardStyle : cardStyle);
                GUI.Label(
                    new Rect(rect.x + 18f, rect.y + 13f, rect.width - 36f, 26f),
                    entry.title,
                    hotkeyStyle
                );
                GUI.Label(
                    new Rect(rect.x + 18f, rect.y + 40f, rect.width - 36f, 22f),
                    entry.subtitle,
                    textStyle
                );
                GUI.Label(
                    new Rect(rect.x + 18f, rect.y + 61f, rect.width - 36f, 20f),
                    saved ? "saved edit exists" : "no saved edit: starts as all-walkable gray",
                    subtitleStyle
                );

                if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
                {
                    BeginEditingMap(entry.variant);
                }
            }

            GUI.Label(
                new Rect(startX, Screen.height - 58f, width, 24f),
                "Esc/F10 closes editor. Saved CSV files go to codex-requests.",
                subtitleStyle
            );
        }

        private void DrawPaintHud()
        {
            EnsureStyles();
            Rect panelRect = new Rect(16f, 52f, 560f, 226f);
            GUI.Box(panelRect, GUIContent.none, panelStyle);
            GUI.Label(
                new Rect(32f, 66f, 500f, 26f),
                $"MAP EDIT  {MapTitle(activeVariant)}",
                titleStyle
            );
            GUI.Label(
                new Rect(32f, 98f, 510f, 24f),
                "All gray tiles are walkable by default. Mark only exceptions.",
                textStyle
            );
            GUI.Label(
                new Rect(32f, 126f, 510f, 24f),
                "1 Blocked(red)   2 Ally start(blue)   3 Enemy start(orange)",
                hotkeyStyle
            );
            GUI.Label(
                new Rect(32f, 152f, 510f, 24f),
                "Left click toggles selected marker  |  [ / ] opacity  |  S save  |  R reload",
                textStyle
            );
            GUI.Label(new Rect(32f, 178f, 510f, 24f), CurrentToolText(), textStyle);
            GUI.Label(new Rect(32f, 204f, 510f, 24f), HoverText(), textStyle);
            if (GUI.Button(new Rect(32f, 232f, 128f, 28f), "Map List"))
            {
                stage = BattleMapRuntimeEditorStage.MapList;
                controller.ClearPreviewHighlights();
                HideUnits();
            }

            string status = Time.realtimeSinceStartup < statusUntil ? statusText : string.Empty;
            if (!string.IsNullOrEmpty(status))
            {
                GUI.Label(new Rect(174f, 234f, 360f, 22f), status, textStyle);
            }
        }

        private void OpenMapList()
        {
            IsEditing = true;
            stage = BattleMapRuntimeEditorStage.MapList;
            activeVariant =
                controller == null ? BattleTestMapVariant.BaekduSnowGate : controller.mapVariant;
            hoverTile = null;
            HideUnits();
            if (controller != null)
            {
                controller.ClearPreviewHighlights();
            }

            BattleMapDebugOverlay debugOverlay = GetComponent<BattleMapDebugOverlay>();
            if (debugOverlay != null)
            {
                debugOverlay.ClearMode();
            }

            SetStatus("Choose a map.");
        }

        private void SetEditing(bool editing)
        {
            IsEditing = editing;
            hoverTile = null;

            if (editing)
            {
                HideUnits();
                stage = BattleMapRuntimeEditorStage.MapList;
            }
            else
            {
                RestoreUnits();
            }

            if (controller != null)
            {
                controller.ClearPreviewHighlights();
            }

            BattleMapDebugOverlay debugOverlay = GetComponent<BattleMapDebugOverlay>();
            if (debugOverlay != null)
            {
                debugOverlay.ClearMode();
            }

            SetStatus(editing ? "Edit mode on. Characters hidden." : "Edit mode off.");
        }

        private void BeginEditingMap(BattleTestMapVariant variant)
        {
            activeVariant = variant;
            stage = BattleMapRuntimeEditorStage.Paint;
            initializedThisSession = false;
            if (controller != null && controller.mapVariant != variant)
            {
                controller.LoadMapForRuntimeEditing(variant);
            }

            HideUnits();
            InitializeEditableMapIfNeeded();
        }

        private void InitializeEditableMapIfNeeded()
        {
            if (initializedThisSession)
            {
                return;
            }

            initializedThisSession = true;
            activeVariant = controller == null ? activeVariant : controller.mapVariant;
            if (BattleMapRuntimeEditStore.HasSavedOverride(activeVariant))
            {
                LoadSavedMap();
                return;
            }

            foreach (BattleTestTile tile in controller.PreviewTiles)
            {
                if (tile == null)
                {
                    continue;
                }

                MakeWalkableDefault(tile);
            }

            SetStatus("No saved edit. Whole map starts walkable gray.");
        }

        private void HandleHotkeys()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            {
                tool = BattleMapRuntimeEditTool.BlockedTile;
                SetStatus("Tool: blocked red tile.");
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            {
                tool = BattleMapRuntimeEditTool.AllyStartZone;
                SetStatus("Tool: ally start blue zone.");
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
            {
                tool = BattleMapRuntimeEditTool.EnemyStartPoint;
                SetStatus("Tool: enemy start orange point.");
            }
            else if (Input.GetKeyDown(KeyCode.LeftBracket))
            {
                overlayAlpha = Mathf.Clamp(overlayAlpha - 0.06f, 0.10f, 0.78f);
                SetStatus($"Opacity {Mathf.RoundToInt(overlayAlpha * 100f)}%");
            }
            else if (Input.GetKeyDown(KeyCode.RightBracket))
            {
                overlayAlpha = Mathf.Clamp(overlayAlpha + 0.06f, 0.10f, 0.78f);
                SetStatus($"Opacity {Mathf.RoundToInt(overlayAlpha * 100f)}%");
            }
            else if (Input.GetKeyDown(KeyCode.S))
            {
                SaveCurrentMap();
            }
            else if (Input.GetKeyDown(KeyCode.R))
            {
                LoadSavedMap();
            }
            else if (Input.GetKeyDown(KeyCode.Tab))
            {
                stage = BattleMapRuntimeEditorStage.MapList;
                controller.ClearPreviewHighlights();
                HideUnits();
            }
        }

        private BattleTestTile ResolveTileAtMouse()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                return null;
            }

            Vector3 world = camera.ScreenToWorldPoint(Input.mousePosition);
            Vector2 point = new Vector2(world.x, world.y);
            Collider2D[] hits = Physics2D.OverlapPointAll(point);
            if (hits != null)
            {
                foreach (Collider2D hit in hits)
                {
                    BattleTestTile tile = hit == null ? null : hit.GetComponent<BattleTestTile>();
                    if (tile != null)
                    {
                        return tile;
                    }
                }
            }

            return controller == null
                ? null
                : controller.GetPreviewTileAt(controller.GetPreviewGridCell(point));
        }

        private void ToggleTile(BattleTestTile tile)
        {
            if (tile == null)
            {
                return;
            }

            switch (tool)
            {
                case BattleMapRuntimeEditTool.BlockedTile:
                    if (IsBlocked(tile))
                    {
                        MakeWalkableDefault(tile);
                    }
                    else
                    {
                        MakeBlocked(tile);
                    }

                    break;
                case BattleMapRuntimeEditTool.AllyStartZone:
                    EnsureWalkableForMarker(tile);
                    tile.deployZone = tile.deployZone > 0 ? 0 : 1;
                    SetStatus(
                        tile.deployZone > 0
                            ? $"Ally start ON {tile.cell.x},{tile.cell.y}"
                            : $"Ally start OFF {tile.cell.x},{tile.cell.y}"
                    );
                    break;
                case BattleMapRuntimeEditTool.EnemyStartPoint:
                    EnsureWalkableForMarker(tile);
                    ToggleTag(tile, BattleMapRuntimeEditStore.EnemySpawnTag);
                    SetStatus(
                        HasTag(tile, BattleMapRuntimeEditStore.EnemySpawnTag)
                            ? $"Enemy start ON {tile.cell.x},{tile.cell.y}"
                            : $"Enemy start OFF {tile.cell.x},{tile.cell.y}"
                    );
                    break;
            }

            EnsureTag(tile, BattleMapRuntimeEditStore.RuntimeEditedTag);
            tile.SetHighlight(OverlayColorFor(tile));
        }

        private static void MakeWalkableDefault(BattleTestTile tile)
        {
            tile.walkable = true;
            tile.occupyAllowed = true;
            tile.moveCost = 1;
            tile.blocksLineOfSight = false;
            tile.blocksProjectiles = false;
            tile.coverBonus = 0;
            tile.baseCoverBonus = 0;
            tile.hazardType = HazardType.None;
            tile.danger = false;
            if (tile.terrain == TerrainType.Wall || tile.terrain == TerrainType.Cliff)
            {
                tile.terrain = TerrainType.Stone;
            }

            EnsureTag(tile, BattleMapRuntimeEditStore.RuntimeEditedTag);
        }

        private static void MakeBlocked(BattleTestTile tile)
        {
            tile.walkable = false;
            tile.occupyAllowed = false;
            tile.moveCost = 99;
            tile.blocksLineOfSight = true;
            tile.blocksProjectiles = true;
            tile.deployZone = 0;
            RemoveTag(tile, BattleMapRuntimeEditStore.EnemySpawnTag);
            tile.terrain = TerrainType.Wall;
            EnsureTag(tile, BattleMapRuntimeEditStore.RuntimeEditedTag);
        }

        private static void EnsureWalkableForMarker(BattleTestTile tile)
        {
            MakeWalkableDefault(tile);
        }

        private void SaveCurrentMap()
        {
            BattleMapRuntimeEditStore.SaveOverrides(
                activeVariant,
                controller == null ? null : controller.PreviewTiles,
                out string repoPath,
                out string persistentPath
            );
            BattleMapRuntimeCatalog.ClearCache();
            SetStatus($"Saved CSV: {ShortPath(repoPath)}");
            Debug.Log($"[BattleMapRuntimeEditorOverlay] Saved runtime edit CSV: {repoPath}");
            Debug.Log(
                $"[BattleMapRuntimeEditorOverlay] Saved persistent edit CSV: {persistentPath}"
            );
        }

        private void LoadSavedMap()
        {
            if (
                !BattleMapRuntimeEditStore.TryLoadBestOverride(
                    activeVariant,
                    out List<BattleMapRuntimeCellEdit> edits,
                    out string path,
                    out string message
                )
            )
            {
                SetStatus(message);
                return;
            }

            int applied = 0;
            for (int i = 0; i < edits.Count; i++)
            {
                BattleMapRuntimeCellEdit edit = edits[i];
                BattleTestTile tile = controller.GetPreviewTileAt(edit.cell);
                if (tile == null)
                {
                    continue;
                }

                edit.ApplyTo(tile);
                applied++;
            }

            controller.ClearPreviewHighlights();
            BattleMapRuntimeCatalog.ClearCache();
            SetStatus($"Loaded {applied} cells: {ShortPath(path)}");
        }

        private Color OverlayColorFor(BattleTestTile tile)
        {
            if (tile == null)
            {
                return Color.clear;
            }

            if (IsBlocked(tile))
            {
                return WithAlpha(BlockedTileColor, overlayAlpha + 0.10f);
            }

            if (HasTag(tile, BattleMapRuntimeEditStore.EnemySpawnTag))
            {
                return WithAlpha(EnemyStartColor, overlayAlpha + 0.16f);
            }

            if (tile.deployZone > 0)
            {
                return WithAlpha(AllyStartColor, overlayAlpha + 0.12f);
            }

            return WithAlpha(DefaultTileColor, overlayAlpha);
        }

        private string CurrentToolText()
        {
            switch (tool)
            {
                case BattleMapRuntimeEditTool.AllyStartZone:
                    return $"Tool: ALLY START ZONE   Opacity {Mathf.RoundToInt(overlayAlpha * 100f)}%";
                case BattleMapRuntimeEditTool.EnemyStartPoint:
                    return $"Tool: ENEMY START POINT   Opacity {Mathf.RoundToInt(overlayAlpha * 100f)}%";
                default:
                    return $"Tool: BLOCKED TILE   Opacity {Mathf.RoundToInt(overlayAlpha * 100f)}%";
            }
        }

        private static bool ShouldOpenFromCommandLine()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], "--map-editor", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string MapTitle(BattleTestMapVariant variant)
        {
            for (int i = 0; i < MapEntries.Length; i++)
            {
                if (MapEntries[i].variant == variant)
                {
                    return MapEntries[i].title;
                }
            }

            return variant.ToString();
        }

        private string HoverText()
        {
            if (hoverTile == null)
            {
                return "Hover: none";
            }

            string state =
                IsBlocked(hoverTile) ? "BLOCKED"
                : HasTag(hoverTile, BattleMapRuntimeEditStore.EnemySpawnTag) ? "ENEMY START"
                : hoverTile.deployZone > 0 ? "ALLY START"
                : "WALKABLE";
            return $"Hover: {hoverTile.cell.x},{hoverTile.cell.y} {state}";
        }

        private void HideUnits()
        {
            if (controller == null)
            {
                return;
            }

            foreach (BattleTestUnit unit in controller.PreviewUnits)
            {
                if (unit != null && unit.view != null)
                {
                    unit.view.gameObject.SetActive(false);
                }
            }
        }

        private void RestoreUnits()
        {
            if (controller == null)
            {
                return;
            }

            foreach (BattleTestUnit unit in controller.PreviewUnits)
            {
                if (unit != null && unit.view != null && !unit.defeated)
                {
                    unit.view.gameObject.SetActive(true);
                }
            }
        }

        private static bool IsBlocked(BattleTestTile tile)
        {
            return tile == null || !tile.walkable || !tile.occupyAllowed || tile.moveCost >= 90;
        }

        private static bool HasTag(BattleTestTile tile, string tag)
        {
            return tile != null && tile.HasTag(tag);
        }

        private static void ToggleTag(BattleTestTile tile, string tag)
        {
            if (HasTag(tile, tag))
            {
                RemoveTag(tile, tag);
            }
            else
            {
                EnsureTag(tile, tag);
            }
        }

        private static void EnsureTag(BattleTestTile tile, string tag)
        {
            if (tile == null || string.IsNullOrEmpty(tag))
            {
                return;
            }

            if (tile.tags == null)
            {
                tile.tags = new List<string>();
            }

            if (!tile.HasTag(tag))
            {
                tile.tags.Add(tag);
            }
        }

        private static void RemoveTag(BattleTestTile tile, string tag)
        {
            if (tile == null || tile.tags == null || string.IsNullOrEmpty(tag))
            {
                return;
            }

            for (int i = tile.tags.Count - 1; i >= 0; i--)
            {
                if (string.Equals(tile.tags[i], tag, System.StringComparison.OrdinalIgnoreCase))
                {
                    tile.tags.RemoveAt(i);
                }
            }
        }

        private void SetStatus(string text)
        {
            statusText = text ?? string.Empty;
            statusUntil = Time.realtimeSinceStartup + 2.7f;
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = Mathf.Clamp01(alpha);
            return color;
        }

        private static string ShortPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            const int max = 58;
            if (path.Length <= max)
            {
                return path;
            }

            return "..." + path.Substring(path.Length - max);
        }

        private void EnsureStyles()
        {
            if (panelStyle != null)
            {
                return;
            }

            panelStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = MakeTexture(new Color(0f, 0f, 0f, 0.76f)) },
                padding = new RectOffset(12, 12, 10, 10),
            };
            listPanelStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = MakeTexture(new Color(0.015f, 0.018f, 0.020f, 0.93f)) },
            };
            cardStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = MakeTexture(new Color(0.07f, 0.08f, 0.075f, 0.94f)) },
                border = new RectOffset(2, 2, 2, 2),
            };
            savedCardStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = MakeTexture(new Color(0.11f, 0.10f, 0.055f, 0.96f)) },
                border = new RectOffset(2, 2, 2, 2),
            };
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.86f, 0.45f, 1f) },
            };
            subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { textColor = new Color(0.70f, 0.73f, 0.70f, 1f) },
            };
            textStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                normal = { textColor = new Color(0.92f, 0.92f, 0.86f, 1f) },
            };
            hotkeyStyle = new GUIStyle(textStyle)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.80f, 0.94f, 1f, 1f) },
            };
        }

        private static Texture2D MakeTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }
    }
}
