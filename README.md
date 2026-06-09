# 조선 무협 SRPG v0.9

> 현재 작업 브랜치: `noncombat-ui-v1.0` /
> 기준 브랜치: `story-start-v0.8` /
> 현재 게임 루프 버전: `v0.9` /
> 다음 목표: `v1.0` 비전투 UI / 허브 / 스토리 구조 정리

`main` 브랜치는 구버전 전투 프로토타입 기준일 수 있습니다. 비전투 UI, 허브, 저장/설정, 스토리 흐름 작업은 `story-start-v0.8`에서 파생된 `noncombat-ui-v1.0` 브랜치를 기준으로 확인하세요.

조선 문파들이 중원무림맹의 흡수와 동화 압박에 맞서 해동문을 중심으로 연합하는 Unity 기반 SRPG 게임 루프 프로토타입입니다.

이 저장소의 현재 목표는 단순한 “전투 테스트”가 아니라, 타이틀에서 시작해 도입부, 첫 선택, 튜토리얼 전투, 전투 결과, 해동문 허브로 이어지는 플레이 가능한 게임 루프를 검증하는 것입니다.

## 현재 구현된 씬

- `Boot`: GameRoot와 세션 서비스를 준비하고 타이틀로 진입합니다.
- `Title`: 새 게임, 이어하기, 전투 테스트, 설정, 종료 진입점입니다.
- `NewGameSetup`: 난이도, 문파명, 박성준 성향, 초기 무공을 고릅니다.
- `Prologue`: 중원 감찰단의 현판령과 박성준의 첫 선택지를 보여줍니다.
- `BattlePrep`: 첫 전투의 승패 조건, 보상, 전투 보정, 위험 정보를 확인합니다.
- `BattleTest`: 폐사당 방어전 전투 프로토타입입니다.
- `BattleResult`: 승패, 보상, 평판, 동료 승인도, 무림 소문을 정산합니다.
- `Hub_Pyesadang`: 전투 후 해동문 폐사당 거점에서 출정, 연무장, 동료, 문파, 객잔, 의원, 장터, 서고, 저장, 설정을 확인합니다.
- `MissionBoard`: 임무를 선택해 전투 준비로 이동합니다.
- `WorldMap`: 다음 장 확장 전 임시 월드맵 자리입니다.
- `CharacterAssetPreview`: 캐릭터 비주얼 확인용 개발 씬입니다.

## 실행 순서

Unity 6000.4 계열에서 `UnityScaffold` 프로젝트를 열고 `Boot` 씬을 실행합니다.

기본 루프:

```text
Boot
 → Title
 → NewGameSetup
 → Prologue
 → BattlePrep
 → BattleTest
 → BattleResult
 → Hub_Pyesadang
 → MissionBoard / BattlePrep / BattleTest ...
```

타이틀의 `전투 시험` 버튼은 스토리 도입 없이 전투 루프만 빠르게 확인하는 개발용 진입점입니다.

## 완료된 것

- GameRoot 기반 세션/저장/씬 전환 흐름
- 타이틀, 새 게임 설정, 프롤로그, 첫 선택지
- 중원 감찰단의 현판령과 조선 문파 연합 도입부
- 첫 전투 준비, 전투 결과, 해동문 허브 루프
- 임무 게시판과 첫 임무 흐름
- 캐릭터 비주얼 데이터와 2D 전장 표시
- d20 명중 판정, 반격, 추격, 무공 비용/쿨다운
- 지형지물 상호작용과 전투 예측 표시

## 아직 임시인 것

- BattleTest 전투 HUD는 Canvas/TextMeshPro 전환 중입니다.
- 전투 맵은 Unity 2D Tilemap 기반 전장으로 전환 중이며, diamond tile 렌더러는 디버그 fallback으로 유지합니다.
- 직업별 모션, 무기별 애니메이션, Timeline 전투 연출은 아직 제외했습니다.
- SkillData ScriptableObject와 BattleTest 전투 루프의 완전 연결은 이후 작업입니다.
- 월드맵과 설정 화면은 자리만 잡힌 상태입니다.
- 세이브 슬롯 UI, 사운드, 해상도 설정은 미구현입니다.

## Battle Map Tilemap Pipeline

- BattleTest now uses a Unity 2D Tilemap battlefield by default; the old per-cell diamond GameObject renderer is kept behind `useLegacyDiamondTerrain` for debug fallback.
- Author production maps under `UnityScaffold/Assets/JoseonMurimTactics/Art/BattleMaps`.
- Use `Joseon Murim Tactics > Battle Maps > Generate Tile Assets` to regenerate `TerrainTileData`, prop tiles, overlay tiles, and battle-map materials from `Resources/MapAssets`.
- Scene maps should attach `BattleMapTilemapBinder` to `Grid_BattleMap` and keep visual layers separate: Ground, Road, Water, Cliff, Decor, Props, Overlay, Highlight_Move, Highlight_Attack, Highlight_Danger.
- `TacticalGridOverlay` is the tactical source of truth for move cost, elevation, cover, line-of-sight blocking, fall, water, fire, smoke, objective, and lane data.
- Interactive map props should use `MapPropView` plus the needed tactical component: `CoverProvider`, `LineOfSightBlocker`, `DestructibleProp`, `InteractableProp`, or `MapLightAnchor`.
- Run `Joseon Murim Tactics > Validate Current Battle Map` before committing a map. The validator checks open-area ratio, lanes, chokepoints, elevation levels, interactables, high ground, line-of-sight blockers, destructible terrain, and start-to-objective pathing.

## 전투 방향

v0.9 전투는 고전 SRPG 감각을 목표로 합니다.

- 아군 페이즈에서 원하는 아군을 자유 순서로 행동시킵니다.
- 모든 아군이 행동하면 적 페이즈로 넘어갑니다.
- 공격 전 예측창에서 명중, 피해, 내공, 쿨다운, 지형 보정, 반격, 추격, 예상 HP를 확인합니다.
- 전투 로그는 짧은 피드백 중심으로 두고, 상세 d20 계산은 별도 상세 표시로 옮길 예정입니다.

## 장기 방향

- Canvas 기반 전투 UI 프리팹화
- TextMeshPro 한글 UI 정리
- 지형 목표와 위험 지형 경고 강화
- 다중 무공 슬롯과 직업/문파별 무공 성장
- 3세력 전투를 위한 NeutralPhase 확장
- 전투 연출, 카메라 포커스, 데미지 팝업, 파훼 팝업
