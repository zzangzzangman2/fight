from __future__ import annotations

import argparse
import csv
import json
import math
import re
from collections import Counter, defaultdict
from pathlib import Path

import UnityPy
from PIL import Image, ImageDraw


ANIM_TOKENS = (
    "idle",
    "walk",
    "move",
    "run",
    "attack",
    "attack1",
    "attack2",
    "attack3",
    "attack4",
    "attack5",
    "skill",
    "exskill",
    "injuredall",
    "hit",
    "guard",
    "die",
)


def sanitize(value: str) -> str:
    value = re.sub(r"[^A-Za-z0-9_.-]+", "_", value.strip())
    return value.strip("_") or "unnamed"


def infer_prefix(sprite_name: str) -> str:
    numbered = re.match(r"^(.+)_([^_]+)_\d+$", sprite_name)
    if numbered:
        return numbered.group(1)

    lower = sprite_name.lower()
    candidates = []
    for token in ANIM_TOKENS:
        marker = f"_{token}_"
        idx = lower.find(marker)
        if idx >= 0:
            candidates.append((idx, sprite_name[:idx]))
    if candidates:
        return min(candidates, key=lambda item: item[0])[1]
    parts = sprite_name.rsplit("_", 1)
    return parts[0] if len(parts) == 2 else sprite_name


def infer_animation(sprite_name: str, prefix: str) -> str:
    numbered = re.match(r"^(.+)_([^_]+)_\d+$", sprite_name)
    if numbered:
        return numbered.group(2)

    tail = sprite_name[len(prefix) :].strip("_")
    if not tail:
        return "unknown"
    parts = tail.split("_")
    if parts and parts[0]:
        return parts[0]
    return "unknown"


def trim_image(image: Image.Image) -> tuple[Image.Image, tuple[int, int, int, int] | None]:
    bbox = image.getbbox()
    if bbox is None:
        return image, None
    return image.crop(bbox), bbox


def apply_palette(index_image: Image.Image, palette: Image.Image) -> Image.Image:
    source = index_image.convert("RGBA")
    palette = palette.convert("RGBA")
    output = Image.new("RGBA", source.size, (0, 0, 0, 0))
    src = source.load()
    dst = output.load()
    pal = palette.load()
    palette_width, palette_height = palette.size

    for y in range(source.height):
        for x in range(source.width):
            index = src[x, y][3]
            if index <= 0:
                continue

            px = index % 16
            py = index // 16
            if px >= palette_width:
                px = palette_width - 1
            if py >= palette_height:
                py = palette_height - 1

            red, green, blue, alpha = pal[px, py]
            if alpha <= 0 and (red, green, blue) == (0, 0, 0):
                continue
            dst[x, y] = (red, green, blue, 255)

    return output


def make_contact_sheet(
    samples: list[tuple[str, Image.Image]],
    output_path: Path,
    *,
    columns: int = 8,
    cell: int = 64,
    scale: int = 2,
    limit: int = 128,
) -> None:
    if not samples:
        return

    samples = samples[:limit]
    tile_width = cell * scale
    label_height = 28
    tile_height = tile_width + label_height
    rows = math.ceil(len(samples) / columns)
    sheet = Image.new("RGBA", (columns * tile_width, rows * tile_height), (18, 18, 22, 255))

    for index, (name, image) in enumerate(samples):
        trimmed, _ = trim_image(image)
        canvas = Image.new("RGBA", (cell, cell), (42, 42, 46, 255))
        guide = ImageDraw.Draw(canvas)
        for grid in range(0, cell, 16):
            guide.line((grid, 0, grid, cell), fill=(78, 78, 84, 255))
            guide.line((0, grid, cell, grid), fill=(78, 78, 84, 255))
        guide.rectangle((0, 0, cell - 1, cell - 1), outline=(118, 118, 126, 255))

        x = max(0, (cell - trimmed.width) // 2)
        y = max(0, cell - trimmed.height - 4)
        canvas.alpha_composite(trimmed, (x, y))

        preview = canvas.resize((tile_width, tile_width), Image.Resampling.NEAREST)
        label = Image.new("RGBA", (tile_width, label_height), (24, 24, 28, 255))
        draw = ImageDraw.Draw(label)
        draw.text((4, 2), name[:28], fill=(232, 232, 226, 255))
        draw.text((4, 15), f"{trimmed.width}x{trimmed.height}", fill=(170, 190, 200, 255))

        col = index % columns
        row = index // columns
        px = col * tile_width
        py = row * tile_height
        sheet.alpha_composite(preview, (px, py))
        sheet.alpha_composite(label, (px, py + tile_width))

    output_path.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(output_path)


def extract_bundle(bundle_path: Path, output_root: Path) -> list[dict[str, str | int]]:
    bundle_name = bundle_path.stem
    env = UnityPy.load(str(bundle_path))

    palettes: dict[str, Image.Image] = {}
    for obj in env.objects:
        if obj.type.name != "Texture2D":
            continue
        data = obj.read()
        name = getattr(data, "m_Name", "") or getattr(data, "name", "")
        if not name.endswith("_palette"):
            continue
        try:
            image = data.image.convert("RGBA")
        except Exception:
            continue
        palettes[name] = image
        palette_path = output_root / "Palettes" / bundle_name / f"{sanitize(name)}.png"
        palette_path.parent.mkdir(parents=True, exist_ok=True)
        image.save(palette_path)

    records: list[dict[str, str | int]] = []
    samples_by_prefix: dict[str, list[tuple[str, Image.Image]]] = defaultdict(list)
    missing_palettes: Counter[str] = Counter()

    for obj in env.objects:
        if obj.type.name != "Sprite":
            continue

        data = obj.read()
        sprite_name = getattr(data, "m_Name", "") or getattr(data, "name", "")
        if not sprite_name:
            continue

        prefix = infer_prefix(sprite_name)
        animation = infer_animation(sprite_name, prefix)
        palette_name = f"{prefix}_palette"
        palette = palettes.get(palette_name)

        try:
            indexed_image = data.image.convert("RGBA")
        except Exception:
            continue

        if palette is not None:
            final_image = apply_palette(indexed_image, palette)
            palette_status = "applied"
        else:
            final_image = indexed_image
            palette_status = "missing"
            missing_palettes[prefix] += 1

        trimmed, bbox = trim_image(final_image)
        sprite_dir = output_root / "Sprites" / "Pixel" / bundle_name / sanitize(prefix)
        sprite_dir.mkdir(parents=True, exist_ok=True)
        sprite_path = sprite_dir / f"{sanitize(sprite_name)}.png"
        final_image.save(sprite_path)

        if len(samples_by_prefix[prefix]) < 8:
            samples_by_prefix[prefix].append((sprite_name, final_image))

        rect = getattr(data, "m_Rect", None)
        records.append(
            {
                "bundle": bundle_name,
                "prefix": prefix,
                "animation": animation,
                "sprite": sprite_name,
                "palette": palette_name if palette is not None else "",
                "palette_status": palette_status,
                "sprite_width": int(getattr(rect, "width", final_image.width)) if rect else final_image.width,
                "sprite_height": int(getattr(rect, "height", final_image.height)) if rect else final_image.height,
                "trim_width": trimmed.width,
                "trim_height": trimmed.height,
                "bbox_left": bbox[0] if bbox else "",
                "bbox_top": bbox[1] if bbox else "",
                "bbox_right": bbox[2] if bbox else "",
                "bbox_bottom": bbox[3] if bbox else "",
                "path": sprite_path.relative_to(output_root).as_posix(),
            }
        )

    contact_samples: list[tuple[str, Image.Image]] = []
    for prefix in sorted(samples_by_prefix):
        contact_samples.extend(samples_by_prefix[prefix][:2])
    make_contact_sheet(contact_samples, output_root / "ContactSheets" / f"{bundle_name}.png")

    summary = {
        "bundle": bundle_name,
        "source": str(bundle_path),
        "sprite_count": len(records),
        "unit_prefix_count": len(samples_by_prefix),
        "palette_count": len(palettes),
        "missing_palette_prefixes": dict(sorted(missing_palettes.items())),
    }
    summary_path = output_root / "Summaries" / f"{bundle_name}.json"
    summary_path.parent.mkdir(parents=True, exist_ok=True)
    summary_path.write_text(json.dumps(summary, ensure_ascii=False, indent=2), encoding="utf-8")

    return records


def write_manifest(records: list[dict[str, str | int]], output_root: Path) -> None:
    manifest_path = output_root / "manifest.csv"
    manifest_path.parent.mkdir(parents=True, exist_ok=True)
    with manifest_path.open("w", newline="", encoding="utf-8") as handle:
        writer = csv.DictWriter(handle, fieldnames=list(records[0].keys()))
        writer.writeheader()
        writer.writerows(records)

    unit_rows = []
    grouped: dict[tuple[str, str], list[dict[str, str | int]]] = defaultdict(list)
    for record in records:
        grouped[(str(record["bundle"]), str(record["prefix"]))].append(record)

    for (bundle, prefix), rows in sorted(grouped.items()):
        animations = sorted({str(row["animation"]) for row in rows})
        unit_rows.append(
            {
                "bundle": bundle,
                "prefix": prefix,
                "sprite_count": len(rows),
                "animations": "|".join(animations),
                "avg_trim_width": round(sum(int(row["trim_width"]) for row in rows) / len(rows), 2),
                "avg_trim_height": round(sum(int(row["trim_height"]) for row in rows) / len(rows), 2),
                "max_trim_width": max(int(row["trim_width"]) for row in rows),
                "max_trim_height": max(int(row["trim_height"]) for row in rows),
            }
        )

    unit_manifest_path = output_root / "unit_manifest.csv"
    with unit_manifest_path.open("w", newline="", encoding="utf-8") as handle:
        writer = csv.DictWriter(handle, fieldnames=list(unit_rows[0].keys()))
        writer.writeheader()
        writer.writerows(unit_rows)


def main() -> None:
    parser = argparse.ArgumentParser(description="Extract paletted battle-unit pixels from Android Unity bundles.")
    parser.add_argument("--source", required=True, type=Path, help="AndroidAssets/battle/unit folder")
    parser.add_argument("--out", required=True, type=Path, help="Output folder inside the fight repo")
    args = parser.parse_args()

    source = args.source
    output_root = args.out
    output_root.mkdir(parents=True, exist_ok=True)

    records: list[dict[str, str | int]] = []
    for bundle_path in sorted(source.glob("battle_unit*.unity3d")):
        records.extend(extract_bundle(bundle_path, output_root))

    if not records:
        raise SystemExit(f"No sprites extracted from {source}")

    write_manifest(records, output_root)
    readme_path = output_root / "README.generated.txt"
    readme_path.write_text(
        "\n".join(
            [
                "Generated by tools/extract_android_battle_pixels.py",
                f"Source: {source}",
                f"Sprites: {len(records)}",
                "Sprites/Pixel/: Unity-ready paletted PNG frames grouped by bundle and unit prefix.",
                "Palettes/: original palette textures used to color index sprites.",
                "ContactSheets/: quick visual preview per battle_unit bundle.",
                "Unity import: place this folder under Assets/JoseonMurimTactics/Art/Characters so CharacterArtPostprocessor imports the frames as Point-filtered Sprite assets with 64 PPU.",
                "manifest.csv: per-sprite dimensions, animation token, and output path.",
                "unit_manifest.csv: per-unit summary for future selection.",
            ]
        )
        + "\n",
        encoding="utf-8",
    )

    print(f"Extracted {len(records)} sprites into {output_root}")


if __name__ == "__main__":
    main()
