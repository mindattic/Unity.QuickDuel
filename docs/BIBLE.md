---
codex: 1
project: Unity.QuickDuel
code: UQD
layer: bible
status: living
updated: 2026-10-22
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
- **Unreadable cue.** The standoff is drawn uniformly from 3 to 6.5 seconds after the 3-2-1 countdown, so
  the only way to win is to react, not to count.
- **Patience is a skill.** Striking during the standoff loses the round outright, and the loser is taunted
  with a line from the old Android build ("Jumped the Gun", "Too Soon", ...).
- **Couch first.** Two players on one machine (keyboard A / L, or one gamepad each), or one player against
  an AI on Easy, Normal or Hard. A match is best of 3.

## 3. What it is NOT {#UQD-§3}
- **NOT Bushido Coda.** No overworld, merchants, weather, items, stats (ART, LCK, PWR), focus bar, skill
  dials, combos, clash button-mash, or arenas and characters as gameplay. Those were dropped on purpose;
  adding any back needs a Bible change first.
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
                                                      OnlineDuelHost, OnlineDuelClient
  Runtime/   MonoBehaviours on the one "Duel" object  DuelController, DuelInput, DuelSession, DuelView, DuelTaunt, DuelAudio
  Online/    the wire, over Unity.Multiplayer         OnlineDuel (a NetEntity; compiles only with the package present)
  Editor/    generators                               DuelSceneBuilder (writes Assets/Scenes/Duel.unity), QuickDuelBuild
  Tests/PlayMode/                                     scene, input, session, view, taunt and audio tests
  Tests/Online/                                       host + client over the Unity.Multiplayer test kit
Tests/Core.Tests                                      NUnit tests of Core, run with `dotnet test Core.slnx`
```

### 4.1 A round {#UQD-§4.1}
`Duel` is one round, a phase machine driven by caller-supplied timestamps in seconds:

| Phase | Lasts | On screen and in the ear |
| --- | --- | --- |
| `Ready` | until Space / Start | title and controls |
| `Countdown` | `CountdownSeconds` (3) | 3-2-1, a beep on each number |
| `Standoff` | `MinStandoffSeconds`–`MaxStandoffSeconds` (3–6.5), random | letterbox closes in, drumroll |
| `Strike` | until both strike, or `StrikeTimeoutSeconds` (3) | colours invert, the cue, the blade unsheathed |
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
`DuelSession` owns the mode (Local PvP → vs AI Easy → Normal → Hard, cycled with Tab / Select between
rounds only) and the `Match`. A match is first to 2 wins (best of 3); ties and no-strike rounds award
nobody; changing mode starts a fresh match. The scoreboard shows wins and each side's best reaction.

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
batch mode); the UI is built in code by `DuelView`. Never hand-edit the scene YAML.

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
- ✅ Standalone Windows build with `deploy.ps1` (UQD-T11).
- ✅ Docs site on GitHub Pages via the shared pipeline (UQD-T10).
- ✅ Online PvP protocol and transport over Unity.Multiplayer, per [§4.4](#UQD-§4.4) (UQD-T14):
  `OnlineDuelTests` in Core.Tests, and host + client PlayMode tests in `Tests/Online`.
- ✅ The round headline names the AI in vs-AI mode (UQD-T17, `DuelSessionTests`).

## 7. Frontier {#UQD-§7}
- ⬜ Online PvP in the Duel scene: a Host / Join menu, and `DuelSession` / `DuelView` driven by an
  `OnlineDuel` instead of a local `Duel`.
