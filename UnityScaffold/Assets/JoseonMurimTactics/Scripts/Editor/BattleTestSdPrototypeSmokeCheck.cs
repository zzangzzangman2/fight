using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JoseonMurimTactics.Editor
{
public static class BattleTestSdPrototypeSmokeCheck
{
    private const string OriginalUnitId = "park_sungjun";
    private const string SdUnitId = "park_sungjun_sd_test";
    private const string SdVisualId = "park_sungjun_tactical_sd_v1";
    private const string PixelUnitId = "park_sungjun_pixel_test";
    private const string PixelVisualId = "park_sungjun_pixel";

    [MenuItem("Joseon Murim Tactics/Combat/Smoke Check BattleTest Protagonist Prototypes")]
    public static void Run()
    {
        EditorSceneManager.OpenScene(BattleTestSceneLauncher.ScenePath, OpenSceneMode.Single);
        BattleTestController controller = UnityEngine.Object.FindAnyObjectByType<BattleTestController>();
        Require(controller != null, "BattleTest scene must contain a BattleTestController.");

        RequirePrototypeSet(controller.unitDefinitions, "BattleTest scene");

        MethodInfo builder = typeof(BattleTestController).GetMethod(
            "BuildBaekduSnowGateUnitDefinitions", BindingFlags.NonPublic | BindingFlags.Static);
        Require(builder != null, "BattleTestController.BuildBaekduSnowGateUnitDefinitions must exist.");
        BattleTestUnitDefinition[] built =
            (BattleTestUnitDefinition[])builder.Invoke(null, new object[] { controller.unitDefinitions });
        RequirePrototypeSet(built, "Default BattleTest build path");

        Debug.Log("[BattleTestSdPrototypeSmokeCheck] BattleTest protagonist prototypes are wired into the scene.");
    }

    private static void RequirePrototypeSet(BattleTestUnitDefinition[] units, string context)
    {
        BattleTestUnitDefinition original = FindUnit(units, OriginalUnitId, out int allyCount);
        BattleTestUnitDefinition sd = FindUnit(units, SdUnitId, out _);
        BattleTestUnitDefinition pixel = FindUnit(units, PixelUnitId, out _);

        Require(allyCount >= 7, context + " must expose SD and pixel protagonists alongside the roster.");
        Require(original == null, context + " must not include the old high-fidelity park_sungjun unit.");
        Require(sd != null, context + " must include park_sungjun_sd_test.");
        Require(pixel != null, context + " must include park_sungjun_pixel_test.");
        RequirePrototypeVisual(sd, SdUnitId, SdVisualId);
        RequirePrototypeVisual(pixel, PixelUnitId, PixelVisualId);
    }

    private static BattleTestUnitDefinition FindUnit(BattleTestUnitDefinition[] units, string id, out int allyCount)
    {
        allyCount = 0;
        BattleTestUnitDefinition found = null;
        if (units == null)
        {
            return null;
        }

        foreach (BattleTestUnitDefinition unit in units)
        {
            if (unit == null)
            {
                continue;
            }

            if (unit.faction == Faction.Ally)
            {
                allyCount++;
            }

            if (string.Equals(unit.id, id, StringComparison.Ordinal))
            {
                found = unit;
            }
        }

        return found;
    }

    private static void RequirePrototypeVisual(BattleTestUnitDefinition prototype, string unitId, string visualId)
    {
        Require(prototype.faction == Faction.Ally, unitId + " must be an ally.");
        Require(prototype.visual != null, unitId + " must have CharacterVisualData.");
        Require(string.Equals(prototype.visual.visualId, visualId, StringComparison.Ordinal),
                unitId + " must use " + visualId + " visual data.");
        Require(prototype.visual.pixelSpriteMode, visualId + " must render in pixel sprite mode.");
        Require(prototype.visual.fullBodySprite != null || prototype.visual.idlePoseSprite != null,
                visualId + " must expose a visible board sprite.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
}
