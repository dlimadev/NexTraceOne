# P4.5 — Post-Change Gap Report: Contract Versioning, Compatibility & Validation

**Data:** 2026-03-26  
**Fase:** P4.5 — Fecho enterprise de Versioning, Compatibility & Validation  
**Classificação:** P4_PHASE_COMPLETE_WITH_CONTROLLED_GAPS

---

## 1. O que foi resolvido

| Item | Resolução |
|------|-----------|
| `ContractProtocol.WorkerService` inexistente | Criado como valor 6 no enum |
| Background services usando `OpenApi` como protocolo fallback | `RegisterBackgroundServiceContract` agora usa `WorkerService` |
| Diff silencioso para WorkerService | `WorkerServiceDiffCalculator` implementado com breaking/additive/non-breaking por tipo |
| Modelo canónico vazio para WorkerService | `CanonicalModelBuilder.BuildFromWorkerService()` mapeia service name, trigger, schedule, inputs, outputs |
| `BackgroundServiceSpecParser` ausente | Criado com parsing resiliiente de JSON estruturado |
| SecurityDefinition rule aplicada incorretamente a WSDL e WorkerService | `ContractRuleEngine` agora excui essa regra para esses protocolos |
| Scorecard penalizando WorkerService por ausência de security | `ContractScorecardCalculator` protocol-aware nos 4 scores |
| Frontend sem suporte ao tipo `WorkerService` | `ContractProtocol` TypeScript type atualizado |
| Diff header sem contexto de protocolo | `VersioningSection` exibe protocolo do contrato alvo |
| Cobertura de testes para novas peças | 35 novos testes (607 → 642) |

---

## 2. O que ainda ficou pendente

### 2.1 EF Core migrations

As 7 migrations acumuladas desde P4.1-P4.4 ainda não foram geradas nem aplicadas:
- `ctr_soap_contract_details`, `ctr_soap_draft_metadata`
- `ctr_event_contract_details`, `ctr_event_draft_metadata`
- `ctr_background_service_contract_details`, `ctr_background_service_draft_metadata`
- `cat_portal_contract_publications`

A coluna de protocolo do `ContractVersion` pode precisar de migração de update para registros existentes que usavam `OpenApi` como fallback para BackgroundService.

### 2.2 UpdateDraftMetadata para SOAP, Event e BackgroundService

Os comandos `UpdateDraftMetadata` por tipo ainda não foram implementados (pendente desde P4.1-P4.3).

### 2.3 `ContractEvidencePack` e `ContractReview` — suporte explícito por tipo

Embora o `ContractScorecard` e `ContractRuleEngine` sejam protocol-aware, o `ContractEvidencePack` e o `ContractReview` não têm campos específicos para:
- BackgroundService: trigger histórico, side effects observados, execução evidenciada
- WSDL: WSDL versioning evidence, WS-Security compliance
- AsyncAPI: schema compatibility evidence, consumer impact evidence

### 2.4 `SpectralRuleset` — suporte a WorkerService

O `SpectralRuleset` e o motor de Spectral rules ainda não têm rulesets específicos para WorkerService. Para P4.6+:
- regras de lint para TriggerType, ScheduleExpression, nomes de inputs/outputs
- integração do SpectralRulesetOrigin com WorkerService

### 2.5 Diff visual por protocolo no frontend

O componente `DiffResults` no `VersioningSection` é genérico e exibe listas de mudanças sem distinção visual por protocolo. Para WorkerService, seria mais útil:
- agrupar mudanças por secção (Trigger, Schedule, Inputs, Outputs, SideEffects)
- mostrar ícones/labels semânticos específicos de workers

### 2.6 Protobuf e GraphQL

Esses dois protocolos ainda retornam `EmptyResult()` e `EmptyModel()`. Não eram requisito desta fase.

---

## 3. O que fica explicitamente para P4.6 e fases seguintes

| Item | Prioridade |
|------|-----------|
| EF Core migrations para P4.1-P4.5 | Alta (necessário para deploy) |
| `UpdateDraftMetadata` para SOAP/Event/BackgroundService | Alta |
| EvidencePack e Review com campos específicos por tipo | Média |
| Spectral rulesets para WorkerService | Média |
| Diff visual protocol-aware no Studio | Média |
| Protobuf / GraphQL diff e canonical model | Baixa |
| ContractVersion backfill de protocolo (OpenApi → WorkerService) | Alta (script de migração) |

---

## 4. Limitações residuais

### Backfill de registros existentes
Contratos BackgroundService registados antes desta fase têm `Protocol = OpenApi (0)` na base de dados. Uma migração de backfill será necessária após geração das migrations.

### Canonical model de WorkerService usa SpecVersion para TriggerType
O campo `SpecVersion` de `ContractCanonicalModel` foi reutilizado para armazenar `TriggerType` no modelo de WorkerService. Isso é uma limitação do modelo canónico existente. Idealmente `ContractCanonicalModel` deveria ter um campo `Category` ou `ServiceKind` para este caso.

### WorkerService sem ScheduleExpression em regra W3
A regra W3 usa `model.Description` para verificar se existe ScheduleExpression porque o Description está mapeado para `scheduleExpression` no canonical model. Funciona, mas é implícito.

---

## 5. Resumo

P4.5 fecha a brecha principal de versionamento e compatibilidade enterprise para Background Service Contracts. O módulo Contracts agora suporta diff semântico funcional real para todos os 4 tipos contratuais (REST, SOAP/WSDL, AsyncAPI/Event, BackgroundService/WorkerService), com regras específicas por tipo, scorecard protocol-aware e 642 testes passando.
