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
| `docs/ORIGINS.md` | Where the idea comes from: Samurai Kirby, then the old Bushido Coda builds |

## Build and test

Open the folder in Unity 6000.4.3f1. Test the rules without the editor:

```
dotnet test Core.slnx
```

Rounds are judged by input event timestamps, not by frame, so the frame rate can't decide a duel.
