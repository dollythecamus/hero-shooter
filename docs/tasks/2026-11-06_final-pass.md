# 2026-11-06 — final pass: six human gates, the rework, and publication

**Status:** closed. This is the block that took the project from "code exists" to "delivered".
Deadline note: the brief said 2026-10-30, the class granted an extension to 2026-11-03, and on 2026-11-06 the
professors accepted a late delivery outright — so this last block was finished on merit, not against a clock.

## What was wrong at the start

Nothing had ever been *played*. Every claim in the repo before this block came from headless probes, and the
built binaries had not been launched by a human. Six passes later, each one found something no probe had.

## The six gates

| Gate | What she reported | Cause found | Fix |
|---|---|---|---|
| 1 (11:00, 15 min trial) | HUD boxes stretched; minimap shows no enemies; pause buttons dead; mouse stays free after resume; frag board PASS | panels used a 10 px rounded sprite at `Simple`; the minimap drew only the hero; **the HUD canvas had no `GraphicRaycaster` at all**; the cursor was set once in `Resume()` | panels sliced; `MinimapBlips`; raycaster added; cursor re-derived every frame |
| 2 | Esc leaves the cursor unlocked; camera keeps turning behind the panel; bar still smeared; point-blank enemies not hit | pause state lived in two code paths; mouse look is read in raw pixels so `timeScale` never stopped it; fills still used the rounded sprite; the shot started 0.5 m past the hero | single `SetPaused` + `EnforceCursor`; camera returns early when frozen; borderless `White.png` on 35 images; round starts at the hero's depth and `Projectile` ignores its owner |
| 3 | tutorial: dark overlay, text outside the panel (she fixed both), enemy-health wording, end button unclickable and inert | the button had **two** causes: my own cursor enforcement locked the pointer, and the exit called `GameFlow.Instance`, which does not exist in that scene | completion freezes and frees the cursor, timed exit on unscaled time, `SceneExit` fallback; copy rewritten in player language |
| 4 | all PASS except Scissors' Overclock reloading immediately | not a bug in the ability: 7/s × 1.8 empties a 12-round magazine in 0.95 s | her decision — refill the magazine on activation (`Weapon.RefillMagazine`) |
| 5 | only some corpses despawn; dead bodies still stand like dummies | `WaveDirector` recycled only what it spawned; hand-placed enemies were tracked for bars but never for recycling | `Track()` covers every enemy; pooled → pool, hand-placed → deactivate; `EnemyBrain.TipOver/RestorePose` lays the body down and stands it back up |
| 6 | corpses PASS | — | closed |

A **seventh** issue came from the level side rather than a gate: enemies walked through cover on both maps. The
scenes carried a stale *legacy* navmesh next to the `NavMeshSurface` data, so a plain rebake changed nothing. Clearing
the legacy dataset first is what took effect; the tutorial had no surface at all and got one.

**Also fixed outside the gates:** every weapon gained ammunition and a reload (no reserve, so a run cannot end
empty-handed), per-hero aim FOV with Paper at 30°, the wall climb went 3 → 5 m/s with a real mantle, fire rates came
down (Rock 1.1 → 0.85/s, Paper 1.0 → 0.7/s), and the assignment's "+3 mechanics" were confirmed as the wave director,
damage numbers and the pause menu.

## Verification used

- **Play-mode probes** through the editor bridge: captures of component state, `NavMesh.SamplePosition` inside every
  obstacle footprint, pool counters, blocked-vs-allowed paths.
- **Injected input:** `InputSystem.QueueStateEvent` for real Esc presses; `EventSystem` raycast + `ExecuteEvents` to
  click real buttons. That is how "the pause menu's Main Menu loads `00_Boot`" and "the tutorial's BackButton loads
  `00_Boot`" were proven, and how the camera freeze was measured (300 px delta → yaw 36.00° playing, 0.00° paused).
- **Six human passes** on built binaries — the only evidence that counts for "does it look and feel right".

## Artefacts

- Builds: Linux 102.98 MB, Windows 102.50 MB, zero errors, from the final commit.
- Packages: `hero-shooter-2026-11-06-{linux,windows}.zip` (38.4 / 39.9 MB) in `~/builds/`, CRC-tested and one of them
  extracted and launched headless to prove the copy runs.
- Repo: https://github.com/dollythecamus/hero-shooter (private) with release `v1.0.0` carrying both archives.
- Documents: `README.md`, `ARCHITECTURE.md`, `AGENTS.md`, `docs/CODE_DEFENSE.md` and the seven feature docs were all
  brought to the final state in this block; `docs/PROJECT_LEDGER.md` holds the evidence trail.

## What this block did not do

No automated tests were added; the accept criteria stay "observed in play, quoted with numbers". Enemies are not
networked, there is no save, and there is one game mode.
