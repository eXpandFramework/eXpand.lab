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
typeScript loader and this tree's `resolve.mjs` floor). Details:
`expand-build-tests.md`; the floor itself: `resolve.md`.

One `buildRealRunner({ entry })` build hands this tree's own `index.ts` to pi's
loader, so the extension under test is the file on disk and pi's own
`ExtensionRunner` dispatches T1 to T5. The BOOT is proven through pi-dev's shared
ledger (T6): a cold key pays one real boot of this tree and records it, every
later run is served that record, and the fixture holds no spawn of its own. The
pick is cancelled through the harness `ui` override; no rendering is asserted.

- T1 — pi's loader imported this tree's entry and `/devexpress` is registered on
pi's own runner.
- T2 — pi's own dispatch runs the command.
- T3 — the engine's re-registration is the live one and keeps the boot
description.
- T4 — an empty pick aborts with the menu's own text through the lazily loaded
engine.
- T5 — an entry pi's loader cannot build is refused loudly, with pi's own reason
(the entry's path is in the message).
- T6 — the tree's sources are proven to boot by pi-dev's ledger (one real boot
when the key is cold, none when it is warm).
- extra — pi's runner reported no handler error.

pi-dev's ledger could not serve a project tree until 2026-10-08 (its spawn always
appended a platform member off the base, and the base had to be canonical); both
are fixed there — commits `126b8f84` and `cd98454a`. See `expand-build-tests.md`
for the probe text and the boot numbers the old case asserted.
