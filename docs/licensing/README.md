# Módulo 2 — CommercialGovernance (Licensing & Entitlements)

## Visão Geral

O módulo CommercialGovernance é a fonte de verdade para todo o ciclo de vida de licenciamento da plataforma NexTraceOne. Cobre:

- **Plano comercial** e edições (Community, Professional, Enterprise, Unlimited)
- **Licença** com validade temporal, status e hardware binding
- **Entitlements** baseados em capabilities e quotas de uso
- **Ativação** online e offline (prontidão para cenários air-gapped)
- **Trial** estruturado com extensão e conversão
- **Usage metering** com thresholds e warnings proativos
- **Telemetry consent** respeitando LGPD/GDPR
- **Vendor operations** para gestão interna da NexTraceOne

## Arquitetura

```
CommercialGovernance/
├── Domain/
│   ├── Entities/
│   │   ├── License.cs                 ← Aggregate Root
│   │   ├── LicenseCapability.cs       ← Capability habilitada
│   │   ├── LicenseActivation.cs       ← Registro de ativação
│   │   ├── UsageQuota.cs              ← Quota de uso
│   │   ├── HardwareBinding.cs         ← Vínculo de hardware
│   │   ├── LicenseThresholdAlert.cs   ← Alerta de threshold
│   │   └── TelemetryConsent.cs        ← Consentimento de telemetria
│   ├── Enums/
│   │   ├── LicenseType.cs             ← Trial, Standard, Enterprise
│   │   ├── LicenseEdition.cs          ← Community, Professional, Enterprise, Unlimited
│   │   ├── LicenseStatus.cs           ← Active, GracePeriod, Expired, Suspended, Revoked, PendingActivation
│   │   ├── DeploymentModel.cs         ← SaaS, SelfHosted, OnPremise
│   │   ├── ActivationMode.cs          ← Online, Offline, Hybrid
│   │   ├── CommercialModel.cs         ← Perpetual, Subscription, UsageBased, Trial, Internal
│   │   ├── MeteringMode.cs            ← RealTime, Periodic, Manual, Disabled
│   │   ├── TelemetryConsentStatus.cs  ← NotRequested, Granted, Denied, Partial
│   │   ├── EnforcementLevel.cs        ← NeverBreak, Soft, Hard, Warn
│   │   └── WarningLevel.cs            ← Normal, Advisory, Warning, Critical, Exceeded
│   ├── Errors/
│   │   └── LicensingErrors.cs         ← Códigos i18n estáveis
│   └── SharedContracts/
│       ├── DTOs/                       ← DTOs públicos
│       ├── IntegrationEvents/          ← Eventos cross-módulo
│       └── ServiceInterfaces/          ← ILicensingModule
├── Application/
│   ├── Features/
│   │   ├── ActivateLicense/            ← Ativação de licença
│   │   ├── GetLicenseStatus/           ← Status completo
│   │   ├── CheckCapability/            ← Verificação de capability
│   │   ├── TrackUsageMetric/           ← Rastreamento de uso
│   │   ├── AlertLicenseThreshold/      ← Alertas de threshold
│   │   ├── GetLicenseHealth/           ← Health score
│   │   ├── VerifyLicenseOnStartup/     ← Verificação no boot
│   │   ├── StartTrial/                 ← Início de trial
│   │   ├── ExtendTrial/                ← Extensão de trial
│   │   ├── ConvertTrial/               ← Conversão para full
│   │   ├── IssueLicense/               ← Vendor: emissão
│   │   ├── RevokeLicense/              ← Vendor: revogação
│   │   ├── RehostLicense/              ← Vendor: rehost
│   │   ├── ListLicenses/               ← Vendor: listagem
│   │   ├── GetTelemetryConsent/         ← Consulta consentimento
│   │   └── UpdateTelemetryConsent/      ← Altera consentimento
│   └── Abstractions/
│       ├── ILicenseRepository.cs
│       ├── IHardwareBindingRepository.cs
│       └── IHardwareFingerprintProvider.cs
└── Infrastructure/
    ├── Endpoints/
    │   └── LicensingEndpointModule.cs  ← 16 endpoints Minimal API
    ├── Persistence/
    │   ├── LicensingDbContext.cs
    │   └── Repositories/
    └── Services/
```

## Suporte a On-Prem / Self-Hosted / SaaS

O domínio é **único** — não há duplicação por modelo de entrega. O que varia:

| Aspecto | SaaS | Self-Hosted | On-Premise |
|---------|------|-------------|------------|
| DeploymentModel | SaaS | SelfHosted | OnPremise |
| ActivationMode | Online | Hybrid | Offline |
| CommercialModel | Subscription | Subscription | Perpetual |
| MeteringMode | RealTime | Periodic | Manual/Disabled |
| Hardware Binding | Automático | Automático | Manual |
| Grace Period | Padrão | Padrão | Estendido |

## Separação de Contextos Funcionais

### 1. Tenant Licensing Experience

Frontend em `/licensing`. Permissão: `licensing:read`.

Cobertura:
- Visão geral da licença (status, tipo, edição, validade)
- Capabilities habilitadas/desabilitadas
- Quotas de uso com barras de progresso e thresholds
- Trial (status, dias restantes, extensão, conversão)
- Health score da licença
- Alertas e warnings proativos
- Consentimento de telemetria (grant total, parcial, deny — LGPD/GDPR)

### 2. Vendor Licensing Operations

Frontend em `/vendor/licensing`. Permissão: `licensing:vendor:license:read`.

Cobertura:
- Listagem paginada de todas as licenças
- Emissão de novas licenças com configuração completa
- Revogação permanente de licenças
- Rehost (migração de hardware)
- Gestão de trials
- Visão por deployment model

## Permissões

### Tenant (cliente)
- `licensing:read` — visualizar status, capabilities, quotas
- `licensing:write` — ativar licença, registrar uso

### Vendor Operations (backoffice NexTraceOne)
- `licensing:vendor:license:create` — emitir licenças
- `licensing:vendor:license:revoke` — revogar licenças
- `licensing:vendor:license:rehost` — rehost de hardware
- `licensing:vendor:license:read` — listar licenças
- `licensing:vendor:key:generate` — gerar chaves
- `licensing:vendor:trial:extend` — estender trials
- `licensing:vendor:activation:issue` — emitir ativações
- `licensing:vendor:tenant:manage` — gestão de tenants
- `licensing:vendor:telemetry:view` — visualizar telemetria

## API Endpoints

### Tenant Licensing (12 endpoints)
| Método | Path | Descrição |
|--------|------|-----------|
| POST | `/api/v1/licensing/activate` | Ativar licença |
| GET | `/api/v1/licensing/verify` | Verificar validade |
| GET | `/api/v1/licensing/status` | Status completo |
| GET | `/api/v1/licensing/capabilities/{code}` | Verificar capability |
| POST | `/api/v1/licensing/usage` | Registrar uso |
| GET | `/api/v1/licensing/thresholds` | Alertas de threshold |
| POST | `/api/v1/licensing/trial/start` | Iniciar trial |
| POST | `/api/v1/licensing/trial/extend` | Estender trial |
| POST | `/api/v1/licensing/trial/convert` | Converter trial |
| GET | `/api/v1/licensing/health` | Health score |
| GET | `/api/v1/licensing/telemetry-consent` | Consultar consentimento de telemetria |
| POST | `/api/v1/licensing/telemetry-consent` | Atualizar consentimento de telemetria |

### Vendor Operations (4 endpoints)
| Método | Path | Descrição |
|--------|------|-----------|
| POST | `/api/v1/licensing/vendor/issue` | Emitir licença |
| POST | `/api/v1/licensing/vendor/revoke` | Revogar licença |
| POST | `/api/v1/licensing/vendor/rehost` | Rehost de hardware |
| GET | `/api/v1/licensing/vendor/licenses` | Listar licenças |

## Testes

105 testes unitários cobrindo:
- **Domain** (67 testes): License aggregate, capabilities, quotas, trial, health score, revoke, rehost, TelemetryConsent, enums
- **Application** (38 testes): Handlers de ativação, status, capability, trial, vendor ops, telemetry consent (get, update grant/deny/partial, validators)

## i18n

4 idiomas suportados: en, pt-BR, pt-PT, es.

Chaves de erro seguem o padrão `Licensing.{Entidade}.{Condição}` para mapeamento direto com i18n no frontend.

## Seeds para Desenvolvimento

Scripts SQL em `database/seeds/commercial-governance/` com massa de teste cobrindo:
- 10 licenças com cenários variados
- Todos os deployment models (SaaS, Self-Hosted, On-Premise)
- Trial ativo, expirado, convertido
- Grace period
- Licença revogada
- Thresholds próximos do limite
- Consentimentos de telemetria (granted, partial, denied, not requested)

Ver `database/seeds/commercial-governance/README.md` para detalhes.

---

## Catálogo Comercial (CommercialCatalog)

### Descrição do Subdomínio

O CommercialCatalog é o subdomínio responsável pela definição e gestão do catálogo de planos comerciais, pacotes de funcionalidades (feature packs) e seus itens. Funciona como a **fonte de verdade para a oferta comercial** da plataforma, permitindo que a equipa vendor configure quais capabilities estão disponíveis em cada plano.

A relação central do subdomínio é:

```
Plan ──(N:N via PlanFeaturePackMapping)──▶ FeaturePack ──(1:N)──▶ FeaturePackItem
```

Cada `FeaturePackItem` mapeia diretamente para um **código de capability** que é verificado em runtime pelo `License.CheckCapability`. Isto permite que a composição comercial (quais funcionalidades pertencem a qual plano) seja completamente configurável sem alterações de código.

### Entidades

| Entidade | Tipo | Descrição |
|----------|------|-----------|
| `Plan` | Aggregate Root | Representa um plano comercial (ex: Community, Professional, Enterprise). Contém nome, descrição, edição e estado ativo/inativo. |
| `FeaturePack` | Aggregate Root | Agrupamento lógico de funcionalidades (ex: "Core Features", "Advanced Analytics"). Permite composição modular de capabilities. |
| `FeaturePackItem` | Entity (child de FeaturePack) | Item individual dentro de um FeaturePack. Mapeia para um código de capability (`capabilityCode`) verificado pelo `License.CheckCapability`. |
| `PlanFeaturePackMapping` | Entity (join) | Associação N:N entre Plan e FeaturePack. Permite que o mesmo FeaturePack seja reutilizado em múltiplos planos. |

### Diagrama de Relações

```
┌──────────┐       ┌──────────────────────┐       ┌───────────────┐
│   Plan   │──N:N──│ PlanFeaturePackMapping│──N:N──│  FeaturePack  │
│ (Aggr.)  │       │       (Join)          │       │   (Aggr.)     │
└──────────┘       └──────────────────────┘       └───────┬───────┘
                                                          │ 1:N
                                                  ┌───────▼───────┐
                                                  │FeaturePackItem │
                                                  │(capabilityCode)│
                                                  └───────────────┘
                                                          │
                                                          ▼
                                               License.CheckCapability
```

## Geração de Chaves (GenerateLicenseKey)

### Descrição

A feature `GenerateLicenseKey` permite à equipa vendor gerar chaves de licença criptograficamente seguras para distribuição a clientes. A geração utiliza `RandomNumberGenerator` (CSPRNG do .NET) para garantir entropia adequada e resistência a ataques de predição.

### Especificações Técnicas

| Aspecto | Detalhe |
|---------|---------|
| **Algoritmo** | `RandomNumberGenerator` (CSPRNG) |
| **Entropia** | 256-bit (32 bytes aleatórios) |
| **Formato** | `NXKEY-XXXX-XXXX-XXXX-XXXX` |
| **Charset** | Alfanumérico maiúsculo (A-Z, 0-9) |
| **Permissão** | `licensing:vendor:license:manage` |

### Fluxo

1. Vendor autenticado envia `POST /api/v1/licensing/vendor/generate-key`
2. Handler valida permissão `licensing:vendor:license:manage`
3. `RandomNumberGenerator` gera 32 bytes aleatórios
4. Bytes são codificados no formato `NXKEY-XXXX-XXXX-XXXX-XXXX`
5. Chave é retornada ao vendor (não é persistida — o vendor associa à licença no momento da emissão)

## Novas Permissões do Catálogo

As seguintes permissões foram adicionadas para controlo de acesso ao catálogo comercial:

| Permissão | Descrição |
|-----------|-----------|
| `licensing:vendor:plan:create` | Criar novos planos comerciais |
| `licensing:vendor:plan:read` | Consultar planos existentes |
| `licensing:vendor:featurepack:create` | Criar novos feature packs e seus itens |
| `licensing:vendor:featurepack:read` | Consultar feature packs existentes |
| `licensing:vendor:license:manage` | Gestão avançada de licenças (inclui geração de chaves) |

Todas as permissões pertencem ao contexto **Vendor Operations** e requerem autenticação com role de administrador ou operador comercial.

## Endpoints do Catálogo

### Vendor Catalog (5 endpoints)

| Método | Path | Permissão | Descrição |
|--------|------|-----------|-----------|
| POST | `/api/v1/licensing/vendor/plans` | `licensing:vendor:plan:create` | Criar novo plano comercial |
| GET | `/api/v1/licensing/vendor/plans` | `licensing:vendor:plan:read` | Listar planos comerciais (paginado) |
| POST | `/api/v1/licensing/vendor/feature-packs` | `licensing:vendor:featurepack:create` | Criar novo feature pack com itens |
| GET | `/api/v1/licensing/vendor/feature-packs` | `licensing:vendor:featurepack:read` | Listar feature packs com itens (paginado) |
| POST | `/api/v1/licensing/vendor/generate-key` | `licensing:vendor:license:manage` | Gerar chave de licença criptograficamente segura |

### Exemplos de Payload

**Criar Plano:**
```json
{
  "name": "Professional",
  "description": "Plano para equipas de desenvolvimento",
  "edition": "Professional",
  "featurePackIds": ["<featurepack-id-1>", "<featurepack-id-2>"]
}
```

**Criar Feature Pack:**
```json
{
  "name": "Core Features",
  "description": "Funcionalidades base da plataforma",
  "items": [
    { "capabilityCode": "catalog:import", "description": "Importação de contratos" },
    { "capabilityCode": "catalog:diff", "description": "Diff semântico de contratos" }
  ]
}
```

**Resposta Generate Key:**
```json
{
  "licenseKey": "NXKEY-A7K2-M9PX-R4WL-B6QT"
}
```

## Frontend — Catálogo Comercial

### Novas Tabs

A secção Vendor (`/vendor/licensing`) foi expandida com três novos separadores:

| Tab | Rota | Descrição |
|-----|------|-----------|
| **Plans** | `/vendor/licensing/plans` | Listagem e criação de planos comerciais com associação de feature packs |
| **Feature Packs** | `/vendor/licensing/feature-packs` | Listagem e criação de feature packs com gestão de itens (capabilities) |
| **Generate Key** | `/vendor/licensing/generate-key` | Interface para geração de chaves de licença com cópia para clipboard |

### i18n

Chaves de tradução adicionadas no namespace `vendorCatalog.*` nos 4 idiomas suportados (en, pt-BR, pt-PT, es):

```
vendorCatalog.plans.title
vendorCatalog.plans.create
vendorCatalog.plans.name
vendorCatalog.plans.description
vendorCatalog.plans.edition
vendorCatalog.plans.featurePacks
vendorCatalog.plans.empty
vendorCatalog.featurePacks.title
vendorCatalog.featurePacks.create
vendorCatalog.featurePacks.name
vendorCatalog.featurePacks.description
vendorCatalog.featurePacks.items
vendorCatalog.featurePacks.capabilityCode
vendorCatalog.featurePacks.empty
vendorCatalog.generateKey.title
vendorCatalog.generateKey.generate
vendorCatalog.generateKey.copyToClipboard
vendorCatalog.generateKey.copied
vendorCatalog.generateKey.description
```

## SQL Seeds — Catálogo Comercial

Scripts de seed adicionados em `database/seeds/commercial-governance/` para popular o catálogo em ambientes de desenvolvimento e teste:

### 06-seed-plans.sql (5 planos)

| Plano | Edição | Descrição |
|-------|--------|-----------|
| Community | Community | Plano gratuito com funcionalidades base |
| Professional | Professional | Para equipas de desenvolvimento |
| Enterprise | Enterprise | Para organizações com governança avançada |
| Unlimited | Unlimited | Acesso completo a todas as funcionalidades |
| Trial | Professional | Plano de avaliação com duração limitada |

### 07-seed-feature-packs.sql (3 packs + 14 itens)

| Feature Pack | Itens | Capabilities |
|--------------|-------|-------------|
| **Core Features** | 5 itens | `catalog:import`, `catalog:diff`, `catalog:browse`, `change:create`, `change:view` |
| **Governance Features** | 5 itens | `workflow:create`, `workflow:approve`, `ruleset:manage`, `promotion:request`, `promotion:approve` |
| **Intelligence Features** | 4 itens | `blast-radius:calculate`, `change-score:compute`, `ai:consult`, `audit:export` |

### 08-seed-plan-featurepack-mappings.sql (11 mapeamentos)

| Plano | Feature Packs Associados |
|-------|--------------------------|
| Community | Core Features |
| Professional | Core Features, Governance Features |
| Enterprise | Core Features, Governance Features, Intelligence Features |
| Unlimited | Core Features, Governance Features, Intelligence Features |
| Trial | Core Features, Governance Features |

Esta estrutura de seeds permite testar cenários de licenciamento com diferentes combinações de plano e capabilities desde o primeiro momento de desenvolvimento.
