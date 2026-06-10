# Test Swordsman 2D Vertical Slice

## 구현 요약

- 새 테스트 캐릭터 `test_swordsman`을 추가했다.
- 기준 이미지는 `C:\Users\godho\Downloads\배경제거.png`를 사용했다.
- 게임용 파생 스프라이트는 fullbody, bust, icon으로 분리했다.
- `CharacterVisualData`에 상태별 색과 모션 튜닝 값을 추가했다.
- `CharacterVisualController`는 단일 2D 스프라이트를 절차적으로 움직여 idle, selected, move, attack, skill, hit, guard, defeat, victory, wait 상태를 재생한다.
- `BattleTest.unity`와 `BattleTestSceneLauncher`의 첫 아군을 새 테스트 캐릭터 비주얼로 교체했다.

## 주요 파일

- `Assets/JoseonMurimTactics/Art/Characters/TestSwordsman/Source/test_swordsman_reference.png`
- `Assets/JoseonMurimTactics/Art/Characters/TestSwordsman/Sprites/test_swordsman_fullbody.png`
- `Assets/JoseonMurimTactics/Art/Characters/TestSwordsman/Portraits/test_swordsman_bust.png`
- `Assets/JoseonMurimTactics/Art/Characters/TestSwordsman/Portraits/test_swordsman_icon.png`
- `Assets/JoseonMurimTactics/Art/Characters/TestSwordsman/VisualData/test_swordsman_visual.asset`
- `Assets/JoseonMurimTactics/Art/Effects/TestSwordsman/`
- `Assets/JoseonMurimTactics/Scripts/Data/CharacterVisualData.cs`
- `Assets/JoseonMurimTactics/Scripts/Presentation/CharacterVisualController.cs`
- `Assets/JoseonMurimTactics/Scripts/Presentation/BattleTestController.cs`
- `Assets/JoseonMurimTactics/Scripts/Editor/BattleTestSceneLauncher.cs`
- `Assets/JoseonMurimTactics/Scenes/BattleTest.unity`

## 테스트 방법

1. Unity에서 `Assets/JoseonMurimTactics/Scenes/BattleTest.unity`를 연다.
2. Play를 누른다.
3. 첫 번째 아군 `청월검 소윤`이 새 SD 2D 캐릭터로 보이는지 확인한다.
4. 이동, 공격, 무공, 방어, 대기, 피격, 패배, 승리 상태를 전투 흐름에서 확인한다.
5. 메뉴 `Joseon Murim Tactics/Open Battle Test Scene`으로 씬을 다시 만들었을 때도 첫 아군이 같은 비주얼을 쓰는지 확인한다.

## 현재 한계

- 지금은 한 장의 fullbody 스프라이트를 코드로 흔들고 기울이는 방식이다.
- 팔, 머리, 검, 표정이 분리된 본 애니메이션은 아직 없다.
- `Art/Effects/TestSwordsman`에 래스터 이펙트 초안이 들어가 있지만, 런타임은 현재 코드 생성 이펙트를 우선 사용한다.
- 고품질 상용 수준으로 가려면 표정/포즈/공격 프레임/피격 프레임/패배 프레임을 별도 스프라이트로 추가하는 편이 좋다.

## 캐릭터 확장 방법

1. `Assets/JoseonMurimTactics/Art/Characters/<CharacterId>/` 아래에 `Source`, `Sprites`, `Portraits`, `VisualData` 폴더를 만든다.
2. 배경제거 fullbody PNG를 `Sprites`에 넣고, bust/icon 이미지를 `Portraits`에 둔다.
3. `CharacterVisualData` 에셋을 새로 만들고 fullbody, bust, portrait, face icon을 연결한다.
4. `heightInTiles`, `spriteOffset`, `shadowWidth`, `shadowHeight`로 보드 위 크기와 발 위치를 맞춘다.
5. `BattleTestUnitDefinition.visual`에 새 VisualData를 연결한다.
6. 고유 애니메이터가 필요해지면 `animatorController`를 채우고, 현재 절차 애니메이션은 기본 fallback으로 둔다.

## 멀티 프레임 애니메이션 (프레임 시트)

단일 포즈 대신 여러 장을 넣으면 프레임 애니메이션으로 자동 전환된다. 비워두면 기존 단일 포즈 + 절차 변형 그대로 동작한다.

- 파일 규칙: 기본 포즈 `{id}_{pose}.png`가 1프레임, 추가 프레임은 `{id}_{pose}_2.png`, `{id}_{pose}_3.png` … 최대 `_8`까지. 번호는 연속이어야 하며 빠진 번호에서 수집이 멈춘다.
  - 예: `park_sungjun_move.png`, `park_sungjun_move_2.png`, `park_sungjun_move_3.png`, `park_sungjun_move_4.png` → 4프레임 걷기 사이클.
- 지원 포즈: `idle`, `move`, `attack`, `skill`, `hit` (defeated/acted는 단일 포즈 유지).
- 메뉴 `Joseon Murim Tactics > Combat > Rebuild Six Character Team Assets`를 실행하면 프레임 파일을 자동 수집해 `CharacterVisualData.moveFrames` 등에 채운다.
- 재생 규칙:
  - `move` 프레임은 시간이 아니라 **보폭 위상**(타일당 1보, 발자국 데칼과 동일 위상)에 동기화된다 — 4~6프레임 권장.
  - `attack`/`skill`/`hit` 프레임은 무기 타임라인 진행도(선딜→타격→후딜)에 맞춰 1회 재생된다 — 3~4프레임 권장(중간 프레임이 타격 순간).
  - `idle` 프레임은 `idleFrameRate`(기본 4fps)로 순환한다.
- 모든 프레임은 같은 캔버스 크기/피벗으로 그려야 한다(발 위치 고정). 임포터 설정은 리빌드 메뉴가 기본 포즈와 동일하게 맞춘다.
- 의상(`CharacterOutfitData`)에도 같은 프레임 슬롯이 있어 의상별 모션 오버라이드가 가능하다.
