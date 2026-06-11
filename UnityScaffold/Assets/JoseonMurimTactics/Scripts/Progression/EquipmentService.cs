using System.Collections.Generic;
using UnityEngine;

namespace JoseonMurimTactics
{
/// <summary>BuildBonus 결과 — 전투 유닛 수치에 더해지는 장비 보정.</summary>
public struct EquipmentBonus
{
    public int atk;   // 피해(damageMin/Max)
    public int acc;   // 명중(attackBonus)
    public int hp;    // 최대 체력
    public int guard; // 방어(defense)
    public int inner; // 내공(maxInner)
    public int move;  // 이동(moveRange)

    public bool IsEmpty => atk == 0 && acc == 0 && hp == 0 && guard == 0 && inner == 0 && move == 0;

    public override string ToString()
    {
        List<string> parts = new List<string>();
        if (atk != 0) parts.Add($"공+{atk}");
        if (acc != 0) parts.Add($"명+{acc}");
        if (hp != 0) parts.Add($"체+{hp}");
        if (guard != 0) parts.Add($"방+{guard}");
        if (inner != 0) parts.Add($"내+{inner}");
        if (move != 0) parts.Add($"이+{move}");
        return string.Join(" ", parts);
    }
}

/// <summary>
/// 캐릭터별 장비 장착/해제/강화(허브 경제 MVP).
/// 장착 상태: stringVars["equip:<charId>:<slot>"] = itemId.
/// 강화 레벨: intVars["equip:level:<itemId>"] — 같은 itemId 장비끼리 공유(설계 §C).
/// 같은 itemId를 동시에 장착할 수 있는 캐릭터 수는 보유 수량 이하로 제한한다.
/// </summary>
public sealed class EquipmentService
{
    private readonly GameSession session;
    private readonly InventoryService inventory;
    private readonly StoryFlagService flags;

    public EquipmentService(GameSession session)
    {
        this.session = session;
        inventory = new InventoryService(session);
        flags = new StoryFlagService(session);
    }

    public static string EquipKey(string characterId, EquipmentSlot slot)
    {
        return "equip:" + CharacterGrowthCatalog.NormalizeCharacterId(characterId) + ":" +
               slot.ToString().ToLowerInvariant();
    }

    public static string LevelKey(string itemId)
    {
        return "equip:level:" + InventoryService.NormalizeItemId(itemId);
    }

    public string GetEquipped(string characterId, EquipmentSlot slot)
    {
        if (session == null)
        {
            return null;
        }

        return session.stringVars.TryGetValue(EquipKey(characterId, slot), out string itemId) &&
                       !string.IsNullOrEmpty(itemId)
                   ? itemId
                   : null;
    }

    /// <summary>해당 itemId를 장착 중인 캐릭터 수(중복 장착 제한 검사용).</summary>
    public int EquippedCount(string itemId)
    {
        if (session == null)
        {
            return 0;
        }

        string key = InventoryService.NormalizeItemId(itemId);
        int count = 0;
        foreach (KeyValuePair<string, string> pair in session.stringVars)
        {
            if (pair.Key.StartsWith("equip:") && !pair.Key.StartsWith("equip:level:") && pair.Value == key)
            {
                count++;
            }
        }

        return count;
    }

    public bool CanEquip(string characterId, string itemId, out string reason)
    {
        reason = string.Empty;
        EquipmentInfo info = EquipmentCatalog.Get(itemId);
        if (info == null)
        {
            reason = "장비할 수 없는 물건이다.";
            return false;
        }

        string charId = CharacterGrowthCatalog.NormalizeCharacterId(characterId);
        if (info.IsExclusive && info.requiredCharacterId != charId)
        {
            reason = $"{info.displayName}은(는) {CharacterGrowthCatalog.DisplayName(info.requiredCharacterId)} 전용이다.";
            return false;
        }

        int owned = inventory.GetCount(info.id);
        if (owned <= 0)
        {
            reason = "보유하고 있지 않다. 장터에서 구할 수 있다.";
            return false;
        }

        // 같은 슬롯에 이미 같은 장비면 통과(아무 일도 안 함), 아니면 보유 수량 검사.
        string current = GetEquipped(charId, info.slot);
        if (current != info.id && EquippedCount(info.id) >= owned)
        {
            reason = "보유 수량만큼 이미 다른 동료가 쓰고 있다.";
            return false;
        }

        return true;
    }

    /// <summary>장착. 같은 슬롯의 기존 장비는 자동 교체된다.</summary>
    public bool Equip(string characterId, string itemId, out string reason)
    {
        if (!CanEquip(characterId, itemId, out reason))
        {
            return false;
        }

        EquipmentInfo info = EquipmentCatalog.Get(itemId);
        session.stringVars[EquipKey(characterId, info.slot)] = info.id;
        return true;
    }

    public bool Unequip(string characterId, EquipmentSlot slot)
    {
        if (session == null)
        {
            return false;
        }

        return session.stringVars.Remove(EquipKey(characterId, slot));
    }

    public int GetUpgradeLevel(string itemId)
    {
        if (session == null)
        {
            return 0;
        }

        return session.intVars.TryGetValue(LevelKey(itemId), out int level) ? Mathf.Max(0, level) : 0;
    }

    public bool CanUpgrade(string itemId, out string reason)
    {
        reason = string.Empty;
        EquipmentInfo info = EquipmentCatalog.Get(itemId);
        if (info == null)
        {
            reason = "강화할 수 없는 물건이다.";
            return false;
        }

        if (inventory.GetCount(info.id) <= 0)
        {
            reason = "보유하고 있지 않다.";
            return false;
        }

        int level = GetUpgradeLevel(info.id);
        if (level >= info.maxUpgradeLevel)
        {
            reason = "이미 최대 강화 단계다.";
            return false;
        }

        UpgradeCost cost = EquipmentCatalog.CostFor(level + 1);
        if (GetSilver() < cost.silver)
        {
            reason = $"은냥이 부족하다. (필요 {cost.silver})";
            return false;
        }

        if (cost.materialCount > 0)
        {
            string materialId = EquipmentCatalog.MaterialFor(info.slot);
            if (inventory.GetCount(materialId) < cost.materialCount)
            {
                reason = $"재료가 부족하다. ({InventoryService.Label(materialId)} {cost.materialCount}개 필요)";
                return false;
            }
        }

        return true;
    }

    /// <summary>강화 실행. 성공 시 은냥/재료 차감 후 도달 레벨을 반환, 실패 시 -1.</summary>
    public int Upgrade(string itemId, out string reason)
    {
        if (!CanUpgrade(itemId, out reason))
        {
            return -1;
        }

        EquipmentInfo info = EquipmentCatalog.Get(itemId);
        int nextLevel = GetUpgradeLevel(info.id) + 1;
        UpgradeCost cost = EquipmentCatalog.CostFor(nextLevel);

        AddSilver(-cost.silver);
        if (cost.materialCount > 0)
        {
            inventory.Consume(EquipmentCatalog.MaterialFor(info.slot), cost.materialCount);
        }

        session.intVars[LevelKey(info.id)] = nextLevel;
        return nextLevel;
    }

    /// <summary>캐릭터의 세 슬롯 장비 + 강화 보정 합산. 전투 유닛 생성 시 적용한다.</summary>
    public EquipmentBonus BuildBonus(string characterId)
    {
        EquipmentBonus bonus = new EquipmentBonus();
        if (session == null)
        {
            return bonus;
        }

        foreach (EquipmentSlot slot in SlotOrder)
        {
            EquipmentInfo info = EquipmentCatalog.Get(GetEquipped(characterId, slot));
            if (info == null)
            {
                continue;
            }

            int atk = info.atk, acc = info.acc, hp = info.hp, guard = info.guard, inner = info.inner;
            EquipmentCatalog.UpgradeBonus(info, GetUpgradeLevel(info.id), ref atk, ref acc, ref hp, ref guard,
                                          ref inner);
            bonus.atk += atk;
            bonus.acc += acc;
            bonus.hp += hp;
            bonus.guard += guard;
            bonus.inner += inner;
            bonus.move += info.move;
        }

        return bonus;
    }

    /// <summary>"백야검+1 · 누빈 도복 · 청옥 부적" 식 요약. 아무것도 없으면 빈 문자열.</summary>
    public string Summary(string characterId)
    {
        List<string> parts = new List<string>();
        foreach (EquipmentSlot slot in SlotOrder)
        {
            EquipmentInfo info = EquipmentCatalog.Get(GetEquipped(characterId, slot));
            if (info == null)
            {
                continue;
            }

            int level = GetUpgradeLevel(info.id);
            parts.Add(level > 0 ? $"{info.displayName}+{level}" : info.displayName);
        }

        return string.Join(" · ", parts);
    }

    public static readonly EquipmentSlot[] SlotOrder = { EquipmentSlot.Weapon, EquipmentSlot.Armor,
                                                         EquipmentSlot.Accessory };

    private int GetSilver()
    {
        return session == null ? 0 : flags.GetInt("silver");
    }

    private void AddSilver(int delta)
    {
        if (session != null)
        {
            flags.AddInt("silver", delta);
        }
    }
}
}
