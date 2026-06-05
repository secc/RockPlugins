# org.secc.Themes

> Southeast's Rock site themes — the master pages, page layouts, and styling that skin Rock's external and check-in sites.

## Overview

This "plugin" is not compiled code; it is the collection of Rock **themes** Southeast maintains.
Each theme is a folder of ASP.NET master pages, page layouts (`.aspx`), styling (LESS/SCSS), scripts,
and image/Lava assets that Rock loads to render a Site. They cover the public website, the member
portal, landing pages, child/check-in screens, seasonal sites (Easter, Christmas Together), and a
Church Online Platform skin. Designers and developers edit these to change how Rock-rendered pages
look.

## Project Info

- **Project file:** none — no `.csproj`; these are RockWeb theme assets (not a compiled assembly).
- **Root namespace:** the few code-behind files use `RockWeb.Themes.*.Layouts` — in practice only two values appear: `RockWeb.Themes.Stark.Layouts` (Stark starter-theme leftover, in most themes' `Error.aspx.cs`) and `RockWeb.Themes.MySecc.Layouts` (`my-secc`, plus an Easter copy-paste — see Observations).
- **Target framework:** n/a (markup, styles, and ASP.NET page code-behind compiled in-place by RockWeb).
- **Deploys to:** `RockWeb/Themes/<ThemeName>/` (each top-level folder maps to a Rock theme of the same name).

## Project Layout

```
/Themes/
  SECC2019/               Primary public-site theme (Layouts/Styles/Scripts/Assets + ChurchOnline/)
  SECC2019Portal/         Member-portal variant of SECC2019 (ships a Compass config.rb)
  SECC2019_Child_Invert/  Check-in / children's screens variant (Checkin.aspx, Checkin-Site.Master)
  SECC2019_LandingPage/   Landing-page theme — many Full*/Simple* card/overlay layouts
  SECC2024/               Newer public-site theme (adds TwoColumn layout)
  SECC2014/               Legacy public-site theme
  Easter/                 Seasonal site theme (+ Lava assets under Assets/Lava)
  ChristmasTogether/      Seasonal check-in theme (Checkin layout only)
  LWYA/                   "Love Where You Are" site theme (own fonts, map .kmz assets)
  Rogers/                 Campus/site theme
  KyleIdlman17/           One-off event/site theme
  my-secc/                "my.secc" theme (ships Site.Master.cs code-behind)
  SECC2019/ChurchOnline/  Church Online Platform skins (Template.liquid + SCSS, 3 sub-themes)
  SEMobileApp/            Lava snippets for the mobile app (GrowTabs, Give card) — no layouts
  RockConfiguration_Baks/ Backup copies of Global Attribute email-template Lava (header/footer/full)
```

## Components

Each theme follows Rock's theme convention: a `Layouts/Site.Master` plus one `.aspx` per page layout,
with `Styles/`, `Scripts/`, and `Assets/` alongside. There are no REST endpoints, blocks, jobs, or
field types here.

### Themes

| Theme | Kind | Layouts | Notes |
|-------|------|---------|-------|
| `SECC2019` | Public site | 10 | Primary site theme; also hosts the `ChurchOnline/` skins. |
| `SECC2019Portal` | Site (portal) | 10 | Ships a Compass `config.rb` for SCSS compilation. |
| `SECC2019_Child_Invert` | Check-in | 11 | Adds `Checkin.aspx` + `Checkin-Site.Master`, `Sections`/`SidebarSections`. |
| `SECC2019_LandingPage` | Landing pages | 17 | Full*/Simple* card, centered, fifty-fifty, and overlay layouts. |
| `SECC2024` | Public site | 11 | Newer site theme; adds a `TwoColumn` layout. |
| `SECC2014` | Public site (legacy) | 9 | Older site theme; LESS styles + vendor scripts. |
| `Easter` | Seasonal site | 8 | Includes `Assets/Lava/` page-render snippets and `Site.Master.cs`. |
| `ChristmasTogether` | Seasonal check-in | 1 | `Checkin.aspx` + `Site.Master` only; LESS variable overrides. |
| `LWYA` | Site | 8 | Own webfonts, component scripts, and map `.kmz` assets. |
| `Rogers` | Site | 8 | Standard site layout set. |
| `KyleIdlman17` | Site/event | 9 | One-off themed site. |
| `my-secc` | Site | 8 | Ships `Site.Master.cs` setting the site-name heading. |

### Other assets (no layouts)

| Folder | Contents |
|--------|----------|
| `SECC2019/ChurchOnline/Themes/` | Church Online Platform skins — `SEOnlineDarkTheme`, `SEOnlineDarkThemeYellow`, `SEOnlineLightTheme`, each `Template.liquid` + `javascript.js` + `stylesheet*.scss`. |
| `SEMobileApp/` | Lava for the mobile app: `GrowTabs.lava`, `Give/Give.lava`, `Give/HtmlToImage/GiveCard.html`. |
| `RockConfiguration_Baks/GlobalAttributeEmailTemplates/` | Backup Lava for the Email Header / Footer / Full email-template Global Attributes. |

## Dependencies & Integrations

- **Rock:** the page code-behind inherits `Rock.Web.UI.RockMasterPage` / `RockPage`; themes are
  loaded by Rock's Site/Layout framework. Layouts render Rock blocks via `<Rock:Zone>` placeholders.
- **Third-party (front-end):** Bootstrap (LESS/SCSS), Font Awesome / glyphicons (LWYA fonts),
  jQuery and component scripts (Isotope, Magnific Popup, Swiper, WOW, YouTube/Vimeo players, etc.),
  Compass/Sass (`SECC2019Portal/config.rb`). `Styles/.gitignore` files indicate compiled CSS is
  generated, not all committed.
- **Church Online Platform:** `SECC2019/ChurchOnline` skins use the platform's `Template.liquid` format.

## Observations

*Noticed while documenting — not a full audit.*

- **Improvement:** Several themes' `Layouts/Error.aspx.cs` still carry the scaffolded
  `RockWeb.Themes.Stark.Layouts` namespace (the Rock starter theme), and `Easter`'s `Site.Master.cs`
  declares the `RockWeb.Themes.MySecc.Layouts` namespace / `MySeccMasterPage` class — copy-paste
  leftovers. Harmless at runtime but misleading; worth renaming if a theme's code-behind is ever
  touched.
- **Improvement:** `RockConfiguration_Baks/` is a manual backup of Global-Attribute email Lava sitting
  in source control with no obvious consumer. Confirm it is intentional documentation rather than dead
  weight, and that it doesn't drift from the live Global Attribute values.
- **Improvement:** Many `Styles/` folders carry both source (`.less`/`.scss`) and committed compiled
  `.css`, plus `.gitignore` rules — the build/compile story differs per theme (Compass for the Portal,
  LESS elsewhere). Worth confirming which CSS is authoritative before editing styles.

## Making Changes

- To restyle a page, edit the matching theme's `Styles/` source (LESS or SCSS) and recompile to CSS;
  for `SECC2019Portal` that means running Compass against its `config.rb`.
- To change page structure, edit the relevant `Layouts/*.aspx` (or `Site.Master`) in that theme; keep
  Rock `<Rock:Zone>` placeholders intact so block placement still works.
- New page layouts must be added both as a `.aspx` file here and registered as a Layout on the Site in
  the Rock admin UI.
- Church Online skins live under `SECC2019/ChurchOnline/Themes/`; mobile-app Lava under `SEMobileApp/`.
