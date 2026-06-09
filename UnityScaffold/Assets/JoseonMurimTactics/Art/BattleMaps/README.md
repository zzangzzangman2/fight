# BattleMaps Art Layout

This folder keeps high-quality battle map assets separate from runtime test scaffolding.

- `Tilesets`: Terrain `TerrainTileData` tile assets generated from MAP tile sprites.
- `Props`: Prop tile assets and later interactive prop prefabs.
- `Overlays`: Range, danger, objective, and tactical overlay tiles.
- `Materials`: URP 2D sprite-lit and highlight materials.

Use `Joseon Murim Tactics > Battle Maps > Generate Tile Assets` in Unity to rebuild generated assets from
`Resources/MapAssets`.

## Authoring Structure

Scene-authored battle maps should use this hierarchy:

```text
Grid_BattleMap
  Tilemap_Backdrop_Base
  Tilemap_Backdrop_Distant
  Tilemap_Ground_Base
  Tilemap_Ground_Variation
  Tilemap_Road_Path
  Tilemap_Road_Edge
  Tilemap_Water_Base
  Tilemap_Water_Surface
  Tilemap_Cliff_Top
  Tilemap_Cliff_Face
  Tilemap_Cliff_Edge
  Tilemap_Decor_Ground
  Tilemap_Decor_GrassRockSnow
  Tilemap_Props_BehindUnits
  Tilemap_Props_FrontOfUnits
  Tilemap_Shadow_AO
  Tilemap_Fog_Mist
  Tilemap_Grid_Subtle
  Tilemap_Highlight_Move
  Tilemap_Highlight_Attack
  Tilemap_Highlight_Danger
  Tilemap_Highlight_PathArrow
  PropsRoot
  LightsRoot
  TacticalGridOverlay
```

Attach `BattleMapTilemapBinder` to `Grid_BattleMap`. The binder keeps visual Tilemaps separate from tactical data,
then syncs `TerrainTileData` into `TacticalGridOverlay` or `BattleMapData`.

## Tactical Checklist

The first production sample is `압록강 협곡 관문`.

- Center bridge bottleneck with 1-2 tile pressure.
- Left bamboo forest bypass with cover and line-of-sight blocking.
- Right cliff high ground with fall edges and a beacon.
- Shallow ford, ruined shrine altar, lanterns, cart, broken wall, and collapse points.
- At least 6 interactable props using `MapPropView`, `InteractableProp`, and related tactical components.
- Validate with `Joseon Murim Tactics > Validate Current Battle Map`.

## Map Quality Standard v1.7

Production battle maps must be authored as tactical dioramas, not only generated at runtime.

- Minimum tactical size: 18x12. Recommended sample size: 20x14.
- Use at least 3 elevation levels and 8 terrain types.
- Include a central 1-2 tile choke, a left forest/bamboo flank, a right cliff high ground route, top gate/shrine cells, and water/ice/ford interaction.
- Include at least 6 interactable props with `SpriteRenderer`, `SortingGroup`, `ShadowBlob`, `MapPropView`, `InteractableProp`, and optional `Light2D`.
- Include at least 8 line-of-sight blockers, 10 cover cells, and 6 cliff drop edges.
- Avoid visible 3x3 repetition of the same tactical tile. Use `TerrainVariantSet` for deterministic visual variation.
- Keep highlights subtle: movement <= 0.18 alpha, attack <= 0.16 alpha, danger as a quiet outline/danger layer.
- The camera background cannot be black. Use mountains, mist, river/ice, sky wash, or scene backdrop sprites.
- Units and props need grounding shadows. Props should also be Y-sorted or grouped with `SortingGroup`.
- `BattleMapTilemapBinder` should sync authored tile layers into `TacticalGridOverlay`; runtime map generation is only a fallback.
