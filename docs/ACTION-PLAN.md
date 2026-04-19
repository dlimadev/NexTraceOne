# NexTraceOne — Plano de Ação para Resolução de Gaps

> **Data de criação:** Abril 2026  
> **Baseado em:** Análise completa do estado do projeto (Abril 2026)  
> **Referência:** [IMPLEMENTATION-STATUS.md](./IMPLEMENTATION-STATUS.md) · [FUTURE-ROADMAP.md](./FUTURE-ROADMAP.md)

---

## Sumário Executivo

O NexTraceOne encontra-se num estado de maturidade elevado (~98% implementado). Os gaps identificados dividem-se em 4 categorias:

1. **Bugs / TODOs ativos em código de produção** — impacto imediato
2. **Stubs / dados sintéticos sem persistência** — funcionalidades visivelmente incompletas para o utilizador final
3. **Integrações externas pendentes** — features que requerem infraestrutura ou serviços externos
4. **Gaps de qualidade** — EF Core Designer files, cobertura de testes, documentação

Este plano define a ordem de resolução recomendada, o esforço estimado por item e a abordagem técnica para cada gap.

---

## Prioridade 1 — Bugs e TODOs Críticos em Produção

> **Critério:** Comportamento incorreto ou enganoso visível ao utilizador; resolve-se com uma mudança cirúrgica no código existente.

---

### ACT-001 — PromotionPage: `serviceName` usa `releaseId` como fallback incorreto ✅ RESOLVIDO (Abril 2026)

**Ficheiros:** `src/frontend/src/features/change-governance/pages/PromotionPage.tsx`  
**Linhas:** 147–148, 174–175  
**Impacto:** O gate de FinOps (`EvaluateReleaseBudgetGate`) recebe `serviceName = req.releaseId` em vez do nome real do serviço. O gate avalia budget pelo `serviceName` — com um UUID, nunca encontra configuração de budget, resultando em avaliações incorretas ou em fallback que podem bloquear ou permitir promoções indevidamente.

**Causa raiz:** O formulário de promoção (`PromotionRequest`) só armazena `releaseId`. O backend não inclui `serviceName` na resposta de `PromotionRequest`.

**Solução:**

1. **Backend** — adicionar o campo `ServiceName` ao DTO de resposta de `ListPromotionRequests` / `GetPromotionRequest` em `ChangeGovernance`. O handler deve enriquecer com o `serviceName` do `Release` associado via `IChangeIntelligenceReader`.

2. **Frontend** — no `PromotionPage.tsx`, substituir:
   ```ts
   // Antes (incorreto)
   serviceName: req.releaseId,
   
   // Depois (correto)
   serviceName: req.serviceName ?? req.releaseId,
   ```
   Aguardar até o backend expor o campo, ou introduzir lookup separado via `getReleaseById`.

3. **Testes** — adicionar caso de teste em `PromotionPage.test.tsx` que verifica que `serviceName` no payload do gate é diferente do `releaseId`.

**Esforço estimado:** Médio (3–5h backend + 1h frontend + 1h testes)

---

### ACT-002 — PromotionPage: custos com placeholder zero no gate FinOps ✅ RESOLVIDO (Abril 2026)

**Ficheiros:** `src/frontend/src/features/change-governance/pages/PromotionPage.tsx`  
**Linhas:** 150–151  
**Impacto:** `actualCostPerDay: 0` e `baselineCostPerDay: 0` são enviados ao endpoint `EvaluateReleaseBudgetGate`. Com custo zero, o gate de budget nunca pode distinguir uma regressão de custo real de uma release normal — o gate fica efetivamente inativo para comparação de custo real.

**Causa raiz:** Os dados de custo real deveriam vir do contexto FinOps da release (telemetria / `ICostIntelligenceModule`), mas o frontend não os carrega antes de avaliar o gate.

**Solução:**

1. **Backend** — expor endpoint `GET /api/v1/finops/service/{serviceName}/cost-context?environment={env}` que retorne `actualCostPerDay` e `baselineCostPerDay` para um serviço num ambiente. Este handler deve delegar para `ICostIntelligenceModule.GetCurrentCostContextAsync()`.

2. **Frontend** — no `PromotionPage.tsx`, antes de chamar `evaluateReleaseBudgetGate`, fazer lookup do contexto de custo e passar os valores reais. Enquanto o lookup estiver em progresso, mostrar estado de carregamento no botão de promoção.

3. **Testes** — adicionar teste de integração que verifica que `actualCostPerDay` no payload do gate não é zero quando existem dados de custo.

**Esforço estimado:** Alto (6–8h backend + 2h frontend + 2h testes)

---

### ACT-003 — Export endpoint retorna 501 Not Implemented ✅ RESOLVIDO (Abril 2026)

**Ficheiros:** `src/modules/configuration/NexTraceOne.Configuration.API/Endpoints/ExportEndpointModule.cs`  
**Impacto:** Qualquer fluxo de exportação (relatórios, auditoria, contratos) retorna 501. A feature está visível no produto mas completamente não funcional.

**Solução mínima viável (MVP):**

1. Implementar exportação CSV síncronos para as entidades mais críticas: `audit_events`, `contracts`, `releases`.

2. Handler inicial (sem Quartz job): o endpoint gera o ficheiro em memória e retorna como `application/octet-stream`. Adequado para datasets até ~10MB.

3. Estrutura base:
   ```csharp
   // ExportEndpointModule.cs
   group.MapPost("/", async (ExportRequest request, ISender mediator, CancellationToken ct) =>
   {
       var result = await mediator.Send(new ExportData.Command(request.Entity, request.Format, request.Columns), ct);
       if (result.IsFailure) return Results.BadRequest(result.Error);
       return Results.File(result.Value.Content, result.Value.ContentType, result.Value.FileName);
   });
   ```

4. Handlers a criar: `ExportData.Command` em `Configuration.Application` delegando para repositórios por entidade.

5. **Fase 2 (roadmap):** substituir por geração assíncrona via Quartz job com notificação ao utilizador.

**Esforço estimado:** Alto (8–12h backend para CSV/JSON básicos)

---

### ACT-004 — SAML SSO: UpdateSamlSsoConfig não persiste alterações ✅ RESOLVIDO (Abril 2026)

**Ficheiros:** `src/modules/governance/NexTraceOne.Governance.Application/Features/GetSamlSsoConfig/GetSamlSsoConfig.cs`  
**Impacto:** Quando o Platform Admin configura SAML SSO via UI (`SamlSsoPage`), as alterações são perdidas ao reiniciar a aplicação porque `UpdateSamlSsoConfig` apenas faz echo do request sem persistir em base de dados.

**Causa raiz:** O handler `UpdateHandler` lê `IConfiguration` mas não escreve em nenhum repositório ou tabela.

**Solução:**

1. **Domain** — criar entidade `SamlSsoConfiguration` no módulo `IdentityAccess.Domain` com campos: `EntityId`, `SsoUrl`, `SloUrl`, `IdpCertificate`, `JitProvisioningEnabled`, `DefaultRole`, `AttributeMappings`, `TenantId`.

2. **Infrastructure** — criar `ISamlSsoConfigurationRepository` e implementação EF Core em `IdentityAccess.Infrastructure`. Adicionar nova migration ao `IdentityDbContext` com tabela `iam_saml_sso_configurations`.

3. **Application** — mover `GetSamlSsoConfig` e `UpdateSamlSsoConfig` do módulo Governance para o módulo IdentityAccess (bounded context correto). O handler de update passa a chamar `repository.UpsertAsync(config, ct)`.

4. **Fallback** — manter leitura de `IConfiguration` como fallback se nenhuma configuração estiver em base de dados.

5. **Testes** — adicionar testes unitários para `UpdateSamlSsoConfig` com repositório mock + teste de integração PostgreSQL.

**Esforço estimado:** Médio-Alto (6–8h backend + 1h migration + 2h testes)

---

## Prioridade 2 — Stubs com Dados Sintéticos (Integração com Plataforma Interna)

> **Critério:** A feature depende de dados já existentes na plataforma (observabilidade, custo, ambientes) mas ainda não está ligada ao pipeline correto. Solução é interna — não requer serviços cloud externos.

---

### ACT-005 — GetRightsizingReport: ligar ao ICostIntelligenceModule ✅ RESOLVIDO (Abril 2026)

**Ficheiros:** `src/modules/governance/NexTraceOne.Governance.Application/Features/GetRightsizingReport/GetRightsizingReport.cs`  
**Estado atual:** Retorna lista vazia com `SimulatedNote`.  
**Dados disponíveis:** `CostIntelligenceDbContext` já persiste `CostAttribution`, `CostRecord` e `BudgetForecast` por serviço.

**Solução:**

1. Injetar `ICostIntelligenceModule` no `Handler`.
2. Chamar `ICostIntelligenceModule.GetCostAttributionsAsync()` para listar serviços com dados de custo.
3. Calcular `currentSpec` vs `recommendedSpec` com base em limiares configuráveis: se `CostRecord.CpuUsagePct < 20%` → recomendar downgrade; se `MemoryUsagePct > 80%` → recomendar upgrade.
4. Remover `SimulatedNote` quando `Recommendations.Count > 0`.
5. Adicionar testes unitários com `ICostIntelligenceModule` mockado.

**Esforço estimado:** Médio (4–6h)

---

### ACT-006 — GetCapacityForecast: ligar ao ITelemetryQueryService ✅ RESOLVIDO (Abril 2026)

**Ficheiros:** `src/modules/governance/NexTraceOne.Governance.Application/Features/GetCapacityForecast/GetCapacityForecast.cs`  
**Estado atual:** Valores fixos hardcoded (CPU 42%, Memory 61%, etc.).  
**Dados disponíveis:** `ITelemetryQueryService` e `ObservabilityProfile` em `RuntimeIntelligenceDbContext` têm snapshots históricos de CPU/memória.

**Solução:**

1. Injetar `IRuntimeIntelligenceModule` no `Handler`.
2. Ler os últimos N snapshots de `RuntimeBaseline` por serviço/ambiente.
3. Calcular tendência de crescimento linear simples (regressão dos últimos 4 semanas).
4. Expor `ForecastedUsagePct` com base na extrapolação a 30 dias.
5. Definir limiares de risco: Low < 60%, Medium 60–80%, High 80–90%, Critical > 90%.
6. Remover `SimulatedNote` quando dados reais estiverem disponíveis.

**Esforço estimado:** Médio (5–7h)

---

### ACT-007 — GetNonProdSchedules: persistir agendas em base de dados ✅ RESOLVIDO (Abril 2026)

**Ficheiros:** `src/modules/governance/NexTraceOne.Governance.Application/Features/GetNonProdSchedules/GetNonProdSchedules.cs`  
**Estado atual:** Retorna lista hardcoded em memória com ambientes "staging" e "qa" fixos.

**Solução:**

1. **Domain** — criar entidade `NonProdSchedule` em `Governance.Domain` com campos: `EnvironmentId`, `EnvironmentName`, `Enabled`, `ActiveDaysOfWeek`, `ActiveFromHour`, `ActiveToHour`, `Timezone`, `Override`, `EstimatedSavingPct`.

2. **Infrastructure** — criar `INonProdScheduleRepository` e tabela `gov_nonprod_schedules` via nova migration em `GovernanceDbContext`.

3. **Handler** — substituir lista hardcoded por `await repository.ListAllAsync(ct)`. Seed inicial pode inserir os dois ambientes existentes.

4. **UpdateNonProdSchedule** — passa a persistir via repositório em vez de operar em memória.

**Esforço estimado:** Médio (5–7h backend + 1h migration)

---

### ACT-008 — GetDemoSeedStatus: substituir static field por persistência real ✅ RESOLVIDO (Abril 2026)

**Ficheiros:** `src/modules/governance/NexTraceOne.Governance.Application/Features/GetDemoSeedStatus/GetDemoSeedStatus.cs`  
**Estado atual:** `_state`, `_seededAt`, `_entitiesCount` são campos estáticos em memória — perdem estado ao reiniciar.  
**Impacto:** O Platform Admin não consegue saber se a demo foi executada numa instância diferente (ex: load balancer, após restart).

**Solução:**

1. Criar tabela `gov_demo_seed_state` via migration com colunas: `state`, `seeded_at`, `entities_count`, `tenant_id`.
2. Criar `IDemoSeedStateRepository` e `EfDemoSeedStateRepository`.
3. Substituir campos estáticos por `await repository.GetOrDefaultAsync(ct)` e `await repository.UpsertAsync(state, ct)`.
4. Implementar `RunDemoSeed` com lógica real: inserir entidades de demonstração via Unit of Work usando `ConfigurationDefinitionSeeder` como referência.

**Esforço estimado:** Médio (4–6h)

---

### ACT-009 — SearchCatalog: documentar como READY (já implementado) ✅ RESOLVIDO (Abril 2026)

**Ficheiros:** `src/modules/catalog/NexTraceOne.Catalog.Application/Portal/Features/SearchCatalog/SearchCatalog.cs`  
**Estado atual:** Marcado como "stub intencional" na documentação — mas a análise do código revela que o handler **já está implementado** com PostgreSQL FTS/LIKE, combinando contratos e serviços com facetas.

**Ação necessária:**

1. Remover menção de "stub" do `IMPLEMENTATION-STATUS.md`.
2. Atualizar status de `SearchCatalog` de `PARTIAL` para `READY` na secção Developer Portal.
3. Verificar se `IServiceAssetRepository.SearchAsync()` tem índice GIN no campo `Name` para performance.
4. Adicionar testes unitários para `SearchCatalog.Handler` (actualmente ausentes).

**Esforço estimado:** Baixo (1–2h)

---

## Prioridade 3 — Integrações Externas Pendentes

> **Critério:** Requerem infraestrutura externa (Elasticsearch, Kafka, PKI, cloud billing) ou protocolo de terceiros (SAML, cloud APIs). Dependência de decisão de produto ou infraestrutura antes de implementar.

---

### ACT-010 — GetExternalHttpAudit: ligar ao Elasticsearch ✅ RESOLVIDO (Abril 2026)

**Solução implementada:**

1. Criada interface `IHttpAuditReader` em `Governance.Application/Abstractions` com `QueryAsync(filter, ct)` e DTOs `HttpAuditFilter`, `HttpAuditPage`, `HttpAuditEntry`.
2. Implementado `ObservabilityHttpAuditReader` em `Governance.Infrastructure/Observability` que usa `IObservabilityProvider.IsHealthyAsync()` + `QueryTracesAsync(filter, ct)` com `ServiceKind=REST`.
3. Handler `GetExternalHttpAudit.Handler` agora injeta `IHttpAuditReader`; retorna `SimulatedNote` quando provider não está disponível (fallback gracioso), sem nota quando há dados reais.
4. Registado `services.AddScoped<IHttpAuditReader, ObservabilityHttpAuditReader>()` em `GovernanceInfrastructure.DependencyInjection`.
5. Adicionada referência `BuildingBlocks.Observability` ao `Governance.Infrastructure.csproj`.
6. Testes: 13 novos testes em `HttpAuditAndSupportBundleTests.cs`.

**Ficheiros afetados:**
- `Governance.Application/Abstractions/IHttpAuditReader.cs` (novo)
- `Governance.Application/Features/GetExternalHttpAudit/GetExternalHttpAudit.cs` (actualizado)
- `Governance.Infrastructure/Observability/ObservabilityHttpAuditReader.cs` (novo)
- `Governance.Infrastructure/DependencyInjection.cs` (actualizado)
- `Governance.Infrastructure/NexTraceOne.Governance.Infrastructure.csproj` (referência adicionada)

---

### ACT-011 — GetCanaryRollouts: integração com sistema de canary externo ✅ RESOLVIDO (Abril 2026)

**Ficheiros:** `src/modules/governance/NexTraceOne.Governance.Application/Features/GetCanaryRollouts/GetCanaryRollouts.cs`  
**Dependência:** Existência de sistema de canary deployment (Argo Rollouts, Flagger, LaunchDarkly, ou similar).

**Solução:**

1. Criar interface `ICanaryProvider` em `Integrations.Domain` com método `GetActiveRolloutsAsync(environment, ct)`.
2. Criar implementação `NullCanaryProvider` (retorna lista vazia — comportamento atual) como default.
3. Criar implementação `ArgoRolloutsProvider` ou `FlaggerProvider` quando a integração for decidida.
4. Registar o provider via feature flag `integrations.canary.provider` no sistema de configuração existente.
5. Injetar `ICanaryProvider` no handler de `GetCanaryRollouts`.

**Pré-requisito:** Decisão sobre qual sistema de canary deployment usar.  
**Esforço estimado:** Médio (4–6h para abstração + provider específico a definir)

---

### ACT-012 — GetGreenOpsReport: calcular emissões com dados de telemetria reais ✅ RESOLVIDO (Abril 2026)

**Ficheiros:** `src/modules/governance/NexTraceOne.Governance.Application/Features/GetGreenOpsReport/GetGreenOpsReport.cs`  
**Dependência:** Dados de CPU/memória disponíveis via `IRuntimeIntelligenceModule` ou `ITelemetryQueryService`.

**Solução:**

1. Injetar `IRuntimeIntelligenceModule` no `Handler`.
2. Agregar consumo de recursos dos últimos 30 dias por serviço: `total_kwh = (avg_cpu_cores * power_per_core + avg_ram_gb * power_per_gb) * hours`.
3. Calcular emissões: `kg_co2 = total_kwh * intensity_factor_kg_per_kwh` (do `IConfiguration`).
4. Construir `TopServices` com os 5 serviços com maior emissão.
5. Calcular `Trend` comparando com mês anterior.
6. `UpdateConfigHandler` persiste `IntensityFactor` e `EsgTarget` em base de dados (criar tabela `gov_greenops_config`).

**Pré-requisito:** `RuntimeIntelligenceDbContext` com snapshots históricos populados.  
**Esforço estimado:** Alto (8–10h)

---

### ACT-013 — GetSupportBundles: geração real de bundles de suporte ✅ RESOLVIDO (Abril 2026)

**Solução implementada:**

1. Entidade `SupportBundle` com ID fortemente tipado, estados Pending/Generating/Ready/Failed, `ZipContent` (byte[]) inline.
2. Migration `20260419130000_AddSupportBundles` e tabela `gov_support_bundles` no PostgreSQL.
3. `ISupportBundleRepository` com ListAsync/GetByIdAsync/AddAsync/Update + implementação EF `SupportBundleRepository`.
4. Handler `GetSupportBundles.Handler` agora lê do repositório real e calcula `download_url`.
5. Handler `GetSupportBundles.GenerateHandler` gera ZIP real em memória (System.IO.Compression) com: platform-summary.json, config-summary.json (sanitizado, sem segredos), governance-summary.json (teams, domains, packs).
6. Novo handler `GetSupportBundles.DownloadHandler` (query `DownloadBundle`) para download de ficheiro ZIP.
7. Endpoint `GET /platform/support-bundles/{bundleId:guid}/download` adicionado em `PlatformAdminEndpointModule`.
8. Testes: 10 testes adicionados em `HttpAuditAndSupportBundleTests.cs`.

**Ficheiros afetados:**
- `Governance.Domain/Entities/SupportBundle.cs` (novo)
- `Governance.Application/Abstractions/ISupportBundleRepository.cs` (novo)
- `Governance.Application/Features/GetSupportBundles/GetSupportBundles.cs` (actualizado)
- `Governance.Infrastructure/Persistence/Repositories/SupportBundleRepository.cs` (novo)
- `Governance.Infrastructure/Persistence/Configurations/SupportBundleEntityConfiguration.cs` (novo)
- `Governance.Infrastructure/Persistence/Migrations/20260419130000_AddSupportBundles.cs` (novo)
- `Governance.Infrastructure/Persistence/GovernanceDbContext.cs` (DbSet adicionado)
- `Governance.Infrastructure/DependencyInjection.cs` (registo adicionado)
- `Governance.API/Endpoints/PlatformAdminEndpointModule.cs` (endpoint download adicionado)

---

### ACT-014 — GetRestorePoints: integração com sistema de backup ✅ RESOLVIDO (Abril 2026)

**Ficheiros:** `src/modules/governance/NexTraceOne.Governance.Application/Features/GetRestorePoints/GetRestorePoints.cs`  
**Dependência:** Sistema de backup (pg_dump agendado, pgBackRest, Barman, ou similar).

**Solução:**

1. Criar interface `IBackupProvider` em `Integrations.Domain` com método `ListRestorePointsAsync(ct)` e `InitiateRecoveryAsync(restorePointId, scope, dryRun, ct)`.
2. Criar `NullBackupProvider` (retorna lista vazia) como default registado via DI.
3. Criar `PgDumpBackupProvider` ou `PgBackRestProvider` quando infraestrutura de backup for definida.
4. Registar provider via feature flag `platform.backup.provider`.
5. `InitiateRecovery` deve criar job de recovery auditado em `gov_support_bundles` ou tabela dedicada.

**Pré-requisito:** Decisão sobre estratégia de backup para ambientes on-prem.  
**Esforço estimado:** Médio (5–7h abstração + provider específico)

---

### ACT-015 — SAML 2.0 Protocol Handlers completos

**Ficheiros:** `src/modules/identityaccess/NexTraceOne.IdentityAccess.Infrastructure/`, `src/modules/identityaccess/NexTraceOne.IdentityAccess.Application/Features/FederatedLogin/`  
**Estado atual:** `FederatedLogin` aceita tokens OIDC genéricos; domínio SAML (entidades `SsoGroupMapping`, `ExternalIdentity`) existe mas não tem protocolo SAML 2.0 implementado.

**Solução:**

1. Adicionar pacote `ITfoxtec.Identity.Saml2` (OSS, license MIT) ao projeto `IdentityAccess.Infrastructure`.
2. Criar `SamlAuthController` com endpoints: `GET /auth/saml/sso` (redirect para IdP) e `POST /auth/saml/acs` (Assertion Consumer Service).
3. `ACS Handler` extrai `NameID`, `email`, `groups` do assertion SAML assinado, valida assinatura com `IdpCertificate` da configuração.
4. Chamar `FederatedLogin.Command` com `Provider = "saml"` e dados extraídos.
5. Mapear `SsoGroupMapping` para roles internas.
6. Ligar `SamlSsoConfiguration` (criada em ACT-004) para fornecer `EntityId`, `SsoUrl`, `SloUrl`, `IdpCertificate` dinamicamente.

**Dependência:** ACT-004 concluído primeiro.  
**Esforço estimado:** Muito Alto (16–24h)

---

### ACT-016 — Kafka Real Producer/Consumer

**Ficheiros:** `src/modules/integrations/`, `src/platform/NexTraceOne.BackgroundWorkers/`  
**Estado atual parcial (Abril 2026):** `IKafkaEventProducer` interface criada em `Integrations.Domain` com `KafkaMessage` record. `NullKafkaEventProducer` implementado em `Integrations.Infrastructure/Kafka/` e registado via DI como singleton. Testes unitários: 6/6 passam.

**Pendente (requer Kafka cluster):**

1. Criar `ConfluentKafkaEventProducer` em pacote `NexTraceOne.Integrations.Kafka` usando `Confluent.Kafka` NuGet.
2. `KafkaEventConsumer` (BackgroundService) consome tópicos configurados e chama `ProcessIngestionPayload`.
3. Configurar topics via `IConfiguration "Kafka:Topics:*"`.
4. Feature flag `integrations.kafka.enabled` controla ativação.

**Pré-requisito:** Cluster Kafka disponível (dev: Docker Compose, prod: MSK/Confluent Cloud/self-hosted).  
**Esforço restante:** Médio (6–10h)

---

### ACT-017 — FinOps com dados de cloud billing reais (AWS/Azure/GCP)

**Ficheiros:** `src/modules/operationalintelligence/` (CostIntelligence), `src/modules/governance/` (FinOps)  
**Estado atual:** `CostIntelligenceDbContext` persiste custo mas os dados são inseridos manualmente ou via seed.

**Solução:**

1. Criar interface `ICloudBillingProvider` em `Integrations.Domain`.
2. Criar implementações: `AwsCostExplorerProvider`, `AzureCostManagementProvider`, `GcpBillingProvider`.
3. Registar via feature flag `finops.billing.provider` (valores: `aws`, `azure`, `gcp`, `manual`).
4. Quartz job `CloudBillingIngestionJob` executa diariamente: chama provider, normaliza para `CostRecord` domain entity, persiste via `ICostIntelligenceModule`.
5. Mapear custos cloud por `resource_tag` → `service_name` com regras configuráveis.

**Pré-requisito:** Credenciais cloud + decisão sobre qual cloud provider suportar primeiro.  
**Esforço estimado:** Muito Alto (20–30h por provider)

---

## Prioridade 4 — Gaps de Qualidade e Tooling

> **Critério:** Não afetam funcionalidades visíveis ao utilizador, mas afetam developer experience, manutenibilidade e operações futuras.

---

### ACT-018 — Regenerar 13 EF Core Designer Files ausentes

**Contexto:** 13 migrações não têm ficheiro `.Designer.cs`. Impede uso de algumas ferramentas `dotnet ef` em CI.

**Módulos afectados:**
- AuditCompliance: `P7_4_AuditCorrelationId`
- IdentityAccess: `AddTenantOrganizationFields`
- Catalog: `P52B_DeveloperSurveys`, `W04_LegacyContractGovernance`, `AddCatalogSearchGinIndexes`, `P52_DeveloperExperienceScore`
- Configuration: `AddPhase3To8ConfigurationTables`
- OperationalIntelligence: `P51_PredictiveIntelligence`, `W01_TelemetryStoreFoundation`, `AddCustomCharts`, `AddChaosExperiments`
- AIKnowledge: `P05_Innovation`, `P04_BackendEnhancements`

**Solução:** Em ambiente local com PostgreSQL ativo:
```bash
# Para cada migration sem Designer file:
dotnet ef migrations script <PreviousMigration> <TargetMigration> \
  --project <InfraProject> \
  --startup-project src/platform/NexTraceOne.ApiHost \
  --idempotent
```
Ou regenerar com:
```bash
dotnet ef database update --project <InfraProject> --startup-project src/platform/NexTraceOne.ApiHost
```

**Alternativa (sem PostgreSQL local):** Adicionar step no CI com `Testcontainers.PostgreSql` que executa `dotnet ef migrations bundle` e valida que todas as migrations aplicam sem erro.

**Esforço estimado:** Baixo-Médio (2–4h com PostgreSQL local disponível)

---

### ACT-019 — Implementar GraphQL Contract Type (parser + visual builder) ✅ RESOLVIDO (Abril 2026)

**Ficheiros:** `src/modules/catalog/NexTraceOne.Catalog.Domain/Contracts/Services/GraphQlSpecParser.cs`, `GraphQlDiffCalculator.cs`, `ContractDiffCalculator.cs`  
**Estado:** `GraphQlSpecParser` extrai tipos, campos, interfaces, inputs e enums via SDL text analysis. `GraphQlDiffCalculator` implementa breaking changes (type/field/enum removed, root type fields), additive (type/field/enum added) e non-breaking (enum value added). `ContractDiffCalculator` agora delega a `GraphQlDiffCalculator` para `ContractProtocol.GraphQl`. 8 testes unitários passam.

**Pendente (frontend):** `VisualGraphQlBuilder.tsx` — formulário SDL com syntax highlighting. Estimado: 6–8h frontend.

---

### ACT-020 — Implementar Protobuf/gRPC Contract Type (parser + visual builder) ✅ RESOLVIDO (Abril 2026)

**Ficheiros:** `src/modules/catalog/NexTraceOne.Catalog.Domain/Contracts/Services/ProtobufSpecParser.cs`, `ProtobufDiffCalculator.cs`, `ContractDiffCalculator.cs`  
**Estado:** `ProtobufSpecParser` extrai messages (com field names + field numbers), services, RPCs e enums via .proto text analysis. `ProtobufDiffCalculator` implementa breaking changes (message/service/rpc removed, field removed, field number reused, enum value removed), additive e non-breaking. `ContractDiffCalculator` agora delega a `ProtobufDiffCalculator` para `ContractProtocol.Protobuf`. 10 testes unitários passam.

**Pendente (frontend):** `VisualProtobufBuilder.tsx` — formulário .proto com preview de services/messages. Estimado: 6–8h frontend.

---

### ACT-021 — Adicionar testes unitários ao SearchCatalog.Handler ✅ RESOLVIDO (Abril 2026)

**Ficheiros:** `tests/modules/catalog/NexTraceOne.Catalog.Tests/`  
**Estado atual:** `SearchCatalog.Handler` implementado mas sem testes dedicados.

**Solução:**

Criar `SearchCatalogTests.cs` com casos:
1. Query com termo válido → retorna contratos e serviços combinados
2. Query com `TypeFilter = "OpenApi"` → filtra apenas contratos OpenAPI
3. Query com `StatusFilter = "Published"` → filtra apenas contratos publicados
4. Query vazia ou com termo < 2 chars → retorna `ValidationError`
5. Nenhum resultado → retorna lista vazia com `TotalCount = 0`

**Esforço estimado:** Baixo (2–3h)

---

### ACT-022 — Testes E2E para fluxos de SAML SSO (após ACT-015)

**Ficheiros:** `tests/platform/NexTraceOne.E2E.Tests/Flows/`  
**Dependência:** ACT-015 concluído.

**Solução:**

Criar `SamlSsoFlowTests.cs` com:
1. `GET /auth/saml/sso` → redireciona para `SsoUrl` configurado
2. `POST /auth/saml/acs` com assertion válido → retorna JWT
3. `POST /auth/saml/acs` com assinatura inválida → retorna 401
4. Login federado com mapeamento de grupos → utilizador recebe roles corretos

**Esforço estimado:** Médio (4–6h)

---

### ACT-023 — Testes de integração para Export endpoint (após ACT-003)

**Ficheiros:** `tests/platform/NexTraceOne.IntegrationTests/CriticalFlows/`  
**Dependência:** ACT-003 concluído.

**Solução:**

Adicionar ao `CoreApiHostIntegrationTests.cs`:
1. `POST /api/v1/export` com `entity = "contracts"` → retorna 200 com `Content-Type: text/csv`
2. `POST /api/v1/export` com `entity = "audit_events"` → retorna 200 com CSV
3. `POST /api/v1/export` com `format = "json"` → retorna 200 com JSON
4. `POST /api/v1/export` com entidade inválida → retorna 400

**Esforço estimado:** Baixo-Médio (2–4h)

---

### ACT-024 — Documentação: OpenAPI export como artefacto de build

**Contexto:** O produto tem a Ingestion API e a API principal sem documentação pública exportada como ficheiro estático para consumidores externos (CI/CD pipelines, ITSM).

**Solução:**

1. Adicionar `Swashbuckle.AspNetCore` (se ainda não presente) ou usar `Microsoft.AspNetCore.OpenApi` (.NET 9+).
2. Configurar geração de `openapi.json` em tempo de build via `dotnet publish` ou `MSBuild target`.
3. Criar script `scripts/export-openapi.sh` que executa o ApiHost em modo headless e exporta `/openapi.json`.
4. Adicionar ao `docker-compose.yml` um target de documentação.
5. Publicar `openapi.json` como artifact no CI/CD.

**Esforço estimado:** Baixo-Médio (2–4h)

---

### ACT-025 — Elasticsearch: ativar segurança em docker-compose para staging

**Ficheiros:** `docker-compose.yml`, `docker-compose.production.yml`  
**Contexto:** `xpack.security.enabled: "false"` está comentado como "apenas para dev local", mas sem garantia de que não seja usado em staging.

**Solução:**

1. Criar `docker-compose.staging.yml` explícito com `xpack.security.enabled: "true"` e credenciais via `.env`.
2. Adicionar ao `README.md` instrução clara: "Nunca usar `docker-compose.yml` base em produção ou staging sem override de segurança".
3. Adicionar validação no startup do `ApiHost`: se `ASPNETCORE_ENVIRONMENT != Development` e Elasticsearch estiver configurado sem autenticação → log warning crítico.

**Esforço estimado:** Baixo (1–2h)

---

## Matriz de Priorização

| ID | Título | Prioridade | Esforço | Dependências | Impacto |
|---|---|---|---|---|---|
| ACT-001 | PromotionPage: serviceName incorreto | ✅ DONE | Médio | — | Bug visível em FinOps gate |
| ACT-002 | PromotionPage: custos placeholder zero | ✅ DONE | Alto | ACT-001 | Gate de budget inativo |
| ACT-003 | Export endpoint 501 | ✅ DONE | Alto | — | Feature completamente inativa |
| ACT-004 | SAML write-through não persiste | ✅ DONE | Médio-Alto | — | Configuração SSO perdida após restart |
| ACT-005 | Rightsizing ligar a ICostIntelligenceModule | ✅ DONE | Médio | — | Dados reais de rightsizing |
| ACT-006 | Capacity Forecast ligar a IRuntimeIntelligenceModule | ✅ DONE | Médio | — | Forecast baseado em dados reais |
| ACT-007 | NonProdSchedules persistir em BD | ✅ DONE | Médio | — | Agendas não perdidas após restart |
| ACT-008 | DemoSeedStatus substituir campo estático | ✅ DONE | Médio | — | Estado persistente em multi-instância |
| ACT-009 | SearchCatalog documentar como READY | ✅ DONE | Baixo | — | Correcção de documentação |
| ACT-010 | ExternalHttpAudit ligar Elasticsearch | ✅ DONE | Alto | ES ativo | Auditoria HTTP real |
| ACT-011 | CanaryRollouts integração canary externo | ✅ DONE | Médio | Sistema canary | Dashboard canary real |
| ACT-012 | GreenOps calcular de telemetria real | ✅ DONE | Alto | Telemetria histórica | ESG reporting real |
| ACT-013 | SupportBundles geração real | ✅ DONE | Alto | Storage decision | Support bundles funcionais |
| ACT-014 | RestorePoints integração backup | ✅ DONE | Médio | Backup system | Recovery operacional |
| ACT-015 | SAML 2.0 Protocol Handlers | 🟠 P3 | Muito Alto | ACT-004 | SSO enterprise completo |
| ACT-016 | Kafka Producer/Consumer real | 🟠 P3 | Alto | Kafka cluster | Event streaming real |
| ACT-017 | FinOps cloud billing real | 🟠 P3 | Muito Alto | Cloud credentials | FinOps com dados cloud reais |
| ACT-018 | Regenerar EF Core Designer files | 🟢 P4 | Baixo | PostgreSQL local | Developer tooling |
| ACT-019 | GraphQL Contract Type | ✅ DONE | Alto | — | Suporte a GraphQL contracts |
| ACT-020 | Protobuf Contract Type | ✅ DONE | Alto | — | Suporte a gRPC contracts |
| ACT-021 | Testes SearchCatalog | ✅ DONE | Baixo | — | Cobertura de testes |
| ACT-022 | Testes E2E SAML | 🟢 P4 | Médio | ACT-015 | Validação SSO |
| ACT-023 | Testes Export Handler | ✅ DONE | Baixo | ACT-003 | Validação export (unit) |
| ACT-024 | OpenAPI export como artefacto | ✅ DONE | Baixo | — | Documentação pública |
| ACT-025 | Elasticsearch segurança staging | ✅ DONE | Baixo | — | Segurança staging |

---

## Ordem de Execução Recomendada

### Sprint 1 — Bugs Críticos (1–2 semanas)
1. ACT-001 — PromotionPage serviceName
2. ACT-004 — SAML write-through persistência
3. ACT-003 — Export endpoint MVP (CSV/JSON síncronos)
4. ACT-009 — SearchCatalog documentação correcção
5. ACT-025 — Elasticsearch segurança staging

### Sprint 2 — Dados Reais (2–3 semanas)
6. ACT-002 — PromotionPage custos reais
7. ACT-005 — Rightsizing ligar ICostIntelligenceModule
8. ACT-006 — Capacity Forecast ligar IRuntimeIntelligenceModule
9. ACT-007 — NonProdSchedules persistência BD
10. ACT-008 — DemoSeedStatus persistência BD

### Sprint 3 — Integrações Externas (3–6 semanas, ordem por prioridade de negócio)
11. ACT-010 — ExternalHttpAudit (se Elasticsearch disponível)
12. ACT-011 — CanaryRollouts (se sistema canary definido)
13. ACT-012 — GreenOps emissões reais (se telemetria histórica disponível)
14. ACT-013 — SupportBundles geração real
15. ACT-014 — RestorePoints (se backup provider definido)
16. ACT-015 — SAML 2.0 Protocol Handlers completos
17. ACT-016 — Kafka Producer/Consumer

### Sprint 4 — Qualidade e Extensão (2–4 semanas)
18. ACT-018 — EF Core Designer files
19. ACT-021 — Testes SearchCatalog
20. ACT-023 — Testes integração Export
21. ACT-022 — Testes E2E SAML
22. ACT-024 — OpenAPI export artefacto
23. ACT-019 — GraphQL Contract Type
24. ACT-020 — Protobuf Contract Type
25. ACT-017 — FinOps cloud billing (roadmap, alta complexidade)

---

## Referências

- [IMPLEMENTATION-STATUS.md](./IMPLEMENTATION-STATUS.md) — estado actual de cada módulo
- [FUTURE-ROADMAP.md](./FUTURE-ROADMAP.md) — features de evolução futura
- [ARCHITECTURE-OVERVIEW.md](./ARCHITECTURE-OVERVIEW.md) — bounded contexts e DbContexts
- [SECURITY-ARCHITECTURE.md](./SECURITY-ARCHITECTURE.md) — decisões de segurança
- [TESTING-STRATEGY.md](./TESTING-STRATEGY.md) — estratégia de testes

---

*Última actualização: Abril 2026 — Baseado em análise completa do repositório.*
