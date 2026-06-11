using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace JoseonMurimTactics
{
[DisallowMultipleComponent]
public sealed class BattleHUDController : MonoBehaviour
{
    private static readonly Color InkPanel = new Color(0.027f, 0.063f, 0.094f, 0.72f);
    private static readonly Color InkPanelStrong = new Color(0.020f, 0.033f, 0.045f, 0.84f);
    private static readonly Color InkPanelSoft = new Color(0.070f, 0.090f, 0.090f, 0.58f);
    private static readonly Color SnowGlass = new Color(0.86f, 0.93f, 0.98f, 0.10f);
    private static readonly Color Ink = new Color(0.945f, 0.905f, 0.785f, 1f);
    private static readonly Color Muted = new Color(0.680f, 0.720f, 0.700f, 0.86f);
    private static readonly Color Gold = new Color(0.850f, 0.650f, 0.260f, 0.96f);
    private static readonly Color Cyan = new Color(0.230f, 0.720f, 0.910f, 0.95f);
    private static readonly Color AllyGreen = new Color(0.480f, 0.880f, 0.420f, 0.95f);
    private static readonly Color EnemyRed = new Color(0.910f, 0.360f, 0.360f, 0.96f);
    private static readonly Color ButtonBg = new Color(0.045f, 0.075f, 0.092f, 0.86f);
    private static readonly Color Disabled = new Color(0.050f, 0.058f, 0.060f, 0.46f);

    private const int MaxRosterSlots = 6;
    private const float ReferenceWidth = 1920f;
    private const float ReferenceHeight = 1080f;

    private BattleTestController owner;
    private Canvas canvas;
    private Font koreanFont;

    private Text phaseTitle;
    private Text phaseInstruction;

    private RectTransform objectivePanel;
    private Text objectiveText;
    private Text objectiveMiniLabel;
    private float objectiveIntroUntil;

    private RectTransform selectedUnitCard;
    private RectTransform selectedPromptCard;
    private Text selectedPortraitText;
    private Text selectedNameText;
    private Text selectedSectText;
    private Text selectedMoveText;
    private Text selectedStatusText;
    private Image selectedHpFill;
    private Image selectedInnerFill;

    private RectTransform commandPanel;
    private readonly List<Button> commandButtons = new List<Button>();
    private readonly List<Text> commandLabels = new List<Text>();

    private RectTransform rosterPanel;
    private readonly List<RosterSlot> rosterSlots = new List<RosterSlot>();

    private RectTransform forecastPanel;
    private Text forecastTitle;
    private Text forecastLeft;
    private Text forecastCenter;
    private Text forecastRight;

    private RectTransform hoverPanel;
    private Text hoverTitle;
    private Text hoverBody;

    private RectTransform logToastPanel;
    private Text logToastText;
    private RectTransform logPanel;
    private Text logText;
    private Text logMiniLabel;
    private string lastLogLine;
    private float logToastUntil;

    private RectTransform helpPanel;
    private Text helpText;
    private bool helpVisible;

    private RectTransform dicePopupPanel;
    private Text dicePopupText;

    public void Initialize(BattleTestController controller)
    {
        owner = controller;
        EnsureEventSystem();
        UiTheme.EnsureStyles();
        koreanFont = UiTheme.Font;
        if (koreanFont == null)
        {
            koreanFont = Font.CreateDynamicFontFromOSFont(
                new[] { "Malgun Gothic", "맑은 고딕", "Noto Sans CJK KR", "Nanum Gothic", "Gulim" }, 18);
        }

        objectiveIntroUntil = Time.time + 3f;
        Build();
    }

    private void Update()
    {
        if (canvas == null)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            owner.HudToggleLog();
        }

        if (Input.GetKeyDown(KeyCode.F1) ||
            (Input.GetKeyDown(KeyCode.Slash) && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))))
        {
            helpVisible = !helpVisible;
        }

        helpPanel.gameObject.SetActive(helpVisible);
        UpdateTransientPanels();
    }

    public void Refresh(BattleHudSnapshot snapshot)
    {
        if (canvas == null || snapshot == null)
        {
            return;
        }

        phaseTitle.text = PhaseText(snapshot.phase, snapshot.battleOver) + " · " + snapshot.round + "턴";
        phaseInstruction.text = CompactInstruction(snapshot);

        bool objectiveExpanded = Time.time < objectiveIntroUntil || snapshot.showObjectiveOverlay;
        objectivePanel.gameObject.SetActive(objectiveExpanded);
        objectiveMiniLabel.text = objectiveExpanded ? "목표 닫기" : "목표 O";
        objectiveText.text = CompactObjective(snapshot.objectiveText);

        UpdateSelectedUnit(snapshot);
        UpdateCommands(snapshot);
        UpdateForecast(snapshot);
        UpdateRoster(snapshot);
        UpdateLog(snapshot);
        UpdateTooltip(snapshot);
        UpdateDicePopup(snapshot);
        UpdateTransientPanels();
    }

    public bool PointerOverHud(Vector3 screenPosition)
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        return results.Count > 0;
    }

    private void Build()
    {
        GameObject canvasObject = new GameObject("BattleHUD_Canvas");
        canvasObject.transform.SetParent(transform, false);
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10000;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();
        RectTransform root = canvasObject.GetComponent<RectTransform>();

        RectTransform phasePanel = Panel("TopPhaseRibbon", root, TopCenter(), new Vector2(520f, 56f),
                                         new Vector2(0f, -18f), InkPanelStrong, "ui_battle_panel_phase_ribbon_9slice");
        phaseTitle = MakeText("PhaseTitleText", phasePanel, StretchMin(), StretchMax(),
                              new Vector2(20f, 22f), new Vector2(-20f, -6f), 23, FontStyle.Bold,
                              TextAnchor.MiddleCenter);
        phaseInstruction = MakeText("PhaseInstructionText", phasePanel, StretchMin(), StretchMax(),
                                    new Vector2(20f, 5f), new Vector2(-20f, -30f), 13, FontStyle.Normal,
                                    TextAnchor.MiddleCenter);

        RectTransform objectiveButton = MakeButton("ObjectiveMiniButton", root, TopLeft(), new Vector2(96f, 34f),
                                                   new Vector2(24f, -24f), () => owner.HudToggleObjective(),
                                                   out objectiveMiniLabel);
        objectiveMiniLabel.fontSize = 13;
        objectiveMiniLabel.text = "목표 O";

        objectivePanel = Panel("ObjectiveExpandedPanel", root, TopLeft(), new Vector2(340f, 150f),
                               new Vector2(24f, -64f), InkPanelStrong, "ui_battle_panel_ink_glass_9slice");
        objectiveText = MakeText("ObjectiveText", objectivePanel, StretchMin(), StretchMax(),
                                 new Vector2(16f, 12f), new Vector2(-16f, -12f), 13, FontStyle.Bold,
                                 TextAnchor.UpperLeft);

        RectTransform helpButton = MakeButton("HelpMiniButton", root, TopRight(), new Vector2(94f, 34f),
                                              new Vector2(-24f, -24f), () => helpVisible = !helpVisible,
                                              out Text helpMiniLabel);
        helpMiniLabel.fontSize = 13;
        helpMiniLabel.text = "F1 도움말";

        helpPanel = Panel("HelpOverlayPanel", root, TopRight(), new Vector2(320f, 240f),
                          new Vector2(-24f, -66f), InkPanelStrong, "ui_battle_panel_log_9slice");
        helpText = MakeText("HelpText", helpPanel, StretchMin(), StretchMax(), new Vector2(16f, 14f),
                            new Vector2(-16f, -14f), 13, FontStyle.Bold, TextAnchor.UpperLeft);
        helpText.text =
            "전투 단축키\n" +
            "1 이동   2 공격   3 무공\n" +
            "4 방어   5 지형   Space 대기\n" +
            "Tab 위협   H 고저   C 엄폐   V 시야\n" +
            "O 목표   L 로그   S 정찰\n" +
            "Alt 지형 이름   Esc 이동 모드";
        helpPanel.gameObject.SetActive(false);

        selectedPromptCard = Panel("SelectedPromptCard", root, BottomLeft(), new Vector2(230f, 42f),
                                   new Vector2(24f, 24f), InkPanel, "ui_battle_panel_ink_glass_9slice");
        MakeText("SelectedPromptText", selectedPromptCard, StretchMin(), StretchMax(),
                 new Vector2(14f, 0f), new Vector2(-14f, 0f), 14, FontStyle.Bold,
                 TextAnchor.MiddleCenter).text = "행동할 아군 선택";

        selectedUnitCard = Panel("SelectedUnitCard", root, BottomLeft(), new Vector2(360f, 96f),
                                 new Vector2(24f, 24f), InkPanel, "ui_battle_panel_ink_glass_9slice");
        RectTransform portrait = Panel("PortraitFrame", selectedUnitCard, TopLeft(), new Vector2(64f, 64f),
                                       new Vector2(14f, -16f), new Color(0.06f, 0.12f, 0.13f, 0.74f),
                                       "ui_battle_panel_ink_glass_9slice");
        selectedPortraitText = MakeText("PortraitGlyph", portrait, StretchMin(), StretchMax(),
                                        Vector2.zero, Vector2.zero, 24, FontStyle.Bold, TextAnchor.MiddleCenter);
        selectedNameText = MakeText("SelectedNameText", selectedUnitCard, TopLeft(), TopRight(),
                                    new Vector2(90f, -36f), new Vector2(-16f, -12f), 17, FontStyle.Bold,
                                    TextAnchor.MiddleLeft);
        selectedSectText = MakeText("SelectedSectText", selectedUnitCard, TopLeft(), TopRight(),
                                    new Vector2(90f, -56f), new Vector2(-16f, -36f), 12, FontStyle.Normal,
                                    TextAnchor.MiddleLeft);
        selectedHpFill = Gauge("SelectedHpGauge", selectedUnitCard, new Vector2(90f, -62f), new Vector2(210f, 8f),
                               AllyGreen);
        selectedInnerFill = Gauge("SelectedInnerGauge", selectedUnitCard, new Vector2(90f, -76f), new Vector2(210f, 8f),
                                  Cyan);
        selectedMoveText = MakeText("SelectedMoveText", selectedUnitCard, TopLeft(), TopRight(),
                                    new Vector2(90f, -94f), new Vector2(-16f, -76f), 12, FontStyle.Bold,
                                    TextAnchor.MiddleLeft);
        selectedStatusText = MakeText("SelectedStatusText", selectedUnitCard, BottomLeft(), BottomRight(),
                                      new Vector2(14f, 4f), new Vector2(-14f, 22f), 11, FontStyle.Normal,
                                      TextAnchor.MiddleLeft);

        rosterPanel = Panel("RosterStrip", root, BottomCenter(), new Vector2(680f, 72f), new Vector2(0f, 18f),
                            InkPanel, "ui_battle_panel_ink_glass_9slice");

        commandPanel = Panel("CommandRibbon", root, BottomRight(), new Vector2(420f, 92f), new Vector2(-24f, 24f),
                             InkPanel, "ui_battle_panel_ink_glass_9slice");
        BuildCommandButtons();

        forecastPanel = Panel("ForecastCard", root, BottomCenter(), new Vector2(620f, 118f),
                              new Vector2(0f, 112f), InkPanelStrong, "ui_battle_panel_forecast_9slice");
        forecastTitle = MakeText("ForecastTitle", forecastPanel, TopLeft(), TopRight(),
                                 new Vector2(18f, -30f), new Vector2(-18f, -8f), 15, FontStyle.Bold,
                                 TextAnchor.MiddleCenter);
        forecastLeft = MakeText("ForecastAttacker", forecastPanel, new Vector2(0f, 0f), new Vector2(0.32f, 1f),
                                new Vector2(18f, 12f), new Vector2(-8f, -38f), 13, FontStyle.Normal,
                                TextAnchor.UpperLeft);
        forecastCenter = MakeText("ForecastResult", forecastPanel, new Vector2(0.32f, 0f), new Vector2(0.68f, 1f),
                                  new Vector2(12f, 12f), new Vector2(-12f, -38f), 16, FontStyle.Bold,
                                  TextAnchor.UpperCenter);
        forecastRight = MakeText("ForecastTarget", forecastPanel, new Vector2(0.68f, 0f), new Vector2(1f, 1f),
                                 new Vector2(8f, 12f), new Vector2(-18f, -38f), 13, FontStyle.Normal,
                                 TextAnchor.UpperRight);

        hoverPanel = Panel("HoverTooltip", root, BottomLeft(), new Vector2(300f, 96f), Vector2.zero,
                           new Color(0.020f, 0.040f, 0.050f, 0.80f), "ui_battle_panel_ink_glass_9slice");
        hoverPanel.pivot = new Vector2(0f, 1f);
        hoverTitle = MakeText("HoverTitle", hoverPanel, TopLeft(), TopRight(),
                              new Vector2(14f, -30f), new Vector2(-14f, -8f), 14, FontStyle.Bold,
                              TextAnchor.MiddleLeft);
        hoverBody = MakeText("HoverBody", hoverPanel, StretchMin(), StretchMax(), new Vector2(14f, 10f),
                             new Vector2(-14f, -34f), 12, FontStyle.Normal, TextAnchor.UpperLeft);

        MakeButton("LogMiniButton", root, BottomRight(), new Vector2(84f, 30f), new Vector2(-24f, 84f),
                   () => owner.HudToggleLog(), out logMiniLabel).gameObject.SetActive(true);
        logMiniLabel.fontSize = 12;
        logMiniLabel.text = "기록 L";

        logToastPanel = Panel("LogToast", root, BottomRight(), new Vector2(360f, 44f), new Vector2(-24f, 132f),
                              InkPanelStrong, "ui_battle_panel_log_9slice");
        logToastText = MakeText("LogToastText", logToastPanel, StretchMin(), StretchMax(),
                                new Vector2(14f, 0f), new Vector2(-14f, 0f), 13, FontStyle.Bold,
                                TextAnchor.MiddleLeft);

        logPanel = Panel("ExpandedLogPanel", root, RightCenter(), new Vector2(360f, 360f), new Vector2(-24f, 0f),
                         InkPanelStrong, "ui_battle_panel_log_9slice");
        logText = MakeText("LogText", logPanel, StretchMin(), StretchMax(), new Vector2(16f, 16f),
                           new Vector2(-16f, -16f), 13, FontStyle.Normal, TextAnchor.UpperLeft);

        dicePopupPanel = Panel("BattleNoticeToast", root, Center(), new Vector2(420f, 104f), new Vector2(0f, 122f),
                               new Color(0.04f, 0.25f, 0.34f, 0.92f), "ui_battle_panel_forecast_9slice");
        dicePopupText = MakeText("BattleNoticeText", dicePopupPanel, StretchMin(), StretchMax(),
                                 new Vector2(16f, 8f), new Vector2(-16f, -8f), 22, FontStyle.Bold,
                                 TextAnchor.MiddleCenter);

        selectedUnitCard.gameObject.SetActive(false);
        forecastPanel.gameObject.SetActive(false);
        hoverPanel.gameObject.SetActive(false);
        logToastPanel.gameObject.SetActive(false);
        logPanel.gameObject.SetActive(false);
        dicePopupPanel.gameObject.SetActive(false);
    }

    private void BuildCommandButtons()
    {
        AddCommandButton(0, "行\n이동", () => owner.HudSetCommand(BattleCommandMode.Move));
        AddCommandButton(1, "攻\n공격", () => owner.HudSetCommand(BattleCommandMode.Attack));
        AddCommandButton(2, "武\n무공", () => owner.HudSetCommand(BattleCommandMode.Skill));
        AddCommandButton(3, "守\n방어", () => owner.HudGuard());
        AddCommandButton(4, "地\n지형", () => owner.HudSetCommand(BattleCommandMode.Interact));
        AddCommandButton(5, "待\n대기", () => owner.HudWait());
    }

    private void AddCommandButton(int index, string label, Action action)
    {
        int column = index % 3;
        int row = index / 3;
        RectTransform buttonRect = MakeButton("CommandButton_" + index, commandPanel, TopLeft(),
                                              new Vector2(120f, 32f),
                                              new Vector2(20f + column * 130f, -18f - row * 40f), action,
                                              out Text text);
        text.text = label;
        text.fontSize = 13;
        commandButtons.Add(buttonRect.GetComponent<Button>());
        commandLabels.Add(text);
    }

    private void UpdateSelectedUnit(BattleHudSnapshot snapshot)
    {
        BattleTestUnit unit = snapshot.activeUnit;
        selectedUnitCard.gameObject.SetActive(unit != null);
        selectedPromptCard.gameObject.SetActive(unit == null && snapshot.phase == BattlePhase.PlayerPhase &&
                                                !snapshot.battleOver);
        if (unit == null)
        {
            return;
        }

        selectedPortraitText.text = FirstGlyph(unit.definition.displayName);
        selectedNameText.text = unit.definition.displayName;
        selectedSectText.text = unit.definition.sectName;
        selectedMoveText.text = $"이동 {unit.actions.movementLeft} · 내공 {unit.inner}/{unit.definition.maxInner}";
        selectedStatusText.text = snapshot.unitStatuses.TryGetValue(unit, out string status) ? TrimStatus(status) : string.Empty;
        SetGauge(selectedHpFill, unit.hp, unit.definition.maxHp);
        SetGauge(selectedInnerFill, unit.inner, unit.definition.maxInner);
    }

    private void UpdateCommands(BattleHudSnapshot snapshot)
    {
        bool playerUnitReady = snapshot.phase == BattlePhase.PlayerPhase && snapshot.activeUnit != null &&
                               !snapshot.battleOver;
        bool show = snapshot.scoutMode || playerUnitReady;
        commandPanel.gameObject.SetActive(show);
        if (!show)
        {
            return;
        }

        if (snapshot.scoutMode)
        {
            SetCommand(0, "行\n배치", false, false);
            SetCommand(1, "攻\n정찰", false, false);
            SetCommand(2, "武\n확인", false, false);
            SetCommand(3, "守\n대기", false, false);
            SetCommand(4, "地\n지형", false, false);
            SetCommand(5, "待\n시작", snapshot.canWait, true);
            return;
        }

        SetCommand(0, "行\n이동", snapshot.canMove, snapshot.commandMode == BattleCommandMode.Move);
        SetCommand(1, "攻\n공격", snapshot.canAttack, snapshot.commandMode == BattleCommandMode.Attack);
        SetCommand(2, "武\n무공", snapshot.canSkill, snapshot.commandMode == BattleCommandMode.Skill);
        SetCommand(3, "守\n방어", snapshot.canGuard, false);
        SetCommand(4, "地\n지형", snapshot.canTerrain, snapshot.commandMode == BattleCommandMode.Interact);
        SetCommand(5, "待\n대기", snapshot.canWait, false);
    }

    private void SetCommand(int index, string label, bool enabled, bool active)
    {
        Button button = commandButtons[index];
        Image image = button.GetComponent<Image>();
        button.interactable = enabled;
        bool activeEnabled = active && enabled;
        image.color = activeEnabled ? Gold : enabled ? ButtonBg : Disabled;
        commandLabels[index].text = label;
        commandLabels[index].color = enabled ? activeEnabled ? new Color(0.020f, 0.030f, 0.034f, 1f) : Ink : Muted;
    }

    private void UpdateForecast(BattleHudSnapshot snapshot)
    {
        bool attackMode = snapshot.commandMode == BattleCommandMode.Attack ||
                          snapshot.commandMode == BattleCommandMode.Skill;
        bool hasTargetContext = !string.IsNullOrWhiteSpace(snapshot.forecastLeft) ||
                                !string.IsNullOrWhiteSpace(snapshot.forecastRight);
        bool show = snapshot.hasForecast && attackMode && hasTargetContext && !snapshot.battleOver;
        forecastPanel.gameObject.SetActive(show);
        if (!show)
        {
            return;
        }

        forecastTitle.text = string.IsNullOrEmpty(snapshot.forecastTitle) ? "전투 예측" : snapshot.forecastTitle;
        forecastLeft.text = snapshot.forecastLeft;
        forecastCenter.text = EmphasizeForecastCenter(snapshot.forecastCenter);
        forecastRight.text = snapshot.forecastRight;
    }

    private void UpdateRoster(BattleHudSnapshot snapshot)
    {
        int visibleCount = Mathf.Min(MaxRosterSlots, snapshot.allies.Count);
        while (rosterSlots.Count < visibleCount)
        {
            rosterSlots.Add(CreateRosterSlot(rosterSlots.Count));
        }

        for (int i = 0; i < rosterSlots.Count; i++)
        {
            RosterSlot slot = rosterSlots[i];
            bool active = i < visibleCount;
            slot.root.gameObject.SetActive(active);
            if (!active)
            {
                continue;
            }

            BattleTestUnit unit = snapshot.allies[i];
            bool isActiveUnit = unit == snapshot.activeUnit;
            slot.name.text = ShortName(unit.definition.displayName);
            slot.detail.text = unit.acted ? "완" : unit.defeated ? "패" : snapshot.selectableUnits.Contains(unit) ? "선" : "-";
            slot.badge.text = unit.acted ? "完" : unit.defeated ? "敗" : string.Empty;
            slot.badge.gameObject.SetActive(unit.acted || unit.defeated);
            slot.name.color = unit.defeated ? Muted : isActiveUnit ? Gold : Ink;
            slot.detail.color = unit.defeated ? Muted : isActiveUnit ? Gold : Cyan;
            slot.button.interactable = snapshot.selectableUnits.Contains(unit);
            slot.background.color = isActiveUnit
                                        ? new Color(Gold.r, Gold.g, Gold.b, 0.72f)
                                        : unit.acted
                                            ? new Color(0.085f, 0.080f, 0.070f, 0.72f)
                                            : ButtonBg;
            SetGauge(slot.hpFill, unit.hp, unit.definition.maxHp);
            SetGauge(slot.innerFill, unit.inner, unit.definition.maxInner);

            BattleTestUnit captured = unit;
            slot.button.onClick.RemoveAllListeners();
            slot.button.onClick.AddListener(() => owner.HudSelectUnit(captured));
        }
    }

    private RosterSlot CreateRosterSlot(int index)
    {
        float x = 20f + index * 108f;
        RectTransform buttonRect = MakeButton("RosterSlot_" + index, rosterPanel, TopLeft(), new Vector2(98f, 54f),
                                              new Vector2(x, -10f), null, out Text label);
        label.gameObject.SetActive(false);
        Button button = buttonRect.GetComponent<Button>();
        Image background = buttonRect.GetComponent<Image>();
        Text name = MakeText("RosterName_" + index, buttonRect, TopLeft(), TopRight(),
                             new Vector2(8f, -22f), new Vector2(-8f, -4f), 13, FontStyle.Bold,
                             TextAnchor.MiddleLeft);
        Text detail = MakeText("RosterDetail_" + index, buttonRect, TopLeft(), TopRight(),
                               new Vector2(62f, -22f), new Vector2(-8f, -4f), 12, FontStyle.Bold,
                               TextAnchor.MiddleRight);
        Image hpFill = Gauge("RosterHp_" + index, buttonRect, new Vector2(8f, -29f), new Vector2(82f, 6f), AllyGreen);
        Image innerFill = Gauge("RosterInner_" + index, buttonRect, new Vector2(8f, -40f), new Vector2(82f, 5f), Cyan);
        Text badge = MakeText("RosterBadge_" + index, buttonRect, StretchMin(), StretchMax(), Vector2.zero, Vector2.zero,
                              18, FontStyle.Bold, TextAnchor.MiddleCenter);
        badge.color = new Color(0.08f, 0.07f, 0.04f, 0.92f);
        badge.gameObject.SetActive(false);
        return new RosterSlot(buttonRect, button, background, name, detail, badge, hpFill, innerFill);
    }

    private void UpdateLog(BattleHudSnapshot snapshot)
    {
        string latest = snapshot.logs.Count > 0 ? snapshot.logs[snapshot.logs.Count - 1] : string.Empty;
        if (!string.IsNullOrEmpty(latest) && latest != lastLogLine)
        {
            lastLogLine = latest;
            logToastUntil = Time.time + 2f;
        }

        logPanel.gameObject.SetActive(snapshot.showLog);
        logMiniLabel.text = snapshot.showLog ? "닫기 L" : "기록 L";
        if (snapshot.showLog)
        {
            int start = Mathf.Max(0, snapshot.logs.Count - 12);
            List<string> lines = new List<string>();
            for (int i = start; i < snapshot.logs.Count; i++)
            {
                lines.Add(snapshot.logs[i]);
            }

            logText.text = "전투 로그\n" + string.Join("\n", lines);
        }
    }

    private void UpdateTooltip(BattleHudSnapshot snapshot)
    {
        bool hasHoverText = !string.IsNullOrWhiteSpace(snapshot.hoverTitle) ||
                            !string.IsNullOrWhiteSpace(snapshot.hoverBody);
        hoverPanel.gameObject.SetActive(hasHoverText);
        if (!hasHoverText)
        {
            return;
        }

        hoverTitle.text = snapshot.hoverTitle;
        hoverBody.text = ShortTooltipBody(snapshot.hoverBody);
        Vector2 size = new Vector2(300f, string.IsNullOrWhiteSpace(snapshot.hoverBody) ? 58f : 96f);
        hoverPanel.sizeDelta = size;
        hoverPanel.anchoredPosition = ClampTooltipPosition(Input.mousePosition, size);
    }

    private void UpdateDicePopup(BattleHudSnapshot snapshot)
    {
        bool visible = !string.IsNullOrEmpty(snapshot.noticeText);
        dicePopupPanel.gameObject.SetActive(visible);
        if (visible)
        {
            dicePopupText.text = snapshot.noticeText;
        }
    }

    private void UpdateTransientPanels()
    {
        if (logToastPanel == null)
        {
            return;
        }

        bool showToast = !logPanel.gameObject.activeSelf && !string.IsNullOrEmpty(lastLogLine) &&
                         Time.time < logToastUntil;
        logToastPanel.gameObject.SetActive(showToast);
        if (showToast)
        {
            logToastText.text = lastLogLine;
        }
    }

    private RectTransform Panel(string name, Transform parent, Vector2 anchor, Vector2 size, Vector2 anchoredPosition,
                                Color fallbackColor, string spriteName)
    {
        GameObject panelObject = new GameObject(name);
        panelObject.transform.SetParent(parent, false);
        RectTransform rect = panelObject.AddComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = PivotForAnchor(anchor);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;

        Image image = panelObject.AddComponent<Image>();
        ApplySpriteOrColor(image, spriteName, fallbackColor);
        image.raycastTarget = false;

        Outline outline = panelObject.AddComponent<Outline>();
        outline.effectColor = new Color(Gold.r, Gold.g, Gold.b, 0.20f);
        outline.effectDistance = new Vector2(1f, -1f);
        return rect;
    }

    private Text MakeText(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin,
                          Vector2 offsetMax, int size, FontStyle style, TextAnchor alignment)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        Text text = textObject.AddComponent<Text>();
        text.font = koreanFont;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = Ink;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;
        text.supportRichText = true;
        return text;
    }

    private RectTransform MakeButton(string name, Transform parent, Vector2 anchor, Vector2 size,
                                     Vector2 anchoredPosition, Action action, out Text label)
    {
        GameObject buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = PivotForAnchor(anchor);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;

        Image image = buttonObject.AddComponent<Image>();
        ApplySpriteOrColor(image, "ui_battle_button_normal_9slice", ButtonBg);
        image.raycastTarget = true;
        Button button = buttonObject.AddComponent<Button>();
        Sprite hoverSprite = BattleHudAssetRegistry.LoadSprite("ui_button_hover");
        Sprite pressedSprite = BattleHudAssetRegistry.LoadSprite("ui_button_pressed");
        Sprite disabledSprite = BattleHudAssetRegistry.LoadSprite("ui_button_disabled");
        if (hoverSprite != null || pressedSprite != null || disabledSprite != null)
        {
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState
            {
                highlightedSprite = hoverSprite,
                pressedSprite = pressedSprite,
                selectedSprite = hoverSprite,
                disabledSprite = disabledSprite
            };
        }
        else
        {
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(Cyan.r, Cyan.g, Cyan.b, 0.82f);
            colors.pressedColor = new Color(Gold.r, Gold.g, Gold.b, 0.92f);
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.54f);
            button.colors = colors;
        }
        if (action != null)
        {
            button.onClick.AddListener(() => action());
        }

        label = MakeText(name + "_Label", rect, StretchMin(), StretchMax(), Vector2.zero, Vector2.zero, 13,
                         FontStyle.Bold, TextAnchor.MiddleCenter);
        return rect;
    }

    private Image Gauge(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, Color fillColor)
    {
        RectTransform bg = Panel(name + "_Bg", parent, TopLeft(), size, anchoredPosition,
                                 new Color(0.010f, 0.018f, 0.020f, 0.82f), string.Empty);
        Outline outline = bg.GetComponent<Outline>();
        if (outline != null)
        {
            Destroy(outline);
        }

        GameObject fillObject = new GameObject(name + "_Fill");
        fillObject.transform.SetParent(bg, false);
        RectTransform fillRect = fillObject.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        Image fill = fillObject.AddComponent<Image>();
        fill.color = fillColor;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.raycastTarget = false;
        return fill;
    }

    private static void SetGauge(Image fill, int current, int max)
    {
        if (fill == null)
        {
            return;
        }

        fill.fillAmount = max <= 0 ? 0f : Mathf.Clamp01(current / (float)max);
    }

    private static void ApplySpriteOrColor(Image image, string spriteName, Color fallbackColor)
    {
        Sprite sprite = BattleHudAssetRegistry.LoadSprite(spriteName);
        if (sprite != null)
        {
            image.sprite = sprite;
            image.type = sprite.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple;
            image.color = Color.white;
        }
        else
        {
            image.color = fallbackColor;
        }
    }

    private Vector2 ClampTooltipPosition(Vector3 screenPosition, Vector2 size)
    {
        Vector2 reference = new Vector2(screenPosition.x * (ReferenceWidth / Mathf.Max(1f, Screen.width)),
                                        screenPosition.y * (ReferenceHeight / Mathf.Max(1f, Screen.height)));
        Vector2 pos = reference + new Vector2(18f, -18f);
        if (pos.x + size.x > ReferenceWidth - 18f)
        {
            pos.x = reference.x - size.x - 18f;
        }

        if (pos.y - size.y < 18f)
        {
            pos.y = reference.y + size.y + 18f;
        }

        pos.x = Mathf.Clamp(pos.x, 18f, ReferenceWidth - size.x - 18f);
        pos.y = Mathf.Clamp(pos.y, size.y + 18f, ReferenceHeight - 18f);
        return pos;
    }

    private static string CompactInstruction(BattleHudSnapshot snapshot)
    {
        if (snapshot.battleOver)
        {
            return "전투 결과를 확인하세요.";
        }

        if (snapshot.phase == BattlePhase.EnemyPhase)
        {
            return "적군이 행동 중입니다.";
        }

        if (snapshot.scoutMode)
        {
            return "배치와 지형을 정찰하세요.";
        }

        if (snapshot.activeUnit == null)
        {
            return "행동할 아군을 선택하세요.";
        }

        switch (snapshot.commandMode)
        {
        case BattleCommandMode.Attack:
            return "공격 대상을 선택하세요.";
        case BattleCommandMode.Skill:
            return "무공 대상을 선택하세요.";
        case BattleCommandMode.Interact:
            return "활용할 지형을 선택하세요.";
        default:
            return "이동할 칸을 선택하세요.";
        }
    }

    private static string CompactObjective(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "주 목표를 확인 중입니다.";
        }

        return value.Replace("단축: S 정찰 / Tab 위협 / H 고저 / C 엄폐 / V 시야 / O 목표", "단축키는 F1 도움말에서 확인");
    }

    private static string ShortTooltipBody(string body)
    {
        if (string.IsNullOrEmpty(body))
        {
            return string.Empty;
        }

        string[] lines = body.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        int count = Mathf.Min(lines.Length, Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? 4 : 3);
        List<string> compact = new List<string>();
        for (int i = 0; i < count; i++)
        {
            compact.Add(lines[i]);
        }

        return string.Join("\n", compact);
    }

    private static string EmphasizeForecastCenter(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.Replace("명중 ", "명중\n").Replace("거리 ", "\n거리 ");
    }

    private static string TrimStatus(string status)
    {
        if (string.IsNullOrEmpty(status))
        {
            return "상태 이상 없음";
        }

        return status.Length > 34 ? status.Substring(0, 34) : status;
    }

    private static string ShortName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return "?";
        }

        string trimmed = name.Replace(" ", string.Empty);
        return trimmed.Length <= 2 ? trimmed : trimmed.Substring(0, 2);
    }

    private static string FirstGlyph(string name)
    {
        return string.IsNullOrEmpty(name) ? "?" : name.Substring(0, 1);
    }

    private static Vector2 PivotForAnchor(Vector2 anchor)
    {
        return new Vector2(anchor.x, anchor.y);
    }

    private static Vector2 Center()
    {
        return new Vector2(0.5f, 0.5f);
    }

    private static Vector2 TopLeft()
    {
        return new Vector2(0f, 1f);
    }

    private static Vector2 TopCenter()
    {
        return new Vector2(0.5f, 1f);
    }

    private static Vector2 TopRight()
    {
        return new Vector2(1f, 1f);
    }

    private static Vector2 RightCenter()
    {
        return new Vector2(1f, 0.5f);
    }

    private static Vector2 BottomLeft()
    {
        return new Vector2(0f, 0f);
    }

    private static Vector2 BottomCenter()
    {
        return new Vector2(0.5f, 0f);
    }

    private static Vector2 BottomRight()
    {
        return new Vector2(1f, 0f);
    }

    private static Vector2 StretchMin()
    {
        return Vector2.zero;
    }

    private static Vector2 StretchMax()
    {
        return Vector2.one;
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    private static string PhaseText(BattlePhase phase, bool battleOver)
    {
        if (battleOver)
        {
            return "전투 종료";
        }

        switch (phase)
        {
        case BattlePhase.EnemyPhase:
            return "적군 페이즈";
        case BattlePhase.NeutralPhase:
            return "중립 페이즈";
        default:
            return "아군 페이즈";
        }
    }

    private sealed class RosterSlot
    {
        public readonly RectTransform root;
        public readonly Button button;
        public readonly Image background;
        public readonly Text name;
        public readonly Text detail;
        public readonly Text badge;
        public readonly Image hpFill;
        public readonly Image innerFill;

        public RosterSlot(RectTransform root, Button button, Image background, Text name, Text detail, Text badge,
                          Image hpFill, Image innerFill)
        {
            this.root = root;
            this.button = button;
            this.background = background;
            this.name = name;
            this.detail = detail;
            this.badge = badge;
            this.hpFill = hpFill;
            this.innerFill = innerFill;
        }
    }
}

public sealed class BattleHudSnapshot
{
    public BattlePhase phase;
    public int round;
    public bool battleOver;
    public bool scoutMode;
    public string instruction;
    public string objectiveText;
    public string unitInfoText;
    public string hoverTitle;
    public string hoverBody;
    public string forecastTitle;
    public string forecastLeft;
    public string forecastCenter;
    public string forecastRight;
    public bool hasForecast;
    public bool showLog;
    public bool showThreatRange;
    public bool showElevationOverlay;
    public bool showCoverOverlay;
    public bool showSightOverlay;
    public bool showObjectiveOverlay;
    public bool canMove;
    public bool canAttack;
    public bool canSkill;
    public bool canGuard;
    public bool canTerrain;
    public bool canWait;
    public string noticeText;
    public BattleCommandMode commandMode;
    public BattleTestUnit activeUnit;
    public readonly List<BattleTestUnit> allies = new List<BattleTestUnit>();
    public readonly HashSet<BattleTestUnit> selectableUnits = new HashSet<BattleTestUnit>();
    public readonly Dictionary<BattleTestUnit, string> unitStatuses = new Dictionary<BattleTestUnit, string>();
    public readonly List<string> logs = new List<string>();
}
}
