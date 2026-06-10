# Test Swordsman Motion Checklist

Scene: `Assets/JoseonMurimTactics/Scenes/BattleTest.unity`

## Generated Assets

- Weapon set: `ScriptableObjects/Weapons/sword_motion_set.asset`
- Combat visual data: `ScriptableObjects/Characters/test_swordsman_combat_visual.asset`
- Unit prefab: `Prefabs/Units/test_swordsman_unit.prefab`
- VFX prefabs: slash, skill, guard
- Animation clips: idle, selected idle, walk, attack sword 01, skill sword 01, hit, guard, defeat, victory, acted

## Keyboard Tests

- `M`: move active unit to a reachable tile using path walk.
- `A`: basic attack nearest enemy.
- `S`: sword skill nearest enemy when an active unit exists. Without active unit, the old scout-mode shortcut remains.
- `H`: hit reaction.
- `G`: guard/parry pose.
- `D`: defeat pose.
- `V`: victory pose.

## Click Tests

1. Select an ally.
2. Click a reachable tile and confirm the unit walks tile-by-tile.
3. Select Attack and click an enemy.
4. Confirm slash motion starts before damage.
5. Confirm HP changes at the hit frame and target plays Hit or Guard.
6. Use Skill and confirm longer lunge, skill burst, hit frame, and camera shake.

## Placeholder Limits

- The current SD body is a single full-body sprite with procedural motion, not a final layered PSB rig.
- VFX are placeholder sprites/prefabs.
- Generated animation clips are timing/reference clips; runtime motion is still driven by `CharacterVisualController`.
- Projectile weapons need an additional projectile impact timeline before Bow, Talisman, or HiddenWeapon production use.

## Pass Criteria

- No teleporting during normal move.
- Damage is delayed until `HitFrame` log output.
- Attack/Skill/Guard/Hit/Defeat/Victory can be triggered in `BattleTest`.
- `WeaponAnimationSet` controls movement and attack timing.
