using UnityEngine;

namespace JoseonMurimTactics
{
    /// <summary>
    /// [8] WorldMap placeholder — 다음 장 개방 예고. v0.8에서는 제1장 목표만 안내하고 거점으로 돌려보낸다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorldMapController : MonoBehaviour
    {
        private GameRoot root;

        private void Awake()
        {
            root = GameRoot.EnsureExists();
        }

        private void OnGUI()
        {
            UiTheme.Begin(true);
            UiTheme.DrawMountains();
            float w = Screen.width;
            float h = Screen.height;
            float s = UiTheme.Scale;

            UiTheme.LabelShadow(new Rect(0f, 58f * s, w, 58f * s), "제1장 · 의주 객잔", UiTheme.Title);
            GUI.Label(new Rect(0f, 122f * s, w, 30f * s), "― 다음 장 준비 중 ―", UiTheme.BodyCenter);
            UiTheme.DrawDivider(w * 0.5f, 162f * s, 360f * s);

            float pw = Mathf.Min(720f * s, w - 120f * s);
            Rect panel = new Rect(w * 0.5f - pw * 0.5f, 180f * s, pw, 260f * s);
            UiTheme.DrawPanel(panel);
            GUI.Label(new Rect(panel.x + 28f * s, panel.y + 22f * s, panel.width - 56f * s, 34f * s), "다음 목표", UiTheme.Heading);
            GUI.Label(new Rect(panel.x + 28f * s, panel.y + 68f * s, panel.width - 56f * s, panel.height - 90f * s),
                "· 의주 객잔으로 이동한다.\n· 암기의 고수 한비연이 처음 등장한다.\n· 흩어진 조선 문파의 회합을 준비한다.\n\n" +
                "(제1장 객잔 맵 전투와 한비연 영입은 v1.0에서 구현 예정입니다.)",
                UiTheme.Body);

            float bw = 280f * s;
            if (GUI.Button(new Rect(w * 0.5f - bw * 0.5f, h - 110f * s, bw, 58f * s), "거점으로 돌아가기", UiTheme.ButtonPrimary))
            {
                root.Flow.GoToHub(SceneNames.HubPyesadang);
            }
        }
    }
}
