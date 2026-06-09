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
  Tilemap_Ground
  Tilemap_Road
  Tilemap_Water
  Tilemap_Cliff
  Tilemap_Decor
  Tilemap_Props
  Tilemap_Overlay
  Tilemap_Highlight_Move
  Tilemap_Highlight_Attack
  Tilemap_Highlight_Danger
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
