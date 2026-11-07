# Privacy Policy / Política de Privacidade

**Audit Date:** 2026-07-24
**Repository:** ei-dungeon-back
**Status:** INCOMPLETE — requires organizational input before publication

**Language Note:** This document is a draft template. When finalized and intended for Brazilian data subjects, it must be written in Brazilian Portuguese (pt-BR).

---

## Status

This document CANNOT be published as-is. The following sections require organizational input:

- Controller identity and contact information
- DPO (Encarregado) contact information
- Organizational purposes for data processing
- Legal bases selected for each processing activity
- Official retention periods
- Contractual terms with third-party processors
- Organizational commitments

---

## Draft Structure (Based on LGPD Art. 9 and Art. 18)

### 1. Controlador / Controller Identity

**NOT_VERIFIABLE** — The repository does not contain information about the data controller.

Required information:
- Organization name
- CNPJ/CPF
- Address
- Contact email
- Contact phone

### 2. Encarregado / Data Protection Officer (DPO)

**NOT_VERIFIABLE** — The repository does not contain DPO information.

Required information:
- DPO name
- DPO email
- DPO contact information

### 3. Dados Pessoais Coletados / Personal Data Collected

Based on technical evidence:

| Dados | Finalidade Técnica | Base Legal |
|-------|-------------------|------------|
| Nome de usuário (Username) | Identificação da conta | NOT_VERIFIABLE |
| Nome completo (Full Name) | Exibição no perfil | NOT_VERIFIABLE |
| Email | Identificação, contato | NOT_VERIFIABLE |
| Senha (hash BCrypt) | Autenticação | NOT_VERIFIABLE |
| Nome do personagem | Conteúdo do jogo | NOT_VERIFIABLE |
| Descrição do personagem | Conteúdo do jogo | NOT_VERIFIABLE |
| Texto de jogada (play prompt) | Conteúdo do jogo RPG | NOT_VERIFIABLE |
| Dados do mundo gerados por IA | Conteúdo do jogo | NOT_VERIFIABLE |

### 4. Finalidades do Tratamento / Processing Purposes

**NOT_VERIFIABLE** — The following are technical purposes inferred from the code. Organizational purposes must be defined:

| Finalidade Técnica | Descrição |
|-------------------|-----------|
| Gerenciamento de contas | Criação, autenticação e perfil de usuários |
| Sessões JWT | Gerenciamento de tokens de acesso e atualização |
| Jogos RPG | Criação, execução e exclusão de sessões de jogo |
| Geração de conteúdo por IA | Respostas narrativas geradas por LLM para interações de jogo |
| Análise de jogadas | Validação de ações do usuário pelo LLM |
| Resumo de jogadas | Condensação de histórico quando excede limite de tokens |
| Rate limiting | Controle de requisições por IP |
| Logs | Registro de atividades da aplicação |

### 5. Bases Legais / Legal Bases

**NOT_VERIFIABLE** — The legal basis for each processing activity has not been determined. Possible bases under LGPD Art. 7:

- Consentimento (Art. 7, I)
- Cumprimento de obrigação legal (Art. 7, II)
- Execução de contrato (Art. 7, V)
- Legítimo interesse (Art. 7, IX)

### 6. Compartilhamento de Dados / Data Sharing

Based on technical evidence:

| Destinatário | Dados Compartilhados | Finalidade Técnica |
|-------------|---------------------|-------------------|
| OpenRouter (API) | Informações do personagem, texto de jogadas, world info, resumos | Geração de conteúdo por IA |
| OpenAI (via OpenRouter) | Mesmos dados acima | Modelo de linguagem |
| Provedor de hospedagem a definir | Hospedagem da aplicação | Infraestrutura |
| PostgreSQL | Todos os dados da aplicação | Armazenamento |

**NOT_VERIFIABLE:** Contractual terms, DPAs, and data handling policies with these providers.

### 7. Transferência Internacional de Dados / International Data Transfer

**NOT_VERIFIABLE** — The servers of OpenRouter, OpenAI, and the future hosting provider may be located outside Brazil. This must be verified.

### 8. Retenção de Dados / Data Retention

| Dados | Período de Retenção Técnico | Período Oficial |
|-------|---------------------------|----------------|
| Conta de usuário | Indefinido (sem TTL) | NOT_VERIFIABLE |
| Token de acesso JWT | 30 minutos (stateless) | N/A |
| Token de atualização | 7 dias (com rotação) | NOT_VERIFIABLE |
| Jogos | Indefinido (sem TTL) | NOT_VERIFIABLE |
| Jogadas | Indefinido (sem TTL) | NOT_VERIFIABLE |
| Logs | 7 dias (rotação Serilog) | NOT_VERIFIABLE |

### 9. Direitos dos Titulares / Data Subject Rights

Based on LGPD Art. 18 and technical evidence:

| Direito | Disponível? | Mecanismo Técnico |
|---------|------------|-------------------|
| Confirmação da existência | PARCIALMENTE | Endpoints de consulta de dados |
| Acesso aos dados | PARCIALMENTE | Listagem de jogos e jogadas |
| Correção | NÃO IMPLANTADO | Apenas alteração de senha |
| Anonimização, bloqueio ou eliminação | PARCIALMENTE | Exclusão de jogo (cascata) |
| Portabilidade | NÃO IMPLANTADO | Sem mecanismo de exportação |
| Eliminação dos dados tratados com consentimento | NÃO IMPLANTADO | Sem exclusão de conta |
| Informação sobre compartilhamento | NÃO IMPLANTADO | Sem política de privacidade |
| Revogação do consentimento | PARCIALMENTE | Logout (revoga tokens) |

### 10. Segurança dos Dados / Data Security

Technical measures implemented (evidence from repository):
- BCrypt password hashing (`EncryptionService.cs`)
- SHA256 refresh token hashing (`TokenService.cs`)
- JWT with HMAC-SHA256 signing (`TokenService.cs`)
- HTTPS redirect (`Program.cs:196`)
- Role-based access control (`[Authorize(Roles)]`)
- Rate limiting (`ServiceCollectionExtensions.cs`)
- Parameterized database queries (EF Core)
- Generic error messages (`AppExceptionHandler.cs`)

### 11. Cookies

**NOT_APPLICABLE** — The application uses JWT bearer tokens, not cookies for authentication.

### 12. Menores de Idade / Children's Data

**NOT_VERIFIABLE** — The application does not appear to collect data from children, but age verification is not implemented.

---

## Missing Information Summary

To complete this privacy policy, the following organizational information is required:

1. Controller identity (name, CNPJ, address, contact)
2. DPO (Encarregado) identity and contact
3. Stated purposes for data processing
4. Selected legal bases for each processing activity
5. Official retention periods
6. Third-party processor contracts and DPAs
7. International transfer safeguards
8. Actual production configuration (vs. code repository)
9. Age restrictions or verification mechanisms
10. Organizational commitments regarding data security
