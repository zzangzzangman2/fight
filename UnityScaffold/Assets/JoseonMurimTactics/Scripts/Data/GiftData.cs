using System.Collections.Generic;

namespace JoseonMurimTactics
{
/// <summary>
/// 동료 선물 한 종의 정의. 선물은 연애도를 올리는
/// 정식 공략 수단이다. favoriteCompanionId가 맞으면 선호 보너스가 붙는다.
/// </summary>
public sealed class GiftInfo
{
    public string id;
    public string displayName;
    public int price;
    /// <summary>이 선물을 특히 좋아하는 동료 id(비면 범용).</summary>
    public string favoriteCompanionId;
    public int baseDelta;
    public int favoriteDelta;
    public string description;
    /// <summary>선호 동료가 받았을 때의 반응 한 줄.</summary>
    public string favoriteReaction;

    public GiftInfo(string id, string displayName, int price, string favoriteCompanionId, int baseDelta,
                    int favoriteDelta, string description, string favoriteReaction)
    {
        this.id = id;
        this.displayName = displayName;
        this.price = price;
        this.favoriteCompanionId = favoriteCompanionId;
        this.baseDelta = baseDelta;
        this.favoriteDelta = favoriteDelta;
        this.description = description;
        this.favoriteReaction = favoriteReaction;
    }

    public int DeltaFor(string companionId)
    {
        string favoriteId = CharacterIdAliasResolver.Normalize(favoriteCompanionId);
        string targetId = CharacterIdAliasResolver.Normalize(companionId);
        return !string.IsNullOrEmpty(favoriteId) && favoriteId == targetId ? favoriteDelta : baseDelta;
    }

    public bool IsFavoriteOf(string companionId)
    {
        string favoriteId = CharacterIdAliasResolver.Normalize(favoriteCompanionId);
        string targetId = CharacterIdAliasResolver.Normalize(companionId);
        return !string.IsNullOrEmpty(favoriteId) && favoriteId == targetId;
    }
}

/// <summary>선물 코드 카탈로그. 가격대: 범용 12~20은냥, 선호 선물 25~40은냥.</summary>
public static class GiftCatalog
{
    private static readonly List<GiftInfo> Items = new List<GiftInfo>
    {
        new GiftInfo("gift_snow_herbal_tea", "설산 약차", 28, "baek_ryeon", 3, 8,
                     "설악산 약초로 우린 따뜻한 차. 백련 공략 선물.",
                     "백련의 차가운 눈매가 김 너머로 사르르 풀린다."),
        new GiftInfo("gift_spicy_jerky", "매운 육포", 25, "do_arin", 3, 8,
                     "혀가 얼얼한 매운 육포. 도아린 공략 선물.",
                     "도아린이 한 입 베어 물고는 씩 웃는다. \"이거지!\""),
        new GiftInfo("gift_lightning_charm", "번개무늬 부적", 28, "jin_seoyul", 3, 8,
                     "뇌문이 새겨진 장난스러운 부적. 진서율 공략 선물.",
                     "진서율의 눈이 번개처럼 반짝인다. \"문주님, 취향 저격인데요?\""),
        new GiftInfo("gift_flower_ribbon", "꽃비단 매듭", 25, "shin_seoa", 3, 8,
                     "남원 비단으로 엮은 꽃 매듭. 신서아 응원 선물.",
                     "신서아가 매듭을 머리에 달고 빙글 돈다. \"어때요? 예쁘죠!\""),
        new GiftInfo("gift_black_lotus_thread", "흑련 손질실", 28, "han_biyeon", 3, 8,
                     "암기 손질에 쓰는 질긴 검은 실. 한비연 공략 선물.",
                     "한비연이 실을 손끝에 감아 보며 작게 웃는다. \"…쓸 만하네.\""),
        new GiftInfo("gift_fine_inkstick", "고급 먹", 18, null, 5, 5,
                     "은은한 솔향이 나는 고급 먹. 누구에게나 무난한 선물.",
                     null)
    };

    private static readonly Dictionary<string, GiftInfo> Map = BuildMap();

    private static Dictionary<string, GiftInfo> BuildMap()
    {
        Dictionary<string, GiftInfo> map = new Dictionary<string, GiftInfo>();
        foreach (GiftInfo item in Items)
        {
            map[item.id] = item;
        }

        return map;
    }

    public static IReadOnlyList<GiftInfo> All => Items;

    public static GiftInfo Get(string itemId)
    {
        string key = InventoryService.NormalizeItemId(itemId);
        return !string.IsNullOrEmpty(key) && Map.TryGetValue(key, out GiftInfo info) ? info : null;
    }

    public static bool IsGift(string itemId)
    {
        return Get(itemId) != null;
    }
}
}
