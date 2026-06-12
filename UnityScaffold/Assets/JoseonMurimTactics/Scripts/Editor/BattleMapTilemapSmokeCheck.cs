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

        int visibleTerrainTiles = groundBase.GetUsedTilesCount() + groundVariation.GetUsedTilesCount() +
                                  roadPath.GetUsedTilesCount() + roadEdge.GetUsedTilesCount() +
                                  cliffTop.GetUsedTilesCount() + cliffFace.GetUsedTilesCount() +
                                  waterBase.GetUsedTilesCount() + waterSurface.GetUsedTilesCount();
        Require(visibleTerrainTiles == 0,
                "Painted battle map should keep terrain tilemaps empty so runtime tints do not band the backdrop.");
        Require(UnityEngine.Object.FindObjectsByType<MapPropView>(FindObjectsSortMode.None).Length >= 6,
                "Expected generated interactable props to carry MapPropView metadata.");
        VerifyBlockedMapCells(controller);
        VerifyElevationMovementRules(controller);
        VerifyPlayerMoveFlow(controller);

        CleanupGeneratedChildren(controller.transform);
        VerifyBanditLairFreeTimeMap(controller);
        VerifyWildlifeFreeTimeMap(controller, HubController.WolfPassBattleId, BattleTestMapVariant.WolfPass,
                                  "wolf_alpha");
        VerifyWildlifeFreeTimeMap(controller, HubController.TigerRavineBattleId, BattleTestMapVariant.TigerRavine,
                                  "tiger_boss_sangun");
        VerifyWildlifeFreeTimeMap(controller, HubController.LeopardCliffBattleId, BattleTestMapVariant.LeopardCliff,
                                  "leopard_boss_shadow");
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
        VerifyUnitAnchorsAndPropOverlap(controller, units);
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
        Require(first.actions.movementLeft == 0, "Committed movement should spend all remaining movement.");
        Require(!first.CanMove, "Moved ally should not be able to move again.");
        Require(GetPrivate<BattleCommandMode>(controller, "commandMode") != BattleCommandMode.Move,
                "Moved ally should leave move command mode.");
        BattleHudSnapshot postMoveHud = (BattleHudSnapshot)InvokePrivate(controller, "CreateHudSnapshot");
        Require(!postMoveHud.canMove, "HUD should disable Move after a unit has moved.");
        Require(postMoveHud.canAttack, "HUD should keep Attack available after moving.");
        Require(!postMoveHud.canSkill, "HUD should disable Skill after moving.");
        Require(!postMoveHud.canGuard, "HUD should disable Guard after moving.");
        Require(!postMoveHud.canTerrain, "HUD should disable terrain interaction after moving.");
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

    private static void VerifyBanditLairFreeTimeMap(BattleTestController controller)
    {
        BattleEntryAdapter.SetPendingBattle(HubController.BanditLairBattleId);
        SetPrivate(controller, "mapAssetSpritesLoaded", false);
        InvokePrivate(controller, "ApplyBattleEntryConfiguration");
        BattleEntryAdapter.Clear();
        Require(controller.mapVariant == BattleTestMapVariant.BanditLair,
                "Bandit lair battle id should select the BanditLair map variant.");

        GetPrivate<List<BattleTestUnit>>(controller, "units").Clear();
        GetPrivate<List<BattleTestInteractable>>(controller, "interactables").Clear();
        InvokePrivate(controller, "EnsureMapVisualSprites");
        InvokePrivate(controller, "CreateTerrain");
        Require(GameObject.Find("Battlefield_Tilemap") != null, "Bandit lair tilemap battlefield was not created.");
        RequireTilemap("Tilemap_Ground_Base");
        RequireTilemap("Tilemap_Road_Path");
        RequireTilemap("Tilemap_Cliff_Top");
        RequireTilemap("Tilemap_Water_Base");
        RequireTilemap("Tilemap_Highlight_Move");
        Require(GameObject.Find("PropsRoot") != null, "Bandit lair props root was not created.");
        Require(UnityEngine.Object.FindObjectsByType<MapPropView>(FindObjectsSortMode.None).Length >= 6,
                "Bandit lair should generate interactable prop metadata.");

        VerifyBanditLairTerrainRules(controller);
        InvokePrivate(controller, "SpawnUnits");
        List<BattleTestUnit> units = GetPrivate<List<BattleTestUnit>>(controller, "units");
        VerifyUnitAnchorsAndPropOverlap(controller, units);
        Require(FindUnit(units, "bandit_boss_gwakchil") != null, "Expected bandit boss unit for free-time lair.");
        Require(FindUnit(units, "iron_wolf_captain") == null, "Bandit lair should not spawn the main-story captain.");
        VerifyBanditLairBlockedCellsStayUnreachable(controller, units);

        CleanupGeneratedChildren(controller.transform);
    }

    private static void VerifyBanditLairTerrainRules(BattleTestController controller)
    {
        BattleTestTile[,] tiles = GetPrivate<BattleTestTile[,]>(controller, "tiles");
        RequireBlocked(tiles, new Vector2Int(0, 6), "bandit outer forest wall");
        RequireBlocked(tiles, new Vector2Int(5, 8), "bandit palisade wall");
        RequireBlocked(tiles, new Vector2Int(12, 10), "bandit cave wall");
        RequireBlocked(tiles, new Vector2Int(4, 4), "bandit drainage ditch");

        BattleTestTile ropeBridge = RequireTile(tiles, new Vector2Int(7, 4), "bandit rope bridge");
        BattleTestTile muddyBank = RequireTile(tiles, new Vector2Int(3, 4), "bandit muddy ditch bank");
        BattleTestTile snareTrap = RequireTile(tiles, new Vector2Int(5, 6), "bandit snare trap");
        BattleTestTile slope = RequireTile(tiles, new Vector2Int(12, 7), "bandit watchtower slope");
        BattleTestTile tower = RequireTile(tiles, new Vector2Int(13, 8), "bandit watchtower");
        BattleTestTile supplyCache = RequireTile(tiles, new Vector2Int(8, 10), "bandit stolen supply cache");

        Require(ropeBridge.walkable && ropeBridge.moveCost == 1, "Bandit rope bridge should be the fast crossing.");
        Require(muddyBank.walkable && muddyBank.moveCost == 3, "Bandit muddy bank should be slow but passable.");
        Require(snareTrap.walkable && snareTrap.danger && snareTrap.moveCost == 3,
                "Bandit trap should be passable, dangerous, and costly.");
        Require(slope.walkable && slope.elevation == 1, "Bandit watchtower slope should be walkable elevation 1.");
        Require(tower.walkable && tower.elevation == 2 && tower.coverBonus >= 2,
                "Bandit watchtower should be covered elevation 2.");
        Require(supplyCache.walkable && supplyCache.objective && supplyCache.elevation == 2,
                "Bandit supply cache should be a walkable objective on high ground.");
        Require(StepMoveCost(controller, slope, tower) == tower.moveCost + 1,
                "Bandit watchtower climb should allow a costly one-level climb.");
        Require(StepMoveCost(controller, ropeBridge, tiles[6, 4]) == int.MaxValue,
                "Bandit rope bridge should not leak into the deep ditch.");
    }

    private static void VerifyBanditLairBlockedCellsStayUnreachable(BattleTestController controller,
                                                                    List<BattleTestUnit> units)
    {
        BattleTestUnit hanBiyeon = FindUnit(units, "han_biyeon");
        Require(hanBiyeon != null, "Expected Han Biyeon in bandit lair test roster.");
        Dictionary<Vector2Int, int> reachable =
            (Dictionary<Vector2Int, int>)InvokePrivate(controller, "GetReachableCells", hanBiyeon);
        Require(!reachable.ContainsKey(new Vector2Int(5, 8)),
                "Bandit palisade wall should not appear in reachable tiles.");
        Require(!reachable.ContainsKey(new Vector2Int(4, 4)),
                "Bandit deep ditch should not appear in reachable tiles.");
    }

    private static void VerifyWildlifeFreeTimeMap(BattleTestController controller, string battleId,
                                                  BattleTestMapVariant expectedVariant, string bossId)
    {
        BattleEntryAdapter.SetPendingBattle(battleId);
        SetPrivate(controller, "mapAssetSpritesLoaded", false);
        InvokePrivate(controller, "ApplyBattleEntryConfiguration");
        BattleEntryAdapter.Clear();
        Require(controller.mapVariant == expectedVariant,
                $"{battleId} should select the {expectedVariant} map variant.");

        GetPrivate<List<BattleTestUnit>>(controller, "units").Clear();
        GetPrivate<List<BattleTestInteractable>>(controller, "interactables").Clear();
        InvokePrivate(controller, "EnsureMapVisualSprites");
        InvokePrivate(controller, "CreateTerrain");
        Require(GameObject.Find("Battlefield_Tilemap") != null, $"{expectedVariant} tilemap battlefield was not created.");
        RequireTilemap("Tilemap_Ground_Base");
        RequireTilemap("Tilemap_Road_Path");
        RequireTilemap("Tilemap_Cliff_Top");
        RequireTilemap("Tilemap_Water_Base");
        RequireTilemap("Tilemap_Highlight_Move");
        Require(GameObject.Find("PropsRoot") != null, $"{expectedVariant} props root was not created.");
        Require(UnityEngine.Object.FindObjectsByType<MapPropView>(FindObjectsSortMode.None).Length >= 6,
                $"{expectedVariant} should generate interactable prop metadata.");

        switch (expectedVariant)
        {
        case BattleTestMapVariant.WolfPass:
            VerifyWolfPassTerrainRules(controller);
            break;
        case BattleTestMapVariant.TigerRavine:
            VerifyTigerRavineTerrainRules(controller);
            break;
        case BattleTestMapVariant.LeopardCliff:
            VerifyLeopardCliffTerrainRules(controller);
            break;
        }

        InvokePrivate(controller, "SpawnUnits");
        List<BattleTestUnit> units = GetPrivate<List<BattleTestUnit>>(controller, "units");
        VerifyUnitAnchorsAndPropOverlap(controller, units);
        Require(FindUnit(units, bossId) != null, $"Expected {bossId} boss unit for {expectedVariant}.");
        Require(FindUnit(units, "iron_wolf_captain") == null,
                $"{expectedVariant} should not spawn the main-story captain.");
        VerifyWildlifeBlockedCellsStayUnreachable(controller, units, expectedVariant);

        CleanupGeneratedChildren(controller.transform);
    }

    private static void VerifyWolfPassTerrainRules(BattleTestController controller)
    {
        BattleTestTile[,] tiles = GetPrivate<BattleTestTile[,]>(controller, "tiles");
        RequireBlocked(tiles, new Vector2Int(0, 6), "wolf pass outer forest wall");
        RequireBlocked(tiles, new Vector2Int(2, 9), "wolf pass birch blocker");
        RequireBlocked(tiles, new Vector2Int(4, 6), "wolf pass fallen log blocker");
        RequireBlocked(tiles, new Vector2Int(13, 10), "wolf pass den rock wall");

        BattleTestTile bridge = RequireTile(tiles, new Vector2Int(7, 4), "wolf pass creek bridge");
        BattleTestTile shallow = RequireTile(tiles, new Vector2Int(5, 4), "wolf pass shallow creek");
        BattleTestTile slope = RequireTile(tiles, new Vector2Int(11, 6), "wolf pass ridge slope");
        BattleTestTile ridge = RequireTile(tiles, new Vector2Int(11, 7), "wolf pass eastern ridge");
        BattleTestTile den = RequireTile(tiles, new Vector2Int(12, 10), "wolf pass den objective");

        Require(bridge.walkable && bridge.moveCost == 1, "Wolf pass bridge should be the fast creek crossing.");
        Require(shallow.walkable && shallow.moveCost == 3 && shallow.danger,
                "Wolf pass shallow creek should be slow, passable, and dangerous.");
        Require(ridge.walkable && ridge.elevation == 2, "Wolf pass ridge should be walkable elevation 2.");
        Require(den.walkable && den.objective && den.elevation == 2,
                "Wolf pass den should be a walkable objective on high ground.");
        Require(StepMoveCost(controller, slope, ridge) == ridge.moveCost + 1,
                "Wolf pass ridge climb should add one elevation cost.");
        Require(StepMoveCost(controller, bridge, tiles[6, 4]) == int.MaxValue,
                "Wolf pass bridge should not leak into the deep creek.");
    }

    private static void VerifyTigerRavineTerrainRules(BattleTestController controller)
    {
        BattleTestTile[,] tiles = GetPrivate<BattleTestTile[,]>(controller, "tiles");
        RequireBlocked(tiles, new Vector2Int(0, 6), "tiger ravine outer cliff wall");
        RequireBlocked(tiles, new Vector2Int(6, 8), "tiger ravine central cliff wall");
        RequireBlocked(tiles, new Vector2Int(9, 4), "tiger ravine boulder blocker");
        RequireBlocked(tiles, new Vector2Int(10, 5), "tiger ravine collapsed boulder");

        BattleTestTile reed = RequireTile(tiles, new Vector2Int(4, 6), "tiger ravine reed cover");
        BattleTestTile slope = RequireTile(tiles, new Vector2Int(12, 8), "tiger ravine rock shelf slope");
        BattleTestTile high = RequireTile(tiles, new Vector2Int(13, 8), "tiger ravine H3 shelf");
        BattleTestTile villagers = RequireTile(tiles, new Vector2Int(14, 9), "tiger ravine villagers objective");

        Require(reed.walkable && reed.moveCost == 2 && reed.coverBonus >= 2 && reed.blocksLineOfSight,
                "Tiger ravine reeds should be slow cover that blocks sight.");
        Require(slope.walkable && slope.elevation == 2, "Tiger ravine shelf slope should be elevation 2.");
        Require(high.walkable && high.elevation == 3, "Tiger ravine high shelf should be elevation 3.");
        Require(villagers.walkable && villagers.objective && villagers.elevation == 3,
                "Tiger ravine villagers objective should sit on H3 high ground.");
        Require(StepMoveCost(controller, slope, high) == high.moveCost + 1,
                "Tiger ravine H3 climb should add one elevation cost.");
        Require(StepMoveCost(controller, tiles[5, 8], tiles[6, 8]) == int.MaxValue,
                "Tiger ravine central cliff wall should block adjacent movement.");
    }

    private static void VerifyLeopardCliffTerrainRules(BattleTestController controller)
    {
        BattleTestTile[,] tiles = GetPrivate<BattleTestTile[,]>(controller, "tiles");
        RequireBlocked(tiles, new Vector2Int(7, 5), "leopard cliff gap");
        RequireBlocked(tiles, new Vector2Int(2, 8), "leopard cliff bamboo blocker");
        RequireBlocked(tiles, new Vector2Int(12, 5), "leopard cliff rock drop");
        RequireBlocked(tiles, new Vector2Int(14, 10), "leopard cliff upper rock blocker");

        BattleTestTile bridge = RequireTile(tiles, new Vector2Int(8, 5), "leopard cliff rope bridge");
        BattleTestTile bamboo = RequireTile(tiles, new Vector2Int(4, 7), "leopard cliff bamboo path");
        BattleTestTile slope = RequireTile(tiles, new Vector2Int(13, 7), "leopard cliff shelf slope");
        BattleTestTile high = RequireTile(tiles, new Vector2Int(13, 8), "leopard cliff H3 shelf");
        BattleTestTile herbs = RequireTile(tiles, new Vector2Int(14, 8), "leopard cliff herb objective");

        Require(bridge.walkable && bridge.moveCost == 1, "Leopard cliff rope bridge should be the fast crossing.");
        Require(bamboo.walkable && bamboo.moveCost == 2 && bamboo.coverBonus >= 2 && bamboo.blocksLineOfSight,
                "Leopard cliff bamboo path should be slow sight-blocking cover.");
        Require(high.walkable && high.elevation == 3, "Leopard cliff herb shelf should be elevation 3.");
        Require(herbs.walkable && herbs.objective && herbs.elevation == 3,
                "Leopard cliff herb objective should sit on H3 high ground.");
        Require(StepMoveCost(controller, slope, high) == high.moveCost + 1,
                "Leopard cliff H3 climb should add one elevation cost.");
        Require(StepMoveCost(controller, bridge, tiles[7, 5]) == int.MaxValue,
                "Leopard cliff rope bridge should not leak into the chasm.");
    }

    private static void VerifyWildlifeBlockedCellsStayUnreachable(BattleTestController controller,
                                                                  List<BattleTestUnit> units,
                                                                  BattleTestMapVariant variant)
    {
        BattleTestUnit hanBiyeon = FindUnit(units, "han_biyeon");
        Require(hanBiyeon != null, $"Expected Han Biyeon in {variant} test roster.");
        Dictionary<Vector2Int, int> reachable =
            (Dictionary<Vector2Int, int>)InvokePrivate(controller, "GetReachableCells", hanBiyeon);

        Vector2Int blocked = variant == BattleTestMapVariant.WolfPass ? new Vector2Int(9, 4) :
                             variant == BattleTestMapVariant.TigerRavine ? new Vector2Int(10, 5) :
                             new Vector2Int(6, 5);
        Require(!reachable.ContainsKey(blocked), $"{variant} blocked cell {blocked} should not be reachable.");
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

    private static void VerifyUnitAnchorsAndPropOverlap(BattleTestController controller, List<BattleTestUnit> units)
    {
        List<BattleTestInteractable> interactables =
            GetPrivate<List<BattleTestInteractable>>(controller, "interactables");
        HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();
        foreach (BattleTestUnit unit in units)
        {
            if (unit == null || unit.definition == null)
            {
                continue;
            }

            BattleTestTile tile = GetPrivate<BattleTestTile[,]>(controller, "tiles")[unit.cell.x, unit.cell.y];
            Require(tile != null && tile.walkable,
                    $"{unit.definition.displayName} should spawn on a walkable tactical cell, got {unit.cell}.");
            Require(!occupied.Contains(unit.cell),
                    $"{unit.definition.displayName} should not share initial cell {unit.cell} with another unit.");
            occupied.Add(unit.cell);

            Vector3 unitWorld = (Vector3)InvokePrivate(controller, "UnitWorldPosition", unit.cell);
            Vector3 gridWorld = (Vector3)InvokePrivate(controller, "GridToWorld", unit.cell);
            Require(Vector3.Distance(unitWorld, gridWorld) <= 0.001f,
                    $"{unit.definition.displayName} should be grounded at the tactical cell center.");
            Require(!(bool)InvokePrivate(controller, "IsCellUnsafeForInitialSpawn", unit.cell),
                    $"{unit.definition.displayName} should not spawn on or beside a large map prop at {unit.cell}.");

            foreach (BattleTestInteractable interactable in interactables)
            {
                Require(interactable == null || interactable.cell != unit.cell,
                        $"{unit.definition.displayName} should not spawn on interactable {interactable?.id}.");
            }

            VerifyBattleVisualScale(unit);
        }
    }

    private static void VerifyBattleVisualScale(BattleTestUnit unit)
    {
        CharacterVisualData visual = unit.definition.visual;
        Require(visual != null, $"{unit.definition.displayName} should have CharacterVisualData.");
        Sprite sprite = visual == null ? null : visual.idlePoseSprite != null ? visual.idlePoseSprite : visual.fullBodySprite;
        Require(sprite != null, $"{unit.definition.displayName} should have a battle sprite.");
        Require(visual.heightInTiles >= 0.9f && visual.heightInTiles <= 1.35f,
                $"{unit.definition.displayName} board height should stay close to one tile.");

        if (unit.definition.faction != Faction.Enemy)
        {
            return;
        }

        float runtimeScale = visual.heightInTiles / Mathf.Max(0.01f, SpriteMeshHeight(sprite));
        Require(runtimeScale >= 1.2f,
                $"{unit.definition.displayName} enemy sprite appears to include oversized transparent padding; runtime scale {runtimeScale:0.00}.");
    }

    private static float SpriteMeshHeight(Sprite sprite)
    {
        if (sprite != null && sprite.vertices != null && sprite.vertices.Length > 0)
        {
            float minY = sprite.vertices[0].y;
            float maxY = minY;
            for (int i = 1; i < sprite.vertices.Length; i++)
            {
                float y = sprite.vertices[i].y;
                if (y < minY)
                {
                    minY = y;
                }
                else if (y > maxY)
                {
                    maxY = y;
                }
            }

            if (maxY > minY + 0.0001f)
            {
                return maxY - minY;
            }
        }

        return sprite == null ? 0f : sprite.bounds.size.y;
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

    private static void SetPrivate(BattleTestController controller, string fieldName, object value)
    {
        FieldInfo field = typeof(BattleTestController).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
        {
            throw new MissingFieldException(nameof(BattleTestController), fieldName);
        }

        field.SetValue(controller, value);
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
