# JoaoLoureiro.Portfolio Backend

This is the backend for my portfolio application. It is a simple .NET 8 API that handles sending contact messages from the frontend.

## Technologies

*   .NET 8
*   ASP.NET Core
*   Docker
*   Serilog for logging
*   FluentValidation for request validation

## Project Structure

The solution follows a clean architecture pattern:

*   `JoaoLoureiro.Portfolio.Api`: The main API project, containing the endpoint for sending emails.
*   `JoaoLoureiro.Portfolio.Application`: Contains the core business logic and interfaces.
*   `JoaoLoureiro.Portfolio.Domain`: Contains the domain entities.
*   `JoaoLoureiro.Portfolio.Infrastructure`: Contains the implementation of services, such as the email sender.

## Content persistence

The content store uses SQLite through Entity Framework Core. The repository contains
the approved post and localized-post tables, project tables, and the synchronized
bilingual locale catalog. The provider, DI registration, and migration tooling are
shared by the content use cases.

SQLite is appropriate for the current single-backend-instance runtime (`instances: 1`
in the PM2 configuration and one backend service in Compose). It is durable only when
its database file is placed on storage outside the container filesystem. Compose mounts
the named Docker volume `portfolio-content-data` at `/var/lib/portfolio` and configures
`ContentStore__Path=/var/lib/portfolio/portfolio.db`. Do not run production with the
development-relative `data/portfolio.db` path.

The image-publishing workflow only builds and publishes images; deployment remains
responsible for preserving the named volume, running migrations before switching the
image, and maintaining backups. For a direct/PM2 deployment, set
`ContentStore__Path` to an absolute host-backed path included in the backup plan.

### Migrations

Install the matching EF Core CLI once on the deployment or migration host:

```bash
dotnet tool install --global dotnet-ef --version 8.0.30
```

Run migration commands from `backend/`, with `ContentStore__Path` pointing to the
database being migrated:

```bash
dotnet ef migrations add <MigrationName> \
  --project JoaoLoureiro.Portfolio.Infrastructure \
  --startup-project JoaoLoureiro.Portfolio.Api \
  --context PortfolioDbContext

dotnet ef database update \
  --project JoaoLoureiro.Portfolio.Infrastructure \
  --startup-project JoaoLoureiro.Portfolio.Api \
  --context PortfolioDbContext
```

Create and review the migration in a controlled environment before deployment. The
`SeedProjects` migration inserts the initial three project records and their ordered
localized children once; later Admin edits are not overwritten by application startup.
The `AddLocaleCatalog` migration creates only the catalog tables. The first catalog
read or update inserts the reviewed English and Portuguese built-in catalog at revision
1 when no catalog row exists; this bootstrap is idempotent and never overwrites an
existing revision.
Stop the backend or take the SQLite backup before applying a migration. Roll back to a
known migration with `dotnet ef database update <PreviousMigration>` only after
reviewing the generated SQLite operations; if a migration fails or data safety is in
doubt, stop the backend and restore the last verified database backup instead.

For the Compose runtime, stop `portfolio-backend`, back up the
`portfolio-content-data` volume, run the migration against that volume, then start the
service and verify `/health`. Backups must include the SQLite database file and be
stored outside the Docker volume; test restoration periodically in an isolated volume.

Locale content is updated through the protected `PUT /admin/api/locales` endpoint and
uses the expected revision for optimistic concurrency. Before deploying the code that
requires this schema, stop the backend, back up the database, and apply the migration:

```bash
dotnet ef database update AddLocaleCatalog \
  --project JoaoLoureiro.Portfolio.Infrastructure \
  --startup-project JoaoLoureiro.Portfolio.Api \
  --context PortfolioDbContext
```

To roll back locale storage, restore the last verified SQLite backup rather than
manually deleting catalog rows. A reviewed EF rollback to the previous migration is
also possible while the backend is stopped, but it removes all managed locale edits.
The anonymous `GET /api/locales/{locale}` endpoint serves only the active catalog and
the frontend retains its checked-in catalog as a safe fallback if the store is
unavailable.

The `AddEmailDeliveryPolicy` migration adds one durable policy row. It stores only the
enabled flag, recipient address, sender display name, subject prefix, and revision; it
does not store SMTP credentials or connection details. Apply it with the same migration
command above before enabling the Admin Email delivery surface. The first read seeds
the row from `EmailDelivery:Initial*` values only when it does not exist, so later Admin
edits survive restarts.

### Project image policy

The initial project capability has no upload endpoint or media library. Add an image by
deploying the file with the frontend under `frontend/public/` and saving its root-relative
path (for example, `/Portfolio.png`), or by saving an HTTPS image URL whose host is in
the shared allowlist: `github.com`, `joaoloureiro.dev.br`, or
`happy.joaoloureiro.dev.br`. The API rejects other schemes, protocol-relative URLs,
traversal paths, and unapproved hosts; the Next.js image configuration must be updated
and reviewed if this allowlist changes. Keep deployed assets in the frontend release
and include them in the normal release/rollback procedure.

The test web host replaces the configured file with an open per-fixture SQLite
`:memory:` connection. Tests therefore need no production credentials, external
service, or writable repository/container state.

## Getting Started

### Prerequisites

*   .NET 8 SDK
*   An SMTP server for sending emails.

### Running the application

1.  Clone the repository.
2.  Navigate to the `backend` directory.
3.  Configure the `SmtpTransport` values through user secrets or deployment
    environment variables. The committed appsettings values are placeholders.
4.  Run the application using the following command:

    ```bash
    dotnet run --project JoaoLoureiro.Portfolio.Api
    ```

## API Endpoints

### POST /api/email/send

This endpoint accepts a contact form submission and sends it as an email.

**Request Body:**

```json
{
  "name": "John Doe",
  "email": "john.doe@example.com",
  "message": "Hello, I would like to get in touch with you."
}
```

**Responses:**

*   `200 OK`: If the message was sent successfully.
*   `400 Bad Request`: If the request is invalid.
*   `413 Payload Too Large`: If the request exceeds the server-side body limit.
*   `429 Too Many Requests`: If the client has exceeded the bounded contact rate.
*   `503 Service Unavailable`: If delivery is disabled or unavailable.
*   `504 Gateway Timeout`: If the bounded SMTP operation times out.

### GET /health

`/health/live` is a cheap liveness probe. `/health/ready` and `/health` perform a
cached, bounded SMTP readiness check and return only `{ "status": "healthy" }` or
`{ "status": "unhealthy" }`; they never return host, account, recipient, credentials,
or provider exception details.

### Admin email delivery

`GET /admin/api/email-delivery`, `PUT /admin/api/email-delivery`, and
`POST /admin/api/email-delivery/test` require the `PortfolioAdmin` policy. The update
request carries `expectedRevision` for optimistic concurrency. The test action always
uses the saved recipient and a fixed subject/body; it cannot be used to probe an
arbitrary address.

### Locale catalog

`GET /api/locales/{locale}` is an anonymous read of the active `en` or `pt` public
messages. `GET /admin/api/locales` and `PUT /admin/api/locales` require the
`PortfolioAdmin` policy. An update sends the expected revision plus a `changes` object;
each changed key must contain both `en` and `pt` values:

```json
{
  "expectedRevision": 1,
  "changes": {
    "hero.title": {"en": "New title", "pt": "Novo título"},
    "atlas.interfaces.points": {"en": ["Routes"], "pt": ["Rotas"]}
  }
}
```

The update is rejected with `409` when the revision is stale and is committed as one
bilingual revision. Only the fixed editable catalog is accepted; projects, posts,
structured principles, locale routing, and Admin copy are outside this endpoint.

## Configuration

The application is configured using the `appsettings.json` file in the `JoaoLoureiro.Portfolio.Api` project.

*   `SmtpTransport`: deployment-managed SMTP host, port, explicit `None`, `StartTls`,
    or `SslOnConnect` security mode, authentication credentials, envelope/from address,
    and bounded timeouts. Do not put real values in committed files.
*   `EmailDelivery`: first-run seed values for the Admin-managed policy. These values
    are not reapplied after the durable policy row exists.
*   `CorsOrigins`: A comma-separated list of allowed origins for CORS.
*   `ProxyIP`: The IP address of a trusted proxy.
*   `Serilog`: Configuration for logging.

### Admin OIDC resource-server configuration

The API expects the following runtime configuration values before it starts:

* `Authentication__Oidc__Issuer`: The expected Authentik issuer URL.
* `Authentication__Oidc__Audience`: The expected API audience.
* `Authentication__Oidc__DiscoveryAddress`: The HTTPS OpenID Connect discovery address used to load public signing keys.
* `Authentication__Oidc__AdminClaimType`: The validated claim containing group membership, normally `groups`.
* `Authentication__Oidc__AdminGroup`: The exact Authentik group value accepted by the `PortfolioAdmin` policy.

Provide these values through the deployment secret store, environment variables, or local user secrets. Do not commit provider credentials or production values to the repository. The API accepts only `Authorization: Bearer <token>`; it does not trust `X-authentik-*` headers. In the controlled `Testing` environment, an HTTP discovery address may be used by isolated tests; other environments require HTTPS.

In production, HTTPS terminates at the trusted edge proxy. The backend has no host port and is reached by the frontend BFF over the private Compose network.
