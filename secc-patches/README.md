# SECC Core Rock Customizations

Inventory of every SECC customization to core Rock (secc/Rock fork), so each
Rock version upgrade is a mechanical re-apply instead of archaeology. Also our
running record of core divergence from upstream — useful for upgrade planning
and any future migration analysis.

**Lives in secc/RockPlugins** because Rock version branches are copied fresh
from upstream (SparkDevNetwork/Rock) — anything committed on a version branch
doesn't carry forward. RockPlugins persists across Rock versions.

**Scope:** core Rock changes only. Plugin work lives in RockPlugins normally
and doesn't belong in this manifest.

## How it works

Each customization is one squashed commit on a secc/Rock version branch,
marked with a **tag**. Tags are repo-level, not branch-level, so they survive
version-branch churn — a fresh v17 branch copied from upstream can still
cherry-pick any tagged commit, with full 3-way merge from real history.

The manifest below is the authoritative list of what to re-apply. The
`patches/` folder holds `format-patch` exports as a readable/offline backup;
the tags are the primary mechanism.

## Manifest

| Tag | Jira | Description | Files touched | First applied | Non-code deploy steps |
|---|---|---|---|---|---|
| `secc/ROCK-8640-connect-gate` | ROCK-8640 | S&S Connect button visibility gate on Connection Request Board + Detail blocks. Rock Admin bypass; reads all 3 `SecurityToConnect` attribute key variants. | `RockWeb/Blocks/Connection/ConnectionRequestBoard.ascx.cs`, `RockWeb/Blocks/Connection/ConnectionRequestDetail.ascx.cs` | 1.13.7 (ported to 1.16.12) | Set **Safety & Security Role** block setting on every instance of both blocks, per environment (pick from role picker — GUIDs differ per env). Verify `ConnectableStatuses` values per secc/Rock PR #4 review. |

## Upgrade workflow (new Rock version)

```bash
# 1. In secc/Rock, create the new version branch from upstream
git fetch upstream --tags
git checkout -b hotfix-1.17.x upstream/hotfix-1.17.x

# 2. Walk the manifest, cherry-picking each tag in order
git cherry-pick secc/ROCK-8640-connect-gate
# ...one per manifest row
```

When a cherry-pick conflicts, that's the feature, not a bug — it's an explicit
worklist of which customizations need human attention on the new version
(e.g., upstream rewrote or moved the block, WebForms → Obsidian). Resolve,
commit, then **re-tag so the next upgrade starts from the fixed version**:

```bash
git tag -f secc/ROCK-8640-connect-gate <new-sha>
git push origin -f secc/ROCK-8640-connect-gate
# refresh the backup patch too:
git format-patch -1 <new-sha> -o <rockplugins>/secc-patches/patches/
```

If a tag is ever lost (repo re-fork, etc.), fall back to the patch file:
`git am --3way secc-patches/patches/<file>.patch`

## Adding a new customization

1. Land it on the current version branch as **one squashed commit**
   (squash-merge the PR).
2. Tag it: `git tag secc/<jira>-<slug> <sha> && git push origin secc/<jira>-<slug>`
3. Export the backup patch: `git format-patch -1 <sha> -o <rockplugins>/secc-patches/patches/`
   and rename with the next sequence number.
4. Add a manifest row, including any post-deploy config the change needs.

## Conventions

- One tag = one feature/fix = one squashed commit. Never bundle.
- Manifest order is apply order. Keep independent changes independent so a
  conflict in one doesn't block the rest.
- Re-point the tag (and refresh the patch) whenever a port required changes,
  so the manifest always references the newest working version.
- Record every non-code step (block settings, attribute data, security roles)
  in the manifest — the commit only carries the code.
- If upstream ever absorbs one of our changes, remove the row and note it in
  the commit message removing it.
