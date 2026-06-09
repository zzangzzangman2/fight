using UnityEngine;

namespace JoseonMurimTactics
{
    /// <summary>
    /// [7] BattleResult — 승/패, 전투 등급(갑/을/병), 보상, 승인도·평판 변화, 무림 소문 표시.
    /// 결과를 세션에 1회 정산(평판/승인도/은전/퀘스트)하고 자동 저장한 뒤 허브로 돌아간다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BattleResultController : MonoBehaviour
    {
        private GameRoot root;
        private BattleResultData result;
        private BattleDefinition def;
        private string grade;
        private string rumor;

        private void Awake()
        {
            root = GameRoot.EnsureExists();
            result = BattleResultBridge.LastResult;
            if (result == null)
            {
                // 씬을 직접 열었을 때의 안전 기본값.
                result = new BattleResultData { battleId = HubController.FirstBattleId, outcome = BattleOutcome.Victory, defeatedBoss = "감찰사 위지강", turnCount = 6 };
                result.completedObjectives.Add("OBJ_DEFEAT_WIJIGANG");
            }

            def = BattleCatalog.Get(result.battleId);
            grade = ComputeGrade();
            rumor = root.Narration != null ? root.Narration.GenerateRumor(result) : string.Empty;
            ApplyOnce();
        }

        private void ApplyOnce()
        {
            foreach (BattleResultData.StatDelta f in result.factionChanges)
            {
                root.Reputation.Add(f.id, f.delta);
            }

            foreach (BattleResultData.StatDelta a in result.approvalChanges)
            {
                root.Approval.Add(a.id, a.delta);
            }

            if (result.silver != 0)
            {
                root.Flags.AddInt("silver", result.silver);
            }

            root.Quests.ResolveBattle(result, def);
            root.Session.lastBattleResult = result;
            if (result.Won)
            {
                root.Flags.SetFlag("FLAG_CH00_BATTLE_WON");
            }

            root.Save.Save(root.Session);
        }

        private string ComputeGrade()
        {
            if (!result.Won)
            {
                return "—";
            }

            int optionalDone = 0;
            foreach (string objId in result.completedObjectives)
            {
                if (objId != "OBJ_DEFEAT_WIJIGANG") optionalDone++;
            }

            if (optionalDone >= 2 && result.turnCount <= 8) return "갑";
            if (optionalDone >= 1) return "을";
            return "병";
        }

        private void OnGUI()
        {
            UiTheme.Begin(true);
            float w = Screen.width;
            float h = Screen.height;
            float s = UiTheme.Scale;
            float margin = 48f * s;

            bool won = result.Won;
            string banner = won ? "승 리" : "패 배";
            GUIStyle bannerStyle = new GUIStyle(UiTheme.Logo) { fontSize = Mathf.RoundToInt(56 * s) };
            bannerStyle.normal.textColor = won ? UiTheme.Navy : UiTheme.SealRed;
            GUI.Label(new Rect(0f, 28f * s, w, 80f * s), banner, bannerStyle);
            GUI.Label(new Rect(0f, 104f * s, w, 30f * s), def.title, UiTheme.BodyCenter);

            // 등급 인장
            float seal = 92f * s;
            UiTheme.DrawSeal(new Rect(w - margin - seal, 34f * s, seal, seal), grade);

            float top = 150f * s;
            float bottom = h - 96f * s;
            float colGap = 24f * s;
            float colW = (w - margin * 2f - colGap) * 0.5f;

            // 왼쪽: 보상/정산
            Rect left = new Rect(margin, top, colW, bottom - top);
            UiTheme.DrawPanel(left);
            float lx = left.x + 22f * s;
            float lw = left.width - 44f * s;
            float y = left.y + 18f * s;

            if (won)
            {
                GUI.Label(new Rect(lx, y, lw, 32f * s), "보상", UiTheme.Heading); y += 38f * s;
                GUI.Label(new Rect(lx + 10f * s, y, lw, 28f * s), $"은전 {result.silver}", UiTheme.Body); y += 32f * s;
                foreach (string item in result.rewardItems)
                {
                    GUI.Label(new Rect(lx + 10f * s, y, lw - 10f * s, 26f * s), "· " + item, UiTheme.Small);
                    y += 28f * s;
                }
                y += 12f * s;
            }

            GUI.Label(new Rect(lx, y, lw, 32f * s), "전황", UiTheme.Heading); y += 38f * s;
            GUI.Label(new Rect(lx + 10f * s, y, lw - 10f * s, 26f * s), $"소요 턴 {result.turnCount}", UiTheme.Small); y += 30f * s;
            foreach (string objId in result.completedObjectives)
            {
                GUI.Label(new Rect(lx + 10f * s, y, lw - 10f * s, 26f * s), "✔ " + ObjectiveLabel(objId), UiTheme.Small); y += 28f * s;
            }
            foreach (string objId in result.failedObjectives)
            {
                GUIStyle fail = new GUIStyle(UiTheme.Small); fail.normal.textColor = UiTheme.SealRed;
                GUI.Label(new Rect(lx + 10f * s, y, lw - 10f * s, 26f * s), "✘ " + ObjectiveLabel(objId), fail); y += 28f * s;
            }
            foreach (string id in result.woundedCompanions)
            {
                GUI.Label(new Rect(lx + 10f * s, y, lw - 10f * s, 26f * s), $"※ {CompanionCatalog.Name(id)} 부상", UiTheme.Small); y += 28f * s;
            }

            // 오른쪽: 변화/소문
            Rect right = new Rect(margin + colW + colGap, top, colW, bottom - top);
            UiTheme.DrawPanel(right);
            float rx = right.x + 22f * s;
            float rw = right.width - 44f * s;
            float ry = right.y + 18f * s;

            GUI.Label(new Rect(rx, ry, rw, 32f * s), "세력 평판 변화", UiTheme.Heading); ry += 38f * s;
            if (result.factionChanges.Count == 0) { GUI.Label(new Rect(rx + 10f * s, ry, rw, 26f * s), "변화 없음", UiTheme.Small); ry += 30f * s; }
            foreach (BattleResultData.StatDelta f in result.factionChanges)
            {
                GUI.Label(new Rect(rx + 10f * s, ry, rw - 10f * s, 26f * s), $"{FactionIds.Label(f.id)} {Signed(f.delta)}", UiTheme.Small); ry += 28f * s;
            }
            ry += 10f * s;

            GUI.Label(new Rect(rx, ry, rw, 32f * s), "동료 승인도 변화", UiTheme.Heading); ry += 38f * s;
            if (result.approvalChanges.Count == 0) { GUI.Label(new Rect(rx + 10f * s, ry, rw, 26f * s), "변화 없음", UiTheme.Small); ry += 30f * s; }
            foreach (BattleResultData.StatDelta a in result.approvalChanges)
            {
                GUI.Label(new Rect(rx + 10f * s, ry, rw - 10f * s, 26f * s), $"{CompanionCatalog.Name(a.id)} {Signed(a.delta)}", UiTheme.Small); ry += 28f * s;
            }
            ry += 12f * s;

            GUI.Label(new Rect(rx, ry, rw, 32f * s), "무림 소문", UiTheme.Heading); ry += 38f * s;
            UiTheme.DrawFill(new Rect(rx, ry, rw, right.yMax - 22f * s - ry), UiTheme.HanjiPanelAlt);
            GUI.Label(new Rect(rx + 12f * s, ry + 10f * s, rw - 24f * s, right.yMax - 44f * s - ry),
                "“" + rumor + "”", new GUIStyle(UiTheme.Body) { fontStyle = FontStyle.Italic });

            // 하단
            float bw = 280f * s;
            float by = h - 78f * s;
            if (GUI.Button(new Rect(w * 0.5f - bw - 10f * s, by, bw, 56f * s), "거점으로 돌아가기", UiTheme.ButtonPrimary))
            {
                root.Flow.GoToHub(SceneNames.HubPyesadang);
            }

            GUI.enabled = won;
            if (GUI.Button(new Rect(w * 0.5f + 10f * s, by, bw, 56f * s), won ? "다음 장 (준비 중)" : "다음 장", UiTheme.Button))
            {
                root.Flow.GoToWorldMap();
            }
            GUI.enabled = true;
        }

        private static string ObjectiveLabel(string objId)
        {
            switch (objId)
            {
                case "OBJ_DEFEAT_WIJIGANG": return "위지강 제압";
                case "OBJ_SAVE_DISCIPLE": return "다친 제자 구출";
                case "OBJ_KEEP_ALTAR": return "제단 보존";
                default: return objId;
            }
        }

        private static string Signed(int v)
        {
            return v > 0 ? "+" + v : v.ToString();
        }
    }
}
