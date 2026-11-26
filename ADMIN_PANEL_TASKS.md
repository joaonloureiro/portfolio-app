# Admin panel OIDC tasks

> Status (2026-09-10): implementation-backed checks are complete. The remaining unchecked items require the external Authentik/Komodo configuration or controlled production verification and are intentionally not marked until performed.

This document is the application contract for the protected Admin panel. Authentik application creation, Komodo secret injection, and final Traefik deployment must be completed in the target homelab; source code does not contain provider credentials.

## Application contract

| Request | Destination | Authentication boundary |
| --- | --- | --- |
| `/`, localized public routes, and Next.js assets | Frontend | Public |
| `/admin` and `/admin/**` | Frontend | Frontend OIDC session |
| `/auth/login`, `/auth/logout` | Frontend | OIDC session entry/exit routes |
| `/api/auth/**` | Frontend | Better Auth OIDC protocol handlers |
| `/admin/api/**` | Frontend BFF | Server-side session, then backend Bearer token |
| Backend `/admin/api/**` | Internal backend only | ASP.NET Core JWT Bearer + `PortfolioAdmin` policy |
| Other `/api/**` routes | Backend | Explicitly public where mapped |

The browser never receives an access token, refresh token, ID token, client secret, or backend credential. Better Auth stores the session in the server-side SQLite database and sends only an HttpOnly session cookie; OAuth tokens are encrypted at rest. Browser Admin mutations use same-origin BFF requests with a CSRF token. The BFF forwards only an allowlisted path and method to `BACKEND_INTERNAL_URL`, with `Authorization: Bearer <access_token>` generated server-side.

The Admin interface is English-only. Public English and Portuguese routes remain unchanged.

### Task 1 status clarification

The repository implementation for Tasks 2–6 is complete and tested. Task 1 remains open because its checkboxes are actions in the external Authentik and Komodo environments: no provider, blueprint, discovery endpoint, or secret store is present in this repository. They can be checked only after the operator creates the application, verifies discovery/JWKS, binds the administrator group, injects production values, and completes a controlled sign-in test.

## Task 1: Create the Authentik OIDC application

This task is an external deployment prerequisite and must be completed in Authentik before public deployment.

- [ ] Create a new OAuth2/OIDC provider and application; do not reuse the old Proxy Provider.
- [ ] Use a confidential client with Authorization Code and PKCE.
- [ ] Register the exact callback URI: `https://joaoloureiro.dev.br/api/auth/callback/authentik`.
- [ ] Register the post-logout redirect URI: `https://joaoloureiro.dev.br/`.
- [ ] Select an asymmetric signing key and expose a stable `jwks_uri` through discovery.
- [ ] Enable `openid`, `profile`, `email`, and refresh-token support (`offline_access` where enabled by policy).
- [ ] Add the `groups` claim, or change `OIDC_ADMIN_CLAIM_TYPE` and the backend claim setting together.
- [ ] Bind only the intended administrator group to the application; the expected value is `OIDC_ADMIN_GROUP`.
- [ ] Verify the discovery and JWKS URLs work publicly over HTTPS.

## Task 2: Implement frontend authentication

- [x] Use Better Auth's generic OAuth integration with the existing Node 22 runtime.
- [x] Protect `/admin` and all Admin pages; an anonymous request starts `/auth/login`.
- [x] Generate and validate state, nonce, and S256 PKCE values server-side through the OAuth integration.
- [x] Exchange the authorization code server-side at `/api/auth/callback/authentik`.
- [x] Store the session in the server-side SQLite database and keep the browser cookie Secure/HttpOnly/SameSite.
- [x] Never store tokens in browser storage, props, rendered HTML, or client bundles.
- [x] Refresh expired access tokens server-side and retain the refreshed session.
- [x] Clear the local session and redirect through the provider end-session endpoint on logout.
- [x] Fail closed with a non-sensitive state when OIDC configuration or discovery is unavailable.

## Task 3: Implement the frontend BFF

- [x] Add server-side `/admin/api/[...path]` handlers for the explicit Admin API allowlist.
- [x] Require a valid encrypted frontend session and return `401` when it is absent or invalid.
- [x] Forward requests only to the fixed internal `BACKEND_INTERNAL_URL`.
- [x] Send only `Authorization: Bearer <access_token>` and required JSON headers upstream.
- [x] Do not forward browser cookies, incoming Authorization headers, or arbitrary headers.
- [x] Restrict paths and methods to `/me` and the explicit posts and projects operations.
- [x] Require a same-origin, double-submit CSRF token for state-changing requests.
- [x] Preserve backend `403` responses and translate upstream failures to bounded safe responses.
- [x] Retry one expired-token response through the server-side refresh-token flow.

## Task 4: Implement backend authorization

- [x] Configure ASP.NET Core JWT Bearer authentication for the `Authentication:Oidc` options section.
- [x] Validate issuer, audience, RSA signature, allowed algorithm, expiry, and `nbf`.
- [x] Load signing keys from the configured Authentik discovery document/JWKS metadata.
- [x] Accept only the standard `Authorization: Bearer` scheme; do not use provider proxy headers.
- [x] Require the `PortfolioAdmin` policy for the `/admin/api/**` route group.
- [x] Authorize the configured `AdminClaimType`/`AdminGroup` claim pair.
- [x] Keep `/health` and the explicitly public `/api/email/send` endpoint anonymous.
- [x] Return `401` for missing/invalid tokens and `403` for valid non-admin identities.

## Task 5: Update deployment configuration

- [x] Add frontend OIDC issuer, client ID, derived callback, logout, scope, backend URL, and Better Auth settings to Compose.
- [x] Require the client secret and Better Auth secret from Komodo variable injection.
- [x] Pass backend issuer, audience, discovery, claim type, and admin group through Compose.
- [x] Keep the backend without direct host ports and do not apply the old external auth middleware.
- [x] Route `/admin/api/**` on the public site to the frontend; exclude that path from the direct backend router.
- [x] Document the final frontend and backend OIDC environment contract in the READMEs.
- [ ] Add the final Authentik application/provider to the Authentik blueprint after the callback and claim names are confirmed.
- [ ] Verify Komodo contains production values and no secrets are committed to the repository.

## Task 6: Test before public deployment

- [x] Anonymous `/` remains accessible through the existing public route tests/build path.
- [x] `/admin` starts the OIDC login flow when no local session exists.
- [x] Anonymous `/admin/api/me` returns `401` through the BFF and backend.
- [x] Invalid, expired, wrong-issuer, wrong-audience, invalid-signature, and disallowed-algorithm tokens return `401`.
- [x] Authenticated non-admin users return `403`.
- [x] Admin API requests are forwarded only through the authenticated BFF in focused tests.
- [x] Backend `403` responses and BFF CSRF failures are preserved.
- [ ] Run a controlled Authentik integration test for callback, refresh, logout, and post-logout redirect.
- [x] Keep tokens and secrets out of browser storage, client code, and application error messages.
- [ ] Confirm production access logs and provider logs do not record credentials or token values.

## Runtime configuration

Frontend server variables:

- `BACKEND_INTERNAL_URL`
- `PUBLIC_BASE_URL`
- `BETTER_AUTH_URL`
- `BETTER_AUTH_SECRET` (Komodo only)
- `AUTH_DATABASE_PATH`
- `OIDC_ISSUER`
- `OIDC_CLIENT_ID`
- `OIDC_CLIENT_SECRET` (Komodo only)
- `OIDC_POST_LOGOUT_REDIRECT_URI` (the public `/` route)
- `OIDC_SCOPE`

Backend variables:

- `Authentication__Oidc__Issuer`
- `Authentication__Oidc__Audience`
- `Authentication__Oidc__DiscoveryAddress`
- `Authentication__Oidc__AdminClaimType`
- `Authentication__Oidc__AdminGroup`

The callback is derived as `/api/auth/callback/authentik` from `PUBLIC_BASE_URL`; logout is configured through `OIDC_POST_LOGOUT_REDIRECT_URI`. If Authentik uses a different group claim or audience, update the provider, Komodo variables, backend options, and this document together.
