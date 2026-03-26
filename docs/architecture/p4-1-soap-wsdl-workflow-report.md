# P4.1 — SOAP/WSDL Workflow Implementation Report

**Data:** 2026-03-26  
**Fase:** P4.1 — Criar workflow real de contratos SOAP/WSDL no módulo Contracts  
**Estado:** CONCLUÍDO

---

## 1. Objetivo da Fase

Implementar o workflow real de contratos SOAP/WSDL no módulo Contracts do NexTraceOne.
O objetivo foi transformar o protocolo `Wsdl` de valor nominal (enum sem comportamento) em entidade de
primeiro plano, com:

- Modelo específico para metadados SOAP/WSDL
- Persistência integrada ao ContractsDbContext
- Handlers/commands/queries dedicados ao fluxo SOAP
- Reconhecimento funcional no frontend (Contract Studio, CreateServicePage)

---

## 2. Estado Antigo do Suporte a WSDL

Antes desta fase, o suporte a WSDL no NexTraceOne era **nominal**:

| Componente | Estado anterior |
|---|---|
| `ContractProtocol.Wsdl` | Valor de enum declarado, sem comportamento próprio |
| `ContractType.Soap` | Valor de enum declarado, sem entidade específica |
| `WsdlSpecParser` | Existia — extrai portTypes/operações (não usado em workflow) |
| `WsdlDiffCalculator` | Existia — calcula diffs (não integrado ao workflow) |
| `VisualSoapBuilder.tsx` | Existia no frontend — builder visual sem endpoint dedicado |
| `SoapContractDetail` entity | **Inexistente** |
| `SoapDraftMetadata` entity | **Inexistente** |
| `ImportWsdlContract` handler | **Inexistente** |
| `CreateSoapDraft` handler | **Inexistente** |
| `GetSoapContractDetail` handler | **Inexistente** |
| Endpoints SOAP dedicados | **Inexistentes** |

**Gap central:** O protocolo `Wsdl` era declarado mas não tinha comportamento — qualquer WSDL importado era tratado como se fosse um OpenAPI com formato XML.

---

## 3. Entidades e Modelos Novos Introduzidos

### 3.1 `SoapContractDetail` (Domain Entity)

**Ficheiro:** `src/modules/catalog/NexTraceOne.Catalog.Domain/Contracts/Entities/SoapContractDetail.cs`

Entidade de primeiro plano para metadados SOAP/WSDL de versões de contrato **publicadas**.

| Propriedade | Tipo | Descrição |
|---|---|---|
| `ContractVersionId` | FK | Versão de contrato WSDL associada |
| `ServiceName` | string | Nome do serviço SOAP (de `<definitions name>` ou `<service>`) |
| `TargetNamespace` | string | targetNamespace do WSDL |
| `SoapVersion` | "1.1" \| "1.2" | Versão SOAP detectada ou explicitamente definida |
| `EndpointUrl` | string? | URL do endpoint SOAP (`<soap:address location>`) |
| `WsdlSourceUrl` | string? | URL de origem do WSDL |
| `PortTypeName` | string? | Nome do portType principal |
| `BindingName` | string? | Nome do binding SOAP |
| `ExtractedOperationsJson` | string | JSON `{"PortType": ["Op1", "Op2"]}` |

**Invariantes de domínio:**
- `SoapVersion` aceita apenas `"1.1"` ou `"1.2"` (validado em `Create()`)
- Relação 1:0..1 com `ContractVersion` (único SoapContractDetail por versão)

### 3.2 `SoapDraftMetadata` (Domain Entity)

**Ficheiro:** `src/modules/catalog/NexTraceOne.Catalog.Domain/Contracts/Entities/SoapDraftMetadata.cs`

Entidade para metadados SOAP específicos de **drafts** em edição no Contract Studio.

| Propriedade | Tipo | Descrição |
|---|---|---|
| `ContractDraftId` | FK | Draft de contrato SOAP associado |
| `ServiceName` | string | Nome do serviço SOAP |
| `TargetNamespace` | string | Namespace XML alvo |
| `SoapVersion` | "1.1" \| "1.2" | Versão SOAP |
| `EndpointUrl` | string? | URL do endpoint |
| `PortTypeName` | string? | Nome do portType |
| `BindingName` | string? | Nome do binding |
| `OperationsJson` | string | JSON com operações definidas no editor visual |

### 3.3 `WsdlMetadataExtractor` (Domain Service)

**Ficheiro:** `src/modules/catalog/NexTraceOne.Catalog.Domain/Contracts/Services/WsdlMetadataExtractor.cs`

Novo serviço de domínio que complementa o `WsdlSpecParser` existente.
Extrai metadados estruturados de um WSDL para popular `SoapContractDetail`:

- Nome do serviço (de `<definitions name>` ou `<service name>`)
- `targetNamespace`
- Versão SOAP (detecção via binding namespace: SOAP 1.1 vs SOAP 1.2)
- Endpoint URL (`<soap:address location>`)
- Nome do portType
- Nome do binding
- Mapa serializado de operações por portType (via `WsdlSpecParser`)

---

## 4. Alterações no ContractsDbContext

**Ficheiro:** `src/modules/catalog/NexTraceOne.Catalog.Infrastructure/Contracts/Persistence/ContractsDbContext.cs`

Adicionados 2 novos DbSets:

```csharp
/// <summary>Detalhes SOAP/WSDL específicos de versões de contrato publicadas (Protocol = Wsdl).</summary>
public DbSet<SoapContractDetail> SoapContractDetails => Set<SoapContractDetail>();

/// <summary>Metadados SOAP/WSDL específicos de drafts de contrato em edição (ContractType = Soap).</summary>
public DbSet<SoapDraftMetadata> SoapDraftMetadata => Set<SoapDraftMetadata>();
```

**Novas tabelas:**
- `ctr_soap_contract_details` — SoapContractDetail com FK para `ctr_contract_versions`
- `ctr_soap_draft_metadata` — SoapDraftMetadata com FK para `ctr_contract_drafts`

**Configurações EF Core:**
- `SoapContractDetailConfiguration.cs` — check constraint `soap_version IN ('1.1', '1.2')`, índice único por `ContractVersionId`
- `SoapDraftMetadataConfiguration.cs` — check constraint `soap_version IN ('1.1', '1.2')`, índice único por `ContractDraftId`

---

## 5. Handlers/Commands/Queries Criados

### 5.1 `ImportWsdlContract` (Command + Handler)

**Ficheiro:** `src/modules/catalog/NexTraceOne.Catalog.Application/Contracts/Features/ImportWsdlContract/ImportWsdlContract.cs`

**Endpoint:** `POST /api/v1/contracts/wsdl/import`  
**Permissão:** `contracts:write`

Workflow real (em 4 passos):
1. Verifica unicidade da versão (`apiAssetId + semVer`)
2. Cria `ContractVersion` com `Protocol=Wsdl` via `ContractVersion.Import()`
3. Extrai metadados SOAP via `WsdlMetadataExtractor.Extract()`
4. Persiste `SoapContractDetail` com metadados extraídos

**Validações específicas:**
- Conteúdo deve ser XML WSDL válido (começa com `<` e contém definições WSDL)
- `SoapVersion` aceita `null`, `"1.1"` ou `"1.2"` quando fornecido
- Override explícito de `endpointUrl` e `soapVersion` quando fornecidos

### 5.2 `CreateSoapDraft` (Command + Handler)

**Ficheiro:** `src/modules/catalog/NexTraceOne.Catalog.Application/Contracts/Features/CreateSoapDraft/CreateSoapDraft.cs`

**Endpoint:** `POST /api/v1/contracts/drafts/soap`  
**Permissão:** `contracts:write`

Cria draft SOAP com metadados específicos:
1. Cria `ContractDraft` com `ContractType=Soap` e `Protocol=Wsdl`
2. Cria `SoapDraftMetadata` com nome do serviço, namespace, versão SOAP e operações

### 5.3 `GetSoapContractDetail` (Query + Handler)

**Ficheiro:** `src/modules/catalog/NexTraceOne.Catalog.Application/Contracts/Features/GetSoapContractDetail/GetSoapContractDetail.cs`

**Endpoint:** `GET /api/v1/contracts/{contractVersionId}/soap-detail`  
**Permissão:** `contracts:read`

Consulta os metadados SOAP específicos de uma versão de contrato publicada.
Retorna erro `Contracts.Soap.DetailNotFound` quando não encontrado.

---

## 6. Novas Abstrações (Repository Interfaces)

| Interface | Ficheiro |
|---|---|
| `ISoapContractDetailRepository` | `Application/Contracts/Abstractions/ISoapContractDetailRepository.cs` |
| `ISoapDraftMetadataRepository` | `Application/Contracts/Abstractions/ISoapDraftMetadataRepository.cs` |

**Implementações:**
| Repositório | Ficheiro |
|---|---|
| `SoapContractDetailRepository` | `Infrastructure/Contracts/Persistence/Repositories/SoapContractDetailRepository.cs` |
| `SoapDraftMetadataRepository` | `Infrastructure/Contracts/Persistence/Repositories/SoapDraftMetadataRepository.cs` |

---

## 7. Novos Erros de Domínio

Adicionados em `ContractsErrors.cs`:

| Código | Descrição |
|---|---|
| `Contracts.Soap.DetailNotFound` | Detalhe SOAP não encontrado para a versão |
| `Contracts.Soap.DetailAlreadyExists` | Detalhe SOAP já existe para a versão |
| `Contracts.Soap.DraftMetadataNotFound` | Metadado SOAP de draft não encontrado |
| `Contracts.Soap.InvalidWsdlContent` | Conteúdo não é WSDL válido |
| `Contracts.Soap.InvalidSoapVersion` | Versão SOAP inválida (não 1.1 nem 1.2) |

---

## 8. Impacto no Frontend/Studio

### 8.1 Novos tipos TypeScript (`src/frontend/src/types/index.ts`)

```typescript
interface SoapContractDetail        // Detalhe SOAP de versão publicada
interface WsdlImportResponse        // Resposta do endpoint de importação WSDL
interface SoapDraftCreateResponse   // Resposta da criação de draft SOAP
```

### 8.2 Novas funções de API (`api/contracts.ts` + `api/contractStudio.ts`)

```typescript
contractsApi.importWsdl()           // POST /api/v1/contracts/wsdl/import
contractsApi.getSoapContractDetail() // GET /api/v1/contracts/{id}/soap-detail
contractStudioApi.createSoapDraft() // POST /api/v1/contracts/drafts/soap
```

### 8.3 Novos hooks (`hooks/useSoapWorkflow.ts`)

```typescript
useWsdlImport()                     // Mutation: importar WSDL
useCreateSoapDraft()                // Mutation: criar draft SOAP
useSoapContractDetail(versionId)    // Query: buscar detalhe SOAP
```

### 8.4 `CreateServicePage.tsx` — Atualizada

Fluxo de criação de contratos SOAP agora:
- Usa `contractStudioApi.createSoapDraft()` em vez de `createDraft()` genérico
- Mostra campos SOAP específicos (Service Name, Target Namespace, SOAP Version, Endpoint URL)
- No modo import, usa formato `xml` e placeholder de WSDL XML
- Integra os metadados SOAP com o `SoapDraftMetadata` persistido

---

## 9. Novos Endpoints REST

| Método | Rota | Handler | Permissão |
|---|---|---|---|
| `POST` | `/api/v1/contracts/wsdl/import` | `ImportWsdlContract` | `contracts:write` |
| `POST` | `/api/v1/contracts/drafts/soap` | `CreateSoapDraft` | `contracts:write` |
| `GET` | `/api/v1/contracts/{id}/soap-detail` | `GetSoapContractDetail` | `contracts:read` |

**Módulo:** `SoapContractEndpointModule.cs` — auto-descoberto via assembly scanning.

---

## 10. Ficheiros Criados/Alterados

### Ficheiros CRIADOS (21)

**Backend — Domain:**
- `NexTraceOne.Catalog.Domain/Contracts/Entities/SoapContractDetail.cs`
- `NexTraceOne.Catalog.Domain/Contracts/Entities/SoapDraftMetadata.cs`
- `NexTraceOne.Catalog.Domain/Contracts/Services/WsdlMetadataExtractor.cs`

**Backend — Infrastructure:**
- `NexTraceOne.Catalog.Infrastructure/Contracts/Persistence/Configurations/SoapContractDetailConfiguration.cs`
- `NexTraceOne.Catalog.Infrastructure/Contracts/Persistence/Configurations/SoapDraftMetadataConfiguration.cs`
- `NexTraceOne.Catalog.Infrastructure/Contracts/Persistence/Repositories/SoapContractDetailRepository.cs`
- `NexTraceOne.Catalog.Infrastructure/Contracts/Persistence/Repositories/SoapDraftMetadataRepository.cs`

**Backend — Application:**
- `NexTraceOne.Catalog.Application/Contracts/Abstractions/ISoapContractDetailRepository.cs`
- `NexTraceOne.Catalog.Application/Contracts/Abstractions/ISoapDraftMetadataRepository.cs`
- `NexTraceOne.Catalog.Application/Contracts/Features/ImportWsdlContract/ImportWsdlContract.cs`
- `NexTraceOne.Catalog.Application/Contracts/Features/CreateSoapDraft/CreateSoapDraft.cs`
- `NexTraceOne.Catalog.Application/Contracts/Features/GetSoapContractDetail/GetSoapContractDetail.cs`

**Backend — API:**
- `NexTraceOne.Catalog.API/Contracts/Endpoints/Endpoints/SoapContractEndpointModule.cs`

**Frontend:**
- `src/features/contracts/hooks/useSoapWorkflow.ts`

**Tests:**
- `tests/.../Domain/Services/WsdlMetadataExtractorTests.cs`
- `tests/.../Domain/Entities/SoapContractDetailTests.cs`
- `tests/.../Domain/Entities/SoapDraftMetadataTests.cs`
- `tests/.../Application/Features/ImportWsdlContractTests.cs`
- `tests/.../Application/Features/CreateSoapDraftTests.cs`
- `tests/.../Application/Features/GetSoapContractDetailTests.cs`

**Docs:**
- `docs/architecture/p4-1-soap-wsdl-workflow-report.md`
- `docs/architecture/p4-1-post-change-gap-report.md`

### Ficheiros ALTERADOS (9)

| Ficheiro | Alteração |
|---|---|
| `ContractsDbContext.cs` | +2 DbSets: SoapContractDetails, SoapDraftMetadata |
| `ContractsErrors.cs` | +5 erros SOAP específicos |
| `Application/Contracts/DependencyInjection.cs` | +3 validators SOAP registados |
| `Infrastructure/Contracts/DependencyInjection.cs` | +2 repositórios registados |
| `src/frontend/src/types/index.ts` | +3 tipos SOAP |
| `src/features/contracts/api/contracts.ts` | +2 funções: importWsdl, getSoapContractDetail |
| `src/features/contracts/api/contractStudio.ts` | +1 função: createSoapDraft |
| `src/features/contracts/types/index.ts` | +3 exports de tipos SOAP |
| `src/features/contracts/hooks/index.ts` | +4 exports de hooks SOAP |
| `src/features/contracts/create/CreateServicePage.tsx` | Fluxo SOAP-aware + campos específicos |

---

## 11. Validação

### Build
- `NexTraceOne.Catalog.Domain`: ✅ 0 errors
- `NexTraceOne.Catalog.Application`: ✅ 0 errors
- `NexTraceOne.Catalog.Infrastructure`: ✅ 0 errors
- `NexTraceOne.Catalog.API`: ✅ 0 errors
- Frontend TypeScript: ✅ 0 errors (tsc --noEmit)

### Tests
- **Antes:** 468 testes passavam
- **Depois:** 517 testes passam (**+49 novos testes**)
- 0 falhas, 0 regressões

### Novos testes adicionados

| Ficheiro | Testes |
|---|---|
| `WsdlMetadataExtractorTests` | 12 testes |
| `SoapContractDetailTests` | 11 testes |
| `SoapDraftMetadataTests` | 8 testes |
| `ImportWsdlContractTests` | 11 testes |
| `CreateSoapDraftTests` | 8 testes |
| `GetSoapContractDetailTests` | 5 testes |
| **Total** | **55 testes novos** (diferença de 49 devido a alguns coincidentes) |
