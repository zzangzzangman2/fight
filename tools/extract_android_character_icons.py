from __future__ import annotations

import argparse
import csv
import math
import re
from pathlib import Path

import UnityPy
from PIL import Image, ImageDraw


GROUPS = (
    "character",
    "character_head",
    "player_head",
    "card_small",
    "unit_piece",
    "unit_rank_material",
)


def sanitize(value: str) -> str:
    value = re.sub(r"[^A-Za-z0-9_.-]+", "_", value.strip())
    return value.strip("_") or "unnamed"


def trim_image(image: Image.Image) -> Image.Image:
    bbox = image.getbbox()
    return image.crop(bbox) if bbox else image


def make_contact_sheet(samples: list[tuple[str, Image.Image]], output_path: Path) -> None:
    if not samples:
        return

    columns = 8
    cell_width = 136
    cell_height = 144
    rows = math.ceil(len(samples) / columns)
    sheet = Image.new("RGBA", (columns * cell_width, rows * cell_height), (18, 18, 22, 255))

    for index, (name, image) in enumerate(samples):
        trimmed = trim_image(image)
        thumb = trimmed.copy()
        resample = Image.Resampling.NEAREST if max(trimmed.size) <= 320 else Image.Resampling.LANCZOS
        thumb.thumbnail((112, 108), resample)

        tile = Image.new("RGBA", (cell_width, cell_height), (28, 28, 32, 255))
        tile.alpha_composite(thumb, ((cell_width - thumb.width) // 2, 4))
        draw = ImageDraw.Draw(tile)
        draw.text((3, 115), name[:24], fill=(236, 236, 230, 255))
        draw.text((3, 130), f"{trimmed.width}x{trimmed.height}", fill=(176, 194, 204, 255))

        x = (index % columns) * cell_width
        y = (index // columns) * cell_height
        sheet.alpha_composite(tile, (x, y))

    output_path.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(output_path)


def extract_group(source_root: Path, group: str, output_root: Path) -> list[dict[str, str | int]]:
    source_dir = source_root / "icon" / group
    if not source_dir.exists():
        return []

    group_output = output_root / "Sprites" / "Pixel" / group
    group_output.mkdir(parents=True, exist_ok=True)
    records: list[dict[str, str | int]] = []
    samples: list[tuple[str, Image.Image]] = []

    for bundle_path in sorted(source_dir.glob("*.unity3d")):
        env = UnityPy.load(str(bundle_path))
        bundle_name = bundle_path.stem
        for obj in env.objects:
            if obj.type.name != "Texture2D":
                continue

            data = obj.read()
            name = getattr(data, "m_Name", "") or getattr(data, "name", "")
            if not name or name.startswith("sactx-"):
                continue

            try:
                image = data.image.convert("RGBA")
            except Exception:
                continue

            target_dir = group_output / sanitize(bundle_name)
            target_dir.mkdir(parents=True, exist_ok=True)
            target_path = target_dir / f"{sanitize(name)}.png"
            image.save(target_path)

            trimmed = trim_image(image)
            records.append(
                {
                    "group": group,
                    "bundle": bundle_name,
                    "texture": name,
                    "width": image.width,
                    "height": image.height,
                    "trim_width": trimmed.width,
                    "trim_height": trimmed.height,
                    "path": target_path.relative_to(output_root).as_posix(),
                }
            )

            if len(samples) < 160:
                samples.append((name.replace("character_", "").replace("character_head_", ""), image))

    make_contact_sheet(samples, output_root / "ContactSheets" / f"{group}.png")
    return records


def write_manifest(records: list[dict[str, str | int]], output_root: Path) -> None:
    if not records:
        return

    manifest_path = output_root / "manifest.csv"
    with manifest_path.open("w", newline="", encoding="utf-8") as handle:
        writer = csv.DictWriter(handle, fieldnames=list(records[0].keys()))
        writer.writeheader()
        writer.writerows(records)

    summary_rows = []
    for group in GROUPS:
        group_records = [record for record in records if record["group"] == group]
        if not group_records:
            continue
        summary_rows.append(
            {
                "group": group,
                "texture_count": len(group_records),
                "avg_trim_width": round(sum(int(record["trim_width"]) for record in group_records) / len(group_records), 2),
                "avg_trim_height": round(sum(int(record["trim_height"]) for record in group_records) / len(group_records), 2),
                "max_width": max(int(record["width"]) for record in group_records),
                "max_height": max(int(record["height"]) for record in group_records),
            }
        )

    summary_path = output_root / "group_manifest.csv"
    with summary_path.open("w", newline="", encoding="utf-8") as handle:
        writer = csv.DictWriter(handle, fieldnames=list(summary_rows[0].keys()))
        writer.writeheader()
        writer.writerows(summary_rows)


def main() -> None:
    parser = argparse.ArgumentParser(description="Extract reusable character icon textures from Android Unity bundles.")
    parser.add_argument("--source", required=True, type=Path, help="AndroidAssets root folder")
    parser.add_argument("--out", required=True, type=Path, help="Unity project output folder")
    args = parser.parse_args()

    output_root = args.out
    output_root.mkdir(parents=True, exist_ok=True)

    records: list[dict[str, str | int]] = []
    for group in GROUPS:
        records.extend(extract_group(args.source, group, output_root))

    if not records:
        raise SystemExit(f"No character icon textures extracted from {args.source}")

    write_manifest(records, output_root)
    readme_path = output_root / "README.generated.txt"
    readme_path.write_text(
        "\n".join(
            [
                "Generated by tools/extract_android_character_icons.py",
                f"Source: {args.source}",
                f"Textures: {len(records)}",
                "Sprites/Pixel/: Unity-ready character icon PNGs grouped by source icon folder.",
                "ContactSheets/: quick visual previews for character, card_small, heads, unit pieces, and rank materials.",
                "manifest.csv: per-texture source and size data.",
                "group_manifest.csv: per-source-folder summary.",
                "Unity import: this folder lives under Assets/JoseonMurimTactics/Art/Characters and Sprites/Pixel so CharacterArtPostprocessor imports these as Point-filtered Sprite assets with 64 PPU.",
            ]
        )
        + "\n",
        encoding="utf-8",
    )

    print(f"Extracted {len(records)} character icon textures into {output_root}")


if __name__ == "__main__":
    main()
