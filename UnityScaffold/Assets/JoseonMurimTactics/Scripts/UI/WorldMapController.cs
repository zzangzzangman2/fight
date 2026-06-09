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

        private static readonly MapNode[] Nodes =
        {
            new MapNode("폐사당", "해동문 임시 거점", new Vector2(0.735f, 0.535f), "거점",
                "압록강 폐사당. 현판령에 맞선 첫 깃발이 선 곳이다.", UiTheme.SealRed),
            new MapNode("백두검문", "박성준 · 빛/검", new Vector2(0.846f, 0.405f), "아군 문파",
                "백두산 계열 검문. 박성준이 조선 문파 연합의 깃발을 세우려는 본류다.", UiTheme.Gold),
            new MapNode("설악창문", "백련 · 서리/창", new Vector2(0.852f, 0.563f), "영입 문파",
                "강원 설악산 자락의 창문. 차가운 창끝과 의원의 마음을 함께 지닌 백련의 본가.", UiTheme.NavyLight),
            new MapNode("천뢰봉문", "서아 · 전기/봉", new Vector2(0.815f, 0.612f), "영입 단서",
                "경성의 봉술 문파. 어린 천재 서아가 번개처럼 소문을 몰고 다닌다.", UiTheme.Teal),
            new MapNode("흑연문", "한비연 · 어둠/독", new Vector2(0.852f, 0.610f), "영입 단서",
                "경성 뒷골목과 사신로 정보를 잇는 암기 문파. 한비연의 그림자가 먼저 닿는다.", UiTheme.Navy),
            new MapNode("풍매문", "매화령 · 바람/꽃/부채", new Vector2(0.798f, 0.686f), "영입 문파",
                "전라 남원 쪽 풍류 문파. 매화령의 부채와 꽃바람이 연합의 숨통을 틔운다.", UiTheme.Teal),
            new MapNode("화왕도문", "도아린 · 불/도", new Vector2(0.823f, 0.680f), "영입 문파",
                "경상 화왕산의 도문. 도아린이 불길 같은 돌파력으로 중원식 현판령에 맞선다.", UiTheme.SealRed),
            new MapNode("압록강 나루", "강변 연락로", new Vector2(0.700f, 0.520f), "탐색 가능",
                "중원 감찰단과 조선 문파의 소문이 가장 먼저 섞이는 나루.", UiTheme.Teal),
            new MapNode("의주 객잔", "소문과 영입 단서", new Vector2(0.675f, 0.565f), "새 소문",
                "객잔에는 한비연의 이름과 중원 사신로의 움직임이 함께 돈다.", UiTheme.Gold),
            new MapNode("한양 연락망", "조정 관심", new Vector2(0.833f, 0.622f), "잠김",
                "조정과 무림의 경계에서 명분과 체면이 오가는 길.", UiTheme.Navy),
            new MapNode("중원 사신로", "감찰단 보급선", new Vector2(0.535f, 0.610f), "위험",
                "현판령 문서와 감찰단 물자가 오가는 길목.", UiTheme.SealRed),
            new MapNode("감찰단 거점", "중원 강경파", new Vector2(0.430f, 0.705f), "위험",
                "강경파 감찰단이 서쪽 문파들을 장악하며 세력을 키운다.", UiTheme.SealRed),
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
            GUI.Label(new Rect(44f * s, 78f * s, w - 88f * s, 28f * s), "해동 조선 문파와 중원 감찰단의 압박선", UiTheme.BodyCenter);
            UiTheme.DrawDivider(w * 0.5f, 114f * s, w - 96f * s);

            Rect mapPanel = new Rect(36f * s, 136f * s, w - 410f * s, h - 220f * s);
            Rect sidePanel = new Rect(mapPanel.xMax + 22f * s, mapPanel.y, 330f * s, mapPanel.height);
            Rect bottomBar = new Rect(mapPanel.x, mapPanel.yMax + 18f * s, mapPanel.width + 22f * s + sidePanel.width, 58f * s);

            DrawMapPanel(mapPanel, s);
            DrawSidePanel(sidePanel, s);
            DrawBottomBar(bottomBar, s);
        }

        private void DrawMapPanel(Rect panel, float s)
        {
            UiTheme.DrawPanel(panel);

            Rect toolbar = new Rect(panel.x + 18f * s, panel.y + 16f * s, panel.width - 36f * s, 48f * s);
            DrawToolbar(toolbar, s);

            Rect viewport = new Rect(panel.x + 18f * s, toolbar.yMax + 12f * s, panel.width - 36f * s, panel.height - 86f * s);
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
            UiTheme.DrawFill(new Rect(0f, 0f, viewport.width, viewport.height), new Color(UiTheme.Hanji.r, UiTheme.Hanji.g, UiTheme.Hanji.b, 0.04f));
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
            GUI.Label(new Rect(x - 126f * s, r.y + 9f * s, 118f * s, 28f * s),
                $"배율 {Mathf.RoundToInt(zoom * 100f)}%", UiTheme.SmallMuted);

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
            GUI.Label(new Rect(story.x + 12f * s, story.y + 10f * s, story.width - 24f * s, story.height - 20f * s), node.description, UiTheme.Body);
            y = story.yMax + 16f * s;

            GUI.Label(new Rect(x, y, w, 30f * s), "강호 동향", UiTheme.Heading);
            y += 38f * s;
            GUI.Label(new Rect(x, y, w, 120f * s),
                "· 조선은 오른쪽 해동 권역으로 표시된다.\n· 백두검문이 연합의 중심축이다.\n· 붉은 사신로는 중원 감찰단의 압박선이다.",
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
                    Vector2 oldOrigin = new Vector2(viewport.width * 0.5f, viewport.height * 0.5f)
                        - new Vector2(drawSize.x * centerUv.x, drawSize.y * centerUv.y);
                    Vector2 uvAtMouse = new Vector2(
                        Mathf.InverseLerp(oldOrigin.x, oldOrigin.x + drawSize.x, local.x),
                        Mathf.InverseLerp(oldOrigin.y, oldOrigin.y + drawSize.y, local.y));
                    Vector2 newDrawSize = drawSize * (zoom / oldZoom);
                    Vector2 newOrigin = local - new Vector2(newDrawSize.x * uvAtMouse.x, newDrawSize.y * uvAtMouse.y);
                    centerUv = new Vector2(
                        (viewport.width * 0.5f - newOrigin.x) / newDrawSize.x,
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

                UiTheme.DrawFill(new Rect(pos.x - radius, pos.y - radius, radius * 2f, radius * 2f), new Color(n.color.r, n.color.g, n.color.b, i == selectedNode ? 0.45f : 0.26f));
                UiTheme.DrawSeal(new Rect(pos.x - radius * 0.74f, pos.y - radius * 0.74f, radius * 1.48f, radius * 1.48f), StatusGlyph(n.status), i == selectedNode ? -7f : -3f);

                if (i == selectedNode)
                {
                    GUIStyle label = new GUIStyle(UiTheme.Small)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontStyle = FontStyle.Bold
                    };
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
            if (status == "위험") return "危";
            if (status == "잠김") return "鎖";
            if (status == "새 소문") return "聞";
            if (status == "탐색 가능") return "探";
            if (status == "영입 문파") return "緣";
            if (status == "영입 단서") return "聞";
            if (status == "아군 문파") return "本";
            return "門";
        }

        private static string FactionHint(MapNode node)
        {
            if (node.status == "위험") return "중원 감찰단";
            if (node.status == "아군 문파") return "해동문/백두검문";
            if (node.status == "영입 문파") return "조선문파연합 후보";
            if (node.status == "영입 단서") return "조선 문파 소문";
            if (node.status == "새 소문") return "객잔/상인";
            if (node.status == "거점") return "해동문";
            return "미확인";
        }

        private static string NextHint(MapNode node)
        {
            if (node.status == "아군 문파") return "연합 중심지";
            if (node.status == "영입 문파") return "인연 임무 필요";
            if (node.status == "영입 단서") return "소문 추적";
            if (node.status == "잠김") return "소문 또는 임무 필요";
            if (node.status == "위험") return "정찰 권장";
            if (node.status == "새 소문") return "객잔 방문";
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
    }
}
