# API Guidelines

## Required Context

Read [README.md](README.md) and [ARCHITECTURE.md](ARCHITECTURE.md) before API analysis or implementation. Read ignored `LOCAL_GUIDES.md`, when present, for machine-specific setup; keep new local environment details there without copying credentials into tracked documentation. Review and update the architecture document when implementation changes architecture.

This guide consolidates `.aipartner/memory/project-standards.md`. For outdated version, deployment, CI, testing, or transaction descriptions in that historical snapshot, follow this guide and the current architecture.

## Repository Skills

- [aspnet-core](../.agents/skills/aspnet-core/SKILL.md) for work on controllers, middleware, hosting, dependency injection, authentication, authorization, or SignalR.
- [csharp-developer](../.agents/skills/csharp-developer/SKILL.md) for C# implementation involving domain logic, use cases, services, EF Core persistence, or async and cancellation behavior.
- [rest-api-design](../.agents/skills/rest-api-design/SKILL.md) for changes to REST routes, HTTP methods, request and response contracts, status codes, or versioning.

## Layout and Conventions

`ei-back/` is one .NET 9 application with `Core/Domain`, `Core/Application`, `Infrastructure`, and `UserInterface` layers. Controllers call use cases and services; persistence uses repository interfaces and Unit of Work. Respect the documented transaction and streaming commit exceptions. Preserve namespaces and existing project names.

Use four-space indentation and descriptive dependency names. Follow nearby constructor style; newer feature classes use primary constructors. Forward cancellation through I/O. Tests live in `ei-back.Tests/`, with xUnit, FakeItEasy, and FluentAssertions; follow the existing `Operation_Condition_ExpectedResult` naming pattern.

### Architecture Rules

- Keep `Core/Domain` independent of `Infrastructure` and `UserInterface`; layer boundaries are enforced through namespaces within the single application project.
- Keep business logic out of controllers. Delegate each operation to a use case and return the appropriate DTO.
- Access persistence through repository interfaces; do not use `DbContext` directly in application code.
- Use constructor injection for dependencies, never a service locator. Central DI registration lives in `Infrastructure/Extensions/ServiceCollectionExtensions.cs`.
- Organize use cases under `Core/Application/UseCase/{Domain}/`, domain services under `Core/Application/Service/{Domain}/`, and their contracts in `Interfaces/` or `Core/Application/Repository/`.

### Unit of Work and Transactions

- Normally, the controller calls `IUnitOfWork.Commit()` or `CommitAsync()` after its use case finishes.
- Preserve `NewUserPlayUseCase`'s internal commit for streaming atomicity.
- Registration saves the user and initial verification code with one Application-owned `CommitAsync()`; do not add another controller commit.
- Existing-account verification/recovery flows use `ExecuteInTransactionAsync` and acquire the email lock before querying challenge state. The transaction owns saving/committing; do not nest transactions or manually commit/rollback inside it.
- Send verification/recovery email after the database commit, outside locks. Follow `ARCHITECTURE.md` for concurrency, failure handling, and cancellation details.

### Naming, Errors, and API Contracts

- Use PascalCase for C# classes, methods, properties, and enum values; use snake_case for database columns. Serialize enums as strings with `JsonStringEnumConverter`.
- Preserve nullable reference types and implicit usings in the project configuration.
- Use the `Async` suffix for EF/I/O-bound operations, following existing exceptions for synchronous use cases.
- Define domain exceptions in `Core/Domain/DomainExceptions/`; map application errors to HTTP through `Infrastructure/Exceptions/AppExceptionHandler`.
- Use structured Serilog logging through `ILogger<T>`; file output rolls daily alongside console output.
- Document every endpoint with Swagger XML summaries/remarks and `[ProducesResponseType]` attributes. Preserve the existing rate-limit policies for public and authenticated endpoints.
- Keep mappings in `Infrastructure/Mappings/MappingsProfile.cs` and persistence configuration around `EIContext`.

## Commands

Run from `api/`:

```powershell
dotnet build ei-back.Tests/ei-back.Tests.csproj -c Release
dotnet test ei-back.Tests/ei-back.Tests.csproj -c Release
dotnet run --project ei-back/ei-back.csproj
docker compose up -d --build
```

Run EF commands from `api/ei-back/`: `dotnet ef migrations add <Name> --context EIContext` or `dotnet ef database update --context EIContext`. Apply migrations only to the intended database. Enable `uuid-ossp` for historical migrations. Database tests require `VERIFICATION_TEST_CONNECTION` pointing to an isolated, migrated database.

## Testing and Quality Gates

The build and existing test suite must pass before work is considered done. Cover domain-service behavior and encryption; use `A.Fake<T>()` and `A.CallTo(() => ...).Returns(...)` for mocks and FluentAssertions for assertions. Test files remain in the separate `ei-back.Tests/` project, with domain, application/use-case, and infrastructure suites. Extend the relevant suites when changing behavior. No .NET formatter or API-specific `.editorconfig` is currently configured.

## Commits and Pull Requests

Require atomic Conventional Commits with an optional scope, for example `feat(api): add game export`. The API's existing type vocabulary includes `feat`, `fix`, `config`, `refac`, and `doc`. Follow the root guide for PR descriptions and validation evidence.

## Configuration and Containers

Use .NET User Secrets or runtime environment variables for PostgreSQL, `keys:OpenRouterApiKey`, `GenAISettings:AiModel`, `TokenConfigurations:Secret`, `Resend:ApiToken`, `Resend:From`, and `VerificationCode:HashKey`. See [README.md](README.md) for mappings and setup. Preserve the project UserSecretsId. Never log tokens, verification codes, passwords, or provider error bodies.

Direct development HTTP uses port 5231; Compose defaults to API port 8080 and web origin 8000. Keep Compose project name `ei-dungeon-back` to retain the existing database volume. Both Docker SDK and runtime images use .NET 9. The active workflow is [api-container.yml](../.github/workflows/api-container.yml), with `api/` as its Docker context. Its copied module-level workflow is inactive.

JWT authentication uses HS256 with `Admin`, `CommonUser`, and `PremiumUser` roles. SignalR is exposed at `/hubs`, with its JWT supplied through `access_token` for WebSocket connections. The API health endpoint is `/health`. AI calls use OpenRouter through the OpenAI connector. The current deployment target is an Ubuntu VPS; see `ARCHITECTURE.md` and the sibling infrastructure repository for its HTTPS, network, and image configuration.
