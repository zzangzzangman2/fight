using System;
using System.Text;

namespace JoseonMurimTactics
{
    public static class ProgressionSnapshotFormatter
    {
        public static string FormatCharacterLine(ProgressionService progression, string characterId)
        {
            if (progression == null)
            {
                return string.Empty;
            }

            CharacterProgressState state = progression.GetSnapshot(characterId);
            string xpText = state.IsMaxLevel ? "MAX" : state.xp + "/" + state.xpToNext;
            return state.displayName + " Lv." + state.level + " " + state.realmName + " XP " + xpText + " 무공점 " + state.martialPoints;
        }

        public static string FormatDetailed(ProgressionService progression, string characterId)
        {
            if (progression == null)
            {
                return string.Empty;
            }

            CharacterProgressState s = progression.GetSnapshot(characterId);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(FormatCharacterLine(progression, characterId));
            sb.AppendLine("HP 보너스 +" + s.hpBonus + " / 내공 보너스 +" + s.innerBonus);
            sb.AppendLine("근력 +" + s.statBonuses.strength + " / 민첩 +" + s.statBonuses.agility + " / 내공력 +" + s.statBonuses.innerPower + " / 정신 +" + s.statBonuses.spirit + " / 통찰 +" + s.statBonuses.insight + " / 매력 +" + s.statBonuses.charm);
            if (s.mastery.Count > 0)
            {
                sb.AppendLine("무공 숙련:");
                for (int i = 0; i < s.mastery.Count; i++)
                {
                    sb.AppendLine("- " + s.mastery[i].key + " " + s.mastery[i].value + "/1000");
                }
            }

            return sb.ToString();
        }

        public static string FormatParty(ProgressionService progression)
        {
            if (progression == null)
            {
                return string.Empty;
            }

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < CharacterGrowthCatalog.CorePartyIds.Length; i++)
            {
                sb.AppendLine(FormatCharacterLine(progression, CharacterGrowthCatalog.CorePartyIds[i]));
            }

            return sb.ToString();
        }

        public static string FormatRewardGraphLine(CharacterReward reward)
        {
            if (reward == null)
            {
                return string.Empty;
            }

            string before = reward.beforeXpToNext <= 0 ? "MAX" : reward.beforeXp + "/" + reward.beforeXpToNext;
            string after = reward.afterXpToNext <= 0 ? "MAX" : reward.afterXp + "/" + reward.afterXpToNext;
            string level = reward.beforeLevel == reward.afterLevel ? "Lv." + reward.afterLevel : "Lv." + reward.beforeLevel + "→" + reward.afterLevel;
            return reward.displayName + " " + level + " XP +" + reward.appliedXp + " (" + before + "→" + after + ")";
        }

        public static string FormatXpBar(float progress01, int width)
        {
            int safeWidth = Math.Max(4, width);
            int filled = Math.Max(0, Math.Min(safeWidth, (int)Math.Round(progress01 * safeWidth)));
            StringBuilder sb = new StringBuilder(safeWidth);
            for (int i = 0; i < safeWidth; i++)
            {
                sb.Append(i < filled ? '■' : '□');
            }

            return sb.ToString();
        }

        public static string FormatFactionConquest(FactionConquestService service, string factionId)
        {
            if (service == null || string.IsNullOrEmpty(factionId))
            {
                return string.Empty;
            }

            FactionConquestState s = service.GetState(factionId);
            return s.displayName + " 정복도 " + s.stage + "/100 / 장악 " + s.control + " / 적대 " + s.hostility + (s.defeated ? " / 격파" : string.Empty);
        }
    }
}
