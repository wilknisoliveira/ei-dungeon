# Data Retention — Technical Conformity

**Audit Date:** 2026-07-24
**Repository:** ei-dungeon-back
**Status:** Based on code audit evidence only

---

## 1. Retention Mechanisms Found

### 1.1 JWT Access Token TTL

- **Mechanism:** Stateless expiration embedded in JWT token
- **Lifetime:** 30 minutes (configurable via `TokenConfigurations:Minutes`)
- **Enforcement:** `ValidateLifetime = true` in `JwtBearerOptions` (`Program.cs:71`)
- **Cleanup:** N/A — tokens expire and are rejected; not stored server-side
- **Evidence:** `Program.cs:65-91`, `appsettings.json:20`

### 1.2 Refresh Token TTL

- **Mechanism:** `ExpiresAt` timestamp stored in `refresh_tokens` table
- **Lifetime:** 7 days (configurable via `TokenConfigurations:DaysToExpiry`)
- **Enforcement:** Checked in `RefreshTokenUseCase.cs:50`
- **Cleanup:** Expired tokens are NOT proactively deleted. They accumulate indefinitely.
- **Evidence:** `RefreshTokenUseCase.cs:50`, `RefreshToken.cs`, `appsettings.json:21`

### 1.3 Log File Retention

- **Mechanism:** Serilog rolling file sink
- **Lifetime:** 7 days retained, 10 MB max size per file
- **Enforcement:** Configured in `Program.cs:29-34`
- **Cleanup:** Automatic file rolling by Serilog
- **Evidence:** `Program.cs:29-34`

---

## 2. Retention Mechanisms NOT Found

| Data Category | Expected Mechanism | Status | Evidence |
|---------------|-------------------|--------|----------|
| User accounts | TTL, soft-delete, or archival | **NONE** | No `IsDeleted`, `DeletedAt`, or TTL columns on `users` table |
| Games | TTL, soft-delete, or archival | **NONE** | Games persist until manually deleted |
| Players | TTL, soft-delete, or archival | **NONE** | Players persist until game is deleted |
| Plays | TTL, soft-delete, or archival | **NONE** | All play history persists indefinitely |
| GameInfo records | TTL or cleanup | **NONE** | Reference data persists indefinitely |
| Expired refresh tokens | Cleanup job or TTL | **NONE** | No background services or scheduled tasks |
| Summary plays | TTL or archival | **NONE** | LLM-generated summaries persist indefinitely |
| WorldInfo | TTL or archival | **NONE** | LLM-generated world data persists in `games.world_info` |

---

## 3. Background Services

**No background services exist in the codebase.**

- Searched for: `BackgroundService`, `IHostedService`, `Timer`, `cron`, `schedule`, `Hangfire`, `Quartz`
- Result: Zero matches
- The only background operation is the `Task.Run` call in `NewUserPlayUseCase.cs:236-243` for summary generation, which is a one-shot fire-and-forget task, not a recurring cleanup job.

---

## 4. Token Accumulation Problem

Expired refresh tokens accumulate in the `refresh_tokens` table because:

1. When a refresh token is used, it is deleted (rotation) — `RefreshTokenUseCase.cs:53`
2. When a refresh token expires without being used, it is rejected but NOT deleted — `RefreshTokenUseCase.cs:50`
3. When a user logs out, their tokens are deleted — `LogoutUseCase.cs:30-40`
4. No cleanup job removes expired tokens that were never used again

**Result:** Any session that expires without a refresh or logout leaves an orphaned record.

---

## 5. Observations

### 5.1 No Data Lifecycle Management

The application has no concept of data lifecycle beyond token expiration. Once data is written to PostgreSQL, it persists until explicitly deleted through an application operation or direct database manipulation.

### 5.2 No Automatic Cleanup

There are no automatic mechanisms to clean up:
- Expired refresh tokens
- Orphaned game data
- Old play history
- Stale log files beyond Serilog's built-in retention

### 5.3 Summary Generation as Implicit Archival

The `GeneratePlaysSummaryService` creates summary plays when the total token count exceeds 14,000. While this compresses context, it does not delete the original plays — they persist alongside the summary.

---

## 6. Evidence Summary

| Retention Mechanism | Status | Evidence |
|--------------------|--------|----------|
| JWT access token TTL (30 min) | IMPLEMENTED | `Program.cs:71`, `appsettings.json:20` |
| Refresh token TTL (7 days) | PARTIALLY IMPLEMENTED — no proactive cleanup | `RefreshTokenUseCase.cs:50`, `appsettings.json:21` |
| Log file rotation (7 days, 10 MB) | IMPLEMENTED | `Program.cs:29-34` |
| User account TTL | NOT IMPLEMENTED | No TTL columns, no scheduled tasks |
| Game/Player/Play TTL | NOT IMPLEMENTED | No TTL columns, no scheduled tasks |
| Expired token cleanup | NOT IMPLEMENTED | No background services |
