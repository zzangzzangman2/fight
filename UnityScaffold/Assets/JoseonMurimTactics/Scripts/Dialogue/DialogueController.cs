using System.Collections.Generic;
using UnityEngine;

namespace JoseonMurimTactics
{
/// <summary>
/// Runtime dialogue presenter for IMGUI screens.
/// Handles typewriter text, choice effects, compact history, and a wuxia-style dialogue frame.
/// </summary>
public sealed class DialogueController
{
    private const float AutoAdvanceDelay = 1.05f;

    private readonly DialogueScript script;
    private readonly GameRoot root;
    private readonly List<string> history = new List<string>();
    private DialogueNode current;
    private string preparedNodeId;
    private int visibleChars;
    private float typeAccumulator;
    private float autoAdvanceTimer;

    public bool IsFinished { get; private set; }
    public string LastEffect { get; private set; }

    public DialogueController(DialogueScript script, GameRoot root)
    {
        this.script = script;
        this.root = root;
        current = script != null ? script.Get(script.startNodeId) : null;
        IsFinished = current == null;
    }

    public void Draw(float screenW, float screenH)
    {
        if (IsFinished || current == null)
        {
            return;
        }

        PrepareCurrentNode();
        HandleKeyboardShortcut();

        float s = UiTheme.Scale;
        GameSettings settings = GameSettings.Load();
        bool hasSpeaker = !string.IsNullOrEmpty(current.speakerName);
        bool hasChoices = current.HasChoices;

        float margin = Mathf.Clamp(38f * s, 22f, 58f * s);
        float boxH = Mathf.Clamp(hasChoices ? 410f * s : 292f * s, screenH * 0.30f, screenH * 0.52f);
        Rect box = new Rect(margin, screenH - boxH - margin, screenW - margin * 2f, boxH);
        Rect inner = new Rect(box.x + 42f * s, box.y + 58f * s, box.width - 84f * s, box.height - 104f * s);

        float portraitSize = Mathf.Clamp(210f * s, 128f, Mathf.Min(270f * s, screenH * 0.34f));
        float portraitReserve = hasSpeaker && screenW > 820f ? Mathf.Min(portraitSize * 0.44f, box.width * 0.22f) : 0f;
        inner.width -= portraitReserve;

        DrawCinematicShade(screenW, screenH, s);
        DrawHistory(screenW, box.y - 116f * s, s);
        if (hasSpeaker)
        {
            DrawPortrait(box, portraitSize, s);
        }

        DrawDialogueFrame(box, s);
        DrawNamePlate(box, s);

        string line = VisibleLine(settings);
        bool complete = IsLineComplete();
        GUIStyle bodyStyle = DialogueBodyStyle(s);
        GUI.Label(inner, line, bodyStyle);

        if (complete)
        {
            DrawTailGlyph(box, s);
        }

        if (!string.IsNullOrEmpty(LastEffect))
        {
            GUI.Label(new Rect(box.x + 38f * s, box.yMax - 44f * s, box.width * 0.56f, 24f * s), LastEffect,
                      UiTheme.SmallMuted);
        }

        if (hasChoices && complete)
        {
            DrawChoices(box, inner, s, settings);
        }
        else
        {
            DrawAdvanceButton(box, s, complete, settings.autoDialogue);
        }

        HandleAutoAdvance(settings, complete);
    }

    private void PrepareCurrentNode()
    {
        if (current == null || preparedNodeId == current.id)
        {
            return;
        }

        preparedNodeId = current.id;
        visibleChars = 0;
        typeAccumulator = 0f;
        autoAdvanceTimer = 0f;

        string speaker = string.IsNullOrEmpty(current.speakerName) ? "서술" : current.speakerName;
        history.Add($"{speaker}: {current.line}");
        if (history.Count > 8)
        {
            history.RemoveAt(0);
        }
    }

    private void HandleKeyboardShortcut()
    {
        Event e = Event.current;
        if (e == null || e.type != EventType.KeyDown)
        {
            return;
        }

        if (e.keyCode == KeyCode.Space || e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
        {
            ActivatePrimary();
            e.Use();
        }
    }

    private void ActivatePrimary()
    {
        int length = CurrentLineLength();
        if (visibleChars < length)
        {
            visibleChars = length;
            typeAccumulator = 0f;
            autoAdvanceTimer = 0f;
            return;
        }

        if (!current.HasChoices)
        {
            Advance(current.nextNodeId);
        }
    }

    private string VisibleLine(GameSettings settings)
    {
        string line = current.line ?? string.Empty;
        if (line.Length == 0)
        {
            return string.Empty;
        }

        bool repaint = Event.current == null || Event.current.type == EventType.Repaint;
        if (visibleChars < line.Length && repaint)
        {
            float speed = Mathf.Lerp(22f, 96f, Mathf.Clamp01(settings.textSpeed));
            typeAccumulator += Time.unscaledDeltaTime * speed;
            int add = Mathf.FloorToInt(typeAccumulator);
            if (add > 0)
            {
                visibleChars = Mathf.Min(line.Length, visibleChars + add);
                typeAccumulator -= add;
            }
        }

        int count = Mathf.Clamp(visibleChars, 0, line.Length);
        string visible = line.Substring(0, count);
        if (count < line.Length && Mathf.Repeat(Time.unscaledTime * 2.5f, 1f) > 0.34f)
        {
            visible += "▌";
        }

        return visible;
    }

    private void HandleAutoAdvance(GameSettings settings, bool complete)
    {
        if (!complete || current == null || current.HasChoices || !settings.autoDialogue)
        {
            autoAdvanceTimer = 0f;
            return;
        }

        bool repaint = Event.current == null || Event.current.type == EventType.Repaint;
        if (!repaint)
        {
            return;
        }

        autoAdvanceTimer += Time.unscaledDeltaTime;
        if (autoAdvanceTimer >= AutoAdvanceDelay)
        {
            Advance(current.nextNodeId);
        }
    }

    private int CurrentLineLength()
    {
        return current == null || current.line == null ? 0 : current.line.Length;
    }

    private bool IsLineComplete()
    {
        return visibleChars >= CurrentLineLength();
    }

    private void DrawDialogueFrame(Rect box, float s)
    {
        UiTheme.DrawPanel(box);

        Rect topBand = new Rect(box.x + 8f * s, box.y + 8f * s, box.width - 16f * s, 34f * s);
        UiTheme.DrawFill(topBand, new Color(0.120f, 0.086f, 0.082f, 0.94f));
        UiTheme.DrawFill(new Rect(topBand.x, topBand.yMax - 4f * s, topBand.width, 4f * s), UiTheme.SealRed);
        UiTheme.DrawFill(new Rect(box.x + 18f * s, box.yMax - 18f * s, box.width - 36f * s, 2f * s), UiTheme.Gold);

        for (int i = 0; i < 9; i++)
        {
            float x = topBand.x + 18f * s + i * 32f * s;
            DrawDiamond(new Rect(x, topBand.y + 10f * s, 10f * s, 10f * s),
                        new Color(UiTheme.Gold.r, UiTheme.Gold.g, UiTheme.Gold.b, 0.55f));
        }
    }

    private void DrawNamePlate(Rect box, float s)
    {
        string name = string.IsNullOrEmpty(current.speakerName) ? "서술" : current.speakerName;
        float plateW = Mathf.Clamp(118f * s + name.Length * 24f * s, 150f * s, 320f * s);
        Rect plate = new Rect(box.x + 42f * s, box.y - 20f * s, plateW, 46f * s);

        UiTheme.DrawFill(new Rect(plate.x - 12f * s, plate.y + 8f * s, 18f * s, 30f * s), UiTheme.Ink);
        UiTheme.DrawFill(new Rect(plate.x + plate.width - 6f * s, plate.y + 8f * s, 18f * s, 30f * s), UiTheme.Ink);
        UiTheme.DrawFill(plate, UiTheme.Navy);
        UiTheme.DrawFill(new Rect(plate.x + 4f * s, plate.y + 4f * s, plate.width - 8f * s, plate.height - 8f * s),
                         new Color(0.196f, 0.111f, 0.180f, 0.92f));
        DrawDiamond(new Rect(plate.x + 12f * s, plate.y + 17f * s, 12f * s, 12f * s), UiTheme.Gold);
        DrawDiamond(new Rect(plate.xMax - 24f * s, plate.y + 17f * s, 12f * s, 12f * s), UiTheme.Gold);

        GUIStyle nameStyle =
            new GUIStyle(UiTheme.Speaker) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.RoundToInt(23f * s) };
        nameStyle.normal.textColor = UiTheme.HanjiPanel;
        GUI.Label(plate, name, nameStyle);
    }

    private void DrawPortrait(Rect box, float size, float s)
    {
        Rect frame = new Rect(box.xMax - size - 30f * s, box.y - size + 70f * s, size, size);
        UiTheme.DrawPanel(frame, true);

        Rect inner = new Rect(frame.x + 14f * s, frame.y + 14f * s, frame.width - 28f * s, frame.height - 28f * s);
        UiTheme.DrawFill(inner, new Color(0.905f, 0.879f, 0.812f, 0.94f));
        UiTheme.DrawFill(new Rect(inner.x, inner.y, inner.width, 24f * s), new Color(0.120f, 0.086f, 0.082f, 0.82f));

        float seal = Mathf.Min(inner.width, inner.height) * 0.58f;
        Rect sealRect = new Rect(inner.center.x - seal * 0.5f, inner.center.y - seal * 0.55f, seal, seal);
        UiTheme.DrawSeal(sealRect, SpeakerGlyph(), -6f);

        GUIStyle caption =
            new GUIStyle(UiTheme.Small) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        caption.normal.textColor = UiTheme.Ink;
        GUI.Label(new Rect(inner.x + 6f * s, inner.yMax - 36f * s, inner.width - 12f * s, 28f * s), current.speakerName,
                  caption);
    }

    private string SpeakerGlyph()
    {
        if (string.IsNullOrEmpty(current.speakerName))
        {
            return "記";
        }

        return current.speakerName.Substring(0, 1);
    }

    private void DrawChoices(Rect box, Rect inner, float s, GameSettings settings)
    {
        float y = inner.y + Mathf.Min(96f * s, inner.height * 0.42f);
        float bh = Mathf.Clamp(46f * s, 34f, 58f * s);
        float gap = 9f * s;
        GUIStyle choiceStyle = new GUIStyle(UiTheme.Button) { alignment = TextAnchor.MiddleLeft,
                                                              fontSize = Mathf.RoundToInt(19f * s), wordWrap = true };
        choiceStyle.padding = new RectOffset(Mathf.RoundToInt(20f * s), Mathf.RoundToInt(14f * s), 6, 6);

        for (int i = 0; i < current.choices.Count; i++)
        {
            DialogueChoice c = current.choices[i];
            string prefix = c.disposition.HasValue ? $"[{StoryEnumLabels.Label(c.disposition.Value)}] " : string.Empty;
            string preview = settings.choiceEffectPreview ? PreviewEffects(c) : string.Empty;
            string label = $"{ChoiceMark(i)}  {prefix}{c.text}";
            if (!string.IsNullOrEmpty(preview))
            {
                label += $"  ({preview})";
            }

            if (GUI.Button(new Rect(inner.x, y, inner.width, bh), label, choiceStyle))
            {
                Choose(c);
                return;
            }

            y += bh + gap;
        }
    }

    private void DrawAdvanceButton(Rect box, float s, bool complete, bool autoDialogue)
    {
        float bw = 168f * s;
        Rect button = new Rect(box.xMax - 40f * s - bw, box.yMax - 58f * s, bw, 42f * s);
        string nextLabel = complete ? "계속" : "전체 표시";
        if (GUI.Button(button, nextLabel, UiTheme.ButtonPrimary))
        {
            ActivatePrimary();
        }

        GUIStyle hint = new GUIStyle(UiTheme.SmallMuted) { alignment = TextAnchor.MiddleRight };
        string hintText = autoDialogue && complete && !current.HasChoices ? "자동 진행" : "Space / Enter";
        GUI.Label(new Rect(button.x - 180f * s, button.y + 8f * s, 168f * s, 24f * s), hintText, hint);
    }

    private void DrawTailGlyph(Rect box, float s)
    {
        Rect r = new Rect(box.xMax - 78f * s, box.yMax - 92f * s, 18f * s, 18f * s);
        Color c = Mathf.Repeat(Time.unscaledTime * 1.6f, 1f) > 0.5f ? UiTheme.SealRed : UiTheme.Gold;
        DrawDiamond(r, c);
    }

    private void DrawHistory(float screenW, float y, float s)
    {
        int endExclusive = history.Count - 1;
        if (endExclusive <= 0 || screenW < 920f)
        {
            return;
        }

        float width = Mathf.Min(560f * s, screenW - 90f * s);
        Rect panel = new Rect(screenW - width - 42f * s, Mathf.Max(104f * s, y), width, 104f * s);
        UiTheme.DrawPanel(panel, true);
        GUI.Label(new Rect(panel.x + 14f * s, panel.y + 8f * s, panel.width - 28f * s, 22f * s), "지난 흐름",
                  UiTheme.SmallMuted);

        int start = Mathf.Max(0, endExclusive - 3);
        float lineY = panel.y + 34f * s;
        for (int i = start; i < endExclusive; i++)
        {
            string text = history[i];
            if (text.Length > 62)
            {
                text = text.Substring(0, 61) + "…";
            }

            GUI.Label(new Rect(panel.x + 14f * s, lineY, panel.width - 28f * s, 20f * s), text, UiTheme.SmallMuted);
            lineY += 22f * s;
        }
    }

    private static void DrawCinematicShade(float screenW, float screenH, float s)
    {
        Color ink = new Color(UiTheme.Ink.r, UiTheme.Ink.g, UiTheme.Ink.b, 0.30f);
        UiTheme.DrawFill(new Rect(0f, 0f, screenW, 52f * s), ink);
        UiTheme.DrawFill(new Rect(0f, screenH - 18f * s, screenW, 18f * s), ink);
        UiTheme.DrawFill(new Rect(0f, 52f * s, screenW, 3f * s), UiTheme.SealRed);
    }

    private static GUIStyle DialogueBodyStyle(float s)
    {
        GUIStyle style =
            new GUIStyle(UiTheme.Body) { fontSize = Mathf.RoundToInt(23f * s), alignment = TextAnchor.UpperLeft,
                                         wordWrap = true, richText = true };
        style.normal.textColor = UiTheme.Ink;
        return style;
    }

    private static void DrawDiamond(Rect rect, Color color)
    {
        Matrix4x4 matrix = GUI.matrix;
        GUIUtility.RotateAroundPivot(45f, rect.center);
        UiTheme.DrawFill(rect, color);
        GUI.matrix = matrix;
    }

    private static string ChoiceMark(int index)
    {
        switch (index)
        {
        case 0:
            return "一";
        case 1:
            return "二";
        case 2:
            return "三";
        case 3:
            return "四";
        default:
            return (index + 1).ToString();
        }
    }

    private void Choose(DialogueChoice c)
    {
        LastEffect = ApplyEffects(c);
        Advance(c.nextNodeId);
    }

    private string ApplyEffects(DialogueChoice c)
    {
        if (root == null)
        {
            return string.Empty;
        }

        List<string> parts = new List<string>();

        foreach (IdDelta d in c.approvalChanges)
        {
            if (d.delta == 0)
                continue;
            if (c.romanticIntent && !root.Approval.CanApplyRomanticEffect(d.id))
            {
                parts.Add($"{CompanionCatalog.Name(d.id)} 연애 반응 제외");
                continue;
            }
            root.Approval.Add(d.id, d.delta);
            parts.Add($"{CompanionCatalog.Name(d.id)} 호감 {Arrow(d.delta)}");
        }

        foreach (IdDelta d in c.factionChanges)
        {
            if (d.delta == 0)
                continue;
            root.Reputation.Add(d.id, d.delta);
            parts.Add($"{FactionIds.Label(d.id)} {Arrow(d.delta)}");
        }

        foreach (string flag in c.flagsAdded)
        {
            root.Flags.SetFlag(flag);
        }

        foreach (IdDelta m in c.battleModifiers)
        {
            root.Flags.SetInt("battlemod:" + m.id, m.delta);
        }

        return parts.Count == 0 ? string.Empty : string.Join("   ", parts);
    }

    private static string PreviewEffects(DialogueChoice c)
    {
        List<string> parts = new List<string>();
        foreach (IdDelta d in c.approvalChanges)
        {
            if (d.delta != 0)
                parts.Add($"{CompanionCatalog.Name(d.id)} {Signed(d.delta)}");
        }

        foreach (IdDelta d in c.factionChanges)
        {
            if (d.delta != 0)
                parts.Add($"{FactionIds.Label(d.id)} {Signed(d.delta)}");
        }

        foreach (IdDelta d in c.battleModifiers)
        {
            if (d.delta != 0)
                parts.Add("전투 보정 " + Signed(d.delta));
        }

        return parts.Count == 0 ? string.Empty : string.Join(", ", parts);
    }

    private static string Arrow(int delta)
    {
        return delta > 0 ? "▲" : "▼";
    }

    private static string Signed(int delta)
    {
        return delta > 0 ? "+" + delta : delta.ToString();
    }

    private void Advance(string nextId)
    {
        DialogueNode next = script.Get(nextId);
        if (next == null)
        {
            current = null;
            IsFinished = true;
            return;
        }

        current = next;
        preparedNodeId = null;
    }
}
}
