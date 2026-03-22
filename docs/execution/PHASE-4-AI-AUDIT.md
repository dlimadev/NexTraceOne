# Phase 4 — AI Audit

## Como a Auditoria Funciona

O AI Audit do NexTraceOne regista toda utilização de modelos de IA, criando um trilho de auditoria completo para compliance e governança enterprise.

### Entidade: `AIUsageEntry`
Cada entrada de auditoria captura:
- **Utilizador**: userId, userDisplayName
- **Modelo**: modelId, modelName, provider
- **Tokens**: promptTokens, completionTokens, totalTokens (calculado)
- **Resultado**: Allowed, Blocked, QuotaExceeded
- **Contexto**: clientType (Web, VsCode, VisualStudio), contextScope, correlationId
- **Política**: policyName (quando aplicável)
- **Conversação**: conversationId (quando aplicável)
- **Classificação**: isInternal/isExternal (derivado)
- **Timestamp**: data/hora UTC da utilização

### Fluxo de Registo
1. Requisição de IA é recebida
2. Policy engine avalia permissões e quotas
3. Resultado (Allowed/Blocked/QuotaExceeded) é determinado
4. `AIUsageEntry` é criado e persistido
5. Dados ficam disponíveis para consulta via API

## Filtros e Persistência

### Filtros Disponíveis
- **Por resultado**: All, Allowed, Blocked, QuotaExceeded
- **Por texto**: busca em userDisplayName e modelName
- **Paginação**: pageSize configurável (default 200)

### Persistência
- Tabela: `ai_gov_usage_entries`
- DbContext: `AiGovernanceDbContext`
- Índices: por tenantId, userId, modelId, timestamp

### Endpoint
- `GET /api/v1/ai/audit` — Lista entries com filtros

## Segurança e Compliance

### Permissões
- `ai:governance:read` — obrigatório para acesso à auditoria
- Dados são filtrados por tenant (multi-tenancy)

### Dados Sensíveis
- A auditoria **não** armazena o conteúdo dos prompts/respostas
- Armazena apenas metadados de utilização (tokens, modelo, resultado)
- CorrelationId permite rastreio sem exposição de dados

### Compliance
- Trilho de auditoria imutável (append-only)
- Suporta filtragem por período para relatórios
- Dados incluem classificação interna/externa para governança de dados

## Testes

### Backend
- `UsageEntry_Record_ShouldSetProperties`
- `UsageEntry_Record_Internal_ShouldDeriveIsExternal`
- `UsageEntry_Record_ShouldCalculateTotalTokens`
- `UsageEntry_Record_WithoutConversation_ShouldBeNull`
- Total: 4 testes unitários de entidade

### Frontend (novos — Fase 4)
- Loading state, success render com tabela, error state, empty state
- Result badges (Allowed/Blocked), token counts, client types
- Policy name display e dash para null
- Total: 8 testes
