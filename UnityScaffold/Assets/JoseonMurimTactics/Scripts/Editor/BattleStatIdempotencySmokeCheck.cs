using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace JoseonMurimTactics.Editor
{
// GPT 피드백 v1.4 P0-7 대응: 전투 진입/맵전환/재시작 시 장비·성장 보너스가 유닛 정의에
// 누적되지 않는지(=매 전투 baseline에서 새로 복제되는지) 회귀 테스트로 고정한다.
public static class BattleStatIdempotencySmokeCheck
{
    [MenuItem("Joseon Murim Tactics/Progression/Smoke Check Battle Stat Idempotency")]
    public static void Run()
    {
        BattleTestUnitDefinition[] baseline =
        {
            new BattleTestUnitDefinition
            {
                id = "park_sungjun",
                displayName = "박성준",
                faction = Faction.Ally,
                maxHp = 30,
                maxInner = 4,
                attackBonus = 5,
                defense = 12,
                damageMin = 5,
                damageMax = 9,
                moveRange = 4
            },
            new BattleTestUnitDefinition
            {
                id = "enemy_dummy",
                displayName = "허수아비",
                faction = Faction.Enemy,
                maxHp = 30,
                attackBonus = 4,
                defense = 12,
                damageMin = 4,
                damageMax = 7
            }
        };

        MethodInfo builder = typeof(BattleTestController).GetMethod(
            "BuildBaekduSnowGateUnitDefinitions", BindingFlags.NonPublic | BindingFlags.Static);
        Require(builder != null, "BattleTestController.BuildBaekduSnowGateUnitDefinitions must exist");

        // 1) 빌더는 baseline을 복제해야 한다 — 결과를 변형해도 baseline은 그대로여야 한다.
        BattleTestUnitDefinition[] first = (BattleTestUnitDefinition[])builder.Invoke(null, new object[] { baseline });
        BattleTestUnitDefinition firstAlly = FindAlly(first);
        Require(firstAlly != null, "build result must contain an ally");
        int builtAttack = firstAlly.attackBonus;

        // ApplyEquipmentBonuses가 하는 일(복제본 직접 변형)을 흉내 낸다.
        firstAlly.attackBonus += 100;
        firstAlly.maxHp += 100;

        Require(baseline[0].attackBonus == 5 && baseline[0].maxHp == 30,
                "builder must clone — mutating the built unit must NOT change the baseline");

        // 2) 같은 baseline으로 다시 빌드하면 스탯은 누적되지 않고 원래 값이어야 한다.
        BattleTestUnitDefinition[] second = (BattleTestUnitDefinition[])builder.Invoke(null, new object[] { baseline });
        BattleTestUnitDefinition secondAlly = FindAlly(second);
        Require(secondAlly != null, "second build result must contain an ally");
        Require(secondAlly.attackBonus == builtAttack && secondAlly.maxHp == 30,
                "re-entering battle must not accumulate stats on the unit definition");

        Debug.Log("[BattleStatIdempotencySmokeCheck] Battle stat idempotency smoke check passed.");
    }

    private static BattleTestUnitDefinition FindAlly(BattleTestUnitDefinition[] units)
    {
        if (units == null)
        {
            return null;
        }

        foreach (BattleTestUnitDefinition unit in units)
        {
            if (unit != null && unit.faction == Faction.Ally)
            {
                return unit;
            }
        }

        return null;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
}
