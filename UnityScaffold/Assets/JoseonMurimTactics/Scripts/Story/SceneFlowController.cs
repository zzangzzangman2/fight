namespace JoseonMurimTactics
{
    /// <summary>씬 이름 상수. EditorBuildSettings에 등록된 이름과 일치해야 한다.</summary>
    public static class SceneNames
    {
        public const string Boot = "Boot";
        public const string Title = "Title";
        public const string NewGameSetup = "NewGameSetup";
        public const string Prologue = "Prologue";
        public const string HubPyesadang = "Hub_Pyesadang";
        public const string MissionBoard = "MissionBoard";
        public const string BattlePrep = "BattlePrep";
        public const string Battle = "BattleTest"; // 기존 전투 씬 재사용
        public const string BattleResult = "BattleResult";
        public const string WorldMap = "WorldMap";
    }

    /// <summary>
    /// 씬 전환 통제 + 페이드. 실제 로딩과 페이드 코루틴은 GameRoot가 수행하고,
    /// 이 클래스는 의미 있는 흐름 메서드(설계 §7-3)를 제공한다.
    /// </summary>
    public sealed class SceneFlowController
    {
        private readonly GameRoot root;

        public SceneFlowController(GameRoot root)
        {
            this.root = root;
        }

        public void GoToTitle()
        {
            root.LoadSceneWithFade(SceneNames.Title);
        }

        /// <summary>타이틀의 "새 게임" → 새 GameSession을 만들고 설정 화면으로.</summary>
        public void StartNewGame()
        {
            root.BeginNewSession();
            root.LoadSceneWithFade(SceneNames.NewGameSetup);
        }

        public void GoToPrologue()
        {
            root.LoadSceneWithFade(SceneNames.Prologue);
        }

        public void GoToHub(string hubId = SceneNames.HubPyesadang)
        {
            string scene = string.IsNullOrEmpty(hubId) ? SceneNames.HubPyesadang : hubId;
            root.LoadSceneWithFade(scene);
        }

        public void GoToMissionBoard()
        {
            root.LoadSceneWithFade(SceneNames.MissionBoard);
        }

        public void GoToBattlePrep(string battleId)
        {
            BattleEntryAdapter.SetPendingBattle(battleId);
            root.LoadSceneWithFade(SceneNames.BattlePrep);
        }

        /// <summary>출격: 준비된 battleId로 전투 씬 진입.</summary>
        public void GoToBattle(string battleId)
        {
            BattleEntryAdapter.SetPendingBattle(battleId);
            BattleResultBridge.BeginBattle(battleId);
            root.LoadSceneWithFade(SceneNames.Battle);
        }

        public void GoToBattleResult(BattleResultData result)
        {
            BattleResultBridge.SetResult(result);
            root.LoadSceneWithFade(SceneNames.BattleResult);
        }

        public void GoToWorldMap()
        {
            root.LoadSceneWithFade(SceneNames.WorldMap);
        }
    }
}
