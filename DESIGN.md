---
name: João Loureiro
description: Public identity for independent software engineering engagements
colors:
  cobalt: "#0822EC"
  yellow: "#E8FF52"
  black: "#111216"
  white: "#FFFFFF"
  dark-page: "#18191F"
  field: "#F3F4F8"
  field-line: "#BFC1CA"
typography:
  display:
    fontFamily: "Barlow Condensed, sans-serif"
    fontSize: "clamp(4.25rem, 8.45vw, 8.1rem)"
    fontWeight: 900
    lineHeight: 0.82
    letterSpacing: "-0.022em"
  section:
    fontFamily: "Barlow Condensed, sans-serif"
    fontSize: "clamp(3.35rem, 7.1vw, 7rem)"
    fontWeight: 800
    lineHeight: 0.95
    letterSpacing: "-0.022em"
  body:
    fontFamily: "Atkinson Hyperlegible Next, sans-serif"
    fontSize: "1.04rem"
    fontWeight: 400
    lineHeight: 1.55
rounded:
  square: "0"
spacing:
  field-gap: "1.1rem"
  action-gap: "1.15rem"
components:
  contact-action:
    backgroundColor: "{colors.yellow}"
    textColor: "{colors.black}"
    rounded: "{rounded.square}"
    padding: "0.65rem 1rem"
    height: "3.15rem"
  price-band:
    backgroundColor: "{colors.yellow}"
    textColor: "{colors.black}"
    rounded: "{rounded.square}"
  intake-field:
    backgroundColor: "{colors.field}"
    textColor: "{colors.black}"
    rounded: "{rounded.square}"
    padding: "0.6rem 0.8rem"
    height: "3.1rem"
---

# João Loureiro

## Overview

The hiring page opens like an industrial campaign poster. The existing JL monogram sits above a condensed headline and a braided cable rendered in cobalt and acid yellow. A dedicated price band leads into a charcoal gallery of published work.

## Colors

Cobalt fills the masthead, opening, and services. Acid yellow identifies the main action, the second headline line, and the entire rate band. Charcoal carries the project gallery, project title bands, and footer. The contact surface uses white by default and charcoal in dark mode.

## Typography

Barlow Condensed sets the poster headline, section titles, and prices. Atkinson Hyperlegible Next supports service copy, navigation controls, and form fields. The compact display face gives long monthly amounts room for their cadence labels.

## Layout

The opening spans the viewport with type at left and the cable artwork at right. The price band aligns its heading and both linked offers side by side. Published work pairs a wide lead project with a two-column gallery below. Services use rows, with expertise and supporting technologies beside each service description. The intake uses two columns on wide screens.

At 720px and below, the artwork sits between the headline and supporting copy. Rates, projects, and intake stack. At 390px and below, the two contact input fields also stack.

## Elevation & Depth

Full-width color fields and thin rules separate sections. The cable artwork supplies the depth in the opening. Rate arrows and project images have restrained hover motion with reduced-motion support.

## Shapes

The rate band, links, images, and fields use square edges. Thin outlines indicate keyboard focus on every color field.

## Components

- **Rates.** Both offers show the amount, cadence, and scope. Each link preselects its engagement in the contact form.
- **Work.** Each published project provides its original imagery or current screenshot, translated summary, technologies, and available links.
- **Intake.** Visible field labels and a native selector support the brief. Visitors can also use the direct email link.
- **Opening artwork.** The site ships `frontend/public/cobalt-cable.webp`, an AI-generated image designed for the cobalt opening. Its generation prompt is recorded in `.impeccable/assets/cobalt-cable.webp.json`. The image is decorative and carries no text or project claims.

## Do's and Don'ts

- Keep both prices easy to scan and their respective scopes explicit.
- Use the original JL monogram on a high-contrast surface.
- Give project evidence enough space to read on desktop and mobile.
