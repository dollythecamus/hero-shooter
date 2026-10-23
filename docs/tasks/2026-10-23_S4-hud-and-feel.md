# S4 — HUD, aiming and visual feedback (added 2026-10-23, runs with the ability rework)

Chloe added this as its own sprint step, alongside the ability rework (`2026-10-23_S3b-ability-rework.md`).
Two rules apply to everything here:

1. **UI is authored as prefabs, never built at runtime** (same rule as the character select screen).
2. Every gameplay state must be **visible without reading the console** — a demo has to show it.

## 1. HUD (authored prefab `Prefabs/UI/HUD.prefab`)

| Element | What it shows |
|---|---|
| Health | hero health bar + numeric value |
| Ability slot Q | ability name, cooldown as a radial/linear fill, key hint |
| Ability slot E | same, plus the **meter** for channelled abilities (Rock's Bulwark 5 s reservoir) |
| Weapon | charge indicator (Paper) and fire-rate window (Scissors) if room allows |
| Hit marker | short crosshair flash when the player's shot deals damage |

Requirements: no `Instantiate` at runtime for UI, all widgets authored, values pushed by a `HudController`
that reads `Health`, `PlayerAbilities`, `Weapon`, `PlayerStatus`.

## 2. Enemy health bars

- World-space bar above each enemy, **hidden at full health, visible below max**.
- One authored prefab (`Prefabs/UI/EnemyHealthBar.prefab`), pooled and parented to each enemy at spawn —
  no `Instantiate` per damage tick.
- Turns visibly distinct when the enemy is **marked** (Paper) or **snared**, so those two abilities get their
  feedback for free.

## 3. Aiming (RMB, all characters)

- FOV tightens, camera offsets slightly over the shoulder, aim crosshair replaces the hip crosshair.
- Paper gets a distinct **sniper reticle**.
- Implementation home: `ThirdPersonCameraRig` (FOV + offset + aim state) and the HUD (crosshair swap).
- Numbers (FOV, offset, smoothing) still open — see the questions in `docs/PROJECT_LEDGER.md`.

## 4. Ability visual feedback

| Ability | Feedback |
|---|---|
| Rock Slam | outward particle burst on impact; camera/proximity shake optional |
| Rock Bulwark | shield tint around the body, intensity falling with the meter; visible meter on the HUD |
| Paper Mark | marker above the marked enemy + HUD chip with remaining time |
| Paper Snare | root/bind effect on the snared enemy + HUD chip with remaining time |
| Scissors Climb | dust/particle while climbing, small puff on wall exit |
| Scissors Overclock | particles + trails + **coloured bullets** while active |

## 5. Bullet feedback

- Impact flash/spark on every hit (pooled, one prefab).
- **Paper's bullets carry a trail from muzzle to impact**, fading shortly after — shorter lifetime than the
  bullet itself, so the tracer reads as a line, not a wire.
- HUD hit marker on a landed player shot.

## Gates

- HUD shows live health, both abilities and their cooldown ratios while playing; no console needed.
- Enemy bar appears only when the enemy is damaged, above the right enemy, and never in front when off-screen.
- Aiming changes FOV/offset/crosshair visibly on all three heroes; Paper's reticle differs.
- Each ability produces its particles/marker at the moment it fires.
- Paper's tracer is visible from muzzle to impact.
- `grep -rn "Instantiate(" Assets/_Project/Scripts` still returns only `Core/ObjectPool.cs` — UI and VFX must
  be pooled or authored, not created per event.
