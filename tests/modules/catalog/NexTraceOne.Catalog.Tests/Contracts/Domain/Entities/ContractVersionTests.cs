using NexTraceOne.Contracts.Domain.Entities;

namespace NexTraceOne.Contracts.Tests.Domain.Entities;

/// <summary>
/// Testes de domínio para o aggregate ContractVersion.
/// </summary>
public sealed class ContractVersionTests
{
    private const string ValidSpec = """{"openapi":"3.0.0","info":{"title":"Test","version":"1.0.0"},"paths":{"/users":{"get":{"responses":{"200":{"description":"OK"}}}}}}""";

    [Fact]
    public void Import_Should_CreateContractVersion_When_InputIsValid()
    {
        var apiAssetId = Guid.NewGuid();

        var result = ContractVersion.Import(apiAssetId, "1.0.0", ValidSpec, "json", "https://example.com/spec");

        result.IsSuccess.Should().BeTrue();
        result.Value.ApiAssetId.Should().Be(apiAssetId);
        result.Value.SemVer.Should().Be("1.0.0");
        result.Value.Format.Should().Be("json");
        result.Value.ImportedFrom.Should().Be("https://example.com/spec");
        result.Value.IsLocked.Should().BeFalse();
    }

    [Fact]
    public void Import_Should_ReturnFailure_When_SpecContentIsEmpty()
    {
        var result = ContractVersion.Import(Guid.NewGuid(), "1.0.0", string.Empty, "json", "upload");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contracts.ContractVersion.EmptySpecContent");
    }

    [Fact]
    public void Lock_Should_Succeed_When_VersionIsNotLocked()
    {
        var version = ContractVersion.Import(Guid.NewGuid(), "1.0.0", ValidSpec, "json", "upload").Value;
        var lockedAt = new DateTimeOffset(2025, 06, 01, 10, 0, 0, TimeSpan.Zero);

        var result = version.Lock("admin", lockedAt);

        result.IsSuccess.Should().BeTrue();
        version.IsLocked.Should().BeTrue();
        version.LockedBy.Should().Be("admin");
        version.LockedAt.Should().Be(lockedAt);
    }

    [Fact]
    public void Lock_Should_ReturnFailure_When_VersionIsAlreadyLocked()
    {
        var version = ContractVersion.Import(Guid.NewGuid(), "1.0.0", ValidSpec, "json", "upload").Value;
        var lockedAt = new DateTimeOffset(2025, 06, 01, 10, 0, 0, TimeSpan.Zero);

        version.Lock("admin", lockedAt);
        var secondResult = version.Lock("admin2", lockedAt);

        secondResult.IsFailure.Should().BeTrue();
        secondResult.Error.Code.Should().Be("Contracts.ContractVersion.AlreadyLocked");
    }
}
