using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JoseonMurimTactics
{
public sealed class WorldMapView : UIScreenBase
{
    [SerializeField]
    private RectTransform nodeRoot;
    [SerializeField]
    private Button nodeButtonPrefab;
    [SerializeField]
    private TMP_Text detailText;
    [SerializeField]
    private List<WorldMapNodeData> nodes = new List<WorldMapNodeData>();

    private readonly List<Button> spawned = new List<Button>();
    private GameRoot root;

    public void Bind(GameRoot gameRoot, IReadOnlyList<WorldMapNodeData> source)
    {
        root = gameRoot;
        nodes.Clear();
        if (source != null)
        {
            nodes.AddRange(source);
        }

        Refresh();
    }

    public void Refresh()
    {
        Clear();
        if (nodeRoot == null || nodeButtonPrefab == null)
        {
            return;
        }

        Rect rect = nodeRoot.rect;
        for (int i = 0; i < nodes.Count; i++)
        {
            WorldMapNodeData node = nodes[i];
            if (node == null)
            {
                continue;
            }

            Button button = Instantiate(nodeButtonPrefab, nodeRoot);
            RectTransform t = button.GetComponent<RectTransform>();
            if (t != null)
            {
                t.anchorMin = t.anchorMax = new Vector2(0f, 1f);
                t.anchoredPosition =
                    new Vector2(rect.width * node.normalizedPosition.x, -rect.height * node.normalizedPosition.y);
            }

            TMP_Text label = button.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.text = NodeLabel(node);
            }

            int index = i;
            button.interactable = node.IsUnlocked(root != null ? root.Flags : null);
            button.onClick.AddListener(() => Select(index));
            spawned.Add(button);
        }
    }

    public void Select(int index)
    {
        if (index < 0 || index >= nodes.Count || detailText == null)
        {
            return;
        }

        WorldMapNodeData node = nodes[index];
        string done = node.IsCompleted(root != null ? root.Flags : null) ? "완료" : StateLabel(node.state);
        detailText.text =
            $"{node.displayName}\n{node.subtitle}\n상태 {done} · 권장 {node.recommendedLevel} · 압박 {node.factionPressure}\n{node.description}";
    }

    private void Clear()
    {
        foreach (Button button in spawned)
        {
            if (button != null)
            {
                Destroy(button.gameObject);
            }
        }

        spawned.Clear();
    }

    private static string NodeLabel(WorldMapNodeData node)
    {
        return $"{StateGlyph(node.state)} {node.displayName}";
    }

    private static string StateGlyph(WorldMapNodeState state)
    {
        switch (state)
        {
        case WorldMapNodeState.Locked:
            return "鎖";
        case WorldMapNodeState.Completed:
            return "完";
        case WorldMapNodeState.Danger:
            return "危";
        case WorldMapNodeState.CompanionEvent:
            return "緣";
        case WorldMapNodeState.Hub:
            return "門";
        default:
            return "行";
        }
    }

    private static string StateLabel(WorldMapNodeState state)
    {
        switch (state)
        {
        case WorldMapNodeState.Locked:
            return "잠김";
        case WorldMapNodeState.Completed:
            return "완료";
        case WorldMapNodeState.Danger:
            return "위험";
        case WorldMapNodeState.CompanionEvent:
            return "동료 이벤트";
        case WorldMapNodeState.Hub:
            return "거점";
        default:
            return "가능";
        }
    }
}
}
