# Data Subject Rights Procedure

**Audit Date:** 2026-07-24
**Repository:** ei-dungeon-back
**Status:** INCOMPLETE — technical mechanisms partially implemented; organizational procedures required

---

## 1. Available Rights (LGPD Art. 18)

### 1.1 Confirmation and Access (Art. 18, I-II)

**Technical mechanism:**
- Users can view their own games: `GET /api/game`
- Users can view play history: `GET /api/play?gameId=`
- Users can view game details: `GET /api/game/{gameId}`

**Limitation:**
- No dedicated "view my profile" endpoint (Admin-only `GET /api/user` lists all users)
- No comprehensive data export endpoint

**Organizational procedure:** NOT_DEFINED

### 1.2 Correction (Art. 18, III)

**Technical mechanism:**
- Password change: `PATCH /api/user/auth/change-password`

**Limitation:**
- No mechanism to correct username, full name, or email

**Organizational procedure:** NOT_DEFINED

### 1.3 Anonymization, Blocking, or Deletion (Art. 18, IV)

**Technical mechanism:**
- Account deletion: `DELETE /api/user/{id}` (cascades to games, players, plays, refresh tokens)
- Game deletion: `DELETE /api/game/{gameId}` (cascades to players and plays)

**Authorization:**
- Admin can delete any user; non-admin can only delete self

**Limitation:**
- No individual play deletion
- No anonymization mechanism

**Organizational procedure:** NOT_DEFINED

### 1.4 Data Portability (Art. 18, V)

**Technical mechanism:**
- None

**Organizational procedure:** NOT_DEFINED

### 1.5 Deletion of Data Processed with Consent (Art. 18, VI)

**Technical mechanism:**
- Account deletion: `DELETE /api/user/{id}` cascades to all associated data
- Game deletion: `DELETE /api/game/{gameId}` cascades to associated players and plays

**Limitation:**
- No mechanism to delete data sent to third-party AI provider

**Organizational procedure:** NOT_DEFINED

### 1.6 Information about Sharing (Art. 18, VII)

**Technical mechanism:**
- None

**Organizational procedure:** NOT_DEFINED

### 1.8 Revocation of Consent (Art. 18, IX)

**Technical mechanism:**
- Logout (single session): `POST /api/user/auth/logout` with refresh token
- Logout (all sessions): `POST /api/user/auth/logout` without body

**Limitation:**
- Only revokes authentication tokens
- No mechanism to revoke consent for specific processing activities

**Organizational procedure:** NOT_DEFINED

---

## 2. Request Channels

**NOT_DEFINED** — The repository does not specify how data subject requests should be submitted.

Possible channels (to be defined by the organization):
- Email
- In-app form
- API endpoint
- Physical mail
- Phone

---

## 3. Identity Verification

**NOT_DEFINED** — The repository does not specify how data subject identity is verified for rights requests.

Current technical capabilities:
- JWT token authentication verifies the requester is an authenticated user
- Game ownership validation ensures users can only access their own data
- No additional identity verification for rights requests

---

## 4. Response Timeline

**NOT_DEFINED** — LGPD Art. 18 §5 requires response within 15 days (simplified) or 15 days (complete statement).

---

## 5. Responsible Parties

**NOT_DEFINED** — The repository does not specify who handles data subject requests.

---

## 6. Limitations and Exceptions

Based on technical evidence:
- Access tokens cannot be revoked immediately (stateless JWT, 30-min window)
- Data sent to third-party AI providers cannot be recalled from the provider
- Admin-only endpoints expose all user data to administrators
- No mechanism to distinguish between controller and processor data

---

## 7. Technical Implementation Summary

| Right | Endpoint | Status | Gap |
|-------|----------|--------|-----|
| Access (own data) | GET /api/game, GET /api/play | PARTIAL | No profile view, no export |
| Correction | PATCH change-password | PARTIAL | Only password; no name/email |
| Deletion (account) | DELETE /api/user/{id} | IMPLEMENTED | Cascades correctly |
| Deletion (game) | DELETE /api/game/{id} | IMPLEMENTED | Cascades correctly |
| Portability | None | NOT_IMPLEMENTED | No export |
| Consent revocation | POST logout | PARTIAL | Tokens only, not processing consent |
| Information | None | NOT_IMPLEMENTED | No privacy notice |
