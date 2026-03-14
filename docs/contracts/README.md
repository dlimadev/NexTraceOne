# Módulo 5 — Contracts & Interoperability

## Visão Geral

O módulo **Contracts & Interoperability** é o motor de governança multi-protocolo do NexTraceOne.
Gerencia o ciclo de vida completo de contratos de API — desde a importação de especificações até a
assinatura digital para promoção em produção — com suporte a múltiplos protocolos de comunicação.

## Protocolos Suportados

| Protocolo | Status | Parser | Diff Semântico | Notas |
|-----------|--------|--------|----------------|-------|
| **OpenAPI 3.x** | ✅ Completo | `OpenApiSpecParser` | `OpenApiDiffCalculator` | Linha principal REST/HTTP |
| **Swagger 2.0** | ✅ Completo | `SwaggerSpecParser` | `SwaggerDiffCalculator` | Legado, migração assistida |
| **AsyncAPI 2.x/3.x** | ✅ Completo | `AsyncApiSpecParser` | `AsyncApiDiffCalculator` | Event-driven, mensageria |
| **WSDL 1.1/2.0** | ✅ Completo | `WsdlSpecParser` | `WsdlDiffCalculator` | SOAP/Web Services enterprise |
| **Protobuf** | 🔲 Futuro | — | — | gRPC (evolutivo) |
| **GraphQL SDL** | 🔲 Futuro | — | — | GraphQL (evolutivo) |

## Arquitetura

```
┌─────────────────────────────────────────────────────────────────┐
│                        API Layer                                 │
│  ContractsEndpointModule (21 endpoints REST)                     │
├─────────────────────────────────────────────────────────────────┤
│                     Application Layer                            │
│  21 Features VSA (Import, Version, Diff, Lock, Sign, Export,     │
│  Search, Violations, Lifecycle, Classification, Sync, Validate,  │
│  GenerateScorecard, GenerateEvidencePack, EvaluateContractRules, │
│  GetCompatibilityAssessment)                                     │
├─────────────────────────────────────────────────────────────────┤
│                       Domain Layer                               │
│  ContractVersion (AR) │ ContractDiff │ ContractArtifact          │
│  ContractRuleViolation │ ContractScorecard │ ContractEvidencePack│
│  ──────────────────────────────────────────                      │
│  Domain Services:                                                │
│  OpenApiSpecParser │ SwaggerSpecParser │ AsyncApiSpecParser       │
│  WsdlSpecParser │ ContractCanonicalizer                          │
│  OpenApiDiffCalculator │ SwaggerDiffCalculator                   │
│  AsyncApiDiffCalculator │ WsdlDiffCalculator                     │
│  ContractDiffCalculator (orquestrador multi-protocolo)           │
│  CanonicalModelBuilder │ ContractScorecardCalculator             │
│  ContractRuleEngine                                              │
│  ──────────────────────────────────────────                      │
│  Value Objects: SemanticVersion │ ContractSignature               │
│  ContractProvenance │ ChangeEntry │ ContractCanonicalModel       │
│  ContractOperation │ ContractSchemaElement                       │
│  CompatibilityAssessment │ SchemaRegistryBinding                 │
│  InteroperabilityProfile │ SchemaEvolutionRule                   │
│  ──────────────────────────────────────────                      │
│  Enums: KafkaSchemaCompatibility                                 │
├─────────────────────────────────────────────────────────────────┤
│                   Infrastructure Layer                            │
│  ContractsDbContext │ ContractVersionRepository                   │
│  EF Core Configurations │ Migrations                             │
├─────────────────────────────────────────────────────────────────┤
│                    Contracts Layer                                │
│  IContractsModule (interface cross-module)                        │
└─────────────────────────────────────────────────────────────────┘
```

## Modelo de Domínio

### ContractVersion (Aggregate Root)

Entidade principal que representa uma versão versionada de um contrato multi-protocolo.

**Propriedades:**
- `ApiAssetId` — FK para o módulo EngineeringGraph
- `SemVer` — versão semântica (ex: "1.2.3")
- `SpecContent` — conteúdo bruto da especificação (JSON, YAML ou XML)
- `Format` — formato: "json", "yaml" ou "xml"
- `Protocol` — protocolo: OpenApi, Swagger, Wsdl, AsyncApi, Protobuf, GraphQl
- `LifecycleState` — estado no ciclo de vida
- `IsLocked` — indica se está bloqueada
- `Signature` — assinatura digital (SHA-256)
- `Provenance` — proveniência (lineage) para auditoria

### Ciclo de Vida (State Machine)

```
Draft → InReview → Approved → Locked → Deprecated → Sunset → Retired
         ↑   ↓        ↑  ↓
         └───┘        └──┘
       (Return       (Return
        to Draft)     to InReview)
```

| Transição | Descrição |
|-----------|-----------|
| Draft → InReview | Submissão para revisão |
| InReview → Approved | Aprovação por tech lead |
| InReview → Draft | Retorno para rascunho |
| Approved → Locked | Bloqueio para produção |
| Approved → InReview | Retorno para revisão |
| Locked → Deprecated | Depreciação com aviso |
| Deprecated → Sunset | Início do sunset |
| Sunset → Retired | Aposentadoria definitiva |

## Fluxos Principais

### Import de Contrato

```
POST /api/v1/contracts
{
  "apiAssetId": "guid",
  "semVer": "1.0.0",
  "specContent": "{ ... }",
  "format": "json",
  "importedFrom": "upload",
  "protocol": "OpenApi"
}
```

**Campos:**
- `format` — aceita "json", "yaml" ou "xml" (XML necessário para WSDL)
- `protocol` — opcional, padrão "OpenApi". Valores: OpenApi, Swagger, Wsdl, AsyncApi, Protobuf, GraphQl
- `importedFrom` — origem do import: "upload", URL, "ai-generated", "migration"

**Auto-detecção de protocolo (novo):**
Quando o protocolo é informado como OpenApi (padrão), o sistema tenta detectar automaticamente:
- Se o conteúdo contém `"swagger"` → utiliza Swagger
- Se o conteúdo contém `"asyncapi"` → utiliza AsyncApi
- Se o conteúdo XML contém `<definitions` → utiliza WSDL
- Caso contrário, mantém o protocolo informado

**Validação de tamanho (novo):**
O conteúdo da especificação é limitado a 5 MB para prevenir ataques DoS.

1. Valida formato semântico da versão
2. Auto-detecta protocolo quando possível
3. Verifica se já existe versão com mesmo SemVer para o API Asset
4. Cria `ContractVersion` com estado `Draft` e protocolo detectado
5. Persiste via `IUnitOfWork`

### Criar Nova Versão

```
POST /api/v1/contracts/versions
{
  "apiAssetId": "guid",
  "semVer": "1.1.0",
  "specContent": "{ ... }",
  "format": "json",
  "importedFrom": "upload",
  "protocol": null
}
```

**Nota:** Quando `protocol` é null/omitido, a nova versão herda o protocolo da versão anterior,
garantindo continuidade na cadeia de versionamento.

### Diff Semântico

```
POST /api/v1/contracts/diff
{
  "baseVersionId": "guid",
  "targetVersionId": "guid"
}
```

1. Carrega ambas as versões do repositório
2. Detecta o protocolo da versão alvo
3. Delega ao `ContractDiffCalculator` (orquestrador multi-protocolo)
4. Classifica mudanças: Breaking, Additive, NonBreaking
5. Sugere próxima versão semântica
6. Persiste o diff na versão alvo

### Assinatura Digital

```
POST /api/v1/contracts/{id}/sign
```

1. Verifica que contrato está no estado Locked ou Approved
2. Canonicaliza o conteúdo via `ContractCanonicalizer`
3. Computa hash SHA-256 do conteúdo canônico
4. Armazena fingerprint, algoritmo, assinante e timestamp

### Verificação de Assinatura

```
GET /api/v1/contracts/{id}/verify
```

1. Recomputa hash SHA-256 do conteúdo atual
2. Compara com fingerprint armazenado
3. Retorna resultado: válido, inválido ou não assinado

## Endpoints da API

| Método | Rota | Descrição |
|--------|------|-----------|
| POST | `/api/v1/contracts` | Importar contrato (com auto-detecção de protocolo) |
| POST | `/api/v1/contracts/versions` | Criar nova versão |
| POST | `/api/v1/contracts/diff` | Computar diff semântico |
| GET | `/api/v1/contracts/{id}/classification` | Classificar mudança |
| GET | `/api/v1/contracts/suggest-version` | Sugerir versão semântica |
| GET | `/api/v1/contracts/history/{apiAssetId}` | Histórico de versões |
| GET | `/api/v1/contracts/{id}/detail` | Detalhes completos |
| GET | `/api/v1/contracts/{id}/export` | Exportar especificação |
| GET | `/api/v1/contracts/search` | Pesquisar contratos (paginado, com filtros) |
| GET | `/api/v1/contracts/{id}/violations` | Listar violações de regras |
| GET | `/api/v1/contracts/{id}/validate` | Validar integridade estrutural do contrato |
| POST | `/api/v1/contracts/sync` | Sincronizar contratos em lote (CI/CD) |
| POST | `/api/v1/contracts/{id}/lock` | Bloquear versão |
| POST | `/api/v1/contracts/{id}/lifecycle` | Transicionar lifecycle |
| POST | `/api/v1/contracts/{id}/deprecate` | Depreciar versão |
| POST | `/api/v1/contracts/{id}/sign` | Assinar versão |
| GET | `/api/v1/contracts/{id}/verify` | Verificar assinatura |
| GET | `/api/v1/contracts/{id}/scorecard` | Gerar scorecard técnico |
| GET | `/api/v1/contracts/{id}/evidence-pack` | Gerar evidence pack para workflow |
| GET | `/api/v1/contracts/{id}/rules` | Avaliar regras determinísticas |
| POST | `/api/v1/contracts/compatibility` | Avaliar compatibilidade entre versões |

### Pesquisa de Contratos (novo)

```
GET /api/v1/contracts/search?protocol=OpenApi&lifecycleState=Draft&searchTerm=1.0&page=1&pageSize=20
```

Parâmetros opcionais:
- `protocol` — filtrar por protocolo (OpenApi, Swagger, Wsdl, AsyncApi)
- `lifecycleState` — filtrar por estado do lifecycle
- `apiAssetId` — filtrar por ativo de API
- `searchTerm` — busca textual na versão semântica
- `page` — página (padrão: 1)
- `pageSize` — tamanho da página (padrão: 20, máximo: 100)

### Violações de Regras (novo)

```
GET /api/v1/contracts/{id}/violations
```

Retorna lista de violações de regras de governança para a versão do contrato.

## Diff Semântico por Protocolo

### OpenAPI / Swagger

Detecta mudanças em:
- **Paths** — adicionados/removidos (breaking se removido)
- **Métodos HTTP** — adicionados/removidos (breaking se removido)
- **Parâmetros** — adicionados/removidos (breaking se obrigatório adicionado ou qualquer removido)

### AsyncAPI

Detecta mudanças em:
- **Channels** — adicionados/removidos
- **Operações** (publish/subscribe) — adicionadas/removidas
- **Campos de mensagem** — adicionados/removidos (breaking se obrigatório adicionado)

### WSDL

Detecta mudanças em:
- **Port Types / Services** — adicionados/removidos
- **Operações SOAP** — adicionadas/removidas
- **Partes de mensagem** — adicionadas/removidas

## Semantic Versioning Advisor

Sugestão automática baseada no nível de mudança:

| Nível | Ação no SemVer | Exemplo |
|-------|----------------|---------|
| Breaking | Bump Major | 1.2.3 → 2.0.0 |
| Additive | Bump Minor | 1.2.3 → 1.3.0 |
| NonBreaking | Bump Patch | 1.2.3 → 1.2.4 |

## Modelo Canônico Interno

O módulo possui um modelo canônico interno (`ContractCanonicalModel`) que abstrai as diferenças
entre protocolos, permitindo raciocínio unificado sobre contratos independentemente do formato original.

### Componentes do Modelo Canônico

| Tipo | Descrição |
|------|-----------|
| `ContractCanonicalModel` | Representação normalizada: operações, schemas, metadados, segurança |
| `ContractOperation` | Operação normalizada (REST path+method, SOAP operation, AsyncAPI channel) |
| `ContractSchemaElement` | Elemento de schema com tipo, nome, obrigatoriedade e hierarquia |

O `CanonicalModelBuilder` constrói o modelo a partir de qualquer protocolo suportado:
- **OpenAPI/Swagger**: extrai paths, operações, schemas, securitySchemes, servers, tags
- **AsyncAPI**: extrai channels (publish/subscribe), schemas, servers
- **WSDL**: extrai portTypes, operações, elementos XSD

## Scorecard Técnico

O `ContractScorecard` fornece avaliação multi-dimensional da qualidade de um contrato:

| Dimensão | Peso | O que avalia |
|----------|------|-------------|
| **Quality** | 30% | Presença de descrições, segurança, naming |
| **Completeness** | 25% | Operações documentadas, schemas definidos, exemplos |
| **Compatibility** | 25% | Ausência de violações, conformidade com regras |
| **Risk** | 20% | Inversão: segurança ausente, violações, operações sem documentação |

Cada dimensão produz um score de 0.0 a 1.0 com justificativa textual. O `OverallScore` é a
média ponderada. Calculado pelo `ContractScorecardCalculator` a partir do modelo canônico.

## Evidence Pack para Workflow

O `ContractEvidencePack` agrega todas as evidências técnicas necessárias para decisões de
governança em um único pacote auditável:

- Nível de mudança (Breaking/Additive/NonBreaking)
- Contagem de breaking, aditivas e non-breaking changes
- Versão semântica recomendada
- Scores de qualidade e risco
- Contagem de violações de regras
- Resumo executivo e técnico
- Lista de consumers potencialmente impactados
- Flags: `RequiresWorkflowApproval`, `RequiresChangeNotification`

Gerado pela feature `GenerateEvidencePack`, alimenta o módulo de Workflow & Approval.

## Motor de Regras Determinísticas

O `ContractRuleEngine` avalia regras de conformidade organizacional sobre o modelo canônico:

| Regra | Severidade | O que verifica |
|-------|-----------|----------------|
| `SecurityDefinition` | Error | Presença de esquemas de autenticação |
| `OperationDescription` | Warning | Descrição em todas as operações |
| `NamingConvention` | Warning | Nomes sem espaços ou caracteres especiais |
| `SchemaCompleteness` | Warning | Todos os schemas com tipo definido |
| `ExamplesCoverage` | Info | Presença de exemplos |
| `DeprecationDocumentation` | Warning | Operações depreciadas com documentação |
| `VersioningConvention` | Warning | Formato SemVer válido |

Cada violação inclui nome da regra, severidade, mensagem explicativa, path e sugestão de correção.

## Kafka / Schema Registry Readiness

O módulo está preparado para integração com Kafka e Schema Registry:

### Tipos de Domínio

| Tipo | Descrição |
|------|-----------|
| `SchemaRegistryBinding` | Vinculação de contrato a subject/versão/topic do Schema Registry |
| `KafkaSchemaCompatibility` | Enum: None, Backward, BackwardTransitive, Forward, ForwardTransitive, Full, FullTransitive |
| `SchemaEvolutionRule` | Regra de evolução de schema com severidade e sugestão de correção |
| `InteroperabilityProfile` | Perfil de capacidades: formatos de export, conversão, round-trip |

### Integração Futura

- Importação direta de schemas do Confluent Schema Registry
- Validação de compatibilidade por modo (BACKWARD, FORWARD, FULL)
- Tracking de subjects, versões e relações topic↔producer↔consumer
- Diffs e impactos em consumers tratados como mudanças de contrato de primeira classe

## Assinatura e Integridade

### Canonicalização

O `ContractCanonicalizer` normaliza o conteúdo antes do hashing:
- **JSON**: ordena chaves recursivamente, remove indentação
- **YAML/XML**: normaliza quebras de linha, remove espaços extras
- **Limite**: máximo 10 MB de conteúdo

### Fingerprint SHA-256

A `ContractSignature` armazena:
- `Fingerprint` — hash SHA-256 do conteúdo canônico
- `Algorithm` — "SHA-256"
- `SignedBy` — identificação do assinante
- `SignedAt` — timestamp da assinatura

## Interface Cross-Module

```csharp
public interface IContractsModule
{
    Task<ChangeLevel?> GetLatestChangeLevelAsync(Guid apiAssetId, CancellationToken ct);
    Task<bool> HasContractVersionAsync(Guid apiAssetId, CancellationToken ct);
    Task<decimal?> GetLatestOverallScoreAsync(Guid apiAssetId, CancellationToken ct);
    Task<bool> RequiresWorkflowApprovalAsync(Guid apiAssetId, CancellationToken ct);
}
```

Permite que outros módulos (EngineeringGraph, ChangeIntelligence, Workflow) consultem dados de contratos
sem acessar diretamente o DbContext do módulo. Os novos métodos `GetLatestOverallScoreAsync` e
`RequiresWorkflowApprovalAsync` suportam integração com o motor de workflow e governança de mudanças.

## Frontend

A página de Contracts (`ContractsPage.tsx`) oferece:
- **Importação** — formulário com protocolo, versão, conteúdo (com auto-detecção de formato)
- **Criação de Versão** — formulário para criar nova versão a partir de versão anterior
- **Histórico** — tabela com versões filtradas por API Asset
- **Detalhe** — painel expansível com metadados, proveniência, spec
- **Diff Semântico** — comparação visual entre versões com categorização
- **Lifecycle** — transições de estado com botões contextuais
- **Assinatura** — assinar, verificar integridade
- **Violações** — painel de violações de regras com severidade
- **Exportação** — visualizar conteúdo raw
- **Rota protegida** — `ProtectedRoute` com permissão `contracts:read`
- **i18n** — 149+ chaves em 4 idiomas (en, pt-BR, pt-PT, es) com paridade perfeita

## Testes

- **216+ testes** cobrindo:
  - Entidades de domínio (ContractVersion, lifecycle, lock, sign, ContractScorecard, ContractEvidencePack)
  - Value Objects (SemanticVersion, ContractSignature, ContractCanonicalModel, ContractOperation, ContractSchemaElement, CompatibilityAssessment, SchemaRegistryBinding, InteroperabilityProfile, SchemaEvolutionRule)
  - Parsers (OpenAPI, Swagger, WSDL, AsyncAPI)
  - Diff Calculators (todos os protocolos + orquestrador)
  - Domain Services (CanonicalModelBuilder, ContractScorecardCalculator, ContractRuleEngine)
  - Features de aplicação (import, version, diff, lock, sign, export, lifecycle, scorecard, evidence pack, rules, compatibility)
  - Import multi-protocolo (OpenAPI, Swagger, WSDL/XML, AsyncAPI)
  - Auto-detecção de protocolo (Swagger, AsyncAPI, WSDL a partir do conteúdo)
  - Validação multi-protocolo (ValidateContractIntegrity por protocolo)
  - Herança de protocolo entre versões
  - Canonicalization (JSON key sorting, XML/YAML line normalization)
  - Segurança de assinatura (timing-safe comparison)

## Segurança

### Assinatura Timing-Safe (novo)

A verificação de assinatura utiliza `CryptographicOperations.FixedTimeEquals` para prevenir
ataques de timing, onde um atacante poderia inferir o fingerprint correto medindo o tempo
de resposta da comparação de strings.

### Validação de Tamanho (novo)

O conteúdo da especificação é limitado a 5 MB via FluentValidation `MaximumLength(5_242_880)`,
aplicado tanto no import quanto na criação de nova versão, prevenindo ataques DoS.

### Rota Protegida (novo)

O acesso à página de contratos é protegido por `ProtectedRoute` com permissão `contracts:read`,
garantindo que apenas usuários autorizados acessem a funcionalidade.

## Decisões Arquiteturais

1. **Parsers como domain services estáticos** — cálculos puros sem I/O, fáceis de testar
2. **Orquestrador multi-protocolo** — `ContractDiffCalculator` delega ao parser correto via pattern matching
3. **Canonicalização determinística** — garante que assinaturas permanecem válidas após reformatação
4. **State machine no aggregate** — transições validadas no domínio, não no handler
5. **Protobuf/GraphQL como evolutivos** — enum definido, parser a implementar quando necessário
6. **Auto-detecção de protocolo** — inferência automática a partir do conteúdo (Swagger, AsyncAPI, WSDL)
7. **Verificação timing-safe** — previne ataques de timing na verificação de assinatura
8. **Pesquisa paginada** — busca com filtros por protocolo, estado, ativo e versão
9. **Modelo canônico internal** — `ContractCanonicalModel` abstrai diferenças entre protocolos para raciocínio unificado
10. **Scorecard multi-dimensional** — 4 dimensões ponderadas com justificativas textuais auditáveis
11. **Evidence pack auto-gerado** — agrega diff + scorecard + regras em pacote único para workflow
12. **Motor de regras determinístico** — `ContractRuleEngine` avalia regras sem IA, resultados reproduzíveis
13. **Kafka/Schema Registry como tipos de domínio** — readiness via value objects, sem dependência de runtime client

## Riscos Residuais

1. **Diff não cobre schemas** — mudanças em request/response body types não são detectadas (apenas paths, métodos e parâmetros)
2. **YAML parsing heurístico** — para OpenAPI/AsyncAPI em YAML, o parsing é baseado em indentação, não em parser YAML completo
3. **Sem notificação automática** — depreciação não notifica consumers automaticamente (requer integração com DeveloperPortal)
4. **Contract Studio não implementado** — edição visual de contratos é funcionalidade evolutiva (P2)
5. **Design-First Accelerator não implementado** — geração assistida de contratos é funcionalidade evolutiva (P2)
6. **Protobuf/GraphQL stub** — validação retorna sucesso com 0 counts; parsing completo é evolutivo
7. **Schema Registry client** — tipos de domínio prontos, mas sem client HTTP para Confluent Schema Registry (P2)
8. **EF migration pendente** — `ContractScorecard` e `ContractEvidencePack` requerem migration para persistência

## Scripts SQL de Mock (Dev/Debug)

Disponíveis em `database/seeds/contracts/`:

| Script | Conteúdo |
|--------|----------|
| `00-reset.sql` | Cleanup de todos os dados de seed |
| `01-rest-contracts.sql` | 4 contratos REST (OpenAPI 3.1 + Swagger 2.0) |
| `02-soap-wsdl-contracts.sql` | 3 contratos SOAP (WSDL 1.1 com cenários breaking/non-breaking) |
| `03-asyncapi-kafka-contracts.sql` | 4 contratos event-driven (AsyncAPI 2.6 para Kafka) |
| `04-diffs-and-scenarios.sql` | 6 diffs pré-computados (aditivo/breaking por protocolo) |

Detalhes completos em `database/seeds/contracts/README.md`.
