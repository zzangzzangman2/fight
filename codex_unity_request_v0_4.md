# Codex 요청문: 조선 무협 SRPG v0.4를 Unity 2D Tilemap 프로젝트로 구현

아래 저장소의 현재 v0.3 HTML/UnityScaffold를 기준으로, 전투 방식을 고전 Fire Emblem식 SRPG로 전면 개편해주세요.

저장소: https://github.com/zzangzzangman2/fight

## 0. 가장 중요한 방향

현재 느낌은 “노드 위 카드 토큰”에 가까운데, 목표는 다음입니다.

```text
고전 Fire Emblem식 2D SRPG
- 플레이어 페이즈 / 적 페이즈
- 아군 유닛 클릭
- 이동 가능 칸 표시
- 이동
- 사거리 안 적 선택
- d20 공격 판정
- 반격 가능하면 자동 반격
- 행동 완료 유닛은 회색 처리
- 다음 유닛 선택
```

Unity에서는 격자 자체가 시스템상 존재해도 좋습니다. 단, 화면은 바둑판이 아니라 실제 2D 타일맵이어야 합니다.

## 1. Unity 패키지/기능

필수:

```text
Unity 2D
2D Tilemap Editor
C# ScriptableObject 데이터
URP 2D Renderer 선택 가능
Cinemachine 2D Camera
Timeline 컷신/절기 연출
Yarn Spinner 대화/퀘스트 플래그
Firebase AI Logic + Gemini API는 추후 전투 묘사/대사 생성용으로만 연결
```

## 2. 씬 구조

```text
BattleScene_AprokTemple
 ├─ Grid
 │   ├─ Tilemap_Ground        // 마당, 흙길, 물가, 대나무숲, 지붕
 │   ├─ Tilemap_Height        // 고지/벼랑/지붕 장식
 │   ├─ Tilemap_Collision     // 무너진 담장, 막힌 지형
 │   ├─ Tilemap_Props         // 등불, 향로, 술수레, 제단 종, 다리 밧줄
 │   └─ Tilemap_Highlights    // 이동/공격/지형/적 사정권 하이라이트
 ├─ UnitsRoot
 │   ├─ AllyUnits
 │   └─ EnemyUnits
 ├─ BattleUI
 │   ├─ PhaseBanner
 │   ├─ UnitStatusPanel
 │   ├─ CommandMenu
 │   ├─ BattleForecastPanel
 │   ├─ CombatLog
 │   └─ RosterPanel
 ├─ Cameras
 │   ├─ MainCamera
 │   └─ CinemachineVirtualCamera
 └─ BattleSystems
```

## 3. ScriptableObject 데이터

### TerrainTileData

```csharp
public enum TerrainTag { Road, Bamboo, Forest, Water, Bridge, Roof, Cliff, Ruin, Shrine, Fire, Ice, Cover, High }

[CreateAssetMenu(menuName = "JoseonMurim/Terrain Tile Data")]
public class TerrainTileData : ScriptableObject
{
    public string id;
    public string displayName;
    public int moveCost = 1;
    public int defenseBonus;
    public int avoidBonus;
    public int height;
    public bool walkable = true;
    public bool blocksLineOfSight;
    public List<TerrainTag> tags;
}
```

### UnitData

```csharp
public enum Faction { Ally, Enemy }

[CreateAssetMenu(menuName = "JoseonMurim/Unit Data")]
public class UnitData : ScriptableObject
{
    public string id;
    public string displayName;
    public string role;
    public Faction faction;
    public int level;
    public int maxHp;
    public int maxInner;
    public int guard;
    public int movement;
    public int morale;
    public UnitStats stats;
    public List<SkillData> skills;
    public Sprite placeholderSprite; // 실제 에셋 전에는 사람형 임시 스프라이트 사용
}

[System.Serializable]
public struct UnitStats
{
    public int strength;
    public int agility;
    public int innerPower;
    public int spirit;
    public int insight;
    public int charm;
}
```

### SkillData

```csharp
public enum SkillType { Attack, Social, Debuff, Heal, Rally, Self, TerrainSkill, Aoe }
public enum SkillTag { Sword, Palm, Needle, Poison, Ice, Counter, Ranged, Melee, Formation, Qinggong, Nonlethal }

[CreateAssetMenu(menuName = "JoseonMurim/Skill Data")]
public class SkillData : ScriptableObject
{
    public string id;
    public string displayName;
    public SkillType type;
    public List<SkillTag> tags;
    public int rangeMin;
    public int rangeMax;
    public string damageDice; // "1d8", "2d6"
    public StatKey attackStat;
    public int hitBonus;
    public int breakGain;
    public int innerCost;
    public int maxUses; // -1이면 무제한
    public int cooldown;
    public bool canCounter;
    public bool reactionOnly;
    public int push;
    public StatusEffectData statusOnHit;
}
```

## 4. 런타임 클래스

```text
BattleState
- 현재 phase, round, selectedUnit, selectedSkill, map 상태, combat log

BattleUnit
- UnitData 참조
- 현재 HP/내공/기세/파훼/상태이상/남은 사용횟수/쿨다운/acted

PhaseTurnController
- StartPlayerPhase()
- EndPlayerPhase()
- StartEnemyPhase()
- EndEnemyPhase()

UnitSelectionController
- 클릭한 아군 선택
- 선택 유닛 이동 범위 표시
- 이동 후 CommandMenu 표시

MovementRangeService
- BFS로 이동 가능 칸 계산
- 이동비용, 고저차, 물가, 대나무숲, 막힌 지형, 점유 유닛 반영

AttackRangeService
- 선택 무공의 사거리 표시
- 이동 전/이동 후 공격 가능 대상 계산

CombatResolver
- d20 공격 판정
- 명중/대성공/대실패
- 피해 주사위
- 지형 방어/회피/고저차/엄폐/상태이상 보정
- 파훼/기세 갱신

CounterResolver
- 피격자가 생존했고 공격자가 피격자의 무공 사거리 안이면 자동 반격
- 반격 가능 무공 중 reactionOnly 우선, 없으면 기본 반격 무공 사용

TerrainInteractionSystem
- 등불, 향로, 술수레, 다리 밧줄, 대나무 덫, 사당 종
- 전부 d20 체크 사용

BattleForecastPresenter
- 공격 전 예측 UI
- d20 보너스, 방어 목표값, 예상 피해, 반격 여부 표시

EnemyPhaseAI
- 가장 가까운 아군 추적
- 현재 위치에서 공격 가능하면 공격
- 아니면 이동 후 공격 가능한 칸 탐색
- 지형 보정이 좋은 칸 선호
```

## 5. 전투 흐름

```text
Player Phase 시작
  모든 아군 acted=false
  유닛 클릭
  MovementRangeService가 파란 칸 표시
  이동 칸 클릭
  CommandMenu 표시
    - 무공
    - 지형 사용
    - 대기
  무공 선택 시 빨간 사거리 표시
  적 클릭 시 BattleForecast 표시 후 공격 실행
  CombatResolver 실행
  CounterResolver 실행
  추격 가능하면 추가 공격
  유닛 acted=true
  모든 아군 acted면 End Phase 가능
Enemy Phase 시작
  각 적이 순서대로 이동/공격
  끝나면 round++ 후 Player Phase
```

## 6. d20 판정식

```text
공격 합계 = d20 + 능력치 보정 + 숙련 + 무공 명중 보정 + 지형/상태 보정
방어 목표 = 대상 Guard + 대상 타일 방어/회피 + 상태 방어 보정

natural 20 = 대성공, 피해 주사위 2배 + 기세 상승 + 파훼 추가
natural 1 = 대실패, 공격자 기세 하락 + 노출 상태
```

이점/불리:

```text
기회 = d20 두 번 굴려 높은 값
불리 = d20 두 번 굴려 낮은 값
```

기회 조건 예시:

```text
고지에서 저지대 공격
대상이 파훼/간파/노출
은신 상태의 암기
물가 대상에게 빙공
```

불리 조건 예시:

```text
연막 너머 원거리 공격
저지대에서 지붕/벼랑 근접 공격
둔화 상태
강엄폐 대상 원거리 공격
```

## 7. 반격 시스템

반격은 필수입니다.

```text
공격자가 공격
대상 생존
대상에게 canCounter=true인 무공 존재
공격자가 대상 무공의 rangeMin~rangeMax 안에 있음
대상 내공/사용횟수 조건 충족
→ 자동 반격
```

예시:

```text
박성준이 1칸에서 사군자검 사용
청성검수는 중원정도검 range 1 보유
→ 청성검수 생존 시 반격

한비연이 3칸에서 비화독침 사용
청성검수는 range 1 검만 보유
→ 반격 불가

사천궁수가 4칸에서 관통수전 사용
한비연은 비화독침 range 2~4 보유
→ 한비연 생존 시 원거리 반격 가능
```

## 8. 캐릭터별 무공

아군은 모두 성인 캐릭터이며, 실제 에셋 전에는 사람형 placeholder sprite를 사용합니다.

### 박성준

```text
역할: 해동문 문주 / 풍류검 / 지휘
무공:
- 사군자검: 근접 기본 검법, 무제한, 반격 가능
- 농월풍류: 1~3칸 심리전, 2회, 실패 시 박성준 기세 감소
- 문주호령: 아군 기세 회복, 1전투 1회
- 태평농월보: 이동 +2, 경공 상태, 2회
```

박성준은 호색한/풍류 문주 기믹이 있지만, 전투에서는 노골적 성적 묘사가 아니라 허세/도발/심리전/동료 승인도 리스크로 처리합니다.

### 윤서화

```text
역할: 예검 반격수
무공:
- 월하반조검: 근접 검법, 파훼 높음, 반격 가능
- 검로재기: 1~3칸 간파/파훼
- 예검반격: 반격 전용 무공
```

### 백련

```text
역할: 빙백심법 / 치유
무공:
- 빙백장: 1~2칸 빙공, 둔화
- 설화심법: 1~3칸 회복, 중독/화상 정화
- 한설빙로: 물가를 얼려 빠른 길로 변경
```

### 한비연

```text
역할: 암기 / 독공
무공:
- 비화독침: 2~4칸 독침, 원거리 반격 가능
- 흑립잠행: 이동 +2, 은신
- 만천화우: 2~4칸 범위 암기, 1전투 1회
```

### 도아린

```text
역할: 장법 / 비살상 제압
무공:
- 파산권: 근접 피해 + 1칸 밀치기
- 철산고: 큰 피해 + 2칸 밀치기
- 금나수: 비살상 제압, 무장해제/파훼
```

## 9. 지형

Unity Tilemap에는 최소 아래 지형을 만드세요.

```text
마당: 기본 지형
흙길: 이동비용 1
대나무숲: 이동비용 2, 방어 +1, 회피 +2, 한비연 이동비용 1
갈대숲: 이동비용 2, 회피 +2, 잠행 보너스
압록강 여울: 이동비용 3, 경공 실패 시 노출
나무다리: 이동비용 1, 병목
누각 지붕: 고도 1, 방어 +1, 원거리 유리
벼랑 능선: 고도 2, 방어 +2, 밀치기 낙하 위험
무너진 담장: 이동 불가, 시야/이동 차단
폐사당 제단: 고도 1, 기세 이벤트 가능
불붙은 잔해: 진입 시 화상/피해
얼어붙은 여울: 백련의 한설빙로로 생성
```

## 10. 지형지물 상호작용

```text
제단 향로: 통찰 DC 12, 연막 생성
붉은 등불: 민첩 DC 12, 화염 지대 생성
다리 밧줄: 근력 DC 13, 다리 일부 파괴
술수레: 근력 DC 11, 강엄폐 생성 + 인접 적 노출
휘어진 대나무: 민첩 DC 13, 덫 설치
사당 종: 정신 DC 12, 아군 기세 상승
```

## 11. UI 요구사항

```text
왼쪽/중앙: 2D Tilemap 전장
오른쪽: 선택 유닛 상태 + 명령 메뉴 + 전투 예측
하단 또는 별도 패널: 아군/적 로스터 + 전투 로그
```

공격 전 Battle Forecast는 반드시 표시합니다.

```text
공격자 / 무공
대상 / 거리
d20 모드: 일반/기회/불리
명중 보너스 합계
대상 방어 목표값
피해 주사위
반격 가능 여부와 반격 무공
```

## 12. 임시 에셋

실제 캐릭터 에셋은 나중에 뽑을 예정이므로 지금은 다음으로 처리하세요.

```text
- 동그라미 토큰 금지
- 사람형 실루엣 placeholder 사용
- 머리/몸통/팔/다리/무기 형태가 보이게 구성
- 이름표 표시
- 아군/적 색상 구분
- 행동 완료 시 회색 처리
- 전투불능 시 눕거나 흐려짐
```

## 13. AI 연결은 아직 Mock

Firebase AI Logic + Gemini API는 지금 전투 판정에 넣지 말고 Mock 인터페이스만 만드세요.

```csharp
public interface ICombatNarrationService
{
    Task<CombatNarrationResult> GenerateAsync(CombatNarrationRequest request);
}
```

실제 사용처:

```text
- 공격 결과 문장
- 반격 대사
- 지형 사용 묘사
- 전투 후 무림 소문
```

절대 AI가 피해량, 명중률, 승패를 결정하지 않게 하세요.

## 14. 완료 기준

1차 완료 기준:

```text
- Unity에서 14x10 압록강 폐사당 Tilemap 표시
- 사람형 임시 스프라이트 유닛 표시
- 아군 페이즈/적 페이즈 동작
- 아군 클릭 → 이동 범위 표시 → 이동
- 무공 선택 → 사거리 표시 → 적 클릭 → 전투 예측 → d20 공격
- 반격 자동 처리
- 무공별 사용횟수/내공/쿨다운 처리
- 지형 보정과 지형지물 3개 이상 동작
- 적 AI가 이동 후 공격
- 전투 종료 조건 작동
```

이 HTML 프로토타입의 `index.html`, `styles.css`, `app.js`를 참고해서 Unity C#으로 옮기면 됩니다.
