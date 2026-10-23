# Hero Shooter

UNICAP — *Level Design e Programação 3D*, individual project. Due **2026-10-30 22:00**.
Presentation 18:30–20:00, source-code explanation 20:20–22:00.

A third-person wave shooter in Unity 6 (URP): pick 1 of 3 characters, pick a map, fight escalating
waves of FSM-driven ranged and melee enemies with pooled projectiles and two abilities per character.

## Read in this order

1. `docs/ASSIGNMENT.md` — the brief as received (pt-BR) + English translation + requirement checklist.
2. `docs/PLAN.md` — architecture, requirement→system map, the one-day sprint schedule, decisions owed.
3. `docs/PROJECT_LEDGER.md` — what is true **now**: status, next action, open questions.
4. `docs/HERO_ROSTER.md` — the three heroes, their abilities as specified, aiming and feedback requirements.
5. `docs/LAYERS.md` — layer indices and the mask values that follow from them.
6. `docs/tasks/` — one file per sprint block: what was built, what was verified, what was reverted.

## Status

Unity project in place and importing clean — **no gameplay code yet**. Sprint block S1 is the next
step. See the ledger's *Next action*.

## Layout

```
Assets/_Project/   gameplay tree (scenes, scripts, data, prefabs) — see PLAN.md § 3
Assets/Settings/   URP assets from the template
docs/              assignment brief, plan, ledger, code-defense notes, reference image
```

## Project facts

- Unity **6000.6.3f1**, URP 17.6.0, Input System, AI Navigation, ProBuilder
- No imported art: ProBuilder primitives + URP Lit materials. Levels are ProBuilder meshes.
- Build targets: Linux x86_64 and Windows x86_64 (Windows module installed 2026-10-23, Mono backend)
