---
name: expand-build-tests
description: "Companion for .pi/extensions/expand-build/expand-build-tests.ts — the eXpand tree's /devexpress loader contract driven through pi's OWN runtime: one buildRealRunner({ entry }) build that hands the tree's own index.ts to pi's loader, what stays stubbed, the loud-refusal case, the resolve.mjs floor verdict, and the BOOT, proven through pi-dev's ledger with the nochat spawn this suite used to pay kept as history. Use when a case fails or when changing the loader, its command, or the suite's shape."
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
the ledger proof in T6 pays one real boot when the key is cold.

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

## The boot, proven through pi-dev's ledger

T6 asks pi-dev's shared ledger for this tree's boot proof: a cold key pays ONE
real boot through pi's canonical runner and records it, and every later run is
served that record with nothing spawned. The fixture holds no spawn of its own,
and the load contract stays on the entry route — pi's loader importing this
tree's file IS the load (T1), with a refused entry loud (T5).

**What used to block it, measured on this tree before the fix** (kept because the
failure text is the reason the machinery looks the way it does):

- the spawn always appended a `dependency-manager/index.ts` member resolved off
  the base (`buildScoutCommand`, "Always load dependency-manager in spawned pi"),
  which a project tree does not carry — the boot then died with the exit-1 error
  below and recorded nothing;
- the base had to be spelled canonically: a base reached through the pane link
  (`C:\Work\expand`, real `D:\expand`) booted fine and then reported
  `booted, NOT recorded: openDb(...): path mismatch`, because the sqlite registry
  refuses to open one file under two spellings.

```
Error: Failed to load extension "C:\Work\expand\.pi\extensions\dependency-manager\index.ts":
Extension path does not exist: C:\Work\expand\.pi\extensions\dependency-manager\index.ts
verdict: {"ok":false,"hit":false,"status":1,...,"detail":"spawn did not prove a boot (exit status 1)…"}
```

Both are fixed in pi-dev today: the member is appended only where the base carries
it (`withDependencyManager`), and the ledger canonicalizes its base
(`canonicalDirPath`) — commits `126b8f84` and `cd98454a`. The key was never the
problem (`bp1|expand-build|expand-build/index.ts|0.84.2|<digest>` built for this
tree all along, because the closure digest is agent-dir-relative).

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
- **T6** — the tree's sources are proven to boot by pi-dev's ledger: a cold key
  pays one real boot, a warm one is served with nothing spawned, and this file
  holds no spawn either way.
- **extra** — pi's runner reported no handler error (a broken handler is a silent
  no-op otherwise).
