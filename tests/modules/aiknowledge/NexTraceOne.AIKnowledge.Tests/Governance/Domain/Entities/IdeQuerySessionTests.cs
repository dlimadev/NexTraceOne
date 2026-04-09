using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Tests.Governance.Domain.Entities;

public sealed class IdeQuerySessionTests
{
    // ── Factory method: valid creation ───────────────────────────────────

    [Fact]
    public void Create_With_Valid_Data_Returns_Session()
    {
        var tenantId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var session = IdeQuerySession.Create(
            userId: "dev-1",
            ideClient: "vscode",
            ideClientVersion: "1.90.0",
            queryType: IdeQueryType.ContractSuggestion,
            queryText: "Suggest a REST contract for OrderService",
            queryContext: "{\"file\":\"OrderController.cs\"}",
            modelUsed: "llama3.2:3b",
            tenantId: tenantId,
            submittedAt: now);

        session.Should().NotBeNull();
        session.Id.Value.Should().NotBeEmpty();
        session.UserId.Should().Be("dev-1");
        session.IdeClient.Should().Be("vscode");
        session.IdeClientVersion.Should().Be("1.90.0");
        session.QueryType.Should().Be(IdeQueryType.ContractSuggestion);
        session.QueryText.Should().Be("Suggest a REST contract for OrderService");
        session.QueryContext.Should().Be("{\"file\":\"OrderController.cs\"}");
        session.ResponseText.Should().BeNull();
        session.ModelUsed.Should().Be("llama3.2:3b");
        session.TokensUsed.Should().Be(0);
        session.PromptTokens.Should().Be(0);
        session.CompletionTokens.Should().Be(0);
        session.Status.Should().Be(IdeQuerySessionStatus.Processing);
        session.GovernanceCheckResult.Should().BeNull();
        session.ResponseTimeMs.Should().BeNull();
        session.SubmittedAt.Should().Be(now);
        session.RespondedAt.Should().BeNull();
        session.ErrorMessage.Should().BeNull();
        session.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void Create_Generates_Unique_Ids()
    {
        var s1 = CreateValidSession();
        var s2 = CreateValidSession();
        s1.Id.Should().NotBe(s2.Id);
    }

    // ── Guard clause validation ─────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Rejects_Invalid_UserId(string? userId)
    {
        var act = () => IdeQuerySession.Create(
            userId: userId!,
            ideClient: "vscode",
            ideClientVersion: "1.0",
            queryType: IdeQueryType.GeneralQuery,
            queryText: "query",
            queryContext: null,
            modelUsed: "model",
            tenantId: Guid.NewGuid(),
            submittedAt: DateTimeOffset.UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Rejects_Invalid_IdeClient(string? ideClient)
    {
        var act = () => IdeQuerySession.Create(
            userId: "user-1",
            ideClient: ideClient!,
            ideClientVersion: "1.0",
            queryType: IdeQueryType.GeneralQuery,
            queryText: "query",
            queryContext: null,
            modelUsed: "model",
            tenantId: Guid.NewGuid(),
            submittedAt: DateTimeOffset.UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Rejects_Invalid_IdeClientVersion(string? version)
    {
        var act = () => IdeQuerySession.Create(
            userId: "user-1",
            ideClient: "vscode",
            ideClientVersion: version!,
            queryType: IdeQueryType.GeneralQuery,
            queryText: "query",
            queryContext: null,
            modelUsed: "model",
            tenantId: Guid.NewGuid(),
            submittedAt: DateTimeOffset.UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Rejects_Invalid_QueryType()
    {
        var act = () => IdeQuerySession.Create(
            userId: "user-1",
            ideClient: "vscode",
            ideClientVersion: "1.0",
            queryType: (IdeQueryType)99,
            queryText: "query",
            queryContext: null,
            modelUsed: "model",
            tenantId: Guid.NewGuid(),
            submittedAt: DateTimeOffset.UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Rejects_Invalid_QueryText(string? queryText)
    {
        var act = () => IdeQuerySession.Create(
            userId: "user-1",
            ideClient: "vscode",
            ideClientVersion: "1.0",
            queryType: IdeQueryType.GeneralQuery,
            queryText: queryText!,
            queryContext: null,
            modelUsed: "model",
            tenantId: Guid.NewGuid(),
            submittedAt: DateTimeOffset.UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Rejects_Invalid_ModelUsed(string? model)
    {
        var act = () => IdeQuerySession.Create(
            userId: "user-1",
            ideClient: "vscode",
            ideClientVersion: "1.0",
            queryType: IdeQueryType.GeneralQuery,
            queryText: "query",
            queryContext: null,
            modelUsed: model!,
            tenantId: Guid.NewGuid(),
            submittedAt: DateTimeOffset.UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Rejects_Default_TenantId()
    {
        var act = () => IdeQuerySession.Create(
            userId: "user-1",
            ideClient: "vscode",
            ideClientVersion: "1.0",
            queryType: IdeQueryType.GeneralQuery,
            queryText: "query",
            queryContext: null,
            modelUsed: "model",
            tenantId: Guid.Empty,
            submittedAt: DateTimeOffset.UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    // ── Respond ─────────────────────────────────────────────────────────

    [Fact]
    public void Respond_Sets_Response_Data_And_Status()
    {
        var session = CreateValidSession();
        var respondedAt = DateTimeOffset.UtcNow;

        session.Respond(
            responseText: "Here is the suggested contract...",
            tokensUsed: 500,
            promptTokens: 200,
            completionTokens: 300,
            responseTimeMs: 1234,
            respondedAt: respondedAt);

        session.ResponseText.Should().Be("Here is the suggested contract...");
        session.TokensUsed.Should().Be(500);
        session.PromptTokens.Should().Be(200);
        session.CompletionTokens.Should().Be(300);
        session.ResponseTimeMs.Should().Be(1234);
        session.Status.Should().Be(IdeQuerySessionStatus.Responded);
        session.RespondedAt.Should().Be(respondedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Respond_Rejects_Invalid_ResponseText(string? responseText)
    {
        var session = CreateValidSession();
        var act = () => session.Respond(responseText!, 100, 50, 50, 500, DateTimeOffset.UtcNow);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Respond_Rejects_Negative_TokensUsed()
    {
        var session = CreateValidSession();
        var act = () => session.Respond("response", -1, 0, 0, 500, DateTimeOffset.UtcNow);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Respond_Rejects_Negative_PromptTokens()
    {
        var session = CreateValidSession();
        var act = () => session.Respond("response", 100, -1, 0, 500, DateTimeOffset.UtcNow);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Respond_Rejects_Negative_CompletionTokens()
    {
        var session = CreateValidSession();
        var act = () => session.Respond("response", 100, 50, -1, 500, DateTimeOffset.UtcNow);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Respond_Rejects_Negative_ResponseTimeMs()
    {
        var session = CreateValidSession();
        var act = () => session.Respond("response", 100, 50, 50, -1, DateTimeOffset.UtcNow);
        act.Should().Throw<ArgumentException>();
    }

    // ── Block ───────────────────────────────────────────────────────────

    [Fact]
    public void Block_Sets_GovernanceResult_And_Status()
    {
        var session = CreateValidSession();
        var respondedAt = DateTimeOffset.UtcNow;

        session.Block("{\"policy\":\"external-data-blocked\"}", respondedAt);

        session.GovernanceCheckResult.Should().Be("{\"policy\":\"external-data-blocked\"}");
        session.Status.Should().Be(IdeQuerySessionStatus.Blocked);
        session.RespondedAt.Should().Be(respondedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Block_Rejects_Invalid_GovernanceCheckResult(string? result)
    {
        var session = CreateValidSession();
        var act = () => session.Block(result!, DateTimeOffset.UtcNow);
        act.Should().Throw<ArgumentException>();
    }

    // ── Fail ────────────────────────────────────────────────────────────

    [Fact]
    public void Fail_Sets_ErrorMessage_And_Status()
    {
        var session = CreateValidSession();
        var respondedAt = DateTimeOffset.UtcNow;

        session.Fail("Model inference timeout", respondedAt);

        session.ErrorMessage.Should().Be("Model inference timeout");
        session.Status.Should().Be(IdeQuerySessionStatus.Failed);
        session.RespondedAt.Should().Be(respondedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Fail_Rejects_Invalid_ErrorMessage(string? errorMessage)
    {
        var session = CreateValidSession();
        var act = () => session.Fail(errorMessage!, DateTimeOffset.UtcNow);
        act.Should().Throw<ArgumentException>();
    }

    // ── Strongly-typed ID ───────────────────────────────────────────────

    [Fact]
    public void IdeQuerySessionId_New_Creates_Unique_Id()
    {
        var id1 = IdeQuerySessionId.New();
        var id2 = IdeQuerySessionId.New();

        id1.Should().NotBe(id2);
        id1.Value.Should().NotBeEmpty();
    }

    [Fact]
    public void IdeQuerySessionId_From_Preserves_Value()
    {
        var guid = Guid.NewGuid();
        var id = IdeQuerySessionId.From(guid);

        id.Value.Should().Be(guid);
    }

    // ── Helper ──────────────────────────────────────────────────────────

    private static IdeQuerySession CreateValidSession() =>
        IdeQuerySession.Create(
            userId: "dev-1",
            ideClient: "vscode",
            ideClientVersion: "1.90.0",
            queryType: IdeQueryType.ContractSuggestion,
            queryText: "Suggest a REST contract for OrderService",
            queryContext: null,
            modelUsed: "llama3.2:3b",
            tenantId: Guid.NewGuid(),
            submittedAt: DateTimeOffset.UtcNow);
}
