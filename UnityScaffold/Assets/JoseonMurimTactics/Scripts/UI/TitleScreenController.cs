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
        private bool hasSave;

        private void Awake()
        {
            root = GameRoot.EnsureExists();
            hasSave = root.Save != null && root.Save.HasSave();
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

            float bw = 320f * s;
            float bh = 58f * s;
            float gap = 18f * s;
            float x = w * 0.5f - bw * 0.5f;
            float y = h * 0.46f;

            if (Button(new Rect(x, y, bw, bh), "새 게임", true))
            {
                root.Flow.StartNewGame();
            }
            y += bh + gap;

            GUI.enabled = hasSave;
            if (Button(new Rect(x, y, bw, bh), hasSave ? "이어하기" : "이어하기 (저장 없음)", false))
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

            if (Button(new Rect(x, y, bw, bh), "설정", false))
            {
                showSettings = !showSettings;
            }
            y += bh + gap;

            if (Button(new Rect(x, y, bw, bh), "종료", false))
            {
                Application.Quit();
            }

            GUI.Label(new Rect(0f, h - 40f * s, w, 28f * s),
                "v0.8 Story Start Framework", UiTheme.SmallMuted);

            if (showSettings)
            {
                DrawSettings(w, h, s);
            }
        }

        private void DrawSettings(float w, float h, float s)
        {
            float pw = 480f * s;
            float ph = 240f * s;
            Rect panel = new Rect(w * 0.5f - pw * 0.5f, h * 0.5f - ph * 0.5f, pw, ph);
            UiTheme.DrawPanel(panel);

            GUI.Label(new Rect(panel.x + 24f * s, panel.y + 18f * s, pw - 48f * s, 34f * s), "설정", UiTheme.Heading);
            GUI.Label(new Rect(panel.x + 24f * s, panel.y + 64f * s, pw - 48f * s, 100f * s),
                "v0.8에서는 설정 항목이 아직 준비 중입니다.\n해상도/소리 옵션은 이후 버전에서 추가됩니다.", UiTheme.Body);

            float bw = 160f * s;
            if (Button(new Rect(panel.x + pw - bw - 24f * s, panel.y + ph - 58f * s, bw, 44f * s), "닫기", false))
            {
                showSettings = false;
            }
        }

        private static bool Button(Rect rect, string label, bool primary)
        {
            return GUI.Button(rect, label, primary ? UiTheme.ButtonPrimary : UiTheme.Button);
        }
    }
}
