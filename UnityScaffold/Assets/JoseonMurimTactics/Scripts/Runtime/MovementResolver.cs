using System.Collections.Generic;
using UnityEngine;

namespace JoseonMurimTactics
{
    public sealed class MovementResolver
    {
        private readonly BattleMapData map;

        public MovementResolver(BattleMapData map)
        {
            this.map = map;
        }

        public BattleNodeData FindNode(string nodeId)
        {
            return map.nodes.Find(node => node.id == nodeId);
        }

        public int Distance(string startNodeId, string endNodeId)
        {
            if (startNodeId == endNodeId)
            {
                return 0;
            }

            Queue<string> frontier = new Queue<string>();
            Dictionary<string, int> distance = new Dictionary<string, int>();
            frontier.Enqueue(startNodeId);
            distance[startNodeId] = 0;

            while (frontier.Count > 0)
            {
                string current = frontier.Dequeue();
                BattleNodeData node = FindNode(current);
                if (node == null)
                {
                    continue;
                }

                foreach (string next in node.neighbors)
                {
                    if (distance.ContainsKey(next))
                    {
                        continue;
                    }

                    distance[next] = distance[current] + 1;
                    if (next == endNodeId)
                    {
                        return distance[next];
                    }

                    frontier.Enqueue(next);
                }
            }

            return int.MaxValue;
        }

        public bool TryMove(CombatantRuntime unit, string destinationNodeId, CombatLog log)
        {
            int cost = Distance(unit.currentNodeId, destinationNodeId);
            if (cost == int.MaxValue || cost > unit.actions.movementLeft)
            {
                log.Add("Move", unit.DisplayName + " 이동 실패: 거리 초과.");
                return false;
            }

            unit.currentNodeId = destinationNodeId;
            unit.actions.movementLeft -= cost;
            log.Add("Move", unit.DisplayName + " -> " + FindNode(destinationNodeId).displayName + " 이동.");
            return true;
        }
    }
}
