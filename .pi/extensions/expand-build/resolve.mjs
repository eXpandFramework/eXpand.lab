/**
 * resolve.mjs — the `@pi/` name floor for a plain-node host, in this tree.
 *
 * The eXpand tree is not a sibling of the pi agent dir, so nothing under it can
 * form a relative path to a shared extension: its code writes the NAME
 * (`@pi/pi-dev/real-runner.js`). Under pi the native loader's ext-ts-shared
 * answers that name. A test process the runner starts is plain
 * `node --import tsx`, where nothing does, and the suite's first pi-dev import
 * would die with ERR_MODULE_NOT_FOUND.
 *
 * The TABLE is not repeated here. This file imports the loader's own
 * `agent/extensions/pi-dev/ext-ts-shared.ts` and arms its `sharedRedirect` as a
 * host resolve hook, so the whitelist
 * (`extensions/shared-utilities.json`), the `x.js` -> `x.ts` step and the
 * `contracts` case stay the ONE definition the pi loader also uses.
 * `business/expenses/.pi/extensions/expenses/resolve.mjs` is the fleet's
 * precedent for the hook shape; it carries its own copy of the table, which
 * this file deliberately does not repeat (doc: skills/pi-dev/ext-ts-shared.md).
 *
 * The agent dir is located the way every project tree locates it: `~/.pi/agent`.
 * A missing table is a loud ERR_MODULE_NOT_FOUND, never a silent fallback.
 *
 * Use it BEFORE the modules that need it:
 *   - in a suite: `await import(new URL("./resolve.mjs", import.meta.url).href)`
 *     as the driver's first statement, then import the pi-dev modules;
 *   - in a plain-node suite: `node --import ./.pi/extensions/expand-build/resolve.mjs <suite>`.
 *
 * A `.mjs` host imports the table's `.ts`, so the hook needs a TypeScript loader
 * (`--import tsx`, what the test-runner already uses). Under plain node the
 * table's own import is the failure, and it is loud.
 */
import { registerHooks } from "node:module";
import { homedir } from "node:os";
import { join } from "node:path";
import { pathToFileURL } from "node:url";

/** The loader's own table, at its canonical place in the agent tree. */
const TABLE = join(homedir(), ".pi", "agent", "extensions", "pi-dev", "ext-ts-shared.ts");

const { sharedRedirect } = await import(pathToFileURL(TABLE).href);

registerHooks({
  resolve(specifier, context, nextResolve) {
    if (specifier.startsWith("@pi/")) {
      const target = sharedRedirect(specifier);
      if (target) return nextResolve(pathToFileURL(target).href, context);
    }
    return nextResolve(specifier, context);
  },
});
