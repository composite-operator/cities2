# Compatibility policy

## Current matrix

| Mod | Version | Checked game version |
| --- | ---: | ---: |
| Map Editor Bridge | 0.4.12 | 1.6.2f1 |
| Stacklight | 0.2.7 | 1.6.2f1 |

For these releases, both backends built with the official toolchain against
the installed 1.6.2f1 assemblies. Both UIs passed TypeScript and webpack
checks. The Bridge bundle passed five registration cases. The Stacklight
release DLL passed ten verification checks, including the installed playset
query's return type. The publisher accepted both releases. The in-game checks
in steps 7 and 8 remain open.

## Forward compatibility

Unknown future game versions cannot be guaranteed. The projects reduce common
patch failures in these ways:

- Stacklight discovers the current asynchronous or legacy synchronous active-
  playset query at runtime. If neither API exists, logging continues and the
  Mods view reports that its context is unavailable.
- Map Editor Bridge requests stock UI modules through the official module
  registry. If a patch moves a module, it accepts one unambiguous match with
  the same file name and export. It applies the unlock at that resolved path.
  A rejected unlock leaves the stock grid in use and the editor hooks register.
- Both projects use the game's native bindings and placement tools instead of
  copying game state or save serialization.
- `GameVersion` remains `1.6.*` because that is the checked API family. Change
  the range only after a compile and subscribed-package smoke test on a later
  family.

## Required check after a game update

1. Confirm the exact version in the game's `version` file or `Game.dll`.
2. Run `npm ci`, `npm run check`, and `npm run build` in both `ui` folders.
3. Build both C# projects against the updated `Cities2_Data\Managed` directory.
4. Create release packages and verify their hashes and privacy scan. The
   Bridge builder tests the compiled registrar against stock, moved, ambiguous,
   absent, and rejected item grids. Run Stacklight's verification program
   against the release DLL and the installed managed assemblies.
5. Publish the new versions to their existing Paradox Mods IDs.
6. Remove publisher-created local mod copies.
7. Start the game with the subscribed packages and check `Player.log`,
   `MapEditorPlus.log`, and `Stacklight.log` for mod-owned errors.
8. In the Map Editor, verify construction menus, stock asset selection, road
   naming, and one reversible placement. In Stacklight, verify the Logs and
   Mods views and refresh the active playset.

Do not describe a later game family as compatible until these gates pass.
