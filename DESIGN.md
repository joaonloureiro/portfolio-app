---
name: João Loureiro
description: Personal portfolio in the V4 Signal Terrain Obsidian design system
colors:
  bg-obsidian: "#06080D"
  surface: "#0D121C"
  surface-elevated: "#141B29"
  ink: "#F3F5F8"
  muted: "#98A3B5"
  emerald: "#10B981"
  emerald-ink: "#34D399"
  emerald-on: "#04150E"
  ice: "#38BDF8"
  light-bg: "#F3F5F8"
  light-surface: "#FFFFFF"
  light-ink: "#0B111D"
  light-muted: "#465368"
  light-emerald: "#047857"
  light-ice: "#0369A1"
typography:
  display:
    fontFamily: "Sora, sans-serif"
    fontSize: "clamp(2.65rem, 5.6vw, 4.9rem)"
    fontWeight: 800
    lineHeight: 1.02
    letterSpacing: "-0.035em"
  section:
    fontFamily: "Sora, sans-serif"
    fontSize: "clamp(1.9rem, 3.3vw, 2.75rem)"
    fontWeight: 700
    lineHeight: 1.08
    letterSpacing: "-0.028em"
  body:
    fontFamily: "Manrope, sans-serif"
    fontSize: "1.02rem"
    fontWeight: 400
    lineHeight: 1.6
  mono:
    fontFamily: "JetBrains Mono, monospace"
    fontSize: "0.82rem"
    fontWeight: 500
    lineHeight: 1.4
rounded:
  pill: "999px"
  frame: "20px"
  control: "10px"
spacing:
  section-y: "88px"
  spine-gap: "88px"
  chapter-gap: "46px"
components:
  primary-action:
    backgroundColor: "{colors.emerald}"
    textColor: "{colors.emerald-on}"
    rounded: "{rounded.control}"
    padding: "12px 22px"
    height: "48px"
  hud-pill:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.ink}"
    rounded: "{rounded.pill}"
    padding: "8px 16px"
    height: "44px"
  intake-field:
    backgroundColor: "#0B1019"
    textColor: "{colors.ink}"
    rounded: "{rounded.control}"
    padding: "12px 14px"
    height: "48px"
---

# João Loureiro

## Overview

The portfolio uses a dark-first **Signal Terrain** design system (`Sora`, `Manrope`, `JetBrains Mono`) built on an Obsidian canvas (`#06080D`) with Emerald (`#10B981` / `#34D399`) and Ice (`#38BDF8`) signal accents. A full-viewport interactive 3D perspective terrain canvas with flowing signal pulses sits behind Admin-configured intro copy, leading into an alternating Vertical Signal Spine populated dynamically from `/admin/projects` and ending with a direct contact form and icon-only profile links.

## Colors

- **Dark (default):** Canvas `#06080D`, card/pill surface `#0D121C`, elevated surface `#141B29`, primary ink `#F3F5F8`, muted ink `#98A3B5`, primary emerald `#10B981` (paired with `#04150E` text), emerald label ink `#34D399`, ice secondary `#38BDF8`.
- **Light toggle:** Canvas `#F3F5F8`, surface `#FFFFFF`, elevated `#E6EBF2`, primary ink `#0B111D`, muted `#465368`, accessible emerald `#047857` (paired with `#FFFFFF` text), accessible ice `#0369A1`.

## Typography

- `Sora` handles display headlines (`clamp(2.65rem, 5.6vw, 4.9rem)`) and chapter headings.
- `Manrope` carries readable body copy, form labels, and navigation controls.
- `JetBrains Mono` sets index tags (`01 // PROJECT`), technology pills, language/theme HUD triggers, and metadata labels (`>= 12.5px`).

## Layout

- **Floating Corner HUD:** Persistent top-left identity pill (`JL` monogram SVG + `João Loureiro`) and top-right utility pills (`PT/EN` language switcher, theme icon toggle, `Contact` action).
- **Hero Terrain:** Full-viewport (`92vh`) 3D perspective mesh canvas responding to pointer tilt, paused cleanly under `prefers-reduced-motion`.
- **Vertical Signal Spine:** Scroll-linked runner advancing down a left rail beside alternating 2-column project chapters and 3D mouse-tilt screenshot frames, collapsing to a clean single column below `920px`.
- **Contact & Icon-Only Footer:** Two-column contact intro + direct email + SMTP form (`Name`, `Email`, `Message`), followed by a compact footer with icon-only GitHub, LinkedIn, and Email links (`44px x 44px` hit targets, `aria-label` + `title`).

## Do's and Don'ts

- Keep all public copy and project items (including images) driven by `/admin/locales` and `/admin/projects`.
- Preserve the existing geometric `JL` monogram SVG verbatim.
- Keep profile links strictly as accessible icons (no visible link text).
- Avoid colored `box-shadow` glow on dark cards, gradient text, and infinite pulsing dots.
