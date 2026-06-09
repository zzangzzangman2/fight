using System;
using System.IO;
using UnityEngine;

namespace JoseonMurimTactics
{
/// <summary>세이브 슬롯 요약(타이틀/슬롯 화면 표시용).</summary>
public sealed class SaveSlotSummary
{
    public string slotId;
    public bool exists;
    public string sectName;
    public string chapterTitle;
    public string location;
    public string playTimeText;
    public string savedAtText;
    public string difficulty;
    public string disposition;
    public string mainQuestSummary;
    public int silver;
    public int companionCount;
    public string recentMissionId;
    public int saveVersion;
    public long savedAtUnixSeconds;
}

/// <summary>
/// GameSession을 JSON으로 저장/로드. 자동 슬롯 1개 + 수동 슬롯 3개.
/// 임시 파일에 먼저 쓰고 교체하는 원자적 저장, saveVersion으로 마이그레이션을 대비한다(설계 v1.0 §9).
/// </summary>
public sealed class SaveManager
{
    public const int CurrentVersion = 1;
    public const string AutoSlot = "auto";
    public static readonly string[] ManualSlots = { "1", "2", "3" };
    public static readonly string[] AllSlots = { "auto", "1", "2", "3" };

    private static string PathFor(string slot)
    {
        return Path.Combine(Application.persistentDataPath, "save_" + slot + ".json");
    }

    public bool HasSave(string slot)
    {
        try
        {
            return File.Exists(PathFor(slot));
        }
        catch
        {
            return false;
        }
    }

    public bool HasAnySave()
    {
        foreach (string slot in AllSlots)
        {
            if (HasSave(slot))
                return true;
        }

        return false;
    }

    /// <summary>가장 최근에 저장된 슬롯 id. 없으면 null.</summary>
    public string LatestSlotId()
    {
        string best = null;
        long bestTime = long.MinValue;
        foreach (string slot in AllSlots)
        {
            SaveSlotSummary s = Peek(slot);
            if (s.exists && s.savedAtUnixSeconds >= bestTime)
            {
                bestTime = s.savedAtUnixSeconds;
                best = slot;
            }
        }

        return best;
    }

    public SaveSlotSummary PeekLatestSaveSummary()
    {
        string slot = LatestSlotId();
        return string.IsNullOrEmpty(slot) ? new SaveSlotSummary { exists = false } : Peek(slot);
    }

    public bool Save(GameSession session, string slot)
    {
        if (session == null || string.IsNullOrEmpty(slot))
        {
            return false;
        }

        try
        {
            session.saveVersion = CurrentVersion;
            session.savedAtUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            string dest = PathFor(slot);
            string tmp = dest + ".tmp";
            File.WriteAllText(tmp, session.ToJson());

            if (File.Exists(dest))
            {
                File.Replace(tmp, dest, null);
            }
            else
            {
                File.Move(tmp, dest);
            }

            Debug.Log($"[SaveManager] Saved slot '{slot}' -> {dest}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Save slot '{slot}' failed: {e.Message}");
            return false;
        }
    }

    /// <summary>호환용: 자동 슬롯에 저장.</summary>
    public bool Save(GameSession session)
    {
        return Save(session, AutoSlot);
    }

    public GameSession Load(string slot)
    {
        try
        {
            string path = PathFor(slot);
            if (!File.Exists(path))
            {
                return null;
            }

            return GameSession.FromJson(File.ReadAllText(path));
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Load slot '{slot}' failed: {e.Message}");
            return null;
        }
    }

    /// <summary>호환용: 가장 최근 슬롯 로드(타이틀 '이어하기').</summary>
    public GameSession Load()
    {
        string slot = LatestSlotId();
        return slot == null ? null : Load(slot);
    }

    public void Delete(string slot)
    {
        try
        {
            string path = PathFor(slot);
            if (File.Exists(path))
                File.Delete(path);
            string tmp = path + ".tmp";
            if (File.Exists(tmp))
                File.Delete(tmp);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveManager] Delete slot '{slot}' failed: {e.Message}");
        }
    }

    public SaveSlotSummary Peek(string slot)
    {
        SaveSlotSummary s = new SaveSlotSummary { slotId = slot, exists = false };
        try
        {
            string path = PathFor(slot);
            if (!File.Exists(path))
            {
                return s;
            }

            GameSession.SaveDto dto = GameSession.ParseDto(File.ReadAllText(path));
            if (dto == null)
            {
                return s;
            }

            s.exists = true;
            s.sectName = string.IsNullOrEmpty(dto.sectName) ? "해동검문" : dto.sectName;
            s.chapterTitle = ChapterTitle(dto.currentChapterId);
            s.location = LocationTitle(dto.currentHubId);
            s.difficulty = StoryEnumLabels.Label((GameDifficulty)dto.difficulty);
            s.disposition = StoryEnumLabels.Label((HeroDisposition)dto.heroDisposition);
            s.saveVersion = dto.saveVersion;
            s.savedAtUnixSeconds = dto.savedAtUnixSeconds;
            s.playTimeText = FormatPlayTime(dto.playTimeSeconds);
            s.savedAtText = FormatSavedAt(dto.savedAtUnixSeconds);
            s.mainQuestSummary = MainQuestSummary(dto);
            s.silver = GetPair(dto.intVars, "silver");
            s.companionCount = dto.recruitedCompanionIds != null ? dto.recruitedCompanionIds.Count : 0;
            s.recentMissionId = RecentMission(dto);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveManager] Peek slot '{slot}' failed: {e.Message}");
            s.exists = false;
        }

        return s;
    }

    private static string ChapterTitle(string chapterId)
    {
        switch (chapterId)
        {
        case "CHAPTER_01":
        case "CH1_STARTED":
            return "제1장 · 꺼져가는 천광";
        case "CH2_IRON_WOLF_APPEARED":
            return "제2장 · 철랑문의 발톱";
        default:
            return string.IsNullOrEmpty(chapterId) ? "제0장" : chapterId;
        }
    }

    private static string MainQuestSummary(GameSession.SaveDto dto)
    {
        if (dto.storyFlags != null && dto.storyFlags.Contains(StoryFlags.FirstBattleWon))
        {
            return "철랑문의 배후와 검은 표식을 추적하라";
        }

        return "백두천광검문을 다시 세워라";
    }

    private static string LocationTitle(string hubId)
    {
        switch (hubId)
        {
        case SceneNames.HubPyesadang:
            return "백두산 검각";
        default:
            return string.IsNullOrEmpty(hubId) ? "미상" : hubId;
        }
    }

    private static string RecentMission(GameSession.SaveDto dto)
    {
        if (dto.storyFlags != null && dto.storyFlags.Contains(StoryFlags.FirstBattleWon))
        {
            return "BATTLE_PYESADANG_DEFENSE";
        }

        return string.Empty;
    }

    private static int GetPair(System.Collections.Generic.List<GameSession.StringIntPair> pairs, string key)
    {
        if (pairs == null || string.IsNullOrEmpty(key))
        {
            return 0;
        }

        foreach (GameSession.StringIntPair pair in pairs)
        {
            if (pair.key == key)
            {
                return pair.value;
            }
        }

        return 0;
    }

    private static string FormatPlayTime(double seconds)
    {
        if (seconds <= 0)
            return "00:00";
        TimeSpan t = TimeSpan.FromSeconds(seconds);
        return t.TotalHours >= 1 ? $"{(int)t.TotalHours}:{t.Minutes:00}:{t.Seconds:00}"
                                 : $"{t.Minutes:00}:{t.Seconds:00}";
    }

    private static string FormatSavedAt(long unix)
    {
        if (unix <= 0)
            return "-";
        try
        {
            return DateTimeOffset.FromUnixTimeSeconds(unix).LocalDateTime.ToString("yyyy-MM-dd HH:mm");
        }
        catch
        {
            return "-";
        }
    }
}
}
