# Codex Visual Upgrade Handoff

Last updated: 2026-06-12
Branch: `map-quality-v1.2`
Project: `UnityScaffold`

## Current State

- Remote `origin/map-quality-v1.2` was fetched and local branch was clean before this handoff file was added.
- Previous visual/blending work is already pushed in commit `c8498d8` (`Add companion recruitment visit flow and visual assets`).
- No visible Unity/player window was launched during the company-side work. All Unity checks were batch-only.

## Completed

- PowerShell Korean/UTF-8 handling was fixed via the user profile so future PowerShell reads/writes should not corrupt Korean text.
- Battle character scene blending was implemented:
  - map ambient tint and desaturation
  - softer ink rim
  - two-layer ground shadow
  - fixed-direction cast shadow
  - ground contact blend
  - full-screen paper texture/vignette overlay
- Tactical tile click collider generation was unified through a shared isometric collider helper.
- Unit click collider was narrowed/raised to reduce accidental tile hits.
- Enemy and ally PPU were unified in the character asset builder.
- Battle presentation hooks were added:
  - `BattleCameraFx`
  - `DamagePopupPresenter`
  - `BattleImpactPresenter`
  - `SimpleSpriteFlash`
- Visual data assets and editor tooling were added:
  - `BattleVisualProfile`
  - `BattleVfxLibrary`
  - `BattleUiSkinData`
  - `VisualUpgradeV1Installer`
  - `VisualUpgradeV1Validator`
- Generated VisualUpgradeV1 assets were imported:
  - 2 concept images
  - 12 tiles
  - 16 props
  - 12 UI sprites
  - 8 VFX sheets
  - 32 sliced VFX frame sprites
  - 10 character full-body pilot/reference sprites
  - 5 portraits
  - Park Sungjun corrected v2 pilot sprite
- Park Sungjun naming was corrected to `park_sungjun`; do not reintroduce `park_sungjoon`.
- Park Sungjun visual rule was corrected:
  - 17 years old
  - short black hair
  - clean-shaven
  - no beard, no mustache, no stubble
  - use existing project sprite as the primary reference
- The prompt guide now records the Park Sungjun v2 correction and warns not to one-off replace only one character.

## Validation Already Run

Unity path used:

```powershell
C:\Program Files\Unity\Hub\Editor\6000.4.9f1\Editor\Unity.exe
```

Batch import:

```powershell
Unity.exe -batchmode -quit -nographics -projectPath C:\Users\godho\Downloads\fight\UnityScaffold -executeMethod JoseonMurimTactics.Editor.VisualUpgradeV1Installer.ImportGeneratedSprites
```

Batch compile:

```powershell
Unity.exe -batchmode -quit -nographics -projectPath C:\Users\godho\Downloads\fight\UnityScaffold
```

Visual validator:

```powershell
Unity.exe -batchmode -quit -nographics -projectPath C:\Users\godho\Downloads\fight\UnityScaffold -executeMethod JoseonMurimTactics.Editor.VisualUpgradeV1Validator.ValidateVisualSetup
```

Result:

- C# compile: passed.
- Visual validator: passed.
- Validator found 98 generated VisualUpgradeV1 sprites.
- All 8 VFX clips were linked.
- All 12 UI skin sprites were linked.
- BattleTest scene hooks existed for camera FX, damage popups, and impact presenter.
- `git diff --check` had no whitespace errors after trimming Unity-generated layer meta whitespace. Existing CRLF warnings may appear for previously touched content-authoring files.

## Important Caution

- Do not launch visible Unity/player while at work. At home, visible Play Mode/screenshots are needed for final visual QA.
- Do not do a full URP 2D lighting conversion for this request. The follow-up explicitly said not to expand scope that way.
- Do not replace only Park Sungjun in runtime. If character sprites are replaced, regenerate and integrate the whole party/enemy batch together.
- Current full-body character images under `Art/VisualUpgradeV1/Characters` are pilot/reference assets. They are not a complete one-to-one replacement for every existing animation layer.
- Park Sungjun v2 is better for age/hair/facial-hair correctness, but still should be treated as a pilot until the full character batch matches existing compact SD proportions.

## Next Tasks

1. At home, open Unity visibly and inspect `BattleTest` in Play Mode.
2. Capture/compare the first battle screen at normal gameplay zoom:
   - character grounding
   - shadow direction
   - paper/vignette strength
   - move/attack/danger highlight readability
   - UI panel readability
3. Confirm the generated VisualUpgradeV1 tiles/props are actually used in the visible battlefield, not only stored in ScriptableObject profiles.
4. If the map still looks too close to the old test board, use the generated tile/prop set to rebuild the visible BattleTest tilemap composition:
   - snowy ground variation
   - ruined gate
   - stone stair chokepoint
   - frozen stream/cracked ice
   - cliff edge/height read
   - torches
   - pine line-of-sight blockers
   - cover rocks
   - interactable props
5. Verify combat presentation in Play Mode:
   - attack start focus
   - hit shake
   - hit flash
   - damage popup
   - counter flash/icon
   - at least 4 VFX types visible in real combat
6. Decide character strategy:
   - short-term: keep existing compact SD runtime sprites and rely on blending.
   - full fix: regenerate all party/enemy animation-layer sprites as a consistent compact SD batch with matching pivot/PPU/canvas.
7. If doing full character regeneration, start with one Park Sungjun idle replacement only as a visual test, then expand to everyone only after the in-map comparison works.
8. Run final checks again:

```powershell
git diff --check
rg -n "<<<<<<<|=======|>>>>>>>" UnityScaffold/Assets
Unity.exe -batchmode -quit -nographics -projectPath C:\Users\godho\Downloads\fight\UnityScaffold
Unity.exe -batchmode -quit -nographics -projectPath C:\Users\godho\Downloads\fight\UnityScaffold -executeMethod JoseonMurimTactics.Editor.VisualUpgradeV1Validator.ValidateVisualSetup
```

## Key Files

- `UnityScaffold/Assets/JoseonMurimTactics/Scripts/Presentation/CharacterVisualController.cs`
- `UnityScaffold/Assets/JoseonMurimTactics/Scripts/Presentation/BattleTestController.cs`
- `UnityScaffold/Assets/JoseonMurimTactics/Scripts/Presentation/BattleUnitGroundingStabilizer.cs`
- `UnityScaffold/Assets/JoseonMurimTactics/Scripts/Presentation/Visuals/`
- `UnityScaffold/Assets/JoseonMurimTactics/Scripts/Editor/VisualUpgradeV1Installer.cs`
- `UnityScaffold/Assets/JoseonMurimTactics/Scripts/Editor/VisualUpgradeV1Validator.cs`
- `UnityScaffold/Assets/JoseonMurimTactics/Art/VisualUpgradeV1/`
- `UnityScaffold/Assets/JoseonMurimTactics/ScriptableObjects/Visuals/`
- `UnityScaffold/Assets/JoseonMurimTactics/Docs/VisualUpgradeV1/imagegen_prompts.md`

## Park Sungjun Reference Rule

Use this exactly for future image generation:

```text
Park Sungjun is a clean-shaven 17-year-old boy hero with short tousled black hair and blue-gray eyes.
Primary reference is the existing project `park_sungjun` compact SD sprite, not a tall generated standee.
No beard, no mustache, no stubble, no long hair, no adult facial structure.
```

