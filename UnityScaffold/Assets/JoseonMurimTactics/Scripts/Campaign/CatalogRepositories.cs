using System.Collections.Generic;

namespace JoseonMurimTactics
{
public interface ICompanionRepository
{
    CompanionInfo Get(string companionId);
    IEnumerable<CompanionInfo> All { get; }
}

public interface IMissionRepository
{
    MissionInfo Get(string missionId);
    IReadOnlyList<MissionInfo> All { get; }
}

public interface IBattleDefinitionRepository
{
    BattleDefinition Get(string battleId);
}

public interface ILoreRepository
{
    LoreEntry Get(string loreId);
    IReadOnlyList<LoreEntry> All { get; }
}

public interface IShopRepository
{
    IReadOnlyList<ShopItemInfo> StockFor(string shopId, GameSession session);
}

public sealed class LoreEntry
{
    public string id;
    public string title;
    public string category;
    public string body;
    public string unlockFlag;
}

public sealed class ShopItemInfo
{
    public string id;
    public string displayName;
    public InventoryItemType type;
    public int price;
    public string unlockFlag;
    public int stockLimit;
}

public sealed class CodeBackedCompanionRepository : ICompanionRepository
{
    public CompanionInfo Get(string companionId)
    {
        return CompanionCatalog.Info(companionId);
    }

    public IEnumerable<CompanionInfo> All => CompanionCatalog.All;
}

public sealed class CodeBackedMissionRepository : IMissionRepository
{
    public MissionInfo Get(string missionId)
    {
        return MissionCatalog.Get(missionId);
    }

    public IReadOnlyList<MissionInfo> All => MissionCatalog.All;
}

public sealed class CodeBackedBattleDefinitionRepository : IBattleDefinitionRepository
{
    public BattleDefinition Get(string battleId)
    {
        return BattleCatalog.Get(battleId);
    }
}

public sealed class CodeBackedLoreRepository : ILoreRepository
{
    private readonly List<LoreEntry> entries = new List<LoreEntry>
    {
        new LoreEntry
        {
            id = "lore_black_mark",
            title = "검은 표식",
            category = "사건",
            body = "백두산 영맥을 노린 철랑문 정찰조의 표식. 첫 전투 이후 서고에서 단서를 정리한다.",
            unlockFlag = StoryFlags.FirstBattleWon
        },
        new LoreEntry
        {
            id = "lore_baekdu_cheongwang",
            title = "백두천광검문",
            category = "문파",
            body = "백두산 자락의 몰락한 검문. 박성준이 문파 이름과 성향을 정하며 다시 세운다.",
            unlockFlag = StoryFlags.Chapter1Started
        }
    };

    public LoreEntry Get(string loreId)
    {
        if (string.IsNullOrEmpty(loreId))
        {
            return null;
        }

        foreach (LoreEntry entry in entries)
        {
            if (entry.id == loreId)
            {
                return entry;
            }
        }

        return null;
    }

    public IReadOnlyList<LoreEntry> All => entries;
}

public sealed class CodeBackedShopRepository : IShopRepository
{
    private readonly List<ShopItemInfo> defaultStock = new List<ShopItemInfo>
    {
        new ShopItemInfo
        {
            id = "medicine_bundle",
            displayName = "약재 꾸러미",
            type = InventoryItemType.Consumable,
            price = 18,
            stockLimit = 3
        },
        new ShopItemInfo
        {
            id = "wood_bundle",
            displayName = "목재 묶음",
            type = InventoryItemType.Material,
            price = 12,
            stockLimit = 5
        },
        new ShopItemInfo
        {
            id = "throwing_dagger_bundle",
            displayName = "투척 비수 묶음",
            type = InventoryItemType.Consumable,
            price = 30,
            stockLimit = 3
        },
        new ShopItemInfo
        {
            id = "inner_power_pill",
            displayName = "내공단",
            type = InventoryItemType.Consumable,
            price = 60,
            stockLimit = 1,
            unlockFlag = StoryFlags.FirstBattleWon
        }
    };

    public IReadOnlyList<ShopItemInfo> StockFor(string shopId, GameSession session)
    {
        return defaultStock;
    }
}
}
