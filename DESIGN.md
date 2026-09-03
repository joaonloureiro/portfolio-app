---
name: João Loureiro Portfolio
description: A living atlas for complete engineering systems, honest explorations, and future field notes.
colors:
  primary: "#EF8C4C"
  route: "#747FBE"
  ledger: "#10182D"
  ink: "#161616"
  paper: "#E7DDD1"
  paper-bright: "#F1E8DE"
  dark-text: "#F7F2EB"
  paper-text: "#20243B"
  muted-dark: "#C9C6D7"
  muted-paper: "#52566D"
  error: "#F28B82"
typography:
  display:
    fontFamily: "Oswald, sans-serif"
    fontSize: "clamp(2.8rem, 10vw, 5rem)"
    fontWeight: 300
    lineHeight: 0.92
    letterSpacing: "0.025em"
  headline:
    fontFamily: "Oswald, sans-serif"
    fontSize: "clamp(2rem, 2.7vw, 3.15rem)"
    fontWeight: 300
    lineHeight: 0.98
    letterSpacing: "0.025em"
  title:
    fontFamily: "Oswald, sans-serif"
    fontSize: "clamp(1.22rem, 1.8vw, 1.75rem)"
    fontWeight: 300
    lineHeight: 1.08
    letterSpacing: "0.025em"
  body:
    fontFamily: "Barlow, sans-serif"
    fontSize: "16px"
    fontWeight: 400
    lineHeight: 1.55
    letterSpacing: "normal"
  label:
    fontFamily: "Oswald, sans-serif"
    fontSize: "0.72rem"
    fontWeight: 600
    lineHeight: 1.2
    letterSpacing: "0.12em"
rounded:
  square: "0"
spacing:
  xs: "0.32rem"
  sm: "0.8rem"
  md: "1.25rem"
  lg: "2rem"
  xl: "3rem"
components:
  button-primary:
    backgroundColor: "{colors.primary}"
    textColor: "{colors.paper-text}"
    rounded: "{rounded.square}"
    padding: "1rem 1.1rem"
  button-ghost:
    backgroundColor: "transparent"
    textColor: "{colors.dark-text}"
    rounded: "{rounded.square}"
    padding: "0.7rem 0.85rem"
  input-field:
    backgroundColor: "transparent"
    textColor: "{colors.dark-text}"
    rounded: "{rounded.square}"
    padding: "0.8rem 0"
  project-card:
    backgroundColor: "{colors.paper-bright}"
    textColor: "{colors.paper-text}"
    rounded: "{rounded.square}"
    padding: "1.25rem 2rem 1.7rem"
---

# Design System: João Loureiro Portfolio

## Overview

**Creative North Star: "The Living Atlas"**

The portfolio treats engineering breadth as one connected system, not a list of tools. The visual language is brand-led editorial: a near-black identity rail and deep navy illustrated atlas establish a technical field, while restrained apricot signals and cream paper chapters make the reading path memorable. Condensed display type gives the work a confident, published voice without claiming outcomes the projects have not earned.

The public journey moves from orientation to evidence to conversation. A responsive architectural illustration makes interfaces, services, data, deployment, and observability visible at a glance; honest project entries carry the proof; Field Notes reserves a credible seam for future Posts; the contact surface closes the loop. The system is intentionally flat, square, and diagrammatic. It rejects an empty centered hero and a generic card-grid portfolio as the primary identity.

**Key Characteristics:**
- A fixed identity rail with a vertical wordmark on wide screens, with a compact mobile bar below the desktop breakpoint.
- One responsive raster atlas with live localized copy and a clear project action.
- Flat tonal surfaces, hairline dividers, square corners, and precise technical imagery.
- Honest project framing and an explicit coming-soon seam for future writing.

## Colors

The palette pairs a vivid warm signal with a cool technical field and paper-like reading surfaces. The default dark mode carries the strongest brand contrast; light mode reassigns the same roles to softer paper values.

### Primary
- **Apricot Signal** (`#EF8C4C`): Primary actions, selection, compact status details, and the small moments that ask the visitor to move.

### Secondary
- **Periwinkle Route** (`#747FBE`): Secondary connections inside the atlas image, status marks, focus-adjacent accents, and quiet wayfinding.

### Neutral
- **Ledger Navy** (`#10182D`): The engineering atlas and its diagram field.
- **Near-Black Ink** (`#161616`): The identity rail, footer, and mobile shell.
- **Warm Paper** (`#E7DDD1`): Field Notes and editorial reading chapters.
- **Bright Paper** (`#F1E8DE`): Selected work and other high-contrast paper surfaces.
- **Dark Text** (`#F7F2EB`): Primary text on the rail and ledger.
- **Paper Text** (`#20243B`): Primary text on light surfaces.
- **Muted Dark** (`#C9C6D7`) and **Muted Paper** (`#52566D`): Secondary copy and metadata.

**The One Signal Rule.** Apricot is reserved for actions, identity moments, and small status details; it should never become a full-section background or a wall of text.

## Typography

**Display Font:** Oswald (with a sans-serif fallback)
**Body Font:** Barlow (with a sans-serif fallback)
**Label/Mono Font:** Oswald is also used for uppercase labels, navigation, and compact metadata.

**Character:** Oswald gives the atlas a narrow, poster-like editorial voice that follows the approved C3 reference more closely. Barlow keeps paragraphs, form copy, and project context open and readable. Uppercase labels use generous tracking as a wayfinding device, not as decoration.

### Hierarchy
- **Display** (300, up to `6rem`, `0.9`): The hero role and large section titles.
- **Headline** (300, `clamp(2rem, 2.7vw, 3.15rem)`, `0.98`): Large editorial statements.
- **Title** (400, `clamp(1.22rem, 1.8vw, 1.75rem)`, `1.08`): Field Notes and compact feature titles.
- **Body** (400, `16px`, `1.55`): Explanatory copy, project context, notes, and form labels.
- **Label** (600, `0.72rem`, `0.12em`, uppercase): Navigation, layer metadata, section eyebrows, and technical annotations.

**The Condensed Voice Rule.** Use the display face for orientation and hierarchy; keep long-form reading in Barlow and do not set paragraphs in all caps.

## Layout

Wide screens use a fixed `13rem` identity rail and a fluid main canvas. The first surface is a full-bleed illustrated hero with live copy held in its calm left field and the layered system artwork centered to the right. It hands off to one continuous paper work index, split roughly `28 / 72` between Field Notes and Selected Explorations, with three project records aligned in a ledger.

The image composition and rail create a stable left-to-right reading rhythm without centering the page. The main content uses generous outer padding, hairline dividers, and a deliberate transition from dark atlas to warm paper. At `980px` and below, the rail becomes a fixed mobile bar, the image crops toward the core machinery, and the live copy settles against a dark lower fade. At `680px`, paper sections collapse to one column and project entries become a readable vertical sequence.

## Elevation & Depth

This is a flat-by-default system. Depth comes from tonal adjacency, borders, and image cropping rather than cards floating on shadows. Motion is restrained: color and border transitions are short, and project images can lift subtly on hover. Reduced-motion preferences remove nonessential transitions.

### Shadow Vocabulary
- **Focus underline** (`box-shadow: 0 2px 0 -1px var(--accent-apricot)`): A compact, structural focus cue for text inputs; it never becomes a decorative glow.

**The Flat Ledger Rule.** Keep surfaces flush and square at rest. Use tonal change, dividers, and authored geometry before adding elevation.

## Shapes

The form language is intentionally square: interactive controls, project frames, image crops, notifications, and paper chapters use `0` radius. Hairline borders and short accent bars carry the geometry. The signature silhouette comes from the atlas image's stacked architectural planes and single warm route. Focus rings remain visible and offset, with a 2px apricot outline.

## Components

### Buttons
- **Shape:** Square, flush corners (`0`).
- **Primary:** Apricot background with paper-text ink, condensed uppercase label, and generous vertical padding.
- **Hover / Focus:** Shift to the periwinkle route on hover where the action is secondary; preserve the global 2px apricot focus outline and a visible keyboard offset.
- **Secondary / Ghost:** Transparent rail controls use a hairline border and inherit the current surface color; they become apricot on hover.

### Cards / Containers
- **Corner Style:** Square (`0`); no floating card stack.
- **Background:** Project entries sit on bright paper; Field Notes uses the warmer paper token; the hero remains a continuous dark image field.
- **Shadow Strategy:** No resting shadow. Use borders, crop frames, and tonal contrast.
- **Border:** Hairline dividers separate entries and chapters.
- **Internal Padding:** Compact entries start around `1.25rem`; outer chapters use the larger `2rem`–`5rem` rhythm.

### Inputs / Fields
- **Style:** Transparent fields with a bottom border, square corners, and Barlow body copy.
- **Focus:** Apricot bottom-border shift and a one-pixel underline shadow; the global focus outline remains available for keyboard users.
- **Error / Disabled:** Error copy uses the error token; invalid fields keep their label and actionable message visible.

### Navigation
- **Style:** The fixed desktop rail uses the official interlocking brand mark, a vertical João Loureiro wordmark with visible breathing room before its divider, and three numbered uppercase links anchored to a thin vertical spine with circular stops. Mobile uses a compact bar and disclosure navigation. Hover shifts to apricot, while periwinkle carries numbers and active wayfinding.

### Atlas Hero
The hero uses a wide, text-free raster illustration based on the approved atlas composition. Localized HTML copy remains separate from the image for accessibility, translation, and responsive wrapping. `next/image` fills the surface, while viewport-specific object positioning keeps the core machinery visible without maintaining brittle SVG coordinates.

The artwork carries the visual system layers; it must not contain baked-in labels, navigation, logos, or buttons. Live copy sits over the deliberately quiet portion of the image and keeps sufficient contrast at every crop.

### Field Notes Seam
The Field Notes column is the left-hand entry to the shared paper work index. It says “coming soon” plainly and gives the future Posts section a home without decorative vectors or becoming a separate full-width chapter.

### Notifications
Toasts use a larger square status frame with a clear icon, localized context and state metadata, message text, and a keyboard-accessible dismiss action. Success uses apricot, errors use the error token, and neutral/loading states use periwinkle.

### Project Detail
Project details reuse the Living Atlas rather than switching to a generic case-study template: a clean navy hero frames the project title and image without decorative route lines, cream chapters carry the explanation and numbered feature ledger, and an apricot technical plate owns technologies and external actions.

## Do's and Don'ts

### Do:
- **Do** keep the identity rail, illustrated atlas, paper chapters, and restrained apricot signals as a coherent tonal sequence.
- **Do** show breadth through concrete systems, decisions, imagery, and project context.
- **Do** preserve square geometry, hairline dividers, and condensed editorial labels.
- **Do** keep important text live in HTML rather than baking it into imagery.
- **Do** keep Portuguese and English copy complete and synchronized.
- **Do** make future Posts feel like a natural Field Notes continuation without fabricating articles.

### Don't:
- **Don't** replace the atlas with a centered hero or a generic three-card grid.
- **Don't** separate Field Notes into a full-width strip or restore oversized editorial project headings; those belong to the discarded hybrid, not the approved C3.
- **Don't** add authored diagram vectors back to the Hero or Field Notes.
- **Don't** invent clients, testimonials, metrics, outcomes, or commercial case-study language for personal explorations.
- **Don't** spend apricot across large decorative areas; keep it as a signal.
- **Don't** hide the contact path, keyboard focus, reduced-motion behavior, or mobile navigation.
- **Don't** fold a future Admin surface into the expressive public reading journey.
