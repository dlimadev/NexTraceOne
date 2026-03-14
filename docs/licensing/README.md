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
