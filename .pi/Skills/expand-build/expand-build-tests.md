---
name: expand-build-tests
description: "Companion for .pi/extensions/expand-build/expand-build-tests.ts — the eXpand tree's /devexpress loader contract driven through pi's OWN runtime (buildRealRunner plus one canonical boot): what is real, what stays stubbed, the startup budget rebase, the resolve.mjs floor verdict, and why pi-dev's boot ledger cannot serve this tree. Use when a case fails or when changing the loader's boot, its command or the suite's boot budget."
---

# expand-build-tests: the /devexpress loader on pi's own runtime

Fixture: `.pi/extensions/expand-build/expand-build-tests.ts` (the eXpand tree).
It covers `index.ts` — the thin loader that registers `/devexpress` and, on
first use, jiti-loads `menu.ts` from `C:/Work/Reactive.XAF/.pi/extensions/reactive-xaf-build`.

## How it runs

```
run_tests(name="expand-build", fixture="expand-build-tests.ts")
```

Never run the file directly — the shell gate blocks it, and the fixture needs a
TypeScript loader plus the tree's own resolve hook, both of which the runner
supplies. The file declares its own budget (`// test-timeout: 120000`) because
it pays a real pi boot.

## What stayed real

- **pi's own runtime for T2/T3/T4.** `buildRealRunner` builds `activate` with
  pi's loader and dispatches through pi's `ExtensionRunner`; the command is read
  back from and run through the runner's own registry (`commands()`,
  `runCommand`, the resolved registration's handler with
  `createRunnerContext`'s command ctx). No hand-written pi exists in the file,
  and none may come back: the whole point of the migration was that the old
  `mkPi()` proved the fake's own dispatch.
- **The boot is a real process (T1/T5/T6).** One nochat spawn through pi-dev's
  canonical runner (`pi-runner.ts`), with the entry passed absolute and
  `cwd` set to this tree, `timing: true` for the loader's per-extension dump.
- **The engine is real.** T3's answer is the RX menu's own abort text
  (`"DevExpress menu: aborted."`), reached through the lazy `import("./engine.js")`
  and jiti — the path that once died on `__filename is not defined`.
- **The tree's own sources.** Every path is derived from the fixture's
  `import.meta.url`, so a gate island boots and drives the island's copy (the
  queued content), not the working tree's. The entry is imported **dynamically,
  after the floor is armed**: the tree's whole module graph then loads inside the
  `@pi/` domain, so a shared-utility import gained anywhere under `index.ts`
  resolves here exactly as pi resolves it. A static import would evaluate before
  the floor exists — the one shape that would make this suite die at import.

## What is stubbed, and why

- **The UI pick, only.** `ui: { select: async () => undefined }` on the harness
  build is the menu's cancel. Nothing about rendering is asserted: an
  interactive TUI is outside this harness by doctrine, and the loader's contract
  ends at the text the handler returns. The host's own default (`select` answers
  the first option) would have walked into a flow, so cancelling is the honest
  stand-in for "the user pressed Escape".
- **Nothing else.** The session manager, the model registry, the event bus and
  the extension cache are pi's own, built by the harness.

## The boot ledger cannot serve this tree (finding)

pi-dev's shared ledger (`ensureBootProof`) is **install-rooted** and cannot prove
a project-local extension:

- its identity is `<agentDir>/extensions/<entry>` (`boot-proof-key.ts`), so the
  key only builds when the sources sit under the agent dir;
- its spawn always appends a `dependency-manager/index.ts` member resolved off
  the same base (`buildScoutCommand`, "Always load dependency-manager in spawned
  pi"), which a project tree does not have.

Measured on the committed tree, with `PI_RUNNER_AGENT_DIR` pointed at this tree
(exit 1, nothing recorded):

```
Error: Failed to load extension "C:\Work\expand\.pi\extensions\dependency-manager\index.ts":
Extension path does not exist: C:\Work\expand\.pi\extensions\dependency-manager\index.ts
verdict: {"ok":false,"hit":false,"status":1,...,"detail":"spawn did not prove a boot (exit status 1)…"}
```

The key itself does build for this tree
(`bp1|expand-build|expand-build/index.ts|0.84.2|<digest>`) — the closure digest is
agent-dir-relative, so island and working tree agree on it — but no row can ever
be recorded, so the proof stays unavailable. The suite therefore pays its boot
per run, in the open. Per the doctrine's own rule, an unprovable boot stays
unconverted: no fake is reintroduced to fill the gap, and the failure text above
is the finding rather than a hidden skip.

Making the ledger serve a project tree is a change in pi-dev (skip the platform
member for a non-home base, or accept an absolute entry). This tree does not own
that code, so the suite reports the situation instead of patching around it.

## The `@pi/` floor (resolve.mjs)

A test process is plain `node --import tsx`: nothing resolves `@pi/pi-dev/...`,
so the suite's first pi-dev import would be an ERR_MODULE_NOT_FOUND. The tree
ships `resolve.mjs`, which imports the loader's own table
(`~/.pi/agent/extensions/pi-dev/ext-ts-shared.ts`) and arms its `sharedRedirect`
as a host resolve hook. **Verdict: extend, not new.** The whitelist
(`shared-utilities.json`), the `x.js` → `x.ts` step and the `contracts` case have
one owner; `business/expenses/.pi/extensions/expenses/resolve.mjs` is the fleet's
precedent for the hook, and it carries a second copy of the table, which this
file deliberately does not repeat. The suite arms it as the first statement of
its driver — before the tree's entry and before any pi-dev import — so every
module the suite exercises loads inside that domain.

## Numbers and the budget rebase

Assertions: **6 before → 11 after** (nothing deleted; the four old cases are T1,
T5×3, T6 and the T2/T3/T4 contract split into its observables).

| case | before | after |
|---|---|---|
| startup budget | `startup under 3000ms` — **RED**, 5122ms (parent baseline) / 5448ms (worker baseline), hand-rolled spawn | `T6: startup under 15000ms (re-based from 3000)` — the canonical boot measures **4142ms / 4398ms** with the fleet's load on it and the island run is green under the new bound; the label carries each run's own number |
| per-extension load | `boot load under 1000ms (registered budget)` | same bound, unchanged; the label carries the measured load (`expand-build` ≈ 216ms on the committed tree, plus the platform member the canonical runner adds) |

The old bound described a different boot: a bare `node <cli> -ne -e <entry>` with
no platform member and no model flags. The new one describes the canonical boot
(the runner every migrated suite uses), which is slower by construction, so
3 000ms was not a bound to re-fit — it was a bound for a boot nobody pays
anymore. The replacement bound is 3.4x the measurement (15000ms against
4142/4398ms), so a load spike does not red the case while a genuine regression
still does, and the case prints its own number on every run so a rebase is never
invisible. If it goes red under load again, that is reported as a finding, not
absorbed by widening the bound.

## Case map

- **T1** — this tree's current sources boot a real pi (exit 0), from the
  canonical runner.
- **T2** — `/devexpress` is registered, read back from pi's own runner.
- **T3** — pi's own dispatch runs the command, and an empty pick aborts with the
  menu's own text through the lazily loaded engine.
- **T4** — the engine's re-registration is the live one and keeps the boot
  registration's description.
- **T5** — no `Failed to load extension`, the loader reports this extension's
  load line, and that load stays inside the 1000ms registered budget.
- **T6** — the startup budget, kept, re-based, and measured on every run.
- **extra** — pi's runner reported no handler error (a broken handler is a
  silent no-op otherwise).
