# Visual Quality V1 Implementation Notes

## Implemented Code Foundation
- `CharacterVisualController` supports scene ambient tint, desaturation, two-stage shadows, fixed-direction cast shadow, ink rim, lower body ground blend, and foot occlusion.
- `BattleTestController` injects map-specific ambient and ground colors, creates a paper/vignette world overlay, unifies tactical cell collider shape, and narrows unit click hitboxes.
- `TeamCharacterAssetBuilder` uses one pose PPU value for ally and enemy generated pose sprites.
- `BattleImpactPresenter`, `BattleCameraFx`, `DamagePopupPresenter`, and `SimpleSpriteFlash` provide presentation-only combat hooks.

## Pending Asset Pass
Generated art should land in `Art/VisualUpgradeV1`. Use the prompts in `Docs/VisualUpgradeV1/imagegen_prompts.md`. Project-bound generated files must be copied from the image generation output area into the Unity project before references are added.

## Editor Workflow
Use:
- `Joseon Murim Tactics > Visual Upgrade V1 > Import Generated Sprites`
- `Joseon Murim Tactics > Visual Upgrade V1 > Apply To Current BattleTest`
- `Joseon Murim Tactics > Visual Upgrade V1 > Validate Visual Setup`

The installer applies import settings and creates baseline ScriptableObject assets. It does not rewrite battle rules.

## Validation Gates
- Unity batchmode compile has no `error CS`.
- `git diff --check` passes.
- `tools/Test-Utf8Text.ps1` passes.
- No conflict markers under `UnityScaffold/Assets`.
- Visual validator reports required folders, data assets, and BattleTest hooks present.

## No Visible Window Policy
For office-safe validation, use Unity `-batchmode -quit -nographics` and editor validators. Do not launch standalone players or capture visible windows unless explicitly approved.
