using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace JoseonMurimTactics.Editor
{
public static class BattleMapTileAssetGenerator
{
    private const string ResourceTileRoot = "Assets/JoseonMurimTactics/Resources/MapAssets/Tiles";
    private const string ResourcePropRoot = "Assets/JoseonMurimTactics/Resources/MapAssets/Objects";
    private const string TilesetOutputRoot = "Assets/JoseonMurimTactics/Art/BattleMaps/Tilesets/Generated";
    private const string PropOutputRoot = "Assets/JoseonMurimTactics/Art/BattleMaps/Props/Generated";
    private const string OverlayOutputRoot = "Assets/JoseonMurimTactics/Art/BattleMaps/Overlays/Generated";
    private const string MaterialOutputRoot = "Assets/JoseonMurimTactics/Art/BattleMaps/Materials";

    private static readonly Dictionary<TerrainType, string> TerrainSprites = new Dictionary<TerrainType, string>
    {
        { TerrainType.Plain, "plain_moss" },
        { TerrainType.Hill, "hill_moss" },
        { TerrainType.Stone, "stone_courtyard" },
        { TerrainType.Road, "road_stair" },
        { TerrainType.ShrineFloor, "shrine_floor" },
        { TerrainType.Bamboo, "bamboo_floor" },
        { TerrainType.Forest, "forest_floor" },
        { TerrainType.ShallowWater, "shallow_water" },
        { TerrainType.DeepWater, "deep_water" },
        { TerrainType.Water, "shallow_water" },
        { TerrainType.Wood, "wood_plank" },
        { TerrainType.Bridge, "wood_bridge" },
        { TerrainType.Roof, "roof_tile" },
        { TerrainType.Cliff, "cliff_face" },
        { TerrainType.Wall, "wall_broken" },
        { TerrainType.Rubble, "rubble" },
        { TerrainType.Mud, "mud_path" },
        { TerrainType.Snow, "snow_edge" },
        { TerrainType.Ice, "ice_slick" },
        { TerrainType.Gate, "gate_threshold" },
        { TerrainType.Interior, "shrine_floor" },
        { TerrainType.Fire, "fire_scorch" },
        { TerrainType.Smoke, "smoke_veil" },
        { TerrainType.Trap, "trap_mark" },
    };

    private static readonly string[] PropSprites =
    {
        "sect_signboard",
        "incense_burner",
        "red_lantern",
        "oil_jar",
        "wine_cart",
        "fallen_wall",
        "bridge_rope",
        "bamboo_bundle",
        "stone_lantern",
        "falling_boulder",
        "flame_pillar",
        "smoke_wisp",
        "baekdu_snow_pine",
        "baekdu_snowdrift_cover",
        "baekdu_ice_crystal",
        "baekdu_frozen_stone_lantern",
        "baekdu_broken_snow_gate",
        "baekdu_frozen_rope_posts",
        "baekdu_snow_boulder",
        "baekdu_hot_spring_steam",
    };

    [MenuItem("Joseon Murim Tactics/Battle Maps/Generate Tile Assets")]
    public static void Generate()
    {
        EnsureFolder(TilesetOutputRoot);
        EnsureFolder(PropOutputRoot);
        EnsureFolder(OverlayOutputRoot);
        EnsureFolder(MaterialOutputRoot);
        GenerateMaterials();
        GenerateTerrainTiles();
        GeneratePropTiles();
        GenerateOverlayTiles();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[BattleMapTileAssetGenerator] Generated battle map tile assets.");
    }

    private static void GenerateTerrainTiles()
    {
        foreach (KeyValuePair<TerrainType, string> pair in TerrainSprites)
        {
            Sprite sprite = LoadSprite(ResourceTileRoot, pair.Value);
            if (sprite == null)
            {
                continue;
            }

            TerrainTileData tile = LoadOrCreate<TerrainTileData>(
                $"{TilesetOutputRoot}/Terrain_{pair.Key}.asset");
            tile.sprite = sprite;
            tile.color = Color.white;
            tile.flags = TileFlags.None;
            tile.colliderType = Tile.ColliderType.None;
            tile.terrainType = pair.Key;
            tile.moveCost = DefaultMoveCost(pair.Key);
            tile.walkable = DefaultWalkable(pair.Key);
            tile.blocksMovement = !tile.walkable;
            tile.blocksLineOfSight = DefaultBlocksLineOfSight(pair.Key);
            tile.blocksProjectiles = tile.blocksLineOfSight;
            tile.isChokePoint = pair.Key == TerrainType.Bridge || pair.Key == TerrainType.Gate;
            tile.capacity = tile.isChokePoint ? 1 : 2;
            tile.elevation = DefaultElevation(pair.Key);
            tile.coverType = DefaultCover(pair.Key);
            tile.coverBonus = DefaultCoverBonus(tile.coverType);
            tile.hazardType = DefaultHazard(pair.Key);
            tile.northEdge = DefaultEdge(pair.Key);
            tile.eastEdge = DefaultEdge(pair.Key);
            tile.southEdge = DefaultEdge(pair.Key);
            tile.westEdge = DefaultEdge(pair.Key);
            tile.occupyAllowed = tile.walkable;
            EditorUtility.SetDirty(tile);
        }
    }

    private static void GeneratePropTiles()
    {
        foreach (string prop in PropSprites)
        {
            Sprite sprite = LoadSprite(ResourcePropRoot, prop);
            if (sprite == null)
            {
                continue;
            }

            Tile tile = LoadOrCreate<Tile>($"{PropOutputRoot}/Prop_{prop}.asset");
            tile.sprite = sprite;
            tile.color = Color.white;
            tile.flags = TileFlags.None;
            tile.colliderType = Tile.ColliderType.None;
            EditorUtility.SetDirty(tile);
        }
    }

    private static void GenerateOverlayTiles()
    {
        CreateOverlayTile("Highlight_Move", "shallow_water", new Color(0.25f, 0.58f, 1f, 0.34f));
        CreateOverlayTile("Highlight_Attack", "trap_mark", new Color(1f, 0.18f, 0.16f, 0.42f));
        CreateOverlayTile("Overlay_Objective", "gate_threshold", new Color(1f, 0.78f, 0.28f, 0.26f));
        CreateOverlayTile("Overlay_Danger", "fire_scorch", new Color(0.86f, 0.16f, 0.08f, 0.24f));
    }

    private static void CreateOverlayTile(string name, string spriteStem, Color color)
    {
        Sprite sprite = LoadSprite(ResourceTileRoot, spriteStem);
        if (sprite == null)
        {
            return;
        }

        Tile tile = LoadOrCreate<Tile>($"{OverlayOutputRoot}/{name}.asset");
        tile.sprite = sprite;
        tile.color = color;
        tile.flags = TileFlags.None;
        tile.colliderType = Tile.ColliderType.None;
        EditorUtility.SetDirty(tile);
    }

    private static void GenerateMaterials()
    {
        Shader spriteLit = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
        Shader fallback = spriteLit == null ? Shader.Find("Sprites/Default") : spriteLit;
        if (fallback == null)
        {
            return;
        }

        Material litMaterial = LoadOrCreateMaterial($"{MaterialOutputRoot}/BattleMap_SpriteLit.mat", fallback);
        litMaterial.color = Color.white;
        EditorUtility.SetDirty(litMaterial);

        Material highlightMaterial = LoadOrCreateMaterial($"{MaterialOutputRoot}/BattleMap_Highlight.mat", fallback);
        highlightMaterial.color = new Color(1f, 1f, 1f, 0.66f);
        EditorUtility.SetDirty(highlightMaterial);
    }

    private static T LoadOrCreate<T>(string assetPath) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
        if (asset != null)
        {
            return asset;
        }

        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, assetPath);
        return asset;
    }

    private static Material LoadOrCreateMaterial(string assetPath, Shader shader)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
        if (material != null)
        {
            return material;
        }

        material = new Material(shader);
        AssetDatabase.CreateAsset(material, assetPath);
        return material;
    }

    private static Sprite LoadSprite(string root, string stem)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>($"{root}/{stem}.png");
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
        {
            return;
        }

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

    private static int DefaultMoveCost(TerrainType terrainType)
    {
        switch (terrainType)
        {
        case TerrainType.Bamboo:
        case TerrainType.Forest:
        case TerrainType.Hill:
        case TerrainType.Mud:
        case TerrainType.Rubble:
        case TerrainType.Ice:
            return 2;
        case TerrainType.ShallowWater:
        case TerrainType.Water:
        case TerrainType.DeepWater:
            return 3;
        case TerrainType.Wall:
        case TerrainType.Cliff:
            return 99;
        default:
            return 1;
        }
    }

    private static bool DefaultWalkable(TerrainType terrainType)
    {
        return terrainType != TerrainType.Wall && terrainType != TerrainType.Cliff &&
               terrainType != TerrainType.DeepWater;
    }

    private static bool DefaultBlocksLineOfSight(TerrainType terrainType)
    {
        return terrainType == TerrainType.Wall || terrainType == TerrainType.Cliff ||
               terrainType == TerrainType.Bamboo || terrainType == TerrainType.Forest ||
               terrainType == TerrainType.Smoke || terrainType == TerrainType.Rubble;
    }

    private static int DefaultElevation(TerrainType terrainType)
    {
        switch (terrainType)
        {
        case TerrainType.Roof:
            return 3;
        case TerrainType.Hill:
        case TerrainType.Wall:
        case TerrainType.Cliff:
        case TerrainType.ShrineFloor:
            return 2;
        case TerrainType.Bridge:
        case TerrainType.Rubble:
        case TerrainType.Stone:
            return 1;
        default:
            return 0;
        }
    }

    private static CoverType DefaultCover(TerrainType terrainType)
    {
        switch (terrainType)
        {
        case TerrainType.Rubble:
        case TerrainType.Wall:
            return CoverType.Heavy;
        case TerrainType.Bamboo:
        case TerrainType.Forest:
        case TerrainType.Hill:
        case TerrainType.ShrineFloor:
            return CoverType.Light;
        default:
            return CoverType.None;
        }
    }

    private static int DefaultCoverBonus(CoverType coverType)
    {
        switch (coverType)
        {
        case CoverType.Full:
            return 4;
        case CoverType.Heavy:
            return 2;
        case CoverType.Light:
            return 1;
        default:
            return 0;
        }
    }

    private static HazardType DefaultHazard(TerrainType terrainType)
    {
        switch (terrainType)
        {
        case TerrainType.Fire:
            return HazardType.Fire;
        case TerrainType.Smoke:
            return HazardType.Smoke;
        case TerrainType.Ice:
            return HazardType.Ice;
        case TerrainType.Trap:
            return HazardType.Trap;
        case TerrainType.Water:
        case TerrainType.DeepWater:
            return HazardType.DeepWater;
        case TerrainType.Cliff:
            return HazardType.Fall;
        default:
            return HazardType.None;
        }
    }

    private static EdgeType DefaultEdge(TerrainType terrainType)
    {
        switch (terrainType)
        {
        case TerrainType.Cliff:
            return EdgeType.CliffDrop;
        case TerrainType.Wall:
            return EdgeType.HighWall;
        case TerrainType.Bridge:
            return EdgeType.BridgeRail;
        case TerrainType.Water:
        case TerrainType.ShallowWater:
        case TerrainType.DeepWater:
            return EdgeType.WaterBank;
        default:
            return EdgeType.None;
        }
    }
}
}
