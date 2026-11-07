# Third-Party Data Processing — Technical Conformity

**Audit Date:** 2026-07-24
**Repository:** ei-dungeon-back
**Status:** Based on code audit evidence only

---

## 1. Active Third-Party Integrations

### 1.1 OpenRouter (LLM Provider)

- **Provider:** OpenRouter (`https://openrouter.ai/api/v1`)
- **Service:** LLM API proxy (routes to OpenAI `gpt-3.5-turbo-0125`)
- **Authentication:** API key (`keys:OpenRouterApiKey`)
- **Data sent:**
  - Player character info (name, description, race, skills)
  - WorldInfo (AI-generated game world JSON)
  - Play history (user prompts + AI narrations)
  - Summary text
  - Analysis results (structured)
- **Data NOT sent:**
  - Username, email, full name, password
  - JWT tokens, IP address, device info
- **Trigger:** Every play interaction (2-3 LLM calls per play)
- **Evidence:** `GenAi.cs:30-36`, `NewUserPlayUseCase.cs`, all play services
- **Contractual status:** NOT_VERIFIABLE — no DPA or contract evidence in repository
- **Data retention by provider:** NOT_VERIFIABLE — depends on OpenRouter terms
- **Data location:** NOT_VERIFIABLE — not specified in code

### 1.2 PostgreSQL (Database)

- **Provider:** Self-hosted or managed by a provider that has not yet been selected
- **Service:** Primary data store
- **Authentication:** Connection string (`PostgresConnection:PostgresConnectionString`)
- **Data stored:** All application entities (users, games, plays, players, refresh tokens, game info)
- **Evidence:** `Program.cs:126`, `EIContext.cs`
- **Deployment:** Hosting platform not yet selected

### 1.3 Microsoft Container Registry (Docker Images)

- **Provider:** Microsoft (`mcr.microsoft.com`)
- **Service:** .NET runtime and SDK Docker images
- **Images:** `dotnet/aspnet:8.0`, `dotnet/sdk:8.0`
- **Data:** No application data sent; build-time dependency only
- **Evidence:** `ei-back/Dockerfile`

---

## 2. Unused / Scaffolding Integrations

### 2.1 ExternalApiWebClient

- **Status:** NOT IMPLEMENTED / UNUSED
- **Evidence:** `Infrastructure/ExternalAPIs/Client/Core/ExternalApiWebClient.cs` exists but is not registered in DI (`AddInfraHttpClients()` is a no-op stub at `ServiceCollectionExtensions.cs:86-89`). No usages found in the codebase.

---

## 3. Integrations NOT Found

| Category | Status | Evidence |
|----------|--------|----------|
| Email provider (SMTP, SendGrid, etc.) | NOT PRESENT | No email sending code |
| Analytics (Sentry, Datadog, etc.) | NOT PRESENT | No analytics SDK |
| Payment processor (Stripe, etc.) | NOT PRESENT | No payment code despite PremiumUser role |
| Push notifications | NOT PRESENT | No notification code |
| CDN / asset hosting | NOT PRESENT | No CDN configuration |
| External logging (Seq, ELK, etc.) | NOT PRESENT | Logging is local file only |
| Social login (Google, GitHub, etc.) | NOT PRESENT | Only username/password auth |
| CAPTCHA / bot protection | NOT PRESENT | Rate limiting only |

---

## 4. Third-Party API Key Management

| Key | Config Location | Storage | Evidence |
|-----|----------------|---------|----------|
| OpenRouter API Key | `keys:OpenRouterApiKey` | User Secrets or env vars (placeholder in appsettings) | `GenAi.cs:30`, `appsettings.json:34` |
| JWT Secret | `TokenConfigurations:Secret` | User Secrets or env vars (placeholder in appsettings) | `Program.cs:52-58`, `appsettings.json:19` |
| PostgreSQL Connection String | `PostgresConnection:PostgresConnectionString` | User Secrets or env vars (placeholder in appsettings) | `Program.cs:126`, `appsettings.json:3` |

---

## 5. Observations

### 5.1 Minimal Third-Party Surface

The application has a very limited third-party integration surface. The only active external API call (beyond the database) is to OpenRouter for LLM processing. This simplifies the third-party compliance assessment.

### 5.2 No Contracts in Repository

No Data Processing Agreements (DPAs), Terms of Service, or contractual documents with third-party providers are present in the repository. These would need to be obtained from the organization.

### 5.3 OpenRouter Data Flow Risk

The OpenRouter integration is the highest-risk third-party relationship because:
1. User-generated text (play prompts) is sent on every interaction
2. Full game context (character info, world state, play history) is transmitted
3. No PII filtering is applied before transmission
4. Provider data retention and training policies are external to the codebase

---

## 6. Evidence Summary

| Integration | Data Flow | Contract Evidence | Retention Evidence |
|------------|-----------|-------------------|-------------------|
| OpenRouter | Game content to LLM | NOT_VERIFIABLE | NOT_VERIFIABLE |
| PostgreSQL | All app data | N/A (self-managed) | N/A |
| Hosting provider to be selected | Container hosting | NOT_VERIFIABLE | NOT_VERIFIABLE |
| MCR | Build images only | N/A | N/A |
