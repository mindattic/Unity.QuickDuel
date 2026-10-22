# Origins: Bushido Coda

QuickDuel is the core of an older game, "Bushido Coda" (the name now belongs to a book). Those
builds carried overworld maps, merchants, weather, items, stats, skill dials and a focus bar. Under
all of it was one moment: two samurai wait, and the faster strike wins. QuickDuel keeps only that.

## Versions found on R:\Projects (surveyed 2026-10-21)

| Version | Where | Tech |
| --- | --- | --- |
| BushidoCoda (8-29-11 to 9-1-11) | `_PRE_2017\BushidoCoda\8-29-11` ... | Android, Java |
| BushidoCodaDev (9-2-11 to 10-26-12) | `_PRE_2017\BushidoCoda\9-*`, `10-*`, `11-*` | Android, Java (maps, items, weather, conversations) |
| BushidoCodaNew (12-02-11 to 05-22-12) | `_PRE_2017\BushidoCoda\12-*`, `01-*`, `05-*` | Android, Java (`BattleSystem.java`) |
| BushidoCoda (06-13-12 to 03-15-13) | `_PRE_2017\BushidoCoda\06-13-12` ... `03-15-13` | Android, Java |
| Shipped APK | `KYOCERA RISE\bushidocoda\BushidoCoda.apk` (`dev.bushido.coda`) | Android package |
| gmlBushidoCoda (7-31-13 to 9-11-13) | `_PRE_2017\gmlBushidoCoda\*`, `GameMaker\gmlBushidoCoda`, `Game Maker Games\9-4-13` | GameMaker Studio (GML) |
| Windows build | `c336045\ryandebraal.com\bushidocoda\BushidoCoda.exe` | Native x86 PE, no CLI metadata (REA): the GameMaker runner, not XNA |
| Art, music, site | `_PRE_2017\Bushido Coda GFX`, `bcsite_old`, zipped snapshots under `c336045\ryandebraal.com` | — |

No XNA build was found under R:\Projects.

## The rules as the old code had them

From `gmlBushidoCoda\09-11-13_5.58am\scripts\gameclock_*.gml` and `BattleSystem.java`:

- **Startup** (3 s), then **countdown** 3-2-1 with a beep on each number.
- **Drumroll**: `irandom_range(3 * room_speed, 6.5 * room_speed)` — 3 to 6.5 seconds of waiting.
- **Action**: drumroll stops, unsheath sound, the arena colors invert. Reaction time counts from here.
- **Premature strike**: pressing during the drumroll loses the round outright. The Android build
  taunted the loser with one of "Premature Strike", "Stumbled", "Impetuous Attack", "Clumsy Move",
  "Hurried Action", "Jumped the Gun", "Overzealous", "Too Soon", "Too Early", "Fail".
- **Winner**: lower reaction time wins (`determine_winner.gml`).
- **Aftermath**: letterbox exits, slash sound, blood, the winner stands.

Dropped on purpose: stats (ART, LCK, PWR), focus, skills, combos, reaction penalties, the clash
button-mash, arenas and characters as gameplay. The old build ran the action phase at 1000 FPS to
time reactions by frame; QuickDuel times them by input event timestamp instead.
