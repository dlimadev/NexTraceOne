using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;

using CreateFeature = NexTraceOne.Configuration.Application.Features.CreateServiceCustomField.CreateServiceCustomField;
using DeleteFeature = NexTraceOne.Configuration.Application.Features.DeleteServiceCustomField.DeleteServiceCustomField;
using ListFeature = NexTraceOne.Configuration.Application.Features.ListServiceCustomFields.ListServiceCustomFields;

namespace NexTraceOne.Configuration.Tests.Application.Features;

/// <summary>
/// Testes de CreateServiceCustomField, DeleteServiceCustomField e ListServiceCustomFields —
/// gestão de campos personalizados para serviços no catálogo.
/// </summary>
public sealed class ServiceCustomFieldTests
{
    private const string TenantId = "tenant-001";
    private static readonly DateTimeOffset FixedNow = new(2025, 1, 15, 10, 0, 0, TimeSpan.Zero);

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    // ── CreateServiceCustomField ──────────────────────────────────────────────

    [Fact]
    public async Task CreateServiceCustomField_Should_Create_Successfully()
    {
        var repo = Substitute.For<IServiceCustomFieldRepository>();
        var clock = CreateClock();

        var sut = new CreateFeature.Handler(repo, clock);
        var result = await sut.Handle(
            new CreateFeature.Command(TenantId, "Cost Center", "Text", false, "", 0),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.FieldName.Should().Be("Cost Center");
        result.Value.FieldId.Should().NotBeEmpty();
        await repo.Received(1).AddAsync(Arg.Any<ServiceCustomField>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateServiceCustomField_Should_Return_Correct_Fields()
    {
        var repo = Substitute.For<IServiceCustomFieldRepository>();
        var clock = CreateClock();

        var sut = new CreateFeature.Handler(repo, clock);
        var result = await sut.Handle(
            new CreateFeature.Command(TenantId, "Region", "Select", true, "EU", 5),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var response = result.Value;
        response.FieldName.Should().Be("Region");
        response.FieldType.Should().Be("Select");
        response.IsRequired.Should().BeTrue();
        response.SortOrder.Should().Be(5);
    }

    // ── DeleteServiceCustomField ──────────────────────────────────────────────

    [Fact]
    public async Task DeleteServiceCustomField_Should_Delete_When_Found()
    {
        var repo = Substitute.For<IServiceCustomFieldRepository>();
        var field = ServiceCustomField.Create(TenantId, "Tier", "Text", false, "", 0, FixedNow);

        repo.GetByIdAsync(Arg.Any<ServiceCustomFieldId>(), TenantId, Arg.Any<CancellationToken>())
            .Returns(field);

        var sut = new DeleteFeature.Handler(repo);
        var result = await sut.Handle(
            new DeleteFeature.Command(field.Id.Value, TenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        await repo.Received(1).DeleteAsync(Arg.Any<ServiceCustomFieldId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteServiceCustomField_Should_Fail_When_Not_Found()
    {
        var repo = Substitute.For<IServiceCustomFieldRepository>();

        repo.GetByIdAsync(Arg.Any<ServiceCustomFieldId>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((ServiceCustomField?)null);

        var sut = new DeleteFeature.Handler(repo);
        var result = await sut.Handle(
            new DeleteFeature.Command(Guid.NewGuid(), TenantId),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    // ── ListServiceCustomFields ───────────────────────────────────────────────

    [Fact]
    public async Task ListServiceCustomFields_Should_Return_Fields()
    {
        var repo = Substitute.For<IServiceCustomFieldRepository>();

        var fields = new List<ServiceCustomField>
        {
            ServiceCustomField.Create(TenantId, "Cost Center", "Text", false, "", 1, FixedNow),
            ServiceCustomField.Create(TenantId, "Region", "Select", true, "EU", 0, FixedNow),
        };
        repo.ListByTenantAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(fields);

        var sut = new ListFeature.Handler(repo);
        var result = await sut.Handle(new ListFeature.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.Items[0].FieldName.Should().Be("Region");
        result.Value.Items[1].FieldName.Should().Be("Cost Center");
    }

    [Fact]
    public async Task ListServiceCustomFields_Should_Return_Empty_When_None()
    {
        var repo = Substitute.For<IServiceCustomFieldRepository>();

        repo.ListByTenantAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(new List<ServiceCustomField>());

        var sut = new ListFeature.Handler(repo);
        var result = await sut.Handle(new ListFeature.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
    }
}
