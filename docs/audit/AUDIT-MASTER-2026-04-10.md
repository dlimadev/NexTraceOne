# NexTraceOne — Relatório de Auditoria Mestre
**Data:** 2026-04-10  
**Escopo:** Codebase completo — Frontend, Backend, Database, API Contracts, Segurança  
**Branch auditada:** `claude/audit-codebase-FtRWb`  
**Modo:** Analysis

---

## 1. Sumário Executivo

Esta auditoria cobriu aproximadamente **3.800 ficheiros** (3.112 backend C#, 676 frontend TypeScript/React), **27 DbContexts**, **12 módulos** e toda a camada de configuração/infra.

Foram identificados **51 problemas** distribuídos em 5 categorias:

| Categoria | Crítico | Alto | Médio | Baixo | Total |
|-----------|---------|------|-------|-------|-------|
| Segurança | 2 | 2 | 3 | 3 | 10 |
| Base de Dados | 1 | 1* | 1 | — | 3 |
| API Contracts | 1 | 1 | 1 | — | 3 |
| Backend | 3 | 3 | 8 | 6 | 20 |
| Frontend | 4 | 2 | 3 | 1 | 10 |
| **Total** | **11** | **9** | **16** | **10** | **46** |

> *O problema de TenantId.IsRequired() afeta ~30 ficheiros de configuração, contado como 1 issue sistémico.

---

## 2. Problemas Críticos — Acção Imediata

### [C-01] Colisão de Tabela no ChangeGovernance
**Área:** Database | **Ficheiros:** 2 `PromotionGateConfiguration.cs`  
Dois DbContexts diferentes (ChangeIntelligenceDbContext e PromotionDbContext) mapeiam entidades distintas para a mesma tabela `chg_promotion_gates`, causando conflitos de migração e corrupção de dados.  
→ **Ver:** [AUDIT-DATABASE-2026-04-10.md](./AUDIT-DATABASE-2026-04-10.md#c-01)

### [C-02] Chave JWT de Fallback Hardcoded no Código
**Área:** Segurança | **Ficheiro:** `NexTraceOne.BuildingBlocks.Security/DependencyInjection.cs`  
Constante `devFallbackKey` visível no código-fonte e nos binários compilados. Se executada em produção, todos os tokens JWT ficam comprometidos.  
→ **Ver:** [AUDIT-SECURITY-2026-04-10.md](./AUDIT-SECURITY-2026-04-10.md#c-02)

### [C-03] API Keys Armazenadas em Memória sem Encriptação
**Área:** Segurança | **Ficheiro:** `ApiKeyAuthenticationHandler.cs`  
Chaves de API lidas de appsettings e armazenadas em plain text em memória sem rotação ou auditoria de uso.  
→ **Ver:** [AUDIT-SECURITY-2026-04-10.md](./AUDIT-SECURITY-2026-04-10.md#c-03)

### [C-04] 6 Endpoints de Autenticação em Falta no Backend
**Área:** API Contracts | **Ficheiro:** `identity.ts` (frontend)  
`activateAccount`, `forgotPassword`, `resetPassword`, `resendMfaCode`, `getInvitationDetails`, `acceptInvitation` — o frontend chama estes endpoints que não existem no backend. Fluxos de activação de conta e recuperação de password estão completamente quebrados.  
→ **Ver:** [AUDIT-API-2026-04-10.md](./AUDIT-API-2026-04-10.md#c-04)

### [C-05] Endpoint de Export Stub — Sem Implementação Real
**Área:** Backend | **Ficheiro:** `ExportEndpointModule.cs`  
`/api/v1/export` retorna status "queued" hardcoded sem job Quartz real. Export de dados não funciona.  
→ **Ver:** [AUDIT-BACKEND-2026-04-10.md](./AUDIT-BACKEND-2026-04-10.md#c-05)

### [C-06] Heurísticas Seed-Based no OnCall Intelligence
**Área:** Backend | **Ficheiro:** `GetOnCallIntelligence.cs`  
Indicadores de fadiga calculados com `Math.Min(20m + (seed % 30), 60m)` — dados pseudo-aleatórios apresentados como métricas reais.  
→ **Ver:** [AUDIT-BACKEND-2026-04-10.md](./AUDIT-BACKEND-2026-04-10.md#c-06)

### [C-07] ~ [C-10] Campos GUID expostos na UI (4 ecrãs)
**Área:** Frontend | **Ficheiros:** `CanonicalEntityImpactCascadePage.tsx`, `ContractHealthTimelinePage.tsx`, `DependencyDashboardPage.tsx`, `LicenseCompliancePage.tsx`  
Utilizadores são obrigados a introduzir UUIDs brutos em campos de texto — anti-padrão crítico de UX.  
→ **Ver:** [AUDIT-FRONTEND-2026-04-10.md](./AUDIT-FRONTEND-2026-04-10.md#c-07)

---

## 3. Problemas de Alta Prioridade

| ID | Área | Descrição |
|----|------|-----------|
| A-01 | Database | ~30 configurações de entidade sem `TenantId.IsRequired()` — risco de bypass de RLS |
| A-02 | Segurança | Break Glass sem workflow de aprovação — qualquer utilizador autenticado pode solicitar |
| A-03 | Segurança | Operações em ambiente de produção sem autorização adicional |
| A-04 | Backend | Silent exception handling em 3 handlers (`catch { /* empty */ }`) |
| A-05 | Backend | Null result sem validação em `IncidentCorrelationService` |
| A-06 | API | DTO mismatch no endpoint de correlação de incidentes (Guid vs string) |
| A-07 | Frontend | Strings hardcoded sem i18n em 4 ficheiros de features críticas |

---

## 4. Problemas de Prioridade Média

| ID | Área | Descrição |
|----|------|-----------|
| M-01 | Database | Comentário de tabela `prm_promotion_gates` vs código `chg_promotion_gates` |
| M-02 | Backend | Thresholds de correlation score hardcoded (80, 45, 20) |
| M-03 | Backend | Thresholds temporais hardcoded (1h, 4h, 12h, 24h) |
| M-04 | Backend | Moeda padrão EUR hardcoded no benchmarking |
| M-05 | Backend | `GenerateMockServer` não reporta erro quando spec está vazia |
| M-06 | Backend | Integração de eventos em falta no módulo Configuration |
| M-07 | Backend | Validation sem `IsInEnum()` no AddBookmark |
| M-08 | Backend | Código gerado contém TODO — stubs não funcionais entregues |
| M-09 | API | Rota `/runbooks/suggest` pode colidir com `/{runbookId}` |
| M-10 | Frontend | Missing loading state no `ContractHealthTimelinePage` |
| M-11 | Frontend | Validação de formato UUID ausente antes de submit |
| M-12 | Segurança | Delegação de permissões disponível a qualquer utilizador autenticado |
| M-13 | Segurança | Sem rate limiting específico para falhas de API Key |
| M-14 | Segurança | Validação de state OIDC não documentada/verificada |

---

## 5. Módulos com Maior Número de Issues

| Módulo | Issues | Severidade Máxima |
|--------|--------|-------------------|
| `identityaccess` | 9 | Crítico (API gaps) |
| `operationalintelligence` | 7 | Crítico (heurísticas) |
| `catalog` (frontend + backend) | 7 | Crítico (GUID inputs) |
| `changegovernance` | 4 | Crítico (colisão de tabela) |
| `configuration` | 4 | Alto (export stub) |
| `governance` | 3 | Médio |

---

## 6. Plano de Remediação por Sprint

### Sprint 1 — Bloqueadores (C-01 a C-10, A-01 a A-03)
1. Corrigir colisão de tabela `chg_promotion_gates` → `prm_promotion_gates`
2. Adicionar `.IsRequired()` a todas as configurações de TenantId
3. Remover constante JWT de fallback hardcoded
4. Migrar API Keys para storage encriptado em DB
5. Implementar 6 endpoints de auth em falta
6. Substituir inputs GUID por entity pickers nos 4 ecrãs afectados

### Sprint 2 — Estabilização (A-04 a A-07, M-01 a M-05)
1. Substituir `catch { /* empty */ }` por logging + `Result.Failure()`
2. Corrigir null handling em `IncidentCorrelationService`
3. Adicionar workflow de aprovação ao Break Glass
4. Corrigir DTO mismatch no endpoint de correlação
5. Externalizar thresholds para configuração
6. Adicionar i18n às strings hardcoded no frontend

### Sprint 3 — Qualidade (M-06 a M-14, Low)
1. Implementar integration events no módulo Configuration
2. Corrigir routing de runbooks
3. Adicionar loading states nos ecrãs em falta
4. Substituir heurísticas OnCall por dados reais
5. Remover mock data permanente do Export endpoint

---

## 7. Relatórios Detalhados

- [AUDIT-BACKEND-2026-04-10.md](./AUDIT-BACKEND-2026-04-10.md)
- [AUDIT-FRONTEND-2026-04-10.md](./AUDIT-FRONTEND-2026-04-10.md)
- [AUDIT-DATABASE-2026-04-10.md](./AUDIT-DATABASE-2026-04-10.md)
- [AUDIT-API-2026-04-10.md](./AUDIT-API-2026-04-10.md)
- [AUDIT-SECURITY-2026-04-10.md](./AUDIT-SECURITY-2026-04-10.md)

---

## 8. Pontos Positivos Identificados

O NexTraceOne demonstra maturidade técnica significativa em várias áreas:

- Arquitectura modular bem isolada — sem violações de DbContext entre módulos
- CSRF protection correctamente implementada (double-submit cookie, constant-time comparison)
- JWT validation robusto com HMAC-SHA256, audience/issuer validation
- Password hashing com PBKDF2-SHA256, 100.000 iterações, salt aleatório
- Tenant isolation via JWT claims com RLS no PostgreSQL
- Rate limiting abrangente por categoria de endpoint
- Security event audit trail com risk scores
- Soft delete global via `NexTraceDbContextBase`
- Outbox pattern implementado em todos os módulos
- 27 DbContexts com migrations activas e ModelSnapshot
- i18n estruturado e aplicado na maioria do frontend
- Design system consistente com Radix UI + Tailwind
