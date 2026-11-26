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
- Each use case and its interface expose one public operation named `Handler`. Group classes, interfaces and DTOs by feature. Reusable helpers belong in Application services; independent operations remain separate use cases.
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

## Free Sparks

CommonUser gameplay uses Spark grants. Creation, first scene and later turns are separate paid operations. Creation and streaming finalize in Application-owned transactions on the Free path. Each reserves before AI and commits the saved result with the debit. The reservation has a per-attempt lease and a client operation ID so a stale worker cannot commit after a retry and a lost response can be reconciled. Admin and Premium retain their existing gameplay transaction paths.

Spark allocation, queue distribution and administrative changes share a PostgreSQL transaction lock. Cycle availability accounts for consumed Sparks, unexpired grants and active reservations on expired grants. A grant always charges its funding cycle, including after rollover. A hosted service handles renewal, periodic distribution and daily snapshots. It checks OpenRouter through a separate management client and sends email through Resend after persistence. The admin API exposes audited settings, cycle actions, snapshots and the latest observed credits.

Rollover checks OpenRouter coverage against full cycle capacity plus outstanding commitments from earlier cycles. A manual reset of a deficit-blocked cycle uses the current control line rounded up to whole Sparks plus those commitments. Resetting the cycle preserves its capacity.

## Support Tickets

The support ticket API owns private conversations keyed by the player's internal user ID. The player-facing endpoints expose only the caller's tickets. Admin endpoints provide cross-user listings, server-side filters and stable pagination, and status, type, and reply updates. Tickets and messages live in PostgreSQL.

Ticket writes that change an existing conversation use one Application-owned transaction and serialize operations on that ticket. This protects closure, reply, and rating invariants. Creation commits the ticket and initial message together. Notifications are sent after persistence through `ISupportTicketNotificationService` and the existing provider-independent `IEmailSender`. The email describes the event and shows the recorded update time. A combined administrative reply and status change emits one email, localized using the language captured at ticket creation. Provider failures do not change the saved ticket. Sparks reports and team ticket alerts resolve the same `Notifications:AdministrativeRecipientsCsv` setting with the legacy Sparks fallback.

## Conventions
- EF/I/O-bound service and repository methods use the `Async` suffix. Use-case entry methods retain the established `Handler` name for synchronous and asynchronous operations.
- Controllers normally call `IUnitOfWork.Commit()` or `CommitAsync()` after use case handlers. Gameplay streaming owns its commit in the application layer. Free adventure creation also owns its commit so the debit and adventure are saved together.
- Enums serialized as strings (`JsonStringEnumConverter`).
- All responses include `application/json` content type.

## Container Deployment
- Multi-stage Dockerfile uses .NET 9 SDK and ASP.NET Core runtime images.
- Final image runs as the non-root `app` user and exposes HTTP port 8080.
- The deployment target is an Ubuntu 24.04 VPS. Host Nginx terminates HTTPS for `dungeon.wildforges.com` and `api.dungeon.wildforges.com`; Certbot's Nginx plugin manages certificates and automatic renewal. Infrastructure bootstrap and deployment instructions live in the sibling `../../infra` repository.
- The sibling `../../infra/apps/dungeon/compose.yaml` defines the `dungeon-production` stack using versioned GHCR image references supplied through a Git-ignored `.env`, frontend `127.0.0.1:8000`, and backend `127.0.0.1:8080`. PostgreSQL 16 has a persistent named volume, no published port, and an internal network shared only with the backend. A second backend network allows outbound AI requests. Database health gates backend startup; fresh volumes initialize `uuid-ossp`. Container logs rotate. `../../infra/apps/dungeon/DEPLOYMENT.md` documents manual deployment, backups, and independent application updates. The local API Compose retains its services and uses explicit project name `ei-dungeon-back` to preserve the existing database volume after nesting under `api/`. `AddTrustedReverseProxy` configures one-hop `X-Forwarded-For`/`X-Forwarded-Proto` processing; `UseForwardedHeaders` runs first in the request pipeline. `ReverseProxy:KnownProxies` adds explicit comma-separated IPs to framework loopback trust and rejects malformed addresses. Production Compose passes the existing backend bridge gateway through `ReverseProxy__KnownProxies`; Docker manages subnet allocation. A new backend image is required to deploy this behavior.

## Container CI
- The monorepo root `../.github/workflows/api-container.yml` runs on pull requests targeting `main` and pushes to `main`.
- A .NET 9 job restores dependencies, builds the application and tests in Release mode, and runs unit tests. The container job requires this job to pass.
- Docker Buildx builds `api/ei-back/Dockerfile` with `api/` as context (paths from the monorepo root) for Linux AMD64, reusing GitHub Actions build cache.
- Pull requests build without publishing or registry login. Matching pushes to `main` publish `ghcr.io/<owner>/ei-dungeon/api:sha-<full-commit-sha>` and `:latest` using the built-in `GITHUB_TOKEN` with `packages: write` permission.
- Database credentials, JWT secrets, AI keys, Resend settings and the verification hash key are supplied at runtime. Production deployment uses the shared `../.github/workflows/deploy-production.yml` workflow through SSH. Manual dispatch selects a published commit tag per service and pins its digest. `DUNGEON_AUTO_DEPLOY=true` enables an optional call after successful main publication. The provisioned root-owned script in `../../infra/apps/dungeon/scripts/deploy.sh` serializes updates, backs up PostgreSQL before backend replacement and verifies local/public readiness. The `production` environment supports optional required reviewers subject to GitHub plan features. Failed updates retain previous image references and operation state. Backend rollback requires schema compatibility because startup applies migrations. `/health` covers PostgreSQL, while auth/email functionality needs separate operational verification.

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
- Registration accepts email/password, returns email and issues the first email-verification code. Sign-in checks the password before returning 403 with `code: email_verification_required` for an unverified user; no token pair is issued. Refresh rejects unverified users with generic 401. Confirmation does not automatically sign in or force a password change.
- `VerificationCode` binds a challenge to user, normalized email snapshot and purpose (`EmailVerification`, `PasswordRecovery`). Six-digit strings are generated with cryptographic randomness and stored as HMAC-SHA256 values bound to record ID, user, purpose and email. Codes expire after ten minutes, allow five failed matches and are single use. Resending invalidates older challenges. History enforces one issuance per rolling minute and five per rolling hour per user/purpose, including signup.
- Application `VerificationFlowService` coordinates repositories, lifecycle rules and the email sender. Dedicated use cases implement each request/confirm action. `IUnitOfWork` lives in `Core/Application/Repository`; Infrastructure implements it. `ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>>, CancellationToken)` owns the transaction and automatic save/commit. It rejects nested transactions (including another UnitOfWork sharing the context) and manual Commit/CommitAsync/Rollback inside its operation; failures and cancellation roll back and clear tracking. Uniqueness violations during CommitAsync or explicit transaction execution become a generic Application `PersistenceConflictException`, mapped to registration conflict by the signup use case. `VerificationCodeRepository.AcquireLockAsync` requires an active transaction and acquires a PostgreSQL transaction-scoped advisory lock keyed by normalized email. Issuance/confirmation queries occur after lock acquisition. Failed confirmation outcomes commit attempt changes before controllers map them to errors. Recovery password hashing, code consumption and deletion of the user's refresh tokens share one transaction. Already-issued access tokens expire normally.
- **Commit convention exception:** registration saves the new user and initial code with one `IUnitOfWork.CommitAsync` inside Application; it does not acquire the challenge lock or open an explicit transaction. Concurrent registrations are protected by unique database indexes. Existing-account challenge flows use `IUnitOfWork.ExecuteInTransactionAsync` and acquire the email lock before querying state. Signup does not call a second controller commit. Unrelated use cases retain their existing convention. Email IO is attempted only after commit, outside locks. Delivery failure is logged without message bodies, codes, passwords or provider exception text and never rolls back committed state. No outbox or delivery retry worker is included.
- Application owns `IEmailSender`; Infrastructure registers `ResendEmailSender` using the Resend 0.24.0 SDK via injected `IResend` and its managed HTTP client. Configure `Resend:ApiToken` and `Resend:From` (including optional display name); sender syntax and nonempty token are validated when the relevant options are resolved. From must be allowed by the Resend account/domain. Application owns the provider-independent OutgoingEmail value and IVerificationEmailComposer/VerificationEmailComposer. VerificationFlowService composes the purpose-specific localized subject and HTML/plain-text bodies after commit, then passes the ready message to IEmailSender.SendAsync. CodeDelivery carries the lifetime derived from the created record (ExpiresAt minus CreatedAt), so email wording uses that value rather than a second ten-minute constant. ResendEmailSender only maps recipient, subject and bodies to the SDK message, applies the configured From address, and submits it; it knows neither verification purposes nor code-format rules. Cryptographic code generation and confirmation validation remain in their existing verification components. It forwards cancellation and rejects unsuccessful SDK responses even when no exception was thrown. No token/code/provider error body is logged. A successful API submission is acceptance by Resend, not confirmed inbox delivery; there are no delivery webhooks or automatic application retries. `UnavailableEmailSender` was removed. Local Compose maps `RESEND_API_TOKEN`, `RESEND_FROM`, and `VERIFICATION_CODE_HASH_KEY` to the corresponding backend options; production environments can provide `Resend__ApiToken`, `Resend__From`, and `VerificationCode__HashKey` directly. Configure `VerificationCode:HashKey` through User Secrets or `VerificationCode__HashKey` environment configuration with at least 32 UTF-8 bytes; there is no working default. Resolving the protector fails clearly if the key is missing/short. Rotating the key invalidates outstanding codes.
- `VerificationRequest` shares five requests/minute/IP between both request routes. `VerificationConfirm` shares ten between both confirmation routes. Both use six sliding-window segments, zero queue, HTTP 429 and Retry-After if the framework supplies metadata. CORS exposes Retry-After; it may be absent. All policies derive the IP from `Connection.RemoteIpAddress` only, after trusted one-hop forwarded-header processing. Middleware buckets are instance-local; database challenge controls persist across restarts and IP changes.
- Generic request acknowledgements do not guarantee delivery or complete account secrecy: the public `check-userinfo` endpoint has been retired. Duplicate-registration outcomes and synchronous delivery timing can still disclose information, and constant-time issuance responses are not provided. No new role or active flags are imported from the reference system.
- Frontend integration conventions are documented in [the web README](../web/README.md). Local login/signup use email and preserve the existing verification/recovery routes. HTTP contracts are defined by controller actions, DTOs and Swagger.
- New feature classes use primary constructors with descriptive dependency names, including the Repository suffix (userRepository, verificationCodeRepository, refreshTokenRepository). Pre-existing classes retain their prior constructor style; CreateUserUseCase keeps its explicit constructor and readonly fields. The former VerificationTransaction class and interface have been removed. Other existing Application dependencies on Infrastructure (exceptions, token configuration and extensions) remain outside this refactor.

## Email visual template

All user-facing emails use the shared Application-owned EI Dungeon layout under `Core/Application/Service/Email/Templates/` and include an equivalent plain-text part. The shared layout follows the frontend light theme with the EI-DUNGEON wordmark, violet and orange accent colors, inline styles, and presentation tables capped at 560px. Its font stacks use local fallbacks. Metadata requests a light color scheme. User-provided text is HTML-encoded. Verification and password recovery retain the selectable six-digit code and lifetime from `CodeDelivery.ValidFor`. Guild Mail uses event details and the conversation link. The Sparks waitlist grant preserves the amount and expiration from the grant. Sample verification previews with a fictitious code live under `docs/email-previews/`.

## Request and email localization

`AddApplicationLocalization` registers the existing ASP.NET localization system and supported formatting/UI cultures `en`, `pt-BR`, and `es`, defaulting to `en`. `UseRequestLocalization` runs before endpoints and other culture consumers. The frontend sends its selected locale in `Accept-Language` on API requests. Password recovery uses the current request culture. Guild Mail and the Sparks queue capture that culture at creation for later notifications. Standard provider priority is query string, culture cookie, and then `Accept-Language`. Unsupported values fall back to the configured default if no provider selects a supported culture. Parent culture fallback supports `en-US` to `en` and `es-ES` to `es`. Portuguese uses `pt-BR` explicitly. CORS permits `Accept-Language`.

VerificationEmailComposer receives IStringLocalizer<VerificationEmailComposer>. Translated subjects and all HTML/plain-text wording reside in Infrastructure/Resources/Core/Application/Service/Email/VerificationEmailComposer[.pt-BR|.es].resx, with the neutral resource providing English. The template receives translated values, encodes them, and uses the resource language for HTML lang and lifetime number formatting. No translated strings remain fixed in the HTML layout. The language is selected per request, not stored on the user or derived from GameLanguage. Email transport remains provider-independent.

LocalizationAssemblyInfo.cs declares RootNamespace ei_back because the assembly name is ei-back, and declares neutral resources as English. AuthController resources moved from the generic en-US.resx to correctly scoped UserInterface/Api/AuthController resource files for the three languages. Other hardcoded API errors were not migrated in this feature. Tests use real resources through the shared registration and verify header selection, fallback and both email purposes.
## Provisioned administrator email state

Migration 20261102183758_MarkSeededAdminEmailVerified sets EmailVerifiedAt to 2026-11-02 00:00:00 UTC only for the seeded administrator ID 7d9ff283-6174-40a6-a317-f32a4a0620d0 when its timestamp is null. This is an explicit provisioning decision for the intentionally fictitious account, not a role-based authentication bypass. Existing timestamps and all other accounts remain unchanged. Rollback clears only the matching provisioning timestamp. Login, refresh, email verification and recovery rules are unchanged; the account now satisfies their existing verified-email checks. No new columns are introduced and historical seed migrations remain intact.

Migration `UseReservedSeededAdminEmail` replaces the seeded administrator's original `admin@gmail.com` with `admin@ei-dungeon.invalid`, updating both email and uppercase normalized email only when the known seeded GUID still uses the original address. Customized addresses and all other account fields remain unchanged. The account retains its provisioned verification state, but cannot receive password recovery email. Historical migrations remain intact, so fresh databases receive the correction after the original seed. Rolling back this corrective migration restores the original address only for the matching seeded GUID and replacement email fields.

## Google authentication and internal identity

User stores email, normalized email, verified timestamp, optional password, role and the existing GUID. UserName and FullName are removed by SimplifyUserIdentityAndAddGoogleAuth. Existing user IDs, password hashes, roles and ownership remain intact. Authenticated game/play/refresh/logout operations resolve the internal subject using GetUserIdUseCase. Both old signed subject claims and newly issued tokens work. New JWTs contain sub, email, role and jti, without the removed fields. ApplicationSessionService shares session issuance across local and Google sign-in.

GoogleAuthController exposes /api/user/auth/google/challenge, /google, /google/link, /password and /account. Each endpoint delegates to a separate Application use case in Core/Application/UseCase/GoogleAuth: IssueGoogleChallengeUseCase, GoogleSigninUseCase, LinkGoogleUseCase, CreatePasswordUseCase and GetCurrentAccountUseCase. Each class and interface has a single public Handler method. Feature DTOs live in GoogleAuth/Dtos; shared local-user/token contracts stay in User/Dtos. GoogleAuthFlowService shares proof consumption, verified-account lookup, identity association and transaction/conflict handling through an Application interface. Business decisions remain in the individual use cases. GoogleIdentityVerifier implements the Application verification interface with Google.Apis.Auth in Infrastructure. No ASP.NET Identity migration or Client Secret exchange is introduced.

ExternalIdentity has unique provider/subject and user/provider indexes, giving one Google identity per local account. Explicit linking requires a verified local account, the same verified provider email and an unclaimed subject. Returning sign-in resolves the stored subject before considering new-registration email criteria and never silently changes the local email. New authoritative verified Gmail/Workspace identities create CommonUser accounts without local passwords. Others receive traditional-registration guidance. Matching existing email requires explicit authenticated linking.

GoogleAuthChallenge persists hashes of a random nonce and browser secret, purpose, optional internal user ID, five-minute expiry and consumption timestamp. HttpOnly per-attempt cookies use SameSite=Strict, /api/user/auth path and Secure in production. This fits the current same-site subdomains and consistent localhost development. Cross-origin browser JSON requests use the CORS policy in Program.cs, without a separate controller-level Origin check. Expired attempts are pruned when issuing new attempts. Provider token validation occurs before database transactions. Atomic conditional consumption and unique indexes prevent replay/duplicate identities. Subject advisory locks serialize same-provider account creation. Account locks protect first-password creation, which reloads password state after acquiring the existing normalized-email lock. Successful attempts and account/session writes share one transaction.

Settings retrieves current account capabilities from the API. Google-only users confirm the already linked provider identity and create a bcrypt local password. Existing-password changes still require currentPassword. Recovery issuance and confirmation require both verified email and a pre-existing password. Recovery remains generic and cannot establish the first password. Association and user-bound challenge records cascade on account deletion.

GoogleAuth:ClientId is runtime configuration. The web uses public GOOGLE_CLIENT_ID at build time or googleClientId in its development environment. Missing configuration disables Google without blocking local authentication. Google proof endpoints preserve rate limits and stable errors. Anonymous proofs avoid Bearer injection/refresh. Authenticated proofs retain Bearer credentials but are not automatically replayed after a provider 401. GIS loads only in browser rendering, so public prerender stays supported.

The administrator tutorial is docs/google-auth-setup.md. Production environment wiring and rollback instructions are maintained in English in ../../infra/apps/dungeon/DEPLOYMENT.md. Coordinate the frontend/API rollout, validate a database backup, and account for old API images being incompatible after column removal. The migration blocks schema-only Down because it cannot restore dropped values.
