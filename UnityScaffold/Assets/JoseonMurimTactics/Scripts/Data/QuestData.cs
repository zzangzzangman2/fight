using System.Collections.Generic;
using UnityEngine;

namespace JoseonMurimTactics
{
/// <summary>퀘스트 정보(설계 §7-12, §8).</summary>
[CreateAssetMenu(fileName = "Quest", menuName = "JoseonMurim/Quest Data")]
public sealed class QuestData : ScriptableObject
{
    public string questId;
    public string title;
    [TextArea]
    public string summary;
    public List<string> objectives = new List<string>();
    public List<string> rewards = new List<string>();
}
}
