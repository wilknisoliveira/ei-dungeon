# EI Dungeon Backend — Architecture

## Stack
- **.NET 9** ASP.NET Core (`net9.0`; Docker SDK and runtime images also use .NET 9)
- **PostgreSQL** + EF Core 8 + Npgsql
- **JWT** auth with roles: `Admin`, `CommonUser`, `PremiumUser`
- **SignalR** hub at `/hubs` — JWT passed via `?access_token=` query param for WebSocket connections
- **GenAI**: OpenRouter via the OpenAI `ChatClient` and `Microsoft.Extensions.AI` adapter; configure `keys:OpenRouterApiKey` and `GenAISettings:AiModel`. The current client uses `https://openrouter.ai/api/v1`.
- **xUnit** + **FakeItEasy** + **FluentAssertions** for tests
- **Swagger** at `/swagger` (dev only), **Health** at `/health`
- **AutoMapper**, **Serilog** (file + console), **IStringLocalizer** for multi-language

## Project Layout (single-project Clean Architecture)
```
ei-back/
├── Core/Domain/         — Entities, Enums, DomainExceptions
├── Core/Application/    — UseCases, Services, Repository interfaces
├── Infrastructure/      — EF Context, Migrations, GenAI, Token, Extensions, Swagger, Mappings
├── UserInterface/Api/   — Controllers
└── UserInterface/Hubs/  — SignalR hubs
```

- No separate projects per layer; namespaces enforce boundaries.
- Controllers call **UseCases** which use **Services** and **Repository** interfaces.
- DI registration in `Infrastructure/Extensions/ServiceCollectionExtensions.cs`.
- AutoMapper profile in `Infrastructure/Mappings/MappingsProfile.cs`.

## API Controllers
| Route | File |
|---|---|
| `api/user/auth/signin` | `AuthController.cs` |
| `api/user` | `UserController.cs` |
| `api/game` | `GameController.cs` (GET, POST, PATCH `/{gameId}`, DELETE `/{gameId}`) |
| `api/play` | `PlayController.cs` |
| `api/game/GameInfo` | `GameInfoController.cs` |
| `api/role` | `RoleController.cs` |

## Game Language
Each game has a `GameLanguage` (enum: `Portuguese`, `English`, `Spanish`; default `English`) stored in the `game_language` column. The language is set at creation time via `GameDtoRequest.GameLanguage` and can be updated via `PATCH /api/game/{gameId}`. All GenAI system prompts are in English with a `<language>` instruction tag appended per game language, so the Game Master responds in the configured language. Changing the language affects only future responses — existing history is untouched.

## Conventions
- Async methods suffix with `Async` only for EF/IO-bound operations; some sync UseCases exist (`Signin`).
- `IUnitOfWork.Commit()` / `CommitAsync()` called in controllers after use case handlers. **Exception**: `NewUserPlayUseCase` handles its own commit internally for both first-play and subsequent-play flows, since streaming requires atomic persistence before the stream finalizes.
- Enums serialized as strings (`JsonStringEnumConverter`).
- All responses include `application/json` content type.

## Container Deployment
- Multi-stage Dockerfile uses .NET 9 SDK and ASP.NET Core runtime images.
- Final image runs as the non-root `app` user and exposes HTTP port 8080.
- The deployment target is an Ubuntu 24.04 VPS. Host Nginx terminates HTTPS for `dungeon.wildforges.com` and `api.dungeon.wildforges.com`; Certbot's Nginx plugin manages certificates and automatic renewal. Infrastructure bootstrap and deployment instructions live in the sibling `../../infra` repository.
- The sibling `../../infra/compose.yaml` defines the `dungeon-production` stack using versioned GHCR image references supplied through a Git-ignored `.env`, frontend `127.0.0.1:8000`, and backend `127.0.0.1:8080`. PostgreSQL 16 has a persistent named volume, no published port, and an internal network shared only with the backend. A second backend network allows outbound AI requests. Database health gates backend startup; fresh volumes initialize `uuid-ossp`. Container logs rotate. `../../infra/DEPLOYMENT.md` documents manual deployment, backups, and independent application updates. The local API Compose retains its services and uses explicit project name `ei-dungeon-back` to preserve the existing database volume after nesting under `api/`. `AddTrustedReverseProxy` configures one-hop `X-Forwarded-For`/`X-Forwarded-Proto` processing; `UseForwardedHeaders` runs first in the request pipeline. `ReverseProxy:KnownProxies` adds explicit comma-separated IPs to framework loopback trust and rejects malformed addresses. Production Compose passes the existing backend bridge gateway through `ReverseProxy__KnownProxies`; Docker manages subnet allocation. A new backend image is required to deploy this behavior.

## Container CI
- The monorepo root `../.github/workflows/api-container.yml` runs on pull requests targeting `main` and pushes to `main`.
- A .NET 9 job restores dependencies, builds the application and tests in Release mode, and runs unit tests. The container job requires this job to pass.
- Docker Buildx builds `api/ei-back/Dockerfile` with `api/` as context (paths from the monorepo root) for Linux AMD64, reusing GitHub Actions build cache.
- Pull requests build without publishing or registry login. Pushes to `main` publish `ghcr.io/<owner>/ei-dungeon-back:sha-<full-commit-sha>` and `:latest` using the built-in `GITHUB_TOKEN` with `packages: write` permission.
- Database credentials, JWT secrets, and AI keys are supplied at runtime, not during CI. Deployment remains outside this workflow.

## Local Docker Compose

The API module `api/compose.yaml` defines two services. Run Compose from `api/`; its explicit project name is `ei-dungeon-back`, and its named database volume remains `ei-dungeon-back_postgres_data`:

```text
.env
  └─> Docker Compose substitutions
        ├─> backend (ASP.NET Core environment variables)
        │     └─> depends on postgres: service_healthy
        └─> postgres (database, user, password)
              └─> postgres_data named volume
```

- Developers copy `.env.example` to the Git-ignored `.env`. Compose explicitly maps those values to nested ASP.NET Core keys using double underscores; container environment values override `appsettings.json`.
- The backend connects to host `postgres` on the internal Compose network. Host ports for the backend and PostgreSQL remain configurable through `.env`.
- PostgreSQL 16 runs `docker/postgres/init-uuid-ossp.sql` only when initializing a fresh named volume. This makes `uuid_generate_v4()` available to the historical EF migrations.
- PostgreSQL has a database-aware health check. Compose starts the backend only after that check passes.
- Both Compose services use `restart: unless-stopped`, so Docker restarts them after failures or daemon restarts unless a user stopped them explicitly.
- The backend retains `dbContext.Database.Migrate()` at application startup, so it applies pending migrations after the database becomes ready.
- Service-scoped Compose commands manage `backend` without recreating or removing PostgreSQL. `docker compose down -v` is the explicit destructive database reset.

## Email Verification and Password Recovery

- `User.NormalizedEmail` stores trimmed invariant-uppercase email and has a unique index; `EmailVerifiedAt` is nullable. `UserRepository.FindByEmail` uses the same normalizer as registration and challenge flows. The migration `20261101152632_EmailVerificationRecovery` backfills normalized emails without automatically verifying existing accounts.
- Public POST routes under `/api/user/auth`: `email-verification`, `email-verification/confirm`, `password-recovery`, `password-recovery/confirm`. Request routes accept email and return generic empty 202 acknowledgements. Confirmation accepts email/code (plus newPassword for recovery), returns empty 204 or a generic 400. Data annotation failures use normal MVC validation responses.
- Registration retains its existing response and issues the first email-verification code. Sign-in checks the password before returning 403 with `code: email_verification_required` for an unverified user; no token pair is issued. Refresh rejects unverified users with generic 401. Confirmation does not automatically sign in or force a password change.
- `VerificationCode` binds a challenge to user, normalized email snapshot and purpose (`EmailVerification`, `PasswordRecovery`). Six-digit strings are generated with cryptographic randomness and stored as HMAC-SHA256 values bound to record ID, user, purpose and email. Codes expire after ten minutes, allow five failed matches and are single use. Resending invalidates older challenges. History enforces one issuance per rolling minute and five per rolling hour per user/purpose, including signup.
- Application `VerificationFlowService` coordinates repositories, lifecycle rules and the email sender. Dedicated use cases implement each request/confirm action. `IUnitOfWork` lives in `Core/Application/Repository`; Infrastructure implements it. `ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>>, CancellationToken)` owns the transaction and automatic save/commit. It rejects nested transactions (including another UnitOfWork sharing the context) and manual Commit/CommitAsync/Rollback inside its operation; failures and cancellation roll back and clear tracking. Uniqueness violations during CommitAsync or explicit transaction execution become a generic Application `PersistenceConflictException`, mapped to registration conflict by the signup use case. `VerificationCodeRepository.AcquireLockAsync` requires an active transaction and acquires a PostgreSQL transaction-scoped advisory lock keyed by normalized email. Issuance/confirmation queries occur after lock acquisition. Failed confirmation outcomes commit attempt changes before controllers map them to errors. Recovery password hashing, code consumption and deletion of the user's refresh tokens share one transaction. Already-issued access tokens expire normally.
- **Commit convention exception:** registration saves the new user and initial code with one `IUnitOfWork.CommitAsync` inside Application; it does not acquire the challenge lock or open an explicit transaction. Concurrent registrations are protected by unique database indexes. Existing-account challenge flows use `IUnitOfWork.ExecuteInTransactionAsync` and acquire the email lock before querying state. Signup no longer calls a second controller commit. Unrelated use cases retain their existing convention. Email IO is attempted only after commit, outside locks. Delivery failure is logged without message bodies, codes, passwords or provider exception text and never rolls back committed state. No outbox or delivery retry worker is included.
- Application owns `IEmailSender`; Infrastructure registers `ResendEmailSender` using the Resend 0.24.0 SDK via injected `IResend` and its managed HTTP client. Configure `Resend:ApiToken` and `Resend:From` (including optional display name); sender syntax and nonempty token are validated when the relevant options are resolved. From must be allowed by the Resend account/domain. Application owns the provider-independent OutgoingEmail value and IVerificationEmailComposer/VerificationEmailComposer. VerificationFlowService composes the purpose-specific localized subject and HTML/plain-text bodies after commit, then passes the ready message to IEmailSender.SendAsync. CodeDelivery carries the lifetime derived from the created record (ExpiresAt minus CreatedAt), so email wording uses that value rather than a second ten-minute constant. ResendEmailSender only maps recipient, subject and bodies to the SDK message, applies the configured From address, and submits it; it knows neither verification purposes nor code-format rules. Cryptographic code generation and confirmation validation remain in their existing verification components. It forwards cancellation and rejects unsuccessful SDK responses even when no exception was thrown. No token/code/provider error body is logged. A successful API submission is acceptance by Resend, not confirmed inbox delivery; there are no delivery webhooks or automatic application retries. `UnavailableEmailSender` was removed. Local Compose maps `RESEND_API_TOKEN`, `RESEND_FROM`, and `VERIFICATION_CODE_HASH_KEY` to the corresponding backend options; production environments can provide `Resend__ApiToken`, `Resend__From`, and `VerificationCode__HashKey` directly. Configure `VerificationCode:HashKey` through User Secrets or `VerificationCode__HashKey` environment configuration with at least 32 UTF-8 bytes; there is no working default. Resolving the protector fails clearly if the key is missing/short. Rotating the key invalidates outstanding codes.
- `VerificationRequest` shares five requests/minute/IP between both request routes. `VerificationConfirm` shares ten between both confirmation routes. Both use six sliding-window segments, zero queue, HTTP 429 and Retry-After if the framework supplies metadata. CORS exposes Retry-After; it may be absent. All policies derive the IP from `Connection.RemoteIpAddress` only, after trusted one-hop forwarded-header processing. Middleware buckets are instance-local; database challenge controls persist across restarts and IP changes.
- Generic request acknowledgements do not guarantee delivery or complete account secrecy: the existing `check-userinfo` endpoint still exposes email availability, and constant-time issuance responses are not provided. No new role or active flags are imported from the reference system.
- Frontend implementation is outside this feature. Its integration guide is generated at `docs/frontend-email-auth.md` only after backend validation.
- New feature classes use primary constructors with descriptive dependency names, including the Repository suffix (userRepository, verificationCodeRepository, refreshTokenRepository). Pre-existing classes retain their prior constructor style; CreateUserUseCase keeps its explicit constructor and readonly fields. The former VerificationTransaction class and interface have been removed. Other existing Application dependencies on Infrastructure (exceptions, token configuration and extensions) remain outside this refactor.

## Email visual template

VerificationEmailComposer uses the shared Application-owned Templates/VerificationEmailTemplate.cs for both verification and password recovery. The template follows the sibling frontend light theme: text wordmark EI-DUNGEON, violet palette tone 40 (#7d00fa), orange palette tone 40 (#964900), on-surface #1d1b1e and on-surface-variant #49454e. It declares Averia Serif Libre/Georgia for headings and Manrope/Helvetica/Arial for body text, with local font fallbacks and no external stylesheet/font/image dependencies. No background colors are applied; the default canvas is white. Inline styles and presentation tables provide a fluid layout capped at 560px. Metadata requests light color scheme, although final dark-mode behavior belongs to the mail client. All interpolated HTML text is encoded. The six-digit code remains contiguous selectable text, with expiration taken from CodeDelivery.ValidFor. Plain-text content remains available; no provider/transport or API contract changed. Sample previews with a fictitious code live under docs/email-previews/; desktop and a 360px mobile viewport were inspected in Chrome, not live mail clients.

## Request and email localization

`AddApplicationLocalization` registers the existing ASP.NET localization system and supported formatting/UI cultures `en`, `pt-BR`, and `es`, defaulting to `en`. `UseRequestLocalization` runs immediately after trusted forwarded headers, before endpoints and other culture consumers. The frontend sends its selected locale in `Accept-Language` for signup, verification requests and password recovery requests. Standard provider priority remains query string, culture cookie, Accept-Language; unsupported values fall back to the configured default if no provider selects a supported culture. Parent culture fallback supports en-US -> en and es-ES -> es; Portuguese uses pt-BR explicitly. CORS permits Accept-Language.

VerificationEmailComposer receives IStringLocalizer<VerificationEmailComposer>. Translated subjects and all HTML/plain-text wording reside in Infrastructure/Resources/Core/Application/Service/Email/VerificationEmailComposer[.pt-BR|.es].resx, with the neutral resource providing English. The template receives translated values, encodes them, and uses the resource language for HTML lang and lifetime number formatting. No translated strings remain fixed in the HTML layout. The language is selected per request, not stored on the user or derived from GameLanguage. Email transport remains provider-independent.

LocalizationAssemblyInfo.cs declares RootNamespace ei_back because the assembly name is ei-back, and declares neutral resources as English. AuthController resources moved from the generic en-US.resx to correctly scoped UserInterface/Api/AuthController resource files for the three languages. Other hardcoded API errors were not migrated in this feature. Tests use real resources through the shared registration and verify header selection, fallback and both email purposes.
## Provisioned administrator email state

Migration 20261102183758_MarkSeededAdminEmailVerified sets EmailVerifiedAt to 2026-11-02 00:00:00 UTC only for the seeded administrator ID 7d9ff283-6174-40a6-a317-f32a4a0620d0 when its timestamp is null. This is an explicit provisioning decision for the intentionally fictitious account, not a role-based authentication bypass. Existing timestamps and all other accounts remain unchanged. Rollback clears only the matching provisioning timestamp. Login, refresh, email verification and recovery rules are unchanged; the account now satisfies their existing verified-email checks. No new columns are introduced and historical seed migrations remain intact.
