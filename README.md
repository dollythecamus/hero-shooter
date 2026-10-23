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
   `weapons-and-projectiles`, `enemy-fsm`, `characters-and-abilities`, `hud-aiming-and-feedback` (planned),
   `editor-mcp-tooling`).
9. `docs/tasks/` — one file per sprint block: what was built, what was verified, what was reverted.

## Status

S1 (player + camera + pooled shooting), S2 (enemy FSM, both archetypes) and S3 (3 characters, 6 abilities,
authored character select) are landed. S3's six ability *effects* were never measured in play mode; the ability
rework, HUD/aiming/VFX and the remaining brief items (minimap, Tab frags, map select, tutorial) are not built.
Truth lives in `docs/PROJECT_LEDGER.md`.

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
