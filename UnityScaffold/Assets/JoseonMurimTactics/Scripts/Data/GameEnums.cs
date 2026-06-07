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
        Wall
    }

    public enum CoverType
    {
        None,
        Light,
        Heavy
    }

    public enum HazardType
    {
        None,
        Slippery,
        Smoke,
        Fire,
        Ice,
        Fall
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
        Social
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
        TerrainCue
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
}
