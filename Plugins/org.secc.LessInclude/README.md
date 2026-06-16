# org.secc.LessInclude

> A CMS block that lets admins edit LESS variables/theme per page and compiles them to CSS on save, swapping the page's stylesheet for the result.

## Overview

LessInclude is a single Rock block (category **CMS**) that overrides a page's default theme CSS with
a compiled-on-demand variant. An admin edits four LESS sources (variable overrides, full variables,
theme, and print) in the block's custom-settings modal; on save the block compiles `bootstrap.less`
and `theme.less` to CSS via the dotless engine and links the resulting files into the page header.
Each block instance keeps its own copy of the LESS files under the active theme's `Styles/<blockId>/`
folder, so per-page styling can diverge from the site theme. The plugin also ships the **StarkLess**
theme as a starting point.

## Project Info

- **Project file:** `org.secc.LessInclude.csproj` (compiles only `Properties/AssemblyInfo.cs` to an
  assembly; the `.ascx`/`.ascx.cs` block and `Themes/` are xcopied to RockWeb by a post-build step).
- **Root namespace:** `org.secc.LessInclude` (block class namespace is `RockWeb.Plugins.org_secc.LessInclude`).
- **Target framework:** .NET Framework 4.7.2
- **Deploys to:** `RockWeb/Plugins/org_secc/LessInclude/` (block markup), `RockWeb/Themes/` (StarkLess
  theme), and `RockWeb/bin/` (`dotless.*`).

## Project Layout

```
/org_secc/LessInclude/   LessInclude.ascx (+ .ascx.cs) — the CMS block + custom-settings modal
/Themes/StarkLess/       Sample theme: Layouts/, Styles/ (LESS + base CSS), Assets/Lava/
/Properties/             AssemblyInfo.cs (only compiled file)
```

## Components

### Blocks

Category in Rock: **CMS**.

| Block | Purpose | Custom settings (LESS editors) |
|-------|---------|--------------------------------|
| Less Include | Includes custom CSS via LESS into the current page; compiles LESS to CSS on save and replaces the page's default stylesheet links. | **Variables**, **Overrides**, **Theme**, **Print** |

The four settings are declared as `CodeEditorField` attributes scoped to `"CustomSetting"` (so they
appear only in the block's settings modal, not standard block properties). They map to files written
under the theme's `Styles/<blockId>/` folder:

| Setting (key) | Type | File written | Notes |
|---------------|------|--------------|-------|
| **Variables** | LESS code editor | `_variables.less` | Full variable list. |
| **Overrides** | LESS code editor | `_variable-overrides.less` | Variable overrides. |
| **Theme** | LESS code editor | `theme.less` | Template/theme LESS. |
| **Print** | LESS code editor | `_print.less` | Print stylesheet LESS. |

Runtime behavior (`LessInclude.ascx.cs`):

- `OnLoad` hides the page's `CSSDefault` placeholder, then links the block-specific `bootstrap.css`
  and `theme.css` (falling back to the theme-level files if no per-block copy exists). Resolved paths
  are cached per `BlockId + fileName`.
- On first save the block creates `Styles/<blockId>/` and seeds it from `themeCustom.less` /
  `bootstrapCustom.less`.
- `mdEdit_SaveClick` writes the four LESS files, then `ProcessLess` compiles `bootstrap.less` and
  `theme.less` with `dotless.Core` (`Less.Parse`), saves the CSS, and clears the cached paths.

## Dependencies & Integrations

- **Rock:** `RockBlockCustomSettings`, `RockBlock`/`RockPage`, `CodeEditorField`, `Rock:ModalDialog`,
  `Rock:CodeEditor`, the block caching API (`GetCacheItem`/`AddCacheItem`/`RemoveCacheItem`).
- **Third-party:** `dotless` 1.5.2 (`dotless.Core` — LESS-to-CSS compilation).
- No REST, jobs, Lava filters, field types, or migrations.

## Observations

*Noticed while documenting — not a full audit.*

- **Security (medium):** The block writes files into the live theme's `Styles/<blockId>/` directory
  at runtime based on admin-entered LESS, and links them into the page header. Since the settings
  modal is admin-only this is expected, but the block has effective write access to the theme
  directory and compiles attacker-controllable LESS — worth confirming the settings modal is gated to
  trusted CMS-admin roles and that the theme folder isn't writable by lower-privilege paths.
- **Improvement:** Every file-touching method (`GetFileLocation` on `OnLoad`, plus `GetPageFile`,
  `ProcessLess`, and `SavePageFile` on the settings/save path) sets `Environment.CurrentDirectory`
  on a shared web process. Setting process-global CWD from a request is not thread-safe and can race
  with concurrent requests; consider passing absolute mapped paths to the file APIs instead of
  relying on CWD.
- **Improvement:** Compiled CSS is written directly into `RockWeb/Themes/...`, which is typically
  redeployed/overwritten on plugin or theme updates. Per-block compiled output living under the theme
  folder may be lost on deploy — worth confirming the `Styles/<blockId>/` artifacts are
  backed up or regenerated.

## Making Changes

- Block logic (CSS swap, LESS compilation, per-block file storage) lives in
  `org_secc/LessInclude/LessInclude.ascx.cs`; the settings modal markup is in the matching `.ascx`.
- To change which LESS files compile or where output lands, edit `ProcessLess` / `SavePageFile` /
  `GetFileLocation` in the `.ascx.cs`.
- The sample theme (layouts, base LESS/CSS, seed `themeCustom.less` / `bootstrapCustom.less`) is under
  `Themes/StarkLess/`; new LESS settings would add a `CodeEditorField` attribute plus a save call in
  `mdEdit_SaveClick`.
