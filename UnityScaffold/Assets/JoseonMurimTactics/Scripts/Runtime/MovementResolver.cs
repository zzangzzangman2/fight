using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace JoseonMurimTactics
{
public sealed class MovementResolver
{
    private static readonly Vector2Int[] Directions = { new Vector2Int(1, 0), new Vector2Int(-1, 0),
                                                        new Vector2Int(0, 1), new Vector2Int(0, -1) };

    private readonly BattleMapData map;
    private readonly Tilemap terrainTilemap;

    public MovementResolver(BattleMapData map, Tilemap terrainTilemap = null)
    {
        this.map = map;
        this.terrainTilemap = terrainTilemap;
    }

    public BattleCellData FindCell(Vector2Int cell)
    {
        if (map == null)
        {
            return null;
        }

        return map.cells.Find(item => item.cell == cell);
    }

    public bool IsInside(Vector2Int cell)
    {
        if (terrainTilemap != null && terrainTilemap.HasTile(ToTilemapCell(cell)))
        {
            return true;
        }

        if (map == null)
        {
            return false;
        }

        if (map.cells.Count > 0)
        {
            return FindCell(cell) != null;
        }

        if (map.size.x <= 0 || map.size.y <= 0)
        {
            return false;
        }

        return cell.x >= map.origin.x && cell.y >= map.origin.y && cell.x < map.origin.x + map.size.x &&
               cell.y < map.origin.y + map.size.y;
    }

    public int GridDistance(Vector2Int start, Vector2Int end)
    {
        return Mathf.Abs(start.x - end.x) + Mathf.Abs(start.y - end.y);
    }

    public int Distance(Vector2Int start, Vector2Int end)
    {
        return PathCost(start, end);
    }

    public int PathCost(Vector2Int start, Vector2Int end)
    {
        if (start == end)
        {
            return 0;
        }

        Dictionary<Vector2Int, int> reachable = GetReachableCells(start, int.MaxValue, true);
        return reachable.ContainsKey(end) ? reachable[end] : int.MaxValue;
    }

    public Dictionary<Vector2Int, int> GetReachableCells(Vector2Int start, int movementBudget,
                                                         bool includeStart = false)
    {
        Dictionary<Vector2Int, int> cost = new Dictionary<Vector2Int, int>();
        if (!CanStand(start))
        {
            return cost;
        }

        Queue<Vector2Int> frontier = new Queue<Vector2Int>();
        frontier.Enqueue(start);
        cost[start] = 0;

        while (frontier.Count > 0)
        {
            Vector2Int current = frontier.Dequeue();
            foreach (Vector2Int next in Neighbors(current))
            {
                int moveCost = MoveCost(next);
                if (moveCost == int.MaxValue)
                {
                    continue;
                }

                int currentCost = cost[current];
                if (currentCost > int.MaxValue - moveCost)
                {
                    continue;
                }

                int nextCost = currentCost + moveCost;
                if (nextCost > movementBudget)
                {
                    continue;
                }

                if (cost.ContainsKey(next) && cost[next] <= nextCost)
                {
                    continue;
                }

                cost[next] = nextCost;
                frontier.Enqueue(next);
            }
        }

        if (!includeStart)
        {
            cost.Remove(start);
        }

        return cost;
    }

    public IEnumerable<Vector2Int> Neighbors(Vector2Int cell)
    {
        for (int i = 0; i < Directions.Length; i++)
        {
            yield return cell + Directions[i];
        }
    }

    public int MoveCost(Vector2Int cell)
    {
        if (!IsInside(cell) || IsFallCell(cell) || GetTerrainType(cell) == TerrainType.Wall)
        {
            return int.MaxValue;
        }

        BattleCellData data = FindCell(cell);
        if (data != null)
        {
            return data.walkable ? Mathf.Max(1, data.moveCost) : int.MaxValue;
        }

        TerrainTileData tile = TerrainTile(cell);
        if (tile != null)
        {
            return tile.walkable ? Mathf.Max(1, tile.moveCost) : int.MaxValue;
        }

        return terrainTilemap == null || terrainTilemap.HasTile(ToTilemapCell(cell)) ? 1 : int.MaxValue;
    }

    public bool CanStand(Vector2Int cell)
    {
        return MoveCost(cell) != int.MaxValue;
    }

    public bool IsFallCell(Vector2Int cell)
    {
        return GetHazardType(cell) == HazardType.Fall;
    }

    public TerrainType GetTerrainType(Vector2Int cell)
    {
        BattleCellData data = FindCell(cell);
        if (data != null)
        {
            return data.terrainType;
        }

        TerrainTileData tile = TerrainTile(cell);
        return tile == null ? TerrainType.Stone : tile.terrainType;
    }

    public int GetElevation(Vector2Int cell)
    {
        BattleCellData data = FindCell(cell);
        if (data != null)
        {
            return data.elevation;
        }

        TerrainTileData tile = TerrainTile(cell);
        return tile == null ? 0 : tile.elevation;
    }

    public CoverType GetCoverType(Vector2Int cell)
    {
        BattleCellData data = FindCell(cell);
        if (data != null)
        {
            return data.coverType;
        }

        TerrainTileData tile = TerrainTile(cell);
        return tile == null ? CoverType.None : tile.coverType;
    }

    public HazardType GetHazardType(Vector2Int cell)
    {
        BattleCellData data = FindCell(cell);
        if (data != null)
        {
            return data.hazardType;
        }

        TerrainTileData tile = TerrainTile(cell);
        return tile == null ? HazardType.None : tile.hazardType;
    }

    public string DisplayName(Vector2Int cell)
    {
        BattleCellData data = FindCell(cell);
        if (data != null && !string.IsNullOrEmpty(data.displayName))
        {
            return data.displayName;
        }

        return "(" + cell.x + ", " + cell.y + ")";
    }

    public Vector2Int DirectionFromTo(Vector2Int from, Vector2Int to)
    {
        int dx = to.x - from.x;
        int dy = to.y - from.y;
        if (dx == 0 && dy == 0)
        {
            return Vector2Int.zero;
        }

        if (Mathf.Abs(dx) >= Mathf.Abs(dy))
        {
            return new Vector2Int(dx > 0 ? 1 : -1, 0);
        }

        return new Vector2Int(0, dy > 0 ? 1 : -1);
    }

    public bool TryProjectPush(Vector2Int start, Vector2Int direction, int distance, out Vector2Int destination,
                               out bool fell, out bool blocked)
    {
        destination = start;
        fell = false;
        blocked = false;

        if (direction == Vector2Int.zero || distance <= 0)
        {
            return false;
        }

        for (int i = 0; i < distance; i++)
        {
            Vector2Int next = destination + direction;
            if (!IsInside(next) || IsFallCell(next))
            {
                destination = next;
                fell = true;
                return true;
            }

            if (!CanStand(next))
            {
                blocked = true;
                return destination != start;
            }

            destination = next;
        }

        return destination != start;
    }

    public bool TryMove(CombatantRuntime unit, Vector2Int destinationCell, CombatLog log)
    {
        int cost = PathCost(unit.currentCell, destinationCell);
        if (cost == int.MaxValue || cost > unit.actions.movementLeft)
        {
            log.Add("Move", unit.DisplayName + " 이동 실패: 거리 초과.");
            return false;
        }

        unit.currentCell = destinationCell;
        unit.actions.movementLeft -= cost;
        log.Add("Move", unit.DisplayName + " -> " + DisplayName(destinationCell) + " 이동.");
        return true;
    }

    private TerrainTileData TerrainTile(Vector2Int cell)
    {
        return terrainTilemap == null ? null : terrainTilemap.GetTile<TerrainTileData>(ToTilemapCell(cell));
    }

    private Vector3Int ToTilemapCell(Vector2Int cell)
    {
        return new Vector3Int(cell.x, cell.y, 0);
    }
}
}
