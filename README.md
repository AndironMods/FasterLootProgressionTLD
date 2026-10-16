# Faster Loot Progression

Adds a fully integrated "Looting" skill to The Long Dark's in-game Skills tab. Searching containers levels up the skill, and you can track your progress right in the Journal — styled to look just like the game's native skills.

As you level up, container searches get faster, all the way to instant looting at max level.

## Features

- Native-looking "Looting" entry in the Journal → Skills tab, with icon, level diamond, and progress bar
- Progress bar that fills toward each next level
- Detail panel with the skill's level, tier name, description, and image
- Displays the native Level Up Message at each level
- Five cumulative benefit tiers:
  - Level 1 — Basic looting skill
  - Level 2 — +25% faster looting
  - Level 3 — +50% faster looting
  - Level 4 — +75% faster looting
  - Level 5 — +100% faster looting (instant)
- Progress is saved per save slot (via ModData)
- The speed bonus applies to looting only: containers, safes, locked objects and rock caches (doors, harvesting, loading zones etc. stay vanilla)
- Tier names display in your game's language automatically
- Display-only — does not alter the game's skill system, so all vanilla skills behave normally

## Requirements

- The Long Dark 2.55
- MelonLoader v0.7.3
- ModData mod (stores your progress per save slot)

## Installation

1. Install MelonLoader if you haven't already (run the game once so it generates the `Mods` folder).
2. Install ModData into the same `Mods` folder.
3. Download this mod.
4. Copy `FasterLootProgression.dll` into:
   `...\steamapps\common\TheLongDark\Mods\`
5. Launch the game. Open your Journal and go to the Skills tab — "Looting" will appear at the bottom of the skill list.

## Deinstallation

Delete `FasterLootProgression.dll` from your `Mods` folder.
