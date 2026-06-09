# v1.0 Noncombat UI Plan

## Branch Status

- Working branch: `noncombat-ui-v1.0`
- Base loop: `story-start-v0.8` / game loop `v0.9`
- Scope: title, new game setup, prologue dialogue, battle prep/result UI, hub, mission board, world map, save/load, settings, UI structure.
- Excluded: `BattleTest` combat logic, `CombatResolver`, `SkillResolver`, `TurnManager`, combat result math.

## Current Loop

```text
Boot -> Title -> NewGameSetup -> Prologue -> BattlePrep -> BattleTest -> BattleResult -> Hub_Pyesadang -> MissionBoard -> BattlePrep
```

v1.0 keeps the existing IMGUI screens as a working prototype while adding the script-side foundation for Canvas/TextMeshPro screens. Scene flow and session state stay stable so art and prefab replacement can happen without touching combat code.

## Added Foundations

- `Assets/JoseonMurimTactics/Scripts/UI/Core`
- `GameSettings` backed by `PlayerPrefs`
- Save flow with one auto slot plus three manual slots
- Hub action points stored in session `intVars`
- World map node data for locked, mission, danger, completed, and occupied states

## Canvas/TMP Prefab Targets

Create these prefabs under `Assets/JoseonMurimTactics/UI/Prefabs` in the next Unity editor pass:

- `UI_Root`
- `Screen_Title`
- `Screen_NewGameSetup`
- `Screen_PrologueDialogue`
- `Screen_BattlePrep`
- `Screen_BattleResult`
- `Screen_Hub`
- `Screen_MissionBoard`
- `Screen_WorldMap`
- `Screen_Settings`
- `Modal_Confirm`
- `Popup_Toast`
- `Tooltip`
- `SaveSlotCard`
- `MissionCard`
- `CompanionCard`
- `FactionMeter`

Use placeholder `Image` panels and TMP text first. The visual direction is bright hanji, ink linework, teal/navy accents, red seal marks, and thin gold dividers.

## Data Migration Targets

Hardcoded strings should move toward ScriptableObject assets in:

- `Assets/JoseonMurimTactics/Data/Dialogues`
- `Assets/JoseonMurimTactics/Data/Missions`
- `Assets/JoseonMurimTactics/Data/Lore`
- `Assets/JoseonMurimTactics/Data/Companions`
- `Assets/JoseonMurimTactics/Data/HubLocations`
- `Assets/JoseonMurimTactics/Data/Settings`

Yarn Spinner can replace or import the current dialogue model later because node ids, speaker ids, choices, flags, approval deltas, reputation deltas, and scene commands are already separated.
