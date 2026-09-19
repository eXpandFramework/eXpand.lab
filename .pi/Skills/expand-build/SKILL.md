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

Run: `npx tsx C:/Work/expand/.pi/extensions/expand-build/expand-build-tests.ts`

- startup speed — nochat spawn with `cwd: C:/Work/expand`, `agentDir: C:/Work/expand/.pi`, `exts: ["expand-build/index.ts"]`.
- E1 — spawn does not print Failed to load extension.
- E2 — activate registers `/devexpress`.
- E3 — invoking the command reaches the engine (the menu's own abort text, not a load error).
- E4 — the stub's description equals the engine's own registration.
