using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace JoseonMurimTactics.Editor
{
public static class CharacterPlayerBuild
{
    private const string BattleBuildFolder = "Builds/BattleTest";
    private const string BattleExeName = "JoseonMurimTacticsBattleTest.exe";
    private const string GameBuildFolder = "Builds/Windows";
    private const string GameExeName = "JoseonMurimTactics.exe";

    public static void BuildWindowsGame()
    {
        string[] scenes = EnabledBuildScenes();
        if (scenes.Length == 0)
        {
            throw new InvalidOperationException("No enabled scenes are configured in EditorBuildSettings.");
        }

        if (!scenes[0].EndsWith("/Boot.unity", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Full game build must start from Boot.unity.");
        }

        BuildWindowsPlayer(scenes, GameBuildFolder, GameExeName);
    }

    public static void BuildWindowsBattleTest()
    {
        BattleTestSceneLauncher.RebuildBattleTestScene();
        AndroidPixelBattleTestSampleBuilder.InstallSamples();
        BuildWindowsPlayer(BattleTestSceneLauncher.ScenePath, BattleBuildFolder, BattleExeName);
    }

    public static void BuildWindowsBattleTestCurrentScene()
    {
        BuildWindowsPlayer(BattleTestSceneLauncher.ScenePath, BattleBuildFolder, BattleExeName);
    }

    private static void BuildWindowsPlayer(string scenePath, string buildFolder, string exeName)
    {
        BuildWindowsPlayer(new[] { scenePath }, buildFolder, exeName);
    }

    private static void BuildWindowsPlayer(string[] scenes, string buildFolder, string exeName)
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string outputFolder = Path.Combine(projectRoot, buildFolder);
        Directory.CreateDirectory(outputFolder);

        string exePath = Path.Combine(outputFolder, exeName);
        BuildPlayerOptions options =
            new BuildPlayerOptions { scenes = scenes, locationPathName = exePath, target = BuildTarget.StandaloneWindows64,
                                     options = BuildOptions.None };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded || !File.Exists(exePath))
        {
            throw new InvalidOperationException($"Windows player build failed: {report.summary.result}; exe exists={File.Exists(exePath)}");
        }

        Debug.Log($"[CharacterPlayerBuild] Built player: {exePath}");
    }

    private static string[] EnabledBuildScenes()
    {
        List<string> scenes = new List<string>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
            {
                scenes.Add(scene.path);
            }
        }

        return scenes.ToArray();
    }
}
}
