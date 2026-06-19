# NexTrace.Sdk

Cliente .NET oficial para a plataforma NexTraceOne.

## Instalação

```bash
dotnet add package NexTrace.Sdk
```

## Uso rápido

```csharp
using NexTrace.Sdk;

var client = new NexTraceSdkClient(new NexTraceSdkOptions
{
    BaseUrl = "https://api.nextraceone.example.com",
    ApiToken = "seu-token-aqui",
    RetryCount = 2
});

// Catálogo de serviços
var service = await client.Services.GetServiceAsync("payments");
var services = await client.Services.ListServicesAsync("platform-team");

// Criar serviço
var created = await client.Services.CreateServiceAsync(new CreateServiceRequest
{
    Name = "orders",
    Team = "platform-team",
    Tier = "critical"
});

// Contratos
var contract = await client.Contracts.GetContractAsync("contract-1");
var diff = await client.Contracts.DiffContractAsync("contract-1", "contract-2");
var migration = await client.Contracts.MigrationPatchAsync("contract-1", "contract-2");

// Changes
var score = await client.Changes.GetConfidenceScoreAsync("release-123");
var promotion = await client.Changes.RequestPromotionAsync(new PromotionRequestRequest
{
    ReleaseId = "release-123",
    TargetEnvironment = "production"
});

// Compliance
var coverage = await client.Compliance.CheckCoverageAsync("SOC2");

// Integrações — geração acelerada de clientes consumidores
var integration = await client.Integrations.GenerateConsumerClientAsync(new GenerateConsumerClientRequest
{
    ProviderName = "payments-api",
    ConsumerName = "orders-consumer",
    RootNamespace = "OrdersConsumer"
});

foreach (var contract in integration?.GeneratedContracts ?? [])
{
    Console.WriteLine($"Generated {contract.Files.Count} files for {contract.ApiName}");
}

// Registar relação de consumo
var relationship = await client.Integrations.RegisterConsumerAsync(new RegisterConsumerRequest
{
    ApiAssetId = "api-asset-guid",
    ConsumerName = "orders-consumer",
    ConsumerKind = "Service",
    ConsumerEnvironment = "Production"
});
```

## CLI `nex integration`

The `nex` CLI exposes the same acceleration flow for command-line usage:

```bash
# Generate a typed consumer client for a provider service
nex integration scaffold \
  --provider payments-api \
  --consumer orders-consumer \
  --namespace OrdersConsumer \
  --output ./generated

# Filter to specific paths or operationIds
nex integration scaffold \
  --provider payments-api \
  --consumer orders-consumer \
  --routes "/api/v1/payments,listRefunds"

# Generate and auto-register the consumer relationship
nex integration scaffold \
  --provider payments-api \
  --consumer orders-consumer \
  --register \
  --confidence 0.95

# Register a consumer relationship manually
nex integration register \
  --provider-api <api-asset-guid> \
  --consumer orders-consumer
```

Notes:
- Only `csharp` is supported by the current backend generator. Other values for `--lang` will be rejected.
- `--routes` filters the OpenAPI spec before code generation, keeping only matching paths or operationIds.
- `--confidence` is used when `--register` is enabled; it must be between `0.01` and `1.0`.

## Resiliência

O SDK usa `Microsoft.Extensions.Http.Resilience` para retry automático em falhas transitórias (5xx, timeout). Configure via `RetryCount` e `RetryDelaySeconds`.

## Testes

```bash
dotnet test tests/tools/NexTrace.Sdk.Tests/NexTrace.Sdk.Tests.csproj
```
