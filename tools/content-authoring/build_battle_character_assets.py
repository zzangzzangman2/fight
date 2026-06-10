from __future__ import annotations

from dataclasses import dataclass
from math import hypot
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter


REPO_ROOT = Path(__file__).resolve().parents[2]
CHARACTER_ROOT = REPO_ROOT / "UnityScaffold" / "Assets" / "JoseonMurimTactics" / "Art" / "Characters"
CANVAS_SIZE = (512, 384)
FOOTLINE_Y = 352


@dataclass(frozen=True)
class CharacterSheet:
    character_id: str
    source_file: str


CHARACTERS = [
    CharacterSheet("baek_ryeon", "baek_ryeon_battle_sheet.png"),
    CharacterSheet("do_arin", "do_arin_battle_sheet.png"),
    CharacterSheet("jin_seoyul", "jin_seoyul_battle_sheet.png"),
    CharacterSheet("shin_seoa", "shin_seoa_battle_sheet.png"),
    CharacterSheet("han_biyeon", "han_biyeon_battle_sheet.png"),
    CharacterSheet("park_sungjun", "park_sungjun_battle_sheet.png"),
]

POSE_CELLS = {
    "idle": (0, 0),
    "move": (1, 0),
    "attack": (2, 0),
    "skill": (3, 0),
    "hit": (0, 1),
    "defeated": (1, 1),
    "acted": (2, 1),
}

POSE_BOX_OVERRIDES = {
    "idle": (0.00, 0.00, 0.26, 0.50),
    "move": (0.22, 0.00, 0.50, 0.50),
    "attack": (0.46, 0.00, 0.77, 0.50),
    "skill": (0.73, 0.00, 1.00, 0.50),
    "hit": (0.00, 0.50, 0.27, 0.96),
    "defeated": (0.24, 0.50, 0.60, 0.96),
    "acted": (0.52, 0.50, 0.80, 0.96),
}

CHARACTER_POSE_BOX_OVERRIDES = {
    ("do_arin", "acted"): (0.68, 0.50, 1.00, 0.96),
    ("han_biyeon", "acted"): (0.68, 0.50, 1.00, 0.96),
}

POSE_CENTERS = {
    "idle": (0.13, 0.31),
    "move": (0.36, 0.31),
    "attack": (0.60, 0.31),
    "skill": (0.84, 0.31),
    "hit": (0.13, 0.70),
    "defeated": (0.40, 0.70),
    "acted": (0.62, 0.70),
}

CHARACTER_POSE_CENTER_OVERRIDES = {
    ("do_arin", "acted"): (0.82, 0.70),
    ("han_biyeon", "acted"): (0.82, 0.70),
}

DISCARD_CENTERS = {
    "baek_ryeon": [(0.84, 0.70)],
    "jin_seoyul": [(0.84, 0.70)],
    "shin_seoa": [(0.84, 0.70)],
}

MAX_COMPONENT_DISTANCE = 360


def is_checker_background(pixel: tuple[int, int, int, int]) -> bool:
    r, g, b, alpha = pixel
    if alpha < 16:
        return True

    maximum = max(r, g, b)
    minimum = min(r, g, b)
    saturation = maximum - minimum
    return (maximum > 216 and saturation < 38) or (r > 236 and g > 236 and b > 236)


def remove_checker_background(image: Image.Image) -> Image.Image:
    image = image.convert("RGBA")
    width, height = image.size
    pixels = image.load()
    background = bytearray(width * height)
    stack: list[tuple[int, int]] = []

    for x in range(width):
        stack.append((x, 0))
        stack.append((x, height - 1))

    for y in range(height):
        stack.append((0, y))
        stack.append((width - 1, y))

    while stack:
        x, y = stack.pop()
        index = y * width + x
        if background[index] or not is_checker_background(pixels[x, y]):
            continue

        background[index] = 1
        for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
            if 0 <= nx < width and 0 <= ny < height and not background[ny * width + nx]:
                stack.append((nx, ny))

    for y in range(height):
        for x in range(width):
            if background[y * width + x]:
                pixels[x, y] = (0, 0, 0, 0)

    return image


def connected_components(image: Image.Image) -> list[dict[str, object]]:
    alpha = image.getchannel("A")
    width, height = alpha.size
    alpha_pixels = alpha.load()
    seen = bytearray(width * height)
    components: list[dict[str, object]] = []

    for y in range(height):
        for x in range(width):
            index = y * width + x
            if seen[index] or alpha_pixels[x, y] == 0:
                continue

            stack = [(x, y)]
            seen[index] = 1
            pixels: list[tuple[int, int]] = []
            min_x = max_x = x
            min_y = max_y = y

            while stack:
                cx, cy = stack.pop()
                pixels.append((cx, cy))
                min_x = min(min_x, cx)
                max_x = max(max_x, cx)
                min_y = min(min_y, cy)
                max_y = max(max_y, cy)

                for nx, ny in ((cx + 1, cy), (cx - 1, cy), (cx, cy + 1), (cx, cy - 1)):
                    if 0 <= nx < width and 0 <= ny < height:
                        n_index = ny * width + nx
                        if not seen[n_index] and alpha_pixels[nx, ny] > 0:
                            seen[n_index] = 1
                            stack.append((nx, ny))

            area = len(pixels)
            if area < 6:
                continue

            components.append({
                "pixels": pixels,
                "bbox": (min_x, min_y, max_x + 1, max_y + 1),
                "area": area,
                "center": ((min_x + max_x + 1) * 0.5, (min_y + max_y + 1) * 0.5),
            })

    return components


def pose_center(character_id: str, pose: str, sheet: Image.Image) -> tuple[float, float]:
    normalized = CHARACTER_POSE_CENTER_OVERRIDES.get((character_id, pose), POSE_CENTERS[pose])
    return sheet.width * normalized[0], sheet.height * normalized[1]


def assign_components(character_id: str, sheet: Image.Image) -> dict[str, Image.Image]:
    cleaned = remove_checker_background(sheet)
    components = connected_components(cleaned)
    pose_centers = {pose: pose_center(character_id, pose, cleaned) for pose in POSE_CELLS}
    candidate_centers = dict(pose_centers)
    for index, normalized in enumerate(DISCARD_CENTERS.get(character_id, [])):
        candidate_centers[f"_discard_{index}"] = (cleaned.width * normalized[0], cleaned.height * normalized[1])

    assigned: dict[str, list[dict[str, object]]] = {pose: [] for pose in POSE_CELLS}

    for component in components:
        cx, cy = component["center"]  # type: ignore[misc]
        nearest_pose = min(
            candidate_centers,
            key=lambda pose: hypot(cx - candidate_centers[pose][0], cy - candidate_centers[pose][1]),
        )
        distance = hypot(cx - candidate_centers[nearest_pose][0], cy - candidate_centers[nearest_pose][1])
        if not nearest_pose.startswith("_discard") and distance <= MAX_COMPONENT_DISTANCE:
            assigned[nearest_pose].append(component)

    sprites: dict[str, Image.Image] = {}
    source_pixels = cleaned.load()
    for pose, pose_components in assigned.items():
        pose_image = Image.new("RGBA", cleaned.size, (0, 0, 0, 0))
        pose_pixels = pose_image.load()
        for component in pose_components:
            for x, y in component["pixels"]:  # type: ignore[index]
                pose_pixels[x, y] = source_pixels[x, y]

        sprites[pose] = fit_to_battle_canvas(pose_image)

    return sprites


def fit_to_battle_canvas(image: Image.Image) -> Image.Image:
    bounds = image.getchannel("A").getbbox()
    output = Image.new("RGBA", CANVAS_SIZE, (0, 0, 0, 0))
    if bounds is None:
        return output

    cropped = image.crop(bounds)
    scale = min(470 / cropped.width, 310 / cropped.height)
    resized_size = (max(1, round(cropped.width * scale)), max(1, round(cropped.height * scale)))
    cropped = cropped.resize(resized_size, Image.Resampling.LANCZOS)
    cropped = cropped.filter(ImageFilter.UnsharpMask(radius=0.7, percent=65, threshold=3))

    x = (CANVAS_SIZE[0] - cropped.width) // 2
    y = FOOTLINE_Y - cropped.height
    if y < 0:
        cropped = cropped.crop((0, -y, cropped.width, cropped.height))
        y = 0

    output.alpha_composite(cropped, (x, y))
    return output


def make_portrait(sprite: Image.Image, size: int, zoom: float, y_bias: int) -> Image.Image:
    bounds = sprite.getchannel("A").getbbox()
    output = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    if bounds is None:
        return output

    cropped = sprite.crop(bounds)
    target_width = int(size * zoom)
    scale = target_width / cropped.width
    resized = cropped.resize((max(1, round(cropped.width * scale)), max(1, round(cropped.height * scale))),
                             Image.Resampling.LANCZOS)
    x = (size - resized.width) // 2
    y = y_bias
    output.alpha_composite(resized, (x, y))
    return output


def save_preview(all_sprites: dict[str, dict[str, Image.Image]]) -> None:
    poses = list(POSE_CELLS)
    thumb_w, thumb_h, label_h = 176, 148, 24
    preview = Image.new("RGB", (thumb_w * len(poses), (thumb_h + label_h) * len(all_sprites)), (32, 32, 36))
    draw = ImageDraw.Draw(preview)

    for row, (character_id, sprites) in enumerate(all_sprites.items()):
        for column, pose in enumerate(poses):
            sprite = sprites[pose]
            background = Image.new("RGBA", CANVAS_SIZE, (42, 42, 46, 255))
            bg_draw = ImageDraw.Draw(background)
            step = 32
            for y in range(0, CANVAS_SIZE[1], step):
                for x in range(0, CANVAS_SIZE[0], step):
                    if (x // step + y // step) % 2 == 0:
                        bg_draw.rectangle((x, y, x + step - 1, y + step - 1), fill=(70, 70, 76, 255))

            bg_draw.line((0, FOOTLINE_Y, CANVAS_SIZE[0], FOOTLINE_Y), fill=(255, 220, 80, 120), width=1)
            composite = Image.alpha_composite(background, sprite)
            composite.thumbnail((thumb_w, thumb_h - label_h), Image.Resampling.LANCZOS)

            x = column * thumb_w + (thumb_w - composite.width) // 2
            y = row * (thumb_h + label_h) + label_h + (thumb_h - label_h - composite.height) // 2
            preview.paste(composite.convert("RGB"), (x, y))
            draw.text((column * thumb_w + 4, row * (thumb_h + label_h) + 4),
                      f"{character_id}\n{pose}", fill=(235, 235, 235))

    preview_path = REPO_ROOT / "tools" / "content-authoring" / "battle_character_contact_sheet.png"
    preview.save(preview_path)


def main() -> None:
    all_sprites: dict[str, dict[str, Image.Image]] = {}

    for character in CHARACTERS:
        source_path = CHARACTER_ROOT / character.character_id / "Source" / character.source_file
        sheet = Image.open(source_path).convert("RGBA")
        sprites = assign_components(character.character_id, sheet)

        sprite_dir = CHARACTER_ROOT / character.character_id / "Sprites"
        portrait_dir = CHARACTER_ROOT / character.character_id / "Portraits"
        sprite_dir.mkdir(parents=True, exist_ok=True)
        portrait_dir.mkdir(parents=True, exist_ok=True)

        for pose, sprite in sprites.items():
            sprite.save(sprite_dir / f"{character.character_id}_{pose}.png")

        portrait = make_portrait(sprites["idle"], 512, 1.85, -82)
        icon = make_portrait(sprites["idle"], 256, 1.95, -46)
        portrait.save(portrait_dir / f"{character.character_id}_portrait.png")
        icon.save(portrait_dir / f"{character.character_id}_icon.png")
        all_sprites[character.character_id] = sprites

    save_preview(all_sprites)


if __name__ == "__main__":
    main()
