---
name: expand-build
description: Use when working on or invoking /devexpress in the eXpand repo. Thin loader that imports the Reactive.XAF engine with expandProfile — no pane, watcher, or azdo copy.
---

# expand-build

Project-local extension at `D:/expand/.pi/extensions/expand-build/`.
Pi auto-discovers `cwd/.pi/extensions`, so this only loads in the eXpand
tree. Skill lives at `D:/expand/.pi/Skills/expand-build/SKILL.md`.

`activate` registers `/devexpress` only. The first command jiti-loads
`C:/Work/Reactive.XAF/.pi/extensions/reactive-xaf-build/menu.ts` — the
composition root that owns `registerBuildCommand` — and takes
`expandProfile` from that tree's `profile.ts`, attaching
`{ profile: expandProfile }`.
`expandProfile.detect(cwd)` matches `Directory.Packages.props` +
`Xpand/Xpand.ExpressApp.Modules`. The engine, pane, watcher, and azdo
scripts stay in Reactive.XAF.

Both modules are ESM: the loader anchors itself with `import.meta.url`, never
`__filename`/`__dirname`/`require`. The native TypeScript loader injects those
CommonJS globals per file and only for paths matched against an extension root,
and this tree is reached through a link (pane `C:\Work\expand`, real
`D:\expand`) — an unmatched file loses them and the command dies with
`__filename is not defined`.

## Expand flow (via the shared engine)

DX pins → RX depPins (`Xpand.Extensions*` / `Xpand.XAF.*` from the matching
feed) → `bx lab` / `bx Release` in a pane → commit (required) → `git push`
to `lab` or `eXpand` → `px` / `px -Release` → watch 32/39 → 38 → 37.

- Lab GitHub `eXpand.lab`: assert published (do not PATCH).
- Release GitHub `eXpand`: publish the draft.
- Version file is written by the local `bx` build. We never edit
  `XpandAssemblyInfo.cs`. `build.ps1` is the version we bump — the Release
  flow bumps it to the next version after the last published on the feeds
  (Xpand server + nuget.org), the Lab flow to the DX base.

See `reactive-xaf-build/profile.md` for the profile fields.

## Tests (`expand-build-tests.ts`)

Run: `run_tests(name="expand-build", fixture="expand-build-tests.ts")` — never the
file directly (the shell gate blocks it, and the fixture needs the runner's
typeScript loader and this tree's `resolve.mjs` floor). Details and numbers:
`expand-build-tests.md`.

The suite drives pi's own runtime: `activate` is built by pi's loader and
dispatched through pi's `ExtensionRunner` (shared harness, `pi-dev/real-runner.ts`),
and the boot is ONE real nochat spawn through pi-dev's canonical runner with the
project entry absolute and `cwd` at this tree. The pick is cancelled through the
harness `ui` override; no rendering is asserted.

- T1 — this tree's current sources boot a real pi (exit 0).
- T2 — `/devexpress` is registered, read back from pi's own runner.
- T3 — pi's own dispatch runs the command; an empty pick aborts with the menu's
own text through the lazily loaded engine.
- T4 — the engine's re-registration is the live one and keeps the boot
description.
- T5 — no Failed to load extension, the loader reports this extension's load
line, and that load stays inside the 1000ms registered budget.
- T6 — the startup budget, kept and re-based (3000ms was red: 5122/5448ms
measured under fleet load); the case prints its own measurement every run.

pi-dev's boot ledger cannot serve a project tree (its identity is
`<agentDir>/extensions/<entry>` and its spawn always appends a platform member
off that base), so the boot is paid per run instead of being served from the
ledger — no fake fills the gap. See `expand-build-tests.md` for the probe.
