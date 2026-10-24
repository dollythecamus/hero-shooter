# Hero Shooter

UNICAP — *Level Design e Programação 3D*, individual project. Due **2026-10-30 22:00**.
Presentation 18:30–20:00, source-code explanation 20:20–22:00.

A third-person wave shooter in Unity 6 (URP): pick 1 of 3 characters, pick a map, fight escalating
waves of FSM-driven ranged and melee enemies with pooled projectiles and two abilities per character.

## Read in this order

1. `ARCHITECTURE.md` — what exists: stack, components, conventions, directory map, feature-doc index.
2. `AGENTS.md` — commands that work here, permission boundaries, done criteria.
3. `docs/ASSIGNMENT.md` — the brief as received (pt-BR) + English translation + requirement checklist.
4. `docs/PLAN.md` — sprint plan and requirement→system map (its § 3 file list is partly aspirational; see
   ARCHITECTURE.md *Not built yet*).
5. `docs/PROJECT_LEDGER.md` — what is true **now**: status, next action, open questions.
6. `docs/HERO_ROSTER.md` — the three heroes, their abilities as specified, aiming and feedback requirements.
   Where it disagrees with the code, the roster wins.
7. `docs/LAYERS.md` — layer indices and the mask values that follow from them.
8. `docs/features/` — one doc per system (`damage-and-pooling`, `player-movement-and-camera`,
   `weapons-and-projectiles`, `enemy-fsm`, `characters-and-abilities`, `hud-aiming-and-feedback`,
   `editor-mcp-tooling`).
9. `docs/tasks/` — one file per sprint block: what was built, what was verified, what was reverted.
10. `docs/studies/` — process studies, not part of the game. `2026-10-24-loop-retro.md` is the doc-drift
    post-mortem of this repo (23 human interventions, six documentation failure modes) plus the plan for a
    fully autonomous agent loop; it is the reason `AGENTS.md`'s gates are shaped the way they now are.

## Status

S1 (player + camera + pooled shooting), S2 (enemy FSM, both archetypes), S3 (3 characters, 6 abilities, authored
character select), S3b (ability rework), S4 (aiming, authored HUD, enemy bars, pooled VFX) and S5 (boot menu →
character select → map select, two ProBuilder maps, gated tutorial) are **landed and measured in play mode**.
Still to build: **S6** (the three self-chosen mechanics — default wave director, ultimate meter, pause menu,
blocked on Chloe's pick) and **S7** (Linux + Windows builds, `docs/CODE_DEFENSE.md`, submission); the minimap and
Tab frag board are deferred out of the sprint. Nothing has been built or *looked at* yet — every gate is a
play-mode measurement, not a judgement of how it looks. Truth lives in `docs/PROJECT_LEDGER.md`; scope and gates
per block in `docs/PLAN.md` § 6.

## Layout

```
Assets/_Project/   gameplay tree — scenes, scripts, SO data, prefabs, materials (full map in ARCHITECTURE.md)
Assets/Settings/   URP assets from the template
docs/              assignment brief, plan, ledger, roster, layers, feature docs, per-block task files
```

## Project facts

- Unity **6000.6.3f1**, URP 17.6.0, Input System, AI Navigation, ProBuilder
- No imported art: ProBuilder primitives + URP Lit materials. Levels are ProBuilder meshes.
- Build targets: Linux x86_64 and Windows x86_64 (Windows module installed 2026-10-23, Mono backend)
