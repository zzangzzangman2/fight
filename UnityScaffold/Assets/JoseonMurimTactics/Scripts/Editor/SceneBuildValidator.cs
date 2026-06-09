#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace JoseonMurimTactics.EditorTools
{
public static class SceneBuildValidator
{
    private static readonly string[] RequiredScenes = { SceneNames.Boot,         SceneNames.Title,
                                                        SceneNames.NewGameSetup, SceneNames.Prologue,
                                                        SceneNames.BattlePrep,   SceneNames.Battle,
                                                        SceneNames.BattleResult, SceneNames.HubPyesadang,
                                                        SceneNames.MissionBoard, SceneNames.WorldMap };

    [MenuItem("Joseon Murim/Validate Scenes")]
    public static void ValidateScenes()
    {
        HashSet<string> enabledSceneNames = new HashSet<string>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (!scene.enabled)
            {
                continue;
            }

            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scene.path);
            enabledSceneNames.Add(sceneName);
        }

        int missing = 0;
        foreach (string required in RequiredScenes)
        {
            if (!enabledSceneNames.Contains(required))
            {
                missing++;
                Debug.LogWarning($"[SceneBuildValidator] Missing scene in Build Settings: {required}");
            }
        }

        if (missing == 0)
        {
            Debug.Log("[SceneBuildValidator] All required Joseon Murim scenes are enabled in Build Settings.");
        }
    }

    [MenuItem("Joseon Murim/Open Startup Scene")]
    public static void OpenStartupScene()
    {
        EditorSceneManager.OpenScene("Assets/JoseonMurimTactics/Scenes/Boot.unity");
    }
}
}
#endif
