# 2026-11-06 — Hero Shooter human-gate trial

**Status:** planned with Chloe · 2026-11-06 11:00–11:15 Recife time (UTC−03)

## Fixed task

Run the existing Linux build and report one result: `PASS`, `ISSUE`, or `BLOCKED`.

**Build:** `/home/chloe/builds/hero-shooter/linux/HeroShooter.x86_64` (verified executable on 2026-11-05).

Launch it with:

```sh
/home/chloe/builds/hero-shooter/linux/HeroShooter.x86_64
```

From the boot menu, choose **Play → Rock → Arena**. In gameplay, check only these three things:

1. **HUD + minimap:** health/HUD is visible and readable; the actual minimap image is visible and shows the player/map without obvious clipping or overlap.
2. **Frag board:** hold `Tab`; the board appears. Release `Tab`; it disappears.
3. **Pause/resume:** press `Esc`; the pause panel appears. Click **Resume**; gameplay continues.

### Record the result

- `PASS` — all three checks completed and passed.
- `ISSUE — <what you saw>` — at least one check failed; quote the visible behavior, not a diagnosis.
- `BLOCKED — <what stopped you>` — the build could not be launched or the checks could not be reached; include the exact error or obstacle.

No preparation or code inspection is part of Chloe’s task. If an issue is reported, the agent owns diagnosis, fixes, and automated rechecks. Do not change the user’s working-tree files during this gate.

## Scope boundary

This is a 15-minute first slice and a trial of the human-gate workflow, **not** completion of the full play pass. The second map, tutorial, damage-number behavior and other assignment checks remain open until separately verified. A `PASS` closes only these three observations.

## Result — reported by Chloe 2026-11-06 11:05

Verdict: **ISSUE** — one of three checks passed.

| # | Check | Result |
|---|-------|--------|
| 1 | HUD + minimap | **ISSUE** — HUD readable, but layout boxes look stretched horizontally; minimap shows the player dot only, enemies do not appear |
| 2 | Frag board (Tab) | **PASS** — appears on hold, disappears on release |
| 3 | Pause/resume | **ISSUE** — buttons in the panel are not clickable; after resuming, the mouse does not lock |

Her words, check 1: *"hud readable but ISSUE: layout boxes look stretched horizontally, just a bad looking visual. minimap ok but ISSUE: player dot appears in minimap, enemies should also appear as visible dots in minimap"*.
Check 3: *"buttons in panel not clickable, and when resuming from pause, mouse doesn't lock"*.

Not a full play pass. Never reached: damage numbers, second map (`21_Map_Alley`), tutorial, Scissors (she did not pick that hero — revisit later).

## Scheduling boundary

Chloe chose the time in chat. No Google Calendar event or Dayboard item was created: Calendar credentials are absent and Dayboard reports that this device is not paired. The time is recorded here and in the project ledger, but this file does not itself generate a reminder.
