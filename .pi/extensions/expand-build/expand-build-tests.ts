// test-timeout: 120000 — a cold ledger key pays one real boot of this tree.
/**
 * expand-build-tests — behavior contract for the eXpand tree's /devexpress loader.
 *
 * Runtime exercise: pi's OWN loader and runner, in-process. ONE
 * `buildRealRunner({ entry })` build hands this tree's own `index.ts` to
 * pi's loader (jiti plus pi's alias table), so the extension under test is the
 * file on disk, imported the way pi imports it at boot, and pi's own
 * ExtensionRunner dispatches. No hand-written pi exists here, and this file
 * HOLDS NO SPAWN: the nochat spawn it used to pay, and the two timing budgets
 * it carried, are gone. The boot itself is proven through pi-dev's shared
 * ledger (T6), which pays one real boot for a cold key and serves every later
 * run from the record; the measured numbers the old case asserted stay in
 * skills/expand-build/expand-build-tests.md.
 *
 * The load contract is carried by the entry route: pi's loader importing THIS
 * tree's file IS the load, and pi's loader refusing one is loud (T5). Until
 * 2026-10-08 the ledger could not prove this tree: its spawn always appended a
 * `dependency-manager` member resolved off the base, which a project tree does
 * not carry, and the base spelling had to be canonical (both fixed in pi-dev —
 * commits 126b8f84 and cd98454a). Nothing fake filled that gap in the meantime,
 * and the probe's measured failure text stays in the companion doc.
 *
 * The plain-tsx host cannot resolve `@pi/` names, so the tree's own floor
 * (resolve.mjs) is armed BEFORE the harness import: the harness module itself is
 * a `@pi/` name. The tree's entry is not imported by this file — pi's loader
 * imports it. Nothing else about the tree is stubbed.
 *
 * Run: run_tests(name="expand-build", fixture="expand-build-tests.ts")
 */
/* oxlint-disable no-console -- test harness prints PASS/FAIL to stdout */
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

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

/** This fixture's own tree, so the suite proves the sources it lives in: the
 *  entry handed to pi's loader is derived from this file's location, and a gate
 *  island loads the island's copy (the queued content). */
const EXT_DIR = dirname(fileURLToPath(import.meta.url));
const TREE_ROOT = dirname(dirname(dirname(EXT_DIR)));
const ENTRY = join(EXT_DIR, "index.ts");

/** The boot registration: captured before ANY dispatch, because the engine
 *  re-registers the command on first use (T3 compares against it). */
interface BootRegistration {
  handler: unknown;
  description: string;
}

/** The registration pi's own registry holds right now. */
function registrationOf(handle: any): any {
  return handle.runner.getCommand("devexpress");
}

/** T1 — pi's loader imported this tree's entry from disk, so the tree's own
 *  sources are what pi built; and the command pi registered is on its runner. */
function caseLoad(handle: any, boot: BootRegistration): void {
  const cmd = registrationOf(handle);
  boot.handler = cmd?.handler;
  boot.description = String(cmd?.description ?? "");
  assert(
    "T1: pi's loader imported this tree's entry, and /devexpress is registered on pi's own runner",
    handle.commands().includes("devexpress") && typeof cmd?.handler === "function",
    JSON.stringify(handle.commands()),
  );
}

/** T2 — pi's own registry resolves the command and runs the handler it resolved. */
async function caseDispatch(handle: any): Promise<void> {
  const reached = await handle.runCommand("devexpress");
  assert("T2: pi's own dispatch runs the registered command", reached === true, String(reached));
}

/** T3 — the engine's re-registration is the live one: a different handler object
 *  carrying the boot registration's own text. */
function caseReRegistration(handle: any, boot: BootRegistration): void {
  const live = registrationOf(handle);
  assert(
    "T3: the engine's own registration is the live one",
    live?.handler !== boot.handler,
    "the boot registration is still the resolved one",
  );
  assert(
    "T3: the re-registration keeps the boot description",
    String(live?.description ?? "") === boot.description,
    `${boot.description} | ${String(live?.description ?? "")}`.slice(0, 220),
  );
}

/** T4 — the lazily loaded engine's own abort text, on the runner's own command
 *  ctx, with the pick cancelled through the harness `ui` override. */
async function caseMenuAbort(handle: any): Promise<void> {
  const live = registrationOf(handle);
  const answer = await live.handler([], handle.runner.createCommandContext());
  assert(
    "T4: an empty pick aborts with the menu's own text",
    answer === "DevExpress menu: aborted.",
    String(answer),
  );
}

/** extra — a handler that throws is a silent no-op unless the report is read. */
function caseNoHandlerError(handle: any): void {
  assert(
    "no handler error was reported by pi's runner",
    handle.host.errors.length === 0,
    JSON.stringify(handle.host.errors).slice(0, 220),
  );
}

/** T5 — an entry pi's loader cannot build is refused LOUDLY: the build rejects
 *  with pi's own reason and names the entry, instead of handing back a runner
 *  with no extension in it. */
async function caseLoadFailure(buildRealRunner: (options: any) => Promise<any>): Promise<void> {
  const missing = join(EXT_DIR, "no-such-entry.ts");
  let message = "";
  try {
    const handle = await buildRealRunner({ entry: missing, cwd: TREE_ROOT });
    handle.dispose();
  } catch (err) {
    message = String((err as Error)?.message ?? err);
  }
  assert(
    "T5: an entry pi's loader cannot build rejects the build with pi's own reason",
    message.includes("no-such-entry"),
    message.slice(0, 240) || "the build resolved a missing entry",
  );
}

/** T6 — the tree's own sources are PROVEN to boot by pi-dev's ledger: a cold
 *  key pays ONE real boot through pi's canonical runner and records it, a warm
 *  key is served with nothing spawned. Either way this file holds no spawn. */
function caseBootProof(proof: any): void {
  assert(
    "T6: the tree's sources are proven to boot by pi-dev's ledger",
    proof?.ok === true,
    JSON.stringify(proof ?? {}).slice(0, 240),
  );
}

(async () => {
  // Section: the floor — @pi/ names resolved for a plain-tsx host, armed BEFORE
  // the harness import, which is itself a @pi/ name.
  await import(new URL("./resolve.mjs", import.meta.url).href);
  const { buildRealRunner } = await import("@pi/pi-dev/real-runner.js");
  const { ensureBootProof } = await import("@pi/pi-dev/boot-proof.js");

  // Section: T1-T4 + the handler-error case — one loader build of this tree
  const boot: BootRegistration = { handler: undefined, description: "" };
  const handle = await buildRealRunner({
    entry: ENTRY,
    cwd: TREE_ROOT,
    ui: { select: async () => undefined },
  });
  try {
    caseLoad(handle, boot);
    await caseDispatch(handle);
    caseReRegistration(handle, boot);
    await caseMenuAbort(handle);
    caseNoHandlerError(handle);
  } finally {
    handle.dispose();
  }

  // Section: T6 — the tree's boot proof, served from pi-dev's ledger
  const proof = ensureBootProof("expand-build", "expand-build/index.ts", {
    agentDir: join(TREE_ROOT, ".pi"),
    timeoutSec: 120,
  });
  caseBootProof(proof);

  // Section: T5 — a refused entry is loud, with pi's own reason
  await caseLoadFailure(buildRealRunner);

  console.log(`\n${ok} passed, ${fail} failed`);
  process.exit(fail > 0 ? 1 : 0);
})();
