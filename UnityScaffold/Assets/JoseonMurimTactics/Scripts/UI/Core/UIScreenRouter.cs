using System.Collections.Generic;
using UnityEngine;

namespace JoseonMurimTactics
{
/// <summary>Small screen router for Canvas/TMP prefabs. It does not replace SceneFlowController.</summary>
[DisallowMultipleComponent]
public sealed class UIScreenRouter : MonoBehaviour
{
    [SerializeField]
    private UIScreenBase[] screens;

    private readonly Dictionary<string, UIScreenBase> map = new Dictionary<string, UIScreenBase>();

    public UIScreenBase Current { get; private set; }

    private void Awake()
    {
        Rebuild();
    }

    public void Rebuild()
    {
        map.Clear();
        if (screens == null || screens.Length == 0)
        {
            screens = GetComponentsInChildren<UIScreenBase>(true);
        }

        foreach (UIScreenBase screen in screens)
        {
            if (screen != null && !string.IsNullOrEmpty(screen.ScreenId))
            {
                map[screen.ScreenId] = screen;
                screen.Hide();
            }
        }
    }

    public bool Show(string screenId)
    {
        if (string.IsNullOrEmpty(screenId) || !map.TryGetValue(screenId, out UIScreenBase next))
        {
            Debug.LogWarning($"[UIScreenRouter] Unknown screen '{screenId}'.");
            return false;
        }

        if (Current != null && Current != next)
        {
            Current.Hide();
        }

        Current = next;
        Current.Show();
        return true;
    }
}
}
