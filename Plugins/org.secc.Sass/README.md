# org.secc.Sass

> Adds SCSS/SASS compilation to Rock themes — Rock natively compiles only LESS, so this plugin compiles each theme's `Styles/*.scss` to CSS on startup and from the CMS theme blocks.

## Overview

Rock's built-in theme pipeline compiles LESS (via `dotless`). This plugin extends `RockTheme`
with a `CompileSass()` method (using the `SharpScss`/LibSass native library) and runs it at
application startup against every theme, so SCSS authored under a theme's `Styles/` folder is
turned into CSS. It also ships drop-in replacements for three Rock CMS blocks (Site Detail,
Theme List, Include Admin CSS) so that compiling a theme from the admin UI runs **both** the
LESS and the new SCSS compile. It is used by Southeast's web team to author themes in SCSS.

## Project Info

- **Project file:** `org.secc.Sass.csproj`
- **Root namespace:** `org.secc.Sass`
- **Target framework:** .NET Framework 4.7.2
- **Deploys to:** `RockWeb/bin/` (`org.secc.Sass.dll` + `.pdb` and `SharpScss.*`),
  `RockWeb/LibSass/` (the `SharpScss.1.3.8/runtimes` native LibSass binaries), and
  `RockWeb/Plugins/org_secc/` (the entire `org_secc` tree — the three block `.ascx` +
  `.ascx.cs` files) — all via the `PostBuildEvent` `xcopy` steps.

## Project Layout

```
Startup.cs            IRockStartup hook — compiles SASS for every theme on app start
ThemeExtensions.cs    RockTheme.CompileSass() extension (SharpScss/LibSass wrapper)
/org_secc/Sass/       Overridden Rock CMS blocks (.ascx + .ascx.cs):
                        SiteDetail, ThemeList, AdminCss
```

## Components

### Startup hook

| Class | Type | Purpose |
|-------|------|---------|
| `Startup` | `IRockStartup` (`StartupOrder` 0) | On app start, loops `RockTheme.GetThemes()` and calls `CompileSass()` on each. |

### Extension method

| Method | Purpose |
|--------|---------|
| `ThemeExtensions.CompileSass(this RockTheme, out string messages)` | Recursively collects `.scss` files under the theme's `Styles/` folder, skips partials (names starting with `_`), and writes a sibling `.css` for each top-level `.scss` via `Scss.ConvertToCss`. Honors `theme.AllowsCompile`. Selects the `win-x64` / `win-x86` native LibSass directory via `SetDllDirectory` based on process bitness. |

### Blocks

These are forks of the corresponding stock Rock CMS blocks, modified so theme compilation also
runs the SASS compile. They live in namespace `RockWeb.Plugins.org_secc.Sass`.

| Block | Category | Purpose | Key settings |
|-------|----------|---------|--------------|
| Theme List | SECC > CMS | Lists themes; clone / compile / delete. Compile runs `theme.Compile()` (LESS) **and** `theme.CompileSass()`. | Theme Styler Page (`LinkedPage`) |
| Site Detail | CMS | Stock site editor with a "Compile Theme" button that runs both LESS and SASS compiles for the site's theme. | Default File Type (`BinaryFileTypeField`, key `DefaultFileType`) |
| Include Admin CSS | SECC > CMS | Adds `~~/Styles/theme.css` only when `PageCache.IncludeAdminFooter` is set **and** the user can administrate the page or administrate/edit any block on it (so admin styling loads only when needed). | (none) |

## Dependencies & Integrations

- **Rock:** `RockTheme` (extended), `IRockStartup`, the Rock block/UI framework (`RockBlock`),
  `SiteService` / `PageService` / `LayoutService` / `AttributeService` and the attribute /
  authorization framework (used by the forked Site Detail block).
- **Third-party:** `SharpScss` 1.3.8 (LibSass binding) with native `LibSass` runtimes — the
  only NuGet package in `packages.config`. The `.csproj` also carries an explicit assembly
  reference to `dotless.Core` (Rock's LESS compiler) from `RockWeb\Bin`; note the LESS pass
  itself runs through Rock's own `RockTheme.Compile()`, not a direct call into dotless from
  this plugin. `Newtonsoft.Json` (used for Site Detail view-state) comes transitively via the
  Rock project reference, not as a direct package reference here.

## Observations

*Noticed while documenting — not a full audit.*

- **Improvement:** `CompileSass` returns `true` in the happy path even when nothing matched
  (no `Styles/` dir, no files, or `AllowsCompile` false), and only returns `false` on an
  exception. The single `out messages` is also overwritten by each successive compile call in
  the blocks (`Compile` then `CompileSass` both write the same variable), so a LESS error
  message can be lost once the SASS pass succeeds. Worth confirming the surfaced message
  reflects the failing step.
- **Improvement:** `Startup.OnStartup` ignores the `themeSuccess` / `themeMessage` results of
  every per-theme `CompileSass` call, so a theme that fails to compile at boot does so silently
  with no log entry. Consider logging failures.
- **Improvement:** The forked Site Detail / Theme List / Include Admin CSS blocks are copies of
  stock Rock blocks with small edits. On a Rock upgrade these will not pick up upstream changes
  and can drift from the core blocks — worth re-diffing against the current Rock versions
  periodically.

## Making Changes

- To change how SCSS is discovered or compiled (partials handling, output path, source maps),
  edit `ThemeExtensions.CompileSass` — it is the single point that wraps `SharpScss`.
- To change when compilation runs at boot, edit `Startup.OnStartup`; the admin-triggered
  compiles live in `gCompileTheme_Click` (`ThemeList.ascx.cs`) and `btnCompileTheme_Click`
  (`SiteDetail.ascx.cs`), each of which calls both `theme.Compile()` and `theme.CompileSass()`.
- If a Rock upgrade changes the stock Site Detail / Theme List / Admin CSS blocks, re-apply the
  SASS-compile edits to the forked `.ascx.cs` files under `org_secc/Sass/`.
- New compiled files must end in `.scss` and live under a theme's `Styles/` folder; prefix
  shared partials with `_` to keep them from being compiled to standalone CSS.
