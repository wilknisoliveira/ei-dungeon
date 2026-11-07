# Repository Guidelines

## Project Structure & Module Organization

EI Dungeon is an RPG application with AI-generated game responses. This monorepo contains two independently built applications:

- `api/ei-back/`: ASP.NET Core (.NET 9) API. `Core/Domain/` contains entities, `Core/Application/` contains use cases and services, `Infrastructure/` contains persistence and integrations, and `UserInterface/` contains controllers and hubs.
- `api/ei-back.Tests/`: backend tests; `api/docs/`: API integration documentation.
- `web/src/app/`: Angular application; `web/src/assets/`: static assets; `web/src/i18n/`: English, Brazilian Portuguese, and Spanish translations.
- `.github/workflows/`: active API and web container workflows. Module-level workflow copies are inactive references.
- `specs/`: feature specifications and tasks; `.agents/skills/`: repository skills.

Read the relevant module's `AGENTS.md` before changing it. For API work, also read `api/ARCHITECTURE.md` and the local, ignored `api/LOCAL_GUIDES.md` when present.

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

## Testing Guidelines

Backend tests use xUnit, FakeItEasy, and FluentAssertions in `*Tests.cs`. Web tests use Jasmine/Karma in colocated `*.spec.ts`. Set `VERIFICATION_TEST_CONNECTION` only to a migrated, isolated PostgreSQL database to enable database tests. No coverage threshold is configured. Update all three translations when user-visible strings change.

## Commit & Pull Request Guidelines

Require Conventional Commits with short, imperative descriptions, such as `feat(api): add game export` or `fix(web): handle expired sessions`. Keep each commit scoped to one logical change. Describe affected modules, link relevant issues, report validation results and skipped checks, and include screenshots for UI changes.

## Security & Configuration

Keep credentials in ignored `.env` files or .NET User Secrets. Commit environment examples only. Preserve Compose project names and existing database volumes; avoid destructive resets.
