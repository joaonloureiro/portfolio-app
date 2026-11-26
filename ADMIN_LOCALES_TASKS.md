# Admin locale-content management tasks

This backlog adds protected management of the public portfolio's English and Portuguese copy. It extends the authenticated Admin shell defined in `ADMIN_PANEL_TASKS.md` and preserves the public locale routes.

Complete one numbered task at a time. Assign a subagent only one numbered task, review its diff and validation output, then check the task before starting the next one. Tasks that change persistence, `Program.cs`, the Admin navigation, or the public locale-loading path must not run in parallel.

## Confirmed scope

> Status (2026-09-09): implementation-backed checks and automated verification are complete through the locale capability code. The remaining unchecked items are controlled-environment, production, or visual/manual verification and are intentionally not marked until performed.

- The supported locale codes remain `en` and `pt`; the default remains `pt`.
- Admin manages the values of existing public copy, including ordered string-list values where the current catalog uses them.
- Admin does not add or remove locale codes, change locale routing, translate Admin copy, or edit project/post records through the locale module.
- Project translations belong to `ADMIN_PROJECTS_TASKS.md`; post translations belong to `ADMIN_POSTS_TASKS.md`.
- English and Portuguese must keep the same key/type shape and must be submitted and published together.
- Stored content is plain text. Do not add arbitrary HTML, scripts, or a rich-text editor.

If changing the supported locale set or default locale is required later, create a separate deployment-aware backlog: those values affect middleware, static route generation, metadata, navigation, and cache behavior and are not merely content edits.

## Shared content-persistence checkpoint

The repository currently has no database or durable content store. Before Task 3, inspect whether another Admin capability has already established one.

- If a store exists, extend its current conventions and migration strategy.
- If none exists, complete Task 2 as a separately reviewed foundation change.
- Do not assume that a container's writable filesystem is durable. Record the selected provider, connection configuration, migration command, volume/backup expectation, and local-test strategy before implementation.
- Do not introduce a generic repository. Use a capability-specific boundary such as `ILocaleCatalogStore`.
- Only one subagent may own persistence bootstrap or a schema migration at a time.

## Application and security rules

- Keep locale/content rules in Domain or Application, persistence in Infrastructure, protected DTOs/endpoints in API, and rendering/internationalization in the frontend.
- Every mutation must inherit the existing `PortfolioAdmin` policy and return RFC 7807 `ProblemDetails` with stable, non-sensitive error keys.
- Keep OIDC tokens inside the encrypted server-side session. Mutations must go through the existing Admin BFF; never fetch protected backend endpoints from a Client Component.
- Use optimistic concurrency so an older Admin page cannot silently overwrite a newer catalog revision.
- Validate locale code, key path, value type, string length, list length, and total request size at the backend boundary.
- Never accept unexpected catalog keys or mismatched English/Portuguese shapes.
- Preserve a safe built-in catalog for startup, tests, and a controlled fallback; never show raw keys or provider errors to public visitors.
- Invalidate only relevant public caches after a successful publish.

## Task 1: Freeze the editable catalog contract

Goal: turn the current message files into an explicit, testable management contract before adding storage.

- [x] Inventory every key and leaf type in `frontend/src/locales/en.json` and `frontend/src/locales/pt.json`.
- [x] Verify both catalogs have identical keys and corresponding leaf types.
- [x] Classify editable values as scalar string or ordered string list.
- [x] Exclude current `projects.project_*` values from the managed locale contract because project records will own them after project migration.
- [x] Reserve post content for the Posts capability rather than creating arbitrary post keys.
- [x] Define backend limits for key length/depth, scalar length, list item count/length, and complete request size.
- [x] Define the atomic update contract: one request contains both `en` and `pt` values for every changed key plus the expected revision.
- [x] Define stable error keys for invalid shape, unknown key, invalid value, revision conflict, and unavailable storage.
- [x] Add a small contract test or validation fixture that fails when the two built-in catalogs drift.
- [x] Record any intentionally non-editable keys and the reason near the contract.

Acceptance criteria:

- [x] A reviewer can see exactly which copy the module may change.
- [x] Arrays and scalar strings cannot be exchanged accidentally.
- [x] Projects, posts, Admin copy, locale routing, and the default locale remain out of scope.
- [x] No production behavior changes in this task.

## Task 2: Establish or adopt durable content persistence

Goal: provide the smallest durable storage foundation needed for managed catalogs.

- [x] Inspect deployment configuration and document the approved persistence provider and durability/backup assumptions.
- [x] Add only the provider packages required by the approved choice and update NuGet lock files through restore.
- [x] Configure the connection through runtime configuration with startup validation and no committed secret.
- [x] Add the minimal Infrastructure registration and schema/migration mechanism.
- [x] Add a deterministic test-store strategy that needs no external service or production credentials.
- [x] Prove that application startup and migrations fail with actionable, non-sensitive errors when configuration is invalid.
- [x] Document how an operator applies migrations before deploying application code that requires the new schema.
- [x] Do not add project, post, media, identity, role, audit-framework, or generic repository tables in advance.

Acceptance criteria:

- [x] Content survives application restarts under the documented runtime model.
- [x] Automated tests remain isolated and deterministic.
- [x] Domain and Application have no provider-specific dependency.
- [x] The provider choice and migration/backup responsibilities are explicit.

## Task 3: Model catalog revisions and application use cases

Goal: represent managed locale content without coupling use cases to JSON files or the database provider.

- [x] Add the smallest Domain/Application model for one synchronized bilingual catalog revision.
- [x] Preserve the fixed `en`/`pt` locale invariant and the catalog schema from Task 1.
- [x] Add a capability-specific persistence abstraction.
- [x] Add use cases to read the active catalog, read the Admin-editable catalog, and replace values conditionally on the expected revision.
- [x] Seed the first active revision from the existing catalogs without changing any public wording.
- [x] Make seed/bootstrap behavior idempotent and safe on an existing database.
- [x] Reject incomplete bilingual updates, unknown keys, invalid leaf types, and out-of-range values.
- [x] Return a new revision only after the complete update commits atomically.
- [x] Add focused unit tests for schema equality, limits, atomic bilingual updates, seed idempotency, and revision conflicts.

Acceptance criteria:

- [x] There is never an active half-English/half-Portuguese update.
- [x] A stale expected revision cannot overwrite a newer revision.
- [x] The initial stored content exactly matches the current public copy.
- [x] Use cases do not depend on ASP.NET Core, Next.js, or a concrete database provider.

## Task 4: Add protected Admin and anonymous public locale endpoints

Goal: expose managed copy through narrow API contracts.

- [x] Add `GET /admin/api/locales` to return editable keys, both locale values, and the current revision.
- [x] Add `PUT /admin/api/locales` to validate and atomically publish a bilingual update using the expected revision.
- [x] Add `GET /api/locales/{locale}` as an anonymous, read-only endpoint for active public messages.
- [x] Return `400` for invalid input, `404` for an unsupported locale, `409` for a revision conflict, and `503` for unavailable storage.
- [x] Keep transport DTOs in API and map explicitly.
- [x] Apply response-size limits and prevent caching of protected responses.
- [x] Give successful public catalog responses an explicit safe cache policy and revision/ETag.
- [x] Ensure the protected endpoints inherit `PortfolioAdmin` from the route group.
- [x] Do not return database metadata, internal exception text, authentication claims, or unpublished state.

Acceptance criteria:

- [x] Anonymous callers cannot read or mutate Admin contracts.
- [x] Public callers can read only the active catalog for a supported locale.
- [x] Concurrent Admin edits have deterministic conflict behavior.
- [x] Existing `/admin/api/me`, contact, health, and localized routes retain their contracts.

## Task 5: Add backend verification

Goal: lock down authorization, validation, concurrency, and public-read behavior.

- [x] Test missing, malformed, wrong-subject, and valid administrator authentication for the new Admin endpoints.
- [x] Test that the Admin route-group policy protects both read and update operations.
- [x] Test unsupported locale, unknown key, missing counterpart, mismatched type, excessive length, and excessive request size.
- [x] Test a successful bilingual update and subsequent anonymous reads for both locales.
- [x] Test a stale revision returns `409` and preserves the newer content.
- [x] Test a failed update leaves the active bilingual revision unchanged.
- [x] Test initial seeding is idempotent and never overwrites later Admin edits.
- [x] Use a controlled test store; do not call an external database, authentik, or another live service.

Acceptance criteria:

- [x] Tests fail if authorization, bilingual atomicity, validation, or concurrency protection is removed.
- [x] No test uses production content credentials or network access.

## Task 6: Extend the server-only Admin API boundary

Goal: let Server Components and server-side mutations use the locale endpoints without exposing the JWT.

- [x] Add locale-specific result and request types without `any` or unsafe assertions.
- [x] Add a server-only function to read the Admin catalog.
- [x] Add a server-side mutation to submit the expected revision and bilingual changes.
- [x] Forward only the server-side `Authorization: Bearer` token and required content headers.
- [x] Reuse the existing timeout/cancellation and no-store behavior.
- [x] Model validation, unauthorized, forbidden, conflict, timeout, malformed-response, and unavailable results explicitly.
- [x] Map stable backend error keys to concise English Admin messages.
- [x] Invalidate the relevant public locale cache only after a successful update.
- [x] Add tests proving the JWT and backend-only details never enter returned props, HTML, or client-visible error payloads.

Acceptance criteria:

- [x] Locale management works through a typed server-only boundary.
- [x] A conflict is distinguishable from validation and availability failures.
- [x] No protected request is issued directly by browser JavaScript.

## Task 7: Build the Admin locale editor

Goal: provide a focused, accessible bilingual copy editor.

- [x] Add a Locales destination to the existing Admin navigation without adding it to public navigation.
- [x] Render the initial catalog in a Server Component.
- [x] Group fields by the existing message namespace and display the stable key path as context.
- [x] Show English and Portuguese inputs together for each value.
- [x] Render ordered string lists as simple repeatable rows with explicit add, remove, and reorder controls.
- [x] Use a focused Client Component only for form interaction, dirty state, and submission feedback.
- [x] Warn before discarding unsaved changes during in-module navigation where feasible without a global state library.
- [x] Show field-level limits and accessible validation errors.
- [x] Require explicit confirmation before publishing the complete change set.
- [x] Handle loading, empty, validation, conflict, unauthorized, forbidden, timeout, and unavailable states.
- [x] On conflict, preserve the user's unsaved values and offer a clear reload action; do not silently retry the write.
- [x] Support keyboard use, visible focus, narrow/mobile layouts, wide layouts, and reduced motion.

Acceptance criteria:

- [x] An administrator can update English and Portuguese copy together and confirm success.
- [x] Every input has an accessible label that identifies locale and content key.
- [x] No locale code, routing setting, project record, or post record can be changed here.
- [x] Public visitors and bundles receive no Admin code or identity data beyond existing route boundaries.

## Task 8: Read managed catalogs in the public Next.js app

Goal: make published Admin changes visible without regressing localization or resilience.

- [x] Add a small server-only public-catalog reader with a bounded timeout and explicit cache/revalidation behavior.
- [x] Validate the backend response before passing messages to `next-intl`.
- [x] Use the managed active catalog for `en` and `pt` request rendering.
- [x] Keep a known-good built-in catalog as a controlled fallback when storage/backend access is unavailable or malformed.
- [x] Log only a stable operational event for fallback use; do not log catalog bodies or internal response details.
- [x] Preserve `generateStaticParams`, locale middleware, `/en/**`, `/pt/**`, the `pt` default, metadata, and language switching.
- [x] Prove cache invalidation makes a successful Admin update visible within the documented bound.
- [x] Remove obsolete catalog-loading code only after both locales render successfully through the new path.
- [x] Do not move public layouts or pages to Client Components.

Acceptance criteria:

- [x] Valid managed changes render in both public locales.
- [x] Backend/store failure serves the last known built-in copy rather than raw keys or a broken page.
- [x] Locale routes, metadata, hydration, and language switching behave as before.
- [x] The Admin JWT is never sent on anonymous locale reads.

## Task 9: Validate the complete locale capability

Goal: verify the migration, editing journey, public rendering, and unchanged boundaries.

- [x] Run backend restore, build, and tests from `backend/` using the locked-mode sequence in `AGENTS.md`.
- [x] Run frontend lint, type-check, tests, and build from `frontend/` using the existing lock file.
- [ ] Verify the seeded English and Portuguese pages are text-for-text equivalent to the pre-migration pages.
- [ ] Update one scalar and one ordered-list value in a controlled environment and verify both locales.
- [ ] Verify stale-tab conflict handling does not lose either administrator's changes silently.
- [ ] Verify unavailable storage/backend behavior uses the safe public fallback and an actionable Admin error.
- [ ] Verify no Admin link appears publicly and no JWT, raw claim, secret, or catalog body appears in logs.
- [ ] Verify mobile, wide, keyboard-only, light, dark, and reduced-motion Admin states.
- [x] Document migration, rollback, backup, and recovery steps for locale content.

## Locale capability definition of done

- [x] The administrator can safely update existing English and Portuguese public copy together.
- [x] The locale set remains fixed to `en` and `pt`, with `pt` as the default.
- [x] Catalog shape, value limits, optimistic concurrency, authorization, and request size are enforced by the backend.
- [x] Public localized routes use valid active content and retain a controlled built-in fallback.
- [x] Projects and posts remain owned by their feature-specific models.
- [x] Applicable backend and frontend checks pass, or pre-existing blockers are reported precisely.
- [x] Persistence, migration, backup, rollback, and residual risks are documented.
