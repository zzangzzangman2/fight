using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;

namespace JoseonMurimTactics
{
    /// <summary>
    /// Fire-Emblem style movement preview for BattleTest.
    ///
    /// The existing BattleTestController already owns the real movement rules.
    /// This component reads that state with reflection and only paints a preview overlay when the
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
            Blocked
        }

        private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags AnyStatic = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly Type Vector2IntType = typeof(Vector2Int);

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
        private readonly List<SpriteRenderer> visibleRenderers = new List<SpriteRenderer>(128);
        private readonly Stack<SpriteRenderer> rendererPool = new Stack<SpriteRenderer>(128);
        private readonly Dictionary<Vector2Int, int> reachableCells = new Dictionary<Vector2Int, int>(128);
        private readonly List<object> cachedTiles = new List<object>(256);
        private readonly List<Vector2Int> blockedScratch = new List<Vector2Int>(128);

        private object clickedUnit;
        private int clickedFrame = -1000;
        private object lastActiveUnit;
        private Vector2Int lastActiveCell = new Vector2Int(int.MinValue, int.MinValue);
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

            object activeUnit = GetValue(controller, "activeUnit");
            if (!ReferenceEquals(activeUnit, lastActiveUnit))
            {
                lastActiveUnit = activeUnit;
                lastActiveCell = new Vector2Int(int.MinValue, int.MinValue);
                lastCommandName = string.Empty;
            }

            bool visible = ShouldShow(activeUnit);
            lastVisible = visible;

            if (!visible)
            {
                HideOverlay();
                if (hideNativeHighlightsUntilClicked)
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

            object clicked = PickUnitUnderMouse();
            if (clicked == null)
            {
                clickedUnit = null;
                clickedFrame = Time.frameCount;
                return;
            }

            clickedUnit = clicked;
            clickedFrame = Time.frameCount;
        }

        private object PickUnitUnderMouse()
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
                object unit = ExtractUnitFromCollider(hits[i]);
                if (unit != null)
                {
                    return unit;
                }
            }

            // Some runtime unit views may not have a collider on the full sprite.
            // Fallback: choose the nearest unit transform around the clicked cell.
            object nearest = null;
            float best = clickPickRadius * clickPickRadius;
            foreach (object unit in EnumerateUnits())
            {
                if (unit == null || ReadBool(unit, "defeated"))
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

        private object ExtractUnitFromCollider(Collider2D hit)
        {
            if (hit == null)
            {
                return null;
            }

            Transform cursor = hit.transform;
            while (cursor != null)
            {
                MonoBehaviour[] behaviours = cursor.GetComponents<MonoBehaviour>();
                for (int i = 0; i < behaviours.Length; i++)
                {
                    MonoBehaviour behaviour = behaviours[i];
                    if (behaviour == null)
                    {
                        continue;
                    }

                    Type type = behaviour.GetType();
                    if (type.Name == "BattleTestUnitView")
                    {
                        object unit = GetValue(behaviour, "Unit") ?? GetValue(behaviour, "unit");
                        if (unit != null)
                        {
                            return unit;
                        }
                    }

                    // CharacterVisualController usually lives under a BattleTestUnitView.
                    object possibleUnit = GetValue(behaviour, "Unit");
                    if (possibleUnit != null && possibleUnit.GetType().Name.Contains("BattleTestUnit"))
                    {
                        return possibleUnit;
                    }
                }

                cursor = cursor.parent;
            }

            return null;
        }

        private bool ShouldShow(object activeUnit)
        {
            if (activeUnit == null || ReadBool(controller, "battleOver"))
            {
                return false;
            }

            if (hideWhenBusy && ReadBool(controller, "busy"))
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

            if (ReadBool(activeUnit, "defeated"))
            {
                return false;
            }

            if (!IsAlly(activeUnit))
            {
                return false;
            }

            if (ReadBool(activeUnit, "moved") || ReadBool(activeUnit, "acted"))
            {
                // 이미 움직였거나 행동 완료한 캐릭터는 이동 미리보기를 띄우지 않는다.
                return false;
            }

            object canMove = GetValue(activeUnit, "CanMove");
            if (canMove is bool && !(bool)canMove)
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
            object phaseTurn = GetValue(controller, "phaseTurn");
            if (phaseTurn == null)
            {
                object phase = GetValue(controller, "phase");
                return phase == null || string.Equals(phase.ToString(), "PlayerPhase", StringComparison.OrdinalIgnoreCase);
            }

            object isPlayer = GetValue(phaseTurn, "IsPlayerPhase");
            if (isPlayer is bool)
            {
                return (bool)isPlayer;
            }

            object phaseValue = GetValue(phaseTurn, "CurrentPhase") ?? GetValue(phaseTurn, "Phase");
            return phaseValue == null || string.Equals(phaseValue.ToString(), "PlayerPhase", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsMoveCommand()
        {
            object commandMode = GetValue(controller, "commandMode");
            return commandMode == null || string.Equals(commandMode.ToString(), "Move", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsAlly(object unit)
        {
            object definition = GetValue(unit, "definition");
            object faction = GetValue(definition, "faction") ?? GetValue(unit, "faction");
            return faction == null || string.Equals(faction.ToString(), "Ally", StringComparison.OrdinalIgnoreCase);
        }

        private void Rebuild(object activeUnit)
        {
            Vector2Int start = ReadCell(activeUnit);
            string commandName = (GetValue(controller, "commandMode") ?? string.Empty).ToString();
            if (ReferenceEquals(activeUnit, lastActiveUnit) && start == lastActiveCell && commandName == lastCommandName && visibleRenderers.Count > 0)
            {
                // Keep the overlay stable unless the selected unit or command changed.
                return;
            }

            lastActiveCell = start;
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

            if (!showBlockedCells)
            {
                return;
            }

            int blockedRadius = Mathf.Max(1, moveBudget) + Mathf.Max(0, blockedPreviewPadding);
            cachedTiles.Clear();
            CollectTiles(cachedTiles);

            for (int i = 0; i < cachedTiles.Count; i++)
            {
                object tile = cachedTiles[i];
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

        private bool ShouldDrawBlocked(object tile, Vector2Int cell, Vector2Int start, int budget, int distance)
        {
            if (tile == null)
            {
                return false;
            }

            if (!ReadBool(tile, "walkable", true) || ReadBool(tile, "blocksMovement"))
            {
                return true;
            }

            object occupant = InvokeMethod(controller, "UnitAt", cell);
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
            renderer.sprite = diamondSprite;
            renderer.color = ColorFor(kind, cost);
            renderer.sortingLayerName = "Default";
            renderer.sortingOrder = sortingOrder + (kind == PreviewKind.Current ? 2 : kind == PreviewKind.Blocked ? 1 : 0);
            renderer.transform.SetParent(overlayRoot, false);
            renderer.transform.position = CellWorldPosition(cell) + worldOffset;
            renderer.transform.localRotation = Quaternion.identity;
            renderer.transform.localScale = new Vector3(tileWidth * tileScalePadding, tileHeight * tileScalePadding, 1f);
            renderer.gameObject.SetActive(true);
            visibleRenderers.Add(renderer);
        }

        private Color ColorFor(PreviewKind kind, int cost)
        {
            switch (kind)
            {
                case PreviewKind.Current:
                    return currentColor;
                case PreviewKind.Blocked:
                    return blockedColor;
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

        private SpriteRenderer GetRenderer()
        {
            if (rendererPool.Count > 0)
            {
                return rendererPool.Pop();
            }

            GameObject obj = new GameObject("PreviewCell");
            obj.hideFlags = HideFlags.DontSave;
            SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sharedMaterial = null;
            return renderer;
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
            InvokeMethod(controller, "ClearHighlights");
        }

        private void FillReachable(object activeUnit, Dictionary<Vector2Int, int> output)
        {
            object result = InvokeMethod(controller, "GetReachableCells", activeUnit);
            if (result == null)
            {
                return;
            }

            IDictionary dictionary = result as IDictionary;
            if (dictionary != null)
            {
                foreach (DictionaryEntry entry in dictionary)
                {
                    if (entry.Key is Vector2Int)
                    {
                        output[(Vector2Int)entry.Key] = SafeInt(entry.Value, 0);
                    }
                }

                return;
            }

            IEnumerable enumerable = result as IEnumerable;
            if (enumerable == null)
            {
                return;
            }

            foreach (object item in enumerable)
            {
                object key = GetValue(item, "Key");
                object value = GetValue(item, "Value");
                if (key is Vector2Int)
                {
                    output[(Vector2Int)key] = SafeInt(value, 0);
                }
            }
        }

        private int ResolveMoveBudget(object activeUnit, Dictionary<Vector2Int, int> reachable)
        {
            object effective = InvokeMethod(controller, "EffectiveMoveRange", activeUnit);
            int budget = SafeInt(effective, 0);

            if (budget <= 0)
            {
                object definition = GetValue(activeUnit, "definition");
                budget = SafeInt(GetValue(definition, "moveRange"), 0);
            }

            if (budget <= 0)
            {
                object actions = GetValue(activeUnit, "actions");
                budget = SafeInt(GetValue(actions, "movementLeft"), 0);
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

        private void CollectTiles(List<object> output)
        {
            object tiles = GetValue(controller, "tiles");
            IEnumerable enumerable = tiles as IEnumerable;
            if (enumerable != null)
            {
                foreach (object tile in enumerable)
                {
                    if (tile != null)
                    {
                        output.Add(tile);
                    }
                }

                return;
            }

            BattleMapSceneController sceneController = UnityEngine.Object.FindAnyObjectByType<BattleMapSceneController>();
            if (sceneController != null && sceneController.Cells != null)
            {
                foreach (object cell in sceneController.Cells)
                {
                    if (cell != null)
                    {
                        output.Add(cell);
                    }
                }
            }
        }

        private IEnumerable<object> EnumerateUnits()
        {
            object units = GetValue(controller, "units");
            IEnumerable enumerable = units as IEnumerable;
            if (enumerable == null)
            {
                yield break;
            }

            foreach (object unit in enumerable)
            {
                if (unit != null)
                {
                    yield return unit;
                }
            }
        }

        private Vector2Int ReadCell(object source)
        {
            object value = GetValue(source, "cell") ?? GetValue(source, "currentCell");
            if (value is Vector2Int)
            {
                return (Vector2Int)value;
            }

            return new Vector2Int(int.MinValue, int.MinValue);
        }

        private Vector3 UnitWorldPosition(object unit)
        {
            object view = GetValue(unit, "view");
            Component component = view as Component;
            if (component != null)
            {
                return component.transform.position;
            }

            Transform transformValue = view as Transform;
            if (transformValue != null)
            {
                return transformValue.position;
            }

            return CellWorldPosition(ReadCell(unit));
        }

        private Vector3 CellWorldPosition(Vector2Int cell)
        {
            object direct = InvokeMethod(controller, "UnitWorldPosition", cell);
            if (direct is Vector3)
            {
                return (Vector3)direct;
            }

            direct = InvokeMethod(controller, "TileWorldPosition", cell);
            if (direct is Vector3)
            {
                return (Vector3)direct;
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

        private static object GetValue(object target, string memberName)
        {
            if (target == null || string.IsNullOrEmpty(memberName))
            {
                return null;
            }

            Type type = target.GetType();
            while (type != null)
            {
                FieldInfo field = type.GetField(memberName, AnyInstance);
                if (field != null)
                {
                    return field.GetValue(target);
                }

                PropertyInfo property = type.GetProperty(memberName, AnyInstance);
                if (property != null && property.GetIndexParameters().Length == 0)
                {
                    return property.GetValue(target, null);
                }

                type = type.BaseType;
            }

            return null;
        }

        private static object InvokeMethod(object target, string methodName, params object[] args)
        {
            if (target == null || string.IsNullOrEmpty(methodName))
            {
                return null;
            }

            Type type = target.GetType();
            while (type != null)
            {
                MethodInfo method = FindMethod(type, methodName, args);
                if (method != null)
                {
                    return method.Invoke(target, args);
                }

                type = type.BaseType;
            }

            return null;
        }

        private static MethodInfo FindMethod(Type type, string methodName, object[] args)
        {
            MethodInfo[] methods = type.GetMethods(AnyInstance | AnyStatic);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method.Name != methodName)
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length != args.Length)
                {
                    continue;
                }

                bool compatible = true;
                for (int p = 0; p < parameters.Length; p++)
                {
                    if (args[p] == null)
                    {
                        continue;
                    }

                    Type parameterType = parameters[p].ParameterType;
                    if (!parameterType.IsInstanceOfType(args[p]))
                    {
                        // Reflection does not consider Vector2Int boxed as custom structs through IsInstanceOfType in every Unity profile.
                        if (!(parameterType == Vector2IntType && args[p] is Vector2Int))
                        {
                            compatible = false;
                            break;
                        }
                    }
                }

                if (compatible)
                {
                    return method;
                }
            }

            return null;
        }

        private static bool ReadBool(object target, string memberName, bool defaultValue = false)
        {
            object value = GetValue(target, memberName);
            if (value is bool)
            {
                return (bool)value;
            }

            return defaultValue;
        }

        private static int SafeInt(object value, int defaultValue)
        {
            if (value == null)
            {
                return defaultValue;
            }

            try
            {
                return Convert.ToInt32(value);
            }
            catch
            {
                return defaultValue;
            }
        }

        private static int Manhattan(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }
    }
}
