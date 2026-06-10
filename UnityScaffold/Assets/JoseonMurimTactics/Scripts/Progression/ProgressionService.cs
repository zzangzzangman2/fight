using System;
using System.Collections.Generic;

namespace JoseonMurimTactics
{
    /// <summary>
    /// 캐릭터 레벨, 경지, 성장률, 무공 숙련을 처리한다.
    /// 저장은 GameSession.intVars/storyFlags를 사용해 기존 SaveDto 충돌을 피한다.
    /// </summary>
    public sealed class ProgressionService
    {
        private const int MasteryMax = 1000;
        private static readonly int[] MasteryMilestones = { 50, 150, 300, 500, 750, 1000 };

        private readonly GameSession session;

        public ProgressionService(GameSession session)
        {
            this.session = session;
        }

        public CharacterProgressState GetSnapshot(string characterId)
        {
            string id = CharacterGrowthCatalog.NormalizeCharacterId(characterId);
            EnsureInitialized(id);
            SyncRealm(id);

            CharacterProgressState state = new CharacterProgressState();
            state.characterId = id;
            state.displayName = CharacterGrowthCatalog.DisplayName(id);
            state.level = ProgressionKeys.GetInt(session, ProgressionKeys.Level(id), 1);
            state.xp = ProgressionKeys.GetInt(session, ProgressionKeys.Xp(id), 0);
            state.totalXp = ProgressionKeys.GetInt(session, ProgressionKeys.TotalXp(id), 0);
            state.xpToNext = XpTable.GetXpToNext(state.level);
            state.realmTier = ProgressionKeys.GetInt(session, ProgressionKeys.RealmTier(id), 0);

            RealmInfo realm = RealmCatalog.GetByLevel(state.level);
            state.realmId = realm.id;
            state.realmName = realm.displayName;
            state.martialPoints = ProgressionKeys.GetInt(session, ProgressionKeys.MartialPoints(id), 0);
            state.hpBonus = ProgressionKeys.GetInt(session, ProgressionKeys.HpBonus(id), 0);
            state.innerBonus = ProgressionKeys.GetInt(session, ProgressionKeys.InnerBonus(id), 0);
            state.statBonuses = new SixStats
            {
                strength = ProgressionKeys.GetInt(session, ProgressionKeys.StatBonus(id, "strength"), 0),
                agility = ProgressionKeys.GetInt(session, ProgressionKeys.StatBonus(id, "agility"), 0),
                innerPower = ProgressionKeys.GetInt(session, ProgressionKeys.StatBonus(id, "innerPower"), 0),
                spirit = ProgressionKeys.GetInt(session, ProgressionKeys.StatBonus(id, "spirit"), 0),
                insight = ProgressionKeys.GetInt(session, ProgressionKeys.StatBonus(id, "insight"), 0),
                charm = ProgressionKeys.GetInt(session, ProgressionKeys.StatBonus(id, "charm"), 0)
            };

            for (int i = 0; i < CharacterGrowthCatalog.GrowthMeterKeys.Length; i++)
            {
                string key = CharacterGrowthCatalog.GrowthMeterKeys[i];
                state.growthMeters.Add(new ProgressionIntPair(key, ProgressionKeys.GetInt(session, ProgressionKeys.GrowthMeter(id, key), 0)));
            }

            for (int i = 0; i < CharacterGrowthCatalog.CommonMasteryTags.Length; i++)
            {
                string tag = CharacterGrowthCatalog.CommonMasteryTags[i];
                int value = ProgressionKeys.GetInt(session, ProgressionKeys.Mastery(id, tag), 0);
                if (value > 0)
                {
                    state.mastery.Add(new ProgressionIntPair(tag, value));
                }
            }

            return state;
        }

        public List<LevelUpResult> AddXp(string characterId, int amount, ProgressionXpSourceType sourceType, bool allowOverflowTrainingCredit)
        {
            List<LevelUpResult> results = new List<LevelUpResult>();
            string id = CharacterGrowthCatalog.NormalizeCharacterId(characterId);
            if (amount <= 0 || session == null)
            {
                return results;
            }

            EnsureInitialized(id);

            int level = ProgressionKeys.GetInt(session, ProgressionKeys.Level(id), 1);
            if (level >= XpTable.MaxLevel)
            {
                AddTrainingCreditIfAllowed(amount, sourceType, allowOverflowTrainingCredit, results, id, "최대 레벨 도달");
                return results;
            }

            int xp = ProgressionKeys.GetInt(session, ProgressionKeys.Xp(id), 0) + amount;
            ProgressionKeys.AddInt(session, ProgressionKeys.TotalXp(id), amount);

            int safety = 0;
            while (level < XpTable.MaxLevel && safety < 100)
            {
                safety++;
                int needed = XpTable.GetXpToNext(level);
                if (needed <= 0 || xp < needed)
                {
                    break;
                }

                int nextLevel = level + 1;
                string blockReason;
                if (!CanAdvanceToLevel(id, nextLevel, out blockReason))
                {
                    int clampedXp = Math.Max(0, needed - 1);
                    int overflow = Math.Max(1, xp - clampedXp);
                    xp = clampedXp;
                    int credit = ConvertOverflowToTrainingCredit(overflow, sourceType, allowOverflowTrainingCredit);
                    if (credit > 0)
                    {
                        ProgressionKeys.AddInt(session, ProgressionKeys.TrainingCredit, credit);
                    }

                    LevelUpResult blocked = new LevelUpResult
                    {
                        characterId = id,
                        displayName = CharacterGrowthCatalog.DisplayName(id),
                        oldLevel = level,
                        newLevel = level,
                        oldRealmName = RealmCatalog.GetByLevel(level).displayName,
                        newRealmName = RealmCatalog.GetByLevel(nextLevel).displayName,
                        blocked = true,
                        blockedReason = blockReason,
                        convertedTrainingCredit = credit
                    };
                    results.Add(blocked);
                    break;
                }

                xp -= needed;
                int oldLevel = level;
                level = nextLevel;
                ProgressionKeys.SetInt(session, ProgressionKeys.Level(id), level);
                LevelUpResult result = ApplyLevelUpGrowth(id, oldLevel, level);
                results.Add(result);
            }

            ProgressionKeys.SetInt(session, ProgressionKeys.Xp(id), Math.Max(0, xp));
            SyncRealm(id);
            return results;
        }

        public MasteryGainResult AddMastery(string characterId, string masteryTag, int amount)
        {
            string id = CharacterGrowthCatalog.NormalizeCharacterId(characterId);
            string tag = string.IsNullOrEmpty(masteryTag) ? CharacterGrowthCatalog.Get(id).defaultMasteryTag : masteryTag;
            int safeAmount = Math.Max(0, amount);
            int oldValue = ProgressionKeys.GetInt(session, ProgressionKeys.Mastery(id, tag), 0);
            int newValue = ProgressionKeys.Clamp(oldValue + safeAmount, 0, MasteryMax);
            ProgressionKeys.SetInt(session, ProgressionKeys.Mastery(id, tag), newValue);

            MasteryGainResult result = new MasteryGainResult
            {
                characterId = id,
                masteryTag = tag,
                oldValue = oldValue,
                newValue = newValue,
                appliedAmount = Math.Max(0, newValue - oldValue)
            };

            for (int i = 0; i < MasteryMilestones.Length; i++)
            {
                int threshold = MasteryMilestones[i];
                if (oldValue < threshold && newValue >= threshold)
                {
                    result.crossedMilestones.Add(threshold);
                    ProgressionKeys.SetFlag(session, ProgressionKeys.MasteryMilestoneFlag(id, tag, threshold));
                }
            }

            return result;
        }

        public void GrantBreakthrough(string characterId, string breakthroughFlag)
        {
            if (string.IsNullOrEmpty(breakthroughFlag))
            {
                return;
            }

            string id = CharacterGrowthCatalog.NormalizeCharacterId(characterId);
            ProgressionKeys.SetFlag(session, ProgressionKeys.CharacterBreakthroughFlag(id, breakthroughFlag));
        }

        public void GrantPartyBreakthrough(string breakthroughFlag)
        {
            if (string.IsNullOrEmpty(breakthroughFlag))
            {
                return;
            }

            ProgressionKeys.SetFlag(session, breakthroughFlag);
            ProgressionKeys.SetFlag(session, breakthroughFlag + ":party");
        }

        public void EnsureCorePartyInitialized()
        {
            for (int i = 0; i < CharacterGrowthCatalog.CorePartyIds.Length; i++)
            {
                EnsureInitialized(CharacterGrowthCatalog.CorePartyIds[i]);
            }
        }

        private void EnsureInitialized(string characterId)
        {
            if (session == null)
            {
                return;
            }

            string id = CharacterGrowthCatalog.NormalizeCharacterId(characterId);
            if (ProgressionKeys.GetInt(session, ProgressionKeys.Level(id), 0) <= 0)
            {
                ProgressionKeys.SetInt(session, ProgressionKeys.Level(id), 1);
                ProgressionKeys.SetInt(session, ProgressionKeys.Xp(id), 0);
                ProgressionKeys.SetInt(session, ProgressionKeys.TotalXp(id), 0);
                ProgressionKeys.SetInt(session, ProgressionKeys.RealmTier(id), 0);
                ProgressionKeys.SetInt(session, ProgressionKeys.MartialPoints(id), 0);
                ProgressionKeys.SetInt(session, ProgressionKeys.HpBonus(id), 0);
                ProgressionKeys.SetInt(session, ProgressionKeys.InnerBonus(id), 0);
            }
        }

        private void SyncRealm(string characterId)
        {
            int level = ProgressionKeys.GetInt(session, ProgressionKeys.Level(characterId), 1);
            RealmInfo realm = RealmCatalog.GetByLevel(level);
            ProgressionKeys.SetInt(session, ProgressionKeys.RealmTier(characterId), realm.tier);
        }

        private bool CanAdvanceToLevel(string characterId, int nextLevel, out string reason)
        {
            CampaignArcInfo arc = CampaignArcCatalog.GetCurrent(session);
            if (nextLevel > arc.levelCap)
            {
                reason = "현재 캠페인 아크 레벨 상한 " + arc.levelCap + " 도달";
                return false;
            }

            int currentLevel = ProgressionKeys.GetInt(session, ProgressionKeys.Level(characterId), 1);
            RealmInfo currentRealm = RealmCatalog.GetByLevel(currentLevel);
            RealmInfo nextRealm = RealmCatalog.GetByLevel(nextLevel);
            if (currentRealm.tier != nextRealm.tier && nextRealm.requiresBreakthrough)
            {
                bool hasPersonal = ProgressionKeys.HasFlag(session, ProgressionKeys.CharacterBreakthroughFlag(characterId, nextRealm.breakthroughFlag));
                bool hasGlobal = ProgressionKeys.HasFlag(session, nextRealm.breakthroughFlag) || ProgressionKeys.HasFlag(session, nextRealm.breakthroughFlag + ":party");
                if (!hasPersonal && !hasGlobal)
                {
                    reason = nextRealm.displayName + " 돌파 조건 필요: " + nextRealm.breakthroughFlag;
                    return false;
                }
            }

            reason = string.Empty;
            return true;
        }

        private int ConvertOverflowToTrainingCredit(int overflow, ProgressionXpSourceType sourceType, bool allow)
        {
            if (!allow || overflow <= 0)
            {
                return 0;
            }

            float ratio = sourceType == ProgressionXpSourceType.Story ? 0.70f : 0.35f;
            if (sourceType == ProgressionXpSourceType.Training)
            {
                ratio = 0.50f;
            }

            return Math.Max(1, (int)Math.Round(overflow * ratio));
        }

        private void AddTrainingCreditIfAllowed(int amount, ProgressionXpSourceType sourceType, bool allow, List<LevelUpResult> results, string characterId, string reason)
        {
            int credit = ConvertOverflowToTrainingCredit(amount, sourceType, allow);
            if (credit > 0)
            {
                ProgressionKeys.AddInt(session, ProgressionKeys.TrainingCredit, credit);
            }

            results.Add(new LevelUpResult
            {
                characterId = characterId,
                displayName = CharacterGrowthCatalog.DisplayName(characterId),
                oldLevel = XpTable.MaxLevel,
                newLevel = XpTable.MaxLevel,
                oldRealmName = RealmCatalog.GetByLevel(XpTable.MaxLevel).displayName,
                newRealmName = RealmCatalog.GetByLevel(XpTable.MaxLevel).displayName,
                blocked = true,
                blockedReason = reason,
                convertedTrainingCredit = credit
            });
        }

        private LevelUpResult ApplyLevelUpGrowth(string characterId, int oldLevel, int newLevel)
        {
            CharacterGrowthProfile profile = CharacterGrowthCatalog.Get(characterId);
            RealmInfo oldRealm = RealmCatalog.GetByLevel(oldLevel);
            RealmInfo newRealm = RealmCatalog.GetByLevel(newLevel);

            LevelUpResult result = new LevelUpResult
            {
                characterId = characterId,
                displayName = profile.displayName,
                oldLevel = oldLevel,
                newLevel = newLevel,
                oldRealmName = oldRealm.displayName,
                newRealmName = newRealm.displayName,
                realmChanged = oldRealm.tier != newRealm.tier
            };

            ApplyMeterGain(characterId, "hp", profile.hpGrowth, result);
            ApplyMeterGain(characterId, "inner", profile.innerGrowth, result);
            ApplyMeterGain(characterId, "strength", profile.strengthGrowth, result);
            ApplyMeterGain(characterId, "agility", profile.agilityGrowth, result);
            ApplyMeterGain(characterId, "innerPower", profile.innerPowerGrowth, result);
            ApplyMeterGain(characterId, "spirit", profile.spiritGrowth, result);
            ApplyMeterGain(characterId, "insight", profile.insightGrowth, result);
            ApplyMeterGain(characterId, "charm", profile.charmGrowth, result);

            if (newLevel % 4 == 0)
            {
                AddMartialPoint(characterId, 1, result);
            }

            if (result.TotalStatGain <= 0)
            {
                AddStatBonus(characterId, profile.primaryStatKey, 1, result);
                result.notes.Add("무성장 방지 보정");
            }

            if (result.realmChanged)
            {
                ApplyRealmEntryBonus(characterId, newRealm, profile.primaryStatKey, result);
            }

            return result;
        }

        private void ApplyMeterGain(string characterId, string statKey, int growth, LevelUpResult result)
        {
            int meter = ProgressionKeys.GetInt(session, ProgressionKeys.GrowthMeter(characterId, statKey), 0) + Math.Max(0, growth);
            int thresholdCount = 0;
            while (meter >= 100)
            {
                meter -= 100;
                thresholdCount++;
            }

            ProgressionKeys.SetInt(session, ProgressionKeys.GrowthMeter(characterId, statKey), meter);
            if (thresholdCount <= 0)
            {
                return;
            }

            if (statKey == "hp")
            {
                int amount = thresholdCount * 2;
                ProgressionKeys.AddInt(session, ProgressionKeys.HpBonus(characterId), amount);
                result.hpBonus += amount;
                return;
            }

            if (statKey == "inner")
            {
                ProgressionKeys.AddInt(session, ProgressionKeys.InnerBonus(characterId), thresholdCount);
                result.innerBonus += thresholdCount;
                return;
            }

            AddStatBonus(characterId, statKey, thresholdCount, result);
        }

        private void AddStatBonus(string characterId, string statKey, int amount, LevelUpResult result)
        {
            if (amount <= 0)
            {
                return;
            }

            ProgressionKeys.AddInt(session, ProgressionKeys.StatBonus(characterId, statKey), amount);
            switch (statKey)
            {
                case "strength": result.strengthBonus += amount; break;
                case "agility": result.agilityBonus += amount; break;
                case "innerPower": result.innerPowerBonus += amount; break;
                case "spirit": result.spiritBonus += amount; break;
                case "insight": result.insightBonus += amount; break;
                case "charm": result.charmBonus += amount; break;
            }
        }

        private void AddMartialPoint(string characterId, int amount, LevelUpResult result)
        {
            if (amount <= 0)
            {
                return;
            }

            ProgressionKeys.AddInt(session, ProgressionKeys.MartialPoints(characterId), amount);
            result.martialPointBonus += amount;
        }

        private void ApplyRealmEntryBonus(string characterId, RealmInfo newRealm, string primaryStatKey, LevelUpResult result)
        {
            if (newRealm == null)
            {
                return;
            }

            switch (newRealm.tier)
            {
                case 1:
                    ProgressionKeys.AddInt(session, ProgressionKeys.HpBonus(characterId), 2);
                    ProgressionKeys.AddInt(session, ProgressionKeys.InnerBonus(characterId), 1);
                    result.hpBonus += 2;
                    result.innerBonus += 1;
                    result.notes.Add("이류 진입 보너스");
                    break;
                case 2:
                    ProgressionKeys.AddInt(session, ProgressionKeys.HpBonus(characterId), 3);
                    result.hpBonus += 3;
                    AddStatBonus(characterId, primaryStatKey, 1, result);
                    result.notes.Add("일류 진입 보너스");
                    break;
                case 3:
                    ProgressionKeys.AddInt(session, ProgressionKeys.HpBonus(characterId), 4);
                    ProgressionKeys.AddInt(session, ProgressionKeys.InnerBonus(characterId), 1);
                    result.hpBonus += 4;
                    result.innerBonus += 1;
                    AddMartialPoint(characterId, 1, result);
                    result.unlockedPassiveIds.Add("passive:jeoljeong_focus");
                    break;
                case 4:
                    ProgressionKeys.AddInt(session, ProgressionKeys.HpBonus(characterId), 5);
                    result.hpBonus += 5;
                    AddStatBonus(characterId, primaryStatKey, 1, result);
                    AddMartialPoint(characterId, 1, result);
                    result.unlockedPassiveIds.Add("passive:chojeoljeong_inner_flow");
                    break;
                case 5:
                    ProgressionKeys.AddInt(session, ProgressionKeys.HpBonus(characterId), 6);
                    ProgressionKeys.AddInt(session, ProgressionKeys.InnerBonus(characterId), 2);
                    result.hpBonus += 6;
                    result.innerBonus += 2;
                    AddMartialPoint(characterId, 2, result);
                    result.unlockedPassiveIds.Add("passive:hwa_body_as_blade");
                    break;
                case 6:
                    ProgressionKeys.AddInt(session, ProgressionKeys.HpBonus(characterId), 8);
                    result.hpBonus += 8;
                    AddStatBonus(characterId, primaryStatKey, 2, result);
                    AddMartialPoint(characterId, 2, result);
                    result.unlockedPassiveIds.Add("passive:hyeon_reading_fate");
                    break;
                case 7:
                    ProgressionKeys.AddInt(session, ProgressionKeys.HpBonus(characterId), 10);
                    ProgressionKeys.AddInt(session, ProgressionKeys.InnerBonus(characterId), 3);
                    result.hpBonus += 10;
                    result.innerBonus += 3;
                    AddMartialPoint(characterId, 2, result);
                    result.unlockedPassiveIds.Add("passive:saengsa_return_from_death");
                    break;
            }
        }
    }
}
