# Third-Party Processors

**Audit Date:** 2026-07-24
**Repository:** ei-dungeon-back
**Status:** Partially documented — technical integrations identified; contractual status NOT_VERIFIABLE

---

## 1. Active Third-Party Processors

### 1.1 OpenRouter (LLM Provider)

| Field | Value | Source |
|-------|-------|--------|
| **Provider** | OpenRouter | `GenAi.cs:36` |
| **Service** | LLM API proxy (routes to OpenAI) | `GenAi.cs:36` |
| **Endpoint** | `https://openrouter.ai/api/v1` | `GenAi.cs:36` |
| **Data Sent** | Player character info, WorldInfo, play history, summaries | All play services |
| **Data NOT Sent** | Username, email, full name, password, JWT tokens | Verified in code |
| **Authentication** | API key (`keys:OpenRouterApiKey`) | `GenAi.cs:30` |
| **Purpose** | AI-generated narrative content for RPG gameplay | `NewUserPlayUseCase.cs` |
| **Contract/DPA** | NOT_VERIFIABLE | No evidence in repository |
| **Data Retention** | NOT_VERIFIABLE | External to codebase |
| **Data Location** | NOT_VERIFIABLE | Not specified in code |
| **Training Policy** | NOT_VERIFIABLE | External to codebase |

### 1.2 PostgreSQL (Database)

| Field | Value | Source |
|-------|-------|--------|
| **Provider** | PostgreSQL (hosting provider not yet defined) | `Program.cs:126` |
| **Service** | Primary data store | `EIContext.cs` |
| **Data Stored** | All application entities | All Map files |
| **Authentication** | Connection string | `Program.cs:126` |
| **Purpose** | Application data persistence | — |
| **Contract/DPA** | NOT_VERIFIABLE | Depends on hosting arrangement |
| **Data Location** | NOT_VERIFIABLE | Depends on deployment |

### 1.3 Microsoft Container Registry

| Field | Value | Source |
|-------|-------|--------|
| **Provider** | Microsoft (`mcr.microsoft.com`) | `ei-back/Dockerfile` |
| **Service** | .NET runtime and SDK Docker images | `ei-back/Dockerfile` |
| **Data** | No application data (build-time only) | — |
| **Contract/DPA** | NOT_APPLICABLE | No data transferred |

---

## 2. Unused Integrations

### 2.1 ExternalApiWebClient

- **Status:** NOT IMPLEMENTED
- **Evidence:** `Infrastructure/ExternalAPIs/Client/Core/ExternalApiWebClient.cs` exists but is not registered in DI (`AddInfraHttpClients()` is a no-op stub). No usages found.

---

## 3. Integrations NOT Found

| Category | Status |
|----------|--------|
| Email provider | NOT PRESENT |
| Analytics | NOT PRESENT |
| Payment processor | NOT PRESENT |
| Push notifications | NOT PRESENT |
| CDN | NOT PRESENT |
| External logging | NOT PRESENT |
| Social login | NOT PRESENT |
| CAPTCHA | NOT PRESENT |

---

## 4. Contractual Assessment

| Provider | Contract Evidence | DPA Evidence | Terms of Service | Data Processing Terms |
|----------|------------------|--------------|-----------------|---------------------|
| OpenRouter | NOT_VERIFIABLE | NOT_VERIFIABLE | NOT_VERIFIABLE | NOT_VERIFIABLE |
| PostgreSQL | NOT_VERIFIABLE | NOT_VERIFIABLE | NOT_VERIFIABLE | NOT_VERIFIABLE |

---

## 5. Required Actions

To complete this document, the following information is needed:

1. **OpenRouter:**
   - Is there a contract or DPA?
   - What are OpenRouter's data retention policies?
   - Where are OpenRouter's servers located?
   - Does OpenRouter use data for model training?

2. **PostgreSQL:**
   - Is the database self-hosted or managed?
   - If managed, which provider?
   - Is there a DPA with the database provider?

3. **Hosting platform:**
   - Which provider will host the application?
   - Is there a contract and DPA with that provider?
   - Where are the provider's servers located?
   - What are the provider's data handling policies?
