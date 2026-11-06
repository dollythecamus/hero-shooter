# Code defense — what to open, what to say

Half the grade is this walkthrough (20:20–22:00). One block per system: **the file to open**, **the one
sentence that frames it**, **the three points that carry it**, and **the measured line** that proves it.
Numbers in *Evidence* were produced by play-mode probes on this machine, not inferred.

**Updated 2026-11-06** after three human passes on real builds. Where a block now cites a gate, that is
Chloe playing the built game, not a probe — the two kinds of evidence are labelled apart on purpose, because
"it measured right" and "it plays right" failed differently twice today.

Open files in this order — it follows a bullet's life: input → hero → weapon → projectile → damage →
enemy → HUD. FSM and abilities are the two "design" blocks; skip between them if the examiner leads.

---

## 0. The 60-second architecture frame

`ARCHITECTURE.md` (root) and `Assets/_Project/Scripts/`.

- **One responsibility per file.** `Health` holds a number and raises events; it does not know what death
  means. Whoever needs to react subscribes — that is why the frag board, the wave director and the bars all
  read the *same* `Health.Died` without any of them touching damage code.
- **Data over hardcode.** Characters, abilities, maps and enemy stats are ScriptableObjects under
  `Assets/_Project/Data/`. Balance is an inspector edit, no recompile.
- **One `EnemyBrain`, five states, two bodies.** Ranged and melee differ by an attack strategy plus a data
  asset, not by two brains.
- **Nothing allocates in a firefight.** Projectiles, VFX, enemy bodies and damage numbers all come out of
  warm-up pools (`Core/ObjectPool.cs`). `Instantiate` appears in exactly one file — provable live:
  `grep -rn "Instantiate(" Assets/_Project/Scripts` → `Core/ObjectPool.cs:43` only.

---

## 1. Tutorial — `Scripts/Systems/TutorialDirector.cs` + `Scripts/UI/TutorialPanelUI.cs`

**Frame:** a state machine over *gameplay events*, not a timer, so the tutorial cannot advance while the
player is still reading.

- Five gated steps: Move → Look → Shoot → Ability → Kill → done. Each gate reads the same systems the game
  uses (`PlayerMotor.PlanarSpeed`, `Weapon` shots fired, `PlayerAbilities` activations, `Health.Died`).
- The scene is `Scenes/10_Tutorial.unity`; the panel is the authored prefab `Prefabs/UI/TutorialPanel.prefab`.
- **Design choice worth saying out loud:** the tutorial has no enemies. The kill step uses three static
  targets carrying the same `Health` component, so a stray enemy cannot kill the player mid-explanation.
- The onboarding text is **player-facing**: it says "shoot a dummy until it drops", never "the Health
  component". The implementation vocabulary belongs in this document, which is the answer if asked why the
  tutorial does not mention pooling.
- Completion **freezes the clock and hands the cursor back** (`Time.timeScale = 0`, cursor unlocked) so the
  panel's button is clickable — a locked cursor made it unreachable until the 2026-11-06 gate. The exit
  routes through `SceneExit.ReturnToMenu`, which uses `GameFlow` when it exists and loads `00_Boot` directly
  when it does not (the tutorial played straight from the editor has no `GameFlow` instance — that null is
  why the button "did nothing").

*Evidence:* five gates complete → `COMPLETED=True panelTitle='TRAINING COMPLETE'`, `timeScale=0 cursor=None`;
*2026-11-06 human gate:* clicking the panel's BackButton loads `00_Boot` (raycast hit `BackButton` →
scene `00_Boot`), and the same run proves the pause menu's Main Menu entry.

## 2. Third-person camera — `Scripts/Player/ThirdPersonCameraRig.cs` (+ `PlayerMotor.cs`, `InputReader.cs`)

**Frame:** the camera is a boom with collision, and input is read in exactly one place.

- `InputReader` is a static facade over the Input System: gameplay never touches `Keyboard`/`Mouse`, so a
  rebind or a controller is one file.
- The rig orbits a pivot above the hero, pulls the boom in when level geometry is between camera and hero
  (`collisionMask` = Structure layer = 8), and lerps with an exponential smoothing that is frame-rate
  independent (`1 - Exp(-sharpness * dt)`).
- RMB aiming: FOV 60 → 42 (per-character override: Paper zooms to **30**), +0.55 right / −0.20 down offset,
  movement ×0.9 (×0.6 for Paper), weapon spread halved. Paper swaps in a sniper reticle (gap cross + mildots).
- **While aiming the body locks to camera yaw** (`PlayerMotor` faces `camera.forward`, legs strafe). Before
  that, the body turned into the movement direction, which swung the hero across the fixed shoulder offset
  and read as "the camera changed sides" — the 2026-11-06 gate.
- **A frozen clock freezes the camera** (`LateUpdate` returns when `timeScale <= 0`). Mouse look is read in
  raw pixels, not scaled by `deltaTime`, so pausing alone never stopped the orbit.
- **The pause menu writes straight into this rig** (`MouseSensitivity`, `InvertY`, `AimFov`) — one number,
  no copy.

*Evidence:* `fov=60.0 → 45.7 → 42.2 → 42.0`, release `59.9`; Paper `hip=False aim=False sniper=True`
(2026-10-23 probe). *2026-11-06 gate, injected mouse delta:* 300 px moved yaw 0 → **36.00** while playing
and **0.00** while paused, which is the same delta proving both halves.

## 3–4. Enemy FSM, ranged and melee — `Scripts/Enemies/EnemyBrain.cs`, `Enemies/FSM/StateMachine.cs`, `Enemies/States/*.cs`

**Frame:** a shared brain decides, a per-archetype strategy acts. That sentence is the whole design.

- Five states — Search, Chase, Attack, Flee, CallAllies — registered in `RegisterStates()`. **Every
  transition lives in one method**, `EvaluateTransitions()`, in priority order: no target → Search; hurt
  below `fleeHealthPercent` → Flee; in range → Attack; in sight → Chase; ally call heard → CallAllies.
- Ranged vs melee: `RangedAttack.cs` (line of sight, fire rate, backs off when too close) vs
  `MeleeAttack.cs` (wind-up telegraph, lunge, recover), chosen by the `IEnemyAttack` component and tuned by
  `EnemyStats` (a ScriptableObject). Same brain, two bodies.
- "Calls friends" is `AllyCall.Broadcasted` → `CallAlliesState`, and the ally converges on **the caller's
  target**, not its own — that was a real defect found by a probe and fixed.
- Death is honest: `OnDied` freezes the FSM and switches the agent off, and says out loud that recycling
  the body belongs to whoever spawned it.

*Evidence:* `IMPACT … enemyHp=50 enemyMoved=2.12 knocked=True`; `D5 broadcast: state=CallAllies
callTarget=Player … dPlayer 12.00 → 5.63` while `dOwn` grows (2026-10-23).

## 5. Pooled shooting — `Scripts/Core/ObjectPool.cs`, `Weapons/ProjectilePool.cs`, `Weapons/Weapon.cs`, `Weapons/Projectile.cs`

**Frame:** the brief asks for no `Instantiate` at runtime; here `Instantiate` exists once, during warm-up.

- `ObjectPool<T>` builds N instances in `Awake`, then only enables/disables. `Get(position, rotation)` +
  `OnSpawned()`, `Release()` + `OnDespawned()` — the pooled object owns its own reset (`IPooled`).
- `ProjectilePool` is a thin subclass so the inspector can hold a concrete component; the projectile
  releases itself on hit or lifetime, so no caller has to remember to.
- `Weapon` owns rate of fire, spread and the aim solve; `PlayerAbilities` is ordered **before** it
  (`[DefaultExecutionOrder(-100)]`) so a shield raised on the same frame as a shot cannot leak a bullet
  through — a defect the probes caught.
- **The round leaves the camera axis, not the barrel.** `Weapon.ResolveAim` starts the projectile on the
  camera-centre ray at the hero's own depth: the reticle and the bullet are the same line at every range.
  The first version aimed *from the muzzle at a point on that ray*, which is why the crosshair lied at close
  range. Because the round now starts inside the hero, `Projectile` sweeps with a non-alloc
  `SphereCastNonAlloc` and skips its owner's colliders — that skip is what stops point-blank melee enemies
  being missed.
- **Ammunition is data, not code:** `WeaponDefinition.magazineSize` and `reloadTime`; `Weapon` owns the
  count, reloads on a deadline (`Time.time >= _reloadEnd`, so a paused game freezes a reload), auto-reloads
  when empty, and `R` refills whenever the magazine is not full. There is **no reserve** — a run cannot end
  with the player empty-handed. `pelletPerShot` + `pelletSpreadDegrees` make Rock's eight-pellet shotgun a
  data row, not a second weapon class.
- The HUD reads two ratios off the weapon: `ShotReadyRatio` (fire-rate bar filling to the next shot) and
  `ReloadRatio` (the same bar turns amber and tracks the reload).

*Evidence:* pool `created=64` before and after 61 shots, `active=0`; `rangedCreated=6 rangedActive=1` after wave 1.
*2026-11-06:* Rock `SetDefinition` → `mag=6`, reload refused at a full magazine; both player builds compiled
and the shotgun, ammo readout and fire-rate bar all passed a human gate.

## 6. Three characters — `Scripts/Data/CharacterDefinition.cs`, `Scripts/Player/HeroLoadout.cs`, `UI/CharacterSelectUI.cs`

**Frame:** choosing a character is one `Apply(CharacterDefinition)` call — there is no per-character script.

- `CharacterDefinition` holds identity, body scale/colour, health, speed, aim multiplier, weapon and the two
  ability assets. `HeroLoadout.Apply` fans that out to health, motor, weapon, abilities and appearance.
- Appearance is a `MaterialPropertyBlock` write, so a colour change does not instance a material.
- The pick travels between scenes in `GameFlow` (a singleton), so the select screen can live in `00_Boot`
  while the match lives in a map scene.
- **UI is authored, not built:** `Prefabs/UI/CharacterSelect.prefab` is a real prefab; the scripts only
  bind and toggle.

*Evidence:* `cards=3 card0Name=ROCK`; after picking Paper `heroLabel='FIGHTING AS PAPER'
selectedCharacter=Paper`; the map loads with `maxHealth=90` (Paper's stat), proving the carry-through.

## 7. Two abilities per character — `Scripts/Player/PlayerAbilities.cs`, `Player/AbilityShapes.cs`, `Player/Abilities/*.cs`

**Frame:** five shapes cover six abilities, and cooldowns are owned in one place.

- Shapes: **instant** (Slam, Mark, Snare, Overclock), **channelled** (Bulwark), **passive** (Wall Climb).
  `AbilityDefinition.kind` decides; `AbilityFactory` builds the instance from the asset.
- `PlayerAbilities` owns slots, cooldowns, the Bulwark meter and the HUD-facing ratios —
  `CooldownRatio` is 0 = just used, 1 = ready (documented in the XML comment, and the HUD trusts it).
- **One-mover rule:** only one component may call `CharacterController.Move` in a frame. Slam and the climb
  suspend `PlayerMotor` while they drive the controller; that rule is why the exit jump carries a momentum
  flag instead of fighting ground acceleration.
- Effects are multipliers with deadlines (`Health.ApplyDamageMultiplier`, `EnemyBrain.ApplySpeedMultiplier`)
  so a second application refreshes instead of restoring early.
- **Effects are visible on the enemy, not only in the corner chip.** `VFX/EnemyStatusRings` draws, per bound
  enemy: a white ring on the target a ready Mark *would* take (only while aiming and off cooldown, using the
  same line-of-sight rule as the cast, so the preview cannot promise a target the cast refuses), amber while
  marked, cyan plus a tether while snared. Rings are `LineRenderer` circles built in code — no imported art,
  nothing per-map. `EnemyHealthBar` also stops hiding a **marked** enemy at full health, which was why the
  mark was invisible until the first hit.

*Evidence:* `apexAboveTakeOff=3.96`; Bulwark `raised=True meter=4.94 dmgMult=0.40`, `hp 180 → 176 (10 raw)`,
`depleted after 5.34 s … +8.6 s canRaise=True`; climb `0.08 → 2.19 m in 0.7 s`, exit jump `outward=4.90`;
Mark `dmgMult=1.35`, Snare `speedMult=0.25`. *2026-11-06 gate:* all three heroes' abilities played and passed,
but the mark/snare *readability* fix landed after that pass — it is the one thing here no human has seen yet.

## 8. Minimap — `Scripts/UI/MinimapCamera.cs`, `Assets/_Project/Data/Minimap/MinimapRT.renderTexture`, `Prefabs/UI/HUD.prefab`

**Frame:** a second camera renders the world from above into a texture; the HUD shows that texture.

- `MinimapCamera` rides the **hero prefab**, so every map gets a minimap with no per-scene wiring. It writes
  world position and world rotation in `LateUpdate` — the parent's facing can never tip the map.
- Culling mask is `201` = Default + Structure + Player + Enemy: the UI layer is deliberately out, so
  world-space enemy bars never bleed into the map.
- 256×256 `RenderTexture` asset → `RawImage` inside the authored `MinimapFrame` (top-right, 232 px, 6 px
  inset). `HudController.PushMinimapArrow` turns only the arrow from the hero's yaw.
- **Enemies appear as dots** (`UI/MinimapBlips`): the map camera is axis-aligned and orthographic, so a blip
  is a plain world offset ÷ `MinimapCamera.HalfExtent` — no second projection that could drift from the
  texture underneath. The dot list comes from `EnemyBarBinder.Enemies`, the same list the health bars use, so
  a wave-spawned body cannot show a bar but no dot. Off-map enemies pin to the rim.

*Evidence:* `camera=True ortho=True size=26 mask=201 rt=MinimapRT`; live `cam pos=(5.00, 29.60, -7.00)
rot=(90.00, 0.00, 0.00)` with the hero yawed 90° — i.e. exactly 30 m up, bolt upright. *2026-11-06 gate:* the
minimap (dot layer included) passed twice.

## 9. Tab frag board — `Scripts/Systems/ScoreService.cs`, `Scripts/UI/FragBoardUI.cs`, `InputReader.FragsHeld`

**Frame:** the scoreboard subscribes to `Health.Died`; nothing in the damage or enemy code knows it exists.

- `ScoreService` registers enemies (discovered at Start, and **bound explicitly by the wave director** for
  bodies it pulls from a pool) and recognises the hero by layer, not by name. Kills, deaths and
  "enemies alive" are three properties.
- `FragBoardUI` only displays: Tab (`InputReader.FragsHeld`) switches the **authored** panel on, fills name /
  kills / deaths columns and the enemies-alive footer.
- Layer constants are documented in the file (`HeroLayer = 6`, `EnemyLayer = 7`) because they are the
  contract with `docs/LAYERS.md`.

*Evidence:* `score=True alive=4 kills=0` at start; after one kill `alive=3 kills=1`; with Tab held the panel
reads `ROCK / 1 / 0 / ENEMIES ALIVE 3`.

## 10. Map select — `Scripts/Systems/GameFlow.cs`, `Scripts/Data/MapDefinition.cs`, `UI/MapSelectUI.cs`

**Frame:** two `MapDefinition` assets, two ProBuilder scenes, one router.

- `GameFlow` is the only `SceneManager.LoadScene` caller (3 call sites, all in that file) and carries the
  chosen hero and map across the load. `Systems/SceneExit` is the one *other* way out: it prefers `GameFlow`
  and falls back to loading `00_Boot`, so a menu or tutorial exit still works when the scene was opened
  without the boot flow (editor tests, or a build started on a map).
- `MapDefinition` names the scene; `MapSelectUI` builds cards from the asset list, so a third map is data.
- The maps are ProBuilder geometry (`20_Map_Arena`, `21_Map_Alley`) with a `NavMeshSurface` bake each —
  that is the level-design half of the grade, and they are the two scenes the enemies path on.
- **Baking is the trap worth explaining.** The scenes had drifted into carrying a *legacy* navmesh (baked
  before the current layout) alongside the surface's data, so enemies walked through cover while the
  `NavMeshSurface` looked healthy. A plain rebake changed nothing — the stale dataset stayed loaded. The fix
  is `UnityEditor.AI.NavMeshBuilder.ClearAllNavMeshes()` first, then `surface.BuildNavMesh()`, then save and
  reopen to confirm. Verified by sampling the navmesh *inside* every obstacle's footprint: Arena 13/23 → 4
  (all four are hollow shells with unreachable interior islands), Alley 12/17 → 0, Tutorial 6/10 → 0 after
  adding the surface it never had.

*Evidence:* `mapCards=2 names=ARENA|ALLEY`; `E scene=20_Map_Arena selectedMap=ARENA hero=Paper … navmesh
agents=4 onNavMesh=4`; Alley `agentsOnNavMesh=4/4 stair/ramp present=True doorway=True balcony=True`.
*2026-11-06 gate:* both maps played and passed after the rebake.

---

# The three self-chosen mechanics (requirement 11)

## A. Wave director — `Scripts/Systems/WaveDirector.cs`, `Scripts/Enemies/EnemyPool.cs`

- Waves 1–5, size 3 → 11, a breather between them, spawns placed on the NavMesh in a ring around the hero
  (`NavMesh.SamplePosition`) so no spawn points had to be authored per map.
- **Enemies come out of a pool too** (`EnemyPool : ObjectPool<EnemyBrain>`; `EnemyBrain` implements
  `IPooled`), which keeps the "`Instantiate` in one file" claim true even with a spawner.
- It reuses `ScoreService` and `Health.Died` for kills, and respawns the hero 2 s after death — a wave game
  with a permanent corpse is not playable.

*Evidence:* wave 1 → `alive=3 scoreAlive=7`; pools `rangedCreated=6 rangedActive=1 meleeCreated=6
meleeActive=2` (warm-up, not instantiation); after a spawned enemy died `alive=2 kills=1`; hero death →
`deaths=1 hp=200 pos=(0.00, 0.08, -22.00) movedFromDeathSpot=0.00`.

## C. Damage numbers — `Scripts/UI/DamageNumber.cs`, `Scripts/UI/DamageNumberPool.cs`, `Scripts/Core/Health.cs`

- A pooled floating number at the hit point: world point → canvas space every frame, rises and fades over
  0.85 s. Heavy hits (≥35) are amber, so a Mark-boosted shot is visible at a glance.
- `Health.TakeDamage` now raises `Damaged`/`Died` with **what actually landed** (after Mark's ×1.35), so the
  number cannot contradict the health bar next to it.
- Spawning is driven by the same `EnemyBarBinder.EnemyDamaged` event the hit marker uses — one hook, two
  readouts.

*Evidence:* after a hit, `damageNumbersActive=1 damagePoolChildren=16` (pooled, reused).

## E. Pause menu — `Scripts/UI/PauseMenu.cs` (+ `ThirdPersonCameraRig.MouseSensitivity` / `InvertY` / `AimFov`)

- Esc freezes the match with `Time.timeScale = 0`, unlocks the cursor and shows the authored panel; Esc or
  Resume restores it. A frozen clock already stops the FSM, the wave loop and the weapon — no system needs a
  pause flag.
- **The pause state is one boolean and the cursor is derived from it** (`SetPaused` → `EnforceCursor` every
  frame), with a 0.15 s toggle cooldown. The first version set the cursor inside `Pause()`/`Resume()` only,
  and the gate found the mouse free after resuming. It claims the cursor only while the clock is running, so
  any other system that freezes the game (the tutorial's completion panel) can have a clickable pointer.
- Three buttons: Resume, **Main Menu** (`SceneExit.ReturnToMenu`, works with or without `GameFlow`) and Quit.
  The Main Menu entry is the guaranteed way out of any scene — added after the tutorial's own exit turned out
  to be a dead end.
- Sensitivity slider and invert-Y write into the camera rig directly (no duplicated setting), quit calls
  `Application.Quit` (and leaves play mode in the editor). Resume restores the clock it found, so pausing on
  top of another freeze does not restart the game underneath.

*Evidence:* `paused timeScale=0 panel=True` → `resumed timeScale=1 panel=False`. *2026-11-06 gate:* two full
Esc cycles with injected key events — open → `timeScale 0`, `cursor None`; close → `timeScale 1`,
`cursor Locked`; a 300 px mouse delta moved the camera 36.00° playing and 0.00° paused; clicking Main Menu
loaded `00_Boot`.

---

# Builds — `Assets/_Project/Editor/BuildScript.cs`

- The scene list is read from **Build Settings**, not repeated in code, so a scene added there cannot be
  missing from a build.
- Linux and Windows x86_64 from the same commit, into `/home/chloe/builds/hero-shooter/{linux,windows}`.
- Command (headless, this machine needs `steam-run` plus 64-bit `libtinfo`/`libstdc++` for the shader
  compiler child):

```
steam-run tools/unity-run.sh -batchmode -nographics -projectPath . \
  -executeMethod HeroShooter.EditorTools.BuildScript.BuildLinux
```

*Evidence:* `[Build] result=Succeeded size=102.8MB time=00:04:09 errors=0` (Linux), `result=Succeeded
size=102.3MB time=00:03:43 errors=0` (Windows). *2026-11-06, in-editor:* Linux **102.97 MB / 21.7 s** and
Windows **102.49 MB / 19.8 s**, `errors: 0` on both — the same commit produced both targets.

---

# Questions that come with this codebase

**"Why is the enemy count in two places?"** No — `WaveDirector` owns how many are alive in this wave;
`ScoreService` owns kills/deaths/enemies for scoring. Both read the same `Health.Died`; neither computes the
other's number.

**"What happens if two things want to move the player?"** The one-mover rule: `PlayerMotor` is the mover;
Slam and the climb suspend it explicitly while they drive the controller.

**"Where would a third map or a fourth hero go?"** `Data/Maps/` or `Data/Characters/` — new assets, no code.

---

# Honest gaps (say them before they are found)

- **Three human passes happened on 2026-11-06** (Rock, Paper, Scissors; Arena and Alley; the tutorial), and
  each one found things no probe had. The *last* round of fixes — the mark/snare rings, the tutorial's exit,
  the borderless bar sprites, ammo without a reserve, the lower fire rates — was built and checked by probe,
  **not yet played**. Say that plainly rather than claiming a clean final pass.
- Damage numbers and the wave HUD line have never been reviewed on screen; the probes only counted them.
- Tab and held keys cannot be held through an injected keyboard in a headless probe (the editor overwrites
  injected key state each input update). Esc is now proven end-to-end with injected `InputSystem` events
  (2026-11-06); Tab is proven by its handler plus a human hold.
- No automated tests; `com.unity.test-framework` is installed but unused and must not be presented as a gate.
- Four `Cover_*` blocks in the Arena are hollow shells: the bake leaves an unwalkable-around island of floor
  inside them. Unreachable, so harmless, but do not claim "every obstacle is carved" without that caveat.
- Enemies are not networked, there is no save, and balance is untouched beyond what the gates measured.
