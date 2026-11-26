# IDEA — João Loureiro Portfolio

## The idea

A bilingual personal portfolio and technical publishing home that helps employers, recruiters, and prospective clients understand how João Loureiro builds complete software systems—from the interface and API to deployment, automation, and the infrastructure that keeps them running.

The site should make his engineering range easy to scan, support deeper exploration through real projects and technical writing, and provide a direct way to start a conversation.

## The problem

A résumé or a list of technologies gives visitors limited evidence of how an engineer thinks and works. Personal projects can provide that evidence, but only when their purpose, decisions, limitations, and artifacts are easy to understand.

This portfolio connects professional context with concrete work in one recognizable place, available in Portuguese and English.

## Who it serves

| Audience | What they need |
| --- | --- |
| Employers and recruiters | Quickly understand João's background, engineering range, and relevance to a role. |
| Prospective freelance clients | Evaluate his ability to understand a problem and deliver a working system. |
| Technical readers | Explore project decisions and useful engineering notes, and return for new writing. |
| João, as the site owner | Maintain projects, translations, and posts through a protected workspace. |

## The promise

Show engineering through understandable evidence: what a project does, why it exists, how its parts fit together, and what was learned along the way.

The distinguishing perspective is software as a complete working system. Frontend, backend, delivery, automation, and self-hosted infrastructure form a connected practice rather than an isolated technology inventory.

## Core experience

1. **Understand:** The homepage introduces João and his technical perspective with a clear visual identity.
2. **Explore:** Selected projects lead to detail pages explaining purpose, implementation choices, and available artifacts or links.
3. **Read:** Real technical posts expand on decisions, experiments, and lessons from practical work.
4. **Connect:** A contact form and relevant professional links let interested visitors start a conversation.
5. **Maintain:** João uses a separate authenticated Admin workspace to manage the content behind the public experience.

Visitors can switch between Portuguese and English and use the site comfortably on mobile or desktop, with keyboard access and supported theme preferences.

## Essential product scope

### Public portfolio

- Introduction, professional background, and a coherent overview of technical interests.
- Selected projects with localized detail pages and honest descriptions of their maturity.
- Technical post listing and article pages, made discoverable when real content is published.
- Contact form with clear validation, submission feedback, and safe delivery handling.
- Complete Portuguese and English content, responsive layouts, accessible navigation, and useful search metadata.

### Owner workspace

- Protected Admin access with server-enforced authorization.
- Project and post management, including draft and publication controls.
- Maintenance of supported localized content.
- Operational controls for contact-email delivery where supported by the application.

The Admin workspace is an English working surface, separate from the bilingual public reading experience.

## Content and visual direction

Use the approved João Loureiro identity: linked monogram and wordmark, interlocking pattern, and the palette `#2B304D`, `#636B97`, `#EEAA74`, `#EAE3DB`, `#161616`, and `#FFFFFF`.

The public direction is brand-led editorial: strong hierarchy, readable content, purposeful imagery, and room to explore. The Admin experience prioritizes clear actions, readable forms, and dependable feedback.

Describe personal projects as explorations and demonstrations where that is their actual context. Publish only verified experience and real writing. Commercial outcomes, clients, metrics, and testimonials require supporting evidence.

## Deliberate boundaries

- Public visitors do not need accounts to browse or make contact.
- The portfolio does not need a general-purpose CMS, social network, or multi-tenant platform.
- Comments, scheduled publishing, media uploads, and a media library are outside the initial scope.
- Avoid filler articles, fabricated case studies, and public navigation that leads to an empty publishing destination.
- Keep the existing brand, localization, themes, project routes, and contact journey consistent as the product evolves.

## Technical foundation

The existing repository provides the foundation:

- **Public site and Admin:** Next.js 16 App Router, React 19, strict TypeScript, `next-intl`, and Tailwind CSS 4.
- **Backend:** ASP.NET Core targeting .NET 8, with API, Application, Domain, and Infrastructure boundaries.
- **Content:** SQLite persistence for projects, posts, and the locale catalog, with migrations and persistent storage.
- **Admin identity:** Authentik OIDC with protected server-side sessions and backend authorization.
- **Delivery:** Container images and Docker Compose, with Traefik routing and environment-provided configuration.

This is a product brief for an existing application. Source and configuration establish implementation details; they do not, by themselves, establish what is deployed or verified live.

## How the product should evolve

1. **Make the portfolio clear and dependable:** Keep the introduction, project evidence, bilingual navigation, and contact journey coherent and accessible.
2. **Make ownership sustainable:** Complete and validate the protected content workflows, persistent storage, and recovery procedures needed to maintain the site safely.
3. **Build the publishing habit:** Add real engineering articles, connect them to relevant projects, and expose the public Posts destination when content is ready.
4. **Improve from evidence:** Refine content and navigation based on actual visitor questions and maintenance friction before adding more features.

These are product priorities, not a claim that each capability is pending or production-ready. The repository already contains Admin, content-persistence, and post implementations; release readiness requires separate verification.

## What success looks like

- A first-time visitor can quickly explain what João works on and find a relevant example.
- Project pages communicate context, decisions, and limitations clearly enough to support a hiring or project conversation.
- Portuguese and English offer complete, consistent public journeys.
- Visitors can read, navigate, and submit a contact request across supported devices and accessibility preferences.
- João can maintain content without routine source edits or exposing drafts and protected operations.
- Published writing develops a credible body of practical engineering knowledge over time.

These are qualitative acceptance goals. Set numerical targets only after establishing a real baseline.

## Related documents

- [PRODUCT.md](PRODUCT.md): product positioning, audience, brand commitments, and constraints.
- [DESIGN.md](DESIGN.md): visual and interaction guidance.
- [README.md](README.md): architecture, setup, authentication, and deployment overview.
- [AGENTS.md](AGENTS.md): engineering conventions and validation requirements.
- [backend/README.md](backend/README.md): backend persistence and operational procedures.
