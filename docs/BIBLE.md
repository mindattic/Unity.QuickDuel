---
codex: 1
project: Unity.QuickDuel
code: UQD
layer: bible
status: living
updated: 2026-10-24
---

# Unity.QuickDuel — Project Bible
> Single source of truth for what Unity.QuickDuel IS, is NOT, and the rules that keep it coherent.
> README says how to build and play it; [ORIGINS](ORIGINS.md) says where it comes from; this says how to
> think about the game.

Status legend: ✅ done (verified by a test/build) · 🟡 partial · ⬜ planned · `living`.

## 1. The one sentence {#UQD-§1}
Unity.QuickDuel is a Unity 6 reaction duel: two duelists wait through a standoff of unknown length, the
screen flashes and a blade is drawn, and whoever strikes first wins — strike before the cue and you lose.

## 2. The product promise {#UQD-§2}
- **One moment, nothing else.** The whole game is the wait and the strike. It is the core of Ryan's old
  *Bushido Coda* (Android 2011–13, GameMaker 2013), itself grown out of Samurai Kirby; see
  [ORIGINS](ORIGINS.md).
- **Fair to the millisecond.** A strike is timed by its input event's own timestamp, not by the frame that
  noticed it, so frame rate can never decide a duel ([UQD-LAW-2](#UQD-LAW-2)).
- **Unreadable cue.** The standoff is drawn uniformly from 3 to 6.5 seconds after the countdown, so
  the only way to win is to react, not to count.
- **Patience is a skill.** Striking during the standoff loses the round outright, and the loser is taunted
  with a line from the old Android build ("Jumped the Gun", "Too Soon", ...).
- **Couch first.** Two players on one machine (keyboard A / L, or one gamepad each), or one player against
  an AI on Easy, Normal or Hard. A match is best of 3.

## 3. What it is NOT {#UQD-§3}
- **NOT Bushido Coda.** No overworld, merchants, weather, items, stats (ART, LCK, PWR), focus bar, skill
  dials, combos, clash button-mash, or arenas and characters as gameplay. Those were dropped on purpose;
  adding any back needs a Bible change first.
- **NOT a character game.** The original's five fighters (Myrmidon, Ronin, Gypsy, Vagabond, Exile) are
  **cosmetic only** ([§4.5](#UQD-§4.5)): a name and the idle, run, attack, defeat and weapon art. They carry
  no stats (PWR, VIT, SPD, LCK), no skills, no HP and no portrait or description, and which fighter stands in
  a seat never changes how a round is timed, judged or scored. There is no character select: the authority
  draws both seats' fighters for each match.
- **NOT frame-timed.** The 2013 build ran its action phase at 1000 FPS to time reactions by frame.
  QuickDuel never does; see [UQD-LAW-2](#UQD-LAW-2).
- **NOT a game where the AI cheats.** The AI draws a reaction time from its difficulty's range when the
  countdown starts and strikes at exactly `CueAt + reaction`; it never sees the player's input and never
  strikes early.
- **NOT online in the shipped scene (yet).** Online PvP is built and tested at the protocol and transport
  level (§4.4, `OnlineDuel`), but the Duel scene has no Host / Join menu; that is [frontier](#UQD-§7).

## 4. Architecture canon {#UQD-§4}

```
Assets/Unity.QuickDuel/
  Core/      engine-free rules (noEngineReferences)   Duel, DuelSettings, DuelResult, DuelJudge, Match, AiOpponent,
                                                      MatchSession, OnlineDuelHost, OnlineDuelClient
  Core/Settings/ the tuning registry                  TuningRegistry (named profiles), Json, SettingsReader, ISettingsSource
  Core/Content/  the original asset manifest          AssetManifest (sprites, sounds, fonts, typefaces, arenas, fighters, credits),
                                                      ArenaDraw (each round's arena roll and the arena it lands on),
                                                      FighterDraw (each match's fighter roll and the pair it lands on),
                                                      FighterLineup (where the fighters stand, their fade and tint)
  StreamingAssets/Settings/tuning.json                every tuning number, as data
  Art/, Audio/, Fonts/  the original's files           copied by OriginalAssetGenerator, never edited by hand
  Data/assets.json, Data/credits.txt                  the generated asset manifest and the original credits text
  Runtime/   MonoBehaviours on the one "Duel" object  DuelController, DuelInput, DuelSession, DuelView, DuelTaunt, DuelAudio,
                                                      TuningStore, ArenaBackdrop (an arena as the scene carries it),
                                                      FighterLook (a fighter as the scene carries it)
  Online/    the wire, over Unity.Multiplayer         OnlineDuel (a NetEntity; compiles only with the package present),
                                                      OnlineDuelLobby, OnlineDuelMenu, OnlineDuel.prefab, QuickDuelNetPrefabs
  Editor/    generators                               DuelSceneBuilder (writes Assets/Scenes/Duel.unity), OnlineSceneBuilder,
                                                      QuickDuelBuild, OriginalAssetGenerator + OriginalAssetImportRules
  Tests/PlayMode/                                     scene, input, session, view, taunt and audio tests
  Tests/Online/                                       host + client over the Unity.Multiplayer test kit
Tests/Core.Tests                                      NUnit tests of Core, run with `dotnet test Core.slnx`
```

### 4.1 A round {#UQD-§4.1}
`Duel` is one round, a phase machine driven by caller-supplied timestamps in seconds:

| Phase | Lasts | On screen and in the ear |
| --- | --- | --- |
| `Ready` | until Space / Start | title and controls |
| `Countdown` | `CountdownSeconds` (4) | Get Ready for the first quarter, then 3-2-1 at 25%, 50% and 75%, a beep on each number |
| `Standoff` | `MinStandoffSeconds`–`MaxStandoffSeconds` (3–6.5), random | letterbox closes in, drumroll |
| `Strike` | until both strike, or `StrikeTimeoutSeconds` (3) | the arena backdrop's colours invert, the cue, the blade unsheathed |
| `Resolved` | until the next round | slash, the loser falls, reaction times in ms, headline |

The cue time `CueAt` is fixed when the round begins. After the first strike the round waits
`SettleSeconds` (50 ms) for a later-delivered input event that happened earlier, then resolves.

### 4.2 Judging {#UQD-§4.2}
`DuelJudge.Judge(left, right)` settles a round from reaction times alone (seconds from the cue; negative
means before it; null means no strike). In order:

1. **Forfeit** — a reported reaction that is not finite, or under the human floor (100 ms), forfeits.
   Local play passes a floor of 0, since its strikes are real input events; the floor exists for online
   reports.
2. **EarlyStrike** — a negative reaction loses; if both struck early, the earlier one loses.
3. **NoStrike** — nobody struck before the timeout. Nobody scores.
4. **Tie** — identical reactions. Nobody scores.
5. **FasterStrike** — the lower reaction wins; a lone striker wins.

### 4.3 Session and match {#UQD-§4.3}
`MatchSession` (Core, UQD-T30) is the one authority over a local duel's round, mode and match: it owns the
mode (Local PvP → vs AI Easy → Normal → Hard), the difficulty mapping, the `Match`, the current `Duel` and
the AI's wiring (scheduling and striking on its own behalf). Nothing changes any of that except its three
commands -- `BeginRound`, `Strike`, `SetMode` -- each named by the seat issuing it (`playerId`: 0 the left
seat, 1 the right, the seam a future `NetEntity` will plug a real player id through, AGENTS.md rule 9); every
change it makes is announced only as an event (`RoundStarted`, `PhaseChanged`, `Resolved`, `ModeChanged`).
`DuelController` threads Unity's clock and tuning through to those commands and republishes its events;
`DuelSession` is a thin view that turns Tab / gamepad Select into `SetMode` and draws the mode, score and
verdict labels from the session's state. The mode is cycled only between rounds; changing it always starts
a fresh match. A match is first to 2 wins (best of 3); ties and no-strike rounds award nobody. The
scoreboard shows wins and each side's best reaction.

AI reaction ranges (`AiOpponent.ReactionRange`): Easy 450–700 ms, Normal 300–450 ms, Hard 200–300 ms.

### 4.4 Online PvP design {#UQD-§4.4}
Credited to Christopher Danielson. The host only listens: it sends each round's standoff length, each
client runs its own countdown and cue and measures its own reaction from its own cue to its own key press,
and sends back just that number. The host judges the two numbers with `DuelJudge` (human floor on). Ping
never enters the measurement, so neither player is favoured by latency.

Built as (UQD-T14):

- **`OnlineDuelHost`** (Core) draws each round's standoff (and hands out its countdown), accepts the first
  report per side for the open round only, and judges once both are in, or when the report window closes
  (`CountdownSeconds + standoff + StrikeTimeoutSeconds + SettleSeconds + ReportGraceSeconds` (2 s) after the
  round began) with a silent side counted as no strike. A finished match is replaced on the next round.
- **`OnlineDuelClient`** (Core) runs each announced round as an ordinary local `Duel` on its own clock with the
  host's lengths (`Duel.Begin(now, standoffSeconds)`), and reports its own side's reaction once that duel
  resolves: at once for an early strike, after the settle window for a strike, after the timeout for none.
  A round no newer than the current one is ignored.
- **`OnlineDuel`** (Online, a `NetEntity`) is the wire: the server `Seat`s two client ids (synced),
  `BeginRound` sends `RoundBegan(round, countdown, standoff)` to every peer, the seated duelists send
  `ReportReaction(round, struck, reaction)`, reports from any other connection are dropped, and
  `RoundJudged` carries the verdict to every peer, which records it in its own `Match`. A local strike is
  `OnlineDuel.Strike(eventTime)`.

In the Duel scene (UQD-T21):

- **Host / Join menu** (`OnlineDuelMenu`, built in code on `DuelView`'s canvas, left edge under the left score,
  shown between rounds only). **Host** starts a listen server on the scene's `NetBootstrap`; this player duels on
  the left. **Join** dials the typed address (`192.168.1.20` or `192.168.1.20:7777`, parsed by
  Unity.Multiplayer's `HostAddress`); that player duels on the right. **Leave** ends either. Two players at most,
  joined by address; no join-code service is set up. Typing in the address field and clicking the menu never
  strikes, begins a round or cycles the mode (`UiFocus`).
- **`OnlineDuelLobby`** (Online) is the session flow, apart from the menu: on the host it spawns the one
  `OnlineDuel` (network key `quickduel.duel`) once the session is up, seats the host on the left and the first
  player to join on the right, and empties that seat when the player leaves (`OnlineDuel.Unseat`); on every peer
  it hands the `OnlineDuel` to `DuelController.Online` once it arrives and takes it back when the session ends.
- **`DuelController.Online`** (an `IOnlineDuelSource`, Runtime's seam that `OnlineDuel` implements, since Runtime
  cannot name Online) drives the scene instead of the local `MatchSession`, which stands idle and resumes unchanged
  afterwards. The phase is this machine's own round's, but `Resolved` (and `Result`, the slash, the fall, the
  headline, the score) waits for the host's verdict: the local round saw only this side's strike, so its own
  result is never shown. Between this side's report and the verdict the headline reads `...`. Only the host's
  Space / Start begins a round, and only with both seats taken; every strike key strikes for this machine's seat;
  Tab / Select does nothing online. The scoreboard marks the player's own seat `(you)` and the victory / defeat
  sting is heard from it.
- **Scene generation**: `OnlineSceneBuilder` (run by `DuelSceneBuilder`) makes `OnlineDuel.prefab` (NetworkObject,
  NetKey, OnlineDuel, with its `GlobalObjectIdHash` filled in batch mode) and its `NetPrefabList`, networks the scene
  with Unity.Multiplayer's `NetSceneSetup`, and sets the `NetBootstrap` never to start by itself: no auto start, and
  above all no serving when headless, since batch-mode tests and builds load this scene too.

### 4.5 The fighters {#UQD-§4.5}
Cosmetic data only ([§3](#UQD-§3)). `Data/assets.json`'s `fighters` section, written by `OriginalAssetGenerator`
from `load_characters.gml` (in its id order: Myrmidon, Ronin, Gypsy, Vagabond, Exile), gives each fighter its
`character_name` and its `character_asset_idle/run/attack/defeat`, `weapon_asset` and `weapon_asset_ground` sprites;
the stats, skills and portraits in the same script are not read. `AssetManifest.Fighters` validates it.

- **Who stands where.** The authority draws one fighter roll per match (`MatchSession.FighterRoll` locally, on
  construction, on a mode change and when a finished match is replaced; `OnlineDuelHost.FighterRoll` online, sent with
  every `RoundBegan` so both peers agree), and `FighterDraw.Pick` maps it onto a fighter for each seat, both
  independently and uniformly, so both seats can be the same fighter.
- **On the field** (`FighterLineup`, from `globals.gml`): the left fighter stands at a quarter of the screen width
  (`left_position_idle = room_width / 4`) facing right, the right one at three quarters
  (`right_position_idle`) mirrored to face left (`image_xscale = -1`), both centred two thirds of the way down
  (`vertical_position = room_height - room_height / 3`), drawn at the original's 640 x 480 scale (2.25x on the
  1920 x 1080 stage).
- **Fade in.** Each fighter starts invisible and fades in at `obj_player`'s `fadespeed`, 0.01 alpha per 60 fps tick
  (1.67 s to full), whenever a new pair takes the field (the scene's first frame and every new match). It is timed
  by the clock, 0.6 alpha a second, not counted per rendered frame ([UQD-LAW-2](#UQD-LAW-2)); a stalled frame
  advances it by at most 50 ms, as the original's frame-locked fade would not have jumped either.
- **Same fighter, two colours.** When both seats hold the same fighter, the left one is tinted `color_red`
  (RGB 196, 64, 64) and the right one `color_blue` (64, 64, 196), as `obj_game` does with `image_blend`;
  otherwise neither is tinted.
- **Deliberate deviation**: the original's player chose a fighter on the character select screen (Myrmidon by
  default) and its single-player opponent was `choose_opponent`'s random pick per location; both screens are
  dropped, so the rebuild draws both seats at random for each match.
- **Motion through a round** (`FighterMotion`, UQD-T35, from the `set_state_*` scripts, `obj_player`, `gameclock_action`
  and `obj_gameclock`'s alarm). On the cue both fighters switch to their run sprite on their run marks, 40% and 60%
  of the width (`left/right_position_run = room_width / 2 -/+ room_width / 10`), and creep together 0.01 original
  pixel per step at the cue's `actionFPS` of 1000, 10 original pixels a second, timed by the clock
  ([UQD-LAW-2](#UQD-LAW-2)), until the verdict. The verdict (`determine_winner`) puts both at the middle: the winner
  plays its attack strip, drawn in front (`depth = -1`), dashing on each 60 fps step by `1 + 0.1` of the gap left to
  the loser's idle mark; the loser plays its defeat strip. Strips advance at `image_speed` 0.1 (a frame every 10
  steps) and hold their last frame (Animation End). Three seconds after the verdict (`alarm[0]`) the winner shows its
  idle sprite again where it stopped; the next round puts both back on their idle marks. A draw or nobody striking
  moves no one, as `determine_winner`'s draw branch does nothing. An early strike plays the same winner and loser
  motion from the idle marks, as the original's premature strike does.
- **Fixed defect**: `obj_player`'s dash for a winner facing left tests `x < right_position_idle` instead of
  `x > left_position_idle`, so the right-hand winner never stops and drifts on to 10 pixels past the left mark; the
  rebuild mirrors the left-hand winner's dash and stops it on the mark.
- **Deliberate deviation**: the original held a round's loser on the first defeat frame while it had HP left and
  played the whole strip only for the killing blow; the rebuild has no HP ([§3](#UQD-§3)), so every round's loser
  plays the whole defeat strip, in place of the toppling box it had before.

## 5. The Laws {#UQD-§5}

Unity.QuickDuel **inherits the org-wide house rules** in `../../MindAttic.HouseRules.md`. Of note here:
HOUSE-LAW-8 (done is verified, not asserted), HOUSE-LAW-14 (docs publish through the shared docs site),
HOUSE-LAW-15 (don't wait on a full local test run before landing).

Project-specific laws:

### UQD-LAW-1 — The rules stay engine-free {#UQD-LAW-1}
Everything that decides who wins lives in `Assets/Unity.QuickDuel/Core` with `noEngineReferences: true`,
and compiles and is tested outside Unity through `Tests/Core.Tests`. Runtime code feeds timestamps in and
draws what comes out; it never judges a round itself.

### UQD-LAW-2 — Time by event, never by frame {#UQD-LAW-2}
A strike carries its input event's own timestamp (`CallbackContext.time`), and every clock handed to the
`Duel` is `Time.realtimeSinceStartupAsDouble`, the same timeline. The AI strikes at its drawn time, not
at the frame that notices it. No rule may read `Time.frameCount`, `deltaTime` or "this frame".

### UQD-LAW-3 — One moment only {#UQD-LAW-3}
A change that adds stats, items, skills, maps or any input other than "strike" and "next round" (plus the
between-rounds mode toggle) is out of scope unless this Bible is changed first ([§3](#UQD-§3)).

### UQD-LAW-4 — The scene is generated {#UQD-LAW-4}
`Assets/Scenes/Duel.unity` is written by `DuelSceneBuilder` (menu *QuickDuel → Rebuild Duel Scene*, or
batch mode); the UI Toolkit tree is built in code by `DuelView` (UQD-T31). Never hand-edit the scene YAML.

### UQD-LAW-5 — One home per fact {#UQD-LAW-5}
Rules live here; history and the surveyed old builds live in [ORIGINS](ORIGINS.md); story and ticket status
live in [USER_STORIES.md](USER_STORIES.md) (generated — never hand-edited, see `tickets db render`).

## 6. Verified state {#UQD-§6}
- ✅ Engine-free duel core: phases, random standoff, early strike, settle window, timeout (UQD-T1, `DuelTests`).
- ✅ `DuelJudge` from reported reactions, with the human floor (UQD-T13, `DuelJudgeTests`).
- ✅ Best-of-N `Match` with best reactions (UQD-T7, `MatchTests`); AI opponent (UQD-T8, `AiOpponentTests`).
- ✅ Core tests in CI on push and pull request (UQD-T9).
- ✅ Duel scene and controller (UQD-T3), two-player input by event timestamp (UQD-T2), presentation (UQD-T4),
  taunts (UQD-T6), original Bushido Coda audio (UQD-T5), vs-AI mode and match scoreboard (UQD-T15) —
  PlayMode tests under `Assets/Unity.QuickDuel/Tests/PlayMode`.
- ✅ Tuning is data (UQD-T28, `TuningRegistryTests`): every number (round clock, AI reaction ranges, wins needed,
  human floor, online report grace, taunt lines) lives in `StreamingAssets/Settings/tuning.json` as named profiles
  (`standard` holds figures re-derived from BushidoCoda.gmx, see the tuning provenance below), validated on load with every problem named by file,
  key and line, and read through `TuningRegistry` / `TuningStore`; Core has no number baked in. The
  `com.mindattic.unity.shared` package is in the manifest, ready for the player-profile work (the registry itself is
  Core code; nothing yet uses the package). `TuningStore` reads the file from disk, so it is desktop-only: Android
  and WebGL need a UnityWebRequest source before they ship.
- ✅ Tuning provenance (UQD-T57, checked against `BushidoCoda.gmx` and the Android `BattleSystem.java`, 03-15-13):
  - **Countdown, 4 s**: `obj_gameclock` `countdown_time = 4 * room_speed`, "Get Ready" for the first quarter, then
    3, 2, 1 at `gameclock_countdown.gml`'s 25%, 50%, 75%. The rebuild had wrongly kept 3 s; fixed, and the
    number shown follows the same quarters (`Duel.CountdownNumber`).
  - **Standoff, 3 to 6.5 s**: `drumroll_time = irandom_range(3 * room_speed, 6.5 * room_speed)`; matches.
  - **Taunts**: the ten lines of `RandomPrematureString` in the Android `BattleSystem.java`, drawn uniformly
    (`GetRandomNumber(1, 10)`); matches exactly. (The GameMaker build shows only "Premature Strike!".)
  - **AI reaction ranges (Easy 450-700, Normal 300-450, Hard 200-300 ms)**: the original has none. Its computer
    strikes at `ART + irandom_range(-LCK, LCK)` ticks at 1000 fps with LCK 5 to 10, and ART starts at 15
    (`obj_player`), set from the human's ART at `obj_game` creation and thereafter fed only by its own past
    reactions (`calculate_art` averages them), so it never adapts to the human and always strikes in about
    5 to 25 ms: unbeatable. **Fixed defect**: the rebuild's fixed difficulty ranges stand in; they are the
    rebuild's own figures, not the original's.
  - **Wins needed, 2**: the original has no rounds-to-win rule (an HP bar per side, `MAXHP = 50 + VIT * 10`,
    damage from `calculate_damage` as `PWR * 2 + irandom_range(0, LCK)`, doubled on a 1-in-20-ish crit; not
    from the reaction gap); best of 3 is the rebuild's own deliberate rule.
  - **Human floor, 100 ms**: the original has none (it ran locally); the rebuild's own, for online reports only.
  - **Strike timeout 3 s, settle window 50 ms, online report grace 2 s**: the rebuild's own, for timestamped
    and online input; the original has no equivalent.
- ✅ Standalone Windows build with `deploy.ps1` (UQD-T11).
- ✅ Docs site on GitHub Pages via the shared pipeline (UQD-T10).
- ✅ Online PvP protocol and transport over Unity.Multiplayer, per [§4.4](#UQD-§4.4) (UQD-T14):
  `OnlineDuelTests` in Core.Tests, and host + client PlayMode tests in `Tests/Online`.
- ✅ The round headline names the AI in vs-AI mode (UQD-T17, `DuelSessionTests`).
- ✅ End-to-end port check of the Windows build against `BushidoCoda.gmx`: every mode reached and
  played to a match verdict; the room-by-room map is in [ORIGINS](ORIGINS.md) (UQD-T19).
- ✅ Render pipeline (UQD-T29, `RenderPipelineTests`): the project had no render pipeline package at all
  (the built-in pipeline, no URP in `Packages/manifest.json`, `GraphicsSettings.defaultRenderPipeline`
  null). URP 17.4.0 is now in the manifest; `RenderPipelineBuilder` (menu *QuickDuel → Build Render
  Pipeline*, or batch mode, and run by `DuelSceneBuilder` before every scene rebuild) creates
  `Assets/Unity.QuickDuel/Rendering/QuickDuelURP.asset` (a forward Universal Renderer — the game has no
  sprites or 2D lights, only UI, so the 2D renderer is not needed) and assigns it as the default
  pipeline and on every quality level. The pipeline-agnostic layer is the whole game: `DuelView` draws
  only a UI Toolkit tree (UQD-T31) over a camera backdrop; no shader or material in the repo names URP.
  Verified by a Windows Standalone build (`deploy.ps1`) that launches and renders the Duel scene.
- ✅ Session and match flow moved to Core (UQD-T30, `MatchSessionTests`): `MatchSession` is now the one
  authority over the round, mode and match, with `Strike`/`BeginRound`/`SetMode` each carrying a player id
  (§4.3); `DuelController` and `DuelSession` are thin Unity views over it, covered by the existing PlayMode
  suite (`DuelSceneTests`, `DuelSessionTests`, `DuelQuitTests`, `DuelInputTests`, `DuelTauntTests`,
  `DuelAudioTests`, `DuelViewTests`).
- ✅ UI Toolkit, not legacy uGUI (UQD-T31, `DuelViewTests`/`DuelSessionTests`/`DuelTauntTests`): the UI was
  `UnityEngine.UI.Text` off `Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")` drawn on a hand-built
  `Canvas`/`CanvasScaler`, AGENTS.md rule 6's other standards gap ("UI Toolkit or the shared MindAttic UI
  components"). `DuelView` now builds a `UIDocument`/`VisualElement` tree in code (the scene is still
  generated, [UQD-LAW-4](#UQD-LAW-4)) through `Resources/UI/DuelPanelSettings.asset` (scale-with-screen-size,
  1920x1080 reference — built once by `DuelPanelBuilder`, run by `DuelSceneBuilder` the same way
  `RenderPipelineBuilder` is), `Resources/UI/Duel.uss` for its structural USS classes, and a font asset
  (`Resources/UI/Fonts/Cinzel.ttf`, OFL-licensed, already vetted for MindAttic games in Unity.GridGame) set
  on the root so every label inherits it. `DuelSession` and `DuelTaunt` build their own labels the same way
  (`DuelView.CreateLabel`/`CreateEdgeLabel`), through the same `VisualElement` tree. The `DuelView` surface
  the PlayMode tests read (`CountdownText`, `CueText`, `HeadlineText`, `PromptText`, `ReactionText`,
  `HasFallen`, `Inverted`, `LetterboxAmount`, `FormatReaction`, `Headline`) is unchanged; `UnityEngine.UI`
  is no longer a Runtime assembly reference.

- ✅ Online PvP in the Duel scene (UQD-T21, [§4.4](#UQD-§4.4)): the Host / Join menu, and `DuelController`,
  `DuelSession` and `DuelView` driven by an `OnlineDuel` instead of the local `MatchSession`. `OnlineDuelSceneTests`
  plays a host's and a joined client's scene over real loopback (the host starts the round, each strikes on its own
  clock, both scenes show the host's verdict and score, and a leaver's seat empties while their scene goes back to
  local play); `OnlineDuelMenuTests` loads the generated scene and checks that it never hosts or serves by itself,
  that Host / Leave put it online and back, and that a bad address is refused with a reason.

- ✅ The original's art, sound and fonts in the project (UQD-T32, `AssetManifestTests`): `OriginalAssetGenerator`
  (menu *QuickDuel → Import Original Assets*, or `-executeMethod Unity.QuickDuel.EditorTools.OriginalAssetGenerator.Generate
  -originalRoot <BushidoCoda.gmx>`) copies out of the GameMaker project the 76 sprites the parity tickets draw (the five
  fighters' idle, run, attack and defeat frames, the 12 weapon and ground-weapon sprites, the 11 arenas `load_arenas.gml`
  names, the 14 loading images, the title, four corner arts, the katana cursor, two blood drops, ten splatters and the
  flash), the twelve duel, hit-ground, flick and click sounds, the five bitmap fonts (atlas plus GameMaker glyph table)
  and the Old English TrueType face, plus `credits.txt`, and writes `Data/assets.json` from each sprite's
  `.sprite.gmx` (size, origin, frames) and `load_arenas.gml` (names, descriptions). `OriginalAssetImportRules` applies
  the import settings in code on every import: single sprite, 1 pixel per unit, point filter, no mipmaps, no
  compression, pivot at the GameMaker origin. The copies and their `.meta` files are committed, so neither the game nor
  CI needs the original; `AssetManifest` (Core) validates the manifest on load. The one exception is
  `sfx_defeat.ogg`: the original's `.ogg` fails Unity's FMOD import, so the committed re-encode of its `.wav`
  (UQD-T24) is kept and the generator never overwrites it.

- ✅ The 11 arenas and a real invert (UQD-T33, `ArenaDrawTests`, `DuelViewTests`): every round is fought on one of
  `load_arenas.gml`'s arenas (Wastes of Nod, Pastel Fields, Frozen Tundra, The Fumaroles, Dusty Junction, Sozza's Point,
  South Ridge, Arid Plains, Interior Sea, Tu'verone Forest, Harvest Cresent), read from the data file
  `Data/assets.json` (name, description, backdrop sprite) by `DuelSceneBuilder` into `DuelView`'s serialized arena list.
  The authority draws an arena roll with each round (`MatchSession.ArenaRoll` locally, `OnlineDuelHost.ArenaRoll`
  online, sent in the `RoundBegan` RPC so both peers show the same arena) and `ArenaDraw.Pick` maps it onto the list;
  the backdrop covers the screen behind everything else. On the cue the whole backdrop inverts with a UI Toolkit
  `invert` filter, the same per-pixel `1 - colour` as `obj_arena`'s `draw_set_blend_mode_ext(10, 1)` (inverse
  destination colour) rectangle over the view; the old tint of the placeholder boxes and labels is gone, and as in
  the original only the backdrop inverts, not what is drawn over it.
  - **Fixed defect**: `load_arenas.gml` gives South Ridge Sozza's Point's id (5), so South Ridge overwrote it and
    `irandom_range(0, 9)` could only ever draw 10 arenas; every one of the 11 is drawn now.
  - **Deliberate deviation**: the original rolled the arena once per game (`load_arenas.gml` and
    `choose_random_arena.gml`, kept by `obj_game`); the rebuild draws one per round, so the duels of a match do not
    all look alike (UQD-US-P2).
  - The display name keeps the original's spelling, "Harvest Cresent", as `load_arenas.gml` has it; the names and
    descriptions were only shown on the dropped overworld (`obj_locationart`), so nothing shows them yet.

- ✅ The five original fighters, cosmetic only (UQD-T34, [§4.5](#UQD-§4.5); `FighterDrawTests`, `AssetManifestTests`,
  `DuelViewTests`): Myrmidon, Ronin, Gypsy, Vagabond and Exile from `load_characters.gml` (name, idle, run, attack and
  defeat strips, weapon and ground weapon; no stats) in `Data/assets.json`, carried into the scene by `DuelSceneBuilder`
  as `DuelView`'s `FighterLook` list in place of the placeholder boxes. The authority draws a fighter roll per match
  (sent with `RoundBegan` online); the pair stands in its idle sprites at a quarter and three quarters of the width,
  facing each other, fades in at 0.01 alpha per original 60 fps step, and is tinted red and blue when both seats hold
  the same fighter.
- ✅ The fighters' motion (UQD-T35, [§4.5](#UQD-§4.5); `FighterMotionTests`, `DuelViewTests`): on the cue both run
  from 40% and 60% of the width, creeping together 0.01 pixel a 1000 fps step; the winner dashes from the middle on to
  the loser's mark (easing `1 + 0.1` of the gap a 60 fps step) playing its attack strip in front, the loser plays its
  defeat strip at the middle, and three seconds on the winner returns to idle. The loser no longer topples.

## 7. Frontier {#UQD-§7}
- ⬜ Port gaps found by UQD-T19: a strike during the 3-2-1 countdown should be ignored, not lose
  (UQD-T22); Escape quits the player (UQD-T23); the victory / defeat sting (UQD-T24); a mouse /
  touch strike for the left duelist (UQD-T25).
