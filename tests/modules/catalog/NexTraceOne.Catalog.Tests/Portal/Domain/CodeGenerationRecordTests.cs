using NexTraceOne.Catalog.Domain.Portal.Entities;

namespace NexTraceOne.Catalog.Tests.Portal.Domain;

/// <summary>
/// Testes de domínio para o aggregate CodeGenerationRecord.
/// Valida criação com dados válidos e atribuição correta de todas as propriedades,
/// incluindo cenários com e sem IA e com template opcional.
/// </summary>
public sealed class CodeGenerationRecordTests
{
    private static readonly DateTimeOffset Now = new(2025, 06, 15, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_Should_ReturnRecord_When_InputIsValid()
    {
        var apiAssetId = Guid.NewGuid();
        var requestedById = Guid.NewGuid();

        var record = CodeGenerationRecord.Create(
            apiAssetId,
            "Payments API",
            "2.1.0",
            requestedById,
            "CSharp",
            "SdkClient",
            "public class PaymentsClient { }",
            isAiGenerated: false,
            templateId: "sdk-csharp-v1",
            Now);

        record.ApiAssetId.Should().Be(apiAssetId);
        record.ApiName.Should().Be("Payments API");
        record.ContractVersion.Should().Be("2.1.0");
        record.RequestedById.Should().Be(requestedById);
        record.Language.Should().Be("CSharp");
        record.GenerationType.Should().Be("SdkClient");
        record.GeneratedCode.Should().Contain("PaymentsClient");
        record.IsAiGenerated.Should().BeFalse();
        record.TemplateId.Should().Be("sdk-csharp-v1");
        record.GeneratedAt.Should().Be(Now);
    }

    [Fact]
    public void Create_Should_ReturnRecord_When_AiGenerated()
    {
        var record = CodeGenerationRecord.Create(
            Guid.NewGuid(),
            "Users API",
            "1.0.0",
            Guid.NewGuid(),
            "TypeScript",
            "IntegrationExample",
            "// AI-generated integration example",
            isAiGenerated: true,
            templateId: null,
            Now);

        record.IsAiGenerated.Should().BeTrue();
        record.TemplateId.Should().BeNull();
        record.Language.Should().Be("TypeScript");
    }

    [Fact]
    public void Create_Should_SetIdAutomatically()
    {
        var record = CodeGenerationRecord.Create(
            Guid.NewGuid(),
            "Catalog API",
            "3.0.0",
            Guid.NewGuid(),
            "Python",
            "DataModels",
            "class CatalogModel: pass",
            isAiGenerated: false,
            templateId: null,
            Now);

        record.Id.Should().NotBeNull();
        record.Id.Value.Should().NotBeEmpty();
    }
}
