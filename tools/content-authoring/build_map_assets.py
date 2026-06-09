from __future__ import annotations

import math
import random
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFilter

from build_map_asset_catalog import main as build_catalog


REPO = Path(__file__).resolve().parents[2]
ASSET_ROOT = REPO / "UnityScaffold" / "Assets" / "JoseonMurimTactics" / "Resources" / "MapAssets"
TILE_DIR = ASSET_ROOT / "Tiles"
OBJECT_DIR = ASSET_ROOT / "Objects"
PREVIEW_PATH = Path(__file__).with_name("map_asset_contact_sheet.png")

TILE_SIZE = (256, 128)
OBJECT_SIZE = (256, 256)
SCALE = 4


def color(hex_value: str, alpha: int = 255) -> tuple[int, int, int, int]:
    value = hex_value.lstrip("#")
    return tuple(int(value[i : i + 2], 16) for i in (0, 2, 4)) + (alpha,)


def mix(a: tuple[int, int, int, int], b: tuple[int, int, int, int], t: float) -> tuple[int, int, int, int]:
    t = max(0.0, min(1.0, t))
    return tuple(round(a[i] + (b[i] - a[i]) * t) for i in range(4))


def sc(v: float) -> int:
    return round(v * SCALE)


def spt(points: list[tuple[float, float]]) -> list[tuple[int, int]]:
    return [(sc(x), sc(y)) for x, y in points]


def add_noise(base: Image.Image, mask: Image.Image, seed: int, strength: int = 22) -> Image.Image:
    rng = random.Random(seed)
    noise = Image.new("RGBA", base.size, (0, 0, 0, 0))
    px = noise.load()
    for y in range(0, base.height, SCALE):
        for x in range(0, base.width, SCALE):
            n = rng.randint(-strength, strength)
            alpha = rng.randint(12, 32)
            shade = (255, 255, 255, alpha) if n >= 0 else (0, 0, 0, alpha)
            for yy in range(y, min(y + SCALE, base.height)):
                for xx in range(x, min(x + SCALE, base.width)):
                    px[xx, yy] = shade
    noise.putalpha(ImageChops.multiply(noise.getchannel("A"), mask))
    return Image.alpha_composite(base, noise)


def diamond_mask(width: int, height: int, inset: int = 8) -> Image.Image:
    mask = Image.new("L", (width * SCALE, height * SCALE), 0)
    draw = ImageDraw.Draw(mask)
    points = spt([(width / 2, inset), (width - inset, height / 2), (width / 2, height - inset), (inset, height / 2)])
    draw.polygon(points, fill=255)
    return mask


def make_gradient(size: tuple[int, int], top: tuple[int, int, int, int], bottom: tuple[int, int, int, int]) -> Image.Image:
    image = Image.new("RGBA", size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    for y in range(size[1]):
        t = y / max(1, size[1] - 1)
        draw.line([(0, y), (size[0], y)], fill=mix(top, bottom, t))
    return image


def draw_brush_line(
    draw: ImageDraw.ImageDraw,
    points: list[tuple[float, float]],
    fill: tuple[int, int, int, int],
    width: float = 2,
) -> None:
    draw.line(spt(points), fill=fill, width=max(1, sc(width)), joint="curve")


def tile_canvas() -> tuple[Image.Image, ImageDraw.ImageDraw, Image.Image]:
    width, height = TILE_SIZE
    image = Image.new("RGBA", (width * SCALE, height * SCALE), (0, 0, 0, 0))
    mask = diamond_mask(width, height)
    return image, ImageDraw.Draw(image, "RGBA"), mask


def finish_tile(image: Image.Image) -> Image.Image:
    image = image.filter(ImageFilter.UnsharpMask(radius=1.1 * SCALE, percent=55, threshold=3))
    return image.resize(TILE_SIZE, Image.Resampling.LANCZOS)


def paint_tile_base(
    stem: str,
    top: tuple[int, int, int, int],
    bottom: tuple[int, int, int, int],
    edge: tuple[int, int, int, int],
    side: tuple[int, int, int, int],
    depth: int = 14,
) -> tuple[Image.Image, ImageDraw.ImageDraw, Image.Image, random.Random]:
    width, height = TILE_SIZE
    rng = random.Random(sum(ord(c) for c in stem) * 17)
    image, draw, mask = tile_canvas()
    top_points = [(width / 2, 7), (width - 8, height / 2), (width / 2, height - 9), (8, height / 2)]
    lower = [(x, y + depth) for x, y in top_points]
    shadow = [(x + 8, y + depth + 5) for x, y in top_points]
    draw.polygon(spt(shadow), fill=color("000000", 58))
    draw.polygon(spt([top_points[1], lower[1], lower[2], top_points[2]]), fill=mix(side, color("000000"), 0.18))
    draw.polygon(spt([top_points[2], lower[2], lower[3], top_points[3]]), fill=mix(side, color("000000"), 0.32))
    draw.line(spt([lower[3], lower[2], lower[1]]), fill=color("000000", 70), width=sc(2.4))

    gradient = make_gradient(image.size, top, bottom)
    gradient.putalpha(mask)
    image.alpha_composite(gradient)
    image = add_noise(image, mask, rng.randint(1, 999999), 18)
    draw = ImageDraw.Draw(image, "RGBA")

    draw.line(spt([top_points[3], top_points[0], top_points[1]]), fill=mix(edge, color("ffffff"), 0.38), width=sc(2.0))
    draw.line(spt([top_points[1], top_points[2], top_points[3]]), fill=mix(edge, color("000000"), 0.12), width=sc(2.5))
    draw.line(spt(top_points + [top_points[0]]), fill=edge, width=sc(1.2))
    return image, draw, mask, rng


def clipped_layer(base: Image.Image, mask: Image.Image) -> tuple[Image.Image, ImageDraw.ImageDraw]:
    layer = Image.new("RGBA", base.size, (0, 0, 0, 0))
    return layer, ImageDraw.Draw(layer, "RGBA")


def composite_clipped(base: Image.Image, layer: Image.Image, mask: Image.Image) -> Image.Image:
    layer.putalpha(ImageChops.multiply(layer.getchannel("A"), mask))
    return Image.alpha_composite(base, layer)


def random_point(rng: random.Random, x_radius: float = 90, y_radius: float = 38) -> tuple[float, float]:
    return 128 + rng.uniform(-x_radius, x_radius), 64 + rng.uniform(-y_radius, y_radius)


def paint_grass(draw: ImageDraw.ImageDraw, rng: random.Random, count: int, bright: tuple[int, int, int, int]) -> None:
    for _ in range(count):
        x, y = random_point(rng, 96, 42)
        height = rng.uniform(9, 28)
        lean = rng.uniform(-8, 8)
        fill = mix(bright, color("111b0c", 190), rng.uniform(0.0, 0.30))
        draw_brush_line(draw, [(x, y), (x + lean, y - height)], fill, rng.uniform(0.9, 1.8))
        if rng.random() < 0.35:
            draw_brush_line(draw, [(x, y - height * 0.55), (x + lean + rng.choice([-9, 9]), y - height * 0.75)], fill, 0.8)


def paint_stone(draw: ImageDraw.ImageDraw, rng: random.Random, count: int, light: tuple[int, int, int, int]) -> None:
    for _ in range(count):
        x, y = random_point(rng, 92, 40)
        length = rng.uniform(18, 58)
        angle = rng.uniform(-0.32, 0.32)
        fill = mix(light, color("211c16", 150), rng.uniform(0.1, 0.55))
        draw_brush_line(draw, [(x, y), (x + math.cos(angle) * length, y + math.sin(angle) * length * 0.34)], fill, rng.uniform(0.8, 1.7))
        if rng.random() < 0.25:
            draw_brush_line(draw, [(x + length * 0.45, y), (x + length * 0.54, y + rng.uniform(-11, 11))], color("241f18", 80), 0.8)


def save_tile(stem: str, terrain: str) -> None:
    palettes = {
        "plain_moss": (color("65754a"), color("394d2c"), color("233018"), color("1d2615")),
        "hill_moss": (color("8a865c"), color("5f613d"), color("3b3b23"), color("30311f")),
        "stone_courtyard": (color("8a8370"), color("5f5a4d"), color("2d2a24"), color("2a2821")),
        "road_stair": (color("9a8c68"), color("706345"), color("342a1d"), color("312618")),
        "shrine_floor": (color("b09d72"), color("7c6a48"), color("4a3418"), color("3a2a17")),
        "bamboo_floor": (color("496f3b"), color("264924"), color("152911"), color("14230e")),
        "forest_floor": (color("385c31"), color("1d371d"), color("0f2010"), color("0d1b0f")),
        "shallow_water": (color("4ba3a9"), color("246f7a"), color("113e48"), color("0e3138")),
        "deep_water": (color("286c83"), color("0d3b57"), color("082436"), color("071c2a")),
        "wood_plank": (color("9b6336"), color("68401f"), color("2b1708"), color("281407")),
        "wood_bridge": (color("976139"), color("5d3518"), color("241207"), color("201006")),
        "roof_tile": (color("933629"), color("64201a"), color("2d0805"), color("240604")),
        "cliff_face": (color("676158"), color("3e3a34"), color("151310"), color("171512")),
        "wall_broken": (color("776f60"), color("514b41"), color("1b1814"), color("191612")),
        "rubble": (color("766a59"), color("554939"), color("201a13"), color("1b1710")),
        "mud_path": (color("6c4b2e"), color("46301e"), color("21140a"), color("1c1007")),
        "snow_edge": (color("d8d7c9"), color("aaa892"), color("6a6753"), color("777260")),
        "ice_slick": (color("c6eef0"), color("7ec1cb"), color("4c7c84"), color("507980")),
        "gate_threshold": (color("895a3c"), color("5e3520"), color("281307"), color("211006")),
        "fire_scorch": (color("74402b"), color("421b12"), color("250906"), color("1e0705")),
        "smoke_veil": (color("858576"), color("5f6056"), color("2e2f29"), color("272821")),
        "trap_mark": (color("6b5038"), color("46311f"), color("23140c"), color("1f1109")),
    }
    top, bottom, edge, side = palettes[stem]
    image, draw, mask, rng = paint_tile_base(stem, top, bottom, edge, side)
    layer, layer_draw = clipped_layer(image, mask)

    if stem in {"plain_moss", "hill_moss"}:
        paint_grass(layer_draw, rng, 55 if stem == "plain_moss" else 42, color("adc76f", 155))
        if stem == "hill_moss":
            for i in range(4):
                y = 42 + i * 10
                layer_draw.arc([sc(42), sc(y - 25), sc(214), sc(y + 25)], 200, 342, fill=color("e1d18d", 72), width=sc(1.3))
    elif stem in {"stone_courtyard", "road_stair", "shrine_floor"}:
        paint_stone(layer_draw, rng, 22 if stem != "shrine_floor" else 28, color("e6d8b2", 100))
        if stem == "road_stair":
            for i in range(5):
                y = 37 + i * 11
                draw_brush_line(layer_draw, [(54, y), (204, y + rng.uniform(-2, 2))], color("332719", 90), 1.2)
                draw_brush_line(layer_draw, [(57, y - 2), (200, y - 1)], color("d3bd83", 55), 0.8)
        if stem == "shrine_floor":
            for x in (76, 104, 128, 152, 180):
                draw_brush_line(layer_draw, [(x, 27), (x + 3, 101)], color("533717", 70), 1.0)
            layer_draw.ellipse([sc(105), sc(42), sc(151), sc(86)], outline=color("f6d782", 94), width=sc(1.4))
            layer_draw.line(spt([(128, 45), (128, 84), (110, 65), (146, 65)]), fill=color("fff0a4", 72), width=sc(1.1))
    elif stem in {"bamboo_floor", "forest_floor"}:
        paint_grass(layer_draw, rng, 45, color("8fcf68", 160))
        for x in [62, 84, 107, 132, 159, 187, 203]:
            lean = rng.uniform(-9, 7)
            draw_brush_line(layer_draw, [(x, 104), (x + lean, 25)], color("142a12", 145), 2.0)
            for y in [42, 58, 75, 92]:
                draw_brush_line(layer_draw, [(x + lean * (1 - y / 104), y), (x + rng.choice([-18, 18]), y + rng.uniform(-5, 5))], color("8dcf63", 105), 1.2)
        if stem == "forest_floor":
            for _ in range(9):
                x, y = random_point(rng, 76, 31)
                layer_draw.ellipse([sc(x - 13), sc(y - 10), sc(x + 18), sc(y + 12)], fill=color("12220f", 86))
    elif stem in {"shallow_water", "deep_water", "ice_slick"}:
        for _ in range(16):
            x, y = random_point(rng, 90, 38)
            w = rng.uniform(28, 76)
            layer_draw.arc([sc(x - w / 2), sc(y - 8), sc(x + w / 2), sc(y + 11)], 15, 165, fill=color("d6ffff", rng.randint(55, 135)), width=sc(rng.uniform(0.8, 1.5)))
        if stem == "deep_water":
            layer_draw.ellipse([sc(70), sc(33), sc(186), sc(93)], fill=color("031829", 92))
            layer_draw.ellipse([sc(88), sc(42), sc(168), sc(82)], fill=color("0a3952", 60))
        if stem == "ice_slick":
            for _ in range(12):
                x, y = random_point(rng, 82, 36)
                draw_brush_line(layer_draw, [(x, y), (x + rng.uniform(-35, 35), y + rng.uniform(-7, 7))], color("ffffff", 128), 0.9)
    elif stem in {"wood_plank", "wood_bridge", "gate_threshold"}:
        for x in range(50, 212, 27):
            draw_brush_line(layer_draw, [(x, 28), (x + rng.uniform(-5, 5), 102)], color("2b1708", 95), 1.4)
        for y in [38, 55, 73, 92]:
            draw_brush_line(layer_draw, [(44, y), (212, y + rng.uniform(-2, 2))], color("e5a75f", 65), 1.2)
        if stem == "wood_bridge":
            draw_brush_line(layer_draw, [(37, 38), (220, 88)], color("271407", 145), 2.0)
            draw_brush_line(layer_draw, [(38, 91), (219, 38)], color("301809", 122), 1.6)
        if stem == "gate_threshold":
            layer_draw.rectangle([sc(89), sc(34), sc(167), sc(94)], outline=color("271307", 125), width=sc(2.0))
            layer_draw.rectangle([sc(99), sc(43), sc(157), sc(84)], outline=color("d49a54", 62), width=sc(1.0))
    elif stem == "roof_tile":
        for x in range(52, 214, 18):
            draw_brush_line(layer_draw, [(x, 26), (x + 20, 102)], color("2d0905", 132), 1.8)
            draw_brush_line(layer_draw, [(x + 2, 27), (x + 21, 100)], color("d45a42", 55), 0.9)
        for y in [42, 58, 75, 92]:
            draw_brush_line(layer_draw, [(47, y), (210, y + 4)], color("f0a27d", 56), 1.1)
    elif stem in {"cliff_face", "wall_broken", "rubble"}:
        for _ in range(18 if stem == "rubble" else 12):
            x, y = random_point(rng, 88, 38)
            radius = rng.uniform(4, 14)
            pts = [(x - radius, y), (x - radius * 0.2, y - radius * 0.7), (x + radius, y - radius * 0.15), (x + radius * 0.3, y + radius * 0.78)]
            layer_draw.polygon(spt(pts), fill=mix(top, color("000000"), rng.uniform(0.18, 0.42)))
            layer_draw.line(spt([pts[1], pts[2]]), fill=color("ddd1b8", 48), width=sc(0.8))
        if stem == "cliff_face":
            for x in [68, 98, 132, 165, 194]:
                draw_brush_line(layer_draw, [(x, 23), (x - rng.uniform(4, 18), 105)], color("070605", 92), 1.5)
        if stem == "wall_broken":
            for x in range(52, 192, 34):
                layer_draw.rectangle([sc(x), sc(40), sc(x + 31), sc(58)], outline=color("15120f", 92), width=sc(1))
                layer_draw.rectangle([sc(x + 15), sc(62), sc(x + 47), sc(80)], outline=color("15120f", 80), width=sc(1))
    elif stem == "mud_path":
        for _ in range(16):
            x, y = random_point(rng, 84, 38)
            layer_draw.ellipse([sc(x - rng.uniform(9, 22)), sc(y - rng.uniform(3, 7)), sc(x + rng.uniform(11, 24)), sc(y + rng.uniform(4, 9))], fill=color("21140b", rng.randint(55, 110)))
            layer_draw.arc([sc(x - 18), sc(y - 10), sc(x + 18), sc(y + 10)], 20, 160, fill=color("a5794c", 42), width=sc(0.8))
    elif stem == "snow_edge":
        for _ in range(34):
            x, y = random_point(rng, 94, 42)
            radius = rng.uniform(1.0, 3.2)
            layer_draw.ellipse([sc(x - radius), sc(y - radius), sc(x + radius), sc(y + radius)], fill=color("ffffff", rng.randint(52, 145)))
        paint_stone(layer_draw, rng, 8, color("6f6b58", 72))
    elif stem == "fire_scorch":
        layer_draw.ellipse([sc(72), sc(34), sc(186), sc(92)], fill=color("1d0805", 120))
        for _ in range(12):
            x, y = random_point(rng, 62, 25)
            draw_brush_line(layer_draw, [(x, y + 10), (x + rng.uniform(-12, 12), y - rng.uniform(14, 34))], color("ff9b2e", rng.randint(90, 160)), 1.3)
            draw_brush_line(layer_draw, [(x + 3, y + 6), (x + rng.uniform(-7, 7), y - rng.uniform(8, 22))], color("ffe06a", rng.randint(70, 135)), 0.8)
    elif stem == "smoke_veil":
        for _ in range(13):
            x, y = random_point(rng, 78, 32)
            layer_draw.ellipse([sc(x - 20), sc(y - 10), sc(x + 22), sc(y + 12)], fill=color("d4d1bf", rng.randint(36, 82)))
    elif stem == "trap_mark":
        paint_stone(layer_draw, rng, 8, color("a9784d", 60))
        for pts in [[(82, 37), (174, 92)], [(174, 36), (82, 92)], [(128, 28), (128, 100)]]:
            draw_brush_line(layer_draw, pts, color("9c2720", 150), 2.0)

    image = composite_clipped(image, layer, mask)
    out = finish_tile(image)
    out.save(TILE_DIR / f"{stem}.png")


def object_canvas() -> tuple[Image.Image, ImageDraw.ImageDraw, random.Random]:
    image = Image.new("RGBA", (OBJECT_SIZE[0] * SCALE, OBJECT_SIZE[1] * SCALE), (0, 0, 0, 0))
    return image, ImageDraw.Draw(image, "RGBA"), random.Random()


def finish_object(image: Image.Image) -> Image.Image:
    image = image.filter(ImageFilter.UnsharpMask(radius=0.85 * SCALE, percent=65, threshold=2))
    return image.resize(OBJECT_SIZE, Image.Resampling.LANCZOS)


def draw_ellipse_shadow(draw: ImageDraw.ImageDraw, x: float, y: float, w: float, h: float, alpha: int = 58) -> None:
    draw.ellipse([sc(x - w / 2), sc(y - h / 2), sc(x + w / 2), sc(y + h / 2)], fill=color("000000", alpha))


def draw_rect(draw: ImageDraw.ImageDraw, box: tuple[float, float, float, float], fill, outline=None, width: float = 1) -> None:
    draw.rectangle([sc(v) for v in box], fill=fill, outline=outline, width=max(1, sc(width)))


def draw_poly(draw: ImageDraw.ImageDraw, points: list[tuple[float, float]], fill, outline=None, width: float = 1) -> None:
    draw.polygon(spt(points), fill=fill)
    if outline:
        draw.line(spt(points + [points[0]]), fill=outline, width=max(1, sc(width)), joint="curve")


def save_object(stem: str) -> None:
    image, draw, _ = object_canvas()

    if stem == "sect_signboard":
        draw_ellipse_shadow(draw, 128, 214, 128, 26, 55)
        for x in [83, 166]:
            draw_rect(draw, (x, 82, x + 12, 202), color("2a1609"), color("110804"), 1.4)
            draw_rect(draw, (x + 2, 84, x + 5, 197), color("9e632f", 125))
        draw_poly(draw, [(49, 58), (207, 58), (191, 105), (65, 105)], color("8b5422"), color("1f0f06"), 2.6)
        draw_poly(draw, [(62, 67), (194, 67), (184, 91), (72, 91)], color("d19a49"), color("5d3316"), 1.4)
        draw_rect(draw, (103, 103, 153, 151), color("5a2b16"), color("241207"), 2.0)
        draw.line(spt([(114, 113), (142, 113), (128, 136), (114, 113)]), fill=color("f6d77b", 210), width=sc(2.0))
        draw.line(spt([(58, 58), (126, 48), (198, 58)]), fill=color("f1c76e", 82), width=sc(1.4))
    elif stem == "incense_burner":
        draw_ellipse_shadow(draw, 128, 213, 116, 22, 50)
        for x in [109, 128, 148]:
            draw_brush_line(draw, [(x, 119), (x + 8, 88), (x - 6, 58), (x + 12, 35)], color("d7d0bd", 78), 4.2)
            draw_brush_line(draw, [(x + 2, 120), (x + 3, 78)], color("f4eddb", 36), 1.3)
        draw_rect(draw, (88, 143, 168, 165), color("3f382f"), color("17120d"), 2)
        draw.ellipse([sc(79), sc(122), sc(177), sc(158)], fill=color("756b5e"), outline=color("1a1510"), width=sc(2.2))
        draw.ellipse([sc(98), sc(115), sc(158), sc(143)], fill=color("b8ad96"), outline=color("473f35"), width=sc(1.2))
        draw_rect(draw, (108, 153, 148, 174), color("5d5144"), color("201a15"), 1.5)
        draw_brush_line(draw, [(98, 164), (82, 199)], color("211a13"), 4.0)
        draw_brush_line(draw, [(158, 164), (174, 199)], color("211a13"), 4.0)
    elif stem == "red_lantern":
        draw_ellipse_shadow(draw, 128, 215, 92, 20, 55)
        draw_brush_line(draw, [(128, 36), (128, 70)], color("1d0b07"), 3.8)
        draw_rect(draw, (103, 69, 153, 82), color("241008"), color("080302"), 1.2)
        draw.ellipse([sc(101), sc(84), sc(155), sc(160)], fill=color("c82f20"), outline=color("2b0805"), width=sc(2.2))
        draw.ellipse([sc(113), sc(95), sc(143), sc(149)], fill=color("ff8b36", 225))
        draw.ellipse([sc(120), sc(107), sc(136), sc(141)], fill=color("ffef8f", 180))
        draw_rect(draw, (111, 158, 145, 172), color("281007"), color("0b0302"), 1.2)
        draw_brush_line(draw, [(128, 172), (128, 202)], color("bb7a3c"), 1.8)
    elif stem == "oil_jar":
        draw_ellipse_shadow(draw, 128, 214, 106, 24, 55)
        draw.ellipse([sc(85), sc(116), sc(171), sc(202)], fill=color("9c5b27"), outline=color("2a1408"), width=sc(2.6))
        draw.ellipse([sc(104), sc(82), sc(152), sc(124)], fill=color("d09a56"), outline=color("35190a"), width=sc(2.0))
        draw_rect(draw, (112, 76, 144, 102), color("5a2c12"), color("1f0d04"), 1.5)
        draw.ellipse([sc(116), sc(91), sc(140), sc(106)], fill=color("f3c869", 160))
        draw_brush_line(draw, [(98, 137), (74, 126)], color("261206"), 4.2)
        draw_brush_line(draw, [(158, 137), (182, 126)], color("261206"), 4.2)
        draw.arc([sc(96), sc(135), sc(162), sc(190)], 190, 330, fill=color("f0b464", 72), width=sc(1.5))
    elif stem == "wine_cart":
        draw_ellipse_shadow(draw, 130, 216, 150, 28, 55)
        draw_poly(draw, [(58, 119), (174, 99), (195, 153), (76, 180)], color("81502d"), color("221106"), 2.4)
        for x in [78, 106, 135, 162]:
            draw_brush_line(draw, [(x, 109), (x + 6, 169)], color("2b1608", 82), 1.2)
        for y in [122, 142, 161]:
            draw_brush_line(draw, [(65, y), (188, y - 18)], color("e3a766", 60), 1.2)
        for cx, cy in [(91, 181), (166, 169)]:
            draw.ellipse([sc(cx - 20), sc(cy - 20), sc(cx + 20), sc(cy + 20)], fill=color("211207"), outline=color("070302"), width=sc(1.5))
            draw.ellipse([sc(cx - 9), sc(cy - 9), sc(cx + 9), sc(cy + 9)], fill=color("68401f"))
        draw_brush_line(draw, [(52, 115), (82, 132)], color("201006"), 4.2)
        draw_brush_line(draw, [(175, 104), (210, 89)], color("201006"), 4.2)
    elif stem == "fallen_wall":
        draw_ellipse_shadow(draw, 130, 211, 150, 25, 55)
        blocks = [(55, 143, 103, 174), (103, 124, 151, 157), (150, 139, 202, 171), (83, 95, 130, 127), (131, 93, 176, 122), (65, 122, 98, 146)]
        for i, box in enumerate(blocks):
            shade = mix(color("8b8273"), color("403a32"), i / len(blocks) * 0.35)
            draw_rect(draw, box, shade, color("191512"), 1.5)
            draw_brush_line(draw, [(box[0] + 4, box[1] + 5), (box[2] - 7, box[1] + 4)], color("d4c7aa", 55), 0.8)
        draw_brush_line(draw, [(66, 120), (194, 166)], color("080706", 90), 2.2)
        draw_brush_line(draw, [(114, 97), (154, 172)], color("080706", 70), 1.5)
    elif stem == "bridge_rope":
        draw_ellipse_shadow(draw, 132, 209, 130, 22, 48)
        draw_brush_line(draw, [(54, 138), (98, 108), (153, 116), (210, 78)], color("261506"), 5.0)
        draw_brush_line(draw, [(50, 158), (106, 128), (160, 136), (212, 98)], color("ba8548"), 3.5)
        for x, y in [(87, 130), (126, 120), (172, 108)]:
            draw_brush_line(draw, [(x - 7, y - 12), (x + 10, y + 16)], color("f2cc82", 2_00), 2.1)
            draw_brush_line(draw, [(x - 13, y + 4), (x + 15, y - 5)], color("5c3618", 160), 1.3)
    elif stem == "bamboo_bundle":
        draw_ellipse_shadow(draw, 130, 214, 132, 25, 52)
        for i, x in enumerate([82, 97, 113, 130, 147, 164, 179]):
            draw_brush_line(draw, [(x, 60 + i % 2 * 4), (x - 18 + i * 2, 193)], color("86bf5f" if i % 2 else "c5d67c"), 5.0)
            draw_brush_line(draw, [(x - 2, 101), (x + 18, 88)], color("2d5a22", 120), 2.0)
        draw_brush_line(draw, [(70, 134), (184, 118)], color("31200e"), 5.4)
        draw_brush_line(draw, [(75, 155), (190, 138)], color("5b3517"), 4.2)
        draw_brush_line(draw, [(78, 134), (178, 119)], color("f0ce82", 85), 1.1)
    elif stem == "stone_lantern":
        draw_ellipse_shadow(draw, 128, 216, 112, 24, 52)
        draw_rect(draw, (105, 151, 151, 197), color("766f64"), color("191611"), 2.1)
        draw_poly(draw, [(84, 132), (172, 132), (153, 153), (103, 153)], color("b6aa92"), color("181510"), 2.2)
        draw_rect(draw, (97, 98, 159, 132), color("81786a"), color("181510"), 2.1)
        draw_poly(draw, [(82, 88), (174, 88), (154, 105), (102, 105)], color("c6b99d"), color("181510"), 2.0)
        draw_rect(draw, (116, 106, 140, 126), color("ffd87a", 135), color("2e281e"), 1.1)
        draw_rect(draw, (112, 66, 144, 88), color("756e63"), color("181510"), 1.8)
        draw_brush_line(draw, [(103, 155), (151, 187)], color("211e19", 62), 1.2)
    elif stem == "falling_boulder":
        draw_ellipse_shadow(draw, 128, 215, 118, 26, 52)
        pts = [(75, 140), (98, 96), (146, 82), (189, 120), (175, 179), (104, 195)]
        draw_poly(draw, pts, color("706756"), color("16130f"), 2.2)
        draw_poly(draw, [(98, 96), (146, 82), (132, 128), (88, 150)], color("8f846e", 210))
        draw_brush_line(draw, [(100, 109), (139, 129), (171, 116)], color("ded0b3", 95), 1.7)
        draw_brush_line(draw, [(111, 173), (149, 149), (177, 160)], color("050504", 82), 2.0)
    elif stem == "flame_pillar":
        draw_ellipse_shadow(draw, 128, 213, 102, 28, 50)
        glow = Image.new("RGBA", image.size, (0, 0, 0, 0))
        gd = ImageDraw.Draw(glow, "RGBA")
        gd.ellipse([sc(83), sc(93), sc(173), sc(205)], fill=color("ff611e", 42))
        image.alpha_composite(glow.filter(ImageFilter.GaussianBlur(sc(7))))
        draw = ImageDraw.Draw(image, "RGBA")
        draw_poly(draw, [(128, 45), (162, 120), (140, 196), (103, 156)], color("ff541b", 230))
        draw_poly(draw, [(126, 78), (150, 132), (129, 184), (111, 139)], color("ffd75f", 225))
        draw_poly(draw, [(104, 92), (122, 144), (99, 181), (86, 133)], color("a92114", 175))
        draw.line(spt([(128, 45), (162, 120), (140, 196), (103, 156), (128, 45)]), fill=color("310704", 90), width=sc(1.3))
    elif stem == "smoke_wisp":
        draw_ellipse_shadow(draw, 128, 214, 100, 22, 34)
        smoke = Image.new("RGBA", image.size, (0, 0, 0, 0))
        sd = ImageDraw.Draw(smoke, "RGBA")
        for box, alpha in [((90, 142, 166, 200), 75), ((62, 108, 134, 178), 68), ((118, 84, 198, 156), 60), ((82, 58, 150, 131), 46), ((108, 32, 174, 98), 32)]:
            sd.ellipse([sc(v) for v in box], fill=color("c8c4b4", alpha))
        image.alpha_composite(smoke.filter(ImageFilter.GaussianBlur(sc(1.8))))
    else:
        draw_ellipse_shadow(draw, 128, 214, 90, 22, 50)

    finish_object(image).save(OBJECT_DIR / f"{stem}.png")


def build_contact_sheet() -> None:
    files = [
        TILE_DIR / "plain_moss.png",
        TILE_DIR / "stone_courtyard.png",
        TILE_DIR / "road_stair.png",
        TILE_DIR / "shrine_floor.png",
        TILE_DIR / "bamboo_floor.png",
        TILE_DIR / "shallow_water.png",
        TILE_DIR / "wood_bridge.png",
        TILE_DIR / "roof_tile.png",
        TILE_DIR / "wall_broken.png",
        TILE_DIR / "snow_edge.png",
        OBJECT_DIR / "sect_signboard.png",
        OBJECT_DIR / "incense_burner.png",
        OBJECT_DIR / "red_lantern.png",
        OBJECT_DIR / "oil_jar.png",
        OBJECT_DIR / "wine_cart.png",
        OBJECT_DIR / "fallen_wall.png",
        OBJECT_DIR / "bamboo_bundle.png",
        OBJECT_DIR / "stone_lantern.png",
    ]
    thumb = 136
    pad = 16
    cols = 6
    rows = math.ceil(len(files) / cols)
    sheet = Image.new("RGBA", (pad + cols * (thumb + pad), pad + rows * (thumb + 42)), color("191510"))
    draw = ImageDraw.Draw(sheet)
    for index, file in enumerate(files):
        x = pad + (index % cols) * (thumb + pad)
        y = pad + (index // cols) * (thumb + 42)
        draw.rounded_rectangle([x, y, x + thumb, y + thumb], radius=8, fill=color("332a20"), outline=color("b88935", 130), width=1)
        image = Image.open(file).convert("RGBA")
        image.thumbnail((thumb - 14, thumb - 14), Image.Resampling.LANCZOS)
        sheet.alpha_composite(image, (x + (thumb - image.width) // 2, y + (thumb - image.height) // 2))
        draw.text((x, y + thumb + 6), file.stem[:19], fill=color("eadcaf"))
    sheet.convert("RGB").save(PREVIEW_PATH, quality=95)


def main() -> None:
    TILE_DIR.mkdir(parents=True, exist_ok=True)
    OBJECT_DIR.mkdir(parents=True, exist_ok=True)
    for stem in [
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
    ]:
        save_tile(stem, stem)
    for stem in [
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
    ]:
        save_object(stem)
    build_catalog()
    build_contact_sheet()


if __name__ == "__main__":
    main()
