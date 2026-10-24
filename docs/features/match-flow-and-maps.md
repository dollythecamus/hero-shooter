# Match flow, maps & tutorial

## Purpose

Everything that happens **between** matches: which hero and which map the player picked, how a scene is loaded,
and the scripted tutorial. The match itself (movement, weapons, enemies, HUD) is unchanged — this feature only
wraps it. `GameFlow` is the one object that survives a scene load and the only code that calls
`SceneManager.LoadScene`; every screen is an authored prefab controller that fills widgets and raises a choice.

## Interface

```csharp
class GameFlow : MonoBehaviour {          // DontDestroyOnLoad singleton, the only object in 00_Boot that survives
    static GameFlow Instance { get; }
    CharacterDefinition[] Roster { get; }
    MapDefinition[] Maps { get; }
    CharacterDefinition SelectedCharacter { get; }
    MapDefinition SelectedMap { get; }
    void SetCharacter(CharacterDefinition character);
    void SetMap(MapDefinition map);
    bool StartMatch();                    // false unless both picks exist and SelectedMap.IsLoadable
    bool StartTutorial();                 // SelectedCharacter defaults to Roster[0]
    void ReturnToMenu();                  // unlocks the cursor, loads bootSceneName
    void Quit();                          // Application.Quit; stops play mode under UNITY_EDITOR
}

class MapDefinition : ScriptableObject {  // menu: Hero Shooter/Map Definition
    string displayName, blurb, sizeLabel;
    string sceneName;                     // must equal a Build Settings entry name — this is what LoadScene gets
    Color accentColor;                    // card tint
    bool IsLoadable => !string.IsNullOrEmpty(sceneName);
}

interface ICharacterPicker { void PickCharacter(int index); }
// Implemented by MatchBootstrap (select screen inside a match scene) and MainMenuUI (select screen in 00_Boot),
// so CharacterSelectUI/CharacterCardUI never know which flow they are part of.

class MainMenuUI : MonoBehaviour, ICharacterPicker {   // controller for Prefabs/UI/MainMenu.prefab
    void Play();                          // menu canvas off → CharacterSelect.Show(roster, this)
    void PickCharacter(int index);        // GameFlow.SetCharacter → select hidden → MapSelect.Show(maps, flow, hero)
    void Tutorial(); void Quit();
    void RefreshFlow();                   // flow = GameFlow.Instance when one exists — see Behavior notes
}

class MapSelectUI : MonoBehaviour {       // controller for Prefabs/UI/MapSelect.prefab
    bool IsVisible { get; }
    void Show(MapDefinition[] maps, GameFlow flow, CharacterDefinition chosenHero);  // fills N cards + "FIGHTING AS X"
    void Hide();
    void Select(int index);               // Hide → flow.SetMap → flow.StartMatch(); warns if the map has no scene
}
class MapCardUI : MonoBehaviour { void Click(); void Bind(MapDefinition map, int index, MapSelectUI owner); }
// Same authored-prefab pattern as CharacterCardUI: the button is a persistent listener, the card knows its index.

class TutorialDirector : MonoBehaviour {
    enum Gate { Move, Look, Shoot, Ability, Kill }
    [Serializable] class Step { Gate gate; string title; string body; float threshold; }
    int StepIndex { get; } int StepCount { get; } bool Completed { get; }
    float StepProgress { get; }           // 0..1 for the current step, probe-readable
    Gate CurrentGate { get; }
    void ReturnNow();                     // same path the auto-return takes (returnDelay seconds after completion)
}
class TutorialPanelUI : MonoBehaviour { Show(); Hide(); SetStep(i, total, title, body); SetComplete(title, body); ReturnNow(); }
```

## Scenes

| Scene | Build index | Holds |
|---|---|---|
| `00_Boot` | 0 | camera + light, `EventSystem`, `GameFlow` (roster 3, maps 2), `MainMenu` / `CharacterSelect` / `MapSelect` canvases (last two start inactive) |
| `10_Tutorial` | 1 | arena rig + 30×30 course, `Tutorial` object (`TutorialDirector`), `TutorialPanel` instance, 3 static targets, **no enemies** |
| `20_Map_Arena` | 2 | 60×60 courtyard: raised 16×16 centre, two ramps, one ProBuilder stair, 4 corner cover blocks, 2 low walls, 4 pillars, 5 m perimeter; 2 ranged + 2 melee |
| `21_Map_Alley` | 3 | 46×34 with a divider wall and a ProBuilder doorway as the only ground route between halves, a balcony at 4.5 m reached by a ramp; 2 ranged on the balcony + 2 melee behind the doorway |
| `90_TestArena` | 4 | the dev scene, unchanged — it still runs the select screen inside the match (`MatchBootstrap.BeginSelection`) |

Map scenes are **copies of `90_TestArena`'s rig**, not instances of a rig prefab: `HudController → HeroLoadout`,
`Match → HeroLoadout`/camera rig/`SpawnPoint` and the HUD's own references are same-scene references, and a rig
prefab would have to re-own all of them. Cost: a rig change must be applied to three scenes. `90_TestArena` is
not wired to `GameFlow` at all, so it keeps working as a standalone dev scene.

NavMesh: the map scenes carry a `NavMeshSurface` (`collectObjects = All`, `useGeometry = PhysicsColliders`,
`layerMask = everything`) and bake into `<Scene>/NavMesh.asset`; `90_TestArena` keeps its original legacy bake.
Measured after the S5 bake: Arena 295 triangles over 61×61 m, Alley 225 triangles, and 4/4 agents on the mesh
in both.

## Behavior notes

- **`MatchBootstrap.Start` has three paths**, in this order: `GameFlow.SelectedCharacter` present → apply it
  (a map scene, arriving from the menu); no `selectUI` and a roster → apply `roster[0]` (a map scene opened
  directly in the editor); otherwise → `BeginSelection()` (the dev scene's modal pick). `Select(int)` and
  `PickCharacter(int)` both end in the same private `ApplySelection`.
- **The boot scene's `GameFlow` destroys itself** when an instance survived the scene load (`Awake`), so after
  returning from a map `MainMenuUI`'s serialized `flow` reference is a *destroyed* object and every guarded call
  would silently no-op. `MainMenuUI.RefreshFlow()` (called from `Awake` and `Start`) prefers `GameFlow.Instance`.
  Found by the gate probe, not by reading the code: the first `Tutorial()` click after a map did nothing.
- **The tutorial's Move gate counts travel only while `PlayerMotor.PlanarSpeed > 0.3`**, not raw position delta.
  `MatchBootstrap` teleports the hero to the spawn point on load, and a raw delta finished step 1 before the
  player touched a key — a false pass the probe caught (`step=1/5` at the first frame sampled).
- Gates and their sources: Move → accumulated planar travel while moving (`_moved ≥ threshold`), Look →
  accumulated `Mathf.DeltaAngle` of the camera yaw (`≥ threshold`), Shoot → `Weapon.ShotsFired` above the value
  recorded when the step opened, Ability → `PlayerAbilities.Activated` (added by this block), Kill → `Health.Died`
  on one of the three targets wired into the director. Steps run strictly in order; a later gate cannot complete
  an earlier one.
- The tutorial has **no enemies**: a tutorial that can kill the player mid-explanation is a demo hazard. The Kill
  step uses the three static targets, which carry the same `Health` component as every enemy.
- `TutorialPanelUI.ReturnNow` exists because the panel is a prefab and the director is a scene object: a button's
  persistent listener can only target something in the prefab, so the panel forwards to the scene-wired director.
- `MapSelectUI.Select` hides the canvas **before** loading, and `GameFlow.StartMatch` refuses a map with an empty
  `sceneName` (`Debug.LogWarning`), so a half-authored map asset cannot strand the player on a blank screen.
- `GameFlow.Quit` stops play mode under `UNITY_EDITOR` instead of doing nothing, so the button is demonstrable in
  the editor.
- Not here, deliberately: no pause menu, no score, no wave spawning, no restart-after-death. Those are S6.

## Invariants (re-runnable)

```bash
grep -rn "SceneManager.LoadScene" Assets/_Project/Scripts   # 3 call sites, all in Systems/GameFlow.cs
grep -rn "Instantiate(" Assets/_Project/Scripts            # one hit: Core/ObjectPool.cs:43
grep -rn "PickCharacter" Assets/_Project/Scripts           # interface + 2 implementers + 1 caller (CharacterCardUI)
```

Build order is asserted by `ProjectSettings/EditorBuildSettings.asset`: `00_Boot` must stay index 0 or a fresh
launch starts in a map with no hero.
