using UnityEngine;

namespace JoseonMurimTactics
{
/// <summary>
/// New game setup: difficulty, sect name, and Park Sungjun's disposition.
/// The protagonist always starts from swordsmanship.
/// </summary>
[DisallowMultipleComponent]
public sealed class NewGameSetupController : MonoBehaviour
{
    private static readonly string[] SectPresets = { "천광검문", "백두검문", "한양검계", "청해무관", "흑립방" };
    private static readonly string[] ForbiddenSectWords = { "비속어", "음란", "살인", "혐오" };

    private GameRoot root;
    private string sectName = "백두천광검문";
    private GameDifficulty difficulty = GameDifficulty.Murim;
    private HeroDisposition disposition = HeroDisposition.Romantic;
    private bool showConfirm;

    private void Awake()
    {
        root = GameRoot.EnsureExists();
        if (root.Session != null)
        {
            sectName = string.IsNullOrEmpty(root.Session.sectName) ? sectName : root.Session.sectName;
            difficulty = root.Session.difficulty;
            disposition = root.Session.heroDisposition;
        }
    }

    private void OnGUI()
    {
        UiTheme.Begin(true);
        UiTheme.DrawMountains();

        float w = Screen.width;
        float h = Screen.height;
        float s = UiTheme.Scale;
        float margin = Mathf.Clamp(42f * s, 28f, 64f * s);

        GUI.Label(new Rect(margin, 24f * s, w - margin * 2f, 50f * s), "새 게임", UiTheme.Title);
        GUI.Label(new Rect(margin, 70f * s, w - margin * 2f, 26f * s),
                  "난이도  >  문파명  >  박성준 성향  >  확인", UiTheme.SmallMuted);
        UiTheme.DrawDivider(w * 0.5f, 98f * s, w - margin * 2f);

        float top = 126f * s;
        float bottomBar = 92f * s;
        float gap = 24f * s;
        float leftW = Mathf.Min(1040f * s, (w - margin * 2f - gap) * 0.62f);
        float rightW = w - margin * 2f - gap - leftW;
        if (rightW < 420f * s)
        {
            rightW = Mathf.Min(500f * s, w - margin * 2f);
            leftW = w - margin * 2f - gap - rightW;
        }

        Rect leftPanel = new Rect(margin, top, leftW, h - top - bottomBar);
        Rect summaryPanel = new Rect(margin + leftW + gap, top, rightW, h - top - bottomBar);
        UiTheme.DrawPanel(leftPanel);
        UiTheme.DrawPanel(summaryPanel);

        DrawChoices(leftPanel, s);
        DrawSummary(summaryPanel, s);
        DrawBottomButtons(w, h, margin, s);

        if (showConfirm)
        {
            DrawConfirm(w, h, s);
        }
    }

    private void DrawChoices(Rect panel, float s)
    {
        float x = panel.x + 28f * s;
        float y = panel.y + 24f * s;
        float innerW = panel.width - 56f * s;
        float sectionGap = 24f * s;
        float btnH = 48f * s;

        SectionLabel(x, ref y, innerW, s, "난이도", StoryEnumLabels.Blurb(difficulty));
        y = Row3(x, y, innerW, btnH, GameDifficulty.Story, GameDifficulty.Murim, GameDifficulty.BloodPath, ref difficulty);
        y += sectionGap;

        SectionLabel(x, ref y, innerW, s, "문파명", "2~12자, 공백 없이 사용합니다.");
        sectName = GUI.TextField(new Rect(x, y, innerW, btnH), sectName ?? string.Empty, 12, UiTheme.TextField);
        y += btnH + 10f * s;
        y = PresetRow(x, y, innerW, btnH, s);
        GUI.Label(new Rect(x, y, innerW, 24f * s), IsSectNameValid() ? "사용 가능한 문파명입니다." : SectNameValidationMessage(),
                  UiTheme.SmallMuted);
        y += 34f * s + sectionGap;

        SectionLabel(x, ref y, innerW, s, "박성준 성향", StoryEnumLabels.Blurb(disposition));
        y = Row4Disposition(x, y, innerW, btnH, ref disposition);
        y += sectionGap;

        SectionLabel(x, ref y, innerW, s, "주인공 검법", "박성준은 검법으로 시작합니다. 별도 선택 단계는 없습니다.");
        Rect sword = new Rect(x, y, innerW, 70f * s);
        UiTheme.DrawPanel(sword, true);
        GUI.Label(new Rect(sword.x + 18f * s, sword.y + 12f * s, sword.width - 36f * s, 24f * s), "백야검결 · 검법 고정",
                  UiTheme.Body);
        GUI.Label(new Rect(sword.x + 18f * s, sword.y + 38f * s, sword.width - 36f * s, 22f * s),
                  "반격과 베기에 능한 백두천광검문의 정공 검로.", UiTheme.SmallMuted);
    }

    private void DrawSummary(Rect panel, float s)
    {
        float x = panel.x + 26f * s;
        float y = panel.y + 24f * s;
        float w = panel.width - 52f * s;

        GUI.Label(new Rect(x, y, w, 34f * s), "선택 요약", UiTheme.Heading);
        y += 48f * s;
        SummaryLine(x, ref y, w, s, "문파명", string.IsNullOrEmpty(sectName) ? "(미정)" : sectName);
        SummaryLine(x, ref y, w, s, "난이도", StoryEnumLabels.Label(difficulty));
        SummaryLine(x, ref y, w, s, "성향", StoryEnumLabels.Label(disposition));
        SummaryLine(x, ref y, w, s, "주인공", "검법 · 백야검결");
        y += 12f * s;

        GUI.Label(new Rect(x, y, w, 30f * s), "예상 보너스", UiTheme.Heading);
        y += 34f * s;
        GUI.Label(new Rect(x, y, w, 114f * s), BonusPreview(disposition), UiTheme.Body);
        y += 128f * s;

        GUI.Label(
            new Rect(x, y, w, 150f * s),
            "박성준은 백두산의 낡은 검각 앞에 섰다.\n문파의 이름은 곧 집이고,\n집은 끝까지 지켜야 할 사람들의 얼굴이었다.",
            UiTheme.Small);
    }

    private void DrawBottomButtons(float w, float h, float margin, float s)
    {
        float bw = 220f * s;
        float by = h - 70f * s;
        if (Btn(new Rect(margin, by, bw, 52f * s), "← 뒤로", false))
        {
            root.Flow.GoToTitle();
        }

        GUI.enabled = IsSectNameValid();
        if (Btn(new Rect(w - margin - bw, by, bw, 52f * s), "이야기 시작 →", true))
        {
            showConfirm = true;
        }
        GUI.enabled = true;
    }

    private void Commit()
    {
        GameSession session = root.Session;
        session.sectName = NormalizeSectName();
        session.difficulty = difficulty;
        session.heroDisposition = disposition;
        session.startingArt = StartingArt.Sword;
        session.currentChapterId = "CHAPTER_01";
        root.Flags.SetFlag(StoryFlags.Chapter1Started);
        Debug.Log($"[NewGameSetup] sect={session.sectName} diff={difficulty} disp={disposition} art={StartingArt.Sword}");
    }

    private void DrawConfirm(float w, float h, float s)
    {
        UiTheme.DrawFill(new Rect(0f, 0f, w, h), new Color(0f, 0f, 0f, 0.62f));

        float pw = Mathf.Min(620f * s, w - 80f * s);
        float ph = Mathf.Min(360f * s, h - 90f * s);
        Rect panel = new Rect(w * 0.5f - pw * 0.5f, h * 0.5f - ph * 0.5f, pw, ph);
        UiTheme.DrawPanel(panel);

        float x = panel.x + 28f * s;
        float y = panel.y + 24f * s;
        float innerW = panel.width - 56f * s;
        GUI.Label(new Rect(x, y, innerW, 36f * s), "이 설정으로 시작할까요?", UiTheme.Heading);
        y += 52f * s;
        SummaryLine(x, ref y, innerW, s, "문파명", NormalizeSectName());
        SummaryLine(x, ref y, innerW, s, "난이도", StoryEnumLabels.Label(difficulty));
        SummaryLine(x, ref y, innerW, s, "성향", StoryEnumLabels.Label(disposition));
        SummaryLine(x, ref y, innerW, s, "주인공", "검법 · 백야검결");
        y += 8f * s;
        GUI.Label(new Rect(x, y, innerW, 54f * s), "성향은 대화 판정과 동료 반응에 적용됩니다.", UiTheme.SmallMuted);

        float bw = 180f * s;
        if (Btn(new Rect(panel.x + 28f * s, panel.yMax - 66f * s, bw, 46f * s), "다시 고르기", false))
        {
            showConfirm = false;
        }

        if (Btn(new Rect(panel.xMax - bw - 28f * s, panel.yMax - 66f * s, bw, 46f * s), "시작", true))
        {
            Commit();
            root.Flow.GoToPrologue();
        }
    }

    private static void SectionLabel(float x, ref float y, float w, float s, string title, string blurb)
    {
        GUI.Label(new Rect(x, y, w, 30f * s), title, UiTheme.Heading);
        y += 34f * s;
        GUI.Label(new Rect(x, y, w, 28f * s), blurb, UiTheme.SmallMuted);
        y += 34f * s;
    }

    private static void SummaryLine(float x, ref float y, float w, float s, string label, string value)
    {
        GUI.Label(new Rect(x, y, w * 0.38f, 28f * s), label, UiTheme.SmallMuted);
        GUI.Label(new Rect(x + w * 0.38f, y, w * 0.62f, 28f * s), value, UiTheme.Body);
        y += 34f * s;
    }

    private float Row3(float x, float y, float w, float h, GameDifficulty a, GameDifficulty b, GameDifficulty c,
                       ref GameDifficulty sel)
    {
        float gap = 10f * UiTheme.Scale;
        float bw = (w - gap * 2f) / 3f;
        if (Toggle(new Rect(x, y, bw, h), StoryEnumLabels.Label(a), sel == a))
            sel = a;
        if (Toggle(new Rect(x + bw + gap, y, bw, h), StoryEnumLabels.Label(b), sel == b))
            sel = b;
        if (Toggle(new Rect(x + (bw + gap) * 2f, y, bw, h), StoryEnumLabels.Label(c), sel == c))
            sel = c;
        return y + h + 8f * UiTheme.Scale;
    }

    private float PresetRow(float x, float y, float w, float h, float s)
    {
        float gap = 8f * s;
        float bw = (w - gap * (SectPresets.Length - 1)) / SectPresets.Length;
        for (int i = 0; i < SectPresets.Length; i++)
        {
            if (Btn(new Rect(x + (bw + gap) * i, y, bw, h), SectPresets[i], false))
            {
                sectName = SectPresets[i];
            }
        }

        return y + h + 8f * s;
    }

    private float Row4Disposition(float x, float y, float w, float h, ref HeroDisposition sel)
    {
        float gap = 10f * UiTheme.Scale;
        float bw = (w - gap * 3f) / 4f;
        HeroDisposition[] all = { HeroDisposition.Chivalrous, HeroDisposition.Royal, HeroDisposition.Conqueror,
                                  HeroDisposition.Romantic };
        for (int i = 0; i < all.Length; i++)
        {
            if (Toggle(new Rect(x + (bw + gap) * i, y, bw, h), StoryEnumLabels.Label(all[i]), sel == all[i]))
            {
                sel = all[i];
            }
        }

        return y + h + 8f * UiTheme.Scale;
    }

    private static bool Toggle(Rect rect, string label, bool selected)
    {
        return GUI.Button(rect, label, selected ? UiTheme.ButtonPrimary : UiTheme.Button);
    }

    private static bool Btn(Rect rect, string label, bool primary)
    {
        return GUI.Button(rect, label, primary ? UiTheme.ButtonPrimary : UiTheme.Button);
    }

    private bool IsSectNameValid()
    {
        string normalized = NormalizeSectName();
        if (normalized.Length < 2 || normalized.Length > 12 || HasWhitespace(normalized))
        {
            return false;
        }

        foreach (string word in ForbiddenSectWords)
        {
            if (normalized.Contains(word))
            {
                return false;
            }
        }

        return true;
    }

    private string NormalizeSectName()
    {
        string value = string.IsNullOrWhiteSpace(sectName) ? "백두천광검문" : sectName.Trim();
        return value.Length > 12 ? value.Substring(0, 12) : value;
    }

    private string SectNameValidationMessage()
    {
        string normalized = NormalizeSectName();
        if (normalized.Length < 2 || normalized.Length > 12)
        {
            return "문파명은 2~12자로 정해야 합니다.";
        }

        if (HasWhitespace(normalized))
        {
            return "문파명에는 공백을 넣을 수 없습니다.";
        }

        foreach (string word in ForbiddenSectWords)
        {
            if (normalized.Contains(word))
            {
                return "문파명에 사용할 수 없는 표현이 들어 있습니다.";
            }
        }

        return "문파명을 확인해 주세요.";
    }

    private static bool HasWhitespace(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        foreach (char c in value)
        {
            if (char.IsWhiteSpace(c))
            {
                return true;
            }
        }

        return false;
    }

    private static string BonusPreview(HeroDisposition d)
    {
        switch (d)
        {
        case HeroDisposition.Royal:
            return "- 조정/명분 대화 판정 +1\n- 문파 정치와 협상에 강함\n- 검법 고정 시작";
        case HeroDisposition.Chivalrous:
            return "- 백성 보호 선택 보너스\n- 동료 신뢰 상승이 쉬움\n- 검법 고정 시작";
        case HeroDisposition.Conqueror:
            return "- 위압/돌파 판정 +1\n- 적 굴복 선택에 강함\n- 검법 고정 시작";
        default:
            return "- 대화/도발 판정 +1\n- 실패 시 승인도 하락 가능\n- 검법 고정 시작";
        }
    }
}
}
