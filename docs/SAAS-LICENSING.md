# NexTraceOne — Modelo de Licenciamento

O NexTraceOne usa o modelo **Host Units** inspirado no Dynatrace Full-Stack Monitoring — previsível, baseado em recursos do host, não em volume de dados ingeridos.

---

## 1. O Que é um Host Unit (HU)

Um Host Unit representa a "pegada" de um host monitorado, calculado com base nos recursos provisionados:

```
HU = max(RAM_GB / 8, vCPUs / 4)
```

Arredondado para o múltiplo de 0.5 mais próximo, com mínimo de 0.5 HU.

### Exemplos

| Host | RAM | vCPUs | Cálculo | HU |
|---|---|---|---|---|
| Dev VM pequena | 4 GB | 2 | max(4/8, 2/4) = max(0.5, 0.5) | **0.5** |
| App server típico | 16 GB | 4 | max(16/8, 4/4) = max(2.0, 1.0) | **2.0** |
| Database server | 64 GB | 8 | max(64/8, 8/4) = max(8.0, 2.0) | **8.0** |
| K8s node grande | 128 GB | 32 | max(128/8, 32/4) = max(16.0, 8.0) | **16.0** |
| Container (1/4 node) | — | — | Calculado no nó físico, não no pod | per-node |

**Nota para K8s**: O HU é calculado por nó físico (node), não por pod. Um nó com 16 GB e 4 vCPUs = 2 HU, independentemente de quantos pods correm nele.

---

## 2. Planos e Preços

### SaaS

| Plano | HU incluídas | APM | Infra | AI Governance | Suporte | Preço base |
|---|---|---|---|---|---|---|
| **Starter** | 5 HU | ✅ | ✅ | ❌ | Comunidade | €75/mês |
| **Professional** | 20 HU | ✅ | ✅ | ✅ | Email 8x5 | €440/mês |
| **Enterprise** | Ilimitado | ✅ | ✅ | ✅ + SLA | 24x7 + TAM | Negociado |

HU adicionais além das incluídas:
- Starter: €18/HU/mês
- Professional: €22/HU/mês
- Enterprise: negociado (desconto por volume)

### Self-hosted / On-Premise

| Tipo | Modelo | Suporte |
|---|---|---|
| **Team** | €15/HU/mês (anual) | Email |
| **Business** | €20/HU/mês (anual) | SLA 4h 8x5 |
| **Enterprise** | Perpétua + 20% manutenção/ano | SLA 1h 24x7 + onboarding |

---

## 3. Entidades de Domínio

### `AgentRegistration`

Registra cada NexTrace Agent que reporta ao servidor. Persiste entre restarts via `host_unit_id` (UUID estável no host).

```csharp
// src/modules/licensing/NexTraceOne.Licensing.Domain/Entities/AgentRegistration.cs
public class AgentRegistration : Entity<Guid>
{
    public Guid TenantId { get; private set; }
    public string HostUnitId { get; private set; }       // UUID estável por host
    public string Hostname { get; private set; }
    public double HostUnitCount { get; private set; }    // calculado: max(RAM/8, vCPU/4)
    public double RamGb { get; private set; }
    public int VCpus { get; private set; }
    public string AgentVersion { get; private set; }
    public string OperatingSystem { get; private set; }  // linux, windows
    public string CollectionMode { get; private set; }   // OpenTelemetryCollector, ClrProfiler
    public DateTimeOffset LastHeartbeatAt { get; private set; }
    public bool IsActive { get; private set; }

    public static double CalculateHostUnits(double ramGb, int vCpus)
    {
        var raw = Math.Max(ramGb / 8.0, vCpus / 4.0);
        return Math.Round(raw * 2, MidpointRounding.AwayFromZero) / 2.0; // arredonda a 0.5
    }

    public void RecordHeartbeat(double ramGb, int vCpus, string agentVersion)
    {
        RamGb = ramGb;
        VCpus = vCpus;
        AgentVersion = agentVersion;
        HostUnitCount = CalculateHostUnits(ramGb, vCpus);
        LastHeartbeatAt = DateTimeOffset.UtcNow;
        IsActive = true;
    }

    public void MarkInactive()
    {
        IsActive = false;
    }
}
```

### `TenantLicense`

Representa a licença ativa de um tenant, incluindo o plano, HU contratadas e capabilities desbloqueadas.

```csharp
// src/modules/licensing/NexTraceOne.Licensing.Domain/Entities/TenantLicense.cs
public class TenantLicense : AggregateRoot<Guid>
{
    public Guid TenantId { get; private set; }
    public LicensePlan Plan { get; private set; }
    public double ContractedHostUnits { get; private set; }
    public double UsedHostUnits { get; private set; }       // calculado pelo job
    public IReadOnlyList<string> Capabilities { get; private set; }
    public DateTimeOffset ValidFrom { get; private set; }
    public DateTimeOffset ValidUntil { get; private set; }
    public bool IsOverLimit => UsedHostUnits > ContractedHostUnits * 1.1; // 10% grace

    public static TenantLicense CreateStarter(Guid tenantId) =>
        new()
        {
            TenantId = tenantId,
            Plan = LicensePlan.Starter,
            ContractedHostUnits = 5,
            Capabilities = new[] { "apm", "infrastructure" },
            ValidFrom = DateTimeOffset.UtcNow,
            ValidUntil = DateTimeOffset.UtcNow.AddMonths(1)
        };

    public static TenantLicense CreateProfessional(Guid tenantId, double hostUnits) =>
        new()
        {
            TenantId = tenantId,
            Plan = LicensePlan.Professional,
            ContractedHostUnits = hostUnits,
            Capabilities = new[] { "apm", "infrastructure", "ai-governance", "service-contracts" },
            ValidFrom = DateTimeOffset.UtcNow,
            ValidUntil = DateTimeOffset.UtcNow.AddMonths(1)
        };

    public void RecalculateUsage(double totalActiveHostUnits)
    {
        UsedHostUnits = totalActiveHostUnits;
        AddDomainEvent(new LicenseUsageRecalculatedEvent(TenantId, UsedHostUnits, ContractedHostUnits));
    }
}

public enum LicensePlan { Starter, Professional, Enterprise }
```

---

## 4. `LicenseRecalculationJob`

Job agendado (Quartz.NET) que roda a cada hora para recalcular o uso de HU por tenant.

```csharp
// src/modules/licensing/NexTraceOne.Licensing.Application/Jobs/LicenseRecalculationJob.cs
[DisallowConcurrentExecution]
public class LicenseRecalculationJob : IJob
{
    private readonly ILicensingRepository _repo;
    private readonly ILogger<LicenseRecalculationJob> _logger;

    public async Task Execute(IJobExecutionContext context)
    {
        // Agents sem heartbeat há mais de 1 hora são marcados como inativos
        var staleThreshold = DateTimeOffset.UtcNow.AddHours(-1);
        await _repo.MarkStaleAgentsInactiveAsync(staleThreshold);

        // Recalcula HU por tenant
        var tenants = await _repo.GetTenantsWithActiveAgentsAsync();
        foreach (var tenantId in tenants)
        {
            var agents = await _repo.GetActiveAgentsAsync(tenantId);
            var totalHu = agents.Sum(a => a.HostUnitCount);

            var license = await _repo.GetTenantLicenseAsync(tenantId);
            license.RecalculateUsage(totalHu);
            await _repo.SaveAsync(license);

            if (license.IsOverLimit)
                _logger.LogWarning("Tenant {TenantId} over HU limit: {Used}/{Contracted}",
                    tenantId, license.UsedHostUnits, license.ContractedHostUnits);
        }
    }
}
```

Registro do job:

```csharp
services.AddQuartz(q =>
{
    var key = new JobKey("LicenseRecalculation");
    q.AddJob<LicenseRecalculationJob>(opts => opts.WithIdentity(key));
    q.AddTrigger(opts => opts
        .ForJob(key)
        .WithSimpleSchedule(s => s.WithIntervalInHours(1).RepeatForever()));
});
```

---

## 5. Integração com `HasCapability()`

O fluxo completo para que `_currentTenant.HasCapability("ai-governance")` funcione:

### Passo 1 — Agent Heartbeat (NexTrace Agent → Ingestion API)

```json
POST /v1/agent/heartbeat
Authorization: ApiKey <tenant-api-key>

{
  "host_unit_id": "550e8400-e29b-41d4-a716-446655440000",
  "hostname": "prod-app-01",
  "ram_gb": 16.0,
  "vcpus": 4,
  "agent_version": "0.3.1",
  "os": "linux"
}
```

### Passo 2 — JWT com Capabilities

```csharp
// JwtTokenGenerator.cs (a modificar)
public async Task<string> GenerateAsync(User user, Guid tenantId)
{
    var license = await _licensingService.GetLicenseAsync(tenantId);
    var capabilities = license?.Capabilities ?? Array.Empty<string>();

    var claims = new List<Claim>
    {
        new("sub", user.Id.ToString()),
        new("tenant_id", tenantId.ToString()),
        new("tenant_slug", tenant.Slug),
        new("capabilities", JsonSerializer.Serialize(capabilities))
        // Resultado: capabilities: ["apm","infrastructure","ai-governance","service-contracts"]
    };

    return _jwtHandler.WriteToken(new JwtSecurityToken(claims: claims, expires: ...));
}
```

### Passo 3 — Middleware lê Capabilities

```csharp
// TenantResolutionMiddleware.cs (a modificar)
var capabilitiesClaim = principal.FindFirst("capabilities")?.Value;
var capabilities = string.IsNullOrEmpty(capabilitiesClaim)
    ? Array.Empty<string>()
    : JsonSerializer.Deserialize<string[]>(capabilitiesClaim);

_currentTenantAccessor.Set(tenantId, slug, name, isActive, capabilities);
// CurrentTenantAccessor já tem o Set() com IEnumerable<string>? capabilities
// e já popula _capabilities como HashSet<string>
```

### Passo 4 — Gate nas Features

```csharp
// Qualquer CommandHandler ou Endpoint premium:
public async Task<Result> Handle(CreateAiBudgetCommand command, CancellationToken ct)
{
    if (!_currentTenant.HasCapability("ai-governance"))
        return Result.Forbidden("AI Governance requires Professional plan or higher.");

    // ... lógica normal
}
```

---

## 6. Capabilities por Plano

| Capability string | Starter | Professional | Enterprise |
|---|---|---|---|
| `apm` | ✅ | ✅ | ✅ |
| `infrastructure` | ✅ | ✅ | ✅ |
| `service-contracts` | ❌ | ✅ | ✅ |
| `ai-governance` | ❌ | ✅ | ✅ |
| `change-confidence` | ❌ | ✅ | ✅ |
| `multi-tenant-hierarchy` | ❌ | ❌ | ✅ |
| `sso-custom-domain` | ❌ | ❌ | ✅ |
| `data-retention-extended` | ❌ | ❌ | ✅ |
| `audit-export` | ❌ | ❌ | ✅ |

---

## 7. Alertas de Licença

Eventos de domínio disparados pelo `LicenseRecalculationJob`:

| Evento | Threshold | Ação |
|---|---|---|
| `LicenseApproaching80Percent` | UsedHU > 80% contratado | Email ao admin do tenant |
| `LicenseApproaching100Percent` | UsedHU > 95% contratado | Email urgente + notificação in-app |
| `LicenseOverLimit` | UsedHU > 110% contratado | Email + upsell trigger |
| `LicenseExpiringSoon` | ValidUntil < 30 dias | Email de renovação |
| `LicenseExpired` | ValidUntil < agora | Degradar para modo read-only |

---

*Ver também: `SAAS-STRATEGY.md`, `NEXTTRACE-AGENT.md`, `SAAS-ROADMAP.md`*
