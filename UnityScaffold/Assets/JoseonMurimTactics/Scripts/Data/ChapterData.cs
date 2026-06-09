using System.Collections.Generic;
using UnityEngine;

namespace JoseonMurimTactics
{
    /// <summary>한 장의 정보(설계 §7-5, §8).</summary>
    [CreateAssetMenu(fileName = "Chapter", menuName = "JoseonMurim/Chapter Data")]
    public sealed class ChapterData : ScriptableObject
    {
        public string chapterId;
        public string chapterTitle;
        public string openingSceneId;
        public string hubSceneName;
        public string mainBattleId;
        public string nextChapterId;
        public List<string> unlockQuestIds = new List<string>();
        public List<string> availableCompanionIds = new List<string>();
    }
}
