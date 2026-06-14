using System.Collections.Generic;
using UnityEngine;

namespace JoseonMurimTactics
{
// BattleTestController의 적 AI 타겟 선택/치유 판단 헬퍼 (GPT 피드백 P0-5: 점진적 partial 분리).
// 동작 변경 없음 — 기존 메서드를 그대로 옮겼다. RunEnemyPhase 본체는 BattleTestController.cs에 남아 이들을 호출한다.
public sealed partial class BattleTestController
{
    // 적 AI 타겟 선택: 단순 최근접 대신 처치각 > 도달성 > 저HP > 근접 순으로 점수화한다.
    private BattleTestUnit ChooseEnemyTarget(BattleTestUnit unit)
    {
        BattleTestUnit best = null;
        int bestScore = int.MinValue;

        foreach (BattleTestUnit other in units)
        {
            if (other == null || other.defeated || other.definition.faction == unit.definition.faction)
            {
                continue;
            }

            int distance = GridDistance(unit.cell, other.cell);
            int score = -distance * 4;

            bool reachable = CanReachToAttackThisTurn(unit, other);
            if (reachable)
            {
                score += 40;
            }

            // 부상한 대상 우선(마무리 유도).
            score += Mathf.Clamp(60 - other.hp, 0, 60);

            // 이번 턴 공격으로 쓰러뜨릴 수 있으면 최우선.
            if (reachable && EstimateAttackDamage(unit, other) >= other.hp)
            {
                score += 120;
            }

            // 반격 봉쇄(파훼)된 대상은 약간 선호.
            if (other.marked)
            {
                score += 6;
            }

            if (score > bestScore)
            {
                bestScore = score;
                best = other;
            }
        }

        // 점수상 후보가 없으면(이론상) 최근접으로 폴백.
        return best != null ? best : FindNearestEnemy(unit);
    }

    // 힐러 적이 지금(이동 없이) 치유할 가장 부상이 큰 아군. 없으면 null.
    private BattleTestUnit FindEnemyHealTarget(BattleTestUnit healer)
    {
        if (healer == null || healer.definition.specialEffect != BattleSpecialEffect.Heal || !CanUseSpecial(healer))
        {
            return null;
        }

        int range = EffectiveSpecialRange(healer);
        BattleTestUnit best = null;
        int bestMissing = 0;
        foreach (BattleTestUnit ally in units)
        {
            if (ally == null || ally.defeated || ally.definition.faction != healer.definition.faction)
            {
                continue;
            }

            int missing = ally.definition.maxHp - ally.hp;
            if (missing <= 0 || GridDistance(healer.cell, ally.cell) > range)
            {
                continue;
            }

            if (missing > bestMissing)
            {
                bestMissing = missing;
                best = ally;
            }
        }

        return best;
    }

    // 이번 턴(현재 위치 또는 한 번 이동)에 target을 기본 공격으로 때릴 수 있는가.
    private bool CanReachToAttackThisTurn(BattleTestUnit unit, BattleTestUnit target)
    {
        if (CanHitFrom(unit, unit.cell, target))
        {
            return true;
        }

        if (unit.moved)
        {
            return false;
        }

        foreach (Vector2Int cell in GetReachableCells(unit).Keys)
        {
            if (cell == unit.cell || UnitAt(cell) != null)
            {
                continue;
            }

            if (CanHitFrom(unit, cell, target))
            {
                return true;
            }
        }

        return false;
    }

    private bool CanHitFrom(BattleTestUnit unit, Vector2Int from, BattleTestUnit target)
    {
        BattleTestTile fromTile = TileAt(from);
        int range = EffectiveAttackRange(unit, fromTile);
        return BattleTargetingService.CanAttackFrom(from, target.cell, range, TileAt, IsInside).canTarget;
    }

    // 1회 공격으로 줄 수 있는 대략적 최대 피해(치명·고저는 제외, 무공·방어는 반영) — 처치각 판단용.
    private int EstimateAttackDamage(BattleTestUnit attacker, BattleTestUnit target)
    {
        int damage = attacker.definition.damageMax;
        if (CanUseSpecial(attacker) && IsHostileAttackSpecial(attacker.definition.specialEffect))
        {
            damage += attacker.definition.specialPower;
        }

        if (target.guarded)
        {
            damage = Mathf.Max(1, Mathf.CeilToInt(damage * 0.55f));
        }

        return damage;
    }
}
}
