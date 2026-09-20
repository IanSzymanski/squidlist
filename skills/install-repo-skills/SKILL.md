---
name: install-repo-skills
description: Install or synchronize all valid Codex skills found in a repository into the current agent's local skills directory.
---

# Install repository skills

Use this skill only when the user asks to add, install, or synchronize the skills contained in the current repository.

## Discovery

1. Resolve the repository root from the current workspace. Prefer the Git root when available.
2. Inspect the immediate child directories under `<repo-root>/skills/`.
3. Treat a directory as a skill only when it contains `SKILL.md` at its root. Preserve its complete directory, including `agents/`, `scripts/`, `references/`, and `assets/` when present.
4. Validate each skill name and frontmatter before installation. Skip invalid entries and report the exact reason.
   - Accept only skill directory basenames matching `^[a-z0-9]+(?:-[a-z0-9]+)*---
name: install-repo-skills
description: Install or synchronize all valid Codex skills found in a repository into the current agent's local skills directory.
---

# Install repository skills

Use this skill only when the user asks to add, install, or synchronize the skills contained in the current repository.

## Discovery

1. Resolve the repository root from the current workspace. Prefer the Git root when available.
2. Inspect the immediate child directories under `<repo-root>/skills/`.
3. Treat a directory as a skill only when it contains `SKILL.md` at its root. Preserve its complete directory, including `agents/`, `scripts/`, `references/`, and `assets/` when present.
; reject separators, `..`, empty names, and names outside this format.
   - Require the root `SKILL.md` frontmatter `name` to exactly match the skill directory basename.
   - Reject symlinked/reparse-point skill directories and do not follow symlinks while discovering or copying resources.

Do not scan arbitrary folders outside the repository's `skills/` directory, follow symlinks outside the repository, or install files that are not part of a discovered skill directory.

## Destination and overwrite safety

- Treat the repository copy as canonical. Installing a missing skill is safe; updating an existing local skill is a separate synchronization change.
- Use the configured Codex skills directory, normally `$CODEX_HOME/skills`; if `CODEX_HOME` is unavailable, use the local Codex skills directory under the user's profile (normally `~/.codex/skills`).
- Create the destination directory when needed.
- Resolve the configured skills root to an absolute canonical path. Before copying, resolve each destination and require it to remain inside that skills root; refuse and report any path traversal or symlink/reparse-point escape.
- For a skill that is not already installed, copy the full skill directory.
- If a destination with the same skill name already exists, compare it before changing anything. Report drift and obtain the developer's explicit verbal confirmation before overwriting or synchronizing it.
- Never delete local-only skills, destination files outside the matching skill directory, or unrelated Codex configuration.

If repository work reveals that a skill itself needs guidance changes, flag the proposed update to the developer. Do not modify the repository skill or synchronize local copies until that update has been verbally confirmed.

## Verification

After installation, verify that every requested destination contains a readable root `SKILL.md`, valid required frontmatter, and any referenced resources that were copied with it. Report installed, skipped, conflicting, and invalid skills separately, including source and destination paths.

Do not invoke the installed skills as part of installation. Installing a skill changes the local agent environment, so perform the copy only after the user's request is clear and stop if a conflict requires a choice.
