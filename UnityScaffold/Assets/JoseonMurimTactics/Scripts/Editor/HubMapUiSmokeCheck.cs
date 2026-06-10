using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace JoseonMurimTactics.Editor
{
public static class HubMapUiSmokeCheck
{
    private const string HubScenePath = "Assets/JoseonMurimTactics/Scenes/Hub_Pyesadang.unity";
    private const string HubMapAssetPath = "Assets/JoseonMurimTactics/Resources/UI/hub_free_time_map_v1.png";

    [MenuItem("Joseon Murim Tactics/Hub/Smoke Check Free-Time Map UI")]
    public static void Run()
    {
        Texture2D map = AssetDatabase.LoadAssetAtPath<Texture2D>(HubMapAssetPath);
        Require(map != null, "Free-time hub map texture was not imported.");
        Require(map.width >= 1200, "Free-time hub map texture is too narrow for the map UI.");
        Require(map.height >= 650, "Free-time hub map texture is too short for the map UI.");

        EditorSceneManager.OpenScene(HubScenePath);
        HubController controller = Object.FindAnyObjectByType<HubController>();
        Require(controller != null, "Hub_Pyesadang scene is missing HubController.");
        RequireFreeTimeMission("MISSION_FREE_SOBAEK_BANDIT_LAIR", HubController.BanditLairBattleId);
        RequireFreeTimeMission("MISSION_FREE_SOBAEK_WOLF_PASS", HubController.WolfPassBattleId);
        RequireFreeTimeMission("MISSION_FREE_SOBAEK_TIGER_RAVINE", HubController.TigerRavineBattleId);
        RequireFreeTimeMission("MISSION_FREE_SOBAEK_LEOPARD_CLIFF", HubController.LeopardCliffBattleId);

        Debug.Log("[HubMapUiSmokeCheck] Free-time hub map UI smoke check passed.");
    }

    private static void RequireFreeTimeMission(string missionId, string battleId)
    {
        MissionInfo mission = MissionCatalog.Get(missionId);
        Require(mission != null, missionId + " is missing from MissionCatalog.");
        Require(mission.battleId == battleId, missionId + " should point to " + battleId + ".");
        Require(mission.repeatable && mission.consumesFreeTime, missionId + " should be a free-time repeatable mission.");

        BattleDefinition battle = BattleCatalog.Get(battleId);
        Require(battle != null, battleId + " is missing from BattleCatalog.");
        Require(battle.repeatable, battleId + " should be repeatable.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new System.InvalidOperationException(message);
        }
    }
}
}
