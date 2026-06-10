# 2D Combat Motion Pipeline

This vertical slice keeps the battlefield on the existing 2D SRPG Tilemap flow and drives the unit visual through `CharacterVisualController`.

## Movement Flow

1. Select an active unit.
2. Click a reachable tile or press `M` in the test scene.
3. `BattleTestController.FindMovePath` builds a tile path.
4. `AnimateMove` walks the view through each tile center instead of teleporting.
5. `BattleTestUnitView.WalkSecondsPerTile` reads timing from `CharacterVisualData` or its `WeaponAnimationSet`.
6. `CharacterVisualController` plays the Move state, faces the travel direction, adds hop/lean, and shows footstep dust.
7. `MoveSettleTime` adds a small arrival pause before returning to Idle/SelectedIdle/Wait.

## Attack Flow

1. Attack or skill target is selected.
2. `RunAttackCommand` or `RunEnemyActionCommand` locks the battle flow.
3. `ExecuteAttackSequence` faces attacker and target, starts Attack or Skill motion, and rolls the result early.
4. The code waits until `CombatActionTimeline.VfxTime`, then until `HitTime`.
5. `ApplyAttackResultAtHitFrame` applies damage, miss/guard, hit reaction, defeat state, and log output.
6. Camera shake and recovery wait run after the hit frame.
7. The actor spends the main action and returns to Acted/Wait state.

## Hit Frame Rule

Damage is not applied at button press. It is applied only by `ApplyAttackResultAtHitFrame`, which is called after the timeline reaches `WeaponAnimationSet.attackHitTime` or `skillHitTime`.

## Runtime Visual Layer

The current slice uses a procedural 2D SD controller rather than a final PSB rig. It already exposes the same states expected by an Animator pipeline:

- Idle
- SelectedIdle
- Move
- Attack
- Skill
- Hit
- Guard
- Defeat
- Victory
- Wait/Acted

Generated `.anim` clips exist as replaceable placeholders and timing references. Final production art can replace the procedural pose layer with PSD/PSB bones or authored Sprite animations while keeping `WeaponAnimationSet` timing and combat code intact.

## Test Scene

Use `Assets/JoseonMurimTactics/Scenes/BattleTest.unity`.
