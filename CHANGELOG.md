# Changelog

## v1.3.0

- Progress is now stored per save slot via ModData (ModData is now required). Progress from the old global `skilldata.json` is imported once into the first save slot without data.
- Fixed the looting speed bonus being applied multiple times, so the real search times now match the documented tiers (-25% / -50% / -75% / instant).
- Fixed the speed bonus affecting doors, harvesting, loading zones, repairs and other hold interactions. It now only applies to looting: containers, safes, locked objects and rock caches.
- Fixed hold times shrinking further each time a search was cancelled and restarted.
- Container searches now award a point reliably once per container.
- Much quieter MelonLoader log.

## v1.2.0

- Use now the game's native skill notification system to display level-ups.

## v1.1.0

- Add custom looting icon in skill tree

## v1.0.0

- Initial release
