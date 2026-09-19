/**
 * expand-build/engine — jiti-loads the Reactive.XAF /devexpress engine.
 * Imported only from the command handler, never at boot.
 *
 * ESM module, so it must not reach for `__filename`, `__dirname` or
 * `require`: the native TypeScript loader injects those CommonJS globals per
 * file, and only for paths matched against an extension root — the eXpand
 * tree is reached through a link (pane C:\Work\expand, real D:\expand), so an
 * unmatched file loses them and the command dies with "__filename is not
 * defined". `import.meta.url` is the ESM spelling.
 */

import { createRequire } from "node:module";
import * as fs from "node:fs";
import * as path from "node:path";
import { fileURLToPath } from "node:url";

const RX = "C:/Work/Reactive.XAF/.pi/extensions/reactive-xaf-build";

function requireFromPi(): NodeRequire {
  const piPkg = path.join(
    path.dirname(process.execPath),
    "node_modules",
    "@earendil-works",
    "pi-coding-agent",
    "package.json",
  );
  if (!fs.existsSync(piPkg)) {
    throw new Error("expand-build: pi-coding-agent not next to node.exe at " + piPkg);
  }
  return createRequire(piPkg);
}

function loadEngine(): {
  expandProfile: unknown;
  registerBuildCommand: (pi: unknown, seams?: unknown) => void;
} {
  const fileOf = (name: string) => {
    const file = path.join(RX, name + ".ts");
    if (!fs.existsSync(file)) throw new Error("expand-build: missing " + file);
    return file;
  };
  const { createJiti } = requireFromPi()("jiti") as {
    createJiti: (id: string, opts?: object) => (id: string) => any;
  };
  // The base id only anchors relative specifiers; every target below is an
  // absolute path, so any real file serves as the anchor.
  const jiti = createJiti(fileURLToPath(import.meta.url), { moduleCache: false });
  // registerBuildCommand lives in the menu — the composition root that owns
  // the command — not in build.ts, which is the engine the menu drives.
  return {
    expandProfile: jiti(fileOf("profile")).expandProfile,
    registerBuildCommand: jiti(fileOf("menu")).registerBuildCommand,
  };
}

export function attachEngine(pi: any): (args: unknown, ctx: unknown) => Promise<unknown> {
  let captured: { handler?: (args: unknown, ctx: unknown) => Promise<unknown> } | undefined;
  const orig = pi.registerCommand.bind(pi);
  pi.registerCommand = (name: string, def: unknown) => {
    if (name === "devexpress") {
      captured = def as { handler?: (args: unknown, ctx: unknown) => Promise<unknown> };
    }
    return orig(name, def);
  };
  const engine = loadEngine();
  engine.registerBuildCommand(pi, { profile: engine.expandProfile });
  pi.registerCommand = orig;
  if (!captured?.handler) throw new Error("expand-build: registerBuildCommand did not register");
  return captured.handler;
}
