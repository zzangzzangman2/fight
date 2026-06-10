using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JoseonMurimTactics
{
    /// <summary>
    /// BattleResult 화면 위에 성장 수치 상승을 카드/바 그래프로 보여주는 오버레이.
    /// 기존 BattleResultController의 텍스트 요약을 건드리지 않고 RuntimeInitializeOnLoadMethod로 자동 주입한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BattleResultGrowthGraphOverlay : MonoBehaviour
    {
        private const string ObjectName = "BattleResultGrowthGraphOverlay_v1_3";
        private bool collapsed;
        private Vector2 scroll;
        private float bornAt;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallHooks()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
            TryCreateForScene(SceneManager.GetActiveScene());
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            TryCreateForScene(scene);
        }

        private static void TryCreateForScene(Scene scene)
        {
            if (scene.name != "BattleResult")
            {
                return;
            }

            if (GameObject.Find(ObjectName) != null)
            {
                return;
            }

            GameObject go = new GameObject(ObjectName);
            go.AddComponent<BattleResultGrowthGraphOverlay>();
        }

        private void Awake()
        {
            bornAt = Time.realtimeSinceStartup;
        }

        private void OnGUI()
        {
            if (SceneManager.GetActiveScene().name != "BattleResult")
            {
                return;
            }

            RewardBundle bundle;
            BattleResultData result = BattleResultBridge.LastResult;
            if (!ProgressionRewardMemory.TryGet(result, out bundle))
            {
                if (!ProgressionRewardMemory.IsFresh(20f) || !ProgressionRewardMemory.TryGetLatest(out bundle))
                {
                    return;
                }
            }

            if (bundle == null || bundle.characterRewards == null || bundle.characterRewards.Count <= 0)
            {
                return;
            }

            UiTheme.Begin(false);
            GUI.depth = -340;
            float s = UiTheme.Scale;
            float w = Mathf.Min(Screen.width - 80f * s, 1040f * s);
            float h = collapsed ? 62f * s : Mathf.Min(360f * s, Screen.height * 0.40f);
            Rect panel = new Rect((Screen.width - w) * 0.5f, Screen.height - h - 88f * s, w, h);
            UiTheme.DrawPanel(panel, true);

            float x = panel.x + 20f * s;
            float y = panel.y + 14f * s;
            float innerW = panel.width - 40f * s;

            GUI.Label(new Rect(x, y, innerW - 118f * s, 32f * s), "성장 그래프", UiTheme.Heading);
            if (GUI.Button(new Rect(panel.xMax - 112f * s, y - 2f * s, 88f * s, 32f * s), collapsed ? "펼치기" : "접기", UiTheme.Button))
            {
                collapsed = !collapsed;
            }

            y += 34f * s;
            GUI.Label(new Rect(x, y, innerW, 22f * s), "출진 " + bundle.deployedMemberCount + "명 적용 · 비출진 동료 성장 없음 · 권장 Lv." + bundle.recommendedLevel, UiTheme.SmallMuted);
            if (collapsed)
            {
                return;
            }

            y += 30f * s;
            float contentH = Mathf.Max(180f * s, Mathf.Ceil(bundle.characterRewards.Count / 2f) * 104f * s + 8f * s);
            Rect viewRect = new Rect(x, y, innerW, panel.yMax - y - 18f * s);
            Rect contentRect = new Rect(0f, 0f, innerW - 18f * s, contentH);
            scroll = GUI.BeginScrollView(viewRect, scroll, contentRect);

            float t = Mathf.Clamp01((Time.realtimeSinceStartup - bornAt) / 0.92f);
            t = Mathf.SmoothStep(0f, 1f, t);

            int columns = contentRect.width >= 860f * s ? 3 : 2;
            float gap = 12f * s;
            float cardW = (contentRect.width - gap * (columns - 1)) / columns;
            float cardH = 94f * s;

            for (int i = 0; i < bundle.characterRewards.Count; i++)
            {
                CharacterReward reward = bundle.characterRewards[i];
                int col = i % columns;
                int row = i / columns;
                Rect card = new Rect(col * (cardW + gap), row * (cardH + gap), cardW, cardH);
                DrawGrowthCard(card, reward, t, s);
            }

            GUI.EndScrollView();
        }

        private static void DrawGrowthCard(Rect rect, CharacterReward reward, float t, float s)
        {
            Color panelColor = new Color(UiTheme.HanjiPanelAlt.r, UiTheme.HanjiPanelAlt.g, UiTheme.HanjiPanelAlt.b, 0.88f);
            UiTheme.DrawFill(rect, panelColor);
            UiTheme.DrawFill(new Rect(rect.x, rect.y, rect.width, Mathf.Max(2f, 2f * s)), reward.LeveledUp ? UiTheme.GoldBright : UiTheme.Teal);

            float x = rect.x + 10f * s;
            float y = rect.y + 8f * s;
            float w = rect.width - 20f * s;

            GUIStyle nameStyle = new GUIStyle(UiTheme.Small) { fontStyle = FontStyle.Bold };
            nameStyle.normal.textColor = reward.LeveledUp ? UiTheme.GoldBright : UiTheme.Ink;
            GUI.Label(new Rect(x, y, w * 0.62f, 22f * s), reward.displayName, nameStyle);

            string levelText = reward.beforeLevel == reward.afterLevel ? "Lv." + reward.afterLevel : "Lv." + reward.beforeLevel + "→" + reward.afterLevel;
            GUI.Label(new Rect(x + w * 0.62f, y, w * 0.38f, 22f * s), levelText + "  +" + reward.appliedXp + "XP", RightStyle(UiTheme.Small));
            y += 24f * s;

            DrawXpBar(new Rect(x, y, w, 16f * s), reward, t, s);
            y += 21f * s;

            string xpText = BuildXpText(reward);
            GUI.Label(new Rect(x, y, w, 18f * s), xpText, UiTheme.SmallMuted);
            y += 20f * s;

            string statText = BuildStatText(reward);
            if (string.IsNullOrEmpty(statText))
            {
                statText = reward.blockedByCap ? "돌파 대기 · 수련치 +" + reward.convertedTrainingCredit : "숙련 +" + reward.totalMasteryApplied + " / 유대 +" + reward.bondDelta;
            }

            GUI.Label(new Rect(x, y, w, 20f * s), statText, UiTheme.Small);

            if (reward.realmChanged)
            {
                Rect seal = new Rect(rect.xMax - 58f * s, rect.yMax - 34f * s, 48f * s, 24f * s);
                UiTheme.DrawFill(seal, new Color(UiTheme.SealRed.r, UiTheme.SealRed.g, UiTheme.SealRed.b, 0.82f));
                GUI.Label(seal, "경지↑", new GUIStyle(UiTheme.Small) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold });
            }
            else if (reward.LeveledUp)
            {
                Rect badge = new Rect(rect.xMax - 68f * s, rect.yMax - 34f * s, 58f * s, 24f * s);
                UiTheme.DrawFill(badge, new Color(UiTheme.Gold.r, UiTheme.Gold.g, UiTheme.Gold.b, 0.82f));
                GUI.Label(badge, "LEVEL", new GUIStyle(UiTheme.Small) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold });
            }
        }

        private static void DrawXpBar(Rect rect, CharacterReward reward, float t, float s)
        {
            UiTheme.DrawFill(rect, new Color(0f, 0f, 0f, 0.40f));
            float before = reward.BeforeProgress01;
            float after = reward.AfterProgress01;

            Rect beforeRect = new Rect(rect.x, rect.y, rect.width * before, rect.height);
            UiTheme.DrawFill(beforeRect, new Color(UiTheme.NavyLight.r, UiTheme.NavyLight.g, UiTheme.NavyLight.b, 0.72f));

            float animated;
            if (reward.afterLevel > reward.beforeLevel)
            {
                animated = Mathf.Lerp(0f, after, t);
                Rect flash = new Rect(rect.x, rect.y, rect.width * Mathf.Lerp(before, 1f, Mathf.Min(1f, t * 1.45f)), rect.height);
                UiTheme.DrawFill(flash, new Color(UiTheme.Gold.r, UiTheme.Gold.g, UiTheme.Gold.b, 0.28f));
            }
            else
            {
                animated = Mathf.Lerp(before, after, t);
            }

            Rect afterRect = new Rect(rect.x, rect.y, rect.width * animated, rect.height);
            UiTheme.DrawFill(afterRect, new Color(UiTheme.Teal.r, UiTheme.Teal.g, UiTheme.Teal.b, 0.92f));
            UiTheme.DrawFill(new Rect(rect.x, rect.y, rect.width, Mathf.Max(1f, 1f * s)), new Color(1f, 1f, 1f, 0.20f));
        }

        private static string BuildXpText(CharacterReward reward)
        {
            string before = reward.beforeXpToNext <= 0 ? "MAX" : reward.beforeXp + "/" + reward.beforeXpToNext;
            string after = reward.afterXpToNext <= 0 ? "MAX" : reward.afterXp + "/" + reward.afterXpToNext;
            string realm = reward.realmChanged ? " · " + reward.beforeRealmName + "→" + reward.afterRealmName : " · " + reward.afterRealmName;
            return before + " → " + after + realm;
        }

        private static string BuildStatText(CharacterReward reward)
        {
            List<string> parts = new List<string>();
            AddPart(parts, "HP", reward.hpBonus);
            AddPart(parts, "내공", reward.innerBonus);
            AddPart(parts, "근력", reward.strengthBonus);
            AddPart(parts, "민첩", reward.agilityBonus);
            AddPart(parts, "내공력", reward.innerPowerBonus);
            AddPart(parts, "정신", reward.spiritBonus);
            AddPart(parts, "통찰", reward.insightBonus);
            AddPart(parts, "매력", reward.charmBonus);
            AddPart(parts, "무공점", reward.martialPointBonus);

            if (parts.Count <= 0)
            {
                return string.Empty;
            }

            if (parts.Count > 4)
            {
                return parts[0] + "  " + parts[1] + "  " + parts[2] + "  외 +" + (parts.Count - 3);
            }

            return string.Join("  ", parts.ToArray());
        }

        private static void AddPart(List<string> parts, string label, int amount)
        {
            if (amount > 0)
            {
                parts.Add(label + " +" + amount);
            }
        }

        private static GUIStyle RightStyle(GUIStyle baseStyle)
        {
            GUIStyle style = new GUIStyle(baseStyle);
            style.alignment = TextAnchor.UpperRight;
            return style;
        }
    }
}
