# Phase 4 — Backend Modular Refactor Plan

## Objetivo

A Fase 4 expande a fundação arquitetural do NexTraceOne adicionando:

1. **ICurrentEnvironment** — abstração de ambiente no BuildingBlocks.Application
2. **Enriquecimento de contexto tenant/ambiente** nas entidades IncidentRecord e Release
3. **AI Readiness Surfaces** — interfaces de consulta contextual para o módulo AI

## Motivação

### Por que ICurrentEnvironment no BuildingBlocks?

Antes da Fase 4, os módulos operacionais (OI, CG) não tinham acesso ao ambiente ativo
sem depender diretamente do módulo IdentityAccess. Isso violava o isolamento modular.

A solução é expor `ICurrentEnvironment` em `BuildingBlocks.Application` (camada compartilhada),
implementado pelo `IdentityAccess.Infrastructure` como adapter sobre o `EnvironmentContextAccessor`.

### Por que enriquecer IncidentRecord e Release com TenantId/EnvironmentId?

Ambas as entidades já tinham um campo `Environment` (string) com o nome do ambiente.
Porém, para correlação cruzada, consultas de IA e análise comparativa entre ambientes,
é necessário um identificador estruturado (`Guid`) que referencie o registro de ambiente
do módulo IdentityAccess.

Os novos campos são **nullable** para garantir retrocompatibilidade total.

### Por que AI Readiness Surfaces?

O módulo de IA precisará consultar incidentes e releases filtrados por tenant e ambiente.
As surfaces (`IIncidentContextSurface`, `IReleaseContextSurface`) expõem essa capacidade
de forma isolada, auditável e tenant-safe.

## Mudanças por Camada

| Camada | Mudança |
|--------|---------|
| BuildingBlocks.Application | Nova interface ICurrentEnvironment |
| IdentityAccess.Infrastructure | CurrentEnvironmentAdapter + registro DI |
| OI.Domain | IncidentRecord.TenantId, EnvironmentId, SetTenantContext |
| OI.Application | CreateIncidentInput + TenantId/EnvironmentId; IIncidentContextSurface |
| OI.Infrastructure | EfIncidentStore.SetTenantContext; IncidentContextSurface; migração |
| CG.Domain | Release.TenantId, EnvironmentId, SetTenantContext |
| CG.Application | IReleaseContextSurface |
| CG.Infrastructure | ReleaseContextSurface; migração |
