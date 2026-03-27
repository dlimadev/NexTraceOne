# Notifications — Finalização do Papel do Módulo

> **Módulo:** 11 — Notifications  
> **Data:** 2026-03-25  
> **Fase:** N8-R — Reexecução completa  
> **Estado:** ✅ FINALIZADO

---

## 1. O que é Notifications no NexTraceOne

O módulo Notifications é o **centro de notificações e alertas transversal** da plataforma NexTraceOne. Ele não é um simples envio de e-mails ou uma configuração isolada — é o módulo responsável por fechar o ciclo comunicacional entre eventos operacionais, decisões de governança, alertas de segurança e acções pendentes, entregando informação ao utilizador certo, no canal certo, no momento certo.

### Responsabilidades fundamentais

| Responsabilidade | Descrição |
|-----------------|-----------|
| **Central de notificações** | Recepção, persistência e apresentação de notificações internas (in-app) |
| **Delivery multi-canal** | Entrega via Email e Microsoft Teams, com rastreabilidade |
| **Preferências por utilizador** | Controlo de canais/categorias por utilizador |
| **Templates** | Resolução de templates por tipo de evento (interno e externo) |
| **Deduplicação** | Janela de 5 minutos para evitar duplicatas |
| **Retry e status** | Rastreio de entrega com retry e estados (Pending/Delivered/Failed/Skipped) |
| **Escalação** | Escalação automática de notificações não atendidas |
| **Digest** | Agrupamento periódico de notificações (digestão) |
| **Quiet Hours** | Respeito a horários de silêncio |
| **Supressão** | Regras de supressão por contexto |
| **Auditoria** | Rastreabilidade completa do ciclo de vida da notificação |

---

## 2. Por que é importante no produto final

O NexTraceOne é uma plataforma de observabilidade com change intelligence. Sem notificações:
- Incidentes não são comunicados às equipas certas
- Aprovações pendentes ficam invisíveis
- Violações de compliance passam despercebidas
- Alertas de segurança (break glass, acessos não autorizados) não chegam a quem precisa
- Mudanças e releases não são notificadas aos stakeholders

O módulo é **transversal por design** — consome eventos de **8 módulos** distintos via event handlers dedicados.

---

## 3. Do que o módulo é dono

| Ownership | Artefactos |
|-----------|-----------|
| **Entidade Notification** | `Notification.cs` — aggregate root com 20+ propriedades e 10+ métodos de domínio |
| **Entidade NotificationDelivery** | `NotificationDelivery.cs` — rastreio de entrega externa |
| **Entidade NotificationPreference** | `NotificationPreference.cs` — preferências por utilizador/canal/categoria |
| **Orquestração** | `NotificationOrchestrator.cs` — pipeline completo de processamento |
| **Templates** | `NotificationTemplateResolver.cs` + `ExternalChannelTemplateResolver.cs` |
| **Delivery** | `ExternalDeliveryService.cs`, `EmailNotificationDispatcher.cs`, `TeamsNotificationDispatcher.cs` |
| **Intelligence** | Deduplicação, agrupamento, digest, escalação, quiet hours, supressão |
| **Persistência** | `NotificationsDbContext` com tabelas `ntf_*` |
| **API** | 7 endpoints REST em `/api/v1/notifications/` |
| **Frontend** | 3 páginas, 2 componentes (NotificationBell, NotificationItem), 4 hooks |

---

## 4. Do que o módulo NÃO é dono

| Não é dono de | Pertence a |
|--------------|-----------|
| Eventos de origem (incidentes, aprovações, etc.) | Módulos emissores (Change Gov, OI, Catalog, etc.) |
| Identidade do utilizador / roles | Identity & Access |
| Definições de configuração (settings gerais) | Configuration |
| Canais de integração externa (SMTP servers, Teams webhooks) | Integrations |
| Regras de compliance ou governança | Governance / Audit & Compliance |
| Métricas de observabilidade | Operational Intelligence |

---

## 5. Por que NÃO é apenas envio de e-mail

O módulo vai muito além do envio de e-mail:

1. **Notificações in-app** — Central persistente com estados (Unread/Read/Acknowledged/Archived/Dismissed)
2. **Multi-canal** — Email E Teams, não apenas um
3. **Inteligência** — Deduplicação, agrupamento, digest, escalação automática, quiet hours
4. **Preferências** — Controlo granular por utilizador/canal/categoria
5. **Rastreabilidade** — Cada entrega é registada com status, retry count, timestamps
6. **Auditoria** — Ciclo de vida completo auditável
7. **Política mandatória** — Notificações críticas não podem ser desactivadas pelo utilizador (`MandatoryNotificationPolicy`)
8. **Governança** — Integração com catálogo de governança (`NotificationCatalogGovernance`)
9. **Health & Metrics** — Métricas de saúde e performance do módulo

---

## 6. Maturidade actual

| Camada | Maturidade | Justificação |
|--------|-----------|-------------|
| Backend (Domínio) | 🟢 85% | 3 entidades completas, 6 enums, 3 domain events, strongly-typed IDs |
| Backend (Application) | 🟢 85% | 21 abstrações, 7 handlers, orchestrator completo |
| Backend (Infrastructure) | 🟡 70% | 15+ serviços implementados, mas sem migrations |
| Backend (API) | 🟢 80% | 7 endpoints funcionais com permissões |
| Frontend | 🟡 70% | 3 páginas, 4 hooks, 2 componentes, mas sem sidebar menu |
| Persistência | 🟠 50% | DbContext e configs completos, MAS 0 migrations |
| Segurança | 🔴 30% | Permissões NÃO registadas no `RolePermissionCatalog` |
| Documentação | 🔴 30% | Fragmentada em 12 ficheiros, não consolidada |
| **Global** | **🟡 65%** | **Funcionalmente robusto, mas com gaps estruturais críticos** |
