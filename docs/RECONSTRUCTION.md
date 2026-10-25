---
codex: 1
project: Unity.QuickDuel
code: UQD
layer: reconstruction
status: living
updated: 2026-10-24
---

# Unity.QuickDuel - Reconstruction (REA of the original)

> What the original GameMaker prototype was, object by object, and where each piece stands in this rebuild.
> Audit ticket UQD-T26 (owner 2026-10-23). Rules and laws live in the [BIBLE](BIBLE.md); the history of the
> old builds lives in [ORIGINS](ORIGINS.md). Build the tickets below in order and the repo is a complete,
> polished proof of concept of the original.

## 1. The original

| Item | Value |
| --- | --- |
| Source | `R:\Projects\_PRE_2022\_PRE_2021\_PRE_2020\GameMaker\gmlBushidoCoda\BushidoCoda.gmx` (GameMaker Studio 1.x, GML, 7-31-13 to 9-11-13; a second copy sits under `_PRE_2017\gmlBushidoCoda`) |
| Size | 10 rooms, 640 x 480 at 60 fps (`room0` at 30), about 110 objects, 52 scripts, about 250 sprites, 8 backgrounds, 5 fonts, 23 sounds, 1 data file (`credits.txt`) |
| Time | Frame counted: normal play at 60 fps, the action phase at 1000 fps (`room_speed = actionFPS`), so a reaction was the number of 1 ms ticks since the cue |
| Dropped on purpose | Stats and skills, focus and combos, reaction penalty, the clash, the overworld and travel, character select, conversations and the talk box, health and virtue screens (see [BIBLE section 3](BIBLE.md#UQD-§3)) |

### 1.1 The round (`obj_gameclock`, `gameclock_*.gml`)

| Phase (`mode`) | Original behaviour | Rebuild |
| --- | --- | --- |
| `startup` | 3 s (`startup_time = 3 * room_speed`) while the loading fade clears and both fighters fade in idle; the first key press skips to `countdown` | Ready phase, waits for Space / Start (deliberate) |
| `countdown` | 4 s (`countdown_time`): "Get Ready" for the first quarter, then 3, 2, 1 at 25%, 50%, 75%, a beep on each, each number fading at 0.02 alpha per tick | Matches: 4 s countdown, Get Ready above the number throughout, then 3-2-1 with a beep, each fading at 0.02 alpha a 60 fps step (UQD-T57, UQD-T37) |
| `drumroll` | `irandom_range(3 * room_speed, 6.5 * room_speed)` ticks, drumroll sound, `obj_letterbox` closes in 1 px per tick to 12% of the height | Same 3 to 6.5 s, drumroll, letterbox |
| `action` | Drumroll stops, unsheath sound, `arena.inverted = true` (blend-mode invert of the whole backdrop), fighters switch to run sprites, a counting-up number shows, the computer opponent strikes at `ART + irandom(-LCK, LCK)` ticks | Cue, a real colour invert of the arena backdrop (UQD-T33), unsheath, adaptive computer replaced by Easy / Normal / Hard ranges |
| `clash` | Reactions within 3 ticks start a button-mash bar (`obj_clash`, `action_clash`) | Dropped on purpose; a dead heat is a tie that scores nobody |
| `aftermath` | Letterbox exits, slash sound, blood emitter, 6 to 12 splatters, white flash, `determine_winner`, loser's weapon(s) thrown, winner reaction number slides, 3 s, then a loading-art fade and the next `startup` | Slash, loser falls, reaction times and headline; no blood, splatter, flash, weapons or fade |
| Premature strike | `action_attack` during `drumroll`: the striker loses outright (`reaction_time = opponent ART + 1 to 50`), "Premature Strike!" shown | Early strike loses; Android taunt list shown (UQD-T6) |
| Strike during `countdown` | Ignored (no handler outside `drumroll` and `action`) | Fixed to match (UQD-T22) |
| Winner | Lower `reaction_time + reaction_penalty`; equal is a draw (nothing happens) | `DuelJudge` (lower wins, tie scores nobody) |
| Match end | An HP bar per side, damage from the reaction gap; 0 HP goes to `gameover` or `victory` | Best of 3 verdict (deliberate) |

### 1.2 Input (`obj_game`)

| Input | Original | Rebuild |
| --- | --- | --- |
| Space or left mouse | Strike for the left fighter in single player; skips `startup` | Space / Start next round; mouse and touch strike for the left duelist (UQD-T25) |
| Left / Right Ctrl | The two strikes in Versus | A / L, and one gamepad each (deliberate) |
| F1 | Toggle fullscreen | Missing (UQD-T50) |
| F12 | Toggle the debug overlay (frame counter, both reaction numbers, fps) | Missing (UQD-T50) |
| R | Go to the `location` map | Dropped |
| Escape | Back to character select (menu: `game_end`) | Quits the player (UQD-T23) |

## 2. Inventory and disposition

Disposition key: **built** (in the rebuild), **missing** (a ticket builds it), **dropped** (deliberately not rebuilt, with the reason).

### 2.1 Rooms

| Room | Contents | Disposition |
| --- | --- | --- |
| `intro` | `obj_intro`, four cloud layers x3, `obj_typewriter_intro`, `obj_fade_intro` | missing, UQD-T44 (the Ready screen stands in) |
| `mainmenu` | `obj_title`, buttons New Game, Versus, Options, Quit, Credits, Scores, `obj_mainmenu`, `obj_mouse`, `obj_fade_mainmenu` | missing, UQD-T45, T46, T47 (Options, Scores were empty stubs: dropped) |
| `gamescreen` | `obj_jukebox`, `obj_game`, corner arts, `obj_mouse`, `obj_fade_gamescreen`, `obj_letterbox` | partly built; remainder UQD-T33 to T43 |
| `victory`, `gameover` | `obj_victory`, `obj_virtue` x7, retry and continue buttons | missing, UQD-T48 (verdict screen; HP and virtues dropped) |
| `characterselect`, `room2` | `obj_characterselect`, five `obj_btn_<fighter>`, `obj_doll`, `obj_descbox`, typewriter | dropped: choosing a fighter (fighters become cosmetic, UQD-T34) |
| `location`, `travel` | `obj_location`, `obj_map`, `obj_walker`, `obj_travel`, `obj_talkbox`, `obj_dialogwriter`, portraits | dropped: overworld |
| `room0` | `obj_game` only, 30 fps test room, not in the room order | dropped |

### 2.2 Objects

| Group | Objects | Disposition |
| --- | --- | --- |
| Round | `obj_game`, `obj_gameclock`, `obj_letterbox`, `obj_arena`, `obj_player` | built as `Duel`, `DuelSession`, `DuelView` (letterbox, arena backdrop and its invert, UQD-T33), `obj_player` as the cosmetic fighters standing, facing and fading in (UQD-T34) and running, creeping, dashing and falling (UQD-T35, `FighterMotion`) |
| Aftermath | `obj_blood_emitter`, `obj_blood_drop`, `obj_splatter`, `obj_flash`, `obj_weapon` | missing, UQD-T40, T41 |
| Transitions | `obj_fade`, `obj_fade_gamescreen`, `_intro`, `_mainmenu`, `_characterselect`, `_location`, `_travel` | gamescreen, intro and mainmenu fades missing, UQD-T42, T44, T45; the rest dropped |
| Frame | `obj_cornerart_left`, `obj_cornerart_right`, `obj_title`, `obj_mouse` (katana cursor), `obj_time` (clock) | missing, UQD-T43, T45, T46, T50 |
| Intro | `obj_intro`, `obj_typewriter_intro`, `obj_cloud1` to `4`, `obj_cloud_parent` | missing, UQD-T44 |
| Menu | `obj_btn_newgame`, `_versus`, `_quit`, `_credits`, `_options`, `_scores`, `_start`, `_retry`, `_nextbattle` | New Game, Versus, Quit, Credits, Retry: UQD-T45, T47, T48; Options, Scores, Start: dropped |
| Ambient | `obj_sand_emitter` / `obj_sand`, `obj_butterfly_emitter` / `obj_butterfly`, `obj_snow_emitter` / `obj_snow`, `obj_fog_emitter` / `obj_fog`, `obj_fly_emitter` / `obj_fly`, `obj_rain_emitter` / `obj_raindrop`, `obj_leaf_emitter`, `obj_firefly_emitter` / `obj_firefly`, `obj_tree_emitter` / `obj_tree1`, `obj_lens`, `obj_spotlight` | missing, UQD-T49 (rain, leaf, firefly, tree, lens, spotlight were unused or location-only: optional) |
| Clash | `obj_clash` | dropped |
| Stats and combat | `obj_healthbar`, `obj_focusbar`, `obj_focusmeter`, `obj_focusarrow`, `obj_meterarrow`, `obj_artmeter`, `obj_skill`, `obj_skills`, `obj_stances`, `obj_combo`, `obj_buff`, `obj_debufficon`, `obj_criticalhit`, `obj_counter`, `obj_float_text`, `obj_flash_text` | dropped: stats, focus, skills, combos |
| Overworld and talk | `obj_location`, `obj_locationart`, `obj_map`, `obj_walker`, `obj_travel`, `obj_talkbox`, `obj_textdone`, `obj_dialogwriter`, `obj_portrait_left` / `right`, `obj_descbox`, `obj_virtue`, `obj_selectbox`, `obj_doll`, `obj_characterselect`, `obj_typewriter_location` / `characterselect` | dropped |
| Audio | `obj_jukebox` | dropped: its script `play_random_music` is entirely commented out, so the original plays no music |
| Scratch | `object39`, `object57`, `object66`, `obj_lens` | dropped: empty or unused |

### 2.3 Scripts (52)

| Script | Disposition |
| --- | --- |
| `gameclock_startup`, `_countdown`, `_drumroll`, `_action`, `_aftermath` | built in `Duel` and `DuelView`; presentation remainder UQD-T37 to T43 |
| `gameclock_clash`, `action_clash` | dropped |
| `action_attack`, `determine_winner` | built as `Duel.Strike`, `DuelJudge` (the early-strike rule matches; the clash branch dropped) |
| `add_reaction`, `calculate_art`, `random_luck` | dropped (adaptive AI replaced by `AiOpponent` ranges) |
| `set_state_idle`, `_running`, `_action`, `_attacking`, `_defeat` | built as `FighterMotion` (idle UQD-T34; running, attacking and defeat UQD-T35); `set_state_action` is dead code (never called) and is not rebuilt |
| `spawn_weapons` | missing, UQD-T41 |
| `load_arenas`, `choose_random_arena`, `setup_arena_properties` | `load_arenas` is `Data/assets.json`'s arenas, `choose_random_arena` is `ArenaDraw` (per round, all 11, UQD-T33); `setup_arena_properties` missing, UQD-T49 |
| `load_characters`, `choose_opponent`, `set_character_description` | `load_characters` is `Data/assets.json`'s fighters (name and sprites only), `choose_opponent` is `FighterDraw` (both seats, drawn per match, since character select is dropped) (UQD-T34); stats, skills, portraits and descriptions dropped |
| `load_text`, `init_text`, `process_text`, `dialog`, `mouseover_message_draw` | intro text missing UQD-T44; the rest dropped |
| `draw_text_outline`, `_alpha`, `_color`, `add_shadow`, `draw_light`, `create_lens`, `RGB` | outline text built as `OutlinedText` and `OriginalType` (UQD-T36); lighting dropped |
| `show_float_text`, `_color` | dropped (damage numbers) |
| `calculate_damage`, `check_regen`, `focus_percent`, `skill_*`, `reset_combos`, `reset_selectboxes` | dropped |
| `ai_fly`, `ai_firefly`, `ai_butterfly` | missing, UQD-T49 |
| `difference_between`, `play_random_music`, `globals`, `script9` | globals: constants carried in settings (UQD-T28); the rest dropped |

### 2.4 Sprites, backgrounds, fonts, sounds, data

| Group | Assets | Disposition |
| --- | --- | --- |
| Fighters | `spr_character_<exile,gypsy,myrmidon,ronin,vagabond>_<idle,run,attack,defeat>` (idle and run 1 frame, attack and defeat 4), weapons and ground weapons, Myrmidon shield, portraits, profiles, icons, silhouettes | idle, run, attack, defeat, weapons: imported (UQD-T32), named per fighter in `Data/assets.json`, idle drawn (UQD-T34), run, attack and defeat drawn (UQD-T35), weapons by T41; portraits, profiles, icons, silhouettes: dropped |
| Arenas | 17 `spr_arena_*` sprites (11 named in `load_arenas`), `arena_desert` background, `bg_dojo`, `bg_panorama1` | the 11 `load_arenas.gml` arenas imported with their names and descriptions (UQD-T32), drawn by UQD-T33, one per round, behind everything. Fixed defect (UQD-T33): `load_arenas.gml` gives Sozza's Point and South Ridge the same id (5), so South Ridge overwrote Sozza's Point and only 10 arenas could be chosen; all 11 are drawn now |
| Transitions | `spr_loading1` to `14` | imported (UQD-T32), drawn by UQD-T42 |
| Aftermath | `spr_blood_drop1`, `2`, `spr_splatter1` to `10`, `spr_flash`, `bg_splatter_scroll`, `spr_white` | blood drops, splatters, flash imported (UQD-T32), drawn by UQD-T40 |
| Frame and menu | `spr_titlescreen`, `spr_cornerart_left`, `left2`, `right`, `right2`, `spr_btn_left1` to `4`, `spr_btn_right1` to `4` (each with `_over`), `spr_katana_cursor`, `bg_black` | title, corner arts, cursor imported (UQD-T32), drawn by UQD-T43, T45, T46 |
| Intro | `spr_cloud1` to `4` | missing, UQD-T44 |
| Ambient | `spr_sand1`, `2`, `spr_butterfly` (6), `spr_snow1`, `2`, `spr_fly`, `fly2`, `spr_firefly`, `dark`, `strip`, `spr_rain`, `raindrop`, `rainemitter`, `spr_leaf1`, `2`, `spr_tree1`, `spr_spotlight` | sand, butterfly, snow, fog (`spr_cloud1`), fly: UQD-T49; the rest optional |
| Results | `bg_victory`, `bg_gameover`, `spr_virtue_*` (7) | backgrounds UQD-T48; virtues dropped |
| Stats and combat UI | health, focus, art, skill, stance, combo, clash, critical-hit, buff, descbox, selectbox, talk and map sprites | dropped |
| Fonts | `fnt_basic`, `fnt_mono`, `fnt_oldenglish`, `fnt_oldenglish_small`, `fnt_tiny` (ttf: oldenglish, sansation, cyberia, gnuolane, infinity, junction) | the five bitmap fonts (atlas and glyph table) and `oldenglish.ttf` imported (UQD-T32); the five bitmap fonts built as UI Toolkit font assets and drawn as outlined text (UQD-T36); the other ttfs came from the Android builds and are unused |
| Sound: duel | `sfx_beep`, `sfx_drumroll`, `sfx_unsheath`, `sfx_slash`, `sfx_victory`, `sfx_defeat` | built (UQD-T5, T24) |
| Sound: aftermath and menu | `sfx_hitground1` to `4`, `sfx_flick`, `sfx_click` | imported (UQD-T32), played by UQD-T41, T45 |
| Sound: dropped | `sfx_disabled`, `sfx_bomb`, `sfx_powerup_activate` (focus and skills); `music_*` x8 (no script plays them) | dropped |
| Data | `datafiles/credits.txt` (arena photo credits) | imported as `Data/credits.txt` (UQD-T32), shown by UQD-T47 |
| Project | `bushidocoda.ico`, splash images, `help.rtf`, `html` / `Install` web and installer pages | icon and product name: UQD-T50; the rest dropped |

## 3. The audit result

### 3.1 What the rebuild has

The engine-free duel (phases, random standoff, early strike, settle window, timeout, `DuelJudge`, `Match`,
`AiOpponent`, online host and client), two-player input by event timestamp, the Input System, the generated
Duel scene, countdown, letterbox, cue invert, taunts, the original six duel sounds, a best-of-3 scoreboard,
Escape quit, mouse and touch strike, a Windows build, online protocol and transport, 30+ Core tests and
PlayMode tests.

### 3.2 What it lacks

**Everything visual.** The scene draws coloured boxes and the built-in legacy font: no art asset is imported,
there are no arenas, no fighters (no run, attack or defeat), no blood, splatter, flash or dropped weapons,
no round fade, no intro, no main menu, no cursor, no credits, no ambient effects, no victory or game over
screen, no fullscreen toggle. Of the original's 27 duel sounds only six are present.

**Standards.** Measured against AGENTS.md and the shared Unity standards:

| Area | Finding |
| --- | --- |
| Assembly definitions | Good: Core (`noEngineReferences`), Runtime, Editor, Online (version-define gated), PlayMode and Online tests, one-way references. Gap: no EditMode test asmdef (UQD-T51) |
| Engine-free Core | Rules are in Core with 30+ NUnit tests, but `DuelSession` (mode, difficulty, match reset, AI wiring) lives in the MonoBehaviour layer, and `Duel.Strike` takes no command or player id (UQD-T30) |
| Input System | Good: `InputAction`s with event timestamps, gamepad per side |
| URP | Missing: no render pipeline package (UQD-T29) |
| Unity.Shared profiles and settings | Partly done (UQD-T28): `com.mindattic.unity.shared` is in the manifest and every tuning number is in `tuning.json` (named profiles, validated on load); the registry is the game's own Core code, and wiring player profiles through Unity.Shared comes with the settings screens |
| UI | Legacy uGUI `Text` with `LegacyRuntime.ttf`; the standard is UI Toolkit or the shared components (UQD-T31) |
| Tests | Core is covered; Editor generators and the new scenes are not (UQD-T51); CI runs only `dotnet test Core.slnx` (UQD-T52) |
| README and BIBLE | README lists only Core, Tests and docs and none of the Runtime, Online or Editor layers; BIBLE section 3 and 7 need the cosmetic fighters and parity scenes (UQD-T53) |
| Scene built in code | Good: `DuelSceneBuilder` generates the scene and `DuelView` builds the UI |

## 4. Build order

Each ticket requires the one before it (linked in the ticket database). Standards that change how everything
after them is built come first; the final ticket verifies the whole.

| Order | Ticket | Story | What |
| --- | --- | --- | --- |
| 1 | UQD-T28 | UQD-US-S1 | Unity.Shared settings registry and profiles; tuning as data |
| 2 | UQD-T29 | UQD-US-S2 | URP behind a pipeline-agnostic layer |
| 3 | UQD-T30 | UQD-US-S3 | Session, match and commands (with player id) into Core |
| 4 | UQD-T31 | UQD-US-S4 | UI Toolkit (or shared components) instead of legacy uGUI |
| 5 | UQD-T32 | UQD-US-P1 | Art, audio and font asset pipeline |
| 6 | UQD-T33 | UQD-US-P2 | The 11 arenas; real invert |
| 7 | UQD-T34 | UQD-US-P3 | Cosmetic fighters on the field |
| 8 | UQD-T35 | UQD-US-P4 | Run, attack and defeat motion |
| 9 | UQD-T36 | UQD-US-P5 | Original fonts and outlined text |
| 10 | UQD-T37 | UQD-US-P6 | Get Ready and fading 3-2-1 |
| 11 | UQD-T38 | UQD-US-P7 | Live reaction counter |
| 12 | UQD-T39 | UQD-US-P8 | Winner banner and sliding reaction |
| 13 | UQD-T40 | UQD-US-P9 | Blood, splatter and flash |
| 14 | UQD-T41 | UQD-US-P10 | Weapon toss and hit-ground sounds |
| 15 | UQD-T42 | UQD-US-P11 | Loading-art round fade |
| 16 | UQD-T43 | UQD-US-P12 | Corner art |
| 17 | UQD-T44 | UQD-US-P13 | Intro: clouds and typewriter story |
| 18 | UQD-T45 | UQD-US-P14 | Main menu with hover and click sounds |
| 19 | UQD-T46 | UQD-US-P15 | Katana cursor |
| 20 | UQD-T47 | UQD-US-P16 | Credits screen |
| 21 | UQD-T48 | UQD-US-P17 | Victory and game over screens |
| 22 | UQD-T49 | UQD-US-P18 | Per-arena ambient effects |
| 23 | UQD-T50 | UQD-US-P19 | Fullscreen, 640 x 480 framing, clock, debug overlay |
| 24 | UQD-T51 | UQD-US-S5 | EditMode and scene tests |
| 25 | UQD-T52 | UQD-US-S6 | CI that runs and builds Unity |
| 26 | UQD-T53 | UQD-US-S7 | README and BIBLE brought current |
| 27 | UQD-T54 | UQD-US-P20 | Final play-through against the original |

UQD-T21 (online in the Duel scene) is older and independent: it builds on UQD-T30's command model, so it is
best worked after UQD-T30.

## 5. Deliberate deviations (stay unless the BIBLE changes)

- Reactions are timed by input event timestamp, not by ticks at 1000 fps ([UQD-LAW-2](BIBLE.md#UQD-LAW-2)).
- No clash: a dead heat scores nobody.
- Fighters are cosmetic, with no stats; the computer opponent has fixed Easy / Normal / Hard ranges, not an
  adaptive ART.
- A match is best of 3, not HP.
- The round waits at Ready for Space / Start rather than starting itself after 3 s.
- Versus uses A / L and gamepads, not Left / Right Ctrl.
- The original plays no music (its jukebox script is commented out), so none is added.
