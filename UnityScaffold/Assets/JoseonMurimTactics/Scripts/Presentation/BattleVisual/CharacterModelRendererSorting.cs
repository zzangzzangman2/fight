using UnityEngine;

namespace JoseonMurimTactics
{
public static class CharacterModelRendererSorting
{
    public static int CalculateOrder(Transform transform, int baseSortingOrder, int sortingOffset)
    {
        if (transform == null)
        {
            return baseSortingOrder + sortingOffset;
        }

        return baseSortingOrder - Mathf.RoundToInt(transform.position.y * 100f) + sortingOffset;
    }

    public static void Apply(Renderer[] renderers, string sortingLayerName, int sortingOrder)
    {
        if (renderers == null)
        {
            return;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            renderer.sortingLayerName = string.IsNullOrEmpty(sortingLayerName) ? "Default" : sortingLayerName;
            renderer.sortingOrder = sortingOrder;
        }
    }
}
}
