using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.IdentityAccess.Domain.Entities;

/// <summary>
/// Registo de um NexTrace Agent instalado num host.
/// Usado para contagem de Host Units para billing SaaS.
/// Prefixo de tabela: iam_
/// </summary>
public sealed class AgentRegistration : Entity<AgentRegistrationId>
{
    private AgentRegistration() { }

    public Guid TenantId { get; private set; }

    /// <summary>UUID estável do agente persistido em ~/.nexttrace/host_unit_id</summary>
    public Guid HostUnitId { get; private set; }

    public string HostName { get; private set; } = string.Empty;
    public string AgentVersion { get; private set; } = string.Empty;
    public string DeploymentMode { get; private set; } = string.Empty;

    public int CpuCores { get; private set; }
    public decimal RamGb { get; private set; }

    /// <summary>Host Units calculados: max(RamGb/8, CpuCores/4), arredondado para 0.5.</summary>
    public decimal HostUnits { get; private set; }

    public AgentRegistrationStatus Status { get; private set; }
    public DateTimeOffset LastHeartbeatAt { get; private set; }
    public DateTimeOffset RegisteredAt { get; private set; }

    public static AgentRegistration Register(
        Guid tenantId,
        Guid hostUnitId,
        string hostName,
        string agentVersion,
        string deploymentMode,
        int cpuCores,
        decimal ramGb,
        DateTimeOffset now)
    {
        Guard.Against.Default(tenantId);
        Guard.Against.Default(hostUnitId);
        Guard.Against.NullOrWhiteSpace(hostName);
        Guard.Against.NullOrWhiteSpace(agentVersion);

        var hostUnits = ComputeHostUnits(cpuCores, ramGb);

        return new AgentRegistration
        {
            Id = AgentRegistrationId.New(),
            TenantId = tenantId,
            HostUnitId = hostUnitId,
            HostName = hostName.Trim(),
            AgentVersion = agentVersion.Trim(),
            DeploymentMode = string.IsNullOrWhiteSpace(deploymentMode) ? "standalone" : deploymentMode.Trim(),
            CpuCores = Math.Max(1, cpuCores),
            RamGb = Math.Max(0.5m, ramGb),
            HostUnits = hostUnits,
            Status = AgentRegistrationStatus.Active,
            LastHeartbeatAt = now,
            RegisteredAt = now,
        };
    }

    public void RecordHeartbeat(string agentVersion, int cpuCores, decimal ramGb, DateTimeOffset now)
    {
        AgentVersion = agentVersion.Trim();
        CpuCores = Math.Max(1, cpuCores);
        RamGb = Math.Max(0.5m, ramGb);
        HostUnits = ComputeHostUnits(cpuCores, ramGb);
        Status = AgentRegistrationStatus.Active;
        LastHeartbeatAt = now;
    }

    public void MarkInactive(DateTimeOffset now)
    {
        Status = AgentRegistrationStatus.Inactive;
        LastHeartbeatAt = now;
    }

    private static decimal ComputeHostUnits(int cpuCores, decimal ramGb)
    {
        var byRam = ramGb / 8m;
        var byCpu = cpuCores / 4m;
        var raw = Math.Max(byRam, byCpu);
        return Math.Round(raw * 2m, MidpointRounding.AwayFromZero) / 2m;
    }
}
