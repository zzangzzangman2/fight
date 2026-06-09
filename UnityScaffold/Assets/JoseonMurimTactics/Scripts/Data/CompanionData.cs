using System;
using System.Collections.Generic;
using UnityEngine;

namespace JoseonMurimTactics
{
[Serializable]
public struct ApprovalRule
{
    public string eventId; // 어떤 사건/선택에 반응하는가
    public int delta;      // 승인도 변화
    public string note;
}

/// <summary>
/// 동료 기본 설정 ScriptableObject(설계 §7-6). v0.8 런타임은 CompanionCatalog(코드)를 쓰며,
/// 이 에셋은 이후 데이터 저작/교체용으로 둔다.
/// </summary>
[CreateAssetMenu(fileName = "Companion", menuName = "JoseonMurim/Companion Data")]
public sealed class CompanionData : ScriptableObject
{
    public string companionId;
    public string displayName;
    public string title;
    public string role;
    public string region;
    public string sectName;
    public int age;
    public string mbti;
    public string element;
    public string weapon;
    public string speechTone;
    [TextArea]
    public string profile;
    public Sprite portraitPlaceholder;
    public List<string> startingSkillIds = new List<string>();
    public List<ApprovalRule> approvalRules = new List<ApprovalRule>();
    public string personalQuestId;
}
}
