using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace JoseonMurimTactics.Editor
{
public static class BaekduSnowfieldGridSmokeCheck
{
    public static void Run()
    {
        bool ok = true;
        Dictionary<Vector2Int, BattleMapRuntimeCell> cells =
            new Dictionary<Vector2Int, BattleMapRuntimeCell>();
        List<Vector2Int> allyStarts = new List<Vector2Int>();
        List<Vector2Int> enemyStarts = new List<Vector2Int>();

        foreach (BattleMapRuntimeCell cell in BattleMapRuntimeCatalog.Cells(BattleTestMapVariant.BaekduSnowfieldGrid))
        {
            if (cell == null)
            {
                continue;
            }

            cells[cell.cell] = cell;
            if (IsStandable(cell) && cell.deployZone > 0)
            {
                allyStarts.Add(cell.cell);
            }

            if (IsStandable(cell) && cell.HasTag(BattleMapRuntimeEditStore.EnemySpawnTag))
            {
                enemyStarts.Add(cell.cell);
            }
        }

        Check(cells.Count == 192, "BaekduSnowfieldGrid must expose all 16x12 cells.", ref ok);
        Check(allyStarts.Count >= 6, "BaekduSnowfieldGrid needs enough ally deployment tiles.", ref ok);
        Check(enemyStarts.Count >= 6, "BaekduSnowfieldGrid needs enough enemy spawn tiles.", ref ok);
        CheckRoute(cells, "central stairs", ref ok, new Vector2Int(7, 2), new Vector2Int(7, 3),
                   new Vector2Int(7, 4), new Vector2Int(7, 5), new Vector2Int(7, 6),
                   new Vector2Int(7, 7), new Vector2Int(7, 8), new Vector2Int(7, 9));
        CheckRoute(cells, "left ice bridge stairs", ref ok, new Vector2Int(5, 2), new Vector2Int(4, 3),
                   new Vector2Int(2, 4), new Vector2Int(3, 4), new Vector2Int(4, 4),
                   new Vector2Int(4, 5), new Vector2Int(4, 6), new Vector2Int(5, 7),
                   new Vector2Int(5, 8), new Vector2Int(5, 9), new Vector2Int(6, 9));
        CheckRoute(cells, "right snow ramp", ref ok, new Vector2Int(10, 2), new Vector2Int(11, 3),
                   new Vector2Int(12, 4), new Vector2Int(12, 5), new Vector2Int(11, 6),
                   new Vector2Int(10, 7), new Vector2Int(10, 8), new Vector2Int(10, 9),
                   new Vector2Int(9, 9));

        HashSet<Vector2Int> reachable = Flood(cells, allyStarts);
        for (int i = 0; i < enemyStarts.Count; i++)
        {
            Check(reachable.Contains(enemyStarts[i]),
                  $"Enemy spawn {enemyStarts[i].x},{enemyStarts[i].y} must be reachable from ally starts.",
                  ref ok);
        }

        if (!ok)
        {
            EditorApplication.Exit(1);
            return;
        }

        Debug.Log(
            $"[BaekduSnowfieldGridSmokeCheck] PASS cells={cells.Count} allies={allyStarts.Count} enemies={enemyStarts.Count} reachable={reachable.Count}"
        );
        EditorApplication.Exit(0);
    }

    private static void CheckRoute(
        IReadOnlyDictionary<Vector2Int, BattleMapRuntimeCell> cells,
        string label,
        ref bool ok,
        params Vector2Int[] route
    )
    {
        for (int i = 0; i < route.Length; i++)
        {
            Vector2Int cell = route[i];
            Check(cells.TryGetValue(cell, out BattleMapRuntimeCell data) && IsStandable(data),
                  $"{label} route cell {cell.x},{cell.y} must be standable.",
                  ref ok);
        }
    }

    private static HashSet<Vector2Int> Flood(
        IReadOnlyDictionary<Vector2Int, BattleMapRuntimeCell> cells,
        IReadOnlyList<Vector2Int> starts
    )
    {
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        for (int i = 0; i < starts.Count; i++)
        {
            if (visited.Add(starts[i]))
            {
                queue.Enqueue(starts[i]);
            }
        }

        Vector2Int[] neighbors =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            for (int i = 0; i < neighbors.Length; i++)
            {
                Vector2Int next = current + neighbors[i];
                if (visited.Contains(next) ||
                    !cells.TryGetValue(next, out BattleMapRuntimeCell cell) ||
                    !IsStandable(cell))
                {
                    continue;
                }

                visited.Add(next);
                queue.Enqueue(next);
            }
        }

        return visited;
    }

    private static bool IsStandable(BattleMapRuntimeCell cell)
    {
        return cell != null && cell.walkable && cell.occupyAllowed && cell.moveCost < 90;
    }

    private static void Check(bool condition, string message, ref bool ok)
    {
        if (condition)
        {
            return;
        }

        ok = false;
        Debug.LogError("[BaekduSnowfieldGridSmokeCheck] " + message);
    }
}
}
