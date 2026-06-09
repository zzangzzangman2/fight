using System.Collections.Generic;
using UnityEngine;

namespace JoseonMurimTactics
{
public sealed class LineOfSightResolver
{
    private readonly MovementResolver movementResolver;

    public LineOfSightResolver(MovementResolver movementResolver)
    {
        this.movementResolver = movementResolver;
    }

    public bool HasLineOfSight(Vector2Int fromCell, Vector2Int toCell)
    {
        if (!movementResolver.IsInside(toCell))
        {
            return false;
        }

        int sourceElevation = movementResolver.GetElevation(fromCell);
        foreach (Vector2Int cell in CellsOnLine(fromCell, toCell))
        {
            if (cell == fromCell || cell == toCell)
            {
                continue;
            }

            if (!movementResolver.IsInside(cell))
            {
                return false;
            }

            if (movementResolver.GetHazardType(cell) == HazardType.Smoke)
            {
                return false;
            }

            if (!movementResolver.BlocksLineOfSight(cell))
            {
                continue;
            }

            int blockerElevation = movementResolver.GetElevation(cell);
            if (sourceElevation >= blockerElevation + 2)
            {
                continue;
            }

            return false;
        }

        return true;
    }

    public int CoverBonus(Vector2Int targetCell, int range)
    {
        if (range <= 1)
        {
            return 0;
        }

        CoverType coverType = movementResolver.GetCoverType(targetCell);
        if (coverType == CoverType.Heavy)
        {
            return 4;
        }

        return coverType == CoverType.Light ? 2 : 0;
    }

    private IEnumerable<Vector2Int> CellsOnLine(Vector2Int fromCell, Vector2Int toCell)
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
}
}
