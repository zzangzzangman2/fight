using System;
using UnityEngine;

namespace JoseonMurimTactics
{
[Serializable]
public enum RollMode
{
    None = 0,
    Normal = 1,
    Advantage = 2,
    Disadvantage = 3,
    AutoHit = 4,
    AutoMiss = 5
}

[Serializable]
public sealed class BattleForecastData
{
    public bool canUseSkill;
    public string failureReason;
    public int distance;
    public RollMode rollMode;
    public int attackBonus;
    public int targetDefense;
    public int requiredRoll;
    public int hitChancePercent;
    public int minDamage;
    public int maxDamage;
    public int criticalMinDamage;
    public int criticalMaxDamage;

    public bool willCounter;
    public SkillData counterSkill;
    public int counterDistance;
    public int counterHitChancePercent;
    public int counterMinDamage;
    public int counterMaxDamage;

    public bool willPushOnHit;
    public int pushDistance;
    public Vector2Int pushDestination;
    public bool pushBlocked;
    public bool pushCausesFall;
}
}
