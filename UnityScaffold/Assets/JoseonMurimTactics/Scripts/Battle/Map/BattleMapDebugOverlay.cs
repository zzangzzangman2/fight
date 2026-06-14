using System.Collections.Generic;
using UnityEngine;

namespace JoseonMurimTactics
{
public enum BattleMapDebugOverlayMode
{
    None,
    Walkable,
    Elevation,
    LineOfSight,
    Cover,
    MoveCost,
    DeployZone
}

[DisallowMultipleComponent]
public sealed class BattleMapDebugOverlay : MonoBehaviour
{
    private BattleTestController controller;
    private BattleMapDebugOverlayMode mode;
    private GUIStyle labelStyle;
    private GUIStyle headerStyle;

    public BattleMapDebugOverlayMode Mode => mode;

    public void Bind(BattleTestController owner)
    {
        controller = owner;
    }

    private void Update()
    {
        if (controller == null)
        {
            controller = FindAnyObjectByType<BattleTestController>();
        }

        if (Input.GetKeyDown(KeyCode.F1))
        {
            Toggle(BattleMapDebugOverlayMode.Walkable);
        }
        else if (Input.GetKeyDown(KeyCode.F2))
        {
            Toggle(BattleMapDebugOverlayMode.Elevation);
        }
        else if (Input.GetKeyDown(KeyCode.F3))
        {
            Toggle(BattleMapDebugOverlayMode.LineOfSight);
        }
        else if (Input.GetKeyDown(KeyCode.F4))
        {
            Toggle(BattleMapDebugOverlayMode.Cover);
        }
        else if (Input.GetKeyDown(KeyCode.F5))
        {
            Toggle(BattleMapDebugOverlayMode.MoveCost);
        }
        else if (Input.GetKeyDown(KeyCode.F6))
        {
            Toggle(BattleMapDebugOverlayMode.DeployZone);
        }
    }

    private void LateUpdate()
    {
        if (mode == BattleMapDebugOverlayMode.None || controller == null || controller.PreviewTiles == null)
        {
            return;
        }

        foreach (BattleTestTile tile in controller.PreviewTiles)
        {
            if (tile != null)
            {
                tile.SetHighlight(ColorFor(tile));
            }
        }
    }

    private void OnGUI()
    {
        if (mode == BattleMapDebugOverlayMode.None || controller == null)
        {
            return;
        }

        EnsureStyles();
        GUI.Label(new Rect(18f, 18f, 420f, 24f),
                  $"DEBUG {mode} | F1 W/B  F2 E  F3 LOS  F4 Cover  F5 Cost  F6 Deploy",
                  headerStyle);

        Camera camera = Camera.main;
        if (camera == null)
        {
            return;
        }

        foreach (BattleTestTile tile in controller.PreviewTiles)
        {
            if (tile == null)
            {
                continue;
            }

            Vector3 world = controller.GetPreviewTileWorldPosition(tile.cell);
            Vector3 screen = camera.WorldToScreenPoint(world);
            if (screen.z < 0f || screen.x < -80f || screen.x > Screen.width + 80f ||
                screen.y < -80f || screen.y > Screen.height + 80f)
            {
                continue;
            }

            string text = LabelFor(tile);
            Vector2 size = labelStyle.CalcSize(new GUIContent(text));
            Rect rect = new Rect(screen.x - size.x * 0.5f, Screen.height - screen.y - size.y * 0.5f,
                                 size.x + 6f, size.y + 2f);
            GUI.Label(rect, text, labelStyle);
        }
    }

    private void Toggle(BattleMapDebugOverlayMode next)
    {
        mode = mode == next ? BattleMapDebugOverlayMode.None : next;
        if (controller != null)
        {
            controller.ClearPreviewHighlights();
        }
    }

    private Color ColorFor(BattleTestTile tile)
    {
        switch (mode)
        {
        case BattleMapDebugOverlayMode.Walkable:
            return tile.walkable && tile.occupyAllowed
                       ? new Color(0.10f, 0.55f, 1f, 0.46f)
                       : new Color(1f, 0.12f, 0.08f, 0.50f);
        case BattleMapDebugOverlayMode.Elevation:
            return Color.Lerp(new Color(0.10f, 0.40f, 1f, 0.34f),
                              new Color(1f, 0.80f, 0.12f, 0.56f),
                              Mathf.Clamp01(tile.elevation / 3f));
        case BattleMapDebugOverlayMode.LineOfSight:
            return tile.blocksLineOfSight || tile.blocksProjectiles
                       ? new Color(0.95f, 0.42f, 0.18f, 0.52f)
                       : new Color(0.20f, 0.82f, 0.48f, 0.24f);
        case BattleMapDebugOverlayMode.Cover:
            return tile.coverBonus > 0
                       ? new Color(0.22f, 0.85f, 0.36f, Mathf.Clamp01(0.24f + tile.coverBonus * 0.12f))
                       : new Color(0.25f, 0.25f, 0.25f, 0.12f);
        case BattleMapDebugOverlayMode.MoveCost:
            return tile.moveCost >= 90
                       ? new Color(1f, 0.10f, 0.08f, 0.48f)
                       : Color.Lerp(new Color(0.12f, 0.55f, 1f, 0.32f),
                                    new Color(1f, 0.75f, 0.12f, 0.52f),
                                    Mathf.Clamp01((tile.moveCost - 1f) / 3f));
        case BattleMapDebugOverlayMode.DeployZone:
            return tile.deployZone > 0
                       ? new Color(0.00f, 0.90f, 1f, 0.52f)
                       : new Color(0.22f, 0.22f, 0.22f, 0.12f);
        default:
            return Color.clear;
        }
    }

    private string LabelFor(BattleTestTile tile)
    {
        switch (mode)
        {
        case BattleMapDebugOverlayMode.Walkable:
            return $"{tile.cell.x},{tile.cell.y} {(tile.walkable && tile.occupyAllowed ? "W" : "B")}";
        case BattleMapDebugOverlayMode.Elevation:
            return $"{tile.cell.x},{tile.cell.y} E{tile.elevation}";
        case BattleMapDebugOverlayMode.LineOfSight:
            return $"{tile.cell.x},{tile.cell.y} L{(tile.blocksLineOfSight ? 1 : 0)} P{(tile.blocksProjectiles ? 1 : 0)}";
        case BattleMapDebugOverlayMode.Cover:
            return $"{tile.cell.x},{tile.cell.y} C{tile.coverBonus}";
        case BattleMapDebugOverlayMode.MoveCost:
            return $"{tile.cell.x},{tile.cell.y} M{tile.moveCost}";
        case BattleMapDebugOverlayMode.DeployZone:
            return $"{tile.cell.x},{tile.cell.y} D{tile.deployZone}";
        default:
            return string.Empty;
        }
    }

    private void EnsureStyles()
    {
        if (labelStyle != null)
        {
            return;
        }

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 10,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white },
            padding = new RectOffset(3, 3, 1, 1)
        };
        labelStyle.normal.background = MakeTexture(new Color(0f, 0f, 0f, 0.58f));

        headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.94f, 0.88f, 0.70f, 1f) },
            padding = new RectOffset(8, 8, 4, 4)
        };
        headerStyle.normal.background = MakeTexture(new Color(0f, 0f, 0f, 0.70f));
    }

    private static Texture2D MakeTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }
}
}
