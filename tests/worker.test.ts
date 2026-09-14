import { describe, expect, it, vi } from "vitest";
import worker from "../worker/index";
import type { Env } from "../worker/env";

function environment() {
  const fetch = vi.fn(async () => new Response("app shell"));
  return { env: { ASSETS: { fetch } } as unknown as Env, fetch };
}
describe("Worker routing", () => {
  it("returns uncached liveness without database or Drive access", async () => {
    const { env, fetch } = environment();
    const response = await worker.fetch(new Request("https://squidlist.test/api/health"), env);
    expect(response.status).toBe(200);
    expect(response.headers.get("cache-control")).toBe("no-store");
    expect(await response.json()).toEqual({ status: "ok", service: "squidlist" });
    expect(fetch).not.toHaveBeenCalled();
  });
  it("rejects unsupported health methods", async () => {
    const { env } = environment();
    const response = await worker.fetch(new Request("https://squidlist.test/api/health", { method: "POST" }), env);
    expect(response.status).toBe(405);
    expect(response.headers.get("allow")).toBe("GET");
  });
  it.each(["/api", "/api/missing", "/api/media/example/stream"])("keeps %s out of SPA fallback", async (path) => {
    const { env, fetch } = environment();
    const response = await worker.fetch(new Request("https://squidlist.test" + path), env);
    expect(response.status).toBe(404);
    expect(await response.json()).toEqual({ error: "Not found" });
    expect(fetch).not.toHaveBeenCalled();
  });
  it("delegates frontend routes to assets", async () => {
    const { env, fetch } = environment();
    const response = await worker.fetch(new Request("https://squidlist.test/library"), env);
    expect(await response.text()).toBe("app shell");
    expect(fetch).toHaveBeenCalledOnce();
  });
});
