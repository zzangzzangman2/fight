using System;
using System.Collections.Generic;
using UnityEngine;

namespace JoseonMurimTactics
{
public readonly struct BattleTargetingForecast
{
    public readonly bool canTarget;
    public readonly string reason;

    public BattleTargetingForecast(bool canTarget, string reason)
    {
        this.canTarget = canTarget;
        this.reason = reason;
    }
}

public static class BattleTargetingService
{
    public static BattleTargetingForecast CanAttack(BattleTestUnit attacker, BattleTestUnit target, int range,
                                                    Func<Vector2Int, BattleTestTile> tileAt,
                                                    Func<Vector2Int, bool> isInside)
    {
        if (attacker == null || target == null || attacker.defeated || target.defeated)
        {
            return new BattleTargetingForecast(false, "invalid");
        }

        if (attacker.definition.faction == target.definition.faction)
        {
            return new BattleTargetingForecast(false, "same faction");
        }

        return CanAttackFrom(attacker.cell, target.cell, range, tileAt, isInside);
    }

    public static BattleTargetingForecast CanAttackFrom(Vector2Int fromCell, Vector2Int targetCell, int range,
                                                        Func<Vector2Int, BattleTestTile> tileAt,
                                                        Func<Vector2Int, bool> isInside)
    {
        int distance = GridDistance(fromCell, targetCell);
        if (distance > range)
        {
            return new BattleTargetingForecast(false, "out of range");
        }

        BattleTestTile fromTile = tileAt == null ? null : tileAt(fromCell);
        BattleTestTile targetTile = tileAt == null ? null : tileAt(targetCell);
        if (range <= 1)
        {
            return IsMeleeReachable(fromTile, targetTile)
                       ? new BattleTargetingForecast(true, "melee")
                       : new BattleTargetingForecast(false, "height or edge blocked");
        }

        if (!HasLineOfSight(fromCell, targetCell, tileAt, isInside))
        {
            return new BattleTargetingForecast(false, "line of sight blocked");
        }

        return new BattleTargetingForecast(true, "ranged");
    }

    public static BattleTargetingForecast CanUseSkill(BattleTestUnit actor, BattleTestUnit target, int range,
                                                      bool hostileAttackLike,
                                                      Func<Vector2Int, BattleTestTile> tileAt,
                                                      Func<Vector2Int, bool> isInside)
    {
        if (actor == null || target == null || actor.defeated || target.defeated)
        {
            return new BattleTargetingForecast(false, "invalid");
        }

        int distance = GridDistance(actor.cell, target.cell);
        if (distance > range)
        {
            return new BattleTargetingForecast(false, "out of range");
        }

        if (range <= 1)
        {
            return IsMeleeReachable(tileAt(actor.cell), tileAt(target.cell))
                       ? new BattleTargetingForecast(true, "adjacent skill")
                       : new BattleTargetingForecast(false, "height or edge blocked");
        }

        if (hostileAttackLike && !HasLineOfSight(actor.cell, target.cell, tileAt, isInside))
        {
            return new BattleTargetingForecast(false, "line of sight blocked");
        }

        return new BattleTargetingForecast(true, "skill");
    }

    public static bool IsMeleeReachable(BattleTestTile from, BattleTestTile to)
    {
        if (from == null || to == null || GridDistance(from.cell, to.cell) > 1)
        {
            return false;
        }

        int diff = Mathf.Abs(to.elevation - from.elevation);
        int maxAllowedDelta = BattlePathService.IsStairLinked(from, to) ? 2 : 1;
        if (diff > maxAllowedDelta)
        {
            return false;
        }

        return !BlocksMeleeEdge(from, to);
    }

    public static bool HasLineOfSight(Vector2Int fromCell, Vector2Int toCell,
                                      Func<Vector2Int, BattleTestTile> tileAt,
                                      Func<Vector2Int, bool> isInside)
    {
        if (tileAt == null || isInside == null || !isInside(fromCell) || !isInside(toCell))
        {
            return false;
        }

        if (GridDistance(fromCell, toCell) <= 1)
        {
            return true;
        }

        BattleTestTile source = tileAt(fromCell);
        int sourceElevation = source == null ? 0 : source.elevation;
        foreach (Vector2Int cell in CellsOnLine(fromCell, toCell))
        {
            if (cell == fromCell || cell == toCell)
            {
                continue;
            }

            BattleTestTile tile = tileAt(cell);
            if (tile == null)
            {
                return false;
            }

            if (tile.smokeTurns > 0 || tile.blocksProjectiles)
            {
                return false;
            }

            if (!tile.blocksLineOfSight)
            {
                continue;
            }

            if (sourceElevation >= tile.elevation + 2)
            {
                continue;
            }

            return false;
        }

        return true;
    }

    public static List<Vector2Int> GetAttackableTiles(Vector2Int fromCell, int range,
                                                      Func<Vector2Int, BattleTestTile> tileAt,
                                                      Func<Vector2Int, bool> isInside)
    {
        List<Vector2Int> result = new List<Vector2Int>();
        for (int y = fromCell.y - range; y <= fromCell.y + range; y++)
        {
            for (int x = fromCell.x - range; x <= fromCell.x + range; x++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                if (!isInside(cell) || cell == fromCell || GridDistance(fromCell, cell) > range)
                {
                    continue;
                }

                if (CanAttackFrom(fromCell, cell, range, tileAt, isInside).canTarget)
                {
                    result.Add(cell);
                }
            }
        }

        return result;
    }

    private static bool BlocksMeleeEdge(BattleTestTile from, BattleTestTile to)
    {
        if (from == null || to == null)
        {
            return false;
        }

        Vector2Int direction = to.cell - from.cell;
        return IsBlockingEdge(EdgeToward(from, direction)) || IsBlockingEdge(EdgeToward(to, -direction));
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
        return edge == EdgeType.HighWall || edge == EdgeType.CliffDrop || edge == EdgeType.Fence;
    }

    private static IEnumerable<Vector2Int> CellsOnLine(Vector2Int fromCell, Vector2Int toCell)
    {
        int x0 = fromCell.x;
        int y0 = fromCell.y;
        int x1 = toCell.x;
        int y1 = toCell.y;
        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            yield return new Vector2Int(x0, y0);

            if (x0 == x1 && y0 == y1)
            {
                yield break;
            }

            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }

            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }

    private static int GridDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}
}
