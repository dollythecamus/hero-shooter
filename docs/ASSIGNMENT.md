# Hero Shooter — assignment brief

Source: UNICAP LMS, discipline **Level Design e Programação 3D**.
Image: `docs/reference/assignment.jpeg` (screenshot as received, 2026-10-23).

- **Title:** Hero Shooter
- **Due:** 2026-10-30, 22:00 (-03)
- **Team:** individual project ("Projeto Individual")
- **Submissions:** multiple allowed
- **Assessment, two moments (same day, evening):**
  1. 18:30–20:00 — game presentation to the Level Design e Programação 3D class
  2. 20:20–22:00 — explanation of the game's source code

Half the grade is a **code walkthrough she must deliver out loud**, so every system needs to be
something she can open, point at and defend — not something that merely runs.

## Original (pt-BR, verbatim)

```
Hero Shooter

Due October 30, 2026 10:00 PM •
Multiple submissions allowed

Instructions
Projeto Individual

Mecânicas mínimas que devem ter no jogo:

• Tutorial;
• Camera do Player em third person;
• Inimigo Ranged (FSM) – Procura, Ataca, Persegue, Corre, chama amigos;
• Inimigo Meele (FSM) – Procura, Ataca, Persegue, Corre, chama amigos;
• Tiro com mecânica correta (sem instantiate, utilize object pooling);
• Selecionar entre 3 personagens;
• 2 Habilidades para cada um dos 3 personagens;
• Mini Mapa;
• Tab para ver Frags;
• Escolher Mapa;
• +3 mecânicas a escolha do aluno;

A avaliação ocorrerá em 2 momentos:

1-Apresentação do jogo para a Disciplina de Level Design e Programação 3D as 18:30 até 20hrs
2-Explicação do código fonte do jogo as 20:20hr até 22hrs.
```

## Translation (en)

```
Hero Shooter

Due October 30, 2026 10:00 PM •
Multiple submissions allowed

Instructions
Individual Project

Minimum mechanics the game must have:

• Tutorial;
• Third-person player camera;
• Ranged enemy (FSM) – Searches, Attacks, Chases, Runs away, calls friends;
• Melee enemy (FSM) – Searches, Attacks, Chases, Runs away, calls friends;
• Shooting with correct mechanics (no Instantiate at runtime — use object pooling);
• Choose between 3 characters;
• 2 abilities for each of the 3 characters;
• Minimap;
• Tab to see frags (kill scoreboard);
• Choose the map;
• +3 mechanics of the student's choice;

Assessment happens in 2 moments:

1 — Presentation of the game to the Level Design and 3D Programming class, 18:30 to 20:00
2 — Explanation of the game's source code, 20:20 to 22:00
```

## Requirement checklist

| # | Requirement | Graded system | Owner |
|---|---|---|---|
| 1 | Tutorial | `TutorialDirector` + tutorial scene | Chloe + agent |
| 2 | Third-person camera | `ThirdPersonCameraRig` | agent |
| 3 | Ranged enemy FSM (5 states + call allies) | `EnemyBrain` + `RangedEnemy` | agent |
| 4 | Melee enemy FSM (5 states + call allies) | `EnemyBrain` + `MeleeEnemy` | agent |
| 5 | Pooled shooting (no runtime `Instantiate`) | `ObjectPool<T>` + `ProjectilePool` + `Weapon` | agent |
| 6 | 3 selectable characters | `CharacterDefinition` (SO) + character-select screen | agent |
| 7 | 2 abilities per character (6 total) | `AbilityDefinition` (SO) + 6 ability classes | agent + Chloe (design) |
| 8 | Minimap | second ortho camera → RenderTexture → `RawImage` | agent |
| 9 | Tab frag board | `ScoreService` + `FragBoardUI` (hold Tab) | agent |
| 10 | Map select | `MapDefinition` (SO) + map-select screen → scene load | agent |
| 11 | +3 free mechanics | wave director (`Systems/WaveDirector.cs`), damage numbers (`UI/DamageNumber*.cs`), pause menu (`UI/PauseMenu.cs`) — **chosen 2026-11-03, all three built and played** | Chloe (design) + agent |

## Delivery (2026-11-06)

- **Code:** https://github.com/dollythecamus/hero-shooter (private; access given at submission).
- **Builds:** release `v1.0.0` on that repo — `hero-shooter-2026-11-06-linux.zip` (38.4 MB) and
  `-windows.zip` (39.9 MB), plus the same archives in `~/builds/` for the LMS upload.
- **Walkthrough:** `docs/CODE_DEFENSE.md`, one block per requirement.
- Every row above was confirmed from a built binary by a human across six passes on 2026-11-06; the fixes those
  passes produced are in `docs/PROJECT_LEDGER.md`.

