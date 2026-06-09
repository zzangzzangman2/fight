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

        foreach (Vector2Int cell in CellsOnLine(fromCell, toCell))
        {
            if (cell == fromCell)
            {
                continue;
            }

            if (movementResolver.GetHazardType(cell) == HazardType.Smoke)
            {
                return false;
            }

            if (cell != toCell && movementResolver.GetTerrainType(cell) == TerrainType.Wall)
            {
                return false;
            }
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
        int steps = Mathf.Max(Mathf.Abs(toCell.x - fromCell.x), Mathf.Abs(toCell.y - fromCell.y));
        if (steps == 0)
        {
            yield return fromCell;
            yield break;
        }

        HashSet<Vector2Int> yielded = new HashSet<Vector2Int>();
        for (int i = 0; i <= steps; i++)
        {
            float t = (float)i / steps;
            Vector2Int cell = new Vector2Int(Mathf.RoundToInt(Mathf.Lerp(fromCell.x, toCell.x, t)),
                                             Mathf.RoundToInt(Mathf.Lerp(fromCell.y, toCell.y, t)));

            if (yielded.Add(cell))
            {
                yield return cell;
            }
        }
    }
}
}
