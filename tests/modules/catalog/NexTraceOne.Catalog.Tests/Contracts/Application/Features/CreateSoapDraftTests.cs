using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

using CreateSoapDraftFeature = NexTraceOne.Catalog.Application.Contracts.Features.CreateSoapDraft.CreateSoapDraft;

namespace NexTraceOne.Catalog.Tests.Contracts.Application.Features;

/// <summary>
/// Testes unitários para <see cref="CreateSoapDraftFeature"/>.
/// Valida o workflow de criação de draft SOAP: criação de ContractDraft com tipo e protocolo corretos
/// e população do SoapDraftMetadata com os metadados SOAP fornecidos.
/// </summary>
public sealed class CreateSoapDraftTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 06, 01, 10, 0, 0, TimeSpan.Zero);

    private static IContractDraftRepository CreateDraftRepository() =>
        Substitute.For<IContractDraftRepository>();

    private static ISoapDraftMetadataRepository CreateMetadataRepository() =>
        Substitute.For<ISoapDraftMetadataRepository>();

    private static IContractsUnitOfWork CreateUnitOfWork() =>
        Substitute.For<IContractsUnitOfWork>();

    private static IDateTimeProvider CreateDateTimeProvider()
    {
        var provider = Substitute.For<IDateTimeProvider>();
        provider.UtcNow.Returns(FixedNow);
        return provider;
    }

    [Fact]
    public async Task Handle_Should_Create_Draft_With_Soap_Type_And_Wsdl_Protocol()
    {
        var draftRepo = CreateDraftRepository();
        var metaRepo = CreateMetadataRepository();
        var unitOfWork = CreateUnitOfWork();
        var dateTimeProvider = CreateDateTimeProvider();
        var sut = new CreateSoapDraftFeature.Handler(draftRepo, metaRepo, unitOfWork, dateTimeProvider);

        var command = new CreateSoapDraftFeature.Command(
            Title: "Payment Service Contract",
            Author: "dev@example.com",
            ServiceName: "PaymentService",
            TargetNamespace: "http://example.com/payments");

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        draftRepo.Received(1).Add(Arg.Is<ContractDraft>(d =>
            d.ContractType == ContractType.Soap
            && d.Protocol == ContractProtocol.Wsdl
            && d.Title == "Payment Service Contract"));
    }

    [Fact]
    public async Task Handle_Should_Create_SoapDraftMetadata_With_ServiceName_And_Namespace()
    {
        var draftRepo = CreateDraftRepository();
        var metaRepo = CreateMetadataRepository();
        var unitOfWork = CreateUnitOfWork();
        var dateTimeProvider = CreateDateTimeProvider();
        var sut = new CreateSoapDraftFeature.Handler(draftRepo, metaRepo, unitOfWork, dateTimeProvider);

        var command = new CreateSoapDraftFeature.Command(
            Title: "Payment Service Contract",
            Author: "dev@example.com",
            ServiceName: "PaymentService",
            TargetNamespace: "http://example.com/payments",
            SoapVersion: "1.2");

        await sut.Handle(command, CancellationToken.None);

        metaRepo.Received(1).Add(Arg.Is<SoapDraftMetadata>(m =>
            m.ServiceName == "PaymentService"
            && m.TargetNamespace == "http://example.com/payments"
            && m.SoapVersion == "1.2"));
    }

    [Fact]
    public async Task Handle_Should_Return_DraftId_And_SoapVersion_In_Response()
    {
        var draftRepo = CreateDraftRepository();
        var metaRepo = CreateMetadataRepository();
        var unitOfWork = CreateUnitOfWork();
        var dateTimeProvider = CreateDateTimeProvider();
        var sut = new CreateSoapDraftFeature.Handler(draftRepo, metaRepo, unitOfWork, dateTimeProvider);

        var command = new CreateSoapDraftFeature.Command(
            Title: "Test Contract",
            Author: "dev@example.com",
            ServiceName: "TestService",
            TargetNamespace: "http://example.com/test",
            SoapVersion: "1.1");

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DraftId.Should().NotBeEmpty();
        result.Value.Title.Should().Be("Test Contract");
        result.Value.SoapVersion.Should().Be("1.1");
        result.Value.ServiceName.Should().Be("TestService");
        result.Value.TargetNamespace.Should().Be("http://example.com/test");
    }

    [Fact]
    public async Task Handle_Should_Commit_UnitOfWork()
    {
        var draftRepo = CreateDraftRepository();
        var metaRepo = CreateMetadataRepository();
        var unitOfWork = CreateUnitOfWork();
        var dateTimeProvider = CreateDateTimeProvider();
        var sut = new CreateSoapDraftFeature.Handler(draftRepo, metaRepo, unitOfWork, dateTimeProvider);

        var command = new CreateSoapDraftFeature.Command(
            Title: "Test",
            Author: "dev@example.com",
            ServiceName: "TestService",
            TargetNamespace: "http://example.com");

        await sut.Handle(command, CancellationToken.None);

        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // ── Validator tests ──────────────────────────────────────────────

    [Theory]
    [InlineData("1.0")]
    [InlineData("2.0")]
    [InlineData("")]
    public void Validator_Should_Fail_For_Invalid_SoapVersion(string soapVersion)
    {
        var validator = new CreateSoapDraftFeature.Validator();
        var command = new CreateSoapDraftFeature.Command(
            Title: "Test",
            Author: "dev@example.com",
            ServiceName: "TestService",
            TargetNamespace: "http://example.com",
            SoapVersion: soapVersion);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_Should_Pass_For_Valid_Command()
    {
        var validator = new CreateSoapDraftFeature.Validator();
        var command = new CreateSoapDraftFeature.Command(
            Title: "Payment Service",
            Author: "dev@example.com",
            ServiceName: "PaymentService",
            TargetNamespace: "http://example.com/payments",
            SoapVersion: "1.1");

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
