# MindAttic project agent entrypoint

Read the shared protocol at ..\mindattic-agent-standard\AGENTS.md and this project's own
documentation. Common prompt commands are implemented by the shared runner; do not add a second
copy under a provider-specific command folder.

## Project context

- The duel rules live in `Assets/Unity.QuickDuel/Core` and must stay engine-free
  (`noEngineReferences: true`); they compile outside Unity through `Tests/Unity.QuickDuel.Core`.
- Run the rule tests with `dotnet test Core.slnx` (NUnit).
- The game is a port of the gameplay of Ryan's old "Bushido Coda" (Android 2011-13, GameMaker 2013);
  see `docs/ORIGINS.md`. Keep only the reaction duel — no maps, skills, stats or dials.
