using System;
using System.Collections.Generic;
using UnityEngine;

namespace JoseonMurimTactics
{
[CreateAssetMenu(menuName = "Joseon Murim Tactics/Battle Map Data")]
public sealed class BattleMapData : ScriptableObject
{
    public string mapId;
    public string displayName;
    public string oneLineConcept;
    [TextArea(2, 5)]
    public string briefingText;
    public Vector2Int origin;
    public Vector2Int size = new Vector2Int(8, 8);
    public List<BattleCellData> cells = new List<BattleCellData>();
    public List<InteractableObjectData> objects = new List<InteractableObjectData>();
    public MapQualityTarget qualityTarget = new MapQualityTarget();
}

[Serializable]
public sealed class BattleCellData
{
    public Vector2Int cell;
    public string displayName;
    public Vector3 worldPosition;
    public TerrainType terrainType = TerrainType.Stone;
    public int moveCost = 1;
    public bool walkable = true;
    public bool blocksMovement;
    public bool blocksLineOfSight;
    public bool isChokePoint;
    public int capacity = 1;
    public int elevation;
    public CoverType coverType;
    public HazardType hazardType;
    public EdgeType northEdge;
    public EdgeType eastEdge;
    public EdgeType southEdge;
    public EdgeType westEdge;
    public string zoneId;
    public string laneId;
    public string visualTileKey;
    public string decorSetKey;
}

[Serializable]
public sealed class InteractableObjectData
{
    public string id;
    public string displayName;
    public Vector2Int cell;
    public ActionSlot requiredActionSlot = ActionSlot.Main;
    public StatType stat = StatType.Strength;
    public int dc = 12;
    public int radius = 1;
    public InteractableEffectType effectType;
    public InteractableKind kind;
    public TimelineCue timelineCue = TimelineCue.TerrainCue;
    public bool consumedOnUse = true;
    public string visualPrefabKey;
}
}
