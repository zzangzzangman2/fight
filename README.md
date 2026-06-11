# 조선 무협 SRPG v1.6 Pipeline Hardening

> 현재 통합 기준: `main` = `map-quality-v1.2` /
> 기준 흐름: `story-start-v0.8` → `noncombat-ui-v1.1` → `map-quality-v1.2` → `main` /
> 현재 게임 루프 버전: `v1.6 pipeline hardening` /
> 다음 목표: 대표 수작업 맵, 맵 검증, 콘텐츠 제작 파이프라인 안정화

`main` 브랜치는 새 채팅과 기본 QA가 참고하는 최신 통합 기준입니다. 최신 맵 파이프라인, 콘텐츠 편집기, Unity Resources 연동, BattleTest Tilemap 전장은 `main`과 `map-quality-v1.2`에 함께 유지합니다.

브랜치별 목적:

- `main`: 최신 전투/맵 통합 기준. 새 채팅, 기본 테스트, QA 기준으로 우선 확인합니다.
- `story-start-v0.8`: Boot부터 프롤로그까지의 시작 루프.
- `noncombat-ui-v1.1`: 타이틀, 새 게임, 허브, 저장/설정 등 비전투 UI 흐름.
- `map-quality-v1.2`: Tilemap 전장, 맵 에셋, 콘텐츠 편집기, 오프라인 기본값, palette refine을 검증하는 맵 QA 브랜치. `main`과 같은 최신 기준으로 유지합니다.

조선 문파들이 중원무림맹의 흡수와 동화 압박에 맞서 해동문을 중심으로 연합하는 Unity 기반 SRPG 게임 루프 프로토타입입니다.

이 저장소의 현재 목표는 단순한 “전투 테스트”가 아니라, 타이틀에서 시작해 도입부, 첫 선택, 튜토리얼 전투, 전투 결과, 허브로 이어지는 플레이 가능한 게임 루프와 제작용 MAP/대사 파이프라인을 함께 검증하는 것입니다.

최근 흐름:

- `e8cb8d1`: Tilemap 전장, 맵 에셋 파이프라인, Unity Resources 연동 기반 확장.
- `3282d01`: 콘텐츠 편집기 레이아웃과 palette refine.
- 현재 hardening: 압축된 코드 포맷 정리, 대표맵 지표 강화, 백두산 설산맵 높낮이/막힘 검증, README와 기본 브랜치 기준 최신화.

## 현재 구현된 씬

- `Boot`: GameRoot와 세션 서비스를 준비하고 타이틀로 진입합니다.
- `Title`: 새 게임, 이어하기, 전투 테스트, 설정, 종료 진입점입니다.
- `NewGameSetup`: 난이도, 문파명, 박성준 성향, 초기 무공을 고릅니다.
- `Prologue`: 중원 감찰단의 현판령과 박성준의 첫 선택지를 보여줍니다.
- `BattlePrep`: 첫 전투의 승패 조건, 보상, 전투 보정, 위험 정보를 확인합니다.
- `BattleTest`: `백두산 설문 관문전` 대표맵 전투 프로토타입입니다.
- `BattleResult`: 승패, 보상, 평판, 동료 승인도, 무림 소문을 정산합니다.
- `Hub_Pyesadang`: 전투 후 해동문 폐사당 거점에서 출정, 연무장, 동료(대화·선물), 장비(장착·강화), 문파, 객잔, 의원, 장터(보급품·선물·장비·재료), 서고, 저장, 설정을 확인합니다.
- `MissionBoard`: 임무를 선택해 전투 준비로 이동합니다.
- `WorldMap`: 다음 장 확장 전 임시 월드맵 자리입니다.

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
- 허브 경제 루프: 장터 4탭(보급품/선물/장비/재료), 동료 선물, 장비창, 강화, 전투 스탯 반영

## 허브 경제 / 선물 / 장비 (MVP)

설정: 박성준 18세, 동료 5인(백련·도아린·진서율·신서아·한비연)은 15세. 나이 데이터는 추후 계속 조정 예정이며, 연애 공략 가능 여부는 나이와 무관하게 항상 허용(`CanReceiveRomanticEffects` 상시 true). 선물은 연애도(승인도와 동일 게이지)를 올린다.

- 통화 표시는 "은냥", 내부 저장 키는 `silver` 유지.
- 선물: 동료 1명당 하루 1회(`intVars["gift:last_day:<id>"]`). 범용 +5, 일반 +3, 최애 선물 +8. 가격 18~28은냥.
- 장비: 무기/방어구/장신구 3슬롯. 장착 상태는 `stringVars["equip:<charId>:<slot>"]`, 강화 레벨은 `intVars["equip:level:<itemId>"]`(같은 itemId 공유). 같은 장비의 동시 장착 수는 보유 수량 이하.
- 장비 가격: 초급 70~90은냥, 중급(전용 무기/철린 조끼) 120~130은냥.
- 강화(+0~+5): +1 30냥 / +2 45냥+재료1 / +3 70냥+재료2 / +4 100냥+재료2 / +5 140냥+재료3. 재료: 무기 철광석, 방어구 질긴 천, 장신구 옥 조각(장터 12~20냥).
- 강화 효과: 무기 공+1/단계·명+1/2단계, 방어구 체+2/단계·방+1/2단계, 장신구 내공+1/단계.
- 전투 반영: `BattleTestController.ApplyEquipmentBonuses()`가 아군 유닛 생성 전에 `EquipmentService.BuildBonus()`를 더한다(체력/내공/명중/방어/피해/이동). 출격 준비 화면에 인원별 장비 요약 표시.
- 수입 기준: 자유시간 반복 의뢰 38~55은냥, 객잔 품팔이 +35, 순찰 +10 — 첫 의뢰 후 바로 선물 1~2개 또는 초급 장비 1개 구매 가능.
- 저장 호환: 구 세이브에 `stringVars`가 없어도 정상 로드(null 방어).

## 아직 임시인 것

- BattleTest 전투 HUD는 Canvas/TextMeshPro 전환 중입니다.
- 전투 맵은 Unity 2D Tilemap 기반 전장으로 전환 중이며, diamond tile 렌더러는 디버그 fallback으로 유지합니다.
- 직업별 모션, 무기별 애니메이션, Timeline 전투 연출은 아직 제외했습니다.
- SkillData ScriptableObject와 BattleTest 전투 루프의 완전 연결은 이후 작업입니다.
- 월드맵과 설정 화면은 자리만 잡힌 상태입니다.
- 세이브 슬롯 UI, 사운드, 해상도 설정은 미구현입니다.

## Battle Map Tilemap Pipeline

- BattleTest는 Unity 2D Tilemap 전장을 기본으로 사용합니다. 기존 per-cell diamond GameObject 렌더러는 `useLegacyDiamondTerrain` 디버그 fallback에서만 사용합니다.
- 제작 맵은 `UnityScaffold/Assets/JoseonMurimTactics/Art/BattleMaps` 아래에서 관리합니다.
- `Joseon Murim Tactics > Battle Maps > Generate Tile Assets`로 `Resources/MapAssets`의 Tiles/Objects를 `TerrainTileData`, prop tile, overlay tile, battle-map material로 재생성합니다.
- Scene map은 `Grid_BattleMap`에 `BattleMapTilemapBinder`를 붙이고 레이어를 분리합니다: Ground, Road, Water, Cliff, Decor, Props, Overlay, Highlight_Move, Highlight_Attack, Highlight_Danger.
- `TacticalGridOverlay`가 move cost, elevation, cover, line-of-sight block, fall, water, ice, fire, smoke, objective, lane 데이터의 source of truth입니다.
- Interactive map prop은 `MapPropView`와 필요한 전술 컴포넌트(`CoverProvider`, `LineOfSightBlocker`, `DestructibleProp`, `InteractableProp`, `MapLightAnchor`)를 함께 사용합니다.
- `Joseon Murim Tactics > Validate Current Battle Map`은 최소 2개 경로, 1~2칸 병목, 2단계 이상 고저차, 3개 이상 상호작용 프롭, 2개 이상 시야 차단 지형, 낙하/밀치기 지점, 양측 시작점-목표 도달 가능성, open-area warning을 검사합니다.

## MAP Authoring Tool

- 제작 도구는 `tools/content-authoring` 아래에만 둡니다. 게임 런타임 UI는 browser CSS에 의존하지 않습니다.
- 빠른 실행: `run-content-authoring.cmd` 또는 `node tools/content-authoring/server.js`.
- 서버 실행 모드는 저장 시 `UnityScaffold/Assets/JoseonMurimTactics/Resources/AuthoringContent/content_manifest.json`에 바로 반영합니다.
- `tools/content-authoring/index.html`을 file://로 직접 열면 `defaults.js` 기반 오프라인 미리보기를 사용하고, 저장 서버가 없을 때는 `content_manifest.json` 다운로드로 fallback합니다.
- Prologue는 `AuthoringContentManifest.LoadFromResources()`로 `chapter1_prologue`를 우선 로드하고, manifest가 없거나 비어 있으면 C# fallback 대사를 사용합니다.
- 나이는 표시용 메타데이터로만 사용합니다. `romanticIntent` 선택지 저장과 런타임 적용은 `romanceEligible=false` 캐릭터만 차단합니다.

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
