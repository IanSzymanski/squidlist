# Squidlist

Personal music library and playback app. **P0 foundation only.** No player, Drive authentication, streaming, offline storage, or polished UI has been implemented.

## Development

Requires Node.js 22.12+ and pnpm (the lockfile records exact dependencies).

```sh
pnpm install
pnpm db:migrate:local
pnpm dev
```

Open the URL printed by Vite. React and the API share an origin. The Cloudflare Vite plugin runs the Worker locally in workerd with a local D1 binding.

```sh
pnpm typecheck
pnpm test
pnpm build
pnpm preview
```

With the dev server running, run `pnpm test:smoke` to verify the real Worker
health endpoint, API errors and frontend fallback. Set `SQUIDLIST_TEST_URL`
to test another local port (Wrangler preview normally uses 8787).

`GET /api/health` returns `{"status":"ok","service":"squidlist"}`. This is liveness, not a database/Drive readiness check. Other API routes return JSON 404; unsupported health methods return 405.

## Architecture

- `src/domain`: shared, platform-independent media, artist, album, playlist and queue types. Audio/video form a discriminated union. Duration and source offsets use seconds. Storage provider identity is extensible data; credentials never enter these models.
- `src/app`, `src/components`: presentation. Platform integrations belong behind services, never inside React.
- `worker/storage`: server-only MediaStorage interface and deliberately failing GoogleDriveStorage placeholder. Streaming contracts preserve Range status/headers and stream bodies without buffering entire files.
- `worker/db`, `migrations`: D1 persistence. P0 creates only app_metadata; P2 defines the library schema.
- `src/audio`: contracts for AudioEngine and QueueManager. Queue selection is separate from media playback. Future dual channels, gains and crossfading are documented, not implemented.
- `src/media`: resolver and download contracts. Future resolver chooses a valid completed OPFS file or Worker stream endpoint, with explicit URL cleanup.
- `src/db`: future IndexedDB download metadata adapter. OPFS holds media bytes; a Service Worker will cache shell/static assets, not act as the primary audio store.
- `public`: reserved for P7 assets. No Service Worker registration or installable PWA yet.

Browser and Worker TypeScript configurations use separate platform types. Tests directly exercise Worker routing and storage failure behavior in Node; production runtime checks can run through Wrangler. The initial structure deliberately omits empty route implementations for library/playlists/media.

The sequential queue is one future orchestration mechanism, not the fundamental media model. A future timeline can schedule clips with source offsets independently; no mix timeline data or scheduler is implemented in P0.

## D1 and deployment

The all-zero D1 UUID in wrangler.jsonc is a **local-only placeholder**. No cloud resource has been provisioned.

1. Authenticate: `pnpm exec wrangler login`.
2. Create D1: `pnpm exec wrangler d1 create squidlist`.
3. Replace database_id with the returned UUID.
4. Apply remote migrations deliberately: `pnpm db:migrate:remote`.
5. When ready for deployment (P9), run `pnpm deploy`.

Vite generates the production Worker/assets configuration in dist and Wrangler discovers it through the plugin's generated configuration. Do not commit build output or local D1 state.

## Decisions before P1

- Choose single-owner Drive authorization versus multi-user OAuth, and define app/API access control before exposing private media.
- Select the Google Cloud project, OAuth client/redirect URIs, least-privilege Drive scopes, and credential/token storage and refresh strategy on the server.
- Provision the intended Cloudflare account and D1 database before remote development/deployment.
- Define the initial media-ID-to-Drive-file lookup for the streaming proof of concept; the full importer/library schema remains P2.
- Validate Drive Range behavior, 206/416 propagation, seeking, MIME types, token expiry and quota/error handling using real test media.
- Keep credentials in Worker secrets or an appropriately protected server-side store. Never use VITE_ variables for secrets.

See [TASKS.md](TASKS.md) for the full MVP backlog.
