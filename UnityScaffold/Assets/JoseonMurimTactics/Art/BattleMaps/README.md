# BattleMaps Art Layout

This folder keeps battle map support assets separate from the active BattleTest scene.

- `Tilesets/Generated`: Shared `TerrainTileData` assets generated from `Resources/MapAssets`.
- `Props`: Prop tile assets and future interactive prop prefabs.
- `Overlays`: Range, danger, objective, and tactical overlay tiles.
- `Materials`: URP 2D sprite-lit and highlight materials.

Use `Joseon Murim Tactics > Battle Maps > Generate Tile Assets` in Unity to rebuild shared generated assets from
`Resources/MapAssets`.

## Active Battle Map

The active playable battle test is `Assets/JoseonMurimTactics/Scenes/BattleTest.unity`.

The old `BattleMap_Baekdu_SnowGate_v1` authored diorama sample, its `DioramaGenerated` tiles, screenshots, builder, and
SnowGate tactical layout were removed from the active project because they are not part of the current playable battle
path and were easy to confuse with live map work. Recover them from Git history only if they become useful again.

## Map Quality Notes

Current battle-map polish should target `BattleTest.unity` and its runtime presentation code first.

- Minimum tactical size: 18x12. Recommended sample size: 20x14.
- Use at least 3 elevation levels and 8 terrain types.
- Include a central choke, a flank route, high ground, objective cells, hazards, and interactable props.
- Avoid visible 3x3 repetition of the same tactical tile.
- Keep movement, attack, and danger highlights readable without flooding the whole map.
- Units and props need grounding shadows and clear row sorting.
