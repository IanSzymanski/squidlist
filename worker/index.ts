import type { Env } from "./env";
import { health } from "./routes/health";

export default {
  async fetch(request: Request, env: Env): Promise<Response> {
    const path = new URL(request.url).pathname;
    if (path === "/api/health") {
      if (request.method !== "GET") {
        return Response.json({ error: "Method not allowed" }, { status: 405, headers: { Allow: "GET" } });
      }
      return health();
    }
    if (path === "/api" || path.startsWith("/api/")) {
      return Response.json({ error: "Not found" }, { status: 404 });
    }
    return env.ASSETS.fetch(request);
  },
} satisfies ExportedHandler<Env>;
