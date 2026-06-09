using UnityEngine;

namespace JoseonMurimTactics
{
    /// <summary>
    /// [8] WorldMap — Downloads/지도1, 지도2를 프로젝트 Resources로 가져와 쓰는
    /// 확대/축소 가능한 중원-조선 전략 지도.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorldMapController : MonoBehaviour
    {
        private const float MinZoom = 0.82f;
        private const float MaxZoom = 2.65f;
        private const float InitialZoom = 1.78f;

        private static readonly Vector2 InitialFocus = new Vector2(0.805f, 0.360f);

        private static readonly MapNode[] Nodes =
        {
            new MapNode("폐사당", "해동문 임시 거점", new Vector2(0.805f, 0.370f), "거점",
                "압록강 폐사당. 현판령에 맞선 첫 깃발이 선 곳이다.", UiTheme.SealRed),
            new MapNode("압록강 나루", "강변 연락로", new Vector2(0.835f, 0.405f), "탐색 가능",
                "중원 감찰단과 조선 문파의 소문이 가장 먼저 섞이는 나루.", UiTheme.Teal),
            new MapNode("의주 객잔", "소문과 영입 단서", new Vector2(0.770f, 0.455f), "새 소문",
                "객잔에는 한비연의 이름과 중원 사신로의 움직임이 함께 돈다.", UiTheme.Gold),
            new MapNode("백두 산길", "북방 산문", new Vector2(0.865f, 0.235f), "잠김",
                "눈 덮인 산길. 백두 계열 문파의 흔적이 남아 있다.", UiTheme.NavyLight),
            new MapNode("한양 연락망", "조정 관심", new Vector2(0.820f, 0.575f), "잠김",
                "조정과 무림의 경계에서 명분과 체면이 오가는 길.", UiTheme.Navy),
            new MapNode("중원 사신로", "감찰단 보급선", new Vector2(0.680f, 0.455f), "위험",
                "현판령 문서와 감찰단 물자가 오가는 길목.", UiTheme.SealRed),
            new MapNode("청성 감찰 거점", "중원 강경파", new Vector2(0.440f, 0.555f), "위험",
                "강경파 감찰단이 서쪽 문파들을 장악하며 세력을 키운다.", UiTheme.SealRed),
        };

        private GameRoot root;
        private Texture2D factionMap;
        private Texture2D terrainMap;
        private int selectedNode;
        private int mapLayer = 1;
        private float zoom = InitialZoom;
        private Vector2 centerUv = InitialFocus;
        private bool dragging;

        private void Awake()
        {
            root = GameRoot.EnsureExists();
            factionMap = LoadMapTexture("WorldMap/jido_1");
            terrainMap = LoadMapTexture("WorldMap/jido_2");
        }

        private void OnGUI()
        {
            UiTheme.Begin(true);
            UiTheme.DrawMountains();

            float w = Screen.width;
            float h = Screen.height;
            float s = UiTheme.Scale;

            UiTheme.LabelShadow(new Rect(44f * s, 24f * s, w - 88f * s, 58f * s), "중원 강호도", UiTheme.Title);
            GUI.Label(new Rect(44f * s, 78f * s, w - 88f * s, 28f * s), "압록강의 현판령 이후, 조선 문파와 중원 감찰단의 길", UiTheme.BodyCenter);
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

            Texture2D map = ActiveMap;
            if (map == null)
            {
                GUI.Label(viewport, "지도 이미지를 불러오지 못했습니다.", UiTheme.BodyCenter);
                return;
            }

            Vector2 baseSize = FitSize(viewport, map);
            Vector2 drawSize = baseSize * zoom;
            HandleMapInput(viewport, drawSize);
            drawSize = baseSize * zoom;
            centerUv = ClampCenter(centerUv, viewport, drawSize);

            Vector2 localCenter = new Vector2(viewport.width * 0.5f, viewport.height * 0.5f);
            Vector2 origin = localCenter - new Vector2(drawSize.x * centerUv.x, drawSize.y * centerUv.y);
            Rect mapRect = new Rect(origin.x, origin.y, drawSize.x, drawSize.y);

            GUI.BeginGroup(viewport);
            GUI.color = new Color(0.98f, 0.94f, 0.84f, 1f);
            GUI.DrawTexture(mapRect, map, ScaleMode.StretchToFill);
            GUI.color = Color.white;
            UiTheme.DrawFill(new Rect(0f, 0f, viewport.width, viewport.height), new Color(UiTheme.Hanji.r, UiTheme.Hanji.g, UiTheme.Hanji.b, 0.08f));
            DrawMapGrid(viewport, s);
            DrawNodes(mapRect, s);
            GUI.EndGroup();

            UiTheme.DrawFill(new Rect(viewport.x, viewport.y, viewport.width, 2f * s), UiTheme.Gold);
            UiTheme.DrawFill(new Rect(viewport.x, viewport.yMax - 2f * s, viewport.width, 2f * s), UiTheme.Gold);
            UiTheme.DrawFill(new Rect(viewport.x, viewport.y, 2f * s, viewport.height), UiTheme.Gold);
            UiTheme.DrawFill(new Rect(viewport.xMax - 2f * s, viewport.y, 2f * s, viewport.height), UiTheme.Gold);
        }

        private void DrawToolbar(Rect r, float s)
        {
            float tabW = 120f * s;
            if (GUI.Button(new Rect(r.x, r.y, tabW, r.height), "지형도", mapLayer == 1 ? UiTheme.ButtonPrimary : UiTheme.Button))
            {
                mapLayer = 1;
            }

            if (GUI.Button(new Rect(r.x + tabW + 8f * s, r.y, tabW, r.height), "세력도", mapLayer == 0 ? UiTheme.ButtonPrimary : UiTheme.Button))
            {
                mapLayer = 0;
            }

            float bw = 48f * s;
            float x = r.xMax - (bw * 3f + 16f * s);
            if (GUI.Button(new Rect(x, r.y, bw, r.height), "-", UiTheme.Button))
            {
                zoom = Mathf.Max(MinZoom, zoom / 1.18f);
            }

            if (GUI.Button(new Rect(x + bw + 8f * s, r.y, bw, r.height), "+", UiTheme.Button))
            {
                zoom = Mathf.Min(MaxZoom, zoom * 1.18f);
            }

            if (GUI.Button(new Rect(x + (bw + 8f * s) * 2f, r.y, bw, r.height), "◎", UiTheme.Button))
            {
                ToggleOverview();
            }

            GUI.Label(new Rect(r.x + tabW * 2f + 24f * s, r.y, r.width - tabW * 2f - 210f * s, r.height),
                $"배율 {Mathf.RoundToInt(zoom * 100f)}%", UiTheme.SmallMuted);
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
                "· 현판령 소문이 압록강 주변으로 번지는 중\n· 감찰단 보급선은 중원 사신로에 집중\n· 의주 객잔에서 새 영입 단서 확인 가능",
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
                    Vector2 oldDrawSize = drawSize;
                    Vector2 oldOrigin = new Vector2(viewport.width * 0.5f, viewport.height * 0.5f)
                        - new Vector2(oldDrawSize.x * centerUv.x, oldDrawSize.y * centerUv.y);
                    Vector2 uvAtMouse = new Vector2(
                        Mathf.InverseLerp(oldOrigin.x, oldOrigin.x + oldDrawSize.x, local.x),
                        Mathf.InverseLerp(oldOrigin.y, oldOrigin.y + oldDrawSize.y, local.y));
                    Vector2 newDrawSize = oldDrawSize * (zoom / oldZoom);
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
                float radius = (i == selectedNode ? 15f : 12f) * s;
                Rect hit = new Rect(pos.x - 62f * s, pos.y - 23f * s, 124f * s, 46f * s);

                UiTheme.DrawFill(new Rect(pos.x - radius, pos.y - radius, radius * 2f, radius * 2f), new Color(n.color.r, n.color.g, n.color.b, 0.30f));
                UiTheme.DrawSeal(new Rect(pos.x - radius * 0.72f, pos.y - radius * 0.72f, radius * 1.44f, radius * 1.44f), StatusGlyph(n.status), i == selectedNode ? -7f : -3f);

                GUIStyle label = new GUIStyle(UiTheme.Small)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = i == selectedNode ? FontStyle.Bold : FontStyle.Normal
                };
                label.normal.textColor = i == selectedNode ? UiTheme.Navy : UiTheme.Ink;
                GUI.Label(new Rect(pos.x - 72f * s, pos.y + 15f * s, 144f * s, 24f * s), n.name, label);

                if (GUI.Button(hit, GUIContent.none, GUIStyle.none))
                {
                    selectedNode = i;
                    centerUv = n.uv;
                    zoom = Mathf.Max(zoom, 1.35f);
                }
            }
        }

        private void DrawMapGrid(Rect viewport, float s)
        {
            Color c = new Color(UiTheme.Ink.r, UiTheme.Ink.g, UiTheme.Ink.b, 0.08f);
            for (int i = 1; i < 4; i++)
            {
                float x = viewport.width * i / 4f;
                UiTheme.DrawFill(new Rect(x, 0f, 1f * s, viewport.height), c);
            }

            for (int i = 1; i < 3; i++)
            {
                float y = viewport.height * i / 3f;
                UiTheme.DrawFill(new Rect(0f, y, viewport.width, 1f * s), c);
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

        private Texture2D ActiveMap
        {
            get
            {
                if (mapLayer == 0 && factionMap != null) return factionMap;
                if (terrainMap != null) return terrainMap;
                return factionMap;
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
            return "門";
        }

        private static string FactionHint(MapNode node)
        {
            if (node.status == "위험") return "중원 감찰단";
            if (node.status == "새 소문") return "객잔/상인";
            if (node.status == "거점") return "해동문";
            return "미확인";
        }

        private static string NextHint(MapNode node)
        {
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
