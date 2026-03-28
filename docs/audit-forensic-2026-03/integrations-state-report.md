# Relatório de Integrações — NexTraceOne
**Auditoria Forense | 28 de Março de 2026**

---

## Objetivo da Área no Contexto do Produto

Integrações devem ser capacidades governadas — não acoplamentos ad-hoc. O NexTraceOne precisa de ingestão de eventos CI/CD, identity providers, e fontes de telemetria para fechar os fluxos de Change Intelligence e Operational Intelligence.

---

## Estado Atual

### Módulo Integrations

`src/modules/integrations/` | 35 ficheiros | `IntegrationsDbContext` | 3 migrações

**Status: STUB**

O módulo tem estrutura base (DbContext, entidades básicas como `Integration`, `Connector`, `CredentialVault`) e 3 migrações mas os conectores externos não têm lógica real de ingestão.

**Evidência:** `src/modules/integrations/NexTraceOne.Integrations.Infrastructure/` — adapters existem como templates sem implementação de chamadas reais.

---

## Integrações por Categoria

### Identity Providers

| Provider | Estado | Evidência |
|---|---|---|
| Azure AD / Entra ID (OIDC) | ✅ CONFIGURADO | `appsettings.json` → `OidcProviders.azure` |
| Keycloak (OIDC) | ⚠️ CONFIGURÁVEL | Via `OidcProviders` config (não hardcoded) |
| Generic OIDC | ✅ SUPORTADO | `OidcProviderService` em IdentityAccess |
| SAML | ❌ NÃO CONFIRMADO | Não encontrado em código atual |

**Avaliação:** OIDC funcional. SAML não confirmado.

---

### CI/CD e Deploy Events

| Fonte | Estado | Gap |
|---|---|---|
| GitLab | ❌ STUB | Connector existe como template |
| Jenkins | ❌ STUB | — |
| GitHub Actions | ❌ STUB | — |
| Azure DevOps | ❌ STUB | — |
| Generic webhook | ⚠️ Parcial | `NexTraceOne.Ingestion.Api` pode receber webhooks mas sem parser específico |

**Impacto crítico:** Sem ingestão real de eventos CI/CD, `ChangeGovernance` não pode correlacionar deploys automaticamente. A ingestão manual é o único caminho atual.

---

### Telemetria / Observabilidade

| Fonte | Estado | Evidência |
|---|---|---|
| OpenTelemetry traces | ✅ CONFIGURADO | `BuildingBlocks.Observability`, OTLP exporter |
| OpenTelemetry metrics | ✅ CONFIGURADO | `NexTraceMeters` |
| Logs estruturados (Serilog) | ✅ CONFIGURADO | — |
| ClickHouse (analytics) | ✅ CONFIGURADO | `build/clickhouse/`, `ClickHouseAnalyticsWriter` |
| Logs IIS | ⚠️ Documentado | Sem ingestão confirmada via `NexTraceOne.Ingestion.Api` |
| Logs .NET estruturados | ✅ Produzidos | — |
| Kafka / Event streaming | ⚠️ Event contracts modelados | Ingestão real de Kafka não confirmada |

---

### AI Providers

| Provider | Estado | Evidência |
|---|---|---|
| Ollama (local) | ✅ CONFIGURADO | `appsettings.json` — `localhost:11434`, enabled |
| OpenAI | ✅ CONFIGURADO (desativado) | `appsettings.json` — `Enabled: false`, ApiKey vazio |
| Anthropic | ❌ Não presente | — |
| Azure OpenAI | ❌ Não presente | — |

**Avaliação:** Ollama como provider preferencial está correto para self-hosted. OpenAI disponível mas desativado (boa prática de default on-prem). `IExternalAIRoutingPort` abstrai os providers corretamente mas sem implementação ligada.

---

### Knowledge Sources

| Fonte | Estado |
|---|---|
| Internal knowledge (KnowledgeDbContext) | ⚠️ Presente mas sem migrações |
| Contract context (ContractsDbContext) | ✅ Context builders existem |
| Change context (ChangeIntelligenceDbContext) | ✅ Context builders existem |
| Service context (CatalogGraphDbContext) | ✅ Context builders existem |
| External docs (Backstage import) | ✅ `ImportFromBackstage` handler real |
| OpenAPI import | ✅ `ImportContract` handler real |

---

## Cross-Module Interfaces — Status

As interfaces cross-module são o mecanismo de integração interno entre módulos.

| Interface | Módulo Origem | Consumidores | Estado |
|---|---|---|---|
| `IContractsModule` | catalog | Developer Portal, Governance, AI | ❌ Sem consumidores registados |
| `IAiOrchestrationModule` | aiknowledge | AI features cross-module | ❌ PLAN |
| `IExternalAiModule` | aiknowledge | ExternalAI consumers | ❌ PLAN |
| `IPromotionModule` | changegovernance | Governance | ❌ PLAN |
| `IRulesetGovernanceModule` | changegovernance | Catalog, Governance | ❌ PLAN |
| `IChangeIntelligenceModule` | changegovernance | Governance, AI, OpsIntelligence | ❌ PLAN |
| `ICostIntelligenceModule` | operationalintelligence | Governance FinOps | ❌ PLAN |
| `IRuntimeIntelligenceModule` | operationalintelligence | Reliability, AI | ❌ PLAN |

**Total: 8 interfaces definidas, 0 consumidores implementados cross-module.**

---

## Impacto das Integrações Ausentes

| Integração Ausente | Capacidade Bloqueada |
|---|---|
| Conectores CI/CD | Ingestão automática de deploys → ChangeGovernance |
| `IContractsModule` implementado | Developer Portal, 7 stubs backend |
| `ICostIntelligenceModule` implementado | FinOps real em Governance |
| `IChangeIntelligenceModule` implementado | Governance com dados reais de mudanças |
| Ollama conectado via `IExternalAIRoutingPort` | AI Assistant funcional |
| Kafka ingestão real | Event Contracts com dados reais |

---

## Recomendações

1. **Crítico:** Implementar `IExternalAIRoutingPort` → Ollama (fecha AI Assistant)
2. **Alta:** Implementar `IContractsModule` com consumidores cross-module (fecha Developer Portal)
3. **Alta:** Implementar conector GitLab/GitHub para ingestão de eventos CI/CD
4. **Alta:** Implementar `ICostIntelligenceModule` (fecha FinOps)
5. **Média:** Validar pipeline de ingestão OpenTelemetry E2E
6. **Média:** Confirmar/implementar SAML se for requisito imediato

---

*Data: 28 de Março de 2026*
