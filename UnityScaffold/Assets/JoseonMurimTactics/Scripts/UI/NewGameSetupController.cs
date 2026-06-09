using UnityEngine;

namespace JoseonMurimTactics
{
/// <summary>
/// [2] NewGameSetup — 난이도 / 문파명 / 성향 / 초기 무공 선택. 결과를 GameSession에 저장 후 Prologue로.
/// </summary>
[DisallowMultipleComponent]
public sealed class NewGameSetupController : MonoBehaviour
{
    private static readonly string[] SectPresets = { "천광검문", "백두검문", "한양검계", "청해무관", "흑립방" };
    private static readonly string[] ForbiddenSectWords = { "비속어", "욕설", "음란", "성인", "혐오" };

    private GameRoot root;
    private string sectName = "백두천광검문";
    private GameDifficulty difficulty = GameDifficulty.Murim;
    private HeroDisposition disposition = HeroDisposition.Romantic;
    private StartingArt art = StartingArt.Sword;
    private bool showConfirm;

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
        GUI.Label(new Rect(margin, 70f * s, w - margin * 2f, 26f * s),
                  "1. 난이도  >  2. 문파명  >  3. 문주 성향  >  4. 초기 무공  >  5. 확인", UiTheme.SmallMuted);
        UiTheme.DrawDivider(w * 0.5f, 88f * s, w - margin * 2f);

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
        y = Row3(margin, y, leftW, btnH, GameDifficulty.Story, GameDifficulty.Murim, GameDifficulty.BloodPath,
                 ref difficulty);
        GUI.Label(new Rect(margin, y, leftW, lineH), StoryEnumLabels.Blurb(difficulty), UiTheme.Small);
        y += lineH + gap;

        GUI.Label(new Rect(margin, y, leftW, lineH), "문파명", UiTheme.Heading);
        y += lineH + 6f * s;
        sectName =
            GUI.TextField(new Rect(margin, y, leftW * 0.44f, btnH), sectName ?? string.Empty, 12, UiTheme.TextField);
        float px = margin + leftW * 0.44f + 8f * s;
        float pw = (leftW * 0.56f - 8f * s);
        float presetW = (pw - 8f * s * 4f) / 5f;
        for (int i = 0; i < SectPresets.Length; i++)
        {
            if (Btn(new Rect(px + (presetW + 8f * s) * i, y, presetW, btnH), SectPresets[i], false))
            {
                sectName = SectPresets[i];
            }
        }
        y += btnH + gap;
        GUI.Label(new Rect(margin, y - gap + 2f * s, leftW, 24f * s),
                  IsSectNameValid() ? "2~12자 문파명 사용 가능" : SectNameValidationMessage(),
                  UiTheme.SmallMuted);

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
        GUI.Label(new Rect(sx, sy, sw, 110f * s), "예상 보너스\n" + BonusPreview(disposition, art), UiTheme.Body);
        sy += 120f * s;
        GUI.Label(
            new Rect(sx, sy, sw, 118f * s),
            "박성준은 백두산의 낡은 검각 앞에 섰다.\n문파의 이름은 곧 집이고,\n집은 곧 지켜야 할 사람들의 얼굴이었다.",
            UiTheme.Small);

        // ----- 하단 버튼 -----
        float bw = 220f * s;
        float by = h - 78f * s;
        if (Btn(new Rect(margin, by, bw, 56f * s), "← 뒤로", false))
        {
            root.Flow.GoToTitle();
        }

        GUI.enabled = IsSectNameValid();
        if (Btn(new Rect(w - margin - bw, by, bw, 56f * s), "이야기 시작 →", true))
        {
            showConfirm = true;
        }
        GUI.enabled = true;

        if (showConfirm)
        {
            DrawConfirm(w, h, s);
        }
    }

    private void Commit()
    {
        GameSession session = root.Session;
        session.sectName = NormalizeSectName();
        session.difficulty = difficulty;
        session.heroDisposition = disposition;
        session.startingArt = art;
        session.currentChapterId = "CHAPTER_01";
        root.Flags.SetFlag(StoryFlags.Chapter1Started);
        Debug.Log($"[NewGameSetup] sect={session.sectName} diff={difficulty} disp={disposition} art={art}");
    }

    private void DrawConfirm(float w, float h, float s)
    {
        Rect dim = new Rect(0f, 0f, w, h);
        UiTheme.DrawFill(dim, new Color(0f, 0f, 0f, 0.35f));

        float pw = 520f * s;
        float ph = 300f * s;
        Rect panel = new Rect(w * 0.5f - pw * 0.5f, h * 0.5f - ph * 0.5f, pw, ph);
        UiTheme.DrawPanel(panel);

        float x = panel.x + 24f * s;
        float y = panel.y + 20f * s;
        float innerW = panel.width - 48f * s;
        GUI.Label(new Rect(x, y, innerW, 34f * s), "이 설정으로 시작할까요?", UiTheme.Heading);
        y += 48f * s;
        SummaryLine(x, ref y, innerW, s, "문파명", NormalizeSectName());
        SummaryLine(x, ref y, innerW, s, "난이도", StoryEnumLabels.Label(difficulty));
        SummaryLine(x, ref y, innerW, s, "성향", StoryEnumLabels.Label(disposition));
        SummaryLine(x, ref y, innerW, s, "초기 무공", StoryEnumLabels.Label(art));
        y += 8f * s;
        GUI.Label(new Rect(x, y, innerW, 48f * s), "풍류 성향은 대화, 재치, 도발 판정 중심으로 적용됩니다.", UiTheme.SmallMuted);

        float bw = 180f * s;
        if (Btn(new Rect(panel.x + 24f * s, panel.yMax - 62f * s, bw, 44f * s), "다시 고르기", false))
        {
            showConfirm = false;
        }

        if (Btn(new Rect(panel.xMax - bw - 24f * s, panel.yMax - 62f * s, bw, 44f * s), "시작", true))
        {
            Commit();
            root.Flow.GoToPrologue();
        }
    }

    private static void SummaryLine(float x, ref float y, float w, float s, string label, string value)
    {
        GUI.Label(new Rect(x, y, w * 0.40f, 30f * s), label, UiTheme.SmallMuted);
        GUI.Label(new Rect(x + w * 0.40f, y, w * 0.60f, 30f * s), value, UiTheme.Body);
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
        return y + h + 6f * UiTheme.Scale;
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

        return y + h + 6f * UiTheme.Scale;
    }

    private float Row4Art(float x, float y, float w, float h, ref StartingArt sel)
    {
        float gap = 10f * UiTheme.Scale;
        float bw = (w - gap * 3f) / 4f;
        StartingArt[] all = { StartingArt.Sword, StartingArt.Ice, StartingArt.HiddenWeapon, StartingArt.Fist,
                              StartingArt.InnerArt };
        gap = 8f * UiTheme.Scale;
        bw = (w - gap * (all.Length - 1)) / all.Length;
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
        string value = string.IsNullOrWhiteSpace(sectName) ? "해동검문" : sectName.Trim();
        return value.Length > 12 ? value.Substring(0, 12) : value;
    }

    private string SectNameValidationMessage()
    {
        string normalized = NormalizeSectName();
        if (normalized.Length < 2 || normalized.Length > 12)
        {
            return "문파명은 앞뒤 공백을 제외하고 2~12자로 정해야 합니다.";
        }

        if (HasWhitespace(normalized))
        {
            return "문파명에는 공백을 넣을 수 없습니다.";
        }

        foreach (string word in ForbiddenSectWords)
        {
            if (normalized.Contains(word))
            {
                return "문파명에 사용할 수 없는 표현이 포함되어 있습니다.";
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

    private static string BonusPreview(HeroDisposition d, StartingArt a)
    {
        string disposition;
        switch (d)
        {
        case HeroDisposition.Royal:
            disposition = "- 조정/명분 설득 +1\n- 사파 협상 리스크";
            break;
        case HeroDisposition.Chivalrous:
            disposition = "- 민심/동료 신뢰 +1\n- 약자 보호 보상 증가";
            break;
        case HeroDisposition.Conqueror:
            disposition = "- 위압/항복 유도 +1\n- 선한 동료 승인도 리스크";
            break;
        default:
            disposition = "- 대화/도발 판정 +1\n- 실패 시 승인도 하락 가능";
            break;
        }

        return disposition + "\n- " + StoryEnumLabels.Label(a) + " 무공 해금";
    }
}
}
