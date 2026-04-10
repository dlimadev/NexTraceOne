using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

using VerifyContractComplianceFeature = NexTraceOne.Catalog.Application.Contracts.Features.VerifyContractCompliance.VerifyContractCompliance;
using ListContractVerificationsFeature = NexTraceOne.Catalog.Application.Contracts.Features.ListContractVerifications.ListContractVerifications;
using GetContractVerificationDetailFeature = NexTraceOne.Catalog.Application.Contracts.Features.GetContractVerificationDetail.GetContractVerificationDetail;
using GenerateContractChangelogFeature = NexTraceOne.Catalog.Application.Contracts.Features.GenerateContractChangelog.GenerateContractChangelog;
using ApproveContractChangelogFeature = NexTraceOne.Catalog.Application.Contracts.Features.ApproveContractChangelog.ApproveContractChangelog;
using ListContractChangelogsFeature = NexTraceOne.Catalog.Application.Contracts.Features.ListContractChangelogs.ListContractChangelogs;
using GetContractChangelogFeature = NexTraceOne.Catalog.Application.Contracts.Features.GetContractChangelog.GetContractChangelog;

namespace NexTraceOne.Catalog.Tests.Application.Features;

/// <summary>
/// Testes unitários para as features de verificação de compliance contratual e changelog.
/// Cobre VerifyContractCompliance, ListContractVerifications, GetContractVerificationDetail,
/// GenerateContractChangelog, ApproveContractChangelog, ListContractChangelogs e GetContractChangelog.
/// </summary>
public sealed class ContractComplianceVerificationTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 7, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly string TenantIdStr = TenantId.ToString();

    private static ICurrentTenant CreateCurrentTenant()
    {
        var tenant = Substitute.For<ICurrentTenant>();
        tenant.Id.Returns(TenantId);
        return tenant;
    }

    private static ICurrentUser CreateCurrentUser(string id = "user@test.com")
    {
        var user = Substitute.For<ICurrentUser>();
        user.Id.Returns(id);
        user.IsAuthenticated.Returns(true);
        return user;
    }

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    /// <summary>Spec JSON OpenAPI simples com um endpoint GET /pets.</summary>
    private const string SimpleSpec = """
    {
      "openapi": "3.0.0",
      "paths": {
        "/pets": {
          "get": { "summary": "List pets" }
        }
      }
    }
    """;

    /// <summary>Spec JSON OpenAPI com endpoint adicional POST /pets.</summary>
    private const string SpecWithNewEndpoint = """
    {
      "openapi": "3.0.0",
      "paths": {
        "/pets": {
          "get": { "summary": "List pets" },
          "post": { "summary": "Create pet" }
        }
      }
    }
    """;

    /// <summary>Spec JSON OpenAPI sem endpoints (todos removidos).</summary>
    private const string EmptyPathsSpec = """
    {
      "openapi": "3.0.0",
      "paths": {}
    }
    """;

    // ══════════════════════════════════════════════════════════════════
    // VerifyContractCompliance
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task VerifyContractCompliance_Should_Return_Pass_When_Specs_Are_Identical()
    {
        var apiAssetId = Guid.NewGuid();
        var cvRepo = Substitute.For<IContractVersionRepository>();
        var vRepo = Substitute.For<IContractVerificationRepository>();
        var uow = Substitute.For<IContractsUnitOfWork>();

        var latestVersion = ContractVersion.Import(apiAssetId, "1.0.0", SimpleSpec, "json", "import", ContractProtocol.OpenApi).Value;
        cvRepo.GetLatestByApiAssetAsync(apiAssetId, Arg.Any<CancellationToken>()).Returns(latestVersion);

        var sut = new VerifyContractComplianceFeature.Handler(cvRepo, vRepo, uow, CreateClock(), CreateCurrentTenant(), CreateCurrentUser());

        var result = await sut.Handle(new VerifyContractComplianceFeature.Command(
            apiAssetId.ToString(), "PetService", SimpleSpec, "json", "ci", null, null, null, null, false), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Pass");
        result.Value.BreakingChangesCount.Should().Be(0);
    }

    [Fact]
    public async Task VerifyContractCompliance_Should_Return_Block_When_Breaking_Changes_Detected()
    {
        var apiAssetId = Guid.NewGuid();
        var cvRepo = Substitute.For<IContractVersionRepository>();
        var vRepo = Substitute.For<IContractVerificationRepository>();
        var uow = Substitute.For<IContractsUnitOfWork>();

        var latestVersion = ContractVersion.Import(apiAssetId, "1.0.0", SimpleSpec, "json", "import", ContractProtocol.OpenApi).Value;
        cvRepo.GetLatestByApiAssetAsync(apiAssetId, Arg.Any<CancellationToken>()).Returns(latestVersion);

        var sut = new VerifyContractComplianceFeature.Handler(cvRepo, vRepo, uow, CreateClock(), CreateCurrentTenant(), CreateCurrentUser());

        // Submitting empty paths spec => the GET /pets endpoint is removed => breaking
        var result = await sut.Handle(new VerifyContractComplianceFeature.Command(
            apiAssetId.ToString(), "PetService", EmptyPathsSpec, "json", "ci", null, null, null, null, false), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Block");
        result.Value.BreakingChangesCount.Should().BeGreaterThan(0);
        result.Value.RemovedEndpoints.Should().NotBeEmpty();
    }

    [Fact]
    public async Task VerifyContractCompliance_Should_Return_Error_When_No_Contract_Found()
    {
        var apiAssetId = Guid.NewGuid();
        var cvRepo = Substitute.For<IContractVersionRepository>();
        var vRepo = Substitute.For<IContractVerificationRepository>();
        var uow = Substitute.For<IContractsUnitOfWork>();

        cvRepo.GetLatestByApiAssetAsync(apiAssetId, Arg.Any<CancellationToken>()).Returns((ContractVersion?)null);

        var sut = new VerifyContractComplianceFeature.Handler(cvRepo, vRepo, uow, CreateClock(), CreateCurrentTenant(), CreateCurrentUser());

        var result = await sut.Handle(new VerifyContractComplianceFeature.Command(
            apiAssetId.ToString(), "PetService", SimpleSpec, "json", "ci", null, null, null, null, false), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Error");
    }

    [Fact]
    public async Task VerifyContractCompliance_Should_Not_Persist_When_DryRun_Is_True()
    {
        var apiAssetId = Guid.NewGuid();
        var cvRepo = Substitute.For<IContractVersionRepository>();
        var vRepo = Substitute.For<IContractVerificationRepository>();
        var uow = Substitute.For<IContractsUnitOfWork>();

        var latestVersion = ContractVersion.Import(apiAssetId, "1.0.0", SimpleSpec, "json", "import", ContractProtocol.OpenApi).Value;
        cvRepo.GetLatestByApiAssetAsync(apiAssetId, Arg.Any<CancellationToken>()).Returns(latestVersion);

        var sut = new VerifyContractComplianceFeature.Handler(cvRepo, vRepo, uow, CreateClock(), CreateCurrentTenant(), CreateCurrentUser());

        var result = await sut.Handle(new VerifyContractComplianceFeature.Command(
            apiAssetId.ToString(), "PetService", SimpleSpec, "json", "ci", null, null, null, null, true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await vRepo.DidNotReceive().AddAsync(Arg.Any<ContractVerification>(), Arg.Any<CancellationToken>());
        await uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task VerifyContractCompliance_Should_Persist_Verification_When_Not_DryRun()
    {
        var apiAssetId = Guid.NewGuid();
        var cvRepo = Substitute.For<IContractVersionRepository>();
        var vRepo = Substitute.For<IContractVerificationRepository>();
        var uow = Substitute.For<IContractsUnitOfWork>();

        var latestVersion = ContractVersion.Import(apiAssetId, "1.0.0", SimpleSpec, "json", "import", ContractProtocol.OpenApi).Value;
        cvRepo.GetLatestByApiAssetAsync(apiAssetId, Arg.Any<CancellationToken>()).Returns(latestVersion);

        var sut = new VerifyContractComplianceFeature.Handler(cvRepo, vRepo, uow, CreateClock(), CreateCurrentTenant(), CreateCurrentUser());

        var result = await sut.Handle(new VerifyContractComplianceFeature.Command(
            apiAssetId.ToString(), "PetService", SimpleSpec, "json", "ci", null, null, null, null, false), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await vRepo.Received(1).AddAsync(Arg.Any<ContractVerification>(), Arg.Any<CancellationToken>());
        await uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task VerifyContractCompliance_Should_Detect_Removed_Endpoints_As_Breaking()
    {
        var apiAssetId = Guid.NewGuid();
        var cvRepo = Substitute.For<IContractVersionRepository>();
        var vRepo = Substitute.For<IContractVerificationRepository>();
        var uow = Substitute.For<IContractsUnitOfWork>();

        // Existing spec has GET /pets and POST /pets
        var latestVersion = ContractVersion.Import(apiAssetId, "1.0.0", SpecWithNewEndpoint, "json", "import", ContractProtocol.OpenApi).Value;
        cvRepo.GetLatestByApiAssetAsync(apiAssetId, Arg.Any<CancellationToken>()).Returns(latestVersion);

        var sut = new VerifyContractComplianceFeature.Handler(cvRepo, vRepo, uow, CreateClock(), CreateCurrentTenant(), CreateCurrentUser());

        // Submitting only GET /pets => POST /pets is removed => breaking
        var result = await sut.Handle(new VerifyContractComplianceFeature.Command(
            apiAssetId.ToString(), "PetService", SimpleSpec, "json", "ci", null, null, null, null, true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Block");
        result.Value.RemovedEndpoints.Should().Contain(e => e.Contains("POST", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task VerifyContractCompliance_Should_Detect_New_Endpoints_As_Additive()
    {
        var apiAssetId = Guid.NewGuid();
        var cvRepo = Substitute.For<IContractVersionRepository>();
        var vRepo = Substitute.For<IContractVerificationRepository>();
        var uow = Substitute.For<IContractsUnitOfWork>();

        // Existing spec has only GET /pets
        var latestVersion = ContractVersion.Import(apiAssetId, "1.0.0", SimpleSpec, "json", "import", ContractProtocol.OpenApi).Value;
        cvRepo.GetLatestByApiAssetAsync(apiAssetId, Arg.Any<CancellationToken>()).Returns(latestVersion);

        var sut = new VerifyContractComplianceFeature.Handler(cvRepo, vRepo, uow, CreateClock(), CreateCurrentTenant(), CreateCurrentUser());

        // Submitting GET /pets + POST /pets => POST /pets is new => additive
        var result = await sut.Handle(new VerifyContractComplianceFeature.Command(
            apiAssetId.ToString(), "PetService", SpecWithNewEndpoint, "json", "ci", null, null, null, null, true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AdditiveChangesCount.Should().BeGreaterThan(0);
        result.Value.NewEndpoints.Should().Contain(e => e.Contains("POST", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task VerifyContractCompliance_Validator_Should_Reject_Empty_ApiAssetId()
    {
        var validator = new VerifyContractComplianceFeature.Validator();

        var validationResult = await validator.ValidateAsync(new VerifyContractComplianceFeature.Command(
            "", "PetService", SimpleSpec, "json", "ci", null, null, null, null, false));

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "ApiAssetId");
    }

    [Fact]
    public async Task VerifyContractCompliance_Validator_Should_Reject_Empty_SpecContent()
    {
        var validator = new VerifyContractComplianceFeature.Validator();

        var validationResult = await validator.ValidateAsync(new VerifyContractComplianceFeature.Command(
            Guid.NewGuid().ToString(), "PetService", "", "json", "ci", null, null, null, null, false));

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "SpecContent");
    }

    // ══════════════════════════════════════════════════════════════════
    // ListContractVerifications
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ListContractVerifications_Should_Return_Verifications_By_Service()
    {
        var vRepo = Substitute.For<IContractVerificationRepository>();
        var apiAssetId = Guid.NewGuid().ToString();

        var verification = ContractVerification.Create(
            TenantIdStr, apiAssetId, "PetService", Guid.NewGuid(), "hash1",
            VerificationStatus.Pass, 0, 1, 0, "{}", "[]", "ci", null, null, null, null,
            FixedNow, FixedNow, "user1");

        vRepo.ListByServiceAsync("PetService", 1, 20, Arg.Any<CancellationToken>())
            .Returns(new List<ContractVerification> { verification });

        var sut = new ListContractVerificationsFeature.Handler(vRepo);

        var result = await sut.Handle(
            new ListContractVerificationsFeature.Query("PetService", null, 1, 20), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].ServiceName.Should().Be("PetService");
    }

    [Fact]
    public async Task ListContractVerifications_Should_Return_Verifications_By_ApiAsset()
    {
        var vRepo = Substitute.For<IContractVerificationRepository>();
        var apiAssetId = Guid.NewGuid().ToString();

        var verification = ContractVerification.Create(
            TenantIdStr, apiAssetId, "PetService", Guid.NewGuid(), "hash1",
            VerificationStatus.Pass, 0, 1, 0, "{}", "[]", "ci", null, null, null, null,
            FixedNow, FixedNow, "user1");

        vRepo.ListByApiAssetAsync(apiAssetId, Arg.Any<CancellationToken>())
            .Returns(new List<ContractVerification> { verification });

        var sut = new ListContractVerificationsFeature.Handler(vRepo);

        var result = await sut.Handle(
            new ListContractVerificationsFeature.Query(null, apiAssetId, 1, 20), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task ListContractVerifications_Validator_Should_Reject_Invalid_Page()
    {
        var validator = new ListContractVerificationsFeature.Validator();

        var validationResult = await validator.ValidateAsync(
            new ListContractVerificationsFeature.Query(null, null, 0, 20));

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "Page");
    }

    // ══════════════════════════════════════════════════════════════════
    // GetContractVerificationDetail
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetContractVerificationDetail_Should_Return_Detail_When_Found()
    {
        var vRepo = Substitute.For<IContractVerificationRepository>();
        var apiAssetId = Guid.NewGuid().ToString();

        var verification = ContractVerification.Create(
            TenantIdStr, apiAssetId, "PetService", Guid.NewGuid(), "hash1",
            VerificationStatus.Pass, 0, 1, 0, "{}", "[]", "ci", "main", "abc123", null, "prod",
            FixedNow, FixedNow, "user1");

        vRepo.GetByIdAsync(verification.Id, Arg.Any<CancellationToken>()).Returns(verification);

        var sut = new GetContractVerificationDetailFeature.Handler(vRepo);

        var result = await sut.Handle(
            new GetContractVerificationDetailFeature.Query(verification.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.VerificationId.Should().Be(verification.Id.Value);
        result.Value.ServiceName.Should().Be("PetService");
        result.Value.Status.Should().Be("Pass");
    }

    [Fact]
    public async Task GetContractVerificationDetail_Should_Return_Error_When_Not_Found()
    {
        var vRepo = Substitute.For<IContractVerificationRepository>();
        var verificationId = Guid.NewGuid();

        vRepo.GetByIdAsync(ContractVerificationId.From(verificationId), Arg.Any<CancellationToken>())
            .Returns((ContractVerification?)null);

        var sut = new GetContractVerificationDetailFeature.Handler(vRepo);

        var result = await sut.Handle(
            new GetContractVerificationDetailFeature.Query(verificationId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    // ══════════════════════════════════════════════════════════════════
    // GenerateContractChangelog
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GenerateContractChangelog_Should_Create_Changelog_Successfully()
    {
        var clRepo = Substitute.For<IContractChangelogRepository>();
        var uow = Substitute.For<IContractsUnitOfWork>();

        var sut = new GenerateContractChangelogFeature.Handler(clRepo, uow, CreateClock(), CreateCurrentTenant(), CreateCurrentUser());

        var result = await sut.Handle(new GenerateContractChangelogFeature.Command(
            Guid.NewGuid().ToString(), "PetService", "1.0.0", "1.1.0", Guid.NewGuid(), null,
            (int)ChangelogSource.Verification, "[]", "Added POST /pets endpoint", null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ChangelogId.Should().NotBe(Guid.Empty);
        result.Value.CreatedAt.Should().Be(FixedNow);
        await clRepo.Received(1).AddAsync(Arg.Any<ContractChangelog>(), Arg.Any<CancellationToken>());
        await uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateContractChangelog_Validator_Should_Reject_Empty_ApiAssetId()
    {
        var validator = new GenerateContractChangelogFeature.Validator();

        var validationResult = await validator.ValidateAsync(new GenerateContractChangelogFeature.Command(
            "", "PetService", "1.0.0", "1.1.0", Guid.NewGuid(), null,
            (int)ChangelogSource.Manual, "[]", "Summary", null));

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "ApiAssetId");
    }

    // ══════════════════════════════════════════════════════════════════
    // ApproveContractChangelog
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ApproveContractChangelog_Should_Approve_Changelog()
    {
        var clRepo = Substitute.For<IContractChangelogRepository>();
        var uow = Substitute.For<IContractsUnitOfWork>();

        var changelog = ContractChangelog.Create(
            TenantIdStr, Guid.NewGuid().ToString(), "PetService", "1.0.0", "1.1.0",
            Guid.NewGuid(), null, ChangelogSource.Manual, "[]", "Summary",
            null, null, null, FixedNow, "user1");

        clRepo.GetByIdAsync(changelog.Id, Arg.Any<CancellationToken>()).Returns(changelog);

        var sut = new ApproveContractChangelogFeature.Handler(clRepo, uow, CreateClock(), CreateCurrentUser());

        var result = await sut.Handle(
            new ApproveContractChangelogFeature.Command(changelog.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ChangelogId.Should().Be(changelog.Id.Value);
        changelog.IsApproved.Should().BeTrue();
        await uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApproveContractChangelog_Should_Return_Error_When_Changelog_Not_Found()
    {
        var clRepo = Substitute.For<IContractChangelogRepository>();
        var uow = Substitute.For<IContractsUnitOfWork>();
        var changelogId = Guid.NewGuid();

        clRepo.GetByIdAsync(ContractChangelogId.From(changelogId), Arg.Any<CancellationToken>())
            .Returns((ContractChangelog?)null);

        var sut = new ApproveContractChangelogFeature.Handler(clRepo, uow, CreateClock(), CreateCurrentUser());

        var result = await sut.Handle(
            new ApproveContractChangelogFeature.Command(changelogId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApproveContractChangelog_Should_Return_Error_When_Already_Approved()
    {
        var clRepo = Substitute.For<IContractChangelogRepository>();
        var uow = Substitute.For<IContractsUnitOfWork>();

        var changelog = ContractChangelog.Create(
            TenantIdStr, Guid.NewGuid().ToString(), "PetService", "1.0.0", "1.1.0",
            Guid.NewGuid(), null, ChangelogSource.Manual, "[]", "Summary",
            null, null, null, FixedNow, "user1");
        changelog.Approve("approver@test.com", FixedNow);

        clRepo.GetByIdAsync(changelog.Id, Arg.Any<CancellationToken>()).Returns(changelog);

        var sut = new ApproveContractChangelogFeature.Handler(clRepo, uow, CreateClock(), CreateCurrentUser());

        var result = await sut.Handle(
            new ApproveContractChangelogFeature.Command(changelog.Id.Value), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    // ══════════════════════════════════════════════════════════════════
    // ListContractChangelogs
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ListContractChangelogs_Should_Return_Changelogs_By_ApiAsset()
    {
        var clRepo = Substitute.For<IContractChangelogRepository>();
        var apiAssetId = Guid.NewGuid().ToString();

        var changelog = ContractChangelog.Create(
            TenantIdStr, apiAssetId, "PetService", "1.0.0", "1.1.0",
            Guid.NewGuid(), null, ChangelogSource.Manual, "[]", "Summary",
            null, null, null, FixedNow, "user1");

        clRepo.ListByApiAssetAsync(apiAssetId, Arg.Any<CancellationToken>())
            .Returns(new List<ContractChangelog> { changelog });

        var sut = new ListContractChangelogsFeature.Handler(clRepo);

        var result = await sut.Handle(
            new ListContractChangelogsFeature.Query(apiAssetId, false), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].ApiAssetId.Should().Be(apiAssetId);
    }

    [Fact]
    public async Task ListContractChangelogs_Should_Return_Pending_Approval_Only()
    {
        var clRepo = Substitute.For<IContractChangelogRepository>();

        var pending = ContractChangelog.Create(
            TenantIdStr, Guid.NewGuid().ToString(), "PetService", "1.0.0", "1.1.0",
            Guid.NewGuid(), null, ChangelogSource.Manual, "[]", "Pending summary",
            null, null, null, FixedNow, "user1");

        clRepo.ListPendingApprovalAsync(Arg.Any<CancellationToken>())
            .Returns(new List<ContractChangelog> { pending });

        var sut = new ListContractChangelogsFeature.Handler(clRepo);

        var result = await sut.Handle(
            new ListContractChangelogsFeature.Query(null, true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].IsApproved.Should().BeFalse();
    }

    // ══════════════════════════════════════════════════════════════════
    // GetContractChangelog
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetContractChangelog_Should_Return_Changelog_When_Found()
    {
        var clRepo = Substitute.For<IContractChangelogRepository>();

        var changelog = ContractChangelog.Create(
            TenantIdStr, Guid.NewGuid().ToString(), "PetService", "1.0.0", "1.1.0",
            Guid.NewGuid(), null, ChangelogSource.Verification, "[]", "Change summary",
            null, null, "abc123", FixedNow, "user1");

        clRepo.GetByIdAsync(changelog.Id, Arg.Any<CancellationToken>()).Returns(changelog);

        var sut = new GetContractChangelogFeature.Handler(clRepo);

        var result = await sut.Handle(
            new GetContractChangelogFeature.Query(changelog.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ChangelogId.Should().Be(changelog.Id.Value);
        result.Value.ServiceName.Should().Be("PetService");
        result.Value.Summary.Should().Be("Change summary");
    }

    [Fact]
    public async Task GetContractChangelog_Should_Return_Error_When_Not_Found()
    {
        var clRepo = Substitute.For<IContractChangelogRepository>();
        var changelogId = Guid.NewGuid();

        clRepo.GetByIdAsync(ContractChangelogId.From(changelogId), Arg.Any<CancellationToken>())
            .Returns((ContractChangelog?)null);

        var sut = new GetContractChangelogFeature.Handler(clRepo);

        var result = await sut.Handle(
            new GetContractChangelogFeature.Query(changelogId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
