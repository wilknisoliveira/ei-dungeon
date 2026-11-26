# Repository Guidelines

This `AGENTS.md` is the source of repository guidance. If the user requests a change to `CLAUDE.md`, apply it here.

## Project Structure & Module Organization

EI Dungeon is an RPG application with AI-generated game responses. This monorepo contains two independently built applications:

- `api/ei-back/`: ASP.NET Core (.NET 9) API. `Core/Domain/` contains entities, `Core/Application/` contains use cases and services, `Infrastructure/` contains persistence and integrations, and `UserInterface/` contains controllers and hubs.
- `api/ei-back.Tests/`: backend tests; `api/docs/`: API integration documentation.
- `web/src/app/`: Angular application; `web/src/assets/`: static assets; `web/src/i18n/`: English, Brazilian Portuguese, and Spanish translations.
- `.github/workflows/`: active API and web container workflows. Module-level workflow copies are inactive references.
- `.github/workflows/deploy-production.yml`: manual/reusable SSH production deployment. Application-owned VPS configuration lives in sibling `../infra/apps/dungeon/` within the shared VM repository.
- `specs/`: feature specifications and tasks; `.agents/skills/`: repository skills.

Read [api/AGENTS.md](api/AGENTS.md) or [web/AGENTS.md](web/AGENTS.md) before designing, reviewing, or changing the corresponding module, including when starting from the repository root. Read both for work that spans both modules. For API work, also read `api/ARCHITECTURE.md` and the local, ignored `api/LOCAL_GUIDES.md` when present.

## Repository Skills

When the task matches a skill listed here or in an affected module's guide, read and apply the linked `SKILL.md` even without an explicit skill mention. Use the linked repository copy when a personal skill has the same name. Apply skill guidance within the repository's conventions and the task's scope.

- [ci-cd-and-automation](.agents/skills/ci-cd-and-automation/SKILL.md) for changes to GitHub Actions, build and publish pipelines, automated quality gates, or deployment workflows.

## Codex Subagents

Use the repository's custom subagents when their expertise helps with investigation, implementation, or review:

- [backend](.codex/agents/backend.toml) for API, game rules, AI game master behavior, persistence, and streaming.
- [frontend](.codex/agents/frontend.toml) for the Angular application, API integration, landing pages, and SEO.
- [deploy](.codex/agents/deploy.toml) for CI/CD, image publishing, and deployment configuration.

Give each subagent a clear goal, the relevant repository guidance, and the paths it may edit. Run independent tasks in parallel when useful. For changes that span modules, agree on shared contracts before delegating edits, assign non-overlapping files, and verify the integrated result. Handle small, self-contained tasks directly when delegation adds little value.

Select the matching custom agent type when delegating; a task label alone does not select its profile. Each profile maps its responsibilities to the repository skills and requires loading only the skills applicable to the assignment. Module guides remain the source of project conventions. Investigation and review assignments are read-only unless implementation is authorized. Subagents share the workspace and must preserve other agents' and users' edits.

Require each handoff to summarize findings or changes, files changed, validation outcomes (including skipped checks and reasons), blockers, and cross-module follow-up. The parent agent owns integration and final verification. After changing a profile, validate its TOML and linked guidance, then exercise the custom agent with a small read-only task and inspect its evidence and tool activity; availability or a self-reported role alone does not validate behavior.

## Build, Test, and Development Commands

From the repository root:

```powershell
dotnet build api/ei-back.Tests/ei-back.Tests.csproj -c Release
dotnet test api/ei-back.Tests/ei-back.Tests.csproj -c Release
dotnet run --project api/ei-back/ei-back.csproj
```

From `web/`, run `npm ci` to install locked dependencies, `npm start` for development, and `npm test -- --watch=false --browsers=ChromeHeadless` for tests. Set `SITE_URL` before `npm run build`; set `API_URL` to select the backend. Run `node scripts/verify-seo-output.mjs` afterward using the same `SITE_URL`.

Run `docker compose up -d --build` separately from `api/` and `web/`; each has its own `.env` and stack.

## Coding Style & Naming Conventions

Match nearby code. Use four-space indentation; `web/.editorconfig` specifies single-quoted TypeScript strings. Angular uses kebab-case filenames; C# uses PascalCase types and camelCase parameters. Preserve API layer boundaries and existing dependency-injection patterns. VS Code selects Prettier for web formatting; no npm lint script exists.

Backend use cases follow one operation per class/interface, with a single public `Handler` entry method. Group them by feature and extract shared logic into Application services. See `api/AGENTS.md` for the complete backend conventions and transaction exceptions.

## Testing Guidelines

Backend tests use xUnit, FakeItEasy, and FluentAssertions in `*Tests.cs`. Web tests use Jasmine/Karma in colocated `*.spec.ts`. Set `VERIFICATION_TEST_CONNECTION` only to a migrated, isolated PostgreSQL database to enable database tests. No coverage threshold is configured. Update all three translations when user-visible strings change, except on administration pages. Administration pages are English-only: do not add language selectors, Angular i18n markers, `$localize` messages or translation catalog entries for their content, dialogs, feedback or pagination.

## Commit & Pull Request Guidelines

Require Conventional Commits with short, imperative descriptions, such as `feat(api): add game export` or `fix(web): handle expired sessions`. Keep each commit scoped to one logical change. Describe affected modules, link relevant issues, report validation results and skipped checks, and include screenshots for UI changes.

## Security & Configuration

Keep credentials in ignored `.env` files or .NET User Secrets. Commit environment examples only. Preserve Compose project names and existing database volumes; avoid destructive resets.

Production deploys published commit tags resolved to digests. Manual dispatch is the default, with automatic deployment enabled only by `DUNGEON_AUTO_DEPLOY=true`. Follow `../infra/docs/VM-SETUP.md` for the shared `deploy` account, `../infra/docs/ADD-PROJECT.md` for per-project VM and GitHub onboarding, and `../infra/apps/dungeon/DEPLOYMENT.md` for Dungeon settings, operation and recovery. Maintain deployment documentation in English. Configure `DUNGEON_SSH_USER=deploy` explicitly and keep each repository's SSH key separate. Keep Dungeon-specific infrastructure scripts under its application directory. Preserve production project `dungeon-production` and volume `dungeon-production_postgres_data`.

Whenever application Compose configuration or environment variables change, update the corresponding production configuration in `../infra/apps/dungeon/` as part of the same work. Keep the production `compose.yaml`, `.env.example`, and `DEPLOYMENT.md` aligned with the application settings, and validate the updated production configuration with `docker compose config --quiet`.

## Emails to users

Always render user-facing emails with the shared EI Dungeon HTML email template. Provide a plain-text alternative with equivalent information and localize both versions in the recipient language supported by the flow. Keep delivery provider code separate from email wording and layout.

## Account identity

Local authentication uses unique normalized email. Identify authorized users and game ownership by the existing internal GUID in application JWT sub. Google identities use a separate provider subject linked to that GUID. FullName/UserName are removed from active contracts and the users table. Google setup and environment configuration are documented in api/docs/google-auth-setup.md. The public GOOGLE_CLIENT_ID is compiled into web builds and mapped to GoogleAuth__ClientId for the API.
