# 🏗️ Service Creation Studio — Plano de Correções e Novas Funcionalidades

> **Data**: 2026-04-05  
> **Módulos Impactados**: Catalog (Templates), AIKnowledge (Orchestration), Governance, Integrations  
> **Objetivo Principal**: Acelerar desenvolvimento com governança, segurança, auditoria e testabilidade  
> **Princípio**: Velocidade SEM comprometer qualidade — mitigar erros antes de chegarem a produção  

---

## Índice

1. [Resumo Executivo](#1-resumo-executivo)
2. [Estado Atual — O que Já Existe](#2-estado-atual--o-que-já-existe)
3. [Correções Pendentes (Bugs e Gaps)](#3-correções-pendentes-bugs-e-gaps)
4. [Fase 1: Template Manifest Semântico Rico](#4-fase-1-template-manifest-semântico-rico)
5. [Fase 2: Dependency Health & SBOM](#5-fase-2-dependency-health--sbom)
6. [Fase 3: Security Gate Pipeline](#6-fase-3-security-gate-pipeline)
7. [Fase 4: Contract-to-Code Pipeline](#7-fase-4-contract-to-code-pipeline)
8. [Fase 5: Preview, Export & Catalog Registration](#8-fase-5-preview-export--catalog-registration)
9. [Fase 6: AI-Powered Quality Gates](#9-fase-6-ai-powered-quality-gates)
10. [Fase 7: Funcionalidades de Aceleração com Segurança](#10-fase-7-funcionalidades-de-aceleração-com-segurança)
11. [Pesquisa de Mercado — Features Adicionais](#11-pesquisa-de-mercado--features-adicionais)
12. [Cronograma de Execução](#12-cronograma-de-execução)
13. [Métricas de Sucesso](#13-métricas-de-sucesso)
14. [Definição de Pronto (DoD)](#14-definição-de-pronto-dod)

---

## 1. Resumo Executivo

O NexTraceOne já possui infraestrutura sólida de templates (`ServiceTemplate`), IA (`GenerateAiScaffold`, 9 agentes especializados), geração de testes (`GenerateTestScenarios`, `GenerateRobotFrameworkDraft`) e governança (`PolicyAsCodeDefinition`, `RulesetGovernance`).

**O gap principal** está na **orquestração inteligente** entre estas peças e na **ausência de funcionalidades de supply chain security** (SBOM, dependency health, vulnerability scanning).

### Visão Final

```
Template Selection → AI Customization → Contract Generation → Code Scaffolding
    → Security Scan → Test Generation → Governance Validation
    → Dependency Audit → Preview & Export → Catalog Registration
```

Cada serviço criado pelo NexTraceOne nasce:
- ✅ Com contrato OpenAPI/AsyncAPI governado
- ✅ Com código gerado e validado por IA
- ✅ Com testes unitários e de contrato
- ✅ Com SBOM (Software Bill of Materials)
- ✅ Com dependências auditadas e sem vulnerabilidades conhecidas
- ✅ Com security scan automático (SAST)
- ✅ Com políticas de governança aplicadas
- ✅ Registado automaticamente no Service Catalog

---

## 2. Estado Atual — O que Já Existe

### ✅ Completo

| Componente | Localização | Estado |
|---|---|---|
| **ServiceTemplate** (Domain) | `Catalog.Domain/Templates/Entities/ServiceTemplate.cs` | Slug, Version, Language, ServiceType, Tags, GovernancePolicyIds, BaseContractSpec, ScaffoldingManifestJson |
| **CRUD Templates** (7 Features) | `Catalog.Application/Templates/Features/` | Create, Get, List, Update, Activate, Deactivate, Scaffold |
| **ScaffoldServiceFromTemplate** | `Catalog.Application/Templates/Features/ScaffoldServiceFromTemplate/` | Substituição de variáveis `{{ServiceName}}`, `{{Domain}}`, `{{TeamName}}` |
| **GenerateAiScaffold** | `AIKnowledge.Application/Orchestration/Features/GenerateAiScaffold/` | IA gera controllers, DTOs, domain, tests, infra baseado em prompt |
| **ICatalogTemplatesModule** | `Catalog.Contracts/Templates/ServiceInterfaces/` | Contrato inter-módulo Catalog→AIKnowledge |
| **9 AI Agents** | `AIKnowledge.Domain/Governance/Entities/DefaultAgentCatalog.cs` | service-analyst, contract-designer, change-advisor, incident-responder, test-generator, docs-assistant, security-reviewer, event-designer, service-scaffold-agent |
| **GenerateTestScenarios** | `AIKnowledge.Application/Orchestration/Features/GenerateTestScenarios/` | Gera cenários de teste a partir de contratos |
| **GenerateRobotFrameworkDraft** | `AIKnowledge.Application/Orchestration/Features/GenerateRobotFrameworkDraft/` | Gera código Robot Framework |
| **PolicyAsCodeDefinition** | `Governance.Domain/Entities/PolicyAsCodeDefinition.cs` | Políticas como código (Rego/OPA style) |
| **Frontend Pages** | `frontend/src/features/catalog/pages/` | TemplateLibraryPage, TemplateDetailPage, TemplateEditorPage, AiScaffoldWizardPage |
| **ZIP Download** | `AiScaffoldWizardPage.tsx` | JSZip 3.10.1 para download do scaffold |

### 🔶 Parcial

| Componente | Estado | Gap |
|---|---|---|
| Dependency Tracking | DependencyMetricsRepository existe para inter-serviços | Falta tracking de pacotes/bibliotecas (NuGet, npm, Maven) |
| Security Audit | SecurityAuditRecorder para auth events | Falta SAST/DAST scanning de código gerado |
| Contract-to-Code | GenerateCode no Portal gera SDK/clients | Falta pipeline unificado contrato→servidor |

### 🔴 Não Existe

| Funcionalidade | Impacto |
|---|---|
| **SBOM (Software Bill of Materials)** | Não há registo de dependências por serviço |
| **Vulnerability Scanning de Dependências** | Não verifica CVEs em NuGet/npm/Maven |
| **SAST (Static Analysis Security Testing)** | Não analisa código gerado para vulnerabilidades |
| **Dependency Health Dashboard** | Não mostra saúde/licenças/vulnerabilidades por template |
| **Template Marketplace** | Não há partilha entre organizações |
| **Live Template Sync** | Quando template evolui, serviços gerados não são notificados |
| **Environment Blueprint** | Templates não incluem Docker/Helm/IaC |

---

## 3. Correções Pendentes (Bugs e Gaps)

### BUG-01: RegisterServiceAsset aceita apenas 3 campos ⚠️ CRÍTICO

**Problema**: Frontend coleta 11 campos, API client envia 3, backend aceita 3.

**Correção**:
- [ ] Expandir `RegisterServiceAsset.Command` para todos os 11 campos
- [ ] Atualizar Handler para persistir campos adicionais
- [ ] Atualizar Validator com regras para campos opcionais
- [ ] Atualizar `serviceCatalog.ts:registerService()` para enviar todos os campos
- [ ] Atualizar testes unitários

### BUG-02: Enum Framework ausente no backend ⚠️ ALTO

**Problema**: Frontend oferece "Framework / SDK" mas `ServiceType` enum não tem `Framework`.

**Correção**:
- [ ] Adicionar `Framework = 20` ao enum `ServiceType`
- [ ] Criar migration PostgreSQL
- [ ] Atualizar testes de serialização

### BUG-03: Campo domain não enviado pelo API client ⚠️ ALTO

**Correção**:
- [ ] Adicionar `domain` ao tipo TypeScript e payload

### BUG-04: SchemaPropertyEditor $ref sem validação ⚠️ MÉDIO

**Correção**:
- [ ] Autocomplete dropdown com canonical entities
- [ ] Validação de existência do $ref
- [ ] Aviso para entidades deprecated

### GAP-01: Outbox processing incompleto ⚠️ CRÍTICO

**Problema**: 23 DbContexts têm tabelas de outbox sem processamento ativo.

**Correção**:
- [ ] Implementar OutboxProcessor genérico para todos os DbContexts
- [ ] Registar em background service no ApiHost
- [ ] Monitorizar processamento com métricas

---

## 4. Fase 1: Template Manifest Semântico Rico

### Objetivo
Evoluir o `ScaffoldingManifestJson` de ficheiros estáticos com variáveis para um **schema rico** que descreva arquitectura, entidades, rotas, pacotes e dependências.

### Novo Schema: `TemplateManifestV2`

```json
{
  "version": "2.0",
  "architecture": {
    "pattern": "DDD-CleanArchitecture",
    "layers": ["API", "Application", "Domain", "Infrastructure"],
    "conventions": {
      "namingStyle": "PascalCase",
      "folderStructure": "FeatureSlice"
    }
  },
  "packages": {
    "required": [
      { "name": "MediatR", "version": ">=12.0", "ecosystem": "nuget" },
      { "name": "FluentValidation", "version": ">=11.0", "ecosystem": "nuget" }
    ],
    "optional": [
      { "name": "Serilog", "version": ">=3.0", "ecosystem": "nuget", "category": "logging" }
    ]
  },
  "entities": {
    "expandable": true,
    "base": [
      { "name": "{{EntityName}}", "properties": ["Id", "CreatedAt", "UpdatedAt"] }
    ]
  },
  "endpoints": {
    "expandable": true,
    "base": [
      { "method": "GET", "path": "/api/v1/{{resource}}", "operation": "List" },
      { "method": "GET", "path": "/api/v1/{{resource}}/{id}", "operation": "GetById" },
      { "method": "POST", "path": "/api/v1/{{resource}}", "operation": "Create" },
      { "method": "PUT", "path": "/api/v1/{{resource}}/{id}", "operation": "Update" },
      { "method": "DELETE", "path": "/api/v1/{{resource}}/{id}", "operation": "Delete" }
    ]
  },
  "infrastructure": {
    "dockerfile": true,
    "dockerCompose": true,
    "ciPipeline": { "provider": "github-actions" },
    "healthCheck": true
  },
  "governance": {
    "requiredPolicies": ["naming-convention", "api-versioning", "error-handling"],
    "securityScanOnGenerate": true,
    "testCoverageMinimum": 80
  }
}
```

### Tarefas de Implementação

- [ ] **Domain**: Criar `TemplateManifestV2` value object com parsing e validação
- [ ] **Domain**: Adicionar propriedade `ArchitecturePatternJson` a `ServiceTemplate`
- [ ] **Application**: Evoluir `ScaffoldServiceFromTemplate` para interpretar ManifestV2
- [ ] **Application**: `GenerateAiScaffold` recebe ManifestV2 como contexto adicional
- [ ] **Frontend**: Evolução do `TemplateEditorPage` com editor visual para ManifestV2
- [ ] **Frontend**: Preview visual da arquitectura (layers, packages, entities)
- [ ] **Migration**: Migration para novo campo em ServiceTemplate
- [ ] **Testes**: 15+ testes para parsing, validação e expansão do manifest

---

## 5. Fase 2: Dependency Health & SBOM

### Objetivo
Implementar **rastreamento completo de dependências** de bibliotecas por serviço/template, com detecção de vulnerabilidades, licenças e saúde.

### 5.1 Novo Domínio: `DependencyGovernance`

**Localização**: `Catalog.Domain/DependencyGovernance/` (sub-domínio do Catalog)

#### Entidades

```
ServiceDependencyProfile (Aggregate Root)
├── ServiceId (Guid)
├── TemplateId (Guid?)
├── Language (TemplateLanguage)
├── LastScanAt (DateTimeOffset)
├── SbomFormat (CycloneDX | SPDX)
├── SbomContent (string) — JSON do SBOM completo
├── HealthScore (int 0-100)
├── TotalDependencies (int)
├── DirectDependencies (int)
├── TransitiveDependencies (int)
└── Dependencies (IReadOnlyList<PackageDependency>)

PackageDependency (Entity)
├── PackageName (string) — ex: "Newtonsoft.Json"
├── Version (string) — ex: "13.0.3"
├── Ecosystem (PackageEcosystem) — NuGet | Npm | Maven | PyPI | Go | Cargo
├── IsDirect (bool) — dependência directa vs transitiva
├── License (string?) — ex: "MIT", "Apache-2.0"
├── LicenseRisk (LicenseRiskLevel) — Low | Medium | High | Critical
├── LatestStableVersion (string?) — última versão estável disponível
├── IsOutdated (bool)
├── DeprecationNotice (string?) — se pacote está deprecated
└── Vulnerabilities (IReadOnlyList<PackageVulnerability>)

PackageVulnerability (Value Object)
├── CveId (string) — ex: "CVE-2024-12345"
├── Severity (VulnerabilitySeverity) — Low | Medium | High | Critical
├── CvssScore (decimal) — 0.0 a 10.0
├── Description (string)
├── AffectedVersionRange (string) — ex: "<13.0.4"
├── FixedInVersion (string?) — ex: "13.0.4"
├── PublishedAt (DateTimeOffset)
├── Source (string) — NVD | GitHub Advisory | OSV
└── ExploitMaturity (ExploitMaturity) — NotDefined | ProofOfConcept | Active
```

#### Features (VSA)

| Feature | Tipo | Descrição |
|---|---|---|
| `ScanServiceDependencies` | Command | Analisa ficheiro de projeto (.csproj, package.json, pom.xml) e extrai dependências |
| `GetServiceDependencyProfile` | Query | Retorna perfil completo de dependências de um serviço |
| `ListVulnerableDependencies` | Query | Lista dependências com CVEs conhecidos (filtros: severity, ecosystem) |
| `GetDependencyHealthDashboard` | Query | Agregação: top vulneráveis, mais desatualizadas, licenças de risco |
| `CheckDependencyPolicies` | Command | Valida dependências contra políticas (licenças proibidas, versões mínimas) |
| `GenerateSbom` | Command | Gera SBOM em formato CycloneDX ou SPDX |
| `CompareDependencyVersions` | Query | Compara versões entre ambientes (dev vs prod) |
| `SuggestDependencyUpgrades` | Query | IA sugere upgrades seguros com análise de breaking changes |
| `DetectLicenseConflicts` | Query | Detecta conflitos de licença (ex: GPL em projeto MIT) |
| `GetTemplateDependencyHealth` | Query | Saúde das dependências por template (agregado) |

### 5.2 Fontes de Dados de Vulnerabilidades

| Fonte | Protocolo | Cobertura |
|---|---|---|
| **GitHub Advisory Database** | REST API (ghsa) | NuGet, npm, Maven, PyPI, Go, Cargo |
| **OSV (Open Source Vulnerabilities)** | REST API | Universal, mantido pelo Google |
| **NVD (National Vulnerability Database)** | REST API v2.0 | CVEs universais (NIST) |
| **NuGet Audit** | Built-in .NET 8+ | NuGet nativo |
| **npm audit** | Built-in npm | npm nativo |

### 5.3 Integração com Templates

Quando um template é usado para scaffolding:
1. Template define `packages.required` e `packages.optional` no ManifestV2
2. Antes de gerar: `CheckDependencyPolicies` valida pacotes contra políticas
3. Após gerar: `ScanServiceDependencies` cria perfil de dependências
4. Dashboard mostra: quais templates geram serviços com dependências vulneráveis

### 5.4 Frontend

- [x] **DependencyDashboardPage**: Visão global de saúde de dependências ✅ `/catalog/dependency-dashboard`
- [ ] **ServiceDependencyTab**: Tab no detalhe do serviço com lista de dependências
- [ ] **VulnerabilityAlertBanner**: Banner em serviços com CVEs críticos
- [x] **LicenseComplianceView**: Visualização de licenças e conflitos ✅ `LicenseCompliancePage` `/catalog/license-compliance`
- [ ] **DependencyGraph (evolução)**: Expandir `DependencyGraph.tsx` existente para incluir pacotes

### 5.5 Tarefas de Implementação

- [x] **Domain**: Criar entidades `ServiceDependencyProfile`, `PackageDependency`, `PackageVulnerability` ✅
- [x] **Domain**: Enums `PackageEcosystem`, `LicenseRiskLevel`, `VulnerabilitySeverity`, `ExploitMaturity` ✅
- [x] **Application**: 10 features VSA (tabela acima) ✅
- [x] **Infrastructure**: `DependencyGovernanceDbContext` + migration ✅
- [ ] **Infrastructure**: Adapters para GitHub Advisory, OSV, NVD APIs
- [ ] **Infrastructure**: Parser de ficheiros de projeto (.csproj, package.json, pom.xml, go.mod, Cargo.toml)
- [x] **API**: 10 endpoints com rate limiting e autorização ✅
- [x] **Frontend**: `DependencyDashboardPage` + `LicenseCompliancePage` ✅ (ServiceDependencyTab + VulnerabilityAlertBanner + DependencyGraph evolução pendentes)
- [ ] **AI Agent**: Novo agente `dependency-advisor` no DefaultAgentCatalog
- [x] **i18n**: Keys para 4 locales ✅
- [x] **Testes**: 30+ testes unitários ✅

---

## 6. Fase 3: Security Gate Pipeline

### Objetivo
Garantir que **todo código gerado** pelo NexTraceOne passa por análise de segurança antes de ser disponibilizado ao developer.

### 6.1 Novo Domínio: `SecurityGate`

**Localização**: `Governance.Domain/SecurityGate/` ou novo sub-módulo em AuditCompliance

#### Conceito: Security Gate como Step Obrigatório

```
[Template + Prompt] → [AI Generate Code] → 🛡️ SECURITY GATE → [Preview] → [Export]
                                                    │
                                          ┌─────────┼─────────┐
                                          │         │         │
                                     SAST Scan  License   Dependency
                                                Check      Audit
```

#### Entidades

```
SecurityScanResult (Aggregate Root)
├── ScanId (Guid)
├── TargetType (ScanTarget) — GeneratedCode | Contract | Template
├── TargetId (Guid) — ScaffoldId, ContractVersionId, etc.
├── ScannedAt (DateTimeOffset)
├── ScanProvider (string) — "internal", "semgrep", "sonarqube"
├── OverallRisk (SecurityRiskLevel) — Clean | Low | Medium | High | Critical
├── PassedGate (bool) — se passou no security gate
├── Findings (IReadOnlyList<SecurityFinding>)
└── Summary (SecurityScanSummary)

SecurityFinding (Entity)
├── FindingId (Guid)
├── RuleId (string) — ex: "CWE-89", "OWASP-A01"
├── Category (SecurityCategory) — Injection | BrokenAuth | XSS | SSRF | Crypto | etc.
├── Severity (FindingSeverity) — Info | Low | Medium | High | Critical
├── FilePath (string) — ficheiro afectado
├── LineNumber (int?)
├── Description (string)
├── Remediation (string) — sugestão de correção
├── CweId (string?) — CWE reference
├── OwaspCategory (string?) — OWASP Top 10 category
├── IsAiGenerated (bool) — se o finding foi detectado por IA
└── Status (FindingStatus) — Open | Acknowledged | Mitigated | FalsePositive

SecurityScanSummary (Value Object)
├── TotalFindings (int)
├── CriticalCount (int)
├── HighCount (int)
├── MediumCount (int)
├── LowCount (int)
├── InfoCount (int)
├── TopCategories (IReadOnlyList<string>)
└── RemediationEstimate (TimeSpan?) — estimativa de tempo para corrigir
```

### 6.2 Features (VSA)

| Feature | Tipo | Descrição |
|---|---|---|
| `ScanGeneratedCode` | Command | Executa análise SAST no código gerado pelo scaffold |
| `ScanContractSecurity` | Command | Analisa contrato OpenAPI para vulnerabilidades (auth, injection patterns) |
| `GetSecurityScanResult` | Query | Retorna resultado de um scan específico |
| `ListSecurityFindings` | Query | Lista findings com filtros (severity, category, status) |
| `AcknowledgeFinding` | Command | Marca finding como acknowledged/false positive |
| `GetSecurityDashboard` | Query | Agregação: trends, top categories, compliance score |
| `EvaluateSecurityGate` | Command | Decide pass/fail baseado em policies (ex: 0 critical, max 3 high) |
| `GenerateSecurityReport` | Query | Relatório PDF/HTML para auditoria |

### 6.3 Análise SAST Interna (sem dependência externa)

Para o MVP, implementar **scanner interno** baseado em regras:

| Categoria | Regras de Detecção |
|---|---|
| **SQL Injection** | Detecção de string concatenation em queries, falta de parameterized queries |
| **XSS** | Saída não sanitizada em templates HTML, falta de encoding |
| **Hardcoded Secrets** | Regex para API keys, passwords, connection strings em código |
| **Insecure Deserialization** | Uso de `BinaryFormatter`, `JavaScriptSerializer` inseguro |
| **Path Traversal** | Acesso a ficheiros com input do utilizador sem validação |
| **Missing Auth** | Endpoints sem `[Authorize]` ou equivalente |
| **Insecure Crypto** | MD5, SHA1 para hashing de passwords, DES/3DES |
| **CORS Misconfiguration** | `AllowAnyOrigin()` sem restrições |
| **Missing Input Validation** | Endpoints sem FluentValidation ou DataAnnotations |
| **Logging Sensitive Data** | PII em mensagens de log |

### 6.4 Integração com AI Security Reviewer

O agente `security-reviewer` existente no DefaultAgentCatalog pode ser invocado como parte do Security Gate:

```
Generated Code → Internal SAST Rules → AI Security Review → Combined SecurityScanResult
```

### 6.5 Tarefas de Implementação

- [ ] **Domain**: Entidades `SecurityScanResult`, `SecurityFinding`, enums
- [ ] **Application**: 8 features VSA
- [ ] **Infrastructure**: `SecurityGateDbContext` + migration
- [ ] **Infrastructure**: `InternalSastScanner` com 10+ regras
- [ ] **Integration**: Integração com agente `security-reviewer`
- [ ] **API**: Endpoints com autorização
- [ ] **Frontend**: Security findings viewer, dashboard, gate status
- [ ] **Testes**: 25+ testes unitários para scanner e features

---

## 7. Fase 4: Contract-to-Code Pipeline

### Objetivo
Pipeline unificado: **Contrato OpenAPI → Código Servidor + Cliente + Testes** — tudo governado e auditado.

### 7.1 Fluxo Completo

```
[OpenAPI Spec] ──┬──→ [Server Stubs] (.NET, Java, Node.js, Go, Python)
                 ├──→ [SDK Clients] (C#, TypeScript, Java, Python)
                 ├──→ [Contract Tests] (Robot Framework, xUnit, Jest)
                 ├──→ [Mock Server] (auto-gerado para frontend)
                 └──→ [Postman Collection] (para testes manuais)
```

### 7.2 Features Necessárias

| Feature | Tipo | Descrição |
|---|---|---|
| `GenerateServerFromContract` | Command | Gera código servidor completo a partir do contrato |
| `GenerateClientSdkFromContract` | Command | Gera SDK client tipado (já parcialmente existe em GenerateCode) |
| `GenerateContractTests` | Command | Gera testes de contrato (integração com GenerateTestScenarios) |
| `GenerateMockServer` | Command | Gera mock server com respostas baseadas nos examples do contrato |
| `GeneratePostmanCollection` | Command | Exporta contrato como Postman Collection v2.1 |
| `OrchestrateContractPipeline` | Command | **Orquestrador**: executa todos os acima em sequência |

### 7.3 Evolução da Feature Existente `GenerateCode`

A feature `GenerateCode` no Portal já gera SDK Clients e Integration Examples. Evolução necessária:

- [ ] Separar geração de **servidor** vs **cliente**
- [ ] Adicionar geração de **mock server** (JSON Server ou WireMock config)
- [ ] Adicionar geração de **Postman Collection**
- [ ] Adicionar **orquestrador** que chama todas as gerações
- [ ] Integrar com Security Gate (Fase 3) após cada geração

### 7.4 Tarefas de Implementação

- [ ] **Application**: 6 features VSA (tabela acima)
- [ ] **Application**: Refactoring de `GenerateCode` para suportar server stubs
- [ ] **Infrastructure**: Templates de mock server por linguagem
- [ ] **Infrastructure**: Conversor OpenAPI → Postman Collection
- [ ] **AI Integration**: `GenerateAiScaffold` aceita contrato como input principal
- [ ] **Frontend**: Pipeline wizard (select contract → choose outputs → generate)
- [ ] **Testes**: 20+ testes unitários

---

## 8. Fase 5: Preview, Export & Catalog Registration

### Objetivo
Depois do código gerado e validado, o developer pode **pré-visualizar**, **exportar** e **registar** automaticamente no catálogo.

### 8.1 Preview Interativo

**Componentes Frontend**:

| Componente | Descrição |
|---|---|
| `FileTreePreview` | Árvore de ficheiros gerados com ícones por tipo |
| `CodePreview` | Editor read-only com syntax highlighting (Monaco Editor) |
| `ContractPreview` | Swagger UI embebido para preview do contrato |
| `DependencyPreview` | Lista de pacotes com health indicators |
| `SecurityPreview` | Resumo de findings do Security Gate |
| `GovernancePreview` | Políticas aplicadas e status de compliance |

### 8.2 Opções de Export

| Método | Descrição | Prioridade |
|---|---|---|
| **Download ZIP** | ✅ Já existe (JSZip) | — |
| **Push to Git** | Push para repositório via integração (GitHub/GitLab/Azure DevOps) | Alta |
| **Create Branch** | Criar branch num repo existente com os ficheiros | Média |
| **Pull Request** | Abrir PR com os ficheiros gerados | Média |

### 8.3 Auto-Registration no Service Catalog

Quando o developer exporta, automaticamente:

1. `RegisterServiceAsset` é chamado com os metadados do template
2. Contrato OpenAPI é versionado via `CreateContractVersion`
3. Dependências são registadas via `ScanServiceDependencies`
4. Equipa e domínio são associados
5. Evento de auditoria é emitido

### 8.4 Tarefas de Implementação

- [ ] **Frontend**: 6 componentes de preview
- [ ] **Frontend**: Integração Monaco Editor para code preview
- [ ] **Backend**: `PushToRepository` feature (via Integrations module)
- [ ] **Backend**: `CreatePullRequestWithScaffold` feature
- [ ] **Backend**: `AutoRegisterScaffoldedService` orchestrator
- [ ] **API**: Endpoints de export e registration
- [ ] **Testes**: 15+ testes

---

## 9. Fase 6: AI-Powered Quality Gates

### Objetivo
IA valida **automaticamente** a qualidade do código gerado antes de disponibilizar ao developer.

### 9.1 Quality Gate Pipeline

```
Generated Code
  ├── 🧪 Test Coverage Gate → IA verifica se testes cobrem happy paths + edge cases
  ├── 🏗️ Architecture Gate → IA verifica se segue DDD/Clean Architecture do template
  ├── 📝 Documentation Gate → IA verifica se XML docs e README existem
  ├── 🛡️ Security Gate → (Fase 3)
  ├── 📦 Dependency Gate → (Fase 2)
  └── ✅ Governance Gate → Políticas as Code do módulo Governance
```

### 9.2 AI Agents para Quality Gates

| Agente | Responsabilidade | Existe? |
|---|---|---|
| `security-reviewer` | Revê código para vulnerabilidades | ✅ Existe |
| `test-generator` | Valida e completa testes | ✅ Existe |
| `architecture-fitness-agent` | **Novo**: Valida conformidade arquitetural | 🔴 Criar |
| `documentation-quality-agent` | **Novo**: Valida completude da documentação | 🔴 Criar |
| `dependency-advisor` | **Novo**: Analisa e recomenda dependências | 🔴 Criar |

### 9.3 Architectural Fitness Functions

Regras automáticas que validam se o código gerado respeita os padrões do template:

| Regra | Verificação |
|---|---|
| **Layer Dependency** | Domain não referencia Infrastructure |
| **Naming Convention** | Classes seguem PascalCase, interfaces com I-prefix |
| **CQRS Separation** | Commands e Queries em classes separadas |
| **Handler Pattern** | Cada feature tem Command/Query + Validator + Handler |
| **DI Registration** | Todos os serviços registados em DependencyInjection.cs |
| **Error Handling** | Uso de `Result<T>` em vez de exceptions para fluxo normal |
| **Logging Pattern** | Structured logging com categorias corretas |
| **Validation Pattern** | FluentValidation para todos os Commands |

### 9.4 Tarefas de Implementação

- [ ] **Domain**: `QualityGateResult`, `ArchitecturalFitnessFinding` entities
- [ ] **Application**: Features para cada gate
- [ ] **AI Agents**: 3 novos agentes no DefaultAgentCatalog
- [ ] **Infrastructure**: Implementação das fitness functions
- [ ] **Frontend**: Quality Gate results viewer (pass/fail por categoria)
- [ ] **Testes**: 20+ testes

---

## 10. Fase 7: Funcionalidades de Aceleração com Segurança

### 10.1 Instant Mock Server 🆕

**Conceito**: Ao criar um contrato OpenAPI, gerar automaticamente um mock server que responde com dados realistas baseados nos schemas e examples.

**Impacto**: Frontend team fica desbloqueado imediatamente — não precisa esperar backend ficar pronto.

**Implementação**:
- [ ] Feature `GenerateMockServerConfig` que produz configuração WireMock/JSON Server
- [ ] Mock data gerado via IA baseado nos schemas
- [ ] Endpoint para servir mocks em runtime (opcional)
- [ ] UI para ver/testar endpoints do mock

### 10.2 ADR (Architecture Decision Record) Generator 🆕

**Conceito**: Ao criar um serviço via scaffold, a IA gera automaticamente ADRs documentando decisões de arquitectura.

**Impacto**: Decisões são documentadas desde o início — não ficam perdidas em conversas.

**Implementação**:
- [ ] Feature `GenerateArchitectureDecisionRecord`
- [ ] Template de ADR seguindo formato padrão (título, contexto, decisão, consequências)
- [ ] Integração com Knowledge Hub do NexTraceOne
- [ ] Inclusão no ZIP/repo gerado

### 10.3 Environment Blueprint 🆕

**Conceito**: Template inclui definição de infraestrutura (Dockerfile, docker-compose, Helm chart básico).

**Impacto**: Serviço é deployável imediatamente após scaffold — reduz time-to-first-deploy de dias para minutos.

**Implementação**:
- [ ] Campos no ManifestV2 para infraestrutura
- [ ] Templates de Dockerfile por linguagem
- [ ] Templates de docker-compose com database, cache, etc.
- [ ] Templates de Helm chart básico
- [ ] CI/CD pipeline templates (GitHub Actions, GitLab CI, Azure DevOps)

### 10.4 Developer Onboarding Path 🆕

**Conceito**: Guia interativo para novos developers: setup → primeiro serviço → primeiro deploy.

**Impacto**: Reduz onboarding time de semanas para horas.

**Implementação**:
- [ ] Feature `GetOnboardingPath` com steps personalizados por role
- [ ] Checklist interativo no frontend
- [ ] Integração com templates (sugerir primeiro template)
- [ ] Métricas de progresso do onboarding

### 10.5 Smart Template Recommendations 🆕

**Conceito**: Ao criar um serviço, a IA recomenda o template mais adequado baseado na descrição.

**Impacto**: Developer não precisa conhecer todos os templates — a IA guia para o melhor caminho.

**Implementação**:
- [ ] Feature `RecommendTemplateForService`
- [ ] IA analisa descrição + domínio + linguagem preferida
- [ ] Ranking com justificação
- [ ] Integração no Step 1 do wizard

### 10.6 Pre-Commit Governance Check 🆕

**Conceito**: Antes de cada export/push, validar automaticamente contra políticas de governança.

**Impacto**: Erros de governança são detectados antes de chegarem ao repositório.

**Implementação**:
- [ ] Integração com `PolicyAsCodeDefinition` (já existe)
- [ ] Validação de naming conventions, versioning, contract compliance
- [ ] Report de violações com sugestões de correção
- [ ] Opção de auto-fix para violações simples

### 10.7 Change Impact Preview 🆕

**Conceito**: Antes de criar um novo serviço, mostrar o impacto no grafo de dependências do ecossistema.

**Impacto**: Previne criação de serviços duplicados ou com dependências circulares.

**Implementação**:
- [ ] Feature `PreviewServiceImpact`
- [ ] Análise de topology graph existente
- [ ] Detecção de serviços similares (IA)
- [ ] Visualização de dependências previstas

### 10.8 Compliance as Code — Shift Left 🆕

**Conceito**: Políticas de compliance (PCI-DSS, SOC2, HIPAA) são verificadas **durante a geração**, não após.

**Impacto**: Serviços nascem compliant — não precisam de remediação posterior.

**Implementação**:
- [ ] Mapeamento de categorias de compliance para templates
- [ ] Validação automática de requisitos por tipo de dados (PII, financeiro, saúde)
- [ ] Geração automática de controles (logging, encryption, access control)
- [ ] Report de compliance pré-deploy

---

## 11. Pesquisa de Mercado — Features Adicionais

### 11.1 Plataformas de Referência Pesquisadas

| Plataforma | Diferencial Relevante | Aplicação no NexTraceOne |
|---|---|---|
| **Backstage (Spotify/CNCF)** | Golden Paths — templates que guiam pelo caminho correto | TemplateManifestV2 com governance built-in |
| **Amplication** | Live Templates — quando template muda, serviços são re-sincronizados | Live Template Sync (Fase futura) |
| **GitHub Copilot Agents** | Agent mode — IA planeia e executa autonomamente | GenerateAiScaffold já faz isto; expandir com Quality Gates |
| **gpt-engineer** | Prompt → código completo com identidade configurável | service-scaffold-agent com system prompt especializado |
| **Snyk** | SCA (Software Composition Analysis) com fix automático | Dependency Health (Fase 2) com SuggestDependencyUpgrades |
| **SonarQube** | Quality Gates com thresholds configuráveis | AI Quality Gates (Fase 6) |
| **Semgrep** | SAST com regras customizáveis | Internal SAST Scanner (Fase 3) |
| **Dependabot** | Auto-update de dependências com PRs automáticos | SuggestDependencyUpgrades + PushToRepository |
| **OWASP Dependency-Check** | CVE scanning para múltiplos ecossistemas | Vulnerability scanning (Fase 2) |
| **CycloneDX** | Standard SBOM com suporte a VEX | GenerateSbom feature (Fase 2) |
| **Goa Framework** | Design-first — zero drift entre contrato e código | Contract-to-Code Pipeline (Fase 4) |
| **Stoplight Studio** | Visual API designer drag-and-drop | Visual Contract Designer (já existe no Contract Studio) |
| **Terraform/Pulumi** | Infrastructure as Code em templates | Environment Blueprint (Fase 7.3) |
| **Waypoint (HashiCorp)** | Build → Deploy → Release pipeline unificado | Integração futura com CI/CD |

### 11.2 Features Inovadoras Identificadas na Pesquisa

| # | Feature | Descrição | Impacto em Segurança | Prioridade |
|---|---|---|---|---|
| 1 | **SBOM Attestation** | Assinar digitalmente o SBOM com chave do tenant para prova de proveniência | Audit trail de supply chain | Alta |
| 2 | **Dependency Pinning Policy** | Política que proíbe ranges (`>=1.0`) e exige versões exactas (`1.0.3`) | Previne ataques de dependency confusion | Alta |
| 3 | **License Allowlist/Denylist** | Configuração por tenant de licenças permitidas/proibidas | Compliance legal automático | Alta |
| 4 | **Breaking Change Detection** | Ao actualizar dependência, IA detecta breaking changes antes de aplicar | Previne erros em produção | Alta |
| 5 | **Reproducible Builds** | Gerar lock files (package-lock.json, packages.lock.json) obrigatoriamente | Build determinístico | Média |
| 6 | **VEX (Vulnerability Exploitability eXchange)** | Documentar se uma CVE é explorável no contexto específico do serviço | Reduz noise de falsos positivos | Média |
| 7 | **Dependency Drift Monitor** | Alertar quando dependências de prod divergem do que foi aprovado | Detecta alterações não autorizadas | Média |
| 8 | **Secure Defaults Generator** | Templates incluem configurações seguras por defeito (HTTPS, CORS restrito, etc.) | Previne misconfiguration | Alta |
| 9 | **API Contract Fuzzing** | Gerar testes de fuzzing automáticos a partir do contrato | Detecta edge cases e vulnerabilidades | Média |
| 10 | **Runtime Dependency Graph** | Em runtime, detectar quais dependências são realmente usadas vs declaradas | Reduz superfície de ataque | Baixa |
| 11 | **Canary Analysis on Dependencies** | Antes de upgrade, testar em canary com a nova versão | Previne regressões | Baixa |
| 12 | **AI Code Smell Detection** | IA detecta code smells no código gerado (God Classes, Feature Envy, etc.) | Qualidade de manutenção | Média |

---

## 12. Cronograma de Execução

### Sprint 0 — Correções (1 semana)
- [ ] BUG-01: RegisterServiceAsset 11 campos
- [ ] BUG-02: Enum Framework
- [ ] BUG-03: Domain no API client
- [ ] BUG-04: SchemaPropertyEditor $ref
- [ ] GAP-01: Outbox processing

### Sprint 1 — Fase 1: Template Manifest V2 (2 semanas)
- [ ] Schema ManifestV2 com architecture, packages, entities, endpoints
- [ ] Evolução do TemplateEditorPage
- [ ] Integração com GenerateAiScaffold
- [ ] Testes

### Sprint 2-3 — Fase 2: Dependency Health & SBOM (3 semanas)
- [ ] Domain entities e enums
- [ ] 10 features VSA
- [ ] Adaptadores para GitHub Advisory / OSV
- [ ] Parser de ficheiros de projeto
- [ ] Frontend: Dashboard + ServiceDependencyTab
- [ ] Testes

### Sprint 4 — Fase 3: Security Gate Pipeline (2 semanas)
- [ ] Domain entities
- [ ] 8 features VSA
- [ ] Internal SAST Scanner (10 regras)
- [ ] Integração com AI security-reviewer
- [ ] Frontend: Security findings viewer
- [ ] Testes

### Sprint 5 — Fase 4: Contract-to-Code Pipeline (2 semanas)
- [ ] 6 features VSA
- [ ] Mock server generation
- [ ] Postman collection export
- [ ] Orquestrador de pipeline
- [ ] Frontend: Pipeline wizard
- [ ] Testes

### Sprint 6 — Fase 5: Preview, Export & Registration (2 semanas)
- [ ] 6 componentes de preview
- [ ] Monaco Editor integration
- [ ] Push to Git / Create PR
- [ ] Auto-register no Service Catalog
- [ ] Testes

### Sprint 7 — Fase 6: AI Quality Gates (2 semanas)
- [ ] 3 novos AI agents
- [ ] Architectural fitness functions
- [ ] Quality Gate pipeline
- [ ] Frontend: Quality Gate viewer
- [ ] Testes

### Sprint 8-9 — Fase 7: Features de Aceleração (3 semanas)
- [ ] Mock Server instantâneo
- [ ] ADR Generator
- [ ] Environment Blueprint (Docker/Helm/CI)
- [ ] Smart Template Recommendations
- [ ] Pre-Commit Governance
- [ ] Testes

### Total Estimado: ~17 semanas (4 meses)

---

## 13. Métricas de Sucesso

| Métrica | Baseline Atual | Meta |
|---|---|---|
| **Time-to-First-Scaffold** | N/A (novo) | < 5 minutos |
| **Time-to-First-Deploy** | Dias (manual) | < 30 minutos (com blueprint) |
| **% Serviços com SBOM** | 0% | 100% dos scaffolded |
| **% Serviços com Security Scan** | 0% | 100% dos scaffolded |
| **Vulnerabilidades em Produção** | Desconhecido | 0 critical no scaffold |
| **Test Coverage no Scaffold** | 0% (sem testes) | > 80% (testes gerados) |
| **Governance Violations no Export** | N/A | 0 critical blocked |
| **Developer NPS do Scaffold** | N/A | > 60 (via DeveloperSurvey) |
| **Templates Activos** | 0 | > 10 (DotNet, Node, Java, Go, Python × REST, Event, gRPC) |
| **DxScore Improvement** | Baseline | > 20% melhoria em cycle time |

---

## 14. Definição de Pronto (DoD)

Cada feature é considerada pronta quando:

### Backend
- [ ] Domain entity com validação e invariantes
- [ ] Feature VSA (Command/Query + Validator + Handler + Response)
- [ ] Migration EF Core (se novo DbContext ou tabela)
- [ ] Endpoint Minimal API com autorização e rate limiting
- [ ] Testes unitários (≥ 3 por feature: happy path + validation fail + edge case)
- [ ] XML docs em português para todas as classes públicas
- [ ] Logging estruturado com categorias

### Frontend
- [ ] Componente com i18n completo (4 locales: en, pt-BR, pt-PT, es)
- [ ] Estados de loading, erro e vazio
- [ ] Responsividade (desktop + tablet)
- [ ] Sem textos hardcoded
- [ ] Coerência com Design System existente

### Segurança
- [ ] Sem secrets hardcoded
- [ ] Input validation em todos os campos
- [ ] Autorização verificada no backend (frontend é UX-only)
- [ ] Audit trail para acções sensíveis

### Qualidade
- [ ] Build com 0 erros
- [ ] Testes existentes continuam a passar
- [ ] Code review aprovado
- [ ] Documentação atualizada se necessário

---

## Apêndice A: Referências de Pesquisa

| Tópico | Referência |
|---|---|
| SBOM Standards | CycloneDX (OWASP), SPDX (Linux Foundation) |
| SCA (Software Composition Analysis) | Snyk, Dependabot, OWASP Dependency-Check |
| SAST (Static Application Security Testing) | Semgrep, SonarQube, CodeQL |
| API Security | OWASP API Security Top 10 (2023) |
| Supply Chain Security | SLSA Framework (Supply-chain Levels for Software Artifacts) |
| Vulnerability Databases | NVD (NIST), GitHub Advisory, OSV (Google) |
| License Compliance | SPDX License List, OSI Approved Licenses |
| Developer Platforms | Backstage (CNCF), Amplication, Humanitec, Port |
| AI Code Generation | GitHub Copilot, gpt-engineer, Cursor, Cody (Sourcegraph) |
| Architectural Fitness | ArchUnit, NetArchTest, fitness functions (Building Evolutionary Architectures) |

## Apêndice B: Novos AI Agents Propostos

| Agent Name | Category | Capabilities | System Prompt Summary |
|---|---|---|---|
| `dependency-advisor` | DependencyGovernance | chat, analysis | Analisa dependências, sugere upgrades seguros, detecta licenças problemáticas |
| `architecture-fitness-agent` | ArchitecturalGovernance | analysis | Valida conformidade com patterns (DDD, Clean Architecture, CQRS) |
| `documentation-quality-agent` | DocumentationAssistance | analysis, generation | Valida completude de docs, sugere melhorias, gera ADRs |

## Apêndice C: Novos Enums Propostos

```csharp
// PackageEcosystem.cs
public enum PackageEcosystem { NuGet = 0, Npm = 1, Maven = 2, PyPI = 3, Go = 4, Cargo = 5, Gem = 6 }

// LicenseRiskLevel.cs
public enum LicenseRiskLevel { Low = 0, Medium = 1, High = 2, Critical = 3 }

// VulnerabilitySeverity.cs
public enum VulnerabilitySeverity { None = 0, Low = 1, Medium = 2, High = 3, Critical = 4 }

// ExploitMaturity.cs
public enum ExploitMaturity { NotDefined = 0, Unproven = 1, ProofOfConcept = 2, Functional = 3, Active = 4 }

// SecurityCategory.cs (OWASP Top 10)
public enum SecurityCategory { Injection = 0, BrokenAuth = 1, SensitiveDataExposure = 2, XmlExternalEntities = 3, BrokenAccessControl = 4, SecurityMisconfiguration = 5, Xss = 6, InsecureDeserialization = 7, InsufficientLogging = 8, Ssrf = 9 }

// ScanTarget.cs
public enum ScanTarget { GeneratedCode = 0, Contract = 1, Template = 2, Dependency = 3 }

// SecurityRiskLevel.cs
public enum SecurityRiskLevel { Clean = 0, Low = 1, Medium = 2, High = 3, Critical = 4 }
```
