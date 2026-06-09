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
    public readonly Dictionary<string, int> factionReputation = new Dictionary<string, int>();
    public readonly Dictionary<string, int> inventory = new Dictionary<string, int>();
    public readonly HashSet<string> storyFlags = new HashSet<string>();
    public readonly Dictionary<string, int> intVars = new Dictionary<string, int>();
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
        return recruitedCompanionIds.Contains(companionId);
    }

    public void RecruitCompanion(string companionId)
    {
        if (!string.IsNullOrEmpty(companionId) && !recruitedCompanionIds.Contains(companionId))
        {
            recruitedCompanionIds.Add(companionId);
        }
    }

    // ----- 직렬화 -----

    public string ToJson()
    {
        SaveDto dto = new SaveDto { sectName = sectName,
                                    heroDisposition = (int)heroDisposition,
                                    difficulty = (int)difficulty,
                                    startingArt = (int)startingArt,
                                    currentChapterId = currentChapterId,
                                    recruitedCompanionIds = new List<string>(recruitedCompanionIds),
                                    companionApproval = Pairs(companionApproval),
                                    factionReputation = Pairs(factionReputation),
                                    inventory = Pairs(inventory),
                                    storyFlags = new List<string>(storyFlags),
                                    intVars = Pairs(intVars),
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
        FillFromPairs(session.factionReputation, dto.factionReputation);
        FillFromPairs(session.inventory, dto.inventory);
        FillFromPairs(session.intVars, dto.intVars);

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

    [Serializable]
    public struct StringIntPair
    {
        public string key;
        public int value;
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
        public List<StringIntPair> factionReputation;
        public List<StringIntPair> inventory;
        public List<string> storyFlags;
        public List<StringIntPair> intVars;
        public List<string> appliedBattleResultIds;
        public int actionsTaken;
        public long savedAtUnixSeconds;
        public int saveVersion;
        public double playTimeSeconds;
        public string currentHubId;
    }
}
}
