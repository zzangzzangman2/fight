namespace JoseonMurimTactics
{
    /// <summary>
    /// 메인 퀘스트/보조 목표 상태 관리. 허브와 전투 결과를 잇는다.
    /// v0.8에서는 StoryFlag 위에 가벼운 플래그 트래킹으로 구현한다.
    /// </summary>
    public sealed class QuestManager
    {
        private readonly StoryFlagService flags;

        public QuestManager(StoryFlagService flags)
        {
            this.flags = flags;
        }

        public void CompleteQuest(string questId)
        {
            if (!string.IsNullOrEmpty(questId))
            {
                flags.SetFlag("QUEST:" + questId);
            }
        }

        public bool IsQuestComplete(string questId)
        {
            return !string.IsNullOrEmpty(questId) && flags.HasFlag("QUEST:" + questId);
        }

        public void CompleteObjective(string objectiveId)
        {
            if (!string.IsNullOrEmpty(objectiveId))
            {
                flags.SetFlag("OBJ:" + objectiveId);
            }
        }

        public bool IsObjectiveComplete(string objectiveId)
        {
            return !string.IsNullOrEmpty(objectiveId) && flags.HasFlag("OBJ:" + objectiveId);
        }

        /// <summary>전투 결과를 퀘스트 진행에 반영한다.</summary>
        public void ResolveBattle(BattleResultData result, BattleDefinition def)
        {
            if (result == null || def == null)
            {
                return;
            }

            if (result.completedObjectives != null)
            {
                foreach (string objId in result.completedObjectives)
                {
                    CompleteObjective(objId);
                }
            }

            if (result.Won)
            {
                CompleteQuest(def.questId);
            }
        }
    }
}
