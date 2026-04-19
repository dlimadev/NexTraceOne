using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Governance.Features.GetModel;
using NexTraceOne.AIKnowledge.Application.Governance.Features.ListAvailableModels;
using NexTraceOne.AIKnowledge.Application.Governance.Features.ListModels;
using NexTraceOne.AIKnowledge.Application.Governance.Features.RegisterModel;
using NexTraceOne.AIKnowledge.Application.Governance.Features.UpdateModel;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AIKnowledge.Tests.Governance.Application.Features;

/// <summary>
/// Testes unitários para RegisterModel, GetModel, ListModels, UpdateModel, ListAvailableModels.
/// Cobre registo de modelos, consulta, listagem com filtros, atualização e autorização.
/// </summary>
public sealed class ModelRegistryGapsTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly IAiModelRepository _modelRepository = Substitute.For<IAiModelRepository>();
    private readonly IAiModelAuthorizationService _authService = Substitute.For<IAiModelAuthorizationService>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    public ModelRegistryGapsTests()
    {
        _clock.UtcNow.Returns(FixedNow);
    }

    // ── RegisterModel ──────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterModel_ValidCommand_PersistsAndReturnsModelId()
    {
        AIModel? persisted = null;
        _modelRepository.AddAsync(Arg.Do<AIModel>(m => persisted = m), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var command = new RegisterModel.Command(
            Name: "gpt-4o",
            DisplayName: "GPT-4o",
            Provider: "OpenAI",
            ModelType: "Chat",
            IsInternal: false,
            Capabilities: "chat,code,reasoning",
            SensitivityLevel: 3);

        var handler = new RegisterModel.Handler(_modelRepository, _clock);
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ModelId.Should().NotBe(Guid.Empty);
        persisted.Should().NotBeNull();
        persisted!.Name.Should().Be("gpt-4o");
        persisted.Provider.Should().Be("OpenAI");
        persisted.IsInternal.Should().BeFalse();
        persisted.IsExternal.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterModel_InternalModel_SetsIsInternalTrue()
    {
        AIModel? persisted = null;
        _modelRepository.AddAsync(Arg.Do<AIModel>(m => persisted = m), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var command = new RegisterModel.Command(
            Name: "deepseek-r1",
            DisplayName: "DeepSeek R1",
            Provider: "Internal",
            ModelType: "Analysis",
            IsInternal: true,
            Capabilities: "reasoning,code",
            SensitivityLevel: 1);

        var handler = new RegisterModel.Handler(_modelRepository, _clock);
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        persisted!.IsInternal.Should().BeTrue();
        persisted.IsExternal.Should().BeFalse();
    }

    // ── GetModel ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetModel_ExistingModel_ReturnsDetails()
    {
        var modelId = Guid.NewGuid();
        var model = CreateModel("gpt-4o", "GPT-4o", "OpenAI", ModelType.Chat, isInternal: false);

        _modelRepository.GetByIdAsync(Arg.Is<AIModelId>(x => x == AIModelId.From(modelId)), Arg.Any<CancellationToken>())
            .Returns(model);

        var handler = new GetModel.Handler(_modelRepository);
        var result = await handler.Handle(new GetModel.Query(modelId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("gpt-4o");
        result.Value.Provider.Should().Be("OpenAI");
        result.Value.IsInternal.Should().BeFalse();
        result.Value.IsExternal.Should().BeTrue();
    }

    [Fact]
    public async Task GetModel_NotFound_ReturnsFailure()
    {
        _modelRepository.GetByIdAsync(Arg.Any<AIModelId>(), Arg.Any<CancellationToken>())
            .Returns((AIModel?)null);

        var handler = new GetModel.Handler(_modelRepository);
        var result = await handler.Handle(new GetModel.Query(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Model.NotFound");
    }

    // ── ListModels ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ListModels_NoFilters_ReturnsAllModels()
    {
        var models = new List<AIModel>
        {
            CreateModel("gpt-4o", "GPT-4o", "OpenAI", ModelType.Chat, isInternal: false),
            CreateModel("deepseek-r1", "DeepSeek R1", "Ollama", ModelType.Analysis, isInternal: true),
        };
        _modelRepository.ListAsync(null, null, null, null, Arg.Any<CancellationToken>())
            .Returns(models.AsReadOnly());

        var handler = new ListModels.Handler(_modelRepository);
        var result = await handler.Handle(new ListModels.Query(null, null, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(2);
        result.Value.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task ListModels_FilterByProvider_PassesProviderToRepository()
    {
        var models = new List<AIModel>
        {
            CreateModel("gpt-4o", "GPT-4o", "OpenAI", ModelType.Chat, isInternal: false),
        };
        _modelRepository.ListAsync("OpenAI", null, null, null, Arg.Any<CancellationToken>())
            .Returns(models.AsReadOnly());

        var handler = new ListModels.Handler(_modelRepository);
        var result = await handler.Handle(new ListModels.Query("OpenAI", null, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
        result.Value.Items[0].Provider.Should().Be("OpenAI");
    }

    [Fact]
    public async Task ListModels_FilterInternalOnly_PassesFlagToRepository()
    {
        var models = new List<AIModel>
        {
            CreateModel("deepseek", "DeepSeek", "Ollama", ModelType.Chat, isInternal: true),
        };
        _modelRepository.ListAsync(null, null, null, true, Arg.Any<CancellationToken>())
            .Returns(models.AsReadOnly());

        var handler = new ListModels.Handler(_modelRepository);
        var result = await handler.Handle(new ListModels.Query(null, null, null, true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(x => x.IsInternal);
    }

    [Fact]
    public async Task ListModels_EmptyResult_ReturnsTotalCountZero()
    {
        _modelRepository.ListAsync(null, null, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<AIModel>().AsReadOnly());

        var handler = new ListModels.Handler(_modelRepository);
        var result = await handler.Handle(new ListModels.Query(null, null, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(0);
    }

    // ── UpdateModel ────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateModel_UpdateDisplayName_PersistsChange()
    {
        var modelId = Guid.NewGuid();
        var model = CreateModel("gpt-4o", "GPT-4o", "OpenAI", ModelType.Chat, isInternal: false);

        _modelRepository.GetByIdAsync(Arg.Is<AIModelId>(x => x == AIModelId.From(modelId)), Arg.Any<CancellationToken>())
            .Returns(model);

        var command = new UpdateModel.Command(
            ModelId: modelId,
            DisplayName: "GPT-4o Updated",
            Capabilities: null,
            DefaultUseCases: null,
            SensitivityLevel: null,
            NewStatus: null);

        var handler = new UpdateModel.Handler(_modelRepository);
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _modelRepository.Received(1).UpdateAsync(model, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateModel_ModelNotFound_ReturnsFailure()
    {
        _modelRepository.GetByIdAsync(Arg.Any<AIModelId>(), Arg.Any<CancellationToken>())
            .Returns((AIModel?)null);

        var command = new UpdateModel.Command(
            ModelId: Guid.NewGuid(),
            DisplayName: "Name",
            Capabilities: null,
            DefaultUseCases: null,
            SensitivityLevel: null,
            NewStatus: null);

        var handler = new UpdateModel.Handler(_modelRepository);
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Model.NotFound");
    }

    [Fact]
    public async Task UpdateModel_StatusTransition_DeactivatesModel()
    {
        var modelId = Guid.NewGuid();
        var model = CreateModel("old-model", "Old Model", "OpenAI", ModelType.Chat, isInternal: false);

        _modelRepository.GetByIdAsync(Arg.Is<AIModelId>(x => x == AIModelId.From(modelId)), Arg.Any<CancellationToken>())
            .Returns(model);

        var command = new UpdateModel.Command(
            ModelId: modelId,
            DisplayName: null,
            Capabilities: null,
            DefaultUseCases: null,
            SensitivityLevel: null,
            NewStatus: "Inactive");

        var handler = new UpdateModel.Handler(_modelRepository);
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _modelRepository.Received(1).UpdateAsync(model, Arg.Any<CancellationToken>());
    }

    // ── ListAvailableModels ─────────────────────────────────────────────────

    [Fact]
    public async Task ListAvailableModels_ReturnsInternalAndExternalSeparated()
    {
        var authResult = new ModelAuthorizationResult(
            Models: new List<AuthorizedModel>
            {
                new(Guid.NewGuid(), "deepseek-r1", "DeepSeek R1", "Ollama", "Chat",
                    IsInternal: true, IsExternal: false, "Active", "chat,code", true, "deepseek-r1", 8192),
                new(Guid.NewGuid(), "gpt-4o", "GPT-4o", "OpenAI", "Chat",
                    IsInternal: false, IsExternal: true, "Active", "chat,code,vision", false, "gpt-4o", 128000),
            },
            AllowExternalModels: true,
            AppliedPolicyName: "default-policy");

        _authService.GetAvailableModelsAsync(Arg.Any<CancellationToken>())
            .Returns(authResult);

        var handler = new ListAvailableModels.Handler(_authService);
        var result = await handler.Handle(new ListAvailableModels.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.InternalModels.Should().ContainSingle(m => m.IsInternal);
        result.Value.ExternalModels.Should().ContainSingle(m => m.IsExternal);
        result.Value.AllowExternalModels.Should().BeTrue();
        result.Value.AppliedPolicyName.Should().Be("default-policy");
        result.Value.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task ListAvailableModels_ExternalNotAllowed_ExternalListEmpty()
    {
        var authResult = new ModelAuthorizationResult(
            Models: new List<AuthorizedModel>
            {
                new(Guid.NewGuid(), "deepseek-r1", "DeepSeek R1", "Ollama", "Chat",
                    IsInternal: true, IsExternal: false, "Active", "chat", true, null, null),
            },
            AllowExternalModels: false,
            AppliedPolicyName: "internal-only-policy");

        _authService.GetAvailableModelsAsync(Arg.Any<CancellationToken>())
            .Returns(authResult);

        var handler = new ListAvailableModels.Handler(_authService);
        var result = await handler.Handle(new ListAvailableModels.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ExternalModels.Should().BeEmpty();
        result.Value.AllowExternalModels.Should().BeFalse();
        result.Value.TotalCount.Should().Be(1);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private AIModel CreateModel(
        string name, string displayName, string provider,
        ModelType modelType, bool isInternal) =>
        AIModel.Register(
            name, displayName, provider, modelType, isInternal,
            capabilities: "chat", sensitivityLevel: 3, registeredAt: FixedNow);
}
