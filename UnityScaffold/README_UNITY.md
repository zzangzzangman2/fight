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

Random Chat 계열의 Unity 2D/VN급 캐릭터 문법을 기준으로, HTML placeholder가 아니라 Unity에서 바로 물릴 수 있는 full-body PNG를 추가했습니다.

```text
Assets/JoseonMurimTactics/Art/Characters
 ├─ Source/adult_murim_lineup_chroma.png
 ├─ Sprites/adult_murim_lineup_alpha.png
 └─ Sprites/Individuals/*_fullbody.png
```

각 유닛은 `CombatantData.visual`에 `CharacterVisualData`를 연결하고, 프리팹에는 `CharacterVisualController`를 붙입니다. 컨트롤러가 스프라이트 스케일, 그림자, 선택 링, 숨쉬는 idle 모션, y축 깊이 정렬을 처리합니다.

## 다음 작업

1. Unity에서 `Assets/Create/Joseon Murim Tactics` 메뉴로 전투원/무공/맵 데이터를 만든다.
2. `BattleMapData`에 압록강 폐사당 노드를 등록한다.
3. `TurnManager` 오브젝트를 씬에 만들고 전투원 데이터와 시작 노드를 연결한다.
4. 캐릭터 프리팹에 `CharacterVisualController`를 붙이고 `CharacterVisualData.fullBodySprite`를 연결한다.
5. `CombatLog`를 UI Text/TMP 패널에 연결한다.
6. Cinemachine 카메라와 Timeline 컷인을 실제 에셋으로 교체한다.
7. `GeminiNarrationClientMock`을 Firebase AI Logic 클라이언트로 교체하되, 명중/피해/승패 판정은 계속 C#에서만 처리한다.
