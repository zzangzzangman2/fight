# Codex 요청문: 조선 무협 전술 RPG Unity MVP v0.3

아래 요구사항대로 Unity 2D 프로젝트의 전투 MVP를 만들어줘.

## 1. 개발 스택

- Unity 2D URP
- C#
- ScriptableObject 기반 데이터
- Unity Tilemap 또는 커스텀 NavNode 기반 전술 맵
- Cinemachine: 전투 카메라 이동/줌
- Timeline: 절기 컷인/지형 상호작용 연출 큐
- Yarn Spinner: 전투 전후 대화와 동료 승인도 이벤트
- Firebase AI Logic + Gemini API: 전투 묘사/대사/자유 대사 해석만 담당

Gemini는 데미지, 명중률, 승패, 보상을 결정하면 안 된다. 모든 판정은 Unity C# 코드가 한다.

## 2. 전투 콘셉트

발더스게이트식 파티 전술을 무협식으로 변환한다.

```text
파티 규모: 아군 5명 vs 적 5명 또는 적 다수
전투 방식: 턴제
판정: d20 필수
행동 경제: 주행동 1개 + 보조행동 1개 + 반응 1개 + 이동력
핵심 자원: 체력, 내공, 기세, 파훼, 이동력, 무공 사용횟수
핵심 맵 요소: 고저차, 엄폐, 물가, 연막, 화염, 낙하, 파괴 가능 지형지물
```

## 3. 절대 바둑판처럼 보이면 안 됨

내부적으로는 Tilemap/Grid/NavNode를 써도 된다. 하지만 화면에는 정사각 격자를 그대로 노출하지 말고, 실제 2D 전장처럼 보여야 한다.

권장 방식:

```text
- 배경: hand-painted/top-down 2D map 또는 임시 컬러 블록 맵
- 전술 노드: 디버그 모드에서만 표시
- 이동 가능 범위: 캐릭터를 선택했을 때만 부드러운 하이라이트로 표시
- 지형지물: 클릭 가능한 아이콘 또는 오브젝트 스프라이트로 표시
- 캐릭터: 지금은 에셋 자리 placeholder, 이름 텍스트 표시
```

## 4. 캐릭터와 무공 차별화

아군은 주인공 박성준 + 여성 동료 4명으로 시작한다.

### 박성준

- 역할: 해동문 문주, 풍류검, 심리전 지휘
- 기믹: 호색한/풍류남. 단, 전투 시스템에서는 노골적 성적 표현이 아니라 심리전/도발/호감도 리스크로 처리한다.
- 무공:
  - 사군자검: 기본 검법, 파훼 안정 누적
  - 농월추파: 보조행동 심리전, 적 기세 감소, 실패 시 동료 기세/승인도 하락
  - 태평농월보: 보조행동 경공, 이동력 증가, 기회 부여
  - 문주호령: 1전투 1회, 아군 전체 기세 회복 및 기회 부여

### 윤서화

- 역할: 예검 반격수
- 무공:
  - 월하반조검: 근접 반격검, 파훼 높음
  - 검로재기: 피해 대신 상대 초식 간파, 파훼 대량 누적
  - 예검반격 예약: 반응, 근접 공격을 받으면 피해 감소 및 공격자 파훼 증가

### 백련

- 역할: 빙백심법, 치유, 지형 제어
- 무공:
  - 빙백장: 원거리 빙공, 물가 대상에게 기회, 둔화
  - 설화심법: 아군 회복, 중독/화상 정화
  - 한설봉로: 지점을 빙판으로 바꾸어 이동 방해

### 한비연

- 역할: 암기, 독공, 잠행
- 무공:
  - 비화독침: 긴 사거리 독침, 중독
  - 흑립잠행: 보조행동 경공/은신, 다음 암기 공격 강화
  - 만천화우: 1전투 1회 범위 암기

### 도아린

- 역할: 권장, 장법, 비살상 제압
- 무공:
  - 파산권: 근접 피해, 밀치기
  - 철산고: 큰 파훼와 밀치기, 쿨다운
  - 금나수: 무장해제/제압, 비살상 승리 조건 보조

## 5. 지형지물 상호작용

맵에는 다음 지형지물이 있어야 한다.

```text
객잔 술상:
- 보조행동
- 걷어차서 강엄폐 생성
- 인접 적 넘어짐 판정

꺼져가는 향로:
- 주행동
- 연막 생성
- 강엄폐, 암기/잠행 연계

대나무숲:
- 주행동
- 베어서 시야 차단 또는 넘어짐 유발
- 한비연 암기/잠행과 시너지

흔들리는 등불:
- 주행동
- 화염 지대 생성
- 화상 상태 부여

낡은 나무다리:
- 주행동
- 파괴 시 통로 차단, 위의 유닛 여울로 추락

무너진 불상:
- 주행동
- 내공 판정으로 파편 폭발
- 주변 적 파훼 증가

압록강 여울:
- 물가 지형
- 이동 시 미끄러짐 판정
- 백련의 빙공 강화

누각 지붕/벼랑:
- 고지
- 원거리 명중 보너스
- 밀치기 성공 시 낙하 피해
```

## 6. d20 판정 규칙

```text
공격 판정:
d20 + 스탯 보정 + 숙련 + 무공 보정 + 지형/상태 보정 >= 대상 방어도

스킬 체크:
d20 + 스탯 보정 + 숙련 >= DC

기회:
- 높은 지형에서 낮은 지형 공격
- 은신 후 암기 공격
- 상대가 파훼/간파 상태
- 물가 대상에게 빙공 사용

불리:
- 강엄폐 대상 원거리 공격
- 미끄러운 물가에서 경공 아닌 행동
- 넘어짐/중독/연막 등 불리 상태
```

## 7. ScriptableObject 구조

필요한 데이터 클래스:

```csharp
CombatantData
- id, displayName, faction, role
- maxHp, maxInner, armorClass, movement
- strength, agility, innerPower, spirit, insight, charm
- List<SkillData> skills
- portraitPlaceholderName

SkillData
- id, displayName, actionSlot
- targetType, range
- stat, attackBonus, damageDice, healDice
- innerCost, cooldown, usesPerBattle
- breakGain, moraleDamage, pushDistance
- tags
- timelineCue

BattleMapData
- List<BattleNodeData> nodes
- List<InteractableObjectData> objects

BattleNodeData
- id, displayName, worldPosition
- terrainType, elevation, coverType, hazardType
- neighbors

InteractableObjectData
- id, displayName, nodeId
- requiredActionSlot, stat, dc
- effectType, timelineCue
```

## 8. 시스템 클래스

필요한 런타임 클래스:

```csharp
TurnManager
ActionEconomy
DiceRoller
SkillResolver
TerrainResolver
MovementResolver
LineOfSightResolver
CombatLog
BattleCameraDirector
TimelineCuePlayer
CompanionApprovalSystem
GeminiNarrationClientMock
```

## 9. UI 요구사항

- 오른쪽 패널: 현재 캐릭터 정보, 체력/내공/기세/파훼, 무공 카드 표시
- 하단 로그: d20 굴림, 피해, 파훼, 상태이상, AI 묘사 표시
- 전장: 캐릭터 placeholder와 이름 표시
- 지형지물: 클릭 가능한 아이콘 표시
- 디버그 버튼: 전술 노드 보기/숨기기

## 10. 우선 구현 순서

1. BattleNode 기반 이동과 거리 계산
2. d20 DiceRoller
3. 주행동/보조행동/반응 ActionEconomy
4. SkillData와 캐릭터별 무공 5명 구현
5. 지형지물 6개 상호작용 구현
6. 전투 로그
7. 적 자동 행동 간단 AI
8. TimelineCue/GeminiNarration은 Mock으로 로그만 출력
9. 이후 실제 Timeline, Firebase AI Logic, Gemini 구조화 출력 연결
