# Plano de Verificação de Conformidade de Contratos — NexTraceOne

> **Versão:** 1.0  
> **Data:** 2026-04-10  
> **Módulos impactados:** Catalog, ChangeGovernance, Configuration, Integrations, CLI  
> **Pilares reforçados:** Contract Governance, Change Intelligence, Source of Truth, Operational Consistency

---

## 1. Contexto e Problema

O NexTraceOne posiciona-se como **Source of Truth para contratos de serviços**. Atualmente, o produto já permite:

- Criar contratos (REST, SOAP, Event/AsyncAPI, Background) via Contract Studio
- Versionar e gerir o ciclo de vida (Draft → InReview → Approved → Locked → Deprecated → Sunset → Retired)
- Computar diffs semânticos e classificar breaking changes
- Avaliar compliance gates antes de promoções entre ambientes
- Importar contratos via batch (SyncContracts) para integração com CI/CD
- Detetar drift entre contrato declarado e operações observadas em produção

### O Problema Central

**Não existe ainda um mecanismo que garanta que o contrato efetivamente implementado no código-fonte corresponde ao que foi desenhado/aprovado no NexTraceOne.**

Isto significa que:

1. Uma equipa pode desenhar um contrato REST no NexTraceOne com endpoints X, Y, Z
2. O developer implementa apenas X e Y, ou adiciona endpoints W não previstos
3. O deploy acontece sem que o NexTraceOne detete a divergência
4. O NexTraceOne continua a mostrar o contrato "oficial" mas a realidade em produção é diferente
5. Incidentes podem ocorrer por contratos desatualizados ou divergentes

### Consequências do gap

- **Source of Truth comprometida** — o contrato no NexTraceOne não reflete a realidade
- **Falsa confiança em mudanças** — compliance gates passam mas o contrato real é diferente
- **Changelog incompleto** — alterações no código-fonte não geram registos no NexTraceOne
- **Drift silencioso** — divergências acumulam-se sem visibilidade

---

## 2. Visão da Solução

### 2.1 Objetivo

Criar um **fluxo completo de verificação de conformidade de contratos** que garanta que:

- O contrato implementado no código-fonte corresponde ao aprovado no NexTraceOne
- Divergências são detetadas automaticamente no pipeline de CI/CD
- O utilizador pode escolher o nível de enforcement (avisar, bloquear build, bloquear deploy)
- Alterações no contrato geram changelog automático
- O NexTraceOne mantém-se como Source of Truth efetiva e verificável

### 2.2 Princípio arquitetural

O fluxo deve funcionar em **3 camadas complementares**:

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        DESIGN TIME (NexTraceOne)                        │
│  Contrato desenhado → Aprovado → Locked → Publicado como "verdade"     │
└──────────────────────────────────┬──────────────────────────────────────┘
                                   │
                                   ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                       BUILD TIME (CI/CD Pipeline)                       │
│  Contrato extraído do código → Comparado com NexTraceOne → Resultado   │
│  ┌─────────────┐  ┌──────────────────┐  ┌──────────────────────────┐   │
│  │ nex contract │→│ API NexTraceOne  │→│ Pass / Warn / Block      │   │
│  │   verify     │  │ /verify-compliance│  │ + Changelog gerado      │   │
│  └─────────────┘  └──────────────────┘  └──────────────────────────┘   │
└──────────────────────────────────┬──────────────────────────────────────┘
                                   │
                                   ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                      RUNTIME (Observabilidade)                          │
│  Drift detection contínuo: endpoints observados vs contrato oficial    │
│  (já parcialmente implementado via DetectContractDrift)                 │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 3. Abordagens de Verificação

O utilizador do NexTraceOne deve poder escolher qual abordagem de verificação usar, conforme a maturidade e necessidades da organização.

### 3.1 Abordagem A — Verificação por Spec File (Recomendada para início)

**Como funciona:**

1. O repositório do serviço contém o ficheiro de especificação (OpenAPI, WSDL, AsyncAPI, etc.)
2. No pipeline CI/CD, o CLI do NexTraceOne extrai o spec e envia para a API
3. A API compara com a versão aprovada/locked no NexTraceOne
4. Retorna resultado: Pass, Warn, Block + diff detalhado

**Vantagens:** Simples, não requer instrumentação de código, funciona com qualquer linguagem  
**Limitações:** Depende de o developer manter o spec atualizado no repositório

### 3.2 Abordagem B — Verificação por Extração Automática do Código

**Como funciona:**

1. O pipeline CI/CD usa ferramentas específicas da linguagem para extrair o contrato do código compilado
2. Para .NET: usa reflection ou Swagger/OpenAPI generation (Swashbuckle, NSwag)
3. Para Java: usa springdoc-openapi ou Swagger annotations
4. O spec extraído é enviado para comparação com o NexTraceOne

**Vantagens:** Contrato sempre reflete o código real, sem manutenção manual de spec  
**Limitações:** Requer setup por stack tecnológica, pode precisar de build completo

### 3.3 Abordagem C — Verificação Híbrida (Spec + Runtime)

**Como funciona:**

1. Build time: Compara spec do repositório com NexTraceOne (Abordagem A)
2. Post-deploy: Deteta drift com observações de runtime (DetectContractDrift existente)
3. Correlação: Liga divergências de build-time com drift de runtime

**Vantagens:** Cobertura completa, deteta problemas em ambas as fases  
**Limitações:** Mais complexo de configurar

### 3.4 Abordagem D — Consumer-Driven Contract Testing (CDCT)

**Como funciona:**

1. Consumidores registam expectativas no NexTraceOne (já existe: `RegisterConsumerExpectation`)
2. No pipeline do provider, as expectativas são verificadas contra o spec atual
3. Breaking changes que violem expectativas de consumidores são bloqueados

**Vantagens:** Garante compatibilidade real com consumidores  
**Limitações:** Requer participação ativa dos consumidores

---

## 4. Plano de Implementação

### Fase 1 — Backend: API de Verificação de Conformidade

#### 4.1.1 Novo Feature: `VerifyContractCompliance`

**Módulo:** Catalog  
**Localização:** `src/modules/catalog/NexTraceOne.Catalog.Application/Contracts/Features/VerifyContractCompliance/`

**Responsabilidade:**

- Receber spec do pipeline (conteúdo OpenAPI/WSDL/AsyncAPI)
- Identificar o contrato correspondente no NexTraceOne (por ApiAssetId + ServiceName)
- Comparar com a versão aprovada/locked mais recente
- Computar diff semântico
- Avaliar compliance gates aplicáveis
- Retornar resultado estruturado
- Gerar changelog automático quando há alterações

**Command Input:**

```
VerifyContractComplianceCommand
├── ApiAssetId (string) — identificador do contrato no NexTraceOne
├── ServiceName (string) — nome do serviço
├── SpecContent (string) — conteúdo do spec extraído do código
├── SpecFormat (string) — formato: openapi-yaml, openapi-json, wsdl, asyncapi, protobuf
├── SourceBranch (string?) — branch de origem
├── CommitSha (string?) — SHA do commit
├── PipelineId (string?) — identificador do pipeline
├── SourceSystem (string) — ex: "github-actions", "gitlab-ci", "jenkins", "azure-devops"
├── EnvironmentName (string?) — ambiente alvo
└── DryRun (bool) — se true, não persiste resultados
```

**Command Output:**

```
VerifyContractComplianceResult
├── Status (enum) — Pass | Warn | Block
├── ContractVersionId (Guid?) — versão de referência usada
├── ContractVersionSemVer (string?) — ex: "2.1.0"
├── DiffSummary
│   ├── BreakingChanges (ChangeEntry[])
│   ├── NonBreakingChanges (ChangeEntry[])
│   ├── AdditiveChanges (ChangeEntry[])
│   ├── RemovedEndpoints (string[])
│   └── NewEndpoints (string[])
├── ComplianceViolations (Violation[])
├── SuggestedAction (string) — ex: "Criar nova versão no NexTraceOne"
├── ChangelogEntries (ChangelogEntry[]) — entradas geradas
├── VerificationId (Guid) — ID do registo de verificação
└── Message (string) — mensagem descritiva
```

#### 4.1.2 Nova Entidade: `ContractVerification`

**Módulo:** Catalog  
**Localização:** `src/modules/catalog/NexTraceOne.Catalog.Domain/Contracts/Entities/`

**Responsabilidade:** Persistir o histórico de verificações de conformidade para auditoria.

```
ContractVerification
├── Id (Guid)
├── TenantId (Guid)
├── ApiAssetId (string)
├── ServiceName (string)
├── ContractVersionId (Guid?) — versão de referência
├── SpecContentHash (string) — hash SHA-256 do spec enviado
├── Status (VerificationStatus) — Pass | Warn | Block | Error
├── BreakingChangesCount (int)
├── NonBreakingChangesCount (int)
├── AdditiveChangesCount (int)
├── DiffDetails (jsonb) — diff detalhado serializado
├── ComplianceViolations (jsonb) — violações encontradas
├── SourceSystem (string) — origem: github-actions, jenkins, etc.
├── SourceBranch (string?)
├── CommitSha (string?)
├── PipelineId (string?)
├── EnvironmentName (string?)
├── VerifiedAt (DateTimeOffset)
├── VerifiedBy (string?) — user ou sistema
├── CreatedAt (DateTimeOffset)
└── CreatedBy (string)
```

#### 4.1.3 Novo Feature: `ListContractVerifications`

Query para consultar histórico de verificações com filtros por serviço, ambiente, status e período.

#### 4.1.4 Novo Feature: `GetContractVerificationDetail`

Query para consultar detalhe de uma verificação específica.

#### 4.1.5 Novo Endpoint

**Rota:** `POST /api/v1/contracts/verify-compliance`  
**Permissão:** `contracts:verify` (nova permissão granular)  
**Rate limiting:** Configurável por tenant

---

### Fase 2 — Configuração e Parametrização

#### 4.2.1 Nova Entidade: `ContractCompliancePolicy`

**Módulo:** Configuration  
**Localização:** `src/modules/configuration/NexTraceOne.Configuration.Domain/Entities/`

**Responsabilidade:** Definir políticas de compliance de contratos por tenant, equipa ou ambiente.

```
ContractCompliancePolicy
├── Id (Guid)
├── TenantId (Guid)
├── Name (string) — nome da política
├── Description (string?) — descrição
├── Scope (PolicyScope) — Organization | Team | Environment | Service
├── ScopeId (string?) — ID do escopo (team/env/service ID)
├── IsActive (bool)
│
│  ── Verificação de Conformidade ──
├── VerificationMode (VerificationMode) — SpecFile | AutoExtract | Hybrid | Disabled
├── VerificationApproach (VerificationApproach) — Passive | Active | Strict
│
│  ── Ações em caso de divergência ──
├── OnBreakingChange (ComplianceAction) — Ignore | Warn | BlockBuild | BlockDeploy
├── OnNonBreakingChange (ComplianceAction) — Ignore | Warn | BlockBuild | BlockDeploy
├── OnNewEndpoint (ComplianceAction) — Ignore | Warn | BlockBuild | BlockDeploy
├── OnRemovedEndpoint (ComplianceAction) — Ignore | Warn | BlockBuild | BlockDeploy
├── OnMissingContract (ComplianceAction) — Ignore | Warn | BlockBuild | BlockDeploy
├── OnContractNotApproved (ComplianceAction) — Ignore | Warn | BlockBuild | BlockDeploy
│
│  ── Changelog automático ──
├── AutoGenerateChangelog (bool) — gerar changelog automaticamente
├── ChangelogFormat (ChangelogFormat) — Markdown | Json | Both
├── RequireChangelogApproval (bool) — exigir aprovação do changelog
│
│  ── Consumer-Driven Contracts ──
├── EnforceCdct (bool) — ativar verificação CDCT
├── CdctFailureAction (ComplianceAction) — ação quando CDCT falha
│
│  ── Drift Detection ──
├── EnableRuntimeDriftDetection (bool) — ativar verificação runtime
├── DriftDetectionIntervalMinutes (int) — intervalo de verificação
├── DriftThresholdForAlert (decimal) — score de drift para alerta (0.0-1.0)
├── DriftThresholdForIncident (decimal) — score de drift para incidente (0.0-1.0)
│
│  ── Notificações ──
├── NotifyOnVerificationFailure (bool) — notificar em falha
├── NotifyOnBreakingChange (bool) — notificar em breaking change
├── NotifyOnDriftDetected (bool) — notificar em drift
├── NotificationChannels (string[]) — canais: email, webhook, teams
│
│  ── Auditoria ──
├── CreatedAt (DateTimeOffset)
├── CreatedBy (string)
├── UpdatedAt (DateTimeOffset?)
└── UpdatedBy (string?)
```

#### 4.2.2 Novos Enums

```csharp
public enum VerificationMode
{
    Disabled = 0,       // Verificação desativada
    SpecFile = 1,       // Comparação por spec file no repositório
    AutoExtract = 2,    // Extração automática do código compilado
    Hybrid = 3          // Spec file + drift detection runtime
}

public enum VerificationApproach
{
    Passive = 0,        // Apenas regista divergências, nunca bloqueia
    Active = 1,         // Regista e pode avisar, mas não bloqueia por padrão
    Strict = 2          // Aplica todas as regras de bloqueio configuradas
}

public enum ComplianceAction
{
    Ignore = 0,         // Não tomar ação
    Warn = 1,           // Aviso no pipeline (exit code 0)
    BlockBuild = 2,     // Falha o build (exit code 1)
    BlockDeploy = 3     // Permite build, bloqueia deploy/promoção
}

public enum ChangelogFormat
{
    Markdown = 0,
    Json = 1,
    Both = 2
}
```

#### 4.2.3 Features de Configuração

| Feature | Tipo | Descrição |
|---------|------|-----------|
| `CreateContractCompliancePolicy` | Command | Criar nova política |
| `UpdateContractCompliancePolicy` | Command | Atualizar política existente |
| `ActivateContractCompliancePolicy` | Command | Ativar política |
| `DeactivateContractCompliancePolicy` | Command | Desativar política |
| `GetContractCompliancePolicy` | Query | Consultar política por ID |
| `ListContractCompliancePolicies` | Query | Listar políticas com filtros |
| `GetEffectivePolicy` | Query | Resolver política efetiva para um serviço/ambiente (cascata) |

#### 4.2.4 Resolução de Política Efetiva (Cascata)

Quando um serviço é verificado, a política aplicável deve ser resolvida por cascata:

```
1. Política específica do Serviço (se existir)
   ↓ fallback
2. Política da Equipa dona do serviço (se existir)
   ↓ fallback
3. Política do Ambiente alvo (se existir)
   ↓ fallback
4. Política da Organização (default)
```

A política mais específica prevalece. Se nenhuma política existir, o comportamento default é `Passive` (apenas regista, nunca bloqueia).

#### 4.2.5 Novos Endpoints de Configuração

```
POST   /api/v1/config/contract-compliance-policies          config:write
GET    /api/v1/config/contract-compliance-policies          config:read
GET    /api/v1/config/contract-compliance-policies/{id}     config:read
PUT    /api/v1/config/contract-compliance-policies/{id}     config:write
POST   /api/v1/config/contract-compliance-policies/{id}/activate    config:write
POST   /api/v1/config/contract-compliance-policies/{id}/deactivate  config:write
GET    /api/v1/config/contract-compliance-policies/effective         config:read
       ?serviceId=&environmentName=&teamId=
```

---

### Fase 3 — CLI: Comando de Verificação

#### 4.3.1 Novo Comando: `nex contract verify`

**Localização:** `tools/NexTraceOne.CLI/Commands/ContractCommands/`

**Uso:**

```bash
# Verificação básica por spec file
nex contract verify \
  --spec ./openapi.yaml \
  --service "orders-api" \
  --url https://nextrace.example.com/api \
  --format text

# Verificação com contexto de CI/CD
nex contract verify \
  --spec ./openapi.yaml \
  --service "orders-api" \
  --api-asset-id "orders-api-v2" \
  --environment production \
  --source-system github-actions \
  --commit-sha abc123 \
  --branch main \
  --pipeline-id "run-456" \
  --url https://nextrace.example.com/api \
  --format json \
  --strict

# Verificação com bloqueio de build
nex contract verify \
  --spec ./openapi.yaml \
  --service "orders-api" \
  --fail-on-breaking \
  --url https://nextrace.example.com/api
```

**Parâmetros:**

| Parâmetro | Tipo | Obrigatório | Descrição |
|-----------|------|:-----------:|-----------|
| `--spec` | string | ✅ | Caminho para o ficheiro de spec |
| `--service` | string | ✅ | Nome do serviço no NexTraceOne |
| `--url` | string | ✅ | URL base da API do NexTraceOne |
| `--api-asset-id` | string | | Identificador do contrato (se diferente de service) |
| `--environment` | string | | Ambiente alvo (dev, staging, prod) |
| `--source-system` | string | | Sistema de origem (github-actions, jenkins, etc.) |
| `--commit-sha` | string | | SHA do commit |
| `--branch` | string | | Branch de origem |
| `--pipeline-id` | string | | ID do pipeline/run |
| `--format` | enum | | Formato de saída: `text` (default), `json`, `junit` |
| `--strict` | flag | | Usa modo strict (segue política configurada no NexTraceOne) |
| `--fail-on-breaking` | flag | | Falha (exit 1) se houver breaking changes |
| `--fail-on-any-change` | flag | | Falha (exit 1) se houver qualquer alteração |
| `--fail-on-missing` | flag | | Falha (exit 1) se o contrato não existir no NexTraceOne |
| `--dry-run` | flag | | Não persiste resultados no NexTraceOne |
| `--token` | string | | Token de autenticação (ou via env `NEXTRACE_TOKEN`) |
| `--output` | string | | Ficheiro de saída para o relatório |

**Exit Codes:**

| Código | Significado |
|--------|-------------|
| 0 | Verificação passou (sem bloqueios) |
| 1 | Verificação falhou (breaking changes ou política violada) |
| 2 | Erro de execução (ficheiro não encontrado, API indisponível, etc.) |
| 3 | Contrato não encontrado no NexTraceOne |

#### 4.3.2 Novo Comando: `nex contract diff`

```bash
# Comparar spec local com versão no NexTraceOne
nex contract diff \
  --spec ./openapi.yaml \
  --service "orders-api" \
  --url https://nextrace.example.com/api

# Comparar duas versões no NexTraceOne
nex contract diff \
  --service "orders-api" \
  --from "1.2.0" \
  --to "1.3.0" \
  --url https://nextrace.example.com/api
```

#### 4.3.3 Novo Comando: `nex contract changelog`

```bash
# Gerar changelog entre spec local e versão no NexTraceOne
nex contract changelog \
  --spec ./openapi.yaml \
  --service "orders-api" \
  --url https://nextrace.example.com/api \
  --format markdown \
  --output ./CHANGELOG-contracts.md
```

#### 4.3.4 Novo Comando: `nex contract sync`

```bash
# Sincronizar spec do repositório com NexTraceOne (criar/atualizar versão)
nex contract sync \
  --spec ./openapi.yaml \
  --service "orders-api" \
  --version "1.3.0" \
  --source-system github-actions \
  --url https://nextrace.example.com/api
```

---

### Fase 4 — Changelog Automático

#### 4.4.1 Nova Entidade: `ContractChangelog`

**Módulo:** Catalog  
**Localização:** `src/modules/catalog/NexTraceOne.Catalog.Domain/Contracts/Entities/`

```
ContractChangelog
├── Id (Guid)
├── TenantId (Guid)
├── ApiAssetId (string)
├── ServiceName (string)
├── FromVersion (string?) — versão anterior (null se primeira)
├── ToVersion (string) — versão atual
├── ContractVersionId (Guid) — versão de referência
├── VerificationId (Guid?) — ID da verificação que originou
├── Source (ChangelogSource) — Manual | Verification | Promotion | Import
│
│  ── Entradas do Changelog ──
├── Entries (ChangelogEntry[]) — lista de alterações (jsonb)
│   ├── Type (string) — "breaking", "non-breaking", "additive", "deprecation", "removal"
│   ├── Category (string) — "endpoint", "schema", "parameter", "response", "security", "header"
│   ├── Path (string) — ex: "GET /orders/{id}"
│   ├── Description (string) — descrição legível
│   ├── Severity (string) — "critical", "major", "minor", "info"
│   └── Suggestion (string?) — sugestão de correção
│
│  ── Metadados ──
├── Summary (string) — resumo gerado (pode ser por IA)
├── MarkdownContent (string?) — changelog em formato Markdown
├── JsonContent (string?) — changelog em formato JSON
├── IsApproved (bool) — se requer aprovação, indica estado
├── ApprovedBy (string?)
├── ApprovedAt (DateTimeOffset?)
│
│  ── Auditoria ──
├── CreatedAt (DateTimeOffset)
├── CreatedBy (string)
└── CommitSha (string?)
```

#### 4.4.2 Features de Changelog

| Feature | Tipo | Descrição |
|---------|------|-----------|
| `GenerateContractChangelog` | Command | Gerar changelog a partir de diff (manual ou automático) |
| `ApproveContractChangelog` | Command | Aprovar changelog pendente |
| `ListContractChangelogs` | Query | Listar changelogs com filtros |
| `GetContractChangelog` | Query | Detalhe de um changelog |
| `ExportContractChangelog` | Query | Exportar em Markdown/JSON/HTML |
| `GetContractChangelogTimeline` | Query | Timeline de alterações do contrato |

#### 4.4.3 Integração do Changelog com Verificação

Quando a verificação de conformidade deteta alterações:

```
Verificação → Diff → Changelog gerado automaticamente
                         ↓
              Se política exige aprovação:
                  → Changelog fica pendente
                  → Notificação ao owner do contrato
                  → Aprovação necessária antes de deploy
              Se não exige:
                  → Changelog registado como aprovado
                  → Visível na timeline do contrato
```

#### 4.4.4 Formato do Changelog Markdown (exemplo)

```markdown
# Changelog — orders-api

## [1.3.0] - 2026-04-10

### ⚠️ Breaking Changes
- **DELETE** `GET /orders/{id}/legacy` — Endpoint removido
- **MODIFIED** `POST /orders` — Campo `customerName` renomeado para `customer.fullName`

### ✨ New Features
- **ADDED** `GET /orders/{id}/tracking` — Novo endpoint de tracking
- **ADDED** `POST /orders/{id}/cancel` — Endpoint de cancelamento

### 🔧 Non-Breaking Changes
- **MODIFIED** `GET /orders` — Novo parâmetro opcional `status` adicionado
- **MODIFIED** Response `Order` — Campo opcional `metadata` adicionado

### 📋 Summary
2 breaking changes, 2 new endpoints, 2 non-breaking changes.
Source: CI/CD verification (github-actions, commit abc123).
Verified against NexTraceOne version 1.2.0.
```

---

### Fase 5 — Integração com CI/CD Pipelines

#### 4.5.1 GitHub Actions

```yaml
# .github/workflows/contract-check.yml
name: Contract Compliance Check
on:
  pull_request:
    paths:
      - 'src/**/openapi.yaml'
      - 'src/**/swagger.json'
      - 'src/**/asyncapi.yaml'
  push:
    branches: [main, release/*]

jobs:
  contract-verify:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Install NexTraceOne CLI
        run: |
          dotnet tool install --global NexTraceOne.CLI \
            --version ${{ vars.NEXTRACE_CLI_VERSION }}

      - name: Verify Contract Compliance
        env:
          NEXTRACE_TOKEN: ${{ secrets.NEXTRACE_TOKEN }}
          NEXTRACE_URL: ${{ vars.NEXTRACE_URL }}
        run: |
          nex contract verify \
            --spec ./src/api/openapi.yaml \
            --service "${{ github.event.repository.name }}" \
            --environment "${{ github.ref == 'refs/heads/main' && 'production' || 'development' }}" \
            --source-system github-actions \
            --commit-sha "${{ github.sha }}" \
            --branch "${{ github.ref_name }}" \
            --pipeline-id "${{ github.run_id }}" \
            --strict \
            --format json \
            --output contract-report.json

      - name: Upload Contract Report
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: contract-compliance-report
          path: contract-report.json

      - name: Comment PR with Contract Status
        if: github.event_name == 'pull_request'
        uses: actions/github-script@v7
        with:
          script: |
            const fs = require('fs');
            const report = JSON.parse(fs.readFileSync('contract-report.json', 'utf8'));
            const status = report.status === 'Pass' ? '✅' : report.status === 'Warn' ? '⚠️' : '❌';
            const body = `## ${status} Contract Compliance: ${report.status}\n\n` +
              `**Service:** ${report.serviceName}\n` +
              `**Reference Version:** ${report.contractVersionSemVer || 'N/A'}\n\n` +
              `| Type | Count |\n|------|-------|\n` +
              `| Breaking Changes | ${report.diffSummary?.breakingChanges?.length || 0} |\n` +
              `| Non-Breaking Changes | ${report.diffSummary?.nonBreakingChanges?.length || 0} |\n` +
              `| New Endpoints | ${report.diffSummary?.newEndpoints?.length || 0} |\n` +
              `| Removed Endpoints | ${report.diffSummary?.removedEndpoints?.length || 0} |\n`;
            github.rest.issues.createComment({
              issue_number: context.issue.number,
              owner: context.repo.owner,
              repo: context.repo.repo,
              body: body
            });
```

#### 4.5.2 GitLab CI

```yaml
# .gitlab-ci.yml (excerpt)
contract-verify:
  stage: validate
  image: mcr.microsoft.com/dotnet/sdk:10.0
  script:
    - dotnet tool install --global NexTraceOne.CLI
    - export PATH="$PATH:$HOME/.dotnet/tools"
    - nex contract verify
        --spec ./openapi.yaml
        --service "$CI_PROJECT_NAME"
        --environment "$CI_ENVIRONMENT_NAME"
        --source-system gitlab-ci
        --commit-sha "$CI_COMMIT_SHA"
        --branch "$CI_COMMIT_BRANCH"
        --pipeline-id "$CI_PIPELINE_ID"
        --strict
        --format junit
        --output contract-report.xml
  artifacts:
    reports:
      junit: contract-report.xml
  rules:
    - changes:
        - "**/*.yaml"
        - "**/*.json"
```

#### 4.5.3 Azure DevOps

```yaml
# azure-pipelines.yml (excerpt)
- task: DotNetCoreCLI@2
  displayName: 'Install NexTraceOne CLI'
  inputs:
    command: 'custom'
    custom: 'tool'
    arguments: 'install --global NexTraceOne.CLI'

- script: |
    nex contract verify \
      --spec ./openapi.yaml \
      --service "$(Build.Repository.Name)" \
      --environment "$(Environment.Name)" \
      --source-system azure-devops \
      --commit-sha "$(Build.SourceVersion)" \
      --branch "$(Build.SourceBranchName)" \
      --pipeline-id "$(Build.BuildId)" \
      --strict
  displayName: 'Verify Contract Compliance'
  env:
    NEXTRACE_TOKEN: $(NexTraceToken)
    NEXTRACE_URL: $(NexTraceUrl)
```

#### 4.5.4 Jenkins

```groovy
// Jenkinsfile (excerpt)
stage('Contract Compliance') {
    steps {
        sh '''
            dotnet tool install --global NexTraceOne.CLI
            export PATH="$PATH:$HOME/.dotnet/tools"
            nex contract verify \
                --spec ./openapi.yaml \
                --service "${JOB_NAME}" \
                --source-system jenkins \
                --commit-sha "${GIT_COMMIT}" \
                --branch "${GIT_BRANCH}" \
                --pipeline-id "${BUILD_ID}" \
                --strict \
                --format junit \
                --output contract-report.xml
        '''
        junit 'contract-report.xml'
    }
}
```

---

### Fase 6 — Integração com Promotion Gates

#### 4.6.1 Evolução do `EvaluateContractComplianceGate`

O gate de compliance existente no ChangeGovernance deve ser estendido para considerar:

1. **Histórico de verificações** — o serviço passou na verificação de conformidade recentemente?
2. **Política efetiva** — qual é a política configurada para aquele ambiente?
3. **Changelog pendente** — existe changelog não aprovado?
4. **CDCT** — as expectativas de consumidores foram verificadas?

**Novo fluxo de decisão:**

```
Promotion Request
  ↓
Verificar política efetiva para serviço + ambiente
  ↓
┌─ Política.OnBreakingChange == BlockDeploy?
│   → Verificar última verificação de conformidade
│   → Se houve breaking change não resolvida → Block
│
├─ Política.RequireChangelogApproval?
│   → Verificar se changelog está aprovado
│   → Se pendente → Block
│
├─ Política.EnforceCdct?
│   → Verificar expectativas de consumidores
│   → Se violadas → Block
│
├─ Política.OnMissingContract == BlockDeploy?
│   → Verificar se o serviço tem contrato registado
│   → Se não tem → Block
│
└─ Todas as verificações passaram → Pass
```

---

### Fase 7 — Frontend: Painel de Verificação de Conformidade

#### 4.7.1 Novas Páginas

| Página | Rota | Descrição |
|--------|------|-----------|
| `ContractComplianceDashboard` | `/contracts/governance/compliance` | Dashboard de conformidade com KPIs |
| `ContractVerificationHistory` | `/contracts/governance/verifications` | Histórico de verificações |
| `ContractVerificationDetail` | `/contracts/governance/verifications/:id` | Detalhe de uma verificação |
| `ContractChangelogPage` | `/contracts/:contractVersionId/changelog` | Changelog do contrato |
| `ContractCompliancePoliciesPage` | `/settings/contract-compliance` | Gestão de políticas |
| `ContractCompliancePolicyFormPage` | `/settings/contract-compliance/new` | Criar/editar política |

#### 4.7.2 KPIs do Dashboard de Conformidade

- **Taxa de conformidade** — % de verificações que passaram nos últimos 30 dias
- **Serviços sem contrato** — serviços cadastrados sem contrato registado
- **Breaking changes detetadas** — total nos últimos 30 dias
- **Changelogs pendentes** — changelogs aguardando aprovação
- **Drift score médio** — score médio de drift em produção
- **Verificações por pipeline** — distribuição por source system
- **Tendência de conformidade** — gráfico temporal

#### 4.7.3 Regras de i18n

Todos os textos devem usar chaves i18n. Exemplo de chaves necessárias:

```json
{
  "contracts.compliance.title": "Contract Compliance",
  "contracts.compliance.dashboard": "Compliance Dashboard",
  "contracts.compliance.verifications": "Verification History",
  "contracts.compliance.policy.create": "Create Policy",
  "contracts.compliance.policy.scope.organization": "Organization",
  "contracts.compliance.policy.scope.team": "Team",
  "contracts.compliance.policy.scope.environment": "Environment",
  "contracts.compliance.policy.scope.service": "Service",
  "contracts.compliance.action.ignore": "Ignore",
  "contracts.compliance.action.warn": "Warn",
  "contracts.compliance.action.blockBuild": "Block Build",
  "contracts.compliance.action.blockDeploy": "Block Deploy",
  "contracts.compliance.status.pass": "Pass",
  "contracts.compliance.status.warn": "Warning",
  "contracts.compliance.status.block": "Blocked",
  "contracts.compliance.changelog.title": "Contract Changelog",
  "contracts.compliance.changelog.pendingApproval": "Pending Approval",
  "contracts.compliance.changelog.approved": "Approved",
  "contracts.compliance.verification.noContract": "No contract registered for this service"
}
```

---

## 5. Parâmetros de Configuração Detalhados

### 5.1 Tabela de Parâmetros por Cenário

| Cenário | Parâmetro | Valor Recomendado | Descrição |
|---------|-----------|-------------------|-----------|
| **Equipa iniciante** | `VerificationMode` | `SpecFile` | Apenas compara spec file |
| | `VerificationApproach` | `Passive` | Só regista, nunca bloqueia |
| | `OnBreakingChange` | `Warn` | Avisa mas não bloqueia |
| | `AutoGenerateChangelog` | `true` | Gera changelog automático |
| | `RequireChangelogApproval` | `false` | Sem aprovação necessária |
| **Equipa madura** | `VerificationMode` | `SpecFile` | Comparação por spec |
| | `VerificationApproach` | `Active` | Avisa e pode bloquear |
| | `OnBreakingChange` | `BlockBuild` | **Impede o build** |
| | `OnRemovedEndpoint` | `BlockBuild` | **Impede remoção sem aprovação** |
| | `OnMissingContract` | `Warn` | Avisa se não há contrato |
| | `AutoGenerateChangelog` | `true` | Changelog automático |
| | `RequireChangelogApproval` | `true` | Aprovação pelo owner |
| | `EnforceCdct` | `true` | Verifica consumidores |
| **Ambiente produção** | `VerificationMode` | `Hybrid` | Spec + runtime drift |
| | `VerificationApproach` | `Strict` | Aplica todas as regras |
| | `OnBreakingChange` | `BlockDeploy` | **Impede deploy em produção** |
| | `OnRemovedEndpoint` | `BlockDeploy` | **Impede remoção** |
| | `OnNewEndpoint` | `Warn` | Avisa endpoints não documentados |
| | `OnMissingContract` | `BlockDeploy` | **Impede deploy sem contrato** |
| | `OnContractNotApproved` | `BlockDeploy` | **Contrato deve estar Approved/Locked** |
| | `EnableRuntimeDriftDetection` | `true` | Drift detection contínuo |
| | `DriftDetectionIntervalMinutes` | `60` | A cada hora |
| | `DriftThresholdForAlert` | `0.1` | Alerta com 10% de drift |
| | `DriftThresholdForIncident` | `0.3` | Incidente com 30% de drift |
| | `EnforceCdct` | `true` | CDCT obrigatório |
| | `CdctFailureAction` | `BlockDeploy` | **Bloqueia se CDCT falhar** |

### 5.2 Cenário: "Impedir build se contrato mudou"

Para implementar o cenário onde o build é impedido se o contrato foi alterado sem atualização no NexTraceOne:

**Configuração no NexTraceOne:**

```json
{
  "name": "Block Build on Contract Change",
  "scope": "Organization",
  "verificationMode": "SpecFile",
  "verificationApproach": "Strict",
  "onBreakingChange": "BlockBuild",
  "onNonBreakingChange": "Warn",
  "onNewEndpoint": "BlockBuild",
  "onRemovedEndpoint": "BlockBuild",
  "onMissingContract": "BlockBuild",
  "onContractNotApproved": "BlockBuild",
  "autoGenerateChangelog": true,
  "requireChangelogApproval": false
}
```

**No pipeline CI/CD:**

```bash
# A flag --strict faz o CLI seguir a política configurada no NexTraceOne
# Se a política diz BlockBuild → exit code 1 → build falha
nex contract verify \
  --spec ./openapi.yaml \
  --service "orders-api" \
  --strict \
  --url https://nextrace.example.com/api
```

### 5.3 Cenário: "Apenas avisar, nunca bloquear"

```json
{
  "name": "Passive Monitoring Only",
  "scope": "Organization",
  "verificationMode": "SpecFile",
  "verificationApproach": "Passive",
  "onBreakingChange": "Warn",
  "onNonBreakingChange": "Ignore",
  "onNewEndpoint": "Ignore",
  "onRemovedEndpoint": "Warn",
  "onMissingContract": "Warn",
  "onContractNotApproved": "Ignore",
  "autoGenerateChangelog": true,
  "requireChangelogApproval": false
}
```

---

## 6. Gaps Identificados no Fluxo Atual de Contratos

### 6.1 Gaps Críticos (Endereçados neste plano)

| # | Gap | Severidade | Solução proposta |
|---|-----|------------|------------------|
| G1 | **Sem verificação build-time** — não há mecanismo para verificar se o código implementado corresponde ao contrato aprovado | 🔴 Crítico | `VerifyContractCompliance` API + CLI |
| G2 | **Sem changelog automático** — alterações no contrato não geram changelog estruturado e auditável | 🔴 Crítico | `ContractChangelog` entidade + geração automática |
| G3 | **Sem política configurável** — não há parametrização para definir o nível de enforcement por equipa/ambiente | 🟠 Alto | `ContractCompliancePolicy` entidade + cascata |
| G4 | **CLI incompleto para contratos** — o CLI (`nex`) não tem comandos para verificação de contratos | 🟠 Alto | Novos comandos: `verify`, `diff`, `changelog`, `sync` |
| G5 | **Sem integração CI/CD documentada** — não há exemplos ou guias para integrar verificação no pipeline | 🟠 Alto | Exemplos para GitHub Actions, GitLab CI, Azure DevOps, Jenkins |

### 6.2 Gaps Adicionais Identificados

| # | Gap | Severidade | Descrição | Impacto |
|---|-----|------------|-----------|---------|
| G6 | **Sem versionamento automático sugerido no pipeline** | 🟡 Médio | Quando o CLI deteta alterações, deveria sugerir automaticamente a próxima versão semântica e oferecer opção de criar nova versão no NexTraceOne | Reduz friction de manutenção |
| G7 | **Drift detection não é agendado** | 🟡 Médio | `DetectContractDrift` existe como feature mas não há job/scheduler (Quartz) para executá-lo periodicamente | Drift só é detetado sob demanda |
| G8 | **Sem notificação proativa ao owner** | 🟡 Médio | Quando um contrato diverge, o owner do serviço/contrato não é notificado automaticamente | Divergências ficam silenciosas |
| G9 | **Sem histórico de conformidade por serviço** | 🟡 Médio | Não há timeline de verificações associada ao serviço para consulta rápida | Falta visibilidade operacional |
| G10 | **CDCT sem execução automática** | 🟡 Médio | `ConsumerExpectation` existe mas as expectativas não são verificadas automaticamente contra o spec | Expectativas existem mas não são enforcement |
| G11 | **Sem relatório de cobertura de contratos** | 🟡 Médio | Não há relatório que mostre: quantos serviços têm contrato, quantos estão conformes, quantos divergem | Falta visão executiva |
| G12 | **Sem suporte a múltiplos specs por serviço** | 🟢 Baixo | Um serviço pode ter REST + Event contract; a verificação deve suportar múltiplos ficheiros num único comando | Complexidade de serviços reais |
| G13 | **Sem webhook para resultado de verificação** | 🟢 Baixo | Webhook já existe para `contract.published` mas não para `contract.verification.passed/failed` | Limita automações externas |
| G14 | **Sem integração com IDE** | 🟢 Baixo | Extensões VS Code/Visual Studio não verificam conformidade em tempo real enquanto o developer edita o spec | Developer experience |
| G15 | **Sem comparação cross-environment** | 🟢 Baixo | Não é possível comparar o contrato deployed em staging vs o deployed em produção | Confiança em promoções |

### 6.3 Gaps no Changelog

| # | Gap | Descrição |
|---|-----|-----------|
| CL1 | **Sem entidade dedicada** | O `GenerateSemanticChangelog` existe mas gera texto sem persistência estruturada |
| CL2 | **Sem timeline visual** | Frontend não mostra timeline de alterações do contrato |
| CL3 | **Sem export** | Changelog não pode ser exportado em formatos standard |
| CL4 | **Sem link com Change Intelligence** | Changelog não está correlacionado com mudanças do módulo Changes |
| CL5 | **Sem aprovação** | Não há workflow de aprovação de changelog antes de deploy |
| CL6 | **Sem notificação a consumidores** | Quando há alteração no contrato, consumidores registados não são notificados do changelog |

---

## 7. Fluxo Completo Proposto (End-to-End)

```
┌──────────────────────────────────────────────────────────────────────┐
│ 1. DESIGN TIME — Contrato nasce no NexTraceOne                      │
│    ┌─────────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐  │
│    │ Create Draft │ →  │ Review   │ →  │ Approve  │ →  │  Lock    │  │
│    └─────────────┘    └──────────┘    └──────────┘    └──────────┘  │
│           ↓                                               ↓         │
│    Spec publicado como "versão oficial" no NexTraceOne              │
└───────────────────────────────────┬──────────────────────────────────┘
                                    │
                                    ▼
┌──────────────────────────────────────────────────────────────────────┐
│ 2. DEVELOPMENT — Developer implementa o serviço                     │
│    ┌─────────────────────────────────────────────────────────────┐   │
│    │ Developer escreve código + mantém spec no repositório      │   │
│    │ (openapi.yaml / wsdl / asyncapi.yaml)                      │   │
│    └─────────────────────────────────────────────────────────────┘   │
│           ↓                                                          │
│    Developer faz commit + push                                       │
└───────────────────────────────────┬──────────────────────────────────┘
                                    │
                                    ▼
┌──────────────────────────────────────────────────────────────────────┐
│ 3. BUILD TIME — Pipeline CI/CD executa verificação                  │
│    ┌──────────────────┐                                              │
│    │ nex contract      │                                             │
│    │   verify          │─────→ API NexTraceOne                       │
│    │   --spec X        │         VerifyContractCompliance             │
│    │   --strict        │                                             │
│    └────────┬─────────┘                                              │
│             ↓                                                        │
│    ┌────────────────────────────────────────┐                        │
│    │ Resolução de Política Efetiva          │                        │
│    │ (Service → Team → Environment → Org)   │                        │
│    └────────┬───────────────────────────────┘                        │
│             ↓                                                        │
│    ┌────────────────────────────────────────┐                        │
│    │ Comparação: Spec Local vs NexTraceOne  │                        │
│    │ → Compute Semantic Diff                │                        │
│    │ → Classify Breaking Changes            │                        │
│    │ → Evaluate Compliance Gates            │                        │
│    │ → Check Consumer Expectations (CDCT)   │                        │
│    └────────┬───────────────────────────────┘                        │
│             ↓                                                        │
│    ┌────────────────────────────────────────┐                        │
│    │ Resultado:                              │                       │
│    │  ✅ Pass → Build continua              │                        │
│    │  ⚠️ Warn → Build continua + log       │                        │
│    │  ❌ Block → Build falha (exit 1)       │                        │
│    └────────┬───────────────────────────────┘                        │
│             ↓                                                        │
│    ┌────────────────────────────────────────┐                        │
│    │ Efeitos colaterais:                     │                       │
│    │  → ContractVerification registada       │                       │
│    │  → ContractChangelog gerado (se config) │                       │
│    │  → Webhook enviado (se config)          │                       │
│    │  → Notificação ao owner (se config)     │                       │
│    └────────────────────────────────────────┘                        │
└───────────────────────────────────┬──────────────────────────────────┘
                                    │
                                    ▼
┌──────────────────────────────────────────────────────────────────────┐
│ 4. DEPLOY TIME — Promotion com gate de compliance                   │
│    ┌────────────────────────────────────────┐                        │
│    │ ChangeGovernance:                       │                       │
│    │   EvaluateContractComplianceGate        │                       │
│    │   → Verifica última verificação         │                       │
│    │   → Verifica changelog aprovado         │                       │
│    │   → Verifica CDCT                       │                       │
│    │   → Verifica contrato Approved/Locked   │                       │
│    │   → Aplica política do ambiente alvo    │                       │
│    └────────┬───────────────────────────────┘                        │
│             ↓                                                        │
│    ✅ Promotion aprovada ou ❌ Bloqueada                             │
└───────────────────────────────────┬──────────────────────────────────┘
                                    │
                                    ▼
┌──────────────────────────────────────────────────────────────────────┐
│ 5. RUNTIME — Drift detection contínuo                               │
│    ┌────────────────────────────────────────┐                        │
│    │ Quartz Job: ContractDriftDetectionJob   │                       │
│    │   → Compara spec com operações reais    │                       │
│    │   → Calcula drift score                 │                       │
│    │   → Se score > threshold → Alerta       │                       │
│    │   → Se score > threshold2 → Incidente   │                       │
│    └────────────────────────────────────────┘                        │
└──────────────────────────────────────────────────────────────────────┘
```

---

## 8. Ordem de Implementação Recomendada

### Sprint 1 — Fundação (Backend)

- [ ] Criar entidade `ContractVerification` + migration
- [ ] Criar feature `VerifyContractCompliance` (command + handler)
- [ ] Criar feature `ListContractVerifications` (query)
- [ ] Criar feature `GetContractVerificationDetail` (query)
- [ ] Criar endpoint `POST /api/v1/contracts/verify-compliance`
- [ ] Criar endpoints de leitura de verificações
- [ ] Testes unitários para `VerifyContractCompliance`

### Sprint 2 — Configuração e Políticas

- [ ] Criar entidade `ContractCompliancePolicy` no módulo Configuration + migration
- [ ] Criar enums: `VerificationMode`, `VerificationApproach`, `ComplianceAction`
- [ ] Criar features CRUD de políticas
- [ ] Criar feature `GetEffectivePolicy` com lógica de cascata
- [ ] Criar endpoints de configuração de políticas
- [ ] Testes unitários para resolução de política

### Sprint 3 — Changelog

- [ ] Criar entidade `ContractChangelog` + migration
- [ ] Criar feature `GenerateContractChangelog`
- [ ] Criar feature `ApproveContractChangelog`
- [ ] Criar features de query: `ListContractChangelogs`, `GetContractChangelog`, `ExportContractChangelog`
- [ ] Integrar geração de changelog com `VerifyContractCompliance`
- [ ] Testes unitários

### Sprint 4 — CLI

- [ ] Criar `ContractVerifyCommand`
- [ ] Criar `ContractDiffCommand`
- [ ] Criar `ContractChangelogCommand`
- [ ] Criar `ContractSyncCommand`
- [ ] Testes para comandos CLI
- [ ] Documentação dos comandos

### Sprint 5 — Integração CI/CD e Promotion Gates

- [ ] Evoluir `EvaluateContractComplianceGate` no ChangeGovernance
- [ ] Criar novos event types para webhook: `contract.verification.*`
- [ ] Criar job Quartz para drift detection agendado
- [ ] Criar exemplos de integração: GitHub Actions, GitLab CI, Azure DevOps, Jenkins
- [ ] Documentação de integração

### Sprint 6 — Frontend

- [ ] Criar `ContractComplianceDashboard` page
- [ ] Criar `ContractVerificationHistory` page
- [ ] Criar `ContractVerificationDetail` page
- [ ] Criar `ContractChangelogPage`
- [ ] Criar `ContractCompliancePoliciesPage`
- [ ] Criar `ContractCompliancePolicyFormPage`
- [ ] Adicionar chaves i18n (pt-BR, pt-PT, en, es)
- [ ] Integrar com routes existentes

### Sprint 7 — CDCT e Refinamentos

- [ ] Implementar verificação automática de ConsumerExpectations no pipeline
- [ ] Implementar relatório de cobertura de contratos
- [ ] Implementar comparação cross-environment
- [ ] Refinamentos de UX baseados em feedback
- [ ] Testes E2E

---

## 9. Considerações de Segurança

| Aspeto | Consideração |
|--------|--------------|
| **Autenticação CLI** | Token de API com permissão `contracts:verify`, rotação configurável |
| **Rate limiting** | Endpoint de verificação deve ter rate limiting por tenant |
| **Conteúdo do spec** | Validar tamanho máximo (5MB), sanitizar conteúdo antes de processar |
| **Auditoria** | Toda verificação deve ser registada com origem, timestamp e resultado |
| **Tenant isolation** | Verificações e políticas isoladas por tenant |
| **Permissões** | Nova permissão granular `contracts:verify` separada de `contracts:write` |
| **Webhook segurança** | Payloads de webhook assinados com HMAC |

---

## 10. Impacto nos Módulos Existentes

| Módulo | Impacto | Alterações |
|--------|---------|------------|
| **Catalog** | 🔴 Alto | Novas entidades, features, endpoints, migrations |
| **Configuration** | 🟠 Médio | Nova entidade `ContractCompliancePolicy` + features |
| **ChangeGovernance** | 🟡 Médio | Evolução do `EvaluateContractComplianceGate` |
| **Integrations** | 🟢 Baixo | Novos event types para webhook |
| **CLI** | 🟠 Médio | 4 novos comandos |
| **Frontend** | 🟠 Médio | 6 novas páginas + i18n + componentes |

---

## 11. Métricas de Sucesso

| Métrica | Meta | Medição |
|---------|------|---------|
| % de serviços com verificação ativa | >80% em 6 meses | Dashboard de compliance |
| % de builds com verificação | >90% em 3 meses | Contagem de `ContractVerification` |
| Tempo médio de deteção de divergência | <24h | Diferença entre commit e verificação |
| Taxa de conformidade | >95% | Verificações Pass / Total |
| Changelogs gerados automaticamente | >90% | Changelogs com `Source=Verification` |
| Drift score médio em produção | <0.05 | Média de drift scores |

---

## 12. Riscos e Mitigações

| Risco | Probabilidade | Impacto | Mitigação |
|-------|:------------:|:-------:|-----------|
| Resistência das equipas à adoção | Alta | Alto | Começar em modo `Passive`, migrar gradualmente |
| Falsos positivos na comparação | Média | Médio | Algoritmo de diff robusto, whitelist de diferenças aceitáveis |
| Performance do endpoint de verificação | Média | Médio | Cache de versões locked, processamento assíncrono para specs grandes |
| Specs desatualizados no repositório | Alta | Alto | Abordagem B (extração automática) como alternativa |
| Complexidade de multi-spec por serviço | Baixa | Médio | Suportar manifesto com múltiplos specs |

---

## 13. Glossário

| Termo | Definição |
|-------|-----------|
| **Spec File** | Ficheiro de especificação do contrato (OpenAPI, WSDL, AsyncAPI, etc.) |
| **Drift** | Divergência entre contrato declarado e comportamento real em runtime |
| **Compliance Gate** | Portão de qualidade que avalia conformidade antes de promoção |
| **CDCT** | Consumer-Driven Contract Testing — verificação baseada em expectativas de consumidores |
| **Breaking Change** | Alteração incompatível com consumidores existentes |
| **Verification** | Ato de comparar spec do código com versão aprovada no NexTraceOne |
| **Policy Cascade** | Resolução de política por especificidade: Service > Team > Environment > Organization |
| **Changelog** | Registo estruturado de alterações entre versões do contrato |
| **Source of Truth** | O NexTraceOne como referência autoritativa para contratos |

---

> **Próximo passo:** Validar este plano com os stakeholders e priorizar as fases de implementação conforme capacidade da equipa e urgência dos gaps identificados.
