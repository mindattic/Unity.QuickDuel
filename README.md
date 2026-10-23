# Unity.QuickDuel

Two duelists face each other and wait. The moment comes. Whoever strikes first wins.

That's the whole game:

1. A 3-2-1 countdown.
2. A standoff of unknown length (3 to 6.5 seconds).
3. The cue: the screen flashes and a blade is drawn.
4. Both players hit their key. The faster reaction wins.
5. Strike before the cue and you lose.

## Layout

| Path | What |
| --- | --- |
| `Assets/Unity.QuickDuel/Core` | Engine-free duel rules (`Duel`, `DuelSettings`, `DuelResult`) |
| `Tests/Core.Tests` | NUnit tests for the rules, run outside Unity |
| `docs/BIBLE.md` | Game design: what the game is and isn't, the round, judging, and the project laws |
| `docs/ORIGINS.md` | Where the idea comes from: Samurai Kirby, then the old Bushido Coda builds |
| `docs/RECONSTRUCTION.md` | Reverse-engineering of the original, what the rebuild still lacks, and the ordered build plan |

## Build and test

Open the folder in Unity 6000.4.3f1. Test the rules without the editor:

```
dotnet test Core.slnx
```

Build the Windows player to `Build/QuickDuel/QuickDuel.exe` (exits non-zero on failure):

```
powershell -File deploy.ps1
```

Controls: left player A or gamepad 1 south button, right player L or gamepad 2 south button,
Space or Start begins the next round. Tab or Select (between rounds) cycles local PvP and vs AI
(Easy, Normal, Hard; the AI plays the right side). Escape, or gamepad Select held while pressing
Start (between rounds), quits the player. A match is best of 3.

Rounds are judged by input event timestamps, not by frame, so the frame rate can't decide a duel.
