# Project ledger — hero-shooter

Updated: 2026-10-23 11:00 (-03) · session 20261023_105517_ed63d44c · owner default profile

## Goal

A buildable Unity 6 third-person hero shooter that satisfies all 11 minimum mechanics in
`docs/ASSIGNMENT.md`, submitted to the LMS by 2026-10-30 22:00 and defensible out loud in the
20:20–22:00 source-code session.

## Done when

- Build artifact runs from a clean folder and every checklist row in `docs/ASSIGNMENT.md` is
  demonstrable in it.
- `docs/CODE_DEFENSE.md` has one block per system, each verified by opening the named file.
- LMS shows a submitted file before 2026-10-30 22:00.

## Status

scaffold — `docs/`, `README.md`, `.gitignore`/`.gitattributes` exist; **no Unity project yet**
(`Assets/`, `ProjectSettings/`, `Packages/` absent).

## Next action

Create the Unity project in place: URP blank template on editor 6000.6.3f1 at
`/home/chloe/Unity/Hub/Editor/6000.6.3f1`, then commit `Assets/`, `Packages/`, `ProjectSettings/`.

**Stopping point:** editor opens the empty `10_Tutorial`-less project and play mode runs once — stop
and report before writing any gameplay script.

## Open questions

- Which 3 custom mechanics? (proposal A wave director + B ultimate meter + E pause menu) — Chloe
- The 3 characters and their 2 abilities each — Chloe (design), needed by 2026-10-26
- Art approach: grey-box (default) or Blender models — Chloe
- Build target: Windows, Linux, or WebGL — Chloe
- GitHub repo `dollythecamus/hero-shooter` — create on first push, or keep local only? — Chloe

## Decisions

- 2026-10-23 — Unity 6000.6.3f1 + URP, matching `~/projects/shootshoot` — only editor installed,
  and its URP project lets `PlayerControl` / `ShootPool` / `Projectile` be reused directly.
- 2026-10-23 — grey-box first, art last — 7-day deadline, code explanation is half the grade.
- 2026-10-23 — single generic `EnemyBrain` FSM with per-type attack strategy — makes the code
  walkthrough "one brain, two bodies" instead of duplicated AI.

## Evidence

- `docs/reference/assignment.jpeg` — the brief as received (due date, mechanics, assessment slots).
- `docs/ASSIGNMENT.md` — verbatim pt-BR + translation + 11-row checklist.
- `~/projects/shootshoot/Assets/shootshoot/Scripts/{PlayerControl,ShootPool,Projectile}.cs` —
  reusable third-person motor and pooled shooter (132 / 82 / 42 lines).
- `ls /home/chloe/Unity/Hub/Editor/` → `6000.6.3f1` (only editor).

## Uncommitted at last checkpoint

nothing committed yet — initial commit pending

## Handoff

Created the project directory and its planning docs from the LMS screenshot. Verified: editor
version installed, URP template tarball present, reuse candidates in `shootshoot` real. NOT
verified: anything Unity-side — no project exists yet, so nothing has been opened or run. Next:
create the Unity project (see Next action).

## Dispatches

none

## Requirements log

- 2026-10-23 (session 20261023_105517_ed63d44c) — read the assignment image, translate it to English,
  create the project directory, write the plan — landed in `docs/ASSIGNMENT.md`, `docs/PLAN.md`, this ledger.
