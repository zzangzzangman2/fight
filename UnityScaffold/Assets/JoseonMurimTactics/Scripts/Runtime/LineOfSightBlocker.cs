using UnityEngine;

namespace JoseonMurimTactics
{
[DisallowMultipleComponent]
public sealed class LineOfSightBlocker : MonoBehaviour
{
    public bool blocksLineOfSight = true;
    public int height = 1;
    public int radius;

    public void Configure(int newHeight, int newRadius = 0)
    {
        blocksLineOfSight = true;
        height = Mathf.Max(1, newHeight);
        radius = Mathf.Max(0, newRadius);
    }
}
}
