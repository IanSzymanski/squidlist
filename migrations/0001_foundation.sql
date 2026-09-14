-- P0 infrastructure only. Media/library tables are intentionally deferred to P2.
CREATE TABLE app_metadata (
  key TEXT PRIMARY KEY NOT NULL,
  value TEXT NOT NULL
);
INSERT INTO app_metadata (key, value) VALUES ('foundation_version', '1');
