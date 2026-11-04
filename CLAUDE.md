# CLAUDE.md

SECC's custom Rock RMS plugins (Rock 1.16, .NET Framework 4.7.2). Coding standards, copyright
header and README rules live in the Copilot instructions; follow them:

@.github/copilot-instructions.md

`Plugins/README.md` indexes every plugin; each `Plugins/org.secc.*/README.md` describes that
plugin. Read the plugin's README before changing it.

## Workspace layout

- This repo sits next to a Rock checkout at `../Rock` (secc/Rock, branch `hotfix-1.16.12`).
  The folder names matter: `Rock.sln` references `..\RockPlugins-16\Plugins\*.csproj`, and
  `../Rock/RockWeb/Plugins/org_secc/*` are symlinks back into this repo.
- Read Rock platform source from `../Rock` when you need an API or pattern. Don't edit it from
  here; core fixes go to secc/Rock as their own branch.
- Do parallel work in git worktrees of this repo (`.claude/worktrees/`), not in a second clone.
- `RockWeb/` at this repo's root is untracked and large. Scope searches to `Plugins/` (or use
  `git grep`); a recursive grep from the root crawls it and times out.

## Building

Plugins reference Rock as a prebuilt DLL (`$(SolutionDir)RockWeb\Bin\Rock.dll`), so there is
no need to rebuild Rock to build a plugin.

- Build with `Tools/build-plugin.ps1` (PowerShell). No arguments builds the plugins changed vs
  `origin/master`; or pass names (`Workflow`, `LinkList,Event`). It warns when `../Rock`
  source is newer than its DLLs in `RockWeb\Bin`, copies the plugin DLL/PDB into
  `RockWeb\Bin`, and runs the plugin's pre/post-build copy steps (third-party DLLs, scripts,
  assets) except the `org_secc` and `Themes` copies. Use `-NoCopy` for a compile check that
  copies nothing and doesn't recycle a running site.
- If the stale-Rock warning appears and the change matters, rebuild Rock once in Visual Studio.
- Plugins with no csproj (Administration, CheckinMonitor, Connection, Equip, Event,
  FamilyOnMission, GroupTrackerDemo, RegistrationCart, Themes) are `.ascx`/`.ascx.cs` blocks
  compiled by RockWeb at runtime. There is nothing to build; errors show up only when the page
  loads, so type-check carefully.
- Obsidian UI (`*.Obsidian` folders in LinkList and RegistrationCart): a solution build does not
  refresh the JS bundle. Run `npm run deploy` in the `.Obsidian` folder, then hard-refresh.
- Run MSBuild from PowerShell, not Git Bash: Bash mangles `/p:` arguments.

## Git and PRs

- PRs target `master`. Put the JIRA key in the branch name and commit subject:
  `ROCK-1234 Short imperative summary`.
- Working copies often hold untracked in-progress files under `Plugins/` and `RockWeb/` that
  aren't part of your change. Stage explicit paths; never `git add -A` or `git add .`. Check
  `git status --short` before committing.
- When a change affects what a plugin does or how it's configured, update its README and end it
  with `Last updated: YYYY-MM-DD`.
- Repo files are CRLF in the working copy. `sed -i` from Git Bash strips the CRs; use
  `perl -pi -e` or an editor tool for in-place edits.
- Write long PR bodies to a file and pass `--body-file`.

## Deployment facts

- The pipeline deploys the whole repo at once (every plugin DLL and runtime-compiled block).
  Don't flag deploy-order or version-skew risk between plugins in this repo.
- The pipeline does **not** deploy `Content/` or `Plugins/*/Themes/`. Those are copied by hand
  to the IIS file share, and the prod share is the source of truth. Never copy repo -> share
  blind: compare share, master and the merge base first, and copy back with LF line endings.
- Plugin migrations live in `Plugins/*/Migrations/` and run on app start after deploy.

## Rock data and safety

- Never write to a Rock database directly. Data fixes are SQL scripts committed under
  `Tools/<ticket>/` for a person to review and run.
- Never commit credentials, connection strings or API keys.
- On workflow User Entry Forms, a field that isn't Visible must also be not Editable.
