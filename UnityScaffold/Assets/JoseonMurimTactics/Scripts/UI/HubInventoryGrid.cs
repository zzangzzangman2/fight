using System.Collections.Generic;
using UnityEngine;

namespace JoseonMurimTactics
{
/// <summary>
/// 허브 정비창 하단의 아이템 그리드(설계 §D). 타입별 색 띠 + 수량 + 강화 단계 배지를 가진
/// 카드형 셀을 그리고, 클릭된 itemId를 반환한다. 1280x720에서도 이름이 읽히는 크기를 유지한다.
/// </summary>
public sealed class HubInventoryGrid
{
    private Vector2 scroll;

    public static Color AccentFor(InventoryItemType type)
    {
        switch (type)
        {
        case InventoryItemType.Equipment:
            return UiTheme.GoldBright;
        case InventoryItemType.Gift:
            return new Color(0.94f, 0.45f, 0.62f, 1f); // 연분홍 — 선물/연애
        case InventoryItemType.Material:
            return new Color(0.72f, 0.52f, 0.34f, 1f); // 청동
        case InventoryItemType.KeyItem:
            return UiTheme.SkyAccent;
        default:
            return UiTheme.Teal;
        }
    }

    /// <summary>그리드를 그리고, 이번 프레임에 클릭된 itemId(없으면 null)를 반환.</summary>
    public string Draw(Rect rect, float s, IReadOnlyList<InventoryStack> stacks, string selectedItemId,
                       EquipmentService equipment)
    {
        string clicked = null;
        UiTheme.DrawFill(rect, new Color(0.012f, 0.018f, 0.018f, 0.55f));

        if (stacks == null || stacks.Count == 0)
        {
            GUI.Label(new Rect(rect.x + 16f * s, rect.y + 12f * s, rect.width - 32f * s, 40f * s),
                      "비어 있다. 장터에서 물건을 들여올 수 있다.", UiTheme.SmallMuted);
            return null;
        }

        float pad = 8f * s;
        int columns = Mathf.Max(2, Mathf.FloorToInt((rect.width - pad) / (190f * s)));
        float cellW = (rect.width - pad * (columns + 1)) / columns;
        float cellH = 66f * s;
        int rows = (stacks.Count + columns - 1) / columns;
        float contentH = rows * (cellH + pad) + pad;

        Rect view = new Rect(0f, 0f, rect.width - 18f * s, Mathf.Max(contentH, rect.height));
        scroll = GUI.BeginScrollView(rect, scroll, view);

        for (int i = 0; i < stacks.Count; i++)
        {
            InventoryStack stack = stacks[i];
            int col = i % columns;
            int row = i / columns;
            Rect cell = new Rect(pad + col * (cellW + pad), pad + row * (cellH + pad), cellW, cellH);

            bool selected = !string.IsNullOrEmpty(selectedItemId) &&
                            InventoryService.NormalizeItemId(selectedItemId) == stack.itemId;
            bool hover = cell.Contains(Event.current.mousePosition);
            Color fill = selected ? new Color(0.105f, 0.165f, 0.140f, 0.96f)
                                  : hover ? new Color(0.085f, 0.100f, 0.092f, 0.94f)
                                          : new Color(0.045f, 0.056f, 0.054f, 0.92f);
            UiTheme.DrawFill(new Rect(cell.x + 2f * s, cell.y + 3f * s, cell.width, cell.height),
                             new Color(0f, 0f, 0f, 0.30f));
            UiTheme.DrawFill(cell, fill);

            Color accent = AccentFor(stack.type);
            UiTheme.DrawFill(new Rect(cell.x, cell.y, 4f * s, cell.height), accent);
            if (selected)
            {
                DrawFrame(cell, Mathf.Max(1f, 1.4f * s), UiTheme.GoldBright);
            }

            DrawItemIcon(new Rect(cell.x + 12f * s, cell.y + 9f * s, 42f * s, 42f * s), stack.itemId, stack.type,
                         s);

            GUIStyle name = new GUIStyle(UiTheme.Body)
            {
                fontSize = Mathf.RoundToInt(16f * s),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperLeft,
                clipping = TextClipping.Clip,
                wordWrap = false
            };
            string label = InventoryService.Label(stack.itemId);
            int level = equipment != null && stack.type == InventoryItemType.Equipment
                            ? equipment.GetUpgradeLevel(stack.itemId)
                            : 0;
            if (level > 0)
            {
                label += $" <color=#F5C75C>+{level}</color>";
            }

            GUI.Label(new Rect(cell.x + 64f * s, cell.y + 8f * s, cell.width - 110f * s, 24f * s), label, name);

            GUIStyle sub = new GUIStyle(UiTheme.SmallMuted)
            {
                fontSize = Mathf.RoundToInt(12f * s),
                alignment = TextAnchor.UpperLeft,
                clipping = TextClipping.Clip
            };
            string typeLine = TypeLabel(stack.type);
            if (equipment != null && stack.type == InventoryItemType.Equipment)
            {
                typeLine += " · 장착 " + equipment.EquippedCount(stack.itemId);
            }

            GUI.Label(new Rect(cell.x + 64f * s, cell.y + 36f * s, cell.width - 110f * s, 20f * s), typeLine, sub);

            GUIStyle countStyle = new GUIStyle(UiTheme.Body)
            {
                fontSize = Mathf.RoundToInt(15f * s),
                alignment = TextAnchor.MiddleRight,
                fontStyle = FontStyle.Bold
            };
            countStyle.normal.textColor = UiTheme.Ink;
            GUI.Label(new Rect(cell.xMax - 52f * s, cell.y + 8f * s, 42f * s, 22f * s), "x" + stack.count, countStyle);

            if (GUI.Button(cell, GUIContent.none, GUIStyle.none))
            {
                clicked = stack.itemId;
            }
        }

        GUI.EndScrollView();
        return clicked;
    }

    public static string TypeLabel(InventoryItemType type)
    {
        switch (type)
        {
        case InventoryItemType.Equipment:
            return "장비";
        case InventoryItemType.Gift:
            return "선물";
        case InventoryItemType.Material:
            return "재료";
        case InventoryItemType.KeyItem:
            return "단서";
        default:
            return "소모품";
        }
    }

    public static void DrawItemIcon(Rect rect, string itemId, InventoryItemType type, float s)
    {
        Color accent = AccentFor(type);
        UiTheme.DrawFill(new Rect(rect.x + 2f * s, rect.y + 3f * s, rect.width, rect.height),
                         new Color(0f, 0f, 0f, 0.26f));
        UiTheme.DrawFill(rect, new Color(0.82f, 0.74f, 0.56f, 0.92f));
        DrawFrame(rect, Mathf.Max(1f, 1.15f * s), accent);

        Sprite sprite = IconSpriteRegistry.LoadSprite(itemId);
        if (sprite != null && sprite.texture != null)
        {
            Rect inner = new Rect(rect.x + 3f * s, rect.y + 3f * s, rect.width - 6f * s, rect.height - 6f * s);
            Rect texRect = sprite.textureRect;
            Texture2D texture = sprite.texture;
            Rect coords = new Rect(texRect.x / texture.width, texRect.y / texture.height,
                                   texRect.width / texture.width, texRect.height / texture.height);
            GUI.DrawTextureWithTexCoords(inner, texture, coords, true);
            return;
        }

        GUIStyle fallback = new GUIStyle(UiTheme.Body)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.RoundToInt(16f * s),
            fontStyle = FontStyle.Bold
        };
        fallback.normal.textColor = UiTheme.Ink;
        string label = TypeLabel(type);
        GUI.Label(rect, string.IsNullOrEmpty(label) ? "?" : label.Substring(0, 1), fallback);
    }

    private static void DrawFrame(Rect rect, float thick, Color color)
    {
        UiTheme.DrawFill(new Rect(rect.x, rect.y, rect.width, thick), color);
        UiTheme.DrawFill(new Rect(rect.x, rect.yMax - thick, rect.width, thick), color);
        UiTheme.DrawFill(new Rect(rect.x, rect.y, thick, rect.height), color);
        UiTheme.DrawFill(new Rect(rect.xMax - thick, rect.y, thick, rect.height), color);
    }
}
}
