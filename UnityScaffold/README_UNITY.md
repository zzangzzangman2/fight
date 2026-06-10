# UnityScaffold

`codex_unity_request_v0_4.md`의 요구사항을 Unity C#로 옮기기 위한 프로젝트 골격입니다.

## 열기

Unity Hub에서 이 폴더를 프로젝트로 추가합니다.

```text
C:\Users\sjpark\Downloads\joseon-murim-tactics\UnityScaffold
```

## 들어 있는 것

- `CombatantData`, `SkillData`, `BattleMapData` ScriptableObject
- `BattleNodeData`, `InteractableObjectData` 데이터 구조
- `DiceRoller`, `ActionEconomy`, `TurnManager`
- `SkillResolver`, `TerrainResolver`, `MovementResolver`, `LineOfSightResolver`
- `CombatLog`, `BattleCameraDirector`, `TimelineCuePlayer`
- `CharacterVisualData`, `CharacterVisualController`
- `Art/Characters`의 고급 2D 성인 무협 캐릭터 스프라이트
- `CompanionApprovalSystem`
- `GeminiNarrationClientMock`

## 캐릭터 에셋

현재 전투 테스트는 SchoolCombat 캐릭터 시각 에셋만 사용합니다.

```text
Assets/JoseonMurimTactics/Art/Characters
 ├─ Sprites/SchoolCombatIndividuals/*.png
 └─ VisualData/SchoolCombat/*_visual.asset
```

각 유닛은 `CharacterVisualData`를 통해 `CharacterVisualController`에 연결됩니다. 구 full-body preview 전용 씬과 스크립트는 더 이상 사용하지 않습니다.

## 다음 작업

1. Unity에서 `Assets/Create/Joseon Murim Tactics` 메뉴로 전투원/무공/맵 데이터를 만든다.
2. `BattleMapData`에 압록강 폐사당 노드를 등록한다.
3. `TurnManager` 오브젝트를 씬에 만들고 전투원 데이터와 시작 노드를 연결한다.
4. 캐릭터 프리팹에 `CharacterVisualController`를 붙이고 `CharacterVisualData.fullBodySprite`를 연결한다.
5. `CombatLog`를 UI Text/TMP 패널에 연결한다.
6. Cinemachine 카메라와 Timeline 컷인을 실제 에셋으로 교체한다.
7. `GeminiNarrationClientMock`을 Firebase AI Logic 클라이언트로 교체하되, 명중/피해/승패 판정은 계속 C#에서만 처리한다.
