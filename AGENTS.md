# AGENTS.md — hero-shooter

Unity 6 third-person hero shooter (UNICAP assignment; the brief said 2026-10-30, a late delivery was agreed with
the professors on 2026-11-06). **The project is complete, played and published** — `docs/PROJECT_LEDGER.md` is the
record of what was verified and when; this file is how to work on it. Five scenes, in build order:
`00_Boot` (menu, `GameFlow`, both select screens) → `10_Tutorial` → `20_Map_Arena` → `21_Map_Alley` →
`90_TestArena` (the dev scene, still opens the character select inside the match). Scene routing is `GameFlow`
(`Scripts/Systems/GameFlow.cs`) and it is the only `SceneManager.LoadScene` caller; a match scene starts by
`MatchBootstrap` applying the carried pick. `Scripts/Systems/SceneExit.cs` is the one deliberate second exit
(it prefers `GameFlow` and falls back to loading `00_Boot`, for scenes opened without the boot flow).
Read `ARCHITECTURE.md` first, then only the feature doc for the system you are touching
(`docs/features/match-flow-and-maps.md` for anything touching scenes, maps or tutorial).

> **Shell trap:** the agent's `HOME` is `/var/lib/hermes`, but the GitHub credentials live in
> `/home/chloe/.config/gh`. Run `gh` and `git` for this repo with `HOME=/home/chloe`; the repo carries a
> repo-local `credential.helper = !gh auth git-credential` so pushes authenticate without touching the global
> git config.

## Commands

Everything below was run on this host. The "UNVERIFIED" markers from the 2026-10-23 draft are gone: every command
here has since been executed, most of them many times (see `docs/PROJECT_LEDGER.md` for the runs).

```bash
# Open the project (GUI editor — the normal working mode)
~/Unity/Hub/Editor/6000.6.3f1/Editor/Unity -projectpath /home/chloe/projects/hero-shooter
#   while it is open, Temp/UnityLockfile exists and holds the project — nothing else may open it

# Does the project compile? (through the editor bridge, not from the CLI)
python3 mcp_unity.py call refresh_unity '{"mode":"force","scope":"all","compile":"request","wait_for_ready":true}'
python3 mcp_unity.py call read_console '{"count":80}'      # filter for "error CS"
#   cumulative-log caveat still applies: grep -c "error CS" over Editor.log can never return to 0.
#   A tiny MCP client lives in the scratch dir (mcp_unity.py) and talks JSON-RPC over POST /mcp.

# Static done-criteria greps
grep -rn "Instantiate(" Assets/_Project/Scripts    # must return ONLY Core/ObjectPool.cs:43
grep -rn "SceneManager.LoadScene(" Assets/_Project/Scripts  # GameFlow.cs (3) + SceneExit.cs (1)
git diff --check                                   # keep it clean; Unity's own trailing spaces excepted

# Editor bridge (how prefabs/scenes/ProBuilder are wired instead of editing YAML)
curl -sS http://127.0.0.1:8089/health              # {"status":"healthy",...,"version":"10.0.0"}
#   PORT IS 8089 on this host (8080 was taken) — Window > MCP for Unity > Auto-Connect starts it.
#   Setup + pitfalls: docs/features/editor-mcp-tooling.md

# Builds (in-editor, both targets, from Build Settings' scene list)
#   through the bridge: manage_build {"action":"build","target":"linux64"|"windows64","output_path":"..."}
#   headless equivalent (editor must be closed):
tools/unity-run.sh -batchmode -nographics -projectPath . \
  -executeMethod HeroShooter.EditorTools.BuildScript.BuildLinux
#   output: /home/chloe/builds/hero-shooter/{linux,windows}; archives in /home/chloe/builds/*.zip

# Publish (see the shell trap above)
export HOME=/home/chloe
git push origin main && gh release view v1.0.0

# Play-mode verification (there are no automated tests)
#   Editor Play mode + console probes through the MCP bridge (execute_code / read_console).
#   Keyboard/mouse can be injected: InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.Escape))
#   — that is how the Esc/pause path was proven end-to-end on 2026-11-06.
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

- The static greps above still pass, and the compile gate above returns 0 (no `error CS` line *after* the last
  `Tundra build success` — the cumulative count is not the gate).
- The behaviour named in the task's gate was **observed in play mode** and the observation is quoted
  (numbers, log lines), not summarised as "works".
- `git diff` was read by Hermes, not only produced by the implementing model.
- `git diff --check` was checked too, with this caveat: **Unity's own serializer writes `m_Name: ` / `value: `
  with a trailing space for empty strings**, so a saved scene or prefab always adds a few `trailing whitespace`
  lines to that check — engine output, not code, and not hand-editable while the editor holds the file. Report
  them; do not "fix" them.
- `git status --short` shows only intended paths — no `Library/`, `Temp/`, `Logs/`, no stray `.meta` churn.
- The touched `docs/features/*.md` is updated from the actual diff, and `docs/PROJECT_LEDGER.md` records the
  block, the gate result and the uncommitted files at checkpoint.
- **A requirement is not done until a human ran it from a built binary.** Probe numbers are evidence about code,
  not about the game: on 2026-11-06 six human passes each found something no probe had (a canvas with no
  raycaster, a mouse that would not re-lock, bars smeared by a stretched sprite, bodies that never cleared).
  Record what she saw, in her words, next to what was measured.

## Worker delegation

Coding model: `opencode` (`--model opencode-go/deepseek-v4.1-flash`, stronger on request). Attach only the
touched feature doc(s) plus this file — never the whole `docs/features/` directory. Scope every delegation with
an explicit out-of-bounds file list. Details: `docs-driven-dev-pipeline/references/delegation.md`.

## Comment policy

Comments carry only what code cannot say: why a non-obvious choice exists, a gotcha, an external constraint.
Serialized field names, layer-mask integers and prefab wiring constraints *are* legitimate comment material
(they are invisible in the C#). Never restate the code; never write "increment counter" above an increment.
