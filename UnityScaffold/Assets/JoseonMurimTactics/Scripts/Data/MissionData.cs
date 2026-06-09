using System.Collections.Generic;
using UnityEngine;

namespace JoseonMurimTactics
{
/// <summary>
/// 임무(출정 단위) 정보 ScriptableObject(설계 v0.9 §7-7). 전략 화면(MissionBoard)에서 보여주는
/// 메타데이터이며, battleId로 실제 전투 정의(BattleCatalog)와 연결된다. v0.8 런타임은 MissionCatalog(코드)를
/// 쓰고, 이 에셋은 이후 데이터 저작/교체용이다.
/// </summary>
[CreateAssetMenu(fileName = "Mission", menuName = "JoseonMurim/Mission Data")]
public sealed class MissionData : ScriptableObject
{
    public string missionId;
    public string title;
    public string location;
    public string battleId;
    public int recommendedLevel = 1;
    public string enemyFaction;
    public string difficulty;
    [TextArea]
    public string summary;
    public string victoryConditionShort;
    public List<string> rewardPreview = new List<string>();
    public string requiredFlag;
    public string completeFlag;
    [TextArea]
    public string dangerNotes;
    public bool isStory = true;
}
}
