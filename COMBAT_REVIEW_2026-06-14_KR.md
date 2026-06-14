# 전투 시스템 분석 & 수정 리포트 (2026-06-14)

대상: `Assets/JoseonMurimTactics/Scripts/Presentation/BattleTestController.cs` (실제 플레이되는 전투 엔진)
작업: Claude(코드). 에셋 수정 필요 시 `codex-requests/`에 별도 md 작성.

---

## 0. 핵심 구조 파악

전투 엔진이 **두 개**로 갈라져 있음:

| 엔진 | 유닛 타입 | 턴 모델 | 실제 사용처 |
|------|-----------|---------|-------------|
| **`BattleTestController`** (10,495줄) | `BattleTestUnit` | 페이즈제(아군/적, `PhaseTurnController`) | ✅ **실제 플레이 전투** (HUD/카메라/런처가 전부 이걸 씀) |
| `TurnManager` + `SkillResolver` + `CombatantRuntime` | `CombatantRuntime` | 이니셔티브 순서제 | ❌ 런타임에서 **아무도 안 씀**. 에디터 스모크 테스트(`MurimStatIntegrationSmokeCheck`)와 내레이션만 참조 |

> 즉, 정교해 보이는 `SkillResolver`(주사위·LoS·지형 판정 676줄)는 **실 게임 전투에 연결돼 있지 않음**. 실제 전투 수치/판정은 전부 `BattleTestController` 안에 인라인으로 들어 있음. (→ 권장사항 R3)

---

## 1. 수정 완료 (코드)

### ✅ FIX-1 (높음): 적이 사거리/시야 밖에서도 공격하던 버그
- **위치:** `BattleTestController.RunEnemyPhase()`
- **증상:** 적 페이즈에서 이동 후 `RunEnemyActionCommand`를 **무조건** 호출. 이 경로의 기본 공격(`ExecuteAttackSequence`→`RollAttackResult`)은 **사거리/시야를 전혀 재확인하지 않음**. 그래서 근접 적이 이동으로 사거리 안에 못 들어가도 **맵 건너편에서, 담장/연막 너머로 그냥 때림**. (플레이어의 `TryAttack`은 사거리·LoS를 검사하므로 적만 무법 상태 = 불공정 + 명중률/위협 예측이 무의미해짐)
- **수정:** 행동 직전 플레이어와 **동일한** 사거리·시야 게이트 추가. 못 닿으면 헛스윙 대신 접근만 하고 대기 + 로그 `[적] … 사거리 밖 — 대기.`

```csharp
int actionRange = useSpecial ? EffectiveSpecialRange(activeUnit) : EffectiveAttackRange(activeUnit);
bool needsLineOfSight = actionRange > 1 &&
                        (!useSpecial || IsHostileAttackSpecial(activeUnit.definition.specialEffect));
bool canReachTarget = GridDistance(activeUnit.cell, target.cell) <= actionRange &&
                      (!needsLineOfSight || HasLineOfSight(activeUnit.cell, target.cell));
if (canReachTarget) { yield return RunEnemyActionCommand(activeUnit, target, useSpecial); }
else { activeUnit.view.PlayWait(); activeUnit.SpendMainAction(); AddLog(...); }
```

### ✅ FIX-2 (중간): 위협 범위 오버레이(Tab)가 적 이동을 무시
- **위치:** `BattleTestController.IsInEnemyThreat()` → `RebuildEnemyThreatCells()` / `AddThreatFromStandCell()` 신규
- **증상:** 위협 오버레이가 적의 **현재 칸 + 공격 사거리**만 빨갛게 칠함. 적의 **이동 사거리를 포함하지 않아** 실제 위험(이동+공격 = 최대 이동5+사거리1=6칸)을 크게 과소표시. FIX-1로 적이 "이동 후 공격"을 제대로 하게 되면서 이 과소표시가 더 위험해짐.
- **수정:** `RefreshHighlights` 시 적별로 도달 가능 칸(BFS)을 구하고, 각 정지 칸에서 닿는 공격 사거리(현재 칸은 무공/반격기 사거리도) 셀을 LoS 검사해 위협셋에 누적. `IsInEnemyThreat`는 셋 조회로 단순화. 거리 메트릭(맨해튼)은 실제 공격 판정과 동일하게 맞춤. (이벤트성 호출이라 성능 영향 無)

---

## 2. 검증

- ✅ **컴파일 클린:** Unity 6000.4.9f1 batchmode, `error CS` 0건 (`work/compile-check.log`)
- ✅ **빌드 성공:** `BuildWindowsBattleTest`, 신규 `Assembly-CSharp.dll`(2026-06-14 09:51) (`work/build-battletest.log`)
- ✅ **런타임 무예외:** 캡처 실행(`work/battle-shots/01~04.png`) 중 Player.log **예외 0건**, 위협 오버레이 토글 정상 동작
- ⚠️ 위협 영역의 "넓어진 빨간 칸"은 전술 카메라가 중앙 아군에 줌인돼 상단 적 부근이 프레임 밖이라 스크린샷엔 안 잡힘(코드 경로는 무예외 실행 확인). 적 페이즈 "사거리 밖 대기" 로그는 캡처가 아군 페이즈만 찍어 미수집.

---

## 3. 남은 권장사항 (미적용 — 결정 필요)

| ID | 우선 | 내용 | 성격 |
|----|------|------|------|
| **R1** | 높음 | **적 AI 깊이**: 현재 `FindNearestEnemy`로 **가장 가까운 적만** 노림. HP/처치각/위협 무시, 아군 치유·방어·후퇴·지형지물 사용 안 함. 처치 가능 대상 우선, 위험 시 후퇴, 힐러 적의 아군 치유 등 추가 가능 | 코드(나) |
| **R2** | 중간 | **불 위에서 이동 사망 처리**: 적이 불칸 진입 시 `ApplyTileEntry`로 죽어도 `AnimateMove`가 시체를 걷게 하고 `CheckBattleEnd`가 지연됨. 이동 직후 사망 가드 | 코드(나) |
| **R3** | 중간 | **죽은 전투 엔진 정리**: `TurnManager`/`SkillResolver`/`CombatantRuntime`/리졸버 체인이 실 게임 미사용. 통합하거나 명시적으로 제거(스모크 테스트 의존성 처리 필요) | 코드(나) |
| **R4** | 낮음 | **고정 RNG 시드**: `random = new System.Random(20260608)` 고정 + `BuildBattle`에서 리시드 안 함 → 게임 첫 실행 첫 전투의 주사위가 매번 동일. 전투 시작 시 리시드하면 변화↑ (단, 결정론을 의도했다면 유지) | 코드(나) |
| **R5** | 낮음 | **위협 오버레이 가독성**: 빨강 tint(alpha 0.30)가 밝은 설원 위에서 약함 + 이동 미리보기 중엔 억제됨. 대비 강화/이동 중에도 표시 검토 | 코드(색상 상수) |

---

## 4. 에셋 영향

이번 FIX-1/FIX-2 및 위 R1~R5는 **전부 코드 영역** → **Codex(에셋) 요청 불필요**.
추후 전투에서 스프라이트/이펙트/맵 에셋 수정이 필요해지면 `codex-requests/`에 요청 md를 추가함.
