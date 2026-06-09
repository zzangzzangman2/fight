from __future__ import annotations

import math
from collections import deque
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw

from build_map_asset_catalog import main as build_catalog


REPO = Path(__file__).resolve().parents[2]
ASSET_ROOT = REPO / "UnityScaffold" / "Assets" / "JoseonMurimTactics" / "Resources" / "MapAssets"
SOURCE_DIR = ASSET_ROOT / "Source"
TILE_DIR = ASSET_ROOT / "Tiles"
OBJECT_DIR = ASSET_ROOT / "Objects"
CONTACT_SHEET = Path(__file__).with_name("map_asset_contact_sheet.png")

TILE_STEMS = [
    "plain_moss",
    "hill_moss",
    "stone_courtyard",
    "road_stair",
    "shrine_floor",
    "bamboo_floor",
    "forest_floor",
    "shallow_water",
    "deep_water",
    "wood_plank",
    "wood_bridge",
    "roof_tile",
    "cliff_face",
    "wall_broken",
    "rubble",
    "mud_path",
    "snow_edge",
    "ice_slick",
    "gate_threshold",
    "fire_scorch",
    "smoke_veil",
    "trap_mark",
]

OBJECT_STEMS = [
    "sect_signboard",
    "incense_burner",
    "red_lantern",
    "oil_jar",
    "wine_cart",
    "fallen_wall",
    "bridge_rope",
    "bamboo_bundle",
    "stone_lantern",
    "falling_boulder",
    "flame_pillar",
    "smoke_wisp",
]

BAEKDU_SNOW_TILE_STEMS = [
    "baekdu_snow_plain",
    "baekdu_deep_snow",
    "baekdu_wind_snow_ridge",
    "baekdu_ice_slick",
    "baekdu_frozen_stream",
    "baekdu_dark_frozen_water",
    "baekdu_volcanic_snow_rock",
    "baekdu_snow_basalt_cliff",
    "baekdu_snow_stone_courtyard",
    "baekdu_snow_shrine_floor",
    "baekdu_frozen_stair_road",
    "baekdu_snow_mountain_pass",
    "baekdu_snow_bamboo_floor",
    "baekdu_snow_pine_floor",
    "baekdu_hot_spring_ground",
    "baekdu_cracked_ice_hazard",
]

BAEKDU_SNOW_OBJECT_STEMS = [
    "baekdu_snow_pine",
    "baekdu_snowdrift_cover",
    "baekdu_ice_crystal",
    "baekdu_frozen_stone_lantern",
    "baekdu_broken_snow_gate",
    "baekdu_frozen_rope_posts",
    "baekdu_snow_boulder",
    "baekdu_hot_spring_steam",
]


def is_key_green(pixel: tuple[int, int, int, int]) -> bool:
    r, g, b, a = pixel
    if a == 0:
        return True
    return g > 130 and g - max(r, b) > 58 and g > r * 1.42 and g > b * 1.42


def remove_green(image: Image.Image) -> Image.Image:
    image = image.convert("RGBA")
    pixels = image.load()
    width, height = image.size
    key = [[False] * width for _ in range(height)]

    for y in range(height):
        for x in range(width):
            key[y][x] = is_key_green(pixels[x, y])

    # Keep the background and inner green holes transparent, but avoid erasing natural moss.
    transparent = [[False] * width for _ in range(height)]
    queue: deque[tuple[int, int]] = deque()
    for x in range(width):
        for y in (0, height - 1):
            if key[y][x] and not transparent[y][x]:
                transparent[y][x] = True
                queue.append((x, y))
    for y in range(height):
        for x in (0, width - 1):
            if key[y][x] and not transparent[y][x]:
                transparent[y][x] = True
                queue.append((x, y))

    while queue:
        x, y = queue.popleft()
        for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
            if 0 <= nx < width and 0 <= ny < height and key[ny][nx] and not transparent[ny][nx]:
                transparent[ny][nx] = True
                queue.append((nx, ny))

    for y in range(height):
        for x in range(width):
            r, g, b, a = pixels[x, y]
            if transparent[y][x] or (key[y][x] and g > 185):
                pixels[x, y] = (r, g, b, 0)
                continue

            # Despill only likely chroma fringe, not ordinary foliage.
            if a > 0 and g > 118 and g - max(r, b) > 32 and g > r * 1.22 and g > b * 1.22:
                g = min(g, max(r, b) + 22)
                pixels[x, y] = (r, g, b, a)

    return image


def alpha_bbox(image: Image.Image) -> tuple[int, int, int, int]:
    alpha = image.getchannel("A")
    bbox = alpha.getbbox()
    if bbox is None:
        return (0, 0, image.width, image.height)
    left, top, right, bottom = bbox
    pad = max(10, min(image.width, image.height) // 30)
    return (
        max(0, left - pad),
        max(0, top - pad),
        min(image.width, right + pad),
        min(image.height, bottom + pad),
    )


def remove_stray_components(image: Image.Image) -> Image.Image:
    alpha = image.getchannel("A")
    pixels = alpha.load()
    width, height = image.size
    visited = [[False] * width for _ in range(height)]
    components: list[tuple[int, int, int, int, int, list[tuple[int, int]]]] = []

    for y in range(height):
        for x in range(width):
            if visited[y][x] or pixels[x, y] <= 8:
                continue

            queue: deque[tuple[int, int]] = deque([(x, y)])
            visited[y][x] = True
            points: list[tuple[int, int]] = []
            left = right = x
            top = bottom = y
            while queue:
                cx, cy = queue.popleft()
                points.append((cx, cy))
                left = min(left, cx)
                right = max(right, cx)
                top = min(top, cy)
                bottom = max(bottom, cy)
                for nx, ny in ((cx - 1, cy), (cx + 1, cy), (cx, cy - 1), (cx, cy + 1)):
                    if 0 <= nx < width and 0 <= ny < height and not visited[ny][nx] and pixels[nx, ny] > 8:
                        visited[ny][nx] = True
                        queue.append((nx, ny))

            components.append((len(points), left, top, right, bottom, points))

    if not components:
        return image

    largest = max(components, key=lambda item: item[0])
    largest_area, largest_left, largest_top, largest_right, largest_bottom, _ = largest
    keep: set[tuple[int, int]] = set()
    for area, left, top, right, bottom, points in components:
        touches_cell_edge = left <= 5 or top <= 5 or right >= width - 6 or bottom >= height - 6
        if touches_cell_edge and area < largest_area * 0.35:
            continue

        close_to_main = not (bottom < largest_top - 22 or top > largest_bottom + 22 or right < largest_left - 22 or left > largest_right + 22)
        meaningful = area >= max(90, largest_area * 0.006)
        if area == largest_area or meaningful or (area >= 32 and close_to_main):
            keep.update(points)

    cleaned = Image.new("RGBA", image.size, (0, 0, 0, 0))
    source = image.load()
    target = cleaned.load()
    for x, y in keep:
        target[x, y] = source[x, y]
    return cleaned


def fit_sprite(image: Image.Image, size: int, fill: float = 0.94, bottom_bias: int = 18) -> Image.Image:
    cleaned = remove_stray_components(image)
    crop = cleaned.crop(alpha_bbox(cleaned))
    if crop.width == 0 or crop.height == 0:
        return Image.new("RGBA", (size, size), (0, 0, 0, 0))

    max_width = int(size * fill)
    max_height = int(size * fill)
    scale = min(max_width / crop.width, max_height / crop.height)
    resized = crop.resize((max(1, round(crop.width * scale)), max(1, round(crop.height * scale))), Image.Resampling.LANCZOS)
    canvas = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    x = (size - resized.width) // 2
    y = max(0, size - resized.height - bottom_bias)
    canvas.alpha_composite(resized, (x, y))
    return canvas


def extract_grid(sheet_path: Path, columns: int, rows: int, stems: list[str], out_dir: Path, size: int, fill: float) -> None:
    sheet = Image.open(sheet_path).convert("RGBA")
    cell_width = sheet.width / columns
    cell_height = sheet.height / rows
    out_dir.mkdir(parents=True, exist_ok=True)

    for index, stem in enumerate(stems):
        col = index % columns
        row = index // columns
        crop = sheet.crop(
            (
                round(col * cell_width),
                round(row * cell_height),
                round((col + 1) * cell_width),
                round((row + 1) * cell_height),
            )
        )
        sprite = fit_sprite(remove_green(crop), size=size, fill=fill)
        sprite.save(out_dir / f"{stem}.png")


def build_contact_sheet() -> None:
    files = [
        TILE_DIR / "plain_moss.png",
        TILE_DIR / "hill_moss.png",
        TILE_DIR / "stone_courtyard.png",
        TILE_DIR / "road_stair.png",
        TILE_DIR / "shrine_floor.png",
        TILE_DIR / "bamboo_floor.png",
        TILE_DIR / "forest_floor.png",
        TILE_DIR / "shallow_water.png",
        TILE_DIR / "deep_water.png",
        TILE_DIR / "wood_plank.png",
        TILE_DIR / "wood_bridge.png",
        TILE_DIR / "roof_tile.png",
        TILE_DIR / "cliff_face.png",
        TILE_DIR / "wall_broken.png",
        TILE_DIR / "rubble.png",
        TILE_DIR / "mud_path.png",
        TILE_DIR / "snow_edge.png",
        TILE_DIR / "ice_slick.png",
        TILE_DIR / "gate_threshold.png",
        TILE_DIR / "fire_scorch.png",
        TILE_DIR / "smoke_veil.png",
        TILE_DIR / "trap_mark.png",
        OBJECT_DIR / "sect_signboard.png",
        OBJECT_DIR / "incense_burner.png",
        OBJECT_DIR / "red_lantern.png",
        OBJECT_DIR / "oil_jar.png",
        OBJECT_DIR / "wine_cart.png",
        OBJECT_DIR / "fallen_wall.png",
        OBJECT_DIR / "bridge_rope.png",
        OBJECT_DIR / "bamboo_bundle.png",
        OBJECT_DIR / "stone_lantern.png",
        OBJECT_DIR / "falling_boulder.png",
        OBJECT_DIR / "flame_pillar.png",
        OBJECT_DIR / "smoke_wisp.png",
    ]
    files.extend(TILE_DIR / f"{stem}.png" for stem in BAEKDU_SNOW_TILE_STEMS)
    files.extend(OBJECT_DIR / f"{stem}.png" for stem in BAEKDU_SNOW_OBJECT_STEMS)
    thumb = 128
    pad = 14
    cols = 6
    rows = math.ceil(len(files) / cols)
    sheet = Image.new("RGBA", (pad + cols * (thumb + pad), pad + rows * (thumb + 42)), (24, 20, 15, 255))
    draw = ImageDraw.Draw(sheet)
    for index, file in enumerate(files):
        x = pad + (index % cols) * (thumb + pad)
        y = pad + (index // cols) * (thumb + 42)
        draw.rounded_rectangle([x, y, x + thumb, y + thumb], radius=8, fill=(47, 39, 30, 255), outline=(184, 137, 53, 140), width=1)
        image = Image.open(file).convert("RGBA")
        image.thumbnail((thumb - 10, thumb - 10), Image.Resampling.LANCZOS)
        sheet.alpha_composite(image, (x + (thumb - image.width) // 2, y + (thumb - image.height) // 2))
        draw.text((x, y + thumb + 5), file.stem[:18], fill=(234, 220, 174, 255))
    sheet.convert("RGB").save(CONTACT_SHEET, quality=95)


def main() -> None:
    extract_grid(SOURCE_DIR / "map_tiles_illustration_sheet.png", 4, 6, TILE_STEMS, TILE_DIR, 512, 0.96)
    extract_grid(SOURCE_DIR / "map_objects_illustration_sheet.png", 4, 3, OBJECT_STEMS, OBJECT_DIR, 512, 0.92)
    extract_grid(SOURCE_DIR / "baekdu_snow_tiles_illustration_sheet.png", 4, 4, BAEKDU_SNOW_TILE_STEMS, TILE_DIR, 512, 0.96)
    extract_grid(SOURCE_DIR / "baekdu_snow_objects_illustration_sheet.png", 4, 2, BAEKDU_SNOW_OBJECT_STEMS, OBJECT_DIR, 512, 0.92)
    build_catalog()
    build_contact_sheet()


if __name__ == "__main__":
    main()
