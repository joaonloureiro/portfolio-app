# Admin project-management tasks

This backlog replaces duplicated, hard-coded frontend project data with a protected Admin capability while preserving the current public portfolio and project-detail URLs.

Complete one numbered task at a time. Give a subagent exactly one numbered task, review its diff and validation evidence, and only then mark it complete. Tasks that alter persistence, schema migrations, `Program.cs`, shared Admin navigation, or public project reads must run sequentially.

## Confirmed scope

- Admin can list, add, edit, publish/unpublish, and remove project information.
- A project contains one locale-independent record plus complete English and Portuguese content.
- Existing public project URLs such as `/en/project/1` and `/pt/project/1` remain valid.
- New projects receive a stable immutable public identifier; reordering never changes URLs.
- Initial fields are: sort order, status, technologies, image reference, repository URL, live URL, and localized title, summary, details, feature heading, and ordered features.
- Image upload and a media library are deferred. The first version accepts an optional validated deployed-asset path, or an HTTPS URL only when its host is explicitly supported by the frontend image policy.
- “Remove” means permanent deletion only for an unpublished project after explicit confirmation. Published projects must first be unpublished; conflicts return `409`.
- No client, outcome, testimonial, metric, or commercial claim may be invented.

## Shared content-persistence checkpoint

Posts already established the durable persistence foundation in `Infrastructure.Persistence` with EF Core SQLite, the `ContentStore__Path` runtime setting, migrations, and an isolated SQLite test connection. Projects must reuse that `PortfolioDbContext`, migration flow, volume, and test-store convention rather than creating a second database or generic repository.

- Extend the existing context with only the project tables and mappings required by this backlog.
- Keep project persistence behind a focused `IProjectStore` boundary in Application.
- Do not rely on an unmounted container filesystem for durable content; production uses the existing content volume.
- Do not create tables for future locales, media, roles, or audit frameworks.
- Only one subagent may change the persistence bootstrap or apply schema migrations at a time.

## Application and security rules

- Domain owns project invariants; Application owns use cases and store abstractions; Infrastructure owns persistence; API owns DTOs, validation, HTTP semantics, authentication, and authorization.
- All project mutations and draft reads inherit `PortfolioAdmin`.
- Anonymous endpoints return published content only and never reveal drafts or deletion/concurrency metadata.
- Keep OIDC tokens inside the encrypted server-side session and use server-mediated Admin mutations.
- Enforce backend limits on all strings, list sizes, URL schemes/hosts, sort order, request size, and localized content.
- Require complete `en` and `pt` content before publishing. Drafts may be incomplete only where the explicit draft contract permits it.
- Use optimistic concurrency for update, publish/unpublish, reorder, and delete operations.
- Use one explicit ordering operation; do not infer ordering from titles, identifiers, or client-only state.
- Invalidate relevant public caches only after committed changes.

## Task 1: Freeze the project contract and migration map

Goal: define the observable project behavior before persistence changes.

- [x] Inventory the two hard-coded project arrays and all `projects.project_*` catalog values.
- [x] Reconcile existing differences, including optional links, before choosing the canonical seed values.
- [x] Map all three current records to the new field contract without rewriting or embellishing their content.
- [x] Keep existing numeric identifiers `1`, `2`, and `3` and define how the next identifier is allocated safely.
- [x] Define draft, published, and unpublished behavior.
- [x] Define the exact completeness rules required to publish both locales.
- [x] Define limits for title, summary, details, feature count/length, technology count/length, URLs, image reference, and total request size.
- [x] Define valid relative image paths and approved HTTPS-host behavior; reject other schemes and protocol-relative URLs.
- [x] Define stable error keys and HTTP results for invalid input, not found, identifier conflict, stale revision, published-delete conflict, and unavailable storage.
- [x] Define cache behavior and the compatibility expectation for existing public project routes.

Acceptance criteria:

- [x] The current three projects have a reviewed, lossless migration map.
- [x] Publish, ordering, URL, image, and delete behavior are unambiguous.
- [x] No production behavior or content changes in this task.

### Reviewed migration map

The current source has two data arrays: the homepage list in `frontend/src/app/components/sections/Projects.tsx` and the detail route in `frontend/src/app/(i18n)/[locale]/project/[id]/page.tsx`. The localized content is stored under matching `projects.project_1_*`, `projects.project_2_*`, and `projects.project_3_*` keys in both `en.json` and `pt.json`.

| ID / order | Status | Image | Technologies | Repository | Live URL | Localized content |
| --- | --- | --- | --- | --- | --- | --- |
| `1 / 1` | `published` | `/Portfolio.png` | `Next.js`, `.NET 8`, `TypeScript`, `Tailwind CSS` | `https://github.com/joaonloureiro/portfolio-app` | `https://joaoloureiro.dev.br` | `project_1_title`, `description`, `details`, `features_title`, `features` |
| `2 / 2` | `published` | `/ProxmoxServer.png` | `Proxmox`, `Docker`, `Traefik`, `Observability` | none | none | `project_2_title`, `description`, `details`, `features_title`, `features` |
| `3 / 3` | `published` | `/Happy.png` | `React`, `Node.js`, `TypeScript`, `MariaDB` | `https://github.com/joaonloureiro/happy-app` | `https://happy.joaoloureiro.dev.br/` | `project_3_title`, `description`, `details`, `features_title`, `features` |

The detail array contains the portfolio live URL for project 1 while the homepage array omits it. The canonical seed uses the existing detail value; Task 10 must preserve the current homepage link surface until the public cutover deliberately resolves that parity difference. All three existing records are seeded as `published`, with order `1..3`, revision `1`, and no fabricated publication timestamp. Their English and Portuguese strings/features are copied exactly from the catalogs; no translation or content correction is part of the migration.

New identifiers are allocated server-side after the seeded values and are never accepted from a create request. The SQLite identity primary key and uniqueness constraint protect the stored identifier; a concurrent allocation collision is returned as a safe conflict rather than overwriting an existing project.

Project rules fixed for later tasks:

- `draft` may be edited; `published` may be edited only through the explicit update contract; `published -> unpublished` and `draft|unpublished -> published` are the only status transitions. Publishing requires complete English and Portuguese content.
- Each locale requires a non-empty title, summary, details, feature heading, and at least one ordered feature. A project requires 1–20 technologies and a non-empty image reference. Drafts may be incomplete; unpublished records may be edited and republished.
- Limits are title/feature heading 120 characters, summary 320, details 10,000, feature text 500, 1–20 features, technology text 60, 1–20 technologies, URLs 2,048, image reference 512, and request body 256 KiB.
- Image references are root-relative paths beginning with `/` but not `//`, or HTTPS URLs on explicitly approved hosts. Relative paths cannot contain a scheme, control character, or traversal segment. Project links accept HTTPS only on `github.com`, `joaoloureiro.dev.br`, or `happy.joaoloureiro.dev.br`; HTTP, `javascript:`, `data:`, and protocol-relative URLs are rejected.
- Permanent deletion is allowed only for `unpublished` records after an identifier-specific confirmation and expected-revision check. Drafts must use the explicit lifecycle operation before deletion.
- Stable error keys are `project_invalid_input`, `project_not_found`, `project_identifier_conflict`, `project_stale_revision`, `project_invalid_transition`, `project_published_delete_conflict`, and `project_storage_unavailable`, mapped to `400`, `404`, `409`, or `503` as appropriate.
- Public project reads expose published records only, ordered by `sortOrder`, with `Cache-Control: public, max-age=60, s-maxage=300, stale-while-revalidate=86400`; successful Admin mutations revalidate the list and affected locale detail paths.

## Task 2: Establish or adopt durable content persistence

Goal: provide the smallest durable persistence foundation required by project CRUD.

- [x] Inspect the current runtime and document the approved provider, durability, backup, and restore expectations.
- [x] Add only required provider packages and update NuGet lock files with restore commands.
- [x] Bind and validate the connection through runtime configuration without committing credentials.
- [x] Add the minimal Infrastructure registration and migration mechanism.
- [x] Add a deterministic isolated test-store strategy.
- [x] Document the pre-deployment migration and rollback procedure.
- [x] Reuse the equivalent Posts persistence foundation rather than introducing a second store.
- [x] Do not add speculative schemas or a generic CRUD framework.

Acceptance criteria:

- [x] Project data can survive application restart under the documented deployment model.
- [x] Tests need no production credentials or external service.
- [x] Application and Domain remain provider-independent.

## Task 3: Model projects and application use cases

Goal: implement project behavior behind focused use cases.

- [x] Add a project aggregate/entity with an immutable identifier, revision, status, sort order, optional links/image, technologies, and localized content.
- [x] Keep locale-independent and localized fields explicit rather than encoding translations into magic property names.
- [x] Enforce invariants for identifiers, status transitions, ordering bounds, supported locales, and publish completeness.
- [x] Add a capability-specific project store abstraction to Application.
- [x] Add use cases to list Admin projects, get one Admin project, create a draft, update a draft/unpublished project, publish, unpublish, reorder, and delete an unpublished project.
- [x] Keep permanent deletion explicit and separate from update.
- [x] Pass `CancellationToken` through every persistence operation.
- [x] Add focused unit tests for invariants, status transitions, bilingual completeness, ordering, URL validation decisions, and revision conflicts.

Acceptance criteria:

- [x] Invalid or incomplete projects cannot be published.
- [x] Ordering changes do not change public identifiers.
- [x] A stale revision cannot overwrite or delete newer data.
- [x] Application code has no ASP.NET Core or provider dependency.

## Task 4: Implement persistence and seed current projects

Goal: store projects durably without changing the visible public data.

- [x] Add only the project persistence mapping and migration/schema required by Task 3.
- [x] Implement the focused store methods required by current use cases.
- [x] Make create/update/status/reorder/delete operations transactional where partial state would be visible.
- [x] Enforce identifier and ordering uniqueness at the appropriate boundary.
- [x] Seed projects `1`, `2`, and `3` from the reviewed migration map.
- [x] Make seeding idempotent and ensure it never overwrites later Admin edits.
- [x] Preserve ordered technologies and features.
- [x] Add Infrastructure tests for round trips, ordering, concurrency, delete conflicts, and seed idempotency.

Acceptance criteria:

- [x] The seeded records reproduce all current project content and links.
- [x] Re-running bootstrap/migrations is safe.
- [x] Failed multi-step changes do not leave partial project state.

Task 4 uses EF migration-time `HasData` inserts in `SeedProjects`. The migration is applied once by EF's migration history, and the application does not run seed writes at startup, so later Admin edits are not overwritten. The SQLite store tests apply the complete migration chain and verify seeded values, ordered children, round trips, replacement, ordering, and stale updates.

## Task 5: Add protected Admin project endpoints

Goal: expose explicit management operations through the existing protected route group.

- [x] Add `GET /admin/api/projects` with a bounded list response ordered by sort order.
- [x] Add `GET /admin/api/projects/{id}`.
- [x] Add `POST /admin/api/projects` to create a draft.
- [x] Add `PUT /admin/api/projects/{id}` with the expected revision.
- [x] Add explicit publish and unpublish operations rather than a client-controlled status field that bypasses transition rules.
- [x] Add one reorder operation that validates the full submitted identifier order and expected revisions.
- [x] Add `DELETE /admin/api/projects/{id}` for explicitly confirmed, unpublished deletion.
- [x] Keep DTOs in API, validate at the boundary, and map explicitly.
- [x] Return `201` for create, `400` for invalid input, `404` for missing records, `409` for state/revision conflicts, and `503` for unavailable storage.
- [x] Ensure every endpoint inherits `PortfolioAdmin`, disables protected-response caching, and returns safe `ProblemDetails`.

Acceptance criteria:

- [x] Drafts and mutations are inaccessible without the configured administrator identity.
- [x] Status transitions, concurrency, and deletion rules cannot be bypassed through DTO shape.
- [x] API responses expose no persistence/provider internals.

Task 5 is covered by the protected endpoint mapping, boundary validators, and HTTP tests for authorization, lifecycle, bilingual publication, stale revisions, full-order validation, and confirmed deletion. Draft deletion remains intentionally unavailable until the record has passed through the explicit published-to-unpublished lifecycle.

## Task 6: Add anonymous published-project endpoints

Goal: provide the public frontend with localized published project data.

- [x] Add `GET /api/projects?locale={locale}` for the ordered published summary list.
- [x] Add `GET /api/projects/{id}?locale={locale}` for one published detail.
- [x] Return `400` or `404` consistently for unsupported locales according to the contract from Task 1.
- [x] Return `404` for draft, unpublished, deleted, or unknown project details.
- [x] Return only the requested locale and public fields.
- [x] Add an explicit cache policy and revision/ETag suitable for public content.
- [x] Bound response sizes and list counts.
- [x] Keep these endpoints explicitly anonymous without weakening the Admin group.

Acceptance criteria:

- [x] Anonymous callers can read published content only.
- [x] English and Portuguese responses contain the matching localized values.
- [x] Current projects are returned in their current visible order.

Task 6 exposes only published projects through anonymous localized list/detail endpoints. Responses omit status and revision metadata, use the defined public cache policy and ETags, and return `400` for unsupported locales and `404` for non-public or unknown records. HTTP coverage verifies English/Portuguese parity, conditional requests, ordering, and draft exclusion.

## Task 7: Add backend API verification

Goal: protect project behavior at the use-case and HTTP boundaries.

- [x] Test authorization for every Admin operation, including a wrong-subject token.
- [x] Test create, read, update, publish, unpublish, reorder, and delete happy paths.
- [x] Test field/list/request limits and invalid URL/image schemes.
- [x] Test publish rejects missing English or Portuguese content.
- [x] Test deletion rejects a published project.
- [x] Test stale revisions for update, status, reorder, and delete.
- [x] Test anonymous lists/details exclude all non-published records.
- [x] Test the three seeded projects match the migration contract in both locales.
- [x] Test failures leave prior data and order intact.
- [x] Use local deterministic tokens and the controlled test store; do not contact authentik or external infrastructure.

Acceptance criteria:

- [x] Tests fail if Admin authorization or public filtering is removed.
- [x] Tests detect content loss, URL breakage, ordering regressions, and lost-update behavior.

Task 7 verification currently covers the project HTTP and persistence boundaries with deterministic local JWTs, SQLite migrations, seeded parity, localization, cache validators, invalid input, authorization, lifecycle, concurrency, ordering, and public filtering tests. No Authentik or external service is contacted.

## Task 8: Extend the server-only Admin project boundary

Goal: expose typed project operations to the Admin UI without exposing authentication material.

- [x] Add strict project DTO/result parsing in a project-specific server-only module or cohesive extension of the existing boundary.
- [x] Add server-side functions/actions for list, get, create, update, publish, unpublish, reorder, and delete.
- [x] Forward only the server-side `Authorization: Bearer` token and required request headers.
- [x] Preserve timeout, cancellation, no-store, and safe error mapping behavior.
- [x] Model validation, unauthorized, forbidden, not-found, conflict, timeout, malformed-response, and unavailable results.
- [x] Invalidate public project list/detail caches only after successful committed mutations.
- [x] Add tests that malformed backend payloads fail closed.
- [x] Add tests proving no JWT or protected response is serialized to Client Component props or HTML.

Acceptance criteria:

- [x] All Admin project mutations are server-mediated and strictly typed.
- [x] Conflict and validation states remain actionable without revealing internals.
- [x] The browser never receives OIDC access, refresh, or ID tokens.

Task 8 adds the server-only `admin-project-api` boundary and extends the frontend BFF allowlist for project operations. It validates response shapes and size, forwards only the server-held bearer token, uses bounded no-store requests, preserves safe status mapping, and revalidates public project paths only after successful state-changing upstream responses.

## Task 9: Build the Admin project list and editor

Goal: make project management clear, incremental, and accessible.

- [x] Add Projects to the existing Admin navigation and preserve `/admin` as the module home.
- [x] Add a Server Component project list showing identifier, localized title, status, order, and last revision/update time if available.
- [x] Add explicit actions for create draft, edit, publish/unpublish, reorder, and delete.
- [x] Build a focused editor with locale-independent fields separated from paired English/Portuguese fields.
- [x] Use simple repeatable controls for technologies and ordered features; do not add a form framework or data grid without a demonstrated need.
- [x] Show URL/image constraints and a preview of the selected image reference.
- [x] Prevent publish controls until client-known completeness checks pass, while still enforcing all rules on the server.
- [x] Require typed/explicit confirmation that identifies the project before permanent deletion.
- [x] Preserve unsaved form values on validation or conflict errors.
- [x] Handle loading, empty, success, validation, unauthorized, forbidden, not-found, conflict, timeout, and unavailable states.
- [x] Ensure keyboard operation, visible focus, accessible reorder controls, mobile/wide layouts, and reduced-motion behavior.

Acceptance criteria:

- [x] The administrator can complete each operation without editing source files.
- [x] Status, locale, and destructive actions are unmistakable.
- [x] Reordering works without pointer-only drag and drop.
- [x] No public navigation exposes the Admin module.

Task 9 adds the Projects module to the authenticated Admin shell, with Server Component list loading and client-mediated mutations through the BFF. The editor preserves local form state on failures, separates shared/project-localized fields, and provides keyboard-visible reordering, lifecycle state, empty/error states, and responsive styling. Frontend lint, type-check, tests, production build, and the Impeccable detector pass.

## Task 10: Cut public project pages over to managed data

Goal: remove duplicated hard-coded project data after proving compatible public reads.

- [x] Add a small server-only public project reader with bounded timeout and explicit cache behavior.
- [x] Render the homepage project list from the localized published endpoint.
- [x] Render project-detail routes from the localized published-detail endpoint.
- [x] Keep pages/layouts as Server Components where browser state is not required; remove the detail page's client dependency if no longer needed.
- [x] Preserve `/[locale]/project/[id]`, current link behavior, metadata, headings, image sizing, external-link safety, and missing-project behavior.
- [x] Replace hard-coded total counts such as `03` with derived accessible output.
- [x] Remove the duplicated project arrays only after parity tests pass.
- [x] Remove migrated `projects.project_*` catalog entries only after all callers use project records; retain shared project UI labels in the locale catalogs.
- [x] Define an actionable, non-sensitive unavailable state that does not present stale drafts or raw errors.
- [x] Invalidate/revalidate list and affected detail paths after successful Admin changes.

Acceptance criteria:

- [x] The current three projects look and behave as before in English and Portuguese.
- [x] A newly published project appears in the configured order and has a working detail URL.
- [x] Drafts/unpublished projects never appear publicly.
- [x] Project data has one authoritative source after migration.

Task 10 replaces both hard-coded project arrays with the server-only public project reader and the published localized API. The detail page is a Server Component, count output is derived from the published list, public cache tags are explicit, and failed reads show localized safe copy without stale or draft data. Reader tests and the production build pass.

## Task 11: Validate and document the complete project capability

Goal: verify the full lifecycle, migration, public compatibility, and operations.

- [x] Run backend locked restore, build, and tests from `backend/`.
- [x] Run frontend lint, type-check, tests, and build from `frontend/`.
- [x] Compare all seeded project fields and rendered pages against the pre-migration content in both locales.
- [x] Smoke-test draft creation, bilingual editing, publish, reorder, unpublish, and confirmed delete through the deterministic HTTP test host.
- [x] Verify stale-tab conflicts preserve the committed version and the user's unsaved form values.
- [x] Verify malformed URLs, oversized requests, incomplete translations, and published deletion are rejected.
- [x] Verify unavailable persistence/backend states are safe and actionable.
- [x] Verify keyboard-only, mobile, wide, light, dark, and reduced-motion Admin behavior through accessible controls, responsive/reduced-motion styles, component tests, and the UI detector.
- [x] Verify logs contain no JWTs, raw claims, secrets, full project bodies, or internal database/provider errors through source and logging-template review.
- [x] Document migrations, backup/restore, rollback, and how deployed image assets are added under the initial media policy.

Task 11 validation completed on 2026-09-09. Backend locked restore/build/tests passed; frontend install/lint/type-check/tests/build passed; Compose variable interpolation passed with placeholders; and `git diff --check` reported no whitespace errors. Live Authentik sign-in, production Komodo values, provider logs, and a real device matrix remain deployment checks in `ADMIN_PANEL_TASKS.md`, not repository-local project checks.

## Project capability definition of done

- [x] Admin can add, update, order, publish/unpublish, and safely remove project records.
- [x] Published project data is localized, complete, validated, durable, and served from one source.
- [x] Existing identifiers, URLs, content, layout, accessibility, and public behavior are preserved through migration.
- [x] Drafts and mutations remain protected by backend authorization and server-only frontend mediation.
- [x] Concurrency and destructive-operation safeguards are covered by tests.
- [x] Applicable checks pass, or pre-existing blockers are reported precisely.
