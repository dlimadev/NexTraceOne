# Fase 2 — Spec de implementação (Security Risk Signals + EPSS/KEV)

> **Handoff para ambiente com .NET 10 + Postgres.** Esta spec estende o que **já existe** no `changegovernance` (`ServiceRiskProfile`, `RiskSignalType`, `ComputeServiceRiskProfile`) — **não cria** um modelo novo. Inclui os pontos que exigem migration EF e verificação de runtime/RLS, que não podem ser feitos com segurança no ambiente atual (sem .NET; jobs de teste de backend do CI quebrados).

## Contexto descoberto (já existe)
- `ServiceRiskProfile` (AuditableEntity) — score global ponderado: vuln 40% / change-failure 25% / blast-radius 20% / policy 15%. Campos `VulnerabilityScore`, `ActiveSignalsJson`, etc.
- `RiskSignalType` — enum: `VulnerabilityCritical`, `HighChangeFailureRate`, `LargeBlastRadius`, `PolicyViolation`, `NoOwner`, `StaleContract`, `UnreviewedRelease`.
- `ComputeServiceRiskProfile` — feature que calcula o perfil.
- Catalog: `IServiceDependencyProfileRepository.ListWithVulnerabilitiesAsync(minSeverity)` + `PackageDependency.Vulnerabilities` (com `Severity`, `CveId`).
- OperationalIntelligence: `IVulnerabilityAdvisoryReader` → hoje `NullVulnerabilityAdvisoryReader` (honest-null).

---

## 2.2 — Ponte Catalog→Vulnerabilidades (sem migration)

**Objetivo:** substituir `NullVulnerabilityAdvisoryReader` por dados reais, sem OI tocar o DbContext do Catalog.

1. **Catalog.Contracts** — nova interface cross-module:
   ```csharp
   public interface ICatalogVulnerabilityModule
   {
       Task<IReadOnlyList<VulnerableServiceDto>> ListServicesWithHighOrCriticalAsync(
           DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default);
   }
   public sealed record VulnerableServiceDto(Guid ServiceId, string ServiceName, int CriticalCount, int HighCount, DateTimeOffset LastScanAt);
   ```
2. **Catalog.Infrastructure** — `CatalogVulnerabilityModuleService` implementa, consultando perfis com `LastScanAt ∈ [from,to]` e vulns Critical/High, juntando o nome do serviço. Respeitar **tenant/RLS** (o serviço roda com contexto de tenant). Registar no DI do Catalog.
3. **OperationalIntelligence.Infrastructure** — `CatalogVulnerabilityAdvisoryReader : IVulnerabilityAdvisoryReader` consome `ICatalogVulnerabilityModule` e mapeia para nomes de serviço. Trocar o registo DI: `NullVulnerabilityAdvisoryReader` → real.
4. **Verificação:** testes de handler (OI) com `ICatalogVulnerabilityModule` mockado; nada de migration (lê tabelas `cat_*` existentes).

---

## 2.3 — Enriquecimento EPSS/KEV (air-gap-friendly)

**Objetivo:** priorizar CVEs por exploitabilidade (EPSS = probabilidade; KEV = explorada ativamente).

1. **Application port** (catalog DependencyGovernance):
   ```csharp
   public interface IExploitabilityEnricher
   {
       Task<ExploitabilityScore> GetAsync(string cveId, CancellationToken ct = default);
   }
   public sealed record ExploitabilityScore(string CveId, double? EpssProbability, bool KnownExploited, bool IsOffline);
   ```
2. **Infrastructure impl** `EpssKevExploitabilityEnricher`:
   - EPSS: `GET https://api.first.org/data/v1/epss?cve=<id>`; KEV: catálogo CISA (JSON).
   - **Air-gap:** usar HttpClient com `AddStandardResilienceHandler()` + o `AirGapHttpMessageHandler` global; se bloqueado/offline → retornar `IsOffline=true` com valores nulos (degrada, não falha). Cache (IDistributedCache) do catálogo KEV por 24h.
   - Config: `Security:Exploitability:Enabled` (default false em air-gapped).
3. **Integração no scoring:** ajustar `ComputeServiceRiskProfile` para elevar o `VulnerabilityScore`/sinal quando há CVE em KEV ou EPSS alto (novo `RiskSignalType.KnownExploitedVulnerability`). **Requer migration** se persistir novos campos.

---

## Migrations necessárias (rodar em ambiente .NET)
- Novo `RiskSignalType.KnownExploitedVulnerability` (enum int — sem migration se só lógica).
- Se persistir EPSS/KEV no perfil ou em nova tabela `chg_security_risk_signals`:
  ```bash
  dotnet ef migrations add AddSecurityExploitabilitySignals \
    --project src/modules/changegovernance/NexTraceOne.ChangeGovernance.Infrastructure \
    --startup-project src/platform/NexTraceOne.ApiHost \
    --context ChangeGovernanceDbContext
  ```

## Superfície nos tools (Fase 1 já entregue, estende aqui)
- `nex security risk <serviceId>` → expõe `ServiceRiskProfile` (novo método no `SecurityClient`).
- Confidence score passa a considerar `KnownExploitedVulnerability` (item 3.1 do plano).

## Verificação obrigatória (no ambiente .NET)
```bash
dotnet build NexTraceOne.sln
dotnet test --filter "FullyQualifiedName!~E2E&FullyQualifiedName!~IntegrationTests"
# + testes de RLS/tenant para a query da ponte (2.2)
```
