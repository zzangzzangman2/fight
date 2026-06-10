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

        Debug.Log("[HubMapUiSmokeCheck] Free-time hub map UI smoke check passed.");
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
