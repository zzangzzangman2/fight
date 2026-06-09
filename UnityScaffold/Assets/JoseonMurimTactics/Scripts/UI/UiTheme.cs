using UnityEngine;

namespace JoseonMurimTactics
{
    /// <summary>
    /// 시작부/허브/대화 화면 공용 IMGUI 스킨. 설계 §9의 밝은 조선 무협풍:
    /// 한지 크림 바탕, 먹선, 남색/청록 포인트, 붉은 인장, 금색 강조. 어두운 검정 바탕 금지.
    /// 한글은 OS 폰트(맑은 고딕 등)를 동적 로드해 IMGUI에서 렌더한다.
    /// 모든 컨트롤러는 OnGUI 맨 처음에 UiTheme.Begin(true)를 호출한다.
    /// </summary>
    public static class UiTheme
    {
        // ----- 팔레트 -----
        public static readonly Color Hanji = new Color(0.953f, 0.925f, 0.851f, 1f);       // 한지 크림 바탕
        public static readonly Color HanjiPanel = new Color(0.984f, 0.969f, 0.925f, 1f);  // 패널(더 밝은 한지)
        public static readonly Color HanjiPanelAlt = new Color(0.929f, 0.890f, 0.804f, 1f);
        public static readonly Color Ink = new Color(0.157f, 0.137f, 0.122f, 1f);         // 먹
        public static readonly Color InkSoft = new Color(0.337f, 0.302f, 0.267f, 1f);
        public static readonly Color Navy = new Color(0.122f, 0.227f, 0.373f, 1f);        // 남색
        public static readonly Color Teal = new Color(0.180f, 0.490f, 0.455f, 1f);        // 청록
        public static readonly Color SealRed = new Color(0.698f, 0.227f, 0.180f, 1f);     // 붉은 인장
        public static readonly Color Gold = new Color(0.784f, 0.635f, 0.294f, 1f);        // 금색

        private static bool built;
        private static int builtForHeight;
        private static Font font;

        private static Texture2D texHanji;
        private static Texture2D texPanel;
        private static Texture2D texPanelAlt;
        private static Texture2D texInkLine;
        private static Texture2D texNavy;
        private static Texture2D texTeal;
        private static Texture2D texSeal;
        private static Texture2D texGold;
        private static Texture2D texButton;
        private static Texture2D texButtonHover;
        private static Texture2D texButtonActive;

        public static GUIStyle Logo { get; private set; }
        public static GUIStyle Title { get; private set; }
        public static GUIStyle Heading { get; private set; }
        public static GUIStyle Body { get; private set; }
        public static GUIStyle BodyCenter { get; private set; }
        public static GUIStyle Small { get; private set; }
        public static GUIStyle SmallMuted { get; private set; }
        public static GUIStyle Button { get; private set; }
        public static GUIStyle ButtonPrimary { get; private set; }
        public static GUIStyle Panel { get; private set; }
        public static GUIStyle PanelSoft { get; private set; }
        public static GUIStyle Speaker { get; private set; }
        public static GUIStyle TextField { get; private set; }

        public static float Scale { get; private set; }
        public static Font Font => font;

        /// <summary>스타일을 보장하고, fillBackground가 true면 한지 바탕을 전체에 그린다.</summary>
        public static void Begin(bool fillBackground)
        {
            EnsureStyles();
            if (fillBackground)
            {
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), texHanji, ScaleMode.StretchToFill);
            }
        }

        public static void EnsureStyles()
        {
            // 도메인 리로드 비활성 시 텍스처/폰트가 파괴됐을 수 있으니 null 체크로 재생성.
            if (built && texHanji != null && font != null && builtForHeight == Screen.height)
            {
                return;
            }

            Scale = Mathf.Clamp(Screen.height / 1080f, 0.6f, 2.4f);

            font = LoadKoreanFont();

            texHanji = Solid(Hanji);
            texPanel = Solid(HanjiPanel);
            texPanelAlt = Solid(HanjiPanelAlt);
            texInkLine = Solid(Ink);
            texNavy = Solid(Navy);
            texTeal = Solid(Teal);
            texSeal = Solid(SealRed);
            texGold = Solid(Gold);
            texButton = Solid(new Color(0.972f, 0.949f, 0.890f, 1f));
            texButtonHover = Solid(new Color(0.937f, 0.882f, 0.776f, 1f));
            texButtonActive = Solid(new Color(0.886f, 0.808f, 0.667f, 1f));

            Logo = MakeLabel(Mathf.RoundToInt(64 * Scale), FontStyle.Bold, Ink, TextAnchor.MiddleCenter);
            Title = MakeLabel(Mathf.RoundToInt(40 * Scale), FontStyle.Bold, Navy, TextAnchor.MiddleCenter);
            Heading = MakeLabel(Mathf.RoundToInt(26 * Scale), FontStyle.Bold, Teal, TextAnchor.MiddleLeft);
            Body = MakeLabel(Mathf.RoundToInt(20 * Scale), FontStyle.Normal, Ink, TextAnchor.UpperLeft);
            Body.wordWrap = true;
            BodyCenter = MakeLabel(Mathf.RoundToInt(20 * Scale), FontStyle.Normal, Ink, TextAnchor.MiddleCenter);
            BodyCenter.wordWrap = true;
            Small = MakeLabel(Mathf.RoundToInt(16 * Scale), FontStyle.Normal, InkSoft, TextAnchor.UpperLeft);
            Small.wordWrap = true;
            SmallMuted = MakeLabel(Mathf.RoundToInt(15 * Scale), FontStyle.Normal, new Color(0.45f, 0.41f, 0.36f, 1f), TextAnchor.MiddleLeft);
            Speaker = MakeLabel(Mathf.RoundToInt(24 * Scale), FontStyle.Bold, SealRed, TextAnchor.MiddleLeft);

            Button = MakeButton(texButton, texButtonHover, texButtonActive, Ink, Mathf.RoundToInt(22 * Scale));
            ButtonPrimary = MakeButton(Solid(Navy), Solid(new Color(0.176f, 0.310f, 0.482f, 1f)), Solid(new Color(0.090f, 0.176f, 0.298f, 1f)), HanjiPanel, Mathf.RoundToInt(22 * Scale));

            Panel = MakeBox(texPanel, Ink);
            PanelSoft = MakeBox(texPanelAlt, InkSoft);

            TextField = new GUIStyle();
            TextField.font = font;
            TextField.fontSize = Mathf.RoundToInt(22 * Scale);
            TextField.alignment = TextAnchor.MiddleLeft;
            TextField.normal.background = Solid(new Color(1f, 0.992f, 0.965f, 1f));
            TextField.normal.textColor = Ink;
            TextField.focused.background = Solid(Color.white);
            TextField.focused.textColor = Ink;
            TextField.border = new RectOffset(2, 2, 2, 2);
            TextField.padding = new RectOffset(10, 10, 6, 6);

            built = true;
            builtForHeight = Screen.height;
        }

        // ----- 그리기 헬퍼 -----

        /// <summary>먹선 테두리가 있는 한지 패널.</summary>
        public static void DrawPanel(Rect rect, bool soft = false)
        {
            EnsureStyles();
            GUI.DrawTexture(rect, texInkLine, ScaleMode.StretchToFill); // 먹선 테두리
            float b = Mathf.Max(2f, 2f * Scale);
            Rect inner = new Rect(rect.x + b, rect.y + b, rect.width - b * 2f, rect.height - b * 2f);
            GUI.DrawTexture(inner, soft ? texPanelAlt : texPanel, ScaleMode.StretchToFill);
        }

        /// <summary>색 면을 채운다.</summary>
        public static void DrawFill(Rect rect, Color color)
        {
            EnsureStyles();
            Texture2D tex = texPanel;
            if (color == Navy) tex = texNavy;
            else if (color == Teal) tex = texTeal;
            else if (color == SealRed) tex = texSeal;
            else if (color == Gold) tex = texGold;
            else if (color == Ink) tex = texInkLine;
            else { GuiColorFill(rect, color); return; }
            GUI.DrawTexture(rect, tex, ScaleMode.StretchToFill);
        }

        public static void DrawHLine(Rect rect, Color color)
        {
            GuiColorFill(rect, color);
        }

        /// <summary>붉은 인장 도장(둥근 사각). 우상단 등 강조용.</summary>
        public static void DrawSeal(Rect rect, string glyph)
        {
            EnsureStyles();
            GUI.DrawTexture(rect, texSeal, ScaleMode.StretchToFill);
            GUIStyle sealText = new GUIStyle(BodyCenter)
            {
                fontSize = Mathf.RoundToInt(rect.height * 0.42f),
                fontStyle = FontStyle.Bold
            };
            sealText.normal.textColor = HanjiPanel;
            GUI.Label(rect, glyph, sealText);
        }

        private static void GuiColorFill(Rect rect, Color color)
        {
            Color prev = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, texPanel, ScaleMode.StretchToFill);
            GUI.color = prev;
        }

        // ----- 내부 빌더 -----

        private static Font LoadKoreanFont()
        {
            string[] candidates =
            {
                "Malgun Gothic", "맑은 고딕", "MalgunGothic",
                "Noto Sans CJK KR", "NanumGothic", "Nanum Gothic",
                "Gulim", "굴림", "Dotum", "돋움", "Batang", "Arial Unicode MS"
            };

            Font f = null;
            try
            {
                f = Font.CreateDynamicFontFromOSFont(candidates, Mathf.RoundToInt(20 * Mathf.Clamp(Screen.height / 1080f, 0.6f, 2.4f)));
            }
            catch
            {
                f = null;
            }

            if (f == null)
            {
                f = Font.CreateDynamicFontFromOSFont("Arial", 20);
            }

            return f;
        }

        private static GUIStyle MakeLabel(int size, FontStyle style, Color color, TextAnchor anchor)
        {
            GUIStyle s = new GUIStyle();
            s.font = font;
            s.fontSize = Mathf.Max(8, size);
            s.fontStyle = style;
            s.alignment = anchor;
            s.normal.textColor = color;
            s.richText = true;
            return s;
        }

        private static GUIStyle MakeButton(Texture2D normal, Texture2D hover, Texture2D active, Color textColor, int size)
        {
            GUIStyle s = new GUIStyle();
            s.font = font;
            s.fontSize = Mathf.Max(8, size);
            s.fontStyle = FontStyle.Bold;
            s.alignment = TextAnchor.MiddleCenter;
            s.padding = new RectOffset(12, 12, 8, 8);
            s.border = new RectOffset(2, 2, 2, 2);
            s.normal.background = normal;
            s.normal.textColor = textColor;
            s.hover.background = hover;
            s.hover.textColor = textColor;
            s.active.background = active;
            s.active.textColor = textColor;
            s.focused.background = hover;
            s.focused.textColor = textColor;
            return s;
        }

        private static GUIStyle MakeBox(Texture2D bg, Color textColor)
        {
            GUIStyle s = new GUIStyle();
            s.font = font;
            s.normal.background = bg;
            s.normal.textColor = textColor;
            s.border = new RectOffset(2, 2, 2, 2);
            s.padding = new RectOffset(14, 14, 12, 12);
            return s;
        }

        private static Texture2D Solid(Color color)
        {
            Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }
    }
}
