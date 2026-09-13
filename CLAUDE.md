# FasterLootProgression

MelonLoader-Mod für *The Long Dark*. Fügt dem Journal ein neues "Looting"-Skill
hinzu, das nativ wie die eingebauten Skills aussieht und mit steigendem Level
das Durchsuchen von Containern beschleunigt (bis hin zu sofortigem Looten auf
Stufe 5).

Details zu Features und Installation stehen in [README.md](README.md),
Versionshistorie in [CHANGELOG.md](CHANGELOG.md).

## Tech-Stack

- .NET 6.0, C# (`LangVersion latest`), `AllowUnsafeBlocks`, `PlatformTarget x64`
- MelonLoader (Mod-Loader) + Harmony (0Harmony) für Runtime-Patching
- Il2CppInterop, da *The Long Dark* als IL2CPP-Build läuft (kein normales
  Mono-Reflection möglich — Zugriffe auf Spielklassen laufen über die
  Il2Cpp-Interop-Typen)
- Zielspiel: The Long Dark 2.55, benötigt MelonLoader v0.7.3

## Projektstruktur

- `FasterLootProgression.cs` — MelonMod-Einstiegspunkt (OnInitialize/Harmony-Patches)
- `LootProgressionSkill.cs` — Definition des Looting-Skills (Level, Tiers, Namen/Beschreibungen)
- `LootProgressionSkillManager.cs` — Verwaltung von Skill-Zustand/Fortschritt
- `LootProgressionUI.cs` — Integration in die native Journal/Skills-UI (Icon, Levelraute, Progressbar, Detailpanel)
- `Lootlevelupnotifier.cs` — Nutzt das native Level-Up-Benachrichtigungssystem des Spiels
- `Resources/` — eingebettete Icons (`two-arrows-up1.png`, `two-arrows-up2.png`)
- `MelonLoader_Libs/` — **nicht im Repo** (siehe `.gitignore`). Enthält
  MelonLoader-, Harmony- und Spiel-DLLs (u. a. `Assembly-CSharp.dll`), die als
  `HintPath`-Referenzen im `.csproj` gebraucht werden, aber nicht
  weiterverteilt werden. Muss lokal aus einer MelonLoader-Installation bzw.
  dem Spielverzeichnis befüllt werden, bevor der Build funktioniert.

## Build

```
dotnet build FasterLootProgression.csproj
```

Voraussetzung: `MelonLoader_Libs/` lokal mit den passenden DLLs aus der
MelonLoader-Installation und dem Spielverzeichnis (`Assembly-CSharp.dll`,
`Il2Cppmscorlib.dll`, `UnityEngine.*`, `Unity.Addressables.dll`) befüllen.

Das Ergebnis (`FasterLootProgression.dll`) wird zum Testen nach
`...\steamapps\common\TheLongDark\Mods\` kopiert.

## Arbeiten mit dem Code

- Es handelt sich um IL2CPP-Interop-Code: Spieltypen werden über generierte
  Wrapper (`Il2Cpp*`) angesprochen, nicht direkt reflektiert. Bei neuen
  Patches auf bestehende Muster in `FasterLootProgression.cs` achten.
- Die UI-Integration in `LootProgressionUI.cs` spiegelt bewusst das Aussehen
  der nativen Skills-UI — Änderungen hier sollten visuell konsistent mit den
  vanilla Skill-Einträgen bleiben.
- Änderungen an sichtbarem Verhalten/Version bitte in `CHANGELOG.md`
  dokumentieren (Format: `## vX.Y.Z` + Stichpunkte).
- Der Mod ist rein additiv/"display-only" bzgl. des vanilla Skill-Systems —
  keine Änderungen an den nativen Skills selbst vornehmen.
