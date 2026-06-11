using System.Collections.Generic;

namespace JoseonMurimTactics
{
/// <summary>
/// 장비 한 종의 정의(허브 경제 MVP). 유니크 인스턴스 없이 "itemId + 강화 레벨" 방식.
/// 강화 레벨은 intVars["equip:level:" + itemId]에 저장되어 같은 itemId 장비끼리 공유한다.
/// </summary>
public sealed class EquipmentInfo
{
    public string id;
    public string displayName;
    public EquipmentSlot slot;
    public int price;
    /// <summary>전용 장비 캐릭터 id(비면 공용).</summary>
    public string requiredCharacterId;
    public string description;

    // 기본 보정(전투 수치 반영: atk=피해, acc=명중, hp=최대 체력, guard=방어, inner=내공, move=이동)
    public int atk;
    public int acc;
    public int hp;
    public int guard;
    public int inner;
    public int move;

    public int maxUpgradeLevel = 5;

    public EquipmentInfo(string id, string displayName, EquipmentSlot slot, int price, string requiredCharacterId,
                         string description, int atk = 0, int acc = 0, int hp = 0, int guard = 0, int inner = 0,
                         int move = 0)
    {
        this.id = id;
        this.displayName = displayName;
        this.slot = slot;
        this.price = price;
        this.requiredCharacterId = requiredCharacterId;
        this.description = description;
        this.atk = atk;
        this.acc = acc;
        this.hp = hp;
        this.guard = guard;
        this.inner = inner;
        this.move = move;
    }

    public bool IsExclusive => !string.IsNullOrEmpty(requiredCharacterId);
}

/// <summary>강화 1회 비용(은냥 + 재료 수). 재료 종류는 슬롯별로 정해진다.</summary>
public struct UpgradeCost
{
    public int silver;
    public int materialCount;

    public UpgradeCost(int silver, int materialCount)
    {
        this.silver = silver;
        this.materialCount = materialCount;
    }
}

/// <summary>장비/재료 코드 카탈로그.
/// 경제 밸런스 메모: 첫 의뢰 보상 30~60은냥, 객잔 품팔이 +35 기준.
/// 일반 선물 12~20 / 선호 선물 25~40 / 초급 장비 60~90 / 중급 장비 120~180.
/// 강화 +1 30은냥, +2 45+재료1, +3 70+재료2, +4 100+재료2, +5 140+재료3.</summary>
public static class EquipmentCatalog
{
    private static readonly List<EquipmentInfo> Items = new List<EquipmentInfo>
    {
        new EquipmentInfo("training_sword", "수련검", EquipmentSlot.Weapon, 70, null,
                          "연무장에서 손에 익힌 기본 검. 누구나 다룰 수 있다.", atk: 1),
        new EquipmentInfo("baekya_sword", "백야검", EquipmentSlot.Weapon, 120, "park_sungjun",
                          "밤에도 꺼지지 않는 천광검문의 검. 빛 무공의 명중이 올라간다.", atk: 2, acc: 2),
        new EquipmentInfo("frost_spear", "설화창", EquipmentSlot.Weapon, 120, "baek_ryeon",
                          "설악의 서리를 머금은 창. 얼음·제어 초식이 한층 매서워진다.", atk: 2, acc: 1),
        new EquipmentInfo("fire_blade", "화왕도", EquipmentSlot.Weapon, 120, "do_arin",
                          "화왕도문의 진전이 깃든 도. 돌파 일격의 무게가 다르다.", atk: 2, acc: 1),
        new EquipmentInfo("padded_dobok", "누빈 도복", EquipmentSlot.Armor, 80, null,
                          "소백촌 아낙들이 누벼 준 도복. 가볍고 따뜻하다.", hp: 4),
        new EquipmentInfo("iron_scale_vest", "철린 조끼", EquipmentSlot.Armor, 130, null,
                          "철 비늘을 덧댄 조끼. 칼끝과 발톱을 한 번 더 막아 준다.", hp: 6, guard: 1),
        new EquipmentInfo("jade_charm", "청옥 부적", EquipmentSlot.Accessory, 75, null,
                          "맑은 내공의 흐름을 돕는 청옥 부적.", inner: 1),
        new EquipmentInfo("travel_talisman", "행로 부적", EquipmentSlot.Accessory, 90, null,
                          "먼 길의 발걸음을 가볍게 하는 부적. 이동이 한 칸 늘어난다.", move: 1)
    };

    private static readonly Dictionary<string, EquipmentInfo> Map = BuildMap();

    private static Dictionary<string, EquipmentInfo> BuildMap()
    {
        Dictionary<string, EquipmentInfo> map = new Dictionary<string, EquipmentInfo>();
        foreach (EquipmentInfo item in Items)
        {
            map[item.id] = item;
        }

        return map;
    }

    public static IReadOnlyList<EquipmentInfo> All => Items;

    public static EquipmentInfo Get(string itemId)
    {
        string key = InventoryService.NormalizeItemId(itemId);
        return !string.IsNullOrEmpty(key) && Map.TryGetValue(key, out EquipmentInfo info) ? info : null;
    }

    public static bool IsEquipment(string itemId)
    {
        return Get(itemId) != null;
    }

    public static string SlotLabel(EquipmentSlot slot)
    {
        switch (slot)
        {
        case EquipmentSlot.Weapon:
            return "무기";
        case EquipmentSlot.Armor:
            return "방어구";
        default:
            return "장신구";
        }
    }

    /// <summary>해당 슬롯 강화에 쓰는 재료 itemId.</summary>
    public static string MaterialFor(EquipmentSlot slot)
    {
        switch (slot)
        {
        case EquipmentSlot.Weapon:
            return "iron_ore";
        case EquipmentSlot.Armor:
            return "fine_cloth";
        default:
            return "jade_shard";
        }
    }

    /// <summary>targetLevel(+1~+5)로 올리는 비용. 범위를 벗어나면 silver 0.</summary>
    public static UpgradeCost CostFor(int targetLevel)
    {
        switch (targetLevel)
        {
        case 1:
            return new UpgradeCost(30, 0);
        case 2:
            return new UpgradeCost(45, 1);
        case 3:
            return new UpgradeCost(70, 2);
        case 4:
            return new UpgradeCost(100, 2);
        case 5:
            return new UpgradeCost(140, 3);
        default:
            return new UpgradeCost(0, 0);
        }
    }

    /// <summary>강화 레벨에 따른 추가 보정(설계: 무기 공+1/단계·명+1/2단계, 방어구 체+2/단계·방+1/2단계, 장신구 내공+1/단계).</summary>
    public static void UpgradeBonus(EquipmentInfo info, int level, ref int atk, ref int acc, ref int hp,
                                    ref int guard, ref int inner)
    {
        if (info == null || level <= 0)
        {
            return;
        }

        switch (info.slot)
        {
        case EquipmentSlot.Weapon:
            atk += level;
            acc += level / 2;
            break;
        case EquipmentSlot.Armor:
            hp += level * 2;
            guard += level / 2;
            break;
        default:
            inner += level;
            break;
        }
    }

    /// <summary>"공격 +2 · 명중 +1"식 효과 요약. level은 강화 포함 표시용.</summary>
    public static string DescribeBonus(EquipmentInfo info, int level)
    {
        if (info == null)
        {
            return string.Empty;
        }

        int atk = info.atk, acc = info.acc, hp = info.hp, guard = info.guard, inner = info.inner;
        UpgradeBonus(info, level, ref atk, ref acc, ref hp, ref guard, ref inner);

        List<string> parts = new List<string>();
        if (atk != 0) parts.Add($"공격 +{atk}");
        if (acc != 0) parts.Add($"명중 +{acc}");
        if (hp != 0) parts.Add($"체력 +{hp}");
        if (guard != 0) parts.Add($"방어 +{guard}");
        if (inner != 0) parts.Add($"내공 +{inner}");
        if (info.move != 0) parts.Add($"이동 +{info.move}");
        return parts.Count > 0 ? string.Join(" · ", parts) : "보정 없음";
    }
}

/// <summary>강화 재료 코드 카탈로그(장터 재료 탭).</summary>
public static class MaterialCatalog
{
    public sealed class MaterialInfo
    {
        public string id;
        public string displayName;
        public int price;
        public string description;

        public MaterialInfo(string id, string displayName, int price, string description)
        {
            this.id = id;
            this.displayName = displayName;
            this.price = price;
            this.description = description;
        }
    }

    private static readonly List<MaterialInfo> Items = new List<MaterialInfo>
    {
        new MaterialInfo("iron_ore", "철광석", 15, "무기 강화 재료. 뒷산 폐광에서도 가끔 나온다."),
        new MaterialInfo("fine_cloth", "질긴 천", 15, "방어구 강화 재료. 소백촌 베틀에서 짠 천."),
        new MaterialInfo("jade_shard", "옥 조각", 20, "장신구 강화 재료. 맑은 기운이 감도는 옥."),
        new MaterialInfo("wood_bundle", "목재 묶음", 12, "문파 시설 복구 재료."),
        new MaterialInfo("goat_milk", "산양 젖", 14, "늑대 고개 방목민들이 나눠 준 진한 산양 젖."),
        new MaterialInfo("tough_leather", "질긴 가죽", 16, "야수의 발톱을 견딘 질긴 가죽. 방어구 손질에 쓴다."),
        new MaterialInfo("tiger_pelt", "호피 조각", 28, "산군의 무늬가 남은 가죽 조각. 고급 방어구 재료."),
        new MaterialInfo("emergency_medicine", "응급 약재", 18, "전투 직후 상처를 덮는 데 쓰는 급한 약재 묶음."),
        new MaterialInfo("rare_herb", "희귀 약초", 24, "절벽 약초길에서만 드문드문 나는 향 짙은 약초."),
        new MaterialInfo("leopard_pelt", "표범 무늬 가죽", 26, "그림자 표범의 무늬가 남은 가죽. 가볍고 질기다.")
    };

    private static readonly Dictionary<string, MaterialInfo> Map = BuildMap();

    private static Dictionary<string, MaterialInfo> BuildMap()
    {
        Dictionary<string, MaterialInfo> map = new Dictionary<string, MaterialInfo>();
        foreach (MaterialInfo item in Items)
        {
            map[item.id] = item;
        }

        return map;
    }

    public static IReadOnlyList<MaterialInfo> All => Items;

    public static MaterialInfo Get(string itemId)
    {
        string key = InventoryService.NormalizeItemId(itemId);
        return !string.IsNullOrEmpty(key) && Map.TryGetValue(key, out MaterialInfo info) ? info : null;
    }
}
}
