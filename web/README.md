# EI Dungeon Web

Angular and Angular Material frontend for the [EI Dungeon monorepo](../README.md). API setup is documented in [api/README.md](../api/README.md); contributor rules are in [AGENTS.md](AGENTS.md).

Run all commands below from `web/`.

## Development

Use Node.js 22.22.3 (the CI version) and npm. Install locked dependencies and start Angular's development server:

```powershell
npm ci
npm start
```

The development API origin is `http://localhost:5231`, configured in `src/environments/environment.ts`. Production builds use the `API_URL` build-time override or default to `http://localhost:8080`. Setting `API_URL` does not change `ng serve` configuration.

## Validation

```powershell
npx tsc --project tsconfig.app.json --noEmit
npm test -- --watch=false --browsers=ChromeHeadless
npm audit --audit-level=high
```

Tests use Jasmine/Karma and require Chrome. There is no npm lint command or enforced coverage threshold. After a production build, run `node scripts/verify-seo-output.mjs` with the same `SITE_URL` used for that build.

## Internationalization

The application supports three languages: English, Portuguese (Brazil), and Spanish.

### Running a specific language

```
# English (default)
npx ng serve

# Portuguese (Brazil)
npx ng serve --configuration=pt-BR

# Spanish
npx ng serve --configuration=es
```

### Building for production (all languages)

Set `SITE_URL` to the final public HTTPS origin (without a path), then run:

```powershell
$env:SITE_URL = "https://example.com"
npm run build
```

This statically prerenders the public landing pages and generates crawler files in `dist/ei-dungeon-web/browser/`:

| Directory | Language |
|-----------|----------|
| `en/` | English |
| `pt-BR/` | Portuguese |
| `es/` | Spanish |

Deploy the contents of `dist/ei-dungeon-web/browser/` to a static HTTPS host. Preserve the locale directories and serve `/robots.txt` and `/sitemap.xml` from the site root. Only the localized landing pages are intended for indexing; authenticated application routes remain client-rendered and default to `noindex`.

## Docker

The production image builds all three localized Angular applications and serves the static output with nginx. Docker and the Docker Compose plugin are the only host requirements.

For local use, build and start the application with:

```sh
docker compose up --build -d
```

The local defaults are `SITE_URL=http://localhost:8000` and host port `8000`. Open:

- `http://localhost:8000/` — redirects using the browser's preferred language
- `http://localhost:8000/en/` — English
- `http://localhost:8000/pt-BR/` — Portuguese (Brazil)
- `http://localhost:8000/es/` — Spanish
- `http://localhost:8000/robots.txt` and `http://localhost:8000/sitemap.xml` — crawler files served from the site root

Docker Compose reads `web/.env` when run from this module. The explicit project name is `ei-dungeon-web`. Copy the versioned example when you want persistent local or deployment-specific values:

```powershell
Copy-Item .env.example .env
```

Keep `SITE_URL` aligned with the published port for local builds. For example, to use port `8001`, update `.env` or set both variables before rebuilding:

```powershell
$env:APP_PORT = "8001"
$env:SITE_URL = "http://localhost:8001"
docker compose up --build -d
```

For production, set `SITE_URL` to the final public HTTPS origin before building. The value is embedded in canonical links, `hreflang` links, social metadata, `robots.txt`, and `sitemap.xml`:

```powershell
$env:SITE_URL = "https://example.com"
$env:APP_PORT = "8000"
docker compose up --build -d
```

The container listens for HTTP on port 80. Terminate public HTTPS in the hosting platform, load balancer, or reverse proxy in front of the container.

Check container health with `docker compose ps`. Stop and remove the service with:

```sh
docker compose down
```

## GitHub Actions container publishing

The [root container workflow](../.github/workflows/web-container.yml) checks pull requests to `main` and pushes to `main`. It runs
TypeScript checks, headless unit tests, the production build for all three locales,
SEO output verification, and an npm dependency audit. The Docker image is built only
after those checks pass. Shell commands run from `web/`; the npm cache uses
`web/package-lock.json`, and Docker uses `web/` as its build context. The copied workflow
under `web/.github/` is an inactive reference; the root workflow is authoritative.
There is currently no lint command configured in this project.

Before publishing, add these **repository variables in the new monorepo** under GitHub Settings → Secrets
and variables → Actions → Variables:

| Variable | Value |
|---|---|
| `SITE_URL` | Final public HTTPS site origin, such as `https://dungeon.example.com` |
| `API_URL` | Public HTTPS backend URL, such as `https://api.example.com` |

Both values are public and embedded at build time; changing them requires a new build.
Pull requests use placeholder HTTPS URLs so checks also work for forks. Main builds
fail if either variable is missing. Local builds may use HTTP for localhost; when
`API_URL` is omitted from `npm run build`, the production environment file provides
the local API fallback.

Matching pushes to `main` publish `ghcr.io/<owner>/ei-dungeon/web:latest` and
`ghcr.io/<owner>/ei-dungeon/web:sha-<full-commit-sha>` using the built-in `GITHUB_TOKEN`.
Pull requests build without publishing. The image targets `linux/amd64` and uses
GitHub Actions Docker layer caching. Allow GitHub Actions to publish packages in
the repository or organization settings. No registry password secret is needed.
The workflow derives the image path from the repository name. Configure pull access
in GitHub package settings, then point `FRONTEND_IMAGE` at a published tag or digest
when deploying.

The `Web container` workflow uses separate concurrency and Docker cache scopes from
the API. Required checks for this app are `Web build and test` and
`Web container (publish on main)`. Configure those names in the monorepo's `main`
branch rules after they are available. The local migration does not modify remote
rules or publish images.

Deploy a chosen image tag through your hosting platform. Use a previous `sha-...`
tag for rollback. This workflow publishes images; deployment remains a separate step.
Configure branch protection to require the workflow checks before merging.
