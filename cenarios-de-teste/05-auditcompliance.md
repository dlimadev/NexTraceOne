# Cenários de Teste — Módulo AuditCompliance

**Módulo:** AuditCompliance  
**Versão do documento:** 1.0  
**Data:** 2026-05-18  
**Responsável:** Equipe de QA  
**Total de casos:** 40

---

## Índice

1. [Ciclo de Vida de Políticas de Compliance](#1-ciclo-de-vida-de-políticas-de-compliance)
2. [Campanhas de Auditoria](#2-campanhas-de-auditoria)
3. [Avaliação Contínua de Compliance](#3-avaliação-contínua-de-compliance)
4. [Retenção de Dados](#4-retenção-de-dados)
5. [Exportação de Relatórios e Evidências](#5-exportação-de-relatórios-e-evidências)
6. [Frameworks de Compliance](#6-frameworks-de-compliance)
7. [Trilha de Auditoria e Integridade](#7-trilha-de-auditoria-e-integridade)
8. [Multi-Tenant e Segurança](#8-multi-tenant-e-segurança)

---

## 1. Ciclo de Vida de Políticas de Compliance

### TC-AUD-001 — Criar política de compliance com severidade Critical

| Campo | Valor |
|-------|-------|
| **Módulo** | AuditCompliance |
| **Feature** | CreateCompliancePolicy |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `CreateCompliancePolicy.Handler` |

**Pré-condições:**
- Tenant autenticado com permissão de gestão de compliance
- `ICompliancePolicyRepository` disponível (substituto configurado)

**Passos:**
1. Enviar `CreateCompliancePolicy.Command` com `Name="data-encryption-at-rest"`, `DisplayName="Criptografia de Dados em Repouso"`, `Category="Security"`, `Severity=ComplianceSeverity.Critical`, `EvaluationCriteria="ALL data stores must use AES-256 encryption"`, `TenantId=tenantId`
2. Handler valida via `CreateCompliancePolicy.Validator`
3. Cria entidade `CompliancePolicy` via `CompliancePolicy.Create()`
4. Persiste via `ICompliancePolicyRepository.Add()`
5. Confirma `IAuditComplianceUnitOfWork.CommitAsync()`

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.PolicyId` preenchido (Guid não vazio)
- `result.Value.IsActive == false` (políticas iniciam inativas)
- `result.Value.Name == "data-encryption-at-rest"`

**Critério de Aceite:** HTTP 201 Created com body `{ "policyId": "...", "name": "...", "isActive": false }`

---

### TC-AUD-002 — Rejeitar política com nome vazio

| Campo | Valor |
|-------|-------|
| **Módulo** | AuditCompliance |
| **Feature** | CreateCompliancePolicy |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `CreateCompliancePolicy.Validator` |

**Pré-condições:**
- Tenant autenticado

**Passos:**
1. Enviar `CreateCompliancePolicy.Command` com `Name=""`, `DisplayName="Política Sem Nome"`, `Category="Security"`, `Severity=ComplianceSeverity.High`, `TenantId=tenantId`
2. `ValidationBehavior` dispara `CreateCompliancePolicy.Validator`
3. Regra `RuleFor(x => x.Name).NotEmpty()` falha

**Resultado Esperado:**
- `result.IsSuccess == false`
- `result.Error.Type == ErrorType.Validation`
- Mensagem indica que `Name` não pode ser vazio

**Critério de Aceite:** HTTP 422 Unprocessable Entity

---

### TC-AUD-003 — Ativar política de compliance inativa

| Campo | Valor |
|-------|-------|
| **Módulo** | AuditCompliance |
| **Feature** | ActivateCompliancePolicy |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `ActivateCompliancePolicy.Handler` |

**Pré-condições:**
- Política em estado inativo criada (TC-AUD-001)

**Passos:**
1. Enviar `ActivateCompliancePolicy.Command` com `PolicyId=policyId`
2. Handler busca política via `ICompliancePolicyRepository.GetByIdAsync(policyId)`
3. Chama `policy.Activate()`
4. Persiste via `IAuditComplianceUnitOfWork.CommitAsync()`

**Resultado Esperado:**
- `result.IsSuccess == true`
- Política recuperada com `IsActive == true`
- `ActivatedAt` preenchido com timestamp atual

**Critério de Aceite:** HTTP 200 OK; política aparece em listagens de políticas ativas

---

### TC-AUD-004 — Desativar política de compliance ativa

| Campo | Valor |
|-------|-------|
| **Módulo** | AuditCompliance |
| **Feature** | DeactivateCompliancePolicy |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `DeactivateCompliancePolicy.Handler` |

**Pré-condições:**
- Política em estado ativo (TC-AUD-003)

**Passos:**
1. Enviar `DeactivateCompliancePolicy.Command` com `PolicyId=policyId`, `Reason="Substituída por nova versão"`
2. Handler busca política, chama `policy.Deactivate(reason)`
3. Persiste mudança

**Resultado Esperado:**
- `result.IsSuccess == true`
- Política com `IsActive == false`
- `DeactivatedAt` e `DeactivationReason` registrados

**Critério de Aceite:** HTTP 200 OK; política não avaliada em checks futuros

---

### TC-AUD-005 — Atualizar política de compliance existente

| Campo | Valor |
|-------|-------|
| **Módulo** | AuditCompliance |
| **Feature** | UpdateCompliancePolicy |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `UpdateCompliancePolicy.Handler` |

**Pré-condições:**
- Política existente com `PolicyId`

**Passos:**
1. Enviar `UpdateCompliancePolicy.Command` com `PolicyId`, `DisplayName="Criptografia AES-256 — Revisada"`, `EvaluationCriteria="ALL data stores must use AES-256-GCM encryption with key rotation every 90 days"`
2. Handler atualiza campos da entidade
3. Persiste via `IUnitOfWork.CommitAsync()`

**Resultado Esperado:**
- `result.IsSuccess == true`
- `DisplayName` e `EvaluationCriteria` atualizados
- `UpdatedAt` reflete o timestamp da atualização

**Critério de Aceite:** HTTP 200 OK com campos atualizados

---

### TC-AUD-006 — Listar políticas de compliance com filtro por categoria

| Campo | Valor |
|-------|-------|
| **Módulo** | AuditCompliance |
| **Feature** | ListCompliancePolicies |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `ListCompliancePolicies.Handler` |

**Pré-condições:**
- 10 políticas: 4 `Security`, 3 `Privacy`, 3 `Operational`

**Passos:**
1. Enviar `ListCompliancePolicies.Query` com `Category="Security"`, `IsActive=true`
2. Handler filtra via repositório

**Resultado Esperado:**
- `result.Value.Policies.Count == 4`
- Todas as políticas retornadas com `Category="Security"`

**Critério de Aceite:** HTTP 200 OK com lista filtrada

---

### TC-AUD-007 — Buscar política inexistente retorna NotFound

| Campo | Valor |
|-------|-------|
| **Módulo** | AuditCompliance |
| **Feature** | GetCompliancePolicy |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `GetCompliancePolicy.Handler` |

**Pré-condições:**
- Nenhuma política com ID especificado

**Passos:**
1. Enviar `GetCompliancePolicy.Query` com `PolicyId=Guid.NewGuid()`
2. Repositório retorna `null`
3. Handler retorna `Error.NotFound`

**Resultado Esperado:**
- `result.IsSuccess == false`
- `result.Error.Type == ErrorType.NotFound`

**Critério de Aceite:** HTTP 404 Not Found

---

## 2. Campanhas de Auditoria

### TC-AUD-008 — Criar campanha de auditoria SOC2

| Campo | Valor |
|-------|-------|
| **Módulo** | AuditCompliance |
| **Feature** | CreateAuditCampaign |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `CreateAuditCampaign.Handler` |

**Pré-condições:**
- Tenant autenticado com permissão de auditoria
- Pelo menos 3 políticas de compliance ativas

**Passos:**
1. Enviar `CreateAuditCampaign.Command` com `Name="SOC2 Q2/2026"`, `Framework="SOC2"`, `StartDate=2026-04-01`, `EndDate=2026-06-30`, `Scope="All systems"`, `AssignedTo="auditor@company.com"`, `PolicyIds=[id1, id2, id3]`
2. Handler cria campanha vinculando políticas
3. Persiste via `IUnitOfWork.CommitAsync()`

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.CampaignId` preenchido
- `Status="Draft"`
- 3 políticas vinculadas

**Critério de Aceite:** HTTP 201 Created com `campaignId`

---

### TC-AUD-009 — Buscar campanha de auditoria por ID

| Campo | Valor |
|-------|-------|
| **Módulo** | AuditCompliance |
| **Feature** | GetAuditCampaign |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `GetAuditCampaign.Handler` |

**Pré-condições:**
- Campanha criada (TC-AUD-008)

**Passos:**
1. Enviar `GetAuditCampaign.Query` com `CampaignId`
2. Handler recupera campanha com políticas vinculadas

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.Name == "SOC2 Q2/2026"`
- `result.Value.Framework == "SOC2"`
- `result.Value.Policies.Count == 3`

**Critério de Aceite:** HTTP 200 OK com detalhes completos da campanha

---

### TC-AUD-010 — Transicionar campanha de auditoria para InProgress

| Campo | Valor |
|-------|-------|
| **Módulo** | AuditCompliance |
| **Feature** | TransitionAuditCampaign |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `TransitionAuditCampaign.Handler` |

**Pré-condições:**
- Campanha em status `Draft` (TC-AUD-008)

**Passos:**
1. Enviar `TransitionAuditCampaign.Command` com `CampaignId`, `TargetStatus="InProgress"`
2. Handler valida transição de estado (`Draft → InProgress`)
3. Atualiza `Status` e `StartedAt`

**Resultado Esperado:**
- `result.IsSuccess == true`
- Campanha com `Status="InProgress"` e `StartedAt` preenchido

**Critério de Aceite:** HTTP 200 OK; campanha ativa para coleta de evidências

---

### TC-AUD-011 — Rejeitar transição inválida de campanha (Completed → Draft)

| Campo | Valor |
|-------|-------|
| **Módulo** | AuditCompliance |
| **Feature** | TransitionAuditCampaign |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `TransitionAuditCampaign.Handler` |

**Pré-condições:**
- Campanha em status `Completed`

**Passos:**
1. Enviar `TransitionAuditCampaign.Command` com `CampaignId`, `TargetStatus="Draft"`
2. Handler valida que transição `Completed → Draft` é inválida

**Resultado Esperado:**
- `result.IsSuccess == false`
- `result.Error.Type == ErrorType.Business`
- Mensagem indica transição inválida

**Critério de Aceite:** HTTP 422 Unprocessable Entity

---

### TC-AUD-012 — Listar campanhas de auditoria com paginação

| Campo | Valor |
|-------|-------|
| **Módulo** | AuditCompliance |
| **Feature** | ListAuditCampaigns |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `ListAuditCampaigns.Handler` |

**Pré-condições:**
- 15 campanhas criadas para o tenant

**Passos:**
1. Enviar `ListAuditCampaigns.Query` com `Page=1`, `PageSize=10`
2. Handler retorna primeiros 10 resultados

**Resultado Esperado:**
- `result.Value.Campaigns.Count == 10`
- `result.Value.TotalCount == 15`
- `result.Value.HasNextPage == true`

**Critério de Aceite:** HTTP 200 OK com paginação correta

---

## 3. Avaliação Contínua de Compliance

### TC-AUD-013 — Avaliar compliance contínua de serviço contra todas as políticas ativas

| Campo | Valor |
|-------|-------|
| **Módulo** | AuditCompliance |
| **Feature** | EvaluateContinuousCompliance |
| **Tipo** | Integração |
| **Prioridade** | Crítica |
| **Handler** | `EvaluateContinuousCompliance.Handler` |

**Pré-condições:**
- 5 políticas ativas para o tenant: 3 de Security, 2 de Privacy
- `IComplianceResultRepository` disponível

**Passos:**
1. Enviar `EvaluateContinuousCompliance.Command` com `ResourceType="Service"`, `ResourceId="payment-service"`, `Category=null`, `TenantId=tenantId`, `TriggeredBy="deploy-event"`
2. Handler obtém todas as políticas ativas (`ListAsync(isActive: true)`)
3. Filtra por tenant e avalia cada política
4. Persiste resultados como `ComplianceResult` com `EvaluatedBy="system:continuous"`
5. Retorna sumário

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.PoliciesEvaluated == 5`
- `result.Value.Compliant + result.Value.NonCompliant == 5`
- `result.Value.ResourceType == "Service"`

**Critério de Aceite:** HTTP 200 OK com sumário; resultados persistidos em banco

---

### TC-AUD-014 — Avaliar compliance contínua filtrando por categoria Security

| Campo | Valor |
|-------|-------|
| **Módulo** | AuditCompliance |
| **Feature** | EvaluateContinuousCompliance |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `EvaluateContinuousCompliance.Handler` |

**Pré-condições:**
- 5 políticas ativas: 3 `Security`, 2 `Privacy`

**Passos:**
1. Enviar `EvaluateContinuousCompliance.Command` com `Category="Security"`, demais campos válidos
2. Handler filtra `ListAsync(isActive: true, category: "Security")`
3. Avalia apenas as 3 políticas de Security

**Resultado Esperado:**
- `result.Value.PoliciesEvaluated == 3`
- Apenas políticas de categoria `Security` avaliadas

**Critério de Aceite:** HTTP 200 OK; políticas de Privacy não incluídas no resultado

---

### TC-AUD-015 — Avaliação retorna zero quando não há políticas para o tenant

| Campo | Valor |
|-------|-------|
| **Módulo** | AuditCompliance |
| **Feature** | EvaluateContinuousCompliance |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `EvaluateContinuousCompliance.Handler` |

**Pré-condições:**
- Tenant sem políticas de compliance configuradas

**Passos:**
1. Enviar `EvaluateContinuousCompliance.Command` com `TenantId=tenantSemPoliticas`
2. Handler recebe lista vazia de políticas
3. Retorna resposta com zeros

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.PoliciesEvaluated == 0`
- `result.Value.Compliant == 0`
- `result.Value.NonCompliant == 0`

**Critério de Aceite:** HTTP 200 OK com sumário vazio; sem erros

---

### TC-AUD-016 — Registrar resultado de compliance e consultar lista

| Campo | Valor |
|-------|-------|
| **Módulo** | AuditCompliance |
| **Feature** | RecordComplianceResult / ListComplianceResults |
| **Tipo** | Integração |
| **Prioridade** | Alta |
| **Handler** | `RecordComplianceResult.Handler`, `ListComplianceResults.Handler` |

**Pré-condições:**
- Política de compliance ativa
- `IComplianceResultRepository` disponível

**Passos:**
1. Enviar `RecordComplianceResult.Command` com `PolicyId`, `ResourceType="Deploy"`, `ResourceId="deploy-42"`, `IsCompliant=false`, `EvaluatedBy="ci-pipeline"`, `Notes="Missing encryption key rotation"`
2. Handler persiste resultado
3. Enviar `ListComplianceResults.Query` com `PolicyId`

**Resultado Esperado:**
- Resultado registrado com `IsCompliant=false`
- `ListComplianceResults` retorna o resultado criado
- `EvaluatedBy == "ci-pipeline"` registrado para rastreabilidade

**Critério de Aceite:** HTTP 200 OK; resultado auditável disponível

---

### TC-AUD-017 — Gate de remediação bloqueia deploy com compliance violada

| Campo | Valor |
|-------|-------|
| **Módulo** | AuditCompliance |
| **Feature** | EvaluateContinuousCompliance |
| **Tipo** | Integração |
| **Prioridade** | Crítica |
| **Handler** | `EvaluateContinuousCompliance.Handler` |

**Pré-condições:**
- Política `data-encryption-at-rest` ativa com `Severity=Critical`
- Serviço não possui criptografia configurada

**Passos:**
1. Pipeline de CI/CD dispara `EvaluateContinuousCompliance.Command` com `ResourceId="payment-service"`, `TriggeredBy="pre-deploy-gate"`
2. Handler avalia serviço contra política de criptografia
3. Avaliação retorna `IsCompliant=false` para política Critical

**Resultado Esperado:**
- `result.Value.NonCompliant >= 1`
- Ao menos uma violação com `Severity="Critical"` registrada
- Pipeline de CI/CD usa resultado para bloquear deploy

**Critério de Aceite:** HTTP 200 OK; gate identifica violação; deploy não avança

---

## 4. Retenção de Dados

### TC-AUD-018 — Configurar política de retenção por tipo de dado

| Campo | Valor |
|-------|-------|
| **Módulo** | AuditCompliance |
| **Feature** | ConfigureRetention |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `ConfigureRetention.Handler` |

**Pré-condições:**
- Tenant autenticado com permissão de gestão de retenção

**Passos:**
1. Enviar `ConfigureRetention.Command` com `DataType="AuditLog"`, `RetentionDays=2555` (7 anos), `ArchiveAfterDays=365`, `DeleteAfterDays=2555`, `Justification="SOC2 requirement — 7 years"`
2. Handler valida e persiste política de retenção

**Resultado Esperado:**
- `result.IsSuccess == true`
- Política de retenção criada com `RetentionDays=2555`
- `DataType="AuditLog"` corretamente mapeado

**Critério de Aceite:** HTTP 201 Created; política aplicada a novos dados de auditoria

---

### TC-AUD-019 — Aplicar retenção e arquivar registros elegíveis

| Campo | Valor |
|-------|-------|
| **Módulo** | AuditCompliance |
| **Feature** | ApplyRetention |
| **Tipo** | Integração |
| **Prioridade** | Alta |
| **Handler** | `ApplyRetention.Handler` |

**Pré-condições:**
- Política de retenção configurada (TC-AUD-018)
- 500 registros de auditoria com mais de 1 ano de idade

**Passos:**
1. Enviar `ApplyRetention.Command` com `DataType="AuditLog"`, `DryRun=false`
2. Handler consulta registros elegíveis para arquivamento
3. Arquiva registros com mais de 365 dias
4. Registra sumário da operação

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.RecordsArchived >= 500`
- `result.Value.RecordsDeleted == 0` (apenas arquivamento, não deleção)

**Critério de Aceite:** HTTP 200 OK; registros arquivados sem perda de dados

---

### TC-AUD-020 — Dry-run de retenção não modifica dados

| Campo | Valor |
|-------|-------|
| **Módulo** | AuditCompliance |
| **Feature** | ApplyRetention |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `ApplyRetention.Handler` |

**Pré-condições:**
- Política de retenção configurada
- Registros elegíveis para arquivamento

**Passos:**
1. Enviar `ApplyRetention.Command` com `DryRun=true`
2. Handler simula operação sem efetuar modificações
3. Retorna estimativa de registros afetados

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.DryRun == true`
- `result.Value.EstimatedRecordsToArchive > 0`
- Nenhum registro modificado no banco

**Critério de Aceite:** HTTP 200 OK; contagem estimada sem efeitos colaterais

---

### TC-AUD-021 — Obter políticas de retenção configuradas

| Campo | Valor |
|-------|-------|
| **Módulo** | AuditCompliance |
| **Feature** | GetRetentionPolicies |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `GetRetentionPolicies.Handler` |

**Pré-condições:**
- 3 políticas de retenção configuradas: AuditLog, ComplianceResult, AccessLog

**Passos:**
1. Enviar `GetRetentionPolicies.Query`
2. Handler retorna todas as políticas do tenant

**Resultado Esperado:**
- `result.Value.Policies.Count == 3`
- Cada política com `DataType`, `RetentionDays`, `ArchiveAfterDays`

**Critério de Aceite:** HTTP 200 OK com lista de políticas

---

## 5. Exportação de Relatórios e Evidências

### TC-AUD-022 — Exportar relatório de auditoria em formato PDF

| Campo | Valor |
|-------|-------|
| **Módulo** | AuditCompliance |
| **Feature** | ExportAuditReport |
| **Tipo** | Integração |
| **Prioridade** | Alta |
| **Handler** | `ExportAuditReport.Handler` |

**Pré-condições:**
- Campanha de auditoria com resultados registrados (TC-AUD-010)
- `IReportGeneratorService` configurado

**Passos:**
1. Enviar `ExportAuditReport.Command` com `CampaignId`, `Format="PDF"`, `IncludeEvidence=true`
2. Handler coleta dados da campanha, políticas e resultados
3. Gera relatório em PDF via `IReportGeneratorService`
4. Retorna URL de download ou stream

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.DownloadUrl` ou `result.Value.ReportBytes` preenchido
- Relatório contém sumário de compliance, políticas avaliadas e evidências

**Critério de Aceite:** HTTP 200 OK com link de download válido por 24h

---

### TC-AUD-023 — Exportar evidências de compliance em ZIP

| Campo | Valor |
|-------|-------|
| **Módulo** | AuditCompliance |
| **Feature** | ExportComplianceEvidences |
| **Tipo** | Integração |
| **Prioridade** | Alta |
| **Handler** | `ExportComplianceEvidences.Handler` |

**Pré-condições:**
- Campanha com 50+ resultados de compliance (conformes e não-conformes)

**Passos:**
1. Enviar `ExportComplianceEvidences.Command` com `CampaignId`, `IncludeNonCompliant=true`, `IncludeCompliant=false`
2. Handler filtra resultados não-conformes
3. Empacota evidências em arquivo ZIP

**Resultado Esperado:**
- `result.IsSuccess == true`
- ZIP contém apenas evidências de não-conformidade
- Cada arquivo de evidência nomeado por `PolicyName_ResourceId`

**Critério de Aceite:** HTTP 200 OK com arquivo ZIP; evidências compliant excluídas

---

### TC-AUD-024 — Gerar relatório de auditoria pronto para certificadores externos

| Campo | Valor |
|-------|-------|
| **Módulo** | AuditCompliance |
| **Feature** | GenerateAuditReadyReport |
| **Tipo** | Integração |
| **Prioridade** | Alta |
| **Handler** | `GenerateAuditReadyReport.Handler` |

**Pré-condições:**
- Campanha SOC2 com todos os controles avaliados
- Taxa de conformidade >= 95%

**Passos:**
1. Enviar `GenerateAuditReadyReport.Command` com `CampaignId`, `AuditFirm="Deloitte"`, `CertificationPeriod="2025-01-01 a 2025-12-31"`
2. Handler gera relatório formatado para auditores externos
3. Inclui sumário executivo, controles, evidências e exceções documentadas

**Resultado Esperado:**
- `result.IsSuccess == true`
- Relatório com estrutura de controles SOC2
- Exceções documentadas com justificativas
- Assinatura digital do tenant incluída

**Critério de Aceite:** HTTP 200 OK; relatório pronto para submissão a auditor externo

---

### TC-AUD-025 — Obter relatório de compliance por framework

| Campo | Valor |
|-------|-------|
| **Módulo** | AuditCompliance |
| **Feature** | GetComplianceReport |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `GetComplianceReport.Handler` |

**Pré-condições:**
- Resultados de compliance registrados para `ISO27001`

**Passos:**
1. Enviar `GetComplianceReport.Query` com `Framework="ISO27001"`, `Period="2025"`, `TenantId`
2. Handler agrega resultados por controle ISO 27001

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.ComplianceScore` entre 0 e 100
- `result.Value.ControlResults` mapeados por número de controle ISO
- Controles não avaliados marcados como `NotAssessed`

**Critério de Aceite:** HTTP 200 OK com relatório estruturado por framework

---

## 6. Frameworks de Compliance

### TC-AUD-026 — Avaliação SOC2: verificar controles de disponibilidade e segurança

| Campo | Valor |
|-------|-------|
| **Módulo** | AuditCompliance |
| **Feature** | EvaluateContinuousCompliance |
| **Tipo** | Integração |
| **Prioridade** | Crítica |
| **Handler** | `EvaluateContinuousCompliance.Handler` |

**Pré-condições:**
- Políticas SOC2 ativas: CC6.1 (Acesso Lógico), CC9.2 (Disponibilidade), CC7.2 (Monitoramento)
- Serviço de produção em avaliação

**Passos:**
1. Enviar `EvaluateContinuousCompliance.Command` com `ResourceType="Service"`, `ResourceId="api-gateway"`, `Category="SOC2"`, `TenantId`
2. Handler avalia serviço contra controles SOC2 ativos
3. Verifica: MFA habilitado, uptime >= 99.9%, alertas configurados

**Resultado Esperado:**
- `result.Value.PoliciesEvaluated == 3`
- Detalhes por controle SOC2 nos resultados persistidos
- Violações em CC7.2 (se alertas não configurados) registradas

**Critério de Aceite:** HTTP 200 OK; resultados mapeados para controles SOC2 Trust Services Criteria

---

### TC-AUD-027 — Verificação ISO 27001: controle A.12.1 (Procedimentos Operacionais)

| Campo | Valor |
|-------|-------|
| **Módulo** | AuditCompliance |
| **Feature** | EvaluateContinuousCompliance |
| **Tipo** | Integração |
| **Prioridade** | Alta |
| **Handler** | `EvaluateContinuousCompliance.Handler` |

**Pré-condições:**
- Política ISO 27001 A.12.1 ativa: "Todos os procedimentos operacionais devem ser documentados"
- Serviço sem documentação de runbook vinculada

**Passos:**
1. Disparar avaliação para `ResourceId="database-cluster"`, `Category="ISO27001"`
2. Handler avalia contra critérios da política
3. Ausência de documentação resulta em não-conformidade

**Resultado Esperado:**
- `result.Value.NonCompliant >= 1`
- Resultado registrado com `Notes="Runbook não encontrado para database-cluster"`

**Critério de Aceite:** HTTP 200 OK; não-conformidade A.12.1 documentada

---

### TC-AUD-028 — Verificação HIPAA: proteção de dados de saúde (PHI)

| Campo | Valor |
|-------|-------|
| **Módulo** | AuditCompliance |
| **Feature** | EvaluateContinuousCompliance |
| **Tipo** | Integração |
| **Prioridade** | Crítica |
| **Handler** | `EvaluateContinuousCompliance.Handler` |

**Pré-condições:**
- Políticas HIPAA ativas: criptografia PHI em trânsito e em repouso, controle de acesso mínimo, log de acesso a PHI
- Serviço `patient-records-api` em avaliação

**Passos:**
1. Enviar `EvaluateContinuousCompliance.Command` com `Category="HIPAA"`, `ResourceId="patient-records-api"`
2. Handler avalia todos os controles HIPAA aplicáveis

**Resultado Esperado:**
- `result.Value.PoliciesEvaluated >= 3` (criptografia, acesso, log)
- Qualquer violação registrada com `Severity=Critical`
- `ComplianceScore` calculado

**Critério de Aceite:** HTTP 200 OK; violações HIPAA Critical priorizadas em alertas

---

### TC-AUD-029 — Verificação GDPR: consentimento e direito ao esquecimento

| Campo | Valor |
|-------|-------|
| **Módulo** | AuditCompliance |
| **Feature** | EvaluateContinuousCompliance |
| **Tipo** | Integração |
| **Prioridade** | Crítica |
| **Handler** | `EvaluateContinuousCompliance.Handler` |

**Pré-condições:**
- Políticas GDPR ativas: consentimento explícito para coleta, mecanismo de exclusão implementado, DPO designado
- Serviço `user-registration-service` em avaliação

**Passos:**
1. Enviar `EvaluateContinuousCompliance.Command` com `Category="GDPR"`, `ResourceId="user-registration-service"`
2. Handler avalia políticas de consentimento e exclusão

**Resultado Esperado:**
- Conformidade com Art. 7 (consentimento) verificada
- Conformidade com Art. 17 (direito ao esquecimento) verificada
- Violações identificadas com referência ao artigo GDPR aplicável

**Critério de Aceite:** HTTP 200 OK; relatório mapeado para artigos GDPR

---

### TC-AUD-030 — Verificação PCI-DSS: proteção de dados de cartão

| Campo | Valor |
|-------|-------|
| **Módulo** | AuditCompliance |
| **Feature** | EvaluateContinuousCompliance |
| **Tipo** | Integração |
| **Prioridade** | Crítica |
| **Handler** | `EvaluateContinuousCompliance.Handler` |

**Pré-condições:**
- Políticas PCI-DSS ativas: tokenização de PAN, TLS 1.2+, monitoramento de redes
- Serviço de pagamento em avaliação

**Passos:**
1. Enviar `EvaluateContinuousCompliance.Command` com `Category="PCI-DSS"`, `ResourceId="payment-processor"`
2. Handler avalia controles PCI-DSS Requirements 3, 4, e 10

**Resultado Esperado:**
- Requisito 3 (proteção de CHD) avaliado
- Requisito 4 (criptografia em trânsito) avaliado
- Requisito 10 (logging) avaliado
- Score PCI-DSS calculado

**Critério de Aceite:** HTTP 200 OK; mapeamento para requisitos PCI-DSS SAQ D

---

### TC-AUD-031 — Verificação NIS2: segurança de redes e sistemas de informação

| Campo | Valor |
|-------|-------|
| **Módulo** | AuditCompliance |
| **Feature** | EvaluateContinuousCompliance |
| **Tipo** | Integração |
| **Prioridade** | Alta |
| **Handler** | `EvaluateContinuousCompliance.Handler` |

**Pré-condições:**
- Políticas NIS2 ativas: plano de resposta a incidentes, avaliação de risco, continuidade de negócios
- Organização classificada como entidade essencial

**Passos:**
1. Enviar `EvaluateContinuousCompliance.Command` com `Category="NIS2"`, `ResourceId="critical-infrastructure"`
2. Handler avalia controles NIS2 Art. 21 (medidas de segurança)

**Resultado Esperado:**
- Plano de resposta a incidentes verificado
- Avaliação de risco documentada verificada
- Violações referenciadas aos artigos NIS2 aplicáveis

**Critério de Aceite:** HTTP 200 OK; não-conformidades priorizadas por prazo de notificação NIS2 (24h/72h)

---

### TC-AUD-032 — Sumário de framework de compliance agrega múltiplos frameworks

| Campo | Valor |
|-------|-------|
| **Módulo** | AuditCompliance |
| **Feature** | GetComplianceFrameworkSummary |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `GetComplianceFrameworkSummary.Handler` |

**Pré-condições:**
- Resultados registrados para SOC2, ISO27001, GDPR nos últimos 30 dias

**Passos:**
1. Enviar `GetComplianceFrameworkSummary.Query` com `TenantId`, `PeriodDays=30`
2. Handler agrega scores por framework

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.Frameworks` lista SOC2, ISO27001, GDPR com scores individuais
- Score geral agregado calculado

**Critério de Aceite:** HTTP 200 OK com dashboard de frameworks

---

## 7. Trilha de Auditoria e Integridade

### TC-AUD-033 — Registrar evento de auditoria com hash de integridade

| Campo | Valor |
|-------|-------|
| **Módulo** | AuditCompliance |
| **Feature** | RecordAuditEvent |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Handler** | `RecordAuditEvent.Handler` |

**Pré-condições:**
- Tenant autenticado

**Passos:**
1. Enviar `RecordAuditEvent.Command` com `EventType="UserLogin"`, `ActorId="user-123"`, `ActorType="User"`, `ResourceType="Session"`, `ResourceId="session-abc"`, `Details={"ip": "192.168.1.1", "userAgent": "..."}`
2. Handler gera hash SHA-256 do evento para integridade
3. Persiste evento com hash e timestamp imutável

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.EventId` preenchido
- `result.Value.IntegrityHash` preenchido (SHA-256)

**Critério de Aceite:** HTTP 200 OK; evento imutável na trilha de auditoria

---

### TC-AUD-034 — Verificar integridade da cadeia de eventos de auditoria

| Campo | Valor |
|-------|-------|
| **Módulo** | AuditCompliance |
| **Feature** | VerifyChainIntegrity |
| **Tipo** | Integração |
| **Prioridade** | Crítica |
| **Handler** | `VerifyChainIntegrity.Handler` |

**Pré-condições:**
- 1000 eventos de auditoria registrados em cadeia (cada hash referencia o anterior)
- Nenhum evento adulterado

**Passos:**
1. Enviar `VerifyChainIntegrity.Query` com `StartEventId`, `EndEventId`, `TenantId`
2. Handler recalcula hashes de cada evento e verifica contra hash armazenado
3. Verifica que cada evento referencia corretamente o anterior

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.IsIntegrityValid == true`
- `result.Value.EventsVerified == 1000`
- `result.Value.TamperedEvents.Count == 0`

**Critério de Aceite:** HTTP 200 OK; cadeia íntegra confirmada

---

### TC-AUD-035 — Detectar adulteração em evento de auditoria

| Campo | Valor |
|-------|-------|
| **Módulo** | AuditCompliance |
| **Feature** | VerifyChainIntegrity |
| **Tipo** | Segurança |
| **Prioridade** | Crítica |
| **Handler** | `VerifyChainIntegrity.Handler` |

**Pré-condições:**
- Evento de auditoria com hash adulterado diretamente no banco

**Passos:**
1. Modificar diretamente o campo `IntegrityHash` do evento no banco (simulação de adulteração)
2. Enviar `VerifyChainIntegrity.Query` para o intervalo afetado
3. Handler detecta hash incorreto ao recalcular

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.IsIntegrityValid == false`
- `result.Value.TamperedEvents` contém o `EventId` adulterado

**Critério de Aceite:** HTTP 200 OK; adulteração detectada e reportada

---

### TC-AUD-036 — Pesquisar log de auditoria com filtros múltiplos

| Campo | Valor |
|-------|-------|
| **Módulo** | AuditCompliance |
| **Feature** | SearchAuditLog |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `SearchAuditLog.Handler` |

**Pré-condições:**
- 500 eventos de auditoria de tipos variados

**Passos:**
1. Enviar `SearchAuditLog.Query` com `EventType="UserLogin"`, `ActorId="user-123"`, `DateFrom=2026-01-01`, `DateTo=2026-01-31`, `PageSize=20`
2. Handler aplica todos os filtros

**Resultado Esperado:**
- `result.Value.Events` contém apenas logins do `user-123` em janeiro
- Paginação correta
- Eventos ordenados por `OccurredAt DESC`

**Critério de Aceite:** HTTP 200 OK com resultados filtrados e ordenados

---

### TC-AUD-037 — Obter dashboard de compliance com score geral

| Campo | Valor |
|-------|-------|
| **Módulo** | AuditCompliance |
| **Feature** | GetComplianceDashboard |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `GetComplianceDashboard.Handler` |

**Pré-condições:**
- Resultados de compliance dos últimos 90 dias
- Múltiplas políticas e frameworks ativos

**Passos:**
1. Enviar `GetComplianceDashboard.Query` com `TenantId`, `PeriodDays=90`
2. Handler agrega: score geral, tendência, top 5 violações, frameworks

**Resultado Esperado:**
- `result.Value.OverallScore` entre 0 e 100
- `result.Value.Trend` indica direção (improving/degrading)
- `result.Value.TopViolations` lista os 5 controles mais violados
- `result.Value.ByFramework` breakdown por framework

**Critério de Aceite:** HTTP 200 OK com dashboard executivo completo

---

## 8. Multi-Tenant e Segurança

### TC-AUD-038 — Tenant A não acessa políticas do Tenant B

| Campo | Valor |
|-------|-------|
| **Módulo** | AuditCompliance |
| **Feature** | GetCompliancePolicy |
| **Tipo** | Segurança |
| **Prioridade** | Crítica |
| **Handler** | `GetCompliancePolicy.Handler` + `TenantRlsInterceptor` |

**Pré-condições:**
- Tenant A com política `policy-a` criada
- Tenant B autenticado separadamente

**Passos:**
1. Autenticar como Tenant B (JWT com `TenantId=tenant-b`)
2. Enviar `GetCompliancePolicy.Query` com `PolicyId=policy-a` (pertence ao Tenant A)
3. `TenantRlsInterceptor` aplica `SET app.current_tenant_id = tenant-b` no PostgreSQL
4. RLS filtra — política do Tenant A não retornada

**Resultado Esperado:**
- `result.IsSuccess == false`
- `result.Error.Type == ErrorType.NotFound`
- Nenhum dado do Tenant A exposto ao Tenant B

**Critério de Aceite:** HTTP 404; isolamento RLS confirmado para módulo AuditCompliance

---

### TC-AUD-039 — Exportação de relatório restrita ao próprio tenant

| Campo | Valor |
|-------|-------|
| **Módulo** | AuditCompliance |
| **Feature** | ExportAuditReport |
| **Tipo** | Segurança |
| **Prioridade** | Crítica |
| **Handler** | `ExportAuditReport.Handler` + `TenantIsolationBehavior` |

**Pré-condições:**
- Campanha do Tenant A com `CampaignId=campaign-a`
- Tenant B autenticado

**Passos:**
1. Tenant B tenta exportar relatório com `CampaignId=campaign-a`
2. `TenantIsolationBehavior` verifica tenant no JWT
3. Busca da campanha retorna vazia (RLS filtra Tenant A)

**Resultado Esperado:**
- `result.IsSuccess == false`
- HTTP 404 Not Found ou HTTP 403 Forbidden
- Dados do Tenant A não incluídos no relatório

**Critério de Aceite:** Dados de auditoria cross-tenant inacessíveis

---

### TC-AUD-040 — Avaliação de compliance invocada sem tenant retorna erro

| Campo | Valor |
|-------|-------|
| **Módulo** | AuditCompliance |
| **Feature** | EvaluateContinuousCompliance |
| **Tipo** | Segurança |
| **Prioridade** | Alta |
| **Handler** | `TenantIsolationBehavior` |

**Pré-condições:**
- Requisição sem JWT válido (sem tenant resolvido)

**Passos:**
1. Enviar `EvaluateContinuousCompliance.Command` sem header `Authorization`
2. `TenantResolutionMiddleware` não consegue resolver tenant
3. `TenantIsolationBehavior` rejeita (Command não implementa `IPublicRequest`)

**Resultado Esperado:**
- `result.IsSuccess == false`
- `result.Error.Type == ErrorType.Unauthorized`

**Critério de Aceite:** HTTP 401 Unauthorized; handler não executado

---

*Fim do documento — 40 casos de teste para o módulo AuditCompliance*
