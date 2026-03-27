# P4.1 — Post-Change Gap Report

**Data:** 2026-03-26  
**Fase:** P4.1 — Criar workflow real de contratos SOAP/WSDL no módulo Contracts

---

## 1. O Que Foi Resolvido

| Item | Antes | Depois |
|---|---|---|
| `SoapContractDetail` entity | Inexistente | ✅ Criada com invariantes de domínio |
| `SoapDraftMetadata` entity | Inexistente | ✅ Criada para drafts SOAP |
| `WsdlMetadataExtractor` service | Inexistente | ✅ Criado — extrai service name, namespace, SOAP version, endpoint, portType, binding, operações |
| `ImportWsdlContract` handler | Inexistente | ✅ Criado — workflow real de importação com extração de metadados |
| `CreateSoapDraft` handler | Inexistente | ✅ Criado — criação de draft SOAP com SoapDraftMetadata |
| `GetSoapContractDetail` query | Inexistente | ✅ Criado — consulta detalhe SOAP de versão publicada |
| Endpoints SOAP dedicados | Inexistentes | ✅ 3 endpoints: import, create-draft, get-detail |
| `SoapContractDetailRepository` | Inexistente | ✅ Criado |
| `SoapDraftMetadataRepository` | Inexistente | ✅ Criado |
| EF Core: tabelas SOAP | Inexistentes | ✅ ctr_soap_contract_details + ctr_soap_draft_metadata com check constraints |
| ContractsDbContext | Sem DbSets SOAP | ✅ +2 DbSets SOAP |
| Erros de domínio SOAP | Inexistentes | ✅ 5 novos erros específicos |
| Frontend: tipos SOAP | Inexistentes | ✅ SoapContractDetail, WsdlImportResponse, SoapDraftCreateResponse |
| Frontend: API SOAP | Inexistentes | ✅ importWsdl, getSoapContractDetail, createSoapDraft |
| Frontend: hooks SOAP | Inexistentes | ✅ useWsdlImport, useCreateSoapDraft, useSoapContractDetail |
| CreateServicePage SOAP-aware | Genérico para todos os tipos | ✅ Campos SOAP específicos + usa createSoapDraft |
| Testes de workflow SOAP | Inexistentes | ✅ 49 novos testes |

**Diagnóstico pré-P4.1 (INCOMPLETE):** Contract Governance SOAP  
**Diagnóstico pós-P4.1 (PARTIAL → FUNCTIONAL):** Workflow SOAP real — modelo, persistência, handlers, endpoints, frontend mínimo

---

## 2. O Que Ainda Está Pendente

### 2.1 EF Core Migration

A migration do EF Core para criar as tabelas físicas `ctr_soap_contract_details` e `ctr_soap_draft_metadata` ainda **não foi gerada** nesta fase.

**Impacto:** As tabelas não existirão em base de dados até que a migration seja gerada e aplicada.  
**Prioridade:** Alta — necessário para que o workflow funcione em ambiente real.  
**Ação sugerida:** P4.2 deve incluir `dotnet ef migrations add AddSoapContractTables`.

### 2.2 WSDL Import endpoint — deduplica de upload vs URL

O endpoint `ImportWsdlContract` recebe o conteúdo WSDL inline. Não existe ainda:
- Upload de ficheiro WSDL direto
- Fetch de WSDL a partir de URL remota (ex: `http://example.com/service?wsdl`)

**Impacto:** Fluxo de importação por URL não está coberto.  
**Ação sugerida:** P4.2 pode adicionar `WsdlSourceFetcher` service para ingestão via URL.

### 2.3 UpdateSoapDraftMetadata command

Não foi criado um command específico para atualizar o `SoapDraftMetadata` após a criação do draft
(quando o utilizador edita os campos SOAP no studio visual).

**Impacto:** O builder visual (`VisualSoapBuilder`) ainda não persiste actualizações de metadados
no `SoapDraftMetadata`. O conteúdo WSDL é atualizado via `UpdateDraftContent`, mas os metadados
SOAP estruturados ficam com os valores iniciais.  
**Ação sugerida:** P4.2 deve criar `UpdateSoapDraftMetadata` command + handler + endpoint.

### 2.4 Sincronização automática WSDL↔SoapContractDetail

Quando o `specContent` de uma `ContractVersion` com `Protocol=Wsdl` é alterado,
o `SoapContractDetail` associado não é re-extraído automaticamente.

**Impacto:** Após edição do WSDL, os metadados podem ficar desatualizados.  
**Ação sugerida:** Criar `ReSyncWsdlMetadata` command ou integrar com o evento de publicação.

### 2.5 DraftStudioPage — reconhecimento SOAP contextual

O `DraftStudioPage` ainda não exibe os metadados SOAP específicos (service name, namespace, operações, endpoint)
em secção dedicada para drafts com `ContractType=Soap`.

**Impacto:** Usabilidade reduzida — o utilizador vê apenas o spec content XML sem contexto SOAP.  
**Ação sugerida:** P4.2 deve integrar `useSoapContractDetail` / `soapDraftMetadata` no painel de detalhes do Studio.

### 2.6 Diff semântico SOAP contextualizado

O `WsdlDiffCalculator` existe e funciona, mas o resultado do diff ainda não está integrado com
o `SoapContractDetail` para mostrar mudanças de portTypes/operações em contexto SOAP no frontend.

**Impacto:** Diff de contratos WSDL funciona tecnicamente mas não é apresentado com contexto SOAP-específico.  
**Ação sugerida:** P4.2 pode adicionar `WsdlDiffSummary` response tipo-específico.

### 2.7 Scorecard SOAP específico

O `ContractScorecard` é calculado pelo `ContractScorecardCalculator`, mas sem métricas
SOAP-específicas (ex: cobertura de operações, completude de portTypes, presença de endpoint).

**Impacto:** Scorecard genérico — não penaliza contratos WSDL incompletos.  
**Ação sugerida:** Fase futura.

### 2.8 Geração por IA de contrato SOAP

Fora do escopo desta fase (conforme roadmap P4.1).  
**Ação sugerida:** P5.x — Agente de geração de contrato SOAP.

---

## 3. O Que Fica Explicitamente para P4.2

| Item | Prioridade |
|---|---|
| EF Core migration para tabelas SOAP | 🔴 Alta |
| `UpdateSoapDraftMetadata` command + handler + endpoint | 🔴 Alta |
| WSDL fetch via URL remota | 🟡 Média |
| `DraftStudioPage` — painel de metadados SOAP | 🟡 Média |
| Diff SOAP contextualizado no frontend | 🟡 Média |
| Re-sincronização automática de metadados ao editar WSDL | 🟡 Média |
| Scorecard SOAP específico | 🟢 Baixa |

---

## 4. Limitações Residuais após P4.1

| Limitação | Descrição |
|---|---|
| Tabelas não migradas | As tabelas `ctr_soap_contract_details` e `ctr_soap_draft_metadata` existem no modelo mas não na base de dados até a migration ser gerada/aplicada |
| WSDL por URL | Importação via URL remota não suportada nesta fase |
| Studio sem contexto SOAP | DraftStudioPage não exibe metadados SOAP estruturados |
| Metadados de draft estáticos | Após criação, `SoapDraftMetadata` não é atualizado automaticamente quando o WSDL do draft muda |
| IA SOAP | Geração por IA de contrato SOAP não implementada |

---

## 5. Classificação Pós-P4.1

| Capability | Estado anterior | Estado atual |
|---|---|---|
| Contract Governance SOAP | ❌ INCOMPLETE | ⚠️ PARTIAL — Modelo real, workflow mínimo funcional |
| WSDL Import | ❌ Nominal | ✅ Real — com extração de metadados |
| SOAP Draft Creation | ❌ Inexistente | ✅ Real — com SoapDraftMetadata |
| SOAP Detail Query | ❌ Inexistente | ✅ Real — com portTypes, operações, endpoint |
| SOAP Frontend Recognition | ⚠️ Visual Builder existia sem backend real | ✅ CreateServicePage SOAP-aware + API integrada |
| EF Core persistence | ❌ Sem tabelas | ⚠️ Configurado, migration pendente |
