# Environment Management — Module Boundary Finalization

> **Módulo:** 02 — Environment Management  
> **Data:** 2026-03-25  
> **Fase:** N4-R — Reexecução completa  
> **Estado:** ✅ FINALIZADO

---

## 1. Problema actual

Environment Management **não existe como bounded context independente**. Todas as suas entidades, features e persistência vivem dentro do módulo Identity & Access (`src/modules/identityaccess/`).

Isto cria acoplamento excessivo entre:
- gestão de ambientes (lifecycle, perfil, criticidade)
- controlo de acesso/identidade (utilizadores, roles, permissões)

O relatório `docs/architecture/phase-a-open-items.md` (item OI-04) classifica isto como **BLOCKING** para a aplicação do prefixo `env_`.

---

## 2. Decisão de fronteira final

### O que passa a ser dono de Environment Management

| Responsabilidade | Entidade/Artefacto | Justificação |
|-----------------|-------------------|--------------|
| Lifecycle do ambiente | `Environment` (aggregate root) | Core do módulo — criação, edição, activação, desactivação |
| Perfil e criticidade | `EnvironmentProfile`, `EnvironmentCriticality` | Dimensão funcional do ambiente |
| Metadados operacionais | `Code`, `Region`, `Description`, `IsProductionLike`, `IsPrimaryProduction` | Informação operacional inerente ao ambiente |
| Políticas por ambiente | `EnvironmentPolicy` | Promotion approval, freeze window, quality gates |
| Políticas de telemetria | `EnvironmentTelemetryPolicy` | Retenção, verbosidade, anonimização por ambiente |
| Bindings de integração | `EnvironmentIntegrationBinding` | Conectores CI/CD, observability, alerting por ambiente |
| UI Profile | `EnvironmentUiProfile` | Badge color, protection warning — apresentação |
| Resolução de contexto | `TenantEnvironmentContext` | VO que encapsula contexto resolvido |
| Repositório | `IEnvironmentRepository` | CRUD e queries de ambientes |
| API endpoints | `/api/v1/environments/*` | Toda a gestão de ambientes |
| Frontend CRUD | `EnvironmentsPage` | Deve migrar para `features/environment-management/` |

### O que continua em Identity & Access

| Responsabilidade | Entidade/Artefacto | Justificação |
|-----------------|-------------------|--------------|
| Controlo de acesso a ambiente | `EnvironmentAccess` | Parte do authorization framework do Identity |
| Validação de acesso | `IEnvironmentAccessValidator` | Necessário no middleware auth do Identity |
| Authorization handler | `EnvironmentAccessAuthorizationHandler` | Policy-based auth do ASP.NET |
| Resolução middleware | `EnvironmentResolutionMiddleware` | Cross-cutting concern que opera no request pipeline |

**Nota:** `EnvironmentAccess` é uma entidade partilhada. O Environment Management é dono do seu ciclo de vida (GrantAccess, RevokeAccess), mas Identity necessita de lê-la para avaliação de autorização.

### O que continua em Change Governance

| Responsabilidade | Entidade/Artefacto | Justificação |
|-----------------|-------------------|--------------|
| Deployment environments | `DeploymentEnvironment` (entidade própria em `ChangeGovernance.Domain`) | Representa estágios no pipeline de promoção, NÃO ambientes runtime |
| Promotion gates | `PromotionGate` | Associado a `DeploymentEnvironmentId`, não `EnvironmentId` |
| Promotion requests | `PromotionRequest` | `SourceEnvironmentId`, `TargetEnvironmentId` — contexto de deployment |

**Decisão:** Change Governance mantém a sua entidade `DeploymentEnvironment` separada. No futuro, pode consumir `Environment` do Environment Management para enriquecimento (risk scoring via `IsPrimaryProduction`, `Criticality`), mas não deve duplicar o conceito.

### O que continua em Operational Intelligence

| Responsabilidade | Entidade/Artefacto | Justificação |
|-----------------|-------------------|--------------|
| Incidentes por ambiente | `IncidentRecord.EnvironmentId` | Referência por ID, não dependência directa |
| Métricas por ambiente | via `ICurrentEnvironment` | Interface cross-cutting do BuildingBlocks |

### O que continua em AI & Knowledge

| Responsabilidade | Entidade/Artefacto | Justificação |
|-----------------|-------------------|--------------|
| Comparação de ambientes | `EnvironmentComparisonContext` | Usa dados do Environment Management via leitura |
| Risk analysis | `PromotionRiskAnalysisContext` | Consome `IsPrimaryProduction` e `Criticality` |

### O que o Audit & Compliance apenas observa

- Auditoria de ações sobre ambientes (criar, editar, activar, desactivar, designar primary production)
- Auditoria de alterações de acesso (grant, revoke)
- Não é dono de nenhuma entidade de ambiente

---

## 3. Interface cross-module

### Interface exposta pelo BuildingBlocks

```csharp
// src/building-blocks/NexTraceOne.BuildingBlocks.Application/Abstractions/ICurrentEnvironment.cs
public interface ICurrentEnvironment
{
    Guid EnvironmentId { get; }
    bool IsResolved { get; }
    bool IsProductionLike { get; }
}
```

**Implementação:** `CurrentEnvironmentAdapter` em `IdentityAccess.Infrastructure` — adaptador que lê do `EnvironmentContextAccessor`.

Após extracção, este adapter deve migrar para o módulo Environment Management ou ficar num adapter partilhado.

---

## 4. Regras de fronteira

### ✅ O que NUNCA deve ficar fora de Environment Management

1. Definição do que é um ambiente (entidade `Environment`)
2. Perfil e criticidade do ambiente
3. Designação de primary production
4. Políticas por ambiente
5. Metadados operacionais do ambiente

### ❌ O que NUNCA deve ficar dentro de Environment Management

1. Autenticação e autorização de utilizadores
2. Gestão de roles e permissões genéricas
3. Deployment pipelines e promoção (Change Governance)
4. Métricas e incidents (Operational Intelligence)
5. Regras de compliance genéricas (Audit & Compliance)

### ⚠️ Áreas cinzentas que precisam de decisão

| Área | Opção A | Opção B | Recomendação |
|------|---------|---------|-------------|
| `EnvironmentAccess` | Em Environment Management | Em Identity | **Em Identity** — é authorization concern |
| `EnvironmentResolutionMiddleware` | Em Environment Management | Em Identity/Platform | **Em Identity** — opera no pipeline auth |
| `EnvironmentComparisonPage` | Em Environment Management | Em Operations | **Em Operations** — compara métricas, não gestão |
| `EnvironmentContext.tsx` | Em Environment Management | Em Shell | **Em Shell** — é contexto global da aplicação |

---

## 5. Plano de extracção (resumo)

### Fase 1 — Preparação (sem breaking changes)

1. Criar projetos `src/modules/environmentmanagement/` (Domain, Application, Infrastructure, API, Contracts)
2. Copiar entidades `Environment`, `EnvironmentPolicy`, `EnvironmentTelemetryPolicy`, `EnvironmentIntegrationBinding`
3. Copiar enums `EnvironmentProfile`, `EnvironmentCriticality`
4. Copiar value objects `TenantEnvironmentContext`, `EnvironmentUiProfile`
5. Criar `EnvironmentDbContext` com DbSets e prefixo `env_`

### Fase 2 — Migração de features

6. Mover features CQRS (Create, List, Update, SetPrimary, GetPrimary)
7. Criar novos endpoints em `/api/v1/environments/`
8. Mover `EnvironmentsPage` para `features/environment-management/`
9. Registar permissões `env:environments:read`, `env:environments:write`, `env:access:write`

### Fase 3 — Corte

10. Remover entidades duplicadas do IdentityAccess
11. Manter `EnvironmentAccess` em Identity com referência por ID
12. Actualizar `EnvironmentResolutionMiddleware` para consumir via interface
13. Gerar migration `env_` com novo schema

---

## 6. Diagrama de dependência pós-extracção

```
┌────────────────────┐
│  BuildingBlocks     │
│  ICurrentEnvironment│◄───────────────────────────────────┐
└────────┬───────────┘                                     │
         │                                                 │
┌────────▼────────────┐     ┌───────────────────────┐     │
│ Environment Mgmt    │◄────│ Identity & Access      │     │
│ - Environment       │     │ - EnvironmentAccess    │     │
│ - EnvironmentPolicy │     │ - Resolution Middleware │     │
│ - TelemetryPolicy   │     │ - Access Validator     │     │
│ - IntegrationBinding│     └───────────────────────┘     │
│ - EnvironmentDbCtx  │                                    │
└────────┬────────────┘                                    │
         │                                                 │
    ┌────▼─────┐  ┌────────────────┐  ┌──────────────┐   │
    │ Change   │  │ Operational    │  │ AI &         │───┘
    │ Gov      │  │ Intelligence   │  │ Knowledge    │
    └──────────┘  └────────────────┘  └──────────────┘
```
