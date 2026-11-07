# AI Data Processing — Technical Conformity

**Audit Date:** 2026-07-24
**Repository:** ei-dungeon-back
**Status:** Based on code audit evidence only

---

## 1. AI System Overview

- **Provider:** OpenRouter (proxy to OpenAI)
- **Endpoint:** `https://openrouter.ai/api/v1` (hardcoded in `GenAi.cs:36`)
- **Default Model:** `gpt-3.5-turbo-0125` (configurable via `GenAISettings:AiModel`)
- **Client Library:** `Microsoft.Extensions.AI.OpenAI` 10.8.0
- **Purpose:** D&D-style RPG game master — generates narrative responses to player actions

---

## 2. Data Sent to LLM Provider

### 2.1 Per-Play Interaction (Every User Action)

| Data Element | Source | Format | Max Size | Evidence |
|-------------|--------|--------|----------|----------|
| Player character name | User-provided | String | Unbounded | `PlayDtoRequest.cs`, `InitialMasterPlayService.cs` |
| Player character description | User-provided | String | Unbounded | `Player.Description`, `CreateGameUseCase.cs` |
| Player character race | User-selected | Enum string | Fixed | `Player.Race`, `CharacterRace.cs` |
| Player ability scores | User-provided | 6 integers (8-18) | Fixed | `Player.Strength/Dexterity/etc.` |
| WorldInfo | AI-generated, user-modified | JSON string | Unbounded | `Game.WorldInfo` |
| Play prompt (user text) | User-provided | String | 2000 chars | `PlayDtoRequest.Prompt` |
| Play history | Accumulated | Alternating text blocks | Grows over time | `NewUserPlayUseCase.cs` |
| Summary text | AI-generated | String | ~4000 tokens | `GeneratePlaysSummaryService.cs` |

### 2.2 Analysis Call (Per Play)

| Data Element | Included | Evidence |
|-------------|----------|----------|
| Player info | Yes | `PlayAnalyzerService.cs` |
| WorldInfo | Yes | `PlayAnalyzerService.cs` |
| Play history (all except current) | Yes | `PlayAnalyzerService.cs` |
| Current user play | Yes | `PlayAnalyzerService.cs` |

### 2.3 Summary Generation (When Token Limit Exceeded)

| Data Element | Included | Evidence |
|-------------|----------|----------|
| Player info | Yes | `GeneratePlaysSummaryService.cs` |
| Previous summary | Yes | `GeneratePlaysSummaryService.cs` |
| ALL play history | Yes | `GeneratePlaysSummaryService.cs` |

### 2.4 Data NOT Sent to LLM

- Username, email, full name (account PII)
- Password (hashed or plaintext)
- JWT tokens
- IP address
- Device information
- User agent
- Session identifiers

---

## 3. LLM Call Sequence

```
User sends play prompt
    ↓
PlayAnalyzerService (LLM Call #1 — ~50 tokens output)
    Input: player info + world info + play history + user play
    Output: Structured AnalyzerDtoResponse (Result, Reason, Skill, DC)
    Persistence: NOT persisted (in-memory only)
    ↓
StreamGenerateMasterPlay (LLM Call #2 — 100-400 tokens output)
    Input: player info + world info + summary + analysis + play history
    Output: Streaming narrative text
    Persistence: Saved as Play (PlayerType.Master)
    ↓
IF token count > 14000:
    GeneratePlaysSummaryService (LLM Call #3 — ~4000 tokens output, background)
        Input: player info + all play history + previous summary
        Output: Condensed summary
        Persistence: Saved as Play (PlayerType.System)
```

---

## 4. Prompt Structure

All prompts are in Brazilian Portuguese. Example structure:

```xml
<system-role-instruction>
  [RPG master instructions in Portuguese]
</system-role-instruction>

<player-info>
  PlayerName: {name}
  PlayerDescription: {description}
  Race: {race}
  Skills: Strength={value}, Dexterity={value}, ...
</player-info>

<world-info>
  {Full JSON: NPCs, locations, events, treasures, factions}
</world-info>

<summary>
  {Previous summary of earlier plays}
</summary>

<analysis>
  {Result of PlayAnalyzer: Ok/InvalidPlay/RollDice/ClarificationNeeded/PlayerDied}
</analysis>

<last-plays>
  # Summary: {system text}
  ## Master Table: {AI narration}
  ## {playerName}(player): {user text}
</last-plays>

<user-play>
  # {playerName}(player): {new user text}
</user-play>
```

---

## 5. Data Persistence from AI Processing

| AI Output | Persisted As | Location | Lifetime |
|-----------|-------------|----------|----------|
| Master narration | `Play` (PlayerType.Master) | `plays.prompt` column | Indefinite |
| Summary | `Play` (PlayerType.System) | `plays.prompt` column | Indefinite |
| WorldInfo | `Game.WorldInfo` | `games.world_info` column | Indefinite |
| Analysis | NOT persisted | In-memory only | Request-scoped |

---

## 6. Token Counting

- **Package:** `Tiktoken` 2.2.0 (local, no external calls)
- **Model used for counting:** `gpt-4` encoder (approximation)
- **Purpose:** Determine when to trigger summary generation
- **Threshold:** `PlayOptions:LimitTokens` (default 14,000)
- **Evidence:** `NewUserPlayUseCase.cs:371-393`

---

## 7. Provider Data Handling Assessment

### 7.1 OpenRouter

- **Data retention:** NOT_VERIFIABLE from codebase — depends on OpenRouter's terms of service
- **Data training:** NOT_VERIFIABLE — OpenRouter's policy on using prompts for model training is external to the codebase
- **Data location:** NOT_VERIFIABLE — OpenRouter's server location is not specified in code
- **DPA/Contract:** NOT_VERIFIABLE — no evidence of Data Processing Agreement in repository

### 7.2 OpenAI (via OpenRouter)

- **Data retention:** NOT_VERIFIABLE from codebase — depends on OpenAI's API data policies
- **Data training:** NOT_VERIFIABLE — OpenAI's API data usage policies are external to the codebase
- **Data location:** NOT_VERIFIABLE — OpenAI's server location is not specified in code
- **DPA/Contract:** NOT_VERIFIABLE — no evidence of Data Processing Agreement in repository

---

## 8. Observations

### 8.1 No PII Filtering

User-generated text (play prompts up to 2000 chars) is sent directly to the LLM without any PII detection, filtering, or sanitization. While the application is designed for RPG gameplay, users could theoretically include personal information in their play prompts.

### 8.2 No Anonymization

Character names and descriptions are sent as-is. There is no pseudonymization or anonymization applied before transmission to the LLM provider.

### 8.3 No Consent for AI Processing

There is no visible consent mechanism for users to agree to their game content being processed by an external AI provider. The consent aspect would need to be verified in the frontend application.

### 8.4 Stream Processing

The `PlayController` uses `IAsyncEnumerable` for SSE streaming of LLM responses. Data flows directly from OpenRouter through the API to the client without being buffered in memory (except for the final persistence to database).

### 8.5 Debug/Development Bypass

The `Features:BypassLlmGenerator` flag in `appsettings.Development.json` allows bypassing the LLM entirely, generating Lorem Ipsum text instead. This is a development feature and should not be enabled in production.

---

## 9. Evidence Summary

| Aspect | Status | Evidence |
|--------|--------|----------|
| LLM provider identified | CONFORMING | `GenAi.cs:36`, `appsettings.json:37` |
| Data sent to LLM documented | CONFORMING | All service files analyzed |
| Account PII excluded from LLM | CONFORMING | No username/email/name in prompts |
| PII filtering on user text | NOT_IMPLEMENTED | Free-text sent as-is |
| Provider data retention verified | NOT_VERIFIABLE | External to codebase |
| Provider DPA/contract | NOT_VERIFIABLE | No evidence in repository |
| Consent for AI processing | NOT_VERIFIABLE | Frontend-dependent |
| Token counting implemented | CONFORMING | `Tiktoken` package, `NewUserPlayUseCase.cs:371-393` |
