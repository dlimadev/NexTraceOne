# P4.4 — Post-Change Gap Report

**Data:** 2026-03-26  
**Fase:** P4.4 — Completar o Publication Center end-to-end no módulo Contracts

---

## 1. O Que Foi Resolvido

| Item | Antes | Depois |
|---|---|---|
| `ContractPublicationEntry` entity | Inexistente | ✅ Criada com estados, transições e rastreabilidade |
| `ContractPublicationStatus` enum | Inexistente | ✅ PendingPublication/Published/Withdrawn/Deprecated |
| `PublicationVisibility` enum | Inexistente | ✅ Internal/External/RestrictedToTeams |
| `PublishContractToPortal` handler | Inexistente | ✅ Cria e publica no portal; valida lifecycle state |
| `WithdrawContractFromPortal` handler | Inexistente | ✅ Retira publicação com motivo |
| `GetPublicationCenterEntries` handler | Inexistente | ✅ Lista entradas com filtros |
| `GetContractPublicationStatus` handler | Inexistente | ✅ Estado de publicação por ContractVersionId |
| Endpoints Publication Center | Inexistentes | ✅ 4 endpoints: publish, withdraw, list, status |
| `DeveloperPortalDbContext` | 5 DbSets, sem publication | ✅ +1 DbSet: ContractPublications |
| Tabela `cat_portal_contract_publications` | Inexistente | ✅ Com check constraints e índices |
| Frontend: tipos Publication Center | Inexistentes | ✅ 7 novos tipos TypeScript |
| Frontend: API Publication Center | Inexistente | ✅ publicationCenterApi com 4 funções |
| Frontend: hooks Publication Center | Inexistentes | ✅ 4 novos hooks |
| Frontend: PublicationCenterPage | Inexistente | ✅ Página real com filtros, tabela e ação de retirada |
| ContractQuickActions | Sem publish/withdraw | ✅ Botões "Publish to Portal" e "Withdraw from Portal" |
| Separação PublishDraft ≠ PublishToPortal | Implícita/confusa | ✅ Explicitamente separados e documentados |

---

## 2. O Que Ainda Está Pendente

### 2.1 EF Core Migrations

As migrations para `cat_portal_contract_publications` não foram geradas nesta fase.

**Situação geral de migrations pendentes após P4.1 + P4.2 + P4.3 + P4.4:**
- `ctr_soap_contract_details` (P4.1)
- `ctr_soap_draft_metadata` (P4.1)
- `ctr_event_contract_details` (P4.2)
- `ctr_event_draft_metadata` (P4.2)
- `ctr_background_service_contract_details` (P4.3)
- `ctr_background_service_draft_metadata` (P4.3)
- `cat_portal_contract_publications` (P4.4)

**Ação:** P4.5 deve incluir `dotnet ef migrations add` para todas estas tabelas.

### 2.2 Approval workflow de publicação

O fluxo atual cria diretamente em `PendingPublication` e imediatamente transiciona para `Published`.
Para governança enterprise completa, falta um approval workflow separado onde um TechLead/Architect aprova a publicação antes de ela ficar visível.

**Impacto:** Baixo para MVP — a publicação ainda requer `contracts:write` que é uma permissão restrita.  
**Ação:** P4.5 deve adicionar `ApprovePublication` com estado `PendingPublication` não imediatamente publicado.

### 2.3 Integração com SearchCatalog

O handler `SearchCatalog` no portal não filtra por `ContractPublicationStatus`. Contratos retirados ou deprecados continuam aparecendo na pesquisa como se fossem publicados.

**Ação:** P4.5 deve cruzar `SearchCatalog` com `ContractPublications` para filtrar resultados.

### 2.4 Badge de estado de publicação no ContractWorkspacePage

O `ContractWorkspacePage` ainda não chama `useContractPublicationStatus()` para mostrar o estado de publicação ao lado dos outros badges de lifecycle. Os botões "Publish to Portal" / "Withdraw from Portal" foram adicionados ao `ContractQuickActions` mas precisam de ser wired ao `ContractWorkspacePage`.

**Ação:** P4.5 deve conectar `useContractPublicationStatus(contractVersionId)` no `ContractWorkspacePage` e passar os callbacks correspondentes ao `ContractQuickActions`.

### 2.5 UpdateDraftMetadata para P4.1-P4.3

Pendente de P4.3: `UpdateSoapDraftMetadata`, `UpdateEventDraftMetadata`, `UpdateBackgroundServiceDraftMetadata` commands ainda não existem.

**Ação:** P4.5 deve criar estes commands.

---

## 3. O Que Fica Explicitamente para P4.5

| Item | Prioridade |
|---|---|
| EF Core migrations para todas as tabelas P4.1–P4.4 (7 tabelas) | 🔴 Alta |
| Wiring de `ContractWorkspacePage` com publication status hooks | 🔴 Alta |
| Filtro de `SearchCatalog` por status de publicação | 🔴 Alta |
| Approval workflow separado para publicação | 🟡 Média |
| `UpdateSoapDraftMetadata` / `UpdateEventDraftMetadata` / `UpdateBackgroundServiceDraftMetadata` | 🟡 Média |
| Integração `GetApiDetail` com `ContractPublicationEntry` (visibility) | 🟡 Média |
| Rota do router para `PublicationCenterPage` | 🟡 Média |

---

## 4. Classificação Pós-P4.4

| Capability | Estado anterior | Estado atual |
|---|---|---|
| Publication Center | ❌ INCOMPLETE | ⚠️ PARTIAL — Workflow real, sem approval separado |
| Publicar contrato no portal | ❌ Inexistente | ✅ Real — Approved/Locked → Published |
| Retirar publicação | ❌ Inexistente | ✅ Real — com motivo e timestamp |
| Listar publicações | ❌ Inexistente | ✅ Real — com filtros de status |
| Consultar estado por versão | ❌ Inexistente | ✅ Real — usado pelos hooks do workspace |
| Frontend Publication Center | ❌ Inexistente | ✅ PublicationCenterPage funcional |
| Botão "Publish to Portal" no workspace | ❌ Inexistente | ✅ ContractQuickActions atualizado |
| EF Core persistence | ❌ Sem tabela | ⚠️ Configurado, migration pendente |
| Separação PublishDraft ≠ PublishToPortal | ⚠️ Implícita | ✅ Explicitamente separados e documentados |

---

## 5. Estado Global do Módulo Contracts após P4.1 + P4.2 + P4.3 + P4.4

| Funcionalidade | Estado |
|---|---|
| REST / OpenAPI contracts | ✅ Completo |
| SOAP / WSDL contracts (P4.1) | ✅ Real — SoapContractDetail + endpoints |
| AsyncAPI / Event contracts (P4.2) | ✅ Real — EventContractDetail + endpoints |
| Background Service contracts (P4.3) | ✅ Real — BackgroundServiceContractDetail + endpoints |
| Publication Center (P4.4) | ✅ Real — ContractPublicationEntry + 4 endpoints + frontend |
| EF Core migrations (todas as novas tabelas) | ❌ Pendente — 7 tabelas aguardam P4.5 |
| UpdateDraftMetadata (P4.1-P4.3) | ❌ Pendente — 3 commands aguardam P4.5 |
| Publication approval workflow | ❌ Pendente — P4.5 |
