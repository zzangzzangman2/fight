using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace JoseonMurimTactics.Editor
{
public static class ParkSungjunPixelPrototypeBuilder
{
    public const string VisualId = "park_sungjun_pixel";
    public const string SheetPath =
        "Assets/JoseonMurimTactics/Art/Characters/park_sungjun/Sprites/Pixel/park_sungjun_pixel_sheet.png";
    public const string FrameFolder =
        "Assets/JoseonMurimTactics/Art/Characters/park_sungjun/Sprites/Pixel/Frames";
    public const string VisualPath =
        "Assets/JoseonMurimTactics/Art/Characters/park_sungjun/VisualData/park_sungjun_pixel_visual.asset";

    private const int Columns = 6;
    private const int CellSize = 64;
    private static readonly Vector2 Pivot = new Vector2(0.5f, 0.08f);

    [MenuItem("Joseon Murim Tactics/Combat/Build Park Sungjun Pixel Prototype")]
    public static void Build()
    {
        ConfigureFrameImporters();
        CharacterVisualData visual = LoadOrCreateVisual();
        Dictionary<string, Sprite> sprites = LoadSprites();

        Sprite[] idle = Row(sprites, "idle", 4);
        Sprite[] walk = Row(sprites, "walk", 6);
        Sprite[] attack = Row(sprites, "attack", 6);
        Sprite[] skill = Row(sprites, "skill", 6);
        Sprite[] hit = Row(sprites, "hit", 2);
        Sprite[] guard = Row(sprites, "guard", 2);
        Sprite defeated = Get(sprites, "defeated", 0);

        visual.visualId = VisualId;
        visual.fullBodySprite = idle[0];
        visual.idlePoseSprite = idle[0];
        visual.movePoseSprite = walk[0];
        visual.attackPoseSprite = attack[3];
        visual.skillPoseSprite = skill[3];
        visual.hitPoseSprite = hit[0];
        visual.defeatedPoseSprite = defeated;
        visual.actedPoseSprite = guard[0];
        visual.idleSidePoseSprite = idle[0];
        visual.idleBackPoseSprite = idle[0];
        visual.moveSidePoseSprite = walk[0];
        visual.moveBackPoseSprite = walk[0];
        visual.idleFrames = idle;
        visual.moveFrames = walk;
        visual.moveSideFrames = walk;
        visual.moveBackFrames = walk;
        visual.attackFrames = attack;
        visual.skillFrames = skill;
        visual.hitFrames = hit;
        visual.idleFrameRate = 4f;
        visual.pixelSpriteMode = true;
        visual.livingMotion = null;
        visual.idleClip = null;
        visual.selectedIdleClip = null;
        visual.moveClip = null;
        visual.attackClip = null;
        visual.skillClip = null;
        visual.hitClip = null;
        visual.guardClip = null;
        visual.waitClip = null;
        visual.defeatClip = null;
        visual.victoryClip = null;
        visual.turnStartClip = null;
        visual.lowHpClip = null;
        visual.blinkFaceSprite = null;
        visual.happyFaceSprite = null;
        visual.angryFaceSprite = null;
        visual.painFaceSprite = null;
        visual.seriousFaceSprite = null;
        visual.enableBlink = false;
        visual.enableLayerSway = false;
        visual.enableFootDust = true;
        visual.enableSelectionPop = true;
        visual.enableImpactFreeze = true;
        visual.defaultOutfit = null;
        visual.outfitOptions = new CharacterOutfitData[0];
        visual.defaultWeaponType = WeaponType.Sword;
        visual.weaponAnimationSet = null;
        visual.heightInTiles = 1.02f;
        visual.spriteOffset = new Vector2(0f, 0.01f);
        visual.sortingOffset = 0;
        visual.idleAmplitude = 0.018f;
        visual.idleSpeed = 0.8f;
        visual.breathingScale = 0.006f;
        visual.shadowWidth = 0.58f;
        visual.shadowHeight = 0.15f;
        visual.normalTint = Color.white;
        visual.selectedTint = new Color(1f, 0.9496f, 0.804f, 1f);
        visual.actedTint = new Color(0.72f, 0.78f, 0.86f, 1f);
        visual.hitTint = new Color(0.9928f, 0.6848f, 0.6556f, 1f);
        visual.guardTint = new Color(1f, 0.937f, 0.755f, 1f);
        visual.defeatedTint = new Color(0.55f, 0.55f, 0.55f, 1f);
        visual.moveSecondsPerTile = 0.2f;
        visual.moveSettleTime = 0.08f;
        visual.moveLeanDegrees = 0f;
        visual.attackLunge = 0.06f;
        visual.skillPulseScale = 0.04f;
        visual.hitRecoil = 0.06f;

        EditorUtility.SetDirty(visual);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ParkSungjunPixelPrototypeBuilder] Built " + VisualPath);
    }

    private static void ConfigureFrameImporters()
    {
        string[] rowNames = { "idle", "walk", "attack", "skill", "hit", "guard", "defeated" };
        foreach (string rowName in rowNames)
        {
            for (int col = 0; col < Columns; col++)
            {
                string path = FramePath(rowName, col);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    throw new InvalidOperationException("Missing pixel frame importer: " + path);
                }

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = CellSize;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;

                TextureImporterSettings settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMode = (int)SpriteImportMode.Single;
                settings.spritePixelsPerUnit = CellSize;
                settings.spriteMeshType = SpriteMeshType.FullRect;
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = Pivot;
                importer.SetTextureSettings(settings);

                TextureImporterPlatformSettings platform = importer.GetDefaultPlatformTextureSettings();
                platform.format = TextureImporterFormat.RGBA32;
                platform.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SetPlatformTextureSettings(platform);
                importer.SaveAndReimport();
            }
        }
    }

    private static CharacterVisualData LoadOrCreateVisual()
    {
        CharacterVisualData visual = AssetDatabase.LoadAssetAtPath<CharacterVisualData>(VisualPath);
        if (visual != null)
        {
            return visual;
        }

        visual = ScriptableObject.CreateInstance<CharacterVisualData>();
        AssetDatabase.CreateAsset(visual, VisualPath);
        return visual;
    }

    private static Dictionary<string, Sprite> LoadSprites()
    {
        Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        string[] rowNames = { "idle", "walk", "attack", "skill", "hit", "guard", "defeated" };
        foreach (string rowName in rowNames)
        {
            for (int col = 0; col < Columns; col++)
            {
                string spriteName = SpriteName(rowName, col);
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(FramePath(rowName, col));
                if (sprite == null)
                {
                    throw new InvalidOperationException("Missing imported pixel frame: " + spriteName);
                }

                sprites[spriteName] = sprite;
            }
        }

        return sprites;
    }

    private static Sprite[] Row(Dictionary<string, Sprite> sprites, string row, int count)
    {
        Sprite[] result = new Sprite[count];
        for (int i = 0; i < count; i++)
        {
            result[i] = Get(sprites, row, i);
        }

        return result;
    }

    private static Sprite Get(Dictionary<string, Sprite> sprites, string row, int index)
    {
        string spriteName = SpriteName(row, index);
        if (!sprites.TryGetValue(spriteName, out Sprite sprite) || sprite == null)
        {
            throw new InvalidOperationException("Missing pixel sprite frame: " + spriteName);
        }

        return sprite;
    }

    private static string SpriteName(string row, int index)
    {
        return "park_sungjun_pixel_" + row + "_" + index.ToString("00");
    }

    private static string FramePath(string row, int index)
    {
        return FrameFolder + "/" + SpriteName(row, index) + ".png";
    }
}
}
