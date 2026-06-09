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
    private Vector2 settingsScroll;

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

        UiTheme.LabelShadow(new Rect(0f, h * 0.13f, w, 104f * s), "白頭天光", UiTheme.Logo);
        GUI.Label(new Rect(0f, h * 0.27f, w, 50f * s), "조선 무협 SRPG", UiTheme.Title);
        UiTheme.DrawDivider(w * 0.5f, h * 0.345f, 360f * s);
        GUI.Label(new Rect(0f, h * 0.365f, w, 30f * s), "― 꺼져가는 천광 ―", UiTheme.BodyCenter);
        GUI.Label(new Rect(0f, h * 0.405f, w, 26f * s), "백두산 검각 / 소백촌 / 중원 문파의 검은 표식",
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

        if (root.Debug != null && root.Debug.showBattleTestButton)
        {
            if (Button(new Rect(x, y, bw, bh), "전투 시험 [개발용]", false))
            {
                if (root.Debug.allowDebugSessionFactory)
                {
                    root.LoadExistingSession(DebugSessionFactory.CreateBattleTestSession());
                }

                root.Flow.GoToBattle(HubController.FirstBattleId);
            }

            y += bh + gap;
        }

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
                  "v1.3 비전투 UI 흐름 · Enter 선택 / Esc 뒤로 / 방향키 이동",
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
        if (slotInfo.exists && slotInfo.versionMismatch)
        {
            detail += " · " + slotInfo.versionWarning;
        }
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
        float pw = 520f * s;
        float ph = Mathf.Min(h - 70f * s, 620f * s);
        Rect panel = new Rect(w * 0.5f - pw * 0.5f, h * 0.5f - ph * 0.5f, pw, ph);
        UiTheme.DrawPanel(panel);

        GUI.Label(new Rect(panel.x + 24f * s, panel.y + 18f * s, pw - 48f * s, 34f * s), "설정", UiTheme.Heading);
        Rect scrollRect = new Rect(panel.x + 24f * s, panel.y + 58f * s, pw - 48f * s, ph - 128f * s);
        float contentW = scrollRect.width - 18f * s;
        Rect contentRect = new Rect(0f, 0f, contentW, 650f * s);
        settingsScroll = GUI.BeginScrollView(scrollRect, settingsScroll, contentRect);

        float y = 0f;
        Slider(0f, ref y, contentW, s, "전체 볼륨", ref settings.masterVolume);
        Slider(0f, ref y, contentW, s, "BGM 볼륨", ref settings.bgmVolume);
        Slider(0f, ref y, contentW, s, "효과음 볼륨", ref settings.sfxVolume);
        Slider(0f, ref y, contentW, s, "UI 볼륨", ref settings.uiVolume);
        Slider(0f, ref y, contentW, s, "텍스트 속도", ref settings.textSpeed);
        Slider(0f, ref y, contentW, s, "자동 대화 속도", ref settings.autoTextSpeed);
        Slider(0f, ref y, contentW, s, "주사위 연출", ref settings.diceAnimationSpeed);
        Slider(0f, ref y, contentW, s, "적 턴 속도", ref settings.enemyPhaseSpeed);
        Slider(0f, ref y, contentW, s, "UI 크기", ref settings.uiScale, 0.8f, 1.4f);

        y += 6f * s;
        ToggleRow(0f, ref y, contentW, s, "전체화면", ref settings.fullscreen, "수직동기화", ref settings.vsync);
        ToggleRow(0f, ref y, contentW, s, "화면 흔들림", ref settings.screenShake, "자동 대화", ref settings.autoDialogue);
        ToggleRow(0f, ref y, contentW, s, "상세 로그", ref settings.detailedCombatMath, "선택 효과 미리보기",
                  ref settings.choiceEffectPreview);
        ToggleRow(0f, ref y, contentW, s, "확인 팝업", ref settings.confirmPopups, "이동/공격 확인",
                  ref settings.confirmMoveAttack);
        ToggleRow(0f, ref y, contentW, s, "큰 글자", ref settings.largeText, "고대비", ref settings.highContrast);
        ToggleRow(0f, ref y, contentW, s, "연출 줄이기", ref settings.reduceMotion, "색각 보조", ref settings.colorBlindAssist);
        y += 4f * s;

        GUI.Label(new Rect(0f, y, contentW, 24f * s), "해상도", UiTheme.SmallMuted);
        y += 26f * s;
        settings.resolutionIndex = GUI.SelectionGrid(new Rect(0f, y, contentW, 36f * s),
                                                     Mathf.Clamp(settings.resolutionIndex, 0, 2),
                                                     new[] { "1280x720", "1600x900", "1920x1080" }, 3, UiTheme.Button);
        GUI.EndScrollView();

        float bw = 160f * s;
        if (Button(new Rect(panel.x + 24f * s, panel.y + ph - 58f * s, bw, 44f * s), "저장", true))
        {
            settings.Save();
            if (root.Notifications != null)
            {
                root.Notifications.Push("설정 저장", NotificationKind.Success);
            }
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

    private static void ToggleRow(float x, ref float y, float w, float s, string leftLabel, ref bool leftValue,
                                  string rightLabel, ref bool rightValue)
    {
        float col = (w - 10f * s) * 0.5f;
        leftValue = GUI.Toggle(new Rect(x, y, col, 28f * s), leftValue, leftLabel, UiTheme.Body);
        rightValue = GUI.Toggle(new Rect(x + col + 10f * s, y, col, 28f * s), rightValue, rightLabel, UiTheme.Body);
        y += 30f * s;
    }

    private static bool Button(Rect rect, string label, bool primary)
    {
        return GUI.Button(rect, label, primary ? UiTheme.ButtonPrimary : UiTheme.Button);
    }
}
}
