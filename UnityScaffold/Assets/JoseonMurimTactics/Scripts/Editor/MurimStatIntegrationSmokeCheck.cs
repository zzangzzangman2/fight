using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace JoseonMurimTactics.Editor
{
// 실제 플레이 전투(BattleTestController)의 성장 반영만 검증한다.
// 과거 이니셔티브 엔진(TurnManager/SkillResolver/CombatantRuntime)을 검증하던 부분은
// 해당 엔진이 게임에서 사용되지 않아 2026-06 정리 때 제거됨.
public static class MurimStatIntegrationSmokeCheck
{
    [MenuItem("Joseon Murim Tactics/Progression/Smoke Check Murim Stat Combat Integration")]
    public static void Run()
    {
        VerifyBattleTestDefinitionGrowth();
        Debug.Log("[MurimStatIntegrationSmokeCheck] Murim stat combat integration smoke check passed.");
    }

    private static void VerifyBattleTestDefinitionGrowth()
    {
        Type controllerType = typeof(BattleTestController);
        MethodInfo method = controllerType.GetMethod("ApplyGrowthBonuses",
                                                     BindingFlags.NonPublic | BindingFlags.Static);
        Require(method != null, "BattleTestController.ApplyGrowthBonuses must exist");

        BattleTestUnitDefinition definition = new BattleTestUnitDefinition
        {
            maxHp = 30,
            maxInner = 4,
            attackBonus = 5,
            defense = 12,
            damageMin = 3,
            damageMax = 6,
            specialPower = 4,
            specialAttackBonus = 1,
            initiative = 10,
            agility = 10,
            moveRange = 4
        };
        CharacterProgressState growth = new CharacterProgressState
        {
            hpBonus = 5,
            innerBonus = 2,
            statBonuses = new SixStats
            {
                strength = 8,
                agility = 8,
                innerPower = 8,
                spirit = 8,
                insight = 8
            }
        };

        method.Invoke(null, new object[] { definition, growth });
        Require(definition.maxHp > 30, "BattleTest growth must change maxHp");
        Require(definition.maxInner > 4, "BattleTest growth must change maxInner");
        Require(definition.attackBonus > 5, "BattleTest growth must change attackBonus");
        Require(definition.defense > 12, "BattleTest growth must change defense");
        Require(definition.damageMin > 3 && definition.damageMax > 6, "BattleTest growth must change damage");
        Require(definition.specialPower > 4, "BattleTest growth must change specialPower");
        Require(definition.specialAttackBonus > 1, "BattleTest growth must change specialAttackBonus");
        Require(definition.initiative > 10, "BattleTest growth must change initiative");
        Require(definition.moveRange > 4, "BattleTest growth must change moveRange");
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
