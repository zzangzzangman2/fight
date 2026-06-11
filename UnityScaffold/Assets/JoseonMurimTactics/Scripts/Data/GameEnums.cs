namespace JoseonMurimTactics
{
public enum Faction
{
    Ally,
    Enemy,
    Neutral
}

public enum StatType
{
    Strength,
    Agility,
    InnerPower,
    Spirit,
    Insight,
    Charm
}

public enum ActionSlot
{
    Main,
    Bonus,
    Reaction,
    Free
}


public enum CombatElementType
{
    None,
    Fire,
    Ice,
    Lightning,
    WindFlower,
    DarkPoison,
    Light = 6
}
public enum WeaponType
{
    Sword,
    Dao,
    Spear,
    Bow,
    Fist,
    Dagger,
    Staff,
    Fan,
    Talisman,
    HiddenWeapon
}
public enum TargetType
{
    Self,
    Ally,
    Enemy,
    Node,
    Object
}

public enum TerrainType
{
    Stone,
    Wood,
    Water,
    Bamboo,
    Bridge,
    Roof,
    Cliff,
    Wall,
    Plain,
    Road,
    ShrineFloor,
    Forest,
    ShallowWater,
    DeepWater,
    Mud,
    Snow,
    Ice,
    Hill,
    Gate,
    Interior,
    Fire,
    Smoke,
    Trap,
    Rubble
}

public enum CoverType
{
    None,
    Light,
    Heavy,
    Full
}

public enum HazardType
{
    None,
    Slippery,
    Smoke,
    Fire,
    Ice,
    Fall,
    Trap,
    Poison,
    Collapse,
    DeepWater
}

public enum EdgeType
{
    None,
    SlopeUp,
    SlopeDown,
    LowWall,
    HighWall,
    CliffDrop,
    Fence,
    Gate,
    WaterBank,
    BridgeRail
}

public enum InteractableKind
{
    IncenseBurner,
    Lantern,
    OilJar,
    WineCart,
    FallenWall,
    WoodenBridge,
    BambooBundle,
    RockLantern,
    SectSignboard,
    Beacon,
    Ladder,
    Gate
}

public enum BattleMapEffectType
{
    CreateSmoke,
    CreateFire,
    CollapseBridge,
    PushAdjacent,
    CreateCover,
    RemoveCover,
    OpenGate,
    CloseGate,
    TriggerAlarm,
    BreakWall,
    FreezeWater,
    DropRock,
    RaiseBridge
}

public enum BattleMapTargetPattern
{
    Self,
    Adjacent4,
    Radius,
    Line,
    DefinedCells
}

public enum SkillTag
{
    Sword,
    Dao,
    Palm,
    Poison,
    Ice,
    Heal,
    Movement,
    Formation,
    Stance,
    Stealth,
    Nonlethal,
    Social,
    Light,
    Fire,
    Lightning,
    Wind,
    Flower,
    Dark,
    Spear,
    Blade,
    Staff,
    Fan,
    Dagger,
    Throwing,
    Support,
    Debuff,
    Counter,
    FollowUp,
    Terrain
}

public enum TimelineCue
{
    None,
    ParkGentlemanSword,
    ParkMoonCharm,
    ParkMoonStep,
    ParkCommand,
    YunMoonReturn,
    YunMeasureLine,
    YunEtiquetteCounter,
    BaekIcePalm,
    BaekSnowBreath,
    BaekFrostSeal,
    HanPoisonNeedle,
    HanSmokeStep,
    HanNeedleRain,
    DoMountainPalm,
    DoIronBody,
    DoQinna,
    TerrainCue,
    ParkLightSword,
    FrostSpearSeal,
    FireBladeRush,
    LightningStaffDance,
    FlowerWindFan,
    ShadowPoisonDagger,
    CompanionJoin,
    ApprovalUp,
    ApprovalDown
}

public enum InteractableEffectType
{
    CreateCover,
    CreateSmoke,
    CreateFire,
    CreateIce,
    KnockProne,
    Push,
    CollapseBridge,
    ShatterAltar,
    BlockSight
}

public enum InventoryItemType
{
    Consumable,
    Equipment,
    Material,
    KeyItem,
    Gift
}

/// <summary>캐릭터 정비창 장비 슬롯(허브 경제 MVP). 의상/보조무기/무공서는 이후 확장.</summary>
public enum EquipmentSlot
{
    Weapon,
    Armor,
    Accessory
}

public enum WorldMapNodeState
{
    Locked,
    Available,
    Completed,
    Danger,
    CompanionEvent,
    Hub
}
}
