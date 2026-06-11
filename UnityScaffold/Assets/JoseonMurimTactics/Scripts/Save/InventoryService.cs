using System;
using System.Collections.Generic;

namespace JoseonMurimTactics
{
[Serializable]
public struct InventoryStack
{
    public string itemId;
    public int count;
    public InventoryItemType type;

    public InventoryStack(string itemId, int count, InventoryItemType type)
    {
        this.itemId = itemId;
        this.count = count;
        this.type = type;
    }
}

public sealed class InventoryService
{
    private readonly GameSession session;

    public InventoryService(GameSession session)
    {
        this.session = session;
    }

    public int GetCount(string itemId)
    {
        if (session == null || string.IsNullOrEmpty(itemId))
        {
            return 0;
        }

        NormalizeStoredKeys();
        return session.inventory.TryGetValue(NormalizeItemId(itemId), out int count) ? count : 0;
    }

    public void SetCount(string itemId, int count)
    {
        if (session == null || string.IsNullOrEmpty(itemId))
        {
            return;
        }

        NormalizeStoredKeys();
        string key = NormalizeItemId(itemId);
        if (count <= 0)
        {
            session.inventory.Remove(key);
            return;
        }

        session.inventory[key] = count;
    }

    public int AddItem(string itemId, int count = 1)
    {
        if (count <= 0 || string.IsNullOrEmpty(itemId))
        {
            return GetCount(itemId);
        }

        NormalizeStoredKeys();
        string key = NormalizeItemId(itemId);
        int next = GetCount(key) + count;
        session.inventory[key] = next;
        return next;
    }

    public bool Consume(string itemId, int count = 1)
    {
        if (count <= 0)
        {
            return true;
        }

        int current = GetCount(itemId);
        if (current < count)
        {
            return false;
        }

        SetCount(itemId, current - count);
        return true;
    }

    public bool Purchase(StoryFlagService flags, string itemId, int count, int price)
    {
        if (flags == null || string.IsNullOrEmpty(itemId) || count <= 0)
        {
            return false;
        }

        if (flags.GetInt("silver") < price)
        {
            return false;
        }

        flags.AddInt("silver", -price);
        AddItem(itemId, count);
        return true;
    }

    public int AddItemFromDisplayName(string displayName, int count = 1)
    {
        return AddItem(NormalizeItemId(displayName), count);
    }

    public List<InventoryStack> AllStacks()
    {
        List<InventoryStack> stacks = new List<InventoryStack>();
        if (session == null)
        {
            return stacks;
        }

        NormalizeStoredKeys();
        foreach (KeyValuePair<string, int> pair in session.inventory)
        {
            if (pair.Value > 0)
            {
                stacks.Add(new InventoryStack(pair.Key, pair.Value, GuessType(pair.Key)));
            }
        }

        stacks.Sort((a, b) => string.CompareOrdinal(a.itemId, b.itemId));
        return stacks;
    }

    public static string NormalizeItemId(string item)
    {
        if (string.IsNullOrEmpty(item))
        {
            return string.Empty;
        }

        string trimmed = item.Trim();
        switch (trimmed)
        {
        case "약재 꾸러미":
        case "약초 꾸러미":
        case "약재_꾸러미":
        case "약초_꾸러미":
        case "supply:medicine":
            return "medicine_bundle";
        case "내공단":
            return "inner_power_pill";
        case "투척 비수 묶음":
        case "투척_비수_묶음":
            return "throwing_dagger_bundle";
        case "목재 묶음":
        case "목재_묶음":
        case "supply:wood":
        case "material:wood_bundle":
            return "wood_bundle";
        case "철광석":
        case "material:iron_ore":
            return "iron_ore";
        case "질긴 천":
        case "질긴_천":
        case "material:fine_cloth":
            return "fine_cloth";
        case "옥 조각":
        case "옥_조각":
        case "material:jade_shard":
            return "jade_shard";
        case "산양 젖":
        case "산양_젖":
        case "material:goat_milk":
            return "goat_milk";
        case "질긴 가죽":
        case "질긴_가죽":
        case "supply:leather":
        case "material:tough_leather":
            return "tough_leather";
        case "호피 조각":
        case "호피_조각":
        case "material:tiger_pelt":
            return "tiger_pelt";
        case "응급 약재":
        case "응급_약재":
        case "material:emergency_medicine":
            return "emergency_medicine";
        case "희귀 약초":
        case "희귀_약초":
        case "supply:herb":
        case "material:rare_herb":
            return "rare_herb";
        case "표범 무늬 가죽":
        case "표범_무늬_가죽":
        case "material:leopard_pelt":
            return "leopard_pelt";
        case "마을 감사패":
        case "마을_감사패":
            return "village_thanks_plaque";
        case "사당 현판 보존 명성":
        case "사당_현판_보존_명성":
            return "temple_signboard_renown";
        case "무공 단서: 돌계단 방진":
        case "무공_단서__돌계단_방진":
            return "skill_clue_stone_step_formation";
        case "무공 단서: 새벽일섬":
        case "무공_단서__새벽일섬":
            return "skill_clue_dawn_flash";
        default:
            return trimmed.ToLowerInvariant().Replace(" ", "_").Replace(":", "_");
        }
    }

    public static string Label(string itemId)
    {
        string key = NormalizeItemId(itemId);
        GiftInfo gift = GiftCatalog.Get(key);
        if (gift != null)
        {
            return gift.displayName;
        }

        EquipmentInfo equip = EquipmentCatalog.Get(key);
        if (equip != null)
        {
            return equip.displayName;
        }

        MaterialCatalog.MaterialInfo material = MaterialCatalog.Get(key);
        if (material != null)
        {
            return material.displayName;
        }

        switch (key)
        {
        case "medicine_bundle":
            return "약재 꾸러미";
        case "inner_power_pill":
            return "내공단";
        case "throwing_dagger_bundle":
            return "투척 비수 묶음";
        case "village_thanks_plaque":
            return "마을 감사패";
        case "temple_signboard_renown":
            return "사당 현판 보존 명성";
        case "skill_clue_stone_step_formation":
            return "무공 단서: 돌계단 방진";
        case "skill_clue_dawn_flash":
            return "무공 단서: 새벽일섬";
        default:
            return itemId;
        }
    }

    /// <summary>아이템 id의 분류(장터/정비창 탭 분류용).</summary>
    public static InventoryItemType TypeOf(string itemId)
    {
        return GuessType(itemId);
    }

    private static InventoryItemType GuessType(string itemId)
    {
        string key = NormalizeItemId(itemId);
        if (key.Contains("clue") || key.Contains("renown") || key.EndsWith("_plaque", StringComparison.Ordinal))
        {
            return InventoryItemType.KeyItem;
        }

        if (GiftCatalog.IsGift(key))
        {
            return InventoryItemType.Gift;
        }

        if (EquipmentCatalog.IsEquipment(key))
        {
            return InventoryItemType.Equipment;
        }

        if (key.Contains("wood") || MaterialCatalog.Get(key) != null)
        {
            return InventoryItemType.Material;
        }

        return InventoryItemType.Consumable;
    }

    private void NormalizeStoredKeys()
    {
        if (session == null || session.inventory == null || session.inventory.Count == 0)
        {
            return;
        }

        Dictionary<string, int> normalized = null;
        foreach (KeyValuePair<string, int> pair in session.inventory)
        {
            string key = NormalizeItemId(pair.Key);
            if (string.IsNullOrEmpty(key) || key != pair.Key || pair.Value <= 0)
            {
                normalized = new Dictionary<string, int>();
                break;
            }
        }

        if (normalized == null)
        {
            return;
        }

        foreach (KeyValuePair<string, int> pair in session.inventory)
        {
            if (pair.Value <= 0)
            {
                continue;
            }

            string key = NormalizeItemId(pair.Key);
            if (string.IsNullOrEmpty(key))
            {
                continue;
            }

            normalized.TryGetValue(key, out int current);
            normalized[key] = current + pair.Value;
        }

        session.inventory.Clear();
        foreach (KeyValuePair<string, int> pair in normalized)
        {
            session.inventory[pair.Key] = pair.Value;
        }
    }
}
}
