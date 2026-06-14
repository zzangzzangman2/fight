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
    private const float DialogueShadeHeightRatio = 0.32f;
    private const float ChoiceShadeHeightRatio = 0.42f;
    private const float DialogueShadeBackdropScale = 1.30f;
    private const float ChoiceShadeBackdropScale = 1.18f;

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

        // 모바일 미연시식 레이아웃: 전신 일러 위로 하단 투명 그라데이션이 깔리고,
        // 그 위에 이름+소속 태그, 빛나는 구분선, 본문이 프레임 없이 얹힌다.
        float shadeH = screenH * (hasChoices ? ChoiceShadeHeightRatio : DialogueShadeHeightRatio);
        float shadeScale = hasChoices ? ChoiceShadeBackdropScale : DialogueShadeBackdropScale;

        DrawBackground(screenW, screenH);
        DrawStanding(screenW, screenH);
        UiTheme.DrawBottomShade(new Rect(0f, screenH - shadeH * shadeScale, screenW, shadeH * shadeScale));

        float textX = screenW * 0.055f;
        float textW = screenW * 0.84f;
        float bodyY = screenH - shadeH + 24f * s;
        if (hasSpeaker)
        {
            bodyY = DrawNameRow(textX, screenH - shadeH + 14f * s, textW, s);
        }

        string line = VisibleLine(settings);
        bool complete = IsLineComplete();
        Rect bodyRect = new Rect(textX, bodyY, textW, screenH - bodyY - 36f * s);
        UiTheme.LabelShadow(bodyRect, line, DialogueBodyStyle(s));

        if (!string.IsNullOrEmpty(LastEffect))
        {
            GUI.Label(new Rect(textX, screenH - 32f * s, screenW * 0.6f, 24f * s), LastEffect, UiTheme.SmallMuted);
        }

        if (complete && !hasChoices)
        {
            DrawContinueIndicator(screenW, screenH, s, effectiveAuto, skipMode);
        }

        if (hasChoices && complete)
        {
            DrawChoices(screenW, screenH, s, settings);
        }

        DrawQuickBar(screenW, s, settings);
        DrawQuickPanel(screenW, screenH, s);
        DrawQuickMessage(screenW, s);

        HandleAutoAdvance(settings, complete, effectiveAuto);
        DrawTapCatcher(screenW, screenH, s, hasChoices, complete);
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

    /// <summary>이름 옆 소속/직함 태그 폴백. 제작 manifest를 거치지 않는 C# 폴백 대사용.</summary>
    private static readonly Dictionary<string, string> FallbackSpeakerTitles = new Dictionary<string, string>
    {
        { "박성준", "백두천광검문 소문주" },
        { "박무겸", "백두천광검문 문주" },
        { "연옥", "백두천광검문 사범" },
        { "초희", "소백촌 약방" },
        { "윤서화", "해동문 예검수" },
        { "백련", "설악창문 의술" },
        { "한비연", "흑립방" },
        { "도아린", "파산권문" },
    };

    private static readonly Dictionary<string, Texture2D> StandingCache = new Dictionary<string, Texture2D>();

    private void DrawBackground(float screenW, float screenH)
    {
        string resource = !string.IsNullOrEmpty(current.backgroundResource)
                              ? current.backgroundResource
                              : DialogueBackgroundRegistry.ResolveResourcePath(current.backgroundId);
        Texture2D background = DialogueBackgroundRegistry.LoadBackgroundTexture(resource);
        if (background == null)
        {
            return;
        }

        GUI.DrawTexture(new Rect(0f, 0f, screenW, screenH), background, ScaleMode.ScaleAndCrop);
        UiTheme.DrawFill(new Rect(0f, 0f, screenW, screenH), new Color(0.03f, 0.04f, 0.06f, 0.18f));
    }

    private string SpeakerTitle()
    {
        if (!string.IsNullOrEmpty(current.speakerTitle))
        {
            return current.speakerTitle;
        }

        return !string.IsNullOrEmpty(current.speakerName) &&
                       FallbackSpeakerTitles.TryGetValue(current.speakerName, out string title)
                   ? title
                   : null;
    }

    /// <summary>대화창 뒤에 한 명짜리 전신/스탠딩 일러를 중앙 정렬로 그린다.</summary>
    private void DrawStanding(float screenW, float screenH)
    {
        string sourceResource = current.portraitResource;
        string resource = PortraitRegistry.ResolveStandingPortraitResource(sourceResource);
        if (string.IsNullOrEmpty(resource))
        {
            return;
        }

        Texture2D tex = LoadStandingTexture(resource);
        if (tex == null && !string.Equals(resource, sourceResource, System.StringComparison.OrdinalIgnoreCase))
        {
            tex = LoadStandingTexture(sourceResource);
        }

        if (tex == null)
        {
            return;
        }

        float h = screenH * 0.96f;
        float w = h * (tex.width / (float)Mathf.Max(1, tex.height));
        float maxW = screenW * 0.72f;
        if (w > maxW)
        {
            float scale = maxW / w;
            w *= scale;
            h *= scale;
        }

        float x = (screenW - w) * 0.5f;
        GUI.DrawTexture(new Rect(x, screenH - h, w, h), tex, ScaleMode.ScaleToFit);
    }

    private static Texture2D LoadStandingTexture(string resource)
    {
        if (string.IsNullOrEmpty(resource))
        {
            return null;
        }

        if (!StandingCache.TryGetValue(resource, out Texture2D tex))
        {
            tex = PortraitRegistry.LoadPortraitTexture(resource);
            StandingCache[resource] = tex; // 실패도 캐시해 매 프레임 재시도를 막는다.
        }

        return tex;
    }

    /// <summary>이름(흰색 대형) + 소속 태그(하늘색) + 빛나는 구분선. 본문 시작 y를 돌려준다.</summary>
    private float DrawNameRow(float x, float y, float width, float s)
    {
        string name = current.speakerName;
        GUIStyle nameStyle = new GUIStyle(UiTheme.Speaker)
        {
            fontSize = Mathf.RoundToInt(31f * s),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.LowerLeft
        };
        nameStyle.normal.textColor = Color.white;
        float nameH = 42f * s;
        UiTheme.LabelShadow(new Rect(x, y, width * 0.6f, nameH), name, nameStyle);

        string title = SpeakerTitle();
        if (!string.IsNullOrEmpty(title))
        {
            GUIStyle tagStyle = new GUIStyle(UiTheme.Small)
            {
                fontSize = Mathf.RoundToInt(17f * s),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.LowerLeft
            };
            tagStyle.normal.textColor = UiTheme.SkyAccent;
            float nameW = nameStyle.CalcSize(new GUIContent(name)).x;
            UiTheme.LabelShadow(new Rect(x + nameW + 14f * s, y, width - nameW - 18f * s, nameH - 4f * s), title,
                                tagStyle);
        }

        float lineY = y + nameH + 8f * s;
        UiTheme.DrawFill(new Rect(x - 2f * s, lineY - 1.5f * s, width, 4.5f * s),
                         new Color(UiTheme.SkyAccent.r, UiTheme.SkyAccent.g, UiTheme.SkyAccent.b, 0.10f));
        UiTheme.DrawFill(new Rect(x - 2f * s, lineY, width, 1.5f * s), new Color(1f, 1f, 1f, 0.50f));
        UiTheme.DrawFill(new Rect(x - 2f * s, lineY, width * 0.22f, 2f * s),
                         new Color(UiTheme.SkyAccent.r, UiTheme.SkyAccent.g, UiTheme.SkyAccent.b, 0.92f));
        return lineY + 15f * s;
    }

    /// <summary>화면 중앙에 떠 있는 반투명 선택지 카드 스택.</summary>
    private void DrawChoices(float screenW, float screenH, float s, GameSettings settings)
    {
        float w = Mathf.Min(760f * s, screenW * 0.58f);
        float x = (screenW - w) * 0.5f;
        float bh = Mathf.Clamp(54f * s, 42f, 66f * s);
        float gap = 12f * s;
        int count = current.choices.Count;
        float y = Mathf.Max(84f * s, screenH * 0.52f - (bh + gap) * count);

        GUIStyle choiceStyle = new GUIStyle(UiTheme.Body)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.RoundToInt(19f * s),
            fontStyle = FontStyle.Bold,
            wordWrap = false
        };

        for (int i = 0; i < count; i++)
        {
            DialogueChoice c = current.choices[i];
            string prefix = c.disposition.HasValue ? $"[{StoryEnumLabels.Label(c.disposition.Value)}] " : string.Empty;
            string preview = settings.choiceEffectPreview ? PreviewEffects(c) : string.Empty;
            string label = $"{prefix}{c.text}";
            if (!string.IsNullOrEmpty(preview))
            {
                label += $"  <size={Mathf.RoundToInt(14f * s)}>({preview})</size>";
            }

            Rect rect = new Rect(x, y, w, bh);
            bool hover = Event.current != null && rect.Contains(Event.current.mousePosition);
            UiTheme.DrawFill(new Rect(rect.x + 3f * s, rect.y + 4f * s, rect.width, rect.height),
                             new Color(0f, 0f, 0f, 0.30f));
            UiTheme.DrawFill(rect, hover ? new Color(0.075f, 0.190f, 0.300f, 0.94f)
                                         : new Color(0.022f, 0.062f, 0.110f, 0.84f));
            UiTheme.DrawFill(new Rect(rect.x, rect.y, 4f * s, rect.height),
                             new Color(UiTheme.SkyAccent.r, UiTheme.SkyAccent.g, UiTheme.SkyAccent.b,
                                       hover ? 1f : 0.72f));
            UiTheme.DrawFill(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), new Color(1f, 1f, 1f, 0.14f));

            choiceStyle.normal.textColor = hover ? Color.white : new Color(0.88f, 0.93f, 0.98f, 1f);
            GUI.Label(rect, label, choiceStyle);
            if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
            {
                Choose(c);
                return;
            }

            y += bh + gap;
        }
    }

    /// <summary>본문 우하단 깜빡이는 진행 표시(▼). AUTO/SKIP 상태면 라벨로 대체.</summary>
    private void DrawContinueIndicator(float screenW, float screenH, float s, bool autoDialogue, bool skipping)
    {
        Rect rect = new Rect(screenW * 0.895f, screenH - 56f * s, screenW * 0.09f, 32f * s);
        GUIStyle style = new GUIStyle(UiTheme.Small)
        {
            alignment = TextAnchor.MiddleRight,
            fontStyle = FontStyle.Bold,
            fontSize = Mathf.RoundToInt(18f * s)
        };

        if (skipping || autoDialogue)
        {
            style.normal.textColor = new Color(UiTheme.SkyAccent.r, UiTheme.SkyAccent.g, UiTheme.SkyAccent.b, 0.85f);
            GUI.Label(rect, skipping ? "▶▶ SKIP" : "AUTO", style);
            return;
        }

        float alpha = Mathf.Lerp(0.25f, 0.95f, Mathf.PingPong(Time.unscaledTime * 1.6f, 1f));
        style.fontSize = Mathf.RoundToInt(21f * s);
        style.normal.textColor = new Color(UiTheme.SkyAccent.r, UiTheme.SkyAccent.g, UiTheme.SkyAccent.b, alpha);
        GUI.Label(rect, "▼", style);
    }

    /// <summary>화면 아무 곳이나 눌러 진행. 다른 버튼/패널이 먼저 이벤트를 소비하도록 마지막에 그린다.</summary>
    private void DrawTapCatcher(float screenW, float screenH, float s, bool hasChoices, bool complete)
    {
        if (hasChoices && complete)
        {
            return;
        }

        if (quickPanel != QuickPanel.None)
        {
            return;
        }

        Rect zone = new Rect(0f, 64f * s, screenW, screenH - 64f * s);
        if (GUI.Button(zone, GUIContent.none, GUIStyle.none))
        {
            ActivatePrimary();
        }
    }

    /// <summary>우상단 반투명 칩 바(LOG/SAVE/LOAD/AUTO/SKIP) — 모바일 미연시식 배치.</summary>
    private void DrawQuickBar(float screenW, float s, GameSettings settings)
    {
        float gap = Mathf.Max(5f, 8f * s);
        float bw = Mathf.Max(56f, 72f * s);
        float bh = Mathf.Max(26f, 34f * s);
        float total = bw * 5f + gap * 4f;
        if (total > screenW - 24f)
        {
            bw = Mathf.Max(44f, (screenW - 24f - gap * 4f) / 5f);
            total = bw * 5f + gap * 4f;
        }

        Rect bar = new Rect(Mathf.Max(12f, screenW - 22f * s - total), 16f * s, total, bh);
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
        bool hover = Event.current != null && rect.Contains(Event.current.mousePosition);
        Color bg = active ? new Color(0.110f, 0.360f, 0.560f, 0.94f)
                          : new Color(0.018f, 0.050f, 0.092f, hover ? 0.88f : 0.62f);
        UiTheme.DrawFill(rect, bg);
        UiTheme.DrawFill(new Rect(rect.x, rect.yMax - 2f, rect.width, 2f),
                         active ? UiTheme.SkyAccent : new Color(1f, 1f, 1f, hover ? 0.30f : 0.14f));

        GUIStyle style = new GUIStyle(UiTheme.Small)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            fontSize = Mathf.Max(11, Mathf.RoundToInt(14f * s))
        };
        style.normal.textColor = active ? Color.white : new Color(0.80f, 0.87f, 0.94f, 1f);
        GUI.Label(rect, label, style);
        return GUI.Button(rect, GUIContent.none, GUIStyle.none);
    }

    private void DrawQuickPanel(float screenW, float screenH, float s)
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
        Rect panel = new Rect(screenW - width - 22f * s, 60f * s, width, height);
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

    private void DrawQuickMessage(float screenW, float s)
    {
        if (quickMessageTimer <= 0f || string.IsNullOrEmpty(quickMessage))
        {
            return;
        }

        float w = Mathf.Min(340f * s, screenW * 0.42f);
        Rect toast = new Rect((screenW - w) * 0.5f, 64f * s, w, 38f * s);
        UiTheme.DrawFill(toast, new Color(0.018f, 0.050f, 0.092f, 0.88f));
        UiTheme.DrawFill(new Rect(toast.x, toast.yMax - 2f, toast.width, 2f),
                         new Color(UiTheme.SkyAccent.r, UiTheme.SkyAccent.g, UiTheme.SkyAccent.b, 0.85f));
        GUIStyle style = new GUIStyle(UiTheme.Small) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        style.normal.textColor = Color.white;
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

    private static GUIStyle DialogueBodyStyle(float s)
    {
        GUIStyle style =
            new GUIStyle(UiTheme.Body) { fontSize = Mathf.RoundToInt(23f * s), alignment = TextAnchor.UpperLeft,
                                         wordWrap = true, richText = true };
        style.normal.textColor = new Color(0.965f, 0.978f, 1f, 1f);
        return style;
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
