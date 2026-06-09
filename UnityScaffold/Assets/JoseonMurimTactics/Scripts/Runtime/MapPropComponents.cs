using UnityEngine;

namespace JoseonMurimTactics
{
[DisallowMultipleComponent]
public sealed class MapPropView : MonoBehaviour
{
    public string propId;
    public string displayName;
    public Vector2Int cell;
    public InteractableKind kind;
    public string visualPrefabKey;
    public bool interactive;

    public void Configure(string newPropId, string newDisplayName, Vector2Int newCell, InteractableKind newKind,
                          bool newInteractive)
    {
        propId = newPropId;
        displayName = newDisplayName;
        cell = newCell;
        kind = newKind;
        visualPrefabKey = newPropId;
        interactive = newInteractive;
    }
}
}
