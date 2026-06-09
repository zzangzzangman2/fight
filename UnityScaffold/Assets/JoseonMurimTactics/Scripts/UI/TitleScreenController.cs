using UnityEngine;

namespace JoseonMurimTactics
{
/// <summary>
/// [1] Title — 새 게임 / 이어하기 / 설정 / 종료. 밝은 한지 배경.
/// </summary>
[DisallowMultipleComponent]
public sealed class TitleScreenController : MonoBehaviour
{
    private GameRoot root;
    private bool showSettings;
    private bool showLoad;
    private bool hasSave;
    private SaveSlotSummary latestSave;
    private GameSettings settings;

    private void Awake()
    {
        root = GameRoot.EnsureExists();
        hasSave = root.Save != null && root.Save.HasAnySave();
        latestSave = root.Save != null ? root.Save.PeekLatestSaveSummary() : null;
        settings = GameSettings.Load();
    }

    private void OnGUI()
    {
        UiTheme.Begin(true);
        UiTheme.DrawMountains();
        float w = Screen.width;
        float h = Screen.height;
        float s = UiTheme.Scale;

        UiTheme.LabelShadow(new Rect(0f, h * 0.13f, w, 104f * s), "海東劍門", UiTheme.Logo);
        GUI.Label(new Rect(0f, h * 0.27f, w, 50f * s), "조선 무협 SRPG", UiTheme.Title);
        UiTheme.DrawDivider(w * 0.5f, h * 0.345f, 360f * s);
        GUI.Label(new Rect(0f, h * 0.365f, w, 30f * s), "― 압록강의 현판령 ―", UiTheme.BodyCenter);
        GUI.Label(new Rect(0f, h * 0.405f, w, 26f * s), "배경 시안: 압록강 안개 / 백두산 능선 / 한양 외곽",
                  UiTheme.SmallMuted);

        float bw = 320f * s;
        float bh = 52f * s;
        float gap = 14f * s;
        float x = w * 0.5f - bw * 0.5f;
        float y = h * 0.45f;

        if (Button(new Rect(x, y, bw, bh), "새 게임", true))
        {
            root.Flow.StartNewGame();
        }
        y += bh + gap;

        GUI.enabled = hasSave;
        string continueText = hasSave && latestSave != null && latestSave.exists
                                  ? $"이어하기  {latestSave.chapterTitle} / {latestSave.playTimeText}"
                                  : "이어하기 (저장 없음)";
        if (Button(new Rect(x, y, bw, bh), continueText, false))
        {
            GameSession loaded = root.Save.Load();
            if (loaded != null)
            {
                root.LoadExistingSession(loaded);
                root.Flow.GoToHub();
            }
        }
        GUI.enabled = true;
        y += bh + gap;

        if (Button(new Rect(x, y, bw, bh), "불러오기", false))
        {
            showLoad = !showLoad;
            showSettings = false;
        }
        y += bh + gap;

        if (Button(new Rect(x, y, bw, bh), "전투 시험 [개발용]", false))
        {
            root.Flow.GoToBattle(HubController.FirstBattleId);
        }
        y += bh + gap;

        if (Button(new Rect(x, y, bw, bh), "설정", false))
        {
            showSettings = !showSettings;
            showLoad = false;
        }
        y += bh + gap;

        if (Button(new Rect(x, y, bw, bh), "종료", false))
        {
            Application.Quit();
        }

        GUI.Label(new Rect(0f, h - 40f * s, w, 28f * s),
                  "v0.9 게임 루프 프로토타입 · noncombat-ui-v1.0 · Enter 선택 / Esc 뒤로 / 방향키 이동",
                  UiTheme.SmallMuted);

        if (showLoad)
        {
            DrawLoadSlots(w, h, s);
        }

        if (showSettings)
        {
            DrawSettings(w, h, s);
        }
    }

    private void DrawLoadSlots(float w, float h, float s)
    {
        float pw = 560f * s;
        float ph = 400f * s;
        Rect panel = new Rect(w * 0.5f - pw * 0.5f, h * 0.5f - ph * 0.5f, pw, ph);
        UiTheme.DrawPanel(panel);

        GUI.Label(new Rect(panel.x + 24f * s, panel.y + 18f * s, pw - 48f * s, 34f * s), "불러오기", UiTheme.Heading);
        float y = panel.y + 66f * s;

        DrawSlotButton(panel.x + 24f * s, ref y, pw - 48f * s, s, SaveManager.AutoSlot);
        foreach (string slot in SaveManager.ManualSlots)
        {
            DrawSlotButton(panel.x + 24f * s, ref y, pw - 48f * s, s, slot);
        }

        float bw = 160f * s;
        if (Button(new Rect(panel.x + pw - bw - 24f * s, panel.y + ph - 58f * s, bw, 44f * s), "닫기", false))
        {
            showLoad = false;
        }
    }

    private void DrawSlotButton(float x, ref float y, float w, float s, string slot)
    {
        SaveSlotSummary slotInfo = root.Save.Peek(slot);
        Rect row = new Rect(x, y, w, 62f * s);
        UiTheme.DrawPanel(row, true);

        string label = slot == SaveManager.AutoSlot ? "자동 저장" : $"슬롯 {slot}";
        string detail =
            slotInfo.exists
                ? $"{slotInfo.sectName} · {slotInfo.chapterTitle} · {slotInfo.playTimeText} · {slotInfo.savedAtText}"
                : "비어 있음";
        GUI.Label(new Rect(row.x + 14f * s, row.y + 8f * s, row.width - 150f * s, 24f * s), label, UiTheme.Body);
        GUI.Label(new Rect(row.x + 14f * s, row.y + 34f * s, row.width - 150f * s, 22f * s), detail,
                  UiTheme.SmallMuted);

        GUI.enabled = slotInfo.exists;
        if (GUI.Button(new Rect(row.xMax - 120f * s, row.y + 10f * s, 104f * s, 42f * s), "불러오기",
                       UiTheme.ButtonPrimary))
        {
            GameSession loaded = root.Save.Load(slot);
            if (loaded != null)
            {
                root.LoadExistingSession(loaded);
                root.Flow.GoToHub();
            }
        }
        GUI.enabled = true;
        y += 72f * s;
    }

    private void DrawSettings(float w, float h, float s)
    {
        float pw = 480f * s;
        float ph = 470f * s;
        Rect panel = new Rect(w * 0.5f - pw * 0.5f, h * 0.5f - ph * 0.5f, pw, ph);
        UiTheme.DrawPanel(panel);

        GUI.Label(new Rect(panel.x + 24f * s, panel.y + 18f * s, pw - 48f * s, 34f * s), "설정", UiTheme.Heading);
        float y = panel.y + 64f * s;
        Slider(panel.x + 24f * s, ref y, pw - 48f * s, s, "BGM 볼륨", ref settings.bgmVolume);
        Slider(panel.x + 24f * s, ref y, pw - 48f * s, s, "효과음 볼륨", ref settings.sfxVolume);
        Slider(panel.x + 24f * s, ref y, pw - 48f * s, s, "텍스트 속도", ref settings.textSpeed);
        Slider(panel.x + 24f * s, ref y, pw - 48f * s, s, "UI 크기", ref settings.uiScale, 0.8f, 1.4f);

        settings.fullscreen = GUI.Toggle(new Rect(panel.x + 24f * s, y, pw - 48f * s, 28f * s), settings.fullscreen,
                                         "전체화면", UiTheme.Body);
        y += 34f * s;
        settings.screenShake = GUI.Toggle(new Rect(panel.x + 24f * s, y, pw - 48f * s, 28f * s), settings.screenShake,
                                          "화면 흔들림", UiTheme.Body);
        y += 34f * s;
        settings.autoDialogue = GUI.Toggle(new Rect(panel.x + 24f * s, y, pw - 48f * s, 28f * s), settings.autoDialogue,
                                           "자동 대화", UiTheme.Body);
        y += 34f * s;
        settings.detailedCombatMath = GUI.Toggle(new Rect(panel.x + 24f * s, y, pw - 48f * s, 28f * s),
                                                 settings.detailedCombatMath, "전투 상세 계산 표시", UiTheme.Body);
        y += 38f * s;

        GUI.Label(new Rect(panel.x + 24f * s, y, pw - 48f * s, 24f * s), "해상도", UiTheme.SmallMuted);
        y += 26f * s;
        settings.resolutionIndex = GUI.SelectionGrid(new Rect(panel.x + 24f * s, y, pw - 48f * s, 36f * s),
                                                     Mathf.Clamp(settings.resolutionIndex, 0, 2),
                                                     new[] { "1280x720", "1600x900", "1920x1080" }, 3, UiTheme.Button);

        float bw = 160f * s;
        if (Button(new Rect(panel.x + 24f * s, panel.y + ph - 58f * s, bw, 44f * s), "저장", true))
        {
            settings.Save();
        }

        if (Button(new Rect(panel.x + pw - bw - 24f * s, panel.y + ph - 58f * s, bw, 44f * s), "닫기", false))
        {
            settings.Save();
            showSettings = false;
        }
    }

    private static void Slider(float x, ref float y, float w, float s, string label, ref float value, float min = 0f,
                               float max = 1f)
    {
        GUI.Label(new Rect(x, y, w * 0.42f, 26f * s), label, UiTheme.SmallMuted);
        value = GUI.HorizontalSlider(new Rect(x + w * 0.42f, y + 8f * s, w * 0.42f, 20f * s), value, min, max);
        GUI.Label(new Rect(x + w * 0.86f, y, w * 0.14f, 26f * s), value.ToString("0.00"), UiTheme.SmallMuted);
        y += 34f * s;
    }

    private static bool Button(Rect rect, string label, bool primary)
    {
        return GUI.Button(rect, label, primary ? UiTheme.ButtonPrimary : UiTheme.Button);
    }
}
}
