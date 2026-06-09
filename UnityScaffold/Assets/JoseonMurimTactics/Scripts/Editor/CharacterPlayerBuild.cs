using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace JoseonMurimTactics.Editor
{
public static class CharacterPlayerBuild
{
    private const string PreviewScenePath = "Assets/JoseonMurimTactics/Scenes/CharacterAssetPreview.unity";
    private const string PreviewBuildFolder = "Builds/CharacterAssetPreview";
    private const string PreviewExeName = "JoseonMurimTacticsPreview.exe";
    private const string BattleBuildFolder = "Builds/BattleTest";
    private const string BattleExeName = "JoseonMurimTacticsBattleTest.exe";

    public static void BuildWindowsBattleTest()
    {
        BattleTestSceneLauncher.RebuildBattleTestScene();
        BuildWindowsPlayer(BattleTestSceneLauncher.ScenePath, BattleBuildFolder, BattleExeName);
    }

    public static void BuildWindowsPreview()
    {
        EnsurePreviewScene();
        BuildWindowsPlayer(PreviewScenePath, PreviewBuildFolder, PreviewExeName);
    }

    private static void BuildWindowsPlayer(string scenePath, string buildFolder, string exeName)
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string outputFolder = Path.Combine(projectRoot, buildFolder);
        Directory.CreateDirectory(outputFolder);

        string exePath = Path.Combine(outputFolder, exeName);
        BuildPlayerOptions options =
            new BuildPlayerOptions { scenes = new[] { scenePath }, locationPathName = exePath,
                                     target = BuildTarget.StandaloneWindows64, options = BuildOptions.None };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new InvalidOperationException($"Windows player build failed: {report.summary.result}");
        }

        Debug.Log($"[CharacterPlayerBuild] Built player: {exePath}");
    }

    private static void EnsurePreviewScene()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(PreviewScenePath) != null)
        {
            return;
        }

        CharacterPreviewLauncher.RebuildPreviewScene();
    }
}
}
