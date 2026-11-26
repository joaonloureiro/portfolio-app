# Admin email-delivery configuration tasks

This backlog makes the portfolio contact-email behavior safely configurable from the protected Admin surface while keeping transport credentials and secret rotation in deployment configuration.

Complete one numbered task at a time. Give a subagent one numbered task, review its diff and validation evidence, then check it before assigning the next one. Tasks that alter persistence, `Program.cs`, shared Admin navigation, the public contact endpoint, or SMTP Infrastructure must not run in parallel.

## Confirmed scope and trust boundary

> Status (2026-09-09): implementation-backed checks and automated verification are complete through the capability code. The remaining unchecked items are controlled-environment, production, or visual/manual verification and are intentionally not marked until performed.

Admin-managed delivery policy:

- enable or disable contact delivery;
- recipient address;
- sender display name;
- subject prefix;
- the revision used for optimistic concurrency.

Deployment-managed transport configuration:

- SMTP host and port;
- TLS/security mode;
- authentication username and password;
- envelope/from address;
- connection/send timeout limits.

The Admin UI may report whether transport is configured and may trigger a bounded test to the configured recipient. It must never return the SMTP password, raw secret values, JWT, connection string, or provider error. It must not accept an arbitrary test recipient.

Arbitrary HTML/body templates, credential rotation, multiple providers, attachments, queues, delivery history, inbound mail, marketing/bulk mail, and automatic retries are deferred. Automatic retries can create duplicate contact messages and require an explicit idempotency design.

## Shared persistence checkpoint

Admin-managed policy must survive restart. Before Task 3, inspect whether another Admin capability has established durable storage.

- Reuse its approved provider, migration flow, connection configuration, backup strategy, and deterministic test-store conventions.
- If no durable store exists, complete Task 2 as a separate foundation task.
- Do not store SMTP credentials or connection secrets in the content database.
- Do not rely on an unmounted container filesystem.
- Use a focused boundary such as `IEmailDeliveryPolicyStore`; do not add a generic settings repository/framework.
- Only one subagent may own persistence bootstrap or migrations at a time.

## Application and security rules

- Application owns the delivery-policy use cases and external boundaries; Infrastructure owns MailKit and persistence; API owns request validation, authorization, rate limiting, and HTTP mapping.
- Keep provider-specific types out of Domain and Application.
- Validate runtime transport options at startup and validate Admin policy at every write.
- Keep the public `/api/email/send` anonymous but abuse-resistant and bounded.
- Every settings read/mutation/test operation inherits `PortfolioAdmin`.
- Keep OIDC tokens inside the encrypted server-side session and server-mediate all Admin mutations.
- Encode untrusted contact values separately for text and HTML email bodies; do not interpolate raw input into HTML.
- Do not log contact message bodies, sender addresses, recipient addresses, credentials, JWTs, raw claims, or provider response text.
- Use optimistic concurrency for policy updates.
- Return stable `ProblemDetails` error keys without exposing SMTP/provider internals.

## Task 1: Freeze the delivery and configuration contract

Goal: define which behavior can change at runtime and how failures appear before modifying the sender.

- [x] Inventory `SmtpSettings`, `SmtpEmailSender`, `SmtpHealthCheck`, `/api/email/send`, frontend contact handling, tests, and runtime configuration.
- [x] Confirm the Admin-managed and deployment-managed field split above.
- [x] Define safe defaults for enabled state, sender display name, and subject prefix.
- [x] Define limits and validation for recipient/from addresses, display name, subject prefix, ports, timeouts, and total requests.
- [x] Choose explicit allowed TLS modes and forbid silent downgrade in production; replace implicit `StartTlsWhenAvailable` behavior according to that decision.
- [x] Define contact send timeout/cancellation behavior and stable public error keys for disabled, throttled, invalid, timed out, and unavailable delivery.
- [x] Define test-send semantics: fixed configured recipient, clearly marked subject/body, bounded rate, no arbitrary content/target, and no provider detail in the response.
- [x] Define whether public success keeps the current response shape exactly; coordinate any deliberate contract change with the frontend in the same numbered task.
- [x] Define health behavior so a public health response cannot leak host, account, recipient, exception, or provider details.
- [x] Document why automatic retry and arbitrary templates are excluded from the first version.

Acceptance criteria:

- [x] Every configurable field has an owner, validation rule, and secret-exposure decision.
- [x] Public and Admin error semantics are explicit.
- [x] No production behavior changes in this task.

## Task 2: Establish or adopt durable policy persistence

Goal: store only the safe Admin-managed delivery policy.

- [x] Inspect deployment and document the approved persistence provider, durability, backup, and restore assumptions.
- [x] Add only required provider packages and update lock files through restore commands.
- [x] Configure the connection through runtime configuration with startup validation and no committed secret.
- [x] Add the smallest Infrastructure registration and migration mechanism.
- [x] Add a deterministic isolated test-store strategy.
- [x] Document migration and rollback execution.
- [x] Reuse an equivalent store foundation from another capability rather than adding a second provider/context.
- [x] Verify the schema contains no SMTP host credential, username, password, JWT, or connection string.

Acceptance criteria:

- [x] Admin-managed policy survives application restart.
- [x] Secrets remain in the deployment secret/configuration boundary.
- [x] Domain/Application remain provider-independent and tests need no external service.

## Task 3: Separate transport options from delivery policy

Goal: make the current configuration ownership explicit and validated.

- [x] Replace mutable/ambiguous `SmtpSettings` with clearly named strongly typed transport options appropriate to Infrastructure.
- [x] Validate host, port, security mode, credentials when required, from address, and bounded timeouts at startup.
- [x] Keep secrets out of committed configuration examples; include safe option names/placeholders only.
- [x] Add a safe transport-readiness result that reports configured/not configured without returning secret values.
- [x] Add an Application delivery-policy model with enabled state, recipient, sender display name, subject prefix, and revision.
- [x] Add a focused policy-store abstraction and get/update use cases with optimistic concurrency.
- [x] Seed an initial policy that preserves the current recipient and subject behavior, applies the reviewed safe sender-display default, and is idempotent without overwriting later edits.
- [x] Add unit tests for option validation, policy limits, seed behavior, and revision conflicts.

Acceptance criteria:

- [x] Connection/authentication secrets and editable delivery policy are separate in code and storage.
- [x] Missing/invalid production transport configuration fails fast without printing secret values.
- [x] Existing effective recipient and subject behavior is preserved; any sender-display change is the explicit hardening decision from Task 1.

## Task 4: Harden contact-message validation and abuse controls

Goal: bound the public work performed before an SMTP connection is attempted.

- [x] Add explicit maximum lengths for name, email, and message in the backend validator.
- [x] Add a request-body size limit appropriate to those fields.
- [x] Normalize only safe whitespace needed by the contract; preserve meaningful message line breaks.
- [x] Add the built-in ASP.NET Core rate limiter to the contact endpoint with a small explicit policy.
- [x] Verify forwarded-client-IP trust uses known proxies/networks before keying a limiter by client address.
- [x] Return stable localized frontend error behavior for `400`, `413`, `429`, timeout, disabled, and unavailable results.
- [x] Ensure rejected/limited requests never call the email sender.
- [x] Do not add a CAPTCHA or external anti-bot provider without a separate privacy/UX requirement.
- [x] Add endpoint tests for boundary lengths, oversized bodies, rate limiting, and zero sender calls on rejection.

Acceptance criteria:

- [x] Public input and SMTP work are bounded by server-side controls.
- [x] The current valid contact journey still succeeds.
- [x] No new production dependency is required for basic rate limiting.

## Task 5: Make SMTP composition and delivery policy-driven

Goal: apply the validated policy safely when composing and sending a message.

- [x] Read the active policy through Application before attempting delivery.
- [x] Fail closed with the agreed stable error when delivery is disabled or policy is unavailable.
- [x] Compose recipient, display name, and subject prefix only from validated policy.
- [x] Preserve the visitor email as `Reply-To`; do not use it as the authenticated/envelope sender.
- [x] HTML-encode visitor name, email, and message before building the HTML body.
- [x] Preserve a correct plain-text alternative.
- [x] Use the explicit transport security mode and bounded connect/authenticate/send/disconnect cancellation.
- [x] Do not automatically retry an ambiguous send failure.
- [x] Translate expected timeout/configuration/transport failures into stable Application outcomes; let the API map them without provider details.
- [x] Replace success/error logs that contain recipient or personal data with stable structured operational events.
- [x] Add unit/Infrastructure tests using a fake or controlled in-process boundary; do not call live SMTP.

Acceptance criteria:

- [x] Admin policy changes affect subsequent sends without exposing or changing transport secrets.
- [x] Contact input cannot inject executable HTML into the email body.
- [x] Cancellation/timeouts are honored and provider exceptions do not cross the boundary.
- [x] One request produces at most one send attempt in this version.

## Task 6: Add protected email-settings endpoints

Goal: expose safe policy operations and a bounded test action.

- [x] Add `GET /admin/api/email-delivery` returning editable policy, revision, and secret-safe transport readiness only.
- [x] Add `PUT /admin/api/email-delivery` with expected-revision concurrency.
- [x] Add `POST /admin/api/email-delivery/test` that sends a fixed test message to the configured recipient.
- [x] Apply a strict test-send rate limit and prevent caller-supplied recipient, subject, or body.
- [x] Keep DTOs and validation in API and map explicitly.
- [x] Return `400` for invalid policy, `409` for stale revision, `429` for throttling, and `503`/`504` for safe unavailable/timeout outcomes.
- [x] Ensure all endpoints inherit `PortfolioAdmin` and protected responses use no-store.
- [x] Never serialize the password, credential values, full username, JWT, connection string, provider response, or exception text.
- [x] Add a stable audit-style operational log for settings changed/test requested without logging old/new addresses or other personal data.

Acceptance criteria:

- [x] Only the configured administrator can read/update policy or request a test.
- [x] The API cannot be used to probe or send to arbitrary email addresses.
- [x] Secret values and provider details are absent from success, error, and log output.

## Task 7: Make health checks safe and bounded

Goal: retain useful readiness information without making public health probes perform or reveal unsafe SMTP work.

- [x] Decide whether SMTP connectivity belongs in readiness only rather than every public liveness response.
- [x] Split liveness/readiness endpoints if required by the current runtime contract.
- [x] Bound SMTP health-check connection/authentication/disconnect time.
- [x] Use the same explicit security mode as normal delivery.
- [x] Avoid authenticating on overly frequent probes; use a documented check cadence/cache when the health system supports it.
- [x] Return only a coarse public status without exception, host, account, or recipient data.
- [x] Keep detailed diagnostics internal/protected or in privacy-safe structured logs.
- [x] Add tests for healthy, timeout, authentication failure, and redaction behavior without live SMTP.

Acceptance criteria:

- [x] Health endpoints are cheap/bounded enough for the documented probe cadence.
- [x] Public health output and logs expose no SMTP configuration or provider exception details.
- [x] Deployment health behavior remains documented and compatible.

## Task 8: Add backend integration verification

Goal: protect authorization, settings behavior, public compatibility, and sender isolation.

- [x] Test missing/invalid/wrong-subject/administrator authentication for all settings endpoints.
- [x] Test get/update happy paths and verify a subsequent send uses the new policy.
- [x] Test stale policy revision returns `409` and preserves the committed policy.
- [x] Test invalid addresses, display name/subject lengths, and request limits.
- [x] Test disabled delivery, throttling, timeout, unavailable policy, and transport failure public mappings.
- [x] Test the test endpoint always targets only the configured recipient and respects its rate limit.
- [x] Test visitor content is safely encoded in HTML and preserved in text.
- [x] Test cancellation and exactly one send attempt.
- [x] Test health output redaction.
- [x] Use fake sender/transport, deterministic policy store, local JWTs, and controlled time; never call SMTP or authentik.

Acceptance criteria:

- [x] Tests fail if authorization, secret redaction, recipient restrictions, encoding, concurrency, or abuse limits are removed.
- [x] The original valid public contact request remains covered.

## Task 9: Extend the server-only Admin email boundary

Goal: expose safe typed policy operations without leaking the JWT or transport secrets.

- [x] Add strict policy/readiness/result types and runtime payload parsing.
- [x] Add server-only get, update, and test functions/actions.
- [x] Forward only the server-side `Authorization: Bearer` token and required content headers.
- [x] Reuse bounded timeout, cancellation, no-store, and safe error mapping.
- [x] Model validation, unauthorized, forbidden, conflict, throttled, timeout, malformed-response, and unavailable results.
- [x] Ensure returned readiness is coarse and cannot reconstruct a username/password/host secret.
- [x] Add tests proving JWTs, secrets, provider text, and internal errors never reach props, HTML, or client-visible responses.

Acceptance criteria:

- [x] Settings operations are typed, server-mediated, and fail closed.
- [x] Admin can distinguish validation, conflict, throttling, timeout, and unavailable states safely.
- [x] Browser code never receives an SMTP credential or an OIDC token.

## Task 10: Build the Admin email-delivery settings UI

Goal: provide a clear operational control without presenting a misleading secret-management surface.

- [x] Add Email delivery to the existing Admin navigation only.
- [x] Render current policy and readiness in a Server Component.
- [x] Add accessible fields for enabled state, recipient address, sender display name, and subject prefix.
- [x] Clearly label SMTP credentials/connection as deployment-managed and show only a coarse configured/unavailable status.
- [x] Add an explicit save action with revision-aware conflict handling.
- [x] Add a separate test-delivery action explaining that it sends only to the configured recipient.
- [x] Require confirmation before disabling delivery.
- [x] Preserve unsaved values after validation/conflict failures.
- [x] Handle loading, success, validation, unauthorized, forbidden, conflict, throttled, timeout, malformed, and unavailable states.
- [x] Do not add password fields, reveal buttons, arbitrary recipient inputs, raw HTML templates, or provider-debug panels.
- [x] Ensure keyboard use, visible focus, useful touch targets, mobile/wide layouts, and reduced motion.

Acceptance criteria:

- [x] Admin can safely change delivery policy and send a bounded test without editing source.
- [x] The UI makes the deployment-managed secret boundary unmistakable.
- [x] Disabling delivery and sending a test are explicit, accessible actions.
- [x] No Admin controls or state appear in the public portfolio.

## Task 11: Coordinate frontend contact behavior

Goal: keep public contact feedback actionable for the newly explicit backend outcomes.

- [x] Map validation, oversized, throttled, disabled, timeout, and unavailable responses to stable public error keys.
- [x] Add synchronized English and Portuguese messages for any new public error keys.
- [x] Preserve existing form fields, success behavior, accessibility, and no-sensitive-error rule.
- [x] Prevent duplicate submission while a request is pending.
- [x] Keep focus management and live-region/toast feedback useful for keyboard and assistive-technology users.
- [x] Add frontend tests for each new status and for a successful send.
- [x] Do not reveal configured recipient, sender, SMTP readiness, or provider details publicly.

Acceptance criteria:

- [x] Public visitors receive concise localized next steps for expected failures.
- [x] The existing successful contact journey is unchanged from the visitor's perspective.
- [x] No private email-delivery setting is exposed by frontend error handling.

## Task 12: Validate and document the complete email capability

Goal: verify configuration, delivery, abuse resistance, secret handling, and operations end to end.

- [x] Run backend locked restore, build, and tests from `backend/`.
- [x] Run frontend lint, type-check, tests, and build from `frontend/`.
- [ ] In a controlled non-production environment, update each Admin-managed field and verify the next send uses it.
- [ ] Verify stale-tab conflict handling preserves the committed policy and unsaved form values.
- [ ] Verify disable/enable behavior and a rate-limited test delivery to the configured recipient.
- [ ] Verify request-size/rate limits, HTML encoding, timeout/cancellation, and one-attempt behavior.
- [ ] Verify liveness/readiness behavior under SMTP success, failure, and timeout.
- [ ] Search rendered output, logs, and API responses for JWTs, credentials, recipients, sender addresses, contact bodies, raw claims, and provider exceptions.
- [ ] Verify Admin UI keyboard/mobile/wide/light/dark/reduced-motion states and public form behavior in both locales.
- [ ] Update safe configuration examples and operator documentation for transport secrets, policy migration, backup/restore, health probes, rollback, and credential rotation outside Admin.

## Email-delivery capability definition of done

- [x] Admin can update the approved non-secret delivery policy and trigger a bounded fixed-recipient test.
- [x] SMTP transport secrets remain deployment-managed, startup-validated, and absent from Admin/API output and durable policy storage.
- [x] Public contact work is validated, size-limited, rate-limited, cancellable, safely encoded, and non-retrying.
- [x] Health checks are bounded and redact provider/configuration details.
- [x] Existing valid contact behavior and bilingual accessible feedback are preserved.
- [x] Authorization, optimistic concurrency, secret redaction, and failure mappings are covered by deterministic tests.
- [x] Applicable checks pass, or pre-existing blockers are reported precisely.
