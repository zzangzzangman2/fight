using System;
using System.Collections.Generic;
using UnityEngine;

namespace JoseonMurimTactics
{
    public static class BattlePathService
    {
        public static bool CanStandOnTile(BattleTestTile tile)
        {
            if (tile == null || !tile.walkable || tile.moveCost >= 90 || !tile.occupyAllowed)
            {
                return false;
            }

            if (
                tile.terrain == TerrainType.DeepWater
                || tile.terrain == TerrainType.Cliff
                || tile.terrain == TerrainType.Wall
                || tile.hazardType == HazardType.DeepWater
                || tile.hazardType == HazardType.Fall
            )
            {
                return false;
            }

            return true;
        }

        public static bool CanEnterCell(
            BattleTestUnit unit,
            BattleTestTile tile,
            BattleTestUnit occupant,
            bool blockedByInteractable
        )
        {
            if (!CanStandOnTile(tile) || blockedByInteractable)
            {
                return false;
            }

            return occupant == null || occupant == unit;
        }

        public static int StepMoveCost(BattleTestTile from, BattleTestTile to)
        {
            if (!CanStandOnTile(to))
            {
                return int.MaxValue;
            }

            if (BlocksEdgeMovement(from, to))
            {
                return int.MaxValue;
            }

            int elevationDiff = from == null ? 0 : to.elevation - from.elevation;
            int absElevationDiff = Mathf.Abs(elevationDiff);
            int maxAllowedDelta = IsStairLinked(from, to) ? 2 : 1;
            if (absElevationDiff > maxAllowedDelta)
            {
                return int.MaxValue;
            }

            int cost = Mathf.Max(1, to.moveCost);
            if (elevationDiff > 0)
            {
                cost += elevationDiff;
            }
            else if (elevationDiff < -1 && !IsStairLinked(from, to))
            {
                cost += 1;
            }

            if (to.fireTurns > 0)
            {
                cost += 1;
            }

            return cost;
        }

        public static Dictionary<Vector2Int, int> GetReachableCells(
            BattleTestUnit unit,
            int moveRange,
            Func<Vector2Int, BattleTestTile> tileAt,
            Func<Vector2Int, IEnumerable<Vector2Int>> neighbors,
            Func<BattleTestUnit, Vector2Int, bool> canEnterCell
        )
        {
            Dictionary<Vector2Int, int> cost = new Dictionary<Vector2Int, int>();
            if (unit == null || tileAt == null || neighbors == null || canEnterCell == null)
            {
                return cost;
            }

            List<Vector2Int> frontier = new List<Vector2Int>();
            cost[unit.cell] = 0;
            frontier.Add(unit.cell);

            while (frontier.Count > 0)
            {
                Vector2Int current = PopLowestCost(frontier, cost);
                foreach (Vector2Int next in neighbors(current))
                {
                    if (!canEnterCell(unit, next))
                    {
                        continue;
                    }

                    int stepCost = StepMoveCost(tileAt(current), tileAt(next));
                    if (stepCost == int.MaxValue)
                    {
                        continue;
                    }

                    int nextCost = cost[current] + stepCost;
                    if (nextCost > moveRange)
                    {
                        continue;
                    }

                    if (cost.TryGetValue(next, out int oldCost) && oldCost <= nextCost)
                    {
                        continue;
                    }

                    cost[next] = nextCost;
                    if (!frontier.Contains(next))
                    {
                        frontier.Add(next);
                    }
                }
            }

            return cost;
        }

        public static List<Vector2Int> FindMovePath(
            BattleTestUnit unit,
            Vector2Int destination,
            int moveRange,
            Func<Vector2Int, BattleTestTile> tileAt,
            Func<Vector2Int, IEnumerable<Vector2Int>> neighbors,
            Func<BattleTestUnit, Vector2Int, bool> canEnterCell
        )
        {
            Dictionary<Vector2Int, int> cost = new Dictionary<Vector2Int, int>();
            Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            if (unit == null || tileAt == null || neighbors == null || canEnterCell == null)
            {
                return new List<Vector2Int>();
            }

            List<Vector2Int> frontier = new List<Vector2Int>();
            cost[unit.cell] = 0;
            frontier.Add(unit.cell);

            while (frontier.Count > 0)
            {
                Vector2Int current = PopLowestCost(frontier, cost);
                if (current == destination)
                {
                    break;
                }

                foreach (Vector2Int next in neighbors(current))
                {
                    if (!canEnterCell(unit, next))
                    {
                        continue;
                    }

                    int stepCost = StepMoveCost(tileAt(current), tileAt(next));
                    if (stepCost == int.MaxValue)
                    {
                        continue;
                    }

                    int nextCost = cost[current] + stepCost;
                    if (nextCost > moveRange)
                    {
                        continue;
                    }

                    if (cost.TryGetValue(next, out int oldCost) && oldCost <= nextCost)
                    {
                        continue;
                    }

                    cost[next] = nextCost;
                    cameFrom[next] = current;
                    if (!frontier.Contains(next))
                    {
                        frontier.Add(next);
                    }
                }
            }

            if (!cost.ContainsKey(destination))
            {
                return new List<Vector2Int>();
            }

            List<Vector2Int> path = new List<Vector2Int>();
            Vector2Int cell = destination;
            path.Add(cell);
            while (cell != unit.cell)
            {
                if (!cameFrom.TryGetValue(cell, out Vector2Int previous))
                {
                    return new List<Vector2Int>();
                }

                cell = previous;
                path.Add(cell);
            }

            path.Reverse();
            return path;
        }

        public static bool IsStairLinked(BattleTestTile from, BattleTestTile to)
        {
            return HasAnyTag(from, "stairs", "ramp") || HasAnyTag(to, "stairs", "ramp");
        }

        private static bool BlocksEdgeMovement(BattleTestTile from, BattleTestTile to)
        {
            if (from == null || to == null)
            {
                return false;
            }

            Vector2Int direction = to.cell - from.cell;
            EdgeType fromEdge = EdgeToward(from, direction);
            EdgeType toEdge = EdgeToward(to, -direction);
            return IsBlockingEdge(fromEdge) || IsBlockingEdge(toEdge);
        }

        private static EdgeType EdgeToward(BattleTestTile tile, Vector2Int direction)
        {
            if (direction.x > 0)
            {
                return tile.eastEdge;
            }

            if (direction.x < 0)
            {
                return tile.westEdge;
            }

            if (direction.y > 0)
            {
                return tile.northEdge;
            }

            if (direction.y < 0)
            {
                return tile.southEdge;
            }

            return EdgeType.None;
        }

        private static bool IsBlockingEdge(EdgeType edge)
        {
            return edge == EdgeType.HighWall
                || edge == EdgeType.CliffDrop
                || edge == EdgeType.Fence;
        }

        private static bool HasAnyTag(BattleTestTile tile, params string[] tags)
        {
            if (tile == null || tile.tags == null || tags == null)
            {
                return false;
            }

            for (int i = 0; i < tags.Length; i++)
            {
                if (tile.HasTag(tags[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static Vector2Int PopLowestCost(
            List<Vector2Int> frontier,
            Dictionary<Vector2Int, int> cost
        )
        {
            int bestIndex = 0;
            int bestCost = int.MaxValue;
            for (int i = 0; i < frontier.Count; i++)
            {
                Vector2Int cell = frontier[i];
                int value = cost.TryGetValue(cell, out int c) ? c : int.MaxValue;
                if (value < bestCost)
                {
                    bestIndex = i;
                    bestCost = value;
                }
            }

            Vector2Int result = frontier[bestIndex];
            frontier.RemoveAt(bestIndex);
            return result;
        }
    }
}
