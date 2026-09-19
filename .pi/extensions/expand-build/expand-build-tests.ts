/**
 * expand-build-tests — project-local /devexpress loader for eXpand.
 * Run: npx tsx C:/Work/expand/.pi/extensions/expand-build/expand-build-tests.ts
 */
/* oxlint-disable no-console -- test harness prints PASS/FAIL to stdout */
import { spawnSync } from "node:child_process";
import { existsSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import activate from "./index.js";

let ok = 0;
let fail = 0;
function assert(label: string, cond: boolean, detail?: string): void {
  if (cond) {
    ok++;
    console.log("PASS " + label);
  } else {
    fail++;
    console.log("FAIL " + label + (detail ? " — " + detail : ""));
  }
}
function mkPi(): any {
  const cmds = new Map<string, any>();
  const regs: Array<{ name: string; description?: string }> = [];
  return {
    registerCommand: (n: string, d: any) => {
      regs.push({ name: n, description: d?.description });
      cmds.set(n, d);
    },
    _cmds: cmds,
    _regs: regs,
  };
}
function resolvePiCli(): string {
  const pathVar = process.env.PATH ?? "";
  for (const dir of pathVar.split(";")) {
    if (!dir) continue;
    const cli = join(dir, "node_modules", "@earendil-works", "pi-coding-agent", "dist", "cli.js");
    if (existsSync(cli)) return cli;
  }
  throw new Error("expand-build-tests: pi CLI not found on PATH");
}
function parseBootTimings(text: string): Record<string, number> {
  const out: Record<string, number> = {};
  const re = /extensions[\\/]([^\\/]+)[\\/](?:[^\\/:]+\.ts) (module import|factory): (\d+)ms/g;
  let m: RegExpExecArray | null;
  while ((m = re.exec(text))) {
    out[m[1]] = (out[m[1]] ?? 0) + Number(m[3]);
  }
  return out;
}
const runPi = (opts: {
  prompt: string;
  exts: string[];
  timeoutSec?: number;
  timing?: boolean;
}): { text: string; timings: Record<string, number> } => {
  const env: Record<string, string> = { ...process.env as Record<string, string> };
  if (opts.timing) env.PI_TIMING = "1";
  delete env.PI_SESSION_ID;
  delete env.TMUX;
  delete env.TMUX_PANE;
  const extPath = join(__dirname, opts.exts[0].split("/").pop() ?? "index.ts");
  const r = spawnSync("node", [
    resolvePiCli(),
    "-ne",
    "-e", extPath,
    "--mode", "json",
    "-p", opts.prompt,
    "--session-dir", join(tmpdir(), "pi-xpand-" + Date.now()),
  ], {
    encoding: "utf-8",
    timeout: (opts.timeoutSec ?? 30) * 1000,
    cwd: join(__dirname, "..", "..", ".."),
    env,
    stdio: ["ignore", "pipe", "pipe"],
    shell: false,
  });
  const text = ((r.stdout || "") + "\n" + (r.stderr || "")).trim();
  return { text, timings: parseBootTimings(text) };
};

(async () => {
  // Section: startup speed
  const start = Date.now();
  const boot = runPi({
    prompt: "",
    exts: ["expand-build/index.ts"],
    timeoutSec: 30,
    timing: true,
  });
  const elapsed = Date.now() - start;
  assert("startup under 3000ms", elapsed < 3000, `took ${elapsed}ms`);
  assert("boot load under 1000ms (registered budget)", boot.timings["expand-build"] < 1000, `load: ${boot.timings["expand-build"]}ms`);
  // Section: E1 — pi in the eXpand tree loads the engine
  assert(
    "E1: no Failed to load extension",
    !boot.text.includes("Failed to load extension"),
    boot.text.slice(0, 400),
  );
  // Section: E2 — activate registers /devexpress
  {
    const pi = mkPi();
    activate(pi);
    assert("E2: /devexpress registered", typeof pi._cmds.get("devexpress")?.handler === "function");
  }
  // Section: E3 — invoking /devexpress reaches the engine. The stub's handler
  // lazily imports ./engine.js, attaches the RX engine and delegates to the
  // command that call registers; an empty pick aborts with the menu's own
  // text. Before the fix this died with "__filename is not defined" (an ESM
  // module reaching for a CommonJS global) and, behind it, with a
  // registerBuildCommand that no longer exists on the engine module.
  {
    const pi = mkPi();
    activate(pi);
    const ctx = {
      cwd: join(__dirname, "..", "..", ".."),
      ui: { select: async () => undefined, notify: async () => {} },
    };
    const answer = await pi._cmds.get("devexpress").handler([], ctx);
    assert("E3: the engine answers through the menu", answer === "DevExpress menu: aborted.", String(answer));
    const regs = (pi._regs as Array<{ name: string; description?: string }>)
      .filter((r) => r.name === "devexpress");
    assert("E4: the stub carries the engine's own description",
      regs.length === 2 && regs[0].description === regs[1].description,
      regs.map((r) => String(r.description)).join(" | ").slice(0, 220));
  }
  console.log(`\n${ok} passed, ${fail} failed`);
  process.exit(fail > 0 ? 1 : 0);
})();
