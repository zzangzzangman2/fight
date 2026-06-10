using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace JoseonMurimTactics.Editor
{
public static class BattleMapTilemapSmokeCheck
{
    private const string BattleTestScenePath = "Assets/JoseonMurimTactics/Scenes/BattleTest.unity";

    [MenuItem("Joseon Murim Tactics/Battle Maps/Smoke Check Tilemap Battlefield")]
    public static void Run()
    {
        EditorSceneManager.OpenScene(BattleTestScenePath);
        BattleTestController controller = UnityEngine.Object.FindAnyObjectByType<BattleTestController>();
        if (controller == null)
        {
            throw new MissingReferenceException("BattleTestController not found in BattleTest scene.");
        }

        controller.useTilemapBattlefield = true;
        controller.useLegacyDiamondTerrain = false;
        InvokePrivate(controller, "EnsureMapVisualSprites");
        InvokePrivate(controller, "CreateTerrain");

        GameObject battlefield = GameObject.Find("Battlefield_Tilemap");
        Require(battlefield != null, "Battlefield_Tilemap was not created.");
        Require(GameObject.Find("Tilemap_Ground") != null, "Tilemap_Ground was not created.");
        Require(GameObject.Find("Tilemap_Road") != null, "Tilemap_Road was not created.");
        Require(GameObject.Find("Tilemap_Cliff") != null, "Tilemap_Cliff was not created.");
        Require(GameObject.Find("Tilemap_Water") != null, "Tilemap_Water was not created.");
        Require(GameObject.Find("Tilemap_Decor") != null, "Tilemap_Decor was not created.");
        Require(GameObject.Find("Tilemap_Props") != null, "Tilemap_Props was not created.");
        Require(GameObject.Find("Tilemap_Overlay") != null, "Tilemap_Overlay was not created.");
        Require(GameObject.Find("Tilemap_Highlight_Move") != null, "Tilemap_Highlight_Move was not created.");
        Require(GameObject.Find("Tilemap_Highlight_Attack") != null, "Tilemap_Highlight_Attack was not created.");
        Require(GameObject.Find("Tilemap_Highlight_Danger") != null, "Tilemap_Highlight_Danger was not created.");
        Require(GameObject.Find("PropsRoot") != null, "PropsRoot was not created.");
        Require(GameObject.Find("LightsRoot") != null, "LightsRoot was not created.");
        SpriteRenderer paintedBackdrop = GameObject.Find("Painted Map Backdrop")?.GetComponent<SpriteRenderer>();
        Require(paintedBackdrop != null && paintedBackdrop.sprite != null,
                "Painted battle map backdrop sprite was not loaded.");

        BattleMapTilemapBinder binder = battlefield.GetComponent<BattleMapTilemapBinder>();
        Require(binder != null, "BattleMapTilemapBinder was not created.");

        TacticalGridOverlay overlay = battlefield.GetComponentInChildren<TacticalGridOverlay>();
        Require(overlay != null, "TacticalGridOverlay was not created.");
        Require(overlay.Cells.Count == controller.width * controller.height,
                $"TacticalGridOverlay expected {controller.width * controller.height} cells, got {overlay.Cells.Count}.");

        Tilemap ground = GameObject.Find("Tilemap_Ground").GetComponent<Tilemap>();
        Tilemap road = GameObject.Find("Tilemap_Road").GetComponent<Tilemap>();
        Tilemap cliff = GameObject.Find("Tilemap_Cliff").GetComponent<Tilemap>();
        Tilemap water = GameObject.Find("Tilemap_Water").GetComponent<Tilemap>();
        Require(ground.GetUsedTilesCount() + road.GetUsedTilesCount() + cliff.GetUsedTilesCount() +
                water.GetUsedTilesCount() > 0, "No terrain tiles were assigned.");
        Require(UnityEngine.Object.FindObjectsByType<MapPropView>(FindObjectsSortMode.None).Length >= 6,
                "Expected generated interactable props to carry MapPropView metadata.");

        CleanupGeneratedChildren(controller.transform);
        Debug.Log("[BattleMapTilemapSmokeCheck] BattleTest Tilemap battlefield smoke check passed.");
    }

    private static void InvokePrivate(BattleTestController controller, string methodName)
    {
        MethodInfo method = typeof(BattleTestController).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (method == null)
        {
            throw new MissingMethodException(nameof(BattleTestController), methodName);
        }

        method.Invoke(controller, null);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new UnityException(message);
        }
    }

    private static void CleanupGeneratedChildren(Transform root)
    {
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            UnityEngine.Object.DestroyImmediate(root.GetChild(i).gameObject);
        }
    }
}
}
