# Records of Processing Activities (ROPA)

**Audit Date:** 2026-07-24
**Repository:** ei-dungeon-back
**Status:** Partially complete — requires organizational input

---

## 1. Processing Activity: User Account Management

| Field | Value | Source |
|-------|-------|--------|
| **Activity** | User registration, authentication, profile management | Evidence: `CreateUserUseCase.cs`, `SigninUseCase.cs` |
| **Purpose** | NOT_VERIFIABLE — requires organizational input | — |
| **Legal Basis** | NOT_VERIFIABLE — requires organizational input | LGPD Art. 7 options: contract execution (V), consent (I), legitimate interest (IX) |
| **Data Subjects** | Application users (players of the RPG game) | Evidence: `User.cs` entity |
| **Personal Data** | Username, Full Name, Email, Password (BCrypt hash), Role | Evidence: `User.cs` |
| **Collection** | User self-registration via `POST /api/user` | Evidence: `UserController.cs:57-61` |
| **Processing** | Account creation, authentication, authorization, profile retrieval | Evidence: `SigninUseCase.cs`, `GetUserUseCase.cs` |
| **Storage** | PostgreSQL `users` table | Evidence: `EIContext.cs`, `UserMap.cs` |
| **Retention** | Indefinite (no TTL or cleanup mechanism) | Evidence: No retention mechanism found |
| **Deletion** | NOT_IMPLEMENTED — no user deletion mechanism | Evidence: No `DeleteUserUseCase` |
| **Recipients** | None (internal processing only) | Evidence: No external sharing of account data |
| **International Transfer** | NOT_VERIFIABLE — depends on PostgreSQL hosting location | — |
| **Security Measures** | BCrypt password hashing, JWT authentication, role-based access control, HTTPS | Evidence: `EncryptionService.cs`, `TokenService.cs` |

---

## 2. Processing Activity: JWT Session Management

| Field | Value | Source |
|-------|-------|--------|
| **Activity** | Token generation, refresh, rotation, revocation | Evidence: `TokenService.cs`, `RefreshTokenUseCase.cs`, `LogoutUseCase.cs` |
| **Purpose** | NOT_VERIFIABLE — requires organizational input | — |
| **Legal Basis** | NOT_VERIFIABLE — requires organizational input | — |
| **Data Subjects** | Authenticated application users | — |
| **Personal Data** | Refresh token (SHA256 hash), User ID link, Expiry timestamp | Evidence: `RefreshToken.cs` |
| **Collection** | Generated during login, validated during refresh | Evidence: `SigninUseCase.cs:62-80` |
| **Processing** | Token generation, hashing, storage, validation, rotation, revocation | Evidence: `RefreshTokenUseCase.cs`, `LogoutUseCase.cs` |
| **Storage** | PostgreSQL `refresh_tokens` table | Evidence: `RefreshTokenMap.cs` |
| **Retention** | 7 days (TTL), but expired tokens not proactively cleaned | Evidence: `appsettings.json:21` |
| **Deletion** | On logout (single or all) and on refresh (rotation) | Evidence: `LogoutUseCase.cs`, `RefreshTokenUseCase.cs` |
| **Recipients** | None (internal only) | — |
| **International Transfer** | NOT_VERIFIABLE | — |
| **Security Measures** | SHA256 hashing, cryptographically secure random generation | Evidence: `TokenService.cs`, `EncryptionService.cs` |

---

## 3. Processing Activity: Game Management

| Field | Value | Source |
|-------|-------|--------|
| **Activity** | Game creation, listing, deletion | Evidence: `GameController.cs`, `CreateGameUseCase.cs`, `DeleteGameUseCase.cs` |
| **Purpose** | NOT_VERIFIABLE — requires organizational input | — |
| **Legal Basis** | NOT_VERIFIABLE — requires organizational input | — |
| **Data Subjects** | Application users (game owners) | — |
| **Personal Data** | Game name, Owner User ID (link to PII), WorldInfo (AI-generated) | Evidence: `Game.cs` |
| **Collection** | User creates game via `POST /api/game` | Evidence: `GameController.cs:47-78` |
| **Processing** | Game CRUD, ownership validation | Evidence: `CreateGameUseCase.cs`, `GetGameByIdUseCase.cs` |
| **Storage** | PostgreSQL `games` table | Evidence: `GameMap.cs` |
| **Retention** | Indefinite (no TTL) | Evidence: No retention mechanism |
| **Deletion** | CASCADE DELETE to players and plays | Evidence: `DeleteGameUseCase.cs`, `PlayerMap.cs:33`, `PlayMap.cs:22` |
| **Recipients** | None (internal only) | — |
| **International Transfer** | NOT_VERIFIABLE | — |
| **Security Measures** | Ownership validation, role-based access | Evidence: `GetGameByIdUseCase.cs` |

---

## 4. Processing Activity: RPG Play Interaction (AI-Generated Content)

| Field | Value | Source |
|-------|-------|--------|
| **Activity** | User submits play prompt → AI generates narrative response | Evidence: `PlayController.cs`, `NewUserPlayUseCase.cs` |
| **Purpose** | NOT_VERIFIABLE — requires organizational input | — |
| **Legal Basis** | NOT_VERIFIABLE — requires organizational input | — |
| **Data Subjects** | Application users (players) | — |
| **Personal Data** | Play prompt (free text, max 2000 chars), Character name, Description, Race, Skills | Evidence: `PlayDtoRequest.cs`, `Player.cs` |
| **Derived Data** | AI-generated master narrations, summaries, WorldInfo updates | Evidence: `InitialMasterPlayService.cs`, `GeneratePlaysSummaryService.cs` |
| **Collection** | User submits play via `POST /api/play` | Evidence: `PlayController.cs:90-108` |
| **Processing** | Play analysis (LLM), narrative generation (LLM), summarization (LLM), persistence | Evidence: `PlayAnalyzerService.cs`, `NewUserPlayUseCase.cs` |
| **Third-Party Transmission** | **OpenRouter API** (proxy to OpenAI GPT-3.5-turbo) — player info, world info, play history, summaries | Evidence: `GenAi.cs:30-36` |
| **Storage** | PostgreSQL `plays` table (user input + AI responses) | Evidence: `PlayMap.cs` |
| **Retention** | Indefinite (no TTL) | Evidence: No retention mechanism |
| **Deletion** | CASCADE DELETE when game is deleted | Evidence: `PlayMap.cs:22,27` |
| **Recipients** | OpenRouter (LLM provider) | Evidence: `GenAi.cs:36` |
| **International Transfer** | NOT_VERIFIABLE — OpenRouter/OpenAI server location unknown | — |
| **Security Measures** | API key authentication to OpenRouter, HTTPS | Evidence: `GenAi.cs:30-36` |

---

## 5. Processing Activity: AI Play Analysis

| Field | Value | Source |
|-------|-------|--------|
| **Activity** | LLM analyzes user play for validity | Evidence: `PlayAnalyzerService.cs` |
| **Purpose** | NOT_VERIFIABLE — requires organizational input | — |
| **Legal Basis** | NOT_VERIFIABLE — requires organizational input | — |
| **Data Subjects** | Application users (players) | — |
| **Personal Data** | Player info, world info, play history, new user play | Evidence: `PlayAnalyzerService.cs` |
| **Collection** | Triggered automatically on each play | — |
| **Processing** | Structured LLM analysis (Ok/InvalidPlay/RollDice/ClarificationNeeded/PlayerDied) | Evidence: `PlayAnalyzerService.cs` |
| **Storage** | NOT persisted (in-memory only, request-scoped) | Evidence: `PlayAnalyzerService.cs` |
| **Third-Party Transmission** | OpenRouter API | Evidence: `GenAi.cs` |
| **Retention** | Request-scoped (not persisted) | — |

---

## 6. Processing Activity: AI Summary Generation

| Field | Value | Source |
|-------|-------|--------|
| **Activity** | LLM generates summary when play history exceeds token limit | Evidence: `GeneratePlaysSummaryService.cs` |
| **Purpose** | NOT_VERIFIABLE — requires organizational input | — |
| **Legal Basis** | NOT_VERIFIABLE — requires organizational input | — |
| **Data Subjects** | Application users (players) | — |
| **Personal Data** | Player info, all play history, previous summary | Evidence: `GeneratePlaysSummaryService.cs` |
| **Collection** | Triggered when token count > 14,000 | Evidence: `NewUserPlayUseCase.cs:236-243` |
| **Processing** | LLM summarization | Evidence: `GeneratePlaysSummaryService.cs` |
| **Storage** | PostgreSQL `plays` table (PlayerType.System) | Evidence: `GeneratePlaysSummaryService.cs` |
| **Retention** | Indefinite | — |
| **Third-Party Transmission** | OpenRouter API | Evidence: `GenAi.cs` |

---

## 7. Processing Activity: Rate Limiting / Logging

| Field | Value | Source |
|-------|-------|--------|
| **Activity** | IP-based rate limiting, application logging | Evidence: `ServiceCollectionExtensions.cs`, Serilog config |
| **Purpose** | NOT_VERIFIABLE — requires organizational input | — |
| **Legal Basis** | NOT_VERIFIABLE — requires organizational input | — |
| **Data Subjects** | All API requesters | — |
| **Personal Data** | IP addresses (rate limiting), usernames (logging) | Evidence: `ServiceCollectionExtensions.cs:162-167`, controller log statements |
| **Processing** | Request counting, log writing | — |
| **Storage** | In-memory (rate limiting), local log files (logging) | Evidence: `Program.cs:28-34` |
| **Retention** | Rate limiting: per-request (ephemeral). Logs: 7-day rolling | Evidence: `Program.cs:29-34` |

---

## Missing Information

The following information is required to complete the ROPA but cannot be determined from the repository:

1. **Controller/Operator identity:** Who is the data controller? Who is the operator?
2. **Organizational purposes:** What are the stated purposes for each processing activity?
3. **Legal bases:** Which legal basis has been selected for each processing activity?
4. **DPO information:** Who is the Data Protection Officer?
5. **Contracts with processors:** Are there DPAs with OpenRouter and the future PostgreSQL or application hosting providers?
6. **Organizational retention decisions:** What are the official retention periods?
7. **Backup policies:** Are there database backups outside the repository?
8. **Incident history:** Have there been any data security incidents?
9. **International transfers:** Where are OpenRouter/OpenAI servers located?
10. **Organizational security policies:** What security policies exist beyond the code?
