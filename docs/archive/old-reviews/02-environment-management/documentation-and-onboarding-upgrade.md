# Environment Management — Documentation and Onboarding Upgrade

> **Módulo:** 02 — Environment Management  
> **Data:** 2026-03-25  
> **Fase:** N4-R — Reexecução completa  
> **Estado:** ✅ FINALIZADO

---

## 1. Estado actual da documentação

### 1.1 README do módulo

**Estado:** ❌ **Inexistente**

Não existe `README.md` dedicado ao Environment Management em nenhum nível:
- ❌ `src/modules/environmentmanagement/README.md` — módulo backend não existe
- ❌ `docs/modules/environment-management/README.md` — sem documentação de módulo
- ❌ `src/frontend/src/features/environment-management/README.md` — feature frontend não existe como directório dedicado

### 1.2 Documentação de review existente

| Documento | Caminho | Estado |
|----------|---------|--------|
| `module-overview.md` | `docs/11-review-modular/02-environment-management/module-overview.md` | ✅ Existe — overview geral |
| `module-consolidated-review.md` | `docs/11-review-modular/02-environment-management/module-consolidated-review.md` | ✅ Existe — review consolidado |
| `README.md` | `docs/11-review-modular/02-environment-management/README.md` | ✅ Existe — índice do review |
| `current-state-inventory.md` | `docs/11-review-modular/02-environment-management/current-state-inventory.md` | ✅ Existe — inventário N4-R |
| `module-boundary-finalization.md` | `docs/11-review-modular/02-environment-management/module-boundary-finalization.md` | ✅ Existe — fronteiras N4-R |

**Nota:** Estes são documentos de **review/audit**, não documentação operacional para developers.

---

## 2. Documentação fragmentada

### 2.1 Subdirectórios de review existentes

| Directório | Conteúdo | Estado |
|-----------|----------|--------|
| `docs/11-review-modular/02-environment-management/ai/` | Review IA do módulo | ⚠️ Verificar conteúdo |
| `docs/11-review-modular/02-environment-management/backend/` | Review backend | ⚠️ Verificar conteúdo |
| `docs/11-review-modular/02-environment-management/database/` | Review database | ⚠️ Verificar conteúdo |
| `docs/11-review-modular/02-environment-management/documentation/` | Review de documentação | ⚠️ Verificar conteúdo |
| `docs/11-review-modular/02-environment-management/frontend/` | Review frontend | ⚠️ Verificar conteúdo |
| `docs/11-review-modular/02-environment-management/quality/` | Review qualidade | ⚠️ Verificar conteúdo |
| `docs/11-review-modular/02-environment-management/b1-consolidation/` | Consolidação B1 | ⚠️ Verificar conteúdo |

### 2.2 Documentação em outros locais

| Ficheiro | Referência a Environment | Tipo |
|---------|-------------------------|------|
| `docs/architecture/phase-a-open-items.md` | OI-04: BLOCKING — env_ prefix | Impedimento técnico |
| `docs/architecture/module-boundaries.md` | Fronteiras de módulos | Arquitectura |
| Execution/audit reports (se existem) | Histórico de evolução | Histórico |

---

## 3. Documentação ausente — análise prioritária

| # | Documento | Impacto | Prioridade | Esforço |
|---|----------|---------|-----------|---------|
| D-01 | **Module README** — propósito, arquitectura, entidades, endpoints, setup, dependências | Onboarding impossível sem conhecimento tribal | **ALTA** | 4h |
| D-02 | **API Reference** — 6 endpoints actuais + endpoints planeados, request/response examples | Consumidores não conseguem integrar sem ler código | **ALTA** | 3h |
| D-03 | **Domain Model Documentation** — aggregates, entities, enums, value objects, invariantes | Compreensão do domínio requer leitura de código | **MÉDIA** | 2h |
| D-04 | **Environment Lifecycle Guide** — fluxos de criação, activação, desactivação, primary designation | Operadores não compreendem fluxos sem contexto | **MÉDIA** | 2h |
| D-05 | **Access Control Model** — níveis de acesso, temporal access, grant/revoke flows | Administradores sem guia de gestão de acessos | **MÉDIA** | 2h |
| D-06 | **Permissions Guide** — namespace `env:*`, mapping permissões → acções | Admins e developers sem referência de permissões | **MÉDIA** | 1h |
| D-07 | **Cross-Module Integration Guide** — como outros módulos consomem dados de Environment | Developers de outros módulos sem guia de integração | **BAIXA** | 2h |
| D-08 | **Environment Profiles Reference** — 9 perfis, criticidade, UI profiles, badges | Configuração de novos ambientes sem referência | **BAIXA** | 1h |
| D-09 | **Troubleshooting Guide** — problemas comuns, resolução de middleware, RLS issues | Suporte e on-call sem referência | **BAIXA** | 2h |

---

## 4. Classes e métodos sem XML docs

### 4.1 Domain layer

| Classe | Métodos sem docs | Prioridade |
|--------|-----------------|-----------|
| `Environment` (247 linhas) | Todos os factory methods e domain methods (8+) | ALTA |
| `EnvironmentAccess` (174 linhas) | `Create()`, `Revoke()`, `IsActiveAt()`, `ChangeAccessLevel()` | ALTA |
| `EnvironmentPolicy` (124 linhas) | Classe inteira — entidade Phase 2 sem docs | MÉDIA |
| `EnvironmentTelemetryPolicy` (122 linhas) | Classe inteira | MÉDIA |
| `EnvironmentIntegrationBinding` (90+ linhas) | Classe inteira | MÉDIA |
| `TenantEnvironmentContext` | Propósito do VO não documentado | MÉDIA |
| `EnvironmentUiProfile` | Lógica de resolução de badge/warning | MÉDIA |
| `EnvironmentProfile` enum | Significado de cada perfil não documentado | BAIXA |
| `EnvironmentCriticality` enum | Critérios para cada nível não documentados | BAIXA |

### 4.2 Application layer (CQRS handlers)

| Handler | Ficheiro | Docs | Prioridade |
|---------|---------|------|-----------|
| `CreateEnvironment` | `Application/Features/CreateEnvironment/` | ❌ | MÉDIA |
| `ListEnvironments` | `Application/Features/ListEnvironments/` | ❌ | BAIXA |
| `UpdateEnvironment` | `Application/Features/UpdateEnvironment/` | ❌ | MÉDIA |
| `SetPrimaryProductionEnvironment` | `Application/Features/SetPrimaryProductionEnvironment/` | ❌ | ALTA — acção crítica |
| `GetPrimaryProductionEnvironment` | `Application/Features/GetPrimaryProductionEnvironment/` | ❌ | BAIXA |
| `GrantEnvironmentAccess` | `Application/Features/GrantEnvironmentAccess/` | ❌ | ALTA — acção sensível |

### 4.3 Infrastructure layer

| Classe | Propósito sem docs | Prioridade |
|--------|-------------------|-----------|
| `EnvironmentContextAccessor` | Como acessa o ambiente resolvido | MÉDIA |
| `EnvironmentAccessValidator` | Algoritmo de validação de acesso | ALTA |
| `EnvironmentProfileResolver` | Lógica de resolução de badge/warning/AI flag | MÉDIA |
| `TenantEnvironmentContextResolver` | Como resolve contexto tenant+environment | MÉDIA |
| `EnvironmentResolutionMiddleware` | Como processa header `X-Environment-Id` | ALTA |
| `EnvironmentRepository` | Queries específicas (primary production, etc.) | MÉDIA |
| `CurrentEnvironmentAdapter` | Implementação de `ICurrentEnvironment` | BAIXA |
| `EnvironmentConfiguration` (EF) | Mapping details, indexes, constraints | MÉDIA |
| `EnvironmentAccessConfiguration` (EF) | Mapping details | MÉDIA |

### 4.4 Frontend

| Componente | Docs inline | Prioridade |
|-----------|-------------|-----------|
| `EnvironmentsPage.tsx` (~434 linhas) | ⚠️ Verificar JSDoc | MÉDIA |
| `EnvironmentComparisonPage.tsx` (~623 linhas) | ⚠️ Verificar JSDoc | MÉDIA |
| `EnvironmentContext.tsx` (~261 linhas) | ⚠️ Verificar JSDoc | ALTA — shell concern usado globalmente |
| `EnvironmentBanner.tsx` (~46 linhas) | ⚠️ Verificar JSDoc | BAIXA |

---

## 5. Documentação mínima obrigatória (módulo funcional)

| # | Documento | Caminho alvo | Conteúdo mínimo | Esforço |
|---|----------|-------------|-----------------|---------|
| 1 | **Module README** | `src/modules/environmentmanagement/README.md` | Propósito, arquitectura (Clean Arch + DDD + CQRS), entidades, endpoints, dependências, convenções | 4h |
| 2 | **API Reference** | `docs/modules/environment-management/api-reference.md` | 6 endpoints com request/response JSON, permissões, error codes | 3h |
| 3 | **Environment Lifecycle** | `docs/modules/environment-management/environment-lifecycle.md` | Diagrama de estados, fluxos de criação/activação/primary designation | 2h |

**Total mínimo: 9 horas**

---

## 6. Documentação de fluxos

### 6.1 Fluxos que devem ser documentados

| Fluxo | Complexidade | Módulos envolvidos | Prioridade |
|-------|-------------|-------------------|-----------|
| Criar ambiente | Baixa | Environment Management | ALTA |
| Designar primary production | Média | Environment Management, (Audit) | ALTA |
| Grant environment access | Média | Environment Management, Identity | ALTA |
| Resolver ambiente no request | Alta | Identity (middleware), Environment Management (adapter), BuildingBlocks (interface) | ALTA |
| Revogar acesso (a implementar) | Baixa | Environment Management, (Audit, Notifications) | MÉDIA |
| Comparar ambientes | Média | Environment Management, Operational Intelligence, (AI) | MÉDIA |
| Aplicar policy a ambiente (Phase 2) | Alta | Environment Management, Change Governance | BAIXA |

### 6.2 Formato recomendado

```
## Fluxo: Designar Primary Production

### Pré-condições
- Utilizador com permissão `env:environments:admin`
- Ambiente activo e não eliminado

### Sequência
1. Frontend envia POST /api/v1/environments/{id}/set-primary-production
2. Handler carrega ambiente actual primary production (se existir)
3. Handler revoga designação do anterior: `previousPrimary.RevokePrimaryProductionDesignation()`
4. Handler designa o novo: `targetEnvironment.DesignateAsPrimaryProduction()`
5. Handler salva alterações (ambas entidades)
6. Domain event: `PrimaryProductionDesignated` (a implementar)
7. Partial unique index garante unicidade

### Pós-condições
- Exactamente 1 ambiente é primary production por tenant
- Previous primary tem `IsPrimaryProduction = false`
- New primary tem `IsPrimaryProduction = true` e `IsProductionLike = true`
```

---

## 7. Notas de onboarding

### 7.1 O que um novo developer precisa saber

1. **Módulo não tem backend dedicado** — tudo está embebido em `src/modules/identityaccess/`. Procurar ficheiros com prefixo `Environment` no Identity module.

2. **5 entidades, 2 persistidas** — `Environment` e `EnvironmentAccess` têm DbSet e EF mapping. `EnvironmentPolicy`, `EnvironmentTelemetryPolicy` e `EnvironmentIntegrationBinding` estão definidas no domínio mas **não têm persistência**.

3. **Permissões genéricas** — endpoints usam `identity:users:read/write` em vez de `env:*`. Isto é um gap conhecido (SEC-01).

4. **Tabelas com prefixo errado** — `identity_environments` e `identity_environment_accesses` deveriam ter prefixo `env_`. Bloqueado por OI-04.

5. **Frontend em feature errada** — `EnvironmentsPage` está em `features/identity-access/` em vez de `features/environment-management/`.

6. **Sem sidebar** — a página de ambientes não aparece na navegação. Acesso apenas por URL directo `/environments`.

7. **2 conceitos de "ambiente"** — `Environment` (Environment Management) ≠ `DeploymentEnvironment` (Change Governance). O primeiro é runtime, o segundo é estágio de promotion pipeline.

8. **Resolução de ambiente** — header `X-Environment-Id` → `EnvironmentResolutionMiddleware` → `ICurrentEnvironment`. A interface está no BuildingBlocks.

9. **RLS automático** — todas as queries são filtradas por `tenant_id` via `TenantRlsInterceptor`. Não é necessário filtrar manualmente.

10. **Sem concurrency tokens** — nenhuma entidade tem `RowVersion`/`xmin`. Edições concorrentes resultam em "last write wins" silencioso.

### 7.2 Armadilhas conhecidas

| Armadilha | Detalhe | Mitigação |
|----------|---------|-----------|
| Editar domínio no Identity | Código de Environment está em IdentityAccess.Domain | Procurar por "Environment" no Identity module |
| Confundir Environment com DeploymentEnvironment | Nomes similares, domínios diferentes | Verificar namespace: `IdentityAccess.Domain` vs `ChangeGovernance.Domain` |
| Testar sem tenant | RLS bloqueia tudo sem `TenantId` | Garantir que test setup inclui `TenantId` |
| Slug duplicado | Resulta em `DbUpdateException` não tratada | Validar slug antes de salvar |
| Desactivar primary production | Domain permite (guard ausente) | Bug conhecido (DM-02) |

---

## 8. Plano de execução de documentação

### Phase 1 — Imediata (com módulo actual)

| # | Acção | Esforço | Dependência |
|---|-------|---------|------------|
| DOC-01 | Criar Module README (provisório, referenciando localização no Identity) | 3h | Nenhuma |
| DOC-02 | Adicionar XML docs a `Environment.cs` — factory methods e domain methods | 1h | Nenhuma |
| DOC-03 | Adicionar XML docs a `EnvironmentAccess.cs` | 30min | Nenhuma |
| DOC-04 | Adicionar XML docs a `SetPrimaryProductionEnvironment` handler | 30min | Nenhuma |
| DOC-05 | Adicionar XML docs a `GrantEnvironmentAccess` handler | 30min | Nenhuma |
| DOC-06 | Adicionar XML docs a `EnvironmentResolutionMiddleware` | 30min | Nenhuma |
| DOC-07 | Adicionar JSDoc a `EnvironmentContext.tsx` | 30min | Nenhuma |

### Phase 2 — Após criação do módulo dedicado

| # | Acção | Esforço | Dependência |
|---|-------|---------|------------|
| DOC-08 | Reescrever Module README no novo módulo | 4h | Módulo backend dedicado |
| DOC-09 | Criar API Reference com exemplos JSON | 3h | Endpoints finalizados |
| DOC-10 | Criar Environment Lifecycle Guide | 2h | Domain model finalizado |
| DOC-11 | Criar Access Control Model docs | 2h | Permissões `env:*` implementadas |
| DOC-12 | Criar Cross-Module Integration Guide | 2h | Interfaces estáveis |

**Total: ~20 horas**
