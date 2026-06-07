# UnityScaffold

`codex_unity_request_v0_3.md`의 요구사항을 Unity C#로 옮기기 위한 최소 프로젝트 골격입니다.

## 열기

Unity Hub에서 이 폴더를 프로젝트로 추가합니다.

```text
C:\Users\sjpark\Downloads\joseon-murim-tactics-work\joseon-murim-tactics-v0_3\UnityScaffold
```

## 들어 있는 것

- `CombatantData`, `SkillData`, `BattleMapData` ScriptableObject
- `BattleNodeData`, `InteractableObjectData` 데이터 구조
- `DiceRoller`, `ActionEconomy`, `TurnManager`
- `SkillResolver`, `TerrainResolver`, `MovementResolver`, `LineOfSightResolver`
- `CombatLog`, `BattleCameraDirector`, `TimelineCuePlayer`
- `CompanionApprovalSystem`
- `GeminiNarrationClientMock`

## 다음 작업

1. Unity에서 `Assets/Create/Joseon Murim Tactics` 메뉴로 전투원/무공/맵 데이터를 만든다.
2. `BattleMapData`에 압록강 폐사당 노드를 등록한다.
3. `TurnManager` 오브젝트를 씬에 만들고 전투원 데이터와 시작 노드를 연결한다.
4. 캐릭터 placeholder 프리팹을 노드 `worldPosition`에 배치한다.
5. `CombatLog`를 UI Text/TMP 패널에 연결한다.
6. Cinemachine 카메라와 Timeline 컷인을 실제 에셋으로 교체한다.
7. `GeminiNarrationClientMock`을 Firebase AI Logic 클라이언트로 교체하되, 명중/피해/승패 판정은 계속 C#에서만 처리한다.
