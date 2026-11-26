# EI Dungeon

> Status: in development

EI Dungeon is a role-playing game simulator with players and game responses controlled by generative AI. The goal is to let RPG players create quick adventures through a browser interface. This monorepo contains the Angular web application and ASP.NET Core API, each with its own dependencies, build, and container image.

## Features

- Email/password and Google registration/sign-in, explicit Google linking, email verification, and password recovery.
- Game creation and management, a game list, and streaming chat plays.
- Guild Mail for private player support conversations, with an administrative queue and email notifications.
- English, Brazilian Portuguese, and Spanish interfaces and localized game responses.
- Public landing pages with static prerendering and SEO output.
- PostgreSQL persistence, migrations, health checks, and Docker support.

Longer-term goals include premium account flows and administration tools for tuning AI responses and managing users.

## Repository structure

| Path | Purpose |
|---|---|
| [api/](api/README.md) | .NET 9 API, PostgreSQL Compose stack, API docs and architecture |
| `api/ei-back/` | Domain, application, infrastructure, HTTP controllers and hubs |
| `api/ei-back.Tests/` | xUnit unit and database tests |
| [web/](web/README.md) | Angular 22 and Angular Material web application, nginx image |
| `web/src/app/`, `web/src/assets/`, `web/src/i18n/` | Application code, static assets, and translations |
| [.github/workflows/](.github/workflows/) | Active API and web validation/image workflows |
| `specs/` | Feature plans and execution tasks |
| [AGENTS.md](AGENTS.md), `api/AGENTS.md`, `web/AGENTS.md` | Shared and module contributor guidance |
| `.agents/skills/` | Repository skills with usage rules in the shared and module guides |

The original repositories remain available for history. Their current working trees were copied here without their `.git` directories. Internal project names are retained. Module-level `.github/workflows/` copies are inactive references; edit the root workflows for CI changes.

## Local development

For container development, install Docker Engine and Docker Compose v2. On a fresh checkout, create `api/.env` and `web/.env` from their adjacent `.env.example` files, then configure them using the module READMEs. Preserve existing local `.env` files.

From the repository root:

```powershell
docker compose --env-file api/.env -f api/compose.yaml up -d --build
docker compose --env-file web/.env -f web/docker-compose.yml up -d --build
```

Defaults are web `http://localhost:8000`, API health `http://localhost:8080/health`, and development Swagger `http://localhost:8080/swagger`. Configure an AI provider key for generated gameplay and email settings for verification/recovery. No working credentials are included in versioned configuration.

The stacks retain project names `ei-dungeon-back` and `ei-dungeon-web`. The API uses named volume `ei-dungeon-back_postgres_data`; moving directories does not reset it. Run service-specific commands from each module, and avoid `docker compose down -v` unless intentionally deleting local data.

For native development, see [API setup](api/README.md#run-without-docker) and [web setup](web/README.md#development). The API's direct development HTTP port is 5231; Angular development uses that URL, while the production web build defaults to API port 8080.

See the [Google setup tutorial](api/docs/google-auth-setup.md) for administrator configuration. Accounts use internal IDs and unique email. Registration collects email and password with client-side confirmation. Google-only users can create their first password in Settings after confirming their linked Google identity.

## Validation

From the root, run `dotnet build api/ei-back.Tests/ei-back.Tests.csproj -c Release` and `dotnet test api/ei-back.Tests/ei-back.Tests.csproj -c Release`. Database tests additionally require an isolated, migrated PostgreSQL database through `VERIFICATION_TEST_CONNECTION`.

From `web/`, run `npm ci`, `npx tsc --project tsconfig.app.json --noEmit`, and `npm test -- --watch=false --browsers=ChromeHeadless`. Set `SITE_URL` and optionally `API_URL`, then run `npm run build`, `node scripts/verify-seo-output.mjs`, and `npm audit --audit-level=high`.

## Container publishing

Both root workflows validate pull requests targeting `main` and pushes to `main`. Only main-branch pushes publish `latest` and `sha-<full-commit-sha>` tags:

Each workflow runs only when its module (`api/**` or `web/**`) or its own root workflow file changes. API-only changes do not rebuild the web image, and web-only changes do not rebuild the API image. Changes limited to root documentation skip both workflows.

- [API workflow](.github/workflows/api-container.yml): `ghcr.io/wilknisoliveira/ei-dungeon/api`.
- [Web workflow](.github/workflows/web-container.yml): `ghcr.io/wilknisoliveira/ei-dungeon/web`.

Set `SITE_URL` and `API_URL` repository variables for the web build. Set optional public `GOOGLE_CLIENT_ID` to enable Google sign-in. Each workflow derives its image path from the repository name and publishes using its `GITHUB_TOKEN`.

## Production deployment

Use **Actions → Deploy production → Run workflow** on branch `main` to select backend, frontend or both and a published `sha-<full-commit-sha>` tag for each selected service. The workflow verifies successful main image publication, resolves digests and calls the restricted SSH deployment script. The VPS installs that content and checks local/public readiness. API updates require a validated database backup first.

Manual dispatch is the default. Set repository variable `DUNGEON_AUTO_DEPLOY=true` to deploy each service after its successful main image publication. The same workflow supports optional required reviewers through environment `production`, subject to GitHub account features. Each service has independent versions. Use manual selection of both images for a coordinated release.

The shared sibling infrastructure repository groups Dungeon configuration under `../infra/apps/dungeon/`. Start with [common VM setup](https://github.com/wilknisoliveira/infra/blob/main/docs/VM-SETUP.md), then [project onboarding for VM and GitHub](https://github.com/wilknisoliveira/infra/blob/main/docs/ADD-PROJECT.md). Use the [Dungeon runbook](https://github.com/wilknisoliveira/infra/blob/main/apps/dungeon/DEPLOYMENT.md) for its actual settings, migration and operation. The shared Linux account is `deploy`, with a separate project key and forced command. Set `DUNGEON_SSH_USER=deploy` explicitly. On the VPS the application path is `/opt/infra/apps/dungeon`. Preserve Compose project `dungeon-production` and its database volume when moving files. Backend image rollback requires schema compatibility because startup applies migrations.

When configuring branch protection, account for these path filters: GitHub can leave required checks pending when their entire workflow is skipped. Avoid requiring both filtered workflows unconditionally; use an always-running required gate if every PR must have a check. See [GitHub's path-filter documentation](https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-syntax#onpushpull_requestpull_request_targetpathspaths-ignore).

## Contributing and license

Read [Repository Guidelines](AGENTS.md) and the module guide before making changes. Keep secrets in ignored environment files or .NET User Secrets; frontend build variables are public. Include validation results and screenshots for UI changes in pull requests.

Created by [Wilknis Deyvis](https://www.linkedin.com/in/wilknis/). Distributed under the [MIT License](LICENSE); original module license notices are preserved in `api/LICENSE` and `web/LICENSE`.
