# BattleMapData Validator

- mapId: `baekdu_snow_gate`
- source: `Resources/BattleMaps/baekdu_snow_gate_data`
- size: `16x12`
- cells: `192`
- result: `PASS`

## Checks

- 16x12 = 192 cells
- no missing or duplicate coordinates
- blocked cells use impassable movement cost or blocksMovement
- moveCost>=90 is rejected by BattlePathService.CanStandOnTile
- stairs/ramp tags form a connected route
- deploy and spawn cells are standable
- BattleMapData asset matches RuntimeCatalog fallback
