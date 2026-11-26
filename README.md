# João Loureiro Portfolio

Full-stack personal portfolio and blog, with a public bilingual site and a protected English Admin workspace.

[![Portfolio](https://img.shields.io/badge/Live_Demo-joaoloureiro.dev.br-blue?style=for-the-badge&logo=icloud)](https://joaoloureiro.dev.br)

## Architecture

- `frontend/`: Next.js 16, React 19, TypeScript, and `next-intl`.
- `backend/`: ASP.NET Core `net8.0`, organized into API, Application, Domain, and Infrastructure projects.
- `docker-compose.yml`: production runtime with Traefik routing and a private backend service.

## Admin OIDC contract

The Admin panel uses Authentik OIDC Authorization Code + PKCE. Register these exact URLs in the confidential OIDC application:

- Callback: `https://joaoloureiro.dev.br/api/auth/callback/authentik`
- Post-logout redirect: `https://joaoloureiro.dev.br/`

Better Auth stores encrypted provider tokens in a server-side SQLite database and gives the browser only an HttpOnly session cookie. Browser Admin calls use the same-origin `/admin/api/**` BFF, which forwards allowlisted requests to the internal backend with `Authorization: Bearer`. The backend accepts only standard Bearer tokens and validates issuer, audience, lifetime, signature, and signing algorithm before applying the `PortfolioAdmin` group policy.

Configure these values through Komodo or local user secrets. Never commit the client secret or Better Auth secret:

- Frontend: `BACKEND_INTERNAL_URL`, `PUBLIC_BASE_URL`, `BETTER_AUTH_URL`, `BETTER_AUTH_SECRET`, `AUTH_DATABASE_PATH`, `OIDC_ISSUER`, `OIDC_DISCOVERY_ADDRESS`, `OIDC_CLIENT_ID`, `OIDC_CLIENT_SECRET`, `OIDC_POST_LOGOUT_REDIRECT_URI`, and `OIDC_SCOPE`. Register `/api/auth/callback/authentik` as the OIDC callback path.
- Backend: `Authentication__Oidc__Issuer`, `Authentication__Oidc__Audience`, `Authentication__Oidc__DiscoveryAddress`, `Authentication__Oidc__AdminClaimType`, and `Authentication__Oidc__AdminGroup`.

The Authentik application must publish the configured admin claim (normally `groups`), use an asymmetric signing key, enable `openid profile email` and refresh-token support, and bind only the intended administrator group.

## Local development

### Backend

```bash
cd backend
dotnet restore JoaoLoureiro.Portfolio.slnx --locked-mode
dotnet build JoaoLoureiro.Portfolio.slnx --no-restore
dotnet run --project JoaoLoureiro.Portfolio.Api
```

Configure SMTP and the `Authentication:Oidc` values through user secrets or environment variables before starting the API. The API fails fast when required OIDC settings are missing outside the controlled test environment.

### Frontend

```bash
cd frontend
npm ci
npm run dev -- --port 3001
```

Copy `frontend/.env.example` to `.env.local` and replace the placeholders. The local configuration expects the backend at `http://localhost:3000` and the frontend at `http://localhost:3001`. Do not use `NEXT_PUBLIC_` for server-only values.

## Deployment

Production uses the root `docker-compose.yml`. The backend has no host port; Traefik exposes public backend routes while the public frontend host serves `/admin` and `/admin/api/**`. Komodo supplies the OIDC issuer, audience, discovery address, administrator group, client secret, Better Auth secret, and persistent auth database volume.

Project content is stored in the backend SQLite database and managed through the protected Admin Projects module. Public pages read only published records from the localized `/api/projects` endpoints. The initial media policy accepts deployed files under `frontend/public/` by root-relative path or approved HTTPS image hosts; uploads and a media library are intentionally deferred. See [`backend/README.md`](backend/README.md) for migration, backup/restore, rollback, and asset-release procedures.

## License

This project is licensed under the MIT License. See the `LICENSE` file for details.
