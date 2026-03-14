using NexTraceOne.BuildingBlocks.Domain.Enums;
using NexTraceOne.Contracts.Domain.Entities;
using NexTraceOne.Contracts.Domain.Enums;

namespace NexTraceOne.Contracts.Tests.Domain.Entities;

/// <summary>
/// Testes unitários para <see cref="ContractEvidencePack"/>.
/// Valida criação e integridade do pacote de evidências para workflow.
/// </summary>
public sealed class ContractEvidencePackTests
{
    [Fact]
    public void Create_Should_BuildEvidencePack_When_AllFieldsProvided()
    {
        var contractVersionId = ContractVersionId.New();
        var apiAssetId = Guid.NewGuid();
        var consumers = new List<string> { "ServiceA", "ServiceB" };

        var pack = ContractEvidencePack.Create(
            contractVersionId, apiAssetId, ContractProtocol.OpenApi, "2.0.0",
            ChangeLevel.Breaking, 3, 1, 2, "2.0.0",
            0.65m, 0.35m, 2, true, true,
            "Breaking change in Users API",
            "3 breaking, 1 additive, 2 non-breaking changes",
            consumers, DateTimeOffset.UtcNow, "system");

        pack.ContractVersionId.Should().Be(contractVersionId);
        pack.ApiAssetId.Should().Be(apiAssetId);
        pack.Protocol.Should().Be(ContractProtocol.OpenApi);
        pack.ChangeLevel.Should().Be(ChangeLevel.Breaking);
        pack.BreakingChangeCount.Should().Be(3);
        pack.RequiresWorkflowApproval.Should().BeTrue();
        pack.RequiresChangeNotification.Should().BeTrue();
        pack.ImpactedConsumers.Should().HaveCount(2);
        pack.OverallScore.Should().Be(0.65m);
        pack.RiskScore.Should().Be(0.35m);
    }

    [Fact]
    public void Create_Should_ClampScores_When_ValuesExceedRange()
    {
        var pack = ContractEvidencePack.Create(
            ContractVersionId.New(), Guid.NewGuid(), ContractProtocol.OpenApi, "1.0.0",
            ChangeLevel.NonBreaking, 0, 0, 0, "1.0.0",
            1.5m, -0.5m, 0, false, false,
            "Summary", "Technical", [], DateTimeOffset.UtcNow, "user");

        pack.OverallScore.Should().Be(1.0m);
        pack.RiskScore.Should().Be(0.0m);
    }

    [Fact]
    public void Create_Should_GenerateUniqueId()
    {
        var pack1 = ContractEvidencePack.Create(
            ContractVersionId.New(), Guid.NewGuid(), ContractProtocol.OpenApi, "1.0.0",
            ChangeLevel.NonBreaking, 0, 0, 0, "1.0.0", 0.5m, 0.5m, 0, false, false,
            "S1", "T1", [], DateTimeOffset.UtcNow, "user");

        var pack2 = ContractEvidencePack.Create(
            ContractVersionId.New(), Guid.NewGuid(), ContractProtocol.OpenApi, "1.0.0",
            ChangeLevel.NonBreaking, 0, 0, 0, "1.0.0", 0.5m, 0.5m, 0, false, false,
            "S2", "T2", [], DateTimeOffset.UtcNow, "user");

        pack1.Id.Should().NotBe(pack2.Id);
    }

    [Fact]
    public void Create_Should_StoreImpactedConsumers()
    {
        var consumers = new List<string> { "PaymentService", "ShippingService", "NotificationService" };

        var pack = ContractEvidencePack.Create(
            ContractVersionId.New(), Guid.NewGuid(), ContractProtocol.AsyncApi, "1.0.0",
            ChangeLevel.Breaking, 1, 0, 0, "2.0.0", 0.3m, 0.7m, 3, true, true,
            "Breaking event schema change", "Field removed from event payload",
            consumers, DateTimeOffset.UtcNow, "system");

        pack.ImpactedConsumers.Should().HaveCount(3);
        pack.ImpactedConsumers.Should().Contain("PaymentService");
        pack.Protocol.Should().Be(ContractProtocol.AsyncApi);
    }
}
