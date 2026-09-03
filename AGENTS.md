# Portfolio engineering guide

## Purpose and scope

This repository is the main personal portfolio. Improve it incrementally: leave each touched area clearer, safer, easier to test, and no more complicated than the product currently needs.

These instructions apply to the whole repository. Keep durable engineering guidance in this file instead of creating a new policy document for each problem. Update this file only when a convention is broadly useful across future work.

## Start every task with evidence

1. Read the relevant source, nearby tests, package/project manifests, and deployment configuration before proposing a change.
2. Treat executable configuration and source as authoritative when documentation is stale.
3. Check the working tree and preserve unrelated user changes.
4. State material assumptions. Ask only when a choice is genuinely product-defining, destructive, or impossible to infer safely.
5. Make the smallest coherent change that solves the root problem. Avoid drive-by rewrites.
6. Validate the narrowest affected area first, then the broader application when practical.

## Current architecture

- `backend/` is ASP.NET Core targeting `net8.0`, organized into API, Application, Domain, and Infrastructure projects. `Program.cs` is the composition root and currently exposes minimal APIs.
- `frontend/` is Next.js 15 App Router with React 19, strict TypeScript, `next-intl`, Tailwind CSS 4, and Framer Motion.
- `.gitea/workflows/deploy.yml` is the deployment workflow. PM2 and the ecosystem files are part of the current runtime model.
- The root README contains older backend details. Do not infer the active stack from prose alone; verify manifests and source.
- The current frontend is React/Next.js, not Angular. Do not introduce Angular packages or Angular-shaped patterns unless a task explicitly adds an Angular application. If Angular is added later, follow the Angular rules near the end of this file.

## Engineering principles

- KISS: prefer direct, readable code and standard framework features.
- YAGNI: do not add extension points, generic frameworks, repositories, message buses, state libraries, or configuration that no current requirement uses.
- SOLID is a design aid, not a quota for interfaces or classes. Separate responsibilities where they change for different reasons; introduce abstractions at real boundaries such as email delivery, persistence, time, or external services.
- Keep coupling low and cohesion high. Put behavior with the data or feature it belongs to.
- Prefer composition over inheritance and explicit dependencies over service location or hidden global state.
- Remove duplication only when the shared concept is stable. A small amount of obvious repetition is better than the wrong abstraction.
- Optimize for maintainers reading the code. Names should explain intent; comments should explain non-obvious reasons or constraints.
- Preserve existing behavior unless the task intentionally changes it. Keep refactors and behavior changes separately reviewable when practical.

## Backend: C# and ASP.NET Core

### Boundaries

- Domain contains business concepts and invariants and must not depend on ASP.NET Core, SMTP, persistence, or other infrastructure.
- Application coordinates use cases and owns the abstractions required by those use cases. It may depend on Domain, not Infrastructure or API.
- Infrastructure implements external concerns such as SMTP. Keep provider-specific types and configuration inside this layer.
- API owns transport concerns, request/response contracts, middleware, endpoint mapping, authentication, and authorization. It composes dependencies but should not contain reusable business rules.
- Dependencies point inward. Do not bypass a boundary for convenience.
- Keep `Program.cs` understandable. Extract cohesive registration or endpoint groups when it becomes difficult to scan, not merely to reduce line count.

### C# conventions

- Respect nullable reference types and fix warnings deliberately; do not suppress them broadly.
- Use clear domain names, PascalCase for public members/types, camelCase for locals/parameters, and `Async` on genuinely asynchronous methods.
- Prefer immutable records for value-like contracts and options when that improves clarity. Do not convert entities mechanically.
- Accept `CancellationToken` at I/O boundaries and pass it through.
- Use asynchronous APIs for network, disk, and database work. Avoid `.Result`, `.Wait()`, fire-and-forget tasks, and unnecessary `Task.Run`.
- Use dependency injection and options binding. Validate required configuration at startup.
- Keep methods focused and guard invalid state early. Avoid deep nesting, boolean flag arguments, magic strings, and catch-all utility classes.
- Catch exceptions only when adding context, translating an expected failure, compensating, or producing the correct boundary response. Never silently swallow failures.
- Use structured logging with stable message templates. Do not interpolate secrets or unnecessary personal data into logs.
- Keep NuGet lock files consistent. Do not upgrade the SDK, target framework, or packages incidentally.

### API behavior

- Validate every untrusted request at the server boundary. Enforce length, format, range, and required-field limits even when the UI also validates.
- Use consistent HTTP semantics and RFC 7807 `ProblemDetails` responses. Do not expose stack traces, provider errors, internal hostnames, or implementation details.
- Keep transport DTOs separate from domain/infrastructure types. Map explicitly when behavior or security boundaries matter.
- Keep public endpoints abuse-resistant. For the contact endpoint, consider rate limiting, request-size limits, timeouts, anti-automation controls, and bounded SMTP work before adding richer behavior.
- Version or preserve compatibility for externally consumed contracts. Coordinate frontend and backend changes in the same task when a contract changes.
- Health checks should be cheap, bounded, and avoid leaking sensitive configuration.

## Frontend: Next.js, React, and TypeScript

### Structure and componentization

- Follow App Router conventions. Prefer Server Components; add `"use client"` only where browser APIs, client state, effects, or event handlers require it.
- Organize by feature or route. Keep route files focused on composition and move reusable feature behavior into nearby components, hooks, or services.
- A component should have one clear responsibility. Split components when independent behavior, reuse, testing, or readability improves—not according to an arbitrary line count.
- Prefer composition and explicit props. Avoid prop drilling across many layers; use context only for genuinely shared, stable state.
- Keep state as local as possible. Derive values instead of synchronizing duplicate state, and do not use effects for pure calculations.
- Keep network access behind a small typed boundary. Model loading, empty, success, validation, and failure states explicitly.
- Preserve strict typing. Avoid `any`, unsafe assertions, and non-null assertions unless the invariant is proven and documented nearby.
- Use semantic HTML first. Add ARIA only where native semantics are insufficient.

### Next.js and user experience

- Preserve server/client boundaries and avoid sending secrets or unnecessary data to the browser. Only variables intentionally public may use the `NEXT_PUBLIC_` prefix.
- Use framework image, font, metadata, routing, and loading/error facilities where appropriate.
- Keep client bundles small. Do not move an entire page to the client for one interactive child; lazy-load expensive optional UI.
- Avoid layout shift and animation that blocks interaction. Respect `prefers-reduced-motion`.
- Every interactive control must work with a keyboard, have visible focus, and expose an accessible name. Maintain useful contrast and touch target sizes.
- Treat mobile, narrow, wide, light, and dark layouts as supported states.
- All user-facing copy belongs in the existing internationalization system. Keep English and Portuguese message keys synchronized and do not assemble translated sentences from fragments.
- Preserve SEO basics: meaningful headings, metadata, link text, canonical behavior, and crawlable primary content.
- Display actionable, non-sensitive errors. Never render raw server exceptions.

## Authentication, authorization, and security

- Authentication proves identity; authorization decides what that identity may do. Design, implement, and test them separately.
- There is no reason to add authentication to a public portfolio unless a real protected capability requires it. Do not build speculative account systems.
- If protected capabilities are introduced, prefer a proven OIDC/OAuth provider and authorization-code flow with PKCE. Keep authorization decisions on the server and deny by default.
- Use policy/claim/resource-based authorization for business rules. A hidden button or route guard is never sufficient authorization.
- Prefer secure, `HttpOnly`, `SameSite`, HTTPS-only cookies for browser sessions when the architecture permits. If cookies authenticate state-changing requests, address CSRF explicitly. Do not store long-lived bearer tokens in local storage.
- Apply least privilege to identities, credentials, files, networks, and deployment tokens. Rotate or revoke credentials after suspected exposure.
- Never commit secrets. Use local user secrets or ignored environment files for development and the deployment secret store for hosted environments. Commit only safe examples with placeholder values.
- Keep CORS to explicit trusted origins. Do not combine credentialed requests with wildcard origins.
- Trust forwarded headers only from known proxies/networks and keep HTTPS redirect/proxy behavior consistent with deployment.
- Encode output through framework defaults; do not bypass escaping for untrusted HTML. Validate external URLs and redirect targets.
- Avoid logging contact message bodies, tokens, credentials, or other sensitive personal data. Collect and retain only what the feature needs.
- Pin dependencies through existing lock files, review new packages for maintenance and security, and remove unused packages.

## Testing strategy

- A behavior change is incomplete without proportionate verification. Prefer a small test pyramid: many deterministic unit/component tests, focused integration tests at important boundaries, and a few end-to-end tests for critical journeys.
- Backend unit tests should cover domain/application rules without network access. Integration tests should exercise minimal API routes, validation, serialization, middleware, and authorization using an in-memory test host and fakes or controlled test infrastructure.
- Frontend tests should assert behavior through accessible roles and user interaction, not implementation details. Test localized content, form validation, loading/error states, keyboard use, and server/client boundaries where relevant.
- For bug fixes, add a regression test that fails for the original defect when feasible.
- Tests must be isolated, deterministic, and free of production credentials or live SMTP/external API calls.
- Do not weaken assertions, delete tests, add blanket exclusions, or lower quality gates merely to make validation pass.
- The repository currently has limited test infrastructure. Add the smallest conventional setup when a change needs it; do not introduce multiple competing test frameworks.

## Validation commands

Run commands from the relevant directory and use the repository's lock files. Start with checks closest to the change.

### Backend

```text
dotnet restore JoaoLoureiro.Portfolio.slnx --locked-mode
dotnet build JoaoLoureiro.Portfolio.slnx --no-restore
dotnet test JoaoLoureiro.Portfolio.slnx --no-build
```

Run these from `backend/`. If the pinned SDK and target framework disagree, report the exact issue instead of silently changing either one.

### Frontend

```text
npm ci
npm run lint
npx tsc --noEmit
npm run build
```

Run these from `frontend/`. Do not switch package managers or rewrite `package-lock.json` without a deliberate migration.

For documentation-only changes, validate links, paths, and examples; full builds are optional. If an existing command is broken or no tests exist, say so clearly and run the remaining meaningful checks.

## Dependencies, data, and operations

- Prefer existing platform/framework capabilities before adding a package. A new production dependency needs a concrete benefit, compatible license, maintained release history, acceptable bundle/runtime cost, and no simpler existing option.
- Do not edit generated output, lock files by hand, compiled assets, or migration history unless the task specifically requires it.
- Keep environment-specific values out of source. Changes to configuration must include safe defaults or fail-fast validation and update the relevant example/documentation.
- Preserve deployment behavior. When changing ports, proxy headers, health checks, environment variables, process names, or build output, inspect and update the Gitea workflow and runtime configuration together.
- Use observability that helps action: structured logs, correlation where useful, meaningful health signals, and no noisy success logging in hot paths.
- Avoid destructive data or deployment operations unless explicitly requested and their target is verified.

## Definition of done

A change is done when:

- the requested behavior works and acceptance criteria are met;
- architecture and security boundaries remain clear;
- edge cases, failure states, accessibility, localization, and responsive behavior were considered where relevant;
- tests were added or updated in proportion to risk;
- applicable format, lint, type-check, test, and build commands pass, or pre-existing blockers are reported precisely;
- no secrets, debug artifacts, generated junk, or unrelated changes are included;
- user-facing or operational documentation is updated only when behavior, setup, or deployment actually changed;
- the handoff summarizes the outcome, validation performed, and any real residual risk.
