# PARTE 12 — Module Dependency Map

> **Módulo:** AI & Knowledge (07)
> **Data:** 2026-03-25
> **Prompt:** N13

---

## 1. Mapa de dependências

```
                          ┌─────────────────────┐
                          │   Identity & Access  │
                          │    (01)              │
                          └──────────┬──────────┘
                                     │ UserId, TenantId, Roles, Permissions
                                     ▼
┌──────────┐    events    ┌─────────────────────┐    queries     ┌──────────────┐
│  Catalog │◄────────────│   AI & Knowledge    │──────────────►│  Contracts   │
│   (03)   │             │       (07)          │               │    (04)      │
└──────────┘             └─────────┬───────────┘               └──────────────┘
                                   │
                    ┌──────────────┼──────────────┐
                    │              │              │
                    ▼              ▼              ▼
          ┌────────────┐  ┌──────────────┐  ┌──────────────┐
          │   Change   │  │ Operational  │  │   Audit &    │
          │ Governance │  │ Intelligence │  │ Compliance   │
          │    (05)    │  │    (06)      │  │    (10)      │
          └────────────┘  └──────────────┘  └──────────────┘
                                                    ▲
                                                    │ events (futuro)
                                                    │
          ┌────────────┐                   ┌──────────────┐
          │Notifications│◄──── events ────│  AI & Know   │
          │    (11)     │    (futuro)      │   (07)       │
          └────────────┘                   └──────────────┘
```

---

## 2. Dependência com Identity & Access (01)

| Aspecto | Tipo | Detalhes |
|---------|------|---------|
| Autenticação | Runtime | JWT claims (UserId, TenantId, Roles) |
| Permissões | Runtime | `ai:assistant:*`, `ai:governance:*`, `ai:ide:*`, `ai:runtime:*` |
| User profile | Query | Nome do utilizador para conversas |
| Tenant config | Query | Configuração do tenant para políticas |

**Acoplamento:** 🟢 Baixo — via JWT claims e permission checks standard
**Interface:** HTTP headers + `ICurrentUser` abstraction

---

## 3. Dependência com Catalog (03)

| Aspecto | Tipo | Detalhes |
|---------|------|---------|
| Service definitions | Query (Orchestration) | `AskCatalogQuestion` precisa de dados do catálogo |
| Service metadata | Context assembly | Grounding de contexto com dados de serviços |
| Service dependencies | Query (Orchestration) | Análise de impacto e promoção |

**Acoplamento:** 🟡 Médio — Orchestration depende de queries ao Catalog
**Interface:** Integration query (via application service ou endpoint HTTP)
**Estado:** ⚠️ Integração provavelmente parcial — features de Orchestration possivelmente stubs

---

## 4. Dependência com Contracts (04)

| Aspecto | Tipo | Detalhes |
|---------|------|---------|
| Contract definitions | Query (Orchestration) | Análise AI de contratos |
| Contract schemas | Context assembly | Grounding com schemas/payloads |

**Acoplamento:** 🟡 Médio — similar a Catalog
**Interface:** Integration query
**Estado:** ⚠️ Provavelmente parcial

---

## 5. Dependência com Change Governance (05)

| Aspecto | Tipo | Detalhes |
|---------|------|---------|
| Change requests | Query (Orchestration) | `ClassifyChangeWithAI` precisa de dados do change |
| Release data | Query (Orchestration) | `SummarizeReleaseForApproval`, `AssessPromotionReadiness` |
| Semantic versioning | Query (Orchestration) | `SuggestSemanticVersionWithAI` |

**Acoplamento:** 🟡 Médio — 3+ features de Orchestration dependem
**Interface:** Integration query
**Estado:** ⚠️ Provavelmente parcial

---

## 6. Dependência com Operational Intelligence (06)

| Aspecto | Tipo | Detalhes |
|---------|------|---------|
| Incidents | Query (Orchestration) | Investigação AI de incidentes |
| Telemetry data | Query (Runtime) | `SearchTelemetry` |
| Environment data | Query (Orchestration) | `CompareEnvironments`, `AnalyzeNonProdEnvironment` |

**Acoplamento:** 🟡 Médio — análise assistida depende de dados operacionais
**Interface:** Integration query
**Estado:** ⚠️ Provavelmente parcial

---

## 7. Dependência com Audit & Compliance (10)

| Aspecto | Tipo | Detalhes |
|---------|------|---------|
| Emissão de eventos | Eventos (produtor) | AI & Knowledge emite eventos de uso de IA |
| Audit trail | Eventos (produtor) | Execuções de agents, mudanças de configuração |

**Acoplamento:** 🟢 Baixo — unidirecional (AI produz, Audit consome)
**Interface:** Domain events (futuro — **não implementado**)
**Estado:** ❌ Nenhum evento publicado atualmente

---

## 8. Dependência com Notifications (11)

| Aspecto | Tipo | Detalhes |
|---------|------|---------|
| Alertas de orçamento | Eventos (produtor) | Quando quota/orçamento excedido |
| Alertas de execução | Eventos (produtor) | Quando agent falha ou timeout |

**Acoplamento:** 🟢 Baixo — unidirecional
**Interface:** Domain events (futuro — **não implementado**)
**Estado:** ❌ Nenhum evento publicado

---

## 9. Dependência com Configuration (09)

| Aspecto | Tipo | Detalhes |
|---------|------|---------|
| Feature flags de IA | Query | Ativar/desativar funcionalidades de IA |
| Configuração de providers | Query | Parâmetros de configuração |

**Acoplamento:** 🟢 Baixo
**Estado:** ⚠️ Uso de Configuration não confirmado

---

## 10. O que o módulo consome de outros

| Módulo fonte | Dados consumidos | Interface | Estado |
|-------------|-----------------|-----------|--------|
| Identity & Access | UserId, TenantId, Roles | JWT claims | ✅ Funcional |
| Catalog | Service definitions | Integration query | ⚠️ Parcial |
| Contracts | Contract definitions | Integration query | ⚠️ Parcial |
| Change Governance | Change requests, releases | Integration query | ⚠️ Parcial |
| Operational Intelligence | Incidents, telemetry | Integration query | ⚠️ Parcial |
| Configuration | Feature flags | Configuration API | ⚠️ Não confirmado |

---

## 11. O que o módulo expõe para outros

| Módulo destino | Dados expostos | Interface | Estado |
|---------------|---------------|-----------|--------|
| Audit & Compliance | Eventos de uso de IA | Domain events | ❌ Não implementado |
| Notifications | Alertas de orçamento/execução | Domain events | ❌ Não implementado |
| Product Analytics | Métricas de uso de IA | Domain events | ❌ Não implementado |
| Catalog (futuro) | Classificação AI de serviços | Orchestration API | ⚠️ Parcial |
| Change Governance (futuro) | Classificação AI de changes | Orchestration API | ⚠️ Parcial |

---

## 12. Resumo

| Dimensão | Estado |
|----------|--------|
| Consumo de Identity & Access | ✅ Funcional |
| Consumo de módulos operacionais | ⚠️ Parcial (Orchestration features incertas) |
| Emissão de domain events | ❌ Zero eventos publicados |
| Integração com Audit | ❌ Não implementada |
| Integração com Notifications | ❌ Não implementada |
| **Acoplamento geral** | 🟡 Médio — dependências lógicas corretas, implementação incompleta |
