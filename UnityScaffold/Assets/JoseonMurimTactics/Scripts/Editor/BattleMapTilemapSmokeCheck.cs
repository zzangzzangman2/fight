using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace JoseonMurimTactics.Editor
{
public static class BattleMapTilemapSmokeCheck
{
    private const string BattleTestScenePath = "Assets/JoseonMurimTactics/Scenes/BattleTest.unity";

    [MenuItem("Joseon Murim Tactics/Battle Maps/Smoke Check Tilemap Battlefield")]
    public static void Run()
    {
        EditorSceneManager.OpenScene(BattleTestScenePath);
        BattleTestController controller = UnityEngine.Object.FindAnyObjectByType<BattleTestController>();
        if (controller == null)
        {
            throw new MissingReferenceException("BattleTestController not found in BattleTest scene.");
        }

        controller.useAuthoredSceneMap = false;
        controller.useTilemapBattlefield = true;
        controller.useLegacyDiamondTerrain = false;
        controller.mapVariant = BattleTestMapVariant.BaekduMountainSnowfield;
        InvokePrivate(controller, "EnsureMapVisualSprites");
        InvokePrivate(controller, "CreateTerrain");

        GameObject battlefield = GameObject.Find("Battlefield_Tilemap");
        Require(battlefield != null, "Battlefield_Tilemap was not created.");
        Tilemap groundBase = RequireTilemap("Tilemap_Ground_Base");
        Tilemap groundVariation = RequireTilemap("Tilemap_Ground_Variation");
        Tilemap roadPath = RequireTilemap("Tilemap_Road_Path");
        Tilemap roadEdge = RequireTilemap("Tilemap_Road_Edge");
        Tilemap cliffTop = RequireTilemap("Tilemap_Cliff_Top");
        Tilemap cliffFace = RequireTilemap("Tilemap_Cliff_Face");
        RequireTilemap("Tilemap_Cliff_Edge");
        Tilemap waterBase = RequireTilemap("Tilemap_Water_Base");
        Tilemap waterSurface = RequireTilemap("Tilemap_Water_Surface");
        RequireTilemap("Tilemap_Decor_Ground");
        RequireTilemap("Tilemap_Decor_GrassRockSnow");
        RequireTilemap("Tilemap_Props_BehindUnits");
        RequireTilemap("Tilemap_Props_FrontOfUnits");
        RequireTilemap("Tilemap_Grid_Subtle");
        RequireTilemap("Tilemap_Highlight_Move");
        RequireTilemap("Tilemap_Highlight_Attack");
        RequireTilemap("Tilemap_Highlight_Danger");
        RequireTilemap("Tilemap_Highlight_PathArrow");
        Require(GameObject.Find("PropsRoot") != null, "PropsRoot was not created.");
        Require(GameObject.Find("LightsRoot") != null, "LightsRoot was not created.");
        SpriteRenderer paintedBackdrop = GameObject.Find("Painted Map Backdrop")?.GetComponent<SpriteRenderer>();
        Require(paintedBackdrop != null && paintedBackdrop.sprite != null,
                "Painted battle map backdrop sprite was not loaded.");
        Require(paintedBackdrop.sprite.name.Contains("baekdu_mountain_snowfield"),
                "Expected the Baekdu mountain snowfield painted backdrop.");

        BattleMapTilemapBinder binder = battlefield.GetComponent<BattleMapTilemapBinder>();
        Require(binder != null, "BattleMapTilemapBinder was not created.");

        TacticalGridOverlay overlay = battlefield.GetComponentInChildren<TacticalGridOverlay>();
        Require(overlay != null, "TacticalGridOverlay was not created.");
        Require(overlay.Cells.Count == controller.width * controller.height,
                $"TacticalGridOverlay expected {controller.width * controller.height} cells, got {overlay.Cells.Count}.");

        Require(groundBase.GetUsedTilesCount() + groundVariation.GetUsedTilesCount() +
                roadPath.GetUsedTilesCount() + roadEdge.GetUsedTilesCount() +
                cliffTop.GetUsedTilesCount() + cliffFace.GetUsedTilesCount() +
                waterBase.GetUsedTilesCount() + waterSurface.GetUsedTilesCount() > 0,
                "No terrain tiles were assigned.");
        Require(UnityEngine.Object.FindObjectsByType<MapPropView>(FindObjectsSortMode.None).Length >= 6,
                "Expected generated interactable props to carry MapPropView metadata.");
        VerifyBlockedMapCells(controller);
        VerifyElevationMovementRules(controller);
        VerifyPlayerMoveFlow(controller);

        CleanupGeneratedChildren(controller.transform);
        Debug.Log("[BattleMapTilemapSmokeCheck] BattleTest Tilemap battlefield smoke check passed.");
    }

    private static Tilemap RequireTilemap(string name)
    {
        Tilemap tilemap = GameObject.Find(name)?.GetComponent<Tilemap>();
        Require(tilemap != null, $"{name} was not created.");
        return tilemap;
    }

    private static void VerifyBlockedMapCells(BattleTestController controller)
    {
        BattleTestTile[,] tiles = GetPrivate<BattleTestTile[,]>(controller, "tiles");
        RequireBlocked(tiles, new Vector2Int(0, 6), "left snow pine wall");
        RequireBlocked(tiles, new Vector2Int(2, 9), "snow pine blocker");
        RequireBlocked(tiles, new Vector2Int(11, 8), "snow basalt boulder blocker");
        RequireBlocked(tiles, new Vector2Int(5, 3), "deep frozen channel");
        RequireBlocked(tiles, new Vector2Int(4, 10), "northern basalt cliff");
    }

    private static void RequireBlocked(BattleTestTile[,] tiles, Vector2Int cell, string label)
    {
        BattleTestTile tile = tiles[cell.x, cell.y];
        Require(tile != null, $"Expected blocker tile for {label} at {cell}.");
        Require(!tile.walkable, $"{label} at {cell} should block movement.");
        Require(tile.moveCost >= 99, $"{label} at {cell} should have impassable move cost.");
    }

    private static void VerifyElevationMovementRules(BattleTestController controller)
    {
        BattleTestTile[,] tiles = GetPrivate<BattleTestTile[,]>(controller, "tiles");
        BattleTestTile snowPass = RequireTile(tiles, new Vector2Int(9, 7), "snow pass approach");
        BattleTestTile centralRidge = RequireTile(tiles, new Vector2Int(9, 8), "central high ridge");
        BattleTestTile hotSpringRamp = RequireTile(tiles, new Vector2Int(12, 7), "hot spring snow ramp");
        BattleTestTile hotSpring = RequireTile(tiles, new Vector2Int(12, 8), "hot spring high ground");
        BattleTestTile openSnow = RequireTile(tiles, new Vector2Int(15, 8), "open snow beside high ground");
        BattleTestTile steepHighGround = RequireTile(tiles, new Vector2Int(14, 8), "steep hot spring flank");
        BattleTestTile safeCrossing = RequireTile(tiles, new Vector2Int(7, 3), "safe frozen crossing");
        BattleTestTile thinIce = RequireTile(tiles, new Vector2Int(4, 3), "thin ice edge");

        Require(snowPass.walkable && snowPass.elevation == 1, "Snow pass should be walkable elevation 1.");
        Require(centralRidge.walkable && centralRidge.elevation == 2, "Central ridge should be walkable elevation 2.");
        Require(hotSpringRamp.walkable && hotSpringRamp.elevation == 2,
                "Hot spring ramp should be walkable elevation 2.");
        Require(hotSpring.walkable && hotSpring.elevation == 3, "Hot spring should be walkable elevation 3.");
        Require(safeCrossing.walkable && safeCrossing.moveCost == 1,
                "Narrow frozen crossing should be the fast safe ice route.");
        Require(thinIce.walkable && thinIce.moveCost == 3, "Thin ice edge should be passable but slow.");

        Require(StepMoveCost(controller, snowPass, centralRidge) == centralRidge.moveCost + 1,
                "Climbing one elevation level should add movement cost.");
        Require(StepMoveCost(controller, hotSpringRamp, hotSpring) == hotSpring.moveCost + 1,
                "Hot spring ramp should allow a costly one-level climb.");
        Require(StepMoveCost(controller, openSnow, steepHighGround) == int.MaxValue,
                "Direct three-level hot spring flank climb should be blocked.");
        Require(StepMoveCost(controller, tiles[3, 9], tiles[4, 9]) == int.MaxValue,
                "Basalt cliff edge should block movement even when adjacent.");
        Require(StepMoveCost(controller, safeCrossing, tiles[6, 3]) == int.MaxValue,
                "Safe crossing should not leak into the deep frozen channel.");
    }

    private static BattleTestTile RequireTile(BattleTestTile[,] tiles, Vector2Int cell, string label)
    {
        BattleTestTile tile = tiles[cell.x, cell.y];
        Require(tile != null, $"Expected {label} tile at {cell}.");
        return tile;
    }

    private static int StepMoveCost(BattleTestController controller, BattleTestTile from, BattleTestTile to)
    {
        return (int)InvokePrivate(controller, "StepMoveCost", from, to);
    }

    private static void VerifyPlayerMoveFlow(BattleTestController controller)
    {
        InvokePrivate(controller, "SpawnUnits");
        InvokePrivate(controller, "BeginPlayerPhase");

        List<BattleTestUnit> units = GetPrivate<List<BattleTestUnit>>(controller, "units");
        BattleTestUnit first = GetPrivate<BattleTestUnit>(controller, "activeUnit");
        Require(first != null && first.definition.faction == Faction.Ally, "Expected an active ally after player phase begins.");
        Require(first.CanMove, "First active ally should be able to move before moving.");
        Require(GetPrivate<BattleCommandMode>(controller, "commandMode") == BattleCommandMode.Move,
                "First active ally should start in move command mode.");
        VerifyTacticalCameraFocus(controller, first);

        Dictionary<Vector2Int, int> reachable =
            (Dictionary<Vector2Int, int>)InvokePrivate(controller, "GetReachableCells", first);
        BattleTestTile[,] tiles = GetPrivate<BattleTestTile[,]>(controller, "tiles");
        BattleTestTile destination = FindReachableDestination(first, reachable, tiles, units);
        Require(destination != null, "Expected at least one reachable movement tile.");

        InvokePrivate(controller, "TryMove", first, destination);
        Require(first.moved, "Moved ally should be marked as moved.");
        Require(!first.CanMove, "Moved ally should not be able to move again.");
        Require(GetPrivate<BattleCommandMode>(controller, "commandMode") != BattleCommandMode.Move,
                "Moved ally should leave move command mode.");
        VerifyTacticalCameraFocus(controller, first);

        InvokePrivate(controller, "SetCommandMode", BattleCommandMode.Move);
        Require(GetPrivate<BattleCommandMode>(controller, "commandMode") != BattleCommandMode.Move,
                "Move command should stay blocked after moving once.");

        InvokePrivate(controller, "EndTurn");
        Require(first.acted, "Wait should spend the current ally main action.");

        BattleTestUnit next = GetPrivate<BattleTestUnit>(controller, "activeUnit");
        int allyCount = 0;
        foreach (BattleTestUnit unit in units)
        {
            if (unit != null && !unit.defeated && unit.definition.faction == Faction.Ally)
            {
                allyCount++;
            }
        }

        if (allyCount > 1)
        {
            Require(next != null && next != first, "Wait should advance to the next ready ally.");
            Require(next.CanMove, "Next ready ally should be able to move.");
            Require(GetPrivate<BattleCommandMode>(controller, "commandMode") == BattleCommandMode.Move,
                    "Next ready ally should start in move command mode.");
        }

        VerifyBlockedCellsStayUnreachable(controller, units);
    }

    private static void VerifyBlockedCellsStayUnreachable(BattleTestController controller, List<BattleTestUnit> units)
    {
        BattleTestUnit hanBiyeon = FindUnit(units, "han_biyeon");
        Require(hanBiyeon != null, "Expected Han Biyeon test unit.");
        Dictionary<Vector2Int, int> hanReachable =
            (Dictionary<Vector2Int, int>)InvokePrivate(controller, "GetReachableCells", hanBiyeon);
        Require(!hanReachable.ContainsKey(new Vector2Int(2, 9)),
                "Snow pine blocker should not appear in Han Biyeon's reachable tiles.");
        Require(!hanReachable.ContainsKey(new Vector2Int(0, 6)),
                "Left tree wall should not appear in Han Biyeon's reachable tiles.");

        BattleTestUnit jinSeoyul = FindUnit(units, "jin_seoyul");
        Require(jinSeoyul != null, "Expected Jin Seoyul test unit.");
        Dictionary<Vector2Int, int> jinReachable =
            (Dictionary<Vector2Int, int>)InvokePrivate(controller, "GetReachableCells", jinSeoyul);
        Require(!jinReachable.ContainsKey(new Vector2Int(11, 8)),
                "Snow boulder blocker should not appear in Jin Seoyul's reachable tiles.");
        Require(!jinReachable.ContainsKey(new Vector2Int(5, 3)),
                "Deep frozen channel should not appear in reachable tiles.");
        Require(!jinReachable.ContainsKey(new Vector2Int(4, 10)),
                "Northern basalt cliff should not appear in reachable tiles.");
    }

    private static void VerifyTacticalCameraFocus(BattleTestController controller, BattleTestUnit focusUnit)
    {
        Camera camera = Camera.main;
        Require(camera != null, "Expected main camera for tactical camera follow check.");

        float fullSize = (float)InvokePrivate(controller, "CalculateFullMapCameraSize", camera);
        float tacticalSize = (float)InvokePrivate(controller, "CalculateTacticalCameraSize", camera);
        Require(camera.orthographic, "Battle camera should remain orthographic.");
        Require(camera.orthographicSize < fullSize - 0.25f,
                $"Camera should zoom in after the full-map intro. Size {camera.orthographicSize}, full {fullSize}.");
        Require(camera.orthographicSize <= tacticalSize + 0.05f,
                $"Camera should stay at tactical zoom. Size {camera.orthographicSize}, tactical {tacticalSize}.");

        Vector3 unitWorld = (Vector3)InvokePrivate(controller, "UnitWorldPosition", focusUnit.cell);
        Vector2 cameraPoint = new Vector2(camera.transform.position.x, camera.transform.position.y);
        Vector2 unitPoint = new Vector2(unitWorld.x, unitWorld.y);
        Require(Vector2.Distance(cameraPoint, unitPoint) <= tacticalSize + 1.5f,
                "Camera should keep the focused unit inside the tactical view.");
    }

    private static BattleTestUnit FindUnit(List<BattleTestUnit> units, string id)
    {
        foreach (BattleTestUnit unit in units)
        {
            if (unit != null && unit.definition != null && unit.definition.id == id)
            {
                return unit;
            }
        }

        return null;
    }

    private static BattleTestTile FindReachableDestination(BattleTestUnit unit, Dictionary<Vector2Int, int> reachable,
                                                           BattleTestTile[,] tiles, List<BattleTestUnit> units)
    {
        foreach (KeyValuePair<Vector2Int, int> pair in reachable)
        {
            if (pair.Key == unit.cell)
            {
                continue;
            }

            BattleTestTile tile = tiles[pair.Key.x, pair.Key.y];
            if (tile != null && tile.walkable && !IsOccupied(pair.Key, units))
            {
                return tile;
            }
        }

        return null;
    }

    private static bool IsOccupied(Vector2Int cell, List<BattleTestUnit> units)
    {
        foreach (BattleTestUnit unit in units)
        {
            if (unit != null && !unit.defeated && unit.cell == cell)
            {
                return true;
            }
        }

        return false;
    }

    private static object InvokePrivate(BattleTestController controller, string methodName, params object[] args)
    {
        MethodInfo method = typeof(BattleTestController).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (method == null)
        {
            throw new MissingMethodException(nameof(BattleTestController), methodName);
        }

        return method.Invoke(controller, args);
    }

    private static T GetPrivate<T>(BattleTestController controller, string fieldName)
    {
        FieldInfo field = typeof(BattleTestController).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
        {
            throw new MissingFieldException(nameof(BattleTestController), fieldName);
        }

        return (T)field.GetValue(controller);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new UnityException(message);
        }
    }

    private static void CleanupGeneratedChildren(Transform root)
    {
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            UnityEngine.Object.DestroyImmediate(root.GetChild(i).gameObject);
        }
    }
}
}
