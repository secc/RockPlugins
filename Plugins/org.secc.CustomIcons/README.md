# org.secc.CustomIcons

> Ships a Southeast-branded icon webfont (`se-*` classes) and the LESS that wires it into Rock's Font Awesome stack.

## Overview

CustomIcons is a static front-end resource plugin, not a behavioral one. It bundles an
IcoMoon-generated icon font (Southeast ministry/brand glyphs) plus the LESS needed to register
the `@font-face` and expose `se-*` CSS classes, then hooks that LESS into Rock's Font Awesome
override file so the glyphs are usable anywhere Rock renders an icon (block icons, page icons,
Lava). There is no compiled code path of consequence — the assembly is effectively a content
container, and a pre-build step copies the fonts/styles into RockWeb.

## Project Info

- **Project file:** `org.secc.CustomIcons.csproj`
- **Root namespace:** `CustomIcons`
- **Target framework:** .NET Framework 4.7.2
- **Deploys to:** Pre-build `xcopy` into RockWeb — `Fonts\*` to `RockWeb/Assets/Fonts` (landing in a
  `CustomIcons/` subfolder), `Styles\*` to `RockWeb/Styles`, and `Overrides\*` to
  `RockWeb/Styles/FontAwesome`. (At runtime the LESS references the font via
  `../../../Assets/fonts/CustomIcons` — note the lowercase `fonts`.) The built `CustomIcons.dll`
  carries no runtime logic — only `AssemblyInfo`.

## Project Layout

```
/Fonts/CustomIcons/    CustomIcons.{eot,svg,ttf,woff} — the IcoMoon webfont files
/Styles/CustomIcons/   style.less (@font-face + .se / .se-* classes), variables.less (glyph codepoints)
/Overrides/            font-awesome.less — imports fontawesome.less then CustomIcons style.less
/Properties/           AssemblyInfo.cs (stub — still titled "ClassLibrary1")
selection.json         IcoMoon project export (regenerate the font from this)
```

## Components

No REST endpoints, blocks, jobs, field types, Lava filters, or migrations — this plugin only
contributes CSS/font assets.

### CSS Classes

`.se` is the base class (sets `font-family: CustomIcons`); each `.se-*` class sets the glyph via a
`:before` `content` codepoint defined in `variables.less`.

| Class | Codepoint |
|-------|-----------|
| `.se-c920` | `\e900` |
| `.se-bb`   | `\e901` |
| `.se-ciw`  | `\e902` |
| `.se-cw`   | `\e903` |
| `.se-et`   | `\e904` |
| `.se-hsm`  | `\e905` |
| `.se-in`   | `\e906` |
| `.se-la`   | `\e907` |
| `.se-lwya` | `\e908` |
| `.se-msm`  | `\e909` |
| `.se-rv`   | `\e90a` |
| `.se-sw`   | `\e90b` |
| `.se-kids` | `\e90c` |
| `.se-rock` | `\e90d` |
| `.se-se`   | `\e90e` |

Usage mirrors Font Awesome: `<i class="se se-rock"></i>`.

## Dependencies & Integrations

- **Rock:** Hooks into Rock's Font Awesome LESS pipeline by overriding
  `RockWeb/Styles/FontAwesome/font-awesome.less` (imports the stock `fontawesome.less` first, then
  the custom `style.less`). No Rock runtime APIs.
- **Third-party:** IcoMoon (font generation; `selection.json` is the source export). No NuGet
  packages, no managed dependencies.

## Observations

*Noticed while documenting — not a full audit.*

- **Improvement:** `AssemblyInfo.cs` still carries the default scaffold metadata
  (`AssemblyTitle` / `AssemblyProduct` = `"ClassLibrary1"`, copyright `2016`). Cosmetic, but worth
  correcting for a clean assembly identity.
- **Improvement:** `Overrides/font-awesome.less` *replaces* RockWeb's stock `font-awesome.less` via
  the pre-build `xcopy`. A Rock core upgrade that changes that file could be silently clobbered (or
  this override could go stale against a newer Font Awesome). Worth confirming the override stays in
  sync on Rock upgrades.
- **Improvement:** Deployment relies entirely on a Visual Studio `PreBuildEvent` `xcopy`; there's no
  runtime/Startup registration. A non-VS or CI build that skips the pre-build step would compile the
  (empty) DLL without copying the assets.

## Making Changes

- To add or rename a glyph: re-import `selection.json` into IcoMoon, export, drop the new font files
  in `Fonts/CustomIcons/`, then add the new codepoint to `Styles/CustomIcons/variables.less` and the
  matching `.se-*` rule to `style.less`.
- The cache-buster query string (`?hhvrrz`) in `style.less` is the IcoMoon export hash — bump it when
  you ship a new font so browsers don't serve a stale cached font.
- This plugin has no C#/runtime behavior; for plugins that *use* icons in UI, see the block-based
  plugins such as [org.secc.PastoralCare](../org.secc.PastoralCare/README.md).
