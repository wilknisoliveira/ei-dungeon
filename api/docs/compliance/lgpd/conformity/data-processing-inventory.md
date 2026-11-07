# Data Processing Inventory — Technical Conformity

**Audit Date:** 2026-07-24
**Repository:** ei-dungeon-back
**Status:** Based on code audit evidence only

---

## 1. Personal Data Categories Identified

### 1.1 User Account Data

| Data Category | Field | Entity | Table | Column | Sensitivity | Evidence |
|---------------|-------|--------|-------|--------|-------------|----------|
| Username | `UserName` | `User` | `users` | `user_name` | HIGH | `Core/Domain/Entity/User.cs` |
| Full Name | `FullName` | `User` | `users` | `full_name` | HIGH | `Core/Domain/Entity/User.cs` |
| Email | `Email` | `User` | `users` | `email` | HIGH | `Core/Domain/Entity/User.cs` |
| Password (hash) | `Password` | `User` | `users` | `password` | CRITICAL | `Core/Domain/Entity/User.cs` |
| Role | `Role` | `User` | `users` | `role` | LOW | `Core/Domain/Entity/User.cs` |

### 1.2 Authentication / Session Data

| Data Category | Field | Entity | Table | Column | Sensitivity | Evidence |
|---------------|-------|--------|-------|--------|-------------|----------|
| Refresh Token Hash | `TokenHash` | `RefreshToken` | `refresh_tokens` | `token_hash` | HIGH | `Core/Domain/Entity/RefreshToken.cs` |
| Refresh Token User Link | `UserId` | `RefreshToken` | `refresh_tokens` | `user_id` | MEDIUM | `Core/Domain/Entity/RefreshToken.cs` |
| Token Expiry | `ExpiresAt` | `RefreshToken` | `refresh_tokens` | `expires_at` | LOW | `Core/Domain/Entity/RefreshToken.cs` |

### 1.3 Game Data (User-Created Content)

| Data Category | Field | Entity | Table | Column | Sensitivity | Evidence |
|---------------|-------|--------|-------|--------|-------------|----------|
| Game Name | `Name` | `Game` | `games` | `name` | LOW | `Core/Domain/Entity/Game.cs` |
| Owner User Link | `OwnerUserId` | `Game` | `games` | `owner_user_id` | MEDIUM | `Core/Domain/Entity/Game.cs` |
| World Info | `WorldInfo` | `Game` | `games` | `world_info` | LOW | `Core/Domain/Entity/Game.cs` — AI-generated game world JSON |
| Game Status | `GameStatus` | `Game` | `games` | `game_status` | LOW | `Core/Domain/Entity/Game.cs` |

### 1.4 Player Character Data

| Data Category | Field | Entity | Table | Column | Sensitivity | Evidence |
|---------------|-------|--------|-------|--------|-------------|----------|
| Character Name | `Name` | `Player` | `players` | `name` | LOW | `Core/Domain/Entity/Player.cs` — user-created game character |
| Character Description | `Description` | `Player` | `players` | `description` | LOW | `Core/Domain/Entity/Player.cs` — free text, could contain personal info |
| Character Race | `Race` | `Player` | `players` | `race` | LOW | `Core/Domain/Entity/Player.cs` — enum value |
| Ability Scores | `Strength/Dexterity/Intelligence/Constitution/Charisma/Wisdom` | `Player` | `players` | various | LOW | `Core/Domain/Entity/Player.cs` |

### 1.5 Play / Narrative Data

| Data Category | Field | Entity | Table | Column | Sensitivity | Evidence |
|---------------|-------|--------|-------|--------|-------------|----------|
| Play Prompt | `Prompt` | `Play` | `plays` | `prompt` | VARIABLE | `Core/Domain/Entity/Play.cs` — free-form text up to 2000 chars |
| Play Owner Link | `GameId` | `Play` | `plays` | `game_id` | MEDIUM | `Core/Domain/Entity/Play.cs` — indirect link to user via Game |
| Player Link | `PlayerId` | `Play` | `plays` | `player_id` | LOW | `Core/Domain/Entity/Play.cs` — links to character |

### 1.6 Reference Data (Non-Personal)

| Data Category | Field | Entity | Table | Column | Sensitivity | Evidence |
|---------------|-------|--------|-------|--------|-------------|----------|
| Game Info Type | `Type` | `GameInfo` | `game_infos` | `type` | NONE | `Core/Domain/Entity/GameInfo.cs` — reference data |
| Game Info Value | `Value` | `GameInfo` | `game_infos` | `value` | NONE | `Core/Domain/Entity/GameInfo.cs` — reference data |

---

## 2. Personal Data Sources

| Source | Data Collected | Collection Mechanism | Evidence |
|--------|---------------|---------------------|----------|
| User Registration | Username, FullName, Email, Password | `POST /api/user` | `UserController.cs`, `CreateUserUseCase.cs` |
| User Login | Username, Password | `POST /api/user/auth/signin` | `AuthController.cs`, `SigninUseCase.cs` |
| Password Change | CurrentPassword, NewPassword | `PATCH /api/user/auth/change-password` | `AuthController.cs`, `ChangePasswordUseCase.cs` |
| Game Creation | GameName, CharacterName, CharacterDescription, Race, Skills | `POST /api/game` | `GameController.cs`, `CreateGameUseCase.cs` |
| Play Creation | Prompt (free text, max 2000 chars) | `POST /api/play` | `PlayController.cs`, `NewUserPlayUseCase.cs` |

---

## 3. Derived / Generated Data

| Derived Data | Source | Generation Method | Storage | Evidence |
|-------------|--------|-------------------|---------|----------|
| World Info JSON | LLM generation from player info + game context | `UpsertWorldInfoService.cs` (currently commented out) | `games.world_info` column | `Core/Application/Service/Play/UpsertWorldInfoService.cs` |
| Play Summaries | LLM summarization of play history | `GeneratePlaysSummaryService.cs` | `plays.prompt` column (PlayerType.System) | `Core/Application/Service/Play/GeneratePlaysSummaryService.cs` |
| Master Narrations | LLM-generated D&D narrative | `NewUserPlayUseCase.cs` | `plays.prompt` column (PlayerType.Master) | `Core/Application/UseCase/Play/NewUserPlayUseCase.cs` |
| Play Analysis | LLM structured analysis of user plays | `PlayAnalyzerService.cs` | Not persisted (in-memory only) | `Core/Application/Service/Play/PlayAnalyzerService.cs` |

---

## 4. Data Processing Activities

| Processing Activity | Description | Legal Basis (NOT VERIFIABLE) | Evidence |
|--------------------|-------------|------------------------------|----------|
| User account management | Creation, authentication, profile management | NOT_VERIFIABLE | `CreateUserUseCase.cs`, `SigninUseCase.cs`, `GetUserUseCase.cs` |
| Password storage | BCrypt hashing with legacy SHA256 migration | NOT_VERIFIABLE | `EncryptionService.cs`, `SigninUseCase.cs` |
| JWT session management | Access/refresh token generation, rotation, revocation | NOT_VERIFIABLE | `TokenService.cs`, `RefreshTokenUseCase.cs`, `LogoutUseCase.cs` |
| Game creation and management | CRUD operations for game sessions | NOT_VERIFIABLE | `GameController.cs`, `CreateGameUseCase.cs` |
| LLM narrative generation | Sending game context to OpenRouter/OpenAI for AI narration | NOT_VERIFIABLE | `GenAi.cs`, `NewUserPlayUseCase.cs`, `InitialMasterPlayService.cs` |
| Play history storage | Persisting all user inputs and AI responses | NOT_VERIFIABLE | `NewUserPlayUseCase.cs`, `PlayMap.cs` |
| Summary generation | LLM summarization when play history exceeds token limit | NOT_VERIFIABLE | `GeneratePlaysSummaryService.cs` |
| Role-based access control | Admin/CommonUser/PremiumUser roles controlling feature access | NOT_VERIFIABLE | `UserRole.cs`, `[Authorize]` attributes on controllers |
| Rate limiting | IP-based request throttling | NOT_VERIFIABLE | `ServiceCollectionExtensions.cs` |
| Logging | Usernames logged in application logs | NOT_VERIFIABLE | `AuthController.cs`, `GameController.cs`, `PlayController.cs` |

---

## 5. Observations

### 5.1 Personal Data Minimization

- **Passwords:** Stored as BCrypt hashes, never returned in API responses. `EncryptionService.cs` confirms no plaintext password logging.
- **Email availability check:** `GET /api/user/check-userinfo` exposes email availability without authentication — this is a design choice that could enable email enumeration.
- **User listing:** `GET /api/user` (Admin only) returns `UserName`, `FullName`, and `Email` for all users.

### 5.2 Free-Text Fields as Risk Surface

The `plays.prompt` field (max 2000 chars) and `players.description` field are free-text user inputs. While intended as RPG narrative content, they could theoretically contain personal information if users choose to include it. No sanitization or PII detection is applied before:
1. Storage in PostgreSQL
2. Transmission to OpenRouter/OpenAI

### 5.3 Third-Party Data Exposure

User-generated game content (character names, descriptions, play prompts, world info) is transmitted to OpenRouter (proxying to OpenAI) during every play interaction. Account-level PII (username, email, full name, password hash) is NOT sent to the LLM provider.

---

## 6. Data Categories NOT Present

The following personal data categories are NOT collected or processed by this application:

- Phone numbers
- Physical addresses
- Date of birth
- Government ID numbers
- Financial data
- Biometric data
- Location data (beyond IP for rate limiting)
- Device/browser fingerprinting
- Cookies (beyond session management)
- Children's data
- Health data
- Racial/ethnic data (character race is a fantasy game concept, not ethnic data)
