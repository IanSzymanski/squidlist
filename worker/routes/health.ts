/** Liveness only: this does not claim D1 or Drive are ready. */
export function health(): Response {
  return Response.json({ status: "ok", service: "squidlist" }, {
    headers: { "Cache-Control": "no-store" },
  });
}
