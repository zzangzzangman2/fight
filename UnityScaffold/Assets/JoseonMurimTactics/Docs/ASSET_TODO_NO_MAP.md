# No-Map Asset Checklist Build

Generated from:

- `C:\\Users\\godho\\Downloads\\에셋\\codex_no_map_asset_checklist.txt`

## Scope

This pass intentionally excludes MAP, Tilemap ground/height/props/collision art, battle map layout, and world-map backgrounds.
Character body SD combat sprites are also treated as a separate workstream. This pass only creates replaceable no-map placeholders and mapping data.

## Generated Counts

- VFX sprites: 50
- VFX prefabs: 50
- UI/icon sprites: 209
- NPC portrait sprites: 48
- Enemy pose sprites: 140
- Enemy portrait sprites: 5
- Enemy visual prefabs: 20
- Dialogue background PNGs: 15

## Runtime / Data Links

- `generated_asset_manifest.json` lists every placeholder with category, path, alias, size, prefab path, and transparency verification counts.
- `icon_mapping.json` maps SkillData-adjacent element, weapon, status, combat, hub, reward, and UI ids to stable sprite paths.
- `vfx_mapping.json` records the WeaponAnimationSet links written by this pass.
- `audio_cue_manifest.json` reserves SFX/BGM cue names without generating fake audio files.

## WeaponAnimationSet Links

- `park_sungjun` -> `Assets/JoseonMurimTactics/ScriptableObjects/Weapons/park_sungjun_sword_motion_set.asset`
- `baek_ryeon` -> `Assets/JoseonMurimTactics/ScriptableObjects/Weapons/baek_ryeon_spear_motion_set.asset`
- `do_arin` -> `Assets/JoseonMurimTactics/ScriptableObjects/Weapons/do_arin_dao_motion_set.asset`
- `jin_seoyul` -> `Assets/JoseonMurimTactics/ScriptableObjects/Weapons/jin_seoyul_staff_motion_set.asset`
- `seo_a` -> `Assets/JoseonMurimTactics/ScriptableObjects/Weapons/shin_seoa_fan_motion_set.asset`
- `han_biyeon` -> `Assets/JoseonMurimTactics/ScriptableObjects/Weapons/han_biyeon_dagger_motion_set.asset`

## Alias Rules

- `seo_a` = `shin_seoa`
- `park_sungjun` = `protagonist` = `park`

## Replacement Rules

- Replace placeholder PNGs in place with the same filenames whenever possible.
- Keep PNG alpha; do not bake UI labels into images. Use TextMeshPro for text.
- VFX and character SD body assets stay separate.
- Do not add MAP or Tilemap art to this checklist pipeline.
