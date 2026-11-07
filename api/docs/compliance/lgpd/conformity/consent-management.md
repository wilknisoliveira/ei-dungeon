# Consent Management — Technical Conformity

**Audit Date:** 2026-07-24
**Repository:** ei-dungeon-back
**Status:** Based on code audit evidence only

---

## 1. Consent Mechanisms Found

**None.**

The repository contains no consent collection, storage, management, or revocation mechanisms at the application level.

---

## 2. What Was Searched

| Mechanism | Search Terms | Result |
|-----------|-------------|--------|
| Consent entity/model | `Consent`, `consent`, `同意` | NOT FOUND |
| Consent database table | `consent`, `DbSet<Consent>` | NOT FOUND |
| Consent checkbox/toggle | `consent`, `agree`, `accept`, `opt-in` | NOT FOUND |
| Consent API endpoint | `consent`, `agree`, `accept` | NOT FOUND |
| Terms acceptance | `terms`, `accept`, `agree` | NOT FOUND |
| Cookie consent | `cookie`, `consent`, `gdpr` | NOT FOUND |
| Privacy consent | `privacy`, `consent` | NOT FOUND |

---

## 3. Implicit Consent Points (Not True Consent)

### 3.1 User Registration

- **Endpoint:** `POST /api/user`
- **Fields collected:** UserName, FullName, Password, Email
- **Consent mechanism:** None — registration does not include a consent checkbox or agreement
- **Evidence:** `UserController.cs:57-61`, `CreateUserUseCase.cs`

### 3.2 AI Data Processing

- **Trigger:** Every `POST /api/play` interaction
- **Data sent to third party:** Game character info, play prompts, world info
- **Consent mechanism:** None — no user acknowledgment that data will be processed by an external AI provider
- **Evidence:** `NewUserPlayUseCase.cs`, `GenAi.cs`

---

## 4. Observations

### 4.1 No Granular Consent

The application does not distinguish between:
- Consent for account creation and management
- Consent for data processing
- Consent for AI/LLM data transmission
- Consent for data retention

### 4.2 No Consent Versioning

There is no mechanism to track consent versions or obtain renewed consent when terms change.

### 4.3 No Consent Records

There are no records of when, how, or what consent was obtained from users.

### 4.4 Frontend Dependency

Consent collection, if any, would need to be verified in the companion frontend application (`ei-dungeon-web`). The backend repository alone provides no evidence of consent management.

---

## 5. LGPD Relevance

Under LGPD, consent (when used as a legal basis) must be:
- Free and informed (Art. 8, I)
- Given in writing or by other means that demonstrate the data subject's intent (Art. 8, II)
- Specific to each processing purpose (Art. 8, IV)
- Revocable at any time (Art. 8, V)
- Prior to processing (Art. 7, I)

**Note:** The repository audit cannot determine whether consent is the appropriate legal basis for this application's processing. Other legal bases (e.g., contract execution under Art. 7, V) may apply. This determination requires organizational and legal information not available in the codebase.

---

## 6. Evidence Summary

| Aspect | Status | Evidence |
|--------|--------|----------|
| Consent entity/model | NOT_FOUND | No consent-related classes in codebase |
| Consent collection | NOT_FOUND | No consent checkbox or agreement mechanism |
| Consent storage | NOT_FOUND | No consent database table |
| Consent versioning | NOT_FOUND | No version tracking |
| Consent revocation | NOT_FOUND | No consent withdrawal mechanism |
| Consent records | NOT_FOUND | No audit trail of consent |
| Frontend consent | NOT_VERIFIABLE | Requires inspecting `ei-dungeon-web` repository |
