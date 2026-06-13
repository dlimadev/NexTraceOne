# Análise — Governança de Serviços & Aceleração de Desenvolvimento

> **Data:** 2026-06-13
> **Escopo:** Verificar se a visão de "governar serviços ponta a ponta desde a criação, com padrões pré-determinados (modelos de arquitetura), qualidade de código (Spectral/SonarQube) e uma ferramenta de IDE que faz scaffolding a partir do serviço/contrato cadastrado no NexTraceOne" já está implementada — e, onde não estiver, propor um plano de ação e novas funcionalidades.
> **Premissa de design transversal:** tudo deve funcionar **sem IA** (caminho determinístico) e usar IA **apenas como acelerador opcional** quando configurada.

---

## 1. Sumário Executivo

**Veredito: a visão descrita já está, na sua maior parte, implementada — e com profundidade.** O NexTraceOne possui hoje:

- Um **registo de modelos de arquitetura** governados (`ServiceTemplate` + `TemplateManifestV2`), com padrão arquitetural, stack, estrutura de pastas, dependências obrigatórias e *quality gates*.
- **Scaffolding ponta a ponta** a partir desses modelos, exposto em **3 superfícies**: extensão de **Visual Studio**, extensão de **VS Code** e **CLI** (`nex scaffold`).
- Especificação do **tipo de serviço** na criação (REST interna/externa, **Kafka consumer/producer**, background worker, gRPC, GraphQL, SOAP, e até ativos mainframe) — exatamente o exemplo citado.
- **Spectral** (o "spektral" referido) totalmente integrado para *linting* de contratos, com marketplace de pacotes e scoring.
- **Integração com SonarQube** real (webhook de ingestão, persistência, relatório de tech-debt).
- Caminho **determinístico** (substituição de variáveis) **e** caminho **assistido por IA** (`GenerateAiScaffold`) com **fallback automático** quando não há provider de IA.

**O que falta** não é a fundação — é **fechar o laço de governança**: os *quality gates* declarados nos modelos hoje são **descritivos**, não são **forçados** contra os dados reais que já chegam do SonarQube/Spectral; e não existe uma verificação **determinística** de conformidade arquitetural (estrutura/dependências/camadas) do repositório real contra o modelo que o originou — só existe o agente de IA `ArchitectureFitness` (subjetivo e dependente de IA).

A Secção 5 detalha as lacunas; a Secção 6 propõe o plano de ação; a Secção 7 lista novas funcionalidades.

---

## 2. Mapeamento Ponto-a-Ponto: Visão → Implementação Real

| # | Requisito da visão | Estado | Evidência (artefato real) |
|---|--------------------|--------|---------------------------|
| 1 | Governar serviços **desde a criação** com padrões pré-determinados | ✅ Implementado | `ServiceTemplate` aplica `GovernancePolicyIds`, `DefaultDomain`/`DefaultTeam`, `BaseContractSpec` ao serviço criado |
| 2 | **Cadastrar modelos / designer de arquitetura** | 🟡 Parcial | CRUD completo de templates (API + CLI + web `TemplateEditorPage.tsx`); falta um *designer* visual (hoje é editor de formulário/JSON) |
| 3 | **Especificar o modelo ao criar serviço** (Kafka consumer, API interna, API externa…) | ✅ Implementado | `ServiceType` (21 valores: `KafkaConsumer`, `KafkaProducer`, `RestApi`, `GrpcService`, `BackgroundService`…) + `ExposureType` (`Internal`/`External`/`Partner`) |
| 4 | **Modelos de arquitetura** (padrão, camadas, stack) | ✅ Implementado | `TemplateManifestV2` → `ManifestArchitecture` (Pattern/Style/Layers), `ManifestStack`, `ManifestFolder[]`, `ManifestRequiredDependency[]` |
| 5 | **Ferramenta de IDE** (Visual Studio **ou** VS Code) que cria a estrutura do projeto | ✅ Implementado (ambos) | VS: `NexAiScaffoldCommand.cs` + `NexScaffoldDialog.cs`; VS Code: `extension.ts` `handleScaffoldCommand` |
| 6 | Estrutura criada **com base no serviço e no contrato** do NexTraceOne | ✅ Implementado | Scaffold escreve ficheiros + `BaseContractSpec` (OpenAPI/AsyncAPI/WSDL) detectado por tipo de serviço |
| 7 | **Qualidade de código tipo Spectral** | ✅ Implementado | `ContractLintRuleset`, `ActivateSpectralPackage`, `GetSpectralMarketplace`, `ComputeRulesetScore`, frontend `useSpectralRulesets.ts` (50+ ficheiros) |
| 8 | **Integração com SonarQube** | ✅ Implementado | `SonarQubeIngestEndpoints.cs` (`POST /api/v1/quality/sonarqube/analysis`), `CodeQualityRecord`, `GetCodeQualityReport`, `ScanProvider.SonarQube` |
| 9 | **Acelerar o desenvolvimento** com padrões pré-estabelecidos | ✅ Implementado | Pilar "Developer Acceleration"; `GenerateEnvironmentBlueprint` (Dockerfile, compose, CI/CD, Helm) |
| 10 | Funcionar **com ou sem IA** | ✅ Implementado | Determinístico: `ScaffoldServiceFromTemplate`; IA: `GenerateAiScaffold` com `GenerateFallbackFiles` quando provider indisponível |

**Conclusão do mapeamento:** 8 de 10 itens estão plenamente implementados; 2 são parciais (designer visual de arquitetura; e o item transversal de *enforcement* — ver §5).

---

## 3. O Que Já Existe (por pilar)

### 3.1 Registo de Modelos de Arquitetura (`ServiceTemplate` + `TemplateManifestV2`)

- **Entidade:** `src/modules/catalog/.../Templates/Entities/ServiceTemplate.cs`
  - `Slug`, `DisplayName`, `Version`, `ServiceType`, `Language`, `Tags`
  - `DefaultDomain`, `DefaultTeam`, `GovernancePolicyIds` (aplicados automaticamente)
  - `BaseContractSpec`, `ScaffoldingManifestJson`, `RepositoryTemplateUrl`
  - `ArchitecturePatternJson` → serializa `TemplateManifestV2`
  - `UsageCount`, `IsActive`, multi-tenant
- **Modelo de arquitetura:** `.../Templates/ValueObjects/TemplateManifestV2.cs`
  - `ManifestArchitecture` — Pattern (Clean/Layered/Hexagonal/CQRS…), Style, Layers, Description
  - `ManifestStack` — Runtime, Language, Framework, AdditionalFrameworks
  - `ManifestFolder[]` — Path, Purpose, IsRequired
  - `ManifestRequiredDependency[]` — Name, MinVersion, Ecosystem, Reason
  - `ManifestQualityGates` — `TestCoverageMinimum` (default 70), `RequireUnitTests`, `RequireIntegrationTests`, `RequireOpenApiSpec`, `RequiredLinters`

### 3.2 Scaffolding em 3 superfícies

- **Visual Studio:** wizard WPF (`NexScaffoldDialog`), busca templates ativos, faz `POST /templates/slug/{slug}/scaffold`, escreve ficheiros com proteção de *path traversal*, grava o contrato base e abre o `.csproj`.
- **VS Code:** `extension.ts` com *quick-pick* multi-passo, chat participant `@nextraceone /scaffold`, Language Model Tools, MCP.
- **CLI:** `nex scaffold templates | init | register` (`tools/NexTraceOne.CLI/Commands/ScaffoldCommand.cs`).
- **Backend determinístico:** `ScaffoldServiceFromTemplate` — valida nome kebab-case, substitui `{{ServiceName}}`/`{{Domain}}`/etc., devolve `ScaffoldPlan` (ficheiros + políticas + contrato).
- **Backend com IA:** `GenerateAiScaffold` — gera conteúdo real dos ficheiros via `IExternalAIRoutingPort`; `GenerateEnvironmentBlueprint` — Dockerfile/compose/CI-CD/Helm.

### 3.3 Qualidade de Código — Spectral (contratos) + SonarQube (código)

- **Spectral:** `ContractLintRuleset`, regras por *asset type* (`RulesetBinding`), `ComputeRulesetScore` (`100 - errors*10 - warnings*5 - infos*1`), `ActivateSpectralPackage`, `GetSpectralMarketplace`, violações em `ctr_contract_rule_violations`.
- **SonarQube:** webhook `SonarQubeIngestEndpoints.cs` extrai `quality_gate`, `coverage`, `bugs`, `vulnerabilities`, `code_smells`, `duplicated_lines_density`; persiste em `CodeQualityRecord` (`ctr_code_quality_records`); `GetCodeQualityReport` agrega *pass rate*, cobertura média e *tech-debt score* (`bugs*3 + vulnerabilities*5 + codeSmells*1`).
- **Governança de mudança:** `Ruleset`/`RulesetBinding`, `SecurityGate` (`EvaluateSecurityGate`), `PolicyAsCodeDefinition` (Advisory→SoftEnforce→HardEnforce), `RunComplianceChecks`, `TechnicalDebtItem`.

### 3.4 Contratos e Portal do Desenvolvedor

Contract Studio (draft→review→publish), scorecard 4-dimensões, multi-protocolo (OpenAPI/Swagger/WSDL/AsyncAPI/Protobuf + mainframe), portal com subscriptions, playground, geração de SDK e analytics.

---

## 4. Garantia "Funciona Sem IA"

O design **já separa** o caminho determinístico do assistido por IA:

| Capacidade | Sem IA (determinístico) | Com IA (acelerador) |
|------------|-------------------------|---------------------|
| Scaffolding | `ScaffoldServiceFromTemplate` (substituição de variáveis) | `GenerateAiScaffold` (gera lógica real) |
| Indisponibilidade de provider | — | `GenerateFallbackFiles` devolve scaffold mínimo + flag `IsFallback` |
| Geração de contrato | Template `BaseContractSpec` | `AiDraftGeneratorService` (verifica capability `ai_enabled`, *fallback* nulo) |
| Linting de contrato | Spectral (regras determinísticas) | Classificação de breaking-change assistida |
| Análise de arquitetura | **(lacuna — ver §5.2)** | `ArchitectureFitness` (LLM) |

`AiRoutingOptions` tem `EnableDeterministicFallback` + prefixo `[FALLBACK_PROVIDER_UNAVAILABLE]`, e `AiExecutionGateway` faz health-check/ranking de providers. **A regra está respeitada.**

---

## 5. Lacunas Identificadas (com evidência)

### 5.1 🔴 *Quality gates* do modelo são declarativos, não forçados (laço aberto)

`TemplateManifestV2.ManifestQualityGates` define mínimos (cobertura 70%, linters, testes). Mas uma busca por `QualityGates`/`TestCoverageMinimum`/`RequiredLinters` em todo o código encontra **apenas** o próprio value object e os seus testes — **nenhum consumidor** liga esses mínimos aos dados reais que **já chegam** do SonarQube (`CodeQualityRecord`) ou do Spectral. Ou seja: o modelo *diz* "cobertura ≥ 70%", mas nada compara isso com a cobertura real do serviço nem bloqueia/sinaliza divergência.

### 5.2 🔴 Não há verificação **determinística** de conformidade arquitetural

Todos os resultados de `drift`/`conformance` no código pertencem a **drift de runtime/ambiente** (módulo `operationalintelligence`: `DetectEnvironmentDrift`, `RuntimeBaseline`…). A única análise da **estrutura/arquitetura do código** é o agente `ArchitectureFitness` — **baseado em LLM**, portanto subjetivo e indisponível sem IA. **Não existe** um verificador que compare, de forma determinística, a estrutura real do repositório (pastas, camadas, dependências) com o `ManifestFolder[]`/`ManifestRequiredDependency[]` do modelo que o gerou.

### 5.3 🟡 *Quality gate* de código não participa do gate de promoção

A promoção em `changegovernance` usa rulesets/Spectral (contratos) e `SecurityGate` (segurança). O `CodeQualityReport` (SonarQube) **alimenta um relatório**, mas não foi encontrado como **gate** que bloqueie promoção/release quando o *quality gate* do Sonar falha ou cobertura < mínimo do template.

### 5.4 🟡 Designer de arquitetura é editor de formulário/JSON, não visual

Existe `TemplateEditorPage.tsx` (web) e CRUD via API/CLI, mas não há um *designer* visual de camadas/dependências como sugere "designer de arquitetura". Funcionalmente coberto; experiência poderia evoluir.

### 5.5 🟢 Lacunas menores / placeholders conhecidos

- Geração de scaffold para **GraphQL** e IaC **Terraform/CloudFormation** ausente (só REST/AsyncAPI/WSDL e Docker/Helm).
- Sem UI de **criar/editar templates dentro da IDE** (só consumir).
- *Null readers* phase-gated (deprecation forecast, feature-flag risk, portal adoption) — esperado por design (Parte 21 do CLAUDE.md).

---

## 6. Plano de Ação (faseado, verificável)

> Cada fase é independente e tem critério de verificação. Determinístico primeiro; IA é sempre *opcional/aditiva*.

### Fase 1 — Fechar o laço dos *quality gates* (resolve §5.1 e §5.3) — **prioridade alta**

1. Criar feature `EvaluateTemplateQualityGates` (catalog ou governance):
   - Entrada: `serviceId` (+ opcional `templateId` de origem).
   - Lê o `TemplateManifestV2.QualityGates` do template de origem do serviço e o `CodeQualityRecord` mais recente (Sonar) + score Spectral.
   - Devolve `QualityGateEvaluation { Passed, Breaches[] }` comparando cobertura, bugs, vulnerabilidades e linters exigidos.
   - **Determinístico**; IA opcional só para *sugerir remediação* (texto).
   - **Verificar:** teste de handler — serviço com cobertura 60% e template exigindo 70% ⇒ `Passed=false` com breach "coverage".
2. Persistir a origem do template no serviço: adicionar `OriginTemplateId`/`OriginTemplateVersion` em `ServiceAsset` (migration).
   - **Verificar:** scaffold de um serviço grava o template de origem; query retorna-o.
3. Expor `GET /api/v1/catalog/services/{id}/quality-gate` + cartão no portal.
   - **Verificar:** endpoint retorna avaliação; teste de contrato.

> **Estado de implementação (atualizado):** passos 1 e 2 implementados.
> - **Passo 1** — feature `EvaluateTemplateQualityGates` (catalog
>   `Application/Contracts/Features/`) + endpoint
>   `GET /api/v1/quality/services/{serviceId}/gate` + testes unitários.
>   Avaliação **determinística** (cobertura vs. mínimo do manifesto e quality gate
>   do SonarQube).
> - **Passo 2** — `ServiceAsset` ganha `OriginTemplateId`/`OriginTemplateVersion`
>   (migration `AddServiceOriginTemplate`), preenchidos no fluxo de registo
>   (`RegisterServiceAsset` ← `AutoRegisterScaffoldedService`). Quando o gate é
>   avaliado sem `templateId`/`templateSlug`, o template é resolvido a partir do
>   template de origem do serviço; sem origem, devolve `NoTemplateLinked`.
>
> Falta apenas as superfícies de scaffold (VS/VS Code/CLI) passarem o `templateId`
> no registo e o cartão de quality gate no portal.

### Fase 2 — Quality gate como gate de promoção (resolve §5.3)

1. No fluxo de promoção de `changegovernance`, adicionar avaliação de `QualityGateEvaluation` como *check* (modo configurável `Advisory`/`SoftEnforce`/`HardEnforce`, reutilizando o padrão de `PolicyAsCodeDefinition.EnforcementMode`).
   - **Verificar:** release de serviço que falha o gate do Sonar é bloqueado em `HardEnforce` e apenas avisado em `Advisory`.

> **Estado de implementação (atualizado):** implementado.
> - Contrato inter-módulo `ICatalogQualityGateModule` (Catalog.Contracts) +
>   implementação `CatalogQualityGateModuleService` (reutiliza a feature da Fase 1
>   via MediatR — sem aceder ao DbContext do Catalog).
> - Feature `EvaluateCodeQualityPromotionGate` (changegovernance) aplica o modo
>   `CodeQualityGateEnforcement` (Advisory/SoftEnforce/HardEnforce) e devolve um
>   veredito com `Blocking` — convertível num `GateEvaluationInput` do motor de
>   gates existente. Endpoint
>   `GET /api/v1/promotion/services/{serviceId}/code-quality-gate?enforcement=…`.
> - Testes: HardEnforce bloqueia quando falha; SoftEnforce avisa; Advisory nunca
>   bloqueia; gate aprovado nunca bloqueia.
>
> A ligação automática ao motor de promoção (incluir este veredito na lista de
> gates de uma `PromotionRequest`) e a origem do modo via configuração de ambiente
> ficam para o próximo incremento.

### Fase 3 — Conformidade arquitetural determinística (resolve §5.2)

1. Criar `ArchitectureConformanceChecker` (determinístico) que recebe um *manifesto de estrutura* do repositório (lista de pastas + dependências do `.csproj`/`package.json`/`pom.xml`) e compara com `ManifestFolder[]`/`ManifestRequiredDependency[]`.
   - Implementar como: (a) comando CLI `nex conformance check` que corre **localmente/na CI** e faz `POST` do resultado; e (b) feature backend `EvaluateArchitectureConformance`.
   - **Verificar:** repo sem a pasta `tests/` exigida ⇒ breach "missing required folder: tests".
2. O agente `ArchitectureFitness` (IA) torna-se **enriquecimento opcional** por cima do resultado determinístico (sugestões de refactor), nunca pré-requisito.
   - **Verificar:** com IA desligada, a conformância continua a produzir resultado válido.

### Fase 4 — Acabamentos

1. Geradores de scaffold para **GraphQL** e **Terraform** em `GenerateEnvironmentBlueprint`.
2. Comando de IDE/CLI para **criar/editar template** (não só consumir).
3. (Opcional) Designer visual de camadas no `TemplateEditorPage`.

---

## 7. Novas Funcionalidades Recomendadas (vertente de aceleração)

| Ideia | Valor | IA? |
|-------|-------|-----|
| **Golden Path Score por serviço** | Combina conformidade arquitetural + quality gate + scorecard de contrato num único índice de "aderência ao padrão", visível no portal | Não (IA opcional p/ explicação) |
| **Tech Radar / catálogo de stacks aprovadas** | Templates marcam stack como Adopt/Trial/Hold; scaffold avisa se a stack está em Hold | Não |
| **Conformance gate na CI (`nex conformance check`)** | Bloqueia merge se o repo divergir do modelo (pastas/dependências/cobertura) | Não |
| **Drift de template** | Notifica serviços criados de um template quando o template evolui (nova versão de dependência, novo quality gate) | Não |
| **Scaffold incremental** | "Adicionar um endpoint/consumer ao serviço existente seguindo o padrão", não só projeto novo | IA opcional acelera |
| **Migração assistida de contrato → código** | A partir de um `ContractVersion` publicado, gerar/atualizar handlers e DTOs | IA opcional |
| **Catálogo de regras Spectral + Sonar quality profiles versionados por tenant** | Unificar a definição de "qualidade" (contrato + código) num único *governance pack* distribuível | Não |

---

## 8. Apêndice — Arquivos-Chave

**Modelos / Scaffolding**
- `src/modules/catalog/.../Templates/Entities/ServiceTemplate.cs`
- `src/modules/catalog/.../Templates/ValueObjects/TemplateManifestV2.cs`
- `src/modules/catalog/.../Templates/Features/ScaffoldServiceFromTemplate/ScaffoldServiceFromTemplate.cs`
- `src/modules/catalog/.../Templates/Features/GenerateEnvironmentBlueprint/GenerateEnvironmentBlueprint.cs`
- `src/modules/aiknowledge/.../Orchestration/Features/GenerateAiScaffold/GenerateAiScaffold.cs`
- `src/modules/catalog/.../Templates/ServiceTemplateEndpointModule.cs`

**IDE / CLI**
- `tools/ide-extensions/visualstudio/Commands/NexAiScaffoldCommand.cs`, `ToolWindows/NexScaffoldDialog.cs`
- `tools/ide-extensions/vscode/extension.ts`, `package.json`
- `tools/NexTraceOne.CLI/Commands/ScaffoldCommand.cs`

**Qualidade de código**
- `src/platform/NexTraceOne.Ingestion.Api/Endpoints/SonarQubeIngestEndpoints.cs`
- `src/modules/catalog/.../Contracts/Abstractions/ICodeQualityRepository.cs`
- `src/modules/catalog/.../Contracts/Features/GetCodeQualityReport/GetCodeQualityReport.cs`
- `src/modules/changegovernance/.../RulesetGovernance/...` (Spectral: `ActivateSpectralPackage`, `GetSpectralMarketplace`, `ComputeRulesetScore`)
- `src/modules/governance/.../Entities/PolicyAsCodeDefinition.cs`, `SecurityGate/...`

**Tipos de serviço / contrato**
- `src/modules/catalog/.../Graph/Entities/ServiceAsset.cs`, `Graph/Enums/ServiceType.cs`
- `src/modules/catalog/.../Contracts/Entities/ContractVersion.cs`, `ContractDraft.cs`, `ContractScorecard.cs`

**Frontend**
- `src/frontend/src/features/catalog/pages/{TemplateLibraryPage,TemplateDetailPage,TemplateEditorPage,AiScaffoldWizardPage}.tsx`
- `src/frontend/src/features/contracts/hooks/useSpectralRulesets.ts`
