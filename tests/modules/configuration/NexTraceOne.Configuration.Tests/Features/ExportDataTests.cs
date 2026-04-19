using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Configuration.Application.Features.ExportData;

namespace NexTraceOne.Configuration.Tests.Features;

/// <summary>
/// Testes unitários para <see cref="ExportData.Handler"/> (ACT-023).
/// Valida geração de CSV e JSON para entidades suportadas,
/// validação de input e formato de ficheiro.
/// </summary>
public sealed class ExportDataTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 19, 14, 0, 0, TimeSpan.Zero);

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> SampleContractRows() =>
    [
        new Dictionary<string, object?> { ["id"] = "c-001", ["name"] = "Payments API", ["protocol"] = "OpenApi" },
        new Dictionary<string, object?> { ["id"] = "c-002", ["name"] = "Orders API", ["protocol"] = "Wsdl" }
    ];

    // ── Validator ─────────────────────────────────────────────────────────────

    [Fact]
    public void Validator_Should_Pass_For_Contracts_Csv()
    {
        var validator = new ExportData.Validator();
        var result = validator.Validate(new ExportData.Command("contracts", "csv", null));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_Should_Pass_For_AuditEvents_Json()
    {
        var validator = new ExportData.Validator();
        var result = validator.Validate(new ExportData.Command("audit_events", "json", null));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_Should_Reject_UnsupportedEntity()
    {
        var validator = new ExportData.Validator();
        var result = validator.Validate(new ExportData.Command("invalid_entity", "csv", null));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Entity");
    }

    [Fact]
    public void Validator_Should_Reject_UnsupportedFormat()
    {
        var validator = new ExportData.Validator();
        var result = validator.Validate(new ExportData.Command("contracts", "xml", null));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Format");
    }

    [Fact]
    public void Validator_Should_Reject_EmptyEntity()
    {
        var validator = new ExportData.Validator();
        var result = validator.Validate(new ExportData.Command("", "csv", null));
        result.IsValid.Should().BeFalse();
    }

    // ── Handler — CSV ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Handler_Should_Return_Csv_When_FormatIsCsv()
    {
        var repo = Substitute.For<IExportDataRepository>();
        repo.GetExportRowsAsync("contracts", null, Arg.Any<CancellationToken>())
            .Returns(SampleContractRows());

        var handler = new ExportData.Handler(repo, CreateClock());
        var result = await handler.Handle(
            new ExportData.Command("contracts", "csv", null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ContentType.Should().Be("text/csv");
        result.Value.FileName.Should().StartWith("contracts_").And.EndWith(".csv");

        var csv = System.Text.Encoding.UTF8.GetString(result.Value.Content);
        csv.Should().Contain("id");
        csv.Should().Contain("name");
        csv.Should().Contain("c-001");
        csv.Should().Contain("Payments API");
    }

    [Fact]
    public async Task Handler_Should_Return_Json_When_FormatIsJson()
    {
        var repo = Substitute.For<IExportDataRepository>();
        repo.GetExportRowsAsync("contracts", null, Arg.Any<CancellationToken>())
            .Returns(SampleContractRows());

        var handler = new ExportData.Handler(repo, CreateClock());
        var result = await handler.Handle(
            new ExportData.Command("contracts", "json", null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ContentType.Should().Be("application/json");
        result.Value.FileName.Should().StartWith("contracts_").And.EndWith(".json");

        var json = System.Text.Encoding.UTF8.GetString(result.Value.Content);
        json.Should().Contain("c-001");
        json.Should().Contain("Payments API");
    }

    [Fact]
    public async Task Handler_Should_Return_EmptyCsv_When_NoRows()
    {
        var repo = Substitute.For<IExportDataRepository>();
        repo.GetExportRowsAsync("audit_events", null, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<IReadOnlyDictionary<string, object?>>());

        var handler = new ExportData.Handler(repo, CreateClock());
        var result = await handler.Handle(
            new ExportData.Command("audit_events", "csv", null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Content.Should().BeEmpty();
    }

    [Fact]
    public async Task Handler_Should_Include_ColumnFilter_When_Provided()
    {
        var columns = new[] { "id", "name" };
        var repo = Substitute.For<IExportDataRepository>();
        repo.GetExportRowsAsync("contracts", columns, Arg.Any<CancellationToken>())
            .Returns([
                new Dictionary<string, object?> { ["id"] = "c-001", ["name"] = "Payments API" }
            ]);

        var handler = new ExportData.Handler(repo, CreateClock());
        var result = await handler.Handle(
            new ExportData.Command("contracts", "csv", columns), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await repo.Received(1).GetExportRowsAsync("contracts", columns, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handler_Should_IncludeTimestamp_In_FileName()
    {
        var repo = Substitute.For<IExportDataRepository>();
        repo.GetExportRowsAsync(Arg.Any<string>(), Arg.Any<string[]?>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = new ExportData.Handler(repo, CreateClock());
        var result = await handler.Handle(
            new ExportData.Command("contracts", "csv", null), CancellationToken.None);

        // FixedNow = 2026-04-19 14:00:00 → "20260419_140000"
        result.Value.FileName.Should().Contain("20260419_140000");
    }

    [Fact]
    public async Task Handler_Should_EscapeCommas_In_CsvValues()
    {
        var repo = Substitute.For<IExportDataRepository>();
        repo.GetExportRowsAsync("contracts", null, Arg.Any<CancellationToken>())
            .Returns([
                new Dictionary<string, object?> { ["name"] = "Service, With Comma" }
            ]);

        var handler = new ExportData.Handler(repo, CreateClock());
        var result = await handler.Handle(
            new ExportData.Command("contracts", "csv", null), CancellationToken.None);

        var csv = System.Text.Encoding.UTF8.GetString(result.Value.Content);
        // Valores com vírgula devem estar entre aspas
        csv.Should().Contain("\"Service, With Comma\"");
    }
}
