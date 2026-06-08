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
        private static readonly Color Hanji = new Color(0.95f, 0.88f, 0.72f, 0.94f);
        private static readonly Color Ink = new Color(0.11f, 0.22f, 0.24f, 1f);
        private static readonly Color PaleJade = new Color(0.78f, 0.91f, 0.85f, 0.94f);
        private static readonly Color Gold = new Color(0.88f, 0.68f, 0.22f, 1f);

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
        private BattleHudSnapshot lastSnapshot;

        public void Initialize(BattleTestController controller)
        {
            owner = controller;
            EnsureEventSystem();
            koreanFont = Font.CreateDynamicFontFromOSFont("Malgun Gothic", 18);
            if (koreanFont == null)
            {
                koreanFont = new Font("C:/Windows/Fonts/malgun.ttf");
            }

            Build();
        }

        public void Refresh(BattleHudSnapshot snapshot)
        {
            lastSnapshot = snapshot;
            if (canvas == null)
            {
                return;
            }

            phaseTitle.text = Korean.Phase(snapshot.phase) + "  -  " + snapshot.round + "라운드";
            phaseInstruction.text = snapshot.instruction;
            objectiveText.text = "압록강 폐사당 탈환전\n주 목표: 중원 감찰사 제압\n보조: 8턴 안 승리 / 제단 보존 / 지형 활용\n패배: 박성준 또는 백련 전투불능 / 12턴 초과";
            unitInfoText.text = BuildUnitInfo(snapshot.activeUnit);
            UpdateHover(snapshot);
            UpdateCommands(snapshot);
            UpdateForecast(snapshot);
            UpdateRoster(snapshot);
            UpdateLog(snapshot);
            UpdateDicePopup(snapshot);
            legendText.text = "■ 이동   ■ 공격   ■ 무공   ■ 활성   ■ 지형 활용   ■ 위험 범위";
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

            RectTransform phasePanel = Panel("상단 전황", root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(620f, 86f), new Vector2(0f, -18f), PaleJade);
            phaseTitle = MakeText("phase title", phasePanel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -40f), new Vector2(-18f, -8f), 24, FontStyle.Bold, TextAnchor.MiddleLeft);
            phaseInstruction = MakeText("phase instruction", phasePanel, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(18f, 10f), new Vector2(-18f, 38f), 15, FontStyle.Normal, TextAnchor.MiddleLeft);

            RectTransform objectivePanel = Panel("목표", root, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(330f, 154f), new Vector2(18f, -18f), Hanji);
            objectiveText = MakeText("objective text", objectivePanel, StretchMin(), StretchMax(), new Vector2(14f, 10f), new Vector2(-14f, -10f), 15, FontStyle.Bold, TextAnchor.UpperLeft);

            RectTransform infoPanel = Panel("선택 유닛", root, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(330f, 150f), new Vector2(18f, -180f), Hanji);
            unitInfoText = MakeText("unit info", infoPanel, StretchMin(), StretchMax(), new Vector2(14f, 10f), new Vector2(-14f, -10f), 15, FontStyle.Normal, TextAnchor.UpperLeft);

            RectTransform hoverPanel = Panel("전술 정보", root, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(330f, 150f), new Vector2(18f, 64f), Hanji);
            hoverTitle = MakeText("hover title", hoverPanel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(14f, -42f), new Vector2(-14f, -12f), 18, FontStyle.Bold, TextAnchor.MiddleLeft);
            hoverBody = MakeText("hover body", hoverPanel, StretchMin(), StretchMax(), new Vector2(14f, 12f), new Vector2(-14f, -48f), 14, FontStyle.Normal, TextAnchor.UpperLeft);

            commandPanel = Panel("명령", root, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(256f, 416f), new Vector2(-18f, 0f), Hanji);
            MakeText("command title", commandPanel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -42f), new Vector2(-16f, -12f), 20, FontStyle.Bold, TextAnchor.MiddleLeft).text = "명령";
            BuildCommandButtons();

            forecastPanel = Panel("전투 예측", root, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(560f, 230f), new Vector2(-286f, -296f), Hanji);
            forecastTitle = MakeText("forecast title", forecastPanel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -42f), new Vector2(-16f, -12f), 20, FontStyle.Bold, TextAnchor.MiddleLeft);
            forecastLeft = MakeText("forecast left", forecastPanel, new Vector2(0f, 0f), new Vector2(0.34f, 1f), new Vector2(16f, 14f), new Vector2(-8f, -52f), 14, FontStyle.Normal, TextAnchor.UpperLeft);
            forecastCenter = MakeText("forecast center", forecastPanel, new Vector2(0.34f, 0f), new Vector2(0.68f, 1f), new Vector2(8f, 14f), new Vector2(-8f, -52f), 14, FontStyle.Normal, TextAnchor.UpperLeft);
            forecastRight = MakeText("forecast right", forecastPanel, new Vector2(0.68f, 0f), new Vector2(1f, 1f), new Vector2(8f, 14f), new Vector2(-16f, -52f), 14, FontStyle.Normal, TextAnchor.UpperLeft);

            rosterPanel = Panel("아군 로스터", root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(960f, 106f), new Vector2(0f, 18f), Hanji);
            MakeText("roster title", rosterPanel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(14f, -34f), new Vector2(-14f, -8f), 18, FontStyle.Bold, TextAnchor.MiddleLeft).text = "아군 배치 순서";

            logPanel = Panel("전투 로그", root, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(332f, 190f), new Vector2(-18f, 18f), Hanji);
            logText = MakeText("log text", logPanel, StretchMin(), StretchMax(), new Vector2(14f, 10f), new Vector2(-14f, -10f), 13, FontStyle.Normal, TextAnchor.UpperLeft);
            logCollapsedPanel = Panel("로그 접힘", root, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(170f, 36f), new Vector2(-18f, 18f), Hanji);
            logCollapsedText = MakeText("log collapsed", logCollapsedPanel, StretchMin(), StretchMax(), Vector2.zero, Vector2.zero, 13, FontStyle.Normal, TextAnchor.MiddleCenter);

            RectTransform legendPanel = Panel("범례", root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(620f, 34f), new Vector2(0f, -104f), new Color(0.94f, 0.84f, 0.60f, 0.84f));
            legendText = MakeText("legend", legendPanel, StretchMin(), StretchMax(), new Vector2(14f, 0f), new Vector2(-14f, 0f), 13, FontStyle.Bold, TextAnchor.MiddleCenter);

            dicePopupPanel = Panel("d20 팝업", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(360f, 104f), new Vector2(0f, 122f), new Color(0.98f, 0.86f, 0.44f, 0.94f));
            dicePopupText = MakeText("dice popup text", dicePopupPanel, StretchMin(), StretchMax(), new Vector2(14f, 8f), new Vector2(-14f, -8f), 22, FontStyle.Bold, TextAnchor.MiddleCenter);
            dicePopupPanel.gameObject.SetActive(false);
        }

        private void BuildCommandButtons()
        {
            AddCommandButton(0, "1 이동", () => owner.HudSetCommand(BattleCommandMode.Move));
            AddCommandButton(1, "2 공격", () => owner.HudSetCommand(BattleCommandMode.Attack));
            AddCommandButton(2, "3 무공", () => owner.HudSetCommand(BattleCommandMode.Skill));
            AddCommandButton(3, "4 방어", () => owner.HudGuard());
            AddCommandButton(4, "5 지형 활용", () => owner.HudSetCommand(BattleCommandMode.Terrain));
            AddCommandButton(5, "대기", () => owner.HudWait());
            AddCommandButton(6, "위험 범위", () => owner.HudToggleThreat());
            AddCommandButton(7, "천기역전", () => owner.HudRewind());
            AddCommandButton(8, "로그", () => owner.HudToggleLog());
            AddCommandButton(9, "전투 재시작", () => owner.HudResetBattle());
        }

        private void AddCommandButton(int index, string label, Action action)
        {
            int column = index % 2;
            int row = index / 2;
            RectTransform buttonRect = MakeButton("command " + index, commandPanel, new Vector2(16f + (column * 118f), -54f - (row * 66f)), new Vector2(102f, 50f), action, out Text text);
            text.text = label;
            commandButtons.Add(buttonRect.GetComponent<Button>());
            commandLabels.Add(text);
        }

        private void UpdateCommands(BattleHudSnapshot snapshot)
        {
            SetCommand(0, "1 이동\n" + Ready(snapshot.canMove), snapshot.canMove, snapshot.commandMode == BattleCommandMode.Move);
            SetCommand(1, "2 공격\n" + Ready(snapshot.canAttack), snapshot.canAttack, snapshot.commandMode == BattleCommandMode.Attack);
            SetCommand(2, "3 무공\n" + Ready(snapshot.canSkill), snapshot.canSkill, snapshot.commandMode == BattleCommandMode.Skill);
            SetCommand(3, "4 방어\n" + Ready(snapshot.canGuard), snapshot.canGuard, false);
            SetCommand(4, "5 지형\n" + Ready(snapshot.canTerrain), snapshot.canTerrain, snapshot.commandMode == BattleCommandMode.Terrain);
            SetCommand(5, "대기\n행동 종료", snapshot.canWait, false);
            SetCommand(6, snapshot.showThreatRange ? "위험 범위\n표시 중" : "위험 범위\n숨김", true, snapshot.showThreatRange);
            SetCommand(7, "천기역전\n" + snapshot.rewindUsesRemaining + "회", snapshot.canRewind, false);
            SetCommand(8, snapshot.showLog ? "로그\n표시" : "로그\n접힘", true, snapshot.showLog);
            SetCommand(9, "전투\n재시작", true, false);
        }

        private void SetCommand(int index, string label, bool enabled, bool active)
        {
            Button button = commandButtons[index];
            Image image = button.GetComponent<Image>();
            button.interactable = enabled;
            image.color = active ? Gold : enabled ? new Color(0.92f, 0.78f, 0.42f, 0.96f) : new Color(0.45f, 0.44f, 0.38f, 0.62f);
            commandLabels[index].text = label;
            commandLabels[index].color = enabled ? Ink : new Color(0.22f, 0.22f, 0.20f, 0.65f);
        }

        private void UpdateForecast(BattleHudSnapshot snapshot)
        {
            forecastPanel.gameObject.SetActive(snapshot.hasForecast);
            if (!snapshot.hasForecast)
            {
                return;
            }

            BattleForecast forecast = snapshot.forecast;
            forecastTitle.text = "전투 예측";
            if (!forecast.valid)
            {
                forecastLeft.text = string.Empty;
                forecastCenter.text = Korean.ForecastReason(forecast.reason);
                forecastRight.text = string.Empty;
                return;
            }

            forecastLeft.text = forecast.actorName + "\n" + forecast.actionName + "\n피해 " + forecast.damageMin + "~" + forecast.damageMax + "\n기세 +" + forecast.moraleChange;
            forecastCenter.text = "d20 " + forecast.requiredD20 + "+  명중 " + forecast.hitPercent + "%\n치명 " + forecast.critPercent + "%\n파훼 +" + forecast.breakGain + "\n상성: " + Korean.Advantage(forecast.styleAdvantage) + "\n보정: " + Korean.Modifiers(forecast.modifierSummary);
            forecastRight.text = forecast.targetName + "\n" + (forecast.counterPossible ? "반격 " + forecast.counterDamageMin + "~" + forecast.counterDamageMax : "반격 불가: " + Korean.CounterReason(forecast.counterReason)) + "\n" + (forecast.followUpPossible ? "추격 " + forecast.followUpDamageMin + "~" + forecast.followUpDamageMax : "추격 없음") + "\n예상 HP -> " + forecast.projectedTargetHp;
        }

        private void UpdateHover(BattleHudSnapshot snapshot)
        {
            if (snapshot.hoveredUnit != null)
            {
                BattleTestUnit unit = snapshot.hoveredUnit;
                hoverTitle.text = unit.definition.displayName + " / " + Korean.FactionName(unit.definition.faction);
                hoverBody.text = "HP " + unit.hp + "/" + unit.definition.maxHp + "   방어 " + snapshot.hoverDefense + "\n무공: " + unit.definition.specialName + "\n상태: " + Korean.Status(snapshot.hoverStatus);
                return;
            }

            if (snapshot.hoveredTile != null)
            {
                BattleTestTile tile = snapshot.hoveredTile;
                hoverTitle.text = Korean.Terrain(tile.terrain) + "  (" + tile.cell.x + "," + tile.cell.y + ")";
                hoverBody.text = "이동 비용 " + tile.moveCost + "   엄폐 +" + tile.coverBonus + "\n고저차 " + tile.elevation + "   상태 " + Korean.Hazard(tile.hazard);
                return;
            }

            hoverTitle.text = "전술 정보";
            hoverBody.text = "유닛이나 지형에 마우스를 올리면 이동 비용, 엄폐, 고저차, 위험도를 보여줍니다.";
        }

        private void UpdateRoster(BattleHudSnapshot snapshot)
        {
            while (rosterButtons.Count < snapshot.allies.Count)
            {
                int index = rosterButtons.Count;
                RectTransform buttonRect = MakeButton("roster " + index, rosterPanel, new Vector2(14f + (index * 186f), -44f), new Vector2(174f, 58f), null, out Text text);
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
                label.text = unit.definition.displayName + "\nHP " + unit.hp + "/" + unit.definition.maxHp + "  내공 " + unit.inner + "/" + unit.definition.maxInner + "\n" + Korean.Status(snapshot.unitStatuses[unit]);
                label.color = unit.defeated ? new Color(0.28f, 0.28f, 0.25f, 0.75f) : Ink;
                button.interactable = snapshot.selectableUnits.Contains(unit);
                button.GetComponent<Image>().color = unit == snapshot.activeUnit ? Gold : unit.turnEnded ? new Color(0.62f, 0.60f, 0.50f, 0.76f) : new Color(0.84f, 0.90f, 0.72f, 0.96f);
                BattleTestUnit captured = unit;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => owner.HudSelectUnit(captured));
            }
        }

        private void UpdateLog(BattleHudSnapshot snapshot)
        {
            logPanel.gameObject.SetActive(snapshot.showLog);
            logCollapsedPanel.gameObject.SetActive(!snapshot.showLog);
            logCollapsedText.text = "전투 로그 접힘 (L)";
            if (!snapshot.showLog)
            {
                return;
            }

            int start = Mathf.Max(0, snapshot.logs.Count - 8);
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

        private string BuildUnitInfo(BattleTestUnit unit)
        {
            if (unit == null)
            {
                return "선택 유닛: 없음\n아군을 선택하세요.";
            }

            string status = lastSnapshot != null && lastSnapshot.unitStatuses.ContainsKey(unit) ? lastSnapshot.unitStatuses[unit] : string.Empty;
            return "선택: " + unit.definition.displayName + "\n문파/초식: " + Korean.Style(unit.definition.style) + "\nHP " + unit.hp + "/" + unit.definition.maxHp + "   내공 " + unit.inner + "/" + unit.definition.maxInner + "\n이동 " + unit.definition.moveRange + "   민첩 " + unit.definition.agility + "\n상태: " + Korean.Status(status);
        }

        private RectTransform Panel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Vector2 anchoredPosition, Color color)
        {
            GameObject panelObject = new GameObject(name);
            panelObject.transform.SetParent(parent, false);
            RectTransform rect = panelObject.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = anchorMin.x == anchorMax.x && anchorMin.x == 1f ? new Vector2(1f, anchorMin.y) : anchorMin.x == anchorMax.x && anchorMin.x == 0.5f ? new Vector2(0.5f, anchorMin.y) : new Vector2(anchorMin.x, anchorMin.y);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;
            Image image = panelObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            Outline outline = panelObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.16f, 0.13f, 0.10f, 0.42f);
            outline.effectDistance = new Vector2(1f, -1f);
            return rect;
        }

        private Text MakeText(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, int size, FontStyle style, TextAnchor alignment)
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
            return text;
        }

        private RectTransform MakeButton(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, Action action, out Text label)
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
            image.color = new Color(0.92f, 0.78f, 0.42f, 0.96f);
            Button button = buttonObject.AddComponent<Button>();
            if (action != null)
            {
                button.onClick.AddListener(() => action());
            }

            label = MakeText(name + " label", rect, StretchMin(), StretchMax(), Vector2.zero, Vector2.zero, 13, FontStyle.Bold, TextAnchor.MiddleCenter);
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

        private static string Ready(bool ready)
        {
            return ready ? "가능" : "불가";
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
        public string instruction;
        public BattleTestUnit activeUnit;
        public BattleTestUnit hoveredUnit;
        public BattleTestTile hoveredTile;
        public int hoverDefense;
        public string hoverStatus;
        public BattleForecast forecast;
        public bool hasForecast;
        public bool showLog;
        public bool showThreatRange;
        public bool canMove;
        public bool canAttack;
        public bool canSkill;
        public bool canGuard;
        public bool canTerrain;
        public bool canWait;
        public bool canRewind;
        public int rewindUsesRemaining;
        public string noticeText;
        public BattleCommandMode commandMode;
        public readonly List<BattleTestUnit> allies = new List<BattleTestUnit>();
        public readonly HashSet<BattleTestUnit> selectableUnits = new HashSet<BattleTestUnit>();
        public readonly Dictionary<BattleTestUnit, string> unitStatuses = new Dictionary<BattleTestUnit, string>();
        public readonly List<string> logs = new List<string>();
    }

    public static class Korean
    {
        public static string Phase(BattlePhase phase)
        {
            switch (phase)
            {
                case BattlePhase.EnemyPhase:
                    return "적군 페이즈";
                case BattlePhase.Victory:
                    return "승리";
                case BattlePhase.Defeat:
                    return "패배";
                default:
                    return "아군 페이즈";
            }
        }

        public static string FactionName(Faction faction)
        {
            switch (faction)
            {
                case Faction.Enemy:
                    return "적군";
                case Faction.Neutral:
                    return "중립";
                default:
                    return "아군";
            }
        }

        public static string Style(SkillStyle style)
        {
            switch (style)
            {
                case SkillStyle.Blade:
                    return "도법";
                case SkillStyle.Spear:
                    return "창법";
                case SkillStyle.Palm:
                    return "권장";
                case SkillStyle.HiddenWeapon:
                    return "암기";
                case SkillStyle.Poison:
                    return "독공";
                case SkillStyle.Ice:
                    return "빙공";
                case SkillStyle.Mind:
                    return "심법";
                default:
                    return "검법";
            }
        }

        public static string Terrain(TerrainType terrain)
        {
            switch (terrain)
            {
                case TerrainType.Wood:
                    return "목재 바닥";
                case TerrainType.Water:
                    return "압록강 여울";
                case TerrainType.Bamboo:
                    return "대나무숲";
                case TerrainType.Bridge:
                    return "낡은 다리";
                case TerrainType.Roof:
                    return "누각 지붕";
                case TerrainType.Cliff:
                    return "벼랑 능선";
                case TerrainType.Wall:
                    return "무너진 담장";
                default:
                    return "사당 마당";
            }
        }

        public static string Hazard(HazardType hazard)
        {
            switch (hazard)
            {
                case HazardType.Slippery:
                    return "미끄러움";
                case HazardType.Smoke:
                    return "연막";
                case HazardType.Fire:
                    return "화염";
                case HazardType.Ice:
                    return "빙판";
                case HazardType.Fall:
                    return "낙하 위험";
                default:
                    return "없음";
            }
        }

        public static string Advantage(StyleAdvantage advantage)
        {
            switch (advantage)
            {
                case StyleAdvantage.Advantage:
                    return "유리";
                case StyleAdvantage.Disadvantage:
                    return "불리";
                default:
                    return "보통";
            }
        }

        public static string Status(string status)
        {
            if (string.IsNullOrWhiteSpace(status) || status == "Ready")
            {
                return "준비";
            }

            return status
                .Replace("Down", "전투불능")
                .Replace("Action Done", "행동 완료")
                .Replace("Done", "행동 완료")
                .Replace("Guard", "방어")
                .Replace("Broken", "파훼")
                .Replace("Prone", "넘어짐")
                .Replace("Disarmed", "무장해제")
                .Replace("Poison", "독")
                .Replace("Slow", "둔화")
                .Replace("Marked", "표식")
                .Replace("Moved", "이동 완료")
                .Replace("Support", "협공")
                .Replace("Ready", "준비");
        }

        public static string CounterReason(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return "조건 불충족";
            }

            return reason
                .Replace("Ready", "가능")
                .Replace("No defender", "대상 없음")
                .Replace("Target down", "대상 전투불능")
                .Replace("No counter skill", "반격 무공 없음")
                .Replace("Broken", "파훼")
                .Replace("Disarmed", "무장해제")
                .Replace("Prone", "넘어짐")
                .Replace("Counter spent", "반격 소모")
                .Replace("Out of range", "사거리 밖")
                .Replace("Inner force low", "내공 부족")
                .Replace("Blocked", "봉쇄");
        }

        public static string ForecastReason(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return "예측할 대상이 없습니다.";
            }

            return CounterReason(reason)
                .Replace("No valid target", "유효한 대상 없음");
        }

        public static string Modifiers(string modifiers)
        {
            if (string.IsNullOrWhiteSpace(modifiers))
            {
                return "추가 보정 없음";
            }

            return modifiers
                .Replace("no extra modifiers", "추가 보정 없음")
                .Replace("style", "상성")
                .Replace("terrain", "지형")
                .Replace("support", "협공")
                .Replace("cover", "엄폐")
                .Replace("Smoke", "연막")
                .Replace("Fire", "화염")
                .Replace("Ice", "빙판")
                .Replace("Slippery", "미끄러움");
        }
    }
}
