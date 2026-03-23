# Wave 3 — Security and Operations Hardening

## Escopo da Onda

A Onda 3 é uma onda de **hardening estrutural** focada em segurança e operação enterprise. Não é uma onda funcional de negócio — é de endurecimento da fundação operacional.

## Gaps Tratados

| GAP | Descrição | Estado |
|-----|-----------|--------|
| GAP-010 | EncryptionInterceptor documentado mas ausente | ✅ Resolvido |
| GAP-015 | Rate limiting apenas em auth | ✅ Resolvido |
| GAP-016 | GetPlatformHealth com subsistemas hardcoded | ✅ Resolvido |
| GAP-021 | CORS sem fechamento por ambiente | ✅ Resolvido |
| GAP-022 | Alerting sem integração com incidentes | ✅ Resolvido |

## Alterações Realizadas

### 1. Encryption At-Rest (GAP-010)
- Criado `EncryptedFieldAttribute` em `BuildingBlocks.Core.Attributes`
- Implementada convenção automática em `NexTraceDbContextBase.OnModelCreating` que aplica `EncryptedStringConverter` (AES-256-GCM) a todas as propriedades marcadas com `[EncryptedField]`
- Aplicado a `EnvironmentIntegrationBinding.BindingConfigJson` (campo que armazena credenciais de integração)
- A encriptação é transparente: cifra ao persistir, decifra ao ler

### 2. Rate Limiting Expansion (GAP-015)
- Adicionadas 3 novas políticas: `ai` (30 req/min), `data-intensive` (50 req/min), `operations` (40 req/min)
- Aplicada política `ai` a endpoints de AI Runtime, Orchestration e ExternalAI
- Aplicada política `data-intensive` a endpoints de Catalog, Reports e FinOps
- Aplicada política `operations` a endpoints de Incidents, Reliability e Runtime Intelligence

### 3. Platform Health Real (GAP-016)
- Criada abstração `IPlatformHealthProvider` na camada Application
- Implementado `HealthCheckPlatformHealthProvider` no ApiHost usando `HealthCheckService` real
- `GetPlatformHealth` agora consulta health checks reais em vez de retornar valores hardcoded
- Subsistemas sem fonte real (BackgroundJobs, Ingestion) reportam `Unknown` em vez de `Healthy` fake

### 4. CORS Per-Environment (GAP-021)
- Adicionada validação que exige origens CORS explícitas em ambientes não-Development/CI
- Em Staging/Production, fallback para localhost é bloqueado com `InvalidOperationException`
- Documentação inline explica como configurar via `Cors:AllowedOrigins` ou env vars

### 5. Alerting → Incidents Integration (GAP-022)
- Criada interface `IOperationalAlertHandler` no building block de Observability
- Implementado `IncidentAlertHandler` que cria incidentes automaticamente para alertas Error/Critical
- `AlertGateway` agora invoca o handler após dispatch para canais
- Mapeamento automático de severidade e tipo de incidente a partir do alerta

## Impacto na Maturidade Enterprise

- **Proteção de dados**: campos sensíveis protegidos em repouso com AES-256-GCM
- **Proteção contra abuso**: superfície da API protegida com rate limiting por categoria
- **Honestidade operacional**: saúde da plataforma baseada em dados reais
- **Segurança CORS**: política fechada por ambiente, sem defaults inseguros em produção
- **Resposta operacional**: alertas conectados ao fluxo de incidentes

## Testes Adicionados

- 35 testes de encriptação, rate limiting e CORS (Infrastructure.Tests)
- 11 testes de integração alerting→incidentes (OperationalIntelligence.Tests)
- 4 testes de GetPlatformHealth com health provider real (Governance.Tests)
- Total: **50 novos testes** cobrindo as mudanças desta onda
