using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using JoseonMurimTactics.EditorTools;

namespace JoseonMurimTactics.Editor
{
// GPT 피드백 v1.4 P0-8: 한 번에 도는 프로젝트 스모크 체크 진입점.
// 사용 예: Unity.exe -batchmode -quit -nographics -projectPath UnityScaffold
//          -executeMethod JoseonMurimTactics.Editor.ProjectSmokeCheck.RunAll
//
// 핵심(core) 체크는 실패 시 예외를 던져 배치 종료코드에 반영한다.
// 베스트에포트 체크는 씬/에셋 상태(WIP)에 의존할 수 있어 로그만 남기고 종료코드엔 영향 주지 않는다.
public static class ProjectSmokeCheck
{
    [MenuItem("Joseon Murim Tactics/Progression/Run All Smoke Checks")]
    public static void RunAll()
    {
        int coreFailures = 0;

        // --- 핵심: 실패하면 빌드/CI를 막아야 하는 항목 ---
        coreFailures += Core("BuildSettings 씬 존재", VerifyBuildScenes);
        coreFailures += Core("전투 변형별 유닛 생성", VerifyBattleVariantGeneration);
        coreFailures += Core("무림 스탯 전투 통합", () => MurimStatIntegrationSmokeCheck.Run());
        coreFailures += Core("장비/성장 보너스 idempotency", () => BattleStatIdempotencySmokeCheck.Run());

        // --- 베스트에포트: 씬/에셋 상태에 의존, 로그만 남김 ---
        BestEffort("에셋 검증", () =>
        {
            if (!JoseonMurimAssetValidator.ValidateAssets(false))
            {
                throw new InvalidOperationException("에셋 검증에서 문제가 보고됨");
            }
        });
        BestEffort("VisualUpgradeV1 검증", () => VisualUpgradeV1Validator.ValidateVisualSetup());
        BestEffort("전투 타일맵 스모크", () => BattleMapTilemapSmokeCheck.Run());

        if (coreFailures > 0)
        {
            throw new InvalidOperationException(
                $"[ProjectSmokeCheck] 핵심 체크 {coreFailures}건 실패. 위 로그를 확인하세요.");
        }

        Debug.Log("[ProjectSmokeCheck] 모든 핵심 스모크 체크 통과.");
    }

    private static int Core(string name, Action step)
    {
        try
        {
            step();
            Debug.Log($"[ProjectSmokeCheck] PASS(core): {name}");
            return 0;
        }
        catch (Exception e)
        {
            Debug.LogError($"[ProjectSmokeCheck] FAIL(core): {name} — {e.Message}");
            return 1;
        }
    }

    private static void BestEffort(string name, Action step)
    {
        try
        {
            step();
            Debug.Log($"[ProjectSmokeCheck] PASS(best-effort): {name}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[ProjectSmokeCheck] WARN(best-effort): {name} — {e.Message}");
        }
    }

    private static void VerifyBuildScenes()
    {
        if (EditorBuildSettings.scenes == null || EditorBuildSettings.scenes.Length == 0)
        {
            throw new InvalidOperationException("BuildSettings에 씬이 하나도 없습니다.");
        }

        int enabled = 0;
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (scene != null && scene.enabled)
            {
                enabled++;
            }
        }

        if (enabled == 0)
        {
            throw new InvalidOperationException("BuildSettings에 활성화된 씬이 없습니다.");
        }
    }

    // 모든 전투 변형 빌더가 baseline에서 아군/적 유닛을 정상 생성하는지 확인한다.
    private static void VerifyBattleVariantGeneration()
    {
        BattleTestUnitDefinition[] baseline =
        {
            new BattleTestUnitDefinition { id = "park_sungjun", faction = Faction.Ally },
            new BattleTestUnitDefinition { id = "baek_ryeon", faction = Faction.Ally },
            new BattleTestUnitDefinition { id = "iron_wolf_guard_1", faction = Faction.Enemy }
        };

        string[] builders =
        {
            "BuildBaekduSnowGateUnitDefinitions",
            "BuildBanditLairUnitDefinitions",
            "BuildWolfPassUnitDefinitions",
            "BuildTigerRavineUnitDefinitions",
            "BuildLeopardCliffUnitDefinitions",
            "BuildSeorakPassRescueUnitDefinitions"
        };

        foreach (string builderName in builders)
        {
            MethodInfo builder = typeof(BattleTestController).GetMethod(
                builderName, BindingFlags.NonPublic | BindingFlags.Static);
            if (builder == null)
            {
                throw new InvalidOperationException($"빌더 메서드 없음: {builderName}");
            }

            BattleTestUnitDefinition[] result =
                (BattleTestUnitDefinition[])builder.Invoke(null, new object[] { baseline });
            if (result == null || result.Length == 0)
            {
                throw new InvalidOperationException($"{builderName} 결과가 비어 있습니다.");
            }

            bool hasAlly = false;
            bool hasEnemy = false;
            foreach (BattleTestUnitDefinition unit in result)
            {
                if (unit == null)
                {
                    continue;
                }

                if (unit.faction == Faction.Ally)
                {
                    hasAlly = true;
                }
                else if (unit.faction == Faction.Enemy)
                {
                    hasEnemy = true;
                }
            }

            if (!hasAlly || !hasEnemy)
            {
                throw new InvalidOperationException($"{builderName} 결과에 아군/적이 모두 있지 않습니다.");
            }
        }
    }
}
}
