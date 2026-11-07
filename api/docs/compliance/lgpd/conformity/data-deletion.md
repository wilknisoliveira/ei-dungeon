# Data Deletion — Technical Conformity

**Audit Date:** 2026-07-24
**Repository:** ei-dungeon-back
**Status:** Based on code audit evidence only

---

## 1. Deletion Mechanisms Found

### 1.1 Game Deletion

- **Endpoint:** `DELETE /api/game/{gameId}`
- **Authorization:** `Admin, CommonUser, PremiumUser`
- **Use Case:** `DeleteGameUseCase.cs`
- **Behavior:**
  1. Validates game exists and belongs to authenticated user
  2. Calls `_gameRepository.Delete(gameId)` — removes Game entity from EF tracker
  3. Calls `_unitOfWork.CommitAsync()`
- **Cascade Behavior (via PostgreSQL FK constraints):**
  - Game → Players: CASCADE DELETE (`PlayerMap.cs:33`)
  - Game → Plays: CASCADE DELETE (`PlayMap.cs:22`)
  - Player → Plays: CASCADE DELETE (`PlayMap.cs:27`)
- **Result:** Deleting a game removes the game, all its players, and all associated plays
- **Evidence:** `DeleteGameUseCase.cs`, `GameController.cs:135-158`, `PlayerMap.cs:33`, `PlayMap.cs:22,27`

### 1.2 Refresh Token Revocation (Logout)

- **Endpoint:** `POST /api/user/auth/logout`
- **Authorization:** `Admin, CommonUser, PremiumUser`
- **Use Case:** `LogoutUseCase.cs`
- **Modes:**
  1. **Single session:** Deletes the specific refresh token by hash
  2. **Full logout:** Deletes ALL refresh tokens for the user
- **Evidence:** `LogoutUseCase.cs:24-41`

### 1.3 User Account Deletion

- **Endpoint:** `DELETE /api/user/{id}`
- **Authorization:** `Admin, CommonUser, PremiumUser`
- **Use Case:** `DeleteUserUseCase.cs`
- **Authorization Logic:**
  - Admin users can delete any user by ID
  - Non-admin users can only delete their own account (route `id` must match JWT `sub` claim)
  - Returns 403 Forbidden if non-admin attempts to delete another user
- **Behavior:**
  1. Validates user exists (throws `NotFoundException` if not found)
  2. Calls `_userRepository.Delete(userId)` — removes User entity from EF tracker
  3. Controller calls `_unitOfWork.CommitAsync()` — cascade removes all associated data
- **Cascade Behavior (via PostgreSQL FK constraints):**
  - User → RefreshTokens: CASCADE DELETE (`refresh_tokens.user_id`)
  - User → Games: CASCADE DELETE (`games.owner_user_id`)
  - Game → Players: CASCADE DELETE (`players.game_id`)
  - Game → Plays: CASCADE DELETE (`plays.game_id`)
  - Player → Plays: CASCADE DELETE (`plays.player_id`)
- **Result:** Deleting a user removes the user account and all associated data (games, players, plays, refresh tokens)
- **Evidence:** `DeleteUserUseCase.cs`, `IDeleteUserUseCase.cs`, `UserController.cs:141-190`, `ServiceCollectionExtensions.cs:66`

### 1.4 Refresh Token Rotation (Implicit Deletion)

- **Endpoint:** `POST /api/user/auth/refresh`
- **Use Case:** `RefreshTokenUseCase.cs`
- **Behavior:** On valid refresh, old token is deleted and new one is created
- **Evidence:** `RefreshTokenUseCase.cs:53`

---

## 2. Deletion Mechanisms NOT Found

### 2.1 Expired Refresh Token Cleanup

- **Status:** NOT IMPLEMENTED
- **Evidence:**
  - No background services, scheduled tasks, or cleanup jobs
  - Expired tokens that are never refreshed remain in the database indefinitely
  - `RefreshTokenUseCase.cs:50` rejects expired tokens but does not delete them

### 2.3 Game Info Cleanup

- **Status:** NOT IMPLEMENTED
- **Evidence:**
  - `GameInfo` entity has no FK relationships to User, Game, or Player
  - No delete endpoint for GameInfo records
  - Admin can only create, not delete

---

## 3. Cascade Delete Chain Analysis

```
User (users.id) ← DELETE /api/user/{id}
    ↓ CASCADE DELETE
RefreshToken (refresh_tokens.user_id)

User (users.id) ← DELETE /api/user/{id}
    ↓ CASCADE DELETE
Game (games.owner_user_id)
    ↓ CASCADE DELETE
Player (players.game_id)
    ↓ CASCADE DELETE
Play (plays.player_id)

Game (games.id) ← DELETE /api/game/{gameId}
    ↓ CASCADE DELETE
Player (players.game_id)
    ↓ CASCADE DELETE
Play (plays.player_id)

Game (games.id) ← DELETE /api/game/{gameId}
    ↓ CASCADE DELETE
Play (plays.game_id)
```

**Observation:** The cascade chain is complete for both User deletion and Game deletion. All associated data is removed at the database level via PostgreSQL FK constraints.

---

## 4. Third-Party Data Deletion

### 4.1 OpenRouter/OpenAI

- **Status:** NOT_VERIFIABLE
- **Evidence:** The application sends data to OpenRouter during play interactions. There is no mechanism to request deletion of data previously sent to the LLM provider. OpenRouter's retention policies are external to the codebase.
- **Note:** OpenRouter and OpenAI have their own data retention policies. The application has no control over or visibility into how long the provider retains prompts and completions.

### 4.2 Application Hosting

- **Status:** NOT_VERIFIABLE
- **Evidence:** The future deployment platform has not been selected. Data deletion requirements must be evaluated when a hosting provider is chosen.

---

## 5. Observations

### 5.1 No Soft Delete

The application uses hard deletes only. There is no `IsDeleted` flag, `DeletedAt` timestamp, or archival mechanism on any entity. Once deleted, data cannot be recovered from the application layer.

### 5.2 No Data Export / Download

There is no mechanism for users to export or download their data. The only way to view data is through the API endpoints (game list, play history).

### 5.3 User-Initiated Deletion

Users can now perform the following deletions:
1. Deleting their own account (cascades to refresh tokens, games, players, plays)
2. Deleting a game (cascades to players and plays)
3. Logging out (which revokes refresh tokens)

There is no way for a user to:
- Delete specific plays
- Delete specific players
- Request admin deletion of another user (Admin can delete anyone via the same endpoint)

---

## 6. Evidence Summary

| Deletion Mechanism | Status | Evidence |
|-------------------|--------|----------|
| User account deletion (cascade) | IMPLEMENTED | `DeleteUserUseCase.cs`, `UserController.cs:141-190` |
| Game deletion (cascade) | IMPLEMENTED | `DeleteGameUseCase.cs`, cascade FKs |
| Refresh token revocation | IMPLEMENTED | `LogoutUseCase.cs` |
| Refresh token rotation | IMPLEMENTED | `RefreshTokenUseCase.cs` |
| Expired token cleanup | NOT IMPLEMENTED | No background services |
| GameInfo cleanup | NOT IMPLEMENTED | No delete endpoint |
| Data export/download | NOT IMPLEMENTED | No mechanism |
| Third-party data deletion | NOT_VERIFIABLE | External to codebase |
