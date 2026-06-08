# SRPG Combat v0.5 Request

Primary goal: upgrade the Unity scaffold from initiative-based turn order to Fire Emblem-style phase SRPG combat.

## Highest Priority

- Replace unit-by-unit initiative flow with `PlayerPhase -> EnemyPhase -> next round`.
- Add `PhaseTurnController`.
- Add `UnitSelectionController`.
- Add `BattleForecastService` and a forecast panel.
- Add `CounterattackService`.
- Add `BreakResolver`.
- Add `ThreatRangeService`.
- Add `ObjectiveManager`.
- Add `EnemyTacticsAI`.

## Core Combat Direction

- Fire Emblem-style player unit selection in any order.
- Sword of Convallaria-style terrain decisions: elevation, cover, hazards, chokepoints.
- Musou/murim flavor: martial skill style matchups, break, morale/inner-power hooks.
- d20-based resolution, but always forecast before committing.

## v0.5 Must-Haves

- Battle forecast before attack:
  - required d20
  - hit percent
  - damage range
  - crit chance
  - break gain
  - style matchup
  - terrain bonus
  - counterattack status
- Style matchup MVP:
  - Sword beats Blade
  - Blade beats Spear
  - Spear beats Sword
  - advantage gives hit bonus and break gain
  - Broken blocks counterattack
- Counterattack rules:
  - target survives
  - target is not Broken / Disarmed / Prone
  - attacker is in counter range
  - defender has inner/cooldown/uses available
  - counter skill is allowed
- Tactical terrain:
  - bamboo forest: move cost, cover
  - shallow water: move cost, slippery/ice interaction
  - roof/high ground: attack bonus
  - cliff/ridge: cover and later fall hooks
  - bridge: chokepoint
  - smoke/fire/ice hazards

## Sample Battle

Battle: 압록강 폐사당 탈환전

Allies:
- Park Sungjun
- Yun Seohwa
- Baek Ryeon
- Han Biyeon
- Do Arin

Enemies:
- Central Swordsman x2
- Qingcheng Spearman x1
- Sichuan Poisoner x1
- Central Inspector x1

Objectives:
- Main: subdue Central Inspector.
- Bonus: win within 8 turns, preserve altar, subdue poisoner without killing.
- Defeat: Park Sungjun down, Baek Ryeon down, or round 12 exceeded.
