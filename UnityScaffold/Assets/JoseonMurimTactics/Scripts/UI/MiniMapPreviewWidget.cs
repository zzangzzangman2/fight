using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace JoseonMurimTactics
{
public sealed class MiniMapPreviewWidget : MonoBehaviour
{
    [SerializeField]
    private RectTransform tileRoot;
    [SerializeField]
    private Image tilePrefab;

    public void BuildPlaceholder(int width, int height, IReadOnlyList<Vector2Int> allyStarts,
                                 IReadOnlyList<Vector2Int> enemyStarts)
    {
        if (tileRoot == null || tilePrefab == null || width <= 0 || height <= 0)
        {
            return;
        }

        for (int i = tileRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(tileRoot.GetChild(i).gameObject);
        }

        Vector2 size = tileRoot.rect.size;
        float cellW = size.x / width;
        float cellH = size.y / height;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Image tile = Instantiate(tilePrefab, tileRoot);
                RectTransform rect = tile.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                rect.sizeDelta = new Vector2(cellW - 1f, cellH - 1f);
                rect.anchoredPosition = new Vector2(x * cellW, -y * cellH);
                tile.color = TileColor(new Vector2Int(x, y), allyStarts, enemyStarts);
            }
        }
    }

    private static Color TileColor(Vector2Int cell, IReadOnlyList<Vector2Int> allies, IReadOnlyList<Vector2Int> enemies)
    {
        if (Contains(allies, cell))
        {
            return new Color(0.169f, 0.514f, 0.494f, 0.92f);
        }

        if (Contains(enemies, cell))
        {
            return new Color(0.706f, 0.220f, 0.169f, 0.92f);
        }

        return new Color(0.854f, 0.801f, 0.665f, 0.82f);
    }

    private static bool Contains(IReadOnlyList<Vector2Int> cells, Vector2Int cell)
    {
        if (cells == null)
        {
            return false;
        }

        for (int i = 0; i < cells.Count; i++)
        {
            if (cells[i] == cell)
            {
                return true;
            }
        }

        return false;
    }
}
}
