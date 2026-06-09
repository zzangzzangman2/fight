using System.Collections.Generic;
using UnityEngine;

namespace JoseonMurimTactics
{
    /// <summary>
    /// 전투 진입 정의(설계 §7-13). 어떤 씬으로 들어가고 승패/목표가 무엇인지. v0.8 런타임은
    /// BattleCatalog(코드)를 쓰며, 이 에셋은 이후 데이터 저작/교체용이다.
    /// </summary>
    [CreateAssetMenu(fileName = "BattleEntry", menuName = "JoseonMurim/Battle Entry Data")]
    public sealed class BattleEntryData : ScriptableObject
    {
        public string battleId;
        public string sceneName = "BattleTest";
        public string displayTitle;
        public string location;
        public string victoryCondition;
        public List<string> defeatConditions = new List<string>();
        public List<string> objectives = new List<string>();
    }
}
