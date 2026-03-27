# P4.2 — Post-Change Gap Report

**Data:** 2026-03-26  
**Fase:** P4.2 — Criar workflow real de Event Contracts / AsyncAPI no módulo Contracts

---

## 1. O Que Foi Resolvido

| Item | Antes | Depois |
|---|---|---|
| `EventContractDetail` entity | Inexistente | ✅ Criada — channels, mensagens, servidores, versão AsyncAPI |
| `EventDraftMetadata` entity | Inexistente | ✅ Criada para drafts de evento no Studio |
| `AsyncApiMetadataExtractor` service | Inexistente | ✅ Criado — extrai title, asyncapi version, content type, channels, messages, servers |
| `ImportAsyncApiContract` handler | Inexistente | ✅ Criado — workflow real de importação com extração de metadados |
| `CreateEventDraft` handler | Inexistente | ✅ Criado — criação de draft com EventDraftMetadata |
| `GetEventContractDetail` query | Inexistente | ✅ Criado — consulta detalhe AsyncAPI de versão publicada |
| Endpoints AsyncAPI dedicados | Inexistentes | ✅ 3 endpoints: asyncapi/import, drafts/event, event-detail |
| Repositórios AsyncAPI | Inexistentes | ✅ EventContractDetailRepository + EventDraftMetadataRepository |
| EF Core: tabelas AsyncAPI | Inexistentes | ✅ ctr_event_contract_details + ctr_event_draft_metadata |
| ContractsDbContext | Sem DbSets AsyncAPI | ✅ +2 DbSets AsyncAPI |
| Erros de domínio AsyncAPI | Inexistentes | ✅ 3 novos erros específicos |
| Frontend: tipos AsyncAPI | Inexistentes | ✅ EventContractDetail, AsyncApiImportResponse, EventDraftCreateResponse |
| Frontend: API AsyncAPI | Inexistentes | ✅ importAsyncApi, getEventContractDetail, createEventDraft |
| Frontend: hooks AsyncAPI | Inexistentes | ✅ useAsyncApiImport, useCreateEventDraft, useEventContractDetail |
| CreateServicePage Event-aware | Genérico | ✅ Campos AsyncAPI específicos + usa createEventDraft |
| Testes de workflow AsyncAPI | Inexistentes | ✅ 41 novos testes (558 total) |

**Diagnóstico pré-P4.2:** Contract Governance AsyncAPI / Event Contracts — INCOMPLETE  
**Diagnóstico pós-P4.2:** Workflow AsyncAPI real — modelo, persistência, handlers, endpoints, frontend mínimo

---

## 2. O Que Ainda Está Pendente

### 2.1 EF Core Migrations

As migrations para as novas tabelas `ctr_event_contract_details` e `ctr_event_draft_metadata`
ainda **não foram geradas**. As tabelas existem no modelo EF Core mas não existem fisicamente
na base de dados até as migrations serem geradas e aplicadas.

**Impacto:** Endpoints não funcionarão contra base de dados real até migrations aplicadas.  
**Ação:** P4.3 deve incluir `dotnet ef migrations add AddEventContractTables`.

_Nota: as migrations de P4.1 (SOAP) também ainda não foram geradas._

### 2.2 UpdateEventDraftMetadata command

Não foi criado um command para atualizar o `EventDraftMetadata` após a criação do draft
(quando o utilizador edita channels/mensagens no VisualEventBuilder).

**Impacto:** Alterações feitas no VisualEventBuilder não persistem nos metadados estruturados.  
**Ação:** P4.3 deve criar `UpdateEventDraftMetadata` command + handler + endpoint.

### 2.3 Sincronização automática AsyncAPI↔EventContractDetail

Quando o `specContent` de uma `ContractVersion` com `Protocol=AsyncApi` é alterado,
o `EventContractDetail` associado não é re-extraído automaticamente.

**Impacto:** Metadados podem ficar desatualizados após edição da spec.  
**Ação:** Criar `ReSyncAsyncApiMetadata` command ou integrar com publicação.

### 2.4 DraftStudioPage — painel de metadados AsyncAPI

O `DraftStudioPage` e o `VisualEventBuilder` ainda não exibem os metadados AsyncAPI
específicos (channels, mensagens, servidores, version) vindos do `EventDraftMetadata` persistido.

**Impacto:** Usabilidade reduzida — o builder visual é local sem persistência estruturada.  
**Ação:** P4.3 deve integrar `useEventContractDetail` / `useEventWorkflow` no Studio.

### 2.5 AsyncAPI versão 3.0 — channels vs operations

AsyncAPI 3.0 reorganizou a estrutura (channels sem operações diretas; operações são entidades separadas).
O `AsyncApiMetadataExtractor` e `AsyncApiSpecParser` atuais assumem a estrutura 2.x.

**Impacto:** Specs AsyncAPI 3.0 terão channels extraídos incorretamente.  
**Ação:** Fase futura — deteção de versão e parsing diferenciado.

### 2.6 Diff semântico AsyncAPI contextualizado no frontend

O `AsyncApiDiffCalculator` existe e funciona, mas o resultado não é apresentado com contexto
AsyncAPI-específico (channels adicionados/removidos, mudanças de schema de mensagem).

**Ação:** Fase futura.

### 2.7 Geração por IA de contrato AsyncAPI

Fora do escopo desta fase.  
**Ação:** P5.x — Agente de geração de contrato de eventos.

---

## 3. O Que Fica Explicitamente para P4.3

| Item | Prioridade |
|---|---|
| EF Core migrations (AsyncAPI + SOAP pendentes de P4.1) | 🔴 Alta |
| `UpdateEventDraftMetadata` command + handler + endpoint | 🔴 Alta |
| `DraftStudioPage` painel de metadados AsyncAPI | 🟡 Média |
| `VisualEventBuilder` integrado com persistência EventDraftMetadata | 🟡 Média |
| Re-sincronização automática de metadados ao editar spec | 🟡 Média |
| AsyncAPI 3.0 — parsing diferenciado | 🟢 Baixa |
| Diff AsyncAPI contextualizado no frontend | 🟢 Baixa |

---

## 4. Classificação Pós-P4.2

| Capability | Estado anterior | Estado atual |
|---|---|---|
| Contract Governance AsyncAPI / Event Contracts | ❌ INCOMPLETE | ⚠️ PARTIAL — Modelo real, workflow mínimo funcional |
| AsyncAPI Import | ❌ Nominal | ✅ Real — com extração de channels, mensagens, servidores |
| Event Draft Creation | ❌ Inexistente | ✅ Real — com EventDraftMetadata |
| Event Detail Query | ❌ Inexistente | ✅ Real — com channels, mensagens, servidores, content type |
| Frontend Event Recognition | ⚠️ VisualEventBuilder local sem backend | ✅ CreateServicePage Event-aware + API integrada |
| EF Core persistence | ❌ Sem tabelas | ⚠️ Configurado, migrations pendentes |
| Tópicos/Channels entidade dedicada | ❌ Inexistente | ✅ Persistidos em ChannelsJson + MessagesJson (formato JSON, não entidade relacional) |
| Schema Registry | ❌ Inexistente | ⚠️ MessagesJson captura schemas básicos (relacional completo fora de escopo) |
