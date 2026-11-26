# Product

<!-- impeccable:product-schema 1 -->

## Platform

web

## Users

Employers, hiring managers, recruiters, prospective freelance clients, and technical readers evaluating João Loureiro’s software engineering work—plus João himself maintaining projects, posts, locales, and email delivery through the authenticated Admin workspace.

## Product Purpose

Present João Loureiro as an individual full-stack and systems-oriented software engineer—connecting web interfaces, .NET services, workflow automation, and self-hosted infrastructure through real project artifacts, engineering decisions, technical writing, and a direct contact flow. Success means a visitor quickly understands what João builds end to end, inspects real work with honest context, reads published field notes, and starts a hiring, freelance, or technical conversation.

## Positioning

Software presented as complete, operating systems—bridging frontend applications, .NET services, automation, CI/CD, and self-hosted infrastructure—evidenced through real artifacts and engineering notes rather than agency pitch copy or isolated technology lists.

## Operating Context

Visitors arrive on desktop or mobile in Portuguese or English with light or dark theme preferences across three distinct surfaces: the public portfolio and project details (`/[locale]`, `/[locale]/project/[id]`), public technical writing (`/[locale]/posts`), and the separate English-only authenticated Admin workspace (`/admin`). Public USD contracting rates and the freelance engagement selector are removed in favor of a general contact flow.

## Capabilities and Constraints

- Public portfolio, project details, and posts are bilingual (`pt` and `en`) and powered by the ASP.NET Core 8 backend with SQLite persistence.
- Projects and Posts are created, edited, ordered, and published in Admin; Admin Locales manages localized copy in both languages.
- Public Posts routes (`/[locale]/posts`) are in scope and must present both an honest empty/ready state and published articles cleanly without placeholder filler.
- The contact form validates input and submits to the existing backend SMTP delivery endpoint without public pricing or an engagement-type selector.
- The Admin workspace (`/admin`) is protected by Authentik OIDC with server-enforced authorization and stays English-only, fast, and uncluttered.
- Full keyboard access, visible focus, reduced motion, light/dark themes, and narrow mobile viewports are required.

## Brand Commitments

- Retain the existing João Loureiro (`JL`) monogram (`frontend/public/logo.png`).
- Visual identity (palette, typography, imagery, and composition) is open for full replacement.
- Never read like an agency or freelance sales funnel; maintain the voice of an individual systems engineer.
- Keep project maturity and scope honest—do not fabricate commercial clients, testimonials, metrics, or placeholder posts.

## Evidence on Hand

- Published project records and screenshots: Portfolio (`frontend/public/Portfolio.png`), Homelab (`frontend/public/ProxmoxServer.png`), and Happy (`frontend/public/Happy.png`), with localized details, technology lists, and links from the backend.
- Existing `JL` monogram (`frontend/public/logo.png`).
- Verified technical background across `.NET`, React, Next.js, Angular, MariaDB, Redis, RabbitMQ, n8n, Docker, CI/CD, and self-hosted Proxmox infrastructure (`frontend/src/locales/en.json` and `pt.json`).
- Working contact delivery endpoint and authenticated Admin workspace (Projects, Posts, Locales, Email Delivery).
- Absences not to fabricate: no client logos, commercial testimonials, revenue benchmarks, or pre-existing published articles yet.

## Product Principles

- Let real engineering artifacts and technical decisions lead over marketing copy.
- Show interface, backend services, automation, and infrastructure as one connected practice.
- Keep personal explorations and homelab systems honest in their actual scope and maturity.
- Tailor each surface to its job: showcase work on the portfolio, optimize comprehension in Posts, and prioritize operational clarity in Admin.
- Preserve Portuguese/English parity, accessibility, and light/dark usability across every public route.

## Accessibility & Inclusion

Bilingual Portuguese (`pt`) and English (`en`) public experience; semantic HTML, full keyboard navigation, visible focus states, WCAG-compliant contrast in light and dark themes, and `prefers-reduced-motion` support.
