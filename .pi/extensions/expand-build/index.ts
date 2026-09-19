/**
 * expand-build — project-local /devexpress loader for eXpand.
 * Boot only registers the command. The RX engine loads on first use.
 */

import type { attachEngine } from "./engine.js";

export default function (pi: any): void {
  let handler: ((args: unknown, ctx: unknown) => Promise<unknown>) | undefined;
  pi.registerCommand("devexpress", {
    // The engine re-registers this command on first use; the E4 contract pins
    // the two registrations together, so keep this text equal to menu.ts's.
    description:
      "DevExpress menu: Build | Publish | Last build status | Cancel AzDO build | Start AzDO watcher → RX-XAF | eXpand → Lab | Release",
    handler: async (args: unknown, ctx: unknown) => {
      if (!handler) {
        const spec = "./engine.js";
        const mod = await import(spec) as { attachEngine: typeof attachEngine };
        handler = mod.attachEngine(pi);
      }
      return handler(args, ctx);
    },
  });
}
