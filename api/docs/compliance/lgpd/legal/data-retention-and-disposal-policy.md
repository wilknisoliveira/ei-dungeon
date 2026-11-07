# Data Retention and Disposal Policy

**Audit Date:** 2026-07-24
**Repository:** ei-dungeon-back
**Status:** INCOMPLETE — technical mechanisms partially identified; organizational decisions required

---

## 1. Technical Retention Mechanisms

| Data Type | Technical TTL | Enforcement | Evidence |
|-----------|--------------|-------------|----------|
| JWT access tokens | 30 minutes | Stateless (token expired, rejected) | `Program.cs:71`, `appsettings.json:20` |
| Refresh tokens | 7 days | Checked on refresh, NOT proactively cleaned | `RefreshTokenUseCase.cs:50`, `appsettings.json:21` |
| Log files | 7 days rolling | Serilog rolling file sink | `Program.cs:29-34` |

---

## 2. Data Without Retention Limits

| Data Type | Retention | Disposal Mechanism | Evidence |
|-----------|-----------|-------------------|----------|
| User accounts | Indefinite | NOT IMPLEMENTED | No TTL, no deletion endpoint |
| Games | Indefinite | Manual deletion (cascade) | `DeleteGameUseCase.cs` |
| Players | Indefinite | Cascade on game deletion | `PlayerMap.cs:33` |
| Plays | Indefinite | Cascade on game deletion | `PlayMap.cs:22,27` |
| GameInfo | Indefinite | NOT IMPLEMENTED | No delete endpoint |
| Expired refresh tokens | Indefinite | NOT IMPLEMENTED | No cleanup job |

---

## 3. Organizational Retention Periods

**NOT_DEFINED** — The following retention periods need to be determined by the organization:

| Data Category | Recommended Technical Capability | Organizational Decision Required |
|--------------|--------------------------------|--------------------------------|
| User accounts | Account deletion endpoint | What is the retention period for inactive accounts? |
| Game data | TTL or archival mechanism | How long should game data be retained after last activity? |
| Play history | TTL or cleanup mechanism | How long should play history be retained? |
| Refresh tokens | Cleanup job (see NC-TECH-002) | 7-day TTL is technical; confirm this meets requirements |
| Logs | 7-day rolling (implemented) | Is 7 days sufficient for operational needs? |
| LLM interaction logs | Not logged externally | Should LLM interactions be logged for audit? |

---

## 4. Disposal Mechanisms

### 4.1 Implemented

| Mechanism | Trigger | Scope | Evidence |
|-----------|---------|-------|----------|
| Game deletion | User request | Game + players + plays (cascade) | `DeleteGameUseCase.cs` |
| Refresh token revocation | Logout | Single session or all sessions | `LogoutUseCase.cs` |
| Refresh token rotation | Token refresh | Old token deleted | `RefreshTokenUseCase.cs:53` |
| Log rotation | Size/time threshold | Log files | `Program.cs:29-34` |

### 4.2 Not Implemented

| Mechanism | Required For | Status |
|-----------|-------------|--------|
| User account deletion | Account cleanup | NOT IMPLEMENTED |
| Expired token cleanup | Token hygiene | NOT IMPLEMENTED |
| Data archival | Long-term retention management | NOT IMPLEMENTED |
| Backup purging | Backup lifecycle | NOT VERIFIABLE |
| Third-party data deletion | OpenRouter/OpenAI data | NOT_VERIFIABLE |

---

## 5. Observations

### 5.1 No Data Lifecycle Management

The application has no concept of data lifecycle. Once data is written to PostgreSQL, it persists until:
1. A user manually deletes a game (cascading to players and plays)
2. Direct database manipulation

### 5.2 Token Cleanup Gap

Expired refresh tokens accumulate indefinitely. This is a specific retention issue that should be addressed (see Technical Non-Conformity NC-TECH-002).

### 5.3 Backup Considerations

The repository does not contain information about database backup policies. Backup retention and disposal are outside the scope of the code audit but must be addressed in the organizational retention policy.

---

## 6. Required Organizational Decisions

1. What are the official retention periods for each data category?
2. What backup strategy is in place and what are backup retention periods?
3. How will data be disposed of after the retention period?
4. Who is responsible for enforcing retention policies?
5. How will retention compliance be audited?
