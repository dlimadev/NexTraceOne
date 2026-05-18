# Cenários de Teste — Módulo Catalog

> **Projeto:** NexTraceOne  
> **Módulo:** Catalog  
> **Versão do Documento:** 1.0  
> **Data:** 2026-05-18  
> **Responsável:** QA Engineering  
> **Framework:** xUnit + FluentAssertions + NSubstitute  

---

## Convenções

| Tipo | Descrição |
|------|-----------|
| **Unitário** | Handler testado em isolamento com substitutos (NSubstitute) |
| **Integração** | Handler + EF Core InMemory ou banco real |
| **Contrato** | Validação de schemas e regras de negócio de contrato |
| **Segurança** | Isolamento de tenant, autorização, vazamento de dados |

**Prioridades:** Crítica > Alta > Média > Baixa

---

## 1. Contratos — Criação de Rascunhos

### TC-CAT-001 — Criar rascunho REST com dados válidos

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | CreateDraft |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Handler** | `CreateDraft.Handler` |

**Pré-condições:**
- Tenant autenticado com capability `contract_studio`
- Repositório de rascunhos disponível

**Passos:**
1. Instanciar handler com `IDraftRepository` substituto e `ICurrentTenant` válido
2. Enviar `CreateDraft.Command` com `Title = "Pagamentos API"`, `Protocol = OpenApi`, `OwnerServiceId` válido
3. Verificar que `IDraftRepository.AddAsync` foi invocado uma vez
4. Verificar que `IUnitOfWork.CommitAsync` foi invocado

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.DraftId` é um `Guid` não-nulo

**Critério de Aceite:** `result.IsSuccess == true && result.Value.DraftId != Guid.Empty`

---

### TC-CAT-002 — Criar rascunho com título vazio deve falhar na validação

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | CreateDraft / Validator |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `CreateDraft.Validator` |

**Pré-condições:**
- Nenhuma dependência de infraestrutura necessária

**Passos:**
1. Instanciar `CreateDraft.Validator`
2. Chamar `Validate` com `Command(Title = "", Protocol = OpenApi, OwnerServiceId = Guid.NewGuid())`
3. Inspecionar `ValidationResult.Errors`

**Resultado Esperado:**
- `ValidationResult.IsValid == false`
- Erro aponta para propriedade `Title`

**Critério de Aceite:** `errors.Any(e => e.PropertyName == "Title")`

---

### TC-CAT-003 — Criar rascunho de evento assíncrono (AsyncAPI)

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | CreateEventDraft |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `CreateEventDraft.Handler` |

**Pré-condições:**
- Tenant com capability `contract_studio`
- `Protocol = AsyncApi` suportado

**Passos:**
1. Enviar `CreateEventDraft.Command` com `EventType = "OrderPlaced"`, `Protocol = AsyncApi`
2. Verificar que o rascunho é persistido com `DraftType = EventDraft`
3. Verificar que `SemVer` inicial é `"0.1.0"`

**Resultado Esperado:**
- Rascunho criado com `Protocol == AsyncApi`
- `result.IsSuccess == true`

**Critério de Aceite:** `result.Value.Protocol == "AsyncApi"`

---

### TC-CAT-004 — Criar rascunho SOAP

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | CreateSoapDraft |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `CreateSoapDraft.Handler` |

**Pré-condições:**
- Tenant com capability `contract_studio`

**Passos:**
1. Enviar `CreateSoapDraft.Command` com `WsdlContent` preenchido, `ServiceName = "LegacyBillingWS"`
2. Verificar que o rascunho é salvo com `Protocol = Wsdl`
3. Verificar que o conteúdo WSDL é preservado

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.Protocol == "Wsdl"`

**Critério de Aceite:** `result.IsSuccess == true`

---

### TC-CAT-005 — Criar rascunho de Background Service

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | CreateBackgroundServiceDraft |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `CreateBackgroundServiceDraft.Handler` |

**Pré-condições:**
- Tenant autenticado

**Passos:**
1. Enviar `CreateBackgroundServiceDraft.Command` com `Name = "StockSyncJob"`, `Schedule = "0 */6 * * *"`
2. Verificar que o rascunho é criado com `DraftType = BackgroundService`

**Resultado Esperado:**
- `result.IsSuccess == true`

**Critério de Aceite:** `result.Value.DraftId != Guid.Empty`

---

### TC-CAT-006 — Atualizar conteúdo do rascunho

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | UpdateDraftContent |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `UpdateDraftContent.Handler` |

**Pré-condições:**
- Rascunho existente no repositório com status `Draft`

**Passos:**
1. Configurar substituto de `IDraftRepository.GetByIdAsync` para retornar rascunho válido
2. Enviar `UpdateDraftContent.Command` com novo `SpecContent` (YAML OpenAPI)
3. Verificar que `IDraftRepository` recebeu chamada de atualização
4. Verificar commit

**Resultado Esperado:**
- `result.IsSuccess == true`
- Conteúdo da spec foi atualizado no agregado

**Critério de Aceite:** `result.IsSuccess == true`

---

### TC-CAT-007 — Atualizar metadados do rascunho

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | UpdateDraftMetadata |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `UpdateDraftMetadata.Handler` |

**Pré-condições:**
- Rascunho existente no repositório

**Passos:**
1. Enviar `UpdateDraftMetadata.Command` com `Description = "Nova descrição"`, `Tags = ["payments", "v2"]`
2. Verificar que os metadados foram atualizados no agregado

**Resultado Esperado:**
- `result.IsSuccess == true`
- Metadados persistidos corretamente

**Critério de Aceite:** `result.IsSuccess == true`

---

### TC-CAT-008 — Atualizar rascunho inexistente deve retornar NotFound

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | UpdateDraftContent |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `UpdateDraftContent.Handler` |

**Pré-condições:**
- `IDraftRepository.GetByIdAsync` retorna `null` para o ID fornecido

**Passos:**
1. Configurar substituto para retornar `null`
2. Enviar `UpdateDraftContent.Command` com `DraftId = Guid.NewGuid()`

**Resultado Esperado:**
- `result.IsFailure == true`
- `result.Error.Type == ErrorType.NotFound`

**Critério de Aceite:** `result.Error.Type == ErrorType.NotFound`

---

## 2. Contratos — Fluxo de Revisão e Aprovação

### TC-CAT-009 — Submeter rascunho para revisão

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | SubmitDraftForReview |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Handler** | `SubmitDraftForReview.Handler` |

**Pré-condições:**
- Rascunho em estado `Draft` com conteúdo de spec preenchido

**Passos:**
1. Configurar rascunho com `LifecycleState = Draft`
2. Enviar `SubmitDraftForReview.Command` com `DraftId` válido
3. Verificar transição de estado para `UnderReview`

**Resultado Esperado:**
- `result.IsSuccess == true`
- Estado do rascunho transicionou para `UnderReview`

**Critério de Aceite:** `result.Value.LifecycleState == "UnderReview"`

---

### TC-CAT-010 — Aprovar rascunho em revisão

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | ApproveDraft |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Handler** | `ApproveDraft.Handler` |

**Pré-condições:**
- Rascunho em estado `UnderReview`

**Passos:**
1. Configurar rascunho com `LifecycleState = UnderReview`
2. Enviar `ApproveDraft.Command` com `ReviewId` e `Comment = "Aprovado conforme padrão REST"`
3. Verificar transição de estado para `Approved`

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.LifecycleState == "Approved"`

**Critério de Aceite:** `result.IsSuccess == true`

---

### TC-CAT-011 — Rejeitar rascunho em revisão

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | RejectDraft |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `RejectDraft.Handler` |

**Pré-condições:**
- Rascunho em estado `UnderReview`

**Passos:**
1. Enviar `RejectDraft.Command` com `Reason = "Falta documentação dos erros 4xx"`
2. Verificar transição de estado para `Rejected`
3. Verificar que o motivo de rejeição foi persistido

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.LifecycleState == "Rejected"`

**Critério de Aceite:** `result.Value.LifecycleState == "Rejected"`

---

### TC-CAT-012 — Publicar rascunho aprovado

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | PublishDraft |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Handler** | `PublishDraft.Handler` |

**Pré-condições:**
- Rascunho em estado `Approved`
- Config `catalog.contract.creation.approval_required = true`

**Passos:**
1. Configurar rascunho com `LifecycleState = Approved`
2. Enviar `PublishDraft.Command`
3. Verificar transição para `Published`
4. Verificar que evento de integração `ContractPublishedIntegrationEvent` foi emitido

**Resultado Esperado:**
- `result.IsSuccess == true`
- Evento publicado no `IEventBus`

**Critério de Aceite:** `result.IsSuccess == true`

---

### TC-CAT-013 — Tentar publicar rascunho não-aprovado deve falhar

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | PublishDraft |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `PublishDraft.Handler` |

**Pré-condições:**
- Rascunho em estado `Draft` (não aprovado)
- Config `catalog.contract.creation.approval_required = true`

**Passos:**
1. Configurar rascunho com `LifecycleState = Draft`
2. Enviar `PublishDraft.Command`

**Resultado Esperado:**
- `result.IsFailure == true`
- Erro do tipo `Business` ou `Validation`

**Critério de Aceite:** `result.IsFailure == true`

---

### TC-CAT-014 — Listar rascunhos do tenant

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | ListDrafts |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `ListDrafts.Handler` |

**Pré-condições:**
- Dois rascunhos pertencentes ao tenant A; um rascunho ao tenant B

**Passos:**
1. Configurar `ICurrentTenant.Id = tenantA`
2. Configurar substituto de repositório para retornar apenas os dois rascunhos do tenant A
3. Enviar `ListDrafts.Query` sem filtros
4. Verificar que apenas 2 rascunhos são retornados

**Resultado Esperado:**
- `result.Value.Items.Count == 2`
- Nenhum item pertence ao tenant B

**Critério de Aceite:** `result.Value.Items.All(d => d.TenantId == tenantA.Id)`

---

### TC-CAT-015 — Obter rascunho por ID

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | GetDraft |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `GetDraft.Handler` |

**Pré-condições:**
- Rascunho persistido com ID conhecido

**Passos:**
1. Configurar repositório para retornar rascunho com ID `draftId`
2. Enviar `GetDraft.Query(draftId)`
3. Verificar campos retornados

**Resultado Esperado:**
- `result.Value.DraftId == draftId`
- Campos `Title`, `Protocol`, `LifecycleState` presentes

**Critério de Aceite:** `result.IsSuccess == true && result.Value.DraftId == draftId`

---

### TC-CAT-016 — Obter rascunho de outro tenant deve retornar NotFound

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | GetDraft |
| **Tipo** | Segurança |
| **Prioridade** | Crítica |
| **Handler** | `GetDraft.Handler` |

**Pré-condições:**
- Rascunho pertence ao tenant B
- Usuário autenticado no tenant A

**Passos:**
1. Configurar `ICurrentTenant.Id = tenantA`
2. Configurar repositório para retornar `null` (filtro RLS ativo)
3. Enviar `GetDraft.Query(draftIdDeTenantB)`

**Resultado Esperado:**
- `result.IsFailure == true`
- `result.Error.Type == ErrorType.NotFound`

**Critério de Aceite:** `result.Error.Type == ErrorType.NotFound`

---

## 3. Contratos — Versionamento e Ciclo de Vida

### TC-CAT-017 — Criar nova versão de contrato publicado

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | CreateContractVersion |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Handler** | `CreateContractVersion.Handler` |

**Pré-condições:**
- Contrato `Published` existente com versão `1.0.0`

**Passos:**
1. Enviar `CreateContractVersion.Command` com `ApiAssetId` do contrato e `SemVer = "2.0.0"`
2. Verificar que nova versão é criada com `SemVer = "2.0.0"`
3. Verificar que versão anterior permanece `Published`

**Resultado Esperado:**
- `result.IsSuccess == true`
- Nova versão com `SemVer = "2.0.0"` e `LifecycleState = Draft`

**Critério de Aceite:** `result.Value.SemVer == "2.0.0"`

---

### TC-CAT-018 — Transicionar estado de ciclo de vida do contrato

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | TransitionLifecycleState |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `TransitionLifecycleState.Handler` |

**Pré-condições:**
- Versão de contrato em estado `Published`

**Passos:**
1. Enviar `TransitionLifecycleState.Command` com `TargetState = Deprecated`
2. Verificar que estado transicionou para `Deprecated`

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.LifecycleState == "Deprecated"`

**Critério de Aceite:** `result.IsSuccess == true`

---

### TC-CAT-019 — Bloquear versão de contrato

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | LockContractVersion |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `LockContractVersion.Handler` |

**Pré-condições:**
- Versão de contrato em estado `Published` e não bloqueada

**Passos:**
1. Enviar `LockContractVersion.Command` com `ContractVersionId` e `Reason = "Auditoria fiscal Q4"`
2. Verificar que versão está bloqueada para edição

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.IsLocked == true`

**Critério de Aceite:** `result.Value.IsLocked == true`

---

### TC-CAT-020 — Assinar versão de contrato

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | SignContractVersion |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `SignContractVersion.Handler` |

**Pré-condições:**
- Versão de contrato em estado `Approved`
- Chave de assinatura configurada

**Passos:**
1. Enviar `SignContractVersion.Command` com `ContractVersionId` e credenciais de assinatura
2. Verificar que `Signature` e `SignedAt` são preenchidos

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.Signature` não está vazio

**Critério de Aceite:** `result.Value.Signature != null`

---

### TC-CAT-021 — Verificar assinatura de contrato válida

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | VerifySignature |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `VerifySignature.Handler` |

**Pré-condições:**
- Versão de contrato assinada com assinatura válida

**Passos:**
1. Enviar `VerifySignature.Query` com `ContractVersionId`
2. Verificar que a assinatura é validada com sucesso

**Resultado Esperado:**
- `result.Value.IsValid == true`

**Critério de Aceite:** `result.Value.IsValid == true`

---

### TC-CAT-022 — Verificar assinatura de contrato adulterada

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | VerifySignature |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `VerifySignature.Handler` |

**Pré-condições:**
- Versão de contrato com `SpecContent` modificado após assinatura

**Passos:**
1. Modificar `SpecContent` da versão assinada no repositório
2. Enviar `VerifySignature.Query`

**Resultado Esperado:**
- `result.Value.IsValid == false`
- Motivo de falha descrito em `result.Value.FailureReason`

**Critério de Aceite:** `result.Value.IsValid == false`

---

### TC-CAT-023 — Iniciar depreciação de contrato

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | InitiateContractDeprecation |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `InitiateContractDeprecation.Handler` |

**Pré-condições:**
- Versão de contrato em estado `Published`

**Passos:**
1. Enviar `InitiateContractDeprecation.Command` com `ContractVersionId` e `Reason = "Substituída pela v3"`
2. Verificar que estado transicionou para `DeprecationPending`

**Resultado Esperado:**
- `result.IsSuccess == true`

**Critério de Aceite:** `result.IsSuccess == true`

---

### TC-CAT-024 — Agendar depreciação futura

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | ScheduleContractDeprecation |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `ScheduleContractDeprecation.Handler` |

**Pré-condições:**
- Versão de contrato `Published`
- `IDeprecationScheduleRepository` disponível

**Passos:**
1. Enviar `ScheduleContractDeprecation.Command` com `DeprecateAt = now + 90 dias`
2. Verificar que agendamento é persistido com data correta

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.DeprecateAt` corresponde à data fornecida

**Critério de Aceite:** `result.Value.DeprecateAt > DateTimeOffset.UtcNow`

---

### TC-CAT-025 — Publicar contrato no marketplace

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | PublishToMarketplace |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `PublishToMarketplace.Handler` |

**Pré-condições:**
- Versão de contrato `Published` com exemplos e sem violações de lint

**Passos:**
1. Enviar `PublishToMarketplace.Command` com `ContractVersionId`
2. Verificar que `MarketplacePublishedAt` é preenchido

**Resultado Esperado:**
- `result.IsSuccess == true`
- Contrato visível no marketplace

**Critério de Aceite:** `result.Value.PublishedToMarketplace == true`

---

### TC-CAT-026 — Pesquisar contratos no marketplace

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | SearchMarketplace |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `SearchMarketplace.Handler` |

**Pré-condições:**
- 3 contratos publicados no marketplace, 2 com tag `payments`

**Passos:**
1. Enviar `SearchMarketplace.Query` com `Tags = ["payments"]`
2. Verificar que apenas 2 resultados são retornados

**Resultado Esperado:**
- `result.Value.Items.Count == 2`

**Critério de Aceite:** `result.Value.Items.All(c => c.Tags.Contains("payments"))`

---

## 4. Contratos — Análise Semântica e Breaking Changes

### TC-CAT-027 — Computar diff semântico entre versões compatíveis (non-breaking)

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | ComputeSemanticDiff |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Handler** | `ComputeSemanticDiff.Handler` |

**Pré-condições:**
- Versão base `1.0.0` com endpoint `GET /orders`
- Versão alvo `1.1.0` adiciona endpoint `GET /orders/{id}` (additive)
- Ambas com mesmo `Protocol = OpenApi`

**Passos:**
1. Configurar repositório para retornar as duas versões
2. Enviar `ComputeSemanticDiff.Query(BaseVersionId, TargetVersionId)`
3. Verificar `ChangeLevel = Additive`
4. Verificar `SuggestedSemVer` segue bump minor (`1.1.0` → `1.2.0` se base for `1.1.0`)

**Resultado Esperado:**
- `result.Value.ChangeLevel == ChangeLevel.Additive`
- `result.Value.BreakingChanges` é lista vazia
- `result.Value.AdditiveChanges.Count > 0`
- Evento `BreakingChangeDetectedIntegrationEvent` NÃO foi publicado

**Critério de Aceite:** `result.Value.ChangeLevel == Additive && result.Value.BreakingChanges.Count == 0`

---

### TC-CAT-028 — Detectar breaking change (campo obrigatório removido)

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | ComputeSemanticDiff |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Handler** | `ComputeSemanticDiff.Handler` |

**Pré-condições:**
- Versão base contém campo obrigatório `customerId` no request body
- Versão alvo remove campo `customerId`

**Passos:**
1. Enviar `ComputeSemanticDiff.Query(BaseVersionId, TargetVersionId)`
2. Verificar `ChangeLevel = Breaking`
3. Verificar que evento `BreakingChangeDetectedIntegrationEvent` foi publicado via `IEventBus`
4. Verificar que `SuggestedSemVer` fez bump major

**Resultado Esperado:**
- `result.Value.ChangeLevel == ChangeLevel.Breaking`
- `result.Value.BreakingChanges.Count >= 1`
- Descrição do breaking change menciona `customerId`

**Critério de Aceite:** `result.Value.ChangeLevel == Breaking && eventBus.ReceivedPublishAsync()`

---

### TC-CAT-029 — Rejeitar diff entre protocolos diferentes

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | ComputeSemanticDiff |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `ComputeSemanticDiff.Handler` |

**Pré-condições:**
- Versão base com `Protocol = OpenApi`
- Versão alvo com `Protocol = AsyncApi`

**Passos:**
1. Enviar `ComputeSemanticDiff.Query(BaseVersionId, TargetVersionId)`

**Resultado Esperado:**
- `result.IsFailure == true`
- Erro indica `ProtocolMismatchForDiff`

**Critério de Aceite:** `result.Error.Code == "PROTOCOL_MISMATCH_FOR_DIFF"`

---

### TC-CAT-030 — Analisar evolução de schema

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | AnalyzeSchemaEvolution |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `AnalyzeSchemaEvolution.Handler` |

**Pré-condições:**
- Contrato com 3 versões publicadas em sequência

**Passos:**
1. Enviar `AnalyzeSchemaEvolution.Query` com `ApiAssetId`
2. Verificar que o histórico de evolução inclui todas as 3 versões
3. Verificar que breaking changes são identificados corretamente

**Resultado Esperado:**
- `result.Value.Versions.Count == 3`
- Timeline de evolução está em ordem cronológica

**Critério de Aceite:** `result.IsSuccess == true && result.Value.Versions.Count == 3`

---

### TC-CAT-031 — Classificar breaking change proposto

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | ClassifyBreakingChange |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `ClassifyBreakingChange.Handler` |

**Pré-condições:**
- Mudança candidata: remoção de endpoint

**Passos:**
1. Enviar `ClassifyBreakingChange.Command` com descrição da mudança
2. Verificar que a classificação retorna `Severity = Critical`

**Resultado Esperado:**
- `result.Value.Severity == "Critical"`
- Impacto estimado descrito

**Critério de Aceite:** `result.IsSuccess == true`

---

### TC-CAT-032 — Propor breaking change com justificativa

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | ProposeBreakingChange |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `ProposeBreakingChange.Handler` |

**Pré-condições:**
- Contrato `Published` existente

**Passos:**
1. Enviar `ProposeBreakingChange.Command` com `ContractVersionId`, `Description` e `MigrationGuide`
2. Verificar que proposta é criada com status `Pending`

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.Status == "Pending"`

**Critério de Aceite:** `result.Value.Status == "Pending"`

---

### TC-CAT-033 — Responder proposta de breaking change — Aprovação

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | RespondToBreakingChangeProposal |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `RespondToBreakingChangeProposal.Handler` |

**Pré-condições:**
- Proposta de breaking change em status `Pending`

**Passos:**
1. Enviar `RespondToBreakingChangeProposal.Command` com `Decision = "Approved"` e `Comments`
2. Verificar status atualizado para `Approved`

**Resultado Esperado:**
- `result.Value.Status == "Approved"`

**Critério de Aceite:** `result.IsSuccess == true`

---

### TC-CAT-034 — Detectar drift de contrato

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | DetectContractDrift |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `DetectContractDrift.Handler` |

**Pré-condições:**
- Spec publicada v1.0.0
- Spec em produção diverge (campo adicional não documentado detectado)

**Passos:**
1. Enviar `DetectContractDrift.Query` com `ContractVersionId`
2. Verificar que drift é detectado

**Resultado Esperado:**
- `result.Value.HasDrift == true`
- Lista de divergências não está vazia

**Critério de Aceite:** `result.Value.HasDrift == true`

---

### TC-CAT-035 — Validar integridade de contrato

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | ValidateContractIntegrity |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `ValidateContractIntegrity.Handler` |

**Pré-condições:**
- Versão de contrato com `SpecContent` sintaticamente inválido (JSON malformado)

**Passos:**
1. Enviar `ValidateContractIntegrity.Query` com `ContractVersionId`
2. Verificar que validação reporta falha de integridade

**Resultado Esperado:**
- `result.Value.IsValid == false`
- Mensagem de erro descreve o problema de sintaxe

**Critério de Aceite:** `result.Value.IsValid == false`

---

### TC-CAT-036 — Validar prontidão para publicação — todos os checks passam

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | ValidateContractPublicationReadiness |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Handler** | `ValidateContractPublicationReadiness.Handler` |

**Pré-condições:**
- Config: `block_on_lint_errors = true`, `require_examples = true`, `approval_required = true`
- Versão de contrato: sem violações de lint, com exemplos, com `LifecycleState >= Approved`

**Passos:**
1. Configurar substitutos de `IConfigurationResolutionService` para retornar `true` em todos os 3 checks
2. Enviar `ValidateContractPublicationReadiness.Query(ContractVersionId)`

**Resultado Esperado:**
- `result.Value.IsReadyToPublish == true`
- `result.Value.TotalChecks == 3`
- `result.Value.PassedChecks == 3`
- `result.Value.FailedChecks == 0`

**Critério de Aceite:** `result.Value.IsReadyToPublish == true && result.Value.FailedChecks == 0`

---

### TC-CAT-037 — Validar prontidão para publicação — bloqueio por lint

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | ValidateContractPublicationReadiness |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `ValidateContractPublicationReadiness.Handler` |

**Pré-condições:**
- Config: `block_on_lint_errors = true`
- Versão de contrato: 2 violações de regra (`RuleViolations.Count = 2`)

**Passos:**
1. Configurar substitutos conforme pré-condições
2. Enviar `ValidateContractPublicationReadiness.Query(ContractVersionId)`

**Resultado Esperado:**
- `result.Value.IsReadyToPublish == false`
- Check `lint_errors` com `Passed = false`
- Mensagem cita quantidade de violações

**Critério de Aceite:** `result.Value.Checks.Single(c => c.CheckId == "lint_errors").Passed == false`

---

### TC-CAT-038 — Validar prontidão para publicação — falta de exemplos

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | ValidateContractPublicationReadiness |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `ValidateContractPublicationReadiness.Handler` |

**Pré-condições:**
- Config: `require_examples = true`
- `SpecContent` sem as palavras-chave `example` ou `examples`

**Passos:**
1. Configurar versão de contrato com spec sem exemplos
2. Enviar `ValidateContractPublicationReadiness.Query`

**Resultado Esperado:**
- Check `require_examples` com `Passed = false`
- `IsReadyToPublish == false`

**Critério de Aceite:** `result.Value.Checks.Single(c => c.CheckId == "require_examples").Passed == false`

---

## 5. Contratos — Avaliação de Compliance e Regras

### TC-CAT-039 — Avaliar compliance de contrato com regras aprovadas

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | EvaluateContractCompliance |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `EvaluateContractCompliance.Handler` |

**Pré-condições:**
- Contrato com `SpecContent` válido
- Regras de compliance configuradas para o tenant

**Passos:**
1. Enviar `EvaluateContractCompliance.Query` com `ContractVersionId`
2. Verificar resultado de conformidade

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.ComplianceScore` está entre 0 e 100

**Critério de Aceite:** `result.Value.ComplianceScore >= 0 && result.Value.ComplianceScore <= 100`

---

### TC-CAT-040 — Avaliar regras de contrato com violações

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | EvaluateContractRules |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `EvaluateContractRules.Handler` |

**Pré-condições:**
- Ruleset configurado com regra: "operações GET devem ter parâmetro de paginação"
- Contrato sem paginação

**Passos:**
1. Enviar `EvaluateContractRules.Command` com `ContractVersionId` e `RulesetId`
2. Verificar violações detectadas

**Resultado Esperado:**
- `result.Value.Violations.Count >= 1`
- Violação descreve a regra de paginação

**Critério de Aceite:** `result.Value.Violations.Count > 0`

---

### TC-CAT-041 — Gerar prévia de parsing da spec

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | ParseSpecPreview |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `ParseSpecPreview.Handler` |

**Pré-condições:**
- Nenhuma dependência de persistência

**Passos:**
1. Enviar `ParseSpecPreview.Command` com `SpecContent` em YAML OpenAPI 3.0 válido e `Protocol = OpenApi`
2. Verificar que endpoints são extraídos

**Resultado Esperado:**
- `result.Value.Endpoints.Count > 0`
- `result.Value.ParseErrors` está vazio

**Critério de Aceite:** `result.Value.ParseErrors.Count == 0`

---

### TC-CAT-042 — Gerar rascunho com IA

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | GenerateDraftFromAi |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `GenerateDraftFromAi.Handler` |

**Pré-condições:**
- Serviço de IA disponível (substituto)
- Tenant com capability `ai_contract_generation`

**Passos:**
1. Configurar substituto de `IAiContractGenerationService` para retornar spec gerada
2. Enviar `GenerateDraftFromAi.Command` com `ServiceDescription = "API de gerenciamento de pedidos"`
3. Verificar que rascunho é criado com conteúdo gerado pela IA

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.DraftId != Guid.Empty`

**Critério de Aceite:** `result.IsSuccess == true`

---

### TC-CAT-043 — Adicionar exemplo ao rascunho

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | AddDraftExample |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `AddDraftExample.Handler` |

**Pré-condições:**
- Rascunho existente em estado editável

**Passos:**
1. Enviar `AddDraftExample.Command` com `DraftId`, `EndpointPath = "/orders"`, `ExampleContent` JSON
2. Verificar que exemplo é adicionado ao rascunho

**Resultado Esperado:**
- `result.IsSuccess == true`

**Critério de Aceite:** `result.IsSuccess == true`

---

### TC-CAT-044 — Exportar rascunho em formato específico

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | ExportDraft |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `ExportDraft.Handler` |

**Pré-condições:**
- Rascunho OpenAPI existente

**Passos:**
1. Enviar `ExportDraft.Command` com `Format = "json"`
2. Verificar que conteúdo retornado é JSON válido

**Resultado Esperado:**
- `result.Value.Content` é string JSON não vazia
- `result.Value.ContentType == "application/json"`

**Critério de Aceite:** `result.IsSuccess == true`

---

## 6. Saúde e Dashboards de Contratos

### TC-CAT-045 — Obter score de saúde de contrato

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | GetContractHealthScore |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `GetContractHealthScore.Handler` |

**Pré-condições:**
- Contrato existente com histórico de avaliações

**Passos:**
1. Enviar `GetContractHealthScore.Query` com `ApiAssetId`
2. Verificar que score entre 0–100 é retornado

**Resultado Esperado:**
- `result.Value.Score` está entre 0 e 100
- `result.Value.Grade` (ex: `A`, `B`, `C`) presente

**Critério de Aceite:** `result.Value.Score >= 0 && result.Value.Score <= 100`

---

### TC-CAT-046 — Recalcular score de saúde de contrato

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | RecalculateContractHealthScore |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `RecalculateContractHealthScore.Handler` |

**Pré-condições:**
- Contrato com histórico de versões e compliance

**Passos:**
1. Enviar `RecalculateContractHealthScore.Command` com `ApiAssetId`
2. Verificar que score recalculado é persistido

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.UpdatedScore != result.Value.PreviousScore` (se houve mudança)

**Critério de Aceite:** `result.IsSuccess == true`

---

### TC-CAT-047 — Obter timeline de saúde de contrato

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | GetContractHealthTimeline |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `GetContractHealthTimeline.Handler` |

**Pré-condições:**
- 5 avaliações de saúde armazenadas em datas distintas

**Passos:**
1. Enviar `GetContractHealthTimeline.Query` com `ApiAssetId` e `Period = "30d"`
2. Verificar que timeline retorna pontos em ordem cronológica

**Resultado Esperado:**
- `result.Value.Points.Count <= 5`
- Pontos em ordem ascendente de data

**Critério de Aceite:** `result.IsSuccess == true`

---

### TC-CAT-048 — Computar dashboard de saúde de contratos

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | ComputeContractHealthDashboard |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `ComputeContractHealthDashboard.Handler` |

**Pré-condições:**
- 10 contratos no tenant com scores variados

**Passos:**
1. Enviar `ComputeContractHealthDashboard.Query`
2. Verificar agregação: médias, distribuição por grade

**Resultado Esperado:**
- `result.Value.AverageScore` é numérico
- `result.Value.DistributionByGrade` não está vazio

**Critério de Aceite:** `result.IsSuccess == true`

---

## 7. Schema Types — GraphQL, Protobuf e Data Contracts

### TC-CAT-049 — Analisar schema GraphQL válido

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | AnalyzeGraphQlSchema |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `AnalyzeGraphQlSchema.Handler` |

**Pré-condições:**
- Schema GraphQL SDL válido

**Passos:**
1. Enviar `AnalyzeGraphQlSchema.Command` com `SchemaContent` SDL válido e `ContractVersionId`
2. Verificar que tipos, queries e mutations são extraídos

**Resultado Esperado:**
- `result.Value.Types.Count > 0`
- `result.Value.ParseErrors` está vazio

**Critério de Aceite:** `result.Value.ParseErrors.Count == 0`

---

### TC-CAT-050 — Detectar breaking changes em schema GraphQL

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | DetectGraphQlBreakingChanges |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Handler** | `DetectGraphQlBreakingChanges.Handler` |

**Pré-condições:**
- Schema base com campo `User.email: String!`
- Schema alvo remove campo `User.email`

**Passos:**
1. Enviar `DetectGraphQlBreakingChanges.Query` com `BaseVersionId` e `TargetVersionId`
2. Verificar que remoção de campo é detectada como breaking

**Resultado Esperado:**
- `result.Value.BreakingChanges.Count >= 1`
- Mudança descreve remoção de `User.email`

**Critério de Aceite:** `result.Value.BreakingChanges.Any(c => c.Field == "User.email")`

---

### TC-CAT-051 — Analisar schema Protobuf válido

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | AnalyzeProtobufSchema |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `AnalyzeProtobufSchema.Handler` |

**Pré-condições:**
- Arquivo `.proto` válido com mensagens e serviços definidos

**Passos:**
1. Enviar `AnalyzeProtobufSchema.Command` com conteúdo `.proto`
2. Verificar que mensagens e serviços são listados

**Resultado Esperado:**
- `result.Value.Messages.Count > 0`
- `result.Value.Services.Count > 0`

**Critério de Aceite:** `result.IsSuccess == true`

---

### TC-CAT-052 — Detectar breaking changes em Protobuf (campo renumerado)

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | DetectProtobufBreakingChanges |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Handler** | `DetectProtobufBreakingChanges.Handler` |

**Pré-condições:**
- Schema base: `string name = 1;`
- Schema alvo: `string name = 2;` (número de campo alterado — breaking em Protobuf)

**Passos:**
1. Enviar `DetectProtobufBreakingChanges.Query`
2. Verificar que renumeração é identificada como breaking

**Resultado Esperado:**
- `result.Value.BreakingChanges.Count >= 1`

**Critério de Aceite:** `result.Value.BreakingChanges.Count > 0`

---

### TC-CAT-053 — Analisar Data Contract Schema

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | AnalyzeDataContractSchema |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `AnalyzeDataContractSchema.Handler` |

**Pré-condições:**
- Data contract em formato JSON Schema válido

**Passos:**
1. Enviar `AnalyzeDataContractSchema.Command` com `SchemaContent` JSON Schema
2. Verificar extração de campos, tipos e restrições

**Resultado Esperado:**
- `result.Value.Fields.Count > 0`

**Critério de Aceite:** `result.IsSuccess == true`

---

## 8. SBOM e Governança de Dependências

### TC-CAT-054 — Ingerir SBOM com componentes válidos

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | IngestSbomRecord |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Handler** | `IngestSbomRecord.Handler` |

**Pré-condições:**
- `ISbomRepository` disponível
- `IDateTimeProvider` configurado

**Passos:**
1. Instanciar `IngestSbomRecord.Handler` com substitutos
2. Enviar `Command` com `TenantId`, `ServiceId = "payments-svc"`, `Version = "1.2.0"`, e lista de 5 componentes
3. Verificar que `ISbomRepository.AddAsync` foi chamado
4. Verificar retorno de `Guid` válido

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value != Guid.Empty`

**Critério de Aceite:** `result.IsSuccess == true && result.Value != Guid.Empty`

---

### TC-CAT-055 — Ingerir SBOM com TenantId vazio deve falhar

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | IngestSbomRecord / Validator |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `IngestSbomRecord.Validator` |

**Pré-condições:**
- Nenhuma

**Passos:**
1. Instanciar `IngestSbomRecord.Validator`
2. Validar `Command` com `TenantId = ""`

**Resultado Esperado:**
- `ValidationResult.IsValid == false`
- Erro em `TenantId`

**Critério de Aceite:** `errors.Any(e => e.PropertyName == "TenantId")`

---

### TC-CAT-056 — Ingerir SBOM com lista de componentes vazia — deve aceitar

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | IngestSbomRecord |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `IngestSbomRecord.Handler` |

**Pré-condições:**
- `ISbomRepository` disponível

**Passos:**
1. Enviar `Command` com `Components = []` (lista vazia)
2. Verificar que handler aceita o comando (lista vazia é permitida pelo validator)

**Resultado Esperado:**
- `result.IsSuccess == true`
- SBOM persistido sem componentes

**Critério de Aceite:** `result.IsSuccess == true`

---

### TC-CAT-057 — Gerar SBOM para serviço

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | GenerateSbom |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `GenerateSbom.Handler` |

**Pré-condições:**
- Serviço registrado com dependências conhecidas

**Passos:**
1. Enviar `GenerateSbom.Command` com `ServiceId`
2. Verificar que SBOM é gerado e persistido

**Resultado Esperado:**
- `result.IsSuccess == true`
- SBOM contém lista de componentes

**Critério de Aceite:** `result.Value.SbomId != Guid.Empty`

---

### TC-CAT-058 — Verificar políticas de dependências

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | CheckDependencyPolicies |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `CheckDependencyPolicies.Handler` |

**Pré-condições:**
- Política configurada: "não permitir pacotes com licença GPL"
- SBOM do serviço contém componente com licença `GPL-3.0`

**Passos:**
1. Enviar `CheckDependencyPolicies.Query` com `ServiceId`
2. Verificar violação de política detectada

**Resultado Esperado:**
- `result.Value.Violations.Count >= 1`
- Violação descreve componente GPL

**Critério de Aceite:** `result.Value.Violations.Any(v => v.License.Contains("GPL"))`

---

### TC-CAT-059 — Detectar conflitos de licença

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | DetectLicenseConflicts |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `DetectLicenseConflicts.Handler` |

**Pré-condições:**
- SBOM com componentes `MIT` e `AGPL-3.0` (incompatíveis em alguns contextos)

**Passos:**
1. Enviar `DetectLicenseConflicts.Query` com `ServiceId`
2. Verificar conflitos identificados

**Resultado Esperado:**
- `result.Value.Conflicts.Count >= 1`

**Critério de Aceite:** `result.Value.Conflicts.Count > 0`

---

### TC-CAT-060 — Avaliar gate de promoção de vulnerabilidades

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | EvaluateVulnerabilityPromotionGate |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Handler** | `EvaluateVulnerabilityPromotionGate.Handler` |

**Pré-condições:**
- SBOM com 1 componente com CVE de severidade `Critical`
- Config: `vulnerability.gate.block_on_critical = true`

**Passos:**
1. Enviar `EvaluateVulnerabilityPromotionGate.Query` com `ServiceId` e `TargetEnvironment = "production"`
2. Verificar que gate bloqueia a promoção

**Resultado Esperado:**
- `result.Value.Passed == false`
- Motivo cita CVE crítica

**Critério de Aceite:** `result.Value.Passed == false`

---

### TC-CAT-061 — Listar dependências vulneráveis

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | ListVulnerableDependencies |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `ListVulnerableDependencies.Handler` |

**Pré-condições:**
- SBOM ingerido com 2 componentes com `CveCount > 0`

**Passos:**
1. Enviar `ListVulnerableDependencies.Query` com `ServiceId`
2. Verificar lista de dependências vulneráveis

**Resultado Esperado:**
- `result.Value.Count == 2`

**Critério de Aceite:** `result.Value.All(d => d.CveCount > 0)`

---

### TC-CAT-062 — Sugerir upgrades de dependências

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | SuggestDependencyUpgrades |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `SuggestDependencyUpgrades.Handler` |

**Pré-condições:**
- Componente `log4j 2.14.1` com CVE conhecida; versão `2.17.1` disponível sem CVE

**Passos:**
1. Enviar `SuggestDependencyUpgrades.Query` com `ServiceId`
2. Verificar sugestão de upgrade para `2.17.1`

**Resultado Esperado:**
- `result.Value.Suggestions.Any(s => s.SuggestedVersion == "2.17.1")`

**Critério de Aceite:** `result.IsSuccess == true`

---

### TC-CAT-063 — Ingerir relatório de advisory de segurança

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | IngestAdvisoryReport |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `IngestAdvisoryReport.Handler` |

**Pré-condições:**
- Relatório de advisory em formato NVD/OSV

**Passos:**
1. Enviar `IngestAdvisoryReport.Command` com relatório JSON contendo 3 CVEs
2. Verificar que todas as 3 entradas são persistidas

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.IngestedCount == 3`

**Critério de Aceite:** `result.Value.IngestedCount == 3`

---

## 9. Grafo de Serviços e Topologia

### TC-CAT-064 — Registrar interface de serviço

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | RegisterServiceInterface (CreateServiceInterface) |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `CreateServiceInterface.Handler` |

**Pré-condições:**
- Serviço registrado no catálogo

**Passos:**
1. Enviar `CreateServiceInterface.Command` com `ServiceId`, `InterfaceType = "REST"`, `BaseUrl`
2. Verificar que interface é persistida

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.InterfaceId != Guid.Empty`

**Critério de Aceite:** `result.IsSuccess == true`

---

### TC-CAT-065 — Vincular contrato a interface de serviço

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | BindContractToInterface |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `BindContractToInterface.Handler` |

**Pré-condições:**
- Interface de serviço existente
- Contrato `Published` existente

**Passos:**
1. Enviar `BindContractToInterface.Command` com `InterfaceId` e `ContractVersionId`
2. Verificar que binding é criado

**Resultado Esperado:**
- `result.IsSuccess == true`

**Critério de Aceite:** `result.IsSuccess == true`

---

### TC-CAT-066 — Obter grafo de ativos

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | GetAssetGraph |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `GetAssetGraph.Handler` |

**Pré-condições:**
- 5 serviços com dependências entre si formando um grafo

**Passos:**
1. Enviar `GetAssetGraph.Query`
2. Verificar que nós e arestas são retornados

**Resultado Esperado:**
- `result.Value.Nodes.Count == 5`
- `result.Value.Edges.Count > 0`

**Critério de Aceite:** `result.IsSuccess == true`

---

### TC-CAT-067 — Detectar dependências circulares no grafo

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | DetectCircularDependencies |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Handler** | `DetectCircularDependencies.Handler` |

**Pré-condições:**
- Grafo com ciclo: `A → B → C → A`

**Passos:**
1. Registrar links: A→B, B→C, C→A via `AddServiceLink`
2. Enviar `DetectCircularDependencies.Query`

**Resultado Esperado:**
- `result.Value.CircularPaths.Count >= 1`
- Ciclo `A→B→C→A` está presente na lista

**Critério de Aceite:** `result.Value.CircularPaths.Count > 0`

---

### TC-CAT-068 — Detectar drift de ownership

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | DetectOwnershipDrift |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `DetectOwnershipDrift.Handler` |

**Pré-condições:**
- 3 serviços sem equipe de ownership definida

**Passos:**
1. Enviar `DetectOwnershipDrift.Query`
2. Verificar que os 3 serviços são listados como drift de ownership

**Resultado Esperado:**
- `result.Value.ServicesWithoutOwner.Count == 3`

**Critério de Aceite:** `result.Value.ServicesWithoutOwner.Count == 3`

---

### TC-CAT-069 — Computar maturidade de serviço

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | ComputeServiceMaturity |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `ComputeServiceMaturity.Handler` |

**Pré-condições:**
- Serviço com contrato publicado, SBOM e owner definido

**Passos:**
1. Enviar `ComputeServiceMaturity.Command` com `ServiceId`
2. Verificar score de maturidade e dimensões avaliadas

**Resultado Esperado:**
- `result.Value.MaturityScore >= 0`
- `result.Value.Dimensions` contém `contract`, `sbom`, `ownership`

**Critério de Aceite:** `result.IsSuccess == true`

---

### TC-CAT-070 — Exportar inventário para Backstage

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | ExportToBackstage |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `ExportToBackstage.Handler` |

**Pré-condições:**
- 3 serviços registrados no catálogo

**Passos:**
1. Enviar `ExportToBackstage.Command`
2. Verificar que payload YAML no formato `catalog-info.yaml` do Backstage é gerado

**Resultado Esperado:**
- `result.Value.YamlContent` contém `apiVersion: backstage.io/v1alpha1`
- 3 entradas no payload

**Critério de Aceite:** `result.IsSuccess == true`

---

## 10. Developer Experience e Feature Flags

### TC-CAT-071 — Computar score de Developer Experience

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | ComputeDeveloperExperienceScore |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `ComputeDeveloperExperienceScore.Handler` |

**Pré-condições:**
- Dados de NPS e uso de IDE disponíveis

**Passos:**
1. Enviar `ComputeDeveloperExperienceScore.Command` com `ServiceId`
2. Verificar cálculo das dimensões: documentação, exemplos, suporte

**Resultado Esperado:**
- `result.Value.Score` entre 0 e 100
- `result.Value.Breakdown` detalha cada dimensão

**Critério de Aceite:** `result.Value.Score >= 0 && result.Value.Score <= 100`

---

### TC-CAT-072 — Registrar uso de IDE

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | RecordIdeUsage |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `RecordIdeUsage.Handler` |

**Pré-condições:**
- `IIDEUsageRepository` disponível

**Passos:**
1. Enviar `RecordIdeUsage.Command` com `UserId`, `IdeName = "vscode"`, `ContractId`, `Action = "viewed"`
2. Verificar que registro é persistido

**Resultado Esperado:**
- `result.IsSuccess == true`

**Critério de Aceite:** `result.IsSuccess == true`

---

### TC-CAT-073 — Ingerir estado de feature flag

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | IngestFeatureFlagState |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `IngestFeatureFlagState.Handler` |

**Pré-condições:**
- `IFeatureFlagRepository` disponível (EF Core real)

**Passos:**
1. Enviar `IngestFeatureFlagState.Command` com `ServiceId`, `FlagKey = "new-checkout"`, `IsEnabled = true`
2. Verificar que registro é criado ou atualizado (upsert)

**Resultado Esperado:**
- `result.IsSuccess == true`
- Flag com `FlagKey = "new-checkout"` persistida

**Critério de Aceite:** `result.IsSuccess == true`

---

### TC-CAT-074 — Obter feature flag efetiva para serviço

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | GetEffectiveFeatureFlag |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `GetEffectiveFeatureFlag.Handler` |

**Pré-condições:**
- Feature flag `new-checkout` persistida com `IsEnabled = true` para `ServiceId` e tenant

**Passos:**
1. Enviar `GetEffectiveFeatureFlag.Query` com `ServiceId` e `FlagKey = "new-checkout"`
2. Verificar que estado `IsEnabled = true` é retornado

**Resultado Esperado:**
- `result.Value.IsEnabled == true`

**Critério de Aceite:** `result.Value.IsEnabled == true`

---

### TC-CAT-075 — Obter inventário de feature flags

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | GetFeatureFlagInventoryReport |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `GetFeatureFlagInventoryReport.Handler` |

**Pré-condições:**
- 5 feature flags ingeridas para o tenant

**Passos:**
1. Enviar `GetFeatureFlagInventoryReport.Query`
2. Verificar que todas as 5 flags são listadas

**Resultado Esperado:**
- `result.Value.TotalFlags == 5`

**Critério de Aceite:** `result.IsSuccess == true`

---

## 11. Relatórios e Análises

### TC-CAT-076 — Relatório de compatibilidade retroativa de API

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | GetApiBackwardCompatibilityReport |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `GetApiBackwardCompatibilityReport.Handler` |

**Pré-condições:**
- Contratos com histórico de diffs semânticos

**Passos:**
1. Enviar `GetApiBackwardCompatibilityReport.Query` com `Period = "90d"`
2. Verificar métricas de compatibilidade

**Resultado Esperado:**
- `result.Value.BreakingChangeCount >= 0`
- `result.Value.CompatibilityRate` entre 0 e 100

**Critério de Aceite:** `result.IsSuccess == true`

---

### TC-CAT-077 — Relatório de cobertura de schema de API

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | GetApiSchemaCoverageReport |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `GetApiSchemaCoverageReport.Handler` |

**Pré-condições:**
- 10 endpoints; 8 com schemas documentados; 2 sem

**Passos:**
1. Enviar `GetApiSchemaCoverageReport.Query` com `ApiAssetId`
2. Verificar taxa de cobertura

**Resultado Esperado:**
- `result.Value.CoveragePercentage == 80`

**Critério de Aceite:** `result.IsSuccess == true`

---

### TC-CAT-078 — Relatório de adoção de contratos

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | GetContractAdoptionReport |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `GetContractAdoptionReport.Handler` |

**Pré-condições:**
- 5 consumidores registrados; 3 adotaram o contrato v2

**Passos:**
1. Enviar `GetContractAdoptionReport.Query` com `ContractVersionId`
2. Verificar taxa de adoção

**Resultado Esperado:**
- `result.Value.AdoptionRate == 60`

**Critério de Aceite:** `result.IsSuccess == true`

---

### TC-CAT-079 — Relatório de risco de dependências

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | GetDependencyRiskReport |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `GetDependencyRiskReport.Handler` |

**Pré-condições:**
- Serviços com vulnerabilidades e licenças conflitantes

**Passos:**
1. Enviar `GetDependencyRiskReport.Query`
2. Verificar categorias de risco (high, medium, low)

**Resultado Esperado:**
- `result.Value.RiskDistribution` contém entradas para cada nível

**Critério de Aceite:** `result.IsSuccess == true`

---

### TC-CAT-080 — Relatório de risco de cadeia de suprimentos

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | GetSupplyChainRiskReport |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `GetSupplyChainRiskReport.Handler` |

**Pré-condições:**
- SBOM com pacotes de fontes não verificadas

**Passos:**
1. Enviar `GetSupplyChainRiskReport.Query`
2. Verificar itens de risco identificados

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.RiskItems.Count > 0`

**Critério de Aceite:** `result.IsSuccess == true`

---

### TC-CAT-081 — Relatório de previsão de depreciação

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | GetContractDeprecationForecast |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `GetContractDeprecationForecast.Handler` |

**Pré-condições:**
- 2 contratos com depreciação agendada nos próximos 30 dias

**Passos:**
1. Enviar `GetContractDeprecationForecast.Query` com `Horizon = "30d"`
2. Verificar previsão com as 2 depreciações

**Resultado Esperado:**
- `result.Value.UpcomingDeprecations.Count == 2`

**Critério de Aceite:** `result.IsSuccess == true`

---

### TC-CAT-082 — Isolamento multi-tenant no GetContractListing

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | GetContractListing |
| **Tipo** | Segurança |
| **Prioridade** | Crítica |
| **Handler** | `GetContractListing.Handler` |

**Pré-condições:**
- Tenant A possui 5 contratos
- Tenant B possui 3 contratos
- `TenantRlsInterceptor` ativo no contexto de banco de dados

**Passos:**
1. Autenticar como tenant A
2. Enviar `GetContractListing.Query` sem filtros de tenant
3. Verificar que apenas os 5 contratos do tenant A são retornados

**Resultado Esperado:**
- `result.Value.Total == 5`
- Nenhum contrato do tenant B está presente

**Critério de Aceite:** `result.Value.Items.All(c => c.TenantId == tenantA)`

---

### TC-CAT-083 — Relatório de exposição a vulnerabilidades

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | GetVulnerabilityExposureReport |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `GetVulnerabilityExposureReport.Handler` |

**Pré-condições:**
- SBOM com componentes de diferentes severidades de CVE

**Passos:**
1. Enviar `GetVulnerabilityExposureReport.Query`
2. Verificar distribuição por severidade

**Resultado Esperado:**
- `result.Value.BySeverity` contém chaves `Critical`, `High`, `Medium`, `Low`

**Critério de Aceite:** `result.IsSuccess == true`

---

## 12. Relatório de Caminho Crítico e Linhagem

### TC-CAT-084 — Relatório de caminho crítico de serviços

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | GetCriticalPathReport |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `GetCriticalPathReport.Handler` |

**Pré-condições:**
- Grafo de 10 serviços com dependências aninhadas
- Serviço `payment-gateway` identificado como crítico (3 dependências diretas de tier-1)

**Passos:**
1. Enviar `GetCriticalPathReport.Query`
2. Verificar que `payment-gateway` aparece no caminho crítico

**Resultado Esperado:**
- `result.Value.CriticalPath.Any(s => s.ServiceId == paymentGatewayId)`

**Critério de Aceite:** `result.IsSuccess == true`

---

### TC-CAT-085 — Relatório de linhagem de contrato

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | GetContractLineageReport |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `GetContractLineageReport.Handler` |

**Pré-condições:**
- Contrato com 4 versões publicadas ao longo de 6 meses

**Passos:**
1. Enviar `GetContractLineageReport.Query` com `ApiAssetId`
2. Verificar que linhagem completa é retornada em ordem cronológica

**Resultado Esperado:**
- `result.Value.Versions.Count == 4`
- Versões em ordem crescente de data de publicação

**Critério de Aceite:** `result.IsSuccess == true`

---

### TC-CAT-086 — Resumo de contratos do tenant

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | GetContractsSummary |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `GetContractsSummary.Handler` |

**Pré-condições:**
- 10 contratos: 5 publicados, 3 rascunhos, 2 depreciados

**Passos:**
1. Enviar `GetContractsSummary.Query`
2. Verificar contagem por estado

**Resultado Esperado:**
- `result.Value.Published == 5`
- `result.Value.Draft == 3`
- `result.Value.Deprecated == 2`

**Critério de Aceite:** `result.Value.Total == 10`

---

### TC-CAT-087 — Relatório de saúde de documentação

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | GetDocumentationHealthReport |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `GetDocumentationHealthReport.Handler` |

**Pré-condições:**
- 8 contratos: 5 com exemplos e descrições; 3 sem exemplos

**Passos:**
1. Enviar `GetDocumentationHealthReport.Query`
2. Verificar índice de saúde da documentação

**Resultado Esperado:**
- `result.Value.HealthIndex` reflete a proporção (62,5%)
- Contratos sem exemplos listados separadamente

**Critério de Aceite:** `result.IsSuccess == true`

---

*Total de cenários de teste neste documento: **87***

---

## Apêndice — Mapeamento de Handlers para Cenários

| Cenário | Handler |
|---------|---------|
| TC-CAT-001 a 005 | Criação de rascunhos |
| TC-CAT-006 a 008 | Atualização de rascunhos |
| TC-CAT-009 a 016 | Fluxo de revisão e aprovação |
| TC-CAT-017 a 026 | Versionamento e ciclo de vida |
| TC-CAT-027 a 044 | Análise semântica, diff, compliance |
| TC-CAT-045 a 048 | Saúde e dashboards |
| TC-CAT-049 a 053 | Schema types (GraphQL, Protobuf, Data Contracts) |
| TC-CAT-054 a 063 | SBOM e dependências |
| TC-CAT-064 a 070 | Grafo de serviços e topologia |
| TC-CAT-071 a 075 | Developer Experience e Feature Flags |
| TC-CAT-076 a 087 | Relatórios e análises |
