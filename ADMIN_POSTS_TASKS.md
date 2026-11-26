# Admin post-management tasks

This backlog adds the first real Field Notes publishing capability: protected post authoring and public bilingual reading without fabricating content.

Complete one numbered task at a time. Assign one numbered task to one subagent, review the implementation and validation, and only then check it off. Persistence/schema changes, shared Admin navigation changes, and public Field Notes cutover must not be implemented concurrently by separate subagents.

## Confirmed scope

- Admin can list, create, edit, preview, publish/unpublish, and remove posts.
- Posts use one stable locale-independent slug and paired English/Portuguese content.
- Drafts may be incomplete; publication requires complete content in both supported locales.
- Initial localized fields are title, excerpt, and body.
- Initial shared fields are slug, status, optional publication timestamp, and updated timestamp/revision.
- Bodies use Markdown without embedded raw HTML. Rendering must use a maintained parser/sanitizer with unsafe URL protocols and raw HTML disabled.
- Cover images, media upload, comments, categories, tags, scheduled publishing, revisions/history, feeds, search, analytics, and collaborative editing are deferred until requested.
- “Remove” means permanent deletion only for an unpublished post after explicit confirmation. A published post must first be unpublished.
- Do not create sample/filler articles. The public Posts destination becomes discoverable only when at least one real post is published.

## Shared content-persistence checkpoint

The current repository has no durable content store. Before Task 3, inspect whether Projects or Locales work already established one.

- Reuse the approved provider, migrations, connection configuration, backup policy, and deterministic test-store convention when present.
- If absent, complete Task 2 as a separately reviewed foundation change before adding post tables.
- Do not rely on a container-local writable filesystem for durability.
- Do not introduce a generic repository or CMS framework. Use a focused boundary such as `IPostStore`.
- Do not add schemas for future comments, media, tags, roles, or analytics.
- Only one subagent may own persistence bootstrap or migrations at a time.

## Application and security rules

- Domain owns slug/status/publication invariants; Application owns use cases and storage/clock abstractions; Infrastructure owns persistence; API owns DTOs, validation, authorization, and HTTP semantics.
- Admin draft reads and every mutation inherit the `PortfolioAdmin` policy.
- Anonymous endpoints return published posts only.
- Keep OIDC tokens inside the encrypted server-side session and use the `/admin/api/**` BFF for browser mutations.
- Validate slug, timestamps, Markdown/body sizes, locale completeness, request size, and revision at the server boundary.
- Never render raw Markdown as HTML with `dangerouslySetInnerHTML` unless output has been processed by an explicitly reviewed sanitizer; prefer renderer components that keep raw HTML disabled.
- Use optimistic concurrency for update, publish/unpublish, and delete.
- Inject time at the Application boundary where publication behavior needs deterministic tests.
- Store timestamps in UTC; render Admin times consistently in English using `America/Sao_Paulo` unless the product later selects another zone.

## Task 1: Freeze the post and public-route contract

Goal: define the smallest credible publishing model before selecting implementation details.

- [x] Confirm public routes as `/[locale]/posts` and `/[locale]/posts/[slug]`.
- [x] Define a lowercase slug grammar, length, uniqueness, reserved words, and immutability policy.
- [x] Define draft, published, and unpublished status transitions.
- [x] Define whether publication time is set automatically on first publish and preserved on later republish; do not add scheduling.
- [x] Define required English and Portuguese title, excerpt, and body fields for publication.
- [x] Define limits for title, excerpt, body, slug, list count, and total request size.
- [x] Define the supported Markdown subset and explicitly forbid raw HTML and unsafe link/image protocols.
- [x] Define public ordering, pagination threshold/defaults, and empty behavior.
- [x] Define stable error keys and HTTP results for invalid input, not found, slug conflict, stale revision, invalid transition, published-delete conflict, and unavailable storage.
- [x] Define the no-fabricated-content migration state: zero seeded posts and no public navigation link until the first publication.

### Frozen contract

This contract is intentionally implementation-neutral. It defines observable behavior for the first post capability; it does not authorize persistence, API, renderer, or UI work in Task 1.

#### Routes and localization

- The public collection route is `/[locale]/posts` and the public detail route is `/[locale]/posts/[slug]`.
- `locale` is limited to the existing supported locales `en` and `pt`. The route renders only the requested locale; it never falls back to the other locale or exposes both translations in one public response.
- `slug` is one stable, locale-independent identifier shared by both localized routes. Changing a title or translation never changes the slug.
- Unknown, deleted, draft, or unpublished posts are indistinguishable to anonymous detail readers and resolve as not found.

#### Slugs

- Slugs are trimmed, lowercase ASCII, 1–80 characters, and must match `^[a-z0-9]+(?:-[a-z0-9]+)*$`. Leading/trailing hyphens, repeated hyphens, whitespace, underscores, punctuation, and non-ASCII characters are invalid.
- Uniqueness is case-insensitive at the boundary and enforced against the canonical lowercase value. A slug is immutable after creation; correcting one requires a new post and does not create redirects in this capability.
- The reserved words are `admin`, `api`, `about`, `contact`, `en`, `favicon`, `health`, `notes`, `posts`, `project`, `pt`, `robots`, and `sitemap`. A reserved word cannot be used as the complete slug; future route additions must extend this list before implementation.

#### Lifecycle and timestamps

- A new post is always created as `draft`. Drafts may have incomplete localized content and are visible only to authorized Admin operations.
- The only valid status transitions are `draft -> published`, `published -> unpublished`, and `unpublished -> published`. There is no direct client-controlled status field.
- `publish` is allowed only when both `en` and `pt` have a non-empty title, excerpt, and body after trimming. `unpublish` requires the current status to be `published` and preserves all content.
- Draft and unpublished posts may be edited while incomplete. A published post may be edited, but an update must still leave both locales complete; otherwise the update is rejected and the published version remains unchanged.
- Permanent deletion is allowed only for `draft` or `unpublished` posts. Deleting a published post first requires a successful unpublish. Deletion always requires the expected current revision and explicit confirmation at the Admin boundary.
- `publishedAt` is assigned by the server's UTC clock on the first successful publish. It is never client-supplied, cleared by unpublish, or changed on later republish. Scheduling and future publication timestamps are out of scope.
- `updatedAt` is a server-generated UTC timestamp. Each successful content, lifecycle, or deletion mutation advances the optimistic-concurrency `revision`; stale revisions fail without changing the post.

#### Content and request limits

- Each locale has exactly `title`, `excerpt`, and `body` in the first version. Publication requires all six values for `en` and `pt`; no locale fallback or partial publication is permitted.
- Maximum lengths are: slug 80 characters, title 120 characters per locale, excerpt 320 characters per locale, and body 50,000 characters per locale. Limits are measured after trimming for required-field checks; request size is measured separately as UTF-8 bytes.
- Admin post lists default to 20 items and accept at most 50 items per page. Public lists default to 10 items and accept at most 20 items per page. Page numbers are positive 1-based integers. No endpoint returns an unbounded collection.
- JSON request bodies are limited to 256 KiB. Oversized bodies and any field or page-size violation use the invalid-input contract; the server must reject them before persistence or Markdown rendering.

#### Markdown safety

- Bodies accept only the first-version Markdown subset: paragraphs, ATX headings, emphasis, strong emphasis, unordered and ordered lists, blockquotes, fenced code blocks, inline code, and links/images with safe destinations.
- Raw HTML, HTML comments, embedded scripts/styles, tables, footnotes, task-list extensions, and arbitrary renderer extensions are not supported. Raw HTML is disabled in the eventual renderer and must not be emitted as executable markup.
- Link and image destinations must use `https://` URLs. Fragment-only links are allowed for links. `http://`, `javascript:`, `data:`, `vbscript:`, protocol-relative URLs (`//…`), relative URLs, and obfuscated/encoded variants are rejected or neutralized consistently by the later renderer contract.
- Markdown is stored as validated source, not as pre-rendered HTML. The first version adds no upload/media capability.

#### Public ordering, pagination, and empty state

- Published lists are ordered newest first by `publishedAt` descending, with the immutable post identifier ascending as the deterministic tie-breaker. The ordering is identical in both locales.
- Public pagination starts at 10 items per page and is required once more than 10 published posts exist; clients may request up to 20. The response reports whether another page exists so clients do not infer completeness from item count.
- An empty public collection returns a successful bounded empty list, not sample content. A public detail request for any unavailable post returns not found.
- The public Posts routes may exist before publication, but they must not be linked from public navigation or the Field Notes section until at least one real post is published.

#### Stable errors and HTTP results

All protected and public post operations use safe `ProblemDetails` responses where an error body is applicable. The stable machine-readable key is carried as `errorKey`; messages must not expose provider details, claims, tokens, or full Markdown bodies.

| Condition | HTTP result | `errorKey` |
| --- | ---: | --- |
| Malformed, invalid, oversized, incomplete, or unsupported input | `400` | `post_invalid_input` |
| Unsupported locale in a post API request | `400` | `post_unsupported_locale` |
| Unknown, deleted, draft, or unpublished public post | `404` | `post_not_found` |
| Missing Admin post by identifier | `404` | `post_not_found` |
| Slug already used by another post | `409` | `post_slug_conflict` |
| Expected revision is stale | `409` | `post_stale_revision` |
| Status operation is not valid for the current status | `409` | `post_invalid_transition` |
| Delete requested while the post is published | `409` | `post_published_delete_conflict` |
| Store or required persistence dependency is unavailable | `503` | `post_storage_unavailable` |

Unauthorized and forbidden Admin responses retain the existing authentication boundary and are not replaced by post-specific errors. No post error reveals whether an anonymous request failed because a record is draft, unpublished, deleted, or unknown.

#### Empty migration state

- Task 1 seeds zero posts and makes no production behavior change.
- No sample, placeholder, or fabricated article is created to exercise the routes.
- Existing Field Notes presentation remains unchanged until a real post is published. Only then may the public navigation and Field Notes section expose the localized Posts destination.

Acceptance criteria:

- [x] Route, slug, Markdown, publication, deletion, ordering, and localization rules are unambiguous.
- [x] The first implementation does not imply scheduling, history, media, taxonomy, or comments.
- [x] No production behavior changes in this task.

## Task 2: Establish or adopt durable content persistence

Goal: provide the smallest durable store required for post authoring.

- [x] Inspect the deployment model and record the approved provider, volume/durability, backup, and restore responsibilities.
- [x] Add only required provider packages and update lock files using restore.
- [x] Configure and validate the connection from runtime configuration without committing secrets.
- [x] Add the minimal Infrastructure registration and migration mechanism.
- [x] Add an isolated deterministic test-store strategy.
- [x] Document how migrations and rollback run before deployment.
- [x] Reuse an equivalent foundation from another Admin content capability instead of adding a second provider or context.
- [x] Do not add speculative tables or a generic CRUD/data-access framework.

Acceptance criteria:

- [x] Post data survives application restart under the documented runtime model.
- [x] Tests require no production credentials or external service.
- [x] Domain and Application remain provider-independent.

## Task 3: Model posts and application use cases

Goal: implement post lifecycle rules independently of HTTP and persistence details.

- [x] Add a post aggregate/entity with immutable identifier/slug policy, revision, status, UTC timestamps, and localized title/excerpt/body.
- [x] Represent supported locales explicitly rather than using magic `TitleEn`/`TitlePt` branching throughout the code.
- [x] Enforce slug grammar/uniqueness, status transitions, timestamp rules, and bilingual publication completeness.
- [x] Add a focused post-store abstraction.
- [x] Add use cases to list Admin posts, get one draft/post, create a draft, update, preview input, publish, unpublish, and delete an unpublished post.
- [x] Add anonymous use cases to list and read published localized posts.
- [x] Add a small time abstraction only if required for deterministic publication timestamps.
- [x] Pass `CancellationToken` through persistence I/O.
- [x] Add unit tests for slug rules, bilingual completeness, status transitions, timestamp behavior, public filtering, and optimistic concurrency.

Acceptance criteria:

- [x] Incomplete or invalid content cannot be published.
- [x] Draft/unpublished content cannot flow through anonymous use cases.
- [x] A stale revision cannot overwrite or delete a newer post.
- [x] Use cases have no ASP.NET Core, Markdown-renderer, or provider dependency.

## Task 4: Implement post persistence

Goal: durably store the exact state required by current post use cases.

- [x] Add only post and localized-post persistence mappings plus the required migration/schema.
- [x] Enforce unique slug and concurrency constraints at the appropriate persistence boundary.
- [x] Preserve Markdown source exactly within validated bounds.
- [x] Keep create/update/status/delete operations atomic.
- [x] Start with no seeded posts.
- [x] Implement stable public ordering by publication time with a deterministic identifier tie-breaker.
- [x] Add Infrastructure tests for round trips, locale content, uniqueness, ordering, transitions, concurrency, and deletion.

Acceptance criteria:

- [x] An empty database has no fabricated post content.
- [x] Duplicate slugs and stale writes fail deterministically.
- [x] Failed operations do not leave partially updated translations or status.

## Task 5: Add protected Admin post endpoints

Goal: expose explicit authoring operations under the existing protected group.

- [x] Add `GET /admin/api/posts` with bounded pagination or the agreed bounded list contract.
- [x] Add `GET /admin/api/posts/{id}`.
- [x] Add `POST /admin/api/posts` to create a draft.
- [x] Add `PUT /admin/api/posts/{id}` with the expected revision.
- [x] Add explicit publish and unpublish operations.
- [x] Add `DELETE /admin/api/posts/{id}` for explicitly confirmed, unpublished deletion.
- [x] Keep preview out of the Task 5 HTTP surface because no server renderer exists yet; the Application layer exposes a non-persisting preview validator for future safe rendering.
- [x] Validate DTOs at the API boundary and map explicitly.
- [x] Return `201`, `400`, `404`, `409`, and `503` according to the Task 1 contract.
- [x] Disable protected-response caching and return safe `ProblemDetails` without Markdown bodies in logs/errors.

Acceptance criteria:

- [x] Every draft read and mutation inherits the `PortfolioAdmin` policy.
- [x] Publication and deletion rules cannot be bypassed through a request DTO.
- [x] Protected responses reveal no provider or authentication internals.

## Task 6: Add anonymous published-post endpoints

Goal: supply localized post lists and detail content to the public frontend.

- [x] Add `GET /api/posts?locale={locale}` with bounded pagination and published summaries only.
- [x] Add `GET /api/posts/{slug}?locale={locale}` with published detail only.
- [x] Return only the requested locale and public fields.
- [x] Return `404` for drafts, unpublished, deleted, and unknown posts without revealing which case occurred.
- [x] Apply explicit public cache headers/revision ETags.
- [x] Keep endpoints anonymous without weakening Admin authorization.
- [x] Bound page size and response body size through bounded pagination and the domain’s published field limits.
- [x] Do not return pre-rendered unsafe HTML from the API in the first version.

Acceptance criteria:

- [x] Anonymous clients can never enumerate or read drafts.
- [x] Published posts have stable ordering and localized content.
- [x] An empty store returns an empty bounded list and no fabricated article.

## Task 7: Add backend verification

Goal: verify lifecycle, localization, authorization, and public filtering.

- [x] Test missing/invalid/wrong-subject/administrator authentication for all Admin operations.
- [x] Test create, edit, publish, unpublish, and delete happy paths.
- [x] Test invalid/duplicate/reserved slugs and immutable-slug behavior.
- [x] Test Markdown/body/request-size limits and forbidden publication when a locale is incomplete.
- [x] Test publication timestamp and republish behavior with a controlled clock.
- [x] Test stale revisions for update, status transitions, and delete.
- [x] Test published deletion is rejected.
- [x] Test anonymous list/detail exclude drafts and unpublished posts in both locales.
- [x] Test empty-store behavior.
- [x] Use deterministic tokens, time, and test storage with no live authentik or network dependency.

Acceptance criteria:

- [x] Tests fail if Admin authorization, lifecycle rules, bilingual completeness, or public filtering is removed.
- [x] No test includes production credentials or fabricated public articles.

## Task 8: Select and verify safe Markdown rendering

Goal: render useful post content without introducing an XSS or maintenance burden.

- [x] Evaluate existing framework capability and one maintained minimal renderer before adding a dependency.
- [x] Record package maintenance, license, bundle/runtime impact, lock-file change, and why a simpler existing option is insufficient.
- [x] Keep rendering on the server where practical.
- [x] Disable raw HTML.
- [x] Reject or neutralize `javascript:`, `data:`, protocol-relative, and other unsupported URLs.
- [x] Add accessible heading, link, list, quote, inline-code, and code-block rendering.
- [x] Ensure external links use safe relationship attributes without forcing every link into a new tab.
- [x] Add tests with script tags, event handlers, encoded protocols, malformed Markdown, oversized code blocks, and normal supported content.
- [x] Do not build a regex-based Markdown parser or sanitizer.

Acceptance criteria:

- [x] Supported Markdown renders predictably in server output.
- [x] Raw HTML and unsafe URLs cannot become executable browser content.
- [x] The dependency decision is explicit and the public client bundle remains proportionate.

## Task 9: Extend the server-only Admin post boundary

Goal: expose typed authoring operations without leaking OIDC tokens.

- [x] Add strict post/result parsing in a post-specific server-only module or cohesive boundary extension.
- [x] Add server-side functions/actions for list, get, create, update, publish, unpublish, and delete.
- [x] Forward only the required JWT and content headers.
- [x] Reuse bounded timeout, cancellation, no-store, and safe error mapping.
- [x] Model validation, unauthorized, forbidden, not-found, conflict, timeout, malformed-response, and unavailable results.
- [x] Invalidate public post list/detail caches only after successful mutations.
- [x] Add tests for malformed backend payloads and safe errors.
- [x] Prove JWTs and draft data not needed by an interactive control are absent from client props and HTML.

Acceptance criteria:

- [x] All authoring operations are server-mediated and strictly typed.
- [x] Draft bodies and JWTs remain outside client-visible responses except the body intentionally loaded into the protected editor.
- [x] Conflict states preserve actionable context without revealing internals.

## Task 10: Build the Admin post list and editor

Goal: provide a small, accessible writing workflow.

- [x] Add Posts to the existing Admin navigation without adding a public link yet.
- [x] Add a Server Component list with title, slug, status, publication time, and revision/update context.
- [x] Add explicit create, edit, preview, publish/unpublish, and delete actions.
- [x] Build paired English/Portuguese title, excerpt, and Markdown body fields.
- [x] Use plain textareas and a server-safe preview; do not add a rich-text/editor framework.
- [x] Show Markdown help for the supported subset and field limits.
- [x] Require explicit confirmation before publish/unpublish and identifier-specific confirmation before permanent delete.
- [x] Preserve unsaved values on validation or concurrency errors.
- [x] Warn about incomplete locale content before publication while retaining server enforcement.
- [x] Handle loading, empty, success, validation, unauthorized, forbidden, not-found, conflict, timeout, and unavailable states.
- [x] Ensure keyboard access, visible focus, logical tab order, accessible preview semantics, mobile/wide layouts, and reduced motion.

Acceptance criteria:

- [x] An administrator can draft and safely preview both locales without editing source.
- [x] Publication state and destructive actions are unmistakable.
- [x] Unsafe Markdown does not execute in preview.
- [x] No public navigation changes until a real post is published.

## Task 11: Add public Posts and Field Notes integration

Goal: expose real published writing as a natural continuation of the existing public design.

- [x] Add server-only public post readers with bounded timeouts and explicit cache behavior.
- [x] Add localized `/[locale]/posts` and `/[locale]/posts/[slug]` Server Component routes.
- [x] Render localized metadata, semantic article structure, publication time, and safe Markdown.
- [x] Preserve canonical/hreflang behavior and return the framework's not-found result for unavailable public records.
- [x] Keep the existing Field Notes “coming soon” state while no post is published.
- [x] When at least one real post is published, replace the coming-soon state with a localized, crawlable link/list derived from published data.
- [x] Add shared UI labels to both existing public locale catalogs and keep their keys synchronized.
- [x] Provide a non-sensitive unavailable state; never fall back to a draft or expose backend errors.
- [x] Revalidate lists, details, metadata, and Field Notes after publish/unpublish/update/delete.
- [x] Preserve the Living Atlas design, mobile behavior, keyboard use, focus visibility, and reduced-motion behavior.

Acceptance criteria:

- [x] No empty Posts destination is promoted publicly.
- [x] The first real published post becomes discoverable in both locales with safe rendered content.
- [x] Drafts and unpublished posts return no public content.
- [x] Public pages remain Server Components and carry no Admin identity data.

## Task 12: Validate and document the complete post capability

Goal: verify an empty launch, full authoring lifecycle, safe rendering, and operations.

- [x] Run backend locked restore, build, and tests from `backend/`.
- [x] Run frontend lint, type-check, tests, and build from `frontend/`.
- [x] Verify the initial empty state keeps the current Field Notes presentation and exposes no empty navigation destination.
- [x] In a controlled environment, draft bilingual content, preview it, publish it, edit it, unpublish it, and delete it.
- [x] Verify stale-tab conflicts do not silently overwrite newer content.
- [x] Verify incomplete translations, unsafe Markdown/URLs, oversized requests, and published deletion fail safely.
- [x] Verify public list, detail, metadata, not-found, and cache invalidation behavior in both locales.
- [x] Verify Admin and public pages with keyboard-only, mobile, wide, light, dark, and reduced-motion states.
- [x] Verify logs contain no JWT, raw claims, secrets, full draft bodies, or internal provider errors.
- [x] Document migrations, backup/restore, rollback, Markdown support, and publication recovery.

## Post capability definition of done

- [x] Admin can add, update, preview, publish/unpublish, and safely remove bilingual posts.
- [x] Zero fabricated posts are seeded or displayed.
- [x] Drafts remain protected and published routes expose only localized public fields.
- [x] Markdown output is tested against executable HTML and unsafe URLs.
- [x] Optimistic concurrency and destructive-operation safeguards are enforced.
- [x] Public Field Notes changes only when real published content exists.
- [x] Applicable checks pass, or pre-existing blockers are reported precisely.
