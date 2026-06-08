# 조선 무협 SRPG v0.4

이 버전은 기존 v0.3의 “전술 노드/2D 전장” 방향을 버리고, 고전 Fire Emblem식 SRPG 흐름으로 다시 잡은 HTML 프로토타입입니다.

## 핵심 변경

- 아군 페이즈 / 적 페이즈 구조
- 아군 유닛 클릭 → 이동 가능 칸 표시 → 이동 → 무공/지형/대기 선택
- 행동 완료 유닛 회색 처리
- 사거리 안 적 선택 시 d20 공격 판정
- 반격 가능 무공이 있으면 자동 반격
- 민첩 차이가 크면 추격 공격
- 캐릭터마다 완전히 다른 무공 구성
- 무공별 내공 비용, 사용 횟수, 쿨다운
- 실제 2D 타일맵처럼 보이는 지형
- 사람형 임시 스프라이트 토큰
- 대나무숲, 지붕, 벼랑, 물가, 다리, 제단, 등불, 향로, 술수레, 대나무 덫 등 지형 활용

## 실행

브라우저에서 `index.html`을 열면 됩니다.

## Random Chat 엔진/비율 참고

`C:\Users\sjpark\Downloads\2d_map.mp4`와 Steam App `2244200 Random Chat` 정보를 기준으로 캐릭터와 맵 비율을 다시 맞췄습니다.

- 영상 해상도는 640x360, 16:9입니다.
- 맵은 학교/거리 배경의 쿼터뷰 2D 타일맵처럼 보입니다.
- 캐릭터는 타일 1칸 높이와 거의 같은 2.2~2.6등신 사람형 스프라이트 비율입니다.
- 현재 프로토타입도 타일 1칸 대비 캐릭터 높이를 약 1.0배로 맞췄습니다.
- 엔진은 Unity Engine으로 확인했습니다. SteamDB의 Random Chat 앱 정보와 depot 파일 목록에 Unity/URP/2D Animation/Tilemap 계열 런타임이 잡힙니다.
- 근거: Steam `https://store.steampowered.com/app/2244200/Random_Chat/`, SteamDB `https://steamdb.info/app/2244200/info/`, depot `https://steamdb.info/depot/2244201/`
- 구현 기준은 Unity 2D URP, 2D Tilemap/Grid, 2D Animation 또는 Spine 계열 캐릭터, SpriteShape/Tilemap 배경, Timeline/DOTween식 연출입니다.
- 원본 Random Chat의 저작권 에셋을 복제하지 않고, 엔진 구조와 화면 비율만 참고합니다.

## Unity 이전 방향

Unity에서는 다음 구조를 추천합니다.

```text
Grid
 ├─ Tilemap_Ground
 ├─ Tilemap_Height
 ├─ Tilemap_Props
 ├─ Tilemap_Highlight
 └─ UnitLayer

Core Scripts
 ├─ PhaseTurnController.cs
 ├─ UnitSelectionController.cs
 ├─ MovementRangeService.cs
 ├─ AttackRangeService.cs
 ├─ CombatResolver.cs
 ├─ CounterResolver.cs
 ├─ TerrainInteractionSystem.cs
 ├─ BattleForecastPresenter.cs
 └─ EnemyPhaseAI.cs
```

## 주인공/동료 방향

박성준은 성인 호색한/풍류 문주 기믹을 유지하되, 전투 시스템에서는 노골적 묘사가 아니라 심리전, 허세, 도발, 동료 승인도 리스크로 처리합니다. 여성 동료들은 모두 독립적인 성인 고수이며 각자 무공, 전투 역할, 개인 목표, 정치적 입장이 있습니다.
