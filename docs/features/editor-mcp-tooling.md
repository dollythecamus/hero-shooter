# Editor MCP tooling — MCP for Unity

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
- Batchmode runs (`-batchmode -quit`) and the GUI editor must not share the project: check
  `Temp/UnityLockfile` and `pgrep -f <project path>` before any headless run.
