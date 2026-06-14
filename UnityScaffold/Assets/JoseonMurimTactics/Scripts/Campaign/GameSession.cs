using System;
using System.Collections.Generic;
using UnityEngine;

namespace JoseonMurimTactics
{
/// <summary>
/// 현재 플레이 중인 게임의 모든 진행 상태. 씬을 넘어 GameRoot가 보관하며
/// SaveManager가 JSON으로 직렬화한다. 런타임은 Dictionary/HashSet을 쓰고,
/// 저장 시에는 JsonUtility가 다룰 수 있는 SaveDto로 변환한다.
/// </summary>
public sealed class GameSession
{
    public string sectName = "백두천광검문";
    public HeroDisposition heroDisposition = HeroDisposition.Romantic;
    public GameDifficulty difficulty = GameDifficulty.Murim;
    public StartingArt startingArt = StartingArt.Sword;

    public string currentChapterId = "CHAPTER_01";

    public readonly List<string> recruitedCompanionIds = new List<string>();
    public readonly Dictionary<string, int> companionApproval = new Dictionary<string, int>();
    public readonly Dictionary<string, int> companionInjury = new Dictionary<string, int>();
    public readonly Dictionary<string, int> companionFatigue = new Dictionary<string, int>();
    public readonly Dictionary<string, int> factionReputation = new Dictionary<string, int>();
    public readonly Dictionary<string, int> inventory = new Dictionary<string, int>();
    public readonly HashSet<string> storyFlags = new HashSet<string>();
    public readonly HashSet<string> completedMissionIds = new HashSet<string>();
    public readonly HashSet<string> unlockedCodexEntryIds = new HashSet<string>();
    public readonly Dictionary<string, int> intVars = new Dictionary<string, int>();
    // 캐릭터별 장착 장비 등 문자열 상태("equip:<charId>:<slot>" -> itemId). 구 세이브에는 없으므로 null 방어 필수.
    public readonly Dictionary<string, string> stringVars = new Dictionary<string, string>();
    public readonly Dictionary<string, int> missionAttempts = new Dictionary<string, int>();
    public readonly List<string> appliedBattleResultIds = new List<string>();

    // 마지막 전투 결과(허브/결과 화면 표시용 임시값, 저장 대상 아님).
    [NonSerialized]
    public BattleResultData lastBattleResult;

    public int actionsTaken;
    public long savedAtUnixSeconds;
    public int saveVersion;
    public double playTimeSeconds;
    public string currentHubId = "Hub_Pyesadang";

    public GameSession()
    {
    }

    public bool HasCompanion(string companionId)
    {
        string id = CharacterIdAliasResolver.Normalize(companionId);
        return !string.IsNullOrEmpty(id) && recruitedCompanionIds.Contains(id);
    }

    public void RecruitCompanion(string companionId)
    {
        string id = CharacterIdAliasResolver.Normalize(companionId);
        if (!string.IsNullOrEmpty(id) && !recruitedCompanionIds.Contains(id))
        {
            recruitedCompanionIds.Add(id);
        }
    }

    // ----- 직렬화 -----

    public string ToJson()
    {
        NormalizeCharacterIdsForCompatibility();
        SaveDto dto = new SaveDto { sectName = sectName,
                                    heroDisposition = (int)heroDisposition,
                                    difficulty = (int)difficulty,
                                    startingArt = (int)startingArt,
                                    currentChapterId = currentChapterId,
                                    recruitedCompanionIds = new List<string>(recruitedCompanionIds),
                                    companionApproval = Pairs(companionApproval),
                                    companionInjury = Pairs(companionInjury),
                                    companionFatigue = Pairs(companionFatigue),
                                    factionReputation = Pairs(factionReputation),
                                    inventory = Pairs(inventory),
                                    storyFlags = new List<string>(storyFlags),
                                    completedMissionIds = new List<string>(completedMissionIds),
                                    unlockedCodexEntryIds = new List<string>(unlockedCodexEntryIds),
                                    intVars = Pairs(intVars),
                                    stringVars = StringPairs(stringVars),
                                    missionAttempts = Pairs(missionAttempts),
                                    appliedBattleResultIds = new List<string>(appliedBattleResultIds),
                                    actionsTaken = actionsTaken,
                                    savedAtUnixSeconds = savedAtUnixSeconds,
                                    saveVersion = saveVersion,
                                    playTimeSeconds = playTimeSeconds,
                                    currentHubId = currentHubId };
        return JsonUtility.ToJson(dto, true);
    }

    public static GameSession FromJson(string json)
    {
        SaveDto dto = JsonUtility.FromJson<SaveDto>(json);
        GameSession session = new GameSession();
        if (dto == null)
        {
            return session;
        }

        session.sectName = string.IsNullOrEmpty(dto.sectName) ? session.sectName : dto.sectName;
        session.heroDisposition = (HeroDisposition)dto.heroDisposition;
        session.difficulty = (GameDifficulty)dto.difficulty;
        session.startingArt = (StartingArt)dto.startingArt;
        session.currentChapterId =
            string.IsNullOrEmpty(dto.currentChapterId) ? session.currentChapterId : dto.currentChapterId;
        session.actionsTaken = dto.actionsTaken;
        session.savedAtUnixSeconds = dto.savedAtUnixSeconds;
        session.saveVersion = dto.saveVersion;
        session.playTimeSeconds = dto.playTimeSeconds;
        session.currentHubId = string.IsNullOrEmpty(dto.currentHubId) ? session.currentHubId : dto.currentHubId;

        if (dto.recruitedCompanionIds != null)
        {
            session.recruitedCompanionIds.AddRange(dto.recruitedCompanionIds);
        }

        FillFromPairs(session.companionApproval, dto.companionApproval);
        FillFromPairs(session.companionInjury, dto.companionInjury);
        FillFromPairs(session.companionFatigue, dto.companionFatigue);
        FillFromPairs(session.factionReputation, dto.factionReputation);
        FillFromPairs(session.inventory, dto.inventory);
        FillFromPairs(session.intVars, dto.intVars);
        FillFromStringPairs(session.stringVars, dto.stringVars);
        FillFromPairs(session.missionAttempts, dto.missionAttempts);

        if (dto.appliedBattleResultIds != null)
        {
            session.appliedBattleResultIds.AddRange(dto.appliedBattleResultIds);
        }

        if (dto.storyFlags != null)
        {
            foreach (string flag in dto.storyFlags)
            {
                session.storyFlags.Add(flag);
            }
        }

        FillSet(session.completedMissionIds, dto.completedMissionIds);
        FillSet(session.unlockedCodexEntryIds, dto.unlockedCodexEntryIds);
        session.NormalizeCharacterIdsForCompatibility();

        return session;
    }

    /// <summary>전체 세션을 만들지 않고 세이브 슬롯 요약만 빠르게 읽기 위한 DTO 파싱.</summary>
    public static SaveDto ParseDto(string json)
    {
        return string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<SaveDto>(json);
    }

    private static List<StringIntPair> Pairs(Dictionary<string, int> source)
    {
        List<StringIntPair> list = new List<StringIntPair>(source.Count);
        foreach (KeyValuePair<string, int> kvp in source)
        {
            list.Add(new StringIntPair { key = kvp.Key, value = kvp.Value });
        }

        return list;
    }

    private static void FillFromPairs(Dictionary<string, int> target, List<StringIntPair> pairs)
    {
        if (pairs == null)
        {
            return;
        }

        foreach (StringIntPair pair in pairs)
        {
            if (!string.IsNullOrEmpty(pair.key))
            {
                target[pair.key] = pair.value;
            }
        }
    }

    private static List<StringStringPair> StringPairs(Dictionary<string, string> source)
    {
        List<StringStringPair> list = new List<StringStringPair>(source.Count);
        foreach (KeyValuePair<string, string> kvp in source)
        {
            list.Add(new StringStringPair { key = kvp.Key, value = kvp.Value });
        }

        return list;
    }

    private static void FillFromStringPairs(Dictionary<string, string> target, List<StringStringPair> pairs)
    {
        if (pairs == null)
        {
            return;
        }

        foreach (StringStringPair pair in pairs)
        {
            if (!string.IsNullOrEmpty(pair.key) && !string.IsNullOrEmpty(pair.value))
            {
                target[pair.key] = pair.value;
            }
        }
    }

    private static void FillSet(HashSet<string> target, List<string> values)
    {
        if (values == null)
        {
            return;
        }

        foreach (string value in values)
        {
            if (!string.IsNullOrEmpty(value))
            {
                target.Add(value);
            }
        }
    }

    private void NormalizeCharacterIdsForCompatibility()
    {
        NormalizeIdList(recruitedCompanionIds);
        NormalizeIdDictionary(companionApproval);
        NormalizeIdDictionary(companionInjury);
        NormalizeIdDictionary(companionFatigue);
        NormalizeCharacterScopedIntVars();
        NormalizeCharacterScopedStringVars();
        NormalizeCharacterScopedFlags();
    }

    private static void NormalizeIdList(List<string> values)
    {
        if (values == null)
        {
            return;
        }

        HashSet<string> seen = new HashSet<string>();
        for (int i = values.Count - 1; i >= 0; i--)
        {
            string id = CharacterIdAliasResolver.Normalize(values[i]);
            if (string.IsNullOrEmpty(id) || seen.Contains(id))
            {
                values.RemoveAt(i);
                continue;
            }

            values[i] = id;
            seen.Add(id);
        }
    }

    private static void NormalizeIdDictionary(Dictionary<string, int> values)
    {
        if (values == null)
        {
            return;
        }

        List<KeyValuePair<string, int>> pairs = new List<KeyValuePair<string, int>>(values);
        values.Clear();
        foreach (KeyValuePair<string, int> pair in pairs)
        {
            string id = CharacterIdAliasResolver.Normalize(pair.Key);
            if (string.IsNullOrEmpty(id))
            {
                continue;
            }

            if (values.TryGetValue(id, out int existing))
            {
                values[id] = Math.Max(existing, pair.Value);
            }
            else
            {
                values[id] = pair.Value;
            }
        }
    }

    private void NormalizeCharacterScopedIntVars()
    {
        List<KeyValuePair<string, int>> pairs = new List<KeyValuePair<string, int>>(intVars);
        intVars.Clear();
        foreach (KeyValuePair<string, int> pair in pairs)
        {
            string key = NormalizeCharacterScopedKey(pair.Key);
            if (string.IsNullOrEmpty(key))
            {
                continue;
            }

            if (intVars.TryGetValue(key, out int existing))
            {
                intVars[key] = Math.Max(existing, pair.Value);
            }
            else
            {
                intVars[key] = pair.Value;
            }
        }
    }

    private void NormalizeCharacterScopedStringVars()
    {
        List<KeyValuePair<string, string>> pairs = new List<KeyValuePair<string, string>>(stringVars);
        stringVars.Clear();
        foreach (KeyValuePair<string, string> pair in pairs)
        {
            string key = NormalizeCharacterScopedKey(pair.Key);
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(pair.Value))
            {
                continue;
            }

            if (!stringVars.ContainsKey(key))
            {
                stringVars[key] = pair.Value;
            }
        }
    }

    private void NormalizeCharacterScopedFlags()
    {
        List<string> flags = new List<string>(storyFlags);
        storyFlags.Clear();
        foreach (string flag in flags)
        {
            string normalized = NormalizeCharacterScopedKey(flag);
            if (!string.IsNullOrEmpty(normalized))
            {
                storyFlags.Add(normalized);
            }
        }
    }

    private static string NormalizeCharacterScopedKey(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return key;
        }

        string normalized = NormalizeSimpleSuffix(key, "gift:last_day:");
        normalized = NormalizeSimpleSuffix(normalized, "companion:visit:last_day:");
        normalized = NormalizeSimpleSuffix(normalized, "companion:visit:count:");
        normalized = NormalizeSimpleSuffix(normalized, "pending_first_impression:");
        normalized = NormalizeSimpleSuffix(normalized, "applied_first_impression:");
        normalized = NormalizeSimpleSuffix(normalized, "deployment:active:member:");
        normalized = NormalizeColonScoped(normalized, "growth:");
        normalized = NormalizeColonScoped(normalized, "equip:");
        return normalized;
    }

    private static string NormalizeSimpleSuffix(string key, string prefix)
    {
        if (string.IsNullOrEmpty(key) || !key.StartsWith(prefix, StringComparison.Ordinal))
        {
            return key;
        }

        string id = CharacterIdAliasResolver.Normalize(key.Substring(prefix.Length));
        return string.IsNullOrEmpty(id) ? key : prefix + id;
    }

    private static string NormalizeColonScoped(string key, string prefix)
    {
        if (string.IsNullOrEmpty(key) || !key.StartsWith(prefix, StringComparison.Ordinal))
        {
            return key;
        }

        int start = prefix.Length;
        int end = key.IndexOf(':', start);
        if (end < 0)
        {
            return key;
        }

        string id = CharacterIdAliasResolver.Normalize(key.Substring(start, end - start));
        return string.IsNullOrEmpty(id) ? key : prefix + id + key.Substring(end);
    }

    [Serializable]
    public struct StringIntPair
    {
        public string key;
        public int value;
    }

    [Serializable]
    public struct StringStringPair
    {
        public string key;
        public string value;
    }

    [Serializable]
    public sealed class SaveDto
    {
        public string sectName;
        public int heroDisposition;
        public int difficulty;
        public int startingArt;
        public string currentChapterId;
        public List<string> recruitedCompanionIds;
        public List<StringIntPair> companionApproval;
        public List<StringIntPair> companionInjury;
        public List<StringIntPair> companionFatigue;
        public List<StringIntPair> factionReputation;
        public List<StringIntPair> inventory;
        public List<string> storyFlags;
        public List<string> completedMissionIds;
        public List<string> unlockedCodexEntryIds;
        public List<StringIntPair> intVars;
        public List<StringStringPair> stringVars;
        public List<StringIntPair> missionAttempts;
        public List<string> appliedBattleResultIds;
        public int actionsTaken;
        public long savedAtUnixSeconds;
        public int saveVersion;
        public double playTimeSeconds;
        public string currentHubId;
    }
}
}
