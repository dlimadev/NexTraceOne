# Contract Conformance — Fases de Implementação

> Parte do plano: [01-OVERVIEW.md](01-OVERVIEW.md)

---

## Visão geral das fases

| Fase | Nome | Foco | Prioridade |
|------|------|------|-----------|
| **F1** | Core Conformance Engine | Endpoint + entidade + resolver | Crítica |
| **F2** | Parametrização & Políticas | Configuration module + policy service | Alta |
| **F3** | CI Token Management | Tokens com binding + endpoints | Alta |
| **F4** | Changelog | Entidade + handlers + queries | Alta |
| **F5** | Webhook Delivery | Notificação a consumers | Alta |
| **F6** | Promotion Gate Extension | Conformance como gate de promoção | Média |
| **F7** | Frontend — Conformance & Tokens | UI para visualização e gestão | Média |
| **F8** | Frontend — Changelog | Timeline e feed de eventos | Média |
| **F9** | Notificações Avançadas | Sunset, drift, consumer acceptance | Baixa |
| **F10** | Runtime Drift → Changelog | Fechar loop runtime | Baixa |

---

## Fase 1 — Core Conformance Engine

**Objectivo:** Implementar o mecanismo central de validação de implementação. Após esta fase, o CI já pode chamar o NexTraceOne e receber resultado de conformance.

### Backend

**Novas features CQRS:**

```
Application/Contracts/Features/
  ValidateImplementation/
    ValidateImplementationCommand.cs
    ValidateImplementationCommandHandler.cs
    ValidateImplementationResponse.cs
    ValidateImplementationValidator.cs
```

**Novo domain service:**

```
Application/Contracts/Abstractions/
  IActiveContractResolver.cs

Application/Contracts/Services/
  ActiveContractResolver.cs
```

**Nova entidade + repositório:**

```
Domain/Contracts/Entities/
  ContractConformanceCheck.cs

Application/Contracts/Abstractions/
  IContractConformanceCheckRepository.cs

Infrastructure/Contracts/Persistence/
  Repositories/ContractConformanceCheckRepository.cs
  Configurations/ContractConformanceCheckConfiguration.cs
  Migrations/AddContractConformanceCheck.cs
```

**Novo endpoint:**

```
API/Contracts/Endpoints/
  ContractConformanceEndpointModule.cs
    POST /contracts/validate-implementation
    GET  /contracts/{id}/conformance-history
    GET  /services/{serviceId}/conformance-status
```

**Lógica do handler `ValidateImplementationCommandHandler`:**

```
1. Autenticar request (JWT ou CI Token)
2. Resolver serviceId (de CI Token ou do body)
3. Chamar IActiveContractResolver → ContractVersion
4. Parsear spec implementada (reutilizar parsers existentes)
5. Chamar ContractDiffCalculator (reutilizar)
6. Chamar ClassifyBreakingChange (reutilizar)
7. Calcular score de conformance
8. Criar ContractConformanceCheck + persistir
9. Raise ConformanceCheckCompletedDomainEvent
10. Avaliar política (ver Fase 2) → recommendation
11. Retornar resposta
```

**Testes:**

```
Tests/Contracts/
  ValidateImplementation/
    ValidateImplementationHandlerTests.cs       — unit tests do handler
    ActiveContractResolverTests.cs              — testes de resolução
  E2E/
    contract-conformance-flows.spec.ts          — Playwright E2E
```

---

## Fase 2 — Parametrização & Políticas

**Objectivo:** Tornar o comportamento de conformance configurável por tenant, equipa e ambiente. Sem esta fase, a política é hardcoded.

### Backend

**Seeder de configurações:**

```
Infrastructure/Contracts/Persistence/Seeders/
  ContractConformanceConfigurationSeeder.cs
```

Registar todos os `ConfigurationDefinition` descritos em [03-CONFIGURATION-PARAMETERS.md](03-CONFIGURATION-PARAMETERS.md).

**Serviço de política:**

```
Application/Contracts/Services/
  ContractConformancePolicyService.cs

Application/Contracts/Abstractions/
  IContractConformancePolicyService.cs
```

**Integração no handler (Fase 1 → actualizar):**

```csharp
// ValidateImplementationCommandHandler.cs
var policy = await _policyService.ResolveForServiceAsync(serviceId, environmentId, ct);

// Avaliar se o ambiente é obrigatório
if (!policy.IsRequired)
    return Result.Success(response with { Recommendation = Warn });

// Avaliar blocking policy
var recommendation = policy.BlockingPolicy switch
{
    BlockingPolicy.BreakingOnly        => result.BreakingCount > 0 ? Block : Approve,
    BlockingPolicy.AnyDrift            => result.DeviationCount > 0 ? Block : Approve,
    BlockingPolicy.ScoreBelowThreshold => result.Score < policy.ScoreThreshold ? Block : Approve,
    BlockingPolicy.WarnOnly            => Warn,
    BlockingPolicy.Disabled            => Approve,
    _                                  => Inconclusive
};
```

**Novos ConfigurationDefinition a seedar:**

Ver lista completa em [03-CONFIGURATION-PARAMETERS.md](03-CONFIGURATION-PARAMETERS.md).

---

## Fase 3 — CI Token Management

**Objectivo:** Permitir que equipas criem tokens de CI com binding a serviço, eliminando a necessidade de GUIDs manuais no pipeline.

### Backend

**Nova entidade:**

```
Domain/Contracts/Entities/
  ContractCiToken.cs

Application/Contracts/Abstractions/
  IContractCiTokenRepository.cs

Infrastructure/Contracts/Persistence/
  Repositories/ContractCiTokenRepository.cs
  Configurations/ContractCiTokenConfiguration.cs
  Migrations/AddContractCiTokens.cs
```

**Features CQRS:**

```
Application/Contracts/Features/
  CreateCiToken/
    CreateCiTokenCommand.cs
    CreateCiTokenCommandHandler.cs

  RevokeCiToken/
    RevokeCiTokenCommand.cs
    RevokeCiTokenCommandHandler.cs

  ListCiTokens/
    ListCiTokensQuery.cs
    ListCiTokensQueryHandler.cs
```

**Autenticação por CI Token:**

Criar `CiTokenAuthenticationHandler` que:
- Lê header `Authorization: CiToken ctr_ci_pXXXX`
- Verifica hash contra `ctr_ci_tokens`
- Valida expiração, ambientes permitidos, is_active
- Popula ClaimsPrincipal com serviceId do token

**Endpoints:**

```
POST   /contracts/ci-tokens
GET    /contracts/ci-tokens
DELETE /contracts/ci-tokens/{id}
```

**Actualizar `IActiveContractResolver`:**

Adicionar resolução por CiTokenId:
```csharp
if (context.CiTokenId is not null)
{
    var token = await _ciTokenRepo.GetByIdAsync(context.CiTokenId, ct);
    context = context with { ServiceSlug = null, /* usa token.ServiceId */ };
}
```

---

## Fase 4 — Changelog

**Objectivo:** Registar automaticamente uma linha do tempo auditável de eventos de contrato.

### Backend

**Nova entidade:**

```
Domain/Contracts/Entities/
  ContractChangelogEntry.cs
  Enums/ContractChangelogEventType.cs

Application/Contracts/Abstractions/
  IContractChangelogRepository.cs

Infrastructure/Contracts/Persistence/
  Repositories/ContractChangelogRepository.cs
  Configurations/ContractChangelogEntryConfiguration.cs
  Migrations/AddContractChangelog.cs
```

**Domain Events novos:**

```
Domain/Contracts/Events/
  ConformanceCheckCompletedDomainEvent.cs
```

**Event Handlers:**

```
Application/Contracts/EventHandlers/
  ContractPublishedChangelogHandler.cs
  BreakingChangeDetectedChangelogHandler.cs
  DraftApprovedChangelogHandler.cs
  DraftRejectedChangelogHandler.cs
  ContractDeprecatedChangelogHandler.cs
  ConformanceCheckResultChangelogHandler.cs
```

**Features CQRS:**

```
Application/Contracts/Features/
  GetContractChangelog/
  GetContractChangelogFeed/
```

**Job de retenção:**

```
Infrastructure/Contracts/Jobs/
  ContractChangelogRetentionJob.cs
```

**Endpoints:**

```
GET /contracts/{apiAssetId}/changelog
GET /contracts/changelog/feed
```

---

## Fase 5 — Webhook Delivery

**Objectivo:** Implementar a entrega real de eventos via webhook para consumers. Desbloqueia notificações externas.

### Backend

**Implementar `WebhookDeliveryService`** no módulo Integrations:

```
Integrations/Application/Services/
  WebhookDeliveryService.cs         — HTTP POST para subscriber endpoint
  WebhookPayloadBuilder.cs          — Serialização do payload por tipo de evento
  WebhookDeliveryRetryJob.cs        — Quartz job para retentativas

Integrations/Application/Features/
  DeliverWebhookEvent/
    DeliverWebhookEventCommand.cs
    DeliverWebhookEventCommandHandler.cs
```

**Integration Events relevantes a entregar:**

- `ContractPublishedIntegrationEvent`
- `BreakingChangeDetectedIntegrationEvent`
- `ContractDeprecatedIntegrationEvent`
- `ConformanceCheckFailedIntegrationEvent` (novo)

**Handler no módulo Integrations:**

```csharp
// Assina integration events do outbox e entrega via webhook
public class ContractEventWebhookHandler :
    IIntegrationEventHandler<ContractPublishedIntegrationEvent>,
    IIntegrationEventHandler<BreakingChangeDetectedIntegrationEvent>
```

---

## Fase 6 — Promotion Gate Extension

**Objectivo:** Exigir conformance check válido antes de promover entre ambientes.

### Backend

**Actualizar `EvaluateContractComplianceGate`:**

```csharp
// Adicionar verificação:
if (policy.RequireConformanceCheck)
{
    var latestCheck = await _conformanceRepo
        .GetLatestForServiceEnvironmentAsync(serviceId, sourceEnvId, ct);

    if (latestCheck is null || latestCheck.Status != ConformanceStatus.Compliant)
        return Result.Failure(ContractsErrors.ConformanceCheckRequired);

    var maxAge = TimeSpan.FromHours(policy.ConformanceMaxAgeHours);
    if (DateTime.UtcNow - latestCheck.CreatedAt > maxAge)
        return Result.Failure(ContractsErrors.ConformanceCheckStale);
}
```

---

## Fase 7 — Frontend — Conformance & Tokens

**Objectivo:** Dar visibilidade ao estado de conformance e permitir gestão de CI Tokens na UI.

### Frontend

**Nova tab no ContractWorkspacePage:**

```tsx
// features/contracts/workspace/tabs/ConformanceHistoryTab.tsx
// Mostra histórico de conformance checks com status, score, desvios e links
```

**Badge de conformance no ContractCatalogPage:**

```tsx
// Badge por ambiente: ✓ Compliant | ⚠ Drifted | ✗ Breaking | — No check
```

**Nova página de CI Tokens:**

```tsx
// features/contracts/ci-tokens/CiTokensPage.tsx
// /contracts/ci-tokens
// Lista tokens, cria novo, revoga, mostra prefixo e último uso
```

**Widget "Conformance Status" no Service Detail Page:**

```tsx
// features/services/detail/widgets/ContractConformanceWidget.tsx
// Status por ambiente com link para histórico
```

---

## Fase 8 — Frontend — Changelog

**Objectivo:** Expor a linha do tempo de eventos de contrato na UI.

### Frontend

**Nova tab "Changelog" no ContractWorkspacePage:**

```tsx
// features/contracts/workspace/tabs/ContractChangelogTab.tsx
// Timeline visual com filtros por tipo de evento e breaking
```

**Widget "Recent Contract Events" no dashboard de Team Lead:**

```tsx
// features/dashboard/widgets/ContractChangelogFeedWidget.tsx
// Últimos N eventos de contratos da equipa
```

---

## Fases 9 e 10 — Funcionalidades Avançadas

### Fase 9 — Notificações Avançadas

- Notificação proactiva N dias antes do sunset de um contrato
- Consumer acceptance workflow (consumer faz acknowledge de breaking change)
- `ConsumerExpectation` com lógica de validação real

### Fase 10 — Runtime Drift → Changelog

- `DetectContractDrift` gera alertas + entradas de changelog automaticamente
- Correlação drift runtime ↔ ConformanceCheck CI na UI
- Widget de drift no dashboard operacional

---

## Dependências entre fases

```
F1 (Core Engine)
  └─ F2 (Políticas) — F1 usa policy hardcoded até F2 estar pronta
       └─ F3 (CI Token) — F3 actualiza o resolver da F1
            └─ F4 (Changelog) — depende de domain events da F1
                 └─ F5 (Webhook) — depende de integration events da F4
                      └─ F6 (Promotion Gate) — usa repositório da F1
                           └─ F7 (Frontend Conformance) — consome endpoints de F1+F3
                                └─ F8 (Frontend Changelog) — consome endpoints de F4
                                     └─ F9 (Notificações avançadas)
                                          └─ F10 (Runtime Drift)
```

---

## Ficheiros de migração (ordem)

```
AddContractConformanceChecks          — Fase 1
AddContractCiTokens                   — Fase 3
AddContractChangelog                  — Fase 4
SeedContractConformanceConfiguration  — Fase 2 (data migration)
```

---

## Critérios de aceitação por fase

| Fase | Critério mínimo |
|------|----------------|
| F1 | Pipeline CI consegue chamar endpoint e receber resultado estruturado |
| F2 | Admin pode configurar política por tenant/ambiente sem redeploy |
| F3 | Team owner cria token, pipeline usa token sem GUID manual |
| F4 | Publicar um contrato gera entrada de changelog; check CI gera entrada |
| F5 | Webhook é entregue ao consumer quando breaking change é publicado |
| F6 | Promoção PRE→PROD falha se não existir conformance check recente |
| F7 | Engineer vê estado de conformance por ambiente na UI |
| F8 | Team Lead vê timeline de eventos de contrato na UI |
