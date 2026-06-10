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
    private static readonly Color PanelBg = new Color(0.07f, 0.065f, 0.055f, 0.82f);
    private static readonly Color PanelStrong = new Color(0.045f, 0.042f, 0.036f, 0.90f);
    private static readonly Color PanelSoft = new Color(0.12f, 0.105f, 0.075f, 0.76f);
    private static readonly Color Ink = new Color(0.94f, 0.89f, 0.76f, 1f);
    private static readonly Color Muted = new Color(0.72f, 0.68f, 0.56f, 0.84f);
    private static readonly Color Gold = new Color(0.86f, 0.63f, 0.18f, 0.96f);
    private static readonly Color ButtonBg = new Color(0.22f, 0.18f, 0.10f, 0.90f);
    private static readonly Color Disabled = new Color(0.18f, 0.17f, 0.14f, 0.58f);

    private BattleTestController owner;
    private Canvas canvas;
    private Font koreanFont;
    private Text phaseTitle;
    private Text phaseInstruction;
    private Text objectiveText;
    private Text unitInfoText;
    private Text hoverTitle;
    private Text hoverBody;
    private Text forecastTitle;
    private Text forecastLeft;
    private Text forecastCenter;
    private Text forecastRight;
    private Text logText;
    private Text logCollapsedText;
    private Text legendText;
    private RectTransform hoverPanel;
    private RectTransform commandPanel;
    private RectTransform rosterPanel;
    private RectTransform forecastPanel;
    private RectTransform logPanel;
    private RectTransform logCollapsedPanel;
    private RectTransform dicePopupPanel;
    private Text dicePopupText;
    private readonly List<Button> commandButtons = new List<Button>();
    private readonly List<Text> commandLabels = new List<Text>();
    private readonly List<Button> rosterButtons = new List<Button>();
    private readonly List<Text> rosterLabels = new List<Text>();

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

        Build();
    }

    public void Refresh(BattleHudSnapshot snapshot)
    {
        if (canvas == null)
        {
            return;
        }

        phaseTitle.text = PhaseText(snapshot.phase, snapshot.battleOver) + "  -  " + snapshot.round + "턴";
        phaseInstruction.text = snapshot.instruction;
        objectiveText.text = snapshot.objectiveText;
        unitInfoText.text = snapshot.unitInfoText;
        hoverTitle.text = snapshot.hoverTitle;
        hoverBody.text = snapshot.hoverBody;
        bool hasHoverText = !string.IsNullOrWhiteSpace(snapshot.hoverTitle) ||
                            !string.IsNullOrWhiteSpace(snapshot.hoverBody);
        hoverPanel.gameObject.SetActive(hasHoverText);
        UpdateCommands(snapshot);
        UpdateForecast(snapshot);
        UpdateRoster(snapshot);
        UpdateLog(snapshot);
        UpdateDicePopup(snapshot);
        legendText.text =
            $"Tab 위협 {OnOff(snapshot.showThreatRange)}   H 고저 {OnOff(snapshot.showElevationOverlay)}   C 엄폐 {OnOff(snapshot.showCoverOverlay)}   V 시야 {OnOff(snapshot.showSightOverlay)}   O 목표 {OnOff(snapshot.showObjectiveOverlay)}";
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
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();
        RectTransform root = canvasObject.GetComponent<RectTransform>();

        RectTransform phasePanel = Panel("상단 전황", root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                                         new Vector2(560f, 72f), new Vector2(0f, -16f), PanelStrong);
        phaseTitle = MakeText("phase title", phasePanel, new Vector2(0f, 1f), new Vector2(1f, 1f),
                              new Vector2(16f, -34f), new Vector2(-16f, -6f), 22, FontStyle.Bold,
                              TextAnchor.MiddleLeft);
        phaseInstruction = MakeText("phase instruction", phasePanel, new Vector2(0f, 0f), new Vector2(1f, 0f),
                                    new Vector2(16f, 8f), new Vector2(-16f, 34f), 13, FontStyle.Normal,
                                    TextAnchor.MiddleLeft);

        RectTransform objectivePanel = Panel("목표", root, new Vector2(0f, 1f), new Vector2(0f, 1f),
                                             new Vector2(360f, 104f), new Vector2(16f, -16f), PanelBg);
        objectiveText = MakeText("objective text", objectivePanel, StretchMin(), StretchMax(), new Vector2(14f, 10f),
                                 new Vector2(-14f, -10f), 13, FontStyle.Bold, TextAnchor.UpperLeft);

        RectTransform infoPanel = Panel("선택 유닛", root, new Vector2(0f, 1f), new Vector2(0f, 1f),
                                        new Vector2(320f, 122f), new Vector2(16f, -132f), PanelBg);
        unitInfoText = MakeText("unit info", infoPanel, StretchMin(), StretchMax(), new Vector2(14f, 10f),
                                new Vector2(-14f, -10f), 13, FontStyle.Normal, TextAnchor.UpperLeft);

        hoverPanel = Panel("전술 정보", root, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                           new Vector2(310f, 106f), new Vector2(16f, 12f), PanelBg);
        hoverTitle = MakeText("hover title", hoverPanel, new Vector2(0f, 1f), new Vector2(1f, 1f),
                              new Vector2(14f, -34f), new Vector2(-14f, -8f), 16, FontStyle.Bold,
                              TextAnchor.MiddleLeft);
        hoverBody = MakeText("hover body", hoverPanel, StretchMin(), StretchMax(), new Vector2(14f, 12f),
                             new Vector2(-14f, -40f), 12, FontStyle.Normal, TextAnchor.UpperLeft);

        commandPanel = Panel("명령", root, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                             new Vector2(226f, 318f), new Vector2(-16f, 0f), PanelBg);
        MakeText("command title", commandPanel, new Vector2(0f, 1f), new Vector2(1f, 1f),
                 new Vector2(14f, -36f), new Vector2(-14f, -10f), 18, FontStyle.Bold,
                 TextAnchor.MiddleLeft).text = "명령";
        BuildCommandButtons();

        forecastPanel = Panel("전투 예측", root, new Vector2(1f, 0f), new Vector2(1f, 0f),
                              new Vector2(430f, 134f), new Vector2(-16f, 166f), PanelStrong);
        forecastTitle = MakeText("forecast title", forecastPanel, new Vector2(0f, 1f), new Vector2(1f, 1f),
                                 new Vector2(14f, -34f), new Vector2(-14f, -8f), 17, FontStyle.Bold,
                                 TextAnchor.MiddleLeft);
        forecastLeft = MakeText("forecast left", forecastPanel, new Vector2(0f, 0f), new Vector2(0.34f, 1f),
                                new Vector2(14f, 10f), new Vector2(-7f, -42f), 12, FontStyle.Normal,
                                TextAnchor.UpperLeft);
        forecastCenter = MakeText("forecast center", forecastPanel, new Vector2(0.34f, 0f), new Vector2(0.68f, 1f),
                                  new Vector2(7f, 10f), new Vector2(-7f, -42f), 12, FontStyle.Normal,
                                  TextAnchor.UpperLeft);
        forecastRight = MakeText("forecast right", forecastPanel, new Vector2(0.68f, 0f), new Vector2(1f, 1f),
                                 new Vector2(7f, 10f), new Vector2(-14f, -42f), 12, FontStyle.Normal,
                                 TextAnchor.UpperLeft);

        rosterPanel = Panel("아군 로스터", root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                            new Vector2(890f, 78f), new Vector2(0f, 14f), PanelBg);
        MakeText("roster title", rosterPanel, new Vector2(0f, 1f), new Vector2(1f, 1f),
                 new Vector2(14f, -28f), new Vector2(-14f, -6f), 15, FontStyle.Bold,
                 TextAnchor.MiddleLeft).text = "아군 배치 순서";

        logPanel = Panel("전투 로그", root, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(300f, 132f),
                         new Vector2(-16f, 14f), PanelBg);
        logText = MakeText("log text", logPanel, StretchMin(), StretchMax(), new Vector2(14f, 10f),
                           new Vector2(-14f, -10f), 12, FontStyle.Normal, TextAnchor.UpperLeft);
        logCollapsedPanel = Panel("로그 접힘", root, new Vector2(1f, 0f), new Vector2(1f, 0f),
                                  new Vector2(132f, 30f), new Vector2(-16f, 14f), PanelSoft);
        logCollapsedText = MakeText("log collapsed", logCollapsedPanel, StretchMin(), StretchMax(), Vector2.zero,
                                    Vector2.zero, 12, FontStyle.Normal, TextAnchor.MiddleCenter);

        RectTransform legendPanel = Panel("범례", root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                                          new Vector2(540f, 28f), new Vector2(0f, -92f), PanelSoft);
        legendText = MakeText("legend", legendPanel, StretchMin(), StretchMax(), new Vector2(14f, 0f),
                              new Vector2(-14f, 0f), 12, FontStyle.Bold, TextAnchor.MiddleCenter);

        dicePopupPanel = Panel("전술 알림", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                               new Vector2(420f, 104f), new Vector2(0f, 122f),
                               new Color(0.72f, 0.48f, 0.12f, 0.92f));
        dicePopupText = MakeText("dice popup text", dicePopupPanel, StretchMin(), StretchMax(), new Vector2(14f, 8f),
                                 new Vector2(-14f, -8f), 22, FontStyle.Bold, TextAnchor.MiddleCenter);
        dicePopupPanel.gameObject.SetActive(false);
    }

    private void BuildCommandButtons()
    {
        AddCommandButton(0, "1 이동", () => owner.HudSetCommand(BattleCommandMode.Move));
        AddCommandButton(1, "2 공격", () => owner.HudSetCommand(BattleCommandMode.Attack));
        AddCommandButton(2, "3 무공", () => owner.HudSetCommand(BattleCommandMode.Skill));
        AddCommandButton(3, "4 방어", () => owner.HudGuard());
        AddCommandButton(4, "5 지형", () => owner.HudSetCommand(BattleCommandMode.Interact));
        AddCommandButton(5, "대기", () => owner.HudWait());
        AddCommandButton(6, "위협 범위", () => owner.HudToggleThreat());
        AddCommandButton(7, "엄폐 표시", () => owner.HudToggleCover());
        AddCommandButton(8, "로그", () => owner.HudToggleLog());
        AddCommandButton(9, "전투 재시작", () => owner.HudResetBattle());
    }

    private void AddCommandButton(int index, string label, Action action)
    {
        int column = index % 2;
        int row = index / 2;
        RectTransform buttonRect = MakeButton("command " + index, commandPanel,
                                              new Vector2(14f + (column * 106f), -48f - (row * 50f)),
                                              new Vector2(92f, 40f), action, out Text text);
        text.text = label;
        commandButtons.Add(buttonRect.GetComponent<Button>());
        commandLabels.Add(text);
    }

    private void UpdateCommands(BattleHudSnapshot snapshot)
    {
        if (snapshot.scoutMode)
        {
            SetCommand(0, "정찰 중\n아군 재배치", false, false);
            SetCommand(1, "지형\n조사", false, false);
            SetCommand(2, "적 사거리\n확인", false, false);
            SetCommand(3, "지형지물\n확인", false, false);
            SetCommand(4, "배치 칸\n전용", false, false);
            SetCommand(5, "정찰 종료\n전투 시작", snapshot.canWait, true);
            SetCommand(6, snapshot.showThreatRange ? "위협 범위\n표시 중" : "위협 범위\n숨김", true,
                       snapshot.showThreatRange);
            SetCommand(7, snapshot.showCoverOverlay ? "엄폐\n표시 중" : "엄폐\n숨김", true,
                       snapshot.showCoverOverlay);
            SetCommand(8, snapshot.showLog ? "로그\n표시" : "로그\n접힘", true, snapshot.showLog);
            SetCommand(9, "전투\n재시작", true, false);
            return;
        }

        SetCommand(0, "1 이동\n" + Ready(snapshot.canMove), snapshot.canMove,
                   snapshot.commandMode == BattleCommandMode.Move);
        SetCommand(1, "2 공격\n" + Ready(snapshot.canAttack), snapshot.canAttack,
                   snapshot.commandMode == BattleCommandMode.Attack);
        SetCommand(2, "3 무공\n" + Ready(snapshot.canSkill), snapshot.canSkill,
                   snapshot.commandMode == BattleCommandMode.Skill);
        SetCommand(3, "4 방어\n" + Ready(snapshot.canGuard), snapshot.canGuard, false);
        SetCommand(4, "5 지형\n" + Ready(snapshot.canTerrain), snapshot.canTerrain,
                   snapshot.commandMode == BattleCommandMode.Interact);
        SetCommand(5, "대기\n넘기기", snapshot.canWait, false);
        SetCommand(6, snapshot.showThreatRange ? "위협 범위\n표시 중" : "위협 범위\n숨김", true,
                   snapshot.showThreatRange);
        SetCommand(7, snapshot.showCoverOverlay ? "엄폐\n표시 중" : "엄폐\n숨김", true,
                   snapshot.showCoverOverlay);
        SetCommand(8, snapshot.showLog ? "로그\n표시" : "로그\n접힘", true, snapshot.showLog);
        SetCommand(9, "전투\n재시작", true, false);
    }

    private void SetCommand(int index, string label, bool enabled, bool active)
    {
        Button button = commandButtons[index];
        Image image = button.GetComponent<Image>();
        button.interactable = enabled;
        image.color = active ? Gold : enabled ? ButtonBg : Disabled;
        commandLabels[index].text = label;
        commandLabels[index].color = enabled ? active ? new Color(0.09f, 0.075f, 0.045f, 1f) : Ink : Muted;
    }

    private void UpdateForecast(BattleHudSnapshot snapshot)
    {
        forecastPanel.gameObject.SetActive(snapshot.hasForecast);
        if (!snapshot.hasForecast)
        {
            return;
        }

        forecastTitle.text = snapshot.forecastTitle;
        forecastLeft.text = snapshot.forecastLeft;
        forecastCenter.text = snapshot.forecastCenter;
        forecastRight.text = snapshot.forecastRight;
    }

    private void UpdateRoster(BattleHudSnapshot snapshot)
    {
        while (rosterButtons.Count < snapshot.allies.Count)
        {
            int index = rosterButtons.Count;
            RectTransform buttonRect = MakeButton("roster " + index, rosterPanel,
                                                  new Vector2(18f + (index * 142f), -32f),
                                                  new Vector2(134f, 42f), null, out Text text);
            text.fontSize = 12;
            rosterButtons.Add(buttonRect.GetComponent<Button>());
            rosterLabels.Add(text);
        }

        for (int i = 0; i < rosterButtons.Count; i++)
        {
            bool active = i < snapshot.allies.Count;
            rosterButtons[i].gameObject.SetActive(active);
            if (!active)
            {
                continue;
            }

            BattleTestUnit unit = snapshot.allies[i];
            Button button = rosterButtons[i];
            Text label = rosterLabels[i];
            string status = snapshot.unitStatuses.TryGetValue(unit, out string value) ? value : string.Empty;
            label.text = unit.definition.displayName + "  HP " + unit.hp + "/" + unit.definition.maxHp +
                         "\n내공 " + unit.inner + "/" + unit.definition.maxInner + "  " + status;
            bool isActiveUnit = unit == snapshot.activeUnit;
            label.color = unit.defeated ? Muted : isActiveUnit ? new Color(0.09f, 0.075f, 0.045f, 1f) : Ink;
            button.interactable = snapshot.selectableUnits.Contains(unit);
            button.GetComponent<Image>().color = isActiveUnit
                                                      ? Gold
                                                      : unit.acted
                                                          ? new Color(0.16f, 0.15f, 0.12f, 0.82f)
                                                          : ButtonBg;
            BattleTestUnit captured = unit;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => owner.HudSelectUnit(captured));
        }
    }

    private void UpdateLog(BattleHudSnapshot snapshot)
    {
        logPanel.gameObject.SetActive(snapshot.showLog);
        logCollapsedPanel.gameObject.SetActive(!snapshot.showLog);
        logCollapsedText.text = "로그 (L)";
        if (!snapshot.showLog)
        {
            return;
        }

        int start = Mathf.Max(0, snapshot.logs.Count - 5);
        List<string> lines = new List<string>();
        for (int i = start; i < snapshot.logs.Count; i++)
        {
            lines.Add(snapshot.logs[i]);
        }

        logText.text = "전투 로그\n" + string.Join("\n", lines);
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

    private RectTransform Panel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 size,
                                Vector2 anchoredPosition, Color color)
    {
        GameObject panelObject = new GameObject(name);
        panelObject.transform.SetParent(parent, false);
        RectTransform rect = panelObject.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = anchorMin.x == anchorMax.x && anchorMin.x == 1f
                         ? new Vector2(1f, anchorMin.y)
                         : anchorMin.x == anchorMax.x && anchorMin.x == 0.5f
                             ? new Vector2(0.5f, anchorMin.y)
                             : new Vector2(anchorMin.x, anchorMin.y);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;

        Image image = panelObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = true;

        Outline outline = panelObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.60f, 0.44f, 0.16f, 0.34f);
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

    private RectTransform MakeButton(string name, Transform parent, Vector2 anchoredPosition, Vector2 size,
                                     Action action, out Text label)
    {
        GameObject buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;

        Image image = buttonObject.AddComponent<Image>();
        image.color = ButtonBg;
        Button button = buttonObject.AddComponent<Button>();
        if (action != null)
        {
            button.onClick.AddListener(() => action());
        }

        label = MakeText(name + " label", rect, StretchMin(), StretchMax(), Vector2.zero, Vector2.zero, 12,
                         FontStyle.Bold, TextAnchor.MiddleCenter);
        return rect;
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

    private static string Ready(bool ready)
    {
        return ready ? "가능" : "불가";
    }

    private static string OnOff(bool value)
    {
        return value ? "ON" : "OFF";
    }

    private static Vector2 StretchMin()
    {
        return Vector2.zero;
    }

    private static Vector2 StretchMax()
    {
        return Vector2.one;
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
