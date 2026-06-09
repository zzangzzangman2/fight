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

        return session.inventory.TryGetValue(NormalizeItemId(itemId), out int count) ? count : 0;
    }

    public void SetCount(string itemId, int count)
    {
        if (session == null || string.IsNullOrEmpty(itemId))
        {
            return;
        }

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

        switch (item)
        {
        case "약재 꾸러미":
        case "약초 꾸러미":
        case "supply:medicine":
            return "medicine_bundle";
        case "내공단":
            return "inner_power_pill";
        case "투척 비수 묶음":
            return "throwing_dagger_bundle";
        case "목재 묶음":
            return "wood_bundle";
        case "무공 단서: 새벽일섬":
            return "skill_clue_dawn_flash";
        default:
            return item.Trim().ToLowerInvariant().Replace(" ", "_").Replace(":", "_");
        }
    }

    public static string Label(string itemId)
    {
        switch (NormalizeItemId(itemId))
        {
        case "medicine_bundle":
            return "약재 꾸러미";
        case "inner_power_pill":
            return "내공단";
        case "throwing_dagger_bundle":
            return "투척 비수 묶음";
        case "wood_bundle":
            return "목재 묶음";
        case "skill_clue_dawn_flash":
            return "무공 단서: 새벽일섬";
        default:
            return itemId;
        }
    }

    private static InventoryItemType GuessType(string itemId)
    {
        string key = NormalizeItemId(itemId);
        if (key.Contains("clue"))
        {
            return InventoryItemType.KeyItem;
        }

        if (key.Contains("wood"))
        {
            return InventoryItemType.Material;
        }

        return InventoryItemType.Consumable;
    }
}
}
