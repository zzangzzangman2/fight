# Living 2D Battle Character Pipeline

## Direction

Model-based battle characters are intentionally not part of the current battle pipeline. Battle units stay on the existing 2D `CharacterVisualController` path, and new work should improve the feel of the 2D sprites instead of bringing back model assets or renderer code.

The current priority is:

- Keep battle sprites grounded with a separate shadow renderer.
- Make selection, turn start, movement, attack, hit, guard, wait, low HP, and victory states react visibly.
- Keep all new data optional so existing `CharacterVisualData` assets continue to work.
- Prefer transparent SD battle sprites with clean silhouettes and bottom-center feet.

## Runtime Additions

- `CharacterLivingMotionProfile` stores optional per-character motion tuning.
- `CharacterSpriteAnimationClipData` can replace raw sprite arrays when a proper clip asset exists.
- `CharacterVisualData` now has optional Living 2D clip/profile/emotion fields.
- `CharacterVisualController` still supports existing pose sprites and `Sprite[]` arrays as fallback.

Fallback order for a state is:

1. Optional `CharacterSpriteAnimationClipData`
2. Existing pose frame array
3. Existing single pose sprite
4. Full body/bust/portrait fallback

## Battle Test Hooks

- Direct ally selection calls `PlayClickReaction()`.
- The first active ally and the next active ally after an action call `PlayTurnStart()`.
- HP ratio at or below 25% calls `PlayLowHp(true)`.

## Sprite Cleanup

The six main-character `Source/*_battle_sheet.png` files were rebuilt from existing transparent per-frame `Sprites` PNGs. This removes the old opaque white/gray sheet backgrounds while preserving the actual frame art and effects.

Runtime `Sprites/*.png` were audited separately. The remaining white pixels on visible battle sprites are mostly intentional slash, ice, flower, or light effects rather than connected background plates.

## Guardrails

- Do not reintroduce model battle assets, imported mesh battle visuals, or model render controllers.
- Do not change battle movement rules, damage formulas, progression, world map, or dialogue systems as part of visual polish.
- Keep Unity builds passing with `play-windows-player-preview.cmd --build-only`.
