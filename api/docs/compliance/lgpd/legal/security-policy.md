# Security Policy

**Audit Date:** 2026-07-24
**Repository:** ei-dungeon-back
**Status:** Partially documented — technical controls identified; organizational policy required

---

## 1. Technical Security Controls (Evidence-Based)

The following controls were verified through code audit:

### 1.1 Authentication

| Control | Implementation | Evidence |
|---------|---------------|----------|
| Password hashing | BCrypt (with legacy SHA256 migration) | `EncryptionService.cs` |
| JWT signing | HMAC-SHA256 | `TokenService.cs:29` |
| Token validation | Issuer, Audience, Lifetime, Signing Key | `Program.cs:69-72` |
| Refresh token hashing | SHA256 | `SigninUseCase.cs:75` |
| Refresh token rotation | Old token deleted, new token created | `RefreshTokenUseCase.cs:53,69-76` |
| Refresh token revocation | Single-session and full logout | `LogoutUseCase.cs:30-40` |
| User enumeration protection | Generic errors on duplicate username and failed login | `CreateUserUseCase.cs:23-25`, `SigninUseCase.cs:40` |

### 1.2 Authorization

| Control | Implementation | Evidence |
|---------|---------------|----------|
| Role-based access control | Admin, CommonUser, PremiumUser roles | `[Authorize(Roles)]` on controllers |
| Data isolation | Game ownership validation | `GetGameByIdUseCase.cs` |
| Least privilege defaults | New users assigned CommonUser | `CreateUserUseCase.cs` |

### 1.3 Rate Limiting

| Policy | Limit | Window | Target |
|--------|-------|--------|--------|
| Login | 5 req | 1 min | `POST /api/user/auth/signin` |
| Signup | 3 req | 1 min | `POST /api/user` |
| Refresh | 10 req | 1 min | `POST /api/user/auth/refresh` |
| UsernameCheck | 20 req | 1 min | `GET /api/user/check-userinfo` |
| Authenticated | 120 req | 1 min | All authenticated endpoints |

- **Partition:** Client IP
- **Algorithm:** Sliding window
- **Evidence:** `ServiceCollectionExtensions.cs:97-160`

### 1.4 Encryption in Transit

| Control | Implementation | Evidence |
|---------|---------------|----------|
| HTTPS redirect | `UseHttpsRedirection()` | `Program.cs:196` |
| HSTS | NOT IMPLEMENTED | — |
| TLS for LLM calls | OpenAI SDK uses HTTPS | `GenAi.cs:36` |

### 1.5 Database Security

| Control | Implementation | Evidence |
|---------|---------------|----------|
| Parameterized queries | EF Core LINQ throughout | All repository files |
| Raw SQL parameterization | `FromSqlRaw` with positional params | `GameInfoRepository.cs:22` |
| Connection string management | Config-based, not hardcoded | `Program.cs:126` |

### 1.6 Logging

| Control | Implementation | Evidence |
|---------|---------------|----------|
| Structured logging | Serilog (console + file) | `Program.cs:28-34` |
| Log rotation | Daily, 7-day retention, 10 MB max | `Program.cs:29-34` |
| Sensitive data exclusion | Passwords, tokens, emails NOT logged | Verified in controllers |
| PII in logs | Usernames logged in plaintext | `AuthController.cs:80,140` |

### 1.7 Exception Handling

| Control | Implementation | Evidence |
|---------|---------------|----------|
| Global exception handler | `AppExceptionHandler` | `Program.cs:49` |
| Client-safe messages | Generic "Something went wrong" | `AppExceptionHandler.cs:27` |
| Internal details hidden | Stack traces logged, not returned | `AppExceptionHandler.cs:30` |

### 1.8 CORS

| Control | Implementation | Evidence |
|---------|---------------|----------|
| Configurable origins | `Cors:AllowedOrigins` config | `Program.cs:103-113` |
| Default | `http://localhost:4200` | `Program.cs:108` |
| Credentials | Allowed | `Program.cs:111` |

---

## 2. Organizational Security Policy

**NOT_CREATED** — The repository does not contain an organizational security policy document.

The following would need to be defined:

1. **Information security policy** — overall approach to data security
2. **Access control policy** — who has access to what systems
3. **Password policy** — organizational requirements beyond code-level minimums
4. **Incident response policy** — how security incidents are handled
5. **Data classification policy** — how data is categorized and protected
6. **Vendor management policy** — how third-party providers are assessed
7. **Backup and recovery policy** — database backup strategy
8. **Change management policy** — how code changes are reviewed and deployed
9. **Physical security policy** — where servers and data are stored
10. **Employee training policy** — security awareness training

---

## 3. Observations

### 3.1 Strong Technical Foundation

The application implements solid technical security controls:
- BCrypt password hashing (industry standard)
- JWT with full validation
- Refresh token rotation and revocation
- Role-based access control
- Rate limiting
- Parameterized queries
- HTTPS enforcement

### 3.2 Gaps Requiring Organizational Action

1. No organizational security policy document
2. No incident response procedure
3. No access control policy beyond RBAC
4. No vendor assessment for OpenRouter or the future hosting provider
5. No backup policy visible in repository
6. No HSTS headers
7. No account lockout mechanism
