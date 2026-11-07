# Privacy by Design — Technical Conformity

**Audit Date:** 2026-07-24
**Repository:** ei-dungeon-back
**Status:** Based on code audit evidence only

---

## 1. Data Minimization

### 1.1 What IS Minimized

| Aspect | Evidence | Status |
|--------|----------|--------|
| Password handling | Never returned in API responses; BCrypt hashed | CONFORMING |
| Refresh tokens | SHA256 hash stored; plaintext returned only once | CONFORMING |
| Play analysis results | Not persisted (in-memory only) | CONFORMING |
| Account PII excluded from LLM | Username, email, full name not sent to OpenRouter | CONFORMING |
| JWT tokens | Not stored server-side | CONFORMING |

### 1.2 What is NOT Minimized

| Aspect | Evidence | Status |
|--------|----------|--------|
| Play prompts stored indefinitely | All user text persists in `plays.prompt` | NON_CONFORMING |
| Full play history sent to LLM | All accumulated plays sent on each interaction | NON_CONFORMING |
| Usernames logged in plaintext | Controllers log usernames to Serilog | PARTIALLY_CONFORMING |
| No data retention limits | No TTL on any entity except tokens | NON_CONFORMING |

---

## 2. Pseudonymization / Anonymization

### 2.1 What IS Pseudonymized

| Aspect | Evidence | Status |
|--------|----------|--------|
| Refresh tokens | Stored as SHA256 hash, not plaintext | CONFORMING |
| User IDs | UUIDs (not sequential integers) | CONFORMING |
| Passwords | BCrypt hash with salt | CONFORMING |

### 2.2 What is NOT Pseudonymized

| Aspect | Evidence | Status |
|--------|----------|--------|
| Play prompts sent to LLM | Sent as-is with character names | NON_CONFORMING |
| No anonymization before LLM transmission | Free text passed directly | NON_CONFORMING |

---

## 3. User Data Isolation

### 3.1 What IS Isolated

| Mechanism | Evidence | Status |
|-----------|----------|--------|
| Game ownership validation | `GetGameByIdUseCase.cs` checks `OwnerUser.UserName == userName` | CONFORMING |
| Play access scoped to game owner | `GetPlaysUseCase.cs` validates ownership | CONFORMING |
| Refresh tokens scoped to user | `RefreshTokenRepository.FindAllByUserId` | CONFORMING |
| Role-based access control | `[Authorize(Roles)]` on all endpoints | CONFORMING |

### 3.2 What is NOT Isolated

| Mechanism | Evidence | Status |
|-----------|----------|--------|
| User listing exposes all PII | `GET /api/user` returns all users to Admin | PARTIALLY_CONFORMING |
| No field-level access control | Admin can see all user data | NOT_APPLICABLE (Admin access is expected) |

---

## 4. Default Privacy Settings

| Setting | Default | Evidence | Status |
|---------|---------|----------|--------|
| User role on registration | CommonUser (least privilege) | `CreateUserUseCase.cs` | CONFORMING |
| Game WorldInfo | Empty string (no data) | `Game.cs` default | CONFORMING |
| Player ability scores | 8 (minimum) | `Player.cs` defaults | CONFORMING |
| CORS origins | localhost only (dev safe) | `Program.cs:108` | CONFORMING |

---

## 5. Security by Default

| Mechanism | Evidence | Status |
|-----------|----------|--------|
| BCrypt password hashing | `EncryptionService.cs` | CONFORMING |
| Refresh token hashing | `SigninUseCase.cs:75` | CONFORMING |
| JWT full validation | `Program.cs:69-72` | CONFORMING |
| HTTPS redirect | `Program.cs:196` | CONFORMING |
| Rate limiting on auth endpoints | `ServiceCollectionExtensions.cs` | CONFORMING |
| Generic error messages | `AppExceptionHandler.cs:27` | CONFORMING |
| User enumeration protection | `CreateUserUseCase.cs:23-25` | CONFORMING |

---

## 6. Transparency Controls

| Mechanism | Evidence | Status |
|-----------|----------|--------|
| Privacy policy | NOT FOUND | NOT_IMPLEMENTED |
| Data processing disclosure | NOT FOUND | NOT_IMPLEMENTED |
| AI usage disclosure | NOT FOUND | NOT_IMPLEMENTED |

---

## 7. Observations

### 7.1 Strong Foundation

The application has a solid foundation for privacy by design:
- Proper password hashing (BCrypt)
- Token rotation and revocation
- User data isolation
- Role-based access control
- Rate limiting
- No unnecessary data collection

### 7.2 Key Gaps

The primary gaps are:
1. **No data retention limits** — data persists indefinitely
2. **No PII filtering** before LLM transmission
3. **No transparency mechanisms** — no privacy policy or data processing disclosure
4. **No consent management** — no granular consent for processing activities

---

## 8. Evidence Summary

| Principle | Status | Key Evidence |
|-----------|--------|-------------|
| Data minimization | PARTIALLY_CONFORMING | Good for passwords/tokens; poor for retention |
| Pseudonymization | PARTIALLY_CONFORMING | UUIDs and hashes; no anonymization before LLM |
| User data isolation | CONFORMING | Ownership validation on all endpoints |
| Default privacy | CONFORMING | Least-privilege defaults |
| Security by default | CONFORMING | BCrypt, JWT validation, rate limiting |
| Transparency | NOT_IMPLEMENTED | No privacy policy or disclosure |
