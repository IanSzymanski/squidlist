# D1 migrations

Use numbered, forward-only SQL files. Wrangler maintains its own migration ledger.
Run `pnpm db:migrate:local` before local database development.
The first migration validates the D1 foundation without implementing the P2 library schema.

Remote provisioning is manual: `pnpm exec wrangler d1 create squidlist`.
Copy the returned database UUID into wrangler.jsonc, authenticate Wrangler, then
run `pnpm db:migrate:remote` when ready to modify the remote database.
Never reuse the zero placeholder UUID for production.
