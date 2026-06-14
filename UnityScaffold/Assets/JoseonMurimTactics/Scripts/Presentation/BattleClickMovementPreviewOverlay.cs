using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace JoseonMurimTactics
{
    /// <summary>
    /// Fire-Emblem style movement preview for BattleTest.
    ///
    /// The existing BattleTestController already owns the real movement rules.
    /// This component reads the controller preview API and only paints a preview overlay when the
    /// player has directly clicked the currently acting ally. If the selected unit is not the unit
    /// whose turn/action is currently active, the overlay stays hidden.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(9200)]
    public sealed class BattleClickMovementPreviewOverlay : MonoBehaviour
    {
        private enum PreviewKind
        {
            Current,
            Reachable,
            Blocked,
            PathStraight,
            PathTurn,
            PathStart,
            PathEnd,
            CursorTarget,
            CursorInvalid
        }

        private static Material defaultSpriteMaterial;

        [Header("Selection Gate")]
        [SerializeField] private bool requireDirectUnitClick = true;
        [SerializeField] private bool hideNativeHighlightsUntilClicked = true;
        [SerializeField] private bool showOnlyDuringMoveCommand = true;
        [SerializeField] private bool hideWhenBusy = true;

        [Header("Blocked Preview")]
        [SerializeField] private bool showBlockedCells = true;
        [SerializeField] private int blockedPreviewPadding = 1;
        [SerializeField] private int maxBlockedCells = 90;

        [Header("Look")]
        [SerializeField] private int sortingOrder = 178;
        [SerializeField] private float tileScalePadding = 0.94f;
        [SerializeField] private float clickPickRadius = 0.62f;
        [SerializeField] private float refreshInterval = 0.035f;
        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 0.018f, -0.02f);
        [SerializeField] private Color currentColor = new Color(1f, 0.74f, 0.18f, 0.52f);
        [SerializeField] private Color reachableColor = new Color(0.18f, 0.52f, 1f, 0.42f);
        [SerializeField] private Color blockedColor = new Color(1f, 0.16f, 0.12f, 0.40f);

        private BattleTestController controller;
        private Transform overlayRoot;
        private Sprite diamondSprite;
        private Sprite currentCellSprite;
        private Sprite reachableCellSprite;
        private Sprite blockedCellSprite;
        private Sprite pathStraightSprite;
        private Sprite pathTurnSprite;
        private Sprite pathStartSprite;
        private Sprite pathEndSprite;
        private Sprite cursorTargetSprite;
        private Sprite cursorInvalidSprite;
        private readonly List<SpriteRenderer> visibleRenderers = new List<SpriteRenderer>(128);
        private readonly Stack<SpriteRenderer> rendererPool = new Stack<SpriteRenderer>(128);
        private readonly Dictionary<Vector2Int, int> reachableCells = new Dictionary<Vector2Int, int>(128);
        private readonly List<BattleTestTile> cachedTiles = new List<BattleTestTile>(256);
        private readonly List<Vector2Int> blockedScratch = new List<Vector2Int>(128);

        private BattleTestUnit clickedUnit;
        private int clickedFrame = -1000;
        private BattleTestUnit lastActiveUnit;
        private Vector2Int lastActiveCell = new Vector2Int(int.MinValue, int.MinValue);
        private Vector2Int lastHoverCell = new Vector2Int(int.MinValue, int.MinValue);
        private string lastCommandName = string.Empty;
        private bool lastVisible;
        private float nextRefreshTime;

        public bool Visible => lastVisible;

        private void Awake()
        {
            controller = GetComponent<BattleTestController>();
            if (controller == null)
            {
                controller = UnityEngine.Object.FindAnyObjectByType<BattleTestController>();
            }

            EnsureRoot();
            EnsureSprite();
        }

        private void OnDisable()
        {
            HideOverlay();
        }

        private void Update()
        {
            if (controller == null)
            {
                controller = UnityEngine.Object.FindAnyObjectByType<BattleTestController>();
            }

            if (!Input.GetMouseButtonDown(0))
            {
                return;
            }

            RegisterDirectClick();
        }

        private void LateUpdate()
        {
            if (controller == null)
            {
                HideOverlay();
                return;
            }

            if (Time.unscaledTime < nextRefreshTime && Time.frameCount != clickedFrame)
            {
                return;
            }

            nextRefreshTime = Time.unscaledTime + Mathf.Max(0.005f, refreshInterval);

            BattleTestUnit activeUnit = controller.PreviewActiveUnit;
            if (!ReferenceEquals(activeUnit, lastActiveUnit))
            {
                lastActiveUnit = activeUnit;
                lastActiveCell = new Vector2Int(int.MinValue, int.MinValue);
                lastHoverCell = new Vector2Int(int.MinValue, int.MinValue);
                lastCommandName = string.Empty;
            }

            bool visible = ShouldShow(activeUnit);
            lastVisible = visible;

            if (!visible)
            {
                HideOverlay();
                if (hideNativeHighlightsUntilClicked && !controller.PreviewDeploymentMode)
                {
                    ClearNativeHighlightsOnce();
                }
                return;
            }

            Rebuild(activeUnit);
        }

        private void RegisterDirectClick()
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                // UI 버튼 클릭은 캐릭터 직접 클릭으로 보지 않는다.
                return;
            }

            BattleTestUnit clicked = PickUnitUnderMouse();
            if (clicked == null)
            {
                clickedUnit = null;
                clickedFrame = Time.frameCount;
                return;
            }

            clickedUnit = clicked;
            clickedFrame = Time.frameCount;
        }

        private BattleTestUnit PickUnitUnderMouse()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                return null;
            }

            Vector3 world = camera.ScreenToWorldPoint(Input.mousePosition);
            Vector2 point = new Vector2(world.x, world.y);
            Collider2D[] hits = Physics2D.OverlapPointAll(point);

            for (int i = 0; i < hits.Length; i++)
            {
                BattleTestUnit unit = ExtractUnitFromCollider(hits[i]);
                if (unit != null)
                {
                    return unit;
                }
            }

            // Some runtime unit views may not have a collider on the full sprite.
            // Fallback: choose the nearest unit transform around the clicked cell.
            BattleTestUnit nearest = null;
            float best = clickPickRadius * clickPickRadius;
            foreach (BattleTestUnit unit in EnumerateUnits())
            {
                if (unit == null || unit.defeated)
                {
                    continue;
                }

                Vector3 unitWorld = UnitWorldPosition(unit);
                float sqr = ((Vector2)unitWorld - point).sqrMagnitude;
                if (sqr <= best)
                {
                    best = sqr;
                    nearest = unit;
                }
            }

            return nearest;
        }

        private BattleTestUnit ExtractUnitFromCollider(Collider2D hit)
        {
            if (hit == null)
            {
                return null;
            }

            BattleTestUnitView view = hit.GetComponentInParent<BattleTestUnitView>();
            return view == null ? null : view.Unit;
        }

        private bool ShouldShow(BattleTestUnit activeUnit)
        {
            if (activeUnit == null || controller.PreviewBattleOver)
            {
                return false;
            }

            if (hideWhenBusy && controller.PreviewBusy)
            {
                return false;
            }

            if (!IsPlayerPhase())
            {
                return false;
            }

            if (requireDirectUnitClick && !ReferenceEquals(activeUnit, clickedUnit))
            {
                return false;
            }

            if (activeUnit.defeated)
            {
                return false;
            }

            if (!IsAlly(activeUnit))
            {
                return false;
            }

            if (activeUnit.moved || activeUnit.acted)
            {
                return false;
            }

            if (!activeUnit.CanMove)
            {
                return false;
            }

            if (showOnlyDuringMoveCommand && !IsMoveCommand())
            {
                return false;
            }

            return true;
        }

        private bool IsPlayerPhase()
        {
            return controller != null && controller.PreviewIsPlayerPhase;
        }

        private bool IsMoveCommand()
        {
            return controller == null || controller.PreviewCommandMode == BattleCommandMode.Move;
        }

        private bool IsAlly(BattleTestUnit unit)
        {
            return unit != null && unit.definition != null && unit.definition.faction == Faction.Ally;
        }

        private void Rebuild(BattleTestUnit activeUnit)
        {
            Vector2Int start = ReadCell(activeUnit);
            Vector2Int hoverCell = ResolveHoverCell();
            string commandName = controller == null ? string.Empty : controller.PreviewCommandMode.ToString();
            if (ReferenceEquals(activeUnit, lastActiveUnit) && start == lastActiveCell &&
                hoverCell == lastHoverCell && commandName == lastCommandName && visibleRenderers.Count > 0)
            {
                // Keep the overlay stable unless the selected unit or command changed.
                return;
            }

            lastActiveCell = start;
            lastHoverCell = hoverCell;
            lastCommandName = commandName;
            HideOverlay();
            reachableCells.Clear();
            blockedScratch.Clear();
            FillReachable(activeUnit, reachableCells);

            int moveBudget = ResolveMoveBudget(activeUnit, reachableCells);
            float tileWidth;
            float tileHeight;
            ResolveTileSize(out tileWidth, out tileHeight);

            DrawCell(start, PreviewKind.Current, 0, tileWidth, tileHeight);

            foreach (KeyValuePair<Vector2Int, int> pair in reachableCells)
            {
                if (pair.Key == start)
                {
                    continue;
                }

                DrawCell(pair.Key, PreviewKind.Reachable, pair.Value, tileWidth, tileHeight);
            }

            if (showBlockedCells)
            {
                int blockedRadius = Mathf.Max(1, moveBudget) + Mathf.Max(0, blockedPreviewPadding);
                cachedTiles.Clear();
                CollectTiles(cachedTiles);

                for (int i = 0; i < cachedTiles.Count; i++)
                {
                    BattleTestTile tile = cachedTiles[i];
                    Vector2Int cell = ReadCell(tile);
                    if (cell == start || reachableCells.ContainsKey(cell))
                    {
                        continue;
                    }

                    int distance = Manhattan(start, cell);
                    if (distance > blockedRadius)
                    {
                        continue;
                    }

                    if (!ShouldDrawBlocked(tile, cell, start, moveBudget, distance))
                    {
                        continue;
                    }

                    blockedScratch.Add(cell);
                }

                blockedScratch.Sort((a, b) => Manhattan(start, a).CompareTo(Manhattan(start, b)));
                int count = Mathf.Min(maxBlockedCells, blockedScratch.Count);
                for (int i = 0; i < count; i++)
                {
                    DrawCell(blockedScratch[i], PreviewKind.Blocked, -1, tileWidth, tileHeight);
                }
            }

            DrawHoverPath(activeUnit, start, hoverCell, tileWidth, tileHeight);
        }

        private bool ShouldDrawBlocked(BattleTestTile tile, Vector2Int cell, Vector2Int start, int budget, int distance)
        {
            if (tile == null)
            {
                return false;
            }

            if (!tile.walkable)
            {
                return true;
            }

            BattleTestUnit occupant = controller == null ? null : controller.GetPreviewUnitAt(cell);
            if (occupant != null && !ReferenceEquals(occupant, lastActiveUnit))
            {
                return true;
            }

            // Walkable but not reachable due to cost/elevation/body-blocking/pathing.
            return distance <= budget + Mathf.Max(0, blockedPreviewPadding);
        }

        private void DrawCell(Vector2Int cell, PreviewKind kind, int cost, float tileWidth, float tileHeight)
        {
            SpriteRenderer renderer = GetRenderer();
            renderer.name = kind + "_" + cell.x + "_" + cell.y;
            renderer.sprite = SpriteFor(kind) == null ? diamondSprite : SpriteFor(kind);
            ApplyDefaultSpriteMaterial(renderer);
            renderer.color = ColorFor(kind, cost);
            renderer.sortingLayerName = "Default";
            renderer.sortingOrder = sortingOrder + (kind == PreviewKind.Current ? 2 : kind == PreviewKind.Blocked ? 1 : 0);
            renderer.transform.SetParent(overlayRoot, false);
            renderer.transform.position = CellWorldPosition(cell) + worldOffset;
            renderer.transform.localRotation = Quaternion.identity;
            renderer.transform.localScale = ScaleToWorldSize(renderer.sprite, tileWidth * tileScalePadding,
                                                             tileHeight * tileScalePadding);
            renderer.gameObject.SetActive(true);
            visibleRenderers.Add(renderer);
        }

        private void DrawHoverPath(BattleTestUnit activeUnit, Vector2Int start, Vector2Int hoverCell, float tileWidth,
                                   float tileHeight)
        {
            if (IsInvalidCell(hoverCell) || hoverCell == start)
            {
                return;
            }

            BattleTestUnit occupant = controller == null ? null : controller.GetPreviewUnitAt(hoverCell);
            bool validDestination = reachableCells.ContainsKey(hoverCell) &&
                                    (occupant == null || ReferenceEquals(occupant, activeUnit));
            if (!validDestination)
            {
                DrawPathMarker(hoverCell, PreviewKind.CursorInvalid, 0f, tileWidth, tileHeight);
                return;
            }

            List<Vector2Int> path = controller == null
                                        ? new List<Vector2Int>()
                                        : controller.GetPreviewMovePath(activeUnit, hoverCell);
            if (path.Count < 2)
            {
                DrawPathMarker(hoverCell, PreviewKind.CursorTarget, 0f, tileWidth, tileHeight);
                return;
            }

            for (int i = 0; i < path.Count; i++)
            {
                PreviewKind kind;
                float rotation;
                if (i == 0)
                {
                    kind = PreviewKind.PathStart;
                    rotation = AngleFromTo(path[i], path[i + 1]);
                }
                else if (i == path.Count - 1)
                {
                    kind = PreviewKind.PathEnd;
                    rotation = AngleFromTo(path[i - 1], path[i]);
                }
                else
                {
                    Vector2Int incoming = path[i] - path[i - 1];
                    Vector2Int outgoing = path[i + 1] - path[i];
                    kind = incoming == outgoing ? PreviewKind.PathStraight : PreviewKind.PathTurn;
                    rotation = AngleFromTo(path[i], path[i + 1]);
                }

                DrawPathMarker(path[i], kind, rotation, tileWidth, tileHeight);
            }

            DrawPathMarker(hoverCell, PreviewKind.CursorTarget, 0f, tileWidth, tileHeight);
        }

        private void DrawPathMarker(Vector2Int cell, PreviewKind kind, float rotation, float tileWidth,
                                    float tileHeight)
        {
            SpriteRenderer renderer = GetRenderer();
            renderer.name = kind + "_" + cell.x + "_" + cell.y;
            renderer.sprite = SpriteFor(kind) == null ? diamondSprite : SpriteFor(kind);
            ApplyDefaultSpriteMaterial(renderer);
            renderer.color = ColorFor(kind, 0);
            renderer.sortingLayerName = "Default";
            renderer.sortingOrder = sortingOrder + 9;
            renderer.transform.SetParent(overlayRoot, false);
            renderer.transform.position = CellWorldPosition(cell) + worldOffset + new Vector3(0f, 0.030f, -0.03f);
            renderer.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);
            float size = Mathf.Min(tileWidth, tileHeight) * (kind == PreviewKind.CursorTarget ||
                         kind == PreviewKind.CursorInvalid ? 0.92f : 0.62f);
            renderer.transform.localScale = ScaleToWorldSize(renderer.sprite, size, size);
            renderer.gameObject.SetActive(true);
            visibleRenderers.Add(renderer);
        }

        private static Vector3 ScaleToWorldSize(Sprite sprite, float worldWidth, float worldHeight)
        {
            if (sprite == null || sprite.bounds.size.x <= 0.001f || sprite.bounds.size.y <= 0.001f)
            {
                return new Vector3(worldWidth, worldHeight, 1f);
            }

            return new Vector3(worldWidth / sprite.bounds.size.x, worldHeight / sprite.bounds.size.y, 1f);
        }

        private Color ColorFor(PreviewKind kind, int cost)
        {
            switch (kind)
            {
                case PreviewKind.Current:
                    return currentColor;
                case PreviewKind.Blocked:
                    return blockedColor;
                case PreviewKind.PathStraight:
                case PreviewKind.PathTurn:
                case PreviewKind.PathStart:
                case PreviewKind.PathEnd:
                case PreviewKind.CursorTarget:
                case PreviewKind.CursorInvalid:
                    return Color.white;
                default:
                    if (cost <= 0)
                    {
                        return reachableColor;
                    }

                    // Slightly fade expensive edge cells so the movement budget reads like a graph.
                    float alpha = Mathf.Clamp(reachableColor.a - (cost * 0.012f), 0.22f, reachableColor.a);
                    return new Color(reachableColor.r, reachableColor.g, reachableColor.b, alpha);
            }
        }

        private Sprite SpriteFor(PreviewKind kind)
        {
            switch (kind)
            {
                case PreviewKind.Current:
                    return currentCellSprite;
                case PreviewKind.Blocked:
                    return blockedCellSprite;
                case PreviewKind.PathStraight:
                    return pathStraightSprite;
                case PreviewKind.PathTurn:
                    return pathTurnSprite;
                case PreviewKind.PathStart:
                    return pathStartSprite;
                case PreviewKind.PathEnd:
                    return pathEndSprite;
                case PreviewKind.CursorTarget:
                    return cursorTargetSprite;
                case PreviewKind.CursorInvalid:
                    return cursorInvalidSprite;
                default:
                    return reachableCellSprite;
            }
        }

        private SpriteRenderer GetRenderer()
        {
            if (rendererPool.Count > 0)
            {
                return rendererPool.Pop();
            }

            GameObject obj = new GameObject("PreviewCell");
            obj.hideFlags = HideFlags.DontSave;
            SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
            ApplyDefaultSpriteMaterial(renderer);
            return renderer;
        }

        private static void ApplyDefaultSpriteMaterial(SpriteRenderer renderer)
        {
            if (renderer == null)
            {
                return;
            }

            Material material = DefaultSpriteMaterial();
            if (material != null)
            {
                renderer.sharedMaterial = material;
            }
        }

        private static Material DefaultSpriteMaterial()
        {
            if (defaultSpriteMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader != null)
                {
                    defaultSpriteMaterial = new Material(shader) { name = "RuntimePreviewSpriteMaterial" };
                }
            }

            return defaultSpriteMaterial;
        }

        private void HideOverlay()
        {
            for (int i = 0; i < visibleRenderers.Count; i++)
            {
                SpriteRenderer renderer = visibleRenderers[i];
                if (renderer == null)
                {
                    continue;
                }

                renderer.gameObject.SetActive(false);
                rendererPool.Push(renderer);
            }

            visibleRenderers.Clear();
        }

        private void ClearNativeHighlightsOnce()
        {
            // BattleTestController.RefreshHighlights can be called by command/phase changes.
            // Clearing here keeps movement highlights click-gated even when the controller auto-selects
            // the next ally or the player clicks a unit that cannot currently act.
            if (controller != null)
            {
                controller.ClearPreviewHighlights();
            }
        }

        private void FillReachable(BattleTestUnit activeUnit, Dictionary<Vector2Int, int> output)
        {
            if (controller == null || activeUnit == null)
            {
                return;
            }

            foreach (KeyValuePair<Vector2Int, int> pair in controller.GetPreviewReachableCells(activeUnit))
            {
                output[pair.Key] = pair.Value;
            }
        }

        private int ResolveMoveBudget(BattleTestUnit activeUnit, Dictionary<Vector2Int, int> reachable)
        {
            int budget = controller == null || activeUnit == null ? 0 : controller.GetPreviewMoveRange(activeUnit);
            if (budget <= 0 && activeUnit != null)
            {
                budget = activeUnit.actions == null ? 0 : activeUnit.actions.movementLeft;
            }

            if (budget <= 0)
            {
                foreach (int value in reachable.Values)
                {
                    budget = Mathf.Max(budget, value);
                }
            }

            return Mathf.Max(1, budget);
        }

        private void CollectTiles(List<BattleTestTile> output)
        {
            if (controller != null && controller.PreviewTiles != null)
            {
                foreach (BattleTestTile tile in controller.PreviewTiles)
                {
                    if (tile != null)
                    {
                        output.Add(tile);
                    }
                }

                return;
            }

        }

        private IEnumerable<BattleTestUnit> EnumerateUnits()
        {
            if (controller != null && controller.PreviewUnits != null)
            {
                foreach (BattleTestUnit unit in controller.PreviewUnits)
                {
                    if (unit != null)
                    {
                        yield return unit;
                    }
                }

                yield break;
            }
        }

        private static Vector2Int ReadCell(BattleTestUnit source)
        {
            return source == null ? InvalidCell() : source.cell;
        }

        private static Vector2Int ReadCell(BattleTestTile source)
        {
            return source == null ? InvalidCell() : source.cell;
        }

        private Vector3 UnitWorldPosition(BattleTestUnit unit)
        {
            return unit == null || unit.view == null ? CellWorldPosition(ReadCell(unit)) : unit.view.transform.position;
        }

        private Vector3 CellWorldPosition(Vector2Int cell)
        {
            if (controller != null)
            {
                return controller.GetPreviewUnitWorldPosition(cell);
            }

            BattleMapSceneController sceneController = UnityEngine.Object.FindAnyObjectByType<BattleMapSceneController>();
            if (sceneController != null)
            {
                return sceneController.CellToWorld(cell);
            }

            BattleMapTilemapBinder binder = UnityEngine.Object.FindAnyObjectByType<BattleMapTilemapBinder>();
            if (binder != null && binder.Grid != null)
            {
                return binder.Grid.CellToWorld(new Vector3Int(cell.x, cell.y, 0));
            }

            return new Vector3(cell.x, cell.y, 0f);
        }

        private void ResolveTileSize(out float tileWidth, out float tileHeight)
        {
            tileWidth = 1.16f;
            tileHeight = 0.62f;

            BattleMapSceneController sceneController = UnityEngine.Object.FindAnyObjectByType<BattleMapSceneController>();
            if (sceneController != null)
            {
                tileWidth = Mathf.Max(0.1f, sceneController.TileWidth);
                tileHeight = Mathf.Max(0.1f, sceneController.TileHeight);
                return;
            }

            BattleMapTilemapBinder binder = UnityEngine.Object.FindAnyObjectByType<BattleMapTilemapBinder>();
            if (binder != null && binder.Grid != null)
            {
                Vector3 size = binder.Grid.cellSize;
                if (size.x > 0.01f)
                {
                    tileWidth = size.x;
                }
                if (size.y > 0.01f)
                {
                    tileHeight = size.y;
                }
            }
        }

        private void EnsureRoot()
        {
            if (overlayRoot != null)
            {
                return;
            }

            Transform existing = transform.Find("ClickMovementPreviewOverlay");
            if (existing != null)
            {
                overlayRoot = existing;
                return;
            }

            GameObject root = new GameObject("ClickMovementPreviewOverlay");
            root.hideFlags = HideFlags.DontSave;
            root.transform.SetParent(transform, false);
            overlayRoot = root.transform;
        }

        private void EnsureSprite()
        {
            currentCellSprite = LoadTacticalSprite("ui_tile_current_unit");
            reachableCellSprite = LoadTacticalSprite("ui_tile_move_reachable");
            blockedCellSprite = LoadTacticalSprite("ui_tile_move_blocked");
            pathStraightSprite = LoadTacticalSprite("ui_path_arrow_straight");
            pathTurnSprite = LoadTacticalSprite("ui_path_arrow_turn");
            pathStartSprite = LoadTacticalSprite("ui_path_arrow_start");
            pathEndSprite = LoadTacticalSprite("ui_path_arrow_end");
            cursorTargetSprite = LoadTacticalSprite("ui_cursor_target");
            cursorInvalidSprite = LoadTacticalSprite("ui_cursor_invalid");

            diamondSprite = reachableCellSprite;
            if (diamondSprite != null)
            {
                return;
            }

            const int size = 96;
            const int center = size / 2;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "RuntimeDiamondPreviewTile";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            Color clear = new Color(1f, 1f, 1f, 0f);
            Color fill = new Color(1f, 1f, 1f, 0.52f);
            Color edge = new Color(1f, 1f, 1f, 0.96f);
            Color glow = new Color(1f, 1f, 1f, 0.20f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Abs(x - center) / (float)(center - 4);
                    float dy = Mathf.Abs(y - center) / (float)(center - 4);
                    float d = dx + dy;
                    Color pixel = clear;
                    if (d <= 0.86f)
                    {
                        pixel = fill;
                    }
                    else if (d <= 1.00f)
                    {
                        pixel = edge;
                    }
                    else if (d <= 1.08f)
                    {
                        pixel = glow;
                    }

                    texture.SetPixel(x, y, pixel);
                }
            }

            texture.Apply(false, true);
            diamondSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size, 0, SpriteMeshType.FullRect);
            diamondSprite.name = "RuntimeDiamondPreviewSprite";
        }

        private static Sprite LoadTacticalSprite(string id)
        {
            return string.IsNullOrEmpty(id) ? null : Resources.Load<Sprite>("UI/BattleHUD/Tactical/" + id);
        }

        private Vector2Int ResolveHoverCell()
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return InvalidCell();
            }

            Camera camera = Camera.main;
            if (camera == null)
            {
                return InvalidCell();
            }

            Vector3 world = camera.ScreenToWorldPoint(Input.mousePosition);
            Vector2 point = new Vector2(world.x, world.y);
            Collider2D[] hits = Physics2D.OverlapPointAll(point);
            for (int i = 0; i < hits.Length; i++)
            {
                BattleTestTile tile = hits[i] == null ? null : hits[i].GetComponent<BattleTestTile>();
                if (tile != null)
                {
                    return tile.cell;
                }
            }

            if (controller != null)
            {
                Vector2Int gridCell = controller.GetPreviewGridCell(point);
                BattleTestTile tile = controller.GetPreviewTileAt(gridCell);
                return tile == null ? InvalidCell() : gridCell;
            }

            return InvalidCell();
        }

        private float AngleFromTo(Vector2Int from, Vector2Int to)
        {
            Vector3 start = CellWorldPosition(from);
            Vector3 end = CellWorldPosition(to);
            Vector3 delta = end - start;
            if (delta.sqrMagnitude <= 0.000001f)
            {
                return 0f;
            }

            return Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        }

        private static Vector2Int InvalidCell()
        {
            return new Vector2Int(int.MinValue, int.MinValue);
        }

        private static bool IsInvalidCell(Vector2Int cell)
        {
            return cell.x == int.MinValue && cell.y == int.MinValue;
        }


        private static int Manhattan(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }
    }
}
