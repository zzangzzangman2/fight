using UnityEngine;

namespace JoseonMurimTactics
{
    /// <summary>
    /// [0] Boot — 로고/간단 로딩, 저장 데이터 확인 후 Title로. GameRoot를 생성한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BootController : MonoBehaviour
    {
        public float holdSeconds = 1.0f;

        private GameRoot root;
        private float timer;
        private bool leaving;

        private void Awake()
        {
            root = GameRoot.EnsureExists();
        }

        private void Update()
        {
            timer += Time.unscaledDeltaTime;
            bool skip = Input.anyKeyDown || Input.GetMouseButtonDown(0);
            if (!leaving && (timer >= holdSeconds || skip))
            {
                leaving = true;
                root.Flow.GoToTitle();
            }
        }

        private void OnGUI()
        {
            UiTheme.Begin(true);
            UiTheme.DrawMountains();

            float w = Screen.width;
            float h = Screen.height;
            float s = UiTheme.Scale;

            UiTheme.LabelShadow(new Rect(0f, h * 0.34f, w, 96f * s), "海東", UiTheme.Logo);
            GUI.Label(new Rect(0f, h * 0.49f, w, 50f * s), "조선 무협 SRPG", UiTheme.Title);
            UiTheme.DrawDivider(w * 0.5f, h * 0.575f, 320f * s);

            // 인장
            float seal = 60f * s;
            UiTheme.DrawSeal(new Rect(w * 0.5f - seal * 0.5f, h * 0.61f, seal, seal), "印");

            GUI.Label(new Rect(0f, h * 0.75f, w, 30f * s), "불러오는 중...", UiTheme.BodyCenter);
        }
    }
}
