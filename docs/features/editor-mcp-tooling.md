# Editor MCP tooling — MCP for Unity

> **Currency note (2026-11-06):** this document describes the system as designed and built on 2026-10-23/24. Everything that changed afterwards is listed in the *2026-11-06 update* at the bottom; where the two disagree, the update wins and `docs/PROJECT_LEDGER.md` has the evidence.

Installed 2026-10-23, same integration and pin as `UnityXR_3DMuseu`:

    "com.coplaydev.unity-mcp": "https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#v10.0.0"

Pinned to `#v10.0.0` (2026-06-30 release) for determinism; Unity 6.x supported.
Recorded here because the package line change in `Packages/manifest.json` is a project decision,
not an incidental import.

## Why

The agent cannot wire prefabs, scenes or ProBuilder geometry by editing `.unity` / `.prefab` YAML while
the GUI editor holds the project — the editor's next save overwrites the disk edit. The bridge lets the
agent drive the open editor instead.

## Setup (once per editor session)

1. Open the project normally; Package Manager imports the git package
   (`Library/PackageCache/com.coplaydev.unity-mcp@<hash>/`).
2. MCP server half needs `uv` (Python 3.10+) on PATH: present on this host as
   `/home/chloe/.nix-profile/bin/uv` (uv 0.11.21, verified 2026-10-23). If the editor's PATH lacks it,
   launch the editor from a shell that has it.
3. **Window → MCP for Unity → Auto-Connect.** The operator starts this; it is not auto-running.
   It spawns `mcp-for-unity --transport http --http-url http://127.0.0.1:8080`.
4. Probe: `curl -sS http://127.0.0.1:8080/health` → `{"status":"healthy","version":"10.0.0"}`.

## v10 transport notes

- FastMCP **HTTP** on `127.0.0.1:8080` (`/mcp`), not the old TCP StdioBridgeHost. The skill's
  `scripts/mcp_unity_client.py::find_unity_port` returns None for v10 — use the HTTP client recipe in
  `unity-maestro/references/mcp-for-unity-bridge.md`.
- Client side: run the `mcp` package from the same uv environment the editor spawned
  (`ps aux | grep mcp-for-unity` → `/home/chloe/.cache/uv/archive-v0/<hash>/bin/python`).
- `execute_code` compiles with **CodeDom C# 6** unless Roslyn is installed: no string interpolation,
  no out-vars. `scripting_ext` group must be activated with `manage_tools` before `execute_code` works.

## Pitfalls

- A successful `manage_editor play` does not prove play mode started — verify `Application.isPlaying`
  via `execute_code`, and grep `Logs/Editor.log` for `error CS`.
- **A play-mode transition returns stale edit-mode values.** `execute_code` fired during the transition sees
  `Application.isPlaying == False` and a scene where no `Start` has run; `mcpforunity://editor/state` can be
  30 s stale (`staleness.is_stale`). Read the state twice, and use `Time.time`/`Time.frameCount` to judge whether
  the game is actually ticking.
- **Game time advances only as the editor renders frames, and one `execute_code` call buys roughly one frame**
  (clamped by `Time.maximumDeltaTime`, 0.333 s), so `sleep 8` in the shell does not advance an 8 s cooldown.
  Probe with an `UnityEditor.EditorApplication.update` callback that samples and `Debug.Log`s, let it run, then
  read `Logs/Editor.log` from disk (`grep -a '\[Probe\]' Logs/Editor.log`) — that is how the 2026-10-23 ability
  probes were measured. Set `Time.timeScale`/`Time.maximumDeltaTime` back to 1 / 0.333 before finishing.
- **Enter Play Mode Options here have Reload Scene *and* Reload Domain disabled**, so play runs on the open
  scene and runtime mutations leak into it: a play session on 2026-10-23 wrote `90_TestArena.unity` (camera rig
  position/distance), `CharacterSelect.prefab` (canvas render mode) and `ProjectSettings/TimeManager.asset`
  (the probe's `timeScale` 4 / `maximumDeltaTime` 8). After any play-mode work run `git status --short`, restore
  the churn with `git checkout --`, then reload the scene from disk
  (`EditorSceneManager.OpenScene(path, OpenSceneMode.Single)`) so the editor does not save it again.
- Leftover `EditorApplication.update` callbacks survive across `execute_code` calls (each call compiles a new
  assembly) and keep running in later probes — give every callback a self-cancel condition and prefer one probe
  per play session.
- Batchmode runs (`-batchmode -quit`) and the GUI editor must not share the project: check
  `Temp/UnityLockfile` and `pgrep -f <project path>` before any headless run.

## 2026-11-06 update — port 8089, and how prefabs/scenes were actually edited

- **The bridge listens on `127.0.0.1:8089`, not 8080** (8080 was already taken on this host).
  `curl -sS http://127.0.0.1:8089/health` → `{"status":"healthy",...,"version":"10.0.0"}`.
- **Prefab edits** go through `execute_code` with `PrefabUtility.LoadPrefabContents(path)` → mutate →
  `SaveAsPrefabAsset` → `UnloadPrefabContents`. That is how the HUD's `GraphicRaycaster`, the sliced panels, the
  `MainMenu` button (wired with `UnityEventTools.AddPersistentListener`), `MinimapBlips` and the sniper reticle were
  authored. Serialized private fields are set through `SerializedObject`.
- **Scene edits** (steps text, navmesh bake) use the same `execute_code` path with
  `EditorSceneManager.MarkSceneDirty` + `SaveScene`, and `OpenScene(path, Single)` to prove the result persisted.
- **Input can be injected for real:** `InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.Escape))`
  produced a working Esc press, which is how the pause/cursor path was verified end-to-end.
- **UI clicks can be verified without a mouse:** raycast the `EventSystem` at the button's rect centre, then
  `ExecuteEvents.Execute(hit, ped, ExecuteEvents.pointerClickHandler)` — that is how "the button loads `00_Boot`"
  was proven. Beware raycasting in the same frame a panel is activated: `Graphic.depth` is still −1 and nothing hits.
- Play-mode probes still dirty scenes; commit before playing so `git checkout` is a safe restore.
