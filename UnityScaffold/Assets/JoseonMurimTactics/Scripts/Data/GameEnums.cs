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
    KeyItem
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
