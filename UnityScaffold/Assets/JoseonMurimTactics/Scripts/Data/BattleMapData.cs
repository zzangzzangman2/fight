using System;
using System.Collections.Generic;
using UnityEngine;

namespace JoseonMurimTactics
{
    [CreateAssetMenu(menuName = "Joseon Murim Tactics/Battle Map Data")]
    public sealed class BattleMapData : ScriptableObject
    {
        public Vector2Int origin;
        public Vector2Int size = new Vector2Int(8, 8);
        public List<BattleCellData> cells = new List<BattleCellData>();
        public List<InteractableObjectData> objects = new List<InteractableObjectData>();
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
        public int elevation;
        public CoverType coverType;
        public HazardType hazardType;
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
        public InteractableEffectType effectType;
        public TimelineCue timelineCue = TimelineCue.TerrainCue;
        public bool consumedOnUse = true;
    }
}
