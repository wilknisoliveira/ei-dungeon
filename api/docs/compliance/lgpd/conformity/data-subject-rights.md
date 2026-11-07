# Data Subject Rights — Technical Conformity

**Audit Date:** 2026-07-24
**Repository:** ei-dungeon-back
**Status:** Based on code audit evidence only

---

## 1. Rights Assessment

### 1.1 Right to Access (Art. 18, II — LGPD)

**Technical mechanisms found:**

| Mechanism | Endpoint | What it returns | Limitation |
|-----------|----------|----------------|------------|
| Get user profile | `GET /api/user` (Admin only) | All users' UserName, FullName, Email | Admin-only; no self-service |
| Get game list | `GET /api/game` | User's own games | Returns game metadata, not full data |
| Get play history | `GET /api/play?gameId=` | User's own plays for a game | Returns play prompts and player info |
| Get game by ID | `GET /api/game/{gameId}` | Single game details | Owner-scoped |

**Status:** PARTIALLY_CONFORMING — Users can view their own data through game/play endpoints, but there is no dedicated "export my data" or "download my data" mechanism. Admin can view all users.

### 1.2 Right to Correction (Art. 18, III — LGPD)

**Technical mechanisms found:**

| Mechanism | Endpoint | What can be corrected | Limitation |
|-----------|----------|----------------------|------------|
| Change password | `PATCH /api/user/auth/change-password` | Password only | Cannot correct name, email, or username |

**Status:** NOT_IMPLEMENTED — No mechanism to correct username, full name, or email after registration.

### 1.3 Right to Deletion (Art. 18, VI — LGPD)

**Technical mechanisms found:**

| Mechanism | Endpoint | What can be deleted | Limitation |
|-----------|----------|-------------------|------------|
| Delete account | `DELETE /api/user/{id}` | User + cascade to games, players, plays, refresh tokens | Cannot delete specific plays |
| Delete game | `DELETE /api/game/{gameId}` | Game + cascade to players + plays | Cannot delete account or specific plays |

**Authorization logic for account deletion:**
- Admin users can delete any user by ID
- Non-admin users can only delete their own account (route `id` must match JWT `sub` claim)
- Returns 403 Forbidden if non-admin attempts to delete another user

**Status:** PARTIALLY_CONFORMING — Account deletion is implemented with full cascade. Individual play deletion is not available.

### 1.4 Right to Data Portability (Art. 18, V — LGPD)

**Technical mechanisms found:**

| Mechanism | Status |
|-----------|--------|
| Data export / download | NOT IMPLEMENTED |
| Machine-readable data format | NOT IMPLEMENTED |
| API for data retrieval | Partial (game/play endpoints return JSON) |

**Status:** NOT_IMPLEMENTED — No export mechanism exists.

### 1.5 Right to Revoke Consent (Art. 18, IX — LGPD)

**Technical mechanisms found:**

| Mechanism | Endpoint | What it does |
|-----------|----------|-------------|
| Logout (all sessions) | `POST /api/user/auth/logout` (no body) | Revokes all refresh tokens |
| Logout (single session) | `POST /api/user/auth/logout` {RefreshToken} | Revokes specific refresh token |

**Status:** PARTIALLY_CONFORMING — Logout revokes authentication tokens, but there is no mechanism to:
- Revoke consent for data processing
- Revoke consent for AI/LLM data usage
- Opt out of specific processing activities

### 1.6 Right to Information (Art. 18, I — LGPD)

**Technical mechanisms found:**

| Mechanism | Status |
|-----------|--------|
| Privacy policy | NOT_FOUND in repository |
| Privacy notice | NOT_FOUND in repository |
| Data processing disclosure | NOT_FOUND in repository |

**Status:** NOT_IMPLEMENTED — No in-app privacy information visible to users.

---

## 2. Technical Mechanisms Summary

| Right | Technical Mechanism | Status |
|-------|-------------------|--------|
| Access | Game/play listing endpoints (self-scoped) | PARTIALLY_CONFORMING |
| Correction | Password change only | NOT_IMPLEMENTED |
| Deletion | Account deletion (cascade) + game deletion (cascade) | PARTIALLY_CONFORMING |
| Portability | No export mechanism | NOT_IMPLEMENTED |
| Consent revocation | Token revocation via logout | PARTIALLY_CONFORMING |
| Information | No privacy policy or notice | NOT_IMPLEMENTED |
| Cross-border transfer info | Not applicable (no visible mechanism) | NOT_APPLICABLE |
| Process termination | Not applicable | NOT_APPLICABLE |

---

## 3. Observations

### 3.1 No Self-Service Account Management

Users cannot:
- Update their profile (name, email)
- View all their data in one place
- Export their data
- See what processing occurs on their data

Users can:
- Delete their own account (cascades to all associated data)

### 3.2 Admin-Only User Listing

The only endpoint that returns user PII across all users is `GET /api/user`, which is Admin-only. There is no self-service endpoint for a user to see their own complete profile data.

### 3.3 No Data Processing Transparency

There is no mechanism within the application to inform users about:
- What data is collected
- How it is processed
- That it is sent to an external AI provider
- What the retention periods are
- How to exercise their rights

### 3.4 Game Deletion as Partial Right Exercise

Game deletion (`DELETE /api/game/{gameId}`) cascades to delete players and plays, which effectively removes all game-related data for that game. This is a partial implementation of the right to deletion for game-specific data, but not for account data.

---

## 4. Evidence Summary

| Right | Mechanism | Evidence | Status |
|-------|-----------|----------|--------|
| Access (profile) | GET /api/user (Admin only) | `UserController.cs:103-139` | PARTIALLY_CONFORMING |
| Access (own data) | GET /api/game, GET /api/play | `GameController.cs`, `PlayController.cs` | PARTIALLY_CONFORMING |
| Correction | PATCH change-password only | `AuthController.cs` | NOT_IMPLEMENTED |
| Deletion (account) | DELETE /api/user/{id} | `UserController.cs:141-190`, `DeleteUserUseCase.cs` | PARTIALLY_CONFORMING |
| Deletion (game) | DELETE /api/game/{id} | `GameController.cs` | PARTIALLY_CONFORMING |
| Portability | None | No mechanism exists | NOT_IMPLEMENTED |
| Consent revocation | POST logout | `AuthController.cs` | PARTIALLY_CONFORMING |
| Information/notice | None | No privacy policy in repository | NOT_IMPLEMENTED |
