using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.IdentityAccess.Tests.TestDoubles;

/// <summary>
/// Implementação simples de tenant atual para cenários de teste.
/// </summary>
internal sealed class TestCurrentTenant(Guid id, bool isActive = true, string name = "Test Tenant") : ICurrentTenant
{
    public Guid Id { get; } = id;

    public string Slug { get; } = "test-tenant";

    public string Name { get; } = name;

    public bool IsActive { get; } = isActive;

    public bool HasCapability(string capability) => true;
}
