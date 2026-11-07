# Incident Response Procedure

**Version:** 1.0
**Last Updated:** 2026-07-24
**Status:** TEMPLATE — fill in organizational details before use
**LGPD Reference:** Art. 48 (security incident communication)

---

## 1. Technical Detection Capabilities

The following are already available in the codebase:

| Capability | Implementation | Location |
|-----------|---------------|----------|
| Application logging | Serilog (console + rolling file) | `Infrastructure/Logs/logs.txt`, 7-day retention |
| Exception logging | `AppExceptionHandler` logs stack traces at Error level | `Infrastructure/Exceptions/AppExceptionHandler.cs:30` |
| Health monitoring | PostgreSQL health check | `GET /health` |
| Rate limiting | IP-based sliding window (5 policies) | `Infrastructure/Extensions/ServiceCollectionExtensions.cs:97-160` |
| HTTPS enforcement | `UseHttpsRedirection()` | `Program.cs:196` |
| CORS | Configurable allowed origins | `Program.cs:103-113` |

---

## 2. Incident Classification

### 2.1 Severity Levels

| Level | Name | Description | Response Time | Examples |
|-------|------|-------------|---------------|----------|
| **SEV-1** | Critical | Active data breach, unauthorized access to personal data, service completely down | Immediate (< 1 hour) | Database compromise, JWT secret leaked, mass data exfiltration |
| **SEV-2** | High | Confirmed security incident with potential data exposure | < 4 hours | Unauthorized admin access, API key compromised, targeted attack detected |
| **SEV-3** | Medium | Suspicious activity, potential vulnerability exploited | < 24 hours | Unusual rate limit triggers, repeated failed logins from new IPs, anomalous error patterns |
| **SEV-4** | Low | Minor security event, no confirmed data exposure | < 72 hours | Single failed login attempt, minor config drift, non-critical service degradation |

### 2.2 Incident Categories

| Category | Description | LGPD Relevance |
|----------|-------------|----------------|
| **Data Breach** | Unauthorized access to, or disclosure of, personal data | Art. 48 — ANPD and data subject notification required |
| **Unauthorized Access** | Account compromise, privilege escalation, bypass of auth controls | Art. 48 — assess if personal data was exposed |
| **Service Disruption** | DDoS, infrastructure failure, database outage | Art. 48 — assess if availability of personal data is affected |
| **Data Integrity** | Unauthorized modification of personal data | Art. 48 — assess scope of modified data |
| **Third-Party Incident** | Security event at OpenRouter or the PostgreSQL/application hosting provider | Art. 48 — assess if user data was affected |
| **Internal Misconfiguration** | Secrets exposed, CORS too permissive, logging PII | Assess exposure; may require notification |

---

## 3. Roles and Responsibilities

**FILL IN before use:**

| Role | Name | Contact | Responsibility |
|------|------|---------|---------------|
| Incident Commander | _[fill in]_ | _[fill in]_ | Overall coordination, decisions |
| Technical Lead | _[fill in]_ | _[fill in]_ | Technical investigation, containment |
| DPO (Encarregado) | _[fill in]_ | _[fill in]_ | LGPD compliance, ANPD notification |
| Communications | _[fill in]_ | _[fill in]_ | Data subject notification, external comms |
| Legal | _[fill in]_ | _[fill in]_ | Legal assessment, regulatory filing |

---

## 4. Response Procedure

### Phase 1: Detection and Triage

```
1. Incident detected (alert, user report, log analysis, health check failure)
2. Initial assessment:
   a. What happened?
   b. When did it happen?
   c. What systems are affected?
   d. What data may be involved?
   e. Is the incident ongoing?
3. Classify severity (SEV-1 through SEV-4)
4. Notify Incident Commander
5. Create incident record (use Section 8 template)
6. Begin containment if ongoing
```

### Phase 2: Containment

```
Short-term containment:
  - Isolate affected systems
  - Block malicious IPs (update CORS, rate limiting, or firewall)
  - Revoke compromised credentials (JWT secret, API keys, user sessions)
  - Force logout all sessions: DELETE refresh tokens via database
  - If API key compromised: rotate keys immediately

Long-term containment:
  - Deploy emergency fixes
  - Enable additional logging
  - Set up monitoring on affected areas
  - Preserve evidence (logs, database snapshots)
```

**Application-specific containment actions:**

| Scenario | Action |
|----------|--------|
| JWT secret compromised | Generate new secret, invalidate all existing tokens (delete all refresh tokens), redeploy |
| OpenRouter API key compromised | Rotate key in OpenRouter dashboard, update User Secrets/env vars, redeploy |
| Database credentials exposed | Rotate PostgreSQL password, update connection string, redeploy |
| Admin account compromised | Reset admin password, revoke all refresh tokens, review role assignments |
| User data exposed via API | Identify affected users, assess scope, patch vulnerability |
| Rate limiting bypassed | Identify spoofed IPs, update rate limiting partition key logic |

### Phase 3: Eradication

```
1. Identify root cause
2. Remove threat/malware/unauthorized access
3. Patch vulnerability
4. Verify clean state
5. Update dependencies if vulnerable package identified
```

### Phase 4: Recovery

```
1. Restore services from clean backups if needed
2. Verify system integrity
3. Confirm all services operational
4. Monitor for recurrence
5. Resume normal operations
```

### Phase 5: Post-Incident

```
1. Complete incident record (Section 8)
2. Conduct post-incident review
3. Document lessons learned
4. Implement preventive measures
5. Update this procedure if gaps were identified
6. File regulatory notifications if required (see Section 6)
```

---

## 5. LGPD Notification Requirements

### 5.1 ANPD Notification (Art. 48 §1)

**Required when:** A security incident may create risk or relevant harm to data subjects.

**Timeline:** Without undue delay (prazo razoável).

**Contents (Art. 48 §1):**
1. Description of the nature of affected personal data
2. Information about the data subjects involved
3. Description of the technical and security measures adopted
4. Risks related to the incident
5. Reasons for delay, if applicable
6. Measures taken or proposed to mitigate the effects

**Template:**

```
To: Autoridade Nacional de Proteção de Dados (ANPD)
Date: [date]
From: [controller name]
Re: Security Incident Notification — [incident ID]

1. NATURE OF INCIDENT:
   [Description of what happened]

2. AFFECTED DATA:
   [Types of personal data involved]
   [Approximate number of data subjects affected]

3. DATA SUBJECTS:
   [Categories of individuals affected]

4. SECURITY MEASURES:
   [Technical and organizational measures in place]
   [Measures taken in response]

5. RISKS:
   [Assessment of risk to data subjects]

6. MITIGATION:
   [Measures taken or proposed to reduce impact]

7. CONTACT:
   [DPO name and contact information]
```

### 5.2 Data Subject Notification (Art. 48 §1)

**Required when:** The incident may cause risk or relevant harm to data subjects.

**Timeline:** Without undue delay.

**Contents:**
1. Clear description of the incident
2. Types of data involved
3. Measures taken by the controller
4. Recommendations for the data subject
5. Contact information for questions

**Template (Portuguese — for Brazilian data subjects):**

```
Assunto: Notificação de Incidente de Segurança

Prezado(a) [nome do titular],

Informamos que ocorreu um incidente de segurança que pode ter afetado
seus dados pessoais. Abaixo, fornecemos os detalhes:

1. O QUE ACONTECEU:
   [Descrição clara do incidente]

2. DADOS AFETADOS:
   [Tipos de dados pessoais envolvidos]

3. MEDIDAS ADOTADAS:
   [Medidas tomadas para conter e resolver o incidente]

4. RECOMENDAÇÕES:
   [Ações que o titular pode tomar para se proteger]

5. CONTATO:
   [Email/telefone do Encarregado para dúvidas]

Lamentamos o inconveniente e reforçamos nosso compromisso com a
proteção dos seus dados pessoais.

Atenciosamente,
[nome do controlador]
```

### 5.3 When Notification is NOT Required

Per ANPD guidance, notification may not be required when:
- The incident does not create risk or relevant harm to data subjects
- Data was encrypted and the encryption key was not compromised
- The incident was quickly contained with no data exposure
- The data involved is not personal data

**Always document the decision** even when notification is not made.

---

## 6. Communication Channels

**FILL IN before use:**

| Channel | Purpose | Contact |
|---------|---------|---------|
| Internal escalation | Slack/Teams/Phone | _[fill in]_ |
| ANPD notification | Official submission | _[fill in email/form]_ |
| Data subject notification | Email / in-app notification | _[fill in]_ |
| Law enforcement | If criminal activity suspected | _[fill in]_ |
| Third-party providers | OpenRouter and hosting-provider support | _[fill in]_ |

---

## 7. Application-Specific Incident Playbooks

### 7.1 JWT Secret Compromised

```
SEVERITY: SEV-1 (Critical)
IMPACT: All user sessions can be forged

RESPONSE:
1. Generate new JWT secret (min 64 chars, cryptographically random)
2. Store in User Secrets / env vars
3. Delete ALL refresh tokens: TRUNCATE TABLE refresh_tokens;
4. Redeploy application with new secret
5. All users will be forced to re-login (access tokens invalid, refresh tokens deleted)
6. Review: how was the secret exposed?
7. Notify affected users if data may have been accessed
```

### 7.2 OpenRouter API Key Compromised

```
SEVERITY: SEV-2 (High)
IMPACT: Unauthorized LLM usage, potential billing impact, data sent to LLM

RESPONSE:
1. Revoke key in OpenRouter dashboard immediately
2. Generate new API key
3. Update key in User Secrets / env vars
4. Redeploy application
5. Review OpenRouter logs for unauthorized usage
6. Assess: was any personal data sent by the attacker?
7. If personal data was sent → treat as data breach (Art. 48)
```

### 7.3 Database Credentials Exposed

```
SEVERITY: SEV-1 (Critical)
IMPACT: Direct access to all personal data

RESPONSE:
1. Rotate PostgreSQL password immediately
2. Update connection string in User Secrets / env vars
3. Redeploy application
4. Review database access logs for unauthorized connections
5. Check for unauthorized data modifications
6. Assess scope of data potentially accessed
7. Notify ANPD and data subjects if personal data was accessed
```

### 7.4 User Account Compromised

```
SEVERITY: SEV-2 (High)
IMPACT: Unauthorized access to user's games and play data

RESPONSE:
1. Identify compromised account(s)
2. Reset password for affected accounts
3. Revoke all refresh tokens for affected users
4. Review account activity logs for unauthorized actions
5. Check if other accounts were accessed from the same session
6. Notify affected user(s)
```

### 7.5 Data Breach via API Vulnerability

```
SEVERITY: SEV-1 (Critical)
IMPACT: Potential mass data exposure

RESPONSE:
1. Patch vulnerability immediately
2. Deploy emergency fix
3. Review access logs to determine scope
4. Identify all affected users and data types
5. Preserve evidence (logs, database state)
6. Notify ANPD within reasonable time
7. Notify affected data subjects
8. Document full incident timeline
9. Conduct post-incident review
```

---

## 8. Incident Record Template

```
INCIDENT ID: INC-[YYYY]-[NNN]
DATE DETECTED: [YYYY-MM-DD HH:MM]
SEVERITY: [SEV-1/2/3/4]
CATEGORY: [Data Breach / Unauthorized Access / Service Disruption / etc.]
STATUS: [Open / Contained / Eradicated / Recovered / Closed]

DETECTED BY: [name/system]
INCIDENT COMMANDER: [name]

DESCRIPTION:
[What happened — factual description]

TIMELINE:
[HH:MM] — [Event]
[HH:MM] — [Event]
...

AFFECTED SYSTEMS:
- [System 1]
- [System 2]

AFFECTED DATA:
- [Data type 1]
- [Data type 2]

AFFECTED USERS:
- [Number] users affected
- [Categories of users]

CONTAINMENT ACTIONS TAKEN:
1. [Action]
2. [Action]

ERADICATION ACTIONS TAKEN:
1. [Action]
2. [Action]

RECOVERY ACTIONS TAKEN:
1. [Action]
2. [Action]

ROOT CAUSE:
[Description of root cause]

LESSONS LEARNED:
1. [Lesson]
2. [Lesson]

PREVENTIVE MEASURES:
1. [Measure]
2. [Measure]

ANPD NOTIFICATION:
- Required: [Yes/No]
- Date notified: [YYYY-MM-DD or N/A]
- Justification for not notifying: [if applicable]

DATA SUBJECT NOTIFICATION:
- Required: [Yes/No]
- Date notified: [YYYY-MM-DD or N/A]
- Method: [Email / In-app / Other]

FOLLOW-UP ACTIONS:
- [ ] [Action item 1]
- [ ] [Action item 2]

CLOSED BY: [name]
DATE CLOSED: [YYYY-MM-DD]
```

---

## 9. Evidence Preservation Checklist

During an incident, preserve:

- [ ] Application logs (`Infrastructure/Logs/logs.txt`)
- [ ] Database access logs (PostgreSQL logs)
- [ ] Health check responses (`/health`)
- [ ] Rate limiting logs (HTTP 429 responses)
- [ ] Exception logs (`AppExceptionHandler` output)
- [ ] Network logs from the hosting platform
- [ ] Configuration snapshots (before and after changes)
- [ ] Database backup (point-in-time if possible)
- [ ] Access logs from third-party providers (OpenRouter)
- [ ] Screenshots or copies of affected data
- [ ] Timeline of response actions taken

---

## 10. Procedure Maintenance

| Action | Frequency | Responsible |
|--------|-----------|-------------|
| Review and update this procedure | Every 6 months | Incident Commander |
| Test incident response (tabletop exercise) | Annually | All roles |
| Update roles and contacts | When personnel change | Communications |
| Review after each incident | After every SEV-1/SEV-2 | Incident Commander |
| Update notification templates | Annually | DPO |
