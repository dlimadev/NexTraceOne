# Notifications — Documentação e Onboarding

> **Módulo:** 11 — Notifications  
> **Data:** 2026-03-25  
> **Fase:** N8-R — Reexecução completa  
> **Estado:** ✅ FINALIZADO

---

## 1. Estado actual da documentação

### 1.1 Documentação existente

| Documento | Ficheiro | Estado |
|-----------|---------|--------|
| Module review | `docs/11-review-modular/11-notifications/module-review.md` | ✅ Existe (115 linhas) |
| Consolidated review | `docs/11-review-modular/11-notifications/module-consolidated-review.md` | ✅ Existe (141 linhas) |
| Architecture (boundary matrix) | `docs/architecture/module-boundary-matrix.md` | ✅ Secção Notifications |
| Architecture (data placement) | `docs/architecture/module-data-placement-matrix.md` | ✅ Secção Notifications |
| Architecture (table prefixes) | `docs/architecture/database-table-prefixes.md` | ✅ ntf_ documentado |

### 1.2 Documentação fragmentada

⚠️ **12 ficheiros NOTIFICATIONS-*** espalhados nos docs de execução que precisam de consolidação:

Estes ficheiros foram gerados durante fases de implementação mas não estão unificados. O conteúdo é parcial e repetitivo.

### 1.3 Documentação inline (código)

| Classe | XML Docs | Estado |
|--------|---------|--------|
| `NotificationsDbContext.cs` | ✅ Presente | Summary para classe e DbSets |
| `Notification.cs` | ⚠️ Parcial | Alguns métodos sem XML docs |
| `NotificationDelivery.cs` | ⚠️ Parcial | Sem docs nos métodos |
| `NotificationPreference.cs` | ⚠️ Parcial | Sem docs nos métodos |
| `NotificationOrchestrator.cs` | ⚠️ Parcial | Fluxo complexo sem docs adequados |
| `NotificationTemplateResolver.cs` | ❌ Ausente | Sem documentação dos templates |
| `ExternalChannelTemplateResolver.cs` | ❌ Ausente | Sem documentação dos templates de canal |
| Event handlers (8 ficheiros) | ❌ Ausente | Sem documentação dos eventos consumidos |
| Intelligence services (5 ficheiros) | ❌ Ausente | Sem documentação das regras |
| Governance services (4 ficheiros) | ❌ Ausente | Sem documentação |

---

## 2. Lacunas na documentação

| # | Lacuna | Impacto | Prioridade |
|---|--------|---------|-----------|
| DOC-01 | **Sem README.md** no módulo backend | Novo developer não sabe por onde começar | 🔴 Alto |
| DOC-02 | **Sem documentação do fluxo de delivery** | Pipeline não compreensível sem ler código | 🔴 Alto |
| DOC-03 | **Sem documentação dos templates** | Não se sabe quais templates existem | 🟠 Médio |
| DOC-04 | **Sem documentação dos event handlers** | Não se sabe quais eventos são consumidos | 🟠 Médio |
| DOC-05 | **Sem documentação das regras de inteligência** | Dedup, escalação, quiet hours não documentados | 🟠 Médio |
| DOC-06 | **Sem documentação da API** | Endpoints sem Swagger/OpenAPI descriptions | 🟠 Médio |
| DOC-07 | **12 ficheiros NOTIFICATIONS-* fragmentados** | Documentação dispersa e difícil de navegar | 🟡 Baixo |
| DOC-08 | **Sem diagramas de arquitectura** | Fluxos complexos sem representação visual | 🟡 Baixo |

---

## 3. Classes e fluxos que precisam de explicação

### 3.1 Fluxos principais

| Fluxo | Complexidade | Documentação actual |
|-------|-------------|-------------------|
| Event → Notification → Delivery | Alta | ❌ Apenas inferível do código |
| Template resolution (interno) | Média | ❌ Templates não listados |
| Template resolution (externo/canal) | Média | ❌ Formatos não documentados |
| Channel routing decision | Alta | ❌ Lógica não documentada |
| Deduplication window | Baixa | ❌ 5-min window não documentado |
| Quiet hours enforcement | Média | ❌ Regras não documentadas |
| Mandatory notification policy | Média | ❌ Quais são mandatórias? |
| Preference management | Baixa | ⚠️ Parcial (API documentada nos endpoints) |

### 3.2 Classes que precisam de XML docs

| Classe | Prioridade | Razão |
|--------|-----------|-------|
| `NotificationOrchestrator.cs` | 🔴 P0 | Classe central do pipeline |
| `NotificationTemplateResolver.cs` | 🔴 P0 | 13 templates não documentados |
| `ExternalChannelTemplateResolver.cs` | 🟠 P1 | Formatos de Email/Teams |
| `ExternalDeliveryService.cs` | 🟠 P1 | Orquestração de delivery |
| `NotificationRoutingEngine.cs` | 🟠 P1 | Lógica de routing complexa |
| `MandatoryNotificationPolicy.cs` | 🟠 P1 | Quais categorias são mandatórias |
| 8 Event handlers | 🟠 P1 | Mapeamento evento → notificação |
| 5 Intelligence services | 🟡 P2 | Regras de negócio |
| 4 Governance services | 🟡 P2 | Health, metrics, audit |

---

## 4. Notas de onboarding

### Para um novo developer do módulo Notifications:

1. **Começar por:** `NotificationOrchestrator.cs` — é o coração do módulo
2. **Entender as 3 entidades:** Notification, NotificationDelivery, NotificationPreference
3. **Entender o pipeline:** Evento → Handler → Request → Orchestrator → Template → Persist → Deliver
4. **Entender os 8 event handlers:** Cada um consome eventos de um módulo diferente
5. **Templates:** In-code em `NotificationTemplateResolver.cs` (13 tipos) + `ExternalChannelTemplateResolver.cs` (Email HTML + Teams Card)
6. **Canais:** In-app (sempre), Email (via SMTP), Teams (via webhook)
7. **Preferências:** Por utilizador/categoria/canal, com mandatory policy
8. **Intelligence:** Dedup (5 min), agrupamento, escalação, quiet hours, supressão
9. **Base de dados:** 3 tabelas ntf_*, 0 migrations (pendente)
10. **Permissões:** 4 permissões, **NÃO registadas no RolePermissionCatalog** (blocker)

### Fluxo de delivery (visão rápida):
```
Incident criado → IncidentNotificationHandler → NotificationRequest
→ Orchestrator.ProcessAsync()
  → Validate(request)
  → ResolveRecipients()
  → ResolveTemplate("IncidentCreated", {params})
  → Per recipient:
    → IsDuplicate? Skip : Continue
    → Notification.Create(...)
    → Store.AddAsync()
    → ExternalDeliveryService.ProcessAsync()
      → RoutingEngine.DetermineChannels()
      → EmailDispatcher.SendAsync() → NotificationDelivery.MarkDelivered()
      → TeamsDispatcher.SendAsync() → NotificationDelivery.MarkDelivered()
  → Store.SaveChangesAsync()
  → Return NotificationResult
```

---

## 5. Documentação mínima do módulo

### 5.1 README.md (a criar)

Conteúdo obrigatório:
1. Propósito do módulo
2. Estrutura de projectos (Domain, Application, Infrastructure, API, Contracts)
3. Entidades principais
4. Pipeline de delivery (diagrama)
5. Templates disponíveis
6. Canais suportados
7. Event handlers e eventos consumidos
8. Endpoints da API
9. Permissões necessárias
10. Como adicionar um novo tipo de notificação

### 5.2 Documentação de fluxos

| Fluxo | Documento sugerido |
|-------|-------------------|
| Pipeline de delivery | docs/11-review-modular/11-notifications/delivery-pipeline.md |
| Templates reference | docs/11-review-modular/11-notifications/templates-reference.md |
| Event handlers mapping | docs/11-review-modular/11-notifications/event-handlers-mapping.md |

---

## 6. Backlog de documentação

| # | Item | Prioridade | Esforço |
|---|------|-----------|---------|
| DOC-B01 | 🔴 Criar README.md no módulo backend | P0 | 4h |
| DOC-B02 | 🔴 Documentar pipeline de delivery ponta a ponta | P0 | 3h |
| DOC-B03 | 🟠 Documentar todos os 13 templates com exemplos | P1 | 3h |
| DOC-B04 | 🟠 Documentar event handlers (8) com mapeamento de eventos | P1 | 2h |
| DOC-B05 | 🟠 Adicionar XML docs ao NotificationOrchestrator | P1 | 2h |
| DOC-B06 | 🟠 Adicionar XML docs aos template resolvers | P1 | 2h |
| DOC-B07 | 🟠 Adicionar Swagger/OpenAPI descriptions aos endpoints | P1 | 2h |
| DOC-B08 | 🟡 Consolidar 12 ficheiros NOTIFICATIONS-* fragmentados | P2 | 4h |
| DOC-B09 | 🟡 Adicionar XML docs a todas as entidades de domínio | P2 | 2h |
| DOC-B10 | 🟡 Documentar regras de inteligência (dedup, escalation, etc.) | P2 | 3h |
| DOC-B11 | 🟡 Criar diagramas de arquitectura (Mermaid/PlantUML) | P3 | 4h |

**Esforço total estimado:** ~31h
