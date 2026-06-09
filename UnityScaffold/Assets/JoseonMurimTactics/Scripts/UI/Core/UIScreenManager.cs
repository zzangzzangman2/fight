using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace JoseonMurimTactics
{
[DisallowMultipleComponent]
public sealed class UIScreenManager : MonoBehaviour
{
    [SerializeField]
    private Canvas canvas;
    [SerializeField]
    private CanvasScaler scaler;
    [SerializeField]
    private GraphicRaycaster raycaster;
    [SerializeField]
    private List<UIScreenBase> screens = new List<UIScreenBase>();

    private readonly Dictionary<string, UIScreenBase> byId = new Dictionary<string, UIScreenBase>();

    public UIScreenBase Current { get; private set; }

    private void Awake()
    {
        EnsureCanvas();
        RebuildMap();
    }

    public void Register(UIScreenBase screen)
    {
        if (screen == null || string.IsNullOrEmpty(screen.ScreenId))
        {
            return;
        }

        if (!screens.Contains(screen))
        {
            screens.Add(screen);
        }

        byId[screen.ScreenId] = screen;
    }

    public bool Show(string screenId)
    {
        if (string.IsNullOrEmpty(screenId) || !byId.TryGetValue(screenId, out UIScreenBase next))
        {
            return false;
        }

        if (Current != null && Current != next)
        {
            Current.Hide();
        }

        Current = next;
        Current.Show();
        FocusFirstSelectable(Current.transform);
        return true;
    }

    public static UIScreenManager EnsureInScene()
    {
        UIScreenManager existing = FindAnyObjectByType<UIScreenManager>();
        if (existing != null)
        {
            return existing;
        }

        GameObject root = new GameObject("NoncombatCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                                         typeof(GraphicRaycaster), typeof(UIScreenManager));
        return root.GetComponent<UIScreenManager>();
    }

    private void EnsureCanvas()
    {
        if (canvas == null)
        {
            canvas = GetComponent<Canvas>();
        }

        if (scaler == null)
        {
            scaler = GetComponent<CanvasScaler>();
        }

        if (raycaster == null)
        {
            raycaster = GetComponent<GraphicRaycaster>();
        }

        if (canvas != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
        }

        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        if (EventSystem.current == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }
    }

    private void RebuildMap()
    {
        byId.Clear();
        if (screens.Count == 0)
        {
            screens.AddRange(GetComponentsInChildren<UIScreenBase>(true));
        }

        foreach (UIScreenBase screen in screens)
        {
            Register(screen);
        }
    }

    private static void FocusFirstSelectable(Transform root)
    {
        if (root == null || EventSystem.current == null)
        {
            return;
        }

        Selectable selectable = root.GetComponentInChildren<Selectable>(true);
        if (selectable != null)
        {
            EventSystem.current.SetSelectedGameObject(selectable.gameObject);
        }
    }
}
}
