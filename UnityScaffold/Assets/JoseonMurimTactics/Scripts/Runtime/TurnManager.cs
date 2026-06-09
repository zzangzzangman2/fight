using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace JoseonMurimTactics
{
    public sealed class TurnManager : MonoBehaviour
    {
        public BattleMapData battleMap;
        public Tilemap terrainTilemap;
        public List<CombatantData> combatants = new List<CombatantData>();
        public List<Vector2Int> startCells = new List<Vector2Int>();
        public int seed = 20260607;
        public CombatLog combatLog;

        private readonly List<CombatantRuntime> turnOrder = new List<CombatantRuntime>();
        private DiceRoller dice;
        private MovementResolver movementResolver;
        private LineOfSightResolver lineOfSightResolver;
        private SkillResolver skillResolver;
        private TerrainResolver terrainResolver;
        private int currentIndex;
        private int round = 1;

        public IReadOnlyList<CombatantRuntime> TurnOrder { get { return turnOrder; } }
        public CombatantRuntime ActiveUnit { get { return turnOrder.Count == 0 ? null : turnOrder[currentIndex]; } }
        public int Round { get { return round; } }
        public SkillResolver SkillResolver { get { return skillResolver; } }
        public TerrainResolver TerrainResolver { get { return terrainResolver; } }
        public MovementResolver MovementResolver { get { return movementResolver; } }

        private void Awake()
        {
            if (combatLog == null)
            {
                combatLog = gameObject.AddComponent<CombatLog>();
            }
        }

        private void Start()
        {
            StartBattle();
        }

        public void StartBattle()
        {
            dice = new DiceRoller(seed);
            movementResolver = new MovementResolver(battleMap, terrainTilemap);
            lineOfSightResolver = new LineOfSightResolver(movementResolver);
            skillResolver = new SkillResolver(dice, movementResolver, lineOfSightResolver, combatLog);
            terrainResolver = new TerrainResolver(battleMap, dice, movementResolver, combatLog);
            turnOrder.Clear();

            for (int i = 0; i < combatants.Count; i++)
            {
                CombatantData data = combatants[i];
                Vector2Int cell = i < startCells.Count ? startCells[i] : Vector2Int.zero;
                CombatantRuntime runtime = new CombatantRuntime(data, cell);
                DiceRoll initiative = dice.RollD20();
                int initiativeScore = initiative.total + data.stats.Modifier(StatType.Agility);
                runtime.cooldowns["_initiative"] = initiativeScore;
                turnOrder.Add(runtime);
                combatLog.Add("Init", data.displayName + " 선공권 " + initiativeScore + ".");
            }

            turnOrder.Sort((a, b) => b.cooldowns["_initiative"].CompareTo(a.cooldowns["_initiative"]));
            currentIndex = 0;
            round = 1;
            BeginTurn();
        }

        public void EndTurn()
        {
            currentIndex++;
            if (currentIndex >= turnOrder.Count)
            {
                currentIndex = 0;
                round++;
            }

            BeginTurn();
        }

        private void BeginTurn()
        {
            CombatantRuntime active = ActiveUnit;
            if (active == null)
            {
                return;
            }

            if (active.defeated || active.surrendered)
            {
                EndTurn();
                return;
            }

            active.StartTurn();
            combatLog.Add("Turn", round + "라운드 " + active.DisplayName + " 차례.");
        }
    }
}
