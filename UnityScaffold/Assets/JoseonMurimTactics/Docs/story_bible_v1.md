# 조선 무협 SRPG Story Bible v1

작성 기준: 2026-06-14 피드백 v1.4

## 캐릭터 ID 기준

신규 데이터는 아래 canonical ID만 사용한다.

- `park_sungjun`: 박성준
- `baek_ryeon`: 백련
- `do_arin`: 도아린
- `jin_seoyul`: 진서율
- `shin_seoa`: 신서아
- `han_biyeon`: 한비연
- `yudalgeun`: 유달근

`seo_a`는 신서아의 과거 ID다. 신규 데이터에는 쓰지 않고, 구 세이브/구 에셋/구 장면을 읽기 위한 alias로만 둔다.

## 나이 기준

- 박성준: 17세 고정. 문서와 UI에서 18세, 20세 등 다른 나이로 표기하지 않는다.
- 백련: 15세
- 도아린: 15세
- 진서율: 15세
- 신서아: 15세
- 한비연: 15세

## 관계/로맨스 표현

동료 수치는 기존 기획대로 연애도/승인도/호감/유대 표현을 함께 사용할 수 있다.

- UI의 기본 게이지 명칭은 "연애도"를 유지한다.
- 선물, 방문, 선택지는 연애도 상승과 동료 반응을 줄 수 있다.
- `romanticIntent`는 로맨스/플러팅 선택지 태그로 유지한다.
- `seo_a` ID 정리와 별개로 로맨스 기획은 변경하지 않는다.

## 초상화 연결 기준

현재 런타임 `Resources.Load`로 바로 연결된 핵심 동료 초상화는 백련이다.

- `baek_ryeon_fullbody`: `Portraits/Companions/baek_ryeon/baek_ryeon_fullbody`

`Art/VisualUpgradeV1/Portraits`에 있는 박성준, 도아린, 진서율, 한비연 초상화는 아직 `Resources/Portraits/Companions`로 승격되지 않았다. 승격 시 GUID 중복을 피하기 위해 Unity import로 새 `.meta`를 생성한 뒤 manifest와 `PortraitRegistry`에 연결한다.
