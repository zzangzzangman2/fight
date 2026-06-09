using UnityEngine;

namespace JoseonMurimTactics
{
/// <summary>Stores tooltip text and anchor position for a Canvas tooltip prefab.</summary>
[DisallowMultipleComponent]
public sealed class TooltipPresenter : MonoBehaviour
{
    public string Text { get; private set; }
    public Vector2 ScreenPosition { get; private set; }
    public bool IsVisible { get; private set; }

    public void Show(string text, Vector2 screenPosition)
    {
        Text = text;
        ScreenPosition = screenPosition;
        IsVisible = !string.IsNullOrEmpty(text);
        gameObject.SetActive(IsVisible);
    }

    public void Hide()
    {
        Text = null;
        IsVisible = false;
        gameObject.SetActive(false);
    }
}
}
