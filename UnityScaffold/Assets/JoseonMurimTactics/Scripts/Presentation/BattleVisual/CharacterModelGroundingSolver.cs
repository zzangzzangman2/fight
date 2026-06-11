using UnityEngine;

namespace JoseonMurimTactics
{
public static class CharacterModelGroundingSolver
{
    public static void Apply(Transform modelRoot, Renderer[] renderers, float groundY)
    {
        if (modelRoot == null || renderers == null || renderers.Length == 0)
        {
            return;
        }

        if (!TryGetBounds(renderers, out Bounds bounds))
        {
            return;
        }

        float footDelta = bounds.min.y - groundY;
        Vector3 position = modelRoot.position;
        position.y -= footDelta;
        modelRoot.position = position;
    }

    private static bool TryGetBounds(Renderer[] renderers, out Bounds combined)
    {
        combined = default;
        bool initialized = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled)
            {
                continue;
            }

            if (!initialized)
            {
                combined = renderer.bounds;
                initialized = true;
            }
            else
            {
                combined.Encapsulate(renderer.bounds);
            }
        }

        return initialized;
    }
}
}
