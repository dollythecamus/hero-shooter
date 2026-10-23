# Project ledger — hero-shooter

Updated: 2026-10-23 13:05 (-03) · session 20261023_105517_ed63d44c · owner default profile
Sprint: **one long block today from 13:00**, then 10-24 → 10-29 as buffer (plan § 6–7).

## Goal

A buildable Unity 6 third-person hero shooter that satisfies all 11 minimum mechanics in
`docs/ASSIGNMENT.md`, submitted to the LMS by 2026-10-30 22:00 and defensible out loud in the
20:20–22:00 source-code session.

## Done when

- Linux **and** Windows builds run from a clean folder and every checklist row in
  `docs/ASSIGNMENT.md` is demonstrable in them.
- `docs/CODE_DEFENSE.md` has one block per system, each verified by opening the named file.
- LMS shows a submitted file before 2026-10-30 22:00.

## Status

project stands, no gameplay yet — Unity 6000.6.3f1 URP project imports clean (exit 0,
`Logs/import-2.log`: `Exiting batchmode successfully now!`), 31 packages resolved incl.
`com.unity.probuilder` + `com.unity.ai.navigation`, `Assets/_Project` folder tree created (26 metas),
commit `caf7525`, tree clean.

## Next action

S1 of the sprint (13:20): write `PlayerMotor` + `ThirdPersonCameraRig` (ported from
`~/projects/shootshoot/Assets/shootshoot/Scripts/PlayerControl.cs`), `ObjectPool<T>`,
`ProjectilePool`, `Weapon`, `Projectile`; grey-box a test arena in ProBuilder.

**Stopping point:** after the S1 gate — 60 rounds fired, pool object count unchanged from warm-up,
`grep -rn "Instantiate(" Assets/_Project/Scripts` showing warm-up only.

## Open questions

- Which 3 custom mechanics? decided at S6 (21:30); default A wave director + B ultimate meter + E pause menu
- The 3 characters and their 2 abilities — decided at S3 (17:10), it is design work, not code
- Git remote `dollythecamus/hero-shooter`: push or stay local? — Chloe
- Is the 13:00 start holding, and does the sprint run to ~23:30? — Chloe

## Decisions

- 2026-10-23 — Unity 6000.6.3f1 + URP — only editor installed; lets `shootshoot` code be reused.
- 2026-10-23 — **no art assets**: ProBuilder primitives + URP Lit materials (textured/coloured).
- 2026-10-23 — **levels are ProBuilder**; level design is a separate part of the discipline, not part of
  this brief, so the maps double as that evidence.
- 2026-10-23 — builds: **Windows + Linux**.
- 2026-10-23 — schedule: **one sprint today from 13:00**, remaining days are buffer + defense rehearsal.
- 2026-10-23 — free-choice mechanics and character designs are decided *inside* the sprint, once the
  mandatory systems stand (Chloe's call).
- 2026-10-23 — packages trimmed to inputsystem / URP / AI Navigation / ProBuilder / ugui / test-framework
  / IDE support; collab-proxy, visual scripting and timeline removed (fewer things to explain, faster import).
- 2026-10-23 — single generic `EnemyBrain` FSM with per-type attack strategy — "one brain, two bodies".

## Evidence

- `docs/reference/assignment.jpeg` — the brief as received.
- `docs/ASSIGNMENT.md` — verbatim pt-BR + translation + 11-row checklist.
- `Logs/import-2.log` (gitignored) — import exit 0, 0 compiler errors.
- `Packages/packages-lock.json` — 31 packages, probuilder 6.1.2, ai.navigation 2.0.12.
- `~/projects/shootshoot/Assets/shootshoot/Scripts/{PlayerControl,ShootPool,Projectile}.cs` — reuse source (132 / 82 / 42 lines).
- `ls /home/chloe/Unity/Hub/Editor/6000.6.3f1/Editor/Data/PlaybackEngines/` → `AndroidPlayer`, `LinuxStandaloneSupport` only — **no Windows support yet**; `unityhub install-modules --version 6000.6.3f1 -m windows-mono` started 13:00 (module id `windows-il2cpp` does not exist in this Hub).
- A headless import had already run on this path at 11:17 (`Logs/first-import.log`) before this session
  touched it — cause unknown; the tree was the same URP blank template, so nothing was lost.

## Uncommitted at last checkpoint

none — `git status --short` empty at commit `caf7525`.

## Handoff

Created the project directory, brief + plan + ledger (commit `d0c984e`); then, on approval, the Unity
project itself: URP blank template extracted to editor version 6000.6.3f1, manifest trimmed plus
ProBuilder, headless import run to completion, `Assets/_Project` skeleton in place (commit `caf7525`).
Verified: import exit 0 with no compiler errors, packages resolved, git tree clean. NOT verified: the
editor has not been opened interactively, no scene run, no build produced, and the Windows module
download is still in flight. Next: S1 player + shooting code.

## Dispatches

none

## Requirements log

- 2026-10-23 (session 20261023_105517_ed63d44c) — read the assignment image, translate it to English,
  create the project directory, write the plan — `docs/ASSIGNMENT.md`, `docs/PLAN.md`, this ledger.
- 2026-10-23 — revision: one sprint today from 13:00; mechanics decided when the mandatory work stands;
  characters decided when being built; no art, textured materials on simple shapes; ProBuilder levels;
  Windows + Linux builds; approve headless Unity project creation — `docs/PLAN.md` § 5–9, commit
  `caf7525`, project created.
