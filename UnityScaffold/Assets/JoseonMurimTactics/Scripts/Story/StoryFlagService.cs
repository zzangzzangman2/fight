using UnityEngine;

namespace JoseonMurimTactics
{
    /// <summary>
    /// 스토리 플래그 및 정수 변수 Set/Get. 대화 선택지 조건, 퀘스트/동료/전투 분기 판정에 쓴다.
    /// GameSession을 직접 조작한다.
    /// </summary>
    public sealed class StoryFlagService
    {
        private readonly GameSession session;

        public StoryFlagService(GameSession session)
        {
            this.session = session;
        }

        public bool HasFlag(string flag)
        {
            return !string.IsNullOrEmpty(flag) && session.storyFlags.Contains(flag);
        }

        public void SetFlag(string flag)
        {
            if (!string.IsNullOrEmpty(flag))
            {
                session.storyFlags.Add(flag);
            }
        }

        public void ClearFlag(string flag)
        {
            if (!string.IsNullOrEmpty(flag))
            {
                session.storyFlags.Remove(flag);
            }
        }

        public int GetInt(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return 0;
            }

            return session.intVars.TryGetValue(key, out int value) ? value : 0;
        }

        public void SetInt(string key, int value)
        {
            if (!string.IsNullOrEmpty(key))
            {
                session.intVars[key] = value;
            }
        }

        public void AddInt(string key, int amount)
        {
            if (!string.IsNullOrEmpty(key))
            {
                session.intVars[key] = GetInt(key) + amount;
            }
        }
    }
}
