using UnityEngine;

namespace JoseonMurimTactics
{
/// <summary>
/// [8] WorldMap — 게임용 조선-중원 통합 강호도.
/// </summary>
[DisallowMultipleComponent]
public sealed class WorldMapController : MonoBehaviour
{
    private const float MinZoom = 0.92f;
    private const float MaxZoom = 2.65f;
    private const float InitialZoom = 1.0f;

    private static readonly Vector2 InitialFocus = new Vector2(0.5f, 0.5f);

    private static readonly MapNode[] Nodes = {
        new MapNode("백두천광검문", "박성준 · 빛/검", new Vector2(0.846f, 0.405f), "거점",
                    "백두산 천지의 새벽빛을 검에 담는 문파. 꺼져가는 천광이 다시 타오르려 한다.", UiTheme.Gold),
        new MapNode("설악창문", "백련 · 서리/창", new Vector2(0.852f, 0.563f), "영입 문파",
                    "강원 설악산 자락의 창문. 차가운 창끝과 의원의 마음을 함께 지닌 백련의 본가.", UiTheme.NavyLight),
        new MapNode("천뢰봉문", "진서율 · 전기/봉", new Vector2(0.815f, 0.612f), "영입 단서",
                    "경성의 봉술 문파. 천재 봉술가 진서율이 번개처럼 소문을 몰고 다닌다.", UiTheme.Teal),
        new MapNode("흑련암문", "한비연 · 어둠/독", new Vector2(0.852f, 0.610f), "영입 단서",
                    "황해도 구월산의 암기 문파. 한비연의 그림자가 독살 누명의 단서를 쫓는다.", UiTheme.Navy),
        new MapNode("화접풍류문", "신서아 · 바람/꽃/부채", new Vector2(0.798f, 0.686f), "영입 문파",
                    "전라 남원 쪽 풍류 문파. 막내 신서아의 부채와 꽃바람이 연합의 숨통을 틔운다.", UiTheme.Teal),
        new MapNode("화왕도문", "도아린 · 불/도", new Vector2(0.823f, 0.680f), "영입 문파",
                    "경상 화왕산의 도문. 도아린이 불길 같은 돌파력으로 길을 연다.", UiTheme.SealRed),
        new MapNode("소백촌", "마을 신뢰", new Vector2(0.700f, 0.520f), "탐색 가능",
                    "백두천광검문 아래 마을. 생계, 복구, 신뢰가 1장의 중심이다.", UiTheme.Teal),
        new MapNode("철랑문", "2장 적대 문파", new Vector2(0.675f, 0.565f), "위험",
                    "백두산 영맥과 천광검문의 비급을 노리는 중원 하위 문파.", UiTheme.SealRed),
        new MapNode("모용세가 사절로", "3장 위협", new Vector2(0.535f, 0.610f), "잠김",
                    "후견을 명분으로 백두산에 손을 뻗는 오대세가의 길목.", UiTheme.Navy),
        new MapNode("중원 사신로", "밀정 보급선", new Vector2(0.430f, 0.705f), "위험",
                    "철랑문과 모용세가의 밀서와 보급이 오가는 길목.", UiTheme.SealRed),
    };

    private static readonly MapLabel[] Labels = {
        new MapLabel("구파일방", "장경각 · 진무궁", new Vector2(0.245f, 0.245f), "派", UiTheme.Gold, new Vector2(32f, -54f), true),
        new MapLabel("오대세가", "남궁검각 · 기문루", new Vector2(0.430f, 0.520f), "家", UiTheme.Teal, new Vector2(-148f, -34f), true),
        new MapLabel("중원무림맹", "의협전 · 총단", new Vector2(0.535f, 0.395f), "盟", UiTheme.Gold, new Vector2(-92f, -62f), true),
        new MapLabel("마교", "천마신전 · 혈월단", new Vector2(0.145f, 0.695f), "魔", UiTheme.SealRed, new Vector2(28f, -58f), true),
        new MapLabel("모용세가", "모용별궁 · 사절로", new Vector2(0.535f, 0.610f), "慕", UiTheme.SealRed, new Vector2(28f, -50f), true),

        new MapLabel("백두천광검문", "백두산 천검단", new Vector2(0.846f, 0.405f), "本", UiTheme.Gold, new Vector2(24f, -54f), false),
        new MapLabel("설악창문", "설악빙창대", new Vector2(0.852f, 0.563f), "槍", UiTheme.NavyLight, new Vector2(-126f, -42f), false),
        new MapLabel("천뢰봉문", "경성 번뢰전", new Vector2(0.815f, 0.612f), "雷", UiTheme.Teal, new Vector2(-126f, 6f), false),
        new MapLabel("흑련암문", "구월산 흑련별원", new Vector2(0.852f, 0.610f), "毒", UiTheme.Navy, new Vector2(26f, -10f), false),
        new MapLabel("화접풍류문", "남원 매화풍루", new Vector2(0.798f, 0.686f), "花", UiTheme.Teal, new Vector2(-118f, 10f), false),
        new MapLabel("화왕도문", "화왕산 도장", new Vector2(0.823f, 0.680f), "火", UiTheme.SealRed, new Vector2(28f, 24f), false),
    };

    private static readonly MapMonument[] Monuments = {
        new MapMonument(new Vector2(0.245f, 0.245f), "寺", UiTheme.Gold, 1.2f),
        new MapMonument(new Vector2(0.360f, 0.185f), "山", UiTheme.NavyLight, 1.0f),
        new MapMonument(new Vector2(0.430f, 0.520f), "家", UiTheme.Teal, 1.05f),
        new MapMonument(new Vector2(0.535f, 0.395f), "盟", UiTheme.Gold, 1.15f),
        new MapMonument(new Vector2(0.145f, 0.695f), "魔", UiTheme.SealRed, 1.2f),
        new MapMonument(new Vector2(0.535f, 0.610f), "慕", UiTheme.SealRed, 1.0f),
        new MapMonument(new Vector2(0.846f, 0.405f), "本", UiTheme.Gold, 1.05f),
        new MapMonument(new Vector2(0.852f, 0.563f), "槍", UiTheme.NavyLight, 0.74f),
        new MapMonument(new Vector2(0.815f, 0.612f), "雷", UiTheme.Teal, 0.74f),
        new MapMonument(new Vector2(0.852f, 0.610f), "毒", UiTheme.Navy, 0.74f),
        new MapMonument(new Vector2(0.798f, 0.686f), "花", UiTheme.Teal, 0.74f),
        new MapMonument(new Vector2(0.823f, 0.680f), "火", UiTheme.SealRed, 0.74f),
    };

    private GameRoot root;
    private Texture2D worldMap;
    private int selectedNode;
    private float zoom = InitialZoom;
    private Vector2 centerUv = InitialFocus;
    private bool dragging;

    private void Awake()
    {
        root = GameRoot.EnsureExists();
        worldMap = LoadMapTexture("WorldMap/joseon_murim_game_map");
    }

    private void OnGUI()
    {
        UiTheme.Begin(true);
        UiTheme.DrawMountains();

        float w = Screen.width;
        float h = Screen.height;
        float s = UiTheme.Scale;

        UiTheme.LabelShadow(new Rect(44f * s, 24f * s, w - 88f * s, 58f * s), "조선-중원 강호도", UiTheme.Title);
        GUI.Label(new Rect(44f * s, 78f * s, w - 88f * s, 28f * s), "백두산 문파 재건과 중원 세가의 압박선",
                  UiTheme.BodyCenter);
        UiTheme.DrawDivider(w * 0.5f, 114f * s, w - 96f * s);

        Rect mapPanel = new Rect(36f * s, 136f * s, w - 410f * s, h - 220f * s);
        Rect sidePanel = new Rect(mapPanel.xMax + 22f * s, mapPanel.y, 330f * s, mapPanel.height);
        Rect bottomBar =
            new Rect(mapPanel.x, mapPanel.yMax + 18f * s, mapPanel.width + 22f * s + sidePanel.width, 58f * s);

        DrawMapPanel(mapPanel, s);
        DrawSidePanel(sidePanel, s);
        DrawBottomBar(bottomBar, s);
    }

    private void DrawMapPanel(Rect panel, float s)
    {
        UiTheme.DrawPanel(panel);

        Rect toolbar = new Rect(panel.x + 18f * s, panel.y + 16f * s, panel.width - 36f * s, 48f * s);
        DrawToolbar(toolbar, s);

        Rect viewport =
            new Rect(panel.x + 18f * s, toolbar.yMax + 12f * s, panel.width - 36f * s, panel.height - 86f * s);
        UiTheme.DrawFill(viewport, new Color(0.170f, 0.185f, 0.172f, 1f));

        if (worldMap == null)
        {
            GUI.Label(viewport, "강호도 이미지를 불러오지 못했습니다.", UiTheme.BodyCenter);
            return;
        }

        Vector2 baseSize = FitSize(viewport, worldMap);
        Vector2 drawSize = baseSize * zoom;
        HandleMapInput(viewport, drawSize);
        drawSize = baseSize * zoom;
        centerUv = ClampCenter(centerUv, viewport, drawSize);

        Vector2 localCenter = new Vector2(viewport.width * 0.5f, viewport.height * 0.5f);
        Vector2 origin = localCenter - new Vector2(drawSize.x * centerUv.x, drawSize.y * centerUv.y);
        Rect mapRect = new Rect(origin.x, origin.y, drawSize.x, drawSize.y);

        GUI.BeginGroup(viewport);
        GUI.color = new Color(0.98f, 0.94f, 0.84f, 1f);
        GUI.DrawTexture(mapRect, worldMap, ScaleMode.StretchToFill);
        GUI.color = Color.white;
        UiTheme.DrawFill(new Rect(0f, 0f, viewport.width, viewport.height),
                         new Color(UiTheme.Hanji.r, UiTheme.Hanji.g, UiTheme.Hanji.b, 0.04f));
        DrawMonuments(mapRect, s);
        DrawLabels(mapRect, s);
        DrawNodes(mapRect, s);
        GUI.EndGroup();

        UiTheme.DrawFill(new Rect(viewport.x, viewport.y, viewport.width, 2f * s), UiTheme.Gold);
        UiTheme.DrawFill(new Rect(viewport.x, viewport.yMax - 2f * s, viewport.width, 2f * s), UiTheme.Gold);
        UiTheme.DrawFill(new Rect(viewport.x, viewport.y, 2f * s, viewport.height), UiTheme.Gold);
        UiTheme.DrawFill(new Rect(viewport.xMax - 2f * s, viewport.y, 2f * s, viewport.height), UiTheme.Gold);
    }

    private void DrawToolbar(Rect r, float s)
    {
        GUI.Label(new Rect(r.x, r.y + 8f * s, r.width * 0.55f, 28f * s),
                  "통합 강호도 · 노드를 선택하면 상세 정보가 열린다", UiTheme.SmallMuted);

        float bw = 58f * s;
        float x = r.xMax - (bw * 3f + 16f * s);
        GUI.Label(new Rect(x - 126f * s, r.y + 9f * s, 118f * s, 28f * s), $"배율 {Mathf.RoundToInt(zoom * 100f)}%",
                  UiTheme.SmallMuted);

        if (GUI.Button(new Rect(x, r.y, bw, r.height), "-", UiTheme.Button))
        {
            zoom = Mathf.Max(MinZoom, zoom / 1.18f);
        }

        if (GUI.Button(new Rect(x + bw + 8f * s, r.y, bw, r.height), "+", UiTheme.Button))
        {
            zoom = Mathf.Min(MaxZoom, zoom * 1.18f);
        }

        if (GUI.Button(new Rect(x + (bw + 8f * s) * 2f, r.y, bw, r.height), "전체", UiTheme.Button))
        {
            ToggleOverview();
        }
    }

    private void DrawSidePanel(Rect panel, float s)
    {
        UiTheme.DrawPanel(panel, true);
        MapNode node = Nodes[Mathf.Clamp(selectedNode, 0, Nodes.Length - 1)];

        float x = panel.x + 22f * s;
        float y = panel.y + 18f * s;
        float w = panel.width - 44f * s;

        GUI.Label(new Rect(x, y, w, 34f * s), node.name, UiTheme.Heading);
        UiTheme.DrawSeal(new Rect(panel.xMax - 64f * s, panel.y + 16f * s, 42f * s, 42f * s), StatusGlyph(node.status));
        y += 42f * s;

        GUI.Label(new Rect(x, y, w, 26f * s), node.subtitle, UiTheme.SmallMuted);
        y += 38f * s;
        Line(x, ref y, w, s, "상태", node.status);
        Line(x, ref y, w, s, "세력", FactionHint(node));
        Line(x, ref y, w, s, "다음", NextHint(node));
        y += 12f * s;

        Rect story = new Rect(x, y, w, 132f * s);
        UiTheme.DrawFill(story, new Color(1f, 0.98f, 0.92f, 0.82f));
        GUI.Label(new Rect(story.x + 12f * s, story.y + 10f * s, story.width - 24f * s, story.height - 20f * s),
                  node.description, UiTheme.Body);
        y = story.yMax + 16f * s;

        GUI.Label(new Rect(x, y, w, 30f * s), "강호 동향", UiTheme.Heading);
        y += 38f * s;
        GUI.Label(new Rect(x, y, w, 120f * s),
                  "· 조선은 오른쪽 해동 권역으로 표시된다.\n· 백두천광검문이 재건의 중심축이다.\n· 붉은 사신로는 중원 " +
                  "세가와 하위 문파의 압박선이다.",
                  UiTheme.Small);
    }

    private void DrawBottomBar(Rect bar, float s)
    {
        UiTheme.DrawPanel(bar, true);
        float bw = 190f * s;
        if (GUI.Button(new Rect(bar.x + 16f * s, bar.y + 8f * s, bw, 42f * s), "거점으로", UiTheme.Button))
        {
            root.Flow.GoToHub(SceneNames.HubPyesadang);
        }

        if (GUI.Button(new Rect(bar.x + 216f * s, bar.y + 8f * s, bw, 42f * s), "임무 게시판", UiTheme.ButtonPrimary))
        {
            root.Flow.GoToMissionBoard();
        }

        MapNode node = Nodes[Mathf.Clamp(selectedNode, 0, Nodes.Length - 1)];
        GUI.Label(new Rect(bar.x + 424f * s, bar.y + 10f * s, bar.width - 440f * s, 38f * s),
                  $"{node.name} · {node.status}", UiTheme.Body);
    }

    private void HandleMapInput(Rect viewport, Vector2 drawSize)
    {
        Event e = Event.current;
        if (e == null)
        {
            return;
        }

        bool inside = viewport.Contains(e.mousePosition);
        if (e.type == EventType.ScrollWheel && inside)
        {
            float oldZoom = zoom;
            float factor = Mathf.Pow(1.12f, -e.delta.y);
            zoom = Mathf.Clamp(zoom * factor, MinZoom, MaxZoom);
            if (!Mathf.Approximately(oldZoom, zoom))
            {
                Vector2 local = e.mousePosition - new Vector2(viewport.x, viewport.y);
                Vector2 oldOrigin = new Vector2(viewport.width * 0.5f, viewport.height * 0.5f) -
                                    new Vector2(drawSize.x * centerUv.x, drawSize.y * centerUv.y);
                Vector2 uvAtMouse = new Vector2(Mathf.InverseLerp(oldOrigin.x, oldOrigin.x + drawSize.x, local.x),
                                                Mathf.InverseLerp(oldOrigin.y, oldOrigin.y + drawSize.y, local.y));
                Vector2 newDrawSize = drawSize * (zoom / oldZoom);
                Vector2 newOrigin = local - new Vector2(newDrawSize.x * uvAtMouse.x, newDrawSize.y * uvAtMouse.y);
                centerUv = new Vector2((viewport.width * 0.5f - newOrigin.x) / newDrawSize.x,
                                       (viewport.height * 0.5f - newOrigin.y) / newDrawSize.y);
            }

            e.Use();
        }
        else if (e.type == EventType.MouseDown && inside && e.button == 0)
        {
            dragging = true;
        }
        else if (e.type == EventType.MouseDrag && dragging && e.button == 0)
        {
            centerUv -= new Vector2(e.delta.x / drawSize.x, e.delta.y / drawSize.y);
            e.Use();
        }
        else if (e.type == EventType.MouseUp && e.button == 0)
        {
            dragging = false;
        }
        else if (e.type == EventType.MouseDown && inside && e.button == 2)
        {
            ToggleOverview();
            e.Use();
        }
    }

    private void DrawNodes(Rect mapRect, float s)
    {
        for (int i = 0; i < Nodes.Length; i++)
        {
            MapNode n = Nodes[i];
            Vector2 pos = new Vector2(mapRect.x + mapRect.width * n.uv.x, mapRect.y + mapRect.height * n.uv.y);
            float radius = (i == selectedNode ? 17f : 12f) * s;
            Rect hit = new Rect(pos.x - 42f * s, pos.y - 42f * s, 84f * s, 84f * s);

            UiTheme.DrawFill(new Rect(pos.x - radius, pos.y - radius, radius * 2f, radius * 2f),
                             new Color(n.color.r, n.color.g, n.color.b, i == selectedNode ? 0.45f : 0.26f));
            UiTheme.DrawSeal(new Rect(pos.x - radius * 0.74f, pos.y - radius * 0.74f, radius * 1.48f, radius * 1.48f),
                             StatusGlyph(n.status), i == selectedNode ? -7f : -3f);

            if (i == selectedNode)
            {
                GUIStyle label =
                    new GUIStyle(UiTheme.Small) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                label.normal.textColor = UiTheme.Navy;
                Rect tag = new Rect(pos.x - 76f * s, pos.y + 21f * s, 152f * s, 28f * s);
                UiTheme.DrawFill(tag, new Color(1f, 0.96f, 0.82f, 0.82f));
                GUI.Label(tag, n.name, label);
            }

            if (GUI.Button(hit, GUIContent.none, GUIStyle.none))
            {
                selectedNode = i;
                centerUv = n.uv;
                zoom = Mathf.Max(zoom, 1.28f);
            }
        }
    }

    private void DrawLabels(Rect mapRect, float s)
    {
        for (int i = 0; i < Labels.Length; i++)
        {
            DrawLabel(Labels[i], mapRect, s);
        }
    }

    private void DrawLabel(MapLabel label, Rect mapRect, float s)
    {
        Vector2 anchor = MapToLocal(mapRect, label.uv);
        Vector2 offset = label.offset * s;
        float width = (label.major ? 162f : 116f) * s;
        float height = (label.major ? 52f : 42f) * s;
        Rect rect = new Rect(anchor.x + offset.x, anchor.y + offset.y, width, height);
        rect.x = Mathf.Clamp(rect.x, 8f * s, mapRect.width - rect.width - 8f * s);
        rect.y = Mathf.Clamp(rect.y, 8f * s, mapRect.height - rect.height - 8f * s);

        Vector2 edge = new Vector2(Mathf.Clamp(anchor.x, rect.x, rect.xMax), Mathf.Clamp(anchor.y, rect.y, rect.yMax));
        DrawLine(anchor, edge, new Color(label.color.r, label.color.g, label.color.b, label.major ? 0.56f : 0.38f), label.major ? 2.0f * s : 1.3f * s);

        UiTheme.DrawFill(new Rect(rect.x + 3f * s, rect.y + 4f * s, rect.width, rect.height), new Color(0.050f, 0.037f, 0.026f, 0.30f));
        UiTheme.DrawFill(rect, label.major ? new Color(1f, 0.945f, 0.760f, 0.90f) : new Color(1f, 0.955f, 0.810f, 0.82f));
        UiTheme.DrawFill(new Rect(rect.x, rect.y, 4f * s, rect.height), new Color(label.color.r, label.color.g, label.color.b, 0.88f));

        Rect seal = new Rect(rect.x + 9f * s, rect.y + 8f * s, (label.major ? 32f : 25f) * s, (label.major ? 32f : 25f) * s);
        UiTheme.DrawSeal(seal, label.glyph, label.major ? -4f : -7f);

        GUIStyle nameStyle = new GUIStyle(label.major ? UiTheme.Body : UiTheme.Small)
        {
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperLeft,
            clipping = TextClipping.Clip
        };
        nameStyle.normal.textColor = UiTheme.Navy;
        GUIStyle detailStyle = new GUIStyle(UiTheme.SmallMuted) { clipping = TextClipping.Clip };

        float textX = seal.xMax + 8f * s;
        GUI.Label(new Rect(textX, rect.y + 6f * s, rect.xMax - textX - 6f * s, 22f * s), label.name, nameStyle);
        GUI.Label(new Rect(textX, rect.y + (label.major ? 28f : 25f) * s, rect.xMax - textX - 6f * s, 18f * s), label.detail, detailStyle);
    }

    private void DrawMonuments(Rect mapRect, float s)
    {
        for (int i = 0; i < Monuments.Length; i++)
        {
            MapMonument m = Monuments[i];
            Vector2 pos = MapToLocal(mapRect, m.uv);
            float w = 25f * m.scale * s;
            float h = 30f * m.scale * s;
            Color glow = new Color(m.color.r, m.color.g, m.color.b, 0.20f);
            UiTheme.DrawFill(new Rect(pos.x - w * 0.7f, pos.y - h * 0.68f, w * 1.4f, h * 1.30f), glow);

            Rect body = new Rect(pos.x - w * 0.40f, pos.y - h * 0.12f, w * 0.80f, h * 0.58f);
            Rect roof = new Rect(pos.x - w * 0.56f, pos.y - h * 0.42f, w * 1.12f, h * 0.22f);
            UiTheme.DrawFill(roof, new Color(m.color.r, m.color.g, m.color.b, 0.82f));
            UiTheme.DrawFill(body, new Color(0.92f, 0.80f, 0.56f, 0.84f));
            UiTheme.DrawSeal(new Rect(pos.x - w * 0.36f, pos.y - h * 0.42f, w * 0.72f, w * 0.72f), m.glyph, -8f);
        }
    }

    private static Vector2 MapToLocal(Rect mapRect, Vector2 uv)
    {
        return new Vector2(mapRect.x + mapRect.width * uv.x, mapRect.y + mapRect.height * uv.y);
    }

    private static void DrawLine(Vector2 a, Vector2 b, Color color, float width)
    {
        if (width <= 0f)
        {
            return;
        }

        Matrix4x4 matrix = GUI.matrix;
        Color oldColor = GUI.color;
        Vector2 delta = b - a;
        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        GUI.color = color;
        GUIUtility.RotateAroundPivot(angle, a);
        GUI.DrawTexture(new Rect(a.x, a.y - width * 0.5f, delta.magnitude, width), Texture2D.whiteTexture);
        GUI.matrix = matrix;
        GUI.color = oldColor;
    }

    private void ToggleOverview()
    {
        if (zoom > 1.05f)
        {
            zoom = MinZoom;
            centerUv = new Vector2(0.5f, 0.5f);
        }
        else
        {
            zoom = InitialZoom;
            centerUv = Nodes[Mathf.Clamp(selectedNode, 0, Nodes.Length - 1)].uv;
        }
    }

    private static Texture2D LoadMapTexture(string path)
    {
        Texture2D texture = Resources.Load<Texture2D>(path);
        if (texture != null)
        {
            return texture;
        }

        Sprite sprite = Resources.Load<Sprite>(path);
        return sprite != null ? sprite.texture : null;
    }

    private static Vector2 FitSize(Rect viewport, Texture texture)
    {
        float aspect = texture.width / (float)texture.height;
        float width = viewport.width;
        float height = width / aspect;
        if (height > viewport.height)
        {
            height = viewport.height;
            width = height * aspect;
        }

        return new Vector2(width, height);
    }

    private static Vector2 ClampCenter(Vector2 center, Rect viewport, Vector2 drawSize)
    {
        float minX = drawSize.x <= viewport.width ? 0.5f : viewport.width * 0.5f / drawSize.x;
        float maxX = drawSize.x <= viewport.width ? 0.5f : 1f - minX;
        float minY = drawSize.y <= viewport.height ? 0.5f : viewport.height * 0.5f / drawSize.y;
        float maxY = drawSize.y <= viewport.height ? 0.5f : 1f - minY;
        return new Vector2(Mathf.Clamp(center.x, minX, maxX), Mathf.Clamp(center.y, minY, maxY));
    }

    private static string StatusGlyph(string status)
    {
        if (status == "위험")
            return "危";
        if (status == "잠김")
            return "鎖";
        if (status == "새 소문")
            return "聞";
        if (status == "탐색 가능")
            return "探";
        if (status == "영입 문파")
            return "緣";
        if (status == "영입 단서")
            return "聞";
        if (status == "아군 문파")
            return "本";
        return "門";
    }

    private static string FactionHint(MapNode node)
    {
        if (node.status == "위험")
            return "중원 하위 문파";
        if (node.status == "아군 문파")
            return "백두천광검문";
        if (node.status == "영입 문파")
            return "조선문파연합 후보";
        if (node.status == "영입 단서")
            return "조선 문파 소문";
        if (node.status == "새 소문")
            return "객잔/상인";
        if (node.status == "거점")
            return "백두천광검문";
        return "미확인";
    }

    private static string NextHint(MapNode node)
    {
        if (node.status == "아군 문파")
            return "연합 중심지";
        if (node.status == "영입 문파")
            return "인연 임무 필요";
        if (node.status == "영입 단서")
            return "소문 추적";
        if (node.status == "잠김")
            return "소문 또는 임무 필요";
        if (node.status == "위험")
            return "정찰 권장";
        if (node.status == "새 소문")
            return "객잔 방문";
        return "출정 준비";
    }

    private static void Line(float x, ref float y, float w, float s, string label, string value)
    {
        GUI.Label(new Rect(x, y, w * 0.34f, 28f * s), label, UiTheme.SmallMuted);
        GUI.Label(new Rect(x + w * 0.34f, y, w * 0.66f, 28f * s), value, UiTheme.Body);
        y += 32f * s;
    }

    private struct MapNode
    {
        public readonly string name;
        public readonly string subtitle;
        public readonly Vector2 uv;
        public readonly string status;
        public readonly string description;
        public readonly Color color;

        public MapNode(string name, string subtitle, Vector2 uv, string status, string description, Color color)
        {
            this.name = name;
            this.subtitle = subtitle;
            this.uv = uv;
            this.status = status;
            this.description = description;
            this.color = color;
        }
    }

    private struct MapLabel
    {
        public readonly string name;
        public readonly string detail;
        public readonly Vector2 uv;
        public readonly string glyph;
        public readonly Color color;
        public readonly Vector2 offset;
        public readonly bool major;

        public MapLabel(string name, string detail, Vector2 uv, string glyph, Color color, Vector2 offset, bool major)
        {
            this.name = name;
            this.detail = detail;
            this.uv = uv;
            this.glyph = glyph;
            this.color = color;
            this.offset = offset;
            this.major = major;
        }
    }

    private struct MapMonument
    {
        public readonly Vector2 uv;
        public readonly string glyph;
        public readonly Color color;
        public readonly float scale;

        public MapMonument(Vector2 uv, string glyph, Color color, float scale)
        {
            this.uv = uv;
            this.glyph = glyph;
            this.color = color;
            this.scale = scale;
        }
    }
}
}
