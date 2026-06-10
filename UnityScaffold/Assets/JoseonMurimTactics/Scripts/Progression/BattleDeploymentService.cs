using System;
using System.Collections.Generic;

namespace JoseonMurimTactics
{
    /// <summary>
    /// 전투 출진 파티를 GameSession.storyFlags에 저장한다.
    /// 핵심 규칙: 박성준은 항상 출진, 동료는 실제 선택/출진한 인원만 XP·숙련·유대를 받는다.
    /// SaveDto 구조를 바꾸지 않기 위해 문자열 flag만 사용한다.
    /// </summary>
    public static class BattleDeploymentService
    {
        public const int MaxCompanionSlots = 5;

        private const string ActiveMemberPrefix = "deployment:active:member:";
        private const string ActiveBattlePrefix = "deployment:active:battle:";
        private const string ExplicitDeploymentFlag = "deployment:active:explicit";

        public static void SetActiveParty(GameSession session, BattleDefinition definition, IEnumerable<string> selectedCompanionIds)
        {
            if (session == null)
            {
                return;
            }

            List<string> party = new List<string>();
            AddUnique(party, CharacterGrowthCatalog.ProtagonistId);

            int companionCount = 0;
            if (selectedCompanionIds != null)
            {
                foreach (string raw in selectedCompanionIds)
                {
                    if (companionCount >= MaxCompanionSlots)
                    {
                        break;
                    }

                    string id = CharacterGrowthCatalog.NormalizeCharacterId(raw);
                    if (id == CharacterGrowthCatalog.ProtagonistId)
                    {
                        continue;
                    }

                    if (!IsKnownCompanion(id))
                    {
                        continue;
                    }

                    if (!IsRosterMember(definition, id))
                    {
                        continue;
                    }

                    if (!IsBattleReady(session, id))
                    {
                        continue;
                    }

                    if (AddUnique(party, id))
                    {
                        companionCount++;
                    }
                }
            }

            StoreActiveParty(session, definition, party, true);
        }

        public static List<string> ResolveRewardParticipants(GameSession session, BattleDefinition definition)
        {
            List<string> active = GetActiveParty(session);
            if (active.Count > 0 && ActiveBattleMatches(session, definition))
            {
                return active;
            }

            return BuildDefaultParty(session, definition);
        }

        public static List<string> BuildDefaultParty(GameSession session, BattleDefinition definition)
        {
            List<string> party = new List<string>();
            AddUnique(party, CharacterGrowthCatalog.ProtagonistId);

            List<string> candidates = GetCandidateCompanions(session, definition, false);
            for (int i = 0; i < candidates.Count && i < MaxCompanionSlots; i++)
            {
                AddUnique(party, candidates[i]);
            }

            return party;
        }

        public static List<string> GetActiveParty(GameSession session)
        {
            List<string> party = new List<string>();
            if (session == null || session.storyFlags == null)
            {
                return party;
            }

            bool hasAny = false;
            foreach (string flag in session.storyFlags)
            {
                if (string.IsNullOrEmpty(flag) || !flag.StartsWith(ActiveMemberPrefix, StringComparison.Ordinal))
                {
                    continue;
                }

                hasAny = true;
                string id = CharacterGrowthCatalog.NormalizeCharacterId(flag.Substring(ActiveMemberPrefix.Length));
                AddUnique(party, id);
            }

            if (!hasAny)
            {
                return party;
            }

            EnsureProtagonistFirst(party);
            return party;
        }

        public static List<string> GetActiveCompanions(GameSession session)
        {
            List<string> party = GetActiveParty(session);
            List<string> companions = new List<string>();
            for (int i = 0; i < party.Count; i++)
            {
                if (party[i] != CharacterGrowthCatalog.ProtagonistId)
                {
                    companions.Add(party[i]);
                }
            }

            return companions;
        }

        public static List<string> GetCandidateCompanions(GameSession session, BattleDefinition definition)
        {
            return GetCandidateCompanions(session, definition, true);
        }

        public static bool HasExplicitDeployment(GameSession session, BattleDefinition definition)
        {
            return session != null && session.storyFlags != null && session.storyFlags.Contains(ExplicitDeploymentFlag) && ActiveBattleMatches(session, definition);
        }

        public static string ActiveBattleId(GameSession session)
        {
            if (session == null || session.storyFlags == null)
            {
                return string.Empty;
            }

            foreach (string flag in session.storyFlags)
            {
                if (!string.IsNullOrEmpty(flag) && flag.StartsWith(ActiveBattlePrefix, StringComparison.Ordinal))
                {
                    return flag.Substring(ActiveBattlePrefix.Length);
                }
            }

            return string.Empty;
        }

        public static void EnsureDefaultStored(GameSession session, BattleDefinition definition)
        {
            if (session == null)
            {
                return;
            }

            List<string> active = GetActiveParty(session);
            if (active.Count > 0 && ActiveBattleMatches(session, definition))
            {
                return;
            }

            StoreActiveParty(session, definition, BuildDefaultParty(session, definition), false);
        }

        private static List<string> GetCandidateCompanions(GameSession session, BattleDefinition definition, bool battleReadyOnly)
        {
            List<string> candidates = new List<string>();
            List<string> roster = RosterIds(definition);

            if (roster.Count <= 0)
            {
                for (int i = 0; i < CharacterGrowthCatalog.CorePartyIds.Length; i++)
                {
                    roster.Add(CharacterGrowthCatalog.CorePartyIds[i]);
                }
            }

            for (int i = 0; i < roster.Count; i++)
            {
                string id = CharacterGrowthCatalog.NormalizeCharacterId(roster[i]);
                if (id == CharacterGrowthCatalog.ProtagonistId || !IsKnownCompanion(id))
                {
                    continue;
                }

                if (battleReadyOnly && !IsBattleReady(session, id))
                {
                    continue;
                }

                if (!IsRecruitedOrRosterOpen(session, id))
                {
                    continue;
                }

                AddUnique(candidates, id);
            }

            return candidates;
        }

        private static void StoreActiveParty(GameSession session, BattleDefinition definition, List<string> party, bool explicitSelection)
        {
            if (session == null || session.storyFlags == null)
            {
                return;
            }

            // 이전 전투의 출진 flag가 HashSet 안에 남으면 보상 대상이 섞일 수 있으므로
            // 새 active party를 저장하기 직전에 항상 정리한다.
            ClearActiveParty(session);

            if (party == null || party.Count <= 0)
            {
                party = BuildDefaultParty(session, definition);
            }

            EnsureProtagonistFirst(party);

            if (definition != null && !string.IsNullOrEmpty(definition.id))
            {
                session.storyFlags.Add(ActiveBattlePrefix + BattleKey(definition));
            }

            if (explicitSelection)
            {
                session.storyFlags.Add(ExplicitDeploymentFlag);
            }

            for (int i = 0; i < party.Count; i++)
            {
                string id = CharacterGrowthCatalog.NormalizeCharacterId(party[i]);
                session.storyFlags.Add(ActiveMemberPrefix + id);
            }
        }

        private static void ClearActiveParty(GameSession session)
        {
            if (session == null || session.storyFlags == null)
            {
                return;
            }

            List<string> remove = new List<string>();
            foreach (string flag in session.storyFlags)
            {
                if (string.IsNullOrEmpty(flag))
                {
                    continue;
                }

                if (flag == ExplicitDeploymentFlag || flag.StartsWith(ActiveMemberPrefix, StringComparison.Ordinal) || flag.StartsWith(ActiveBattlePrefix, StringComparison.Ordinal))
                {
                    remove.Add(flag);
                }
            }

            for (int i = 0; i < remove.Count; i++)
            {
                session.storyFlags.Remove(remove[i]);
            }
        }

        private static bool ActiveBattleMatches(GameSession session, BattleDefinition definition)
        {
            string activeBattle = ActiveBattleId(session);
            if (string.IsNullOrEmpty(activeBattle) || definition == null || string.IsNullOrEmpty(definition.id))
            {
                return true;
            }

            return string.Equals(activeBattle, BattleKey(definition), StringComparison.Ordinal);
        }

        private static List<string> RosterIds(BattleDefinition definition)
        {
            List<string> ids = new List<string>();
            if (definition == null || definition.roster == null)
            {
                return ids;
            }

            for (int i = 0; i < definition.roster.Count; i++)
            {
                AddUnique(ids, CharacterGrowthCatalog.NormalizeCharacterId(definition.roster[i]));
            }

            return ids;
        }

        private static bool IsRosterMember(BattleDefinition definition, string characterId)
        {
            if (definition == null || definition.roster == null || definition.roster.Count <= 0)
            {
                return true;
            }

            string id = CharacterGrowthCatalog.NormalizeCharacterId(characterId);
            for (int i = 0; i < definition.roster.Count; i++)
            {
                if (CharacterGrowthCatalog.NormalizeCharacterId(definition.roster[i]) == id)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsKnownCompanion(string characterId)
        {
            return CompanionCatalog.Info(characterId) != null;
        }

        private static bool IsBattleReady(GameSession session, string companionId)
        {
            if (string.IsNullOrEmpty(companionId))
            {
                return false;
            }

            if (session == null)
            {
                return true;
            }

            CompanionStateService states = new CompanionStateService(session);
            return states.IsBattleReady(companionId);
        }

        private static bool IsRecruitedOrRosterOpen(GameSession session, string companionId)
        {
            if (session == null || session.recruitedCompanionIds == null || session.recruitedCompanionIds.Count <= 0)
            {
                return true;
            }

            return session.recruitedCompanionIds.Contains(companionId);
        }

        private static void EnsureProtagonistFirst(List<string> party)
        {
            if (party == null)
            {
                return;
            }

            string hero = CharacterGrowthCatalog.ProtagonistId;
            party.Remove(hero);
            party.Insert(0, hero);
        }

        private static bool AddUnique(List<string> list, string characterId)
        {
            if (list == null || string.IsNullOrEmpty(characterId))
            {
                return false;
            }

            string id = CharacterGrowthCatalog.NormalizeCharacterId(characterId);
            if (list.Contains(id))
            {
                return false;
            }

            list.Add(id);
            return true;
        }

        private static string BattleKey(BattleDefinition definition)
        {
            return definition == null ? string.Empty : ProgressionKeys.SafeId(definition.id).Replace(":", "_");
        }
    }
}
