# Codex 요청: 픽셀 전투 스프라이트 시트 (프로토타입 1명 = 진서율)

작성: 2026-06-14 / 전투 코드(Claude) ↔ 에셋(Codex)
목표: 둘째 참고 이미지 같은 **클래식 픽셀 SRPG 전투 유닛**을 1명(진서율)으로 먼저 만들어 전투에 넣고 룩/공정 검증.
(코드 측은 Claude가 `CharacterVisualData.pixelSpriteMode` + 픽셀 렌더 모드를 준비함. 아래 포맷에 맞춰 시트만 주면 됨.)

## 대상
- 캐릭터: **jin_seoyul (진서율)** 1명만. 검증 후 동일 포맷으로 나머지 5명 확장.
- 대상 비주얼 데이터: `Art/Characters/jin_seoyul/VisualData/jin_seoyul_visual.asset` (의 픽셀용 변형 또는 신규 `jin_seoyul_pixel_visual.asset`)

## 스타일
- 참고: 사용자가 준 둘째 이미지(연보라/녹색 톤 픽셀 아이소 SRPG). **제한 팔레트, 또렷한 실루엣, 약한 림라이트, 안티에일리어싱 없음(하드 픽셀)**.
- 무협 컨셉 유지: 진서율 = 경성 천뢰봉문, 검사. 흑/남색 무복 + 검.
- 톤: 게임 전장이 야간 설산이라 **약간 어두운 명도에서도 읽히는 외곽/대비**.

## 시트 규격 (★ 이 포맷에 정확히 맞춰주세요)
- **셀(프레임) 크기: 64 × 64 px**, 정사각. 발은 **셀 하단 중앙**(가로 중앙, 세로 하단에서 약 4px 위)에 정렬 — 모든 프레임 발 위치 동일.
- **단일 PNG 시트**, 격자 배열. **행 = 애니메이션, 열 = 프레임**:
  - row0 `idle` : 4프레임 (호흡/대기)
  - row1 `walk` : 6프레임
  - row2 `attack` : 6프레임 (베기, 타격은 4번째 프레임쯤)
  - row3 `skill` : 6프레임 (검기/무공)
  - row4 `hit` : 2프레임 (피격 움찔)
  - row5 `guard` : 2프레임 (방어 자세)
  - row6 `defeated` : 1프레임 (쓰러짐)
- 즉 시트 크기 = **가로 6×64=384 px, 세로 7×64=448 px** (빈 칸은 투명).
- **배경 완전 투명**, 프레임 간 캐릭터 크기/발높이 일관.
- 방향: **정면-측면 3/4 한 방향만**(좌우는 코드가 X-플립). 측면/후면은 검증 후 추가(지금 불필요).

## 임포트/배치 (Unity)
- PNG 임포트: **Sprite (2D), Sprite Mode = Multiple, Grid by Cell Size 64×64**, **Filter Mode = Point**, **Compression = None**, Pixels Per Unit는 기존 캐릭터와 맞게(대략 64 권장, Claude가 board fit 조정).
- 슬라이스된 스프라이트를 `CharacterVisualData`의 프레임 배열에 매핑:
  - `idleFrames` ← row0 4장, `moveFrames` ← row1 6장, `attackFrames` ← row2 6장,
    `skillFrames` ← row3 6장, `hitFrames` ← row4 2장
  - 단일 포즈: `idlePoseSprite`=idle[0], `attackPoseSprite`=attack[3], `skillPoseSprite`=skill[3],
    `hitPoseSprite`=hit[0], `defeatedPoseSprite`=defeated, `actedPoseSprite`=idle[0]
  - `idleFrameRate` = 4 (걷기는 코드가 이동 시간에 맞춤)
- **`pixelSpriteMode = true`** 로 설정(이 bool은 Claude가 `CharacterVisualData`에 추가함). 이게 켜지면 코드가 리빙2D 레이어(얼굴/의상/머리/그림자효과)와 모션 흔들림을 끄고 픽셀 프레임만 깔끔히 렌더.

## 산출물
1. `Art/Characters/jin_seoyul/Sprites/Pixel/jin_seoyul_pixel_sheet.png` (위 규격) + .meta(Point/Multiple/Grid64).
2. 슬라이스 스프라이트가 매핑되고 `pixelSpriteMode=true`인 `jin_seoyul_pixel_visual.asset`.
3. (선택) 대화용 고퀄 초상화는 **그대로 유지** — 픽셀은 전투 유닛에만.

## 참고
- 코드 측(Claude): `pixelSpriteMode`면 body 단일 스프라이트 + frame 배열 재생, 리빙2D 레이어/모션/블링크 비활성, Point 필터 강제. 기존 6명은 영향 없음(플래그 기본 false).
- 1명 OK 나오면 같은 시트 포맷으로 park_sungjun/han_biyeon/do_arin/shin_seoa/baek_ryeon + 적들 확장.
