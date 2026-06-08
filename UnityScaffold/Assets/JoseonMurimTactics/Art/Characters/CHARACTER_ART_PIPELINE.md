# Character Art Pipeline

The current HTML tokens are only placeholders. Unity should use polished adult 2D character sprites with SpriteRenderer, 2D Animation, and subtle presentation motion.

## Generated Source Assets

- `Source/adult_murim_lineup_chroma.png`
  - Original generated lineup on chroma background.
- `Sprites/adult_murim_lineup_alpha.png`
  - Background-removed source sheet.
- `Sprites/Individuals/*_fullbody.png`
  - Cleaned individual full-body sprites ready for Unity import.
- `Sprites/individuals_preview.png`
  - Preview only, used to inspect crop quality.

## Unity Setup

1. Open the project in Unity `6000.3`.
2. Let Package Manager resolve the 2D packages:
   - 2D Animation
   - PSD Importer
   - SpriteShape
   - 2D Tilemap Extras
3. For each `*_fullbody.png`, create a `CharacterVisualData` asset.
4. Assign the PNG sprite to `fullBodySprite`.
5. Set `heightInTiles` around `1.1` to `1.25` for Random Chat-like one-tile character presence.
6. Add `CharacterVisualController` to the unit prefab and bind it through `CombatantData.visual`.

## Target Quality Bar

- Adult original characters only.
- Polished Korean 2D visual-novel style.
- Full-body sprites with expressive face, clean linework, layered clothing, and readable silhouette.
- No direct copying of existing Random Chat characters or files.
- Final production should replace single-sheet generation with layered PSD/PSB files, then rig with Unity 2D Animation bones.
