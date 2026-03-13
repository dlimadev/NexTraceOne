# Licensing & Entitlements — API de Integração Externa

## Visão Geral

O módulo Licensing & Entitlements do NexTraceOne expõe uma API REST para
gestão de licenças, capacidades (capabilities), quotas de utilização, trials
e health monitoring. Permite integração com sistemas de billing, provisioning
e monitorização externos.

Todas as APIs seguem o padrão REST sob `/api/v1/licensing/` e utilizam
JWT Bearer ou API Key para autenticação.

## Autenticação e Autorização

### Mecanismos Suportados

| Mecanismo | Header | Descrição |
|-----------|--------|-----------|
| JWT Bearer | `Authorization: Bearer <token>` | Autenticação via token JWT do módulo Identity |
| API Key | `X-Api-Key: <key>` | Autenticação sistema-a-sistema para integração automatizada |

### API Key (Sistema-a-Sistema)

Para integrações automatizadas (ex: sistema de billing), configurar API Keys
em `Security:ApiKeys` com as permissões apropriadas:

```json
{
  "Security": {
    "ApiKeys": [
      {
        "ClientId": "billing-system",
        "ClientName": "Billing Integration",
        "Key": "<api-key-value>",
        "TenantId": "TENANT_UUID",
        "Permissions": ["licensing:read", "licensing:write"]
      }
    ]
  }
}
```

## Endpoints

### Gestão de Licença

| Método | Rota | Descrição |
|--------|------|-----------|
| POST | `/activate` | Activar licença com chave e fingerprint de hardware |
| GET | `/verify?licenseKey={key}` | Verificar validade da licença (usado no arranque da aplicação) |
| GET | `/status?licenseKey={key}` | Obter estado completo da licença |
| GET | `/capabilities/{capabilityCode}?licenseKey={key}` | Verificar se uma capability está activa |
| GET | `/health?licenseKey={key}` | Obter health score da licença |
| GET | `/thresholds?licenseKey={key}` | Obter thresholds de utilização actuais |

### Tracking de Utilização

| Método | Rota | Descrição |
|--------|------|-----------|
| POST | `/usage` | Registar métrica de utilização |

### Trial Management

| Método | Rota | Descrição |
|--------|------|-----------|
| POST | `/trial/start` | Iniciar período trial |
| POST | `/trial/extend` | Estender trial activo |
| POST | `/trial/convert` | Converter trial em licença paga |

## Detalhes dos Endpoints

### POST `/activate`

Activa uma licença associando-a a um hardware fingerprint.

**Request:**
```json
{
  "licenseKey": "NXTRACE-XXXX-XXXX-XXXX",
  "hardwareFingerprint": "hw-fingerprint-hash",
  "customerName": "Empresa XYZ"
}
```

**Response (200 OK):**
```json
{
  "licenseId": "UUID",
  "licenseKey": "NXTRACE-XXXX-XXXX-XXXX",
  "activatedAt": "2026-03-13T10:00:00Z",
  "expiresAt": "2027-03-13T10:00:00Z",
  "edition": "Enterprise",
  "capabilities": ["api-catalog", "blast-radius", "workflow-approval"]
}
```

### GET `/status?licenseKey={key}`

Retorna estado completo da licença incluindo capabilities e quotas.

**Response (200 OK):**
```json
{
  "licenseId": "UUID",
  "licenseKey": "NXTRACE-XXXX-XXXX-XXXX",
  "customerName": "Empresa XYZ",
  "isActive": true,
  "isExpired": false,
  "isInGracePeriod": false,
  "expiresAt": "2027-03-13T10:00:00Z",
  "daysRemaining": 365,
  "licenseType": "Commercial",
  "edition": "Enterprise",
  "isTrial": false,
  "trialConverted": false,
  "gracePeriodDays": 30,
  "capabilities": [
    { "code": "api-catalog", "name": "API Catalog", "isEnabled": true },
    { "code": "blast-radius", "name": "Blast Radius Analysis", "isEnabled": true }
  ],
  "usageQuotas": [
    {
      "metricCode": "api-imports",
      "currentUsage": 45,
      "limit": 100,
      "thresholdReached": false,
      "usagePercentage": 45.0,
      "warningLevel": "Normal",
      "enforcementLevel": "Warn"
    }
  ],
  "activationCount": 1
}
```

### GET `/capabilities/{capabilityCode}?licenseKey={key}`

Verifica se uma capability específica está activa para a licença.

**Response (200 OK):**
```json
{
  "code": "blast-radius",
  "name": "Blast Radius Analysis",
  "isEnabled": true
}
```

### POST `/usage`

Regista uma métrica de utilização para tracking de quotas.

**Request:**
```json
{
  "licenseKey": "NXTRACE-XXXX-XXXX-XXXX",
  "metricCode": "api-imports",
  "quantity": 1
}
```

### POST `/trial/start`

Inicia um período trial de avaliação.

**Request:**
```json
{
  "customerName": "Empresa Demo",
  "customerEmail": "demo@empresa.com",
  "edition": "Enterprise"
}
```

**Response (200 OK):**
```json
{
  "licenseId": "UUID",
  "licenseKey": "NXTRACE-TRIAL-XXXX-XXXX",
  "trialDays": 30,
  "expiresAt": "2026-04-13T10:00:00Z",
  "capabilities": ["api-catalog", "blast-radius"]
}
```

## Contratos Públicos (DTOs)

### LicenseStatusDto
Estado completo da licença com capabilities e quotas.

### CapabilityStatusDto
```json
{ "code": "string", "name": "string", "isEnabled": true }
```

### UsageQuotaDto
```json
{
  "metricCode": "string",
  "currentUsage": 0,
  "limit": 100,
  "thresholdReached": false,
  "usagePercentage": 0.0,
  "warningLevel": "Normal",
  "enforcementLevel": "Warn"
}
```

## Integration Events (Outbox Pattern)

| Evento | Campos | Descrição |
|--------|--------|-----------|
| `LicenseActivatedIntegrationEvent` | `LicenseId`, `LicenseKey`, `HardwareFingerprint` | Publicado quando uma licença é activada |
| `LicenseThresholdReachedIntegrationEvent` | `LicenseId`, `LicenseKey`, `MetricCode`, `CurrentUsage`, `Limit` | Publicado quando um threshold de quota é atingido |

## Interface Cross-Module

```csharp
public interface ILicensingModule
{
    Task<bool> IsLicenseActiveAsync(string licenseKey, CancellationToken ct);
    Task<bool> HasCapabilityAsync(string licenseKey, string capabilityCode, CancellationToken ct);
    Task<LicenseStatusDto?> GetLicenseStatusAsync(string licenseKey, CancellationToken ct);
    Task TrackUsageAsync(string licenseKey, string metricCode, long quantity, CancellationToken ct);
    Task<IReadOnlyList<string>> GetActiveCapabilitiesAsync(string licenseKey, CancellationToken ct);
}
```

## Enforcement Automático (Pipeline)

O NexTraceOne inclui um `LicenseCapabilityBehavior` no pipeline MediatR que
verifica automaticamente capabilities antes de executar handlers.

Commands/Queries que implementem `IRequiresCapability` terão a capability
verificada antes da execução:

```csharp
public interface IRequiresCapability
{
    string RequiredCapability { get; }
}
```

Se a capability não estiver activa, o pipeline retorna `Error.Forbidden`
com código `Licensing.Capability.NotEnabled`.

## Códigos de Erro (i18n)

| Código | Descrição |
|--------|-----------|
| `Licensing.License.NotFound` | Licença não encontrada |
| `Licensing.License.Expired` | Licença expirada |
| `Licensing.License.Inactive` | Licença desactivada |
| `Licensing.License.AlreadyActivated` | Licença já activada |
| `Licensing.Capability.NotEnabled` | Capability não disponível na edição |
| `Licensing.Quota.Exceeded` | Quota de utilização excedida |
| `Licensing.Trial.AlreadyStarted` | Trial já iniciado |
| `Licensing.Trial.Expired` | Trial expirado |
| `Licensing.HardwareBinding.Mismatch` | Hardware fingerprint não corresponde |

## Códigos HTTP

| Código | Descrição |
|--------|-----------|
| 200 | Operação bem sucedida |
| 400 | Payload inválido |
| 401 | Não autenticado |
| 403 | Sem permissão ou capability não activa |
| 404 | Licença não encontrada |
| 409 | Conflito (ex: licença já activada) |
| 429 | Rate limit excedido (100 req/min por IP) |

## Exemplos de Integração

### Verificar Licença (curl)

```bash
# Verificar estado da licença
curl -s "http://localhost:5000/api/v1/licensing/status?licenseKey=NXTRACE-XXXX-XXXX-XXXX" \
  -H "Authorization: Bearer $TOKEN" | jq .

# Verificar capability específica
curl -s "http://localhost:5000/api/v1/licensing/capabilities/blast-radius?licenseKey=NXTRACE-XXXX-XXXX-XXXX" \
  -H "Authorization: Bearer $TOKEN" | jq .
```

### Integração com Sistema de Billing (Python)

```python
import requests

BASE_URL = "http://localhost:5000/api/v1/licensing"
HEADERS = {
    "Content-Type": "application/json",
    "X-Api-Key": "<billing-system-api-key>",
}

# Activar licença
response = requests.post(
    f"{BASE_URL}/activate",
    headers=HEADERS,
    json={
        "licenseKey": "NXTRACE-XXXX-XXXX-XXXX",
        "hardwareFingerprint": "hw-hash-abc123",
        "customerName": "Empresa XYZ",
    },
)
result = response.json()
print(f"License activated: {result['licenseId']}, expires: {result['expiresAt']}")

# Tracking de utilização
requests.post(
    f"{BASE_URL}/usage",
    headers=HEADERS,
    json={
        "licenseKey": "NXTRACE-XXXX-XXXX-XXXX",
        "metricCode": "api-imports",
        "quantity": 1,
    },
)
```

## Modelo de Licenciamento

### Tipos de Licença
- **Trial**: Avaliação gratuita com prazo limitado (30 dias por defeito)
- **Commercial**: Licença paga com todas as capabilities da edição
- **Internal**: Licença interna para uso próprio

### Edições
- **Community**: Funcionalidades básicas
- **Professional**: Funcionalidades avançadas (blast radius, workflow)
- **Enterprise**: Todas as funcionalidades (BreakGlass, AccessReview, AI)

### Enforcement Levels
- **None**: Sem enforcement, apenas informativo
- **Warn**: Warning proactivo quando threshold atingido
- **SoftLimit**: Permite operação mas com degradação
- **HardLimit**: Bloqueia operação quando limite excedido

## Auditabilidade

- Activações de licença registadas como SecurityEvents
- Tracking de utilização auditável por tenant
- Thresholds atingidos geram Integration Events para Audit
- Todas as operações passam pelo AuditInterceptor

## Observabilidade

- Logs em inglês com structured logging (Serilog)
- Tracing OpenTelemetry para activações e verificações
- Health score da licença disponível via `/health`
- Métricas de utilização por capability/quota
