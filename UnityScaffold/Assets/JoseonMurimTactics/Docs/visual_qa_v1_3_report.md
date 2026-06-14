# Visual QA v1.3 Report

Date: 2026-06-13
Branch: visual-qa-polish-v1.3
Baseline: origin/map-quality-v1.2

## Scope

- Restored the painted Baekdu battle backdrop instead of the generated tile fallback.
- Removed floating face/eye overlays when a full-body sprite is active.
- Hid free-floating prop sprites on the painted map while preserving interactable gameplay data.
- Reworked the top-right HUD card and roster text sizing to avoid clipped labels.
- Added the pre-battle deployment phase with limited starting cells, blue deploy cells, and red boundary cells.
- Replaced the text-only deployment strip with full-body character slot art.
- Added directional movement support and connected ImageGen2 walk-cycle sprites for the active ally and enemy roster.
- Regenerated Jin Seoyul's front/side/back walk sheet as ImageGen2 v2 with stronger two-foot stride silhouettes.
- Strengthened attack/skill presentation with a screen-space cut overlay, impact flash, camera punch, and visible damage popup fallback.
- Restored the Baekdu snow gate painted map as the default BattleTest map after the temporary snowfield fallback.
- Kept character render layers fully opaque and removed the selected-unit sorting jump that made rear units overlap front units.
- Fixed basic attack targeting so clicking an enemy-occupied tile resolves to that unit even when the sprite collider is missed.
- Fixed the default move-mode click order so hostile unit clicks are handled before tile movement attempts, allowing an adjacent enemy click to fire a basic attack instead of trying to move into the occupied tile.
- Added attack-mode tolerance for painted-map projection misses: a one-tile visual click offset around one unique attackable enemy resolves to that enemy.
- Added validator coverage for basic attack adjacency: the four direct neighbor tiles must remain range 1.
- Added validator coverage for tile-only attack targeting: an adjacent enemy-occupied tile must resolve to that enemy.
- Restored Baek Ryeon's dialogue standing portrait mapping while preserving romantic/approval presentation effects.

## ImageGen2 Walk Assets

New ImageGen2 walk assets live under:

- `Assets/JoseonMurimTactics/Art/Characters/jin_seoyul/Sprites/ImageGen2/`
- `Assets/JoseonMurimTactics/Art/Characters/park_sungjun/Sprites/ImageGen2/`
- `Assets/JoseonMurimTactics/Art/Characters/baek_ryeon/Sprites/ImageGen2/`
- `Assets/JoseonMurimTactics/Art/Characters/han_biyeon/Sprites/ImageGen2/`
- `Assets/JoseonMurimTactics/Art/Characters/do_arin/Sprites/ImageGen2/`
- `Assets/JoseonMurimTactics/Art/Characters/shin_seoa/Sprites/ImageGen2/`
- `Assets/JoseonMurimTactics/Art/Characters/Enemies/enemy_black_hat_swordsman/ImageGen2/`
- `Assets/JoseonMurimTactics/Art/Characters/Enemies/enemy_black_hat_archer/ImageGen2/`
- `Assets/JoseonMurimTactics/Art/Characters/Enemies/enemy_black_hat_boss_gwakchil/ImageGen2/`

Connected in:

- `Assets/JoseonMurimTactics/Art/Characters/jin_seoyul/VisualData/jin_seoyul_visual.asset`
- `Assets/JoseonMurimTactics/Art/Characters/park_sungjun/VisualData/park_sungjun_visual.asset`
- `Assets/JoseonMurimTactics/Art/Characters/baek_ryeon/VisualData/baek_ryeon_visual.asset`
- `Assets/JoseonMurimTactics/Art/Characters/han_biyeon/VisualData/han_biyeon_visual.asset`
- `Assets/JoseonMurimTactics/Art/Characters/do_arin/VisualData/do_arin_visual.asset`
- `Assets/JoseonMurimTactics/Art/Characters/shin_seoa/VisualData/shin_seoa_visual.asset`
- `Assets/JoseonMurimTactics/Art/Characters/Enemies/enemy_black_hat_swordsman/VisualData/enemy_black_hat_swordsman_visual.asset`
- `Assets/JoseonMurimTactics/Art/Characters/Enemies/enemy_black_hat_archer/VisualData/enemy_black_hat_archer_visual.asset`
- `Assets/JoseonMurimTactics/Art/Characters/Enemies/enemy_black_hat_boss_gwakchil/VisualData/enemy_black_hat_boss_gwakchil_visual.asset`

Frame sets:

- 9 active battle characters now have 4 front walk frames, 4 side walk frames, and 4 back walk frames.
- Total new runtime walk frame PNGs: 108.
- Naming pattern: `<visual_id>_walk_front_imagegen2_01.png` through `_04.png`, plus matching `side` and `back` sets.
- Jin Seoyul also has 12 v2 replacement runtime frames named `jin_seoyul_walk_<direction>_imagegen2_v2_01.png` through `_04.png`.

The generated PNGs were post-processed to remove detached alpha fragments so weapon ornaments do not appear as floating debris.

## Movement Behavior

- `CharacterVisualData` now supports side/back pose overrides and side/back move-frame arrays.
- `CharacterVisualController` tracks a real movement vector instead of only left/right facing.
- Front, side, and back movement choose their matching frame arrays.
- Visual-level move frames are preferred over legacy outfit move frames, so the new ImageGen2 frames are used at runtime.
- Movement timing is clamped to a readable minimum so the walk cycle is visible instead of snapping.
- The "move complete" notice is delayed until after the movement coroutine finishes.
- Idle side/back pose references are filled from the directional movement poses, so units no longer snap back to a front-facing idle after moving side/back.
- Full-body cast shadows and ink-rim overlays are disabled while moving so the walk-cycle feet are not covered by a semi-transparent duplicate body.
- The QA debug move target now prefers uncrowded cells, making walk-cycle captures easier to read.

## Deployment Behavior

- Battle start enters deployment mode before the first action phase.
- Units can only be placed on configured starting cells.
- Occupied cells are blocked except for the selected unit's current cell.
- Blue cells show valid starting positions; red cells show the surrounding boundary/danger area.
- The deployment strip uses full-body unit art and HP state instead of large text-only blocks.

## Combat Presentation

- Debug attack/skill hotkeys now force a presentation hit when the selected target is out of normal range, so QA captures always exercise the attack timeline.
- Attack start now triggers a darkened screen-space cut with fast diagonal speed lines and a small camera punch.
- Hit frame now triggers a stronger impact flash, camera shake, and zoom pulse.
- Damage numbers are spawned through both the world-space TMP popup and a high-order screen-space overlay fallback, so the number remains visible over character art and map detail.
- The move-complete HUD notice is cleared before attack/skill playback so it no longer covers the action.
- Hostile clicks are resolved before move clicks in the default command state, so an adjacent enemy click does not get swallowed as an invalid movement order.
- Baekdu Snow Gate terrain logic now matches the painted backdrop instead of the old river/bridge layout; the central stone lane at `(10,5)` is no longer treated as DeepWater.
- Player deployment cells are restricted to authored blue start cells on standable painted ground, with red boundary cells only around that legal area.
- Initial ally/enemy starts now stay on safe authored cells without fallback correction, and enemies no longer start on breach objectives.
- Movement, deployment, push landing, debug move, and pathfinding now share the same standability guard so painted rocks, trees, walls, cliff edges, and water cannot leak back into legal standing cells.
- `tools/capture-battle.ps1` now restores and temporarily topmosts the BattleTest window before screenshotting, avoiding accidental browser captures during QA.

## Verification

Build command:

`cmd /c play-windows-player-preview.cmd --build-only`

Result:

`C:\Users\sjpark\Downloads\fight\UnityScaffold\Builds\BattleTest\JoseonMurimTacticsBattleTest.exe`

Validator command:

`C:\Program Files\Unity\Hub\Editor\6000.4.9f1\Editor\Unity.exe -batchmode -quit -projectPath C:\Users\sjpark\Downloads\fight\UnityScaffold -executeMethod JoseonMurimTactics.Editor.VisualUpgradeV1Validator.ValidateVisualSetup`

Final validator log:

`C:\Users\sjpark\Documents\Codex\2026-06-12\c-users-sjpark-downloads-fight-git\outputs\unity-visual-validator-v1-3-terrain-placement-mask.log`

Key PASS lines:

- `PASS: BattleTest keeps the painted Baekdu map backdrop: baekdu_snow_gate_srpg_ground.`
- `PASS: BattleTest basic attack distance treats four direct neighbor tiles as range 1.`
- `PASS: BattleTest has units with basic attack range 1 or higher.`
- `PASS: BattleTest tile click targeting resolves an adjacent enemy-occupied tile to that enemy.`
- `PASS: BattleTest basic attack permits a valid enemy on the direct front tile.`
- `PASS: BattleTest attack mode tolerates a one-tile visual click offset around a unique adjacent enemy.`
- `PASS: Battle deployment supports dragging roster characters onto starting cells.`
- `PASS: Battle deployment roster slots have begin/drag/end handlers.`
- `PASS: Battle characters default to front-facing idle before movement.`
- `PASS: Baekdu painted-map mask blocks water/wall art while keeping the central stone lane usable.`
- `PASS: Baekdu tile (10,5) is central stone ground, not deep water.`
- `PASS: BattleTest runtime units spawn only on standable painted-map cells.`
- `PASS: BattleTest runtime units keep authored safe start cells without fallback correction.`
- `PASS: BattleTest starts active; enemies do not spawn on breach objectives.`
- `PASS: Battle deployment cells are restricted to standable, unblocked starting cells.`
- `PASS: Baek Ryeon allows romantic presentation effects.`
- `PASS: Dialogue scene has standing/portrait binding: chapter1_baek_ryeon_join_after_battle`
- `PASS: Generated VisualUpgradeV1 sprites found: 98.`

Capture outputs:

`C:\Users\sjpark\Documents\Codex\2026-06-12\c-users-sjpark-downloads-fight-git\outputs\battle-shots-movement-v1-3\`

- `01-deployment.png`
- `03-moving-0140ms.png`
- `04-moving-0360ms.png`
- `05-moving-0640ms.png`
- `movement-crop-contact-final.png`
- `movement-v2-no-body-ghost-crop-contact.png`
- `imagegen2-walk-frames-final-contact.png`
- `jin-seoyul-imagegen2-v2-walk-frames-cleaned.png`
- `park-sungjun-imagegen2-walk-frames.png`
- `baek_ryeon-imagegen2-walk-frames.png`
- `allies-imagegen2-walk-frames-han-do-shin.png`
- `enemies-imagegen2-walk-frames.png`

`C:\Users\sjpark\Documents\Codex\2026-06-12\c-users-sjpark-downloads-fight-git\outputs\battle-shots-attack-v1-3\`

- `03-after-debug-move.png`
- `04-action-start-0090ms.png`
- `05-action-cut-0220ms.png`
- `06-action-hit-0580ms.png`
- `06-action-hit-0580ms-popup-zoom.png`
- `07-action-recover-0840ms.png`
- `attack-action-screen-damage-final-contact.png`

`C:\Users\sjpark\Documents\Codex\2026-06-12\c-users-sjpark-downloads-fight-git\outputs\battle-shots-attack-adjacency-v1-3\`

- `01-scout.png`
- `02-player-phase-move.png`
- `03-threat-overlay.png`
- `04-attack-mode.png`

`C:\Users\sjpark\Documents\Codex\2026-06-12\c-users-sjpark-downloads-fight-git\outputs\battle-shots-tile-click-target-v1-3\`

- `01-scout.png`
- `02-player-phase-move.png`
- `03-threat-overlay.png`
- `04-attack-mode.png`

`C:\Users\sjpark\Documents\Codex\2026-06-12\c-users-sjpark-downloads-fight-git\outputs\battle-shots-terrain-placement-mask-v1-3\`

- `01-scout.png`
- `02-player-phase-move.png`
- `03-threat-overlay.png`
- `04-attack-mode.png`

## Remaining Art Scope

The active BattleTest roster now has directional two-foot ImageGen2 walk cycles. Remaining art scope is broader polish, not missing directional walk coverage: future passes should tune frame consistency, attack/skill directional poses, and any new characters added outside the current active roster.
