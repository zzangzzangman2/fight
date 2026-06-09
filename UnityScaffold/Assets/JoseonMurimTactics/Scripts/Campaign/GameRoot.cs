using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JoseonMurimTactics
{
    /// <summary>
    /// 씬을 넘어 살아있는 게임 루트. GameSession과 모든 스토리 서비스, 씬 전환을 보관한다.
    /// Boot 씬에서 생성되며, 다른 씬을 에디터에서 직접 Play해도 EnsureExists()로 자동 생성되어
    /// 단독 테스트가 가능하다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameRoot : MonoBehaviour
    {
        public static GameRoot Instance { get; private set; }

        public GameSession Session { get; private set; }
        public SceneFlowController Flow { get; private set; }
        public StoryFlagService Flags { get; private set; }
        public CompanionApprovalService Approval { get; private set; }
        public FactionReputationService Reputation { get; private set; }
        public SaveManager Save { get; private set; }
        public IAINarrationService Narration { get; private set; }
        public QuestManager Quests { get; private set; }

        public bool IsFading { get; private set; }

        private Texture2D fadeTex;
        private float fadeAlpha;

        /// <summary>씬 컨트롤러가 Awake에서 호출. 루트가 없으면 만들어 단독 실행을 보장한다.</summary>
        public static GameRoot EnsureExists()
        {
            if (Instance != null)
            {
                return Instance;
            }

            GameObject go = new GameObject("GameRoot");
            return go.AddComponent<GameRoot>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            fadeTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            fadeTex.hideFlags = HideFlags.HideAndDontSave;
            fadeTex.SetPixel(0, 0, new Color(0.102f, 0.090f, 0.078f, 1f)); // 따뜻한 먹빛
            fadeTex.Apply();

            if (Session == null)
            {
                BindSession(new GameSession());
            }
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void Update()
        {
            if (Session != null)
            {
                Session.playTimeSeconds += Time.unscaledDeltaTime;
            }
        }

        /// <summary>
        /// 스토리 흐름으로 전투 씬에 들어오면, 기존 BattleTest 씬/컨트롤러를 건드리지 않고
        /// 결과 복귀용 오버레이를 런타임에 주입한다(설계 §5: BattleEntryAdapter 방식).
        /// 전투 씬을 에디터에서 직접 열면 PendingBattleId가 없어 주입되지 않는다.
        /// </summary>
        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != SceneNames.Battle)
            {
                return;
            }

            if (string.IsNullOrEmpty(BattleResultBridge.CurrentBattleId))
            {
                return;
            }

            // 타입명으로 주입해 스토리 코어가 전투 씬 전용 오버레이에 컴파일 의존하지 않게 한다.
            System.Type overlayType = System.Type.GetType("JoseonMurimTactics.BattleReturnOverlay, Assembly-CSharp");
            if (overlayType != null && FindAnyObjectByType(overlayType) == null)
            {
                new GameObject("BattleReturnOverlay").AddComponent(overlayType);
            }
        }

        /// <summary>새 게임 시작 시 깨끗한 세션으로 교체.</summary>
        public void BeginNewSession()
        {
            BindSession(new GameSession());
        }

        public void LoadExistingSession(GameSession session)
        {
            BindSession(session ?? new GameSession());
        }

        private void BindSession(GameSession session)
        {
            Session = session;
            Flags = new StoryFlagService(Session);
            Approval = new CompanionApprovalService(Session);
            Reputation = new FactionReputationService(Session);
            Quests = new QuestManager(Flags);
            Save = Save ?? new SaveManager();
            Narration = Narration ?? new MockAINarrationService();
            Flow = Flow ?? new SceneFlowController(this);
        }

        // ----- 씬 전환 + 페이드 -----

        public void LoadSceneWithFade(string sceneName)
        {
            if (!gameObject.activeInHierarchy)
            {
                SceneManager.LoadScene(sceneName);
                return;
            }

            StopAllCoroutines();
            StartCoroutine(FadeAndLoad(sceneName));
        }

        private IEnumerator FadeAndLoad(string sceneName)
        {
            IsFading = true;
            yield return Fade(0f, 1f, 0.22f);

            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
            if (op != null)
            {
                while (!op.isDone)
                {
                    yield return null;
                }
            }

            yield return Fade(1f, 0f, 0.26f);
            IsFading = false;
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            float elapsed = 0f;
            fadeAlpha = from;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                fadeAlpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            fadeAlpha = to;
        }

        private void OnGUI()
        {
            if (fadeAlpha <= 0.001f || fadeTex == null)
            {
                return;
            }

            GUI.depth = -1000; // 최상단
            Color prev = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, fadeAlpha);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), fadeTex, ScaleMode.StretchToFill);
            GUI.color = prev;
        }
    }
}
