# 조선 무협 SRPG v0.9

중원 무림맹의 강압에 맞서 흩어진 조선 문파들이 해동문 박성준을 중심으로 뭉치는, 조선 무협 SRPG입니다.

이 버전은 더 이상 "전투 테스트 씬"이 아니라 **게임 루프 프로토타입**입니다.
타이틀에서 시작해 → 프롤로그 → 거점(허브) → 임무 선택 → 출격 준비 → 전투 → 결과 → 거점 복귀까지
하나의 흐름으로 이어집니다.

## 게임 흐름 (씬)

```
Boot → Title → NewGameSetup → Prologue
     → Hub_Pyesadang → MissionBoard → BattlePrep → BattleTest → BattleResult → Hub_Pyesadang
                                                                             ↘ WorldMap(다음 장 예고)
```

| 씬 | 역할 |
|---|---|
| `Boot` | 로고/로딩, 전역 GameRoot 초기화 → 타이틀 |
| `Title` | 새 게임 / 이어하기(저장 있을 때) / 설정 / 종료 |
| `NewGameSetup` | 난이도 · 문파명 · 박성준 성향 · 초기 무공 선택 |
| `Prologue` | 압록강 폐사당 — 위지강의 현판령, 4가지 성향 선택지 |
| `Hub_Pyesadang` | 해동문 거점: 출정/연무장/동료/문파/객잔/의원/장터/서고/저장/설정 |
| `MissionBoard` | 출정 임무 선택(적 정보·보상·위험 지형) |
| `BattlePrep` | 출격 준비: 승패 조건·보조 목표·인원·보상·맵 미리보기 |
| `BattleTest` | 전투 (기존 전투 씬 재사용) |
| `BattleResult` | 승/패·등급·보상·평판/승인도 변화·무림 소문 |
| `WorldMap` | 제1장 의주 객잔 예고(준비 중) |

## 실행

1. Unity **6000.3.0f1**(이상)로 `UnityScaffold` 프로젝트를 연다.
2. `Assets/JoseonMurimTactics/Scenes/Boot.unity`를 열고 Play.
   - 빌드 세팅에 `Boot`이 인덱스 0으로 등록되어 있어 빌드/플레이 모두 타이틀부터 시작한다.
3. 조작은 기본적으로 마우스(버튼)이며, 전투(BattleTest)는 키보드(1 이동 / 2 공격 / 3 무공 / 4 방어 / Space 턴 종료 / R 재시작)도 사용한다.

`index.html`은 초기 조작감 확인용 v0.4 HTML 프로토타입으로 참고용으로만 남겨둔다.

## 완료된 것 / 임시(placeholder)

**완료(게임 루프·시작부·허브):**
- 타이틀→프롤로그→허브→임무→출격 준비→전투→결과→복귀 전체 흐름
- GameRoot(세션·서비스 허브), 저장/로드(단일 자동 저장 슬롯), 스토리 플래그
- 동료 승인도 / 세력 평판 / 문파 기조(정책) / 무림 소문(Mock)
- 밝은 조선 무협풍 시작부 UI(한지/먹/남청/인장/금), 한글 표기

**임시/다음 단계:**
- 전투는 기존 `BattleTest`를 재사용하며 유닛/지형은 placeholder다. 전투 결과는 실제 승패를 읽어 결과 화면으로 넘긴다.
- 시작부 UI는 코드 기반 IMGUI다. 전투 HUD의 Canvas/TextMeshPro 생산형 UI 교체는 별도 전투 작업 트랙에서 진행 중이다.
- 캐릭터/배경 일러스트, 음성, 컷신은 아직 placeholder(이름/실루엣/색상)다.
- AI 내레이션(무림 소문/NPC 잡담)은 `MockAINarrationService` 고정 문장이며, 메인 진행/수치 판정에는 쓰지 않는다.

## 세계관 / 주인공·동료 방향

중원 무림맹의 강경 정파가 조선 문파들을 하위 분파로 흡수하려 하며 중원식 언어·예법·문파 등록을 강요한다.
약소한 조선 문파들은 해동문 박성준을 중심으로 연합을 시도한다.

박성준은 풍류·호색 기믹을 유지하되, 시스템에서는 노골적 묘사가 아니라 심리전·허세·도발·동료 승인도 리스크로 처리한다.
여성 동료(윤서화·백련·한비연·도아린·매화령·강초희)는 모두 독립적인 성인 고수로, 각자 무공·전투 역할·개인 목표·정치적 입장을 가진다.

## 코드 구조 (UnityScaffold)

- `Scripts/Campaign` — GameRoot, GameSession, SaveManager, QuestManager, BattleEntry/ResultBridge, Mission/Companion/Battle 카탈로그
- `Scripts/Story` — SceneFlow, StoryFlag(+상수), 성향/승인도/평판 서비스, Boot/Prologue
- `Scripts/UI` — UiTheme(무협 IMGUI 스킨) + Title/NewGameSetup/MissionBoard/BattlePrep/BattleResult/WorldMap
- `Scripts/Hub` — HubController
- `Scripts/Dialogue` — DialogueController/모델
- `Scripts/Data` — ScriptableObject(Companion/Chapter/Faction/Quest/BattleEntry/Mission)
- `Scripts/Presentation`, `Scripts/Runtime` — 전투(BattleTest) 계열

진행 브랜치: `story-start-v0.8` (시작부/허브/UI v0.8–v0.9 작업).
