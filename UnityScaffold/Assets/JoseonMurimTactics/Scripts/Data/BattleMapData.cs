using System;
using System.Collections.Generic;
using UnityEngine;

namespace JoseonMurimTactics
{
    [CreateAssetMenu(menuName = "Joseon Murim Tactics/Battle Map Data")]
    public sealed class BattleMapData : ScriptableObject
    {
        public List<BattleNodeData> nodes = new List<BattleNodeData>();
        public List<InteractableObjectData> objects = new List<InteractableObjectData>();
    }

    [Serializable]
    public sealed class BattleNodeData
    {
        public string id;
        public string displayName;
        public Vector3 worldPosition;
        public TerrainType terrainType;
        public int elevation;
        public CoverType coverType;
        public HazardType hazardType;
        public List<string> neighbors = new List<string>();
    }

    [Serializable]
    public sealed class InteractableObjectData
    {
        public string id;
        public string displayName;
        public string nodeId;
        public ActionSlot requiredActionSlot = ActionSlot.Main;
        public StatType stat = StatType.Strength;
        public int dc = 12;
        public InteractableEffectType effectType;
        public TimelineCue timelineCue = TimelineCue.TerrainCue;
        public bool consumedOnUse = true;
    }
}
