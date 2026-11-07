# Security Controls — Technical Conformity

**Audit Date:** 2026-07-24
**Repository:** ei-dungeon-back
**Status:** Based on code audit evidence only

---

## 1. Authentication

### 1.1 Password Hashing

- **Algorithm:** BCrypt (via `BCrypt.Net-Next` 4.2.0)
- **Implementation:** `EncryptionService.cs`
  - `ComputeBcryptHash(string input)` → `BCrypt.Net.BCrypt.HashPassword(input)`
  - `VerifyBcryptHash(string input, string hash)` → `BCrypt.Net.BCrypt.Verify(input, hash)`
- **New users:** Always hashed with BCrypt (`CreateUserUseCase.cs:30`)
- **Legacy migration:** SHA256 hashes auto-migrated to BCrypt on successful login (`SigninUseCase.cs:42-58`)
- **Status:** CONFORMING (BCrypt is industry standard for password hashing)

### 1.2 Password Strength Requirements

| Policy | Registration | Login | Password Change |
|--------|-------------|-------|-----------------|
| Minimum length | 8 chars | 4 chars | 4 chars |
| Maximum length | 50 chars | 50 chars | 50 chars |
| Complexity (uppercase, lowercase, digits, special) | NONE | NONE | NONE |
| Password history | NONE | NONE | NONE |

- **Evidence:** `UserDtoRequest.cs:14-15`, `LoginDtoRequest.cs:10-11`, `PasswordDtoRequest.cs:10-11`
- **Status:** PARTIALLY_CONFORMING — minimum length exists but no complexity rules

### 1.3 User Enumeration Protection

- **Registration:** Duplicate username returns generic error `"Invalid registration data."` (`CreateUserUseCase.cs:23-25`)
- **Login:** Non-existent user returns same `null` as wrong password (`SigninUseCase.cs:40`)
- **Status:** CONFORMING

---

## 2. Session Management

### 2.1 JWT Access Tokens

- **Algorithm:** HMAC-SHA256 (`TokenService.cs:29`)
- **Lifetime:** 30 minutes (configurable)
- **Validation:** Issuer, Audience, Lifetime, Signing Key all validated (`Program.cs:69-72`)
- **Storage:** Client-side only (not stored server-side)
- **Status:** CONFORMING

### 2.2 Refresh Tokens

- **Generation:** 32 random bytes via `RandomNumberGenerator.Create()` (`TokenService.cs:44-52`)
- **Storage:** SHA256 hash stored in `refresh_tokens` table; plaintext returned to client
- **Lifetime:** 7 days
- **Rotation:** Implemented — old token deleted, new token created on each refresh (`RefreshTokenUseCase.cs:53,69-76`)
- **Revocation:** Single-session or full logout (`LogoutUseCase.cs:30-40`)
- **Expiry check:** Validated on refresh (`RefreshTokenUseCase.cs:50`)
- **Status:** CONFORMING

### 2.3 Token Revocation Limitations

- **Access tokens:** NOT revocable (stateless JWT). Remain valid until expiry.
- **Refresh tokens:** Revocable via logout
- **Status:** PARTIALLY_CONFORMING — access token cannot be revoked immediately on logout

---

## 3. Authorization

### 3.1 Role-Based Access Control

| Role | Capabilities |
|------|-------------|
| Admin | Full access: list users, manage roles, create games, play, manage game info, SignalR hub |
| PremiumUser | Create games, play, view own games/plays, logout, change password |
| CommonUser | View own games/plays, logout, change password (cannot create games or play) |

- **Evidence:** `[Authorize(Roles = "...")]` attributes on all controllers
- **Status:** CONFORMING

### 3.2 Data Isolation

- **Game access:** `GetGameByIdUseCase` validates `OwnerUser.UserName == userName` (`GetGameByIdUseCase.cs`)
- **Play access:** `GetPlaysUseCase` validates game ownership before returning plays
- **User listing:** Admin-only endpoint returns all users' PII
- **Status:** CONFORMING — user data is properly scoped to authenticated user

---

## 4. Rate Limiting

| Policy | Limit | Window | Applied To | Evidence |
|--------|-------|--------|------------|----------|
| Login | 5 req | 1 min | `POST /api/user/auth/signin` | `ServiceCollectionExtensions.cs:103-112` |
| Signup | 3 req | 1 min | `POST /api/user` | `ServiceCollectionExtensions.cs:114-123` |
| Refresh | 10 req | 1 min | `POST /api/user/auth/refresh` | `ServiceCollectionExtensions.cs:125-134` |
| UsernameCheck | 20 req | 1 min | `GET /api/user/check-userinfo` | `ServiceCollectionExtensions.cs:136-145` |
| Authenticated | 120 req | 1 min | All authenticated endpoints | `ServiceCollectionExtensions.cs:147-158` |

- **Partition key:** Client IP (from `X-Forwarded-For` or `RemoteIpAddress`)
- **Algorithm:** Sliding window
- **Rejection:** HTTP 429
- **Status:** CONFORMING — but `X-Forwarded-For` trust without validation could be bypassed

---

## 5. Encryption

### 5.1 Data in Transit

- **HTTPS redirect:** Enforced via `UseHttpsRedirection()` (`Program.cs:196`)
- **HSTS:** NOT implemented — no `UseHsts()` call
- **Status:** PARTIALLY_CONFORMING

### 5.2 Data at Rest

- **Password hashes:** BCrypt (industry standard)
- **Refresh tokens:** SHA256 hash stored
- **Database encryption:** NOT verified (depends on PostgreSQL deployment configuration)
- **Status:** NOT_VERIFIABLE (database-level encryption depends on infrastructure)

---

## 6. Logging

### 6.1 What is Logged

- Usernames in plaintext (login, logout, registration, password change, game operations)
- Exception stack traces at Error level
- Application events at Information level

### 6.2 What is NOT Logged

- Passwords (plaintext or hashed)
- Refresh tokens
- JWT access tokens
- Email addresses
- Full names
- Play prompts or AI responses

### 6.3 Log Storage

- **Console:** Standard output
- **File:** `Infrastructure/Logs/logs.txt` — rolling daily, 7-day retention, 10 MB max
- **Evidence:** `Program.cs:28-34`

### 6.4 Status

- **PII in logs:** Usernames are logged in plaintext — this is a moderate concern
- **Sensitive data exclusion:** Passwords, tokens, and emails are NOT logged — this is good
- **Status:** PARTIALLY_CONFORMING

---

## 7. CORS Configuration

- **Allowed origins:** Configurable via `Cors:AllowedOrigins` (comma-separated)
- **Default:** `http://localhost:4200` (fallback)
- **Methods:** All (`AllowAnyMethod()`)
- **Headers:** Content-Type, Authorization, X-Requested-With, Accept, Origin, Cookie
- **Credentials:** Allowed
- **Preflight cache:** 1 hour
- **Status:** PARTIALLY_CONFORMING — `AllowAnyMethod()` is broader than necessary

---

## 8. SQL Injection Protection

- **ORM:** Entity Framework Core with parameterized queries
- **Raw SQL:** `FromSqlRaw` with positional parameters (safe)
- **Evidence:** `GameInfoRepository.cs:22`, all repository files use LINQ
- **Status:** CONFORMING

---

## 9. Exception Handling

- **Global handler:** `AppExceptionHandler` (IExceptionHandler)
- **Client response:** Generic `"Something went wrong"` for unhandled exceptions
- **Custom exceptions:** Typed exceptions mapped to HTTP status codes (400, 401, 403, 404, 500, 502)
- **Internal details:** Not exposed to client (good)
- **Status:** CONFORMING

---

## 10. Dependency Security

| Package | Version | Known Issues |
|---------|---------|-------------|
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 8.0.0 | Minor version mismatch with .NET 9 target |
| `Microsoft.EntityFrameworkCore` | 8.0.0 | Minor version mismatch with .NET 9 target |
| `BCrypt.Net-Next` | 4.2.0 | Current |
| All other packages | Various | No known critical vulnerabilities (as of audit date) |

- **Status:** PARTIALLY_CONFORMING — version mismatches should be addressed

---

## 11. Summary

| Control Area | Status | Key Finding |
|-------------|--------|-------------|
| Password hashing | CONFORMING | BCrypt with legacy migration |
| Password strength | PARTIALLY_CONFORMING | No complexity rules |
| JWT authentication | CONFORMING | HMAC-SHA256, full validation |
| Refresh token management | CONFORMING | Rotation, hashing, revocation |
| Role-based access | CONFORMING | Three roles, proper enforcement |
| Data isolation | CONFORMING | Game ownership validated |
| Rate limiting | CONFORMING | IP-based, 5 policies |
| HTTPS | PARTIALLY_CONFORMING | Redirect present, HSTS missing |
| SQL injection | CONFORMING | Parameterized queries throughout |
| Logging | PARTIALLY_CONFORMING | Usernames logged in plaintext |
| CORS | PARTIALLY_CONFORMING | AllowAnyMethod too broad |
| Secrets management | PARTIALLY_CONFORMING | User Secrets configured, but placeholders in config |
| Account lockout | NOT_IMPLEMENTED | Only IP-based rate limiting |
