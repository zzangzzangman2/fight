# Battle HUD Polish Checklist

Target commit: post-b0d4503 follow-up.

## Runtime Visuals

- Confirm no `BattleCanvasHud` object appears during play.
- Confirm `UseLegacyOnGui` remains disabled and no IMGUI debug HUD is visible.
- Confirm phase ribbon is top-center, objective/help are top corners, selected-unit card is bottom-left, roster is bottom-center, and command dock is bottom-right.
- Confirm forecast, tooltip, log toast, and expanded log do not cover command buttons or the selected-unit card.
- Confirm command buttons are at least 72x64 logical pixels and display icon sprites for move, attack, skill, guard, terrain, and wait.

## Fonts

- Confirm titles, command labels, phase text, and numeric shortcuts use `MaplestoryOTFBold`.
- Confirm body/help/log/objective text uses `MaplestoryOTFLight`.
- Confirm a development warning is logged if either required font resource is missing.

## Resources

- Confirm panels load through `Resources/UI/BattleHUD` aliases: `ui_battle_panel_ink_glass_9slice`, `ui_battle_panel_phase_ribbon_9slice`, `ui_battle_panel_forecast_9slice`, and button-state aliases.
- Confirm gauges load `ui_hp_bar_bg`, `ui_hp_bar_fill`, `ui_inner_bar_bg`, and `ui_inner_bar_fill`.
- Confirm there is no lime-green debug fallback visible when assets load normally.

## Input

- Confirm decorative `Image` and `Text` HUD objects have `raycastTarget=false`.
- Confirm `PointerOverHud` blocks clicks only on active `Selectable`, `Button`, `ScrollRect`, `InputField`, or HUD marker objects.
- Confirm clicking a reachable movement tile still moves the selected unit.

## Resolution Sweep

- Check 1280x720, 1600x900, 1920x1080, and 2560x1440.
- In each resolution, verify no text overlaps, command buttons remain clickable, and tooltip/log panels stay within screen bounds.
