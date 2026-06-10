using System;
using System.Collections.Generic;

namespace JoseonMurimTactics
{
    [Serializable]
    public sealed class BattleContributionRecord
    {
        public string characterId;
        public int damageDealt;
        public int damageTaken;
        public int healingDone;
        public int kills;
        public int assists;
        public int objectives;
        public int tacticalActions;

        public int Score
        {
            get
            {
                return damageDealt + healingDone + kills * 80 + assists * 35 + objectives * 100 + tacticalActions * 20 + damageTaken / 3;
            }
        }
    }

    public sealed class BattleContributionSummary
    {
        public Dictionary<string, BattleContributionRecord> records = new Dictionary<string, BattleContributionRecord>();

        public BattleContributionRecord GetOrCreate(string characterId)
        {
            string id = CharacterGrowthCatalog.NormalizeCharacterId(characterId);
            BattleContributionRecord record;
            if (!records.TryGetValue(id, out record))
            {
                record = new BattleContributionRecord { characterId = id };
                records[id] = record;
            }

            return record;
        }

        public string MvpCharacterId()
        {
            string bestId = null;
            int bestScore = int.MinValue;
            foreach (KeyValuePair<string, BattleContributionRecord> pair in records)
            {
                int score = pair.Value != null ? pair.Value.Score : 0;
                if (score > bestScore)
                {
                    bestScore = score;
                    bestId = pair.Key;
                }
            }

            return bestId;
        }

        public int ScoreFor(string characterId)
        {
            string id = CharacterGrowthCatalog.NormalizeCharacterId(characterId);
            BattleContributionRecord record;
            return records.TryGetValue(id, out record) && record != null ? record.Score : 0;
        }
    }

    /// <summary>
    /// 전투 엔진이 나중에 훅을 붙일 수 있는 기여도 수집기.
    /// 현재 브리지는 전투 로그 연동 전이므로 기여도 정보가 없으면 평균 보상만 쓴다.
    /// </summary>
    public sealed class BattleContributionTracker
    {
        private readonly BattleContributionSummary summary = new BattleContributionSummary();

        public BattleContributionSummary Summary
        {
            get { return summary; }
        }

        public void RecordDamage(string characterId, int amount)
        {
            summary.GetOrCreate(characterId).damageDealt += Math.Max(0, amount);
        }

        public void RecordDamageTaken(string characterId, int amount)
        {
            summary.GetOrCreate(characterId).damageTaken += Math.Max(0, amount);
        }

        public void RecordHealing(string characterId, int amount)
        {
            summary.GetOrCreate(characterId).healingDone += Math.Max(0, amount);
        }

        public void RecordKill(string characterId)
        {
            summary.GetOrCreate(characterId).kills += 1;
        }

        public void RecordAssist(string characterId)
        {
            summary.GetOrCreate(characterId).assists += 1;
        }

        public void RecordObjective(string characterId)
        {
            summary.GetOrCreate(characterId).objectives += 1;
        }

        public void RecordTacticalAction(string characterId)
        {
            summary.GetOrCreate(characterId).tacticalActions += 1;
        }
    }
}
