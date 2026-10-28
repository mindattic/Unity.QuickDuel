---
codex: 1
project: Unity.QuickDuel
code: UQD
layer: reconstruction
status: living
updated: 2026-10-26
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
| `startup` | 3 s (`startup_time = 3 * room_speed`) while the loading fade clears and both fighters fade in idle; the first key press skips to `countdown` | Matches (UQD-T66): `DuelStartup` begins the countdown 3 s (`startupSeconds`) after the image is gone, or the scene's start for the first round; a click (or pad South) during it, from the reset on, begins it at once |
| `countdown` | 4 s (`countdown_time`): "Get Ready" for the first quarter, then 3, 2, 1 at 25%, 50%, 75%, a beep on each, each number fading at 0.02 alpha per tick | Matches: 4 s countdown, Get Ready above the number throughout, then 3-2-1 with a beep, each fading at 0.02 alpha a 60 fps step (UQD-T57, UQD-T37) |
| `drumroll` | `irandom_range(3 * room_speed, 6.5 * room_speed)` ticks, drumroll sound, `obj_letterbox` closes in 1 px per tick to 12% of the height | Same 3 to 6.5 s, drumroll, letterbox |
| `action` | Drumroll stops, unsheath sound, `arena.inverted = true` (blend-mode invert of the whole backdrop), fighters switch to run sprites, a counting-up number shows, the computer opponent strikes at `ART + irandom(-LCK, LCK)` ticks | Cue, a real colour invert of the arena backdrop (UQD-T33), unsheath, the counting-up `ceil(count)` in ms read off the event clock, frozen at the first strike and fading 0.003 a 60 fps step (UQD-T38), adaptive computer replaced by Easy / Normal / Hard ranges |
| `clash` | Reactions within 3 ticks start a button-mash bar (`obj_clash`, `action_clash`) | Dropped on purpose; a dead heat is a tie that scores nobody |
| `aftermath` | Letterbox exits, slash sound, blood emitter, 6 to 12 splatters, white flash, `determine_winner`, loser's weapon(s) thrown, winner reaction number slides, 3 s, then a loading-art fade and the next `startup` | Slash, loser falls, reaction times; the winner's name and Wins! above the loser's mark and the winner's number sliding 1 px a 60 fps step away from the loser as it fades (UQD-T39); blood emitter, 6 to 12 splatters and the white flash (UQD-T40); the loser's weapon (and the Myrmidon's shield or the Ronin's second sword) thrown spinning, landing with a hit-ground sound (UQD-T41); 3 s on, one of the 14 loading images fades in at 0.005 a step, the battle is reset under it and it fades out (UQD-T42) |
| Premature strike | `action_attack` during `drumroll`: the striker loses outright (`reaction_time = opponent ART + 1 to 50`), "Premature Strike!" shown | Early strike loses; an Android taunt line drawn centred where "Premature Strike!" was, white Old English (UQD-T6, UQD-T39) |
| Strike during `countdown` | Ignored (no handler outside `drumroll` and `action`) | Fixed to match (UQD-T22) |
| Winner | Lower `reaction_time + reaction_penalty`; equal is a draw (nothing happens) | `DuelJudge` (lower wins, tie scores nobody) |
| No strike | `action` counts until someone strikes; no timeout | Matches locally (UQD-T68: `DuelTuning.ToLocalSettings`, no timeout); online rounds give up after `strikeTimeoutSeconds` (3 s), the rebuild's own |
| Match end | An HP bar per side, damage from the reaction gap; 0 HP goes to `gameover` or `victory` | Best of 3 (deliberate), then the victory or game over screen under the full loading image (UQD-T48) |

### 1.2 Input (`obj_game`)

| Input | Original | Rebuild |
| --- | --- | --- |
| Space or left mouse | Strike for the left fighter in single player; skips `startup` | Matches (UQD-T69): against the computer Space or the mouse (and touch) strike for the left fighter; a click skips the startup in either mode (UQD-T66) |
| Left / Right Ctrl | The two strikes in Versus | Matches (UQD-T69; A / L until then); one gamepad each as well, which the original lacked |
| F1 | Toggle fullscreen | Built (UQD-T50: `GameWindow` in every scene) |
| F12 | Toggle the debug overlay (frame counter, both reaction numbers, fps) | Built as a developer toggle, off at start, editor and development builds only (UQD-T50: `DuelDebugOverlay`) |
| R | Go to the `location` map | Dropped |
| Escape | Back to character select at any moment (menu: `game_end`; victory and gameover rooms: none) | Leaves the duel for the main menu, the dropped character select's place, at any moment; nothing under the verdict; the menu quits (UQD-T23, UQD-T65) |

## 2. Inventory and disposition

Disposition key: **built** (in the rebuild), **missing** (a ticket builds it), **dropped** (deliberately not rebuilt, with the reason).

### 2.1 Rooms

| Room | Contents | Disposition |
| --- | --- | --- |
| `intro` | `obj_intro`, four cloud layers x3, `obj_typewriter_intro`, `obj_fade_intro` | built (UQD-T44): `Intro.unity`, the game's first scene (`IntroScene`, `IntroController`); it leads to the main menu (UQD-T45) |
| `mainmenu` | `obj_title`, buttons New Game, Versus, Options, Quit, Credits, Scores, `obj_mainmenu`, `obj_mouse`, `obj_fade_mainmenu` | built (UQD-T45): `MainMenu.unity` (`MainMenu`, `MenuFade`, `MainMenuController`), New Game, Versus, Quit and the computer's difficulty in Options' slot; cursor built (UQD-T46: `KatanaCursor`), Credits built (UQD-T47: `CreditsRoll`; its screen never existed: fixed) (Options, Scores were empty stubs: dropped) |
| `gamescreen` | `obj_jukebox`, `obj_game`, corner arts, `obj_mouse`, `obj_fade_gamescreen`, `obj_letterbox` | built (UQD-T33 to T43, T49, T50): `Duel.unity` with its arena, fighters, corner arts, fade, letterbox and clock |
| `victory`, `gameover` | `obj_victory`, `obj_virtue` x7, retry and continue buttons | built (UQD-T48): `VerdictScreen`, `DuelVerdict` in the Duel scene, cut to as the original's room_goto; "Game Over" (never drawn: `obj_gameover` was not placed) fixed, "Victory" added, `obj_victory`'s empty labels filled; Retry a new match, Continue the menu; HP and virtues dropped |
| `characterselect`, `room2` | `obj_characterselect`, five `obj_btn_<fighter>`, `obj_doll`, `obj_descbox`, typewriter | dropped: choosing a fighter (fighters become cosmetic, UQD-T34) |
| `location`, `travel` | `obj_location`, `obj_map`, `obj_walker`, `obj_travel`, `obj_talkbox`, `obj_dialogwriter`, portraits | dropped: overworld |
| `room0` | `obj_game` only, 30 fps test room, not in the room order | dropped |

### 2.2 Objects

| Group | Objects | Disposition |
| --- | --- | --- |
| Round | `obj_game`, `obj_gameclock`, `obj_letterbox`, `obj_arena`, `obj_player` | built as `Duel`, `DuelSession`, `DuelView` (letterbox, arena backdrop and its invert, UQD-T33), `obj_player` as the cosmetic fighters standing, facing and fading in (UQD-T34) and running, creeping, dashing and falling (UQD-T35, `FighterMotion`) |
| Aftermath | `obj_blood_emitter`, `obj_blood_drop`, `obj_splatter`, `obj_flash`, `obj_weapon` | built: blood, splatter, flash (UQD-T40: `BloodSpray`, `SplatterField`, `WhiteFlash`, `DuelBlood`), `obj_weapon` (UQD-T41: `WeaponToss`, `DuelWeapons`) |
| Transitions | `obj_fade`, `obj_fade_gamescreen`, `_intro`, `_mainmenu`, `_characterselect`, `_location`, `_travel` | gamescreen fade built (UQD-T42: `RoundFade`, `DuelFade`); intro fade built (UQD-T44: `LoadingFade`, its random image fixed), and the Duel scene opens under the intro's image as the menu did; mainmenu fade built (UQD-T45: `MenuFade`, its shared first pass kept); the rest dropped |
| Frame | `obj_cornerart_left`, `obj_cornerart_right`, `obj_title`, `obj_mouse` (katana cursor), `obj_time` (clock) | corner arts built (UQD-T43: `CornerArt`, `DuelCornerArt`, sliding in from off the screen on start, `x += 1 + 0.05` of the distance left a step); title built (UQD-T45), katana cursor built (UQD-T46: `KatanaCursor` on the menu and the duel, tip at the pointer, under the letterbox and fades, hidden while a gamepad is in use); clock built (UQD-T50: `RoundClock`, `DuelClock`); the 640 x 480 room letterboxed into any window (UQD-T50: `OriginalFrame`) |
| Intro | `obj_intro`, `obj_typewriter_intro`, `obj_cloud1` to `4`, `obj_cloud_parent` | built (UQD-T44: `IntroScene`, `IntroController`); a key or pad button skips as well as the click (deliberate) |
| Menu | `obj_btn_newgame`, `_versus`, `_quit`, `_credits`, `_options`, `_scores`, `_start`, `_retry`, `_nextbattle` | New Game, Versus, Quit built (UQD-T45), Options' slot holds the difficulty; Credits built (UQD-T47); `_retry`, `_nextbattle` built (UQD-T48, the verdict's Retry and Continue); Scores, Start: dropped |
| Ambient | `obj_sand_emitter` / `obj_sand`, `obj_butterfly_emitter` / `obj_butterfly`, `obj_snow_emitter` / `obj_snow`, `obj_fog_emitter` / `obj_fog`, `obj_fly_emitter` / `obj_fly`, `obj_rain_emitter` / `obj_raindrop`, `obj_leaf_emitter`, `obj_firefly_emitter` / `obj_firefly`, `obj_tree_emitter` / `obj_tree1`, `obj_lens`, `obj_spotlight` | built (UQD-T49): `AmbientField`, `DuelAmbient`; sand, butterfly, snow, fog and fly per arena, the unused rain, leaf and firefly as options; tree, lens and spotlight dropped (never placed, or the character select's) |
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
| `set_state_idle`, `_running`, `_action`, `_attacking`, `_defeat` | built as `FighterMotion` (idle UQD-T34; running, attacking and defeat UQD-T35; the defeat strip held on its first frame but for the killing blow, UQD-T64); `set_state_action` is dead code (never called) and is not rebuilt |
| `spawn_weapons` | built (UQD-T41, `WeaponToss`) |
| `load_arenas`, `choose_random_arena`, `setup_arena_properties` | `load_arenas` is `Data/assets.json`'s arenas, `choose_random_arena` is `ArenaDraw` (once per match as the original's once per game, all 11, UQD-T33, UQD-T63); `setup_arena_properties` is the manifest's per-arena `ambient`, played by `DuelAmbient` (UQD-T49) |
| `load_characters`, `choose_opponent`, `set_character_description` | `load_characters` is `Data/assets.json`'s fighters (name and sprites only), `choose_opponent` is `FighterDraw` (both seats, drawn per match, since character select is dropped) (UQD-T34); stats, skills, portraits and descriptions dropped |
| `load_text`, `init_text`, `process_text`, `dialog`, `mouseover_message_draw` | `introText` read into the asset manifest and typed out by the intro (UQD-T44); the rest dropped |
| `draw_text_outline`, `_alpha`, `_color`, `add_shadow`, `draw_light`, `create_lens`, `RGB` | outline text built as `OutlinedText` and `OriginalType` (UQD-T36); lighting dropped |
| `show_float_text`, `_color` | dropped (damage numbers) |
| `calculate_damage`, `check_regen`, `focus_percent`, `skill_*`, `reset_combos`, `reset_selectboxes` | dropped |
| `ai_fly`, `ai_firefly`, `ai_butterfly` | built in `AmbientField` (UQD-T49), the flies' missing lower bound fixed |
| `difference_between`, `play_random_music`, `globals`, `script9` | globals: constants carried in settings (UQD-T28); the rest dropped |

### 2.4 Sprites, backgrounds, fonts, sounds, data

| Group | Assets | Disposition |
| --- | --- | --- |
| Fighters | `spr_character_<exile,gypsy,myrmidon,ronin,vagabond>_<idle,run,attack,defeat>` (idle and run 1 frame, attack and defeat 4), weapons and ground weapons, Myrmidon shield, portraits, profiles, icons, silhouettes | idle, run, attack, defeat, weapons: imported (UQD-T32), named per fighter in `Data/assets.json`, idle drawn (UQD-T34), run, attack and defeat drawn (UQD-T35), weapons, ground weapons and the shield thrown (UQD-T41); portraits, profiles, icons, silhouettes: dropped |
| Arenas | 17 `spr_arena_*` sprites (11 named in `load_arenas`), `arena_desert` background, `bg_dojo`, `bg_panorama1` | the 11 `load_arenas.gml` arenas imported with their names and descriptions (UQD-T32), drawn by UQD-T33, one per match (UQD-T63), behind everything. Fixed defect (UQD-T33): `load_arenas.gml` gives Sozza's Point and South Ridge the same id (5), so South Ridge overwrote Sozza's Point and only 10 arenas could be chosen; all 11 are drawn now |
| Transitions | `spr_loading1` to `14` | imported (UQD-T32), drawn by the round transition (UQD-T42) |
| Aftermath | `spr_blood_drop1`, `2`, `spr_splatter1` to `10`, `spr_flash`, `bg_splatter_scroll`, `spr_white` | blood drops, splatters, flash imported (UQD-T32), drawn (UQD-T40); `bg_splatter_scroll` is the main menu's background (drawn by UQD-T45) and `spr_white` (`obj_lens`) is not the aftermath's |
| Frame and menu | `spr_titlescreen`, `spr_cornerart_left`, `left2`, `right`, `right2`, `spr_btn_left1` to `4`, `spr_btn_right1` to `4` (each with `_over`), `spr_katana_cursor`, `bg_black` | title, corner arts, cursor imported (UQD-T32); `left2` and `right2` drawn by the corner arts (UQD-T43), the plain pair an earlier cut nothing draws; title and the buttons `right1` to `3` and `left4` with their `_over` twins drawn by the main menu (UQD-T45), cursor by `KatanaCursor` (UQD-T46) |
| Intro | `spr_cloud1` to `4`, `bg_mapscreen` | imported and drawn by the intro (UQD-T44) |
| Ambient | `spr_sand1`, `2`, `spr_butterfly` (6), `spr_snow1`, `2`, `spr_fly`, `fly2`, `spr_firefly`, `dark`, `strip`, `spr_rain`, `raindrop`, `rainemitter`, `spr_leaf1`, `2`, `spr_tree1`, `spr_spotlight` | the `ambient` group (UQD-T49): sand, butterfly, snow, fly, rain, leaf, firefly and dark; fog draws `spr_cloud1`; `strip`, `raindrop`, `rainemitter`, `tree1`, `spotlight` unused |
| Results | `bg_victory`, `bg_gameover`, `spr_virtue_*` (7) | backgrounds and `spr_btn_right4` imported and drawn by the verdict (UQD-T48); virtues dropped |
| Stats and combat UI | health, focus, art, skill, stance, combo, clash, critical-hit, buff, descbox, selectbox, talk and map sprites | dropped |
| Fonts | `fnt_basic`, `fnt_mono`, `fnt_oldenglish`, `fnt_oldenglish_small`, `fnt_tiny` (ttf: oldenglish, sansation, cyberia, gnuolane, infinity, junction) | the five bitmap fonts (atlas and glyph table) and `oldenglish.ttf` imported (UQD-T32); the five bitmap fonts built as UI Toolkit font assets and drawn as outlined text (UQD-T36); the other ttfs came from the Android builds and are unused |
| Sound: duel | `sfx_beep`, `sfx_drumroll`, `sfx_unsheath`, `sfx_slash`, `sfx_victory`, `sfx_defeat` | built (UQD-T5, T24) |
| Sound: aftermath and menu | `sfx_hitground1` to `4`, `sfx_flick`, `sfx_click` | imported (UQD-T32), hit-ground played as a thrown weapon lands (UQD-T41), flick and click by the main menu's buttons (UQD-T45) |
| Sound: dropped | `sfx_disabled`, `sfx_bomb`, `sfx_powerup_activate` (focus and skills); `music_*` x8 (no script plays them) | dropped |
| Data | `datafiles/credits.txt` (arena photo credits) | imported as `Data/credits.txt` (UQD-T32), shown on the main menu's credits screen (UQD-T47), with the project's `option_author` as the author's credit |
| Project | `bushidocoda.ico`, splash images, `help.rtf`, `html` / `Install` web and installer pages | icon built (UQD-T50: `bushidocoda.ico`'s 64 x 64 frame, `PlayerIconBuilder`), product name QuickDuel; the rest dropped |

## 3. The audit result

### 3.1 What the rebuild has

The engine-free duel (phases, random standoff, early strike, settle window, timeout, `DuelJudge`, `Match`,
`AiOpponent`, online host and client), two-player input by event timestamp, the Input System, the generated
Duel scene, countdown, letterbox, cue invert, taunts, the original six duel sounds, a best-of-3 scoreboard,
Escape back to the menu, mouse and touch strike, a Windows build, online protocol and transport, Core, EditMode and
PlayMode tests.

### 3.2 What it lacks

**Visuals: complete, play-through done (UQD-T54).** Every item the audit once listed as missing (art, arenas,
fighters and their motion, blood, splatter, flash, weapons, round fade, intro, main menu, cursor, credits, ambient
effects, victory and game over screens, fullscreen) is built; see the dispositions in section 2. Of the original's 27
duel sounds, the duel's six and the aftermath and menu's six are used; the rest are dropped on purpose.

The play-through, with evidence in `docs/parity`:

| Stage | How | Evidence |
| --- | --- | --- |
| Launch, intro, menu | The built Windows player (`play.ps1 -Build`, muted) launched, ran the intro's typewriter story, and a Space press reached the menu; no errors in `Logs/play.log` | `live-player-intro.png`, `live-player-menu.png` |
| Each mode | `PlaythroughTests.EveryModeIsPlayableAndDrawn`: two players, vs AI Easy, Normal and Hard each reach a countdown frame, then a strike and a winner | `playthrough-mode-*.png` |
| Clean win | `ACleanWinIsDrawn` | `playthrough-clean-win.png` |
| Premature strike | `APrematureStrikeLosesAndIsDrawn`: early strike loses, the other side wins | `playthrough-premature-strike.png` |
| No strike | `ARoundNobodyStrikesInIsDrawn` | `playthrough-no-strike.png` |
| Verdict | `AWonMatchEndsOnTheVictoryScreen`, `ALostMatchEndsOnTheGameOverScreen`: a real two-round match each | `playthrough-verdict-victory.png`, `playthrough-verdict-gameover.png` |
| Against the original | Intro, menu, game over and victory beside the original's own art | `compare-*.png` |

The original GameMaker build was **not** run (it has no mute and the owner was at the computer), so the comparison is
against the original's assets and rooms (its sprites, backgrounds and room layouts: positions, sizes, text), not live
captures of the original. The scripted frames are drawn through the UI panel at 1440 x 1080 (the 640 x 480 room at 3x).
Every row of section 2 is marked built or dropped; the repo is declared complete.

**Standards.** Measured against AGENTS.md and the shared Unity standards:

| Area | Finding |
| --- | --- |
| Assembly definitions | Good: Core (`noEngineReferences`), Runtime, Editor, Online (version-define gated), the EditMode, PlayMode and Online test asmdefs, one-way references |
| Engine-free Core | Done (UQD-T30): `MatchSession` owns mode, difficulty, match reset and AI wiring, and `Strike` takes a player id; local and online play reach the scene through one `IDuelAuthority` seam (UQD-T70) |
| Input System | Good: `InputAction`s with event timestamps, gamepad per side |
| URP | Done (UQD-T29): `QuickDuelURP` behind `RenderPipelineBuilder` |
| Unity.Shared profiles and settings | Done (UQD-T28, UQD-T61, UQD-T74): `com.mindattic.unity.shared` is in the manifest, every tuning number is in `tuning.json` (named profiles, validated on load), and the player's shared profile sets the game's volume (its sound-effect volume and mute, the session mute among them); the original had no settings screen (its Options button was an empty stub), so none is added |
| UI | Done (UQD-T31): UI Toolkit trees built in code |
| Tests | Done: Core NUnit, EditMode (`Tests/Editor`, UQD-T51), PlayMode with `ParitySceneTests` and the committed `docs/screenshots`; CI runs Core and, behind the Unity license secrets, EditMode, PlayMode and the Windows build (UQD-T52) |
| README and BIBLE | Done (UQD-T53): README lists Runtime, Online, Editor, the scenes, the settings registry and the art pipeline; BIBLE sections 3, 4, 6 and 7 cover the cosmetic fighters, arenas, intro and menu flow |
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

Current with the faithfulness audit of 2026-10-26 (UQD-T63 to T69, and the follow-ups UQD-T70, T73): the audit
compared the rebuild with the original line by line under the faithful-port rule, and every behaviour that had
drifted from a working original was restored. What is left is only what the original could not give a Unity
rebuild, each recorded in the BIBLE ([section 4.8](BIBLE.md#UQD-§4.8)).

- Reactions are timed by input event timestamp, not by ticks at 1000 fps ([UQD-LAW-2](BIBLE.md#UQD-LAW-2)).
- No clash: a dead heat scores nobody.
- Fighters are cosmetic, with no stats; the computer opponent has fixed Easy / Normal / Hard ranges, not an
  adaptive ART.
- A match is best of 3, not HP; the round that decides the match stands in for the killing blow.
- Both seats' fighters are drawn at random per match, as there is no character select or `choose_opponent`.
- The taunt wording is the Android build's ten lines, drawn where the GameMaker build wrote "Premature Strike!".
- Online rounds (no original) give up after a 3 s strike timeout, show the host's verdict and let the host's Space
  / Start begin each round; local rounds have none of these.
- The original plays no music (its jukebox script is commented out), so none is added.

Restored by the audit, no longer deviations: the arena is rolled once per match, not per round (UQD-T63); a round's
loser holds its first defeat frame unless the round decides the match (UQD-T64); Escape leaves the duel for the main
menu at any moment (UQD-T65); the startup runs by itself and a click skips it, with no Ready screen (UQD-T66); no
in-duel mode cycle or mode line (UQD-T67); a local round waits for the first strike with no timeout (UQD-T68); the
original's strike keys, Space or mouse against the computer and Left / Right Ctrl in Versus (UQD-T69); and the
per-side reaction lines and the red "!" the gamescreen never drew are gone (UQD-T73).
