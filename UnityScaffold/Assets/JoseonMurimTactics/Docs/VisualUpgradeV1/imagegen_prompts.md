# Visual Upgrade V1 Image Generation Prompts

Common constraints for every generated asset:
Original Joseon murim tactical RPG art, no text, no logo, no watermark, no copied IP, age-appropriate non-sexual character designs, clean game readability.

## Concept
`Concepts/baekdusan_gate_battle_concept_16x9.png`

Prompt:
Original Joseon murim tactical RPG battle scene, snowy Baekdu mountain sect gate at night, ruined stone stairs, broken wooden signboard, torn sect banners, frozen stream, pine trees covered in snow, cliff ridges, warm torches contrasting with cold blue moonlight, dramatic 2D painterly pixel-inspired game art, quarter-view tactical map readability, commercial indie SRPG quality, no text, no logo, no watermark, 16:9 composition.

`Concepts/baek_ryeon_bandit_rescue_concept_16x9.png`

Prompt:
Original Joseon murim story event scene, snowy mountain road near Seorak spear sect, adult spear heroine in white-blue outfit surrounded by rough bandits, protagonist party arriving to help, dramatic but non-sexual, moonlit snow forest, warm lantern light, painterly 2D visual novel tactical RPG event art, no text, no logo, no watermark, 16:9 composition.

## Tiles
Common tile prompt:
Single original 2D tactical RPG ground tile sprite, Joseon murim snowy mountain battlefield, readable top-down quarter-view tile, painterly pixel-inspired texture, clean edges, no character, no text, no logo, no watermark.

Targets:
- `Tiles/snow_ground_01.png`: soft untouched snow, subtle blue shadows.
- `Tiles/snow_ground_02.png`: uneven snow with footprints and small stones.
- `Tiles/snow_ground_03.png`: wind-swept snow, darker edge shadows.
- `Tiles/packed_snow_road_01.png`: packed snow road, muddy gray path visible under snow.
- `Tiles/stone_stair_snow_01.png`: old stone stair tile with snow on edges.
- `Tiles/frozen_stream_01.png`: frozen stream ice tile, blue translucent ice.
- `Tiles/ice_crack_01.png`: cracked dangerous ice tile, tactical hazard readability.
- `Tiles/cliff_top_snow_01.png`: snowy cliff top, darker rim, height readable.
- `Tiles/cliff_side_ice_01.png`: icy cliff side vertical face, blue gray rock.
- `Tiles/shrine_floor_ruined_01.png`: ruined shrine stone floor with snow dust.
- `Tiles/burned_ground_01.png`: scorched snow and ash, used for fire hazard.
- `Tiles/smoke_ground_01.png`: dim smoky ground overlay, semi-transparent feel.

## Props
Common prop prompt:
Original 2D game prop sprite for Joseon murim snowy battlefield, perfectly flat solid #00ff00 chroma-key background for background removal, transparent-ready cutout, quarter-view tactical RPG perspective, painterly pixel-inspired detail, strong readable silhouette, no cast shadow, no text, no logo, no watermark.

Targets:
- `Props/broken_sect_gate.png`
- `Props/broken_signboard_haedongmun.png`
- `Props/torn_banner_blue.png`
- `Props/torn_banner_red_enemy.png`
- `Props/torch_stand_lit.png`
- `Props/stone_lantern_snow.png`
- `Props/frozen_pine_small.png`
- `Props/frozen_pine_large.png`
- `Props/snow_rock_cover_01.png`
- `Props/snow_rock_cover_02.png`
- `Props/wooden_palisade_broken.png`
- `Props/shrine_bell_snow.png`
- `Props/incense_burner_frozen.png`
- `Props/bamboo_trap_snow.png`
- `Props/bandit_supply_cart.png`
- `Props/ice_bridge_rope.png`

## VFX
Common VFX prompt:
Transparent-ready 2D game VFX sprite sheet on perfectly flat solid #00ff00 chroma-key background, 4 animation frames arranged left to right, original Joseon murim tactical RPG effect, painterly pixel-inspired, high readability on snowy battlefield, no text, no logo, no watermark.

Targets:
- `VFX/vfx_sword_slash_silver_4f.png`
- `VFX/vfx_frost_spear_4f.png`
- `VFX/vfx_hit_spark_red_4f.png`
- `VFX/vfx_heal_inner_light_4f.png`
- `VFX/vfx_snow_step_puff_4f.png`
- `VFX/vfx_counter_flash_4f.png`
- `VFX/vfx_danger_aura_loop_4f.png`
- `VFX/vfx_phase_snow_swirl_4f.png`

## Characters Pilot
Regenerate as a consistent batch, not one-off final replacements. Pilot first:
`Characters/park_sungjun_fullbody_visual_v1.png`
`Characters/park_sungjun_fullbody_visual_v2.png`

Prompt:
Original Joseon murim tactical RPG full-body sprite, transparent-ready cutout on perfectly flat solid #00ff00 chroma-key background, 3/4 overhead view from 25 to 35 degrees, fixed upper-left key light, low saturation mid-value palette, soft ink edge, no baked ground shadow, no halo, elegant sword, white/navy/gold Baekdu light-sword sect robe armor, readable tactical silhouette, no text, no logo, no watermark, non-sexual design. Park Sungjun must match the existing project reference: 17-year-old youthful Korean male protagonist, short tousled black hair, large blue-gray eyes, clean-shaven face, no beard, no mustache, no stubble, no long hair, playful confident young expression.

V2 correction:
Use the existing `park_sungjun` sprite sheet as the primary reference, not the previous generated full-body image. Keep Park Sungjun compact and youthful: about 2 to 2.3 heads tall, very large head, short limbs, short tousled black hair, clean-shaven 17-year-old face, no beard, no mustache, no stubble, no adult facial structure. The generated v2 candidate exists as a reference-aware correction, but do not batch replace the party until all characters can be regenerated together at the same compact SD scale.

Current character visual outputs:
- `Characters/park_sungjun_fullbody_visual_v1.png` — short tousled black hair, blue-gray eyes, clean-shaven 17-year-old protagonist, white/navy/gold light-sword robe armor.
- `Characters/park_sungjun_fullbody_visual_v2.png` — corrected Park Sungjun candidate generated with the existing top-left project sprite as the primary reference; still treated as a pilot, not a one-off party-wide replacement.
- `Characters/baek_ryeon_fullbody_visual_v1.png` — long dark blue hair, blue eyes, snowflake ornament, white/ice-blue spear robe armor.
- `Characters/do_arin_fullbody_visual_v1.png` — high dark ponytail with red-orange flame tips, amber eyes, covered red/black dao battle robe.
- `Characters/jin_seoyul_fullbody_visual_v1.png` — dark navy hair with blue/gold scholar ornaments, blue eyes, ornate staff, white/blue/gold robe armor.
- `Characters/shin_seoa_fullbody_visual_v1.png` — soft brown hair with flower ornaments, warm brown eyes, pastel pink/mint floral fan outfit.
- `Characters/han_biyeon_fullbody_visual_v1.png` — dark violet side ponytail, purple eyes, masked full-coverage black/purple dagger stealth robe.

Reference lock:
Before generating any character replacement, inspect the existing `Art/Characters/<id>/Sprites/<id>_idle.png` and `Portraits/<id>_portrait.png`. Keep the chibi tactical proportions, weapon silhouette, hair length/color, eye color, age read, and sect palette from those references. Do not invent adult versions, facial hair, long hair for Park Sungjun, or revealing outfits for teenage party members.
