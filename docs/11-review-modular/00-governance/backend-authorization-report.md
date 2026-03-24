# Relatório de Autorização e Segurança — NexTraceOne

> **Data:** 2025-01  
> **Versão:** 1.0  
> **Tipo:** Auditoria de autorização, permissões, rate limiting, tenant isolation  
> **Escopo:** Todo o backend

---

## 1. Resumo

| Métrica | Valor |
|---------|-------|
| Permissões únicas | 73 |
| Políticas de rate limiting | 6 |
| Mecanismos de autenticação | 3 (JWT, API Key, Cookie Session) |
| Middleware de segurança | 11 (pipeline completo) |
| Protecção CSRF | ✅ CsrfTokenValidator |
| Multi-tenancy | ✅ PostgreSQL RLS |
| Encriptação de campos | ✅ AES-256-GCM |

---

## 2. Autenticação

### 2.1 Mecanismos

| Mecanismo | Âmbito | Implementação | Estado |
|-----------|--------|--------------|--------|
| JWT Bearer | Utilizadores interactivos (frontend, IDE) | JWT handler no pipeline de autenticação | ✅ ACTIVO |
| API Key | Integrações machine-to-machine | Header/query parameter validation | ✅ ACTIVO |
| Cookie Session | Frontend SPA | CookieSessionCsrfProtection middleware | ✅ ACTIVO |

### 2.2 Pipeline de Middleware (Ordem de Execução)

```
1.  ResponseCompression
2.  HttpsRedirection
3.  CORS
4.  RateLimiter
5.  SecurityHeaders
6.  GlobalExceptionHandler
7.  CookieSessionCsrfProtection
8.  Authentication
9.  TenantResolution
10. EnvironmentResolution
11. Authorization
```

**Observações:**
- Rate limiting executa antes da autenticação — protecção contra brute force
- SecurityHeaders antes de exception handler — headers de segurança mesmo em erro
- CSRF antes de authentication — validação de token anti-forgery para cookies
- TenantResolution após authentication — necessita do token para identificar tenant

---

## 3. Mapa Completo de Permissões (73)

### 3.1 Permissões do Módulo AIKnowledge

| Permissão | Tipo | Endpoints Associados | Rate Limit |
|-----------|------|---------------------|------------|
| ai:assistant:read | Leitura | /api/ai/external/*, /api/ai/orchestration/* | ai (30/60s) |
| ai:assistant:write | Escrita | /api/ai/external/*, /api/ai/orchestration/* | ai (30/60s) |
| ai:governance:read | Leitura | /api/ai/governance/* | ai (30/60s) |
| ai:governance:write | Escrita | /api/ai/governance/* | ai (30/60s) |
| ai:ide:read | Leitura | /api/ai/ide/* | ai (30/60s) |
| ai:ide:write | Escrita | /api/ai/ide/* | ai (30/60s) |
| ai:runtime:read | Leitura | /api/ai/runtime/* | ai (30/60s) |
| ai:runtime:write | Escrita | /api/ai/runtime/* | ai (30/60s) |

### 3.2 Permissões do Módulo AuditCompliance

| Permissão | Tipo | Endpoints Associados | Rate Limit |
|-----------|------|---------------------|------------|
| audit:compliance:read | Leitura | /api/audit/compliance | Global (100/60s) |
| audit:compliance:write | Escrita | /api/audit/compliance | Global (100/60s) |
| audit:events:write | Escrita | /api/audit/events | Global (100/60s) |
| audit:reports:read | Leitura | /api/audit/reports, /api/audit/export | data-intensive (50/60s) |
| audit:trail:read | Leitura | /api/audit/events, /api/audit/trail/* | data-intensive (50/60s) |

### 3.3 Permissões do Módulo Catalog

| Permissão | Tipo | Endpoints Associados | Rate Limit |
|-----------|------|---------------------|------------|
| catalog:assets:read | Leitura | /api/catalog/services/*, /api/source-of-truth/* | Global (100/60s) |
| catalog:assets:write | Escrita | /api/catalog/services/* | Global (100/60s) |
| contracts:import | Escrita | /api/contracts/import | Global (100/60s) |
| contracts:read | Leitura | /api/contracts/* | Global (100/60s) |
| contracts:write | Escrita | /api/contracts/*, /api/contracts/studio/* | Global (100/60s) |
| developer-portal:read | Leitura | /api/portal/* | Global (100/60s) |
| developer-portal:write | Escrita | /api/portal/apis/*/try | Global (100/60s) |

### 3.4 Permissões do Módulo ChangeGovernance

| Permissão | Tipo | Endpoints Associados | Rate Limit |
|-----------|------|---------------------|------------|
| change-intelligence:read | Leitura | /api/changes/* | Global (100/60s) |
| change-intelligence:write | Escrita | /api/changes/* | Global (100/60s) |
| promotion:environments:write | Escrita | /api/promotion/environments | Global (100/60s) |
| promotion:gates:override | Escrita | /api/promotion/gates/*/override | auth-sensitive (10/60s) |
| promotion:requests:read | Leitura | /api/promotion/requests | Global (100/60s) |
| promotion:requests:write | Escrita | /api/promotion/requests | Global (100/60s) |
| rulesets:execute | Execução | /api/rulesets/*/execute | operations (40/60s) |
| rulesets:read | Leitura | /api/rulesets/* | Global (100/60s) |
| rulesets:write | Escrita | /api/rulesets/* | Global (100/60s) |
| workflow:instances:read | Leitura | /api/workflows/instances/* | Global (100/60s) |
| workflow:instances:write | Escrita | /api/workflows/instances/* | auth-sensitive (10/60s) |
| workflow:templates:write | Escrita | /api/workflows/templates/* | Global (100/60s) |

### 3.5 Permissões do Módulo Configuration

| Permissão | Tipo | Endpoints Associados | Rate Limit |
|-----------|------|---------------------|------------|
| configuration:read | Leitura | /api/configuration/* | Global (100/60s) |
| configuration:write | Escrita | /api/configuration/*, /api/configuration/reset | auth-sensitive (10/60s) |

### 3.6 Permissões do Módulo Governance

| Permissão | Tipo | Endpoints Associados | Rate Limit |
|-----------|------|---------------------|------------|
| governance:analytics:read | Leitura | /api/governance/analytics | data-intensive (50/60s) |
| governance:analytics:write | Escrita | /api/governance/analytics | Global (100/60s) |
| governance:compliance:read | Leitura | /api/governance/compliance | Global (100/60s) |
| governance:controls:read | Leitura | /api/governance/controls | Global (100/60s) |
| governance:domains:read | Leitura | /api/governance/domains/* | Global (100/60s) |
| governance:domains:write | Escrita | /api/governance/domains/* | Global (100/60s) |
| governance:evidence:read | Leitura | /api/governance/evidence | Global (100/60s) |
| governance:finops:read | Leitura | /api/governance/finops | data-intensive (50/60s) |
| governance:packs:read | Leitura | /api/governance/packs/* | Global (100/60s) |
| governance:packs:write | Escrita | /api/governance/packs/* | Global (100/60s) |
| governance:policies:read | Leitura | /api/governance/policies | Global (100/60s) |
| governance:reports:read | Leitura | /api/governance/reports, /api/governance/executive | data-intensive (50/60s) |
| governance:risk:read | Leitura | /api/governance/risk | Global (100/60s) |
| governance:teams:read | Leitura | /api/governance/teams/*, /api/governance/onboarding, /api/governance/scoped-context | Global (100/60s) |
| governance:teams:write | Escrita | /api/governance/teams/*, /api/governance/onboarding | Global (100/60s) |
| governance:waivers:read | Leitura | /api/governance/waivers | Global (100/60s) |
| governance:waivers:write | Escrita | /api/governance/waivers | Global (100/60s) |
| integrations:read | Leitura | /api/governance/integrations | Global (100/60s) |
| integrations:write | Escrita | /api/governance/integrations | Global (100/60s) |

### 3.7 Permissões do Módulo IdentityAccess

| Permissão | Tipo | Endpoints Associados | Rate Limit |
|-----------|------|---------------------|------------|
| identity:permissions:read | Leitura | /api/identity/permissions | Global (100/60s) |
| identity:roles:assign | Escrita | /api/identity/roles/assign | auth-sensitive (10/60s) |
| identity:roles:read | Leitura | /api/identity/roles | Global (100/60s) |
| identity:sessions:read | Leitura | /api/identity/sessions, /api/identity/access-review, /api/identity/runtime-context | Global (100/60s) |
| identity:sessions:revoke | Escrita | /api/identity/sessions/revoke, /api/identity/breakglass, /api/identity/session | auth-sensitive (10/60s) |
| identity:users:read | Leitura | /api/identity/users, /api/identity/environments | Global (100/60s) |
| identity:users:write | Escrita | /api/identity/users, /api/identity/delegation | Global (100/60s) |

### 3.8 Permissões do Módulo Notifications

| Permissão | Tipo | Endpoints Associados | Rate Limit |
|-----------|------|---------------------|------------|
| notifications:inbox:read | Leitura | /api/notifications/inbox | Global (100/60s) |
| notifications:inbox:write | Escrita | /api/notifications/inbox/*/read | Global (100/60s) |
| notifications:preferences:read | Leitura | /api/notifications/preferences | Global (100/60s) |
| notifications:preferences:write | Escrita | /api/notifications/preferences | Global (100/60s) |

### 3.9 Permissões do Módulo OperationalIntelligence

| Permissão | Tipo | Endpoints Associados | Rate Limit |
|-----------|------|---------------------|------------|
| operations:automation:approve | Escrita | /api/operations/automation/*/approve | auth-sensitive (10/60s) |
| operations:automation:execute | Execução | /api/operations/automation/*/execute | operations (40/60s) |
| operations:automation:read | Leitura | /api/operations/automation | operations (40/60s) |
| operations:automation:write | Escrita | /api/operations/automation | operations (40/60s) |
| operations:cost:read | Leitura | /api/operations/costs | data-intensive (50/60s) |
| operations:cost:write | Escrita | /api/operations/costs | operations (40/60s) |
| operations:incidents:read | Leitura | /api/operations/incidents/* | operations (40/60s) |
| operations:incidents:write | Escrita | /api/operations/incidents/* | operations (40/60s) |
| operations:mitigation:read | Leitura | /api/operations/mitigation | operations (40/60s) |
| operations:mitigation:write | Escrita | /api/operations/mitigation | operations (40/60s) |
| operations:reliability:read | Leitura | /api/operations/reliability | data-intensive (50/60s) |
| operations:runbooks:read | Leitura | /api/operations/runbooks | Global (100/60s) |
| operations:runtime:read | Leitura | /api/operations/runtime | operations (40/60s) |
| operations:runtime:write | Escrita | /api/operations/runtime | operations (40/60s) |

### 3.10 Permissões de Plataforma

| Permissão | Tipo | Endpoints Associados | Rate Limit |
|-----------|------|---------------------|------------|
| platform:admin:read | Leitura | /api/governance/platform-status, /api/governance/delegated-admin, /api/identity/tenants | Global (100/60s) |

---

## 4. Rate Limiting — Análise Detalhada

### 4.1 Políticas e Cobertura

| Política | Limite | Endpoints Cobertos | Justificação |
|----------|--------|-------------------|-------------|
| Global | 100/IP/60s | Todos os endpoints base | Protecção DDoS genérica |
| auth | 20/IP/60s | Login, register, refresh | Protecção contra brute force |
| auth-sensitive | 10/IP/60s | Password reset, MFA, role assign, session revoke, gate override, config reset, workflow approve, automation approve, delegated admin | Operações de alto risco |
| ai | 30/IP/60s | Todos os /api/ai/* | Controlo de custos de IA |
| data-intensive | 50/IP/60s | Reports, exports, analytics, FinOps, reliability, audit trail, cost, topology | Protecção de queries pesadas |
| operations | 40/IP/60s | Automation, incidents, mitigation, runtime, rulesets execute | Operações com side-effects |

### 4.2 Análise de Gaps no Rate Limiting

| Endpoint | Rate Limit Actual | Recomendação |
|----------|------------------|-------------|
| /api/contracts/import | Global (100) | Deveria ser data-intensive (50) — importação é pesada |
| /api/source-of-truth/search | data-intensive (50) | ✅ Correcto |
| /api/governance/delegated-admin POST | auth-sensitive (10) | ✅ Correcto |

---

## 5. Multi-Tenancy

### 5.1 Implementação

| Componente | Mecanismo | Caminho (est.) |
|------------|-----------|----------------|
| TenantResolutionMiddleware | Extrai tenant do JWT/header e define no contexto | Security building block |
| TenantRlsInterceptor | Aplica filtro RLS via PostgreSQL em cada query | Infrastructure building block |
| TenantIsolationBehavior | Valida acesso ao tenant no pipeline MediatR | Application building block |

### 5.2 Fluxo de Isolamento

```
Request → TenantResolutionMiddleware → extrai TenantId do token/header
       → TenantIsolationBehavior → valida que o handler opera no tenant correcto
       → TenantRlsInterceptor → aplica SET app.current_tenant no PostgreSQL
       → Query executa com RLS activo → dados isolados por tenant
```

### 5.3 Cobertura

| DbContext | Tenant Isolation | Mecanismo |
|-----------|-----------------|-----------|
| IdentityDb | ✅ | RLS |
| AuditDb | ✅ | RLS |
| ContractsDb | ✅ | RLS |
| CatalogGraphDb | ✅ | RLS |
| DeveloperPortalDb | ✅ | RLS |
| GovernanceDb | ✅ | RLS |
| ConfigurationDb | ✅ | RLS |
| NotificationsDb | ✅ | RLS |
| ChangeIntelDb | ✅ | RLS |
| PromotionDb | ✅ | RLS |
| RulesetGovernanceDb | ✅ | RLS |
| WorkflowDb | ✅ | RLS |
| AutomationDb | ✅ | RLS |
| CostIntelDb | ✅ | RLS |
| IncidentDb | ✅ | RLS |
| ReliabilityDb | ✅ | RLS |
| RuntimeIntelDb | ✅ | RLS |
| ExternalAiDb | ✅ | RLS |
| AiGovernanceDb | ✅ | RLS |
| AiOrchestrationDb | ✅ | RLS |

---

## 6. Environment Access

### 6.1 Resolução de Ambiente

O middleware `EnvironmentResolution` executa após `TenantResolution` e antes de `Authorization`, permitindo:

- Acesso condicionado por ambiente (dev, staging, production)
- Diferentes permissões por ambiente
- Isolamento de dados por ambiente dentro do mesmo tenant

### 6.2 Controlo de Acesso a Ambientes

| Mecanismo | Implementação |
|-----------|--------------|
| EnvironmentAccess entity | Controla que utilizadores acedem a que ambientes |
| JIT Access | Acesso temporário a ambientes sensíveis |
| BreakGlass | Acesso de emergência com auditoria reforçada |
| Expiração automática | Via BackgroundWorkers (IdentityExpirationJob) |

---

## 7. CSRF Protection

### 7.1 Implementação

| Componente | Detalhe |
|------------|---------|
| CsrfTokenValidator | Valida token anti-forgery para sessões baseadas em cookies |
| Posição no pipeline | Após SecurityHeaders, antes de Authentication |
| Âmbito | Apenas sessões cookie (não se aplica a JWT/API Key) |

### 7.2 Fluxo

```
Request com cookie → CookieSessionCsrfProtection middleware
  → Verifica X-CSRF-Token header
  → Valida contra token armazenado na sessão
  → Se inválido: 403 Forbidden
  → Se válido: prossegue pipeline
```

---

## 8. Encriptação de Campos

### 8.1 AES-256-GCM

| Componente | Implementação |
|------------|--------------|
| EncryptedStringConverter | EF Core value converter para encriptação transparente |
| Algoritmo | AES-256-GCM |
| Âmbito | Campos marcados com atributo/configuração |
| Transparência | Encriptação/desencriptação automática na persistência |

### 8.2 Campos Encriptados (estimativa)

| Tipo de Campo | Módulo |
|---------------|--------|
| API Keys | identityaccess |
| Tokens de integração | governance |
| Credenciais de provider IA | aiknowledge |
| Segredos de configuração | configuration |

---

## 9. API Key Authentication

### 9.1 Implementação

| Aspecto | Detalhe |
|---------|---------|
| Localização | Header `X-Api-Key` ou query parameter |
| Validação | Contra entidades persistidas no IdentityDb |
| Escopo | Integrações M2M, CLI, IDE extensions |
| Permissões | Associadas ao API key com escopo limitado |

---

## 10. Gaps e Riscos de Segurança

### 10.1 Gaps Identificados

| Gap | Severidade | Módulo | Detalhe |
|-----|-----------|--------|---------|
| Sem seed de produção para roles/permissions | ALTA | identityaccess | Deploy inicial sem dados base de autorização |
| /api/contracts/import sem rate limit adequado | MÉDIA | catalog | Importação pesada com limite Global (100) |
| Sem política de rotação de API keys | MÉDIA | identityaccess | Keys podem ser válidas indefinidamente |
| Sem verificação de complexidade de password | BAIXA | identityaccess | Depende de validação no handler |

### 10.2 Riscos

| Risco | Probabilidade | Impacto | Mitigação |
|-------|--------------|---------|-----------|
| Escalação de privilégios via delegation | BAIXA | ALTO | Delegation tem expiração automática |
| BreakGlass abuse | BAIXA | ALTO | Auditoria reforçada + expiração automática |
| Tenant data leak via RLS bypass | MUITO BAIXA | CRÍTICO | RLS a nível PostgreSQL, não apenas aplicação |
| CSRF em endpoints sensíveis | BAIXA | ALTO | CsrfTokenValidator activo para cookies |

---

## 11. Recomendações

### Prioridade ALTA

| # | Recomendação | Justificação |
|---|-------------|-------------|
| 1 | Criar seed de produção para roles e permissões | Deployment requer dados base de autorização |
| 2 | Rever rate limit de /api/contracts/import | Importação pesada com limite demasiado permissivo |
| 3 | Implementar rotação de API keys | Keys sem expiração são risco de segurança |

### Prioridade MÉDIA

| # | Recomendação | Justificação |
|---|-------------|-------------|
| 4 | Documentar mapa de permissões por persona | Facilita auditoria e review |
| 5 | Implementar política de complexidade de password | Reforça segurança de autenticação |
| 6 | Auditoria periódica de BreakGlass usage | Monitorizar acessos de emergência |

### Prioridade BAIXA

| # | Recomendação | Justificação |
|---|-------------|-------------|
| 7 | Adicionar telemetria a tentativas de acesso falhadas | Visibilidade sobre ataques |
| 8 | Documentar fluxo completo de autenticação | Facilita onboarding |
