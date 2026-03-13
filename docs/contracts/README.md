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
│  ContractsEndpointModule (13 endpoints REST)                     │
├─────────────────────────────────────────────────────────────────┤
│                     Application Layer                            │
│  14 Features VSA (Import, Version, Diff, Lock, Sign, Export...) │
├─────────────────────────────────────────────────────────────────┤
│                       Domain Layer                               │
│  ContractVersion (AR) │ ContractDiff │ ContractArtifact          │
│  ContractRuleViolation │ ContractLock │ OpenApiSchema            │
│  ──────────────────────────────────────────                      │
│  Domain Services:                                                │
│  OpenApiSpecParser │ SwaggerSpecParser │ AsyncApiSpecParser       │
│  WsdlSpecParser │ ContractCanonicalizer                          │
│  OpenApiDiffCalculator │ SwaggerDiffCalculator                   │
│  AsyncApiDiffCalculator │ WsdlDiffCalculator                     │
│  ContractDiffCalculator (orquestrador multi-protocolo)           │
│  ──────────────────────────────────────────                      │
│  Value Objects: SemanticVersion │ ContractSignature               │
│  ContractProvenance │ ChangeEntry                                │
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

1. Valida formato semântico da versão
2. Verifica se já existe versão com mesmo SemVer para o API Asset
3. Cria `ContractVersion` com estado `Draft` e protocolo informado
4. Persiste via `IUnitOfWork`

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
| POST | `/api/v1/contracts` | Importar contrato |
| POST | `/api/v1/contracts/versions` | Criar nova versão |
| POST | `/api/v1/contracts/diff` | Computar diff semântico |
| GET | `/api/v1/contracts/{id}/classification` | Classificar mudança |
| GET | `/api/v1/contracts/suggest-version` | Sugerir versão semântica |
| GET | `/api/v1/contracts/history/{apiAssetId}` | Histórico de versões |
| GET | `/api/v1/contracts/{id}/detail` | Detalhes completos |
| GET | `/api/v1/contracts/{id}/export` | Exportar especificação |
| POST | `/api/v1/contracts/{id}/lock` | Bloquear versão |
| POST | `/api/v1/contracts/{id}/lifecycle` | Transicionar lifecycle |
| POST | `/api/v1/contracts/{id}/deprecate` | Depreciar versão |
| POST | `/api/v1/contracts/{id}/sign` | Assinar versão |
| GET | `/api/v1/contracts/{id}/verify` | Verificar assinatura |

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
}
```

Permite que outros módulos (EngineeringGraph, ChangeIntelligence) consultem dados de contratos
sem acessar diretamente o DbContext do módulo.

## Frontend

A página de Contracts (`ContractsPage.tsx`) oferece:
- **Importação** — formulário com protocolo, versão, conteúdo
- **Histórico** — tabela com versões filtradas por API Asset
- **Detalhe** — painel expansível com metadados, proveniência, spec
- **Diff Semântico** — comparação visual entre versões
- **Lifecycle** — transições de estado com botões contextuais
- **Assinatura** — assinar, verificar integridade
- **Exportação** — visualizar conteúdo raw
- **i18n** — 127 chaves em 4 idiomas (en, pt-BR, pt-PT, es) com paridade perfeita

## Testes

- **158+ testes** cobrindo:
  - Entidades de domínio (ContractVersion, lifecycle, lock, sign)
  - Value Objects (SemanticVersion, ContractSignature)
  - Parsers (OpenAPI, Swagger, WSDL, AsyncAPI)
  - Diff Calculators (todos os protocolos + orquestrador)
  - Features de aplicação (import, version, diff, lock, sign, export, lifecycle)
  - Import multi-protocolo (OpenAPI, Swagger, WSDL/XML, AsyncAPI)
  - Herança de protocolo entre versões
  - Validação de formatos (json, yaml, xml)
  - Canonicalization (JSON key sorting, XML/YAML line normalization)

## Decisões Arquiteturais

1. **Parsers como domain services estáticos** — cálculos puros sem I/O, fáceis de testar
2. **Orquestrador multi-protocolo** — `ContractDiffCalculator` delega ao parser correto via pattern matching
3. **Canonicalização determinística** — garante que assinaturas permanecem válidas após reformatação
4. **State machine no aggregate** — transições validadas no domínio, não no handler
5. **Protobuf/GraphQL como evolutivos** — enum definido, parser a implementar quando necessário

## Riscos Residuais

1. **Diff não cobre schemas** — mudanças em request/response body types não são detectadas (apenas paths, métodos e parâmetros)
2. **YAML parsing heurístico** — para OpenAPI/AsyncAPI em YAML, o parsing é baseado em indentação, não em parser YAML completo
3. **Sem notificação automática** — depreciação não notifica consumers automaticamente (requer integração com DeveloperPortal)
4. **Sem batch import** — importação é individual, sem suporte a lote ou scan de repositórios
