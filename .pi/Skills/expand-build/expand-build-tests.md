---
name: expand-build-tests
description: "Companion for .pi/extensions/expand-build/expand-build-tests.ts — the eXpand tree's /devexpress loader contract driven through pi's OWN runtime: one buildRealRunner({ entry }) build that hands the tree's own index.ts to pi's loader, what stays stubbed, the loud-refusal case, the resolve.mjs floor verdict, and the nochat boot this suite removed (with its measured numbers) plus why pi-dev's boot ledger cannot serve a project tree. Use when a case fails or when changing the loader, its command, or the suite's shape."
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
supplies. No budget header is needed any more: the suite spawns nothing (see
"The boot case (removed)").

## What stayed real

- **pi's own loader, on this tree's own file.** ONE build —
  `buildRealRunner({ entry: <this tree's index.ts>, cwd: <tree root> })` — hands
  the entry to pi's loader (`loadExtensions`, jiti plus pi's alias table), so the
  extension under test is the file on disk, imported the way pi imports it, not a
  factory the test imported itself. A build the loader cannot resolve rejects with
  pi's own reason (T5).
- **pi's own runtime for T1-T4.** The command is read back from and run through
  pi's `ExtensionRunner` registry: `commands()`, `runCommand`, and the resolved
  registration's handler invoked with the runner's own
  `createCommandContext()`. No hand-written pi exists in the file, and none may
  come back: the whole point of the migration was that the old `mkPi()` proved the
  fake's own dispatch.
- **The engine is real.** T4's answer is the RX menu's own abort text
  (`"DevExpress menu: aborted."`), reached through the lazy `import("./engine.js")`
  — the path that once died on `__filename is not defined`.
- **The tree's own sources.** Every path is derived from the fixture's
  `import.meta.url`, so a gate island loads and drives the island's copy (the
  queued content), not the working tree's.

## What is stubbed, and why

- **The UI pick, only.** `ui: { select: async () => undefined }` on the harness
  build is the menu's cancel. Nothing about rendering is asserted: an interactive
  TUI is outside this harness by doctrine, and the loader's contract ends at the
  text the handler returns. The host's own default (`select` answers the first
  option) would have walked into a flow, so cancelling is the honest stand-in for
  "the user pressed Escape".
- **Nothing else.** The session manager, the model registry, the event bus and the
  extension cache are pi's own, built by the harness.

## The boot case (removed) — and the ledger finding it left behind

Every case drives the harness now, so the nochat spawn this suite used to pay and
both timing budgets it carried are gone, and the load contract moved to the entry
route: pi's loader importing this tree's file IS the load (T1), and a refused
entry is loud (T5). The measured boot numbers are kept under History below,
because a future suite that wants a boot budget again starts from them.

An in-process boot is not available to replace the spawn, and no fake fills the
gap. pi-dev's shared ledger (`ensureBootProof`) is **install-rooted** and cannot
prove a project-local extension:

- its identity is `<agentDir>/extensions/<entry>` (`boot-proof-key.ts`), so the key
  only builds when the sources sit under the agent dir;
- its spawn always appends a `dependency-manager/index.ts` member resolved off the
  same base (`buildScoutCommand`, "Always load dependency-manager in spawned pi"),
  which a project tree does not have.

Measured on the committed tree, with `PI_RUNNER_AGENT_DIR` pointed at this tree
(exit 1, nothing recorded):

```
Error: Failed to load extension "C:\Work\expand\.pi\extensions\dependency-manager\index.ts":
Extension path does not exist: C:\Work\expand\.pi\extensions\dependency-manager\index.ts
verdict: {"ok":false,"hit":false,"status":1,...,"detail":"spawn did not prove a boot (exit status 1)…"}
```

The key itself does build for this tree
(`bp1|expand-build|expand-build/index.ts|0.84.2|<digest>`) — the closure digest is
agent-dir-relative, so island and working tree agree on it — but no row can ever be
recorded. Making the ledger serve a project tree is a change in pi-dev (skip the
platform member for a non-home base, or accept an absolute entry). This tree does
not own that code.

## History (removed boot)

The removed case was ONE canonical nochat spawn (`pi-runner.ts`, `timing: true`)
asserting exit 0, no `Failed to load extension`, the loader's per-extension load
line inside 1000ms (≈216ms measured on the committed tree), and startup inside
15000ms, re-based from a red 3000ms (5122ms parent / 5448ms worker baseline)
against 4142ms / 4398ms measured for that boot with the fleet's load on it.
Nothing in the suite asserts these numbers any more.

## The `@pi/` floor (resolve.mjs)

A test process is plain `node --import tsx`: nothing resolves `@pi/pi-dev/...`, so
the suite's first pi-dev import would be an ERR_MODULE_NOT_FOUND. The tree ships
`resolve.mjs`, which imports the loader's own table
(`~/.pi/agent/extensions/pi-dev/ext-ts-shared.ts`) and arms its `sharedRedirect` as
a host resolve hook. **Verdict: extend, not new.** The whitelist
(`shared-utilities.json`), the `x.js` → `x.ts` step and the `contracts` case have
one owner; `business/expenses/.pi/extensions/expenses/resolve.mjs` is the fleet's
precedent for the hook, and it carries a second copy of the table, which this file
deliberately does not repeat. The suite arms it as the first statement of its
driver — before the harness import, which is itself a `@pi/` name. The tree's own
entry is NOT imported by the test: pi's loader imports it.

## Case map

- **T1** — pi's loader imported this tree's entry, and `/devexpress` is registered
  on pi's own runner.
- **T2** — pi's own dispatch runs the registered command.
- **T3** — the engine's re-registration is the live one and keeps the boot
  registration's description.
- **T4** — an empty pick aborts with the menu's own text through the lazily loaded
  engine.
- **T5** — an entry pi's loader cannot build rejects the build with pi's own reason
  (the entry's path travels in the message).
- **extra** — pi's runner reported no handler error (a broken handler is a silent
  no-op otherwise).
