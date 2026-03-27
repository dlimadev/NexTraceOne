# P4.3 — Post-Change Gap Report

**Data:** 2026-03-26  
**Fase:** P4.3 — Criar workflow real de Background Service Contracts no módulo Contracts

---

## 1. O Que Foi Resolvido

| Item | Antes | Depois |
|---|---|---|
| `BackgroundServiceContractDetail` entity | Inexistente | ✅ Criada — ServiceName, Category, TriggerType, Schedule, Inputs, Outputs, SideEffects |
| `BackgroundServiceDraftMetadata` entity | Inexistente | ✅ Criada para drafts de background service no Studio |
| `RegisterBackgroundServiceContract` handler | Inexistente | ✅ Criado — workflow real de registo |
| `CreateBackgroundServiceDraft` handler | Inexistente | ✅ Criado — criação de draft com BackgroundServiceDraftMetadata |
| `GetBackgroundServiceContractDetail` query | Inexistente | ✅ Criado — consulta detalhe de background service |
| Endpoints Background Service dedicados | Inexistentes | ✅ 3 endpoints: register, drafts/background-service, background-service-detail |
| Repositórios Background Service | Inexistentes | ✅ BackgroundServiceContractDetailRepository + BackgroundServiceDraftMetadataRepository |
| EF Core: tabelas Background Service | Inexistentes | ✅ ctr_background_service_contract_details + ctr_background_service_draft_metadata |
| Check constraint em TriggerType | Inexistente | ✅ IN ('Cron','Interval','EventTriggered','OnDemand','Continuous') |
| ContractsDbContext | Sem DbSets para este tipo | ✅ +2 DbSets |
| Erros de domínio Background Service | Inexistentes | ✅ 3 novos erros específicos |
| Frontend: tipos Background Service | Inexistentes | ✅ 3 novos tipos TypeScript |
| Frontend: API Background Service | Inexistente | ✅ registerBackgroundService, getBackgroundServiceContractDetail, createBackgroundServiceDraft |
| Frontend: hooks Background Service | Inexistentes | ✅ 3 novos hooks |
| CreateServicePage BackgroundService-aware | Genérico | ✅ Campos específicos: Name, Category, TriggerType, ScheduleExpression |
| Testes de workflow Background Service | Inexistentes | ✅ 33 novos testes (589 total) |

---

## 2. O Que Ainda Está Pendente

### 2.1 EF Core Migrations

As migrations para `ctr_background_service_contract_details` e `ctr_background_service_draft_metadata`
ainda **não foram geradas**.

**Status geral de migrations pendentes após P4.1 + P4.2 + P4.3:**
- `ctr_soap_contract_details` (P4.1)
- `ctr_soap_draft_metadata` (P4.1)
- `ctr_event_contract_details` (P4.2)
- `ctr_event_draft_metadata` (P4.2)
- `ctr_background_service_contract_details` (P4.3)
- `ctr_background_service_draft_metadata` (P4.3)

**Ação:** P4.4 deve incluir `dotnet ef migrations add AddSpecificContractDetailTables`.

### 2.2 UpdateBackgroundServiceDraftMetadata command

Não foi criado um command para atualizar os metadados do draft após a criação (quando o utilizador
edita category/trigger/schedule no Studio).

**Impacto:** Alterações feitas no Studio não persistem nos metadados estruturados.  
**Ação:** P4.4 deve criar `UpdateBackgroundServiceDraftMetadata` command + handler + endpoint.

### 2.3 DraftStudioPage — painel de metadados Background Service

O `DraftStudioPage` ainda não exibe os metadados Background Service específicos vindos do
`BackgroundServiceDraftMetadata` persistido.

**Ação:** P4.4 deve integrar os hooks de Background Service no Studio.

### 2.4 Side Effects como entidades relacionais

`SideEffectsJson` é persitido como JSON (lista de strings). Para maior semântica e consultabilidade
(ex: listar todos os contratos que escrevem na tabela X), seria preferível uma entidade relacional.

**Impacto:** Limitado — para o workflow mínimo é suficiente.  
**Ação:** Fase futura — entidade `BackgroundServiceSideEffect` quando necessário para queries.

### 2.5 Integração com Quartz e runtime de jobs

Fora do escopo desta fase.  
**Ação:** Fase futura — correlação entre BackgroundServiceContract e execuções reais de jobs.

---

## 3. O Que Fica Explicitamente para P4.4

| Item | Prioridade |
|---|---|
| EF Core migrations para todas as tabelas P4.1 + P4.2 + P4.3 | 🔴 Alta |
| `UpdateBackgroundServiceDraftMetadata` command | 🔴 Alta |
| `UpdateSoapDraftMetadata` command (também pendente de P4.1) | 🔴 Alta |
| `UpdateEventDraftMetadata` command (também pendente de P4.2) | 🔴 Alta |
| DraftStudioPage — painéis de metadados por tipo (SOAP/Event/BackgroundService) | 🟡 Média |
| Side effects como entidades relacionais (quando justificado) | 🟢 Baixa |
| Integração com Quartz runtime | 🟢 Baixa (fase futura separada) |

---

## 4. Classificação Pós-P4.3

| Capability | Estado anterior | Estado atual |
|---|---|---|
| Background Service Contracts | ❌ INCOMPLETE | ⚠️ PARTIAL — Modelo real, workflow mínimo funcional |
| Registo de Background Service | ❌ Inexistente | ✅ Real — com metadados específicos |
| Draft de Background Service | ❌ Genérico | ✅ Real — com BackgroundServiceDraftMetadata |
| Consulta de detalhe | ❌ Inexistente | ✅ Real — com categoria, trigger, schedule, inputs, outputs, side effects |
| Frontend BackgroundService-aware | ❌ Genérico | ✅ CreateServicePage com campos específicos |
| EF Core persistence | ❌ Sem tabelas | ⚠️ Configurado, migrations pendentes |
| TriggerType validation | ❌ Inexistente | ✅ Check constraint + validator FluentValidation |

---

## 5. Estado Global do Módulo Contracts após P4.1 + P4.2 + P4.3

| Tipo Contratual | Workflow | Entidades Específicas | Frontend |
|---|---|---|---|
| REST / OpenAPI | ✅ Completo | ContractVersion nativa | ✅ |
| SOAP / WSDL | ✅ Real (P4.1) | SoapContractDetail + SoapDraftMetadata | ✅ |
| Event / AsyncAPI | ✅ Real (P4.2) | EventContractDetail + EventDraftMetadata | ✅ |
| Background Service | ✅ Real (P4.3) | BackgroundServiceContractDetail + BackgroundServiceDraftMetadata | ✅ |
| Migrations para P4.1-P4.3 | ❌ Pendente | — | — |
| UpdateDraftMetadata para P4.1-P4.3 | ❌ Pendente | — | — |
