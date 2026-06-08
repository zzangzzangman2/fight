using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace JoseonMurimTactics.Editor
{
    public static class CharacterPlayerBuild
    {
        private const string ScenePath = "Assets/JoseonMurimTactics/Scenes/CharacterAssetPreview.unity";
        private const string BuildFolder = "Builds/CharacterAssetPreview";
        private const string ExeName = "JoseonMurimTacticsPreview.exe";

        public static void BuildWindowsPreview()
        {
            EnsurePreviewScene();

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string outputFolder = Path.Combine(projectRoot, BuildFolder);
            Directory.CreateDirectory(outputFolder);

            string exePath = Path.Combine(outputFolder, ExeName);
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = exePath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"Windows preview build failed: {report.summary.result}");
            }

            Debug.Log($"[CharacterPlayerBuild] Built player: {exePath}");
        }

        private static void EnsurePreviewScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                return;
            }

            CharacterPreviewLauncher.RebuildPreviewScene();
        }
    }
}
