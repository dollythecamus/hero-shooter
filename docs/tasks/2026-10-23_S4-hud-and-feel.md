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

---

## VFX half — landed and measured 2026-10-24 10:2x–11:1x

Blocked for a while by the project's disabled **Particle System module** (`error CS1069` on `ParticleSystem`;
`Packages/manifest.json` had no `com.unity.modules.*` entries). Chloe approved adding
`"com.unity.modules.particlesystem": "1.0.0"`; with it the nine VFX scripts compile. Nothing else in the block
needed a manifest change.

**New code (`Assets/_Project/Scripts/VFX/`)**

- `VfxOneShot.cs` — one pooled burst (`IPooled`): plays its `ParticleSystem` on spawn, takes a per-burst colour
  (`SetColor`), returns itself to the pool after `lifetime` (0.7 s default).
- `VfxPool.cs` — `ObjectPool<VfxOneShot>` + static `Instance`; `VfxPool.Spawn(position, rotation, scale, color)`
  and `Spawn(position, scale, color)` do nothing when the scene has no pool, so VFX can never break gameplay.
- `BulwarkShieldVfx.cs` — authored shell renderer shown only while `BulwarkShield.IsRaised`, scaled
  `Lerp(60 %, 100 %, meter)` and alpha `Lerp(0.25, 1, meter)` through a `MaterialPropertyBlock` (`_BaseColor`).

**Hooks added**

- `Projectile`: `trail`, `body`, `overclockMaterial`, `impactColor`, `impactBurstScale` fields; `Launch(...)`
  and `ProjectilePool.Fire(...)` gained `Color trailColor` + `bool overclocked`; `ApplyVisuals` sets
  `trail.emitting/startColor` and swaps `body.sharedMaterial` per launch; `Impact` spawns a burst at the hit
  point on **every** hit, including walls.
- `Weapon`: `TracerColor` (pale blue, used when the weapon `RequiresCharge` → Paper) and `OverclockGreen`
  (overrides everything while overclocked).
- `PlayerStatus`: `StatusEffect.Overclock` marker + `HasEffect(kind)`; `Apply` ignores it (no multiplier).
- `Overclock`: applies the marker alongside FireRate/Speed.
- `SlamDriver.Impact`: burst at the landing point, scale = `radius × burstRadiusFraction` (0.45 → 2.25 for a
  5 m slam), orange.
- `WallClimbPassive`: dust every `dustInterval` (0.22 s) while climbing, plus a puff in `Stop()` and in
  `ExitJump()`.

**Authored assets (all done)**

1. `Art/VFX/T_Spark.png` — 64×64 radial alpha, generated procedurally, imported uncompressed with
   `alphaIsTransparency`.
2. `Art/Materials/M_Vfx.mat` — URP `Particles/Unlit`, transparent + additive blend, Base Map `T_Spark`.
3. `Art/Materials/M_Projectile_Overclock.mat` — URP Lit, green `_BaseColor` + emission.
4. `Art/Materials/M_Shield.mat` — URP `Particles/Unlit`, blue, `SrcAlpha`/`OneMinusSrcAlpha`.
5. `Prefabs/VFX/Blast.prefab` — `ParticleSystem` (0.4 s, one 12-particle sphere burst, gravity 0.6,
   colour-over-lifetime fade, size-over-life shrink, additive `M_Vfx`) + `VfxOneShot` (lifetime 0.7).
6. Scene object `Vfx` → `VfxPool`, `prefab` = Blast, `warmUpCount` 24.
7. `Prefabs/Projectiles/Projectile.prefab` — `TrailRenderer` (0.15 s, 0.06 wide, `M_Vfx`, emitting off) wired
   as `trail`, plus `body` (the `Sphere` child's `MeshRenderer`) and `overclockMaterial`.
8. `Player.prefab` — child `ShieldVfx` sphere at y 0.9, scale 1.6, `M_Shield`, `BulwarkShieldVfx`
   (`fullScale` 1.6), collider stripped.

**Measured in play mode (quoted probe lines)**

- Pool: `A0 vfx created=24 active=0` at start; `A8 DONE vfxCreated=24` after a shot, a slam, a Bulwark cycle
  and a climb — **the pool never grew**.
- Rock hip fire: `emitting=False trailCol=RGBA(0.00, 0.00, 0.00, 0.00) body='M_Projectile'`.
- Paper: `emitting=True trailCol=RGBA(0.60, 0.92, 1.00, 0.55)`.
- Impact: `B1 after 0.3s: bullet=no-active-bullet bursts=[scale=1.00 playing=True pos=(2.3, 0.0, -5.5)]
  created=24` — the bullet was consumed and exactly one burst appeared at the contact point; `B2` shows it
  finished (`playing=False`).
- Overclock: `B3 scissors overclock=True hasEffect=True` → `B4 overclock shot: emitting=True
  trailCol=RGBA(0.30, 1.00, 0.40, 0.65) body='M_Projectile_Overclock'`.
- Slam: `A3 slam start=True` → `A4 slam landing: vfx active=1 created=24 bursts=[scale=2.25 playing=True
  pos=(0.0, 0.2, -6.0)] slamActive=False` (2.25 = 5 m radius × 0.45).
- Bulwark shell: `A5 raised=True shellEnabled=False` (the shell's `Update` had not run yet in that frame) →
  `A6 after 1.4s: shellEnabled=True shellScale=1.43 meter=0.72` (1.43 = Lerp(0.96, 1.6, 0.72)) →
  `A7 released: shellEnabled=False`.
- Climb dust: **not measurable end-to-end** — the editor will not hold a key across frames (`InputState.Change`
  is overwritten by the real device each input update; queued `KeyboardState` events read back as not pressed),
  so the two call sites were invoked directly: `after Stop()=1`, `after Climb() step=2`, `created=24`.
- Static gate: `grep -rn "Instantiate(" Assets/_Project/Scripts` → still one hit (`Core/ObjectPool.cs:43`).

