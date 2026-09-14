import assert from "node:assert/strict";

// Run against an already-started pnpm dev or pnpm preview server.
const base = process.env.SQUIDLIST_TEST_URL ?? "http://127.0.0.1:5173";
const health = await fetch(new URL("/api/health", base));
assert.equal(health.status, 200);
assert.equal(health.headers.get("cache-control"), "no-store");
assert.deepEqual(await health.json(), { status: "ok", service: "squidlist" });
const missing = await fetch(new URL("/api/missing", base));
assert.equal(missing.status, 404);
assert.deepEqual(await missing.json(), { error: "Not found" });
const method = await fetch(new URL("/api/health", base), { method: "POST" });
assert.equal(method.status, 405);
assert.equal(method.headers.get("allow"), "GET");
for (const path of ["/", "/library"]) {
  const page = await fetch(new URL(path, base));
  assert.equal(page.status, 200);
  assert.match(page.headers.get("content-type"), /text\/html/);
  assert.match(await page.text(), /<div id="root"><\/div>/);
}
console.log("Runtime smoke passed: health, method rejection, API 404, frontend and SPA fallback.");
