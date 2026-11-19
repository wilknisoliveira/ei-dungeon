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

Deploy a chosen published image through **Actions → Deploy production → Run workflow** on `main`, selecting `frontend` and its `sha-<full-commit-sha>` tag. The shared workflow verifies publication, resolves the digest, connects by SSH and checks the localized HTTPS route after replacement. Use a previous published tag for frontend rollback.

Manual deployment is the default. Set repository variable `DUNGEON_AUTO_DEPLOY=true` to call that workflow after successful web publication. Optional reviewers use environment `production`, subject to GitHub account features. The sibling `../../infra/apps/dungeon/DEPLOYMENT.md` documents production credentials, migration and recovery. Configure branch protection to require the applicable workflow checks before merging, accounting for path filters.

## Google sign-in and simpler registration

Login uses email/password or the official Google button. Signup asks for email, password and confirmation. New eligible Google users go directly to Home. Existing unlinked accounts must log in and explicitly link Google in Settings. Google-only accounts show Create password and require a new confirmation through the linked Google account. Account capabilities are fetched from the authenticated API rather than inferred from old token profile fields.

Login and signup place the official Google button below the local form, separated by a localized "or" label and horizontal rules. This alternative, including its separator, is hidden when Google is not configured.

The shared Google button prepares `outline` and `outline_dark` once per attempt in overlapping grid cells. Theme changes only switch visibility; the hidden version is inert and excluded from accessibility navigation. This preserves the button DOM, challenge, nonce and credential callback without re-rendering the SDK on each toggle. Both versions use the same type, size and text. Only initial rendering, explicit retry and periodic challenge renewal initialize a new authentication attempt. The Google script's `hl` query parameter and both buttons' `locale` use the current page language from LocaleService (`en`, `pt-BR` or `es`), rather than the browser's preferred language. Language selection loads the corresponding localized application.

Anonymous authentication calls use `PUBLIC_AUTH` and omit application Bearer tokens. Authenticated Google link/password calls retain their Bearer token and send browser credentials (`withCredentials: true`). Never automatically refresh and replay a rejected Google proof. Keep Google credentials only in memory for the current operation; persist only the application's token pair under the existing `tokenInfo` key. Show localized application messages instead of raw server/provider errors.

After traditional signup, open email verification; confirmation does not automatically sign in. Password recovery displays the same generic acknowledgement and Google login reminder for every address. Only verified accounts with an existing local password are eligible for recovery. A Google-only account creates its first password in authenticated Settings. Preserve six-digit code validation and the shared resend cooldown; handle HTTP 429 and `Retry-After` when present. Request acknowledgements do not establish account existence or guarantee email delivery.

Set googleClientId in src/environments/environment.ts for npm start. For production/Docker builds, set the public GOOGLE_CLIENT_ID environment/build variable or the Actions repository variable with that name. It is optional and empty disables the Google actions. Changing a production Client ID requires rebuilding the web image. Configure the identical Client ID on the API.

Read the [Google setup tutorial](../api/docs/google-auth-setup.md) for console settings, origins, matching environments and smoke tests. The frontend forwards browser-bound proof cookies with Google operations. Local development should consistently use localhost for both services. The current production frontend and API share the wildforges.com site.
