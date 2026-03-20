# Fase 5 — Integration Bindings

## Visão Geral

O sistema de Integration Bindings estabelece como o NexTraceOne gerencia as configurações de integração (endpoints, brokers, webhooks) de forma isolada por tenant e ambiente, sem hardcoding e com validação de segurança de prod vs. não-prod.

---

## Problema que Resolve

Em plataformas multi-tenant com múltiplos ambientes, cada tenant pode ter:

- Kafka próprio (diferente por ambiente)
- Webhooks específicos (diferente por ambiente)
- ITSM endpoints (sandbox vs. produção)
- IdP (dev, staging, prod)

Sem um sistema de bindings, o código teria URLs e configurações hardcoded ou em configurações globais, tornando impossível o isolamento por tenant/ambiente.

---

## Componentes

### IntegrationBindingDescriptor

**Localização:** `BuildingBlocks.Application.Integrations`

Representa a configuração de uma integração para um tenant+ambiente:

```csharp
public sealed record IntegrationBindingDescriptor
{
    public required Guid BindingId { get; init; }
    public required Guid TenantId { get; init; }
    public Guid? EnvironmentId { get; init; }     // null = binding global do tenant
    public required string IntegrationType { get; init; }  // "kafka", "http", "itsm"
    public required string BindingName { get; init; }
    public required string Endpoint { get; init; }
    public bool IsProductionBinding { get; init; }
    public bool IsActive { get; init; }
    public string? MetadataJson { get; init; }
}
```

**Tipos de integração suportados (string convention):**

| Tipo | Exemplo de Endpoint |
|---|---|
| `kafka` | `kafka-qa.tenantabc.internal:9092` |
| `http` | `https://api.tenantabc.com/webhooks` |
| `webhook` | `https://hooks.tenantabc.com/events` |
| `itsm` | `https://itsm.tenantabc.com/api/v2` |
| `idp` | `https://auth.tenantabc.com` |
| `k8s` | `https://k8s-api.tenantabc.internal` |

### IIntegrationContextResolver

**Localização:** `BuildingBlocks.Application.Integrations`

Interface para resolver o binding correto para o contexto atual:

```csharp
public interface IIntegrationContextResolver
{
    Task<IntegrationBindingDescriptor?> ResolveAsync(
        string integrationType, Guid tenantId, Guid? environmentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IntegrationBindingDescriptor>> ListActiveBindingsAsync(
        Guid tenantId, Guid? environmentId,
        CancellationToken cancellationToken = default);

    Task<bool> HasActiveBindingAsync(
        string integrationType, Guid tenantId, Guid? environmentId,
        CancellationToken cancellationToken = default);
}
```

### NullIntegrationContextResolver (Stub)

Implementação padrão registrada em BuildingBlocks — retorna null/vazio para todos os bindings.

**Regra de substituição:** O módulo de Integrações (futuro) registra uma implementação concreta que lê da base de dados:

```csharp
// No DI do módulo de integrações (futuro)
services.AddScoped<IIntegrationContextResolver, DbIntegrationContextResolver>();
```

---

## Política de Segurança

### Regra principal

> Um binding de produção NUNCA deve ser retornado para um ambiente não-produtivo, e vice-versa, a menos que a política do tenant/ambiente explicitamente permita (sandbox controlado).

Esta regra é responsabilidade da implementação concreta do `IIntegrationContextResolver`, não da interface.

### Fluxo de resolução (implementação futura)

```
ResolveAsync("kafka", tenantId, environmentId)
    │
    ├── Busca bindings ativos por tenant + ambiente
    ├── Se ambiente é produção:
    │       └── Filtra apenas IsProductionBinding = true
    ├── Se ambiente é não-produção:
    │       └── Filtra apenas IsProductionBinding = false
    │           (a menos que tenant tenha política de sandbox)
    └── Retorna primeiro binding ativo encontrado
```

---

## Registro no DI

Registrado em `AddBuildingBlocksApplication()`:

```csharp
// Fase 5: Contexto distribuído — integração e correlação
services.AddScoped<IIntegrationContextResolver, NullIntegrationContextResolver>();
```

A implementação concreta substitui via `services.AddScoped<IIntegrationContextResolver, ConcreteImpl>()` registrado depois — o último registration vence no DI padrão do .NET.

---

## Uso Esperado (handlers de módulos)

```csharp
public sealed class PublishEventToKafkaHandler(
    IIntegrationContextResolver resolver,
    ICurrentTenant tenant,
    ICurrentEnvironment environment)
{
    public async Task Handle(PublishEventCommand command, CancellationToken ct)
    {
        var binding = await resolver.ResolveAsync("kafka", tenant.Id, environment.EnvironmentId, ct);
        if (binding is null)
            throw new InvalidOperationException("No Kafka binding for tenant/environment");

        // usa binding.Endpoint para conectar ao Kafka correto
    }
}
```
