# Cenários de Teste — Módulo: Governance

> **Versão:** 1.0  
> **Data:** 2026-05-18  
> **Responsável:** QA Engineering  
> **Módulo:** Governance  
> **Total de casos:** 37

---

## Sumário

| ID | Título | Prioridade |
|----|--------|-----------|
| TC-GOV-001 | Criar pacote de governança com sucesso | Crítica |
| TC-GOV-002 | Atualizar pacote de governança existente | Alta |
| TC-GOV-003 | Obter pacote de governança por ID | Alta |
| TC-GOV-004 | Listar pacotes de governança com paginação | Média |
| TC-GOV-005 | Aplicar pacote de governança a um ativo | Crítica |
| TC-GOV-006 | Verificar cobertura de pacote de conformidade | Alta |
| TC-GOV-007 | Obter resumo de conformidade (GetComplianceSummary) | Crítica |
| TC-GOV-008 | Identificar lacunas de conformidade (GetComplianceGaps) | Alta |
| TC-GOV-009 | Criar ruleset de automação | Alta |
| TC-GOV-010 | Ativar ruleset (ActivateRuleset) | Alta |
| TC-GOV-011 | Arquivar ruleset (ArchiveRuleset) | Média |
| TC-GOV-012 | Excluir ruleset inativo | Alta |
| TC-GOV-013 | Instalar rulesets padrão (InstallDefaultRulesets) | Alta |
| TC-GOV-014 | Fazer upload de ruleset via arquivo | Média |
| TC-GOV-015 | Vincular ruleset a tipo de ativo (BindRulesetToAssetType) | Alta |
| TC-GOV-016 | Computar pontuação de ruleset (ComputeRulesetScore) | Alta |
| TC-GOV-017 | Obter achados de ruleset (GetRulesetFindings) | Alta |
| TC-GOV-018 | Executar verificação pré-commit de governança | Crítica |
| TC-GOV-019 | Simular aplicação de política (SimulatePolicyApplication) | Alta |
| TC-GOV-020 | Registrar política como código (RegisterPolicyAsCode) | Alta |
| TC-GOV-021 | Obter resumo de controles (GetControlsSummary) | Alta |
| TC-GOV-022 | Obter matriz de cobertura de conformidade | Alta |
| TC-GOV-023 | Obter relatório de lacunas entre padrões | Alta |
| TC-GOV-024 | Criar solicitação de isenção de governança | Crítica |
| TC-GOV-025 | Aprovar isenção de governança | Crítica |
| TC-GOV-026 | Rejeitar isenção de governança com justificativa | Alta |
| TC-GOV-027 | Expirar isenções vencidas (ExpireGovernanceWaivers) | Alta |
| TC-GOV-028 | Listar isenções de governança por status | Média |
| TC-GOV-029 | Obter relatório de escalação de governança | Alta |
| TC-GOV-030 | Ativar pacote Spectral (ActivateSpectralPackage) | Alta |
| TC-GOV-031 | Obter marketplace Spectral (GetSpectralMarketplace) | Média |
| TC-GOV-032 | Criar categoria de taxonomia | Média |
| TC-GOV-033 | Criar domínio organizacional | Alta |
| TC-GOV-034 | Atualizar domínio e verificar histórico | Média |
| TC-GOV-035 | Obter dependências entre domínios (GetCrossTeamDependencies) | Alta |
| TC-GOV-036 | Criar e vincular equipe a domínio | Alta |
| TC-GOV-037 | Obter resumo de governança por equipe | Alta |

---

### TC-GOV-001 — Criar pacote de governança com sucesso

| Campo | Valor |
|-------|-------|
| **Módulo** | Governance |
| **Feature** | CreateGovernancePack |
| **Tipo** | Integração |
| **Prioridade** | Crítica |

**Pré-condições:**
- Usuário autenticado com role `governance:admin`
- Tenant com capability `ai_governance` ou `governance_packs`

**Passos:**
1. Autenticar com JWT contendo capability de governança
2. Enviar `POST /api/governance/packs` com body `{ "name": "ISO 27001 Compliance Pack", "standard": "ISO27001", "version": "2022", "controls": [...] }`
3. Verificar resposta HTTP e ID gerado

**Resultado Esperado:**
- HTTP 201 Created
- Body contém `id` (GUID), `name`, `standard`, `version`, `status: "Draft"`
- Pack disponível via `GET /api/governance/packs/{id}`

**Critério de Aceite:** `result.IsSuccess == true` / HTTP 201 com ID persistido

---

### TC-GOV-002 — Atualizar pacote de governança existente

| Campo | Valor |
|-------|-------|
| **Módulo** | Governance |
| **Feature** | UpdateGovernancePack |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Pack `ISO 27001 Compliance Pack` criado com status `Draft`

**Passos:**
1. Autenticar com JWT de admin
2. Enviar `PUT /api/governance/packs/{id}` com body atualizado incluindo novo controle
3. Verificar versionamento

**Resultado Esperado:**
- HTTP 200 OK
- Pack retornado com controle adicionado
- Versão incrementada ou `updatedAt` atualizado

**Critério de Aceite:** Atualização persistida sem perda de dados anteriores

---

### TC-GOV-003 — Obter pacote de governança por ID

| Campo | Valor |
|-------|-------|
| **Módulo** | Governance |
| **Feature** | GetGovernancePack |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Pack criado com ID conhecido

**Passos:**
1. Autenticar com JWT válido
2. Enviar `GET /api/governance/packs/{id}`

**Resultado Esperado:**
- HTTP 200 OK
- Dados completos do pack: nome, padrão, versão, controles, status, `createdAt`

**Critério de Aceite:** HTTP 200 e todos os campos do pack presentes

---

### TC-GOV-004 — Listar pacotes de governança com paginação

| Campo | Valor |
|-------|-------|
| **Módulo** | Governance |
| **Feature** | ListGovernancePacks |
| **Tipo** | Integração |
| **Prioridade** | Média |

**Pré-condições:**
- Pelo menos 15 packs criados no tenant atual

**Passos:**
1. Autenticar com JWT válido
2. Enviar `GET /api/governance/packs?page=1&pageSize=10`
3. Enviar `GET /api/governance/packs?page=2&pageSize=10`

**Resultado Esperado:**
- Página 1: 10 itens, `totalCount >= 15`
- Página 2: restante dos itens
- Nenhuma sobreposição entre páginas

**Critério de Aceite:** Paginação correta e isolada por tenant via `TenantId`

---

### TC-GOV-005 — Aplicar pacote de governança a um ativo

| Campo | Valor |
|-------|-------|
| **Módulo** | Governance |
| **Feature** | ApplyGovernancePack |
| **Tipo** | Integração |
| **Prioridade** | Crítica |

**Pré-condições:**
- Pack ativo com status `Published`
- Ativo de serviço com ID conhecido registrado no módulo Catalog

**Passos:**
1. Autenticar com JWT de admin
2. Enviar `POST /api/governance/packs/{packId}/apply` com body `{ "targetAssetId": "{serviceId}", "assetType": "Service" }`
3. Verificar conformidade calculada

**Resultado Esperado:**
- HTTP 202 Accepted
- Job de avaliação disparado; após conclusão, score de conformidade disponível via `GetComplianceSummary`

**Critério de Aceite:** Aplicação registrada; evento de integração publicado via Outbox para o módulo de Catalog

---

### TC-GOV-006 — Verificar cobertura de pacote de conformidade

| Campo | Valor |
|-------|-------|
| **Módulo** | Governance |
| **Feature** | GetPackCoverage |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Pack ISO 27001 aplicado a 3 serviços; 1 serviço sem cobertura

**Passos:**
1. Autenticar com JWT válido
2. Enviar `GET /api/governance/packs/{packId}/coverage`

**Resultado Esperado:**
- `coveredAssets: 3`, `totalAssets: 4`, `coveragePercentage: 75.0`
- Lista de ativos sem cobertura identificada

**Critério de Aceite:** Percentual calculado corretamente; ativos descobertos listados

---

### TC-GOV-007 — Obter resumo de conformidade (GetComplianceSummary)

| Campo | Valor |
|-------|-------|
| **Módulo** | Governance |
| **Feature** | GetComplianceSummary |
| **Tipo** | Integração |
| **Prioridade** | Crítica |

**Pré-condições:**
- Múltiplos packs aplicados; alguns controles falhando

**Passos:**
1. Autenticar com JWT válido
2. Enviar `GET /api/governance/compliance/summary`

**Resultado Esperado:**
- `totalControls`, `passing`, `failing`, `exempt`, `overallScore` (0–100)
- Distribuição por padrão (ISO, SOC2, LGPD etc.)

**Critério de Aceite:** Score calculado e distribuição por padrão correta

---

### TC-GOV-008 — Identificar lacunas de conformidade (GetComplianceGaps)

| Campo | Valor |
|-------|-------|
| **Módulo** | Governance |
| **Feature** | GetComplianceGaps |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Pack SOC 2 aplicado; controle `CC6.1` sem evidência

**Passos:**
1. Autenticar com JWT de auditoria
2. Enviar `GET /api/governance/compliance/gaps?standard=SOC2`

**Resultado Esperado:**
- Lista de controles sem cobertura, incluindo `CC6.1`
- Cada gap contém `controlId`, `description`, `severity`, `recommendation`

**Critério de Aceite:** `gaps.Count > 0` e `CC6.1` presente na lista

---

### TC-GOV-009 — Criar ruleset de automação

| Campo | Valor |
|-------|-------|
| **Módulo** | Governance |
| **Feature** | CreateAutomationRule |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Usuário com role `governance:rulesets`

**Passos:**
1. Autenticar com JWT adequado
2. Enviar `POST /api/governance/rulesets` com body `{ "name": "API Security Rules", "rules": [{ "id": "no-public-apis", "condition": "...", "severity": "High" }] }`

**Resultado Esperado:**
- HTTP 201 Created
- Ruleset criado com status `Inactive`
- ID único gerado

**Critério de Aceite:** Ruleset persistido com status inicial correto

---

### TC-GOV-010 — Ativar ruleset (ActivateRuleset)

| Campo | Valor |
|-------|-------|
| **Módulo** | Governance |
| **Feature** | ActivateRuleset |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Ruleset `API Security Rules` com status `Inactive`

**Passos:**
1. Autenticar com JWT de admin
2. Enviar `POST /api/governance/rulesets/{id}/activate`
3. Verificar status

**Resultado Esperado:**
- HTTP 200 OK
- Status alterado para `Active`
- Ativação registrada no histórico de auditoria com timestamp

**Critério de Aceite:** `status == "Active"` após ativação

---

### TC-GOV-011 — Arquivar ruleset (ArchiveRuleset)

| Campo | Valor |
|-------|-------|
| **Módulo** | Governance |
| **Feature** | ArchiveRuleset |
| **Tipo** | Integração |
| **Prioridade** | Média |

**Pré-condições:**
- Ruleset ativo sem vinculações ativas a tipos de ativos

**Passos:**
1. Autenticar com JWT de admin
2. Enviar `POST /api/governance/rulesets/{id}/archive`

**Resultado Esperado:**
- HTTP 200 OK
- Status = `Archived`
- Ruleset não aparece em listagens por padrão (filtrado por `IsArchived`)

**Critério de Aceite:** Ruleset arquivado e oculto na listagem padrão

---

### TC-GOV-012 — Excluir ruleset inativo

| Campo | Valor |
|-------|-------|
| **Módulo** | Governance |
| **Feature** | DeleteRuleset |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Ruleset com status `Inactive` sem vinculações

**Passos:**
1. Autenticar com JWT de admin
2. Enviar `DELETE /api/governance/rulesets/{id}`
3. Tentar obter o ruleset excluído

**Resultado Esperado:**
- HTTP 204 No Content
- `GET /api/governance/rulesets/{id}` retorna HTTP 404

**Critério de Aceite:** Exclusão lógica (soft-delete) ou física com 404 na consulta posterior

---

### TC-GOV-013 — Instalar rulesets padrão (InstallDefaultRulesets)

| Campo | Valor |
|-------|-------|
| **Módulo** | Governance |
| **Feature** | InstallDefaultRulesets |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Tenant recém-provisionado sem rulesets

**Passos:**
1. Autenticar como admin de plataforma
2. Enviar `POST /api/governance/rulesets/install-defaults`

**Resultado Esperado:**
- HTTP 200 OK
- Lista de rulesets instalados retornada (mínimo: API governance, security, LGPD básico)
- Rulesets disponíveis na listagem do tenant

**Critério de Aceite:** Rulesets padrão instalados e visíveis para o tenant

---

### TC-GOV-014 — Fazer upload de ruleset via arquivo

| Campo | Valor |
|-------|-------|
| **Módulo** | Governance |
| **Feature** | UploadRuleset |
| **Tipo** | Integração |
| **Prioridade** | Média |

**Pré-condições:**
- Arquivo YAML/JSON válido com regras Spectral/OPA

**Passos:**
1. Autenticar com JWT de admin
2. Enviar `POST /api/governance/rulesets/upload` com `multipart/form-data` contendo o arquivo
3. Verificar parsing e criação do ruleset

**Resultado Esperado:**
- HTTP 201 Created
- Ruleset criado a partir do arquivo com regras parseadas corretamente

**Critério de Aceite:** Arquivo processado; ruleset disponível com regras corretas

---

### TC-GOV-015 — Vincular ruleset a tipo de ativo (BindRulesetToAssetType)

| Campo | Valor |
|-------|-------|
| **Módulo** | Governance |
| **Feature** | BindRulesetToAssetType |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Ruleset ativo `API Security Rules`
- Tipo de ativo `RestApi` registrado

**Passos:**
1. Autenticar com JWT de admin
2. Enviar `POST /api/governance/rulesets/{id}/bind` com body `{ "assetType": "RestApi" }`
3. Registrar novo ativo do tipo `RestApi`
4. Verificar se avaliação automática foi disparada

**Resultado Esperado:**
- HTTP 200 OK na vinculação
- Novos ativos do tipo `RestApi` avaliados automaticamente pelo ruleset

**Critério de Aceite:** Vinculação registrada; avaliação automática acionada para novos ativos

---

### TC-GOV-016 — Computar pontuação de ruleset (ComputeRulesetScore)

| Campo | Valor |
|-------|-------|
| **Módulo** | Governance |
| **Feature** | ComputeRulesetScore |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Ruleset vinculado a 5 ativos; 2 com falhas

**Passos:**
1. Autenticar com JWT de admin
2. Enviar `POST /api/governance/rulesets/{id}/compute-score`

**Resultado Esperado:**
- HTTP 200 OK
- `score: 60.0`, `passing: 3`, `failing: 2`, `totalAssets: 5`

**Critério de Aceite:** Score calculado proporcionalmente ao número de ativos conformes

---

### TC-GOV-017 — Obter achados de ruleset (GetRulesetFindings)

| Campo | Valor |
|-------|-------|
| **Módulo** | Governance |
| **Feature** | GetRulesetFindings |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Ruleset avaliado com pelo menos 3 violações encontradas

**Passos:**
1. Autenticar com JWT válido
2. Enviar `GET /api/governance/rulesets/{id}/findings`

**Resultado Esperado:**
- Lista de achados com `ruleId`, `severity`, `assetId`, `assetType`, `description`, `recommendation`
- Ordenados por severidade (Critical > High > Medium > Low)

**Critério de Aceite:** Achados retornados com todos os campos obrigatórios preenchidos

---

### TC-GOV-018 — Executar verificação pré-commit de governança

| Campo | Valor |
|-------|-------|
| **Módulo** | Governance |
| **Feature** | RunPreCommitGovernanceCheck |
| **Tipo** | Integração |
| **Prioridade** | Crítica |

**Pré-condições:**
- Repositório com política de governança ativa bloqueando APIs sem autenticação

**Passos:**
1. Autenticar com token de CI/CD
2. Enviar `POST /api/governance/policies/pre-commit-check` com payload do diff contendo nova rota sem autenticação
3. Verificar resultado

**Resultado Esperado:**
- HTTP 422 Unprocessable Entity
- Lista de violações identificadas no diff com regra `no-public-apis` violada
- Commit bloqueado

**Critério de Aceite:** Verificação pré-commit detecta violação e bloqueia a operação

---

### TC-GOV-019 — Simular aplicação de política (SimulatePolicyApplication)

| Campo | Valor |
|-------|-------|
| **Módulo** | Governance |
| **Feature** | SimulatePolicyApplication |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Política de governança definida; ativo de teste disponível

**Passos:**
1. Autenticar com JWT válido
2. Enviar `POST /api/governance/policies/simulate` com `{ "policyId": "{id}", "targetAssetId": "{id}", "dryRun": true }`
3. Verificar resultado sem aplicação real

**Resultado Esperado:**
- HTTP 200 OK
- `simulationResult` contendo `passing`, `failing`, `warnings`
- Nenhuma alteração no estado do ativo

**Critério de Aceite:** Simulação não altera estado; resultado reflete avaliação correta

---

### TC-GOV-020 — Registrar política como código (RegisterPolicyAsCode)

| Campo | Valor |
|-------|-------|
| **Módulo** | Governance |
| **Feature** | RegisterPolicyAsCode |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Política em formato Rego (OPA) ou YAML disponível
- Usuário com role `governance:policy-admin`

**Passos:**
1. Autenticar com JWT adequado
2. Enviar `POST /api/governance/policies/as-code` com payload contendo nome, linguagem (`rego`) e conteúdo da política
3. Obter a política registrada

**Resultado Esperado:**
- HTTP 201 Created
- Política armazenada e disponível via `GetPolicyAsCode`
- Hash do conteúdo calculado para integridade

**Critério de Aceite:** Política registrada com hash calculado e recuperável

---

### TC-GOV-021 — Obter resumo de controles (GetControlsSummary)

| Campo | Valor |
|-------|-------|
| **Módulo** | Governance |
| **Feature** | GetControlsSummary |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Múltiplos controles avaliados em diferentes padrões

**Passos:**
1. Autenticar com JWT válido
2. Enviar `GET /api/governance/controls/summary`

**Resultado Esperado:**
- Totais por status: `passed`, `failed`, `notEvaluated`, `exempt`
- Distribuição por padrão (ISO 27001, SOC 2, LGPD, PCI-DSS)
- Score geral calculado

**Critério de Aceite:** Sumário completo com distribuição por padrão correta

---

### TC-GOV-022 — Obter matriz de cobertura de conformidade

| Campo | Valor |
|-------|-------|
| **Módulo** | Governance |
| **Feature** | GetComplianceCoverageMatrixReport |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Múltiplos padrões com controles mapeados a serviços

**Passos:**
1. Autenticar com JWT de auditoria
2. Enviar `GET /api/governance/reports/compliance-coverage-matrix`

**Resultado Esperado:**
- Matriz com serviços nas linhas e padrões nas colunas
- Cada célula indica: `covered`, `partial`, `missing`

**Critério de Aceite:** Matriz gerada com todos os serviços e padrões ativos

---

### TC-GOV-023 — Obter relatório de lacunas entre padrões

| Campo | Valor |
|-------|-------|
| **Módulo** | Governance |
| **Feature** | GetCrossStandardComplianceGapReport |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Padrões ISO 27001 e SOC 2 ativos; controles sobrepostos identificados

**Passos:**
1. Autenticar com JWT de auditoria
2. Enviar `GET /api/governance/reports/cross-standard-gap?standards=ISO27001,SOC2`

**Resultado Esperado:**
- Controles presentes em um padrão mas ausentes no outro
- Mapeamento de equivalências entre controles
- Lacunas classificadas por severidade

**Critério de Aceite:** Lacunas identificadas com mapeamento de equivalência correto

---

### TC-GOV-024 — Criar solicitação de isenção de governança

| Campo | Valor |
|-------|-------|
| **Módulo** | Governance |
| **Feature** | CreateGovernanceWaiver |
| **Tipo** | Integração |
| **Prioridade** | Crítica |

**Pré-condições:**
- Controle `CC7.1` em falha para serviço `payment-svc`
- Usuário com role `governance:waiver-request`

**Passos:**
1. Autenticar com JWT adequado
2. Enviar `POST /api/governance/waivers` com body `{ "controlId": "CC7.1", "assetId": "{id}", "reason": "Legado aguardando migração Q3/2026", "expiresAt": "2026-09-30" }`

**Resultado Esperado:**
- HTTP 201 Created
- Isenção criada com status `PendingApproval`
- ID da isenção retornado

**Critério de Aceite:** Isenção criada com status `PendingApproval` e roteada para aprovador

---

### TC-GOV-025 — Aprovar isenção de governança

| Campo | Valor |
|-------|-------|
| **Módulo** | Governance |
| **Feature** | ApproveGovernanceWaiver |
| **Tipo** | Integração |
| **Prioridade** | Crítica |

**Pré-condições:**
- Isenção com status `PendingApproval` existente
- Usuário com role `governance:waiver-approver`

**Passos:**
1. Autenticar como aprovador
2. Enviar `POST /api/governance/waivers/{id}/approve` com body `{ "comment": "Aprovado conforme política de legado" }`
3. Verificar status da isenção
4. Verificar impacto no controle: controle marcado como `Exempt`

**Resultado Esperado:**
- HTTP 200 OK
- Isenção com status `Approved`
- Controle associado com status `Exempt` no resumo de conformidade

**Critério de Aceite:** Isenção aprovada; controle marcado como `Exempt` sem afetar score negativamente

---

### TC-GOV-026 — Rejeitar isenção de governança com justificativa

| Campo | Valor |
|-------|-------|
| **Módulo** | Governance |
| **Feature** | RejectGovernanceWaiver |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Isenção com status `PendingApproval`
- Aprovador identificado no JWT

**Passos:**
1. Autenticar como aprovador
2. Enviar `POST /api/governance/waivers/{id}/reject` com body `{ "reason": "Risco inaceitável para ambiente de produção" }`

**Resultado Esperado:**
- HTTP 200 OK
- Isenção com status `Rejected`
- Motivo da rejeição registrado
- Controle permanece com status de falha

**Critério de Aceite:** Rejeição registrada com motivo; controle não alterado

---

### TC-GOV-027 — Expirar isenções vencidas (ExpireGovernanceWaivers)

| Campo | Valor |
|-------|-------|
| **Módulo** | Governance |
| **Feature** | ExpireGovernanceWaivers |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Isenção aprovada com `expiresAt` = data anterior a hoje (2026-05-17)

**Passos:**
1. Autenticar como job de plataforma ou admin
2. Enviar `POST /api/governance/waivers/expire` (ou job automático)
3. Verificar status da isenção expirada
4. Verificar que o controle volta ao status de falha

**Resultado Esperado:**
- HTTP 200 OK
- Isenções expiradas marcadas com status `Expired`
- Controle associado volta ao status `Failing`

**Critério de Aceite:** Expiração automática funcional; controle volta ao estado original

---

### TC-GOV-028 — Listar isenções de governança por status

| Campo | Valor |
|-------|-------|
| **Módulo** | Governance |
| **Feature** | ListGovernanceWaivers |
| **Tipo** | Integração |
| **Prioridade** | Média |

**Pré-condições:**
- Isenções em múltiplos estados: `PendingApproval`, `Approved`, `Rejected`, `Expired`

**Passos:**
1. Autenticar com JWT válido
2. Enviar `GET /api/governance/waivers?status=Approved`
3. Enviar `GET /api/governance/waivers?status=PendingApproval`

**Resultado Esperado:**
- Cada chamada retorna apenas isenções no status solicitado
- Paginação funcional

**Critério de Aceite:** Filtro por status funcional; isolamento por tenant correto

---

### TC-GOV-029 — Obter relatório de escalação de governança

| Campo | Valor |
|-------|-------|
| **Módulo** | Governance |
| **Feature** | GetGovernanceEscalationReport |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Isenções com prazo vencendo em menos de 7 dias
- Controles críticos em falha há mais de 30 dias sem isenção

**Passos:**
1. Autenticar com JWT de liderança de governança
2. Enviar `GET /api/governance/reports/escalation`

**Resultado Esperado:**
- Lista de itens críticos necessitando atenção
- Isenções próximas de expirar identificadas com dias restantes
- Violações crônicas sem remediação destacadas

**Critério de Aceite:** Relatório de escalação com todos os itens de alta prioridade

---

### TC-GOV-030 — Ativar pacote Spectral (ActivateSpectralPackage)

| Campo | Valor |
|-------|-------|
| **Módulo** | Governance |
| **Feature** | ActivateSpectralPackage |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Pacote Spectral disponível no marketplace com ID conhecido
- Tenant com permissão de governança

**Passos:**
1. Autenticar com JWT de admin
2. Enviar `POST /api/governance/spectral/packages/{packageId}/activate`
3. Verificar ruleset gerado a partir do pacote

**Resultado Esperado:**
- HTTP 200 OK
- Pacote ativo para o tenant
- Ruleset correspondente criado e disponível

**Critério de Aceite:** Pacote ativado; ruleset gerado corretamente

---

### TC-GOV-031 — Obter marketplace Spectral (GetSpectralMarketplace)

| Campo | Valor |
|-------|-------|
| **Módulo** | Governance |
| **Feature** | GetSpectralMarketplace |
| **Tipo** | Integração |
| **Prioridade** | Média |

**Pré-condições:**
- Marketplace com pelo menos 5 pacotes disponíveis

**Passos:**
1. Autenticar com JWT válido
2. Enviar `GET /api/governance/spectral/marketplace`

**Resultado Esperado:**
- Lista de pacotes com `name`, `description`, `version`, `author`, `downloadCount`, `isActivated`
- Pacotes já ativados marcados como `isActivated: true`

**Critério de Aceite:** Marketplace retornado com estado de ativação por tenant correto

---

### TC-GOV-032 — Criar categoria de taxonomia

| Campo | Valor |
|-------|-------|
| **Módulo** | Governance |
| **Feature** | CreateTaxonomyCategory |
| **Tipo** | Integração |
| **Prioridade** | Média |

**Pré-condições:**
- Usuário com role `governance:taxonomy`

**Passos:**
1. Autenticar com JWT adequado
2. Enviar `POST /api/governance/taxonomy/categories` com body `{ "name": "Data Classification", "description": "Categorias de classificação de dados", "parentId": null }`
3. Criar subcategoria com `parentId` preenchido

**Resultado Esperado:**
- HTTP 201 Created para ambas as chamadas
- Hierarquia pai/filho refletida na listagem

**Critério de Aceite:** Hierarquia de taxonomia criada corretamente

---

### TC-GOV-033 — Criar domínio organizacional

| Campo | Valor |
|-------|-------|
| **Módulo** | Governance |
| **Feature** | CreateDomain |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Usuário com role `governance:domains`

**Passos:**
1. Autenticar com JWT adequado
2. Enviar `POST /api/governance/domains` com body `{ "name": "Pagamentos", "description": "Domínio de processamento de pagamentos", "ownerId": "{userId}" }`

**Resultado Esperado:**
- HTTP 201 Created
- Domínio com ID gerado e `ownerId` registrado

**Critério de Aceite:** Domínio criado com proprietário vinculado

---

### TC-GOV-034 — Atualizar domínio e verificar histórico

| Campo | Valor |
|-------|-------|
| **Módulo** | Governance |
| **Feature** | UpdateDomain |
| **Tipo** | Integração |
| **Prioridade** | Média |

**Pré-condições:**
- Domínio `Pagamentos` criado com proprietário `user-A`

**Passos:**
1. Autenticar como `user-A` (proprietário)
2. Enviar `PUT /api/governance/domains/{id}` alterando descrição e adicionando tags
3. Verificar campo `updatedAt` e `updatedBy`

**Resultado Esperado:**
- HTTP 200 OK
- `updatedAt` atualizado, `updatedBy` reflete `user-A`
- Histórico de alterações disponível

**Critério de Aceite:** Auditoria automática via `AuditInterceptor` populada corretamente

---

### TC-GOV-035 — Obter dependências entre domínios (GetCrossTeamDependencies)

| Campo | Valor |
|-------|-------|
| **Módulo** | Governance |
| **Feature** | GetCrossTeamDependencies |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Domínios `Pagamentos` e `Identidade` com dependência declarada entre serviços

**Passos:**
1. Autenticar com JWT válido
2. Enviar `GET /api/governance/domains/{paymentsDomainId}/cross-dependencies`

**Resultado Esperado:**
- Lista de dependências externas: serviços de outros domínios dos quais `Pagamentos` depende
- Dependências reversas (quem depende de `Pagamentos`) também listadas

**Critério de Aceite:** Grafo de dependências cross-domínio correto e bidirecional

---

### TC-GOV-036 — Criar e vincular equipe a domínio

| Campo | Valor |
|-------|-------|
| **Módulo** | Governance |
| **Feature** | CreateTeam |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Domínio `Pagamentos` criado
- Usuário com role `governance:teams`

**Passos:**
1. Autenticar com JWT adequado
2. Enviar `POST /api/governance/teams` com body `{ "name": "Time Pix", "domainId": "{paymentsDomainId}", "members": [{ "userId": "...", "role": "lead" }] }`
3. Verificar via `GetTeamDetail`

**Resultado Esperado:**
- HTTP 201 Created
- Time vinculado ao domínio correto
- Membro com role `lead` registrado

**Critério de Aceite:** Time criado e associado ao domínio; membros registrados corretamente

---

### TC-GOV-037 — Obter resumo de governança por equipe

| Campo | Valor |
|-------|-------|
| **Módulo** | Governance |
| **Feature** | GetTeamGovernanceSummary |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Time `Time Pix` com serviços avaliados; 2 controles falhando

**Passos:**
1. Autenticar com JWT válido
2. Enviar `GET /api/governance/teams/{id}/governance-summary`

**Resultado Esperado:**
- `complianceScore`, `failingControls: 2`, `openWaivers`, `pendingRemediation`
- Comparação com média do domínio e da plataforma

**Critério de Aceite:** Resumo com todos os indicadores calculados e comparação de benchmark presente
