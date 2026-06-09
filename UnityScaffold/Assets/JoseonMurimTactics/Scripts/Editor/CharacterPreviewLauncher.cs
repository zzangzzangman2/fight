using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JoseonMurimTactics.Editor
{
public static class CharacterPreviewLauncher
{
    private const string ScenePath = "Assets/JoseonMurimTactics/Scenes/CharacterAssetPreview.unity";
    private const string VisualDataFolder = "Assets/JoseonMurimTactics/Art/Characters/VisualData";
    private const string SpriteFolder = "Assets/JoseonMurimTactics/Art/Characters/Sprites/Individuals";

    private static readonly CharacterSpec[] Characters = {
        new CharacterSpec("park_sungjun", "박성준", "park_sungjun_fullbody.png", -3.75f, 1.34f),
        new CharacterSpec("yun_seohwa", "매화령", "yun_seohwa_fullbody.png", -2.25f, 1.22f),
        new CharacterSpec("baek_ryeon", "백련", "baek_ryeon_fullbody.png", -0.75f, 1.18f),
        new CharacterSpec("han_biyeon", "한비연", "han_biyeon_fullbody.png", 0.75f, 1.18f),
        new CharacterSpec("do_arin", "도아린", "do_arin_fullbody.png", 2.25f, 1.22f),
        new CharacterSpec("strategist", "서아", "strategist_fullbody.png", 3.75f, 1.22f)
    };

    [MenuItem("Joseon Murim Tactics/Open Character Asset Preview")]
    public static void OpenPreviewScene()
    {
        RebuildPreviewScene();
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
    }

    public static void OpenAndPlay()
    {
        OpenPreviewScene();
        EditorApplication.delayCall += () =>
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.isPlaying = true;
            }
        };
    }

    public static void RebuildPreviewScene()
    {
        EnsureFolder("Assets/JoseonMurimTactics/Scenes");
        EnsureFolder(VisualDataFolder);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "CharacterAssetPreview";

        CreateCamera();
        CreateLight();
        CreateBackdrop();
        CreateCharacters();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new GameObject("Preview Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 3.1f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.08f, 0.065f, 0.05f, 1f);
        cameraObject.transform.position = new Vector3(0f, 1.08f, -10f);
        cameraObject.tag = "MainCamera";
    }

    private static void CreateLight()
    {
        GameObject lightObject = new GameObject("Soft Preview Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 0.85f;
        lightObject.transform.rotation = Quaternion.Euler(45f, -25f, 0f);
    }

    private static void CreateBackdrop()
    {
        GameObject backdrop = GameObject.CreatePrimitive(PrimitiveType.Quad);
        backdrop.name = "Warm Dark Backdrop";
        backdrop.transform.position = new Vector3(0f, 1f, 1f);
        backdrop.transform.localScale = new Vector3(10f, 5.8f, 1f);

        Collider collider = backdrop.GetComponent<Collider>();
        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }

        Material material = new Material(Shader.Find("Sprites/Default"));
        material.name = "CharacterPreviewBackdrop";
        material.color = new Color(0.12f, 0.095f, 0.07f, 1f);
        backdrop.GetComponent<MeshRenderer>().sharedMaterial = material;
    }

    private static void CreateCharacters()
    {
        GameObject root = new GameObject("Character Preview Root");

        for (int i = 0; i < Characters.Length; i++)
        {
            CharacterSpec spec = Characters[i];
            CharacterVisualData visual = EnsureVisualData(spec);

            GameObject unit = new GameObject(spec.displayName);
            unit.transform.SetParent(root.transform, false);
            unit.transform.position = new Vector3(spec.x, 0f, 0f);

            CharacterVisualController controller = unit.AddComponent<CharacterVisualController>();
            controller.visual = visual;
            controller.sortingLayerName = "Default";
            controller.baseSortingOrder = 1000 + i;
            controller.ApplyVisual();
            controller.SetSelected(i == 0);

            CreateLabel(unit.transform, spec.displayName);
        }
    }

    private static CharacterVisualData EnsureVisualData(CharacterSpec spec)
    {
        string assetPath = $"{VisualDataFolder}/{spec.id}_visual.asset";
        CharacterVisualData visual = AssetDatabase.LoadAssetAtPath<CharacterVisualData>(assetPath);
        if (visual == null)
        {
            visual = ScriptableObject.CreateInstance<CharacterVisualData>();
            AssetDatabase.CreateAsset(visual, assetPath);
        }

        string spritePath = $"{SpriteFolder}/{spec.spriteFile}";
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (sprite == null && File.Exists(spritePath))
        {
            AssetDatabase.ImportAsset(spritePath, ImportAssetOptions.ForceUpdate);
            sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        }

        visual.visualId = spec.id;
        visual.fullBodySprite = sprite;
        visual.heightInTiles = spec.heightInTiles;
        visual.spriteOffset = new Vector2(0f, 0.14f);
        visual.idleAmplitude = 0.035f;
        visual.idleSpeed = 0.8f + (Mathf.Abs(spec.x) * 0.05f);
        visual.breathingScale = 0.012f;
        visual.shadowWidth = 0.72f;
        visual.shadowHeight = 0.17f;
        visual.sortingOffset = Mathf.RoundToInt(-spec.x * 2f);

        EditorUtility.SetDirty(visual);
        return visual;
    }

    private static void CreateLabel(Transform parent, string text)
    {
        GameObject label = new GameObject("Name Label");
        label.transform.SetParent(parent, false);
        label.transform.localPosition = new Vector3(0f, -0.16f, -0.02f);

        TextMesh mesh = label.AddComponent<TextMesh>();
        mesh.text = text;
        mesh.anchor = TextAnchor.MiddleCenter;
        mesh.alignment = TextAlignment.Center;
        mesh.fontSize = 52;
        mesh.characterSize = 0.018f;
        mesh.color = new Color(0.96f, 0.88f, 0.72f, 1f);

        MeshRenderer renderer = label.GetComponent<MeshRenderer>();
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = 2000;
    }

    private static void EnsureFolder(string folder)
    {
        string[] parts = folder.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }

    private readonly struct CharacterSpec
    {
        public readonly string id;
        public readonly string displayName;
        public readonly string spriteFile;
        public readonly float x;
        public readonly float heightInTiles;

        public CharacterSpec(string id, string displayName, string spriteFile, float x, float heightInTiles)
        {
            this.id = id;
            this.displayName = displayName;
            this.spriteFile = spriteFile;
            this.x = x;
            this.heightInTiles = heightInTiles;
        }
    }
}
}
