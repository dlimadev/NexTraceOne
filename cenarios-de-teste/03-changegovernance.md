# Cenários de Teste — Módulo ChangeGovernance

> **Projeto:** NexTraceOne  
> **Módulo:** ChangeGovernance  
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
| **Fluxo** | Sequência de comandos que representa um fluxo de negócio completo |
| **Segurança** | Isolamento de tenant, autorização, vazamento de dados |

**Prioridades:** Crítica > Alta > Média > Baixa

---

## 1. Release Ingest — Ingestão e Consulta de Releases

### TC-CGV-001 — Ingerir commit de release com dados válidos

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | IngestCommit |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Handler** | `IngestCommit.Handler` |

**Pré-condições:**
- `IReleaseRepository` disponível
- Tenant autenticado

**Passos:**
1. Instanciar handler com repositórios substitutos
2. Enviar `IngestCommit.Command` com `CommitSha = "abc123"`, `ServiceId`, `Message = "feat: adiciona endpoint de rastreamento"`, `Author = "dev@empresa.com"`, `BranchName = "main"`
3. Verificar que release é criada e persistida
4. Verificar que `IChangeIntelligenceUnitOfWork.CommitAsync` foi chamado

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.ReleaseId != Guid.Empty`

**Critério de Aceite:** `result.IsSuccess == true && result.Value.ReleaseId != Guid.Empty`

---

### TC-CGV-002 — Ingerir commit com SHA vazio deve falhar na validação

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | IngestCommit / Validator |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `IngestCommit.Validator` |

**Pré-condições:**
- Nenhuma dependência de infraestrutura necessária

**Passos:**
1. Instanciar `IngestCommit.Validator`
2. Chamar `Validate` com `Command(CommitSha = "", ServiceId = Guid.NewGuid(), Message = "...")`
3. Inspecionar `ValidationResult.Errors`

**Resultado Esperado:**
- `ValidationResult.IsValid == false`
- Erro aponta para propriedade `CommitSha`

**Critério de Aceite:** `errors.Any(e => e.PropertyName == "CommitSha")`

---

### TC-CGV-003 — Ingerir release externa (ArgoCD, Jenkins, etc.)

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | IngestExternalRelease |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `IngestExternalRelease.Handler` |

**Pré-condições:**
- Sistema externo de CI/CD registrado

**Passos:**
1. Enviar `IngestExternalRelease.Command` com `ExternalReleaseKey = "jenkins-build-42"`, `ServiceId`, `Version = "3.2.1"`, `Source = "jenkins"`
2. Verificar que release externa é persistida com `Source = "jenkins"`

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.Source == "jenkins"`

**Critério de Aceite:** `result.IsSuccess == true`

---

### TC-CGV-004 — Obter release por ID

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | GetRelease |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `GetRelease.Handler` |

**Pré-condições:**
- Release persistida com `ReleaseId` conhecido

**Passos:**
1. Configurar `IReleaseRepository.GetByIdAsync` para retornar release
2. Enviar `GetRelease.Query(ReleaseId)`
3. Verificar campos `ServiceId`, `CommitSha`, `ChangeLevel`

**Resultado Esperado:**
- `result.Value.ReleaseId == releaseId`

**Critério de Aceite:** `result.IsSuccess == true && result.Value.ReleaseId == releaseId`

---

### TC-CGV-005 — Listar releases por serviço

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | ListReleasesByService |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `ListReleasesByService.Handler` |

**Pré-condições:**
- 4 releases para `ServiceId A`; 2 releases para `ServiceId B`

**Passos:**
1. Configurar repositório para filtrar por `ServiceId A`
2. Enviar `ListReleasesByService.Query(ServiceIdA)`
3. Verificar que apenas 4 releases são retornadas

**Resultado Esperado:**
- `result.Value.Count == 4`
- Todas as releases pertencem ao `ServiceId A`

**Critério de Aceite:** `result.Value.All(r => r.ServiceId == serviceIdA)`

---

### TC-CGV-006 — Gerar notas de release automaticamente

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | GenerateReleaseNotes |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `GenerateReleaseNotes.Handler` |

**Pré-condições:**
- Release com 3 commits: 1 `feat:`, 1 `fix:`, 1 `chore:`
- Serviço de IA ou formatador convencional disponível

**Passos:**
1. Enviar `GenerateReleaseNotes.Command` com `ReleaseId`
2. Verificar que notas são geradas com seções `Features`, `Bug Fixes`, `Chores`

**Resultado Esperado:**
- `result.Value.Notes` contém as 3 seções
- `result.Value.GeneratedAt` preenchido

**Critério de Aceite:** `result.IsSuccess == true`

---

### TC-CGV-007 — Obter calendário de releases

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | GetReleaseCalendar |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `GetReleaseCalendar.Handler` |

**Pré-condições:**
- 5 releases planejadas para os próximos 30 dias

**Passos:**
1. Enviar `GetReleaseCalendar.Query` com `From = today`, `To = today + 30d`
2. Verificar que 5 entradas são retornadas

**Resultado Esperado:**
- `result.Value.Entries.Count == 5`

**Critério de Aceite:** `result.IsSuccess == true`

---

### TC-CGV-008 — Registrar janela de release

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | RegisterReleaseWindow |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `RegisterReleaseWindow.Handler` |

**Pré-condições:**
- Nenhuma janela de release existente para o período

**Passos:**
1. Enviar `RegisterReleaseWindow.Command` com `Name = "Janela Semanal"`, `StartsAt = next Monday 08h`, `EndsAt = next Monday 12h`
2. Verificar que janela é persistida

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.ReleaseWindowId != Guid.Empty`

**Critério de Aceite:** `result.IsSuccess == true`

---

## 2. Change Intelligence — Classificação e Avaliação de Mudanças

### TC-CGV-009 — Classificar nível de mudança como Major

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | ClassifyChangeLevel |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Handler** | `ClassifyChangeLevel.Handler` |

**Pré-condições:**
- Release com breaking change detectado no contrato

**Passos:**
1. Enviar `ClassifyChangeLevel.Command` com `ReleaseId`, indicando breaking change no contrato
2. Verificar que nível de mudança é `Breaking` ou `Major`

**Resultado Esperado:**
- `result.Value.ChangeLevel == ChangeLevel.Breaking`

**Critério de Aceite:** `result.Value.ChangeLevel == ChangeLevel.Breaking`

---

### TC-CGV-010 — Computar score de mudança (ChangeScore)

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | ComputeChangeScore |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Handler** | `ComputeChangeScore.Handler` |

**Pré-condições:**
- Release com `ChangeLevel = Breaking`, `Environment = "production"`, blast radius de 50 consumidores

**Passos:**
1. Configurar calculadora de score com fatores reais
2. Enviar `ComputeChangeScore.Command` com `ReleaseId`
3. Verificar que score reflete risco elevado

**Resultado Esperado:**
- `result.Value.Score > 70` (limiar de risco alto)
- `result.Value.Factors` detalha contribuições individuais

**Critério de Aceite:** `result.Value.Score > 0 && result.Value.Score <= 100`

---

### TC-CGV-011 — Calcular blast radius com lista de consumidores

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | CalculateBlastRadius |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Handler** | `CalculateBlastRadius.Handler` |

**Pré-condições:**
- `IReleaseRepository.GetByIdAsync` retorna release válida
- `IBlastRadiusRepository` disponível
- `IChangeScoreCalculator` configurado
- Feature `MetricsBlastRadiusEnabled = true`

**Passos:**
1. Instanciar handler com todos os substitutos
2. Enviar `Command(ReleaseId, DirectConsumers = ["svc-A","svc-B"], TransitiveConsumers = ["svc-C","svc-D","svc-E"])`
3. Verificar `TotalAffectedConsumers == 5`
4. Verificar que score é recalculado e persistido

**Resultado Esperado:**
- `result.Value.TotalAffectedConsumers == 5`
- `result.Value.BlastRadiusReportId != Guid.Empty`
- `result.Value.UpdatedScore > 0`

**Critério de Aceite:** `result.Value.TotalAffectedConsumers == 5`

---

### TC-CGV-012 — Blast radius desabilitado retorna resposta vazia

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | CalculateBlastRadius |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `CalculateBlastRadius.Handler` |

**Pré-condições:**
- `IEnvironmentBehaviorService.IsEnabledAsync("metrics.blast_radius.enabled")` retorna `false`

**Passos:**
1. Configurar `environmentBehaviorService` para retornar `false`
2. Enviar `Command(ReleaseId, ...)`

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.TotalAffectedConsumers == 0`
- `result.Value.ScoreSource == "disabled"`
- `IBlastRadiusRepository.Add` NÃO foi chamado

**Critério de Aceite:** `result.Value.ScoreSource == "disabled"`

---

### TC-CGV-013 — Blast radius com ReleaseId inválido retorna NotFound

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | CalculateBlastRadius |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `CalculateBlastRadius.Handler` |

**Pré-condições:**
- Feature habilitada
- `IReleaseRepository.GetByIdAsync` retorna `null`

**Passos:**
1. Configurar repositório para retornar `null`
2. Enviar `Command(Guid.NewGuid(), [], [])`

**Resultado Esperado:**
- `result.IsFailure == true`
- `result.Error.Type == ErrorType.NotFound`

**Critério de Aceite:** `result.Error.Type == ErrorType.NotFound`

---

### TC-CGV-014 — Verificar prontidão para deploy

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | CheckDeployReadiness |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Handler** | `CheckDeployReadiness.Handler` |

**Pré-condições:**
- Release com `ChangeLevel = Patch`, sem freeze ativo, gates aprovados

**Passos:**
1. Configurar repositórios e verificar ausência de freeze
2. Enviar `CheckDeployReadiness.Query(ReleaseId, TargetEnvironment = "staging")`
3. Verificar que todos os checks passam

**Resultado Esperado:**
- `result.Value.IsReady == true`
- `result.Value.Blockers` está vazio

**Critério de Aceite:** `result.Value.IsReady == true && result.Value.Blockers.Count == 0`

---

### TC-CGV-015 — Avaliar viabilidade de rollback

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | AssessRollbackViability |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `AssessRollbackViability.Handler` |

**Pré-condições:**
- Release com migração de banco de dados irreversível aplicada

**Passos:**
1. Enviar `AssessRollbackViability.Query(ReleaseId)`
2. Verificar que rollback é classificado como inviável

**Resultado Esperado:**
- `result.Value.IsViable == false`
- `result.Value.Reason` descreve a migração irreversível

**Critério de Aceite:** `result.Value.IsViable == false`

---

### TC-CGV-016 — Computar breakdown de confiança de mudança

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | ComputeChangeConfidenceBreakdown |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `ComputeChangeConfidenceBreakdown.Handler` |

**Pré-condições:**
- Release com lint score, blast radius e histórico de rollback disponíveis

**Passos:**
1. Enviar `ComputeChangeConfidenceBreakdown.Command(ReleaseId)`
2. Verificar dimensões: `LintScore`, `BlastRadius`, `RollbackHistory`, `TestCoverage`

**Resultado Esperado:**
- `result.Value.OverallConfidence` entre 0 e 100
- `result.Value.Dimensions` contém as 4 dimensões

**Critério de Aceite:** `result.Value.OverallConfidence >= 0 && result.Value.OverallConfidence <= 100`

---

### TC-CGV-017 — Correlacionar trace a mudança

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | CorrelateTraceToChange |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `CorrelateTraceToChange.Handler` |

**Pré-condições:**
- Release deployada há 10 minutos
- Trace de erro ocorrido 5 minutos após o deploy

**Passos:**
1. Enviar `CorrelateTraceToChange.Command(TraceId, ServiceId, OccurredAt = agora - 5min)`
2. Verificar que release candidata é identificada

**Resultado Esperado:**
- `result.Value.CorrelatedReleaseId != null`
- `result.Value.Confidence > 0.5`

**Critério de Aceite:** `result.IsSuccess == true`

---

### TC-CGV-018 — Vincular work item a release

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | AddWorkItemToRelease |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `AddWorkItemToRelease.Handler` |

**Pré-condições:**
- Release existente

**Passos:**
1. Enviar `AddWorkItemToRelease.Command(ReleaseId, WorkItemId = "PROJ-123", WorkItemType = "Story")`
2. Verificar que associação é persistida

**Resultado Esperado:**
- `result.IsSuccess == true`

**Critério de Aceite:** `result.IsSuccess == true`

---

### TC-CGV-019 — Remover work item de release

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | RemoveWorkItemFromRelease |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `RemoveWorkItemFromRelease.Handler` |

**Pré-condições:**
- Work item `PROJ-123` associado à release

**Passos:**
1. Enviar `RemoveWorkItemFromRelease.Command(ReleaseId, WorkItemId = "PROJ-123")`
2. Verificar que associação é removida

**Resultado Esperado:**
- `result.IsSuccess == true`

**Critério de Aceite:** `result.IsSuccess == true`

---

## 3. Freeze Windows — Criação, Atualização e Verificação

### TC-CGV-020 — Criar janela de freeze global com dados válidos

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | CreateFreezeWindow |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Handler** | `CreateFreezeWindow.Handler` |

**Pré-condições:**
- `IFreezeWindowRepository` disponível
- `ICurrentUser` configurado
- `IDateTimeProvider` configurado

**Passos:**
1. Instanciar handler com substitutos
2. Enviar `Command(Name = "Black Friday 2026", Reason = "Período crítico de vendas", Scope = Global, StartsAt = 2026-11-27, EndsAt = 2026-11-29)`
3. Verificar que janela é criada com scope `Global`
4. Verificar que `IChangeIntelligenceUnitOfWork.CommitAsync` foi chamado

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.FreezeWindowId != Guid.Empty`
- `result.Value.Scope == FreezeScope.Global`

**Critério de Aceite:** `result.IsSuccess == true && result.Value.Scope == FreezeScope.Global`

---

### TC-CGV-021 — Validação: EndsAt deve ser posterior a StartsAt

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | CreateFreezeWindow / Validator |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `CreateFreezeWindow.Validator` |

**Pré-condições:**
- Nenhuma dependência de infraestrutura

**Passos:**
1. Instanciar `CreateFreezeWindow.Validator`
2. Chamar `Validate` com `Command(StartsAt = 2026-12-01, EndsAt = 2026-11-30)` (EndsAt anterior a StartsAt)
3. Verificar falha de validação

**Resultado Esperado:**
- `ValidationResult.IsValid == false`
- Mensagem: `"Freeze window end must be after start."`

**Critério de Aceite:** `errors.Any(e => e.PropertyName == "EndsAt")`

---

### TC-CGV-022 — Criar freeze de escopo por ambiente

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | CreateFreezeWindow |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `CreateFreezeWindow.Handler` |

**Pré-condições:**
- Nenhuma janela de freeze ativa para `production`

**Passos:**
1. Enviar `Command(Scope = Environment, ScopeValue = "production", StartsAt = ..., EndsAt = ...)`
2. Verificar que janela é criada com `ScopeValue = "production"`

**Resultado Esperado:**
- `result.Value.Scope == FreezeScope.Environment`

**Critério de Aceite:** `result.IsSuccess == true`

---

### TC-CGV-023 — Verificar conflito de freeze — com conflito global ativo

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | CheckFreezeConflict |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Handler** | `CheckFreezeConflict.Handler` |

**Pré-condições:**
- Janela de freeze `Global` ativa cobrindo o momento `T`

**Passos:**
1. Configurar `IFreezeWindowRepository.ListActiveAtAsync(T)` para retornar 1 freeze global
2. Enviar `Query(At = T, Environment = "production")`

**Resultado Esperado:**
- `result.Value.HasConflict == true`
- `result.Value.ActiveFreezes.Count == 1`
- Freeze listado tem `Scope == "Global"`

**Critério de Aceite:** `result.Value.HasConflict == true`

---

### TC-CGV-024 — Verificar conflito de freeze — sem conflito no horário

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | CheckFreezeConflict |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `CheckFreezeConflict.Handler` |

**Pré-condições:**
- Nenhuma janela de freeze ativa no momento `T + 2h`

**Passos:**
1. Configurar repositório para retornar lista vazia
2. Enviar `Query(At = T + 2h, Environment = null)`

**Resultado Esperado:**
- `result.Value.HasConflict == false`
- `result.Value.ActiveFreezes.Count == 0`

**Critério de Aceite:** `result.Value.HasConflict == false`

---

### TC-CGV-025 — Freeze de ambiente não bloqueia ambiente diferente

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | CheckFreezeConflict |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `CheckFreezeConflict.Handler` |

**Pré-condições:**
- Janela de freeze com `Scope = Environment`, `ScopeValue = "production"` ativa

**Passos:**
1. Configurar repositório para retornar freeze de `production`
2. Enviar `Query(At = T, Environment = "staging")`
3. Verificar que freeze de `production` é filtrado fora do resultado

**Resultado Esperado:**
- `result.Value.HasConflict == false`
- Freeze de `production` não está na lista filtrada

**Critério de Aceite:** `result.Value.HasConflict == false`

---

### TC-CGV-026 — Atualizar janela de freeze existente

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | UpdateFreezeWindow |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `UpdateFreezeWindow.Handler` |

**Pré-condições:**
- Janela de freeze existente com `Name = "Freeze Antigo"`

**Passos:**
1. Enviar `UpdateFreezeWindow.Command(FreezeWindowId, Name = "Freeze Atualizado", EndsAt = nova data)`
2. Verificar que campos foram atualizados

**Resultado Esperado:**
- `result.IsSuccess == true`

**Critério de Aceite:** `result.IsSuccess == true`

---

### TC-CGV-027 — Desativar janela de freeze

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | DeactivateFreezeWindow |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `DeactivateFreezeWindow.Handler` |

**Pré-condições:**
- Janela de freeze ativa existente

**Passos:**
1. Enviar `DeactivateFreezeWindow.Command(FreezeWindowId)`
2. Verificar que janela é marcada como inativa

**Resultado Esperado:**
- `result.IsSuccess == true`
- Janela não aparece mais em consultas de freezes ativos

**Critério de Aceite:** `result.IsSuccess == true`

---

### TC-CGV-028 — Listar janelas de freeze do tenant

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | ListFreezeWindows |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `ListFreezeWindows.Handler` |

**Pré-condições:**
- 3 janelas de freeze: 2 ativas, 1 inativa

**Passos:**
1. Enviar `ListFreezeWindows.Query(OnlyActive = true)`
2. Verificar que apenas 2 são retornadas

**Resultado Esperado:**
- `result.Value.Count == 2`

**Critério de Aceite:** `result.IsSuccess == true`

---

## 4. Fluxos de Aprovação

### TC-CGV-029 — Criar política de aprovação para ambiente de produção

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | CreateApprovalPolicy |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Handler** | `CreateApprovalPolicy.Handler` |

**Pré-condições:**
- Nenhuma política para `production` e `Breaking` existente

**Passos:**
1. Enviar `CreateApprovalPolicy.Command(Environment = "production", MinChangeLevel = "Breaking", RequiredApprovers = ["team-lead", "security"], TimeoutHours = 24)`
2. Verificar que política é persistida

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.PolicyId != Guid.Empty`

**Critério de Aceite:** `result.IsSuccess == true`

---

### TC-CGV-030 — Responder ao pedido de aprovação com decisão "Approved"

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | RespondToApprovalRequest |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Handler** | `RespondToApprovalRequest.Handler` |

**Pré-condições:**
- `ApprovalRequest` em status `Pending` com token de callback válido
- `IDateTimeProvider` retorna data dentro do prazo de validade do token

**Passos:**
1. Instanciar handler com substitutos
2. Enviar `Command(CallbackToken = "token-valido", Decision = "Approved", RespondedBy = "team-lead@empresa.com")`
3. Verificar que token é hasheado (SHA-256) para busca segura
4. Verificar que `ApprovalRequest.Respond` é chamado com `Approved`
5. Verificar commit

**Resultado Esperado:**
- `result.Value.Status == "Approved"`
- `result.Value.Updated == true`

**Critério de Aceite:** `result.Value.Status == "Approved" && result.Value.Updated == true`

---

### TC-CGV-031 — Responder ao pedido de aprovação com decisão "Rejected"

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | RespondToApprovalRequest |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `RespondToApprovalRequest.Handler` |

**Pré-condições:**
- `ApprovalRequest` em status `Pending` com token válido

**Passos:**
1. Enviar `Command(CallbackToken, Decision = "Rejected", RespondedBy = "security@empresa.com", Comments = "Mudança requer mais análise")`
2. Verificar atualização de status para `Rejected`

**Resultado Esperado:**
- `result.Value.Status == "Rejected"`
- `result.Value.Updated == true`

**Critério de Aceite:** `result.Value.Status == "Rejected"`

---

### TC-CGV-032 — Rejeitar token de callback inválido

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | RespondToApprovalRequest |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `RespondToApprovalRequest.Handler` |

**Pré-condições:**
- `IApprovalRequestRepository.GetByCallbackTokenHashAsync` retorna `null` para token fornecido

**Passos:**
1. Enviar `Command(CallbackToken = "token-inexistente", Decision = "Approved", RespondedBy = "user")`

**Resultado Esperado:**
- `result.IsFailure == true`
- `result.Error.Code == "APPROVAL_REQUEST_NOT_FOUND"`

**Critério de Aceite:** `result.Error.Code == "APPROVAL_REQUEST_NOT_FOUND"`

---

### TC-CGV-033 — Token de callback expirado deve ser rejeitado

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | RespondToApprovalRequest |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `RespondToApprovalRequest.Handler` |

**Pré-condições:**
- `ApprovalRequest` com `ExpiresAt` 2 horas no passado
- `IDateTimeProvider.UtcNow` retorna data após `ExpiresAt`

**Passos:**
1. Configurar `dateTimeProvider.UtcNow = ApprovalRequest.ExpiresAt + 2h`
2. Enviar `Command(CallbackToken válido, Decision = "Approved", RespondedBy = "user")`

**Resultado Esperado:**
- `result.IsFailure == true`
- `result.Error.Code == "APPROVAL_TOKEN_EXPIRED"`

**Critério de Aceite:** `result.Error.Code == "APPROVAL_TOKEN_EXPIRED"`

---

### TC-CGV-034 — Resposta duplicada a aprovação já processada é idempotente

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | RespondToApprovalRequest |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `RespondToApprovalRequest.Handler` |

**Pré-condições:**
- `ApprovalRequest` já em status `Approved` (já processada)

**Passos:**
1. Enviar `Command(CallbackToken, Decision = "Approved", RespondedBy = "user")` para requisição já aprovada

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.Updated == false` (nenhuma alteração)
- `result.Value.Status == "Approved"` (estado original mantido)

**Critério de Aceite:** `result.Value.Updated == false`

---

### TC-CGV-035 — Decisão inválida deve falhar na validação

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | RespondToApprovalRequest / Validator |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `RespondToApprovalRequest.Validator` |

**Pré-condições:**
- Nenhuma

**Passos:**
1. Instanciar `RespondToApprovalRequest.Validator`
2. Chamar `Validate` com `Decision = "MaybeYes"` (valor inválido)

**Resultado Esperado:**
- `ValidationResult.IsValid == false`
- Mensagem: `"Decision must be 'Approved' or 'Rejected'."`

**Critério de Aceite:** `errors.Any(e => e.PropertyName == "Decision")`

---

### TC-CGV-036 — Listar aprovações pendentes

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | ListPendingApprovals |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `ListPendingApprovals.Handler` |

**Pré-condições:**
- 3 ApprovalRequests em status `Pending`; 2 em status `Approved`

**Passos:**
1. Enviar `ListPendingApprovals.Query`
2. Verificar que apenas 3 são retornadas

**Resultado Esperado:**
- `result.Value.Count == 3`

**Critério de Aceite:** `result.Value.All(a => a.Status == "Pending")`

---

### TC-CGV-037 — Relatório de workflow de aprovação

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | GetApprovalWorkflowReport |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `GetApprovalWorkflowReport.Handler` |

**Pré-condições:**
- 10 aprovações nos últimos 30 dias: 7 aprovadas, 2 rejeitadas, 1 expirada

**Passos:**
1. Enviar `GetApprovalWorkflowReport.Query(Period = "30d")`
2. Verificar métricas de aprovação

**Resultado Esperado:**
- `result.Value.TotalRequests == 10`
- `result.Value.ApprovalRate == 70`

**Critério de Aceite:** `result.IsSuccess == true`

---

## 5. Promotion Gates — Criação, Avaliação e Override

### TC-CGV-038 — Criar gate de promoção obrigatório

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | CreatePromotionGate |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Handler** | `CreatePromotionGate.Handler` |

**Pré-condições:**
- Ambiente `production` configurado

**Passos:**
1. Enviar `CreatePromotionGate.Command(EnvironmentId, GateName = "Security Scan", IsRequired = true, GateType = "SecurityScan")`
2. Verificar que gate é persistido como obrigatório

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.IsRequired == true`

**Critério de Aceite:** `result.Value.IsRequired == true`

---

### TC-CGV-039 — Avaliar gates de promoção — todos aprovados

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | EvaluatePromotionGates |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Handler** | `EvaluatePromotionGates.Handler` |

**Pré-condições:**
- `PromotionRequest` em status `Pending`
- 2 gates configurados: `SecurityScan` (required) e `UnitTests` (required)
- `ChangePromotionGatesEnabled = true` para o ambiente
- `IPromotionRequestRepository.GetByIdAsync` retorna a requisição

**Passos:**
1. Instanciar handler com todos os substitutos
2. Enviar `Command(PromotionRequestId, EvaluatedBy = "ci-pipeline", Evaluations = [{GateId1, Passed=true}, {GateId2, Passed=true}])`
3. Verificar que ambos os gates passam e promoção é aprovada

**Resultado Esperado:**
- `result.Value.Status == "Approved"`
- `result.Value.AllRequiredPassed == true`
- `result.Value.PassedGates == 2`
- `result.Value.TotalGates == 2`

**Critério de Aceite:** `result.Value.AllRequiredPassed == true && result.Value.Status == "Approved"`

---

### TC-CGV-040 — Gate obrigatório reprovado bloqueia promoção

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | EvaluatePromotionGates |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Handler** | `EvaluatePromotionGates.Handler` |

**Pré-condições:**
- `PromotionRequest` em status `Pending`
- Gate `SecurityScan` (IsRequired = true)
- Gates habilitados para o ambiente

**Passos:**
1. Enviar `Command(PromotionRequestId, Evaluations = [{SecurityScanGateId, Passed=false}])`
2. Verificar que promoção é rejeitada

**Resultado Esperado:**
- `result.Value.Status == "Rejected"`
- `result.Value.AllRequiredPassed == false`

**Critério de Aceite:** `result.Value.Status == "Rejected" && result.Value.AllRequiredPassed == false`

---

### TC-CGV-041 — Gates desabilitados para ambiente: bypass automático

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | EvaluatePromotionGates |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `EvaluatePromotionGates.Handler` |

**Pré-condições:**
- `IEnvironmentBehaviorService.IsEnabledAsync("env.behavior.change.promotion_gates.enabled")` retorna `false`
- `PromotionRequest` em status `Pending` para ambiente `staging`

**Passos:**
1. Configurar `environmentBehaviorService` para retornar `false`
2. Enviar `Command(PromotionRequestId, EvaluatedBy = "ci-pipeline", Evaluations = [])`

**Resultado Esperado:**
- `result.Value.Status == "Approved"` (bypass automático)
- `result.Value.AllRequiredPassed == true`
- Todas as avaliações registradas com `Details` contendo "Bypassed"

**Critério de Aceite:** `result.Value.Status == "Approved"`

---

### TC-CGV-042 — Gate GateId inválido retorna NotFound

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | EvaluatePromotionGates |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `EvaluatePromotionGates.Handler` |

**Pré-condições:**
- `PromotionRequest` válida
- Gates habilitados
- GateId na avaliação não existe no mapa de gates do ambiente

**Passos:**
1. Enviar `Command` com `GateId = Guid.NewGuid()` (não cadastrado)

**Resultado Esperado:**
- `result.IsFailure == true`
- Erro indica gate não encontrado

**Critério de Aceite:** `result.IsFailure == true`

---

### TC-CGV-043 — Override justificado de gate reprovado

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | OverrideGateWithJustification |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Handler** | `OverrideGateWithJustification.Handler` |

**Pré-condições:**
- `GateEvaluation` existente com `Passed = false`
- `IGateEvaluationRepository.GetByIdAsync` retorna a avaliação

**Passos:**
1. Instanciar handler com substitutos
2. Enviar `Command(GateEvaluationId, Justification = "Aprovado pelo CISO — risco aceito formalmente", OverriddenBy = "ciso@empresa.com")`
3. Verificar que `evaluation.Override(...)` foi chamado
4. Verificar commit

**Resultado Esperado:**
- `result.Value.GateEvaluationId == gateEvaluationId`
- `result.Value.OverrideJustification == "Aprovado pelo CISO — risco aceito formalmente"`

**Critério de Aceite:** `result.Value.OverrideJustification != null`

---

### TC-CGV-044 — Override de gate inexistente retorna NotFound

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | OverrideGateWithJustification |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `OverrideGateWithJustification.Handler` |

**Pré-condições:**
- `IGateEvaluationRepository.GetByIdAsync` retorna `null`

**Passos:**
1. Enviar `Command(GateEvaluationId = Guid.NewGuid(), Justification = "...", OverriddenBy = "user")`

**Resultado Esperado:**
- `result.IsFailure == true`
- `result.Error.Type == ErrorType.NotFound`

**Critério de Aceite:** `result.Error.Type == ErrorType.NotFound`

---

### TC-CGV-045 — Override sem justificativa deve falhar na validação

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | OverrideGateWithJustification / Validator |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `OverrideGateWithJustification.Validator` |

**Pré-condições:**
- Nenhuma

**Passos:**
1. Instanciar `OverrideGateWithJustification.Validator`
2. Chamar `Validate` com `Justification = ""`

**Resultado Esperado:**
- `ValidationResult.IsValid == false`
- Erro em `Justification`

**Critério de Aceite:** `errors.Any(e => e.PropertyName == "Justification")`

---

### TC-CGV-046 — Criar solicitação de promoção

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | CreatePromotionRequest |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Handler** | `CreatePromotionRequest.Handler` |

**Pré-condições:**
- Ambiente origem `staging` e destino `production` configurados
- Release em estado válido para promoção

**Passos:**
1. Enviar `CreatePromotionRequest.Command(ReleaseId, SourceEnvironmentId, TargetEnvironmentId, RequestedBy = "engenheiro@empresa.com")`
2. Verificar que solicitação é criada com status `Pending`

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.Status == "Pending"`

**Critério de Aceite:** `result.Value.Status == "Pending"`

---

### TC-CGV-047 — Bloquear promoção manualmente

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | BlockPromotion |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `BlockPromotion.Handler` |

**Pré-condições:**
- `PromotionRequest` em status `Pending`

**Passos:**
1. Enviar `BlockPromotion.Command(PromotionRequestId, Reason = "Incidente P1 em produção — freeze manual")`
2. Verificar que promoção é bloqueada

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.Status == "Blocked"`

**Critério de Aceite:** `result.Value.Status == "Blocked"`

---

### TC-CGV-048 — Aprovar promoção manualmente

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | ApprovePromotion |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `ApprovePromotion.Handler` |

**Pré-condições:**
- `PromotionRequest` em status `InEvaluation`

**Passos:**
1. Enviar `ApprovePromotion.Command(PromotionRequestId, ApprovedBy = "diretor@empresa.com")`
2. Verificar que promoção é aprovada

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.Status == "Approved"`

**Critério de Aceite:** `result.Value.Status == "Approved"`

---

### TC-CGV-049 — Obter delta de prontidão para promoção

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | GetPromotionReadinessDelta |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `GetPromotionReadinessDelta.Handler` |

**Pré-condições:**
- Versão em `staging` com 2 dimensões aprovadas e 1 pendente

**Passos:**
1. Enviar `GetPromotionReadinessDelta.Query(ReleaseId, TargetEnvironment = "production")`
2. Verificar delta de prontidão

**Resultado Esperado:**
- `result.Value.ReadinessPercentage < 100`
- Dimensão pendente listada em `Gaps`

**Critério de Aceite:** `result.IsSuccess == true`

---

### TC-CGV-050 — Listar gates por ambiente

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | ListPromotionGatesByEnvironment |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `ListPromotionGatesByEnvironment.Handler` |

**Pré-condições:**
- 3 gates configurados para `production`; 1 gate para `staging`

**Passos:**
1. Enviar `ListPromotionGatesByEnvironment.Query(EnvironmentId = productionId)`
2. Verificar que apenas 3 gates são retornados

**Resultado Esperado:**
- `result.Value.Count == 3`

**Critério de Aceite:** `result.IsSuccess == true`

---

## 6. Evidence Packs — Execução de Lint e Evidências

### TC-CGV-051 — Executar lint para release com findings

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | ExecuteLintForRelease |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Handler** | `ExecuteLintForRelease.Handler` |

**Pré-condições:**
- `IRulesetRepository.GetByIdAsync` retorna ruleset válido
- `ILintResultRepository` disponível

**Passos:**
1. Instanciar handler com substitutos
2. Enviar `Command(RulesetId, ReleaseId, ApiAssetId, Findings = [{Rule="no-400-without-schema", Severity=Error, Message="...", Path="/orders"}, {Rule="must-have-examples", Severity=Warning, Message="...", Path="/orders"}])`
3. Verificar cálculo de score: `100 - (1×10) - (1×5) = 85`
4. Verificar que resultado é persistido

**Resultado Esperado:**
- `result.Value.Score == 85`
- `result.Value.TotalFindings == 2`
- `result.Value.LintResultId != Guid.Empty`

**Critério de Aceite:** `result.Value.Score == 85 && result.Value.TotalFindings == 2`

---

### TC-CGV-052 — Score de lint zerado com múltiplos erros graves

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | ExecuteLintForRelease |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `ExecuteLintForRelease.Handler` |

**Pré-condições:**
- 15 findings de severidade `Error`

**Passos:**
1. Enviar `Command` com 15 findings `Severity = Error`
2. Verificar que score não fica negativo (clampado em 0)

**Resultado Esperado:**
- `result.Value.Score == 0` (clampado: `100 - 15×10 = -50 → 0`)

**Critério de Aceite:** `result.Value.Score == 0`

---

### TC-CGV-053 — Lint com ruleset inexistente retorna NotFound

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | ExecuteLintForRelease |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `ExecuteLintForRelease.Handler` |

**Pré-condições:**
- `IRulesetRepository.GetByIdAsync` retorna `null`

**Passos:**
1. Enviar `Command(RulesetId = Guid.NewGuid(), ...)`

**Resultado Esperado:**
- `result.IsFailure == true`
- Erro indica ruleset não encontrado

**Critério de Aceite:** `result.IsFailure == true`

---

### TC-CGV-054 — Anexar provenance SLSA a release

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | AttachSlsaProvenance |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `AttachSlsaProvenance.Handler` |

**Pré-condições:**
- Release existente
- Provenance SLSA nível 2 disponível

**Passos:**
1. Enviar `AttachSlsaProvenance.Command(ReleaseId, ProvenanceJson, SlsaLevel = 2)`
2. Verificar que provenance é associado à release

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.SlsaLevel == 2`

**Critério de Aceite:** `result.IsSuccess == true`

---

### TC-CGV-055 — Anexar evidência de CI/CD

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | AttachCiCdEvidence |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `AttachCiCdEvidence.Handler` |

**Pré-condições:**
- Release existente com pipeline CI finalizado

**Passos:**
1. Enviar `AttachCiCdEvidence.Command(ReleaseId, PipelineId = "pipeline-456", PipelineUrl, Status = "success", CoveragePercent = 87.5)`
2. Verificar que evidência é persistida

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.EvidenceId != Guid.Empty`

**Critério de Aceite:** `result.IsSuccess == true`

---

### TC-CGV-056 — Obter pacote de evidências de release

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | GetEvidencePack |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `GetEvidencePack.Handler` |

**Pré-condições:**
- Release com SLSA provenance, CI/CD evidence e lint result anexados

**Passos:**
1. Enviar `GetEvidencePack.Query(ReleaseId)`
2. Verificar que todas as 3 evidências são retornadas no pacote

**Resultado Esperado:**
- `result.Value.Items.Count == 3`
- Evidências dos tipos `SlsaProvenance`, `CiCdEvidence`, `LintResult` presentes

**Critério de Aceite:** `result.Value.Items.Count == 3`

---

### TC-CGV-057 — Relatório de cobertura do pacote de evidências

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | GetEvidencePackCoverageReport |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `GetEvidencePackCoverageReport.Handler` |

**Pré-condições:**
- 10 releases nos últimos 30 dias; 7 com evidence pack completo

**Passos:**
1. Enviar `GetEvidencePackCoverageReport.Query(Period = "30d")`
2. Verificar taxa de cobertura de 70%

**Resultado Esperado:**
- `result.Value.CoverageRate == 70`

**Critério de Aceite:** `result.IsSuccess == true`

---

## 7. Deployment e Pós-Release

### TC-CGV-058 — Atualizar estado de deployment

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | UpdateDeploymentState |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Handler** | `UpdateDeploymentState.Handler` |

**Pré-condições:**
- Release existente com deployment em andamento

**Passos:**
1. Enviar `UpdateDeploymentState.Command(ReleaseId, State = "Deployed", Environment = "production", DeployedAt = now)`
2. Verificar que estado é atualizado

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.State == "Deployed"`

**Critério de Aceite:** `result.IsSuccess == true`

---

### TC-CGV-059 — Registrar rollback de release

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | RegisterRollback |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Handler** | `RegisterRollback.Handler` |

**Pré-condições:**
- Release deployada em `production` com incidente registrado

**Passos:**
1. Enviar `RegisterRollback.Command(ReleaseId, Reason = "Regressão em checkout", TargetVersion = "1.2.3", RolledBackBy = "oncall@empresa.com")`
2. Verificar que rollback é registrado e release anterior restaurada

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.RollbackId != Guid.Empty`

**Critério de Aceite:** `result.IsSuccess == true`

---

### TC-CGV-060 — Iniciar revisão pós-release

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | StartPostReleaseReview |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `StartPostReleaseReview.Handler` |

**Pré-condições:**
- Release em estado `Deployed`

**Passos:**
1. Enviar `StartPostReleaseReview.Command(ReleaseId, ReviewerIds = ["dev1", "dev2"])`
2. Verificar que revisão é iniciada com status `InProgress`

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.ReviewStatus == "InProgress"`

**Critério de Aceite:** `result.IsSuccess == true`

---

### TC-CGV-061 — Obter relatório de impacto de release

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | GetReleaseImpactReport |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `GetReleaseImpactReport.Handler` |

**Pré-condições:**
- Release com blast radius calculado e métricas de observabilidade disponíveis

**Passos:**
1. Enviar `GetReleaseImpactReport.Query(ReleaseId)`
2. Verificar que relatório contém métricas de impacto

**Resultado Esperado:**
- `result.Value.AffectedConsumers > 0`
- `result.Value.ErrorRateDelta` presente

**Critério de Aceite:** `result.IsSuccess == true`

---

### TC-CGV-062 — Registrar baseline de release

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | RecordReleaseBaseline |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `RecordReleaseBaseline.Handler` |

**Pré-condições:**
- Release deployada com sucesso

**Passos:**
1. Enviar `RecordReleaseBaseline.Command(ReleaseId, ErrorRate = 0.1, P99LatencyMs = 250, SloTarget = 99.9)`
2. Verificar que baseline é persistido

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.BaselineId != Guid.Empty`

**Critério de Aceite:** `result.IsSuccess == true`

---

## 8. Workflow Templates

### TC-CGV-063 — Criar template de workflow de aprovação

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | CreateWorkflowTemplate |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `CreateWorkflowTemplate.Handler` |

**Pré-condições:**
- Nenhum template existente com mesmo nome

**Passos:**
1. Enviar `CreateWorkflowTemplate.Command(Name = "Deploy Produção Padrão", Steps = ["lint", "security-scan", "approve", "deploy"])`
2. Verificar que template é persistido com 4 steps

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.StepCount == 4`

**Critério de Aceite:** `result.IsSuccess == true`

---

### TC-CGV-064 — Instanciar template de workflow

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | InstantiateTemplate |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `InstantiateTemplate.Handler` |

**Pré-condições:**
- Template de workflow existente
- Release existente

**Passos:**
1. Enviar `InstantiateTemplate.Command(TemplateId, ReleaseId)`
2. Verificar que instância de workflow é criada com todos os steps

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.WorkflowId != Guid.Empty`

**Critério de Aceite:** `result.IsSuccess == true`

---

### TC-CGV-065 — Obter status de workflow em execução

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | GetWorkflowStatus |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `GetWorkflowStatus.Handler` |

**Pré-condições:**
- Workflow em execução com 2 steps concluídos e 1 pendente

**Passos:**
1. Enviar `GetWorkflowStatus.Query(WorkflowId)`
2. Verificar que status reflete estado atual

**Resultado Esperado:**
- `result.Value.CompletedSteps == 2`
- `result.Value.PendingSteps == 1`
- `result.Value.OverallStatus == "InProgress"`

**Critério de Aceite:** `result.IsSuccess == true`

---

## 9. Relatórios e Análises de Mudança

### TC-CGV-066 — Relatório de blast radius por serviço

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | GetBlastRadiusReport |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `GetBlastRadiusReport.Handler` |

**Pré-condições:**
- Múltiplas releases com blast radius calculado

**Passos:**
1. Enviar `GetBlastRadiusReport.Query(ServiceId, Period = "30d")`
2. Verificar dados de blast radius por release

**Resultado Esperado:**
- `result.Value.AverageBlastRadius >= 0`
- `result.Value.MaxBlastRadius >= result.Value.AverageBlastRadius`

**Critério de Aceite:** `result.IsSuccess == true`

---

### TC-CGV-067 — Heatmap de frequência de mudanças

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | GetChangeFrequencyHeatmap |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `GetChangeFrequencyHeatmap.Handler` |

**Pré-condições:**
- 90 dias de histórico de releases

**Passos:**
1. Enviar `GetChangeFrequencyHeatmap.Query(Period = "90d")`
2. Verificar que heatmap contém 90 entradas (uma por dia)

**Resultado Esperado:**
- `result.Value.DataPoints.Count == 90`
- Cada ponto tem `Date` e `ChangeCount`

**Critério de Aceite:** `result.IsSuccess == true`

---

### TC-CGV-068 — Relatório de lead time de mudanças

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | GetChangeLeadTimeReport |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `GetChangeLeadTimeReport.Handler` |

**Pré-condições:**
- 20 releases com timestamps de commit e deploy registrados

**Passos:**
1. Enviar `GetChangeLeadTimeReport.Query(Period = "30d")`
2. Verificar cálculo de lead time médio e percentis

**Resultado Esperado:**
- `result.Value.AverageLeadTimeHours > 0`
- `result.Value.P95LeadTimeHours >= result.Value.AverageLeadTimeHours`

**Critério de Aceite:** `result.IsSuccess == true`

---

### TC-CGV-069 — Previsão de risco de mudança (ML)

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | GetChangeRiskPrediction |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `GetChangeRiskPrediction.Handler` |

**Pré-condições:**
- Release com características: hora do deploy (sexta 17h), blast radius alto, sem testes

**Passos:**
1. Enviar `GetChangeRiskPrediction.Query(ReleaseId)`
2. Verificar que risco previsto é alto

**Resultado Esperado:**
- `result.Value.RiskLevel == "High"`
- `result.Value.RiskScore > 70`

**Critério de Aceite:** `result.IsSuccess == true`

---

### TC-CGV-070 — Relatório de cadência de deploys

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | GetDeploymentCadenceReport |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `GetDeploymentCadenceReport.Handler` |

**Pré-condições:**
- 30 deploys nos últimos 30 dias

**Passos:**
1. Enviar `GetDeploymentCadenceReport.Query(Period = "30d")`
2. Verificar frequência diária

**Resultado Esperado:**
- `result.Value.DeploymentsPerDay == 1.0` (30 deploys / 30 dias)
- `result.Value.TotalDeployments == 30`

**Critério de Aceite:** `result.IsSuccess == true`

---

### TC-CGV-071 — Relatório de taxa de sucesso de releases

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | GetReleaseSuccessRateReport |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `GetReleaseSuccessRateReport.Handler` |

**Pré-condições:**
- 10 releases: 8 com sucesso, 2 com rollback

**Passos:**
1. Enviar `GetReleaseSuccessRateReport.Query(Period = "30d")`
2. Verificar taxa de sucesso

**Resultado Esperado:**
- `result.Value.SuccessRate == 80`
- `result.Value.RollbackCount == 2`

**Critério de Aceite:** `result.Value.SuccessRate == 80`

---

### TC-CGV-072 — Relatório de padrão de rollbacks

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | GetRollbackPatternReport |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `GetRollbackPatternReport.Handler` |

**Pré-condições:**
- 5 rollbacks nos últimos 90 dias com motivos documentados

**Passos:**
1. Enviar `GetRollbackPatternReport.Query(Period = "90d")`
2. Verificar padrões identificados (ex: maioria às sextas, serviços recorrentes)

**Resultado Esperado:**
- `result.Value.TotalRollbacks == 5`
- `result.Value.TopRollbackReasons` não está vazio

**Critério de Aceite:** `result.IsSuccess == true`

---

### TC-CGV-073 — Relatório de velocidade de mudança por equipe

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | GetTeamChangeVelocityReport |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `GetTeamChangeVelocityReport.Handler` |

**Pré-condições:**
- 3 equipes com histórico de deploys nos últimos 30 dias

**Passos:**
1. Enviar `GetTeamChangeVelocityReport.Query(Period = "30d")`
2. Verificar que cada equipe tem métricas individuais

**Resultado Esperado:**
- `result.Value.Teams.Count == 3`
- Cada equipe tem `DeployFrequency`, `LeadTime`, `ChangeFailureRate`

**Critério de Aceite:** `result.IsSuccess == true`

---

### TC-CGV-074 — Relatório de distribuição de blast radius

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | GetBlastRadiusDistributionReport |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `GetBlastRadiusDistributionReport.Handler` |

**Pré-condições:**
- Releases com blast radius variando de 1 a 100 consumidores

**Passos:**
1. Enviar `GetBlastRadiusDistributionReport.Query(Period = "30d")`
2. Verificar distribuição em faixas (1-10, 11-50, 51-100, 100+)

**Resultado Esperado:**
- `result.Value.Buckets.Count == 4`

**Critério de Aceite:** `result.IsSuccess == true`

---

### TC-CGV-075 — Resumo de mudanças do tenant

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | GetChangesSummary |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `GetChangesSummary.Handler` |

**Pré-condições:**
- 50 mudanças nos últimos 7 dias: 5 breaking, 30 additive, 15 patch

**Passos:**
1. Enviar `GetChangesSummary.Query(Period = "7d")`
2. Verificar contagem por nível de mudança

**Resultado Esperado:**
- `result.Value.Breaking == 5`
- `result.Value.Additive == 30`
- `result.Value.Patch == 15`
- `result.Value.Total == 50`

**Critério de Aceite:** `result.Value.Total == 50`

---

## 10. Segurança — Isolamento Multi-Tenant

### TC-CGV-076 — Isolamento de tenant em ListReleases

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | ListReleases |
| **Tipo** | Segurança |
| **Prioridade** | Crítica |
| **Handler** | `ListReleases.Handler` |

**Pré-condições:**
- Tenant A possui 8 releases
- Tenant B possui 5 releases
- `TenantRlsInterceptor` ativo

**Passos:**
1. Autenticar como tenant A
2. Enviar `ListReleases.Query` sem filtros de tenant
3. Verificar que apenas as 8 releases do tenant A são retornadas

**Resultado Esperado:**
- `result.Value.Count == 8`
- Nenhuma release do tenant B está presente

**Critério de Aceite:** `result.Value.All(r => r.TenantId == tenantA.Id)`

---

### TC-CGV-077 — Isolamento de tenant em GetRelease — acesso cruzado bloqueado

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | GetRelease |
| **Tipo** | Segurança |
| **Prioridade** | Crítica |
| **Handler** | `GetRelease.Handler` |

**Pré-condições:**
- Release pertence ao tenant B
- Usuário autenticado no tenant A

**Passos:**
1. Configurar `ICurrentTenant.Id = tenantA`
2. Configurar repositório para retornar `null` (filtro RLS ativo para tenant A)
3. Enviar `GetRelease.Query(releaseIdDeTenantB)`

**Resultado Esperado:**
- `result.IsFailure == true`
- `result.Error.Type == ErrorType.NotFound`

**Critério de Aceite:** `result.Error.Type == ErrorType.NotFound`

---

### TC-CGV-078 — Freeze de tenant A não afeta tenant B

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | CheckFreezeConflict |
| **Tipo** | Segurança |
| **Prioridade** | Crítica |
| **Handler** | `CheckFreezeConflict.Handler` |

**Pré-condições:**
- Janela de freeze ativa no tenant A
- `ICurrentTenant` configurado para tenant B

**Passos:**
1. Autenticar como tenant B
2. Enviar `CheckFreezeConflict.Query(At = agora, Environment = "production")`
3. Verificar que freeze do tenant A não é retornado

**Resultado Esperado:**
- `result.Value.HasConflict == false`
- `result.Value.ActiveFreezes.Count == 0`

**Critério de Aceite:** `result.Value.HasConflict == false`

---

### TC-CGV-079 — Policies de aprovação isoladas por tenant

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | ListApprovalPolicies |
| **Tipo** | Segurança |
| **Prioridade** | Crítica |
| **Handler** | `ListApprovalPolicies.Handler` |

**Pré-condições:**
- Tenant A tem 3 policies; Tenant B tem 2 policies
- RLS ativo no contexto

**Passos:**
1. Autenticar como tenant A
2. Enviar `ListApprovalPolicies.Query`
3. Verificar que apenas 3 policies do tenant A são retornadas

**Resultado Esperado:**
- `result.Value.Count == 3`

**Critério de Aceite:** `result.Value.All(p => p.TenantId == tenantA.Id)`

---

## 11. Fluxos End-to-End

### TC-CGV-080 — Fluxo completo: commit → blast radius → aprovação → deploy

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | Fluxo E2E |
| **Tipo** | Fluxo |
| **Prioridade** | Crítica |
| **Handler** | Múltiplos handlers |

**Pré-condições:**
- Tenant com pipeline CI/CD integrado
- Ambiente `production` com gate de segurança obrigatório
- Policy de aprovação para `Breaking` changes

**Passos:**
1. `IngestCommit` → release criada com `ChangeLevel = Breaking`
2. `CalculateBlastRadius` → 25 consumidores afetados, score recalculado
3. `CheckDeployReadiness` → bloqueado por política de aprovação
4. `RespondToApprovalRequest(Decision = "Approved")` → aprovação registrada
5. `CreatePromotionRequest` → solicitação `Pending`
6. `EvaluatePromotionGates` → gate aprovado
7. `UpdateDeploymentState(State = "Deployed")`

**Resultado Esperado:**
- Cada etapa retorna `result.IsSuccess == true`
- Deploy final registrado em `production`

**Critério de Aceite:** Todos os handlers retornam `IsSuccess == true` em sequência

---

### TC-CGV-081 — Fluxo de rollback: deploy falho → rollback → revisão pós-release

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | Fluxo de Rollback |
| **Tipo** | Fluxo |
| **Prioridade** | Crítica |
| **Handler** | `RegisterRollback`, `StartPostReleaseReview`, `GetPostReleaseReview` |

**Pré-condições:**
- Release deployada com falha detectada (taxa de erro >5%)
- Versão anterior disponível para rollback

**Passos:**
1. `UpdateDeploymentState(State = "Failed")` → release marcada como falha
2. `AssessRollbackViability` → viabilidade confirmada
3. `RegisterRollback(Reason = "Taxa de erro 8% — acima do SLO")` → rollback registrado
4. `StartPostReleaseReview` → revisão iniciada
5. `GetPostReleaseReview` → dados da revisão retornados

**Resultado Esperado:**
- Rollback registrado com `RollbackId` válido
- Revisão iniciada com status `InProgress`

**Critério de Aceite:** `result.IsSuccess == true` em todas as etapas

---

### TC-CGV-082 — Freeze conflict bloqueia deploy automático

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | CheckFreezeConflict + CheckDeployReadiness |
| **Tipo** | Fluxo |
| **Prioridade** | Crítica |
| **Handler** | Múltiplos handlers |

**Pré-condições:**
- Janela de freeze `Global` ativa para o período atual
- Release pronta para deploy

**Passos:**
1. `CheckFreezeConflict(At = agora)` → `HasConflict = true`
2. `CheckDeployReadiness(ReleaseId)` → deve incluir freeze como bloqueador

**Resultado Esperado:**
- `CheckFreezeConflict.HasConflict == true`
- `CheckDeployReadiness.IsReady == false`
- `Blockers` contém item referente ao freeze ativo

**Critério de Aceite:** `CheckDeployReadiness.Blockers.Any(b => b.Type == "FreezeConflict")`

---

### TC-CGV-083 — Gate de vulnerabilidade bloqueia promoção para produção

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | EvaluatePromotionGates (gate de vulnerabilidade) |
| **Tipo** | Fluxo |
| **Prioridade** | Crítica |
| **Handler** | `EvaluatePromotionGates.Handler` |

**Pré-condições:**
- Gate `VulnerabilityScan` configurado como `IsRequired = true` em `production`
- Avaliação do gate indica `Passed = false` (CVE crítica encontrada)

**Passos:**
1. `CreatePromotionRequest` para `production`
2. `EvaluatePromotionGates(Evaluations = [{VulnerabilityGateId, Passed=false}])`

**Resultado Esperado:**
- `result.Value.Status == "Rejected"`
- `result.Value.AllRequiredPassed == false`

**Critério de Aceite:** `result.Value.Status == "Rejected"`

---

### TC-CGV-084 — Exclusão de política de aprovação

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | DeleteApprovalPolicy |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `DeleteApprovalPolicy.Handler` |

**Pré-condições:**
- Policy de aprovação existente e sem aprovações pendentes vinculadas

**Passos:**
1. Enviar `DeleteApprovalPolicy.Command(PolicyId)`
2. Verificar que policy é removida

**Resultado Esperado:**
- `result.IsSuccess == true`
- Policy não retornada em `ListApprovalPolicies`

**Critério de Aceite:** `result.IsSuccess == true`

---

### TC-CGV-085 — Verificar se janela de mudança está aberta

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | IsChangeWindowOpen |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `IsChangeWindowOpen.Handler` |

**Pré-condições:**
- Janela de mudança configurada para seg-sex 08h-18h

**Passos:**
1. Configurar `IDateTimeProvider` para retornar quarta 14h
2. Enviar `IsChangeWindowOpen.Query(Environment = "production")`

**Resultado Esperado:**
- `result.Value.IsOpen == true`

**Critério de Aceite:** `result.Value.IsOpen == true`

---

### TC-CGV-086 — Janela de mudança fechada fora do horário

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | IsChangeWindowOpen |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `IsChangeWindowOpen.Handler` |

**Pré-condições:**
- Janela de mudança configurada para seg-sex 08h-18h

**Passos:**
1. Configurar `IDateTimeProvider` para retornar sábado 10h
2. Enviar `IsChangeWindowOpen.Query(Environment = "production")`

**Resultado Esperado:**
- `result.Value.IsOpen == false`

**Critério de Aceite:** `result.Value.IsOpen == false`

---

### TC-CGV-087 — Relatório DORA Metrics — deploy frequency e lead time

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | GetDoraMetrics (interno) |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `GetDoraMetrics.Handler` |

**Pré-condições:**
- 30 dias de histórico de deploys com timestamps de commit e deploy

**Passos:**
1. Enviar `GetDoraMetrics.Query(Period = "30d")`
2. Verificar as 4 métricas DORA: Deployment Frequency, Lead Time for Changes, Change Failure Rate, MTTR

**Resultado Esperado:**
- `result.Value.DeploymentFrequency > 0`
- `result.Value.LeadTimeForChangesHours > 0`
- `result.Value.ChangeFailureRate >= 0`
- `result.Value.MttrHours >= 0`

**Critério de Aceite:** `result.IsSuccess == true`

---

### TC-CGV-088 — Solicitar aprovação externa (EvaluateChangeAdvisoryBoard)

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | EvaluateChangeAdvisoryBoard |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `EvaluateChangeAdvisoryBoard.Handler` |

**Pré-condições:**
- Release de nível `Breaking` para ambiente `production`
- Membros do CAB configurados

**Passos:**
1. Enviar `EvaluateChangeAdvisoryBoard.Command(ReleaseId)`
2. Verificar que notificações foram enviadas para membros do CAB
3. Verificar que status de aprovação é `PendingCab`

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.CabStatus == "PendingCab"`

**Critério de Aceite:** `result.IsSuccess == true`

---

### TC-CGV-089 — Princípio dos quatro olhos — aprovação dual

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | EvaluateFourEyesPrinciple |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `EvaluateFourEyesPrinciple.Handler` |

**Pré-condições:**
- Release criada pelo usuário A
- Apenas 1 aprovação registrada (do usuário A mesmo)

**Passos:**
1. Enviar `EvaluateFourEyesPrinciple.Query(ReleaseId)`
2. Verificar que princípio não é satisfeito (criador não pode aprovar)

**Resultado Esperado:**
- `result.Value.IsSatisfied == false`
- `result.Value.Reason` menciona necessidade de aprovador independente

**Critério de Aceite:** `result.Value.IsSatisfied == false`

---

### TC-CGV-090 — Princípio dos quatro olhos — satisfeito com aprovador diferente

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | EvaluateFourEyesPrinciple |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `EvaluateFourEyesPrinciple.Handler` |

**Pré-condições:**
- Release criada pelo usuário A
- Aprovação registrada pelo usuário B (diferente do criador)

**Passos:**
1. Enviar `EvaluateFourEyesPrinciple.Query(ReleaseId)`
2. Verificar que princípio é satisfeito

**Resultado Esperado:**
- `result.Value.IsSatisfied == true`

**Critério de Aceite:** `result.Value.IsSatisfied == true`

---

*Total de cenários de teste neste documento: **90***

---

## Apêndice — Mapeamento de Handlers para Cenários

| Cenários | Subsistema |
|----------|------------|
| TC-CGV-001 a 008 | Release Ingest — ingestão e consultas |
| TC-CGV-009 a 019 | Change Intelligence — classificação e work items |
| TC-CGV-020 a 028 | Freeze Windows |
| TC-CGV-029 a 037 | Fluxos de Aprovação |
| TC-CGV-038 a 050 | Promotion Gates |
| TC-CGV-051 a 057 | Evidence Packs |
| TC-CGV-058 a 062 | Deployment e Pós-Release |
| TC-CGV-063 a 065 | Workflow Templates |
| TC-CGV-066 a 075 | Relatórios e Análises |
| TC-CGV-076 a 079 | Segurança / Isolamento Multi-Tenant |
| TC-CGV-080 a 090 | Fluxos End-to-End e casos de contorno |
