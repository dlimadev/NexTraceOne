using NexTraceOne.BuildingBlocks.Application.Abstractions;

using GetTenantSchemasFeature = NexTraceOne.Governance.Application.Features.GetTenantSchemas.GetTenantSchemas;
using ProvisionTenantSchemaFeature = NexTraceOne.Governance.Application.Features.GetTenantSchemas.ProvisionTenantSchema;

namespace NexTraceOne.Governance.Tests.Application.Features;

/// <summary>
/// Testes unitários para GetTenantSchemas e ProvisionTenantSchema.
/// Valida listagem de schemas, derivação de nomes e provisionamento idempotente.
/// </summary>
public sealed class GetTenantSchemasTests
{
    private readonly ITenantSchemaManager _schemaManager = Substitute.For<ITenantSchemaManager>();

    // ── GetTenantSchemas — happy path ─────────────────────────────────────────

    [Fact]
    public async Task GetSchemas_WhenSchemasExist_ReturnsAllItems()
    {
        _schemaManager.ListTenantSchemasAsync(Arg.Any<CancellationToken>())
            .Returns(new List<string> { "acme", "globocorp" });

        _schemaManager.GetSearchPath("acme").Returns("tenant_acme, public");
        _schemaManager.GetSearchPath("globocorp").Returns("tenant_globocorp, public");

        var handler = new GetTenantSchemasFeature.Handler(_schemaManager);
        var result = await handler.Handle(new GetTenantSchemasFeature.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalSchemas.Should().Be(2);
        result.Value.Schemas.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetSchemas_DerivesSchemaName_FromSlug()
    {
        _schemaManager.ListTenantSchemasAsync(Arg.Any<CancellationToken>())
            .Returns(new List<string> { "bankxyz" });

        _schemaManager.GetSearchPath("bankxyz").Returns("tenant_bankxyz, public");

        var handler = new GetTenantSchemasFeature.Handler(_schemaManager);
        var result = await handler.Handle(new GetTenantSchemasFeature.Query(), CancellationToken.None);

        var schema = result.Value.Schemas[0];
        schema.TenantSlug.Should().Be("bankxyz");
        schema.SchemaName.Should().Be("tenant_bankxyz");
        schema.SearchPath.Should().Be("tenant_bankxyz, public");
    }

    [Fact]
    public async Task GetSchemas_WhenNoSchemas_ReturnsTotalZero()
    {
        _schemaManager.ListTenantSchemasAsync(Arg.Any<CancellationToken>())
            .Returns(new List<string>());

        var handler = new GetTenantSchemasFeature.Handler(_schemaManager);
        var result = await handler.Handle(new GetTenantSchemasFeature.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalSchemas.Should().Be(0);
        result.Value.Schemas.Should().BeEmpty();
    }

    // ── ProvisionTenantSchema ─────────────────────────────────────────────────

    [Fact]
    public async Task ProvisionSchema_NewTenant_CreatesSchema_AndReturnsWasCreatedTrue()
    {
        _schemaManager.EnsureSchemaCreatedAsync("newcorp", Arg.Any<CancellationToken>())
            .Returns(true);

        var handler = new ProvisionTenantSchemaFeature.Handler(_schemaManager);
        var result = await handler.Handle(
            new ProvisionTenantSchemaFeature.Command("newcorp"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TenantSlug.Should().Be("newcorp");
        result.Value.SchemaName.Should().Be("tenant_newcorp");
        result.Value.WasCreated.Should().BeTrue();
    }

    [Fact]
    public async Task ProvisionSchema_ExistingTenant_ReturnsWasCreatedFalse()
    {
        _schemaManager.EnsureSchemaCreatedAsync("acme", Arg.Any<CancellationToken>())
            .Returns(false);

        var handler = new ProvisionTenantSchemaFeature.Handler(_schemaManager);
        var result = await handler.Handle(
            new ProvisionTenantSchemaFeature.Command("acme"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.WasCreated.Should().BeFalse();
    }

    [Fact]
    public async Task ProvisionSchema_EmptySlug_ReturnsValidationError()
    {
        var handler = new ProvisionTenantSchemaFeature.Handler(_schemaManager);
        var result = await handler.Handle(
            new ProvisionTenantSchemaFeature.Command(""),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await _schemaManager.DidNotReceive().EnsureSchemaCreatedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
