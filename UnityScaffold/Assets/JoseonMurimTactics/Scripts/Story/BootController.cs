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

            float w = Screen.width;
            float h = Screen.height;
            float s = UiTheme.Scale;

            GUI.Label(new Rect(0f, h * 0.36f, w, 90f * s), "海東", UiTheme.Logo);
            GUI.Label(new Rect(0f, h * 0.50f, w, 50f * s), "조선 무협 SRPG", UiTheme.Title);

            // 인장
            float seal = 64f * s;
            UiTheme.DrawSeal(new Rect(w * 0.5f - seal * 0.5f, h * 0.60f, seal, seal), "印");

            GUI.Label(new Rect(0f, h * 0.74f, w, 30f * s), "불러오는 중...", UiTheme.BodyCenter);
        }
    }
}
