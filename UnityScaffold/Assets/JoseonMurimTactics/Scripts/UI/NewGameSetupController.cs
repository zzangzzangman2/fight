using UnityEngine;

namespace JoseonMurimTactics
{
    /// <summary>
    /// [2] NewGameSetup — 난이도 / 문파명 / 성향 / 초기 무공 선택. 결과를 GameSession에 저장 후 Prologue로.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NewGameSetupController : MonoBehaviour
    {
        private static readonly string[] SectPresets = { "해동검문", "백두무관", "청학검가", "의주문" };

        private GameRoot root;
        private string sectName = "해동검문";
        private GameDifficulty difficulty = GameDifficulty.Murim;
        private HeroDisposition disposition = HeroDisposition.Romantic;
        private StartingArt art = StartingArt.Sword;

        private void Awake()
        {
            root = GameRoot.EnsureExists();
            if (root.Session != null)
            {
                sectName = string.IsNullOrEmpty(root.Session.sectName) ? sectName : root.Session.sectName;
                difficulty = root.Session.difficulty;
                disposition = root.Session.heroDisposition;
                art = root.Session.startingArt;
            }
        }

        private void OnGUI()
        {
            UiTheme.Begin(true);
            float w = Screen.width;
            float h = Screen.height;
            float s = UiTheme.Scale;
            float margin = 48f * s;

            GUI.Label(new Rect(margin, 28f * s, w - margin * 2f, 50f * s), "새 게임", UiTheme.Title);
            UiTheme.DrawHLine(new Rect(margin, 84f * s, w - margin * 2f, 2f * s), UiTheme.Gold);

            float colTop = 104f * s;
            float leftW = (w - margin * 2f) * 0.60f;
            float rightX = margin + leftW + 24f * s;
            float rightW = w - margin - rightX;

            // ----- 왼쪽: 선택 -----
            float y = colTop;
            float lineH = 34f * s;
            float btnH = 50f * s;
            float gap = 12f * s;

            GUI.Label(new Rect(margin, y, leftW, lineH), "난이도", UiTheme.Heading);
            y += lineH + 6f * s;
            y = Row3(margin, y, leftW, btnH,
                GameDifficulty.Story, GameDifficulty.Murim, GameDifficulty.BloodPath, ref difficulty);
            GUI.Label(new Rect(margin, y, leftW, lineH), StoryEnumLabels.Blurb(difficulty), UiTheme.Small);
            y += lineH + gap;

            GUI.Label(new Rect(margin, y, leftW, lineH), "문파명", UiTheme.Heading);
            y += lineH + 6f * s;
            sectName = GUI.TextField(new Rect(margin, y, leftW * 0.62f, btnH), sectName ?? string.Empty, 16, UiTheme.TextField);
            float px = margin + leftW * 0.62f + 8f * s;
            float pw = (leftW * 0.38f - 8f * s);
            // 프리셋 두 개를 작은 버튼으로
            if (Btn(new Rect(px, y, pw * 0.5f - 4f * s, btnH), "해동검문", false)) sectName = "해동검문";
            if (Btn(new Rect(px + pw * 0.5f + 4f * s, y, pw * 0.5f - 4f * s, btnH), "청학검가", false)) sectName = "청학검가";
            y += btnH + gap;

            GUI.Label(new Rect(margin, y, leftW, lineH), "박성준 성향", UiTheme.Heading);
            y += lineH + 6f * s;
            y = Row4Disposition(margin, y, leftW, btnH, ref disposition);
            GUI.Label(new Rect(margin, y, leftW, lineH * 1.4f), StoryEnumLabels.Blurb(disposition), UiTheme.Small);
            y += lineH * 1.4f + gap;

            GUI.Label(new Rect(margin, y, leftW, lineH), "초기 무공", UiTheme.Heading);
            y += lineH + 6f * s;
            y = Row4Art(margin, y, leftW, btnH, ref art);
            GUI.Label(new Rect(margin, y, leftW, lineH), StoryEnumLabels.Blurb(art), UiTheme.Small);

            // ----- 오른쪽: 요약 -----
            Rect summary = new Rect(rightX, colTop, rightW, h - colTop - 110f * s);
            UiTheme.DrawPanel(summary);
            float sx = summary.x + 22f * s;
            float sy = summary.y + 18f * s;
            float sw = summary.width - 44f * s;
            GUI.Label(new Rect(sx, sy, sw, 34f * s), "선택 요약", UiTheme.Heading);
            sy += 46f * s;
            SummaryLine(sx, ref sy, sw, s, "문파명", string.IsNullOrEmpty(sectName) ? "(미정)" : sectName);
            SummaryLine(sx, ref sy, sw, s, "난이도", StoryEnumLabels.Label(difficulty));
            SummaryLine(sx, ref sy, sw, s, "성향", StoryEnumLabels.Label(disposition));
            SummaryLine(sx, ref sy, sw, s, "초기 무공", StoryEnumLabels.Label(art));
            sy += 10f * s;
            GUI.Label(new Rect(sx, sy, sw, 140f * s),
                "신생 조선 문파의 문주 박성준.\n중원무림맹 감찰단의 현판령에 맞서\n흩어진 조선 문파를 하나로 묶는다.", UiTheme.Body);

            // ----- 하단 버튼 -----
            float bw = 220f * s;
            float by = h - 78f * s;
            if (Btn(new Rect(margin, by, bw, 56f * s), "← 뒤로", false))
            {
                root.Flow.GoToTitle();
            }

            if (Btn(new Rect(w - margin - bw, by, bw, 56f * s), "이야기 시작 →", true))
            {
                Commit();
                root.Flow.GoToPrologue();
            }
        }

        private void Commit()
        {
            GameSession session = root.Session;
            session.sectName = string.IsNullOrWhiteSpace(sectName) ? "해동검문" : sectName.Trim();
            session.difficulty = difficulty;
            session.heroDisposition = disposition;
            session.startingArt = art;
            session.currentChapterId = "CH00_PROLOGUE";
            Debug.Log($"[NewGameSetup] sect={session.sectName} diff={difficulty} disp={disposition} art={art}");
        }

        private static void SummaryLine(float x, ref float y, float w, float s, string label, string value)
        {
            GUI.Label(new Rect(x, y, w * 0.40f, 30f * s), label, UiTheme.SmallMuted);
            GUI.Label(new Rect(x + w * 0.40f, y, w * 0.60f, 30f * s), value, UiTheme.Body);
            y += 34f * s;
        }

        private float Row3(float x, float y, float w, float h, GameDifficulty a, GameDifficulty b, GameDifficulty c, ref GameDifficulty sel)
        {
            float gap = 10f * UiTheme.Scale;
            float bw = (w - gap * 2f) / 3f;
            if (Toggle(new Rect(x, y, bw, h), StoryEnumLabels.Label(a), sel == a)) sel = a;
            if (Toggle(new Rect(x + bw + gap, y, bw, h), StoryEnumLabels.Label(b), sel == b)) sel = b;
            if (Toggle(new Rect(x + (bw + gap) * 2f, y, bw, h), StoryEnumLabels.Label(c), sel == c)) sel = c;
            return y + h + 6f * UiTheme.Scale;
        }

        private float Row4Disposition(float x, float y, float w, float h, ref HeroDisposition sel)
        {
            float gap = 10f * UiTheme.Scale;
            float bw = (w - gap * 3f) / 4f;
            HeroDisposition[] all = { HeroDisposition.Chivalrous, HeroDisposition.Royal, HeroDisposition.Conqueror, HeroDisposition.Romantic };
            for (int i = 0; i < all.Length; i++)
            {
                if (Toggle(new Rect(x + (bw + gap) * i, y, bw, h), StoryEnumLabels.Label(all[i]), sel == all[i]))
                {
                    sel = all[i];
                }
            }

            return y + h + 6f * UiTheme.Scale;
        }

        private float Row4Art(float x, float y, float w, float h, ref StartingArt sel)
        {
            float gap = 10f * UiTheme.Scale;
            float bw = (w - gap * 3f) / 4f;
            StartingArt[] all = { StartingArt.Sword, StartingArt.Fist, StartingArt.HiddenWeapon, StartingArt.InnerArt };
            for (int i = 0; i < all.Length; i++)
            {
                if (Toggle(new Rect(x + (bw + gap) * i, y, bw, h), StoryEnumLabels.Label(all[i]), sel == all[i]))
                {
                    sel = all[i];
                }
            }

            return y + h + 6f * UiTheme.Scale;
        }

        private static bool Toggle(Rect rect, string label, bool selected)
        {
            return GUI.Button(rect, label, selected ? UiTheme.ButtonPrimary : UiTheme.Button);
        }

        private static bool Btn(Rect rect, string label, bool primary)
        {
            return GUI.Button(rect, label, primary ? UiTheme.ButtonPrimary : UiTheme.Button);
        }
    }
}
