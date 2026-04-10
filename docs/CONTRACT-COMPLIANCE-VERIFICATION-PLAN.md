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

---

## 14. Estado da Implementação (2026-04-10)

### 14.1 Visão Geral

A implementação inicial (Sprints 1–4 do plano) foi executada, criando a fundação completa da funcionalidade de verificação de conformidade. Total: **52 ficheiros**, **~4.500 linhas de código de produção**, **880 linhas de testes** (33 testes unitários).

### 14.2 Inventário de Artefactos Implementados

#### Camada de Domínio — Catalog

| Artefacto | Ficheiro | Linhas | Estado |
|-----------|----------|:------:|:------:|
| `ContractVerification` entity | `Catalog.Domain/Contracts/Entities/ContractVerification.cs` | 155 | ✅ Completo |
| `ContractVerificationId` strongly-typed ID | `Catalog.Domain/Contracts/Entities/ContractVerificationId.cs` | — | ✅ Completo |
| `ContractChangelog` entity | `Catalog.Domain/Contracts/Entities/ContractChangelog.cs` | 151 | ✅ Completo |
| `ContractChangelogId` strongly-typed ID | `Catalog.Domain/Contracts/Entities/ContractChangelogId.cs` | — | ✅ Completo |
| `VerificationStatus` enum | `Catalog.Domain/Contracts/Enums/VerificationStatus.cs` | — | ✅ Completo |
| `ChangelogSource` enum | `Catalog.Domain/Contracts/Enums/ChangelogSource.cs` | — | ✅ Completo |
| `ChangelogFormat` enum | `Catalog.Domain/Contracts/Enums/ChangelogFormat.cs` | — | ✅ Completo |
| `ContractsErrors` (5 novos erros) | `Catalog.Domain/Contracts/Errors/ContractsErrors.cs` | — | ✅ Completo |

#### Camada de Domínio — Configuration

| Artefacto | Ficheiro | Linhas | Estado |
|-----------|----------|:------:|:------:|
| `ContractCompliancePolicy` entity | `Configuration.Domain/Entities/ContractCompliancePolicy.cs` | 265 | ✅ Completo |
| `ContractCompliancePolicyId` strongly-typed ID | `Configuration.Domain/Entities/ContractCompliancePolicyId.cs` | — | ✅ Completo |
| `VerificationMode` enum | `Configuration.Domain/Enums/VerificationMode.cs` | — | ✅ Completo |
| `VerificationApproach` enum | `Configuration.Domain/Enums/VerificationApproach.cs` | — | ✅ Completo |
| `ComplianceAction` enum | `Configuration.Domain/Enums/ComplianceAction.cs` | — | ✅ Completo |
| `PolicyScope` enum | `Configuration.Domain/Enums/PolicyScope.cs` | — | ✅ Completo |

#### Camada de Aplicação — Catalog (7 Features VSA)

| Feature | Tipo | Ficheiro | Estado |
|---------|------|----------|:------:|
| `VerifyContractCompliance` | Command | 310 linhas, spec diff, SHA-256 hashing, operation extraction | ✅ Completo |
| `ListContractVerifications` | Query | Paginação por service/apiAsset | ✅ Completo |
| `GetContractVerificationDetail` | Query | Detalhe por ID | ✅ Completo |
| `GenerateContractChangelog` | Command | Criação de changelog | ✅ Completo |
| `ApproveContractChangelog` | Command | Workflow de aprovação | ✅ Completo |
| `ListContractChangelogs` | Query | Filtro por apiAsset e pendingApproval | ✅ Completo |
| `GetContractChangelog` | Query | Detalhe por ID | ✅ Completo |

#### Camada de Aplicação — Configuration (4 Features VSA)

| Feature | Tipo | Estado |
|---------|------|:------:|
| `CreateContractCompliancePolicy` | Command | ✅ Completo |
| `ListContractCompliancePolicies` | Query | ✅ Completo |
| `GetEffectiveCompliancePolicy` | Query (cascata Service→Team→Env→Org) | ✅ Completo |
| `DeleteContractCompliancePolicy` | Command | ✅ Completo |

#### Camada de Infraestrutura

| Artefacto | Tabela PostgreSQL | Índices | Estado |
|-----------|-------------------|:-------:|:------:|
| `ContractVerificationConfiguration` | `ctr_contract_verifications` | 5 (TenantId, ApiAssetId, ServiceName, Status, composto) | ✅ Completo |
| `ContractChangelogConfiguration` | `ctr_contract_changelogs` | 4 (TenantId, ApiAssetId, IsApproved, composto) | ✅ Completo |
| `ContractCompliancePolicyConfiguration` | `cfg_contract_compliance_policies` | 4 (TenantId, Scope, IsActive, composto) | ✅ Completo |
| `ContractVerificationRepository` | — | 5 métodos | ✅ Completo |
| `ContractChangelogRepository` | — | 4 métodos | ✅ Completo |
| `ContractCompliancePolicyRepository` | — | 6 métodos | ✅ Completo |
| DI Registration (Catalog) | — | 2 repositórios | ✅ Completo |
| DI Registration (Configuration) | — | 1 repositório | ✅ Completo |
| DbContext (Catalog) | — | 2 DbSets adicionados | ✅ Completo |
| DbContext (Configuration) | — | 1 DbSet adicionado | ✅ Completo |

#### Camada de API (Endpoints)

| Endpoint Module | Rotas | Permissão | Estado |
|-----------------|:-----:|-----------|:------:|
| `ContractVerificationEndpointModule` | 3 (POST, GET list, GET detail) | `contracts:verify`, `contracts:read` | ✅ Completo |
| `ContractChangelogEndpointModule` | 4 (POST create, POST approve, GET list, GET detail) | `contracts:write`, `contracts:read` | ✅ Completo |
| `ContractCompliancePoliciesEndpointModule` | 4 (POST, GET list, GET effective, DELETE) | `config:write`, `config:read` | ✅ Completo |

#### CLI

| Comando | Subcomandos | Estado |
|---------|-------------|:------:|
| `nex contract` | `verify`, `diff`, `changelog`, `sync` | ✅ Completo (531 linhas) |
| Registo em `Program.cs` | — | ✅ Completo |

#### Integração e Cross-cutting

| Artefacto | Estado |
|-----------|:------:|
| `ContractVerificationPassedIntegrationEvent` | ✅ Completo |
| `ContractVerificationFailedIntegrationEvent` | ✅ Completo |
| `ContractChangelogGeneratedIntegrationEvent` | ✅ Completo |
| RLS para `ctr_contract_verifications` | ✅ Completo |
| RLS para `ctr_contract_changelogs` | ✅ Completo |
| RLS para `cfg_contract_compliance_policies` | ✅ Completo |

#### Testes

| Ficheiro de Teste | Testes | Linhas | Estado |
|-------------------|:------:|:------:|:------:|
| `ContractComplianceVerificationTests.cs` (Catalog) | 23 | 569 | ✅ Criado |
| `ContractCompliancePolicyTests.cs` (Configuration) | 10 | 311 | ✅ Criado |

### 14.3 Validação de Build

| Projeto | Build Status |
|---------|:------------:|
| `Catalog.Domain` | ✅ 0 erros |
| `Catalog.Application` | ✅ 0 erros |
| `Catalog.Infrastructure` | ✅ 0 erros |
| `Catalog.API` | ✅ 0 erros |
| `Configuration.Domain` | ✅ 0 erros |
| `Configuration.Application` | ✅ 0 erros |
| `Configuration.Infrastructure` | ✅ 0 erros |
| `Configuration.API` | ✅ 0 erros |
| `NexTraceOne.CLI` | ✅ 0 erros |
| Testes (Catalog + Configuration) | ⚠️ Pendente `dotnet restore` |

---

## 15. Análise de Dependências e Compatibilidade

### 15.1 Dependências entre Módulos

```
ContractVerification Flow:
┌──────────────────────────────────────────────────────────────┐
│ Catalog.API                                                   │
│   ContractVerificationEndpointModule                          │
│   ContractChangelogEndpointModule                             │
│     ↓ ISender (MediatR)                                       │
│ Catalog.Application                                           │
│   VerifyContractCompliance → IContractVersionRepository       │
│                             → IContractVerificationRepository │
│                             → IContractsUnitOfWork            │
│                             → IDateTimeProvider               │
│                             → ICurrentTenant                  │
│                             → ICurrentUser                    │
│     ↓                                                         │
│ Catalog.Domain                                                │
│   ContractVerification (Entity<TId>)                          │
│   ContractChangelog (Entity<TId>)                              │
│     ↓                                                         │
│ Catalog.Infrastructure                                        │
│   ContractsDbContext (EF Core)                                │
│   ContractVerificationRepository                              │
│   ContractChangelogRepository                                 │
└──────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────┐
│ Configuration.API                                             │
│   ContractCompliancePoliciesEndpointModule                    │
│     ↓ ISender (MediatR)                                       │
│ Configuration.Application                                     │
│   CreateContractCompliancePolicy                              │
│   GetEffectiveCompliancePolicy → IContractCompliancePolicyRepo│
│                                → ICurrentTenant               │
│     ↓                                                         │
│ Configuration.Domain                                          │
│   ContractCompliancePolicy (AuditableEntity<TId>)             │
│     ↓                                                         │
│ Configuration.Infrastructure                                  │
│   ConfigurationDbContext (EF Core)                             │
│   ContractCompliancePolicyRepository                          │
└──────────────────────────────────────────────────────────────┘
```

### 15.2 Compatibilidade com Padrões Existentes

| Padrão | Compatibilidade | Notas |
|--------|:--------------:|-------|
| **VSA (Vertical Slice Architecture)** | ✅ | Todas as 11 features seguem o padrão Command/Query + Validator + Handler + Response num único ficheiro |
| **DDD / Entity<TId>** | ✅ | `ContractVerification` e `ContractChangelog` usam `Entity<TId>` com factory methods e guard clauses |
| **AuditableEntity<TId>** | ✅ | `ContractCompliancePolicy` usa `AuditableEntity<TId>` com `SetCreated/SetUpdated` |
| **Strongly-typed IDs** | ✅ | `ContractVerificationId`, `ContractChangelogId`, `ContractCompliancePolicyId` via `TypedIdBase` |
| **Result<T> pattern** | ✅ | Todos os handlers retornam `Result<Response>` |
| **FluentValidation** | ✅ | Todos os features têm `Validator` nested class |
| **CancellationToken** | ✅ | Todas as operações async recebem `CancellationToken` |
| **Guard clauses** | ✅ | `Guard.Against.Null(request)` em todos os handlers |
| **ICurrentTenant / ICurrentUser** | ✅ | Usados para tenant isolation e auditoria |
| **IDateTimeProvider** | ✅ | Usado em vez de `DateTime.Now` |
| **sealed classes** | ✅ | Todas as classes são `sealed` |
| **File-scoped namespaces** | ✅ | Consistente com o resto do codebase |
| **XML doc comments em PT** | ✅ | Documentação inline em português |
| **Error catalog centralizado** | ✅ | 5 novos erros adicionados a `ContractsErrors` |
| **Integration events** | ✅ | 3 novos events via `IntegrationEventBase` |
| **EndpointModule pattern** | ✅ | Auto-discovered via reflection |
| **EF Configuration** | ✅ | `IEntityTypeConfiguration<T>` com jsonb, HasConversion, índices |

### 15.3 Dependências de Building Blocks Utilizados

| Building Block | Módulo | Uso |
|----------------|--------|-----|
| `Entity<TId>` | Core | Base para `ContractVerification`, `ContractChangelog` |
| `AuditableEntity<TId>` | Core | Base para `ContractCompliancePolicy` |
| `TypedIdBase` | Core | Strongly-typed IDs |
| `ICommand<T>` / `IQuery<T>` | Application | CQRS pattern |
| `ICommandHandler<T,R>` / `IQueryHandler<T,R>` | Application | MediatR handlers |
| `Result<T>` / `Error` | Core | Error handling pattern |
| `Guard` | Core | Guard clauses |
| `IUnitOfWork` | Application | Persistência transacional |
| `ICurrentTenant` / `ICurrentUser` | Security | Multi-tenancy e identidade |
| `IDateTimeProvider` | Core | Abstração temporal |
| `AbstractValidator<T>` | FluentValidation | Validação |
| `IntegrationEventBase` | Application | Eventos cross-module |

---

## 16. Análise de Cobertura de Testes

### 16.1 Testes Unitários Implementados

#### Catalog — ContractComplianceVerificationTests (23 testes)

| Feature | Teste | Cenário |
|---------|-------|---------|
| `VerifyContractCompliance` | Should_Return_Pass_When_Specs_Are_Identical | Specs idênticos → status Pass |
| | Should_Return_Block_When_Breaking_Changes_Detected | Endpoint removido → status Block |
| | Should_Return_Error_When_No_Contract_Found | Contrato inexistente → status Error |
| | Should_Not_Persist_When_DryRun_Is_True | DryRun=true → sem persistência |
| | Should_Persist_Verification_When_Not_DryRun | DryRun=false → persiste verificação |
| | Should_Detect_Removed_Endpoints_As_Breaking | Endpoint removido classificado como breaking |
| | Should_Detect_New_Endpoints_As_Additive | Endpoint novo classificado como additive |
| | Validator_Should_Reject_Empty_ApiAssetId | Validação de input |
| | Validator_Should_Reject_Empty_SpecContent | Validação de input |
| `ListContractVerifications` | Should_Return_Verifications_By_Service | Filtro por serviço |
| | Should_Return_Verifications_By_ApiAsset | Filtro por API asset |
| | Validator_Should_Reject_Invalid_Page | Page < 1 rejeitado |
| `GetContractVerificationDetail` | Should_Return_Detail_When_Found | Retorno por ID |
| | Should_Return_Error_When_Not_Found | Erro quando inexistente |
| `GenerateContractChangelog` | Should_Create_Changelog_Successfully | Criação com sucesso |
| | Validator_Should_Reject_Empty_ApiAssetId | Validação de input |
| `ApproveContractChangelog` | Should_Approve_Changelog | Aprovação com sucesso |
| | Should_Return_Error_When_Changelog_Not_Found | Erro quando inexistente |
| | Should_Return_Error_When_Already_Approved | Erro quando já aprovado |
| `ListContractChangelogs` | Should_Return_Changelogs_By_ApiAsset | Filtro por API asset |
| | Should_Return_Pending_Approval_Only | Filtro pendentes |
| `GetContractChangelog` | Should_Return_Changelog_When_Found | Retorno por ID |
| | Should_Return_Error_When_Not_Found | Erro quando inexistente |

#### Configuration — ContractCompliancePolicyTests (10 testes)

| Feature | Teste | Cenário |
|---------|-------|---------|
| `CreateContractCompliancePolicy` | Should_Create_Policy_Successfully | Criação com sucesso |
| | Validator_Should_Reject_Empty_Name | Validação de input |
| | Should_Return_Unauthorized_When_Not_Authenticated | Segurança |
| `ListContractCompliancePolicies` | Should_Return_Policies_For_Tenant | Isolamento por tenant |
| | Should_Filter_By_Scope | Filtro por scope |
| `GetEffectiveCompliancePolicy` | Should_Resolve_Service_Scope_First | Cascata: serviço primeiro |
| | Should_Fallback_To_Organization_Scope | Cascata: fallback organização |
| | Should_Return_Default_When_No_Policy_Exists | Comportamento default Passive |
| `DeleteContractCompliancePolicy` | Should_Delete_Policy | Eliminação com sucesso |
| | Should_Return_Error_When_Not_Found | Erro quando inexistente |

### 16.2 Gaps de Cobertura de Testes

| Gap | Severidade | Descrição | Recomendação |
|-----|:----------:|-----------|--------------|
| **Testes de integração** | 🔴 Alta | Nenhum teste de integração com PostgreSQL real para verificar queries, configurations e migrations | Adicionar testes Testcontainers para repositórios |
| **CLI ContractCommand** | 🟠 Média | Comando CLI tem 531 linhas sem testes unitários (4 subcomandos) | Testes com mock de HttpClient |
| **EF Configurations** | 🟡 Média | Configurações EF não verificadas contra migration | Gerar migration e testar com Testcontainers |
| **Integration Events** | 🟡 Média | Events declarados mas não há testes de disparo pós-verificação | Verificar que VerifyContractCompliance publica evento |
| **Edge cases do diff** | 🟠 Média | Diff de spec cobre operações REST simples; falta cobertura para WSDL, AsyncAPI, edge cases | Testes com specs reais de cada formato |
| **Policy cascade com dados reais** | 🟡 Média | Cascata testada com mocks; falta teste de integração com dados em PostgreSQL | Teste de integração com seed de políticas |

### 16.3 Cobertura por Módulo (Estimativa)

| Módulo | Features Implementadas | Testes | Cobertura estimada |
|--------|:---------------------:|:------:|:------------------:|
| Catalog (contract verification) | 7 | 23 | ~70% |
| Configuration (compliance policy) | 4 | 10 | ~65% |
| CLI (contract command) | 4 subcomandos | 0 | 0% |

---

## 17. Análise de Segurança e RLS

### 17.1 Row-Level Security (RLS)

| Tabela | RLS em `apply-rls.sql` | Coluna `TenantId` | Estado |
|--------|:---------------------:|:-----------------:|:------:|
| `ctr_contract_verifications` | ✅ Configurado | `TenantId` (string) | ✅ Protegida |
| `ctr_contract_changelogs` | ✅ Configurado | `TenantId` (string) | ✅ Protegida |
| `cfg_contract_compliance_policies` | ✅ Configurado | `TenantId` (string) | ✅ Protegida |

As 3 novas tabelas estão cobertas por RLS no ficheiro `apply-rls.sql`, seguindo o padrão `tenant_isolation` existente.

### 17.2 Autorização nos Endpoints

| Endpoint | Permissão | Padrão |
|----------|-----------|--------|
| `POST /api/v1/contracts/verifications` | `contracts:verify` | ✅ Nova permissão granular |
| `GET /api/v1/contracts/verifications` | `contracts:read` | ✅ Reutiliza permissão existente |
| `GET /api/v1/contracts/verifications/{id}` | `contracts:read` | ✅ Reutiliza permissão existente |
| `POST /api/v1/contracts/changelogs` | `contracts:write` | ✅ Reutiliza permissão existente |
| `POST /api/v1/contracts/changelogs/{id}/approve` | `contracts:write` | ✅ Reutiliza permissão existente |
| `GET /api/v1/contracts/changelogs` | `contracts:read` | ✅ Reutiliza permissão existente |
| `GET /api/v1/contracts/changelogs/{id}` | `contracts:read` | ✅ Reutiliza permissão existente |
| `POST /api/v1/contract-compliance-policies` | `config:write` | ✅ Reutiliza permissão existente |
| `GET /api/v1/contract-compliance-policies` | `config:read` | ✅ Reutiliza permissão existente |
| `GET /api/v1/contract-compliance-policies/effective` | `config:read` | ✅ Reutiliza permissão existente |
| `DELETE /api/v1/contract-compliance-policies/{id}` | `config:write` | ✅ Reutiliza permissão existente |

### 17.3 Considerações de Segurança Verificadas

| Aspeto | Estado | Notas |
|--------|:------:|-------|
| Token de autenticação no CLI | ✅ | Via `--token` ou env `NEXTRACE_TOKEN` |
| Isolamento por tenant nos repositórios | ✅ | `ICurrentTenant` usado em todos os handlers |
| Input validation (FluentValidation) | ✅ | Todos os commands/queries têm validators |
| SpecContent size limit | ⚠️ Pendente | Recomenda-se limite de 5MB no validator |
| Rate limiting no endpoint de verificação | ⚠️ Pendente | Recomenda-se throttling por tenant |
| HMAC signing em webhooks | ⚠️ Pendente | Para futura integração com webhooks |
| Sanitização de SpecContent | ⚠️ Pendente | Validar que não contém payloads maliciosos |

### 17.4 Recomendações de Hardening

1. **Adicionar limite de tamanho no SpecContent** — `RuleFor(x => x.SpecContent).MaximumLength(5_242_880)` (5MB)
2. **Rate limiting** — Configurar middleware de rate limiting no endpoint `POST /api/v1/contracts/verifications`
3. **Auditoria** — Integrar com o módulo AuditCompliance para registar verificações e alterações de política
4. **Sanitização** — Validar formato do SpecContent antes de processar (rejeitar conteúdo que não seja YAML/JSON/XML válido)

---

## 18. Gaps Pendentes e Próximos Passos

### 18.1 Artefactos Ainda Não Implementados

| # | Artefacto | Sprint Planeado | Prioridade | Descrição |
|---|-----------|:--------------:|:----------:|-----------|
| P1 | **EF Core Migration** | Sprint 1 | 🔴 Crítico | As 3 tabelas novas não têm migration EF Core gerado. Necessário `dotnet ef migrations add` para Catalog e Configuration |
| P2 | **UpdateContractCompliancePolicy** feature | Sprint 2 | 🟠 Alto | CRUD incompleto — falta `Update` (plano previa na Fase 2) |
| P3 | **ActivateContractCompliancePolicy** feature | Sprint 2 | 🟠 Alto | Plano previa `Activate`/`Deactivate` como features separadas |
| P4 | **GetContractCompliancePolicy** (by ID) | Sprint 2 | 🟠 Alto | Query individual por ID — plano previa na Fase 2 |
| P5 | **ExportContractChangelog** feature | Sprint 3 | 🟡 Médio | Exportar changelog em Markdown/JSON/HTML |
| P6 | **GetContractChangelogTimeline** feature | Sprint 3 | 🟡 Médio | Timeline visual de alterações |
| P7 | **Integração changelog ↔ verificação** | Sprint 3 | 🟠 Alto | `VerifyContractCompliance` deveria gerar changelog automaticamente quando detecta alterações |
| P8 | **EvaluateContractComplianceGate evolução** | Sprint 5 | 🟠 Alto | Gate no ChangeGovernance deve consultar verificações e políticas |
| P9 | **Quartz Job para drift detection** | Sprint 5 | 🟡 Médio | `ContractDriftDetectionJob` agendado |
| P10 | **Webhook events** | Sprint 5 | 🟡 Médio | `contract.verification.passed`, `contract.verification.failed` |
| P11 | **6 páginas frontend** | Sprint 6 | 🟠 Alto | Dashboard, history, detail, changelog, policies, policy form |
| P12 | **i18n keys** | Sprint 6 | 🟡 Médio | ~30 novas chaves para 4 locales |
| P13 | **CDCT verification** | Sprint 7 | 🟢 Baixo | Consumer-driven contract testing automático |
| P14 | **Relatório de cobertura de contratos** | Sprint 7 | 🟢 Baixo | Relatório executivo de conformidade |
| P15 | **CLI testes unitários** | Sprint 4 | 🟡 Médio | 4 subcomandos sem testes |
| P16 | **Testes de integração com PostgreSQL** | Contínuo | 🟠 Alto | Testcontainers para repositórios e configurations |

### 18.2 Decisões Técnicas Pendentes

| # | Decisão | Contexto | Opções | Recomendação |
|---|---------|----------|--------|--------------|
| D1 | **Formato de armazenamento do diff** | `DiffDetails` é jsonb — definir schema formal | Schema aberto vs schema tipado | Schema tipado com `ChangeEntry[]` para facilitar queries |
| D2 | **Integração com ComputeSemanticDiff** | `VerifyContractCompliance` faz diff simplificado; deveria usar `ComputeSemanticDiff` existente | Duplicação vs reutilização | Reutilizar `ComputeSemanticDiff` e `ClassifyBreakingChanges` |
| D3 | **Publicação de integration events** | Events declarados mas `VerifyContractCompliance` handler não os publica | Pub no handler vs decorator | Publicar no handler após persistência, antes de retornar |
| D4 | **GetEffectivePolicy cross-module** | `VerifyContractCompliance` (Catalog) precisa consultar política (Configuration) | Cross-module query vs contrato | Contrato IContractsConfigModule com GetEffectivePolicy |
| D5 | **Formato JUnit no CLI** | Plano prevê `--format junit` mas não está implementado | Implementar vs remover opção | Implementar para compatibilidade com GitLab CI |
| D6 | **CLI format branch morto** | `RenderResult` retorna sempre JSON independente do formato; branch non-JSON chama método correto mas é dead code | Corrigir vs remover | Corrigir para chamar `RenderVerificationText` quando `format != json` |
| D7 | **NonBreakingChanges cálculo** | O cálculo de non-breaking changes subtrai novos endpoints do total, mas não identifica operações modificadas de forma compatível | Simplificação vs precisão | Comparar endpoints comuns para detetar modificações reais |
| D8 | **CreatePolicy dupla chamada** | `CreateContractCompliancePolicy` cria entidade e imediatamente faz Update com os mesmos dados + configuração adicional | Factory method completo vs update | Enriquecer o factory method para aceitar todos os parâmetros |

### 18.3 Roadmap de Próximas Ações (Ordenado por Prioridade)

```
Prioridade 1 — Fundação completa (necessário para MVP funcional)
├── P1: Gerar migrations EF Core (Catalog + Configuration)
├── P7: Integrar geração de changelog automática no VerifyContractCompliance
├── D2: Reutilizar ComputeSemanticDiff no handler de verificação
├── D3: Publicar integration events após verificação
├── D6: Corrigir CLI format branch (RenderVerificationText)
├── D7: Corrigir cálculo de NonBreakingChanges
└── D8: Simplificar CreateContractCompliancePolicy (factory completo)

Prioridade 2 — CRUD completo e cross-module
├── P2: UpdateContractCompliancePolicy
├── P3: ActivateContractCompliancePolicy / DeactivateContractCompliancePolicy
├── P4: GetContractCompliancePolicy (by ID)
├── D4: Contrato cross-module IContractsConfigModule
└── P8: Evolução do EvaluateContractComplianceGate

Prioridade 3 — Qualidade e testes
├── P15: Testes unitários para CLI commands
├── P16: Testes de integração com Testcontainers
└── Hardening de segurança (size limit, rate limiting)

Prioridade 4 — UX e frontend
├── P11: 6 páginas frontend
├── P12: i18n keys
└── P5/P6: Export e timeline de changelog

Prioridade 5 — Capacidades avançadas
├── P9: Quartz Job para drift detection
├── P10: Webhook events
├── P13: CDCT verification
└── P14: Relatório de cobertura
```

---

## 19. Análise de Impacto Detalhada nos Módulos Existentes

### 19.1 Módulo Catalog — Impacto Real

| Componente | Ficheiros Alterados | Ficheiros Novos | Natureza da Alteração |
|------------|:-------------------:|:---------------:|----------------------|
| Domain/Entities | 0 | 4 (2 entities + 2 IDs) | Adição pura, sem breaking changes |
| Domain/Enums | 0 | 3 | Adição pura |
| Domain/Errors | 1 (`ContractsErrors.cs`) | 0 | 5 novos métodos adicionados (append) |
| Application/Abstractions | 0 | 2 (2 interfaces) | Adição pura |
| Application/Features | 0 | 7 (7 novos features) | Adição pura |
| Infrastructure/Repositories | 0 | 2 | Adição pura |
| Infrastructure/Configurations | 0 | 2 | Adição pura |
| Infrastructure/DbContext | 1 (`ContractsDbContext.cs`) | 0 | 2 DbSets adicionados |
| Infrastructure/DI | 1 (`DependencyInjection.cs`) | 0 | 2 registos scoped adicionados |
| API/Endpoints | 0 | 2 | Adição pura, auto-discovered |
| Contracts/IntegrationEvents | 1 (`CatalogIntegrationEvents.cs`) | 0 | 3 records adicionados (append) |

**Risco de regressão: BAIXO** — Apenas 4 ficheiros existentes foram alterados (todos por append), nenhuma lógica existente foi modificada.

### 19.2 Módulo Configuration — Impacto Real

| Componente | Ficheiros Alterados | Ficheiros Novos | Natureza da Alteração |
|------------|:-------------------:|:---------------:|----------------------|
| Domain/Entities | 0 | 2 (1 entity + 1 ID) | Adição pura |
| Domain/Enums | 0 | 4 | Adição pura |
| Application/Abstractions | 0 | 1 | Adição pura |
| Application/Features | 0 | 4 | Adição pura |
| Infrastructure/Repositories | 0 | 1 | Adição pura |
| Infrastructure/Configurations | 0 | 1 | Adição pura |
| Infrastructure/DbContext | 1 (`ConfigurationDbContext.cs`) | 0 | 1 DbSet adicionado |
| Infrastructure/DI | 1 (`DependencyInjection.cs`) | 0 | 1 registo scoped adicionado |
| API/Endpoints | 0 | 1 | Adição pura, auto-discovered |

**Risco de regressão: MUITO BAIXO** — Apenas 2 ficheiros existentes foram alterados (ambos por append).

### 19.3 Módulo CLI — Impacto Real

| Componente | Ficheiros Alterados | Ficheiros Novos | Natureza da Alteração |
|------------|:-------------------:|:---------------:|----------------------|
| Commands | 0 | 1 (`ContractCommand.cs`) | Adição pura |
| Program.cs | 1 | 0 | 1 linha adicionada (`rootCommand.Add(ContractCommand.Create())`) |

**Risco de regressão: MUITO BAIXO**

### 19.4 Infraestrutura — Impacto Real

| Componente | Ficheiros Alterados | Ficheiros Novos | Natureza da Alteração |
|------------|:-------------------:|:---------------:|----------------------|
| `apply-rls.sql` | 1 | 0 | 3 blocos RLS adicionados (append) |

**Risco de regressão: NENHUM** — Append-only.

### 19.5 Impacto em Módulos NÃO Alterados

| Módulo | Impacto Direto | Impacto Futuro (planeado) |
|--------|:--------------:|:-------------------------:|
| ChangeGovernance | Nenhum | Sprint 5 — evolução do `EvaluateContractComplianceGate` |
| Integrations | Nenhum | Sprint 5 — novos webhook event types |
| Notifications | Nenhum | Sprint 5 — notificações de verificação |
| IdentityAccess | Nenhum | Nova permissão `contracts:verify` a registar |
| Knowledge | Nenhum | — |
| OperationalIntelligence | Nenhum | — |
| AIKnowledge | Nenhum | — |
| AuditCompliance | Nenhum | Integração de auditoria de verificações |
| Governance | Nenhum | — |
| ProductAnalytics | Nenhum | — |

---

## 20. Recomendações de Priorização Refinada

### 20.1 MVP Funcional — O que falta para ser usável

Para que a funcionalidade de verificação de contratos seja **usável em produção**, são necessários estes passos mínimos:

| # | Ação | Esforço | Justificação |
|---|------|:-------:|--------------|
| 1 | Gerar migrations EF Core | Baixo | Sem migration, as tabelas não existem em PostgreSQL |
| 2 | Integrar `ComputeSemanticDiff` existente | Médio | Diff atual é simplificado; reutilizar a lógica já robusta |
| 3 | Publicar integration events no handler | Baixo | Events já declarados, falta a invocação |
| 4 | Gerar changelog automático na verificação | Médio | Valor central: verificar + documentar alterações |
| 5 | Testes de integração com PostgreSQL | Médio | Validar que o fluxo funciona end-to-end |
| 6 | Registar permissão `contracts:verify` | Baixo | Sem permissão, endpoint retorna 403 |

### 20.2 Comparação: Plano Original vs Estado Atual

| Sprint | Plano | Estado | Completude |
|--------|-------|--------|:----------:|
| **Sprint 1** — Fundação Backend | Entidades, features, endpoints, testes | Implementado (falta migration) | 🟡 90% |
| **Sprint 2** — Configuração e Políticas | CRUD completo + cascata | Implementado (falta Update, Activate/Deactivate, Get by ID) | 🟡 70% |
| **Sprint 3** — Changelog | Entidades, features, geração automática | Implementado (falta export, timeline, integração com verificação) | 🟡 65% |
| **Sprint 4** — CLI | 4 comandos + testes | Implementado (falta testes, formato JUnit) | 🟡 75% |
| **Sprint 5** — CI/CD e Promotion Gates | Gates, webhooks, Quartz job, exemplos CI/CD | Documentação de CI/CD pronta; código pendente | 🔴 20% |
| **Sprint 6** — Frontend | 6 páginas + i18n | Não iniciado | 🔴 0% |
| **Sprint 7** — CDCT e Refinamentos | CDCT, relatórios, cross-env | Não iniciado | 🔴 0% |

### 20.3 Conclusão

A implementação inicial cobriu **~60% do plano total** em termos de backend/CLI, com foco nas camadas de domínio, aplicação, infraestrutura e API. O código segue todos os padrões arquiteturais do NexTraceOne (DDD, VSA, Clean Architecture, strongly-typed IDs, Result pattern, RLS, multi-tenancy).

**Prioridade imediata:** Gerar migrations, integrar ComputeSemanticDiff, publicar events e completar CRUD de políticas. Estas ações transformam a fundação implementada num MVP funcional de verificação de contratos.

---

> **Última atualização:** 2026-04-10  
> **Próximo passo:** Gerar migrations EF Core para as 3 novas tabelas e completar a integração do diff semântico com o fluxo de verificação.
