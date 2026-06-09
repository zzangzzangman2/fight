using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace JoseonMurimTactics
{
    [DisallowMultipleComponent]
    public sealed class BattleTestController : MonoBehaviour
    {
        public int width = 12;
        public int height = 8;
        public float tileWidth = 1.16f;
        public float tileHeight = 0.62f;
        public BattleTestUnitDefinition[] unitDefinitions = new BattleTestUnitDefinition[0];

        private const int SmokeCoverBonus = 2;
        private const int CoverInteractBonus = 2;
        private const int FireInteractDamage = 4;
        private static readonly bool UseLegacyOnGui = false;

        private readonly List<BattleTestUnit> units = new List<BattleTestUnit>();
        private readonly List<string> battleLog = new List<string>();
        private readonly List<BattleTestInteractable> interactables = new List<BattleTestInteractable>();
        private readonly PhaseTurnController phaseTurn = new PhaseTurnController();
        private readonly System.Random random = new System.Random(20260608);
        private BattleTestTile[,] tiles;
        private Sprite diamondSprite;
        private GUIStyle panelStyle;
        private GUIStyle labelStyle;
        private GUIStyle titleStyle;
        private GUIStyle smallStyle;
        private GUIStyle logStyle;
        private Canvas hudCanvas;
        private TMP_Text hudPhaseText;
        private TMP_Text hudActiveText;
        private TMP_Text hudResourceText;
        private TMP_Text hudObjectiveText;
        private TMP_Text hudPhaseListText;
        private TMP_Text hudLogText;
        private TMP_Text hudInspectText;
        private TMP_Text hudForecastText;
        private TMP_Text hudRosterText;
        private Button hudMoveButton;
        private Button hudAttackButton;
        private Button hudSkillButton;
        private Button hudGuardButton;
        private Button hudInteractButton;
        private Button hudPhaseEndButton;
        private Button hudResetButton;
        private readonly List<Button> hudCommandButtons = new List<Button>();
        private BattleTestUnit activeUnit;
        private BattleTestUnit hoveredUnit;
        private BattleTestTile hoveredTile;
        private int round = 1;
        private bool busy;
        private bool aiQueued;
        private bool battleOver;
        private BattleCommandMode commandMode = BattleCommandMode.Move;

        private void Start()
        {
            BuildBattle();
        }

        private void Update()
        {
            if (battleOver)
            {
                if (Input.GetKeyDown(KeyCode.R))
                {
                    BuildBattle();
                }

                return;
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                BuildBattle();
                return;
            }

            UpdateHover();

            if (busy)
            {
                return;
            }

            if (phaseTurn.IsEnemyPhase)
            {
                if (!aiQueued)
                {
                    aiQueued = true;
                    StartCoroutine(RunEnemyPhase());
                }

                return;
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                EndTurn();
                return;
            }

            if (activeUnit != null && Input.GetKeyDown(KeyCode.Escape))
            {
                SetCommandMode(BattleCommandMode.Move);
            }
            else if (activeUnit != null && Input.GetKeyDown(KeyCode.Alpha1))
            {
                SetCommandMode(BattleCommandMode.Move);
            }
            else if (activeUnit != null && Input.GetKeyDown(KeyCode.Alpha2))
            {
                SetCommandMode(BattleCommandMode.Attack);
            }
            else if (activeUnit != null && Input.GetKeyDown(KeyCode.Alpha3))
            {
                SetCommandMode(BattleCommandMode.Skill);
            }
            else if (activeUnit != null && Input.GetKeyDown(KeyCode.Alpha4))
            {
                GuardActiveUnit();
            }
            else if (activeUnit != null && Input.GetKeyDown(KeyCode.Alpha5))
            {
                SetCommandMode(BattleCommandMode.Interact);
            }

            if (Input.GetMouseButtonDown(0))
            {
                if (PointerOverHud(Input.mousePosition))
                {
                    return;
                }

                HandlePointer(Input.mousePosition);
            }
        }

        private void LateUpdate()
        {
            RefreshCanvasHud();
        }

        private void OnGUI()
        {
            if (!UseLegacyOnGui)
            {
                return;
            }

            EnsureGuiStyles();

            DrawActivePanel();
            DrawObjectivePanel();
            DrawTurnQueuePanel();
            DrawLogPanel();
            DrawInspectPanel();
            DrawForecastPanel();
            DrawRosterPanel();

            if (!battleOver)
            {
                return;
            }

            GUI.Box(new Rect((Screen.width * 0.5f) - 180f, 24f, 360f, 76f), GUIContent.none, panelStyle);
            GUI.Label(new Rect((Screen.width * 0.5f) - 146f, 42f, 310f, 28f), "전투 종료", titleStyle);
        }

        private void DrawActivePanel()
        {
            GUI.Box(new Rect(18f, 18f, 340f, 326f), GUIContent.none, panelStyle);
            string activeName = activeUnit == null ? "선택 없음" : activeUnit.definition.displayName;
            string hp = activeUnit == null ? string.Empty : $"{activeUnit.hp}/{activeUnit.definition.maxHp}";
            string side = activeUnit == null ? string.Empty : FactionLabel(activeUnit.definition.faction);

            GUI.Label(new Rect(34f, 30f, 300f, 28f), $"제 {round}턴 · {PhaseLabel(phaseTurn.CurrentPhase)}", titleStyle);
            GUI.Label(new Rect(34f, 62f, 300f, 24f), $"현재 행동: {activeName}", labelStyle);
            GUI.Label(new Rect(34f, 88f, 300f, 22f), $"진영: {side}   체력: {hp}", smallStyle);

            if (activeUnit != null)
            {
                GUI.Label(new Rect(34f, 112f, 300f, 22f), $"이동: {ActionText(activeUnit.moved)}   행동: {ActionText(activeUnit.acted)}", smallStyle);
                GUI.Label(new Rect(34f, 136f, 300f, 22f), $"내공: {activeUnit.inner}/{activeUnit.definition.maxInner}   방어: {YesNo(activeUnit.guarded)}", smallStyle);
                GUI.Label(new Rect(34f, 160f, 300f, 22f), $"명령: {CommandLabel(commandMode)}", labelStyle);
            }

            bool playerTurn = phaseTurn.IsPlayerPhase && activeUnit != null && activeUnit.definition.faction == Faction.Ally && !busy && !battleOver;
            GUI.enabled = playerTurn;

            if (GUI.Button(new Rect(34f, 192f, 70f, 28f), "이동"))
            {
                SetCommandMode(BattleCommandMode.Move);
            }

            GUI.enabled = playerTurn && activeUnit != null && !activeUnit.acted;
            if (GUI.Button(new Rect(112f, 192f, 70f, 28f), "공격"))
            {
                SetCommandMode(BattleCommandMode.Attack);
            }

            GUI.enabled = playerTurn && activeUnit != null && CanUseSpecial(activeUnit);
            if (GUI.Button(new Rect(190f, 192f, 70f, 28f), "무공"))
            {
                SetCommandMode(BattleCommandMode.Skill);
            }

            GUI.enabled = playerTurn && activeUnit != null && !activeUnit.acted;
            if (GUI.Button(new Rect(268f, 192f, 70f, 28f), "방어"))
            {
                GuardActiveUnit();
            }

            GUI.enabled = playerTurn && activeUnit != null && !activeUnit.acted && HasUsableInteractable(activeUnit);
            if (GUI.Button(new Rect(34f, 230f, 92f, 30f), "지형"))
            {
                SetCommandMode(BattleCommandMode.Interact);
            }

            GUI.enabled = playerTurn;
            if (GUI.Button(new Rect(134f, 230f, 92f, 30f), "페이즈 종료"))
            {
                EndTurn();
            }

            if (GUI.Button(new Rect(234f, 230f, 104f, 30f), "재시작"))
            {
                BuildBattle();
            }

            GUI.enabled = true;

            if (activeUnit != null)
            {
                string skillLine = activeUnit.definition.specialName;
                string cooldown = activeUnit.specialCooldownLeft > 0 ? $"대기 {activeUnit.specialCooldownLeft}턴" : "준비됨";
                GUI.Label(new Rect(34f, 266f, 304f, 22f), $"무공: {skillLine} ({cooldown})", smallStyle);
                GUI.Label(new Rect(34f, 290f, 304f, 22f), $"민첩: {AgilityValue(activeUnit)}   반격: {CounterSummary(activeUnit)}", smallStyle);
            }
        }

        private void DrawTurnQueuePanel()
        {
            float x = Screen.width - 386f;
            GUI.Box(new Rect(x, 18f, 368f, 194f), GUIContent.none, panelStyle);
            GUI.Label(new Rect(x + 16f, 30f, 336f, 26f), "페이즈", titleStyle);

            List<BattleTestUnit> queue = GetTurnQueuePreview(7);
            for (int i = 0; i < queue.Count; i++)
            {
                BattleTestUnit unit = queue[i];
                string marker = unit == activeUnit ? "현재" : (unit.acted ? "완료" : "대기");
                string state = unit.defeated ? "전투불능" : UnitStatusText(unit);
                string line = $"{marker}  {unit.definition.displayName}  {FactionLabel(unit.definition.faction)}  {state}";
                GUI.Label(new Rect(x + 16f, 60f + (i * 18f), 336f, 18f), line, i == 0 ? labelStyle : smallStyle);
            }
        }

        private void DrawObjectivePanel()
        {
            float widthPx = Mathf.Min(500f, Screen.width - 780f);
            if (widthPx < 300f)
            {
                return;
            }

            float x = (Screen.width * 0.5f) - (widthPx * 0.5f);
            GUI.Box(new Rect(x, 18f, widthPx, 112f), GUIContent.none, panelStyle);
            GUI.Label(new Rect(x + 16f, 30f, widthPx - 32f, 24f), "목표", titleStyle);
            GUI.Label(new Rect(x + 16f, 58f, widthPx - 32f, 20f), "중원 사절 호위대를 제압", labelStyle);
            GUI.Label(new Rect(x + 16f, 80f, widthPx - 32f, 18f), "추천: 대나무숲 엄폐, 지붕 고저 +2, 향로/등불 활용", smallStyle);
            GUI.Label(new Rect(x + 16f, 98f, widthPx - 32f, 18f), "위험: 화염 칸 진입 시 피해", smallStyle);
        }

        private void DrawLogPanel()
        {
            float x = Screen.width - 386f;
            GUI.Box(new Rect(x, 224f, 368f, 244f), GUIContent.none, panelStyle);
            GUI.Label(new Rect(x + 16f, 236f, 336f, 26f), "전투 기록", titleStyle);

            int start = Mathf.Max(0, battleLog.Count - 9);
            for (int i = start; i < battleLog.Count; i++)
            {
                GUI.Label(new Rect(x + 16f, 268f + ((i - start) * 22f), 336f, 22f), battleLog[i], logStyle);
            }
        }

        private void DrawInspectPanel()
        {
            GUI.Box(new Rect(18f, 356f, 340f, 190f), GUIContent.none, panelStyle);
            GUI.Label(new Rect(34f, 368f, 300f, 26f), "정보", titleStyle);

            if (hoveredUnit != null)
            {
                GUI.Label(new Rect(34f, 398f, 300f, 22f), $"{hoveredUnit.definition.displayName} ({FactionLabel(hoveredUnit.definition.faction)})", labelStyle);
                GUI.Label(new Rect(34f, 422f, 300f, 22f), $"체력 {hoveredUnit.hp}/{hoveredUnit.definition.maxHp}   방어 {DefenseValue(hoveredUnit, TileAt(hoveredUnit.cell))}", smallStyle);
                GUI.Label(new Rect(34f, 446f, 300f, 22f), $"상태: {UnitStatusText(hoveredUnit)}", smallStyle);
                GUI.Label(new Rect(34f, 470f, 300f, 22f), $"무공: {hoveredUnit.definition.specialName}", smallStyle);
                return;
            }

            if (hoveredTile != null)
            {
                BattleTestInteractable prop = GetInteractableAt(hoveredTile.cell);
                GUI.Label(new Rect(34f, 398f, 300f, 22f), $"{TerrainLabel(hoveredTile.terrain)}  ({hoveredTile.cell.x},{hoveredTile.cell.y})", labelStyle);
                GUI.Label(new Rect(34f, 422f, 300f, 22f), $"이동 비용 {hoveredTile.moveCost}   엄폐 +{hoveredTile.coverBonus}", smallStyle);
                GUI.Label(new Rect(34f, 446f, 300f, 22f), $"고저 {hoveredTile.elevation}   진입 {YesNo(hoveredTile.walkable)}", smallStyle);
                if (prop != null)
                {
                    int distance = activeUnit == null ? -1 : GridDistance(activeUnit.cell, prop.cell);
                    bool usable = activeUnit != null && !activeUnit.acted && distance <= 1;
                    GUI.Label(new Rect(34f, 470f, 300f, 22f), $"지형지물: {prop.displayName}", smallStyle);
                    GUI.Label(new Rect(34f, 494f, 300f, 22f), $"효과: {InteractableEffectText(prop.kind)}   사용: {YesNo(usable)}", smallStyle);
                }
                else
                {
                    GUI.Label(new Rect(34f, 470f, 300f, 22f), $"위험: {TileHazardText(hoveredTile)}", smallStyle);
                }
                return;
            }

            GUI.Label(new Rect(34f, 398f, 300f, 22f), "가리킨 대상 없음", smallStyle);
        }

        private void DrawForecastPanel()
        {
            float x = Screen.width - 386f;
            GUI.Box(new Rect(x, 452f, 368f, 224f), GUIContent.none, panelStyle);
            GUI.Label(new Rect(x + 16f, 464f, 336f, 26f), "전투 예측", titleStyle);

            BattleForecast forecast = BuildForecast(activeUnit, hoveredUnit);
            if (!forecast.valid)
            {
                if (!string.IsNullOrEmpty(forecast.actorName))
                {
                    GUI.Label(new Rect(x + 16f, 494f, 336f, 22f), $"{forecast.actorName} -> {forecast.targetName}  [{forecast.commandName}]", labelStyle);
                    GUI.Label(new Rect(x + 16f, 520f, 336f, 20f), $"불가: {forecast.invalidReason}", smallStyle);
                    GUI.Label(new Rect(x + 16f, 542f, 336f, 20f), $"거리 {forecast.distance} / 사거리 {forecast.range}", smallStyle);
                    GUI.Label(new Rect(x + 16f, 564f, 336f, 20f), forecast.costText, smallStyle);
                }
                else
                {
                    GUI.Label(new Rect(x + 16f, 494f, 336f, 24f), forecast.invalidReason, smallStyle);
                }

                return;
            }

            GUI.Label(new Rect(x + 16f, 494f, 336f, 22f), $"{forecast.actorName} -> {forecast.targetName}  [{forecast.commandName}]", labelStyle);
            GUI.Label(new Rect(x + 16f, 520f, 336f, 20f), $"거리 {forecast.distance} / 사거리 {forecast.range}: {forecast.rangeText}", smallStyle);
            string hitText = forecast.neededRollText == "판정 없음"
                ? "명중: 판정 없음"
                : $"명중: d20 + {forecast.attackBonus} + 고저 {forecast.heightBonus} + 지형 {forecast.terrainBonus} vs 방어 {forecast.defense}";
            GUI.Label(new Rect(x + 16f, 542f, 336f, 20f), hitText, smallStyle);
            GUI.Label(new Rect(x + 16f, 564f, 336f, 20f), $"필요값: {forecast.neededRollText}", smallStyle);
            GUI.Label(new Rect(x + 16f, 586f, 336f, 20f), forecast.damageText, smallStyle);
            GUI.Label(new Rect(x + 16f, 608f, 336f, 20f), forecast.hpAfterText, smallStyle);
            GUI.Label(new Rect(x + 16f, 630f, 336f, 20f), $"{forecast.counterText}   {forecast.followUpText}", smallStyle);
            GUI.Label(new Rect(x + 16f, 650f, 336f, 20f), forecast.costText, smallStyle);
        }

        private void DrawRosterPanel()
        {
            float y = Screen.height - 118f;
            GUI.Box(new Rect(18f, y, Screen.width - 36f, 100f), GUIContent.none, panelStyle);
            GUI.Label(new Rect(34f, y + 12f, 280f, 26f), "부대 현황", titleStyle);

            float cardWidth = Mathf.Min(220f, (Screen.width - 80f) / Mathf.Max(1, units.Count));
            for (int i = 0; i < units.Count; i++)
            {
                BattleTestUnit unit = units[i];
                float x = 34f + (i * cardWidth);
                string prefix = unit == activeUnit ? "> " : string.Empty;
                GUI.Label(new Rect(x, y + 42f, cardWidth - 8f, 22f), $"{prefix}{unit.definition.displayName}", unit == activeUnit ? labelStyle : smallStyle);
                GUI.Label(new Rect(x, y + 64f, cardWidth - 8f, 22f), $"체력 {unit.hp}/{unit.definition.maxHp}  {UnitStatusText(unit)}", smallStyle);
            }
        }

        private void EnsureCanvasHud()
        {
            if (hudCanvas != null)
            {
                return;
            }

            EnsureEventSystem();
            hudCommandButtons.Clear();

            GameObject canvasObject = new GameObject("BattleCanvasHud", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            hudCanvas = canvasObject.GetComponent<Canvas>();
            hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            hudCanvas.sortingOrder = 50;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform root = canvasObject.GetComponent<RectTransform>();
            RectTransform active = CreatePanel(root, "현재 행동 패널", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -18f), new Vector2(340f, 326f));
            hudPhaseText = CreateHudText(active, "페이즈", new Rect(16f, 12f, 308f, 30f), string.Empty, 22f, TextAlignmentOptions.Left, true);
            hudActiveText = CreateHudText(active, "행동 유닛", new Rect(16f, 48f, 308f, 54f), string.Empty, 17f, TextAlignmentOptions.TopLeft, false);
            hudResourceText = CreateHudText(active, "자원", new Rect(16f, 106f, 308f, 54f), string.Empty, 15f, TextAlignmentOptions.TopLeft, false);
            hudMoveButton = CreateHudButton(active, "이동", new Rect(16f, 176f, 70f, 30f), "이동", () => SetCommandMode(BattleCommandMode.Move));
            hudAttackButton = CreateHudButton(active, "공격", new Rect(94f, 176f, 70f, 30f), "공격", () => SetCommandMode(BattleCommandMode.Attack));
            hudSkillButton = CreateHudButton(active, "무공", new Rect(172f, 176f, 70f, 30f), "무공", () => SetCommandMode(BattleCommandMode.Skill));
            hudGuardButton = CreateHudButton(active, "방어", new Rect(250f, 176f, 70f, 30f), "방어", GuardActiveUnit);
            hudInteractButton = CreateHudButton(active, "지형", new Rect(16f, 218f, 94f, 32f), "지형", () => SetCommandMode(BattleCommandMode.Interact));
            hudPhaseEndButton = CreateHudButton(active, "페이즈 종료", new Rect(118f, 218f, 104f, 32f), "페이즈 종료", EndTurn);
            hudResetButton = CreateHudButton(active, "재시작", new Rect(230f, 218f, 90f, 32f), "재시작", BuildBattle);
            hudObjectiveText = CreateHudText(active, "전술 힌트", new Rect(16f, 264f, 308f, 50f), string.Empty, 14f, TextAlignmentOptions.TopLeft, false);

            RectTransform objective = CreatePanel(root, "목표 패널", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(500f, 112f));
            CreateHudText(objective, "목표 제목", new Rect(16f, 12f, 468f, 24f), "목표", 20f, TextAlignmentOptions.Left, true);
            CreateHudText(objective, "목표 본문", new Rect(16f, 42f, 468f, 76f), "중원 사절 호위대를 제압\n추천: 대나무숲 엄폐, 지붕 고저 +2, 향로/등불 활용\n위험: 화염 칸 진입 시 피해", 15f, TextAlignmentOptions.TopLeft, false);

            RectTransform phase = CreatePanel(root, "페이즈 패널", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -18f), new Vector2(368f, 194f));
            CreateHudText(phase, "페이즈 제목", new Rect(16f, 12f, 336f, 24f), "행동 순서", 20f, TextAlignmentOptions.Left, true);
            hudPhaseListText = CreateHudText(phase, "페이즈 목록", new Rect(16f, 42f, 336f, 144f), string.Empty, 14f, TextAlignmentOptions.TopLeft, false);

            RectTransform log = CreatePanel(root, "전투 기록 패널", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -224f), new Vector2(368f, 244f));
            CreateHudText(log, "전투 기록 제목", new Rect(16f, 12f, 336f, 24f), "전투 기록", 20f, TextAlignmentOptions.Left, true);
            hudLogText = CreateHudText(log, "전투 기록", new Rect(16f, 42f, 336f, 200f), string.Empty, 14f, TextAlignmentOptions.TopLeft, false);

            RectTransform inspect = CreatePanel(root, "정보 패널", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -356f), new Vector2(340f, 190f));
            CreateHudText(inspect, "정보 제목", new Rect(16f, 12f, 308f, 24f), "정보", 20f, TextAlignmentOptions.Left, true);
            hudInspectText = CreateHudText(inspect, "정보 본문", new Rect(16f, 42f, 308f, 144f), string.Empty, 14f, TextAlignmentOptions.TopLeft, false);

            RectTransform forecast = CreatePanel(root, "전투 예측 패널", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -452f), new Vector2(368f, 224f));
            CreateHudText(forecast, "전투 예측 제목", new Rect(16f, 12f, 336f, 24f), "전투 예측", 20f, TextAlignmentOptions.Left, true);
            hudForecastText = CreateHudText(forecast, "전투 예측", new Rect(16f, 40f, 336f, 184f), string.Empty, 13f, TextAlignmentOptions.TopLeft, false);

            RectTransform roster = CreatePanel(root, "부대 현황 패널", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(-36f, 100f));
            CreateHudText(roster, "부대 현황 제목", new Rect(16f, 12f, 240f, 24f), "부대 현황", 20f, TextAlignmentOptions.Left, true);
            hudRosterText = CreateHudText(roster, "부대 현황", new Rect(16f, 42f, 1220f, 54f), string.Empty, 14f, TextAlignmentOptions.TopLeft, false);

            RefreshCanvasHud();
        }

        private void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(eventSystemObject);
        }

        private RectTransform CreatePanel(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            Image image = panel.GetComponent<Image>();
            image.color = new Color(0.09f, 0.085f, 0.075f, 0.86f);
            return rect;
        }

        private TMP_Text CreateHudText(RectTransform parent, string name, Rect frame, string text, float size, TextAlignmentOptions alignment, bool bold)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            SetTopLeft(rect, frame);

            TMP_Text tmp = textObject.GetComponent<TMP_Text>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            tmp.alignment = alignment;
            tmp.color = bold ? new Color(0.96f, 0.90f, 0.78f, 1f) : new Color(0.88f, 0.84f, 0.74f, 1f);
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            return tmp;
        }

        private Button CreateHudButton(RectTransform parent, string name, Rect frame, string label, Action action)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            SetTopLeft(rect, frame);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.18f, 0.16f, 0.13f, 0.96f);

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.18f, 0.16f, 0.13f, 0.96f);
            colors.highlightedColor = new Color(0.33f, 0.28f, 0.20f, 1f);
            colors.selectedColor = new Color(0.50f, 0.36f, 0.17f, 1f);
            colors.pressedColor = new Color(0.62f, 0.42f, 0.18f, 1f);
            colors.disabledColor = new Color(0.12f, 0.115f, 0.10f, 0.55f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            button.onClick.AddListener(() => action());

            TMP_Text text = CreateHudText(rect, "라벨", new Rect(0f, 4f, frame.width, frame.height - 8f), label, 14f, TextAlignmentOptions.Center, true);
            text.color = new Color(0.98f, 0.93f, 0.82f, 1f);

            if (name == "이동" || name == "공격" || name == "무공" || name == "지형")
            {
                hudCommandButtons.Add(button);
            }

            return button;
        }

        private static void SetTopLeft(RectTransform rect, Rect frame)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(frame.x, -frame.y);
            rect.sizeDelta = new Vector2(frame.width, frame.height);
        }

        private void RefreshCanvasHud()
        {
            if (hudCanvas == null)
            {
                return;
            }

            string phaseText = battleOver ? "전투 종료" : $"제 {round}턴 · {PhaseLabel(phaseTurn.CurrentPhase)}";
            string activeName = activeUnit == null ? "선택 없음" : activeUnit.definition.displayName;
            string side = activeUnit == null ? "-" : FactionLabel(activeUnit.definition.faction);
            string hp = activeUnit == null ? "-" : $"{activeUnit.hp}/{activeUnit.definition.maxHp}";
            SetText(hudPhaseText, phaseText);
            SetText(hudActiveText, $"현재 행동: {activeName}\n진영: {side}   체력: {hp}");

            if (activeUnit == null)
            {
                SetText(hudResourceText, "아군을 선택하세요.\n페이즈 종료로 적 페이즈 진행");
            }
            else
            {
                SetText(hudResourceText, $"이동: {ActionText(activeUnit.moved)}   행동: {ActionText(activeUnit.acted)}\n내공: {activeUnit.inner}/{activeUnit.definition.maxInner}   명령: {CommandLabel(commandMode)}");
            }

            SetText(hudObjectiveText, "전술: 고지/대나무숲/지형지물을 먼저 확인");
            SetText(hudPhaseListText, BuildPhaseListText());
            SetText(hudLogText, BuildLogText());
            SetText(hudInspectText, BuildInspectText());
            SetText(hudForecastText, BuildHudForecastText());
            SetText(hudRosterText, BuildRosterText());

            bool playerTurn = phaseTurn.IsPlayerPhase && activeUnit != null && activeUnit.definition.faction == Faction.Ally && !busy && !battleOver;
            SetButtonEnabled(hudMoveButton, playerTurn && !activeUnit.moved);
            SetButtonEnabled(hudAttackButton, playerTurn && !activeUnit.acted);
            SetButtonEnabled(hudSkillButton, playerTurn && CanUseSpecial(activeUnit));
            SetButtonEnabled(hudGuardButton, playerTurn && !activeUnit.acted);
            SetButtonEnabled(hudInteractButton, playerTurn && !activeUnit.acted && HasUsableInteractable(activeUnit));
            SetButtonEnabled(hudPhaseEndButton, phaseTurn.IsPlayerPhase && !busy && !battleOver);
            SetButtonEnabled(hudResetButton, true);
            RefreshCommandButtonState(hudMoveButton, commandMode == BattleCommandMode.Move && playerTurn);
            RefreshCommandButtonState(hudAttackButton, commandMode == BattleCommandMode.Attack && playerTurn);
            RefreshCommandButtonState(hudSkillButton, commandMode == BattleCommandMode.Skill && playerTurn);
            RefreshCommandButtonState(hudInteractButton, commandMode == BattleCommandMode.Interact && playerTurn);
        }

        private string BuildPhaseListText()
        {
            StringBuilder builder = new StringBuilder();
            List<BattleTestUnit> queue = GetTurnQueuePreview(7);
            for (int i = 0; i < queue.Count; i++)
            {
                BattleTestUnit unit = queue[i];
                string marker = unit == activeUnit ? "현재" : (unit.acted ? "완료" : "대기");
                builder.Append(marker).Append("  ")
                    .Append(unit.definition.displayName).Append("  ")
                    .Append(FactionLabel(unit.definition.faction)).Append("  ")
                    .AppendLine(unit.defeated ? "전투불능" : UnitStatusText(unit));
            }

            return builder.ToString();
        }

        private string BuildLogText()
        {
            StringBuilder builder = new StringBuilder();
            int start = Mathf.Max(0, battleLog.Count - 8);
            for (int i = start; i < battleLog.Count; i++)
            {
                builder.AppendLine(battleLog[i]);
            }

            return builder.ToString();
        }

        private string BuildInspectText()
        {
            if (hoveredUnit != null)
            {
                return $"{hoveredUnit.definition.displayName} ({FactionLabel(hoveredUnit.definition.faction)})\n체력 {hoveredUnit.hp}/{hoveredUnit.definition.maxHp}   방어 {DefenseValue(hoveredUnit, TileAt(hoveredUnit.cell))}\n상태: {UnitStatusText(hoveredUnit)}\n무공: {hoveredUnit.definition.specialName}";
            }

            if (hoveredTile != null)
            {
                BattleTestInteractable prop = GetInteractableAt(hoveredTile.cell);
                StringBuilder builder = new StringBuilder();
                builder.Append(TerrainLabel(hoveredTile.terrain)).Append("  (").Append(hoveredTile.cell.x).Append(",").Append(hoveredTile.cell.y).AppendLine(")");
                builder.Append("이동 비용 ").Append(hoveredTile.moveCost).Append("   엄폐 +").AppendLine(hoveredTile.coverBonus.ToString());
                builder.Append("고저 ").Append(hoveredTile.elevation).Append("   진입 ").AppendLine(YesNo(hoveredTile.walkable));
                if (prop != null)
                {
                    int distance = activeUnit == null ? -1 : GridDistance(activeUnit.cell, prop.cell);
                    bool usable = activeUnit != null && !activeUnit.acted && distance <= 1;
                    builder.Append("지형지물: ").AppendLine(prop.displayName);
                    builder.Append("효과: ").Append(InteractableEffectText(prop.kind)).Append("   사용: ").Append(YesNo(usable));
                }
                else
                {
                    builder.Append("위험: ").Append(TileHazardText(hoveredTile));
                }

                return builder.ToString();
            }

            return "가리킨 대상 없음";
        }

        private string BuildHudForecastText()
        {
            BattleForecast forecast = BuildForecast(activeUnit, hoveredUnit);
            if (!forecast.valid)
            {
                if (string.IsNullOrEmpty(forecast.actorName))
                {
                    return forecast.invalidReason;
                }

                return $"[{forecast.actorName}: {forecast.commandName}]\n대상: {forecast.targetName}\n불가: {forecast.invalidReason}\n거리 {forecast.distance} / 사거리 {forecast.range}\n{forecast.costText}";
            }

            return $"[{forecast.actorName}: {forecast.commandName}]\n대상: {forecast.targetName}\n명중 {forecast.neededRollText}\n{forecast.damageText}\n{forecast.hpAfterText}\n{forecast.counterText}\n{forecast.followUpText}\n{forecast.costText}";
        }

        private string BuildRosterText()
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < units.Count; i++)
            {
                BattleTestUnit unit = units[i];
                if (i > 0)
                {
                    builder.Append("   |   ");
                }

                if (unit == activeUnit)
                {
                    builder.Append("> ");
                }

                builder.Append(unit.definition.displayName).Append(" 체력 ")
                    .Append(unit.hp).Append("/").Append(unit.definition.maxHp)
                    .Append(" ").Append(UnitStatusText(unit));
            }

            return builder.ToString();
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }

        private static void SetButtonEnabled(Button button, bool enabled)
        {
            if (button != null)
            {
                button.interactable = enabled;
            }
        }

        private static void RefreshCommandButtonState(Button button, bool selected)
        {
            if (button == null)
            {
                return;
            }

            Image image = button.GetComponent<Image>();
            if (image != null && button.interactable)
            {
                image.color = selected ? new Color(0.56f, 0.39f, 0.18f, 1f) : new Color(0.18f, 0.16f, 0.13f, 0.96f);
            }
        }

        private void BuildBattle()
        {
            StopAllCoroutines();
            ClearGeneratedObjects();

            units.Clear();
            battleLog.Clear();
            interactables.Clear();
            activeUnit = null;
            round = 1;
            busy = false;
            aiQueued = false;
            battleOver = false;
            commandMode = BattleCommandMode.Move;
            phaseTurn.Reset();
            hoveredTile = null;
            hoveredUnit = null;

            diamondSprite = diamondSprite == null ? CreateDiamondSprite() : diamondSprite;
            CreateTerrain();
            SpawnUnits();
            units.Sort((left, right) => right.initiative.CompareTo(left.initiative));
            CenterCamera();
            EnsureCanvasHud();

            AddLog("[체계] 전투 준비 완료.");
            BeginPlayerPhase();
        }

        private void ClearGeneratedObjects()
        {
            List<Transform> children = new List<Transform>();
            for (int i = 0; i < transform.childCount; i++)
            {
                children.Add(transform.GetChild(i));
            }

            foreach (Transform child in children)
            {
                Destroy(child.gameObject);
            }

            ClearCanvasHudReferences();
        }

        private void ClearCanvasHudReferences()
        {
            hudCanvas = null;
            hudPhaseText = null;
            hudActiveText = null;
            hudResourceText = null;
            hudObjectiveText = null;
            hudPhaseListText = null;
            hudLogText = null;
            hudInspectText = null;
            hudForecastText = null;
            hudRosterText = null;
            hudMoveButton = null;
            hudAttackButton = null;
            hudSkillButton = null;
            hudGuardButton = null;
            hudInteractButton = null;
            hudPhaseEndButton = null;
            hudResetButton = null;
            hudCommandButtons.Clear();
        }

        private void CreateTerrain()
        {
            tiles = new BattleTestTile[width, height];
            Transform terrainRoot = new GameObject("Terrain").transform;
            terrainRoot.SetParent(transform, false);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    TerrainProfile profile = ResolveTerrain(x, y);
                    Vector2Int cell = new Vector2Int(x, y);
                    GameObject tileObject = new GameObject($"Tile_{x}_{y}_{profile.terrain}");
                    tileObject.transform.SetParent(terrainRoot, false);
                    tileObject.transform.position = GridToWorld(cell);
                    tileObject.transform.localScale = new Vector3(tileWidth, tileWidth, 1f);

                    SpriteRenderer renderer = tileObject.AddComponent<SpriteRenderer>();
                    renderer.sprite = diamondSprite;
                    renderer.color = profile.color;
                    renderer.sortingOrder = (x + y) * 2;

                    PolygonCollider2D collider = tileObject.AddComponent<PolygonCollider2D>();
                    collider.points = new[]
                    {
                        new Vector2(0f, 0.25f),
                        new Vector2(0.5f, 0f),
                        new Vector2(0f, -0.25f),
                        new Vector2(-0.5f, 0f)
                    };

                    GameObject highlight = new GameObject("Highlight");
                    highlight.transform.SetParent(tileObject.transform, false);
                    highlight.transform.localPosition = new Vector3(0f, 0f, -0.02f);
                    SpriteRenderer highlightRenderer = highlight.AddComponent<SpriteRenderer>();
                    highlightRenderer.sprite = diamondSprite;
                    highlightRenderer.color = Color.clear;
                    highlightRenderer.sortingOrder = renderer.sortingOrder + 1;

                    BattleTestTile tile = tileObject.AddComponent<BattleTestTile>();
                    tile.cell = cell;
                    tile.terrain = profile.terrain;
                    tile.elevation = profile.elevation;
                    tile.walkable = profile.walkable;
                    tile.moveCost = profile.moveCost;
                    tile.coverBonus = profile.coverBonus;
                    tile.baseCoverBonus = profile.coverBonus;
                    tile.baseColor = profile.color;
                    tile.terrainRenderer = renderer;
                    tile.highlightRenderer = highlightRenderer;
                    tiles[x, y] = tile;
                }
            }

            CreateInteractables(terrainRoot);
        }

        private void CreateInteractables(Transform terrainRoot)
        {
            Transform propRoot = new GameObject("Interactables").transform;
            propRoot.SetParent(terrainRoot, false);

            AddInteractable(propRoot, "incense", "제단 향로", BattleTestInteractableKind.Smoke, new Vector2Int(5, 2), new Color(0.74f, 0.68f, 0.58f, 1f));
            AddInteractable(propRoot, "lantern", "붉은 등불", BattleTestInteractableKind.Fire, new Vector2Int(7, 2), new Color(1f, 0.32f, 0.18f, 1f));
            AddInteractable(propRoot, "wine_cart", "술수레", BattleTestInteractableKind.Cover, new Vector2Int(3, 5), new Color(0.64f, 0.38f, 0.18f, 1f));
        }

        private void AddInteractable(Transform parent, string id, string displayName, BattleTestInteractableKind kind, Vector2Int cell, Color color)
        {
            if (!IsInside(cell))
            {
                return;
            }

            BattleTestInteractable interactable = new BattleTestInteractable(id, displayName, kind, cell);
            interactables.Add(interactable);

            GameObject propObject = new GameObject(displayName);
            propObject.transform.SetParent(parent, false);
            propObject.transform.position = GridToWorld(cell) + new Vector3(0f, 0.13f, -0.04f);
            propObject.transform.localScale = new Vector3(tileWidth * 0.34f, tileWidth * 0.34f, 1f);

            SpriteRenderer renderer = propObject.AddComponent<SpriteRenderer>();
            renderer.sprite = diamondSprite;
            renderer.color = color;
            renderer.sortingOrder = 2200 + ((cell.x + cell.y) * 2);
            interactable.renderer = renderer;
        }

        private void SpawnUnits()
        {
            Transform unitRoot = new GameObject("Units").transform;
            unitRoot.SetParent(transform, false);

            foreach (BattleTestUnitDefinition definition in unitDefinitions)
            {
                if (!IsInside(definition.startCell))
                {
                    continue;
                }

                GameObject unitObject = new GameObject(definition.displayName);
                unitObject.transform.SetParent(unitRoot, false);
                unitObject.transform.position = UnitWorldPosition(definition.startCell);

                CharacterVisualController visual = unitObject.AddComponent<CharacterVisualController>();
                visual.visual = definition.visual;
                visual.sortingLayerName = "Default";
                visual.baseSortingOrder = 3000;
                visual.ApplyVisual();

                CircleCollider2D collider = unitObject.AddComponent<CircleCollider2D>();
                collider.radius = 0.26f;
                collider.offset = new Vector2(0f, 0.28f);

                BattleTestUnitView view = unitObject.AddComponent<BattleTestUnitView>();
                BattleTestUnit unit = new BattleTestUnit(definition, view);
                unit.initiative = definition.initiative + random.Next(0, 5);
                view.Bind(unit, visual);
                units.Add(unit);
            }
        }

        private void BeginPlayerPhase()
        {
            if (CheckBattleEnd())
            {
                return;
            }

            phaseTurn.BeginPlayerPhase();
            round = phaseTurn.Round;
            aiQueued = false;
            commandMode = BattleCommandMode.Move;
            PrepareFactionForPhase(Faction.Ally);
            if (CheckBattleEnd())
            {
                return;
            }

            activeUnit = FindNextReadyUnit(Faction.Ally);
            AddLog($"[페이즈] 제 {round}턴 아군 페이즈");
            RefreshHighlights();
            RefreshUnits();
        }

        private void BeginEnemyPhase()
        {
            if (CheckBattleEnd())
            {
                return;
            }

            phaseTurn.BeginEnemyPhase();
            aiQueued = false;
            activeUnit = null;
            commandMode = BattleCommandMode.Move;
            PrepareFactionForPhase(Faction.Enemy);
            if (CheckBattleEnd())
            {
                return;
            }

            ClearHighlights();
            AddLog($"[페이즈] 제 {round}턴 적 페이즈");
            RefreshUnits();
        }

        private void EndTurn()
        {
            if (battleOver || !phaseTurn.IsPlayerPhase)
            {
                return;
            }

            EndPlayerPhase();
        }

        private void EndPlayerPhase()
        {
            activeUnit = null;
            ClearHighlights();
            BeginEnemyPhase();
        }

        private void EndEnemyPhase()
        {
            phaseTurn.CompleteEnemyPhase();
            round = phaseTurn.Round;
            TickRoundEffects();
            BeginPlayerPhase();
        }

        private void PrepareFactionForPhase(Faction faction)
        {
            foreach (BattleTestUnit unit in units)
            {
                if (unit.defeated || unit.definition.faction != faction)
                {
                    continue;
                }

                ApplyStartOfTurn(unit);
                if (unit.defeated)
                {
                    continue;
                }

                unit.moved = false;
                unit.acted = false;
            }
        }

        private BattleTestUnit FindNextReadyUnit(Faction faction)
        {
            foreach (BattleTestUnit unit in units)
            {
                if (!unit.defeated && unit.definition.faction == faction && !unit.acted)
                {
                    return unit;
                }
            }

            return null;
        }

        private void HandlePointer(Vector3 screenPosition)
        {
            Vector3 world = Camera.main.ScreenToWorldPoint(screenPosition);
            Vector2 point = new Vector2(world.x, world.y);
            Collider2D[] hits = Physics2D.OverlapPointAll(point);

            BattleTestUnit clickedUnit = null;
            BattleTestTile clickedTile = null;

            foreach (Collider2D hit in hits)
            {
                BattleTestUnitView view = hit.GetComponentInParent<BattleTestUnitView>();
                if (view != null && view.Unit != null && !view.Unit.defeated)
                {
                    clickedUnit = view.Unit;
                    break;
                }
            }

            foreach (Collider2D hit in hits)
            {
                BattleTestTile tile = hit.GetComponent<BattleTestTile>();
                if (tile != null)
                {
                    clickedTile = tile;
                    break;
                }
            }

            if (phaseTurn.IsPlayerPhase && clickedUnit != null && clickedUnit.definition.faction == Faction.Ally)
            {
                SelectPlayerUnit(clickedUnit);
                return;
            }

            if (activeUnit == null)
            {
                AddLog("[UI] 행동할 아군을 선택하세요.");
                return;
            }

            if (commandMode == BattleCommandMode.Attack)
            {
                if (clickedUnit != null && clickedUnit.definition.faction != activeUnit.definition.faction)
                {
                    TryAttack(activeUnit, clickedUnit, false);
                }
                else
                {
                    AddLog("[UI] 공격할 적을 선택하세요.");
                }

                return;
            }

            if (commandMode == BattleCommandMode.Skill)
            {
                if (clickedUnit != null)
                {
                    TrySpecial(activeUnit, clickedUnit);
                }
                else
                {
                    AddLog("[UI] 무공 대상을 선택하세요.");
                }

                return;
            }

            if (commandMode == BattleCommandMode.Interact)
            {
                if (clickedTile != null)
                {
                    TryInteract(activeUnit, clickedTile);
                }
                else
                {
                    AddLog("[UI] 사용할 지형지물을 선택하세요.");
                }

                return;
            }

            if (commandMode == BattleCommandMode.Move && clickedTile != null)
            {
                TryMove(activeUnit, clickedTile);
                return;
            }

            if (clickedUnit != null && clickedUnit.definition.faction != activeUnit.definition.faction)
            {
                SetCommandMode(BattleCommandMode.Attack);
            }
        }

        private void SelectPlayerUnit(BattleTestUnit unit)
        {
            if (!phaseTurn.CanPlayerControl(unit))
            {
                AddLog("[UI] 이미 행동한 아군입니다.");
                return;
            }

            activeUnit = unit;
            commandMode = BattleCommandMode.Move;
            AddLog($"[선택] {unit.definition.displayName}");
            RefreshHighlights();
            RefreshUnits();
        }

        private void TryMove(BattleTestUnit unit, BattleTestTile destination)
        {
            if (unit.moved)
            {
                AddLog("[이동] 이미 이동했습니다.");
                return;
            }

            if (!destination.walkable || UnitAt(destination.cell) != null)
            {
                AddLog("[이동] 진입할 수 없는 칸입니다.");
                return;
            }

            Dictionary<Vector2Int, int> reachable = GetReachableCells(unit);
            if (!reachable.ContainsKey(destination.cell))
            {
                AddLog("[이동] 이동 범위를 벗어났습니다.");
                return;
            }

            unit.cell = destination.cell;
            unit.moved = true;
            ApplyTileEntry(unit, destination);
            StartCoroutine(AnimateMove(unit, UnitWorldPosition(destination.cell)));
            AddLog($"[이동] {unit.definition.displayName} 이동.");
        }

        private bool TryAttack(BattleTestUnit attacker, BattleTestUnit target, bool endAfterAttack)
        {
            if (attacker.acted)
            {
                AddLog("[행동] 이미 행동했습니다.");
                return false;
            }

            if (target.defeated)
            {
                return false;
            }

            int distance = GridDistance(attacker.cell, target.cell);
            if (distance > attacker.definition.attackRange)
            {
                AddLog("[공격] 대상이 사거리 밖입니다.");
                return false;
            }

            ResolveAttack(attacker, target, false);
            ResolvePostAttack(attacker, target, false);
            attacker.acted = true;
            RefreshUnits();

            if (CheckBattleEnd())
            {
                return true;
            }

            if (AdvanceAfterAction(attacker))
            {
                return true;
            }

            if (endAfterAttack)
            {
                EndTurn();
            }
            else
            {
                RefreshHighlights();
            }

            return true;
        }

        private bool ResolveAttack(BattleTestUnit attacker, BattleTestUnit target, bool special)
        {
            BattleTestTile from = TileAt(attacker.cell);
            BattleTestTile to = TileAt(target.cell);
            string moveName = special ? attacker.definition.specialName : "공격";
            int d20 = random.Next(1, 21);
            int heightBonus = from != null && to != null && from.elevation > to.elevation ? 2 : 0;
            int attackBonus = attacker.definition.attackBonus + (special ? attacker.definition.specialAttackBonus : 0);
            int attackTotal = d20 + attackBonus + heightBonus;
            int defense = DefenseValue(target, to);
            bool critical = d20 == 20;
            bool hit = critical || (d20 != 1 && attackTotal >= defense);

            if (!hit)
            {
                AddLog($"[빗나감] {attacker.definition.displayName}의 {moveName} 실패.");
                return false;
            }

            int damage = random.Next(attacker.definition.damageMin, attacker.definition.damageMax + 1);
            if (special)
            {
                damage += attacker.definition.specialPower;
            }

            if (critical)
            {
                damage *= 2;
            }

            damage += heightBonus;
            if (target.guarded)
            {
                damage = Mathf.Max(1, Mathf.CeilToInt(damage * 0.55f));
            }

            target.hp = Mathf.Max(0, target.hp - damage);
            AddLog($"[명중] {attacker.definition.displayName}: {moveName}! 피해 {damage}.");

            if (target.hp == 0)
            {
                target.defeated = true;
                target.view.SetDefeated(true);
                AddLog($"[전투불능] {target.definition.displayName} 쓰러짐.");
            }

            return true;
        }

        private bool TrySpecial(BattleTestUnit actor, BattleTestUnit target)
        {
            if (!CanUseSpecial(actor))
            {
                AddLog("[무공] 사용할 수 없습니다.");
                return false;
            }

            if (!IsValidSpecialTarget(actor, target))
            {
                AddLog("[무공] 대상이 맞지 않습니다.");
                return false;
            }

            int distance = GridDistance(actor.cell, target.cell);
            if (distance > actor.definition.specialRange)
            {
                AddLog("[무공] 대상이 사거리 밖입니다.");
                return false;
            }

            actor.inner -= actor.definition.specialCost;
            actor.specialCooldownLeft = actor.definition.specialCooldown;
            actor.acted = true;

            bool attackLike = ApplySpecialEffect(actor, target, true);
            if (attackLike)
            {
                ResolvePostAttack(actor, target, true);
            }

            RefreshHighlights();
            RefreshUnits();
            if (CheckBattleEnd())
            {
                return true;
            }

            AdvanceAfterAction(actor);
            return true;
        }

        private bool ApplySpecialEffect(BattleTestUnit actor, BattleTestUnit target, bool allowStatus)
        {
            switch (actor.definition.specialEffect)
            {
                case BattleSpecialEffect.Heal:
                    int healed = Mathf.Min(target.definition.maxHp - target.hp, actor.definition.specialPower + random.Next(4, 9));
                    target.hp += Mathf.Max(0, healed);
                    target.poisoned = false;
                    target.chilled = false;
                    AddLog($"[무공] {actor.definition.displayName}: {actor.definition.specialName}. {target.definition.displayName} 회복 {healed}.");
                    return false;
                case BattleSpecialEffect.Poison:
                    bool poisonHit = ResolveAttack(actor, target, true);
                    if (allowStatus && poisonHit && !target.defeated)
                    {
                        target.poisoned = true;
                        AddLog($"[상태] {target.definition.displayName} 중독.");
                    }
                    return true;
                case BattleSpecialEffect.Freeze:
                    bool freezeHit = ResolveAttack(actor, target, true);
                    if (allowStatus && freezeHit && !target.defeated)
                    {
                        target.chilled = true;
                        AddLog($"[상태] {target.definition.displayName} 둔화.");
                    }
                    return true;
                case BattleSpecialEffect.Mark:
                    target.marked = true;
                    AddLog($"[파훼] {actor.definition.displayName}가 {target.definition.displayName}의 검로를 읽었다.");
                    return false;
                case BattleSpecialEffect.BreakGuard:
                    target.guarded = false;
                    target.marked = true;
                    ResolveAttack(actor, target, true);
                    return true;
                default:
                    ResolveAttack(actor, target, true);
                    return true;
            }
        }

        private void ResolvePostAttack(BattleTestUnit attacker, BattleTestUnit target, bool special)
        {
            if (attacker == null || target == null || attacker.defeated || target.defeated)
            {
                return;
            }

            BattleTestCounterMove counter = FindCounterMove(target, attacker);
            if (counter.valid)
            {
                AddLog($"[반격] {target.definition.displayName}: {counter.label}.");
                if (counter.special)
                {
                    target.inner = Mathf.Max(0, target.inner - target.definition.specialCost);
                    target.specialCooldownLeft = Mathf.Max(target.specialCooldownLeft, target.definition.specialCooldown);
                    ApplySpecialEffect(target, attacker, true);
                }
                else
                {
                    ResolveAttack(target, attacker, false);
                }
            }
            else
            {
                AddLog($"[반격] {target.definition.displayName} 반격 불가.");
            }

            if (!attacker.defeated && !target.defeated && CanFollowUp(attacker, target, special))
            {
                AddLog($"[추격] {attacker.definition.displayName}가 빈틈을 찔렀다.");
                ResolveAttack(attacker, target, special);
            }
        }

        private BattleTestCounterMove FindCounterMove(BattleTestUnit defender, BattleTestUnit attacker)
        {
            if (defender == null || attacker == null || defender.defeated || attacker.defeated)
            {
                return BattleTestCounterMove.None;
            }

            int distance = GridDistance(defender.cell, attacker.cell);
            if (defender.marked)
            {
                return BattleTestCounterMove.None;
            }

            if (distance <= defender.definition.attackRange)
            {
                return new BattleTestCounterMove(false, "기본 공격");
            }

            if (CanUseCounterSpecial(defender) && distance <= defender.definition.specialRange)
            {
                return new BattleTestCounterMove(true, defender.definition.specialName);
            }

            return BattleTestCounterMove.None;
        }

        private bool CanUseCounterSpecial(BattleTestUnit unit)
        {
            if (unit == null || unit.defeated || unit.inner < unit.definition.specialCost || unit.specialCooldownLeft > 0)
            {
                return false;
            }

            return IsHostileAttackSpecial(unit.definition.specialEffect);
        }

        private bool CanFollowUp(BattleTestUnit attacker, BattleTestUnit target, bool special)
        {
            if (attacker == null || target == null || attacker.defeated || target.defeated)
            {
                return false;
            }

            int range = special ? attacker.definition.specialRange : attacker.definition.attackRange;
            if (range >= 4 || (special && (attacker.definition.specialEffect == BattleSpecialEffect.Heal || attacker.definition.specialEffect == BattleSpecialEffect.Mark)))
            {
                return false;
            }

            return AgilityValue(attacker) - AgilityValue(target) >= 5;
        }

        private int AgilityValue(BattleTestUnit unit)
        {
            if (unit == null)
            {
                return 0;
            }

            return unit.definition.agility >= 0 ? unit.definition.agility : unit.definition.initiative;
        }

        private bool TryInteract(BattleTestUnit actor, BattleTestTile clickedTile)
        {
            if (actor == null || actor.acted)
            {
                AddLog("[행동] 이미 행동했습니다.");
                return false;
            }

            BattleTestInteractable interactable = FindUsableInteractable(actor, clickedTile.cell);
            if (interactable == null)
            {
                AddLog("[지형] 닿는 곳에 사용할 지형지물이 없습니다.");
                return false;
            }

            interactable.used = true;
            if (interactable.renderer != null)
            {
                interactable.renderer.color = new Color(0.35f, 0.35f, 0.35f, 0.72f);
            }

            BattleTestTile tile = TileAt(interactable.cell);
            switch (interactable.kind)
            {
                case BattleTestInteractableKind.Smoke:
                    if (tile != null)
                    {
                        tile.smokeTurns = 2;
                        tile.coverBonus = tile.baseCoverBonus + (tile.extraCover ? CoverInteractBonus : 0) + SmokeCoverBonus;
                        RefreshTerrainTint(tile);
                    }
                    AddLog($"[지형] {actor.definition.displayName}가 {interactable.displayName}를 흔들었다. 연기 엄폐 +{SmokeCoverBonus}.");
                    break;
                case BattleTestInteractableKind.Fire:
                    if (tile != null)
                    {
                        tile.fireTurns = 2;
                        RefreshTerrainTint(tile);
                    }
                    DamageUnitsAround(actor, interactable.cell, 1, FireInteractDamage, "화염");
                    AddLog($"[지형] {actor.definition.displayName}가 {interactable.displayName}를 터뜨렸다. 화염 피해 {FireInteractDamage}.");
                    break;
                case BattleTestInteractableKind.Cover:
                    if (tile != null)
                    {
                        if (!tile.extraCover)
                        {
                            tile.coverBonus += CoverInteractBonus;
                            tile.extraCover = true;
                        }

                        RefreshTerrainTint(tile);
                    }
                    AddLog($"[지형] {actor.definition.displayName}가 {interactable.displayName}를 밀었다. 엄폐 +{CoverInteractBonus}.");
                    break;
            }

            actor.acted = true;
            RefreshHighlights();
            RefreshUnits();
            if (CheckBattleEnd())
            {
                return true;
            }

            AdvanceAfterAction(actor);
            return true;
        }

        private void GuardActiveUnit()
        {
            if (activeUnit == null || activeUnit.acted || activeUnit.definition.faction != Faction.Ally)
            {
                return;
            }

            activeUnit.guarded = true;
            activeUnit.acted = true;
            AddLog($"[방어] {activeUnit.definition.displayName} 방어 태세.");
            RefreshHighlights();
            RefreshUnits();
            AdvanceAfterAction(activeUnit);
        }

        private bool AdvanceAfterAction(BattleTestUnit actor)
        {
            if (actor == null || actor.definition.faction != Faction.Ally || !phaseTurn.IsPlayerPhase)
            {
                return false;
            }

            if (phaseTurn.AllFactionUnitsActed(units, Faction.Ally))
            {
                AddLog("[페이즈] 모든 아군 행동 완료");
                EndPlayerPhase();
                return true;
            }

            activeUnit = FindNextReadyUnit(Faction.Ally);
            commandMode = BattleCommandMode.Move;
            RefreshHighlights();
            RefreshUnits();
            return false;
        }

        private IEnumerator AnimateMove(BattleTestUnit unit, Vector3 target)
        {
            busy = true;
            Vector3 start = unit.view.transform.position;
            float elapsed = 0f;
            const float duration = 0.18f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                unit.view.transform.position = Vector3.Lerp(start, target, t);
                yield return null;
            }

            unit.view.transform.position = target;
            busy = false;
            RefreshHighlights();
            RefreshUnits();
        }

        private IEnumerator RunEnemyPhase()
        {
            busy = true;
            yield return new WaitForSeconds(0.3f);

            foreach (BattleTestUnit enemy in units)
            {
                if (enemy.defeated || enemy.definition.faction != Faction.Enemy || enemy.acted)
                {
                    continue;
                }

                activeUnit = enemy;
                commandMode = BattleCommandMode.Move;
                RefreshHighlights();
                RefreshUnits();
                yield return new WaitForSeconds(0.25f);

                BattleTestUnit target = FindNearestEnemy(activeUnit);
                if (target == null)
                {
                    activeUnit.acted = true;
                    continue;
                }

                int desiredRange = CanUseSpecial(activeUnit) ? activeUnit.definition.specialRange : activeUnit.definition.attackRange;
                if (GridDistance(activeUnit.cell, target.cell) > desiredRange && !activeUnit.moved)
                {
                    BattleTestTile best = FindBestMoveToward(activeUnit, target.cell);
                    if (best != null)
                    {
                        activeUnit.cell = best.cell;
                        activeUnit.moved = true;
                        ApplyTileEntry(activeUnit, best);
                        yield return AnimateMove(activeUnit, UnitWorldPosition(best.cell));
                        yield return new WaitForSeconds(0.15f);
                    }
                }

                if (CanUseSpecial(activeUnit) && IsValidSpecialTarget(activeUnit, target) && GridDistance(activeUnit.cell, target.cell) <= activeUnit.definition.specialRange)
                {
                    TrySpecial(activeUnit, target);
                }
                else
                {
                    TryAttack(activeUnit, target, false);
                }

                if (battleOver)
                {
                    busy = false;
                    yield break;
                }

                yield return new WaitForSeconds(0.3f);
            }

            busy = false;
            activeUnit = null;
            aiQueued = false;
            EndEnemyPhase();
        }

        private BattleTestTile FindBestMoveToward(BattleTestUnit unit, Vector2Int targetCell)
        {
            Dictionary<Vector2Int, int> reachable = GetReachableCells(unit);
            BattleTestTile best = null;
            int bestDistance = GridDistance(unit.cell, targetCell);
            int bestCost = int.MaxValue;

            foreach (KeyValuePair<Vector2Int, int> pair in reachable)
            {
                if (pair.Key == unit.cell || UnitAt(pair.Key) != null)
                {
                    continue;
                }

                int distance = GridDistance(pair.Key, targetCell);
                if (distance < bestDistance || (distance == bestDistance && pair.Value < bestCost))
                {
                    bestDistance = distance;
                    bestCost = pair.Value;
                    best = TileAt(pair.Key);
                }
            }

            return best;
        }

        private BattleTestUnit FindNearestEnemy(BattleTestUnit unit)
        {
            BattleTestUnit best = null;
            int bestDistance = int.MaxValue;

            foreach (BattleTestUnit other in units)
            {
                if (other.defeated || other.definition.faction == unit.definition.faction)
                {
                    continue;
                }

                int distance = GridDistance(unit.cell, other.cell);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = other;
                }
            }

            return best;
        }

        private void ApplyStartOfTurn(BattleTestUnit unit)
        {
            unit.guarded = false;
            unit.marked = false;
            unit.inner = Mathf.Min(unit.definition.maxInner, unit.inner + 1);
            unit.specialCooldownLeft = Mathf.Max(0, unit.specialCooldownLeft - 1);

            if (unit.poisoned)
            {
                unit.hp = Mathf.Max(0, unit.hp - 3);
                AddLog($"[상태] {unit.definition.displayName} 중독 피해 3.");
                if (unit.hp == 0)
                {
                    unit.defeated = true;
                    unit.view.SetDefeated(true);
                }
            }
        }

        private void TickRoundEffects()
        {
            if (tiles == null)
            {
                return;
            }

            bool faded = false;
            foreach (BattleTestTile tile in tiles)
            {
                if (tile == null)
                {
                    continue;
                }

                int previousSmoke = tile.smokeTurns;
                int previousFire = tile.fireTurns;
                tile.smokeTurns = Mathf.Max(0, tile.smokeTurns - 1);
                tile.fireTurns = Mathf.Max(0, tile.fireTurns - 1);

                if (previousSmoke > 0 && tile.smokeTurns == 0)
                {
                    tile.coverBonus = tile.baseCoverBonus + (tile.extraCover ? CoverInteractBonus : 0);
                    faded = true;
                }

                if (previousFire > 0 && tile.fireTurns == 0)
                {
                    faded = true;
                }

                if (previousSmoke != tile.smokeTurns || previousFire != tile.fireTurns)
                {
                    RefreshTerrainTint(tile);
                }
            }

            if (faded)
            {
                AddLog("[지형] 연기와 화염이 잦아들었다.");
            }
        }

        private int EffectiveMoveRange(BattleTestUnit unit)
        {
            int range = unit.definition.moveRange;
            if (unit.chilled)
            {
                range = Mathf.Max(2, range - 1);
            }

            return range;
        }

        private bool CanUseSpecial(BattleTestUnit unit)
        {
            return unit != null
                && !unit.acted
                && unit.inner >= unit.definition.specialCost
                && unit.specialCooldownLeft <= 0
                && unit.definition.specialEffect != BattleSpecialEffect.None;
        }

        private bool IsValidSpecialTarget(BattleTestUnit actor, BattleTestUnit target)
        {
            if (actor == null || target == null || target.defeated)
            {
                return false;
            }

            if (actor.definition.specialEffect == BattleSpecialEffect.Heal)
            {
                return target.definition.faction == actor.definition.faction && target.hp < target.definition.maxHp;
            }

            return target.definition.faction != actor.definition.faction;
        }

        private BattleForecast BuildForecast(BattleTestUnit actor, BattleTestUnit target)
        {
            if (actor == null || actor.defeated)
            {
                return BattleForecast.Invalid("행동할 아군을 선택하세요.");
            }

            bool attack = commandMode == BattleCommandMode.Attack;
            bool special = commandMode == BattleCommandMode.Skill;
            if (!attack && !special)
            {
                return BattleForecast.Invalid("공격 또는 무공을 선택한 뒤 대상을 가리키세요.");
            }

            if (target == null)
            {
                return BattleForecast.Invalid("대상을 가리키면 전투 예측이 표시됩니다.");
            }

            string commandName = special ? actor.definition.specialName : "공격";
            int range = special ? actor.definition.specialRange : actor.definition.attackRange;
            int distance = GridDistance(actor.cell, target.cell);
            string costText = special
                ? $"소모: 내공 {actor.definition.specialCost} / 재사용 {actor.definition.specialCooldown}턴"
                : "소모: 행동 1회";

            if (target.defeated)
            {
                return BattleForecast.Invalid(actor.definition.displayName, target.definition.displayName, commandName, "이미 전투불능인 대상입니다.", distance, range, costText);
            }

            if (special)
            {
                string unavailable = SpecialUnavailableReason(actor);
                if (!string.IsNullOrEmpty(unavailable))
                {
                    return BattleForecast.Invalid(actor.definition.displayName, target.definition.displayName, commandName, unavailable, distance, range, costText);
                }

                if (!IsValidSpecialTarget(actor, target))
                {
                    return BattleForecast.Invalid(actor.definition.displayName, target.definition.displayName, commandName, "무공 대상 조건이 맞지 않습니다.", distance, range, costText);
                }
            }
            else if (target.definition.faction == actor.definition.faction)
            {
                return BattleForecast.Invalid(actor.definition.displayName, target.definition.displayName, commandName, "아군은 공격 대상이 아닙니다.", distance, range, costText);
            }

            if (distance > range)
            {
                return BattleForecast.Invalid(actor.definition.displayName, target.definition.displayName, commandName, "사거리 밖입니다.", distance, range, costText);
            }

            BattleTestTile from = TileAt(actor.cell);
            BattleTestTile to = TileAt(target.cell);
            int heightBonus = from != null && to != null && from.elevation > to.elevation ? 2 : 0;
            int terrainBonus = 0;
            int attackBonus = actor.definition.attackBonus + (special ? actor.definition.specialAttackBonus : 0);
            int defense = DefenseValue(target, to);
            bool attackLike = !special || IsHostileAttackSpecial(actor.definition.specialEffect);
            BattleTestCounterMove counter = attackLike ? FindCounterMove(target, actor) : BattleTestCounterMove.None;
            bool followUp = attackLike && CanFollowUp(actor, target, special);
            string neededRollText = attackLike ? NeededRollText(defense, attackBonus, heightBonus, terrainBonus) : "판정 없음";
            if (!attackLike)
            {
                attackBonus = 0;
                heightBonus = 0;
                terrainBonus = 0;
                defense = 0;
            }

            int damageMin = 0;
            int damageMax = 0;
            string damageText = "피해: 없음 | 파훼 +0";
            string hpAfterText = $"예상 전투 후 체력: {target.hp}-{target.hp}";
            if (attackLike)
            {
                damageMin = actor.definition.damageMin + (special ? actor.definition.specialPower : 0) + heightBonus;
                damageMax = actor.definition.damageMax + (special ? actor.definition.specialPower : 0) + heightBonus;
                if (target.guarded)
                {
                    damageMin = Mathf.Max(1, Mathf.CeilToInt(damageMin * 0.55f));
                    damageMax = Mathf.Max(1, Mathf.CeilToInt(damageMax * 0.55f));
                }

                damageMin = Mathf.Max(1, damageMin);
                damageMax = Mathf.Max(1, damageMax);
                int breakGain = special ? 18 : 12;
                damageText = $"피해 {damageMin}-{damageMax} | 치명 5% | 파훼 +{breakGain}";
                hpAfterText = $"예상 전투 후 체력: {Mathf.Max(0, target.hp - damageMax)}-{Mathf.Max(0, target.hp - damageMin)}";
            }

            return new BattleForecast(
                true,
                string.Empty,
                actor.definition.displayName,
                target.definition.displayName,
                commandName,
                distance,
                range,
                distance <= range ? "사거리 안" : "사거리 밖",
                attackBonus,
                heightBonus,
                terrainBonus,
                defense,
                neededRollText,
                damageText,
                hpAfterText,
                CounterForecastText(target, actor, counter, attackLike),
                followUp ? $"추격: 가능 (민첩 {AgilityValue(actor)} vs {AgilityValue(target)})" : "추격: 불가",
                costText);
        }

        private string CounterSummary(BattleTestUnit unit)
        {
            if (unit == null)
            {
                return "-";
            }

            return CanUseCounterSpecial(unit) ? unit.definition.specialName : $"공격 R{unit.definition.attackRange}";
        }

        private string CounterForecastText(BattleTestUnit defender, BattleTestUnit attacker, BattleTestCounterMove counter, bool attackLike)
        {
            if (!attackLike)
            {
                return "상대 반격: 없음";
            }

            if (defender == null || attacker == null || defender.defeated)
            {
                return "상대 반격: 없음";
            }

            if (defender.marked)
            {
                return "상대 반격: 파훼 - 반격 봉쇄";
            }

            int distance = GridDistance(defender.cell, attacker.cell);
            if (!counter.valid)
            {
                if (distance > defender.definition.attackRange && distance > defender.definition.specialRange)
                {
                    return "상대 반격: 불가 - 사거리 밖";
                }

                if (defender.inner < defender.definition.specialCost)
                {
                    return "상대 반격: 불가 - 내공 부족";
                }

                if (defender.specialCooldownLeft > 0)
                {
                    return $"상대 반격: 불가 - 재사용 대기 {defender.specialCooldownLeft}턴";
                }

                return "상대 반격: 불가";
            }

            BattleTestTile from = TileAt(defender.cell);
            BattleTestTile to = TileAt(attacker.cell);
            int heightBonus = from != null && to != null && from.elevation > to.elevation ? 2 : 0;
            int attackBonus = defender.definition.attackBonus + (counter.special ? defender.definition.specialAttackBonus : 0);
            int defense = DefenseValue(attacker, to);
            int damageMin = defender.definition.damageMin + (counter.special ? defender.definition.specialPower : 0) + heightBonus;
            int damageMax = defender.definition.damageMax + (counter.special ? defender.definition.specialPower : 0) + heightBonus;
            damageMin = Mathf.Max(1, damageMin);
            damageMax = Mathf.Max(1, damageMax);
            return $"상대 반격: {counter.label} / 명중 {HitChancePercent(defense, attackBonus, heightBonus, 0)}% / 피해 {damageMin}-{damageMax}";
        }

        private string SpecialUnavailableReason(BattleTestUnit unit)
        {
            if (unit == null)
            {
                return "행동할 유닛이 없습니다.";
            }

            if (unit.acted)
            {
                return "이미 행동했습니다.";
            }

            if (unit.definition.specialEffect == BattleSpecialEffect.None)
            {
                return "장착된 무공이 없습니다.";
            }

            if (unit.inner < unit.definition.specialCost)
            {
                return $"내공 부족 ({unit.inner}/{unit.definition.specialCost}).";
            }

            if (unit.specialCooldownLeft > 0)
            {
                return $"재사용 대기 {unit.specialCooldownLeft}턴.";
            }

            return string.Empty;
        }

        private bool IsHostileAttackSpecial(BattleSpecialEffect effect)
        {
            return effect == BattleSpecialEffect.Strike
                || effect == BattleSpecialEffect.Poison
                || effect == BattleSpecialEffect.Freeze
                || effect == BattleSpecialEffect.BreakGuard;
        }

        private string NeededRollText(int defense, int attackBonus, int heightBonus, int terrainBonus)
        {
            int needed = defense - attackBonus - heightBonus - terrainBonus;
            if (needed <= 2)
            {
                return $"{HitChancePercent(defense, attackBonus, heightBonus, terrainBonus)}% / d20 2+ 필요";
            }

            if (needed > 20)
            {
                return "5% / d20 20 필요";
            }

            return $"{HitChancePercent(defense, attackBonus, heightBonus, terrainBonus)}% / d20 {needed}+ 필요";
        }

        private int HitChancePercent(int defense, int attackBonus, int heightBonus, int terrainBonus)
        {
            int needed = defense - attackBonus - heightBonus - terrainBonus;
            if (needed <= 2)
            {
                return 95;
            }

            if (needed > 20)
            {
                return 5;
            }

            return Mathf.Clamp((21 - needed) * 5, 5, 95);
        }

        private int DefenseValue(BattleTestUnit unit, BattleTestTile tile)
        {
            int defense = unit.definition.defense;
            if (tile != null)
            {
                defense += tile.coverBonus;
            }

            if (unit.guarded)
            {
                defense += 2;
            }

            if (unit.marked)
            {
                defense -= 2;
            }

            return defense;
        }

        private void ApplyTileEntry(BattleTestUnit unit, BattleTestTile tile)
        {
            if (unit == null || tile == null || unit.defeated)
            {
                return;
            }

            if (tile.fireTurns > 0)
            {
                int damage = random.Next(2, 6);
                unit.hp = Mathf.Max(0, unit.hp - damage);
                AddLog($"[지형] {unit.definition.displayName} 화염 진입. 피해 {damage}.");
                if (unit.hp == 0)
                {
                    unit.defeated = true;
                    unit.view.SetDefeated(true);
                    AddLog($"[전투불능] {unit.definition.displayName} 쓰러짐.");
                }
            }
        }

        private void DamageUnitsAround(BattleTestUnit actor, Vector2Int center, int radius, int damage, string reason)
        {
            foreach (BattleTestUnit unit in units)
            {
                if (unit.defeated || unit.definition.faction == actor.definition.faction)
                {
                    continue;
                }

                if (GridDistance(unit.cell, center) > radius)
                {
                    continue;
                }

                unit.hp = Mathf.Max(0, unit.hp - damage);
                AddLog($"[지형] {unit.definition.displayName} {reason} 피해 {damage}.");
                if (unit.hp == 0)
                {
                    unit.defeated = true;
                    unit.view.SetDefeated(true);
                    AddLog($"[전투불능] {unit.definition.displayName} 쓰러짐.");
                }
            }
        }

        private BattleTestInteractable FindUsableInteractable(BattleTestUnit actor, Vector2Int clickedCell)
        {
            foreach (BattleTestInteractable interactable in interactables)
            {
                if (interactable.used || interactable.cell != clickedCell)
                {
                    continue;
                }

                if (GridDistance(actor.cell, interactable.cell) <= 1)
                {
                    return interactable;
                }
            }

            return null;
        }

        private bool HasUsableInteractable(BattleTestUnit actor)
        {
            if (actor == null)
            {
                return false;
            }

            foreach (BattleTestInteractable interactable in interactables)
            {
                if (!interactable.used && GridDistance(actor.cell, interactable.cell) <= 1)
                {
                    return true;
                }
            }

            return false;
        }

        private BattleTestInteractable GetInteractableAt(Vector2Int cell)
        {
            foreach (BattleTestInteractable interactable in interactables)
            {
                if (!interactable.used && interactable.cell == cell)
                {
                    return interactable;
                }
            }

            return null;
        }

        private string TileHazardText(BattleTestTile tile)
        {
            if (tile == null)
            {
                return "-";
            }

            List<string> states = new List<string>();
            if (tile.smokeTurns > 0)
            {
                states.Add("연기");
            }

            if (tile.fireTurns > 0)
            {
                states.Add("화염");
            }

            if (tile.extraCover)
            {
                states.Add("중엄폐");
            }

            return states.Count == 0 ? "없음" : string.Join(", ", states);
        }

        private string InteractableEffectText(BattleTestInteractableKind kind)
        {
            switch (kind)
            {
                case BattleTestInteractableKind.Smoke:
                    return $"연기 엄폐 +{SmokeCoverBonus}";
                case BattleTestInteractableKind.Fire:
                    return $"화염 피해 {FireInteractDamage}";
                case BattleTestInteractableKind.Cover:
                    return $"엄폐 +{CoverInteractBonus}";
                default:
                    return "-";
            }
        }

        private void RefreshTerrainTint(BattleTestTile tile)
        {
            if (tile != null && tile.terrainRenderer != null)
            {
                if (tile.fireTurns > 0)
                {
                    tile.terrainRenderer.color = new Color(0.72f, 0.20f, 0.12f, 1f);
                }
                else if (tile.smokeTurns > 0)
                {
                    tile.terrainRenderer.color = new Color(0.54f, 0.54f, 0.50f, 1f);
                }
                else if (tile.extraCover)
                {
                    tile.terrainRenderer.color = new Color(0.44f, 0.29f, 0.17f, 1f);
                }
                else
                {
                    tile.terrainRenderer.color = tile.baseColor;
                }
            }
        }

        private string UnitStatusText(BattleTestUnit unit)
        {
            if (unit.defeated)
            {
                return "전투불능";
            }

            List<string> states = new List<string>();
            if (unit.guarded)
            {
                states.Add("방어");
            }

            if (unit.poisoned)
            {
                states.Add("독");
            }

            if (unit.chilled)
            {
                states.Add("둔화");
            }

            if (unit.marked)
            {
                states.Add("파훼");
            }

            if (unit.moved && unit.acted)
            {
                states.Add("완료");
            }
            else if (unit.moved)
            {
                states.Add("이동함");
            }
            else if (unit.acted)
            {
                states.Add("행동함");
            }

            return states.Count == 0 ? "대기" : string.Join(", ", states);
        }

        private string ActionText(bool spent)
        {
            return spent ? "완료" : "가능";
        }

        private string PhaseLabel(BattlePhase phase)
        {
            switch (phase)
            {
                case BattlePhase.PlayerPhase:
                    return "아군 페이즈";
                case BattlePhase.EnemyPhase:
                    return "적 페이즈";
                case BattlePhase.NeutralPhase:
                    return "중립 페이즈";
                default:
                    return phase.ToString();
            }
        }

        private string FactionLabel(Faction faction)
        {
            switch (faction)
            {
                case Faction.Ally:
                    return "아군";
                case Faction.Enemy:
                    return "적";
                case Faction.Neutral:
                    return "중립";
                default:
                    return faction.ToString();
            }
        }

        private string CommandLabel(BattleCommandMode mode)
        {
            switch (mode)
            {
                case BattleCommandMode.Move:
                    return "이동";
                case BattleCommandMode.Attack:
                    return "공격";
                case BattleCommandMode.Skill:
                    return "무공";
                case BattleCommandMode.Interact:
                    return "지형";
                default:
                    return mode.ToString();
            }
        }

        private string TerrainLabel(TerrainType terrain)
        {
            switch (terrain)
            {
                case TerrainType.Stone:
                    return "석로";
                case TerrainType.Wood:
                    return "목재";
                case TerrainType.Water:
                    return "물가";
                case TerrainType.Bamboo:
                    return "대나무숲";
                case TerrainType.Bridge:
                    return "다리";
                case TerrainType.Roof:
                    return "지붕";
                case TerrainType.Cliff:
                    return "절벽";
                case TerrainType.Wall:
                    return "담장";
                default:
                    return terrain.ToString();
            }
        }

        private void SetCommandMode(BattleCommandMode mode)
        {
            if (activeUnit == null || activeUnit.definition.faction != Faction.Ally)
            {
                return;
            }

            if (mode == BattleCommandMode.Interact && (activeUnit.acted || !HasUsableInteractable(activeUnit)))
            {
                AddLog("[UI] 사용할 수 있는 지형지물이 없습니다.");
                return;
            }

            commandMode = mode;
            RefreshHighlights();
        }

        private List<BattleTestUnit> GetTurnQueuePreview(int count)
        {
            List<BattleTestUnit> queue = new List<BattleTestUnit>();
            if (units.Count == 0)
            {
                return queue;
            }

            Faction primary = phaseTurn.IsEnemyPhase ? Faction.Enemy : Faction.Ally;
            Faction secondary = phaseTurn.IsEnemyPhase ? Faction.Ally : Faction.Enemy;
            AppendFactionPreview(queue, primary, count);
            AppendFactionPreview(queue, secondary, count);
            return queue;
        }

        private void AppendFactionPreview(List<BattleTestUnit> queue, Faction faction, int count)
        {
            foreach (BattleTestUnit unit in units)
            {
                if (queue.Count >= count)
                {
                    return;
                }

                if (!unit.defeated && unit.definition.faction == faction)
                {
                    queue.Add(unit);
                }
            }
        }

        private void UpdateHover()
        {
            hoveredUnit = null;
            hoveredTile = null;

            if (Camera.main == null)
            {
                return;
            }

            Vector3 world = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Collider2D[] hits = Physics2D.OverlapPointAll(new Vector2(world.x, world.y));

            foreach (Collider2D hit in hits)
            {
                BattleTestUnitView view = hit.GetComponentInParent<BattleTestUnitView>();
                if (view != null && view.Unit != null && !view.Unit.defeated)
                {
                    hoveredUnit = view.Unit;
                    break;
                }
            }

            foreach (Collider2D hit in hits)
            {
                BattleTestTile tile = hit.GetComponent<BattleTestTile>();
                if (tile != null)
                {
                    hoveredTile = tile;
                    break;
                }
            }
        }

        private Dictionary<Vector2Int, int> GetReachableCells(BattleTestUnit unit)
        {
            Dictionary<Vector2Int, int> cost = new Dictionary<Vector2Int, int>();
            Queue<Vector2Int> frontier = new Queue<Vector2Int>();
            cost[unit.cell] = 0;
            frontier.Enqueue(unit.cell);

            while (frontier.Count > 0)
            {
                Vector2Int current = frontier.Dequeue();
                foreach (Vector2Int next in Neighbors(current))
                {
                    BattleTestTile tile = TileAt(next);
                    if (tile == null || !tile.walkable)
                    {
                        continue;
                    }

                    BattleTestUnit occupant = UnitAt(next);
                    if (occupant != null && occupant != unit)
                    {
                        continue;
                    }

                    int nextCost = cost[current] + Mathf.Max(1, tile.moveCost);
                    if (nextCost > EffectiveMoveRange(unit))
                    {
                        continue;
                    }

                    if (cost.TryGetValue(next, out int oldCost) && oldCost <= nextCost)
                    {
                        continue;
                    }

                    cost[next] = nextCost;
                    frontier.Enqueue(next);
                }
            }

            return cost;
        }

        private IEnumerable<Vector2Int> Neighbors(Vector2Int cell)
        {
            yield return new Vector2Int(cell.x + 1, cell.y);
            yield return new Vector2Int(cell.x - 1, cell.y);
            yield return new Vector2Int(cell.x, cell.y + 1);
            yield return new Vector2Int(cell.x, cell.y - 1);
        }

        private void RefreshHighlights()
        {
            ClearHighlights();

            if (activeUnit == null || activeUnit.defeated)
            {
                return;
            }

            BattleTestTile activeTile = TileAt(activeUnit.cell);
            if (activeTile != null)
            {
                activeTile.SetHighlight(new Color(1f, 0.76f, 0.18f, 0.62f));
            }

            if (commandMode == BattleCommandMode.Move && !activeUnit.moved)
            {
                foreach (Vector2Int cell in GetReachableCells(activeUnit).Keys)
                {
                    if (cell == activeUnit.cell)
                    {
                        continue;
                    }

                    BattleTestTile tile = TileAt(cell);
                    if (tile != null)
                    {
                        tile.SetHighlight(new Color(0.25f, 0.58f, 1f, 0.38f));
                    }
                }
            }

            if (commandMode == BattleCommandMode.Attack && !activeUnit.acted)
            {
                foreach (BattleTestUnit target in units)
                {
                    if (target.defeated || target.definition.faction == activeUnit.definition.faction)
                    {
                        continue;
                    }

                    if (GridDistance(activeUnit.cell, target.cell) <= activeUnit.definition.attackRange)
                    {
                        BattleTestTile tile = TileAt(target.cell);
                        if (tile != null)
                        {
                            tile.SetHighlight(new Color(1f, 0.18f, 0.16f, 0.52f));
                        }
                    }
                }
            }

            if (commandMode == BattleCommandMode.Skill && CanUseSpecial(activeUnit))
            {
                foreach (BattleTestUnit target in units)
                {
                    if (!IsValidSpecialTarget(activeUnit, target))
                    {
                        continue;
                    }

                    if (GridDistance(activeUnit.cell, target.cell) <= activeUnit.definition.specialRange)
                    {
                        BattleTestTile tile = TileAt(target.cell);
                        if (tile != null)
                        {
                            Color color = activeUnit.definition.specialEffect == BattleSpecialEffect.Heal
                                ? new Color(0.18f, 1f, 0.62f, 0.48f)
                                : new Color(0.72f, 0.28f, 1f, 0.50f);
                            tile.SetHighlight(color);
                        }
                    }
                }
            }

            if (commandMode == BattleCommandMode.Interact && !activeUnit.acted)
            {
                foreach (BattleTestInteractable interactable in interactables)
                {
                    if (interactable.used || GridDistance(activeUnit.cell, interactable.cell) > 1)
                    {
                        continue;
                    }

                    BattleTestTile tile = TileAt(interactable.cell);
                    if (tile != null)
                    {
                        tile.SetHighlight(new Color(1f, 0.62f, 0.16f, 0.58f));
                    }
                }
            }
        }

        private void ClearHighlights()
        {
            if (tiles == null)
            {
                return;
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    tiles[x, y].SetHighlight(Color.clear);
                }
            }
        }

        private void RefreshUnits()
        {
            foreach (BattleTestUnit unit in units)
            {
                unit.view.Refresh(unit == activeUnit);
            }
        }

        private bool CheckBattleEnd()
        {
            bool alliesAlive = false;
            bool enemiesAlive = false;

            foreach (BattleTestUnit unit in units)
            {
                if (unit.defeated)
                {
                    continue;
                }

                if (unit.definition.faction == Faction.Ally)
                {
                    alliesAlive = true;
                }
                else if (unit.definition.faction == Faction.Enemy)
                {
                    enemiesAlive = true;
                }
            }

            if (alliesAlive && enemiesAlive)
            {
                return false;
            }

            battleOver = true;
            ClearHighlights();
            AddLog(alliesAlive ? "[전투 종료] 승리." : "[전투 종료] 패배.");
            return true;
        }

        private BattleTestUnit UnitAt(Vector2Int cell)
        {
            foreach (BattleTestUnit unit in units)
            {
                if (!unit.defeated && unit.cell == cell)
                {
                    return unit;
                }
            }

            return null;
        }

        private BattleTestTile TileAt(Vector2Int cell)
        {
            return IsInside(cell) ? tiles[cell.x, cell.y] : null;
        }

        private bool IsInside(Vector2Int cell)
        {
            return cell.x >= 0 && cell.x < width && cell.y >= 0 && cell.y < height;
        }

        private int GridDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        private Vector3 GridToWorld(Vector2Int cell)
        {
            float x = (cell.x - cell.y) * tileWidth * 0.5f;
            float y = (cell.x + cell.y) * tileHeight * 0.5f;
            return new Vector3(x, y, 0f);
        }

        private Vector3 UnitWorldPosition(Vector2Int cell)
        {
            Vector3 position = GridToWorld(cell);
            position.y += 0.18f;
            return position;
        }

        private TerrainProfile ResolveTerrain(int x, int y)
        {
            if ((x == 6 && y == 5) || (x == 7 && y == 5) || (x == 8 && y == 5))
            {
                return new TerrainProfile(TerrainType.Wall, new Color(0.22f, 0.21f, 0.22f, 1f), 1, 0, 99, false);
            }

            if (y == 3 && x >= 2 && x <= 7)
            {
                bool bridge = x == 4 || x == 5;
                return bridge
                    ? new TerrainProfile(TerrainType.Bridge, new Color(0.49f, 0.31f, 0.16f, 1f), 0, 0, 1, true)
                    : new TerrainProfile(TerrainType.Water, new Color(0.16f, 0.37f, 0.50f, 1f), 0, 0, 3, true);
            }

            if (x <= 2 && y >= 4)
            {
                return new TerrainProfile(TerrainType.Bamboo, new Color(0.22f, 0.42f, 0.24f, 1f), 0, 1, 2, true);
            }

            if (x >= 8 && y <= 2)
            {
                return new TerrainProfile(TerrainType.Roof, new Color(0.54f, 0.23f, 0.16f, 1f), 1, 0, 1, true);
            }

            if (x >= 9 && y >= 5)
            {
                return new TerrainProfile(TerrainType.Cliff, new Color(0.33f, 0.30f, 0.26f, 1f), 1, 2, 2, true);
            }

            return new TerrainProfile(TerrainType.Stone, new Color(0.47f, 0.43f, 0.34f, 1f), 0, 0, 1, true);
        }

        private void CenterCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            Vector3 min = GridToWorld(Vector2Int.zero);
            Vector3 max = GridToWorld(new Vector2Int(width - 1, height - 1));
            Vector3 left = GridToWorld(new Vector2Int(0, height - 1));
            Vector3 right = GridToWorld(new Vector2Int(width - 1, 0));
            Vector3 center = (min + max + left + right) * 0.25f;
            camera.transform.position = new Vector3(center.x, center.y + 0.45f, -10f);
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(4.15f, height * 0.48f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.075f, 0.065f, 1f);
        }

        private bool PointerOverHud(Vector3 screenPosition)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return true;
            }

            float guiY = Screen.height - screenPosition.y;
            Vector2 point = new Vector2(screenPosition.x, guiY);
            Rect leftPanel = new Rect(18f, 18f, 340f, 528f);
            Rect rightPanel = new Rect(Screen.width - 386f, 18f, 368f, 700f);
            Rect bottomPanel = new Rect(18f, Screen.height - 118f, Screen.width - 36f, 100f);
            return leftPanel.Contains(point) || rightPanel.Contains(point) || bottomPanel.Contains(point);
        }

        private Sprite CreateDiamondSprite()
        {
            const int textureWidth = 96;
            const int textureHeight = 48;
            Texture2D texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
            texture.name = "BattleTestDiamond";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Point;

            for (int y = 0; y < textureHeight; y++)
            {
                for (int x = 0; x < textureWidth; x++)
                {
                    float nx = Mathf.Abs(((x + 0.5f) / textureWidth * 2f) - 1f);
                    float ny = Mathf.Abs(((y + 0.5f) / textureHeight * 2f) - 1f);
                    float d = nx + ny;
                    if (d <= 1f)
                    {
                        float edge = d > 0.88f ? 0.74f : 1f;
                        texture.SetPixel(x, y, new Color(edge, edge, edge, 1f));
                    }
                    else
                    {
                        texture.SetPixel(x, y, Color.clear);
                    }
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, textureWidth, textureHeight), new Vector2(0.5f, 0.5f), 96f);
        }

        private void EnsureGuiStyles()
        {
            if (panelStyle != null)
            {
                return;
            }

            panelStyle = new GUIStyle(GUI.skin.box);
            panelStyle.normal.background = MakeTexture(new Color(0.08f, 0.07f, 0.055f, 0.84f));
            panelStyle.border = new RectOffset(6, 6, 6, 6);

            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.normal.textColor = new Color(0.96f, 0.88f, 0.72f, 1f);
            labelStyle.fontSize = 16;
            labelStyle.fontStyle = FontStyle.Bold;

            titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.normal.textColor = new Color(1f, 0.89f, 0.58f, 1f);
            titleStyle.fontSize = 18;
            titleStyle.fontStyle = FontStyle.Bold;

            smallStyle = new GUIStyle(GUI.skin.label);
            smallStyle.normal.textColor = new Color(0.82f, 0.78f, 0.68f, 1f);
            smallStyle.fontSize = 13;
            smallStyle.wordWrap = true;

            logStyle = new GUIStyle(GUI.skin.label);
            logStyle.normal.textColor = new Color(0.86f, 0.82f, 0.74f, 1f);
            logStyle.fontSize = 13;
            logStyle.wordWrap = true;
        }

        private Texture2D MakeTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private string YesNo(bool value)
        {
            return value ? "Yes" : "No";
        }

        private void AddLog(string message)
        {
            battleLog.Add(message);
            if (battleLog.Count > 24)
            {
                battleLog.RemoveAt(0);
            }

            Debug.Log("[BattleTest] " + message);
        }

        private readonly struct BattleForecast
        {
            public static BattleForecast Invalid(string reason)
            {
                return new BattleForecast(false, reason, string.Empty, string.Empty, string.Empty, 0, 0, string.Empty, 0, 0, 0, 0, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
            }

            public static BattleForecast Invalid(string actorName, string targetName, string commandName, string reason, int distance, int range, string costText)
            {
                return new BattleForecast(false, reason, actorName, targetName, commandName, distance, range, "invalid", 0, 0, 0, 0, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, costText);
            }

            public readonly bool valid;
            public readonly string invalidReason;
            public readonly string actorName;
            public readonly string targetName;
            public readonly string commandName;
            public readonly int distance;
            public readonly int range;
            public readonly string rangeText;
            public readonly int attackBonus;
            public readonly int heightBonus;
            public readonly int terrainBonus;
            public readonly int defense;
            public readonly string neededRollText;
            public readonly string damageText;
            public readonly string hpAfterText;
            public readonly string counterText;
            public readonly string followUpText;
            public readonly string costText;

            public BattleForecast(bool valid, string invalidReason, string actorName, string targetName, string commandName, int distance, int range, string rangeText, int attackBonus, int heightBonus, int terrainBonus, int defense, string neededRollText, string damageText, string hpAfterText, string counterText, string followUpText, string costText)
            {
                this.valid = valid;
                this.invalidReason = invalidReason;
                this.actorName = actorName;
                this.targetName = targetName;
                this.commandName = commandName;
                this.distance = distance;
                this.range = range;
                this.rangeText = rangeText;
                this.attackBonus = attackBonus;
                this.heightBonus = heightBonus;
                this.terrainBonus = terrainBonus;
                this.defense = defense;
                this.neededRollText = neededRollText;
                this.damageText = damageText;
                this.hpAfterText = hpAfterText;
                this.counterText = counterText;
                this.followUpText = followUpText;
                this.costText = costText;
            }
        }

        private readonly struct BattleTestCounterMove
        {
            public static readonly BattleTestCounterMove None = new BattleTestCounterMove(false, false, string.Empty);

            public readonly bool valid;
            public readonly bool special;
            public readonly string label;

            public BattleTestCounterMove(bool special, string label)
                : this(true, special, label)
            {
            }

            private BattleTestCounterMove(bool valid, bool special, string label)
            {
                this.valid = valid;
                this.special = special;
                this.label = label;
            }
        }

        private readonly struct TerrainProfile
        {
            public readonly TerrainType terrain;
            public readonly Color color;
            public readonly int elevation;
            public readonly int coverBonus;
            public readonly int moveCost;
            public readonly bool walkable;

            public TerrainProfile(TerrainType terrain, Color color, int elevation, int coverBonus, int moveCost, bool walkable)
            {
                this.terrain = terrain;
                this.color = color;
                this.elevation = elevation;
                this.coverBonus = coverBonus;
                this.moveCost = moveCost;
                this.walkable = walkable;
            }
        }
    }

    [Serializable]
    public sealed class BattleTestUnitDefinition
    {
        public string id;
        public string displayName;
        public Faction faction;
        public CharacterVisualData visual;
        public Vector2Int startCell;
        public int maxHp = 30;
        public int maxInner = 3;
        public int initiative = 10;
        public int agility = -1;
        public int moveRange = 4;
        public int attackRange = 1;
        public int attackBonus = 5;
        public int defense = 14;
        public int damageMin = 5;
        public int damageMax = 9;
        public string specialName = "Special";
        public int specialRange = 1;
        public int specialCost = 1;
        public int specialCooldown = 2;
        public int specialPower = 4;
        public int specialAttackBonus = 1;
        public BattleSpecialEffect specialEffect = BattleSpecialEffect.Strike;
    }

    public enum BattleCommandMode
    {
        Move,
        Attack,
        Skill,
        Interact
    }

    public enum BattleSpecialEffect
    {
        None,
        Strike,
        Heal,
        Poison,
        Freeze,
        Mark,
        BreakGuard
    }

    public enum BattleTestInteractableKind
    {
        Smoke,
        Fire,
        Cover
    }

    public sealed class BattleTestInteractable
    {
        public readonly string id;
        public readonly string displayName;
        public readonly BattleTestInteractableKind kind;
        public readonly Vector2Int cell;
        public bool used;
        public SpriteRenderer renderer;

        public BattleTestInteractable(string id, string displayName, BattleTestInteractableKind kind, Vector2Int cell)
        {
            this.id = id;
            this.displayName = displayName;
            this.kind = kind;
            this.cell = cell;
        }
    }

    public sealed class BattleTestTile : MonoBehaviour
    {
        public Vector2Int cell;
        public TerrainType terrain;
        public int elevation;
        public bool walkable = true;
        public int moveCost = 1;
        public int coverBonus;
        public int baseCoverBonus;
        public Color baseColor = Color.white;
        public int smokeTurns;
        public int fireTurns;
        public bool extraCover;
        public SpriteRenderer terrainRenderer;
        public SpriteRenderer highlightRenderer;

        public void SetHighlight(Color color)
        {
            if (highlightRenderer != null)
            {
                highlightRenderer.color = color;
            }
        }
    }

    public sealed class BattleTestUnitView : MonoBehaviour
    {
        private TextMesh label;
        private CharacterVisualController visualController;

        public BattleTestUnit Unit { get; private set; }

        public void Bind(BattleTestUnit unit, CharacterVisualController controller)
        {
            Unit = unit;
            visualController = controller;
            label = CreateLabel();
            Refresh(false);
        }

        public void Refresh(bool selected)
        {
            if (visualController != null)
            {
                visualController.SetSelected(selected);
            }

            if (label != null && Unit != null)
            {
                label.text = $"{Unit.definition.displayName}\n체력 {Unit.hp}/{Unit.definition.maxHp}";
                label.color = Unit.definition.faction == Faction.Ally
                    ? new Color(0.80f, 0.90f, 1f, Unit.defeated ? 0.45f : 1f)
                    : new Color(1f, 0.72f, 0.68f, Unit.defeated ? 0.45f : 1f);
            }
        }

        public void SetDefeated(bool defeated)
        {
            if (visualController != null)
            {
                visualController.SetDefeated(defeated);
            }

            transform.localScale = defeated ? new Vector3(0.88f, 0.88f, 1f) : Vector3.one;
            Refresh(false);
        }

        private TextMesh CreateLabel()
        {
            GameObject labelObject = new GameObject("Unit Label");
            labelObject.transform.SetParent(transform, false);
            labelObject.transform.localPosition = new Vector3(0f, -0.42f, -0.04f);

            TextMesh mesh = labelObject.AddComponent<TextMesh>();
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.fontSize = 42;
            mesh.characterSize = 0.016f;

            MeshRenderer renderer = labelObject.GetComponent<MeshRenderer>();
            renderer.sortingLayerName = "Default";
            renderer.sortingOrder = 5000;
            return mesh;
        }
    }

    public sealed class BattleTestUnit
    {
        public readonly BattleTestUnitDefinition definition;
        public readonly BattleTestUnitView view;
        public Vector2Int cell;
        public int hp;
        public int inner;
        public int initiative;
        public int specialCooldownLeft;
        public bool moved;
        public bool acted;
        public bool defeated;
        public bool guarded;
        public bool poisoned;
        public bool chilled;
        public bool marked;

        public BattleTestUnit(BattleTestUnitDefinition definition, BattleTestUnitView view)
        {
            this.definition = definition;
            this.view = view;
            cell = definition.startCell;
            hp = definition.maxHp;
            inner = definition.maxInner;
        }
    }
}
