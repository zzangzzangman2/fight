using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace JoseonMurimTactics
{
public interface IBattleReturnStateProvider
{
    bool Ready { get; }
    bool TryResolve(out bool victory, out int turnCount);
}

/// <summary>
/// 스토리 전투에 GameRoot가 런타임 주입하는 결과 복귀 오버레이. 기존 BattleTest 씬/컨트롤러를
/// 건드리지 않고, 리플렉션으로 BattleTestController의 종료 상태(battleOver/units/round)를 읽어
/// 실제 승패를 판정한 뒤 BattleResult로 넘긴다. 리플렉션이 실패하면 수동 버튼으로 폴백한다.
/// 상단 중앙에만 그려 BattleTest 자체 UI(좌/우 패널)와 겹치지 않는다.
/// </summary>
[DisallowMultipleComponent]
public sealed class BattleReturnOverlay : MonoBehaviour
{
    private const float AutoAdvanceDelay = 2.4f;
    private const float FallbackGrace = 1.5f;

    private IBattleReturnStateProvider stateProvider;

    private bool resolved;
    private bool won;
    private int turns = 6;
    private float revealTimer;
    private float searchTimer;
    private bool leaving;

    private void Awake()
    {
        stateProvider = new BattleTestReflectionReturnStateProvider();
    }

    private void Update()
    {
        if (leaving)
        {
            return;
        }

        float dt = Time.unscaledDeltaTime;

        if (stateProvider == null)
        {
            stateProvider = new BattleTestReflectionReturnStateProvider();
        }

        if (stateProvider != null && !stateProvider.Ready)
        {
            searchTimer += dt;
        }

        if (stateProvider != null && !resolved && stateProvider.TryResolve(out bool providerWon, out int providerTurns))
        {
            resolved = true;
            won = providerWon;
            turns = providerTurns;
            revealTimer = 0f;
        }

        if (resolved)
        {
            revealTimer += dt;
            if (revealTimer >= AutoAdvanceDelay)
            {
                Finish(won);
            }
        }
    }

    private void OnGUI()
    {
        if (leaving)
        {
            return;
        }

        UiTheme.EnsureStyles();
        float s = UiTheme.Scale;
        float w = Screen.width;

        float pw = Mathf.Clamp(w - 800f * s, 320f * s, 600f * s);
        float x = (w - pw) * 0.5f;

        if (resolved)
        {
            DrawResultPending(x, pw, s);
            return;
        }

        // 진행 중: 상단 중앙에 목표 안내
        Rect bar = new Rect(x, 12f * s, pw, 64f * s);
        UiTheme.DrawPanel(bar, true);
        GameRoot root = GameRoot.EnsureExists();
        BattleDefinition def = root.BattleRepository != null ? root.BattleRepository.Get(BattleResultBridge.CurrentBattleId)
                                                             : BattleCatalog.Get(BattleResultBridge.CurrentBattleId);
        GUI.Label(new Rect(bar.x + 16f * s, bar.y + 8f * s, bar.width - 32f * s, 26f * s),
                  "승리 조건 — " + def.victoryCondition, UiTheme.Small);
        GUI.Label(new Rect(bar.x + 16f * s, bar.y + 34f * s, bar.width - 32f * s, 24f * s),
                  "패배 — 박성준/백련 전투불능 또는 10턴 초과", UiTheme.SmallMuted);

        // 폴백: 리플렉션이 안 되면 수동 버튼 (소프트락 방지)
        if ((stateProvider == null || !stateProvider.Ready) && searchTimer >= FallbackGrace)
        {
            Rect fb = new Rect(x, bar.yMax + 8f * s, pw, 56f * s);
            UiTheme.DrawPanel(fb);
            float bw = (fb.width - 48f * s) * 0.5f;
            if (GUI.Button(new Rect(fb.x + 16f * s, fb.y + 10f * s, bw, 36f * s), "승리로 종료", UiTheme.ButtonPrimary))
                Finish(true);
            if (GUI.Button(new Rect(fb.x + 32f * s + bw, fb.y + 10f * s, bw, 36f * s), "패배로 종료", UiTheme.Button))
                Finish(false);
        }
    }

    private void DrawResultPending(float x, float pw, float s)
    {
        Rect panel = new Rect(x, 14f * s, pw, 116f * s);
        UiTheme.DrawPanel(panel);

        GUIStyle big = new GUIStyle(UiTheme.Title) { fontSize = Mathf.RoundToInt(38 * s) };
        big.normal.textColor = won ? UiTheme.Navy : UiTheme.SealRed;
        UiTheme.LabelShadow(new Rect(panel.x, panel.y + 10f * s, panel.width, 48f * s), won ? "승 리" : "패 배", big);

        float remain = Mathf.Max(0f, AutoAdvanceDelay - revealTimer);
        GUI.Label(new Rect(panel.x, panel.y + 58f * s, panel.width, 24f * s), $"결과 정리 중… ({remain:0.0}s)",
                  UiTheme.SmallMuted == null
                      ? UiTheme.Small
                      : new GUIStyle(UiTheme.SmallMuted) { alignment = TextAnchor.MiddleCenter });

        float bw = 220f * s;
        if (GUI.Button(new Rect(panel.center.x - bw * 0.5f, panel.yMax - 44f * s, bw, 36f * s), "결과 보기 ▶",
                       UiTheme.ButtonPrimary))
        {
            Finish(won);
        }
    }

    private void Finish(bool victory)
    {
        if (leaving)
        {
            return;
        }

        leaving = true;
        GameRoot root = GameRoot.EnsureExists();
        BattleDefinition def = root.BattleRepository != null ? root.BattleRepository.Get(BattleResultBridge.CurrentBattleId)
                                                             : BattleCatalog.Get(BattleResultBridge.CurrentBattleId);

        BattleResultData result =
            new BattleResultData { battleId = def.id, outcome = victory ? BattleOutcome.Victory : BattleOutcome.Defeat,
                                   defeatedBoss = def.bossName, turnCount = turns,
                                   runId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString() };

        if (victory)
        {
            // 일반 전투에서 자동 판정 가능한 것은 대장 제압(주 목표)뿐.
            result.completedObjectives.Add("OBJ_DEFEAT_SCOUTS");
            result.silver = def.silverReward;
            result.rewardItems.AddRange(def.rewardItems);
            foreach (IdDelta f in def.factionOnWin)
                result.factionChanges.Add(new BattleResultData.StatDelta(f.id, f.delta));
            foreach (IdDelta a in def.approvalOnWin)
                result.approvalChanges.Add(new BattleResultData.StatDelta(a.id, a.delta));
        }
        else
        {
            result.failedObjectives.Add("OBJ_DEFEAT_SCOUTS");
        }

        root.Flow.GoToBattleResult(result);
    }
}

public sealed class BattleTestReflectionReturnStateProvider : IBattleReturnStateProvider
{
    private BattleTestController controller;
    private FieldInfo overField;
    private FieldInfo unitsField;
    private FieldInfo roundField;

    public bool Ready => controller != null && overField != null && unitsField != null;

    public bool TryResolve(out bool victory, out int turnCount)
    {
        EnsureBound();
        victory = false;
        turnCount = 6;

        if (!Ready)
        {
            return false;
        }

        bool over = false;
        object value = overField.GetValue(controller);
        if (value is bool b)
        {
            over = b;
        }

        if (!over)
        {
            return false;
        }

        victory = DetermineWon();
        turnCount = ReadRound();
        return true;
    }

    private void EnsureBound()
    {
        if (controller != null)
        {
            return;
        }

        controller = UnityEngine.Object.FindAnyObjectByType<BattleTestController>();
        if (controller == null)
        {
            return;
        }

        System.Type t = typeof(BattleTestController);
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
        overField = t.GetField("battleOver", flags);
        unitsField = t.GetField("units", flags);
        roundField = t.GetField("round", flags);
    }

    private bool DetermineWon()
    {
        bool alliesAlive = false;
        bool enemiesAlive = false;
        if (unitsField != null && unitsField.GetValue(controller) is IEnumerable list)
        {
            foreach (object o in list)
            {
                if (!(o is BattleTestUnit u) || u.defeated || u.definition == null)
                {
                    continue;
                }

                if (u.definition.faction == Faction.Ally)
                {
                    alliesAlive = true;
                }
                else if (u.definition.faction == Faction.Enemy)
                {
                    enemiesAlive = true;
                }
            }
        }

        return alliesAlive && !enemiesAlive;
    }

    private int ReadRound()
    {
        if (roundField != null && roundField.GetValue(controller) is int r)
        {
            return Mathf.Max(1, r);
        }

        return 6;
    }
}
}
