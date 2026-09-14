# Squidlist tasks

Current scope: P0 only. Checkboxes track completed work, not future contracts.

## P0 — Foundation

- [x] P0-01 Scaffold React + Vite + TypeScript
- [x] P0-02 Configure Cloudflare Worker and Wrangler
- [x] P0-03 Implement GET /api/health
- [x] P0-04 Configure Cloudflare D1
- [x] P0-05 Create initial D1 migration structure
- [x] P0-06 Define shared domain models
- [x] P0-07 Define MediaStorage interface
- [x] P0-08 Create placeholder GoogleDriveStorage implementation
- [x] P0-09 Establish repository/module structure
- [x] P0-10 Configure TypeScript/typechecking/testing baseline

## P1 — Google Drive Streaming

- [ ] Google Drive authentication
- [ ] Drive metadata lookup
- [ ] Media streaming endpoint
- [ ] HTTP Range support
- [ ] Browser playback proof of concept
- [ ] Seeking validation

## P2 — Library

- [ ] D1 media schema
- [ ] Artists
- [ ] Albums
- [ ] Tracks
- [ ] Drive importer/sync
- [ ] Library API
- [ ] Minimal library UI

## P3 — Playback

- [ ] AudioEngine
- [ ] Persistent playback state
- [ ] QueueManager
- [ ] Play/pause
- [ ] Previous/next
- [ ] Seeking
- [ ] Volume
- [ ] Shuffle
- [ ] Repeat

## P4 — Crossfade

- [ ] Dual playback channels
- [ ] Web Audio routing
- [ ] GainNodes
- [ ] Crossfade scheduler
- [ ] Configurable duration
- [ ] Equal-power fade option
- [ ] Transition edge cases

## P5 — Playlists and Mixing

- [ ] Playlist schema
- [ ] Playlist API
- [ ] Playlist management UI
- [ ] PlaylistQueue
- [ ] MixedPlaylistQueue
- [ ] Basic alternating/interleaved playlist mixing

## P6 — Offline Media

- [ ] IndexedDB download manifest
- [ ] OPFS media storage
- [ ] DownloadManager
- [ ] MediaResolver
- [ ] Track downloads
- [ ] Album downloads
- [ ] Playlist downloads
- [ ] Bounded download concurrency
- [ ] Version/staleness validation
- [ ] Storage usage management
- [ ] Remove-download controls

## P7 — PWA

- [ ] Service Worker
- [ ] Offline app shell
- [ ] Static asset caching
- [ ] Metadata caching
- [ ] Installable PWA
- [ ] Validate fully offline playback of downloaded music

## P8 — Interface

- [ ] Dark theme
- [ ] Desktop-first
- [ ] Responsive enough for mobile
- [ ] Sidebar navigation
- [ ] Library views
- [ ] Persistent bottom player
- [ ] Loading states
- [ ] Error states
- [ ] Empty states
- [ ] Keyboard/media controls

## P9 — Validation and Deployment

- [ ] Unit tests
- [ ] Audio subsystem tests where practical
- [ ] Worker integration tests
- [ ] Range request tests
- [ ] Offline tests
- [ ] Production Cloudflare deployment
- [ ] MVP acceptance testing

## Milestones and constraints

- P1: Drive file → Worker → HTTP Range → browser audio → successful seeking.
- P4: Track A / Gain A + Track B / Gain B → Web Audio → smooth crossfade.
- P8: Keep styling only slightly more polished than a wireframe until the system works.
- Future, outside MVP: timeline-based Mix/MixClip data with source offsets, arbitrary timeline positions, gain, fades, and automation. Do not implement now.

## P0 completion notes

- Typecheck passed for browser, Worker/shared models and tooling.
- Seven unit tests passed; Vite production client and Worker builds passed.
- Runtime smoke passed against Vite + workerd (health, API errors, frontend and SPA fallback).
- Local D1 migration applied, foundation_version read back as 1; repeat run has no pending migrations.
- No remaining peer dependency issues. Initial sandbox/network process failures were resolved with approved execution.
- D1 remote provisioning is pending by design: replace the local placeholder UUID before cloud use. No remote resources or deployment created.
- P1 requires Drive auth/access-control decisions and test media. See README.md.
