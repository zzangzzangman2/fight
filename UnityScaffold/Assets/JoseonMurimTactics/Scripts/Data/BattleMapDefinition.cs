using System;
using System.Collections.Generic;
using UnityEngine;

namespace JoseonMurimTactics
{
[CreateAssetMenu(menuName = "Joseon Murim Tactics/Battle Map Definition")]
public sealed class BattleMapDefinition : ScriptableObject
{
    public string mapId;
    public string displayName;
    public Vector2Int size = new Vector2Int(16, 12);
    public string oneLineConcept;
    public string visualTheme;
    public string tacticalTheme;
    public string objectiveMain;
    public List<string> objectiveSub = new List<string>();
    public List<string> failureCondition = new List<string>();
    public List<MapLaneDefinition> laneDefinitions = new List<MapLaneDefinition>();
    public List<MapZoneDefinition> chokePoints = new List<MapZoneDefinition>();
    public List<MapZoneDefinition> highGroundZones = new List<MapZoneDefinition>();
    public List<MapZoneDefinition> dangerZones = new List<MapZoneDefinition>();
    public List<BattleGridCellData> cells = new List<BattleGridCellData>();
    public List<BattleInteractableDefinition> interactables = new List<BattleInteractableDefinition>();
    public List<BattleSpawnPoint> spawnPoints = new List<BattleSpawnPoint>();
    public List<BattleObjectiveDefinition> objectives = new List<BattleObjectiveDefinition>();
    public List<BattleReinforcementRule> reinforcementRules = new List<BattleReinforcementRule>();
    public MapVisualTheme visualThemeData = new MapVisualTheme();
    public MapQualityTarget qualityTarget = new MapQualityTarget();
    public string cameraIntroPath;
    [TextArea(2, 5)]
    public string briefingText;
    [TextArea(2, 5)]
    public string recommendedPartyNotes;
}

[Serializable]
public sealed class BattleGridCellData
{
    public Vector2Int cell;
    public TerrainType terrain = TerrainType.Plain;
    public int elevation;
    public int moveCost = 1;
    public bool walkable = true;
    public bool blocksMovement;
    public bool blocksLineOfSight;
    public bool isChokePoint;
    public int capacity = 1;
    public CoverType coverType;
    public EdgeType northEdge;
    public EdgeType eastEdge;
    public EdgeType southEdge;
    public EdgeType westEdge;
    public HazardType hazard;
    public string zoneId;
    public string laneId;
    public string visualTileKey;
    public string decorSetKey;
}

[Serializable]
public sealed class BattleInteractableDefinition
{
    public string id;
    public string displayName;
    public Vector2Int cell;
    public InteractableKind kind;
    public ActionSlot requiredActionSlot = ActionSlot.Main;
    public StatType checkStat = StatType.Strength;
    public int dc = 12;
    public int radius = 1;
    public bool consumedOnUse = true;
    public string visualPrefabKey;
    public List<BattleMapEffect> effects = new List<BattleMapEffect>();
}

[Serializable]
public sealed class BattleMapEffect
{
    public BattleMapEffectType effectType;
    public BattleMapTargetPattern targetPattern = BattleMapTargetPattern.Self;
    public List<Vector2Int> definedCells = new List<Vector2Int>();
    public int durationTurns = 1;
    public int power;
    public TimelineCue timelineCue = TimelineCue.TerrainCue;
}

[Serializable]
public sealed class BattleSpawnPoint
{
    public string id;
    public Faction faction;
    public Vector2Int cell;
    public string laneId;
    public string note;
}

[Serializable]
public sealed class BattleObjectiveDefinition
{
    public string id;
    public string displayName;
    public Vector2Int cell;
    public bool optional;
    public string description;
}

[Serializable]
public sealed class BattleReinforcementRule
{
    public string id;
    public int turn;
    public Faction faction = Faction.Enemy;
    public string spawnPointId;
    public List<string> unitIds = new List<string>();
    public string triggerFlag;
    public string description;
}

[Serializable]
public sealed class MapLaneDefinition
{
    public string id;
    public string displayName;
    public string tacticalNote;
    public List<Vector2Int> cells = new List<Vector2Int>();
}

[Serializable]
public sealed class MapZoneDefinition
{
    public string id;
    public string displayName;
    public string tacticalNote;
    public List<Vector2Int> cells = new List<Vector2Int>();
}

[Serializable]
public sealed class MapVisualTheme
{
    public string groundSetKey;
    public string decorSetKey;
    public Color ambientColor = new Color(0.82f, 0.86f, 0.78f, 1f);
    public Color fogColor = new Color(0.70f, 0.78f, 0.72f, 1f);
    public bool use2DLighting = true;
}

[Serializable]
public sealed class MapQualityTarget
{
    public float maxOpenAreaRatio = 0.45f;
    public int minLanes = 3;
    public int minChokePoints = 3;
    public int minElevationLevels = 2;
    public int minInteractables = 6;
    public int minObjectiveCells = 2;
    public int minHighGroundZones = 1;
    public int minLineOfSightBlockerZones = 1;
    public bool requiresDestructibleOrTransformableTerrain = true;
}
}
