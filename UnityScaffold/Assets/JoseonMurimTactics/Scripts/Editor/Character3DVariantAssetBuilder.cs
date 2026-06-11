using System.IO;
using UnityEditor;
using UnityEngine;

namespace JoseonMurimTactics.Editor
{
public static class Character3DVariantAssetBuilder
{
    private const string Root = "Assets/JoseonMurimTactics";
    private const string VariantRoot = Root + "/Art/Characters3D/WuxiaVariants";
    private const string VisualOutputFolder = VariantRoot + "/VisualData";

    private static readonly Candidate[] Candidates = {
        new Candidate("park_sungjun_3d_navy_gold",
                      Root + "/Art/Characters/park_sungjun/VisualData/park_sungjun_visual.asset",
                      VariantRoot + "/ParkSeongjun_NavyGold_Prototype/Yuuka_Original_Mesh.fbx",
                      VisualOutputFolder + "/park_sungjun_3d_navy_gold_visual.asset",
                      new Vector3(0f, 0f, 0f),
                      new Vector3(0f, 180f, 0f),
                      1.12f,
                      0.78f,
                      0.20f),
        new Candidate("baek_ryeon_3d_snow_blue",
                      Root + "/Art/Characters/baek_ryeon/VisualData/baek_ryeon_visual.asset",
                      VariantRoot + "/BaekRyeon_SnowBlue_Prototype/CH0155_Mesh.fbx",
                      VisualOutputFolder + "/baek_ryeon_3d_snow_blue_visual.asset",
                      new Vector3(0f, 0f, 0f),
                      new Vector3(0f, 180f, 0f),
                      1.14f,
                      0.78f,
                      0.20f)
    };

    [MenuItem("Joseon Murim Tactics/Characters/Rebuild 3D Wuxia Candidate Visuals")]
    public static void RebuildWuxiaCandidateVisuals()
    {
        AssetDatabase.Refresh();
        EnsureFolder(VisualOutputFolder);

        foreach (Candidate candidate in Candidates)
        {
            BuildCandidate(candidate);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void BuildCandidate(Candidate candidate)
    {
        CharacterVisualData baseVisual = AssetDatabase.LoadAssetAtPath<CharacterVisualData>(candidate.baseVisualPath);
        GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(candidate.modelPath);
        if (baseVisual == null || modelPrefab == null)
        {
            Debug.LogWarning($"3D visual candidate skipped: {candidate.visualId}");
            return;
        }

        CharacterVisualData visual = AssetDatabase.LoadAssetAtPath<CharacterVisualData>(candidate.outputPath);
        if (visual == null)
        {
            visual = ScriptableObject.CreateInstance<CharacterVisualData>();
            AssetDatabase.CreateAsset(visual, candidate.outputPath);
        }

        EditorUtility.CopySerialized(baseVisual, visual);
        visual.name = Path.GetFileNameWithoutExtension(candidate.outputPath);
        visual.visualId = candidate.visualId;
        visual.battleVisualMode = CharacterBattleVisualMode.Model3D;
        visual.modelPrefab = modelPrefab;
        visual.modelAnimatorController = null;
        visual.modelLocalOffset = candidate.localOffset;
        visual.modelLocalEuler = candidate.localEuler;
        visual.modelScale = CalculateScale(modelPrefab, candidate.targetHeight, 1f);
        visual.modelGroundY = 0f;
        visual.modelShadowWidth = candidate.shadowWidth;
        visual.modelShadowHeight = candidate.shadowHeight;
        visual.modelFaceByYaw = true;
        visual.modelKeepFeetOnGround = true;
        EditorUtility.SetDirty(visual);
    }

    private static float CalculateScale(GameObject modelPrefab, float targetHeight, float fallback)
    {
        GameObject instance = Object.Instantiate(modelPrefab);
        instance.hideFlags = HideFlags.HideAndDontSave;

        try
        {
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return fallback;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds.size.y > 0.001f ? targetHeight / bounds.size.y : fallback;
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    private static void EnsureFolder(string folder)
    {
        string[] parts = folder.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }

    private readonly struct Candidate
    {
        public readonly string visualId;
        public readonly string baseVisualPath;
        public readonly string modelPath;
        public readonly string outputPath;
        public readonly Vector3 localOffset;
        public readonly Vector3 localEuler;
        public readonly float targetHeight;
        public readonly float shadowWidth;
        public readonly float shadowHeight;

        public Candidate(string visualId, string baseVisualPath, string modelPath, string outputPath,
                         Vector3 localOffset, Vector3 localEuler, float targetHeight, float shadowWidth,
                         float shadowHeight)
        {
            this.visualId = visualId;
            this.baseVisualPath = baseVisualPath;
            this.modelPath = modelPath;
            this.outputPath = outputPath;
            this.localOffset = localOffset;
            this.localEuler = localEuler;
            this.targetHeight = targetHeight;
            this.shadowWidth = shadowWidth;
            this.shadowHeight = shadowHeight;
        }
    }
}
}
