# Case study — can a doc-driven agent loop carry a project start to finish?

Written 2026-10-24 11:5x (-03) after the S5 block, from the session store, the git history and the
docs as they stand at `aaa37e9`. This is not a feel-good retro: every claim below has the command
that produced it. § 9 is the plan for a fully autonomous loop, § 10 is the creative-designer role built
from Chloe's answer on design eccentricity, § 11 is what is still open.

Question being tested: **"the failures that needed my intervention were documentation failures;
fixing documentation is what makes a fresh agent session able to continue."**

## 1. Corpus

| Measurement | Value | Where |
|---|---|---|
| Human messages in the whole project | **23** | `messages where role='user'` over the 9 project sessions, excluding duplicate resends and injected OOB wrappers |
| Message rows incl. tool output | ~1 570 | same query, all roles |
| Sessions started cold (new context, no chat continuity) | 6 | session store: 10-23 19:20, 19:37, 19:41, 21:28, 10-24 10:15, 10:42 |
| Commits | 34 | `git rev-list --count HEAD` |
| of which `Docs:` / `Code:` / `Ledger:` / other | 14 / 5 / 5 / 10 | `git log --format=%s` |
| Docs lines added : C# lines added | 3 759 : 6 092 (**62 %**) | `git log --numstat` by path |
| Independent doc audit | 11 wrong claims of ~240 (**4.6 %**) | commit `80944c9` |
| Wall clock | 10-23 10:55 → 10-24 11:41 (~25 h, two sittings) | first and last human message |

Two days of a 10-hour sprint: S1→S5 landed, each one measured in play mode, nothing re-explained
twice.

## 2. Every intervention, classified

`GO` = direction only · `DOC` = documentation / plan / next-step / process · `TASTE` = design or
specification · `ENV` = unblocking tooling · `BUG` = functional defect or contract correction.

| # | Time | Message (abridged) | Class |
|---|---|---|---|
| 1 | 10-23 10:55 | brief + "make the project, write the plan" | GO |
| 2 | 12:47 | four decisions, revise plan to one sprint | TASTE |
| 3 | 13:16 | "begin S1, add Unity MCP" | GO + ENV |
| 4 | 13:26 | "compiled, mcp active" | ENV |
| 5 | 14:29 | "weapon is not firing with lmb, fix before S2; Level.unity was me" | BUG |
| 6 | 14:38 | "you do wiring with bridge, i verify … write that to plan" | DOC |
| 7 | 15:12 | "looks okay" + deferred balance plan + RPS character design | TASTE + DOC |
| 8 | 15:20 | "forgive the interruption, continue" | — |
| 9 | 16:15 | "layermask values are my edit, not a bug … UI shouldn't be built at runtime" | BUG (false alarm) + TASTE |
| 10 | 17:45 | "halt for now, writing docs and updating plans" + feedback step + ability rework spec | DOC + TASTE |
| 11 | 18:12 | answers to 10 numbered design questions | TASTE |
| 12 | 18:23 | "revert and write docs" | DOC |
| 13 | 19:20 | "apply docs-driven-dev-pipeline, create documentation and architecture" | DOC |
| 14 | 19:37 | "execute next actions" | GO |
| 15 | 19:40 | "just add this fix step to next actions" | DOC |
| 16 | 19:41 | "execute next actions" | GO |
| 17 | 20:40 | "mcp is on now, go for 2" | ENV + GO |
| 18 | 21:28 | "continue executing next actions" | GO |
| 19 | 21:48 | "interrupt and write the next steps you didn't complete in docs" | DOC |
| 20 | 10-24 10:15 | "go for continuing your tasks finishing S4" | GO |
| 21 | 10:25 | "editor ok" | ENV |
| 22 | 10:39 | "sprints 5-7 were removed? please recover them. next is not producing builds" | DOC |
| 23 | 10:42 | "continue next actions, now S5" | GO |

Tally: **GO 8 · DOC 8 · TASTE 5 · ENV 4 · BUG 2** — 27 classes over 22 classified messages (msg 8 is noise), 5 messages carrying two.

Chloe's claim checks out on the main axis: DOC ties GO as the largest category, and — the point that matters — **every one of the 7 DOC messages changed what the next session would have
done.** The GO messages changed nothing but the clock. A GO is cheap; a DOC is load-bearing.

Her second claim needs one correction: TASTE is not the only other source. There is also one real
bug report (LMB not firing), one false-alarm correction (the layer edit she made in the editor,
which the agent read as a defect), and 4 tooling unblocks. None of those were avoidable by
documentation; they were environment and perception limits. That matters for the loop plan: they
are the failure class that needs a *channel* (editor bridge health, her editor edits visible to the
agent), not a document.

## 3. The six failure modes, each with the commit that proves it

**F1 — Absence.** Until 19:31 the repo had `PLAN.md` and a ledger but no `ARCHITECTURE.md`,
`AGENTS.md` or feature docs — 8 hours of code where the only description of the system was the
plan that predicted it. Fix: stage-0 retrofit `f1bf7e7` (10 files, 1 011 lines), written from the
code, with a *Not built yet* section naming what `PLAN.md` § 3 pretends exists.

**F2 — Wrong at write time.** The retrofit was written by the session that had built the code. A
separate audit context found **11 of ~240 claims wrong (4.6 %)**, and **3 of the 11 were code
defects the docs had faithfully copied** — the inverted `CooldownRatio` comment, the dead climb
`magnitude`, the ally that walks to its own target (`80944c9` → fixed in `7510f5e`). Documentation
was not only memory here; it was a test instrument that found three real bugs play-testing had not.

**F3 — Rewrite from the block that just finished.** `0b20b2e` (the VFX docs pass) rewrote the
ledger's *Next action* from the block it had just closed: "S4 is closed. What is left before the
deadline is the submission work in `docs/PLAN.md` § 6–7" — and then listed only builds and
`CODE_DEFENSE.md`, **deleting S5, S6 and S7 from the plan of record while citing the very section
that still contained them.** A fresh session at 10:42 would have started producing builds for a
game with one scene. Chloe caught it at 10:39 (`bf19d4e`). This is the single most expensive
defect in the corpus and it is *not* an absence of documentation — it is a document that was
right, rewritten from the wrong source (the finished block) instead of the authority (the plan).

**F4 — The docs pass does not land when the context runs out.** Twice: 21:48 "interrupt and write
the next steps you didn't complete in docs" (verification had finished; the docs-from-diff pass
and the commit had not started) and 22:5x with the S4 block left uncommitted. Cause is structural:
the pipeline puts documentation *after* verification, so documentation is the first thing lost when
time or context ends. But documentation is the *only* thing that survives the session.

**F5 — Boundary ambiguity.** "halt for now, writing docs and updating plans" (17:45) was read as
permission to begin the ability rework; an unverified implementation pass had to be reverted at
18:23. Cost: ~40 minutes and one full revert. Fixed by a written rule ("a halt stops every write"),
which is a convention the docs had to carry because the agent could not infer it.

**F6 — The gates themselves rot.** Found live while writing this, at `HEAD`:

| Documented gate | Reality at HEAD | Why it rotted |
|---|---|---|
| `grep -c "error CS" Logs/Editor.log` → "verified 0" | **21** | The count is cumulative over the whole log; the 21 are old `CS0103 Vfx` lines from the VFX compile that the missing Particle System module caused. As written the criterion can never pass again. |
| `grep -rn "SceneManager.LoadScene"` → "3 sites, all in GameFlow.cs" | **5 lines** | S5 added a `///` comment in `GameFlow` and a tooltip in `MapDefinition` that mention the symbol. The check asserts text, not call sites. |
| "6 task files" | 2 of them (**S2**, **S3**) are referenced by nothing | The ledger links S1, S3b, S4, S5 only. A fresh agent enumerating `docs/tasks/` cannot order two of the six blocks. |
| Ledger timestamps for two of Chloe's messages | off by ~1 h (21:48 recorded as "22:5x", 10:39 as "11:4x") | Hand-typed metadata. Nothing generates it. |
| `README.md` status paragraph | still said S5 was "still to build" at `HEAD` although `9cc4233` landed it | The status block is a fourth independent copy of build state (README, `ARCHITECTURE.md`, feature docs, ledger) and it drifted. Found and fixed while writing this study. |

F6 is the most important finding for the loop, because it means *a human re-reading the docs would
not have caught it* — the checks look authoritative and are wrong. Gate rot is a machine problem and
needs a machine check.

## 4. What the fixes bought, measured

- After the retrofit (19:31), **6 cold session starts produced 5 committed blocks** with no
  architecture re-explanation: `7510f5e` audit fixes, `1960b0a` ability rework, `5c40a99` HUD,
  `4da3ef1` VFX, `9cc4233` S5. Each session opened by reading the ledger's next action and closed by
  appending to it.
- Requirement traceability never drifted: `docs/PLAN.md` § 6 block table → `docs/tasks/*.md` gate
  table → ledger Evidence. The one time that chain broke (F3) it was repaired in 25 minutes.
- The cost: docs are **62 % of the C# line count** and `Docs:`-only commits are **41 % of commits** (another 15 % are ledger checkpoints). That is the price of a
  project that resumes from a cold context, and it is cheap compared with re-deriving an
  architecture from 6 000 lines of C# under a Unity project with serialized wiring.

## 5. What documentation cannot carry

Two limits, both visible in the corpus.

**Eyes.** Every gate in this project is a number. The ledger says so twice, in its own words:
"Nothing has been *looked at* — every row is a number, not a judgement of layout", "Nobody has
looked at the Game view". A doc-driven loop verifies *causality*, not *appearance*.

**Taste.** Five TASTE messages, and **four of the five are specification, not judgement**: the
character design itself (#7), the UI-must-be-a-prefab constraint (#9), the ability rework spec
(#10, which contains "knockback is not working", "climb just boosts them up arbitrarily") and the
answers to the ten questions (#11). She named the cause herself: *"i should've specified character
abilities in detail. if any design detail is missing you should ask me clarifying questions"*.
And the repair that worked was mechanical: the agent emitted **10 numbered design questions**, she
answered them in **one message** (18:12), and the answers became a 6-row verification table a
machine then measured (`1960b0a`). Genuinely un-machineable judgement appears exactly **once** in
the corpus, as one sentence inside #7: *"looks okay from simple testing, nothing looked strange but
definitely will go through more balance testing once we are done"*.

So the taste gap decomposes: **spec completeness** (catchable, by forcing the interview at block
start) and **feel** (not catchable by text; needs a player or a vision proxy). There is a third piece the
corpus makes visible without naming it — the agent's *defaults* — handled in § 10. The loop plan below
treats them as two different controls instead of one vague "human in the loop".

## 6. Failure mode → control map

| Mode | Control (mechanism, not intention) |
|---|---|
| F1 absence | Block 0 of any project is the doc retrofit; no code commit before the hot tier exists |
| F2 wrong at write time | Every doc pass is written by a **different context** than the implementation; a claim audit every ~5 blocks |
| F3 rewrite-from-the-block | *Next action* is a **derived view**: `docs_lint` asserts every block id in the plan schedule appears in the ledger as open or done-with-a-commit |
| F4 docs never landed | Documentation is a **gate**, not a step: a block is not closed until `docs_lint` passes at HEAD; closing order is commit → docs → ledger, all before the context is allowed to end |
| F5 boundary ambiguity | Permissions are a machine-readable list in `AGENTS.md`; anything not listed is a stop; a stop is a state in the ledger, not a sentence in chat |
| F6 gate rot | `gates.sh` **executes** every command in `AGENTS.md` and compares *shape* (regex / call sites / after-last-build), never raw counts; a failing gate fails the commit |
| Eyes | Screenshot → rubric → vision judge, per block, recorded in the task file |
| Averaging (taste / design) | Forced-random constraint roll, published with its seed before design starts; a designer role that owns ≤3 non-negotiables in a written manifesto; `docs_lint` L9 fails a block whose gate table drops one → § 10 |
| Taste / spec | Numbered design-question interview at every block that touches design; unanswered questions become recorded defaults, never silent choices |
| Taste / feel | Deferred playtest window with a human, a self-play metric or a VLM rubric, recorded as a gap, never claimed as verified |

## 7. The loop

Seven roles, each its own context (the 4.6 % error rate is what happens when one context both writes
and checks):

| Role | Reads | Writes | Never |
|---|---|---|---|
| **Controller** (planner) | brief, plan schedule, ledger, `gates.sh` output | task file for the next block, ledger | touches source |
| **Implementer** (`opencode`) | `AGENTS.md`, the one feature doc, the task file | source, wiring, commits | reads the ledger or other feature docs |
| **Verifier** (subagent) | the diff, the gate table | a verdict | fixes anything |
| **Scribe** (subagent) | the diff, the feature doc | feature doc + `ARCHITECTURE.md` line | trusts doc comments |
| **Designer** (see § 10) | theme, the published roll, reference games, playtest/eyes evidence | `docs/DESIGN.md`: manifesto, ≤3 non-negotiables, design-delta tables | implements, or gates anything |
| **Auditor** (subagent, every ~5 blocks) | docs + source | claim-level findings | edits code |
| **Eyes** (vision) | screenshots from the build/editor | look findings vs rubric | judged in the same context that built the UI |

Cycle per block:

```
designer (hour 0): roll N combos → publish seed+rolls → score → DESIGN.md (manifesto + ≤3 non-negotiables)
controller: pick block from plan schedule → write task file (gate table) → doc_lint  [L9: non-negotiables appear]
if block touches design: emit ≤20 numbered questions → answers or recorded defaults → write into task file
after every playtest / eyes pass: designer writes the design-delta table → implementer executes → verifier measures
implementer: build the block
verifier: run every gate row → verdict
eyes: screenshot set → rubric verdict
scribe: feature doc from the diff (bodies, not comments)
controller: commit code ‖ commit docs ‖ commit ledger → docs_lint at HEAD → advance
```

Rules that came straight out of the corpus:

- **Timestamps are generated**, never typed (`date -Iminutes` piped into the entry).
- **Metrics go into the ledger before prose** — probe numbers are the expensive, perishable part.
- **Two commits per block minimum**, code and docs; the ledger third.
- **A closure requires a commit hash that `git cat-file -e` can resolve**, and `docs_lint` checks it.
- **Anything not proven is written as a gap in the same sentence** ("not proven: the five steps
  under a human at the keyboard"), because the next agent reads gaps as facts if they are absent.
- **A block interrupted mid-way is closed as `interrupted` with its resume command**, not left blank.

## 8. `docs_lint` — the checks, with the failures they would have caught

| # | Check | Catches |
|---|---|---|
| L1 | every block id in the plan schedule ∈ ledger (open or done-with-hash) | **F3** — reproduce it: at `0b20b2e` the check reports `S5 S6 S7` missing; at `bf19d4e` and `HEAD` it reports only `S0` (legitimately done) |
| L2 | every ~~done~~ item carries a hash that exists and whose subject matches | phantom completion |
| L3 | no doc says "planned / not built / not started" about a symbol that now exists | **F1** tail, the "cheapest lie in a repo" |
| L4 | every command in `AGENTS.md` is executed; output compared by shape | **F6**, both gates |
| L5 | every `docs/tasks/*` and `docs/features/*` is referenced from README / `ARCHITECTURE.md` / ledger; every referenced path exists | **F6** orphans (today: S2, S3) |
| L6 | ledger dates for logged events match the session store within 5 min, or the entry carries no time | **F6** timestamps |
| L7 | every requirement row in the brief maps to a system *and* to a gate row in some task file | silent scope loss (S6 default mechanics, minimap, frag board) |
| L8 | **resume probe**: a fresh subagent gets hot tier + ledger only and must emit the next action; mismatch with the ledger's next action = doc fault, block stops | **F3/F4** before a human sees it |
| L9 | every non-negotiable in `docs/DESIGN.md` appears in the gate table of every block whose work touches it | design dilution (D1, § 10.7) |
| L10 | `docs/DESIGN.md` exists — with its seed and ≥8 published rolls — before the first code commit | rationalised dice (D4, § 10.2) |

Verified today: L1 (`0b20b2e` → S5–S7 missing; `HEAD` → clean), L4 (`error CS` shape: last
`Tundra build success` at line 7399 of `Logs/Editor.log`, **0** error lines after it; the call-site
grep with the paren → exactly 3, all `GameFlow.cs`), L5 (S2 + S3 orphaned).

## 9. Plan — the autonomous start-to-finish loop (jam parity)

Goal: the loop takes a requirements document and produces a submitted game with the same wall
clock, tool access and deliverable as the humans in the jam, with **no human design input after
the spec interview**.

**P0 — infrastructure, before day 0** (the loop cannot be debugged during the jam):

1. `tools/gates.sh` — one command per project: compile shape, asset/wiring probe through the
   editor bridge, the static greps, `git diff --check` with the engine-whitespace exemption. Every
   command must be *executed* by it, including the ones `AGENTS.md` advertises.
2. `tools/docs_lint.py` — L1–L7. Roughly 200 lines; L1/L5/L6 are file parsing, L4 shells out.
3. `tools/resume_probe.py` — L8: spawn a fresh subagent with `ARCHITECTURE.md` + `AGENTS.md` +
   ledger + the plan schedule, ask for `NEXT: <block> | <gate row 1> | <first action>`, diff.
4. Templates: ledger (goal, done-when, next-action-derived, decisions, open questions, evidence,
   requirements log), task file (gate table + landed evidence + resume command), feature doc.
5. `docs/LOOP.md` — roles, permissions, stop conditions, escalation, kill switch.
6. `tools/design_roll.py` — the constraint roll of § 10: sample N combinations across the category table,
   each element carrying **what it forbids / which gate can observe it / block cost**; write the seed, the
   rolls and the scores into the ledger *before* the design is written.
7. Templates: `docs/DESIGN.md` (manifesto + ≤3 non-negotiables + reference-game table) and the
   design-delta table used to turn playtest evidence into numbers.

**P1 — drills, before day 0** (prove the controls with injected faults; a drill that does not run
is not a control):

| Drill | Inject | Pass |
|---|---|---|
| D1 collapse | rewrite next action from the finished block only | `docs_lint L1` fails naming the dropped blocks |
| D2 gate rot | move a file so a documented grep changes shape | L4 fails on the *doc*, not on the code |
| D3 stale claim | mark a shipped system "planned" | L3 fails |
| D4 resume | kill a session mid-block | L8 reconstructs the next action; ≤2 messages to a correct plan |
| D5 eyes | break the HUD layout deliberately | the vision judge flags it against the rubric |
| D6 boundary | issue "halt" mid-block | no write occurs; ledger shows state `halted` |
| D7 dilution | after block 2, force a gate failure on the non-negotiable | the design is re-negotiated *in writing*; the non-negotiable is still in the design and L9 still finds it in later gate tables |
| D8 unfalsifiable vision | add a non-negotiable with no observation channel | the roll's veto rejects it at hour 0, before any code |
| D9 role collapse | reset the designer's context between phases | its first output is "what does the vision forbid here?", and it matches the manifesto (this is the resume probe applied to taste) |

**P2 — the loop's own success criteria**, measured per project, not per feeling:

- zero unrecoverable resets (a reset that needs a human to re-explain the architecture = failure),
- every closed block has a resolvable commit + a passing `docs_lint` at HEAD,
- `docs_lint` red at any commit = the loop stops and fixes docs before any new code (this is the
  discipline the corpus is missing today: F3 and F6 both survived commits),
- no block closed on "it compiles" — each gate row is a measurement,
- the human intervenes only for: sign-off items in `AGENTS.md`'s permission list, the spec
  interview, and the final playtest.

**P3 — jam-day shape** (48 h, parity):

| Hours | Loop |
|---|---|
| 0–1 | controller reads the theme + rules; writes the plan schedule, requirement map, and the numbered design questions (≤20) |
| 1–2 | **the only human contact**: answers batched into one message; unanswered → recorded defaults |
| 2–8 | blocks 1–2 (core loop playable) + eyes pass each block |
| 8–10 | playtest proxy: self-play metrics + screenshots → rubric verdicts → balance patch as its own block with before/after numbers |
| 10–20 | blocks 3–5 (content, UI, maps) — implementer/verifier/scribe pipelined; audit at block 5 |
| 20–24 | sleep window; nothing runs (the corpus shows no quality gain from the 21:48–10:15 gap and one revert from working past a halt) |
| 24–40 | remaining blocks; every 5 blocks an audit |
| 40–44 | builds, clean-folder run, submission doc, `CODE_DEFENSE`-equivalent README |
| 44–46 | freeze + submission; the loop writes its own post-mortem with the same evidence standard |
| 46–48 | buffer, and the ledger is closed with the unverified list |

**P4 — the honest weak points** (state them before the run, not after):

1. **The ledger is written by the agent that just finished the block.** L8 and the scribe split
   reduce it; nothing removes it.
2. **The vision judge is a weak proxy for fun.** It can catch "HUD overlaps the crosshair"; it
   cannot catch "this fight is boring". P3's playtest proxy is the weakest part of the plan and the
   one most likely to need Chloe's idea.
3. **Engine environments leak into the loop**: an editor that holds the project lock, a GUI-only
   build, a bridge that dies (recorded at 20:12 on 10-23). The loop needs a health check on the
   bridge at every block start and a documented recovery, or it will stall on infrastructure and
   mistake it for a code problem — which is exactly what happened for 90 minutes on 10-24 until
   the missing Particle System module was found.
4. **Cost**: 62 % doc overhead. In a jam with a hard clock that is affordable only because blocks
   are small; if the schedule slips, the docs are what get cut, and then F1–F4 come back within two
   blocks. The controller must protect the doc gate above any individual feature.

## 10. Design eccentricity — the creative role (Chloe's answer, applied)

**The problem she names is measurable in this repo.** The agent's own list of self-chosen mechanics was
A wave director / B ultimate meter / C damage numbers / D destructible cover / E pause menu, ranked "by
grade-per-hour" — five genre defaults, chosen by the criterion that minimises surprise. The only
distinctive decision in the entire build (rock–paper–scissors heroes with a Team Fortress 2 lineage,
`6548b33`) came from Chloe. The averaging is visible in the *plan document*, before any code exists.

Her proposal: one **creative-designer** role that roleplays a vision, rolls forced randomness across
design categories (controls, perspective, genre, art style, platform, themes), scores the combinations
on feasibility and eccentricity, picks one, and studies reference games before writing the design.

The idea is right and it needs four mechanisms to survive the loop. Each one comes from a failure this
corpus already demonstrates.

### 10.1 The roleplay is the sampling temperature; the manifesto is what survives the reset

The lesson of the whole study is that **nothing not written in the repo survives a context reset** — the
ledger exists because of exactly that. An eccentric *persona* evaporates on the first reset; the 10:42
session has no memory of caring about anything. So the designer's first artifact is `docs/DESIGN.md`:

- **the manifesto** (≤1 page): what this game is, in the designer's voice, unhedged;
- **≤3 non-negotiables**: each one a claim about the player's experience that later roles may not
  quietly drop;
- for each non-negotiable: *the gate that observes it*, *the cheap implementation path*, *what it
  forbids*, and *what the design becomes if it must be cut at 75 % of the budget*.

Roleplay is how the designer generates the manifesto. The manifesto is how the designer still exists two
contexts later. Without it the eccentricity is a mood, and moods lose to the first failing gate.

### 10.2 The dice must be published, not narrated

"Rolls three combinations and picks the best" is only *forced* randomness if the roll is published before
the scoring. Otherwise the designer rolls, sees its own preferences, and rationalises — the same
self-report problem the verifier role exists to solve.

- the seed goes in the ledger (`date +%s`), the game and the rolls in `docs/DESIGN.md` § Roll;
- all N rolls are published **before** any scoring text is written;
- the scoring rubric is written before the rolls are read;
- the roll is part of the record: an audit can replay it and check the pick.

Three rolls is too few for a high-variance space (you get one survivable combination, not a good one).
Sample **8–12**, then run a *coherence repair* pass (reject contradictions like VR + tabletop-ASCII), then
score. Every element in the category table carries three annotations that turn an aesthetic list into an
engineering artifact: **what it forbids · which gate can observe it · block cost**.

### 10.3 Eccentricity only survives if it forbids something

"Pick 2 art styles to combine" is a skin: the first implementer that has to build assets averages it back
to "URP Lit, three colours", because nothing in the gate table defends it. "Only one button" is a
constraint: it forbids aim, forbids strafing, and forces a whole different combat system — an implementer
cannot average it away without deleting the game.

So the score is not feasibility × eccentricity. It is:

```
veto:      element has no observation channel            → rejected before scoring
score  =   design compression × distinction
subject to block cost ≤ the block's budget (the controller's number, not the designer's)
```

*compression* = how many downstream decisions the element forces (a control scheme and a camera
perspective score high; a colour palette scores ~0). Requirement: the chosen combination must generate
**≥2 mechanical consequences**, not only cosmetic ones, or it is re-rolled. *distinction* = distance from
the default plan for that genre+platform, which is cheap to state: the default plan is in `PLAN.md`, and
it is the thing we are trying not to write again.

### 10.4 The loop's aesthetic bias is measurability, and it needs a budget line

Every gate in this project is a number: `fov=60.0 → 42.0`, `created=24` before and after, `4/4` agents on
the NavMesh. That is what made 30+ gate rows trustworthy — and it is also a structural bias: a health bar
is cheap to measure, a strange idea is not, so the loop will always prefer the health bar. An eccentric
design evaluated only by cheap gates loses in round one.

Rule: **at least one gate per project, and ~15 % of the block budget, belongs to a feature whose
observation is a rubric judgement (screenshots + VLM, or a self-play metric), and it is protected from
the cut list.** The designer proposes it; the controller reserves the time; the verifier may fail it but
may not delete it.

### 10.5 The reference study needs authority, not length

"Study similar games" is right, and this repo shows what makes such a study load-bearing: `HERO_ROSTER.md`
carries the line *"where it disagrees with the code, the roster wins"*, and it was written as decision
tables with numbers. A reference study without authority is decoration. So `docs/DESIGN.md` carries a
table — *game · what it does · copy · avoid · which of our gates would catch us failing* — and where it
disagrees with an implementer's convenience, the table wins.

### 10.6 Playtest → design delta, in the shape that already worked

The corpus has the pattern: the agent asked **10 numbered design questions**, Chloe answered in **one
message** (18:12), and the answers became a 6-row verification table a machine then measured. The designer
role inherits exactly that shape after every playtest or eyes pass:

| observed | from → to | the gate that will show it | cost |
|---|---|---|---|
| "knockback does nothing" | 0 m displaced → ≥1 m | `enemyMoved` in the probe | S |

Feel fixes enter the loop as *numbers with a rationale and a gate*, never as "make it better". That is how
five taste messages became three shipped fixes with quoted measurements.

### 10.7 Where the other bots do the averaging — and where they must not

Her framing is "the sensible bots average it out to a reasonable working project". Be precise: the
averaging has to happen in **implementation cost**, never in the design document.

- the verifier may fail the implementation and hand the failure back;
- the implementer may propose a cheaper path for a non-negotiable, in writing, with the cost it buys;
- **only the designer may trade a non-negotiable away**, in writing, with the reason and the fallback;
- the controller may not trade it to make a schedule, and `docs_lint` **L9** enforces this: every block
  whose gate table touches a non-negotiable must have a row naming it. A block that quietly serves
  neither the vision nor a gate fails the lint.

### 10.8 Failure modes of this role, each with its control

| Mode | Symptom | Control |
|---|---|---|
| D1 dilution | the ecc becomes "a suggestion" by block 3 | manifesto + non-negotiables + **L9** + drill D7 |
| D2 incoherence | VR + tabletop-ASCII | coherence repair pass + feasibility veto at the roll |
| D3 unfalsifiable vision | "it should feel tense" | every non-negotiable names its observation channel or is rejected (drill D8) |
| D4 rationalised dice | the pick always matches the designer's taste | seed + publish-before-score + audit of the roll |
| D5 cosmetic ecc | the weird thing is a colour | compression score; ≥2 mechanical consequences or re-roll |
| D6 ecc eats the schedule | blocks overrun defending the vision | cost per element at hour 0, fallback and cut order pre-agreed |
| D7 role collapse | after two resets the designer writes like a planner | its first output each phase is "what does the vision forbid here?" — the resume probe, applied to taste (drill D9) |

## 11. Still open

1. ~~The taste idea~~ — answered, recorded in § 10. What is *not* settled inside it: who arbitrates when
   the designer's non-negotiable and the feasibility veto disagree at hour 0 (my default: the veto wins
   and the roll is re-drawn, because an unbuildable vision is not eccentricity, it is a schedule risk).
2. **Which human contact points survive parity.** The spec interview is defensible (a solo jammer also
   interrogates their own design), the mid-run playtest is not — a jammer's playtest is themselves. The
   designer role makes this sharper, not softer: it can *convert* playtest evidence into design deltas,
   but it cannot generate the evidence. Decide before the run whether the loop gets one playtest, one
   VLM/self-play stand-in, or neither.
3. **The engine target.** Unity's editor lock, GUI builds and the MCP bridge are three hard human
   dependencies in this project. Godot (headless export, `--headless` runs, scriptable scenes) removes all
   three and would make a cleaner experiment — but it is a weaker test of "can it survive a real
   toolchain".
4. **Category table ownership.** The six categories she listed are 5–8 options each; the *annotations*
   (forbids / gate / cost) are the expensive part and are worth writing once, for the whole experiment,
   rather than per jam. That is a half-day of work before P0 and it is the piece most likely to be
   skipped.
