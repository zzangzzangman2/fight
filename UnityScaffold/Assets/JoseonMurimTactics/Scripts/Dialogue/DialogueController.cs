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
    private const float SkipAdvanceDelay = 0.08f;
    private const int MaxHistoryLines = 48;

    private readonly DialogueScript script;
    private readonly GameRoot root;
    private readonly List<string> history = new List<string>();
    private DialogueNode current;
    private string preparedNodeId;
    private int visibleChars;
    private float typeAccumulator;
    private float autoAdvanceTimer;
    private bool skipMode;
    private bool hasAutoOverride;
    private bool autoDialogueOverride;
    private QuickPanel quickPanel;
    private Vector2 logScroll;
    private string quickMessage;
    private float quickMessageTimer;

    public bool IsFinished { get; private set; }
    public string LastEffect { get; private set; }

    private enum QuickPanel
    {
        None,
        Log,
        Save,
        Load
    }

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
        TickQuickMessage();
        bool effectiveAuto = EffectiveAuto(settings);
        bool hasSpeaker = !string.IsNullOrEmpty(current.speakerName);
        bool hasChoices = current.HasChoices;
        if (skipMode && hasChoices)
        {
            skipMode = false;
            SetQuickMessage("선택지는 직접 골라주세요.");
        }

        float margin = Mathf.Clamp(38f * s, 22f, 58f * s);
        float wantedH = hasChoices ? 360f * s : 230f * s;
        float minH = hasChoices ? screenH * 0.34f : screenH * 0.22f;
        float maxH = hasChoices ? screenH * 0.50f : screenH * 0.32f;
        float boxH = Mathf.Clamp(wantedH, minH, maxH);
        Rect box = new Rect(margin, screenH - boxH - margin, screenW - margin * 2f, boxH);
        Rect inner = new Rect(box.x + 42f * s, box.y + 58f * s, box.width - 84f * s, box.height - 104f * s);

        float portraitSize = Mathf.Clamp(210f * s, 128f, Mathf.Min(270f * s, screenH * 0.34f));
        float portraitReserve = hasSpeaker && screenW > 820f ? Mathf.Min(portraitSize * 0.44f, box.width * 0.22f) : 0f;
        inner.width -= portraitReserve;

        DrawCinematicShade(screenW, screenH, s);
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
            DrawAdvanceHint(box, s);
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
            DrawAdvanceButton(box, s, complete, effectiveAuto, skipMode);
        }

        DrawQuickBar(screenW, screenH, box, s, settings);
        DrawQuickPanel(screenW, screenH, box, s);
        DrawQuickMessage(box, s);

        HandleAutoAdvance(settings, complete, effectiveAuto);
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
        while (history.Count > MaxHistoryLines)
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

        if (skipMode)
        {
            visibleChars = line.Length;
            return line;
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

    private void HandleAutoAdvance(GameSettings settings, bool complete, bool autoDialogue)
    {
        if (!complete || current == null || current.HasChoices || (!autoDialogue && !skipMode))
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
        float delay = skipMode ? SkipAdvanceDelay : Mathf.Lerp(AutoAdvanceDelay * 1.35f, AutoAdvanceDelay * 0.45f,
                                                               Mathf.Clamp01(settings.autoTextSpeed));
        if (autoAdvanceTimer >= delay)
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
        Rect shadow = new Rect(box.x + 8f * s, box.y + 10f * s, box.width, box.height);
        UiTheme.DrawFill(shadow, new Color(0f, 0f, 0f, 0.38f));

        UiTheme.DrawPanel(box);

        Rect fill = new Rect(box.x + 10f * s, box.y + 10f * s, box.width - 20f * s, box.height - 20f * s);
        UiTheme.DrawFill(fill, new Color(0.020f, 0.032f, 0.032f, 0.95f));

        Rect topBand = new Rect(box.x + 12f * s, box.y + 12f * s, box.width - 24f * s, 30f * s);
        UiTheme.DrawFill(topBand, new Color(0.010f, 0.052f, 0.056f, 0.86f));
        UiTheme.DrawFill(new Rect(topBand.x, topBand.yMax - 2f * s, topBand.width, 2f * s),
                         new Color(UiTheme.Gold.r, UiTheme.Gold.g, UiTheme.Gold.b, 0.72f));
        UiTheme.DrawFill(new Rect(box.x + 24f * s, box.yMax - 18f * s, box.width - 48f * s, 1.5f * s),
                         new Color(UiTheme.Gold.r, UiTheme.Gold.g, UiTheme.Gold.b, 0.55f));

        Rect breath = new Rect(fill.x + 14f * s, fill.y + 48f * s, fill.width - 28f * s, 1f * s);
        UiTheme.DrawFill(breath, new Color(1f, 1f, 1f, 0.06f));
    }

    private void DrawNamePlate(Rect box, float s)
    {
        string name = string.IsNullOrEmpty(current.speakerName) ? "서술" : current.speakerName;
        float plateW = Mathf.Clamp(96f * s + name.Length * 22f * s, 138f * s, 292f * s);
        Rect plate = new Rect(box.x + 42f * s, box.y - 15f * s, plateW, 42f * s);

        UiTheme.DrawFill(new Rect(plate.x + 6f * s, plate.y + 7f * s, plate.width, plate.height),
                         new Color(0f, 0f, 0f, 0.34f));
        UiTheme.DrawFill(plate, new Color(0.010f, 0.020f, 0.024f, 0.98f));
        UiTheme.DrawFill(new Rect(plate.x + 4f * s, plate.y + 4f * s, plate.width - 8f * s, plate.height - 8f * s),
                         new Color(0.055f, 0.030f, 0.052f, 0.94f));
        UiTheme.DrawFill(new Rect(plate.x + 10f * s, plate.yMax - 7f * s, plate.width - 20f * s, 2f * s),
                         new Color(UiTheme.Gold.r, UiTheme.Gold.g, UiTheme.Gold.b, 0.78f));

        GUIStyle nameStyle =
            new GUIStyle(UiTheme.Speaker) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.RoundToInt(22f * s) };
        nameStyle.normal.textColor = UiTheme.Ink;
        GUI.Label(plate, name, nameStyle);
    }

    private void DrawPortrait(Rect box, float size, float s)
    {
        Rect frame = new Rect(box.xMax - size - 30f * s, box.y - size + 70f * s, size, size);
        UiTheme.DrawPanel(frame, true);

        Rect inner = new Rect(frame.x + 14f * s, frame.y + 14f * s, frame.width - 28f * s, frame.height - 28f * s);
        UiTheme.DrawFill(inner, new Color(0.055f, 0.070f, 0.068f, 0.94f));
        UiTheme.DrawFill(new Rect(inner.x, inner.y, inner.width, 24f * s), new Color(0.020f, 0.050f, 0.054f, 0.82f));

        float seal = Mathf.Min(inner.width, inner.height) * 0.58f;
        Rect sealRect = new Rect(inner.center.x - seal * 0.5f, inner.center.y - seal * 0.55f, seal, seal);
        UiTheme.DrawSeal(sealRect, SpeakerGlyph(), -6f);

        GUIStyle caption =
            new GUIStyle(UiTheme.Small) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        caption.normal.textColor = UiTheme.GoldBright;
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

    private void DrawAdvanceButton(Rect box, float s, bool complete, bool autoDialogue, bool skipping)
    {
        float bw = 168f * s;
        Rect button = new Rect(box.xMax - 40f * s - bw, box.yMax - 58f * s, bw, 42f * s);
        string nextLabel = complete ? "계속" : "전체 표시";
        if (GUI.Button(button, nextLabel, UiTheme.ButtonPrimary))
        {
            ActivatePrimary();
        }

        GUIStyle hint = new GUIStyle(UiTheme.SmallMuted) { alignment = TextAnchor.MiddleRight };
        string hintText = skipping ? "빨리감기" : autoDialogue && complete && !current.HasChoices ? "자동 진행" : "Space / Enter";
        GUI.Label(new Rect(button.x - 180f * s, button.y + 8f * s, 168f * s, 24f * s), hintText, hint);
    }

    private void DrawQuickBar(float screenW, float screenH, Rect box, float s, GameSettings settings)
    {
        float gap = Mathf.Max(4f, 6f * s);
        float bw = Mathf.Max(54f, 68f * s);
        float bh = Mathf.Max(24f, 30f * s);
        float total = bw * 5f + gap * 4f;
        if (total > screenW - 20f)
        {
            bw = Mathf.Max(42f, (screenW - 20f - gap * 4f) / 5f);
            total = bw * 5f + gap * 4f;
        }

        Rect bar = new Rect(Mathf.Max(10f, box.xMax - total), Mathf.Min(screenH - bh - 5f, box.yMax + 6f * s), total,
                            bh);
        float x = bar.x;
        if (QuickButton(new Rect(x, bar.y, bw, bh), "LOG", quickPanel == QuickPanel.Log, s))
        {
            TogglePanel(QuickPanel.Log);
        }

        x += bw + gap;
        if (QuickButton(new Rect(x, bar.y, bw, bh), "SAVE", quickPanel == QuickPanel.Save, s))
        {
            TogglePanel(QuickPanel.Save);
        }

        x += bw + gap;
        if (QuickButton(new Rect(x, bar.y, bw, bh), "LOAD", quickPanel == QuickPanel.Load, s))
        {
            TogglePanel(QuickPanel.Load);
        }

        x += bw + gap;
        bool auto = EffectiveAuto(settings);
        if (QuickButton(new Rect(x, bar.y, bw, bh), "AUTO", auto, s))
        {
            hasAutoOverride = true;
            autoDialogueOverride = !auto;
            SetQuickMessage(autoDialogueOverride ? "자동 진행 켜짐" : "자동 진행 꺼짐");
        }

        x += bw + gap;
        if (QuickButton(new Rect(x, bar.y, bw, bh), "SKIP", skipMode, s))
        {
            if (current.HasChoices)
            {
                skipMode = false;
                SetQuickMessage("선택지는 건너뛸 수 없습니다.");
            }
            else
            {
                skipMode = !skipMode;
                SetQuickMessage(skipMode ? "빨리감기 켜짐" : "빨리감기 꺼짐");
            }
        }
    }

    private bool QuickButton(Rect rect, string label, bool active, float s)
    {
        GUIStyle style = new GUIStyle(active ? UiTheme.ButtonPrimary : UiTheme.Button)
        {
            fontSize = Mathf.Max(11, Mathf.RoundToInt(14f * s)),
            padding = new RectOffset(4, 4, 3, 3)
        };
        return GUI.Button(rect, label, style);
    }

    private void DrawQuickPanel(float screenW, float screenH, Rect box, float s)
    {
        if (quickPanel == QuickPanel.None)
        {
            return;
        }

        float width = Mathf.Min(620f * s, screenW - 36f * s);
        float targetHeight = quickPanel == QuickPanel.Log ? 440f * s : quickPanel == QuickPanel.Load ? 430f * s
                                                                                                     : 360f * s;
        float height = Mathf.Min(targetHeight, screenH - 110f * s);
        width = Mathf.Max(360f, width);
        float minHeight = quickPanel == QuickPanel.Load ? 360f : quickPanel == QuickPanel.Save ? 310f : 300f;
        height = Mathf.Min(Mathf.Max(minHeight, height), screenH - 90f * s);
        Rect panel = new Rect(screenW - width - 42f * s, Mathf.Max(70f * s, box.y - height - 16f * s), width,
                              height);
        if (panel.x < 18f * s)
        {
            panel.x = 18f * s;
            panel.width = screenW - 36f * s;
        }

        UiTheme.DrawPanel(panel, true);
        string title = quickPanel == QuickPanel.Log ? "대화 로그" : quickPanel == QuickPanel.Save ? "저장" : "불러오기";
        GUI.Label(new Rect(panel.x + 20f * s, panel.y + 14f * s, panel.width - 92f * s, 32f * s), title,
                  UiTheme.Heading);
        if (GUI.Button(new Rect(panel.xMax - 62f * s, panel.y + 12f * s, 42f * s, 32f * s), "닫기",
                       SmallPanelButtonStyle(s)))
        {
            quickPanel = QuickPanel.None;
            return;
        }

        Rect inner = new Rect(panel.x + 20f * s, panel.y + 56f * s, panel.width - 40f * s, panel.height - 76f * s);
        switch (quickPanel)
        {
        case QuickPanel.Log:
            DrawLogPanel(inner, s);
            break;
        case QuickPanel.Save:
            DrawSlotPanel(inner, s, true);
            break;
        case QuickPanel.Load:
            DrawSlotPanel(inner, s, false);
            break;
        }
    }

    private void DrawLogPanel(Rect rect, float s)
    {
        GUI.Label(new Rect(rect.x, rect.y, rect.width, 24f * s), "지나간 대사를 다시 봅니다.", UiTheme.SmallMuted);
        Rect view = new Rect(rect.x, rect.y + 30f * s, rect.width, rect.height - 30f * s);
        float lineH = Mathf.Max(48f, 58f * s);
        float contentH = Mathf.Max(view.height + 1f, history.Count * lineH + 12f * s);
        logScroll = GUI.BeginScrollView(view, logScroll, new Rect(0f, 0f, view.width - 18f * s, contentH));

        if (history.Count == 0)
        {
            GUI.Label(new Rect(0f, 0f, view.width - 24f * s, 28f * s), "아직 기록된 대사가 없습니다.", UiTheme.Body);
        }
        else
        {
            GUIStyle speakerStyle = new GUIStyle(UiTheme.Small) { fontStyle = FontStyle.Bold };
            speakerStyle.normal.textColor = UiTheme.SealRed;
            GUIStyle lineStyle = new GUIStyle(UiTheme.Body) { fontSize = Mathf.Max(14, Mathf.RoundToInt(18f * s)) };
            float y = 0f;
            for (int i = 0; i < history.Count; i++)
            {
                SplitHistory(history[i], out string speaker, out string line);
                GUI.Label(new Rect(0f, y, view.width - 24f * s, 20f * s), speaker, speakerStyle);
                GUI.Label(new Rect(0f, y + 20f * s, view.width - 24f * s, lineH - 18f * s), line, lineStyle);
                y += lineH;
            }
        }

        GUI.EndScrollView();
    }

    private void DrawSlotPanel(Rect rect, float s, bool saveMode)
    {
        string caption = saveMode ? "수동 슬롯에 현재 진행을 기록합니다." : "저장된 진행을 불러옵니다.";
        GUI.Label(new Rect(rect.x, rect.y, rect.width, 24f * s), caption, UiTheme.SmallMuted);
        float y = rect.y + 34f * s;

        if (saveMode)
        {
            foreach (string slot in SaveManager.ManualSlots)
            {
                DrawSlotRow(rect.x, ref y, rect.width, s, slot, true);
            }
        }
        else
        {
            foreach (string slot in SaveManager.AllSlots)
            {
                DrawSlotRow(rect.x, ref y, rect.width, s, slot, false);
            }
        }
    }

    private void DrawSlotRow(float x, ref float y, float width, float s, string slot, bool saveMode)
    {
        SaveSlotSummary summary = root != null && root.Save != null ? root.Save.Peek(slot) : new SaveSlotSummary();
        Rect row = new Rect(x, y, width, Mathf.Max(58f, 64f * s));
        UiTheme.DrawPanel(row, true);

        GUI.Label(new Rect(row.x + 14f * s, row.y + 8f * s, 96f * s, 22f * s), SlotLabel(slot), UiTheme.Body);
        string detail = summary.exists
                            ? $"{summary.chapterTitle} · {summary.location} · {summary.playTimeText} · {summary.savedAtText}"
                            : "비어 있음";
        GUI.Label(new Rect(row.x + 14f * s, row.y + 34f * s, row.width - 142f * s, 22f * s), detail,
                  UiTheme.SmallMuted);

        bool canUse = saveMode || summary.exists;
        GUI.enabled = canUse;
        string label = saveMode ? "저장" : "로드";
        if (GUI.Button(new Rect(row.xMax - 104f * s, row.y + 13f * s, 84f * s, 38f * s), label,
                       saveMode ? UiTheme.ButtonPrimary : UiTheme.Button))
        {
            if (saveMode)
            {
                SaveToSlot(slot);
            }
            else
            {
                LoadFromSlot(slot);
            }
        }

        GUI.enabled = true;
        y += row.height + 10f * s;
    }

    private void SaveToSlot(string slot)
    {
        bool ok = root != null && root.Save != null && root.Session != null && root.Save.Save(root.Session, slot);
        SetQuickMessage(ok ? $"{SlotLabel(slot)} 저장 완료" : "저장 실패");
        if (ok)
        {
            quickPanel = QuickPanel.None;
        }
    }

    private void LoadFromSlot(string slot)
    {
        if (root == null || root.Save == null)
        {
            SetQuickMessage("로드 실패");
            return;
        }

        GameSession loaded = root.Save.Load(slot);
        if (loaded == null)
        {
            SetQuickMessage("로드 실패");
            return;
        }

        root.LoadExistingSession(loaded);
        SetQuickMessage($"{SlotLabel(slot)} 로드 완료");
        quickPanel = QuickPanel.None;
        skipMode = false;
        if (root.Flags.HasFlag(StoryFlags.HubUnlocked) || root.Flags.HasFlag(StoryFlags.PrologueCompleted))
        {
            root.Flow.GoToHub(root.Session.currentHubId);
        }
        else
        {
            root.Flow.GoToPrologue();
        }
    }

    private void DrawQuickMessage(Rect box, float s)
    {
        if (quickMessageTimer <= 0f || string.IsNullOrEmpty(quickMessage))
        {
            return;
        }

        float w = Mathf.Min(320f * s, box.width * 0.42f);
        Rect toast = new Rect(box.xMax - w - 40f * s, box.y - 64f * s, w, 38f * s);
        UiTheme.DrawFill(toast, new Color(UiTheme.Ink.r, UiTheme.Ink.g, UiTheme.Ink.b, 0.72f));
        GUIStyle style = new GUIStyle(UiTheme.Small) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        style.normal.textColor = UiTheme.Ink;
        GUI.Label(toast, quickMessage, style);
    }

    private void SetQuickMessage(string message)
    {
        quickMessage = message;
        quickMessageTimer = 1.45f;
    }

    private void TickQuickMessage()
    {
        if (quickMessageTimer <= 0f)
        {
            return;
        }

        bool repaint = Event.current == null || Event.current.type == EventType.Repaint;
        if (repaint)
        {
            quickMessageTimer = Mathf.Max(0f, quickMessageTimer - Time.unscaledDeltaTime);
        }
    }

    private bool EffectiveAuto(GameSettings settings)
    {
        return hasAutoOverride ? autoDialogueOverride : settings.autoDialogue;
    }

    private void TogglePanel(QuickPanel panel)
    {
        quickPanel = quickPanel == panel ? QuickPanel.None : panel;
    }

    private static GUIStyle SmallPanelButtonStyle(float s)
    {
        GUIStyle style = new GUIStyle(UiTheme.Button) { fontSize = Mathf.Max(11, Mathf.RoundToInt(14f * s)) };
        style.padding = new RectOffset(4, 4, 3, 3);
        return style;
    }

    private static void SplitHistory(string entry, out string speaker, out string line)
    {
        int split = string.IsNullOrEmpty(entry) ? -1 : entry.IndexOf(": ");
        if (split <= 0)
        {
            speaker = "서술";
            line = entry ?? string.Empty;
            return;
        }

        speaker = entry.Substring(0, split);
        line = entry.Substring(split + 2);
    }

    private static string SlotLabel(string slot)
    {
        return slot == SaveManager.AutoSlot ? "자동" : "수동 " + slot;
    }

    private void DrawAdvanceHint(Rect box, float s)
    {
        float alpha = Mathf.Lerp(0.35f, 0.78f, Mathf.PingPong(Time.unscaledTime * 1.45f, 1f));
        Rect hint = new Rect(box.xMax - 106f * s, box.yMax - 92f * s, 38f * s, 24f * s);
        GUIStyle style = new GUIStyle(UiTheme.SmallMuted)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.RoundToInt(20f * s)
        };
        style.normal.textColor = new Color(UiTheme.Gold.r, UiTheme.Gold.g, UiTheme.Gold.b, alpha);
        GUI.Label(hint, "...", style);
    }

    private static void DrawCinematicShade(float screenW, float screenH, float s)
    {
        UiTheme.DrawFill(new Rect(0f, screenH - 16f * s, screenW, 16f * s), new Color(0f, 0f, 0f, 0.38f));
    }

    private static GUIStyle DialogueBodyStyle(float s)
    {
        GUIStyle style =
            new GUIStyle(UiTheme.Body) { fontSize = Mathf.RoundToInt(23f * s), alignment = TextAnchor.UpperLeft,
                                         wordWrap = true, richText = true };
        style.normal.textColor = UiTheme.Ink;
        return style;
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
