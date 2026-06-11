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
    private static readonly Color Panel = new Color(0.025f, 0.031f, 0.037f, 0.82f);
    private static readonly Color PanelStrong = new Color(0.014f, 0.018f, 0.023f, 0.92f);
    private static readonly Color PanelSoft = new Color(0.050f, 0.060f, 0.066f, 0.70f);
    private static readonly Color Button = new Color(0.080f, 0.090f, 0.095f, 0.88f);
    private static readonly Color ButtonActive = new Color(0.185f, 0.143f, 0.073f, 0.96f);
    private static readonly Color ButtonDisabled = new Color(0.040f, 0.044f, 0.046f, 0.52f);
    private static readonly Color LineGold = new Color(0.760f, 0.575f, 0.250f, 0.96f);
    private static readonly Color TextMain = new Color(0.940f, 0.915f, 0.835f, 1f);
    private static readonly Color TextSub = new Color(0.690f, 0.735f, 0.735f, 0.92f);
    private static readonly Color TextDim = new Color(0.520f, 0.555f, 0.555f, 0.72f);
    private static readonly Color HpFill = new Color(0.690f, 0.180f, 0.160f, 0.96f);
    private static readonly Color InnerFill = new Color(0.170f, 0.590f, 0.680f, 0.96f);
    private static readonly Color GaugeBg = new Color(0.025f, 0.027f, 0.028f, 0.92f);

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
    private readonly List<CommandButtonView> commandViews = new List<CommandButtonView>();

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

    private RectTransform noticePanel;
    private Text noticeText;

    public void Initialize(BattleTestController controller)
    {
        owner = controller;
        EnsureEventSystem();
        UiTheme.EnsureStyles();
        koreanFont = CreateHudFont();
        objectiveIntroUntil = Time.time + 1.35f;
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

        phaseTitle.text = PhaseText(snapshot.phase, snapshot.battleOver) + "  |  " +
                          "\uB77C\uC6B4\uB4DC " + snapshot.round.ToString();
        phaseInstruction.text = CompactInstruction(snapshot);

        bool objectiveExpanded = Time.time < objectiveIntroUntil || snapshot.showObjectiveOverlay;
        objectivePanel.gameObject.SetActive(objectiveExpanded);
        objectiveMiniLabel.text = objectiveExpanded ? "\uBAA9\uD45C \uB2EB\uAE30" : "\uBAA9\uD45C O";
        objectiveText.text = CompactObjective(snapshot.objectiveText);

        UpdateSelectedUnit(snapshot);
        UpdateCommands(snapshot);
        UpdateForecast(snapshot);
        UpdateRoster(snapshot);
        UpdateLog(snapshot);
        UpdateTooltip(snapshot);
        UpdateNotice(snapshot);
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

        RectTransform phasePanel = PanelRect("TopPhaseRibbon", root, TopCenter(), new Vector2(560f, 58f),
                                             new Vector2(0f, -18f), PanelStrong, true);
        AddAccentLine("PhaseAccent", phasePanel, BottomLeft(), BottomRight(), new Vector2(16f, 6f),
                      new Vector2(-16f, 10f), LineGold);
        phaseTitle = MakeText("PhaseTitleText", phasePanel, StretchMin(), StretchMax(),
                              new Vector2(20f, 22f), new Vector2(-20f, -6f), 22, FontStyle.Bold,
                              TextAnchor.MiddleCenter, TextMain);
        phaseInstruction = MakeText("PhaseInstructionText", phasePanel, StretchMin(), StretchMax(),
                                    new Vector2(20f, 4f), new Vector2(-20f, -31f), 13, FontStyle.Bold,
                                    TextAnchor.MiddleCenter, TextSub);

        RectTransform objectiveButton = MakeButton("ObjectiveMiniButton", root, TopLeft(), new Vector2(108f, 34f),
                                                   new Vector2(24f, -24f), () => owner.HudToggleObjective(),
                                                   out objectiveMiniLabel);
        objectiveMiniLabel.fontSize = 13;
        objectiveMiniLabel.text = "\uBAA9\uD45C O";

        objectivePanel = PanelRect("ObjectiveExpandedPanel", root, TopLeft(), new Vector2(350f, 142f),
                                   new Vector2(24f, -66f), PanelStrong, true);
        objectiveText = MakeText("ObjectiveText", objectivePanel, StretchMin(), StretchMax(),
                                 new Vector2(16f, 12f), new Vector2(-16f, -12f), 14, FontStyle.Bold,
                                 TextAnchor.UpperLeft, TextMain);

        RectTransform helpButton = MakeButton("HelpMiniButton", root, TopRight(), new Vector2(104f, 34f),
                                              new Vector2(-24f, -24f), () => helpVisible = !helpVisible,
                                              out Text helpMiniLabel);
        helpMiniLabel.fontSize = 13;
        helpMiniLabel.text = "F1 \uB3C4\uC6C0";

        helpPanel = PanelRect("HelpOverlayPanel", root, TopRight(), new Vector2(332f, 230f),
                              new Vector2(-24f, -66f), PanelStrong, true);
        helpText = MakeText("HelpText", helpPanel, StretchMin(), StretchMax(), new Vector2(16f, 14f),
                            new Vector2(-16f, -14f), 13, FontStyle.Bold, TextAnchor.UpperLeft, TextMain);
        helpText.text =
            "\uC804\uD22C \uB3C4\uC6C0\uB9D0\n" +
            "1 \uC774\uB3D9   2 \uACF5\uACA9   3 \uBB34\uACF5\n" +
            "4 \uBC29\uC5B4   5 \uC9C0\uD615   Space \uB300\uAE30\n" +
            "Tab \uC804\uC220   H \uACE0\uC800   C \uC5C4\uD3D0   V \uC2DC\uC57C\n" +
            "O \uBAA9\uD45C   L \uAE30\uB85D   Esc \uCDE8\uC18C";
        helpPanel.gameObject.SetActive(false);

        selectedPromptCard = PanelRect("SelectedPromptCard", root, BottomLeft(), new Vector2(250f, 42f),
                                       new Vector2(24f, 24f), Panel, true);
        MakeText("SelectedPromptText", selectedPromptCard, StretchMin(), StretchMax(),
                 new Vector2(14f, 0f), new Vector2(-14f, 0f), 14, FontStyle.Bold,
                 TextAnchor.MiddleCenter, TextSub).text = "\uD589\uB3D9\uD560 \uC544\uAD70 \uC120\uD0DD";

        selectedUnitCard = PanelRect("SelectedUnitCard", root, BottomLeft(), new Vector2(378f, 108f),
                                     new Vector2(24f, 24f), PanelStrong, true);
        RectTransform portrait = PanelRect("PortraitFrame", selectedUnitCard, TopLeft(), new Vector2(66f, 66f),
                                           new Vector2(14f, -16f), PanelSoft, true);
        selectedPortraitText = MakeText("PortraitGlyph", portrait, StretchMin(), StretchMax(),
                                        Vector2.zero, Vector2.zero, 26, FontStyle.Bold, TextAnchor.MiddleCenter,
                                        LineGold);
        selectedNameText = MakeText("SelectedNameText", selectedUnitCard, TopLeft(), TopRight(),
                                    new Vector2(92f, -34f), new Vector2(-16f, -10f), 18, FontStyle.Bold,
                                    TextAnchor.MiddleLeft, TextMain);
        selectedSectText = MakeText("SelectedSectText", selectedUnitCard, TopLeft(), TopRight(),
                                    new Vector2(92f, -55f), new Vector2(-16f, -34f), 12, FontStyle.Bold,
                                    TextAnchor.MiddleLeft, TextSub);
        selectedHpFill = Gauge("SelectedHpGauge", selectedUnitCard, new Vector2(92f, -64f),
                               new Vector2(222f, 9f), HpFill);
        selectedInnerFill = Gauge("SelectedInnerGauge", selectedUnitCard, new Vector2(92f, -80f),
                                  new Vector2(222f, 9f), InnerFill);
        selectedMoveText = MakeText("SelectedMoveText", selectedUnitCard, TopLeft(), TopRight(),
                                    new Vector2(92f, -101f), new Vector2(-16f, -82f), 12, FontStyle.Bold,
                                    TextAnchor.MiddleLeft, TextSub);
        selectedStatusText = MakeText("SelectedStatusText", selectedUnitCard, BottomLeft(), BottomRight(),
                                      new Vector2(14f, 5f), new Vector2(-14f, 23f), 11, FontStyle.Bold,
                                      TextAnchor.MiddleLeft, TextDim);

        rosterPanel = PanelRect("RosterStrip", root, BottomCenter(), new Vector2(684f, 76f), new Vector2(0f, 18f),
                                Panel, true);

        commandPanel = PanelRect("CommandRibbon", root, BottomRight(), new Vector2(448f, 108f),
                                 new Vector2(-24f, 24f), PanelStrong, true);
        BuildCommandButtons();

        forecastPanel = PanelRect("ForecastCard", root, BottomCenter(), new Vector2(650f, 120f),
                                  new Vector2(0f, 112f), PanelStrong, true);
        forecastTitle = MakeText("ForecastTitle", forecastPanel, TopLeft(), TopRight(),
                                 new Vector2(18f, -30f), new Vector2(-18f, -8f), 15, FontStyle.Bold,
                                 TextAnchor.MiddleCenter, LineGold);
        forecastLeft = MakeText("ForecastAttacker", forecastPanel, new Vector2(0f, 0f), new Vector2(0.32f, 1f),
                                new Vector2(18f, 12f), new Vector2(-8f, -38f), 13, FontStyle.Bold,
                                TextAnchor.UpperLeft, TextMain);
        forecastCenter = MakeText("ForecastResult", forecastPanel, new Vector2(0.32f, 0f), new Vector2(0.68f, 1f),
                                  new Vector2(12f, 12f), new Vector2(-12f, -38f), 17, FontStyle.Bold,
                                  TextAnchor.UpperCenter, LineGold);
        forecastRight = MakeText("ForecastTarget", forecastPanel, new Vector2(0.68f, 0f), new Vector2(1f, 1f),
                                 new Vector2(8f, 12f), new Vector2(-18f, -38f), 13, FontStyle.Bold,
                                 TextAnchor.UpperRight, TextMain);

        hoverPanel = PanelRect("HoverTooltip", root, BottomLeft(), new Vector2(300f, 96f), Vector2.zero,
                               PanelStrong, true);
        hoverPanel.pivot = new Vector2(0f, 1f);
        hoverTitle = MakeText("HoverTitle", hoverPanel, TopLeft(), TopRight(),
                              new Vector2(14f, -30f), new Vector2(-14f, -8f), 14, FontStyle.Bold,
                              TextAnchor.MiddleLeft, LineGold);
        hoverBody = MakeText("HoverBody", hoverPanel, StretchMin(), StretchMax(), new Vector2(14f, 10f),
                             new Vector2(-14f, -34f), 12, FontStyle.Bold, TextAnchor.UpperLeft, TextMain);

        MakeButton("LogMiniButton", root, BottomRight(), new Vector2(92f, 30f), new Vector2(-24f, 96f),
                   () => owner.HudToggleLog(), out logMiniLabel).gameObject.SetActive(true);
        logMiniLabel.fontSize = 12;
        logMiniLabel.text = "\uAE30\uB85D L";

        logToastPanel = PanelRect("LogToast", root, BottomRight(), new Vector2(372f, 44f), new Vector2(-24f, 144f),
                                  PanelStrong, true);
        logToastText = MakeText("LogToastText", logToastPanel, StretchMin(), StretchMax(),
                                new Vector2(14f, 0f), new Vector2(-14f, 0f), 13, FontStyle.Bold,
                                TextAnchor.MiddleLeft, TextMain);

        logPanel = PanelRect("ExpandedLogPanel", root, RightCenter(), new Vector2(380f, 350f), new Vector2(-24f, 0f),
                             PanelStrong, true);
        logText = MakeText("LogText", logPanel, StretchMin(), StretchMax(), new Vector2(16f, 16f),
                           new Vector2(-16f, -16f), 13, FontStyle.Bold, TextAnchor.UpperLeft, TextMain);

        noticePanel = PanelRect("BattleNoticeToast", root, Center(), new Vector2(430f, 92f), new Vector2(0f, 120f),
                                PanelStrong, true);
        noticeText = MakeText("BattleNoticeText", noticePanel, StretchMin(), StretchMax(),
                              new Vector2(16f, 8f), new Vector2(-16f, -8f), 22, FontStyle.Bold,
                              TextAnchor.MiddleCenter, LineGold);

        selectedUnitCard.gameObject.SetActive(false);
        forecastPanel.gameObject.SetActive(false);
        hoverPanel.gameObject.SetActive(false);
        logToastPanel.gameObject.SetActive(false);
        logPanel.gameObject.SetActive(false);
        noticePanel.gameObject.SetActive(false);
    }

    private void BuildCommandButtons()
    {
        AddCommandButton(0, "\uC774\uB3D9", "\u2197", () => owner.HudSetCommand(BattleCommandMode.Move));
        AddCommandButton(1, "\uACF5\uACA9", "\u528D", () => owner.HudSetCommand(BattleCommandMode.Attack));
        AddCommandButton(2, "\uBB34\uACF5", "\u6B66", () => owner.HudSetCommand(BattleCommandMode.Skill));
        AddCommandButton(3, "\uBC29\uC5B4", "\u76FE", () => owner.HudGuard());
        AddCommandButton(4, "\uC9C0\uD615", "\u5730", () => owner.HudSetCommand(BattleCommandMode.Interact));
        AddCommandButton(5, "\uB300\uAE30", "\u5F85", () => owner.HudWait());
    }

    private void AddCommandButton(int index, string label, string glyph, Action action)
    {
        int column = index % 3;
        int row = index / 3;
        RectTransform buttonRect = MakeButton("CommandButton_" + index, commandPanel, TopLeft(),
                                              new Vector2(128f, 38f),
                                              new Vector2(20f + column * 136f, -18f - row * 46f), action,
                                              out Text text);
        text.text = label;
        text.fontSize = 14;
        text.alignment = TextAnchor.MiddleLeft;
        RectTransform labelRect = text.rectTransform;
        labelRect.offsetMin = new Vector2(44f, 0f);
        labelRect.offsetMax = new Vector2(-8f, 0f);

        Text icon = MakeText("CommandGlyph_" + index, buttonRect, StretchMin(), StretchMax(),
                             new Vector2(10f, 0f), new Vector2(-82f, 0f), 20, FontStyle.Bold,
                             TextAnchor.MiddleCenter, LineGold);
        icon.text = glyph;

        Image activeFrame = SolidImage("CommandActiveFrame_" + index, buttonRect, StretchMin(), StretchMax(),
                                       Vector2.zero, Vector2.zero, new Color(LineGold.r, LineGold.g, LineGold.b, 0.10f));
        AddBorder(activeFrame.gameObject, LineGold, new Vector2(2f, -2f));
        activeFrame.transform.SetAsFirstSibling();
        activeFrame.gameObject.SetActive(false);

        Image disabledOverlay = SolidImage("CommandDisabledOverlay_" + index, buttonRect, StretchMin(), StretchMax(),
                                           Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.34f));
        disabledOverlay.gameObject.SetActive(false);

        commandViews.Add(new CommandButtonView(buttonRect, buttonRect.GetComponent<Button>(),
                                               buttonRect.GetComponent<Image>(), icon, activeFrame,
                                               disabledOverlay, text));
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
        selectedMoveText.text = "\uC774\uB3D9 " + unit.actions.movementLeft + "  |  \uB0B4\uACF5 " +
                                unit.inner + "/" + unit.definition.maxInner;
        selectedStatusText.text = snapshot.unitStatuses.TryGetValue(unit, out string status)
                                      ? TrimStatus(status)
                                      : string.Empty;
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
            SetCommand(0, "\uBC30\uCE58", false, false);
            SetCommand(1, "\uC815\uCC30", false, false);
            SetCommand(2, "\uD655\uC778", false, false);
            SetCommand(3, "\uB300\uAE30", false, false);
            SetCommand(4, "\uC9C0\uD615", false, false);
            SetCommand(5, "\uC2DC\uC791", snapshot.canWait, true);
            return;
        }

        SetCommand(0, "\uC774\uB3D9", snapshot.canMove, snapshot.commandMode == BattleCommandMode.Move);
        SetCommand(1, "\uACF5\uACA9", snapshot.canAttack, snapshot.commandMode == BattleCommandMode.Attack);
        SetCommand(2, "\uBB34\uACF5", snapshot.canSkill, snapshot.commandMode == BattleCommandMode.Skill);
        SetCommand(3, "\uBC29\uC5B4", snapshot.canGuard, false);
        SetCommand(4, "\uC9C0\uD615", snapshot.canTerrain, snapshot.commandMode == BattleCommandMode.Interact);
        SetCommand(5, "\uB300\uAE30", snapshot.canWait, false);
    }

    private void SetCommand(int index, string label, bool enabled, bool active)
    {
        if (index < 0 || index >= commandViews.Count)
        {
            return;
        }

        CommandButtonView view = commandViews[index];
        bool activeEnabled = active && enabled;
        view.button.interactable = enabled;
        view.background.color = enabled ? (activeEnabled ? ButtonActive : Button) : ButtonDisabled;
        view.label.text = label;
        view.label.color = enabled ? TextMain : TextDim;
        view.icon.color = enabled ? (activeEnabled ? LineGold : TextSub) : TextDim;
        view.activeFrame.gameObject.SetActive(activeEnabled);
        view.disabledOverlay.gameObject.SetActive(!enabled);
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

        forecastTitle.text = string.IsNullOrEmpty(snapshot.forecastTitle)
                                 ? "\uC804\uD22C \uC608\uCE21"
                                 : snapshot.forecastTitle;
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
                slot.boundUnit = null;
                continue;
            }

            BattleTestUnit unit = snapshot.allies[i];
            bool isActiveUnit = unit == snapshot.activeUnit;
            slot.boundUnit = unit;
            slot.name.text = ShortName(unit.definition.displayName);
            slot.detail.text = unit.acted ? "\uD589\uB3D9" :
                               unit.defeated ? "\uC804\uD22C\uBD88\uB2A5" :
                               snapshot.selectableUnits.Contains(unit) ? "\uC900\uBE44" : "-";
            slot.badge.text = unit.acted ? "\u5F85" : unit.defeated ? "X" : string.Empty;
            slot.badge.gameObject.SetActive(unit.acted || unit.defeated);
            slot.name.color = unit.defeated ? TextDim : isActiveUnit ? LineGold : TextMain;
            slot.detail.color = unit.defeated ? TextDim : isActiveUnit ? LineGold : TextSub;
            slot.button.interactable = snapshot.selectableUnits.Contains(unit);
            slot.background.color = unit.acted ? new Color(0.060f, 0.060f, 0.056f, 0.76f) : Button;
            slot.activeFrame.gameObject.SetActive(isActiveUnit);
            slot.disabledOverlay.gameObject.SetActive(unit.defeated);
            SetGauge(slot.hpFill, unit.hp, unit.definition.maxHp);
            SetGauge(slot.innerFill, unit.inner, unit.definition.maxInner);
        }
    }

    private RosterSlot CreateRosterSlot(int index)
    {
        float x = 20f + index * 108f;
        RectTransform buttonRect = MakeButton("RosterSlot_" + index, rosterPanel, TopLeft(), new Vector2(98f, 56f),
                                              new Vector2(x, -10f), null, out Text label);
        label.gameObject.SetActive(false);
        Button button = buttonRect.GetComponent<Button>();
        Image background = buttonRect.GetComponent<Image>();
        Image activeFrame = SolidImage("RosterActiveFrame_" + index, buttonRect, StretchMin(), StretchMax(),
                                       Vector2.zero, Vector2.zero, new Color(LineGold.r, LineGold.g, LineGold.b, 0.08f));
        AddBorder(activeFrame.gameObject, LineGold, new Vector2(2f, -2f));
        activeFrame.transform.SetAsFirstSibling();
        activeFrame.gameObject.SetActive(false);
        Image disabledOverlay = SolidImage("RosterDisabledOverlay_" + index, buttonRect, StretchMin(), StretchMax(),
                                           Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.36f));
        disabledOverlay.gameObject.SetActive(false);

        Text name = MakeText("RosterName_" + index, buttonRect, TopLeft(), TopRight(),
                             new Vector2(8f, -21f), new Vector2(-8f, -3f), 13, FontStyle.Bold,
                             TextAnchor.MiddleLeft, TextMain);
        Text detail = MakeText("RosterDetail_" + index, buttonRect, TopLeft(), TopRight(),
                               new Vector2(54f, -21f), new Vector2(-8f, -3f), 11, FontStyle.Bold,
                               TextAnchor.MiddleRight, TextSub);
        Image hpFill = Gauge("RosterHp_" + index, buttonRect, new Vector2(8f, -30f), new Vector2(82f, 6f), HpFill);
        Image innerFill = Gauge("RosterInner_" + index, buttonRect, new Vector2(8f, -42f), new Vector2(82f, 5f),
                                InnerFill);
        Text badge = MakeText("RosterBadge_" + index, buttonRect, StretchMin(), StretchMax(), Vector2.zero, Vector2.zero,
                              18, FontStyle.Bold, TextAnchor.MiddleCenter, LineGold);
        badge.gameObject.SetActive(false);

        RosterSlot slot = new RosterSlot(buttonRect, button, background, activeFrame, disabledOverlay, name, detail,
                                         badge, hpFill, innerFill);
        button.onClick.AddListener(() =>
        {
            if (slot.boundUnit != null)
            {
                owner.HudSelectUnit(slot.boundUnit);
            }
        });
        return slot;
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
        logMiniLabel.text = snapshot.showLog ? "\uB2EB\uAE30 L" : "\uAE30\uB85D L";
        if (snapshot.showLog)
        {
            int start = Mathf.Max(0, snapshot.logs.Count - 12);
            List<string> lines = new List<string>();
            for (int i = start; i < snapshot.logs.Count; i++)
            {
                lines.Add(snapshot.logs[i]);
            }

            logText.text = "\uC804\uD22C \uAE30\uB85D\n" + string.Join("\n", lines);
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
        Vector2 size = new Vector2(310f, string.IsNullOrWhiteSpace(snapshot.hoverBody) ? 58f : 96f);
        hoverPanel.sizeDelta = size;
        hoverPanel.anchoredPosition = ClampTooltipPosition(Input.mousePosition, size);
    }

    private void UpdateNotice(BattleHudSnapshot snapshot)
    {
        bool visible = !string.IsNullOrEmpty(snapshot.noticeText);
        noticePanel.gameObject.SetActive(visible);
        if (visible)
        {
            noticeText.text = snapshot.noticeText;
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

    private RectTransform PanelRect(string name, Transform parent, Vector2 anchor, Vector2 size,
                                    Vector2 anchoredPosition, Color color, bool border)
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
        image.color = color;
        image.raycastTarget = false;
        if (border)
        {
            AddBorder(panelObject, new Color(LineGold.r, LineGold.g, LineGold.b, 0.24f), new Vector2(1f, -1f));
        }

        return rect;
    }

    private Text MakeText(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin,
                          Vector2 offsetMax, int size, FontStyle style, TextAnchor alignment, Color color)
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
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;
        text.supportRichText = true;

        Shadow shadow = textObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.72f);
        shadow.effectDistance = new Vector2(1f, -1f);
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
        image.color = Button;
        image.raycastTarget = true;
        AddBorder(buttonObject, new Color(LineGold.r, LineGold.g, LineGold.b, 0.20f), new Vector2(1f, -1f));

        Button button = buttonObject.AddComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 1.15f);
        colors.pressedColor = new Color(0.84f, 0.78f, 0.64f, 1f);
        colors.disabledColor = new Color(1f, 1f, 1f, 0.55f);
        colors.colorMultiplier = 1f;
        button.colors = colors;
        if (action != null)
        {
            button.onClick.AddListener(() => action());
        }

        label = MakeText(name + "_Label", rect, StretchMin(), StretchMax(), Vector2.zero, Vector2.zero, 13,
                         FontStyle.Bold, TextAnchor.MiddleCenter, TextMain);
        return rect;
    }

    private Image Gauge(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, Color fillColor)
    {
        RectTransform bg = PanelRect(name + "_Bg", parent, TopLeft(), size, anchoredPosition, GaugeBg, true);
        foreach (Outline outline in bg.GetComponents<Outline>())
        {
            Destroy(outline);
        }

        GameObject fillObject = new GameObject(name + "_Fill");
        fillObject.transform.SetParent(bg, false);
        RectTransform fillRect = fillObject.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(1f, 1f);
        fillRect.offsetMax = new Vector2(-1f, -1f);
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

    private static Image SolidImage(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
                                    Vector2 offsetMin, Vector2 offsetMax, Color color)
    {
        GameObject imageObject = new GameObject(name);
        imageObject.transform.SetParent(parent, false);
        RectTransform rect = imageObject.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        Image image = imageObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static void AddAccentLine(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
                                      Vector2 offsetMin, Vector2 offsetMax, Color color)
    {
        SolidImage(name, parent, anchorMin, anchorMax, offsetMin, offsetMax, color);
    }

    private static void AddBorder(GameObject target, Color color, Vector2 distance)
    {
        Outline outline = target.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = distance;
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
            return "\uC804\uD22C \uACB0\uACFC\uB97C \uD655\uC778\uD558\uC138\uC694.";
        }

        if (snapshot.phase == BattlePhase.EnemyPhase)
        {
            return "\uC801\uAD70\uC774 \uD589\uB3D9 \uC911\uC785\uB2C8\uB2E4.";
        }

        if (snapshot.scoutMode)
        {
            return "\uBC30\uCE58\uC640 \uC9C0\uD615\uC744 \uD655\uC778\uD558\uC138\uC694.";
        }

        if (snapshot.activeUnit == null)
        {
            return "\uD589\uB3D9\uD560 \uC544\uAD70\uC744 \uC120\uD0DD\uD558\uC138\uC694.";
        }

        switch (snapshot.commandMode)
        {
        case BattleCommandMode.Attack:
            return "\uACF5\uACA9 \uB300\uC0C1\uC744 \uC120\uD0DD\uD558\uC138\uC694.";
        case BattleCommandMode.Skill:
            return "\uBB34\uACF5 \uB300\uC0C1\uC744 \uC120\uD0DD\uD558\uC138\uC694.";
        case BattleCommandMode.Interact:
            return "\uC0AC\uC6A9\uD560 \uC9C0\uD615\uC744 \uC120\uD0DD\uD558\uC138\uC694.";
        default:
            return "\uC774\uB3D9\uD560 \uD0C0\uC77C\uC744 \uC120\uD0DD\uD558\uC138\uC694.";
        }
    }

    private static string CompactObjective(string value)
    {
        return string.IsNullOrEmpty(value) ? "\uC8FC \uBAA9\uD45C\uB97C \uD655\uC778 \uC911\uC785\uB2C8\uB2E4." : value;
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

        return value.Replace(" ", "\n");
    }

    private static string TrimStatus(string status)
    {
        if (string.IsNullOrEmpty(status))
        {
            return "\uC0C1\uD0DC \uC774\uC0C1 \uC5C6\uC74C";
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

    private static Font CreateHudFont()
    {
        string[] preferredFonts =
        {
            "Maplestory Bold",
            "MapleStory Bold",
            "MaplestoryOTFBold",
            "Maplestory Light",
            "MapleStory Light",
            "NEXON Lv1 Gothic OTF",
            "NEXON Lv1 Gothic",
            "Noto Sans KR",
            "Noto Sans CJK KR",
            "Malgun Gothic",
            "Gulim"
        };
        Font font = Font.CreateDynamicFontFromOSFont(preferredFonts, 18);
        return font != null ? font : UiTheme.Font;
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
            return "\uC804\uD22C \uC885\uB8CC";
        }

        switch (phase)
        {
        case BattlePhase.EnemyPhase:
            return "\uC801\uAD70 \uD398\uC774\uC988";
        case BattlePhase.NeutralPhase:
            return "\uC911\uB9BD \uD398\uC774\uC988";
        default:
            return "\uC544\uAD70 \uD398\uC774\uC988";
        }
    }

    private sealed class CommandButtonView
    {
        public readonly RectTransform root;
        public readonly Button button;
        public readonly Image background;
        public readonly Text icon;
        public readonly Image activeFrame;
        public readonly Image disabledOverlay;
        public readonly Text label;

        public CommandButtonView(RectTransform root, Button button, Image background, Text icon,
                                 Image activeFrame, Image disabledOverlay, Text label)
        {
            this.root = root;
            this.button = button;
            this.background = background;
            this.icon = icon;
            this.activeFrame = activeFrame;
            this.disabledOverlay = disabledOverlay;
            this.label = label;
        }
    }

    private sealed class RosterSlot
    {
        public readonly RectTransform root;
        public readonly Button button;
        public readonly Image background;
        public readonly Image activeFrame;
        public readonly Image disabledOverlay;
        public readonly Text name;
        public readonly Text detail;
        public readonly Text badge;
        public readonly Image hpFill;
        public readonly Image innerFill;
        public BattleTestUnit boundUnit;

        public RosterSlot(RectTransform root, Button button, Image background, Image activeFrame,
                          Image disabledOverlay, Text name, Text detail, Text badge, Image hpFill,
                          Image innerFill)
        {
            this.root = root;
            this.button = button;
            this.background = background;
            this.activeFrame = activeFrame;
            this.disabledOverlay = disabledOverlay;
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
