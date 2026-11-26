# Web Guidelines

## Repository Skills

- [angular-developer](../.agents/skills/angular-developer/SKILL.md) for changes to Angular components, services, routing, forms, signals, HTTP integration, or Angular tests.
- [angular-material-theming](../.agents/skills/angular-material-theming/SKILL.md) for styling custom components to match the Material theme or changing design tokens, theme palettes, or light and dark behavior.
- [bencium-innovative-ux-designer](../.agents/skills/bencium-innovative-ux-designer/SKILL.md) for designing new pages or components or substantially reworking layouts or interactions within EI Dungeon's visual identity.
- [scss-best-practices](../.agents/skills/scss-best-practices/SKILL.md) for writing or refactoring SCSS, including selectors, mixins, and shared styles.
- [landing-page-copywriter](../.agents/skills/landing-page-copywriter/SKILL.md) for writing or revising public landing-page headlines, value propositions, or calls to action.
- [seo](../.agents/skills/seo/SKILL.md) for changes to public-page indexing, metadata, canonical or `hreflang` links, structured data, sitemaps, or `robots.txt`.

## Layout and Style

Read [README.md](README.md) and the current feature plan under the monorepo's `specs/` before feature work. Older module plans remain under `web/specs/` as local context. Application code lives in `src/app/`; translations are in `src/i18n/`, static assets in `src/assets/`, and build/SEO helpers in `scripts/`.

This guide consolidates `.aipartner/memory/project-standards.md`. Use the current routing, theme service, module Compose file, and root workflows when older snapshot descriptions differ.

Follow existing Angular 22 and Angular Material patterns. Use four-space indentation and single-quoted TypeScript strings, as specified by `.editorconfig`. Keep kebab-case component filenames and colocate Jasmine tests in `*.spec.ts`. VS Code selects Prettier; no lint npm script is configured.

### Module Organization and TypeScript

- Preserve strict TypeScript checking, ES2022 targeting, and Angular's strict template/injection checks.
- Default to standalone components. Keep NgModule wrappers for routing (`PagesModule`) and shared exports (`SharedModule`); use the `app-` selector prefix.
- Put business logic and HTTP calls in services; components handle presentation. Prefer `inject()` in guards and newer services.
- Use PascalCase classes and camelCase methods/variables. Use absolute `src/app/` imports instead of deep relative paths.
- Keep guards in `src/app/guards/`, functional interceptors in `src/app/http-interceptors/`, feature pages in `src/app/pages/`, reusable UI in `src/app/shared/`, services in `src/app/service/`, and feature interfaces in `src/app/types/`. Locale/timezone/storage support belongs in `src/app/core/`.
- Validate login and signup inputs before submission. Use `SnackbarService` for user-facing errors; existing diagnostic error logging uses `console.log`.
- Preserve the `tokenInfo` local-storage key, Bearer JWT handling, `jwt-decode` role extraction, and the existing token-refresh deduplication flow.

### Authentication and Public Routes

Preserve the route policy in `src/app/pages/routing.modules.ts`. The landing page, signup, email verification, forgot-password, and reset-password remain public. Login uses `AuthGuard`. Home/settings and Guild Mail use both `AuthGuard` and `HasRoleGuard` with their configured player role lists. Admin uses both guards with only `Admin` permitted. Preserve the Guild Mail ticket return link through sign-in and apply role restrictions where specified.

Load each page with `loadComponent`. Keep `PagesModule` as a routing wrapper without static imports of page implementations or their feature components; do not introduce route preloading. Admin-specific imports remain within the Admin lazy dependency graph. Shared dependencies may be shared between chunks. Preserve the landing-only prerender and client rendering for authenticated routes.

### Administration

The `/admin` page is part of the existing app and deployment, reached from the Home profile menu only for Admin. Its Users section calls existing `/api/user` server-side search/pagination; `name` filters email and `sortDirection` orders by update time, not a selectable table column. Permissions uses the complete `/api/user/role` response and filters/pages each role locally. Do not add account fields, detail navigation, metrics or private-game access that these endpoints do not supply.

Role assignment uses `PUT /api/user/role/apply` and deletion uses `DELETE /api/user/{id}`, always identifying the target by internal GUID. Block deleting/demoting the last Admin in the frontend using loaded role membership, and disable writes when membership is unavailable. This UI block does not enforce concurrency or prevent direct API requests. Preserve existing JWT renewal semantics: a role change does not immediately invalidate issued tokens. Keep user directories in memory rather than browser storage, and never add credential or private-content exposure to Admin.

Sparks settings and reporting live in the English-only Admin area. The player panel and public pilot copy follow the three-locale policy. Free gameplay eligibility and grant expiry come from the API. Preserve the player's draft during balance recovery or queue interaction.

### Google and account capabilities

Login/signup use email and omit username/full name. Signup confirms the password client-side. Fetch /api/user/auth/account for hasPassword/googleLinked. First-password creation requires a new linked-Google proof. Use PUBLIC_AUTH for anonymous Google operations. Authenticated Google proofs keep Bearer tokens but must not be automatically replayed after a 401. Load GIS only in the browser through GoogleAuthService and render its official button. Configure the public googleClientId for development or GOOGLE_CLIENT_ID for production builds.

### Visual Identity and Theming

- Maintain the Dark Fantasy / Gritty identity for solo RPG players: violet primary, orange tertiary, charcoal surfaces, and warm off-white text.
- Keep Material M3 theme definitions in `src/styles.scss`, using `mat.define-theme()`, `mat.$violet-palette`, and `mat.$orange-palette`. Use `--mat-sys-*` tokens for CSS colors instead of hardcoded colors.
- Use Averia Serif Libre for headings and Manrope for body/UI typography; preserve both theme definitions and the `latin-ext` font requests in `src/index.html`.
- Default to dark when no stored preference exists; preserve the `app-theme` local-storage key and synchronize the overlay container with the active theme. Initialize theming through `ThemeService` at application startup. The `app-theme-toggle` component changes the preference; dark-only pages may temporarily force dark and then restore the stored preference.
- Keep the EI-DUNGEON wordmark and fantasy icon on Auth and Signup pages. `palette-compare/` remains outside project scope.

## Commands

Run from `web/`:

```powershell
npm ci
npm start
npx tsc --project tsconfig.app.json --noEmit
npm test -- --watch=false --browsers=ChromeHeadless
$env:SITE_URL = 'http://localhost:8000'
$env:API_URL = 'http://localhost:8080'
npm run build
node scripts/verify-seo-output.mjs
npm audit --audit-level=high
```

The production build includes all three locales and prerenders the public landing pages. Use the same `SITE_URL` for SEO verification. Only public configuration belongs in frontend builds. The development API URL is set in `src/environments/environment.ts`; `API_URL` is a production build-time override, not an `ng serve` override.

## Testing and Quality Gates

Accompany every new `.ts` file with a `*.spec.ts` file. Cover each page/shared component, every public service method including error handling, and guards/interceptors. Tests must pass before submitting a PR; `tsconfig.spec.json` provides Jasmine types. Build, tests, and the production build are required quality gates alongside the existing type/SEO/audit checks.

Preserve production budgets: initial bundle 500 kB warning / 1 MB error, and component styles 6 kB warning / 10 kB error. The editor selects Prettier, but the repository has no installed/configured Prettier CLI, ESLint, or Git hooks.

## Internationalization

Administration pages are English-only, including their dialogs, role labels, feedback, accessibility labels and pagination. Do not add language selectors, i18n markers, `$localize` calls or translation catalog entries for administrative content. Keep English text and number formatting even when an administration page is opened under `/pt-BR/` or `/es/`; mark its content and dialog hosts with `lang="en"`. Shared navigation outside administration follows the language of its containing page.

For all other pages, every user-visible string addition, removal, or change requires an i18n update:

Use `$localize` for TypeScript messages and Angular i18n markers for template strings.

1. Run `npx ng extract-i18n` to regenerate `src/i18n/messages.xlf` (English).
2. Update matching IDs, sources, and translated targets in `messages.pt-BR.xlf` and `messages.es.xlf`.
3. Add new units and remove obsolete units consistently across all three catalogs.
4. Run the production build to catch missing translations; check affected locales and themes for layout changes.

## Git Workflow and Pull Requests

Require atomic Conventional Commits with an optional scope, using types such as `feat`, `fix`, `refactor`, `chore`, `style`, or `config`; for example, `fix(web): handle expired sessions`. Use sequential `NNN-feature-name` or timestamp `YYYYMMDD-HHMMSS-feature-name` identifiers for feature branches, with any prefix required by the active development environment. The main/default branch is the release source of truth. PRs must reference their feature specification and include the root guide's validation evidence and UI screenshots where relevant.

## Containers and CI

Run Compose from `web/`; it reads `web/.env` and uses project name `ei-dungeon-web`. The nginx container serves localized static output on host port 8000 by default. The active workflow is [web-container.yml](../.github/workflows/web-container.yml), with `web/` as Docker context and `web/package-lock.json` as npm's cache dependency path. The copied workflow under this module's `.github/` is inactive.

Production deployment uses the root `deploy-production.yml` workflow and sibling `../../infra/apps/dungeon/` configuration. Manual dispatch selects published commit tags resolved to digests. `DUNGEON_AUTO_DEPLOY=true` enables deployment after successful main publication. Keep public URLs correct at build time and follow the infrastructure deployment guide for SSH and readiness configuration.
