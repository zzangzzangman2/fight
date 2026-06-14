# Codex 요청: 백두 설문 관문전 통행 격자 ↔ 페인트 그림 정렬

작성: 2026-06-14 / 전투 코드(Claude) ↔ 에셋(Codex)
증상: 전투에서 **그림상 벽/석조 관문/얼음강/숲/기둥** 위에도 유닛이 이동/배치됨.

## 원인 (확정)
- 통행 판정 메커니즘은 정상: `CanStandOnTile`이 `walkable=false`/`moveCost≥99`/`Wall`/`Cliff`/`DeepWater`를 막고,
  prop 차단(`IsBlockingInteractable`: Fire/Cover/Rockfall/BambooFall)은 칸을 막음.
- 문제는 이 맵의 **통행 가능 셀이 코드에 손으로 지정**돼 있는데, 그 격자가 페인트 그림과 어긋남.
  - 그림: `Resources/MapAssets/Backgrounds/baekdu_snow_gate_srpg_ground.png` (중앙 눈밭 광장 + 계단만 보행 가능, 상단 석조 관문 벽 / 좌측 얼음강 / 우측·가장자리 숲 / 횃불·깃대 기둥은 막힘)
  - 코드: `Scripts/Presentation/BattleTestController.cs`의
    - `IsBaekduSnowGatePaintedStandCell(x,y)` — 보행 가능 셀(아래 현재값)
    - `BaekduSnowGatePaintedBlockerProfile(x,y)` — 막힌 셀(물/숲/벽/절벽)
- 격자는 16×12 (x 0–15, y 0–11). 아이소 다이아몬드. `GridToWorld`/`tileWidth=1.16`/`tileHeight=0.62`.

## 현재 보행 가능 격자 (IsBaekduSnowGatePaintedStandCell)
```
y0,1: x 4–11
y2,3: x 4–12
y4  : x 5–10
y5  : x 6–11
y6  : x 7–12
y7  : x 8–12
y≥8 : 없음(전부 blocker)
배치 시작칸: (y0, x4–7), (y1, x4–5)
```

## 요청 (둘 중 택1)
### A) 권장: 현재 그림 기준 "보행 가능 셀 표" 산출 → Claude가 코드 반영 (그림 유지)
- 현재 `baekdu_snow_gate_srpg_ground.png` 위에 16×12 아이소 격자를 겹쳐, 각 (x,y)가
  **보행 가능(눈밭/석재 바닥/계단)인지 막힘(벽/석조 관문/얼음강/숲/횃불·깃대 기둥)인지** 표로 산출.
- 표 형식 예(행별): `y0: walk x4-10, block x11` / `y6: walk x8-11, block x7,x12` ...
- 격자 정렬 기준: 런타임 `GridToWorld`(tileWidth 1.16, tileHeight 0.62, 16×12)와
  `CreateMapBackdrop` 배경 배치. (Claude가 좌표계 상세 제공 가능)
- 이 표를 주면 Claude가 `IsBaekduSnowGatePaintedStandCell` + `BaekduSnowGatePaintedBlockerProfile`을
  그대로 다시 작성함. **그림은 안 바뀜.**

### B) 대안: 그림을 격자에 맞춰 재생성 (그림이 바뀜, 단일 소스 = 격자)
- 위 "현재 보행 가능 격자"에 정확히 눈밭/석재 바닥이 오고 그 외엔 벽/관문/얼음강/숲이 오도록 재페인트.
- 단점: 현재 잘 나온 그림을 다시 만들어야 함.

## 참고
- 다른 맵(BanditLair/WolfPass/Tiger/Leopard)은 `Resolve*Terrain`에 벽/blocker가 별도 함수로 박혀 있음 — 동일 정렬 점검 권장(이번 보고는 설문 관문전 기준).
