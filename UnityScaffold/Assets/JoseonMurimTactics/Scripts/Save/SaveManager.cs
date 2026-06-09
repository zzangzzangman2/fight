using System;
using System.IO;
using UnityEngine;

namespace JoseonMurimTactics
{
    /// <summary>
    /// GameSession을 JSON으로 저장/로드. v0.8에서는 단일 자동 저장 슬롯만 사용한다.
    /// 저장 위치: Application.persistentDataPath/save_auto.json
    /// </summary>
    public sealed class SaveManager
    {
        public const string AutoSlotFileName = "save_auto.json";

        private string Path => System.IO.Path.Combine(Application.persistentDataPath, AutoSlotFileName);

        public bool HasSave()
        {
            try
            {
                return File.Exists(Path);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SaveManager] HasSave failed: " + e.Message);
                return false;
            }
        }

        public bool Save(GameSession session)
        {
            if (session == null)
            {
                return false;
            }

            try
            {
                session.savedAtUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                File.WriteAllText(Path, session.ToJson());
                Debug.Log("[SaveManager] Saved to " + Path);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("[SaveManager] Save failed: " + e.Message);
                return false;
            }
        }

        public GameSession Load()
        {
            try
            {
                if (!File.Exists(Path))
                {
                    return null;
                }

                string json = File.ReadAllText(Path);
                return GameSession.FromJson(json);
            }
            catch (Exception e)
            {
                Debug.LogError("[SaveManager] Load failed: " + e.Message);
                return null;
            }
        }

        public void Delete()
        {
            try
            {
                if (File.Exists(Path))
                {
                    File.Delete(Path);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SaveManager] Delete failed: " + e.Message);
            }
        }
    }
}
