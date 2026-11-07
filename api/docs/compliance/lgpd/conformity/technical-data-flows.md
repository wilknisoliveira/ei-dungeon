# Technical Data Flows — Technical Conformity

**Audit Date:** 2026-07-24
**Repository:** ei-dungeon-back
**Status:** Based on code audit evidence only

---

## 1. User Registration Flow

```
Client (Browser)
    |
    | POST /api/user { UserName, FullName, Password, Email }
    | [No auth required, Rate limit: 3/min per IP]
    ↓
UserController.Create()
    ↓
CreateUserUseCase.Handler()
    |-- Duplicate username check (generic error on conflict)
    |-- Password hashed with BCrypt.Net.BCrypt.HashPassword()
    |-- New User entity created (Role = CommonUser)
    ↓
GenericRepository<User>.Insert()
    ↓
UnitOfWork.CommitAsync()
    ↓
PostgreSQL (users table)
```

**Data stored:** UserName, FullName, Email, BCrypt-hashed Password, Role, CreatedAt, UpdatedAt
**Data returned:** UserName, FullName, Email (no password)
**Evidence:** `UserController.cs:57-61`, `CreateUserUseCase.cs:22-36`

---

## 2. Authentication Flow

```
Client
    |
    | POST /api/user/auth/signin { UserName, Password }
    | [No auth required, Rate limit: 5/min per IP]
    ↓
AuthController.Signin()
    ↓
SigninUseCase.Handler()
    |-- UserRepository.ValidateCredentials(userName, password)
    |   |-- If hash starts with "$2" → BCrypt verify
    |   |-- Else → SHA256 verify, then re-hash to BCrypt (migration)
    |-- If valid:
    |   |-- Generate JWT access token (30 min, HMAC-SHA256)
    |   |-- Generate refresh token (32 random bytes, Base64)
    |   |-- Hash refresh token with SHA256
    |   |-- Store RefreshToken entity (UserId, TokenHash, ExpiresAt=7 days)
    |   |-- Return { AccessToken, RefreshToken (plaintext), Expiration }
    ↓
PostgreSQL (refresh_tokens table)
```

**Data stored:** TokenHash (SHA256 of refresh token), UserId, ExpiresAt
**Data returned:** AccessToken (JWT), RefreshToken (plaintext), Expiration timestamp
**Evidence:** `SigninUseCase.cs:31-80`, `TokenService.cs:25-52`

---

## 3. Token Refresh Flow

```
Client
    |
    | POST /api/user/auth/refresh { AccessToken, RefreshToken }
    | [No auth required, Rate limit: 10/min per IP]
    ↓
AuthController.Refresh()
    ↓
RefreshTokenUseCase.Handler()
    |-- Parse expired access token (ValidateLifetime=false, validate signing key)
    |-- Extract username from claims
    |-- Hash provided refresh token with SHA256
    |-- Look up stored token by hash
    |-- If expired: reject (401)
    |-- If valid:
    |   |-- Delete old refresh token (rotation)
    |   |-- Generate new access token + refresh token
    |   |-- Store new RefreshToken entity
    |   |-- Return new token pair
    ↓
PostgreSQL (refresh_tokens: delete old, insert new)
```

**Evidence:** `RefreshTokenUseCase.cs:37-88`

---

## 4. Game Creation Flow

```
Client (PremiumUser/Admin only)
    |
    | POST /api/game { Name, CharacterName, CharacterDescription, Race, Skills }
    | [Bearer auth, Rate limit: 120/min]
    ↓
GameController.Create()
    ↓
CreateGameUseCase.Handler()
    |-- Verify user exists
    |-- Create Game entity (OwnerUserId = current user, WorldInfo = "")
    |-- Create Player entity (RealPlayer type, character info)
    |-- Commit to DB
    ↓
PostgreSQL (games table, players table)
```

**Data stored:** Game name, OwnerUserId, WorldInfo (initially empty), GameStatus
**Data stored:** Player name, description, race, 6 ability scores, type, gameId
**Evidence:** `GameController.cs:47-78`, `CreateGameUseCase.cs`

---

## 5. Play Creation Flow (AI Interaction — Critical for LGPD)

```
Client (PremiumUser/Admin only)
    |
    | POST /api/play { GameId, Prompt }
    | [Bearer auth, Rate limit: 120/min]
    ↓
PlayController.CreateUserPlay()
    ↓
NewUserPlayUseCase.Handler()
    |
    |-- 1. Load game + validate ownership
    |-- 2. Load play context (last summary + recent plays)
    |
    |-- IF NO PLAYS (first turn):
    |   |-- InitialMasterPlayService.Handler()
    |   |   |-- Constructs prompt:
    |   |   |   SYSTEM: [RPG master instructions] + <player> info + <world-info>
    |   |   |   USER: "Create an introduction"
    |   |   |-- Calls GenAi.StreamGetResponse() → OpenRouter API
    |   |   |-- Streams response chunks back to client
    |   |   |-- Saves response as Play (PlayerType.Master)
    |   |-- RETURNS
    |
    |-- IF PLAYS EXIST:
    |   |-- Create Play entity for user input (PlayerType.RealPlayer)
    |   |-- Save to DB immediately
    |   |
    |   |-- LLM CALL #1: PlayAnalyzerService.Handler()
    |   |   |-- Sends to LLM:
    |   |   |   <player-info> + <world-info> + <last-plays> + <user-play>
    |   |   |-- Receives structured AnalyzerDtoResponse
    |   |   |-- NOT persisted (in-memory only)
    |   |
    |   |-- LLM CALL #2: StreamGenerateMasterPlay()
    |   |   |-- Sends to LLM:
    |   |   |   SYSTEM: [master instructions] + <player-info> + <world-info> + <summary> + <analysis>
    |   |   |   USER/ASSISTANT alternating: play history
    |   |   |-- Streams response chunks back to client
    |   |   |-- Saves master response as Play (PlayerType.Master)
    |   |
    |   |-- TOKEN CHECK: If total tokens > 14000:
    |       |-- Background task: GeneratePlaysSummaryService.Handler()
    |       |   |-- Sends to LLM:
    |       |   |   ASSISTANT: previous summary
    |       |   |   SYSTEM: [summarizer instructions] + <player-info>
    |       |   |   USER: <plays> (all play history)
    |       |   |-- Saves summary as Play (PlayerType.System)
    |       |-- RETURN
```

**Data sent to OpenRouter (third party):**
- Player character info (name, description, race, skills)
- Full WorldInfo JSON (NPCs, locations, events, treasures)
- All play history (user prompts + AI narrations)
- Summary text (if generated)
- Analysis results (structured)

**Data NOT sent to OpenRouter:**
- Username, email, full name, password hash
- JWT tokens
- IP address or device info

**Evidence:** `NewUserPlayUseCase.cs`, `GenAi.cs`, `InitialMasterPlayService.cs`, `PlayAnalyzerService.cs`, `GeneratePlaysSummaryService.cs`

---

## 6. Data Storage Summary

```
PostgreSQL Database (ei_db schema)
├── users
│   ├── id (UUID, PK)
│   ├── user_name (VARCHAR, UNIQUE)
│   ├── full_name (VARCHAR)
│   ├── email (VARCHAR)
│   ├── password (VARCHAR — BCrypt hash)
│   ├── role (SMALLINT)
│   ├── created_at (TIMESTAMPTZ)
│   └── updated_at (TIMESTAMPTZ)
│
├── refresh_tokens
│   ├── id (UUID, PK)
│   ├── user_id (UUID, FK → users.id, CASCADE DELETE)
│   ├── token_hash (VARCHAR, UNIQUE — SHA256)
│   ├── expires_at (TIMESTAMPTZ)
│   ├── created_at (TIMESTAMPTZ)
│   └── updated_at (TIMESTAMPTZ)
│
├── games
│   ├── id (UUID, PK)
│   ├── owner_user_id (UUID, FK → users.id, CASCADE DELETE)
│   ├── name (VARCHAR)
│   ├── world_info (TEXT — JSON from LLM)
│   ├── game_status (SMALLINT)
│   ├── last_played_at (TIMESTAMPTZ, nullable)
│   ├── created_at (TIMESTAMPTZ)
│   └── updated_at (TIMESTAMPTZ)
│
├── players
│   ├── id (UUID, PK)
│   ├── game_id (UUID, FK → games.id, CASCADE DELETE)
│   ├── name (VARCHAR — character name)
│   ├── description (VARCHAR — character description)
│   ├── type (SMALLINT — RealPlayer/Master/System)
│   ├── race (VARCHAR — character race enum)
│   ├── strength, dexterity, intelligence, constitution, charisma, wisdom (INTEGER)
│   ├── created_at (TIMESTAMPTZ)
│   └── updated_at (TIMESTAMPTZ)
│
├── plays
│   ├── id (UUID, PK)
│   ├── game_id (UUID, FK → games.id, CASCADE DELETE)
│   ├── player_id (UUID, FK → players.id, CASCADE DELETE)
│   ├── prompt (TEXT — user input or AI response)
│   ├── created_at (TIMESTAMPTZ)
│   └── updated_at (TIMESTAMPTZ)
│
└── game_infos
    ├── id (UUID, PK)
    ├── type (SMALLINT — InfoType enum)
    ├── value (VARCHAR)
    ├── created_at (TIMESTAMPTZ)
    └── updated_at (TIMESTAMPTZ)
```

---

## 7. Third-Party Data Transmission Summary

| Destination | Data Sent | Trigger | Authentication | Evidence |
|-------------|-----------|---------|----------------|----------|
| OpenRouter API → OpenAI | Player info, WorldInfo, play history, summaries | Every play interaction | API key (`keys:OpenRouterApiKey`) | `GenAi.cs:30-36` |
| PostgreSQL | All application data | All CRUD operations | Connection string | `Program.cs:126` |
| Hosting platform to be selected | Application container | Deployment | NOT_VERIFIABLE | No provider configuration yet |

---

## 8. Data at Rest

| Location | Data | Encryption | Access Control |
|----------|------|------------|----------------|
| PostgreSQL database | All entity data | Not verified (depends on deployment) | Application-level RBAC + connection string |
| Serilog log files | Usernames, exceptions | Not encrypted | File system permissions |
| .NET User Secrets | JWT secret, API keys, connection string | Encrypted by DPAPI (Windows) or plaintext (Linux) | File system permissions |

---

## 9. Data in Transit

| Path | Protocol | TLS | Evidence |
|------|----------|-----|----------|
| Client → API | HTTPS | Enforced via `UseHttpsRedirection()` | `Program.cs:196` |
| API → OpenRouter | HTTPS | OpenAI SDK uses HTTPS by default | `GenAi.cs:36` |
| API → PostgreSQL | TCP | Not verified (depends on connection string) | `Program.cs:126` |
| SignalR WebSocket | WSS | Via HTTPS redirection | `Program.cs:191` |
