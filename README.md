# Hero Shooter

A third-person wave shooter built for **UNICAP — Level Design e Programação 3D** (individual project):
pick one of three characters, pick one of two maps, and fight five escalating waves of FSM-driven ranged
and melee enemies with pooled projectiles and two abilities per character.

Unity **6000.6.3f1**, URP, Input System, AI Navigation, ProBuilder. No imported art — every level and prop
is ProBuilder geometry with URP materials.

**Status:** complete and played. Every requirement below has been verified from a built binary by a human,
not only by automated probes. Linux and Windows builds are attached to the latest release.

## The assignment's requirements, and where each one lives

| # | Requirement | Implementation |
|---|---|---|
| 1 | Tutorial | `Scripts/Systems/TutorialDirector.cs`, `Scripts/UI/TutorialPanelUI.cs`, `Scenes/10_Tutorial.unity` |
| 2 | Third-person camera | `Scripts/Player/ThirdPersonCameraRig.cs` (+ `PlayerMotor.cs`) |
| 3 | Ranged enemy FSM | `Scripts/Enemies/EnemyBrain.cs` + `Enemies/States/*` + `RangedAttack.cs` |
| 4 | Melee enemy FSM | same brain, `MeleeAttack.cs` |
| 5 | Shooting without runtime `Instantiate` | `Scripts/Core/ObjectPool.cs`, `Weapons/ProjectilePool.cs` |
| 6 | Three selectable characters | `Scripts/Data/CharacterDefinition.cs` (ScriptableObjects) + `UI/CharacterSelectUI.cs` |
| 7 | Two abilities per character | `Scripts/Data/AbilityDefinition.cs` + `Player/Abilities/*` (six behaviours, three shapes) |
| 8 | Minimap | `Scripts/UI/MinimapCamera.cs` → RenderTexture → `Scripts/UI/MinimapBlips.cs` |
| 9 | Tab frag board | `Scripts/Systems/ScoreService.cs` + `Scripts/UI/FragBoardUI.cs` |
| 10 | Map select | `Scripts/Systems/GameFlow.cs` + `Scripts/Data/MapDefinition.cs` + `UI/MapSelectUI.cs` |
| 11 | Three self-chosen mechanics | wave director (`Systems/WaveDirector.cs`), damage numbers (`UI/DamageNumber*.cs`), pause menu (`UI/PauseMenu.cs`) |

The enemy FSM is one brain with five states — Search, Chase, Attack, Flee, CallAllies — and a per-archetype
attack strategy, so ranged and melee differ by data and one component, not by two brains. Every transition
is in one readable method, `EvaluateTransitions()`.

## Controls

| Input | Action |
|---|---|
| WASD / mouse | move / look |
| Shift | sprint |
| Space | jump |
| Left mouse | fire |
| Right mouse | aim (zooms; Paper scopes in further) |
| R | reload |
| Q / E | abilities — Rock: Ground Slam / Bulwark · Paper: Mark / Snare · Scissors: Overclock (wall climb is passive: hold Space at a wall) |
| Tab (hold) | frag board |
| Esc | pause menu (Resume / Main Menu / Quit) |

## Running it

Prebuilt: download `HeroShooter` from the repository's **Releases** page, unzip, and run
`HeroShooter.x86_64` (Linux; `chmod +x` first) or `HeroShooter.exe` (Windows). Keep the
`HeroShooter_Data` folder next to the executable.

From the editor: open the project with Unity 6000.6.3f1 and play `Scenes/00_Boot.unity` — it drives the
menu → character select → map select → match/tutorial flow.

Builds from the command line (scene list is read from Build Settings, so a scene added there cannot be
missed):

```
Unity -batchmode -nographics -projectPath . \
  -executeMethod HeroShooter.EditorTools.BuildScript.BuildLinux      # or BuildWindows
```

## Where the interesting code is

- `Scripts/Enemies/EnemyBrain.cs` — the whole decision table is `EvaluateTransitions()`, in priority order.
- `Scripts/Core/ObjectPool.cs` — the only `Instantiate` in the project; projectiles, enemies, VFX and
  damage numbers are all recycled through it.
- `Scripts/Weapons/Weapon.cs` — everything about a gun is data (`WeaponDefinition`), including magazine,
  pellets and charge; the shot leaves the camera-centre ray, so the crosshair is true at any range.
- `Scripts/Player/AbilityShapes.cs` — instant / channelled / passive cover all six abilities.
- `Scripts/UI/PauseMenu.cs` — one boolean owns the pause state, and the cursor is derived from it.
- `Scripts/Systems/WaveDirector.cs` — escalating waves, with one corpse lifecycle for every enemy.

## Repository layout

```
Assets/_Project/   gameplay: scenes, scripts, ScriptableObject data, prefabs, materials
Assets/Settings/   URP assets
docs/              brief, architecture, plan, per-system feature docs, code-defence notes
tools/             headless Unity launcher for this machine
```

## Documentation

- `ARCHITECTURE.md` — stack, components, conventions, directory map.
- `docs/ASSIGNMENT.md` — the brief as received (pt-BR) with an English translation.
- `docs/CODE_DEFENSE.md` — one block per system: what to open, how to frame it, what proves it.
- `docs/HERO_ROSTER.md` — the three heroes and their specified abilities.
- `docs/features/` — one document per system.
- `docs/PROJECT_LEDGER.md` — the build log: what was verified, when, and by what evidence.
