# Weapon Animation Set Guide

`WeaponAnimationSet` is the data contract for weapon-specific combat motion.

## Main Fields

- `weaponType`: Sword, Spear, Bow, Fist, Dagger, Staff, Fan, Talisman, or HiddenWeapon.
- Clip references: Idle, SelectedIdle, Walk, Attack, Skill, Guard, Hit, Defeat, Victory, Acted.
- Move timing: `walkSecondsPerTile`, `moveSettleTime`.
- Attack timing: `attackDuration`, `skillDuration`, `attackVfxTime`, `attackHitTime`, `skillVfxTime`, `skillHitTime`, `recoveryTime`.
- VFX references: attack, skill, projectile, trail, impact, guard, footstep.
- Impact feel: attack/skill lunge distance, camera shake strength, camera shake duration.

## Add A New Weapon

1. Create a new `WeaponAnimationSet` asset under `Assets/JoseonMurimTactics/ScriptableObjects/Weapons/`.
2. Set `weaponType` to the new weapon category.
3. Assign motion clips and VFX prefabs.
4. Tune hit/vfx/recovery times so damage lands on the visible impact frame.
5. Assign the asset to a character's `CharacterVisualData.weaponAnimationSet`.

## Weapon Notes

- Spear: longer thrust timing, larger forward reach, straight impact VFX.
- Bow: add projectile prefab and delay damage until projectile impact in a later pass.
- Fist: fast hit times, small lunge, strong impact burst.
- Dagger: short duration and quick recovery.
- Staff/Fan: wider VFX and possible area pattern expansion.
- Talisman/HiddenWeapon: projectile or thrown-object spawn timing should use `OnProjectileSpawnFrame` or a timeline extension.

## Add A New Character

1. Add character art under `Art/Characters/<CharacterId>/`.
2. Create or duplicate `CharacterVisualData`.
3. Assign full body, bust, face icon, default weapon type, and weapon animation set.
4. Create a combat visual data asset if the character needs prefab/portrait packaging.
5. Place the character in a battle unit definition or prefab.

## Replace Placeholder Art

The current sword user is a full-body sprite plus procedural SD pose. For production:

1. Import layered PSD/PSB or a sprite sheet.
2. Preserve sockets such as Root, FullBody, Weapon, Feet, and target center equivalents.
3. Rebuild real animation clips with the same clip names or reassign them in `WeaponAnimationSet`.
4. Re-tune hit frame fields after the authored animation is visible in scene.
