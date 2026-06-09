from __future__ import annotations

import datetime as dt
import json
from pathlib import Path


REPO = Path(__file__).resolve().parents[2]
ASSET_ROOT = REPO / "UnityScaffold" / "Assets" / "JoseonMurimTactics" / "Resources" / "MapAssets"


TILES = [
    ("plain_moss", "백두 이끼 평지", "Plain", "하단 진입로와 완만한 초지용 기본 타일"),
    ("hill_moss", "백두 능선 언덕", "Hill", "고저 차가 보이는 능선/언덕 타일"),
    ("stone_courtyard", "폐사당 석정 마당", "Stone", "중앙 마당과 중립 보행 지형"),
    ("road_stair", "중앙 돌계단 길", "Road", "중앙 병목 돌계단/진입로"),
    ("shrine_floor", "천광 사당 석단", "ShrineFloor", "목표 지점과 폐사당 고지용 타일"),
    ("bamboo_floor", "대숲 그림자 바닥", "Bamboo", "좌측 대나무숲 샛길"),
    ("forest_floor", "짙은 산림 바닥", "Forest", "시야를 가리는 숲 지형"),
    ("shallow_water", "압록 얕은 물결", "ShallowWater", "이동 부담이 있는 얕은 물"),
    ("deep_water", "검푸른 깊은 물", "DeepWater", "통행 제한이 강한 깊은 물"),
    ("wood_plank", "낡은 목판 바닥", "Wood", "누각/목재 지형"),
    ("wood_bridge", "낡은 나무다리", "Bridge", "끊거나 우회할 수 있는 다리"),
    ("roof_tile", "붉은 기와지붕", "Roof", "높은 지붕/누각 고지"),
    ("cliff_face", "검은 절벽면", "Cliff", "통행 불가 절벽/시야 차단"),
    ("wall_broken", "무너진 담장", "Wall", "폐사당 담장과 높은 벽"),
    ("rubble", "흩어진 잔해", "Rubble", "엄폐/이동 부담을 주는 잔해"),
    ("mud_path", "질척한 산길", "Mud", "비 온 뒤 산길/속도 저하 지형"),
    ("snow_edge", "백두 잔설 바닥", "Snow", "백두산 잔설 지형"),
    ("ice_slick", "서리 낀 얼음판", "Ice", "서리/빙결 전술 지형"),
    ("gate_threshold", "문루 문턱", "Gate", "문/입구/봉쇄 지형"),
    ("fire_scorch", "불길 그을림", "Fire", "화염이 남은 위험 지형"),
    ("smoke_veil", "연무 낀 바닥", "Smoke", "시야 차단 연무 지형"),
    ("trap_mark", "암기 함정 표식", "Trap", "함정/위험 표시 타일"),
]

OBJECTS = [
    ("sect_signboard", "백두천광 현판", "SectSignboard", "보호 목표 오브젝트"),
    ("incense_burner", "제단 향로", "IncenseBurner", "연막/기도 연출 오브젝트"),
    ("red_lantern", "붉은 등불", "Lantern", "화염 상호작용 오브젝트"),
    ("oil_jar", "기름항아리", "OilJar", "폭발/화염 전술 오브젝트"),
    ("wine_cart", "술수레", "WineCart", "이동 엄폐 오브젝트"),
    ("fallen_wall", "무너진 담장 조각", "FallenWall", "엄폐/시야 차단 오브젝트"),
    ("bridge_rope", "낡은 다리 밧줄", "BridgeRope", "다리 붕괴 상호작용 오브젝트"),
    ("bamboo_bundle", "대나무 묶음", "BambooBundle", "넘겨서 길목을 막는 오브젝트"),
    ("stone_lantern", "석등", "RockLantern", "낙석/파괴 연출 오브젝트"),
    ("falling_boulder", "낙석 바위", "Rockfall", "낙석 위험 표시"),
    ("flame_pillar", "솟는 화염", "Fire", "화염 상태 연출"),
    ("smoke_wisp", "흩어지는 연무", "Smoke", "연막 상태 연출"),
]

BAEKDU_SNOW_TILES = [
    ("baekdu_snow_plain", "백두 설원 평지", "Snow", "백두산 설산 기본 설원 타일"),
    ("baekdu_deep_snow", "백두 깊은 눈더미", "DeepSnow", "이동을 늦추는 깊은 적설 지형"),
    ("baekdu_wind_snow_ridge", "바람깎인 설릉", "SnowRidge", "고저차가 보이는 설산 능선"),
    ("baekdu_ice_slick", "백두 빙판", "Ice", "미끄러짐/빙결 전술용 얼음판"),
    ("baekdu_frozen_stream", "얼어붙은 얕은 계류", "FrozenStream", "얕은 물이 얼어붙은 지형"),
    ("baekdu_dark_frozen_water", "검푸른 결빙수", "FrozenDeepWater", "깊은 물이 얼어붙은 위험 지형"),
    ("baekdu_volcanic_snow_rock", "눈 덮인 화산암", "VolcanicRock", "백두 화산암과 눈이 섞인 지형"),
    ("baekdu_snow_basalt_cliff", "설산 현무암 절벽", "BasaltCliff", "통행 불가/시야 차단 절벽"),
    ("baekdu_snow_stone_courtyard", "눈 덮인 석정", "SnowStone", "사당 주변 석정 설원"),
    ("baekdu_snow_shrine_floor", "백두 설사당 석단", "SnowShrineFloor", "금문양이 희미한 설산 사당 바닥"),
    ("baekdu_frozen_stair_road", "얼어붙은 돌계단", "FrozenRoad", "빙결된 계단형 병목 지형"),
    ("baekdu_snow_mountain_pass", "설산 고갯길", "SnowPass", "갈색 흙길과 눈이 섞인 산길"),
    ("baekdu_snow_bamboo_floor", "눈 대숲 바닥", "SnowBamboo", "눈 덮인 대나무숲 지형"),
    ("baekdu_snow_pine_floor", "눈 소나무숲 바닥", "SnowPine", "소나무와 눈이 섞인 숲 지형"),
    ("baekdu_hot_spring_ground", "백두 온천 김 바닥", "HotSpring", "눈 속 온천과 증기 지형"),
    ("baekdu_cracked_ice_hazard", "갈라진 빙하 함정", "CrackedIce", "파괴/추락 위험이 있는 얼음 지형"),
]

BAEKDU_SNOW_OBJECTS = [
    ("baekdu_snow_pine", "눈 덮인 소나무", "SnowPine", "설산 숲/시야 차단 소품"),
    ("baekdu_snowdrift_cover", "큰 눈더미 엄폐", "SnowCover", "설산 전투용 엄폐 오브젝트"),
    ("baekdu_ice_crystal", "푸른 빙정 군락", "IceCrystal", "빙결 위험/시야 포인트 오브젝트"),
    ("baekdu_frozen_stone_lantern", "얼어붙은 석등", "FrozenLantern", "설산 사당 조명 오브젝트"),
    ("baekdu_broken_snow_gate", "무너진 설문", "SnowGate", "눈 덮인 사당 문루 잔해"),
    ("baekdu_frozen_rope_posts", "얼어붙은 밧줄 말뚝", "FrozenRope", "빙결된 다리/길목 오브젝트"),
    ("baekdu_snow_boulder", "눈 덮인 화산 바위", "SnowBoulder", "낙석/엄폐 겸용 바위"),
    ("baekdu_hot_spring_steam", "온천 증기", "HotSpringSteam", "시야를 흐리는 김/연무 효과"),
]


def tile_entry(stem: str, title: str, subtype: str, notes: str) -> dict:
    return {
        "id": f"map_tile_{stem}",
        "title": title,
        "category": "terrain",
        "subtype": subtype,
        "resourcePath": f"MapAssets/Tiles/{stem}",
        "previewUrl": f"/resources/MapAssets/Tiles/{stem}.png",
        "file": f"MapAssets/Tiles/{stem}.png",
        "tags": ["MAP", "tile", "murim", subtype],
        "notes": notes,
    }


def object_entry(stem: str, title: str, subtype: str, notes: str) -> dict:
    return {
        "id": f"map_object_{stem}",
        "title": title,
        "category": "object",
        "subtype": subtype,
        "resourcePath": f"MapAssets/Objects/{stem}",
        "previewUrl": f"/resources/MapAssets/Objects/{stem}.png",
        "file": f"MapAssets/Objects/{stem}.png",
        "tags": ["MAP", "object", "murim", subtype],
        "notes": notes,
    }


def main() -> None:
    assets = [tile_entry(*item) for item in TILES]
    assets.extend(object_entry(*item) for item in OBJECTS)
    assets.extend(tile_entry(*item) for item in BAEKDU_SNOW_TILES)
    assets.extend(object_entry(*item) for item in BAEKDU_SNOW_OBJECTS)
    catalog = {
        "version": 1,
        "updatedAt": dt.datetime.now(dt.UTC).replace(microsecond=0).isoformat().replace("+00:00", "Z"),
        "theme": "폐사당 고개 + 백두 설산 무협 MAP 에셋팩",
        "assets": assets,
    }
    ASSET_ROOT.mkdir(parents=True, exist_ok=True)
    (ASSET_ROOT / "map_asset_catalog.json").write_text(
        json.dumps(catalog, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )
    (Path(__file__).with_name("map-assets.js")).write_text(
        "window.JOSEON_MAP_ASSET_CATALOG = " + json.dumps(catalog, ensure_ascii=False, indent=2) + ";\n",
        encoding="utf-8",
    )


if __name__ == "__main__":
    main()
