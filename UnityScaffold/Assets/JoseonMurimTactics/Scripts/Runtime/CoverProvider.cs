using UnityEngine;

namespace JoseonMurimTactics
{
[DisallowMultipleComponent]
public sealed class CoverProvider : MonoBehaviour
{
    public CoverType coverType = CoverType.Light;
    public int coverBonus = 1;
    public int radius = 1;

    public void Configure(CoverType newCoverType, int newCoverBonus, int newRadius = 1)
    {
        coverType = newCoverType;
        coverBonus = Mathf.Max(0, newCoverBonus);
        radius = Mathf.Max(0, newRadius);
    }
}
}
