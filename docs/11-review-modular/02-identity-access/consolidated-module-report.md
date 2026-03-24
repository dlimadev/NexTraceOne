# Identity & Access — Consolidated Module Report

> Gerado a partir da consolidação de todos os relatórios de auditoria e revisão modular do NexTraceOne.
> Última atualização: 2026-03-24

---

## 1. Visão Geral do Módulo

O módulo **Identity & Access** é o módulo fundacional do NexTraceOne. Todos os outros módulos — Contracts, Catalog, Change Governance, Operational Intelligence, AI Knowledge, Governance, etc. — dependem dele para autenticação, autorização, contexto de tenant e ambiente, e rastreabilidade de eventos de segurança.

Implementa uma arquitectura de segurança multi-camada de nível enterprise com:

- **Autenticação híbrida** — JWT Bearer (utilizadores), API Key (machine-to-machine), OIDC Federation (login federado), Cookie Session com CSRF
- **Autorização granular** — 73 permissões únicas em 13 módulos, 7 papéis de sistema, `PermissionPolicyProvider` dinâmico com enforcement deny-by-default
- **Multi-tenancy** — isolamento em 3 camadas (JWT claim → `TenantResolutionMiddleware` → PostgreSQL RLS via `TenantRlsInterceptor`)
- **Gestão de ambientes** — entidade first-class com `EnvironmentProfile`, `Criticality`, `AccessLevel` temporal e resolução via middleware
- **Acesso avançado enterprise** — JIT Access, Break Glass, Delegações temporárias, Access Review Campaigns
- **Rastreabilidade** — `SecurityEvent` com 15+ tipos, risk scoring 0–100, bridge MediatR para módulo de Audit & Compliance

O módulo é a **fonte de verdade** para identidade, permissões, tenants e contexto de segurança de toda a plataforma, alinhando-se directamente com os pilares de **Service Governance**, **Source of Truth** e **Operational Reliability** do NexTraceOne.

---

## 2. Estado Atual

| Dimensão | Maturidade | Notas |
|----------|-----------|-------|
| **Backend** | 🟢 95% | DDD excelente, CQRS completo, 11 módulos de endpoints, domínio rico com 20+ entidades |
| **Frontend** | 🟢 90% | 15 páginas completas (7 públicas + 8 protegidas), persona-aware, react-hook-form + zod |
| **Documentação** | 🟡 60% | Docs de segurança existem, mas sem README de módulo, sem user guide, sem API reference |
| **Testes** | 🟢 85% | 186+ testes, cobertura forte no domínio e application layer |
| **Global** | 🟢 **82%** | — |
| **Prioridade** | **P1** | Fundação — todos os módulos dependem deste |
| **Status** | ✅ Funcional | Módulo mais maduro do produto, com lacunas documentadas e conhecidas |
| **Classificação de Segurança** | ENTERPRISE_READY_APPARENT | ~85% de maturidade enterprise segundo auditoria de segurança |

---

## 3. Problemas Críticos e Bloqueadores

### 🔴 RC-1: MFA modelado mas não enforced em runtime

**Causa raiz:** A política de MFA (`MfaPolicy`) está correctamente modelada como value object no domínio — com factory methods (`ForSaaS`, `ForSelfHosted`, `ForOnPremise`, `Disabled`) e persistência —, mas o **enforcement** não está implementado:

| Componente | Estado |
|------------|--------|
| Value object `MfaPolicy` | ✅ Implementado |
| Persistência no `IdentityDbContext` | ✅ Implementado |
| Enforcement no fluxo de login | ❌ Adiado |
| Step-up para operações privilegiadas | ❌ Adiado |
| UI de enrollment TOTP/WebAuthn | ❌ Não implementada |
| Página `/mfa` para verificação | ✅ Funcional (mas sem enrollment) |

**Severidade:** 🔴 Crítico — é a lacuna de segurança mais relevante do sistema. Qualquer tenant configurado com MFA obrigatório não terá enforcement real.

**Impacto:** Utilizadores podem autenticar-se sem segundo factor mesmo quando a política exige MFA.

**Esforço estimado:** 2–3 semanas (SR-1) — inclui enrollment TOTP com QR code, step-up em login, step-up para operações privilegiadas, e WebAuthn como método preferencial.

---

### 🔴 RC-2: API Key armazenada em memória (appsettings)

**Causa raiz:** As API keys para autenticação machine-to-machine estão armazenadas em `appsettings.json` (in-memory), sem rotação nem scoping.

| Aspecto | Estado |
|---------|--------|
| Comparação segura (`FixedTimeEquals`) | ✅ Resistente a timing attacks |
| Armazenamento em BD encriptada | ❌ Em `appsettings` (MVP1) |
| Rotação de keys | ❌ Não implementada |
| Scoping por permissão/tenant | ❌ Acesso total para todas as keys |

**Severidade:** 🔴 Crítico para produção — qualquer API key comprometida dá acesso total ao sistema.

**Esforço estimado:** 1 semana (SR-7) — migrar para BD com encriptação AES-256-GCM (já suportada pelo `EncryptedStringConverter`), com dual-read temporário durante migração.

---

## 4. Problemas por Camada

### 4.1 Frontend

| # | Problema | Severidade | Ficheiro(s) | Detalhe |
|---|---------|-----------|------------|---------|
| F-1 | Rota `/environments` escondida do menu | 🟡 Médio | `AppSidebar.tsx` | `EnvironmentsPage` acessível apenas internamente, sem item no menu admin |
| F-2 | Mensagens de erro do backend possivelmente não localizadas | 🟡 Médio | API client / backend responses | Backend pode retornar mensagens hardcoded em inglês; frontend usa i18n mas pode exibir respostas directas |
| F-3 | Diferença de contagem permissões frontend (84+) vs backend (73) | 🟢 Baixo | `auth/permissions.ts` vs `RolePermissionCatalog` | Frontend inclui chaves preparadas para funcionalidades futuras e agrupamentos de UI — necessita sincronização periódica |
| F-4 | Comentários inline a 0,95% no frontend | 🟢 Baixo | Componentes React | Código funcional mas praticamente sem documentação inline |

**Nota positiva:** Todas as 15 páginas estão funcionais, com estados de loading, erro e validação via react-hook-form + zod. O design token system (`--nto-*`) está consistente. O auth shell (55% hero + 45% card) é enterprise-grade. A i18n cobre 4 locales (en, es, pt-BR, pt-PT) em todas as páginas de autenticação e administração.

### 4.2 Backend

| # | Problema | Severidade | Componente | Detalhe |
|---|---------|-----------|-----------|---------|
| B-1 | Enforcement de `EnvironmentAccessValidator` parcial | 🟠 Alto | Handlers de comando | A entidade `EnvironmentAccess` e o validator existem, mas a integração nos handlers de comando não está completa |
| B-2 | Background jobs de expiração ausentes | 🟠 Alto | JIT, Break Glass, Delegation | Transições automáticas (Pending→Expired, Active→Expired) dependem de verificação no momento do acesso, sem job proactivo |
| B-3 | Self-action prevention não verificada | 🟠 Alto | `JitAccessEndpoints`, `DelegationEndpoints` | Auto-aprovação de JIT e auto-delegação podem não ser impedidas |
| B-4 | Notificação em tempo real de Break Glass ausente | 🟡 Médio | `BreakGlassEndpoints` | Activação não alerta PlatformAdmin ou SecurityReview em tempo real |
| B-5 | JWT usa HMAC-SHA256 (simétrico) | 🟡 Médio | `JwtTokenService` | Adequado para single-issuer, mas RS256 (assimétrico) seria preferível para cenários multi-serviço |
| B-6 | Auditoria endpoint-a-endpoint por verificar | 🟡 Médio | Todos os endpoints | Necessita confirmação de que 100% dos endpoints têm `[Authorize]` ou `[AllowAnonymous]` explícito |

**Nota positiva:** A arquitectura DDD está exemplar — 20+ entidades com value objects ricos (`SessionPolicy`, `MfaPolicy`, `AuthenticationPolicy`, `RefreshTokenHash`), eventos de domínio (`UserCreatedDomainEvent`, `UserLockedDomainEvent`), factory methods separados para utilizadores locais/federados, e CQRS com MediatR. O rate limiting protege endpoints sensíveis (20/min geral, 10/min login).

### 4.3 Database

| # | Problema | Severidade | Componente | Detalhe |
|---|---------|-----------|-----------|---------|
| D-1 | Apenas 2 migrations (potencial de consolidação) | 🟢 Baixo | `InitialCreate`, `AddIsPrimaryProductionToEnvironment` | Número baixo é positivo; necessita verificação de reversibilidade |
| D-2 | RowVersion/ConcurrencyToken ausente | 🟡 Médio | User, Tenant, Role | Entidades críticas sem controlo optimista de concorrência |
| D-3 | Chaves de cache podem não incluir tenant_id | 🟡 Médio | Camada de cache (se existir) | Risco de data leakage via cache partilhado entre tenants |

**Nota positiva:** O `IdentityDbContext` tem 16 DbSets bem mapeados, RLS via `TenantRlsInterceptor` em TODAS as operações EF Core (Reader, NonQuery, Scalar) com SQL parametrizado, `AuditInterceptor` com timestamps e user tracking, encriptação AES-256-GCM para campos sensíveis, soft delete, e outbox pattern para integration events. O seed inclui 2 tenants, 5 ambientes, 8 registos de acesso e 8 security events exemplificativos.

### 4.4 Segurança

| # | Problema | Severidade | Detalhe |
|---|---------|-----------|---------|
| S-1 | **MFA enforcement adiado** | 🔴 Crítico | Ver secção 3, RC-1 |
| S-2 | **API key em appsettings** | 🔴 Crítico | Ver secção 3, RC-2 |
| S-3 | Validação IP/UserAgent de sessão não implementada | 🟠 Alto | Dados de IP e UserAgent são **recolhidos** em `Session` mas não validados — sem detecção de session hijacking |
| S-4 | Enforcement de post-mortem Break Glass não automatizado | 🟡 Médio | Sem notificação/escalação automática após 24h; limite trimestral de 3 existe mas post-mortem é apenas política, não enforcement |
| S-5 | CORS necessita verificação | 🟡 Médio | Configuração deve ser validada para confirmar que não é wildcard em produção |
| S-6 | JWT secret deve ser configurado externamente | 🟡 Médio | Chave de fallback dev existe; validação de arranque impede uso em produção — mas deve ser monitorizado |

**Nota positiva:** A defesa em profundidade é exemplar: 3 camadas de tenant isolation (JWT→middleware→RLS), 73 permissões deny-by-default, BCrypt para password hashing com bloqueio após 5 tentativas/15min, refresh token rotation com SHA-256, OIDC (Authorization Code + PKCE) com configuração per-tenant, CSRF via middleware, rate limiting diferenciado (auth: 20/min, auth-sensitive: 10/min), `SecurityEvent` com risk scoring (brute force: 80, localização incomum: 60), e pipeline de middleware na ordem correcta.

### 4.5 IA e Agentes

| # | Problema | Severidade | Detalhe |
|---|---------|-----------|---------|
| AI-1 | Nenhuma capacidade de IA implementada | 🟡 Médio | A revisão modular indica IA como "não aplicável" a este módulo, mas o template detalhado (01-identity-access/ai/) identifica 4 capacidades esperadas: assistência na criação de roles, análise de permissões excessivas, detecção de anomalias de acesso, e sugestão de políticas |
| AI-2 | Nenhum agent implementado | 🟡 Médio | 3 agents esperados: Identity Security Agent (monitorização), Access Review Agent (assistência a campanhas), Permission Analyzer Agent (optimização) — todos `NOT_STARTED` |

**Nota:** O plano de fecho do módulo classifica IA/Agents como "N/A para este módulo (segurança manual obrigatória)", indicando decisão consciente de adiar. Contudo, as capacidades de IA identificadas (detecção de anomalias, análise de permissões) alinham-se com o pilar de **AI-assisted Operations** do NexTraceOne e deveriam ser planeadas para fases futuras.

### 4.6 Documentação

| # | Problema | Severidade | Detalhe |
|---|---------|-----------|---------|
| DOC-1 | README do módulo inexistente | 🟠 Alto | Nenhum ficheiro README em `src/modules/identityaccess/` — impossível onboarding rápido |
| DOC-2 | User guide inexistente | 🟠 Alto | Não existe `docs/user-guide/identity-access.md` |
| DOC-3 | API reference inexistente | 🟡 Médio | Endpoints documentados apenas nos relatórios de auditoria, sem docs standalone |
| DOC-4 | Diagrama de fluxo de autenticação inexistente | 🟡 Médio | Sem representação visual dos fluxos JWT/OIDC/MFA/Cookie |
| DOC-5 | Template detalhado (01-identity-access/) maioritariamente vazio | 🟡 Médio | Module overview, domain rules, endpoints, validation, schema, migrations, seeds, comments, onboarding, bugs, debt, test scenarios — todos com estado `NOT_STARTED` e campos `[A PREENCHER]` |

**Nota positiva:** A documentação de segurança existente é extensa — `SECURITY.md`, `SECURITY-ARCHITECTURE.md`, `BACKEND-ENDPOINT-AUTH-AUDIT.md`, e relatórios de auditoria detalhados nos governance reports (autenticação, autorização, tenant isolation, break glass/JIT). XML docs no backend estão a 97,5%.

---

## 5. Dependências

### Dependências deste módulo (de quem depende)

O Identity & Access é o **módulo fundacional** — não depende de nenhum outro módulo funcional do NexTraceOne. Depende apenas dos building blocks:

| Dependência | Tipo | Componente |
|-------------|------|-----------|
| `NexTraceOne.BuildingBlocks.Security` | Framework | `PermissionAuthorizationHandler`, `HttpContextCurrentUser`, API Key handler |
| `NexTraceOne.SharedKernel` | Framework | `Entity<TId>`, `AggregateRoot`, `AuditableEntity`, `TenantRlsInterceptor`, `AuditInterceptor`, `EncryptedStringConverter` |
| PostgreSQL | Infraestrutura | Row-Level Security, `set_config` |

### Módulos que dependem deste

| Módulo | Tipo de dependência |
|--------|-------------------|
| **Todos os 11 restantes** | Autenticação, autorização, contexto tenant/environment |
| Contracts | Permissões `contracts:*`, ownership baseado em identidade |
| Catalog | Permissões `catalog:*`, service ownership por utilizador/equipa |
| Change Governance | Contexto de identidade para registar e validar mudanças |
| Audit & Compliance | `SecurityAuditBridge` — eventos de segurança originam neste módulo |
| AI & Knowledge | Governança de acesso a modelos e capacidades de IA |
| Governance | Permissões de relatórios, compliance, FinOps |

**Implicação:** Qualquer regressão neste módulo bloqueia a totalidade da plataforma.

---

## 6. Quick Wins

| # | Acção | Esforço | Impacto | Severidade |
|---|-------|---------|---------|-----------|
| QW-1 | Adicionar rota `/environments` ao menu admin em `AppSidebar.tsx` | 15 min | Página acessível sem URL manual | 🟡 Médio |
| QW-2 | Verificar e corrigir mensagens de erro i18n vindas do backend | 2h | Backend retorna `code`/`messageKey` em vez de texto hardcoded | 🟡 Médio |
| QW-3 | Criar README mínimo do módulo (`src/modules/identityaccess/README.md`) | 2–3h | Onboarding possível | 🟠 Alto |
| QW-4 | Sincronizar `auth/permissions.ts` (84+) com `RolePermissionCatalog` (73) — remover chaves órfãs | 2h | Consistência frontend↔backend | 🟢 Baixo |
| QW-5 | Preencher templates detalhados do `01-identity-access/` com dados reais da `02-identity-access/module-review.md` | 4–6h | Documentação modular completa | 🟡 Médio |
| QW-6 | Criar user guide `docs/user-guide/identity-access.md` | 3h | Documentação funcional para utilizadores | 🟠 Alto |
| QW-7 | Criar diagrama de fluxo de autenticação (JWT, OIDC, MFA, Cookie) | 2h | Compreensão visual dos fluxos | 🟡 Médio |
| QW-8 | Adicionar teste automatizado que valida `[Authorize]`/`[AllowAnonymous]` em todos os endpoints | 3h | Detecção de endpoints desprotegidos | 🟡 Médio |

**Esforço total estimado:** ~2–3 dias para todos os quick wins.

---

## 7. Refactors Estruturais

| # | Refactor | Esforço | Risco | Severidade | Dependências |
|---|---------|---------|-------|-----------|-------------|
| SR-1 | **Implementar MFA enforcement em runtime** — enrollment TOTP com QR code, step-up em login, step-up para operações privilegiadas, WebAuthn como método preferencial | 2–3 semanas | Médio — pode bloquear utilizadores existentes (mitigação: período de graça 14 dias) | 🔴 Crítico | Nenhuma |
| SR-2 | **Migrar API Key storage** para BD encriptada com rotação e scoping por permissão/tenant | 1 semana | Baixo-Médio — dual-read temporário durante migração | 🔴 Crítico | Nenhuma |
| SR-3 | **Adicionar suporte SAML** para federação enterprise completa (ADFS, Azure AD legacy) | 3–4 semanas | Alto — complexidade protocolar; usar library madura (e.g., Sustainsys.Saml2) | 🟠 Alto | SR-1 (MFA deve estar enforced antes) |
| SR-4 | **Implementar background jobs de expiração** — JIT (Pending→Expired após 4h, Approved→Expired após 8h), Break Glass (Active→Expired após 2h), Delegation (Active→Expired após EndsAt) | 1–2 semanas | Baixo | 🟠 Alto | Nenhuma |
| SR-5 | **Automatizar enforcement de post-mortem Break Glass** — notificação após 12h, escalação após 20h, bloqueio de novo Break Glass sem post-mortem pendente | 1 semana | Baixo | 🟡 Médio | SR-4 |
| SR-6 | **Implementar validação de sessão** por IP/UserAgent — detecção de session hijacking (configurável por tenant) | 1 semana | Baixo | 🟡 Médio | Nenhuma |
| SR-7 | **Completar enforcement de `EnvironmentAccessValidator`** nos handlers de comando | 3–5 dias | Baixo | 🟠 Alto | Nenhuma |
| SR-8 | **Adicionar RowVersion/ConcurrencyToken** em User, Tenant, Role | 3–5 dias | Médio — pode causar breaking changes em handlers existentes | 🟡 Médio | Nenhuma |

**Esforço total estimado:** 8–12 semanas sequenciais; 5–8 semanas com paralelização.

---

## 8. Critérios de Fecho

O módulo Identity & Access será considerado **fechado** (maturidade ≥90%) quando:

### Segurança (obrigatório)
- [ ] MFA enforcement activo para utilizadores com MFA configurado
- [ ] API keys armazenadas em BD com encriptação AES-256-GCM
- [ ] Validação de CORS confirmada em todos os ambientes
- [ ] JWT secret configurado externamente em produção (validação de arranque activa)
- [ ] Self-approval prevention validada em JIT Access
- [ ] Self-delegation prevention validada em Delegations

### Backend (obrigatório)
- [ ] `EnvironmentAccessValidator` enforced em todos os handlers de comando
- [ ] Background jobs de expiração implementados para JIT, Break Glass, Delegation
- [ ] Post-mortem enforcement automatizado para Break Glass
- [ ] Todos os endpoints com `[Authorize]` ou `[AllowAnonymous]` explícito verificado por teste automatizado
- [ ] Seed de produção completo (roles, permissões, configurações)

### Frontend (obrigatório)
- [ ] Rota `/environments` acessível via menu
- [ ] Mensagens de erro de autenticação localizadas em 4 locales
- [ ] Permissões frontend sincronizadas com backend

### Documentação (obrigatório)
- [ ] README do módulo criado
- [ ] User guide `identity-access.md` criado
- [ ] Diagrama de fluxo de autenticação documentado
- [ ] API reference de endpoints documentada

### Testes (obrigatório)
- [ ] Auth flow end-to-end validado (login → session → refresh → logout)
- [ ] Multi-tenancy selection e RLS propagation validados
- [ ] OIDC federated login validado (Google, Azure, Okta)
- [ ] Break Glass + JIT Access workflows validados
- [ ] Teste de integração de isolamento cross-tenant (tenant A não vê dados de tenant B)
- [ ] Cobertura de testes ≥85%

### Desejável (não bloqueador)
- [ ] Suporte SAML implementado
- [ ] Validação de sessão por IP/UserAgent
- [ ] RowVersion em entidades críticas
- [ ] Notificação em tempo real de Break Glass
- [ ] Dashboard de acesso avançado para Auditor/SecurityReview

---

## 9. Plano de Ação Priorizado

### Fase 0 — Quick Wins (1–3 dias)

| Ordem | Acção | Ref | Esforço |
|-------|-------|-----|---------|
| 1 | Criar README do módulo | QW-3 | 2–3h |
| 2 | Adicionar `/environments` ao menu | QW-1 | 15 min |
| 3 | Verificar/corrigir i18n de mensagens de erro | QW-2 | 2h |
| 4 | Criar user guide | QW-6 | 3h |
| 5 | Criar diagrama de fluxo de auth | QW-7 | 2h |
| 6 | Sincronizar permissões frontend↔backend | QW-4 | 2h |
| 7 | Teste automático de endpoints protegidos | QW-8 | 3h |

### Fase 1 — Segurança Crítica (1–2 semanas)

| Ordem | Acção | Ref | Esforço |
|-------|-------|-----|---------|
| 8 | **Migrar API Key storage para BD encriptada** | SR-2 | 1 semana |
| 9 | **Implementar MFA enforcement em runtime** | SR-1 | 2–3 semanas |

### Fase 2 — Backend Completude (2–3 semanas)

| Ordem | Acção | Ref | Esforço |
|-------|-------|-----|---------|
| 10 | Completar enforcement de `EnvironmentAccessValidator` | SR-7 | 3–5 dias |
| 11 | Implementar background jobs de expiração | SR-4 | 1–2 semanas |
| 12 | Automatizar post-mortem enforcement de Break Glass | SR-5 | 1 semana |
| 13 | Implementar self-action prevention (JIT + Delegation) | B-3 | 2–3 dias |

### Fase 3 — Robustez e Observabilidade (1–2 semanas)

| Ordem | Acção | Ref | Esforço |
|-------|-------|-----|---------|
| 14 | Implementar validação de sessão por IP/UserAgent | SR-6 | 1 semana |
| 15 | Adicionar RowVersion em entidades críticas | SR-8 | 3–5 dias |
| 16 | Validar CORS em todos os ambientes de deployment | S-5 | 2h |
| 17 | Notificação em tempo real de Break Glass | B-4 | 3–5 dias |

### Fase 4 — Enterprise Completo (roadmap)

| Ordem | Acção | Ref | Esforço |
|-------|-------|-----|---------|
| 18 | Implementar suporte SAML | SR-3 | 3–4 semanas |
| 19 | Dashboard de acesso avançado (Auditor/SecurityReview) | — | 1–2 semanas |
| 20 | Capacidades de IA (detecção de anomalias, análise de permissões) | AI-1/AI-2 | Definição futura |

---

## 10. Inconsistências entre Relatórios

| # | Inconsistência | Relatório A | Relatório B | Resolução proposta |
|---|---------------|------------|------------|-------------------|
| I-1 | **Maturidade de segurança**: 85% vs 82% | `security-identity-and-access-audit.md` (85% ENTERPRISE_READY_APPARENT) | `modular-review-summary.md` (82% overall) | Os valores medem dimensões diferentes: 85% é segurança pura, 82% inclui documentação (60%) que baixa a média. **Ambos correctos no seu contexto.** |
| I-2 | **Contagem de permissões**: 60+ vs 73 vs 84+ | `frontend-permissions-and-guards-report.md` (60+) | `authorization-and-permissions-report.md` (73 backend) / `security-identity-and-access-audit.md` (84+ frontend) | O frontend report contabiliza apenas permissões directamente usadas em guards, enquanto o ficheiro `permissions.ts` define 84+ strings (inclui preparação futura). O backend tem 73 reais. **Normalizar para: 73 permissões backend, 84+ chaves frontend.** |
| I-3 | **Estado do template detalhado**: `NOT_STARTED` vs revisão completa | `01-identity-access/` (todas as secções `NOT_STARTED`, campos `[A PREENCHER]`) | `02-identity-access/module-review.md` (revisão detalhada e completa) | O template detalhado foi criado como estrutura para preenchimento futuro mas nunca foi actualizado com os dados da revisão já realizada. **O module-review.md é a fonte de verdade actual.** |
| I-4 | **Contagem de entidades**: 20+ vs 37 | `module-review.md` ("20+ entidades") | `backend-persistence-report.md` (37 entidades estimadas para IdentityDb) | A discrepância deve-se ao escopo: 20+ refere-se a entidades de domínio ricas, 37 inclui todas as entidades persistidas (incluindo join tables, configs, etc.). **Ambos correctos.** |
| I-5 | **Número de DbSets**: 15 vs 16 | `security-identity-and-access-audit.md` (15 DbSets) | `module-review.md` (16 DbSets, incluindo EnvironmentAccesses) | A auditoria de segurança foi feita antes da adição de `EnvironmentAccesses`. **16 é o valor actual.** |
| I-6 | **IA no módulo**: "Não aplicável" vs 4 capacidades esperadas | `module-review.md` ("❌ Não aplicável") | `01-identity-access/ai/ai-capabilities.md` (4 capacidades esperadas) / `module-closure-plan.md` ("N/A — segurança manual obrigatória") | Decisão consciente de manter IA fora do módulo de segurança na fase actual. As capacidades esperadas são para fases futuras. **Respeitar decisão actual; reavaliar na Fase 4.** |
