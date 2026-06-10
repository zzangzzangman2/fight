using UnityEngine;

namespace JoseonMurimTactics
{
/// <summary>
/// 시작부/허브/대화 화면 공용 IMGUI 스킨 — 세련된 조선 무협(산수화) 풍.
/// 한지 그라데이션 바탕, 종이결, 먼 산 실루엣, 비네트, 먹 붓선 구분선, 둥근 인장,
/// 금테 9-슬라이스 버튼/패널. 한글은 OS 폰트(맑은 고딕 등)를 동적 로드해 렌더한다.
/// 컨트롤러는 OnGUI 첫 줄에서 UiTheme.Begin(true)를 호출한다.
/// </summary>
public static class UiTheme
{
    // ----- 팔레트 -----
    public static readonly Color Hanji = new Color(0.070f, 0.082f, 0.078f, 1f);
    public static readonly Color HanjiTop = new Color(0.036f, 0.052f, 0.056f, 1f);
    public static readonly Color HanjiBottom = new Color(0.010f, 0.014f, 0.016f, 1f);
    public static readonly Color HanjiPanel = new Color(0.090f, 0.102f, 0.098f, 0.96f);
    public static readonly Color HanjiPanelAlt = new Color(0.060f, 0.072f, 0.070f, 0.92f);
    public static readonly Color Ink = new Color(0.925f, 0.890f, 0.760f, 1f);
    public static readonly Color InkSoft = new Color(0.650f, 0.690f, 0.650f, 1f);
    public static readonly Color Navy = new Color(0.045f, 0.105f, 0.135f, 1f);
    public static readonly Color NavyLight = new Color(0.075f, 0.205f, 0.230f, 1f);
    public static readonly Color Teal = new Color(0.185f, 0.630f, 0.555f, 1f);
    public static readonly Color SealRed = new Color(0.760f, 0.205f, 0.145f, 1f);
    public static readonly Color Gold = new Color(0.835f, 0.625f, 0.250f, 1f);
    public static readonly Color GoldBright = new Color(0.960f, 0.780f, 0.360f, 1f);
    public static readonly Color SkyAccent = new Color(0.290f, 0.760f, 0.955f, 1f); // 대화 이름표/소속 태그 강조색
    private static readonly Color DeepInk = new Color(0.006f, 0.010f, 0.012f, 1f);

    private static bool built;
    private static int builtForHeight;
    private static Font font;

    private static Texture2D texWhite;
    private static Texture2D texBg;
    private static Texture2D texPaper;
    private static Texture2D texVignette;
    private static Texture2D texMountain;
    private static Texture2D texBrush;
    private static Texture2D texSeal;
    private static Texture2D texPanelFill;
    private static Texture2D texShadow;
    private static Texture2D texBottomShade;
    private static Texture2D texBtn;
    private static Texture2D texBtnHover;
    private static Texture2D texBtnActive;
    private static Texture2D texBtnPrimary;
    private static Texture2D texBtnPrimaryHover;
    private static Texture2D texBtnPrimaryActive;
    private static Texture2D texField;
    private static Texture2D titleBackdrop;

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

    public static void Begin(bool fillBackground)
    {
        EnsureStyles();
        if (!fillBackground)
        {
            return;
        }

        Rect full = new Rect(0f, 0f, Screen.width, Screen.height);
        GUI.DrawTexture(full, texBg, ScaleMode.StretchToFill);

        float tile = 220f * Scale;
        Color prev = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, 0.08f);
        GUI.DrawTextureWithTexCoords(full, texPaper, new Rect(0f, 0f, Screen.width / tile, Screen.height / tile));
        GUI.color = prev;

        GUI.DrawTexture(full, texVignette, ScaleMode.StretchToFill);
    }

    /// <summary>화면 하단에 먼 산 실루엣을 그린다(타이틀/월드맵 등 분위기용).</summary>
    public static void DrawMountains()
    {
        EnsureStyles();
        float h = Mathf.Min(Screen.height * 0.42f, 360f * Scale);
        GUI.DrawTexture(new Rect(0f, Screen.height - h, Screen.width, h), texMountain, ScaleMode.StretchToFill);
    }

    public static void DrawTitleBackdrop()
    {
        EnsureStyles();
        Rect full = new Rect(0f, 0f, Screen.width, Screen.height);
        if (titleBackdrop == null)
        {
            titleBackdrop = Resources.Load<Texture2D>("UI/title_baekdu_murim");
        }

        if (titleBackdrop != null)
        {
            GUI.DrawTexture(full, titleBackdrop, ScaleMode.ScaleAndCrop);
        }
        else
        {
            GUI.DrawTexture(full, texBg, ScaleMode.StretchToFill);
            DrawMountains();
        }

        DrawFill(full, new Color(0.000f, 0.010f, 0.014f, 0.34f));
        GUI.DrawTexture(full, texVignette, ScaleMode.StretchToFill);
    }

    /// <summary>화면 가장자리를 살짝 어둡게(비네트). Begin 직후 또는 마지막에 호출 가능.</summary>
    public static void DrawVignette()
    {
        EnsureStyles();
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), texVignette, ScaleMode.StretchToFill);
    }

    /// <summary>위는 투명, 아래로 갈수록 어두워지는 그라데이션. 대화 오버레이(스탠딩 일러 위에 글이 얹히는 방식)용.</summary>
    public static void DrawBottomShade(Rect rect)
    {
        EnsureStyles();
        GUI.DrawTexture(rect, texBottomShade, ScaleMode.StretchToFill);
    }

    // ----- 패널 -----

    /// <summary>먹테 + 금색 헤어라인 + 모서리 장식 + 그림자가 있는 한지 패널.</summary>
    public static void DrawPanel(Rect rect, bool soft = false)
    {
        EnsureStyles();
        float s = Mathf.Max(1f, Scale);

        GUI.DrawTexture(new Rect(rect.x + 5f * s, rect.y + 7f * s, rect.width, rect.height), texShadow,
                        ScaleMode.StretchToFill);

        Tint(rect, new Color(0.015f, 0.020f, 0.020f, 0.92f));
        float b = Mathf.Max(2f, 2.5f * s);
        Rect fill = Inset(rect, b);
        GUI.DrawTexture(fill, soft ? texWhite : texPanelFill, ScaleMode.StretchToFill);
        if (soft)
        {
            Tint(fill, HanjiPanelAlt);
        }

        // 금색 헤어라인
        float g = Mathf.Max(1f, 1f * s);
        DrawFrame(Inset(fill, 4f * s), g, Gold);

        // 모서리 장식 (금색 ㄱ자)
        DrawCornerTicks(Inset(fill, 4f * s), 14f * s, Mathf.Max(2f, 2f * s), Gold);
    }

    public static void DrawFill(Rect rect, Color color)
    {
        EnsureStyles();
        Tint(rect, color);
    }

    public static void DrawHLine(Rect rect, Color color)
    {
        EnsureStyles();
        Tint(rect, color);
    }

    /// <summary>먹 붓선 구분선 + 가운데 금색 마름모.</summary>
    public static void DrawDivider(float centerX, float y, float width)
    {
        EnsureStyles();
        float s = Scale;
        Rect r = new Rect(centerX - width * 0.5f, y - 7f * s, width, 14f * s);
        Color prev = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, 0.9f);
        GUI.DrawTexture(r, texBrush, ScaleMode.StretchToFill);
        GUI.color = prev;

        Rect glow = new Rect(centerX - 18f * s, y - 1f * s, 36f * s, 2f * s);
        Tint(glow, new Color(Gold.r, Gold.g, Gold.b, 0.62f));
    }

    /// <summary>붉은 인장(둥근 사각, 살짝 기울임) + 글자.</summary>
    public static void DrawSeal(Rect rect, string glyph, float tilt = -5f)
    {
        EnsureStyles();
        Matrix4x4 m = GUI.matrix;
        GUIUtility.RotateAroundPivot(tilt, rect.center);
        GUI.DrawTexture(rect, texSeal, ScaleMode.StretchToFill);
        GUIStyle sealText =
            new GUIStyle(BodyCenter) { fontSize = Mathf.RoundToInt(rect.height * 0.46f), fontStyle = FontStyle.Bold };
        sealText.normal.textColor = Ink;
        GUI.Label(rect, glyph, sealText);
        GUI.matrix = m;
    }

    /// <summary>제목을 옅은 먹 그림자와 함께(붓글씨 느낌).</summary>
    public static void LabelShadow(Rect rect, string text, GUIStyle style)
    {
        EnsureStyles();
        float off = Mathf.Max(1f, 2f * Scale);
        GUIStyle shadow = new GUIStyle(style);
        shadow.normal.textColor = new Color(DeepInk.r, DeepInk.g, DeepInk.b, 0.72f);
        GUI.Label(new Rect(rect.x + off, rect.y + off, rect.width, rect.height), text, shadow);
        GUI.Label(rect, text, style);
    }

    // ----- 내부 그리기 -----

    private static void Tint(Rect rect, Color color)
    {
        Color prev = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, texWhite, ScaleMode.StretchToFill);
        GUI.color = prev;
    }

    private static void DrawFrame(Rect rect, float thick, Color color)
    {
        Tint(new Rect(rect.x, rect.y, rect.width, thick), color);
        Tint(new Rect(rect.x, rect.yMax - thick, rect.width, thick), color);
        Tint(new Rect(rect.x, rect.y, thick, rect.height), color);
        Tint(new Rect(rect.xMax - thick, rect.y, thick, rect.height), color);
    }

    private static void DrawCornerTicks(Rect rect, float len, float thick, Color color)
    {
        // 좌상
        Tint(new Rect(rect.x, rect.y, len, thick), color);
        Tint(new Rect(rect.x, rect.y, thick, len), color);
        // 우상
        Tint(new Rect(rect.xMax - len, rect.y, len, thick), color);
        Tint(new Rect(rect.xMax - thick, rect.y, thick, len), color);
        // 좌하
        Tint(new Rect(rect.x, rect.yMax - thick, len, thick), color);
        Tint(new Rect(rect.x, rect.yMax - len, thick, len), color);
        // 우하
        Tint(new Rect(rect.xMax - len, rect.yMax - thick, len, thick), color);
        Tint(new Rect(rect.xMax - thick, rect.yMax - len, thick, len), color);
    }

    private static Rect Inset(Rect r, float by)
    {
        return new Rect(r.x + by, r.y + by, r.width - by * 2f, r.height - by * 2f);
    }

    // ----- 빌드 -----

    public static void EnsureStyles()
    {
        if (built && texBg != null && font != null && builtForHeight == Screen.height)
        {
            return;
        }

        Scale = Mathf.Clamp(Screen.height / 1080f, 0.6f, 2.4f);
        font = LoadKoreanFont();

        texWhite = Solid(Color.white);
        texBg = VerticalGradient(HanjiTop, HanjiBottom, 256);
        texPaper = PaperGrain(128, 0.06f);
        texVignette = Vignette(256, DeepInk, 0.46f);
        texMountain = Mountains(800, 300);
        texBrush = BrushStroke(256, 24);
        texSeal = SealTex(72);
        texPanelFill = VerticalGradient(new Color(0.100f, 0.122f, 0.116f, 0.98f), HanjiPanel, 64);
        texShadow = Solid(new Color(0f, 0f, 0f, 0.42f));
        texBottomShade = VerticalGradient(new Color(0.008f, 0.020f, 0.038f, 0f), new Color(0.008f, 0.020f, 0.038f, 0.94f), 160);

        texBtn = Bordered(new Color(0.075f, 0.092f, 0.088f, 0.96f), new Color(0.300f, 0.240f, 0.130f, 1f), 12, 2);
        texBtnHover = Bordered(new Color(0.100f, 0.160f, 0.148f, 0.98f), Gold, 12, 2);
        texBtnActive = Bordered(new Color(0.035f, 0.050f, 0.052f, 1f), GoldBright, 12, 2);
        texBtnPrimary = Bordered(Navy, Gold, 12, 2);
        texBtnPrimaryHover = Bordered(NavyLight, GoldBright, 12, 2);
        texBtnPrimaryActive = Bordered(new Color(0.024f, 0.060f, 0.072f, 1f), Gold, 12, 2);
        texField = Bordered(new Color(0.035f, 0.045f, 0.044f, 0.96f), new Color(0.350f, 0.295f, 0.165f, 1f), 10, 2);

        Logo = Label(Mathf.RoundToInt(66 * Scale), FontStyle.Bold, Ink, TextAnchor.MiddleCenter);
        Title = Label(Mathf.RoundToInt(40 * Scale), FontStyle.Bold, Ink, TextAnchor.MiddleCenter);
        Heading = Label(Mathf.RoundToInt(25 * Scale), FontStyle.Bold, Teal, TextAnchor.MiddleLeft);
        Body = Label(Mathf.RoundToInt(20 * Scale), FontStyle.Normal, Ink, TextAnchor.UpperLeft);
        Body.wordWrap = true;
        BodyCenter = Label(Mathf.RoundToInt(20 * Scale), FontStyle.Normal, Ink, TextAnchor.MiddleCenter);
        BodyCenter.wordWrap = true;
        Small = Label(Mathf.RoundToInt(16 * Scale), FontStyle.Normal, InkSoft, TextAnchor.UpperLeft);
        Small.wordWrap = true;
        SmallMuted = Label(Mathf.RoundToInt(15 * Scale), FontStyle.Normal, new Color(0.610f, 0.650f, 0.600f, 1f),
                           TextAnchor.MiddleLeft);
        Speaker = Label(Mathf.RoundToInt(24 * Scale), FontStyle.Bold, SealRed, TextAnchor.MiddleLeft);

        Button = Btn(texBtn, texBtnHover, texBtnActive, Ink);
        ButtonPrimary = Btn(texBtnPrimary, texBtnPrimaryHover, texBtnPrimaryActive, new Color(0.98f, 0.96f, 0.91f, 1f));

        Panel = Box(texPanelFill, Ink);
        PanelSoft = Box(texPanelFill, InkSoft);

        TextField = new GUIStyle();
        TextField.font = font;
        TextField.fontSize = Mathf.RoundToInt(22 * Scale);
        TextField.alignment = TextAnchor.MiddleLeft;
        TextField.normal.background = texField;
        TextField.normal.textColor = Ink;
        TextField.focused.background = texField;
        TextField.focused.textColor = Ink;
        TextField.border = new RectOffset(2, 2, 2, 2);
        TextField.padding = new RectOffset(12, 12, 6, 6);

        built = true;
        builtForHeight = Screen.height;
    }

    private static Font LoadKoreanFont()
    {
        // 둥근 계열(모바일 미연시풍 대화 폰트 느낌)을 먼저 시도하고, 없으면 맑은 고딕 계열로 내려간다.
        string[] candidates = { "NanumSquareRound", "NanumSquareRoundOTF", "나눔스퀘어라운드", "NanumSquare",
                                "Noto Sans KR",     "Pretendard",          "Malgun Gothic",    "맑은 고딕",
                                "MalgunGothic",     "Noto Sans CJK KR",    "NanumGothic",      "Nanum Gothic",
                                "Gulim",            "굴림",                "Dotum",            "돋움",
                                "Batang",           "Arial Unicode MS" };

        Font f = null;
        try
        {
            f = Font.CreateDynamicFontFromOSFont(candidates, 22);
        }
        catch
        {
            f = null;
        }
        if (f == null)
        {
            f = Font.CreateDynamicFontFromOSFont("Arial", 22);
        }
        return f;
    }

    private static GUIStyle Label(int size, FontStyle style, Color color, TextAnchor anchor)
    {
        GUIStyle s = new GUIStyle { font = font, fontSize = Mathf.Max(8, size), fontStyle = style, alignment = anchor,
                                    richText = true };
        s.normal.textColor = color;
        return s;
    }

    private static GUIStyle Btn(Texture2D normal, Texture2D hover, Texture2D active, Color textColor)
    {
        GUIStyle s = new GUIStyle { font = font,
                                    fontSize = Mathf.RoundToInt(22 * Scale),
                                    fontStyle = FontStyle.Bold,
                                    alignment = TextAnchor.MiddleCenter,
                                    padding = new RectOffset(12, 12, 8, 8),
                                    border = new RectOffset(3, 3, 3, 3) };
        s.normal.background = normal;
        s.normal.textColor = textColor;
        s.hover.background = hover;
        s.hover.textColor = textColor;
        s.active.background = active;
        s.active.textColor = textColor;
        s.focused.background = normal;
        s.focused.textColor = textColor;
        return s;
    }

    private static GUIStyle Box(Texture2D bg, Color textColor)
    {
        GUIStyle s =
            new GUIStyle { font = font, border = new RectOffset(3, 3, 3, 3), padding = new RectOffset(14, 14, 12, 12) };
        s.normal.background = bg;
        s.normal.textColor = textColor;
        return s;
    }

    // ----- 텍스처 생성기 -----

    private static Texture2D Solid(Color color)
    {
        Texture2D t = new Texture2D(1, 1, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave,
                                                                         wrapMode = TextureWrapMode.Clamp,
                                                                         filterMode = FilterMode.Bilinear };
        t.SetPixel(0, 0, color);
        t.Apply();
        return t;
    }

    private static Texture2D VerticalGradient(Color top, Color bottom, int h)
    {
        Texture2D t = new Texture2D(2, h, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave,
                                                                         wrapMode = TextureWrapMode.Clamp,
                                                                         filterMode = FilterMode.Bilinear };
        for (int y = 0; y < h; y++)
        {
            Color c = Color.Lerp(bottom, top, y / (float)(h - 1));
            t.SetPixel(0, y, c);
            t.SetPixel(1, y, c);
        }
        t.Apply();
        return t;
    }

    private static Texture2D PaperGrain(int size, float contrast)
    {
        Texture2D t = new Texture2D(size, size, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave,
                                                                               wrapMode = TextureWrapMode.Repeat,
                                                                               filterMode = FilterMode.Bilinear };
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // 결정적 의사 난수 (해시) — 도메인 리로드와 무관하게 동일.
                float n = Frac(Mathf.Sin((x * 12.9898f + y * 78.233f)) * 43758.5453f);
                float v = (n - 0.5f) * contrast;
                float a = Mathf.Abs(v);
                Color c = v >= 0f ? new Color(1f, 1f, 1f, a * 0.18f) : new Color(DeepInk.r, DeepInk.g, DeepInk.b, a);
                t.SetPixel(x, y, c);
            }
        }
        t.Apply();
        return t;
    }

    private static Texture2D Vignette(int size, Color edge, float maxAlpha)
    {
        Texture2D t = new Texture2D(size, size, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave,
                                                                               wrapMode = TextureWrapMode.Clamp,
                                                                               filterMode = FilterMode.Bilinear };
        Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
        float maxD = c.magnitude;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c) / maxD;
                float a = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((d - 0.55f) / 0.45f)) * maxAlpha;
                t.SetPixel(x, y, new Color(edge.r, edge.g, edge.b, a));
            }
        }
        t.Apply();
        return t;
    }

    private static Texture2D Mountains(int w, int h)
    {
        Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave,
                                                                         wrapMode = TextureWrapMode.Clamp,
                                                                         filterMode = FilterMode.Bilinear };
        Color clear = new Color(0f, 0f, 0f, 0f);
        for (int x = 0; x < w; x++)
        {
            float fx = x / (float)w;
            // 뒤 능선 (옅은 청회), 앞 능선 (짙은 먹청)
            float back = 0.55f + 0.16f * Mathf.Sin(fx * 6.5f) + 0.07f * Mathf.Sin(fx * 17f + 1.3f);
            float front = 0.34f + 0.20f * Mathf.Sin(fx * 4.1f + 2.0f) + 0.06f * Mathf.Sin(fx * 23f);
            int backY = Mathf.RoundToInt(back * h);
            int frontY = Mathf.RoundToInt(front * h);
            for (int y = 0; y < h; y++)
            {
                Color c = clear;
                if (y < frontY)
                    c = new Color(0.040f, 0.078f, 0.088f, 0.58f);
                else if (y < backY)
                    c = new Color(0.075f, 0.125f, 0.132f, 0.42f);
                // 위로 갈수록 더 옅게 (안개)
                if (c.a > 0f)
                {
                    float fade = Mathf.Clamp01(y / (float)h);
                    c.a *= (1f - fade * 0.55f);
                }
                t.SetPixel(x, y, c);
            }
        }
        t.Apply();
        return t;
    }

    private static Texture2D BrushStroke(int w, int h)
    {
        Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave,
                                                                         wrapMode = TextureWrapMode.Clamp,
                                                                         filterMode = FilterMode.Bilinear };
        for (int x = 0; x < w; x++)
        {
            float fx = x / (float)(w - 1);
            // 양끝 가늘게, 가운데 두껍게 + 약간 거친 가장자리
            float taper = Mathf.Sin(fx * Mathf.PI);
            float thickness = Mathf.Clamp01(taper * 0.9f + 0.1f);
            float jitter = Frac(Mathf.Sin(x * 3.3f) * 1000f) * 0.12f;
            for (int y = 0; y < h; y++)
            {
                float fy = Mathf.Abs((y / (float)(h - 1)) - 0.5f) * 2f;
                float a = fy <= (thickness - jitter) ? Mathf.Lerp(1f, 0.35f, fy / Mathf.Max(0.01f, thickness)) : 0f;
                t.SetPixel(x, y, new Color(DeepInk.r, DeepInk.g, DeepInk.b, a * 0.9f));
            }
        }
        t.Apply();
        return t;
    }

    private static Texture2D SealTex(int size)
    {
        Texture2D t = new Texture2D(size, size, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave,
                                                                               wrapMode = TextureWrapMode.Clamp,
                                                                               filterMode = FilterMode.Bilinear };
        float r = size * 0.18f; // 모서리 둥글기
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool inside = RoundedInside(x, y, size, r);
                bool border = inside && !RoundedInside(x, y, size, r, size * 0.10f);
                Color c;
                if (!inside)
                    c = new Color(0f, 0f, 0f, 0f);
                else if (border)
                    c = new Color(0.529f, 0.149f, 0.118f, 1f); // 짙은 테
                else
                    c = SealRed;
                t.SetPixel(x, y, c);
            }
        }
        t.Apply();
        return t;
    }

    private static bool RoundedInside(int x, int y, int size, float r, float margin = 0f)
    {
        float minX = margin, minY = margin, maxX = size - margin, maxY = size - margin;
        if (x < minX || y < minY || x > maxX || y > maxY)
            return false;
        float cx = Mathf.Clamp(x, minX + r, maxX - r);
        float cy = Mathf.Clamp(y, minY + r, maxY - r);
        float dx = x - cx, dy = y - cy;
        return dx * dx + dy * dy <= r * r || (x >= minX + r && x <= maxX - r) || (y >= minY + r && y <= maxY - r);
    }

    private static Texture2D Bordered(Color fill, Color edge, int size, int border)
    {
        Texture2D t = new Texture2D(size, size, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave,
                                                                               wrapMode = TextureWrapMode.Clamp,
                                                                               filterMode = FilterMode.Point };
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool isEdge = x < border || y < border || x >= size - border || y >= size - border;
                t.SetPixel(x, y, isEdge ? edge : fill);
            }
        }
        t.Apply();
        return t;
    }

    private static float Frac(float v)
    {
        return v - Mathf.Floor(v);
    }
}
}
