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

---

## Landed 2026-10-23 ~21:30–22:55 — aiming, HUD and enemy bars (VFX not started)

**Built.** Aim state on `ThirdPersonCameraRig` (`aimFov 42`, `aimBlendPerSecond 8`, `aimOffset +0.55/−0.20`,
`IsAiming`/`AimBlend`, hip FOV read from the scene camera at `Awake`); `CharacterDefinition.aimMoveMultiplier`
(0.9 everywhere, `Hero_Paper` = 0.6) read by `PlayerMotor`; `CharacterDefinition.sniperReticle` (true on Paper);
`Weapon` halves `spreadDegrees` while RMB is held (one `const`, no per-weapon number). Authored
`Prefabs/UI/HUD.prefab` (health bar + number, Q/E slots with cooldown fill / Bulwark meter / **passive** label,
hip + aim + sniper crosshairs, hit marker, MARK/SNARE chips, Paper's charge bar) driven by `HudController`;
`Prefabs/UI/EnemyHealthBar.prefab` + `EnemyHealthBarPool` + `EnemyBarBinder` (one pooled world-space bar per
enemy, bound at `Start`). Mark and Snare now write a **deadline** (`Health.ApplyDamageMultiplier`,
`EnemyBrain.ApplySpeedMultiplier`) instead of a coroutine restore, which is what gives the chips a countdown and
fixes the non-stack-safe restore the audit flagged.

**Measured in play mode, MCP bridge, 22:2x–22:5x** (probe lines quoted, not inferred):

| Gate | Observed |
|---|---|
| Aiming FOV | `fov=60.0` → `fov@0.2s=45.7` → `fov@0.6s=42.2` → `fov@1.2s=42.0`, release → `59.9` |
| Crosshair swap | aiming, Paper: `hip=False aim=False sniper=True`; released: `hip=True aim=False sniper=False` |
| HUD, Rock | `health='200 / 200' Q='Rock Slam' key='Q' cdRootQ=True E='Rock Bulwark' key='E' meterRootE=True meterFillE=1.00`; shield raised → `meterE=0.74` |
| HUD, Paper | `Q='Paper Mark'/'Q' E='Paper Snare'/'E' chargeRoot=True meterRootE=False passiveQ=False` |
| HUD, Scissors | `Q='Scissors Climb' key='passive' passiveQ=True cdRootQ=False chargeRoot=False` |
| Bar at full | `vis=False` (also `vis=True fill=1.00` for an untouched enemy) |
| Bar when hurt | `vis=True fill=0.60 rgb=(0.85,0.20,0.20)` |
| Bar marked | `rgb=(1.00,0.72,0.20) st='MARK 5.5'` → `st='MARK 4.6'` |
| Bar snared | `rgb=(0.55,0.66,0.78) st='SNARE 2.4'` |
| Never through walls | wall cube between camera and bar → `vis=False`; cube removed → `vis=True` |
| Mark / Snare | `markOk=True dmgMult=1.35 left=6.0`; `SNARE ok=True speedMult=0.25`, chip `'SNARE 2.5s'` → `'SNARE 1.5s'`, `chips=False/True` |
| Hit marker | `hitMarkerAlpha=1.00` after a hit whose `DamageInfo.Source` is the hero (window patched to 1.5 s for observability; authored 0.12 s) |
| Static gates | `Instantiate(` → one hit (`Core/ObjectPool.cs:43`); `attackRange` → declaration only; `Assembly-CSharp.dll` 21:34 with no `error CS` after line 18251 |

**Two defects the pass found and fixed.** (1) The wiring save captured a stale `m_IsActive: 0` override on the
`CharacterSelect` prefab instance — inherited play-mode churn from the previous session's in-memory scene — so
the select screen would not have appeared; restored to `m_IsActive: 1` and re-saved. (2) The probes wrote
`ProjectSettings/TimeManager.asset` (`Time.maximumDeltaTime`), which Unity 6 re-serialized in the new
fixed-point format; reverted with `git checkout --`.

**Not built in this pass** (still open): all of § 4 and § 5 — impact sparks, Paper's tracer, green Overclock
bullets, per-ability particles, camera shake. Also not built: the Scissors fire-rate-window readout (only
Paper's charge bar is drawn), and `DamageLog` is still in the scene (the S1 console probe, to be deleted once
VFX replaces it — deleting it needs sign-off because it is referenced from enemy prefabs).

**Loose end for the docs pass:** `git diff --check` now reports 9 trailing-space lines, all Unity serializer
output in `90_TestArena.unity` (`m_Name: ` and `value: ` for empty strings) — same class as the note already
recorded for `Player.prefab`; not hand-editable while the editor holds the project.

