# 백두산 설문 관문전 전투맵 재구성 QA

작성: 2026-06-14
대상 브랜치: `visual-qa-polish-v1.3`
기준 요청서: `C:\Users\sjpark\Downloads\battle_map_rebuild_work_request_v1_0.txt`

## 구현 요약

- `BattleMapRuntimeCatalog`와 `BaekduSnowGateBattleMapData`를 추가해 백두산 설문 관문전의 16x12 전투 셀을 비주얼과 분리된 런타임 정답 데이터로 정의했다.
- `BattlePathService`를 추가해 이동 가능 표시, 실제 이동, AI 이동 후보가 같은 이동 규칙을 쓰게 했다.
- `BattleTargetingService`를 추가해 기본 공격, 무공, 사거리, 고저, 시야, 예측 패널, AI 공격 판단이 같은 판정 경로를 타게 했다.
- `BattleMapDebugOverlay`를 추가해 F1-F6으로 walkable/elevation/LOS/cover/move cost/deploy zone을 확인할 수 있게 했다.
- `BattleMapAuthoringWindow`와 `BaekduSnowGateMapDataAssetGenerator`를 추가해 16x12 셀 데이터를 편집/생성할 수 있게 했다.
- `baekdu_snow_gate_data.asset`를 `Resources/BattleMaps`로 옮겨 런타임 source-of-truth로 사용하게 했다.
- `BattleMapDataQaTools`를 추가해 192칸 CSV export와 validator를 메뉴/배치 실행으로 남겼다.

## 에디터 툴 사용법

- Unity 메뉴: `Joseon Murim Tactics/Battle Maps/Battle Map Authoring Window`
- `New 16x12`로 새 맵을 만들고 셀을 클릭해 terrain/walkable/elevation/LOS/projectile/cover/deploy/tags를 페인팅한다.
- `Save Asset`으로 `BattleMapData` ScriptableObject를 저장한다.
- 백두산 설문 관문전 기본 데이터 재생성 메뉴: `Joseon Murim Tactics/Battle Maps/Generate Baekdu Snow Gate BattleMapData`

## 테스트 결과

| # | 테스트 | 결과 | 비고 |
|---|---|---|---|
| 1 | `git diff --check` | 성공 | whitespace 문제 없음 |
| 2 | `BattleMapTilemapSmokeCheck.Run` | 성공 | `BattleTest Tilemap battlefield smoke check passed` |
| 3 | 백두산 설문 관문전 주요 길 standable 검사 | 성공 | 하단 접근로, 계단, 상단 관문, 우측 눈길 |
| 4 | 그림상 벽/배경 blocker 검사 | 성공 | `(9,1)`, `(10,2)` 등 오픈 오류 수정 |
| 5 | 계단 이동 비용 검사 | 성공 | 단차 + ramp/stairs 태그 반영 |
| 6 | 벽 칸 이동 거부 검사 | 성공 | 공유 `BattlePathService.StepMoveCost` 사용 |
| 7 | 계단 인접 근접공격 검사 | 성공 | 공유 `BattleTargetingService.CanAttackFrom` 사용 |
| 8 | ScriptableObject 셀 개수 검사 | 성공 | 16x12 = 192개 cell |
| 9 | BattleMapData source-of-truth 검사 | 성공 | `source=BattleMapDataAsset cells=192` 로그 확인 |
| 10 | CSV export | 성공 | `codex-requests\2026-06-14_baekdu_snow_gate_cells.csv` |
| 11 | BattleMapData validator | 성공 | `codex-requests\2026-06-14_battle-map-rebuild-validator.md` PASS |
| 12 | 이동 후 facing 유지 / Undo facing 복구 smoke | 성공 | 월드 좌표 기준 마지막 이동 방향 유지 |
| 13 | 계단/벽/절벽/LOS/연기 공격 코너케이스 smoke | 성공 | `BattleTargetingService` 기준 |
| 14 | 최신 BattleTest 플레이어 빌드 | 성공 | `UnityScaffold\Builds\BattleTest\JoseonMurimTacticsBattleTest.exe` |
| 15 | 씬 파일 자동 재직렬화 정리 | 성공 | `BattleTest.unity` 변경 제외 |

## 남은 확인 항목

- 요청서의 실제 플레이 영상/스크린샷 4장 이상은 아직 별도 파일로 생성하지 않았다. 실행 파일에서 F1-F6 토글을 켠 뒤 walkable/elevation/LOS-cover/move cost/deploy zone 화면을 캡처하면 된다.
- 배경 이미지를 새로 생성하는 P2 작업은 이번 P0 로직 재구성 뒤로 미뤘다.
