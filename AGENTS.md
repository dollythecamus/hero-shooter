# AGENTS.md — hero-shooter

Unity 6 third-person hero shooter (UNICAP assignment, due 2026-10-30 22:00). The only gameplay scene is
`Assets/_Project/Scenes/90_TestArena.unity`; the code entry point is `MatchBootstrap` on the `Match` object.
Read `ARCHITECTURE.md` first, then only the feature doc for the system you are touching.

## Commands

Everything below was run on this host on 2026-10-23 unless marked `UNVERIFIED`.

```bash
# Open the project (GUI editor — the normal working mode)
~/Unity/Hub/Editor/6000.6.3f1/Editor/Unity -projectpath /home/chloe/projects/hero-shooter
#   verified: an editor was already running with exactly this argument (pgrep -af 6000.6.3f1)
#   while it is open, Temp/UnityLockfile exists and holds the project — nothing else may open it

# Does the project compile? (no CLI compile has ever been run on this project)
ls -l Library/ScriptAssemblies/Assembly-CSharp.dll   # fresh timestamp after a recompile
grep -c "error CS" Logs/Editor.log                   # verified 0 → no compile errors in the current session
#   UNVERIFIED — headless compile:
#   Unity -batchmode -quit -nographics -projectPath . -logFile -
#   blocked because the GUI editor currently holds the project; quit the editor first

# Static done-criteria greps (the only automatable gates that exist)
grep -rn "Instantiate(" Assets/_Project/Scripts    # must return ONLY Core/ObjectPool.cs:43
grep -rn "attackRange" Assets/_Project/Scripts     # EnemyStats.attackRange is read by nobody — see enemy-fsm.md
git diff --check                                   # verified clean; keep it clean in every commit

# Editor bridge (how the agent wires prefabs/scenes/ProBuilder instead of editing YAML)
curl -sS http://127.0.0.1:8080/health              # verified {"status":"healthy",...,"version":"10.0.0"}
#   setup + pitfalls: docs/features/editor-mcp-tooling.md

# Builds
#   UNVERIFIED — never run from the CLI on this project. Use the editor: File > Build Profiles,
#   targets Linux x86_64 and Windows x86_64 (Windows module installed, Mono backend).
#   No executeMethod build class exists.

# Play-mode verification (there are no automated tests)
#   Editor Play mode + console probes through the MCP bridge (execute_code / debugger_get_log).
#   Every gate in docs/tasks/*.md was measured this way, never inferred from "it compiles".
```

No test assembly exists. `com.unity.test-framework` is installed but unused: do not present
`dotnet test` or Unity Test Runner as a working gate.

## Permission boundaries

- **MAY do unattended**: edit `Assets/_Project/Scripts/**`, create/edit SO instances under
  `Assets/_Project/Data/**`, wire existing prefabs and the test scene through the MCP bridge, write
  `docs/**`, commit.
- **NEEDS sign-off**: deleting a script, component or asset that is referenced from a prefab or scene;
  changing `Packages/manifest.json`, project layers/tags, `ProjectSettings/**`, build profiles; changing
  serialized tuning values that Chloe set (`docs/HERO_ROSTER.md` decision tables); anything that changes
  gameplay behaviour mid-block while she is play-testing.
- **NEVER**: hand-edit `.unity` / `.prefab` YAML while the GUI editor is open (its next save overwrites the
  file); run batchmode or a second editor against this project; commit `Library/`, `Temp/`, `Logs/`,
  `UserSettings/`, `*.csproj`, `*.sln`; repurpose template assets (`Assets/TutorialInfo/`, `Assets/Settings/`,
  `Assets/Scenes/SampleScene.unity`); start a code phase after a halt — a halt stops all writes.

## Done criteria

A task is complete when:

- The static greps above still pass, and `grep -c "error CS" Logs/Editor.log` is 0 after a recompile.
- The behaviour named in the task's gate was **observed in play mode** and the observation is quoted
  (numbers, log lines), not summarised as "works".
- `git diff` was read by Hermes, not only produced by the implementing model.
- `git status --short` shows only intended paths — no `Library/`, `Temp/`, `Logs/`, no stray `.meta` churn.
- The touched `docs/features/*.md` is updated from the actual diff, and `docs/PROJECT_LEDGER.md` records the
  block, the gate result and the uncommitted files at checkpoint.

## Worker delegation

Coding model: `opencode` (`--model opencode-go/deepseek-v4.1-flash`, stronger on request). Attach only the
touched feature doc(s) plus this file — never the whole `docs/features/` directory. Scope every delegation with
an explicit out-of-bounds file list. Details: `docs-driven-dev-pipeline/references/delegation.md`.

## Comment policy

Comments carry only what code cannot say: why a non-obvious choice exists, a gotcha, an external constraint.
Serialized field names, layer-mask integers and prefab wiring constraints *are* legitimate comment material
(they are invisible in the C#). Never restate the code; never write "increment counter" above an increment.
