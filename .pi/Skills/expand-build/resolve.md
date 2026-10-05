---
name: expand-build/resolve
description: "Use when the eXpand tree's fixtures or plain-node suites cannot resolve `@pi/...` names, when the shared-utilities whitelist or the loader's name rules change, or when a host the pi loader does not drive has to be given the tree's own name resolution. Companion of .pi/extensions/expand-build/resolve.mjs."
---

# resolve.mjs — the `@pi/` name floor for a plain host

Module: `.pi/extensions/expand-build/resolve.mjs`. Consumer:
`expand-build-tests.ts` (the only one today).

## When it matters

A test process the runner starts is plain `node --import tsx`: pi's native
loader is not there, so nothing answers a name like `@pi/pi-dev/real-runner.js`
and the suite dies at its first pi-dev import with `ERR_MODULE_NOT_FOUND`. The
eXpand tree cannot form a relative path to the agent tree, so the name is the
only spelling its code has — this file is what makes that name load.

## What it does

- Imports the loader's own table,
  `~/.pi/agent/extensions/pi-dev/ext-ts-shared.ts`, and arms its
  `sharedRedirect` as a host hook (`module.registerHooks({ resolve })`),
  rewriting any `@pi/` specifier to the file URL the table returns.
- Nothing else. No `.js` → `.ts` rule of its own (tsx already rewrites relative
  specifiers), no whitelist of its own, no second table.

## Why the table is not copied here

The whitelist (`extensions/shared-utilities.json`), the `.js` → `.ts` step and
the `contracts` case have one owner — the loader's `ext-ts-shared.ts`. A copy
drifts the moment the loader's rules change, and the drift is silent.
`business/expenses/.pi/extensions/expenses/resolve.mjs` is the fleet's precedent
for the hook shape and carries its own copy of the table; this tree
deliberately does not. Doc: `skills/pi-dev/ext-ts-shared.md`.

## How to arm it

- **A `.ts` suite.** First statement of the driver:

  ```ts
  await import(new URL("./resolve.mjs", import.meta.url).href);
  ```

  Then import the tree's entry and the pi-dev modules **dynamically**, so
  everything the suite exercises loads inside the domain the hook defines. A
  static import is hoisted and evaluates before the hook exists — that shape
  protects only the imports written after it, and the tree's own graph would
  load unprotected.

- **A plain-node `.mjs` suite.**

  ```
  node --import ./.pi/extensions/expand-build/resolve.mjs <suite>
  ```

## Preconditions and failure modes

- The table is a `.ts`, so the host needs a TypeScript loader (`--import tsx`,
  what the test-runner already uses). Under plain node the table's import fails
  loudly with `ERR_MODULE_NOT_FOUND` — there is no fallback and there must not
  be one: a host that cannot read the table cannot resolve names honestly.
- A renamed or missing `ext-ts-shared.ts` fails the same loud way. The agent dir
  is located as `~/.pi/agent`, the convention every project tree uses.
- The whitelist stays the loader's: a name that is not shared does not resolve
  here either, so this file is not a hole into the agent tree.
- This is a **host-side** hook only. Under pi, the native loader's
  `ext-ts-shared` answers the same names and this file is never loaded.
